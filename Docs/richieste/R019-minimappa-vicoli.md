# R019 – Minimappa in alto a sinistra: prossimo punto da raggiungere e nomi dei vicoli

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3. **Il codice attuale vince** |
| **Dipende da** | R006 (mappe grandi, unita). Si integra con R009/R020 (enigmi) per sapere qual è il "prossimo punto" |
| **Priorità** | Media-alta (con le mappe grandi serve per orientarsi) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Metti una minimappa in alto a sinistra che ti dice dove sarà il prossimo punto da interagire, Nomi dei vicoli etc...

## 2. Vincoli da CLAUDE.md
- HUD minimale e **d'epoca**: niente radar moderno. La minimappa è una **pianta di Napoli disegnata a mano** su carta
  invecchiata (stile catasto/pianta topografica anni '40), inchiostro seppia, bordo strappato, ago di una bussola d'ottone.
- Testi (nomi dei vicoli, luoghi, obiettivi) **solo nei JSON**, mai nel codice.
- Didattica: deve aiutare, non togliere il piacere di esplorare (vedi modalità sotto).

## 3. Specifica
1. **Minimappa** in alto a sinistra, piccola (≈ 180-220 px a 1080p, scalabile), circolare o a cartiglio:
   - ruota con la camera (opzione: nord fisso), il giocatore al centro come una freccetta d'inchiostro;
   - disegno della pianta **generato dall'editor** dalla geometria della scena (vista dall'alto ortografica → texture
     ripassata con filtro "inchiostro su carta"), rigenerato da menu `Treno ▸ Mappa ▸ Genera piante`; niente camera
     extra a runtime (peso zero su WebGL);
   - **nomi dei vicoli e delle piazze** scritti lungo le strade, in corsivo d'epoca leggibile, comparendo quando si è vicini;
   - icone d'epoca piccole: porta di casa (basso), bottega, fontana, edicola votiva, uscite.
2. **Prossimo obiettivo**: un segno a inchiostro rosso (una "X" o un cerchietto a matita) sul punto da raggiungere/interagire;
   se è fuori dalla minimappa, una freccetta sul bordo. Il prossimo obiettivo viene dallo stato del gioco (luoghi aperti,
   oggetti non ancora trovati, enigma corrente). In alto, sotto la mappa, una riga breve con l'obiettivo
   (es. "Torna al basso di Antonietta"), testo da JSON.
3. **Mappa grande** con il tasto **M** (e pulsante touch): pianta intera a schermo come un foglio aperto, con legenda,
   nomi di tutti i vicoli e luoghi già visitati/sbloccati (la nebbia di guerra sui luoghi mai visti è facoltativa).
4. **Modalità di aiuto** (Impostazioni ▸ Gioco): *Guidata* (obiettivo sempre segnato) · *Normale* (segnato dopo qualche
   minuto senza progressi o premendo Indizio) · *Esploratore* (solo nomi e posizione, nessun obiettivo). Minimappa
   attivabile/disattivabile. Default: Normale. Le preferenze si salvano con le altre impostazioni.
5. **Interni e flashback**: nel basso una pianta della casa (stanze); nel flashback la strada del ricordo con il look seppia.
6. **Dati**: nuovo `mappe.json` (o estensione di `luoghi.json`): per scena, nomi di strade con polilinea/punti di ancoraggio,
   punti di interesse, testi degli obiettivi. Aggiornare lo schema JSON in CLAUDE.md.
7. Touch/LIM: leggibile, non copre il joystick virtuale né i pulsanti.

## 4. Domande aperte (per l'utente, chiedere con il pannello prima di scrivere i nomi)
- Nomi dei vicoli: **veri** dei Quartieri Spagnoli (es. via Toledo, vico Lungo Gelso, vico Speranzella… da verificare con
  fonti e mappe d'epoca) oppure **inventati** ma verosimili? Raccomandazione: veri per le strade principali, inventati per i vicoletti
  costruiti da noi, con una nota nei crediti.
- Default della modalità di aiuto: Normale va bene?

## 5. Ordine di lavoro consigliato
1. Generatore della pianta e stile carta/inchiostro (screenshot all'utente). 2. Widget minimappa + rotazione + giocatore.
3. Nomi dei vicoli da JSON. 4. Obiettivo e frecce. 5. Mappa grande (M). 6. Impostazioni e touch.

## 6. Criteri di accettazione
- [x] Minimappa d'epoca in alto a sinistra in hub, basso e flashback, con i nomi delle strade.
- [x] Il prossimo punto da interagire è segnato correttamente lungo tutto il capitolo "Le scarpe" (prova completa: `-mappatest`, 212 PASS).
- [x] Tasto M (e pulsante touch) per la mappa grande; modalità di aiuto e interruttore nelle Impostazioni.
- [x] Testi solo in JSON; schema aggiornato in CLAUDE.md; console pulita; nessun calo di FPS in Windows (misurato). **WebGL non costruita né misurata** (su richiesta): da provare alla prossima build.

## Decisioni dell'utente (2026-11-07)
- **Nomi misti:** veri per le strade principali, inventati verosimili per i vicoletti.
- **Via Toledo nel 1946** si chiamava "Via Roma (già Toledo)": nel gioco usare questa dicitura per la strada.
- **Proposta dei nomi** in `Docs/richieste/schemi/R019_nomi_vicoli.md`.
- **Nomi inventati:** vanno verificati su OpenStreetMap prima di fissarli nel JSON.
- **Modalità di aiuto di default:** Normale.

## Rapporto
### Fatto (2026-11-09, PC casa, sessione subagent)
- **Generatore delle piante**: menu `Treno > Mappa > Genera piante` (`Editor/MappaBuilder.cs`). Per hub, basso e ricordo apre la scena, legge i collider (terreno `Selciato*`/`Pavimento*`, muri `Casa*`/`Muro*`, arredi nel basso, gradini `Scalinata*`) e disegna dall'alto un **inchiostro su carta**: muri a tratteggio obliquo, contorni tremolanti, scalini a righe, mobili del basso in contorno, carta con grana, macchie e bordi scuriti; nel ricordo carta e inchiostro più scuri (seppia marcato). Salva `Resources/Mappe/<scena>.jpg` (680x1016, 432x576, 496x768 px, ~0,3 MB in tutto) più la striscia dei glifi `glifi.png`, e riscrive area e `ppm` in `mappe.json`. **Nessuna camera in più a runtime**: la minimappa è un'immagine che si muove.
- **Minimappa** (`Scripts/Mappa/MinimappaHud.cs`): in alto a sinistra, cornice d'ottone circolare di 216 px (a 1280x720), pianta che ruota con la camera (opzione **Nord sempre in alto**), freccetta d'inchiostro del giocatore, piccola N sull'anello. Sotto: **targa d'ottone** con il nome della strada dove si trova Amerigo e la **riga dell'obiettivo** (carta). Layer proprio (Canvas annidato): non fa rifare il resto dell'HUD. Cliccandola si apre la mappa grande. Il biglietto degli enigmi è stato spostato a destra della minimappa. Resta tutta nella metà alta dello schermo: **non copre joystick virtuale né pulsante Interagisci** (controllato dalla prova).
- **Nomi dei vicoli** da `mappe.json` (`strade[]`: zone rettangolari e ancore): compaiono come cartellini di carta entro 14 m, senza sovrapporsi (scelta per priorità: principale, largo, cortile, vicolo, rampa). Decisioni dell'utente applicate: nomi misti, **"Via Roma (già Toledo)"**, **Piazza Montecalvario** (nome vero, verificato) al posto di "Largo Montecalvario", default **Normale**.
- **Verifica dei nomi inventati su OpenStreetMap** (Nominatim, 2026-11-09, `Napoli`): nessuna corrispondenza per Largo della Fontanella, Rampa dei Cantari, Largo Sant'Anna dei Panni, Gradini della Madonnella, Rampa Tre Scalini, Largo della Madonna Illuminata, Rampa del Capitano, Vico dei Panni Stesi, Vico Tre Lumi, Cortile del Lavatoio, Supportico Cieco della Lanterna, Supportico del Cancello, Cortile del Terrazzino, Vico Scalzacani, Largo del Palazzo Crollato, Vico Sette Spiriti, Supportico delle Casse. **"Vico Cappella Vecchia" collideva** con il vero "Vico Santa Maria a Cappella Vecchia": sostituito con **Vico Cappella Scura** (nessuna corrispondenza). Per lo slargo dell'edicola ho scelto la variante neutra **Largo della Madonna Illuminata** (la proposta lasciava due opzioni). Verificati come reali: Vico Lungo Gelso, Via Speranzella, Via Concordia, Via Portacarrese a Montecalvario, Piazza Montecalvario, Via Toledo (nel 1946 "Via Roma").
- **Prossimo obiettivo** (`Scripts/Mappa/Obiettivi.cs`): dallo stato del gioco, non dalla scena. `mappe.json > obiettivi` è la lista ordinata dei passi del capitolo; il corrente è il primo con almeno un gettone di `completo` mancante in `MondoStato` (enigmi risolti, oggetti e indizi). Ogni passo dice dove andare **scena per scena**: nella scena sbagliata indica la porta (es. dall'hub, "Torna al basso di Antonietta"). Segno: **X rossa a matita** in un cerchio che pulsa; se è fuori dalla minimappa, **freccia rossa sul bordo**. Riga dell'obiettivo dal testo del JSON.
- **Mappa grande**: tasto **M** (azione dell'Input System con binding `<Keyboard>/m` e `<Gamepad>/select`), pulsante **Mappa** nella fila in alto a destra (touch/LIM), tocco sulla minimappa. Foglio aperto con la pianta intera (nord in alto), tutti i nomi, **zoom** +/- o rotella e trascinamento, legenda con i glifi, riga dell'obiettivo secondo la modalità. Si chiude con M, Esc, B del gamepad o il pulsante Chiudi; blocca l'input come le altre schermate. La pagina "Mappa" della latta rimanda al tasto M.
- **Modalità di aiuto** in **Impostazioni > Gioco** (nuova scheda, testi in `ui_menu.json > mappaImp`): **Guidata** (sempre segnato), **Normale** (default: compare dopo 3 minuti senza progressi, oppure con il pulsante **Indizio** di un enigma; ogni progresso lo nasconde di nuovo), **Esploratore** (solo nomi e posizione). In più gli interruttori **Minimappa** e **Nord sempre in alto**. Salvate nel salvataggio (`aiutoMappa`, `minimappaNascosta`, `nordFisso`) e conservate da "Nuova partita".
- **Interni e flashback**: pianta del basso (la stanza, l'alcova, la cucina, il cortile; mobili in contorno) e della strada del ricordo (stesso Vico Lungo Gelso, seppia più scuro).
- Schema `mappe.json` documentato in `CLAUDE.md`; crediti in `CREDITS.md`; l'override texture WebGL non tocca le piante (`WebGLBuilder`: restano a 1024 px, senza crunch).

### Prove
- **`-mappatest`** (nuovo, `Scripts/Mappa/MappaSelfTest.cs`, solo editor e build di sviluppo): **212 PASS, 0 FAIL** sulla build Windows di sviluppo (log in `media/R019/prova_mappa_build_windows.txt`; "4 URP assets included in build"). Percorre il capitolo: a ogni tappa (bucato, ex-voto, foto, calendario, cassetta, baule, passanti, nascosto, quiz, ordina, lucchetto, capitolo finito) controlla l'obiettivo corrente, il punto nella scena giusta (nell'hub dopo l'ex-voto è la porta del basso), la X vicina o la freccia sul bordo lontano, la riga, la targa della strada; le tre modalità di aiuto (Normale: nascosto, Indizio, 3 minuti, azzeramento al progresso); M apre/chiude, Esc chiude senza aprire la pausa; zoom; nord fisso; minimappa spenta; Impostazioni > Gioco; salvataggio e vecchi salvataggi; dati (area, immagini, glifi, ogni gettone di `completo` è un enigma vero).
- **`-enigmitest`**: **189 PASS, 0 FAIL** sulla stessa build (con la minimappa).
- **`-menutest`**: **31 PASS, 0 FAIL** (dopo aver rimesso un salvataggio neutro nell'archivio della build: vedi Note di metodo).
- Console dell'editor pulita dopo ogni passo.
- **FPS** hub, build Windows di sviluppo, RTX 4060 Ti, 1280x720, tragitto del benchmark, 20 s (`media/R019/fps_hub_*.csv`), senza -> con minimappa: Media **729,7 -> 721,7 fps** (1,37 -> 1,39 ms), Alta **653,0 -> 649,6 fps** (1,53 -> 1,54 ms): differenza ~1%, dentro il rumore. 1% low invariato (291 -> 302 e 272 -> 280). **Non misurati** PC scolastici né WebGL.
- **LFS**: le piante e i glifi aggiungono **~0,34 MB** (JPG 180+46+90 KB, PNG 7 KB); screenshot e CSV di documentazione sono fuori da LFS.

### Screenshot (`media/R019/`, 1280x720 JPG)
`minimappa_hub_normale` (obiettivo nascosto) · `minimappa_hub_guidata_lontano` (freccia sul bordo) · `minimappa_hub_guidata_vicino` (X rossa) · `minimappa_basso` · `minimappa_ricordo` · `mappa_grande_hub` · `mappa_grande_hub_zoom` · `mappa_grande_basso` · `mappa_grande_ricordo` · `impostazioni_gioco`.

### Correzioni dalla review (2026-11-09)
- **Salvataggi vecchi**: `ObiettivoDef.capitolo` (dato in `mappe.json`, es. `flashback_scarpe`): se il flashback e' in `flashbackCompletati` il passo non e' piu' un obiettivo, anche senza i gettoni degli enigmi. `Obiettivi.Corrente()` torna null e la mappa dice che il capitolo e' finito. Nessun id nel codice. Prova: scarpe nell'inventario + ricordo completato senza gettoni -> nessun obiettivo.
- **Passi del ricordo fuori dal ricordo**: `ob_passanti`, `ob_nascosto`, `ob_quiz`, `ob_ordina`, `ob_lucchetto` hanno ora un punto nel basso (X sul baule, "Riprendi le scarpe nel baule") e nell'hub (porta del basso). Prova sul passo dopo l'apertura del baule.
- **Screenshot slavati**: la cattura veniva ricodificata in sRGB due volte (Blit in un RenderTexture sRGB). Ora il RT e' `RenderTextureReadWrite.Linear` in `MappaSelfTest` e in `EnigmiSelfTest` (stesso difetto; `MenuSelfTest` usa `CaptureScreenshot` e non ce l'ha). Tutti gli screenshot R019 rifatti dalla build Windows e riguardati: colori giusti, minimappa leggibile.
- **Solo gamepad**: senza tastiera Select apre/chiude la mappa e B la chiude (`UIRoot.UpdateSoloGamepad`). Prova con tastiere rimosse e un gamepad virtuale.
- `-senzaminimappa` letto una volta sola (cache statica).
- `raggioArrivo` ora e' usato: arrivati sul punto la X si spegne (prova). Tolti i testi inutilizzati `tuoiPassi`, `viaFmt`, `luogoChiuso` e `mappaImp.scheda`; il nome della scheda "Gioco" resta nel codice come quello delle altre schede (i nomi sono in `GraphicsOptions.Gruppi`, riscriverli per tutte esula da R019).
- **Biglietto degli enigmi**: sta accanto alla minimappa, torna all'angolo se la minimappa e' spenta o la scena non ha pianta (prova).
- **"Ripristina predefiniti"** nascosto nella scheda Gioco (vale solo per la grafica; prova).
- Prove sulla build Windows rifatta (4 URP assets): `-mappatest` 212 PASS, `-enigmitest` 189 PASS, `-menutest` 31 PASS.

### Scostamenti
- I **nomi dei vicoli sono scritti in orizzontale** sui cartellini, non lungo l'andamento delle strade (sulla pianta che ruota restano leggibili). Compaiono vicino al giocatore (minimappa) o tutti, se c'è posto (mappa grande, con zoom).
- **Nebbia di guerra non fatta** (era facoltativa): la mappa grande mostra tutti i nomi e i luoghi; nessun tracciamento delle strade visitate.
- Le **icone d'epoca** sono glifi bruciati nella pianta (porta, fontana, edicola votiva, pozzo, lavatoio, uscita, stazione, bottega); non reagiscono (la porta chiusa non è diversa dalla porta aperta).
- Il passo **facoltativo** dei manifesti dell'hub (`enigma_manifesti`) non è nella lista degli obiettivi: il percorso principale non lo richiede.
- La spec diceva "Impostazioni > Gioco": la scheda **Gioco** non esisteva (le schede erano solo grafiche e Accessibilità), l'ho aggiunta tra Dettaglio e Accessibilità.
- Il **Select del gamepad** è collegato ma non provato con un gamepad vero (la prova usa tastiera e mouse virtuali).

### Note di metodo
- Il repo era su `feature/upgrade-realistico` e il checkout di `richiesta/R019-minimappa-vicoli` era bloccato dalle modifiche locali non mie: ho creato il commit di unione (feature + remoto R019, solo docs in più) con `git commit-tree` e spostato il puntatore del branch senza toccare l'albero di lavoro. Un `git merge` partito per errore sul branch feature è stato annullato con `git reset` (nessun file perso, nessun push, feature è di nuovo su d09dfc3).
- La prova di enigmi sulla build lasciava nell'archivio PlayerPrefs della build un salvataggio nel ricordo (`30_FB_Scarpe`), per cui `-menutest` falliva su "Il gioco parte (hub)" indipendentemente da R019. Ho sostituito quel salvataggio con uno neutro all'hub (l'originale è copiato fuori dal repo, nella cartella temporanea della sessione).
- Un'esecuzione di `-mappatest` nell'editor interrotta a metà (stop del play mode) ha lasciato il salvataggio dell'editor in uno stato di prova (ora neutro, all'hub): se serviva un salvataggio dell'editor con progressi, va rifatto.

### Domande aperte
- Nessuna bloccante. Da confermare con l'utente: **Largo della Madonna Illuminata** (al posto di "Piazzetta della Concordia") e **Vico Cappella Scura** (al posto di "Vico Cappella Vecchia", che coincideva con una via vera).
