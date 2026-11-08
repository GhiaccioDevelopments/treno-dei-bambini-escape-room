"""
R020 - Modelli degli oggetti degli enigmi (panno con pieghe, ex-voto sbalzati a piede e cuore, cornice, cassetta di latta, forma di legno del calzolaio,
lucchetto, scarpa d'epoca). Originali, fatti a mano in Blender (nessun asset esterno).

Convenzione: ogni modello e' normalizzato in un cubo unitario [-0,5; 0,5] su ogni asse (la scarpa mantiene le proporzioni: lunghezza 1 su X),
fronte verso -Z, su verso +Y. In Unity la parte dell'enigma (sx, sy, sz in metri) lo porta alla misura vera (ParteDef.modello).

Uso (headless):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ArtSource/Models/Enigmi/genera_enigmi.py -- Assets/Art/Models/Enigmi
"""
import bpy, bmesh, math, sys, os, random
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "Enigmi"
os.makedirs(OUT, exist_ok=True)
random.seed(1946)


def pulisci():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for blocco in (bpy.data.meshes, bpy.data.metaballs):
        for b in list(blocco):
            blocco.remove(b)


def attiva(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o


def da_metaball(nome, elementi, soglia=0.05, risoluzione=0.012):
    """elementi: (tipo, x, y, z, rx, ry, rz, sx, sy, sz) -> mesh unica"""
    mb = bpy.data.metaballs.new(nome)
    mb.resolution = risoluzione
    mb.render_resolution = risoluzione
    mb.threshold = soglia
    for el in elementi:
        t, x, y, z, sx, sy, sz = el[:7]
        raggio = el[7] if len(el) > 7 else 1.0
        e = mb.elements.new(type=t)
        e.co = (x, y, z)
        e.radius = raggio
        e.stiffness = 3.0
        if t != "BALL":
            e.size_x, e.size_y, e.size_z = sx, sy, sz
    o = bpy.data.objects.new(nome, mb)
    bpy.context.collection.objects.link(o)
    attiva(o)
    bpy.ops.object.convert(target="MESH")
    return bpy.context.view_layer.objects.active


def normalizza(o, uniforme=False, orient=None):
    attiva(o)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    xs = [v.co.x for v in o.data.vertices]; ys = [v.co.y for v in o.data.vertices]; zs = [v.co.z for v in o.data.vertices]
    c = Vector(((max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, (max(zs) + min(zs)) / 2))
    d = Vector((max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)))
    for v in o.data.vertices:
        v.co -= c
        if uniforme:
            f = 1.0 / max(d.x, 1e-6)
            v.co *= f
        else:
            v.co.x /= max(d.x, 1e-6); v.co.y /= max(d.y, 1e-6); v.co.z /= max(d.z, 1e-6)
    o.data.update()
    for p in o.data.polygons:
        p.use_smooth = True


def scambia_yz(o):
    """Disegnato con Y alto e Z spessore (comodo); Unity vuole alto = Z di Blender e spessore = Y: scambio gli assi e rigiro le facce."""
    bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:
        v.co = Vector((v.co.x, v.co.z, v.co.y))
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.to_mesh(o.data); bm.free()


def esporta(o, nome):
    attiva(o)
    modificatore(o, "TRIANGULATE", quad_method="BEAUTY", ngon_method="BEAUTY")
    o.name = nome
    o.data.name = nome
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, nome + ".fbx"), use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
                             global_scale=1.0, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", path_mode="STRIP")
    print("ESPORTATO", nome, len(o.data.polygons), "poligoni")


def modificatore(o, tipo, **kw):
    attiva(o)
    m = o.modifiers.new(tipo, tipo)
    for k, v in kw.items():
        setattr(m, k, v)
    bpy.ops.object.modifier_apply(modifier=m.name)


# --------------------------------------------------------------------------------------------- panno con pieghe
def panno():
    bm = bmesh.new()
    nx, ny = 30, 40
    vs = []
    for j in range(ny + 1):
        riga = []
        for i in range(nx + 1):
            u, v = i / nx - 0.5, j / ny - 0.5
            # pieghe verticali che si stringono verso le mollette (in alto), un po' di peso in basso
            piega = math.sin((u * 3.2 + 0.3 * math.sin(v * 2)) * math.pi * 2) * (0.5 - 0.35 * (v + 0.5)) + 0.25 * math.sin(u * 17 + v * 3)
            z = -0.07 * piega * (0.55 + 0.45 * (0.5 - v)) + 0.015 * math.sin(v * 9 + u * 5)
            bordo = 0.012 * math.sin(u * 40) * max(0.0, -v - 0.4) * 6
            riga.append(bm.verts.new((u, v + bordo, z)))
        vs.append(riga)
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("panno"); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new("panno", me); bpy.context.collection.objects.link(o)
    modificatore(o, "SOLIDIFY", thickness=0.012, offset=0.0)
    scambia_yz(o)
    normalizza(o)
    esporta(o, "panno")


# --------------------------------------------------------------------------------------------- ex-voto a piede e a cuore (sbalzati)
def cilindro_piatto(x, y, rx, ry, spessore=0.05):
    bpy.ops.mesh.primitive_cylinder_add(vertices=40, radius=1.0, depth=1.0, location=(x, y, 0))
    c = bpy.context.view_layer.objects.active
    c.scale = (rx, ry, spessore)
    attiva(c); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return c


def exvoto_piede():
    # sagoma vista dall'alto come unione di ellissi piatte (tallone, arco, pianta, cinque dita), bordo smussato
    parti = [(0.0, -0.33, 0.115, 0.14), (0.015, -0.1, 0.10, 0.2), (0.02, 0.17, 0.165, 0.13)]
    dita = [(-0.125, 0.33, 0.062), (-0.04, 0.395, 0.052), (0.04, 0.395, 0.048), (0.11, 0.37, 0.044), (0.165, 0.32, 0.038)]
    base = cilindro_piatto(*parti[0])
    for x, y, rx, ry in parti[1:]:
        c = cilindro_piatto(x, y, rx, ry)
        attiva(base); m = base.modifiers.new("u", "BOOLEAN"); m.operation = "UNION"; m.object = c; m.solver = "EXACT"
        bpy.ops.object.modifier_apply(modifier=m.name); bpy.data.objects.remove(c)
    for x, y, r in dita:
        c = cilindro_piatto(x, y, r, r * 1.15)
        attiva(base); m = base.modifiers.new("u", "BOOLEAN"); m.operation = "UNION"; m.object = c; m.solver = "EXACT"
        bpy.ops.object.modifier_apply(modifier=m.name); bpy.data.objects.remove(c)
    o = base
    modificatore(o, "BEVEL", width=0.012, segments=3, limit_method="ANGLE", angle_limit=math.radians(25))
    # sbalzo: il campo e' leggermente incavato rispetto al bordo
    bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:
        if v.co.z < 0 and (abs(v.co.x) < 0.07 and -0.3 < v.co.y < 0.2):
            v.co.z *= 0.8
    bm.to_mesh(o.data); bm.free()
    print("PIEDE poligoni", len(o.data.polygons))
    scambia_yz(o)
    normalizza(o)
    esporta(o, "exvoto_piede")


def exvoto_cuore():
    el = [("BALL", -0.16, 0.12, 0.0, 0.5, 0.5, 0.2), ("BALL", 0.16, 0.12, 0.0, 0.5, 0.5, 0.2), ("ELLIPSOID", 0.0, -0.12, 0.0, 0.42, 0.52, 0.2), ("BALL", 0.0, -0.3, 0.0, 0.2, 0.28, 0.2)]
    o = da_metaball("exvoto_cuore", el, soglia=0.28, risoluzione=0.02)
    attiva(o)
    modificatore(o, "SMOOTH", factor=0.6, iterations=3)
    modificatore(o, "DECIMATE", ratio=0.03)
    scambia_yz(o)
    normalizza(o)
    esporta(o, "exvoto_cuore")


# --------------------------------------------------------------------------------------------- scatole con spigoli smussati
def scatola(nome, bevel=0.03, segmenti=3, extra=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.view_layer.objects.active
    o.name = nome
    if extra:
        extra(o)
    modificatore(o, "BEVEL", width=bevel, segments=segmenti, limit_method="ANGLE")
    return o


def cornice():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.view_layer.objects.active
    bm = bmesh.new(); bm.from_mesh(o.data)
    fronte = [f for f in bm.faces if f.normal.y < -0.9]
    r = bmesh.ops.inset_individual(bm, faces=fronte, thickness=0.14, depth=0.0)
    f2 = [f for f in bm.faces if f.normal.y < -0.9 and f.calc_area() < 0.7]
    for v in {v for f in f2 for v in f.verts}:
        v.co.y += 0.12
    bm.to_mesh(o.data); bm.free()
    modificatore(o, "BEVEL", width=0.02, segments=2, limit_method="ANGLE")
    o.name = "cornice"
    normalizza(o)
    esporta(o, "cornice")


def cassetta():
    # base + coperchio con bordo sporgente e due nervature, come una scatola di latta
    parti = []
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, -0.18)); a = bpy.context.view_layer.objects.active; a.scale = (1.0, 0.62, 1.0); parti.append(a)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.30)); b = bpy.context.view_layer.objects.active; b.scale = (1.04, 0.66, 0.22); parti.append(b)
    for x in (-0.3, 0.3):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x, 0, 0.42)); n = bpy.context.view_layer.objects.active; n.scale = (0.03, 0.55, 0.04); parti.append(n)
    for p in parti:
        attiva(p); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        modificatore(p, "BEVEL", width=0.02, segments=2, limit_method="ANGLE")
    bpy.ops.object.select_all(action="DESELECT")
    for p in parti: p.select_set(True)
    bpy.context.view_layer.objects.active = parti[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    normalizza(o)
    esporta(o, "cassetta")


def forma_legno():
    # forma da calzolaio: piede di legno lungo X con tallone alzato e punta tonda
    el = [("ELLIPSOID", 0.28, 0.0, 0.0, 0.34, 0.14, 0.12), ("ELLIPSOID", -0.05, 0.0, 0.02, 0.30, 0.12, 0.10), ("BALL", -0.32, 0.0, 0.07, 0.17, 0.17, 0.17),
          ("ELLIPSOID", -0.42, 0.0, 0.0, 0.14, 0.14, 0.14)]
    o = da_metaball("forma_legno", el, soglia=0.3, risoluzione=0.015)
    attiva(o)
    modificatore(o, "SMOOTH", factor=0.5, iterations=3)
    modificatore(o, "DECIMATE", ratio=0.03)
    normalizza(o)
    esporta(o, "forma_legno")


def lucchetto():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    c = bpy.context.view_layer.objects.active
    c.scale = (1.0, 0.5, 0.72)
    attiva(c); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    modificatore(c, "BEVEL", width=0.14, segments=4, limit_method="ANGLE")
    bpy.ops.mesh.primitive_torus_add(major_radius=0.33, minor_radius=0.065, major_segments=24, minor_segments=10, location=(0, 0, 0.36), rotation=(0, 0, 0))
    t = bpy.context.view_layer.objects.active
    t.rotation_euler = (math.radians(90), 0, 0)
    t.scale = (1.0, 1.0, 1.0)
    attiva(t); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # taglio la meta' bassa dell'anello (resta l'arco)
    bm = bmesh.new(); bm.from_mesh(t.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < 0.36 - 0.02], context="VERTS")
    bm.to_mesh(t.data); bm.free()
    bpy.ops.object.select_all(action="DESELECT")
    c.select_set(True); t.select_set(True)
    bpy.context.view_layer.objects.active = c
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    normalizza(o)
    esporta(o, "lucchetto")


# --------------------------------------------------------------------------------------------- scarpa d'epoca (un solo paio, proporzioni vere)
def scarpa():
    """Scarpa bassa da uomo: sezioni ellittiche lungo X (tallone -> punta), collo aperto, suola con tacchetto. Lunghezza normalizzata a 1 su X."""
    sezioni = [  # x, mezza larghezza, altezza tomaia, centro z
        (-0.50, 0.00, 0.00), (-0.47, 0.16, 0.30), (-0.40, 0.22, 0.36), (-0.25, 0.20, 0.34), (-0.10, 0.20, 0.26), (0.05, 0.24, 0.20),
        (0.20, 0.27, 0.17), (0.34, 0.26, 0.15), (0.43, 0.20, 0.11), (0.49, 0.10, 0.07), (0.50, 0.00, 0.04),
    ]
    bm = bmesh.new()
    anelli = []
    N = 14
    for (x, w, h) in sezioni:
        anello = []
        for k in range(N):
            a = math.pi * 2 * k / N
            y = math.cos(a) * w
            z = 0.06 + (math.sin(a) * 0.5 + 0.5) * h if math.sin(a) > -0.1 else 0.06 + (math.sin(a) * 0.5 + 0.5) * h * 0.2
            anello.append(bm.verts.new((x, y, z)))
        anelli.append(anello)
    for i in range(len(anelli) - 1):
        for k in range(N):
            bm.faces.new((anelli[i][k], anelli[i][(k + 1) % N], anelli[i + 1][(k + 1) % N], anelli[i + 1][k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("scarpa_tomaia"); bm.to_mesh(me); bm.free()
    tomaia = bpy.data.objects.new("scarpa_tomaia", me); bpy.context.collection.objects.link(tomaia)
    modificatore(tomaia, "SUBSURF", levels=2, render_levels=2)
    # suola: sagoma piatta e tacco
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.02, 0, 0.03)); s = bpy.context.view_layer.objects.active
    s.scale = (0.98, 0.5, 0.06)
    attiva(s); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    modificatore(s, "BEVEL", width=0.025, segments=3, limit_method="ANGLE")
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-0.40, 0, -0.02)); t = bpy.context.view_layer.objects.active
    t.scale = (0.2, 0.42, 0.06)
    attiva(t); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    modificatore(t, "BEVEL", width=0.02, segments=2, limit_method="ANGLE")
    bpy.ops.object.select_all(action="DESELECT")
    for p in (tomaia, s, t): p.select_set(True)
    bpy.context.view_layer.objects.active = tomaia
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    # Z su (come l'asse Blender) -> Y su in Unity: lo gestisce l'export (axis_up=Y)
    normalizza(o, uniforme=True)
    esporta(o, "scarpa")


if __name__ == "__main__":
    for f in (panno, exvoto_piede, exvoto_cuore, cornice, cassetta, forma_legno, lucchetto, scarpa):
        pulisci()
        f()
