using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.Core
{
    [Serializable]
    public class SaveData
    {
        public string scena = "";
        public string spawn = "";
        public List<string> inventario = new List<string>();
        public List<string> flashbackCompletati = new List<string>();
        public List<string> enigmiRisolti = new List<string>(); // "flashbackId/enigmaId"
        public List<string> cutsceneViste = new List<string>();
        public string ritornoScena = "";
        public string ritornoSpawn = "";
        public float volume = 1f;
        // R002: canali audio (1 = livello di base di audio.json). I salvataggi vecchi non li hanno: restano a 1.
        public float volMusica = 1f, volAmbiente = 1f, volEffetti = 1f, volVoci = 1f;
        public bool effettiRidotti; // R001: UI senza tremolii/particelle (accessibilità o PC lento)
        // R005: true = la visuale gira solo tenendo premuto il tasto destro (cursore libero); false = muovendo il mouse (cursore bloccato)
        public bool visualeTastoDestro;
        public List<string> oggettiEnigma = new List<string>(); // R020: oggetti/indizi ottenuti negli enigmi del mondo
        public bool enigmiSemplificati;                          // R009: pannelli 2D al posto delle viste ravvicinate (LIM, accessibilita')
        public bool evidenziaInterattivi;                        // R009: brillio leggero sugli oggetti da esaminare (spento di default)
        public float tempoGioco; // R023: secondi giocati (anteprima di Continua)
        // R019: minimappa. aiutoMappa: 0 = Guidata (obiettivo sempre segnato), 1 = Normale (dopo qualche minuto senza progressi o con Indizio), 2 = Esploratore (nessun obiettivo)
        public int aiutoMappa = 1;
        public bool minimappaNascosta;   // true = minimappa spenta (la mappa grande col tasto M resta)
        public bool nordFisso;           // true = la pianta non ruota con la camera
        public bool filmatoVisto;        // R025: il filmato iniziale e' stato visto (o saltato) in questa partita
    }

    /// <summary>Salvataggio su PlayerPrefs (chiave treno_save, JSON).</summary>
    public static class SaveSystem
    {
        const string Key = "treno_save";

        public static bool Exists() { return PlayerPrefs.HasKey(Key); }

        public static SaveData Load()
        {
            SaveData d = null;
            if (PlayerPrefs.HasKey(Key))
            {
                try { d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)); }
                catch (Exception e) { Debug.LogWarning("Salvataggio illeggibile, riparto da zero: " + e.Message); }
            }
            if (d == null) d = new SaveData();
            if (d.inventario == null) d.inventario = new List<string>();
            if (d.flashbackCompletati == null) d.flashbackCompletati = new List<string>();
            if (d.enigmiRisolti == null) d.enigmiRisolti = new List<string>();
            if (d.oggettiEnigma == null) d.oggettiEnigma = new List<string>();
            if (d.cutsceneViste == null) d.cutsceneViste = new List<string>();
            if (d.scena == null) d.scena = "";
            if (d.spawn == null) d.spawn = "";
            if (d.ritornoScena == null) d.ritornoScena = "";
            if (d.ritornoSpawn == null) d.ritornoSpawn = "";
            TrenoBambini.Puzzles.MondoStato.Migra(d); // R020: gettoni mancanti dei salvataggi vecchi
            return d;
        }

        public static void Save(SaveData d)
        {
            if (d == null) return;
            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("Salvataggio non riuscito: " + e.Message); }
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
