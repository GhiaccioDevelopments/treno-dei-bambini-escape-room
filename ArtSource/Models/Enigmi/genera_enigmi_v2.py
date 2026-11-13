"""
R020 (grafica degli enigmi, 2a passata) - Modelli veri degli oggetti degli enigmi, originali, fatti da codice in Blender.
Prima si generano le texture: python ArtSource/Models/Enigmi/texture_enigmi.py (scrive anche le mappe in _tmp/ usate qui).

Uso (headless):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ArtSource/Models/Enigmi/genera_enigmi_v2.py -- [nome ...]

Convenzioni:
- In Blender: fronte verso -Y, su = +Z, misure in metri, nel sistema della parte (o dell'enigma) del JSON: Unity x = x, Unity y = Blender z, Unity z = Blender y.
- Ogni FBX e' normalizzato in un cubo unitario centrato (come i modelli della 1a passata): in Unity la parte lo riporta alla misura vera.
  Le misure vere e il centro (coordinate Unity) finiscono in ArtSource/Models/Enigmi/modelli_misure.json: servono per le parti del JSON.
- La scarpa mantiene le proporzioni (lunghezza 1 su X, punta verso +X) come prima.
- Gli slot materiale hanno il nome del materiale Unity: ME_* (texture proprie, tenute), MT_* (tinta col colore della parte), MX_Vetro.
- Oggetti grandi (edicola, baule): due livelli di dettaglio <nome>_LOD0 / _LOD1 (Unity crea il LODGroup).
"""
import bpy, bmesh, math, os, sys, json, random
from mathutils import Vector, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Models", "Enigmi")
TMP = os.path.join(ROOT, "ArtSource", "Models", "Enigmi", "_tmp")
MISURE = os.path.join(ROOT, "ArtSource", "Models", "Enigmi", "modelli_misure.json")
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
random.seed(1946)
misure = json.load(open(MISURE)) if os.path.exists(MISURE) else {}


# ============================================================================================ utilita'
def pulisci():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes): bpy.data.meshes.remove(m)


def mat(nome):
    m = bpy.data.materials.get(nome)
    return m if m else bpy.data.materials.new(nome)


def oggetto(nome, bm):
    me = bpy.data.meshes.new(nome)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(nome, me)
    bpy.context.collection.objects.link(o)
    return o


def attiva(o):
    for x in bpy.context.scene.objects:
        if x is not None: x.select_set(False)
    o.select_set(True); bpy.context.view_layer.objects.active = o


def uv_layer(bm):
    return bm.loops.layers.uv.get("UVMap") or bm.loops.layers.uv.new("UVMap")


def uv_scatola(bm, scala, off=(0.0, 0.0), facce=None):
    """Proiezione a cubo per faccia: 1 unita' UV = 'scala' metri (texture ripetibili)."""
    uv = uv_layer(bm)
    for f in (facce or bm.faces):
        n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            c = l.vert.co
            if ax == 0: p = (c.y * (1 if n.x > 0 else -1), c.z)
            elif ax == 1: p = (c.x * (-1 if n.y > 0 else 1), c.z)
            else: p = (c.x, c.y * (1 if n.z > 0 else -1))
            l[uv].uv = (p[0] / scala + off[0], p[1] / scala + off[1])


def pezzo(nome, dim, pos, materiale, scala_uv=0.3, smusso=0.0, seg=2, rot=(0, 0, 0), forma="cubo", vert=16, off=None):
    """Scatola (o cilindro) in metri: dim = (x, y, z) Blender, pos = centro. UV a cubo con la scala del materiale."""
    bm = bmesh.new()
    if forma == "cilindro":   # dim nell'orientamento finale: riporto l'asse del cilindro (z locale) prima della rotazione
        if rot[0] in (90, -90): dim = (dim[0], dim[2], dim[1])
        elif rot[1] in (90, -90): dim = (dim[2], dim[1], dim[0])
        bmesh.ops.create_cone(bm, cap_ends=True, segments=vert, radius1=0.5, radius2=0.5, depth=1.0)
    elif forma == "sfera":
        bmesh.ops.create_uvsphere(bm, u_segments=vert, v_segments=max(4, vert // 2), radius=0.5)
    else:
        bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(dim), verts=bm.verts)
    if smusso > 0 and forma == "cubo":
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=smusso, segments=seg, affect="EDGES", profile=0.5)
    R = Matrix.Rotation(math.radians(rot[0]), 4, "X") @ Matrix.Rotation(math.radians(rot[1]), 4, "Y") @ Matrix.Rotation(math.radians(rot[2]), 4, "Z")
    bmesh.ops.transform(bm, matrix=R, verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(pos), verts=bm.verts)
    bm.normal_update()
    uv_scatola(bm, scala_uv, off or (random.random(), random.random()))
    o = oggetto(nome, bm)
    o.data.materials.append(mat(materiale))
    return o


def unisci(oggetti, nome):
    attiva(oggetti[0])
    for o in oggetti: o.select_set(True)
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = nome; o.data.name = nome
    return o


def spigoli(o, angolo=35):
    """Facce lisce con spigoli vivi oltre 'angolo' gradi."""
    bm = bmesh.new(); bm.from_mesh(o.data)
    for f in bm.faces: f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = e.calc_face_angle(0) < math.radians(angolo)
        else:
            e.smooth = False
    bm.to_mesh(o.data); bm.free()


def tri(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


def esporta(oggetti, nome, uniforme=False, lod1=None):
    """Normalizza nel cubo unitario (stessa trasformazione per tutti i pezzi/LOD), esporta l'FBX, annota misure e centro (Unity)."""
    vs = [o.matrix_world @ v.co for o in oggetti for v in o.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    c = (mn + mx) / 2; d = mx - mn
    tutti = list(oggetti) + ([lod1] if lod1 else [])
    for o in tutti:
        attiva(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        for v in o.data.vertices:
            v.co -= c
            if uniforme: v.co /= d.x
            else: v.co = Vector((v.co.x / max(d.x, 1e-6), v.co.y / max(d.y, 1e-6), v.co.z / max(d.z, 1e-6)))
            # con questi assi d'esportazione Unity vede il modello girato di 180 gradi: lo giro prima (fronte -Y Blender = fronte -Z Unity)
            v.co = Vector((-v.co.x, -v.co.y, v.co.z))
        # la normalizzazione non uniforme deforma le normali solo se le misure cambiano: Unity riporta alle misure vere
        o.data.update()
    if lod1:
        oggetti[0].name = nome + "_LOD0"; lod1.name = nome + "_LOD1"
    for x in bpy.context.scene.objects: x.select_set(False)
    for o in tutti: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, nome + ".fbx"), use_selection=True, object_types={"MESH"}, apply_unit_scale=True, global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="OFF",
                             use_mesh_modifiers=True, path_mode="STRIP", use_tspace=True)
    t = sum(tri(o) for o in oggetti)
    misure[nome] = {"misura": [round(d.x, 4), round(d.z, 4), round(d.y, 4)], "centro": [round(c.x, 4), round(c.z, 4), round(c.y, 4)], "triangoli": t,
                    "triangoli_lod1": tri(lod1) if lod1 else 0, "materiali": sorted({m.name for o in oggetti for m in o.data.materials})}
    print("ESPORTATO", nome, misure[nome])


def lod_da(o, rapporto):
    c = o.copy(); c.data = o.data.copy(); bpy.context.collection.objects.link(c)
    attiva(c)
    m = c.modifiers.new("dec", "DECIMATE"); m.ratio = rapporto; m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)
    return c


def immagine(path):
    im = bpy.data.images.load(path)
    w, h = im.size
    import numpy as np
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, im.channels)
    return px[..., 0], w, h


def campiona(arr, w, h, u, v):
    """u, v in 0..1 (v in alto) -> valore bilineare."""
    x = min(max(u * (w - 1), 0), w - 1); y = min(max(v * (h - 1), 0), h - 1)
    x0, y0 = int(x), int(y); x1, y1 = min(x0 + 1, w - 1), min(y0 + 1, h - 1); fx, fy = x - x0, y - y0
    a = arr[y0, x0] * (1 - fx) + arr[y0, x1] * fx; b = arr[y1, x0] * (1 - fx) + arr[y1, x1] * fx
    return a * (1 - fy) + b * fy


def lastra(nome, w, h, nx, ny, regione, materiale, rilievo=None, maschera=None, spessore=0.0, piega=None):
    """Lastra nel piano XZ (fronte -Y) di w x h metri, UV sulla regione (u, v, lw, lh) dell'atlante; rilievo = (array, W, H, profondita') verso -Y."""
    bm = bmesh.new(); uv = uv_layer(bm)
    vv = []
    for j in range(ny + 1):
        riga = []
        for i in range(nx + 1):
            s, t = i / nx, j / ny
            y = 0.0
            if rilievo: y -= campiona(rilievo[0], rilievo[1], rilievo[2], s, t) * rilievo[3]
            if piega: y += piega(s, t)
            riga.append(bm.verts.new(((s - 0.5) * w, y, (t - 0.5) * h)))
        vv.append(riga)
    for j in range(ny):
        for i in range(nx):
            if maschera:
                m = [campiona(maschera[0], maschera[1], maschera[2], (i + a) / nx, (j + b) / ny) for a, b in ((0, 0), (1, 0), (1, 1), (0, 1), (0.5, 0.5))]
                if max(m) < 0.03: continue
            f = bm.faces.new((vv[j][i], vv[j][i + 1], vv[j + 1][i + 1], vv[j + 1][i]))
            for l, (a, b) in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
                l[uv].uv = (regione[0] + (i + a) / nx * regione[2], regione[1] + (j + b) / ny * regione[3])
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.normal_update()
    for f in bm.faces:
        if f.normal.y > 0: f.normal_flip()
    o = oggetto(nome, bm)
    o.data.materials.append(mat(materiale))
    if spessore > 0:
        attiva(o); m = o.modifiers.new("sol", "SOLIDIFY"); m.thickness = spessore; m.offset = -1.0
        bpy.ops.object.modifier_apply(modifier=m.name)
    return o


# ============================================================================================ ex-voto d'argento sbalzati
EXV = {  # nome modello: (regione, misura w x h, profondita' del rilievo)
    "exvoto_cuore": ("cuore", 0.10, 0.10, 0.007), "exvoto_piede": ("piede", 0.11, 0.20, 0.008), "exvoto_occhi": ("occhi", 0.11, 0.06, 0.005),
    "exvoto_mano": ("mano", 0.065, 0.09, 0.005), "exvoto_gamba": ("gamba", 0.04, 0.10, 0.005)}


def exvoti():
    reg = json.load(open(os.path.join(TMP, "exvoto_regioni.json")))
    for nome, (k, w, h, prof) in EXV.items():
        pulisci()
        H = immagine(os.path.join(TMP, "exvoto_" + k + "_h.png")); M = immagine(os.path.join(TMP, "exvoto_" + k + "_m.png"))
        # le immagini hanno la riga 0 in basso (Blender): v = t
        n = 34
        nx, ny = (n, max(4, int(n * h / w))) if w >= h else (max(4, int(n * w / h)), n)
        o = lastra(nome, w, h, nx, ny, reg[k], "ME_ExVoto", rilievo=(H[0], H[1], H[2], prof), maschera=(M[0], M[1], M[2]), spessore=0.0008)
        spigoli(o, 70)
        esporta([o], nome)


# ============================================================================================ edicola votiva (teca con vetro)
def edicola():
    pulisci()
    P = []
    fx0, fx1, fz0, fz1 = -0.315, 0.315, -0.225, 0.205   # campo di damasco (dove stanno gli ex-voto)
    yv = -0.025                                          # superficie del damasco (come il vecchio velluto)
    larg = 0.05; fronte = -0.13
    P.append(pezzo("schiena", (0.74, 0.02, 0.5), (0, 0.005, -0.01), "ME_LegnoNoce", 0.25))
    dam = lastra("damasco", fx1 - fx0, fz1 - fz0, 2, 2, (0, 0, (fx1 - fx0) / 0.15, (fz1 - fz0) / 0.15), "ME_Damasco")
    dam.location = (0, yv, (fz0 + fz1) / 2); P.append(dam)
    zc = (fz0 + fz1) / 2; hz = fz1 - fz0
    # cassa: quattro lati di legno dalla schiena al vetro
    prof = -fronte + 0.005
    for nome, dim, pos in (("lato_sx", (larg, prof, hz + 2 * larg), (fx0 - larg / 2, fronte / 2, zc)),
                           ("lato_dx", (larg, prof, hz + 2 * larg), (fx1 + larg / 2, fronte / 2, zc)),
                           ("sopra", (fx1 - fx0 + 2 * larg, prof, larg), (0, fronte / 2, fz1 + larg / 2)),
                           ("sotto", (fx1 - fx0 + 2 * larg, prof, larg), (0, fronte / 2, fz0 - larg / 2))):
        P.append(pezzo(nome, dim, pos, "ME_LegnoNoce", 0.25, smusso=0.004))
    # cornice frontale modanata: listello esterno, gola, filetto dorato interno
    for k, (lw, ld, yy, m) in enumerate(((0.064, 0.018, fronte - 0.004, "ME_LegnoNoce"), (0.03, 0.012, fronte - 0.016, "ME_LegnoNoce"), (0.012, 0.01, fronte - 0.008, "ME_Ottone"))):
        ins = {0: -0.007, 1: 0.012, 2: 0.0}[k]
        x0, x1, z0, z1 = fx0 - ins, fx1 + ins, fz0 - ins, fz1 + ins
        if k == 2: x0, x1, z0, z1 = fx0 + 0.004, fx1 - 0.004, fz0 + 0.004, fz1 - 0.004
        for nome, dim, pos in (("cx%d" % k, (lw, ld, z1 - z0 + lw), (x0 - lw / 2 + (lw if k == 2 else 0), yy, (z0 + z1) / 2)),
                               ("dx%d" % k, (lw, ld, z1 - z0 + lw), (x1 + lw / 2 - (lw if k == 2 else 0), yy, (z0 + z1) / 2)),
                               ("tx%d" % k, (x1 - x0 + 2 * lw, ld, lw), (0, yy, z1 + lw / 2 - (lw if k == 2 else 0))),
                               ("bx%d" % k, (x1 - x0 + 2 * lw, ld, lw), (0, yy, z0 - lw / 2 + (lw if k == 2 else 0)))):
            P.append(pezzo(nome, dim, pos, m, 0.25 if m != "ME_Ottone" else 0.08, smusso=0.003 if k < 2 else 0.004, seg=2))
    vetro = lastra("vetro", fx1 - fx0 + 0.01, hz + 0.01, 1, 1, (0, 0, 1, 1), "MX_Vetro")
    vetro.location = (0, fronte + 0.006, zc); P.append(vetro)
    # cornicione con tettuccio e frontone ad arco con la croce
    P.append(pezzo("cornicione", (0.84, 0.17, 0.035), (0, -0.07, fz1 + larg + 0.0175), "ME_LegnoNoce", 0.25, smusso=0.006))
    P.append(pezzo("cornicione2", (0.8, 0.15, 0.02), (0, -0.065, fz1 + larg + 0.045), "ME_LegnoNoce", 0.25, smusso=0.005))
    bm = bmesh.new()
    pts = [(math.cos(a) * 0.24, 0, fz1 + larg + 0.055 + math.sin(a) * 0.11) for a in [math.pi * i / 16 for i in range(17)]]
    vs = [bm.verts.new(p) for p in pts]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f]); bmesh.ops.translate(bm, vec=(0, 0.022, 0), verts=[v for v in r["geom"] if isinstance(v, bmesh.types.BMVert)])
    bmesh.ops.translate(bm, vec=(0, -0.13, 0), verts=bm.verts)
    bm.normal_update(); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv_scatola(bm, 0.25)
    fr = oggetto("frontone", bm); fr.data.materials.append(mat("ME_LegnoNoce")); P.append(fr)
    ztop = fz1 + larg + 0.055 + 0.11
    P.append(pezzo("croce_v", (0.012, 0.012, 0.08), (0, -0.12, ztop + 0.04), "ME_Ottone", 0.08, smusso=0.002))
    P.append(pezzo("croce_o", (0.05, 0.012, 0.012), (0, -0.12, ztop + 0.052), "ME_Ottone", 0.08, smusso=0.002))
    # mensola di marmo con due lumini (vetro rosso, cera)
    zs = fz0 - larg
    P.append(pezzo("mensola", (0.82, 0.2, 0.03), (0, -0.09, zs - 0.015), "ME_Marmo", 0.4, smusso=0.006))
    for x in (-0.24, 0.24):
        P.append(pezzo("lumino" + str(x), (0.05, 0.05, 0.055), (x, -0.155, zs + 0.0275), "ME_VetroRosso", 0.1, forma="cilindro", vert=14))
        P.append(pezzo("cera" + str(x), (0.04, 0.04, 0.01), (x, -0.155, zs + 0.05), "ME_Cera", 0.1, forma="cilindro", vert=12))
        P.append(pezzo("stoppino" + str(x), (0.003, 0.003, 0.012), (x, -0.155, zs + 0.06), "ME_Ferro", 0.1, forma="cilindro", vert=6))
    for x in (-0.33, 0.33):
        P.append(pezzo("staffa" + str(x), (0.015, 0.16, 0.012), (x, -0.08, zs - 0.036), "ME_Ferro", 0.3, smusso=0.002))
    o = unisci(P, "edicola")
    spigoli(o, 40)
    l1 = lod_da(o, 0.45)
    esporta([o], "edicola", lod1=l1)


# ============================================================================================ cassetta di latta litografata
def regione_uv(bm, facce, mappa):
    uv = uv_layer(bm)
    for f in facce:
        for l in f.loops:
            l[uv].uv = mappa(l.vert.co, f.normal)


def scatola_latta(dim, regioni, nome, nastro_fronte=None, ammacca=3, divisioni=(10, 4, 8)):
    """Scatola di latta: cubo suddiviso (per le ammaccature), spigoli verticali tondi, UV sulle regioni dell'atlante."""
    X, Y, Z = dim
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    bm.free()
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=6, use_grid_fill=True)
    bmesh.ops.scale(bm, vec=(X, Y, Z), verts=bm.verts)
    r = random.Random(hash(nome) % 1000)
    for _ in range(ammacca):  # ammaccature su fronte e fianchi
        lato = r.choice([(-1, 1), (1, 1), (-1, 0), (1, 0)])
        ax = lato[1]; sgn = lato[0]
        cen = Vector((r.uniform(-X / 3, X / 3), r.uniform(-Y / 3, Y / 3), r.uniform(-Z / 4, Z / 4)))
        cen[ax] = sgn * dim[ax] / 2
        for v in bm.verts:
            d = (v.co - cen).length
            if abs(v.co[ax] - cen[ax]) < 1e-4 and d < 0.04:
                v.co[ax] -= sgn * 0.004 * (1 - d / 0.04) ** 2
    bmesh.ops.bevel(bm, geom=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 1e-5 and abs(e.verts[0].co.x - e.verts[1].co.x) < 1e-5 and abs(e.verts[0].co.y - e.verts[1].co.y) < 1e-5
                              and abs(abs(e.verts[0].co.x) - X / 2) < 1e-5 and abs(abs(e.verts[0].co.y) - Y / 2) < 1e-5], offset=0.008, segments=3, affect="EDGES")
    bm.normal_update()
    per = 2 * (X + Y)
    def mappa(c, n):
        ax = max(range(3), key=lambda i: abs(n[i]))
        if ax == 2:
            u0, v0, w, h = regioni["sopra"] if n.z > 0 else regioni["sotto"]
            return (u0 + (c.x / X + 0.5) * w, v0 + (c.y / Y * (-1 if n.z > 0 else 1) + 0.5) * h)
        # giro dei lati partendo dallo spigolo anteriore sinistro: fronte (-Y), destra (+X), retro (+Y), sinistra (-X)
        if "giro" in regioni:
            if ax == 1 and n.y < 0: s = c.x + X / 2
            elif ax == 0 and n.x > 0: s = X + (c.y + Y / 2)
            elif ax == 1: s = X + Y + (X / 2 - c.x)
            else: s = 2 * X + Y + (Y / 2 - c.y)
            u0, v0, w, h = regioni["giro"]
            return (u0 + s / per * w, v0 + (c.z / Z + 0.5) * h)
        if ax == 1:
            u0, v0, w, h = regioni["fronte_retro"]
            meta = 0 if n.y < 0 else 0.5
            return (u0 + (meta + ((c.x if n.y < 0 else -c.x) / X + 0.5) * 0.5) * w, v0 + (c.z / Z + 0.5) * h)
        u0, v0, w, h = regioni["fianchi"]
        meta = 0 if n.x > 0 else 0.5
        return (u0 + (meta + ((c.y if n.x > 0 else -c.y) / Y + 0.5) * 0.5) * w, v0 + (c.z / Z + 0.5) * h)
    regione_uv(bm, bm.faces, mappa)
    o = oggetto(nome, bm)
    o.data.materials.append(mat("ME_Latta"))
    return o


def cassetta():
    reg = json.load(open(os.path.join(TMP, "latta_regioni.json")))
    nudo = reg["nudo"]
    # corpo: 0,3 x 0,2 x 0,1 (Unity y 0..0,1)
    pulisci()
    corpo = scatola_latta((0.3, 0.2, 0.1), {"fronte_retro": reg["fronte_retro"], "fianchi": reg["fianchi"], "sopra": nudo, "sotto": nudo}, "corpo", ammacca=3)
    corpo.location = (0, 0, 0.05)
    P = [corpo]
    # piastra d'ottone della serratura a rotelle, con quattro viti, e la finestra scura delle rotelle
    P.append(pezzo("piastra", (0.27, 0.006, 0.072), (0.005, -0.103, 0.05), "ME_Ottone", 0.08, smusso=0.002))
    P.append(pezzo("finestra", (0.22, 0.004, 0.056), (-0.0, -0.106, 0.05), "ME_Ferro", 0.2, smusso=0.001))
    for x, z in ((-0.125, 0.022), (0.135, 0.022), (-0.125, 0.078), (0.135, 0.078)):
        P.append(pezzo("vite", (0.008, 0.004, 0.008), (x, -0.1068, z), "ME_Ottone", 0.05, forma="cilindro", vert=8, rot=(90, 0, 0)))
    # orlo arrotolato in basso
    P.append(pezzo("orlo", (0.302, 0.202, 0.006), (0, 0, 0.003), "ME_Ferro", 0.2, smusso=0.0025))
    o = unisci(P, "cassetta_corpo"); spigoli(o, 40)
    esporta([o], "cassetta_corpo")
    # coperchio: 0,31 x 0,21 x 0,03 con la fascia (etichetta di carta sul davanti)
    pulisci()
    cop = scatola_latta((0.31, 0.21, 0.03), {"giro": reg["fascia"], "sopra": reg["coperchio"], "sotto": nudo}, "coperchio", ammacca=1)
    P = [cop]
    # bordo sbalzato attorno al disegno
    P.append(pezzo("bordino", (0.29, 0.19, 0.003), (0, 0, 0.0155), "ME_Latta", 9.0, smusso=0.0012, off=(reg["fascia"][0] + 0.5, reg["fascia"][1] + 0.01)))
    o = unisci(P, "cassetta_coperchio"); spigoli(o, 40)
    esporta([o], "cassetta_coperchio")


# ============================================================================================ baule da viaggio
def borchie(P, punti, r=0.007):
    for i, (x, y, z, rot) in enumerate(punti):
        P.append(pezzo("borchia%d" % i, (r * 2, r * 2, r), (x, y, z), "ME_Ottone", 0.05, forma="sfera", vert=8, rot=rot))


def baule():
    W, D, H = 1.0, 0.5, 0.52     # corpo: Unity x 1,0, profondita' 0,5, altezza fino all'orlo 0,52
    fondo_int = 0.43
    pulisci()
    P = []
    t = 0.02
    # pareti (fuori legno a doghe, dentro carta a fiorellini) e fondo interno alto (vassoio)
    for nome, dim, pos in (("fr", (W, t, H), (0, -D / 2 + t / 2, H / 2)), ("re", (W, t, H), (0, D / 2 - t / 2, H / 2)),
                           ("sx", (t, D - 2 * t, H), (-W / 2 + t / 2, 0, H / 2)), ("dx", (t, D - 2 * t, H), (W / 2 - t / 2, 0, H / 2)),
                           ("fondo", (W, D, 0.02), (0, 0, 0.01))):
        P.append(pezzo("parete_" + nome, dim, pos, "ME_LegnoBaule", 1.2, smusso=0.003))
    P.append(pezzo("vassoio", (W - 2 * t, D - 2 * t, 0.01), (0, 0, fondo_int - 0.005), "ME_Fodera", 0.3))
    for nome, dim, pos in (("ffr", (W - 2 * t, 0.002, H - fondo_int), (0, -D / 2 + t + 0.001, (H + fondo_int) / 2)),
                           ("fre", (W - 2 * t, 0.002, H - fondo_int), (0, D / 2 - t - 0.001, (H + fondo_int) / 2)),
                           ("fsx", (0.002, D - 2 * t, H - fondo_int), (-W / 2 + t + 0.001, 0, (H + fondo_int) / 2)),
                           ("fdx", (0.002, D - 2 * t, H - fondo_int), (W / 2 - t - 0.001, 0, (H + fondo_int) / 2))):
        P.append(pezzo(nome, dim, pos, "ME_Fodera", 0.3))
    # listelli orizzontali di rinforzo
    for z in (0.14, 0.34):
        P.append(pezzo("listello_f%g" % z, (W - 0.1, 0.012, 0.05), (0, -D / 2 - 0.006, z), "ME_LegnoBaule", 1.2, smusso=0.004))
        P.append(pezzo("listello_r%g" % z, (W - 0.1, 0.012, 0.05), (0, D / 2 + 0.006, z), "ME_LegnoBaule", 1.2, smusso=0.004))
    # bordature di ferro sugli spigoli verticali e sull'orlo, con le borchie
    bb = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(pezzo("angolo%d%d" % (sx, sy), (0.05, 0.05, H), (sx * (W / 2 - 0.022), sy * (D / 2 - 0.022), H / 2), "ME_Ferro", 0.5, smusso=0.004))
            for z in (0.05, 0.18, 0.31, 0.45):
                bb.append((sx * (W / 2 - 0.022), sy * (D / 2 + 0.004), z, (90, 0, 0)))
                bb.append((sx * (W / 2 + 0.004), sy * (D / 2 - 0.022), z, (0, 90, 0)))
    for sy in (-1, 1):
        P.append(pezzo("orlo_fr%d" % sy, (W, 0.008, 0.03), (0, sy * (D / 2 + 0.004), H - 0.015), "ME_Ferro", 0.5, smusso=0.002))
        P.append(pezzo("piede_fr%d" % sy, (W, 0.008, 0.03), (0, sy * (D / 2 + 0.004), 0.015), "ME_Ferro", 0.5, smusso=0.002))
        for x in (-0.36, -0.12, 0.12, 0.36):
            bb.append((x, sy * (D / 2 + 0.009), H - 0.015, (90, 0, 0)))
            bb.append((x, sy * (D / 2 + 0.009), 0.015, (90, 0, 0)))
    borchie(P, bb)
    # cinghie di cuoio che passano sotto il baule, con fibbie d'ottone sul davanti
    for x in (-0.3, 0.3):
        P.append(pezzo("cinghia_f%g" % x, (0.05, 0.006, H - 0.03), (x, -D / 2 - 0.016, (H - 0.03) / 2), "ME_CuoioCinghia", 0.3))
        P.append(pezzo("cinghia_r%g" % x, (0.05, 0.006, H - 0.03), (x, D / 2 + 0.016, (H - 0.03) / 2), "ME_CuoioCinghia", 0.3))
        P.append(pezzo("cinghia_s%g" % x, (0.05, D + 0.035, 0.006), (x, 0, -0.003), "ME_CuoioCinghia", 0.3))
        P.append(pezzo("fibbia%g" % x, (0.06, 0.008, 0.045), (x, -D / 2 - 0.021, 0.3), "ME_Ottone", 0.06, smusso=0.003))
        P.append(pezzo("ardiglione%g" % x, (0.004, 0.006, 0.04), (x, -D / 2 - 0.026, 0.3), "ME_Ferro", 0.05))
    # maniglie laterali: staffe di ferro e passante di cuoio
    for sx in (-1, 1):
        P.append(pezzo("staffa_m%d" % sx, (0.01, 0.16, 0.05), (sx * (W / 2 + 0.005), 0, 0.36), "ME_Ferro", 0.3, smusso=0.003))
        P.append(pezzo("maniglia%d" % sx, (0.02, 0.14, 0.03), (sx * (W / 2 + 0.03), 0, 0.33), "ME_CuoioCinghia", 0.3, smusso=0.008, seg=3))
    o = unisci(P, "baule_corpo"); spigoli(o, 40)
    esporta([o], "baule_corpo", lod1=lod_da(o, 0.4))

    # coperchio (Unity y 0,52..0,62 da chiuso), con le cinghie che continuano e la bandella della serratura che scende davanti
    pulisci()
    P = []
    Hc = 0.1; z0 = H
    P.append(pezzo("tavola", (W + 0.01, D + 0.01, Hc), (0, 0, z0 + Hc / 2), "ME_LegnoBaule", 1.2, smusso=0.01, seg=3))
    P.append(pezzo("fodera", (W - 0.04, D - 0.04, 0.002), (0, 0, z0 - 0.001), "ME_Fodera", 0.3))
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(pezzo("cantonale%d%d" % (sx, sy), (0.07, 0.07, Hc + 0.004), (sx * (W / 2 - 0.03), sy * (D / 2 - 0.03), z0 + Hc / 2), "ME_Ferro", 0.5, smusso=0.006))
    P.append(pezzo("bordo_lat", (W + 0.016, D + 0.016, 0.03), (0, 0, z0 + 0.015), "ME_Ferro", 0.5, smusso=0.003))
    bb = [(x, sy * (D / 2 + 0.009), z0 + 0.015, (90, 0, 0)) for x in (-0.36, -0.12, 0.12, 0.36) for sy in (-1, 1)]
    bb += [(sx * 0.47, sy * 0.22, z0 + Hc + 0.004, (0, 0, 0)) for sx in (-1, 1) for sy in (-1, 1)]
    borchie(P, bb)
    for x in (-0.3, 0.3):
        P.append(pezzo("cinghia_c%g" % x, (0.05, D + 0.03, 0.006), (x, 0, z0 + Hc + 0.003), "ME_CuoioCinghia", 0.3))
        P.append(pezzo("cinghia_cf%g" % x, (0.05, 0.006, Hc), (x, -D / 2 - 0.013, z0 + Hc / 2), "ME_CuoioCinghia", 0.3))
        P.append(pezzo("cinghia_cr%g" % x, (0.05, 0.006, Hc), (x, D / 2 + 0.013, z0 + Hc / 2), "ME_CuoioCinghia", 0.3))
    P.append(pezzo("listello_c", (W - 0.1, 0.05, 0.012), (0, 0, z0 + Hc + 0.006), "ME_LegnoBaule", 1.2, smusso=0.004))
    P.append(pezzo("bandella", (0.05, 0.008, 0.1), (0, -D / 2 - 0.012, z0 + 0.02), "ME_Ottone", 0.06, smusso=0.004))
    P.append(pezzo("cerniera_a", (0.1, 0.02, 0.012), (-0.3, D / 2 + 0.008, z0), "ME_Ferro", 0.3, smusso=0.003))
    P.append(pezzo("cerniera_b", (0.1, 0.02, 0.012), (0.3, D / 2 + 0.008, z0), "ME_Ferro", 0.3, smusso=0.003))
    o = unisci(P, "baule_coperchio"); spigoli(o, 40)
    esporta([o], "baule_coperchio", lod1=lod_da(o, 0.4))

    # serratura d'ottone (parte cliccabile 0,07 x 0,09 x 0,02)
    pulisci()
    P = [pezzo("placca", (0.07, 0.012, 0.09), (0, 0.004, 0), "ME_Ottone", 0.06, smusso=0.006, seg=3),
         pezzo("toppa", (0.012, 0.004, 0.026), (0, -0.003, -0.005), "ME_Ferro", 0.1, smusso=0.004),
         pezzo("toppa_tonda", (0.018, 0.004, 0.018), (0, -0.003, 0.008), "ME_Ferro", 0.1, forma="cilindro", vert=10, rot=(90, 0, 0)),
         pezzo("bordino", (0.06, 0.003, 0.08), (0, -0.0035, 0), "ME_Ottone", 0.06, smusso=0.004)]
    for x, z in ((-0.026, 0.036), (0.026, 0.036), (-0.026, -0.036), (0.026, -0.036)):
        P.append(pezzo("vite", (0.007, 0.003, 0.007), (x, -0.004, z), "ME_Ferro", 0.05, forma="cilindro", vert=8, rot=(90, 0, 0)))
    o = unisci(P, "serratura_baule"); spigoli(o, 40)
    esporta([o], "serratura_baule")

    coperta(); assi(); scatolina()


def coperta():
    """Coperta di lana piegata in tre, morbida: 0,9 x 0,4 x 0,08 (stessa misura della parte cliccabile)."""
    pulisci()
    bm = bmesh.new(); uv = uv_layer(bm)
    nx, ny = 36, 16
    strati = 3; h = 0.08; W, D = 0.9, 0.4
    r = random.Random(5)
    onde = [(r.uniform(0.002, 0.005), r.uniform(2, 6), r.uniform(0, 6)) for _ in range(4)]
    for s in range(strati):
        zc = -h / 2 + h / strati * (s + 0.5)
        th = h / strati * 0.9
        for faccia in (1, -1):
            vs = []
            for j in range(ny + 1):
                riga = []
                for i in range(nx + 1):
                    x = (i / nx - 0.5) * W; y = (j / ny - 0.5) * D
                    z = zc + faccia * th / 2
                    z += sum(a * math.sin(f * x * 6 + p + s) * math.sin(f * y * 9 + p) for a, f, p in onde) * (1.0 if faccia > 0 else 0.6)
                    bordo = max(abs(x) / (W / 2), 0)
                    z -= faccia * th * 0.35 * max(0, bordo - 0.9) * 10 * 0.5
                    riga.append(bm.verts.new((x * (1 - 0.012 * s), y, z)))
                vs.append(riga)
            for j in range(ny):
                for i in range(nx):
                    q = (vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]) if faccia > 0 else (vs[j][i], vs[j + 1][i], vs[j + 1][i + 1], vs[j][i + 1])
                    f = bm.faces.new(q)
                    for l in f.loops:
                        l[uv].uv = (l.vert.co.x / 0.5 + s * 0.31, l.vert.co.y / 0.5 + faccia * 0.2)
    bm.normal_update()
    o = oggetto("coperta", bm); o.data.materials.append(mat("ME_Coperta"))
    # pieghe tonde sui lati corti (dove la coperta gira su se stessa)
    P = [o]
    for s in range(strati - 1):
        lato = 1 if s % 2 == 0 else -1
        P.append(pezzo("piega%d" % s, (0.055, D, h / strati * 1.6), (lato * (W / 2 - 0.012), 0, -h / 2 + h / strati * (s + 1)), "ME_Coperta", 0.5, forma="cilindro", vert=12, rot=(90, 0, 0)))
    o = unisci(P, "coperta"); spigoli(o, 60)
    esporta([o], "coperta")


def assi():
    reg = json.load(open(os.path.join(TMP, "assi_regioni.json")))
    for nome in ("asse_croce", "asse_treno", "asse_scarpa", "fondo"):
        pulisci()
        dim = (0.24, 0.38, 0.03) if nome != "fondo" else (0.9, 0.4, 0.02)
        bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0); bmesh.ops.scale(bm, vec=dim, verts=bm.verts)
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=0.003, segments=2, affect="EDGES")
        bm.normal_update()
        u0, v0, w, h = reg[nome]
        def mappa(c, n, dim=dim, u0=u0, v0=v0, w=w, h=h):
            ax = max(range(3), key=lambda i: abs(n[i]))
            if ax == 2: return (u0 + (c.x / dim[0] + 0.5) * w, v0 + (c.y / dim[1] + 0.5) * h)
            if ax == 1: return (u0 + (c.x / dim[0] + 0.5) * w, v0 + (c.z / dim[2] + 0.5) * 0.02)
            return (u0 + (c.z / dim[2] + 0.5) * 0.02, v0 + (c.y / dim[1] + 0.5) * h)
        regione_uv(bm, bm.faces, mappa)
        o = oggetto(nome, bm); o.data.materials.append(mat("ME_AssiIncise")); spigoli(o, 40)
        esporta([o], nome if nome != "fondo" else "baule_fondo")


def scatolina():
    """Scatolina avvolta in un panno di lino, legata con lo spago: 0,34 x 0,22 x 0,14."""
    pulisci()
    X, Y, Z = 0.34, 0.22, 0.14
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=7, use_grid_fill=True)
    bmesh.ops.scale(bm, vec=(X - 0.01, Y - 0.01, Z - 0.01), verts=bm.verts)
    bmesh.ops.bevel(bm, geom=bm.edges[:], offset=0.014, segments=2, affect="EDGES")
    r = random.Random(9)
    for v in bm.verts:  # panno morbido: gonfiore e grinze
        n = v.co.normalized()
        g = 0.003 * math.sin(v.co.x * 60 + v.co.z * 30) * math.sin(v.co.y * 50) + 0.0025 * r.random()
        v.co += n * g
        if v.co.z > Z / 2 - 0.02: v.co.z += 0.004 * math.sin(v.co.x * 25) * math.cos(v.co.y * 31)
    bm.normal_update(); uv_scatola(bm, 0.25)
    o = oggetto("panno", bm); o.data.materials.append(mat("ME_Lino"))
    P = [o]
    s = 0.007
    P.append(pezzo("spago_x", (X + 0.006, s, Z + 0.006), (0, 0, 0), "ME_Iuta", 0.05, smusso=0.003))
    P.append(pezzo("spago_y", (s, Y + 0.006, Z + 0.006), (0, 0, 0), "ME_Iuta", 0.05, smusso=0.003))
    # i due nastri di spago sono scatole cave: tolgo le facce coperte dal panno (resta il giro di spago)
    for sp in P[1:]:
        bmx = bmesh.new(); bmx.from_mesh(sp.data)
        bmesh.ops.delete(bmx, geom=[f for f in bmx.faces if abs(f.normal.y) > 0.9 and sp.name == "spago_x" or abs(f.normal.x) > 0.9 and sp.name == "spago_y"], context="FACES")
        bmx.to_mesh(sp.data); bmx.free()
    P.append(pezzo("nodo", (0.03, 0.03, 0.018), (0, 0, Z / 2 + 0.01), "ME_Iuta", 0.05, forma="sfera", vert=10))
    P.append(pezzo("cappio_a", (0.05, 0.02, 0.008), (-0.03, 0.006, Z / 2 + 0.008), "ME_Iuta", 0.05, forma="sfera", vert=10, rot=(0, 0, 25)))
    P.append(pezzo("cappio_b", (0.05, 0.02, 0.008), (0.03, -0.004, Z / 2 + 0.008), "ME_Iuta", 0.05, forma="sfera", vert=10, rot=(0, 0, -20)))
    o = unisci(P, "scatolina"); spigoli(o, 50)
    esporta([o], "scatolina")


# ============================================================================================ calendario di carta
def calendario():
    pulisci()
    W, H = 0.44, 0.76
    r = random.Random(3)
    def piega(s, t):   # carta che ondeggia verso il muro (+Y), angoli in basso un po' arricciati
        y = 0.0025 * (1 + math.sin(t * 9 + 1.3)) + 0.002 * math.sin(s * 7)
        y -= 0.006 * max(0, 0.12 - t) / 0.12 * (abs(s - 0.5) * 2) ** 3
        return y + 0.0015
    foglio = lastra("foglio", W, H, 16, 28, (0, 0, 1, 1), "ME_Calendario", piega=piega)
    foglio.location = (0, 0, 0.15)
    P = [foglio]
    ztop = 0.15 + H / 2
    P.append(pezzo("listello", (W + 0.01, 0.007, 0.032), (0, -0.001, ztop - 0.014), "ME_Ferro", 0.2, smusso=0.002))
    P.append(pezzo("occhiello", (0.016, 0.003, 0.016), (0, -0.0055, ztop - 0.012), "ME_Ottone", 0.05, forma="cilindro", vert=10, rot=(90, 0, 0)))
    # spago fino al chiodo
    for k, sx in enumerate((-1, 1)):
        a = pezzo("spago%d" % k, (0.0025, 0.0025, 0.1), (sx * 0.022, 0.0, ztop + 0.043), "ME_Iuta", 0.05, forma="cilindro", vert=6, rot=(0, -sx * 27, 0))
        P.append(a)
    P.append(pezzo("chiodo", (0.012, 0.012, 0.004), (0, 0.001, ztop + 0.088), "ME_Ferro", 0.05, forma="cilindro", vert=8, rot=(90, 0, 0)))
    o = unisci(P, "calendario"); spigoli(o, 50)
    esporta([o], "calendario")
    # placca d'ottone sotto il foglio (0,46 x 0,16 x 0,02)
    pulisci()
    P = [pezzo("placca", (0.46, 0.012, 0.16), (0, 0.004, 0), "ME_Ottone", 0.1, smusso=0.005, seg=3),
         pezzo("bordino", (0.43, 0.004, 0.13), (0, -0.003, 0), "ME_Ottone", 0.1, smusso=0.003)]
    for x, z in ((-0.21, 0.06), (0.21, 0.06), (-0.21, -0.06), (0.21, -0.06)):
        P.append(pezzo("vite", (0.012, 0.004, 0.012), (x, -0.004, z), "ME_Ferro", 0.05, forma="cilindro", vert=8, rot=(90, 0, 0)))
    o = unisci(P, "placca_ottone"); spigoli(o, 40)
    esporta([o], "placca_ottone")
    # segno a matita (decal sottile: la scarpetta disegnata)
    pulisci()
    o = lastra("segno_matita", 0.29, 0.13, 1, 1, (0, 0, 1, 1), "ME_SegnoMatita")
    esporta([o], "segno_matita")


# ============================================================================================ bucato: panni con pieghe vere (simulazione), toppe cucite, mollette
def panno(nome, toppa):
    pulisci()
    W, H = 0.36, 0.5
    nx, ny = 24, 32
    bm = bmesh.new(); uv = uv_layer(bm)
    vs = []
    r = random.Random(hash(nome) % 100)
    for j in range(ny + 1):
        riga = []
        for i in range(nx + 1):
            riga.append(bm.verts.new(((i / nx - 0.5) * W, 0.0015 * r.uniform(-1, 1), (j / ny - 0.5) * H)))
        vs.append(riga)
    for j in range(ny):
        for i in range(nx):
            f = bm.faces.new((vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]))
            for l, (a, b) in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
                l[uv].uv = ((i + a) / nx, (j + b) / ny)
    bm.normal_update()
    for f in bm.faces:
        if f.normal.y > 0: f.normal_flip()
    o = oggetto(nome, bm)
    rest = [v.co.copy() for v in o.data.vertices]
    # stoffa raccolta dalle mollette: in alto i due punti di presa piu' vicini della larghezza (fa le pieghe)
    o.shape_key_add(name="riposo")
    k = o.shape_key_add(name="preso")
    pin = o.vertex_groups.new(name="pin")
    for idx, v in enumerate(o.data.vertices):
        x, z = rest[idx].x, rest[idx].z
        if z > H / 2 - 0.02:
            for px in (-0.12, 0.12):
                if abs(x - px) < 0.03:
                    pin.add([idx], 1.0, "REPLACE")
                    k.data[idx].co.x = px * 0.9 + (x - px) * 0.5
            k.data[idx].co.x = x * 0.92
    o.active_shape_key_index = 1; k.value = 1.0
    attiva(o)
    cl = o.modifiers.new("cloth", "CLOTH")
    s = cl.settings
    s.quality = 8; s.mass = 0.12; s.tension_stiffness = 12; s.compression_stiffness = 12; s.shear_stiffness = 5; s.bending_stiffness = 0.15
    s.air_damping = 2.0; s.vertex_group_mass = "pin"; s.pin_stiffness = 1.0
    s.rest_shape_key = o.data.shape_keys.key_blocks["riposo"]
    s.gravity = (0, 0.6, -9.81)   # un filo di vento verso il muro: il panno non resta piatto
    cl.point_cache.frame_start = 1; cl.point_cache.frame_end = 70
    sc = bpy.context.scene
    for f in range(1, 71): sc.frame_set(f)
    dg = bpy.context.evaluated_depsgraph_get()
    ev = o.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(ev)
    tessuto = bpy.data.objects.new(nome + "_t", me); bpy.context.collection.objects.link(tessuto)
    bpy.data.objects.remove(o, do_unlink=True)
    tessuto.data.materials.clear(); tessuto.data.materials.append(mat("MT_Panno"))
    # la toppa: le facce del panno dentro il rettangolo, staccate di 2 mm verso il davanti, UV sulla meta' giusta dell'atlante
    cx, cz, lato, quadri = toppa
    bm = bmesh.new(); bm.from_mesh(tessuto.data); bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    uvl = uv_layer(bm)
    sel = []
    for f in bm.faces:
        c = Vector((0, 0));
        for l in f.loops: c += Vector(l[uvl].uv)
        c /= len(f.loops)
        x = (c.x - 0.5) * W; z = (c.y - 0.5) * H
        if abs(x - cx) < lato / 2 + 0.01 and abs(z - cz) < lato / 2 + 0.01: sel.append(f)
    bm2 = bmesh.new(); uv2 = uv_layer(bm2); mappa = {}
    for f in sel:
        nv = []
        for l in f.loops:
            v = l.vert
            if v.index not in mappa: mappa[v.index] = bm2.verts.new(v.co + v.normal * 0.0022)
            nv.append(mappa[v.index])
        f2 = bm2.faces.new(nv)
        for l2, l in zip(f2.loops, f.loops):
            x = (l[uvl].uv[0] - 0.5) * W; z = (l[uvl].uv[1] - 0.5) * H
            u = min(max((x - cx) / lato + 0.5, 0.0), 1.0); v = min(max((z - cz) / lato + 0.5, 0.0), 1.0)
            l2[uv2].uv = (u * 0.5 + (0.0 if quadri else 0.5), v)
    bm.free()
    to = oggetto("toppa", bm2); to.data.materials.append(mat("ME_Toppe"))
    attiva(tessuto); m = tessuto.modifiers.new("sol", "SOLIDIFY"); m.thickness = 0.0015; m.offset = 0.0
    bpy.ops.object.modifier_apply(modifier=m.name)
    o = unisci([tessuto, to], nome)
    for p in o.data.polygons: p.use_smooth = True
    esporta([o], nome)


def molletta():
    pulisci()
    P = []
    for k, y in enumerate((-0.0045, 0.0045)):
        bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0); bmesh.ops.scale(bm, vec=(0.01, 0.0075, 0.072), verts=bm.verts)
        for v in bm.verts:   # becco che si assottiglia in basso, impugnatura in alto
            if v.co.z < -0.02: v.co.x *= 0.8; v.co.y += (-0.0012 if y < 0 else 0.0012) * 0 + (0.0015 if (v.co.y * y) < 0 else 0)
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=0.0012, segments=1, affect="EDGES")
        bmesh.ops.translate(bm, vec=(0, y, 0), verts=bm.verts)
        bm.normal_update(); uv_scatola(bm, 0.2, (random.random(), random.random()))
        o = oggetto("ganascia%d" % k, bm); o.data.materials.append(mat("ME_LegnoChiaro")); P.append(o)
    P.append(pezzo("molla", (0.012, 0.02, 0.012), (0, 0, 0.006), "ME_Ferro", 0.1, forma="cilindro", vert=10, rot=(0, 90, 0)))
    P.append(pezzo("molla_b", (0.002, 0.024, 0.026), (0.0065, 0, 0.004), "ME_Ferro", 0.1, smusso=0.0008))
    o = unisci(P, "molletta"); spigoli(o, 40)
    esporta([o], "molletta")


# ============================================================================================ ricordo: forma da calzolaio, lucchetto, cornice con foto, banco
def forma_legno():
    """Forma di legno da calzolaio: sezioni lungo X (tallone -X, punta +X), fondo piatto con il calcio rialzato, taglio a cuneo e piastrina sul tallone."""
    pulisci()
    sez = [(-0.50, 0.10, 0.35, 0.0), (-0.47, 0.16, 0.60, 0.0), (-0.38, 0.19, 0.70, 0.0), (-0.25, 0.18, 0.66, 0.0), (-0.1, 0.19, 0.55, 0.005), (0.05, 0.23, 0.45, 0.01),
           (0.2, 0.25, 0.36, 0.02), (0.32, 0.23, 0.28, 0.035), (0.42, 0.18, 0.2, 0.05), (0.48, 0.11, 0.14, 0.065), (0.5, 0.04, 0.08, 0.075)]
    N = 18
    bm = bmesh.new(); anelli = []
    for x, w, h, alza in sez:
        an = []
        for k in range(N):
            a = 2 * math.pi * k / N
            yy = math.cos(a) * w
            s = math.sin(a)
            zz = alza + (h * (0.5 + 0.5 * s) if s > -0.6 else h * 0.2 * (s + 1) / 0.4 * 0.1)
            an.append(bm.verts.new((x, yy, zz)))
        anelli.append(an)
    for i in range(len(anelli) - 1):
        for k in range(N):
            bm.faces.new((anelli[i][k], anelli[i][(k + 1) % N], anelli[i + 1][(k + 1) % N], anelli[i + 1][k]))
    for an in (anelli[0], anelli[-1]):
        bm.faces.new(an if an is anelli[-1] else list(reversed(an)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update(); uv_scatola(bm, 0.6, (0.3, 0.1))
    o = oggetto("forma", bm); o.data.materials.append(mat("ME_LegnoChiaro"))
    attiva(o); m = o.modifiers.new("sub", "SUBSURF"); m.levels = 1; bpy.ops.object.modifier_apply(modifier=m.name)
    P = [o, pezzo("piastrina", (0.13, 0.12, 0.012), (-0.38, 0, 0.705), "ME_Ferro", 0.2, smusso=0.004),
         pezzo("foro", (0.035, 0.035, 0.01), (-0.38, 0, 0.71), "ME_Ferro", 0.2, forma="cilindro", vert=10),
         pezzo("taglio", (0.012, 0.36, 0.02), (-0.08, 0, 0.56), "ME_Ferro", 0.2, smusso=0.004)]
    o = unisci(P, "forma_legno"); spigoli(o, 50)
    esporta([o], "forma_legno")


def lucchetto():
    """Lucchetto d'ottone a rotelle (parte 0,32 x 0,38 x 0,1): corpo in basso, arco d'acciaio in alto, finestra delle rotelle."""
    pulisci()
    P = [pezzo("corpo", (0.32, 0.1, 0.27), (0, 0, -0.025), "ME_Ottone", 0.15, smusso=0.035, seg=4),
         pezzo("fascia", (0.3, 0.104, 0.02), (0, 0, 0.095), "ME_Ottone", 0.15, smusso=0.004),
         pezzo("finestra", (0.3, 0.02, 0.085), (0, -0.045, -0.0), "ME_Ferro", 0.3, smusso=0.004),
         pezzo("targa", (0.11, 0.006, 0.05), (-0.06, -0.051, -0.1), "ME_Ottone", 0.08, smusso=0.003)]
    for x in (-0.115, 0.115):
        P.append(pezzo("gamba%g" % x, (0.028, 0.028, 0.08), (x, 0, 0.14), "ME_Ferro", 0.2, forma="cilindro", vert=12))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.115, minor_radius=0.014, major_segments=24, minor_segments=10, location=(0, 0, 0.18), rotation=(math.radians(90), 0, 0))
    t = bpy.context.view_layer.objects.active
    bm = bmesh.new(); bm.from_mesh(t.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.y < -0.0001], context="VERTS")   # in coordinate locali del toro: tolgo la meta' bassa
    uv_scatola(bm, 0.2); bm.to_mesh(t.data); bm.free()
    t.data.materials.append(mat("ME_Ferro")); P.append(t)
    o = unisci(P, "lucchetto"); spigoli(o, 40)
    esporta([o], "lucchetto")


def cornice():
    """Cornice di noce modanata con filetto dorato (parte 0,36 x 0,46 x 0,045, fronte -Y), apertura per passepartout 0,29 x 0,39."""
    pulisci()
    W, H, D = 0.36, 0.46, 0.045
    ow, oh = 0.27, 0.37
    P = []
    for k, (w, d, y, m, sc) in enumerate(((0.045, D, 0.0, "ME_LegnoNoce", 0.25), (0.02, 0.008, -D / 2 - 0.002, "ME_LegnoNoce", 0.25), (0.006, 0.006, -D / 2 - 0.001, "ME_Ottone", 0.08))):
        x0 = ow / 2 + (0 if k != 1 else 0.012) + (0 if k != 2 else 0.0)
        z0 = oh / 2 + (0 if k != 1 else 0.012)
        x1 = W / 2 if k == 0 else x0 + w; z1 = H / 2 if k == 0 else z0 + w
        if k == 2: x0, z0 = ow / 2 + 0.003, oh / 2 + 0.003; x1, z1 = x0 + w, z0 + w
        for nome, dim, pos in (("s", (x1 - x0, d, 2 * z1), (-(x0 + x1) / 2, y, 0)), ("d", (x1 - x0, d, 2 * z1), ((x0 + x1) / 2, y, 0)),
                               ("a", (2 * x0, d, z1 - z0), (0, y, (z0 + z1) / 2)), ("b", (2 * x0, d, z1 - z0), (0, y, -(z0 + z1) / 2))):
            P.append(pezzo(nome + str(k), dim, pos, m, sc, smusso=0.004 if k == 0 else 0.002))
    P.append(pezzo("fondo", (W - 0.02, 0.004, H - 0.02), (0, D / 2 - 0.004, 0), "ME_LegnoNoce", 0.25))
    P.append(pezzo("piedino", (0.04, 0.012, 0.2), (0, D / 2 + 0.03, -0.1), "ME_LegnoNoce", 0.25, smusso=0.003, rot=(-20, 0, 0)))
    o = unisci(P, "cornice"); spigoli(o, 40)
    esporta([o], "cornice")
    pulisci()
    o = lastra("fotografia", 0.22, 0.3, 1, 1, (0, 0, 1, 1), "ME_Foto")
    esporta([o], "fotografia")


def banco():
    """Banco del calzolaio (parte 1,2 x 0,9 x 0,55): gambe, fasce, ripiano basso con attrezzi; il piano e il cassetto sono modelli a parte."""
    pulisci()
    X, Y, Z = 1.2, 0.55, 0.9
    P = []
    g = 0.06
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(pezzo("gamba%d%d" % (sx, sy), (g, g, Z), (sx * (X / 2 - g / 2), sy * (Y / 2 - g / 2), Z / 2), "ME_LegnoBanco", 1.0, smusso=0.006))
    P.append(pezzo("fascia_f_sx", (0.32, 0.025, 0.22), (-0.42, -Y / 2 + 0.0125, Z - 0.11), "ME_LegnoBanco", 1.0, smusso=0.003))
    P.append(pezzo("fascia_f_dx", (0.32, 0.025, 0.22), (0.42, -Y / 2 + 0.0125, Z - 0.11), "ME_LegnoBanco", 1.0, smusso=0.003))
    P.append(pezzo("vano", (0.52, 0.02, 0.22), (0, Y / 2 - 0.08, Z - 0.11), "ME_LegnoBanco", 1.0))
    P.append(pezzo("fascia_r", (X - 2 * g, 0.025, 0.22), (0, Y / 2 - 0.0125, Z - 0.11), "ME_LegnoBanco", 1.0, smusso=0.003))
    for sx in (-1, 1):
        P.append(pezzo("fascia_l%d" % sx, (0.025, Y - 2 * g, 0.22), (sx * (X / 2 - 0.0125), 0, Z - 0.11), "ME_LegnoBanco", 1.0, smusso=0.003))
        P.append(pezzo("guida%d" % sx, (0.02, Y - 0.1, 0.02), (sx * 0.27, 0, Z - 0.21), "ME_LegnoBanco", 1.0))
    P.append(pezzo("traverso_f", (X - 2 * g, 0.04, 0.05), (0, -Y / 2 + 0.05, 0.12), "ME_LegnoBanco", 1.0, smusso=0.004))
    P.append(pezzo("ripiano", (X - 0.1, Y - 0.08, 0.025), (0, 0, 0.16), "ME_LegnoBanco", 1.0, smusso=0.004))
    # sul ripiano: una cassetta di chiodi, un rotolo di spago, un pezzo di cuoio
    P.append(pezzo("cassetta_chiodi", (0.22, 0.14, 0.07), (-0.3, 0.05, 0.21), "ME_LegnoChiaro", 0.2, smusso=0.004))
    P.append(pezzo("rotolo", (0.08, 0.08, 0.06), (0.05, 0.06, 0.2), "ME_Iuta", 0.05, forma="cilindro", vert=14))
    P.append(pezzo("ritaglio", (0.3, 0.2, 0.006), (0.32, 0.0, 0.176), "ME_CuoioCinghia", 0.3, smusso=0.002, rot=(0, 0, 12)))
    o = unisci(P, "banco_calzolaio"); spigoli(o, 40)
    esporta([o], "banco_calzolaio", lod1=lod_da(o, 0.5))
    pulisci()
    o = pezzo("piano", (1.26, 0.6, 0.04), (0, 0, 0), "ME_LegnoBanco", 1.0, smusso=0.008, seg=2)
    bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:   # piano consumato al centro, dove si lavora
        if v.co.z > 0: v.co.z -= 0.003 * math.exp(-((v.co.x + 0.1) ** 2 + v.co.y ** 2) / 0.05)
    bm.to_mesh(o.data); bm.free(); spigoli(o, 40)
    esporta([o], "piano_banco")
    pulisci()
    P = [pezzo("frontale", (0.5, 0.03, 0.2), (0, 0.005, 0), "ME_LegnoBanco", 1.0, smusso=0.004),
         pezzo("cornicetta", (0.46, 0.006, 0.16), (0, -0.012, 0), "ME_LegnoBanco", 1.0, smusso=0.003),
         pezzo("pomello", (0.04, 0.03, 0.04), (0, -0.026, 0), "ME_Ottone", 0.06, forma="sfera", vert=12),
         pezzo("bocchetta", (0.03, 0.004, 0.04), (0.12, -0.016, 0.0), "ME_Ottone", 0.06, smusso=0.003)]
    o = unisci(P, "cassetto"); spigoli(o, 40)
    esporta([o], "cassetto")
    pulisci()
    P = [pezzo("manico", (0.2, 0.026, 0.024), (-0.02, 0, 0), "ME_LegnoChiaro", 0.2, smusso=0.008, seg=3),
         pezzo("testa", (0.022, 0.03, 0.05), (0.085, 0, 0.005), "ME_Ferro", 0.1, smusso=0.004),
         pezzo("bocca", (0.03, 0.03, 0.03), (0.085, 0, 0.036), "ME_Ferro", 0.1, forma="cilindro", vert=12),
         pezzo("penna", (0.012, 0.03, 0.03), (0.085, 0, -0.03), "ME_Ferro", 0.1, smusso=0.004)]
    o = unisci(P, "martello"); spigoli(o, 40)
    esporta([o], "martello")
    pulisci()
    bm = bmesh.new()
    pts = [(math.cos(a) * (0.15 + 0.012 * math.sin(a * 5) + 0.01 * math.sin(a * 11)), math.sin(a) * (0.1 + 0.01 * math.sin(a * 7)), 0) for a in [2 * math.pi * i / 28 for i in range(28)]]
    f = bm.faces.new([bm.verts.new(p) for p in pts])
    r = bmesh.ops.extrude_face_region(bm, geom=[f]); bmesh.ops.translate(bm, vec=(0, 0, 0.004), verts=[v for v in r["geom"] if isinstance(v, bmesh.types.BMVert)])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.normal_update(); uv_scatola(bm, 0.3)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    o = oggetto("pelle", bm); o.data.materials.append(mat("ME_CuoioCinghia")); spigoli(o, 40)
    esporta([o], "pelle")


def scarpa():
    """La scarpa sinistra di R023 (Scarpe.blend, originale del team): cuoio tintabile col colore della parte, suola e lacci scuri."""
    pulisci()
    with bpy.data.libraries.load(os.path.join(ROOT, "ArtSource", "Models", "Scarpe", "Scarpe.blend")) as (src, dst):
        dst.objects = ["Scarpa_Sx"]
    o = dst.objects[0]; bpy.context.collection.objects.link(o)
    attiva(o); bpy.ops.object.make_local(type="ALL")
    o.data = o.data.copy()
    o.rotation_euler = (0, 0, math.radians(-90))     # punta da +Y a +X
    attiva(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    ren = {"Cuoio": "MT_Cuoio", "Suola": "ME_Suola", "Lacci": "ME_Lacci"}
    for i, m in enumerate(o.data.materials):
        o.data.materials[i] = mat(ren.get(m.name, m.name))
    o.name = "scarpa"
    esporta([o], "scarpa", uniforme=True)


TUTTI = {"exvoti": exvoti, "edicola": edicola, "cassetta": cassetta, "baule": baule, "calendario": calendario,
         "panni": lambda: (panno("panno_quadri", (0.07, -0.11, 0.17, True)), panno("panno_tela", (-0.06, -0.13, 0.11, False))),
         "molletta": molletta, "forma_legno": forma_legno, "lucchetto": lucchetto, "cornice": cornice, "banco": banco, "scarpa": scarpa}

if __name__ == "__main__":
    for k in (argv or TUTTI.keys()):
        TUTTI[k]()
    json.dump(misure, open(MISURE, "w"), indent=1, sort_keys=True)
