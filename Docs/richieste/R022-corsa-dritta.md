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
- [ ] Causa trovata e scritta nel Rapporto, con prova.
- [ ] Corsa simmetrica: la gamba sinistra si piega come la destra; personaggio dritto (vista frontale e laterale), adulto e bambino.
- [ ] Nessun pattinamento dei piedi; passi audio ancora a tempo.
- [ ] GIF prima/dopo in `Docs/richieste/media/R022/`; l'utente ha approvato la nuova corsa.
- [ ] Console pulita; scene rigenerate e (se necessario) bake finale.

## Rapporto
**Stato: diagnosi fatta, correzione NON completata** (nessun criterio spuntato). Compilato da Claude, PC casa, 2026-11-07.

### Diagnosi (con prove)
- **Mappa Humanoid di `Amerigo_Run.fbx`: corretta** (thigh_l, calf_l, foot_l, ecc. tutte al posto giusto). Sospetto 1 escluso.
- **Causa principale trovata: posa di riferimento (T-pose) dell'avatar della clip sbagliata.** Il `.meta` salvato in R004 contiene la posa che Unity ha
  ricavato da sola (Enforce T-Pose automatico) partendo dal primo fotogramma della corsa, che e' una posa di corsa (nel file il ginocchio sinistro e'
  piegato di circa 96 gradi). Risultato nel `.meta`: bacino inclinato di 30,7 gradi rispetto al modello (`pelvis` 320,8/272,9/179,2 contro 290,2/270/180),
  `calf_l` e `calf_r` a circa 30 gradi dal modello, `foot_r` a 53 gradi (rotazione 300,7/173,4/186,3: ribaltato di 180 gradi sul proprio asse rispetto a `foot_l`).
  Il file originale (nodi grezzi) ha invece il bacino identico al modello (0,0 gradi).
- **Prova numerica** (curve muscolari della clip, `Amerigo_Run.fbx` com'e' ora): piede sinistro `Foot Up-Down` -1,00..0,15 contro destro 0,23..1,31
  (offset di circa 1,2 su un range di +-1); `RootQ.x` 0,23..0,27 (busto inclinato in avanti di circa 30 gradi); `Spine Left-Right` -0,16..0,22 e
  `RootQ.y` +-0,11 (oscillazione laterale/rotazione del bacino). Le curve del ginocchio sono quasi in fase simmetrica ma sfalsate (sx 0,08..1,13, dx 0,15..1,22).
  Sulla clip Walk lo stesso scarto (piede sx -0,57..0,07, dx -0,10..0,79) ma piu' piccolo: probabilmente stesso difetto, meno visibile.
- **Prova visiva**: `Docs/richieste/media/R022/corsa_adulto_prima.jpg` e `corsa_bambino_prima.jpg` (4 fotogrammi del ciclo, riga alta frontale, riga bassa laterale):
  busto inclinato, gambe aperte in modo irregolare, e **nell'adulto le scarpe (mesh separata `shoes03Mesh`) si staccano dalle gambe** nei fotogrammi 2-4.
  Nel bambino (scalzo) niente scarpe da staccare, ma stessa inclinazione. Posa di riferimento dei modelli: `posa_rif_adulto.jpg`/`posa_rif_bambino.jpg`
  (A-pose, gambe dritte e simmetriche: il modello e' a posto).
- Escluso: rotazioni spurie nel prefab (figlio `Game_engine/Root` con rotazione standard 0/270/270 uguale per adulto e bambino, Animator con `applyRootMotion` spento);
  `ThirdPersonController` (Slerp solo sull'asse Y, nessuna inclinazione).
- Misura della geometria del file (clip importata come Generic): ginocchio fino a 105 gradi, inclinazione busto 23-27 gradi, nessuna asimmetria di fase:
  la clip Mixamo in se' e' simmetrica; il difetto e' nel retarget sull'avatar.
- `IsPoseValid` (AvatarSetupTool, via reflection) e' false per tutti: adulto 421, bambino 446, Run 1422, Walk 1393 (errore di posa); piu' alto = piu' lontano da una T-pose.

### Tentativi (scartati, tutti annullati)
1. `avatarSetup = CopyFromOther` con l'avatar di Amerigo adulto: **non possibile**, Unity: "Copied Avatar Rig Configuration mis-match. Transform hierarchy does not match"
   (il modello ha il nodo `Game_engine` sopra `Root`, la clip no).
2. Copia nel `.meta` della clip delle rotazioni locali dell'avatar adulto (posa di riferimento = quella del modello): gambe simmetriche, busto dritto, `RootQ.x` 0,04..0,09,
   foot Up-Down sx -0,89..0,29 / dx -0,83..0,28, ma la corsa diventa una **camminata** (ginocchia poco piegate, falcata corta) e le scarpe dell'adulto restano staccate.
   Scartato: non e' una corsa corretta. Probabile causa: gli assi locali della clip non coincidono col modello (il ginocchio finisce in estensione negativa, `Lower Leg Stretch` -0,30..0,78).
3. `MakePoseValid` di Unity via reflection: restituisce la stessa posa gia' salvata in R004 (e' proprio quella generata in automatico), quindi non cambia nulla.

### Cosa e' stato modificato
- Nessuna modifica a importer, controller, prefab, scene o gameplay (`Amerigo_Run.fbx.meta` e' identico a prima).
- Aggiunto `Assets/_Game/Editor/CorsaFoto.cs` (menu **Treno > Diagnostica > Foto corsa (adulto e bambino)**): foto frontale/laterale a 4 fotogrammi con l'AnimatorController del gioco
  (suffisso del file in EditorPrefs `CorsaFoto.Suffisso`, default `dopo`), `ScattaPosa` (posa di riferimento) e `Misura` (ginocchia e busto; **attenzione**: `Misura` legge le trasformazioni
  subito dopo `Animator.Update` e ha dato valori non affidabili, usare le foto).
- Media in `Docs/richieste/media/R022/` (JPG, fuori LFS): `corsa_adulto_prima`, `corsa_bambino_prima`, `posa_rif_adulto`, `posa_rif_bambino`.

### Cosa resta (prossimi passi consigliati)
1. Scegliere la strada per la posa di riferimento della clip: (a) far scaricare a Mixamo la clip **dal modello T-pose di Amerigo** (FBX con scheletro e pelle, poi estrarre la clip con "Copy from other avatar"
   dall'avatar del modello: stesso scheletro, nessun `Game_engine` di troppo) oppure (b) ricostruire la T-pose a mano dell'avatar della clip con lo strumento Configure Avatar (ginocchia dritte, piedi piatti, bacino dritto) e salvarla nel `.meta`.
2. Dopo il fix: rifare le foto `corsa_*_dopo.jpg` con lo stesso strumento, verificare scarpe attaccate (adulto), gamba sinistra uguale alla destra, busto dritto (inclinazione in avanti 10-15 gradi), poi GIF prima/dopo.
3. Estendere lo stesso controllo a Walk/Idle (stesso difetto in scala minore).
4. Criteri di accettazione: tutti ancora aperti.
