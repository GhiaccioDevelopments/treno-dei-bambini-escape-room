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
    /// Build WebGL (Gzip + decompression fallback, pipeline Mobile_RPAsset).
    /// Menu: Treno/Build WebGL  -  Riga di comando: -executeMethod TrenoBambini.EditorTools.WebGLBuilder.BuildFromCommandLine
    /// </summary>
    public static class WebGLBuilder
    {
        const string OutputFolder = "Build/WebGL";
        const string MobileRpPath = "Assets/Settings/Mobile_RPAsset.asset";

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

            var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(MobileRpPath);
            if (rp == null)
            {
                Debug.LogError("[WebGLBuilder] Render pipeline asset non trovato: " + MobileRpPath);
                return false;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[WebGLBuilder] Il modulo WebGL non e' installato in questo Unity.");
                return false;
            }

            ApplyWebGLSettings();

            // La pipeline Mobile_RPAsset arriva dal livello di qualita "Mobile" (il livello PC e' escluso per WebGL in QualitySettings)
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
