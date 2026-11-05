# TRAMA.md - Bibbia narrativa de "Il treno dei bambini" (escape room)

Documento di lavoro per il gioco. Prima stesura: ricerca web del 2026-11-05. Revisione e verifica delle fonti: 2026-11-05.
Scritto con parole nostre: nessun dialogo o frase del libro o del film è riprodotto o parafrasato da vicino.

## Decisioni dell'utente

> **Approvate tutte dall'utente il 2026-11-05**, così come raccomandate qui sotto. Sono vincolanti per il gioco.

1. **Cornice nel 1994.** Mostrare Amerigo adulto (≈55 anni) che torna nel basso alla morte della madre, con i flashback giocabili nel 1946? *Raccomandazione:* sì, Opzione A (§7.1): poche cutscene e una sola stanza-cornice nel 1994; hub e basso del 1946 restano come sono.
2. **Famiglia di Modena: versione del libro o del film?** Libro: Derna sola, la coppia di cugini Rosa e Alcide Benvenuti con tre figli. Film: Alcide è il fratello di Derna. *Raccomandazione:* libro (fonte primaria, meglio documentata); nei testi dire solo "la famiglia Benvenuti", senza insistere sulle parentele finché non le si verifica sul testo.
3. **Capa 'e fierro.** Nel libro è l'uomo a cui Amerigo porta gli stracci raccolti; nel gioco oggi è un "vicino testardo" (`luoghi.json`, `casa_capa_e_fierro`). *Raccomandazione:* farne il robivecchi del flashback "Stracci" (brontolone ma non cattivo), con battute nostre.
4. **Maddalena.** Nel romanzo e nel film è un personaggio inventato, **Maddalena Criscuolo**; la versione precedente di questo file la scambiava per la partigiana reale Maddalena Cerasuolo. *Raccomandazione:* nel gioco chiamarla solo "Maddalena", personaggio di finzione; se si vuole parlare delle donne delle Quattro giornate, farlo in una scheda didattica separata, con fonti, senza far parlare la persona reale.
5. **Persone reali (Teresa Noce, Maurizio Valenzi, Giorgio Amendola, Maddalena Cerasuolo).** *Raccomandazione:* mai come personaggi con battute; solo citati in schede o testi informativi con date verificate. Amendola non citarlo finché il suo ruolo nel Comitato napoletano non è confermato (§6).
6. **Il finale (lettera nel violino).** La lettera di Antonietta nella custodia è un'invenzione del **film**. *Raccomandazione:* non usarla; finale nostro costruito su elementi comuni a libro e film (il violino conservato sotto il letto) più un oggetto inventato, per esempio la ricevuta del Monte dei pegni riscattata e un piccolo segno lasciato da Antonietta (oggetto, non testo lungo). Testo del finale scritto da zero.
7. **Carmine e Agostino** (fratellino e nipote, solo nel libro). *Raccomandazione:* includere Carmine come breve presenza nel finale 1994 (passaggio del testimone), senza dettagli giudiziari sui genitori, che sono confermati da una sola fonte.
8. **Gioco delle scarpe a punti.** È un'idea del romanzo e la cutscene già fatta (`cutscene_scarpe.json`, battuta "Quelle dei signori valgono un punto, le nostre mezzo") ci si avvicina molto. *Raccomandazione:* l'idea si può tenere (le idee non sono protette, il testo sì), ma conviene riscrivere quella battuta con un meccanismo nostro (per esempio Amerigo indovina il mestiere dalle scarpe), così il gioco ha qualcosa di suo.
9. **Mela annurca.** *Raccomandazione:* nessun oggetto a parte, solo un dettaglio nella cutscene della partenza (oggetto "Cappotto"), per non allungare lo sviluppo.
10. **Dicerie macabre** (bambini in Russia, mutilati, nei forni). *Raccomandazione:* per la secondaria di 2° grado si possono nominare come voci dell'epoca da smontare con le fonti (enigma vero/falso), senza immagini.
11. **Età di Amerigo.** Libro: 7 anni, quasi 8; film: 8. *Raccomandazione:* 7 anni (coerente con CLAUDE.md).

---

**Legenda delle etichette**
- **[LIBRO]** romanzo di Viola Ardone (Einaudi, Stile Libero, 2019)
- **[FILM]** film di Cristina Comencini (Palomar/Netflix, 2024)
- **[STORIA REALE]** fatti storici dei "treni della felicità" e del contesto
- **[GIOCO: proposta]** invenzione nostra per colmare lacune o adattare il gioco
- **(da verificare)** confermato da una sola fonte, fonti in conflitto, o non trovato

**Avvertenza sull'affidabilità.** Né chi ha scritto né chi ha rivisto questo file ha letto il romanzo o visto il film. La ricostruzione si basa su fonti secondarie. La più solida per il libro è la recensione analitica della rivista *Minori giustizia* (FrancoAngeli, n. 4/2019), che conferma: quattro sezioni, ultima nel 1994, Modena, età, famiglia Benvenuti, violino. Prima di scrivere i testi definitivi conviene un controllo sul libro, anche veloce.

---

## 1. Trama del libro [LIBRO]

Romanzo in prima persona, narrato da Amerigo Speranza. **Quattro sezioni** (*Minori giustizia*; Reading Matters; Reading in Translation): le prime tre raccontano l'infanzia (Napoli; Modena; ritorno e fuga), la quarta è ambientata nel **1994** e ha la voce dell'adulto. Le prime tre sezioni hanno una voce infantile con inserti dialettali. Alcune recensioni parlano di "due parti" (infanzia / età adulta): raggruppano lo stesso materiale in modo diverso. Titoli dei capitoli non verificati.

**Sezione 1 - Napoli, autunno 1946.** Amerigo ha **sette anni, quasi otto** (*Minori giustizia*; Studenti.it; Il Fatto Quotidiano) e vive con la madre Antonietta in un basso dei **Quartieri Spagnoli**. Antonietta non ha marito; ad Amerigo si dice che il padre è partito per l'America. Il fratello maggiore (Luigi secondo SoloLibri) è morto di malattia (bronchite) prima che Amerigo nascesse. La madre è analfabeta, ruvida, poco abituata alle tenerezze. Amerigo è sveglio e bravo coi numeri (nel quartiere ha un soprannome scherzoso da "genio"), a scuola va poco. Raccoglie stracci per le case e nelle discariche e li porta a **Capa 'e fierro**, il robivecchi (estratto del libro su Everand; SoloLibri). Passa il tempo con l'amico **Tommasino** e ha il gioco di osservare le scarpe dei passanti. Le sue scarpe sono rotte o troppo strette. PCI e UDI organizzano i treni per portare i bambini poveri presso famiglie del Centro-Nord; nel quartiere girano dicerie terribili. La militante **Maddalena Criscuolo**, giovane compagna che ha partecipato alla liberazione di Napoli (SoloLibri; Reading in Translation), aiuta a organizzare e convince Antonietta. Al saluto Antonietta gli dà una **mela annurca**. Sul treno ci sono anche Tommasino e **Mariuccia** (Il Fatto Quotidiano; Wikipedia it). Nel romanzo compare di sfuggita anche Maurizio Valenzi, futuro sindaco di Napoli (*Minori giustizia*). Il trucco dei topi venduti come animaletti è attestato per il film; per il libro (da verificare).

**Sezione 2 - Modena, inverno 1946-primavera 1947.** Amerigo viene accolto da **Derna**, donna sola e senza figli, militante comunista (vedova secondo Reading Matters: da verificare). Se ne occupa insieme alla coppia di cugini **Rosa e Alcide Benvenuti**, che hanno tre figli, **Rivo, Luzio e Nario** (*Minori giustizia*; QLibri; Reading Matters; SoloLibri). Uno dei ragazzi, quasi coetaneo, all'inizio è geloso (*Minori giustizia*). Rosa ha un forno (QLibri). **Alcide** è accordatore di pianoforti (SoloLibri; Publishers Weekly; Reading Matters lo dice insegnante di piano) e diventa una figura paterna: scopre l'orecchio di Amerigo e gli dà un piccolo violino (costruito da lui secondo SoloLibri) con le lezioni. Derna gli insegna *Bella ciao* (Reading in Translation). Amerigo va a scuola, si apre, ma si sente diviso tra le due famiglie. Città: **Modena**, confermata da quasi tutte le fonti; le indicazioni Bologna (Reading Matters), Ferrara (una recensione QLibri) e Milano (Wikipedia it, libro) sono isolate e vanno considerate errori.

**Sezione 3 - Ritorno a Napoli, estate 1947.** Finito l'anno scolastico, i bambini tornano a casa. Antonietta non capisce i cambiamenti del figlio ed è ostile verso la famiglia del Nord; il violino finisce venduto o impegnato per necessità (QLibri; Cinefilos per il film). Lo scontro diventa rottura: Amerigo scappa da solo in treno verso il Nord, aiutato da una viaggiatrice che lo fa passare per suo figlio (*Minori giustizia*), e non torna più a vivere a Napoli.

**Sezione 4 - 1994.** Amerigo, ora **Amerigo Benvenuti**, violinista affermato (*Minori giustizia*; QLibri; Cinefilos), sulla cinquantina, torna a Napoli alla notizia della morte improvvisa della madre. Da adulto aveva avuto qualche contatto con lei, anche incontrandola, senza recuperare un vero rapporto (*Minori giustizia*). Scopre che lei non l'aveva dimenticato e che aveva conservato il violino (intervista ad Ardone su ArStorica; il traduttore inglese parla del violino ritrovato sotto il letto della madre, commento su Reading in Translation). Conosce **Carmine**, figlio di **Agostino**, il fratellino nato dopo la sua partenza (ArStorica; QLibri; schede librarie): Carmine diventa per lui l'occasione di restituire la solidarietà ricevuta (ArStorica). Che i genitori di Carmine siano stati arrestati e che Amerigo pensi di adottarlo lo dice solo QLibri (da verificare). L'incontro con Maddalena anziana: una sola fonte (da verificare).

**Dall'intervista all'autrice (ArStorica, 2025).** Ardone si è basata sui racconti del padre (nato nel 1945) e della zia e sulla testimonianza di un ex bambino dei treni, poi si è documentata. Per lei la mela è un dono che chiede di essere ricordati; Amerigo non la mangia e la lascia appassire, immagine di un affetto che si sciupa se non viene vissuto. Tema: la genitorialità può essere anche scelta.

**Fonte reale rivendicata.** Lo storico Giovanni Rinaldi, sul proprio blog (giorinaldi.com), scrive che il romanzo si ispira, senza citarla, alla storia di **Americo Marino** (bambino di San Severo ospitato ad Ancona nel 1950) e di **Derna Scandali** (sindacalista CGIL), raccontata nel suo libro *I treni della felicità*. È la posizione di una parte; non l'ho trovata in fonti indipendenti. Amerigo resta un personaggio di finzione.

## 2. Trama del film e differenze [FILM]

Regia di Cristina Comencini; sceneggiatura di Furio Andreotti, Giulia Calenda, Cristina Comencini, Camille Dugay; musiche di Nicola Piovani. Anteprima alla Festa del Cinema di Roma il 20 ottobre 2024, su Netflix dal 4 dicembre 2024 (Wikipedia it/en). Interpreti principali: Christian Cervone (Amerigo bambino), Stefano Accorsi (Amerigo adulto), Serena Rossi (Antonietta), Barbara Ronchi (Derna), Ivan Zerbinati (Alcide Benvenuti), Giorgia Arena (Rosa Benvenuti), Francesco Di Leva (Capa 'e fierro), Antonia Truppo (Maddalena Criscuolo), Dora Romano (Pachiochia), Monica Nappo (Zandragliona), Beatrice Schiros (maestra Ferrari) (Wikipedia it; alcuni database invertono Pachiochia/Zandragliona: da verificare sui titoli di coda).

**Trama (sintesi).**
- **1994, apertura.** Amerigo, violinista di successo, sta per esibirsi quando apprende per telefono la morte della madre (Wikipedia it/en; Wikipedia it colloca la scena a Torino, marzo 1994: da verificare).
- **Napoli, autunno 1946.** Amerigo ha **otto anni** (Wikipedia it/en). Dopo la truffa dei topi colorati Antonietta lo iscrive ai treni, convinta da Maddalena. Alla partenza una vecchia semina paura.
- **Modena.** Lo prende **Derna**, ex partigiana che vive sola dopo aver perso il fidanzato in guerra. Nel film **Alcide è il fratello di Derna** e ha i figli Revo, Lucio e Nario (Wikipedia en). Il ruolo di Rosa Benvenuti nel film non è descritto dalle fonti (forse moglie di Alcide: da verificare). Alcide gli insegna il violino e Amerigo ne riceve uno tutto suo. Lucio è geloso, poi lo difende; insieme aiutano Rossana, una bambina che scappa per nostalgia.
- **Ritorno.** Antonietta gli toglie il violino e lo manda a lavorare; dopo mesi senza notizie Amerigo scopre che la madre ha nascosto lettere e pacchi di Derna e ha impegnato il violino al Monte dei pegni. Fugge in treno e torna da Derna, dove resta (Wikipedia it/en).
- **1994, finale.** Nella casa della madre, sotto il letto, trova la custodia con il violino riscattato e una lettera: Antonietta sapeva della fuga, era in contatto con Derna e aveva scelto di non riprenderlo, per dargli una vita migliore (Wikipedia it/en; Cinefilos; Everyeye). Il film chiude sul tema dell'amore che lascia andare.

**Differenze principali col libro** (le fonti non le elencano in modo sistematico, quindi con cautela):
- **Famiglia ospitante:** libro, Rosa e Alcide sono cugini di Derna e coppia; film, Alcide è il fratello di Derna.
- **Derna:** nel film ex partigiana; nel libro donna sola e militante (vedova solo per una fonte).
- **Età:** 7 anni nel libro, 8 nel film.
- **Finale:** nel film c'è la lettera nella custodia del violino; nel libro il peso del finale è su Carmine e sul ritorno alle origini. Agostino e Carmine non risultano nelle sintesi del film.
- **Personaggi:** il film dà più spazio a Rossana, a Lucio e alla vecchia delle dicerie (Rossana nel libro: da verificare).

## 3. Cronologia con date e luoghi

| Quando | Dove | Cosa | Etichetta |
|---|---|---|---|
| 16 dic 1945 | Milano → Reggio Emilia | Primo convoglio dei treni della felicità, circa 1.800 bambini milanesi | STORIA REALE |
| gen 1946 | Italia | Al V Congresso del PCI si decide di estendere l'iniziativa al Sud | STORIA REALE |
| 10 mar 1946 e seguenti | Italia | Elezioni amministrative: prime votazioni con le donne | STORIA REALE |
| 2-3 giu 1946 | Italia / Napoli | Referendum istituzionale e Assemblea Costituente, primo voto nazionale delle donne. A Napoli la monarchia supera l'80% (Avvenire) | STORIA REALE |
| 11 giu 1946 | Napoli, via Medina | Scontri davanti alla federazione del PCI dopo il referendum; circa 9 morti secondo AGI e Panorama, le cifre variano | STORIA REALE |
| autunno 1946 | Napoli, Quartieri Spagnoli | Amerigo, gli stracci, le scarpe; partenza (novembre secondo una recensione QLibri) | LIBRO |
| dic 1946-estate 1947 | Napoli → Centro-Nord | Partono da Napoli circa 10-12.000 bambini | STORIA REALE |
| inverno 1946-47 | Modena e campagna | Amerigo dai Benvenuti, violino, scuola | LIBRO/FILM |
| fine anno scolastico 1947 | Napoli | Ritorno; violino impegnato o venduto, lettere e pacchi trattenuti | LIBRO/FILM |
| 1947 | Napoli → Modena | Fuga di Amerigo verso il Nord | LIBRO/FILM |
| 1 gen 1948 | Italia | Entra in vigore la Costituzione | STORIA REALE |
| 1994 | Napoli, basso di Antonietta | Morte di Antonietta, ritorno di Amerigo (≈55 anni), violino; Carmine (libro), lettera (film) | LIBRO/FILM |

Nota: la partenza a novembre nel romanzo anticipa di poco i primi convogli storici da Napoli (Natale 1946 secondo Argo Catania): licenza del romanzo o dettaglio da precisare (da verificare). Per il gioco "Napoli 1946" va bene.
**Attenzione all'anacronismo:** la Costituzione è del 1948. Nei flashback 1946 i personaggi non possono citarla; il collegamento con gli articoli lo fanno l'interfaccia didattica o Amerigo adulto.

## 4. Personaggi

Le fonti non descrivono quasi mai l'aspetto fisico: dove manca è **[GIOCO: proposta]**. I capelli bipartiti di Amerigo (CLAUDE.md) non risultano dalle fonti e non le contraddicono.

- **Amerigo Speranza (poi Benvenuti).** [LIBRO/FILM] Narratore; 7 anni quasi 8 nel 1946 (libro), 8 nel film. Sveglio coi numeri, orgoglioso, sa arrangiarsi. Arco: da bambino che nessuno guarda a musicista che sceglie la sua strada, poi il ritorno tardivo alle origini. Adulto: violinista affermato, sui 55 anni nel 1994. Dove viva da adulto le fonti non lo dicono (al Nord). [GIOCO: proposta] adulto: giacca scura sobria anni '90, cappotto lungo, capelli bipartiti in parte grigi, custodia del violino.
- **Antonietta Speranza.** [LIBRO/FILM] Madre, analfabeta, ruvida, ama coi fatti. Nel libro ha poi un altro figlio, Agostino. [GIOCO: proposta] circa 30 anni nel 1946; vestito scuro logoro, scialle, grembiule.
- **Derna.** [LIBRO] Donna sola, senza figli, militante comunista. [FILM] ex partigiana. [GIOCO: proposta] 30-40 anni; vestito scuro semplice, fazzoletto, cappotto di lana rammendato.
- **Rosa Benvenuti.** [LIBRO] Cugina di Derna (insieme al marito Alcide), madre dei tre ragazzi, ha un forno. [FILM] presente nel cast, ruolo non descritto.
- **Alcide Benvenuti.** [LIBRO] Marito di Rosa, accordatore di pianoforti, guida musicale e figura paterna, gli dà il violino. [FILM] Fratello di Derna, gli insegna il violino. [GIOCO: proposta] gilet, maniche rimboccate, cappello di feltro.
- **Rivo, Luzio, Nario.** [LIBRO] Figli di Rosa e Alcide; uno è geloso all'inizio. [FILM] Revo, Lucio, Nario. [GIOCO: proposta] pantaloni corti rattoppati, ginocchia sbucciate.
- **Tommasino.** [LIBRO/FILM] Migliore amico, parte con lui. Il dettaglio sul suo ritorno e i contatti con gli affidatari non è verificato.
- **Mariuccia.** [LIBRO/FILM] Bambina del vicolo che parte con loro (Il Fatto Quotidiano; Wikipedia it). Nel libro un bambino scambia la neve per ricotta (una recensione; chi sia: da verificare). La sua storia successiva: da verificare.
- **Maddalena Criscuolo.** [LIBRO/FILM] Giovane militante comunista che ha partecipato alla liberazione di Napoli, organizza i treni e convince Antonietta (SoloLibri; Reading in Translation; Wikipedia it/en film, interpretata da Antonia Truppo). **Personaggio di finzione.** La prima stesura la identificava con la partigiana reale **Maddalena "Lenuccia" Cerasuolo** (Napoli 1920-1999, Quattro giornate, ponte della Sanità, medaglia di bronzo al valor militare il 24 maggio 1946, Wikipedia en): **errore**. La pagina Wikipedia en su Cerasuolo elenca il romanzo di Ardone tra i libri che la citano o la menzionano; il legame preciso non è chiaro (da verificare sul testo). Nel gioco: personaggio di finzione, si chiama solo "Maddalena".
- **Capa 'e fierro.** [LIBRO] Robivecchi/commerciante di stracci per cui Amerigo raccoglie stracci; secondo una sintesi commerciava in abiti usati con Antonietta. [FILM] Francesco Di Leva. Nel gioco oggi è un vicino testardo (vedi Decisione 3).
- **Pachiochia (non "Pacchiana") e Zandragliona.** [LIBRO/FILM] Donne del vicolo con nomi popolari (SoloLibri le nomina per il libro; Wikipedia it per il cast). Nel film la vecchia che semina paura alla partenza è con ogni probabilità la Pachiochia di Dora Romano (da verificare). [GIOCO: proposta] figura che incarna la paura e la disinformazione, non una cattiva.
- **Rossana.** [FILM] Bambina ospite che scappa per nostalgia; Amerigo e Lucio la aiutano. [LIBRO] (da verificare).
- **Agostino.** [LIBRO] Fratellino di Amerigo, nato dopo la sua partenza o la sua fuga (le fonti non concordano su quale).
- **Carmine.** [LIBRO] Figlio di Agostino, nipote di Amerigo, figura del finale 1994 (ArStorica; QLibri). Genitori arrestati: solo QLibri (da verificare).
- **Fratello maggiore.** [LIBRO] Morto di malattia prima della nascita di Amerigo; Luigi secondo SoloLibri (da verificare).
- **Persone reali citate nel libro:** Maurizio Valenzi, futuro sindaco di Napoli (*Minori giustizia*).

## 5. Oggetti simbolici e temi civici

Gli articoli della Costituzione (in vigore dal 1948) sono collegamenti didattici di oggi, non cose che i personaggi del 1946 possono sapere.

| Oggetto | Significato narrativo | Tema civico |
|---|---|---|
| **Scarpe** | Le scarpe rotte, il gioco di osservarle, le scarpe nuove per il viaggio | Povertà, dignità, uguaglianza (art. 3), tutela dell'infanzia (art. 31), Convenzione ONU sui diritti dell'infanzia (1989) |
| **Stracci** | Il lavoro di Amerigo per Capa 'e fierro | Lavoro minorile (art. 37), economia di sopravvivenza, borsa nera |
| **Volantino / manifesto del treno** | Informazione contro le dicerie; chi organizza e chi diffida | Disinformazione e fonti, libertà di espressione (art. 21), associazioni e partiti (artt. 18, 49), UDI, voto alle donne |
| **Cappotto** | Vestiti dati ai bambini per il viaggio (storicamente in gran parte dalla Croce Rossa, Argo Catania) | Solidarietà (art. 2), accoglienza |
| **Mela annurca** | Il dono di Antonietta alla partenza | Legami familiari (art. 30) |
| **Violino** | Talento, possibilità, poi perdita e ritrovamento | Diritto allo studio (art. 34), cultura (art. 9) |
| **Lettere e pacchi di Derna** | Legame negato, analfabetismo | Istruzione, comunicazione |
| **Finale (violino ritrovato + oggetto nostro)** | Ritorno alle origini, perdono tardivo | Affido familiare: art. 30 e **L. 4 maggio 1983 n. 184** "Diritto del minore ad una famiglia" (modificata dalla L. 149/2001); genitorialità scelta |

Temi trasversali: voto alle donne (amministrative di marzo 1946, poi 2 giugno 1946; artt. 3 e 48), Costituente e Costituzione (1946-48), pregiudizio Nord-Sud, ruolo delle donne nell'UDI e nella Resistenza.

## 6. Contesto storico [STORIA REALE]

**Treni della felicità.** Iniziativa ideata da Teresa Noce e dall'UDI e promossa dal PCI, con il sostegno di ANPI, CGIL e Croce Rossa, attiva tra il 1945 e il 1952 (Wikipedia en; Il Fatto Quotidiano). In tutto circa **70.000** bambini del Sud e del Centro (Lazio, Campania, Puglia, Calabria, Sicilia, Sardegna) ospitati in Emilia-Romagna, Toscana, Marche, Umbria, Veneto, Lombardia (Wikipedia en; *Minori giustizia*). Primo convoglio: **16 dicembre 1945**, circa 1.800 bambini da Milano a Reggio Emilia (Wikipedia en, da Rinaldi). Il V Congresso del PCI (gennaio 1946) decise di concentrarsi sul Sud. Bambini di 4-12 anni, di solito per i mesi freddi (Il Fatto Quotidiano). Il nome "treni della felicità" sarebbe del sindaco di Modena Alfeo Corassori (una fonte, da verificare). A Modena l'accoglienza dei bambini napoletani va dal 1946 al 1948 (giorinaldi.com).

**Da Napoli.** Tra il **Natale 1946 e l'estate 1947** partirono circa **10.000-12.000** bambini napoletani (Argo Catania 12.000; Wikipedia en 12.000 "in quell'anno"; *Minori giustizia* 10.000 in totale). I bambini venivano lavati, vaccinati, nutriti, schedati e vestiti (Argo Catania). Organizzazione: **Comitato per la salvezza dei bambini di Napoli** (attivo 1946-1954, Giulia Buffardi). Tra gli organizzatori napoletani c'era Maurizio Valenzi (*Minori giustizia*). **La presidenza di Giorgio Amendola, data per certa nella prima stesura, non è confermata da nessuna fonte trovata: rimossa (da verificare sul libro di Buffardi).** Bibliografia: Giovanni Rinaldi, *I treni della felicità* (Ediesse, 2009); Giulia Buffardi (a cura di), *Il Comitato per la salvezza dei bambini di Napoli 1946-1954* (Editori Riuniti, 2016); Gaetano Macchiaroli (1979); documentario *Pasta nera* di Alessandro Piva (2011).

**Dicerie.** Diffidenza soprattutto nel mondo cattolico, perché organizzavano i comunisti; circolavano voci che i bambini sarebbero stati mandati in Russia, mutilati, mangiati o mandati nei forni (Argo Catania; Il Fatto Quotidiano; *Minori giustizia*). La storia della "saponetta" non risulta in nessuna fonte: non usarla.

**Napoli 1946.** Referendum: in città la monarchia supera l'80% (Avvenire; Panorama parla di "schiacciante maggioranza"; cifra esatta da prendere dall'Archivio storico delle elezioni del Ministero dell'Interno). **11 giugno 1946**, via Medina: scontri tra monarchici e forze dell'ordine davanti alla federazione comunista, circa 9 morti (AGI; Panorama; Startmag dà cifre un po' diverse). La prima stesura diceva 6 giugno: corretto. Macerie, borsa nera, fame, bassi sovraffollati, presenza alleata: quadro generale credibile ma da documentare con una fonte dedicata prima di farne testo nel gioco (da verificare).

**Modena/Emilia.** Accoglienza organizzata da sezioni UDI e PCI, amministrazioni locali e scuole, spesso da famiglie contadine e operaie non ricche (*Minori giustizia*; Wikipedia en).

**Napoli 1994.** Antonio Bassolino eletto sindaco al ballottaggio del 5 dicembre 1993; **vertice G7 a Napoli, 8-10 luglio 1994** (G7 Information Centre, Università di Toronto; IAI). Se la morte di Antonietta è a marzo (film), il G7 è ancora da venire: al massimo un manifesto o un cantiere. Dettagli materiali del basso nel 1994 (TV, telefono, scooter, fili elettrici): plausibili, [GIOCO: proposta].

## 7. Proposta per il gioco [GIOCO: proposta]

### 7.1 Struttura
- **Cornice (1994):** Amerigo adulto (≈55 anni) torna a Napoli per la morte di Antonietta ed entra nel basso svuotato. Ogni oggetto trovato apre un **ricordo**, cioè l'oggetto/flashback dell'escape room. Hub vicolo e basso restano nel **1946**, come già costruiti.
- **Opzione A (consigliata):** presente 1994 solo in **brevi cutscene** (Timeline) e in una **stanza-cornice** (il basso nel 1994: spoglio, lampadina, calendario 1994, telefono). Serve un secondo set leggero e un look distinto per il presente (più freddo e neutro, meno grana), così il 1946 resta caldo e i flashback più seppia, come vuole CLAUDE.md.
- **Opzione B:** vicolo 1994 giocabile. Costosa (insegne, auto, cavi), sconsigliata per la Fase 2.
- **Ambienti 1946 già fatti:** nessuna modifica.

### 7.2 Oggetti e flashback (uno alla volta; ogni nuovo oggetto solo con l'ok dell'utente)

| Oggetto | Luogo | Scena flashback | Emozione | Tema | Idee di enigmi (non punitivi) |
|---|---|---|---|---|---|
| **Scarpe** (fatto) | Basso / vicolo | Le scarpe di Amerigo e Tommasino | Malinconia | Povertà, dignità | Già fatti; rivedere la battuta sui punti (Decisione 8) |
| **Stracci** | Bottega di Capa 'e fierro | Amerigo porta gli stracci e contratta | Ingegno, imbarazzo | Lavoro minorile, economia di sopravvivenza | Abbina "mestiere di bambino - diritto di oggi"; conti leggeri sul prezzo degli stracci; ordina le fasi del baratto; nascosto: attrezzi del robivecchi |
| **Volantino del treno** | Sede UDI/PCI, vicolo | Maddalena spiega, le madri discutono, la Pachiochia sparge voci | Paura, speranza | Dicerie e fonti, UDI, voto alle donne | Vero/falso sulle dicerie con fonti; ordina la cronologia dic 1945-giu 1946; abbina chi-faceva-cosa; lucchetto con la data del referendum |
| **Cappotto** | Stazione / treno | Vestiti nuovi, la mela di Antonietta, la partenza | Sorpresa, nostalgia | Solidarietà (art. 2), accoglienza | Nascosto: cartellini con i nomi; abbina bambino-cappotto con indizi; quiz sulla solidarietà |
| **Violino** | Modena, casa Benvenuti | Prime note con Alcide, scuola, gelosia del ragazzo | Meraviglia, orgoglio | Istruzione (art. 34), pregiudizio Nord-Sud, cultura | Ordina le note di una melodia semplice; abbina strumento-suono; quiz sul pregiudizio |
| **Finale** | Basso 1994 | Ritorno, fuga, violino ritrovato, Carmine | Commozione | Famiglia, affido, genitorialità scelta | Nessun errore punitivo: ricomporre un oggetto nostro (es. ricevuta del Monte dei pegni), poi cutscene finale con testo originale |

Mela annurca: dettaglio della cutscene "Cappotto" (Decisione 9).

### 7.3 Elementi del presente 1994
Lampadina, calendario 1994, radio, televisore a tubo, telefono fisso, scatola di latta. Niente cellulari. Cappotto lungo di Amerigo adulto, custodia del violino.

## 8. Guida ai dialoghi

Registro: italiano semplice con **tocchi di napoletano comprensibile** (es. "guagliò", "mammà", "jamme") senza trascrizioni pesanti. Frasi corte, immagini concrete, niente prediche. **Nessuna battuta del libro o del film, né parafrasi vicine**; in particolare evitare la frase del film sull'amore che "lascia andare" e le formule che le fonti attribuiscono alla madre nel libro.

- **Amerigo bambino:** vivace, ironico, fa domande, conta tutto.
- **Antonietta:** poche parole, imperativi; l'affetto passa dai gesti (rammendi, cibo).
- **Derna:** italiano misurato con qualche cadenza emiliana; pratica, severa nel tono, gentile nei gesti.
- **Rosa / Alcide:** calore, battute su pane e musica; Alcide parla per immagini sonore.
- **Maddalena:** diretta, energica, organizzatrice.
- **Tommasino:** scherzoso e realista.
- **Capa 'e fierro:** brontolone, mercanteggia, in fondo buono.
- **Amerigo adulto:** italiano piano e sobrio, frasi misurate.

Esempi ORIGINALI (scritti per il gioco, non ripresi da libro o film):
- Amerigo (stracci): "Questi non sono stracci, signo'. Ognuno ha una casa da cui è venuto."
- Capa 'e fierro (stracci): "Tre soldi. E non mi guardare così, che il prezzo non cresce con gli occhi."
- Maddalena (volantino): "Non vi chiedo di fidarvi di me. Vi chiedo di leggere, e di venire a vedere."
- Derna (arrivo): "Qui la stufa è accesa. Il resto lo impariamo un giorno alla volta."
- Alcide (violino): "Prima di suonare, ascolta come suona la stanza."
- Amerigo adulto (basso 1994): "Le pareti sono più basse di come le ricordavo. O sono io che ho smesso di essere piccolo."

Da evitare: caricature del Sud o del Nord, propaganda (PCI, UDI e Chiesa raccontati come attori storici, senza giudizio), immagini macabre, anacronismi (Costituzione citata dai personaggi del 1946), battute di libro o film.

## 9. Punti ancora incerti

1. Ruolo di **Rosa Benvenuti** nel film (moglie di Alcide?).
2. **Derna** nel libro: vedova? Partigiana? (una sola fonte per "vedova").
3. **Agostino:** nato dopo la partenza o dopo la fuga?
4. **Carmine:** arresto dei genitori e adozione (solo QLibri).
5. **Maddalena anziana** nella sezione 4 (una sola fonte); legame tra il personaggio e Maddalena Cerasuolo citato da Wikipedia en.
6. **Rossana** e **topi colorati** nel libro (confermati solo per il film).
7. **Pachiochia/Zandragliona:** quale delle due è la vecchia delle dicerie nel film.
8. **Presidenza del Comitato napoletano** (Amendola non confermato).
9. **Mese della partenza nel libro** (novembre?) rispetto ai convogli storici (Natale 1946).
10. **Data e luogo della scena d'apertura 1994 nel film** (Torino, marzo?).
11. **Dove vive Amerigo adulto** (non indicato).
12. **Napoli 1946 come ambiente** (macerie, alleati, borsa nera): servono fonti dedicate prima di scriverne testi informativi.
13. **Aspetto fisico dei personaggi:** nessuna fonte, tutto è proposta.

---

## Fonti (consultate il 2026-11-05)

Libro e film (pagine lette):
- *Minori giustizia*, n. 4/2019, recensione de *Il treno dei bambini*, FrancoAngeli: https://francoangeli.it/rivista/getArticoloFree/65663/En
- https://it.wikipedia.org/wiki/Il_treno_dei_bambini
- https://it.wikipedia.org/wiki/Il_treno_dei_bambini_(film)
- https://en.wikipedia.org/wiki/The_Children%27s_Train
- https://readingmattersblog.com/2023/08/20/the-childrens-train-by-viola-ardone-translated-by-clarissa-botsford/
- https://readingintranslation.com/2024/12/03/viola-ardones-the-childrens-train/
- https://www.publishersweekly.com/9780062940513
- https://www.qlibri.it/narrativa-italiana/romanzi/il-treno-dei-bambini/
- https://www.sololibri.net/Il-treno-dei-bambini-Ardone.html
- https://www.studenti.it/il-treno-dei-bambini-trama-e-riassunto-del-romanzo-di-viola-ardone.html
- https://www.studenti.it/il-treno-dei-bambini-storia-personaggi-messaggio-romanzo-viola-ardone.html
- https://www.arstorica.it/2025/03/02/il-treno-dei-bambini-intervista-a-viola-ardone/
- https://www.cinefilos.it/tutto-film/approfondimenti/il-treno-dei-bambini-la-storia-vera-dietro-al-film-amerigo-benvenuti-e-un-vero-violinista-658974
- https://www.cinefilos.it/tutto-film/approfondimenti/il-treno-dei-bambini-la-spiegazione-del-finale-cosa-succede-al-violino-di-amerigo-681868
- https://thesouthernbooksellerreview.org/the-childrens-train-by-viola-ardone/
- https://giorinaldi.com/2019/10/29/i-treni-della-felicita-memorandum-per-gli-smemorati/ (blog di Giovanni Rinaldi, fonte di parte)

Libro e film (solo da risultati di ricerca, non letti per intero):
- estratto del romanzo su Everand (Capa 'e fierro e gli stracci): https://www.everand.com/book/488460142/The-Children-s-Train-A-Novel
- https://cinema.everyeye.it/notizie/treno-bambini-spiegazione-finale-film-italiano-conquistato-netflix-764418.html
- https://www.sololibri.net/Il-treno-dei-bambini-film-Netflix-Cristina-Comencini-recensione.html

Storia:
- https://en.wikipedia.org/wiki/Treni_della_felicit%C3%A0
- https://en.wikipedia.org/wiki/Maddalena_Cerasuolo
- https://www.argocatania.it/2018/05/14/treni-della-felicita-quando-nord-e-sud-erano-solidali/
- https://www.ilfattoquotidiano.it/2019/10/31/il-treno-dei-bambini-la-storia-dei-bimbi-poveri-del-sud-che-trovarono-un-futuro-grazie-a-un-affido-di-massa-temporaneo-delle-famiglie-del-nord/5514796/
- Referendum e via Medina (da risultati di ricerca): https://www.avvenire.it/agora/cultura/e-litalia-grido-repubblica_24232 , https://www.panorama.it/news/70-anni-della-repubblica-e-litalia-del-1946 , https://www.agi.it/cultura/news/2026-06-10/addio-italia-umberto-ii-37466039/ , https://www.startmag.it/?p=109908
- G7 Napoli 1994: https://g7.utoronto.ca/summit/1994naples/index.html ; https://www.iai.it/it/node/5116
- Legge 184/1983: https://www.normattiva.it/uri-res/N2Ls?urn:nir:stato:legge:1983-05-04;184 (riferimento noto, pagina non aperta)
