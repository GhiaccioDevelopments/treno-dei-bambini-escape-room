import os
ROOT = os.environ.get("TRENO_ROOT", r".")  # cartella del progetto Il Treno dei Bambini
SCRATCH = os.environ.get("TRENO_SCRATCH", os.path.join(ROOT, "ArtSource", "tmp"))
import importlib, bpy, bmesh
import cloth, trim_child, adult_parts, adult_trim, shoe_adult, shoe_adult_tl, hair_child, hair_adult, hairgen, hairtex, assemble_child, helpers, matlib
T = os.path.join(ROOT, *['ArtSource', 'Characters', 'textures_v4'])
R_COAT = (0, 0.5, 0.5, 1.0); R_DOLCE = (0.5, 0.5, 1.0, 1.0); R_TROUS = (0, 0, 0.5, 0.5); R_BL = (0.5, 0.25, 0.75, 0.5); R_BR = (0.75, 0.25, 1.0, 0.5)
BUTTON_UV = (0.57, 0.12); LACE_UV = (0.69, 0.12)

def cleanup():
    for n in [o.name for o in bpy.data.objects if o.name.endswith('_raw') or o.name.endswith('_j') or o.name in ('HairNew_LOD0', 'Amerigo_a3_LOD0')]:
        bpy.data.objects.remove(bpy.data.objects[n])

def build_parts(neck_top=1.545):
    grey = bpy.data.materials['refgrey']
    bm = adult_parts.build_dolcevita(neck_top=neck_top); do = cloth.to_obj(bm, 'Dolce_raw'); bm.free()
    bm = adult_parts.build_trousers(); to = cloth.to_obj(bm, 'Trous_raw'); bm.free()
    bm = adult_parts.build_coat(hem_z=0.52); co = cloth.to_obj(bm, 'Coat_raw'); bm.free()
    bpy.context.view_layer.update()
    bm = adult_trim.build_coat_details(co, adult_parts.VLINES, R_COAT, BUTTON_UV); dt = cloth.to_obj(bm, 'CoatDet_raw'); bm.free()
    boots = []
    for sg, nm in ((1, 'BootL_raw'), (-1, 'BootR_raw')):
        bm, sf = shoe_adult.build_shoe_loft(sg); o = cloth.to_obj(bm, nm); bm.free(); boots.append(o)
    bpy.context.view_layer.update()
    tongs = []
    for sg, o, nm, rect in ((1, boots[0], 'TongueL_raw', R_BL), (-1, boots[1], 'TongueR_raw', R_BR)):
        bm = shoe_adult_tl.build_tongue_laces(o, sg, rect, LACE_UV); t = cloth.to_obj(bm, nm); bm.free(); tongs.append(t)
    allp = [do, to, co, dt] + boots + tongs
    for o in allp: o.data.materials.append(grey)
    cloth.decimate_obj(co, target_tris=3300); assemble_child.unwrap_to_rect(co, R_COAT)
    cloth.decimate_obj(do, target_tris=1700); assemble_child.unwrap_to_rect(do, R_DOLCE)
    cloth.decimate_obj(to, target_tris=1500); assemble_child.unwrap_to_rect(to, R_TROUS)
    assemble_child.unwrap_to_rect(boots[0], R_BL); assemble_child.unwrap_to_rect(boots[1], R_BR)
    for o, th in ((co, 0.004), (do, 0.003), (to, 0.004), (boots[0], 0.0025), (boots[1], 0.0025)): cloth.rim_solidify(o, th, -1.0)
    for o in allp:
        cloth.transfer_weights(o); cloth.add_armature(o, 'Adulto_Armature')
    return allp

def make_base(skin_zcut=1.585):
    lod0 = bpy.data.objects['AmerigoAdulto_LOD0']
    base = bpy.data.objects['Base']
    base.data = lod0.data.copy()
    bm = bmesh.new(); bm.from_mesh(base.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index in (1, 3, 4)], context='FACES')
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index == 0 and abs(f.calc_center_median().x) < 0.30 and f.calc_center_median().z < 1.545 + 0.62 * (f.calc_center_median().y + 0.08)], context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(base.data); bm.free()
    return base

def join_all(base, hair_obj, parts, hm, cm):
    me = base.data
    me.materials[1] = hm; me.materials[3] = cm
    me.materials.pop(index=4)
    slots = list(me.materials)
    arm = bpy.data.objects['Adulto_Armature']
    dups = []
    for src in parts + [hair_obj]:
        d = src.copy(); d.data = src.data.copy(); d.name = src.name + '_j'; bpy.context.scene.collection.objects.link(d)
        d.data.materials.clear()
        for m in slots: d.data.materials.append(m)
        idx = 1 if src is hair_obj else 3
        for p in d.data.polygons: p.material_index = idx
        for u in d.data.uv_layers: u.name = 'UVMap'
        d.parent = arm
        if src is hair_obj:
            for vg in list(d.vertex_groups): d.vertex_groups.remove(vg)
            vg = d.vertex_groups.new(name='head'); vg.add(list(range(len(d.data.vertices))), 1.0, 'REPLACE')
            for mm in list(d.modifiers): d.modifiers.remove(mm)
            md = d.modifiers.new('Armature', 'ARMATURE'); md.object = arm
        dups.append(d)
    b2 = base.copy(); b2.data = base.data.copy(); b2.name = 'Amerigo_a3_LOD0'; bpy.context.scene.collection.objects.link(b2)
    for u in b2.data.uv_layers: u.name = 'UVMap'
    assemble_child.sel_only(b2)
    for d in dups: d.select_set(True)
    bpy.context.view_layer.objects.active = b2
    bpy.ops.object.join()
    b2 = bpy.data.objects['Amerigo_a3_LOD0']
    bones = {b.name for b in arm.data.bones}
    for g in [g for g in b2.vertex_groups if g.name not in bones]: b2.vertex_groups.remove(g)
    return b2
