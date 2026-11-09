# R025 – Filmato iniziale della nuova partita (doppiato dall'utente)

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity (+ Blender MCP per scene e pose) |
| **Base** | `main` aggiornato. **Il codice attuale vince**. Leggere `Docs/TRAMA.md` prima di scrivere una riga |
| **Dipende da** | R024. Usa Timeline + Cinemachine 3 (sotto-fase 2.6), R019 (minimappa) e R023 (menu) |
| **Priorità** | **Molto alta**: per l'utente è molto importante, "deve essere bellissimo"; si può impiegare tutto il tempo che serve |

## 1. Cosa ha chiesto l'utente (parole sue)
> Quando si crea una nuova partita, come in tutti i giochi, ci deve essere un filmato dove ti spiega tutto: cosa fare, dove sei,
> la storia, e tutto questo doppiato da me. Ci puoi mettere anche un'ora, ma fallo. È molto importante e deve essere bellissimo.

## 2. Obiettivo
Con **Nuova partita** parte un filmato cinematografico in-engine (non un video preregistrato: pesa poco e funziona anche su
WebGL) che porta il giocatore dentro la storia e gli insegna a giocare, senza sembrare un tutorial.

## 3. Specifica
1. **Contenuto** (circa 3–5 minuti, in 5–7 capitoli):
   - *Dove sei*: Napoli, il presente del gioco (Amerigo adulto torna per la morte della madre Antonietta), i Quartieri, il basso.
   - *La storia*: flashback in seppia del 1946, il dopoguerra, la miseria, i treni dei bambini, la solidarietà
     (fedeltà a `TRAMA.md`; dialoghi **originali**, mai frasi del libro o del film).
   - *Cosa fare*: ritrovare gli oggetti di Antonietta; ogni oggetto apre un ricordo con enigmi; il quaderno, l'inventario (latta),
     la minimappa con la X dell'obiettivo, il pulsante Indizio sempre presente, l'errore che non punisce.
   - *Il tema civico*: perché il gioco parla di diritti, Costituzione, solidarietà (secondaria di 2° grado: tono serio, mai infantile).
   - Chiusura che sfuma dentro l'hub: controllo al giocatore e primo obiettivo sulla minimappa.
2. **Regia**: Timeline + Cinemachine 3, camere lente e motivate, luce d'epoca, look neorealista (ricordi in seppia come R007),
   tema del violino (R002), sottotitoli sempre attivi, dissolvenze. Personaggi del gioco esistenti (Amerigo adulto, Amerigo bambino,
   Antonietta se disponibile); niente sembianze degli attori del film.
3. **Controlli**: i tasti si mostrano per il dispositivo in uso (tastiera e mouse, gamepad, touch) come piccole targhette d'epoca,
   non come pannelli moderni.
4. **Doppiaggio**: ogni battuta ha un ID `cs_intro_NNN`, personaggio (Narratore, Amerigo adulto…), testo ed emozione, in
   `Resources/Data/cutscene_intro.json`, e finisce nel copione CSV. Senza audio il filmato funziona lo stesso (testo a tempo
   di lettura, avanza con click/E/Spazio); con audio il testo segue la clip. La lista dei file da registrare la consegna R028 a fine coda.
5. **Salta e rivedi**: tieni premuto per saltare, voce "Rivedi il filmato iniziale" in Pausa o negli Extra del menu.
   Parte da solo solo alla prima Nuova partita.
6. **Leggerezza**: niente video, niente bake extra; ≥60 FPS su Alta, fluido su Bassa e WebGL.
7. Testi solo in JSON. Prova automatica: il filmato parte, finisce, si salta e lascia il giocatore nell'hub col primo obiettivo.

## 4. Ordine di lavoro
1. Copione in italiano, controllato su `TRAMA.md` → **mostrarlo all'utente** prima della regia.
2. Storyboard con screenshot. 3. Timeline. 4. Prova completa + GIF.

## 5. Criteri di accettazione
- [ ] Copione approvato dall'utente: **mostrato all'utente, in attesa di eventuali correzioni**.
- [ ] Filmato completo alla prima Nuova partita, saltabile e rivedibile, in Windows e WebGL: **Windows verificato** (prova automatica su build Windows di sviluppo, 56 controlli su 56); **WebGL in attesa** (build non ancora fatta, come da incarico).
- [x] Battute con ID `cs_intro_NNN` nel CSV del doppiaggio: 41 righe `cs_intro_001..041` (verificato con Treno > Doppiaggio > Esporta copione CSV; `amerigo` risulta "Amerigo bambino", `amerigo_adulto` "Amerigo adulto").
- [x] GIF/screenshot in `Docs/richieste/media/R025/` (23 fotogrammi di storyboard 1280x720 JPG, almeno uno per capitolo, e un'animazione WebP da 80 KB); console pulita.

## Rapporto
### Cosa e' stato fatto
Filmato in-engine di circa 5 minuti e mezzo (354 s misurati senza aiuto, copione 298 s piu' dissolvenze e pause), 7 capitoli, 41 battute del copione `cutscene_intro.json` (testi invariati rispetto alla fase 1: nessun errore evidente trovato), usando le scene vere: `10_Hub_Vicolo` per il presente con pioggia e `30_FB_Scarpe` per il 1946 con il look del ricordo (R007).
Parte da solo con **Nuova partita**, "tieni premuto per saltare", voce **Rivedi il filmato iniziale** in Pausa, sottotitoli sempre visibili, targhette di carta d'epoca per i controlli (tastiera+mouse, gamepad, touch secondo l'ultimo dispositivo usato), alla fine la camera di regia si fonde nella camera di gioco, Amerigo e' nello slargo del basso e sulla minimappa compare la X di `ob_bucato` con la riga dell'obiettivo (forzata per 30 s anche in aiuto Normale).

### Architettura
- `Scripts/Dialogue/FilmatoIniziale.cs`: regista a capitoli guidato dai JSON. `cutscene_intro.json` (copione, ID doppiaggio, `capitoli[]`, `durata`, `controllo`) e `filmato_intro.json` (regia: per capitolo scena, stile di transizione, musica, tinta; per inquadratura camera da/a, attore, inserti; `controlli`, `carte`, `finale`, `testi`). Nessun testo narrativo nel codice.
- Camera: una `CinemachineCamera` di regia (Unity.Cinemachine, priorita' 500) pilotata da script, con il Brain a stacco netto durante il filmato; all'ultima inquadratura (`consegna`) si riabilita il blend EaseInOut 1,8 s verso la `CM_Amerigo` di gioco.
- Scene: le 5 sezioni (hub, ricordo, hub, ricordo, hub) si caricano con `SceneLoader.Load(..., preRivela)` (parametro nuovo: coroutine eseguita a scena pronta e schermo ancora coperto), quindi con la transizione "ricordo" a pellicola gia' esistente. Nessuna scena dedicata, nessun caricamento additivo, nessun bake nuovo.
- Attore: il giocatore stesso (Amerigo adulto nell'hub, bambino nel ricordo), spostato e animato (parametro Speed dell'Animator) lungo percorsi del JSON mentre `ThirdPersonController`, `PlayerInteractor` e `CharacterController` sono spenti.
- Inserti: `FilmatoStudio` (palco buio a y=3300, due luci proprie, camera solo per l'inserto, come l'EsaminaView) costruito con le stesse `parti` degli enigmi (primitive e modelli veri di `Resources/Modelli/Enigmi`); fogli d'epoca e pianta (`Resources/Mappe/10_Hub_Vicolo.jpg` con la croce di `ob_bucato` disegnata) come UI sopra la scena.
- Salvataggio: `SaveData.filmatoVisto`. `Obiettivi.Forza(secondi)` per la X forzata. `UIRoot`, `GameManager`, `CursorManager`, `EscapeRoomController` sanno che il filmato e' in corso (HUD nascosta, pausa e menu chiusi, niente messaggio d'introduzione del ricordo).
- Strumenti: `Treno > Filmato > Storyboard` (foto delle inquadrature di mondo senza entrare in Play), `ArtSource/Copioni/genera_filmato.py` (rigenera `filmato_intro.json`), `FilmatoSelfTest` (prova automatica).

### Prova automatica (build Windows di sviluppo, 1280x720, preset Alta, RTX 4060 Ti)
`TrenoBambini.exe -introtest [-introtestnaturale] [-introtestfoto] [-introtestout cartella]`. Esito: **56 PASS, 0 FAIL** con un tocco a ogni battuta (log in `media/R025/prova_automatica.log`); la corsa a tempo di lettura (`-introtestnaturale`) ha dato gli stessi esiti tranne un difetto del salto durante un cambio scena, poi corretto e riverificato nella corsa con i tocchi. Verifica: parte alla Nuova partita; tutte le 41 battute e i 7 capitoli; targhette `salta, muovi, guarda, interagisci, inventario, mappa, indizio`; HUD nascosta e pausa non apribile durante il filmato; arriva alla fine; stato finale (hub, input libero, Amerigo nello slargo, primo obiettivo `ob_bucato`, X forzata visibile, minimappa con riga dell'obiettivo, salvataggio); un tocco di Esc non salta ma tenuto premuto si'; stesso stato finale dopo il salto; Continua non rifa' partire il filmato; la Pausa ha "Rivedi il filmato iniziale", lo fa partire e a fine/salto Amerigo torna esattamente dov'era; tasti diversi per tastiera / gamepad / touch.
**FPS** (corsa a tempo di lettura, vsync spento, `media/R025/fps_prova_naturale.csv`): media 241 fps, 1% low 73 fps, picco di 110 ms durante i caricamenti; per capitolo medie da 200 a 292 fps. Obiettivo >= 60 FPS su Alta rispettato. Bassa e WebGL non misurati.

### Decisioni prese da Claude
1. **Architettura**: regista a capitoli guidato dai JSON invece di asset Timeline generati: piu' robusto, rigenerabile da script, leggero in WebGL. Cinemachine 3 resta la camera (regia e consegna al gioco).
2. **Scene vere** in sequenza con `SceneLoader` (4 cambi scena, coperti dalla transizione esistente), non una scena 05_Intro ne' scene additive.
3. **Personaggi**: Amerigo adulto e bambino in 3D; Antonietta, Tommasino e Maddalena non hanno modelli, quindi parlano con il ritratto in cornice d'ottone (inquadrature che li evitano). La custodia del violino della regia non esiste come modello: non mostrata.
4. **Si avvia a ogni Nuova partita** (nuovo salvataggio), mai con Continua; "Rivedi" solo da pausa nell'hub o a casa, non dentro un ricordo (toast che lo spiega).
5. **Salta**: Esc, Spazio, E, Invio, clic, tocco, A/B/Start del gamepad tenuti 1,2 s (un tocco breve fa avanzare la battuta). La targhetta del salto compare a ogni pressione e nei primi secondi.
6. **Dispositivo dei tasti** = ultimo ingresso ricevuto (tastiera/mouse, gamepad, touch); per il gamepad la latta e il quaderno dicono "Pulsante Latta" (non ha una scorciatoia).
7. **Finale**: Amerigo e' lasciato a (-2,2; -2,0) nello slargo del basso a 3,8 m dalla X (fuori dai 2,2 m in cui si spegne) e non allo spawn "inizio" (Largo della Fontanella): il salvataggio resta con `spawn = inizio`.
8. **Azioni del copione** (`dissolvenza`, `zoom`, `entra_sx/dx`): le dissolvenze sono scelte dal regista a ogni capitolo/inquadratura, lo zoom e' nel movimento della camera, `scuoti` diventa una breve scossa di camera su cs_intro_016, `entra_*` fa comparire il ritratto.
9. **Inserti**: studio 3D per cucina, binari, cartellini, toppa, scarpe, oggetti degli enigmi, latta e quaderno, lucchetto, tavola apparecchiata; fogli UI per scheda del 2 giugno 1946 e articoli 3, 2, 31, 34 della Costituzione (testo ufficiale, in `filmato_intro.json`); pianta con X a matita. Nomi sui cartellini inventati (Carmela, Gennaro, Rosetta, Peppino).
10. **Ritratto di Amerigo adulto**: illustrazione piatta di ripiego (stile Fase 1) in `Resources/Sprites/ritratti/amerigo_adulto.png`, usata solo dalla vecchia cutscene a ritratti; nel filmato Amerigo adulto e' in 3D.
11. **CSV doppiaggio**: campo `nomeCopione` in `personaggi.json` ("Amerigo bambino", "Amerigo adulto").
12. Musica: `tema_amerigo_solo`/`_ricordo`/`_scoperta` gia' presenti (sintetizzati o registrati secondo AudioLibrary) cambiati per inquadratura; silenzio sulle battute della pioggia e sulla 021.

### Problemi aperti
- **WebGL non verificato** (nessuna build, come da incarico): scene e inserti sono quelli gia' in WebGL, ma il filmato non e' stato provato nel browser; i cambi scena possono durare di piu'.
- Durata ~5:30 contro i 3-5 minuti della specifica: il copione approvato da solo vale 298 s; si puo' accorciare togliendo pre-roll (`pausaSenzaTesto`) o battute.
- Nell'hub con la pioggia compaiono strisce bianche verticali (la pioggia dell'R007 vista da vicino) in alcune inquadrature ravvicinate: non toccato `MeteoController`.
- Senza voci registrate il testo avanza a tempo di lettura: con le voci (R028) i tempi vanno riverificati.
- La prova usa `GameManager.NewGame()` (stessa funzione del pulsante), non il clic sul titolo.
- Dev build: screenshot con la scritta "Development Build" in basso a destra coperta in nero.

