#if UNITY_EDITOR || DEBUG
// R011: prova automatica dei segreti, solo negli editor e nelle build di sviluppo (mai nella build normale).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Player;
using TrenoBambini.Puzzles;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.Segreti
{
    /// <summary>
    /// R011: gioca davvero i segreti di hub, basso e ricordo. Attiva SOLO con -segretitest [-segretitestout cartella] (o -segretifoto per le sole foto di posizionamento);
    /// nell'editor con SessionState "segretitest" / "segretifoto". Per ogni segreto: esiste, e' raggiungibile (riempimento a griglia dal punto di partenza con la capsula del
    /// giocatore), non sta dentro muri o pavimenti, e' appoggiato, si vede (dimensione a schermo da una distanza di ricerca), il PlayerInteractor lo propone quando ci si avvicina,
    /// si raccoglie o si risolve davvero (mouse virtuale per gli enigmi), il salvataggio lo ricorda (anche dopo Continua), la pagina Segreti del quaderno e il contatore della pausa
    /// mostrano i numeri giusti, l'Indizio tace prima e accenna dopo, nessun segreto serve alla storia. Scrive segretitest.log, i JPG 1280x720 e OK.txt / FALLITA.txt.
    /// </summary>
    public class SegretiSelfTest : MonoBehaviour
    {
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-segretitest" || a == "-segretifoto") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("segretitest", false) || UnityEditor.SessionState.GetBool("segretifoto", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("SegretiSelfTest");
            DontDestroyOnLoad(go);
            go.AddComponent<SegretiSelfTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        bool soloFoto;
        string dir, logPath;
        int ok, ko;
        Keyboard kb;
        Mouse ms;
        EsaminaView V { get { return EsaminaView.Current; } }
        SegretiFile F { get { return GameData.Segreti; } }

        void Log(string s)
        {
            Debug.Log("[SegretiTest] " + s);
            File.AppendAllText(logPath, s + "\n", Encoding.UTF8);
        }

        void Check(string nome, bool cond, string dettaglio = "")
        {
            if (cond) ok++; else ko++;
            Log((cond ? "PASS " : "FAIL ") + nome + (dettaglio != "" ? "  [" + dettaglio + "]" : ""));
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var a in Args) if (a == "-segretifoto") soloFoto = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("segretifoto", false)) soloFoto = true;
#endif
            dir = Arg("-segretitestout", Path.Combine(Application.dataPath, "..", "segretitest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "segretitest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova segreti. Salvataggio presente: " + avevaSave + (soloFoto ? " (solo foto)" : ""));

            kb = InputSystem.AddDevice<Keyboard>("SegretiTestKb");
            ms = InputSystem.AddDevice<Mouse>("SegretiTestMouse");
            foreach (var d in InputSystem.devices) if (d != kb && d != ms) InputSystem.DisableDevice(d);
            InputSystem.onDeviceChange += (d, ch) => { if ((ch == InputDeviceChange.Added || ch == InputDeviceChange.Reconnected || ch == InputDeviceChange.Enabled) && d != kb && d != ms) InputSystem.DisableDevice(d); };

            bool fine = false;
            try { StartCoroutine(Corsa(() => fine = true)); } catch (Exception e) { Log("ERRORE " + e); }
            while (!fine) yield return null;

            if (avevaSave) PlayerPrefs.SetString("treno_save", saveBackup); else PlayerPrefs.DeleteKey("treno_save");
            PlayerPrefs.Save();
            Log("Fine prova: " + ok + " PASS, " + ko + " FAIL");
            File.WriteAllText(Path.Combine(dir, ko == 0 ? "OK.txt" : "FALLITA.txt"), ok + " PASS, " + ko + " FAIL\n");
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("segretitest", false);
            UnityEditor.SessionState.SetBool("segretifoto", false);
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator Corsa(Action fine)
        {
            Exception err = null;
            var it = Flusso();
            while (true)
            {
                bool avanti;
                try { avanti = it.MoveNext(); } catch (Exception e) { err = e; break; }
                if (!avanti) break;
                yield return it.Current;
            }
            if (err != null) { Log("ECCEZIONE " + err); ko++; }
            fine();
        }

        // ------------------------------------------------------------------ utilita'
        IEnumerator Attendi(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        IEnumerator Foto(string nome)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) yield break;
            var rt = RenderTexture.GetTemporary(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var out2 = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            out2.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            out2.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(dir, nome + ".jpg"), out2.EncodeToJPG(82));
            Destroy(tex); Destroy(out2);
            yield return null;
        }

        /// <summary>Foto da una camera di prova (stessa luce e post-processing della camera di gioco).</summary>
        IEnumerator Scatta(string nome, Vector3 da, Vector3 verso, float fov)
        {
            // il giocatore non deve coprire il pezzo: per le riprese di prova i suoi renderer si spengono un istante
            var spenti = new List<Renderer>();
            var pl = Giocatore;
            if (pl != null) foreach (var r in pl.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; spenti.Add(r); }
            var go = new GameObject("FotoCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
            cam.transform.position = da; cam.transform.LookAt(verso);
            var d = cam.GetUniversalAdditionalCameraData(); d.renderPostProcessing = true;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            yield return new WaitForEndOfFrame();
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); t.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, nome + ".jpg"), t.EncodeToJPG(82));
            Destroy(t); rt.Release(); Destroy(rt); Destroy(go);
            foreach (var r in spenti) if (r != null) r.enabled = true;
            yield return null;
        }

        IEnumerator ClickMouse(Vector2 pos)
        {
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos }); yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos }); yield return null; yield return null;
        }

        IEnumerator Parte(string id)
        {
            Vector2 p;
            if (V == null || !V.PosizioneSchermo(id, out p)) { Check("parte " + id + " presente nella vista", false); yield break; }
            // il raggio della camera sulla parte (stessa funzione dei clic veri): deve colpire proprio quella parte e non altro
            string colpita = "";
            try
            {
                var mi = typeof(EsaminaView).GetMethod("Colpito", BindingFlags.NonPublic | BindingFlags.Instance);
                var args = new object[] { p, default(RaycastHit) };
                var run = mi.Invoke(V, args);
                if (run != null) { var d = (ParteDef)run.GetType().GetField("d").GetValue(run); colpita = d != null ? d.id : ""; }
            }
            catch (Exception ex) { Log("  (Colpito non invocabile: " + ex.Message + ")"); }
            Check("clic sulla parte '" + id + "': il raggio colpisce la parte giusta", colpita == id, colpita == "" ? "nessuna parte" : colpita);
            int prima = V.Progresso; string msgPrima = Messaggio();
            yield return ClickMouse(p);
            yield return Attendi(0.55f);
            if (V != null && !V.Risolto && V.Progresso == prima && Messaggio() == msgPrima)
            {
                Log("  (il clic del mouse virtuale su '" + id + "' in " + p + " non ha cambiato nulla: clic diretto)");
                V.Clicca(id);
                yield return Attendi(0.55f);
            }
        }

        string Messaggio() { return V != null ? V.UltimoMessaggio : ""; }

        IEnumerator ChiudiMessaggi()
        {
            for (int giro = 0; giro < 12; giro++)
            {
                Button trovato = null;
                foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude))
                {
                    var tx = b.GetComponentInChildren<TextMeshProUGUI>();
                    if (tx != null && (tx.text == "Continua" || tx.text == "Avanti") && b.interactable && b.gameObject.activeInHierarchy) { trovato = b; break; }
                }
                if (trovato == null) yield break;
                trovato.onClick.Invoke();
                yield return Attendi(0.5f);
            }
        }

        bool ToastContiene(string frammento)
        {
            foreach (var t in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude))
                if (t != null && t.gameObject.activeInHierarchy && t.text != null && t.text.Contains(frammento)) return true;
            return false;
        }

        IEnumerator ScenaPronta(string scena)
        {
            float t = 0f;
            while (t < 25f)
            {
                t += Time.unscaledDeltaTime;
                if (SceneManager.GetActiveScene().name == scena && SceneLoader.I != null && !SceneLoader.I.IsLoading && SceneManager.sceneCount == 1) break;
                yield return null;
            }
            yield return Attendi(1.2f);
            yield return ChiudiMessaggi();
        }

        IEnumerator Vai(string scena, string spawn)
        {
            SceneLoader.I.Load(scena, spawn);
            yield return ScenaPronta(scena);
        }

        /// <summary>Partita nuova senza filmato (la prova non deve passare dall'introduzione).</summary>
        IEnumerator NuovaPartita()
        {
            var gm = GameManager.I;
            var campo = typeof(GameManager).GetField("save", BindingFlags.NonPublic | BindingFlags.Instance);
            var vecchio = gm.Save;
            var s = new SaveData { scena = GameManager.SceneHub, spawn = "inizio", filmatoVisto = true, volume = vecchio != null ? vecchio.volume : 1f };
            if (campo != null) campo.SetValue(gm, s);
            SaveSystem.Save(s);
            gm.ResetBlocks();
            SceneLoader.I.Load(GameManager.SceneHub, "inizio");
            yield return ScenaPronta(GameManager.SceneHub);
        }

        Transform Giocatore { get { var p = PlayerInteractor.Active; return p != null ? p.transform : null; } }

        void Teleporta(Vector3 p, Vector3 guarda)
        {
            var pl = Giocatore;
            if (pl == null) return;
            var cc = pl.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            pl.position = p;
            var d = guarda - p; d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) pl.rotation = Quaternion.LookRotation(d.normalized);
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        // ------------------------------------------------------------------ griglia percorribile (come R006 tappa 3, a runtime)
        sealed class Griglia
        {
            public float passo = 0.25f, x0, z0;
            public int w, d;
            public bool[] visto;
            public float[] quota;
            public List<Vector3> celle = new List<Vector3>();
        }

        Griglia Percorribile(float r, float h)
        {
            var pl = Giocatore;
            var g = new Griglia();
            if (pl == null) return g;
            var cc = pl.GetComponent<CharacterController>();
            bool eraAcceso = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            Physics.SyncTransforms();
            Vector3 start = pl.position;
            Bounds all = new Bounds(start, Vector3.zero);
            foreach (var c in FindObjectsByType<Collider>(FindObjectsInactive.Exclude)) if (!c.isTrigger && c.enabled) all.Encapsulate(c.bounds);
            g.w = Mathf.CeilToInt((all.max.x - all.min.x) / g.passo) + 2; g.d = Mathf.CeilToInt((all.max.z - all.min.z) / g.passo) + 2;
            g.x0 = all.min.x - g.passo; g.z0 = all.min.z - g.passo;
            g.visto = new bool[g.w * g.d]; g.quota = new float[g.w * g.d];
            var coda = new Queue<int>();
            int sx = Mathf.RoundToInt((start.x - g.x0) / g.passo), sz = Mathf.RoundToInt((start.z - g.z0) / g.passo);
            g.visto[sz * g.w + sx] = true; g.quota[sz * g.w + sx] = start.y;
            coda.Enqueue(sz * g.w + sx);
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            while (coda.Count > 0)
            {
                int cur = coda.Dequeue();
                int cx = cur % g.w, cz = cur / g.w;
                float cy = g.quota[cur];
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dx[k], nz = cz + dz[k];
                    if (nx < 0 || nz < 0 || nx >= g.w || nz >= g.d || g.visto[nz * g.w + nx]) continue;
                    float px = g.x0 + nx * g.passo, pz = g.z0 + nz * g.passo;
                    RaycastHit hit;
                    if (!Physics.Raycast(new Vector3(px, cy + 0.45f, pz), Vector3.down, out hit, 0.75f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    float y = hit.point.y;
                    if (y - cy > 0.21f) continue;
                    var p0 = new Vector3(px, y + r + 0.21f, pz);
                    var p1 = new Vector3(px, y + h - r, pz);
                    if (Physics.CheckCapsule(p0, p1, r, ~0, QueryTriggerInteraction.Ignore)) continue;
                    g.visto[nz * g.w + nx] = true; g.quota[nz * g.w + nx] = y;
                    g.celle.Add(new Vector3(px, y, pz));
                    coda.Enqueue(nz * g.w + nx);
                }
            }
            if (cc != null && eraAcceso) cc.enabled = true;
            Physics.SyncTransforms();
            return g;
        }

        /// <summary>Cella percorribile piu' vicina al punto (distanza orizzontale), con quota compatibile.</summary>
        static bool Vicina(Griglia g, Vector3 p, float quotaMax, out Vector3 cella, out float dist)
        {
            cella = p; dist = 1e9f;
            foreach (var c in g.celle)
            {
                if (Mathf.Abs(c.y - p.y) > quotaMax) continue;
                float d = new Vector2(c.x - p.x, c.z - p.z).magnitude;
                if (d < dist) { dist = d; cella = c; }
            }
            return dist < 1e8f;
        }

        /// <summary>Linea libera dall'occhio al punto: ignora i solidi che contengono il punto stesso (appoggio, cavita', nicchie) e il pezzo.</summary>
        /// <summary>Muro, edificio, pavimento o soffitto: gli oggetti di scena (tavoli, casse, gabbie, pozzi) hanno collider a scatola grossolani e non contano.</summary>
        static bool Struttura(Collider c)
        {
            string n = c.name;
            if (n.StartsWith("Muro") || n.StartsWith("Casa") || n.StartsWith("Pavimento") || n.StartsWith("Selciato") || n.StartsWith("Soffitto") || n.StartsWith("LOD") || n.Contains("Terreno") || n.StartsWith("Terra")) return true;
            var b = c.bounds.size;
            return Mathf.Max(b.x, Mathf.Max(b.y, b.z)) >= 6f;
        }

        static bool Libero(Vector3 da, Vector3 a, Transform pezzo)
        {
            Vector3 d = a - da; float len = d.magnitude;
            if (len < 0.01f) return true;
            foreach (var h in Physics.RaycastAll(da, d / len, len - 0.02f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (pezzo != null && h.collider.transform.IsChildOf(pezzo)) continue;
                if (!Struttura(h.collider)) continue;
                if (h.collider.bounds.Contains(a)) continue;
                return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ controlli di un pezzo del mondo
        static Bounds Limiti(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>(false);
            bool primo = true; Bounds b = new Bounds(t.position, Vector3.zero);
            foreach (var r in rs) { if (primo) { b = r.bounds; primo = false; } else b.Encapsulate(r.bounds); }
            return b;
        }

        IEnumerator ControllaPezzo(SegretoPezzo p, Griglia g, bool giocatoreAdulto)
        {
            var d = p.def;
            string n = d.id;
            var vis = p.visuale;
            var rs = vis != null ? vis.GetComponentsInChildren<Renderer>(false) : new Renderer[0];
            int attivi = 0, senzaMat = 0;
            foreach (var r in rs) { if (r.enabled && r.gameObject.activeInHierarchy) attivi++; var m = r.sharedMaterials; if (m == null || m.Length == 0 || m[0] == null) senzaMat++; }
            var b = Limiti(vis.transform);
            Log("  " + n + " pos=" + p.transform.position.ToString("F2") + " renderer=" + rs.Length + " bounds=" + b.size.ToString("F3"));
            Check(n + ": renderer attivi con materiale", rs.Length > 0 && attivi == rs.Length && senzaMat == 0, rs.Length + " renderer, " + attivi + " attivi, " + senzaMat + " senza materiale");
            if (rs.Length == 0) yield break;

            // appoggiato (solo per i pezzi sul suolo) e fuori dai solidi
            if ((d.appoggio ?? "suolo") == "suolo")
            {
                var org = new Vector3(b.center.x, b.min.y + 0.4f, b.center.z);
                RaycastHit hit;
                bool suolo = Physics.Raycast(org, Vector3.down, out hit, 3f, ~0, QueryTriggerInteraction.Ignore);
                float dy = suolo ? b.min.y - hit.point.y : 99f;
                Check(n + ": appoggiato (ne' sotto il suolo ne' sospeso)", suolo && Mathf.Abs(dy) <= 0.06f, suolo ? "su " + hit.collider.name + " scarto " + dy.ToString("F3") : "nessun solido sotto");
            }
            var ristretto = new Vector3(Mathf.Max(0.005f, b.extents.x - 0.01f), Mathf.Max(0.005f, b.extents.y - 0.01f), Mathf.Max(0.005f, b.extents.z - 0.01f));
            var dentro = new List<string>();
            foreach (var c in Physics.OverlapBox(b.center, ristretto, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(p.transform)) continue;
                if (c.bounds.max.y <= b.min.y + 0.05f) continue;
                if (!Struttura(c)) continue;
                dentro.Add(c.name);
            }
            Check(n + ": fuori da muri e pavimenti", dentro.Count == 0, string.Join(",", dentro));

            if (!p.CanInteract) yield break; // i decori non si toccano

            // raggiungibile: una cella percorribile entro la portata di interazione (1,6 m dal collider)
            Vector3 cella; float dist;
            var centro = b.center;
            bool trovata = Vicina(g, new Vector3(centro.x, p.transform.position.y, centro.z), 3.0f, out cella, out dist);
            Check(n + ": raggiungibile a piedi dal punto di partenza", trovata && dist <= 1.4f, "cella a " + dist.ToString("F2") + " m, " + cella.ToString("F2"));
            if (!trovata) yield break;

            // il PlayerInteractor lo propone: ci si mette sulla cella percorribile piu' vicina, rivolti verso il pezzo
            // si prova dalla cella piu' vicina e, se un interattore principale sta piu' vicino (i segreti cedono il passo), dalle prossime piu' vicine entro 1,3 m
            var pi = PlayerInteractor.Active;
            var candidate = new List<Vector3>();
            foreach (var c in g.celle)
                if (Mathf.Abs(c.y - cella.y) < 0.5f && new Vector2(c.x - centro.x, c.z - centro.z).magnitude <= 1.3f) candidate.Add(c);
            candidate.Sort((x1, x2) => new Vector2(x1.x - centro.x, x1.z - centro.z).sqrMagnitude.CompareTo(new Vector2(x2.x - centro.x, x2.z - centro.z).sqrMagnitude));
            if (candidate.Count == 0) candidate.Add(cella);
            bool proposto = false; string chi = "nessun prompt";
            for (int k = 0; k < candidate.Count && k < 14 && !proposto; k += Mathf.Max(1, candidate.Count / 14))
            {
                Teleporta(candidate[k], centro);
                yield return Attendi(0.3f);
                proposto = pi != null && pi.Current == p;
                if (!proposto && pi != null && pi.Current != null) chi = "propone " + pi.Current.name;
            }
            Check(n + ": il giocatore vicino lo vede proposto (prompt di interazione)", proposto, chi);

            // visibile: dalle celle entro 3 m con linea libera, il lato lungo a schermo (FOV 50, 1280x720, occhio a 1,6 m) e' almeno 25 px
            float migliore = 0f; Vector3 occhioMigliore = Vector3.zero;
            float lato = Mathf.Max(b.size.x, b.size.z, b.size.y);
            foreach (var c in g.celle)
            {
                float dd = Vector3.Distance(new Vector3(c.x, 0, c.z), new Vector3(centro.x, 0, centro.z));
                if (dd > 3f || Mathf.Abs(c.y - p.transform.position.y) > 1.6f) continue;
                var occhio = c + Vector3.up * 1.6f;
                if (!Libero(occhio, centro, p.transform)) continue;
                float dist3 = Vector3.Distance(occhio, centro);
                float px = lato / Mathf.Max(0.3f, dist3) * (720f / (50f * Mathf.Deg2Rad));
                if (px > migliore) { migliore = px; occhioMigliore = occhio; }
            }
            Check(n + ": si vede da qualche punto raggiungibile (linea libera, >= 25 px)", migliore >= 25f, migliore.ToString("F0") + " px");
            if (migliore > 0f)
            {
                // per i pezzi bassi la ripresa scende vicino al pezzo (sotto un tavolo si vede solo guardando di sbieco)
                // ripresa "da camera di gioco": a circa un metro dal pezzo, dal lato della cella, un po' sopra e rivolta verso di lui
                Vector3 orizz = new Vector3(occhioMigliore.x - centro.x, 0f, occhioMigliore.z - centro.z);
                orizz = orizz.sqrMagnitude > 0.01f ? orizz.normalized : Vector3.back;
                float dist1 = giocatoreAdulto ? 1.15f : 0.9f;
                Vector3 da = centro + orizz * dist1 + Vector3.up * 0.7f;
                RaycastHit bh;
                if (Physics.Linecast(centro + Vector3.up * 0.05f, da, out bh, ~0, QueryTriggerInteraction.Ignore) && Struttura(bh.collider)) da = centro + orizz * 0.7f + Vector3.up * 0.5f;
                yield return Scatta("pezzo_" + n, da, centro, 50f);
            }
        }

        // ------------------------------------------------------------------ flusso
        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);
            ControllaJson();
            if (soloFoto) { yield return SoloFoto(); yield break; }

            // ---------------- salvataggio vecchio: i campi dei segreti mancano e non rompono nulla
            var vecchio = JsonUtility.FromJson<SaveData>("{\"scena\":\"10_Hub_Vicolo\",\"inventario\":[\"scarpe\"]}");
            Check("salvataggio vecchio senza segreti: letto, liste vuote", vecchio != null && vecchio.inventario.Count == 1 && (vecchio.segretiTrovati == null || vecchio.segretiTrovati.Count == 0));

            yield return NuovaPartita();
            Check("hub caricato", SceneManager.GetActiveScene().name == GameManager.SceneHub);
            Check("nuova partita: nessun segreto trovato, contatore 0", SegretiStato.Trovati() == 0);
            Check("la pausa mostra il contatore, il quaderno le caselle vuote", SegretiStato.RigaPausa().Contains("0/" + SegretiStato.Totale()), SegretiStato.RigaPausa());
            yield return StanzaHub();
            yield return StanzaBasso();
            yield return StanzaRicordo();
            yield return Persistenza();
            yield return Indizio();
            yield return UovaDiPasqua();
            yield return NienteBlocca();
            yield return AiutoAutomatico();
            yield return RiviviIlRicordo();
            Log("--- Segreti trovati: " + string.Join(", ", GameManager.I.Save.segretiTrovati));
        }

        // ------------------------------------------------------------------ controlli statici del JSON
        void ControllaJson()
        {
            var f = F;
            Check("segreti.json: almeno un segreto per stanza", f.segreti.Count >= 15, f.segreti.Count + " segreti");
            foreach (var st in f.stanze)
            {
                int coll = f.segreti.FindAll(s => s.scena == st.scena && s.tipo == "collezionabile").Count;
                int en = f.segreti.FindAll(s => s.scena == st.scena && s.tipo == "enigma").Count;
                Check(st.scena + ": >= 1 enigma segreto e 3-5 collezionabili", en >= 1 && coll >= 3 && coll <= 5, en + " enigmi, " + coll + " collezionabili");
            }
            var ids = new HashSet<string>();
            foreach (var s in f.segreti)
            {
                Check("segreto " + s.id + ": id unico", ids.Add(s.id));
                Check("segreto " + s.id + ": nome e scheda scritti", !string.IsNullOrEmpty(s.nome) && !string.IsNullOrEmpty(s.scheda));
                bool haPezzo = f.pezzi.Exists(p => p.segreto == s.id || p.id == s.id) || f.enigmi.Exists(e => e.segreto == s.id);
                Check("segreto " + s.id + ": ha un pezzo di mondo o un enigma", haPezzo);
                if (s.tipo == "enigma") Check("segreto " + s.id + ": ha la pagina dietro le quinte", !string.IsNullOrEmpty(s.ricompensa));
            }
            // nessun segreto serve alla storia: i gettoni dei segreti non compaiono fra i requisiti degli enigmi principali ne' delle porte
            var token = new HashSet<string>();
            foreach (var p in f.pezzi) { token.Add(p.id); foreach (var t in p.produce) token.Add(t); }
            foreach (var e in f.enigmi) { token.Add(e.id); foreach (var t in e.produce) token.Add(t); }
            foreach (var c in f.campane) token.Add(c.produce);
            foreach (var s in f.segreti) token.Add(s.id);
            int abusi = 0; string quali = "";
            foreach (var e in GameData.EnigmiMondo) foreach (var r in e.richiede) if (token.Contains(r)) { abusi++; quali += e.id + ":" + r + " "; }
            foreach (var l in GameData.Luoghi) foreach (var r in l.richiede) if (token.Contains(r)) { abusi++; quali += l.id + ":" + r + " "; }
            var fb = GameData.Flashback("flashback_scarpe");
            if (fb != null) foreach (var e in fb.enigmi) foreach (var r in e.richiede) if (token.Contains(r)) { abusi++; quali += e.id + ":" + r + " "; }
            Check("nessun requisito di porte o enigmi principali dipende da un segreto", abusi == 0, quali);
            // la minimappa non rivela i segreti: nessun nome o id di segreto in mappe.json
            var ta = Resources.Load<TextAsset>("Data/mappe");
            bool rivela = false; string cosa = "";
            if (ta != null)
            {
                foreach (var s in f.segreti) if (ta.text.Contains("\"" + s.id + "\"") || (s.nome.Length > 6 && ta.text.Contains(s.nome))) { rivela = true; cosa += s.id + " "; }
                foreach (var p in f.pezzi) if (ta.text.Contains("\"" + p.id + "\"")) { rivela = true; cosa += p.id + " "; }
            }
            Check("la minimappa e la mappa grande (mappe.json) non nominano alcun segreto", ta != null && !rivela, cosa);
            // nessun testo narrativo nel codice: i segreti di prova hanno testo dal JSON
            int senzaPrompt = 0;
            foreach (var p in f.pezzi) if (p.ruolo != "decoro" && string.IsNullOrEmpty(p.prompt)) senzaPrompt++;
            Check("ogni pezzo interattivo ha il suo prompt nel JSON", senzaPrompt == 0, senzaPrompt.ToString());
            // modelli usati dalle parti
            var nomi = new SortedSet<string>();
            foreach (var p in f.pezzi) foreach (var pt in p.parti) if (!string.IsNullOrEmpty(pt.modello)) nomi.Add(pt.modello);
            foreach (var e in f.enigmi) foreach (var pt in e.parti) if (!string.IsNullOrEmpty(pt.modello)) nomi.Add(pt.modello);
            foreach (var m in nomi) Check("modello " + m + " caricabile", Resources.Load<GameObject>("Modelli/Enigmi/" + m) != null);
        }

        List<SegretoPezzo> PezziScena()
        {
            var l = new List<SegretoPezzo>();
            foreach (var p in FindObjectsByType<SegretoPezzo>(FindObjectsInactive.Include)) if (p != null && p.def != null) l.Add(p);
            l.Sort((a, c) => string.CompareOrdinal(a.def.id, c.def.id));
            return l;
        }

        SegretoPezzo Pezzo(string id)
        {
            foreach (var p in FindObjectsByType<SegretoPezzo>(FindObjectsInactive.Include)) if (p != null && p.def != null && p.def.id == id) return p;
            return null;
        }

        EnigmaMondo Enigma(string id)
        {
            foreach (var e in EnigmaMondo.Tutti) if (e != null && e.def != null && e.def.id == id) return e;
            return null;
        }

        /// <summary>Controlla e poi raccoglie ogni pezzo di una scena (esclusi quelli che dipendono da un enigma o da una condizione).</summary>
        IEnumerator ControllaEPrendi(string scena, bool adulto, string[] ordine)
        {
            int rend = 0, luci = 0, tri = 0;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == "SegretiMondo")
                {
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true)) rend++;
                    foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null) tri += (int)(mf.sharedMesh.GetIndexCount(0) / 3);
                    luci += root.GetComponentsInChildren<Light>(true).Length;
                }
            Log("  peso dei segreti in " + scena + ": " + rend + " renderer, ~" + tri + " triangoli, " + luci + " luci dinamiche");
            Check(scena + ": i segreti pesano poco (< 120 renderer, nessuna luce dinamica)", rend < 120 && luci == 0, rend + " renderer, " + luci + " luci");
            var g = Percorribile(adulto ? 0.28f : 0.2f, adulto ? 1.75f : 1.0f);
            Check(scena + ": area percorribile calcolata", g.celle.Count > 500, g.celle.Count + " celle");
            foreach (var id in ordine)
            {
                var p = Pezzo(id);
                Check(id + ": pezzo presente nel mondo", p != null);
                if (p == null) continue;
                if (!p.Visibile) { Log("  " + id + " non e' ancora visibile (dipende da una condizione)"); continue; }
                yield return ControllaPezzo(p, g, adulto);
            }
            yield return null;
        }

        IEnumerator Prendi(string id, bool conSegreto)
        {
            var p = Pezzo(id);
            if (p == null) { Check(id + ": presente per essere preso", false); yield break; }
            var sg = string.IsNullOrEmpty(p.def.segreto) ? null : p.def.segreto;
            int prima = SegretiStato.Trovati();
            p.Interact();
            yield return Attendi(0.7f);
            yield return Foto("raccolta_" + id);
            yield return ChiudiMessaggi();
            Check(id + ": raccolto, il pezzo sparisce dal mondo", !p.Visibile);
            if (conSegreto) Check(id + ": il segreto e' salvato", SegretiStato.Trovato(sg) && SegretiStato.Trovati() == prima + 1);
            else Check(id + ": il gettone e' nella borsa", MondoStato.Ha(id));
        }

        // ------------------------------------------------------------------ hub
        IEnumerator StanzaHub()
        {
            Log("=== Hub ===");
            yield return InterattoriPrincipali("hub prima");
            var pezzi = PezziScena();
            Check("hub: pezzi dei segreti creati", pezzi.Count >= 8, pezzi.Count + " pezzi");
            yield return ControllaEPrendi(GameManager.SceneHub, true, new[] { "bigl_scarpa", "bigl_mela", "bigl_treno", "hub_cartolina", "hub_settebello", "hub_trottola", "hub_amlire", "hub_ritaglio" });

            var gh = Percorribile(0.28f, 1.75f);
            foreach (var id in new[] { "hub_cartolina", "hub_settebello", "hub_trottola", "hub_amlire", "hub_ritaglio" })
            {
                var pz = Pezzo(id);
                Vector3 cc; float dd;
                if (pz != null && Vicina(gh, pz.transform.position, 3f, out cc, out dd)) { Teleporta(cc, pz.transform.position); yield return Attendi(0.3f); }
                yield return Prendi(id, true);
            }
            Check("hub: cinque collezionabili trovati", SegretiStato.Trovati(GameManager.SceneHub) == 5, SegretiStato.Trovati(GameManager.SceneHub).ToString());

            // enigma "Le tre edicole": senza i biglietti la portella e' chiusa con un messaggio gentile
            var e = Enigma("enigma_portella");
            Check("hub: la portella del sottoscala esiste", e != null);
            if (e == null) yield break;
            var cdp = e.GetComponent<Collider>();
            Check("hub: la portella e' prima del finale non-risolta e non e' marcata (nessuna freccia)", !e.Risolto && e.GetComponent<Glow>() != null && !e.GetComponent<Glow>().on);
            e.Interact();
            yield return Attendi(0.6f);
            Check("hub: senza i biglietti la portella non si apre (messaggio gentile)", V == null && !MondoStato.Risolto("segreti/enigma_portella"));
            yield return ChiudiMessaggi();

            // i biglietti, in un ordine diverso dalla filastrocca
            var g = Percorribile(0.28f, 1.75f);
            foreach (var id in new[] { "bigl_treno", "bigl_scarpa", "bigl_mela" })
            {
                var p = Pezzo(id);
                if (p == null) continue;
                Vector3 cella; float dist;
                if (Vicina(g, p.transform.position, 3f, out cella, out dist)) { Teleporta(cella, p.transform.position); yield return Attendi(0.3f); }
                if (id == "bigl_mela") yield return Foto("edicole_mondo_mela");
                yield return Prendi(id, false);
            }
            Check("hub: i tre biglietti sono nel quaderno (Trovati finora)", MondoStato.Ha("bigl_scarpa") && MondoStato.Ha("bigl_mela") && MondoStato.Ha("bigl_treno") && MondoStato.Descrizione("bigl_mela").Length > 5);

            // la portella: ordine sbagliato (errore gentile), poi giusto
            Vector3 cp; float dp;
            if (Vicina(g, e.transform.position, 3f, out cp, out dp)) { Teleporta(cp, e.transform.position); yield return Attendi(0.4f); }
            yield return Foto("hub_portella_mondo");
            var cam0 = e.transform.position;
            e.Interact();
            yield return Attendi(1.2f);
            Check("hub: con i biglietti la portella si apre in vista ravvicinata", V != null);
            if (V == null) yield break;
            yield return Foto("hub_portella_aperta");
            Check("hub: l'Indizio dentro l'enigma tace (nessun indizio prima della stanza completata)", V.Def.indizi.Count == 0);
            yield return Parte("treno");
            Check("hub: ordine sbagliato = messaggio gentile e si ricomincia", V != null && !V.Risolto && Messaggio().Length > 5, Messaggio());
            yield return Foto("hub_portella_errore");
            yield return Attendi(0.5f);
            foreach (var id in new[] { "scarpa", "mela", "treno" }) yield return Parte(id);
            Check("hub: la filastrocca nell'ordine giusto risolve", V != null && V.Risolto);
            yield return Attendi(1.4f);
            yield return Foto("hub_portella_risolta");
            if (V != null) V.Finito();
            yield return Attendi(1.0f);
            yield return ChiudiMessaggi();
            Check("hub: enigma segreto salvato e segreto trovato", MondoStato.Risolto("segreti/enigma_portella") && SegretiStato.Trovato("hub_edicole"));
            yield return InterattoriPrincipali("hub dopo");
            Check("hub: sei segreti su sei", SegretiStato.Trovati(GameManager.SceneHub) == SegretiStato.Totale(GameManager.SceneHub), SegretiStato.Trovati(GameManager.SceneHub) + "/" + SegretiStato.Totale(GameManager.SceneHub));
            // l'oggetto ricompensa: il quaderno mostra scheda e dietro le quinte
            var voci = SegretiStato.VociQuaderno();
            var vh = voci.Find(v => v.id == GameManager.SceneHub);
            Check("hub: la pagina Segreti mostra " + SegretiStato.Totale(GameManager.SceneHub) + "/" + SegretiStato.Totale(GameManager.SceneHub), vh != null && vh.titolo.Contains(SegretiStato.Totale(GameManager.SceneHub) + "/" + SegretiStato.Totale(GameManager.SceneHub)), vh != null ? vh.titolo : "-");
            Check("hub: la voce dell'enigma ha la pagina dietro le quinte", vh != null && vh.testo.Contains(F.testi.dietroQuinteTitolo));
        }

        // ------------------------------------------------------------------ basso
        IEnumerator StanzaBasso()
        {
            Log("=== Basso ===");
            yield return Vai("20_Int_BassoAntonietta", "ingresso");
            var pezzi = PezziScena();
            Check("basso: pezzi dei segreti creati", pezzi.Count >= 7, pezzi.Count + " pezzi");
            // nessun bagliore automatico sugli enigmi segreti: ne' con "evidenzia interattivi" ne' dopo 150 s senza progressi
            {
                var sv = GameManager.I.Save;
                bool evid = sv.evidenziaInterattivi; float up = EnigmaMondo.UltimoProgresso;
                sv.evidenziaInterattivi = true;
                yield return Attendi(1.0f);
                var normale = Enigma("enigma_foto"); var segr = Enigma("enigma_ripostiglio");
                Check("basso: con 'evidenzia interattivi' brilla l'enigma normale e non quello segreto", normale != null && segr != null && normale.GetComponent<Glow>().on && !segr.GetComponent<Glow>().on);
                sv.evidenziaInterattivi = false;
                float soglia = EnigmaMondo.SogliaBagliore;
                EnigmaMondo.SogliaBagliore = 0.2f;          // "150 s senza progressi" accorciati per la prova
                EnigmaMondo.UltimoProgresso = Mathf.Max(0.001f, Time.time - 5f);
                yield return Attendi(1.0f);
                Check("basso: dopo i secondi senza progressi brilla l'enigma normale e non quello segreto", normale != null && segr != null && normale.GetComponent<Glow>().on && !segr.GetComponent<Glow>().on);
                EnigmaMondo.SogliaBagliore = soglia;
                EnigmaMondo.UltimoProgresso = up;
                yield return Attendi(0.6f);
            }
            yield return InterattoriPrincipali("basso prima");
            yield return ControllaEPrendi("20_Int_BassoAntonietta", true, new[] { "basso_graffi", "basso_santino", "basso_mattonella", "basso_bottone", "basso_biglia" });
            // la mattonella: si solleva e compare il ditale
            var dit = Pezzo("basso_ditale");
            Check("basso: il ditale non si vede finche' la mattonella e' al suo posto", dit != null && !dit.Visibile);
            yield return Prendi("basso_mattonella", false);
            yield return Attendi(0.5f);
            Check("basso: sollevata la mattonella compare il ditale", dit != null && dit.Visibile);
            if (dit != null)
            {
                var g = Percorribile(0.28f, 1.75f);
                yield return ControllaPezzo(dit, g, true);
            }
            yield return Prendi("basso_santino", true);
            yield return Prendi("basso_ditale", true);
            yield return Prendi("basso_bottone", true);
            yield return Prendi("basso_biglia", true);
            Check("basso: quattro collezionabili trovati", SegretiStato.Trovati("20_Int_BassoAntonietta") == 4, SegretiStato.Trovati("20_Int_BassoAntonietta").ToString());

            // l'enigma: piatti, leva, credenza che scivola
            var e = Enigma("enigma_ripostiglio");
            Check("basso: i piatti della mensola esistono", e != null);
            if (e == null) yield break;
            Teleporta(new Vector3(-2.6f, 0f, 0.9f), e.transform.position);
            yield return Attendi(0.5f);
            yield return Foto("basso_ripostiglio_mondo");
            var cred = TrovaPerNome("Credenza_Mobile", e.transform.position) ?? TrovaPerNome("Credenza", e.transform.position);
            Vector3 credPrima = cred != null ? cred.position : Vector3.zero;
            Check("basso: la credenza di scena esiste", cred != null);
            e.Interact();
            yield return Attendi(1.2f);
            Check("basso: si apre la vista ravvicinata dei piatti", V != null);
            if (V == null) yield break;
            yield return Foto("basso_ripostiglio_aperto");
            Check("basso: l'Indizio dell'enigma segreto tace (indizi muti)", V.Def.indizi.Count == 0);
            yield return Parte("piatto_1");
            yield return Parte("piatto_2");
            yield return Foto("basso_ripostiglio_piatti");
            yield return Parte("piatto_3");
            yield return Attendi(0.3f);
            yield return Foto("basso_ripostiglio_fondo");
            yield return Parte("leva");
            Check("basso: tolti i piatti e tirata la leva l'enigma e' risolto", V != null && V.Risolto);
            yield return Attendi(1.4f);
            yield return Foto("basso_ripostiglio_risolto");
            if (V != null) V.Finito();
            yield return Attendi(1.0f);
            yield return ChiudiMessaggi();
            Check("basso: segreto trovato e salvato", MondoStato.Risolto("segreti/enigma_ripostiglio") && SegretiStato.Trovato("basso_ripostiglio"));
            yield return Attendi(2.5f);
            Check("basso: la credenza scivola via (e' stata spostata di oltre 70 cm)", cred != null && Vector3.Distance(cred.position, credPrima) > 0.7f, cred != null ? Vector3.Distance(cred.position, credPrima).ToString("F2") : "-");
            var mat = Pezzo("basso_materassino");
            Check("basso: compare il materassino nel ripostiglio", mat != null && mat.Visibile);
            var nic = Pezzo("basso_nicchia");
            Check("basso: compare la nicchia murata", nic != null && nic.Visibile);
            Teleporta(new Vector3(-2.7f, 0f, 1.0f), new Vector3(-4.3f, 0.5f, 0.6f));
            yield return Attendi(0.8f);
            yield return Foto("basso_ripostiglio_aperto_mondo");
            if (mat != null)
            {
                var g = Percorribile(0.28f, 1.75f);
                yield return ControllaPezzo(mat, g, true);
                mat.Interact();
                yield return Attendi(0.6f);
                yield return Foto("basso_materassino_msg");
                yield return ChiudiMessaggi();
            }
            yield return InterattoriPrincipali("basso dopo (credenza spostata)");
            Check("basso: cinque segreti su cinque", SegretiStato.Trovati("20_Int_BassoAntonietta") == SegretiStato.Totale("20_Int_BassoAntonietta"), SegretiStato.Trovati("20_Int_BassoAntonietta") + "/" + SegretiStato.Totale("20_Int_BassoAntonietta"));
        }

        Transform TrovaPerNome(string nome, Vector3 vicino)
        {
            Transform best = null; float bd = 4f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != nome) continue;
                    float d = Vector3.Distance(t.position, vicino);
                    if (d < bd) { bd = d; best = t; }
                }
            return best;
        }

        // ------------------------------------------------------------------ ricordo
        IEnumerator StanzaRicordo()
        {
            Log("=== Ricordo ===");
            yield return Vai("30_FB_Scarpe", "inizio");
            var pezzi = PezziScena();
            Check("ricordo: pezzi dei segreti creati", pezzi.Count >= 8, pezzi.Count + " pezzi");
            yield return ControllaEPrendi("30_FB_Scarpe", false, new[] { "ric_graffito", "ric_moneta", "ric_pallone", "ric_lista", "ric_uovo", "ric_bottone", "ric_pietra" });
            foreach (var id in new[] { "ric_moneta", "ric_pallone", "ric_lista", "ric_uovo", "ric_bottone" }) yield return Prendi(id, true);
            Check("ricordo: cinque collezionabili trovati", SegretiStato.Trovati("30_FB_Scarpe") == 5, SegretiStato.Trovati("30_FB_Scarpe").ToString());

            // campana di gesso: prima del graffito e' solo un gioco (nessun errore, nessun messaggio)
            var camp = FindAnyObjectByType<CampanaGesso>();
            Check("ricordo: la campana di gesso e' disegnata sul selciato", camp != null);
            if (camp == null) yield break;
            var def = camp.Def;
            Vector3 CasellaPos(int n) { foreach (var c in def.caselle) if (c.n == n) return camp.transform.TransformPoint(new Vector3(c.x, 0f, c.z)); return camp.transform.position; }
            Teleporta(CasellaPos(5) + new Vector3(0f, 0.05f, 0f), CasellaPos(1));
            yield return Attendi(0.5f);
            Teleporta(CasellaPos(5) + new Vector3(0f, 0.05f, 0f), CasellaPos(1));
            yield return Attendi(0.8f);
            Check("ricordo: prima di leggere il graffito la campana non reagisce", !MondoStato.Ha("campana_ok") && !ToastContiene(F.testi.campanaSbagliata));
            // il graffito con la sequenza
            var gr = Pezzo("ric_graffito");
            Check("ricordo: il graffito si legge e da' il numero", gr != null);
            if (gr != null)
            {
                var gg = Percorribile(0.2f, 1.0f);
                Vector3 cc; float dd;
                if (Vicina(gg, gr.transform.position, 3f, out cc, out dd)) Teleporta(cc, gr.transform.position);
                yield return Attendi(0.4f);
                yield return Foto("ricordo_graffito_mondo");
                gr.Interact();
                yield return Attendi(0.8f);
                yield return Foto("ricordo_graffito_msg");
                yield return ChiudiMessaggi();
                Check("ricordo: letto il graffito il numero e' nel quaderno", MondoStato.Ha("numero_campana") && MondoStato.Descrizione("numero_campana").Contains("4 2 7 1"));
            }
            Teleporta(CasellaPos(1) + new Vector3(0f, 0.1f, -1.8f), CasellaPos(3));
            yield return Attendi(0.6f);
            yield return Foto("ricordo_campana_mondo");
            // ordine sbagliato: 1, 2, 3 -> messaggio gentile e si ricomincia
            foreach (var n in new[] { 1, 2, 3 }) { Teleporta(CasellaPos(n) + new Vector3(0f, 0.05f, 0f), CasellaPos(n) + Vector3.forward); yield return Attendi(0.7f); }
            Check("ricordo: saltando nell'ordine sbagliato si ricomincia con un messaggio gentile", !MondoStato.Ha("campana_ok") && ToastContiene(F.testi.campanaSbagliata));
            yield return Attendi(1.0f);
            // ordine giusto
            foreach (var ch in def.sequenza)
            {
                int n = ch - '0';
                Teleporta(CasellaPos(n) + new Vector3(0f, 0.05f, 0f), CasellaPos(n) + Vector3.forward);
                yield return Attendi(0.75f);
            }
            Check("ricordo: la sequenza 4-2-7-1 fa scattare la campana", MondoStato.Ha("campana_ok"));
            var pietra = Pezzo("ric_pietra");
            Vector3 pietraPrima = pietra != null ? pietra.transform.position : Vector3.zero;
            yield return Attendi(1.4f);
            yield return Foto("ricordo_campana_fatta");
            Check("ricordo: la pietra della fontanella scivola via", pietra != null && Vector3.Distance(pietra.transform.position, pietraPrima) > 0.2f);
            var soldo = Pezzo("ric_soldo");
            Check("ricordo: compare il soldo nella cavita'", soldo != null && soldo.Visibile);
            if (soldo != null)
            {
                var gg = Percorribile(0.2f, 1.0f);
                Vector3 cc; float dd;
                if (Vicina(gg, soldo.transform.position, 3f, out cc, out dd)) Teleporta(cc, soldo.transform.position);
                yield return Attendi(0.4f);
                yield return ControllaPezzo(soldo, gg, false);
                yield return Foto("ricordo_soldo_mondo");
                soldo.Interact();
                yield return Attendi(0.7f);
                yield return Foto("ricordo_soldo_msg");
                yield return ChiudiMessaggi();
                yield return Attendi(0.5f);
                yield return ChiudiMessaggi();
            }
            Check("ricordo: soldo trovato, segreto salvato", SegretiStato.Trovato("ric_campana"));
            Check("ricordo: sei segreti su sei", SegretiStato.Trovati("30_FB_Scarpe") == SegretiStato.Totale("30_FB_Scarpe"), SegretiStato.Trovati("30_FB_Scarpe") + "/" + SegretiStato.Totale("30_FB_Scarpe"));
            Check("tutti i segreti: flag per R016 impostato", SegretiStato.Flag("segreti_tutti") && SegretiStato.Trovati() == SegretiStato.Totale());
        }

        // ------------------------------------------------------------------ salvataggio, quaderno e pausa
        IEnumerator Persistenza()
        {
            Log("=== Salvataggio, quaderno, pausa ===");
            GameManager.I.SaveGame();
            var riletto = SaveSystem.Load();
            Check("salvataggio: i segreti trovati sono salvati (treno_save)", riletto.segretiTrovati.Count == SegretiStato.Totale(), riletto.segretiTrovati.Count + "/" + SegretiStato.Totale());
            Check("salvataggio: il flag 'segreti_tutti' e' salvato", riletto.segretiFlag.Contains("segreti_tutti"));
            GameManager.I.Continue();
            yield return ScenaPronta("30_FB_Scarpe");
            Check("ricaricato: i collezionabili gia' trovati non riappaiono", PezziScena().FindAll(p => p.def.ruolo == "raccogli" && p.Visibile).Count == 0);
            var campR = FindAnyObjectByType<CampanaGesso>();
            Check("ricaricato: la campana gia' risolta resta disegnata ma la sua logica e' spenta", campR != null && campR.fatto && campR.GetComponentsInChildren<Renderer>().Length > 10);
            // pagina Segreti del quaderno: apre davvero il quaderno e fotografa
            var voci = SegretiStato.VociQuaderno();
            Check("quaderno: una voce per stanza", voci.Count == 3, voci.Count.ToString());
            foreach (var v in voci) Log("  " + v.titolo);
            Check("quaderno: 'Il basso di Antonietta   5/5'", voci.Exists(v => v.titolo.Contains("5/5") && v.titolo.Contains("basso")));
            Check("quaderno: 'Il ricordo delle scarpe   6/6'", voci.Exists(v => v.titolo.Contains("6/6")));
            Check("quaderno: 'Il vicolo   6/6'", voci.Exists(v => v.id == GameManager.SceneHub && v.titolo.Contains("6/6")));
            Check("quaderno: ogni segreto trovato ha la sua scheda scritta", voci.TrueForAll(v => !v.testo.Contains(F.testi.casellaIgnota)));
            var ui = UIRoot.I;
            if (ui != null && ui.Inventory != null)
            {
                ui.Inventory.Show();
                yield return Attendi(1.0f);
                var m = typeof(InventoryPanel).GetMethod("VaiA", BindingFlags.NonPublic | BindingFlags.Instance);
                var tipoPag = typeof(InventoryPanel).GetNestedType("Pagina", BindingFlags.NonPublic);
                if (m != null && tipoPag != null) { m.Invoke(ui.Inventory, new object[] { Enum.Parse(tipoPag, "Quaderno"), false }); yield return Attendi(0.8f); }
                // scorre in fondo alla pagina dei segreti
                foreach (var sr in FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude)) if (sr.gameObject.activeInHierarchy) sr.verticalNormalizedPosition = 0f;
                yield return Attendi(0.4f);
                yield return Foto("quaderno_segreti_tutti");
                ui.Inventory.Hide();
                yield return Attendi(0.8f);
            }
            if (ui != null && ui.Pause != null)
            {
                ui.Pause.Show();
                yield return Attendi(1.0f);
                var campo = typeof(PausePanel).GetField("segretiTxt", BindingFlags.NonPublic | BindingFlags.Instance);
                var tx = campo != null ? campo.GetValue(ui.Pause) as TMP_Text : null;
                Check("pausa: il contatore dei segreti compare nel menu pausa", tx != null && tx.gameObject.activeInHierarchy && tx.text.Contains(SegretiStato.Totale() + "/" + SegretiStato.Totale()), tx != null ? tx.text : "-");
                yield return Foto("pausa_contatore");
                ui.Pause.Hide();
                yield return Attendi(0.8f);
            }
            Check("l'HUD non mostra mai il contatore dei segreti", !ToastContiene(F.testi.contatoreEtichetta));
        }

        // ------------------------------------------------------------------ Indizio
        IEnumerator Indizio()
        {
            Log("=== Indizio ===");
            var gm = GameManager.I;
            var s = gm.Save;
            // stanza non completata + segreto mancante: l'Indizio tace
            var salvaTrovati = new List<string>(s.segretiTrovati);
            var salvaRis = new List<string>(s.enigmiRisolti); var salvaFb = new List<string>(s.flashbackCompletati);
            s.segretiTrovati.Remove("ric_bottone");
            s.enigmiRisolti.Clear(); s.flashbackCompletati.Clear();
            Check("indizio: a stanza non completata la stanza non risulta completa", !SegretiStato.StanzaCompleta("30_FB_Scarpe"));
            TrenoBambini.Mappa.Obiettivi.RivelaIndizio();
            yield return Attendi(0.5f);
            Check("indizio: a stanza non completata tace sui segreti", !ToastContiene(F.testi.aiutoLieve));
            // stanza completata: accenna soltanto
            foreach (var c in F.stanze.Find(x => x.scena == "30_FB_Scarpe").completo) if (c.StartsWith("ris:")) s.enigmiRisolti.Add(c.Substring(4));
            Check("indizio: a stanza completata la stanza risulta completa", SegretiStato.StanzaCompleta("30_FB_Scarpe"));
            TrenoBambini.Mappa.Obiettivi.RivelaIndizio();
            yield return Attendi(0.5f);
            Check("indizio: a stanza completata accenna 'In questa stanza c'e' ancora qualcosa'", ToastContiene(F.testi.aiutoLieve));
            yield return Foto("indizio_lieve");
            Check("indizio: non indica dove", !ToastContiene("fontanella") && !ToastContiene("sarto"));
            // tutto trovato: tace di nuovo
            s.segretiTrovati.Clear(); s.segretiTrovati.AddRange(salvaTrovati);
            yield return Attendi(5.5f);
            TrenoBambini.Mappa.Obiettivi.RivelaIndizio();
            yield return Attendi(0.4f);
            Check("indizio: se non resta nulla da trovare tace", !ToastContiene(F.testi.aiutoLieve));
            s.enigmiRisolti.Clear(); s.enigmiRisolti.AddRange(salvaRis);
            s.flashbackCompletati.Clear(); s.flashbackCompletati.AddRange(salvaFb);
        }

        // ------------------------------------------------------------------ easter egg
        IEnumerator UovaDiPasqua()
        {
            Log("=== Easter egg ===");
            var s = GameManager.I.Save;
            // violino lontano: solo dopo il finale (aggancio R016)
            s.segretiFlag.Remove("finale_visto");
            yield return Vai(GameManager.SceneHub, "inizio");
            var vi = FindAnyObjectByType<UovoSegreto>();
            Check("easter egg: senza il finale il violino non suona", vi == null || !vi.Attivo);
            SegretiStato.ImpostaFlag("finale_visto");
            yield return Vai(GameManager.SceneHub, "inizio");
            UovoSegreto violino = null;
            foreach (var u in FindObjectsByType<UovoSegreto>(FindObjectsInactive.Include)) if (u.def.tipo == "violino") violino = u;
            Check("easter egg: dopo il finale compare l'uovo del violino al gradino della chiesetta", violino != null && violino.Attivo);
            yield return Attendi(3.0f);
            Check("easter egg: il violino suona da lontano (sorgente 3D)", violino != null && violino.StaSuonando);
            // scarpine in piu' sul filo dopo il capitolo Cappotto (aggancio)
            s.flashbackCompletati.Remove("flashback_cappotto");
            yield return Vai("30_FB_Scarpe", "inizio");
            UovoSegreto sc = null;
            foreach (var u in FindObjectsByType<UovoSegreto>(FindObjectsInactive.Include)) if (u.def.tipo == "scarpe") sc = u;
            Check("easter egg: prima del capitolo Cappotto non ci sono scarpine in piu'", sc == null || !sc.Attivo);
            s.flashbackCompletati.Add("flashback_cappotto");
            yield return Vai(GameManager.SceneHub, "inizio");
            yield return Vai("30_FB_Scarpe", "inizio");
            foreach (var u in FindObjectsByType<UovoSegreto>(FindObjectsInactive.Include)) if (u.def.tipo == "scarpe") sc = u;
            Check("easter egg: dopo il capitolo Cappotto un paio piccolo in piu' sul filo", sc != null && sc.Attivo && sc.GetComponentsInChildren<Renderer>().Length >= 3, sc != null ? sc.Attivo + " " + sc.GetComponentsInChildren<Renderer>().Length + " renderer" : "nessun uovo");
            if (sc != null)
            {
                var gs = Percorribile(0.2f, 1.0f);
                Vector3 occhio = Vector3.zero; float best = 1e9f;
                foreach (var c in gs.celle)
                {
                    float dd = Vector3.Distance(new Vector3(c.x, 0, c.z), new Vector3(sc.transform.position.x, 0, sc.transform.position.z));
                    if (dd < 3.5f || dd > 8f) continue;
                    var o = c + Vector3.up * 1.4f;
                    if (!Libero(o, sc.transform.position, null)) continue;
                    if (Mathf.Abs(dd - 5f) < best) { best = Mathf.Abs(dd - 5f); occhio = o; }
                }
                Check("easter egg: le scarpine sul filo si vedono dalla strada", best < 1e8f);
                if (best < 1e8f) yield return Scatta("easter_scarpine_filo", occhio, sc.transform.position + Vector3.down * 0.1f, 32f);
            }
            s.flashbackCompletati.Remove("flashback_cappotto");
        }

        // ------------------------------------------------------------------ nessun segreto blocca la storia
        IEnumerator NienteBlocca()
        {
            Log("=== La storia non dipende dai segreti ===");
            // partita nuova, nessun segreto toccato: si completano gli enigmi e il capitolo con le sole regole della storia
            yield return NuovaPartita();
            var s = GameManager.I.Save;
            foreach (var e in GameData.EnigmiMondo) MondoStato.Completa(e, "mondo/" + e.id);
            var fb = GameData.Flashback("flashback_scarpe");
            foreach (var e in fb.enigmi) MondoStato.Completa(e, "flashback_scarpe/" + e.id);
            GameManager.I.CompleteFlashback("scarpe");
            yield return Attendi(1.0f);
            Check("storia: tutti gli enigmi principali risolti senza toccare un segreto", SegretiStato.Trovati() == 0 && s.flashbackCompletati.Contains("flashback_scarpe"));
            Check("storia: le stanze risultano completate", SegretiStato.StanzaCompleta(GameManager.SceneHub) && SegretiStato.StanzaCompleta("20_Int_BassoAntonietta") && SegretiStato.StanzaCompleta("30_FB_Scarpe"));
            Door porta = null;
            yield return ScenaPronta(GameManager.SceneHub);
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include)) if (d.luogoId == "basso_antonietta") porta = d;
            Check("storia: la porta del basso esiste", porta != null);
            if (porta != null)
            {
                var luogo = GameData.Luogo("basso_antonietta");
                bool segreto = false; foreach (var r in luogo.richiede) if (SegretiStato.GettoneSegreto(r)) segreto = true;
                Check("storia: la porta del basso non chiede alcun segreto", !segreto);
                porta.Interact();
                yield return ScenaPronta("20_Int_BassoAntonietta");
                Check("storia: senza aver toccato un segreto la porta si apre davvero sul basso", SceneManager.GetActiveScene().name == "20_Int_BassoAntonietta");
                yield return Vai(GameManager.SceneHub, "inizio");
            }
            Check("storia: nessun segreto nel salvataggio", s.segretiTrovati.Count == 0 && !s.segretiFlag.Contains("segreti_tutti"));
        }

        // ------------------------------------------------------------------ gli interattori principali restano raggiungibili e proposti
        readonly Dictionary<string, HashSet<string>> irraggiungibiliPrima = new Dictionary<string, HashSet<string>>();

        IEnumerator InterattoriPrincipali(string quando)
        {
            string scena = SceneManager.GetActiveScene().name;
            bool adulto = !scena.Contains("_FB_");
            var g = Percorribile(adulto ? 0.28f : 0.2f, adulto ? 1.75f : 1.0f);
            var l = new List<Interactable>();
            foreach (var e in EnigmaMondo.Tutti) if (e != null && !e.segreto && e.gameObject.scene.name == scena) l.Add(e);
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Exclude)) l.Add(d);
            foreach (var ip in FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude)) l.Add(ip);
            int ok2 = 0, male = 0; string quali = "";
            string scenaChiave = scena;
            bool dopo = quando.Contains("dopo");
            HashSet<string> base0; irraggiungibiliPrima.TryGetValue(scenaChiave, out base0);
            if (!dopo) irraggiungibiliPrima[scenaChiave] = new HashSet<string>();
            foreach (var it in l)
            {
                var col = it.GetComponentInChildren<Collider>();
                Vector3 p = col != null ? col.bounds.center : it.transform.position;
                Vector3 cella; float dist;
                bool trovata = Vicina(g, new Vector3(p.x, it.transform.position.y, p.z), 3f, out cella, out dist);
                if (!trovata || dist > 1.5f)
                {
                    // gia' fuori portata prima dei segreti (non e' colpa loro): si registra e si confronta nel "dopo"
                    if (!dopo) { irraggiungibiliPrima[scenaChiave].Add(it.name); Log("  (gia' non raggiungibile a piedi senza i segreti: " + it.name + ", a " + dist.ToString("F1") + " m)"); }
                    else if (base0 == null || !base0.Contains(it.name)) { male++; quali += it.name + "(a " + dist.ToString("F1") + " m) "; }
                    continue;
                }
                Teleporta(cella, p);
                yield return Attendi(0.3f);
                var pi = PlayerInteractor.Active;
                bool propone = pi != null && pi.Current != null && !(pi.Current is SegretoPezzo) && (pi.Current == it || pi.Current is Interactable);
                if (propone) ok2++; else { male++; quali += it.name + "(propone " + (pi != null && pi.Current != null ? pi.Current.name : "nulla") + ") "; }
            }
            Check("interattori principali (" + quando + "): quelli raggiungibili restano proposti, nessuno coperto da un segreto", l.Count > 0 && male == 0, ok2 + "/" + l.Count + " " + quali);
        }

        // ------------------------------------------------------------------ l'aiuto lieve compare da solo
        IEnumerator AiutoAutomatico()
        {
            Log("=== Aiuto lieve automatico ===");
            var s = GameManager.I.Save;
            GameManager.I.ResetBlocks();   // la cutscene del capitolo di prova non e' stata scorsa: si libera il gioco come farebbe il giocatore
            s.segretiFlag.RemoveAll(f => f.StartsWith("aiuto_", StringComparison.Ordinal));
            // hub: la stanza principale e' completa (capitolo finito) e nessun segreto trovato
            yield return Vai(GameManager.SceneHub, "inizio");
            yield return Attendi(3.5f);
            Check("aiuto automatico: a stanza completa e segreti da trovare compare da solo, senza premere Indizio", ToastContiene(F.testi.aiutoLieve) && s.segretiFlag.Contains("aiuto_" + GameManager.SceneHub));
            yield return Foto("aiuto_automatico_hub");
            yield return Attendi(7f);
            yield return Vai(GameManager.SceneHub, "inizio");
            yield return Attendi(4f);
            Check("aiuto automatico: una volta sola per stanza", !ToastContiene(F.testi.aiutoLieve));
            // ricordo: all'apertura dell'uscita (tutti gli enigmi risolti)
            yield return Vai("30_FB_Scarpe", "inizio");
            yield return Attendi(3.5f);
            Check("aiuto automatico nel ricordo: compare con l'uscita aperta", ToastContiene(F.testi.aiutoLieve) && s.segretiFlag.Contains("aiuto_30_FB_Scarpe"));
            // stanza non completa: tace
            s.segretiFlag.RemoveAll(f => f.StartsWith("aiuto_", StringComparison.Ordinal));
            var salvaFb = new List<string>(s.flashbackCompletati); s.flashbackCompletati.Clear();
            yield return Vai(GameManager.SceneHub, "inizio");
            yield return Attendi(3.5f);
            Check("aiuto automatico: a stanza non completa tace", !ToastContiene(F.testi.aiutoLieve) && !s.segretiFlag.Contains("aiuto_" + GameManager.SceneHub));
            s.flashbackCompletati.AddRange(salvaFb);
        }

        // ------------------------------------------------------------------ Rivivi il ricordo dalla latta
        IEnumerator RiviviIlRicordo()
        {
            Log("=== Rivivi il ricordo ===");
            var gm = GameManager.I; var s = gm.Save;
            gm.ResetBlocks();
            var pend = typeof(UIRoot).GetField("pendingNewItem", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pend != null) pend.SetValue(UIRoot.I, null);   // l'oggetto del capitolo finito poco fa non conta: si guarda che il rientro non ne metta in coda altri
            yield return Vai(GameManager.SceneHub, "inizio");
            // snapshot dello stato degli enigmi principali e dei progressi
            var risolti = new List<string>(); foreach (var k in s.enigmiRisolti) if (!k.StartsWith("segreti/", StringComparison.Ordinal)) risolti.Add(k);
            int token = 0; foreach (var t in s.oggettiEnigma) if (!SegretiStato.GettoneSegreto(t)) token++;
            int cut = s.cutsceneViste.Count, inv = s.inventario.Count, fbn = s.flashbackCompletati.Count;
            int trovatiPrima = SegretiStato.Trovati("30_FB_Scarpe");
            var ui = UIRoot.I;
            ui.Inventory.Show();
            yield return Attendi(1.0f);
            var m = typeof(InventoryPanel).GetMethod("VaiA", BindingFlags.NonPublic | BindingFlags.Instance);
            var tipoPag = typeof(InventoryPanel).GetNestedType("Pagina", BindingFlags.NonPublic);
            m.Invoke(ui.Inventory, new object[] { Enum.Parse(tipoPag, "Ricordi"), false });
            yield return Attendi(0.8f);
            Button bt = null;
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude)) if (b.name == "Rivivi_scarpe" && b.gameObject.activeInHierarchy) bt = b;
            Check("rivivi: la scheda Ricordi della latta ha il pulsante per il ricordo completato", bt != null);
            yield return Foto("ricordi_rivivi");
            if (bt == null) yield break;
            bt.onClick.Invoke();
            yield return ScenaPronta("30_FB_Scarpe");
            Check("rivivi: si rientra nel ricordo", SceneManager.GetActiveScene().name == "30_FB_Scarpe" && gm.Rivivendo);
            Check("rivivi: gli enigmi principali del ricordo restano risolti", s.enigmiRisolti.Contains("flashback_scarpe/enigma_lucchetto") && s.enigmiRisolti.Contains("flashback_scarpe/enigma_quiz"));
            var uscita = FindAnyObjectByType<EscapeExit>();
            Check("rivivi: l'uscita e' aperta", uscita != null && uscita.GetPrompt() == "Esci dal ricordo");
            yield return Foto("rivivi_ricordo");
            // un segreto rimasto si raccoglie
            var g2 = Percorribile(0.2f, 1.0f);
            var pz = Pezzo("ric_moneta");
            Vector3 cc; float dd;
            if (pz != null && Vicina(g2, pz.transform.position, 3f, out cc, out dd)) { Teleporta(cc, pz.transform.position); yield return Attendi(0.4f); }
            yield return Prendi("ric_moneta", true);
            Check("rivivi: raccolto un segreto del ricordo", SegretiStato.Trovati("30_FB_Scarpe") == trovatiPrima + 1);
            // uscita senza effetti collaterali
            Vector3 c2; float d2;
            if (uscita != null && Vicina(g2, uscita.transform.position, 3f, out c2, out d2)) { Teleporta(c2, uscita.transform.position); yield return Attendi(0.4f); }
            if (uscita != null) uscita.Interact();
            yield return Attendi(0.8f);
            yield return ChiudiMessaggi();
            yield return ScenaPronta(GameManager.SceneHub);
            Check("rivivi: si torna al vicolo", SceneManager.GetActiveScene().name == GameManager.SceneHub && !gm.Rivivendo);
            var risolti2 = new List<string>(); foreach (var k in s.enigmiRisolti) if (!k.StartsWith("segreti/", StringComparison.Ordinal)) risolti2.Add(k);
            int token2 = 0; foreach (var t in s.oggettiEnigma) if (!SegretiStato.GettoneSegreto(t)) token2++;
            Check("rivivi: stato degli enigmi principali invariato", risolti.Count == risolti2.Count && token == token2, risolti.Count + "->" + risolti2.Count + ", " + token + "->" + token2);
            Check("rivivi: niente cutscene ne' oggetto nuovo ne' progressi extra", s.cutsceneViste.Count == cut && s.inventario.Count == inv && s.flashbackCompletati.Count == fbn);
            Check("rivivi: nessuna animazione 'nuovo oggetto' in coda", pend == null || string.IsNullOrEmpty(pend.GetValue(UIRoot.I) as string));
            Check("rivivi: ritorno e salvataggio puliti (nessun ritorno pendente)", string.IsNullOrEmpty(s.ritornoScena));
        }

        // ------------------------------------------------------------------ foto di posizionamento
        IEnumerator SoloFoto()
        {
            yield return NuovaPartita();
            foreach (var sc in new[] { new[] { GameManager.SceneHub, "inizio" }, new[] { "20_Int_BassoAntonietta", "ingresso" }, new[] { "30_FB_Scarpe", "inizio" } })
            {
                yield return Vai(sc[0], sc[1]);
                bool adulto = sc[0] != "30_FB_Scarpe";
                var g = Percorribile(adulto ? 0.28f : 0.2f, adulto ? 1.75f : 1.0f);
                Log("== " + sc[0] + ": " + g.celle.Count + " celle");
                if (sc[0] == GameManager.SceneHub)
                {
                    // mappa a 0,5 m del lotto delle macerie ('#' = percorribile)
                    var sb2 = new StringBuilder();
                    for (float zz = -14f; zz >= -26f; zz -= 0.5f)
                    {
                        sb2.Append(zz.ToString("0.0").PadLeft(6)).Append(' ');
                        for (float xx = 10f; xx <= 28f; xx += 0.5f)
                        {
                            bool c = g.celle.Exists(q => Mathf.Abs(q.x - xx) < 0.26f && Mathf.Abs(q.z - zz) < 0.26f);
                            sb2.Append(c ? '#' : '.');
                        }
                        sb2.Append('\n');
                    }
                    Log("Lotto (x da 10 a 28, riga = z):\n" + sb2);
                }
                var elenco = new List<KeyValuePair<string, Vector3>>();
                foreach (var p in PezziScena()) elenco.Add(new KeyValuePair<string, Vector3>(p.def.id, p.transform.position));
                foreach (var e in EnigmaMondo.Tutti) if (e != null && e.def != null && e.chiave.StartsWith("segreti/")) elenco.Add(new KeyValuePair<string, Vector3>(e.def.id, e.transform.position));
                foreach (var c in FindObjectsByType<CampanaGesso>(FindObjectsInactive.Include)) elenco.Add(new KeyValuePair<string, Vector3>("campana", c.transform.position + c.transform.forward * 2f));
                foreach (var kv in elenco)
                {
                    var centro = kv.Value + Vector3.up * 0.05f;
                    // posizione di ripresa: prima una cella percorribile a 1,5-2,5 m con linea libera, quota occhio 1,6; altrimenti la piu' vicina
                    float best = 1e9f; Vector3 occhio = Vector3.zero; bool ok2 = false;
                    foreach (var fascia in new[] { new Vector2(1.4f, 3.2f), new Vector2(0.7f, 4.5f), new Vector2(0.3f, 7f) })
                    {
                        foreach (var c in g.celle)
                        {
                            float dd = Vector3.Distance(new Vector3(c.x, 0, c.z), new Vector3(centro.x, 0, centro.z));
                            if (dd < fascia.x || dd > fascia.y || Mathf.Abs(c.y - kv.Value.y) > 2.6f) continue;
                            var o = c + Vector3.up * 1.6f;
                            if (!Libero(o, centro, null)) continue;
                            float sc2 = Mathf.Abs(dd - 2.0f);
                            if (sc2 < best) { best = sc2; occhio = o; ok2 = true; }
                        }
                        if (ok2) break;
                    }
                    if (!ok2) { Log("  " + kv.Key + ": nessun punto di ripresa libero"); continue; }
                    yield return Scatta("pos_" + kv.Key, occhio, centro, 50f);
                    Log("  foto " + kv.Key + " da " + occhio.ToString("F2") + " verso " + centro.ToString("F2"));
                }
            }
        }
    }
}
#endif
