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

            var ui = new GameObject("UIRoot", typeof(RectTransform));
            ui.transform.SetParent(transform, false);
            ui.AddComponent<UIRoot>();
        }

        void OnDestroy() { if (I == this) I = null; }

        public static bool IsMenuScene(string sceneName)
        {
            return sceneName == SceneBoot || sceneName == SceneTitolo;
        }

        // ---------- blocco input ----------
        public void PushBlock() { blockers++; }

        public void PopBlock()
        {
            if (blockers > 0) blockers--;
            LastUnblockTime = Time.unscaledTime;
        }

        public void ResetBlocks()
        {
            blockers = 0;
            LastUnblockTime = Time.unscaledTime;
        }

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
            }
            save.scena = SceneHub;
            save.spawn = "inizio";
            SaveSystem.Save(save);
            ResetBlocks();
            SceneLoader.I.Load(SceneHub, "inizio");
        }

        public void Continue()
        {
            var s = Save;
            string scena = string.IsNullOrEmpty(s.scena) ? SceneHub : s.scena;
            string spawn = string.IsNullOrEmpty(s.spawn) ? "inizio" : s.spawn;
            if (!Application.CanStreamedLevelBeLoaded(scena)) { scena = SceneHub; spawn = "inizio"; }
            ResetBlocks();
            SceneLoader.I.Load(scena, spawn);
        }

        /// <summary>Chiamato dallo SceneLoader a scena caricata: memorizza dove si trova il giocatore.</summary>
        public void OnSceneEntered(string scena, string spawn)
        {
            if (IsMenuScene(scena)) return;
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
