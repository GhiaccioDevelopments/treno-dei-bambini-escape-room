import bpy, bmesh, math, random
import numpy as np
from mathutils import Vector, Matrix, noise
from mathutils.bvhtree import BVHTree

def sstep(a, b, x):
    if a == b: return 0.0
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def ref_bm(name='REF_Body'):
    o = bpy.data.objects[name]
    bm = bmesh.new(); bm.from_mesh(o.data)
    bm.transform(o.matrix_world)
    bm.normal_update()
    return bm

def cut(bm, co, no, clear='inner'):
    """clear='inner' removes geometry BEHIND the plane (opposite side of normal), 'outer' removes in front"""
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, dist=1e-6,
                           clear_inner=(clear == 'inner'), clear_outer=(clear == 'outer'))
    bm.verts.ensure_lookup_table()
    # remove loose
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')

def delete_faces_where(bm, fn):
    fs = [f for f in bm.faces if fn(f.calc_center_median())]
    bmesh.ops.delete(bm, geom=fs, context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')

def split_by_plane(bm, co, no):
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, dist=1e-6)

def keep_largest(bm):
    bm.faces.ensure_lookup_table()
    seen = set(); comps = []
    for f in bm.faces:
        if f.index in seen: continue
        st = [f]; seen.add(f.index); cur = []
        while st:
            c = st.pop(); cur.append(c)
            for e in c.edges:
                for n in e.link_faces:
                    if n.index not in seen: seen.add(n.index); st.append(n)
        comps.append(cur)
    comps.sort(key=len, reverse=True)
    for c in comps[1:]:
        bmesh.ops.delete(bm, geom=c, context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')

def boundary_verts(bm):
    s = set()
    for e in bm.edges:
        if e.is_boundary:
            s.add(e.verts[0]); s.add(e.verts[1])
    return s

def to_obj(bm, name, smooth=True, link=True):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    for p in me.polygons: p.use_smooth = smooth
    o = bpy.data.objects.new(name, me)
    if link: bpy.context.scene.collection.objects.link(o)
    return o

def decimate_obj(o, ratio=None, target_tris=None):
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    if target_tris: ratio = min(1.0, target_tris / max(tris, 1))
    md = o.modifiers.new('Dec', 'DECIMATE'); md.decimate_type = 'COLLAPSE'; md.ratio = ratio; md.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = o
    for ob in bpy.context.selected_objects: ob.select_set(False)
    o.select_set(True)
    bpy.ops.object.modifier_apply(modifier=md.name)

def rim_solidify(o, thickness, offset=-1.0):
    md = o.modifiers.new('Rim', 'SOLIDIFY'); md.thickness = thickness; md.offset = offset
    md.use_rim = True; md.use_rim_only = True
    bpy.context.view_layer.objects.active = o
    for ob in bpy.context.selected_objects: ob.select_set(False)
    o.select_set(True)
    bpy.ops.object.modifier_apply(modifier=md.name)

def transfer_weights(o, ref_name='REF_Body', exclude=()):
    ref = bpy.data.objects[ref_name]
    me = ref.data
    bvh = BVHTree.FromObject(ref, bpy.context.evaluated_depsgraph_get())
    names = [g.name for g in ref.vertex_groups]
    # per-vertex weights dict of ref
    W = []
    for v in me.vertices:
        W.append({g.group: g.weight for g in v.groups})
    for vg in list(o.vertex_groups): o.vertex_groups.remove(vg)
    vgs = {n: o.vertex_groups.new(name=n) for n in names}
    for v in o.data.vertices:
        loc, nrm, fi, d = bvh.find_nearest(v.co)
        poly = me.polygons[fi]
        acc = {}
        tot = 0
        for vi in poly.vertices:
            dist = (me.vertices[vi].co - loc).length
            w = 1.0 / (dist + 1e-4)
            tot += w
            for gi, ww in W[vi].items():
                acc[gi] = acc.get(gi, 0) + ww * w
        for gi, ww in acc.items():
            ww /= tot
            if ww > 0.01:
                vgs[names[gi]].add([v.index], ww, 'REPLACE')
    return names

def add_armature(o, arm='Amerigo_Armature'):
    a = bpy.data.objects[arm]
    o.parent = a
    for m in list(o.modifiers):
        if m.type == 'ARMATURE': o.modifiers.remove(m)
    md = o.modifiers.new('Armature', 'ARMATURE'); md.object = a
