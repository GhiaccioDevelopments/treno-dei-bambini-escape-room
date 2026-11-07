# R017 – Mappe pulite: niente sfarfallii tra superfici sovrapposte, niente buchi sul vuoto

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3 (dopo il merge di R006). **Il codice attuale vince** |
| **Dipende da** | R006 (mappe grandi, già unita). Da fare **prima** di R018 (camera) e prima delle tappe 2-3 di R007 |
| **Priorità** | Alta (difetto visibile in tutte le scene) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Migliora molto la mappa, ci sono punti dove sono unite e sfarfallano le mappe oppure punti vuoti dove si vede il vuoto sotto la mappa.

Due difetti distinti:
- **Sfarfallio (z-fighting)**: due superfici complanari o quasi (pavimento su pavimento, muro su muro, decal/manifesto
  appoggiato a filo, pezzi di mappa giuntati che si sovrappongono) lottano per la profondità e "tremolano" muovendo la camera.
- **Buchi sul vuoto**: fessure tra pezzi di pavimento/muro, bordi della mappa aperti, pezzi che non arrivano a terra,
  facce mancanti o normali invertite (backface culling) da cui si vede lo sfondo/skybox sotto o dietro la mappa.

## 2. Situazione di partenza
- Le scene sono generate dagli script editor (`Editor/SceneBuilder*.cs`, `SceneBuilderAmbienti`) con R006: hub
  "Quartieri" molto più grande, basso di Antonietta a più stanze, strada del flashback "Le scarpe" allargata.
- R006 ha già fatto un controllo delle **compenetrazioni** tra oggetti; questa richiesta riguarda le **superfici della mappa**
  (pavimenti, selciato, muri, facciate, soffitti, giunzioni tra blocchi), non i props.

## 3. Specifica
1. **Strumento di diagnosi riproducibile** (menu `Treno ▸ Verifica ▸ Superfici e buchi`), che per ogni scena:
   - trova coppie di renderer con facce complanari sovrapposte (stessa normale, distanza < 2 mm, aree che si intersecano)
     e le elenca con posizione e nomi;
   - cerca **buchi**: griglia di raggi verticali dall'alto su tutta l'area calpestabile e visibile (passo 0,25 m) e raggi
     orizzontali dalla posizione della camera verso i muri; ogni raggio che non colpisce nulla dove ci si aspetta terreno/muro
     è un buco. Elenco + screenshot di posizione;
   - controlla mesh con facce a normale invertita visibili dalla zona giocabile.
   Il rapporto (testo) va in `Docs/richieste/media/R017/`.
2. **Correzioni nei generatori, non a mano**: ogni fix entra nello script che costruisce la scena (offset, misure, fusione
   dei pezzi), così la scena resta riproducibile con `Treno ▸ Costruisci tutte le scene`.
   - Pavimenti/selciato: un solo piano continuo per zona (o pezzi che si toccano bordo a bordo, senza sovrapposizione);
     marciapiedi e gradini con altezza vera, non piani sovrapposti a 1 mm.
   - Decal, manifesti, insegne, macchie: offset di almeno 5-10 mm dalla parete **oppure** URP Decal Projector / shader con
     depth offset; mai complanari.
   - Giunzioni tra blocchi di edifici: sovrapposizione nascosta *dentro* il muro (non sulla faccia visibile) o snap esatto.
   - Bordi del mondo: nessun bordo aperto visibile. Chiusura con facciate, fondali (backdrop) dipinti/low-poly coerenti
     con Napoli 1946, terreno che prosegue oltre i limiti percorribili e collisori invisibili ai margini.
   - Sotto la mappa: un piano "di sicurezza" scuro sotto ogni scena (non visibile in condizioni normali), così un buco
     residuo non mostra mai il cielo; e un **kill-plane** che riporta il giocatore allo SpawnPoint se cade.
3. **Camera far/near**: verificare `near clip plane` (non troppo basso, es. ≥ 0,1) e `far`, perché un rapporto near/far
   esagerato peggiora lo z-fighting; coordinare con R018.
4. Ripetere la diagnosi dopo le correzioni: **zero** sovrapposizioni complanari visibili e **zero** buchi nell'area giocabile.

## 4. Ordine di lavoro consigliato
1. Strumento di diagnosi + primo rapporto (prima/dopo). 2. Fix nei generatori, scena per scena (hub, basso, flashback).
3. Bordi del mondo e piano di sicurezza/kill-plane. 4. Ricostruzione scene, **un solo bake finale**, screenshot.

## 5. Criteri di accettazione
- [ ] Menu di diagnosi presente e documentato; rapporto prima/dopo in `Docs/richieste/media/R017/`.
- [ ] Nessuno sfarfallio visibile girando la camera in hub, basso e flashback (GIF o 3-4 screenshot nei punti segnalati).
- [ ] Nessun punto da cui si veda il vuoto/cielo sotto o dietro la mappa, anche camminando lungo tutti i bordi.
- [ ] Kill-plane funzionante (cadere = ritorno allo spawn, con dissolvenza).
- [ ] Scene rigenerate dai generatori, console pulita, FPS invariati o migliori (preset Alta e Bassa).

## Rapporto
_(da compilare a fine lavoro)_
