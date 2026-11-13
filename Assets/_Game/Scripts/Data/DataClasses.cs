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
        public string sbloccatoDa;      // R012: id di un oggetto: se e' nella latta il luogo risulta sbloccato (anche senza scena: si mostra sbloccatoMsg)
        public string sbloccatoMsg;     // R012: messaggio quando il luogo e' sbloccato ma la sua scena non esiste ancora
    }

    [Serializable] public class OggettiFile { public List<OggettoDef> oggetti = new List<OggettoDef>(); }
    [Serializable]
    public class OggettoDef
    {
        public string id; public string nome; public string descrizione; public string significato;
        public string temaCivico; public string icona; public string flashback; public string cutscene; public string luogo;
        public string modello; // R023: prefab in Resources/Modelli/<modello> mostrato in 3D nella latta (vuoto = solo l'icona)
        public string cutsceneIngresso; // R012: cutscene (id) che parte prima di entrare nel ricordo (non quando lo si rivive)
        public bool titoliDiCoda;       // R016: l'oggetto del finale: dopo la cutscene di chiusura (e quella dei segreti, se trovati tutti) partono i titoli di coda e si torna al menu
        public IngressoDef ingresso;    // R012: oggetto d'ingresso creato a runtime in una scena (es. il fagotto di stracci nel cortile del basso)
    }

    /// <summary>R012: oggetto-ricordo che compare in una scena gia' costruita (nessuna ricostruzione della scena): pezzi di primitive come negli enigmi.</summary>
    [Serializable]
    public class IngressoDef
    {
        public string scena; public float x, y, z, yaw = 0f;
        public string richiedeFlashback;  // l'oggetto compare solo dopo che questo flashback e' stato completato
        public string prompt;
        public List<ParteDef> parti = new List<ParteDef>();
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
        public string bigliettoFare = "Cosa fare", bigliettoOttieni = "Cosa ottieni", bigliettoChiudi = "", btnSpiega = "?"; // R026: biglietto d'apertura
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
        public string punto;                 // R011: nome del punto nei rapporti R006 (vedi segreti.json); dx,dy,dz = spostamento dal punto
        public float dx, dy, dz;
        public string segreto;               // R011: id del segreto che si trova risolvendo questo enigma
        public string completoMsg;           // nascosto: testo aggiunto quando tutti gli oggetti sono trovati
        public List<string> strumenti = new List<string>();   // R012 tessuti: etichette dei quattro strumenti (controluce, filo, spessore, etichetta); monete: [pulsante]
        public List<string> diritti = new List<string>();     // R012 colonne: terza colonna (i diritti di oggi)
        public List<RigaTreDef> righe = new List<RigaTreDef>(); // R012 colonne: oggetto, mestiere e diritto giusti per ogni riga
        public float squilibrio;             // R012 bilancia: grammi in piu' sul piatto sinistro finche' il chiodo e' sotto il piatto
        public List<CollegamentoDef> collegamenti = new List<CollegamentoDef>(); // R013 bacheca: per ogni diceria (parte con azione "diceria", valore = indice) il documento giusto e il verdetto
        public string dopoRicomposto;        // R016 ricomponi: pezzi rivelati quando il documento e' ricomposto (timbro, croce)
        public List<FraseDef> frasi = new List<FraseDef>();   // R013 ciclostile: frasi candidate per la matrice (giusta = chiara e verificabile)
        public int giri;                     // R013 ciclostile: giri di manovella (fogli) da stampare
        public float ritmo;                  // R013 ciclostile: secondi minimi fra due giri (piu' veloce = il foglio sbava)
        public List<DataTimbroDef> date = new List<DataTimbroDef>(); // R013 datario: date da timbrare, in ordine (linea del tempo)
        public List<string> mesi = new List<string>();        // R013 datario: nomi dei mesi sul nastro (12)
        public List<string> anni = new List<string>();        // R013 datario: anni sul nastro
        public List<FiguraDef> figure = new List<FiguraDef>(); // R014 cartellini, cappotti, dialogo: le figure (parte "vuoto" con azione "figura", valore = indice) e che cosa spetta a ciascuna
        public List<FermataDef> fermate = new List<FermataDef>(); // R014 finestrino: i cartelli che passano nel finestrino, in ordine (le citta' da appuntare hanno citta = true)
        public float periodo;                // R014 finestrino: secondi fra due cartelli
        public string cutscene;              // R014: cutscene (id) che parte quando l'enigma e' risolto (mai quando si rivive il ricordo)
        public string cutsceneCapitolo;      // R014: capitolo di quella cutscene (vuoto = tutte le battute)
        public string cosaFare, cosaOttieni; // R026: le due righe del biglietto d'apertura (una frase all'imperativo; che cosa si ottiene)
        public FornoDef forno = new FornoDef();               // R015 forno: il modello del calore (farina, legna, serranda, clessidra)
        public List<SuonoDef> suoni = new List<SuonoDef>();    // R015 suoni: i suoni da riconoscere, in ordine (attrezzo = id della parte)
        public List<CordaDef> corde = new List<CordaDef>();    // R015 accordatura: le corde da accordare con i battimenti
        public List<ImprontaDef> impronte = new List<ImprontaDef>(); // R015 impronte: gli scarponi dei ragazzi (indice = valore delle parti "scarpone" e "orma")
        public List<int> note = new List<int>();               // R015 melodia: note (midi) dei segni colorati (indice = valore delle parti "nota")
        public List<FraseDef> frasi = new List<FraseDef>();    // R015 melodia: le frasi da ascoltare e ripetere
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
        public string errore = "";             // R012: messaggio gentile se l'oggetto in mano non e' quello giusto (usa con elenco di oggetti in richiede)
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
        public string controluce, filo, spessore; // R012 tessuti: che cosa si vede/sente con ciascuna prova
    }

    [Serializable] public class RigaTreDef { public string oggetto, mestiere, diritto, nota; }

    /// <summary>R013 bacheca: la diceria (indice) va collegata al documento con id "a"; verdetto "vera" o "falsa".</summary>
    [Serializable] public class CollegamentoDef { public string voce, a, verdetto, nota; }

    /// <summary>R013 ciclostile: frase candidata; giusta = chiara e verificabile; motivo = spiegazione gentile se scelta per sbaglio.</summary>
    [Serializable] public class FraseDef { public string testo; public bool giusta; public string motivo; }

    /// <summary>R013 datario: data (giorno, mese 1-12, anno) da timbrare; parte = pezzo rivelato sulla linea del tempo; etichetta = che cosa accadde.</summary>
    [Serializable] public class DataTimbroDef { public int giorno, mese, anno; public string etichetta, parte; }

    /// <summary>
    /// R014: una figura viva della vista ravvicinata (bambino della fila, riflesso nel vetro, bambino dello scompartimento). id = parte "vuoto" della figura.
    /// cartellini: cartellino = id della parte del cartellino giusto, collo = quota (locale) dove si appende. cappotti: cappotto = id del cappotto giusto,
    /// taglia = misura del bambino (i cappotti hanno valore = taglia), busto = quota (locale) del cappotto indossato, ok = messaggio quando lo indossa, dopo = pezzi rivelati.
    /// dialogo: paura = che cosa dice il bambino, risposte = le scelte di Amerigo (una giusta per bambino), ok = che cosa dice quando si e' calmato.
    /// </summary>
    [Serializable]
    public class FiguraDef
    {
        public string id, nome, cartellino, cappotto, ok, dopo, paura;
        public int taglia;
        public float collo, busto;
        public List<RispostaDef> risposte = new List<RispostaDef>();
    }

    /// <summary>R014 dialogo: una risposta di Amerigo. giusta = calma il bambino; richiede = oggetto della latta o gettone da mostrare; esito = reazione del bambino (mai un errore).</summary>
    [Serializable] public class RispostaDef { public string testo, richiede, esito; public bool giusta; }

    /// <summary>R014 finestrino: cartello che passa nel finestrino. citta = va appuntato sulla carta; dopo = pezzi rivelati sulla carta; ripete = che cosa dice il controllore se lo perdi; luce = 0 giorno .. 1 notte.</summary>
    /// <summary>R015 forno: gradi iniziali, finestra giusta (giusto0..giusto1), massimo, gradi per ciocco (legna), calo al secondo (raffredda, serranda aperta in piu'),
    /// secondi di cottura e tolleranze (presto = quanto prima si puo' sfornare, tardi = quanto dopo prima che bruci).</summary>
    [Serializable] public class FornoDef { public float inizio = 110f, giusto0 = 220f, giusto1 = 265f, massimo = 340f, legna = 42f, raffredda = 1.2f, serranda = 7f, cottura = 12f, presto = 2.5f, tardi = 4f; }

    /// <summary>R015 suoni: attrezzo = id della parte giusta, nome, tipo di suono sintetizzato (diapason | chiave | feltro | martelletto | corda), onda = descrizione del disegno, ok = che cosa dice Alcide.</summary>
    [Serializable] public class SuonoDef { public string attrezzo, nome, tipo, onda, ok; }

    /// <summary>R015 accordatura: id = parte della corda, nome, nota (midi), scarto iniziale in Hz dal diapason (= battimenti al secondo), passo = Hz per mezzo scatto, dopo = pezzi rivelati quando e' accordata.</summary>
    [Serializable] public class CordaDef { public string id, nome, dopo; public int nota = 69; public float scarto, passo = 0.5f; }

    /// <summary>R015 impronte: lo scarpone di un ragazzo (nome, numero, chiodi sulla suola, tacco).</summary>
    [Serializable] public class ImprontaDef { public string nome, tacco; public int misura, chiodi; }

    /// <summary>R015 melodia: una frase (indici dei segni colorati), secondi per nota, che cosa dice Alcide alla fine.</summary>
    [Serializable] public class FraseDef { public List<int> note = new List<int>(); public float tempo = 0.55f; public string ok; }

    [Serializable] public class FermataDef { public string testo, dopo, ripete; public bool citta; public float luce; }

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
        public string prompt;   // R013: testo del prompt (vuoto = "Raccogli: " + nome)
        public float fx, fy, fz; // R013 voce: punto (mondo) da cui viene la voce (finestra, balcone, lavatoio)
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
        public string rivivi = "", riviviNonQui = ""; // R011: rientrare in un ricordo gia' completato (per i segreti)
    }
    [Serializable]
    /// <summary>R016: testi dei titoli di coda (ui_menu.json, blocco "finale"). velocita = pixel al secondo.</summary>
    [Serializable] public class UiFinale { public string titolo = "", sottotitolo = "", fine = "", salta = ""; public float velocita = 55f; public List<string> righe = new List<string>(); }

    /// <summary>R028: testi del pulsante altoparlante e della riga delle Impostazioni.</summary>
    [Serializable] public class UiVoceNarratore { public string mancante = "", autoNome = "", autoSpiega = ""; }

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
    public class UiEnigmiImp { public string spiegazioneNome = "", spiegazioneSpiega = "", semplificatiNome = "", semplificatiSpiega = "", evidenziaNome = "", evidenziaSpiega = "", si = "Si", no = "No"; }
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
            new UiVoce { id = "filmato", etichetta = "Rivedi il filmato iniziale" },
            new UiVoce { id = "trofei", etichetta = "Trofei" }, new UiVoce { id = "crediti", etichetta = "Crediti" }, new UiVoce { id = "esci", etichetta = "Esci" }
        };
        public UiAnteprima anteprima = new UiAnteprima();
        public UiConferma conferma = new UiConferma();
        public UiRicordi ricordi = new UiRicordi();
        public UiCrediti crediti = new UiCrediti();
        public UiVoceNarratore voce = new UiVoceNarratore(); // R028: pulsante altoparlante del Narratore
        public UiFinale finale = new UiFinale();   // R016: titoli di coda
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

    /// <summary>R027: trofei.json. testi = interfaccia dell'album; trofei = elenco (condizione: vedi TrenoBambini.Album.Trofei).</summary>
    [Serializable] public class TrofeiTesti { public string titolo = "Trofei", sottotitolo = "", contatore = "{0} su {1}", bloccato = "", segreto = "", notifica = "{0}", indietro = "Indietro", famigliaStoria = "", famigliaEnigmi = "", famigliaEsplorazione = "", famigliaMissioni = "", famigliaCivici = ""; }
    [Serializable] public class TrofeoDef { public string id, titolo, descrizione, indizio, icona, condizione, famiglia; public bool segreto; }
    [Serializable] public class TrofeiFile { public TrofeiTesti testi = new TrofeiTesti(); public List<TrofeoDef> trofei = new List<TrofeoDef>(); }

    [Serializable] public class CreditoVoce { public string asset, tipo, autore, licenza; }
    [Serializable] public class CreditiFile { public List<CreditoVoce> voci = new List<CreditoVoce>(); }
}
