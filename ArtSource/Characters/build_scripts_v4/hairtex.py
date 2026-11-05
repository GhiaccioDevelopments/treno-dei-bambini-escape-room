import numpy as np
from texlib import *

GREY = np.array([132, 126, 120], float)

def lerp(a, b, t):
    return a + (b - a) * t

def make_lock(W, Hh, pal, rng, grey=0.0, nstr=11, wide=1.0, tip_mode='taper'):
    """returns rgb (Hh,W,3) float 0-255, alpha (Hh,W), height (Hh,W); row 0 = root (v=0)"""
    v = (np.arange(Hh) / (Hh - 1))[:, None]
    u = (np.arange(W) / (W - 1))[None, :]
    best = np.full((Hh, W), -1.0)
    rgb = np.zeros((Hh, W, 3)); hmap = np.zeros((Hh, W))
    spacing = 0.78 / (nstr - 1)
    for i in range(nstr):
        u0 = 0.11 + i * spacing + rng.uniform(-0.02, 0.02)
        # narrower strands toward lock edges -> rounded lock outline, tip pointed
        edge = abs(u0 - 0.5) * 2
        vend = np.clip(rng.uniform(0.80, 1.0) - 0.20 * edge ** 2, 0.55, 1.0)
        if tip_mode == 'short': vend *= 0.8
        hw0 = spacing * rng.uniform(0.85, 1.25) * wide
        sway = rng.uniform(0.004, 0.022); f = rng.uniform(0.8, 2.2); ph = rng.uniform(0, 6.28)
        uc = u0 + sway * np.sin(2 * np.pi * f * v + ph) + 0.012 * (v ** 2) * (u0 - 0.5) * 2
        hw = hw0 * (1.0 - 0.9 * smoothstep(vend * 0.25, vend, v)) * (0.45 + 0.55 * smoothstep(0, 0.12, v))
        d = np.abs(u - uc)
        inside = (d < hw) & (v < vend)
        prof = np.sqrt(np.clip(1 - (d / np.maximum(hw, 1e-4)) ** 2, 0, 1))
        score = np.where(inside, prof + rng.uniform(0, 0.25), -1)
        # color of this strand
        isgrey = rng.random() < grey
        bright = rng.normal(1.0, 0.035)
        t = v
        col_root, col_mid, col_tip = [np.array(c, float) for c in pal]
        c = np.where((t < 0.5)[..., None], lerp(col_root, col_mid, np.clip(t / 0.5, 0, 1)[..., None]),
                     lerp(col_mid, col_tip, np.clip((t - 0.5) / 0.5, 0, 1)[..., None]))
        c = np.broadcast_to(c, (Hh, W, 3)).copy()
        if isgrey:
            gv = GREY * rng.uniform(0.78, 1.05)
            c = lerp(c, np.broadcast_to(gv, c.shape) * (0.55 + 0.45 * t[..., None]), 0.8)
        c = c * bright * (0.45 + 0.55 * smoothstep(0, 0.30, t))[..., None]
        c = c * (0.72 + 0.28 * prof)[..., None]
        # fine streak
        c = c * (1 + 0.03 * np.sin((u - uc) / max(hw0, 1e-3) * 5 + ph))[..., None]
        upd = score > best
        best = np.where(upd, score, best)
        rgb = np.where(upd[..., None], c, rgb)
        hmap = np.where(upd, prof, hmap)
    alpha = (best > 0).astype(float)
    return np.clip(rgb, 0, 255), alpha, hmap

def make_atlas(outprefix, palL, palR, seed=1, greyL=(0, 0, 0, 0), greyR=(0, 0, 0, 0), cap_cols=None, size=1024, nstr=11, wide=1.0):
    """atlas: left half (u 0-.5) palL, right half palR. 4 lock variants per half.
    cards region v in [0.125,1], cap region v in [0,0.12]. Writes _albedo.png (rgba), _normal.png, _roughness not needed"""
    rng = np.random.default_rng(seed)
    N = size
    albedo = np.zeros((N, N, 4)); height = np.zeros((N, N))
    vc0 = int(N * 0.125)
    Hh = N - vc0; W = N // 8
    for half, (pal, gr) in enumerate(((palL, greyL), (palR, greyR))):
        for k in range(4):
            rgb, a, h = make_lock(W, Hh, pal, rng, grey=gr[k], nstr=nstr + (k % 2), wide=wide)
            x0 = half * (N // 2) + k * W
            albedo[vc0:, x0:x0 + W, :3] = rgb
            albedo[vc0:, x0:x0 + W, 3] = a
            height[vc0:, x0:x0 + W] = h * a
    # cap region: noisy root color
    for half, pal in enumerate((palL, palR)):
        base = np.array(pal[0], float) if cap_cols is None else np.array(cap_cols[half], float)
        n = noise(vc0 - 2, N // 2, 3, seed + 50 + half, 2)
        reg = base[None, None, :] * (0.75 + 0.5 * n[..., None])
        albedo[0:vc0 - 2, half * (N // 2):(half + 1) * (N // 2), :3] = reg
        albedo[0:vc0 - 2, half * (N // 2):(half + 1) * (N // 2), 3] = 1
    # bleed color into transparent pixels
    albedo[..., :3] = dilate_rgb(albedo[..., :3], albedo[..., 3], 10)
    nrm = height_to_normal(height, 3.0)
    albedo[..., 3] *= 255
    img = np.clip(albedo, 0, 255).astype(np.uint8)
    prev = (albedo[..., :3] * (albedo[..., 3:4] / 255) + 120 * (1 - albedo[..., 3:4] / 255)).astype(np.uint8)
    write_png(outprefix + '_preview.png', prev[::-1])
    # flip: array row 0 = v 0 = bottom  -> png row 0 must be top
    write_png(outprefix + '_albedo.png', img[::-1])
    write_png(outprefix + '_normal.png', nrm[::-1])
    return albedo
