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
- [ ] Filmato completo alla prima Nuova partita, saltabile e rivedibile, in Windows e WebGL: **Windows verificato** (prova automatica su build Windows di sviluppo: 65 controlli su 65 a tempo di lettura, 64 su 64 con i tocchi); **WebGL in attesa** (build non ancora fatta, come da incarico).
- [x] Battute con ID `cs_intro_NNN` nel CSV del doppiaggio: 36 righe `cs_intro_001..036` (verificato con Treno > Doppiaggio > Esporta copione CSV; `amerigo` risulta "Amerigo bambino", `amerigo_adulto` "Amerigo adulto").
- [x] GIF/screenshot in `Docs/richieste/media/R025/` (23 fotogrammi di storyboard 1280x720 JPG, almeno tre per capitolo, e un'animazione WebP da 80 KB); console pulita.

## Rapporto
### Cosa e' stato fatto
Filmato in-engine di **4 minuti e 38 secondi** misurati a tempo di lettura (278 s dalla Nuova partita alla fine, con i due cambi scena e le dissolvenze; 269 s con i tocchi), 7 capitoli, **36 battute** `cs_intro_001..036` del copione `cutscene_intro.json`, sulle scene vere: `10_Hub_Vicolo` (presente con pioggia; il capitolo 6 nello stesso hub col look del ricordo, senza pioggia) e `30_FB_Scarpe` (1946, capitoli 2 e 3).
Parte da solo con **Nuova partita** (e con **Continua** se il salvataggio non ha filmato visto ne' progressi); "tieni premuto per saltare"; **Rivedi il filmato iniziale** in Pausa e nel menu principale; sottotitoli sempre visibili; targhette dei controlli in carta d'epoca per tastiera+mouse / gamepad / touch; alla fine la camera di regia si fonde nella camera di gioco, Amerigo e' nello slargo del basso e sulla minimappa compare la X di `ob_bucato` con la riga dell'obiettivo (forzata 30 s anche in aiuto Normale).

### Architettura
- `Scripts/Dialogue/FilmatoIniziale.cs`: regista a capitoli guidato dai JSON. `cutscene_intro.json` (copione, ID doppiaggio, `capitoli[]`, `durata`, `controllo`) e `filmato_intro.json` (regia: scena, transizione, look, musica, tinta per capitolo; camera da/a, attore, inserti per inquadratura; `controlli`, `carte`, `finale`, `testi`), entrambi generati da `ArtSource/Copioni/` (`genera_intro.py`, `genera_filmato.py`). Nessun testo narrativo nel codice. Schema in CLAUDE.md.
- Camera: `CinemachineCamera` di regia (Unity.Cinemachine, priorita' 500) pilotata da script, Brain a stacco netto; all'ultima inquadratura (`consegna`) blend EaseInOut 1,8 s verso la `CM_Amerigo` di gioco.
- Scene: `SceneLoader.Load(..., preRivela)` (coroutine eseguita a mano con try/catch: se la preparazione va in errore la transizione si chiude comunque, mai `IsLoading` bloccato). Solo **due** caricamenti di scena in tutto il filmato (hub, ricordo, hub), coperti dalla transizione "ricordo".
- Attore: il giocatore stesso (adulto nell'hub, bambino nel ricordo), spostato lungo percorsi del JSON con Animator pilotato (`Suolo` con `RaycastNonAlloc`, buffer statico).
- Inserti: `FilmatoStudio` (studio buio a y=3300, luci proprie, camera solo per l'inserto, tinta e nebbia spente mentre dura) con le `parti` degli enigmi (modelli veri di `Resources/Modelli/Enigmi`: scarpe, lucchetto, calendario, cornice, latta, panno a quadri) su un tavolo scuro; fogli d'epoca UI (scheda del 2 giugno 1946, art. 3 secondo comma, articoli 2/31/34, numeri dei treni dei bambini) e pianta con la croce; mentre si nomina la minimappa (cs_intro_024-025) compare la **minimappa vera** in alto a sinistra (`UIRoot.MostraSoloMinimappa`).
- Guardiano: se il regista non fa un passo per 10 s (fuori dai caricamenti) il filmato si chiude da solo (`ForzaChiusura`: scena, camera, input, HUD tornano al giocatore). `GameManager.FilmatoDisponibile()` controlla che ogni capitolo abbia scena, inquadrature e battute. `GameManager.OnSceneEntered` non scrive nel salvataggio mentre il filmato e' in corso.
- Pioggia: Camera Fading di URP su `M_Pioggia` (near 0,8 m, far 4 m; menu Treno > Meteo > Camera Fading sulla pioggia, e `MeteoBuilder`), vale anche in gioco. `MeteoController.Sospendi` spegne la pioggia nel capitolo 6.
- Manifesto "TRENI PER IL NORD" nel ricordo: `testi_ambiente.json` (gruppo ricordo), costruttore del ricordo aggiornato e menu **Treno > Filmato > Aggiungi manifesto Treni al ricordo** (incrementale: la ricostruzione completa avrebbe perso la luce baked).

### Correzioni della review
- A: `OnSceneEntered` sotto filmato; guardiano e `FilmatoDisponibile`; `preRivela` in try/catch; `PreparatoOk=false` prima di Load e `if (skip) yield break`; niente telegramma dei comandi dopo il filmato; niente suono macchina da scrivere; `Suolo` senza allocazioni e senza `SyncTransforms` a ogni frame; Continua senza filmato visto e senza progressi lo avvia; voce "Rivedi il filmato iniziale" anche nel menu principale (`ui_menu.json`, tolto il doppione in `filmato_intro.json`).
- B: strisce di pioggia (B1); inquadrature rifatte: occhi sopra la banda (005), **piedi nudi e sporchi in cammino a ~0,3 m che poi salgono al viso** (007), mano sulla porta (019), panni stesi con il tele (035), manifesto (012); niente ritratti (`conRitratto` vuoto): voce fuori campo con il nome in ottone davanti al testo su dettagli (mani che rammendano, soglia, ombre, panni); **sottotitoli in una striscia sottile nella banda nera in basso, massimo 2 righe, senza riquadro di carta**; inserti UI rimpiccioliti e alzati; targhetta controlli larga quanto il contenuto; art. 3 solo il comma 2; studio con fondale scuro e tavolo, tinta e nebbia spente, via stufa/tavola/binari, Indizio fuori dal lucchetto; capitolo 3 con velo grigio al massimo 0x30 (curva sulle battute); minimappa vera (B7).
- D: pause senza testo 2/1/1/1/1/1/2 s; stacco netto di default (dissolvenze solo tra luoghi, sovrapposte all'inizio battuta); capitolo 6 nell'hub senza caricare il ricordo; copione: tolte 005 e 027, unite 013+014, 019+020, 035+036, ID rinumerati senza buchi; scheda del 2 giugno con "per la prima volta in un voto nazionale".
- C: CLAUDE.md (Schema JSON) aggiornato con `filmato_intro.json`, campi extra di cutscene, `nomeCopione`, `filmatoVisto`.

### Prova automatica (build Windows di sviluppo, 1280x720, preset Alta, RTX 4060 Ti)
`TrenoBambini.exe -introtest [-introtestnaturale] [-introtestfoto] [-introtestout cartella]`. **65 PASS, 0 FAIL a tempo di lettura** (`media/R025/prova_automatica_naturale.txt`) e **64 PASS, 0 FAIL con un tocco a ogni battuta** (`prova_automatica_tocchi.txt`). Verifica: partenza alla Nuova partita; 36 battute e 7 capitoli; targhette `salta, muovi, muovi_guarda, interagisci, inventario, mappa, indizio`; HUD nascosta (tranne la minimappa vera) e pausa non apribile; durata <= 5:00 a tempo di lettura; stato finale (hub, input libero, Amerigo nello slargo, `ob_bucato`, X forzata, minimappa con riga, salvataggio); tocco breve di Esc non salta, tenuto si'; Continua non rifa' partire il filmato; Rivedi dalla Pausa nell'hub, **dal basso 20_Int con salto durante il Rivedi** (si torna nel basso, al punto di prima, salvataggio nel basso) e **dal menu principale** (si torna al titolo); guardiano (regista bloccato: chiusura forzata e gioco libero); tasti per tastiera / gamepad / touch.
**FPS** (Alta, vsync spento, `fps_prova_*.csv`): corsa a tempo di lettura media 723 fps, 1% low 203 fps; con i tocchi media 387 fps, 1% low 76 fps; la corsa della prima versione, a carico simile, aveva dato 241 e 73: il rapporto varia col carico della macchina. Picco di frame 18-80 ms (caricamenti). Obiettivo >= 60 FPS su Alta rispettato. Bassa e WebGL non misurati.

### Decisioni prese da Claude
1. Regista a capitoli guidato dai JSON, Cinemachine per la camera (non asset Timeline): robusto, rigenerabile, leggero per WebGL.
2. Scene vere in sequenza con `SceneLoader`; il capitolo 6 resta nell'hub col look del ricordo (vignetta e grana del Fader, seppia, pioggia sospesa) per risparmiare un caricamento.
3. Antonietta, Tommasino e Maddalena non hanno modelli e i ritratti a cartone stonano: voce fuori campo con il nome davanti al testo, su dettagli. La custodia del violino non e' mostrata.
4. Si avvia a ogni Nuova partita e con Continua se `!filmatoVisto` e nessun progresso; "Rivedi" da Pausa (non dentro un ricordo) e dal menu principale.
5. Salta: Esc, Spazio, E, Invio, clic, tocco, A/B/Start tenuti 1,2 s; un tocco breve avanza la battuta.
6. Dispositivo dei tasti = ultimo ingresso (tastiera/mouse, gamepad, touch); nuova targhetta `muovi_guarda` sull'ultima battuta.
7. Finale a (-2,2; -2,0) nello slargo del basso, 3,8 m dalla X; salvataggio con `spawn = inizio`.
8. Azioni del copione: `dissolvenza`/`zoom`/`entra_*` non pilotano piu' nulla (le sceglie il regista); `scuoti` = scossa di camera su cs_intro_014.
9. Manifesto Treni aggiunto in modo incrementale (menu dedicato) e nel costruttore del ricordo, per non perdere la luce baked: il bake e le scene non sono stati rifatti; il manifesto non e' nella luce baked (usa le probe).
10. Camera Fading: valori 0,8-4 m scelti guardando le inquadrature (5, 19, 34-36); nelle scene con pioggia la sfumatura vale anche in gioco.
11. Il numero di fps varia molto col carico (241-723 sulla stessa macchina): si riporta l'intervallo.
12. Ritratto di Amerigo adulto: illustrazione di ripiego, usata solo dalla vecchia cutscene a ritratti.

### Problemi aperti
- **WebGL non verificato** (nessuna build, come da incarico).
- Senza voci registrate il testo avanza a tempo di lettura: con le voci (R028) i tempi vanno riverificati.
- Resta qualche sottile striscia di pioggia nelle inquadrature ravvicinate dell'hub (molto ridotte); il manifesto Treni e' fuori dal bake.
- La prova usa `GameManager.NewGame()` (la stessa funzione del pulsante), non il clic sul titolo.
- Screenshot dalla build di sviluppo: scritte "Development Build" e contatore FPS coperti in nero.
