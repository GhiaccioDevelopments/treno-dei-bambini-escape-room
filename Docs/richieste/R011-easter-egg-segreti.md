# R011 – Easter egg ed enigmi nascosti in ogni stanza

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 605f952. **Il codice attuale vince** |
| **Dipende da** | R006 (mappe grandi), R009 (sistema "Esamina" e inventario attivo) |
| **Priorità** | Media. Da ripetere in ogni nuova stanza di R012–R016 |

## 1. Cosa ha chiesto l'utente (parole sue)
> Easter egg e enigmi nascosti in ogni stanza per esplorare di più, stanze e mappa molto più grandi.

Obiettivo: premiare chi esplora. Ogni stanza ha segreti **facoltativi** (non bloccano la storia) che si trovano
guardando bene. Le stanze e la mappa più grandi sono già chieste da R006: qui si chiede di **riempire** quello spazio.

## 2. Specifica
- **In ogni scena giocabile** (hub, basso, ogni flashback, e ogni stanza nuova di R012–R016):
  - almeno **1 enigma segreto** facoltativo (es. mattonella che suona vuota, cassetto con doppio fondo, sequenza di
    campanelli dei balconi, disegno sul muro che indica dove scavare), costruito con il sistema di R009;
  - **3–5 segreti piccoli** da trovare: oggetti d'epoca da collezione ("Ricordi di Napoli": figurine, cartoline,
    monete del 1946, giocattoli poveri, ritagli di giornale con notizie vere dell'epoca e fonte in `TRAMA.md`).
- **Collezione nel quaderno**: pagina "Segreti" con caselle vuote per stanza (es. "Basso: 3/5"), ogni pezzo con una breve
  scheda storica o civica originale. Contatore discreto nel menu pausa, mai nell'HUD.
- **Ricompense** solo narrative o estetiche: una cutscene breve in più, un ricordo extra di Amerigo, una pagina di
  "dietro le quinte" sui veri treni della felicità, eventuale finale con un dettaglio in più se si trova tutto.
- **Easter egg** discreti e coerenti col tono serio: rimandi interni al gioco (violino, mela annurca, scarpe), niente
  riferimenti a marchi, meme o altri giochi, niente rottura del tono drammatico.
- Indizi ambientali per trovarli (una luce, un suono, una scritta), nessuna freccia. Il pulsante Indizio **non** svela i
  segreti finché la stanza principale non è completata; dopo, può dare un aiuto leggero ("In questa stanza c'è ancora qualcosa…").
- Dati nei JSON: nuovo `segreti.json` `{ segreti: [ {id, scena, tipo: collezionabile|enigma, nome, scheda, x, y, z, ricompensa} ] }`;
  progressi nel salvataggio (`treno_save`).

## 3. Vincoli
Didattica non punitiva, coerenza d'epoca, testi originali, licenze degli asset come da CLAUDE.md, nessun nuovo oggetto
principale senza ok (i collezionabili non sono "oggetti" dell'escape room).

## 4. Criteri di accettazione
- [x] Ogni scena esistente ha ≥ 1 enigma segreto e 3–5 collezionabili, tutti raggiungibili e facoltativi (hub 1+5, basso 1+4, ricordo 1+5; verificato dalla prova automatica).
- [x] Pagina "Segreti" nel quaderno con progressi salvati.
- [x] Nessun segreto blocca la storia; console pulita; screenshot in `Docs/richieste/media/R011/`.

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R011_schema.md`](schemi/R011_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 premio per chi trova tutti i segreti: una scena in più nel finale (Amerigo consegna a Carmine la "scatola dei ragazzi del vicolo").
  - D2 ritagli di giornale: testi nostri su fatti verificati, nessuna riproduzione d'archivio.
  - Via Medina (11/6/1946) esclusa dallo schema. Il ritaglio "partono i bambini" va al punto 12, perché il punto 17 è di R020-H3.

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto

**Stato:** completata sul branch `richiesta/R011-easter-egg-segreti` (base `feature/upgrade-realistico` @ 94a9db7). Nessun merge, nessuna build WebGL. Prova automatica dei segreti **326 PASS, 0 FAIL** (build Windows di sviluppo, `-segretitest`; stesso esito in Play nell'editor). Prova esistente `-enigmitest` ripetuta nella stessa build: **241 PASS, 0 FAIL** (nessuna regressione degli enigmi principali).

### Cosa e' stato fatto
**Sistema (pronto per R012-R016).** Tutto e' dati: `Assets/_Game/Resources/Data/segreti.json`, generato da `ArtSource/Segreti/genera_segreti.py`, che aggancia ogni segreto per **nome del punto** nei rapporti R006 (`punti_segreti_*.md`) e ne ricava x,y,z. Se le scene vengono rigenerate basta rilanciarlo; **Treno > Segreti > Verifica punti** controlla le coordinate (30 controllate, 0 problemi). Per una nuova stanza: un blocco `stanze`, `segreti`, `pezzi`, `enigmi` nello script e il rapporto dei punti in `MD`: nessun codice da toccare. Codice in `Scripts/Segreti/` (`SegretiStato`, `SegretiSpawner`, `SegretoPezzo`, `CampanaGesso`, `UovoSegreto`, `SegretiSelfTest`), dati in `Scripts/Data/SegretiDati.cs`, menu in `Editor/SegretiTools.cs`.
Gli enigmi segreti usano il sistema di R009/R020 (`EnigmaMondo`, `EsaminaView`, `MondoFab`, parti da primitive); i collezionabili sono pezzi piccoli di primitive con i materiali di `MondoFab` (carta, ottone, vetro, stoffa...): nessun modello nuovo, nessun file nuovo in LFS, Blender non usato. Peso: hub 42 renderer / circa 3.200 triangoli, basso 52 / 4.700, ricordo 60 / 2.800, **nessuna luce dinamica**.
**Salvataggio:** `SaveData.segretiTrovati` e `segretiFlag` (retrocompatibili: un salvataggio vecchio senza i campi si legge e parte da zero, provato). **Quaderno:** la sezione "Segreti" di R023 (aggancio `QuadernoSegreti.Provider`) mostra una voce per stanza ("Il basso di Antonietta 5/5") con una casella per segreto (`[  ]` ancora da trovare, `[x]` nome e scheda, piu' "Dietro le quinte" per gli enigmi). **Pausa:** riga "Segreti trovati: n/17", **mai nell'HUD**. **Indizio:** tace sui segreti finche' la stanza principale non e' completata; dopo, se ne resta uno, mostra solo "In questa stanza c'è ancora qualcosa…" (ganciato in `Obiettivi.RivelaIndizio`, che tutti i pulsanti Indizio gia' chiamano). **Mappa:** la minimappa e la mappa grande non nominano e non mostrano nessun segreto (provato su `mappe.json`; il codice della mappa non legge gli Interactable).

**Hub (10_Hub_Vicolo)**: enigma **"Le tre edicole"**: tre biglietti con un verso della nostra filastrocca ("Prima le scarpe... poi la mela... e alla fine il treno") sui ripiani delle edicole piccole T3 e T5 e della grande, ognuno con il suo segno; la portella del sottoscala del cortile C3 si apre premendo i tre tasselli (scarpa, mela, treno) nell'ordine della filastrocca; dentro, la scatola dei ragazzi del vicolo con il treno disegnato col carbone. Collezionabili (5): cartolina del golfo (valigie), settebello sotto il tavolo di C3, trottola al pozzo C1, banconota AM-lire al grande arco, ritaglio "partono i bambini" ai margini delle macerie. Easter egg: violino lontano al gradino della chiesetta (solo dopo il finale: aggancio).
**Basso (20_Int_BassoAntonietta)**: enigma **"Il ripostiglio murato"**: graffi a semicerchio sul cotto davanti alla credenza; sulla parete, fra il calendario e l'angolo, una mensola con tre piatti: tolti i piatti compare il fondo finto con una leva; la credenza scivola via lungo i graffi e scopre una nicchia murata con il materassino dove dormiva Amerigo d'estate [GIOCO]. Collezionabili (4): santino dietro la tenda, ditale sotto la mattonella del tavolo (la mattonella si solleva), bottone della salopette sotto le pentole, biglia dietro il lavatoio.
**Ricordo (30_FB_Scarpe)**: enigma **"La campana di gesso"**: il graffito "C'ero anch'io" sul muro del cortile porta inciso 4 2 7 1; la campana disegnata col gesso sullo slargo d'ingresso si salta in quell'ordine (fermandosi un istante su ogni casella: prima del graffito e' solo un gioco, errore = si ricomincia con un messaggio gentile); la pietra mobile della fontanella scivola e compare il soldo promesso da Tommasino [GIOCO]. Collezionabili (5): moneta di rame sotto la soglia del barbiere, pallone di stracci, lista della spesa con le scarpe, uovo e biglietto sotto la paglia, bottone d'oro sul manichino del sarto. Easter egg: scarpine in piu' sul filo dopo il capitolo Cappotto (aggancio).
**Ricompense (solo narrative):** pagine "Dietro le quinte" per i tre enigmi (treni della felicita', i bambini di Napoli, chi accoglieva), tutte su fatti di TRAMA §3/§6 verificati; una scheda storica o civica originale per ogni collezionabile.

### Decisioni prese da Claude
1. **Il ripostiglio murato non e' una stanza vera.** Il "blocco pieno" del basso e' massiccio e le pareti sono mesh con collider: aprire un varco avrebbe voluto dire rigenerare la scena e rifare il bake (peso su LFS). Il ripostiglio e' quindi una **nicchia murata dietro la credenza** (parete ovest), con la credenza che scorre; la sua leva sta nella mensola dei piatti. Lo schema parlava di intercapedine "al punto 3": se l'utente la vuole vera e visitabile serve rigenerare il basso con un'apertura (richiesta a parte).
2. **I piatti stanno sulla parete accanto al calendario**, non sopra la credenza: sopra la credenza ci sono la finestra e la foto (R020). Il segreto resta "dietro i piatti".
3. **Monete.** Non ho potuto verificare ne' un taglio da dieci lire del 1946 ne' la data di fine circolazione delle AM-lire: nello schema c'era "da verificare". Ho scelto oggetti sicuri: una **moneta di rame di prima della guerra, consumata, senza taglio ne' data** (soglia del barbiere) e una **banconota AM-lire** descritta solo con cio' che e' certo (stampate dagli Alleati dal 1943, circolavano nel Sud insieme alle lire; niente date di ritiro, niente cifre).
4. **Ritaglio di giornale:** testo nostro "alla maniera" del 1946, nessuna testata, solo fatti di TRAMA §3/§6 verificati (primo convoglio 16 dicembre 1945, circa 1.800 bambini, Milano-Reggio Emilia; circa 70.000 in tutto; Comitato per la salvezza dei bambini di Napoli). Via Medina esclusa come da schema. Nel "dietro le quinte" del basso le cifre di Napoli sono un intervallo con fonti discordi (10.000-12.000); niente sul sovraffollamento dei bassi (in TRAMA e' ancora "da verificare").
5. **Inventati e dichiarati** nel testo: il materassino d'estate e il soldo di Tommasino portano "[GIOCO]"/"(un'invenzione del gioco)"; il biglietto "Per chi ha fame e non lo dice" e i versi della filastrocca sono nostri.
6. **Punti spostati rispetto ai rapporti R006 (con la ragione):** ritaglio a (23,5; -18,4) perche' dietro le macerie (25; -22,2) non c'e' area percorribile entro 4 m; biglietti sui ripiani reali delle edicole (a pochi decimetri dal punto: le quote dei ripiani le ho lette dalla scena); pallone di stracci spostato di 1,5 m per non cadere dentro la campana; bottone sotto le pentole appese, a terra (le pentole sono sagome a muro, dentro non c'e' posto); biglia all'estremita' del lavatoio (e' addossato al muro). Non ho usato punti di R020.
7. **Mobili "static":** la credenza e' static (batching): per farla scorrere se ne usa una **copia identica non batchata** e si spegne l'originale (`scorriOggetto`). La luce cotta resta quella originale.
8. **Indizi ambientali:** luce (le candele delle edicole e la lampada sulla credenza, gia' in scena), scritta (graffito, tasselli dipinti, numeri col gesso), segno sul pavimento (graffi); suono solo per il violino. Il prompt di interazione e il rombo dorato compaiono come per ogni oggetto, solo a meno di 1,6 m. Nessuna freccia, nessun alone, niente sulla mappa.
9. **Campana:** conta la sosta di 0,35 s su una casella (camminando non si fa per caso); prima di leggere il graffito non reagisce e non da' errori.
10. **Premio D1 (scena nel finale):** R016 non esiste ancora. Salvo il flag `segreti_tutti` (vero quando si trovano tutti i 17 segreti) e lo documento: **R016 deve leggere `SegretiStato.Flag("segreti_tutti")`** per la scena in cui Amerigo consegna a Carmine la "scatola dei ragazzi del vicolo", e chiamare `SegretiStato.ImpostaFlag("finale_visto")` a fine gioco (violino lontano al gradino della chiesetta). Aggancio Cappotto: quando `flashbackCompletati` contiene `flashback_cappotto` compaiono le scarpine in piu' sul filo (id da confermare quando R014 definira' il capitolo).
11. Il quaderno usa `[x]` e `[  ]` come caselle (il font non ha le caselle unicode).

### Verifiche (`-segretitest`, build Windows di sviluppo, 326 controlli)
Per ogni pezzo: renderer attivi con materiale; appoggiato (ne' sotto il suolo ne' sospeso); fuori da muri e pavimenti; raggiungibile a piedi (riempimento a griglia di 25 cm dal punto di partenza con la capsula del giocatore: adulto nell'hub e nel basso, bambino nel ricordo); il `PlayerInteractor` lo propone quando ci si avvicina; si vede da qualche punto raggiungibile (linea libera, almeno 25 px a 1280x720, FOV 50); si raccoglie e il salvataggio lo ricorda (anche dopo Continua: non riappare). Enigmi con il mouse virtuale (il raggio colpisce la parte giusta; ordine sbagliato = messaggio gentile e si ricomincia). Campana con il giocatore teletrasportato sulle caselle. Pagina Segreti con i conteggi giusti (6/6, 5/5, 6/6), contatore della pausa, Indizio muto/lieve/muto, easter egg (violino che suona in 3D, scarpine visibili dalla strada), salvataggio vecchio. **Nessun requisito di porte o enigmi principali dipende da un segreto**, e a partita nuova si completano tutti gli enigmi principali e il capitolo senza toccarne nessuno. Il controllo "dentro i muri" e la linea di vista contano solo muri, edifici e pavimenti (i collider a scatola dei mobili sono grossolani); non ho riusato `ControllaRaccoglibili` di R024 perche' lavora su `HiddenItem`: ne ho riscritto i controlli per i pezzi dei segreti. Nell'editor i clic del mouse virtuale non sempre arrivano (stesso limite della prova di R020): per questo la prova ufficiale e' quella in build.
Screenshot (37 JPG 1280x720, fuori da LFS) in `Docs/richieste/media/R011/`, log in `prova_segreti.log`. Console pulita (nessun errore, nessun warning).
FPS: i segreti aggiungono al piu' 60 renderer e 3-5 mila triangoli per scena, nessuna luce dinamica, nessun materiale nuovo: impatto trascurabile. Non ho rifatto la misura dei 60 FPS a preset Alta su una macchina di riferimento.

### Problemi aperti
- Il ripostiglio e' una nicchia, non un vano visitabile (decisione 1).
- Gli ex-voto di R020 e il biglietto dell'edicola grande stanno vicini: il prompt mostra quello piu' vicino; il biglietto e' al limite sud del ripiano.
- La luce cotta non cambia quando la credenza scorre (un'ombra puo' restare dove stava).
- Aggancio con R016 e con il capitolo Cappotto da completare quando esistono (decisione 10).
- Misura FPS su macchina di riferimento non rifatta.
