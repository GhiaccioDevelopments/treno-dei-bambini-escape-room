#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Player
{
    /// <summary>
    /// R018: prova automatica della camera, solo in editor (menu Treno/Verifica/Camera, vedi VerificaCamera).
    /// In Play, per ogni scena del lavoro (Library/R018_lavoro.json) il giocatore viene teletrasportato su una griglia di punti
    /// calpestabili; per ogni punto la camera ruota di 360 gradi (12 passi) a 3 inclinazioni. Per ogni posa si lascia lavorare il
    /// CinemachineBrain (ManualUpdate con smorzamento azzerato e poi portato a regime con passi lunghi, quindi senza bisogno di molti frame: si misura la posa stazionaria, non i transitori) e si controlla:
    ///  - camera dentro un collider solido (muri, soffitti e porte compresi; i props piccoli o bassi, vedi CameraGuard.ContaDimensioni, non contano);
    ///  - camera sul vuoto (nessun suolo sotto di lei) o sotto il suolo del giocatore;
    ///  - ostacolo solido tra la camera e il giocatore (il Deoccluder avrebbe dovuto avvicinare la camera);
    ///  - camera troppo vicina al giocatore (solo conteggio).
    /// Il rapporto va in Docs/richieste/media/R018/rapporto_&lt;etichetta&gt;.txt.
    /// </summary>
    public class CameraProva : MonoBehaviour
    {
        public const string LavoroPath = "Library/R018_lavoro.json";
        const string Cartella = "Docs/richieste/media/R018";

        [Serializable] public class ScenaLavoro { public string scena; public List<Vector3> punti = new List<Vector3>(); }
        [Serializable] public class Lavoro { public string etichetta; public bool foto; public bool emulaPrima; public List<ScenaLavoro> scene = new List<ScenaLavoro>(); }

        static readonly string[] Cat = { "dentro geometria", "sul vuoto", "sotto il suolo", "ostacolo tra camera e giocatore", "troppo vicina (<0,45 m)", "dentro/oltre un muro dissolto (ammesso: interno)" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            if (!File.Exists(LavoroPath)) return;
            var go = new GameObject("CameraProva");
            DontDestroyOnLoad(go);
            go.AddComponent<CameraProva>();
        }

        IEnumerator Start()
        {
            Application.runInBackground = true; // senza, con l'editor in secondo piano il Play non avanza
            Lavoro lavoro = JsonUtility.FromJson<Lavoro>(File.ReadAllText(LavoroPath));
            File.Delete(LavoroPath);
            var rep = new StringBuilder();
            rep.AppendLine("Prova automatica della camera (" + lavoro.etichetta + ") - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            rep.AppendLine("Per ogni punto calpestabile: 12 direzioni x 3 inclinazioni (anello alto, centrale, basso). Errori = dentro geometria + sul vuoto + sotto il suolo + ostacolo tra camera e giocatore.");
            rep.AppendLine();
            long totPose = 0; var tot = new int[Cat.Length];
            string riga = "";

            foreach (var sl in lavoro.scene)
            {
                string path = "Assets/_Game/Scenes/" + sl.scena + ".unity";
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                for (int i = 0; i < 6; i++) yield return null;

                var player = GameObject.FindGameObjectWithTag("Player");
                var rig = FindAnyObjectByType<CameraRig>();
                var brain = FindAnyObjectByType<CinemachineBrain>();
                rep.AppendLine("==================== " + sl.scena + " ====================");
                if (player == null || rig == null || brain == null || rig.orbit == null)
                {
                    rep.AppendLine("  scena senza giocatore/camera: saltata");
                    continue;
                }
                var tpc = player.GetComponent<ThirdPersonController>();
                if (tpc != null) tpc.enabled = false;
                if (lavoro.foto)
                {
                    yield return Foto(lavoro, sl, player, rig, brain);
                    if (!lavoro.emulaPrima && sl.scena.StartsWith("20_")) yield return Movimento(lavoro, player, rig, brain);
                    continue;
                }
                var cc = player.GetComponent<CharacterController>();
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
                int frame = Time.frameCount + 1000;
                var cnt = new int[Cat.Length];
                var esempi = new List<string>[Cat.Length];
                for (int i = 0; i < Cat.Length; i++) esempi[i] = new List<string>();
                long pose = 0;
                var cam = brain.GetComponent<Camera>();
                var punto = rig.target != null ? rig.target : player.transform;
                float[] pitchV = { -1f, 0f, 1f };
                var casiPunto = new HashSet<int>();

                for (int pi = 0; pi < sl.punti.Count; pi++)
                {
                    Vector3 p = sl.punti[pi];
                    if (cc != null) cc.enabled = false;
                    player.transform.position = p + Vector3.up * 0.05f;
                    if (cc != null) cc.enabled = true;
                    Physics.SyncTransforms();
                    for (int vi = 0; vi < pitchV.Length; vi++)
                        for (int k = 0; k < 12; k++)
                        {
                            float yaw = -180f + k * 30f;
                            var o = rig.orbit;
                            o.HorizontalAxis.Value = yaw;
                            var v = o.VerticalAxis; v.Value = pitchV[vi]; o.VerticalAxis = v;
                            rig.vcam.PreviousStateIsValid = false;
                            brain.ManualUpdate(frame++, -1f);
                            // dt grande: lo smorzamento arriva a regime (stato stazionario della camera per quella posa)
                            brain.ManualUpdate(frame++, 2f);
                            brain.ManualUpdate(frame++, 2f);
                            brain.ManualUpdate(frame++, 2f);
                            pose++;
                            Vector3 c = cam.transform.position;
                            Vector3 tgt = punto.position;
                            string desc = "punto " + F(p) + " yaw " + yaw.ToString("0") + " anello " + pitchV[vi].ToString("0") + " camera " + F(c);
                            rig.AggiornaModalita();
                            Controlla(c, tgt, p, player, desc + (CameraRig.Interno ? " [interno]" : ""), cnt, esempi, casiPunto, pi);
                        }
                }
                totPose += pose;
                rep.AppendLine("  Punti: " + sl.punti.Count + ", pose: " + pose);
                for (int i = 0; i < Cat.Length; i++)
                {
                    tot[i] += cnt[i];
                    rep.AppendLine("  " + Cat[i] + ": " + cnt[i]);
                    for (int e = 0; e < esempi[i].Count && e < 12; e++) rep.AppendLine("      " + esempi[i][e]);
                }
                int err = cnt[0] + cnt[1] + cnt[2] + cnt[3];
                riga += sl.scena + " errori " + err + " (" + casiPunto.Count + " punti); ";
                rep.AppendLine("  ERRORI scena: " + err + " su " + pose + " pose, in " + casiPunto.Count + " punti su " + sl.punti.Count);
                rep.AppendLine();
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
                if (tpc != null) tpc.enabled = true;
            }

            if (lavoro.foto)
            {
                Debug.Log("[Verifica camera] Foto " + lavoro.etichetta + " scritte in " + Cartella + "/" + lavoro.etichetta);
                EditorApplication.isPlaying = false;
                yield break;
            }
            int errTot = tot[0] + tot[1] + tot[2] + tot[3];
            rep.AppendLine("TOTALE pose " + totPose + ": " + string.Join(", ", Array.ConvertAll(new[] { 0, 1, 2, 3, 4, 5 }, i => Cat[i] + " " + tot[i])) + ". ERRORI TOTALI: " + errTot);
            string full = Path.GetFullPath(Cartella + "/rapporto_" + lavoro.etichetta + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, rep.ToString(), new UTF8Encoding(false));
            Debug.Log("[Verifica camera] " + lavoro.etichetta + ": pose " + totPose + ", errori " + errTot + " | " + riga + "Rapporto: " + Cartella + "/rapporto_" + lavoro.etichetta + ".txt");
            EditorApplication.isPlaying = false;
        }

        // ---------------------------------------------------------------- foto (prima/dopo nei punti piu' stretti)

        /// <summary>Spazio libero in orizzontale attorno a un punto: somma minima delle distanze di due direzioni opposte (piu' basso = piu' stretto).</summary>
        static float Strettezza(Vector3 p)
        {
            float best = 99f;
            for (int a = 0; a < 4; a++)
            {
                float az = a * 45f * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(az), 0f, Mathf.Cos(az));
                var o = p + Vector3.up * 1.0f;
                float d1 = Physics.Raycast(o, d, out var h1, 6f, ~0, QueryTriggerInteraction.Ignore) ? h1.distance : 6f;
                float d2 = Physics.Raycast(o, -d, out var h2, 6f, ~0, QueryTriggerInteraction.Ignore) ? h2.distance : 6f;
                best = Mathf.Min(best, d1 + d2);
            }
            return best;
        }

        IEnumerator Foto(Lavoro lavoro, ScenaLavoro sl, GameObject player, CameraRig rig, CinemachineBrain brain)
        {
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            int vista = LayerMask.NameToLayer(WallFader.LayerName), porte = LayerMask.NameToLayer(WallFader.LayerPorte);
            if (lavoro.emulaPrima)
            {
                // configurazione di prima di R018: camera che attraversa muri e porte (WallFader li nasconde), niente CameraGuard, niente profili per stanza
                var guard = rig.GetComponent<CameraGuard>(); if (guard != null) guard.enabled = false;
                var deo = rig.GetComponent<CinemachineDeoccluder>(); if (deo != null) deo.CollideAgainst = ~((1 << vista) | (1 << porte));
                foreach (var v in FindObjectsByType<CameraProfileVolume>(FindObjectsInactive.Include)) v.enabled = false;
                rig.enabled = false;
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include)) if (t.name == "VetroFinestraCamera") t.gameObject.SetActive(false); // prima di R018 le finestre erano varchi
                if (sl.scena.StartsWith("20_"))
                {
                    rig.orbit.Orbits = new Cinemachine3OrbitRig.Settings
                    {
                        Top = new Cinemachine3OrbitRig.Orbit { Height = 1.75f, Radius = 1.2f },
                        Center = new Cinemachine3OrbitRig.Orbit { Height = 1.0f, Radius = 2.5f },
                        Bottom = new Cinemachine3OrbitRig.Orbit { Height = 0.2f, Radius = 2.0f },
                        SplineCurvature = 0.5f
                    };
                    var l = rig.vcam.Lens; l.FieldOfView = 55f; rig.vcam.Lens = l;
                    var va = rig.orbit.VerticalAxis; va.Range = new Vector2(-1f, 1f); rig.orbit.VerticalAxis = va;
                }
            }
            // i punti piu' stretti, distanziati tra loro di almeno 4 m
            var ordinati = new List<Vector3>(sl.punti);
            ordinati.Sort((a, b) => Strettezza(a).CompareTo(Strettezza(b)));
            var scelti = new List<Vector3>();
            foreach (var p in ordinati)
            {
                bool lontano = true; foreach (var q in scelti) if ((p - q).magnitude < 4f) { lontano = false; break; }
                if (lontano) scelti.Add(p);
                if (scelti.Count >= 3) break;
            }
            var cam = brain.GetComponent<Camera>();
            const int W = 426, H = 240, N = 6;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            string dir = Path.GetFullPath(Cartella + "/" + lavoro.etichetta);
            Directory.CreateDirectory(dir);
            int frame = Time.frameCount + 5000;
            for (int si = 0; si < scelti.Count; si++)
            {
                var p = scelti[si];
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = p + Vector3.up * 0.05f;
                if (cc != null) cc.enabled = true;
                Physics.SyncTransforms();
                if (rig.enabled) rig.AggiornaModalita();
                var sheet = new Texture2D(W * 3, H * 2, TextureFormat.RGB24, false);
                for (int k = 0; k < N; k++)
                {
                    var o = rig.orbit;
                    o.HorizontalAxis.Value = -180f + k * 60f;
                    var v = o.VerticalAxis; v.Value = 0f; o.VerticalAxis = v;
                    rig.vcam.PreviousStateIsValid = false;
                    brain.ManualUpdate(frame++, -1f);
                    brain.ManualUpdate(frame++, 2f);
                    brain.ManualUpdate(frame++, 2f);
                    yield return new WaitForSecondsRealtime(0.45f); // WallFader dissolve i muri tra camera e giocatore (fade di 0,25 s)
                    // il cross-fade dei LOD avanza con i fotogrammi renderizzati: se ne renderizzano alcuni prima dello scatto finale
                    for (int w = 0; w < 14; w++) { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; yield return null; }
                    brain.ManualUpdate(frame++, 0.001f);
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                    RenderTexture.active = null;
                    int col = k % 3, row = 1 - k / 3;
                    sheet.SetPixels(col * W, row * H, W, H, tex.GetPixels());
                    yield return null;
                }
                sheet.Apply();
                File.WriteAllBytes(dir + "/" + sl.scena + "_punto" + (si + 1) + ".jpg", sheet.EncodeToJPG(80));
                Destroy(sheet);
            }
            rt.Release(); Destroy(rt); Destroy(tex);
        }

        static bool InImpronta(Vector3 p)
        {
            foreach (var v in CameraProfileVolume.Tutti)
            {
                if (v == null || !v.dissolviMuri) continue;
                var b = v.GetComponent<BoxCollider>().bounds;
                if (p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z) return true;
            }
            return false;
        }

        /// <summary>
        /// Sequenza in movimento nel basso (Amerigo attraversa la stanza e passa in cucina, aggiornamento normale di Cinemachine, WallFader attivo):
        /// 8 fotogrammi in una tavola e il rapporto sugli scatti (spostamento massimo della camera in un fotogramma e rapporto con la velocita' di Amerigo).
        /// </summary>
        IEnumerator Movimento(Lavoro lavoro, GameObject player, CameraRig rig, CinemachineBrain brain)
        {
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
            var cam = brain.GetComponent<Camera>();
            var cc = player.GetComponent<CharacterController>();
            var rotta = new[] { new Vector3(0f, 0f, -1.7f), new Vector3(3f, 0f, 0.6f), new Vector3(3f, 0f, 5.6f) };
            if (cc != null) cc.enabled = false;
            player.transform.position = rotta[0] + Vector3.up * 0.05f;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
            rig.Snap();
            for (int i = 0; i < 20; i++) yield return null;
            const int W = 320, H = 180, N = 8;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            var sheet = new Texture2D(W * 4, H * 2, TextureFormat.RGB24, false);
            float lung = 0f; for (int i = 1; i < rotta.Length; i++) lung += Vector3.Distance(rotta[i - 1], rotta[i]);
            const float vel = 2.0f;
            float totTempo = lung / vel, passoFoto = totTempo / N;
            float t = 0f, prossima = 0f; int foto = 0, tratto = 1;
            Vector3 prevCam = cam.transform.position, finestraPos = prevCam; float accT = 0f; float maxSalto = 0f, maxVelCam = 0f; int frames = 0, veloci = 0; var sbm = new StringBuilder();
            while (tratto < rotta.Length && foto < N)
            {
                Vector3 pos = player.transform.position; pos.y = 0f;
                Vector3 meta = rotta[tratto];
                Vector3 d = meta - pos; d.y = 0f;
                float passo = vel * Time.deltaTime;
                if (d.magnitude <= passo) { tratto++; }
                var mov = d.normalized * Mathf.Min(passo, d.magnitude);
                if (cc != null) cc.Move(mov + Vector3.down * 0.5f * Time.deltaTime); else player.transform.position += mov;
                t += Time.deltaTime;
                yield return null; // LateUpdate di Cinemachine e WallFader
                Vector3 cp = cam.transform.position;
                float salto = Vector3.Distance(cp, prevCam);
                // velocita' su una finestra di almeno 50 ms (i fotogrammi dell'editor hanno durate irregolari: la velocita' del singolo fotogramma e' rumorosa)
                accT += Time.deltaTime;
                float velCam = 0f;
                if (accT >= 0.05f) { velCam = Vector3.Distance(cp, finestraPos) / accT; finestraPos = cp; accT = 0f; }
                if (frames > 2)
                {
                    maxSalto = Mathf.Max(maxSalto, salto); maxVelCam = Mathf.Max(maxVelCam, velCam);
                    if (velCam > 5f) { veloci++; if (sbm.Length < 400) sbm.Append("[t=" + t.ToString("0.00") + " s, " + (velCam).ToString("0.0") + " m/s, camera " + F(cp) + "] "); }
                }
                prevCam = cp; frames++;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; // fa avanzare la dissolvenza dei muri
                if (t >= prossima && foto < N)
                {
                    prossima += passoFoto;
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                    RenderTexture.active = null;
                    int col = foto % 4, row = 1 - foto / 4;
                    sheet.SetPixels(col * W, row * H, W, H, tex.GetPixels());
                    foto++;
                }
            }
            sheet.Apply();
            string dir = Path.GetFullPath(Cartella + "/" + lavoro.etichetta);
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(dir + "/20_Int_BassoAntonietta_movimento.jpg", sheet.EncodeToJPG(80));
            string rapporto = "Sequenza in movimento nel basso (" + lavoro.etichetta + "): " + frames + " fotogrammi, Amerigo a " + vel.ToString("0.0") + " m/s. "
                + "Spostamento massimo della camera in un fotogramma: " + (maxSalto * 100f).ToString("0.0") + " cm; velocita' massima della camera (su 50 ms): " + maxVelCam.ToString("0.0") + " m/s (Amerigo " + vel.ToString("0.0") + " m/s). "
                + "Finestre da 50 ms con la camera sopra 5 m/s: " + veloci + " " + sbm + ". " + "Muri dissolti a fine corsa: " + FindObjectsByType<MuroDissolvibile>(FindObjectsInactive.Exclude).Length + " dissolvibili in scena.";
            File.WriteAllText(dir + "/movimento.txt", rapporto, new UTF8Encoding(false));
            Debug.Log("[Verifica camera] " + rapporto);
            rt.Release(); Destroy(rt); Destroy(tex); Destroy(sheet);
        }

        static string F(Vector3 v) { return "(" + v.x.ToString("0.0") + "," + v.y.ToString("0.0") + "," + v.z.ToString("0.0") + ")"; }

        static void Segna(int cat, string desc, int[] cnt, List<string>[] esempi, HashSet<int> casiPunto, int pi, bool errore)
        {
            cnt[cat]++;
            if (esempi[cat].Count < 12) esempi[cat].Add(desc);
            if (errore) casiPunto.Add(pi);
        }

        static void Controlla(Vector3 c, Vector3 tgt, Vector3 suoloGiocatore, GameObject player, string desc, int[] cnt, List<string>[] esempi, HashSet<int> casiPunto, int pi)
        {
            int vista = LayerMask.NameToLayer(WallFader.LayerName), porte = LayerMask.NameToLayer(WallFader.LayerPorte);
            bool interno = CameraRig.Interno;
            // a) dentro un collider
            var ov = Physics.OverlapSphere(c, 0.12f, ~0, QueryTriggerInteraction.Ignore);
            bool solido = false, muro = false;
            foreach (var col in ov)
            {
                if (col.transform.IsChildOf(player.transform) || !CameraGuard.Conta(col)) continue; // i props piccoli la camera li scavalca
                // negli interni muri e porte dissolvibili non contano: WallFader li rende invisibili (overlap sphere 0,6 m attorno alla camera)
                if (interno && (col.gameObject.layer == vista || col.gameObject.layer == porte) && col.GetComponentInParent<MuroDissolvibile>() != null) { muro = true; continue; }
                solido = true; if (desc.Length < 400) desc += " [" + col.name + (col.gameObject.layer == vista ? ", muro" : "") + "]";
            }
            if (solido) Segna(0, desc, cnt, esempi, casiPunto, pi, true);
            else if (muro) Segna(5, desc, cnt, esempi, casiPunto, pi, false);
            // b) suolo sotto la camera e sotto il suolo del giocatore
            Physics.queriesHitBackfaces = false;
            if (!Physics.Raycast(c + Vector3.up * 0.05f, Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                // negli interni la camera puo' stare oltre un muro dissolto, fuori dall'impronta delle stanze: il muro e' invisibile, il vuoto non si vede
                if (interno && !InImpronta(c)) Segna(5, desc, cnt, esempi, casiPunto, pi, false);
                else Segna(1, desc, cnt, esempi, casiPunto, pi, true);
            }
            if (c.y < suoloGiocatore.y + 0.3f) Segna(2, desc, cnt, esempi, casiPunto, pi, true);
            // c) ostacolo solido tra camera e giocatore (escluso il giocatore)
            Vector3 dir = tgt - c; float d = dir.magnitude;
            if (d > 0.05f)
            {
                int maschera = interno ? ~((1 << vista) | (1 << porte)) : ~0; // negli interni i muri tra camera e giocatore si dissolvono
                var hits = Physics.RaycastAll(c, dir / d, d - 0.05f, maschera, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (h.collider.transform.IsChildOf(player.transform) || !CameraGuard.Conta(h.collider)) continue;
                    Segna(3, desc + " [" + h.collider.name + "]", cnt, esempi, casiPunto, pi, true);
                    break;
                }
            }
            if (d < 0.45f) Segna(4, desc, cnt, esempi, casiPunto, pi, false);
        }
    }
}
#endif
