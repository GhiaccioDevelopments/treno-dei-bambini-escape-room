using System;
using System.Collections.Generic;
using System.Text;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Puzzles;
using TrenoBambini.UI;
using UnityEngine;

namespace TrenoBambini.Segreti
{
    /// <summary>
    /// R011: stato dei segreti (progressi in SaveData.segretiTrovati e segretiFlag, retrocompatibile) e regole:
    /// - nessun segreto blocca la storia (sono tutti facoltativi, non compaiono mai fra i requisiti di porte e capitoli);
    /// - il contatore compare solo nel menu pausa e nel quaderno, mai nell'HUD;
    /// - l'Indizio tace sui segreti finche' la stanza principale non e' completata, poi accenna soltanto che c'e' ancora qualcosa.
    /// Per i capitoli futuri (R012-R016): aggiungere le scene a segreti.json (stanze, segreti, pezzi, enigmi); nessun codice da toccare.
    /// </summary>
    public static class SegretiStato
    {
        static SaveData Save { get { return GameManager.I != null ? GameManager.I.Save : null; } }
        static SegretiFile F { get { return GameData.Segreti; } }
        static SegretiTesti T { get { return GameData.Segreti.testi; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Aggancia()
        {
            QuadernoSegreti.Provider = VociQuaderno;
        }

        // ------------------------------------------------------------------ progressi
        public static bool Trovato(string id)
        {
            var s = Save;
            return s != null && !string.IsNullOrEmpty(id) && s.segretiTrovati.Contains(id);
        }

        public static SegretoDef Def(string id)
        {
            var l = F.segreti;
            for (int i = 0; i < l.Count; i++) if (l[i].id == id) return l[i];
            return null;
        }

        /// <summary>Segna un segreto come trovato (idempotente). Ritorna true se e' nuovo. Salva e controlla il premio "tutti i segreti".</summary>
        public static bool Trova(string id)
        {
            var s = Save;
            if (s == null || string.IsNullOrEmpty(id) || s.segretiTrovati.Contains(id)) return false;
            s.segretiTrovati.Add(id);
            if (Totale() > 0 && Trovati() >= Totale() && !s.segretiFlag.Contains("segreti_tutti"))
                s.segretiFlag.Add("segreti_tutti"); // D1: R016 legge questo flag per la scena extra nel finale (vedi ImpostaFlag/Flag)
            GameManager.I.SaveGame();
            return true;
        }

        public static int Totale() { return F.segreti.Count; }
        public static int Trovati()
        {
            int n = 0;
            for (int i = 0; i < F.segreti.Count; i++) if (Trovato(F.segreti[i].id)) n++;
            return n;
        }
        public static int Totale(string scena) { int n = 0; foreach (var d in F.segreti) if (d.scena == scena) n++; return n; }
        public static int Trovati(string scena) { int n = 0; foreach (var d in F.segreti) if (d.scena == scena && Trovato(d.id)) n++; return n; }

        // ------------------------------------------------------------------ flag per gli agganci dei capitoli futuri
        public static bool Flag(string nome)
        {
            var s = Save;
            return s != null && s.segretiFlag.Contains(nome);
        }

        /// <summary>Aggancio per i capitoli futuri: R016 chiama ImpostaFlag("finale_visto") a fine gioco (violino lontano al gradino della chiesetta); R016 legge Flag("segreti_tutti") per la scena extra (D1).</summary>
        public static void ImpostaFlag(string nome)
        {
            var s = Save;
            if (s == null || string.IsNullOrEmpty(nome) || s.segretiFlag.Contains(nome)) return;
            s.segretiFlag.Add(nome);
            GameManager.I.SaveGame();
        }

        /// <summary>Condizione di un file JSON: "fb:id" capitolo completato, "ris:chiave" enigma risolto, "flag:nome", "segreto:id", altrimenti gettone di MondoStato.</summary>
        public static bool Condizione(string c)
        {
            if (string.IsNullOrEmpty(c)) return true;
            var s = Save;
            if (s == null) return false;
            if (c.StartsWith("fb:")) return s.flashbackCompletati.Contains(c.Substring(3));
            if (c.StartsWith("ris:")) return s.enigmiRisolti.Contains(c.Substring(4));
            if (c.StartsWith("flag:")) return s.segretiFlag.Contains(c.Substring(5));
            if (c.StartsWith("segreto:")) return s.segretiTrovati.Contains(c.Substring(8));
            return MondoStato.Ha(c);
        }

        public static bool Tutte(List<string> l)
        {
            if (l == null) return true;
            for (int i = 0; i < l.Count; i++) if (!Condizione(l[i])) return false;
            return true;
        }

        // ------------------------------------------------------------------ stanze e indizio
        public static StanzaSegretiDef Stanza(string scena)
        {
            foreach (var st in F.stanze) if (st.scena == scena) return st;
            return null;
        }

        /// <summary>La stanza principale e' completata quando valgono tutte le condizioni "completo" della stanza (segreti.json).</summary>
        public static bool StanzaCompleta(string scena)
        {
            var st = Stanza(scena);
            return st != null && st.completo.Count > 0 && Tutte(st.completo);
        }

        /// <summary>Premuto Indizio: a stanza principale completata, se resta un segreto, accenna soltanto che c'e' ancora qualcosa. Altrimenti tace.</summary>
        public static void AiutoLieve()
        {
            string scena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (UIRoot.I == null || string.IsNullOrEmpty(T.aiutoLieve)) return;
            if (!StanzaCompleta(scena)) return;
            if (Trovati(scena) >= Totale(scena)) return;
            UIRoot.I.Toast(T.aiutoLieve, 4f);
        }

        // ------------------------------------------------------------------ pagina Segreti del quaderno (R023) e contatore della pausa
        static string NomeStanza(string scena)
        {
            var st = Stanza(scena);
            return st != null && !string.IsNullOrEmpty(st.nome) ? st.nome : scena;
        }

        /// <summary>Una voce per stanza: "Basso 3/5", poi una casella per segreto (vuota = ancora da trovare, piena = nome e scheda).</summary>
        public static List<QuadernoVoce> VociQuaderno()
        {
            var res = new List<QuadernoVoce>();
            var scene = new List<string>();
            foreach (var st in F.stanze) if (!scene.Contains(st.scena)) scene.Add(st.scena);
            foreach (var d in F.segreti) if (!scene.Contains(d.scena)) scene.Add(d.scena);
            foreach (var sc in scene)
            {
                int tot = Totale(sc);
                if (tot == 0) continue;
                var sb = new StringBuilder();
                var righe = F.segreti.FindAll(d => d.scena == sc);
                righe.Sort((a, b) => a.ordine.CompareTo(b.ordine));
                foreach (var d in righe)
                {
                    if (Trovato(d.id))
                    {
                        sb.Append(T.casellaPiena).Append(' ').Append(d.nome);
                        if (!string.IsNullOrEmpty(d.scheda)) sb.Append(": ").Append(d.scheda);
                        if (d.tipo == "enigma" && !string.IsNullOrEmpty(d.ricompensa)) sb.Append("\n      <i>").Append(T.dietroQuinteTitolo).Append(":</i> ").Append(d.ricompensa);
                    }
                    else sb.Append(T.casellaVuota).Append(' ').Append(T.casellaIgnota);
                    sb.Append('\n');
                }
                res.Add(new QuadernoVoce { id = sc, titolo = string.Format(T.stanzaFmt, NomeStanza(sc), Trovati(sc), tot), testo = sb.ToString().TrimEnd() });
            }
            return res;
        }

        /// <summary>Riga del menu pausa (mai nell'HUD).</summary>
        public static string RigaPausa()
        {
            if (Totale() == 0) return "";
            return T.contatoreEtichetta + ": " + string.Format(T.contatoreFmt, Trovati(), Totale());
        }
    }
}
