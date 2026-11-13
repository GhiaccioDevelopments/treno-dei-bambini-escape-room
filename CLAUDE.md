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
- **Mai domande all'utente** (decisione 2026-11-09): non fare domande, né a pannello né in testo, **a meno che l'utente lo chieda
  esplicitamente nel prompt**. Per ogni decisione scegliere da soli l'opzione raccomandata, dirlo in chat e scriverla nel Rapporto
  della richiesta (sezione "Decisioni prese da Claude"), così l'utente può correggerla dopo. Vale anche per le approvazioni chieste
  dalle specifiche (copioni, elenchi, schemi): mostrarli e proseguire senza aspettare. Prevale su qualunque altra regola che dica di chiedere.

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
  Scenes/            00_Boot, 01_Titolo, 10_Hub_Vicolo, 20_Int_BassoAntonietta, 30_FB_Scarpe, 40_FB_Stracci (R012: vicolo degli stracci + bottega di Capa 'e fierro, un'unica scena; Treno > Scene > Costruisci ricordo Stracci), 50_FB_Volantino (R013: vicolo delle voci = set de "Le scarpe" di sera con la pioggia + sede del Comitato; Treno > Scene > Costruisci ricordo Volantino), 60_FB_Stazione + 61_FB_Vagone (R014: un ricordo su due scene, enigmi con campo "scena"; Treno > Scene > Costruisci ricordo Cappotto), 70_FB_Modena (R015: corte innevata con casa di Derna, laboratorio dei Benvenuti e scuola; Treno > Scene > Costruisci ricordo Violino)
  Scripts/Core       GameManager, SaveSystem, SceneLoader, Transition, GameData (loader JSON)
  Scripts/Player     ThirdPersonController (CharacterController), CameraRig (Cinemachine 3), WallFader, PlayerInteractor
  Scripts/World      Interactable, Door, ItemPickup (avvia flashback), IngressoRicordo (R012: oggetti-ricordo creati a runtime da oggetti.json, campo ingresso), Glow (emissione pulsante)
  Scripts/Puzzles    EscapeRoomController, PuzzleBase + Quiz/Lucchetto/Nascosto/Ordina/Abbina (pannelli 2D, anche versione semplificata)
  Scripts/Puzzles/Mondo  R009/R020: EnigmaMondo (oggetto d'epoca nel mondo), EsaminaView (vista ravvicinata 3D; EsaminaViewStracci.cs = meccanismi tessuti, bilancia, monete, colonne; EsaminaViewVolantino.cs = bacheca a fili, ciclostile, datario; EsaminaViewCappotto.cs = cartellini, cappotti, finestrino, dialogo; EsaminaViewViolino.cs = forno, suoni, accordatura, impronte, melodia), MondoFab (pezzi da primitive), MondoStato, EnigmiSelfTest, StraccTest (-straccitest), VolantinoTest (-volantinotest), CappottoTest (-cappottotest), ViolinoTest (-violinotest)
  Scripts/Dialogue   CutscenePlayer, VoiceLibrary
  Scripts/Mappa      R019: MinimappaHud (in alto a sinistra), MappaGrande (tasto M), Obiettivi (prossimo passo dallo stato del gioco), MappaUi, MappaArt, MappaSelfTest
  Scripts/Segreti    R011: SegretiStato (progressi, flag, Indizio muto, pagina Segreti del quaderno, contatore della pausa), SegretiSpawner + SegretoPezzo/CampanaGesso/UovoSegreto (mondo dei segreti), SegretiSelfTest
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
  - R012, campi nuovi dell'oggetto: `cutsceneIngresso` (id cutscene che parte prima di entrare nel ricordo, solo la prima volta; `cutscene` parte all'uscita; se il file ha battute con `capitolo` "ingresso"/"chiusura" ciascuna parte usa solo le sue), `ingresso {scena,x,y,z,yaw,richiedeFlashback,prompt,parti[]}` (l'oggetto d'ingresso compare a runtime in una scena gia' costruita, solo dopo il flashback richiesto e finche' non e' in inventario). `luoghi.json`: `sbloccatoDa` (oggetto che sblocca il luogo) e `sbloccatoMsg` (messaggio se la scena non esiste ancora).
- `flashback_<id>.json`: `{ id, oggetto, titolo, introduzione, conclusione, enigmi: [Enigma] }`
  - R012, meccanismi `tessuti` (passanti[] = pezzi di stoffa con `stile` fibra, `mestiere` categoria, `controluce/filo/spessore`; `strumenti[4]` etichette degli strumenti; tipo `nascosto` + `oggetti[]` per raccoglierli nel mondo), `bilancia` (parti `trave`, `piatto_sx/dx`, `chiodo`, `peso` con `valore` in grammi e `gruppo` sx/dx; `squilibrio` = grammi del chiodo; `strumenti` = 4 messaggi; eventi `pesata_*`), `monete` (parti `banco`, `totale`, `moneta` con `valore` in lire; `codice` = somma giusta; `strumenti` = pulsante, formato del totale, due messaggi), `colonne` (`righe[{oggetto,mestiere,diritto,nota}]`, `mestieri[]`, `diritti[]`, `strumenti` = 3 intestazioni + messaggio d'errore).
  - R013, meccanismi `bacheca` (parti con azione `diceria` (valore = indice), `documento`, `verdetto`; `collegamenti[{voce,a (id del foglio giusto),verdetto: vera|falsa,nota}]`; due collegamenti giusti si fissano insieme; `strumenti` = 10 messaggi, 5-7 = ?/VERA/FALSA), `ciclostile` (`frasi[{testo,giusta,motivo}]` (3 giuste), parti con azione `fase` (valore 1 matrice, 2 inchiostro, `errore` se fuori ordine) e `manovella`, `giri`, `ritmo` = secondi minimi fra due giri; pezzi `tamburo`, `foglio_N` (gruppo `fogli`), `macchia`, gruppo `scritta`), `datario` (parti con azione `nastro` (valore 0 giorno, 1 mese, 2 anno) e `timbra`, `date[{giorno,mese,anno,etichetta,parte}]` in ordine, `mesi[12]`, `anni[]`). Oggetti nascosti con `modello: "voce"` = voci da ascoltare (punto a terra x,y,z, finestra `fx,fy,fz`, `prompt`). `usa` con `richiede` a elenco separato da virgole e `errore` (inventario attivo con piu' oggetti: evento per ciascuno `evento:oggetto`).
  - R015, meccanismi (ogni suono ha un'alternativa visiva) `forno` (`forno{inizio, giusto0, giusto1, massimo, legna, raffredda, serranda, cottura, presto, tardi}`; parti con azione `legna`, `serranda`, `farina`, `inforna`, `sforna`; pezzi `brace`, `fiamma`, `pizzico_bianco|nocciola|nero`, `pane_crudo|forno|cotto`, `clessidra_sopra|sotto`; `strumenti` = 12 messaggi), `suoni` (`suoni[{attrezzo, nome, tipo: diapason|chiave|feltro|martelletto|corda, onda, ok}]` in ordine; parti `attrezzo` (valore = tasto 1-5); parte `onde` = area della lavagnetta), `accordatura` (`corde[{id, nome, nota, scarto, passo, dopo}]`; parti `corda` (valore = indice), `giro` (valore -1/+1), `lancetta`; tasti Z/X), `impronte` (`impronte[{nome, misura, chiodi, tacco}]`; parti `scarpone` e `orma` (valore = ragazzo; `evento: "porta"` = le orme da riconoscere, rivelano `g:pista_1`), `ramo` (valore = bivio, `evento: "giusto"`, `dopo`)), `melodia` (`note[]` midi, `frasi[{note[], tempo, ok}]`; parti `nota` (valore = indice), pallini `frase_F_K` (gruppo `frase_F`), `frase_F_ok`, `rivo_mano`).
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
  - Campi extra di `cutscene_intro.json` (R025, filmato iniziale; id `intro`, 36 battute `cs_intro_001…036`): a livello cutscene `capitoli[{id, titolo, epoca: presente|ricordo, durata, pausaSenzaTesto, battute[primo id, ultimo id], regia{luogo, luce, camera, musica, note}}]`, `durataStimata`, `obiettivoFinale`;
    per battuta `capitolo` (id), `controllo` (targhetta dei controlli: `salta | muovi | muovi_guarda | interagisci | inventario | mappa | indizio`, chiavi astratte tradotte per tastiera+mouse / gamepad / touch), `durata` (secondi di lettura senza audio), `regia{inquadratura, epoca}` (solo note).
    Generato da `ArtSource/Copioni/genera_intro.py` (non modificare a mano): ID rinumerati in ordine, senza buchi.
- `filmato_intro.json` (R025, regia del filmato iniziale; generato da `ArtSource/Copioni/genera_filmato.py`, mai testo narrativo nel codice): `{ parametri{tenutaSalta, velocitaTesto, obiettivoForzatoSecondi, guardiaSecondi, pausaFraBattute, velocitaCammino, conRitratto[]}, testi{...interfaccia del filmato}, controlli[{id, titolo, tastiera[], gamepad[], touch[]}], carte[{id, titolo, stile: scheda|pagina, righe[], larghezza, altezza, inclina}], capitoli[{id, scena, stile: fade|ricordo, look: ""|ricordo, musica ("-" = ferma), tinta, tintaFine, inquadrature[]}], finale{scena, pos[x,z], yaw} }`.
  - Inquadratura: `{da (id prima battuta; la prima parte col capitolo), taglio: ""(stacco)|dissolvenza, p0,p1 (camera), m0,m1 (punto guardato), fov0,fov1, segui: ""|adulto|bambino (coordinate nel sistema dell'attore: x destra, y su, z avanti), ease, scossa, consegna (la camera passa al gioco), musica, attore{presente, pos[x,z], yaw, percorso[x,z,...], velocita, nascondi}, inserto{tipo: pezzi|carta|pianta, parti[] (come gli enigmi), carta, distanza, giraAlSecondo, inclina, luce, indizio, errore, croce, scena, minimappa}}`.
  - Regista: `Scripts/Dialogue/FilmatoIniziale.cs` (+ `FilmatoStudio.cs`, prova `FilmatoSelfTest.cs`, flag `-introtest [-introtestnaturale] [-introtestfoto]`, `-senzafilmato` per le prove di prestazioni). Parte alla Nuova partita (e con Continua se il salvataggio non ha filmato visto ne' progressi), si salta tenendo premuto, si rivede da Pausa e dal menu principale ("Rivedi il filmato iniziale", testo in `ui_menu.json`).
- `personaggi.json`: campo opzionale `nomeCopione` (nome nel copione CSV del doppiaggio: `amerigo` = "Amerigo bambino", `amerigo_adulto` = "Amerigo adulto").
- `SaveData.filmatoVisto` (R025): il filmato iniziale e' stato visto o saltato in questa partita.
- `personaggi.json`: `{ "personaggi": [ {id, nome, ritratto, colore} ] }`
- `testi_ambiente.json`: `{ "testi": [ {chiave, gruppo, tipo, w, h, seed, righe[], carta[r,g,b], inchiostro[r,g,b]} ] }` (testi di manifesti, avvisi, graffiti,
  santini, calendario, insegne, letti dai generatori editor; mai nel codice). `gruppo`: quartiere | basso | ricordo | segreti (quale scena li usa; `segreti` = texture dei pezzi di carta di R011, tipi ritaglio|cartolina|amlire|settebello|biglietto_scarpa|biglietto_mela|biglietto_treno|lista, disegnate da **Treno > Segreti > Genera texture dei pezzi** in `Resources/Texture/Segreti/`, usate dalle parti con `materiale: "tex:<nome>"`); `tipo`: manifesto (testo a righe su carta,
  la prima e' il titolo) | insegna (una riga su tavola dipinta); `chiave` diventa il materiale `MP_<chiave>`; `w`,`h` dimensione texture in pixel; `seed` per macchie e consumo;
  colori 0..1. Lettere ammesse: A-Z, 0-9 e . , ' - ! ? : /
- `mappe.json` (R019, minimappa e obiettivi; le piante JPG stanno in `Resources/Mappe/<scena>.jpg` e le genera **Treno > Mappa > Genera piante**, che riscrive anche `ax0,az0,ax1,az1,ppm`):
  `{ parametri{minutiSenzaProgressi, raggioNome, raggioArrivo, raggioVista}, testi{titoloGrande, pulsante, chiudi, legendaTitolo, obiettivoTitolo, nessunObiettivo, obiettivoNascosto, capitoloFinito, aiuto, aiutoTouch, aiutoSpento, nord, legenda[{glifo,testo}]},
  scene: [ {scena, titolo, tema: normale|ricordo, ax0,az0,ax1,az1,ppm, raggio (m visibili nella minimappa), strade[{id, nome, tipo: principale|vicolo|largo|rampa|cortile|stanza, zone[{x0,x1,z0,z1}], ancore[{x,z}]}], punti[{id, glifo: porta|fontana|edicola|pozzo|lavatoio|uscita|stazione|bottega, nome, x, z}]} ],
  obiettivi: [ {id, testo, capitolo (id del flashback, es. "flashback_scarpe": se e' in `flashbackCompletati` il passo non e' piu' un obiettivo, anche in un salvataggio vecchio senza gettoni), completo[] (gettoni di MondoStato: l'obiettivo corrente e' il primo passo non chiuso dal capitolo con almeno un gettone mancante), punti[{scena, x, z, testo}] (dove andare scena per scena: nella scena sbagliata si indica la porta)} ] }`. `parametri.raggioArrivo`: dentro questa distanza (m) la X dell'obiettivo si spegne.
  `strade[].zone` = rettangoli nel mondo (l'ultima strada della lista che contiene il giocatore da' il nome alla targa); `ancore` = dove scrivere il nome sulla pianta (compare entro `raggioNome` metri).
  Nomi: veri per le strade principali (Via Roma già Toledo nel 1946, Vico Lungo Gelso, Via Speranzella, Via Concordia, Via Portacarrese a Montecalvario, Piazza Montecalvario), inventati ma verificati su OpenStreetMap (nessuna corrispondenza) per i vicoletti.
  Aiuto della mappa in `SaveData`: `aiutoMappa` (0 Guidata, 1 Normale = default, 2 Esploratore), `minimappaNascosta`, `nordFisso`; testi delle righe in `ui_menu.json` (blocco `mappaImp`, scheda Impostazioni > Gioco).
- `segreti.json` (R011, segreti facoltativi di ogni stanza; **generato da `ArtSource/Segreti/genera_segreti.py`**, che aggancia i segreti per NOME del punto nei rapporti `Docs/richieste/media/R006/punti_segreti_*.md`: se le scene vengono rigenerate rilanciare lo script; **Treno > Segreti > Verifica punti** confronta le coordinate; **Treno > Segreti > Prova automatica / Foto di posizionamento** avviano `SegretiSelfTest`, flag `-segretitest` / `-segretifoto`):
  `{ testi{...tutti i testi dell'interfaccia dei segreti...}, stanze[{scena, nome, completo[]}], segreti[{id, scena, tipo: collezionabile|enigma, nome, scheda, ricompensa, punto, x,y,z, ordine}], pezzi[...], enigmi[EnigmaDef + scena + segreto + punto/dx/dy/dz], campane[...], uova[...], gettoni[{id, nome, descrizione}] }`
  - `stanze[].completo` = condizioni di "stanza principale completata" (prefissi: `fb:<capitolo>` capitolo completato, `ris:<chiave>` enigma risolto, `flag:<nome>`, `segreto:<id>`, altrimenti gettone di MondoStato). Prima l'Indizio **tace** sui segreti; dopo, se ne resta uno, mostra solo `testi.aiutoLieve` ("In questa stanza c'è ancora qualcosa…"). Mai frecce, aloni o indicatori sulla mappa.
  - `segreti[]`: la lista che conta (pagina Segreti del quaderno con caselle per stanza, es. "Basso 5/5"; contatore solo nel menu pausa, mai nell'HUD). Un collezionabile si trova raccogliendo il suo pezzo (`pezzi[].segreto`); un enigma segreto quando si risolve il suo `enigmi[]` (`segreto`) o raccogliendo il pezzo premio. `ricompensa` = pagina "dietro le quinte" (solo narrativa).
  - `pezzi[]` (oggetti piccoli di primitive MondoFab, `parti[]` come negli enigmi): `{id, scena, segreto, ruolo: raccogli|indizio|lettura|apri|decoro, punto, dx,dy,dz, x,y,z, yaw, rx, rz, appoggio: suolo|fisso, scala, prompt, titolo, msg, produce[], richiede[], richiedeMsg, appareQuando[], nascondiQuando, scorriQuando + sx,sy,sz (si sposta), scorriOggetto (oggetti di scena che scorrono insieme, copiati perche' static), raggio, parti[]}`. `indizio` = biglietto (gettone `id`, non conta nel totale); `apri` = si toglie e dietro compare altro; `lettura` = scritta da leggere (rileggibile).
  - `campane[]`: campana di gesso (`caselle[{n,x,z,w,d}]`, `sequenza` di numeri di caselle, `richiede` = gettone di lettura del graffito, `produce`); ci si ferma `sosta` secondi su ogni casella; prima del graffito e' solo un gioco, ordine sbagliato = ricomincia con `testi.campanaSbagliata` (mai punizioni).
  - `uova[]` (easter egg): `{id, scena, tipo: violino|scarpe, punto, dx,dy,dz, x,y,z, quando[] (condizioni), raggio, volume, nota}`. Aggancio per R016: a fine gioco `SegretiStato.ImpostaFlag("finale_visto")` (violino lontano al gradino della chiesetta); `SegretiStato.Flag("segreti_tutti")` e' vero quando si trovano tutti i segreti (D1: scena extra del finale); aggancio Cappotto: quando `flashbackCompletati` contiene `flashback_cappotto` compare il paio di scarpe piccole in piu' sul filo.
  - **Aiuto e rientro:** a stanza principale completata e con segreti da trovare compare da solo, una volta per stanza (flag `aiuto_<scena>`), `testi.aiutoLieve` (SegretiMonitor; nel ricordo all'apertura dell'uscita). **Rivivi il ricordo** (scheda Ricordi della latta, testi `ui_menu.json` `ricordi.rivivi`): `GameManager.RivediRicordo` rientra in un ricordo completato senza cutscene, senza "nuovo oggetto" e senza alterare gli enigmi principali (`GameManager.Rivivendo`). `Interactable.Penalita`: i prompt dei segreti cedono il passo agli interattori principali.
  - **Nuova stanza (R012-R016)**: aggiungere un blocco `stanze`, i `segreti`, i `pezzi` e un `enigmi[]` in `genera_segreti.py` (punti da un rapporto `punti_segreti_<scena>.md` generato dallo SceneBuilder, da registrare in `MD`), rilanciarlo; nessun codice da toccare.
  - `SaveData` (retrocompatibile, salvataggi vecchi = liste vuote): `segretiTrovati` (id dei segreti trovati), `segretiFlag` (`finale_visto`, `segreti_tutti`, ...). I gettoni dei pezzi (biglietti, numero del graffito, `campana_ok`) stanno in `oggettiEnigma`, le soluzioni in `enigmiRisolti` come `segreti/<id>`.
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
- Stracci (R012, branch `richiesta/R012-ricordo-stracci`): sviluppato (scena 40_FB_Stracci, 5 enigmi, cutscene `cutscene_stracci`).
- Violino (R015, branch `richiesta/R015-ricordo-violino`): codice scritto senza Unity (scena 70_FB_Modena da costruire, 5 enigmi sul suono, cutscene `cutscene_violino`); oggetto `spartito` nel cassettino del basso dopo il cappotto.
- Cappotto (R014, branch `richiesta/R014-ricordo-cappotto`): codice scritto senza Unity (scene 60_FB_Stazione e 61_FB_Vagone da costruire, 5 enigmi, cutscene `cutscene_cappotto`).
- Volantino (R013, branch `richiesta/R013-ricordo-volantino`): codice scritto senza Unity (scena 50_FB_Volantino da costruire, 5 enigmi, cutscene `cutscene_volantino`); il volantino sblocca `stazione`. Oggetti futuri: volantino del treno, cappotto, violino, finale. **Ok dell'utente (2026-11-06)** a svilupparli tutti
  tramite le richieste R012–R016 (schema degli enigmi di ogni capitolo da approvare prima dello sviluppo).
