import bpy, bmesh, math, random
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
import hairgen, hair_child
from texlib import write_png, height_to_normal, dilate_rgb

C0 = Vector((0, -0.06, 1.68))
TILT = 0.35
RX = Matrix.Rotation(TILT, 3, 'X')
URPT = 3.0
HL = {0: 1.712, 20: 1.720, 40: 1.730, 60: 1.708, 80: 1.682, 100: 1.664, 120: 1.632, 150: 1.592, 180: 1.578,
      -20: 1.720, -40: 1.730, -60: 1.708, -80: 1.682, -100: 1.664, -120: 1.632, -150: 1.592}

def sstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)

def loc_angles(p):
    d = RX @ (p - C0)
    r = d.length
    phi = math.acos(max(-1, min(1, d.z / r)))
    th = math.atan2(d.x, -d.y)
    return th, phi

def az_world(p):
    return math.degrees(math.atan2(p.x, -(p.y - C0.y)))

def hl_z(p):
    a = abs(az_world(p))
    return hairgen.interp_periodic(HL, az_world(p)) + 0.0035 * math.sin(a * 0.23 + 1.3) + 0.0025 * math.sin(a * 0.51 + 0.2)

class Frame:
    def __init__(self, bvh, nth=96):
        self.nth = nth
        self.phi_hl = []
        Rinv = RX.inverted()
        for k in range(nth):
            th = -math.pi + 2 * math.pi * k / nth
            prev = None; res = None
            for i in range(0, 130):
                phi = 0.02 + i * 0.019
                dl = Vector((math.sin(phi) * math.sin(th), -math.sin(phi) * math.cos(th), math.cos(phi)))
                d = Rinv @ dl
                h = bvh.ray_cast(C0, d, 0.6)
                if h[0] is None: continue
                diff = h[0].z - hl_z(h[0])
                if prev is not None and prev[1] > 0 >= diff:
                    t = prev[1] / (prev[1] - diff)
                    res = prev[0] + (phi - prev[0]) * t; break
                prev = (phi, diff)
            self.phi_hl.append(res if res is not None else 2.0)
        # smooth
        a = np.array(self.phi_hl)
        for _ in range(2):
            a = (np.roll(a, 1) + a * 2 + np.roll(a, -1)) / 4
        self.phi_hl = a

    def hl(self, th):
        x = (th + math.pi) / (2 * math.pi) * self.nth
        i = int(math.floor(x)) % self.nth; f = x - math.floor(x)
        return self.phi_hl[i] * (1 - f) + self.phi_hl[(i + 1) % self.nth] * f

    def vprime(self, p):
        th, phi = loc_angles(p)
        return phi / self.hl(th), th

def k_for(c, rng):
    az = abs(az_world(c))
    t = math.exp(-((az - 75) / 32.0) ** 2) * sstep(1.60, 1.67, c.z) * (1.0 - sstep(1.735, 1.77, c.z))
    gr = min(1.0, 0.02 + 1.05 * t + rng.uniform(-0.10, 0.10))
    return int(max(0, min(7, round(gr * 7.0 + rng.uniform(-0.4, 0.4)))))

def make_texture(prefix, palL, palR, greys=(0.0, 0.09, 0.24, 0.45), seed=5, W=1024, H=1024, nstr=230):
    rng = np.random.default_rng(seed)
    BH = H // 16
    alb = np.zeros((H, W, 4), np.float32); hmap = np.zeros((H, W), np.float32)
    GREY = np.array([104, 98, 92], float)
    for band in range(16):
        half = band // 8; k = band % 8
        pal = [np.array(c, float) for c in (palL if half == 0 else palR)]
        v = (np.arange(BH) / (BH - 1))[:, None]
        pitch = W / nstr
        best = np.full((BH, W), -1.0); rgb = np.zeros((BH, W, 3)); hh = np.zeros((BH, W))
        for i in range(nstr):
            cx = (i + rng.uniform(-0.35, 0.35)) * pitch
            hw0 = pitch * rng.uniform(0.85, 1.25)
            vend = rng.uniform(0.66, 0.90)
            sway = rng.uniform(0.0, 1.2)
            cols = np.arange(int(cx - hw0 * 1.6) - 1, int(cx + hw0 * 1.6) + 2)
            cw = cols % W
            xs = cols[None, :].astype(float) + 0.5
            ctr = cx + sway * np.sin(v * rng.uniform(2, 6) + rng.uniform(0, 6))
            hw = hw0 * (1.0 - np.clip((v - vend) / (0.925 - vend + 1e-6), 0, 1) ** 1.0)
            d = np.abs(xs - ctr)
            inside = (d < hw) & (v < 0.925)
            prof = np.sqrt(np.clip(1 - (d / np.maximum(hw, 1e-3)) ** 2, 0, 1))
            score = np.where(inside, prof + rng.uniform(0, 0.3), -1.0)
            isgrey = rng.random() < 0.85 * k / 7.0
            t = v
            c = np.where((t < 0.5)[..., None], pal[0] + (pal[1] - pal[0]) * np.clip(t / 0.5, 0, 1)[..., None],
                         pal[1] + (pal[2] - pal[1]) * np.clip((t - 0.5) / 0.5, 0, 1)[..., None])
            c = np.broadcast_to(c, (BH, len(cols), 3)).copy()
            if isgrey:
                ramp = (t * t * (3 - 2 * np.clip((t - 0.2) / 0.75, 0, 1))) if False else np.clip((t - 0.2) / 0.75, 0, 1)
                ramp = ramp * ramp * (3 - 2 * ramp)
                c = c + (np.broadcast_to(GREY * rng.uniform(0.8, 1.05), c.shape) * (0.6 + 0.4 * t[..., None]) - c) * (0.8 * ramp)[..., None]
            c = c * rng.normal(1.0, 0.06) * (0.80 + 0.20 * prof)[..., None]
            sub_best = best[:, cw]
            upd = score > sub_best
            best[:, cw] = np.where(upd, score, sub_best)
            rgb[:, cw] = np.where(upd[..., None], c, rgb[:, cw])
            hh[:, cw] = np.where(upd, prof, hh[:, cw])
        a = (best > 0).astype(np.float32)
        r0 = band * BH
        alb[r0:r0 + BH, :, :3] = rgb; alb[r0:r0 + BH, :, 3] = a; hmap[r0:r0 + BH] = hh * a
    alb[..., :3] = dilate_rgb(alb[..., :3], alb[..., 3], 8)
    nrm = height_to_normal(hmap, 3.0)
    img = np.clip(alb, 0, 255); img[..., 3] *= 255
    write_png(prefix + '_albedo.png', np.clip(img, 0, 255).astype(np.uint8)[::-1])
    write_png(prefix + '_normal.png', nrm[::-1])
    prev = (img[..., :3] * (img[..., 3:4] / 255) + 120 * (1 - img[..., 3:4] / 255)).astype(np.uint8)
    write_png(prefix + '_preview.png', prev[::-1])

def band_v(band, t):
    return (band + 0.04 + 0.92 * max(0.0, min(1.0, t))) / 16.0

def build_shell(head_bm, fr, rng, off0=0.0050, off1=0.0032):
    bm = bmesh.new(); vmap = {}
    for f in head_bm.faces:
        c = f.calc_center_median()
        if c.z < 1.56: continue
        vps = [fr.vprime(v.co)[0] for v in f.verts]
        if min(vps) >= 1.0: continue
        nv = []
        for v, vp in zip(f.verts, vps):
            if v.index not in vmap:
                off = off0 + off1 * max(0.0, min(1.0, 1.0 - vp))
                vmap[v.index] = bm.verts.new(v.co + v.normal * off)
            nv.append(vmap[v.index])
        try: bm.faces.new(nv)
        except ValueError: pass
    bm.normal_update()
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, 0, 0), plane_no=(1, 0, 0), dist=1e-5)
    for v in bm.verts:
        if abs(v.co.x) < 2e-5: v.co.x = 0.0
    uvl = bm.loops.layers.uv.new('UVMap')
    for f in bm.faces:
        c = f.calc_center_median()
        half = 0 if c.x < 0 else 1
        k = k_for(c, rng)
        band = half * 8 + k
        ths = []; vps = []
        for l in f.loops:
            vp, th = fr.vprime(l.vert.co); ths.append(th); vps.append(vp)
        th0 = ths[0]
        for l, th, vp in zip(f.loops, ths, vps):
            d = th - th0
            d = (d + math.pi) % (2 * math.pi) - math.pi
            u = (th0 + d) / (2 * math.pi) * URPT
            l[uvl].uv = (u, band_v(band, min(vp, 1.08) / 1.08))
    me = bpy.data.meshes.new('HairShell'); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    o = bpy.data.objects.new('HairShell', me); bpy.context.scene.collection.objects.link(o)
    return o

def build_edge(head_bm, fr, rng, seed=3, rows=(0.94, 0.89), dmin=0.0068):
    bvh = BVHTree.FromBMesh(head_bm)
    P = dict(center=tuple(C0), crown=tuple(C0 + (RX.inverted() @ Vector((0, 0, 0.30)))), hairline=HL, off0=0.0072, off_layer=0.0012, off_len=0.0008,
             spiky_off=0.0, no_cap=True, cols=2, yaw_sigma=0.1, lift=0.0, droop=0.6, tip_below=None, width=0.0065, length=None, dmin=dmin, dmin_border=dmin)
    g = hairgen.HairGen(head_bm, P)
    rinv = RX.inverted()
    pts = []; tries = 0
    while tries < 6000:
        tries += 1
        th = rng.uniform(-math.pi, math.pi)
        azd = abs(math.degrees(th))
        zone = azd < 62 or 68 < azd < 118 or azd > 138
        if not zone: continue
        row = rng.choice(rows)
        phi = fr.hl(th) * rng.uniform(row - 0.04, row + 0.03)
        dl = Vector((math.sin(phi) * math.sin(th), -math.sin(phi) * math.cos(th), math.cos(phi)))
        h = bvh.ray_cast(C0, rinv @ dl, 0.6)
        if h[0] is None: continue
        if all((h[0] - q).length > dmin for q in pts): pts.append(h[0])
    cards = []
    for i, h in enumerate(pts):
        loc, n, d = g.surf(h)
        t = g.flow(loc, n)
        t = (Matrix.Rotation(rng.normalvariate(0, 0.12), 3, n) @ t).normalized()
        az = abs(az_world(loc))
        L = (0.0105 if az < 62 else (0.0125 if az < 118 else 0.019)) * rng.uniform(0.7, 1.25)
        cards.append(dict(root=loc, n=n, t=t, L=L, lift=0.0, layer=rng.randrange(2), width=0.0068 * rng.uniform(0.8, 1.3), var=k_for(loc, rng),
                          spiky=False, idx=i, droop=0.5, jit=0.0))
    o = g.build(cards, S=3, widen=1.0, name='HairEdge')
    me = o.data
    cu = np.zeros(len(me.vertices)); cv = np.zeros(len(me.vertices)); var = np.zeros(len(me.vertices)); cid = np.zeros(len(me.vertices))
    me.attributes['cu'].data.foreach_get('value', cu); me.attributes['cv'].data.foreach_get('value', cv)
    me.attributes['var'].data.foreach_get('value', var); me.attributes['cid'].data.foreach_get('value', cid)
    uvl = me.uv_layers.active.data
    for p in me.polygons:
        c = p.center
        half = 0 if c.x < 0 else 1
        for li in p.loop_indices:
            vi = me.loops[li].vertex_index
            band = half * 8 + int(round(var[vi]))
            u0 = (cid[vi] * 0.6180339) % 1.0
            uvl[li].uv = (u0 + cu[vi] * 0.035, band_v(band, 0.45 + 0.47 * cv[vi]))
    bm2 = None
    return o, len(cards)

def build_short(head_name='AmerigoAdulto_LOD0', seed=7, shell_tris=1300, edge=True, edge_keep=1.0):
    rng = random.Random(seed)
    src = hair_child.head_bm(head_name, 1.50)
    bvh = BVHTree.FromBMesh(src)
    fr = Frame(bvh)
    # decimate shell source
    me = bpy.data.meshes.new('hs_tmp'); src.to_mesh(me)
    ob = bpy.data.objects.new('hs_tmp', me); bpy.context.scene.collection.objects.link(ob)
    t0 = sum(len(p.vertices) - 2 for p in me.polygons)
    nb = bmesh.new(); nb.from_mesh(me)
    # keep only faces above the hairline roughly before decimation
    keep = [f for f in nb.faces if min(fr.vprime(v.co)[0] for v in f.verts) < 1.12 and f.calc_center_median().z > 1.55]
    ks = set(keep)
    bmesh.ops.delete(nb, geom=[f for f in nb.faces if f not in ks], context='FACES')
    bmesh.ops.delete(nb, geom=[v for v in nb.verts if not v.link_faces], context='VERTS')
    nb.to_mesh(me); nb.free()
    t1 = sum(len(p.vertices) - 2 for p in me.polygons)
    if shell_tris < t1:
        md = ob.modifiers.new('d', 'DECIMATE'); md.ratio = shell_tris / t1; md.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get(); ev = ob.evaluated_get(dg)
    hb = bmesh.new(); hb.from_mesh(ev.to_mesh()); hb.normal_update()
    bpy.data.objects.remove(ob); bpy.data.meshes.remove(me)
    shell = build_shell(hb, fr, rng)
    objs = [shell]; ncards = 0
    if edge:
        eo, ncards = build_edge(src, fr, rng, seed=seed)
        objs.append(eo)
    hb.free(); src.free()
    return objs, ncards
