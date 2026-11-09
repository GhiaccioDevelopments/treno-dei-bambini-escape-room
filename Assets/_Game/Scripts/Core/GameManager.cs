using TrenoBambini.Data;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Core
{
    /// <summary>
    /// Singleton globale auto-creato prima di ogni scena: salvataggio, blocco input, flusso dei flashback.
    /// Crea UIRoot, SceneLoader e AudioManager.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string SceneHub = "10_Hub_Vicolo";
        public const string SceneTitolo = "01_Titolo";
        public const string SceneBoot = "00_Boot";

        public static GameManager I { get; private set; }

        SaveData save;
        int blockers;
        readonly System.Collections.Generic.List<string> blockReasons = new System.Collections.Generic.List<string>();
        float orphanSince = -1f;

        public SaveData Save { get { if (save == null) save = SaveSystem.Load(); return save; } }
        public bool HasSave { get { return SaveSystem.Exists(); } }
        public int InputBlockers { get { return blockers; } }
        public bool InputBlocked { get { return blockers > 0; } }
        /// <summary>Tempo (unscaled) dell'ultimo sblocco input: serve a non riusare lo stesso tasto/click.</summary>
        public float LastUnblockTime { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            if (I != null) return;
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<SceneLoader>();
            gameObject.AddComponent<DiagOverlay>();
            gameObject.AddComponent<CursorManager>();

            var ui = new GameObject("UIRoot", typeof(RectTransform));
            ui.transform.SetParent(transform, false);
            ui.AddComponent<UIRoot>();
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>R023: vero mentre il menu iniziale mostra il vicolo dell'hub come sfondo (la scena hub e' caricata in additivo, ma e' ancora il menu).</summary>
        public static bool MenuBackdrop;

        public static bool IsMenuScene(string sceneName)
        {
            return sceneName == SceneBoot || sceneName == SceneTitolo;
        }

        // ---------- blocco input ----------
        /// <summary>Chiede un blocco dell'input; il motivo serve solo alla diagnosi (F3).</summary>
        public void PushBlock(string motivo = "?")
        {
            blockers++;
            blockReasons.Add(motivo ?? "?");
        }

        public void PopBlock(string motivo = null)
        {
            if (blockers > 0)
            {
                blockers--;
                int i = motivo != null ? blockReasons.LastIndexOf(motivo) : -1;
                if (i < 0) i = blockReasons.Count - 1;
                if (i >= 0) blockReasons.RemoveAt(i);
            }
            LastUnblockTime = Time.unscaledTime;
        }

        public void ResetBlocks()
        {
            blockers = 0;
            blockReasons.Clear();
            orphanSince = -1f;
            LastUnblockTime = Time.unscaledTime;
        }

        /// <summary>Motivi dei blocchi attivi (diagnosi).</summary>
        public string BlockReasons { get { return string.Join(",", blockReasons); } }

        /// <summary>
        /// Rete di sicurezza: se c'è un blocco ma nessun proprietario noto è attivo (caricamento, dialogo/puzzle, pausa,
        /// inventario, cutscene, dissolvenza) per oltre 1,5 s, il blocco è rimasto "appeso" e lo azzero.
        /// </summary>
        void Update()
        {
            ContaTempo();
            if (blockers <= 0) { orphanSince = -1f; return; }
            var ui = UIRoot.I;
            bool owner = (SceneLoader.I != null && SceneLoader.I.IsLoading)
                || ui == null || ui.ModalOpen
                || (ui.Pause != null && ui.Pause.IsOpen)
                || (ui.Inventory != null && ui.Inventory.IsOpen)
                || (ui.Cutscene != null && ui.Cutscene.IsPlaying)
                || TrenoBambini.Dialogue.FilmatoIniziale.InCorso
                || (ui.Fader != null && ui.Fader.Busy);
            if (owner) { orphanSince = -1f; return; }
            if (orphanSince < 0f) { orphanSince = Time.unscaledTime; return; }
            if (Time.unscaledTime - orphanSince > 1.5f)
            {
                Debug.LogWarning("Blocco input orfano azzerato: " + BlockReasons);
                ResetBlocks();
            }
        }

        // ---------- tempo di gioco ----------
        float ultimoSalvataggioTempo;

        /// <summary>Tempo giocato: conta solo in partita (non nei menu, non con pausa, inventario o impostazioni aperti) e lo salva ogni 30 s.</summary>
        void ContaTempo()
        {
            if (MenuBackdrop || IsMenuScene(SceneManager.GetActiveScene().name) || !SaveSystem.Exists()) return;
            var ui = UIRoot.I;
            bool inPausa = ui != null && ((ui.Pause != null && ui.Pause.IsOpen) || (ui.Inventory != null && ui.Inventory.IsOpen) || (ui.Settings != null && ui.Settings.IsOpen));
            if (inPausa || Time.timeScale <= 0f) return;
            Save.tempoGioco += Time.unscaledDeltaTime;
            if (Time.unscaledTime - ultimoSalvataggioTempo > 30f) SalvaTempo();
        }

        /// <summary>Scrive il salvataggio solo se esiste gia' e si e' in partita (dal titolo non si crea un salvataggio vuoto).</summary>
        void SalvaTempo()
        {
            ultimoSalvataggioTempo = Time.unscaledTime;
            if (save == null || MenuBackdrop || IsMenuScene(SceneManager.GetActiveScene().name) || !SaveSystem.Exists()) return;
            SaveSystem.Save(save);
        }

        // chiusura del programma, app in secondo piano (mobile) o scheda del browser nascosta (WebGL): il tempo non va perso
        void OnApplicationQuit() { SalvaTempo(); }
        void OnApplicationPause(bool inPausa) { if (inPausa) SalvaTempo(); }
        void OnApplicationFocus(bool fuoco) { if (!fuoco) SalvaTempo(); }

        // ---------- salvataggio ----------
        public void SaveGame() { SaveSystem.Save(Save); }

        public void NewGame()
        {
            var vecchio = Save;
            save = new SaveData();
            save.volume = AudioListener.volume;
            if (vecchio != null)
            {
                save.volMusica = vecchio.volMusica; save.volAmbiente = vecchio.volAmbiente;
                save.volEffetti = vecchio.volEffetti; save.volVoci = vecchio.volVoci;
                save.effettiRidotti = vecchio.effettiRidotti;
                save.visualeTastoDestro = vecchio.visualeTastoDestro;
                save.enigmiSemplificati = vecchio.enigmiSemplificati; save.evidenziaInterattivi = vecchio.evidenziaInterattivi;
                save.aiutoMappa = vecchio.aiutoMappa; save.minimappaNascosta = vecchio.minimappaNascosta; save.nordFisso = vecchio.nordFisso;
            }
            save.scena = SceneHub;
            save.spawn = "inizio";
            SaveSystem.Save(save);
            ResetBlocks();
            // R025: alla nuova partita parte il filmato iniziale (salvabile e saltabile); finito, il giocatore e' nell'hub con il primo obiettivo
            var ui = UIRoot.I;
            if (ui != null && ui.Filmato != null && !ui.Filmato.IsPlaying && FilmatoDisponibile())
                ui.Filmato.Avvia(false, null);
            else
                SceneLoader.I.Load(SceneHub, "inizio");
        }

        /// <summary>R025: il filmato iniziale esiste (copione e regia) ed e' attivo (flag -senzafilmato nelle prove di prestazioni lo spegne).</summary>
        public static bool FilmatoDisponibile()
        {
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-senzafilmato") return false;
            var d = GameData.Cutscene("intro");
            var f = GameData.Filmato;
            if (d == null || d.battute == null || d.battute.Count == 0 || f.capitoli.Count == 0) return false;
            // ogni capitolo deve avere una scena, almeno un'inquadratura e almeno una battuta; ogni inquadratura (dopo la prima) cita una battuta che esiste
            for (int i = 0; i < f.capitoli.Count; i++)
            {
                var c = f.capitoli[i];
                if (string.IsNullOrEmpty(c.scena) || c.inquadrature == null || c.inquadrature.Count == 0 || i >= d.capitoli.Count) return false;
                bool haBattute = false;
                foreach (var b in d.battute) if (b != null && b.capitolo == d.capitoli[i].id) { haBattute = true; break; }
                if (!haBattute) return false;
                for (int k = 1; k < c.inquadrature.Count; k++)
                {
                    bool trovata = false;
                    foreach (var b in d.battute) if (b != null && b.id == c.inquadrature[k].da) { trovata = true; break; }
                    if (!trovata) return false;
                }
            }
            return true;
        }

        /// <summary>R025: rivedi il filmato iniziale dalla pausa (o altrove). Alla fine il giocatore torna dov'era.</summary>
        public void RivediFilmato()
        {
            var ui = UIRoot.I;
            if (ui == null || ui.Filmato == null || ui.Filmato.IsPlaying || !FilmatoDisponibile()) return;
            ui.Filmato.Avvia(true, null);
        }

        public void Continue()
        {
            var s = Save;
            string scena = string.IsNullOrEmpty(s.scena) ? SceneHub : s.scena;
            string spawn = string.IsNullOrEmpty(s.spawn) ? "inizio" : s.spawn;
            if (!Application.CanStreamedLevelBeLoaded(scena)) { scena = SceneHub; spawn = "inizio"; }
            ResetBlocks();
            // R025: un salvataggio senza filmato visto e senza alcun progresso (partita iniziata prima del filmato o chiusa subito) rivede l'introduzione
            if (!s.filmatoVisto && SenzaProgressi(s) && UIRoot.I != null && UIRoot.I.Filmato != null && !UIRoot.I.Filmato.IsPlaying && FilmatoDisponibile())
            { UIRoot.I.Filmato.Avvia(false, null); return; }
            SceneLoader.I.Load(scena, spawn);
        }

        static bool SenzaProgressi(SaveData s)
        {
            return s.inventario.Count == 0 && s.flashbackCompletati.Count == 0 && s.enigmiRisolti.Count == 0 && s.oggettiEnigma.Count == 0 && s.cutsceneViste.Count == 0;
        }

        /// <summary>Chiamato dallo SceneLoader a scena caricata: memorizza dove si trova il giocatore.</summary>
        public void OnSceneEntered(string scena, string spawn)
        {
            if (IsMenuScene(scena)) return;
            if (TrenoBambini.Dialogue.FilmatoIniziale.InCorso) return; // R025: il filmato (o "Rivedi") non deve mai lasciare il salvataggio in un ricordo
            var s = Save;
            s.scena = scena;
            if (!string.IsNullOrEmpty(spawn)) s.spawn = spawn;
            SaveGame();
        }

        // ---------- flashback ----------
        public void StartFlashback(string oggettoId)
        {
            var o = GameData.Oggetto(oggettoId);
            FlashbackDef f = o != null ? GameData.Flashback(o.flashback) : GameData.Flashback("flashback_" + oggettoId);
            if (f == null || string.IsNullOrEmpty(f.scena))
            {
                Debug.LogWarning("Flashback non disponibile per l'oggetto: " + oggettoId);
                if (UIRoot.I != null) UIRoot.I.Toast("Questo ricordo non è ancora disponibile.");
                return;
            }
            var s = Save;
            if (string.IsNullOrEmpty(s.ritornoScena))
            {
                s.ritornoScena = SceneManager.GetActiveScene().name;
                s.ritornoSpawn = "oggetto_" + oggettoId;
            }
            SaveGame();
            SceneLoader.I.Load(f.scena, "inizio", TransitionStyle.Ricordo);
        }

        public void CompleteFlashback(string oggettoId)
        {
            var s = Save;
            var o = GameData.Oggetto(oggettoId);
            if (!s.inventario.Contains(oggettoId)) s.inventario.Add(oggettoId);
            if (o != null && !string.IsNullOrEmpty(o.flashback) && !s.flashbackCompletati.Contains(o.flashback))
                s.flashbackCompletati.Add(o.flashback);
            SaveGame();
            if (UIRoot.I != null) UIRoot.I.QueueNewItem(oggettoId); // animazione "nuovo oggetto" al ritorno nel vicolo

            string cutsceneId = o != null ? o.cutscene : null;
            if (!string.IsNullOrEmpty(cutsceneId) && UIRoot.I != null && UIRoot.I.Cutscene != null)
                UIRoot.I.Cutscene.Play(cutsceneId, ReturnFromFlashback);
            else
                ReturnFromFlashback();
        }

        void ReturnFromFlashback()
        {
            var s = Save;
            string scena = string.IsNullOrEmpty(s.ritornoScena) ? SceneHub : s.ritornoScena;
            string spawn = s.ritornoSpawn;
            s.ritornoScena = "";
            s.ritornoSpawn = "";
            s.scena = scena; s.spawn = spawn ?? "";
            SaveGame();
            SceneLoader.I.Load(scena, spawn, TransitionStyle.Ricordo);
        }
    }
}
