using System.IO;
using TrenoBambini.UI.Fx;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Strumenti di controllo per la UI d'epoca (R001). Le texture sono procedurali (generate da codice a runtime):
    /// questo menu le esporta in PNG per poterle guardare e mostrare senza entrare in Play Mode.
    /// </summary>
    public static class UIArtTools
    {
        const string OutDir = "Docs/richieste/media/R001/texture";

        [MenuItem("Treno/UI/Esporta anteprime texture")]
        public static void ExportPreviews()
        {
            Directory.CreateDirectory(OutDir);
            UIArt.WarmUp();
            int n = 0;
            foreach (var s in UIArt.AllSprites())
            {
                if (s == null) continue;
                SavePng(s.texture, Path.Combine(OutDir, s.name.Replace("UIArt_", "") + ".png"));
                n++;
            }
            SavePng(UIArt.Grain(), Path.Combine(OutDir, "grain.png"));
            SavePng(UIArt.Ruled(), Path.Combine(OutDir, "ruled.png"));
            Debug.Log("UI: esportate " + (n + 2) + " texture in " + OutDir);
            EditorUtility.RevealInFinder(OutDir);
        }

        static void SavePng(Texture2D tex, string path)
        {
            // le texture runtime non sono leggibili: si passa da una RenderTexture
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);
        }
    }
}
