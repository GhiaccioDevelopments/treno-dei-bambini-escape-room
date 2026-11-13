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
- [x] Le 5 scarpe si vedono e si raccolgono in Windows; livello 2 completabile dall'inizio alla fine.
- [ ] Stessa prova su WebGL (preset Bassa): **in attesa**, la fa fare il responsabile dopo la build WebGL.
- [x] Controllo automatico nel self-test, PASS.
- [x] Console pulita; screenshot in `Docs/richieste/media/R024/`.

## Rapporto

### Causa (con prova)
Il sospetto della specifica (modelli `cartone/zoccoli/militari/tacchi/pezze` mancanti) **non e' la causa**: `MondoFab.Scarpa` per quegli stili ricadeva su primitive o sul modello `scarpa`, quindi
qualcosa si creava sempre. Prova in Play e poi sulla build Windows (30_FB_Scarpe, `NascostoPuzzle` avviato): le 5 scarpe esistono, renderer attivi e bounds non nulli, `min.y` = quota del selciato
(0,00; la signora 0,90), nessuna esclusione da `sostituisce`/`nascosta`/MondoStato.
Il difetto era **visivo**: calzature da ~29 cm, grigio-scure come il selciato, la stessa scarpa R023 riusata per 3 stili, nessuna luce che le sfiori e prompt solo da vicinissimo. Dalla camera di gioco
(~4 m) il blob scuro si confondeva con le pietre. Prova misurata: lo stesso self-test nuovo, eseguito sul codice di 47a8831 (calzature da primitive, senza luce), **FALLISCE**:
scarpa_cartone 108 pixel diversi dal selciato a 4 m, zoccolo_legno 162 (soglia 250), e in piu' la scarpa_signora risultava a y 0,83 dentro il selciato (0,90). Foto "prima" (ricostruita rimettendo
temporaneamente solo `MondoFab`/`NascostoPuzzle` all'originale): `media/R024/prima_*.jpg`; "dopo": `media/R024/dopo_*.jpg`.

### Cosa e' cambiato
- 5 modelli originali in Blender da codice (`ArtSource/Models/Enigmi/genera_calzature.py`, riusa `genera_enigmi_v2.py`), 760-1490 triangoli, nessuna texture nuova, FBX ~220 KB in LFS in tutto.
  **Tutti IN PIEDI** (cosi' servono alla deduzione e alle forme del calzolaio): `calz_zoccoli` (zoccolo con fascia di cuoio e chiodi), `calz_militari` (scarpone infangato con lacci), `calz_cartone`
  (cuoio con cartone che spunta dall'apertura e dalla suola), `calz_tacchi` (decolte' aperta con tacco dritto e fiocchetto), `calz_pezze` (fagotto di stoffa legato con due code).
- Posa e colore dei raccoglibili nel JSON (non nel codice): `flashback_scarpe.json`, `oggetti[]` dell'enigma nascosto ha ora `rx` (inclinazione, 0 = in piedi; scarpone -72, cartone 78, signora 55: abbandonate su
  un fianco) e `colore` (cuoio piu' chiaro per il contrasto). Campi opzionali in `NascostoDef`. Deduzione e forme del calzolaio restano in piedi e con i colori originali.
- `MondoFab.Scarpa` usa `calz_<stile>` (lunghezza = misura x vera dei FBX: zoccoli 0,275, militari 0,306, cartone 0,356 con i lembi, tacchi 0,248, pezze 0,247) con appoggio a terra; senza prefab ricade sulle
  primitive. Colori di default dello stile ripristinati (i piu' chiari arrivano solo da `NascostoPuzzle`/JSON).
- `NascostoPuzzle`: scala 1,4, collider piu' ampio, luce puntiforme calda senza ombre (`LuceCalzatura`, range 2,8, intensita' 8 con respiro lento): chiazza di luce sul selciato, nessun alone colorato.
- `EnigmiModelli.cs`: 3 materiali solo colore (`ME_Fango`, `ME_Interno`, `ME_Cartone`).
- `EnigmiSelfTest`:
  - itera su **tutti** gli enigmi `nascosto` del ricordo caricato (richiede i gettoni, li avvia, controlla i loro oggetti);
  - per ogni raccoglibile: renderer attivi con materiale; bounds non nulli; appoggiato (origine del raggio fuori dai solidi e primo solido sotto entro 5 cm dal fondo: ne' sotto ne' sospeso, anche per la signora
    a 0,90 sull'appoggio); nessun solido entro 25 cm sopra; fuori dai collider dei muri; **visibilita'** dalla camera di gioco a ~4 m (FOV della camera vera, render con e senza l'oggetto a parita' di luce:
    lato >= 40 px e >= 250 pixel che cambiano di luminosita' oltre 0,06);
  - FAIL per ogni oggetto nascosto il cui modello non e' una calzatura nota ne' un prefab esistente (niente ricaduta silenziosa); i prefab `calz_*` devono caricarsi;
  - foto: ravvicinata e dalla distanza di gioco di ogni raccoglibile.

### Decisioni prese da Claude
- Posa "su un fianco" solo ai raccoglibili, da JSON (`rx`), per leggere la sagoma dall'alto senza falsare deduzione e forme.
- Luce puntiforme reale (nessuna variante di shader da includere nelle build), 5 luci senza ombre solo mentre la ricerca e' attiva; range ridotto a 2,8 su richiesta della review.
- Scala 1,4 e cuoio piu' chiaro solo per i raccoglibili.
- Soglie di visibilita' scelte guardando i valori misurati: dopo la correzione 566-2349 pixel diversi (la signora, il piu' basso, 566), prima 108-911; la soglia 250 fa fallire il vecchio stato.
- LOD1 **non aggiunto**: i calz_* hanno 760-1490 triangoli, sono 5 istanze piccole, e il LODGroup creato dalla pipeline (transizioni 0,35/0,02 in `EnigmiModelli.CreaPrefab`) scatterebbe al LOD1 a qualunque
  distanza di gioco peggiorandoli senza risparmiare nulla di misurabile.
- Materiali riusati, niente texture nuova (LFS); screenshot JPG 1280x720 fuori da LFS.

### Risultati
- Self-test enigmi, build Windows di sviluppo: **241 PASS, 0 FAIL** (nuovi controlli inclusi). Il livello 2 si completa dall'inizio alla fine (cinque raccolte, prova sulle forme, cifra, salvataggio), anche in
  modalita' semplificata e da salvataggio vecchio. Stesso test sul vecchio codice: 227 PASS, **9 FAIL** (5 modelli senza dedicato, 2 visibilita', 2 per la signora sotto il selciato): il controllo prende il bug.
- Console dell'editor: 0 errori.
- Criterio WebGL (preset Bassa): **in attesa**, non eseguito qui (le luci additive sono per pixel, limite 4, su tutti i preset).
- Screenshot in `Docs/richieste/media/R024/`: `dopo_distanza_*` (camera di gioco a 4 m), `dopo_vicino_*`, `prima_distanza_*` e `prima_vicino_scarpa_signora`, `dopo_deduzione_passanti` (scarpe in piedi, colori
  originali), `dopo_forme_calzolaio` (calzature sulle forme dopo la soluzione).

### Problemi aperti
- I modelli sono procedurali, leggibili ma non rifiniti (le pezze e la signora vanno viste da vicino per essere riconosciute al volo); ritoccabili in Blender senza toccare il codice.
- Il self-test nell'editor (Play) non e' affidabile con l'editor senza focus (clic del mouse virtuale): usare la build Windows con `-enigmitest`.
- Prova WebGL pendente.
