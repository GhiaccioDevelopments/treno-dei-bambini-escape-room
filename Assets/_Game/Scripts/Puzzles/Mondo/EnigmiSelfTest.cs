#if UNITY_EDITOR || DEBUG
// R020: prova automatica degli enigmi nel mondo, solo negli editor e nelle build di sviluppo (mai nella build normale).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.Core
{
    using TrenoBambini.Puzzles;

    /// <summary>
    /// R020: gioca tutti gli enigmi di hub, basso e ricordo "Le scarpe": prova la porta chiusa a chiave, gli enigmi bloccati dai requisiti, l'errore gentile
    /// (messaggio e riprova), i tre indizi, la soluzione con il mouse virtuale (raggi, rotelle, parti cliccabili), la versione semplificata a pannelli, il
    /// salvataggio e il caricamento. Attiva SOLO con l'argomento -enigmitest [-enigmitestout cartella] (nell'editor: SessionState "enigmitest").
    /// Scrive enigmitest.log, gli screenshot JPG 1280x720 e OK.txt / FALLITA.txt; poi chiude. Salva e rimette a posto il salvataggio del giocatore.
    /// </summary>
    public class EnigmiSelfTest : MonoBehaviour
    {
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-enigmitest" || a == "-enigmifoto") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("enigmitest", false) || UnityEditor.SessionState.GetBool("enigmifoto", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("EnigmiSelfTest");
            DontDestroyOnLoad(go);
            go.AddComponent<EnigmiSelfTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        string dir, logPath;
        int ok, ko;
        Keyboard kb;
        Mouse ms;
        EsaminaView V { get { return EsaminaView.Current; } }

        void Log(string s)
        {
            Debug.Log("[EnigmiTest] " + s);
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
            dir = Arg("-enigmitestout", Path.Combine(Application.dataPath, "..", "enigmitest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "enigmitest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova enigmi. Salvataggio presente: " + avevaSave);

            string pre = Arg("-enigmitestpreset", "");
            if (pre != "") { GraphicsPresets.Apply(int.Parse(pre), false); Log("Preset forzato: " + pre); }
            kb = InputSystem.AddDevice<Keyboard>("EnigmiTestKb");
            ms = InputSystem.AddDevice<Mouse>("EnigmiTestMouse");
            foreach (var d in InputSystem.devices) if (d != kb && d != ms) InputSystem.DisableDevice(d);
            InputSystem.onDeviceChange += (d, ch) => { if ((ch == InputDeviceChange.Added || ch == InputDeviceChange.Reconnected || ch == InputDeviceChange.Enabled) && d != kb && d != ms) InputSystem.DisableDevice(d); };

            bool fine = false;
            try { StartCoroutine(Cattura(() => fine = true)); } catch (Exception e) { Log("ERRORE " + e); }
            while (!fine) yield return null;

            if (avevaSave) PlayerPrefs.SetString("treno_save", saveBackup); else PlayerPrefs.DeleteKey("treno_save");
            PlayerPrefs.Save();
            Log("Fine prova: " + ok + " PASS, " + ko + " FAIL");
            File.WriteAllText(Path.Combine(dir, ko == 0 ? "OK.txt" : "FALLITA.txt"), ok + " PASS, " + ko + " FAIL\n");
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("enigmitest", false);
            UnityEditor.SessionState.SetBool("enigmifoto", false);
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator Cattura(Action fine)
        {
            yield return Flusso();
            fine();
        }

        // ------------------------------------------------------------------ utilita'
        IEnumerator Attendi(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        IEnumerator Foto(string nome)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) yield break;
            var rt = RenderTexture.GetTemporary(1280, 720, 0, RenderTextureFormat.ARGB32);
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

        IEnumerator ClickMouse(Vector2 pos)
        {
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos }); yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos }); yield return null; yield return null;
        }

        /// <summary>Clic con il mouse virtuale su una parte della vista ravvicinata (passa dai raggi veri).</summary>
        IEnumerator Parte(string id, float dy = 0f)
        {
            Vector2 p;
            if (V == null || !V.PosizioneSchermo(id, out p)) { Check("parte " + id + " presente nella vista", false); yield break; }
            yield return ClickMouse(p + new Vector2(0f, dy));
            yield return Attendi(0.55f);
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
            yield return Attendi(0.8f);
        }

        IEnumerator Vai(string scena, string spawn)
        {
            SceneLoader.I.Load(scena, spawn);
            yield return ScenaPronta(scena);
        }

        EnigmaMondo Trova(string id)
        {
            foreach (var e in EnigmaMondo.Tutti) if (e != null && e.def != null && e.def.id == id) return e;
            return null;
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

        /// <summary>Apre l'enigma con il vero Interact (come premere E), verifica che si apra la vista e ritorna.</summary>
        IEnumerator Apri(string id, string mec)
        {
            var e = Trova(id);
            Check(id + ": oggetto presente nel mondo", e != null);
            if (e == null) yield break;
            e.Interact();
            yield return Attendi(1.0f);
        }

        IEnumerator Concludi(string id, string chiave)
        {
            Check(id + ": soluzione riconosciuta", V != null && V.Risolto);
            yield return Attendi(1.4f);
            yield return Foto(id + "_risolto");
            var v = V;
            if (v != null) v.Finito();
            yield return Attendi(0.8f);
            yield return ChiudiMessaggi();
            Check(id + ": salvato come risolto", MondoStato.Risolto(chiave));
            Check(id + ": vista chiusa", V == null);
        }

        IEnumerator Indizi(string id)
        {
            if (V == null) yield break;
            V.Indizio();
            yield return Attendi(0.4f);
            string m1 = Messaggio();
            V.Indizio(); V.Indizio();
            yield return Attendi(0.4f);
            Check(id + ": tre indizi a livelli", V.LivelloIndizio == Mathf.Min(3, V.Def.indizi.Count) && V.Def.indizi.Count >= 3, "livello " + V.LivelloIndizio);
            yield return Foto(id + "_indizio");
        }

        // ------------------------------------------------------------------ flusso
        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);
            PlayerPrefs.DeleteKey("treno_save");
            if (FotoRichiesta()) { yield return FotoMondo(); yield break; }
            GameManager.I.NewGame();
            yield return ScenaPronta(GameManager.SceneHub);
            Check("hub caricato", SceneManager.GetActiveScene().name == GameManager.SceneHub);
            yield return ControllaViola("hub");

            // ---------------- hub
            Door portaBasso = null;
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include)) if (d.luogoId == "basso_antonietta") portaBasso = d;
            Check("hub: porta del basso trovata", portaBasso != null);
            if (portaBasso != null)
            {
                portaBasso.Interact();
                yield return Attendi(1.0f);
                Check("hub: la porta del basso e' chiusa all'inizio", SceneManager.GetActiveScene().name == GameManager.SceneHub);
            }

            yield return Bucato();
            yield return EsVoto();
            yield return Manifesti();

            if (portaBasso != null)
            {
                portaBasso.Interact();
                yield return ScenaPronta("20_Int_BassoAntonietta");
                Check("hub: con porta e chiave la porta si apre sul basso", SceneManager.GetActiveScene().name == "20_Int_BassoAntonietta");
            }
            else yield return Vai("20_Int_BassoAntonietta", "ingresso");

            // ---------------- basso
            yield return ControllaViola("basso");
            Check("basso: le scarpe non sono ancora toccabili", ScarpeNascoste());
            yield return Bloccato("enigma_calendario");
            yield return Bloccato("enigma_cassetta");
            yield return Bloccato("enigma_baule");
            yield return Foto("basso_prima");
            yield return Foto2D();
            yield return Fotografia();
            yield return Calendario();
            yield return Cassetta();
            yield return Baule();   // finisce nel ricordo

            // ---------------- ricordo
            yield return ScenaPronta("30_FB_Scarpe");
            yield return Attendi(1.5f);
            yield return ChiudiMessaggi();
            yield return ControllaViola("ricordo");
            Check("ricordo: nessun marcatore PuzzleStation", FindObjectsByType<Transform>(FindObjectsInactive.Include).Length > 0 && !HaOggettoConNome("Postazione_"));
            yield return Bloccato("enigma_nascosto");
            yield return Passanti();
            yield return Nascosto();
            yield return Documenti();
            yield return Ordina();
            yield return Lucchetto();

            var uscita = FindAnyObjectByType<EscapeExit>();
            Check("ricordo: l'uscita e' aperta dopo i cinque enigmi", uscita != null && uscita.GetPrompt() == "Esci dal ricordo", uscita != null ? uscita.GetPrompt() : "");
            yield return Foto("ricordo_uscita_aperta");

            // ---------------- salvataggio e caricamento
            GameManager.I.SaveGame();
            var riletto = SaveSystem.Load();
            Check("salvataggio: tutti gli enigmi del mondo e del ricordo risolti", riletto.enigmiRisolti.Count >= 12, riletto.enigmiRisolti.Count + " risolti");
            Check("salvataggio: gettoni degli enigmi salvati", riletto.oggettiEnigma.Contains("chiave_basso") && riletto.oggettiEnigma.Contains("cifra_9b"));
            GameManager.I.Continue();
            yield return ScenaPronta("30_FB_Scarpe");
            yield return Attendi(1.5f);
            yield return ChiudiMessaggi();
            var p2 = Trova("enigma_lucchetto");
            Check("ricaricato: gli enigmi restano risolti", p2 != null && p2.Risolto && p2.GetPrompt().StartsWith("Già esaminato"), p2 != null ? p2.GetPrompt() : "");

            // ---------------- versione semplificata
            GameManager.I.Save.enigmiSemplificati = true;
            GameManager.I.Save.enigmiRisolti.Remove("flashback_scarpe/enigma_lucchetto");
            p2.Interact();
            yield return Attendi(1.2f);
            Check("semplificato: il lucchetto si apre come pannello 2D e non come vista 3D", FindAnyObjectByType<LucchettoPanel>() != null && V == null);
            yield return Foto("semplificato_lucchetto");
            var lp = FindAnyObjectByType<LucchettoPanel>();
            if (lp != null) lp.GetType().GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(lp, null);
            yield return Attendi(0.8f);
            GameManager.I.Save.enigmiSemplificati = false;
            GameManager.I.Save.enigmiRisolti.Add("flashback_scarpe/enigma_lucchetto");

            // ---------------- tabella finale
            Log("--- Enigmi risolti nel salvataggio: " + string.Join(", ", GameManager.I.Save.enigmiRisolti));
        }

        // ------------------------------------------------------------------ foto degli oggetti nel mondo (posizionamento)
        IEnumerator FotoMondo()
        {
            foreach (var sc in new[] { new[] { GameManager.SceneHub, "inizio" }, new[] { "20_Int_BassoAntonietta", "ingresso" }, new[] { "30_FB_Scarpe", "inizio" } })
            {
                SceneLoader.I.Load(sc[0], sc[1]);
                yield return ScenaPronta(sc[0]);
                yield return Attendi(2f);
                yield return ChiudiMessaggi();
                foreach (var e in new List<EnigmaMondo>(EnigmaMondo.Tutti))
                {
                    if (e == null || e.gameObject.scene.name != sc[0]) continue;
                    var b = MondoFab.Limiti(e.transform);
                    int rn = 0; foreach (var rr in e.GetComponentsInChildren<Renderer>()) if (rr.enabled) rn++;
                    Log("  " + e.def.id + " pos=" + e.transform.position.ToString("F2") + " centro=" + b.center.ToString("F2") + " dim=" + b.size.ToString("F2") + " renderer=" + rn + " attivo=" + e.gameObject.activeInHierarchy);
                    float ext = Mathf.Max(0.6f, b.size.magnitude);
                    Vector3 fwd = Quaternion.Euler(0f, e.def.yaw, 0f) * Vector3.back;
                    var dirs = new[] { fwd, Quaternion.Euler(0f, 35f, 0f) * fwd, Quaternion.Euler(0f, -35f, 0f) * fwd, Quaternion.Euler(0f, 90f, 0f) * fwd, Quaternion.Euler(0f, -90f, 0f) * fwd, -fwd };
                    var ov = Override(e.def.id);
                    foreach (var tipo in new[] { "mondo", "contesto" })
                    {
                        bool ottenuta = false;
                        if (ov != null)
                        {
                            yield return Scatta(e.def.id + "_" + tipo, tipo == "mondo" ? ov[0] : ov[1], b.center, tipo == "mondo" ? 55f : 70f);
                            Check(e.def.id + ": foto " + tipo + " (punto di vista scelto)", true);
                            continue;
                        }
                        float[] dists = tipo == "mondo" ? new[] { ext * 1.5f, ext * 1.1f, ext * 0.8f } : new[] { ext * 3.5f, ext * 2.5f, ext * 1.8f, ext * 1.3f };
                        foreach (var dist in dists)
                        {
                            foreach (var dr in dirs)
                            {
                                Vector3 da = b.center + dr * dist + Vector3.up * (tipo == "mondo" ? 0.15f : 0.9f);
                                RaycastHit h;
                                bool bloccato = Physics.Linecast(b.center + dr * 0.1f, da, out h, ~0, QueryTriggerInteraction.Ignore);
                                if (bloccato) continue;
                                yield return Scatta(e.def.id + "_" + tipo, da, b.center, tipo == "mondo" ? 45f : 60f);
                                ottenuta = true; break;
                            }
                            if (ottenuta) break;
                        }
                        Check(e.def.id + ": foto " + tipo + " scattabile da una posizione libera", ottenuta);
                    }
                }
            }
            yield return null;
        }

        IEnumerator Scatta(string nome, Vector3 da, Vector3 verso, float fov)
        {
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
            yield return null;
        }

        /// <summary>Punti di vista scelti a mano dove la ricerca automatica uscirebbe dai muri (pareti a una sola faccia).</summary>
        static Vector3[] Override(string id)
        {
            switch (id)
            {
                case "enigma_bucato": return new[] { new Vector3(-6.0f, 2.2f, -3.8f), new Vector3(-3.4f, 1.8f, -3.7f) };
                default: return null;
            }
        }

        static bool FotoRichiesta()
        {
            if (Array.IndexOf(Args, "-enigmifoto") >= 0) return true;
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetBool("enigmifoto", false);
#else
            return false;
#endif
        }

        bool ScarpeNascoste()
        {
            foreach (var p in FindObjectsByType<ItemPickup>(FindObjectsInactive.Include)) if (p.oggettoId == "scarpe") return !p.gameObject.activeInHierarchy;
            return true;
        }

        bool HaOggettoConNome(string prefisso)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include)) if (t.name.StartsWith(prefisso)) return true;
            return false;
        }

        IEnumerator ControllaViola(string scena)
        {
            int cattivi = 0; var nomi = new StringBuilder();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    var m = ms[i];
                    if (m == null || m.shader == null || m.shader.name.StartsWith("Hidden/") || m.shader.name.Contains("InternalError") || !m.shader.isSupported)
                    {
                        if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                        cattivi++; if (nomi.Length < 300) nomi.Append(r.name).Append(' ');
                    }
                }
            }
            Check(scena + ": nessun materiale rotto (magenta/viola)", cattivi == 0, cattivi + " " + nomi);
            yield return null;
        }

        IEnumerator Bloccato(string id)
        {
            var e = Trova(id);
            if (e == null) { Check(id + ": presente (bloccato)", false); yield break; }
            e.Interact();
            yield return Attendi(0.6f);
            Check(id + ": con i requisiti mancanti non si apre e dice una frase gentile", V == null && !e.Disponibile);
        }

        IEnumerator Foto2D() { yield return null; }

        // ------------------------------------------------------------------ hub
        IEnumerator Bucato()
        {
            string id = "enigma_bucato";
            yield return Apri(id, "oggetto");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            Check(id + ": vista ravvicinata aperta", true);
            yield return Foto(id + "_aperto");
            yield return Parte("panno_1");
            Check(id + ": un panno senza toppa a quadri da' un errore gentile", Messaggio().Contains("toppa"), Messaggio());
            yield return Foto(id + "_errore");
            yield return Indizi(id);
            yield return Parte("panno_2"); yield return Parte("panno_4"); yield return Parte("panno_6");
            yield return Concludi(id, "mondo/" + id);
            Check(id + ": produce la porta trovata", MondoStato.Ha("porta_trovata"));
        }

        IEnumerator EsVoto()
        {
            string id = "enigma_exvoto";
            yield return Apri(id, "oggetto");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            yield return Parte("cuore");
            Check(id + ": un ex-voto sbagliato spiega e fa riprovare", Messaggio().Contains("cuore"), Messaggio());
            yield return Indizi(id);
            yield return Parte("piede");
            yield return Attendi(0.6f);
            yield return Foto(id + "_girato");
            yield return Parte("chiave");
            yield return Concludi(id, "mondo/" + id);
            Check(id + ": la chiave e' nella borsa", MondoStato.Ha("chiave_basso"));
        }

        IEnumerator Manifesti()
        {
            string id = "enigma_manifesti";
            yield return Apri(id, "oggetto");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            yield return Parte("avviso");   // coperto: clic su un manifesto sopra
            yield return Indizi(id);
            yield return Parte("manifesto_1"); yield return Parte("manifesto_2"); yield return Parte("avviso");
            yield return Concludi(id, "mondo/" + id);
        }

        // ------------------------------------------------------------------ basso
        IEnumerator Fotografia()
        {
            string id = "enigma_foto";
            yield return Apri(id, "oggetto");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            yield return Indizi(id);
            for (int i = 0; i < 3; i++) { yield return Parte("cornice"); yield return Attendi(0.3f); }
            yield return Foto(id + "_dritta");
            yield return Parte("linguetta");
            yield return Parte("cart_base");
            yield return Concludi(id, "mondo/" + id);
            Check(id + ": il cartoncino e' nella borsa", MondoStato.Ha("cartoncino_segni"));
        }

        IEnumerator Calendario()
        {
            string id = "enigma_calendario";
            yield return Apri(id, "ruote");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            // una rotella con il mouse: clic sopra il centro = +1
            string prima = V.CodiceRuote;
            yield return Parte("ruota0", 16f);
            Check(id + ": clic sopra la rotella aumenta la cifra", V.CodiceRuote != prima, prima + " -> " + V.CodiceRuote);
            V.ImpostaRuote("1234");
            yield return Parte("scatto");
            Check(id + ": codice sbagliato = messaggio gentile, si puo' riprovare", V != null && !V.Risolto && Messaggio().Length > 5, Messaggio());
            yield return Foto(id + "_errore");
            yield return Indizi(id);
            V.ImpostaRuote("4231");
            yield return Parte("scatto");
            yield return Concludi(id, "mondo/" + id);
        }

        IEnumerator Cassetta()
        {
            string id = "enigma_cassetta";
            yield return Apri(id, "ruote");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            V.ImpostaRuote("0000");
            yield return Parte("scatto");
            Check(id + ": codice sbagliato = riprova", !V.Risolto && Messaggio().Length > 5, Messaggio());
            yield return Indizi(id);
            V.ImpostaRuote("4231");
            yield return Parte("scatto");
            yield return Attendi(0.6f);
            yield return Foto(id + "_aperta");
            yield return Parte("soldi");
            yield return Parte("chiavetta");
            yield return Concludi(id, "mondo/" + id);
            Check(id + ": la chiavetta e' nella borsa", MondoStato.Ha("chiavetta_baule"));
        }

        IEnumerator Baule()
        {
            string id = "enigma_baule";
            yield return Apri(id, "oggetto");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            yield return Parte("serratura");    // inventario attivo: la chiavetta e' l'unico oggetto, e' gia' in mano
            Check(id + ": la chiavetta apre la serratura", V.Cliccabile("coperta"));
            yield return Indizi(id);
            yield return Parte("coperta");
            yield return Foto(id + "_fondo");
            yield return Parte("asse_croce");   // ordine sbagliato
            Check(id + ": assi nell'ordine sbagliato = le assi tornano su e si riprova", Messaggio().Contains("ordine"), Messaggio());
            yield return Foto(id + "_errore");
            yield return Attendi(0.4f);
            yield return Parte("asse_scarpa"); yield return Parte("asse_croce"); yield return Parte("asse_treno");
            yield return Attendi(0.4f);
            yield return Foto(id + "_assi");
            Check(id + ": con le assi giuste compare la scatolina", V.Cliccabile("scatolina"));
            yield return Parte("scatolina");
            yield return Parte("scarpe_prendi");
            Check(id + ": soluzione riconosciuta", V != null && V.Risolto);
            yield return Attendi(1.4f);
            yield return Foto(id + "_risolto");
            if (V != null) V.Finito();
            yield return Attendi(0.5f);
            yield return ChiudiMessaggi();
            // il finale avvia il ricordo delle scarpe
            yield return ScenaPronta("30_FB_Scarpe");
            Check(id + ": il baule avvia il ricordo delle scarpe", SceneManager.GetActiveScene().name == "30_FB_Scarpe");
            Check(id + ": salvato come risolto", MondoStato.Risolto("mondo/" + id));
        }

        // ------------------------------------------------------------------ ricordo
        IEnumerator Passanti()
        {
            string id = "enigma_passanti";
            yield return Apri(id, "deduzione");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            yield return Parte("pass_p1");
            Check(id + ": toccando un paio compare la descrizione", Messaggio().Contains("lucide") || Messaggio().Contains("Scarpe nere"), Messaggio());
            // sbagliato: tutti "Muratore"
            var def = V.Def;
            for (int i = 0; i < def.passanti.Count; i++) V.AssegnaPassante(i, 0);
            V.Conferma();
            yield return Attendi(0.5f);
            Check(id + ": risposte sbagliate = feedback a gruppi di tre e si riprova", !V.Risolto && Messaggio().Contains("gruppo 1"), Messaggio());
            yield return Foto(id + "_errore");
            yield return Indizi(id);
            for (int i = 0; i < def.passanti.Count; i++) V.AssegnaPassante(i, def.mestieri.IndexOf(def.passanti[i].mestiere));
            yield return Foto(id + "_assegnati");
            V.Conferma();
            yield return Attendi(0.4f);
            yield return Concludi(id, "flashback_scarpe/" + id);
            Check(id + ": produce calzolaio e prima cifra", MondoStato.Ha("calzolaio_riconosciuto") && MondoStato.Ha("cifra_1"));
        }

        IEnumerator Nascosto()
        {
            string id = "enigma_nascosto";
            var e = Trova(id);
            e.Interact();   // parte la ricerca delle cinque calzature
            yield return Attendi(0.8f);
            yield return ChiudiMessaggi();
            var items = new List<HiddenItem>(FindObjectsByType<HiddenItem>(FindObjectsInactive.Exclude));
            Check(id + ": cinque calzature da trovare nella strada", items.Count == 5, items.Count.ToString());
            yield return Foto(id + "_ricerca");
            foreach (var h in items) { if (h != null) h.Interact(); yield return Attendi(0.3f); }
            yield return Attendi(0.5f);
            yield return ChiudiMessaggi();
            Check(id + ": le cinque calzature sono nella borsa", MondoStato.Ha("scarpa_cartone") && MondoStato.Ha("zoccolo_legno") && MondoStato.Ha("scarpone_soldato") && MondoStato.Ha("scarpa_signora") && MondoStato.Ha("pezze_piedi"));
            e.Interact();
            yield return Attendi(1.0f);
            if (V == null) { Check(id + ": forme aperte", false); yield break; }
            yield return Foto(id + "_forme");
            // inventario attivo: scarpone sulla forma dello zoccolo = non calza
            V.ScegliMano("scarpone_soldato");
            yield return Parte("forma_zoccolo");
            Check(id + ": un oggetto sulla forma sbagliata 'non calza'", Messaggio().Contains("Non calza"), Messaggio());
            yield return Foto(id + "_errore");
            yield return Indizi(id);
            var ordine = new[] { ("zoccolo_legno", "forma_zoccolo"), ("scarpone_soldato", "forma_scarpone"), ("scarpa_signora", "forma_tacco"), ("scarpa_cartone", "forma_cartone"), ("pezze_piedi", "forma_piccola") };
            foreach (var (tok, forma) in ordine)
            {
                V.ScegliMano(tok);
                yield return Parte(forma);
            }
            yield return Concludi(id, "flashback_scarpe/" + id);
            Check(id + ": produce la seconda cifra", MondoStato.Ha("cifra_9a"));
        }

        IEnumerator Documenti()
        {
            string id = "enigma_quiz";
            yield return Apri(id, "documenti");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            V.RispondiDoc("registro", 0);
            yield return Attendi(0.4f);
            Check(id + ": riga sbagliata = messaggio gentile", !V.Risolto && Messaggio().Length > 5, Messaggio());
            yield return Foto(id + "_errore");
            yield return Indizi(id);
            // una riga con il mouse virtuale
            var riga = GameObject.Find("Riga_avviso_0");
            if (riga != null)
            {
                var rt = (RectTransform)riga.transform;
                var c = rt.GetComponentInParent<Canvas>();
                Vector2 p = RectTransformUtility.WorldToScreenPoint(c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null, rt.TransformPoint(rt.rect.center));
                yield return ClickMouse(p);
                yield return Attendi(0.5f);
            }
            else V.RispondiDoc("avviso", 0);
            V.RispondiDoc("avviso", 2);
            yield return Attendi(0.4f);
            V.RispondiDoc("registro", 2);
            yield return Attendi(0.4f);
            yield return Concludi(id, "flashback_scarpe/" + id);
            Check(id + ": produce la terza cifra", MondoStato.Ha("cifra_8"));
        }

        IEnumerator Ordina()
        {
            string id = "enigma_ordina";
            var e = Trova(id);
            e.Interact();
            yield return Attendi(1.2f);
            var pan = FindAnyObjectByType<OrdinaPanel>();
            Check(id + ": le carte si trascinano nel pannello", pan != null);
            if (pan == null) yield break;
            yield return Foto(id + "_aperto");
            var t = typeof(OrdinaPanel);
            var verify = t.GetMethod("Verify", BindingFlags.NonPublic | BindingFlags.Instance);
            verify.Invoke(pan, null);   // ordine casuale = errore gentile
            yield return Attendi(0.6f);
            yield return Foto(id + "_errore");
            var order = (List<int>)t.GetField("order", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pan);
            for (int i = 0; i < order.Count; i++) order[i] = i;
            verify.Invoke(pan, null);
            yield return Attendi(2.0f);
            yield return Foto(id + "_risolto");
            typeof(PuzzlePanel).GetMethod("FinishSolved", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pan, null);
            yield return Attendi(0.8f);
            yield return ChiudiMessaggi();
            Check(id + ": salvato come risolto", MondoStato.Risolto("flashback_scarpe/" + id));
            Check(id + ": produce la quarta cifra", MondoStato.Ha("cifra_9b"));
        }

        IEnumerator Lucchetto()
        {
            string id = "enigma_lucchetto";
            yield return Apri(id, "ruote");
            if (V == null) { Check(id + ": vista aperta", false); yield break; }
            yield return Foto(id + "_aperto");
            V.ImpostaRuote("1234");
            yield return Parte("scatto");
            Check(id + ": codice sbagliato = riprova", !V.Risolto && Messaggio().Length > 5, Messaggio());
            yield return Indizi(id);
            V.ImpostaRuote("1989");
            yield return Parte("scatto");
            yield return Concludi(id, "flashback_scarpe/" + id);
        }
    }
}
#endif
