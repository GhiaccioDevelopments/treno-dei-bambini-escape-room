using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>R007 tappa 5: build Windows di sviluppo FUORI dal repo per il benchmark (BenchmarkRunner, argomento -benchmark). Menu: Treno/Build/Windows benchmark.</summary>
    public static class BenchmarkBuilder
    {
        /// <summary>Cartella di uscita: variabile d'ambiente TRENO_BUILD_DIR, altrimenti ../treno-build accanto al repo (fuori da Assets e da git).</summary>
        public static string Cartella
        {
            get
            {
                string d = System.Environment.GetEnvironmentVariable("TRENO_BUILD_DIR");
                if (string.IsNullOrEmpty(d)) d = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "treno-build"));
                return Path.Combine(d, "Win");
            }
        }

        [MenuItem("Treno/Build/Windows benchmark")]
        public static void BuildWindows()
        {
            var scene = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Directory.CreateDirectory(Cartella);
            var opt = new BuildPlayerOptions
            {
                scenes = scene,
                locationPathName = Path.Combine(Cartella, "TrenoBambini.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            };
            var r = BuildPipeline.BuildPlayer(opt).summary;
            Debug.Log("[Benchmark] Build Windows: " + r.result + ", " + (r.totalSize / (1024f * 1024f)).ToString("0.0") + " MB in " + r.totalTime.TotalSeconds.ToString("0") + " s -> " + r.outputPath);
        }
    }
}
