# Diario di sviluppo

Memoria condivisa del progetto tra i vari PC. Le voci più recenti sono in alto.
Regole d'uso: vedi la sezione "Diario e lavoro su più PC" in `CLAUDE.md`.

---

## 2026-11-06 – PC casa – Nuove richieste R006–R010, chiusa R003
**Branch:** feature/upgrade-realistico · **Ultimo commit:** fdeaedc
### Fatto
- Chiusa la PR #3 (R003, NPC) su richiesta dell'utente.
- Aperte 5 richieste in bozza verso `feature/upgrade-realistico`: R006 mappe grandi + compenetrazioni (#8),
  R007 grafica realistica con pioggia (#9), R008 impostazioni grafiche dettagliate (#10),
  R009 enigmi dentro il mondo senza oggetti viola (#11), R010 ridisegno enigmi "Le scarpe" (#12).
- Aperte altre 6 richieste: R011 easter egg e segreti in ogni stanza (#13); capitoli nuovi, uno per PR:
  R012 Stracci (#14, contiene le regole comuni), R013 Volantino (#15), R014 Cappotto (#16), R015 Violino (#17), R016 Finale 1994 (#18).
### Decisioni
- L'utente dà l'ok a sviluppare tutti gli altri oggetti/capitoli (annotato in CLAUDE.md).
- Ordine: R006 → R007 → R008, poi R009 → R010 → R011, poi i capitoli R012 → R016. R010 va approvata dall'utente (schema della catena) prima di svilupparla.
### Problemi aperti
- Bug del movimento nel gioco online ancora da confermare dall'utente.
### Prossimi passi
- Implementare R006 per prima.

## 2026-11-06 – PC casa – PR #7, #6, #5 (R005) e #4 (R004) integrate, gioco online aggiornato
**Branch:** feature/upgrade-realistico · **Ultimo commit:** 194ca23 (+ questa voce) · **gh-pages:** d03141e

### Fatto
- **PR #7** `feature/amerigo-makehuman` mergiata in `feature/upgrade-realistico` (Amerigo MakeHuman in gioco, flashback "Le scarpe" con Amerigo scalzo).
- **PR #6 Crediti autori:** finestra Crediti controllata a 1280×720 e 1024×576, nessun taglio. Mergiata.
- **PR #5 / R005 tastiera e cursore:** I/Tab inventario, Esc/P pausa, comandi a inizio partita e in pausa, cursore bloccato con
  visuale col mouse (`CursorManager.cs`, `Assets/Plugins/WebGL/TrenoInput.jslib`), pointer lock richiesto nel `mousedown` del
  canvas, pausa alla perdita del blocco, opzione "Visuale: col tasto destro" salvata, dopo 2 rifiuti del browser il cartellino lo
  suggerisce. Clic sinistro con cursore bloccato: nessuna azione (decisione dell'utente). Joystick a schermo solo in touch o col
  tasto destro. Gamepad ignorato finché non viene usato (ipotesi sul bug "Amerigo fermo").
- **PR #4 / R004 corsa:** clip Mixamo "Slow Run" (In Place) al posto di "Jogging", stesso file e GUID. Moltiplicatore adulto 0,94×
  (3,2 m/s), bambino 1,29× con corsa a 2,2 m/s (decisione dell'utente). Passi audio dal passo della clip. `CREDITS.md` aggiornato.
- Review prima del merge: trovato che la rigenerazione delle scene di R004 scollegava i LightingData → bake rifatto delle 4 scene
  (01, 10, 20, 30), stessi file, ~11 MB LFS.
- Build WebGL (40,4 MB) pubblicata su gh-pages.

### Decisioni
- Merge dei branch richiesta con merge (non rebase/force push).
- Clic sinistro col cursore bloccato non interagisce; joystick nascosto su PC; corsa del bambino 2,2 m/s.

### Problemi aperti
- **Pointer lock vero non verificato**: nel Chrome automatizzato il browser lo rifiuta sempre (WrongDocumentError). Da provare
  dall'utente in un browser normale (anche Firefox).
- **Amerigo fermo online**: non riprodotto su questo PC; se capita ancora: F3 + W premuto, screenshot (righe Gamepad).
- LIM che fa da mouse: col cursore bloccato si esce solo con Esc da tastiera; su LIM usare "Visuale: col tasto destro".
- Il testo lungo del cartellino "Clicca per giocare" (dopo i rifiuti) esce un po' dal bordo.
- La corsa del bambino non è stata guardata a occhio in Play.
- MCP for Unity cade durante refresh/compilazioni lunghe: riavviare il server e fare Reconnect; tenere Unity in primo piano.
- R003 (NPC di Napoli) ancora aperta in bozza.
- Restano i punti aperti della voce precedente (peso modelli MH, copione doppiaggio da rigenerare, chi dà le scarpe ad Amerigo).

### Prossimi passi
- Prova dell'utente sul gioco online: blocco del cursore, comandi, corsa di adulto e bambino.
- Ritocco del cartellino; poi sotto-fase 2.6 (cutscene) o R003 su ok dell'utente.

---

## 2026-11-06 – PC casa – Amerigo MakeHuman in gioco, flashback "Le scarpe" con Amerigo scalzo
**Branch:** feature/amerigo-makehuman · **Ultimo commit:** vedi `git log` (voce scritta prima del commit finale)

### Fatto
- **Generatore MakeHuman portabile:** trova MakeHuman 1.3 (Program Files) o 1.2 (AppData) e la cartella del repo sui vari PC.
  Lanciatore `ArtSource/Characters/makehuman/1_prepara_materiali.bat`. Dopo l'export FBX, `pulisci_percorsi_fbx.py` toglie dai
  file i percorsi con il nome utente del PC. L'utente ha rigenerato i due Amerigo sul PC di casa.
- **Import in Unity:** menu **Treno ▸ Personaggi ▸ Importa Amerigo MakeHuman** (`PersonaggiMakeHumanBuilder.cs`): Humanoid con
  mappa esplicita delle ossa `game_engine` (52 ossa), materiali URP, occhi con cornea ritagliata, LOD, prefab
  `AmerigoMH_Bambino` e `AmerigoMH_Adulto` in `Assets/Art/Characters/AmerigoMH_*`.
- **Sostituzione in gioco (scelta dell'utente):** bambino MH (1,0 m, scalzo) nei flashback, adulto MH (1,75 m, così com'è) nel
  vicolo e nel basso. Flag `UsaAmerigoMakeHuman` in `PersonaggiBuilder.cs`: v5/v6 restano come riserva. Capsula del bambino
  1,0 m, punto camera 0,85 m, orbita ridotta; velocità delle clip misurate sui nuovi avatar; suole a terra. Scene ricostruite e
  luci ricalcolate. Screenshot: `ArtSource/Characters/screenshots/mh_confronto_*` e `mh_ingioco_*`.
- **Flashback "Le scarpe" riscritto (approvato dall'utente):** Amerigo è scalzo, il ricordo è il desiderio di avere scarpe.
  Aggiornati `TRAMA.md` (decisione 12: scostamento voluto dal libro, dove le scarpe sono rotte/strette), `oggetti.json`,
  `flashback_scarpe.json` (stessi 4 enigmi, testi ritoccati), `cutscene_scarpe.json` (13 battute su 19 riscritte, ID invariati).
- **Privacy:** tolto il nome utente del PC dai file attuali (FBX dei quattro Amerigo, metadati di 12 screenshot v5/v6).
  Scelta dell'utente: pulire d'ora in poi, senza riscrivere la storia (il nome resta nelle versioni vecchie già su GitHub).
  I due `.blend` v5/v6 sono compressi (zstd): non verificati, da controllare decomprimendoli.

### Decisioni
- Il bambino MH sostituisce v5, l'adulto MH sostituisce v6 (anche se dimostra meno dei 55 anni della trama).
- Amerigo bambino scalzo anche nel flashback "Le scarpe": trama riscritta.
- Nome utente nei file: pulizia solo dei file attuali.

### Problemi aperti
- La corsa del bambino gira a ~2,8× la velocità della clip: controllare a occhio in Play che non sembri frenetica.
- Modelli MH pesanti (~41-45k poligoni) e senza LOD veri (LOD1 toglie solo i denti): alleggerire in 2.8 per WebGL/PC scuola.
- Da decidere chi dà ad Amerigo il primo paio di scarpe (partenza o famiglia del Nord), prima della cutscene "Cappotto".
- Copione del doppiaggio da rigenerare (Treno ▸ Doppiaggio ▸ Esporta copione CSV).
- Ancora aperto dal mattino: **Amerigo fermo nel gioco online** (vedi voce del 6/11 notte), gioco online non ancora ripubblicato
  con i nuovi Amerigo.
- Merge di `feature/amerigo-makehuman` in `feature/upgrade-realistico` da fare con l'ok dell'utente.

### Prossimi passi
- Prova in Play del bambino (camminata e corsa) e dell'adulto; poi merge nel branch della Fase 2 e nuova build WebGL.
- Bug del movimento online (F3 + W premuto, screenshot).
- Richieste in bozza R004 (animazione di corsa) e R005 (tastiera e cursore), poi sotto-fase 2.6 (cutscene).

---

## 2026-11-06 – PC scuola – Nuovi Amerigo bambino e adulto da MakeHuman
**Branch:** feature/amerigo-makehuman (da `feature/upgrade-realistico`) · **Ultimo commit:** vedi `git log`

### Fatto
- Installato MakeHuman 1.2 sul PC della scuola. Scritti due script in `ArtSource/Characters/makehuman/`:
  `prepara_materiali_amerigo.py` (si lancia col Python di MakeHuman: ricolora capelli, tuta e pelle) e
  `genera_amerigo_makehuman.py` (Utilities ▸ Scripting ▸ Load ▸ Execute: crea i due Amerigo completi).
- Generati e salvati in git: `Amerigo_bambino/` e `Amerigo_adulto/` con `.mhm`, `.fbx` (scheletro `game_engine`),
  `textures/` e 7 espressioni (`espressioni/*.mhm`: felice, triste, sorpresa, decisa, pensierosa, arrabbiata, paura).
  - Bambino: ~1,0 m, molto magro, **scalzo**, pelle sporca su piedi/gambe/mani, salopette lisa con toppe e strappi, capelli castani (short02).
  - Adulto: ~55 anni, abito scuro con cravatta, scarpe nere, capelli castano scuro (short04, scelto dall'utente).
- Asset annotati in `CREDITS.md` (tutti CC0 di MakeHuman).

### Decisioni
- **Capelli di Amerigo solo castani** (niente più castano/ramato): aggiornati `CLAUDE.md`, `Docs/TRAMA.md`, `Docs/BACKLOG.md`.
- **Amerigo bambino poverissimo e scalzo** (aggiornato `CLAUDE.md`).

### Problemi aperti
- ⚠️ **Scalzo vs flashback "Le scarpe"**: TRAMA e cutscene parlano delle scarpe rotte/strette di Amerigo. Da decidere con
  l'utente: variante con scarpe rotte solo per quel flashback, oppure riscrivere cutscene e trama.
- Gli screenshot di MakeHuman erano sbagliati (cattura senza profondità: capelli sopra il viso). Corretto lo script
  (`canvas.grabFramebuffer()`), ma non ancora rieseguito: gli screenshot non sono nel commit. I modelli sono a posto
  (la miniatura salvata da MakeHuman mostra il viso corretto).
- Nel viewport di MakeHuman l'abito dell'adulto ha una macchia nera a quadretti sui fianchi: verificare in Unity.
- I modelli in gioco v5/v6 hanno ancora i capelli castano/ramato (vedi BACKLOG).

### Prossimi passi (PC casa, con Unity aperto e MCP attivo)
1. `git pull` e `git switch feature/amerigo-makehuman`.
2. Importare `ArtSource/Characters/makehuman/Amerigo_bambino/Amerigo_bambino.fbx` e `Amerigo_adulto/Amerigo_adulto.fbx`
   con le texture in `Assets/Art/Characters/AmerigoMH_Bambino/` e `AmerigoMH_Adulto/`: avatar Humanoid, materiali URP
   (capelli con alpha clipping), scala e LOD come i modelli v5/v6.
3. Provarli con le 8 animazioni Mixamo esistenti, screenshot con la luce del gioco accanto a v5/v6 e mandarli all'utente.
   Sostituire v5/v6 solo con il suo ok (e dopo la decisione sul flashback "Le scarpe").

---

## 2026-11-06 – PC scuola – Richiesta R004 (animazione di corsa)
**Branch:** `richiesta/R004-animazione-corsa` (da `feature/upgrade-realistico` @ 9426a9d)

### Fatto
- Scritta la richiesta **R004 – Animazione di corsa nuova (da Mixamo)** (PR in bozza verso `feature/upgrade-realistico`):
  l'attuale `Amerigo_Run.fbx` è "Jogging (In Place)" riprodotta a ~1,7× (adulto) e ~2,35× (bambino), da qui l'effetto brutto.
  Proposta: "Running" di Mixamo (In Place), taratura di `clipRunSpeed`, passi audio a tempo.
- Aggiornato l'elenco in `Docs/richieste/README.md` (R001 e R002 risultano implementate nel branch della Fase 2).

### Problemi aperti
- Il bug del movimento nel gioco online (voce precedente) va ancora provato con F3 da un PC con il browser: questo PC non ha Unity.

### Prossimi passi (PC casa)
- Prima il bug del movimento, poi R004 seguendo `Docs/richieste/README.md`, poi la sotto-fase 2.6.

---

## 2026-11-06 – PC casa – Bug del movimento nel gioco online, pulsante Interagisci, diagnosi F3
**Branch:** feature/upgrade-realistico · **Ultimo commit:** 8d46489 · **gh-pages:** 41101b7

### Fatto
- **Pulsante Interagisci:** su PC non c'è più (si usa E, con il cartellino "E …" vicino agli oggetti). Su touch compare solo
  vicino a un interagibile, dopo il primo tocco reale (`UIRoot.TouchMode`); un tasto della tastiera torna alla modalità PC.
- **Diagnosi del movimento:** finestra di diagnosi con **F3** (o F2) in basso a sinistra: scena, FPS, blocchi dell'input con il
  motivo, tasti W/A/S/D/frecce letti, joystick, gamepad, input del controller, velocità, a terra (`Scripts/Core/DiagOverlay.cs`).
- **Rete di sicurezza:** `GameManager` azzera un blocco dell'input rimasto attivo per oltre 1,5 s senza un proprietario
  (caricamento, dialogo/puzzle, pausa, inventario, cutscene, dissolvenza) e lo scrive in console.
- Zona morta 0,25 sul gamepad. Gioco online ripubblicato tre volte (ultima build 41101b7, dati 31,6 MB).

### Problemi aperti
- ⚠️ **Amerigo non si muove nel gioco online** sul PC di casa dell'utente (Chrome, Nuova partita, fermo del tutto anche col
  joystick a schermo). Causa NON trovata: in editor e nel browser di prova si muove; escluse root motion, timeScale, blocchi dopo
  Nuova partita, errori in console del browser. Il browser di prova andava a ~1 fps, quindi il test della tastiera tenuta premuta
  non è conclusivo. Ipotesi rimaste: tastiera che non arriva al canvas (focus), gamepad "fantasma" a fondo scala, FPS bassissimi.
- Il joystick a schermo compare anche su PC senza touch (in WebGL `Touchscreen.current` esiste sempre): valutare di mostrarlo
  solo in modalità touch.

### Prossimi passi (domani, PC scuola)
1. `git pull`, poi aprire il gioco online con Ctrl+F5 → Nuova partita → clic nell'area di gioco → provare a muoversi.
2. Se resta fermo: premere **F3**, tenere premuto W e fare uno screenshot. Leggere: W=1? blocchi con motivo? gamepad ≠ 0? FPS?
3. Correggere la causa, ripubblicare su gh-pages (l'utente ha autorizzato a ripubblicare senza chiedere).
4. Poi: prova d'ascolto dell'audio, controllo della camminata del bambino, sotto-fase 2.6 (cutscene).
- Nota: l'editor sul PC di casa è rimasto sulla piattaforma WebGL.

---

## 2026-11-05/06 – PC casa – Fase 2.5 personaggi chiusa, R001 e R002 integrate
**Branch:** feature/upgrade-realistico · **Ultimo commit:** vedi `git log` (voce scritta prima del commit finale)

### Fatto
- **Lavoro del PC di casa pubblicato:** il branch `feature/upgrade-realistico` (Fase 2 fino alla 2.5) è stato inviato a GitHub
  per la prima volta. Il lavoro rimasto su questo PC segnalato nella voce del 5/11 è ora sul remoto.
- **R001 (UI d'epoca) e R002 (audio) implementate**, revisionate, corrette e unite nel branch della Fase 2; provate in editor
  (console pulita). Audio: musica, ambienti ed effetti pesanti pre-generati come .ogg (~1 MB) dalla sintesi del gioco
  (`Tools/AudioGen`), sintesi da codice solo come ripiego e per gli effetti brevi.
- **Fase 2.5 approvata:** Amerigo bambino v5 (flashback) e Amerigo adulto v6 (segnaposto, nel vicolo e nel basso), capelli
  divisi castano/ramato verificati in gioco (ramato sul lato destro), 8 animazioni Mixamo (Idle, Idle2, Walk, Run, TurnL, TurnR,
  PickUp, Interact), velocità e animazioni sincronizzate per personaggio, menu riproducibili in Treno ▸ Personaggi.
- Ricerca di capelli/vestiti CC0: nessun asset migliore dei nostri; capelli a ciocche e vestiti generati da script Blender
  (`ArtSource/Characters/build_scripts_v4`, in git solo le versioni finali dei .blend).
- **Gioco online aggiornato** (su richiesta dell'utente): build WebGL della Fase 2 in corso pubblicata su `gh-pages` (commit a4dd3ca).
  Texture ridotte solo per WebGL (override in `WebGLBuilder.cs`): dati da 110 MB a 31,5 MB, totale ~42 MB. Editor rimasto su piattaforma WebGL.
- Bake delle luci spostato su Progressive CPU (il GPU mandava in crash l'editor): ~3,5 minuti per tutte le scene.

### Decisioni
- Domande all'utente sempre con il pannello di scelta.
- Musica pre-generata in file; il tema al violino resta sintetico (nessuna registrazione dal vivo).
- Nel presente (vicolo e basso di Antonietta) si gioca con Amerigo adulto, nei flashback con il bambino.
- Capelli dei due Amerigo: segnaposto approvati, rifinitura in `Docs/BACKLOG.md`.
- Velocità: adulto 1,4/3,2 m/s, bambino 1,1/2,7 m/s (camminata/corsa), regolabili.

### Problemi aperti
- Prova d'ascolto dell'audio (musica, ambienti, passi, ducking) da fare a mano; layout UI a 1280×720 da guardare.
- Picchi al caricamento delle scene (titolo ~450 ms, vicolo ~224 ms in editor) da misurare su una build.
- Animazioni del bambino a ~2,4× per stare al passo: verificare a occhio che non sembrino frenetiche.
- Il castano dei capelli dell'adulto in gioco è molto scuro (contrasto forte): eventualmente ammorbidire.
- `Assets/TextMesh Pro/.../LiberationSans SDF - Fallback.asset` modificato da Unity: non committato apposta.
- Le PR in bozza R001, R002, R003 su GitHub vanno aggiornate/chiuse: R001 e R002 ora sono nel branch della Fase 2.

### Prossimi passi
- Sotto-fase **2.6**: cutscene con Timeline + Cinemachine 3 (controllare prima `Docs/BACKLOG.md`).
- R003 (NPC di Napoli in Blender) dopo la 2.6, riusando la pipeline dei personaggi.
- Merge su `main` e tag v0.2 solo a fine Fase 2, con l'ok dell'utente.

---

## 2026-11-05 – PC scuola – Protocollo richieste, R001 (UI), R002 (audio), R003 (NPC)
**Branch:** main + `richiesta/R001-ui-spettacolare` + `richiesta/R002-audio-musica-violino` + `richiesta/R003-npc-napoli-blender`

### Fatto
- Creato il protocollo per passare idee tra sessioni/PC: `Docs/richieste/README.md` + regola in `CLAUDE.md`.
- Scritta la richiesta **R001 – UI spettacolare in stile antico** (PR in bozza): UI dinamica e "antica"
  (carta, ottone, ceralacca, timbri, biglietti del treno), effetti su pulsanti, prompt interagisci,
  inventario a valigia, HUD, toast, dialoghi, pausa, titolo, enigmi, transizioni, cutscene.
- Scritta la richiesta **R002 – Effetti sonori e musica d'epoca** (PR in bozza): canali audio con slider,
  musica con crossfade e ducking sotto le voci, **"Tema di Amerigo" originale per violino** con varianti,
  ambienti di Napoli 1946, passi per superficie, effetti per enigmi e transizioni. Niente musica del film.
- Scritta la richiesta **R003 – Personaggi in Blender: la gente di Napoli** (PR in bozza): pipeline Blender
  a script (base umana + parti modulari + varianti, atlante unico), 12–16 NPC del 1946 con attività animate,
  battute in `npc.json` con condizioni sui progressi, IK della testa; proposta di fare Amerigo per primo.

### Domande aperte per l'utente
- (R002) Qualcuno può registrare il tema al violino dal vivo? Va bene usare canzoni napoletane di pubblico dominio?
- (R003) Amerigo come primo modello? Battute con espressioni napoletane? Il cast proposto va bene?

### Prossimi passi (PC casa)
- Prima chiudere e pubblicare il lavoro in corso (fase 2.5…), poi implementare R001 sul suo branch
  seguendo `Docs/richieste/README.md` (R001, R002, R003). Merge solo con l'ok dell'utente.

---

## 2026-11-05 – PC scuola – Release v0.1 e avvio del diario
**Branch:** main · **Ultimo commit su GitHub prima di oggi:** 4fd38aa (3/11)

### Fatto
- Creata la release GitHub **v0.1 – Fase 1: vertical slice** con l'elenco dei cambiamenti e il link al gioco.
- Creati questo diario e le regole per lavorare da più PC (sezione nuova in `CLAUDE.md`).

### Problemi aperti
- ⚠️ **Lavoro non pubblicato sul PC di casa.** Nella sessione successiva al 3/11 sono state fatte molte
  modifiche sul PC di casa che non sono mai state inviate a GitHub (nessun commit remoto dopo il 3/11 18:30).
  Il lavoro è ancora solo su quel PC.

### Prossimi passi (al PC di casa)
1. Aprire Claude Code nella cartella del progetto.
2. Fare commit di tutto il lavoro locale non salvato.
3. `git pull --rebase` (per ricevere questo diario e le nuove regole), poi `git push`.
4. Aggiungere qui una voce che descrive tutto quello che era stato fatto in quella sessione.
5. Su conferma: nuova build WebGL su `gh-pages` e release (v0.2 o v0.1.1).

---

## 2026-11-03 – PC casa – Fase 1 (vertical slice)
**Branch:** feature/fase1-vertical-slice → main · **Ultimo commit:** 4fd38aa · **Tag:** v0.1
*(voce ricostruita dalla cronologia git)*

### Fatto
- Configurazione iniziale: CLAUDE.md, README, pacchetto MCP for Unity.
- Passaggio da 2D a **3D in terza persona** (decisione dell'utente).
- Sistemi di gioco: GameManager, salvataggio (PlayerPrefs), caricamento scene, transizioni, dati JSON.
- Scene: Boot, Titolo, Hub del vicolo, Basso di Antonietta, Flashback delle scarpe.
- Oggetto **Le scarpe**: 4 enigmi (quiz, lucchetto, oggetti nascosti, ordina/abbina) + cutscene.
- Inventario, pausa, schermata del titolo, controlli touch, predisposizione al doppiaggio (copione CSV).
- Correzioni dalla revisione: cifre del lucchetto, audio cutscene, click negli interni, salvataggio al ritorno.
- Build WebGL con pipeline Mobile pubblicata su GitHub Pages.

### Decisioni
- Gioco 3D, Unity 6000.6.7f1, URP, build WebGL per PC della scuola, Chromebook e LIM.
- Personaggi per ora segnaposto; modellazione in Blender rimandata.

### Prossimi passi
- Oggetti futuri (solo con ok dell'utente): stracci, volantino del treno, cappotto, violino, finale.
