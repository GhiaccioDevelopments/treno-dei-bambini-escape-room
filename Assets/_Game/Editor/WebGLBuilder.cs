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
                var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sp, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                bool bakedExpected = sp.Contains("01_Titolo") || sp.Contains("10_Hub") || sp.Contains("20_Int") || sp.Contains("30_FB");
                if (bakedExpected && Lightmapping.GetLightingDataAssetForScene(sc) == null)
                    Debug.LogWarning("[WebGLBuilder] Luci da rifare: Treno ▸ Look ▸ Bake luci di tutte le scene (manca il lighting data di " + sp + ")");
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(sc, true);
            }

            ApplyWebGLSettings();

            // La pipeline URP_Bassa arriva dal livello di qualita "Bassa", default di WebGL in QualitySettings (applicato all avvio da GraphicsPresets)
            BuildReport report;
            try
            {
                string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
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
    }
}
