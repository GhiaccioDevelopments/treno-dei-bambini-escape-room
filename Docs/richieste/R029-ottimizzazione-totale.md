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

### Sessione 2026-11-22 (PC casa, con Unity): tutta la coda compilata, costruita e provata
**Fatto**
- Compilazione: un solo errore in tutta la coda (`FraseDef` definita due volte da R013 e R015): unita in una classe sola. Un controllo di compilazione fuori da Unity (Roslyn sulle DLL del motore) ora gira in pochi secondi prima di ogni ricompilazione.
- Scene costruite da script: 40 Stracci, 50 Volantino, 60/61 Cappotto, 70 Modena, 80 basso 1994, piu' hub, basso e ricordo delle scarpe; piante della mappa, punti dei segreti (61, 0 problemi), un solo bake delle 9 scene (612 s).
- **Bug di gioco corretti**: a Modena le porte di casa di Derna, del laboratorio e della scuola erano murate (il muro usava -1 per dire "senza porta", ma le porte hanno coordinate negative): quattro enigmi irraggiungibili. Nel finale il lenzuolo sulla credenza entrava nel muro e uno scatolone stava dentro la bacinella. Texture del biglietto del treno con il nome sbagliato. Obiettivi: finiti gli enigmi di un ricordo la mappa ora dice "capitolo finito" finche' non si esce (prima indicava gia' il capitolo dopo).
- **Enigmi piu' semplici** (richiesta dell'utente): testi di tutti i 41 enigmi principali riscritti (consegna all'imperativo, tre indizi progressivi con l'ultimo che da' quasi la soluzione, errori che dicono cosa provare), in `ArtSource/Enigmi/testi_semplici.py` applicato da `aggiungi_biglietti.py`. Ciclostile 5 -> 3 giri, forno e pianoforte piu' tolleranti.
- **Personaggi**: Capa 'e fierro e altri dieci (Antonietta, Maddalena, la Pachiochia, controllore, due volontarie, Derna, Alcide, Rosa, Carmine) generati con MakeHuman **in automatico** (plugin temporaneo, nessun clic), importati come Humanoid con l'idle di Mixamo. Texture ridotte a 1024 px (budget LFS); in git solo .mhm e screenshot, FBX e texture si rigenerano con `genera_personaggi_makehuman.py`.
- Verifica di raggiungibilita': gli enigmi ora contano la portata della mano (prima segnalava falsi problemi per gli oggetti su tavoli e mensole): 0 problemi in tutte le scene.

**Prove automatiche (tutte nell'editor, 0 FAIL)**
| Prova | Esito |
|---|---|
| -enigmitest | 258 PASS |
| -segretitest | 452 PASS |
| -mappatest | 342 PASS |
| -introtest | 64 PASS |
| -straccitest | 81 PASS |
| -volantinotest | 86 PASS |
| -cappottotest | 87 PASS |
| -violinotest | 84 PASS |
| -finaletest | 77 PASS (fino ai titoli di coda e al menu) |
Le prove vecchie erano scritte per 3 stanze e per il gioco senza filmato iniziale e senza biglietto R026: aggiornate (filmato spento con `GameManager.FilmatoSpento`, biglietto chiuso prima dei clic simulati, opzioni ripulite dopo Nuova partita, conteggi limitati alle stanze visitate).

**Decisioni prese da Claude** (correggibili)
- Nessuna domanda all'utente: ogni scelta qui sopra e' la raccomandata.
- Sorgenti pesanti dei personaggi fuori da git (223 MB), texture a 1024 px.
- L'accompagnatrice seduta nel treno resta una figura semplice (manca una posa seduta); Carmine nel finale resta la figura dell'enigma (il modello 3D e' pronto).
- Limiti degli abiti MakeHuman: gonne sopra il ginocchio e calzini corti, cravatta al controllore.

**Resta da fare**
- Misure FPS per scena e preset (benchmark Windows), peso delle build, gioco a mano col gamepad, voci da registrare (utente).

### Sessione 2026-11-23 (PC casa): personaggi d'epoca, prove complete, misure
**Fatto**
- Abiti d'epoca rigenerati e importati (11 personaggi, avatar validi, 52 ossa): gonna lunga a meta' polpaccio per le donne, Derna in giacca e gonna, controllore senza cravatta e con berretto da ferroviere (un po' alto, accettabile).
- Minimappa: l'obiettivo resta sempre visibile (dopo ogni interazione, al passo successivo, dentro i ricordi, "esci dal ricordo" a capitolo finito).
- Enigma "ordina" giocabile anche senza mouse (frecce + Invio).
- Qualita' di partenza per piattaforma: Windows Alta, WebGL Bassa.

**Prove automatiche sulla build Windows (tutte 0 FAIL, 1662 controlli)**
| Prova | Esito |
|---|---|
| -menutest | 31 PASS |
| -enigmitest | 260 PASS |
| -segretitest | 452 PASS |
| -mappatest | 440 PASS |
| -introtest | 64 PASS |
| -straccitest | 81 PASS |
| -volantinotest | 86 PASS |
| -cappottotest | 87 PASS |
| -violinotest | 84 PASS |
| -finaletest | 77 PASS |

**Benchmark** (`-benchmark`, 15 s per scena e preset, 1280x720 in finestra, RTX 4060 Ti; FPS medi e, tra parentesi, 1% low)
| Scena | Bassa | Media | Alta | Ultra |
|---|---|---|---|---|
| 01_Titolo | 2347 (779) | 600 (254) | 605 (322) | 458 (189) |
| 10_Hub_Vicolo | 843 (247) | 586 (152) | 632 (321) | 461 (187) |
| 20_Int_BassoAntonietta | 1137 (224) | 930 (145) | 892 (157) | 821 (310) |
| 30_FB_Scarpe | 1176 (338) | 801 (341) | 708 (394) | 595 (309) |
| 40_FB_Stracci | 1082 (433) | 709 (428) | 599 (339) | 523 (297) |
| 50_FB_Volantino | 1107 (369) | 728 (430) | 611 (342) | 512 (257) |
| 60_FB_Stazione | 1178 (357) | 819 (421) | 728 (338) | 604 (271) |
| 61_FB_Vagone | 1337 (362) | 1014 (434) | 882 (356) | 815 (494) |
| 70_FB_Modena | 1195 (343) | 911 (516) | 797 (469) | 676 (364) |
| 80_Basso_1994 | 1391 (367) | 986 (155) | 957 (166) | 942 (496) |
Ad Alta il minimo (1% low) e' 157 FPS: il criterio dei 60 FPS e' rispettato con largo margine su questo PC. Manca una misura su un PC scolastico (da fare a scuola con lo stesso comando).

| Peso | Valore (23/11) |
|---|---|
| Build Windows (senza cartella di backup) | 697 MB |
| Git LFS | 806 file, 550 MB (sotto 850) |
