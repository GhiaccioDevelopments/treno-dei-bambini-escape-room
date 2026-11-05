import bpy, numpy as np, math
from texlib import *
from tex_child import raster, n3, vor, weave, lerpc, sst, N

def paint_adult(P, Nn, M, seed=5):
    Hh = Ww = N
    Y, X = np.mgrid[0:Hh, 0:Ww].astype(np.float32)
    alb = np.zeros((Hh, Ww, 3), np.float32); rough = np.full((Hh, Ww), 0.85, np.float32); hgt = np.zeros((Hh, Ww), np.float32)
    x, y, z = P[..., 0], P[..., 1], P[..., 2]
    ax = np.abs(x)
    def tile(x0, x1, y0, y1):
        m = np.zeros((Hh, Ww), bool); m[y0:y1, x0:x1] = True; return m
    nz1 = n3(P, 55, seed); nz2 = n3(P, 17, seed + 1); nz3 = n3(P, 8, seed + 2); fz = n3(P, 130, seed + 3, 2)
    # ---------------- coat (dark brown wool, herringbone) ----------------
    T = tile(0, 512, 512, 1024)
    wv, wc = weave(X, Y, 6, seed, slub=0.3, twill=True)
    # herringbone: flip twill direction every 40px band
    band = (np.floor(X / 44).astype(int) % 2) == 0
    hb = np.where(band, np.sin(2 * np.pi * (X + Y) / 7), np.sin(2 * np.pi * (X - Y) / 7)) * 0.5 + 0.5
    base = np.array([56, 42, 33], np.float32)
    c = base[None, None, :] * (1 + 0.07 * nz1[..., None] + 0.08 * nz2[..., None] + 0.05 * fz[..., None]) * (0.975 + 0.025 * hb[..., None])
    c = lerpc(c, c * np.array([0.8, 0.78, 0.74]), sst(0.4, 1.1, nz3) * 0.5)
    hem = sst(0.80, 0.52, z) * (z < 1.0) * (0.35 + 0.45 * sst(-0.3, 0.9, nz2))
    c = lerpc(c, np.array([44, 36, 28], np.float32)[None, None, :], np.clip(hem, 0, 0.8))
    elbow = np.exp(-(((ax - 0.36) / 0.05) ** 2 + ((z - 1.25) / 0.05) ** 2)) * 0.35
    c = lerpc(c, np.array([88, 68, 52], np.float32)[None, None, :], elbow)
    cuff = sst(0.40, 0.47, ax) * (z < 1.3) * 0.4
    c = lerpc(c, np.array([50, 40, 32], np.float32)[None, None, :], cuff)
    shoulder = sst(1.46, 1.52, z) * 0.22
    c = lerpc(c, np.array([84, 66, 52], np.float32)[None, None, :], shoulder)
    alb = np.where(T[..., None], c, alb)
    rough = np.where(T, 0.88 + 0.07 * fz, rough); hgt = np.where(T, hb * 0.07 + 0.1 * nz1 + 0.06 * fz, hgt)
    # ---------------- dolcevita (rib knit, blu notte) ----------------
    T = tile(512, 1024, 512, 1024)
    rib = np.sin(2 * np.pi * X / 6.0) * 0.5 + 0.5
    knit = (np.sin(2 * np.pi * Y / 4.0 + (np.floor(X / 6.0) % 2) * 2.0) * 0.5 + 0.5)
    base = np.array([44, 52, 70], np.float32)
    c = base[None, None, :] * (1 + 0.08 * nz1[..., None] + 0.08 * nz2[..., None] + 0.06 * fz[..., None]) * (0.93 + 0.07 * (0.6 * rib + 0.4 * knit))[..., None]
    c = lerpc(c, c * np.array([0.85, 0.85, 0.85]), sst(0.3, 1.1, nz3) * 0.4)
    neck = sst(1.50, 1.60, z) * 0.15
    c = lerpc(c, np.array([62, 70, 90], np.float32)[None, None, :], neck)
    alb = np.where(T[..., None], c, alb)
    rough = np.where(T, 0.96, rough); hgt = np.where(T, (0.6 * rib + 0.4 * knit) * 0.18 + 0.1 * nz1, hgt)
    # ---------------- trousers (charcoal flannel) ----------------
    T = tile(0, 512, 0, 512)
    twl = np.sin(2 * np.pi * (X + Y) / 5.0) * 0.5 + 0.5
    base = np.array([58, 58, 60], np.float32)
    c = base[None, None, :] * (1 + 0.07 * nz1[..., None] + 0.07 * nz2[..., None] + 0.08 * fz[..., None]) * (0.98 + 0.02 * twl[..., None])
    hemd = sst(0.45, 0.105, z) * (0.3 + 0.4 * sst(-0.3, 0.9, nz2))
    c = lerpc(c, np.array([40, 36, 32], np.float32)[None, None, :], np.clip(hemd, 0, 0.75))
    knee = np.exp(-(((ax - 0.15) / 0.07) ** 2 + ((z - 0.52) / 0.06) ** 2)) * 0.25
    c = lerpc(c, np.array([76, 76, 78], np.float32)[None, None, :], knee)
    alb = np.where(T[..., None], c, alb)
    rough = np.where(T, 0.9 + 0.05 * fz, rough); hgt = np.where(T, twl * 0.07 + 0.12 * fz, hgt)
    # ---------------- boots ----------------
    for (x0, x1) in ((512, 768), (768, 1024)):
        T = tile(x0, x1, 256, 512)
        nzs = n3(P, 65, seed + 50); nzs2 = n3(P, 21, seed + 51)
        v = vor(256, 256, 70, seed + 52 + x0); crk = np.zeros((Hh, Ww), np.float32); crk[256:512, x0:x1] = np.clip(1 - v / 2.6, 0, 1)
        base = np.array([38, 27, 21], np.float32)
        c = base[None, None, :] * (0.9 + 0.2 * noise(Hh, Ww, 2.0, seed + 61)[..., None]) * (1 + 0.08 * nzs[..., None])
        scuff = sst(0.55, 1.2, nzs2)
        c = lerpc(c, np.array([84, 64, 48], np.float32)[None, None, :], scuff * 0.45)
        crease = np.exp(-(((z - 0.09) / 0.012) ** 2)) * (y < -0.0)
        c = c * (1 - 0.16 * crk[..., None]) * (1 - 0.15 * crease[..., None])
        mud = sst(0.05, 0.005, z) * (0.4 + 0.4 * sst(-0.3, 0.9, nzs2))
        c = lerpc(c, np.array([52, 44, 36], np.float32)[None, None, :], np.clip(mud, 0, 0.7))
        sole = (z < 0.0035)
        c = np.where(sole[..., None], np.array([26, 22, 20], np.float32)[None, None, :] * (0.8 + 0.4 * nzs[..., None]), c)
        alb = np.where(T[..., None], c, alb)
        rough = np.where(T, np.where(sole, 0.85, 0.38 + 0.32 * scuff), rough)
        hgt = np.where(T, -0.18 * crk - 0.2 * crease + 0.1 * nzs, hgt)
    # ---------------- buttons (horn) & laces ----------------
    T = tile(512, 640, 0, 256)
    mt = noise(Hh, Ww, 5.0, seed + 70, 2)
    alb = np.where(T[..., None], np.array([46, 34, 26], np.float32)[None, None, :] * (0.7 + 0.6 * mt[..., None]), alb)
    rough = np.where(T, 0.35, rough); hgt = np.where(T, 0.05 * mt, hgt)
    T = tile(640, 768, 0, 256)
    alb = np.where(T[..., None], np.array([24, 22, 22], np.float32)[None, None, :] * (0.8 + 0.4 * mt[..., None]), alb)
    rough = np.where(T, 0.8, rough)
    return alb, rough, hgt

def repaint_adult(objname, outprefix, mat_index=3, seed=5):
    import bmesh
    from tex_child import finish
    o = bpy.data.objects[objname]
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != mat_index], context='FACES')
    me = bpy.data.meshes.new('tmp_cl'); bm.to_mesh(me); bm.free()
    tmp = bpy.data.objects.new('tmp_cl', me); bpy.context.scene.collection.objects.link(tmp)
    P, Nn, M = raster(tmp)
    alb, rough, hgt = paint_adult(P, Nn, M, seed)
    finish(alb, rough, hgt, M, outprefix)
    bpy.data.objects.remove(tmp); bpy.data.meshes.remove(me)
    for im in bpy.data.images:
        if 'textures_v4' in im.filepath and 'AmerigoAdulto_Clothes' in im.filepath: im.reload()
    return M
