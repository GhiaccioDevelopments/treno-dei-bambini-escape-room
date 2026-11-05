import bpy, bmesh, math
from mathutils import Vector, noise
import cloth
from cloth import sstep

SH_L = Vector((0.124, -0.009, 0.914)); EL_L = Vector((0.235, -0.011, 0.791))

def arm_t(p):
    s = SH_L.copy(); e = EL_L.copy()
    if p.x < 0:
        s.x = -s.x; e.x = -e.x
    ax = (e - s); L = ax.length; ax.normalize()
    return (p - s).dot(ax) / L

def build_shirt(sleeve_t=0.78, hem_z=0.585, neck_z=0.955, target_tris=2300, seed=3):
    bm = cloth.ref_bm()
    cloth.cut(bm, (0, 0, hem_z), (0, 0, 1), 'inner')
    cloth.cut(bm, (0, -0.02, neck_z), Vector((0, 0.5, -1)).normalized(), 'inner')
    for sgn in (1, -1):
        s = Vector((SH_L.x * sgn, SH_L.y, SH_L.z)); e = Vector((EL_L.x * sgn, EL_L.y, EL_L.z))
        ax = (e - s).normalized(); P = s + (e - s) * sleeve_t
        cloth.split_by_plane(bm, P, ax)
        cloth.delete_faces_where(bm, lambda c, ax=ax, P=P, sgn=sgn: (c - P).dot(ax) > 0 and abs(c.x) > 0.19)
    cloth.keep_largest(bm)
    bm.normal_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    # boundary irregularity: gentle low-frequency drop at hem/cuffs
    bset = cloth.boundary_verts(bm)
    for v in bset:
        th = math.atan2(v.co.x, -v.co.y)
        if v.co.z < 0.62 and abs(v.co.x) < 0.2:
            v.co.z -= 0.006 + 0.012 * (noise.noise(Vector((math.cos(th) * 1.6 + seed, math.sin(th) * 1.6, 0.7))) * 0.5 + 0.5)
        elif abs(v.co.x) >= 0.19:
            ax = Vector((1, 0, -0.9)).normalized(); ax.x *= (1 if v.co.x > 0 else -1)
            v.co += ax * (0.008 * noise.noise(Vector((v.co.y * 40 + seed, v.co.z * 40, v.co.x * 25))))
        elif v.co.z > 0.93:
            v.co.z += 0.004 * noise.noise(Vector((math.cos(th) * 2.2 + seed, math.sin(th) * 2.2, 1.7)))
    # offset along normals with baggy/fold shaping
    for v in bm.verts:
        p = v.co; n = v.normal
        th = math.atan2(p.x, -(p.y - 0.0))
        t = arm_t(p) if abs(p.x) > 0.13 else 0.0
        belly = 0.013 * sstep(0.82, 0.60, p.z) * (1.0 if n.y < 0 else 0.7)
        sleeve = 0.0
        if abs(p.x) > 0.15:
            sleeve = 0.004 + 0.010 * max(0.0, min(1.0, t))
        fold = 0.0045 * sstep(0.84, 0.59, p.z) * (math.sin(9 * th + 1.3) + 0.6 * math.sin(17 * th + 0.2))
        wr = 0.003 * noise.noise(p * 38 + Vector((seed, 0, 0)))
        off = 0.0035 + belly + sleeve + fold + wr
        off = max(off, 0.0028)
        v.co = p + n * off
    return bm
