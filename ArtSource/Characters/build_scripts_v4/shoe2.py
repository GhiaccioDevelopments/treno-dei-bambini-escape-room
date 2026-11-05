import bpy, bmesh, math
from mathutils import Vector, Matrix, noise
import cloth, trim_child

def interp(tab, t):
    for i in range(len(tab) - 1):
        a, b = tab[i], tab[i + 1]
        if a[0] <= t <= b[0]:
            u = (t - a[0]) / (b[0] - a[0]); u = (1 - math.cos(u * math.pi)) / 2
            return a[1] * (1 - u) + b[1] * u
    return tab[-1][1]

W = [(0, 0.024), (0.06, 0.033), (0.2, 0.0375), (0.45, 0.0415), (0.62, 0.0445), (0.80, 0.0415), (0.92, 0.032), (0.985, 0.016), (1.0, 0.004)]
H = [(0, 0.062), (0.06, 0.082), (0.2, 0.090), (0.33, 0.074), (0.5, 0.052), (0.65, 0.042), (0.82, 0.035), (0.93, 0.027), (1.0, 0.012)]

def build_shoe_loft(sg=1, nst=17, nring=24, cx=0.1385, seed=2, worn=1.0, toe_hole=False, side_tear=False):
    cx = cx * sg
    bm = bmesh.new()
    rings = []
    for i in range(nst):
        t = i / (nst - 1)
        y = 0.040 - 0.188 * t - 0.006 * math.sin(t * math.pi) * 0  # straight
        w = interp(W, t); h = interp(H, t)
        h *= 1 + 0.04 * worn * noise.noise(Vector((t * 5 + seed, 0.3, 0)))
        p = 2.2 + 0.5 * t
        zb = -0.001
        ring = []
        for k in range(nring):
            ph = 2 * math.pi * k / nring
            c, s = math.cos(ph), math.sin(ph)
            x = cx + sg * w * math.copysign(abs(c) ** (2 / p), c)
            if s >= 0:
                z = zb + h * (abs(s) ** (2 / p))
            else:
                z = zb + 0.002 * s
            # tilt toe cap up slightly
            ring.append(bm.verts.new((x, y, z)))
        rings.append(ring)
    faces = []
    for i in range(nst - 1):
        for k in range(nring):
            a, b = rings[i][k], rings[i][(k + 1) % nring]
            c_, d = rings[i + 1][(k + 1) % nring], rings[i + 1][k]
            faces.append(bm.faces.new((a, b, c_, d)))
    # toe cap
    last = rings[-1]
    faces.append(bm.faces.new(last[::-1]))
    # heel cap
    faces.append(bm.faces.new(rings[0]))
    bm.normal_update()
    # outward normals
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    cloth.cut(bm, (0, 0, 0.071), (0, 0, 1), 'outer')
    del_f = []
    # tears
    tear = []
    if toe_hole:
        tc = Vector((cx, -0.132, 0.026))
        tear = [f for f in bm.faces if (f.calc_center_median() - tc).length < 0.020 and f.normal.z > 0.15]
    if side_tear:
        tc = Vector((cx + sg * 0.0415, -0.085, 0.022))
        tear = [f for f in bm.faces if (f.calc_center_median() - tc).length < 0.016 and f.normal.x * sg > 0.3]
    bmesh.ops.delete(bm, geom=list(set(del_f + tear)), context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.normal_update()
    # sole slab (separate shell): outline from the bottom stations
    outline = []
    # bottom outline = vertices with z minimal per ring side: take ring indices around lower half boundary (sin<0 flat) -> use outer-most x points (k=0 and k=nring//2) plus bottom
    # build a simple closed outline polygon in xy from ring extremes
    right = [rings[i][0] for i in range(nst)]
    left = [rings[i][nring // 2] for i in range(nst)]
    poly = [Vector((v.co.x, v.co.y)) for v in right] + [Vector((v.co.x, v.co.y)) for v in reversed(left)]
    # expand polygon by welt
    cen = Vector((cx, -0.055))
    def exp(p, d):
        v = p - cen
        # approx normal expansion: scale along each axis
        return Vector((cen.x + v.x + math.copysign(d, v.x) * 1.0, cen.y + v.y * 1.0 + math.copysign(d, v.y) * (0.0 if abs(v.y) < 0.1 else 1.0)))
    sole_top = 0.0
    sole_bot = -0.0075
    ring_top = []; ring_out = []; ring_bot = []
    for i, p in enumerate(poly):
        v = p - cen
        # outward direction approx: normalized by ellipse
        d = Vector((v.x / 0.045, v.y / 0.095))
        d = d.normalized() if d.length > 1e-6 else Vector((1, 0))
        # heel is higher
        heel = 1.0 if p.y > -0.01 else 0.0
        top = Vector((p.x, p.y, 0.001))
        out = Vector((p.x + d.x * 0.003, p.y + d.y * 0.0035, -0.002))
        bot = Vector((p.x + d.x * 0.0022, p.y + d.y * 0.003, sole_bot - 0.006 * heel * min(1.0, (p.y + 0.01) / 0.03)))
        ring_top.append(bm.verts.new(top)); ring_out.append(bm.verts.new(out)); ring_bot.append(bm.verts.new(bot))
    n = len(poly)
    sole_faces = []
    for i in range(n):
        j = (i + 1) % n
        sole_faces.append(bm.faces.new((ring_top[i], ring_out[i], ring_out[j], ring_top[j])))
        sole_faces.append(bm.faces.new((ring_out[i], ring_bot[i], ring_bot[j], ring_out[j])))
    sole_faces.append(bm.faces.new(ring_bot[::-1]))
    bm.normal_update()
    # orient sole outward: bottom face should point -z
    bmesh.ops.recalc_face_normals(bm, faces=sole_faces)
    bm.normal_update()
    if sole_faces[-1].normal.z > 0:
        bmesh.ops.reverse_faces(bm, faces=sole_faces)
    return bm, sole_faces
