# R001 – UI spettacolare in stile antico

| | |
|---|---|
| **Scritta da** | Claude, PC scuola (senza Unity), 2026-11-05 |
| **Per** | Claude, PC casa (Unity 6000.6.7f1 + MCP for Unity) |
| **Quando** | Dopo aver chiuso e pubblicato il lavoro in corso (fase 2.5 e seguenti) |
| **Base** | `main` @ 4fd38aa + diario. Il codice UI potrebbe essere cambiato dopo: **il codice attuale vince** |
| **Priorità** | Alta: è la prima cosa che vede chi gioca (prof e compagni) |

## 1. Cosa ha chiesto l'utente (parole sue)
> MIGLIORARE LA UI NOTEVOLMENTE. Deve essere dinamica, bellissima e antica, aggiungi molti effetti alla ui,
> deve essere proprio di un gioco spettacolare, a partire da inventari, pulsanti, interagisci etc...

Tradotto in obiettivo: **ogni elemento dell'interfaccia deve sembrare un oggetto fisico della Napoli del 1946**
(carta, inchiostro, ottone, ceralacca, timbri, biglietti del treno) e **reagire con animazioni** a ogni azione
del giocatore. Il livello di cura deve essere quello di un gioco commerciale, non di un prototipo.

## 2. Situazione di partenza (al 3/11)
- UI uGUI costruita **interamente da codice**: `UIRoot` (layer Hud/Schermate/Cutscene/Modali/Dialoghi/Toast),
  `UIFactory` (pannelli piatti a colore pieno, pulsanti rettangolari, palette Carta/Inchiostro/Accento…),
  `InventoryPanel`, `PausePanel`, `TitleScreen`, `Fader`, `CutscenePlayer`, pannelli enigmi in `Scripts/Puzzles`.
- Grafica procedurale in `Editor/ArtGenerator.cs` (classe `Pix`: poligoni, ellissi, rumore, vignetta, supersampling).
  Unico sprite UI: `Resources/Sprites/ui/vignetta.png`.
- **Nessuna animazione** della UI (a parte il fader), nessun suono UI, nessuna texture sui pannelli.

## 3. Vincoli (non negoziabili)
1. **Tutto generato da codice / menu Treno ▸ …**, come il resto del progetto (regola di CLAUDE.md).
   Le texture nuove si generano in `ArtGenerator` (o in un nuovo `UIArtGenerator`) e si salvano in
   `Resources/Sprites/ui/`. Niente costruzione a mano nell'editor.
2. **WebGL su PC della scuola, Chromebook e LIM.** Budget: deve restare fluido (obiettivo 60 fps, minimo 30)
   su un Chromebook. Quindi:
   - animazioni su `RectTransform`/`CanvasGroup`/colore, **niente** post-processing a schermo intero;
   - elementi che si animano ogni frame su un **Canvas figlio separato** (per non ricostruire tutta la UI);
   - pool per le particelle UI, zero allocazioni per frame (niente LINQ, niente lambda nuove in Update);
   - al massimo 1–2 shader UI custom semplici; se usati, vanno inclusi nella build (materiale in `Resources`
     o *Always Included Shaders*) e provati in WebGL;
   - aumento dimensione build: indicativamente **< 4 MB** (font + texture + suoni).
3. **Nessuna libreria esterna nuova** (niente DOTween/LeanTween): scrivere un piccolo motore di tween interno.
   Se sembra davvero indispensabile un pacchetto, chiedere all'utente.
4. **Originalità:** nessuna grafica, font o suono presi dal film/libro. Font solo con licenza libera
   (OFL / Apache) e file di licenza nel progetto. Suoni generati da codice o CC0 con file crediti.
5. **Didattica non punitiva:** gli errori negli enigmi non devono sembrare "game over" (niente rosso
   aggressivo, niente suoni duri). Errore = oscillazione gentile + inchiostro che sbiadisce + messaggio.
6. **Leggibilità prima dell'estetica:** testo grande (LIM in fondo all'aula), contrasto alto, pulsanti
   touch ≥ 56 px (come ora). Effetti mai sopra il testo mentre lo si legge.
7. **Accessibilità:** opzione **"Effetti ridotti"** nel menu Pausa (salvata in `SaveData`), che toglie
   tremolii, particelle e movimenti grandi lasciando solo dissolvenze brevi. Attivarla anche in automatico
   se gli fps restano sotto 25 per qualche secondo (con un toast che lo dice).
8. Testi della UI sempre in italiano, fuori dal codice quando sono narrativi (regola JSON del progetto).

## 4. Direzione artistica
**Tema:** "l'archivio di un ricordo". Tutto sembra conservato in una valigia di cartone del 1946.

| Materiale | Dove si usa |
|---|---|
| Carta invecchiata con fibre, macchie, bordi irregolari/bruciacchiati | pannelli, dialoghi, inventario |
| Inchiostro seppia/nero, scritte a macchina da scrivere | testi, toast, titoli |
| Ottone brunito con riflesso | cornici, pulsanti principali, ruote del lucchetto |
| Ceralacca rossa scura | pulsante di conferma ("OK", "Avanti") |
| Timbri a inchiostro | feedback di successo ("RISOLTO", "RICORDO RITROVATO") |
| Biglietto ferroviario con punzonature | contatore enigmi |
| Spago e cartellini | etichette, prompt, pulsanti secondari |
| Grana di pellicola e vignetta | transizioni e flashback |

**Palette:** mantenere quella di `UIFactory` come base e aggiungere: ottone `#B08D57`, ottone luce `#E2C48A`,
ceralacca `#7A1F1A`, inchiostro timbro `#2E3A59` (blu antico), carta chiara `#F1E6CC`.
Accentrare tutto in una classe di tema (es. `UITheme`) invece dei colori sparsi.

**Font (proposta, verificare licenze e caratteri italiani àèéìòù «» — ’):**
- titoli: un serif da stampa d'epoca (es. *IM Fell English* o *Playfair Display*, OFL);
- testi lunghi: un serif molto leggibile (es. *Cormorant Garamond* semibold o *Libre Baskerville*, OFL);
- toast/biglietti/etichette: macchina da scrivere (es. *Special Elite*, Apache 2.0).
Generare i TMP Font Asset (con fallback a LiberationSans) da menu editor.

**Ritmo delle animazioni:** apertura 0,25–0,45 s con easing *OutBack* o *OutCubic*, chiusura più rapida
(0,15–0,25 s, *InCubic*). Sempre con tempo *unscaled* (la pausa mette `timeScale` a 0).

## 5. Architettura proposta
Nuova cartella `Scripts/UI/Fx/` (namespace `TrenoBambini.UI.Fx`):

| Classe | Compito |
|---|---|
| `UITheme` | colori, font, durate, curve; un solo punto per cambiare lo stile |
| `UITween` | mini motore di tween (posizione, scala, rotazione, alpha, colore, valore float), easing, ritardo, sequenze, `unscaledTime`, annullamento per target; un solo `MonoBehaviour` runner con lista in pool |
| `ButtonFx` | comportamento animato dei pulsanti (vedi §6.1); si aggiunge in `UIFactory.MakeButton` così tutti i pulsanti esistenti lo ricevono gratis |
| `PanelFx` | apertura/chiusura animata dei pannelli modali (`Show()`/`Hide(Action onDone)`) |
| `Typewriter` | testo che compare lettera per lettera con suono, salto al click, rispetto dei tag rich text |
| `UIParticles` | particelle UI a pool (polvere, schizzi d'inchiostro, scintille dorate, coriandoli di carta) |
| `UISfx` | suoni UI con variazione casuale di pitch; clip generate in editor (`Treno ▸ UI ▸ Genera suoni`) o CC0 |
| `Shake` / `Wobble` | scossa del timbro, oscillazione d'errore, dondolio dei cartellini |
| `UIArtGenerator` (Editor) | texture procedurali: carta 9-slice con bordi strappati, cornice ottone 9-slice, ceralacca, timbri, biglietto, spago, icone tasti, grana pellicola |

`UIFactory` va aggiornato in modo che **le chiamate esistenti producano già la UI nuova**
(es. `MakePanel` → carta texturizzata, `MakeButton` → pulsante ottone/ceralacca con `ButtonFx`), così
i pannelli degli enigmi migliorano subito; poi si rifiniscono uno per uno.

## 6. Specifica elemento per elemento

### 6.1 Pulsanti (tre varianti)
- **Primario** (Gioca, Verifica, Conferma): targa di ottone con bordo inciso e scritta in rilievo.
- **Secondario** (Indietro, Indizio, Chiudi): cartellino di carta con foro e spago.
- **Conferma speciale** (Avanti dopo un successo, OK nei dialoghi importanti): sigillo di ceralacca.
- Stati e animazioni:
  - *riposo*: leggerissimo "respiro" (scala 1→1,015) solo sul pulsante consigliato;
  - *hover*: sale di 4 px, ombra più lunga, **riflesso di luce** che attraversa l'ottone (una passata);
  - *pressione*: schiacciamento (scala 0,94) come un timbro + piccolo schizzo di polvere/inchiostro;
  - *rilascio*: ritorno elastico (*OutBack*) + suono (click metallico per ottone, fruscio per carta,
    "toc" sordo per ceralacca);
  - *disabilitato*: carta sbiadita, nessuna ombra.
- Su touch hover non esiste: pressione e rilascio devono bastare a dare il feedback.

### 6.2 Prompt "Interagisci"
- **Indicatore nel mondo:** sopra l'oggetto interagibile più vicino, un piccolo **rombo dorato** (o occhio
  stilizzato) che fluttua e pulsa; compare con scala da 0 e sparisce dissolvendosi quando ci si allontana.
  Va ancorato alla posizione 3D (screen point dalla camera) e nascosto se l'oggetto è dietro la camera.
- **Prompt a schermo:** cartellino appeso a uno spago che **cade dall'alto e dondola** fino a fermarsi,
  con icona del tasto **E** disegnata come tasto di macchina da scrivere (su touch: icona mano) e il testo
  ("Entra: Basso di Antonietta"). Cambio di testo: il cartellino gira su se stesso (rotazione Y finta con scala X).
- Alla pressione di E: il tasto si abbassa, il cartellino fa un piccolo salto e sparisce.
- Gli oggetti da ricordo (ItemPickup) hanno un prompt speciale con bordo dorato e particelle di luce.

### 6.3 Inventario (tasto I)
- Si apre come una **valigia di cartone** che si spalanca: il coperchio ruota, poi gli scomparti entrano a cascata
  (ritardo 0,04 s tra uno e l'altro). Sfondo del gioco sfocato o scurito con vignetta.
- Ogni oggetto è una **fotografia/cartolina** con angolini e bordo bianco, leggermente storta (rotazione casuale
  ±3°). Hover/selezione: si raddrizza, si solleva, **inclinazione parallasse** seguendo il mouse.
- Slot vuoti: sagoma tratteggiata a matita con "?" e il nome del luogo dove si trova (stimola l'esplorazione).
- Dettaglio oggetto: la cartolina si ingrandisce al centro; un pulsante **"Gira"** mostra il retro, scritto
  a mano/macchina, con *significato* e *tema civico* come **timbro postale**. Animazione di girata con scala X.
- **Nuovo oggetto ottenuto:** dopo la cutscene, l'icona vola dal centro dello schermo verso un'icona-valigia
  nell'HUD, lasciando una scia di scintille; la valigia sobbalza e mostra un "+1" a macchina da scrivere.
- Chiusura: gli scomparti escono in ordine inverso, il coperchio si chiude con un "tump".

### 6.4 HUD
- In alto a sinistra un **biglietto del treno** "Napoli → …" che fa da contatore enigmi del flashback:
  4 caselle; quando un enigma è risolto una **punzonatrice** buca la casella (animazione + suono + coriandoli
  di carta che cadono). Sostituisce l'attuale testo "Enigmi: n/4".
- Icona valigia (inventario) e icona orologio da taschino (pausa) sempre visibili e cliccabili
  (utili su LIM), con lo stesso `ButtonFx`.
- L'HUD entra con una slide morbida quando si arriva in una scena ed esce durante cutscene e dialoghi.

### 6.5 Toast e messaggi brevi
- **Telegramma**: striscia di carta che scivola dal lato con il testo battuto a macchina (`Typewriter`, veloce),
  resta, poi si accartoccia/sfuma. Più toast si impilano invece di sovrascriversi.

### 6.6 Dialoghi modali (introduzione, conclusione, spiegazioni)
- **Lettera** su carta con bordo strappato, titolo in serif d'epoca, testo con `Typewriter` (click = completa).
- Apertura: la lettera arriva dal basso ruotando leggermente e si "posa" (*OutBack*), con ombra che si stringe.
- Pulsante principale = **sigillo di ceralacca**; dialoghi di conferma: ceralacca + cartellino "Annulla".

### 6.7 Menu Pausa (Esc)
- Il gioco si scurisce, entra un **quaderno di scuola anni '40** (righe, margine rosso, copertina nera).
  Le voci sono scritte a mano/inchiostro con una **sottolineatura che si disegna** in hover.
- Contiene: Riprendi, Volume (slider come una **leva d'ottone**), **Effetti ridotti**, Torna al titolo.

### 6.8 Schermata del titolo
- Titolo "Il Treno dei Bambini" con effetto **stampa tipografica**: le lettere scendono e si imprimono una per
  una con un leggero schiacciamento, poi una passata di luce dorata.
- Atmosfera: polvere che fluttua nella luce, **sbuffi di vapore** del treno che salgono lenti dal basso
  (sprite di fumo generato, pochi e grandi, a bassa opacità), grana pellicola leggera.
- Pulsanti che entrano a cascata. Se c'è un salvataggio, "Continua" è il pulsante consigliato (respiro).

### 6.9 Pannelli degli enigmi
Base comune (`PuzzlePanel`): pannello carta su cornice d'ottone, titolo con numero romano dell'enigma,
pulsante **Indizio** a forma di lampadina/lente che si illumina quando l'indizio è disponibile.
- **Quiz:** le opzioni sono cartoncini; scelta giusta = timbro verde "ESATTO" che sbatte + scintille;
  sbagliata = il cartoncino oscilla, l'inchiostro sbiadisce, si gira mostrando la spiegazione gentile.
- **Lucchetto:** lucchetto d'ottone con **ruote che girano davvero** (le cifre scorrono verticalmente con inerzia
  e scatto), suono di clic per ogni cifra. Codice giusto: l'arco si apre con un salto, luce dorata.
  Codice sbagliato: piccola scossa e le ruote tornano indietro.
- **Ordina:** carte trascinabili (drag & drop vero, oltre alle frecce per accessibilità) con ombra che si
  allunga quando le sollevi e le altre carte che si spostano per fare spazio. Verifica: le carte al posto giusto
  ricevono una puntina/timbro.
- **Abbina:** quando si crea una coppia, uno **spago rosso** si tende tra le due voci (linea animata con leggera
  curvatura); coppia sbagliata: lo spago si allenta e cade.
- **Nascosto:** contatore a forma di **lente d'ingrandimento** con i pallini degli oggetti; quando si trova un
  oggetto, la sua icona vola nel contatore.
- **Successo dell'enigma (tutti):** timbro grande "RISOLTO" che sbatte sul pannello con micro-scossa
  dello schermo (solo se effetti non ridotti), polvere, suono di timbro, poi la spiegazione con `Typewriter`.

### 6.10 Transizioni
- **Fade normale** (cambio scena): dissolvenza con vignetta che si chiude a iride.
- **Ricordo** (entrata/uscita flashback): la pellicola "brucia" dal centro verso i bordi (shader UI semplice
  con soglia su texture di rumore, oppure sequenza di sprite generati), virata seppia, grana e righe di pellicola,
  suono di proiettore. Uscita: effetto inverso.
- Durante i flashback: leggera grana e vignetta seppia sull'HUD (overlay statico/scorrevole, economico).

### 6.11 Cutscene
- **Bande cinematografiche** nere che entrano dall'alto e dal basso.
- Ritratti in **cornice ovale** d'ottone che entrano dal lato (azioni `entra_sx` / `entra_dx` già esistenti),
  il personaggio che parla è illuminato, l'altro più scuro.
- Nome del personaggio su **targhetta** sopra la casella; testo con `Typewriter` (velocità coordinata con la voce
  se c'è l'audio); indicatore "continua" = piccola freccia/penna che oscilla.
- Le azioni esistenti (`zoom`, `scuoti`, `dissolvenza`) usano il nuovo `UITween`/`Shake`.

### 6.12 Extra
- **Cursore personalizzato** su PC (pennino stilografico; versione "mano" sopra gli interagibili). Disattivabile
  se crea problemi in WebGL.
- **Suoni UI**: hover (fruscio leggerissimo), click ottone, click carta, timbro, punzonatrice, macchina da scrivere
  (tasti + campanello a fine riga per testi lunghi), valigia apre/chiude, sigillo. Volume legato al volume globale.

## 7. Ordine di lavoro consigliato (un commit o più per punto)
1. `UITheme` + `UITween` + `UISfx` (base) e menu editor per generare texture e suoni UI.
2. Texture procedurali: carta 9-slice, cornice ottone, ceralacca, timbri, biglietto, cartellino, grana.
3. Font TMP + licenze.
4. `UIFactory` aggiornato + `ButtonFx` + `PanelFx` → **build WebGL di prova e misura fps** prima di proseguire.
5. Prompt interagisci + indicatore nel mondo.
6. Inventario.
7. HUD (biglietto, icone) + toast telegramma + dialoghi lettera + `Typewriter`.
8. Pausa (con Effetti ridotti) + titolo.
9. Pannelli enigmi uno per uno.
10. Transizioni + cutscene.
11. Rifinitura, prova completa del flashback delle scarpe, build WebGL finale, Rapporto.

Dopo ogni punto: `read_console`, zero errori, e prova nel Play Mode.

## 8. Criteri di accettazione
- [ ] Nessun pannello a colore piatto rimasto: tutta la UI usa i materiali del §4.
- [ ] Ogni pulsante ha hover, pressione, rilascio animati e suono; funziona con mouse, touch e tastiera dove previsto.
- [ ] Inventario con apertura a valigia, cartoline, dettaglio con retro, animazione "nuovo oggetto".
- [ ] Prompt interagisci con indicatore nel mondo e cartellino animato.
- [ ] Contatore enigmi a biglietto punzonato.
- [ ] Toast, dialoghi, pausa, titolo, cutscene e tutti e 5 i tipi di enigma restilizzati e animati.
- [ ] Transizione "Ricordo" nuova.
- [ ] Opzione "Effetti ridotti" funzionante e salvata; attivazione automatica a fps bassi.
- [ ] Animazioni funzionanti anche in pausa (`unscaledTime`) e mai bloccanti per l'input più di 0,5 s.
- [ ] Testi leggibili sulla LIM (dimensione minima del corpo ~24 px a 1280×720).
- [ ] Build WebGL fluida (≥ 30 fps su un PC scolastico/Chromebook; annotare gli fps misurati nel Rapporto).
- [ ] Dimensione build aumentata di meno di ~4 MB (annotare prima/dopo).
- [ ] Font e suoni con licenza documentata (`Assets/_Game/Art/Licenze/` o simile) e crediti nel README.
- [ ] Zero errori in console, flashback delle scarpe giocabile dall'inizio alla fine.

## 9. Rapporto (da compilare da chi implementa)
**Implementata da:** … · **Date:** … · **Commit finali:** …

### Fatto
- …

### Scostamenti dalla specifica (e perché)
- …

### Domande aperte per l'utente
- …

### Come testare
1. …

### Misure
- fps (PC / Chromebook / LIM): …
- dimensione build prima → dopo: …

### Media
- `Docs/richieste/media/R001/…`
