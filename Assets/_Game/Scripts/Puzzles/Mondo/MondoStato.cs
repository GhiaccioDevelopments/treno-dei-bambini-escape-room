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

        /// <summary>Registra la soluzione: chiave nei risolti, id come gettone, gettoni prodotti; salva.</summary>
        public static void Completa(EnigmaDef def, string chiave)
        {
            var s = Save;
            if (s == null) return;
            if (!s.enigmiRisolti.Contains(chiave)) s.enigmiRisolti.Add(chiave);
            Dai(def.id);
            if (def.produce != null) foreach (var p in def.produce) Dai(p);
            GameManager.I.SaveGame();
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
            UIRoot.I.Toast("Nel quaderno: " + Nome(token) + (string.IsNullOrEmpty(d) ? "" : ". " + d), 5f);
        }
    }
}
