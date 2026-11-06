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

## Rapporto
_(da compilare a fine lavoro)_
