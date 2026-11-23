# R016 – Il finale: il basso nel 1994 · SCHEMA APPROVATO (2026-11-07)

Base: regole comuni R012 §2; TRAMA Decisioni 1 (cornice 1994, Opzione A), 6 (niente lettera del film: violino
sotto il letto + oggetto nostro, es. ricevuta del Monte dei pegni riscattata + un piccolo segno di Antonietta, **non un
testo lungo**), 7 (Carmine, breve, senza dettagli giudiziari); §1 sez. 4, §5 (art. 30, L. 184/1983), §6 Napoli 1994,
§7.3, §8 (evitare la frase del film sull'amore che "lascia andare"). Emozione: commozione. Nessun errore punitivo.
**Meccanismo nuovo:** la stanza a due tempi (1994 ↔ 1946) che cambia mentre si ricorda.

## Luogo e ambiente
`80_Basso_1994`: **stesso impianto** del basso di Antonietta (`20_Int_BassoAntonietta`, generato dallo SceneBuilder)
con arredo 1994 (§7.3): lampadina nuda, calendario 1994, radio, televisore a tubo, telefono fisso, scatola di latta,
mobili coperti da lenzuola, scatoloni; niente cellulari. Look del presente freddo e neutro, meno grana. Ogni angolo ha
anche la versione 1946 (già esistente) per il passaggio tra i due tempi. Marzo 1994 [film, da verificare]: niente G7,
al massimo un manifesto elettorale o un cantiere sullo sfondo (§6).

## Catena di enigmi
**N1 – Il basso svuotato** · telefono, calendario, scatola di latta · *esplorazione guidata*
- Amerigo adulto (giocabile) entra; il telefono squilla a vuoto, il calendario 1994 ha un giorno cerchiato (il suo
  concerto, [GIOCO]); nella scatola di latta il cartellino del treno con il suo numero (aggancio a R014-C1).
- Nessun enigma vero: insegna il gesto "posa un ricordo". Produce: elenco dei 5 posti vuoti.

**N2 – Cinque ricordi al loro posto** · 5 punti della stanza · *stanza a due tempi (nuovo)*
- Consegna: posare ogni oggetto dei capitoli dove "abita": scarpe sulla soglia, stracci nella cesta del cortile,
  volantino sulla credenza, cappotto al chiodo della porta, spartito sul cuscino. A ogni posa quell'angolo torna 1946
  per qualche secondo (luce calda, suoni, una figura di spalle) e lascia un **frammento di carta** a terra.
- Se l'oggetto va nel posto sbagliato: "Qui non c'era", e la luce del posto giusto si accende piano.
- Trama: tutti i capitoli; Antonietta conservava i ricordi (§1 sez. 4). Civico: memoria e legami (art. 30).
- Indizi: 1) Ogni oggetto ha un posto in cui lo usavi. 2) Il cappotto si appende, le scarpe si lasciano entrando. 3) Elenco.
- Produce: 5 frammenti.

**N3 – La ricevuta** · tavolo sotto la lampadina · *ricomposizione di un documento*
- Ricomporre la polizza del Monte dei pegni del violino (1947): numero, descrizione "violino con custodia", timbro
  **RISCATTATO** con una data di qualche anno dopo, e una croce al posto della firma (Antonietta non sapeva scrivere).
- Indizi: 1) I bordi dei pezzi combaciano. 2) Il timbro sta in basso a destra. 3) Ricomposta.
- Civico: credito su pegno e povertà; il gesto di una madre che ricompra quello che aveva dovuto impegnare.
- Produce: il numero della polizza, uguale a quello scritto sulla custodia.

**N4 – Sotto il letto** · letto di ferro dell'alcova · *esamina*
- Sotto il letto, la custodia con lo stesso numero: dentro il violino e il **segno di Antonietta**: il ritaglio di giornale di un concerto di Amerigo, piegato con cura. Non c'è
  lettera: il senso arriva dagli oggetti, non da un testo.

**N5 – Carmine** · porta sul vicolo · *scelta senza errore*
- Bussa Carmine, nipote (figlio di Agostino, §4), ragazzino. Breve scena: Amerigo può dargli il cartellino, accordare
  il violino davanti a lui o suonare. Qualunque scelta porta alla stessa chiusura (D2).

## Cutscene (scaletta, `cs_finale_NNN`, testo scritto da zero)
- Ingresso: telefonata (di spalle, sintetica), viaggio, porta del basso. Amerigo adulto: poche frasi misurate (§8).
- Dopo N4: Amerigo suona nel basso vuoto la melodia di Alcide (R015-M5); la stanza per un attimo è tutta 1946.
- Chiusura: Amerigo e Carmine sulla soglia; voce adulta su una famiglia che si può anche scegliere. Scheda finale:
  affido familiare, art. 30 e L. 184/1983 "Diritto del minore ad una famiglia" (mod. L. 149/2001); i veri treni
  della felicità (§6). Titoli di coda con `CREDITS.md`.
- Se tutti i segreti R011 sono trovati: scena in più (Amerigo consegna a Carmine la "scatola dei ragazzi del vicolo").

## Segreti (R011)
Enigma segreto: la radio sintonizzata male; girando la manopola sulla frequenza segnata a matita sul mobile si sente
il tema del violino suonato da Amerigo adulto [GIOCO]. Collezionabili: fotografia di Amerigo in frac ritagliata da un
giornale, gettone telefonico, biglietto del treno 1994, santino nuovo, l'ex-voto del piede dell'hub (versione 1994).

## Personaggi e asset nuovi
Amerigo adulto (esiste, giocabile) · Carmine (ragazzino, modello semplice, abiti anni '90 sobri) · arredi 1994 (TV a tubo, telefono
a disco o a tasti, radio, lampadina, scatoloni) · violino e custodia · polizza del pegno (texture nostra) ·
shader/transizione tra i due tempi per angolo (blend di luci e oggetti, senza nuovo bake per ciascuno).

## Stima: **M** (layout riusato; il costo è la transizione 1994↔1946 e Carmine).

## Rischi
- Niente lettere e biglietti di Antonietta: è analfabeta (§1, §4) e la Decisione 6 esclude la lettera. Al loro posto
  oggetti e una croce al posto della firma.
- Niente la formula del film "l'amore che lascia andare" (§8): il tema resta, la frase non compare nei testi.
- La transizione a due tempi con luce baked: serve una soluzione leggera (due set di probe o luci dinamiche localizzate).

## Decisioni approvate (2026-11-07)
- **D1 – Segno di Antonietta nella custodia:** il **ritaglio di giornale di un concerto di Amerigo**, piegato con cura
  (lei non sapeva leggerlo). La mela non si usa.
- **D2 – Carmine:** breve presenza in scena con un modello semplice e due-tre battute (Decisione 7 di TRAMA).