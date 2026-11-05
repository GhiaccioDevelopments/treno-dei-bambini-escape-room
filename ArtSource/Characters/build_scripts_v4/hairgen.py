import bpy, bmesh, math, random
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

def interp_periodic(table, az):
    """table: dict az_deg -> value, periodic over 360"""
    keys = sorted(table)
    az = (az + 180) % 360 - 180
    for i, k in enumerate(keys):
        k2 = keys[(i + 1) % len(keys)]
        a, b = k, k2 if k2 > k else k2 + 360
        aa = az if az >= k else az + 360
        if a <= aa <= b:
            t = (aa - a) / (b - a) if b != a else 0
            t = t * t * (3 - 2 * t)
            return table[k] * (1 - t) + table[k2] * t
    return table[keys[0]]

class HairGen:
    def __init__(self, head_bm, P):
        self.P = P
        self.bm = head_bm
        self.bvh = BVHTree.FromBMesh(head_bm)
        self.C = Vector(P['center'])
        self.K = Vector(P['crown'])

    def az(self, p):
        return math.degrees(math.atan2(p.x - self.C.x, -(p.y - self.C.y)))

    def hairline(self, p):
        return interp_periodic(self.P['hairline'], self.az(p))

    def surf(self, p):
        loc, n, idx, d = self.bvh.find_nearest(p)
        return loc, n.normalized(), d

    def flow(self, p, n):
        v = p - self.K
        v = v - n * v.dot(n)
        if v.length < 1e-6:
            v = Vector((0, -1, 0)) - n * n.y * -1
        return v.normalized()

    def pick_roots(self, rng):
        P = self.P
        roots = []
        # 1) border row along the hairline
        tries = 0
        pts = []
        def far_enough(h, dmin):
            return all((h - q).length >= dmin for q in pts)
        # ray casts from center
        cand = []
        for _ in range(P.get('ncand', 9000)):
            th = rng.uniform(0, 2 * math.pi)
            ez = rng.uniform(-0.15, 1.0)
            r = math.sqrt(1 - ez * ez)
            d = Vector((r * math.sin(th), -r * math.cos(th), ez))
            hit = self.bvh.ray_cast(self.C, d, 0.5)
            if hit[0] is None: continue
            cand.append(hit[0])
        # sort by height so low (hairline) cards first
        hl = lambda h: self.hairline(h)
        border = [h for h in cand if hl(h) - 0.002 <= h.z <= hl(h) + P.get('border_band', 0.014)]
        rng.shuffle(border)
        for h in border:
            if far_enough(h, P['dmin_border']):
                pts.append(h)
        nb = len(pts)
        inner = [h for h in cand if h.z > hl(h) + P.get('border_band', 0.014)]
        rng.shuffle(inner)
        for h in inner:
            if far_enough(h, P['dmin']):
                pts.append(h)
        return pts

    def make_cards(self, roots, rng):
        P = self.P
        cards = []
        zr = [r.z for r in roots]
        zmin, zmax = min(zr), max(zr)
        for i, h in enumerate(roots):
            loc, n, d = self.surf(h)
            t = self.flow(loc, n)
            ang = rng.normalvariate(0, P['yaw_sigma'] * (1.7 if (abs(self.az(loc)) < 65 and loc.z < 1.165) else 1.0))
            rot = Matrix.Rotation(ang, 3, n)
            t = (rot @ t).normalized()
            zz = (loc.z - zmin) / max(zmax - zmin, 1e-6)
            L = P['length'](loc, self, rng)
            spiky = rng.random() < P.get('spiky_prob', 0.0)
            lift = P['lift'] * (1.7 if spiky else 1.0) * rng.uniform(0.7, 1.3)
            if spiky:
                L *= 1.25
                t = (Matrix.Rotation(rng.uniform(-0.9, 0.9), 3, n) @ t).normalized()
            layer = min(2, int(zz * 3.0 + rng.uniform(-0.25, 0.25)))
            layer = max(0, layer)
            cards.append(dict(root=loc, n=n, t=t, L=L, lift=lift, layer=layer,
                              width=P['width'] * rng.uniform(0.85, 1.2), var=(P['var_fn'](loc, self, rng) if 'var_fn' in P else rng.randrange(4)),
                              spiky=spiky, idx=i, jit=rng.uniform(-0.006, 0.012),
                              droop=P['droop'] * rng.uniform(0.7, 1.3)))
        tb = P.get('tip_below')
        if tb:
            for c in cards:
                for _ in range(6):
                    pts, _n = self.polyline(c, 4)
                    tip = pts[-1]
                    if tip.z < self.hairline(tip) - tb(abs(self.az(tip))) - c['jit']: c['L'] *= 0.85
                    else: break
        return cards

    def polyline(self, c, S):
        P = self.P
        off0 = P['off0'] + P['off_layer'] * c['layer']
        p = c['root'] + c['n'] * off0
        pts = [p.copy()]
        d = (c['t'] * 0.8 + c['n'] * (0.12 + c['lift'])).normalized()
        step = c['L'] / S
        norms = [c['n'].copy()]
        for i in range(1, S + 1):
            q, nq, _ = self.surf(p)
            tc = self.flow(q, nq)
            f = i / S
            target = (tc * 1.0 + nq * c['lift'] * (1 - f) + Vector((0, 0, -1)) * c['droop'] * f ** 1.4).normalized()
            d = (d * 0.5 + target * 0.5).normalized()
            p = p + d * step
            q, nq, _ = self.surf(p)
            off = off0 + P['off_len'] * f + (P['spiky_off'] * f if c['spiky'] else 0)
            if (p - q).dot(nq) < off:
                p = q + nq * off
            pts.append(p.copy()); norms.append(nq)
        return pts, norms

    def build(self, cards, S=5, widen=1.0, name='HairCards'):
        P = self.P
        COLS = (-1, 1) if P.get('cols', 3) == 2 else (-1, 0, 1)
        bm = bmesh.new()
        lcu = bm.verts.layers.float.new('cu'); lcv = bm.verts.layers.float.new('cv'); lvar = bm.verts.layers.float.new('var')
        lkind = bm.verts.layers.float.new('kind')
        lcid = bm.verts.layers.float.new('cid')
        for c in cards:
            pts, norms = self.polyline(c, S)
            grid = []
            for i, (p, n) in enumerate(zip(pts, norms)):
                f = i / S
                if i < S: dvec = (pts[i + 1] - p)
                else: dvec = (p - pts[i - 1])
                dvec = dvec.normalized()
                w = dvec.cross(n)
                if w.length < 1e-6: w = Vector((1, 0, 0))
                w = w.normalized()
                hw = 0.5 * c['width'] * widen * (1 - 0.40 * f ** 1.2) * (0.75 if i == 0 else 1.0)
                row = []
                for j, s in enumerate(COLS):
                    q = p + w * (hw * s) + n * (0.0016 if s == 0 else 0.0) * (1 if i > 0 else 0)
                    # pull edge verts slightly toward scalp to wrap the skull
                    if s != 0: q = q - n * 0.0012 * (1 if i > 0 else 0)
                    v = bm.verts.new(q)
                    v[lcu] = (s + 1) / 2.0; v[lcv] = f; v[lvar] = c['var']; v[lkind] = 0; v[lcid] = c['idx']
                    row.append(v)
                grid.append(row)
            for i in range(S):
                for j in range(len(COLS) - 1):
                    try:
                        bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
                    except ValueError:
                        pass
        # cap
        if not P.get('no_cap'):
            capfaces = self.cap_faces(bm, lkind, lvar, lcu, lcv)
        bm.normal_update()
        # split at x = 0
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, 0, 0), plane_no=(1, 0, 0), dist=1e-5, use_snap_center=False, clear_inner=False, clear_outer=False)
        # snap verts at x ~ 0 to exactly 0
        for v in bm.verts:
            if abs(v.co.x) < 2e-5: v.co.x = 0.0
        # UVs
        uvl = bm.loops.layers.uv.new('UVMap')
        N = 1024.0
        pad = 0.012
        vc0 = 0.125
        for f in bm.faces:
            cx = sum(v.co.x for v in f.verts) / len(f.verts)
            half = 0 if cx < 0 else 1
            for l in f.loops:
                v = l.vert
                if v[lkind] > 0.5:
                    # cap: solid region
                    u = half * 0.5 + 0.25 + (v.co.y * 3.0) % 0.05 * 0
                    uv = (half * 0.5 + 0.1 + 0.3 * ((v.co.y * 13 + v.co.z * 7) % 1.0), 0.03 + 0.05 * ((v.co.x * 17 + v.co.z * 5) % 1.0))
                else:
                    k = int(round(v[lvar]))
                    u0 = half * 0.5 + k * 0.125
                    cu = min(max(v[lcu], 0), 1)
                    # keep a little inside the lock texture (strands start ~0.11)
                    uu = u0 + 0.125 * (0.07 + 0.86 * cu)
                    vv = vc0 + (1 - vc0) * min(max(v[lcv], 0), 1) * 0.985
                    uv = (uu, vv)
                l[uvl].uv = uv
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        for p in me.polygons: p.use_smooth = True
        o = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(o)
        return o

    def cap_faces(self, bm, lkind, lvar, lcu, lcv):
        P = self.P
        src = getattr(self, 'cap_src', None) or self.bm
        sel = []
        for f in src.faces:
            c = f.calc_center_median()
            if getattr(self, 'cap_src', None) or c.z > self.hairline(c) + P.get('cap_dz', -0.002):
                sel.append(f)
        vmap = {}
        for f in sel:
            nv = []
            for v in f.verts:
                if v.index not in vmap:
                    nvv = bm.verts.new(v.co + v.normal * P.get('cap_off', 0.0015))
                    nvv[lkind] = 1.0; nvv[lvar] = 0; nvv[lcu] = 0; nvv[lcv] = 0
                    vmap[v.index] = nvv
                nv.append(vmap[v.index])
            try:
                bm.faces.new(nv)
            except ValueError:
                pass
        return sel
