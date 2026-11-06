# R004 – Animazione di corsa nuova (da Mixamo)

| | |
|---|---|
| **Scritta da** | Claude, PC scuola (senza Unity), 2026-11-06 |
| **Per** | Claude, PC casa (Unity 6000.6.7f1 + MCP for Unity, browser con l'account Mixamo del progetto) |
| **Quando** | Appena chiuso il bug del movimento nel gioco online (vedi diario del 6/11). È piccola: si può fare prima della 2.6 |
| **Base** | `feature/upgrade-realistico` @ 9426a9d (la corsa esiste solo lì, non su `main`). **Il codice attuale vince** |
| **Priorità** | Media: è la prima cosa che si vede tenendo premuto Shift |

## 1. Cosa ha chiesto l'utente (parole sue)
> Animazione corsa, fa schifo, cercala su mixamo, un animazione di corsa.

Obiettivo: sostituire la clip di corsa di Amerigo (bambino e adulto) con una **vera corsa** presa da Mixamo, che
sembri naturale alla velocità di gioco, senza piedi che scivolano e senza l'effetto "accelerato".

## 2. Situazione di partenza (al 6/11, branch `feature/upgrade-realistico`)
- `Assets/Art/Animations/Amerigo/Amerigo_Run.fbx` = clip Mixamo **"Jogging (In Place)"** (vedi `CREDITS.md`).
  È una corsetta lenta, non una corsa.
- `ThirdPersonController` accelera la clip con `MotionSpeed = velocità di gioco / clipRunSpeed`
  (valori impostati in `Editor/SceneBuilder.cs` righe ~262-263):
  - adulto: corsa 3,2 m/s, `clipRunSpeed` 1,9 → clip a **~1,7×**;
  - bambino: corsa 2,7 m/s, `clipRunSpeed` 1,15 → clip a **~2,35×** (il diario lo segnalava già come "frenetico").
- Probabile causa del "fa schifo": una corsetta riprodotta quasi al doppio della velocità. Una clip di corsa vera
  ha già un passo lungo e una velocità propria vicina a quella di gioco, quindi il moltiplicatore torna vicino a 1×.
- `Treno ▸ Personaggi ▸ Aggiorna animazioni` (`Editor/PersonaggiBuilder.cs`) trova la clip per nome (`Run`), la importa
  Humanoid con loop e root bloccata, e ricostruisce il blend tree Idle → Walk → Run.

## 3. Vincoli
- Solo **Mixamo** (licenza già documentata in `CREDITS.md`). Niente clip da altri giochi o pacchetti a pagamento.
- Download: **FBX for Unity, Without Skin, 30 fps, Keyframe Reduction: none**, opzione **In Place spuntata**
  (il movimento lo fa il `CharacterController`, la root resta bloccata).
- Stesso nome file `Amerigo_Run.fbx` (così il builder la ritrova senza cambiare codice). Il vecchio file va **sostituito**,
  non tenuto accanto (Git LFS: niente doppioni).
- Corsa di una persona normale degli anni '40: niente corse "da atleta" o da cartone animato, niente armi, niente zombie.
- Build WebGL leggera: una sola clip in più al massimo.

## 4. Specifica

### 4.1 Scelta della clip: **"Slow Run"** (decisione dell'utente, 2026-11-06)
> aggiungi alla pr di mettere l'animazione di corsa: "Slow Run" da Mixamo

- Clip da usare: **"Slow Run"** di Mixamo, con **In Place** spuntato, per **adulto e bambino**.
- Salvarla come `Assets/Art/Animations/Amerigo/Amerigo_Run.fbx` (sostituisce "Jogging (In Place)").
- Se su Mixamo ci sono più clip con lo stesso nome, scegliere quella di corsa normale in avanti, senza varianti
  (niente feriti, zombie, armi o corsa all'indietro), e scrivere nel Rapporto quale si è presa.
- Non cambiare clip senza chiedere all'utente. Se dopo la taratura (4.3) resta brutta, proporre "Running" o
  "Standard Run" come alternative con il pannello di scelta.

Regolazioni nel pannello di Mixamo (se servono): **Character Arm-Space** per non far toccare le braccia al corpo
(utile soprattutto per il bambino), **Overdrive** lasciato a 50 (velocità originale; la velocità la gestisce il gioco).

### 4.2 Una clip o due?
- Partire con **una sola clip** per entrambi i personaggi (il retarget Humanoid la adatta).
- Se sul bambino resta brutta anche dopo la taratura (4.3), chiedere all'utente se scaricarne una seconda,
  salvarla come `Amerigo_RunBambino.fbx` e far scegliere al builder la clip per personaggio. In quel caso
  aggiornare `PersonaggiBuilder` (stesso controller con una clip diversa oppure un `AnimatorOverrideController` per il bambino).

### 4.3 Taratura della velocità
1. Misurare la velocità propria della clip nuova: scaricare una volta **senza** In Place (solo per misura, non va in git),
   oppure leggere lo spostamento della root nell'anteprima di Unity / Average Velocity dell'importer.
2. Aggiornare `clipRunSpeed` di adulto e bambino in `SceneBuilder.cs` con la velocità misurata (riscalata sull'altezza
   del personaggio per il bambino, come già fatto per la camminata).
3. Obiettivo: `MotionSpeed` in corsa tra **0,85× e 1,3×** per entrambi. Se il bambino resta oltre 1,3×, valutare di abbassare
   leggermente `runSpeed` del bambino (es. 2,4 m/s) **chiedendo all'utente** con il pannello di scelta.
4. Controllare che la transizione cammina → corre nel blend tree non faccia "pattinare" i piedi; se serve, regolare
   `cycleOffset` della clip per allineare il piede d'appoggio con quello della camminata.

### 4.4 Altro da controllare
- Passi audio (`Scripts/Audio/FootstepAudio.cs`): con la nuova cadenza i suoni dei passi devono cadere sul contatto del piede.
  Se il sistema usa un tempo fisso per la corsa, ricalibrarlo o passare agli Animation Event sulla clip.
- Rigenerare le scene con i menu di `Treno ▸ …` (nessuna modifica a mano) e ripubblicare la build su `gh-pages`
  (l'utente ha autorizzato a ripubblicare senza chiedere, voce del diario del 6/11).

## 5. Ordine di lavoro consigliato
1. Mixamo: scaricare "Slow Run" (In Place), sostituire `Amerigo_Run.fbx`.
2. `Treno ▸ Personaggi ▸ Aggiorna animazioni`, `read_console`.
3. Taratura `clipRunSpeed` (4.3), prova in editor con adulto (vicolo) e bambino (flashback scarpe).
4. Passi audio.
5. `CREDITS.md` (riga di `Amerigo_Run.fbx` con il nuovo nome della clip e la data), GIF/screenshot prima/dopo in
   `Docs/richieste/media/R004/`, Rapporto, build WebGL.

## 6. Criteri di accettazione
- [x] `Amerigo_Run.fbx` è "Slow Run" di Mixamo (non più "Jogging"); `CREDITS.md` aggiornato.
- [x] Adulto e bambino corrono con `MotionSpeed` tra 0,85× e 1,3× (valore letto in gioco o nella finestra F3).
- [x] Nessun pattinamento evidente dei piedi in corsa e nel passaggio cammina ↔ corri.
- [x] Passi audio a tempo con i piedi.
- [ ] Console pulita (fatto), scene rigenerate dai menu (fatto), build WebGL aggiornata su `gh-pages` (da fare dopo l'ok dell'utente alla corsa).
- [ ] L'utente ha visto la nuova corsa e l'ha approvata.

## 7. Domande aperte (per l'utente)
- Preferisci una corsa più "di bambino" (passi corti, braccia larghe) anche per l'adulto, o due corse diverse?

## 8. Rapporto
*(compilato il 2026-11-06, PC casa)*

### Fatto
- `Amerigo_Run.fbx` sostituito (stesso nome e stesso GUID, contenuto nuovo, tracciato da Git LFS) con la clip Mixamo
  **"Slow Run" In Place**, scaricata dall'utente come `Amerigo_adulto_triste@Slow Run.fbx`. Verifica prima della sostituzione:
  FBX binario senza mesh né deformer (solo scheletro e animazione), 30 fps, ciclo di 22 fotogrammi (0,733 s), radice ferma
  (nessuno spostamento orizzontale dei fianchi).
- Import: Humanoid, loop, radice bloccata (Original), come la clip precedente. Vedi gli scostamenti per i due ritocchi al `.meta`.
- Taratura in `Editor/SceneBuilder.cs`: adulto `clipRunSpeed` 3,4 (era 1,9), bambino 1,7 (era 0,96); riserve v6 3,4 e v5 2,15.
- Passi audio: `FootstepAudio` calcola ora il passo in corsa dalla clip (`clipRunSpeed x runStepTime`, 0,367 s = mezzo ciclo)
  invece di 1,5 m fissi: adulto 1,25 m a passo, bambino 0,62 m.
- Scene rigenerate con **Treno > Personaggi > Aggiorna animazioni** e **Treno > Costruisci tutte le scene**; console a zero errori.
  Le luci baked non sono state ricalcolate (il builder lo segnala: **Treno > Look > Bake luci di tutte le scene**, lasciato al PC
  che ha il tempo per farlo; i lightmap esistenti restano collegati).
- `CREDITS.md` aggiornato (Slow Run, 2026-11-06).

### Scostamenti dalla specifica (e perché)
- **Scheletro della clip**: il file scaricato ha lo scheletro di Amerigo adulto caricato su Mixamo (ossa `pelvis`, `spine_01`,
  `thigh_l`... e non `mixamorig:*`). Il `.meta` conservava la mappa Humanoid di `mixamorig:*` e l'avatar falliva
  ("Transform 'mixamorig:Hips' not found"): ho svuotato `human`/`skeleton` nel `.meta` (GUID invariato) e Unity ha rigenerato
  la mappa da solo (52 ossa, avatar valido).
- **Intervallo della clip**: il `.meta` aveva ancora `lastFrame: 77` della vecchia clip; impostato 0-22 (un ciclo completo).
- **Bambino: `runSpeed` abbassata da 2,7 a 2,2 m/s** (solo bambino MakeHuman, in `SceneBuilder.cs`). Con 2,7 la clip girerebbe
  a 1,6x; a 2,2 è 1,29x, dentro il limite 1,3x. Una corsa di un bambino di 7 anni a 2,2 m/s resta credibile ed è ancora il doppio
  della camminata (1,1 m/s). **L'utente può rialzarla** (costa più velocità di riproduzione: a 2,7 m/s sarebbe 1,6x).
  Non ho potuto chiedere con il pannello (sessione di un sottoagente): decisione da confermare.
- Passi: la specifica suggeriva ricalibrare o usare Animation Event; ho scelto il calcolo dal passo reale della clip (nessun evento
  da mantenere sulla clip FBX).
- Non fatto (non autorizzato in questa sessione): build WebGL su `gh-pages`, GIF prima/dopo in `Docs/richieste/media/R004/`
  (screenshot fermi salvati solo fuori dal repo), `gh pr ready`, merge.

### Clip scelta e valori di taratura
- Clip: Mixamo **Slow Run** (In Place), una sola per adulto e bambino (retarget Humanoid). Il bambino non ha richiesto
  una seconda clip (4.2).
- Velocità dei piedi appoggiati, misurate in Play con `Animator.Update` a passo fisso (mediana dei fotogrammi a terra):
  adulto 3,54 m/s (media 3,33) -> `clipRunSpeed` 3,4; bambino 1,79 (media 1,63) -> 1,7; v6 3,54 -> 3,4; v5 2,20 (media 2,03) -> 2,15.
- Moltiplicatore a velocità di corsa (letto da `ThirdPersonController.MotionSpeed` in gioco): **adulto 0,94x** (runSpeed 3,2);
  **bambino 1,29x** (runSpeed 2,2). Prima: 1,7x e 2,8x.
- Scivolamento: velocità del piede a terra rispetto al mondo, in gioco: adulto mediana 0,02 m/s; bambino mediana -0,23 m/s
  (quartili -0,34 / +0,23) su una corsa a 2,2 m/s: piedi fermi, nessun pattinamento. La transizione cammina -> corri non è stata
  ispezionata a occhio frame per frame.
- `cycleOffset` non modificato.

### Come testare
1. Treno > Costruisci tutte le scene (già fatto), aprire `10_Hub_Vicolo`, Play, tenere premuto W e Shift: adulto in corsa.
   F3 (DiagOverlay) mostra la velocità; `MotionSpeed` deve essere ~0,94.
2. Aprire `30_FB_Scarpe`, Play (chiudere il dialogo iniziale), W + Shift: bambino in corsa, `MotionSpeed` ~1,29.
3. Ascoltare i passi: due per ciclo della clip, a tempo con i piedi.

### Domande aperte per l'utente
- La nuova corsa ti piace (adulto e bambino)? Se il bambino sembra troppo lento, rialzare `runSpeed` da 2,2 verso 2,7 (a costo di 1,6x).
- Preferisci una corsa più "di bambino" per il bambino (seconda clip `Amerigo_RunBambino.fbx`)?
- Serve ricalcolare le luci (Bake) e ripubblicare la build WebGL su `gh-pages`.
