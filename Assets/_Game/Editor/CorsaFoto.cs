using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R022: foto di documentazione della corsa. Istanzia un prefab di Amerigo in una scena di anteprima, lo fa correre con l'AnimatorController
    /// del gioco (Speed = 1) e fotografa 4 fotogrammi del ciclo da davanti e di lato, in un unico JPG 1280x720 (fuori da LFS).
    /// Menu: Treno ▸ Diagnostica ▸ Foto corsa (adulto e bambino).
    /// </summary>
    public static class CorsaFoto
    {
        const string PrefabAdulto = "Assets/Art/Characters/AmerigoMH_Adulto/AmerigoMH_Adulto.prefab";
        const string PrefabBambino = "Assets/Art/Characters/AmerigoMH_Bambino/AmerigoMH_Bambino.prefab";
        const string CartellaMedia = "Docs/richieste/media/R022";

        [MenuItem("Treno/Diagnostica/Foto corsa (adulto e bambino)")]
        public static void FotoMenu()
        {
            string sfx = EditorPrefs.GetString("CorsaFoto.Suffisso", "dopo");
            Scatta(PrefabAdulto, "corsa_adulto_" + sfx);
            Scatta(PrefabBambino, "corsa_bambino_" + sfx);
        }

        /// <summary>Scatta la foto di un prefab e salva Docs/richieste/media/R022/&lt;nome&gt;.jpg. Ritorna il percorso.</summary>
        public static string Scatta(string prefabPath, string nome, float speed = 1f, int fotogrammi = 4)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var pru = new PreviewRenderUtility();
            const int W = 320, H = 360;
            var sheet = new Texture2D(W * fotogrammi, H * 2, TextureFormat.RGB24, false);
            try
            {
                pru.camera.fieldOfView = 25f;
                pru.camera.nearClipPlane = 0.1f; pru.camera.farClipPlane = 50f;
                pru.camera.clearFlags = CameraClearFlags.SolidColor;
                pru.camera.backgroundColor = new Color(0.75f, 0.75f, 0.72f);
                pru.lights[0].intensity = 1.1f; pru.lights[0].transform.rotation = Quaternion.Euler(35f, 25f, 0f);
                pru.lights[1].intensity = 0.6f;
                pru.ambientColor = new Color(0.45f, 0.45f, 0.45f);

                var go = Object.Instantiate(prefab);
                pru.AddSingleGO(go);
                go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
                var anim = go.GetComponentInChildren<Animator>();
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.Rebind();
                anim.SetFloat("Speed", speed); anim.SetFloat("MotionSpeed", 1f); anim.SetBool("Grounded", true);
                // porta il parametro smorzato a regime e fai girare qualche ciclo prima di fotografare
                for (int i = 0; i < 120; i++) anim.Update(1f / 60f);
                float ciclo = ClipLength(anim);
                for (int f = 0; f < fotogrammi; f++)
                {
                    if (f > 0) anim.Update(ciclo / fotogrammi);
                    anim.Update(0f);
                    // la pelle (skinning) in anteprima resta indietro di un disegno: si disegna a vuoto una volta prima dello scatto
                    pru.BeginPreview(new Rect(0, 0, 8, 8), GUIStyle.none); pru.camera.Render(); pru.EndPreview();
                    Bounds b = Bounds3D(go);
                    Vector3 c = b.center; float dist = b.size.y * 2.6f;
                    for (int v = 0; v < 2; v++)
                    {
                        // v=0 vista frontale (la camera sta davanti, +Z), v=1 laterale (da +X: il personaggio corre verso destra)
                        Vector3 dir = v == 0 ? Vector3.forward : Vector3.right;
                        pru.camera.transform.position = c + dir * dist;
                        pru.camera.transform.LookAt(c);
                        pru.BeginPreview(new Rect(0, 0, W, H), GUIStyle.none);
                        pru.camera.Render();
                        var tex = pru.EndPreview() as RenderTexture;
                        var prev = RenderTexture.active; RenderTexture.active = tex;
                        var t2 = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
                        t2.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); t2.Apply();
                        RenderTexture.active = prev;
                        sheet.SetPixels(f * W, (1 - v) * H, W, H, t2.GetPixels(0, 0, W, H));
                        Object.DestroyImmediate(t2);
                    }
                }
                // le luci dell'anteprima sotto URP sono deboli: schiarisce l'insieme (solo per la documentazione)
                var px = sheet.GetPixels();
                for (int i = 0; i < px.Length; i++) { px[i].r = Mathf.Min(1f, px[i].r * 2.8f); px[i].g = Mathf.Min(1f, px[i].g * 2.8f); px[i].b = Mathf.Min(1f, px[i].b * 2.8f); }
                sheet.SetPixels(px);
                sheet.Apply();
                Directory.CreateDirectory(CartellaMedia);
                string path = CartellaMedia + "/" + nome + ".jpg";
                File.WriteAllBytes(path, sheet.EncodeToJPG(85));
                Debug.Log("[CorsaFoto] " + path);
                return path;
            }
            finally { pru.Cleanup(); Object.DestroyImmediate(sheet); }
        }

        /// <summary>Foto della posa di riferimento (senza animazione) di un prefab: frontale e laterale.</summary>
        public static string ScattaPosa(string prefabPath, string nome)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var pru = new PreviewRenderUtility();
            const int W = 320, H = 360;
            var sheet = new Texture2D(W * 2, H, TextureFormat.RGB24, false);
            try
            {
                pru.camera.fieldOfView = 25f; pru.camera.nearClipPlane = 0.1f; pru.camera.farClipPlane = 50f;
                pru.camera.clearFlags = CameraClearFlags.SolidColor; pru.camera.backgroundColor = new Color(0.75f, 0.75f, 0.72f);
                pru.lights[0].intensity = 1.1f; pru.lights[0].transform.rotation = Quaternion.Euler(35f, 25f, 0f);
                var go = Object.Instantiate(prefab); pru.AddSingleGO(go);
                var anim = go.GetComponentInChildren<Animator>(); anim.runtimeAnimatorController = null; anim.Rebind(); anim.Update(0f);
                Bounds b = Bounds3D(go); Vector3 c = b.center; float dist = b.size.y * 2.6f;
                for (int v = 0; v < 2; v++)
                {
                    pru.camera.transform.position = c + (v == 0 ? Vector3.forward : Vector3.right) * dist; pru.camera.transform.LookAt(c);
                    pru.BeginPreview(new Rect(0, 0, W, H), GUIStyle.none); pru.camera.Render();
                    var tex = pru.EndPreview() as RenderTexture; var prev = RenderTexture.active; RenderTexture.active = tex;
                    var t2 = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false); t2.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); t2.Apply();
                    RenderTexture.active = prev; sheet.SetPixels(v * W, 0, W, H, t2.GetPixels(0, 0, W, H)); Object.DestroyImmediate(t2);
                }
                var px = sheet.GetPixels();
                for (int i = 0; i < px.Length; i++) { px[i].r = Mathf.Min(1f, px[i].r * 2.8f); px[i].g = Mathf.Min(1f, px[i].g * 2.8f); px[i].b = Mathf.Min(1f, px[i].b * 2.8f); }
                sheet.SetPixels(px); sheet.Apply();
                Directory.CreateDirectory(CartellaMedia);
                string path = CartellaMedia + "/" + nome + ".jpg"; File.WriteAllBytes(path, sheet.EncodeToJPG(85)); return path;
            }
            finally { pru.Cleanup(); Object.DestroyImmediate(sheet); }
        }

        /// <summary>Misure della corsa su un prefab: flessione dei due ginocchia, inclinazione del busto (avanti e di lato), a 8 fotogrammi del ciclo.</summary>
        public static string Misura(string prefabPath, float speed = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var pru = new PreviewRenderUtility();
            var sb = new System.Text.StringBuilder();
            try
            {
                var go = Object.Instantiate(prefab); pru.AddSingleGO(go);
                var anim = go.GetComponentInChildren<Animator>(); anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate; anim.Rebind();
                anim.SetFloat("Speed", speed); anim.SetFloat("MotionSpeed", 1f); anim.SetBool("Grounded", true);
                for (int i = 0; i < 120; i++) anim.Update(1f / 60f);
                float ciclo = ClipLength(anim);
                var tf = new System.Collections.Generic.Dictionary<string, Transform>();
                foreach (var t in go.GetComponentsInChildren<Transform>()) tf[t.name] = t;
                System.Func<string, string, string, float> flex = (a, b, c) => 180f - Vector3.Angle(tf[a].position - tf[b].position, tf[c].position - tf[b].position);
                sb.AppendLine("t  ginocchioSx ginocchioDx  avanti lato(gradi)  pelvi_y");
                for (int f = 0; f < 8; f++)
                {
                    if (f > 0) anim.Update(ciclo / 8f);
                    Vector3 su = tf["neck_01"].position - tf["pelvis"].position;
                    float avanti = Mathf.Atan2(su.z, su.y) * Mathf.Rad2Deg, lato = Mathf.Atan2(su.x, su.y) * Mathf.Rad2Deg;
                    sb.AppendLine(f + "  " + flex("thigh_l", "calf_l", "foot_l").ToString("F0") + "  " + flex("thigh_r", "calf_r", "foot_r").ToString("F0") + "  " + avanti.ToString("F0") + "  " + lato.ToString("F0") + "  " + tf["pelvis"].position.y.ToString("F2"));
                }
                return sb.ToString();
            }
            finally { pru.Cleanup(); }
        }

        static float ClipLength(Animator a)
        {
            var info = a.GetCurrentAnimatorClipInfo(0);
            float best = 0.7333f;
            foreach (var i in info) if (i.weight > 0.5f) best = i.clip.length;
            return best;
        }

        static Bounds Bounds3D(GameObject go)
        {
            Bounds b = new Bounds(go.transform.position + Vector3.up, Vector3.one);
            bool first = true;
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
