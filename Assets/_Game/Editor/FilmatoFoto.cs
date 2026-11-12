using System.Collections.Generic;
using System.IO;
using TrenoBambini.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R025: storyboard del filmato iniziale. Legge filmato_intro.json e, per ogni inquadratura "di mondo" (non inserti), apre la scena, mette l'attore dove dice il
    /// JSON e scatta un JPG 1280x720 all'inizio (_a) e alla fine (_b) del movimento della camera, con il look della scena (volume, nebbia, luce baked).
    /// Gli inserti di pezzi, i fogli e la pianta sono UI/studio: si fotografano in Play. Cartella di uscita: Temp/FilmatoSb (fuori dal repo), da cui si scelgono le foto di Docs.
    /// Menu: Treno > Filmato > Storyboard. Nessuna modifica alle scene (si riaprono senza salvare).
    /// </summary>
    public static class FilmatoFoto
    {
        const string Cartella = "Temp/FilmatoSb";

        [MenuItem("Treno/Filmato/Storyboard (foto delle inquadrature)")]
        public static void Storyboard() { Scatta(null); }

        public static void Scatta(string soloCapitolo)
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Resources/Data/filmato_intro.json");
            if (ta == null) { Debug.LogError("[FilmatoFoto] filmato_intro.json non trovato."); return; }
            var f = JsonUtility.FromJson<FilmatoFile>(ta.text);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Cartella));
            Directory.CreateDirectory(dir);
            int n = 0;
            foreach (var cap in f.capitoli)
            {
                if (!string.IsNullOrEmpty(soloCapitolo) && cap.id != soloCapitolo) continue;
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + cap.scena + ".unity", OpenSceneMode.Single);
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) continue;
                var mod = player.transform.Find("Modello");
                // le camere di gioco non devono interferire: si usa una camera temporanea
                for (int k = 0; k < cap.inquadrature.Count; k++)
                {
                    var s = cap.inquadrature[k];
                    if (s.inserto != null && s.inserto.tipo == "pezzi") continue;
                    Vector3 posA, posB; float yaw = player.transform.eulerAngles.y;
                    ActorPlace(player.transform, s.attore, out posA, out posB, out yaw);
                    if (mod != null) mod.gameObject.SetActive(!(s.attore != null && s.attore.presente && s.attore.nascondi));
                    for (int fase = 0; fase < 2; fase++)
                    {
                        player.transform.position = fase == 0 ? posA : posB;
                        player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                        Pose(s, player.transform, fase, out var pos, out var look, out float fov);
                        string nome = string.Format("{0}_{1:00}{2}", cap.id, k, fase == 0 ? "a" : "b");
                        Render(pos, look, fov, Path.Combine(dir, nome + ".jpg"));
                        n++;
                        if (fase == 0 && Same(s)) break;
                    }
                }
            }
            Debug.Log("[FilmatoFoto] " + n + " fotogrammi in " + dir);
        }

        static bool Same(FilmatoInquadratura s)
        {
            bool mov = s.p1 != null && s.p1.Length >= 3 || s.m1 != null && s.m1.Length >= 3;
            bool cammina = s.attore != null && s.attore.presente && s.attore.percorso != null && s.attore.percorso.Count >= 2;
            return !mov && !cammina;
        }

        static void ActorPlace(Transform player, FilmatoAttore a, out Vector3 posA, out Vector3 posB, out float yaw)
        {
            posA = player.position; posB = posA; yaw = player.eulerAngles.y;
            if (a == null || !a.presente) return;
            if (a.pos != null && a.pos.Length >= 2) posA = new Vector3(a.pos[0], Suolo(a.pos[0], a.pos[1], player.position.y), a.pos[1]);
            if (a.yaw < 9000f) yaw = a.yaw;
            posB = posA;
            if (a.percorso != null && a.percorso.Count >= 2)
            {
                float x = a.percorso[a.percorso.Count - 2], z = a.percorso[a.percorso.Count - 1];
                posB = new Vector3(x, Suolo(x, z, posA.y), z);
                var d = new Vector3(x - posA.x, 0f, z - posA.z);
                if (d.sqrMagnitude > 0.01f) yaw = Quaternion.LookRotation(d).eulerAngles.y;
            }
        }

        static float Suolo(float x, float z, float y0)
        {
            Physics.SyncTransforms();
            float best = float.NegativeInfinity;
            foreach (var h in Physics.RaycastAll(new Vector3(x, y0 + 6f, z), Vector3.down, 14f, ~0, QueryTriggerInteraction.Ignore))
                if (h.normal.y > 0.5f && h.point.y > best && h.point.y < y0 + 1.2f) best = h.point.y;
            return float.IsNegativeInfinity(best) ? y0 : best + 0.02f;
        }

        static Vector3 V(float[] a) { return a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero; }

        static void Pose(FilmatoInquadratura s, Transform player, int fase, out Vector3 pos, out Vector3 look, out float fov)
        {
            Vector3 p0 = V(s.p0), m0 = V(s.m0);
            Vector3 p1 = s.p1 != null && s.p1.Length >= 3 ? V(s.p1) : p0, m1 = s.m1 != null && s.m1.Length >= 3 ? V(s.m1) : m0;
            pos = fase == 0 ? p0 : p1; look = fase == 0 ? m0 : m1;
            float f0 = s.fov0 > 1f ? s.fov0 : 45f, f1 = s.fov1 > 1f ? s.fov1 : f0;
            fov = fase == 0 ? f0 : f1;
            if (!string.IsNullOrEmpty(s.segui))
            {
                var q = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
                pos = player.position + q * pos; look = player.position + q * look;
            }
        }

        static void Render(Vector3 pos, Vector3 look, float fov, string file)
        {
            var go = new GameObject("FilmatoFoto_Temp");
            try
            {
                go.transform.position = pos;
                var dir = look - pos;
                go.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude < 1e-4f ? Vector3.forward : dir, Vector3.up);
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = fov; cam.nearClipPlane = 0.12f; cam.farClipPlane = 150f;
                cam.clearFlags = CameraClearFlags.Skybox;
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.None; data.renderShadows = true;
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { antiAliasing = 2 };
                cam.targetTexture = rt;
                for (int i = 0; i < 2; i++) cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                File.WriteAllBytes(file, tex.EncodeToJPG(80));
                Object.DestroyImmediate(tex);
                rt.Release(); Object.DestroyImmediate(rt);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
