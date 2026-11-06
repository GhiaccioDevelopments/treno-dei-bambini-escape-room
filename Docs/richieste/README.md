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
| R003 | Personaggi in Blender: la gente di Napoli (NPC) | aperta (in attesa del PC di casa) |
| R005 | Inventario e pausa da tastiera, cursore bloccato | integrata in `feature/upgrade-realistico` (6/11), in prova |
| R004 | Animazione di corsa nuova (da Mixamo) | integrata in `feature/upgrade-realistico` (6/11), in prova |
