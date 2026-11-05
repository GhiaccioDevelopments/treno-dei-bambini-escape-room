import os
ROOT = os.environ.get("TRENO_ROOT", r".")  # cartella del progetto Il Treno dei Bambini
SCRATCH = os.environ.get("TRENO_SCRATCH", os.path.join(ROOT, "ArtSource", "tmp"))
import sys, importlib, bpy, bmesh, random, math
SP = SCRATCH
if SP not in sys.path: sys.path.insert(0, SP)
import hairgen; importlib.reload(hairgen)
from mathutils import Vector

def head_bm(src_name='Amerigo_LOD0', zmin=1.0):
    src = bpy.data.objects[src_name]
    bm = bmesh.new(); bm.from_mesh(src.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != 0 or f.calc_center_median().z < zmin], context='FACES')
    bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.normal_update()
    return bm

def length_child(loc, g, rng):
    z = loc.z; az = abs(g.az(loc))
    if z > 1.17: L = 0.040
    elif az < 50: L = 0.026
    elif az < 120: L = 0.028
    else: L = 0.026
    return L * rng.uniform(0.8, 1.3)

OVER = {}
PCH = dict(center=(0, -0.04, 1.10), crown=(0, 0.0, 1.25),
  hairline={0: 1.125, 30: 1.105, 60: 1.088, 90: 1.095, 120: 1.07, 150: 1.045, 180: 1.04, -150: 1.045, -120: 1.07, -90: 1.095, -60: 1.088, -30: 1.105},
  tip_below=lambda a: 0.004 if a<70 else (0.010 if a<130 else 0.02),
  dmin=0.0155, dmin_border=0.0125, border_band=0.012, yaw_sigma=0.55, spiky_prob=0.08, lift=0.08, droop=0.55,
  off0=0.0018, off_layer=0.0026, off_len=0.0016, spiky_off=0.007, width=0.019, length=length_child, cap_off=0.0012, cap_dz=-0.003)

def make_cap_src(bm, g, tris):
    import bmesh as _bm
    b = bm.copy()
    sel = [f for f in b.faces if f.calc_center_median().z > g.hairline(f.calc_center_median()) + g.P.get('cap_in', -0.003)]
    keep = set(sel)
    _bm.ops.delete(b, geom=[f for f in b.faces if f not in keep], context='FACES')
    _bm.ops.delete(b, geom=[v for v in b.verts if not v.link_faces], context='VERTS')
    me = bpy.data.meshes.new('CapTmp'); b.to_mesh(me); b.free()
    ob = bpy.data.objects.new('CapTmp', me); bpy.context.scene.collection.objects.link(ob)
    t0 = sum(len(p.vertices) - 2 for p in me.polygons)
    md = ob.modifiers.new('d', 'DECIMATE'); md.ratio = min(1.0, tris / t0); md.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    nb = _bm.new(); nb.from_mesh(ev.to_mesh())
    nb.normal_update()
    bpy.data.objects.remove(ob); bpy.data.meshes.remove(me)
    return nb


def build_child_hair(over=None, seed=11, S=4, keep=1.0, widen=1.0, name='HairNew_LOD0', ncand=14000, cap_tris=380):
    P = dict(PCH); P['ncand'] = ncand; P.update(OVER)
    if over: P.update(over)
    bm = head_bm()
    g = hairgen.HairGen(bm, P)
    if cap_tris:
        g.cap_src = make_cap_src(bm, g, cap_tris)
    rng = random.Random(seed)
    roots = g.pick_roots(rng)
    cards = g.make_cards(roots, rng)
    if keep < 1.0:
        cards = [c for i, c in enumerate(cards) if (i * 0.6180339) % 1.0 < keep]
    o = g.build(cards, S=S, widen=widen, name=name)
    bm.free()
    return o, len(cards)

def setup_hair_obj(o, mat_name='Amerigo_Hair_v4', arm='Amerigo_Armature'):
    a = bpy.data.objects[arm]
    o.data.materials.clear(); o.data.materials.append(bpy.data.materials[mat_name])
    o.parent = a
    for m in list(o.modifiers): o.modifiers.remove(m)
    for vg in list(o.vertex_groups): o.vertex_groups.remove(vg)
    vg = o.vertex_groups.new(name='head'); vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    md = o.modifiers.new('Armature', 'ARMATURE'); md.object = a

def regen_child(S=3, over2=None):
    import hairtex; importlib.reload(hairtex)
    T = os.path.join(ROOT, *['ArtSource', 'Characters', 'textures_v4'])
    hairtex.make_atlas(T + r"\Amerigo_Hair", palR=[(34,24,17),(60,41,28),(84,58,40)], palL=[(62,30,18),(106,52,28),(134,72,40)], seed=7, nstr=11)
    for n in ('Amerigo_Hair_albedo.png', 'Amerigo_Hair_normal.png'): bpy.data.images[n].reload()
    old = bpy.data.objects.get('HairNew_LOD0')
    if old: bpy.data.objects.remove(old)
    ov = dict(length=lambda loc, g, rng: (0.040 if loc.z > 1.17 else 0.030) * rng.uniform(0.6, 1.4), width=0.0145, dmin=0.0125, dmin_border=0.0100)
    if over2: ov.update(over2)
    o, n = build_child_hair(S=S, over=ov, ncand=26000)
    setup_hair_obj(o)
    return o, n
