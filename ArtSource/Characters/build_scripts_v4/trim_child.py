import bpy, bmesh, math, random
from mathutils import Vector, Matrix, noise
from mathutils.bvhtree import BVHTree
import cloth

def obj_bvh(o):
    dg = bpy.context.evaluated_depsgraph_get()
    return BVHTree.FromObject(o, dg)

def uv_rect(bm, faces, rect, mode, frame=None):
    """assign planar UVs to faces. rect=(u0,v0,u1,v1). mode 'param' uses per-vertex custom (pu,pv) in [0,1]"""
    uvl = bm.loops.layers.uv.verify()
    pu = bm.verts.layers.float.get('pu'); pv = bm.verts.layers.float.get('pv')
    u0, v0, u1, v1 = rect
    for f in faces:
        for l in f.loops:
            l[uvl].uv = (u0 + (u1 - u0) * min(max(l.vert[pu], 0), 1), v0 + (v1 - v0) * min(max(l.vert[pv], 0), 1))

# ---------------- straps -----------------
def strap_path(bvh, x, y_c=0.0, z_c=0.80, phi0=-62, phi1=242, n=30):
    pts = []; nrm = []
    C = Vector((x, y_c, z_c))
    for i in range(n + 1):
        phi = math.radians(phi0 + (phi1 - phi0) * i / n)
        d = Vector((0, -math.cos(phi), math.sin(phi)))
        hit = bvh.ray_cast(C + d * 0.01, d, 0.4)
        if hit[0] is None:
            continue
        pts.append(hit[0]); nrm.append(hit[1].normalized())
    return pts, nrm

def build_strap(bm, pts, nrm, width, lift, rect, lm):
    """ribbon on surface, returns list of faces; adds pu,pv layers"""
    pu = bm.verts.layers.float.get('pu') or bm.verts.layers.float.new('pu')
    pv = bm.verts.layers.float.get('pv') or bm.verts.layers.float.new('pv')
    n = len(pts)
    rows = []
    total = sum((pts[i + 1] - pts[i]).length for i in range(n - 1))
    acc = 0
    for i in range(n):
        t = (pts[i + 1] - pts[i - 1 if i > 0 else 0]) if i < n - 1 else (pts[i] - pts[i - 1])
        if i == 0: t = pts[1] - pts[0]
        t = t.normalized()
        nn = nrm[i]
        s = t.cross(nn).normalized()
        if i > 0: acc += (pts[i] - pts[i - 1]).length
        row = []
        for j, sj in enumerate((-1, 0, 1)):
            p = pts[i] + nn * (lift + (0.0010 if sj == 0 else 0.0)) + s * (width * 0.5 * sj)
            v = bm.verts.new(p); v[pu] = j / 2.0; v[pv] = (acc / total)
            row.append(v)
        rows.append(row)
    faces = []
    for i in range(n - 1):
        for j in range(2):
            f = bm.faces.new((rows[i][j], rows[i][j + 1], rows[i + 1][j + 1], rows[i + 1][j]))
            faces.append(f)
    # ensure outward normals
    for f in faces:
        # compare to surface normal
        c = f.calc_center_median()
    return faces, rows

def add_box(bm, center, ax, ay, az, size, faces_out):
    """box with local axes ax (length), ay (width), az (height), size=(lx,ly,lz)"""
    lx, ly, lz = size
    vs = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                vs.append(bm.verts.new(center + ax * (lx * 0.5 * sx) + ay * (ly * 0.5 * sy) + az * (lz * 0.5 * sz)))
    # index: sx*4+sy*2+sz ordering: idx = ((sx>0)*4 + (sy>0)*2 + (sz>0))
    def V(sx, sy, sz): return vs[(sx > 0) * 4 + (sy > 0) * 2 + (sz > 0)]
    quads = [((-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)),
             ((-1, -1, -1), (-1, 1, -1), (1, 1, -1), (1, -1, -1)),
             ((1, -1, -1), (1, 1, -1), (1, 1, 1), (1, -1, 1)),
             ((-1, -1, -1), (-1, -1, 1), (-1, 1, 1), (-1, 1, -1)),
             ((-1, 1, -1), (-1, 1, 1), (1, 1, 1), (1, 1, -1)),
             ((-1, -1, -1), (1, -1, -1), (1, -1, 1), (-1, -1, 1))]
    for q in quads:
        try:
            f = bm.faces.new([V(*c) for c in q]); faces_out.append(f)
        except ValueError:
            pass
    return vs

def make_buckle(bm, p, t, s, n, faces_out):
    """rectangular slide buckle frame on strap at p with tangent t, side s, normal n"""
    ho = 0.0034  # height above strap surface center
    L = 0.030; W = 0.026; b = 0.0042; h = 0.0048
    base = p + n * ho
    add_box(bm, base + t * (L * 0.5 - b * 0.5), t, s, n, (b, W, h), faces_out)
    add_box(bm, base - t * (L * 0.5 - b * 0.5), t, s, n, (b, W, h), faces_out)
    add_box(bm, base + s * (W * 0.5 - b * 0.5), t, s, n, (L - 2 * b, b, h), faces_out)
    add_box(bm, base - s * (W * 0.5 - b * 0.5), t, s, n, (L - 2 * b, b, h), faces_out)
    # center prong bar
    add_box(bm, base, t, s, n, (b * 0.8, W - 2 * b, h * 0.8), faces_out)

# ---------------- patches -----------------
def build_patch(bm, bvh, center_dir_origin, ray_dir, size, rot, seed, rect, lift=0.0022, thick=0.0026, grid=5, stitch=True, stitch_faces=None, stitch_rect=None, top_faces=None, jitter=0.12):
    """center_dir_origin: point from which a ray along ray_dir hits the cloth at patch center."""
    rng = random.Random(seed)
    hit = bvh.ray_cast(center_dir_origin, ray_dir, 1.0)
    c, n = hit[0], hit[1].normalized()
    up = Vector((0, 0, 1))
    T = n.cross(up)
    if T.length < 1e-4: T = Vector((1, 0, 0))
    T.normalize(); B = n.cross(T).normalized()   # B ~ up on surface
    cr, sr = math.cos(rot), math.sin(rot)
    T2 = T * cr + B * sr; B2 = B * cr - T * sr
    pu = bm.verts.layers.float.get('pu') or bm.verts.layers.float.new('pu')
    pv = bm.verts.layers.float.get('pv') or bm.verts.layers.float.new('pv')
    N = grid
    sx, sy = size
    gv = {}
    ph = rng.uniform(0, 6)
    wob = [rng.uniform(-1, 1) for _ in range(6)]
    for i in range(N + 1):
        for j in range(N + 1):
            a = i / N * 2 - 1; b = j / N * 2 - 1
            edge = max(abs(a), abs(b))
            ang = math.atan2(b, a)
            rscale = 1.0 + jitter * (wob[0] * math.sin(2 * ang + ph) + wob[1] * math.sin(3 * ang + 1.7 * ph) + 0.6 * wob[2] * math.sin(5 * ang))
            corner = 1.0 - 0.20 * abs(a) * abs(b)
            if edge > 0.99: sc = rscale * corner
            else: sc = 1.0 + (rscale * corner - 1.0) * edge
            aa = a * sc; bb = b * sc
            aa += 0.05 * b * (wob[3])   # slight shear
            p0 = c + T2 * (aa * sx * 0.5) + B2 * (bb * sy * 0.5) + n * 0.03
            loc, nn, fi, d = bvh.find_nearest(p0)
            p = loc + nn * lift
            v = bm.verts.new(p)
            v[pu] = (a + 1) / 2; v[pv] = (b + 1) / 2
            gv[(i, j)] = (v, nn, aa, bb)
    faces = []
    for i in range(N):
        for j in range(N):
            f = bm.faces.new((gv[(i, j)][0], gv[(i + 1, j)][0], gv[(i + 1, j + 1)][0], gv[(i, j + 1)][0]))
            faces.append(f)
    # make orientation outward (compare with n)
    nsum = sum((f.normal for f in faces), Vector())
    bm.normal_update()
    if sum((f.normal.dot(n) for f in faces)) < 0:
        bmesh.ops.reverse_faces(bm, faces=faces)
    # thickness: add skirt (extruded underside rim) -> simple: duplicate boundary verts toward the cloth
    bnd = []
    # ordered perimeter indices
    per = [(i, 0) for i in range(N)] + [(N, j) for j in range(N)] + [(i, N) for i in range(N, 0, -1)] + [(0, j) for j in range(N, 0, -1)]
    under = []
    for (i, j) in per:
        v, nn, aa, bb = gv[(i, j)]
        u = bm.verts.new(v.co - nn * thick); u[pu] = v[pu]; u[pv] = v[pv]
        under.append(u)
    side = []
    for k in range(len(per)):
        a_, b_ = gv[per[k]][0], gv[per[(k + 1) % len(per)]][0]
        ua, ub = under[k], under[(k + 1) % len(per)]
        try:
            f = bm.faces.new((a_, ua, ub, b_))
            side.append(f)
        except ValueError:
            pass
    bm.normal_update()
    # fix side normals outward (away from patch center)
    for f in side:
        if (f.calc_center_median() - c).dot(f.normal) < 0:
            f.normal_flip()
    allf = faces + side
    # stitches
    st = []
    if stitch:
        per_pts = []
        for (i, j) in per:
            v, nn, aa, bb = gv[(i, j)]
            q = c + (v.co - c) * 0.0  # unused
            # inset position in patch plane
            a0 = aa * sx * 0.5; b0 = bb * sy * 0.5
            L = math.hypot(a0, b0)
            k = max(0.0, (L - 0.0065) / max(L, 1e-6))
            p0 = c + T2 * (a0 * k) + B2 * (b0 * k) + n * 0.03
            loc, nn2, fi2, d2 = bvh.find_nearest(p0)
            per_pts.append((loc + nn2 * (lift + 0.0004 + 0.0000), nn2))
        # resample along perimeter polyline with spacing
        poly = [p for p, _ in per_pts]
        nrms = [q for _, q in per_pts]
        M = len(poly)
        seglen = [(poly[(k + 1) % M] - poly[k]).length for k in range(M)]
        total = sum(seglen)
        spacing = 0.0125
        cnt = max(8, int(total / spacing))
        for s_i in range(cnt):
            target = total * s_i / cnt
            acc = 0
            for k in range(M):
                if acc + seglen[k] >= target:
                    tt = (target - acc) / max(seglen[k], 1e-9)
                    p = poly[k].lerp(poly[(k + 1) % M], tt)
                    nrm = nrms[k].lerp(nrms[(k + 1) % M], tt).normalized()
                    tg = (poly[(k + 1) % M] - poly[k]).normalized()
                    break
                acc += seglen[k]
            sd = tg.cross(nrm).normalized()
            hl, hw = 0.0036, 0.00085
            vs = []
            for (sa, sb) in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                vv = bm.verts.new(p + tg * (hl * sa) + sd * (hw * sb) + nrm * 0.0003)
                vv[pu] = 0.5; vv[pv] = 0.5
                vs.append(vv)
            try:
                f = bm.faces.new(vs); st.append(f)
            except ValueError:
                pass
        bm.normal_update()
        for f in st:
            if f.normal.dot(n) < 0: f.normal_flip()
    return faces, side, st, c, n
