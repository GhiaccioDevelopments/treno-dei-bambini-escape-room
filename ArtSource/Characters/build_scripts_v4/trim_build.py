import bpy, bmesh, math
from mathutils import Vector, Matrix
import cloth, trim_child
from trim_child import *

FAB = {1: (0.5, 0.25, 0.75, 0.5), 2: (0.75, 0.25, 1.0, 0.5), 3: (0.5, 0.0, 0.75, 0.25), 4: (0.75, 0.0, 1.0, 0.25)}
STRAP_RECT = (0.02, 0.0, 0.10, 0.25)
THREAD_UV = (0.30, 0.12)
METAL_UV = (0.44, 0.12)

def inset(r, d=0.004):
    return (r[0] + d / 1000 * 0 + 0.003, r[1] + 0.003, r[2] - 0.003, r[3] - 0.003)

def set_uv(bm, faces, rect, fixed=None):
    uvl = bm.loops.layers.uv.verify()
    pu = bm.verts.layers.float.get('pu'); pv = bm.verts.layers.float.get('pv')
    u0, v0, u1, v1 = rect if rect else (0, 0, 1, 1)
    for f in faces:
        for l in f.loops:
            if fixed: l[uvl].uv = fixed
            else: l[uvl].uv = (u0 + (u1 - u0) * min(max(l.vert[pu], 0), 1), v0 + (v1 - v0) * min(max(l.vert[pv], 0), 1))

def build_patches(shirt, shorts, seed=40):
    bs = obj_bvh(shirt); bh = obj_bvh(shorts)
    bm = bmesh.new()
    spec = [
        (bs, (0.0, -0.5, 0.885), (0, 1, 0), (0.066, 0.074), 0.12, 1),
        (bs, (-0.098, -0.5, 0.675), (0, 1, 0), (0.046, 0.07), -0.10, 2),
        (bs, (0.0, 0.5, 0.78), (0, -1, 0), (0.07, 0.085), 0.08, 4),
        (bs, (0.183, -0.5, 0.845), (0, 1, 0), (0.046, 0.052), 0.45, 3),
        (bh, (0.100, -0.5, 0.50), (0, 1, 0), (0.085, 0.100), 0.10, 3),
        (bh, (-0.075, 0.5, 0.56), (0, -1, 0), (0.100, 0.090), -0.15, 1),
        (bh, (-0.125, -0.5, 0.44), (0, 1, 0), (0.050, 0.046), 0.2, 2),
    ]
    info = []
    for k, (bv, org, dr, size, rot, fab) in enumerate(spec):
        faces, side, st, c, n = build_patch(bm, bv, Vector(org), Vector(dr), size, rot, seed + k, FAB[fab])
        set_uv(bm, faces + side, inset(FAB[fab]))
        set_uv(bm, st, None, fixed=THREAD_UV)
        info.append((k, tuple(round(x, 3) for x in c), len(faces) + len(side), len(st)))
    return bm, info

def build_straps(shirt, x=0.062):
    bvh = obj_bvh(shirt)
    bm = bmesh.new()
    allfaces = []
    metal = []
    for sg in (1, -1):
        pts, nrm = strap_path(bvh, x * sg)
        faces, rows = build_strap(bm, pts, nrm, 0.020, 0.0030, STRAP_RECT, None)
        set_uv(bm, faces, STRAP_RECT)
        # make orientation consistent: flip if normal points inward
        bm.normal_update()
        outward = sum(f.normal.dot(nrm[min(i, len(nrm) - 1)]) for i, f in enumerate(faces[::2]))
        if outward < 0:
            bmesh.ops.reverse_faces(bm, faces=faces)
        # buckle at ~35% of the front run
        idx = 8
        p = pts[idx]; n = nrm[idx]
        t = (pts[idx + 1] - pts[idx - 1]).normalized(); s = t.cross(n).normalized()
        bf = []
        make_buckle(bm, p + n * 0.0030, t, s, n, bf)
        set_uv(bm, bf, None, fixed=METAL_UV)
        bmesh.ops.recalc_face_normals(bm, faces=bf)
        # buttons at both strap ends
        for ei in (1, len(pts) - 2):
            res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=8, radius1=0.0052, radius2=0.0052, depth=0.0032, calc_uvs=False,
                                        matrix=Matrix.Translation(pts[ei] + nrm[ei] * 0.0050))
            cf = list({f for v in res['verts'] for f in v.link_faces})
            rot = nrm[ei].rotation_difference(Vector((0, 0, 1))).to_matrix().inverted()
            bmesh.ops.rotate(bm, verts=res['verts'], cent=pts[ei] + nrm[ei] * 0.0050, matrix=rot)
            bmesh.ops.recalc_face_normals(bm, faces=cf)
            set_uv(bm, cf, None, fixed=METAL_UV)
        metal += bf
    bm.normal_update()
    bm.faces.ensure_lookup_table()
    # fix boxes orientation (outward from their center)
    return bm
