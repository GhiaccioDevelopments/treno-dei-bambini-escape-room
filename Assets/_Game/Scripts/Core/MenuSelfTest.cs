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

            kb = InputSystem.AddDevice<Keyboard>("MenuTestKb");
            ms = InputSystem.AddDevice<Mouse>("MenuTestMouse");

            yield return Flusso();

            // ripristino del salvataggio del giocatore
            if (avevaSave) PlayerPrefs.SetString("treno_save", saveBackup); else PlayerPrefs.DeleteKey("treno_save");
            PlayerPrefs.Save();
            Log("Fine prova: " + ok + " PASS, " + ko + " FAIL");
            File.WriteAllText(Path.Combine(dir, ko == 0 ? "OK.txt" : "FALLITA.txt"), ok + " PASS, " + ko + " FAIL\n");
            Application.Quit();
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

        /// <summary>Preme S finche' il fuoco e' sulla voce indicata.</summary>
        IEnumerator VaiAVoce(string id)
        {
            for (int i = 0; i < 8 && title.FuocoId != id; i++) yield return Tap(Key.S);
            yield return Attendi(0.2f);
        }

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
        IEnumerator Flusso()
        {
            // --- titolo e salto dell'intro con un CLIC su una voce (I1): non deve partire nulla
            bool pronto = false;
            yield return ScenaTitolo(30f, r => pronto = r);
            Check("Titolo caricato", pronto);
            if (!pronto) yield break;
            float t0 = 0f;
            while (title.Fase != "Intro" && t0 < 40f) { t0 += Time.unscaledDeltaTime; yield return null; }
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
            yield return Attendi(0.8f);
            Check("Dopo il clic l'intro e' saltata (fase Menu)", title.Fase == "Menu", title.Fase);
            Check("Il clic non ha aperto la conferma ne' lanciato nulla", title.Fase != "Conferma" && title.Fase != "Lancio" && SceneManager.GetActiveScene().name != "20_Int_BassoAntonietta");
            yield return Attendi(1.0f);
            Check("Ancora nel menu un secondo dopo", title.Fase == "Menu", title.Fase);
            Check("Il fuoco c'e' (una voce selezionata)", title.FuocoId != "", Selezionato());
            string primaVoce = title.FuocoId;
            yield return Foto("titolo");

            // --- Ricordi
            yield return VaiAVoce("ricordi");
            Check("Fuoco su Ricordi", title.FuocoId == "ricordi", title.FuocoId);
            yield return Tap(Key.Enter);
            yield return Attendi(1.0f);
            Check("Ricordi aperto (Sottomenu)", title.Fase == "Sottomenu", title.Fase);
            yield return Foto("ricordi_titolo");
            yield return Tap(Key.Escape);
            yield return Attendi(1.0f);
            Check("Esc torna al menu", title.Fase == "Menu", title.Fase);
            Check("Fuoco di nuovo su Ricordi", title.FuocoId == "ricordi" && Selezionato() == "Voce_ricordi", title.FuocoId + " / " + Selezionato());

            // --- Impostazioni (M4)
            yield return VaiAVoce("impostazioni");
            yield return Tap(Key.Enter);
            yield return Attendi(0.8f);
            Check("Impostazioni aperte", UIRoot.I.Settings.IsOpen);
            yield return Tap(Key.Escape);
            yield return Attendi(0.8f);
            Check("Impostazioni chiuse con Esc", !UIRoot.I.Settings.IsOpen);
            Check("Dopo le Impostazioni il fuoco e' tornato sulla voce", Selezionato() == "Voce_impostazioni", Selezionato());

            // --- Crediti
            yield return VaiAVoce("crediti");
            yield return Tap(Key.Enter);
            yield return Attendi(1.0f);
            Check("Crediti aperti", title.Fase == "Sottomenu", title.Fase);
            yield return Tap(Key.Escape);
            yield return Attendi(1.0f);
            Check("Crediti chiusi", title.Fase == "Menu", title.Fase);

            // --- Nuova partita: conferma su foglietto, Annulla
            bool haSave = GameManager.I.HasSave;
            yield return VaiAVoce("nuova");
            yield return Tap(Key.Enter);
            yield return Attendi(0.8f);
            if (haSave) Check("Nuova partita con salvataggio: conferma aperta", title.Fase == "Conferma", title.Fase);
            if (haSave) { yield return Foto("conferma"); yield return Tap(Key.Escape); yield return Attendi(0.8f); Check("Annulla (Esc) torna al menu", title.Fase == "Menu", title.Fase); }

            // --- Continua -> gioco
            if (haSave)
            {
                yield return VaiAVoce("continua");
                yield return Attendi(0.6f);
                yield return Foto("titolo_continua");
                yield return Tap(Key.Enter);
                bool gioco = false;
                yield return ScenaGioco(40f, r => gioco = r);
                Check("Continua porta al gioco (hub, un'unica scena)", gioco, SceneManager.GetActiveScene().name + " scene=" + SceneManager.sceneCount);
                if (gioco)
                {
                    yield return Attendi(5f);
                    var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
                    int attivi = 0; foreach (var l in listeners) if (l.enabled) attivi++;
                    Check("Un solo AudioListener attivo nel gioco", attivi == 1, attivi.ToString());
                    // clic al centro: eventuale carta "clicca per giocare"
                    var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    InputSystem.QueueStateEvent(ms, new MouseState { position = c }); yield return null; yield return null;
                    InputSystem.QueueStateEvent(ms, new MouseState { position = c }.WithButton(MouseButton.Left)); yield return null; yield return null;
                    InputSystem.QueueStateEvent(ms, new MouseState { position = c }); yield return null; yield return null;
                    yield return Attendi(1f);
                    float tempo0 = GameManager.I.Save.tempoGioco;
                    yield return Attendi(3f);
                    Check("Il tempo di gioco avanza in partita", GameManager.I.Save.tempoGioco > tempo0 + 2f, tempo0 + " -> " + GameManager.I.Save.tempoGioco);

                    yield return Tap(Key.I);
                    yield return Attendi(1.2f);
                    Check("Tasto I apre l'inventario", UIRoot.I.Inventory.IsOpen);
                    yield return Attendi(0.5f);
                    yield return Foto("inventario_oggetti");
                    yield return Tap(Key.E);                       // pagina Ricordi
                    yield return Attendi(0.8f);
                    yield return Foto("inventario_ricordi");
                    yield return Tap(Key.Q);
                    yield return Attendi(0.5f);
                    float tempoInv = GameManager.I.Save.tempoGioco;
                    yield return Attendi(2f);
                    Check("Il tempo non avanza con l'inventario aperto", GameManager.I.Save.tempoGioco - tempoInv < 0.1f, tempoInv + " -> " + GameManager.I.Save.tempoGioco);
                    yield return Tap(Key.Escape);
                    yield return Attendi(1.2f);
                    Check("Esc chiude l'inventario", !UIRoot.I.Inventory.IsOpen);

                    yield return Tap(Key.Escape);
                    yield return Attendi(1.0f);
                    Check("Esc apre la pausa", UIRoot.I.Pause.IsOpen);
                    float tempoPausa = GameManager.I.Save.tempoGioco;
                    yield return Attendi(2f);
                    Check("Il tempo non avanza in pausa", GameManager.I.Save.tempoGioco - tempoPausa < 0.1f, tempoPausa + " -> " + GameManager.I.Save.tempoGioco);
                    yield return Foto("pausa");
                    var bt = TrovaBottone("Torna al titolo");
                    Check("Pulsante 'Torna al titolo' trovato", bt != null);
                    if (bt != null)
                    {
                        EventSystem.current.SetSelectedGameObject(bt.gameObject);
                        yield return null;
                        yield return Tap(Key.Enter);
                        yield return Attendi(0.8f);
                        var si = TrovaBottone("Sì, torna al titolo");
                        Check("Conferma 'Torna al titolo' aperta", si != null);
                        if (si != null)
                        {
                            EventSystem.current.SetSelectedGameObject(si.gameObject);
                            yield return null;
                            yield return Tap(Key.Enter);
                        }
                        bool torna = false;
                        yield return ScenaTitolo(40f, r => torna = r);
                        Check("Torna al titolo", torna);
                        if (torna)
                        {
                            // --- secondo giro: salto dell'intro con INVIO (I1)
                            float tw = 0f;
                            while (title.Fase != "Intro" && tw < 20f) { tw += Time.unscaledDeltaTime; yield return null; }
                            yield return Attendi(1.0f);
                            yield return Tap(Key.Enter);
                            yield return Attendi(0.9f);
                            Check("Secondo giro: Invio salta l'intro senza lanciare il gioco", title.Fase == "Menu" && SceneManager.GetActiveScene().name != "20_Int_BassoAntonietta", title.Fase);
                            yield return Attendi(1.0f);
                            Check("Secondo giro: ancora nel menu dopo un secondo", title.Fase == "Menu", title.Fase);
                        }
                    }
                }
            }
        }
    }
}
#endif
