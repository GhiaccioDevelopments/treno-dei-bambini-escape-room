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
- [ ] `Amerigo_Run.fbx` è "Slow Run" di Mixamo (non più "Jogging"); `CREDITS.md` aggiornato.
- [ ] Adulto e bambino corrono con `MotionSpeed` tra 0,85× e 1,3× (valore letto in gioco o nella finestra F3).
- [ ] Nessun pattinamento evidente dei piedi in corsa e nel passaggio cammina ↔ corri.
- [ ] Passi audio a tempo con i piedi.
- [ ] Console pulita, scene rigenerate dai menu, build WebGL aggiornata su `gh-pages`.
- [ ] L'utente ha visto la nuova corsa e l'ha approvata.

## 7. Domande aperte (per l'utente)
- Preferisci una corsa più "di bambino" (passi corti, braccia larghe) anche per l'adulto, o due corse diverse?

## 8. Rapporto
*(da compilare da chi implementa)*

### Fatto
### Scostamenti dalla specifica (e perché)
### Clip scelta e valori di taratura
### Come testare
### Domande aperte per l'utente
