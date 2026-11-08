using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TrenoBambini.Core;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R007: screenshot di documentazione 1280x720 JPG (fuori da LFS) con una camera temporanea nell'editor, senza entrare in Play.
    /// Applica un preset grafico (Bassa/Media/Alta/Ultra) come in gioco. Uso: FotoDocumentazione.Scatta(...).
    /// </summary>
    public static class FotoDocumentazione
    {
        public const string Cartella = "Docs/richieste/media/R007";

        /// <summary>Inquadrature R007 (stesso punto di vista delle foto R006): scena, nome, posizione camera, punto guardato.</summary>
        static readonly object[][] Inquadrature =
        {
            new object[] { "10_Hub_Vicolo", "hub_vico_salita", new Vector3(0f, 1.7f, -20f), new Vector3(0f, 2.2f, 6f) },
            new object[] { "10_Hub_Vicolo", "hub_edicola_votiva", new Vector3(0f, 1.7f, 28f), new Vector3(-7.2f, 5.2f, 37f) },
            new object[] { "10_Hub_Vicolo", "hub_piazzetta_fontanella", new Vector3(4f, 1.7f, -36f), new Vector3(0f, 2.5f, -15f) },
            new object[] { "20_Int_BassoAntonietta", "basso_stanza", new Vector3(-3.9f, 1.65f, -2.6f), new Vector3(1.5f, 1.3f, 2.5f) },
            new object[] { "20_Int_BassoAntonietta", "basso_cortile", new Vector3(3.6f, 2.6f, 8.6f), new Vector3(3.6f, 1.2f, 13f) },
            new object[] { "30_FB_Scarpe", "fb_via_bottega_calzolaio", new Vector3(-4.5f, 1.7f, -1f), new Vector3(-9f, 1.5f, -3.5f) },
            new object[] { "30_FB_Scarpe", "fb_via_piazzetta", new Vector3(3f, 1.7f, 20f), new Vector3(-1f, 1.6f, 32f) },
        };

        /// <summary>Scatta tutte le inquadrature di riferimento con un preset; suffisso "alta"/"bassa"/... nel nome file (t1_nome_suffisso).</summary>
        public static void Serie(int preset, string suffisso, string solo = null, string prefisso = "t1_")
        {
            foreach (var q in Inquadrature)
            {
                string nome = (string)q[1];
                if (solo != null && !System.Array.Exists(solo.Split(' '), t => nome.Contains(t))) continue;
                string scena = (string)q[0];
                string pth = "Assets/_Game/Scenes/" + scena + ".unity";
                if (EditorSceneManager.GetActiveScene().path != pth) EditorSceneManager.OpenScene(pth, OpenSceneMode.Single);
                var pos = (Vector3)q[2];
                if (scena == "10_Hub_Vicolo") // nell'hub il suolo cambia quota: l'altezza dell'occhio e' sopra il selciato
                {
                    Physics.SyncTransforms();
                    foreach (var h in Physics.RaycastAll(new Vector3(pos.x, 60f, pos.z), Vector3.down, 100f))
                        if (h.collider.name.StartsWith("Selciato_")) { pos.y += h.point.y; break; }
                }
                var dir = (Vector3)q[3] - pos;
                var e = Quaternion.LookRotation(dir).eulerAngles;
                float pitch = e.x > 180f ? e.x - 360f : e.x;
                Scatta((string)q[0], pos, e.y, pitch, preset, prefisso + nome + (suffisso == "" ? "" : "_" + suffisso));
            }
        }

        /// <summary>Apre la scena (se non e' gia' quella aperta), applica il preset e salva il JPG. Ritorna il percorso.</summary>
        public static string Scatta(string scena, Vector3 pos, float yaw, float pitch, int preset, string nomeFile, float fov = 62f)
        {
            string path = "Assets/_Game/Scenes/" + scena + ".unity";
            if (EditorSceneManager.GetActiveScene().path != path) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GraphicsPresets.Apply(preset, false);

            var go = new GameObject("FotoCam_Temp");
            try
            {
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 300f;
                cam.clearFlags = CameraClearFlags.Skybox;
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = preset == (int)GraphicsPreset.Media ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
                data.renderShadows = true;

                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                int msaa = preset == 3 ? 4 : preset == 2 ? 2 : 1;
                rt.antiAliasing = msaa;
                cam.targetTexture = rt;
                for (int i = 0; i < 2; i++) cam.Render(); // due passate: il volume stack e SSAO si assestano
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Cartella));
                Directory.CreateDirectory(dir);
                string full = Path.Combine(dir, nomeFile + ".jpg");
                File.WriteAllBytes(full, tex.EncodeToJPG(88));
                Object.DestroyImmediate(tex);
                rt.Release();
                Object.DestroyImmediate(rt);
                return full;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
