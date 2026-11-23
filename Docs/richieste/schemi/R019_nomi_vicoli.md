# R019 – Nomi dei vicoli per la minimappa (proposta)

Data: 2026-11-07. Scelta dell'utente: nomi misti (vie principali con il nome vero, vicoletti inventati ma verosimili).
Base: zone di `SceneBuilderQuartiere.cs` (`DefinisciZone`) su `origin/feature/upgrade-realistico`. Ricerca fatta solo con fonti web e `git show`.

## 1. Nomi reali verificati

| Nome | Dove / che cosa | Origine del nome | Nel 1946 | Fonte | Affidabilità |
|---|---|---|---|---|---|
| **Via Roma** (oggi Via Toledo) | L'arteria che costeggia i Quartieri a est; nel gioco è l'uscita dal largo alto | Intitolata a Pedro de Toledo (XVI sec.). Dal 18 ott. 1870 al 1980 si chiamò Via Roma, "già Toledo" | **No: nel 1946 la targa dice Via Roma** (già Toledo). Toledo torna nel 1980 | [Wikipedia en, Via Toledo](https://en.wikipedia.org/wiki/Via_Toledo); [Comune di Napoli](https://www.comune.napoli.it/flex/cm/pages/ServeBLOB.php/L/EN/IDPagina/5731) (proposta Imbriani del 10 ott. 1870, "via Roma ex via Toledo"); [Gran Caffè Gambrinus](https://grancaffegambrinus.com/en/?p=7326) | Buona. Piccola differenza di data (10 vs 18 ottobre 1870): non serve nel gioco |
| **Vico Lungo Gelso** | Asse principale dei Quartieri, parallelo a Toledo/Roma | Dai gelsi (bachi da seta) presenti prima dei lavori di Pedro de Toledo; sistemato dal 1542. Compare già nella pianta Lafrery del 1566 e in quella di Stopendael del 1653 | **Sì, con ogni probabilità** (nome antico e molto radicato; nessuna fonte indica cambi) | [Storiacity](https://www.storiacity.it/guide/1053-vico-lungo-gelso-ai-quartieri); [Espresso Napoletano](https://www.espressonapoletano.it/vico-lungo-del-gelso-la-via-toldeo-dei-quartieri/); [Lunitalaburista](https://issuu.com/lunitalaburista/docs/lunitalaburista-numero19-20191030/s/159181) | Buona sull'origine. "Nel 1946 uguale" è deduzione, non c'è fonte esplicita |
| **Via Speranzella** | Via parallela a Lungo Gelso, "doppio isolato" con essa | Storpiatura di "via di Santa Maria della Speranza" (chiesa sul lato sud). Una guida turistica la lega a Santa Rita: versione meno documentata | **Sì, con ogni probabilità** | [Storiacity](https://www.storiacity.it/guide/1052-via-speranzella-ai-quartieri); [Scaccia la zanzara](http://scaccialazanzara.blogspot.com/2013/02/le-strade-di-napoli.html) | Media (fonti divulgative) |
| **Vico Lungo Teatro Nuovo** | Vico dei Quartieri con teatro dell'Ottocento | Dal Teatro Nuovo (vi si esibirono Bellini e Donizetti, secondo una fonte turistica) | Probabile | [Wikipedia en, Quartieri Spagnoli](https://en.wikipedia.org/wiki/Quartieri_Spagnoli); [Storiacity](https://www.storiacity.it/guide/1052-via-speranzella-ai-quartieri) (lo cita come via vicina) | Media: la storia del teatro non è stata controllata su fonte specifica |
| **Via Concordia** | Traversa dei Quartieri (presso la chiesa di Santa Maria della Concordia) | Dalla chiesa | Probabile, non verificato | [Wikipedia it, Quartieri Spagnoli](https://it.wikipedia.org/wiki/Quartieri_Spagnoli) (cita solo la chiesa); [Wikipedia en](https://en.wikipedia.org/wiki/Quartieri_Spagnoli) (cita la via in un itinerario) | Debole: manca fonte sull'origine e sull'età |
| **Via Portacarrese a Montecalvario** | Via dei Quartieri verso Montecalvario | Non trovata | Probabile, non verificato | [Wikipedia en](https://en.wikipedia.org/wiki/Quartieri_Spagnoli) (la nomina come incrocio con Vico Lungo) | Debole |
| **Pignasecca** | Mercato e zona fra i Quartieri e Montesanto | Dal "pigna secca" (albero seccato): leggenda, non documentata. Nome in uso dal Cinquecento secondo il sito Movery | Sì (il mercato esiste da secoli) | [Movery](https://movery.it/en/campania/napoli/mercato-pignasecca/); [Napolike](https://www.napolike.com/la-pignasecca-napoli) | Debole sull'origine, buona sull'esistenza del toponimo |
| **Via Pasquale Scura** | Strada ripida dei Quartieri, verso Montecalvario | Dedicata a Pasquale Scura (1791-, Guardasigilli con Garibaldi). Data di intitolazione non trovata (dopo il 1860) | Probabile | [ICSAIC (PDF)](https://www.icsaicstoria.it/wp-content/uploads/2015/05/05_3_1_12.IOLE_1.pdf); [Wikipedia it, Quartieri Spagnoli](https://it.wikipedia.org/wiki/Quartieri_Spagnoli) | Media |
| **Piazza Montecalvario** | Piazza dei Quartieri | Dalla zona di Montecalvario | Probabile | [Wikipedia it, Quartieri Spagnoli](https://it.wikipedia.org/wiki/Quartieri_Spagnoli) | Debole sull'origine |
| **Via Concezione a Montecalvario** | Via dei Quartieri | Era l'"Imbrecciata della Concezione" (data del cambio non trovata) | Incerto | [Storiacity](https://www.storiacity.it/guide/1052-via-speranzella-ai-quartieri) | Debole |

Altri nomi reali citati dalle fonti, non usati: Vico Tre Re a Toledo, Vico Sergente Maggiore, Via Nardones, Via Carlo De Cesare, Rampe di San Marcellino (non nei Quartieri).
Non trovati su fonte: **Largo Baracche** (esiste nella toponomastica napoletana, ma nessuna delle pagine lette lo cita: non usato).

Avvertenza generale: nessuna fonte consultata è un'anagrafe delle strade del 1946. Per "nome uguale nel 1946" ho usato l'antichità del toponimo. Se serve certezza assoluta, controllare uno stradario del Comune o una pianta IGM/touring dell'epoca.

## 2. Zone dell'hub e nomi proposti

Dorsale nord-sud in salita (dalla piazzetta alla stazione) e traverse. Nel gioco i nomi compaiono "lungo le strade" (`mappe.json`).

| Zona (builder) | Che cosa è | Nome sulla mappa | Tipo |
|---|---|---|---|
| `P1_Fontanella` | piazzetta con fontanella e bottega dalla luce calda | **Largo della Fontanella** | inventato |
| `M1`, `M2`, `M3` | dorsale in salita | **Vico Lungo Gelso** | reale |
| `F1` (Rampa, +1,2 m) | prima scalinata | **Rampa dei Cantari** | inventato |
| `SlargoAntonietta` | slargo davanti al basso | **Largo Sant'Anna dei Panni** | inventato (Sant'Anna di Palazzo esiste davvero: evitare "Sant'Anna di Palazzo") |
| `F2` | seconda scalinata | **Gradini della Madonnella** | inventato |
| `F3` | terza scalinata | **Rampa Tre Scalini** | inventato |
| `P2_Edicola` | slargo con la grande edicola votiva | **Piazzetta della Concordia** (richiama Via Concordia, realmente dedicata a una chiesa) oppure alternativa neutra **Largo della Madonna Illuminata** | misto / inventato |
| `F4` | quarta scalinata | **Rampa del Capitano** | inventato |
| `LargoStazione` | largo alto, uscita verso il treno | **Largo Montecalvario**, con freccia "verso Via Roma (già Toledo)" | misto |
| `T1`, `W1a`, `W1b`, `W1f`, `W1stub` | traversa e vicolo a ovest (portone sbarrato) | **Vico dei Panni Stesi** (W1), **Vico Tre Lumi** (T1) | inventati |
| `C1_Lavatoio` | cortile del lavatoio | **Cortile del Lavatoio** | inventato (nome comune, non strada) |
| `S1` | vicolo cieco | **Supportico Cieco della Lanterna** | inventato |
| `T3` | traversa con edicola piccola | **Vico Cappella Vecchia** | inventato |
| `T7`, `C3stub` | traversa con cancello | **Supportico del Cancello** | inventato |
| `C3_Terrazzino` | cortile con terrazzino | **Cortile del Terrazzino** | inventato |
| `T2` | traversa est bassa | **Vico Scalzacani** | inventato (vedi note) |
| `Lotto` | lotto bombardato | **Largo del Palazzo Crollato** (macerie dei bombardamenti del 1943) | inventato |
| `E1a`..`E1h` | vicolo laterale a est, parallelo alla dorsale | **Via Speranzella** | reale |
| `T4` | traversa est | **Vico Sette Spiriti** | inventato |
| `T5` | traversa est alta con edicola | **Via Concordia** | reale |
| `S2` | vicolo cieco alto con casse | **Supportico delle Casse** | inventato |
| `T6` | traversa alta | **Via Portacarrese a Montecalvario** | reale |

Così le sei strade vere (Via Roma sull'uscita, Lungo Gelso, Speranzella, Concordia, Portacarrese, Largo/Piazza Montecalvario) fanno da spina, e ogni vicoletto è inventato. Nota onesta per i crediti: l'hub è una Napoli ricomposta, non una pianta fedele; la topologia vera dei Quartieri (griglia spagnola con "decumani" e "cardini") è solo evocata.

## 3. Collisioni con nomi reali (da controllare)

Nomi inventati scelti per non coincidere con vie note, ma **non ho potuto verificarli su uno stradario**: la ricerca non copre l'elenco completo delle strade di Napoli.
- Probabili omonimi di nome generico: "Cortile del Lavatoio", "Supportico delle Casse", "Vico Cappella Vecchia" (a Napoli esistono molte "cappelle"). Rischio basso perché sono descrittivi e non indicano un quartiere.
- Evitati di proposito perché so che esistono davvero o sono dubbi: Vico Pazzariello, Vico Paradiso, Vico Tre Regine, Vico Tre Re, Vico Scassacocchi, Vico Sette Dolori, Sant'Anna di Palazzo, Largo Baracche.
- "Vico Scalzacani" e "Vico Sette Spiriti": hanno sapore napoletano ma potrebbero esistere. Alternative neutre: **Vico dei Cordai**, **Vico del Pozzo Cieco**.
- Azione consigliata: prima di fissare `mappe.json`, controllare ogni nome inventato su stradario comunale o OpenStreetMap, e dichiarare in `CREDITS.md` "nomi dei vicoli: reali per le vie principali, inventati per i vicoletti".

## 4. Note per chi implementa

- Anacronismo: sulla mappa del 1946 la strada grande si chiama **Via Roma**, non Toledo. Nel finale 1994 (R016) sarà **Via Toledo**. Se l'hub è "Napoli della memoria" (D1 raccomandata), usare Via Roma, magari con "già Toledo" in piccolo.
- Nelle schede sul quartiere si può scrivere che Vico Lungo Gelso prende il nome dai gelsi dei bachi da seta, e Speranzella dalla chiesa di Santa Maria della Speranza. Sono fatti di origine divulgativa: indicarli come "secondo la tradizione".
- Non scrivere le leggende di Pignasecca o del bordello medievale di Lungo Gelso (riportata solo da blog turistici).
