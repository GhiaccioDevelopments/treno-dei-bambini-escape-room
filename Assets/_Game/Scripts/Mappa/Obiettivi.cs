using System;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Puzzles;
using UnityEngine;

namespace TrenoBambini.Mappa
{
    /// <summary>
    /// R019: il prossimo obiettivo. Viene dallo stato del gioco, non da una scena: e' il primo passo di mappe.json (obiettivi) i cui gettoni
    /// "completo" non sono ancora tutti nelle mani del giocatore (MondoStato: enigmi risolti, oggetti e indizi ottenuti). Ogni passo dice
    /// dove andare scena per scena; se si e' nella scena sbagliata indica la porta da prendere. Nessun testo e' nel codice.
    /// Modalita' di aiuto (SaveData.aiutoMappa): 0 Guidata (sempre), 1 Normale (dopo qualche minuto senza progressi o con Indizio), 2 Esploratore (mai).
    /// </summary>
    public static class Obiettivi
    {
        public const int Guidata = 0, Normale = 1, Esploratore = 2;

        static int firma = -1;
        static float fermoSecondi;
        static bool rivelato;
        static float forzatoFino = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { firma = -1; fermoSecondi = 0f; rivelato = false; forzatoFino = -1f; }

        static SaveData Save { get { return GameManager.I != null ? GameManager.I.Save : null; } }

        public static int Modo
        {
            get { var s = Save; return s == null ? Normale : Mathf.Clamp(s.aiutoMappa, Guidata, Esploratore); }
        }

        /// <summary>Il primo passo non ancora completato, o null se il capitolo e' finito.</summary>
        public static ObiettivoDef Corrente()
        {
            var l = GameData.Mappe.obiettivi;
            for (int i = 0; i < l.Count; i++)
            {
                if (ChiusoDalCapitolo(l[i]) || Completo(l[i])) continue;
                // finiti gli enigmi di un capitolo, il passo dopo e' uscire dal ricordo (capitolo finito): il capitolo seguente parte solo quando questo e' rivissuto
                if (i > 0 && l[i - 1].capitolo != l[i].capitolo && !string.IsNullOrEmpty(l[i - 1].capitolo) && !ChiusoDalCapitolo(l[i - 1])) return null;
                return l[i];
            }
            return null;
        }

        /// <summary>Il capitolo del passo e' gia' rivissuto (anche in un salvataggio vecchio, senza i gettoni degli enigmi): il passo non e' piu' un obiettivo.</summary>
        public static bool ChiusoDalCapitolo(ObiettivoDef o)
        {
            if (o == null || string.IsNullOrEmpty(o.capitolo)) return false;
            var s = Save;
            return s != null && s.flashbackCompletati != null && s.flashbackCompletati.Contains(o.capitolo);
        }

        public static bool Completo(ObiettivoDef o)
        {
            if (o == null || o.completo == null) return true;
            for (int i = 0; i < o.completo.Count; i++) if (!MondoStato.Ha(o.completo[i])) return false;
            return true;
        }

        /// <summary>Il punto del passo nella scena indicata (null se il passo non ha niente da mostrare li').</summary>
        public static ObiettivoPuntoDef Punto(ObiettivoDef o, string scena)
        {
            if (o == null || o.punti == null) return null;
            for (int i = 0; i < o.punti.Count; i++) if (o.punti[i].scena == scena) return o.punti[i];
            return null;
        }

        /// <summary>Riga breve sotto la mappa: il testo del punto in questa scena, altrimenti quello generale del passo.</summary>
        public static string Testo(ObiettivoDef o, string scena)
        {
            if (o == null) return "";
            var p = Punto(o, scena);
            if (p != null && !string.IsNullOrEmpty(p.testo)) return p.testo;
            return o.testo ?? "";
        }

        /// <summary>
        /// L'obiettivo e' mostrato (X, freccia e riga) in base alla modalita' di aiuto. R029 (bug "il punto sparisce"): Guidata e Normale (default)
        /// lo mostrano SEMPRE, in ogni scena e dopo ogni interazione: prima in Normale ogni progresso lo nascondeva per minutiSenzaProgressi minuti
        /// e il giocatore restava senza meta. Solo Esploratore (scelta del giocatore) lo spegne.
        /// </summary>
        public static bool Mostrato { get { return Modo != Esploratore; } }

        /// <summary>Cosa indicare al giocatore adesso, in questa scena: riga di testo e punto (X) sulla pianta.</summary>
        public struct GuidaInfo
        {
            public bool valida;       // c'e' qualcosa da indicare (false solo a fine gioco o con scena senza pianta)
            public string testo;
            public bool haPunto;
            public float x, z;
            public string stato;      // "passo" | "uscita" | "capitoloFinito" | "fine" (per le prove)
        }

        /// <summary>
        /// R029: l'obiettivo risolto per la scena indicata. Regola generale: se il passo ha un punto in questa scena si indica quello; altrimenti
        /// (scena sbagliata) si indica l'uscita/porta della scena; "capitolo finito" (enigmi del ricordo tutti risolti, ricordo non ancora lasciato)
        /// indica l'uscita con il testo "esci dal ricordo". Non dipende dalla modalita' di aiuto (decide chi la usa con Mostrato).
        /// </summary>
        public static GuidaInfo Guida(string scena)
        {
            var g = new GuidaInfo { testo = "", stato = "fine" };
            var T = GameData.Mappe.testi;
            var ob = Corrente();
            if (ob != null)
            {
                var p = Punto(ob, scena);
                if (p != null)
                {
                    g.valida = true; g.haPunto = true; g.x = p.x; g.z = p.z; g.testo = Testo(ob, scena); g.stato = "passo";
                    return g;
                }
                // scena sbagliata: la porta giusta per uscire da qui
                g.valida = true; g.testo = Testo(ob, scena); g.stato = "uscita";
                PuntoUscita(scena, ref g);
                return g;
            }
            if (!FineCapitolo()) return g; // niente da fare: fine del gioco
            g.valida = true; g.stato = "capitoloFinito"; g.testo = T.esciRicordo;
            PuntoUscita(scena, ref g);
            if (!g.haPunto)
            {
                // fuori da un ricordo (hub): la porta del passo appena chiuso
                var l = GameData.Mappe.obiettivi;
                for (int i = l.Count - 1; i >= 0 && !g.haPunto; i--)
                {
                    if (!Completo(l[i]) && !ChiusoDalCapitolo(l[i])) continue;
                    var p = Punto(l[i], scena);
                    if (p != null) { g.haPunto = true; g.x = p.x; g.z = p.z; }
                    break;
                }
            }
            return g;
        }

        /// <summary>Capitolo finito ma non ancora lasciato: Corrente() e' null perche' il passo dopo e' di un altro capitolo.</summary>
        static bool FineCapitolo()
        {
            var l = GameData.Mappe.obiettivi;
            for (int i = 0; i < l.Count; i++)
            {
                if (ChiusoDalCapitolo(l[i]) || Completo(l[i])) continue;
                return i > 0 && l[i - 1].capitolo != l[i].capitolo && !string.IsNullOrEmpty(l[i - 1].capitolo) && !ChiusoDalCapitolo(l[i - 1]);
            }
            // tutti i passi completi: se l'ultimo capitolo non e' ancora lasciato (es. finale) si indica comunque l'uscita
            return l.Count > 0 && !string.IsNullOrEmpty(l[l.Count - 1].capitolo) && !ChiusoDalCapitolo(l[l.Count - 1]);
        }

        /// <summary>Il punto di uscita della scena in mappe.json (glifo uscita, altrimenti stazione/porta): dove si lascia il ricordo o la stanza.</summary>
        static void PuntoUscita(string scena, ref GuidaInfo g)
        {
            var def = GameData.MappaScena(scena);
            if (def == null || def.punti == null) return;
            MappaPuntoDef scelto = null;
            string[] ordine = { "uscita", "stazione", "porta" };
            for (int k = 0; k < ordine.Length && scelto == null; k++)
                for (int i = 0; i < def.punti.Count; i++) if (def.punti[i].glifo == ordine[k]) { scelto = def.punti[i]; break; }
            if (scelto == null) return;
            g.haPunto = true; g.x = scelto.x; g.z = scelto.z;
            if (g.stato == "uscita" && !string.IsNullOrEmpty(GameData.Mappe.testi.esciRicordo) && SceneRicordo(scena)) g.testo = GameData.Mappe.testi.esciRicordo;
        }

        static bool SceneRicordo(string scena) { var d = GameData.MappaScena(scena); return d != null && d.tema == "ricordo"; }

        /// <summary>R025: dopo il filmato iniziale la X del primo obiettivo resta visibile per un po' anche in aiuto Normale (non in Esploratore).</summary>
        public static bool Forzato { get { return Time.unscaledTime < forzatoFino; } }
        public static void Forza(float secondi) { forzatoFino = Time.unscaledTime + Mathf.Max(0f, secondi); }
        public static void SpegniForzatura() { forzatoFino = -1f; }

        /// <summary>Premuto Indizio in un enigma: in modalita' Normale l'obiettivo compare subito.</summary>
        public static void RivelaIndizio() { rivelato = true; TrenoBambini.Segreti.SegretiStato.AiutoLieve(); } // R011: a stanza completata l'Indizio accenna ai segreti

        /// <summary>Secondi passati senza progressi (solo gioco attivo): serve alle prove.</summary>
        public static float FermoSecondi { get { return fermoSecondi; } }

        /// <summary>Per le prove: azzera o forza il contatore.</summary>
        public static void ImpostaFermo(float secondi, bool rivela) { fermoSecondi = secondi; rivelato = rivela; }

        static int Firma()
        {
            var s = Save;
            if (s == null) return -1;
            int enigmi;
            int gettoni = TrenoBambini.Segreti.SegretiStato.ProgressiStoria(s, out enigmi); // R011: i segreti non azzerano l'attesa dell'obiettivo
            return gettoni * 1009 + enigmi * 31 + s.inventario.Count * 7 + s.flashbackCompletati.Count;
        }

        /// <summary>Da chiamare ogni frame di gioco: ogni progresso (gettone, enigma, oggetto, ricordo) azzera l'attesa e nasconde di nuovo l'obiettivo.</summary>
        public static void Tick(float dt, bool giocoAttivo)
        {
            int f = Firma();
            if (f != firma)
            {
                bool primo = firma < 0;
                firma = f;
                if (!primo) { fermoSecondi = 0f; rivelato = false; }
            }
            if (giocoAttivo) fermoSecondi += dt;
        }
    }
}
