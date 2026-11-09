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
_(vuoto)_
