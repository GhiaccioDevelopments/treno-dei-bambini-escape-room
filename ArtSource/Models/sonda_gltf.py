# Sonda: stampa tri, dimensioni e collegamenti texture di ogni glTF di staging.
# Uso: blender -b --factory-startup --python sonda_gltf.py -- <file.gltf> [...]
import bpy, sys
args = sys.argv[sys.argv.index('--')+1:]
def trace(sock):
    if not sock.is_linked: return None
    n = sock.links[0].from_node
    seen = 0
    while n and seen < 10:
        if n.type == 'TEX_IMAGE': return n.image.name if n.image else None
        nxt = None
        for i in n.inputs:
            if i.is_linked:
                nxt = i.links[0].from_node; break
        n = nxt; seen += 1
    return None
for f in args:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=f)
    tris = 0
    mn = [1e9]*3; mx = [-1e9]*3
    for o in bpy.context.scene.objects:
        if o.type != 'MESH': continue
        tris += sum(len(p.vertices)-2 for p in o.data.polygons)
        for c in o.bound_box:
            w = o.matrix_world @ __import__('mathutils').Vector(c)
            for k in range(3): mn[k] = min(mn[k], w[k]); mx[k] = max(mx[k], w[k])
    print("PROBE", f.split('treno-asset-staging')[-1], "tris", tris, "dim", [round(mx[k]-mn[k],3) for k in range(3)], "min", [round(v,3) for v in mn])
    for m in bpy.data.materials:
        b = m.node_tree.nodes.get('Principled BSDF') if m.node_tree else None
        if not b: print("  MAT", m.name, "no bsdf"); continue
        print("  MAT", m.name, "base", trace(b.inputs['Base Color']), "nor", trace(b.inputs['Normal']), "rough", trace(b.inputs['Roughness']), "metal", trace(b.inputs['Metallic']), "basecol", [round(x,2) for x in b.inputs['Base Color'].default_value], "alpha", m.blend_method if hasattr(m,'blend_method') else '')
    for o in bpy.context.scene.objects:
        if o.type=='MESH': print("  OBJ", o.name, len(o.data.polygons), [s.material.name if s.material else None for s in o.material_slots])
