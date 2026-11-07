# R018 – Telecamera robusta: mai dentro gli edifici, mai sul vuoto

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3. **Il codice attuale vince** |
| **Dipende da** | R017 (mappe senza buchi): parte dei "vuoti" visti dalla camera spariscono con R017 |
| **Priorità** | Alta |

## 1. Cosa ha chiesto l'utente (parole sue)
> La telecamera a volte si bugga, tipo attraverso edifici, la telecamera mostra il vuoto etc.. risolvi.

## 2. Situazione di partenza
- `Scripts/Player/CameraRig.cs`: `CinemachineCamera` + `CinemachineOrbitalFollow` a 3 anelli, input mouse/pad/touch (R005).
- `Scripts/Player/WallFader.cs`: SphereCast camera↔giocatore, rende invisibili i muri "sottili" che la camera
  (CinemachineDeoccluder) ignora. Due sistemi che si sovrappongono: un muro ignorato dal Deoccluder e non sbiadito = camera dentro l'edificio.
- Vicoli stretti dei Quartieri e stanze piccole del basso: casi peggiori.

## 3. Specifica
1. **Riprodurre e catalogare** i casi (screenshot o GIF): camera dentro i muri, attraverso soffitti/balconi, sotto il pavimento,
   oltre il bordo della mappa, scatti bruschi nei vicoli, camera "intrappolata" dietro uno spigolo, passaggi porta.
2. **Collisione della camera affidabile** (Cinemachine 3):
   - `CinemachineDeoccluder` (o `CinemachineDecollider`) con raggio adeguato (~0,25-0,3 m), strategia "pull camera forward",
     layer di collisione corretti (edifici, muri, soffitti, balconi, props grandi); i props piccoli e i trigger esclusi.
   - Tutti gli edifici e le stanze devono avere **collider** coerenti con la mesh visibile (anche tetti e soffitti): verificarlo
     con uno script di controllo (renderer senza collider nel layer "Mondo" = errore).
   - Smorzamento: rientro rapido verso il personaggio quando c'è un ostacolo, ritorno morbido quando si libera (niente scatti).
   - **Mai** sotto il pavimento: limite verticale minimo dell'orbita e controllo che la camera resti sopra il terreno.
   - Distanza minima: se lo spazio è troppo stretto, la camera si avvicina fino a una vista "da sopra la spalla";
     se il giocatore entra nell'inquadratura troppo da vicino, si sbiadisce (dithering) invece di riempire lo schermo.
3. **Interni (basso)**: profilo camera dedicato (orbita più corta, FOV leggermente più ampio, pitch limitato) attivato da
   volumi/trigger per stanza; soffitti sempre collidenti; il `WallFader` solo per i muri "di facciata" verso la camera.
4. **Riconciliare WallFader e Deoccluder**: una sola regola chiara su cosa si sbiadisce e cosa spinge la camera; documentarla
   in testa ai due script.
5. **Near clip** ≥ 0,1 e far coerente con la nebbia (coordinare con R017 per lo z-fighting).
6. **Prova automatica** (menu `Treno ▸ Verifica ▸ Camera`): in Play, il giocatore viene teletrasportato su una griglia di punti
   dell'area giocabile, la camera ruota di 360° a 3 pitch; per ogni fotogramma si controlla che la camera non sia dentro un
   collider e che un raggio verso il basso trovi terreno. Rapporto in `Docs/richieste/media/R018/`.

## 4. Ordine di lavoro consigliato
1. Catalogo dei casi. 2. Prova automatica (prima). 3. Collider mancanti + Deoccluder/Decollider + limiti.
4. Profili interni. 5. Prova automatica (dopo) + GIF nei vicoli stretti e nel basso.

## 5. Criteri di accettazione
- [ ] La prova automatica non trova la camera dentro geometria né sul vuoto in hub, basso e flashback.
- [ ] Nei vicoli stretti e nelle stanze del basso la camera non attraversa muri né soffitti, senza scatti (GIF).
- [ ] Mouse, gamepad e touch invariati (R005), sensibilità e inversione Y rispettate.
- [ ] Console pulita, nessun calo di FPS misurabile (preset Alta e Bassa, anche WebGL).

## Rapporto
_(da compilare a fine lavoro)_
