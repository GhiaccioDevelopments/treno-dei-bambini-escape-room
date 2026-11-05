import bpy, bmesh, math
from mathutils import Vector, Matrix
import cloth, trim_child
from trim_child import obj_bvh, add_box

def surf_hit(bvh, p, d, dist=1.0):
    h = bvh.ray_cast(p, d, dist)
    return h

def ribbon(bm, rows, pu, pv, flip=False):
    faces = []
    for i in range(len(rows) - 1):
        for j in range(len(rows[0]) - 1):
            f = bm.faces.new((rows[i][j], rows[i][j + 1], rows[i + 1][j + 1], rows[i + 1][j]))
            faces.append(f)
    return faces

def build_coat_details(coat_obj, vlines, rect_coat, button_uv, nbutt=3):
    bvh = obj_bvh(coat_obj)
    bm = bmesh.new()
    pu = bm.verts.layers.float.new('pu'); pv = bm.verts.layers.float.new('pv')
    uvl = bm.loops.layers.uv.verify()
    faces_coat = []; faces_btn = []
    u0, v0, u1, v1 = rect_coat
    # ---- lapels
    for si, sg in enumerate((1, -1)):
        A, nn = vlines[0] if sg > 0 else vlines[1]
        T = Vector((sg * 0.080, 0, 1.52))
        d = (T - A)
        outd = (-nn).normalized()
        rows = []
        N = 9
        for i in range(N + 1):
            s = i / N
            p = A + d * s
            width = 0.040 + 0.012 * math.sin(min(1.0, s * 1.25) * math.pi * 0.5)
            row = []
            for k, (w, lift) in enumerate(((0.0, 0.0105), (width * 0.55, 0.0075), (width, 0.0035))):
                h = None
                for fac in (1.0, 0.8, 0.6, 0.4, 0.2, 0.0):
                    q = p + outd * (w * fac)
                    # project to coat surface from front
                    h = bvh.ray_cast(Vector((q.x, -0.6, q.z)), Vector((0, 1, 0)), 1.2)
                    if h[0] is not None: break
                if h[0] is None:
                    h = bvh.ray_cast(Vector((p.x * 0.5, -0.6, p.z - 0.02)), Vector((0, 1, 0)), 1.2)
                loc, nrm = h[0], h[1].normalized()
                v = bm.verts.new(loc + nrm * lift + (Vector((0, 0, 0))))
                v[pu] = (k) / 2.0; v[pv] = s
                row.append(v)
            rows.append(row)
        fs = ribbon(bm, rows, pu, pv)
        bm.normal_update()
        if sum(f.normal.y for f in fs) > 0:
            bmesh.ops.reverse_faces(bm, faces=fs)
        faces_coat += fs
    # ---- collar (back/sides of neck)
    nb = [v for v in coat_obj.data.vertices if v.co.z > 1.5 and abs(v.co.x) < 0.14 and v.co.y > -0.045]
    # restrict to boundary: highest verts around neck
    nbs = sorted(nb, key=lambda v: -v.co.z)
    # take verts within 0.012 of the neck cut: pick by ring: z close to plane
    import math as m
    cands = []
    for v in coat_obj.data.vertices:
        p = v.co
        if p.z > 1.50 and abs(p.x) < 0.12 and p.y > -0.05:
            zplane = 1.53 + 0.55 * (p.y + 0.08)
            if abs(p.z - zplane) < 0.006: cands.append(p)
    cands.sort(key=lambda p: m.atan2(p.x, p.y + 0.02))
    # thin: keep every other to reduce
    pts = cands[::2]
    rows = []
    for i, p in enumerate(pts):
        out = Vector((p.x, p.y + 0.02, 0)); out = out.normalized() if out.length > 1e-6 else Vector((0, 1, 0))
        ang = m.atan2(abs(p.x), p.y + 0.02)
        h = 0.020 - 0.004 * (ang / math.pi)
        a = bm.verts.new(p + Vector((0, 0, 0.002)) + out * 0.002)
        b = bm.verts.new(p + Vector((0, 0, 0.0)) + Vector((0, 0, h * 0.5)) + out * 0.005)
        c = bm.verts.new(p + Vector((0, 0, h)) + out * 0.014)
        for k, v in enumerate((a, b, c)):
            v[pu] = k / 2.0; v[pv] = i / max(1, len(pts) - 1)
        rows.append((a, b, c))
    if len(rows) > 2:
        fs = ribbon(bm, rows, pu, pv)
        bm.normal_update()
        # outward = away from neck center
        for f in fs:
            c = f.calc_center_median()
            if f.normal.dot(Vector((c.x, c.y + 0.02, 0))) < 0: f.normal_flip()
        faces_coat += fs
    # ---- buttons
    for i in range(nbutt):
        z = 1.145 - i * 0.135
        h = bvh.ray_cast(Vector((0.030, -0.6, z)), Vector((0, 1, 0)), 1.2)
        if h[0] is None: continue
        loc, nrm = h[0], h[1].normalized()
        res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=10, radius1=0.0115, radius2=0.0105, depth=0.006, calc_uvs=False,
                                    matrix=Matrix.Translation(loc + nrm * 0.0035))
        rot = nrm.rotation_difference(Vector((0, 0, 1))).to_matrix().inverted()
        bmesh.ops.rotate(bm, verts=res['verts'], cent=loc + nrm * 0.0035, matrix=rot)
        cf = list({f for v in res['verts'] for f in v.link_faces})
        bmesh.ops.recalc_face_normals(bm, faces=cf)
        for v in res['verts']: v[pu] = 0.5; v[pv] = 0.5
        faces_btn += cf
    # ---- pocket flaps
    for sg in (1, -1):
        for (x, z, rot) in ((0.135, 0.86, 0.22),):
            h = bvh.ray_cast(Vector((sg * x, -0.7, z)), Vector((0, 1, 0)), 1.4)
            if h[0] is None: continue
            c, n = h[0], h[1].normalized()
            T = n.cross(Vector((0, 0, 1))).normalized(); B = n.cross(T).normalized()
            cr, sr = math.cos(rot * sg), math.sin(rot * sg)
            T2 = T * cr + B * sr; B2 = B * cr - T * sr
            gv = []
            NN = 5
            for i in range(NN + 1):
                rowv = []
                for j in range(3):
                    a = (i / NN - 0.5) * 0.15; b = (j / 2 - 0.5) * 0.050
                    p0 = c + T2 * a + B2 * b + n * 0.05
                    loc, nn2, fi, dd = bvh.find_nearest(p0)
                    lift = 0.006 + (0.003 if j == 0 else 0.0)  # flap hangs: top edge higher
                    v = bm.verts.new(loc + nn2 * lift)
                    v[pu] = i / NN; v[pv] = j / 2
                    rowv.append(v)
                gv.append(rowv)
            fs = ribbon(bm, gv, pu, pv)
            bm.normal_update()
            if sum(f.normal.dot(n) for f in fs) < 0:
                bmesh.ops.reverse_faces(bm, faces=fs)
            faces_coat += fs
    # UV assign
    for f in faces_coat:
        for l in f.loops:
            l[uvl].uv = (u0 + (u1 - u0) * (0.25 + 0.5 * min(max(l.vert[pu], 0), 1)), v0 + (v1 - v0) * (0.25 + 0.5 * min(max(l.vert[pv], 0), 1)))
    for f in faces_btn:
        for l in f.loops: l[uvl].uv = button_uv
    return bm
