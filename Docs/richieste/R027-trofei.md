# R027 – Trofei (achievement)

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `main` aggiornato. **Il codice attuale vince** |
| **Dipende da** | **ricordi finiti** (R012–R016), easter egg (R011), poi R026 |
| **Priorità** | Media (in coda) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Metti una sezione ACHIEVEMENT (trofei), dove si possono guadagnare giocando, facendo missioni secondarie ecc.
> (una volta finiti i ricordi, valutando quali mettere).

## 2. Specifica
1. **Sezione Trofei** dal menu principale e dalla Pausa, in stile d'epoca: bacheca o album di medaglie, santini o figurine
   (scegliere e motivare, coerente con R023). Trofei bloccati visibili come sagome con un indizio; quelli segreti nascosti fino allo sblocco.
2. **Notifica** discreta allo sblocco (HUD minimale) con un suono breve.
3. **Elenco dei trofei** (20–35) da proporre all'utente **prima** di implementare, con domanda a pannello. Famiglie suggerite:
   - Storia: uno per ogni ricordo completato e per il finale.
   - Enigmi: un enigma senza indizi, un capitolo intero senza errori, tutte le schede civiche lette.
   - Esplorazione: tutti i vicoli visitati (R019), tutti gli easter egg (R011), tutti i manifesti e le insegne letti.
   - Missioni secondarie: se non esistono, proporne 3–6 brevi e coerenti con `TRAMA.md` (es. aiutare un vicino, ritrovare un
     oggetto perso), con testi originali.
   - Civici: legati ai temi (Costituzione, diritti, solidarietà), mai punitivi.
4. **Dati**: `Resources/Data/trofei.json` (`id, titolo, descrizione, indizio, segreto, icona, condizione`), condizioni calcolate dai
   gettoni di MondoStato e dal salvataggio; stato in `SaveData` (PlayerPrefs `treno_save`), compatibile coi salvataggi vecchi.
   Documentare lo schema in `CLAUDE.md`.
5. Prova automatica: ogni trofeo è ottenibile (nessuna condizione impossibile) e lo sblocco sopravvive a salva/carica.

## 3. Criteri di accettazione
- [ ] Elenco dei trofei approvato dall'utente.
- [ ] Sezione Trofei da menu e pausa, notifica, salvataggio; Windows e WebGL.
- [ ] Self-test PASS, console pulita, screenshot in `Docs/richieste/media/R027/`.

## Rapporto
_(vuoto)_
