import bpy, bmesh, math
from mathutils import Vector, noise
import cloth
from cloth import sstep

def build_shorts(waist_z=0.640, hem_z=0.378, seed=5):
    bm = cloth.ref_bm()
    cloth.cut(bm, (0, 0, waist_z), (0, 0, 1), 'outer')
    cloth.cut(bm, (0, 0, hem_z), (0, 0, 1), 'inner')
    cloth.keep_largest(bm)
    bm.normal_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    for v in cloth.boundary_verts(bm):
        if v.co.z < 0.5:
            th = math.atan2(v.co.x, -v.co.y)
            v.co.z += -0.004 + 0.016 * (noise.noise(Vector((v.co.x * 22 + seed, v.co.y * 22, 3.1))) * 0.5 + 0.5) * -1
    for v in bm.verts:
        p = v.co; n = v.normal
        sg = 1 if p.x >= 0 else -1
        xc = sg * (0.074 + (0.598 - p.z) * 0.12)
        th = math.atan2(p.x - xc, -(p.y + 0.02))
        flare = 0.004 + 0.014 * sstep(0.56, 0.378, p.z)
        fold = 0.0035 * sstep(0.62, 0.40, p.z) * (math.sin(7 * th + 0.9 * sg) + 0.5 * math.sin(13 * th + 2.0))
        wr = 0.0025 * noise.noise(p * 33 + Vector((seed, 1, 2)))
        off = (0.0055 + flare + fold + wr) * (0.55 + 0.45 * sstep(0.64, 0.58, p.z) * 0 + 0.45 * sstep(0.645, 0.56, p.z))
        v.co = p + n * max(off, 0.0035)
    return bm
