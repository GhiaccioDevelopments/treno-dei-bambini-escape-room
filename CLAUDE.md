# Il Treno dei Bambini – Escape Room (regole di progetto)

Gioco educativo (Educazione Civica, **secondaria di 2° grado**) ispirato al romanzo
"Il treno dei bambini" di Viola Ardone (2019) e al film di Cristina Comencini (2024).
Protagonista: Amerigo Speranza, Napoli 1946.

## Regole fisse
- **Unity 6.6 (6000.6.7f1)**, **3D URP**, **terza persona** (camera che segue Amerigo), C#.
  Decisione dell'utente (2026-11-03): il gioco è 3D, NON 2D (sostituisce "2D top-down/tilemap" del brief).
- **Piattaforme** (decisione 2026-11-04, Fase 2): build principale **Windows .exe**, avvio in finestra
  ridimensionabile 1280×720, schermo intero da Impostazioni o Alt+Invio. In più una build **WebGL leggera**
  (preset Bassa, renderer Mobile_RPAsset) su `gh-pages` per PC scuola, Chromebook e LIM.
- Personaggi: basi **CC0 MPFB2/MakeHuman** in **Blender** (MCP blender), personalizzate con abiti d'epoca
  (Napoli e Modena 1946), rig, LOD; FBX in `Assets/Art/Characters/`, avatar Humanoid.
  Animazioni **Mixamo** (Git LFS) in `Assets/Art/Animations/`. Screenshot di ogni personaggio all'utente
  prima di considerarlo finito. Personaggi ORIGINALI ispirati alla descrizione del romanzo:
  non copiare foto/sembianze degli attori del film.
- **Amerigo (obbligatorio)**: capelli divisi verticalmente, lato sinistro castano scuro, lato destro
  rosso ramato naturale (non fluo). Vale anche per Amerigo adulto. Bambino magro (~7 anni), vestiti
  logori e rattoppati, scarpe rotte all'inizio.
- Lavoro nell'editor **solo tramite MCP for Unity** (server HTTP `127.0.0.1:8080`).
  Se la connessione cade: fermarsi e chiedere all'utente di riavviare il server
  (Window ▸ MCP for Unity ▸ Start Server, poi `/mcp` ▸ Reconnect).
- Dopo ogni modifica importante: `read_console` e correggere gli errori prima di proseguire.
- L'utente non costruisce nulla a mano: scene, ambienti, materiali, UI sono generati da
  script dell'editor (menu **Treno ▸ …**) e quindi riproducibili.
- **Contenuti fuori dal codice**: oggetti, luoghi, enigmi, cutscene in JSON in
  `Assets/_Game/Resources/Data/`. Il codice non contiene testi narrativi.
- **Mai** immagini, musiche o dialoghi presi dal film/libro: dialoghi originali.
- Didattica non punitiva: errore = feedback gentile + spiegazione + riprova; pulsante Indizio sempre presente.
- Input: nuovo Input System. Tastiera (WASD/frecce, E interagisci, I inventario, Esc pausa), mouse per
  ruotare la camera; touch: joystick virtuale + pulsanti a schermo (LIM/Chromebook).
- Salvataggio: PlayerPrefs (chiave `treno_save`, JSON).
- Lingua del gioco, dei commit e della documentazione: italiano.

## Stile visivo e prestazioni (Fase 2)
- Tono **serio, realistico, cinematografico** (riferimento d'atmosfera: The Last of Us), mai cartoon.
  Look neorealista del dopoguerra: colori desaturati e caldi, seppia e terra, contrasti morbidi,
  grana leggera, luce naturale che filtra tra i palazzi. Interni dei bassi bui, lampade a petrolio e candele.
- Coerenza d'epoca (Napoli 1946): nessun oggetto, abito, insegna o materiale moderno.
  Dubbi storici → chiedere all'utente.
- Flashback con look distinto (seppia più marcato, bordi sfumati). Cutscene con **Timeline + Cinemachine** (pacchetti 6.6.0;
  Cinemachine con API 3: namespace `Unity.Cinemachine`, `CinemachineCamera`, non le vecchie `CinemachineVirtualCamera`).
- Bello ma leggero: luce **baked** (lightmap, light probes, reflection probes), poche luci dinamiche,
  LUT d'epoca, nebbia semplice (no volumetrica), LOD su tutti i modelli, occlusion culling,
  texture compresse e atlas. Obiettivo: ≥60 FPS stabili a preset Alta su PC normale, fluido a Bassa
  su PC scolastici. Preset grafici: Bassa, Media, Alta, Ultra, Personalizzata.
- HUD minimale: solo elementi piccoli che compaiono quando servono; prompt di interazione discreti.
  UI d'epoca (carta invecchiata, tipografia anni '40 leggibile, colori spenti); inventario come
  scatola di latta o quaderno di scuola.
- Menu **Impostazioni** completo (grafica, audio, controlli, gioco, accessibilità) da menu principale
  e pausa, preferenze salvate.

## Asset esterni e licenze
- Fonti affidabili, licenza adatta a un progetto scolastico pubblicato online: **CC0 preferito**,
  CC-BY ammesso con attribuzione. NC/ND o licenze dubbie: segnalarle all'utente
  e non usarle senza il suo ok esplicito.
- Ogni asset usato va annotato in **`CREDITS.md`** (origine, autore, licenza, modifiche).
- Ogni candidato: importato, screenshot con la luce del gioco, valutazione qualità/epoca/peso
  (poligoni, texture); confronti mostrati all'utente, scarti motivati. Se manca qualcosa di
  importante, dirlo invece di ripiegare su soluzioni scadenti.
- Texture PBR complete (albedo, normal, roughness/metallic, AO), materiali uniformati.
- Peso del repo: Git LFS gratuito ha 1 GB di spazio e 1 GB/mese di traffico, tenerlo d'occhio.

## Doppiaggio
- Ogni battuta di cutscene ha un **ID univoco** `cs_<oggetto>_NNN` e un campo `audio` opzionale.
- File audio in `Assets/Audio/Voci/<id>.ogg|wav|mp3` (Git LFS). Il menu
  **Treno ▸ Doppiaggio ▸ Aggiorna libreria voci** li collega alle battute (VoiceLibrary).
- Con audio: il testo avanza a fine clip. Senza audio: avanza con click/E/Spazio.
- **Treno ▸ Doppiaggio ▸ Esporta copione CSV** → `Doppiaggio/copione.csv` (ID, personaggio, testo, emozione, file audio).

## Struttura
```
Assets/_Game/
  Scenes/            00_Boot, 01_Titolo, 10_Hub_Vicolo, 20_Int_BassoAntonietta, 30_FB_Scarpe
  Scripts/Core       GameManager, SaveSystem, SceneLoader, Transition, GameData (loader JSON)
  Scripts/Player     ThirdPersonController (CharacterController), CameraFollow, PlayerInteractor
  Scripts/World      Interactable, Door, ItemPickup (avvia flashback), Glow (emissione pulsante)
  Scripts/Puzzles    EscapeRoomController, PuzzleBase + Quiz/Lucchetto/Nascosto/Ordina/Abbina
  Scripts/Dialogue   CutscenePlayer, VoiceLibrary
  Scripts/UI         UIRoot (UI costruita da codice), Inventario, Pausa, Titolo, Dialog box, Toast
  Scripts/Data       classi serializzabili che rispecchiano i JSON
  Editor/            generatori sprite/tileset, costruttore scene, export CSV, build WebGL
  Resources/Data/    *.json dei contenuti
  Resources/Sprites/ icone inventario, ritratti, sfondi cutscene
  Art/               asset generati della Fase 1 (segnaposto low-poly, materiali seppia)
Assets/Art/          asset realistici della Fase 2 (LFS), annotati in CREDITS.md
  Textures/ Materials/ Models/ Characters/ Animations/ HDRI/ Audio/
Assets/Audio/Voci/   doppiaggio (LFS)
Doppiaggio/          copione CSV esportato
```
Namespace: `TrenoBambini.<Area>` (es. `TrenoBambini.Core`).

## Schema JSON (sintesi)
- `luoghi.json`: `{ "luoghi": [ {id, nome, scena, aperto, descrizione} ] }`
- `oggetti.json`: `{ "oggetti": [ {id, nome, descrizione, significato, temaCivico, icona, flashback, cutscene, luogo} ] }`
- `flashback_<id>.json`: `{ id, oggetto, titolo, introduzione, conclusione, enigmi: [Enigma] }`
  - Enigma: `{ id, tipo: quiz|lucchetto|nascosto|ordina|abbina, titolo, consegna, temaCivico,
    indizi[], spiegazione, x, y, z, ...campi del tipo }`
  - quiz: `domande: [{testo, opzioni[], corretta, spiegazione}]`
  - lucchetto: `codice` (stringa di cifre), `erroreMsg`
  - nascosto: `oggetti: [{id, nome, descrizione, x, y, z}]` (coordinate mondo nella stanza 3D)
  - ordina: `eventi: [{testo, data}]` nell'ordine corretto (mescolati a runtime)
  - abbina: `coppie: [{sinistra, destra}]`
- `cutscene_<id>.json`: `{ id, titolo, sfondo, battute: [ {id, personaggio, testo, emozione, audio, azione, sfondo} ] }`
  - azione: `entra_sx | entra_dx | esce | zoom | scuoti | dissolvenza | ""`
- `personaggi.json`: `{ "personaggi": [ {id, nome, ritratto, colore} ] }`

## Git
- `main` sempre funzionante; ogni fase/oggetto su branch dedicato (`feature/...`), merge solo dopo test.
- Commit piccoli, messaggi in italiano. Tag a fine fase (v0.1, v0.2…), **push solo dopo conferma dell'utente**.
- Build WebGL su branch `gh-pages` (compressione Gzip + Decompression Fallback).
- Remote: `GhiaccioDevelopments/treno-dei-bambini-escape-room` (pubblico). Mai token/password/dati personali.

## Stato fasi
- Fase 1 (v0.1): hub vicolo, basso di Antonietta, oggetto **Le scarpe** (4 enigmi + cutscene),
  inventario, pausa, titolo, salvataggio. **Non sviluppare altri oggetti senza ok dell'utente.**
- Fase 2 (v0.2, branch `feature/upgrade-realistico`): upgrade a gioco realistico d'epoca, stessa
  struttura, storia, JSON, enigmi, inventario, cutscene e ID doppiaggio. Sotto-fasi:
  2.0 setup · 2.1 look d'epoca (preset URP, LUT, nebbia, luce baked) · 2.2 ricerca asset ·
  2.3 ambienti · 2.4 giocatore e camera · 2.5 personaggi · 2.6 cutscene Timeline ·
  2.7 interfaccia, impostazioni, finestra · 2.8 ottimizzazione e consegna.
  Dopo ogni sotto-fase: console pulita + screenshot all'utente per approvazione prima di proseguire.
  Misurare FPS e peso build per preset. Merge su `main` e tag v0.2 solo dopo l'ok dell'utente.
- Oggetti futuri: stracci, volantino del treno, cappotto, violino, finale.
