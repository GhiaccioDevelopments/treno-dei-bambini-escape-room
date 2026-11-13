# ============================================================================= FINALE (R016): il basso nel 1994
STANZE.append({"scena": FIN, "nome": "Il basso, 1994", "completo": [
    "ris:flashback_finale/enigma_basso94", "ris:flashback_finale/enigma_ricevuta", "ris:flashback_finale/enigma_custodia", "ris:flashback_finale/enigma_carmine"]})
BQ_FIN = ("La radio, la frequenza segnata a matita e il violino che suona alla radio sono inventati per il gioco: Amerigo violinista famoso e' nel romanzo, "
          "la trasmissione no.")


def ritaglio_frac():
    return [parte("carta", "cubo", "tex:ritaglio", "", 0, 0.002, 0, 0.1, 0.003, 0.13, ry=-8)]


def gettone():
    return [parte("gettone", "cilindro", "ottone", "#a88a3e", 0, 0.002, 0, 0.026, 0.002, 0.026),
            parte("solco_a", "cubo", "metallo", "#6a5a2a", 0, 0.0035, -0.004, 0.016, 0.001, 0.002),
            parte("solco_b", "cubo", "metallo", "#6a5a2a", 0, 0.0035, 0.004, 0.016, 0.001, 0.002)]


def biglietto_94():
    return [parte("cartoncino", "cubo", "tex:biglietto_treno", "", 0, 0.002, 0, 0.09, 0.003, 0.05, ry=14)]


def santino_nuovo():
    return [parte("santino", "cubo", "carta", "#e8e0c8", 0, 0.002, 0, 0.06, 0.003, 0.1, ry=-20),
            parte("figura", "cubo", "carta", "#6a7aa0", 0, 0.004, -0.005, 0.04, 0.001, 0.06, ry=-20)]


def exvoto_piede():
    return [parte("piede", "cubo", "argento", "#c8c8c0", 0, 0.004, 0, 0.05, 0.008, 0.11, ry=12),
            parte("dita", "sfera", "argento", "#c8c8c0", 0, 0.004, -0.06, 0.05, 0.01, 0.03, ry=12)]


SEGRETI += [
    segreto("fin_radio", FIN, "enigma", "La radio sintonizzata male",
            "Sul televisore, una radio che gracchia. Sul mobile, a matita, un numero: la stazione che Antonietta ascoltava. Girando la manopola si sente un violino.",
            1, "La radio sul mobile", BQ_FIN),
    segreto("fin_frac", FIN, "collezionabile", "Amerigo in frac",
            "Una fotografia ritagliata da un giornale: un violinista in frac, davanti a un'orchestra. Antonietta ne aveva piu' di una, nascoste in posti diversi.",
            2, "Davanti al televisore"),
    segreto("fin_gettone", FIN, "collezionabile", "Un gettone telefonico",
            "Un gettone con i due solchi: per le telefonate dalla cabina del bar all'angolo. Qualcuno, ogni tanto, chiamava il Nord.",
            3, "Accanto al telefono"),
    segreto("fin_biglietto", FIN, "collezionabile", "Un biglietto del treno, 1994",
            "Il biglietto con cui Amerigo e' tornato a Napoli: seconda classe, sola andata. Il ritorno non l'aveva ancora deciso.",
            4, "Sugli scatoloni"),
    segreto("fin_santino", FIN, "collezionabile", "Un santino nuovo",
            "Un santino stampato da poco, con i colori ancora vivi: qualcuno del vicolo lo ha lasciato per Antonietta.",
            5, "Mensola della cucina"),
    segreto("fin_exvoto", FIN, "collezionabile", "L'ex-voto del piede",
            "Un piccolo piede d'argento, come quello dell'edicola del vicolo: un ex-voto per chi cammina tanto, o per chi deve partire.",
            6, "Davanzale del cortile"),
]
PEZZI += [
    pezzo("fin_frac", FIN, "Davanti al televisore", ritaglio_frac(), "raccogli", "fin_frac", "Un ritaglio di giornale", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("fin_gettone", FIN, "Accanto al telefono", gettone(), "raccogli", "fin_gettone", "Un gettone", appoggio="suolo", scala=2.4, raggio=0.4),
    pezzo("fin_biglietto", FIN, "Sugli scatoloni", biglietto_94(), "raccogli", "fin_biglietto", "Un biglietto del treno", appoggio="fisso", scala=1.8, raggio=0.45),
    pezzo("fin_santino", FIN, "Mensola della cucina", santino_nuovo(), "raccogli", "fin_santino", "Un santino", appoggio="suolo", scala=1.8, raggio=0.4),
    pezzo("fin_exvoto", FIN, "Davanzale del cortile", exvoto_piede(), "raccogli", "fin_exvoto", "Un ex-voto", appoggio="suolo", scala=2.0, raggio=0.4),
]
ENIGMI.append({
    "id": "enigma_radio", "tipo": "oggetto", "titolo": "La radio sintonizzata male", "meccanismo": "ruote", "scena": FIN, "segreto": "fin_radio",
    "consegna": "Una radio di bachelite, accesa a volume bassissimo: gracchia fra due stazioni. Sul legno del mobile, a matita, tre cifre ripassate tante volte.",
    "temaCivico": "La musica come diritto di tutti: la radio portava i concerti anche nelle case che un teatro non l'avevano mai visto.",
    "punto": "La radio sul mobile", "dx": 0.0, "dy": 0.0, "dz": 0.0, "yaw": 0, "scala": 1.0, "distanza": 1.1, "inclina": 10,
    "prompt": "la radio", "codice": "927", "soluzione": ["sintonizza"],
    "ruote": {"n": 3, "x": 0.06, "y": 0.08, "z": -0.09, "passo": 0.04, "larghezza": 0.03, "raggio": 0.025, "colore": "#c8b890"},
    "bloccatoMsg": "", "erroreMsg": "Solo fruscio. Il numero a matita sul mobile: 92,7.", "indizi": [],
    "spiegazione": "Novantadue virgola sette. Il fruscio si apre e dentro c'e' un violino: due frasi semplici, che conosci. Le suonavi tu, a Modena, con i pallini colorati.",
    "scheda": "Negli anni Novanta molte radio locali trasmettevano musica classica la sera: chi non andava a teatro ascoltava cosi'.",
    "appunto": "Sulla radio di mamma, 92,7: il violino.",
    "parti": [
        parte("radio", "cubo", "legno", "#5a3a22", 0.0, 0.08, 0.0, 0.3, 0.16, 0.16),
        parte("tela", "cubo", "stoffa", "#b9a988", -0.07, 0.08, -0.081, 0.13, 0.12, 0.002),
        parte("scala", "cubo", "vetro", "#e8dca8", 0.06, 0.12, -0.081, 0.12, 0.03, 0.002, testo="88  92  96  100", dimensione=0.008),
        parte("matita", "testo", "carta", "#3a3a3a", 0.0, 0.0, -0.1, 0.12, 0.03, 1, testo="92,7", dimensione=0.022, font="mano", rx=90),
        parte("manopola", "cilindro", "legno", "#2a1a12", 0.12, 0.08, -0.09, 0.035, 0.012, 0.035, rx=90, clic=True, azione="verifica", evento="sintonizza",
              msg="La lancetta si ferma sul 92,7. Il fruscio si apre."),
    ],
})
