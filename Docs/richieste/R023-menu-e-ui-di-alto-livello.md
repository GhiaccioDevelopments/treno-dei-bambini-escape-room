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
- [ ] Bozze approvate dall'utente.
- [ ] Menu iniziale con sfondo 3D vivo, sequenza d'apertura saltabile, transizioni, anteprima del salvataggio.
- [ ] Inventario 3D con oggetti esaminabili e sezioni Oggetti/Ricordi/Quaderno/Mappa.
- [ ] Mouse, tastiera, gamepad, touch; accessibilità rispettata; 60 FPS a Alta, fluido su WebGL Bassa.
- [ ] Console pulita; GIF/screenshot in `Docs/richieste/media/R023/`; l'utente ha approvato il risultato.

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

### Limiti e cose aperte
- Il vecchio diorama del titolo e' ancora dentro `01_Titolo.unity` (si spegne a runtime): va tolto dal costruttore di scene quando si ricostruisce quella scena.
- Sfocatura vera dello sfondo non fatta: il set della latta copre la scena con vignetta e grana invece di sfocarla.
- La prova dei tasti (Q/E, frecce, gamepad, tocco, skip con un tasto) e' stata fatta chiamando i metodi da codice, non premendo davvero i tasti; gamepad e touch non sono stati provati con un dispositivo.
- Le scarpe sono un modello fatto a mano ma semplice (tomaia lofted, nessuna texture dedicata: usano i materiali del cuoio gia' presenti).
- Gli screenshot catturati con `ScreenCapture` vanno scritti su un `RenderTexture` Linear: con sRGB risultano schiariti (errore trovato e corretto durante il lavoro).
