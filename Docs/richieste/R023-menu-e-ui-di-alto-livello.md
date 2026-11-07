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
_(da compilare a fine lavoro)_
