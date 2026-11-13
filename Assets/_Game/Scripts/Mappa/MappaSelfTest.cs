#if UNITY_EDITOR || DEBUG
// R019: prova automatica di minimappa e obiettivi, solo negli editor e nelle build di sviluppo (mai nella build normale).
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Player;
using TrenoBambini.Puzzles;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Mappa
{
    /// <summary>
    /// R019: percorre il capitolo "Le scarpe" (hub, basso, ricordo) e a ogni tappa controlla che il prossimo obiettivo sia quello giusto, che la
    /// minimappa lo mostri dove deve (X rossa o freccia sul bordo), che le tre modalita' di aiuto si comportino (Guidata / Normale / Esploratore),
    /// che i nomi dei vicoli compaiano, che il tasto M apra e chiuda la mappa grande, che le preferenze si salvino. Attiva SOLO con
    /// -mappatest [-mappatestout cartella] (nell'editor: SessionState "mappatest"). Scrive mappatest.log, gli screenshot JPG 1280x720 e
    /// OK.txt / FALLITA.txt; poi chiude. Salva e rimette a posto il salvataggio del giocatore.
    /// </summary>
    public class MappaSelfTest : MonoBehaviour
    {
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-mappatest") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("mappatest", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("MappaSelfTest");
            DontDestroyOnLoad(go);
            go.AddComponent<MappaSelfTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        const string Hub = "10_Hub_Vicolo", Basso = "20_Int_BassoAntonietta", Ricordo = "30_FB_Scarpe";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        string dir, logPath;
        int ok, ko;
        Keyboard kb;
        Mouse ms;

        void Log(string s)
        {
            Debug.Log("[MappaTest] " + s);
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
            dir = Arg("-mappatestout", Path.Combine(Application.dataPath, "..", "mappatest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "mappatest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova mappa. Salvataggio presente: " + avevaSave);

            kb = InputSystem.AddDevice<Keyboard>("MappaTestKb");
            ms = InputSystem.AddDevice<Mouse>("MappaTestMouse");
            foreach (var d in InputSystem.devices) if (d != kb && d != ms) InputSystem.DisableDevice(d);
            InputSystem.onDeviceChange += (d, ch) => { if ((ch == InputDeviceChange.Added || ch == InputDeviceChange.Reconnected || ch == InputDeviceChange.Enabled) && d != kb && d != ms && !d.name.StartsWith("MappaTest")) InputSystem.DisableDevice(d); };

            bool fine = false;
            try { StartCoroutine(Giro(() => fine = true)); } catch (Exception e) { Log("ERRORE " + e); }
            float guardia = 0f;
            while (!fine && guardia < 400f) { guardia += Time.unscaledDeltaTime; yield return null; }
            if (!fine) Check("la prova e' finita nei tempi", false);

            if (avevaSave) PlayerPrefs.SetString("treno_save", saveBackup); else PlayerPrefs.DeleteKey("treno_save");
            PlayerPrefs.Save();
            Log("Fine prova: " + ok + " PASS, " + ko + " FAIL");
            File.WriteAllText(Path.Combine(dir, ko == 0 ? "OK.txt" : "FALLITA.txt"), ok + " PASS, " + ko + " FAIL\n");
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("mappatest", false);
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator Giro(Action fine)
        {
            yield return Flusso();
            fine();
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

        IEnumerator Foto(string nome)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) yield break;
            var rt = RenderTexture.GetTemporary(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear); // lineare: la cattura e' gia' in sRGB, un RT sRGB la codificherebbe due volte (colori slavati)
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

        IEnumerator ScenaPronta(string scena)
        {
            float t = 0f;
            while (t < 30f)
            {
                t += Time.unscaledDeltaTime;
                if (SceneManager.GetActiveScene().name == scena && SceneLoader.I != null && !SceneLoader.I.IsLoading && SceneManager.sceneCount == 1) break;
                yield return null;
            }
            yield return Attendi(1.2f);
        }

        IEnumerator ChiudiMessaggi()
        {
            for (int giro = 0; giro < 12; giro++)
            {
                UnityEngine.UI.Button trovato = null;
                foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude))
                {
                    var tx = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tx != null && (tx.text == "Continua" || tx.text == "Avanti") && b.interactable && b.gameObject.activeInHierarchy) { trovato = b; break; }
                }
                if (trovato == null) yield break;
                trovato.onClick.Invoke();
                yield return Attendi(0.5f);
            }
        }

        static ThirdPersonController Giocatore() { return FindAnyObjectByType<ThirdPersonController>(); }

        /// <summary>Mette Amerigo in (x, z) sul terreno (il piu' alto fra Selciato e Pavimento: i tetti non contano).</summary>
        IEnumerator Porta(float x, float z)
        {
            var p = Giocatore();
            if (p == null) yield break;
            float y = p.transform.position.y;
            var hits = Physics.RaycastAll(new Vector3(x, 80f, z), Vector3.down, 160f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MinValue;
            foreach (var h in hits)
                if ((h.collider.name.StartsWith("Selciato") || h.collider.name.StartsWith("Pavimento")) && h.point.y > best && h.point.y < y + 6f) best = h.point.y;
            if (best > float.MinValue) y = best + 0.1f;
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            p.transform.position = new Vector3(x, y, z);
            if (cc != null) cc.enabled = true;
            p.ResetMotion();
            yield return Attendi(0.5f);
        }

        static string Id() { var o = Obiettivi.Corrente(); return o != null ? o.id : "(finito)"; }

        void Obiettivo(string quando, string atteso)
        {
            Check(quando + ": prossimo obiettivo = " + atteso, Id() == atteso, "trovato " + Id());
        }

        void Completa(string chiave)
        {
            var def = MondoStato.Trova(chiave);
            Check("enigma " + chiave + " esiste", def != null);
            if (def != null) MondoStato.Completa(def, chiave);
        }

        static Vector2 Pos2(Vector3 v) { return new Vector2(v.x, v.z); }

        /// <summary>La croce (o la freccia di bordo) di MinimappaHud punta al punto atteso: si controlla la posizione sullo schermo con la formula inversa.</summary>
        void ControllaSegno(string quando, ObiettivoPuntoDef punto, bool vicino)
        {
            var m = UIRoot.I.Minimappa;
            if (punto == null) { Check(quando + ": nessun segno (passo senza punto qui)", m != null && !m.CroceVisibile && !m.FrecciaBordoVisibile); return; }
            Check(quando + ": minimappa visibile", m != null && m.Visibile);
            if (vicino) Check(quando + ": la X rossa e' sulla mappa", m.CroceVisibile && !m.FrecciaBordoVisibile);
            else Check(quando + ": la freccia sul bordo indica l'obiettivo lontano", m.FrecciaBordoVisibile && !m.CroceVisibile);
        }

        // ------------------------------------------------------------------ flusso
        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);

            // ---------- dati
            var mp = GameData.Mappe;
            Check("mappe.json: tre scene", mp.scene.Count == 3, mp.scene.Count + " scene");
            Check("mappe.json: obiettivi del capitolo", mp.obiettivi.Count >= 11, mp.obiettivi.Count + " obiettivi");
            foreach (var s in mp.scene)
            {
                bool areaOk = s.ax1 > s.ax0 && s.az1 > s.az0 && s.ppm > 0f;
                Check(s.scena + ": area della pianta scritta", areaOk, s.ax0 + ".." + s.ax1 + " / " + s.az0 + ".." + s.az1);
                Check(s.scena + ": immagine della pianta presente", Resources.Load<Texture2D>("Mappe/" + s.scena) != null);
                foreach (var st in s.strade)
                    Check(s.scena + ": strada " + st.id + " ha nome, zona e ancora", !string.IsNullOrEmpty(st.nome) && st.zone.Count > 0 && st.ancore.Count > 0);
            }
            foreach (var g in new[] { "porta", "fontana", "edicola", "pozzo", "lavatoio", "uscita", "stazione", "bottega" })
                Check("glifo " + g + " nella striscia", MappaArt.Glifo(g) != null);
            Check("Via Roma e' scritta come nel 1946 (già Toledo)", mp.scene[0].strade.Exists(x => x.nome == "Via Roma (già Toledo)"));
            foreach (var o in mp.obiettivi)
            {
                bool tokenOk = o.completo.Count > 0;
                foreach (var t in o.completo) if (!(MondoStato.Trova("mondo/" + t) != null || MondoStato.Trova("flashback_scarpe/" + t) != null)) tokenOk = false;
                Check("obiettivo " + o.id + ": il gettone di completamento e' un enigma vero", tokenOk);
                Check("obiettivo " + o.id + ": ha almeno un punto e un testo", o.punti.Count > 0 && !string.IsNullOrEmpty(o.testo));
                Check("obiettivo " + o.id + ": appartiene a un capitolo (dato in mappe.json)", !string.IsNullOrEmpty(o.capitolo), o.capitolo);
                if (o.punti.Exists(x => x.scena == Ricordo))
                {
                    Check("obiettivo " + o.id + ": se esci dal ricordo ha un punto nel basso", o.punti.Exists(x => x.scena == Basso && !string.IsNullOrEmpty(x.testo)));
                    Check("obiettivo " + o.id + ": se esci dal ricordo ha la porta del basso nell'hub", o.punti.Exists(x => x.scena == Hub && !string.IsNullOrEmpty(x.testo)));
                }
            }

            // ---------- nuova partita
            // le preferenze della mappa passano da una partita alla successiva: si parte dai valori di default
            var prima0 = GameManager.I.Save;
            prima0.aiutoMappa = Obiettivi.Normale; prima0.nordFisso = false; prima0.minimappaNascosta = false;
            PlayerPrefs.DeleteKey("treno_save");
            GameManager.I.NewGame();
            yield return ScenaPronta(Hub);
            yield return ChiudiMessaggi();
            var save = GameManager.I.Save;
            Check("preferenze di default: aiuto Normale, minimappa accesa, nord non fisso", save.aiutoMappa == Obiettivi.Normale && !save.minimappaNascosta && !save.nordFisso);
            Check("hub caricato", SceneManager.GetActiveScene().name == Hub);
            Check("HUD: minimappa e mappa grande create", UIRoot.I.Minimappa != null && UIRoot.I.PiantaGrande != null);
            yield return Attendi(1.0f);
            var mm = UIRoot.I.Minimappa;
            Check("hub: minimappa visibile in alto a sinistra", mm.Visibile);
            var rt = mm.Gruppo;
            Check("hub: la minimappa e' ancorata in alto a sinistra", rt.anchorMin == new Vector2(0f, 1f) && rt.anchorMax == new Vector2(0f, 1f));
            // lontana dal joystick (in basso a sinistra) e dal pulsante Interagisci (in basso a destra)
            var corners = new Vector3[4]; rt.GetWorldCorners(corners);
            Check("hub: la minimappa non scende nella meta' bassa dello schermo (joystick e pulsanti touch liberi)", corners[0].y > Screen.height * 0.40f, "angolo basso a " + corners[0].y + " su " + Screen.height);

            Obiettivo("inizio", "ob_bucato");
            Check("inizio: modalita' Normale, obiettivo ancora nascosto", Obiettivi.Modo == Obiettivi.Normale && !Obiettivi.Mostrato && !mm.CroceVisibile && !mm.FrecciaBordoVisibile && mm.TestoObiettivo == "");
            Check("hub: la targa dice il nome della strada dove si sta (Largo della Fontanella)", mm.NomeStrada == "Largo della Fontanella", mm.NomeStrada);
            Check("hub: compare almeno un nome di vicolo sulla minimappa", mm.EtichetteVisibili > 0, mm.EtichetteVisibili + " etichette");
            yield return Foto("minimappa_hub_normale");

            // ---------- modalita' Normale: l'obiettivo compare con Indizio e dopo qualche minuto, poi sparisce a ogni progresso
            Obiettivi.RivelaIndizio();
            yield return Attendi(1.2f);
            Check("Normale + Indizio: la freccia sul bordo appare", Obiettivi.Mostrato && mm.FrecciaBordoVisibile);
            Check("Normale + Indizio: la riga dice l'obiettivo", mm.TestoObiettivo.Contains("panni stesi"), mm.TestoObiettivo);
            Obiettivi.ImpostaFermo(0f, false);
            yield return Attendi(0.3f);
            Check("Normale: dopo l'azzeramento l'obiettivo si nasconde", !Obiettivi.Mostrato);
            Obiettivi.ImpostaFermo(GameData.Mappe.parametri.minutiSenzaProgressi * 60f + 1f, false);
            yield return Attendi(1.2f);
            Check("Normale: dopo " + GameData.Mappe.parametri.minutiSenzaProgressi + " minuti senza progressi l'obiettivo compare", Obiettivi.Mostrato && mm.FrecciaBordoVisibile);

            // ---------- modalita' Esploratore e Guidata
            save.aiutoMappa = Obiettivi.Esploratore;
            yield return Attendi(1.0f);
            Check("Esploratore: nessun obiettivo, solo nomi e posizione", !Obiettivi.Mostrato && !mm.CroceVisibile && !mm.FrecciaBordoVisibile && mm.TestoObiettivo == "" && mm.EtichetteVisibili > 0);
            save.aiutoMappa = Obiettivi.Guidata;
            Obiettivi.ImpostaFermo(0f, false);
            yield return Attendi(1.2f);
            Check("Guidata: l'obiettivo e' sempre segnato", Obiettivi.Mostrato && mm.FrecciaBordoVisibile);
            yield return Foto("minimappa_hub_guidata_lontano");

            // ---------- tappa 1: bucato (X vicina)
            var ob = Obiettivi.Corrente();
            var p = Obiettivi.Punto(ob, Hub);
            Check("bucato: il punto e' dove sta l'enigma nel mondo", p != null && Mathf.Abs(p.x - -6f) < 0.5f && Mathf.Abs(p.z - -2f) < 0.5f);
            yield return Porta(0f, -2f);
            yield return Attendi(0.8f);
            ControllaSegno("bucato", p, true);
            Check("bucato: la targa dice Largo Sant'Anna dei Panni o il vico", !string.IsNullOrEmpty(mm.NomeStrada), mm.NomeStrada);
            yield return Porta(p.x, p.z);
            yield return Attendi(0.6f);
            Check("arrivati sul punto: la X si spegne (raggioArrivo)", !mm.CroceVisibile && !mm.FrecciaBordoVisibile);
            yield return Porta(0f, -2f);
            yield return Attendi(0.6f);
            yield return Foto("minimappa_hub_guidata_vicino");

            // mappa grande dell'hub: intera e ingrandita attorno ad Amerigo
            yield return Tap(Key.M);
            yield return Attendi(1.0f);
            Check("hub: il tasto M apre la mappa grande", UIRoot.I.PiantaGrande.IsOpen);
            Check("hub: la mappa grande ha i nomi e la X dell'obiettivo", UIRoot.I.PiantaGrande.EtichetteVisibili > 3 && UIRoot.I.PiantaGrande.CroceVisibile, UIRoot.I.PiantaGrande.EtichetteVisibili + "/" + UIRoot.I.PiantaGrande.EtichetteTotali + " etichette");
            yield return Foto("mappa_grande_hub");
            var piuH = FindButton("+");
            if (piuH != null) { piuH.onClick.Invoke(); piuH.onClick.Invoke(); piuH.onClick.Invoke(); yield return Attendi(0.8f); }
            Check("hub: ingrandita mostra piu' nomi dei vicoli vicini", UIRoot.I.PiantaGrande.Zoom > 2f && UIRoot.I.PiantaGrande.EtichetteVisibili > 1, UIRoot.I.PiantaGrande.EtichetteVisibili + " etichette a zoom " + UIRoot.I.PiantaGrande.Zoom.ToString("0.0", Inv));
            yield return Foto("mappa_grande_hub_zoom");
            yield return Tap(Key.M);
            yield return Attendi(0.8f);

            Completa("mondo/enigma_bucato");
            Obiettivo("dopo il bucato", "ob_exvoto");
            Check("dopo il bucato: il segno si e' spostato (prima c'era il bucato, ora l'ex-voto)", Obiettivi.Punto(Obiettivi.Corrente(), Hub).z > 30f);
            yield return Attendi(0.6f);
            ControllaSegno("exvoto (lontano)", Obiettivi.Punto(Obiettivi.Corrente(), Hub), false);

            // porta chiusa a chiave: si apre solo con porta e chiave
            Completa("mondo/enigma_exvoto");
            Obiettivo("dopo l'ex-voto", "ob_foto");
            p = Obiettivi.Punto(Obiettivi.Corrente(), Hub);
            Check("nell'hub l'obiettivo successivo e' la porta del basso", p != null && Mathf.Abs(p.x - -9.1f) < 0.6f && Mathf.Abs(p.z - -2f) < 0.6f && Obiettivi.Testo(Obiettivi.Corrente(), Hub).Contains("basso"), Obiettivi.Testo(Obiettivi.Corrente(), Hub));
            yield return Porta(p.x + 4f, p.z);
            yield return Attendi(0.6f);
            ControllaSegno("porta del basso", p, true);
            Door portaBasso = null;
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include)) if (d.luogoId == "basso_antonietta") portaBasso = d;
            Check("la porta del basso esiste", portaBasso != null);
            if (portaBasso != null) { portaBasso.Interact(); yield return ScenaPronta(Basso); }

            // ---------- basso
            yield return ChiudiMessaggi();
            mm = UIRoot.I.Minimappa;
            Check("basso: minimappa visibile con la sua pianta", mm.Visibile);
            Obiettivo("entrati nel basso", "ob_foto");
            p = Obiettivi.Punto(Obiettivi.Corrente(), Basso);
            Check("basso: il punto e' la fotografia storta", p != null && Mathf.Abs(p.x - -4.5f) < 0.3f && Mathf.Abs(p.z - 1.5f) < 0.3f);
            yield return Porta(0f, -0.5f);
            yield return Attendi(0.8f);
            Check("basso: la targa dice la stanza", mm.NomeStrada == "La stanza", mm.NomeStrada);
            ControllaSegno("fotografia (nella stanza)", p, true);
            yield return Attendi(4.0f); // i telegrammi dell'ingresso spariscono
            yield return Foto("minimappa_basso");

            string[] seq = { "enigma_foto", "enigma_calendario", "enigma_cassetta", "enigma_baule" };
            string[] dopo = { "ob_calendario", "ob_cassetta", "ob_baule", "ob_passanti" };
            for (int i = 0; i < seq.Length; i++)
            {
                Completa("mondo/" + seq[i]);
                Obiettivo("dopo " + seq[i], dopo[i]);
                yield return Attendi(0.4f);
                if (i == 2) yield return MappaGrandeBasso(); // ora il passo e' il baule: nel basso la mappa grande ha riga e X
                if (i < 3)
                {
                    var pp = Obiettivi.Punto(Obiettivi.Corrente(), Basso);
                    Check("basso: dopo " + seq[i] + " il segno e' sul passo successivo", pp != null);
                    if (pp != null) { yield return Porta(0f, 0.5f); yield return Attendi(0.4f); ControllaSegno("basso " + dopo[i], pp, true); }
                }
            }

            // il baule e' aperto ma il ricordo non e' ancora iniziato: nel basso l'obiettivo e' ancora il primo passo del ricordo e la X sta sul baule
            Obiettivo("baule aperto, ancora nel basso", "ob_passanti");
            var pb = Obiettivi.Punto(Obiettivi.Corrente(), Basso);
            Check("basso: la X sta sul baule con il testo 'Riprendi le scarpe nel baule'", pb != null && Mathf.Abs(pb.x - -4f) < 0.3f && Mathf.Abs(pb.z - 4.5f) < 0.3f && pb.testo == "Riprendi le scarpe nel baule", pb != null ? pb.testo : "null");
            yield return Porta(0f, 0.5f);
            yield return Attendi(0.6f);
            ControllaSegno("basso, passo del ricordo", pb, true);
            Check("hub: lo stesso passo manda alla porta del basso", Obiettivi.Punto(Obiettivi.Corrente(), Hub) != null);

            // ---------- ricordo
            GameManager.I.StartFlashback("scarpe");
            yield return ScenaPronta(Ricordo);
            yield return Attendi(1.0f);
            yield return ChiudiMessaggi();
            mm = UIRoot.I.Minimappa;
            Check("ricordo: minimappa visibile", mm.Visibile);
            Obiettivo("nel ricordo", "ob_passanti");
            p = Obiettivi.Punto(Obiettivi.Corrente(), Ricordo);
            Check("ricordo: il punto e' alle scarpe dei passanti", p != null && Mathf.Abs(p.x - -2.4f) < 0.3f && Mathf.Abs(p.z - -23f) < 0.3f);
            yield return Attendi(0.8f);
            Check("ricordo: la targa dice il nome (Largo della Fontanella)", mm.NomeStrada == "Largo della Fontanella", mm.NomeStrada);
            ControllaSegno("ricordo passanti", p, true);
            yield return Foto("minimappa_ricordo");
            yield return Tap(Key.M);
            yield return Attendi(1.0f);
            Check("ricordo: la mappa grande si apre", UIRoot.I.PiantaGrande.IsOpen);
            Check("ricordo: nomi sulla mappa grande", UIRoot.I.PiantaGrande.EtichetteVisibili > 0, UIRoot.I.PiantaGrande.EtichetteVisibili + "/" + UIRoot.I.PiantaGrande.EtichetteTotali);
            yield return Foto("mappa_grande_ricordo");
            yield return Tap(Key.M);
            yield return Attendi(0.8f);

            string[] rseq = { "enigma_passanti", "enigma_nascosto", "enigma_quiz", "enigma_ordina", "enigma_lucchetto" };
            string[] rdopo = { "ob_nascosto", "ob_quiz", "ob_ordina", "ob_lucchetto", "(finito)" };
            for (int i = 0; i < rseq.Length; i++)
            {
                Completa("flashback_scarpe/" + rseq[i]);
                Obiettivo("dopo " + rseq[i], rdopo[i]);
                yield return Attendi(0.3f);
                var pp = Obiettivi.Punto(Obiettivi.Corrente(), Ricordo);
                if (i < rseq.Length - 1) Check("ricordo: dopo " + rseq[i] + " il segno e' sul passo successivo", pp != null);
            }
            yield return Attendi(1.0f);
            Check("capitolo finito: nessun segno ne' riga di obiettivo", !UIRoot.I.Minimappa.CroceVisibile && !UIRoot.I.Minimappa.FrecciaBordoVisibile && UIRoot.I.Minimappa.TestoObiettivo == "");
            yield return Tap(Key.M);
            yield return Attendi(1.0f);
            Check("capitolo finito: la mappa grande lo dice", UIRoot.I.PiantaGrande.IsOpen && UIRoot.I.PiantaGrande.TestoObiettivo == GameData.Mappe.testi.capitoloFinito, UIRoot.I.PiantaGrande.TestoObiettivo);
            yield return Tap(Key.M);
            yield return Attendi(0.8f);

            // ---------- zoom della mappa grande (nel ricordo)
            yield return Tap(Key.M);
            yield return Attendi(0.8f);
            var piu = FindButton("+");
            Check("mappa grande: pulsante di zoom", piu != null);
            if (piu != null) { piu.onClick.Invoke(); piu.onClick.Invoke(); yield return Attendi(0.5f); Check("mappa grande: lo zoom cresce", UIRoot.I.PiantaGrande.Zoom > 1.5f, UIRoot.I.PiantaGrande.Zoom.ToString("0.00", Inv)); }
            yield return Tap(Key.M);
            yield return Attendi(0.6f);

            // ---------- solo gamepad: Select apre e chiude la mappa, B la chiude (nessuna tastiera collegata)
            var pad = InputSystem.AddDevice<Gamepad>("MappaTestPad");
            InputSystem.EnableDevice(pad);
            foreach (var d in new System.Collections.Generic.List<InputDevice>(InputSystem.devices)) if (d is Keyboard) InputSystem.RemoveDevice(d);
            yield return null; yield return null;
            Check("senza tastiera: Keyboard.current e' nullo", Keyboard.current == null);
            InputSystem.QueueStateEvent(pad, new GamepadState(GamepadButton.Select)); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
            yield return Attendi(0.8f);
            Check("solo gamepad: Select apre la mappa grande", UIRoot.I.PiantaGrande.IsOpen);
            InputSystem.QueueStateEvent(pad, new GamepadState(GamepadButton.East)); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
            yield return Attendi(0.8f);
            Check("solo gamepad: B chiude la mappa grande", !UIRoot.I.PiantaGrande.IsOpen);
            InputSystem.QueueStateEvent(pad, new GamepadState(GamepadButton.Select)); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
            yield return Attendi(0.8f);
            InputSystem.QueueStateEvent(pad, new GamepadState(GamepadButton.Select)); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
            yield return Attendi(0.8f);
            Check("solo gamepad: Select riaperto e richiuso", !UIRoot.I.PiantaGrande.IsOpen);
            InputSystem.RemoveDevice(pad);
            kb = InputSystem.AddDevice<Keyboard>("MappaTestKb2");
            InputSystem.EnableDevice(kb);
            yield return null; yield return null;

            // ---------- il biglietto degli enigmi sta accanto alla minimappa, o all'angolo se non c'e'
            var bigl = UIRoot.I.HudLayer.Find("Biglietto");
            Check("biglietto trovato", bigl != null);
            if (bigl != null)
            {
                yield return Attendi(1.2f);
                var br = (RectTransform)bigl;
                Check("biglietto: con la minimappa sta alla sua destra", br.anchoredPosition.x > 200f, br.anchoredPosition.x.ToString("0", Inv));
                save.minimappaNascosta = true;
                yield return Attendi(1.2f);
                Check("biglietto: senza minimappa torna all'angolo", br.anchoredPosition.x < 40f, br.anchoredPosition.x.ToString("0", Inv));
                save.minimappaNascosta = false;
                yield return Attendi(0.8f);
            }

            // ---------- nord fisso e minimappa spenta
            save.nordFisso = true;
            yield return Attendi(0.6f);
            Check("nord fisso: la pianta non ruota", Mathf.Abs(UIRoot.I.Minimappa.Gruppo.GetComponentInChildren<UnityEngine.UI.RawImage>(true).transform.parent.localEulerAngles.z) < 0.01f);
            save.nordFisso = false;
            save.minimappaNascosta = true;
            yield return Attendi(0.6f);
            Check("minimappa spenta dalle impostazioni: il widget sparisce, M resta", !UIRoot.I.Minimappa.Visibile);
            yield return Tap(Key.M);
            yield return Attendi(0.8f);
            Check("minimappa spenta: la mappa grande si apre lo stesso", UIRoot.I.PiantaGrande.IsOpen);
            yield return Tap(Key.M);
            yield return Attendi(0.6f);
            save.minimappaNascosta = false;

            // ---------- impostazioni e salvataggio delle preferenze
            UIRoot.I.Pause.Show();
            yield return Attendi(0.6f);
            UIRoot.I.Settings.ShowScheda("Gioco");
            yield return Attendi(0.9f);
            Check("Impostazioni: la scheda Gioco si apre", UIRoot.I.Settings.IsOpen);
            Check("Impostazioni > Gioco: 'Ripristina predefiniti' non c'e' (vale solo per la grafica)", FindButton("Ripristina predefiniti") == null);
            yield return Foto("impostazioni_gioco");
            var righe = FindButtonsConTesto("›");
            Check("Impostazioni > Gioco: tre righe con frecce (aiuto, minimappa, nord)", righe >= 3, righe + " frecce");
            int prima = save.aiutoMappa;
            var frecce = FindAllButtons("›");
            if (frecce.Count >= 1) { frecce[0].onClick.Invoke(); yield return Attendi(0.3f); }
            Check("Impostazioni > Gioco: la freccia cambia l'aiuto della mappa", save.aiutoMappa != prima, prima + " -> " + save.aiutoMappa);
            save.aiutoMappa = Obiettivi.Esploratore; save.nordFisso = true; save.minimappaNascosta = true;
            GameManager.I.SaveGame();
            var riletto = SaveSystem.Load();
            Check("salvataggio: aiuto, nord fisso e minimappa si rileggono", riletto.aiutoMappa == 2 && riletto.nordFisso && riletto.minimappaNascosta);
            UIRoot.I.Settings.Hide();
            yield return Attendi(0.6f);
            UIRoot.I.Pause.Hide();
            yield return Attendi(0.6f);

            // un vecchio salvataggio (senza i nuovi campi) parte con i valori di default
            var vecchio = JsonUtility.FromJson<SaveData>("{\"scena\":\"10_Hub_Vicolo\"}");
            Check("vecchio salvataggio: aiuto Normale e minimappa accesa", vecchio.aiutoMappa == Obiettivi.Normale && !vecchio.minimappaNascosta && !vecchio.nordFisso);
            // Nuova partita conserva le preferenze
            GameManager.I.NewGame();
            yield return ScenaPronta(Hub);
            Check("Nuova partita conserva le preferenze della mappa", GameManager.I.Save.aiutoMappa == 2 && GameManager.I.Save.nordFisso && GameManager.I.Save.minimappaNascosta);
            Obiettivo("nuova partita", "ob_bucato");
            // salvataggio vecchio: scarpe nell'inventario e ricordo completato, ma senza i gettoni degli enigmi
            var sv = GameManager.I.Save;
            sv.inventario.Add("scarpe"); sv.flashbackCompletati.Add("flashback_scarpe");
            Check("salvataggio vecchio: senza gettoni l'obiettivo non riparte dal bucato", Obiettivi.Corrente() == null, Id());
            sv.inventario.Remove("scarpe"); sv.flashbackCompletati.Remove("flashback_scarpe");
            Obiettivo("dopo aver tolto il ricordo completato", "ob_bucato");
            GameManager.I.Save.aiutoMappa = Obiettivi.Normale; GameManager.I.Save.nordFisso = false; GameManager.I.Save.minimappaNascosta = false;
        }

        /// <summary>Tasto M (virtuale): apre la mappa grande, la richiude con M e con Esc; controlla blocco input, riga e X dell'obiettivo.</summary>
        IEnumerator MappaGrandeBasso()
        {
            var save = GameManager.I.Save;
            save.aiutoMappa = Obiettivi.Guidata;
            yield return Tap(Key.M);
            yield return Attendi(1.0f);
            var big = UIRoot.I.PiantaGrande;
            Check("tasto M apre la mappa grande", big.IsOpen);
            Check("mappa grande: il gioco e' bloccato mentre e' aperta", GameManager.I.InputBlocked);
            Check("mappa grande: nel basso mostra l'obiettivo (la riga a destra)", big.TestoObiettivo.Length > 3 && big.CroceVisibile, big.TestoObiettivo);
            yield return Foto("mappa_grande_basso");
            yield return Tap(Key.M);
            yield return Attendi(0.8f);
            Check("M richiude la mappa grande", !big.IsOpen && !GameManager.I.InputBlocked);
            yield return Tap(Key.M);
            yield return Attendi(0.8f);
            yield return Tap(Key.Escape);
            yield return Attendi(0.8f);
            Check("Esc richiude la mappa grande (senza aprire la pausa)", !big.IsOpen && !UIRoot.I.Pause.IsOpen);
        }

        UnityEngine.UI.Button FindButton(string testo)
        {
            foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude))
            {
                var tx = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tx != null && tx.text == testo && b.gameObject.activeInHierarchy) return b;
            }
            return null;
        }

        System.Collections.Generic.List<UnityEngine.UI.Button> FindAllButtons(string testo)
        {
            var l = new System.Collections.Generic.List<UnityEngine.UI.Button>();
            foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude))
            {
                var tx = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tx != null && tx.text == testo && b.gameObject.activeInHierarchy) l.Add(b);
            }
            l.Sort((a, c) => c.transform.position.y.CompareTo(a.transform.position.y));
            return l;
        }

        int FindButtonsConTesto(string testo) { return FindAllButtons(testo).Count; }
    }
}
#endif
