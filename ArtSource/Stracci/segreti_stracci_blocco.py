
# ============================================================================= STRACCI (R012)
STANZE.append({"scena": STR, "nome": "Il ricordo degli stracci", "completo": [
    "ris:flashback_stracci/enigma_giro", "ris:flashback_stracci/enigma_ceste", "ris:flashback_stracci/enigma_bilancia",
    "ris:flashback_stracci/enigma_conto", "ris:flashback_stracci/enigma_mestieri"]})
BQ_STR = ("Nel dopoguerra molti bambini delle città italiane aiutavano le famiglie con piccoli lavori, come raccogliere materiali di recupero (stracci, ferri vecchi, carta) "
          "da rivendere. Oggi la legge italiana fissa l'età minima per lavorare a 16 anni (legge n. 977 del 1967 e successive modifiche) e la Costituzione (art. 37) tutela il lavoro dei minori.")


def tappo():
    return [parte("disco", "cilindro", "metallo", "#8c8a80", 0, 0.004, 0, 0.034, 0.004, 0.034),
            parte("bordo", "cilindro", "metallo", "#6a685f", 0, 0.002, 0, 0.038, 0.002, 0.038),
            parte("cuore", "cilindro", "metallo", "#a3402f", 0, 0.0085, 0, 0.022, 0.0008, 0.022)]


def bottone_militare():
    return [parte("disco", "cilindro", "ottone", "#b59a3f", 0, 0.004, 0, 0.03, 0.004, 0.03),
            parte("stella", "cilindro", "ottone", "#8a7430", 0, 0.0085, 0, 0.018, 0.0012, 0.018),
            parte("anello", "cilindro", "ottone", "#c9ac4a", 0, 0.0075, 0, 0.028, 0.0006, 0.028)]


def carta_gioco():
    return [parte("cartone", "cubo", "carta", "#d8cba6", 0, 0.002, 0, 0.065, 0.003, 0.10, ry=14),
            parte("seme", "sfera", "stoffa", "#9a3a2e", 0, 0.0045, 0, 0.026, 0.004, 0.026, ry=14)]


def biglia_rossa():
    return [parte("vetro", "sfera", "vetro", "#a85a3a", 0, 0.016, 0, 0.032, 0.032, 0.032),
            parte("anima", "sfera", "vetro", "#e8dcc0", 0, 0.016, 0, 0.012, 0.012, 0.012)]


def ritaglio_stracci():
    return [parte("foglio", "cubo", "tex:ritagliostracci", "", 0, 0.002, 0, 0.12, 0.003, 0.15, ry=-8)]


SEGRETI += [
    segreto("str_carretto", STR, "enigma", "Il cassetto del carretto",
            "Sotto il sedile del carretto di Capa 'e fierro c'è un cassetto che si apre girando la ruota: dentro, un foglietto con i conti scritti a matita.",
            1, "Carretto di Capa 'e fierro", BQ_STR),
    segreto("str_tappo", STR, "collezionabile", "Un tappo a corona",
            "Un tappo di bottiglia schiacciato dai passi, fra i mattoni del lotto bombardato. Anche i tappi servivano: schiacciati e forati, diventavano pedine e rotelle dei giochi di strada.",
            2, "Lotto, tra i mattoni"),
    segreto("str_bottone", STR, "collezionabile", "Un bottone militare",
            "Un bottone di divisa, finito nel mucchio degli stracci: nel dopoguerra i vestiti militari venivano disfatti e cuciti di nuovo, e un bottone buono si riusava.",
            3, "Mucchio di stracci della bottega"),
    segreto("str_ritaglio", STR, "collezionabile", "Ritaglio: piccoli lavori",
            "Un ritaglio scritto alla maniera dei giornali di allora (il testo è nostro): racconta i piccoli lavori dei bambini che aiutavano le famiglie.",
            4, "Cortile dei carrettieri, sotto il carro"),
    segreto("str_carta", STR, "collezionabile", "Una carta da gioco",
            "Una carta da gioco dimenticata sul davanzale del terrazzino: una partita lasciata a metà, tra un giro di stracci e l'altro.",
            5, "Terrazzino, sul davanzale"),
    segreto("str_biglia", STR, "collezionabile", "Una biglia di vetro",
            "Una biglia di vetro rossastra, rotolata vicino al lavatoio del cortile del bucato: il gioco dei bambini che, per un po', smettevano di lavorare.",
            6, "Cortile del bucato, vicino al lavatoio"),
]
PEZZI += [
    pezzo("str_tappo", STR, "Lotto, tra i mattoni", tappo(), "raccogli", "str_tappo", "Un tappo schiacciato", appoggio="suolo", dy=-0.1, scala=1.8, raggio=0.4),
    pezzo("str_bottone", STR, "Mucchio di stracci della bottega", bottone_militare(), "raccogli", "str_bottone", "Un bottone nel mucchio", appoggio="fisso", scala=1.8, raggio=0.4),
    pezzo("str_ritaglio", STR, "Cortile dei carrettieri, sotto il carro", ritaglio_stracci(), "raccogli", "str_ritaglio", "Un ritaglio di giornale", appoggio="suolo", dy=-0.1, scala=1.6, raggio=0.4),
    pezzo("str_carta", STR, "Terrazzino, sul davanzale", carta_gioco(), "raccogli", "str_carta", "Una carta da gioco", appoggio="fisso", scala=1.8, raggio=0.4),
    pezzo("str_biglia", STR, "Cortile del bucato, vicino al lavatoio", biglia_rossa(), "raccogli", "str_biglia", "Una biglia", appoggio="suolo", dy=-0.1, scala=1.8, raggio=0.4),
]
ENIGMI.append({
    "id": "enigma_carretto", "tipo": "oggetto", "titolo": "Il carretto di Capa 'e fierro", "meccanismo": "oggetto", "scena": STR, "segreto": "str_carretto",
    "consegna": "Un carretto da robivecchi, con due ruote grandi e un sedile di tavole. Sotto il sedile c'è qualcosa che non si vede bene.",
    "temaCivico": "Il lavoro dei bambini nel dopoguerra: conti scritti a matita, soldi contati con cura",
    "punto": "Carretto di Capa 'e fierro", "dx": 0.0, "dy": -0.4, "dz": 0.0, "yaw": 90, "scala": 1.5, "sostituisce": "CarrettoCapa", "distanza": 1.6, "inclina": 10,
    "prompt": "il carretto", "soluzione": ["ruota", "cassetto"], "ordinata": True,
    "bloccatoMsg": "", "erroreMsg": "Il cassetto non si muove: prima guarda la ruota.", "indizi": [],
    "spiegazione": "Girando la ruota, un fermo scatta e il cassetto sotto il sedile scorre fuori. Dentro, un foglietto con i conti di Capa 'e fierro: «Oggi tre chili di lana, due di cotone. Il ragazzo conta meglio di me.»",
    "scheda": "Saper contare era una difesa: chi sa fare i conti non si fa imbrogliare, e Amerigo, a sette anni, controllava anche quelli degli adulti.",
    "appunto": "Un foglietto di Capa 'e fierro: «Il ragazzo conta meglio di me.»",
    "parti": [
        parte("pianale", "cubo", "legno", "#5a3d26", 0.0, 0.0, 0.0, 1.1, 0.06, 0.6),
        parte("sponda_a", "cubo", "legno", "#4a3220", 0.0, 0.12, 0.3, 1.1, 0.2, 0.04),
        parte("sponda_b", "cubo", "legno", "#4a3220", -0.55, 0.12, 0.0, 0.04, 0.2, 0.6),
        parte("sedile", "cubo", "legno", "#6a4528", 0.3, 0.2, 0.0, 0.4, 0.04, 0.5),
        parte("sostegno_a", "cubo", "legno", "#4a3220", 0.12, 0.1, 0.0, 0.04, 0.2, 0.5),
        parte("ruota", "cilindro", "legno", "#3a2a1a", 0.0, -0.05, -0.34, 0.5, 0.025, 0.5, rx=90, clic=True, azione="gira", evento="ruota", asse="z", dopo="cassetto,foglio,conti",
              msg="La ruota gira con uno scatto: sotto il sedile qualcosa si sblocca."),
        parte("cerchio", "cilindro", "metallo", "#2d2a27", 0.0, -0.05, -0.34, 0.52, 0.012, 0.52, rx=90),
        parte("mozzo", "cilindro", "metallo", "#2d2a27", 0.0, -0.05, -0.36, 0.08, 0.03, 0.08, rx=90),
        parte("cassetto", "cubo", "legno", "#46362a", 0.3, 0.12, -0.26, 0.3, 0.08, 0.05, nascosta=True, clic=True, azione="muovi", evento="cassetto", asse="-z", valore=0.2,
              msg="Il cassetto scorre fuori: dentro c'è un foglietto."),
        parte("foglio", "cubo", "carta", "#d3c8a8", 0.3, 0.165, -0.28, 0.14, 0.004, 0.1, nascosta=True),
        parte("conti", "testo", "carta", "#2a2018", 0.3, 0.172, -0.28, 0.13, 0.1, 1, testo="3 lana\n2 cotone\nil ragazzo\nconta meglio\ndi me", dimensione=0.016, rx=90, font="mano", nascosta=True),
    ],
})
