# R007 – Grafica realistica e cinematografica, con la pioggia

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ fdeaedc. **Il codice attuale vince** |
| **Dipende da** | R006 (lavorare sulle mappe nuove). R008 dipende da questa |
| **Priorità** | Alta |

## 1. Cosa ha chiesto l'utente (parole sue)
> Migliora notevolmente la grafica, ma non delle UI, ma della grafica come se fosse The Last of Us, realistica, con pioggia.

Obiettivo: il **mondo 3D** (non l'interfaccia) deve sembrare un gioco realistico moderno: luce, materiali, atmosfera,
pioggia. The Last of Us è un riferimento di **qualità e atmosfera** (luce morbida e direzionale, materiali sporchi e
credibili, umidità, profondità), non di contenuti: niente asset, scene o look copiati.

## 2. Situazione di partenza
- URP con preset Bassa/Media/Alta/Ultra (`GraphicsPresets.cs`), LUT seppia, grana, bloom, nebbia semplice, luce baked
  (`LookBuilder.cs`). Il look d'epoca della Fase 2.1 è approvato: **va potenziato, non stravolto** (desaturato, caldo, neorealista).
- L'interfaccia (R001) **non si tocca**.

## 3. Vincoli
- Coerenza d'epoca 1946 e tono serio, mai cartoon (CLAUDE.md, "Stile visivo").
- "Bello ma leggero": luce baked come base. Gli effetti costosi sono **disattivabili** e scalano con i preset (R008).
- Asset esterni: CC0 preferito, CC-BY con attribuzione; ognuno in `CREDITS.md`, con screenshot e valutazione.
  Se manca qualcosa di importante, dirlo invece di ripiegare su roba scadente.
- WebGL (Bassa, Mobile_RPAsset) resta giocabile: lì pioggia e post-effetti in versione minima.

## 4. Specifica
### 4.1 Luce e post-processing
- Bake di qualità più alta (più campioni, risoluzione lightmap adeguata, AO baked), light probes densi nei vicoli,
  reflection probes per ogni ambiente. Cielo coperto e piovoso: luce morbida e fredda fuori, calda dentro (lampade a petrolio, candele).
- Post-processing per preset: tonemapping ACES, color grading d'epoca, SSAO (da Media), bloom morbido, vignetta leggera,
  grana, profondità di campo solo in cutscene, motion blur leggero opzionale, gocce sulla lente opzionali.
- Ombre: cascate e distanza per preset, ombre morbide da Alta, SSAO/contact shadows per radicare gli oggetti a terra.
- Nebbia di distanza/altezza che dà profondità ai vicoli (niente volumetrica costosa; eventualmente piani o shader leggero).

### 4.2 Materiali
- Tutti i materiali PBR completi (albedo, normal, roughness/metallic, AO), densità dei texel coerente, niente texture stirate.
- Sporco, muffa, intonaco scrostato, macchie di umidità con URP Decal Projector (limitati per preset).
- Basoli di pietra lavica con dettaglio forte; legno vissuto; tessuti con normal map.

### 4.3 Pioggia (nuovo)
- Meteo per scena guidato dai dati: campo `meteo` in `luoghi.json` (es. `"pioggia"`, `"coperto"`, `"sereno"`) con intensità.
  Proposta: hub vicolo = pioggia; basso = pioggia sentita da dentro (suono su porta e finestra, gocciolio dal soffitto in un punto);
  flashback = da decidere con l'utente (pannello domande; se non c'è: coperto senza pioggia).
- Effetti: particelle di pioggia attorno alla camera (non su tutta la mappa), schizzi a terra, gocce da grondaie e balconi,
  **superfici bagnate** (roughness più bassa, riflessi) e pozzanghere che riflettono (reflection probe, SSR leggero se disponibile),
  personaggi un po' bagnati (opzionale).
- Audio: pioggia nell'ambiente (`audio.json`, nuovi id `pioggia` e `pioggia_interno`, sintetizzati da codice se manca il file),
  tuoni lontani rari. Seguire lo schema audio esistente.
- Pioggia riducibile o disattivabile nelle impostazioni (R008) e per accessibilità.

## 5. Ordine di lavoro consigliato
1. Luce + post per preset sulle mappe di R006. 2. Materiali e decal. 3. Pioggia (visiva, audio, superfici bagnate).
4. Screenshot prima/dopo a ogni preset e approvazione dell'utente. 5. FPS per preset.

## 6. Criteri di accettazione
- [~] Screenshot in `Docs/richieste/media/R007/`: prima/dopo di hub, basso e ricordo per ogni tappa (`t1_*`, `t2_*`, `t2b_*`, `t3_frame_*`, `t4_*`) approvati dall'utente tappa per tappa. I `t4_*` sono a Bassa e Alta (non Ultra) e sono anteprime dall'editor.
- [~] Pioggia nell'hub guidata da `luoghi.json`, con audio e superfici bagnate: realizzata (campo `meteo`, `MeteoController`, audio `pioggia`/`pioggia_ovattata`, basoli bagnati, pozzanghere), verificata solo nell'anteprima dell'editor e nel benchmark; **non vista in Play dall'utente**.
- [x] Alta ≥ 60 FPS: misurato, 623-949 FPS medi a 1280x720 (tabella sotto). [ ] WebGL Bassa ≥ 30 FPS: **non raggiunto in questa misura** (24 FPS indicativi nel titolo, Chrome automatizzato).
- [x] Console pulita; ogni asset nuovo in `CREDITS.md`. [~] UI invariata: non toccata dal codice, nessun confronto sistematico.

## 7. Decisioni dell'utente (2026-11-06, prima dello sviluppo)
- **Base**: le mappe grandi di R006 (già in `feature/upgrade-realistico`, merge 1af2cc3).
- **Quartiere (presente)**: tardo pomeriggio d'autunno, cielo coperto, **pioggia costante**, lampioni e botteghe che si
  accendono; tono malinconico (ritorno a casa per un lutto).
- **Ricordo "Le scarpe" (1946)**: **sole basso**, luminoso e dorato, nessuna pioggia: contrasto netto con il presente.
  Il campo `meteo` di `luoghi.json` va quindi `pioggia` per l'hub, `sereno` (sole basso) per il flashback; il basso sente la pioggia da dentro.
- **Modelli 3D veri dentro R007**: sostituire gli oggetti fatti con primitive più visibili (letti, panni stesi a "T",
  Madonna dell'edicola a capsula, sedie, tavoli, pentole, carretti, botti, casse, lampioni…) con modelli CC0/CC-BY
  (Poly Haven, Sketchfab…) o rifatti in Blender; annotati in `CREDITS.md`.
- **Qualità**: alta con LOD: texture 2K per gli oggetti vicini, LOD semplici da lontano; WebGL con texture ridotte.
- **Peso**: screenshot di documentazione 1280x720 JPG fuori LFS, un solo bake a fine tappa. LFS è a ~620 MB su 1 GB:
  quando si avvicina a 850 MB avvisare l'utente (comprerà un data pack GitHub) prima del push che lo sfonderebbe.
- Dal review di R006: la radio sul tavolino del barbiere nel flashback è rimasta (plausibile); il cortile del basso e del flashback è troppo scuro e il pavimento battuto legge grigio: da sistemare qui.

## 8. Decisioni dell'utente (2026-11-07)
- **Tappa 1 approvata** (foto `media/R007/t1_*`): si prosegue con le tappe 2-5.
- **R021 fusa in R007**: l'identità "pellicola neorealista" (spec `R021-grafica-realistica-unica.md`, PR #23) si realizza
  dentro le tappe 2-5: tappa 2 modelli veri + set dressing narrativo; tappa 3 materiali fotogrammetrici, decal, cortili +
  post "pellicola" con i look per presente / ricordi / 1994 (3 frame di riferimento da approvare con il pannello);
  tappa 4 pioggia; tappa 5 FPS, peso, confronto prima/dopo, reviewer. Alla chiusura di R007 si chiude anche la PR #23.

## Rapporto

Branch `richiesta/R007-grafica-realistica-pioggia`, cinque tappe (modelli veri, panni, pellicola, materiali/decal, pioggia, misure). Un solo bake per tappa, l'ultimo a fine tappa 4.

### Fatto
- **Tappa 1** luce e cielo coperto, LUT, profili per scena (foto `t1_*`).
- **Tappa 2** oggetti di primitive sostituiti da modelli veri o rifatti in Blender (`ArtSource/Models`), con LOD e collider; panni stesi a 6 capi in 3 varianti di colore, non statici; bancarella, panchina, sedia impagliata, bicicletta d'epoca (wasabicats, CC-BY), pentole di rame, Madonna al posto della capsula.
- **Tappa 3A** pellicola neorealista: grana per luminanza, alone, vignetta, aberrazione, curva a stampa; profili PRESENTE, RICORDI (gate weave, polvere, graffi rari) e 1994 (solo profilo). Bassa: solo grana e curva.
- **Tappa 3B** tufo giallo (tinta), terra battuta ocra, cortili piu' luminosi, decal URP di umidita'/salnitro/macchie (hub 62, ricordo 30), manifesti del referendum 1946, puntellature, gabbiette con uccellino, sedie fuori dai bassi.
- **Tappa 4** pioggia guidata da `luoghi.json` (hub `pioggia`, ricordo `sereno`, basso `pioggia_fuori`), particelle per preset, spruzzi a terra, gocce dai fili, basoli bagnati, pozzanghere, audio di pioggia (ovattato nel basso).
- **Tappa 5** benchmark (`BenchmarkRunner`, argomento `-benchmark`) e build Windows di sviluppo fuori dal repo.

### FPS (build Windows di sviluppo, RTX 4060 Ti, 1280x720, vsync spento, tragitto fisso di 20 s con camera in movimento, pioggia e pellicola attive)
| Scena | Preset | FPS medi | 1% low (FPS) | Frame max (ms) |
|---|---|---|---|---|
| 10_Hub_Vicolo | Bassa | 1068.0 | 414.0 | 4.1 |
| 10_Hub_Vicolo | Media | 711.1 | 274.9 | 8.9 |
| 10_Hub_Vicolo | Alta | 622.9 | 249.6 | 6.4 |
| 10_Hub_Vicolo | Ultra | 528.7 | 227.5 | 6.2 |
| 20_Int_BassoAntonietta | Bassa | 1427.8 | 358.9 | 6.2 |
| 20_Int_BassoAntonietta | Media | 1014.5 | 360.7 | 4.7 |
| 20_Int_BassoAntonietta | Alta | 948.9 | 358.1 | 5.4 |
| 20_Int_BassoAntonietta | Ultra | 821.7 | 313.6 | 5.1 |
| 30_FB_Scarpe | Bassa | 1240.2 | 363.6 | 12.4 |
| 30_FB_Scarpe | Media | 785.3 | 292.8 | 5.0 |
| 30_FB_Scarpe | Alta | 654.0 | 248.5 | 6.7 |
| 30_FB_Scarpe | Ultra | 547.0 | 225.8 | 6.6 |

Alta e' ben oltre l'obiettivo di 60 FPS in tutte le scene: non serve ottimizzare per preset. La camera di prova non e' quella del giocatore e la UI e' presente ma ferma; il caso peggiore e' l'hub in Ultra (529 FPS). Dati grezzi in `Docs/richieste/media/R007/fps_windows.csv`. Su un PC scolastico la GPU e' molto piu' lenta: la misura e' un limite superiore, non la prova su "PC normale".

### Peso
- Build Windows di sviluppo: 474 MB (fuori dal repo, `../treno-build-r007/Win` (variabile TRENO_BUILD_DIR)).
- Build WebGL Bassa (Gzip, renderer Mobile): 59,6 MB (`.../WebGL`, non pubblicata). FPS indicativi in Chrome automatizzato: **24** nella scena del titolo; non misurati hub, basso e ricordo.
- LFS: R007 ha aggiunto circa 151 MB (da 665 a circa 816 MB totali su 1 GB; soglia di avviso 850 MB non superata). I candidati scartati restano nello storage remoto (liberarli richiede di riscrivere la storia).

### Non fatto o limitato (apertamente)
- **Piperno e basoli fotogrammetrici, scrostature a piu' strati**: non aggiunti; i materiali esistenti sono stati solo ritinti (tufo, terra battuta) per non pesare su LFS.
- **Decal assenti nel renderer Lite** (Bassa e WebGL): li disegnano solo Media, Alta e Ultra.
- **Uccellini** delle gabbiette: due blob colorati, non modelli veri.
- **Pioggia non vista in Play dall'utente**: provata solo in anteprima nell'editor e nel benchmark; da tarare in gioco.
- WebGL Bassa sotto i 30 FPS nella sola misura fatta (24 FPS, titolo); da rimisurare su un browser con GPU reale.
- 1 coppia di superfici complanari visibile nel Titolo (Macerie contro Macerie), preesistente.
- Il benchmark usa una camera propria (non il giocatore) e non misura la RAM.
- **Tuoni lontani**, **gocciolio dal soffitto del basso** e **gocce da grondaie e balconi**: non fatti (le gocce ci sono solo dai fili del bucato, da Media in su).
- **Id audio**: la specifica prevedeva `pioggia_interno`; nel codice e in `audio.json` si chiama `pioggia_ovattata`.
- **Profondita' di campo in cutscene** (`GraphicsPresets.CutsceneDepthOfField`): resta predisposta ma non collegata. Le cutscene di questo gioco sono schermate 2D (`CutscenePlayer` mostra uno sfondo a tutto schermo), quindi sfocare il mondo 3D dietro non cambierebbe nulla.
- **Crash intermittente di URP nei decal**: nel benchmark (cambio di scena e di preset a raffica, camera sostituita) il `DecalDrawSystem` di URP andava in crash nel percorso di disegno non istanziato. Con `enableInstancing` sui due materiali dei decal il benchmark completo (12 prove) gira senza crash. Resta da tenere d'occhio nel gioco vero, soprattutto cambiando preset durante la partita.
- **Pioggia nel basso**: ora cade solo in un box fisso sul cortile scoperto (non segue piu' la camera e non entra nelle stanze); la camera e' ricontrollata a ogni frame.
- **Texture condivise**: legno ad assi, pietra, tela di lino e normal del legno scuro stanno in `Assets/Art/Textures/Condivise` (7 MB di duplicati tolti); la maschera del letto e' a 1K. Se si rilanciano gli script `texture_*.py` e l'importatore, rigenerano le copie per modello: vanno riunificate a mano.
- **Percorsi**: nel repo pubblico restano, nella storia dei commit precedenti, alcuni percorsi assoluti con il nome utente (ArtSource/Models/build, BenchmarkBuilder); nella versione attuale sono stati tolti (variabili TRENO_STAGING e TRENO_BUILD_DIR).

### Verifiche finali
Superfici e buchi: 59 complanari, 1 visibile (Titolo), 0 buchi. Compenetrazioni: 0 (restano 4 voci intenzionali del piano di sicurezza). Camera: 10044 pose, 0 errori. Raggiungibilita': 0 problemi.
