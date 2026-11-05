import os
ROOT = os.environ.get("TRENO_ROOT", r".")  # cartella del progetto Il Treno dei Bambini
SCRATCH = os.environ.get("TRENO_SCRATCH", os.path.join(ROOT, "ArtSource", "tmp"))
import sys, importlib, bpy, bmesh, random, math
import hairgen, hair_child
from mathutils import Vector

HEAD = 'Base'

def sstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)

def length_adult(loc, g, rng):
    z = loc.z; az = abs(g.az(loc))
    if z > 1.755: L = 0.062
    elif az < 45: L = 0.040
    elif az < 120: L = 0.050
    else: L = 0.062
    return L * rng.uniform(0.8, 1.25)

def var_adult(loc, g, rng):
    az = abs(g.az(loc))
    t = math.exp(-((az - 75) / 32.0) ** 2) * sstep(1.60, 1.67, loc.z) * (1.0 - sstep(1.735, 1.77, loc.z))
    gr = min(1.0, 0.03 + 1.05 * t + rng.uniform(-0.10, 0.10))
    k = int(max(0, min(3, math.floor(gr * 4.0))))
    return k

PAD = dict(center=(0, -0.06, 1.68), crown=(0, -0.45, 1.85),
  hairline={0: 1.722, 20: 1.732, 40: 1.738, 60: 1.700, 80: 1.672, 100: 1.664, 120: 1.640, 150: 1.612, 180: 1.600, -150: 1.612, -120: 1.640, -100: 1.664, -80: 1.672, -60: 1.700, -40: 1.738, -20: 1.732},
  tip_below=lambda a: -0.002 if a < 50 else (0.008 if a < 130 else 0.02),
  dmin=0.0150, dmin_border=0.0096, border_band=0.015, yaw_sigma=0.30, spiky_prob=0.0, lift=0.10, droop=0.75,
  off0=0.0018, off_layer=0.0026, off_len=0.0016, spiky_off=0.0, width=0.0170, length=length_adult, cap_off=0.0012, cap_dz=-0.003, cap_in=0.006, var_fn=var_adult)

def build_adult_hair(over=None, seed=21, S=4, keep=1.0, widen=1.0, name='HairNew_LOD0', ncand=26000, cap_tris=380):
    P = dict(PAD); P['ncand'] = ncand
    if over: P.update(over)
    bm = hair_child.head_bm(HEAD, 1.58)
    g = hairgen.HairGen(bm, P)
    if cap_tris:
        g.cap_src = hair_child.make_cap_src(bm, g, cap_tris)
    rng = random.Random(seed)
    roots = g.pick_roots(rng)
    cards = g.make_cards(roots, rng)
    if keep < 1.0:
        cards = [c for i, c in enumerate(cards) if (i * 0.6180339) % 1.0 < keep]
    o = g.build(cards, S=S, widen=widen, name=name)
    bm.free()
    return o, len(cards)

def regen_adult(S=4, textures=True):
    import hairtex; importlib.reload(hairtex)
    T = os.path.join(ROOT, *['ArtSource', 'Characters', 'textures_v4'])
    if textures:
        hairtex.make_atlas(T + r"\AmerigoAdulto_Hair", palL=[(62,32,20),(100,54,32),(128,72,44)], palR=[(36,24,16),(66,44,28),(92,62,40)],
                           greyL=(0.02, 0.10, 0.30, 0.58), greyR=(0.02, 0.10, 0.30, 0.58), seed=17, nstr=11)
    old = bpy.data.objects.get('HairNew_LOD0')
    if old: bpy.data.objects.remove(old)
    o, n = build_adult_hair(S=S)
    return o, n
