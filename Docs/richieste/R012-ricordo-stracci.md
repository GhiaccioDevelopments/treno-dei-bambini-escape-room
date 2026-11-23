# R012 – Nuovo ricordo "Stracci": il vicolo degli stracci e la bottega di Capa 'e fierro

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 605f952. **Il codice attuale vince** |
| **Dipende da** | R006, R007, R009 (e R011 per i segreti). Prima delle stanze successive (R013–R016) |
| **Ok dell'utente** | **Sì (2026-11-06)**: "Fai tutte le altre stanze, con enigmi sempre più diversi e belli, deve spaccare il gioco" |

## 1. Cosa ha chiesto l'utente (parole sue)
> FAI TUTTE LE ALTRE STANZE, CON ENIGMI SEMPRE PIÙ DIVERSI E BELLI, DEVE SPACCARE IL GIOCO

Questa è la prima di cinque richieste-capitolo (R012 Stracci, R013 Volantino, R014 Cappotto, R015 Violino, R016 Finale),
nell'ordine della storia. **Regole comuni a tutte e cinque** (scritte qui e richiamate dalle altre):

## 2. Regole comuni ai capitoli R012–R016
- **`Docs/TRAMA.md` vincola tutto** (§7.2 per luogo, emozione, tema e idee di enigmi; §8 guida ai dialoghi; §9 punti
  incerti: niente fatti "da verificare" presentati come certi). Contraddizioni → segnalarle all'utente.
- Struttura di ogni capitolo, come "Le scarpe": oggetto trovato nel basso/hub → cutscene d'ingresso → stanza/flashback
  esplorabile → catena di enigmi → cutscene di chiusura → oggetto in inventario e luogo successivo "aperto".
- **Enigmi sempre più diversi e belli**: ogni capitolo introduce **almeno un meccanismo mai visto prima** nel gioco e
  non ripete la stessa combinazione di tipi. Difficoltà che sale piano piano. Sistema di R009 (oggetti del mondo, vista
  ravvicinata, catene `richiede/produce`, inventario attivo), indizi a 3 livelli, didattica non punitiva, fallback
  "Enigmi semplificati".
- Ogni stanza: grande e dettagliata (standard R006), grafica R007, segreti R011, nessuna compenetrazione (verifica R006).
- Contenuti nei JSON (`oggetti.json`, `luoghi.json`, `flashback_<id>.json`, `cutscene_<id>.json`, `audio.json`),
  battute con ID `cs_<oggetto>_NNN` per il doppiaggio, dialoghi originali, nessun testo nel codice.
- Personaggi nuovi: basi MakeHuman/MPFB in Blender come da CLAUDE.md, screenshot all'utente prima di considerarli finiti,
  sembianze originali (non degli attori del film).
- **Prima di sviluppare ogni capitolo**: presentare all'utente (pannello domande) lo schema della catena di enigmi e la
  bozza della mappa, e aspettare l'ok.
- Scene nuove generate da script (menu **Treno ▸ …**), aggiunte alle Build Settings, bake luci, console pulita, FPS misurati.

## 3. Questo capitolo: "Stracci"
- **Luoghi**: `vicolo_stracci` (vicolo dove Amerigo raccoglie stracci tra macerie e cortili) e `casa_capa_e_fierro`
  (bottega del robivecchi). Capa 'e fierro: brontolone ma non cattivo (TRAMA, Decisione 3).
- **Emozione / tema**: ingegno e imbarazzo; lavoro minorile ed economia di sopravvivenza.
- **Idee di enigmi** (da TRAMA §7.2, da rendere nuovi e "fisici"): raccolta degli stracci giusti nel vicolo (esplorazione a
  tempo libero, non a cronometro); **bilancia a piatti** della bottega con pesi e conti leggeri sul prezzo (meccanismo nuovo);
  ordinare le fasi del baratto; abbinare "mestiere di bambino del 1946 – diritto di oggi" (il collegamento agli articoli lo fa
  l'interfaccia didattica, non i personaggi); attrezzi del robivecchi nascosti.
- Oggetto ottenuto: **gli stracci** (o il soldo guadagnato) → apre il capitolo "Volantino".

## 4. Criteri di accettazione
- [ ] Schema approvato dall'utente prima dello sviluppo.
- [ ] Capitolo giocabile dall'inizio alla fine, salvataggio compreso, con almeno un meccanismo nuovo.
- [ ] Coerenza con TRAMA.md, testi originali, ID battute, console pulita, FPS per preset.
- [ ] Video/GIF in `Docs/richieste/media/R012/`.

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R012_schema.md`](schemi/R012_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 una sola scena: vicolo degli stracci e bottega di Capa 'e fierro.
  - D2 la bilancia è starata per caso (un chiodo sotto il piatto). Capa 'e fierro resta "in fondo buono".

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto

**Stato: codice completo, da provare in Unity (2026-11-12).** La connessione MCP di Unity e' caduta durante la prova finale (regola di progetto: fermarsi). Restano le voci elencate in "Da completare".

### Fatto
- Scena `40_FB_Stracci` generata da `SceneBuilderStracci.cs` (Treno > Scene > Costruisci ricordo Stracci, inclusa in "Costruisci tutte le scene"): 1350 m2 calpestabili, 15 zone senza caricamenti (largo d'ingresso, vico, scala, vico alto, largo e soglia della bottega, bottega interna con soffitto a travi e lanterne, vico dei panni, cortile del bucato, lotto di macerie, vico e cortile dei carrettieri, scala e terrazzino, passaggio alto = scorciatoia ad anello). Aggiunta alle Build Settings, al look (profilo ricordo), alle verifiche R017/R018/raggiungibilita', alla pianta (MappaBuilder) e al benchmark.
- Cinque enigmi (`flashback_stracci.json`, generato da `ArtSource/Stracci/genera_flashback_stracci.py`): S1 il giro degli stracci (raccolta di 10 pezzi nel vicolo + esame del tessuto), S2 le ceste (inventario attivo con elenco di oggetti), S3 la bilancia che sbaglia, S4 il conto con le monete, S5 il muro dei mestieri a tre colonne. Indizi a tre livelli, errori gentili, versione "Enigmi semplificati" (quiz), schede civiche.
- Tre meccanismi nuovi, in `EsaminaViewStracci.cs`: esame del tessuto (controluce, filo, spessore), bilancia a piatti con pesi e chiodo sotto il piatto, pagamento con monete sul banco; piu' l'abbinamento a tre colonne.
- Cutscene `cutscene_stracci` (19 battute `cs_stracci_001..019`, capitoli ingresso e chiusura), nuovi personaggi `capa_e_fierro` e `donna_vicolo` in `personaggi.json`, oggetto `stracci`, fagotto d'ingresso creato a runtime nel cortile del basso (dopo Le scarpe), `sede_partito` sbloccata dagli stracci, obiettivi e pianta in `mappe.json`, audio.
- Segreti R011 (da `genera_segreti.py`): enigma segreto del carretto (cassetto sotto il sedile aprendo girando la ruota) e 5 collezionabili (tappo a corona, bottone militare, ritaglio di giornale, carta da gioco, biglia).
- Prova automatica `StraccTest` (flag `-straccitest`): prima esecuzione 76 PASS / 6 FAIL (i 6 erano errori della prova e un bug di `Registra`, gia' corretti); la seconda esecuzione ha mostrato una regressione (tessuti), corretta nel codice, **non rieseguita** per la caduta di MCP.

### Decisioni prese da Claude
- Ingresso e chiusura nello stesso file `cutscene_stracci.json` con campo `capitolo` (non due file): cosi' resta un unico ID `cs_stracci_NNN` per il doppiaggio.
- Il fagotto compare a runtime da `oggetti.json` (campo `ingresso`) invece di ricostruire la scena del basso: nessun bake in piu', nessun rischio per le lightmap esistenti.
- `sede_partito` non ha ancora una scena: `luoghi.json` ha il nuovo campo `sbloccatoDa`; la porta, sbloccata, mostra un messaggio ("si apre piu' avanti").
- Prezzi (lana 40, cotone 20, lino 25 lire al chilo), pesi delle ceste (1300, 800, 600 g) e il totale (83 lire) sono INVENTATI [GIOCO] e dichiarati nella scheda. Il chiodo falsa la bilancia di 150 g.
- Scheda stracci → lana rigenerata/carta: resta generica (nessuna fonte certa su fabbriche e prezzi), dichiarato nel testo. Il limite di 16 anni per lavorare (legge 977/1967) e' citato nel "dietro le quinte" del segreto: da ricontrollare con una fonte.
- Mappatura mestiere → diritto del muro dei mestieri (lustrascarpe-art. 34, acquaiolo-ONU art. 32, venditore ambulante-art. 37, raccoglitore di stracci-art. 31) scelta come didattica e dichiarata "didattica" nella scheda.
- Nomi di vie e cortili della pianta (Vico degli Stracci, Vico dei Panni, ecc.) inventati e NON verificati su OpenStreetMap (non c'era connessione di verifica in questa sessione).
- Capa 'e fierro ha una battuta in cui ammette la vergogna per il chiodo, ma resta "in fondo buono" (D2).

### Da completare (richiede Unity/MCP)
- Rieseguire `-straccitest` (in particolare tessuti dopo la correzione di `Registra`) e `-enigmitest`/`-segretitest` per le regressioni.
- Compilare le modifiche a `EsaminaView.cs` e rigenerare le texture dei pezzi (Treno > Segreti > Genera texture dei pezzi) per `RitaglioStracci`.
- Personaggio 3D di Capa 'e fierro e suo ritratto per la cutscene (il punto `Npc_CapaEFierro` e' gia' nella scena); donne del vicolo e Tommasino come voci fuori campo/ritratto.
- Un solo bake delle luci per la scena, FPS per preset (Alta e Bassa), screenshot e GIF in `Docs/richieste/media/R012/`.
- Verifiche Treno > Verifica > Superfici e buchi / Camera / Raggiungibilita' sulla nuova scena, e ritocco estetico della bottega (mucchi di stracci e ferri sono ancora blocchi semplici).

### Sessione 2026-11-12 (PC scuola, SENZA Unity ne' MCP: codice scritto a mano, NON compilato, NON testato)
- Rilettura del codice R012 (EsaminaViewStracci, StraccTest, EsaminaView con la correzione di `Registra`, IngressoRicordo, SceneBuilderStracci, GameManager, CutscenePlayer, Door, NascostoPuzzle, DataClasses): tipi, firme e namespace controllati contro i file esistenti, nessun errore di compilazione trovato. La correzione dei tessuti in `Registra` (le prove non risolvono; risolve solo la conferma delle etichette, evento `deduzione`) e' coerente con `StraccTest` (S1).
- Corretti: (1) nei tessuti, girando un'etichetta compariva "Che mestiere fa?" (testo della deduzione delle scarpe): ora solo la descrizione del pezzo; (2) l'elenco delle categorie dei tessuti era intitolato "Mestieri": ora usa l'etichetta dello strumento dal JSON; (3) col gamepad Paga (monete) e Conferma (colonne) non erano raggiungibili: ora il tasto Nord li conferma.
- Bottega: mucchi di stracci e ferri vecchi non piu' solo blocchi: panni d'epoca di traverso, sacchi, ceste, ruote di carro, telaio di branda, bicicletta sul fianco, secchio e pentola (`DettagliMucchio`, `DettagliFerri`, seme casuale separato: il resto della scena non cambia). Senza collider (resta quello della base).
- Capa 'e fierro: `ArtSource/Characters/makehuman/genera_capa_e_fierro_makehuman.py` (gemello di quello di Amerigo: ~50 anni, robusto, testa grossa con capelli grigi corti, tuta da robivecchi scurita e rattoppata, scarpe consumate; personaggio originale); materiali in `prepara_materiali_amerigo.py`. In Unity: menu **Treno > Personaggi > Importa Capa 'e fierro MakeHuman** (prefab `Assets/Art/Characters/CapaEFierroMH/`) e figura sul punto `Npc_CapaEFierro` con collider; se il prefab non c'e' la scena mette un segnaposto di primitive e lo scrive nel log.
- Ritratto a cartone `Sprites/ritratti/capa_e_fierro.png`: menu **Treno > Personaggi > Genera ritratto Capa 'e fierro** (rigenera solo quello).

### Da fare a casa con Unity (in quest'ordine)
1. Aprire il progetto, attendere la compilazione: **console senza errori** (se ce ne sono, riguardano i file di questa sessione: EsaminaView.cs, SceneBuilderStracci.cs, PersonaggiMakeHumanBuilder.cs, ArtGenerator.cs).
2. MakeHuman: doppio clic su `ArtSource/Characters/makehuman/1_prepara_materiali.bat`, poi in MakeHuman Utilities > Scripting > `genera_capa_e_fierro_makehuman.py` > Execute. Controllare il log (avvisi su modificatori/asset mancanti) e gli screenshot in `Capa_e_fierro/screenshot/`; **mandarli all'utente** (regola: personaggi nuovi approvati con screenshot). Committare `.mhm`, `.fbx`, `textures/` come per Amerigo (LFS).
3. Unity: **Treno > Personaggi > Importa Capa 'e fierro MakeHuman**. Verificare nel log: avatar valido, nessun "mesh ... senza materiale" (se compare, correggere i nomi degli slot in `MhCapa`), offset dei piedi; misurare l'altezza del prefab (~1,70 m, altrimenti ritoccare `Scala`).
4. **Treno > Personaggi > Genera ritratto Capa 'e fierro**; guardare il ritratto in una battuta della cutscene.
5. **Treno > Segreti > Genera texture dei pezzi** (per `RitaglioStracci`).
6. **Treno > Scene > Costruisci ricordo Stracci**; guardare la bottega (panni sui mucchi: i modelli sono fatti per stare appesi, controllare che sdraiati di ~80 gradi stiano bene; Capa 'e fierro accanto al banco senza compenetrare il tavolo; collezionabile del mucchio di destra ancora visibile).
7. Verifiche sulla scena: **Treno > Verifica > Compenetrazioni**, **Superfici e buchi**, **Camera**, **Raggiungibilita'**.
8. Un solo **bake** delle luci di `40_FB_Stracci`.
9. Prove automatiche: `-straccitest` (atteso 0 FAIL, in particolare S1 tessuti), poi `-enigmitest` e `-segretitest` per le regressioni (EsaminaView e NascostoPuzzle sono condivisi).
10. FPS per preset Alta e Bassa nella scena (benchmark), screenshot e GIF in `Docs/richieste/media/R012/`.
11. Prova a mano col gamepad di monete (Paga) e muro dei mestieri (Conferma) con il tasto Nord.
12. Aggiornare questo Rapporto, spuntare i criteri e la riga nel README.

### Criteri di accettazione
- [x] Schema approvato dall'utente prima dello sviluppo.
- [ ] Capitolo giocabile dall'inizio alla fine, salvataggio compreso, con almeno un meccanismo nuovo (la prova automatica ha completato tutto il flusso nella prima esecuzione; da riconfermare dopo le correzioni).
- [ ] Coerenza con TRAMA.md, testi originali, ID battute, console pulita, FPS per preset.
- [ ] Video/GIF in `Docs/richieste/media/R012/`.
