# R008 – Impostazioni grafiche dettagliate (ombre, luci, effetti…)

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ fdeaedc. **Il codice attuale vince** |
| **Dipende da** | R007 (le voci da regolare sono soprattutto gli effetti che R007 aggiunge) |
| **Priorità** | Alta: senza questa la grafica nuova rischia di non girare sui PC della scuola |

## 1. Cosa ha chiesto l'utente (parole sue)
> IMPOSTAZIONI: siccome migliori la grafica fai che si può abbassare grafica di ombre, luci etc.. tutto quello che si può modificare.

## 2. Situazione di partenza
- `GraphicsPresets.cs`: preset Bassa/Media/Alta/Ultra (salvati in `treno_grafica`), interruttori di grana e bloom.
- Menu Impostazioni (2.7, stile R001) con sezione Grafica: preset + pochi interruttori. CLAUDE.md prevede anche "Personalizzata".

## 3. Vincoli
- UI nello stile esistente (R001), testi brevi in italiano, ogni voce con una riga di spiegazione
  (es. "Ombre: quanto sono definite. Abbassale se il gioco va a scatti").
- Preferenze salvate (PlayerPrefs, come ora) e applicate **subito**, senza riavvio, dove Unity lo permette; per le voci che
  richiedono di ricaricare la scena, dirlo accanto alla voce.
- Cambiare una singola voce porta il preset a **Personalizzata**; scegliere un preset riscrive tutte le voci.
- WebGL: nascondere o bloccare le voci non supportate invece di mostrarle rotte.

## 4. Specifica: tutte le voci che hanno senso con URP
| Gruppo | Voci |
|---|---|
| Generale | Preset (Bassa/Media/Alta/Ultra/Personalizzata), risoluzione, finestra/schermo intero, VSync, limite FPS (30/60/120/illimitato), scala di rendering (50–100%), contatore FPS |
| Ombre | Qualità (spente/basse/medie/alte/ultra = risoluzione shadow map), distanza, numero di cascate, ombre morbide, ombre delle luci secondarie |
| Luci | Luci secondarie (per pixel/per vertice/spente), numero massimo per oggetto, qualità dei riflessi (blend dei reflection probe, SSR se presente) |
| Effetti | Anti-aliasing (nessuno/FXAA/SMAA/MSAA 2x-4x-8x), occlusione ambientale (SSAO), bloom, grana, vignetta, profondità di campo, motion blur, decal (spente/vicine/tutte), nebbia |
| Meteo | Pioggia (spenta/leggera/normale/intensa), schizzi e gocce, superfici bagnate e pozzanghere riflettenti |
| Dettaglio | Qualità texture (piena/metà/un quarto), filtro anisotropico, distanza di visuale/LOD, densità dei dettagli sparsi (panni, macerie, oggetti) |
| Accessibilità | (esistenti) + "Riduci effetti di movimento" (spegne motion blur, scosse della camera, gocce sulla lente) |

- Pulsante **"Ripristina predefiniti"**. Opzionale: **"Rileva automaticamente"** (misura gli FPS per qualche secondo e propone un preset).

## 5. Criteri di accettazione
- [ ] Ogni voce ha un effetto visibile o misurabile, si salva e sopravvive al riavvio.
- [ ] Cambio di una voce → preset "Personalizzata"; scelta di un preset → voci coerenti.
- [ ] Tabella FPS nel Rapporto: Bassa/Media/Alta/Ultra + "tutto al minimo", nell'hub con la pioggia.
- [ ] WebGL senza voci rotte; console pulita; screenshot del menu in `Docs/richieste/media/R008/`.

## Rapporto
_(da compilare a fine lavoro)_
