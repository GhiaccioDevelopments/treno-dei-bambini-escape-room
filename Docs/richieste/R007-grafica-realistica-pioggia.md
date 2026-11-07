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
- [ ] Screenshot prima/dopo di hub, basso, flashback a Bassa e Ultra in `Docs/richieste/media/R007/`, approvati dall'utente.
- [ ] Pioggia nell'hub guidata da `luoghi.json`, con audio e superfici bagnate.
- [ ] Alta ≥ 60 FPS su PC normale; WebGL Bassa ≥ 30 FPS.
- [ ] UI invariata; console pulita; ogni asset nuovo in `CREDITS.md`.

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
_(da compilare a fine lavoro)_
