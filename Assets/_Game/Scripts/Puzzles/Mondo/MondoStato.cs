using System.Collections.Generic;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R009/R020: stato degli enigmi nel mondo. "Gettoni" = oggetti e indizi ottenuti (chiavi, cartoncini, cifre) e id degli enigmi risolti;
    /// stanno in SaveData.oggettiEnigma. Gli enigmi risolti stanno anche in SaveData.enigmiRisolti ("mondo/id" o "flashback_x/id").
    /// </summary>
    public static class MondoStato
    {
        static SaveData Save { get { return GameManager.I != null ? GameManager.I.Save : null; } }

        public static bool Ha(string token)
        {
            if (string.IsNullOrEmpty(token)) return true;
            var s = Save;
            return s != null && s.oggettiEnigma.Contains(token);
        }

        public static bool HaTutti(List<string> tokens)
        {
            if (tokens == null) return true;
            for (int i = 0; i < tokens.Count; i++) if (!Ha(tokens[i])) return false;
            return true;
        }

        public static void Dai(string token)
        {
            var s = Save;
            if (s == null || string.IsNullOrEmpty(token) || s.oggettiEnigma.Contains(token)) return;
            s.oggettiEnigma.Add(token);
        }

        public static void Togli(string token)
        {
            var s = Save;
            if (s != null && !string.IsNullOrEmpty(token)) s.oggettiEnigma.Remove(token);
        }

        public static bool Risolto(string chiave)
        {
            var s = Save;
            return s != null && s.enigmiRisolti.Contains(chiave);
        }

        /// <summary>Funzione unica dei gettoni di un enigma risolto: il suo id, i suoi produce, i produce delle parti (oggetti presi con l'azione prendi) e gli oggetti nascosti. Idempotente.</summary>
        public static void AssegnaGettoni(EnigmaDef def)
        {
            if (def == null) return;
            Dai(def.id);
            if (def.produce != null) foreach (var p in def.produce) Dai(p);
            if (def.parti != null) foreach (var pt in def.parti) if (!string.IsNullOrEmpty(pt.produce)) Dai(pt.produce);
            if ((def.tipo ?? "").ToLowerInvariant() == "nascosto" && def.oggetti != null) foreach (var o in def.oggetti) Dai(o.id);
        }

        /// <summary>Registra la soluzione: chiave nei risolti, gettoni (anche quelli delle parti, cosi' la versione semplificata non perde nulla); salva.</summary>
        public static void Completa(EnigmaDef def, string chiave)
        {
            var s = Save;
            if (s == null) return;
            if (!s.enigmiRisolti.Contains(chiave)) s.enigmiRisolti.Add(chiave);
            AssegnaGettoni(def);
            GameManager.I.SaveGame();
        }

        /// <summary>Trova la definizione dell'enigma dalla chiave di salvataggio ("mondo/id" o "flashback_x/id").</summary>
        public static EnigmaDef Trova(string chiave)
        {
            int i = chiave.IndexOf('/');
            if (i < 0) return null;
            string gruppo = chiave.Substring(0, i), id = chiave.Substring(i + 1);
            if (gruppo == "mondo") { foreach (var e in GameData.EnigmiMondo) if (e.id == id) return e; return null; }
            var f = GameData.Flashback(gruppo);
            if (f == null) return null;
            foreach (var e in f.enigmi) if (e.id == id) return e;
            return null;
        }

        /// <summary>Migrazione dei salvataggi vecchi (idempotente): per ogni enigma gia' risolto assegna i gettoni che la versione precedente non dava.</summary>
        public static void Migra(SaveData d)
        {
            if (d == null || d.enigmiRisolti == null) return;
            if (d.oggettiEnigma == null) d.oggettiEnigma = new List<string>();
            foreach (var k in new List<string>(d.enigmiRisolti))
            {
                var def = Trova(k);
                if (def == null) continue;
                if (!d.oggettiEnigma.Contains(def.id)) d.oggettiEnigma.Add(def.id);
                if (def.produce != null) foreach (var p in def.produce) if (!d.oggettiEnigma.Contains(p)) d.oggettiEnigma.Add(p);
                if (def.parti != null) foreach (var pt in def.parti) if (!string.IsNullOrEmpty(pt.produce) && !d.oggettiEnigma.Contains(pt.produce)) d.oggettiEnigma.Add(pt.produce);
                if ((def.tipo ?? "").ToLowerInvariant() == "nascosto" && def.oggetti != null) foreach (var o in def.oggetti) if (!d.oggettiEnigma.Contains(o.id)) d.oggettiEnigma.Add(o.id);
            }
        }

        /// <summary>Nome leggibile di un gettone (da enigmi_mondo.json "oggetti" o dagli oggetti nascosti dei flashback).</summary>
        public static string Nome(string token)
        {
            foreach (var o in GameData.OggettiEnigma) if (o.id == token) return o.nome;
            return token;
        }

        public static string Descrizione(string token)
        {
            foreach (var o in GameData.OggettiEnigma) if (o.id == token) return o.descrizione;
            return "";
        }

        /// <summary>Mostra un toast "Hai ottenuto: nome" per ogni gettone nuovo.</summary>
        public static void Annuncia(string token)
        {
            if (UIRoot.I == null || string.IsNullOrEmpty(token)) return;
            var d = Descrizione(token);
            UIRoot.I.Toast(string.Format(GameData.TestiEnigmi.quadernoOggettoFmt, Nome(token)) + (string.IsNullOrEmpty(d) ? "" : ". " + d), 5f);
        }
    }
}
