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
    /// Gestisce la stanza-ricordo: crea gli oggetti-enigma (R009: oggetti d'epoca nel mondo, niente piu' marcatori), mostra l'HUD "Enigmi: n/N"
    /// e apre l'uscita quando tutti sono risolti. Gli enigmi risolti sono salvati; l'ordine lo danno i campi richiede/produce.
    /// </summary>
    public class EscapeRoomController : MonoBehaviour
    {
        public string flashbackId;

        FlashbackDef flashback;
        readonly List<EnigmaMondo> stations = new List<EnigmaMondo>();
        readonly List<string> altrove = new List<string>(); // R014: chiavi degli enigmi dello stesso ricordo che stanno in un'altra scena (stazione e vagone)
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
            string scena = gameObject.scene.name;
            for (int i = 0; i < enigmi.Count; i++)
            {
                // R014: un ricordo su piu' scene: ogni enigma con "scena" nasce solo nella sua; gli altri contano per il totale e per l'uscita
                if (!string.IsNullOrEmpty(enigmi[i].scena) && enigmi[i].scena != scena) { altrove.Add(Key(enigmi[i])); continue; }
                var st = EnigmaMondo.Crea(enigmi[i], Key(enigmi[i]), root);
                st.alRisolto = OnEnigmaSolved;
                st.progresso = SetExtraHud;
                stations.Add(st);
                MondoSpawner.Sostituisci(gameObject.scene, enigmi[i].sostituisce);
            }

            int solvedCount = CountSolved();
            RefreshAll();

            // attende un frame: fader e SceneLoader finiscono di sistemare la scena
            yield return null;
            if (solvedCount == 0 && UIRoot.I != null && !TrenoBambini.Dialogue.FilmatoIniziale.InCorso && !string.IsNullOrEmpty(flashback.introduzione))
                UIRoot.I.ShowMessage(flashback.titolo, flashback.introduzione, null, "nar_" + flashback.oggetto + "_001"); // R028: voce del Narratore
        }

        void OnDestroy()
        {
            if (UIRoot.I != null) UIRoot.I.SetHudCounter("");
        }

        string Key(EnigmaDef e) { return flashbackId + "/" + e.id; }

        /// <summary>Posizione (0..n-1) dell enigma nel flashback, -1 se sconosciuto. Serve al numero romano nel titolo del pannello.</summary>
        public int IndexOfEnigma(EnigmaDef d)
        {
            // R014: posizione nel ricordo intero (non solo fra gli enigmi di questa scena)
            if (flashback != null && flashback.enigmi != null)
                for (int i = 0; i < flashback.enigmi.Count; i++) if (flashback.enigmi[i] == d || flashback.enigmi[i].id == d.id) return i;
            for (int i = 0; i < stations.Count; i++) if (stations[i].def == d || stations[i].def.id == d.id) return i;
            return -1;
        }

        /// <summary>Enigmi del ricordo: quelli di questa scena e (R014) quelli delle altre scene dello stesso ricordo.</summary>
        int Totale { get { return stations.Count + altrove.Count; } }

        int CountSolved()
        {
            int c = 0;
            for (int i = 0; i < stations.Count; i++) if (stations[i].Risolto) c++;
            for (int i = 0; i < altrove.Count; i++) if (MondoStato.Risolto(altrove[i])) c++;
            return c;
        }

        void RefreshAll()
        {
            int solved = CountSolved();
            for (int i = 0; i < stations.Count; i++) stations[i].Aggiorna();
            bool all = solved >= Totale;
            if (exit != null) exit.Setup(flashback, all);
            UpdateHud(solved);
        }

        void UpdateHud(int solved)
        {
            if (UIRoot.I == null) return;
            UIRoot.I.SetHudProgress(solved, Totale, extraHud);
        }

        /// <summary>Testo extra nell'HUD (es. progressi dell'enigma "nascosto"). Vuoto = nessuno.</summary>
        public void SetExtraHud(string text)
        {
            extraHud = text ?? "";
            UpdateHud(CountSolved());
        }

        public void OnEnigmaSolved(EnigmaMondo st)
        {
            extraHud = "";
            RefreshAll();

            if (UIRoot.I == null) return;
            if (CountSolved() >= Totale)
                UIRoot.I.Toast("Hai risolto tutti gli enigmi! Ora puoi uscire dalla porta.", 4f);
        }
    }
}
