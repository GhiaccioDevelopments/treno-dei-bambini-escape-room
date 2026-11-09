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
        public List<string> richiede = new List<string>();     // R020: gettoni necessari per entrare (porta chiusa a chiave)
        public List<string> richiedeMsg = new List<string>();   // messaggio gentile per ciascun gettone mancante (stesso ordine)
        public string aperturaMsg;      // R020: messaggio la prima volta che la porta si apre con i gettoni
        public string bypassOggetto;    // R020: se questo oggetto e' gia' nell'inventario la porta non chiede i gettoni (salvataggi avanzati)
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
        public string nomeCopione; // R025: nome nel copione CSV del doppiaggio (vuoto = nome)
    }

    [Serializable] public class EnigmiMondoFile { public List<EnigmaDef> enigmi = new List<EnigmaDef>(); public List<NascostoDef> oggetti = new List<NascostoDef>(); public EnigmiTesti testi = new EnigmiTesti(); }

    /// <summary>R020: testi dell'interfaccia degli enigmi nel mondo (enigmi_mondo.json, blocco "testi"). I valori di ripiego sono volutamente neutri: i testi veri stanno nel JSON.</summary>
    [Serializable]
    public class EnigmiTesti
    {
        public string guida = "", guidaTouch = "";
        public string btnIndizio = "Indizio", btnChiudi = "Chiudi", btnConsegna = "Consegna", btnAvanti = "Avanti", btnConferma = "Conferma", mestieri = "";
        public string gruppoFmt = "{0}: {1}/{2}", deduzioneErrore = "{0}", domandaFmt = "{0}/{1}: {2}", esatto = "", prossima = "";
        public string inMano = "{0}", manoLabel = "", serveQualcosa = "", nonCalzaNome = "{0}", nonCalza = "", prendiPrimaGiusto = "", prendiPrima = "";
        public string dritta = "", quasiDritta = "", calza = "", lucchettoApre = "", lucchettoNo = "", erroreGenerico = "", erroreOrdine = "", nessunIndizio = "";
        public string indizioFmt = "{0}/{1}: {2}", educazione = "", completo = "", esaminato = "{0}", esamina = "{0}", bloccato = "", nonDisponibile = "";
        public string quadernoFmt = "{0}", quadernoOggettoFmt = "{0}", porta = "", portaChiusa = "", entraFmt = "{0}";
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

        // ---- R009/R020: enigmi dentro il mondo (oggetti d'epoca esaminabili) ----
        public string scena;                 // solo in enigmi_mondo.json: scena in cui sta l'oggetto
        public string meccanismo;            // oggetto | ruote | deduzione | documenti | ordina | nascosto ("" = ricavato dal tipo)
        public float yaw;                    // orientamento nel mondo (gradi)
        public float scala = 1f;             // scala dell'oggetto nel mondo (nella vista ravvicinata e' ingrandito)
        public float distanza = 1.6f;        // distanza della camera nella vista ravvicinata
        public string prompt;                // "Esamina: ..." se diverso dal titolo
        public List<ParteDef> parti = new List<ParteDef>();   // l'oggetto: pezzi di primitive con materiali d'epoca
        public RuotaDef ruote;               // ruote a cifre (lucchetto, calendario)
        public List<string> richiede = new List<string>();    // oggetti o enigmi gia' ottenuti/risolti necessari per esaminare
        public List<string> produce = new List<string>();     // oggetti/indizi ottenuti alla soluzione
        public string bloccatoMsg;           // messaggio gentile se mancano i requisiti
        public string scheda;                // scheda civica breve dopo la soluzione
        public string appunto;               // riga che entra nel quaderno
        public List<string> soluzione = new List<string>();   // eventi (id) da compiere nella vista ravvicinata
        public bool ordinata;                // gli eventi vanno compiuti in ordine
        public List<PassanteDef> passanti = new List<PassanteDef>();
        public List<string> mestieri = new List<string>();
        public List<DocumentoDef> documenti = new List<DocumentoDef>();
        public string sostituisce;           // nomi (separati da virgola) di oggetti gia' presenti nella scena da nascondere perche' l'enigma li rimpiazza
        public float inclina = 8f;           // inclinazione iniziale della camera nella vista ravvicinata (gradi)
        public string completoMsg;           // nascosto: testo aggiunto quando tutti gli oggetti sono trovati
        public string finale;                // se valorizzato (nella scena l'oggetto-ricordo con questo id resta nascosto finche' l'enigma non e' risolto), alla soluzione parte il ricordo di questo oggetto (es. "scarpe")
    }

    /// <summary>Un pezzo dell'oggetto esaminabile: una primitiva (o un testo) con materiale, posizione locale e comportamento al clic.</summary>
    [Serializable]
    public class ParteDef
    {
        public string id;
        public string forma = "cubo";         // cubo | cilindro | sfera | capsula | testo | scarpa (stile in testo) | vuoto (gruppo; con clic ha un collider sx,sy,sz)
        public string materiale = "legno";    // legno | metallo | ottone | argento | carta | stoffa | cuoio | pietra | gesso | vetro | cera
        public string colore = "";            // "#RRGGBB" (vuoto = colore del materiale)
        public float x, y, z;                 // posizione locale (m)
        public float rx, ry, rz;              // rotazione locale (gradi)
        public float sx = 1f, sy = 1f, sz = 1f;
        public string testo = "";             // forma testo: il testo; con altre forme: scritta sulla faccia frontale
        public float dimensione = 0.05f;      // altezza del testo (m)
        public string gruppo = "";
        public bool nascosta;                 // compare solo quando un'azione la rivela (campo dopo)
        public bool clic;                     // cliccabile nella vista ravvicinata
        public string azione = "";            // "" | leggi | gira | ruota | rivela | nascondi | muovi | premi | prendi | usa | errore
        public string evento = "";            // evento registrato (vuoto = id della parte)
        public string dopo = "";              // id o gruppi (g:nome) rivelati, separati da virgola
        public string asse = "y";             // asse per gira/ruota/muovi
        public float valore = 4f;             // gradi per clic (ruota), distanza (muovi)
        public string msg = "";               // messaggio mostrato al clic
        public string richiede = "";          // oggetto necessario per l'azione (inventario attivo)
        public string produce = "";           // oggetto ottenuto con l'azione
        public string modello = "";          // modello vero (Resources/Modelli/Enigmi/<nome>, normalizzato in un cubo unitario) al posto della primitiva: collider e misure restano quelli della parte
        public float msx, msy, msz;           // misura vera del modello (m); 0 = quella della parte (il collider resta sx,sy,sz)
        public float mox, moy, moz;           // centro del modello rispetto al centro della parte (m, assi della parte)
        public string font = "";              // scritte: "" = caratteri da stampa d'epoca, "mano" = scrittura a mano (matita, gesso)
        public string genitore = "";          // id di un altro pezzo: questo ne diventa figlio (posizione locale rispetto al genitore)
        public string bersaglio = "";          // id del pezzo su cui agisce gira/ruota/muovi (vuoto = questo)
        public string quando = "";             // il pezzo compare quando questo evento e' accettato ("risolto" = a enigma risolto)
    }

    [Serializable] public class RuotaDef { public int n = 4; public float x, y, z, passo = 0.14f, larghezza = 0.09f, raggio = 0.07f; public string colore = "#8a7a52"; }

    [Serializable]
    public class PassanteDef
    {
        public string id, nome, descrizione;
        public string stile;      // lucide | chiodate | zoccoli | militari | tacchi | risuolate
        public string mestiere;   // soluzione (uno dei mestieri dell'enigma)
        public string colore = "#3b2d22";
        public string abito = "#5a4a3a";
    }

    [Serializable] public class DocumentoDef { public string id, titolo; public List<string> righe = new List<string>(); }

    [Serializable]
    public class DomandaDef
    {
        public string testo; public List<string> opzioni = new List<string>(); public int corretta;
        public string spiegazione; public string erroreMsg;
        public string documento; public int riga = -1; // R020: la risposta e' la riga indicata nel documento (opzioni/corretta restano per la versione semplificata)
    }

    [Serializable]
    public class NascostoDef
    {
        public string id; public string nome; public string descrizione;
        public float x; public float y; public float z;
        public string modello;
        public float rx;        // R024: inclinazione attorno all'asse lungo (gradi): 0 = in piedi, ~75 = abbandonata su un fianco
        public string colore;   // R024: colore del cuoio (vuoto = predefinito dello stile)
    }

    [Serializable] public class EventoDef { public string testo; public string data; }
    [Serializable] public class CoppiaDef { public string sinistra; public string destra; }

    [Serializable]
    public class CutsceneDef
    {
        public string id; public string titolo; public string sfondo;
        public List<BattutaDef> battute = new List<BattutaDef>();
        public List<CapitoloDef> capitoli = new List<CapitoloDef>(); // R025: capitoli del filmato (solo cutscene_intro)
        public float durataStimata; public string obiettivoFinale;
    }

    /// <summary>R025: capitolo del filmato iniziale: primo e ultimo id battuta. La regia e' in filmato_intro.json.</summary>
    [Serializable]
    public class CapitoloDef { public string id, titolo, epoca; public float durata, pausaSenzaTesto; public List<string> battute = new List<string>(); }

    [Serializable]
    public class BattutaDef
    {
        public string id; public string personaggio; public string testo; public string emozione; public string audio;
        public string azione; public string sfondo;
        public string posizione; // "sx" | "dx"
        public string capitolo, controllo; // R025: capitolo del filmato e targhetta dei controlli (salta, muovi, guarda, interagisci, inventario, mappa, indizio)
        public float durata;               // R025: secondi di lettura senza audio
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
    public class UiEnigmiImp { public string semplificatiNome = "", semplificatiSpiega = "", evidenziaNome = "", evidenziaSpiega = "", si = "Si", no = "No"; }
    /// <summary>R025: voce "Rivedi il filmato iniziale" della pausa.</summary>
    [Serializable]
    public class UiFilmatoMenu { public string rivedi = "Rivedi il filmato iniziale", nonQui = "Il filmato si rivede dal vicolo o da casa, non dentro un ricordo."; }
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
        public UiEnigmiImp enigmiImp = new UiEnigmiImp();
        public UiMappaImp mappaImp = new UiMappaImp();
        public UiFilmatoMenu filmato = new UiFilmatoMenu();

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
            if (enigmiImp == null) enigmiImp = d.enigmiImp;
            if (mappaImp == null) mappaImp = d.mappaImp;
            if (filmato == null) filmato = d.filmato;
            if (inventario == null) inventario = d.inventario;
            if (inventario.sezioni == null || inventario.sezioni.Count != 4) inventario.sezioni = d.inventario.sezioni;
            if (inventario.quaderno == null) inventario.quaderno = d.inventario.quaderno;
            if (inventario.mappa == null) inventario.mappa = d.inventario.mappa;
            return this;
        }
    }
    // ---------------------------------------------------------------- R019: minimappa e obiettivi (mappe.json)
    [Serializable]
    public class MappeFile
    {
        public MappaParametri parametri = new MappaParametri();
        public MappaTesti testi = new MappaTesti();
        public List<MappaScenaDef> scene = new List<MappaScenaDef>();
        public List<ObiettivoDef> obiettivi = new List<ObiettivoDef>();
    }

    /// <summary>Numeri di funzionamento (non testi): tempi e raggi.</summary>
    [Serializable]
    public class MappaParametri
    {
        public float minutiSenzaProgressi = 3f;   // modalita' Normale: dopo tanti minuti senza progressi l'obiettivo compare
        public float raggioNome = 14f;            // le etichette dei vicoli compaiono entro questa distanza (m)
        public float raggioArrivo = 2.2f;         // sotto questa distanza la X dell'obiettivo si "spegne"
        public float raggioVista = 20f;           // metri visibili intorno al giocatore nella minimappa, se la scena non lo dice
    }

    [Serializable] public class MappaLegendaVoce { public string glifo, testo; }

    /// <summary>Testi della minimappa, della mappa grande e delle righe dell'obiettivo (mai nel codice).</summary>
    [Serializable]
    public class MappaTesti
    {
        public string titoloGrande = "Pianta", pulsante = "Mappa", chiudi = "Chiudi", legendaTitolo = "Legenda", obiettivoTitolo = "Prossimo obiettivo";
        public string nessunObiettivo = "", obiettivoNascosto = "", capitoloFinito = "", aiuto = "", aiutoTouch = "", nord = "N";
        public string aiutoSpento = "";
        public List<MappaLegendaVoce> legenda = new List<MappaLegendaVoce>();
    }

    [Serializable] public class MappaRett { public float x0, x1, z0, z1; }
    [Serializable] public class MappaXZ { public float x, z; }

    /// <summary>Una strada, un vicolo, una piazza: nome, zone (rettangoli nel mondo) e punti dove scrivere il nome sulla pianta.</summary>
    [Serializable]
    public class StradaDef
    {
        public string id, nome, tipo;   // tipo: principale | vicolo | largo | rampa | cortile
        public List<MappaRett> zone = new List<MappaRett>();
        public List<MappaXZ> ancore = new List<MappaXZ>();
    }

    /// <summary>Punto d'interesse disegnato sulla pianta: glifo = porta | fontana | edicola | pozzo | lavatoio | uscita | stazione | bottega.</summary>
    [Serializable] public class MappaPuntoDef { public string id, glifo, nome; public float x, z; }

    [Serializable]
    public class MappaScenaDef
    {
        public string scena, titolo, tema;   // tema: normale | ricordo (look seppia)
        public float ax0, az0, ax1, az1, ppm = 8f;   // area coperta dalla pianta (scritta dal generatore) e pixel per metro
        public float raggio;                 // metri visibili intorno al giocatore nella minimappa (0 = valore dei parametri)
        public List<StradaDef> strade = new List<StradaDef>();
        public List<MappaPuntoDef> punti = new List<MappaPuntoDef>();
    }

    /// <summary>Dove portare il giocatore per un obiettivo, scena per scena (nella scena sbagliata si indica la porta da prendere).</summary>
    [Serializable] public class ObiettivoPuntoDef { public string scena, testo; public float x, z; }

    /// <summary>Un passo della storia: finche' non ha tutti i gettoni di "completo" e' il prossimo obiettivo (il primo incompleto della lista).</summary>
    [Serializable]
    public class ObiettivoDef
    {
        public string id, testo;
        public string capitolo;   // id del flashback del capitolo (es. "flashback_scarpe"): se e' tra i flashback completati il passo non e' piu' un obiettivo
        public List<string> completo = new List<string>();
        public List<ObiettivoPuntoDef> punti = new List<ObiettivoPuntoDef>();
    }

    /// <summary>Testi delle righe di Impostazioni &gt; Gioco legate alla minimappa (ui_menu.json, blocco mappaImp).</summary>
    [Serializable]
    public class UiMappaImp
    {
        public string aiutoNome = "Aiuto della mappa", aiutoSpiega = "";
        public string guidata = "Guidata", normale = "Normale", esploratore = "Esploratore";
        public string minimappaNome = "Minimappa", minimappaSpiega = "", nordFissoNome = "Nord in alto", nordFissoSpiega = "";
        public string si = "Si", no = "No";
    }

    [Serializable] public class CreditoVoce { public string asset, tipo, autore, licenza; }
    [Serializable] public class CreditiFile { public List<CreditoVoce> voci = new List<CreditoVoce>(); }
}
