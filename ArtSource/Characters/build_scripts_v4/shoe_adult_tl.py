import bpy, bmesh, math
from mathutils import Vector, Matrix
import cloth, trim_child

def build_tongue_laces(shoe_obj, sg, rect, thread_uv):
    bvh = trim_child.obj_bvh(shoe_obj)
    bm = bmesh.new()
    pu = bm.verts.layers.float.new('pu'); pv = bm.verts.layers.float.new('pv')
    cx = 0.189 * sg
    # tongue path along instep top
    ys = [-0.115 + i * 0.0105 for i in range(10)]
    pts = []; nrm = []
    for y in ys:
        h = bvh.ray_cast(Vector((cx, y, 0.2)), Vector((0, 0, -1)), 0.5)
        if h[0] is None: continue
        pts.append(h[0]); nrm.append(h[1].normalized())
    # extend upward past collar: last point lifted and tilted back
    last = pts[-1]
    pts.append(last + Vector((0, 0.005, 0.016))); nrm.append(Vector((0, -0.8, 0.6)).normalized())
    n = len(pts)
    rows = []
    for i in range(n):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        s = t.cross(nrm[i]).normalized()
        w = 0.038 * (0.75 + 0.25 * i / (n - 1)) * (0.8 if i == 0 else 1)
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
    for k, idx in enumerate((2, 3, 4, 5, 6)):
        if idx >= len(pts) - 1: continue
        p = pts[idx]; nn = nrm[idx]; t = (pts[idx + 1] - pts[idx - 1]).normalized(); s = t.cross(nn).normalized()
        for sgn in (1, -1):
            a = p - s * 0.017 + nn * 0.0058 - t * (0.005 * sgn)
            b = p + s * 0.017 + nn * 0.0058 + t * (0.005 * sgn)
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
