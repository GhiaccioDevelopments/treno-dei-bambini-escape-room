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
- [~] Ogni voce ha un effetto visibile o misurabile, si salva e sopravvive al riavvio (vedi Rapporto: cosa non è verificato).
- [x] Cambio di una voce → preset "Personalizzata"; scelta di un preset → voci coerenti.
- [x] Tabella FPS nel Rapporto: Bassa/Media/Alta/Ultra + "tutto al minimo", nell'hub con la pioggia.
- [~] WebGL senza voci rotte (voci nascoste, verificato solo con la simulazione); console pulita; screenshot del menu in `Docs/richieste/media/R008/`.

## Rapporto
**Fatto (2026-11-08, branch `richiesta/R008-impostazioni-grafiche-dettagliate`, sopra `feature/upgrade-realistico` @ cd78349).**

### Cosa c'è
- **Menu Impostazioni** (`Scripts/UI/SettingsPanel.cs`, stile R001, costruito da codice): sette schede (Generale, Ombre, Luci, Effetti, Meteo, Dettaglio, Accessibilità), elenco scorrevole con barra a leva, per ogni voce nome, riga di spiegazione e controllo a frecce. Si apre dalla **Pausa** (nuovo pulsante "Impostazioni", al posto di "Effetti ridotti", che è passato in Accessibilità) e dal **Titolo** (quarto pulsante). Esc chiude. Pulsante **Ripristina predefiniti** (con conferma).
- **Voci** (`Scripts/Core/GraphicsOptions.cs`, una tabella con testi, opzioni e valori dei quattro preset): Generale (preset, risoluzione, schermo/finestra, sincronia verticale, limite FPS 30/60/120/libero, scala di rendering 50-100%, contatore FPS), Ombre (qualità spente/512/1024/2048/4096, distanza 15-90 m, cascate 1-4, ombre morbide), Luci (luci secondarie spente/per pixel, luci per oggetto 1/2/4/8, qualità dei riflessi), Effetti (anti-aliasing nessuno/FXAA/SMAA/MSAA 2x-4x-8x, occlusione ambientale, bloom, grana, vignetta e aberrazione, profondità di campo delle cutscene, sfocatura di movimento, decal spenti/accesi, nebbia), Meteo (pioggia spenta/leggera/normale/intensa, schizzi e gocce, selciato bagnato e pozzanghere), Dettaglio (qualità texture piena/metà/un quarto, filtro anisotropico, distanza LOD, densità degli oggetti sparsi), Accessibilità (effetti ridotti dell'interfaccia, **riduci effetti di movimento**).
- **Logica** (`GraphicsPresets.cs`): scegliere un preset riscrive tutte le voci legate al preset; cambiare una voce porta a "Personalizzata" (il preset di base resta quello di partenza e scala grana e aberrazione). Non fanno scattare "Personalizzata" schermo, risoluzione, contatore FPS e accessibilità. Tutto si applica subito e si salva in PlayerPrefs (`treno_grafica` = preset di base, `treno_grafica_pers`, `treno_g_<voce>`). I salvataggi del 2.7 (solo preset + grana + bloom) si migrano: grana o bloom spenti diventano "Personalizzata". Le impostazioni URP (scala, MSAA, ombre, luci, riflessi), le funzioni dei renderer e i valori di QualitySettings (sincronia, texture, anisotropico, LOD) si scrivono sugli asset del livello corrente; **nell'editor `GraphicsEditorBackup` salva lo stato originale in `SessionState` (JSON, sopravvive al ricaricamento del dominio) e lo rimette all'uscita dal Play** (o al caricamento dell'editor se il Play è finito male). Fuori dal Play dell'editor gli asset non vengono toccati. Se Bassa ha SSAO o decal accesi usa il renderer di Media (quello leggero non li ha).
- **Meteo** (`MeteoController`): pioggia, schizzi/gocce e selciato bagnato/pozzanghere si regolano al volo dalle voci (prima erano fissati all'avvio della scena).
- **Dettaglio sparso** (`ScenaDettagli.cs`): a "Medi"/"Pochi" nasconde un terzo/due terzi degli oggetti sparsi, ma solo quelli senza collider (non trigger), senza luce cotta (lightmap) e non interagibili: niente ostacoli invisibili né ombre cotte rimaste. Nell'hub risultano idonei solo i panni stesi (120; 69 nascosti a "Pochi"). Per questo il default è "Tutti" anche a Bassa (nascondere i panni cambierebbe troppo il look).
- **Riduci effetti di movimento**: spegne la sfocatura di movimento e, nel ricordo, il tremolio del fotogramma e lo sfarfallio di esposizione della pellicola (`_TrenoPellicolaMov` in `PellicolaRicordi.shader`).
- **WebGL**: nascoste risoluzione, schermo, sincronia, limite FPS, SSAO, sfocatura di movimento, decal; anti-aliasing fino a SMAA (niente MSAA). SSAO, decal e sfocatura sono anche **forzati spenti** nell'applicazione (anche con il preset Media), non solo nascosti (provato con la simulazione: le funzioni del renderer restano spente).
- **Strumenti di prova**: `GraphicsStress` (cambia preset e voci a raffica; da riga di comando `-graphicsstress [scena]`), `-benchmarkscene` e preset 4 = "Minimo" nel benchmark.

### Valori dei preset (cosa cambia rispetto al 2.x)
I preset conservano il look precedente (ombre, MSAA, luci, riflessi, anisotropico, LOD, pioggia e oggetti sparsi uguali al 2.x). Differenze: sincronia verticale spenta a Bassa e accesa negli altri; limite 60/60/120/120 FPS. La pioggia ha cinque livelli (Spenta, Leggera 0,12, Normale 0,45, Forte 0,8, Intensa 1) e i preset usano Leggera/Normale/Forte/Intensa, cioè gli stessi valori di prima; gli schizzi hanno la frequenza di prima (30/60/90 al secondo).

### FPS (build Windows di sviluppo, RTX 4060 Ti, 1280x720, vsync spento, hub con pioggia e pellicola, tragitto fisso di 20 s, camera di prova)
| Preset | FPS medi | 1% low (FPS) | Frame max (ms) |
|---|---|---|---|
| Bassa | 1230,9 | 647,6 | 2,7 |
| Media | 828,6 | 448,0 | 7,1 |
| Alta | 741,6 | 424,3 | 3,0 |
| Ultra | 622,6 | 371,8 | 7,2 |
| Tutto al minimo | 1367,4 | 577,3 | 28,9 |

Dati grezzi in `media/R008/fps_hub_windows.csv` (ordine Ultra, Alta, Media, Bassa, Minimo). "Tutto al minimo" = Bassa con ogni voce al valore più economico (pioggia leggera). Il codice nuovo non aggiunge passi di rendering. **La tabella è stata misurata prima delle correzioni di review** (pioggia e schizzi avevano valori diversi da quelli attuali, oggetti sparsi a Bassa diversi): va rimisurata. I numeri sono un **limite superiore** (GPU molto veloce, camera non del giocatore): non provano nulla su un PC scolastico.

### Provato
- Compilazione e console pulita (solo un avviso interno di Unity "JobTempAlloc" visto una volta cambiando preset nell'editor).
- **Prova a raffica**: 300 cambi di preset e di voci di fila nell'hub in una build Windows di sviluppo, 0 errori e nessun crash; 150 cambi nell'editor con i quattro livelli e 120 con i due livelli WebGL, 0 errori.
- Menu: schede, scorrimento, valori, "Personalizzata" dopo il cambio di una voce, simulazione WebGL (voci nascoste): screenshot in `media/R008/`.

### Non fatto o non verificato (apertamente)
- **"Rileva automaticamente"**: non fatto (era opzionale).
- **Luci "per vertice"**: non offerta. Nessun asset URP usa quella modalità e le build con ogni probabilità tolgono quelle varianti di shader (non verificato): nella build potrebbe essere rotta. Restano "Spente" e "Per pixel".
- **Ombre delle luci secondarie**: voce presente nel codice ma **nascosta**: nessun asset URP le abilita e le build con ogni probabilità tolgono le varianti di shader necessarie (non verificato), quindi non avrebbe effetto.
- **Luci per oggetto**: ha effetto solo a Bassa e Media (Forward); Alta e Ultra usano Forward+ e ignorano il limite (lo dice la spiegazione).
- **Sfocatura di movimento**: spenta di base a tutti i preset (non cambio il look senza l'ok dell'utente); si accende dal menu, non su WebGL. Non guardata a occhio in movimento: provato solo che il componente si aggiunge e non dà errori.
- **Riduci effetti di movimento**: nel gioco non esistono scosse della camera né gocce sulla lente; la voce agisce solo su sfocatura di movimento e pellicola.
- **Risoluzione, schermo intero, sincronia, limite FPS**: nell'editor sono ignorati; **non provati a mano in una finestra Windows**. Alt+Invio non toccato.
- **WebGL**: non fatta una build WebGL con questo codice; la simulazione (`GraphicsPresets.SimulaWebGL`) ha solo mostrato che le voci sono nascoste.
- **Salvataggi vecchi**: la migrazione è scritta ma **non provata** con un vero salvataggio del 2.7.
- **Pioggia e schizzi al volo**: verificati da codice e da una prova (pioggia a 0, pozzanghere spente); non guardati a occhio in movimento.
- **Dettaglio sparso**: provato solo nell'hub (idonei solo i panni); non guardato a occhio, né provato nelle altre scene.
- **Crash dei decal di URP** (`DecalDrawSystem.Draw`): **non risolto, preesistente**. Nel benchmark (ricarica della scena e camera sostituita a ogni prova) si riproduce a caso, sia con questo codice sia con la build di R007 (provata: crash anche lì). Nella prova a raffica nel gioco (300 cambi senza cambiare scena) non è mai comparso. La mia prima versione usava una copia dell'asset URP e peggiorava la cosa (due renderer condividevano le stesse funzioni): scartata.
- La build Windows ha riscritto da sola alcuni asset (flag di stripping in `URP_*.asset`, `ProjectSettings`): **non inclusi nei commit**.
- LFS: nessun asset pesante aggiunto (solo codice, sei JPG e un CSV); nessun bake.
- **Decal "Vicini"**: tolta. In URP la distanza massima dei decal è copiata nei sistemi solo alla creazione della funzione (`DecalRendererFeature`, righe 400 e 404 del sorgente del pacchetto): cambiarla a runtime non ha effetto, e ricrearla sarebbe proprio l'operazione che manda in crash i decal. Restano Spenti/Accesi (letto dal codice, non provato a occhio).
- Dopo la review la tabella FPS non è stata rimisurata (vedi sopra) e non ho rifatto la build Windows con le correzioni.
- Il controllo "nessun salvataggio vuoto dal Titolo" è nel codice (`UIRoot.SalvaSeInCorso`) ma non l'ho provato: nell'editor esiste già un salvataggio.
- `GraphicsStress` usa `#if UNITY_EDITOR || DEBUG` (non `DEVELOPMENT_BUILD`: Unity lo segnala come deprecato).

### Correzioni dopo la review
1. QualitySettings (sincronia, texture, anisotropico, LOD) ripristinati all'uscita dal Play: provato Play a Media con LOD e texture cambiati, Stop: sincronia 0, LOD 0,8, mip 0, livello di partenza, `git status` senza `ProjectSettings/QualitySettings.asset`.
2. Stato originale degli asset in `SessionState`: provato con un ricaricamento del dominio durante il Play (il salvataggio resta e il ripristino riesce).
3. Dettaglio sparso solo su oggetti senza collider e senza luce cotta (vedi sopra); Bassa torna a "Tutti".
4. Nessun salvataggio creato dal Titolo chiudendo le Impostazioni o cambiando gli effetti ridotti (`SalvaSeInCorso`).
5. WebGL: SSAO, decal, sfocatura e ombre delle luci forzati spenti.
6. Decal "Vicini" tolta (motivo sopra).
7. Pioggia e schizzi ai valori di prima; contatore FPS in un componente separato spento finché la voce è "No" (nessun OnGUI), testo e stile in cache.
8. `GraphicsStress`: a fine prova ripristina lo stato dell'utente, Personalizzata compresa (provato: stato identico prima e dopo 100 cambi).
9. Alt+Invio (o il sistema) che cambia la modalità schermo aggiorna `treno_g_schermo` e la dimensione della finestra (controllo di un booleano per frame; non provato in una finestra vera).
