"""
R023 - Scarpe d'epoca consumate (oggetto "Le scarpe"), modello originale fatto a mano in Blender.
Una scarpa bassa di cuoio da uomo, anni '40: tomaia con collo aperto, linguetta, lacci sciatti, suola con tacco e pianta, tutto un po' accasciato.
Un paio: la sinistra in piedi, la destra rovesciata su un fianco. Circa 4000 triangoli in tutto.

Uso (headless):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ArtSource/Models/Scarpe/genera_scarpe.py -- \
      Assets/Art/Models/Scarpe/Scarpe.fbx ArtSource/Models/Scarpe/Scarpe.blend
Slot materiali (rimappati in Unity su M_Cuoio_Scarpe, M_Suola_Scarpa, M_Lacci_Scarpe): Cuoio, Suola, Lacci.
Asse: Y avanti (punta), Z su; esportato in metri.
"""
import bpy, bmesh, math, random, sys
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
out_fbx = argv[0] if len(argv) > 0 else "Scarpe.fbx"
out_blend = argv[1] if len(argv) > 1 else None
random.seed(1946)

L = 0.27          # lunghezza della scarpa (m)
BASE = 0.022      # quota superiore della suola


def sstep(a, b, x):
    s = max(0.0, min(1.0, (x - a) / (b - a)))
    return s * s * (3 - 2 * s)


def halfw(t):
    if t <= 0.0 or t >= 1.0:
        return 0.0
    w = 0.034 + 0.017 * math.sin(min(t / 0.68, 1.0) * math.pi / 2)
    if t > 0.68:
        u = (t - 0.68) / 0.32
        w = 0.051 * math.sqrt(max(0.0, 1 - u ** 2.3))
    if t < 0.07:
        w *= math.sqrt(t / 0.07)
    return w


def height(t):
    s = sstep(0.78, 0.30, t)
    h = 0.030 + 0.056 * s
    if t > 0.9:
        h *= max(0.35, 1 - (t - 0.9) * 5)
    return h


mats = {}
for name, col in (("Cuoio", (0.22, 0.12, 0.07, 1)), ("Suola", (0.09, 0.07, 0.06, 1)), ("Lacci", (0.05, 0.04, 0.03, 1))):
    m = bpy.data.materials.new(name)
    m.diffuse_color = col
    mats[name] = m


def new_obj(name, bm, matname):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(mats[matname])
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


def outline(t0, t1, n, margin):
    pts = []
    for i in range(n + 1):
        t = t0 + (t1 - t0) * i / n
        pts.append((halfw(t) + margin, t * L))
    for i in range(n, -1, -1):
        t = t0 + (t1 - t0) * i / n
        pts.append((-(halfw(t) + margin), t * L))
    return pts


def slab(name, t0, t1, z0, z1, matname, n=14, margin=0.003):
    bm = bmesh.new()
    pts = outline(t0, t1, n, margin)
    clean = []
    for p in pts:
        if not clean or (abs(p[0] - clean[-1][0]) > 1e-5 or abs(p[1] - clean[-1][1]) > 1e-5):
            clean.append(p)
    if abs(clean[0][0] - clean[-1][0]) < 1e-5 and abs(clean[0][1] - clean[-1][1]) < 1e-5:
        clean.pop()
    vs = [bm.verts.new((x, y, z0)) for x, y in clean]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    verts = [e for e in r["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=verts, vec=(0, 0, z1 - z0))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    return new_obj(name, bm, matname)


def upper():
    bm = bmesh.new()
    K = 18
    N = 20
    rings = []
    ts = [0.02 + (0.97 - 0.02) * i / (N - 1) for i in range(N)]
    for t in ts:
        w = halfw(t) * 0.995
        h = height(t)
        ring = []
        for k in range(K + 1):
            a = math.pi * k / K
            x = w * math.cos(a)
            z = BASE + h * (math.sin(a) ** 0.8)
            ring.append(bm.verts.new((x, t * L, z)))
        rings.append(ring)
    for i in range(N - 1):
        for k in range(K):
            bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
    for idx, yoff, flip in ((0, -0.004, True), (N - 1, 0.004, False)):
        ring = rings[idx]
        t = ts[idx]
        c = bm.verts.new((0, t * L + yoff, BASE + height(t) * 0.45))
        for k in range(K):
            tri = (ring[k], ring[k + 1], c) if not flip else (ring[k + 1], ring[k], c)
            bm.faces.new(tri)
    kill = []
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        c = f.calc_center_median()
        t = c.y / L
        w = halfw(t)
        if 0.16 < t < 0.54 and abs(c.x) < 0.72 * w and c.z > BASE + 0.5 * height(t):
            kill.append(f)
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    for v in bm.verts:
        t = v.co.y / L
        slump = 0.006 * sstep(0.1, 0.3, t) * (1 - sstep(0.3, 0.55, t)) * (v.co.z - BASE) / 0.08
        v.co.z -= slump
        v.co.x += random.gauss(0, 0.0007)
        v.co.y += random.gauss(0, 0.0007)
        v.co.z += random.gauss(0, 0.0009)
        if t > 0.8:
            v.co.z += 0.004 * sstep(0.8, 1.0, t)
    ob = new_obj("Tomaia", bm, "Cuoio")
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    mod = ob.modifiers.new("Spessore", "SOLIDIFY")
    mod.thickness = 0.0035
    mod.offset = 1.0
    bpy.ops.object.modifier_apply(modifier="Spessore")
    ob.select_set(False)
    return ob


def lacci_e_linguetta():
    objs = []
    for j in range(4):
        t = 0.54 + 0.045 * j
        w = halfw(t)
        h = height(t)
        bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.0022, depth=w * 1.55,
                                            location=(0, t * L, BASE + h * 0.97 + 0.003),
                                            rotation=(0, math.pi / 2, random.uniform(-0.25, 0.25)))
        o = bpy.context.active_object
        o.name = "Laccio_%d" % j
        o.data.materials.clear()
        o.data.materials.append(mats["Lacci"])
        objs.append(o)
    for s in (-1, 1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.002, depth=0.03,
                                            location=(s * 0.014, 0.74 * L, BASE + height(0.74) + 0.006),
                                            rotation=(0.5, 0.2, s * 0.9))
        o = bpy.context.active_object
        o.data.materials.clear()
        o.data.materials.append(mats["Lacci"])
        objs.append(o)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= 0.046
        v.co.y *= 0.07
        v.co.z *= 0.004
    bmesh.ops.rotate(bm, verts=list(bm.verts), cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(-14), 3, "X"))
    bmesh.ops.translate(bm, verts=list(bm.verts), vec=(0, 0.50 * L, BASE + height(0.5) * 0.95))
    o = new_obj("Linguetta", bm, "Cuoio")
    objs.append(o)
    return objs


def build_shoe(name):
    parts = [slab("SuolaPianta", 0.02, 0.985, 0.012, BASE, "Suola"),
             slab("SuolaTacco", 0.03, 0.27, 0.0, 0.012, "Suola", margin=0.0),
             slab("SuolaAnteriore", 0.50, 0.97, 0.0, 0.012, "Suola", margin=0.001),
             upper()] + lacci_e_linguetta()
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    me = ob.data
    order = {"Cuoio": 0, "Suola": 1, "Lacci": 2}
    remap = {}
    for i, m in enumerate(me.materials):
        remap[i] = order[m.name]
    for poly in me.polygons:
        poly.material_index = remap[poly.material_index]
    me.materials.clear()
    for n in ("Cuoio", "Suola", "Lacci"):
        me.materials.append(mats[n])
    for v in me.vertices:
        v.co.y -= L / 2
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    return ob


sx = build_shoe("Scarpa_Sx")
dx = sx.copy()
dx.data = sx.data.copy()
dx.name = "Scarpa_Dx"
bpy.context.collection.objects.link(dx)
bpy.ops.object.select_all(action="DESELECT")
dx.select_set(True)
bpy.context.view_layer.objects.active = dx
dx.scale.x = -1
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
bm = bmesh.new()
bm.from_mesh(dx.data)
bmesh.ops.reverse_faces(bm, faces=list(bm.faces))
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(dx.data)
bm.free()

sx.location = (-0.075, 0, 0)
sx.rotation_euler = (0, 0, math.radians(-7))
dx.rotation_euler = (0, math.radians(84), math.radians(14))
bpy.context.view_layer.update()
minz = min((dx.matrix_world @ Vector(c)).z for c in dx.bound_box)
dx.location = (0.095, 0.01, -minz)
bpy.context.view_layer.update()

tris = 0
for ob in (sx, dx):
    me = ob.data
    me.calc_loop_triangles()
    tris += len(me.loop_triangles)
print("TRIANGOLI TOTALI:", tris)

bpy.ops.object.select_all(action="DESELECT")
sx.select_set(True)
dx.select_set(True)
bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
                         global_scale=1.0, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                         bake_space_transform=True, mesh_smooth_type="FACE", path_mode="STRIP")
if out_blend:
    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
print("ESPORTATO", out_fbx)
