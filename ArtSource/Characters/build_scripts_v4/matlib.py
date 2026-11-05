import bpy

def load_img(path, cs):
    for im in list(bpy.data.images):
        if im.filepath == path: bpy.data.images.remove(im)
    im = bpy.data.images.load(path); im.colorspace_settings.name = cs
    return im

def make_pbr_mat(name, albedo, normal, rough, normal_strength=1.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial'); bs = nt.nodes.new('ShaderNodeBsdfPrincipled')
    a = nt.nodes.new('ShaderNodeTexImage'); a.image = load_img(albedo, 'sRGB')
    n = nt.nodes.new('ShaderNodeTexImage'); n.image = load_img(normal, 'Non-Color')
    r = nt.nodes.new('ShaderNodeTexImage'); r.image = load_img(rough, 'Non-Color')
    nm = nt.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value = normal_strength
    nt.links.new(a.outputs['Color'], bs.inputs['Base Color'])
    nt.links.new(n.outputs['Color'], nm.inputs['Color']); nt.links.new(nm.outputs['Normal'], bs.inputs['Normal'])
    nt.links.new(r.outputs['Color'], bs.inputs['Roughness'])
    nt.links.new(bs.outputs[0], out.inputs[0])
    return m

def make_hair_mat(name, albedo, normal, spec=0.25, rough=0.65):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial'); bs = nt.nodes.new('ShaderNodeBsdfPrincipled')
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = load_img(albedo, 'sRGB'); t.image.alpha_mode = 'STRAIGHT'
    tn = nt.nodes.new('ShaderNodeTexImage'); tn.image = load_img(normal, 'Non-Color')
    nm = nt.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value = 0.8
    gt = nt.nodes.new('ShaderNodeMath'); gt.operation = 'GREATER_THAN'; gt.inputs[1].default_value = 0.5
    nt.links.new(t.outputs['Color'], bs.inputs['Base Color'])
    nt.links.new(t.outputs['Alpha'], gt.inputs[0]); nt.links.new(gt.outputs[0], bs.inputs['Alpha'])
    nt.links.new(tn.outputs['Color'], nm.inputs['Color']); nt.links.new(nm.outputs['Normal'], bs.inputs['Normal'])
    bs.inputs['Roughness'].default_value = rough
    if 'Specular IOR Level' in bs.inputs: bs.inputs['Specular IOR Level'].default_value = spec
    nt.links.new(bs.outputs[0], out.inputs[0])
    m.surface_render_method = 'DITHERED'; m.use_backface_culling = False
    return m
