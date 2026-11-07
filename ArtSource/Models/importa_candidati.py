# R007 tappa 2: porta i modelli candidati (staging) in Assets/Art/Models/<Oggetto>/<Oggetto>.fbx
# con scala reale, origine a terra, decimazione, LOD0/1/2 e materiali vuoti omonimi.
# Le texture le prepara texture_candidati.py (PIL).
# Uso: blender -b --factory-startup --python importa_candidati.py -- [Nome ...]   (senza nomi: tutti)
import bpy, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lib_modelli as L

STAGING = r'C:\Users\rosan\treno-asset-staging'

# nome: dict(src, asse, valore (m), tri (obiettivo LOD0 o None), prefisso (solo oggetti col nome che inizia cosi'))
CAT = {
    'SediaPaglia':    dict(src='SediaPaglia/scene.gltf', asse='z', valore=0.95),
    'LettoFerro':     dict(src='LettoFerro_alt_KuuttiMetalBed/scene.gltf', asse='y', valore=2.0),
    'Credenza':       dict(src='Credenza_alt_painted_wooden_cabinet/painted_wooden_cabinet_2k.gltf', asse='z', valore=1.15),
    'Bancarella':     dict(src='Bancarella/scene.gltf', asse='z', valore=2.3),
    'Bicicletta':     dict(src='Bicicletta/scene.gltf', asse='y', valore=1.75, tri=4500),
    'Pozzo':          dict(src='Pozzo/scene.gltf', asse='z', valore=2.2),
    'Fontana':        dict(src='Fontana_alt_Diccbudd/scene.gltf', asse='x', valore=3.6),
    'PanchinaTufo':   dict(src='Panchina/scene.gltf', asse='x', valore=1.8),
    'Valigia':        dict(src='Valigia/vintage_suitcase_2k.gltf', asse='x', valore=0.62, tri=4000, prefisso='vintage_suitcase_01'),
    'Valigia2':       dict(src='Valigia/vintage_suitcase_2k.gltf', asse='x', valore=0.62, tri=4000, prefisso='vintage_suitcase_02'),
    'Sacco':          dict(src='Sacco/scene.gltf', asse='x', valore=0.85),
    'Tavolino':       dict(src='Tavolino/round_wooden_table_01_2k.gltf', asse='z', valore=0.74, tri=3000),
    'PentolaOttone':  dict(src='Pentole_brass_pot_02/brass_pot_02_2k.gltf', asse='max', valore=0.37),
    'PadellaOttone':  dict(src='Pentole_brass_pan_01/brass_pan_01_2k.gltf', asse='max', valore=0.45),
    'MadonnaStatuetta': dict(src='Madonna/scene.gltf', asse='z', valore=0.9, tri=5000),
}


def lavora(nome, c):
    L.azzera_scena()
    percorso = os.path.join(STAGING, c['src'].replace('/', os.sep))
    bpy.ops.import_scene.gltf(filepath=percorso)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if c.get('prefisso'):
        for o in list(meshes):
            if not o.name.startswith(c['prefisso']):
                bpy.data.objects.remove(o, do_unlink=True)
        meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    L.stacca_e_applica(meshes)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    obj = L.unisci(meshes)
    L.scala_e_base(obj, c['asse'], c['valore'])
    if c.get('uv_box'):
        L.uv_box(obj.data, c['uv_box'])  # modello senza UV: proiezione a scatola, 1 texture ogni c['uv_box'] metri
    return L.finalizza(nome, obj, tri_obiettivo=c.get('tri'), sorgente=os.path.dirname(percorso))


if __name__ == '__main__':
    nomi = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for n in (nomi or list(CAT)):
        lavora(n, CAT[n])
