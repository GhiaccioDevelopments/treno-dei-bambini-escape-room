using System;
using System.Collections.Generic;

namespace TrenoBambini.Data
{
    // Classi serializzabili che rispecchiano i JSON in Resources/Data (nomi campi = chiavi JSON).

    [Serializable] public class LuoghiFile { public List<LuogoDef> luoghi = new List<LuogoDef>(); }
    [Serializable]
    public class LuogoDef
    {
        public string id; public string nome; public string scena; public string spawn; public string descrizione;
        public string meteo; // R007 t4: pioggia | pioggia_fuori | sereno (vuoto = sereno)
        public bool aperto;
    }

    [Serializable] public class OggettiFile { public List<OggettoDef> oggetti = new List<OggettoDef>(); }
    [Serializable]
    public class OggettoDef
    {
        public string id; public string nome; public string descrizione; public string significato;
        public string temaCivico; public string icona; public string flashback; public string cutscene; public string luogo;
        public string modello; // R023: prefab in Resources/Modelli/<modello> mostrato in 3D nella latta (vuoto = solo l'icona)
    }

    [Serializable] public class PersonaggiFile { public List<PersonaggioDef> personaggi = new List<PersonaggioDef>(); }
    [Serializable]
    public class PersonaggioDef
    {
        public string id; public string nome; public string ritratto;
        public string colore; // "#RRGGBB"
    }

    [Serializable]
    public class FlashbackDef
    {
        public string id; public string oggetto; public string titolo; public string scena;
        public string introduzione; public string conclusione;
        public List<EnigmaDef> enigmi = new List<EnigmaDef>();
    }

    [Serializable]
    public class EnigmaDef
    {
        public string id; public string tipo; public string titolo; public string consegna; public string temaCivico;
        public string spiegazione; public string erroreMsg; public string codice;
        public float x; public float y; public float z;
        public string modello;
        public List<string> indizi = new List<string>();
        public List<DomandaDef> domande = new List<DomandaDef>();
        public List<NascostoDef> oggetti = new List<NascostoDef>();
        public List<EventoDef> eventi = new List<EventoDef>();
        public List<CoppiaDef> coppie = new List<CoppiaDef>();
    }

    [Serializable]
    public class DomandaDef
    {
        public string testo; public List<string> opzioni = new List<string>(); public int corretta;
        public string spiegazione; public string erroreMsg;
    }

    [Serializable]
    public class NascostoDef
    {
        public string id; public string nome; public string descrizione;
        public float x; public float y; public float z;
        public string modello;
    }

    [Serializable] public class EventoDef { public string testo; public string data; }
    [Serializable] public class CoppiaDef { public string sinistra; public string destra; }

    [Serializable]
    public class CutsceneDef
    {
        public string id; public string titolo; public string sfondo;
        public List<BattutaDef> battute = new List<BattutaDef>();
    }

    [Serializable]
    public class BattutaDef
    {
        public string id; public string personaggio; public string testo; public string emozione; public string audio;
        public string azione; public string sfondo;
        public string posizione; // "sx" | "dx"
    }

    // ---- audio.json (R002): quale musica e quale ambiente suonano in ogni scena, eventi e volumi di base ----
    [Serializable]
    public class AudioFile
    {
        public List<AudioSceneDef> scene = new List<AudioSceneDef>();
        public List<AudioEventDef> eventi = new List<AudioEventDef>();
        public AudioVolumiDef volumi = new AudioVolumiDef();
    }

    [Serializable]
    public class AudioSceneDef
    {
        public string scena;
        public string musica;                                  // id musica ("" = nessuna)
        public List<string> ambiente = new List<string>();     // loop d'ambiente
        public List<string> sparsi = new List<string>();       // suoni casuali sparsi
        public float[] intervallo = new float[] { 8f, 20f };   // secondi min/max tra due suoni sparsi
        public string superficie;                              // pietra | legno | ovattato (passi)
    }

    [Serializable]
    public class AudioEventDef
    {
        public string id;       // enigma_risolto, oggetto_ritrovato, ricordo_entra, ricordo_esce, porta_apre ...
        public string stinger;  // musica breve sopra il brano corrente
        public string sfx;      // effetto
    }

    [Serializable]
    public class AudioVolumiDef
    {
        public float musica = 0.6f, ambiente = 0.7f, effetti = 0.9f, voci = 1f;
        public float duckingMusica = 0.35f, duckingAmbiente = 0.6f;
    }

    // R023: testi di menu iniziale e inventario (Resources/Data/ui_menu.json) e crediti (Resources/Data/crediti.json, generato da CREDITS.md).
    // Ogni campo ha un valore di ripiego (M6): se manca nel JSON la UI resta usabile. I testi veri stanno nel JSON.
    [Serializable] public class UiVoce { public string id; public string etichetta; }
    [Serializable]
    public class UiAnteprima
    {
        public string intestazione = "Ultima partita", oggetto1 = "1 oggetto nella latta", oggetti = "{0} oggetti nella latta", nessunOggetto = "La latta e' ancora vuota";
        public string tempo1 = "1 minuto di gioco", tempo = "{0} minuti di gioco", tempoPoco = "meno di un minuto di gioco";
    }
    [Serializable]
    public class UiConferma { public string timbro = "ATTENZIONE", titolo = "Nuova partita", testo = "Il salvataggio verra' cancellato. Continuare?", si = "Si'", no = "Annulla"; }
    [Serializable]
    public class UiRicordi
    {
        public string titolo = "Ricordi", bloccato = "Ricordo da trovare", bloccatoNota = "Si apre quando ritrovi l'oggetto giusto.";
        public string luogo = "Dove", rivissuto = "Rivissuto", nessuno = "Nessun ricordo.", indietro = "Indietro";
    }
    [Serializable]
    public class UiCrediti { public string titolo = "Crediti", intro = "", assetTitolo = "Asset esterni e licenze", indietro = "Indietro"; public List<string> squadra = new List<string>(); }
    [Serializable]
    public class UiQuaderno
    {
        public string appunti = "Appunti", appuntiVuoto = "", indizi = "Indizi", indiziVuoto = "", segreti = "Segreti", segretiVuoto = "";
    }
    [Serializable]
    public class UiMappa { public string titolo = "Mappa", testo = ""; }
    [Serializable]
    public class UiInventario
    {
        public string titoloLatta = "La latta", aiuto = "", aiutoTouch = "", etichettaTema = "Educazione civica", daTrovare = "Da trovare", latta = "Nella latta", chiudi = "Chiudi";
        public string ricordoStato = "Rivissuto", ricordoDaFare = "Da rivivere", ricordiVuoto = "";
        // scheda dell'oggetto (I3)
        public string etichettaRicordo = "Ricordo", cercaVicino = "Un ricordo ancora da trovare: cerca vicino a {0}.", cercaGenerico = "Un ricordo ancora da trovare.", latteVuota = "La latta e' vuota.";
        public string ricordoNonDisponibile = "Ricordo non ancora disponibile.";
        public List<UiVoce> sezioni = new List<UiVoce>
        {
            new UiVoce { id = "oggetti", etichetta = "Oggetti" }, new UiVoce { id = "ricordi", etichetta = "Ricordi" },
            new UiVoce { id = "quaderno", etichetta = "Quaderno" }, new UiVoce { id = "mappa", etichetta = "Mappa" }
        };
        public UiQuaderno quaderno = new UiQuaderno();
        public UiMappa mappa = new UiMappa();
    }
    /// <summary>Etichette dell'HUD e della pausa legate alla latta (M7).</summary>
    [Serializable] public class UiHud { public string latta = "Latta"; }
    [Serializable]
    public class UiMenuFile
    {
        public string titolo1 = "Il treno", titolo2 = "dei bambini", sottotitolo = "Napoli, 1946", premiUnTasto = "", aiutoMenu = "";
        public List<UiVoce> voci = new List<UiVoce>
        {
            new UiVoce { id = "continua", etichetta = "Continua" }, new UiVoce { id = "nuova", etichetta = "Nuova partita" },
            new UiVoce { id = "ricordi", etichetta = "Ricordi" }, new UiVoce { id = "impostazioni", etichetta = "Impostazioni" },
            new UiVoce { id = "crediti", etichetta = "Crediti" }, new UiVoce { id = "esci", etichetta = "Esci" }
        };
        public UiAnteprima anteprima = new UiAnteprima();
        public UiConferma conferma = new UiConferma();
        public UiRicordi ricordi = new UiRicordi();
        public UiCrediti crediti = new UiCrediti();
        public UiInventario inventario = new UiInventario();
        public UiHud hud = new UiHud();

        /// <summary>Dopo il caricamento: rimette i valori di ripiego dove il JSON ha messo null (un blocco o una lista mancanti non devono far crollare la UI).</summary>
        public UiMenuFile Normalizza()
        {
            var d = new UiMenuFile();
            if (voci == null || voci.Count == 0) voci = d.voci;
            if (anteprima == null) anteprima = d.anteprima;
            if (conferma == null) conferma = d.conferma;
            if (ricordi == null) ricordi = d.ricordi;
            if (crediti == null) crediti = d.crediti;
            if (crediti.squadra == null) crediti.squadra = new List<string>();
            if (hud == null) hud = d.hud;
            if (inventario == null) inventario = d.inventario;
            if (inventario.sezioni == null || inventario.sezioni.Count != 4) inventario.sezioni = d.inventario.sezioni;
            if (inventario.quaderno == null) inventario.quaderno = d.inventario.quaderno;
            if (inventario.mappa == null) inventario.mappa = d.inventario.mappa;
            return this;
        }
    }
    [Serializable] public class CreditoVoce { public string asset, tipo, autore, licenza; }
    [Serializable] public class CreditiFile { public List<CreditoVoce> voci = new List<CreditoVoce>(); }
}
