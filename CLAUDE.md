# Il Treno dei Bambini – Escape Room (regole di progetto)

Gioco educativo (Educazione Civica, **secondaria di 2° grado**) ispirato al romanzo
"Il treno dei bambini" di Viola Ardone (2019) e al film di Cristina Comencini (2024).
Protagonista: Amerigo Speranza, Napoli 1946.

## Regole fisse
- **Unity 6.6 (6000.6.7f1)**, 2D URP (Renderer 2D), top-down, C#. Build finale **WebGL**
  (PC scuola, Chromebook, LIM): leggero, niente post-processing pesante.
- Lavoro nell'editor **solo tramite MCP for Unity** (server HTTP `127.0.0.1:8080`).
  Se la connessione cade: fermarsi e chiedere all'utente di riavviare il server
  (Window ▸ MCP for Unity ▸ Start Server, poi `/mcp` ▸ Reconnect).
- Dopo ogni modifica importante: `read_console` e correggere gli errori prima di proseguire.
- L'utente non costruisce nulla a mano: scene, sprite, tilemap, UI sono generati da
  script dell'editor (menu **Treno ▸ …**) e quindi riproducibili.
- **Contenuti fuori dal codice**: oggetti, luoghi, enigmi, cutscene in JSON in
  `Assets/_Game/Resources/Data/`. Il codice non contiene testi narrativi.
- Grafica: pixel art originale generata da codice, palette seppia/anni '40.
  **Mai** immagini, musiche o dialoghi presi dal film/libro: dialoghi originali.
- Didattica non punitiva: errore = feedback gentile + spiegazione + riprova; pulsante Indizio sempre presente.
- Input: nuovo Input System. Tastiera (WASD/frecce, E interagisci, I inventario, Esc pausa)
  + mouse/touch (click per muoversi/interagire, pulsanti a schermo).
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
  Scripts/Player     PlayerController (movimento + click-to-move), PlayerInteractor
  Scripts/World      Interactable, Door, ItemPickup (avvia flashback), Glow
  Scripts/Puzzles    EscapeRoomController, PuzzleBase + Quiz/Lucchetto/Nascosto/Ordina/Abbina
  Scripts/Dialogue   CutscenePlayer, VoiceLibrary
  Scripts/UI         UIRoot (UI costruita da codice), Inventario, Pausa, Titolo, Dialog box, Toast
  Scripts/Data       classi serializzabili che rispecchiano i JSON
  Editor/            generatori sprite/tileset, costruttore scene, export CSV, build WebGL
  Resources/Data/    *.json dei contenuti
  Resources/Sprites/ sprite generati caricati a runtime (icone, ritratti, sfondi)
  Art/Generated/     tile e sprite generati usati nelle scene
Assets/Audio/Voci/   doppiaggio (LFS)
Doppiaggio/          copione CSV esportato
```
Namespace: `TrenoBambini.<Area>` (es. `TrenoBambini.Core`).

## Schema JSON (sintesi)
- `luoghi.json`: `{ "luoghi": [ {id, nome, scena, aperto, descrizione} ] }`
- `oggetti.json`: `{ "oggetti": [ {id, nome, descrizione, significato, temaCivico, icona, flashback, cutscene, luogo} ] }`
- `flashback_<id>.json`: `{ id, oggetto, titolo, introduzione, conclusione, enigmi: [Enigma] }`
  - Enigma: `{ id, tipo: quiz|lucchetto|nascosto|ordina|abbina, titolo, consegna, temaCivico,
    indizi[], spiegazione, x, y, sprite, ...campi del tipo }`
  - quiz: `domande: [{testo, opzioni[], corretta, spiegazione}]`
  - lucchetto: `codice` (stringa di cifre), `erroreMsg`
  - nascosto: `oggetti: [{id, nome, descrizione, x, y, sprite}]` (coordinate in unità mondo nella stanza)
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
