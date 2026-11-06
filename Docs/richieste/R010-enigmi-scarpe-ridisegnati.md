# R010 – Ridisegno degli enigmi di "Le scarpe" con il nuovo sistema

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ fdeaedc. **Il codice attuale vince** |
| **Dipende da** | R009 (sistema), R006 (mappe) |
| **Priorità** | Media-alta |

## 1. Cosa ha chiesto l'utente (parole sue)
> Gli enigmi strutturali in modo molto diverso […] Prendi spunto dai più grandi giochi ad indovinelli.

## 2. Situazione di partenza
`flashback_scarpe.json`: 4 enigmi isolati – quiz "La cassetta dei diritti", nascosto "Il baule delle scarpe",
ordina "Il calendario della memoria", lucchetto "Il lucchetto dell'uscita".

## 3. Vincoli
- **`Docs/TRAMA.md` è vincolante**: Amerigo bambino scalzo che conosce le scarpe di tutti e ne desidera un paio suo
  (Decisione 12); il gioco di osservare le scarpe dei passanti con un meccanismo nostro (Decisione 8); nessun anacronismo
  (la Costituzione è del 1948: il collegamento agli articoli lo fa l'interfaccia didattica, non i personaggi).
- Restano i **temi civici** e gli obiettivi didattici (secondaria di 2° grado); testi originali.
- Non sviluppare altri oggetti/flashback (Stracci, Volantino…): solo "Le scarpe".
- ID delle battute di cutscene invariati (doppiaggio).

## 4. Specifica (proposta da far approvare all'utente prima di implementarla)
Un'unica **catena** di 4–5 enigmi nel ricordo, dove ognuno produce qualcosa che serve al successivo. Struttura d'esempio:
1. **Le scarpe dei passanti** (deduzione alla Obra Dinn): passanti fermi nel ricordo; dalle scarpe (consumate, lucide,
   militari, zoccoli) Amerigo deduce mestiere e condizione di ciascuno. Si ottiene: chi è il calzolaio.
2. **La bottega del calzolaio** (alla The Room): banco con cassetti, forme di legno, un registro; un meccanismo fisico apre
   il cassetto col doppio fondo. Si ottiene: un foglio o una data.
3. **Il calendario della memoria** (ordina, ma nel mondo): disporre foglietti/eventi sul banco o sul muro nell'ordine giusto,
   trascinandoli (assorbe la riga del BACKLOG sul drag & drop dell'enigma "ordina").
4. **La cassetta dei diritti** (il quiz trasformato): la risposta si ricava dai documenti trovati nel ricordo, non da un
   elenco di opzioni; feedback e spiegazione didattica come ora.
5. **Il lucchetto dell'uscita** (lucchetto 3D di R009): il codice si ricompone dagli indizi raccolti prima.
- Indizi a 3 livelli per ogni passo; spiegazione civica a ogni soluzione; con "Enigmi semplificati" si usano i pannelli 2D.
- Prima di implementare: mostrare all'utente lo schema della catena (testi brevi + bozza della mappa) e chiedere l'ok con il
  pannello domande. Poi aggiornare `flashback_scarpe.json` e, se serve, `oggetti.json` / `luoghi.json`.

## 5. Criteri di accettazione
- [ ] Catena approvata dall'utente prima dello sviluppo.
- [ ] Flashback giocabile dall'inizio alla fine solo con oggetti del mondo (niente stazioni o marcatori), mouse/tastiera/touch.
- [ ] Coerenza con TRAMA.md; testi originali; indizi e spiegazioni presenti; salvataggio dei progressi funzionante.
- [ ] Video o GIF del flashback completo in `Docs/richieste/media/R010/`; console pulita.

## Rapporto
_(da compilare a fine lavoro)_
