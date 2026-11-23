# -*- coding: utf-8 -*-
"""R029 "enigmi piu' semplici e capibili": testi riscritti di consegna, cosaFare, indizi (3 livelli progressivi: orienta, indica dove/come,
quasi la soluzione) e messaggi d'errore/blocco gentili, piu' qualche valore di gioco alleggerito.
Applicato da aggiungi_biglietti.py (ultimo passo della catena dei generatori): rilanciare quello dopo ogni generatore.
Chiavi uguali a quelle di aggiungi_biglietti.B. I campi presenti qui sostituiscono quelli del JSON generato.
"""

S = {
    # ================================================================ hub e basso (enigmi_mondo.json)
    "enigma_bucato": {
        "consegna": "Le porte del vicolo si assomigliano tutte. Sui panni stesi cerca le toppe uguali a quella della tua salopette da bambino e toccale.",
        "indizi": ["Guarda i panni stesi: su ognuno c'è una toppa cucita da qualcuno.",
                   "Cerca la toppa a quadri, uguale a quella della tua salopette di bambino. Ce ne sono tre.",
                   "Tocca i tre panni con la toppa a quadri: il filo finisce sopra la porta verde dello slargo."],
        "erroreMsg": "Questa toppa non è quella giusta: cerca la toppa a quadri, uguale a quella della tua salopette.",
    },
    "enigma_exvoto": {
        "consegna": "All'edicola, tra i lumini, ci sono ex-voto d'argento. In un vicolo povero le cose care si affidano a qualcuno: uno degli ex-voto nasconde qualcosa. Trovalo.",
        "indizi": ["Guarda gli ex-voto tra i lumini dell'edicola: uno non è solo un ringraziamento.",
                   "Cerca quello a forma di piede: l'argento non ha un lato solo, ha anche un retro.",
                   "Tocca l'ex-voto a forma di piede per girarlo, poi prendi la chiave che c'è dietro."],
        "erroreMsg": "Questo ex-voto non nasconde niente: cerca quello a forma di piede.",
    },
    "enigma_manifesti": {
        "consegna": "Sulla bacheca i manifesti sono incollati uno sull'altro. Staccali dall'alto, uno alla volta: quello in fondo è il più vecchio e dice di più.",
        "indizi": ["Non fermarti al primo manifesto: ce ne sono altri sotto.",
                   "Tocca i manifesti dall'alto, uno alla volta, per staccarli.",
                   "Stacca i due manifesti sopra, poi tocca il foglio che resta e leggilo."],
        "erroreMsg": "Quel manifesto è ancora coperto: stacca prima quelli che stanno sopra, partendo dall'alto.",
    },
    "enigma_foto": {
        "consegna": "Sulla credenza di Antonietta una fotografia pende storta, ma lei teneva tutto in ordine. Raddrizzala toccando la cornice e guarda che cosa c'è dietro.",
        "indizi": ["Qualcosa sulla credenza non è in ordine: che cosa pende storto?",
                   "È la cornice sopra la credenza: toccala per raddrizzarla.",
                   "Dopo averla raddrizzata, stacca il fondo della cornice e prendi il cartoncino con i tre disegni."],
        "erroreMsg": "Non è qui: tocca la cornice storta sopra la credenza e guarda dietro.",
    },
    "enigma_calendario": {
        "consegna": "Sul calendario della cucina Antonietta ha disegnato scarpette a matita, mese per mese. Conta le scarpette di settembre, ottobre, novembre e dicembre: sono le quattro cifre del codice.",
        "indizi": ["Il cartoncino dietro la foto ti dice che cosa contare: le scarpette.",
                   "Conta solo le scarpette di settembre, ottobre, novembre e dicembre, in quest'ordine: una cifra per mese.",
                   "Il codice è 4231: quattro, due, tre, una."],
        "erroreMsg": "Il codice non è quello: ricontale le scarpette di ogni mese, da settembre a dicembre.",
    },
    "enigma_cassetta": {
        "consegna": "La cassetta di latta ha una combinazione a quattro rotelle. Il numero l'hai già trovato sul calendario della cucina: giralo sulle rotelle.",
        "indizi": ["Serve un numero di quattro cifre: lo hai contato sul calendario della cucina.",
                   "Sono le scarpette contate mese per mese: settembre, ottobre, novembre, dicembre.",
                   "4 - 2 - 3 - 1."],
        "erroreMsg": "La cassetta non si apre: il codice è quello contato sul calendario, una cifra per mese.",
    },
    "enigma_baule": {
        "consegna": "Il baule ai piedi del letto si apre con la chiavetta. Sul fondo ci sono tre assi con un disegno ciascuna: premile nell'ordine dei disegni del cartoncino.",
        "indizi": ["Il baule ha un fondo di assi con dei disegni.",
                   "Gli stessi disegni sono sul cartoncino trovato dietro la fotografia: scarpa, croce, treno.",
                   "Premi le assi nell'ordine: scarpa, croce, treno. Poi prendi le scarpe."],
        "erroreMsg": "Le assi vanno premute nell'ordine dei disegni del cartoncino: riprova dal primo disegno.",
    },
    # ================================================================ Le scarpe (flashback_scarpe.json)
    "flashback_scarpe/enigma_passanti": {
        "consegna": "Sei seduto sul gradino e vedi solo scarpe. Per ogni paio tocca e scegli il mestiere di chi le porta (confermi a gruppi di tre).",
        "cosaFare": "Dai a ogni paio di scarpe il mestiere di chi le porta.",
        "indizi": ["Guarda ogni paio da vicino: suole, chiodi, fango e tacchi dicono che lavoro fa chi le porta.",
                   "Chiodate e bianche di calce: muratore. Zoccoli di legno: lavandaia. Scarponi alti da soldato: reduce.",
                   "Il resto: tacco alto: signora. Lucide e sottili: impiegato. Suole nuove di cuoio chiaro: calzolaio, che ripara le scarpe degli altri."],
        "erroreMsg": "Qualche abbinamento non torna, ma nessun problema: tocca di nuovo le scarpe dubbie e guarda suola e tacco.",
    },
    "flashback_scarpe/enigma_nascosto": {
        "consegna": "Il calzolaio ha lasciato cinque forme di legno sulla mensola. Cerca nella strada cinque scarpe perdute, raccoglile con E e provale sulla forma giusta.",
        "cosaFare": "Trova cinque scarpe nella strada e provale sulle forme giuste.",
        "indizi": ["Le scarpe stanno per terra o appoggiate ai muri della strada: guarda in basso, lungo i bordi e negli angoli.",
                   "Avvicinati a ogni oggetto sospetto e premi E: ti servono uno zoccolo, uno scarpone, un tacco, una scarpa di cartone e una scarpa piccola.",
                   "Con tutte e cinque in mano, provale sulle forme della mensola: la forma e l'etichetta dicono quale scarpa va dove."],
        "erroreMsg": "Ti mancano ancora delle scarpe: cercane altre vicino ai muri e negli angoli della strada.",
        "bloccatoMsg": "Prima capisci chi è il calzolaio: guarda le scarpe di chi passa dal gradino.",
    },
    "flashback_scarpe/enigma_quiz": {
        "consegna": "Nel cassetto del banco ci sono tre fogli: un avviso, il registro dei lavori e una pagina di quaderno. Leggi la domanda e tocca la riga che risponde.",
        "indizi": ["La risposta a ogni domanda è scritta in una riga dei tre fogli: leggi con calma.",
                   "Ogni risposta sta in un solo foglio: scegli quello che parla dell'argomento della domanda (avviso, registro o quaderno).",
                   "Cerca nella domanda le parole chiave (un lavoro, una data, un diritto) e cerca le stesse parole nelle righe: la riga che le contiene è la risposta."],
        "erroreMsg": "Non è la riga giusta: rileggi la domanda e cerca nei fogli la riga che parla proprio di quello.",
        "bloccatoMsg": "Il banco è chiuso a chiave: prima riporta le cinque scarpe sulle forme della mensola.",
    },
    "flashback_scarpe/enigma_ordina": {
        "consegna": "Sul muro del barbiere ci sono sei ritagli di giornale. Trascinali in ordine: in alto il fatto più antico, in basso il più recente.",
        "indizi": ["Cerca la data di ogni ritaglio e parti dal più vecchio: prima la guerra che finisce, poi il voto.",
                   "L'armistizio e la liberazione vengono prima del voto del 2 e 3 giugno 1946; dopo il voto si riunisce l'Assemblea Costituente.",
                   "Chiudi con la prima seduta della Costituente (25 giugno) e poi l'elezione del capo provvisorio dello Stato (28 giugno)."],
        "erroreMsg": "Non è ancora l'ordine giusto. Premi Verifica: ti dice quante carte sono già al posto giusto, poi sposta le altre.",
        "bloccatoMsg": "Il barbiere ha da fare. Prima capisci chi è il calzolaio guardando le scarpe dal gradino, poi torna ai ritagli.",
    },
    "flashback_scarpe/enigma_lucchetto": {
        "consegna": "Per aprire la porta del ricordo serve un codice a quattro cifre. Le cifre le hai già trovate lungo la strada: mettile nell'ordine in cui le hai raccolte.",
        "cosaFare": "Metti le quattro cifre raccolte, nell'ordine in cui le hai trovate.",
        "indizi": ["Le quattro cifre sono scritte nel quaderno, nell'ordine in cui le hai trovate: gesso, matita, margine del quaderno, ritaglio.",
                   "Messe in fila formano un anno, uno degli ultimi del Novecento: la data della Convenzione ONU sui diritti dell'infanzia.",
                   "1 - 9 - 8 - 9."],
        "erroreMsg": "Il lucchetto non si apre: apri il quaderno, rileggi le quattro cifre e mettile nell'ordine in cui le hai trovate.",
        "bloccatoMsg": "Il lucchetto ha quattro rotelle e ti mancano ancora delle cifre: controlla nel quaderno quali hai e risolvi gli enigmi che restano.",
    },
    # ================================================================ Stracci
    "flashback_stracci/enigma_giro": {
        "consegna": "Capa 'e fierro compra solo stracci buoni. Cerca dieci pezzi di stoffa nel vicolo, poi al banco provali uno a uno (controluce, filo, spessore) e dai a ciascuno la sua etichetta.",
        "cosaFare": "Raccogli i dieci stracci del vicolo e riconoscili al banco.",
        "indizi": ["I pezzi sono sparsi nel vicolo, tra cortili, macerie e terrazzini: raccoglili con E. Poi al banco falli passare uno per uno sotto le tre prove.",
                   "Guarda l'insegna dipinta della bottega: mostra che cosa compra Capa 'e fierro (lana, cotone, lino). Quello che non compra è scarto.",
                   "Basta la prova del filo: se si arriccia come una molla e' lana; se si spezza netto e' cotone; se e' lungo, rigido e con nodini e' lino; se si sfarina o non si tira (cera) e' scarto. In tutto: 3 lana, 3 cotone, 2 lino, 2 scarto."],
        "erroreMsg": "Qualche etichetta non è giusta, ma non preoccuparti: rifai le prove sui pezzi dubbi (controluce, filo, spessore) e riprova.",
    },
    "flashback_stracci/enigma_ceste": {
        "consegna": "Metti ogni straccio nella cesta con il suo disegno: tocca un pezzo nel sacco (in alto a destra), poi la cesta.",
        "indizi": ["Ogni cesta ha un disegno: un batuffolo (lana), un fiore di cotone, un fiore azzurro (lino) e una croce (scarto).",
                   "Ricorda le prove fatte al banco: il filo che si arriccia, il filo che si spezza, i nodini in controluce.",
                   "Lana: grigio ruvido, marrone peloso, blu scuro. Cotone: chiaro, rosso, azzurro. Lino: beige e verdolino. Il resto va nella cesta con la croce."],
        "erroreMsg": "Quel pezzo non va in quella cesta, ma non c'è fretta: guarda il disegno sulla cesta e ricorda le prove fatte al banco.",
    },
    "flashback_stracci/enigma_bilancia": {
        "consegna": "La bilancia pende anche quando non dovrebbe. Prima controllala con il peso campione da un chilo; poi pesa le tre ceste, una alla volta, mettendo pesi sul piatto di destra finché i piatti si equilibrano. Tocca un peso per metterlo, toccalo ancora per toglierlo.",
        "cosaFare": "Controlla la bilancia con il peso campione, poi pesa le ceste.",
        "indizi": ["Una bilancia giusta sta ferma con un chilo contro un chilo. La tua sta ferma?",
                   "Metti il peso campione da un chilo (a sinistra) contro il peso da un chilo (a destra): se pende, il difetto è nella bilancia, non nei pesi.",
                   "Guarda sotto il piatto di sinistra, dalla parte davanti: c'è un chiodo incastrato. Toglilo, poi pesa le tre ceste."],
        "erroreMsg": "Non è ancora in equilibrio: se pende a sinistra aggiungi un peso a destra, se pende a destra toglilo.",
    },
    "flashback_stracci/enigma_conto": {
        "consegna": "Capa 'e fierro ti deve pagare. Sul registro, per ogni cesta, moltiplica i chili pesati per il prezzo al chilo, poi somma le tre cifre. Metti sul banco le monete della somma e premi Paga (toccando una moneta la riprendi).",
        "indizi": ["Sul registro trovi per ogni cesta i chili pesati e il prezzo al chilo: lana, cotone e lino non costano uguale.",
                   "Fai un conto per cesta: chili per prezzo al chilo (1,3 per 40; 0,8 per 20; 0,6 per 25). Poi somma i tre risultati.",
                   "Le tre cifre sono 52, 16 e 15: in tutto 83 lire. Per esempio otto monete da 10, una da 2 e una da 1."],
        "erroreMsg": "Il conto non torna ancora: rifai chili per prezzo per ogni cesta, poi somma le tre cifre e controlla le monete sul banco.",
    },
    "flashback_stracci/enigma_mestieri": {
        "consegna": "Sul muro ci sono quattro attrezzi di mestieri che facevano i bambini nel 1946. Per ogni attrezzo scegli il mestiere (colonna di mezzo) e il diritto di oggi che mancava a quel bambino (colonna di destra), poi premi Conferma.",
        "indizi": ["Per ogni attrezzo pensa dove si usava: in strada, alla fontana, al mercato, per le case.",
                   "Poi chiediti che cosa mancava a quel bambino: la scuola, la sicurezza sul lavoro, un'età minima per lavorare, la protezione di chi è piccolo.",
                   "Lustrascarpe: scuola (34). Acquaiolo: lavori pericolosi (ONU 32). Venditore ambulante: età minima (37). Stracci: protezione dell'infanzia (31)."],
        "erroreMsg": "Qualche collegamento non torna: per ogni attrezzo chiediti che cosa mancava a quel bambino e riprova.",
    },
    # ================================================================ Il volantino
    "flashback_volantino/enigma_voci": {
        "consegna": "Dal lavatoio e dalle finestre del vicolo arrivano quattro voci sui treni dei bambini. Avvicinati a ognuna e premi E: finisce nel quaderno. Poi torna qui e rileggile: chi ha visto davvero i fatti?",
        "indizi": ["Le voci vengono dall'alto e dall'acqua: balconi, finestre accese e il lavatoio. Avvicinati a ognuna e premi E.",
                   "Balconi e lavatoio: uno sulla via grande, uno nel vicolo a ovest, una finestra sul largo in alto e il lavatoio del cortile a est. Nel quaderno guarda la parte «Chi lo dice?».",
                   "Le quattro posizioni: il balcone sulla via (lato destro, prima della traversa), il lavatoio del cortile a est, il balcone del vicolo a ovest, la finestra sul largo in alto. Nel quaderno: «lo sanno tutti» è il lavatoio; il parente di un parente è il balcone sulla via; il sogno è il balcone del vicolo; la fonte che cambia è la finestra sul largo."],
        "erroreMsg": "Non è quella la riga: nel quaderno, sotto ogni voce, leggi «Chi lo dice?» e scegli la riga che risponde.",
    },
    "flashback_volantino/enigma_bacheca": {
        "consegna": "Sulla bacheca ci sono le quattro voci (a sinistra) e cinque fogli del Comitato (a destra). Tocca una voce, poi il foglio che le risponde: lo spago li unisce. Alla fine gira il cartellino di ogni voce: vera o falsa? Toccando un foglio da solo, lo leggi.",
        "indizi": ["Ogni voce nasconde una paura: partire lontano, non tornare, il male, la morte. Cerca il foglio che risponde proprio a quella paura.",
                   "Guarda i timbri e le date: la cartolina viene dall'Emilia, il registro dice quando si torna, l'avviso parla del medico, la lettera di un letto e di una tavola. Un foglio non risponde a nessuna voce.",
                   "Russia: cartolina dall'Emilia. Non tornano più: registro delle partenze. Gli fanno del male: avviso delle visite mediche. Forni: lettera della famiglia ospitante. L'elenco del corredo resta senza spago. Tutte e quattro le voci sono false."],
        "erroreMsg": "Un cartellino non è giusto: rileggi il foglio collegato alla voce e chiediti se la conferma o la smentisce.",
    },
    "flashback_volantino/enigma_ciclostile": {
        "consegna": "Prepara il volantino in tre mosse. 1) Scegli tre frasi chiare e vere e premi «Scrivi la matrice». 2) Tocca la matrice sul tamburo, poi l'inchiostro. 3) Gira la manovella tre volte con calma, un giro al secondo circa: se vai troppo in fretta, il foglio sbava.",
        "cosaFare": "Scegli tre frasi vere, prepara la matrice e gira la manovella.",
        "indizi": ["Un volantino deve far capire, non spaventare: niente accuse, niente esagerazioni, niente «lo dicono tutti».",
                   "Scegli le frasi che si possono controllare: una visita del medico, delle lettere da leggere in sede, un registro con le date.",
                   "Le tre frasi giuste: la visita del medico, le lettere delle famiglie in sede, il registro con il ritorno. Poi tocca la matrice, l'inchiostro e gira la manovella tre volte, un giro al secondo."],
        "erroreMsg": "Manca ancora un passo: l'ordine è matrice, inchiostro, manovella. Se il foglio sbava, gira più piano.",
    },
    "flashback_volantino/enigma_ruoli": {
        "consegna": "Sul tavolo ci sono cinque cose e dietro cinque cartoncini con le persone che lavorano per i treni. Scegli una cosa dall'elenco in alto a destra e tocca il cartoncino della persona a cui serve.",
        "indizi": ["Ogni oggetto appartiene a un mestiere: chi veste, chi cura, chi scrive i nomi, chi accoglie, chi guida il treno.",
                   "Le frasi sui cartoncini aiutano: aghi, trentatré, nomi, letto, convoglio.",
                   "Fascia della Croce Rossa: la signora Nunzia (vestiti). Stetoscopio: il dottor Russo. Registro: Maddalena. Lettera dal Nord: la famiglia Malagoli. Berretto: Gennaro, il ferroviere."],
        "erroreMsg": "Quella persona non usa questo oggetto, ma niente fretta: rileggi il cartoncino e prova con un'altra.",
    },
    "flashback_volantino/enigma_timbro": {
        "consegna": "Il retro del volantino ha una linea del tempo con quattro caselle. Leggi i fogli sulla scrivania, gira i nastri del datario (giorno, mese, anno: tocca in alto per andare avanti, in basso per tornare indietro) e premi il timbro. Le date vanno dalla più vecchia; l'ultima è il giorno in cui le donne di tutta Italia hanno votato.",
        "indizi": ["Leggi i fogli sulla scrivania: ognuno porta una data. Le quattro date vanno in ordine, dalla più vecchia.",
                   "Prima i treni del Nord, poi il voto: i treni e il congresso sono d'inverno, tra la fine del 1945 e l'inizio del 1946; il voto delle donne è d'estate.",
                   "In ordine: 16-12-1945, 29-12-1945, 19-01-1946 e infine 02-06-1946."],
        "erroreMsg": "Non è la data giusta per questa casella: rileggi i fogli sulla scrivania e correggi i nastri del datario.",
    },
    # ================================================================ Il cappotto
    "flashback_cappotto/enigma_cartellini": {
        "consegna": "Sul tavolo del Comitato c'è il registro: per ogni numero, età, quartiere e segni. Tocca un cartellino, poi il bambino a cui appartiene. Il tuo lo appendi davanti al vetro della biglietteria, dove vedi il tuo riflesso.",
        "indizi": ["Leggi il registro: per ogni numero ci sono età, quartiere e segni (che cosa tiene in mano, se è scalzo).",
                   "Guarda l'età (chi è il più piccolo, chi il più alto) e che cosa tiene in mano ognuno: fischietto, bambola, trottola. E chi è scalzo?",
                   "112 al tuo riflesso (scalzo, capelli castani); 113 a Mariuccia (trecce e bambola); 114 a Tommasino (il più alto, berretto e trottola); 115 a Pasqualino (il più piccolo, fischietto)."],
        "erroreMsg": "Un cartellino non è al collo giusto: rileggi il registro e confronta età e segni di ogni bambino.",
    },
    "flashback_cappotto/enigma_cappotti": {
        "consegna": "Sul tavolo della Croce Rossa ci sono cappotti usati. Tocca un cappotto, poi il bambino a cui va bene: guarda spalle e maniche. Il tuo ha il numero 112 sul talloncino.",
        "indizi": ["Misura a occhio: un cappotto troppo grande arriva alle ginocchia, uno troppo piccolo non si chiude.",
                   "Dal più piccolo al più grande: Pasqualino, Mariuccia, tu, Tommasino. Due cappotti sono della tua misura: il talloncino ha un numero.",
                   "Pasqualino: il cappottino con due bottoni. Mariuccia: il marrone. Tommasino: il verde lungo. Tu: il blu con la toppa e il numero 112. Il cappotto da uomo non è per nessuno di voi."],
        "erroreMsg": "Questo cappotto non gli va: guarda se è troppo lungo, troppo corto o troppo stretto e prova con un altro.",
    },
    "flashback_cappotto/enigma_mela": {
        "consegna": "Sulla banchina Antonietta ti aspetta con qualcosa in mano. Tocca la mela, poi tocca la valigia di cartone aperta sulla cassa.",
        "indizi": ["Antonietta ha qualcosa in mano: toccalo.",
                   "Quello che ti dà va messo in valigia, per il viaggio.",
                   "Tocca la mela, poi la valigia aperta sulla cassa."],
        "erroreMsg": "Prima prendi la mela da Antonietta, poi tocca la valigia aperta sulla cassa.",
    },
    "flashback_cappotto/enigma_finestrino": {
        "consegna": "Tra Napoli e Modena mancano tre città. Guarda i cartelli che passano nel finestrino: quando passa una città, premi «Appunta» (o tocca il cartello). Se ne perdi una, il controllore te la ripete. Alla fine tocca Modena sulla carta.",
        "indizi": ["Leggi i cartelli, non guardare il paesaggio: non tutti i cartelli sono città. Casello, galleria, deposito e scalo non lo sono.",
                   "Ti servono le tre città in mezzo tra Napoli e Modena, nell'ordine in cui passano: pensa alla strada da Napoli verso il Nord.",
                   "In ordine: Roma, Firenze, Bologna. Poi tocca Modena sulla carta."],
        "erroreMsg": "Quello non è il nome di una città, ma un cartello delle ferrovie: aspetta il prossimo cartello con un nome di città.",
    },
    "flashback_cappotto/enigma_paura": {
        "consegna": "Tre bambini non dormono: hanno paura, ognuno la sua. Tocca un bambino per ascoltarlo, poi scegli come rispondere (pulsanti o tasti 1, 2, 3). Se una risposta non aiuta non è un errore: provane un'altra.",
        "indizi": ["Ognuno ha una paura diversa: ascoltala bene prima di rispondere.",
                   "Il volantino risponde a una sola di quelle paure: quella delle dicerie. Le altre chiedono una carezza o una risata.",
                   "Peppino (Russia, non tornare): il volantino. Rosetta (la mamma che si scorda): il fazzoletto ricamato, consolala. Gaetano (il freddo): la battuta della pastiera."],
        "erroreMsg": "Questa risposta non lo calma, ma puoi riprovare: ascolta di nuovo la sua paura e scegli un altro modo.",
    },
    # ================================================================ Il violino
    "flashback_violino/enigma_forno": {
        "consegna": "Rosa deve infornare il pane. Il forno non ha termometro: si getta un pizzico di farina sulla pietra. Bianca: forno freddo, aggiungi legna. Nocciola: giusto. Nera: troppo caldo, apri la serranda. Poi inforna e sforna quando la clessidra è vuota.",
        "cosaFare": "Leggi il forno con la farina, scaldalo, inforna e sforna a tempo.",
        "indizi": ["Il forno si legge con la farina: tocca il sacchetto e guarda di che colore diventa il pizzico sulla pietra.",
                   "Bianca = freddo, aggiungi legna. Nera = troppo caldo, apri la serranda e aspetta. Nocciola = giusto: inforna subito.",
                   "Servono circa tre ciocchi di legna, finché la farina diventa nocciola; serranda chiusa, pala nel forno; quando la sabbia è scesa tutta tocca il pane."],
        "erroreMsg": "Il forno non è ancora giusto: tocca la farina e guarda il colore, poi aggiungi legna o apri la serranda.",
    },
    "flashback_violino/enigma_attrezzi": {
        "consegna": "Alcide tiene un attrezzo dietro la schiena e lo fa suonare. Premi Ascolta, guarda l'onda sulla lavagnetta e tocca sul banco l'attrezzo che disegna la stessa onda. Anche gli attrezzi, se li tocchi, suonano e disegnano la loro onda.",
        "indizi": ["Premi Ascolta e guarda la forma dell'onda. Poi tocca gli attrezzi sul banco uno per uno: ognuno disegna la sua.",
                   "Cerca l'onda uguale: liscia e lunghissima = diapason; scatti in fila = chiave; gobba bassa e corta = feltri; picco secco = martelletto; onda che trema e ronza = corda.",
                   "L'ordine di Alcide: martelletto, diapason, chiave, corda, feltri."],
        "erroreMsg": "Non è quell'attrezzo: premi di nuovo Ascolta e confronta l'onda con quella che disegna ogni attrezzo.",
    },
    "flashback_violino/enigma_pianoforte": {
        "consegna": "Tre corde del pianoforte sono stonate. Scegli una corda e premi Ascolta: finché è stonata senti «ua-ua-ua» e la lancetta trema. Gira la chiave a mezzi scatti: se il «ua-ua» rallenta vai bene, se accelera gira dall'altra parte. Quando sparisce e la lancetta sta ferma, la corda è accordata.",
        "cosaFare": "Accorda le tre corde finché il «ua-ua» sparisce.",
        "indizi": ["Scegli una corda, premi Ascolta e guarda la lancetta sul telaio: più trema, più la corda è stonata.",
                   "Dopo ogni mezzo scatto: se il battimento rallenta continua, se accelera torna indietro e gira dall'altra parte.",
                   "Poche mosse: il La tre mezzi scatti verso sinistra, il Re due verso destra, il Mi quattro verso sinistra."],
        "erroreMsg": "Non è ancora accordata: gira la chiave piano, a mezzi scatti, e ascolta finché il «ua-ua» sparisce.",
    },
    "flashback_violino/enigma_impronte": {
        "consegna": "Il violino è sparito e fuori, nella neve, ci sono impronte fresche. Tocca uno scarpone dei ragazzi, poi un'impronta: ti dice se combaciano (numero, chiodi, tacco). Quando sai di chi sono le orme che escono dal laboratorio, seguile: a ogni bivio tocca la traccia giusta.",
        "indizi": ["Tocca uno scarpone e poi le orme davanti alla porta: il gioco ti dice che cosa combacia e che cosa no.",
                   "Tre cose da confrontare: la lunghezza (il numero), i chiodi sulla suola e il tacco. Una sola impronta ha il tacco rattoppato.",
                   "Le orme della porta sono di Rivo (tacco rattoppato). Poi: primo bivio a sinistra, secondo a destra, terzo a sinistra, fino alla legnaia della scuola."],
        "erroreMsg": "Non combacia: confronta di nuovo numero, chiodi e tacco con un altro scarpone.",
    },
    "flashback_violino/enigma_melodia": {
        "consegna": "In classe Alcide ti fa sentire una frase sul violino. Premi Ascolta: le note si accendono con i loro colori sulla tastiera e sullo spartito. Poi ripeti la frase toccando i segni colorati nello stesso ordine.",
        "indizi": ["Premi Ascolta tutte le volte che vuoi e guarda l'ordine dei colori che si accendono.",
                   "Se sbagli, Alcide suona più lento e puoi riprovare. Prima frase: quattro note; seconda: sei.",
                   "Prima frase: rosso, verde, blu, verde. Seconda: blu, viola, blu, verde, giallo, rosso."],
        "erroreMsg": "Non era quella nota: riascolta (Alcide la rifà più lenta) e ripeti i colori nell'ordine.",
    },
    # ================================================================ Il finale
    "flashback_finale/enigma_basso94": {
        "consegna": "Il basso è vuoto e freddo. Accanto alla porta ci sono le ultime cose di tua madre: il telefono, il calendario e la scatola di latta. Toccale una per una.",
        "indizi": ["Tocca il telefono, il calendario e la scatola di latta, uno alla volta.",
                   "Il calendario ha un giorno cerchiato; nella latta c'è qualcosa con un numero.",
                   "Telefono, calendario, cartellino: tocca tutti e tre."],
    },
    "flashback_finale/enigma_ricevuta": {
        "consegna": "Sul tavolo ci sono i cinque frammenti e un pezzo già al suo posto: sono un documento strappato. Tocca un pezzo, poi il punto del sottomano dove va. I bordi devono combaciare e le parole avere senso.",
        "indizi": ["I bordi dei pezzi combaciano: in alto il nome del Monte, in basso che cosa è stato impegnato.",
                   "Si legge da sinistra a destra: MONTE DEI - PEGNI - N. 4712, poi VIOLINO CON - CUSTODIA - RISCATTATO.",
                   "Il timbro RISCATTATO sta in basso a destra; il numero in alto a destra."],
        "erroreMsg": "Questo pezzo non va qui: guarda i bordi e prova a leggere la frase che si forma.",
    },
    "flashback_finale/enigma_custodia": {
        "consegna": "Sotto il letto c'è una custodia da violino, chiusa da un lucchetto a quattro rotelle. Il numero giusto è su un documento che hai ricomposto.",
        "indizi": ["Il numero non è sulla custodia: è su un documento che hai ricomposto sul tavolo.",
                   "Cerca sulla polizza ricomposta il numero di quattro cifre accanto a «N.», in alto a destra.",
                   "4712."],
        "erroreMsg": "Il lucchetto non si apre: il numero è quello della polizza ricomposta sul tavolo, accanto a «N.».",
    },
    "flashback_finale/enigma_carmine": {
        "consegna": "Qualcuno bussa alla porta sul vicolo: è Carmine, il figlio di tuo fratello Agostino. Scegli che cosa fare con lui: ogni scelta va bene.",
        "indizi": ["Non ci sono risposte sbagliate: scegli quella che senti tua.",
                   "Puoi dargli il cartellino, accordare il violino o suonare.",
                   "Scegline una qualunque delle tre: va bene comunque."],
    },
}

_POSTI = {
    "scarpe": ("È la soglia di casa: che cosa si lascia fuori, o si toglie, entrando?", "Qui vanno le scarpe."),
    "stracci": ("È una cesta del cortile: che cosa ci si metteva, da vendere a chi compra roba usata?", "Qui vanno gli stracci."),
    "volantino": ("È la credenza della cucina: che cosa si teneva lì, sotto il piatto buono, per non perderlo?", "Qui va il volantino."),
    "cappotto": ("È un chiodo dietro la porta: che cosa si appende lì, quando si torna a casa col freddo?", "Qui va il cappotto."),
    "spartito": ("È un cuscino: che cosa si nasconde lì sotto, per tenerlo al sicuro?", "Qui va lo spartito."),
}
for _k, (_h1, _h3) in _POSTI.items():
    S["flashback_finale/enigma_posto_" + _k] = {
        "consegna": "Uno dei cinque posti della casa segnati nella latta. Apri il vassoio e posa qui il ricordo che abitava questo posto.",
        "indizi": [_h1,
                   "Il cappotto si appende, le scarpe si lasciano entrando, gli stracci vanno nella cesta, il volantino sulla credenza, lo spartito sotto il cuscino.",
                   _h3],
        "erroreMsg": "Qui non c'era: riprendi il ricordo e provane un altro. Ogni posto ne ha uno solo.",
        "bloccatoMsg": "Prima guarda le ultime cose di tua madre nell'angolo accanto alla porta: la latta dice dove vanno i ricordi.",
    }


# Valori di gioco alleggeriti (meno fatica, stesso apprendimento). Chiave -> funzione che modifica l'enigma.
def _ciclostile(e):
    e["giri"], e["ritmo"] = 3, 0.4


def _forno(e):
    e["forno"].update({"giusto0": 200, "giusto1": 290, "cottura": 8, "presto": 4.0, "tardi": 8.0})


def _pianoforte(e):
    for c, s in zip(e["corde"], (1.5, -1.0, 2.0)):
        c["scarto"] = s


V = {
    "flashback_volantino/enigma_ciclostile": _ciclostile,
    "flashback_violino/enigma_forno": _forno,
    "flashback_violino/enigma_pianoforte": _pianoforte,
}
