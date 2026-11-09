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
            for (int i = 0; i < l.Count; i++) if (!ChiusoDalCapitolo(l[i]) && !Completo(l[i])) return l[i];
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

        /// <summary>L'obiettivo e' mostrato (X, freccia e riga) in base alla modalita' di aiuto.</summary>
        public static bool Mostrato
        {
            get
            {
                switch (Modo)
                {
                    case Guidata: return true;
                    case Esploratore: return false;
                    default:
                        return Forzato || rivelato || fermoSecondi >= GameData.Mappe.parametri.minutiSenzaProgressi * 60f;
                }
        }
        }

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
            return s.oggettiEnigma.Count * 1009 + s.enigmiRisolti.Count * 31 + s.inventario.Count * 7 + s.flashbackCompletati.Count;
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
