import os
ROOT = os.environ.get("TRENO_ROOT", r".")  # cartella del progetto Il Treno dei Bambini
SCRATCH = os.environ.get("TRENO_SCRATCH", os.path.join(ROOT, "ArtSource", "tmp"))
import bpy, numpy as np, math
from texlib import *

N = 1024

def raster(obj, N=N):
    me = obj.data
    me.calc_loop_triangles()
    uvd = me.uv_layers.active.data
    nv = len(me.vertices)
    co = np.zeros(nv * 3, np.float32); me.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
    vn = np.zeros(nv * 3, np.float32); me.vertices.foreach_get('normal', vn); vn = vn.reshape(-1, 3)
    nl = len(me.loops)
    uv = np.zeros(nl * 2, np.float32); uvd.foreach_get('uv', uv); uv = uv.reshape(-1, 2)
    P = np.zeros((N, N, 3), np.float32); Nn = np.zeros((N, N, 3), np.float32); M = np.zeros((N, N), bool)
    for tri in me.loop_triangles:
        l = tri.loops; v = tri.vertices
        t = uv[list(l)] * N
        x0 = int(max(0, math.floor(t[:, 0].min() - 1))); x1 = int(min(N - 1, math.ceil(t[:, 0].max() + 1)))
        y0 = int(max(0, math.floor(t[:, 1].min() - 1))); y1 = int(min(N - 1, math.ceil(t[:, 1].max() + 1)))
        if x1 < x0 or y1 < y0: continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        a, b, c = t
        den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(den) < 1e-12: continue
        w0 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / den
        w1 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / den
        w2 = 1 - w0 - w1
        eps = -0.02
        m = (w0 >= eps) & (w1 >= eps) & (w2 >= eps)
        if not m.any(): continue
        pp = w0[..., None] * co[v[0]] + w1[..., None] * co[v[1]] + w2[..., None] * co[v[2]]
        nn = w0[..., None] * vn[v[0]] + w1[..., None] * vn[v[1]] + w2[..., None] * vn[v[2]]
        sub = (slice(y0, y1 + 1), slice(x0, x1 + 1))
        P[sub][m] = pp[m]; Nn[sub][m] = nn[m]; M[sub] |= m
    return P, Nn, M

def n3(pos, f, seed, octs=4):
    rng = np.random.default_rng(seed)
    acc = np.zeros(pos.shape[:2], np.float32)
    for i in range(octs * 3):
        d = rng.normal(size=3); d /= np.linalg.norm(d); ph = rng.uniform(0, 6.28)
        acc += np.sin((pos @ d) * f * (1 + 0.43 * (i // 3)) * (1 + 0.15 * (i % 3)) + ph) / (1 + 0.5 * (i // 3))
    return acc / (octs * 3) ** 0.5 * 0.9

def vor(h, w, npts, seed):
    rng = np.random.default_rng(seed)
    pts = rng.uniform(0, 1, (npts, 2)) * [w, h]
    ys, xs = np.mgrid[0:h, 0:w]
    best = np.full((h, w), 1e9); second = np.full((h, w), 1e9)
    for p in pts:
        d = np.sqrt((xs - p[0]) ** 2 + (ys - p[1]) ** 2)
        upd = d < best
        second = np.where(upd, best, np.minimum(second, d))
        best = np.where(upd, d, best)
    return second - best

def weave(X, Y, period, seed, slub=0.25, twill=False):
    rng = np.random.default_rng(seed)
    h = np.zeros(X.shape, np.float32)
    fx = (X % period) / period; fy = (Y % period) / period
    if twill:
        s = np.sin(2 * np.pi * (X + Y) / period) * 0.5 + 0.5
        h = s
    else:
        ci = (X // period).astype(int); cj = (Y // period).astype(int)
        over = ((ci + cj) % 2 == 0)
        a = np.sin(np.pi * fy) ** 0.7
        b = np.sin(np.pi * fx) ** 0.7
        h = np.where(over, a, b)
    # slubs: per-row / per-column brightness variation
    nx = noise(X.shape[0], X.shape[1], 3.0, seed + 1)
    ny = noise(X.shape[0], X.shape[1], 3.0, seed + 2)
    col = 1 + slub * ((nx - 0.5) * 0.7 * 0 + (np.where(((X // period).astype(int) + (Y // period).astype(int)) % 2 == 0, ny, nx) - 0.5))
    return h.astype(np.float32), col.astype(np.float32)

def lerpc(a, b, t):
    return a + (b - a) * t[..., None]

def sst(a, b, x):
    return smoothstep(a, b, x)

def paint(P, Nn, M, seed=3):
    Hh = Ww = N
    Y, X = np.mgrid[0:Hh, 0:Ww].astype(np.float32)
    alb = np.zeros((Hh, Ww, 3), np.float32); rough = np.full((Hh, Ww), 0.85, np.float32); hgt = np.zeros((Hh, Ww), np.float32)
    x, y, z = P[..., 0], P[..., 1], P[..., 2]
    ax = np.abs(x)
    wv, wc = weave(X, Y, 5, seed)
    wvt, wct = weave(X, Y, 6, seed + 9, twill=True)
    def tile(x0, x1, y0, y1):
        m = np.zeros((Hh, Ww), bool); m[y0:y1, x0:x1] = True; return m
    # ---------------- shirt ----------------
    T = tile(0, 512, 512, 1024)
    base = np.array([172, 156, 124], np.float32)
    nz1 = n3(P, 60, seed); nz2 = n3(P, 18, seed + 1); nz3 = n3(P, 9, seed + 2)
    c = base[None, None, :] * (1 + 0.07 * nz1[..., None] + 0.08 * nz2[..., None]) * (0.96 + 0.04 * wc[..., None])
    stain = sst(0.45, 1.1, nz3)
    c = lerpc(c, c * np.array([0.78, 0.70, 0.55]), stain * 0.55)
    hem = sst(0.69, 0.585, z) * (0.35 + 0.5 * sst(-0.3, 0.9, nz2))
    c = lerpc(c, np.array([92, 78, 58], np.float32)[None, None, :] * (0.8 + 0.2 * nz1[..., None]), np.clip(hem, 0, 0.85))
    cuff = sst(0.16, 0.215, ax) * 0.55
    c = lerpc(c, np.array([104, 88, 66], np.float32)[None, None, :], cuff)
    uarm = np.exp(-(((ax - 0.125) / 0.03) ** 2 + ((z - 0.835) / 0.05) ** 2)) * 0.5
    c = lerpc(c, np.array([150, 124, 78], np.float32)[None, None, :], uarm)
    neck = sst(0.945, 0.985, z) * (ax < 0.1) * 0.5
    c = lerpc(c, np.array([128, 108, 80], np.float32)[None, None, :], neck)
    # fray: threads near hem (dark/light streaks)
    alb = np.where(T[..., None], c, alb)
    rough = np.where(T, 0.88 + 0.08 * nz1, rough)
    hgt = np.where(T, wv * 0.14 + 0.12 * nz1 + 0.05 * n3(P, 140, seed + 7, 1), hgt)
    # ---------------- shorts (wool twill) ----------------
    T = tile(512, 1024, 512, 1024)
    base = np.array([96, 88, 72], np.float32)
    fz = n3(P, 120, seed + 5, 2)
    c = base[None, None, :] * (1 + 0.10 * nz1[..., None] + 0.10 * nz2[..., None] + 0.05 * fz[..., None]) * (0.95 + 0.05 * wct[..., None] * wvt[..., None])
    c = lerpc(c, c * np.array([0.72, 0.68, 0.58]), sst(0.4, 1.1, nz3) * 0.5)
    hemd = sst(0.52, 0.378, z) * (0.35 + 0.4 * sst(-0.3, 0.9, nz2))
    c = lerpc(c, np.array([66, 56, 44], np.float32)[None, None, :], np.clip(hemd, 0, 0.8))
    seat = np.exp(-(((ax - 0.09) / 0.07) ** 2 + ((z - 0.56) / 0.05) ** 2)) * (y > 0.02) * 0.35
    c = lerpc(c, np.array([72, 64, 50], np.float32)[None, None, :], seat)
    alb = np.where(T[..., None], c, alb)
    rough = np.where(T, 0.93 + 0.05 * fz, rough)
    hgt = np.where(T, wvt * 0.16 + 0.2 * fz, hgt)
    # ---------------- patches ----------------
    patch_cols = {1: (140, 122, 94), 2: (98, 70, 46), 3: (90, 88, 82), 4: (124, 102, 76)}
    tiles = {1: (512, 768, 256, 512), 2: (768, 1024, 256, 512), 3: (512, 768, 0, 256), 4: (768, 1024, 0, 256)}
    for k, (x0, x1, y0, y1) in tiles.items():
        T = tile(x0, x1, y0, y1)
        base = np.array(patch_cols[k], np.float32)
        nzp = n3(np.stack([X / 90.0, Y / 90.0, X * 0 + k], -1), 3.0, seed + 20 + k)
        if k in (1, 3):
            w_, c_ = weave(X, Y, 4 if k == 1 else 5, seed + 30 + k, twill=(k == 1))
        else:
            w_, c_ = weave(X, Y, 6, seed + 30 + k)
        c = base[None, None, :] * (1 + 0.08 * nzp[..., None]) * (0.88 + 0.12 * c_[..., None]) * (0.92 + 0.08 * w_[..., None])
        c = lerpc(c, c * np.array([0.75, 0.7, 0.62]), sst(0.2, 1.0, nzp) * 0.45)
        alb = np.where(T[..., None], c, alb)
        rough = np.where(T, 0.9, rough); hgt = np.where(T, w_ * 0.5 + 0.12 * nzp, hgt)
    # ---------------- leather (strap) ----------------
    T = tile(0, 256, 0, 256)
    gr = noise(Hh, Ww, 2.0, seed + 40, 2)
    c = np.array([86, 54, 34], np.float32)[None, None, :] * (0.8 + 0.4 * gr[..., None])
    v = vor(256, 256, 70, seed + 41); crack = np.zeros((Hh, Ww), np.float32); crack[0:256, 0:256] = np.clip(1 - v / 3.5, 0, 1)
    c = c * (1 - 0.18 * crack[..., None])
    # stitch dashes along strap edges (u px ~ 24 and 98)
    dash = (((Y // 9).astype(int) % 2) == 0)
    st_line = (((X > 25) & (X < 28)) | ((X > 94) & (X < 97))) & dash
    c = np.where(st_line[..., None], np.array([128, 104, 72], np.float32), c)
    alb = np.where(T[..., None], c, alb)
    rough = np.where(T, 0.55 + 0.2 * gr, rough); hgt = np.where(T, -0.3 * crack + 0.25 * gr + 0.3 * st_line, hgt)
    # ---------------- thread / metal ----------------
    T = tile(256, 384, 0, 256)
    alb = np.where(T[..., None], np.array([88, 74, 56], np.float32)[None, None, :] * (0.85 + 0.3 * gr[..., None]), alb)
    rough = np.where(T, 0.9, rough)
    T = tile(384, 512, 0, 256)
    mt = noise(Hh, Ww, 6.0, seed + 44, 2)
    alb = np.where(T[..., None], np.array([104, 100, 94], np.float32)[None, None, :] * (0.7 + 0.6 * mt[..., None]), alb)
    rough = np.where(T, 0.42 + 0.25 * mt, rough); hgt = np.where(T, 0.1 * mt, hgt)
    # ---------------- shoes ----------------
    for (x0, x1) in ((0, 256), (256, 512)):
        T = tile(x0, x1, 256, 512)
        nzs = n3(P, 70, seed + 50); nzs2 = n3(P, 22, seed + 51)
        v = vor(256, 256, 55, seed + 52 + x0); crk = np.zeros((Hh, Ww), np.float32); crk[256:512, x0:x1] = np.clip(1 - v / 3.0, 0, 1)
        base = np.array([66, 44, 30], np.float32)
        c = base[None, None, :] * (0.85 + 0.30 * noise(Hh, Ww, 2.0, seed + 60)[..., None]) * (1 + 0.1 * nzs[..., None])
        scuff = sst(0.5, 1.2, nzs2)
        c = lerpc(c, np.array([112, 82, 56], np.float32)[None, None, :], scuff * 0.55)
        c = c * (1 - 0.20 * crk[..., None])
        mud = sst(0.035, 0.005, z) * (0.5 + 0.4 * sst(-0.3, 0.9, nzs2))
        c = lerpc(c, np.array([48, 40, 32], np.float32)[None, None, :], np.clip(mud, 0, 0.8))
        sole = (z < 0.0022)
        c = np.where(sole[..., None], np.array([34, 29, 26], np.float32)[None, None, :] * (0.8 + 0.4 * nzs[..., None]), c)
        alb = np.where(T[..., None], c, alb)
        rough = np.where(T, np.where(sole, 0.85, 0.5 + 0.28 * scuff + 0.1 * nzs), rough)
        hgt = np.where(T, -0.25 * crk + 0.15 * nzs, hgt)
    return alb, rough, hgt

def finish(alb, rough, hgt, M, outprefix):
    # gutters
    mask = M.astype(np.float32)
    alb2 = dilate_rgb(alb, mask, 6)
    rough2 = dilate_rgb(np.repeat(rough[..., None], 3, -1), mask, 6)[..., 0]
    hgt2 = dilate_rgb(np.repeat(hgt[..., None], 3, -1), mask, 6)[..., 0]
    nm = height_to_normal(hgt2, 1.3)
    write_png(outprefix + '_albedo.png', np.clip(alb2, 0, 255).astype(np.uint8)[::-1])
    write_png(outprefix + '_roughness.png', (np.clip(rough2, 0, 1) * 255).astype(np.uint8)[::-1])
    write_png(outprefix + '_normal.png', nm[::-1])
    return alb2

def repaint(objname='Amerigo_v4_LOD0', outprefix=os.path.join(ROOT, *['ArtSource', 'Characters', 'textures_v4', 'Amerigo_Clothes']), mat_index=3, seed=3):
    import bmesh
    o = bpy.data.objects[objname]
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != mat_index], context='FACES')
    me = bpy.data.meshes.new('tmp_cl'); bm.to_mesh(me); bm.free()
    tmp = bpy.data.objects.new('tmp_cl', me); bpy.context.scene.collection.objects.link(tmp)
    P, Nn, M = raster(tmp)
    alb, rough, hgt = paint(P, Nn, M, seed)
    finish(alb, rough, hgt, M, outprefix)
    bpy.data.objects.remove(tmp); bpy.data.meshes.remove(me)
    for im in bpy.data.images:
        if 'textures_v4' in im.filepath and 'Clothes' in im.filepath: im.reload()
    return M
