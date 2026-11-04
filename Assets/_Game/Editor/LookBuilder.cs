using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TrenoBambini.Core;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Look d'epoca (Fase 2.1): preset URP Bassa/Media/Alta/Ultra, LUT neorealismo/flashback, volume profile,
    /// nebbia, luci, light probe, reflection probe e bake. Tutto riproducibile da menu e indipendente dalla geometria.
    /// Menu: Treno/Look/...
    /// </summary>
    public static class LookBuilder
    {
        // ------------------------------------------------------------------ percorsi
        const string QualityFolder = "Assets/Settings/Quality";
        const string VolumesFolder = "Assets/Settings/Volumes";
        const string LightingFolder = "Assets/Settings/Lighting";
        const string LutFolder = "Assets/Art/Textures/LUT";
        const string ScenesFolder = "Assets/_Game/Scenes";

        const string LutNeo = LutFolder + "/LUT_Neorealismo.png";
        const string LutFlash = LutFolder + "/LUT_Flashback.png";
        const string ProfEpoca = VolumesFolder + "/VP_Epoca.asset";
        const string ProfFlash = VolumesFolder + "/VP_Flashback.asset";
        const string LightingAsset = LightingFolder + "/LS_Treno.lighting";

        const int LutSize = 32;

        static readonly string[] LookScenes = { "01_Titolo", "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe" };
        const string LookRoot = "Look";
        const string LookLightName = "Look_Luce";

        // ================================================================== menu

        [MenuItem("Treno/Look/Crea preset grafici e profili")]
        public static void CreatePresetsAndProfiles()
        {
            try
            {
                EnsureFolder(QualityFolder);
                EnsureFolder(VolumesFolder);
                EnsureFolder(LightingFolder);
                EnsureFolder(LutFolder);

                EditorUtility.DisplayProgressBar("Look", "LUT d'epoca", 0.1f);
                CreateLuts();
                EditorUtility.DisplayProgressBar("Look", "Volume profile", 0.3f);
                CreateProfiles();
                EditorUtility.DisplayProgressBar("Look", "Preset URP", 0.5f);
                CreateUrpAssets();
                EditorUtility.DisplayProgressBar("Look", "Livelli di qualita", 0.8f);
                ConfigureQualityLevels();
                EnsureLightingSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[LookBuilder] Preset grafici (Bassa/Media/Alta/Ultra), LUT e volume profile creati.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Treno/Look/Applica look alle scene")]
        public static void ApplyLookMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyLookToScenes();
        }

        /// <summary>Applica il look a tutte le scene (chiamato anche da SceneBuilder.BuildAll). Non fa il bake.</summary>
        public static void ApplyLookToScenes()
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfEpoca) == null
                || AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfFlash) == null
                || AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QualityFolder + "/URP_Alta.asset") == null)
                CreatePresetsAndProfiles();
            EnsureLightingSettings();

            var epoca = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfEpoca);
            var flash = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfFlash);
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingAsset);
            try
            {
                for (int i = 0; i < LookScenes.Length; i++)
                {
                    string n = LookScenes[i];
                    EditorUtility.DisplayProgressBar("Applica look", n, (i + 0.5f) / LookScenes.Length);
                    string path = ScenesFolder + "/" + n + ".unity";
                    if (!File.Exists(path)) { Debug.LogWarning("[LookBuilder] Scena mancante: " + path); continue; }
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    ApplySceneLook(scene, n, n == "30_FB_Scarpe" ? flash : epoca, ls);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Debug.Log("[LookBuilder] Look applicato a " + LookScenes.Length + " scene (bake non eseguito: Treno/Look/Bake luci di tutte le scene).");
        }

        [MenuItem("Treno/Look/Bake luci di tutte le scene")]
        public static void BakeAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                foreach (var n in LookScenes)
                {
                    string path = ScenesFolder + "/" + n + ".unity";
                    EditorUtility.DisplayProgressBar("Bake luci", n, 0.5f);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var t0 = sw.Elapsed.TotalSeconds;
                    bool ok = Lightmapping.Bake();
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[LookBuilder] Bake " + n + ": " + (ok ? "ok" : "FALLITO") + " in " + (sw.Elapsed.TotalSeconds - t0).ToString("0.0") + " s");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LookBuilder] Bake di tutte le scene completato in " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s.");
        }

        // ================================================================== LUT

        static void CreateLuts()
        {
            WriteLut(LutNeo, Neorealismo);
            WriteLut(LutFlash, Flashback);
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static Vector3 Lerp3(Vector3 a, Vector3 b, float t) { return a + (b - a) * t; }

        static float Luma(Vector3 c) { return c.x * 0.299f + c.y * 0.587f + c.z * 0.114f; }

        /// <summary>Desaturato ~35%, ombre fredde/olivastre, medi e alti seppia-ocra, nero sollevato, alti compressi.</summary>
        static Vector3 Neorealismo(Vector3 c)
        {
            float l = Luma(c);
            c = Lerp3(new Vector3(l, l, l), c, 0.65f);                                   // desaturazione 35%
            c = new Vector3(Mathf.Lerp(c.x, Smooth(0, 1, c.x), 0.30f),                   // contrasto morbido (S leggera)
                            Mathf.Lerp(c.y, Smooth(0, 1, c.y), 0.30f),
                            Mathf.Lerp(c.z, Smooth(0, 1, c.z), 0.30f));
            l = Luma(c);
            // ombre fredde-olivastre
            float sh = 1f - Smooth(0.0f, 0.45f, l);
            c += new Vector3(-0.006f, 0.014f, 0.010f) * sh;
            // medi e alti caldi seppia/ocra
            float mid = Smooth(0.10f, 0.65f, l);
            c = new Vector3(c.x * Mathf.Lerp(1f, 1.06f, mid), c.y * Mathf.Lerp(1f, 1.00f, mid), c.z * Mathf.Lerp(1f, 0.84f, mid));
            // nero sollevato, alti compressi
            const float lift = 0.050f, top = 0.945f;
            c = new Vector3(lift + c.x * (top - lift), lift + c.y * (top - lift), lift + c.z * (top - lift));
            return new Vector3(Mathf.Clamp01(c.x), Mathf.Clamp01(c.y), Mathf.Clamp01(c.z));
        }

        /// <summary>Seppia molto marcato, quasi monocromo caldo, nero ancora piu' sollevato.</summary>
        static Vector3 Flashback(Vector3 c)
        {
            float l = Luma(c);
            l = Mathf.Lerp(l, Smooth(0, 1, l), 0.35f);
            // rampa seppia: scuro -> medio -> chiaro
            var dark = new Vector3(0.16f, 0.105f, 0.065f);
            var mid = new Vector3(0.60f, 0.43f, 0.26f);
            var light = new Vector3(0.95f, 0.83f, 0.60f);
            Vector3 sepia = l < 0.5f ? Lerp3(dark, mid, l / 0.5f) : Lerp3(mid, light, (l - 0.5f) / 0.5f);
            // un residuo di colore originale (10%) per non appiattire del tutto
            Vector3 gray = new Vector3(l, l, l);
            Vector3 r = Lerp3(sepia, Lerp3(gray, c, 0.45f) * 0.9f + sepia * 0.1f, 0.10f);
            return new Vector3(Mathf.Clamp01(r.x), Mathf.Clamp01(r.y), Mathf.Clamp01(r.z));
        }

        static void WriteLut(string assetPath, Func<Vector3, Vector3> grade)
        {
            int n = LutSize;
            var tex = new Texture2D(n * n, n, TextureFormat.RGBA32, false, false);
            var px = new Color32[n * n * n];
            for (int b = 0; b < n; b++)
                for (int g = 0; g < n; g++)
                    for (int r = 0; r < n; r++)
                    {
                        var c = grade(new Vector3(r / (n - 1f), g / (n - 1f), b / (n - 1f)));
                        px[g * (n * n) + b * n + r] = new Color32(
                            (byte)Mathf.RoundToInt(c.x * 255f), (byte)Mathf.RoundToInt(c.y * 255f), (byte)Mathf.RoundToInt(c.z * 255f), 255);
                    }
            tex.SetPixels32(px);
            tex.Apply(false);
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.isReadable = false;
            imp.maxTextureSize = 1024;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.SaveAndReimport();
        }

        // ================================================================== Volume profile

        static void CreateProfiles()
        {
            var lutN = AssetDatabase.LoadAssetAtPath<Texture2D>(LutNeo);
            var lutF = AssetDatabase.LoadAssetAtPath<Texture2D>(LutFlash);

            var p = ResetProfile(ProfEpoca);
            var tm = p.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);
            var cl = p.Add<ColorLookup>(true);
            cl.texture.Override(lutN);
            cl.contribution.Override(1f);
            var wb = p.Add<WhiteBalance>(true);
            wb.temperature.Override(6f);
            wb.tint.Override(0f);
            var vg = p.Add<Vignette>(true);
            vg.color.Override(Color.black);
            vg.intensity.Override(0.25f);
            vg.smoothness.Override(0.40f);
            vg.rounded.Override(false);
            var fg = p.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Thin1);
            fg.intensity.Override(0.20f);
            fg.response.Override(0.8f);
            var bl = p.Add<Bloom>(true);
            bl.threshold.Override(1.0f);
            bl.intensity.Override(0.18f);
            bl.scatter.Override(0.6f);
            bl.tint.Override(new Color(1f, 0.85f, 0.65f));
            bl.highQualityFiltering.Override(false);
            SaveProfile(p);

            var f = ResetProfile(ProfFlash);
            var tm2 = f.Add<Tonemapping>(true);
            tm2.mode.Override(TonemappingMode.Neutral);
            var cl2 = f.Add<ColorLookup>(true);
            cl2.texture.Override(lutF);
            cl2.contribution.Override(1f);
            var vg2 = f.Add<Vignette>(true);
            vg2.color.Override(new Color(0.08f, 0.05f, 0.03f));
            vg2.intensity.Override(0.50f);
            vg2.smoothness.Override(0.60f);
            vg2.rounded.Override(false);
            var fg2 = f.Add<FilmGrain>(true);
            fg2.type.Override(FilmGrainLookup.Medium1);
            fg2.intensity.Override(0.35f);
            fg2.response.Override(0.8f);
            var bl2 = f.Add<Bloom>(true);
            bl2.threshold.Override(0.9f);
            bl2.intensity.Override(0.30f);
            bl2.scatter.Override(0.7f);
            bl2.tint.Override(new Color(1f, 0.88f, 0.68f));
            bl2.highQualityFiltering.Override(false);
            SaveProfile(f);
        }

        static VolumeProfile ResetProfile(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<VolumeProfile>();
                p.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(p, path);
            }
            else
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (obj is VolumeComponent) UnityEngine.Object.DestroyImmediate(obj, true);
                p.components.Clear();
            }
            return p;
        }

        static void SaveProfile(VolumeProfile p)
        {
            // le componenti aggiunte via Add<T>() vanno registrate come sub-asset
            foreach (var c in p.components)
            {
                if (c == null) continue;
                c.name = c.GetType().Name;
                if (!AssetDatabase.Contains(c)) AssetDatabase.AddObjectToAsset(c, p);
            }
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssetIfDirty(p);
        }

        // ================================================================== URP assets e livelli di qualita

        static T CopyIfMissing<T>(string src, string dst) where T : UnityEngine.Object
        {
            if (AssetDatabase.LoadAssetAtPath<T>(dst) == null)
            {
                if (!AssetDatabase.CopyAsset(src, dst)) Debug.LogError("[LookBuilder] Copia fallita: " + src + " -> " + dst);
                AssetDatabase.ImportAsset(dst);
            }
            return AssetDatabase.LoadAssetAtPath<T>(dst);
        }

        static void CreateUrpAssets()
        {
            const string mobRp = "Assets/Settings/Mobile_RPAsset.asset";
            const string pcRp = "Assets/Settings/PC_RPAsset.asset";
            const string mobRd = "Assets/Settings/Mobile_Renderer.asset";
            const string pcRd = "Assets/Settings/PC_Renderer.asset";

            // renderer: leggero (senza SSAO) per Bassa/Media, con SSAO per Alta e Ultra (copie separate)
            var rdLite = CopyIfMissing<ScriptableRendererData>(mobRd, QualityFolder + "/URP_Renderer_Lite.asset");
            var rdAlta = CopyIfMissing<ScriptableRendererData>(pcRd, QualityFolder + "/URP_Renderer_Alta.asset");
            var rdUltra = CopyIfMissing<ScriptableRendererData>(pcRd, QualityFolder + "/URP_Renderer_Ultra.asset");

            var bassa = CopyIfMissing<UniversalRenderPipelineAsset>(mobRp, QualityFolder + "/URP_Bassa.asset");
            var media = CopyIfMissing<UniversalRenderPipelineAsset>(mobRp, QualityFolder + "/URP_Media.asset");
            var alta = CopyIfMissing<UniversalRenderPipelineAsset>(pcRp, QualityFolder + "/URP_Alta.asset");
            var ultra = CopyIfMissing<UniversalRenderPipelineAsset>(pcRp, QualityFolder + "/URP_Ultra.asset");

            // ---- Bassa: render scale 0.8, LDR, niente MSAA/SSAO, ombre hard 1 cascata ~25 m
            ConfigRp(bassa, rdLite, hdr: false, msaa: 1, scale: 0.8f, shadowDist: 25f, cascades: 1, shadowRes: 1024,
                     soft: false, softQ: 1, depth: false, opaque: false, forwardPlusNote: false);
            // ---- Media: LDR, ombre soft 2 cascate ~40 m (FXAA sulla camera da GraphicsPresets)
            ConfigRp(media, rdLite, hdr: false, msaa: 1, scale: 1f, shadowDist: 40f, cascades: 2, shadowRes: 1024,
                     soft: true, softQ: 1, depth: false, opaque: false, forwardPlusNote: false);
            // ---- Alta: HDR, MSAA 2x, ombre soft 2 cascate ~60 m, SSAO leggera
            ConfigRp(alta, rdAlta, hdr: true, msaa: 2, scale: 1f, shadowDist: 60f, cascades: 2, shadowRes: 2048,
                     soft: true, softQ: 2, depth: true, opaque: false, forwardPlusNote: true);
            // ---- Ultra: HDR, MSAA 4x, ombre 4 cascate ~90 m a risoluzione piu' alta, SSAO alta qualita'
            ConfigRp(ultra, rdUltra, hdr: true, msaa: 4, scale: 1f, shadowDist: 90f, cascades: 4, shadowRes: 4096,
                     soft: true, softQ: 3, depth: true, opaque: false, forwardPlusNote: true);

            // SSAO: Alta leggera (meta' risoluzione, pochi campioni), Ultra piena
            ConfigSsao(rdAlta, downsample: true, samples: 2, blur: 1, intensity: 0.40f, radius: 0.30f);
            ConfigSsao(rdUltra, downsample: false, samples: 0, blur: 0, intensity: 0.50f, radius: 0.35f);

            // Renderer: Bassa/Media Forward, Alta/Ultra Forward+ (gia' cosi' nelle copie)
            var gs = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QualityFolder + "/URP_Alta.asset");
            GraphicsSettings.defaultRenderPipeline = gs;
        }

        static SerializedProperty P(SerializedObject so, string name)
        {
            var p = so.FindProperty(name);
            if (p == null) Debug.LogWarning("[LookBuilder] Proprieta' non trovata: " + so.targetObject.name + "." + name);
            return p;
        }
        static void SetBool(SerializedObject so, string n, bool v) { var p = P(so, n); if (p != null) p.boolValue = v; }
        static void SetInt(SerializedObject so, string n, int v) { var p = P(so, n); if (p != null) p.intValue = v; }
        static void SetFloat(SerializedObject so, string n, float v) { var p = P(so, n); if (p != null) p.floatValue = v; }

        static void ConfigRp(UniversalRenderPipelineAsset rp, ScriptableRendererData rd, bool hdr, int msaa, float scale,
                             float shadowDist, int cascades, int shadowRes, bool soft, int softQ, bool depth, bool opaque,
                             bool forwardPlusNote)
        {
            var so = new SerializedObject(rp);
            SetBool(so, "m_SupportsHDR", hdr);
            SetInt(so, "m_MSAA", msaa);
            SetFloat(so, "m_RenderScale", scale);
            SetFloat(so, "m_ShadowDistance", shadowDist);
            SetInt(so, "m_ShadowCascadeCount", cascades);
            SetInt(so, "m_MainLightShadowmapResolution", shadowRes);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetBool(so, "m_AdditionalLightShadowsSupported", false);
            SetBool(so, "m_SoftShadowsSupported", soft);
            SetInt(so, "m_SoftShadowQuality", softQ);
            SetBool(so, "m_RequireDepthTexture", depth);
            SetBool(so, "m_RequireOpaqueTexture", opaque);
            SetBool(so, "m_MixedLightingSupported", true);
            SetBool(so, "m_SupportsLightLayers", false);
            SetBool(so, "m_SupportsLightCookies", false);
            SetBool(so, "m_UseAdaptivePerformance", false);
            SetInt(so, "m_ColorGradingMode", 0);     // LDR: la LUT d'epoca e' pensata per questo
            SetInt(so, "m_ColorGradingLutSize", 32); // = LutSize
            SetBool(so, "m_ReflectionProbeBlending", true);
            SetBool(so, "m_ReflectionProbeBoxProjection", true);
            SetInt(so, "m_AdditionalLightsRenderingMode", 1);   // per pixel (poche luci)
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 4);
            var list = P(so, "m_RendererDataList");
            if (list != null)
            {
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = rd;
            }
            SetInt(so, "m_DefaultRendererIndex", 0);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rp);
        }

        static void ConfigSsao(ScriptableRendererData rd, bool downsample, int samples, int blur, float intensity, float radius)
        {
            foreach (var f in rd.rendererFeatures)
            {
                if (f == null || f.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                var so = new SerializedObject(f);
                SetBool(so, "m_Settings.Downsample", downsample);
                SetInt(so, "m_Settings.Samples", samples);
                SetInt(so, "m_Settings.BlurQuality", blur);
                SetFloat(so, "m_Settings.Intensity", intensity);
                SetFloat(so, "m_Settings.Radius", radius);
                SetBool(so, "m_Active", true);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(f);
            }
            EditorUtility.SetDirty(rd);
        }

        // ---- livelli di qualita' (ProjectSettings/QualitySettings.asset)

        sealed class Level
        {
            public string name, rp;
            public int pixelLights, shadows, shadowRes, cascades, aniso;
            public float shadowDist, lodBias;
        }

        static void ConfigureQualityLevels()
        {
            var levels = new[]
            {
                new Level { name = "Bassa", rp = "URP_Bassa", pixelLights = 1, shadows = 1, shadowRes = 1, cascades = 1, shadowDist = 25f, lodBias = 0.8f, aniso = 1 },
                new Level { name = "Media", rp = "URP_Media", pixelLights = 2, shadows = 2, shadowRes = 1, cascades = 2, shadowDist = 40f, lodBias = 1.0f, aniso = 1 },
                new Level { name = "Alta",  rp = "URP_Alta",  pixelLights = 3, shadows = 2, shadowRes = 2, cascades = 2, shadowDist = 60f, lodBias = 1.5f, aniso = 2 },
                new Level { name = "Ultra", rp = "URP_Ultra", pixelLights = 4, shadows = 2, shadowRes = 3, cascades = 4, shadowDist = 90f, lodBias = 2.0f, aniso = 2 },
            };

            var all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (all == null || all.Length == 0) { Debug.LogError("[LookBuilder] QualitySettings.asset non leggibile."); return; }
            var so = new SerializedObject(all[0]);
            var arr = so.FindProperty("m_QualitySettings");
            arr.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                var l = levels[i];
                e.FindPropertyRelative("name").stringValue = l.name;
                e.FindPropertyRelative("pixelLightCount").intValue = l.pixelLights;
                e.FindPropertyRelative("shadows").intValue = l.shadows;
                e.FindPropertyRelative("shadowResolution").intValue = l.shadowRes;
                e.FindPropertyRelative("shadowCascades").intValue = l.cascades;
                e.FindPropertyRelative("shadowDistance").floatValue = l.shadowDist;
                e.FindPropertyRelative("anisotropicTextures").intValue = l.aniso;
                e.FindPropertyRelative("antiAliasing").intValue = 0; // l'MSAA lo decide l'asset URP
                e.FindPropertyRelative("lodBias").floatValue = l.lodBias;
                e.FindPropertyRelative("vSyncCount").intValue = 0;
                e.FindPropertyRelative("realtimeReflectionProbes").intValue = 0;
                // WebGL: solo Bassa e Media (Alta/Ultra esclusi; GraphicsPresets mappa i preset per nome)
                var excl = e.FindPropertyRelative("excludedTargetPlatforms");
                bool noWebGL = l.name == "Alta" || l.name == "Ultra";
                excl.arraySize = noWebGL ? 1 : 0;
                if (noWebGL) excl.GetArrayElementAtIndex(0).stringValue = "WebGL";
                var rpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(QualityFolder + "/" + l.rp + ".asset");
                e.FindPropertyRelative("customRenderPipeline").objectReferenceValue = rpAsset;
            }
            so.FindProperty("m_CurrentQuality").intValue = 2;

            // livello di default per piattaforma: Standalone = Alta (2), WebGL = Bassa (0)
            var map = so.FindProperty("m_PerPlatformDefaultQuality");
            if (map != null && map.isArray)
            {
                for (int i = 0; i < map.arraySize; i++)
                {
                    var el = map.GetArrayElementAtIndex(i);
                    var key = el.FindPropertyRelative("first");
                    var val = el.FindPropertyRelative("second");
                    if (key == null || val == null) continue;
                    string k = key.stringValue;
                    if (k == "Standalone") val.intValue = 2;
                    else if (k == "WebGL") val.intValue = 0;
                    else val.intValue = Mathf.Min(val.intValue, levels.Length - 1);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            QualitySettings.SetQualityLevel(2, false);
        }

        // ================================================================== LightingSettings

        static void EnsureLightingSettings()
        {
            EnsureFolder(LightingFolder);
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingAsset);
            if (ls == null)
            {
                ls = new LightingSettings { name = "LS_Treno" };
                AssetDatabase.CreateAsset(ls, LightingAsset);
            }
            // GPU (Unity Compute, in Unity 6.6 sostituisce il Progressive CPU) se la scheda lo consente, altrimenti Progressive CPU
            bool gpu = SystemInfo.supportsComputeShaders && SystemInfo.graphicsMemorySize >= 2048;
#pragma warning disable CS0618
            ls.lightmapper = gpu ? LightingSettings.Lightmapper.UnityComputeGPU : LightingSettings.Lightmapper.ProgressiveCPU;
#pragma warning restore CS0618
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapResolution = 8f;        // texel per unita (ridotto da 12 per il peso LFS)
            ls.indirectResolution = 2f;
            ls.lightmapMaxSize = 1024;
            ls.lightmapPadding = 2;
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.maxBounces = 2;
            ls.directSampleCount = 32;
            ls.indirectSampleCount = 128;
            ls.environmentSampleCount = 64;
            ls.ao = false;
            EditorUtility.SetDirty(ls);
            AssetDatabase.SaveAssetIfDirty(ls);
        }

        // ================================================================== applicazione alle scene

        static Color Cct(float kelvin)
        {
            var c = Mathf.CorrelatedColorTemperatureToRGB(kelvin);
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return new Color(c.r / m, c.g / m, c.b / m, 1f);
        }

        static void ApplySceneLook(Scene scene, string name, VolumeProfile profile, LightingSettings ls)
        {
            // pulizia di un'applicazione precedente
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == LookRoot) UnityEngine.Object.DestroyImmediate(root);
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (l != null && l.gameObject.name == LookLightName) UnityEngine.Object.DestroyImmediate(l.gameObject);

            var look = new GameObject(LookRoot);
            var vol = look.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0f;
            vol.weight = 1f;
            vol.sharedProfile = profile;

            // sole e luci della scena
            Light sun = RenderSettings.sun;
            if (sun == null)
                foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                    if (l.type == LightType.Directional) { sun = l; break; }

            Color bg;
            Vector3 probeMin, probeMax;
            float probeStep;
            bool exterior = false;

            switch (name)
            {
                case "10_Hub_Vicolo":
                    exterior = true;
                    // luce naturale bassa e radente tra i palazzi, ombre lunghe
                    SetSun(sun, new Color(1.00f, 0.82f, 0.58f), 3.0f, new Vector3(34f, -14f, 0f), 0.9f);
                    SetAmbient(new Color(0.58f, 0.54f, 0.50f), new Color(0.40f, 0.36f, 0.31f), new Color(0.22f, 0.19f, 0.15f));
                    bg = new Color(0.66f, 0.57f, 0.45f);
                    SetFog(0.012f, bg);
                    AddLampLights("LuceLampione", Cct(2500f), 3.0f, 7.5f);
                    AddLampLights("EdicolaLumino", Cct(1900f), 0.9f, 2.5f);
                    probeMin = new Vector3(-3.2f, 0.4f, -19f); probeMax = new Vector3(3.2f, 4.0f, 19f); probeStep = 3.2f;
                    break;
                case "01_Titolo":
                    exterior = true;
                    SetSun(sun, new Color(1.00f, 0.80f, 0.55f), 2.0f, new Vector3(35f, -35f, 0f), 0.9f);
                    SetAmbient(new Color(0.42f, 0.38f, 0.34f), new Color(0.27f, 0.23f, 0.19f), new Color(0.12f, 0.10f, 0.08f));
                    bg = new Color(0.52f, 0.43f, 0.32f);
                    SetFog(0.020f, bg);
                    AddLampLights("LuceLampione", Cct(2300f), 3.0f, 6f);
                    probeMin = new Vector3(-1.2f, 0.4f, -6f); probeMax = new Vector3(1.2f, 3.0f, 6f); probeStep = 2.4f;
                    break;
                case "20_Int_BassoAntonietta":
                    // interno buio: un filo di luce di giorno fredda, lampada a petrolio calda come fonte principale
                    SetSun(sun, new Color(0.78f, 0.82f, 0.90f), 0.7f, new Vector3(58f, 200f, 0f), 0.8f);
                    SetAmbient(new Color(0.22f, 0.20f, 0.18f), new Color(0.15f, 0.13f, 0.11f), new Color(0.07f, 0.06f, 0.05f));
                    bg = new Color(0.05f, 0.035f, 0.025f);
                    SetFog(0.008f, new Color(0.14f, 0.10f, 0.07f));
                    var main = GameObject.Find("Luce Calda");
                    if (main != null)
                    {
                        var ml = main.GetComponent<Light>();
                        ml.color = Color.Lerp(Cct(2600f), Color.white, 0.40f);
                        ml.intensity = 3.0f;
                        ml.range = 8f;
                        ml.shadows = LightShadows.None;
                        ml.lightmapBakeType = LightmapBakeType.Mixed;
                    }
                    AddLampLights("LuminoAltarino", Cct(2200f), 0.5f, 2.5f);
                    AddLampLights("Brace", Cct(2000f), 0.5f, 3f);
                    probeMin = new Vector3(-2.6f, 0.3f, -2.1f); probeMax = new Vector3(2.6f, 2.4f, 2.1f); probeStep = 1.3f;
                    break;
                default: // 30_FB_Scarpe: ricordo, seppia coerente
                    SetSun(sun, new Color(1.00f, 0.78f, 0.52f), 1.2f, new Vector3(50f, -50f, 0f), 0.75f);
                    SetAmbient(new Color(0.34f, 0.27f, 0.19f), new Color(0.22f, 0.17f, 0.12f), new Color(0.10f, 0.08f, 0.05f));
                    bg = new Color(0.30f, 0.23f, 0.15f);
                    SetFog(0.020f, new Color(0.34f, 0.26f, 0.17f));
                    probeMin = new Vector3(-4.2f, 0.4f, -4.2f); probeMax = new Vector3(4.2f, 2.8f, 4.2f); probeStep = 2.1f;
                    break;
            }
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.5f;

            // camere: post-processing sempre attivo
            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None; // gestito per preset da GraphicsPresets
                cam.backgroundColor = bg;
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            MarkStatic(scene);
            AddProbes(look.transform, probeMin, probeMax, probeStep, exterior);
            AddReflectionProbe(look.transform, probeMin, probeMax, bg);

            Lightmapping.SetLightingSettingsForScene(scene, ls);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void SetFog(float density, Color color)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = density;
            RenderSettings.fogColor = color;
        }

        static void SetAmbient(Color sky, Color equator, Color ground)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
        }

        static void SetSun(Light sun, Color color, float intensity, Vector3 euler, float shadowStrength)
        {
            if (sun == null) return;
            sun.transform.rotation = Quaternion.Euler(euler);
            sun.color = color;
            sun.intensity = intensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = shadowStrength;
            sun.lightmapBakeType = LightmapBakeType.Mixed; // ombra dinamica per Amerigo + indiretta baked
            RenderSettings.sun = sun;
        }

        /// <summary>Aggiunge una luce Baked puntiforme come figlia di ogni oggetto con quel nome.</summary>
        static void AddLampLights(string objectName, Color color, float intensity, float range)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name != objectName) continue;
                var go = new GameObject(LookLightName);
                go.transform.SetParent(t, false);
                go.transform.localPosition = Vector3.zero;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = Color.Lerp(color, Color.white, 0.40f); // meno satura: il LUT fa il resto
                l.intensity = intensity;
                l.range = range;
                l.shadows = LightShadows.None;
                l.lightmapBakeType = LightmapBakeType.Baked;
            }
        }

        /// <summary>Statico per l'ambiente (lightmap); esclusi oggetti con script (porte, oggetti raccoglibili, Glow, uscite).</summary>
        static void MarkStatic(Scene scene)
        {
            var roots = new HashSet<string> { "Ambiente", "Arredo", "Diorama" };
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!roots.Contains(root.name)) continue;
                var excluded = new HashSet<Transform>();
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    foreach (var t in mb.GetComponentsInChildren<Transform>(true)) excluded.Add(t);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.isStatic = !excluded.Contains(t);
                }
            }
        }

        static void AddProbes(Transform parent, Vector3 min, Vector3 max, float step, bool exterior)
        {
            var go = new GameObject("LightProbes");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<LightProbeGroup>();
            var pos = new List<Vector3>();
            float[] ys = exterior ? new[] { 0.6f, 1.6f, 3.2f } : new[] { 0.5f, 1.4f, 2.4f };
            for (float x = min.x; x <= max.x + 0.01f; x += step)
                for (float z = min.z; z <= max.z + 0.01f; z += step)
                    foreach (var y in ys)
                        if (y >= min.y - 0.01f && y <= max.y + 0.01f) pos.Add(new Vector3(x, y, z));
            g.probePositions = pos.ToArray();
        }

        static void AddReflectionProbe(Transform parent, Vector3 min, Vector3 max, Color bg)
        {
            var go = new GameObject("ReflectionProbe");
            go.transform.SetParent(parent, false);
            var c = (min + max) * 0.5f;
            c.y = 1.5f;
            go.transform.position = c;
            var rp = go.AddComponent<ReflectionProbe>();
            rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            rp.resolution = 128;
            rp.hdr = true;
            rp.boxProjection = true;
            rp.size = new Vector3(Mathf.Max(8f, (max.x - min.x) + 8f), 8f, Mathf.Max(8f, (max.z - min.z) + 4f));
            rp.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            rp.backgroundColor = bg;
            rp.intensity = 0.8f;
            rp.importance = 1;
        }

        // ================================================================== util

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
