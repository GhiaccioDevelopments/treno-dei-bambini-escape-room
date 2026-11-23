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

**Stato: parte di codice e testi completa, da provare in Unity (2026-11-13).** Scritto al PC della scuola senza Unity. Base: `richiesta/R026-enigmi-chiari`
con dentro R016 (e quindi R012-R015).

### Fatto
- **Biglietto d'apertura** (`Scripts/Puzzles/BigliettoEnigma.cs`): foglietto di carta d'epoca con "Cosa fare" e "Cosa ottieni"; compare all'apertura della vista
  ravvicinata (`EsaminaView`) e dei pannelli 2D (`PuzzlePanel`, versione "Enigmi semplificati"); si chiude con un clic, un tasto, il gamepad o da solo dopo 12 s;
  si riapre con il pulsante **?** accanto a Consegna (vista 3D) o a Chiudi (pannelli).
- Campi JSON nuovi dell'Enigma `cosaFare`, `cosaOttieni`, compilati per **tutti i 48 enigmi** (ricordi, hub, basso, segreti) da
  `ArtSource/Enigmi/aggiungi_biglietti.py` (va rilanciato dopo ogni generatore che riscrive i JSON; fallisce se un enigma non ha il biglietto). Mai la soluzione nel biglietto.
- Etichette in `enigmi_mondo.json` (`bigliettoFare`, `bigliettoOttieni`, `bigliettoChiudi`, `btnSpiega`), riga in Impostazioni > Gioco **"Mostra la spiegazione
  all'apertura"** (sì di default; `SaveData.nascondiSpiegazione`), testi in `ui_menu.json` (`enigmiImp.spiegazione*`).
- Prove: `EnigmiSelfTest.ControllaBiglietti` (tutti gli enigmi hanno le due righe); `ViolinoTest` controlla che il biglietto compaia e si chiuda.

### Non fatto (serve Unity)
- **Punto 3, grafica**: passata sugli enigmi più deboli con screenshot prima/dopo. Senza editor non si vedono i risultati: va fatta a casa, guardando gli enigmi
  dei ricordi nuovi (R012-R016, tutti di primitive) e quelli di hub e basso.
- **Punto 4, giro "da studente"**: va giocato. Gli indizi dei capitoli R013-R016 sono già a tre livelli (spinta, direzione, quasi soluzione).

### Decisioni prese da Claude
- Il biglietto si chiude anche da solo (12 s) perché non blocchi la LIM; il primo 0,4 s ignora i tasti (il clic che apre l'enigma non lo richiude).
- Pulsante "?" solo se l'enigma ha almeno una delle due righe.

### Da fare a casa con Unity
1. Console senza errori (nuovo `BigliettoEnigma.cs`; toccati `EsaminaView.cs`, `PuzzlePanel.cs`, `SettingsPanel.cs`, `SaveSystem.cs`, `DataClasses.cs`, `EnigmiSelfTest.cs`, `ViolinoTest.cs`).
2. Aprire un enigma 3D e uno 2D: posizione e leggibilità del biglietto, del pulsante ? (non deve coprire i pulsanti dei meccanismi a destra) e alla LIM.
3. `-enigmitest`, `-violinotest`; poi punti 3 e 4 con screenshot in `Docs/richieste/media/R026/`.

### Criteri di accettazione
- [x] Biglietto all'apertura di ogni enigma, testi solo in JSON (scritto, da provare).
- [ ] Screenshot prima/dopo degli enigmi ritoccati in `Docs/richieste/media/R026/`.
- [ ] Self-test PASS, console pulita, Windows e WebGL.
