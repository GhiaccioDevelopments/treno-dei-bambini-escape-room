# R011 – Easter egg e segreti in ogni stanza · SCHEMA APPROVATO (2026-11-07)

Base: R009 (Esamina, inventario attivo), R006 (punti già preparati in `Docs/richieste/media/R006/punti_segreti_*.md`).
Tutto **facoltativo**: nessun segreto blocca la storia. Indizi solo ambientali (luce, suono, scritta), niente frecce.
Il pulsante Indizio tace sui segreti finché la stanza non è completata, poi dà solo "In questa stanza c'è ancora qualcosa…".
Pagina **"Segreti"** nel quaderno (caselle per stanza, es. "Basso 3/5"), contatore solo nel menu pausa.
Dati: `segreti.json` `{ segreti: [ {id, scena, tipo: collezionabile|enigma, nome, scheda, x, y, z, ricompensa} ] }`, progressi in `treno_save`.

## Regole sui contenuti
- Collezionabili "Ricordi di Napoli": cartoline, carte napoletane, monete e banconote d'epoca (lire del Regno, AM-lire
  degli Alleati: da verificare quali circolassero a Napoli nel 1946), giocattoli poveri, ritagli di giornale.
- **Ritagli di giornale**: testo nostro su fatti **verificati** di TRAMA §3/§6 (16/12/1945 primo convoglio; 2 giugno 1946;
  ~70.000 bambini; Comitato per la salvezza dei bambini di Napoli). Niente fatti "(da verificare)", niente testate reali
  imitate, niente persone reali con battute (Decisione 5). Via Medina (11/6/1946): solo se l'utente lo vuole (tema violento).
- Tono serio: rimandi interni (scarpe, stracci, mela annurca, violino), niente marchi, meme o altri giochi.
- Via Medina (11/6/1946): esclusa dallo schema.

## Scene esistenti (dettaglio)
**Hub `10_Hub_Vicolo`**
- Enigma segreto **"Le tre edicole"**: nelle edicole piccole T3 e T5 e in quella grande (punti 15, 16, 1) un biglietto
  con un verso di una filastrocca nostra e un segno (scarpa, mela, treno). Messi in ordine indicano la portella del
  sottoscala del cortile C3 (punto 4): dentro, la "scatola dei ragazzi del vicolo" con un treno disegnato col carbone.
- Collezionabili (5): cartolina del golfo senza francobollo (valigie, punto 21) · carta napoletana, il "settebello",
  sotto il tavolo da gioco (23) · trottola nel pozzo C1 (6) · banconota AM-lire nella nicchia del grande arco (11) ·
  ritaglio "partono i bambini" tra le macerie (punto 12; il punto 17 è di R020-H3).
- Easter egg: al gradino della chiesetta (18), dopo il finale, si sente in lontananza il tema del violino (R002).

**Basso `20_Int_BassoAntonietta`**
- Enigma segreto **"Il ripostiglio murato"**: graffi a semicerchio sul pavimento davanti alla credenza (punto 8);
  togliendo i piatti in alto si trova il fondo finto, la credenza si sposta e apre l'intercapedine (punto 3). Dentro:
  il materassino dove dormiva Amerigo d'estate [GIOCO] e la ricompensa (pagina "dietro le quinte": come vivevano i bassi).
- Collezionabili (4): santino dietro la tenda dell'alcova (5) · ditale di Antonietta sotto la mattonella del tavolo (7) ·
  bottone della salopette nella pentola più pesante (10) · biglia di vetro dietro il lavatoio del cortile (14).

**Ricordo `30_FB_Scarpe`**
- Enigma segreto **"La campana di gesso"**: sul graffito "C'ero anch'io" (punto 24) c'è un numero di 4 cifre; saltare le
  caselle della campana (punto 9) in quell'ordine fa scivolare la pietra mobile della fontanella (27): dentro, il soldo
  che Tommasino aveva promesso ad Amerigo [GIOCO].
- Collezionabili (5): moneta da dieci lire sotto la soglia del barbiere (6, verificare che il taglio esistesse nel 1946) ·
  pallone di stracci (10) · biglietto della spesa con una lista di scarpe nella cassa del fruttivendolo (8) · uovo e
  biglietto sotto la paglia (25) · bottone d'oro nel cappotto del sarto (4).
- Easter egg: le scarpe appese al filo (14, 15) cambiano: dopo il capitolo "Cappotto" ce n'è un paio piccolo in più.

## Scene nuove (una riga ciascuna; dettaglio dentro ogni schema R012–R016)
Stracci (vicolo + bottega) · Comitato e vicolo delle voci · stazione e vagone · Modena · basso 1994: in ognuna
1 enigma segreto + 3–5 collezionabili, scelti con un "punti segreti" generato dallo SceneBuilder come per R006.

## Ricompense
Solo narrative: pagina "dietro le quinte" per ogni enigma segreto (i veri treni della felicità, con fonti di TRAMA §6);
5 collezionabili = una scheda storica; **tutti i segreti** = una scena in più nel finale (Amerigo consegna a Carmine la "scatola dei ragazzi del vicolo", vedi R016).

## Asset
Oggetti piccoli (carte, monete, cartoline, trottola, biglia, ditale): pochi poligoni, un atlas comune · texture dei
biglietti e ritagli in `testi_ambiente.json` (gruppo nuovo `segreti`) · credenza spostabile e intercapedine del basso.

## Stima: **M** per le 3 scene esistenti (+ **S** per ogni scena nuova, già contata nei capitoli).

## Rischi
- Fatti storici dei ritagli e tagli delle monete: verificare prima di scrivere le schede.
- I punti R006 si rigenerano con lo SceneBuilder: i segreti vanno agganciati per nome del punto, non a coordinate fisse.
- Conflitto di punti con R020 (bacheca T2, cassapanche del basso): assegnati qui solo punti non usati da R020.

## Decisioni approvate (2026-11-07)
- **D1 – Premio per chi trova tutto:** una breve scena in più nel finale (Amerigo adulto ritrova nel basso 1994 la
  "scatola dei ragazzi del vicolo" e la consegna a Carmine).
- **D2 – Ritagli di giornale:** testi nostri su fatti verificati, impaginati "alla maniera" del 1946. Nessuna
  riproduzione d'archivio.
