# R006 – Mappe molto più grandi e pulizia dei modelli che si compenetrano

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ fdeaedc. **Il codice attuale vince** |
| **Priorità** | Alta. È la **prima** del gruppo R006–R010: R007 (grafica) e R009/R010 (enigmi) lavorano sulle mappe nuove |

## 1. Cosa ha chiesto l'utente (parole sue)
> Ingrandisci molto le mappe […] controlla le mappe: ci sono diversi modelli dentro di loro.

Obiettivo: ambienti **molto più grandi** e credibili da esplorare, senza oggetti incastrati uno dentro l'altro.

## 2. Situazione di partenza
- Scene: `10_Hub_Vicolo`, `20_Int_BassoAntonietta`, `30_FB_Scarpe` (più `01_Titolo` come sfondo), tutte generate da
  `Assets/_Game/Editor/SceneBuilder*.cs` (menu **Treno ▸ …**): oggi un vicolo corto, un basso di una stanza, una stanza del flashback.
- L'utente vede **modelli compenetrati** (mobili, casse, muri, oggetti di scena che si intersecano o fluttuano).
  Cause probabili: posizioni fisse nei builder pensate per i segnaposto della Fase 1 e poi riusate per modelli
  realistici con ingombri diversi; dettagli sparsi (`SceneBuilderDettagli`) senza controllo di sovrapposizione.

## 3. Vincoli
- Tutto resta **generato da script** (nessuna modifica a mano nelle scene), riproducibile da menu.
- Epoca: Napoli, Quartieri Spagnoli, autunno 1946 (`Docs/TRAMA.md`). Niente oggetti moderni.
- Prestazioni: luce baked, occlusion culling, LOD. La mappa più grande non deve far scendere la build WebGL (Bassa)
  sotto i 30 FPS né gonfiare il peso (Git LFS: 1 GB in tutto). Riusare moduli (kit di facciate, porte, balconi).
- Le coordinate di enigmi e oggetti nascosti stanno nei JSON (`x, y, z`): vanno **aggiornate nei JSON**, non nel codice.

## 4. Specifica
### 4.1 Controllo compenetrazioni (prima di tutto)
- Nuovo menu **Treno ▸ Verifica ▸ Compenetrazioni**: per ogni scena segnala coppie di oggetti che si intersecano oltre una
  tolleranza (es. 2 cm, esclusi i contatti voluti con pavimenti e muri), oggetti sospesi (nessun appoggio entro 5 cm) e
  oggetti sotto il pavimento. Rapporto in console + `Docs/richieste/media/R006/compenetrazioni.txt`.
- Correggere **tutte** le segnalazioni nei builder (posizioni calcolate dagli ingombri reali del modello, appoggio a terra
  con raycast, controllo dello spazio libero prima di piazzare un dettaglio). Ripetere la verifica fino a zero errori.

### 4.2 Mappe più grandi (indicativo: almeno 3–4 volte la superficie calpestabile di oggi)
- **Hub vicolo** → un pezzo di Quartieri Spagnoli: vicolo principale in salita con scalinata, 2–3 traverse, uno slargo
  con fontanella o edicola votiva, archi, bassi con porte aperte, panni stesi, balconi, macerie di guerra in un angolo.
  Le strade secondarie finiscono in chiusure credibili (portoni sbarrati, macerie), non in muri invisibili.
  Gli altri luoghi di `luoghi.json` (casa di Capa 'e fierro, sede del partito, stazione, vicolo degli stracci) hanno già
  una porta o uno sbocco visibile, chiuso finché il luogo non è "aperto".
- **Basso di Antonietta** → stanza principale, alcova con letto, angolo cucina, retro o soppalco: stretto e sovraffollato
  come un vero basso, ma con più punti di interesse per gli enigmi (R009/R010).
- **Flashback "Le scarpe"** → spazio del ricordo più ampio (es. tratto di via con botteghe, calzolaio, passanti fermi come
  in un ricordo), coerente con il nuovo disegno degli enigmi (R010).
- Orientamento senza minimappa moderna: luce, insegne d'epoca, punti di riferimento (campanile, edicola votiva).
- Aggiornare spawn, porte, trigger, collisioni della camera nei vicoli stretti, light probes, reflection probes,
  occlusion culling e bake delle luci.

## 5. Ordine di lavoro consigliato
1. Verifica compenetrazioni + correzioni sulle mappe attuali (commit a parte: da solo risolve il problema segnalato).
2. Hub vicolo grande. 3. Basso. 4. Flashback. 5. Bake, FPS per preset, peso build.

## 6. Criteri di accettazione
- [x] **Treno ▸ Verifica ▸ Compenetrazioni** → zero segnalazioni in tutte le scene.
- [x] Hub, basso e flashback almeno 3 volte più grandi, percorribili senza incastrarsi e senza muri invisibili "a vista".
- [x] Scene rigenerabili da menu; console pulita; enigmi e oggetti ancora raggiungibili (coordinate JSON aggiornate).
- [x] FPS e peso build per preset nel Rapporto; WebGL Bassa ≥ 30 FPS (vedi tabella: misura parziale, basso e flashback WebGL non misurati a mano).
- [x] Screenshot prima/dopo di ogni scena in `Docs/richieste/media/R006/`.

## 7. Decisioni dell'utente (2026-11-06, prima dello sviluppo)
- **Hub = grande quartiere** (6–8 volte l'attuale, non 3–4): più strade e piazzette, "con dentro molti easter egg,
  oggetti vari, cose da esplorare e cose nascoste nella storia". I segreti veri e propri sono di R011, ma la mappa va
  progettata **già con angoli, nicchie, cortili e punti nascosti** pensati per ospitarli, e piena di oggetti di scena.
- **Basso** = stanza principale + alcova col letto dietro una tenda + angolo cucina + piccolo cortile sul retro.
- **Flashback "Le scarpe"** = via con botteghe e calzolaio; "sempre tutto più grande, con cose nascoste, easter egg e
  oggetti, per non far annoiare".
- **Asset**: autonomia su CC0/CC-BY (Poly Haven, Sketchfab…), annotati in `CREDITS.md`; confronti all'utente solo per i pezzi importanti.
- Versione online: per reggere il quartiere grande usare occlusion culling, LOD aggressivi a Bassa, densità dei dettagli
  ridotta su WebGL; se non basta, dirlo all'utente invece di rimpicciolire la mappa di nascosto.

## Rapporto
R006 completata in 4 tappe (compenetrazioni, hub, basso + flashback, chiusura). Dettaglio per tappa sotto; la tappa 4 e' in fondo.

### Tappa 2 – hub come grande quartiere (2026-11-06)
- `10_Hub_Vicolo`: 2460 m² calpestabili (prima ~300 m², circa 8x), 36 zone: piazzetta della fontanella, vico principale con 4 scalinate (quota 0 -> 4,8 m),
  slargo dell'edicola votiva con campanile, largo della stazione, 6 traverse, 2 vicoli ciechi, 2 cortili (lavatoio; terrazzino con scala esterna),
  lotto bombardato, 4 ponti-arco (grande arco, sottopasso). Generato da `SceneBuilderQuartiere*.cs` (menu Treno > Costruisci tutte le scene;
  per sola iterazione sull'hub: Treno > Sviluppo > Costruisci solo l'hub, poi bake).
- Punti per i segreti di R011: `Docs/richieste/media/R006/punti_segreti_hub.md` (generato insieme alla scena).
- Verifica: Treno > Verifica > Compenetrazioni = 0 segnalazioni su tutte le scene (la verifica ora usa uno sweep e legge le facce dai triangoli).
- Screenshot: `Docs/richieste/media/R006/hub_*.png`.

### Tappa 3 – basso di Antonietta, flashback "Le scarpe", peso delle lightmap (2026-11-06)
- `20_Int_BassoAntonietta`: da 6 x 5 m (30 m2) a ~108 m2 di pianta (circa 3,6x): stanza principale 9 x 6 m, alcova col letto di ferro dietro una tenda a meta' aperta,
  angolo cucina (fornello a carbone, pentole appese, tavolo, credenza), cortiletto sul retro 5 x 6 m con pozzo, lavatoio, galline, panni, bici e una porta chiusa
  (PortaRetro). Fra alcova e cucina un blocco pieno di 2,4 x 4,3 m e' il ripostiglio nascosto per R011. Sovraffollato: letto, materassi arrotolati, cassapanca, credenza con radio,
  santini, calendario 1946, crocifisso, sedie impagliate, braciere, bacinelle, secchi. Generato da `SceneBuilderBasso.cs`. Resta tutto il resto: porta d'uscita verso l'hub
  (`da_basso_antonietta`), spawn `ingresso` e `oggetto_scarpe`, scarpe raccoglibili (`ItemPickup scarpe`). Punti segreti: `punti_segreti_basso.md`.
- `30_FB_Scarpe`: da stanza 10 x 10 m (100 m2) a tratto di via dei Quartieri Spagnoli con 8 botteghe aperte (calzolaio con banco, forme di legno e scarpe appese; sarto, salumiere, barbiere,
  trattoria, fruttivendolo, latteria, ferramenta), 880 m2 di suolo (9 zone: slargo d'ingresso, via con scalinata, slargo del calzolaio, traversa est, vicolo cieco ovest,
  passaggio, cortile est, piazzetta con fontanella). Riusa il sistema a zone dell'hub (prefisso mesh "Ric"); 14 punti di posizionamento nominati `Passante_NN_*` sotto `PassantiRicordo`
  per R010; easter egg (campana col gesso, scarpe sul filo, trottola, pallone di stracci, radio, graffito). Generato da `SceneBuilderRicordo.cs`. Look seppia del flashback invariato
  (sole piu' alto, ambiente un po' piu' chiaro per leggere la via). Punti segreti e passanti: `punti_segreti_flashback.md`.
- `flashback_scarpe.json`: solo coordinate x,y,z cambiate (vedi `punti_segreti_flashback.md`). I testi (consegne) parlano ancora di "stanza" e di "cassetta": da riscrivere in R010.
- Lightmap: risoluzione per scena in `LookBuilder.LightmapPerScena` (hub 4,5 texel/m, basso 12, flashback 4). Hub da ~48 MB di lightmap (7 EXR) a ~16,8 MB (4 EXR),
  ~18,4 MB con probe e dati; basso ~2,3 MB; flashback ~6,1 MB.
- Verifiche: `Treno > Verifica > Compenetrazioni` e nuova `Treno > Verifica > Raggiungibilita'` (flood-fill a 25 cm con capsula di Amerigo: porte, scarpe, uscita, postazioni degli enigmi,
  oggetti nascosti, passanti). `InteriorWall` ora ha collider a pezzi attorno ai varchi.
- Screenshot: `Docs/richieste/media/R006/basso_*.png`, `fb_via_*.png`, `basso_alto.png`, `fb_alto.png`, `hub_lightmap_ridotte.png`.

### Tappa 4 – soffitto, determinismo, play-test, build WebGL e FPS (2026-11-06)
**Soffitto del basso.** Causa: errore, non scelta. Il generatore non creava nessun soffitto (le stanze erano scatole senza copertura, ereditato dal basso a una stanza
pensato per la camera dall'alto). Soluzione in `SceneBuilderBasso.cs`: lastra `MuroSoffittoStanza` (9,6 x 10,9 m, quota 3,0-3,15 m) sopra stanza, alcova, blocco e cucina
(il cortile resta a cielo aperto) e travi a vista (`AppesoTrave*`, 10 travi) sotto la lastra. Il nome `Muro*` la mette sul layer `VistaLibera`, quindi `WallFader` la nasconde
(solo ombra) quando sta fra camera e Amerigo, cioe' con la camera sopra il soffitto. `WallFader` ora **non** la nasconde con la camera sotto la volta, anche a meno di 60 cm
(prima il controllo di sovrapposizione a 0,6 m l'avrebbe fatta sparire sempre, perche' la camera sta a ~2,45 m). Screenshot: `media/R006/basso_soffitto.png` (vista dall'editor),
`media/R006/basso_gioco_soffitto.png` (camera di gioco: il soffitto e' sopra l'inquadratura).

**Determinismo.** Causa: `FindObjectsByType` non ha ordine garantito e `Array.Sort` e' instabile sui pari merito; il resolver processava le coppie in ordine diverso a ogni build.
Ora `VerificaCompenetrazioni.Raccogli()` ordina i renderer per percorso di indici di fratellanza e lo sweep spareggia per indice. Prova: due rigenerazioni consecutive danno lo stesso hash
MD5 di (nome, posizione) di tutti i Transform in hub, basso e flashback, e lo stesso numero di spostamenti (hub 14, basso 8, flashback 22).

**Play-test del flashback (editor, Play da 00_Boot, Input dal codice con EditorApplication.Step).** Titolo -> nuova partita -> basso -> Interact sulle scarpe con `PlayerInteractor`
(l'oggetto e' agganciato dalla posizione davanti alla porta, non dalla porta) -> `30_FB_Scarpe`. Le 4 postazioni vengono agganciate dall'interactor (quiz -5,5/-4,5; nascosto 0/-18; ordina 4,5/0,9/29 sul
suolo a quota 0,9; lucchetto 14/-14). Quiz, ordina e lucchetto aprono il pannello giusto; per chiuderli ho invocato per riflessione `Solved()`/`FinishSolved()` (le meccaniche dei pannelli sono di R010).
I 5 oggetti nascosti compaiono alle coordinate del JSON (suolo 0,0 o 0,9 per la signora) e sono raccoglibili con l'interactor da posizione a terra; poi si e' completato l'enigma, si e' usata l'uscita
(porta luminosa, 0/2,1/36,9), la cutscene finale (saltata) e il ritorno al basso con `scarpe` nell'inventario e flashback completato. Nessun errore in console. PlayerPrefs `treno_save` cancellato.
Non corretto nulla nei dati: nessun problema trovato. Non provato: meccaniche interne dei pannelli (risposte giuste/sbagliate) e passi a piedi lungo tutto il percorso (verificato dal flood-fill di Raggiungibilita').

**Verifiche e bake.** Compenetrazioni: 0. Raggiungibilita': 0. Bake di tutte le scene (4 scene: 01 11,6 s, hub 81,6 s, basso 17,3 s, flashback 30,9 s).

**Build WebGL.** `Treno > Build WebGL` (Gzip + fallback, preset Bassa), cartella `Build/WebGL` (ignorata da git, nessun commit, nessuna pubblicazione). **45,1 MB** totali (46 MB su disco) contro 40,4 MB della build di prima di R006
(+4,7 MB: hub e flashback molto piu' grandi, lightmap incluse). Il tempo di build e' stato 130 s.

**FPS.** Misura WebGL: Chrome (profilo temporaneo, finestra 1280x800, canvas 960x600) lanciato con `--disable-frame-rate-limit --disable-gpu-vsync` e pilotato via DevTools; FPS = callback di requestAnimationFrame al secondo
(5 s per campione), tasti W/A/D inviati con CDP. PC di sviluppo con GPU potente: i numeri sono un limite alto, non indicano un PC scolastico. Con vsync attivo si ferma a 165 (schermo a 165 Hz).
| Scena / punto | WebGL Bassa (FPS) |
|---|---|
| Titolo | ~1270 |
| Hub, piazzetta della fontanella (spawn) | ~420-440 |
| Hub, vico in salita (prima scalinata) | ~410 |
| Hub, vico alto / scalinate verso lo slargo | 500-640 |
| Basso, flashback | non misurati in WebGL (vedi problemi aperti) |
| Editor, Bassa / Media / Alta / Ultra, 4 punti per scena (hub, basso, flashback) | 1,0-1,8 ms per render a 1280x720, nessuna differenza fra preset (circa 550-1000 FPS di sola resa) |
Misura editor (`Camera.Render` su RenderTexture 1280x720 + lettura di un pixel, editor non in primo piano): **bassa affidabilita'**, i preset non si distinguono (1,0-1,8 ms ovunque); da ripetere con la Game view in primo piano e Stats se serve distinguerli.
Hub: la mappa grande non e' un problema di resa grazie ad occlusion culling e luce baked. Nessuna ottimizzazione necessaria (WebGL Bassa >> 30 FPS); non ho toccato LOD bias, culling o ombre.

**Come testare.** Treno > Costruisci tutte le scene, Treno > Look > Bake luci di tutte le scene, Treno > Verifica > Compenetrazioni e Raggiungibilita' (entrambe 0). In Play dal basso, entrare nel basso e controllare soffitto e travi dalla camera di gioco; prendere le scarpe e percorrere il flashback.

**Problemi aperti.**
- FPS WebGL del basso e del flashback non misurati: raggiungerli a piedi dalla Build era lungo; serve un modo di teletrasporto in build di sviluppo (es. debug con F3) oppure una prova a mano dell'utente.
- Misura editor dei preset poco affidabile (vedi sopra).
- La build WebGL modifica ~84 `.meta` di texture (override WebGL) e due asset di impostazioni: ho ripristinato tutto con git, non fanno parte dei commit.
- Testi del flashback (consegne) ancora a "stanza"/"cassetta" (R010).

**Domande aperte.** Nessuna bloccante. Opzionale: vuoi un comando di debug per teletrasportarsi (solo build di sviluppo) per misurare gli FPS in ogni zona?
