# R029 – Ottimizzazione totale del gioco

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity (+ Blender MCP per LOD e decimazione) |
| **Base** | `main` aggiornato. **Il codice attuale vince** |
| **Dipende da** | **tutto il resto**: è l'**ultima** richiesta della coda (dopo R028) |
| **Priorità** | In coda (ultima), ma da fare in profondità |

## 1. Cosa ha chiesto l'utente (parole sue)
> OTTIMIZZA il gioco al 100%, più che puoi, ottimizza TUTTO (sempre una volta finito tutto, alla fine della coda).

## 2. Specifica
Misurare, ottimizzare, rimisurare: ogni intervento con numeri prima/dopo nel Rapporto.
1. **Misure di base**: benchmark esistente (`-benchmark`) per ogni scena e preset (Bassa, Media, Alta, Ultra) in Windows; WebGL in
   Chrome su un PC normale; tempi di caricamento di ogni scena; RAM e VRAM; peso delle build Windows e WebGL; peso del repo e di LFS.
   Profiler (CPU, GPU, memoria) e Frame Debugger (draw call, batch, SetPass).
2. **Rendering**: SRP Batcher e GPU instancing dove servono, static batching, occlusion culling cotto per ogni scena, LOD su tutti i
   modelli (anche enigmi e ricordi nuovi), ombre e cascate per preset, luci dinamiche al minimo, post-process per preset,
   shader variant stripping, materiali unificati, atlas.
3. **Asset**: texture compresse per piattaforma e alla dimensione giusta, mipmap, audio compresso (Vorbis, musica in streaming,
   mono dove basta), mesh compression, eliminazione degli asset non usati (anche da LFS).
4. **Codice**: niente allocazioni per frame (GC), niente `Find`/`GetComponent` nei loop, pooling, Update solo dove serve, UI
   ricostruita solo quando cambia (canvas separati per HUD statico e dinamico), caricamenti asincroni con schermata d'epoca.
5. **WebGL**: Gzip + Decompression Fallback (come deciso), code stripping alto, IL2CPP ottimizzato per dimensione, build il più
   piccola possibile.
6. **Nessuna perdita visibile** ad Alta/Ultra: screenshot prima/dopo per ogni scena; a Bassa sono ammessi tagli motivati.
7. Un solo bake delle luci finale (regola LFS). Avvisare l'utente se LFS si avvicina a 850 MB.

## 3. Criteri di accettazione
- [ ] Tabella prima/dopo (FPS per scena e preset, caricamenti, RAM, peso delle build e del repo).
- [ ] ≥60 FPS stabili ad Alta su PC normale; fluido a Bassa su PC scolastici; WebGL ≥30 FPS a Bassa.
- [ ] Screenshot prima/dopo in `Docs/richieste/media/R029/`; self-test di tutte le richieste ancora PASS; console pulita.

## Rapporto

**Stato: parziale (2026-11-13).** Al PC della scuola senza Unity si può fare solo la parte di **codice** e di **impostazioni della build**: tutte le misure
(FPS, caricamenti, RAM/VRAM, peso delle build, profiler, frame debugger, screenshot prima/dopo) e gli interventi su rendering e asset richiedono l'editor.
Base: `richiesta/R029-ottimizzazione-totale` con dentro **tutta la coda** (R012-R016, R026-R028).

### Misure fatte qui
| Cosa | Valore (13/11) |
|---|---|
| File in Git LFS | 613, **349 MB** (sotto la soglia di avviso di 850 MB) |
| Cartella del progetto senza `.git` e `Library` | 477 MB |

### Fatto (codice, punto 4)
- Controllo di tutti gli `Update` / `LateUpdate` del gioco (esclusi i self-test): nessuna stringa costruita a ogni frame, nessuna LINQ; le ricerche erano già quasi tutte limitate. Corretti:
  - `CameraGuard.LateUpdate`: `GetComponent<CinemachineBrain>()` a ogni frame -> messo in cache;
  - `MinimappaHud` e `NascostoPuzzle`: quando il giocatore non c'è lo cercavano con `FindAnyObjectByType` a ogni frame -> al massimo due volte al secondo;
  - `Trofei.Vera` (R027): la condizione si divide una volta sola (il monitor la controlla ogni secondo).
- Codice nuovo di R015-R028 scritto senza allocazioni per frame (clip sintetizzate in cache, vista ravvicinata con coroutine solo quando serve).

### Fatto (WebGL, punto 5)
- `WebGLBuilder`: oltre a Gzip + Decompression Fallback, **stripping del codice alto**, **IL2CPP ottimizzato per dimensione**, strip del codice del motore,
  solo eccezioni esplicite. `Assets/_Game/link.xml` protegge tutto `Assembly-CSharp` (classi lette da JsonUtility, componenti aggiunti a runtime).

### Da fare a casa con Unity (in quest'ordine)
1. Chiudere prima tutte le altre richieste (sono dentro questo branch).
2. **Misure di base** con `-benchmark` per ogni scena (ora 11, anche Modena e il basso del 1994) e preset; build Windows e WebGL e loro peso; Profiler e Frame Debugger.
3. Rendering e asset (punti 2 e 3): occlusion culling cotto, LOD per gli enigmi e i ricordi nuovi (tutti di primitive: valutare la fusione delle mesh statiche),
   import delle texture e degli audio, asset non usati. **Le scene nuove sono tante primitive separate**: è il primo posto dove cercare draw call.
4. Build WebGL con le impostazioni nuove: se lo stripping alto rompe qualcosa, scendere a Medium e annotarlo.
5. Un solo bake finale; tabella prima/dopo e screenshot in `Docs/richieste/media/R029/`; tutti i self-test.

### Criteri di accettazione
- [ ] Tabella prima/dopo (FPS per scena e preset, caricamenti, RAM, peso delle build e del repo).
- [ ] ≥60 FPS stabili ad Alta su PC normale; fluido a Bassa su PC scolastici; WebGL ≥30 FPS a Bassa.
- [ ] Screenshot prima/dopo in `Docs/richieste/media/R029/`; self-test di tutte le richieste ancora PASS; console pulita.
