import os
ROOT = os.environ.get("TRENO_ROOT", r".")  # cartella del progetto Il Treno dei Bambini
SCRATCH = os.environ.get("TRENO_SCRATCH", os.path.join(ROOT, "ArtSource", "tmp"))
import importlib, bpy, bmesh
import hairgen, hair_child, hair_short, helpers, assemble_child
PALL = [(70, 32, 17), (112, 52, 26), (132, 66, 34)]; PALR = [(40, 27, 18), (62, 42, 28), (80, 56, 38)]
T = os.path.join(ROOT, *['ArtSource', 'Characters', 'textures_v4'])

def join_hair(objname, hobjs):
    v = bpy.data.objects[objname]
    bm = bmesh.new(); bm.from_mesh(v.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index == 1], context='FACES')
    bmesh.ops.delete(bm, geom=[x for x in bm.verts if not x.link_faces], context='VERTS')
    bm.to_mesh(v.data); bm.free()
    for o in hobjs:
        o.data.materials.clear()
        for m in v.data.materials: o.data.materials.append(m)
        for q in o.data.polygons: q.material_index = 1
        for u in o.data.uv_layers: u.name = 'UVMap'
        o.parent = v.parent
        vg = o.vertex_groups.new(name='head'); vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    assemble_child.sel_only(v)
    for o in hobjs: o.select_set(True)
    bpy.context.view_layer.objects.active = v
    bpy.ops.object.join()

def run(objname='AmerigoAdulto_LOD0', tex=True, **kw):
    for m in (hairgen, hair_child, hair_short): importlib.reload(m)
    if tex:
        hair_short.make_texture(T + r"\AmerigoAdulto_Hair", palL=PALL, palR=PALR, seed=5)
        for im in bpy.data.images:
            if 'AmerigoAdulto_Hair' in im.name: im.reload()
    objs, nc = hair_short.build_short(objname, **kw)
    tr = [helpers.ptri(o) for o in objs]
    join_hair(objname, objs)
    return nc, tr
