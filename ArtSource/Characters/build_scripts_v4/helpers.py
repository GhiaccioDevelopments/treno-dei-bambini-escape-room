import os
ROOT = os.environ.get("TRENO_ROOT", r".")  # cartella del progetto Il Treno dei Bambini
SCRATCH = os.environ.get("TRENO_SCRATCH", os.path.join(ROOT, "ArtSource", "tmp"))
import bpy, math
SP = SCRATCH

def render(cam, path, hide=None, show_only=None, res=(900,1200), samples=24):
    sc = bpy.context.scene
    sc.camera = bpy.data.objects[cam]
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.filepath = path
    sc.render.image_settings.file_format = 'PNG'
    try:
        sc.eevee.taa_render_samples = samples
    except Exception:
        pass
    old = {}
    if show_only is not None:
        for o in bpy.data.objects:
            if o.type in ('MESH',):
                old[o.name] = o.hide_render
                o.hide_render = o.name not in show_only
    bpy.ops.render.render(write_still=True)
    for n, v in old.items():
        bpy.data.objects[n].hide_render = v

def ptri(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)
