# R002 – Effetti sonori e musica d'epoca (tema del violino)

| | |
|---|---|
| **Scritta da** | Claude, PC scuola (senza Unity), 2026-11-05 |
| **Per** | Claude, PC casa (Unity 6000.6.7f1 + MCP for Unity) |
| **Quando** | Dopo aver chiuso il lavoro in corso. Meglio **dopo R001** (vedi §8), ma è indipendente |
| **Base** | `main` @ ead0b9b. Il codice audio potrebbe essere cambiato dopo: **il codice attuale vince** |
| **Priorità** | Alta: oggi il gioco è quasi muto (solo le voci delle cutscene) |

## 1. Cosa ha chiesto l'utente (parole sue)
> aggiungi anche nuovi effetti sonori e musica d'epoca (soprattutto violino, siccome bisogna riprendere
> la storia de "Il treno dei bambini" film e libro)

Obiettivo: dare al gioco un **paesaggio sonoro completo**, cioè ambienti, passi, oggetti ed enigmi, e una **colonna sonora
d'epoca** costruita attorno al **violino**. Il violino è lo strumento legato ad Amerigo nella storia ed è già
previsto come oggetto futuro (`violino` in CLAUDE.md). La musica deve **richiamare l'atmosfera** dell'opera,
non copiarla.

## 2. Situazione di partenza (al 5/11)
- `Core/AudioManager`: solo volume globale (`AudioListener.volume`, salvato in `SaveData.volume`) e una
  `AudioSource` per le voci delle cutscene (`PlayVoice`/`StopVoice`, usata da `CutscenePlayer` via `VoiceLibrary`).
- Nessuna musica, nessun effetto sonoro, nessun ambiente.
- R001 (UI) prevede un `UISfx` per i suoni dell'interfaccia: deve passare dal canale **Effetti** di questa richiesta.

## 3. Vincoli (non negoziabili)
1. **Diritti d'autore: nessun brano, tema o registrazione del film o della sua colonna sonora**, nemmeno
   "rifatto a orecchio" o con le stesse note. Il richiamo all'opera passa da strumento (violino), epoca
   (Napoli 1946), stile (canzone napoletana, melodia malinconica) ed emozioni, **mai** dalle melodie.
2. Fonti ammesse per la musica, in ordine di preferenza:
   a. **Musica originale** scritta per il gioco (vedi §5.1, "Tema di Amerigo");
   b. **composizioni di pubblico dominio** (autori morti da più di 70 anni) in **registrazioni** anch'esse
      libere: pubblico dominio, CC0 o CC BY. Esempi: Musopen, Wikimedia Commons, Internet Archive con licenza
      verificata **file per file**. Attenzione: la composizione può essere libera e la registrazione no;
   c. registrazione fatta dall'utente o da compagni (vedi "Domande aperte").
3. Effetti sonori: **CC0** (es. Freesound filtrato su CC0, Kenney, Sonniss GDC con licenza royalty-free) oppure
   generati da codice. **Ogni file** va elencato in `Assets/Audio/CREDITI.md` con fonte, autore, licenza e link.
   Niente file "trovati in giro" senza licenza chiara.
4. **WebGL:** niente effetti DSP in tempo reale. In WebGL non funzionano i filtri audio (`AudioLowPassFilter`,
   riverbero, ecc.) né gli effetti dell'`AudioMixer`. Quindi effetti come grammofono, riverbero o lontananza vanno
   **applicati ai file** prima, oppure ottenuti con volume e crossfade. Bisogna rispettare la **politica di autoplay** dei browser: niente
   audio prima del primo clic/tocco. Il titolo deve mostrare "Tocca o clicca per iniziare" se l'audio è bloccato.
5. **Peso:** musica in `Streaming` o `Compressed In Memory` con qualità adatta (mono dove basta, 22–32 kHz per
   ambienti e effetti). Budget indicativo: **musica ≤ 10 MB, effetti + ambienti ≤ 5 MB** in più nella build.
   Annotare la dimensione prima e dopo.
6. **Contenuti fuori dal codice** (regola del progetto): quale musica e quale ambiente suonano in ogni scena
   o stato va in un JSON (es. `Resources/Data/audio.json`), non cablato nel codice.
7. Le **voci del doppiaggio hanno sempre la precedenza**: mentre una voce parla, la musica si abbassa
   (ducking) e gli ambienti pure.
8. File audio in `Assets/Audio/...` tracciati con **Git LFS** (come `Voci/`). Controllare `.gitattributes`.

## 4. Architettura proposta
Estendere `AudioManager` (o affiancargli classi in `Scripts/Audio/`, namespace `TrenoBambini.Audio`):

| Elemento | Compito |
|---|---|
| **Canali** | `Musica`, `Ambiente`, `Effetti`, `Voci`, ognuno con volume proprio × volume generale. In `SaveData`: `volMusica`, `volAmbiente`, `volEffetti`, `volVoci` (mantenere `volume` come generale per compatibilità con i salvataggi esistenti) |
| **MusicPlayer** | due `AudioSource` per **crossfade** (1,5–3 s) tra brani; brani in loop con eventuale intro; "stinger" (frammenti brevi) sopra la musica; pausa che abbassa la musica senza fermarla |
| **AmbientPlayer** | 1–2 loop d'ambiente per scena con crossfade + **suoni sparsi casuali** (campane lontane, gabbiani, voce di venditore senza parole comprensibili, carretto, porta che sbatte) a intervalli casuali e posizione stereo casuale |
| **Sfx** | `Sfx.Play(id)` con **variazioni** (più file per lo stesso id, pitch ±5%, volume ±10%), limite di voci contemporanee, pool di `AudioSource`; versione 3D per suoni nel mondo (porte, oggetti) |
| **Ducking** | quando `VoicePlaying` la musica scende a ~35% e l'ambiente a ~60% con rampe morbide |
| **AudioLibrary** | come `VoiceLibrary`: menu `Treno ▸ Audio ▸ Aggiorna libreria` che indicizza `Assets/Audio/{Musica,Ambienti,Effetti}` per id = nome file (es. `passo_pietra_01`, `passo_pietra_02` → id `passo_pietra`) |
| **audio.json** | mappa `scena/stato → musica + ambiente` e le regole degli stinger (vedi §6) |

Il menu Pausa (anche se restilizzato da R001) riceve **4 slider**: Generale, Musica, Ambiente, Effetti, Voci.

## 5. Musica

### 5.1 Il "Tema di Amerigo" (leitmotiv originale per violino)
- Una melodia **originale** breve (8–16 battute), malinconica ma con speranza, in **tonalità minore**
  (es. La minore o Re minore), andamento lento (♩ ≈ 66–76), con un'inflessione da canzone napoletana:
  sesta napoletana, appoggiature, piccoli portamenti del violino.
- **Scriverla come partitura** (MIDI + notazione testuale/ABC in `Assets/Audio/Musica/sorgenti/`) così che:
  - si possa **registrare dal vivo** (violino vero, vedi Domande aperte) → soluzione migliore;
  - in alternativa la si renda con un **campionatore di violino libero** (es. librerie gratuite con licenza che
    consenta l'uso nei giochi, verificare) con riverbero e "patina d'epoca" applicati al file.
- Il tema ritorna in **varianti** che seguono la storia:
  | Variante | Uso | Strumentazione |
  |---|---|---|
  | *Tema – solo* | titolo, finale | violino solo, molto riverbero, fruscio leggero |
  | *Tema – ricordo* | dentro i flashback | violino lontano + pianoforte, filtro "grammofono" applicato al file |
  | *Tema – scoperta* | stinger "oggetto ritrovato" (5–8 s) | violino che sale + arpa/celesta |
  | *Tema – vicolo* | sottofondo dell'hub, molto discreto | mandolino che accenna il tema, violino in risposta |

### 5.2 Brani per luogo/stato (proposta)
| Stato | Atmosfera | Note |
|---|---|---|
| Titolo | malinconia, attesa del viaggio | *Tema – solo*; sotto, sbuffo di vapore del treno lontano |
| Hub vicolo (Napoli 1946) | vita di quartiere povera ma viva | mandolino + chitarra + violino, tempo moderato; **molto basso**, l'ambiente conta di più |
| Interni (basso di Antonietta) | intimità, povertà, affetto | quasi silenzio: solo ambiente + 2–3 note sparse di violino ogni tanto |
| Flashback (escape room) | sospensione, ricordo | *Tema – ricordo*, loop lungo (≥ 2 min) senza stacchi evidenti |
| Enigma risolto | piccola gioia | stinger 2–3 s (pizzicato + campanella) |
| Oggetto ritrovato / cutscene finale | commozione | *Tema – scoperta* poi brano emotivo sotto le battute (con ducking) |
| Pausa | — | musica corrente al 30% con taglio delle alte (file alternativo o semplice volume) |

**Pubblico dominio utilizzabile** come brani di contorno, solo con **registrazione libera** e verificata:
canzoni napoletane dell'Ottocento (es. *Fenesta ca lucive*, *Santa Lucia*, *Funiculì funiculà*), pezzi per
violino di autori classici (es. Paganini, Bach). Ottimi per un **grammofono** o una radio nel mondo di gioco
(suono 3D che proviene da un oggetto nella scena), meno adatti come tema principale.
**Mai** brani che compaiono nella colonna sonora del film.

### 5.3 "Patina d'epoca"
Applicata ai file (non in tempo reale): leggero taglio delle frequenze basse e alte, fruscio/crepitio di
disco a 78 giri **molto discreto** (deve sembrare un ricordo, non disturbare), un po' di riverbero di stanza.
Tenere anche una versione "pulita" del tema per i momenti emotivi.

## 6. Effetti sonori e ambienti

### 6.1 Ambienti (loop + suoni sparsi)
| Scena | Loop | Suoni sparsi |
|---|---|---|
| Hub vicolo | brusio di quartiere lontano (senza parole comprensibili), vento leggero, mare lontano | campane di chiesa, gabbiani, bambini che giocano lontano, carretto su selciato, panni al vento, finestra che si chiude, richiamo di venditore (vocalizzo senza parole) |
| Basso di Antonietta | stanza silenziosa, rumore di strada attutito | pentola sul fuoco, sedia che scricchiola, orologio a pendolo |
| Flashback | "stanza del ricordo": tono basso e morbido | eco di passi, sussurri indistinti, un treno lontano molto attutito |
| Titolo / transizioni | stazione: vapore, fischio del treno lontano | campanella di stazione |

### 6.2 Effetti nel mondo
- **Passi di Amerigo** sincronizzati alla camminata e alla corsa, diversi per superficie: **basolato/pietra**
  (vicolo), **legno o mattonelle** (interni), **ovattati** (flashback). La superficie si può leggere da un tag o
  layer del collider oppure da un campo nella scena. Alternanza sinistro/destro, 4–6 varianti per superficie.
- Porte di legno (apri/chiudi), porta chiusa ("si aprirà più avanti": maniglia che non gira), raccolta oggetto
  (fruscio + "scintilla" dorata), oggetto da ricordo che brilla (ronzio cristallino molto leggero in 3D,
  più forte avvicinandosi), atterraggio dopo un salto se esiste.

### 6.3 Enigmi e transizioni
- Lucchetto: clic delle ruote, scatto di apertura, scossa; quiz: carta, timbro; ordina: carte trascinate;
  abbina: spago teso o allentato; nascosto: "trovato!" (campanella).
- Successo enigma: timbro + stinger musicale. Errore: suono **morbido** (legno sordo o carta), mai buzzer.
- Transizione "Ricordo": **proiettore a pellicola** che parte e un accordo di violino che sale;
  uscita: proiettore che si ferma.
- Cambio scena normale: fischio di treno o passi che si allontanano, a seconda del caso (definito in `audio.json`).

### 6.4 Interfaccia
Se R001 è già fatta, i suoni di `UISfx` passano al canale **Effetti** e vengono indicizzati da `AudioLibrary`.
Se R001 non è ancora fatta, preparare comunque gli id (`ui_click_ottone`, `ui_click_carta`, `ui_timbro`,
`ui_macchina_scrivere`, `ui_valigia_apri`, `ui_valigia_chiudi`, `ui_sigillo`, `ui_hover`) così R001 li trova.

## 7. Schema JSON proposto (`Resources/Data/audio.json`)
```json
{
  "scene": [
    { "scena": "01_Titolo",              "musica": "tema_amerigo_solo",   "ambiente": ["stazione_vapore"] },
    { "scena": "10_Hub_Vicolo",          "musica": "vicolo_mandolino",    "ambiente": ["vicolo_brusio", "vento_mare"],
      "sparsi": ["campane", "gabbiani", "carretto", "venditore", "bambini"], "intervallo": [6, 18], "superficie": "pietra" },
    { "scena": "20_Int_BassoAntonietta", "musica": "",                    "ambiente": ["interno_silenzio"],
      "sparsi": ["pendolo", "sedia", "pentola"], "intervallo": [10, 25], "superficie": "legno" },
    { "scena": "30_FB_Scarpe",           "musica": "tema_amerigo_ricordo","ambiente": ["ricordo_tono"], "superficie": "ovattato" }
  ],
  "eventi": {
    "enigma_risolto":   { "stinger": "stinger_risolto",   "sfx": "ui_timbro" },
    "oggetto_ritrovato":{ "stinger": "tema_amerigo_scoperta" },
    "ricordo_entra":    { "sfx": "proiettore_avvio" },
    "ricordo_esce":     { "sfx": "proiettore_stop" }
  },
  "volumi": { "musica": 0.6, "ambiente": 0.7, "effetti": 0.9, "voci": 1.0, "duckingMusica": 0.35, "duckingAmbiente": 0.6 }
}
```
Aggiornare `CLAUDE.md` (sezione "Schema JSON") con il formato definitivo.

## 8. Rapporto con R001
- Se si fa **prima R001**: lo slider Volume del quaderno di pausa diventa 5 slider; `UISfx` usa `Sfx`.
- Se si fa **prima R002**: `PausePanel` attuale riceve gli slider in forma semplice; R001 li restilizzerà.
- In entrambi i casi un solo sistema audio: niente `AudioSource` sparse nei componenti UI.

## 9. Ordine di lavoro consigliato
1. Canali + `SaveData` (compatibile con i salvataggi vecchi) + slider in pausa.
2. `AudioLibrary` + menu editor + `CREDITI.md` + controllo LFS.
3. `MusicPlayer` (crossfade, stinger, ducking) + `audio.json` + gestione autoplay WebGL.
4. **Tema di Amerigo**: partitura (MIDI/ABC) e prima versione audio (vedi Domande aperte); poi le varianti.
5. Ambienti + suoni sparsi per le 4 scene.
6. Passi per superficie + effetti nel mondo.
7. Effetti di enigmi e transizioni (+ id UI per R001).
8. Mixaggio: livelli bilanciati, nessun clip che satura, ducking sotto le voci delle cutscene.
9. **Build WebGL**: prova su Chrome (PC e Chromebook se possibile), verifica autoplay, peso, assenza di
   scatti nei loop; Rapporto.

## 10. Criteri di accettazione
- [ ] Canali Musica/Ambiente/Effetti/Voci con slider funzionanti e salvati; i vecchi salvataggi si caricano.
- [ ] Ogni scena ha musica e/o ambiente definiti in `audio.json`; i passaggi tra scene usano il crossfade.
- [ ] **Tema di Amerigo** originale al violino presente almeno nelle varianti *solo* (titolo) e *ricordo* (flashback),
      con la partitura salvata nel progetto.
- [ ] Stinger per enigma risolto e oggetto ritrovato.
- [ ] Passi per superficie su pietra, legno e ovattato, con varianti (non sempre lo stesso suono).
- [ ] Ambienti con suoni sparsi casuali nel vicolo e nell'interno.
- [ ] Effetti per porte, raccolta oggetti, enigmi (tutti e 5 i tipi), transizioni.
- [ ] Ducking sotto le voci del doppiaggio.
- [ ] Nessun audio prima dell'interazione in WebGL, nessun errore in console del browser.
- [ ] Loop senza "clic" udibili al punto di ripetizione.
- [ ] `Assets/Audio/CREDITI.md` completo (ogni file: fonte, autore, licenza, link) + crediti nel README.
- [ ] Nessuna musica del film o del suo compositore; brani di pubblico dominio solo con registrazioni libere.
- [ ] Peso aggiunto alla build entro il budget (annotare prima/dopo).

## 11. Domande aperte (per l'utente, da chiedere a casa)
1. **Qualcuno sa suonare il violino?** (tu, un compagno, un prof di musica). Registrare il *Tema di Amerigo*
   dal vivo (anche col telefono in una stanza silenziosa) darebbe un risultato molto più bello e
   "tuo" di qualsiasi campionatore, e sarebbe un ottimo contributo da citare nel progetto scolastico.
   Claude prepara la partitura semplice da leggere.
2. Va bene usare anche **canzoni napoletane di pubblico dominio** (es. da un grammofono nel vicolo), o
   preferisci solo musica originale?

## 12. Rapporto
**Implementata da:** Claude (sessione worktree, senza editor Unity aperto) · **Date:** 2026-11-05 · **Commit finali:** vedi `git log` del branch `richiesta-R002-fase2` (prefisso `R002:`)

### Fatto
- **Audio tutto sintetizzato da codice** (`Scripts/Audio/Synth.cs`, `SfxSynth.cs`, `MusicSynth.cs`): nessun file esterno, nessuna licenza, peso asset zero. Se in futuro si aggiunge un file con lo stesso id in `Assets/Audio/{Musica,Ambienti,Effetti}`, sostituisce il suono sintetico (menu **Treno > Audio > Aggiorna libreria**). Senza file e senza sintesi: silenzio, nessun errore.
- **Canali** Generale/Musica/Ambiente/Effetti/Voci: 5 slider nel quaderno di pausa (layout compattato per farli stare), salvati in `SaveData` (`volMusica`, `volAmbiente`, `volEffetti`, `volVoci`; `volume` resta il generale; i vecchi salvataggi caricano a 1).
- **MusicPlayer**: crossfade 2,5 s su due sorgenti, stinger sopra il brano, musica al 30% in pausa. **Ducking** (musica 35%, ambiente 60%, da `audio.json`) mentre parla una voce del doppiaggio.
- **Tema di Amerigo** originale (La minore, 72 bpm, sesta napoletana) in 4 varianti: `tema_amerigo_solo` (titolo), `tema_amerigo_ricordo` (flashback, con patina da grammofono), `tema_amerigo_scoperta` (stinger 8,5 s), `vicolo_mandolino` (hub) + `stinger_risolto`. Partitura ABC in `Docs/audio/tema_amerigo.abc`.
- **Ambienti** per le 4 scene con suoni sparsi casuali (campane, gabbiani, carretto, venditore, bambini, panni, finestra, pendolo, sedia, pentola, fischio di treno), tutto in `audio.json`.
- **Passi** per superficie (pietra, legno, ovattato), 4 varianti ciascuno, legati alla velocita reale (camminata/corsa), alternanza sinistro/destro.
- **Eventi** (`audio.json` > `eventi`): enigma risolto, oggetto ritrovato, ricordo entra/esce, porta apre/chiusa, raccolta, oggetto nascosto trovato, cambio scena. I suoni UI di R001 passano dal canale Effetti (`UISfx.Volume`) e sono indicizzati con gli id `ui_*`.
- **WebGL**: nessun suono prima del primo clic/tocco/tasto; compare il toast "Tocca o clicca per attivare l'audio." Nessun DSP in tempo reale (solo volumi).

### Scostamenti dalla specifica (e perche)
- Niente registrazioni: il tema e sintetizzato (violino modellato con armoniche, vibrato e portamento). **Una registrazione dal vivo resta la soluzione migliore** (domanda aperta 1): basta salvare il file `.ogg` come `Assets/Audio/Musica/tema_amerigo_solo.ogg` e aggiornare la libreria.
- Il loop `tema_amerigo_ricordo` dura circa 80 s (2 passate), non 2 minuti.
- Il basso di Antonietta non ha le "2-3 note sparse di violino": solo ambiente e suoni sparsi. Manca anche il ronzio 3D dell'oggetto-ricordo (`ricordo_brilla` e sintetizzato ma non agganciato a `ItemPickup`).
- Niente grammofono con canzoni napoletane di pubblico dominio (domanda aperta 2 non risolta, serve registrazione libera verificata).
- `CREDITI.md` e la partitura sono in `Docs/audio/` e non in `Assets/Audio/` (per non generare `.meta` fuori dall'editor).
- `eventi` in `audio.json` e una lista, non un oggetto.
- Il "ricordo_entra" suona sulla partenza della transizione di ricordo insieme al proiettore di R001.

### Fonti audio usate
Nessuna esterna. Tutto originale (vedi `Docs/audio/CREDITI.md` e riga in `CREDITS.md`). Nessuna musica del film o imitazione.

### File audio mancanti / slot vuoti
Nessun slot vuoto in `audio.json` (tutti gli id hanno la sintesi). Da sostituire con registrazioni quando possibile: tema al violino (vero violino), mandolino del vicolo, voce del venditore, campane.

### Domande aperte per l'utente
1. Qualcuno sa suonare il violino per registrare il tema? 2. Canzoni napoletane di pubblico dominio da un grammofono nel vicolo: si o no?

### Verifica (cosa e cosa no)
- Verificato: `dotnet build` di Assembly-CSharp e Assembly-CSharp-Editor (csproj di Unity adattati) senza errori, quindi API e riferimenti sono coerenti; `audio.json` e JSON valido.
- **Non verificato**: esecuzione nell'editor (nessun Unity aperto in questo worktree). Da controllare a mano: l'aspetto dei 5 slider nella pausa, l'ascolto e il bilanciamento di tema/ambienti/passi, assenza di scatti nei loop, il crossfade tra scene, l'attesa del gesto in WebGL, `read_console` pulita, la creazione dell'asset `AudioLibrary` (non necessario finche non ci sono file veri).

### Come testare
1. Aprire il progetto, `read_console` pulita. 2. Titolo: violino solo. Entrare nel vicolo: mandolino basso, ambiente, suoni sparsi, passi su pietra. Basso di Antonietta: passi su legno, quasi silenzio. 3. Raccogliere le scarpe: transizione di ricordo, tema "ricordo", passi ovattati. Risolvere un enigma: timbro + stinger. 4. Pausa: 5 slider; musica scende. 5. Cutscene: la musica si abbassa quando parla una voce. 6. Build WebGL: nessun audio fino al primo clic.

### Problema risolto: sintesi musicale sul thread principale
- **Decisione dell'utente:** pre-generare musica ed effetti pesanti come file audio; la sintesi resta solo come ripiego. Il violino resta sintetico.
- **Come:** `Tools/AudioGen` (progetto console dotnet, `genera.sh`) compila `Synth.cs`, `SfxSynth.cs` e `MusicSynth.cs` con un `Mathf` minimo e scrive i WAV mono 22050 Hz con gli stessi seed; `encode_ogg.py` li converte in Ogg Vorbis (livello 0.8, circa 38 kbps). Per rendere identici i file e il ripiego, il seme di `SfxSynth` non usa più `string.GetHashCode()` (non stabile) ma FNV-1a.
- **File** (in `Assets/Audio/`, Git LFS, 1,05 MB in tutto): Musica = tema_amerigo_solo 42,2 s, tema_amerigo_ricordo 82,2 s, tema_amerigo_scoperta 8,5 s, vicolo_mandolino 42,2 s, stinger_risolto 3,2 s; Ambienti = vicolo_brusio 12 s, vento_mare 13,5 s, ricordo_tono 12 s, stazione_vapore 12 s, interno_silenzio 8,5 s; Effetti = violino_sale 3,4 s, porta_apri 1 s, campane 8 s, venditore 2,6 s. Lunghezze ricontrollate dopo la codifica; giunzioni dei loop senza salti (differenza prima/ultimo campione < 0,005), picchi normalizzati come a runtime.
- **Caricamento:** `AudioLibrary.GetMusic/GetSfx` cercano prima i file (`PickFile`): se esiste, la sintesi non parte. `AudioManager.Awake` chiama `AudioLibrary.Preload()` (carica `Resources/AudioLibrary.asset` e quindi i clip compressi, ~1 MB). Import (primo import di ogni file): mono, Vorbis qualità 0,6, CompressedInMemory (musica e ambienti) o DecompressOnLoad (effetti), `loadInBackground`, niente Streaming (WebGL) - vedi `Editor/AudioImportSettings.cs`, che aggiorna anche la libreria dopo ogni import.
- **Ripiego** (file mancante): la musica si rende a blocchi (note, `ReverbSteps` e `PatinaSteps` cedono ogni 2048 campioni, budget 4 ms per frame) e logga un avviso; gli effetti brevi restano sincroni (pochi ms, fatta eccezione per quelli ora in file).
- **Da verificare in editor:** aprire il progetto (Unity crea i `.meta` dei nuovi file e delle cartelle: committarli), controllare che `Resources/AudioLibrary.asset` sia creato/aggiornato (altrimenti menu **Treno > Audio > Aggiorna libreria**) e committarlo; avviare e controllare nel Profiler che non ci siano picchi in ingresso scena e nessun avviso "uso la sintesi di ripiego"; ascoltare i brani (doppia codifica Vorbis: se la qualità non basta, alzare `s.quality` in `AudioImportSettings` o tenere i WAV come sorgenti); provare la build WebGL. Il blocco di `AudioLibrary.IsBuilding` nel monitor FPS di R001 resta valido solo per il ripiego.

### Misure
- Peso: ~1,05 MB di file `.ogg` (prima 0 byte); tempi di sintesi misurati su PC (dotnet, Release): solo 0,7 s, ricordo 3,5 s, vicolo 4 s, scoperta 0,5 s, ora non più eseguiti a runtime. Memoria: i clip restano compressi in memoria (musica/ambienti).
