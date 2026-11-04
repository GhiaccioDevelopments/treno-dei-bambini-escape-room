# Specifica tecnica Fase 1 (contratto tra moduli)

Leggere anche `CLAUDE.md` (regole). Unity 6000.6, URP 3D, terza persona, WebGL. Input System (nuovo, activeInputHandler=1).
TextMeshPro (`TMPro`, incluso in com.unity.ugui 2.x). Namespace radice `TrenoBambini`. Nessun asmdef (Assembly-CSharp / Assembly-CSharp-Editor).
Unità: 1 = 1 metro. Amerigo è un bambino: altezza ~1.2.

## Scene (Assets/_Game/Scenes) – costruite dal menu `Treno/Costruisci tutte le scene`
| nome | contenuto |
|---|---|
| 00_Boot | solo camera vuota + oggetto con `BootLoader` (carica 01_Titolo) |
| 01_Titolo | camera su un piccolo diorama del vicolo + `TitleScreen` |
| 10_Hub_Vicolo | vicolo low-poly, Amerigo, porte dei 5 luoghi (solo `basso_antonietta` aperto) |
| 20_Int_BassoAntonietta | interno arredato, `ItemPickup` oggettoId=`scarpe`, porta di uscita verso hub |
| 30_FB_Scarpe | stanza-ricordo seppia, `EscapeRoomController` flashbackId=`flashback_scarpe`, porta d'uscita (`EscapeExit`) |

Ogni scena di gioco (10, 20, 30) contiene: Directional Light, `Main Camera` (tag MainCamera) con `CinemachineBrain` e la `CinemachineCamera` `CM_Amerigo` (`CameraRig` + `WallFader`),
il player `Amerigo` (prefab-less: capsula + `CharacterController` + `ThirdPersonController` + `PlayerInteractor`, tag `Player`),
uno o più `SpawnPoint` (id string). Tutti i sistemi globali si auto-creano a runtime (vedi GameManager), quindi ogni scena è
giocabile premendo Play direttamente.

## Runtime – classi e membri pubblici usati da altri moduli

### TrenoBambini.Core
- `GameManager : MonoBehaviour` singleton `GameManager.I`. Creato automaticamente da
  `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, DontDestroyOnLoad. Crea `UIRoot`, `SceneLoader`, `AudioManager`.
  - `SaveData Save {get;}`; `void SaveGame()`; `void NewGame()`; `bool HasSave`
  - `int InputBlockers` + `void PushBlock()`, `void PopBlock()`, `bool InputBlocked`
  - `void StartFlashback(string oggettoId)` → legge `GameData.Oggetto(id).flashback`, transizione "Ricordo", carica la scena del flashback (`FlashbackDef.scena`).
  - `void CompleteFlashback(string oggettoId)` → aggiunge oggetto a inventario, salva, mostra cutscene, poi torna alla scena/spawn di provenienza (`Save.ritornoScena`, `Save.ritornoSpawn`).
- `SceneLoader : MonoBehaviour` `SceneLoader.I.Load(string scena, string spawnId, TransitionStyle stile = TransitionStyle.Fade)`.
  Dopo il caricamento posiziona il player sullo `SpawnPoint` con quell'id (se esiste).
- `enum TransitionStyle { Fade, Ricordo }` – Ricordo = overlay seppia + vignetta + dissolvenza lenta.
- `SaveSystem` (static): `Load()`, `Save(SaveData)`, `Delete()`, `Exists()`; PlayerPrefs chiave `treno_save`, JSON.
- `[Serializable] SaveData { string scena; string spawn; List<string> inventario; List<string> flashbackCompletati;
   List<string> enigmiRisolti /* "flashbackId/enigmaId" */; List<string> cutsceneViste; string ritornoScena; string ritornoSpawn; float volume = 1f; }`
- `GameData` (static): `Luoghi`, `Oggetti`, `Personaggi` (liste, caricate pigramente da `Resources/Data/*.json` con `JsonUtility`),
  `OggettoDef Oggetto(string id)`, `LuogoDef Luogo(string id)`, `PersonaggioDef Personaggio(string id)`,
  `FlashbackDef Flashback(string id)`, `CutsceneDef Cutscene(string id)` (Resources.Load<TextAsset>("Data/"+id)).
- `SpawnPoint : MonoBehaviour { public string id; }`
- `BootLoader : MonoBehaviour` → `SceneLoader.I.Load("01_Titolo", null)`.
- `AudioManager` : volume globale (AudioListener.volume), sorgente per voci.

### TrenoBambini.Data (classi [Serializable] con campi pubblici, nomi = chiavi JSON)
- `LuoghiFile { List<LuogoDef> luoghi }`, `LuogoDef { id, nome, scena, spawn, descrizione; bool aperto }`
- `OggettiFile { List<OggettoDef> oggetti }`, `OggettoDef { id, nome, descrizione, significato, temaCivico, icona, flashback, cutscene, luogo }`
- `PersonaggiFile { List<PersonaggioDef> personaggi }`, `PersonaggioDef { id, nome, ritratto, colore /* "#RRGGBB" */ }`
- `FlashbackDef { id, oggetto, titolo, scena, introduzione, conclusione; List<EnigmaDef> enigmi }`
- `EnigmaDef { id, tipo, titolo, consegna, temaCivico, spiegazione, erroreMsg, codice; float x, y, z; string modello;
   List<string> indizi; List<DomandaDef> domande; List<NascostoDef> oggetti; List<EventoDef> eventi; List<CoppiaDef> coppie }`
- `DomandaDef { testo; List<string> opzioni; int corretta; spiegazione; erroreMsg }`
- `NascostoDef { id, nome, descrizione; float x, y, z; string modello }`  (modello: "scarpa"|"zoccolo"|"cubo"|... forma primitiva)
- `EventoDef { testo, data }`, `CoppiaDef { sinistra, destra }`
- `CutsceneDef { id, titolo, sfondo; List<BattutaDef> battute }`, `BattutaDef { id, personaggio, testo, emozione, audio, azione, sfondo, posizione /* "sx"|"dx" */ }`

### TrenoBambini.Player
- `ThirdPersonController` (richiede CharacterController): WASD/frecce + joystick virtuale (`VirtualJoystick.Value`), movimento relativo alla camera,
  gravità, rotazione verso la direzione. Camminata 1.9 m/s, Shift/leva a fondo corsa 4.0. Fermo se `GameManager.I.InputBlocked`.
- `CameraRig` (su `CM_Amerigo`, CinemachineOrbitalFollow a 3 anelli + Deoccluder): orbita con mouse (tasto destro tenuto), stick destro e
  trascinamento touch nella metà destra dello schermo; sensibilità/inversione Y da `CameraSettings`. `WallFader` nasconde i muri (layer VistaLibera) tra camera e player.
- `PlayerInteractor`: trova l'`Interactable` attivo più vicino entro 1.6 m davanti al player; mostra prompt via `UIRoot.I.ShowPrompt(text)`;
  E / pulsante a schermo "Interagisci" / click sinistro sull'oggetto (raycast, se entro 3 m) → `Interact()`.

### TrenoBambini.World
- `abstract Interactable : MonoBehaviour { public string prompt; public virtual bool CanInteract => true; public abstract void Interact(); }`
- `Door : Interactable { public string luogoId; public string targetScene; public string targetSpawn; }` – se `luogoId` non vuoto e il luogo non è `aperto`
  → `UIRoot.I.Toast("… si aprirà più avanti")`. Prompt automatico "Entra: <nome luogo>".
- `ItemPickup : Interactable { public string oggettoId; }` – se l'oggetto è già in inventario: si disattiva il Glow e il prompt diventa "Ricordo già vissuto" (Interact mostra la descrizione). Altrimenti salva `ritornoScena/ritornoSpawn` (spawn = `"oggetto_"+oggettoId`) e `GameManager.I.StartFlashback(oggettoId)`.
- `Glow : MonoBehaviour` – pulsa l'emissione dei Renderer figli (MaterialPropertyBlock `_EmissionColor`, abilita keyword `_EMISSION` su istanza materiale) + leggero bob; `public bool on`.
- `NpcInfo : Interactable { public string personaggioId; public string battuta; }` – mostra una battuta breve con `UIRoot.I.Toast`.

### TrenoBambini.Puzzles
- `EscapeRoomController : MonoBehaviour { public string flashbackId; }` – carica `FlashbackDef`; all'avvio mostra introduzione (DialogBox);
  per ogni enigma crea una `PuzzleStation` alla posizione (x,y,z) con un modello primitivo; solo il primo non risolto è attivo (Glow), gli altri spenti.
  Risolto l'ultimo → `EscapeExit` si attiva (Glow). HUD "Enigmi: n/4".
- `PuzzleStation : Interactable { EnigmaDef def; }` → apre il pannello del tipo giusto.
- `EscapeExit : Interactable` → mostra `conclusione`, poi `GameManager.I.CompleteFlashback(oggetto)`.
- Pannelli UI (costruiti da codice, modali, `PushBlock/PopBlock`): `QuizPanel`, `LucchettoPanel` (ruote cifre su/giù + tastiera numerica),
  `OrdinaPanel` (carte con frecce su/giù, Verifica: dice quante sono al posto giusto), `AbbinaPanel` (colonna sx/dx, selezione a coppie),
  `NascostoPuzzle` (spawna nella stanza gli oggetti di `oggetti` come Interactable discreti senza glow; contatore trovati; descrizione di ciascuno in Toast).
  Tutti: pulsante **Indizio** (mostra indizi in sequenza), errore → `erroreMsg` gentile + riprova, successo → `spiegazione` + "Avanti".

### TrenoBambini.Dialogue
- `CutscenePlayer` (creato da UIRoot): `void Play(string cutsceneId, Action onDone)`. Overlay a schermo intero: sfondo `Resources/Sprites/sfondi/<sfondo>`,
  ritratti `Resources/Sprites/ritratti/<ritratto>` a sx/dx, box testo con nome (colore personaggio) e testo con effetto macchina da scrivere.
  Azioni: `entra_sx`, `entra_dx` (slide-in), `esce` (fade-out ritratto), `zoom` (sfondo 1→1.15), `scuoti`, `dissolvenza` (nero e ritorno).
  Audio: `VoiceLibrary.Get(string.IsNullOrEmpty(audio) ? id : audio)`; se esiste → play, avanza a fine clip (click salta); altrimenti avanza con click/E/Spazio/Invio.
  Pulsante "Salta" in alto a destra.
- `VoiceLibrary : ScriptableObject` in `Resources/VoiceLibrary.asset`: `List<Entry{string id; AudioClip clip;}> entries`; `static AudioClip Get(string id)`.

### TrenoBambini.UI
- `UIRoot : MonoBehaviour` singleton `UIRoot.I`, Canvas Screen Space Overlay, CanvasScaler 1280×720 match 0.5, EventSystem con `InputSystemUIInputModule`.
  - `ShowPrompt(string)`, `HidePrompt()`, `Toast(string, float secs = 3)`, `ShowMessage(string titolo, string testo, Action onClose)` (modale),
    `SetHudCounter(string)` (testo in alto al centro, vuoto = nascosto), `Fader` (Fade in/out + Ricordo), `CutscenePlayer Cutscene`.
  - HUD: pulsanti Inventario e Pausa in alto a destra; su touch (`Touchscreen.current != null`) joystick virtuale in basso a sx e pulsante Interagisci in basso a dx.
  - `InventoryPanel` (tasto I): griglia icone (`Resources/Sprites/oggetti/<icona>`), dettaglio con nome, descrizione, significato, tema civico.
  - `PausePanel` (Esc/P): Riprendi, Inventario, Volume (slider), Salva, Torna al titolo.
  - Hotkey I/Esc ignorate durante cutscene e nel titolo.
- `UIFactory` (static) helper per creare Panel, Text (TMP), Button, Slider, Image con stile coerente (carta seppia #E8D9B5, inchiostro #3B2A1A, accento #8C3B2A, pulsanti grandi min 56px).
- `TitleScreen : MonoBehaviour` (nella scena 01_Titolo): titolo, sottotitolo "Napoli, 1946", pulsanti Nuova partita (conferma se c'è un salvataggio), Continua (se c'è), Crediti.
  Nuova partita → `GameManager.I.NewGame()` → carica `10_Hub_Vicolo` spawn `inizio`.
- `VirtualJoystick : MonoBehaviour` con `static Vector2 Value`.

## Risorse generate dall'editor (menu `Treno/Genera grafica`) – nomi esatti
- `Resources/Sprites/oggetti/scarpe.png` (icona 128×128)
- `Resources/Sprites/ritratti/{amerigo,antonietta,tommasino,maddalena,narratore}.png` (256×256, busti stilizzati originali, seppia)
- `Resources/Sprites/sfondi/{cs_vicolo,cs_basso,cs_ricordo}.png` (640×360)
- `Resources/Sprites/ui/vignetta.png` (256×256, centro trasparente, bordi scuri)
- Materiali `Art/Materials/*.mat` (URP Lit, palette seppia): Tufo, TufoScuro, Intonaco, Legno, Ciottoli, Ferro, Tessuto1..3, Amerigo, Glow, Ricordo.
- Le icone/ritratti sono Texture importate come Sprite (Single), filtro Bilinear, senza compressione.

## Editor (Assets/_Game/Editor)
- `Treno/Genera grafica` (ArtGenerator), `Treno/Costruisci tutte le scene` (SceneBuilder: costruisce le 5 scene con primitive low-poly, aggiunge le scene ai Build Settings in ordine),
  `Treno/Doppiaggio/Aggiorna libreria voci` (scansiona `Assets/Audio/Voci`), `Treno/Doppiaggio/Esporta copione CSV` (→ `Doppiaggio/copione.csv`, UTF-8 con BOM, `;` separatore, colonne ID;Personaggio;Testo;Emozione;FileAudio),
  `Treno/Build WebGL` (Mobile_RPAsset come pipeline, compressione Gzip + decompressionFallback, output `Build/WebGL`).
