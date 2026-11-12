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
| R008 | Impostazioni grafiche dettagliate (ombre, luci, effetti) | integrata in feature/upgrade-realistico (2026-11-08), PR #10 |
| R009 | Enigmi dentro il mondo, niente più oggetti viola | integrata (2026-11-09) con R020, PR #11 |
| R010 | Ridisegno degli enigmi di "Le scarpe" | integrata (2026-11-09) con R020, PR #12 |
| R011 | Easter egg ed enigmi nascosti in ogni stanza | integrata (2026-11-09): 3 enigmi segreti, 14 collezionabili, pagina Segreti, aiuto lieve automatico, Rivivi il ricordo, 351 PASS in build; D1 e violino in attesa di R016, PR #13 |
| R012 | Nuovo ricordo "Stracci" (regole comuni dei capitoli) | codice completo, da provare in Unity (2026-11-12), PR #14 |
| R013 | Nuovo ricordo "Il volantino del treno" | codice completo, da provare in Unity (2026-11-12), PR #15 |
| R014 | Nuovo ricordo "Il cappotto" | codice completo, da provare in Unity (2026-11-12), PR #16 |
| R015 | Nuovo ricordo "Il violino" | aperta, PR #17 (dopo R014) |
| R016 | Il finale nel 1994 | aperta, PR #18 (dopo R015) |
| R017 | Mappe pulite: niente sfarfallii né buchi sul vuoto | integrata in `feature/upgrade-realistico` (7/11), PR #19 |
| R018 | Telecamera robusta: mai dentro gli edifici né sul vuoto | integrata in `feature/upgrade-realistico` (8/11), PR #20 |
| R019 | Minimappa d'epoca con prossimo obiettivo e nomi dei vicoli | integrata (2026-11-09): minimappa, mappa grande (M), obiettivi, 212 PASS in build, PR #21 |
| R020 | Enigmi d'autore in ogni stanza (quadri, codici, trama) | integrata (2026-11-09): enigmi nel mondo, modelli veri, 189 PASS in build, PR #22 |
| R021 | Grafica realistica con identità unica (pellicola neorealista) | fusa in R007 e integrata (8/11), PR #23 chiusa |
| R022 | Corsa corretta: personaggio dritto, gamba sinistra | integrata in `feature/upgrade-realistico` (7/11), PR #24 |
| R023 | Menu iniziale e inventario rifatti, al livello dei giochi più belli | integrata (2026-11-08): menu col vicolo vivo, latta + quaderno, PR #25 |
| R024 | Le scarpe da raccogliere nel ricordo (livello 2) sono invisibili | integrata (2026-11-09): 5 calzature vere, self-test raccoglibili, 241 PASS in build, online su gh-pages de33510, PR #26 |
| R025 | Filmato iniziale della nuova partita, doppiato dall'utente | integrata (2026-11-09): filmato a 7 capitoli, 36 battute cs_intro, 4:38, 65 PASS in build, PR #27 (voci da registrare con R028) |
| R026 | Enigmi più belli e più chiari: cosa fare e cosa si ottiene | aperta, PR #28 (dopo R011 e R012–R016) |
| R027 | Trofei (achievement) e missioni secondarie | aperta, PR #29 (dopo R026) |
| R028 | Pulsante altoparlante sotto i dialoghi del Narratore, lista dei file da doppiare | aperta, PR #30 (penultima) |
| R029 | Ottimizzazione totale del gioco | aperta, PR #31 (**ultima**) |

**Coda di lavoro (2026-11-09):** R019 → R024 → R025 → R011 → R012 → R013 → R014 → R015 → R016 → R026 → R027 → R028 → R029.
