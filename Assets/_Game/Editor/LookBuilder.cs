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
        const string LutPresente = LutFolder + "/LUT_Presente.png";   // R007: presente piu' freddo e desaturato
        const string ProfEpoca = VolumesFolder + "/VP_Epoca.asset";   // hub e titolo (presente, pioggia)
        const string ProfBasso = VolumesFolder + "/VP_Basso.asset";   // R007: interno del basso (presente)
        const string ProfFlash = VolumesFolder + "/VP_Flashback.asset";
        const string MatFolder = "Assets/Art/Materials";
        const string SkyHub = MatFolder + "/M_Sky_Hub.mat";           // R007: cielo coperto (HDRI CieloCoperto)
        const string SkyRicordo = MatFolder + "/M_Sky_Ricordo.mat";   // R007: cielo procedurale col sole basso
        const string HdriCoperto = "Assets/Art/HDRI/CieloCoperto/CieloCoperto_1k.hdr";
        const string LightingAsset = LightingFolder + "/LS_Treno.lighting";
        /// <summary>R007: bake di bozza (campioni bassi) per tarare il look; l'unico bake finale usa i campioni alti. EditorPrefs Treno.LookBozza.</summary>
        public static bool Bozza { get { return EditorPrefs.GetBool("Treno.LookBozza", false); } set { EditorPrefs.SetBool("Treno.LookBozza", value); } }

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
                || AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfBasso) == null
                || AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QualityFolder + "/URP_Alta.asset") == null)
                CreatePresetsAndProfiles();
            EnsureLightingSettings();

            try
            {
                for (int i = 0; i < LookScenes.Length; i++)
                {
                    string n = LookScenes[i];
                    EditorUtility.DisplayProgressBar("Applica look", n, (i + 0.5f) / LookScenes.Length);
                    string path = ScenesFolder + "/" + n + ".unity";
                    if (!File.Exists(path)) { Debug.LogWarning("[LookBuilder] Scena mancante: " + path); continue; }
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    ApplySceneLook(scene, n, ProfileFor(n), LightingFor(n));
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Debug.Log("[LookBuilder] Look applicato a " + LookScenes.Length + " scene (bake non eseguito: Treno/Look/Bake luci di tutte le scene).");
        }

        /// <summary>R006: applica il look a una sola scena (sviluppo dell'hub). Non fa il bake.</summary>
        public static void ApplyLookToScene(string n)
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfEpoca) == null
                || AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfFlash) == null
                || AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfBasso) == null
                || AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QualityFolder + "/URP_Alta.asset") == null)
                CreatePresetsAndProfiles();
            EnsureLightingSettings();
            string path = ScenesFolder + "/" + n + ".unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ApplySceneLook(scene, n, ProfileFor(n), LightingFor(n));
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LookBuilder] Look applicato a " + n + ".");
        }

        /// <summary>R007 t2: scena di prova dei modelli con la luce e il post-processing dell'hub (cielo coperto, sole debole, nebbia, volume Epoca),
        /// senza le probe dell'hub. Ambiente dal cielo, nessun bake: serve per i confronti "primitiva contro modello".</summary>
        public static void ApplyLookProva(Scene scene)
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfEpoca) == null) CreatePresetsAndProfiles();
            EnsureLightingSettings();
            EnsureSkyMaterials();
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == LookRoot) UnityEngine.Object.DestroyImmediate(root);
            var look = new GameObject(LookRoot);
            var vol = look.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 0f; vol.weight = 1f;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfEpoca);
            Light sun = null;
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (l.type == LightType.Directional) { sun = l; break; }
            SetSun(sun, new Color(0.80f, 0.86f, 0.97f), 0.9f, new Vector3(34f, -14f, 0f), 0.45f);
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            SetSkyAmbient(1.5f);
            var bg = new Color(0.50f, 0.54f, 0.58f);
            SetFog(0.024f, bg);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyHub);
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            DynamicGI.UpdateEnvironment();
            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None;
                cam.backgroundColor = bg;
                cam.clearFlags = CameraClearFlags.Skybox;
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>R006: bake delle luci di una sola scena.</summary>
        public static void BakeScene(string n)
        {
            var scene = EditorSceneManager.OpenScene(ScenesFolder + "/" + n + ".unity", OpenSceneMode.Single);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LookBuilder] Bake " + n + ": " + (ok ? "ok" : "FALLITO") + " in " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s");
        }

        /// <summary>R006: occlusion culling statico (Umbra) della scena aperta.</summary>
        public static void CalcolaOcclusion(string n)
        {
            var scene = EditorSceneManager.OpenScene(ScenesFolder + "/" + n + ".unity", OpenSceneMode.Single);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = StaticOcclusionCulling.Compute();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LookBuilder] Occlusion culling " + n + ": " + (ok ? "ok" : "FALLITO") + " in " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s, dati " + (StaticOcclusionCulling.umbraDataSize / 1024) + " KB");
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

        static VolumeProfile ProfileFor(string n)
        {
            string p = n == "30_FB_Scarpe" ? ProfFlash : n == "20_Int_BassoAntonietta" ? ProfBasso : ProfEpoca;
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(p);
        }

        // ---- R007: bake asincrono a catena (non blocca le chiamate MCP); stato in LookBuilder.BakeStato
        public static string BakeStato = "inattivo";
        static System.Collections.Generic.Queue<string> _coda;
        static System.Diagnostics.Stopwatch _swBake;
        static string _corrente;

        /// <summary>Bake di tutte le scene (o di quelle elencate) una dopo l'altra in modo asincrono. Stato: BakeStato.</summary>
        public static void BakeAllAsync(string[] scene = null)
        {
            if (_coda != null && _coda.Count > 0 || Lightmapping.isRunning) { Debug.LogWarning("[LookBuilder] Bake gia' in corso."); return; }
            _coda = new System.Collections.Generic.Queue<string>(scene ?? LookScenes);
            _swBake = System.Diagnostics.Stopwatch.StartNew();
            BakeStato = "avvio";
            EditorApplication.update += BakeTick;
        }

        static void BakeTick()
        {
            if (Lightmapping.isRunning) return;
            if (_corrente != null)
            {
                var sc = EditorSceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(sc);
                Debug.Log("[LookBuilder] Bake " + _corrente + " finito (" + _swBake.Elapsed.TotalSeconds.ToString("0") + " s dall'inizio).");
                _corrente = null;
            }
            if (_coda.Count == 0)
            {
                EditorApplication.update -= BakeTick;
                AssetDatabase.SaveAssets();
                BakeStato = "finito in " + _swBake.Elapsed.TotalSeconds.ToString("0") + " s";
                Debug.Log("[LookBuilder] Bake di tutte le scene completato: " + BakeStato);
                return;
            }
            _corrente = _coda.Dequeue();
            BakeStato = "in corso: " + _corrente;
            EditorSceneManager.OpenScene(ScenesFolder + "/" + _corrente + ".unity", OpenSceneMode.Single);
            if (!Lightmapping.BakeAsync()) { Debug.LogError("[LookBuilder] BakeAsync fallito: " + _corrente); _corrente = null; }
        }

        // ================================================================== LUT

        static void CreateLuts()
        {
            WriteLut(LutNeo, Neorealismo);
            WriteLut(LutFlash, Flashback);
            WriteLut(LutPresente, Presente);
        }

        /// <summary>R007: presente a Napoli. Desaturato ~45%, ombre fredde blu-verde, medi neutri-freddi, alti appena caldi (le lampade restano calde), nero poco sollevato.</summary>
        static Vector3 Presente(Vector3 c)
        {
            float l = Luma(c);
            c = Lerp3(new Vector3(l, l, l), c, 0.68f);
            c = new Vector3(Mathf.Lerp(c.x, Smooth(0, 1, c.x), 0.35f),
                            Mathf.Lerp(c.y, Smooth(0, 1, c.y), 0.35f),
                            Mathf.Lerp(c.z, Smooth(0, 1, c.z), 0.35f));
            l = Luma(c);
            float sh = 1f - Smooth(0.0f, 0.50f, l);
            c += new Vector3(-0.016f, 0.006f, 0.026f) * sh;
            float mid = Smooth(0.12f, 0.50f, l) * (1f - Smooth(0.62f, 0.92f, l));
            c = new Vector3(c.x * Mathf.Lerp(1f, 0.965f, mid), c.y * Mathf.Lerp(1f, 1.00f, mid), c.z * Mathf.Lerp(1f, 1.045f, mid));
            float hi = Smooth(0.65f, 1.0f, l);
            c = new Vector3(c.x * Mathf.Lerp(1f, 1.035f, hi), c.y, c.z * Mathf.Lerp(1f, 0.955f, hi));
            const float lift = 0.030f, top = 0.965f;
            c = new Vector3(lift + c.x * (top - lift), lift + c.y * (top - lift), lift + c.z * (top - lift));
            return new Vector3(Mathf.Clamp01(c.x), Mathf.Clamp01(c.y), Mathf.Clamp01(c.z));
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
            var lutP = AssetDatabase.LoadAssetAtPath<Texture2D>(LutPresente);
            var lutF = AssetDatabase.LoadAssetAtPath<Texture2D>(LutFlash);

            // Presente (hub, titolo): freddo, desaturato, ACES, grana sottile, bloom morbido
            BuildProfile(ProfEpoca, lutP, 1f, exposure: 0.45f, contrast: 8f, sat: -6f, temp: -6f, tint: 2f,
                         vigColor: Color.black, vigInt: 0.28f, vigSmooth: 0.45f,
                         grain: FilmGrainLookup.Thin1, grainInt: 0.20f, bloomThr: 1.0f, bloomInt: 0.20f, bloomScatter: 0.65f,
                         bloomTint: new Color(1f, 0.88f, 0.72f));
            // Basso (presente, interno): lampade calde nel buio, luce grigia dalle aperture
            BuildProfile(ProfBasso, lutP, 0.9f, exposure: 0.35f, contrast: 6f, sat: 0f, temp: 4f, tint: 0f,
                         vigColor: Color.black, vigInt: 0.30f, vigSmooth: 0.45f,
                         grain: FilmGrainLookup.Thin2, grainInt: 0.22f, bloomThr: 0.9f, bloomInt: 0.28f, bloomScatter: 0.7f,
                         bloomTint: new Color(1f, 0.82f, 0.60f));
            // Ricordo 1946: sole basso dorato, seppia marcato, bordi sfumati, alone morbido sulle luci
            BuildProfile(ProfFlash, lutF, 1f, exposure: 0.85f, contrast: 6f, sat: 0f, temp: 6f, tint: 0f,
                         vigColor: new Color(0.08f, 0.05f, 0.03f), vigInt: 0.38f, vigSmooth: 0.60f,
                         grain: FilmGrainLookup.Medium1, grainInt: 0.35f, bloomThr: 0.85f, bloomInt: 0.40f, bloomScatter: 0.75f,
                         bloomTint: new Color(1f, 0.86f, 0.62f));
        }

        /// <summary>R007: profilo con ACES, color grading (LUT d'epoca + regolazioni), vignetta, grana, bloom e profondita' di campo spenta (solo cutscene).</summary>
        static void BuildProfile(string path, Texture2D lut, float lutContribution, float exposure, float contrast, float sat, float temp, float tint,
                                 Color vigColor, float vigInt, float vigSmooth, FilmGrainLookup grain, float grainInt,
                                 float bloomThr, float bloomInt, float bloomScatter, Color bloomTint)
        {
            var p = ResetProfile(path);
            var tm = p.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.ACES);
            var ca = p.Add<ColorAdjustments>(true);
            ca.postExposure.Override(exposure);
            ca.contrast.Override(contrast);
            ca.saturation.Override(sat);
            var cl = p.Add<ColorLookup>(true);
            cl.texture.Override(lut);
            cl.contribution.Override(lutContribution);
            var wb = p.Add<WhiteBalance>(true);
            wb.temperature.Override(temp);
            wb.tint.Override(tint);
            var vg = p.Add<Vignette>(true);
            vg.color.Override(vigColor);
            vg.intensity.Override(vigInt);
            vg.smoothness.Override(vigSmooth);
            vg.rounded.Override(false);
            var fg = p.Add<FilmGrain>(true);
            fg.type.Override(grain);
            fg.intensity.Override(grainInt);
            fg.response.Override(0.8f);
            var bl = p.Add<Bloom>(true);
            bl.threshold.Override(bloomThr);
            bl.intensity.Override(bloomInt);
            bl.scatter.Override(bloomScatter);
            bl.tint.Override(bloomTint);
            bl.highQualityFiltering.Override(false);
            // profondita' di campo: predisposta ma spenta in gioco; la accende GraphicsPresets.CutsceneDepthOfField
            var dof = p.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(4f);
            dof.focalLength.Override(50f);
            dof.aperture.Override(5.6f);
            dof.active = false;
            SaveProfile(p);
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
            var rdMedia = CopyIfMissing<ScriptableRendererData>(pcRd, QualityFolder + "/URP_Renderer_Media.asset"); // R007: Forward con SSAO leggera
            var rdAlta = CopyIfMissing<ScriptableRendererData>(pcRd, QualityFolder + "/URP_Renderer_Alta.asset");
            var rdUltra = CopyIfMissing<ScriptableRendererData>(pcRd, QualityFolder + "/URP_Renderer_Ultra.asset");

            var bassa = CopyIfMissing<UniversalRenderPipelineAsset>(mobRp, QualityFolder + "/URP_Bassa.asset");
            var media = CopyIfMissing<UniversalRenderPipelineAsset>(mobRp, QualityFolder + "/URP_Media.asset");
            var alta = CopyIfMissing<UniversalRenderPipelineAsset>(pcRp, QualityFolder + "/URP_Alta.asset");
            var ultra = CopyIfMissing<UniversalRenderPipelineAsset>(pcRp, QualityFolder + "/URP_Ultra.asset");

            // R007: ombre per preset (distanza, cascate, soft) negli asset URP.
            // ---- Bassa: render scale 0.8, LDR, niente MSAA/SSAO, ombre hard 1 cascata ~25 m
            ConfigRp(bassa, rdLite, hdr: false, msaa: 1, scale: 0.8f, shadowDist: 25f, cascades: 1, shadowRes: 1024,
                     soft: false, softQ: 1, depth: false, opaque: false, forwardPlusNote: false, split2: 0.3f, split3: new Vector2(0.1f, 0.3f), split4: new Vector3(0.07f, 0.2f, 0.45f), bias: 1.2f);
            // ---- Media: LDR, ombre soft 2 cascate ~40 m, SSAO leggera (FXAA sulla camera da GraphicsPresets)
            ConfigRp(media, rdMedia, hdr: false, msaa: 1, scale: 1f, shadowDist: 40f, cascades: 2, shadowRes: 1024,
                     soft: true, softQ: 1, depth: true, opaque: false, forwardPlusNote: false, split2: 0.25f, split3: new Vector2(0.1f, 0.3f), split4: new Vector3(0.07f, 0.2f, 0.45f), bias: 1.1f);
            // ---- Alta: HDR, MSAA 2x, ombre soft 3 cascate ~60 m, SSAO
            ConfigRp(alta, rdAlta, hdr: true, msaa: 2, scale: 1f, shadowDist: 60f, cascades: 3, shadowRes: 2048,
                     soft: true, softQ: 2, depth: true, opaque: false, forwardPlusNote: true, split2: 0.2f, split3: new Vector2(0.08f, 0.25f), split4: new Vector3(0.07f, 0.2f, 0.45f), bias: 1.0f);
            // ---- Ultra: HDR, MSAA 4x, ombre 4 cascate ~90 m a risoluzione piu' alta, SSAO alta qualita'
            ConfigRp(ultra, rdUltra, hdr: true, msaa: 4, scale: 1f, shadowDist: 90f, cascades: 4, shadowRes: 4096,
                     soft: true, softQ: 3, depth: true, opaque: false, forwardPlusNote: true, split2: 0.2f, split3: new Vector2(0.08f, 0.25f), split4: new Vector3(0.05f, 0.15f, 0.40f), bias: 0.9f);

            // SSAO: Media leggera (meta' risoluzione, campioni minimi), Alta media, Ultra piena. Mai su Bassa / Mobile_RPAsset.
            SetRendererMode(rdMedia, 0); // Forward (la copia di PC_Renderer e' Forward+)
            ConfigSsao(rdMedia, downsample: true, samples: 2, blur: 0, intensity: 0.45f, radius: 0.28f);
            ConfigSsao(rdAlta, downsample: true, samples: 1, blur: 1, intensity: 0.50f, radius: 0.30f);
            ConfigSsao(rdUltra, downsample: false, samples: 0, blur: 0, intensity: 0.60f, radius: 0.35f);

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
                             bool forwardPlusNote, float split2, Vector2 split3, Vector3 split4, float bias)
        {
            var so = new SerializedObject(rp);
            SetBool(so, "m_SupportsHDR", hdr);
            SetInt(so, "m_MSAA", msaa);
            SetFloat(so, "m_RenderScale", scale);
            SetFloat(so, "m_ShadowDistance", shadowDist);
            SetInt(so, "m_ShadowCascadeCount", cascades);
            SetInt(so, "m_MainLightShadowmapResolution", shadowRes);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetFloat(so, "m_Cascade2Split", split2);
            var sp3 = P(so, "m_Cascade3Split"); if (sp3 != null) sp3.vector2Value = split3;
            var sp4 = P(so, "m_Cascade4Split"); if (sp4 != null) sp4.vector3Value = split4;
            SetFloat(so, "m_ShadowDepthBias", bias);
            SetFloat(so, "m_ShadowNormalBias", bias);
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

        static void SetRendererMode(ScriptableRendererData rd, int mode)
        {
            var so = new SerializedObject(rd);
            SetInt(so, "m_RenderingMode", mode);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rd);
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
                e.FindPropertyRelative("realtimeReflectionProbes").boolValue = false;
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

        /// <summary>R006 tappa 3: risoluzione della lightmap (texel per unita') e lato massimo degli atlas per scena, per contenere il peso (obiettivo hub 15-20 MB, basso e flashback <= 10 MB).</summary>
        static void LightmapPerScena(string n, out float res, out int maxSize)
        {
            switch (n)
            {
                case "10_Hub_Vicolo": res = HubLightmapRes; maxSize = 1024; break;
                case "20_Int_BassoAntonietta": res = 12f; maxSize = 1024; break;
                case "30_FB_Scarpe": res = 4f; maxSize = 1024; break;
                default: res = 8f; maxSize = 1024; break;
            }
        }
        const float HubLightmapRes = 4.5f;

        static LightingSettings LightingFor(string n)
        {
            float res; int max;
            LightmapPerScena(n, out res, out max);
            EnsureFolder(LightingFolder);
            string path = LightingFolder + "/LS_Treno_" + n + ".lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (ls == null)
            {
                ls = new LightingSettings { name = "LS_Treno_" + n };
                AssetDatabase.CreateAsset(ls, path);
            }
            ConfiguraLighting(ls);
            ls.lightmapResolution = res;
            ls.lightmapMaxSize = max;
            EditorUtility.SetDirty(ls);
            AssetDatabase.SaveAssetIfDirty(ls);
            return ls;
        }

        static LightingSettings EnsureLightingSettings()
        {
            EnsureFolder(LightingFolder);
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingAsset);
            if (ls == null)
            {
                ls = new LightingSettings { name = "LS_Treno" };
                AssetDatabase.CreateAsset(ls, LightingAsset);
            }
            ConfiguraLighting(ls);
            EditorUtility.SetDirty(ls);
            AssetDatabase.SaveAssetIfDirty(ls);
            return ls;
        }

        static void ConfiguraLighting(LightingSettings ls)
        {
            // Ramo GPU (Unity Compute) DISATTIVATO di proposito (gpu = false): il backend Radeon Rays manda in crash
            // l'editor nel bake delle light probe su questo PC (2026-11-05). Si usa sempre il Progressive CPU,
            // più lento ma stabile; il ramo GPU resta per un eventuale riuso su altre schede.
            bool gpu = false;
#pragma warning disable CS0618
            ls.lightmapper = gpu ? LightingSettings.Lightmapper.UnityComputeGPU : LightingSettings.Lightmapper.ProgressiveCPU;
#pragma warning restore CS0618
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapResolution = 8f;
            ls.indirectResolution = 2f;
            ls.lightmapMaxSize = 1024;
            ls.lightmapPadding = 2;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            if (Bozza)
            {
                // bozza veloce per tarare luci e colori (stessa risoluzione, campioni bassi, niente AO)
                ls.lightmapCompression = LightmapCompression.NormalQuality;
                ls.maxBounces = 2;
                ls.directSampleCount = 16;
                ls.indirectSampleCount = 48;
                ls.environmentSampleCount = 48;
                ls.ao = false;
            }
            else
            {
                // R007: bake finale di qualita' alta (stesso peso: la risoluzione e' decisa da LightmapPerScena)
                ls.lightmapCompression = LightmapCompression.HighQuality;
                ls.maxBounces = 3;
                ls.directSampleCount = 64;
                ls.indirectSampleCount = 256;
                ls.environmentSampleCount = 128;
                ls.ao = true;
                ls.aoMaxDistance = 1.6f;
                ls.aoExponentIndirect = 1.0f;
                ls.aoExponentDirect = 0.5f;
                ls.denoiserTypeDirect = LightingSettings.DenoiserType.OpenImage;
                ls.denoiserTypeIndirect = LightingSettings.DenoiserType.OpenImage;
                ls.denoiserTypeAO = LightingSettings.DenoiserType.OpenImage;
            }
        }

        // ================================================================== applicazione alle scene

        static Color Cct(float kelvin)
        {
            var c = Mathf.CorrelatedColorTemperatureToRGB(kelvin);
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return new Color(c.r / m, c.g / m, c.b / m, 1f);
        }

        /// <summary>R007: materiali skybox. Hub = HDRI coperto Poly Haven (CC0); ricordo = cielo procedurale col sole basso.</summary>
        static void EnsureSkyMaterials()
        {
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(HdriCoperto);
            var hub = AssetDatabase.LoadAssetAtPath<Material>(SkyHub);
            if (hub == null) { hub = new Material(Shader.Find("Skybox/Cubemap")); AssetDatabase.CreateAsset(hub, SkyHub); }
            if (cube != null) hub.SetTexture("_Tex", cube); else Debug.LogWarning("[LookBuilder] HDRI CieloCoperto non trovato: " + HdriCoperto);
            hub.SetColor("_Tint", new Color(0.50f, 0.52f, 0.56f, 1f)); // freddo, appena bluastro
            hub.SetFloat("_Exposure", 1.0f);
            hub.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(hub);

            var ric = AssetDatabase.LoadAssetAtPath<Material>(SkyRicordo);
            if (ric == null) { ric = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(ric, SkyRicordo); }
            ric.SetFloat("_SunSize", 0.06f);
            ric.SetFloat("_SunSizeConvergence", 4f);
            ric.SetFloat("_AtmosphereThickness", 2.2f);   // piu' spessa: sole basso arancio-dorato
            ric.SetColor("_SkyTint", new Color(0.80f, 0.55f, 0.35f, 1f));
            ric.SetColor("_GroundColor", new Color(0.36f, 0.28f, 0.20f, 1f));
            ric.SetFloat("_Exposure", 1.1f);
            EditorUtility.SetDirty(ric);
            AssetDatabase.SaveAssets();
        }

        /// <summary>R007: il pavimento battuto dei cortili leggeva grigio: tono terra calda.</summary>
        static void TintPavimentoBattuto()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_PavimentoBattuto.mat");
            if (m == null) return;
            var c = new Color(1.0f, 0.72f, 0.46f, 1f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            EditorUtility.SetDirty(m);
        }

        static void SetSkyAmbient(float intensity)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = intensity;
        }

        static void ApplySceneLook(Scene scene, string name, VolumeProfile profile, LightingSettings ls)
        {
            // pulizia di un'applicazione precedente
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == LookRoot) UnityEngine.Object.DestroyImmediate(root);
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (l != null && l.gameObject.name == LookLightName) UnityEngine.Object.DestroyImmediate(l.gameObject);

            EnsureSkyMaterials();
            TintPavimentoBattuto();
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyHub);
            bool probesSky = true;
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
                    // R007: tardo pomeriggio d'autunno piovoso. Cielo coperto (HDRI), sole debole e diffuso, ambiente dal cielo,
                    // nebbia fredda di distanza che dissolve i fondali dei vicoli, lampioni e botteghe caldi come unici accenti.
                    SetSun(sun, new Color(0.80f, 0.86f, 0.97f), 0.9f, new Vector3(34f, -14f, 0f), 0.45f);
                    SetSkyAmbient(1.5f);
                    bg = new Color(0.50f, 0.54f, 0.58f);
                    SetFog(0.024f, bg);
                    AddLampLights("LuceLampione", Cct(2500f), 4.5f, 8.5f);
                    AddLampLights("EdicolaLumino", Cct(1900f), 1.2f, 3f);
                    AddLampLights("LuceBottega", Cct(2200f), 5.5f, 10f);
                    AddLampLights("LuceBasso", Cct(2300f), 3.2f, 6.5f);
                    AddLampLights("LuceEdicolaGrande", Cct(1900f), 4.5f, 10f);
                    AddLampLights("LuceCampanile", Cct(2000f), 3.5f, 14f);
                    probeMin = new Vector3(-3.2f, 0.4f, -19f); probeMax = new Vector3(3.2f, 4.0f, 19f); probeStep = 3.2f;
                    break;
                case "01_Titolo":
                    exterior = true;
                    // coerente con l'hub piovoso
                    SetSun(sun, new Color(0.80f, 0.86f, 0.97f), 0.9f, new Vector3(34f, -30f, 0f), 0.45f);
                    SetSkyAmbient(1.5f);
                    bg = new Color(0.50f, 0.54f, 0.58f);
                    SetFog(0.026f, bg);
                    AddLampLights("LuceLampione", Cct(2300f), 4.5f, 7f);
                    probeMin = new Vector3(-1.2f, 0.4f, -6f); probeMax = new Vector3(1.2f, 3.0f, 6f); probeStep = 1.6f;
                    break;
                case "20_Int_BassoAntonietta":
                    // R007: interno buio nel presente. Fuori piove: luce fredda e grigia da finestre e porte (luci baked sulle aperture
                    // + sole debole freddo), lampade a petrolio e candele calde come fonti principali. Il cortile e' a cielo aperto.
                    SetSun(sun, new Color(0.74f, 0.82f, 0.96f), 1.5f, new Vector3(52f, 28f, 0f), 0.6f);
                    SetSkyAmbient(1.3f);
                    bg = new Color(0.07f, 0.06f, 0.055f);
                    SetFog(0.006f, new Color(0.20f, 0.19f, 0.18f));
                    var main = GameObject.Find("Luce Calda");
                    if (main != null)
                    {
                        var ml = main.GetComponent<Light>();
                        ml.color = Color.Lerp(Cct(2600f), Color.white, 0.30f);
                        ml.intensity = 3.6f;
                        ml.range = 8f;
                        ml.shadows = LightShadows.None;
                        ml.lightmapBakeType = LightmapBakeType.Mixed;
                    }
                    AddLampLights("LuminoAltarino", Cct(2200f), 0.7f, 2.8f);
                    AddLampLights("Brace", Cct(2000f), 0.7f, 3.2f);
                    AddLampLights("FiammaLampada", Cct(2300f), 2.2f, 6f);
                    AddLampLights("FiammaCandela", Cct(1900f), 1.0f, 3.4f);
                    probeMin = new Vector3(-4.4f, 0.3f, -2.3f); probeMax = new Vector3(6.2f, 2.4f, 14f); probeStep = 1.4f;
                    probesSky = false;
                    break;
                default: // 30_FB_Scarpe: ricordo 1946, sole basso dorato, ombre lunghe, nessuna pioggia
                    // R007: sole basso (altezza ~22 gradi) lungo l'asse della via, caldo e luminoso; cielo procedurale al tramonto
                    sky = AssetDatabase.LoadAssetAtPath<Material>(SkyRicordo);
                    SetSun(sun, new Color(1.00f, 0.74f, 0.42f), 5.5f, new Vector3(22f, -32f, 0f), 0.95f);
                    SetSkyAmbient(2.3f);
                    bg = new Color(0.62f, 0.47f, 0.30f);
                    SetFog(0.011f, new Color(0.62f, 0.46f, 0.29f));
                    AddLampLights("LuceLampione", Cct(2400f), 2.6f, 7f);
                    AddLampLights("EdicolaLumino", Cct(1900f), 0.9f, 2.5f);
                    AddLampLights("LuceBottega", Cct(2200f), 3.2f, 8f);
                    AddLampLights("LuceBasso", Cct(2300f), 2.0f, 5.5f);
                    exterior = true;
                    probeMin = new Vector3(-17f, 0.4f, -33f); probeMax = new Vector3(20f, 3.6f, 38f); probeStep = 3.2f;
                    break;
            }
            RenderSettings.skybox = sky;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.reflectionIntensity = 1.0f;
            DynamicGI.UpdateEnvironment();

            // camere: post-processing sempre attivo
            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None; // gestito per preset da GraphicsPresets
                cam.backgroundColor = bg;
                cam.clearFlags = CameraClearFlags.Skybox; // R007: cielo vero al posto del colore piatto
            }

            MarkStatic(scene);
            if (name == "10_Hub_Vicolo") AddHubProbes(look.transform, bg);
            else if (name == "20_Int_BassoAntonietta")
            {
                AddFloorProbes(look.transform, probeMin, probeMax, probeStep, new[] { 0.6f, 1.5f, 2.4f }, "Pavimento");
                AddReflectionBox(look.transform, "ReflectionProbe_Stanza", new Vector3(0f, 1.5f, 0.6f), new Vector3(10.5f, 3.4f, 7.5f), bg, false);
                AddReflectionBox(look.transform, "ReflectionProbe_AlcovaCucina", new Vector3(0f, 1.5f, 5.8f), new Vector3(10.5f, 3.4f, 4.6f), bg, false);
                AddWindowLights(look.transform);
                AddReflectionBox(look.transform, "ReflectionProbe_Cortile", new Vector3(3.7f, 2.0f, 11.2f), new Vector3(6.4f, 5.0f, 6.8f), bg);
            }
            else if (name == "30_FB_Scarpe")
            {
                AddFloorProbes(look.transform, probeMin, probeMax, probeStep, new[] { 0.7f, 1.8f, 3.6f }, "Selciato_");
                var cs = new[] { new Vector3(0f, 0f, -27f), new Vector3(0f, 0f, -12f), new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 17f), new Vector3(-6f, 0f, -3f),
                                 new Vector3(-9f, 0f, 14f), new Vector3(9f, 0f, 8f), new Vector3(14f, 0f, -14f), new Vector3(0f, 0.9f, 31f) };
                int k = 0;
                foreach (var c in cs)
                    AddReflectionBox(look.transform, "ReflectionProbe_" + (k++), new Vector3(c.x, c.y + 1.6f, c.z), new Vector3(c.x == 0f && c.z < 20f ? 14f : 16f, 9f, 16f), bg);
            }
            else
            {
                AddProbes(look.transform, probeMin, probeMax, probeStep, exterior);
                AddReflectionProbe(look.transform, probeMin, probeMax, bg, probesSky);
            }

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

        /// <summary>R006: light probe a griglia sul suolo reale del quartiere e reflection probe a riquadri (una per slargo / tratto di via).</summary>
        static void AddHubProbes(Transform parent, Color bg, float step = 2.4f)
        {
            Physics.SyncTransforms();
            var go = new GameObject("LightProbes");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<LightProbeGroup>();
            var pos = new List<Vector3>();
            float[] ys = { 0.7f, 1.8f, 3.6f };
            for (float x = -32f; x <= 32f; x += step)
                for (float z = -46f; z <= 58f; z += step)
                {
                    RaycastHit hit;
                    if (!Physics.Raycast(new Vector3(x, 40f, z), Vector3.down, out hit, 80f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (!hit.collider.gameObject.name.StartsWith("Selciato_")) continue;
                    foreach (var y in ys) pos.Add(new Vector3(x, hit.point.y + y, z));
                }
            g.probePositions = pos.ToArray();
            Debug.Log("[LookBuilder] Hub: " + pos.Count + " light probe.");
            var centri = new[]
            {
                new Vector2(0f, -36f), new Vector2(0f, -17f), new Vector2(0f, 3f), new Vector2(0f, 20f), new Vector2(0f, 38f), new Vector2(0f, 52f),
                new Vector2(-14f, -18f), new Vector2(-15f, 6f), new Vector2(-14f, 16f), new Vector2(-23f, -30f), new Vector2(-19f, 27f),
                new Vector2(16f, -17f), new Vector2(16f, 1f), new Vector2(16f, 16f), new Vector2(19f, 31f)
            };
            int i = 0;
            foreach (var c in centri)
            {
                RaycastHit hit;
                float y = 0f;
                if (Physics.Raycast(new Vector3(c.x, 40f, c.y), Vector3.down, out hit, 80f, ~0, QueryTriggerInteraction.Ignore)) y = hit.point.y;
                var rg = new GameObject("ReflectionProbe_" + (i++));
                rg.transform.SetParent(parent, false);
                rg.transform.position = new Vector3(c.x, y + 1.6f, c.y);
                var rp = rg.AddComponent<ReflectionProbe>();
                rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
                rp.resolution = Bozza ? 64 : 128;
                rp.hdr = true;
                rp.boxProjection = true;
                rp.size = new Vector3(22f, 10f, 22f);
                rp.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
                rp.backgroundColor = bg;
                rp.intensity = 0.8f;
                rp.importance = 1;
            }
        }

        /// <summary>R006: light probe su una griglia che segue il pavimento reale (raycast su collider il cui nome inizia con 'prefisso').</summary>
        static void AddFloorProbes(Transform parent, Vector3 min, Vector3 max, float step, float[] ys, string prefisso)
        {
            Physics.SyncTransforms();
            var go = new GameObject("LightProbes");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<LightProbeGroup>();
            var pos = new List<Vector3>();
            for (float x = min.x; x <= max.x + 0.01f; x += step)
                for (float z = min.z; z <= max.z + 0.01f; z += step)
                {
                    RaycastHit hit;
                    if (!Physics.Raycast(new Vector3(x, 40f, z), Vector3.down, out hit, 80f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (!hit.collider.gameObject.name.StartsWith(prefisso)) continue;
                    foreach (var y in ys) pos.Add(new Vector3(x, hit.point.y + y, z));
                }
            g.probePositions = pos.ToArray();
            Debug.Log("[LookBuilder] " + parent.gameObject.scene.name + ": " + pos.Count + " light probe.");
        }

        /// <summary>R007: luce fredda e grigia del cielo piovoso che entra dalle finestre del basso (spot baked davanti a ogni apertura).</summary>
        static void AddWindowLights(Transform parent)
        {
            var aperture = new[]
            {
                new Vector4(-5.0f, 1.6f, 0.5f, 90f),    // stanza, parete ovest  (guarda verso +x)
                new Vector4(5.0f, 1.6f, 1.0f, 270f),    // stanza, parete est    (guarda verso -x)
                new Vector4(-5.0f, 1.6f, 5.8f, 90f),    // alcova, parete ovest
                new Vector4(5.0f, 1.7f, 5.8f, 270f)     // cucina, parete est
            };
            int i = 0;
            foreach (var a in aperture)
            {
                var go = new GameObject("LuceFinestra_" + (i++));
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(a.x, a.y, a.z);
                go.transform.rotation = Quaternion.Euler(18f, a.w, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.spotAngle = 120f;
                l.innerSpotAngle = 60f;
                l.color = new Color(0.62f, 0.74f, 0.95f);
                l.intensity = 80f;
                l.range = 9f;
                l.shadows = LightShadows.None;
                l.lightmapBakeType = LightmapBakeType.Baked;
            }
        }

        static void AddReflectionBox(Transform parent, string nome, Vector3 centro, Vector3 size, Color bg, bool skybox = true)
        {
            var rg = new GameObject(nome);
            rg.transform.SetParent(parent, false);
            rg.transform.position = centro;
            var rp = rg.AddComponent<ReflectionProbe>();
            rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            rp.resolution = Bozza ? 64 : 128;
            rp.hdr = true;
            rp.boxProjection = true;
            rp.size = size;
            rp.clearFlags = skybox ? UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox : UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            rp.backgroundColor = bg;
            rp.intensity = 0.8f;
            rp.importance = 1;
        }

        static void AddReflectionProbe(Transform parent, Vector3 min, Vector3 max, Color bg, bool skybox = true)
        {
            var go = new GameObject("ReflectionProbe");
            go.transform.SetParent(parent, false);
            var c = (min + max) * 0.5f;
            c.y = 1.5f;
            go.transform.position = c;
            var rp = go.AddComponent<ReflectionProbe>();
            rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            rp.resolution = Bozza ? 64 : 128;
            rp.hdr = true;
            rp.boxProjection = true;
            rp.size = new Vector3(Mathf.Max(8f, (max.x - min.x) + 8f), 8f, Mathf.Max(8f, (max.z - min.z) + 4f));
            rp.clearFlags = skybox ? UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox : UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
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
