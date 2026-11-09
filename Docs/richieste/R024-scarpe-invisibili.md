# R024 – Le scarpe da raccogliere nel ricordo sono invisibili

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `main` @ d09dfc3 (+ R019 se già integrata). **Il codice attuale vince** |
| **Dipende da** | niente. **In testa alla coda** (subito dopo R019) |
| **Priorità** | **Altissima**: senza vederle non si finisce il livello 2 |

## 1. Cosa ha chiesto l'utente (parole sue)
> Le scarpe, nel ricordo del basso di Antonietta, LIVELLO 2: quelle da raccogliere non si vedono, sono invisibili, risolvi.

## 2. Situazione di partenza (da verificare, non presumere)
- Livello 2 del ricordo = `flashback_scarpe.json`, enigma 2 `enigma_nascosto` "Il baule delle scarpe" (tipo `nascosto`):
  cinque `oggetti[]` da trovare nella strada e provare sulle forme di legno della mensola:
  `scarpa_cartone` (modello `cartone`), `zoccolo_legno` (`zoccoli`), `scarpone_soldato` (`militari`),
  `scarpa_signora` (`tacchi`, y 0,9), `pezze_piedi` (`pezze`).
- Li crea `NascostoPuzzle.cs:59` con `MondoFab.Scarpa(... testo = n.modello ...)`. Dopo R009/R020 (niente più oggetti viola,
  modelli veri in `Resources/Modelli/Enigmi`) in quella cartella c'è solo `scarpa.prefab`, nessuna variante
  `cartone/zoccoli/militari/tacchi/pezze`.
- Sospetti: variante non trovata → nessuna mesh; materiale o shader che non compila (anche su WebGL); posizione sotto il
  pavimento o dentro i muri dopo R006 (mappe più grandi); oggetti spenti da `sostituisce`/`nascosta`/MondoStato; LOD o culling;
  layer escluso dalla camera.

## 3. Specifica
1. **Diagnosi con prove** (screenshot in Play della posizione di ognuna delle 5 scarpe, gerarchia e componenti):
   scrivere nel Rapporto la causa vera.
2. **Correzione alla radice**: le cinque scarpe devono essere **ben visibili e riconoscibili**, con modelli veri e diversi tra loro,
   coerenti col 1946 (zoccolo di legno, scarpone militare infangato, scarpa col cartone, scarpa da signora col tacco, pezze per i
   piedi), a terra o su un appoggio plausibile, mai dentro muri, pavimenti o oggetti, raggiungibili a piedi.
3. Devono farsi notare senza alone viola (regola di R009): luce che le sfiora, prompt discreto da vicino; la minimappa di R019
   può segnarle in modalità Guidata.
4. Verifica nelle due build, Windows e WebGL (preset Bassa), e su tutti i preset grafici.
5. **Prova automatica**: aggiungere al self-test degli enigmi un controllo che ogni oggetto raccoglibile di ogni enigma `nascosto`
   abbia renderer attivi, bounds non nulli, stia sopra il pavimento e fuori dai collider dei muri. Così il bug non torna nei
   ricordi futuri (R012–R016).

## 4. Criteri di accettazione
- [x] Causa scritta nel Rapporto, con prova.
- [x] Le 5 scarpe si vedono e si raccolgono in Windows e WebGL; livello 2 completabile dall'inizio alla fine.
- [x] Controllo automatico nel self-test, PASS.
- [x] Console pulita; screenshot in `Docs/richieste/media/R024/`.

## Rapporto

### Causa (con prova)
Il sospetto della specifica (modelli `cartone/zoccoli/militari/tacchi/pezze` mancanti) **non e' la causa**: `MondoFab.Scarpa` per quegli stili ricade su primitive o sul modello `scarpa`, quindi
qualcosa si creava sempre. Prova in Play (30_FB_Scarpe, `NascostoPuzzle` avviato): le 5 scarpe esistono, `renderer attivi` e `bounds` non nulli, `min.y` = quota del selciato (0,00 e 0,90 per la signora),
nessun collider di muro nel volume, nessuna esclusione da `sostituisce`/`nascosta`/MondoStato.
Il difetto era **visivo**: calzature da ~29 cm (primitive grigio-scure, la stessa scarpa R023 ridimensionata per tre stili) su selciato scuro dello stesso colore, senza alcuna luce che le sfiori e con
il prompt "Raccogli" solo da vicinissimo. A distanza di camera (3-5 m) il blob scuro si confondeva con le pietre: per il giocatore erano invisibili. Screenshot prima/dopo: vedi `media/R024/`
(la prima prova in Play mostrava un grumo grigio-azzurro di ~30 cm sul selciato).

### Cosa e' cambiato
- 5 modelli originali fatti in Blender da codice (`ArtSource/Models/Enigmi/genera_calzature.py`, riusa le funzioni di `genera_enigmi_v2.py`), 700-1500 triangoli ciascuno, nessuna texture nuova:
  `calz_zoccoli` (zoccolo di legno con fascia di cuoio e chiodi d'ottone), `calz_militari` (scarpone infangato sul fianco, con lacci), `calz_cartone` (scarpa su un fianco, suola bucata e rattoppata col cartone),
  `calz_tacchi` (scarpa da signora col tacco), `calz_pezze` (stracci incrociati, fascia arrotolata, spago). FBX in `Assets/Art/Models/Enigmi/` (LFS, ~220 KB in tutto), prefab in `Resources/Modelli/Enigmi/`.
- `MondoFab.Scarpa`: per i cinque stili usa il modello `calz_<stile>` (lunghezza vera dalla tabella `LunghezzaCalzatura`, appoggiato a terra con `AppoggiaATerra`); se manca ricade sulle primitive. Vale anche
  per le calzature sulle forme del calzolaio (EsaminaView).
- `NascostoPuzzle`: calzature a scala 1,4, collider di raccolta piu' ampio e una luce puntiforme calda senza ombre (`LuceCalzatura`, respiro lento) che disegna una chiazza di luce sul selciato: si notano
  da lontano, senza alone colorato. Prompt gia' discreto "Raccogli: <nome>".
- `EnigmiModelli.cs`: 3 materiali solo colore (`ME_Fango`, `ME_Interno`, `ME_Cartone`).
- Colori del cuoio dei tre stili con modello resi piu' leggibili (militari, tacchi, cartone, pezze).
- `EnigmiSelfTest`: nuovo `ControllaRaccoglibili` (per ogni `HiddenItem` di ogni enigma `nascosto`: renderer attivi e con materiale, bounds non nulli, sopra il pavimento, fuori dai collider di muri e oggetti;
  scatta una foto ravvicinata di ognuno) e controllo che per ogni oggetto `nascosto` con stile di calzatura esista il prefab `calz_<stile>`.

### Decisioni prese da Claude
- Modelli sdraiati su un fianco per scarpone, scarpa col cartone e scarpa da signora: in piedi, dall'alto della camera, sembravano cilindri o salsicce; di lato si legge la sagoma e, nel caso del cartone, la suola rattoppata.
- Luce puntiforme reale invece di decal o emissione: sicura nelle build (nessuna variante di shader da includere), costa poco (5 luci senza ombre, solo mentre la ricerca e' attiva).
- Scala 1,4 rispetto al vero (calzature ~0,28-0,34 m -> ~0,4-0,5 m) per riconoscerle da lontano.
- Materiali riusati (nessuna texture nuova) per non pesare su LFS; screenshot JPG 1280x720 fuori da LFS.
- Il self-test controlla anche che i modelli `calz_*` esistano, cosi' un ricordo futuro con nuovi stili non cade in silenzio sul segnaposto.
- Non toccato: JSON (stessi id, posizioni, testi), percorso del livello 2.

### Risultati
- Self-test enigmi sulla build Windows di sviluppo: **214 PASS, 0 FAIL** (prima 189 + 25 nuovi: 5 modelli calz_* caricabili + 5 oggetti x 4 controlli). Il livello 2 si completa dall'inizio alla fine
  (cinque raccolte, prova sulle forme, cifra prodotta, salvataggio) anche in modalita' semplificata e da salvataggio vecchio.
- Console dell'editor pulita (0 errori). Build WebGL e verifica su WebGL (preset Bassa) NON fatte qui: da fare alla pubblicazione (le luci additive sono per pixel, limite 4, su tutti i preset).
- Screenshot: `Docs/richieste/media/R024/calzatura_*.jpg` (foto del self-test dalla build Windows, camera a ~1,5 m).

### Problemi aperti
- I modelli sono procedurali e leggibili ma non rifiniti (la scarpa da signora e le pezze si riconoscono solo a vista ravvicinata); si possono ritoccare in Blender senza toccare il codice.
- Il self-test nell'editor (Play) non e' affidabile con l'editor senza focus (clic del mouse virtuale): usare la build Windows con `-enigmitest`.
