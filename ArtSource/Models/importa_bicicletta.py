# R007 tappa 2 parte B: bicicletta d'epoca sobria da "Low-Poly Vintage Bicycle Model" (wasabicats, CC-BY, 2644 tri).
# Telaio nero, sellino e manopole di cuoio scuro, fanale ottone; i raggi sono due card alpha (scartate: le ruote sono modellate).
# Materiali a colore piatto (nessuna texture). Uso: blender -b --factory-startup --python importa_bicicletta.py
import bpy, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lib_modelli as L

STAGING = os.environ.get('TRENO_STAGING', os.path.abspath(os.path.join(L.RADICE, '..', 'treno-asset-staging')))
SRC = os.path.join(STAGING, 'Bicicletta_alt_wasabicats', 'scene.gltf')
GRUPPI = {
    'BiciNero': ('Material.001', 'Material.002', 'Material.003', 'Material.004', 'Material.005', 'Material.006'),
    'Cuoio': ('Material.007', 'Material.008'),
    'Fanale': ('Material.009', 'material_0'),
}
PIATTI = {
    'BiciNero': {'colore': [0.035, 0.035, 0.04], 'metallico': 0.55, 'levig': 0.5},
    'Cuoio': {'colore': [0.11, 0.065, 0.04], 'metallico': 0.0, 'levig': 0.35},
    'Fanale': {'colore': [0.55, 0.47, 0.30], 'metallico': 0.85, 'levig': 0.55},
}

L.azzera_scena()
bpy.ops.import_scene.gltf(filepath=SRC)
for o in list(bpy.context.scene.objects):
    if o.type == 'MESH' and 'WireWheel' in o.name:
        bpy.data.objects.remove(o, do_unlink=True)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
L.stacca_e_applica(meshes)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
obj = L.unisci(meshes)
nomi_slot = [s.material.name if s.material else '' for s in obj.material_slots]
mappa = {}
for g, (nome, lista) in enumerate(GRUPPI.items()):
    for m in lista:
        mappa[m] = g
for p in obj.data.polygons:
    p.material_index = mappa.get(nomi_slot[p.material_index], 0)
obj.data.materials.clear()
for nome in GRUPPI:
    obj.data.materials.append(bpy.data.materials.new(nome))
L.scala_e_base(obj, 'y', 1.75)
L.finalizza('Bicicletta', obj, sorgente='', extra={'piatti': PIATTI})
