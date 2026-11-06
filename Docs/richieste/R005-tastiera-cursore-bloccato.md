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
- [ ] Nel gioco online, su Chrome: **I/Tab** aprono e chiudono l'inventario, **Esc/P** la pausa.
- [ ] Su PC la visuale gira muovendo il mouse, senza tenere premuto nessun tasto; il cursore non si vede.
- [ ] Il cursore ricompare in inventario, pausa, dialoghi, enigmi e cutscene, e si ri-blocca chiudendoli.
- [ ] In WebGL, premendo Esc durante l'esplorazione si apre la pausa (nessuno stato in cui il cursore è libero ma non c'è un menu).
- [ ] Il primo clic per bloccare il cursore non attiva oggetti.
- [ ] Touch e gamepad funzionano come prima; joystick a schermo e pulsanti invariati.
- [ ] Opzione per tornare al tasto destro, salvata.
- [ ] Console pulita, build WebGL aggiornata, l'utente ha provato e approvato.

## 7. Domande aperte (per l'utente)
- Il clic sinistro con il cursore bloccato deve interagire (come E) o restare inutilizzato?

## 8. Rapporto
*(da compilare da chi implementa)*

### Fatto
### Scostamenti dalla specifica (e perché)
### Causa del problema dei tasti nel gioco online
### Come testare
### Domande aperte per l'utente
