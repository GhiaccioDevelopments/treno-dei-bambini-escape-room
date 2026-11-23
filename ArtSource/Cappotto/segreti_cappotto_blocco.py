# ============================================================================= CAPPOTTO (R014): la stazione e la carrozza
STANZE.append({"scena": STAZ, "nome": "Il ricordo del cappotto: la stazione", "completo": [
    "ris:flashback_cappotto/enigma_cartellini", "ris:flashback_cappotto/enigma_cappotti", "ris:flashback_cappotto/enigma_mela"]})
STANZE.append({"scena": VAG, "nome": "Il ricordo del cappotto: il treno", "completo": [
    "ris:flashback_cappotto/enigma_finestrino", "ris:flashback_cappotto/enigma_paura"]})
BQ_CAP = ("Il numero della carrozza e la valigia dimenticata sono inventati per il gioco. Vero è che in molte stazioni lungo la linea le donne dei comitati "
          "aspettavano i treni dei bambini con latte, biscotti e piccoli doni.")


def biglietto_terza():
    return [parte("cartoncino", "cubo", "carta", "#cdbb8a", 0, 0.002, 0, 0.06, 0.003, 0.035, ry=18),
            testo_piano("scritta", "III CL.", 0.012, 0, 0.0042, 0, "#2a2018", 0.05, 0.02, "")]


def bottone_croce_rossa():
    return [parte("bottone", "cilindro", "legno", "#e0d8c4", 0, 0.004, 0, 0.026, 0.004, 0.026),
            parte("croce_v", "cubo", "stoffa", "#a3261c", 0, 0.0085, 0, 0.005, 0.001, 0.016),
            parte("croce_o", "cubo", "stoffa", "#a3261c", 0, 0.0085, 0, 0.016, 0.001, 0.005)]


def gessetto():
    return [parte("gesso", "cilindro", "gesso", "#efe9da", 0, 0.006, 0, 0.012, 0.03, 0.012, rz=90, ry=35)]


def cartolina_firenze():
    return [parte("cartone", "cubo", "tex:cartolinafirenze", "", 0, 0.003, 0, 0.15, 0.004, 0.10, ry=-16)]


def fazzoletto_ricamato():
    return [parte("tela", "cubo", "stoffa", "#e6e0d0", 0, 0.002, 0, 0.12, 0.003, 0.12, ry=24),
            parte("ricamo", "cubo", "stoffa", "#8a2a1e", 0.035, 0.0038, 0.035, 0.025, 0.001, 0.008, ry=24)]


SEGRETI += [
    segreto("cap_valigia", VAG, "enigma", "La valigia dimenticata",
            "Sulla reticella della carrozza c'è una valigia di cartone con un lucchetto a tre rotelle. La combinazione è il numero della carrozza, quello dipinto sulla fiancata quando sei salito.",
            1, "Reticella della valigia", BQ_CAP),
    segreto("cap_biglietto", STAZ, "collezionabile", "Un biglietto di terza classe",
            "Un biglietto di cartoncino, terza classe, bucato dal controllore. I bambini dei treni non lo pagavano: le spese del viaggio le copriva chi organizzava.",
            2, "Panca della sala d'aspetto"),
    segreto("cap_bottone", STAZ, "collezionabile", "Un bottone della Croce Rossa",
            "Un bottone d'osso con una croce rossa dipinta a mano, caduto dal mucchio dei cappotti. Una volontaria lo cercherà per tutta la mattina.",
            3, "Accanto al mucchio di cappotti"),
    segreto("cap_gessetto", STAZ, "collezionabile", "Un gessetto",
            "Un pezzo di gesso: i ferrovieri ci scrivevano numeri e destinazioni sulle fiancate delle carrozze. Questo ha scritto 214.",
            4, "Banchina, sotto il numero della carrozza"),
    segreto("cap_cartolina", VAG, "collezionabile", "Una cartolina da Firenze",
            "Una cartolina con i saluti da Firenze, caduta a un bambino che tornava a casa da un altro viaggio. Firenze è una delle città che il treno attraversa davvero.",
            5, "Corridoio, accanto alla panca"),
    segreto("cap_fazzoletto", VAG, "collezionabile", "Un fazzoletto ricamato",
            "Un fazzoletto di cotone con una lettera ricamata in rosso, storta: lo ha cucito una madre la notte prima della partenza, perché il figlio non lo perdesse. È inventato per il gioco.",
            6, "Davanzale del finestrino"),
]
PEZZI += [
    pezzo("cap_biglietto", STAZ, "Panca della sala d'aspetto", biglietto_terza(), "raccogli", "cap_biglietto", "Un biglietto", appoggio="suolo", scala=2.0, raggio=0.45),
    pezzo("cap_bottone", STAZ, "Accanto al mucchio di cappotti", bottone_croce_rossa(), "raccogli", "cap_bottone", "Un bottone", appoggio="suolo", scala=2.2, raggio=0.4),
    pezzo("cap_gessetto", STAZ, "Banchina, sotto il numero della carrozza", gessetto(), "raccogli", "cap_gessetto", "Un gessetto", appoggio="suolo", scala=2.0, raggio=0.4),
    pezzo("cap_cartolina", VAG, "Corridoio, accanto alla panca", cartolina_firenze(), "raccogli", "cap_cartolina", "Una cartolina", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("cap_fazzoletto", VAG, "Davanzale del finestrino", fazzoletto_ricamato(), "raccogli", "cap_fazzoletto", "Un fazzoletto", appoggio="fisso", scala=1.4, raggio=0.4),
]
ENIGMI.append({
    "id": "enigma_valigia", "tipo": "oggetto", "titolo": "La valigia dimenticata", "meccanismo": "ruote", "scena": VAG, "segreto": "cap_valigia",
    "consegna": "Una valigia di cartone sulla reticella, legata con lo spago e chiusa da un lucchetto a tre rotelle. Sul coperchio, a matita: «Stessa carrozza, stesso numero.»",
    "temaCivico": "Una valigia dimenticata e ritrovata: anche le cose degli altri vanno custodite.",
    "punto": "Reticella della valigia", "dx": 0.0, "dy": 0.0, "dz": 0.0, "yaw": 90, "scala": 1.0, "distanza": 1.4, "inclina": 12,
    "prompt": "la valigia sulla reticella", "codice": "214", "soluzione": ["apri_valigia"],
    "ruote": {"n": 3, "x": 0.06, "y": 0.13, "z": -0.13, "passo": 0.07, "larghezza": 0.05, "raggio": 0.04, "colore": "#9b7a35"},
    "bloccatoMsg": "", "erroreMsg": "Il lucchetto non si apre: che numero era dipinto sulla fiancata della carrozza, quando sei salito?", "indizi": [],
    "spiegazione": "Due, uno, quattro: il lucchetto scatta. Dentro, avvolti in un tovagliolo, biscotti secchi e un biglietto: «Per i bambini del treno, dalle donne della stazione.»",
    "scheda": "Lungo la linea, nelle stazioni, i comitati delle donne portavano ai bambini latte, biscotti e piccoli regali: per molti era il primo segno dell'accoglienza.",
    "appunto": "Nella valigia della reticella: biscotti per i bambini del treno.",
    "parti": [
        parte("valigia", "cubo", "carta", "#8a6a44", 0.0, 0.11, 0.0, 0.5, 0.22, 0.24),
        parte("valigia_bordo", "cubo", "carta", "#6d5234", 0.0, 0.2, 0.0, 0.51, 0.02, 0.25),
        parte("spago", "cubo", "stoffa", "#c9b98a", -0.15, 0.11, 0.0, 0.012, 0.225, 0.245),
        parte("maniglia", "cubo", "cuoio", "#3a2418", 0.0, 0.235, 0.0, 0.12, 0.02, 0.03),
        testo_piano("scritta", "STESSA CARROZZA, STESSO NUMERO", 0.012, 0.0, 0.225, 0.04, "#2a2018", 0.3, 0.04),
        parte("lucchetto", "cubo", "ottone", "#b59a3f", 0.06, 0.13, -0.125, 0.22, 0.07, 0.012),
        parte("lucchetto_ansa", "cilindro", "ottone", "#b59a3f", 0.17, 0.13, -0.13, 0.03, 0.012, 0.03, rx=90, clic=True, azione="verifica", evento="apri_valigia",
              dopo="coperchio_aperto,tovagliolo,biscotto_a,biscotto_b,biglietto_donne", msg="Clic! Il lucchetto si apre e lo spago scivola via."),
        parte("coperchio_aperto", "cubo", "carta", "#7c5e3c", 0.0, 0.34, 0.12, 0.5, 0.24, 0.02, rx=-20, nascosta=True),
        parte("tovagliolo", "cubo", "stoffa", "#e0d8c4", 0.0, 0.215, 0.0, 0.3, 0.01, 0.16, nascosta=True),
        parte("biscotto_a", "cilindro", "cera", "#c49a5a", -0.06, 0.225, 0.0, 0.05, 0.006, 0.05, nascosta=True),
        parte("biscotto_b", "cilindro", "cera", "#b98e50", 0.05, 0.225, 0.02, 0.05, 0.006, 0.05, nascosta=True),
        parte("biglietto_donne", "cubo", "carta", "#e3d9bd", 0.12, 0.222, -0.03, 0.08, 0.002, 0.05, nascosta=True),
    ],
})
