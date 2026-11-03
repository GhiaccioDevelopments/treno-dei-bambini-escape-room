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
        public bool aperto;
    }

    [Serializable] public class OggettiFile { public List<OggettoDef> oggetti = new List<OggettoDef>(); }
    [Serializable]
    public class OggettoDef
    {
        public string id; public string nome; public string descrizione; public string significato;
        public string temaCivico; public string icona; public string flashback; public string cutscene; public string luogo;
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
}
