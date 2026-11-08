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

    // R023: testi di menu iniziale e inventario (Resources/Data/ui_menu.json) e crediti (Resources/Data/crediti.json, generato da CREDITS.md)
    [Serializable] public class UiVoce { public string id; public string etichetta; }
    [Serializable]
    public class UiAnteprima { public string intestazione, oggetto1, oggetti, nessunOggetto, tempo, tempoPoco; }
    [Serializable]
    public class UiConferma { public string timbro, titolo, testo, si, no; }
    [Serializable]
    public class UiRicordi { public string titolo, bloccato, bloccatoNota, luogo, rivissuto, nessuno, indietro; }
    [Serializable]
    public class UiCrediti { public string titolo, intro, assetTitolo, indietro; public List<string> squadra = new List<string>(); }
    [Serializable]
    public class UiQuaderno { public string appunti, appuntiVuoto, indizi, indiziVuoto, segreti, segretiVuoto; }
    [Serializable]
    public class UiMappa { public string titolo, testo; }
    [Serializable]
    public class UiInventario
    {
        public string titoloLatta, aiuto, aiutoTouch, etichettaTema, daTrovare, latta, chiudi, ricordoStato, ricordoDaFare, ricordiVuoto;
        public List<UiVoce> sezioni = new List<UiVoce>();
        public UiQuaderno quaderno = new UiQuaderno();
        public UiMappa mappa = new UiMappa();
    }
    [Serializable]
    public class UiMenuFile
    {
        public string titolo1, titolo2, sottotitolo, premiUnTasto, aiutoMenu;
        public List<UiVoce> voci = new List<UiVoce>();
        public UiAnteprima anteprima = new UiAnteprima();
        public UiConferma conferma = new UiConferma();
        public UiRicordi ricordi = new UiRicordi();
        public UiCrediti crediti = new UiCrediti();
        public UiInventario inventario = new UiInventario();
    }
    [Serializable] public class CreditoVoce { public string asset, tipo, autore, licenza; }
    [Serializable] public class CreditiFile { public List<CreditoVoce> voci = new List<CreditoVoce>(); }
}
