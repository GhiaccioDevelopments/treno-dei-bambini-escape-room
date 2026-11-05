import bpy, bmesh, math, importlib
from mathutils import Vector
import cloth, trim_child, trim_build, shirt_child, shorts_child, shoe2, shoe_child, helpers

R_SHIRT = (0.0, 0.5, 0.5, 1.0)
R_SHORTS = (0.5, 0.5, 1.0, 1.0)
R_SHOE_L = (0.0, 0.25, 0.25, 0.5)
R_SHOE_R = (0.25, 0.25, 0.5, 0.5)

def sel_only(o):
    for ob in bpy.context.selected_objects: ob.select_set(False)
    bpy.context.view_layer.objects.active = o
    o.select_set(True)

def unwrap_to_rect(o, rect, margin=0.006, angle=82):
    sel_only(o)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin, scale_to_bounds=False)
    bpy.ops.uv.pack_islands(rotate=False, margin=margin)
    bpy.ops.object.mode_set(mode='OBJECT')
    me = o.data
    uvl = me.uv_layers.active
    us = [d.uv.x for d in uvl.data]; vs = [d.uv.y for d in uvl.data]
    u0, u1, v0, v1 = min(us), max(us), min(vs), max(vs)
    s = max(u1 - u0, v1 - v0)
    ru0, rv0, ru1, rv1 = rect
    pad = 0.004
    sc = (min(ru1 - ru0, rv1 - rv0) - 2 * pad) / s
    for d in uvl.data:
        d.uv.x = ru0 + pad + (d.uv.x - u0) * sc
        d.uv.y = rv0 + pad + (d.uv.y - v0) * sc

def rim(o, th):
    cloth.rim_solidify(o, th, -1.0)

def set_slots(o, mats, idx):
    o.data.materials.clear()
    for m in mats: o.data.materials.append(m)
    for p in o.data.polygons: p.material_index = idx

def make_all(mats, hair_obj_name='HairNew_LOD0'):
    """mats: list of 4 materials in slot order [Skin, Hair, Face, Clothes]; returns list of part objects (clothes) - raw names consumed"""
    parts = {}
    # shirt
    sh = bpy.data.objects['Shirt_raw']
    cloth.decimate_obj(sh, target_tris=2300)
    unwrap_to_rect(sh, R_SHIRT)
    rim(sh, 0.0028)
    # shorts
    so = bpy.data.objects['Shorts_raw']
    cloth.decimate_obj(so, target_tris=1150)
    unwrap_to_rect(so, R_SHORTS)
    rim(so, 0.0032)
    parts['Shirt'] = sh; parts['Shorts'] = so
    return parts
