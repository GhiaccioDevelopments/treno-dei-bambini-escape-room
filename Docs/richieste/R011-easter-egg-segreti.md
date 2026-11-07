# R011 – Easter egg ed enigmi nascosti in ogni stanza

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 605f952. **Il codice attuale vince** |
| **Dipende da** | R006 (mappe grandi), R009 (sistema "Esamina" e inventario attivo) |
| **Priorità** | Media. Da ripetere in ogni nuova stanza di R012–R016 |

## 1. Cosa ha chiesto l'utente (parole sue)
> Easter egg e enigmi nascosti in ogni stanza per esplorare di più, stanze e mappa molto più grandi.

Obiettivo: premiare chi esplora. Ogni stanza ha segreti **facoltativi** (non bloccano la storia) che si trovano
guardando bene. Le stanze e la mappa più grandi sono già chieste da R006: qui si chiede di **riempire** quello spazio.

## 2. Specifica
- **In ogni scena giocabile** (hub, basso, ogni flashback, e ogni stanza nuova di R012–R016):
  - almeno **1 enigma segreto** facoltativo (es. mattonella che suona vuota, cassetto con doppio fondo, sequenza di
    campanelli dei balconi, disegno sul muro che indica dove scavare), costruito con il sistema di R009;
  - **3–5 segreti piccoli** da trovare: oggetti d'epoca da collezione ("Ricordi di Napoli": figurine, cartoline,
    monete del 1946, giocattoli poveri, ritagli di giornale con notizie vere dell'epoca e fonte in `TRAMA.md`).
- **Collezione nel quaderno**: pagina "Segreti" con caselle vuote per stanza (es. "Basso: 3/5"), ogni pezzo con una breve
  scheda storica o civica originale. Contatore discreto nel menu pausa, mai nell'HUD.
- **Ricompense** solo narrative o estetiche: una cutscene breve in più, un ricordo extra di Amerigo, una pagina di
  "dietro le quinte" sui veri treni della felicità, eventuale finale con un dettaglio in più se si trova tutto.
- **Easter egg** discreti e coerenti col tono serio: rimandi interni al gioco (violino, mela annurca, scarpe), niente
  riferimenti a marchi, meme o altri giochi, niente rottura del tono drammatico.
- Indizi ambientali per trovarli (una luce, un suono, una scritta), nessuna freccia. Il pulsante Indizio **non** svela i
  segreti finché la stanza principale non è completata; dopo, può dare un aiuto leggero ("In questa stanza c'è ancora qualcosa…").
- Dati nei JSON: nuovo `segreti.json` `{ segreti: [ {id, scena, tipo: collezionabile|enigma, nome, scheda, x, y, z, ricompensa} ] }`;
  progressi nel salvataggio (`treno_save`).

## 3. Vincoli
Didattica non punitiva, coerenza d'epoca, testi originali, licenze degli asset come da CLAUDE.md, nessun nuovo oggetto
principale senza ok (i collezionabili non sono "oggetti" dell'escape room).

## 4. Criteri di accettazione
- [ ] Ogni scena esistente ha ≥ 1 enigma segreto e 3–5 collezionabili, tutti raggiungibili e facoltativi.
- [ ] Pagina "Segreti" nel quaderno con progressi salvati.
- [ ] Nessun segreto blocca la storia; console pulita; screenshot in `Docs/richieste/media/R011/`.

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R011_schema.md`](schemi/R011_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 premio per chi trova tutti i segreti: una scena in più nel finale (Amerigo consegna a Carmine la "scatola dei ragazzi del vicolo").
  - D2 ritagli di giornale: testi nostri su fatti verificati, nessuna riproduzione d'archivio.
  - Via Medina (11/6/1946) esclusa dallo schema. Il ritaglio "partono i bambini" va al punto 12, perché il punto 17 è di R020-H3.

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto
_(da compilare a fine lavoro)_
