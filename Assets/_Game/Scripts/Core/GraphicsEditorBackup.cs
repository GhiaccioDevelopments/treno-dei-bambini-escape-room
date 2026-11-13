#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrenoBambini.Core
{
    /// <summary>
    /// R008 (solo editor): le impostazioni grafiche cambiano a runtime asset del progetto (asset URP, funzioni dei renderer, QualitySettings).
    /// Prima della prima modifica in Play si salva lo stato originale in SessionState (JSON, sopravvive al ricaricamento del dominio) e lo si
    /// rimette all'uscita dal Play, oppure al primo caricamento dell'editor se il Play e' finito in modo anomalo. Cosi' nessun diff sugli asset.
    /// </summary>
    [InitializeOnLoad]
    public static class GraphicsEditorBackup
    {
        const string Key = "treno_gfx_backup_v1";

        [Serializable] class Voce { public string id; public string json; public bool asset; }
        [Serializable] class Livello { public float lodBias; public int aniso; public int vsync; public int mip; }
        [Serializable] class Dati { public List<Voce> voci = new List<Voce>(); public List<Livello> livelli = new List<Livello>(); public int livelloCorrente; }

        static GraphicsEditorBackup()
        {
            EditorApplication.playModeStateChanged += st =>
            {
                if (st == PlayModeStateChange.ExitingPlayMode) Ripristina();
            };
            // Play finito male (crash, uscita forzata): al caricamento dell'editor fuori dal Play si rimette tutto a posto
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) Ripristina(); };
        }

        /// <summary>Chiamato prima di ogni modifica: salva lo stato originale una sola volta per sessione di Play.</summary>
        public static void Assicura()
        {
            if (!Application.isPlaying || !string.IsNullOrEmpty(SessionState.GetString(Key, ""))) return;
            var d = new Dati();
            var visti = new HashSet<UnityEngine.Object>();
            var rps = new List<UniversalRenderPipelineAsset>();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                var rp = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (rp != null && !rps.Contains(rp)) rps.Add(rp);
            }
            var def = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (def != null && !rps.Contains(def)) rps.Add(def);
            foreach (var rp in rps)
            {
                d.voci.Add(new Voce { id = AssetDatabase.GetAssetPath(rp), json = EditorJsonUtility.ToJson(rp), asset = true });
                var campo = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var lista = campo != null ? campo.GetValue(rp) as ScriptableRendererData[] : null;
                if (lista == null) continue;
                foreach (var rd in lista)
                {
                    if (rd == null) continue;
                    foreach (var f in rd.rendererFeatures)
                        if (f != null && visti.Add(f))
                            d.voci.Add(new Voce { id = GlobalObjectId.GetGlobalObjectIdSlow(f).ToString(), json = EditorJsonUtility.ToJson(f), asset = false });
                }
            }
            // QualitySettings: valori serializzati dei livelli
            var all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (all != null && all.Length > 0)
            {
                var arr = new SerializedObject(all[0]).FindProperty("m_QualitySettings");
                for (int i = 0; i < arr.arraySize; i++)
                {
                    var e = arr.GetArrayElementAtIndex(i);
                    d.livelli.Add(new Livello
                    {
                        lodBias = e.FindPropertyRelative("lodBias").floatValue,
                        aniso = e.FindPropertyRelative("anisotropicTextures").intValue,
                        vsync = e.FindPropertyRelative("vSyncCount").intValue,
                        mip = e.FindPropertyRelative("globalTextureMipmapLimit").intValue
                    });
                }
            }
            d.livelloCorrente = QualitySettings.GetQualityLevel();
            SessionState.SetString(Key, JsonUtility.ToJson(d));
        }

        public static void Ripristina()
        {
            string js = SessionState.GetString(Key, "");
            if (string.IsNullOrEmpty(js)) return;
            SessionState.EraseString(Key);
            try
            {
                var d = JsonUtility.FromJson<Dati>(js);
                foreach (var v in d.voci)
                {
                    UnityEngine.Object o = null;
                    if (v.asset) o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(v.id);
                    else
                    {
                        GlobalObjectId gid;
                        if (GlobalObjectId.TryParse(v.id, out gid)) o = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                    }
                    if (o != null) EditorJsonUtility.FromJsonOverwrite(v.json, o);
                }
                for (int i = 0; i < d.livelli.Count && i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.lodBias = d.livelli[i].lodBias;
                    QualitySettings.anisotropicFiltering = (AnisotropicFiltering)d.livelli[i].aniso;
                    QualitySettings.vSyncCount = d.livelli[i].vsync;
                    QualitySettings.globalTextureMipmapLimit = d.livelli[i].mip;
                }
                QualitySettings.SetQualityLevel(Mathf.Clamp(d.livelloCorrente, 0, QualitySettings.names.Length - 1), false);
                Application.targetFrameRate = -1;
            }
            catch (Exception e) { Debug.LogWarning("[GraphicsEditorBackup] Ripristino non riuscito: " + e.Message); }
        }
    }
}
#endif
