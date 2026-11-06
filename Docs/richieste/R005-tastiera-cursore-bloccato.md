# R005 – Inventario e pausa da tastiera, cursore bloccato per girare la visuale

| | |
|---|---|
| **Scritta da** | Claude, PC scuola (senza Unity), 2026-11-06 |
| **Per** | Claude, PC casa (Unity 6000.6.7f1 + MCP for Unity) |
| **Quando** | Insieme al bug del movimento nel gioco online (diario del 6/11): probabilmente hanno la stessa causa |
| **Base** | `feature/upgrade-realistico` @ 9426a9d. **Il codice attuale vince** |
| **Priorità** | Alta: riguarda i comandi base su PC |

## 1. Cosa ha chiesto l'utente (parole sue)
> Aggiungi la possibilità di aprire l'inventario e mettere pausa da tastiera, blocca il cursore del mouse permettendo
> di girare la visuale senza aver bisogno di cliccare il mouse.

Obiettivo: su PC i comandi devono essere quelli di un gioco in terza persona normale. Il mouse gira la visuale
**senza tenere premuto alcun tasto** (cursore nascosto e bloccato), e inventario e pausa si aprono da tastiera.

## 2. Situazione di partenza (al 6/11)
- **Tastiera:** `UIRoot.cs` (righe ~512-515) gestisce già **I** = inventario e **Esc / P** = pausa. L'utente però dice
  che "non c'è". Ipotesi da verificare per prima: nel gioco online **la tastiera non arriva al canvas** (stesso sintomo del bug
  "Amerigo non si muove", vedi diario del 6/11). Quindi questa richiesta e quel bug vanno risolti insieme.
  Altre cause possibili: `menuScene`, `Cutscene.IsPlaying`, `Fader.Busy` o `lastModalPopFrame` che restano veri e bloccano i tasti.
- **Camera:** `CameraRig.cs` gira la visuale solo con il **tasto destro premuto** (`rmb`). In WebGL il cursore non viene mai
  bloccato (`SetDragging`: commento "pointer lock fragile nel browser"). Tasto destro senza menu contestuale grazie a
  `Assets/Plugins/WebGL/TrenoContextMenu.jslib`.
- Clic sinistro: usato da `PlayerInteractor` (click sugli oggetti) e da `InputHelper` (conferma nei dialoghi).

## 3. Vincoli
- Nuovo Input System (come ora). Touch e gamepad **non cambiano**: il blocco del cursore vale solo in modalità PC
  (`UIRoot.TouchMode` falso).
- Nel browser il pointer lock lo concede solo un **gesto dell'utente** (clic) e il browser lo toglie sempre con **Esc**:
  è un comportamento del browser, non si può evitare. Il design deve tenerne conto (vedi 4.2).
- Didattica: un ragazzo alla LIM o al Chromebook deve capire subito cosa fare. Testi brevi, in italiano.

## 4. Specifica

### 4.1 Tasti
| Tasto | Azione |
|---|---|
| **I** oppure **Tab** | apre/chiude l'inventario |
| **Esc** oppure **P** | apre/chiude la pausa |
| E | interagisce (come ora) |
| F3 | diagnosi (come ora) |

- I tasti funzionano nel vicolo, negli interni e nei flashback, **non** durante caricamenti, cutscene e enigmi aperti
  (dove Esc chiude il pannello, come già fa `PuzzlePanel`).
- Tab nel browser sposta il focus fuori dal canvas: va intercettato (`preventDefault`) nel jslib solo quando il gioco ha il focus.
- Mostrare i tasti in pausa (sezione "Comandi") e una volta, all'inizio della partita, in un toast:
  "Muovi il mouse per guardarti intorno · I inventario · Esc pausa".

### 4.2 Cursore bloccato (modalità "esplorazione")
- Quando si gioca liberamente su PC: cursore **nascosto e bloccato** (`CursorLockMode.Locked`), la camera segue il
  **movimento del mouse senza clic**. Sensibilità e Y invertita come ora (`CameraSettings`).
- Il cursore torna **libero e visibile** quando si apre qualcosa che va cliccato: inventario, pausa, dialoghi con scelte,
  enigmi, cutscene, titolo, schermate di caricamento. Quando si chiude, il blocco si riprende da solo (in WebGL:
  al primo clic sul gioco, vedi sotto). Centralizzare la decisione in un solo punto (es. `CursorManager` in `Core` che guarda
  `GameManager.InputBlocked`, i menu aperti e `TouchMode`), invece di sparpagliare `Cursor.lockState` nei vari pannelli.
- **WebGL:**
  - il blocco parte al **clic sul canvas** (il browser lo richiede). Finché non è bloccato, al centro dello schermo un
    cartellino "Clicca per giocare" (stile UI d'epoca di R001).
  - Quel primo clic **non** deve contare come interazione (`PlayerInteractor`) o conferma.
  - Se il browser toglie il blocco (Esc, Alt+Tab, cambio scheda) mentre si esplora → **aprire la pausa** automaticamente.
    Così Esc continua a "mettere in pausa" anche se il tasto lo consuma il browser. Chiudendo la pausa con "Riprendi"
    (che è un clic) il blocco riparte.
  - Il browser rifiuta una nuova richiesta di blocco per ~1 s dopo un'uscita con Esc: gestire l'errore senza spam in console
    e riprovare al clic successivo.
  - Se serve, aggiungere le funzioni di pointer lock nel jslib esistente (o in uno nuovo) invece di fidarsi solo di `Cursor.lockState`.
- **Editor/Standalone:** stesso comportamento con `Cursor.lockState`; Esc libera il cursore e apre la pausa.
- Il tasto destro tenuto premuto può restare come alternativa (non dà fastidio), ma non deve più servire.
- Opzione in pausa ▸ Comandi: **"Gira la visuale: muovendo il mouse / tenendo premuto il tasto destro"**, predefinito
  "muovendo il mouse", salvata in `SaveData` (per chi alla LIM preferisce il cursore libero).

### 4.3 Interazione con il clic
- Con il cursore bloccato non si può cliccare su un oggetto preciso: l'interazione su PC passa a **E** sull'oggetto evidenziato
  (come ora) e anche al **clic sinistro** sull'oggetto evidenziato più vicino/al centro dello schermo. Il cartellino "E …" resta.

## 5. Ordine di lavoro consigliato
1. Riprodurre nel gioco online il problema dei tasti (I/Esc) insieme al bug del movimento, con la finestra F3. Correggere la causa.
2. Tab come secondo tasto dell'inventario, testi dei comandi.
3. `CursorManager` + modalità esplorazione in editor, poi in WebGL (pointer lock, "Clicca per giocare", pausa all'uscita).
4. Opzione in pausa, salvataggio, prova su Chrome e (se possibile) su un Chromebook/LIM.
5. Build WebGL su `gh-pages`, Rapporto, voce nel diario.

## 6. Criteri di accettazione
- [x] Nel gioco online, su Chrome: **I/Tab** aprono e chiudono l'inventario, **Esc/P** la pausa. *(provato su build WebGL locale in Chrome con eventi da tastiera; da confermare dall'utente con la tastiera vera)*
- [x] Su PC la visuale gira muovendo il mouse, senza tenere premuto nessun tasto; il cursore non si vede. *(editor; in WebGL provato con il blocco simulato, vedi Rapporto)*
- [x] Il cursore ricompare in inventario, pausa, dialoghi, enigmi e cutscene, e si ri-blocca chiudendoli.
- [x] In WebGL, premendo Esc durante l'esplorazione si apre la pausa (nessuno stato in cui il cursore è libero ma non c'è un menu). *(logica provata simulando la perdita del blocco; con l'Esc vero del browser da confermare)*
- [x] Il primo clic per bloccare il cursore non attiva oggetti.
- [x] Touch e gamepad funzionano come prima; joystick a schermo e pulsanti invariati. *(touch invariato; il joystick su PC compare solo con la visuale "tasto destro"; il gamepad conta dal primo uso)*
- [x] Opzione per tornare al tasto destro, salvata.
- [ ] Console pulita, build WebGL aggiornata, l'utente ha provato e approvato. *(console pulita; build su gh-pages e prova dell'utente da fare)*

## 7. Domande aperte (per l'utente)
- Il clic sinistro con il cursore bloccato deve interagire (come E) o restare inutilizzato? **Deciso dall'utente: nessuna azione** (si usa E).

## 8. Rapporto
*(PC casa, 2026-11-06, Claude con Unity 6000.6.7f1 + MCP for Unity)*

### Fatto
- **`Core/CursorManager.cs`** (nuovo, creato da `GameManager`): unico punto che decide cursore bloccato/libero. Esplorazione =
  scena di gioco, nessun blocco dell'input, nessun menu/dialogo/enigma/cutscene/caricamento/dissolvenza. Su PC (non touch) con la
  visuale "muovendo il mouse" il cursore è bloccato e nascosto. Editor/Standalone: `Cursor.lockState`. WebGL: pointer lock tramite il
  nuovo **`Assets/Plugins/WebGL/TrenoInput.jslib`** (richiesta subito dopo un clic, errori assorbiti in silenzio, stato vero letto da
  `document.pointerLockElement`). Con il cursore bloccato l'EventSystem è spento (niente clic "alla cieca" sulla UI nel punto in cui il
  cursore è sparito). Se il blocco si perde mentre si esplora (Esc del browser, Alt+Tab, Esc in editor) si apre la **pausa**; un Esc
  che arriva subito dopo non la richiude.
- **"Clicca per giocare"**: cartellino di carta al centro (layer Schermate) quando il blocco serve ma il browser non l'ha ancora dato.
  Quel clic (e i 0,25 s dopo il blocco) non attiva oggetti né conferme (`CursorManager.SwallowPointer`, usato da `PlayerInteractor`
  e `InputHelper`).
- **Camera** (`CameraRig`): gira col mouse senza tasti quando il cursore è bloccato; il tasto destro tenuto premuto resta come
  alternativa. La gestione del cursore è uscita da `CameraRig`.
- **Clic sinistro con il cursore bloccato** = **nessuna azione** (decisione dell'utente, sostituisce la proposta del punto 4.3): si interagisce con E.
- **Tasti** (`UIRoot`): I **o Tab** = valigia, Esc **o P** = pausa. Il jslib fa `preventDefault` su Tab solo se il focus è sul canvas
  o sulla pagina; un clic sul canvas gli dà il focus. `WebGLInput.captureAllKeyboardInput = true` esplicito.
- **Comandi**: telegramma una volta per sessione al primo ingresso in gioco ("Muovi il mouse per guardarti intorno · I inventario ·
  Esc pausa"); in pausa voce **Comandi** (lettera con tutti i tasti) e voce **"Visuale: muovendo il mouse / col tasto destro"**,
  salvata in `SaveData.visualeTastoDestro` (conservata da Nuova partita). Voci della pausa un po' più compatte per starci a 720p.
- **Joystick a schermo**: visibile in modalità touch oppure con la visuale "tasto destro" (LIM che simulano il mouse). Su PC con la
  visuale col mouse non compare più (in WebGL `Touchscreen.current` esiste sempre, per questo si vedeva).
- **Movimento più robusto** (`ThirdPersonController`, `InputHelper`): il gamepad conta solo dopo che è stato usato davvero (un tasto o
  una leva mossa rispetto al riposo letto all'inizio), così un gamepad "fantasma" o con un asse fermo a fondo scala non annulla WASD e
  joystick; leve senza NaN; `CharacterController.minMoveDistance = 0`; velocità azzerata se diventa NaN.
- **Diagnosi F3**: nuova riga "Cursore bloccato/libero · vuole blocco · esplora · visuale · gamepad usato".

### Scostamenti dalla specifica (e perché)
- In WebGL il blocco non passa da `Cursor.lockState` ma dal jslib: Unity ripete la richiesta al primo evento successivo e, con il rifiuto
  del browser entro ~1 s da Esc, lascerebbe errori in console; così le richieste partono solo al clic e gli errori sono assorbiti.
- Joystick a schermo nascosto su PC con la visuale col mouse (resta su touch e con l'opzione "tasto destro").
- L'opzione è un pulsante a due stati nella pausa, non una sottosezione "Comandi" (la pausa non ha sottopagine); "Comandi" apre una lettera.
- Build WebGL fatta solo in locale (fuori dal repo); la pubblicazione su gh-pages la decide la sessione principale.

### Causa del problema dei tasti nel gioco online
- **Non riprodotto.** Sul Chrome del PC di casa (Chrome 156, nessun gamepad, nessun touch, 82-165 FPS), con la build pubblicata
  (gh-pages 41101b7): F3 riceve i tasti, **W tenuto premuto muove Amerigo** (anche con il focus sulla pagina e non sul canvas:
  `captureAllKeyboardInput` funziona), **I apre la valigia**, il joystick a schermo trascinato col mouse lo muove. Stesso esito sulla
  build locale con R005. Su questo PC sono quindi esclusi: focus del canvas, `activeInputHandler` (= solo nuovo Input System),
  template HTML, EventSystem che consuma l'input, blocchi dell'input appesi.
- **I/Esc "non c'è"**: con ogni probabilità i tasti funzionavano ma non erano indicati da nessuna parte (ora: telegramma iniziale,
  voce Comandi in pausa, Tab in più).
- **Amerigo fermo**: ipotesi più probabile rimasta, ora neutralizzata: un **gamepad "fantasma" con un asse fermo a fondo scala**
  (driver virtuali, volanti/pedaliere, controller collegato) che, sommato a W o al joystick, annulla il movimento; la zona morta 0,25
  non basta contro un asse a fondo scala. Ora il gamepad è ignorato finché non lo si usa. Altre cause possibili: build vecchia nella
  cache del browser, oppure un altro PC/browser. **Da confermare dall'utente** con F3 (righe "Gamepad" e "gamepad usato").

### Come testare
1. **Editor** (Play da 01_Titolo ▸ Nuova partita): la visuale gira muovendo il mouse senza tasti, il cursore sparisce; I/Tab ed Esc/P
   aprono e chiudono valigia e pausa e il cursore ricompare; chiudendo si ri-blocca; Esc in editor libera il cursore e apre la pausa.
   Provato via MCP: W muove (z -13 → -0,7), Tab/I, Esc/P, mouse +200 px → camera +24° senza tasti, perdita del blocco → pausa,
   dialogo → cursore libero, opzione "tasto destro" → cursore libero e joystick visibile, telegramma dei comandi. Console senza errori.
2. **WebGL** (build locale in Chrome su 127.0.0.1): "Clicca per giocare" compare; Tab non porta il focus fuori dal canvas e apre la
   valigia; Esc apre la pausa, P la chiude; W muove; con il blocco **simulato** il mouse senza tasti gira la camera e la perdita del
   blocco apre la pausa; console del browser senza errori. Il **pointer lock vero** non si è potuto provare: nel Chrome pilotato
   dall'estensione la richiesta parte con l'attivazione utente valida, ma Chrome risponde `WrongDocumentError` (finestra pilotata);
   l'errore è assorbito senza spam.
3. **Utente, gioco online** dopo la nuova build: Ctrl+F5 ▸ Nuova partita ▸ clic su "Clicca per giocare" ▸ muovere il mouse (visuale che
   gira, cursore nascosto) ▸ WASD ▸ I/Tab ▸ Esc (si apre la pausa) ▸ Riprendi (se ricompare il cartellino, un clic). Se Amerigo resta
   fermo: F3 + W premuto + screenshot.

### Domande aperte per l'utente (risolte)
- Clic sinistro con il cursore bloccato: **nessuna azione**, deciso dall'utente. Testo dei comandi aggiornato ("E: interagire").
- Joystick a schermo nascosto su PC con la visuale col mouse: **va bene**, deciso dall'utente.

### Robustezza del pointer lock (revisione)
- La richiesta del blocco parte ora **dentro il listener `mousedown` del canvas** (gesto utente, valido anche per Firefox e Safari): il C#
  imposta solo `window.__trenoWantLock`. La richiesta da `Update` resta come ripiego (es. dopo "Riprendi").
- Flag `exitRequested`: quando il C# chiede l'uscita dal blocco, il successivo `pointerlockchange` non viene scambiato per perdita
  e non apre la pausa; una perdita non voluta (Esc, Alt+Tab) è segnalata una sola volta a `CursorManager`.
- Se il browser rifiuta il blocco almeno 2 volte dopo un clic, il cartellino "Clicca per giocare" aggiunge di scegliere
  "Visuale: col tasto destro" nella pausa.
