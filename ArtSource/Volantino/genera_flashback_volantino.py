# -*- coding: utf-8 -*-
"""R013: genera Assets/_Game/Resources/Data/flashback_volantino.json (i cinque enigmi del ricordo "Il volantino del treno").
I testi del gioco stanno nel JSON generato; questo script e' solo la sorgente riproducibile. Uso: python genera_flashback_volantino.py
Coordinate = quelle di SceneBuilderVolantino.cs (devono coincidere).
Fatti storici: solo quelli confermati in Docs/VERIFICHE_STORICHE.md (vedi il Rapporto di R013); le frasi del volantino si giudicano
per chiarezza e verificabilita', mai per la parte politica (decisione D2 dello schema).
"""
import json, os

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data", "flashback_volantino.json")


def P(id, forma="cubo", mat="legno", col="", x=0, y=0, z=0, sx=1, sy=1, sz=1, **kw):
    d = {"id": id, "forma": forma, "materiale": mat}
    if col: d["colore"] = col
    for k, v in (("x", x), ("y", y), ("z", z)):
        if v: d[k] = v
    for k, v in (("sx", sx), ("sy", sy), ("sz", sz)):
        if v != 1: d[k] = v
    d.update(kw)
    return d


def T(id, testo, x, y, z, sx, sy, dim, col="#3b2a1a", font="", **kw):
    d = P(id, "testo", "legno", col, x, y, z, sx, sy, testo=testo, dimensione=dim, **kw)
    if font: d["font"] = font
    return d


def quiz(testo, opz, corretta, spieg):
    return {"testo": testo, "opzioni": opz, "corretta": corretta, "spiegazione": spieg}


def tavolo(lx, lz, h, col="#5a3d26", gamba="#4a3220"):
    """Piano (spessore 6 cm, superficie a quota h) e quattro gambe."""
    hx, hz = lx / 2 - 0.06, lz / 2 - 0.06
    return [P("piano", "cubo", "legno", col, y=h - 0.03, sx=lx, sy=0.06, sz=lz)] + [
        P("gamba_" + n, "cubo", "legno", gamba, x=gx, y=(h - 0.06) / 2, z=gz, sx=0.07, sy=h - 0.06, sz=0.07)
        for n, gx, gz in (("a", -hx, -hz), ("b", hx, -hz), ("c", -hx, hz), ("d", hx, hz))]


# ---------------------------------------------------------------- posizioni nel mondo (= SceneBuilderVolantino.cs)
Q = 0.9                        # quota del largo in alto, della soglia e della sede
POS_V1 = (-3.6, 0.0, -25.5)    # quaderno delle voci, inizio del vicolo, fronte verso est
POS_V2 = (-7.55, Q, 45.0)      # bacheca, parete ovest della sala, fronte verso est
POS_V3 = (6.5, Q, 44.5)        # ciclostile, parete est della sala, fronte verso ovest
POS_V4 = (0.0, Q, 45.2)        # tavolo dell'accoglienza, centro della sala, fronte verso sud (la porta)
POS_V5 = (0.0, Q, 56.0)        # scrivania con il datario, stanza dei registri, fronte verso sud

# le quattro voci: punto dove fermarsi ad ascoltare (x,y,z) e punto da cui viene la voce (fx,fy,fz)
VOCI = [
    ("voce_russia", "Una voce dal balcone", "Ascolta: la voce dal balcone sulla via",
     "«Quei bambini li mandano in Russia, e chi li vede più!» «E tu come lo sai?» «Me l'ha detto il marito di mia cognata, che l'ha sentito al porto.»",
     (1.8, 0.0, -9.5), (3.3, 4.4, -9.5)),
    ("voce_tornano", "Una voce dal lavatoio", "Ascolta: le donne al lavatoio",
     "«Partono, partono... e non tornano più.» «Chi l'ha detto?» «Lo sanno tutti, comare. Tutti.»",
     (16.6, 0.0, -16.0), (18.2, 1.2, -17.0)),
    ("voce_male", "Una voce dal balcone del vicolo", "Ascolta: la voce dal balcone del vicolo",
     "«Gli fanno del male, a quelle creature.» «E chi te l'ha detto?» «L'ho sognato stanotte. Era un segno.»",
     (-11.0, 0.0, 18.6), (-11.0, 4.4, 20.3)),
    ("voce_forni", "Una voce dalla finestra sul largo", "Ascolta: la voce dalla finestra sul largo",
     "«Li mettono nei forni, ve lo dico io.» «Ma chi lo dice?» «Me l'hanno giurato al mercato... no, era sul tram. Comunque è vero.»",
     (-6.6, Q, 30.5), (-8.3, 5.2, 30.5)),
]


# ---------------------------------------------------------------- V1 le voci del vicolo
def v1():
    oggetti = []
    for id, nome, prompt, testo, (x, y, z), (fx, fy, fz) in VOCI:
        oggetti.append({"id": id, "nome": nome, "descrizione": testo, "x": x, "y": y, "z": z, "modello": "voce", "prompt": prompt,
                        "fx": fx, "fy": fy, "fz": fz})
    righe = [
        "Dal balcone sulla via: «Li mandano in Russia.» Chi lo dice? Il marito di una cognata, che l'ha sentito al porto.",
        "Al lavatoio del cortile: «Non tornano più.» Chi lo dice? «Lo sanno tutti.»",
        "Dal balcone del vicolo: «Gli fanno del male.» Chi lo dice? Una donna che l'ha sognato.",
        "Dalla finestra sul largo: «Li mettono nei forni.» Chi lo dice? Prima il mercato, poi il tram: la fonte cambia.",
    ]
    parti = [
        P("muretto", "cubo", "pietra", "#6d655a", y=0.4, sx=1.1, sy=0.8, sz=0.5),
        P("muretto_cimasa", "cubo", "pietra", "#5d564c", y=0.82, sx=1.16, sy=0.05, sz=0.56),
        P("quaderno", "cubo", "carta", "#cfc3a0", x=-0.1, y=0.855, z=-0.02, ry=8, sx=0.34, sy=0.018, sz=0.24),
        P("quaderno_costa", "cubo", "stoffa", "#3d4756", x=-0.1, y=0.856, z=-0.02, ry=8, sx=0.02, sy=0.02, sz=0.24),
        T("quaderno_t", "le voci", -0.06, 0.866, -0.02, 0.14, 0.06, 0.02, "#2a2018", "mano", rx=90, ry=8),
        P("matita", "cilindro", "legno", "#a0522d", x=0.15, y=0.852, z=-0.06, rz=90, ry=30, sx=0.012, sy=0.08, sz=0.012),
        P("lanterna", "cilindro", "metallo", "#3a3530", x=0.42, y=0.92, z=0.1, sx=0.1, sy=0.08, sz=0.1),
        P("lanterna_vetro", "cilindro", "vetro", "#e8c27a", x=0.42, y=1.02, z=0.1, sx=0.08, sy=0.05, sz=0.08),
    ]
    return {
        "id": "enigma_voci", "tipo": "nascosto", "meccanismo": "documenti", "titolo": "Le voci del vicolo",
        "consegna": "Sera, pioggia fine. Dalle finestre e dal lavatoio escono voci sui treni dei bambini. Avvicinati e ascolta: ogni voce finisce nel quaderno. Quando le avrai sentite tutte e quattro, torna qui e rileggile con calma: chi le ha viste con i propri occhi?",
        "temaCivico": "Libertà di parola e responsabilità di chi parla: oggi l'art. 21 della Costituzione garantisce a tutti la libertà di esprimere il proprio pensiero. Proprio per questo conta chiedersi da dove viene una notizia prima di ripeterla.",
        "indizi": ["Le voci vengono dall'alto e dall'acqua: balconi e finestre accese, e il lavatoio. Nel quaderno, per ogni voce chiediti: chi l'ha vista con i propri occhi?",
                   "Balconi e lavatoio: uno sulla via grande, uno nel vicolo a ovest, una finestra sul largo in alto e il lavatoio del cortile a est. Nel quaderno guarda la parte «Chi lo dice?».",
                   "Le quattro posizioni: il balcone sulla via (lato destro, prima della traversa), il lavatoio del cortile a est, il balcone del vicolo a ovest, la finestra sul largo in alto. Nel quaderno: «lo sanno tutti» è il lavatoio; il parente di un parente è il balcone sulla via; il sogno è il balcone del vicolo; la fonte che cambia è la finestra sul largo."],
        "spiegazione": "Quattro voci, e nessuna vista con i propri occhi: un parente di un parente, «tutti», un sogno, una fonte che cambia. Così nasce e si diffonde una diceria: passa di bocca in bocca e a ogni passaggio sembra più sicura, anche se nessuno l'ha controllata.",
        "erroreMsg": "Non è quella la riga: rileggi la parte «Chi lo dice?» di ogni voce, senza fretta.",
        "scheda": "Nel 1945-46 attorno ai treni dei bambini circolavano dicerie spaventose: che i bambini sarebbero finiti in Russia, o peggio. La diffidenza era forte soprattutto perché l'iniziativa era promossa dai comunisti. Erano voci senza fonte: qui le nominiamo per smontarle, non per crederci.",
        "appunto": "Nel quaderno: quattro voci, nessuna vista con i propri occhi.",
        "x": POS_V1[0], "y": POS_V1[1], "z": POS_V1[2], "yaw": -90, "scala": 1, "distanza": 1.6, "inclina": 20,
        "prompt": "il quaderno delle voci",
        "produce": ["voci_raccolte"],
        "oggetti": oggetti,
        "completoMsg": "Hai sentito tutte e quattro le voci. Torna al quaderno, all'inizio del vicolo, e rileggi chi le ha dette.",
        "documenti": [{"id": "quaderno", "titolo": "Il quaderno delle voci", "righe": righe}],
        "parti": parti,
        "domande": [
            dict(quiz("Quale voce non dice da chi viene, ma solo che «lo sanno tutti»?", ["Quella del lavatoio", "Quella dal balcone sulla via", "Quella del sogno", "Quella dalla finestra sul largo"], 0,
                      "«Lo sanno tutti» non è una fonte: nessuno, in particolare, l'ha visto."), documento="quaderno", riga=1),
            dict(quiz("Quale voce arriva da un parente di un parente, cioè di terza mano?", ["Quella dal balcone sulla via (la Russia)", "Quella del lavatoio", "Quella del sogno", "Quella dei forni"], 0,
                      "Il marito della cognata l'ha sentito al porto da qualcun altro: tre passaggi, e nessuno ha visto niente."), documento="quaderno", riga=0),
            dict(quiz("Quale voce si basa su un sogno e non su un fatto?", ["Quella dal balcone del vicolo (gli fanno del male)", "Quella del lavatoio", "Quella della Russia", "Quella dei forni"], 0,
                      "Un sogno racconta una paura, non un fatto."), documento="quaderno", riga=2),
            dict(quiz("Quale voce cambia fonte mentre la racconta?", ["Quella dalla finestra sul largo (i forni)", "Quella del lavatoio", "Quella della Russia", "Quella del sogno"], 0,
                      "Prima il mercato, poi il tram: se la fonte cambia, non c'è una fonte."), documento="quaderno", riga=3),
        ],
    }


# ---------------------------------------------------------------- V2 la bacheca delle prove
DICERIE = [  # testo breve sul cartoncino, voce completa (msg)
    ("«Li mandano in Russia»", "La voce dal balcone sulla via: «Li mandano in Russia, e chi li vede più.»"),
    ("«Non tornano più»", "La voce del lavatoio: «Partono, e non tornano più.»"),
    ("«Gli fanno del male»", "La voce dal balcone del vicolo: «Gli fanno del male, a quelle creature.»"),
    ("«Li mettono nei forni»", "La voce dalla finestra sul largo: «Li mettono nei forni.»"),
]
DOCUMENTI = [  # id, titolo sul cartoncino, testo da leggere (documenti inventati per il gioco)
    ("doc_registro", "Registro delle partenze",
     "Registro del Comitato, la pagina di una bambina di otto anni: nome, indirizzo, la famiglia che la ospita a Modena. In fondo, a matita: «ritorno previsto alla fine dell'anno di scuola».", "#d6c9a3"),
    ("doc_corredo", "Elenco del corredo",
     "Elenco delle cose da preparare per il viaggio: un cambio di biancheria, un fazzoletto, un pettine. «Chi non le ha, le riceve dal Comitato.»", "#d9cfb2"),
    ("doc_cartolina", "Cartolina dall'Emilia",
     "Una cartolina di un bambino romano partito l'inverno scorso: «Cara mamma, qui c'è la neve. La signora mi fa il pane e vado a scuola con suo figlio. Torno a primavera.» Il timbro postale è di Reggio nell'Emilia.", "#cdb98d"),
    ("doc_visite", "Avviso delle visite mediche",
     "Avviso del Comitato: «Prima della partenza ogni bambino sarà visitato dal medico, vaccinato, lavato e vestito di nuovo. Le madri possono assistere.»", "#d3c7a6"),
    ("doc_lettera", "Lettera di una famiglia ospitante",
     "Lettera di una famiglia di contadini di Modena: «Abbiamo preparato un letto con le coperte di lana e un posto a tavola. A scuola lo accompagnerà nostro figlio. Scriveremo ogni mese.»", "#dccfae"),
]
COLLEGAMENTI = [
    {"voce": "Li mandano in Russia", "a": "doc_cartolina", "verdetto": "falsa", "nota": "La cartolina arriva dall'Emilia, in Italia."},
    {"voce": "Non tornano più", "a": "doc_registro", "verdetto": "falsa", "nota": "Il registro scrive quando si torna."},
    {"voce": "Gli fanno del male", "a": "doc_visite", "verdetto": "falsa", "nota": "Prima di partire, il medico."},
    {"voce": "Li mettono nei forni", "a": "doc_lettera", "verdetto": "falsa", "nota": "Un letto, un posto a tavola, la scuola."},
]


def v2():
    parti = [
        P("sughero", "cubo", "legno", "#9a7a4e", y=1.55, z=0.03, sx=2.8, sy=1.5, sz=0.04),
        P("cornice_alta", "cubo", "legno", "#4a3220", y=2.32, z=0.02, sx=2.9, sy=0.06, sz=0.06),
        P("cornice_bassa", "cubo", "legno", "#4a3220", y=0.78, z=0.02, sx=2.9, sy=0.06, sz=0.06),
        P("cornice_sx", "cubo", "legno", "#4a3220", x=-1.43, y=1.55, z=0.02, sx=0.06, sy=1.6, sz=0.06),
        P("cornice_dx", "cubo", "legno", "#4a3220", x=1.43, y=1.55, z=0.02, sx=0.06, sy=1.6, sz=0.06),
        T("titolo_voci", "LE VOCI", -0.85, 2.2, 0.0, 0.8, 0.1, 0.045, "#2a2018", "mano"),
        T("titolo_prove", "I FOGLI", 0.75, 2.2, 0.0, 0.8, 0.1, 0.045, "#2a2018", "mano"),
        P("gomitolo", "sfera", "stoffa", "#c9b98a", x=1.25, y=0.86, z=-0.06, sx=0.1, sy=0.09, sz=0.1),
    ]
    for i, (breve, voce) in enumerate(DICERIE):
        y = round(1.95 - i * 0.3, 2)
        parti.append(P("diceria_%d" % i, "cubo", "carta", "#e0d4b4", x=-0.75, y=y, z=0.0, sx=0.85, sy=0.22, sz=0.01, testo=breve, dimensione=0.032, font="mano",
                       clic=True, azione="diceria", valore=i, msg=voce))
        parti.append(P("verdetto_%d" % i, "cubo", "carta", "#bfae86", x=-1.3, y=y, z=-0.004, sx=0.18, sy=0.14, sz=0.01, testo="?", dimensione=0.03,
                       clic=True, azione="verdetto", valore=i))
        parti.append(P("spillo_d%d" % i, "sfera", "metallo", "#8a2a20", x=-0.36, y=round(y + 0.06, 2), z=-0.012, sx=0.018, sy=0.018, sz=0.018))
    for j, (id, titolo, testo, col) in enumerate(DOCUMENTI):
        y = round(2.02 - j * 0.29, 2)
        parti.append(P(id, "cubo", "carta", col, x=0.78, y=y, z=0.0, ry=(-2 + 4 * (j % 2)), sx=1.0, sy=0.22, sz=0.01, testo=titolo, dimensione=0.03,
                       clic=True, azione="documento", valore=j, msg=testo))
        parti.append(P("spillo_%s" % id, "sfera", "metallo", "#8a2a20", x=0.32, y=round(y + 0.06, 2), z=-0.012, sx=0.018, sy=0.018, sz=0.018))
    return {
        "id": "enigma_bacheca", "tipo": "quiz", "meccanismo": "bacheca", "titolo": "La bacheca delle prove",
        "consegna": "Sulla bacheca della sede ci sono le quattro voci del vicolo (a sinistra) e cinque fogli del Comitato (a destra). Tocca una voce, poi il foglio che le risponde: lo spago li collega. Quando due collegamenti sono giusti, lo spago diventa rosso e resta fissato. Alla fine gira il cartellino accanto a ogni voce: vera o falsa? Toccando un foglio da solo, lo leggi.",
        "temaCivico": "Informazione e fonti: oggi l'art. 21 della Costituzione (in vigore dal 1948) tutela la libertà di parola e di stampa. Verificare una notizia vuol dire cercare una prova che chiunque possa controllare.",
        "indizi": ["Ogni voce ha dentro una paura: andare lontano, non tornare, il male, la morte. Cerca il foglio che risponde proprio a quella paura.",
                   "Guarda i timbri e le date: la cartolina viene dall'Emilia, il registro dice quando si torna, l'avviso parla del medico, la lettera di un letto e di una tavola. Un foglio non risponde a nessuna voce.",
                   "Russia: cartolina dall'Emilia. Non tornano più: registro delle partenze. Gli fanno del male: avviso delle visite mediche. Forni: lettera della famiglia ospitante. L'elenco del corredo resta senza spago. Tutte e quattro le voci sono false."],
        "spiegazione": "Quattro voci, quattro fogli che le smentiscono, e un foglio che non c'entra. Maddalena lo ripete alle madri: «Non vi chiedo di credere a me. Leggete, e venite a vedere.» Una voce si smonta con una prova che chiunque può controllare.",
        "erroreMsg": "Non tutti i cartellini sono giusti: rileggi il foglio collegato a ogni voce e riprova.",
        "scheda": "La cartolina, il registro, l'avviso e la lettera sono inventati per il gioco [GIOCO]. Vero è che i bambini, prima di partire, venivano visitati, lavati e vestiti, e che li accoglievano famiglie del Centro-Nord, spesso contadine e operaie.",
        "appunto": "Sulla bacheca: quattro voci false, quattro fogli che le smentiscono.",
        "x": POS_V2[0], "y": POS_V2[1], "z": POS_V2[2], "yaw": -90, "scala": 1, "distanza": 1.8, "inclina": 2,
        "prompt": "la bacheca della sede",
        "richiede": ["voci_raccolte"], "bloccatoMsg": "La bacheca è piena di fogli, ma prima ti servono le voci del vicolo: ascoltale e scrivile nel quaderno.",
        "produce": ["parole_giuste"],
        "collegamenti": COLLEGAMENTI,
        "strumenti": [
            "Ora tocca il foglio che risponde a questa voce: lo spago li collegherà.",
            "Lo spago è teso. Quando due collegamenti saranno giusti, diventeranno rossi.",
            "Due collegamenti sono giusti: lo spago diventa rosso e resta fissato.",
            "Questa voce è già collegata al foglio giusto: lo spago rosso non si tocca più.",
            "Tutti gli spaghi sono rossi. Ora gira il cartellino accanto a ogni voce: secondo i fogli, è vera o falsa?",
            "?", "VERA", "FALSA",
            "Gli spaghi sono giusti, ma {0} cartellini non vanno: rileggi che cosa dice il foglio collegato e riprova.",
            "Prima tocca una voce (a sinistra), poi il foglio che le risponde (a destra). Toccando un foglio da solo, lo leggi.",
        ],
        "parti": parti,
        "domande": [
            quiz("Quale foglio risponde alla voce «Li mandano in Russia»?", ["La cartolina dall'Emilia", "L'elenco del corredo", "Il registro delle partenze", "L'avviso delle visite"], 0, "La cartolina ha il timbro di Reggio nell'Emilia."),
            quiz("Quale foglio risponde alla voce «Non tornano più»?", ["Il registro con la data di ritorno", "La lettera della famiglia", "L'elenco del corredo", "La cartolina"], 0, "Il registro scrive quando si torna: alla fine della scuola."),
            quiz("Quale foglio risponde alla voce «Gli fanno del male»?", ["L'avviso delle visite mediche", "L'elenco del corredo", "La cartolina", "Nessuno"], 0, "Prima di partire, il medico visita e vaccina i bambini."),
            quiz("Dopo aver letto i fogli, le quattro voci sono...", ["Tutte false", "Tutte vere", "Due vere e due false", "Non si può dire"], 0, "Ogni voce è smentita da un foglio che si può controllare."),
        ],
    }


# ---------------------------------------------------------------- V3 il ciclostile
FRASI = [
    ("Prima di partire, ogni bambino viene visitato dal medico.", True, ""),
    ("Chi non manda i figli è una cattiva madre.", False,
     "Questa frase non informa: giudica e fa vergognare. Un volantino deve far capire, non spaventare né accusare. Scegli un'altra frase."),
    ("Le lettere delle famiglie che ospitano sono in sede: venite a leggerle.", True, ""),
    ("Al Nord i bambini mangiano carne e dolci tre volte al giorno!", False,
     "È un'esagerazione che nessuno può controllare: promettere troppo è un altro modo di non dire il vero. Scegli una frase che si può verificare."),
    ("Il registro scrive per ogni bambino quando torna: alla fine della scuola.", True, ""),
    ("Lo dicono tutti: è la cosa migliore del mondo.", False,
     "«Lo dicono tutti» è proprio come le voci del vicolo: chi sono questi tutti? Una frase vaga non convince chi ha paura. Scegli una frase con una prova."),
]


def v3():
    TOP = 0.93
    parti = tavolo(1.7, 0.85, 0.93)
    parti += [
        P("basamento", "cubo", "metallo", "#3a3632", y=1.0, sx=0.72, sy=0.14, sz=0.46),
        P("fianco_sx", "cubo", "metallo", "#2d2a27", x=-0.33, y=1.17, sx=0.04, sy=0.22, sz=0.3),
        P("fianco_dx", "cubo", "metallo", "#2d2a27", x=0.33, y=1.17, sx=0.04, sy=0.22, sz=0.3),
        P("tamburo", "cilindro", "metallo", "#5a544c", y=1.2, rz=90, sx=0.26, sy=0.3, sz=0.26),
        P("matrice_tamburo", "cilindro", "carta", "#e3d9bd", y=1.2, rz=90, sx=0.266, sy=0.24, sz=0.266, nascosta=True),
        P("inchiostro_steso", "cilindro", "metallo", "#17140f", y=1.2, rz=90, sx=0.27, sy=0.2, sz=0.27, nascosta=True),
        # la manovella: un gruppo vuoto (ruota attorno al suo asse) con asse, braccio e pomo
        P("manovella", "vuoto", "metallo", x=0.36, y=1.2, rz=90, sx=0.14, sy=0.3, sz=0.3, clic=True, azione="manovella",
          errore="La manovella gira a vuoto: prima la matrice sul tamburo e l'inchiostro."),
        P("manovella_asse", "cilindro", "metallo", "#2d2a27", y=-0.05, sx=0.03, sy=0.06, sz=0.03, genitore="manovella"),
        P("manovella_braccio", "cubo", "metallo", "#2d2a27", y=-0.11, z=-0.06, sx=0.03, sy=0.02, sz=0.15, genitore="manovella"),
        P("manovella_pomo", "cilindro", "legno", "#5a3d26", y=-0.15, z=-0.12, sx=0.04, sy=0.04, sz=0.04, genitore="manovella"),
        # la matrice sul tavolo (prima bianca, poi scritta)
        P("matrice", "cubo", "carta", "#e3d9bd", x=-0.58, y=TOP + 0.003, z=-0.1, ry=-6, sx=0.3, sy=0.004, sz=0.38, clic=True, azione="fase", valore=1, evento="matrice",
          msg="Stendi la matrice cerata sul tamburo e la fissi con le mollette.", errore="La matrice è ancora bianca: prima scegli le tre frasi da scriverci sopra.",
          dopo="matrice_tamburo,-matrice,-g:scritta"),
        T("matrice_scritta", "TRENI PER I BAMBINI\n\nvisita del medico\nlettere delle famiglie\nritorno a fine scuola", -0.58, TOP + 0.007, -0.1, 0.28, 0.34, 0.017, "#1d1a18", "", rx=90, ry=-6,
          nascosta=True, gruppo="scritta"),
        # l'inchiostro
        P("inchiostro", "cilindro", "metallo", "#1d1a18", x=-0.25, y=TOP + 0.04, z=0.25, sx=0.1, sy=0.04, sz=0.1, clic=True, azione="fase", valore=2, evento="inchiostro",
          msg="Versi l'inchiostro nel tamburo: l'odore è forte e le dita diventano nere. Ora la manovella, con un ritmo regolare.",
          errore="Prima stendi la matrice sul tamburo, poi l'inchiostro.", dopo="inchiostro_steso"),
        P("inchiostro_etichetta", "cubo", "carta", "#c9b98a", x=-0.25, y=TOP + 0.04, z=0.199, sx=0.06, sy=0.03, sz=0.003),
        # la risma bianca e il vassoio dei fogli stampati
        P("risma", "cubo", "carta", "#e6dfca", x=0.0, y=TOP + 0.02, z=0.3, sx=0.3, sy=0.04, sz=0.22),
        P("vassoio", "cubo", "legno", "#4a3623", x=0.05, y=TOP + 0.006, z=-0.3, sx=0.36, sy=0.012, sz=0.26),
        P("macchia", "cubo", "carta", "#2a2522", x=0.05, y=TOP + 0.016, z=-0.3, ry=12, sx=0.26, sy=0.004, sz=0.18, nascosta=True),
    ]
    for k in range(1, 6):
        parti.append(P("foglio_%d" % k, "cubo", "carta", "#ece4cc", x=round(0.05 + 0.01 * (k % 2), 3), y=round(TOP + 0.012 + 0.004 * k, 3), z=-0.3, ry=(k * 7) % 11 - 5,
                       sx=0.28, sy=0.003, sz=0.2, nascosta=True, gruppo="fogli"))
    parti.append(T("foglio_titolo", "TRENI PER I BAMBINI", 0.05, TOP + 0.04, -0.3, 0.26, 0.05, 0.018, "#1d1a18", "", rx=90, nascosta=True, quando="risolto"))
    return {
        "id": "enigma_ciclostile", "tipo": "quiz", "meccanismo": "ciclostile", "titolo": "Il ciclostile",
        "consegna": "Il ciclostile della sede: un tamburo, una matrice di carta cerata, l'inchiostro, una manovella. Prima scegli tre frasi da scrivere sulla matrice (un volantino deve far capire, non spaventare) e premi «Scrivi la matrice». Poi stendi la matrice sul tamburo, versa l'inchiostro e gira la manovella con un ritmo regolare, cinque volte: troppo in fretta, il foglio sbava.",
        "temaCivico": "Libertà di stampa e responsabilità di chi informa: oggi l'art. 21 della Costituzione dice che la stampa non può essere soggetta ad autorizzazioni o censure. Proprio per questo chi scrive deve dire cose chiare e controllabili.",
        "indizi": ["Un volantino deve far capire, non spaventare: niente accuse, niente esagerazioni, niente «lo dicono tutti».",
                   "Scegli le frasi che chiunque può controllare: una visita, delle lettere da leggere, un registro con le date. Poi l'ordine è: matrice, inchiostro, manovella.",
                   "Le tre frasi giuste: la visita del medico, le lettere delle famiglie in sede, il registro con il ritorno. Poi tocca la matrice, l'inchiostro e la manovella; gira con calma, circa un giro al secondo, cinque volte."],
        "spiegazione": "Cinque fogli puliti, con tre frasi che chiunque può controllare. Maddalena li conta e sorride: «Domani li attacchiamo in tutto il quartiere.»",
        "erroreMsg": "Qualcosa non va ancora: rileggi le frasi e ricorda l'ordine, matrice, inchiostro, manovella.",
        "scheda": "Il ciclostile era la tipografia di chi aveva pochi soldi: lo usavano associazioni, sindacati, parrocchie, partiti e scuole per stampare avvisi e volantini. Qui le frasi si giudicano solo per chiarezza e verificabilità, mai per le idee di chi le scrive.",
        "appunto": "Cinque volantini stampati: visita del medico, lettere delle famiglie, ritorno a fine scuola.",
        "x": POS_V3[0], "y": POS_V3[1], "z": POS_V3[2], "yaw": 90, "scala": 1, "distanza": 2.0, "inclina": 24,
        "prompt": "il ciclostile",
        "richiede": ["parole_giuste"], "bloccatoMsg": "Il ciclostile è pronto, ma non sai ancora che cosa scrivere: prima metti in ordine le voci e le prove sulla bacheca.",
        # i gettoni dei ruoli servono al tavolo dell'accoglienza (V4): sono le cose che Maddalena affida ad Amerigo insieme al pacco dei volantini
        "produce": ["pacco_volantini", "ruolo_fascia", "ruolo_stetoscopio", "ruolo_registro", "ruolo_lettera", "ruolo_berretto"],
        "frasi": [{"testo": t, "giusta": g, "motivo": m} for t, g, m in FRASI],
        "giri": 5, "ritmo": 0.6,
        "strumenti": [
            "Scrivi la matrice",
            "Sulla matrice ci stanno tre frasi: scegline tre, non di più e non di meno.",
            "La matrice è scritta. Ora stendila sul tamburo, poi l'inchiostro e infine la manovella.",
            "Non ancora: ogni cosa a suo tempo. Matrice, inchiostro, manovella.",
            "Troppo veloce! Il foglio sbava e l'inchiostro fa una macchia. Ricomincia con un giro regolare, senza fretta.",
            "Questo l'hai già fatto.",
            "Un foglio stampato: {0} su {1}.",
        ],
        "parti": parti,
        "domande": [
            quiz("Quale di queste frasi va bene su un volantino?", ["Prima di partire, ogni bambino viene visitato dal medico.", "Chi non manda i figli è una cattiva madre.", "Lo dicono tutti: è la cosa migliore del mondo.", "Al Nord si mangiano dolci tre volte al giorno!"], 0,
                 "Si può controllare: è un fatto, non un giudizio né un'esagerazione."),
            quiz("Perché «Lo dicono tutti» non va bene?", ["Perché è vago come una diceria: chi sono questi tutti?", "Perché è troppo lunga", "Perché è scritta male", "Va benissimo"], 0,
                 "Una frase vaga non convince chi ha paura."),
            quiz("In che ordine si usa il ciclostile?", ["Matrice, inchiostro, manovella", "Manovella, matrice, inchiostro", "Inchiostro, manovella, matrice", "L'ordine non conta"], 0,
                 "Prima la matrice sul tamburo, poi l'inchiostro, poi si gira."),
            quiz("Che cosa succede se giri la manovella troppo in fretta?", ["Il foglio sbava e si ricomincia", "Si stampa il doppio", "Non succede niente", "Si rompe il tamburo"], 0,
                 "Un ritmo regolare dà fogli puliti."),
        ],
    }


# ---------------------------------------------------------------- V4 chi fa che cosa
RUOLI = [
    # gettone, nome, descrizione, oggetto sul tavolo, persona (cartoncino), evento, messaggio giusto, errore
    ("ruolo_fascia", "La fascia della Croce Rossa", "Una fascia bianca da braccio con la croce rossa.", "obj_fascia",
     "Signora Nunzia, volontaria\n«Ho le mani piene di aghi e di bottoni.»", "chi_vestiti",
     "Giusto: la signora Nunzia si mette la fascia. Con le volontarie prepara i vestiti e i cappotti per il viaggio.",
     "La signora Nunzia scuote la testa: «Io cucio e piego vestiti. Questo serve a qualcun altro.» Riprova."),
    ("ruolo_stetoscopio", "Lo stetoscopio", "Lo strumento del medico per ascoltare il respiro.", "obj_stetoscopio",
     "Il dottor Russo\n«Dite trentatré.»", "chi_visita",
     "Giusto: il dottor Russo ascolta il respiro di ogni bambino prima del viaggio.",
     "Il dottore sorride: «Con questo non visito nessuno.» Pensa a che cosa serve per ascoltare il respiro."),
    ("ruolo_registro", "Il registro delle iscrizioni", "Un registro con nomi, età e indirizzi.", "obj_registro",
     "Maddalena\n«Nome, età, indirizzo: scrivete chiaro.»", "chi_iscrizione",
     "Giusto: Maddalena apre il registro. Ogni bambino iscritto ha un nome, un indirizzo e una famiglia che lo aspetta.",
     "Maddalena: «Non è questo che mi serve per scrivere i nomi.» Riprova."),
    ("ruolo_lettera", "La lettera dal Nord", "Una lettera con il timbro di Modena.", "obj_lettera",
     "La famiglia Malagoli, Modena\n«Il letto è pronto, la tavola anche.»", "chi_famiglie",
     "Giusto: è la lettera delle famiglie che ospitano. Scrivono per dire che cosa hanno preparato.",
     "Le famiglie dell'Emilia aspettano qualcosa di scritto, non questo. Riprova."),
    ("ruolo_berretto", "Il berretto da ferroviere", "Un berretto con la visiera e lo stemma delle ferrovie.", "obj_berretto",
     "Gennaro, ferroviere\n«Il convoglio non aspetta nessuno.»", "chi_viaggio",
     "Giusto: Gennaro si mette il berretto. Conosce binari e orari del treno dei bambini.",
     "Gennaro ride: «E io che ci faccio con questo, sul treno?» Riprova."),
]


def oggetto_ruolo(id, x):
    """Gli oggetti sul davanti del tavolo (solo da guardare: in mano li scegli dall'elenco in alto a destra)."""
    if id == "obj_fascia":
        return [P(id, "cubo", "stoffa", "#e6e0d0", x=x, y=0.94, z=-0.22, ry=10, sx=0.22, sy=0.012, sz=0.08),
                P(id + "_c1", "cubo", "stoffa", "#a3261c", x=x, y=0.948, z=-0.22, ry=10, sx=0.05, sy=0.004, sz=0.015),
                P(id + "_c2", "cubo", "stoffa", "#a3261c", x=x, y=0.948, z=-0.22, ry=10, sx=0.015, sy=0.004, sz=0.05)]
    if id == "obj_stetoscopio":
        return [P(id, "cilindro", "metallo", "#9a9a92", x=x, y=0.945, z=-0.22, sx=0.05, sy=0.012, sz=0.05),
                P(id + "_tubo", "cubo", "cuoio", "#1d1a18", x=x + 0.08, y=0.942, z=-0.2, ry=30, sx=0.16, sy=0.012, sz=0.012),
                P(id + "_tubo2", "cubo", "cuoio", "#1d1a18", x=x + 0.07, y=0.942, z=-0.25, ry=-25, sx=0.14, sy=0.012, sz=0.012)]
    if id == "obj_registro":
        return [P(id, "cubo", "cuoio", "#4a2a22", x=x, y=0.955, z=-0.22, ry=-6, sx=0.24, sy=0.04, sz=0.17),
                P(id + "_pagine", "cubo", "carta", "#d8cba6", x=x + 0.006, y=0.955, z=-0.22, ry=-6, sx=0.23, sy=0.032, sz=0.16)]
    if id == "obj_lettera":
        return [P(id, "cubo", "carta", "#d6c9a3", x=x, y=0.937, z=-0.22, ry=14, sx=0.2, sy=0.006, sz=0.12),
                P(id + "_francobollo", "cubo", "carta", "#7a3a2a", x=x + 0.07, y=0.941, z=-0.25, ry=14, sx=0.03, sy=0.002, sz=0.035)]
    return [P(id, "cilindro", "stoffa", "#2b2d33", x=x, y=0.97, z=-0.22, sx=0.18, sy=0.035, sz=0.18),
            P(id + "_visiera", "cubo", "cuoio", "#1d1a18", x=x, y=0.94, z=-0.32, sx=0.14, sy=0.008, sz=0.07),
            P(id + "_stemma", "cubo", "ottone", "#b59a3f", x=x, y=0.975, z=-0.31, sx=0.03, sy=0.025, sz=0.004)]


def v4():
    parti = tavolo(2.6, 0.9, 0.93, "#5a4632", "#46362a")
    ordine_oggetti = ["obj_lettera", "obj_fascia", "obj_berretto", "obj_stetoscopio", "obj_registro"]  # mescolati rispetto ai cartoncini
    for k, oid in enumerate(ordine_oggetti):
        parti += oggetto_ruolo(oid, round(-1.0 + k * 0.5, 2))
    for k, (tok, nome, desc, oid, persona, ev, ok, err) in enumerate(RUOLI):
        x = round(-1.0 + k * 0.5, 2)
        parti.append(P("leggio_%d" % k, "cubo", "legno", "#3a2a1a", x=x, y=0.99, z=0.24, rx=-12, sx=0.44, sy=0.12, sz=0.03))
        parti.append(P(ev, "cubo", "carta", "#e0d4b4", x=x, y=1.13, z=0.22, rx=-12, sx=0.44, sy=0.3, sz=0.01, testo=persona, dimensione=0.022, font="mano",
                       clic=True, azione="usa", richiede=tok, evento=ev, msg=ok, errore=err, dopo="-" + oid + ",-" + oid + "_c1,-" + oid + "_c2,-" + oid + "_tubo,-" + oid + "_tubo2,-" + oid + "_pagine,-" + oid + "_francobollo,-" + oid + "_visiera,-" + oid + "_stemma,segno_" + ev))
        parti.append(P("segno_" + ev, "cubo", "stoffa", "#a3261c", x=round(x + 0.17, 2), y=1.24, z=0.205, rx=-12, sx=0.05, sy=0.05, sz=0.006, nascosta=True))
    oggetti = [{"id": tok, "nome": nome, "descrizione": desc} for tok, nome, desc, *_ in RUOLI]
    return {
        "id": "enigma_ruoli", "tipo": "quiz", "meccanismo": "oggetto", "titolo": "Chi fa che cosa",
        "consegna": "Sul tavolo dell'accoglienza ci sono cinque cose, e dietro cinque cartoncini con le persone che lavorano per i treni. Scegli una cosa dall'elenco in alto a destra e tocca il cartoncino della persona a cui serve. Sul retro del volantino ci va proprio questo: chi fa che cosa.",
        "temaCivico": "Associazioni e partecipazione: oggi la Costituzione riconosce la libertà di associarsi (art. 18) e di riunirsi in partiti (art. 49); la solidarietà è un dovere di tutti (art. 2).",
        "indizi": ["Ogni oggetto è un mestiere: guarda bene che cosa serve a ciascuno.",
                   "Chi veste, chi cura, chi scrive i nomi, chi accoglie, chi guida il treno. Le frasi sui cartoncini aiutano: aghi, trentatré, nomi, letto, convoglio.",
                   "Fascia della Croce Rossa: la signora Nunzia (vestiti). Stetoscopio: il dottor Russo. Registro: Maddalena. Lettera dal Nord: la famiglia Malagoli. Berretto: Gennaro, il ferroviere."],
        "spiegazione": "Cinque persone, cinque compiti: vestire, curare, iscrivere, accogliere, accompagnare. Il retro del volantino lo dice chiaro, così ogni madre sa a chi chiedere.",
        "erroreMsg": "Non è la persona giusta per questa cosa, ma niente fretta: rileggi il cartoncino e riprova.",
        "scheda": "Le persone di questo tavolo sono inventate. I treni dei bambini furono promossi dall'UDI (Unione Donne Italiane) e dal PCI, con l'aiuto di Croce Rossa, sindacati e associazioni partigiane; a Napoli li organizzava il Comitato per la salvezza dei bambini di Napoli. Fra le promotrici c'era Teresa Noce, dirigente del PCI e dell'UDI, che propose di ospitare i bambini nelle campagne del Centro-Nord. I vestiti per il viaggio venivano in gran parte dalla Croce Rossa.",
        "appunto": "Sul retro del volantino: chi veste, chi cura, chi iscrive, chi accoglie, chi accompagna.",
        "x": POS_V4[0], "y": POS_V4[1], "z": POS_V4[2], "yaw": 0, "scala": 1, "distanza": 2.2, "inclina": 22,
        "prompt": "il tavolo dell'accoglienza",
        "richiede": ["pacco_volantini"], "bloccatoMsg": "Il tavolo dell'accoglienza aspetta i volantini: prima stampali con il ciclostile.",
        "produce": ["retro_stampato"],
        "soluzione": [r[5] for r in RUOLI],
        "oggetti": oggetti,
        "parti": parti,
        "domande": [
            quiz("A chi serve lo stetoscopio?", ["Al dottor Russo, per la visita", "A Gennaro, il ferroviere", "Alla famiglia di Modena", "A nessuno"], 0, "Il medico ascolta il respiro dei bambini prima del viaggio."),
            quiz("Chi preparava in gran parte i vestiti per il viaggio?", ["La Croce Rossa e le volontarie", "I ferrovieri", "I bambini stessi", "Nessuno: partivano come erano"], 0, "I vestiti venivano soprattutto dalla Croce Rossa."),
            quiz("Chi accoglieva i bambini al Nord?", ["Famiglie del Centro-Nord, spesso contadine e operaie", "Solo famiglie ricche", "Gli orfanotrofi", "Nessuno"], 0, "Spesso famiglie non ricche, organizzate da UDI, PCI e amministrazioni locali."),
        ],
    }


# ---------------------------------------------------------------- V5 il timbro della data
MESI = ["GEN", "FEB", "MAR", "APR", "MAG", "GIU", "LUG", "AGO", "SET", "OTT", "NOV", "DIC"]
DATE = [  # solo date confermate in Docs/VERIFICHE_STORICHE.md
    (16, 12, 1945, "16 dicembre 1945, il primo treno di bambini parte da Milano", "16 DIC 1945"),
    (29, 12, 1945, "29 dicembre 1945, a Roma si apre il V Congresso del PCI", "29 DIC 1945"),
    (19, 1, 1946, "19 gennaio 1946, partono i treni da Roma per l'Emilia", "19 GEN 1946"),
    (2, 6, 1946, "2 giugno 1946, votano anche le donne", "2 GIU 1946"),
]


def v5():
    parti = tavolo(1.7, 0.8, 0.8, "#4a3623", "#3a2a1a")
    TOP = 0.8
    parti += [
        P("datario_corpo", "cubo", "metallo", "#3a3632", y=TOP + 0.09, z=0.05, sx=0.56, sy=0.18, sz=0.14),
        P("timbro", "vuoto", "metallo", y=TOP + 0.24, z=0.05, sx=0.2, sy=0.16, sz=0.16, clic=True, azione="timbra"),
        P("timbro_asta", "cilindro", "metallo", "#2d2a27", y=0.0, sx=0.03, sy=0.05, sz=0.03, genitore="timbro"),
        P("timbro_pomo", "sfera", "legno", "#5a3d26", y=0.07, sx=0.09, sy=0.08, sz=0.09, genitore="timbro"),
        P("nastro_g", "cubo", "metallo", "#c9bfa6", x=-0.17, y=TOP + 0.09, z=-0.03, sx=0.12, sy=0.1, sz=0.03, clic=True, azione="nastro", valore=0),
        P("nastro_m", "cubo", "metallo", "#c9bfa6", x=0.0, y=TOP + 0.09, z=-0.03, sx=0.15, sy=0.1, sz=0.03, clic=True, azione="nastro", valore=1),
        P("nastro_a", "cubo", "metallo", "#c9bfa6", x=0.19, y=TOP + 0.09, z=-0.03, sx=0.17, sy=0.1, sz=0.03, clic=True, azione="nastro", valore=2),
        T("nastro_su", "+", 0.0, TOP + 0.165, -0.05, 0.5, 0.04, 0.03, "#e6dcc0"),
        T("nastro_giu", "-", 0.0, TOP + 0.015, -0.05, 0.5, 0.04, 0.03, "#e6dcc0"),
        P("tampone", "cubo", "stoffa", "#5a1a16", x=0.5, y=TOP + 0.012, z=0.12, sx=0.16, sy=0.02, sz=0.1),
        # la linea del tempo sul retro del volantino, davanti al datario
        P("linea", "cubo", "carta", "#e3d9bd", y=TOP + 0.002, z=-0.24, sx=0.94, sy=0.004, sz=0.2),
        T("linea_t", "LINEA DEL TEMPO", 0.0, TOP + 0.006, -0.17, 0.6, 0.04, 0.02, "#1d1a18", "", rx=90),
        P("linea_riga", "cubo", "carta", "#1d1a18", y=TOP + 0.005, z=-0.26, sx=0.86, sy=0.002, sz=0.004),
    ]
    xs = [-0.33, -0.11, 0.11, 0.33]
    for k, (g, m, a, et, corta) in enumerate(DATE):
        parti.append(T("casella_%d" % (k + 1), str(k + 1), xs[k], TOP + 0.006, -0.3, 0.05, 0.04, 0.02, "#6a5a45", "mano", rx=90))
        parti.append(T("impronta_%d" % (k + 1), corta, xs[k], TOP + 0.007, -0.24, 0.2, 0.05, 0.022, "#7a1f1a", "", rx=90, nascosta=True))
    # i fogli sulla scrivania: si leggono (le date stanno qui)
    fogli = [
        ("ritaglio_treno", -0.66, 0.12, 8, "#d3c7a6", "Un ritaglio di giornale: «16 dicembre 1945. Da Milano parte il primo treno di bambini: li aspettano le famiglie di Reggio Emilia.»"),
        ("manifesto_voto", -0.62, -0.2, -10, "#cfc0a0", "Un manifesto piegato: «2 giugno 1946: si vota per scegliere fra Repubblica e Monarchia e per l'Assemblea Costituente. Per la prima volta in tutta Italia votano anche le donne.»"),
        ("avviso_roma", 0.64, -0.2, 6, "#d8ccaa", "Un avviso del Comitato: «Dal 19 gennaio 1946 partono da Roma i treni dei bambini per le famiglie dell'Emilia.»"),
        ("appunto_congresso", 0.66, 0.14, -12, "#d9cfb2", "Un appunto a matita: «Il V Congresso del partito si è aperto a Roma il 29 dicembre 1945 ed è finito il 6 gennaio 1946: si è deciso di portare l'accoglienza anche ai bambini del Sud.»"),
    ]
    for id, x, z, ry, col, msg in fogli:
        parti.append(P(id, "cubo", "carta", col, x=x, y=TOP + 0.004, z=z, ry=ry, sx=0.22, sy=0.006, sz=0.28, clic=True, azione="leggi", msg=msg))
    return {
        "id": "enigma_timbro", "tipo": "quiz", "meccanismo": "datario", "titolo": "Il timbro della data",
        "consegna": "Prima di uscire, i volantini vanno timbrati. Sul retro c'è una piccola linea del tempo con quattro caselle: leggi i fogli sulla scrivania, gira i nastri del datario (giorno, mese, anno: tocca in alto per andare avanti, in basso per tornare indietro) e premi il timbro. Le date vanno in ordine, dalla più vecchia; l'ultima è il giorno in cui anche le madri hanno votato.",
        "temaCivico": "Voto alle donne e uguaglianza: oggi l'art. 48 della Costituzione dice che sono elettori tutti i cittadini, uomini e donne, maggiorenni; l'art. 3 che tutti sono uguali davanti alla legge, senza distinzione di sesso.",
        "indizi": ["Prima i treni del Nord, poi il voto.",
                   "Il voto delle donne in tutta Italia è d'estate; i treni e il congresso sono d'inverno, fra la fine del 1945 e l'inizio del 1946.",
                   "In ordine: 16-12-1945, 29-12-1945, 19-01-1946 e infine 02-06-1946."],
        "spiegazione": "Quattro timbri in fila: il primo treno da Milano, il congresso di Roma, i treni da Roma, e il 2 giugno 1946. Il volantino è d'autunno, ma ricorda quel giorno d'estate in cui anche le madri del vicolo hanno votato [GIOCO].",
        "erroreMsg": "Non è la data giusta per la prossima casella: rileggi i fogli e gira i nastri.",
        "scheda": "Il 2 e 3 giugno 1946 gli italiani scelsero fra Repubblica e Monarchia ed elessero l'Assemblea Costituente: per la prima volta votarono le donne in tutta Italia, con la matita copiativa. A Napoli la monarchia prese quasi l'80% dei voti. La Costituzione scritta da quell'Assemblea è entrata in vigore il 1° gennaio 1948.",
        "appunto": "Il volantino è timbrato: 16-12-1945, 29-12-1945, 19-01-1946, 02-06-1946.",
        "x": POS_V5[0], "y": POS_V5[1], "z": POS_V5[2], "yaw": 0, "scala": 1, "distanza": 1.7, "inclina": 34,
        "prompt": "la scrivania con il datario",
        "richiede": ["retro_stampato"], "bloccatoMsg": "Il datario aspetta i volantini con il retro già scritto: prima il tavolo dell'accoglienza.",
        "produce": ["volantino_timbrato"],
        "date": [{"giorno": g, "mese": m, "anno": a, "etichetta": et, "parte": "impronta_%d" % (k + 1)} for k, (g, m, a, et, corta) in enumerate(DATE)],
        "mesi": MESI, "anni": ["1945", "1946", "1947"],
        "codice": "02061946",
        "strumenti": [
            "Timbra",
            "Timbrato: {0}. Avanti con la data che viene dopo.",
            "Questa data c'è nei fogli, ma non è la prossima: la linea del tempo va dalla più vecchia alla più nuova.",
            "Su questa data i fogli della scrivania non dicono niente: rileggili e gira i nastri.",
            "Questa data l'hai già timbrata: passa alla successiva.",
        ],
        "parti": parti,
        "domande": [
            quiz("Quale di questi fatti viene prima?", ["Il primo treno da Milano (16 dicembre 1945)", "I treni da Roma (19 gennaio 1946)", "Il voto del 2 giugno 1946", "Il V Congresso (dal 29 dicembre 1945)"], 0, "Il primo treno parte il 16 dicembre 1945."),
            quiz("Quando partono i treni da Roma per l'Emilia?", ["Dal 19 gennaio 1946", "Il 16 dicembre 1945", "Il 2 giugno 1946", "Nel 1948"], 0, "Dal 19 gennaio 1946."),
            quiz("In che giorno votano per la prima volta le donne in tutta Italia?", ["Il 2 giugno 1946 (e il 3)", "Il 16 dicembre 1945", "Il 1° gennaio 1948", "Il 19 gennaio 1946"], 0, "Il 2 e 3 giugno 1946, referendum e Assemblea Costituente."),
        ],
    }


def main():
    fb = {
        "id": "flashback_volantino", "oggetto": "volantino", "titolo": "Il volantino del treno", "scena": "50_FB_Volantino",
        "introduzione": "Chiudi gli occhi e senti la pioggia fine sulle spalle. È sera, Antonietta ti tiene per mano e cammina in fretta. Dalle finestre e dal lavatoio escono voci, una più spaventosa dell'altra. In fondo al vicolo, oltre il largo, c'è la sede del Comitato, con la luce accesa. Prima di arrivarci, ascolta: che cosa dice la gente dei treni dei bambini?",
        "conclusione": "Il ricordo si spegne come la lampada della sede. Un foglio ciclostilato, tre frasi che chiunque poteva controllare, una croce al posto di una firma. Le voci correvano più veloci dei treni; ma una prova, prima o poi, le raggiungeva.",
        "enigmi": [v1(), v2(), v3(), v4(), v5()],
    }
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(fb, f, ensure_ascii=False, indent=2)
    print("scritto", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
