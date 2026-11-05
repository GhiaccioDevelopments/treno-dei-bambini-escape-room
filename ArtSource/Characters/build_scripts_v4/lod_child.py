import bpy, bmesh, importlib
import cloth, hair_child, helpers

def part_copy(src, mat_idx, ratio, name):
    d = src.copy(); d.data = src.data.copy(); d.name = name
    bpy.context.scene.collection.objects.link(d)
    bm = bmesh.new(); bm.from_mesh(d.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != mat_idx], context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(d.data); bm.free()
    for m in list(d.modifiers): d.modifiers.remove(m)
    if ratio < 1.0:
        md = d.modifiers.new('Dec', 'DECIMATE'); md.ratio = ratio; md.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = d
        for ob in bpy.context.selected_objects: ob.select_set(False)
        d.select_set(True)
        bpy.ops.object.modifier_apply(modifier=md.name)
    return d

def make_lod(src_name, new_name, r_skin, r_face, r_cloth, hair_keep, hair_S, hair_widen, cap_tris, hair_builder=None):
    src = bpy.data.objects[src_name]
    parts = [part_copy(src, 0, r_skin, new_name + '_p0'), part_copy(src, 2, r_face, new_name + '_p2'), part_copy(src, 3, r_cloth, new_name + '_p3')]
    hb = hair_builder or hair_child.build_child_hair
    h, n = hb(keep=hair_keep, S=hair_S, widen=hair_widen, cap_tris=cap_tris, name=new_name + '_hair')
    h.data.materials.clear()
    for m in src.data.materials: h.data.materials.append(m)
    for p in h.data.polygons: p.material_index = 1
    for u in h.data.uv_layers: u.name = 'UVMap'
    arm = src.parent
    h.parent = arm
    for vg in list(h.vertex_groups): h.vertex_groups.remove(vg)
    vg = h.vertex_groups.new(name='head'); vg.add(list(range(len(h.data.vertices))), 1.0, 'REPLACE')
    parts.append(h)
    for p in parts:
        p.parent = arm
        for u in p.data.uv_layers: u.name = 'UVMap'
        for m in list(p.modifiers): p.modifiers.remove(m)
    for ob in bpy.context.selected_objects: ob.select_set(False)
    act = parts[0]
    bpy.context.view_layer.objects.active = act
    for p in parts: p.select_set(True)
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = new_name; o.data.name = new_name
    md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    return o
