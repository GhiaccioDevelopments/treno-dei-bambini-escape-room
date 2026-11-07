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
- [x] La prova automatica non trova la camera dentro geometria né sul vuoto in hub, basso e flashback (9.900 pose, 0 errori; prima 9.972 pose, 3.769 errori).
- [ ] Nei vicoli stretti e nelle stanze del basso la camera non attraversa muri né soffitti, senza scatti (GIF). Verificato: non attraversa (prova + sequenze di screenshot prima/dopo). NON verificato: assenza di scatti in movimento (la prova misura la posa a regime, non i transitori) e nessuna GIF.
- [x] Mouse, gamepad e touch invariati (R005), sensibilità e inversione Y rispettate (verificato dal diff: il codice di input di CameraRig non è stato toccato; non provato con periferiche reali).
- [ ] Console pulita (verificata: nessun errore né avviso), nessun calo di FPS misurabile (preset Alta e Bassa, anche WebGL). FPS NON misurati: serve l'editor in primo piano per un confronto affidabile; costo atteso trascurabile (alcuni raycast e spherecast per fotogramma).

## Rapporto
**Fatto da** Claude (Opus 5.5 con sottomente), PC casa, 2026-11-07, branch `richiesta/R018-telecamera-senza-bug` (partito da `feature/upgrade-realistico` con R022 e R017).

### Catalogo dei casi trovati (prova automatica "prima", camera con regole vecchie, 9.972 pose)
| Caso | Hub | Basso | Flashback |
|---|---|---|---|
| Camera dentro un collider solido (edifici, mobili grandi, muri, porte) | 145 | 1.434 | 271 |
| Camera sul vuoto (nessun suolo sotto di lei: fuori dagli edifici, oltre le finestre) | 8 | 324 | 91 |
| Ostacolo solido tra camera e giocatore (il Deoccluder non lo vede) | 0 | 1.480 | 16 |
| Camera sotto il suolo | 0 | 0 | 0 |
| Pose con errore / pose totali | 153 / 3.276 | 3.238 / 3.420 | 378 / 3.276 |

Cause trovate:
1. **Giocatore addossato a un muro**: lo spherecast del Deoccluder parte già dentro il collider e non lo rileva, quindi la camera finiva dietro/dentro i palazzi (hub, flashback, fontanella, botti).
2. **Muri del basso sul layer VistaLibera ignorati dalla camera** (e soffitto sopra la quota dell'anello alto): la camera usciva dalla stanza e vedeva il vuoto; WallFader nascondeva i muri ma la camera restava fuori.
3. **Porte (PorteVista) attraversabili** e **finestre del basso come varchi**: camera fuori dalla stanza.
4. **Collider mancanti**: Altarino del basso (mobile alto senza collider). Gli altri renderer senza collider (256 in tutto: fili, panni stesi, manifesti, insegne, tende, lampioni a muro, travi appese, macerie piatte, materassi e rotoli bassi) sono decorazioni sottili o basse che la camera può attraversare senza difetti visibili; sono esentate per nome o per dimensione (`VerificaCamera.PrefissiMorbidi`, `CameraGuard.ContaDimensioni`).

### Correzioni
- **Regola unica** documentata in testa a `CameraGuard.cs` e `WallFader.cs`: la camera non attraversa mai niente di solido (muri, soffitti, porte, edifici, props con un lato >= 0,6 m e alti >= 0,4 m o larghi >= 3 m); WallFader resta solo come ultima rete per i muri `VistaLibera` che restano fra camera e giocatore.
- **`CameraGuard`** (nuovo, estensione Cinemachine dopo il Deoccluder): raycast sottile e spherecast dal punto mirato, avvicinamento immediato e rilascio morbido (5 m/s), in vista stretta alza l'inclinazione fino a 84 gradi ("da sopra la spalla"), quota minima dai piedi e dal suolo sotto la camera (0,45 m / 0,3 m), distanza minima 0,12 m, verifica finale "non dentro nulla"; il giocatore si nasconde (solo ombra, con isteresi 0,6/0,8 m) quando la camera è troppo vicina. Nota: non è un vero dithering ma un nascondi/mostra (il materiale del giocatore non ha un dissolvenza a soglia).
- **Deoccluder**: `CollideAgainst` = tutto (VistaLibera e PorteVista compresi), raggio 0,3, strategia PullCameraForward, smorzamento 0,4 / 0,1 in occlusione (invariati).
- **Profili camera**: nuovo `CameraProfileVolume` (volumi BoxCollider trigger per stanza, transizione morbida di 0,5 s su scala dell'orbita, FOV e limiti dell'inclinazione). Nel basso: stanza principale, alcova, cucina (orbita x0,8, FOV 62), cortile (x0,9, FOV 58). Orbita dell'adulto nel basso con anello alto SOTTO il soffitto (1,2 m sopra il punto mirato), FOV 58 (era 55), inclinazione limitata a -0,7..1 (era -1..1).
- **Generatori**: collider su `Altarino`; `VetroFinestraCamera` (collider figlio, solo per la camera: il davanzale è a 1,1 m) in ogni finestra delle pareti interne; `CreaVolumiCamera` nel basso; `BuildCameraRig` con la nuova configurazione. Scene rigenerate da `Treno ▸ Costruisci tutte le scene`.
- **Near/far**: near 0,12 m (`CameraRig.NearClip`, prima 0,1); far = 3 / densità della nebbia esponenziale, tra 40 e 150 m, impostato a runtime da `CameraRig` (con le nebbie attuali resta 150 m; la scelta è pronta per R017 / nebbie future).
- **Strumenti**: `Treno ▸ Verifica ▸ Camera` (prova automatica in Play: griglia di punti calpestabili, 12 direzioni x 3 inclinazioni, ManualUpdate del Brain a regime; si lancia anche con `VerificaCamera.Avvia("etichetta")`) e `Treno ▸ Verifica ▸ Camera - renderer senza collider`. Per le foto: `VerificaCamera.Avvia("etichetta", foto: true, emulaPrima: true|false)`; la prova imposta `Application.runInBackground` (senza, con l'editor in secondo piano il Play non avanza).

### Numeri prima / dopo (stessi criteri, 3 scene)
| | Pose | Dentro geometria | Sul vuoto | Sotto il suolo | Ostacolo camera-giocatore | Camera < 0,45 m | Errori |
|---|---|---|---|---|---|---|---|
| Prima | 9.972 | 1.850 | 423 | 0 | 1.496 | 0 | 3.769 |
| Dopo | 9.900 | 0 | 0 | 0 | 0 | 2 (basso, solo conteggio) | 0 |

Renderer senza collider (esclusi decorazioni sottili o basse): 3 prima (Altarino, MaterassoTerra, RotoliStoffa: gli ultimi due sono bassi e non contano), 0 dopo. Raggiungibilità (R006): 0 problemi. Superfici e buchi (R017): 0 buchi, 0 bordi aperti, complanari visibili 8 (come prima).

### Foto
`Docs/richieste/media/R018/prima/` e `dopo/`: per i 3 punti piu' stretti di hub e basso, 6 direzioni a 60 gradi (1278x480 JPG). "Prima" e' ricostruito dalle scene nuove disattivando CameraGuard, profili per stanza, vetri delle finestre e le collisioni di muri e porte (orbita e FOV del basso di allora). Rapporti: `rapporto_prima.txt`, `rapporto_dopo.txt`, `collider_mancanti_prima.txt`, `collider_mancanti_dopo.txt`.

### Bake e LFS
Scene rigenerate (3 volte durante il lavoro, i dati luce si scollegano) e **un solo bake** a fine lavoro (`Treno ▸ Look ▸ Bake luci di tutte le scene`, 146 s): circa 20 MB di EXR e LightingData nuovi in LFS (totale stimato ~665 MB su 1 GB).

### Limiti noti / da guardare
- Il basso e' buio (lampade a petrolio): con la camera sotto il soffitto le foto sono scure come il gioco vero.
- In un angolo la camera puo' andare quasi a picco sul giocatore: e' la vista "sopra la spalla" richiesta, ma e' un'inquadratura insolita.
- Scatti in movimento non misurati (la prova guarda la posa a regime). FPS non misurati.
