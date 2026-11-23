# R012 – Ricordo "Stracci": il vicolo degli stracci e la bottega di Capa 'e fierro · SCHEMA APPROVATO (2026-11-07)

Base: regole comuni R012 §2, TRAMA Decisione 3 (Capa 'e fierro robivecchi, brontolone ma non cattivo), §1, §5, §7.2, §8.
Emozione: ingegno e imbarazzo. Tema: lavoro minorile, economia di sopravvivenza. Amerigo bambino, scalzo.
**Meccanismi nuovi nel gioco:** esame del tessuto, bilancia a piatti con pesi, pagamento con monete vere sul banco.

## Luoghi e ambienti
- Scena nuova `40_FB_Stracci` (**una scena, due zone**, D1 approvata):
  - **Vicolo degli stracci** (`vicolo_stracci`): cortili, macerie, fili del bucato, scale esterne, portoni dove si bussa.
    Grande, con scorciatoie e terrazzini (standard R006).
  - **Bottega di Capa 'e fierro** (`casa_capa_e_fierro`): basso-magazzino con montagne di stracci, ferri vecchi, carretto,
    ceste per materiale, bilancia a piatti, registro, lanterna. Luce di lampada a petrolio.
- Aggiornare `luoghi.json`: la descrizione "vicino testardo" diventa robivecchi (Decisione 3).

## Ingresso
Oggetto nel presente: un **fagotto di stracci** legato con lo spago nel cortile del basso → cutscene `cutscene_stracci`.

## Catena di enigmi
**S1 – Il giro degli stracci** · stracci sparsi nel vicolo, porte, ceste · *esplorazione + esame del tessuto (nuovo)*
- Consegna: raccogliere 8 pezzi che Capa 'e fierro compra. In vista ravvicinata si guarda la trama in controluce, si tira
  un filo (la lana si arriccia, il cotone si spezza netto), si sente lo spessore. Soluzione: 3 lana, 3 cotone, 2 lino;
  scartare la seta marcita e la tela cerata (non si comprano). Nessun tempo limite.
- Trama: Amerigo raccoglie stracci per le case e porta a Capa 'e fierro (§1). Civico: lavoro dei bambini nel 1946.
- Indizi: 1) Non tutto quello che è vecchio vale qualcosa. 2) L'insegna disegnata della bottega mostra cosa compra.
  3) Lana, cotone, lino sì; seta marcita e tela cerata no.
- Produce: sacco di stracci (inventario).

**S2 – Le ceste** · tre ceste della bottega con un disegno ciascuna · *smistare trascinando (inventario attivo)*
- Mettere ogni straccio nella cesta del suo materiale; Capa 'e fierro brontola ma corregge con gentilezza.
- Civico: perché gli stracci valevano: diventavano carta e lana rigenerata (economia circolare d'epoca; da verificare
  con fonte i dettagli su cartiere e lanifici prima della scheda). Produce: stracci pronti per la pesata.

**S3 – La bilancia che sbaglia** · bilancia a piatti e pesi di ottone · *bilancia (nuovo)*
- Consegna: Amerigo sente che il conto non torna. Pesare un oggetto di peso noto (il peso campione da 1 kg del banco)
  mostra che la bilancia pende: sotto un piatto c'è un chiodo incastrato. Toglierlo, poi pesare le tre ceste
  scegliendo i pesi giusti (1 kg, 500 g, 200 g, 100 g) fino all'equilibrio.
- Trama: ingegno di Amerigo, bravo coi numeri (§1); Capa 'e fierro non è un imbroglione: il chiodo c'è per caso e lui,
  brontolando, gli dà ragione [GIOCO: starata per caso, Capa 'e fierro resta "in fondo buono"].
- Indizi: 1) Una bilancia giusta sta in equilibrio a vuoto? 2) Prova con il peso da 1 kg contro qualcosa che sai quanto
  pesa. 3) Guarda sotto il piatto di sinistra.
- Civico: diritto a una paga giusta e a non essere sfruttati. Produce: tre pesi scritti sul registro.

**S4 – Il conto** · registro dei prezzi (disegni + cifre) e cassetta delle monete · *pagamento con monete sul banco (nuovo)*
- Calcolare quanto spetta (peso × prezzo per materiale, numeri piccoli) e disporre sul banco le monete giuste.
  Prezzi in lire **inventati e plausibili** [GIOCO], marcati come tali nella scheda.
- Indizi: 1) Ogni cesta ha il suo prezzo al chilo. 2) Fai un conto per cesta, poi somma. 3) Somma e monete da usare.
- Civico: economia di sopravvivenza, borsa nera (§5). Produce: **il soldo** guadagnato.

**S5 – Il muro dei mestieri** · parete della bottega con oggetti appesi · *abbina a tre colonne*
- Oggetto → mestiere di bambino del 1946 → diritto di oggi: cassetta da lustrascarpe, secchio dell'acquaiolo,
  cesto del venditore ambulante, sacco degli stracci → art. 37 (lavoro dei minori), art. 34 (scuola), Convenzione ONU
  art. 32 (protezione dal lavoro). I collegamenti agli articoli li fa l'interfaccia, non Capa 'e fierro.
- Indizi: 1) Pensa a dove si usava ogni oggetto. 2) Poi chiediti che cosa mancava a quel bambino. 3) Soluzione.
- Produce: apre la porta della bottega verso il vicolo (uscita dal ricordo).

## Oggetto e cutscene (scaletta, battute `cs_stracci_NNN`, testi originali)
- Ingresso: Amerigo adulto scioglie il fagotto; dissolvenza al bambino che bussa alle porte con il sacco; una donna
  gli dà un vestito liso; Tommasino lo prende in giro, lui risponde con una battuta.
- Chiusura: Capa 'e fierro conta il soldo nella mano di Amerigo, brontola, aggiunge qualcosa di suo; Amerigo torna da
  Antonietta e lo mette nella cassetta di latta (aggancio a R020-B3). Voce adulta: il lavoro dei bambini oggi.
- Oggetto in inventario: **gli stracci** (il fagotto) → si apre `sede_partito` (capitolo Volantino).

## Segreti (R011)
Enigma segreto: il carretto di Capa 'e fierro ha un cassetto sotto il sedile che si apre girando la ruota (vista
ravvicinata). Collezionabili: tappo a corona, bottone militare, ritaglio di giornale, carta da gioco, biglia.

## Personaggi e asset nuovi
Capa 'e fierro (MakeHuman, ~50 anni, grembiule di cuoio, coppola; sembianze originali, non dell'attore del film) ·
2–3 donne del vicolo (varianti) · Tommasino (se non già fatto) · bilancia a piatti con pesi · carretto · ceste ·
mucchi di stracci (pochi poligoni, texture tessuti CC0) · ferri vecchi · registro · monete d'epoca.

## Stima: **L** (scena nuova + 1 personaggio principale + 3 meccanismi nuovi).

## Rischi
- Prezzi e dettagli economici: inventati, dichiararlo; la scheda su stracci → lana rigenerata richiede una fonte.
- Il lavoro minorile va raccontato senza pietismo né colpa ad Antonietta (§8: niente prediche).

## Decisioni approvate
## Decisioni approvate (2026-11-07)
- **D1 – Una sola scena:** vicolo degli stracci + bottega di Capa 'e fierro, senza caricamenti tra le due zone.
- **D2 – La bilancia:** starata per caso (un chiodo sotto il piatto). Capa 'e fierro resta "in fondo buono" (Decisione 3 di TRAMA).
