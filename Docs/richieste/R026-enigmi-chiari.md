# R026 – Enigmi più belli e più chiari: cosa fare e cosa si ottiene

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `main` aggiornato. **Il codice attuale vince** |
| **Dipende da** | **tutti gli enigmi finiti**: R011 e i ricordi R012–R016. Va fatta dopo R016 |
| **Priorità** | Media (in coda) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Migliora gli enigmi, a livello di grafica e di capibilità: aggiungi una piccola descrizione di ciò che si deve fare e cosa si
> ottiene appena si apre un enigma (sempre una volta finiti tutti gli enigmi).

## 2. Specifica
1. **Biglietto d'apertura**: quando si apre un enigma (vista ravvicinata `EsaminaView` o pannello 2D) compare per prima cosa
   un piccolo biglietto d'epoca (carta e tipografia di R001/R023) con due righe:
   - **Cosa fare** (una frase all'imperativo, es. "Prova ogni scarpa sulla forma di legno giusta").
   - **Cosa ottieni** (una frase, es. "La chiavetta del baule e una nuova riga del quaderno").
   Si chiude con un clic o da solo al primo gesto; si riapre con un pulsante "?" sempre visibile accanto a Indizio.
2. Nuovi campi JSON opzionali dell'Enigma: `cosaFare`, `cosaOttieni` (da documentare nello schema di `CLAUDE.md`), compilati per
   **tutti** gli enigmi di hub, basso e di ogni ricordo. Opzione in Impostazioni > Gioco: "Mostra la spiegazione all'apertura" (sì di default).
3. **Grafica**: passata su tutti gli enigmi per portarli al livello dei migliori di R020 (modelli veri, materiali PBR, luce sul
   piano di lavoro, evidenza discreta delle parti cliccabili al passaggio del mouse o del cursore del gamepad, feedback animato
   e sonoro di riuscita). Elenco degli enigmi più deboli con screenshot prima/dopo.
4. **Capibilità**: giro di prova "da studente" di ogni enigma; dove non è chiaro cosa cliccare o in che ordine, correggere prompt,
   guide e indizi (indizi a 3 livelli: spinta, direzione, quasi soluzione). Didattica non punitiva invariata.
5. Prova automatica: ogni enigma ha `cosaFare` e `cosaOttieni` non vuoti; il biglietto compare e si chiude.

## 3. Criteri di accettazione
- [ ] Biglietto all'apertura di ogni enigma, testi solo in JSON.
- [ ] Screenshot prima/dopo degli enigmi ritoccati in `Docs/richieste/media/R026/`.
- [ ] Self-test PASS, console pulita, Windows e WebGL.

## Rapporto
_(vuoto)_
