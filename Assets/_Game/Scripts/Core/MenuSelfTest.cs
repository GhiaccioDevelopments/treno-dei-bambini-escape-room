#if UNITY_EDITOR || DEBUG
// R023: prova automatica del flusso dei menu, solo negli editor e nelle build di sviluppo (mai nella build normale, WebGL compresa).
using System.Collections;
using System.IO;
using System.Text;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.Core
{
    /// <summary>
    /// R023: prova del flusso Titolo, salto dell'intro con un clic, Ricordi, Impostazioni, Crediti, conferma Nuova partita, Continua, gioco,
    /// inventario, pausa, Torna al titolo. Attiva SOLO con l'argomento -menutest [-menutestout cartella] (es. TrenoBambini.exe -menutest -screen-width 1280
    /// -screen-height 720 -screen-fullscreen 0). I tasti e il mouse sono eventi dell'Input System su dispositivi virtuali (passano dal vero percorso
    /// EventSystem -> Button, ma non toccano la tastiera del computer, quindi la prova gira anche con la finestra in secondo piano).
    /// Salva e rimette a posto il salvataggio del giocatore. Scrive menutest.log e gli screenshot PNG nella cartella indicata, poi chiude.
    /// </summary>
    public class MenuSelfTest : MonoBehaviour
    {
        static readonly string[] Args = System.Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            foreach (var a in Args)
                if (a == "-menutest")
                {
                    var go = new GameObject("MenuSelfTest");
                    DontDestroyOnLoad(go);
                    go.AddComponent<MenuSelfTest>();
                    return;
                }
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
        TitleScreen title;

        void Log(string s)
        {
            Debug.Log("[MenuTest] " + s);
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
            dir = Arg("-menutestout", Path.Combine(Application.dataPath, "..", "menutest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "menutest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova. Salvataggio presente: " + avevaSave);

            StartCoroutine(Osserva());
            string pre = Arg("-menutestpreset", "");
            if (pre != "") { GraphicsPresets.Apply(int.Parse(pre), false); Log("Preset forzato: " + pre); }
            kb = InputSystem.AddDevice<Keyboard>("MenuTestKb");
            ms = InputSystem.AddDevice<Mouse>("MenuTestMouse");

            // con la finestra in secondo piano il computer del giocatore potrebbe mandare i suoi tasti, il suo mouse o il suo gamepad (altri giochi aperti):
            // la prova ascolta solo i dispositivi virtuali
            foreach (var d in InputSystem.devices) if (d != kb && d != ms) InputSystem.DisableDevice(d);
            InputSystem.onDeviceChange += (d, ch) => { if ((ch == InputDeviceChange.Added || ch == InputDeviceChange.Reconnected || ch == InputDeviceChange.Enabled) && d != kb && d != ms) InputSystem.DisableDevice(d); };

            yield return Flusso();

            // ripristino del salvataggio del giocatore
            if (avevaSave) PlayerPrefs.SetString("treno_save", saveBackup); else PlayerPrefs.DeleteKey("treno_save");
            PlayerPrefs.Save();
            Log("Fine prova: " + ok + " PASS, " + ko + " FAIL");
            File.WriteAllText(Path.Combine(dir, ko == 0 ? "OK.txt" : "FALLITA.txt"), ok + " PASS, " + ko + " FAIL\n");
            Application.Quit();
        }

        /// <summary>Diagnosi: scrive nel log ogni cambio di fase del titolo, di scena attiva o di numero di scene.</summary>
        IEnumerator Osserva()
        {
            string ultimo = "";
            while (true)
            {
                var ts = FindAnyObjectByType<TitleScreen>();
                string st = (ts != null ? ts.Fase + "/" + ts.FuocoId : "-") + " scena=" + SceneManager.GetActiveScene().name + " n=" + SceneManager.sceneCount + " backdrop=" + GameManager.MenuBackdrop;
                if (st != ultimo) { ultimo = st; Log("  ~ " + st + " @" + Time.unscaledTime.ToString("0.0")); }
                yield return null;
            }
        }

        // ------------------------------------------------------------------ utilita'
        IEnumerator Tap(Key k)
        {
            InputSystem.QueueStateEvent(kb, new KeyboardState(k));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null; yield return null;
        }

        IEnumerator Attendi(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        IEnumerator Foto(string nome)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, nome + ".png"));
            yield return null; yield return null;
        }

        static Vector2 CentroSchermo(RectTransform rt)
        {
            var c = rt.GetComponentInParent<Canvas>();
            Camera cam = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        }

        static GameObject Trova(string nome)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                if (t.name == nome) return t.gameObject;
            return null;
        }

        static Button TrovaBottone(string testo)
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                var tx = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tx != null && tx.text == testo) return b;
            }
            return null;
        }

        static string Selezionato() { var s = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null; return s != null ? s.name : "(nessuno)"; }

        IEnumerator ScenaGioco(float timeout, System.Action<bool> res)
        {
            float t = 0f;
            while (t < timeout)
            {
                t += Time.unscaledDeltaTime;
                var a = SceneManager.GetActiveScene().name;
                if (a == GameManager.SceneHub && !GameManager.MenuBackdrop && SceneManager.sceneCount == 1) { res(true); yield break; }
                yield return null;
            }
            res(false);
        }

        IEnumerator ScenaTitolo(float timeout, System.Action<bool> res)
        {
            float t = 0f;
            while (t < timeout)
            {
                t += Time.unscaledDeltaTime;
                title = FindAnyObjectByType<TitleScreen>();
                if (SceneManager.GetActiveScene().name == GameManager.SceneTitolo || (title != null && GameManager.MenuBackdrop)) { if (title != null) { res(true); yield break; } }
                yield return null;
            }
            res(false);
        }

        // ------------------------------------------------------------------ il flusso
        /// <summary>Aspetta una condizione (massimo timeout secondi); vale true se si e' avverata.</summary>
        IEnumerator Aspetta(System.Func<bool> cond, float timeout, System.Action<bool> res = null)
        {
            float t = 0f;
            while (t < timeout && !cond()) { t += Time.unscaledDeltaTime; yield return null; }
            if (res != null) res(cond());
        }

        void ParcheggiaMouse()
        {
            var p = new Vector2(Screen.width - 4f, 4f);
            InputSystem.QueueStateEvent(ms, new MouseState { position = p });
        }

        /// <summary>Preme S finche' il fuoco e' sulla voce indicata; vale false se non ci arriva (e in quel caso non si deve premere Invio).</summary>
        IEnumerator Raggiungi(string id, System.Action<bool> res)
        {
            var ordine = new System.Collections.Generic.List<string> { "continua", "nuova", "ricordi", "impostazioni", "crediti", "esci" };
            for (int i = 0; i < 10 && title.FuocoId != id; i++)
            {
                int da = ordine.IndexOf(title.FuocoId), a = ordine.IndexOf(id);
                yield return Tap(a >= da ? Key.S : Key.W);
                yield return null;
            }
            yield return Attendi(0.15f);
            bool ok2 = title.FuocoId == id;
            if (!ok2) Check("Fuoco su " + id, false, title.FuocoId);
            res(ok2);
        }

        IEnumerator Flusso()
        {
            // --- titolo e salto dell'intro con un CLIC su una voce (I1): non deve partire nulla
            bool pronto = false;
            yield return ScenaTitolo(30f, r => pronto = r);
            Check("Titolo caricato", pronto);
            if (!pronto) yield break;
            yield return Aspetta(() => title.Fase == "Intro", 40f);
            Check("Intro in corso", title.Fase == "Intro", title.Fase);
            yield return Attendi(1.2f);
            var voce = Trova("Voce_nuova");
            Check("Voce 'Nuova partita' trovata", voce != null);
            var pos = voce != null ? CentroSchermo((RectTransform)voce.transform) : new Vector2(Screen.width * 0.2f, Screen.height * 0.5f);
            Log("Clic (salto intro) a " + pos + " su schermo " + Screen.width + "x" + Screen.height);
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = pos });   // rilascio
            yield return null; yield return null;
            ParcheggiaMouse();
            yield return Attendi(0.8f);
            Check("Dopo il clic l'intro e' saltata (fase Menu)", title.Fase == "Menu", title.Fase);
            Check("Il clic non ha aperto la conferma ne' lanciato nulla", title.Fase != "Conferma" && title.Fase != "Lancio" && SceneManager.sceneCount == 2 && GameManager.MenuBackdrop);
            yield return Attendi(1.0f);
            Check("Ancora nel menu un secondo dopo", title.Fase == "Menu" && GameManager.MenuBackdrop, title.Fase);
            Check("Il fuoco c'e' (una voce selezionata)", title.FuocoId != "", Selezionato());
            yield return Foto("titolo");

            bool su = false;
            // --- Ricordi
            yield return Raggiungi("ricordi", r => su = r);
            if (su)
            {
                yield return Tap(Key.Enter);
                yield return Aspetta(() => title.Fase == "Sottomenu", 3f);
                Check("Ricordi aperto (Sottomenu)", title.Fase == "Sottomenu", title.Fase);
                yield return Attendi(0.9f);
                yield return Foto("ricordi_titolo");
                yield return Tap(Key.Escape);
                yield return Aspetta(() => title.Fase == "Menu", 3f);
                Check("Esc torna al menu", title.Fase == "Menu", title.Fase);
                yield return Attendi(0.5f);
                Check("Fuoco di nuovo su Ricordi", title.FuocoId == "ricordi" && Selezionato() == "Voce_ricordi", title.FuocoId + " / " + Selezionato());
            }

            // --- Impostazioni (M4)
            yield return Raggiungi("impostazioni", r => su = r);
            if (su)
            {
                yield return Tap(Key.Enter);
                yield return Aspetta(() => UIRoot.I.Settings.IsOpen, 3f);
                Check("Impostazioni aperte", UIRoot.I.Settings.IsOpen);
                yield return Attendi(0.8f);
                yield return Tap(Key.Escape);
                yield return Aspetta(() => !UIRoot.I.Settings.IsOpen, 3f);
                Check("Impostazioni chiuse con Esc", !UIRoot.I.Settings.IsOpen);
                yield return Attendi(0.6f);
                Check("Dopo le Impostazioni il fuoco e' tornato sulla voce", Selezionato() == "Voce_impostazioni", Selezionato());
            }

            // --- Crediti
            yield return Raggiungi("crediti", r => su = r);
            if (su)
            {
                yield return Tap(Key.Enter);
                yield return Aspetta(() => title.Fase == "Sottomenu", 3f);
                Check("Crediti aperti", title.Fase == "Sottomenu", title.Fase);
                yield return Attendi(0.9f);
                yield return Tap(Key.Escape);
                yield return Aspetta(() => title.Fase == "Menu", 3f);
                Check("Crediti chiusi", title.Fase == "Menu", title.Fase);
            }

            // --- Nuova partita: conferma su foglietto, Annulla
            bool haSave = GameManager.I.HasSave;
            yield return Raggiungi("nuova", r => su = r);
            if (su && haSave)
            {
                yield return Tap(Key.Enter);
                yield return Aspetta(() => title.Fase == "Conferma", 3f);
                Check("Nuova partita con salvataggio: conferma aperta", title.Fase == "Conferma", title.Fase);
                yield return Attendi(0.9f);
                yield return Foto("conferma");
                yield return Tap(Key.Escape);
                yield return Aspetta(() => title.Fase == "Menu", 3f);
                Check("Annulla (Esc) torna al menu", title.Fase == "Menu", title.Fase);
                yield return Attendi(0.5f);
            }

            // --- Continua -> gioco
            if (!haSave) { Log("Nessun salvataggio: provo Nuova partita al posto di Continua"); }
            string voceGioco = haSave ? "continua" : "nuova";
            yield return Raggiungi(voceGioco, r => su = r);
            if (!su) yield break;
            yield return Attendi(0.9f);
            yield return Foto("titolo_continua");
            yield return Tap(Key.Enter);
            bool gioco = false;
            yield return ScenaGioco(60f, r => gioco = r);
            Check("Il gioco parte (hub, un'unica scena)", gioco, SceneManager.GetActiveScene().name + " scene=" + SceneManager.sceneCount);
            if (!gioco) yield break;

            yield return Attendi(5f);
            int attivi = 0; foreach (var l in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude)) if (l.enabled) attivi++;
            Check("Un solo AudioListener attivo nel gioco", attivi == 1, attivi.ToString());
            var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            InputSystem.QueueStateEvent(ms, new MouseState { position = c }); yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = c }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(ms, new MouseState { position = c }); yield return null; yield return null;
            ParcheggiaMouse();
            yield return Attendi(1f);
            // con la finestra in secondo piano il blocco del cursore cade e il gioco apre da solo la pausa (OpenPauseFromLockLoss): qui la si richiude
            if (UIRoot.I.Pause.IsOpen)
            {
                Log("(la pausa si e' aperta da sola per la perdita del blocco cursore: la chiudo)");
                yield return Attendi(0.7f);
                yield return Tap(Key.Escape);
                yield return Aspetta(() => !UIRoot.I.Pause.IsOpen, 3f);
                yield return Attendi(1f);
            }
            float tempo0 = GameManager.I.Save.tempoGioco;
            yield return Attendi(3f);
            Check("Il tempo di gioco avanza in partita", GameManager.I.Save.tempoGioco > tempo0 + 2f, tempo0 + " -> " + GameManager.I.Save.tempoGioco);

            yield return Tap(Key.I);
            yield return Aspetta(() => UIRoot.I.Inventory.IsOpen, 3f);
            Check("Tasto I apre l'inventario", UIRoot.I.Inventory.IsOpen);
            yield return Attendi(1.2f);
            yield return Foto("inventario_oggetti");
            yield return Tap(Key.E);                       // pagina Ricordi
            yield return Attendi(1.0f);
            yield return Foto("inventario_ricordi");
            yield return Tap(Key.Q);
            yield return Attendi(0.5f);
            float tempoInv = GameManager.I.Save.tempoGioco;
            yield return Attendi(2f);
            Check("Il tempo non avanza con l'inventario aperto", GameManager.I.Save.tempoGioco - tempoInv < 0.1f, tempoInv + " -> " + GameManager.I.Save.tempoGioco);
            yield return Tap(Key.Escape);
            yield return Aspetta(() => !UIRoot.I.Inventory.IsOpen, 3f);
            Check("Esc chiude l'inventario", !UIRoot.I.Inventory.IsOpen);
            yield return Attendi(1.0f);

            // --- con le scarpe nella latta (ricordo ancora da rivivere): scheda oggetto con descrizione, significato, ricordo e stato
            var sv = GameManager.I.Save;
            if (!sv.inventario.Contains("scarpe")) sv.inventario.Add("scarpe");
            sv.flashbackCompletati.Remove("flashback_scarpe");
            yield return Tap(Key.I);
            yield return Aspetta(() => UIRoot.I.Inventory.IsOpen, 3f);
            yield return Attendi(1.4f);
            yield return Foto("inventario_scheda");
            yield return Tap(Key.E);
            yield return Attendi(1.0f);
            yield return Foto("inventario_ricordi_da_rivivere");
            sv.flashbackCompletati.Add("flashback_scarpe");
            yield return Tap(Key.Q); yield return Tap(Key.E);
            yield return Attendi(1.0f);
            yield return Foto("inventario_ricordi_rivissuto");
            yield return Tap(Key.Escape);
            yield return Aspetta(() => !UIRoot.I.Inventory.IsOpen, 3f);
            Check("Inventario con le scarpe aperto e richiuso", !UIRoot.I.Inventory.IsOpen);
            yield return Attendi(1.0f);

            yield return Tap(Key.Escape);
            yield return Aspetta(() => UIRoot.I.Pause.IsOpen, 3f);
            Check("Esc apre la pausa", UIRoot.I.Pause.IsOpen);
            yield return Attendi(0.6f);
            float tempoPausa = GameManager.I.Save.tempoGioco;
            yield return Attendi(2f);
            Check("Il tempo non avanza in pausa", GameManager.I.Save.tempoGioco - tempoPausa < 0.1f, tempoPausa + " -> " + GameManager.I.Save.tempoGioco);
            yield return Foto("pausa");
            var bt = TrovaBottone("Torna al titolo");
            Check("Pulsante 'Torna al titolo' trovato", bt != null);
            if (bt == null) yield break;
            EventSystem.current.SetSelectedGameObject(bt.gameObject);
            yield return null;
            yield return Tap(Key.Enter);
            yield return Attendi(0.9f);
            var si = TrovaBottone("Sì, torna al titolo");
            Check("Conferma 'Torna al titolo' aperta", si != null);
            if (si == null) yield break;
            EventSystem.current.SetSelectedGameObject(si.gameObject);
            yield return null;
            yield return Tap(Key.Enter);
            bool torna = false;
            yield return ScenaTitolo(60f, r => torna = r);
            Check("Torna al titolo", torna);
            if (!torna) yield break;

            // --- secondo giro: salto dell'intro con INVIO (I1)
            yield return Aspetta(() => title.Fase == "Intro", 30f);
            yield return Attendi(1.0f);
            yield return Tap(Key.Enter);
            yield return Attendi(0.9f);
            Check("Secondo giro: Invio salta l'intro senza lanciare il gioco", title.Fase == "Menu" && SceneManager.sceneCount == 2 && GameManager.MenuBackdrop, title.Fase + " n=" + SceneManager.sceneCount);
            yield return Attendi(1.0f);
            Check("Secondo giro: ancora nel menu dopo un secondo", title.Fase == "Menu", title.Fase);
            yield return Foto("titolo_secondo_giro");
        }
    }
}
#endif
