using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R007 tappa 3 (identita' "pellicola neorealista", spec R021 par. 3): aggiunge ai renderer Media/Alta/Ultra il passo a schermo intero
    /// dei ricordi (gate weave, polvere, graffi) con il materiale M_PellicolaRicordi. Il renderer Lite (Bassa/WebGL) non lo riceve.
    /// Grana, alone, vignetta, aberrazione e curva tonale stanno nei Volume profile (LookBuilder.BuildProfile) e si regolano per preset in GraphicsPresets.
    /// Menu: Treno/Look/Aggiungi pellicola ai renderer.
    /// </summary>
    public static class PellicolaBuilder
    {
        const string MatPath = "Assets/Art/Materials/M_PellicolaRicordi.mat";
        const string ShaderName = "Treno/PellicolaRicordi";
        const string FeatureName = "PellicolaRicordi";

        public static Material EnsureMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            var sh = Shader.Find(ShaderName);
            if (sh == null) { Debug.LogError("[Pellicola] Shader mancante: " + ShaderName); return null; }
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, MatPath);
            }
            m.shader = sh;
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Treno/Look/Aggiungi pellicola ai renderer")]
        public static void AddToRenderers()
        {
            var mat = EnsureMaterial();
            if (mat == null) return;
            foreach (var n in new[] { "Media", "Alta", "Ultra" })
            {
                var rd = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Quality/URP_Renderer_" + n + ".asset");
                if (rd == null) { Debug.LogWarning("[Pellicola] Renderer mancante: " + n); continue; }
                AddFeature(rd, mat);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Pellicola] Passo pellicola presente nei renderer Media, Alta e Ultra.");
        }

        public static void AddFeature(ScriptableRendererData rd, Material mat)
        {
            foreach (var f in rd.rendererFeatures)
                if (f != null && f.name == FeatureName)
                {
                    var ex = f as FullScreenPassRendererFeature;
                    if (ex != null) { ex.passMaterial = mat; ex.SetActive(false); EditorUtility.SetDirty(ex); } // spento di default: lo accende GraphicsPresets solo nel ricordo
                    return;
                }
            var feat = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feat.name = FeatureName;
            feat.passMaterial = mat;
            feat.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feat.fetchColorBuffer = true;
            feat.requirements = ScriptableRenderPassInput.None;
            feat.SetActive(false); // acceso a runtime solo nel ricordo (GraphicsPresets)
            AssetDatabase.AddObjectToAsset(feat, rd);
            var so = new SerializedObject(rd);
            var arr = so.FindProperty("m_RendererFeatures");
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = feat;
            var map = so.FindProperty("m_RendererFeatureMap");
            string guid; long id;
            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out guid, out id))
            {
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rd);
        }
    }
}
