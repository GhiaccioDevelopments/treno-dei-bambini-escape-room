using System;
using System.Collections.Generic;

namespace TrenoBambini.Data
{
    // R025: regia del filmato iniziale (Resources/Data/filmato_intro.json). Il copione (battute, ID, testi) sta in cutscene_intro.json;
    // qui ci sono le inquadrature (numeri, mai testo narrativo), gli inserti e i testi dell'interfaccia del filmato.

    [Serializable]
    public class FilmatoFile
    {
        public FilmatoParametri parametri = new FilmatoParametri();
        public FilmatoTesti testi = new FilmatoTesti();
        public List<FilmatoControllo> controlli = new List<FilmatoControllo>();
        public List<FilmatoCarta> carte = new List<FilmatoCarta>();
        public List<FilmatoCapitolo> capitoli = new List<FilmatoCapitolo>();
        public FilmatoFinale finale = new FilmatoFinale();
    }

    [Serializable]
    public class FilmatoParametri
    {
        public float tenutaSalta = 1.2f;          // secondi di pressione continua per saltare
        public float velocitaTesto = 45f;         // caratteri al secondo
        public float obiettivoForzatoSecondi = 30f; // la X dell'obiettivo resta visibile cosi' a lungo dopo il filmato (anche in aiuto Normale)
        public float guardiaSecondi = 0.35f;      // dopo l'inizio di una battuta il clic non la salta
        public float pausaFraBattute = 0.3f;
        public float velocitaCammino = 1.35f;     // m/s degli attori in scena
        public List<string> conRitratto = new List<string>(); // personaggi senza modello 3D: parlano con il ritratto in cornice d'ottone
    }

    [Serializable]
    public class FilmatoTesti
    {
        public string saltaTitolo = "", indizio = "", erroreTitolo = "", erroreTesto = "", piantaTitolo = "", piantaNota = "";
        public string latta = "", quaderno = "";
    }

    /// <summary>Targhetta dei controlli: un titolo e i tasti per ciascun dispositivo (tastiera, gamepad, touch).</summary>
    [Serializable]
    public class FilmatoControllo
    {
        public string id, titolo;
        public List<string> tastiera = new List<string>(), gamepad = new List<string>(), touch = new List<string>();
        public string notaTastiera = "", notaGamepad = "", notaTouch = "";
    }

    /// <summary>Foglio d'epoca mostrato come inserto (scheda elettorale, pagina della Costituzione...).</summary>
    [Serializable]
    public class FilmatoCarta
    {
        public string id, titolo, stile = "carta"; // carta | scheda | pagina
        public List<string> righe = new List<string>();
        public float larghezza = 640f, altezza = 420f, inclina;
    }

    [Serializable]
    public class FilmatoCapitolo
    {
        public string id, scena, stile = "fade";    // stile della transizione per ENTRARE nel capitolo: fade | ricordo
        public string look = "";                     // "ricordo" = look del ricordo (seppia, vignetta, grana) anche in una scena del presente, senza pioggia
        public string musica = "";                   // id musica (audio.json/AudioLibrary); "-" = nessuna musica, "" = quella della scena
        public string tinta = "";                    // "#RRGGBBAA" sopra la scena (look del presente piu' freddo, ricordo che si scalda)
        public string tintaFine = "";                // se presente la tinta passa lentamente a questo colore durante il capitolo
        public List<FilmatoInquadratura> inquadrature = new List<FilmatoInquadratura>();
    }

    [Serializable]
    public class FilmatoInquadratura
    {
        public string da = "";            // id della prima battuta; l'inquadratura dura fino alla successiva (la prima parte con il capitolo)
        public string taglio = "";        // "" = dissolvenza breve (stacco netto se "stacco")
        public float[] p0, p1;            // posizione camera iniziale/finale: mondo, oppure nel sistema dell'attore (x destra, y su, z avanti) se "segui"
        public float[] m0, m1;            // punto guardato (stesso sistema)
        public float fov0 = 45f, fov1 = 0f;
        public string segui = "";         // "" | adulto | bambino
        public string ease = "dolce";     // dolce | lineare
        public float scossa;              // metri di tremolio (scossa breve a inizio inquadratura)
        public bool consegna;             // la camera passa al gioco (si fonde nella camera di Amerigo) a inizio inquadratura
        public string musica = "";        // id musica da far partire con l'inquadratura ("-" = ferma la musica, "" = lascia com'e')
        public FilmatoAttore attore;
        public FilmatoInserto inserto;
    }

    [Serializable]
    public class FilmatoAttore
    {
        public bool presente;             // true = questa inquadratura da' istruzioni all'attore (JsonUtility non distingue "assente" da "vuoto")
        public float[] pos;               // x,z di partenza (y dal suolo); vuoto = non spostarlo
        public float yaw = 9999f;         // gradi (9999 = non cambiare)
        public List<float> percorso = new List<float>(); // coppie x,z da percorrere camminando
        public float velocita;            // m/s (0 = predefinita)
        public bool nascondi;             // l'attore non compare in questa inquadratura
    }

    /// <summary>Inserto: studio buio con pezzi/modelli che ruotano piano, foglio d'epoca o pianta.</summary>
    [Serializable]
    public class FilmatoInserto
    {
        public string tipo = "";          // "" = nessun inserto | pezzi | carta | pianta
        public List<ParteDef> parti = new List<ParteDef>(); // come negli enigmi: forme, materiali, modello, testo
        public string carta = "";         // id in "carte"
        public float distanza = 1f;       // distanza della camera (1 = inquadra tutto)
        public float alto = 0.25f;        // altezza relativa
        public float giraAlSecondo = 14f; // gradi/s attorno ai pezzi
        public float inclina = 12f;
        public float fov = 32f;
        public string luce = "calda";     // calda | lampada
        public string indizio = "";       // "si": il pulsante Indizio evidenziato sopra l'inserto
        public string errore = "";        // "si": biglietto gentile dell'errore
        public string croce = "";         // pianta: "si" = croce dell'obiettivo a matita
        public string scena = "";         // pianta: scena (Resources/Mappe/<scena>.jpg)
        public string minimappa = "";     // "si": mentre dura si vede la minimappa vera in alto a sinistra
        public bool piccola;              // pianta: si rimpicciolisce in alto a sinistra come la minimappa
    }

    [Serializable]
    public class FilmatoFinale
    {
        public string scena = "10_Hub_Vicolo";
        public float[] pos;               // x,z dove Amerigo resta al termine
        public float yaw = 270f;
    }
}
