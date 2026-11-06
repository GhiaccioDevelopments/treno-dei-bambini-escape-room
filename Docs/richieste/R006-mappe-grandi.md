# R006 – Mappe molto più grandi e pulizia dei modelli che si compenetrano

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ fdeaedc. **Il codice attuale vince** |
| **Priorità** | Alta. È la **prima** del gruppo R006–R010: R007 (grafica) e R009/R010 (enigmi) lavorano sulle mappe nuove |

## 1. Cosa ha chiesto l'utente (parole sue)
> Ingrandisci molto le mappe […] controlla le mappe: ci sono diversi modelli dentro di loro.

Obiettivo: ambienti **molto più grandi** e credibili da esplorare, senza oggetti incastrati uno dentro l'altro.

## 2. Situazione di partenza
- Scene: `10_Hub_Vicolo`, `20_Int_BassoAntonietta`, `30_FB_Scarpe` (più `01_Titolo` come sfondo), tutte generate da
  `Assets/_Game/Editor/SceneBuilder*.cs` (menu **Treno ▸ …**): oggi un vicolo corto, un basso di una stanza, una stanza del flashback.
- L'utente vede **modelli compenetrati** (mobili, casse, muri, oggetti di scena che si intersecano o fluttuano).
  Cause probabili: posizioni fisse nei builder pensate per i segnaposto della Fase 1 e poi riusate per modelli
  realistici con ingombri diversi; dettagli sparsi (`SceneBuilderDettagli`) senza controllo di sovrapposizione.

## 3. Vincoli
- Tutto resta **generato da script** (nessuna modifica a mano nelle scene), riproducibile da menu.
- Epoca: Napoli, Quartieri Spagnoli, autunno 1946 (`Docs/TRAMA.md`). Niente oggetti moderni.
- Prestazioni: luce baked, occlusion culling, LOD. La mappa più grande non deve far scendere la build WebGL (Bassa)
  sotto i 30 FPS né gonfiare il peso (Git LFS: 1 GB in tutto). Riusare moduli (kit di facciate, porte, balconi).
- Le coordinate di enigmi e oggetti nascosti stanno nei JSON (`x, y, z`): vanno **aggiornate nei JSON**, non nel codice.

## 4. Specifica
### 4.1 Controllo compenetrazioni (prima di tutto)
- Nuovo menu **Treno ▸ Verifica ▸ Compenetrazioni**: per ogni scena segnala coppie di oggetti che si intersecano oltre una
  tolleranza (es. 2 cm, esclusi i contatti voluti con pavimenti e muri), oggetti sospesi (nessun appoggio entro 5 cm) e
  oggetti sotto il pavimento. Rapporto in console + `Docs/richieste/media/R006/compenetrazioni.txt`.
- Correggere **tutte** le segnalazioni nei builder (posizioni calcolate dagli ingombri reali del modello, appoggio a terra
  con raycast, controllo dello spazio libero prima di piazzare un dettaglio). Ripetere la verifica fino a zero errori.

### 4.2 Mappe più grandi (indicativo: almeno 3–4 volte la superficie calpestabile di oggi)
- **Hub vicolo** → un pezzo di Quartieri Spagnoli: vicolo principale in salita con scalinata, 2–3 traverse, uno slargo
  con fontanella o edicola votiva, archi, bassi con porte aperte, panni stesi, balconi, macerie di guerra in un angolo.
  Le strade secondarie finiscono in chiusure credibili (portoni sbarrati, macerie), non in muri invisibili.
  Gli altri luoghi di `luoghi.json` (casa di Capa 'e fierro, sede del partito, stazione, vicolo degli stracci) hanno già
  una porta o uno sbocco visibile, chiuso finché il luogo non è "aperto".
- **Basso di Antonietta** → stanza principale, alcova con letto, angolo cucina, retro o soppalco: stretto e sovraffollato
  come un vero basso, ma con più punti di interesse per gli enigmi (R009/R010).
- **Flashback "Le scarpe"** → spazio del ricordo più ampio (es. tratto di via con botteghe, calzolaio, passanti fermi come
  in un ricordo), coerente con il nuovo disegno degli enigmi (R010).
- Orientamento senza minimappa moderna: luce, insegne d'epoca, punti di riferimento (campanile, edicola votiva).
- Aggiornare spawn, porte, trigger, collisioni della camera nei vicoli stretti, light probes, reflection probes,
  occlusion culling e bake delle luci.

## 5. Ordine di lavoro consigliato
1. Verifica compenetrazioni + correzioni sulle mappe attuali (commit a parte: da solo risolve il problema segnalato).
2. Hub vicolo grande. 3. Basso. 4. Flashback. 5. Bake, FPS per preset, peso build.

## 6. Criteri di accettazione
- [ ] **Treno ▸ Verifica ▸ Compenetrazioni** → zero segnalazioni in tutte le scene.
- [ ] Hub, basso e flashback almeno 3 volte più grandi, percorribili senza incastrarsi e senza muri invisibili "a vista".
- [ ] Scene rigenerabili da menu; console pulita; enigmi e oggetti ancora raggiungibili (coordinate JSON aggiornate).
- [ ] FPS e peso build per preset nel Rapporto; WebGL Bassa ≥ 30 FPS.
- [ ] Screenshot prima/dopo di ogni scena in `Docs/richieste/media/R006/`.

## 7. Decisioni dell'utente (2026-11-06, prima dello sviluppo)
- **Hub = grande quartiere** (6–8 volte l'attuale, non 3–4): più strade e piazzette, "con dentro molti easter egg,
  oggetti vari, cose da esplorare e cose nascoste nella storia". I segreti veri e propri sono di R011, ma la mappa va
  progettata **già con angoli, nicchie, cortili e punti nascosti** pensati per ospitarli, e piena di oggetti di scena.
- **Basso** = stanza principale + alcova col letto dietro una tenda + angolo cucina + piccolo cortile sul retro.
- **Flashback "Le scarpe"** = via con botteghe e calzolaio; "sempre tutto più grande, con cose nascoste, easter egg e
  oggetti, per non far annoiare".
- **Asset**: autonomia su CC0/CC-BY (Poly Haven, Sketchfab…), annotati in `CREDITS.md`; confronti all'utente solo per i pezzi importanti.
- Versione online: per reggere il quartiere grande usare occlusion culling, LOD aggressivi a Bassa, densità dei dettagli
  ridotta su WebGL; se non basta, dirlo all'utente invece di rimpicciolire la mappa di nascosto.

## Rapporto
_(da compilare a fine lavoro: fatto, scostamenti, come testare, domande aperte)_

### Tappa 2 – hub come grande quartiere (2026-11-06)
- `10_Hub_Vicolo`: 2460 m² calpestabili (prima ~300 m², circa 8x), 36 zone: piazzetta della fontanella, vico principale con 4 scalinate (quota 0 -> 4,8 m),
  slargo dell'edicola votiva con campanile, largo della stazione, 6 traverse, 2 vicoli ciechi, 2 cortili (lavatoio; terrazzino con scala esterna),
  lotto bombardato, 4 ponti-arco (grande arco, sottopasso). Generato da `SceneBuilderQuartiere*.cs` (menu Treno > Costruisci tutte le scene;
  per sola iterazione sull'hub: Treno > Sviluppo > Costruisci solo l'hub, poi bake).
- Punti per i segreti di R011: `Docs/richieste/media/R006/punti_segreti_hub.md` (generato insieme alla scena).
- Verifica: Treno > Verifica > Compenetrazioni = 0 segnalazioni su tutte le scene (la verifica ora usa uno sweep e legge le facce dai triangoli).
- Screenshot: `Docs/richieste/media/R006/hub_*.png`.
