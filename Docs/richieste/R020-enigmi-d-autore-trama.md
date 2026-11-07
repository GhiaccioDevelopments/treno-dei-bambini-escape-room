# R020 – Enigmi d'autore in ogni stanza: quadri, codici, meccanismi, tutto dalla trama

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3. **Il codice attuale vince** |
| **Dipende da** | **R009** (sistema "Esamina", vista ravvicinata, meccanismi 3D, `richiede/produce`): R020 ne è il **contenuto** e lo estende a tutte le stanze. Sostituisce/assorbe **R010** se non ancora implementata. Prima di **R011** (easter egg) e dei capitoli R012–R016, che devono seguirne le regole |
| **Priorità** | Alta |

## 1. Cosa ha chiesto l'utente (parole sue)
> Nelle varie stanze, gli enigmi non possono essere dei blocchi viola, gestisci questo enigma come i migliori giochi di
> enigmi del mondo, con tipo quadri, codici etc (tutto legato alla trama de "Il treno dei Bambini" ovviamente)

La richiesta è stata ripetuta (vedi R009): i blocchi viola sono ancora in gioco. **Priorità assoluta: toglierli.**

## 2. Regole
- **`Docs/TRAMA.md` è vincolante**: ogni enigma nasce da un oggetto, un luogo, un personaggio o un fatto della trama o del
  contesto storico (§3–§7). Nessuna citazione dal libro/film; testi nostri. Persone reali mai con battute (Decisione 5).
- Ogni enigma è un **oggetto d'epoca vero** (modello 3D, Napoli/Modena 1946) dentro la stanza, mai un marcatore.
- **Grandi giochi di enigmi come metro** (meccanismi, non contenuti): *The Room* (oggetti a strati, cassetti segreti),
  *Myst/Riven* (indizi nell'ambiente), *The Witness* (la stanza insegna la regola), *Obra Dinn* (deduzione), *Layton*
  (brevità e indizi a livelli), *Unpacking* (raccontare con gli oggetti), *Escape Simulator* (codici incrociati tra stanze).
- **Catene**, non enigmi isolati: la soluzione di un enigma dà un oggetto/indizio che serve al successivo (`richiede/produce`, R009).
- Didattica non punitiva invariata: errore = feedback gentile + spiegazione + riprova; pulsante Indizio a 3 livelli.
- Ogni enigma porta con sé una **scoperta civica** (scheda breve dopo la soluzione), coerente con il `temaCivico`.
- Fallback a pannello 2D ("Enigmi semplificati") per LIM e accessibilità.

## 3. Catalogo di tipi di enigma (da usare e mescolare)
| Tipo | Esempio legato alla trama |
|---|---|
| **Quadro / immagine** | Nel basso, il santino o la fotografia incorniciata della famiglia: dietro la cornice un numero; oppure un quadro storto che, raddrizzato, allinea un disegno con una crepa del muro |
| **Codice da osservazione** | Il calendario del 1946 appeso: giorni cerchiati a matita (il 2 giugno, la data della partenza) → combinazione del baule |
| **Lucchetto a rotelle 3D** | Cassetta di latta delle cose preziose di Antonietta, combinazione ricavata dal conto dei punti del gioco delle scarpe |
| **Deduzione dalle scarpe** | Il gioco dei mestieri (Decisione 8): da usura, chiodi, fango, vernice delle scarpe dei passanti dedurre il mestiere; la sequenza dei mestieri apre la porta della bottega |
| **Documento da ricomporre** | Il volantino del Comitato strappato in pezzi sparsi nel vicolo, da ricomporre e leggere (contro le dicerie) |
| **Testo nascosto / luce** | Una lettera che si legge solo alla luce della lampada a petrolio, o in controluce alla finestra |
| **Meccanismo a strati** | Il baule sotto il letto: doppio fondo, poi una scatolina, poi la ricevuta del Monte dei pegni (oggetto del finale, Decisione 6) |
| **Suono / musica** | (capitolo Violino) note da riprodurre ascoltando Alcide; l'accordatore che insegna l'orecchio |
| **Mappa / percorso** | Il percorso del treno Napoli → Modena sulla carta ferroviaria: ordinare le stazioni per sbloccare la valigia |
| **Vero/falso con fonti** | Le dicerie dell'epoca (Decisione 10) da smontare confrontando giornali e manifesti trovati nella stanza |
| **Oggetti in inventario usati nel mondo** | Lo spago per legare il fagotto di stracci, la chiave nel lucchetto, la mela annurca (dettaglio) nella valigia |

## 4. Specifica per stanza (questa richiesta copre le stanze **esistenti**)
- **Hub vicolo (1946)**: 2–3 enigmi ambientali brevi che guidano verso il basso (es. edicola votiva con i numeri civici,
  bucato steso che "compone" un messaggio, insegna del calzolaio con le forme delle scarpe).
- **Basso di Antonietta**: 3–4 enigmi a catena con quadro/fotografia, calendario, cassetta di latta, baule col doppio fondo.
  Il punto d'arrivo è l'oggetto che apre il ricordo (Le scarpe).
- **Flashback "Le scarpe"**: ridisegno dei 4 enigmi attuali (`flashback_scarpe.json`) con i nuovi meccanismi, mantenendo
  ID, temi civici e ID delle battute del doppiaggio. Lo schema dettagliato **va approvato dall'utente prima dello sviluppo**.
- Per ogni stanza: **schema degli enigmi** (tabella: oggetto, meccanismo, indizi a 3 livelli, cosa produce, tema civico,
  fonte nella TRAMA) mostrato all'utente con il pannello; si sviluppa dopo l'ok.

## 5. Dati
- Estendere i JSON come in R009 (`modello`, `vista`, `meccanismo`, `richiede`, `produce`, `indizi[3]`, `scheda`), senza rompere
  i tipi esistenti; i nuovi meccanismi (quadro, luce, ricomposizione, deduzione) come nuovi `tipo` o `meccanismo`.
- Modelli 3D d'epoca da fonti CC0 (preferito) o CC-BY, annotati in `CREDITS.md`, con LOD.

## 6. Ordine di lavoro consigliato
1. Trovare e **togliere i blocchi viola** ovunque (screenshot prima/dopo). 2. Se R009 non è ancora fatta, farne prima
   il nucleo (Esamina + vista ravvicinata + un meccanismo). 3. Schema enigmi delle 3 stanze → ok dell'utente.
4. Modelli e meccanismi, una stanza alla volta (basso per primo). 5. Schede civiche e indizi. 6. Prova completa del capitolo.

## 7. Criteri di accettazione
- [ ] Nessun blocco/marcatore viola o colorato in nessuna scena.
- [ ] Ogni enigma è un oggetto d'epoca esaminabile, collegato a un elemento citato di `TRAMA.md`.
- [ ] Almeno: un quadro/foto, un codice da osservazione, un meccanismo a strati, una deduzione dalle scarpe.
- [ ] Catena completa e giocabile hub → basso → flashback, con indizi a 3 livelli e schede civiche.
- [ ] Mouse, tastiera, touch; fallback 2D; ID doppiaggio invariati; console pulita; GIF in `Docs/richieste/media/R020/`.

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R020_schema.md`](schemi/R020_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 "Napoli della memoria": hub e basso hanno l'aspetto del 1946 e si giocano con Amerigo adulto. Il 1994 vero c'è solo in R016.
  - D2 passanti del ricordo: figure intere MakeHuman, ferme e velate.
  - Correzioni: nessuna combinazione dal "conto dei punti" (Decisione 8 di TRAMA); la ricevuta del pegno del violino passa a R016; R010 §4 assorbita (quiz sul 1948 e sul 1989 senza documenti impossibili nel 1946).

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto
_(da compilare a fine lavoro)_
