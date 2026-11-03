# Il Treno dei Bambini – Escape Room (regole di progetto)

Gioco educativo (Educazione Civica, **secondaria di 2° grado**) ispirato al romanzo
"Il treno dei bambini" di Viola Ardone (2019) e al film di Cristina Comencini (2024).
Protagonista: Amerigo Speranza, Napoli 1946.

## Regole fisse
- **Unity 6.6 (6000.6.7f1)**, **3D URP**, **terza persona** (camera che segue Amerigo), C#.
  Decisione dell'utente (2026-11-03): il gioco è 3D, NON 2D (sostituisce "2D top-down/tilemap" del brief).
  Build finale **WebGL** (PC scuola, Chromebook, LIM): low-poly, poche luci, ombre semplici,
  niente post-processing pesante; usare il renderer Mobile_RPAsset per WebGL.
- Personaggi: modellati in **Blender** (MCP blender) ed esportati FBX in `Assets/_Game/Art/Characters/`.
  Fase personaggi rimandata: per ora placeholder (capsule/forme semplici). Personaggi ORIGINALI ispirati
  alla descrizione del romanzo: non copiare foto/sembianze degli attori del film.
- Lavoro nell'editor **solo tramite MCP for Unity** (server HTTP `127.0.0.1:8080`).
  Se la connessione cade: fermarsi e chiedere all'utente di riavviare il server
  (Window ▸ MCP for Unity ▸ Start Server, poi `/mcp` ▸ Reconnect).
- Dopo ogni modifica importante: `read_console` e correggere gli errori prima di proseguire.
- L'utente non costruisce nulla a mano: scene, ambienti, materiali, UI sono generati da
  script dell'editor (menu **Treno ▸ …**) e quindi riproducibili.
- **Contenuti fuori dal codice**: oggetti, luoghi, enigmi, cutscene in JSON in
  `Assets/_Game/Resources/Data/`. Il codice non contiene testi narrativi.
- Grafica: ambienti low-poly originali (moduli semplici: case in tufo, vicoli, panni stesi, macerie),
  materiali in palette seppia/anni '40.
  **Mai** immagini, musiche o dialoghi presi dal film/libro: dialoghi originali.
- Didattica non punitiva: errore = feedback gentile + spiegazione + riprova; pulsante Indizio sempre presente.
- Input: nuovo Input System. Tastiera (WASD/frecce, E interagisci, I inventario, Esc pausa), mouse per
  ruotare la camera; touch: joystick virtuale + pulsanti a schermo (LIM/Chromebook).
- Salvataggio: PlayerPrefs (chiave `treno_save`, JSON).
- Lingua del gioco, dei commit e della documentazione: italiano.

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
  Art/Characters/    FBX personaggi da Blender
  Art/Environment/   moduli low-poly, materiali seppia
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
- Oggetti futuri: stracci, volantino del treno, cappotto, violino, finale.
