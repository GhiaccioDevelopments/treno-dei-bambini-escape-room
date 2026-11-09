# Il Treno dei Bambini – Escape Room (regole di progetto)

Gioco educativo (Educazione Civica, **secondaria di 2° grado**) ispirato al romanzo
"Il treno dei bambini" di Viola Ardone (2019) e al film di Cristina Comencini (2024).
Protagonista: Amerigo Speranza, Napoli 1946.

## Trama (riferimento obbligatorio)
- **`Docs/TRAMA.md`** è la bibbia narrativa (trama di libro e film, cronologia, personaggi, oggetti, temi,
  guida ai dialoghi, proposta per il gioco). Leggerla e seguirla **prima di ogni lavoro** su storia,
  personaggi, ambienti, dialoghi, enigmi o cutscene. Se un lavoro la contraddice, segnalarlo all'utente.

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
- **Amerigo (obbligatorio)**: capelli **castani** (decisione dell'utente 2026-11-06: sostituisce i capelli
  divisi castano/ramato). Vale anche per Amerigo adulto (castano scuro). Bambino (~7 anni) **poverissimo**
  (decisione 2026-11-06): molto magro, **scalzo** con piedi e gambe sporchi, salopette lisa, sporca,
  rattoppata e strappata.
- **Due personaggi Amerigo** (decisione 2026-11-05): nel presente a Napoli (hub) il giocatore è **Amerigo adulto**,
  tornato a Napoli per la morte della madre; nei **flashback** è **Amerigo bambino** (~7 anni, 1946).
  Sono due modelli distinti, entrambi con i capelli castani.
- Generatore MakeHuman dei due Amerigo: `ArtSource/Characters/makehuman/` (prima `prepara_materiali_amerigo.py`
  con il Python di MakeHuman, poi `genera_amerigo_makehuman.py` in Utilities ▸ Scripting).
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
- Input: nuovo Input System. Tastiera (WASD/frecce, E interagisci, I inventario, M mappa grande, Esc pausa; gamepad: Select = mappa), mouse per
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
- Font: ammessi anche SIL OFL 1.1 (ok dell'utente 2026-11-09), con il testo della licenza accanto al file e la riga in CREDITS.md.
- Ogni asset usato va annotato in **`CREDITS.md`** (origine, autore, licenza, modifiche).
- Ogni candidato: importato, screenshot con la luce del gioco, valutazione qualità/epoca/peso
  (poligoni, texture); confronti mostrati all'utente, scarti motivati. Se manca qualcosa di
  importante, dirlo invece di ripiegare su soluzioni scadenti.
- Texture PBR complete (albedo, normal, roughness/metallic, AO), materiali uniformati.
- Peso del repo: Git LFS gratuito ha 1 GB di spazio e 1 GB/mese di traffico, tenerlo d'occhio.
- Screenshot di documentazione: max 1280x720 JPG, fuori da LFS; un solo bake delle luci a fine tappa (ogni bake intermedio pesa su LFS). Quando LFS si avvicina a 1 GB, avvisare l'utente: ha deciso di comprare un data pack GitHub.

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
  Scripts/Player     ThirdPersonController (CharacterController), CameraRig (Cinemachine 3), WallFader, PlayerInteractor
  Scripts/World      Interactable, Door, ItemPickup (avvia flashback), Glow (emissione pulsante)
  Scripts/Puzzles    EscapeRoomController, PuzzleBase + Quiz/Lucchetto/Nascosto/Ordina/Abbina (pannelli 2D, anche versione semplificata)
  Scripts/Puzzles/Mondo  R009/R020: EnigmaMondo (oggetto d'epoca nel mondo), EsaminaView (vista ravvicinata 3D), MondoFab (pezzi da primitive), MondoStato, EnigmiSelfTest
  Scripts/Dialogue   CutscenePlayer, VoiceLibrary
  Scripts/Mappa      R019: MinimappaHud (in alto a sinistra), MappaGrande (tasto M), Obiettivi (prossimo passo dallo stato del gioco), MappaUi, MappaArt, MappaSelfTest
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
- `luoghi.json`: `{ "luoghi": [ {id, nome, scena, aperto, descrizione, richiede[], richiedeMsg[]} ] }` (`richiede` = gettoni necessari per entrare, un messaggio gentile per ciascuno)
- `oggetti.json`: `{ "oggetti": [ {id, nome, descrizione, significato, temaCivico, icona, flashback, cutscene, luogo} ] }`
- `flashback_<id>.json`: `{ id, oggetto, titolo, introduzione, conclusione, enigmi: [Enigma] }`
  - Enigma (R009/R020, campi nuovi, tutti opzionali): `meccanismo: oggetto|ruote|deduzione|documenti|ordina` (vuoto = ricavato dal tipo), `x,y,z,yaw,scala` (posto dell'oggetto nel mondo),
    `prompt`, `parti[]` (l'oggetto: pezzi di primitive), `ruote{n,x,y,z,passo,larghezza,raggio,colore}` + `codice`, `richiede[]` / `produce[]` (gettoni: oggetti, indizi e id di enigmi risolti, salvati in `oggettiEnigma`),
    `bloccatoMsg`, `scheda` (scheda civica dopo la soluzione), `appunto` (riga del quaderno), `soluzione[]` + `ordinata` (eventi da compiere nella vista ravvicinata), `passanti[]` + `mestieri[]` (deduzione),
    `documenti[{id,titolo,righe[]}]` (le domande hanno `documento` e `riga` = riga da indicare; `opzioni/corretta` restano per la versione semplificata), `sostituisce` (oggetti di scena nascosti perche' rimpiazzati), `inclina` (camera), `finale` (id oggetto: alla soluzione parte il suo ricordo).
  - Parte (`parti[]`): `{id, forma: cubo|cilindro|sfera|capsula|testo|scarpa|vuoto, materiale: legno|metallo|ottone|argento|carta|stoffa|cuoio|pietra|gesso|vetro|cera|quadri, colore, x,y,z, rx,ry,rz, sx,sy,sz, testo, dimensione, gruppo, nascosta,
    clic, azione: leggi|gira|ruota|rivela|nascondi|segna|muovi|premi|prendi|usa|verifica|errore, evento, dopo (id, g:gruppo, -id per nascondere), asse, valore, msg, richiede, produce, genitore, bersaglio, quando}`. Fronte dell'oggetto = -z.
  - `enigmi_mondo.json`: `{ testi, enigmi: [Enigma con scena], oggetti: [{id, nome, descrizione}] }`; `testi` = tutti i testi dell'interfaccia degli enigmi (guide, pulsanti, messaggi, formati con {0}); ripiego neutro nel codice. Campi nuovi dell'Enigma: `completoMsg` (nascosto). `luoghi.json`: `aperturaMsg`, `bypassOggetto`. `ui_menu.json`: `enigmiImp` (righe Impostazioni). Parte: `modello` (nome in Resources/Modelli/Enigmi, normalizzato in un cubo unitario; anche su `vuoto`), `msx,msy,msz` + `mox,moy,moz` (misura e centro veri del modello, il collider resta sx,sy,sz), `font` ("" stampa d'epoca, "mano" scrittura a mano). Modelli e texture: `ArtSource/Models/Enigmi/` (texture_enigmi.py, genera_enigmi_v2.py), poi Treno > Enigmi > Crea prefab dei modelli. = enigmi di hub e basso (scene `10_Hub_Vicolo`, `20_Int_BassoAntonietta`) e nomi dei gettoni; gli enigmi del ricordo stanno in `flashback_<id>.json`.
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
- `testi_ambiente.json`: `{ "testi": [ {chiave, gruppo, tipo, w, h, seed, righe[], carta[r,g,b], inchiostro[r,g,b]} ] }` (testi di manifesti, avvisi, graffiti,
  santini, calendario, insegne, letti dai generatori editor; mai nel codice). `gruppo`: quartiere | basso | ricordo (quale scena li usa); `tipo`: manifesto (testo a righe su carta,
  la prima e' il titolo) | insegna (una riga su tavola dipinta); `chiave` diventa il materiale `MP_<chiave>`; `w`,`h` dimensione texture in pixel; `seed` per macchie e consumo;
  colori 0..1. Lettere ammesse: A-Z, 0-9 e . , ' - ! ? : /
- `mappe.json` (R019, minimappa e obiettivi; le piante JPG stanno in `Resources/Mappe/<scena>.jpg` e le genera **Treno > Mappa > Genera piante**, che riscrive anche `ax0,az0,ax1,az1,ppm`):
  `{ parametri{minutiSenzaProgressi, raggioNome, raggioArrivo, raggioVista}, testi{titoloGrande, pulsante, chiudi, legendaTitolo, obiettivoTitolo, nessunObiettivo, obiettivoNascosto, capitoloFinito, aiuto, aiutoTouch, aiutoSpento, nord, legenda[{glifo,testo}]},
  scene: [ {scena, titolo, tema: normale|ricordo, ax0,az0,ax1,az1,ppm, raggio (m visibili nella minimappa), strade[{id, nome, tipo: principale|vicolo|largo|rampa|cortile|stanza, zone[{x0,x1,z0,z1}], ancore[{x,z}]}], punti[{id, glifo: porta|fontana|edicola|pozzo|lavatoio|uscita|stazione|bottega, nome, x, z}]} ],
  obiettivi: [ {id, testo, completo[] (gettoni di MondoStato: l'obiettivo corrente e' il primo con almeno un gettone mancante), punti[{scena, x, z, testo}] (dove andare scena per scena: nella scena sbagliata si indica la porta)} ] }`.
  `strade[].zone` = rettangoli nel mondo (l'ultima strada della lista che contiene il giocatore da' il nome alla targa); `ancore` = dove scrivere il nome sulla pianta (compare entro `raggioNome` metri).
  Nomi: veri per le strade principali (Via Roma già Toledo nel 1946, Vico Lungo Gelso, Via Speranzella, Via Concordia, Via Portacarrese a Montecalvario, Piazza Montecalvario), inventati ma verificati su OpenStreetMap (nessuna corrispondenza) per i vicoletti.
  Aiuto della mappa in `SaveData`: `aiutoMappa` (0 Guidata, 1 Normale = default, 2 Esploratore), `minimappaNascosta`, `nordFisso`; testi delle righe in `ui_menu.json` (blocco `mappaImp`, scheda Impostazioni > Gioco).
- `audio.json`: `{ scene: [ {scena, musica, ambiente[], sparsi[], intervallo[min,max], superficie: pietra|legno|ovattato} ],
  eventi: [ {id, stinger, sfx} ], volumi: {musica, ambiente, effetti, voci, duckingMusica, duckingAmbiente} }`
  - Gli id sono nomi di file in `Assets/Audio/{Musica,Ambienti,Effetti}` (menu **Treno > Audio > Aggiorna libreria**); se manca il file
    suona la versione sintetizzata da codice (`Scripts/Audio/`), se manca anche quella, silenzio. Eventi usati dal codice:
    `enigma_risolto, oggetto_ritrovato, ricordo_entra, ricordo_esce, porta_apre, porta_chiusa, raccolta, trovato, cambio_scena`.

## Git
- `main` sempre funzionante; ogni fase/oggetto su branch dedicato (`feature/...`), merge solo dopo test.
- Commit piccoli, messaggi in italiano. Tag a fine fase (v0.1, v0.2…), **push solo dopo conferma dell'utente**
  (eccezione: il push del lavoro in corso sul branch corrente, vedi "Diario e lavoro su più PC").
- Build WebGL su branch `gh-pages` (compressione Gzip + Decompression Fallback).
- Remote: `GhiaccioDevelopments/treno-dei-bambini-escape-room` (pubblico). Mai token/password/dati personali.

## Diario e lavoro su più PC
L'utente lavora da più computer (PC di casa, PC della scuola). Chat e memorie di Claude restano sul PC
dove sono state create: **l'unica memoria condivisa è `Docs/DIARIO.md`**, che viaggia con il repository.
- **Inizio sessione**: `git pull`, poi leggere le ultime voci di `Docs/DIARIO.md` e `git log -10`.
  Se il diario segnala lavoro rimasto su un altro PC, ricordarlo all'utente prima di iniziare.
- **Durante la sessione**: dopo ogni blocco di lavoro importante, commit.
- **Fine sessione** (l'utente saluta, dice che ha finito o che cambia PC) e comunque prima di risposte
  lunghe di chiusura: aggiungere in cima al diario una voce nel formato sotto, commit e **push sul branch
  corrente** senza chiedere conferma (serve a non lasciare lavoro bloccato su un PC). Merge su `main`, tag,
  release e build su `gh-pages` restano su conferma dell'utente.
- Se il push fallisce perché il remoto è avanti: `git pull --rebase`, risolvere, ripetere il push.
- Il repository è pubblico: nel diario niente dati personali, nomi di persone reali, password o token.
- **Richieste da altre sessioni:** idee dell'utente raccolte su un PC e da implementare su un altro arrivano
  come PR in bozza `[RNNN] ...` su branch `richiesta/...`, con la specifica in `Docs/richieste/`.
  Protocollo completo in `Docs/richieste/README.md`. A inizio sessione: `gh pr list --draft` e segnalare
  all'utente le richieste aperte; implementarle **solo dopo** aver chiuso e pubblicato il lavoro in corso.

Formato voce:
```
## AAAA-MM-GG – PC <casa|scuola> – <titolo breve>
**Branch:** <branch> · **Ultimo commit:** <hash breve>
### Fatto
- ...
### Decisioni
- ...
### Problemi aperti
- ...
### Prossimi passi
- ...
```

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
  Richieste dell'utente in sospeso: `Docs/BACKLOG.md` (controllarlo all'inizio di ogni sotto-fase).
- Oggetti futuri: stracci, volantino del treno, cappotto, violino, finale. **Ok dell'utente (2026-11-06)** a svilupparli tutti
  tramite le richieste R012–R016 (schema degli enigmi di ogni capitolo da approvare prima dello sviluppo).
