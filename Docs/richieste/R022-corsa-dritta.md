# R022 – Corsa corretta: personaggio dritto, gamba sinistra che si piega

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity (+ Blender MCP se serve ritoccare la clip) |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3. **Il codice attuale vince** |
| **Dipende da** | R004 (clip Mixamo "Slow Run", integrata). Indipendente dalle altre richieste |
| **Priorità** | Alta (si vede sempre) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Migliora l'animazione della corsa, è storta, la gamba sinistra sembra che rimanga tesa e il personaggio è storto.

## 2. Situazione di partenza (dal Rapporto di R004)
- `Amerigo_Run.fbx` = Mixamo **Slow Run** (In Place), caricata con lo **scheletro di Amerigo adulto** (`pelvis`, `thigh_l`… non
  `mixamorig:*`); la mappa Humanoid è stata **rigenerata automaticamente** da Unity dopo aver svuotato `human`/`skeleton` nel `.meta`.
- Stessa clip per adulto e bambino (retarget Humanoid); `clipRunSpeed` adulto 3,4, bambino 1,7; bambino a 1,29x.
- Sospetti principali (da verificare, non presumere):
  1. **Mappa Humanoid automatica sbagliata** (es. `thigh_l`/`calf_l` o twist bones assegnati male, ginocchio sinistro mappato
     sull'osso di twist) → gamba sinistra che non si piega.
  2. **T-pose/posa di riferimento** dell'avatar non corretta (Enforce T-Pose), che inclina il busto o una gamba.
  3. Impostazioni della clip: *Root Transform Rotation / Position Y* non "Bake Into Pose", *Based Upon* sbagliato,
     *Mirror*, *Foot IK* spento, limiti muscolari della gamba.
  4. Differenza di proporzioni tra avatar della clip e avatar del modello (retarget), o rotazione del modello nel prefab.
  5. Codice: `ThirdPersonController` ruota il transform con Slerp (verificare che non ci sia inclinazione residua,
     che il modello figlio non abbia rotazione locale, che l'Animator non usi root motion in modo inatteso).

## 3. Specifica
1. **Diagnosi con prove** (screenshot): avatar config (mappa ossa, posa di riferimento) per la clip e per i modelli adulto e
   bambino; la clip in anteprima sull'avatar originale e su Amerigo; vista frontale e laterale in corsa, a 4 fotogrammi del ciclo.
   Scrivere nel Rapporto **quale** era la causa.
2. **Correzione alla radice**: mappa Humanoid corretta e salvata (non affidata all'automatico), posa T corretta, impostazioni
   della clip (Bake Into Pose per rotazione e Y, Foot IK attivo, loop pulito), modello figlio senza rotazioni spurie.
3. Se la clip stessa è difettosa: valutare un'altra clip Mixamo (Run / Jog / Slow Run), scaricata dall'utente se serve;
   in alternativa ritocco in Blender delle curve della gamba sinistra.
4. **Simmetria**: le due gambe devono piegarsi allo stesso modo; busto dritto (leggerissima inclinazione in avanti naturale,
   nessuna inclinazione laterale); braccia non compenetrate col corpo.
5. Bonus (se resta tempo): inclinazione naturale in curva (lean) leggera, transizione cammina↔corri morbida (blend tree per
   velocità), piedi senza pattinamento (verifica come in R004).
6. Vale per **adulto e bambino**.

## 4. Ordine di lavoro consigliato
1. Diagnosi e screenshot. 2. Fix avatar/clip. 3. Verifica in gioco su adulto (hub) e bambino (flashback).
4. Blend tree e lean se serve. 5. GIF prima/dopo.

## 5. Criteri di accettazione
- [x] Causa trovata e scritta nel Rapporto, con prova.
- [x] Corsa simmetrica: la gamba sinistra si piega come la destra; personaggio dritto (vista frontale e laterale), adulto e bambino. (verificato con foto e curve; vedi Rapporto)
- [ ] Nessun pattinamento dei piedi; passi audio ancora a tempo.
- [ ] GIF prima/dopo in `Docs/richieste/media/R022/`; l'utente ha approvato la nuova corsa.
- [x] Console pulita; scene non rigenerate (non serve), nessun bake.

## Rapporto
Compilato da Claude, PC casa, 2026-11-07. Stato: **corsa corretta e verificata con foto; restano aperti pattinamento/passi audio, GIF, approvazione dell'utente e le scarpe dell'adulto in anteprima**.

### Causa (con prove)
- La mappa Humanoid di `Amerigo_Run.fbx` era corretta. Il difetto era la **posa di riferimento (T-pose) dell'avatar della clip**: Unity la ricava dal **primo fotogramma del file**, che in
  questo FBX e' gia' un fotogramma di corsa (ginocchio sinistro piegato di circa 96 gradi, bacino a 30 gradi). L'Enforce T-Pose automatico partiva da li' e salvava nel `.meta` una posa
  sbagliata: `pelvis` a 30,7 gradi dal modello, `calf_l`/`calf_r` a circa 30, `foot_r` ribaltato di 180 gradi sul proprio asse (300,7/173,4/186,3 contro `foot_l` 299,4/356,0/9,1).
- Prova numerica (curve muscolari importate): piede `Foot Up-Down` sx -1,00..0,15 contro dx 0,23..1,31; `RootQ.x` 0,23..0,27 (busto avanti di circa 30 gradi); ginocchia sfalsate.
  Dopo il fix: piede sx -0,88..0,25 / dx -0,85..0,24, ginocchio sx -0,28..0,77 / dx -0,23..0,84, simmetrici.
- Nell'FBX Mixamo tutto il movimento del bacino e' sul **nodo dell'armatura** (Game_engine), non sull'osso `pelvis` (che resta identita' per tutta la clip); il nodo ha anche
  una rotazione statica/animata di circa 16-19 gradi in avanti con imbardata e rollio non centrati (+-8 e -4,5/+0,6 gradi). Prova: Blender headless, `riesporta_clip_gerarchia.py`.
- Escluse: mappa ossa, rotazioni del prefab, applyRootMotion, ThirdPersonController. Il tentativo "Copy From Other Avatar" resta impossibile per gerarchia diversa
  (e non serve: vedi sotto).

### Correzione (riproducibile)
1. `ArtSource/Characters/riesporta_clip_gerarchia.py` (Blender headless, documentato in testa): importa la clip Mixamo originale
   (`ArtSource/Characters/clip_mixamo_originali/Amerigo_Run.fbx`), **cuoce il movimento del nodo nell'osso `pelvis`**, rimette il nodo a una trasformazione fissa e pulita,
   toglie la media di imbardata/rollio del bacino e 8 gradi di inclinazione media (`--pitch-fix 8 --no-yaw-roll-bias`), aggiunge come **fotogramma 0 la posa di riposo** dell'armatura
   (come nei download Mixamo "with skin") e riesporta solo armatura + animazione.
   Comando: `blender.exe -b --factory-startup --python ArtSource/Characters/riesporta_clip_gerarchia.py -- <originale.fbx> Assets/Art/Animations/Amerigo/Amerigo_Run.fbx --pitch-fix 8 --no-yaw-roll-bias`.
2. `Assets/_Game/Editor/ClipAmerigoImporter.cs`, menu **Treno > Personaggi > Applica importer clip Amerigo**: Humanoid con avatar proprio ricavato dalla T-pose del fotogramma 0 (mappa e posa
   rigenerate; pelvis 290,2/270/180 e calf_l 7,3/333,4/358,9 ora identici al modello, gambe in specchio), clip dal fotogramma 1 al 23, loop, rotazione/altezza/XZ del bacino
   in Bake Into Pose, e **ricollega la clip al blend tree** `Amerigo.controller` (l'ID interno cambia con la riesportazione; il GUID del file e' invariato).
   Valido per adulto e bambino (stessa clip, retarget Humanoid, ciascun modello col proprio avatar). Copy From Other non serve: la posa ora coincide col modello.
3. Walk e Idle: controllati con foto (`cammina_adulto_dopo.jpg`, `fermo_adulto_dopo.jpg`): simmetrici e dritti, hanno uno scheletro Mixamo diverso (nomi diversi) e non sono stati toccati.
4. `CorsaFoto.cs`: strumento foto (menu Treno > Diagnostica > Foto corsa), ora con disegno a vuoto prima dello scatto.

### Prove
- `Docs/richieste/media/R022/`: `corsa_adulto_prima/dopo.jpg`, `corsa_bambino_prima/dopo.jpg` (4 fotogrammi del ciclo, riga alta frontale, bassa laterale; nelle foto "prima" lo sfondo era scuro),
  `posa_rif_*.jpg`, `cammina_adulto_dopo.jpg`, `fermo_adulto_dopo.jpg`.
- Dopo: busto con leggera inclinazione in avanti (circa 8-10 gradi), nessuna inclinazione laterale, ginocchia ben piegate alternate in modo uguale, braccia non compenetrate. Bambino pulito.
- Console: nessun errore.

### Cosa resta aperto
- **Scarpe dell'adulto in anteprima**: nelle foto (anche con Walk e prima della correzione) le scarpe appaiono staccate dai pantaloni in alcuni fotogrammi laterali. Misure con `BakeMesh`:
  orlo dei pantaloni a 3 cm dal piede e scarpe a 8 cm dall'osso a ogni fotogramma, quindi in geometria sono attaccate: sembra un artefatto dello skinning della scena di anteprima
  (PreviewRenderUtility). **Da confermare nel gioco** (non e' stato eseguito il Play).
- Pattinamento dei piedi e passi audio a tempo non verificati (nessun Play); la clip ha la stessa durata (0,733 s) e lo stesso nome/GUID di prima, `clipRunSpeed` invariati.
- GIF prima/dopo non fatta. Bonus (lean in curva, blend tree) non fatti. Approvazione dell'utente: aperta.
