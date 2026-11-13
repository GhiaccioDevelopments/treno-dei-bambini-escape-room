# R010 – Ridisegno degli enigmi di "Le scarpe" con il nuovo sistema

> **Nota (2026-11-07): R010 è assorbita da R020.** Lo schema approvato di R020 include il ridisegno degli enigmi di "Le scarpe" e sostituisce la specifica qui sotto. Schema: `richiesta/R020-enigmi-d-autore-trama`, file `Docs/richieste/schemi/R020_schema.md`.
>
> La correzione sul quiz (§4) vale anche qui: le domande sulla Costituzione del 1948 e sulla Convenzione ONU del 1989 non possono avere risposta in documenti che stanno dentro il ricordo del 1946. Diventano spiegazioni o riflessioni nel presente, dette da Amerigo adulto o scritte nel quaderno.


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
- [x] Catena approvata dall'utente prima dello sviluppo (schema R020, 2026-11-07).
- [x] Flashback giocabile dall'inizio alla fine solo con oggetti del mondo (provato in automatico, mouse/tastiera; touch non provato).
- [x] Coerenza con TRAMA.md; testi originali; indizi e spiegazioni presenti; salvataggio dei progressi funzionante.
- [ ] Video o GIF: sostituiti da schermate in `Docs/richieste/media/R020/`; console pulita.

## Rapporto
Assorbita da R020 e implementata sul branch `richiesta/R020-enigmi-d-autore-trama`; vedi il rapporto in `Docs/richieste/R020-enigmi-d-autore-trama.md`.

**Fatto.** Catena di cinque enigmi nel ricordo, tutti oggetti del mondo e senza stazioni: `enigma_passanti` (deduzione dalle scarpe, nuovo), `enigma_nascosto` (calzature cercate nella via e provate sulle forme del calzolaio, inventario attivo), `enigma_quiz` (tre documenti del 1946 da leggere: le risposte sono righe dei fogli; la Costituzione e la Convenzione ONU stanno solo nelle schede civiche), `enigma_ordina` (ritagli da trascinare), `enigma_lucchetto` (rotelle 3D, 1989 dalle quattro cifre raccolte). ID delle battute e degli enigmi invariati.
**Non fatto.** Le sei figure MakeHuman dei passanti (D2): servono a mano in MakeHuman; al loro posto gradino e scarpe nella vista ravvicinata. Nessuna GIF.

