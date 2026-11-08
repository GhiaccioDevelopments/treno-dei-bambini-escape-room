using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R023: screenshot di documentazione 1280x720 JPG (fuori da LFS) dal Game view in Play, per il menu iniziale e l'inventario,
    /// e misura degli FPS. Si usa da codice (execute_code / test) durante il Play: R023Foto.Scatta("nome", secondi).
    /// Il Game view puo' essere portato a una risoluzione fissa con ImpostaGameView (per provare 16:9, 16:10 e 4:3).
    /// </summary>
    public static class R023Foto
    {
        public const string Cartella = "Docs/richieste/media/R023";

        sealed class Runner : MonoBehaviour { }

        public static string Ultimo = "";
        public static string UltimoFps = "";

        static Runner NewRunner()
        {
            var go = new GameObject("R023FotoRunner");
            go.hideFlags = HideFlags.HideAndDontSave;
            return go.AddComponent<Runner>();
        }

        /// <summary>Scatta fra "dopo" secondi (tempo reale) e salva Docs/richieste/media/R023/&lt;nome&gt;.jpg (1280x720; se il Game view ha altre proporzioni, riempie e ritaglia al centro).</summary>
        public static void Scatta(string nome, float dopo = 1f, int w = 1280, int h = 720)
        {
            var r = NewRunner();
            r.StartCoroutine(ScattaCo(r, nome, dopo, w, h));
        }

        static IEnumerator ScattaCo(Runner r, string nome, float dopo, int w, int h)
        {
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < dopo) yield return null;
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            // adatta con riempimento (cover) per non deformare
            float sa = shot.width / (float)shot.height, ta = w / (float)h;
            var scale = sa > ta ? new Vector2(ta / sa, 1f) : new Vector2(1f, sa / ta);
            var off = (Vector2.one - scale) * 0.5f;
            Graphics.Blit(shot, rt, scale, off);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var outTex = new Texture2D(w, h, TextureFormat.RGB24, false);
            outTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            outTex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            string path = Path.Combine(Cartella, nome + ".jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, outTex.EncodeToJPG(86));
            Ultimo = path + " (schermo " + shot.width + "x" + shot.height + ")";
            Debug.Log("[R023Foto] " + Ultimo);
            UnityEngine.Object.Destroy(shot); UnityEngine.Object.Destroy(outTex);
            UnityEngine.Object.Destroy(r.gameObject);
        }

        /// <summary>Serie di fotogrammi (per la GIF): n scatti ogni "passo" secondi, nome_000.jpg ... in Docs/richieste/media/R023/frames/.</summary>
        public static void Serie(string prefisso, int n, float passo, float inizio, int w, int h)
        {
            var r = NewRunner();
            r.StartCoroutine(SerieCo(r, prefisso, n, passo, inizio, w, h));
        }

        static IEnumerator SerieCo(Runner r, string prefisso, int n, float passo, float inizio, int w, int h)
        {
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < inizio) yield return null;
            for (int i = 0; i < n; i++)
            {
                float t1 = Time.realtimeSinceStartup;
                yield return new WaitForEndOfFrame();
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                float sa = shot.width / (float)shot.height, ta = w / (float)h;
                var scale = sa > ta ? new Vector2(ta / sa, 1f) : new Vector2(1f, sa / ta);
                Graphics.Blit(shot, rt, scale, (Vector2.one - scale) * 0.5f);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                string dir = Path.Combine(Cartella, "frames");
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, prefisso + "_" + i.ToString("000") + ".jpg"), tex.EncodeToJPG(80));
                UnityEngine.Object.Destroy(shot); UnityEngine.Object.Destroy(tex);
                while (Time.realtimeSinceStartup - t1 < passo) yield return null;
            }
            Ultimo = "serie " + prefisso + " finita";
            UnityEngine.Object.Destroy(r.gameObject);
        }

        /// <summary>Misura gli FPS medi (e il peggiore, il 1% basso) per "secondi" secondi; il risultato finisce in UltimoFps e nel log.</summary>
        public static void MisuraFps(string etichetta, float secondi)
        {
            var r = NewRunner();
            r.StartCoroutine(FpsCo(r, etichetta, secondi));
        }

        static IEnumerator FpsCo(Runner r, string etichetta, float secondi)
        {
            var dt = new System.Collections.Generic.List<float>();
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < secondi)
            {
                yield return null;
                dt.Add(Time.unscaledDeltaTime);
            }
            dt.Sort();
            float sum = 0f; foreach (var d in dt) sum += d;
            float media = dt.Count / Mathf.Max(0.001f, sum);
            int k = Mathf.Max(1, dt.Count / 100);
            float peggio = 0f; for (int i = dt.Count - k; i < dt.Count; i++) peggio += dt[i];
            float low1 = k / Mathf.Max(0.001f, peggio);
            float mediana = 1f / Mathf.Max(0.0001f, dt[dt.Count / 2]);
            UltimoFps = etichetta + ": " + media.ToString("0") + " FPS medi, mediana " + mediana.ToString("0") + ", 1% basso " + low1.ToString("0") + " (" + dt.Count + " fotogrammi)";
            Debug.Log("[R023Foto] " + UltimoFps);
            UnityEngine.Object.Destroy(r.gameObject);
        }

        // ------------------------------------------------------------------ Game view a risoluzione fissa
        /// <summary>Porta il Game view a una risoluzione fissa (w x h). Con (0,0) torna a "Free Aspect".</summary>
        public static void ImpostaGameView(int w, int h)
        {
            var asm = typeof(Editor).Assembly;
            var gvType = asm.GetType("UnityEditor.GameView");
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var enumType = asm.GetType("UnityEditor.GameViewSizeType");
            var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var inst = single.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(inst, new object[] { 0 });
            var gType = group.GetType();
            int index = 0;
            if (w > 0)
            {
                string label = "R023 " + w + "x" + h;
                int total = (int)gType.GetMethod("GetTotalCount").Invoke(group, null);
                index = -1;
                for (int i = 0; i < total; i++)
                {
                    var s = gType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                    if ((string)sizeType.GetProperty("baseText").GetValue(s) == label) { index = i; break; }
                }
                if (index < 0)
                {
                    var ctor = sizeType.GetConstructor(new[] { enumType, typeof(int), typeof(int), typeof(string) });
                    var ns = ctor.Invoke(new object[] { Enum.ToObject(enumType, 1), w, h, label });
                    gType.GetMethod("AddCustomSize").Invoke(group, new[] { ns });
                    index = total;
                }
            }
            var view = EditorWindow.GetWindow(gvType);
            gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(view, new object[] { index, null });
            view.Repaint();
        }
    }
}
