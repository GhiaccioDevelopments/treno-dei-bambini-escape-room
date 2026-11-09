# R020 (+R010) – Enigmi d'autore: hub, basso e "Le scarpe" ridisegnata · SCHEMA APPROVATO (2026-11-07)

Base: R009 (Esamina, vista ravvicinata, meccanismi 3D, `richiede/produce`), TRAMA.md Decisioni 8 e 12, §3, §5, §6.
Assorbe R010. ID battute `cs_scarpe_001..019` invariati; ID enigmi del ricordo invariati (+1 nuovo).
Ogni enigma: oggetto d'epoca vero, prompt "E – Esamina", indizi a 3 livelli (spinta → direzione → soluzione),
scheda civica dopo la soluzione, fallback 2D "Enigmi semplificati". Nessun marcatore viola.

## Luoghi (esistenti, nessuna scena nuova)
`10_Hub_Vicolo` (punti R006: slargo di Antonietta, edicola grande, bacheca T2) · `20_Int_BassoAntonietta`
(stanza, alcova, cucina) · `30_FB_Scarpe` (via delle botteghe, calzolaio, barbiere, `PassantiRicordo`, porta d'uscita).

## Hub (2 obbligatori + 1 facoltativo): guidano al basso
**H1 – Il filo del bucato** · fili stesi sopra lo slargo · *osservazione/percorso*
- Consegna: Amerigo adulto non ricorda più quale porta fosse la sua. Soluzione: seguire i panni con la stessa toppa a
  quadri della sua salopette fino alla porta senza numero dello slargo.
- Trama: salopette rattoppata (CLAUDE.md), Antonietta che ama con i rammendi (§8). Civico: povertà e dignità (art. 3).
- Indizi: 1) Le toppe dicono chi le ha cucite. 2) Cerca la toppa a quadri che porti nel ricordo. 3) Il filo finisce sopra la porta verde dello slargo.
- Produce: porta del basso trovata (chiusa a chiave).

**H2 – L'ex-voto del piede** · edicola votiva grande (punto 1) · *esamina e ruota*
- Consegna: chi teneva la chiave di una casa povera? Soluzione: tra gli ex-voto d'argento, quello a forma di piede;
  girato, ha la chiave appesa dietro. Uso della chiave sulla porta (inventario attivo).
- Trama: piedi scalzi e medico che non si può pagare (`cs_scarpe_009`); ex-voto di grazia per un piede guarito [GIOCO].
- Civico: diritto alla salute, cure gratuite agli indigenti (art. 32, collegamento dell'interfaccia).
- Indizi: 1) In un vicolo, le cose care si affidano a qualcuno. 2) All'edicola grande un ex-voto parla di piedi. 3) Gira l'ex-voto a forma di piede.

**H3 (facoltativo) – I manifesti a strati** · bacheca T2 (punto 17) · *strati da staccare (alla The Room)*
- Sotto tre manifesti incollati uno sull'altro, un avviso del Comitato per i bambini. Scheda: treni della felicità
  (§6: ~70.000 bambini, primo convoglio 16/12/1945). Prepara R013. Produce: pagina del quaderno "I treni".

## Basso (catena di 4) → l'oggetto "Le scarpe"
**B1 – La fotografia storta** · cornice sopra la credenza · *quadro*
- Consegna: una foto pende storta, Antonietta non l'avrebbe lasciata così. Soluzione: raddrizzarla, il fondo si stacca:
  cartoncino con tre disegni (scarpa, croce, treno), la "scrittura" di chi non sa scrivere.
- Trama: Antonietta analfabeta (§1, §4); fotografia di Amerigo bambino da un fotografo di strada [GIOCO].
- Civico: analfabetismo nel 1946 e diritto all'istruzione (art. 34). Indizi: 1) Qualcosa qui non è in ordine. 2) Guarda
  le cornici sopra la credenza. 3) Raddrizza la foto e guarda dietro. Produce: cartoncino dei segni.

**B2 – Il calendario del '46** · calendario della cucina (`testi_ambiente` "Calendario") · *codice da osservazione*
- Consegna: contare i segni come li contava lei. Soluzione: scarpette disegnate a matita per mese (set-ott-nov-dic) →
  4-2-3-1: ogni scarpetta è un soldo messo da parte per le scarpe di Amerigo [GIOCO].
  La croce sul 2 giugno è il voto di Antonietta: scheda sul primo voto nazionale delle donne e sui simboli della
  scheda che aiutavano chi non sapeva leggere (da verificare con fonte prima di scrivere il testo).
- Trama: §3 (2 giugno 1946), §5 voto alle donne, Amerigo "conta tutto" (§8). Civico: voto alle donne (artt. 3, 48).
- Indizi: 1) Il cartoncino ti dice che cosa cercare. 2) Conta solo le scarpette, mese per mese. 3) Il codice è 4231.

**B3 – La cassetta di latta** · mensola dei lumini · *lucchetto a rotelle 3D (R009)*
- Soluzione 4231. Dentro: pochi soldi "per le scarpe" e la chiavetta del baule. Civico: economia di sopravvivenza,
  sostegno alle famiglie (art. 31). Indizi: 1) Serve un numero. 2) Viene dal calendario. 3) 4-2-3-1.

**B4 – Il baule dell'alcova** · cassapanca ai piedi del letto (punto 6) · *meccanismo a strati*
- Chiavetta → coperta buona → doppio fondo: premere le tre assi incise nell'ordine del cartoncino (scarpa, croce,
  treno) → scatolina → **Le scarpe** (oggetto esistente, apre `cutscene_scarpe` e il ricordo).
- Trama: `oggetti.json` (scarpe, luogo basso). Civico: legami familiari (art. 30).
- Indizi: 1) Il baule è più basso dentro che fuori. 2) Le assi del fondo hanno gli stessi disegni del cartoncino. 3) Premi scarpa, croce, treno.

## Ricordo "Le scarpe" (5 enigmi in catena; 4 ID esistenti + 1 nuovo)
**F1 – Le scarpe dei passanti** (`enigma_passanti`, nuovo) · 6 figure intere MakeHuman, ferme e velate (`PassantiRicordo`) · *deduzione alla Obra Dinn*
- Consegna: guardando solo le scarpe, dare un mestiere a ciascuno; conferma a gruppi di 3 giusti. Soluzione: lucide →
  impiegato; chiodate e sporche di calce → muratore; zoccoli bagnati → lavandaia; scarponi militari → reduce;
  tacchi → signora; suole risuolate a regola d'arte e dita nere di pece → calzolaio.
- Trama: Decisione 8 (meccanismo nostro), `cs_scarpe_002-004`. Civico: disuguaglianze e lavoro.
- Indizi: 1) Guarda suole, chiodi, fango. 2) Chi ripara le scarpe degli altri ha le sue perfette. 3) Soluzione completa.
- Produce: il calzolaio "riconosciuto" apre la bottega + una cifra (1) scritta col gesso sulla suola.

**F2 – Il baule delle scarpe** (`enigma_nascosto`) · 5 calzature (posizioni attuali) e forme del calzolaio · *cerca + inventario attivo*
- Riportare le 5 calzature e calzarle sulle forme giuste; le pezze non entrano in nessuna forma: scheda su chi non ha
  scarpe (testi attuali). Sulla forma più piccola, a matita: "Amerigo" e una cifra (9). Civico invariato.

**F3 – La cassetta dei diritti** (`enigma_quiz`) · cassetto a doppio fondo del banco · *documenti da leggere*
- Dentro, tre documenti d'epoca: avviso del Comitato (che cosa ricevono i bambini), registro dei lavori "a credito" del
  calzolaio, pagina di un quaderno di scuola. Le tre domande si risolvono **indicando la riga giusta** nei documenti.
  Articoli e date (artt. 3, 31; Convenzione ONU 1989) stanno solo nella scheda civica dell'interfaccia, mai nei documenti del 1946: la Costituzione del 1948 e la Convenzione del 1989 non possono comparire nel ricordo. Produce: cifra (8).

**F4 – Il calendario della memoria** (`enigma_ordina`) · ritagli di giornale dal barbiere · *trascinare sul muro*
- Gli stessi 6 eventi, trascinati con mouse/touch (riga del BACKLOG sul drag & drop). Produce: cifra (9).

**F5 – Il lucchetto dell'uscita** (`enigma_lucchetto`) · porta luminosa della piazzetta · *lucchetto a rotelle 3D*
- Codice 1989 ricomposto dalle 4 cifre raccolte (spiegazione attuale invariata).

## Cutscene
`cutscene_scarpe` invariata (ID e testi). Una battuta nuova facoltativa di Amerigo adulto all'ingresso del basso
(`cs_basso_001`, voce sobria, §8). Nessuna citazione da libro o film.

## Personaggi e asset nuovi
6 passanti + calzolaio (MakeHuman, abiti 1946, pose fisse; R003 "NPC" è chiusa) · ex-voto d'argento · foto con cornice ·
calendario 1946 con segni a matita · cassetta di latta con lucchetto a rotelle · cassapanca con doppio fondo animato ·
forme di legno e banco con cassetti · ritagli di giornale (testi nostri) · fili del bucato con toppe.

## Stima: **L** (sistema R009 a parte). Ordine: basso → ricordo → hub.

## Rischi
- Passanti e calzolaio: 7 personaggi in più (peso LFS ~589 MB/1 GB).
- H1/H2 (bucato e chiave lasciati da Antonietta) funzionano perché hub e basso sono "la Napoli della memoria" (D1 approvata).
- Le schede su simboli del referendum ed ex-voto vanno verificate con una fonte prima dei testi definitivi.

## Decisioni approvate (2026-11-07)
- **D1 – "Napoli della memoria":** hub e basso hanno l'aspetto del 1946 e si giocano con Amerigo adulto, che la
  attraversa senza che nessuno gli parli. Il 1994 vero c'è solo in R016.
- **D2 – Passanti del ricordo:** figure intere MakeHuman, ferme e velate.
- Codice d'uscita 1989 ricomposto dalle quattro cifre raccolte (1, 9, 8, 9). La ricevuta del pegno del violino non
  sta nel baule: è l'oggetto di R016 (N3), perché il violino viene impegnato nel 1947.
