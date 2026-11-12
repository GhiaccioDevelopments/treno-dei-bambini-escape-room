# R013 – Nuovo ricordo "Il volantino del treno": la sede del partito e le voci del vicolo

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 605f952. **Il codice attuale vince** |
| **Dipende da** | R012 (regole comuni ai capitoli nella sezione 2 di `R012-ricordo-stracci.md`: valgono anche qui) |
| **Ok dell'utente** | **Sì (2026-11-06)** |

## 1. Cosa ha chiesto l'utente
> FAI TUTTE LE ALTRE STANZE, CON ENIGMI SEMPRE PIÙ DIVERSI E BELLI, DEVE SPACCARE IL GIOCO

## 2. Il capitolo
- **Luogo**: `sede_partito` (sede UDI/PCI: tavoli, ciclostile, manifesti d'epoca originali, registri delle iscrizioni dei
  bambini) e il vicolo dove circolano le voci.
- **Personaggi**: Maddalena (personaggio di finzione, solo "Maddalena", TRAMA §4), le madri, la Pachiochia che sparge
  paura e dicerie (figura della disinformazione, non una cattiva).
- **Emozione / tema**: paura e speranza; dicerie e fonti, UDI, voto alle donne del 1946.
- **Idee di enigmi** (da TRAMA §7.2, da rendere nuovi): **indagine sulle dicerie** – raccogliere le voci nel vicolo e
  confrontarle con documenti e testimoni per dire vero/falso con la fonte (meccanismo nuovo: bacheca di indizi collegati,
  alla Obra Dinn); **ciclostile** da far funzionare per stampare il volantino (meccanismo fisico); ordinare la cronologia
  dic 1945 – giu 1946 (date in `TRAMA.md` §3, solo quelle verificate); abbinare chi faceva cosa; lucchetto con la data del referendum.
- Attenzione agli anacronismi (TRAMA §3 e §6): la Costituzione è del 1948.
- Oggetto ottenuto: **il volantino del treno** → apre il capitolo "Cappotto".

## 3. Criteri di accettazione
Come R012 (schema approvato prima, capitolo completo, meccanismo nuovo, coerenza TRAMA, media in `Docs/richieste/media/R013/`).

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R013_schema.md`](schemi/R013_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 il vicolo delle voci è una variante sera/pioggia del set del ricordo "Le scarpe".
  - D2 le frasi del volantino si giudicano per chiarezza e verificabilità, mai per la parte politica.

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto

**Stato: codice completo, da provare in Unity (2026-11-12).** Lavoro fatto al PC della scuola **senza Unity né MCP**: codice e dati scritti a mano,
controllati leggendo i file esistenti (tipi, firme, namespace), **mai compilati né provati**. La scena `50_FB_Volantino` non esiste ancora: la costruisce il menu a casa.
Base: `richiesta/R013-ricordo-volantino` con dentro `richiesta/R012-ricordo-stracci` (merge senza conflitti; il codice di R012 vince).

### Fatto
- **Scena** `50_FB_Volantino` generata da `Assets/_Game/Editor/SceneBuilderVolantino.cs` (menu **Treno > Scene > Costruisci ricordo Volantino (R013)**, inclusa in
  "Costruisci tutte le scene", che ora costruisce 7 scene e le mette nelle Build Settings):
  - **vicolo delle voci** = lo stesso set del ricordo "Le scarpe" (stesse zone di `DefinisciZoneRicordo`, D1) di sera e sotto la pioggia: botteghe chiuse con la sola
    lampada da muro, lampioni, lavatoio, pozzo, fontanella, edicole, panni dimenticati, manifesto dei treni all'ingresso e manifesto del voto strappato sul largo in alto;
  - **sede del Comitato** in fondo al largo (soglia z 37-40 e sede z 40-60, quota 0,9): due stanze divise da un muro con porta (sala del Comitato e stanza dei registri),
    soffitto a travi, 9 lampade a petrolio, panca d'attesa, sedie spaiate, tavolino, scaffale dei registri, armadio delle visite, valigie, manifesti nostri
    (`testi_ambiente.json`: `ComitatoTreni`, `ComitatoVisite`, `ComitatoVoto`), Maddalena come **segnaposto di primitive** (`Npc_Maddalena`), uscita sul retro.
  - Agganci: look (profilo ricordo, caso "sera di pioggia" in `LookBuilder`), pioggia (`MeteoBuilder.AddMeteo` + `luoghi.json` meteo `pioggia`), decal, pellicola dei
    ricordi per tutte le scene `_FB_` (`GraphicsPresets`), verifiche Superfici/Camera/Compenetrazioni/Raggiungibilità, pianta (`MappaBuilder`), benchmark, `SegretiTools`.
- **Cinque enigmi** in `flashback_volantino.json` (generato da `ArtSource/Volantino/genera_flashback_volantino.py`), catena `richiede/produce`, indizi a 3 livelli,
  errori gentili, domande per "Enigmi semplificati", schede civiche:
  - **V1 Le voci del vicolo** – *ascolto nello spazio (nuovo)*: quattro voci sotto due balconi, una finestra e al lavatoio (`modello: "voce"` in `NascostoPuzzle`,
    componente `VoceVicolo`): finestra accesa che tremola, mormorio da lontano (evento audio `voce_vicolo`), la voce si sente **da sola avvicinandosi** (o con E) e il
    sottotitolo compare sempre; poi nel *quaderno delle voci* (meccanismo documenti) si indica per ogni domanda la voce senza fonte («lo sanno tutti», parente di un
    parente, un sogno, una fonte che cambia).
  - **V2 La bacheca delle prove** – *deduzione a gruppi con lo spago (nuovo)*: 4 voci e 5 fogli (uno non c'entra); toccando voce e foglio si tende uno spago
    (`LineRenderer`); alla Obra Dinn **due collegamenti giusti diventano rossi e restano fissati** senza dire quali sono sbagliati; poi il cartellino vero/falso di ogni voce.
  - **V3 Il ciclostile** – *meccanismo fisico a fasi (nuovo)*: tre frasi su sei per la matrice (si giudicano solo chiarezza e verificabilità, D2; ogni frase scartata ha
    la sua spiegazione), matrice sul tamburo, inchiostro, manovella con ritmo regolare (sotto 0,6 s fra due giri il foglio sbava e si ricomincia), cinque fogli.
  - **V4 Chi fa che cosa** – abbina con oggetti (inventario attivo): cinque cose (fascia della Croce Rossa, stetoscopio, registro, lettera dal Nord, berretto da
    ferroviere) a cinque persone di finzione; è il retro del volantino.
  - **V5 Il timbro della data** – *timbro datario a nastri (nuovo)*: giorno, mese e anno girevoli; si timbrano in ordine quattro date lette sui fogli della scrivania,
    l'ultima il 2 giugno 1946; data fuori ordine, già timbrata o inesistente = errore gentile diverso.
- Codice dei meccanismi: `Assets/_Game/Scripts/Puzzles/Mondo/EsaminaViewVolantino.cs` (bacheca, ciclostile, datario; agganci in `EsaminaView.cs` e in
  `EsaminaViewStracci.ConfermaStracci`: tasto C / gamepad Nord = "Scrivi la matrice" e "Timbra"), `VoceVicolo` in `NascostoPuzzle.cs`, campi nuovi in `DataClasses.cs`
  (`collegamenti`, `frasi`, `giri`, `ritmo`, `date`, `mesi`, `anni`; `NascostoDef.prompt` e `fx, fy, fz`).
- **Cutscene** `cutscene_volantino.json`: 22 battute `cs_volantino_001..022` (ingresso 001-009, chiusura 010-022), dialoghi originali: Amerigo adulto con il foglio;
  Antonietta e Amerigo nel vicolo, lui che "sa leggere tutto"; Maddalena consegna il volantino, la Pachiochia ripete la diceria, una madre risponde con quello che ha visto,
  Antonietta firma con una croce. Personaggi nuovi in `personaggi.json`: `pachiochia`, `madre_vicolo` (senza ritratto, come `donna_vicolo`).
- **Oggetto** `volantino` in `oggetti.json`: foglio ingiallito piegato in quattro **sulla credenza del basso** (creato a runtime da `IngressoRicordo`, solo dopo
  `flashback_stracci`), cutscene d'ingresso e di chiusura, icona provvisoria `scarpe` (come gli stracci).
- **Luoghi**: `ricordo_volantino` (scena 50, meteo pioggia); `sede_partito` ora rimanda alla credenza; **`stazione` sbloccata dal volantino** (`sbloccatoDa`,
  `sbloccatoMsg`, come `sede_partito` con gli stracci: la porta dice che si aprirà più avanti finché R014 non ha la sua scena).
- **Mappa e obiettivi** in `mappe.json`: pianta della scena 50 (strade del ricordo "Le scarpe" + soglia, sala e stanza dei registri), cinque obiettivi `ob_volantino_*`
  con il punto della credenza e del basso.
- **Audio**: scena 50 (tema del ricordo, pioggia), evento `voce_vicolo`.
- **Segreti R011** (`ArtSource/Volantino/segreti_volantino_blocco.py`, inserito da `aggiungi_segreti_volantino.py` in `genera_segreti.py`, poi rilanciato):
  enigma segreto **il registro delle impronte** (contare le 11 impronte d'inchiostro, due rotelle, il cassetto si apre: spilla dell'UDI) e 5 collezionabili
  (matrice rovinata, timbro vecchio, manifesto delle elezioni, cartolina dal Nord, matita copiativa); texture di carta `ManifestoElezioni` e `CartolinaNord`.
  Punti in `Docs/richieste/media/R013/punti_segreti_volantino.md` (scritto a mano con gli stessi nomi e coordinate del builder, che lo riscrive).
- **Prova automatica** `VolantinoTest` (flag `-volantinotest [-volantinotestout cartella]`, menu **Treno > Scene > Prova automatica ricordo Volantino (Play)**):
  credenza prima/dopo Stracci, cutscene d'ingresso, scena e 5 enigmi, voci (anche quella sentita avvicinandosi), bacheca (un giusto = niente, due = fissati, scambi,
  verdetti), salvataggio e ricarica a metà, ciclostile (frasi, fasi fuori ordine, manovella troppo veloce), ruoli, datario (fuori ordine, inesistente, già timbrata),
  uscita (oggetto, cutscene di chiusura, stazione sbloccata), versione semplificata, mappa, obiettivi, segreti.

### Scostamenti dalla specifica
- **Build Settings**: R012 ha la riga della scena in `ProjectSettings/EditorBuildSettings.asset` con il GUID assegnato da Unity; la scena 50 non esiste ancora, quindi
  **l'asset non è stato toccato**: la aggiunge il menu (Costruisci ricordo Volantino o Costruisci tutte le scene).
- Un'unica cutscene con `capitolo` ingresso/chiusura (come Stracci), non due file.
- Personaggi: Maddalena è un segnaposto di primitive in scena (ha già il ritratto 2D per le cutscene); la Pachiochia e la madre parlano solo in cutscene, senza ritratto
  né modello. La volontaria e la seconda madre dello schema non ci sono come figure: compaiono solo come persone dei cartoncini (V4).
- Ciclostile e datario sono pezzi di primitive nella vista ravvicinata, non modelli Blender (nessun CC0 cercato: a scuola niente Blender né verifica delle licenze).
- V1 ha come seconda parte il meccanismo "documenti" (già esistente, R020): il nuovo è l'ascolto nello spazio.
- V4: le cinque cose arrivano in mano con il pacco dei volantini (le produce V3) invece di raccoglierle dal tavolo: con l'azione "prendi" il messaggio di successo
  comparirebbe anche toccando la persona prima di avere la cosa giusta (comportamento attuale di `EsaminaView`).

### Fatti storici: versione prudente scelta
- Date del datario solo **confermate** in `Docs/VERIFICHE_STORICHE.md`: 16-12-1945 primo treno da Milano a Reggio Emilia; 29-12-1945 apertura del V Congresso del PCI
  (lo schema diceva "gen 1946": il congresso va dal 29 dicembre 1945 al 6 gennaio 1946, corretto); 19-01-1946 treni da Roma per l'Emilia; 2-6-1946 voto.
- **Non usato** il 10 marzo 1946 come "primo voto delle donne" (incerto nelle verifiche): nei testi solo "nel 1946 si votò più volte; il 2 giugno votarono per la prima
  volta in tutta Italia".
- Teresa Noce citata solo nella scheda di V4, **senza date di nascita e morte** (non verificate qui): solo il ruolo confermato (propose l'accoglienza nelle campagne).
- Amendola non citato. Napoli e la monarchia: "quasi l'80%" (senza decimali). Croce Rossa e vestiti: "in gran parte" (una sola fonte).
- Documenti della bacheca (registro, cartolina, avviso, lettera), persone del tavolo, impronte e spilla: **inventati per il gioco**, dichiarati nelle schede.

### Decisioni prese da Claude
- Scena unica (vicolo + sede) come Stracci; la sede sta oltre il largo in alto del set "Le scarpe", dove nel ricordo delle scarpe c'era il muro dell'uscita.
- Il volantino d'ingresso sta sulla credenza del basso (x -3,98; y 0,965; z 1,98), davanti alla sveglia: **da controllare a occhio** in Unity.
- Tutte e quattro le voci sono false; il quinto foglio (elenco del corredo) non risponde a nessuna voce, così la deduzione non è banale.
- Ritmo del ciclostile 0,6 s, cinque fogli; codice del segreto: 11 impronte.
- Nomi di finzione: signora Nunzia, dottor Russo, famiglia Malagoli (Modena), Gennaro; Maddalena solo "Maddalena".
- La frase "anche le madri hanno votato" resta [GIOCO] (il volantino è d'autunno ma ricorda il 2 giugno).
- Pellicola dei ricordi accesa per ogni scena con `_FB_` nel nome (prima l'elenco era 30_/40_): vale anche per i capitoli futuri.

### Da fare a casa con Unity (in quest'ordine)
1. Aprire il progetto e attendere la compilazione: **console senza errori** (file nuovi o toccati: `EsaminaViewVolantino.cs`, `EsaminaView.cs`, `EsaminaViewStracci.cs`,
   `NascostoPuzzle.cs`, `DataClasses.cs`, `VolantinoTest.cs`, `SceneBuilderVolantino.cs`, `SceneBuilder.cs`, `LookBuilder.cs`, `MeteoBuilder.cs`, le quattro verifiche,
   `MappaBuilder.cs`, `SegretiTools.cs`, `BenchmarkRunner.cs`, `GraphicsPresets.cs`). I `.meta` dei file nuovi hanno GUID scritti a mano: se Unity li rifiuta, rigenerarli.
2. Prima chiudere i punti di R012 rimasti aperti (stesso ordine del suo Rapporto), perché R013 ci si appoggia.
3. **Treno > Segreti > Genera texture dei pezzi** (per `manifestoelezioni` e `cartolinanord`).
4. **Treno > Scene > Costruisci ricordo Volantino (R013)**: controllare nel log che non manchino modelli o materiali; guardare la sede (bacheca, ciclostile, tavolo,
   scrivania e leggio non devono compenetrare mobili o muri; manifesti appoggiati ai muri; porta della sede sul largo e porta interna percorribili) e il vicolo di sera.
5. Basso: controllare che il volantino sulla credenza si veda e non compenetri sveglia e lampada (eventualmente ritoccare `ingresso` in `aggiorna_dati_volantino.py`).
6. **Treno > Mappa > Genera piante** (scrive anche i limiti `ax0..az1` della scena 50 in `mappe.json`).
7. **Treno > Segreti > Verifica punti** e, se il builder ha spostato qualcosa, `python ArtSource/Segreti/genera_segreti.py`.
8. Verifiche sulla scena: **Compenetrazioni**, **Superfici e buchi**, **Camera**, **Raggiungibilità** (in particolare le quattro voci, il leggio e lo scaffale alto).
9. Un solo **bake** delle luci di `50_FB_Volantino`.
10. Prove automatiche: `-volantinotest` (atteso 0 FAIL), poi `-straccitest`, `-enigmitest` e `-segretitest` per le regressioni (EsaminaView, NascostoPuzzle e GraphicsPresets
    sono condivisi); poi una prova a mano di bacheca (spaghi, inquadratura), ciclostile (pannello delle frasi) e datario (leggibilità dei nastri), anche col gamepad.
11. FPS per preset Alta e Bassa (benchmark), screenshot e GIF in `Docs/richieste/media/R013/`.
12. Personaggi 3D (Maddalena, la Pachiochia, le madri) con MakeHuman come per Capa 'e fierro, screenshot all'utente; ritratti delle cutscene per `pachiochia` e `madre_vicolo`.
13. Aggiornare questo Rapporto, spuntare i criteri e la riga nel README.

### Criteri di accettazione
- [x] Schema approvato prima dello sviluppo (2026-11-07).
- [ ] Capitolo completo e giocabile dall'inizio alla fine, salvataggio compreso, con meccanismi nuovi (scritto e coperto dalla prova automatica, **non ancora eseguito**).
- [ ] Coerenza con TRAMA.md, testi originali, ID battute, console pulita, FPS per preset (testi e ID fatti; console e FPS da verificare in Unity).
- [ ] Media in `Docs/richieste/media/R013/` (per ora solo il rapporto dei punti segreti).
