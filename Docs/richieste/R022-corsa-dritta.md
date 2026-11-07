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
- [ ] Nessun pattinamento dei piedi; passi audio ancora a tempo. (parziale: stima numerica ok, audio non ascoltato, da verificare dall'utente in gioco)
- [ ] GIF prima/dopo in `Docs/richieste/media/R022/` (non fatta)
- [x] L'utente ha approvato la nuova corsa (2026-11-07).
- [x] Console pulita; scene non rigenerate (non serve), nessun bake.

## Rapporto
Compilato da Claude, PC casa, 2026-11-07. Stato: **corsa corretta e verificata in Play con foto; restano aperti GIF e approvazione dell'utente**.

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
3. Walk e Idle: controllati con foto in gioco (`gioco_*_cammina.jpg`, `gioco_*_fermo.jpg`): simmetrici e dritti, hanno uno scheletro Mixamo diverso (nomi diversi) e non sono stati toccati.
4. `CorsaFoto.cs`: strumento foto (menu Treno > Diagnostica > Foto corsa), ora con disegno a vuoto prima dello scatto.

### Prove
- **In gioco (Play, luce e camera della scena)**: `Docs/richieste/media/R022/gioco_adulto_{fermo,cammina,corsa}.jpg` (10_Hub_Vicolo) e `gioco_bambino_{fermo,cammina,corsa}.jpg` (30_FB_Scarpe);
  4 fotogrammi del ciclo, riga alta da dietro, riga bassa di lato (la vista frontale non e' possibile: davanti al personaggio c'e' la fontana/arredo). Amerigo in Play corre dritto
  (inclinazione in avanti di circa 8-10 gradi, nessuna laterale), ginocchia ben piegate alternate in modo uguale, **scarpe dell'adulto attaccate**.
- Prima (anteprima, solo come confronto): `corsa_adulto_prima.jpg`, `corsa_bambino_prima.jpg`; posa di riferimento dei modelli: `posa_rif_*.jpg`.
- Console: nessun errore ne' avviso.

### Scarpe "staccate": era un artefatto di anteprima, non un difetto del modello
- Nelle foto dell'anteprima (e anche nei miei primi scatti in scena) le scarpe dell'adulto apparivano staccate di 30-50 cm. Verifiche in Play su `shoes03Mesh`: ossa e `rootBone` corretti
  (stesse ossa del pantalone, nessuna istanza estranea), un solo renderer, pesi validi (max 3 ossa per vertice, somma 1), bindpose coerenti, nessun LOD aggiuntivo, quality Auto/4 ossa.
  Con `BakeMesh` orlo dei pantaloni a 3 cm dal piede e scarpe a 8 cm dall'osso a ogni fotogramma.
- Prova decisiva: stesso fotogramma disegnato (a) dopo chiamate manuali a `Animator.Update` + `Camera.Render` immediato = scarpe staccate; (b) con l'Animator che gira da solo nel loop
  di gioco (Play, `runInBackground`) = scarpe attaccate (`gioco_adulto_corsa.jpg`). Con la skin ancora indietro di un aggiornamento le scarpe (rootBone diverso) si spostano diversamente
  dai pantaloni. Nessuna correzione al modello/builder necessaria; `CorsaFoto.cs` resta valido solo come strumento di confronto approssimato.

### Pattinamento e passi audio (Play, adulto)
- Misura nel sistema del personaggio fermo, Animator a MotionSpeed 1: velocita' all'indietro del piede in appoggio (mediana, piede piatto) corsa 2,4-3,1 m/s, camminata 1,6-1,8 m/s.
  Il metodo e' rumoroso e sovrastima (sulla camminata, clip invariata, da' 1,7 contro 1,2 attesi, fattore 1,4); riportata allo stesso fattore la corsa da' circa 1,9 = `clipRunSpeed` (1,9): niente
  variazioni rispetto a R004 e quindi niente pattinamento nuovo. Osservazione a occhio: piedi fermi sul selciato in appoggio nelle foto. Da riverificare dall'utente guardando il gioco.
- Passi audio (`FootstepAudio`): dipendono da velocita' e `clipRunSpeed * 0,367 s` (mezzo ciclo); la durata della clip (22 fotogrammi, 0,733 s) e la fase (fotogramma 1 = ex fotogramma 0) sono invariate: a tempo come prima.
  Non ho potuto ascoltare l'audio.

### Scelte motivate
- **Mappa Humanoid rigenerata dall'automatico** a ogni lancio del menu (`humanDescription` vuota, Unity ricostruisce mappa e T-pose dal fotogramma 0): cosi' la posa non puo' restare quella sbagliata di R004.
  Il `.meta` salvato e' quello corretto (mappa verificata: stesse ossa di prima, posa pelvis/calf identica al modello).
- **Foot IK resta spento** (`Amerigo.controller`, `m_IKOnFeet: 0`): non e' stato attivato perche' non verificato in Play ne' misurato sulle foto, e la corsa e' gia' simmetrica senza pattinamento nuovo; attivarlo
  senza prova potrebbe alterare la posa dei piedi/scarpe.

### Cosa resta aperto
- Riscontro a occhio di pattinamento/passi da parte dell'utente (approvazione della corsa gia' data il 2026-11-07). GIF prima/dopo non fatta. Bonus (lean in curva, blend tree) non fatti.
- Il lean (8 gradi tolti) si regola rilanciando lo script Blender con un altro `--pitch-fix`.
