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
- [x] Nessun blocco/marcatore viola o colorato in nessuna scena.
- [x] Ogni enigma è un oggetto d'epoca esaminabile, collegato a un elemento citato di `TRAMA.md`.
- [x] Almeno: un quadro/foto, un codice da osservazione, un meccanismo a strati, una deduzione dalle scarpe.
- [x] Catena completa e giocabile hub → basso → flashback, con indizi a 3 livelli e schede civiche (provata in automatico).
- [x] Mouse, tastiera; fallback 2D; ID doppiaggio invariati; console pulita. Non fatti: touch su schermo vero, GIF (al loro posto schermate in `Docs/richieste/media/R020/`).

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R020_schema.md`](schemi/R020_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 "Napoli della memoria": hub e basso hanno l'aspetto del 1946 e si giocano con Amerigo adulto. Il 1994 vero c'è solo in R016.
  - D2 passanti del ricordo: figure intere MakeHuman, ferme e velate.
  - Correzioni: nessuna combinazione dal "conto dei punti" (Decisione 8 di TRAMA); la ricevuta del pegno del violino passa a R016; R010 §4 assorbita (quiz sul 1948 e sul 1989 senza documenti impossibili nel 1946).

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto

**Branch:** `richiesta/R020-enigmi-d-autore-trama` (contiene anche le specifiche di R009 e R010, assorbite) · **Base:** `feature/upgrade-realistico` @ 14880ac (R023 inclusa) · **Eseguito:** PC casa, 2026-11-08, Claude (Opus 5.5 come sottomente).

### 1. Piano per stanza (fase 1, scritto prima dell'implementazione)

Tutto nei JSON: `enigmi_mondo.json` (hub e basso, nuovo) e `flashback_scarpe.json` (ricordo). Gli oggetti sono pezzi di primitive con materiali d'epoca, costruiti da codice leggendo il JSON: nessun modello nuovo, nessun file in LFS.

| Stanza | Enigma (`id`) | Dove nel mondo | Meccanismo | Produce |
|---|---|---|---|---|
| Hub vicolo | `enigma_bucato` "Il filo del bucato" | filo sopra lo slargo di Antonietta, davanti alla porta del basso (-6,05 / 3,95 / -2) | oggetto: tocca i tre panni con la toppa a quadri | `porta_trovata` |
| Hub vicolo | `enigma_exvoto` "L'ex-voto del piede" | edicola votiva grande (punto 1 di R006), (-7,35 / 4,2 / 37) | oggetto: gira l'ex-voto a forma di piede, prendi la chiave | `chiave_basso` |
| Hub vicolo (facoltativo) | `enigma_manifesti` "I manifesti incollati" | bacheca della traversa T2 (14 / 1,75 / -10,1), rimpiazza il cartello | strati: stacca due manifesti, leggi l'avviso | `pagina_treni` |
| Basso | `enigma_foto` "La fotografia storta" | cornice sopra la credenza (-4,46 / 1,72 / 1,5) | quadro: raddrizza (3 clic), fermaglio, prendi il cartoncino | `cartoncino_segni` |
| Basso | `enigma_calendario` "Il calendario del '46" | calendario della cucina, rimpiazza quello di scena | codice da osservazione: conta le scarpette per mese, 4-2-3-1 (rotelle) | (id risolto) |
| Basso | `enigma_cassetta` "La cassetta di latta" | mensola dei lumini (0,62 / 1,06 / 3,25) | lucchetto a rotelle 3D, codice 4231 | `chiavetta_baule` |
| Basso | `enigma_baule` "Il baule dell'alcova" | cassapanca ai piedi del letto, rimpiazza il modello di scena | strati: serratura (chiavetta in mano), coperta, tre assi in ordine (scarpa, croce, treno), scatolina, scarpe | le scarpe: avvia il ricordo |
| Ricordo | `enigma_passanti` (nuovo) | gradino con i nomi dei mestieri, all'ingresso della via | deduzione dalle scarpe di 6 passanti, conferma a gruppi di 3 | `calzolaio_riconosciuto`, cifra 1 |
| Ricordo | `enigma_nascosto` "Il baule delle scarpe" | le 5 calzature nella via + forme di legno sulla mensola del calzolaio | cerca, poi inventario attivo: ogni calzatura sulla sua forma | cifra 9 |
| Ricordo | `enigma_quiz` "La cassetta dei diritti" | banco del calzolaio | documenti: tre fogli, si indica la riga che risponde | cifra 8 |
| Ricordo | `enigma_ordina` "Il calendario della memoria" | ritagli di giornale dal barbiere | trascinare i sei ritagli in ordine | cifra 9 |
| Ricordo | `enigma_lucchetto` "Il lucchetto dell'uscita" | porta luminosa in fondo alla piazzetta | lucchetto a rotelle 3D, 1989 dalle quattro cifre | apre l'uscita |

Catene: hub (bucato + ex-voto) apre la porta del basso; basso foto → calendario → cassetta → baule → ricordo; ricordo passanti → forme → documenti, passanti → ritagli, tutti → lucchetto. Requisiti nei campi `richiede` / `produce` del JSON.

### 2. Cosa c'è ora

**Sistema (R009).** `EnigmaMondo` (oggetto d'epoca nel mondo, prompt "E - Esamina: ..."), `EsaminaView` (vista ravvicinata: camera propria lontana dal mondo, trascina per girare, rotella per avvicinare, clic sulle parti, Tab/Invio da tastiera, Esc per uscire, Indizio a 3 livelli, errore gentile con riprova, timbro RISOLTO, scheda civica), `MondoFab` (pezzi di primitive con materiali URP e texture procedurali), `MondoStato` (gettoni salvati in `SaveData.oggettiEnigma`), `EnigmiSelfTest`. Meccanismi: oggetto (azioni sui pezzi: leggi, gira, ruota, rivela, segna, muovi, premi, prendi, usa, verifica, errore), ruote a cifre 3D, deduzione, documenti; `ordina` riusa il pannello con trascinamento. Inventario attivo: l'oggetto in mano (in alto a destra) si usa su una parte; la chiave apre da sola la porta dell'hub (nessuna scelta in mano: la porta controlla i gettoni e dice che la chiave gira). Impostazioni, scheda Accessibilita': "Enigmi semplificati" (pannelli 2D: lucchetto, ordina, quiz; ogni enigma ha le domande per questa versione) ed "Evidenzia oggetti interattivi" (brillio leggero, spento di default; si accende da solo dopo 150 s senza progressi). Il quaderno mostra appunti, indizi e oggetti ottenuti degli enigmi risolti.

**Blocchi viola.** Rimossi: `PuzzleStation` e i marcatori colorati non esistono piu' (`EscapeRoomController` crea oggetti d'epoca). Causa piu' probabile (non riprodotta nell'editor, dove il materiale predefinito dei primitivi diventa URP Lit): `PrimitiveFactory` usava `GameObject.CreatePrimitive` con il materiale predefinito built-in, che in un player URP senza shader Standard compila in magenta; ora usa materiali URP Lit creati da codice. La prova cerca materiali rotti (shader mancante o `Hidden/`) in hub, basso e ricordo.

**Dati e doppiaggio.** ID di `cutscene_scarpe` e delle battute invariati; ID degli enigmi del ricordo invariati (+ `enigma_passanti`). Schema esteso e documentato in `CLAUDE.md`.

### 3. Prova (fase 3)

`EnigmiSelfTest` (`-enigmitest [-enigmitestout cartella]`, solo editor e build di sviluppo). Gioca davvero: nuova partita, porta del basso chiusa, enigmi bloccati dai requisiti, per ogni enigma errore gentile + messaggio, i tre indizi, soluzione con **mouse virtuale** (raggi sulle parti, rotelle), salvataggio, "Continua", ricarica, versione semplificata (pannello del lucchetto), assenza di materiali rotti.

- Editor (Play): 99 PASS, 0 FAIL (prima e seconda versione).
- Build Windows di sviluppo (`C:/Users/rosan/treno-build/Win`, "4 URP assets included in build"): **99 PASS, 0 FAIL** sulla versione finale (log completo in `media/R020/prova_enigmi_build_windows.txt`). Il controllo dei materiali rotti passa in hub, basso e ricordo anche nel player.
- Console dell'editor pulita (0 errori, 0 avvisi) dopo ogni modifica.
- Screenshot 1280x720 JPG in `Docs/richieste/media/R020/` (per ogni enigma: aperto, errore, indizio, risolto; `mondo_*.jpg` = oggetto nel mondo).
- FPS (build Windows di sviluppo, 1280x720, vsync spento, 10 s per scena): hub Media 296 (1% peggiore 122) · Alta 256 (113); basso Media 389 (118) · Alta 262 (85, seconda misura da 15 s; la prima misura da 10 s ha avuto un singolo scatto di 143 ms subito dopo il caricamento, 1% peggiore 20: non si ripete); ricordo Media 240 (62) · Alta 207 (57); titolo Media 283 · Alta 244. GPU RTX 4060 Ti, 1280x720, tabella in `media/R020/fps_windows.csv`. Gli oggetti aggiunti sono poche decine di primitive per scena: nessun costo misurabile. Non misurato su PC scolastico ne' su WebGL.

### 5. Revisione dell'8 novembre (dopo la review del coordinatore)

**Bloccanti**
1. *Enigmi semplificati.* `MondoStato.AssegnaGettoni` e' la funzione unica: assegna l'id dell'enigma, i suoi `produce`, i `produce` di tutte le parti (chiave, cartoncino, chiavetta, scarpe) e gli oggetti nascosti; `Completa` la usa. Prima i gettoni delle azioni `prendi` arrivavano solo dalla vista 3D.
2. *Migrazione dei salvataggi vecchi.* `MondoStato.Migra` gira in `SaveSystem.Load` (idempotente): per ogni chiave di `enigmiRisolti` richiama la stessa logica.
3. *Prova.* `EnigmiSelfTest` ora fa anche la corsa completa hub, basso, ricordo in modalita' semplificata (pannelli 2D, con un errore e poi la risposta giusta) e il caricamento di un salvataggio "vecchio" (senza `oggettiEnigma`) a meta' ricordo, che deve poter finire. **157 PASS, 0 FAIL** nell'editor e nella build Windows di sviluppo.

**Decisioni dell'utente (testi nei JSON)**
4. *Scarpe del baule = scarpe del viaggio:* `scarpe_prendi`, spiegazione, appunto, descrizione di `scarpe_trovate` e consegna del baule riscritti (il primo paio di Amerigo, avuto col treno, conservato da Antonietta per tutta la vita). Rimasto da ricontrollare: la cassetta e il calendario parlano ancora di soldi messi da parte "per le scarpe" (un desiderio, non il paio del viaggio).
5. *Ritagli di `enigma_ordina`:* solo fatti fino all'estate 1946 (8 settembre 1943, 27-30 settembre 1943, 25 aprile 1945, 2-3 giugno 1946, 25 giugno 1946, 28 giugno 1946); date verificate e annotate in `Docs/VERIFICHE_STORICHE.md` (sezione 7). Ho scartato le amministrative del 10 marzo 1946 perche' le fonti si contraddicono. 1948 e 1989 stanno solo nelle schede civiche.
6. *Schede:* manifesti con "Centro-Nord"; ex-voto riscritta con la definizione confermata da fonti (Beweb CEI, Avvenire); "d'argento" e' una scelta del gioco e la scheda lo dice.

**Importanti**
7. *Testi fuori dal C#:* tutti nel blocco `testi` di `enigmi_mondo.json` (ripiego neutro nel codice), `aperturaMsg` e `bypassOggetto` in `luoghi.json`, `completoMsg` negli enigmi, `enigmiImp` in `ui_menu.json`; anche gli id fissi ("scarpe", scena del basso, "mondo/enigma_baule") sono ora dati (`finale`, `bypassOggetto`). Schema aggiornato in `CLAUDE.md`.
8. *Solo tastiera e gamepad:* Invio a enigma risolto = Avanti; `C` o Invio senza parte selezionata = Conferma nella deduzione; frecce/Tab/Invio scorrono le righe dei documenti (EventSystem); gamepad: B esce, stick destro ruota, A agisce, croce e spalle cambiano parte, Y conferma. Provati nel test solo per i documenti (gamepad non provato con un pad vero).
9. *Un solo annuncio* dei gettoni (appunto, oppure il primo oggetto prodotto), niente doppio telegramma.
10. *Preset Bassa e WebGL:* la vista ravvicinata spegne SMAA e le ombre morbide della sua luce. I materiali nascono da `Resources/Materiali/MP_Rif.mat` (URP Lit con `_EMISSION` attivo), cosi' la variante dello shader entra nella build. Non ho misurato l'evidenziazione in WebGL (nessuna build WebGL fatta).
11. `EnigmiSelfTest` resta sotto `UNITY_EDITOR || DEBUG` (`DEVELOPMENT_BUILD` fa comparire l'avviso UAC0009 in Unity 6.6 e la console deve restare pulita; `DEBUG` e' definito solo nelle build di sviluppo). `enigma_lucchetto` ha la `scheda`. Il log e' salvato come `.txt` (il `.log` e' in `.gitignore`).

**Grafica: modelli veri**
12. Modelli originali fatti in Blender (script `ArtSource/Models/Enigmi/genera_enigmi.py`, FBX in `Assets/Art/Models/Enigmi/`, prefab in `Resources/Modelli/Enigmi/` con il menu Treno > Enigmi > Crea prefab dei modelli): panno con pieghe (i sei panni del bucato), ex-voto a piede (sagoma con cinque dita, sbalzo) e a cuore, cornice con incavo, forma di legno del calzolaio, lucchetto, scarpa d'epoca (tomaia liscia con suola e tacco, usata per tutte le scarpe: passanti, forme, calzature da cercare). `ParteDef.modello` mette il modello al posto della primitiva; collider, misure, id e azioni delle parti sono invariati. Materiali PBR da codice (colori e texture procedurali 128 px nel materiale, nessuna texture importata), niente LOD (meno di 5000 triangoli per modello).
- *Fatto solo in parte:* baule, cassetta di latta, calendario, banco e assi restano pezzi di primitive (non ho fatto i modelli). L'ex-voto e' una sagoma sbalzata semplice, non un'argenteria rifinita. Nessun modello CC0 scaricato: niente in `CREDITS.md`.
- *Sfera grigia nel ricordo:* era il `PalloneStracci` di scena accanto al gradino; l'enigma dei passanti lo nasconde (`sostituisce`), e il gesso del gradino e' ora spento (colore grigio sporco). In `EscapeRoomController` ora si applica `sostituisce` anche agli enigmi del ricordo.
- *Baule nella foto di contesto:* il punto di vista e' scelto a mano dentro l'alcova (la foto "da vicino" del baule dal lato della tenda e' bloccata dalla tenda: non e' inclusa).
- *LFS:* +0,6 MB (otto FBX), quindi circa 822 MB su 1 GB. Il resto e' codice, JSON e JPG (7,5 MB, fuori da LFS).

**FPS dopo i modelli** (build Windows di sviluppo, RTX 4060 Ti, 1280x720, 15 s per misura, tra parentesi l'1% peggiore): hub Media 227 (72) · Alta 204 (69); basso Media 256 (65) · Alta 247 (64); ricordo Media 235 (63) · Alta 213 (71); titolo Media 222 (68) · Alta 188 (55). Tutto sopra 60 FPS anche all'1% peggiore, salvo il titolo a Alta (55, la scena dei menu, invariata). Tabella in media/R020/fps_windows.csv. Nessun regresso misurabile dei modelli veri (poche decine di mesh da qualche migliaio di triangoli). Non misurato su PC scolastico ne' su WebGL.

**Grafica: modelli veri, seconda passata (9 novembre)**

Tutti originali, fatti da codice e riproducibili: texture con `ArtSource/Models/Enigmi/texture_enigmi.py` (PIL/numpy), modelli con `genera_enigmi_v2.py` (Blender 5.2 headless), poi Treno > Enigmi > Crea font d'epoca e Crea prefab dei modelli. Materiali URP Lit veri in `Resources/Materiali/Enigmi` (albedo, normal, metallo+occlusione+levigatezza): `ME_*` restano, `MT_*` (panno, cuoio delle scarpe) si tingono col colore della parte del JSON. Nessun asset esterno nuovo, salvo due font OFL (vedi sotto); dove serviva ho riusato texture CC0 Poly Haven gia' nel progetto (assi, ferro, lino, iuta). Misure, triangoli e materiali di ogni modello in `ArtSource/Models/Enigmi/modelli_misure.json`.

| Oggetto | Cosa e' ora | Triangoli | Texture |
|---|---|---|---|
| Ex-voto (hub) | Teca votiva di noce: cassa, cornice modanata con filetto dorato, vetro, cornicione, frontone ad arco con croce, mensola di marmo con due lumini rossi; fondo di damasco rosso. Cinque ex-voto in lamina d'argento sbalzata (cuore fiammeggiante, piede, occhi, mano, gamba) con perline, cesello e annerimento nelle cavita' | teca 2802 (LOD1 1260); ex-voto 1300-2800 l'uno | atlante ex-voto 1K (sagoma ritagliata, normal, metallo/occlusione); damasco, marmo, noce, ottone 512 |
| Cassetta (basso) | Latta litografata "Partenope, biscotti all'uovo" (marca e disegno inventati: golfo col Vesuvio in un ovale, cornice d'oro sbalzata, greca), spigoli tondi, ammaccature, ruggine ai bordi, graffi fino alla latta; piastra d'ottone avvitata con la finestra delle rotelle; etichetta di carta con "per le scarpe" scritto a mano (testo dal JSON, font a mano) | corpo 1216, coperchio 888 | latta 1K + normal/MS 512 |
| Rotelle delle combinazioni | Ottone con patina e cifre in carattere da stampa d'epoca (cassetta, calendario, lucchetto) | (cilindri) | ottone 512 |
| Baule (basso) | Baule da viaggio cavo con vassoio e carta da fodera a fiorellini, doghe, listelli, cantonali e bordature di ferro con borchie d'ottone, cinghie di cuoio con fibbie, maniglie, cerniere; coperchio con bandella; serratura d'ottone con bocchetta. Dentro: coperta di lana piegata in tre (righe scure), tre assi con incisi a coltello scarpa, croce e treno, doppio fondo, scatolina avvolta nel lino e legata con lo spago | corpo 5104 (LOD1 2040), coperchio 1820 (LOD1 728), coperta 7000, scatolina 7280, assi 60 | assi incise 1K; coperta, fodera 512; assi/ferro/lino/iuta CC0 del progetto |
| Calendario (basso) | Foglio di carta ingiallita appeso col listello di ferro e lo spago a un chiodo, angoli arricciati, pieghe; in alto un'illustrazione originale del golfo col Vesuvio e un pino; "1946", mesi e conti con caratteri d'epoca; scarpette disegnate a matita come decal (non piu' scarpe 3D grigie); placca d'ottone avvitata | 1108 | calendario 512x1024 |
| Bucato (hub) | Panni con pieghe vere (simulazione di stoffa appesa a due mollette), tela tinta col colore della parte; toppe cucite a punto filza con orlo sfilacciato: quadri scozzesi irregolari e un po' storti, oppure tela grigia; mollette di legno con molla d'acciaio | panno 3400-3600, molletta 232 | panno, toppe 512 |
| Cornice con foto (basso) | Cornice di noce modanata con filetto dorato e piedino; fotografia seppia originale (madre e bambino in posa) con bordo bianco | 1416 | foto 384x512 |
| Banco del calzolaio (ricordo) | Gambe, fasce, ripiano con cassetta di chiodi, rotolo di spago e ritaglio di cuoio; piano consumato al centro; cassetto con cornice, pomello e bocchetta d'ottone; martello vero; pelle dal bordo irregolare | banco 1492 (LOD1 746) | assi CC0 del progetto |
| Forme (ricordo) | Forma da calzolaio a sezioni (tallone, collo del piede, punta rialzata) con piastrina di ferro, foro e taglio | 1764 | legno chiaro 512 |
| Lucchetto (ricordo) | Corpo d'ottone smussato con fascia, finestra delle rotelle, targhetta; arco d'acciaio | 952 | ottone, ferro |
| Scarpe (passanti, forme, baule) | La scarpa sinistra di R023 (tomaia con collo e lacci, suola e tacco) al posto della tomaia liscia; cuoio tinto per stile | 1792 | cuoio tintabile 512 |

Codice: `ParteDef` ha `msx,msy,msz` (misura vera del modello) e `mox,moy,moz` (suo centro) per le parti cliccabili, cosi' il collider resta quello di prima; `font` ("" stampa, "mano" scrittura a mano); una parte `vuoto` puo' avere il suo `modello`. Id, collider e azioni delle parti invariati; parti decorative assorbite dai modelli diventate `vuoto` (velluto, dorso del piede, toppe, bordo, fasce, interno, segni delle assi, pomello, figurine della foto). Il coperchio aperto del baule ruota ora sulla cerniera (rx 100).

- *Font:* Old Standard TT (stampa) e Cedarville Cursive (a mano), SIL Open Font License 1.1, con il testo della licenza accanto ai TTF e annotati in `CREDITS.md`. Atlanti TMP statici in `Resources/Fonts`. Non e' CC0/CC-BY: e' la licenza standard dei font liberi (permette uso, incorporazione e ridistribuzione con la licenza); se non va bene si tolgono i due SDF e torna il font di prima.
- *LFS aggiunto:* circa 7 MB (texture 3,2 MB, 33 FBX 3,2 MB, font 0,6 MB), quindi circa 829 MB su 1 GB.
- *Prova:* build Windows di sviluppo **157 PASS, 0 FAIL** (`media/R020/prova_enigmi_build_windows.txt`). Nell'editor la stessa prova da' 138 PASS e 19 FAIL: falliscono tutti e soli i clic del mouse virtuale (anche su enigmi che non ho toccato, come i manifesti), mentre la build con lo stesso codice passa tutto; l'editor pilotato da MCP non riceve il mouse virtuale nella Game view (ho provato a darle il fuoco, a mandare tutto l'input al gioco e a cambiare risoluzione). Va riprovata con l'editor in primo piano.
- *FPS* (build Windows di sviluppo, RTX 4060 Ti, 1280x720, 15 s per misura, tra parentesi l'1% peggiore): hub Media 731 (262) · Alta 647 (268); basso Media 1108 (284) · Alta 1040 (321); ricordo Media 847 (307) · Alta 723 (282); titolo Media 663 (265) · Alta 589 (252). Tabella `media/R020/fps_windows.csv` (righe del 9 novembre). Le cifre assolute sono molto piu' alte di quelle dell'8 novembre in tutte le scene, compreso il titolo che non e' cambiato: le condizioni della misura (finestra in secondo piano) non sono le stesse, quindi il confronto valido e' fra le scene e il titolo, dove non c'e' peggioramento.
- *Non all'altezza o non fatto:* la fotografia e' un'immagine dipinta da codice, leggibile ma non una vera foto d'epoca; la chiave dell'ex-voto e la chiavetta della cassetta restano di primitive (piccole, si vedono solo nella vista); mensola e schienale delle forme sono il piano del banco riscalato e un cubo; le monete sono cilindri. Gli screenshot automatici della vista ravvicinata escono schiariti e coperti dai telegrammi (e' il modo in cui li cattura la prova, uguale a prima).

### 4. Non fatto, scostamenti, dubbi

- **Passanti MakeHuman (D2): non fatto.** Servono sei figure intere, e il generatore MakeHuman va eseguito a mano dentro MakeHuman (Utilities > Scripting): da qui non si puo'. Al loro posto l'enigma usa il gradino con i mestieri scritti a gesso e, nella vista ravvicinata, le scarpe di sei passanti (stili: lucide, chiodate, zoccoli, scarponi, tacchi, suole nuove) con una descrizione per ciascuna. Quando ci saranno le figure basta metterle sui vuoti `PassantiRicordo`: l'enigma non cambia.
- Nessun modello 3D scaricato (tutto da primitive): niente nuove voci in `CREDITS.md`, **nessun file in LFS** (LFS resta a circa 821 MB).
- Le forme sono semplici (blocchi, capsule): leggibili ma non rifinite. Le posizioni degli oggetti del ricordo (passanti, forme, banco, ritagli, lucchetto) sono provate nella prova ma non rifinite nella scena: si ritoccano da `x,y,z,yaw` nel JSON, senza toccare codice.
- La battuta facoltativa `cs_basso_001` non e' stata aggiunta.
- Nessuna GIF: schermate in sequenza al posto delle GIF richieste da R009/R020/R010.
- Touch: la vista usa il puntatore unico di Input System (mouse e tocco), ma il tocco e' provato solo in teoria (nessuno schermo touch qui). Il pannello "ordina" e' provato con lo stesso metodo del resto, non con un vero trascinamento.
- Nessun bake delle luci rifatto (nessuna scena modificata a mano: gli oggetti nascono a runtime).
- **Dubbi storici / decisioni per l'utente.** (1) Il ritaglio "ordina" elenca eventi del 1947 e 1948 dentro un ricordo del 1946: l'avevo trovato nello schema approvato ("gli stessi 6 eventi"); li ho lasciati, ma potrebbero diventare ritagli "dall'archivio del presente". (2) La scheda dell'ex-voto e' generica e va verificata con una fonte (come dice lo schema). (3) La scheda del calendario usa solo fatti confermati in `VERIFICHE_STORICHE.md` (contrassegni, matita copiativa, 2-3 giugno, voto alle donne) e non dice che i simboli servissero a chi non sapeva leggere. (4) L'avviso del Comitato sulla bacheca e nei documenti e' un testo nostro, plausibile ma non una copia di un documento reale.

