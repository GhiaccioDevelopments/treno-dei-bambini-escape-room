# ============================================================================= VIOLINO (R015): Modena, la corte dei Benvenuti
STANZE.append({"scena": MOD, "nome": "Il ricordo del violino: Modena", "completo": [
    "ris:flashback_violino/enigma_forno", "ris:flashback_violino/enigma_attrezzi", "ris:flashback_violino/enigma_pianoforte",
    "ris:flashback_violino/enigma_impronte", "ris:flashback_violino/enigma_melodia"]})
BQ_VIO = ("La carta d'Italia con le puntine e il disegno nella stufa sono inventati per il gioco. Vero è che nelle scuole emiliane i bambini ospiti "
          "andavano in classe con i figli delle famiglie che li accoglievano.")


def pagella():
    return [parte("cartoncino", "cubo", "tex:pagella", "", 0, 0.002, 0, 0.1, 0.003, 0.13, ry=-12)]


def quaderno_righe():
    return [parte("copertina", "cubo", "carta", "#2e4a6a", 0, 0.004, 0, 0.15, 0.008, 0.2, ry=14),
            parte("etichetta", "cubo", "carta", "#e3d9bd", 0, 0.0085, -0.03, 0.08, 0.001, 0.04, ry=14),
            parte("pagine", "cubo", "carta", "#e8e0c8", 0.004, 0.004, 0, 0.145, 0.006, 0.195, ry=14)]


def pupazzetto():
    return [parte("base", "sfera", "gesso", "#eef0f2", 0, 0.05, 0, 0.1, 0.1, 0.1),
            parte("testa", "sfera", "gesso", "#eef0f2", 0, 0.125, 0, 0.065, 0.065, 0.065),
            parte("naso", "cilindro", "legno", "#b8662c", 0, 0.125, -0.035, 0.008, 0.012, 0.008, rx=90),
            parte("occhio_a", "sfera", "metallo", "#1a1410", -0.012, 0.135, -0.03, 0.008, 0.008, 0.008),
            parte("occhio_b", "sfera", "metallo", "#1a1410", 0.012, 0.135, -0.03, 0.008, 0.008, 0.008)]


def corda_violino():
    return [parte("rotolo", "cilindro", "argento", "#c8c8c0", 0, 0.002, 0, 0.07, 0.002, 0.07),
            parte("rotolo_b", "cilindro", "argento", "#b8b8b0", 0, 0.004, 0, 0.05, 0.002, 0.05),
            parte("pallino", "sfera", "ottone", "#b59a3f", 0.03, 0.004, 0.0, 0.008, 0.008, 0.008),
            parte("bustina", "cubo", "carta", "#d8cba6", -0.05, 0.002, 0.03, 0.06, 0.002, 0.05, ry=20)]


def cartolina_modena():
    return [parte("cartone", "cubo", "tex:cartolinamodena", "", 0, 0.003, 0, 0.15, 0.004, 0.10, ry=10)]


SEGRETI += [
    segreto("vio_carta", MOD, "enigma", "La carta d'Italia",
            "Sulla carta d'Italia della scuola le puntine dei bambini ospiti formano un treno da Napoli a Modena. Ne manca una: rimettila al suo posto.",
            1, "Parete della carta d'Italia", BQ_VIO),
    segreto("vio_pagella", MOD, "collezionabile", "La pagella",
            "La pagella del primo trimestre, scritta in bella calligrafia: lettura buono, aritmetica ottimo, canto ottimo. Il voto in canto la maestra l'ha sottolineato due volte.",
            2, "Banco di Amerigo"),
    segreto("vio_quaderno", MOD, "collezionabile", "Un quaderno a righe",
            "Un quaderno a righe con la copertina blu. Sulla prima pagina, ripassata tante volte: AMERIGO.",
            3, "Davanzale della stanzetta"),
    segreto("vio_pupazzo", MOD, "collezionabile", "Un pupazzetto di neve",
            "Un pupazzetto di neve grande come una mano, con un naso di legno: Nario lo ha fatto per Amerigo, che la neve non l'aveva mai toccata.",
            4, "Dietro il pagliaio"),
    segreto("vio_corda", MOD, "collezionabile", "Una corda di violino",
            "Una corda di violino arrotolata nella sua bustina, caduta dal banco di Alcide. Le corde costavano, e non si buttava niente.",
            5, "Sotto il banco dell'accordatore"),
    segreto("vio_cartolina", MOD, "collezionabile", "Una cartolina di Modena",
            "Una cartolina con i saluti da Modena, comprata da Derna per Antonietta. Sul retro manca ancora l'indirizzo.",
            6, "Tavolo della cucina di Derna"),
]
PEZZI += [
    pezzo("vio_pagella", MOD, "Banco di Amerigo", pagella(), "raccogli", "vio_pagella", "La pagella", appoggio="fisso", scala=1.6, raggio=0.45),
    pezzo("vio_quaderno", MOD, "Davanzale della stanzetta", quaderno_righe(), "raccogli", "vio_quaderno", "Un quaderno", appoggio="fisso", scala=1.2, raggio=0.45),
    pezzo("vio_pupazzo", MOD, "Dietro il pagliaio", pupazzetto(), "raccogli", "vio_pupazzo", "Un pupazzetto di neve", appoggio="suolo", scala=2.0, raggio=0.5),
    pezzo("vio_corda", MOD, "Sotto il banco dell'accordatore", corda_violino(), "raccogli", "vio_corda", "Una corda di violino", appoggio="suolo", scala=2.2, raggio=0.45),
    pezzo("vio_cartolina", MOD, "Tavolo della cucina di Derna", cartolina_modena(), "raccogli", "vio_cartolina", "Una cartolina", appoggio="fisso", scala=1.4, raggio=0.45),
]
# le puntine della carta: Modena in alto, poi Bologna, Firenze, Roma; il posto di Napoli e' vuoto, la puntina e' caduta sulla stufa
PUNTINE = [("modena", -0.04, 1.93), ("bologna", 0.0, 1.86), ("firenze", -0.02, 1.76), ("roma", 0.07, 1.6)]
_parti_carta = [
    parte("carta", "cubo", "carta", "#d8cfae", 0.0, 1.7, -0.01, 1.0, 0.8, 0.01),
    parte("cornice_a", "cubo", "legno", "#4a3220", 0.0, 2.11, -0.015, 1.04, 0.03, 0.02),
    parte("cornice_b", "cubo", "legno", "#4a3220", 0.0, 1.29, -0.015, 1.04, 0.03, 0.02),
    parte("stivale", "cubo", "carta", "#b9a77a", 0.06, 1.68, -0.017, 0.13, 0.62, 0.002, rz=-28),
    parte("pianura", "cubo", "carta", "#b9a77a", -0.06, 1.95, -0.017, 0.36, 0.1, 0.002, rz=-6),
    parte("sicilia", "cubo", "carta", "#b9a77a", 0.12, 1.36, -0.017, 0.14, 0.06, 0.002, rz=8),
    parte("sardegna", "cubo", "carta", "#b9a77a", -0.24, 1.58, -0.017, 0.05, 0.12, 0.002),
    parte("titolo", "testo", "carta", "#2a2018", -0.3, 2.04, -0.02, 0.3, 0.04, 1, testo="ITALIA", dimensione=0.024, font=""),
]
for n, x, y in PUNTINE:
    _parti_carta.append(parte("puntina_" + n, "sfera", "metallo", "#a3261c", x, y, -0.025, 0.022, 0.022, 0.022, clic=True, azione="leggi",
                              msg="Una puntina rossa su %s." % n.capitalize()))
_parti_carta += [
    parte("filo_treno", "cubo", "stoffa", "#2a2018", 0.03, 1.765, -0.022, 0.004, 0.36, 0.002, rz=-14),
    parte("posto_napoli", "cilindro", "carta", "#8a7a5a", 0.13, 1.5, -0.02, 0.03, 0.002, 0.03, rx=90, clic=True, azione="usa", richiede="puntina_napoli",
          evento="napoli_al_suo_posto", dopo="puntina_napoli_su,-posto_napoli,-sportello,sportello_aperto,disegno,disegno_treno,disegno_sole",
          msg="Un buchino nella carta, sul golfo di Napoli: qui c'era una puntina."),
    parte("puntina_napoli_su", "sfera", "metallo", "#a3261c", 0.13, 1.5, -0.025, 0.022, 0.022, 0.022, nascosta=True),
    # la stufa spenta di riserva sotto la carta, con lo sportello chiuso
    parte("stufa", "cilindro", "metallo", "#2e2a26", 0.0, 0.4, -0.32, 0.5, 0.4, 0.5),
    parte("stufa_piano", "cilindro", "metallo", "#2e2a26", 0.0, 0.81, -0.32, 0.56, 0.02, 0.56),
    parte("puntina_napoli", "sfera", "metallo", "#a3261c", 0.12, 0.835, -0.36, 0.022, 0.022, 0.022, clic=True, azione="prendi", produce="puntina_napoli",
          msg="Una puntina rossa caduta sulla stufa. Sulla testa, a matita, una N."),
    parte("sportello", "cubo", "metallo", "#1e1a16", 0.0, 0.36, -0.58, 0.22, 0.2, 0.02),
    parte("sportello_aperto", "cubo", "metallo", "#1e1a16", 0.13, 0.36, -0.68, 0.22, 0.2, 0.02, ry=-75, nascosta=True),
    parte("disegno", "cubo", "carta", "#e8e0c8", 0.0, 0.3, -0.42, 0.18, 0.004, 0.13, nascosta=True),
    parte("disegno_treno", "cubo", "carta", "#2e4a6a", 0.0, 0.305, -0.42, 0.12, 0.002, 0.03, nascosta=True),
    parte("disegno_sole", "cilindro", "carta", "#c88a2a", 0.05, 0.305, -0.46, 0.03, 0.002, 0.03, nascosta=True),
]
ENIGMI.append({
    "id": "enigma_carta_italia", "tipo": "oggetto", "titolo": "La carta d'Italia", "meccanismo": "oggetto", "scena": MOD, "segreto": "vio_carta",
    "consegna": "Sulla parete, la carta d'Italia della scuola. Le puntine rosse dei bambini ospiti, unite da un filo, fanno la linea di un treno. In fondo alla linea manca una puntina.",
    "temaCivico": "Una carta sola per tutti: la Repubblica è una e indivisibile (art. 5), e il treno dei bambini l'ha cucita da Sud a Nord.",
    "punto": "Parete della carta d'Italia", "dx": 0.0, "dy": 0.0, "dz": 0.0, "yaw": 270, "scala": 1.0, "distanza": 2.0, "inclina": 8,
    "prompt": "la carta d'Italia", "soluzione": ["napoli_al_suo_posto"],
    "bloccatoMsg": "", "erroreMsg": "", "indizi": [],
    "spiegazione": "La puntina di Napoli torna in fondo alla linea. Lo sportello della stufa spenta, sotto la carta, si apre con un cigolio: dentro c'è un disegno a pastello. Un treno blu, un sole giallo, e una scritta storta: AMERIGO.",
    "scheda": "Nelle classi emiliane i bambini del Sud erano tanti: le maestre li accoglievano anche con la geografia, per capire da dove venivano e quanto lontano avevano viaggiato.",
    "appunto": "Nella stufa spenta della scuola: il disegno del treno, firmato Amerigo.",
    "oggetti": [{"id": "puntina_napoli", "nome": "La puntina di Napoli", "descrizione": "Una puntina rossa con una N a matita."}],
    "parti": _parti_carta,
})
