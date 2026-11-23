
# ============================================================================= VOLANTINO (R013)
STANZE.append({"scena": VOL, "nome": "Il ricordo del volantino", "completo": [
    "ris:flashback_volantino/enigma_voci", "ris:flashback_volantino/enigma_bacheca", "ris:flashback_volantino/enigma_ciclostile",
    "ris:flashback_volantino/enigma_ruoli", "ris:flashback_volantino/enigma_timbro"]})
BQ_VOL = ("Le impronte d'inchiostro dei bambini sul registro sono un'invenzione del gioco; vero è che i bambini, prima di partire, venivano schedati uno per uno, "
          "oltre che lavati, vaccinati e vestiti. L'UDI (Unione Donne Italiane) fu fra le promotrici dei treni insieme al PCI, con l'aiuto della Croce Rossa e di altre associazioni.")


def matrice_rovinata():
    return [parte("foglio", "cubo", "carta", "#e3d9bd", 0, 0.003, 0, 0.16, 0.004, 0.2, ry=12),
            parte("piega", "cubo", "carta", "#d2c6a4", 0.03, 0.012, 0.02, 0.09, 0.004, 0.1, ry=-20, rz=18),
            parte("macchia", "cubo", "carta", "#1d1a18", -0.03, 0.006, -0.04, 0.06, 0.001, 0.05, ry=30)]


def timbro_vecchio():
    return [parte("gomma", "cubo", "cuoio", "#5a1a16", 0, 0.008, 0, 0.07, 0.016, 0.04),
            parte("base", "cubo", "legno", "#6a4528", 0, 0.024, 0, 0.074, 0.018, 0.044),
            parte("manico", "cilindro", "legno", "#4a3220", 0, 0.05, 0, 0.026, 0.024, 0.026),
            parte("pomo", "sfera", "legno", "#4a3220", 0, 0.078, 0, 0.034, 0.03, 0.034)]


def manifesto_elezioni():
    return [parte("foglio", "cubo", "tex:manifestoelezioni", "", 0, 0.002, 0, 0.13, 0.003, 0.17, ry=-14)]


def cartolina_nord():
    return [parte("cartone", "cubo", "tex:cartolinanord", "", 0, 0.003, 0, 0.15, 0.004, 0.10, ry=20)]


def matita_copiativa():
    return [parte("legno", "cilindro", "legno", "#5b3a6e", 0, 0.006, 0, 0.012, 0.07, 0.012, rz=90, ry=25),
            parte("punta", "cilindro", "legno", "#2e1f3a", 0.072, 0.006, 0.034, 0.008, 0.008, 0.008, rz=90, ry=25)]


SEGRETI += [
    segreto("vol_impronte", VOL, "enigma", "Il cassetto delle impronte",
            "Sul leggio della stanza dei registri c'è un registro aperto con le impronte d'inchiostro delle dita dei bambini. Contarle apre il cassetto: dentro, una spilla dell'UDI.",
            1, "Leggio dei registri", BQ_VOL),
    segreto("vol_matrice", VOL, "collezionabile", "Una matrice rovinata",
            "Una matrice di carta cerata, strappata e macchiata: la prova sbagliata finita nel cestino. Con il ciclostile si sbagliava spesso, e si ricominciava.",
            2, "Cestino del ciclostile"),
    segreto("vol_timbro", VOL, "collezionabile", "Un timbro vecchio",
            "Un timbro di gomma consumato, con le lettere al contrario: serviva a mettere il nome del Comitato sui fogli. Le lettere sono al contrario perché sul foglio escano dritte.",
            3, "Scaffale dei registri"),
    segreto("vol_manifesto", VOL, "collezionabile", "Un manifesto delle elezioni",
            "Un manifesto strappato dal muro del largo. Nel 1946 si votò più volte; il 2 giugno, per la prima volta in tutta Italia, votarono anche le donne.",
            4, "Largo, sotto il manifesto strappato"),
    segreto("vol_cartolina", VOL, "collezionabile", "Una cartolina dal Nord",
            "Una cartolina con i saluti dall'Emilia, caduta vicino al lavatoio: un bambino ospitato scrive che sta bene. È inventata per il gioco, come le altre lettere di questo ricordo.",
            5, "Cortile, accanto al lavatoio"),
    segreto("vol_matita", VOL, "collezionabile", "Una matita copiativa",
            "Una matita copiativa: la mina, bagnata, scrive viola e non si cancella. Il 2 giugno 1946 si votò proprio con una matita così.",
            6, "Sala, davanti alla panca"),
]
PEZZI += [
    pezzo("vol_matrice", VOL, "Cestino del ciclostile", matrice_rovinata(), "raccogli", "vol_matrice", "Una matrice strappata", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("vol_timbro", VOL, "Scaffale dei registri", timbro_vecchio(), "raccogli", "vol_timbro", "Un timbro vecchio", appoggio="fisso", scala=1.6, raggio=0.45),
    pezzo("vol_manifesto", VOL, "Largo, sotto il manifesto strappato", manifesto_elezioni(), "raccogli", "vol_manifesto", "Un manifesto strappato", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("vol_cartolina", VOL, "Cortile, accanto al lavatoio", cartolina_nord(), "raccogli", "vol_cartolina", "Una cartolina", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("vol_matita", VOL, "Sala, davanti alla panca", matita_copiativa(), "raccogli", "vol_matita", "Una matita", appoggio="suolo", scala=1.8, raggio=0.4),
]
IMPRONTE = [(-0.2, -0.1), (-0.12, 0.02), (-0.22, 0.09), (-0.07, -0.12), (-0.05, 0.12), (-0.16, -0.02),
            (0.07, -0.08), (0.15, 0.04), (0.22, -0.11), (0.1, 0.11), (0.2, 0.12)]   # 11 impronte (codice "11")
ENIGMI.append({
    "id": "enigma_impronte", "tipo": "oggetto", "titolo": "Il registro delle impronte", "meccanismo": "ruote", "scena": VOL, "segreto": "vol_impronte",
    "consegna": "Un registro aperto sul leggio. Al posto delle firme, impronte d'inchiostro: dita di bambini che non sapevano ancora scrivere. Sotto, un cassetto con due rotelle di ottone.",
    "temaCivico": "Ogni bambino iscritto è una persona con un nome: contare le impronte vuol dire contare loro.",
    "punto": "Leggio dei registri", "dx": 0.0, "dy": 0.0, "dz": 0.0, "yaw": -90, "scala": 1.0, "distanza": 1.5, "inclina": 30,
    "prompt": "il registro sul leggio", "codice": "11", "soluzione": ["apri_cassetto"],
    "ruote": {"n": 2, "x": -0.04, "y": 0.84, "z": -0.215, "passo": 0.1, "larghezza": 0.06, "raggio": 0.045, "colore": "#9b7a35"},
    "bloccatoMsg": "", "erroreMsg": "Il cassetto non si apre: conta di nuovo le impronte, su tutte e due le pagine.", "indizi": [],
    "spiegazione": "Undici impronte, undici bambini iscritti in una sera. Il cassetto scorre fuori: dentro c'è una spilla smaltata dell'UDI, dimenticata da una volontaria.",
    "scheda": "Chi non sapeva scrivere firmava con una croce o con un'impronta: nel 1946 in Italia erano ancora in tanti a non saper leggere e scrivere.",
    "appunto": "Nel cassetto del leggio: una spilla dell'UDI.",
    "parti": [
        parte("gamba", "cubo", "legno", "#4a3220", 0.0, 0.45, 0.05, 0.08, 0.9, 0.08),
        parte("piede", "cubo", "legno", "#4a3220", 0.0, 0.03, 0.05, 0.5, 0.06, 0.4),
        parte("cassetto", "cubo", "legno", "#5a3d26", 0.0, 0.84, 0.0, 0.5, 0.14, 0.4),
        parte("piano", "cubo", "legno", "#5a3d26", 0.0, 0.93, 0.0, 0.62, 0.04, 0.44),
        parte("pagina_sx", "cubo", "carta", "#d8cba6", -0.14, 0.955, 0.0, 0.27, 0.008, 0.36),
        parte("pagina_dx", "cubo", "carta", "#d8cba6", 0.14, 0.955, 0.0, 0.27, 0.008, 0.36),
        parte("costola", "cubo", "cuoio", "#4a2a22", 0.0, 0.955, 0.0, 0.02, 0.012, 0.37),
    ] + [parte("impronta_%d" % i, "sfera", "cera", "#3a2e4a", x, 0.961, z, 0.026, 0.003, 0.036, ry=(i * 37) % 60 - 30) for i, (x, z) in enumerate(IMPRONTE)] + [
        parte("maniglia", "cilindro", "ottone", "#d6b25a", 0.18, 0.84, -0.21, 0.035, 0.012, 0.035, rx=90, clic=True, azione="verifica", evento="apri_cassetto",
              dopo="cassetto_aperto,spilla,spilla_smalto", msg="Clic! Il cassetto scorre fuori."),
        parte("cassetto_aperto", "cubo", "legno", "#46362a", 0.0, 0.74, -0.3, 0.44, 0.05, 0.22, nascosta=True),
        parte("spilla", "cilindro", "ottone", "#c9ac4a", 0.05, 0.775, -0.3, 0.04, 0.004, 0.04, nascosta=True),
        parte("spilla_smalto", "cilindro", "vetro", "#a3261c", 0.05, 0.78, -0.3, 0.024, 0.002, 0.024, nascosta=True),
    ],
})
