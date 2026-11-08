# Richieste tra sessioni (protocollo Claude ↔ Claude)

Questo progetto viene sviluppato da più PC. Spesso una sessione di Claude (es. **PC scuola**, senza Unity)
raccoglie le idee dell'utente e le scrive come **richieste**; un'altra sessione (es. **PC casa**, con Unity e
MCP for Unity) le implementa. Le due sessioni non si parlano: comunicano **solo tramite questi file e le PR**.

## Come nasce una richiesta (chi scrive)
1. Branch `richiesta/RNNN-titolo-breve` creato da `main`.
2. File `Docs/richieste/RNNN-titolo-breve.md` copiando la struttura di una richiesta esistente:
   obiettivo, vincoli, specifica dettagliata, ordine di lavoro, criteri di accettazione, sezione **Rapporto** vuota.
3. Pull request **in bozza** (draft) verso `main` con titolo `[RNNN] ...` e la stessa sintesi.
4. Voce nel `Docs/DIARIO.md` che elenca la richiesta aperta.

## Come si implementa (chi esegue)
1. **Prima finire e pubblicare il lavoro in corso.** Le richieste non interrompono una fase aperta.
2. `git fetch`, `git checkout richiesta/RNNN-...`, `git rebase origin/main` (la richiesta può essere
   stata scritta su un `main` più vecchio: il codice attuale vince sempre sulla specifica, vedi sotto).
3. Leggere tutta la richiesta. Lavorare **sullo stesso branch**, a piccoli commit (`RNNN: ...`).
4. Se qualcosa della specifica non si adatta al codice attuale: scegliere la soluzione più vicina allo
   spirito della richiesta e **scriverlo nel Rapporto** (sezione "Scostamenti").
5. Se serve una decisione dell'utente: chiederla in chat; se l'utente non c'è, scriverla in
   "Domande aperte" del Rapporto e andare avanti con le parti non bloccate.
6. A fine lavoro: compilare il **Rapporto** (fatto / scostamenti / come testare / screenshot o GIF se
   possibile in `Docs/richieste/media/RNNN/`), spuntare i criteri di accettazione, togliere la bozza alla PR
   (`gh pr ready`) e chiedere all'utente di provarla. **Merge su `main` solo dopo l'ok dell'utente.**
7. Voce nel diario.

## Regole di priorità
- Il **codice su `main`** e le regole di `CLAUDE.md` vincono sulla specifica di una richiesta.
- La specifica dice **cosa** ottenere e **perché**; i nomi di classi e file proposti sono suggerimenti.
- Una richiesta non autorizza a sviluppare nuovi oggetti/flashback: quello resta su ok dell'utente.

## Elenco
| ID | Titolo | Stato |
|---|---|---|
| R001 | UI spettacolare in stile antico | implementata in `feature/upgrade-realistico` (6/11) |
| R002 | Effetti sonori e musica d'epoca (tema del violino) | implementata in `feature/upgrade-realistico` (6/11) |
| R003 | Personaggi in Blender: la gente di Napoli (NPC) | chiusa su richiesta dell'utente (6/11), PR #3 |
| R005 | Inventario e pausa da tastiera, cursore bloccato | integrata in `feature/upgrade-realistico` (6/11), in prova |
| R004 | Animazione di corsa nuova (da Mixamo) | integrata in `feature/upgrade-realistico` (6/11), in prova |
| R006 | Mappe molto più grandi e pulizia dei modelli compenetrati | aperta, PR #8 (prima del gruppo) |
| R007 | Grafica realistica e cinematografica, con la pioggia | integrata in `feature/upgrade-realistico` (8/11, con R021), PR #9 |
| R008 | Impostazioni grafiche dettagliate (ombre, luci, effetti) | aperta, PR #10 (dopo R007) |
| R009 | Enigmi dentro il mondo, niente più oggetti viola | aperta, PR #11 (dopo R006) |
| R010 | Ridisegno degli enigmi di "Le scarpe" | aperta, PR #12 (dopo R009) |
| R011 | Easter egg ed enigmi nascosti in ogni stanza | aperta, PR #13 (dopo R006 e R009) |
| R012 | Nuovo ricordo "Stracci" (regole comuni dei capitoli) | aperta, PR #14 (dopo R006–R009) |
| R013 | Nuovo ricordo "Il volantino del treno" | aperta, PR #15 (dopo R012) |
| R014 | Nuovo ricordo "Il cappotto" | aperta, PR #16 (dopo R013) |
| R015 | Nuovo ricordo "Il violino" | aperta, PR #17 (dopo R014) |
| R016 | Il finale nel 1994 | aperta, PR #18 (dopo R015) |
| R017 | Mappe pulite: niente sfarfallii né buchi sul vuoto | integrata in `feature/upgrade-realistico` (7/11), PR #19 |
| R018 | Telecamera robusta: mai dentro gli edifici né sul vuoto | integrata in `feature/upgrade-realistico` (8/11), PR #20 |
| R019 | Minimappa d'epoca con prossimo obiettivo e nomi dei vicoli | aperta, PR #21 (dopo R006) |
| R020 | Enigmi d'autore in ogni stanza (quadri, codici, trama) | aperta, PR #22 (contenuto di R009, assorbe R010) |
| R021 | Grafica realistica con identità unica (pellicola neorealista) | fusa in R007 e integrata (8/11), PR #23 chiusa |
| R022 | Corsa corretta: personaggio dritto, gamba sinistra | integrata in `feature/upgrade-realistico` (7/11), PR #24 |
| R023 | Menu iniziale e inventario rifatti, al livello dei giochi più belli | aperta, PR #25 (dopo R008) |
