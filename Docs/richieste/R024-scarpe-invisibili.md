# R024 – Le scarpe da raccogliere nel ricordo sono invisibili

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `main` @ d09dfc3 (+ R019 se già integrata). **Il codice attuale vince** |
| **Dipende da** | niente. **In testa alla coda** (subito dopo R019) |
| **Priorità** | **Altissima**: senza vederle non si finisce il livello 2 |

## 1. Cosa ha chiesto l'utente (parole sue)
> Le scarpe, nel ricordo del basso di Antonietta, LIVELLO 2: quelle da raccogliere non si vedono, sono invisibili, risolvi.

## 2. Situazione di partenza (da verificare, non presumere)
- Livello 2 del ricordo = `flashback_scarpe.json`, enigma 2 `enigma_nascosto` "Il baule delle scarpe" (tipo `nascosto`):
  cinque `oggetti[]` da trovare nella strada e provare sulle forme di legno della mensola:
  `scarpa_cartone` (modello `cartone`), `zoccolo_legno` (`zoccoli`), `scarpone_soldato` (`militari`),
  `scarpa_signora` (`tacchi`, y 0,9), `pezze_piedi` (`pezze`).
- Li crea `NascostoPuzzle.cs:59` con `MondoFab.Scarpa(... testo = n.modello ...)`. Dopo R009/R020 (niente più oggetti viola,
  modelli veri in `Resources/Modelli/Enigmi`) in quella cartella c'è solo `scarpa.prefab`, nessuna variante
  `cartone/zoccoli/militari/tacchi/pezze`.
- Sospetti: variante non trovata → nessuna mesh; materiale o shader che non compila (anche su WebGL); posizione sotto il
  pavimento o dentro i muri dopo R006 (mappe più grandi); oggetti spenti da `sostituisce`/`nascosta`/MondoStato; LOD o culling;
  layer escluso dalla camera.

## 3. Specifica
1. **Diagnosi con prove** (screenshot in Play della posizione di ognuna delle 5 scarpe, gerarchia e componenti):
   scrivere nel Rapporto la causa vera.
2. **Correzione alla radice**: le cinque scarpe devono essere **ben visibili e riconoscibili**, con modelli veri e diversi tra loro,
   coerenti col 1946 (zoccolo di legno, scarpone militare infangato, scarpa col cartone, scarpa da signora col tacco, pezze per i
   piedi), a terra o su un appoggio plausibile, mai dentro muri, pavimenti o oggetti, raggiungibili a piedi.
3. Devono farsi notare senza alone viola (regola di R009): luce che le sfiora, prompt discreto da vicino; la minimappa di R019
   può segnarle in modalità Guidata.
4. Verifica nelle due build, Windows e WebGL (preset Bassa), e su tutti i preset grafici.
5. **Prova automatica**: aggiungere al self-test degli enigmi un controllo che ogni oggetto raccoglibile di ogni enigma `nascosto`
   abbia renderer attivi, bounds non nulli, stia sopra il pavimento e fuori dai collider dei muri. Così il bug non torna nei
   ricordi futuri (R012–R016).

## 4. Criteri di accettazione
- [ ] Causa scritta nel Rapporto, con prova.
- [ ] Le 5 scarpe si vedono e si raccolgono in Windows e WebGL; livello 2 completabile dall'inizio alla fine.
- [ ] Controllo automatico nel self-test, PASS.
- [ ] Console pulita; screenshot in `Docs/richieste/media/R024/`.

## Rapporto
_(vuoto)_
