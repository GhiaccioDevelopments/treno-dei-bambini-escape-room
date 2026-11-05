import bpy, bmesh, math, random
from mathutils import Vector, Matrix, noise
import cloth
from cloth import sstep

SH = Vector((0.188, -0.012, 1.427)); EL = Vector((0.356, -0.021, 1.242)); WR = Vector((0.482, -0.199, 1.123))

def sg_v(v, sg):
    return Vector((v.x * sg, v.y, v.z))

def arm_param(p):
    """(t_upper, t_lower) along the arm polyline for point p (x mirrored)"""
    q = Vector((abs(p.x), p.y, p.z))
    a = (EL - SH); b = (WR - EL)
    tu = (q - SH).dot(a) / a.length_squared
    tl = (q - EL).dot(b) / b.length_squared
    return tu, tl

def cut_sleeves(bm, t_end):
    for sg in (1, -1):
        s = sg_v(EL, sg); e = sg_v(WR, sg)
        ax = (e - s).normalized(); P = s + (e - s) * t_end
        cloth.split_by_plane(bm, P, ax)
        cloth.delete_faces_where(bm, lambda c, ax=ax, P=P: (c - P).dot(ax) > 0 and abs(c.x) > 0.3)

def boundary_loops_edges(bm, pred):
    return [e for e in bm.edges if e.is_boundary and pred(e.verts[0].co) and pred(e.verts[1].co)]

def build_dolcevita(hem_z=0.935, neck_top=1.545, neck_slope=0.62, sleeve_t=0.93, seed=3):
    bm = cloth.ref_bm()
    cloth.cut(bm, (0, 0, hem_z), (0, 0, 1), 'inner')
    cloth.cut(bm, (0, -0.08, neck_top), Vector((0, neck_slope, -1)).normalized(), 'inner')
    cut_sleeves(bm, sleeve_t)
    cloth.keep_largest(bm)
    bm.normal_update(); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.normal_update()
    for v in cloth.boundary_verts(bm):
        th = math.atan2(v.co.x, -v.co.y)
        if v.co.z < 1.0 and abs(v.co.x) < 0.35:
            v.co.z -= 0.003 + 0.007 * (noise.noise(Vector((math.cos(th) * 1.4 + seed, math.sin(th) * 1.4, 0.4))) * 0.5 + 0.5)
    for v in bm.verts:
        p = v.co; n = v.normal
        th = math.atan2(p.x, -(p.y - 0.01))
        tu, tl = arm_param(p) if abs(p.x) > 0.17 else (0, 0)
        sleeve = 0.0
        if abs(p.x) > 0.17: sleeve = 0.003 + 0.008 * max(0.0, min(1.0, (tu + tl) / 2))
        belly = 0.007 * sstep(1.30, 1.0, p.z)
        neck = 0.014 * sstep(1.46, 1.53, p.z) + 0.006 * sstep(1.56, 1.60, p.z)
        fold = 0.0022 * sstep(1.25, 0.94, p.z) * (math.sin(10 * th + 0.8) + 0.5 * math.sin(19 * th))
        wr = 0.0016 * noise.noise(p * 30 + Vector((seed, 0, 0)))
        off = 0.0042 + belly + sleeve + neck + fold + wr
        v.co = p + n * max(off, 0.003)
    return bm

def build_trousers(waist_z=1.015, hem_z=0.105, seed=5):
    bm = cloth.ref_bm()
    cloth.cut(bm, (0, 0, waist_z), (0, 0, 1), 'outer')
    cloth.cut(bm, (0, 0, hem_z), (0, 0, 1), 'inner')
    cloth.keep_largest(bm)
    bm.normal_update(); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.normal_update()
    for v in cloth.boundary_verts(bm):
        if v.co.z < 0.3:
            v.co.z += 0.002 * noise.noise(Vector((v.co.x * 30 + seed, v.co.y * 30, 1.0)))
    for v in bm.verts:
        p = v.co; n = v.normal
        sg = 1 if p.x >= 0 else -1
        xc = sg * (0.119 + (0.943 - p.z) * 0.07)
        th = math.atan2(p.x - xc, -(p.y + 0.01))
        flare = 0.004 + 0.010 * sstep(0.55, 0.105, p.z)
        fold = 0.0025 * sstep(0.7, 0.2, p.z) * (math.sin(6 * th + sg) + 0.5 * math.sin(11 * th + 2))
        crease = 0.0 * math.cos(th)
        wr = 0.0016 * noise.noise(p * 24 + Vector((seed, 1, 2)))
        thigh = 0.004 * sstep(0.95, 0.8, p.z)
        off = (0.006 + flare + fold + wr + thigh) * (0.6 + 0.4 * sstep(1.02, 0.93, p.z))
        v.co = p + n * max(off, 0.004)
    return bm

def build_coat(hip_z=0.96, hem_z=0.60, neck_z=1.53, sleeve_t=0.96, seed=7, nring=9):
    bm = cloth.ref_bm()
    cloth.cut(bm, (0, 0, hip_z), (0, 0, 1), 'inner')
    cloth.cut(bm, (0, -0.08, neck_z), Vector((0, 0.55, -1)).normalized(), 'inner')
    cut_sleeves(bm, sleeve_t)
    cloth.keep_largest(bm)
    bm.normal_update(); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.normal_update()
    # V opening via two clean plane cuts
    VA = Vector((0.006, 0, 1.20)); VT = Vector((0.085, 0, 1.54))
    vl = []
    for sg in (1, -1):
        A = Vector((sg * VA.x, 0, VA.z)); d = Vector((sg * (VT.x - VA.x), 0, VT.z - VA.z)).normalized()
        nn = Vector((-sg * d.z, 0, sg * sg * d.x * sg)) if False else Vector((-sg * d.z, 0, abs(d.x)))
        vl.append((A, nn))
        cloth.split_by_plane(bm, A, nn)
    def vopen(c):
        if c.y > -0.02: return False
        return all((c - A).dot(nn) > 1e-5 for A, nn in vl)
    cloth.delete_faces_where(bm, vopen)
    bm.normal_update()
    # offset
    for v in bm.verts:
        p = v.co; n = v.normal
        th = math.atan2(p.x, -(p.y - 0.01))
        tu, tl = arm_param(p) if abs(p.x) > 0.19 else (0, 0)
        sleeve = 0.0
        if abs(p.x) > 0.19: sleeve = 0.010 + 0.003 * max(0.0, min(1.0, (tu + tl) / 2))
        shoulder = -0.002 * sstep(1.40, 1.50, p.z)
        fold = 0.003 * sstep(1.3, 0.97, p.z) * (math.sin(9 * th + 0.5) + 0.5 * math.sin(17 * th + 1))
        wr = 0.0022 * noise.noise(p * 22 + Vector((seed, 0, 0)))
        off = 0.021 + sleeve + shoulder + fold + wr
        v.co = p + n * max(off, 0.017)
    bm.normal_update()
    global VLINES
    VLINES = vl
    # skirt: extrude hip loop in steps
    hip_edges = [e for e in bm.edges if e.is_boundary and e.verts[0].co.z < hip_z + 0.05 and e.verts[1].co.z < hip_z + 0.05 and abs(e.verts[0].co.x) < 0.3 and abs(e.verts[1].co.x) < 0.3]
    ring0 = {}
    cur_edges = hip_edges
    dz = (hip_z - hem_z) / nring
    base_pos = {}
    for k in range(1, nring + 1):
        ext = bmesh.ops.extrude_edge_only(bm, edges=cur_edges)
        nv = [g for g in ext['geom'] if isinstance(g, bmesh.types.BMVert)]
        ne = [g for g in ext['geom'] if isinstance(g, bmesh.types.BMEdge)]
        # which new edges are the boundary of the new ring? (edges not connecting old-new)
        ring_edges = [e for e in ne if e.is_boundary]
        prog = k / nring
        for v in nv:
            if v not in base_pos: base_pos[v] = v.co.copy()
        cur_edges = ring_edges
        # position from the previous ring pos: compute radially from original hip ring positions
        for v in nv:
            p = v.co.copy()
            # find original: approximate by projecting the unflared radius: use stored by previous iteration (v.co is copy of previous ring pos)
            v.co = p
        for v in nv:
            c = Vector((0, 0.005, 0))
            q = v.co - Vector((0, 0, v.co.z)) - c
            # radial flare in steps (apply incremental scale)
            sx = (1 + 0.42 * prog ** 1.1) / (1 + 0.42 * ((k - 1) / nring) ** 1.1)
            sy = (1 + 0.60 * prog ** 1.1) / (1 + 0.60 * ((k - 1) / nring) ** 1.1)
            nx = q.x * sx; ny = q.y * sy
            th = math.atan2(nx, -ny)
            fold = 0.011 * (prog ** 1.3) * (math.sin(8 * th + 0.4) + 0.55 * math.sin(15 * th + 1.2)) - 0.011 * ((k - 1) / nring) ** 1.3 * (math.sin(8 * th + 0.4) + 0.55 * math.sin(15 * th + 1.2))
            rr = math.hypot(nx, ny) + 1e-9
            nx += nx / rr * fold; ny += ny / rr * fold
            v.co = Vector((nx + c.x, ny + c.y, hip_z - dz * k))
    bm.normal_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # hem irregular
    for v in cloth.boundary_verts(bm):
        if v.co.z < 0.7:
            th = math.atan2(v.co.x, -v.co.y)
            v.co.z += 0.006 * noise.noise(Vector((math.cos(th) * 3 + seed, math.sin(th) * 3, 2.0)))
    return bm
