#if UNITY_EDITOR || DEBUG
// R007 t5: il benchmark esiste solo negli editor e nelle build di sviluppo, mai nella build normale (WebGL compresa).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Core
{
    /// <summary>
    /// R007 tappa 5: benchmark dei frame. Attivo SOLO con l'argomento da riga di comando -benchmark (es. TrenoBambini.exe -benchmark
    /// -screen-width 1280 -screen-height 720 -screen-fullscreen 0 [-benchmarkout file.csv] [-benchmarkpresets 0,1,2,3] [-benchmarkseconds 20]).
    /// Per ogni scena (hub, basso, ricordo) e per ogni preset (Bassa, Media, Alta, Ultra) carica la scena, sposta una camera su un tragitto fisso
    /// per circa 20 s e scrive media, 1% low e massimo del frame time in un CSV; poi chiude il gioco. Vsync spento.
    /// </summary>
    public class BenchmarkRunner : MonoBehaviour
    {
        class Percorso
        {
            public string scena;
            public Vector3[] punti;   // x,z dei vertici (y = quota del suolo + 1,7 m, tranne dove y > 0: quota fissa)
            public float altezza = 1.7f;
        }

        static readonly Percorso[] Percorsi =
        {
            new Percorso { scena = "10_Hub_Vicolo", punti = new[] { new Vector3(0f, 0f, -34f), new Vector3(0f, 0f, -8f), new Vector3(0f, 0f, 14f), new Vector3(0f, 0f, 36f) } },
            new Percorso { scena = "20_Int_BassoAntonietta", altezza = 1.65f, punti = new[] { new Vector3(-3.9f, 0f, -2.6f), new Vector3(0f, 0f, 1f), new Vector3(3.0f, 0f, 4.5f), new Vector3(3.6f, 0f, 12f) } },
            new Percorso { scena = "30_FB_Scarpe", punti = new[] { new Vector3(0f, 0f, -22f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 20f), new Vector3(0f, 0f, 36f) } },
        };

        static readonly string[] NomiPreset = { "Bassa", "Media", "Alta", "Ultra" };

        static readonly string[] Args = Environment.GetCommandLineArgs(); // letti una sola volta

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            foreach (var a in Args)
                if (a == "-benchmark")
                {
                    var go = new GameObject("BenchmarkRunner");
                    DontDestroyOnLoad(go);
                    go.AddComponent<BenchmarkRunner>();
                    return;
                }
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            string dirFoto = Arg("-benchmarkshots", "");
            string outPath = Arg("-benchmarkout", Path.Combine(Application.dataPath, "..", "benchmark.csv"));
            float secondi = float.Parse(Arg("-benchmarkseconds", "20"), CultureInfo.InvariantCulture);
            var presets = new List<int>();
            foreach (var t in Arg("-benchmarkpresets", "0,1,2,3").Split(',')) presets.Add(int.Parse(t));
            File.WriteAllText(outPath, "scena;preset;frame;media_ms;fps_medi;fps_1pct_low;max_ms;gpu;risoluzione\n", Encoding.UTF8);
            yield return new WaitForSecondsRealtime(4f); // Boot e titolo

            foreach (var p in Percorsi)
                foreach (int lv in presets)
                {
                    yield return new WaitForSecondsRealtime(1f);   // pausa e pulizia tra una prova e l'altra (il DecalDrawSystem di URP puo' andare in crash se si cambia scena a raffica)
                    yield return Resources.UnloadUnusedAssets();
                    var op = SceneManager.LoadSceneAsync(p.scena);
                    while (!op.isDone) yield return null;
                    yield return null;
                    foreach (var c in Camera.allCameras) c.enabled = false;
                    foreach (var l in FindObjectsByType<AudioListener>(FindObjectsInactive.Include)) l.enabled = false;
                    var go = new GameObject("CameraBenchmark");
                    var cam = go.AddComponent<Camera>();
                    cam.fieldOfView = 62f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f; cam.clearFlags = CameraClearFlags.Skybox;
                    go.tag = "MainCamera";
                    var data = cam.GetUniversalAdditionalCameraData();
                    data.renderPostProcessing = true; data.renderShadows = true;
                    GraphicsPresets.Apply(lv, false);
                    yield return new WaitForSecondsRealtime(2.5f); // assestamento: shader, probe, particelle

                    // lunghezza totale del tragitto
                    float tot = 0f;
                    for (int i = 1; i < p.punti.Length; i++) tot += Vector3.Distance(p.punti[i - 1], p.punti[i]);
                    var dt = new List<float>(4096);
                    float t = 0f, warm = 1.0f; bool foto = false;
                    while (t < secondi)
                    {
                        float d = Time.unscaledDeltaTime;
                        t += d;
                        float s = Mathf.Clamp01(t / secondi) * tot;
                        Vector3 pos = Vector3.zero, dir = Vector3.forward;
                        float acc = 0f;
                        for (int i = 1; i < p.punti.Length; i++)
                        {
                            float seg = Vector3.Distance(p.punti[i - 1], p.punti[i]);
                            if (s <= acc + seg || i == p.punti.Length - 1)
                            {
                                dir = (p.punti[i] - p.punti[i - 1]).normalized;
                                pos = Vector3.Lerp(p.punti[i - 1], p.punti[i], Mathf.Clamp01((s - acc) / Mathf.Max(seg, 0.01f)));
                                break;
                            }
                            acc += seg;
                        }
                        RaycastHit h;
                        if (Physics.Raycast(new Vector3(pos.x, 60f, pos.z), Vector3.down, out h, 120f, ~0, QueryTriggerInteraction.Ignore)) pos.y = h.point.y + p.altezza;
                        else pos.y = p.altezza;
                        float sway = Mathf.Sin(t * 0.9f) * 12f;                  // la camera si guarda attorno: provoca culling e riflessi diversi
                        go.transform.position = pos;
                        go.transform.rotation = Quaternion.Euler(4f, Quaternion.LookRotation(dir).eulerAngles.y + sway, 0f);
                        if (t > warm) dt.Add(d);
                        if (!foto && dirFoto != "" && t > secondi * 0.5f) { foto = true; Directory.CreateDirectory(dirFoto); ScreenCapture.CaptureScreenshot(Path.Combine(dirFoto, p.scena + "_" + NomiPreset[Mathf.Clamp(lv, 0, 3)] + ".png")); }
                        yield return null;
                    }
                    Scrivi(outPath, p.scena, NomiPreset[Mathf.Clamp(lv, 0, 3)], dt);
                    Destroy(go);
                }
            Application.Quit();
        }

        static void Scrivi(string path, string scena, string preset, List<float> dt)
        {
            if (dt.Count == 0) return;
            var s = new List<float>(dt); s.Sort();
            double somma = 0; foreach (var d in dt) somma += d;
            double media = somma / dt.Count;
            float p99 = s[Mathf.Clamp((int)(s.Count * 0.99f), 0, s.Count - 1)];
            // 1% low = FPS medi sul 1% di frame piu' lenti
            int n1 = Mathf.Max(1, s.Count / 100); double peggiori = 0; for (int i = s.Count - n1; i < s.Count; i++) peggiori += s[i];
            double low1 = n1 / peggiori;
            var ci = CultureInfo.InvariantCulture;
            string riga = scena + ";" + preset + ";" + dt.Count + ";" + (media * 1000).ToString("0.00", ci) + ";" + (1.0 / media).ToString("0.0", ci) + ";" + low1.ToString("0.0", ci)
                          + ";" + (s[s.Count - 1] * 1000).ToString("0.0", ci) + ";" + SystemInfo.graphicsDeviceName + ";" + Screen.width + "x" + Screen.height + "\n";
            File.AppendAllText(path, riga, Encoding.UTF8);
            Debug.Log("[Benchmark] " + riga.Trim());
        }
    }
}
#endif
