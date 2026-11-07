"""
R022 - Riesporta le clip Mixamo di Amerigo con il movimento del bacino sulle ossa.

Problema: negli FBX Mixamo (Amerigo_Run/Walk/Idle...) tutto il movimento del bacino (traslazione e rotazione) e' animato sul NODO dell'armatura
(Game_engine), non sull'osso pelvis; il nodo ha anche una rotazione statica (inclinazione in avanti di circa 18 gradi) che Unity salva
nella posa di riferimento (T-pose) dell'avatar Humanoid. Risultato in Unity: posa di riferimento non valida (busto inclinato, ginocchio piegato,
piede destro ribaltato), clip sfalsata/asimmetrica sul modello.

Soluzione: in Blender (headless) il movimento del nodo viene "cotto" nell'osso pelvis, le curve del nodo vengono tolte, il nodo torna a una
trasformazione fissa e pulita, e l'armatura (solo armatura + animazione, niente mesh) viene riesportata in FBX. Sul file riesportato Unity
ricava una posa di riferimento coerente.

Uso (Windows, Blender 5.x):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ArtSource/Characters/riesporta_clip_gerarchia.py -- \
      <clip_in.fbx> <clip_out.fbx> [--pitch-fix GRADI] [--no-yaw-roll-bias]

  --pitch-fix GRADI   toglie GRADI di inclinazione media in avanti del bacino (default 0 = fedele a Mixamo)
  --no-yaw-roll-bias  toglie la media di imbardata e rollio del bacino (resta l'oscillazione), per un busto dritto

Nel file di uscita il fotogramma 0 e' la T-pose di riposo e la corsa va da f0 (di solito 1) a f1.

Il file di uscita puo' sostituire quello d'ingresso (stesso percorso) se il .meta (GUID) resta dov'e'.
"""
import math
import sys

import bpy
from mathutils import Matrix, Quaternion, Euler, Vector

argv = sys.argv[sys.argv.index('--') + 1:]
src, dst = argv[0], argv[1]
pitch_fix = float(argv[argv.index('--pitch-fix') + 1]) if '--pitch-fix' in argv else 0.0
no_bias = '--no-yaw-roll-bias' in argv

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
act = arm.animation_data.action
pel = arm.pose.bones['pelvis']
f0, f1 = int(act.frame_range[0]), int(act.frame_range[1])
sc = bpy.context.scene

# 1) matrice mondo del bacino a ogni fotogramma (nodo animato + posa dell'osso)
worlds = {}
for f in range(f0, f1 + 1):
    sc.frame_set(f)
    bpy.context.view_layer.update()
    worlds[f] = arm.matrix_world.copy() @ pel.matrix.copy()

# 2) trasformazione fissa e pulita del nodo: Z-up con ossa lungo Y (rot X 90), scala invariata
sx = arm.scale[0]
M0 = Matrix.LocRotScale(Vector((0, 0, 0)), Euler((math.radians(90), 0, 0)), Vector((sx, sx, sx)))

# 3) correzioni opzionali (inclinazione/imbardata/rollio medi del bacino) nello spazio dell'armatura pulita
rots = {f: (M0.inverted() @ worlds[f]).to_euler('XYZ') for f in worlds}
mean = [sum(rots[f][i] for f in rots) / len(rots) for i in range(3)]
fixed = {}
for f in worlds:
    P = M0.inverted() @ worlds[f]
    loc, rot, _ = P.decompose()
    e = rot.to_euler('XYZ')
    if pitch_fix:
        e.x -= math.radians(pitch_fix)
    if no_bias:
        e.y -= mean[1]
        e.z -= mean[2]
    fixed[f] = Matrix.LocRotScale(loc, e.to_quaternion(), Vector((1, 1, 1)))

# 4) togli le curve del nodo (location/rotation_euler/scale dell'oggetto)
for layer in act.layers:
    for strip in layer.strips:
        for cb in strip.channelbags:
            for fc in list(cb.fcurves):
                if not fc.data_path.startswith('pose.bones'):
                    cb.fcurves.remove(fc)
arm.matrix_basis = M0

# 5) scrivi il movimento sull'osso pelvis
bpy.context.view_layer.objects.active = arm
pel.rotation_mode = 'QUATERNION'
for f in range(f0, f1 + 1):
    sc.frame_set(f)
    pel.matrix = fixed[f]
    bpy.context.view_layer.update()
    pel.keyframe_insert('location', frame=f)
    pel.keyframe_insert('rotation_quaternion', frame=f)

# 6) fotogramma 0 = posa di riferimento (T-pose di riposo dell'armatura): Unity ricava la T-pose dell'avatar dal primo fotogramma del file,
#    come nei download Mixamo "with skin". La clip utile va da f0 a f1 (in Unity: firstFrame = f0).
for pb in arm.pose.bones:
    pb.rotation_mode = 'QUATERNION'
    pb.location = (0, 0, 0)
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.scale = (1, 1, 1)
    pb.keyframe_insert('location', frame=0)
    pb.keyframe_insert('rotation_quaternion', frame=0)
    pb.keyframe_insert('scale', frame=0)

# 7) esporta solo armatura + animazione
sc.frame_start, sc.frame_end = 0, f1
bpy.ops.object.select_all(action='DESELECT')
arm.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=dst, use_selection=True, object_types={'ARMATURE'},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
    axis_forward='-Z', axis_up='Y', primary_bone_axis='Y', secondary_bone_axis='X',
    apply_scale_options='FBX_SCALE_ALL', mesh_smooth_type='OFF')
print('RIESPORTATA', dst, 'fotogrammi 0 (T-pose) +', f0, f1)
