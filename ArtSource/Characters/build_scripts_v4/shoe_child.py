import bpy, bmesh, math
from mathutils import Vector, Matrix, noise
import cloth, trim_child
from cloth import sstep

TONGUE_UV = (0.12, 0.38)

def build_shoe(sg, collar_z=0.074, off=0.0055, sole_d=0.0075, seed=1, hole=None):
    """sg=+1 left (x>0), -1 right. hole=(center, radius) to cut a tear in the upper."""
    bm = cloth.ref_bm()
    cloth.cut(bm, (0, 0, collar_z), (0, 0, 1), 'outer')
    cloth.delete_faces_where(bm, lambda c: c.x * sg < 0.02)
    cloth.keep_largest(bm)
    bm.normal_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # smooth toes (merge toe bumps), strong near front
    for it in range(26):
        vs = [v for v in bm.verts if v.co.y < -0.07 or it < 3]
        bmesh.ops.smooth_vert(bm, verts=vs, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    bm.normal_update()
    for v in bm.verts:
        n = v.normal
        k = off * (1.0 + 0.35 * sstep(0.03, 0.07, v.co.z))
        v.co = v.co + n * k
    bm.normal_update()
    # flatten bottom: any vert below 0.004 -> z=0
    for v in bm.verts:
        if v.co.z < 0.006: v.co.z = min(v.co.z, 0.0) if v.co.z < 0.002 else v.co.z * 0.5
    bm.normal_update()
    # sole: bottom faces
    bot = [f for f in bm.faces if f.normal.z < -0.55 and f.calc_center_median().z < 0.012]
    ext = bmesh.ops.extrude_face_region(bm, geom=bot)
    nv = [e for e in ext['geom'] if isinstance(e, bmesh.types.BMVert)]
    for v in nv:
        v.co.z -= sole_d
    # welt: push the lower ring outward in xy from foot center line
    cx = 0.138 * sg
    ring = [v for v in nv]
    bm.normal_update()
    boundary = [v for v in ring if any(f not in bot and len(v.link_faces) > 0 for f in v.link_faces)]
    sole_faces = list({f for v in nv for f in v.link_faces})
    for v in nv:
        d = Vector((v.co.x - cx, v.co.y + 0.06, 0))
        if d.length > 1e-6:
            d.normalize()
            v.co += d * 0.0025
    # remove inner bottom faces (original bot faces were consumed by extrude: they remain -> delete)
    bmesh.ops.delete(bm, geom=[f for f in bot if f.is_valid], context='FACES')
    bm.normal_update()
    # tear/hole
    if hole:
        c = Vector(hole[0]); r = hole[1]
        fs = [f for f in bm.faces if (f.calc_center_median() - c).length < r and f.normal.z > 0.1]
        bmesh.ops.delete(bm, geom=fs, context='FACES')
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    return bm, sole_faces

def build_tongue_laces(shoe_obj, sg, rect, thread_uv):
    bvh = trim_child.obj_bvh(shoe_obj)
    bm = bmesh.new()
    pu = bm.verts.layers.float.new('pu'); pv = bm.verts.layers.float.new('pv')
    cx = 0.138 * sg
    # tongue path along instep top
    ys = [-0.080 + i * 0.0085 for i in range(8)]
    pts = []; nrm = []
    for y in ys:
        h = bvh.ray_cast(Vector((cx, y, 0.2)), Vector((0, 0, -1)), 0.5)
        if h[0] is None: continue
        pts.append(h[0]); nrm.append(h[1].normalized())
    # extend upward past collar: last point lifted and tilted back
    last = pts[-1]
    pts.append(last + Vector((0, 0.004, 0.012))); nrm.append(Vector((0, -0.8, 0.6)).normalized())
    n = len(pts)
    rows = []
    for i in range(n):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        s = t.cross(nrm[i]).normalized()
        w = 0.030 * (0.75 + 0.25 * i / (n - 1)) * (0.8 if i == 0 else 1)
        row = []
        for j, sj in enumerate((-1, 0, 1)):
            p = pts[i] + nrm[i] * (0.0034 + (0.0012 if sj == 0 else 0)) + s * (w * 0.5 * sj)
            v = bm.verts.new(p); v[pu] = j / 2; v[pv] = i / (n - 1)
            row.append(v)
        rows.append(row)
    faces = []
    for i in range(n - 1):
        for j in range(2):
            faces.append(bm.faces.new((rows[i][j], rows[i][j + 1], rows[i + 1][j + 1], rows[i + 1][j])))
    bm.normal_update()
    if sum(f.normal.z for f in faces) < 0:
        bmesh.ops.reverse_faces(bm, faces=faces)
    uvl = bm.loops.layers.uv.verify()
    u0, v0, u1, v1 = rect
    # tongue uses leather rect: sub rect near top-left of the shoe tile
    for f in faces:
        for l in f.loops:
            l[uvl].uv = (u0 + (u1 - u0) * (0.62 + 0.36 * l.vert[pu]), v0 + (v1 - v0) * (0.05 + 0.4 * l.vert[pv]))
    # laces: 3 crossed pairs
    lf = []
    for k, idx in enumerate((2, 3, 4)):
        if idx >= len(pts) - 1: continue
        p = pts[idx]; nn = nrm[idx]; t = (pts[idx + 1] - pts[idx - 1]).normalized(); s = t.cross(nn).normalized()
        for sgn in (1, -1):
            a = p - s * 0.014 + nn * 0.0058 - t * (0.004 * sgn)
            b = p + s * 0.014 + nn * 0.0058 + t * (0.004 * sgn)
            m = (a + b) * 0.5 + nn * 0.0016
            vs = []
            for q in (a, m, b):
                d = (b - a).normalized(); sd = d.cross(nn).normalized()
                vs.append((bm.verts.new(q + sd * 0.0021), bm.verts.new(q - sd * 0.0021)))
            for i2 in range(2):
                f = bm.faces.new((vs[i2][0], vs[i2][1], vs[i2 + 1][1], vs[i2 + 1][0]))
                lf.append(f)
    bm.normal_update()
    for f in lf:
        if f.normal.z < 0: f.normal_flip()
        for l in f.loops: l[uvl].uv = thread_uv
    return bm
