import bpy, bmesh, assemble_child, hair_adult

OV = dict(length=lambda loc, g, rng: (0.066 if loc.z > 1.755 else (0.045 if abs(g.az(loc)) < 45 else (0.052 if abs(g.az(loc)) < 120 else 0.064))) * rng.uniform(0.85, 1.15),
          width=0.0120, dmin=0.0108, dmin_border=0.0100, border_band=0.012, yaw_sigma=0.08, lift=0.0, droop=0.9, cols=2, off_layer=0.0020, off_len=0.0010,
          crown=(0, -0.35, 1.62), tip_below=lambda a: 0.002 if a < 50 else (0.006 if a < 130 else 0.02))

def ba(**kw):
    kw.setdefault('ncand', 40000); kw.setdefault('seed', 33)
    ov = dict(OV); ov.update(kw.pop('over2', {}))
    return hair_adult.build_adult_hair(over=ov, **kw)

def replace_hair(objname, builder, **kw):
    v = bpy.data.objects[objname]
    bm = bmesh.new(); bm.from_mesh(v.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index == 1], context='FACES')
    bmesh.ops.delete(bm, geom=[x for x in bm.verts if not x.link_faces], context='VERTS')
    bm.to_mesh(v.data); bm.free()
    o, n = builder(**kw)
    o.data.materials.clear()
    for m in v.data.materials: o.data.materials.append(m)
    for p in o.data.polygons: p.material_index = 1
    for u in o.data.uv_layers: u.name = 'UVMap'
    o.parent = v.parent
    vg = o.vertex_groups.new(name='head'); vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    assemble_child.sel_only(v); o.select_set(True); bpy.context.view_layer.objects.active = v
    bpy.ops.object.join()
    return n
