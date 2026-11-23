using System;
using System.Collections.Generic;

namespace TrenoBambini.Data
{
    /// <summary>R011: contenuti di segreti.json (collezionabili, enigmi segreti, easter egg). Nessun testo narrativo sta nel codice.</summary>
    [Serializable]
    public class SegretiFile
    {
        public SegretiTesti testi = new SegretiTesti();
        public List<StanzaSegretiDef> stanze = new List<StanzaSegretiDef>();
        public List<SegretoDef> segreti = new List<SegretoDef>();
        public List<SegretoPezzoDef> pezzi = new List<SegretoPezzoDef>();
        public List<EnigmaDef> enigmi = new List<EnigmaDef>();       // enigmi segreti (stesso formato di R009/R020, con "scena" e "segreto")
        public List<CampanaDef> campane = new List<CampanaDef>();
        public List<UovoDef> uova = new List<UovoDef>();
        public List<NascostoDef> gettoni = new List<NascostoDef>();  // nome e descrizione dei biglietti e degli indizi (compaiono nel quaderno, in "Trovati finora")
    }

    [Serializable]
    public class SegretiTesti
    {
        public string contatoreFmt = "{0}/{1}";           // menu pausa: "Segreti trovati: 3/17"
        public string contatoreEtichetta = "Segreti";
        public string stanzaFmt = "{0}  {1}/{2}";         // quaderno: "Il basso di Antonietta  3/5"
        public string casellaPiena = "[x]", casellaVuota = "[  ]", casellaIgnota = "ancora da trovare";
        public string trovatoFmt = "{0}";                  // titolo della scheda quando si trova qualcosa
        public string quadernoRiga = "Aggiunto ai segreti del quaderno.";
        public string dietroQuinteTitolo = "Dietro le quinte";
        public string stanzaCompletaTitolo = "Tutto trovato";
        public string aiutoLieve = "";                      // Indizio, a stanza principale completata
        public string raccogli = "Raccogli: {0}", guarda = "Guarda: {0}", apri = "{0}";
        public string richiedeGenerico = "";
        public string campanaPasso = "", campanaSbagliata = "", campanaFatta = "";
        public string tuttiTitolo = "", tuttiTesto = "";
        public string quadernoVuoto = "";
    }

    [Serializable]
    public class StanzaSegretiDef
    {
        public string scena;
        public string nome;
        public List<string> completo = new List<string>();  // condizioni di "stanza principale completata" (prefissi fb: ris: flag:, altrimenti gettone)
    }

    /// <summary>Un segreto da trovare: il suo conteggio entra nella pagina "Segreti" del quaderno e nel contatore della pausa.</summary>
    [Serializable]
    public class SegretoDef
    {
        public string id;
        public string scena;
        public string tipo = "collezionabile";   // collezionabile | enigma
        public string nome;
        public string scheda;                    // scheda storica o civica
        public string ricompensa = "";           // enigma: pagina "dietro le quinte" mostrata alla soluzione
        public string punto = "";                // nome del punto nei rapporti R006 (Docs/richieste/media/R006/punti_segreti_*.md)
        public float x, y, z;                    // calcolati dal punto (ArtSource/Segreti/genera_segreti.py o Treno > Segreti > Verifica punti)
        public int ordine;
    }

    /// <summary>Un pezzo di mondo dei segreti: collezionabile, biglietto-indizio, scritta da leggere, mattonella da sollevare, premio, decoro.</summary>
    [Serializable]
    public class SegretoPezzoDef
    {
        public string id;
        public string scena;
        public string segreto = "";              // id del segreto che si trova raccogliendo questo pezzo (vuoto = nessuno)
        public string ruolo = "raccogli";        // raccogli | indizio | lettura | apri | decoro
        public string punto = "";
        public float dx, dy, dz;                 // spostamento rispetto al punto
        public float x, y, z;                    // posizione finale (calcolata dal punto)
        public float yaw;
        public float rx, rz;                     // inclinazione (es. un biglietto appuntato al muro: rx = -90)
        public string appoggio = "suolo";        // suolo (il pezzo scende fino al primo solido sotto) | fisso (y esatta)
        public float scala = 1f;
        public string prompt = "";
        public string titolo = "";               // titolo del messaggio (vuoto = nome del segreto o del gettone)
        public string msg = "";                  // testo mostrato quando si prende o si legge
        public List<string> produce = new List<string>();
        public List<string> richiede = new List<string>();
        public string richiedeMsg = "";
        public List<string> appareQuando = new List<string>();   // il pezzo c'e' solo quando queste condizioni sono vere
        public string nascondiQuando = "";                      // sparisce quando questa condizione e' vera
        public string scorriQuando = "";                         // si sposta di (sx,sy,sz) quando questa condizione e' vera
        public float sx, sy, sz;                                 // spostamento dello scorrimento
        public string scorriOggetto = "";                        // nomi (separati da virgola) di oggetti di scena vicini che scorrono insieme (es. la credenza e cio' che ci sta sopra)
        public float raggio = 0.45f;             // zona di interazione (trigger) attorno al pezzo
        public List<ParteDef> parti = new List<ParteDef>();
    }

    /// <summary>Campana disegnata col gesso: si salta sulle caselle nell'ordine giusto (la sequenza sta scritta in un graffito).</summary>
    [Serializable]
    public class CampanaDef
    {
        public string id;
        public string scena;
        public string punto = "";
        public float dx, dz;
        public float x, y, z;
        public float yaw;
        public string sequenza = "";             // cifre = numeri delle caselle da toccare, in ordine
        public string produce = "";              // gettone ottenuto
        public string richiede = "";             // gettone necessario (di solito la lettura del graffito)
        public float sosta = 0.35f;              // secondi fermi su una casella perche' conti
        public List<CasellaDef> caselle = new List<CasellaDef>();
    }

    [Serializable]
    public class CasellaDef { public int n; public float x, z, w = 0.8f, d = 0.8f; }

    /// <summary>Easter egg discreto: compare solo quando la condizione e' vera.</summary>
    [Serializable]
    public class UovoDef
    {
        public string id;
        public string scena;
        public string tipo;                      // violino | scarpe
        public string punto = "";
        public float dx, dy, dz;
        public float x, y, z;
        public List<string> quando = new List<string>();
        public float raggio = 14f;               // violino: da quanto lontano si sente
        public float volume = 0.3f;
        public string nota = "";                 // aggancio per i capitoli futuri (non letta dal gioco)
    }
}
