# R023 – Menu iniziale e inventario rifatti, al livello dei giochi più belli

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 4fd772d (dopo R022). **Il codice attuale vince** |
| **Dipende da** | R001 (UI d'epoca: carta, ottone, ceralacca, `UITween`/`UIFx`, già integrata), R007+R021 (look "pellicola neorealista", per lo sfondo del menu), R008 (impostazioni grafiche: la schermata Impostazioni la riusa). Da fare **dopo R008** |
| **Priorità** | Alta: è la prima cosa che vede chi apre il gioco |

## 1. Cosa ha chiesto l'utente (parole sue)
> Menu migliorato tantissimo, UI nuova, come nei giochi più belli del mondo.
> Come menu intendo il menu iniziale di appena si apre il gioco e l'inventario.

Ambito: **schermata del titolo / menu iniziale** e **inventario**. HUD, pausa e dialoghi restano quelli di R001,
salvo adattarli allo stesso linguaggio visivo se diventano incoerenti.

## 2. Obiettivo
Livello dei menu dei giochi narrativi di riferimento (cura cinematografica, transizioni morbide, suono, profondità),
**con un'identità nostra**: Napoli 1946, oggetti fisici dell'epoca, neorealismo. Niente di copiato da altri giochi,
dal film o dal libro (CLAUDE.md). Tono serio, mai cartoon.

## 3. Situazione di partenza
- `Scripts/UI/TitleScreen.cs` (R001: titolo a stampa tipografica, polvere, pulsanti a cascata, "Continua"/"Nuova partita").
- `Scripts/UI/InventoryPanel.cs` (R001: scatola di latta / quaderno), `UIFactory`, `UIRoot`, `Fx/` (`UITween`, `UIFx`).
- UI uGUI costruita da codice; testi dei menu nel codice o nei JSON (verificare: i testi narrativi vanno nei JSON).

## 4. Specifica (proposta)
### 4.1 Menu iniziale "vivo"
- **Sfondo 3D in tempo reale** al posto di uno sfondo piatto: una scena-diorama (scena dedicata leggera o
  una zona dell'hub) col look di R007/R021: vicolo all'imbrunire, bucato che si muove, luce di una finestra, fumo
  lontano di una locomotiva; camera lenta (dolly/carrellata Cinemachine), grana e vignetta della "pellicola".
  Variante WebGL/Bassa: stessa inquadratura con meno effetti o un fotogramma pre-renderizzato.
- **Sequenza d'apertura** (saltabile con qualunque tasto): logo/scritta del gioco che si imprime come su una
  locandina o un biglietto del treno, musica del tema (R002) che entra, poi il menu.
- **Voci del menu come oggetti**: Continua (con anteprima del salvataggio: capitolo, luogo, tempo giocato),
  Nuova partita, Capitoli/Ricordi (sbloccati), Impostazioni (R008), Crediti, Esci (solo .exe).
  Ogni voce reagisce a mouse, tastiera, gamepad e touch: evidenziazione, suono, micro-animazione; focus sempre visibile.
- **Transizioni cinematografiche** tra menu e sottomenu (la camera del diorama si sposta, la carta scorre),
  e dal menu al gioco (dissolvenza sulla scena, nessuno stacco brusco).
- Conferme (es. "Nuova partita sovrascrive il salvataggio") come foglietti/timbri, non popup generici.

### 4.2 Inventario
- **Oggetto fisico in 3D**: la scatola di latta (o il quaderno) si apre in primo piano davanti alla camera,
  con gli oggetti raccolti come **modelli 3D** (non icone) che si possono ruotare ed esaminare; luce calda da interno.
- Scheda dell'oggetto: nome, descrizione, **significato** e tema civico (dai JSON `oggetti.json`) scritti come
  annotazione a mano/a macchina sul quaderno; ricordo collegato (flashback) e suo stato.
- Sezioni: Oggetti · Ricordi (flashback sbloccati) · Quaderno (appunti, indizi, segreti di R011) · Mappa (R019).
- Navigazione completa con tastiera/gamepad/touch; apertura e chiusura animate (< 0,4 s, mai lente);
  il gioco va in pausa sotto, con sfocatura/vignetta dello sfondo.

### 4.3 Qualità e regole
- Tipografia d'epoca leggibile (anni '40), contrasti e dimensioni conformi alle opzioni di accessibilità
  (scala testo, alto contrasto, riduzione movimento che disattiva le animazioni non essenziali).
- Suoni UI coerenti (carta, latta, ottone, macchina da scrivere) da `audio.json`; nessun asset NC/ND.
- Tutto generato da codice/script editor, riproducibile; testi nei JSON; 1280×720 e 16:9/16:10/4:3, LIM, Chromebook.
- Prestazioni: menu ≥ 60 FPS a Alta; su WebGL Bassa fluido; peso aggiunto annotato (LFS).

## 5. Ordine di lavoro consigliato
1. 2-3 bozze (mockup/screenshot) del menu iniziale e dell'inventario → **ok dell'utente col pannello domande**.
2. Diorama del menu + sequenza d'apertura. 3. Voci, sottomenu, transizioni. 4. Inventario 3D.
5. Accessibilità, gamepad/touch, WebGL. 6. Video/GIF e screenshot finali.

## 6. Domande aperte (chiedere col pannello prima di sviluppare)
- Inventario: **scatola di latta** o **quaderno di scuola** come oggetto principale (o latta per gli oggetti + quaderno per gli appunti)?
- Sfondo del menu: **vicolo di Napoli** (presente) o **binario/treno alla stazione** (il viaggio)?

## 7. Criteri di accettazione
Stato aggiornato il 2026-11-08 dopo la revisione e le prove sulla build Windows (vedi "Revisione e prove" nel Rapporto).
- [x] Bozze approvate dall'utente (menu = bozza A, inventario = bozza A).
- [x] Menu iniziale con sfondo 3D vivo, sequenza d'apertura saltabile, transizioni, anteprima del salvataggio. Provato nella build Windows (sfondo vivo a Alta e Media; fotogramma fisso a Bassa/WebGL solo in editor).
- [x] Inventario 3D con oggetti esaminabili e sezioni Oggetti/Ricordi/Quaderno/Mappa (la Mappa e' un segnaposto dichiarato in attesa di R019).
- [ ] Mouse, tastiera, gamepad, touch; accessibilita' rispettata; 60 FPS a Alta, fluido su WebGL Bassa. **Parziale**: mouse e tastiera provati (eventi dell'Input System su dispositivi virtuali); gamepad e touch NON provati con un dispositivo vero; "riduci movimento" gestito ma non provato nella build; alto contrasto e scala del testo non esistono ancora nel progetto; 60 FPS a Alta superati (203 FPS medi, 75 al 1% basso, build di sviluppo); la build WebGL e' stata fatta ma non avviata in un browser.
- [ ] Console pulita; GIF/screenshot in `Docs/richieste/media/R023/`; l'utente ha approvato il risultato. Console dell'editor pulita (nessun errore o avviso) e screenshot presenti; **manca l'approvazione dell'utente sul risultato finale**.

## Rapporto
_Compilato il 2026-11-08 (PC casa). Scelte dell'utente: menu = bozza A (locandina), inventario = bozza A (latta a sinistra, quaderno a destra); sfondo = vicolo dell'hub all'imbrunire._

### Fatto
- **Menu iniziale** (`TitleScreen`, `MenuBackdrop`): il vicolo dell'hub si carica in additivo sotto la scena del titolo (senza giocatore), luce calda e bassa, nebbia, Volume con vignetta, **pellicola di R007 vera** (passo a schermo intero a meta' intensita': `GraphicsPresets.ApplyPellicola` la accende anche quando `GameManager.MenuBackdrop` e' vero), finestre accese con sfarfallio di lampada a petrolio, panni stesi che ondeggiano (i `Panno_*` dell'hub), fumo lontano di una locomotiva, carrellata lenta con una **CinemachineCamera** (priorita' 1000, mossa da script: non e' una spline dolly). Titolo che si imprime lettera per lettera su una locandina, voci a cascata, anteprima di "Continua" (ultimo oggetto, luogo, oggetti nella latta, minuti di gioco: nuovo campo `SaveData.tempoGioco`). Sequenza d'apertura saltabile con qualunque tasto, clic, tocco o pulsante del gamepad; la musica del titolo (audio.json) entra come prima con la dissolvenza di 2,5 s.
- **Voci**: Continua, Nuova partita (conferma su un foglietto con timbro ATTENZIONE, non un popup), Ricordi (elenca i ricordi di `oggetti.json`: rivissuto con titolo e introduzione da `flashback_*.json`, oppure "Ricordo da trovare" con luogo), Impostazioni (apre il `SettingsPanel` di R008), Crediti (da `CREDITS.md` tramite `crediti.json`, 77 voci scorrevoli), Esci (solo fuori da WebGL e dai dispositivi mobili). Sottomenu con la carta che scorre; il lancio del gioco sfuma la locandina mentre parte la dissolvenza di `SceneLoader`.
- **Inventario** (`InventoryPanel`): latta d'epoca aperta su un tavolo, ripresa da una camera dedicata in un set (prefab `Resources/Inventario/StudioLatta`, creato e distrutto a ogni apertura); smalto verde consumato con ruggine e graffi, coperchio in piedi con una locomotiva a vapore originale nel cartiglio, ammaccatura, normal map martellata; texture generate da codice, 1024 px al massimo. Le **scarpe** sono un modello 3D vero fatto in Blender headless (`ArtSource/Models/Scarpe/genera_scarpe.py`, 3584 triangoli per il paio), si girano trascinando o con lo stick destro. Ogni oggetto puo' avere il campo `modello` in `oggetti.json` (prefab in `Resources/Modelli`); senza modello si vede la sua icona. Quaderno con linguette **Oggetti** (significato e tema civico), **Ricordi**, **Quaderno** (appunti e indizi degli enigmi risolti, sezione "Segreti" agganciata a `QuadernoSegreti.Provider` per R011) e **Mappa** (segnaposto dichiarato in attesa di R019). Il gioco resta fermo sotto (input bloccato), il set copre la scena, apertura e chiusura = dissolvenza in nero di 0,13 + 0,15 s (circa 0,3 s); alla chiusura si ripristinano camera, sole, nebbia e riflessi.
- **Navigazione**: voci e controlli sono `Button` con l'EventSystem esistente (mouse, tastiera, gamepad, tocco), il fuoco e' sempre visibile (matita rossa nel menu, anello rosso nell'inventario) e torna da solo sull'ultimo controllo se si clicca nel vuoto. Menu: frecce/W/S, Invio, Esc o B per tornare. Inventario: Q/E o LB/RB per le pagine, frecce/A/D o croce direzionale per gli oggetti, Esc/I o B per chiudere.
- **Accessibilita'**: "riduci movimento" (e gli effetti ridotti di R001) fermano carrellata, panni, fumo e pellicola, accorciano la sequenza d'apertura e fermano rotazione e galleggiamento dell'oggetto. Alto contrasto e scala del testo **non esistono ancora** nel progetto, quindi non sono stati toccati.
- **WebGL e Bassa**: non si carica la scena viva; si mostra un fotogramma fisso (`Resources/Menu/sfondo_vicolo.jpg`, catturato dal menu vivo) con un lentissimo zoom. A grafica Media le luci delle finestre sono 3 invece di 5.
- Testi: `ui_menu.json` (menu, conferma, ricordi, crediti, inventario), `crediti.json` (generato da `CREDITS.md` con Treno > R023 > Genera crediti.json). Bozze e menu "Bozza" tolti.

### Misure (editor, RTX 4060 Ti, FPS senza limite di fotogrammi)
- Menu vivo, preset Media: 418 FPS medi (mediana 495, 1% basso 34); menu con sfondo fermo (Bassa): 698 medi (mediana 843, 1% basso 45); inventario aperto, preset Media: 593 medi (mediana 707, 1% basso 40).
- **Alta non misurata**: il target di build attivo dell'editor e' Web e li' esistono solo i livelli di qualita' Bassa e Media (`GraphicsPresets.ResolveLevel` scende al piu' vicino). Va rifatta sul target Windows. **Nessuna build WebGL o Windows di prova** per il menu: il percorso WebGL (sfondo fermo) e' provato solo in editor forzando il preset Bassa.
- Proporzioni provate nel Game view: 16:9 (1280x720), 16:10 (1280x800) e 4:3 (1024x768): menu e inventario restano leggibili e dentro lo schermo (`prop_*.jpg`).
- Peso aggiunto in LFS circa 3,6 MB (3 PNG della latta, FBX e .blend delle scarpe, fotogramma fisso, GIF); mesh e prefab sono YAML fuori da LFS (circa 1 MB). LFS resta intorno a 820 MB.

### Limiti e cose aperte (aggiornati il 2026-11-08)
- Il vecchio diorama del titolo e' stato tolto: `01_Titolo` e' ora solo camera + `TitleScreen` (menu **Treno > Scene > Ricostruisci solo il Titolo**, `SceneBuilder.BuildTitolo`), niente luci da cuocere; le 5 texture di lightmap e reflection probe della vecchia scena sono state tolte dal repo (restano nello storico LFS).
- Sfocatura vera dello sfondo non fatta: il set della latta copre la scena con vignetta e grana invece di sfocarla.
- Gamepad e touch non provati con un dispositivo vero. Le prove automatiche passano da dispositivi virtuali dell'Input System (tastiera e mouse), non da un gamepad.
- Alto contrasto e scala del testo non esistono nel progetto: non toccati.
- Le scarpe sono un modello fatto a mano ma semplice (tomaia lofted, nessuna texture dedicata: usano i materiali del cuoio gia' presenti).
- Gli screenshot catturati con `ScreenCapture` vanno scritti su un `RenderTexture` Linear: con sRGB risultano schiariti (errore trovato e corretto durante il lavoro).
- Il crash intermittente del `DecalDrawSystem` di URP (giocatore Windows, preset con decal) esiste anche senza R023, ma il menu vivo aggiungeva un caricamento in piu' della stessa scena: vedi sotto.
- Il tempo di gioco e' salvato a ogni uscita, pausa dell'app e ogni 30 s; su WebGL non e' stato aggiunto un hook JavaScript per la scheda nascosta (si affida a `OnApplicationFocus/Pause`, che nei browser recenti scattano alla perdita di visibilita', ma non e' provato).

### Revisione e prove (2026-11-08, PC casa)
Correzioni della revisione, tutte fatte:
- **I1** Il clic, Invio o A che salta l'intro non attiva piu' una voce: durante l'intro la locandina non e' interattiva (`CanvasGroup`), dopo la fine le voci ignorano i comandi per 0,25 s e finche' il tasto non e' rilasciato.
- **I2** Con "riduci movimento" le beat gia' passate si segnano come partite senza suono (e ogni beat in ritardo di oltre 0,3 s non suona).
- **I3** La scheda oggetto mostra descrizione (testo dritto), significato (corsivo), ricordo collegato con il suo stato (Da rivivere / Rivissuto) e il tema civico; gli oggetti da trovare si possono scegliere e mostrano "cerca vicino a <luogo>" senza svelare il nome. Etichette in `ui_menu.json`.
- **I4** Vecchio diorama tolto da `SceneBuilder`; `MenuBackdrop` non spegne piu' le root della scena del titolo; `01_Titolo` tolta anche da `LookBuilder`.
- **I5** Nel menu niente pioggia (`MeteoController.Sereno()`: particelle, gocce e spruzzi spenti, suolo asciutto), niente decal e niente pozzanghere.
- **M1** Il tempo di gioco si salva in `OnApplicationQuit/Pause/Focus` e ogni 30 s, non conta con pausa, inventario o impostazioni aperti; la riga del tempo nell'anteprima sparisce se il tempo e' 0.
- **M2** Lo sfondo vivo rispetta la preferenza Nebbia. **M3** il materiale del fumo e' un'istanza (`new Material`), l'asset non cambia.
- **M4** Chiuse le Impostazioni il fuoco torna sull'ultima voce; un oggetto selezionato ma non piu' attivo conta come fuoco perso.
- **M5** Ricordi dell'inventario: titolo e descrizione restano nascosti finche' l'oggetto non e' stato trovato (come nel sottomenu del titolo).
- **M6** Valori di ripiego in tutte le classi `Ui*` e `UiMenuFile.Normalizza()` (blocchi o liste mancanti o null in `ui_menu.json` non fanno crollare la UI).
- **M7** "Valigia" diventa "Latta" nell'HUD e nella pausa (testo da `ui_menu.json`, nuova icona `UIArt.IconLatta`).
- **M8** Un solo `AudioListener` attivo: quello della camera dell'hub mentre c'e' lo sfondo vivo (verificato in partita: 1).
- **M11** Commento corretto (API reale `QuadernoSegreti.Provider`).

Problemi trovati e risolti durante le prove (non erano nell'elenco della revisione):
- **W/S saltavano una voce su due** nel titolo: il menu gestiva W/S a mano e in piu' la navigazione dell'EventSystem li legge come frecce. Con 6 voci erano raggiungibili solo quelle di posto pari. Tolto il gestore manuale.
- **Esc non chiudeva le Impostazioni aperte dal titolo** (in quella scena `UIRoot` ignora i tasti di gioco). Ora Esc o B le chiudono ovunque.
- **Testo dei Ricordi a una riga sola, piccolo e tagliato** (c'era gia' nello screenshot approvato `inventario_ricordi.jpg`): i testi con ridimensionamento automatico erano creati con il pannello spento. Ora i contenuti si costruiscono a pagina accesa e si rifa il layout (`UIFactory.RefreshText`).
- **Crash nativo del player Windows** (`DecalDrawSystem.Draw`, memcpy in URP) entrando nel gioco dal titolo: tre crash su quattro tentativi prima delle correzioni. Ora il vicolo del menu si scarica a schermo nero prima del caricamento vero (`MenuBackdrop.Congeda`), i decal si spengono prima di ogni cambio scena (`SceneLoader`) e nel menu non ci sono decal. Dopo le correzioni: 5 giri completi di prova senza crash. Non e' una garanzia: il bug e' di URP e si manifestava gia' nel benchmark di R007.

Come ho provato davvero (build Windows di sviluppo in `../treno-build/Win`, fuori dal repo; dall'editor non era possibile: con la finestra in secondo piano l'editor non avanza i fotogrammi):
- Prova automatica `Scripts/Core/MenuSelfTest.cs` (solo editor e build di sviluppo, argomento `-menutest`): tasti e mouse sono eventi dell'Input System su una tastiera e un mouse virtuali (gli altri dispositivi vengono disattivati, cosi' i tasti o il gamepad del computer non entrano nella prova). Salva e rimette a posto il salvataggio. Flusso: Titolo, **salto dell'intro con un clic sopra "Nuova partita"** (non parte nulla, il menu resta aperto), Ricordi, Impostazioni (Esc chiude, il fuoco torna sulla voce), Crediti, Nuova partita con salvataggio (conferma, Annulla con Esc), Continua, gioco, un solo AudioListener, il tempo di gioco avanza, I apre l'inventario, il tempo si ferma con inventario e pausa aperti, Esc chiude, Esc apre la pausa, Torna al titolo con conferma, e **secondo giro: salto dell'intro con Invio** senza lanciare il gioco.
- Risultato ultimo giro: **31 PASS, 0 FAIL**; i giri precedenti dopo le correzioni dei crash: 3 su 3 senza crash (un giro con un solo FAIL, dovuto alla pausa che il gioco apre da solo quando con la finestra in secondo piano cade il blocco del cursore, `OpenPauseFromLockLoss`; la prova ora la richiude).
- Console dell'editor pulita dopo ogni compilazione (nessun errore, nessun avviso).
- La prova NON ha usato i tasti del computer ne' un gamepad vero.

FPS del menu iniziale (build Windows di sviluppo con overlay FPS, RTX 4060 Ti, 1280x720, vsync spento, 20 s a menu fermo dopo l'intro, sfondo vivo):

| Preset | FPS medi | 1% basso | frame massimo |
|---|---|---|---|
| Alta | 203,3 | 74,9 | 21,7 ms |
| Media | 218,9 | 87,7 | 24,2 ms |

(Il benchmark dei frame ora ha anche la voce `01_Titolo`: `-benchmark -benchmarkscene 01_Titolo -benchmarkpresets 2,1`.) Bassa/WebGL non misurati in build: nel titolo usano il fotogramma fisso.

Peso: build Windows di sviluppo 475,3 MB (comprende i simboli di sviluppo e tutte le scene); build WebGL leggera (preset Bassa, Gzip, `WebGLBuilder` con `TRENO_WEBGL_OUT` fuori dal repo) **58,2 MB** in 463 s, compilata senza errori; **non avviata in un browser** e non pubblicata su gh-pages. LFS: **nessun file nuovo in LFS** in questa revisione (solo codice, JSON, scena YAML e screenshot JPG); stima ancora intorno a 820 MB su 1 GB, sotto la soglia di 850 MB.

Screenshot (dalla build Windows, 1280x720 JPG, con l'overlay FPS e la scritta "Development Build" della build di sviluppo): `media/R023/build_titolo.jpg`, `build_titolo_ricordi.jpg`, `build_inventario_scheda.jpg` (scheda con descrizione, significato, ricordo "Da rivivere" e tema), `build_inventario_ricordi_bloccato.jpg`, `build_inventario_ricordi_da_rivivere.jpg`, `build_inventario_ricordi_rivissuto.jpg`.

### Ritocchi dopo la revisione (2026-11-08, ultimo giro)
- **Linguetta attiva dell'inventario**: il bordo rosso del fuoco restava su "Oggetti" anche con un'altra pagina aperta. Ora `VaiA` sposta il fuoco sulla linguetta della pagina mostrata (Q/E, mouse, gamepad) per tutte e 4 le pagine. Verificato in build (`build_inventario_ricordi_rivissuto.jpg`: bordo su "Ricordi").
- **Scarpe in cuoio liscio consumato**: il materiale del cuoio aveva una trama a vimini. Ora `R023Builder.GeneraCuoioScarpe` (menu Treno > R023 > Genera prefab scarpe) costruisce due texture procedurali da 512 px sulla mesh vera, disegnata nello spazio UV con altezza e posizione lungo il piede: grana fine, pieghe nel punto dove il piede si piega, graffi chiari, macchie, punta sbiadita, suola scurita. Nuovo materiale PBR `M_Cuoio_Scarpa_Liscio` (URP/Lit, metallic 0, smoothness 0,32, normal map 0,8), usato solo dal prefab `Resources/Modelli/scarpe`. Il vecchio `M_Cuoio_Scarpe` non e' stato toccato. Peso: due PNG da circa 0,17 e 0,55 MB in LFS (circa 0,7 MB in totale), nessun'altra texture nuova.
- Screenshot rigenerati dalla build di sviluppo: `build_inventario_scheda.jpg`, `build_inventario_ricordi_rivissuto.jpg` (e gli altri `build_*`). Prova automatica ancora 31 PASS, 0 FAIL.
- Nota di metodo: una build Windows fatta subito dopo una build WebGL puo' includere solo 2 asset URP (senza Alta e Ultra) e la scena risulta piu' scura e senza luci delle finestre. Prima di costruire per Windows bisogna avere il target Windows gia' attivo (messaggio "4 URP assets included in build").

