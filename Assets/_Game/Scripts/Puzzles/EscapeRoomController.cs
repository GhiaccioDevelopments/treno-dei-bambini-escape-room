using System.Collections;
using System.Collections.Generic;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// Gestisce la stanza-ricordo: crea le postazioni degli enigmi, attiva la prima non risolta,
    /// mostra l'HUD "Enigmi: n/N" e apre l'uscita alla fine. Gli enigmi risolti sono salvati.
    /// </summary>
    public class EscapeRoomController : MonoBehaviour
    {
        public string flashbackId;

        FlashbackDef flashback;
        readonly List<PuzzleStation> stations = new List<PuzzleStation>();
        EscapeExit exit;
        string extraHud = "";

        IEnumerator Start()
        {
            flashback = GameData.Flashback(flashbackId);
            if (flashback == null)
            {
                Debug.LogWarning("EscapeRoomController: flashback non trovato (" + flashbackId + ")");
                yield break;
            }

            exit = FindAnyObjectByType<EscapeExit>();
            if (exit == null) Debug.LogWarning("EscapeRoomController: nessun EscapeExit nella scena.");

            var root = new GameObject("Enigmi").transform;
            root.SetParent(transform, false);
            var enigmi = flashback.enigmi ?? new List<EnigmaDef>();
            for (int i = 0; i < enigmi.Count; i++) CreateStation(enigmi[i], root);

            int solvedCount = CountSolved();
            RefreshAll();

            // attende un frame: fader e SceneLoader finiscono di sistemare la scena
            yield return null;
            if (solvedCount == 0 && UIRoot.I != null && !string.IsNullOrEmpty(flashback.introduzione))
                UIRoot.I.ShowMessage(flashback.titolo, flashback.introduzione, null);
        }

        void OnDestroy()
        {
            if (UIRoot.I != null) UIRoot.I.SetHudCounter("");
        }

        void CreateStation(EnigmaDef e, Transform parent)
        {
            string modello = e.modello;
            if (string.IsNullOrEmpty(modello))
            {
                switch ((e.tipo ?? "").ToLowerInvariant())
                {
                    case "lucchetto": modello = "cilindro"; break;
                    case "abbina": modello = "sfera"; break;
                    default: modello = "cubo"; break;
                }
            }
            var go = PrimitiveFactory.Create(modello, "Postazione_" + e.id, new Vector3(e.x, e.y, e.z), 0.6f, StationColor(e.tipo), parent);
            var st = go.AddComponent<PuzzleStation>();
            var glow = go.AddComponent<Glow>();
            glow.on = false;
            st.Setup(e, this, glow);
            stations.Add(st);
        }

        static Color StationColor(string tipo)
        {
            switch ((tipo ?? "").ToLowerInvariant())
            {
                case "quiz": return new Color(0.62f, 0.42f, 0.25f);
                case "lucchetto": return new Color(0.45f, 0.45f, 0.5f);
                case "nascosto": return new Color(0.5f, 0.55f, 0.35f);
                case "ordina": return new Color(0.6f, 0.35f, 0.3f);
                case "abbina": return new Color(0.4f, 0.5f, 0.6f);
                default: return new Color(0.6f, 0.5f, 0.4f);
            }
        }

        string Key(EnigmaDef e) { return flashbackId + "/" + e.id; }

        bool IsSolved(EnigmaDef e) { return GameManager.I.Save.enigmiRisolti.Contains(Key(e)); }

        /// <summary>Posizione (0..n-1) dell enigma nel flashback, -1 se sconosciuto. Serve al numero romano nel titolo del pannello.</summary>
        public int IndexOfEnigma(EnigmaDef d)
        {
            for (int i = 0; i < stations.Count; i++) if (stations[i].def == d) return i;
            return -1;
        }

        int CountSolved()
        {
            int c = 0;
            for (int i = 0; i < stations.Count; i++) if (IsSolved(stations[i].def)) c++;
            return c;
        }

        void RefreshAll()
        {
            bool activeAssigned = false;
            int solved = 0;
            for (int i = 0; i < stations.Count; i++)
            {
                bool s = IsSolved(stations[i].def);
                bool active = false;
                if (s) solved++;
                else if (!activeAssigned) { active = true; activeAssigned = true; }
                stations[i].Refresh(active, s);
            }
            bool all = solved >= stations.Count;
            if (exit != null) exit.Setup(flashback, all);
            UpdateHud(solved);
        }

        void UpdateHud(int solved)
        {
            if (UIRoot.I == null) return;
            UIRoot.I.SetHudProgress(solved, stations.Count, extraHud);
        }

        /// <summary>Testo extra nell'HUD (es. progressi dell'enigma "nascosto"). Vuoto = nessuno.</summary>
        public void SetExtraHud(string text)
        {
            extraHud = text ?? "";
            UpdateHud(CountSolved());
        }

        public void OnEnigmaSolved(PuzzleStation st)
        {
            extraHud = "";
            var key = Key(st.def);
            var s = GameManager.I.Save;
            if (!s.enigmiRisolti.Contains(key)) s.enigmiRisolti.Add(key);
            GameManager.I.SaveGame();
            RefreshAll();

            if (UIRoot.I == null) return;
            if (CountSolved() >= stations.Count)
                UIRoot.I.Toast("Hai risolto tutti gli enigmi! Ora puoi uscire dalla porta.", 4f);
        }
    }
}
