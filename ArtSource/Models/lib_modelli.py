# Funzioni condivise per gli script Blender di R007 (tappa 2).
# Uso: importato da importa_candidati.py e crea_oggetti.py (blender -b --factory-startup --python ...).
import bpy, bmesh, os, json, math
from mathutils import Vector, Matrix

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
USCITA_MODELLI = os.path.join(RADICE, 'Assets', 'Art', 'Models')
DIR_META = os.path.join(os.path.dirname(__file__), 'build')


def azzera_scena():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def seleziona(oggetti, attivo=None):
    bpy.ops.object.select_all(action='DESELECT')
    for o in oggetti:
        o.select_set(True)
    bpy.context.view_layer.objects.active = attivo or (oggetti[0] if oggetti else None)


def n_tri(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def dimensioni(obj):
    pts = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def stacca_e_applica(meshes):
    """Toglie i genitori (glTF crea empty con la rotazione Y-up/Z-up) e applica le trasformazioni."""
    for o in meshes:
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
    for o in list(bpy.context.scene.objects):
        if o.type != 'MESH':
            bpy.data.objects.remove(o, do_unlink=True)
    seleziona(meshes)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def unisci(meshes):
    seleziona(meshes)
    if len(meshes) > 1:
        bpy.ops.object.join()
    return bpy.context.view_layer.objects.active


def scala_e_base(obj, asse, valore, pivot='base'):
    """Scala uniforme perche' la dimensione `asse` ('x','y','z','max') valga `valore` metri;
    origine al centro XY e a terra (pivot 'base') o al centro (pivot 'centro')."""
    mn, mx = dimensioni(obj)
    d = mx - mn
    ref = {'x': d.x, 'y': d.y, 'z': d.z, 'max': max(d.x, d.y, d.z)}[asse]
    f = valore / ref if valore else 1.0
    seleziona([obj])
    obj.scale = (f, f, f)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mn, mx = dimensioni(obj)
    c = (mn + mx) * 0.5
    dz = -mn.z if pivot == 'base' else -c.z
    obj.data.transform(Matrix.Translation((-c.x, -c.y, dz)))
    obj.data.update()
    return f


def decima(obj, tri_obiettivo):
    t = n_tri(obj)
    if t <= tri_obiettivo:
        return False
    seleziona([obj])
    m = obj.modifiers.new('decima', 'DECIMATE')
    m.decimate_type = 'COLLAPSE'
    m.ratio = max(0.01, tri_obiettivo / t)
    m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)
    return True


def liscia_con_spigoli(obj, angolo=40.0):
    """Normali ricalcolate: liscio, con spigoli netti oltre `angolo` gradi (esportati come spigoli duri)."""
    seleziona([obj])
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception:
        pass
    me = obj.data
    for p in me.polygons:
        p.use_smooth = True
    bm = bmesh.new()
    bm.from_mesh(me)
    lim = math.radians(angolo)
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = e.calc_face_angle(0.0) < lim
        else:
            e.smooth = True
    bm.to_mesh(me)
    bm.free()
    me.update()


def materiali_puliti(obj):
    """Sostituisce i materiali con materiali vuoti omonimi (niente nodi/riferimenti a texture esterne nell'FBX)."""
    nomi = []
    for i, s in enumerate(obj.material_slots):
        nome = s.material.name if s.material else 'Materiale%d' % i
        nomi.append(nome)
    for i, s in enumerate(obj.material_slots):
        vecchio = s.material
        if vecchio:
            vecchio.name = nomi[i] + '__vecchio'
        nuovo = bpy.data.materials.new(nomi[i])
        s.material = nuovo
    return nomi


def crea_lod(obj, nome, rapporti=(0.5, 0.25), minimo=60):
    """Rinomina obj in nome_LOD0 e crea copie decimate nome_LOD1.. Ritorna la lista di oggetti e dei tri."""
    obj.name = nome + '_LOD0'
    obj.data.name = nome + '_LOD0'
    lista = [obj]
    t0 = n_tri(obj)
    for i, r in enumerate(rapporti, start=1):
        c = obj.copy()
        c.data = obj.data.copy()
        c.name = '%s_LOD%d' % (nome, i)
        c.data.name = c.name
        bpy.context.collection.objects.link(c)
        decima(c, max(minimo, int(t0 * r)))
        lista.append(c)
    return lista


def esporta_fbx(oggetti, percorso):
    os.makedirs(os.path.dirname(percorso), exist_ok=True)
    seleziona(oggetti)
    bpy.ops.export_scene.fbx(
        filepath=percorso, use_selection=True, object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL', global_scale=1.0, apply_unit_scale=True,
        axis_forward='-Z', axis_up='Y', bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type='EDGE', add_leaf_bones=False, use_tspace=True,
        path_mode='STRIP', embed_textures=False)


def scrivi_meta(nome, dati):
    os.makedirs(DIR_META, exist_ok=True)
    with open(os.path.join(DIR_META, nome + '.json'), 'w', encoding='utf-8') as f:
        json.dump(dati, f, indent=1, ensure_ascii=False)


def finalizza(nome, obj, tri_obiettivo=None, lod=True, sorgente='', extra=None, cartella=None, lisci=None):
    """obj: oggetto unico con materiali. Decima, crea LOD, esporta FBX in Assets/Art/Models/<cartella|nome>/<nome>.fbx."""
    t_orig = n_tri(obj)
    decimato = decima(obj, tri_obiettivo) if tri_obiettivo else False
    if lisci is None:
        lisci = decimato
    if lisci:
        liscia_con_spigoli(obj)
    materiali = materiali_puliti(obj)
    if lod and n_tri(obj) >= 400:
        oggetti = crea_lod(obj, nome)
    else:
        obj.name = nome
        obj.data.name = nome
        oggetti = [obj]
    for o in oggetti[1:]:
        if lisci or True:
            liscia_con_spigoli(o)
    mn, mx = dimensioni(oggetti[0])
    cart = cartella or nome
    esporta_fbx(oggetti, os.path.join(USCITA_MODELLI, cart, nome + '.fbx'))
    meta = {'nome': nome, 'cartella': cart, 'sorgente': sorgente, 'materiali': materiali,
            'tri': [n_tri(o) for o in oggetti], 'tri_originali': t_orig,
            'dim': [round(mx.x - mn.x, 3), round(mx.y - mn.y, 3), round(mx.z - mn.z, 3)]}
    if extra:
        meta.update(extra)
    scrivi_meta(nome, meta)
    print('FINALIZZATO', nome, 'tri', meta['tri'], 'dim', meta['dim'], 'materiali', materiali)
    return meta


# ------------------------------------------------------------------ primitive procedurali (bmesh)

def nuovo_oggetto(nome, materiali):
    me = bpy.data.meshes.new(nome)
    o = bpy.data.objects.new(nome, me)
    bpy.context.collection.objects.link(o)
    for m in materiali:
        me.materials.append(bpy.data.materials.new(m))
    return o


def box(bm, mn, mx, mat=0):
    """Parallelepipedo asse-allineato da mn a mx (tuple). Ritorna le facce."""
    cx, cy, cz = [(a + b) * 0.5 for a, b in zip(mn, mx)]
    r = bmesh.ops.create_cube(bm, size=1.0)
    verts = r['verts']
    for v in verts:
        v.co.x = cx + v.co.x * (mx[0] - mn[0])
        v.co.y = cy + v.co.y * (mx[1] - mn[1])
        v.co.z = cz + v.co.z * (mx[2] - mn[2])
    facce = list({f for v in verts for f in v.link_faces})
    for f in facce:
        f.material_index = mat
    return facce


def cilindro(bm, base, alto, raggio, seg=10, mat=0, raggio2=None, asse='z'):
    """Cilindro/cono dritto con asse z (default), x o y. base,alto: coordinate lungo l'asse."""
    r = bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=raggio, radius2=raggio if raggio2 is None else raggio2, depth=abs(alto - base))
    verts = r['verts']
    for v in verts:
        v.co.z += (alto + base) * 0.5
    if asse == 'x':
        bmesh.ops.rotate(bm, verts=verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90), 3, 'Y'))
    elif asse == 'y':
        bmesh.ops.rotate(bm, verts=verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90), 3, 'X'))
    facce = list({f for v in verts for f in v.link_faces})
    for f in facce:
        f.material_index = mat
    return verts, facce


def trasla(bm, verts, d):
    bmesh.ops.translate(bm, verts=verts, vec=d)


def uv_box(me, tile=1.0, per_materiale=None):
    """Proiezione a scatola (asse dominante della normale), tile = metri per ripetizione della texture."""
    bm = bmesh.new()
    bm.from_mesh(me)
    uvl = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        t = tile
        if per_materiale and f.material_index in per_materiale:
            t = per_materiale[f.material_index]
        for l in f.loops:
            c = l.vert.co
            if ax == 0:
                l[uvl].uv = (c.y / t, c.z / t)
            elif ax == 1:
                l[uvl].uv = (c.x / t, c.z / t)
            else:
                l[uvl].uv = (c.x / t, c.y / t)
    bm.to_mesh(me)
    bm.free()


def smussa(obj, larghezza=0.01, segmenti=1):
    seleziona([obj])
    m = obj.modifiers.new('smussa', 'BEVEL')
    m.width = larghezza
    m.segments = segmenti
    m.limit_method = 'ANGLE'
    bpy.ops.object.modifier_apply(modifier=m.name)
