#if UNITY_EDITOR || DEBUG
// R025: prova automatica del filmato iniziale, solo negli editor e nelle build di sviluppo (mai nella build normale).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Mappa;
using TrenoBambini.Player;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Dialogue
{
    /// <summary>
    /// R025: prova del filmato iniziale. Attiva SOLO con -introtest [-introtestout cartella] [-introtestnaturale] [-introtestfoto]:
    /// 1) Nuova partita: il filmato parte da solo, attraversa i 7 capitoli e tutte le 41 battute, mostra le targhette dei controlli e arriva alla fine
    ///    (di default con un tocco di E a ogni battuta; con -introtestnaturale lo lascia andare da solo, a tempo di lettura: serve a misurare gli FPS);
    /// 2) alla fine il giocatore e' nell'hub, libero, col primo obiettivo (ob_bucato) e la X della minimappa forzata;
    /// 3) Nuova partita e "tieni premuto" Esc: il filmato si salta e lascia lo stesso stato;
    /// 4) Continua con un salvataggio esistente: il filmato non riparte; 5) "Rivedi il filmato iniziale" dalla Pausa: parte, si salta e il giocatore torna dov'era;
    /// 6) i tasti cambiano con il dispositivo (tastiera, gamepad, touch).
    /// Scrive introtest.log, OK.txt / FALLITA.txt, gli screenshot JPG 1280x720 (con -introtestfoto: una per battuta) e fps.csv; poi chiude. Rimette a posto il salvataggio.
    /// </summary>
    public class FilmatoSelfTest : MonoBehaviour
    {
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-introtest") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("introtest", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("FilmatoSelfTest");
            DontDestroyOnLoad(go);
            go.AddComponent<FilmatoSelfTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        static bool Flag(string nome) { foreach (var a in Args) if (a == nome) return true; return false; }

        const string Hub = "10_Hub_Vicolo";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        string dir, logPath;
        int ok, ko;
        Keyboard kb;
        Mouse ms;
        bool naturale, foto;

        void Log(string s)
        {
            Debug.Log("[IntroTest] " + s);
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
            naturale = Flag("-introtestnaturale");
            foto = Flag("-introtestfoto");
            dir = Arg("-introtestout", Path.Combine(Application.dataPath, "..", "introtest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "introtest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova filmato. Modo: " + (naturale ? "naturale (a tempo di lettura)" : "con tocchi") + ". Salvataggio presente: " + avevaSave);

            kb = InputSystem.AddDevice<Keyboard>("IntroTestKb");
            ms = InputSystem.AddDevice<Mouse>("IntroTestMouse");
            foreach (var d in InputSystem.devices) if (d != kb && d != ms) InputSystem.DisableDevice(d);
            InputSystem.onDeviceChange += (d, ch) => { if ((ch == InputDeviceChange.Added || ch == InputDeviceChange.Reconnected || ch == InputDeviceChange.Enabled) && d != kb && d != ms && !d.name.StartsWith("IntroTest")) InputSystem.DisableDevice(d); };

            bool fine = false;
            StartCoroutine(Giro(() => fine = true));
            float guardia = 0f;
            float massimo = naturale ? 900f : 480f;
            while (!fine && guardia < massimo) { guardia += Time.unscaledDeltaTime; yield return null; }
            if (!fine) Check("la prova e' finita nei tempi", false);

            if (avevaSave) PlayerPrefs.SetString("treno_save", saveBackup); else PlayerPrefs.DeleteKey("treno_save");
            PlayerPrefs.Save();
            Log("Fine prova: " + ok + " PASS, " + ko + " FAIL");
            File.WriteAllText(Path.Combine(dir, ko == 0 ? "OK.txt" : "FALLITA.txt"), ok + " PASS, " + ko + " FAIL\n");
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("introtest", false);
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ utilita'
        IEnumerator Attendi(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        IEnumerator Tap(Key k)
        {
            InputSystem.QueueStateEvent(kb, new KeyboardState(k));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null; yield return null;
        }

        IEnumerator Tieni(Key k, float secondi)
        {
            InputSystem.QueueStateEvent(kb, new KeyboardState(k));
            float t = 0f;
            while (t < secondi) { t += Time.unscaledDeltaTime; yield return null; }
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null; yield return null;
        }

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
            File.WriteAllBytes(Path.Combine(dir, nome + ".jpg"), out2.EncodeToJPG(80));
            Destroy(tex); Destroy(out2);
            yield return null;
        }

        FilmatoIniziale Film { get { return UIRoot.I != null ? UIRoot.I.Filmato : null; } }

        IEnumerator AttendiFilm(bool inCorso, float max)
        {
            float t = 0f;
            while (t < max && FilmatoIniziale.InCorso != inCorso) { t += Time.unscaledDeltaTime; yield return null; }
        }

        static ThirdPersonController Giocatore() { return FindAnyObjectByType<ThirdPersonController>(); }

        // ------------------------------------------------------------------ prova
        IEnumerator Giro(Action fine)
        {
            // attesa del titolo
            float t0 = 0f;
            while (t0 < 20f && !(SceneManager.GetActiveScene().name == GameManager.SceneTitolo && UIRoot.I != null)) { t0 += Time.unscaledDeltaTime; yield return null; }
            yield return Attendi(1.5f);
            var diag = FindAnyObjectByType<DiagOverlay>(); if (diag != null) diag.enabled = false; // niente contatore FPS negli screenshot
            GraphicsPresets.BenchmarkSenzaLimiti(true);
            GraphicsPresets.Apply(2, false); // Alta
            Check("il filmato e' disponibile (copione e regia caricati)", GameManager.FilmatoDisponibile());
            var def = GameData.Cutscene("intro");
            Check("cutscene_intro ha 41 battute cs_intro_001..041 e 7 capitoli", def != null && def.battute.Count == 41 && def.capitoli.Count == 7 && GameData.Filmato.capitoli.Count == 7,
                def != null ? def.battute.Count + " battute, " + def.capitoli.Count + " capitoli" : "null");
            Check("amerigo_adulto in personaggi.json", GameData.Personaggio("amerigo_adulto") != null && GameData.Personaggio("amerigo_adulto").colore == "#5A4A3C");

            // ---- 1) nuova partita senza salvataggio: il filmato parte da solo e arriva alla fine
            PlayerPrefs.DeleteKey("treno_save"); PlayerPrefs.Save();
            GameManager.I.NewGame();
            yield return AttendiFilm(true, 15f);
            Check("alla Nuova partita il filmato parte da solo", FilmatoIniziale.InCorso);
            var viste = new HashSet<string>();
            var controlli = new HashSet<string>();
            var capitoli = new HashSet<int>();
            var scene = new HashSet<string>();
            bool targaSalta = false, sottotitoliOk = true, hudNascosto = true, pausaChiusa = true, tastiera = true, inserto = false;
            var frame = new List<float>(); var frameCap = new Dictionary<int, List<float>>();
            int scatti = 0; string ultima = ""; float tBatt = 0f; bool fotoFatta = false; float ultimaFoto = -10f;
            float inizio = Time.unscaledTime;
            while (FilmatoIniziale.InCorso && Time.unscaledTime - inizio < (naturale ? 800f : 400f))
            {
                var f = Film;
                capitoli.Add(f.CapitoloIndice);
                if (!string.IsNullOrEmpty(f.ScenaCapitolo)) scene.Add(f.ScenaCapitolo);
                if (!string.IsNullOrEmpty(f.BattutaId) && f.BattutaId != ultima)
                {
                    ultima = f.BattutaId; viste.Add(ultima); tBatt = Time.unscaledTime; fotoFatta = false;
                }
                if (!string.IsNullOrEmpty(f.ControlloMostrato)) { controlli.Add(f.ControlloMostrato); if (f.DispositivoControlli != "tastiera") tastiera = false; }
                if (f.TargaSaltaVisibile) targaSalta = true;
                if (f.InsertoAttivo) inserto = true;
                if (f.BattutaInCorso && Time.unscaledTime - f.InizioBattuta > 0.8f && !f.SottotitoloVisibile && !SceneLoader.I.IsLoading) sottotitoliOk = false;
                if (UIRoot.I.Pause.IsOpen) pausaChiusa = false;
                if (Time.frameCount % 20 == 0)
                {
                    UIRoot.I.TogglePause(); // la pausa non deve aprirsi durante il filmato
                    if (UIRoot.I.Pause.IsOpen) { pausaChiusa = false; UIRoot.I.Pause.Hide(); }
                }
                var hud = UIRoot.I.HudLayer;
                if (hud != null && hud.gameObject.activeSelf) hudNascosto = false;
                if (!SceneLoader.I.IsLoading && Time.unscaledTime - ultimaFoto > 0.6f)
                {
                    float dt = Time.unscaledDeltaTime; frame.Add(dt);
                    if (dt > 0.25f && scatti++ < 8) Log("scatto di " + (dt * 1000f).ToString("0") + " ms: capitolo " + (f.CapitoloIndice + 1) + ", battuta " + f.BattutaId + ", consegna " + f.ConsegnaFatta);
                    List<float> l; if (!frameCap.TryGetValue(f.CapitoloIndice, out l)) frameCap[f.CapitoloIndice] = l = new List<float>();
                    l.Add(dt);
                }
                // una foto per battuta, a ~1,8 s dall'inizio (testo scritto e camera in movimento)
                if (foto && !fotoFatta && ultima != "" && Time.unscaledTime - tBatt > 1.8f && f.SottotitoloVisibile)
                {
                    fotoFatta = true; ultimaFoto = Time.unscaledTime;
                    yield return Foto("film_" + ultima);
                }
                // modo con tocchi: avanza dopo 1,3 s (e dopo la foto, se serve)
                if (!naturale && ultima != "" && Time.unscaledTime - tBatt > 1.3f && f.SottotitoloVisibile && (!foto || fotoFatta))
                {
                    yield return Tap(Key.E);
                    tBatt = Time.unscaledTime + 5f; // evita tocchi doppi sulla stessa battuta
                }
                yield return null;
            }
            yield return Attendi(1.0f);
            Check("il filmato arriva alla fine (non saltato)", Film.Concluso && !Film.Saltato, "tempo " + (Time.unscaledTime - inizio).ToString("0") + " s");
            Check("tutte le 41 battute sono andate in onda", viste.Count == 41, viste.Count + "/41");
            Check("tutti i 7 capitoli sono andati in onda", capitoli.Contains(6) && Film.CapitoliVisti == 7, "visti " + Film.CapitoliVisti);
            Check("il filmato usa sia il presente (hub) sia il ricordo (30_FB_Scarpe)", scene.Contains(Hub) && scene.Contains("30_FB_Scarpe"));
            foreach (var c in new[] { "salta", "muovi", "guarda", "interagisci", "inventario", "mappa", "indizio" })
                Check("targhetta controlli '" + c + "' mostrata", controlli.Contains(c));
            Check("targhetta 'tieni premuto per saltare' visibile all'inizio", targaSalta);
            Check("le targhette sono scritte per tastiera+mouse (nessun gamepad/touch collegato)", tastiera);
            Check("almeno un inserto in studio (pezzi 3D) e' andato in onda", inserto);
            Check("durante il filmato la HUD e' nascosta e la pausa non si apre", hudNascosto && pausaChiusa);
            Check("sottotitoli sempre visibili durante le battute", sottotitoliOk);
            ControllaFinale("filmato completo");
            yield return Foto("finale_hub_obiettivo");
            ReportFps(frame, frameCap);

            // ---- 2) salta tenendo premuto
            PlayerPrefs.DeleteKey("treno_save"); PlayerPrefs.Save();
            GameManager.I.NewGame();
            yield return AttendiFilm(true, 15f);
            float tt = 0f;
            while (tt < 90f && Film.CapitoloIndice < 1) { tt += Time.unscaledDeltaTime; yield return null; }
            Check("nuova partita: il filmato riparte e arriva al secondo capitolo", Film.CapitoloIndice >= 1, "capitolo " + Film.CapitoloIndice);
            yield return Attendi(2.5f);
            yield return Tap(Key.Escape); // un tocco di Esc non salta
            Check("un tocco breve di Esc non salta il filmato", FilmatoIniziale.InCorso);
            yield return Tieni(Key.Escape, 1.6f);
            yield return AttendiFilm(false, 20f);
            Check("tenendo premuto Esc il filmato si salta", Film.Concluso && Film.Saltato && !FilmatoIniziale.InCorso);
            yield return Attendi(1.0f);
            ControllaFinale("filmato saltato");

            // ---- 3) un salvataggio esistente non rifa' partire il filmato
            var gm = GameManager.I;
            gm.SaveGame();
            gm.Continue();
            yield return Attendi(2.0f);
            float tc = 0f;
            while (tc < 20f && (SceneLoader.I.IsLoading || SceneManager.GetActiveScene().name != Hub)) { tc += Time.unscaledDeltaTime; yield return null; }
            yield return Attendi(2.5f);
            Check("Continua con un salvataggio esistente: il filmato non riparte", !FilmatoIniziale.InCorso && SceneManager.GetActiveScene().name == Hub && !GameManager.I.InputBlocked);

            // ---- 4) rivedi dalla pausa
            var p = Giocatore();
            Vector3 posPrima = p != null ? p.transform.position : Vector3.zero;
            float yawPrima = p != null ? p.transform.eulerAngles.y : 0f;
            UIRoot.I.TogglePause();
            yield return Attendi(1.0f);
            Check("la pausa si apre in gioco", UIRoot.I.Pause.IsOpen);
            UnityEngine.UI.Button voce = null;
            foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include))
            {
                var tx = b.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (tx != null && tx.text == GameData.UiMenu.filmato.rivedi) { voce = b; break; }
            }
            Check("la pausa ha la voce 'Rivedi il filmato iniziale'", voce != null);
            if (voce != null) voce.onClick.Invoke();
            yield return AttendiFilm(true, 10f);
            Check("'Rivedi il filmato iniziale' fa partire il filmato", FilmatoIniziale.InCorso && Film.ModoRivedi && !UIRoot.I.Pause.IsOpen);
            yield return Attendi(4f);
            yield return ControllaDispositivi();
            yield return Tieni(Key.Escape, 1.6f);
            yield return AttendiFilm(false, 25f);
            yield return Attendi(1.5f);
            p = Giocatore();
            float dist = p != null ? Vector3.Distance(p.transform.position, posPrima) : 999f;
            Check("dopo il filmato rivisto il giocatore torna dov'era", p != null && dist < 0.5f && SceneManager.GetActiveScene().name == Hub, "distanza " + dist.ToString("0.00") + " m");
            Check("dopo il filmato rivisto il gioco e' libero", !GameManager.I.InputBlocked && p != null && p.enabled);
            Check("rivedere il filmato non cambia il salvataggio (scena e flag)", GameManager.I.Save.scena == Hub && GameManager.I.Save.filmatoVisto);
            yield return Foto("rivisto_dopo");
            fine();
        }

        // stato dopo il filmato (completo o saltato)
        void ControllaFinale(string quando)
        {
            var p = Giocatore();
            var gm = GameManager.I;
            Check(quando + ": scena = hub", SceneManager.GetActiveScene().name == Hub);
            Check(quando + ": controllo al giocatore (input libero, movimento attivo)", !gm.InputBlocked && p != null && p.enabled, "blocchi: " + gm.BlockReasons);
            var cc = p != null ? p.GetComponent<CharacterController>() : null;
            Check(quando + ": il giocatore e' a terra e il suo CharacterController e' acceso", cc != null && cc.enabled);
            var f = GameData.Filmato.finale;
            float d = p != null ? Vector2.Distance(new Vector2(p.transform.position.x, p.transform.position.z), new Vector2(f.pos[0], f.pos[1])) : 999f;
            Check(quando + ": Amerigo e' nello slargo del basso (finale)", d < 1.5f, "distanza " + d.ToString("0.0") + " m");
            var ob = Obiettivi.Corrente();
            Check(quando + ": primo obiettivo = ob_bucato", ob != null && ob.id == "ob_bucato", ob != null ? ob.id : "(nessuno)");
            Check(quando + ": X dell'obiettivo forzata visibile anche in aiuto Normale", Obiettivi.Modo == Obiettivi.Normale && Obiettivi.Forzato && Obiettivi.Mostrato);
            var mm = MinimappaHud.I;
            Check(quando + ": minimappa con la X di ob_bucato e la riga dell'obiettivo", mm != null && mm.Visibile && mm.CroceVisibile && mm.TestoObiettivo == Obiettivi.Testo(ob, Hub),
                mm != null ? "croce=" + mm.CroceVisibile + " testo='" + mm.TestoObiettivo + "'" : "null");
            Check(quando + ": HUD visibile", UIRoot.I.HudLayer.gameObject.activeSelf);
            Check(quando + ": salvataggio con filmato visto", gm.Save.filmatoVisto && gm.Save.cutsceneViste.Contains("intro"));
        }

        IEnumerator ControllaDispositivi()
        {
            var f = Film;
            Check("targhette: tastiera = W A S D", f.TastiPer("muovi", "tastiera") == "W A S D", f.TastiPer("muovi", "tastiera"));
            Check("targhette: gamepad = Leva sinistra", f.TastiPer("muovi", "gamepad") == "Leva sinistra", f.TastiPer("muovi", "gamepad"));
            Check("targhette: touch = Joystick a sinistra", f.TastiPer("muovi", "touch") == "Joystick a sinistra", f.TastiPer("muovi", "touch"));
            yield return Tap(Key.W);
            Check("dispositivo = tastiera dopo un tasto", f.DispositivoOra == "tastiera", f.DispositivoOra);
            var gp = InputSystem.AddDevice<Gamepad>("IntroTestPad");
            InputSystem.QueueStateEvent(gp, new GamepadState(GamepadButton.South));
            yield return Attendi(0.3f);
            InputSystem.QueueStateEvent(gp, new GamepadState());
            yield return Attendi(0.3f);
            InputSystem.QueueStateEvent(gp, new GamepadState(GamepadButton.East));
            yield return Attendi(0.3f);
            InputSystem.QueueStateEvent(gp, new GamepadState());
            yield return Attendi(0.3f);
            Check("dispositivo = gamepad dopo l'uso del gamepad", f.DispositivoOra == "gamepad", f.DispositivoOra);
            yield return Tap(Key.W);
            Check("dispositivo torna a tastiera con un tasto", f.DispositivoOra == "tastiera", f.DispositivoOra);
            InputSystem.RemoveDevice(gp);
            var prop = typeof(UIRoot).GetProperty("TouchMode");
            prop.SetValue(null, true, null);
            Check("dispositivo = touch in modalita' touch", f.DispositivoOra == "touch", f.DispositivoOra);
            prop.SetValue(null, false, null);
            yield return Attendi(0.2f);
        }

        void ReportFps(List<float> frame, Dictionary<int, List<float>> perCap)
        {
            var sb = new StringBuilder("capitolo;frame;fps_medi;fps_1pct_low;max_ms\n");
            foreach (var kv in perCap) sb.AppendLine(Riga((kv.Key + 1).ToString(), kv.Value));
            sb.AppendLine(Riga("tutto", frame));
            File.WriteAllText(Path.Combine(dir, "fps.csv"), sb.ToString(), Encoding.UTF8);
            Log("FPS (preset Alta, " + Screen.width + "x" + Screen.height + ", " + SystemInfo.graphicsDeviceName + "):\n" + sb);
            float media = Media(frame);
            Check("FPS medi del filmato >= 60 (preset Alta)", 1f / Mathf.Max(0.0001f, media) >= 60f, (1f / Mathf.Max(0.0001f, media)).ToString("0") + " fps");
        }

        static float Media(List<float> l) { if (l.Count == 0) return 1f; float s = 0f; foreach (var x in l) s += x; return s / l.Count; }

        static string Riga(string nome, List<float> l)
        {
            if (l.Count == 0) return nome + ";0;0;0;0";
            var c = new List<float>(l); c.Sort();
            float media = Media(l);
            int n1 = Mathf.Max(1, c.Count / 100); float s1 = 0f;
            for (int i = c.Count - n1; i < c.Count; i++) s1 += c[i];
            return nome + ";" + l.Count + ";" + (1f / media).ToString("0.0", Inv) + ";" + (1f / (s1 / n1)).ToString("0.0", Inv) + ";" + (c[c.Count - 1] * 1000f).ToString("0.0", Inv);
        }
    }
}
#endif
