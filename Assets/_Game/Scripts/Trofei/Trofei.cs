using System;
using System.Collections.Generic;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Puzzles;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Album
{
    /// <summary>
    /// R027: i trofei (album di figurine d'epoca). Dati in Resources/Data/trofei.json (generato da ArtSource/Trofei/genera_trofei.py); stato in SaveData.trofei
    /// (id sbloccati) e SaveData.trofeiEventi (eventi registrati dal gioco, es. "senza_indizi:&lt;enigma&gt;", "errore:&lt;scena&gt;", "scheda:&lt;enigma&gt;", "scena:&lt;nome&gt;").
    /// Condizione = token separati da " + " (tutti veri):
    ///   fb:&lt;flashback&gt; completato · ris:&lt;chiave enigma&gt; risolto · gettone:&lt;id&gt; (MondoStato) · inv:&lt;oggetto&gt; · flag:&lt;flag&gt; (SegretiStato) ·
    ///   segreto:&lt;id&gt; trovato · segreti:&lt;N&gt; trovati almeno N · cutscene:&lt;id#capitolo&gt; vista · filmato (filmato iniziale visto) ·
    ///   ev:&lt;evento&gt; registrato · conta:&lt;prefisso&gt;:&lt;N&gt; almeno N eventi distinti con quel prefisso · no:&lt;prefisso&gt; nessun evento con quel prefisso.
    /// Un monitor controlla una volta al secondo e allo sblocco mostra una notifica discreta con un suono breve. Salvataggi vecchi: liste vuote.
    /// </summary>
    public static class Trofei
    {
        static SaveData Save { get { return GameManager.I != null ? GameManager.I.Save : null; } }

        /// <summary>Registra un evento (una volta sola). Chiamato da EsaminaView e dal monitor.</summary>
        public static void Evento(string e)
        {
            var s = Save;
            if (s == null || string.IsNullOrEmpty(e) || s.trofeiEventi.Contains(e)) return;
            s.trofeiEventi.Add(e);
        }

        public static bool Sbloccato(string id) { var s = Save; return s != null && s.trofei.Contains(id); }

        public static int Conta(string prefisso)
        {
            var s = Save; int n = 0;
            if (s != null) foreach (var e in s.trofeiEventi) if (e.StartsWith(prefisso)) n++;
            return n;
        }

        static readonly Dictionary<string, string[]> tokenCache = new Dictionary<string, string[]>();

        public static bool Vera(string condizione)
        {
            var s = Save;
            if (s == null || string.IsNullOrEmpty(condizione)) return false;
            string[] tok;
            if (!tokenCache.TryGetValue(condizione, out tok)) // R029: la condizione si divide una volta sola (il monitor la controlla ogni secondo)
            {
                var l = new List<string>();
                foreach (var raw in condizione.Split('+')) { string t = raw.Trim(); if (t.Length > 0) l.Add(t); }
                tok = l.ToArray();
                tokenCache[condizione] = tok;
            }
            for (int i = 0; i < tok.Length; i++) if (!Token(s, tok[i])) return false;
            return true;
        }

        static bool Token(SaveData s, string t)
        {
            int c = t.IndexOf(':');
            string k = c < 0 ? t : t.Substring(0, c), v = c < 0 ? "" : t.Substring(c + 1);
            switch (k)
            {
                case "fb": return s.flashbackCompletati.Contains(v);
                case "ris": return MondoStato.Risolto(v);
                case "gettone": return MondoStato.Ha(v);
                case "inv": return s.inventario.Contains(v);
                case "flag": return s.segretiFlag.Contains(v);
                case "segreto": return s.segretiTrovati.Contains(v);
                case "segreti": { int n; return int.TryParse(v, out n) && s.segretiTrovati.Count >= n; }
                case "cutscene": return s.cutsceneViste.Contains(v);
                case "filmato": return s.filmatoVisto;
                case "ev": return s.trofeiEventi.Contains(v);
                case "no": return Conta(v) == 0;
                case "conta":
                {
                    int u = v.LastIndexOf(':'); int n;
                    return u > 0 && int.TryParse(v.Substring(u + 1), out n) && Conta(v.Substring(0, u)) >= n;
                }
            }
            Debug.LogWarning("[Trofei] Condizione sconosciuta: " + t);
            return false;
        }

        static readonly string[] Chiavi = { "fb", "ris", "gettone", "inv", "flag", "segreto", "segreti", "cutscene", "filmato", "ev", "no", "conta" };

        /// <summary>Per le prove: ogni token della condizione e' conosciuto (nessuna condizione scritta male, quindi impossibile).</summary>
        public static bool CondizioneValida(string condizione)
        {
            if (string.IsNullOrEmpty(condizione)) return false;
            foreach (var raw in condizione.Split('+'))
            {
                string t = raw.Trim(); int c = t.IndexOf(':');
                if (System.Array.IndexOf(Chiavi, c < 0 ? t : t.Substring(0, c)) < 0) return false;
            }
            return true;
        }

        /// <summary>Controlla tutti i trofei non ancora sbloccati; sblocca quelli veri. Ritorna quanti ne ha sbloccati. avvisa = notifica a schermo.</summary>
        public static int Controlla(bool avvisa = true)
        {
            var s = Save;
            if (s == null) return 0;
            int n = 0;
            foreach (var t in GameData.Trofei.trofei)
            {
                if (s.trofei.Contains(t.id) || !Vera(t.condizione)) continue;
                s.trofei.Add(t.id);
                n++;
                if (avvisa && UIRoot.I != null)
                {
                    UIRoot.I.Toast(string.Format(GameData.Trofei.testi.notifica, t.titolo), 4f);
                    UISfx.Play(Sfx.Bell, 0.45f);
                }
            }
            if (n > 0) GameManager.I.SaveGame();
            return n;
        }
    }

    /// <summary>R027: controlla i trofei una volta al secondo e registra le scene visitate. Creato all'avvio, sopravvive ai cambi di scena.</summary>
    public class TrofeiMonitor : MonoBehaviour
    {
        float t;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            var go = new GameObject("TrofeiMonitor");
            DontDestroyOnLoad(go);
            go.AddComponent<TrofeiMonitor>();
        }

        void OnEnable() { SceneManager.sceneLoaded += Caricata; }
        void OnDisable() { SceneManager.sceneLoaded -= Caricata; }

        void Caricata(Scene sc, LoadSceneMode m)
        {
            if (sc.name.StartsWith("0")) return; // boot e titolo non contano
            Trofei.Evento("scena:" + sc.name);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t < 1f) return;
            t = 0f;
            if (GameManager.I == null || SceneManager.GetActiveScene().name.StartsWith("0")) return;
            Trofei.Controlla();
        }
    }
}
