using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Build WebGL (Gzip + decompression fallback, preset grafico Bassa (URP_Bassa)).
    /// Menu: Treno/Build WebGL  -  Riga di comando: -executeMethod TrenoBambini.EditorTools.WebGLBuilder.BuildFromCommandLine
    /// </summary>
    public static class WebGLBuilder
    {
        const string OutputFolder = "Build/WebGL";
        const string BassaRpPath = "Assets/Settings/Quality/URP_Bassa.asset"; // preset Bassa (Treno > Look > Crea preset grafici e profili)

        [MenuItem("Treno/Build WebGL")]
        public static void Build()
        {
            Run();
        }

        public static void BuildFromCommandLine()
        {
            bool ok = Run();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static void ApplyWebGLSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
        }

        static bool Run()
        {
            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) scenes.Add(s.path);
            if (scenes.Count == 0)
            {
                Debug.LogError("[WebGLBuilder] Nessuna scena abilitata nei Build Settings. Usa prima Treno > Costruisci tutte le scene.");
                return false;
            }

            var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(BassaRpPath);
            if (rp == null)
            {
                Debug.LogError("[WebGLBuilder] Render pipeline asset non trovato: " + BassaRpPath);
                return false;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[WebGLBuilder] Il modulo WebGL non e' installato in questo Unity.");
                return false;
            }

            // le luci sono baked: senza lightingDataAsset la scena in build risulta scura/piatta
            foreach (var sp in scenes)
            {
                // una scena gia aperta nell editor non va richiusa (sarebbe "the last loaded scene")
                var existing = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sp);
                bool giaAperta = existing.IsValid() && existing.isLoaded;
                var sc = giaAperta ? existing : UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sp, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                bool bakedExpected = sp.Contains("10_Hub") || sp.Contains("20_Int") || sp.Contains("30_FB");
                if (bakedExpected && Lightmapping.GetLightingDataAssetForScene(sc) == null)
                    Debug.LogWarning("[WebGLBuilder] Luci da rifare: Treno ▸ Look ▸ Bake luci di tutte le scene (manca il lighting data di " + sp + ")");
                if (!giaAperta && UnityEditor.SceneManagement.EditorSceneManager.sceneCount > 1)
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(sc, true);
            }

            ApplicaOverrideTextureWebGL();
            ApplyWebGLSettings();

            // Splash: il logo Unity si toglie solo per la build WebGL e poi si ripristina (non tocca Standalone)
            bool splashOriginale = PlayerSettings.SplashScreen.showUnityLogo;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // La pipeline URP_Bassa arriva dal livello di qualita "Bassa", default di WebGL in QualitySettings (applicato all avvio da GraphicsPresets)
            BuildReport report;
            try
            {
                string outPath = System.Environment.GetEnvironmentVariable("TRENO_WEBGL_OUT"); // R007 t5: cartella alternativa fuori dal repo
                if (string.IsNullOrEmpty(outPath)) outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
                Directory.CreateDirectory(outPath);

                var options = new BuildPlayerOptions
                {
                    scenes = scenes.ToArray(),
                    locationPathName = outPath,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.None
                };
                Debug.Log("[WebGLBuilder] Avvio build: " + scenes.Count + " scene -> " + outPath);
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                PlayerSettings.SplashScreen.showUnityLogo = splashOriginale;
                AssetDatabase.SaveAssets();
            }

            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[WebGLBuilder] Build riuscita: " + (summary.totalSize / (1024f * 1024f)).ToString("0.0")
                    + " MB in " + summary.totalTime.TotalSeconds.ToString("0") + " s -> " + summary.outputPath);
                return true;
            }
            Debug.LogError("[WebGLBuilder] Build NON riuscita (" + summary.result + "), errori: " + summary.totalErrors);
            return false;
        }

        // ---------------------------------------------------------------------------------
        // Override texture solo per WebGL (la build Standalone resta invariata).
        // Idempotente: reimporta solo le texture il cui override WebGL non e' gia quello voluto.
        // ---------------------------------------------------------------------------------
        const string PiattaformaWebGL = "WebGL";

        static bool EPuntatoreLfs(string path)
        {
            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists) return true;
                if (fi.Length > 1024) return false;
                using (var sr = new StreamReader(path))
                {
                    string riga = sr.ReadLine();
                    return riga != null && riga.StartsWith("version https://git-lfs");
                }
            }
            catch { return true; }
        }

        [MenuItem("Treno/Applica override texture WebGL")]
        public static void ApplicaOverrideTextureWebGL()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets/Art", "Assets/_Game" });
            int modificate = 0, saltate = 0, invariate = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (EPuntatoreLfs(path)) { saltate++; continue; }
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) { saltate++; continue; }
                    if (imp.textureType == TextureImporterType.Cursor)
                    { saltate++; continue; }

                    var voluto = CalcolaImpostazioniWebGL(imp, path);
                    var attuale = imp.GetPlatformTextureSettings(PiattaformaWebGL);
                    if (attuale.overridden == voluto.overridden && attuale.maxTextureSize == voluto.maxTextureSize
                        && attuale.format == voluto.format && attuale.crunchedCompression == voluto.crunchedCompression
                        && attuale.compressionQuality == voluto.compressionQuality)
                    { invariate++; continue; }

                    imp.SetPlatformTextureSettings(voluto);
                    imp.SaveAndReimport();
                    modificate++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            Debug.Log("[WebGLBuilder] Override texture WebGL: " + modificate + " modificate, " + invariate + " gia a posto, " + saltate + " saltate (LFS/non trovate/cursori).");
        }

        static TextureImporterPlatformSettings CalcolaImpostazioniWebGL(TextureImporter imp, string path)
        {
            var s = imp.GetPlatformTextureSettings(PiattaformaWebGL);
            string nome = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            bool personaggio = path.Contains("/Characters/");
            bool normal = imp.textureType == TextureImporterType.NormalMap || nome.Contains("normal");
            bool mappaSecondaria = nome.Contains("_ms") || nome.Contains("_ao") || nome.EndsWith("ao") || nome.Contains("rough")
                || nome.Contains("metal") || nome.Contains("mask") || nome.Contains("height") || nome.Contains("occlusion");

            s.name = PiattaformaWebGL;
            s.overridden = true;

            // UI/sprite: solo limite a 1024, formato com'e' (leggibilita)
            if (imp.textureType == TextureImporterType.Sprite || imp.textureType == TextureImporterType.GUI)
            {
                s.maxTextureSize = 1024;
                return s;
            }
            // lightmap: limite 1024, formato automatico
            if (imp.textureType == TextureImporterType.Lightmap || path.Contains("Lightmap-") || path.Contains("ReflectionProbe-"))
            {
                s.maxTextureSize = path.Contains("ReflectionProbe-") ? 256 : 1024;
                return s;
            }

            // R019: le piante della minimappa (inchiostro e tratteggio sottile) restano a piena risoluzione e senza crunch; i glifi restano come sono
            if (path.Contains("/Resources/Mappe/"))
            {
                s.maxTextureSize = 1024;
                if (path.EndsWith(".png")) return s;
                s.format = TextureImporterFormat.DXT1;
                s.crunchedCompression = false;
                s.compressionQuality = 50;
                return s;
            }

            if (normal)
            {
                s.maxTextureSize = 512;
                s.format = TextureImporterFormat.DXT5; // le normal non vanno crunchate: artefatti visibili
                s.crunchedCompression = false;
                s.compressionQuality = 50;
                return s;
            }

            if (personaggio) s.maxTextureSize = mappaSecondaria ? 512 : 1024;
            else s.maxTextureSize = mappaSecondaria ? 256 : 512;

            bool alfa = imp.DoesSourceTextureHaveAlpha() && imp.alphaSource != TextureImporterAlphaSource.None;
            s.format = alfa ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT1Crunched;
            s.crunchedCompression = true;
            s.compressionQuality = 50;
            return s;
        }    }
}
