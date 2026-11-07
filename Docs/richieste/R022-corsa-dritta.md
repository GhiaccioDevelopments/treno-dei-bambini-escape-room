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
_(da compilare a fine lavoro)_
