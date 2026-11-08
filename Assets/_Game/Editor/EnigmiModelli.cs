using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R020: dai modelli FBX degli enigmi (Assets/Art/Models/Enigmi, generati da ArtSource/Models/Enigmi/genera_enigmi.py) crea i prefab in
    /// Resources/Modelli/Enigmi, che e' dove MondoFab li cerca (ParteDef.modello). Importazione: mesh senza materiali (li mette MondoFab dal JSON), senza collider.
    /// </summary>
    public static class EnigmiModelli
    {
        const string Fbx = "Assets/Art/Models/Enigmi";
        const string Dest = "Assets/_Game/Resources/Modelli/Enigmi";

        [MenuItem("Treno/Enigmi/Crea prefab dei modelli")]
        public static void CreaPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources/Modelli")) AssetDatabase.CreateFolder("Assets/_Game/Resources", "Modelli");
            if (!AssetDatabase.IsValidFolder(Dest)) AssetDatabase.CreateFolder("Assets/_Game/Resources/Modelli", "Enigmi");
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Fbx }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp != null)
                {
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.addCollider = false;
                    imp.importCameras = false; imp.importLights = false;
                    imp.meshCompression = ModelImporterMeshCompression.Medium;
                    imp.SaveAndReimport();
                }
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (fbx == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                string nome = Path.GetFileNameWithoutExtension(path);
                PrefabUtility.SaveAsPrefabAsset(inst, Dest + "/" + nome + ".prefab");
                Object.DestroyImmediate(inst);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Enigmi] Prefab dei modelli creati: " + n);
        }
    }
}
