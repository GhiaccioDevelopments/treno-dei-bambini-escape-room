# -*- coding: utf-8 -*-
"""R014: genera Assets/_Game/Resources/Data/flashback_cappotto.json (i cinque enigmi del ricordo "Il cappotto": la stazione e il treno).
I testi del gioco stanno nel JSON generato; questo script e' solo la sorgente riproducibile. Uso: python genera_flashback_cappotto.py
Coordinate = quelle di SceneBuilderCappotto.cs (devono coincidere). Il ricordo sta su DUE scene: 60_FB_Stazione (C1, C2, C3) e 61_FB_Vagone (C4, C5):
ogni enigma ha il campo "scena" e nasce solo nella sua (EscapeRoomController).
Fatti storici: solo quelli confermati in Docs/VERIFICHE_STORICHE.md (vedi il Rapporto di R014). Persone, numeri dei cartellini e dei cappotti,
nomi dei bambini dello scompartimento: inventati per il gioco. Mariuccia e Tommasino sono personaggi del romanzo: niente cognomi inventati.
Chi da' le scarpe ad Amerigo non si dice (TRAMA, Decisione 12 e §9 n. 14).
"""
import json, math, os, random, re

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data", "flashback_cappotto.json")
STAZ, VAG = "60_FB_Stazione", "61_FB_Vagone"


def P(id, forma="cubo", mat="legno", col="", x=0, y=0, z=0, sx=1, sy=1, sz=1, **kw):
    d = {"id": id, "forma": forma, "materiale": mat}
    if col: d["colore"] = col
    for k, v in (("x", x), ("y", y), ("z", z)):
        if v: d[k] = round(v, 4)
    for k, v in (("sx", sx), ("sy", sy), ("sz", sz)):
        if v != 1: d[k] = round(v, 4)
    for k in ("rx", "ry", "rz"):
        if k in kw: kw[k] = round(kw[k], 2)
    d.update(kw)
    return d


def T(id, testo, x, y, z, sx, sy, dim, col="#3b2a1a", font="", **kw):
    d = P(id, "testo", "legno", col, x, y, z, sx, sy, testo=testo, dimensione=dim, **kw)
    if font: d["font"] = font
    return d


def quiz(testo, opz, corretta, spieg):
    return {"testo": testo, "opzioni": opz, "corretta": corretta, "spiegazione": spieg}


def tavolo(lx, lz, h, col="#5a3d26", gamba="#4a3220", cx=0.0, cz=0.0, pre=""):
    """Piano (spessore 6 cm, superficie a quota h) e quattro gambe, centrato in (cx, cz)."""
    hx, hz = lx / 2 - 0.06, lz / 2 - 0.06
    return [P(pre + "piano", "cubo", "legno", col, x=cx, y=h - 0.03, z=cz, sx=lx, sy=0.06, sz=lz)] + [
        P(pre + "gamba_" + n, "cubo", "legno", gamba, x=cx + gx, y=(h - 0.06) / 2, z=cz + gz, sx=0.07, sy=h - 0.06, sz=0.07)
        for n, gx, gz in (("a", -hx, -hz), ("b", hx, -hz), ("c", -hx, hz), ("d", hx, hz))]


# ---------------------------------------------------------------- posizioni nel mondo (= SceneBuilderCappotto.cs)
POS_C1 = (-8.0, 0.0, -7.0)    # tavolo del Comitato nell'atrio, fronte verso sud (l'ingresso)
POS_C2 = (8.0, 0.0, -7.0)     # tavolo della Croce Rossa con il mucchio di cappotti, fronte verso sud
POS_C3 = (-2.4, 0.0, 14.0)    # banchina, accanto alla porta della carrozza 214: Antonietta e la valigia
POS_C4 = (-0.9, 0.0, 17.9)    # carrozza: la carta ferroviaria sulla parete di testa (a nord), fronte verso sud
POS_C5 = (0.0, 0.0, 9.0)      # carrozza: lo scompartimento a meta' vagone (i tre bambini sulle panche), fronte verso sud

PELLE = "#c99b78"
CAPELLI = "#4a3020"   # Amerigo: castani (CLAUDE.md)


# ---------------------------------------------------------------- figure (bambini da primitive: gruppo "vuoto" cliccabile + pezzi figli)
def figura(id, idx, x, z, h, abito, capelli, nome_msg, scarpe=True, trecce=False, berretto=False, oggetto=None, nascosta=False, seduta=False, gruppo=""):
    """Bambino in piedi (o seduto) alto h, fronte verso -z. Centro del gruppo a meta' altezza. Ritorna (parti, collo, busto)."""
    g = dict(clic=True, azione="figura", valore=idx, msg=nome_msg)
    if nascosta: g["nascosta"] = True
    if gruppo: g["gruppo"] = gruppo
    yc = h / 2 if not seduta else 0.45 + h * 0.26
    parti = [P(id, "vuoto", "stoffa", "", x=x, y=yc, z=z, sx=0.42, sy=(h if not seduta else h * 0.62), sz=0.34, **g)]
    G = dict(genitore=id)
    gamba = h * 0.45
    busto_h = h * 0.32
    testa = 0.17 + h * 0.03
    if not seduta:
        y0 = -h / 2
        for k, dx in (("sx", -0.06), ("dx", 0.06)):
            parti.append(P(id + "_gamba_" + k, "cilindro", "stoffa", abito if h > 0.95 else PELLE, x=dx, y=y0 + gamba / 2, sx=0.075, sy=gamba / 2, sz=0.075, **G))
            parti.append(P(id + "_piede_" + k, "cubo", "cuoio" if scarpe else "stoffa", "#2a1e16" if scarpe else "#8a6a52", x=dx, y=y0 + 0.025, z=-0.04, sx=0.08, sy=0.05, sz=0.15, **G))
        yb = y0 + gamba + busto_h / 2
    else:
        # seduto: cosce in avanti sul sedile (quota 0,45 nel mondo), gambe giu'
        ys = 0.45 - yc
        for k, dx in (("sx", -0.06), ("dx", 0.06)):
            parti.append(P(id + "_coscia_" + k, "cilindro", "stoffa", abito, x=dx, y=ys + 0.04, z=-0.14, rx=90, sx=0.08, sy=0.15, sz=0.08, **G))
            parti.append(P(id + "_gamba_" + k, "cilindro", "stoffa", PELLE, x=dx, y=ys - 0.12, z=-0.28, sx=0.07, sy=0.15, sz=0.07, **G))
            parti.append(P(id + "_piede_" + k, "cubo", "cuoio", "#2a1e16", x=dx, y=ys - 0.27, z=-0.32, sx=0.08, sy=0.05, sz=0.14, **G))
        yb = ys + busto_h / 2 + 0.05
    parti.append(P(id + "_busto", "cubo", "stoffa", abito, x=0, y=yb, sx=0.27, sy=busto_h, sz=0.15, **G))
    for k, dx in (("sx", -0.17), ("dx", 0.17)):
        parti.append(P(id + "_braccio_" + k, "cilindro", "stoffa", abito, x=dx, y=yb - 0.02, rz=(8 if dx > 0 else -8), sx=0.065, sy=busto_h * 0.55, sz=0.065, **G))
        parti.append(P(id + "_mano_" + k, "sfera", "stoffa", PELLE, x=dx * 1.08, y=yb - busto_h * 0.55, sx=0.06, sy=0.06, sz=0.06, **G))
    yt = yb + busto_h / 2 + 0.04 + testa / 2
    parti.append(P(id + "_collo", "cilindro", "stoffa", PELLE, y=yb + busto_h / 2 + 0.02, sx=0.07, sy=0.03, sz=0.07, **G))
    parti.append(P(id + "_testa", "sfera", "stoffa", PELLE, y=yt, sx=testa, sy=testa * 1.1, sz=testa, **G))
    parti.append(P(id + "_capelli", "sfera", "stoffa", capelli, y=yt + 0.03, z=0.02, sx=testa * 1.06, sy=testa * 0.85, sz=testa * 1.05, **G))
    if trecce:
        for k, dx in (("sx", -0.08), ("dx", 0.08)):
            parti.append(P(id + "_treccia_" + k, "cilindro", "stoffa", capelli, x=dx, y=yt - 0.1, z=0.03, sx=0.035, sy=0.08, sz=0.035, **G))
    if berretto:
        parti.append(P(id + "_berretto", "cilindro", "stoffa", "#3c3a36", y=yt + testa * 0.42, z=0.0, sx=testa * 1.12, sy=0.025, sz=testa * 1.12, **G))
        parti.append(P(id + "_visiera", "cubo", "stoffa", "#2e2c29", y=yt + testa * 0.36, z=-testa * 0.55, sx=testa * 0.7, sy=0.012, sz=0.07, **G))
    if oggetto == "bambola":
        parti.append(P(id + "_bambola", "capsula", "stoffa", "#b8866a", x=0.19, y=yb - busto_h * 0.45, z=-0.06, sx=0.07, sy=0.07, sz=0.06, **G))
        parti.append(P(id + "_bambola_testa", "sfera", "stoffa", "#d9c2a0", x=0.19, y=yb - busto_h * 0.45 + 0.09, z=-0.06, sx=0.05, sy=0.05, sz=0.05, **G))
    elif oggetto == "trottola":
        parti.append(P(id + "_trottola", "sfera", "legno", "#3c5c8c", x=-0.19, y=yb - busto_h * 0.5, z=-0.07, sx=0.07, sy=0.07, sz=0.07, **G))
        parti.append(P(id + "_trottola_punta", "cilindro", "metallo", "#303030", x=-0.19, y=yb - busto_h * 0.5 - 0.045, z=-0.07, sx=0.012, sy=0.012, sz=0.012, **G))
    elif oggetto == "fischietto":
        parti.append(P(id + "_fischietto", "cilindro", "metallo", "#9a9a92", x=0.18, y=yb - busto_h * 0.5, z=-0.07, rz=90, sx=0.022, sy=0.04, sz=0.022, **G))
    elif oggetto == "fazzoletto":
        parti.append(P(id + "_fazzoletto", "cubo", "stoffa", "#e0d8c4", x=0.12, y=yb - 0.02, z=-0.085, sx=0.08, sy=0.06, sz=0.01, **G))
    collo = round(yb + busto_h * 0.22, 3)          # dove si appende il cartellino (sotto il mento)
    busto = round(yb - busto_h * 0.08, 3)          # centro del cappotto indossato
    return parti, collo, busto


# i quattro bambini della fila (C1 e C2): numero del cartellino, eta', aspetto
FILA = [
    # id, nome, numero, eta' (testo), altezza, abito, capelli, scarpe, trecce, berretto, oggetto, taglia
    ("pasqualino", "Pasqualino", "115", "5 anni", 0.86, "#6a5a48", "#2e241c", True, False, False, "fischietto", 1),
    ("mariuccia", "Mariuccia", "113", "6 anni", 0.93, "#7a5a48", "#3a2a1c", True, True, False, "bambola", 2),
    ("tommasino", "Tommasino", "114", "8 anni", 1.08, "#4a5a6a", "#2a2018", True, False, True, "trottola", 4),
    ("amerigo", "Amerigo", "112", "7 anni, quasi 8", 1.0, "#5a4a3a", CAPELLI, False, False, False, None, 3),
]
MSG_FIG = {
    "pasqualino": "Il piu' piccolo della fila: stringe in mano un fischietto di latta e non lo molla.",
    "mariuccia": "Mariuccia, con le trecce, tiene stretta una bambola di pezza.",
    "tommasino": "Tommasino e' il piu' alto della fila: berretto in testa e una trottola che gira fra le dita.",
    "amerigo": "Il tuo riflesso nel vetro della biglietteria: scalzo, capelli castani, la salopette rattoppata.",
}


def fila(xs, zf, riflesso_x, nascosta=False, gruppo=""):
    """Le tre figure a sinistra e il riflesso di Amerigo nella cornice del vetro della biglietteria, a destra. Ritorna (parti, figure)."""
    parti, figure = [], []
    for i, (id, nome, num, eta, h, abito, cap, scarpe, trecce, berr, ogg, taglia) in enumerate(FILA):
        x = xs[i] if id != "amerigo" else riflesso_x
        z = zf if id != "amerigo" else 0.18
        pp, collo, busto = figura("fig_" + id, i, x, z, h, abito, cap, MSG_FIG[id], scarpe=scarpe, trecce=trecce, berretto=berr, oggetto=ogg, nascosta=nascosta, gruppo=gruppo)
        parti += pp
        figure.append({"id": "fig_" + id, "nome": nome, "collo": collo, "busto": busto, "taglia": taglia})
    # la cornice del vetro della biglietteria (senza vetro: il riflesso si vede)
    g = dict(nascosta=True, gruppo=gruppo) if nascosta else {}
    rx = riflesso_x
    parti += [
        P("vetro_stipite_sx", "cubo", "legno", "#3d2c1e", x=rx - 0.36, y=0.75, z=-0.02, sx=0.06, sy=1.5, sz=0.08, **g),
        P("vetro_stipite_dx", "cubo", "legno", "#3d2c1e", x=rx + 0.36, y=0.75, z=-0.02, sx=0.06, sy=1.5, sz=0.08, **g),
        P("vetro_traverso", "cubo", "legno", "#3d2c1e", x=rx, y=1.47, z=-0.02, sx=0.78, sy=0.06, sz=0.08, **g),
        P("vetro_davanzale", "cubo", "legno", "#3d2c1e", x=rx, y=0.03, z=-0.02, sx=0.78, sy=0.06, sz=0.12, **g),
        P("vetro_fondo", "cubo", "legno", "#22252a", x=rx, y=0.75, z=0.42, sx=0.72, sy=1.44, sz=0.02, **g),
        T("vetro_scritta", "BIGLIETTI", rx, 1.39, -0.07, 0.6, 0.08, 0.04, "#d8cba6", **g),
    ]
    return parti, figure


# ---------------------------------------------------------------- C1 i cartellini scambiati
REGISTRO = [
    ("112", "N. 112 - Speranza Amerigo. Anni 7, quasi 8. Quartieri Spagnoli. Segni: scalzo, capelli castani."),
    ("113", "N. 113 - Mariuccia (il cognome e' sotto una macchia d'inchiostro). Anni 6. Quartieri Spagnoli. Segni: trecce, una bambola di pezza."),
    ("114", "N. 114 - Tommasino (cognome coperto dal timbro). Anni 8. Quartieri Spagnoli. Segni: il piu' alto della fila, berretto, una trottola."),
    ("115", "N. 115 - Iovine Pasqualino. Anni 5. Rione Sanita'. Segni: il piu' piccolo, un fischietto di latta."),
]


def c1():
    xs = [-1.85, -1.4, -0.92]
    parti, figure = fila(xs, -0.15, 1.35)
    parti += tavolo(1.4, 0.75, 0.8, cz=0.2)
    parti += [
        P("tovaglia", "cubo", "stoffa", "#cfc6ae", y=0.803, z=0.2, sx=1.36, sy=0.006, sz=0.71),
        P("registro_copertina", "cubo", "cuoio", "#4a2a22", x=-0.25, y=0.81, z=0.28, sx=0.62, sy=0.014, sz=0.42),
        P("registro_pagine", "cubo", "carta", "#d8cba6", x=-0.25, y=0.82, z=0.28, sx=0.58, sy=0.01, sz=0.38),
        P("calamaio", "cilindro", "vetro", "#1d1a18", x=0.38, y=0.84, z=0.42, sx=0.06, sy=0.04, sz=0.06),
        P("penna", "cilindro", "legno", "#2a1e16", x=0.3, y=0.812, z=0.4, rz=90, ry=20, sx=0.01, sy=0.08, sz=0.01),
        P("timbro_comitato", "cubo", "legno", "#6a4528", x=0.52, y=0.83, z=0.3, sx=0.07, sy=0.05, sz=0.05),
    ]
    for k, (num, riga) in enumerate(REGISTRO):
        parti.append(P("riga_%s" % num, "cubo", "carta", "#e3d9bd", x=-0.25, y=0.83, z=round(0.42 - k * 0.085, 3), rx=90, sx=0.54, sy=0.07, sz=0.004,
                       testo="N. %s ................" % num, dimensione=0.018, font="mano", clic=True, azione="leggi", msg=riga))
    # i cartellini, bagnati dal vento: si legge solo il numero
    carte = [("113", 0.12, -0.02, 14), ("115", 0.3, 0.06, -9), ("112", 0.47, -0.04, 22), ("114", 0.6, 0.12, -17)]
    for num, x, z, ry in carte:
        parti.append(P("cartellino_%s" % num, "cubo", "carta", "#e6dcc0", x=x, y=0.81, z=z, rx=90, ry=ry, sx=0.12, sy=0.08, sz=0.006,
                       testo="N. %s" % num, dimensione=0.026, clic=True, azione="cartellino", msg="Un cartellino con lo spago, umido di pioggia: si legge solo il numero, %s." % num))
    for f, (id, nome, num, *_r) in zip(figure, FILA):
        f["cartellino"] = "cartellino_%s" % num
    return {
        "id": "enigma_cartellini", "tipo": "quiz", "meccanismo": "cartellini", "titolo": "I cartellini scambiati", "scena": STAZ,
        "consegna": "Una folata dal binario ha fatto volare i cartellini da appendere al collo dei bambini. Si legge solo il numero. Sul tavolo del Comitato c'e' il registro: numero, eta', quartiere e segni di ognuno. Tocca un cartellino, poi il bambino a cui appartiene. Il tuo lo appendi davanti al vetro della biglietteria, dove vedi il tuo riflesso.",
        "temaCivico": "Ogni bambino ha un nome e un'identita': oggi la Convenzione ONU sui diritti dell'infanzia (1989, art. 8) dice che ogni bambino ha diritto alla propria identita', e la Costituzione (art. 3) che tutti hanno pari dignita'.",
        "indizi": ["Il registro dice piu' del nome: eta', quartiere e segni particolari. Tocca le righe per leggerle.",
                   "Guarda l'eta' (chi e' il piu' piccolo, chi il piu' alto) e che cosa tiene in mano ognuno: fischietto, bambola, trottola. E chi e' scalzo?",
                   "112 al tuo riflesso (scalzo, capelli castani); 113 a Mariuccia (trecce e bambola); 114 a Tommasino (il piu' alto, berretto e trottola); 115 a Pasqualino (il piu' piccolo, fischietto)."],
        "spiegazione": "Quattro cartellini, quattro bambini. La volontaria ricontrolla il registro, annuisce e ti stringe lo spago dietro il collo: «Centododici. Non lo perdere, che senza numero il cappotto non te lo do.»",
        "erroreMsg": "Qualche cartellino non e' al collo giusto: rileggi il registro e guarda bene i bambini.",
        "scheda": "Prima della partenza i bambini venivano lavati, visitati, vaccinati, vestiti e schedati uno per uno, con un cartellino di riconoscimento. I numeri e il bambino della Sanita' sono inventati per il gioco; Mariuccia e Tommasino partono con Amerigo anche nel romanzo.",
        "appunto": "Il cartellino di Amerigo: numero 112.",
        "x": POS_C1[0], "y": POS_C1[1], "z": POS_C1[2], "yaw": 0, "scala": 1, "distanza": 2.2, "inclina": 16,
        "prompt": "il registro del Comitato",
        "produce": ["numero_112"],
        "figure": figure,
        "strumenti": [
            "Ora tocca il bambino a cui appendere questo cartellino.",
            "Prima prendi un cartellino dal tavolo, poi tocca il bambino.",
            "{0} cartellini non sono al collo giusto. Rileggi il registro e guarda bene i bambini.",
            "Il cartellino e' al collo di {0}.",
        ],
        "parti": parti,
        "domande": [
            quiz("Di chi e' il cartellino 115?", ["Del bambino piu' piccolo, con il fischietto", "Di Tommasino", "Di Mariuccia", "Di Amerigo"], 0, "Il registro dice: Pasqualino, 5 anni, il piu' piccolo, un fischietto di latta."),
            quiz("Di chi e' il cartellino 113?", ["Della bambina con le trecce e la bambola", "Del bambino con il berretto", "Di Amerigo", "Di Pasqualino"], 0, "Mariuccia: trecce e bambola di pezza."),
            quiz("Di chi e' il cartellino 114?", ["Del piu' alto, con berretto e trottola", "Della bambina con la bambola", "Del piu' piccolo", "Di nessuno"], 0, "Tommasino, 8 anni, il piu' alto della fila."),
            quiz("Qual e' il numero di Amerigo?", ["112", "113", "114", "115"], 0, "Scalzo, capelli castani, 7 anni quasi 8: e' il 112."),
        ],
    }


# ---------------------------------------------------------------- C2 i cappotti della Croce Rossa
COLORI_CAPPOTTO = {"a": "#5b5348", "b": "#3f4a57", "c": "#5a4632", "d": "#4b4f3c", "e": "#6a5a4a", "f": "#3a3a3c"}
CAPPOTTI = [
    # id, taglia, talloncino, colore, x, z (sul tavolo), toppa, msg
    ("cappotto_a", 1, "CR 31", "a", -0.83, -0.2, False, "Un cappottino minuscolo, con due bottoni d'osso. Talloncino: CR 31."),
    ("cappotto_b", 3, "112", "b", -0.28, -0.2, True, "Un cappotto blu con una toppa sul gomito. Sul talloncino, a matita: 112."),
    ("cappotto_c", 2, "CR 58", "c", 0.27, -0.2, False, "Un cappotto marrone, maniche corte, quattro bottoni. Talloncino: CR 58."),
    ("cappotto_d", 4, "CR 12", "d", 0.83, -0.2, True, "Un cappotto verde scuro, lungo, con una toppa sulla spalla. Talloncino: CR 12."),
    ("cappotto_e", 3, "CR 47", "e", -0.4, 0.3, False, "Un cappotto color nocciola, della stessa misura del blu. Talloncino: CR 47."),
]
ADULTO = ("cappotto_f", 7, "CR 2", "f", "Un cappotto da uomo, pesante, appeso all'attaccapanni. Talloncino: CR 2.")


def cappotto(id, taglia, numero, col, x, y, z, toppa, msg, sdraiato=True):
    w, hh = 0.24 + 0.035 * taglia, 0.30 + 0.05 * taglia
    g = dict(clic=True, azione="cappotto", valore=taglia, msg=msg)
    rot = dict(rx=90) if sdraiato else {}
    parti = [P(id, "vuoto", "stoffa", "", x=x, y=y, z=z, sx=w + 0.14, sy=hh, sz=0.1, **rot, **g)]
    G = dict(genitore=id)
    c = COLORI_CAPPOTTO[col]
    parti += [
        P(id + "_corpo", "cubo", "stoffa", c, sx=w, sy=hh, sz=0.085, **G),
        P(id + "_bavero", "cubo", "stoffa", c, y=hh / 2 - 0.03, z=-0.046, sx=w * 0.6, sy=0.05, sz=0.01, **G),
        P(id + "_manica_sx", "cilindro", "stoffa", c, x=-w / 2 - 0.035, y=0.02, rz=-14, sx=0.07, sy=hh * 0.4, sz=0.07, **G),
        P(id + "_manica_dx", "cilindro", "stoffa", c, x=w / 2 + 0.035, y=0.02, rz=14, sx=0.07, sy=hh * 0.4, sz=0.07, **G),
        P(id + "_talloncino", "cubo", "carta", "#e3d9bd", x=w * 0.28, y=hh * 0.3, z=-0.046, sx=0.07, sy=0.04, sz=0.004, testo=numero, dimensione=0.014, **G),
    ]
    nb = 2 if taglia == 1 else 4 if taglia == 2 else 3
    for k in range(nb):
        parti.append(P(id + "_bottone_%d" % k, "sfera", "legno", "#2a2018", x=-0.02, y=hh * 0.3 - k * hh * 0.6 / max(1, nb - 1), z=-0.046, sx=0.022, sy=0.022, sz=0.01, **G))
    if toppa:
        parti.append(P(id + "_toppa", "cubo", "stoffa", "#7a6a52", x=-w * 0.3, y=-hh * 0.15, z=-0.045, sx=0.07, sy=0.06, sz=0.005, **G))
    return parti


def c2():
    xs = [-2.65, -2.2, -1.72]
    parti, figure = fila(xs, -0.25, 2.25, nascosta=True, gruppo="vista")   # nel mondo i bambini sono alla fila del Comitato
    parti += tavolo(2.2, 1.2, 0.8, "#6a5844", "#4a3a2a")
    parti += [P("telo_croce", "cubo", "stoffa", "#e6e0d0", x=0.0, y=0.803, z=0.48, sx=0.5, sy=0.004, sz=0.2),
              P("croce_v", "cubo", "stoffa", "#a3261c", x=0.0, y=0.806, z=0.48, sx=0.05, sy=0.003, sz=0.15),
              P("croce_o", "cubo", "stoffa", "#a3261c", x=0.0, y=0.806, z=0.48, sx=0.15, sy=0.003, sz=0.05)]
    for id, t, num, col, x, z, toppa, msg in CAPPOTTI:
        parti += cappotto(id, t, num, col, x, 0.85, z, toppa, msg)
    # l'attaccapanni con il cappotto da uomo
    idf, tf, numf, colf, msgf = ADULTO
    parti += [P("attaccapanni_piede", "cilindro", "legno", "#3a2a1a", x=1.55, y=0.02, sx=0.4, sy=0.02, sz=0.4),
              P("attaccapanni_asta", "cilindro", "legno", "#3a2a1a", x=1.55, y=0.85, sx=0.05, sy=0.85, sz=0.05),
              P("attaccapanni_gancio", "cubo", "legno", "#3a2a1a", x=1.55, y=1.66, z=-0.06, sx=0.04, sy=0.04, sz=0.12)]
    parti += cappotto(idf, tf, numf, colf, 1.55, 1.25, -0.1, False, msgf, sdraiato=False)
    # la scatola delle scarpe: compare quando Amerigo ha il suo cappotto (nessun biglietto: non si dice chi le manda)
    parti += [
        P("scatola_scarpe", "cubo", "carta", "#a8956c", x=2.25, y=0.06, z=-0.3, sx=0.34, sy=0.12, sz=0.2, nascosta=True),
        P("scatola_coperchio", "cubo", "carta", "#9a875e", x=2.25, y=0.05, z=-0.55, ry=12, sx=0.36, sy=0.02, sz=0.22, nascosta=True),
        P("scarpa_nuova_sx", "scarpa", "cuoio", "#3a2418", x=2.18, y=0.125, z=-0.3, ry=-90, sx=0.75, sy=0.75, sz=0.75, testo="lucide", nascosta=True),
        P("scarpa_nuova_dx", "scarpa", "cuoio", "#3a2418", x=2.32, y=0.125, z=-0.3, ry=-90, sx=0.75, sy=0.75, sz=0.75, testo="lucide", nascosta=True),
    ]
    ok = {
        "fig_pasqualino": "Il cappottino ti sta a pennello, Pasqua'. Lui soffia nel fischietto per dire grazie.",
        "fig_mariuccia": "Mariuccia infila le braccia nel cappotto marrone e ci mette dentro anche la bambola.",
        "fig_tommasino": "Tommasino si guarda il cappotto lungo e fa il gesto del generale. «Mo' sono un signore.»",
        "fig_amerigo": "Nel vetro un bambino con un cappotto blu, la toppa sul gomito e il numero 112. Sotto il mucchio spunta una scatola di cartone: dentro, un paio di scarpe nuove della tua misura. Nessun biglietto.",
    }
    giusti = {"fig_pasqualino": "cappotto_a", "fig_mariuccia": "cappotto_c", "fig_tommasino": "cappotto_d", "fig_amerigo": "cappotto_b"}
    for f in figure:
        f["cappotto"] = giusti[f["id"]]
        f["ok"] = ok[f["id"]]
        if f["id"] == "fig_amerigo":
            f["dopo"] = "scatola_scarpe,scatola_coperchio,scarpa_nuova_sx,scarpa_nuova_dx"
    return {
        "id": "enigma_cappotti", "tipo": "quiz", "meccanismo": "cappotti", "titolo": "I cappotti della Croce Rossa", "scena": STAZ,
        "consegna": "Sul tavolo della Croce Rossa c'e' un mucchio di cappotti usati, lavati e rammendati. A ogni bambino della fila va il suo: misura a occhio spalle e maniche. Tocca un cappotto, poi il bambino; il tuo lo provi davanti al vetro. Il tuo cappotto ha il tuo numero sul talloncino.",
        "temaCivico": "Solidarieta' organizzata: oggi l'art. 2 della Costituzione chiede a tutti i doveri inderogabili di solidarieta'; le associazioni di volontariato ne sono un esempio.",
        "indizi": ["Misura a occhio: spalle e maniche. Un cappotto troppo grande arriva alle ginocchia, uno troppo piccolo non si chiude.",
                   "Dal piu' piccolo al piu' grande: Pasqualino, Mariuccia, tu, Tommasino. Due cappotti sono della tua misura: il talloncino ha un numero.",
                   "Pasqualino: il cappottino con due bottoni. Mariuccia: il marrone. Tommasino: il verde lungo. Tu: il blu con la toppa e il numero 112. Il cappotto da uomo non e' per nessuno di voi."],
        "spiegazione": "Quattro bambini, quattro cappotti. Ti guardi nel vetro: un cappotto blu, il numero 112, e ai piedi un paio di scarpe vere, tutte tue. La volontaria non dice da dove vengono: «Camminaci piano, che sono nuove.»",
        "erroreMsg": "Non e' il cappotto giusto per questo bambino: guarda bene la misura e il talloncino.",
        "scheda": "I vestiti per il viaggio venivano in gran parte dalla Croce Rossa (una sola fonte storica lo dice, quindi resta una formula prudente). I numeri dei talloncini sono inventati per il gioco. Chi desse le scarpe nuove ad Amerigo nel romanzo qui non si dice.",
        "appunto": "Un cappotto blu con il numero 112, e un paio di scarpe nuove.",
        "x": POS_C2[0], "y": POS_C2[1], "z": POS_C2[2], "yaw": 0, "scala": 1, "distanza": 2.0, "inclina": 24,
        "prompt": "il mucchio di cappotti",
        "richiede": ["numero_112"], "bloccatoMsg": "La volontaria della Croce Rossa ti ferma con una mano: «Prima il cartellino, guagliò. Senza numero non so qual e' il tuo.»",
        "produce": ["cappotto_112", "scarpe_nuove"],
        "cutscene": "cutscene_cappotto", "cutsceneCapitolo": "meta",
        "figure": figure,
        "strumenti": [
            "Ora tocca il bambino a cui farlo provare.",
            "Prima prendi un cappotto dal mucchio, poi tocca il bambino.",
            "Troppo grande: le maniche gli coprono le mani e l'orlo arriva alle caviglie. Prova un cappotto piu' piccolo.",
            "Troppo piccolo: non si chiude e le maniche arrivano al gomito. Prova un cappotto piu' grande.",
            "La misura e' giusta, ma questo cappotto aspetta un altro bambino: guarda il numero sul talloncino.",
            "Questo cappotto l'ha gia' trovato il suo bambino.",
        ],
        "parti": parti,
        "domande": [
            quiz("Come si sceglie il cappotto giusto per un bambino?", ["Dalla misura: spalle e maniche", "Dal colore piu' bello", "Si prende il piu' grande, cosi' dura", "A caso"], 0, "Un cappotto troppo grande o troppo piccolo non ripara dal freddo."),
            quiz("Due cappotti sono della misura di Amerigo: quale e' il suo?", ["Quello con il numero 112 sul talloncino", "Quello senza toppe", "Il piu' nuovo", "Nessuno dei due"], 0, "Il 112 e' il numero del suo cartellino."),
            quiz("Chi forniva in gran parte i vestiti per il viaggio?", ["La Croce Rossa", "Le ferrovie", "I bambini stessi", "Nessuno"], 0, "In gran parte la Croce Rossa, con le volontarie."),
        ],
    }


# ---------------------------------------------------------------- C3 la mela
def c3():
    parti = [
        P("cassa", "cubo", "legno", "#6a5034", y=0.22, sx=0.7, sy=0.44, sz=0.45),
        P("cassa_doga", "cubo", "legno", "#5a4228", y=0.22, z=-0.228, sx=0.7, sy=0.05, sz=0.01),
        # la valigia di cartone, aperta
        P("valigia", "cubo", "carta", "#8a6a44", x=-0.05, y=0.5, sx=0.52, sy=0.12, sz=0.34, clic=True, azione="usa", richiede="mela_annurca", evento="mela_in_valigia",
          dopo="mela_dentro", msg=""),
        P("valigia_coperchio", "cubo", "carta", "#7c5e3c", x=-0.05, y=0.68, z=0.2, rx=-70, sx=0.52, sy=0.34, sz=0.02),
        P("valigia_maglia", "cubo", "stoffa", "#6d6a5c", x=-0.12, y=0.565, z=0.02, sx=0.3, sy=0.02, sz=0.22),
        P("valigia_calze", "cubo", "stoffa", "#d0c6ae", x=0.11, y=0.566, z=0.05, sx=0.12, sy=0.02, sz=0.18),
        P("valigia_spago", "cubo", "stoffa", "#c9b98a", x=-0.05, y=0.5, z=-0.172, sx=0.54, sy=0.012, sz=0.006),
        P("mela_dentro", "sfera", "stoffa", "#8a2a1e", x=0.08, y=0.6, z=-0.06, sx=0.07, sy=0.065, sz=0.07, nascosta=True),
        # la mano di Antonietta (la manica nera dello scialle), con la mela annurca
        P("manica", "cilindro", "stoffa", "#1d1a18", x=0.62, y=0.78, z=-0.12, rz=78, sx=0.09, sy=0.22, sz=0.09),
        P("mano", "sfera", "stoffa", "#b98c6c", x=0.4, y=0.74, z=-0.12, sx=0.085, sy=0.07, sz=0.09),
        P("mela", "sfera", "stoffa", "#8a2a1e", x=0.36, y=0.79, z=-0.14, sx=0.075, sy=0.068, sz=0.075, clic=True, azione="prendi", produce="mela_annurca",
          msg="Antonietta ti mette in mano una mela annurca, piccola, rossa e dura. «Questa la tieni per il viaggio.» Le dita le tremano appena."),
        P("mela_picciolo", "cilindro", "legno", "#3a2a1a", x=0.36, y=0.83, z=-0.14, sx=0.006, sy=0.012, sz=0.006),
    ]
    return {
        "id": "enigma_mela", "tipo": "quiz", "meccanismo": "oggetto", "titolo": "La mela", "scena": STAZ,
        "consegna": "Sulla banchina, accanto alla carrozza, Antonietta ti aspetta. Ha qualcosa in mano. Prendilo, poi mettilo nella valigia di cartone.",
        "temaCivico": "Legami familiari: la Costituzione (art. 30) riconosce il dovere e il diritto dei genitori di mantenere, istruire ed educare i figli. Anche partire per un po' puo' essere un modo di prendersene cura.",
        "indizi": ["Antonietta ha qualcosa in mano: toccalo.",
                   "Quello che ti da' va messo in valigia, per il viaggio.",
                   "Tocca la mela, poi la valigia aperta sulla cassa."],
        "spiegazione": "La mela e' in valigia, fra la maglia e le calze. Antonietta chiude il coperchio e lo lega con lo spago, due nodi stretti. Non dice niente. Il fischio del capotreno taglia l'aria: e' ora di salire.",
        "erroreMsg": "",
        "scheda": "Nel romanzo, al saluto, Antonietta da' ad Amerigo una mela annurca: qui e' solo un gesto, senza parole di troppo. Le parole di Antonietta sono scritte per il gioco.",
        "appunto": "Nella valigia: una mela annurca di mamma Antonietta.",
        "x": POS_C3[0], "y": POS_C3[1], "z": POS_C3[2], "yaw": 0, "scala": 1, "distanza": 1.3, "inclina": 20,
        "prompt": "la valigia di cartone",
        "richiede": ["scarpe_nuove"], "bloccatoMsg": "Antonietta ti guarda i piedi nudi e scuote la testa: «Prima il cappotto e le scarpe. Poi vieni a salutarmi.»",
        "produce": ["partenza"],
        "soluzione": ["mela_in_valigia"],
        "oggetti": [{"id": "mela_annurca", "nome": "La mela annurca", "descrizione": "Una mela piccola e rossa, dura, che profuma di cantina."}],
        "parti": parti,
        "domande": [
            quiz("Che cosa ti da' Antonietta alla partenza?", ["Una mela annurca", "Un biglietto da cento lire", "Una lettera", "Un berretto"], 0, "Una mela annurca, da tenere per il viaggio."),
            quiz("Dove la metti?", ["Nella valigia di cartone", "In tasca al controllore", "La mangi subito", "La lasci sulla banchina"], 0, "Nella valigia, fra la maglia e le calze."),
            quiz("Che cosa succede dopo il saluto?", ["Si sale sul treno", "Si torna a casa", "Si va a scuola", "Si aspetta il giorno dopo"], 0, "Il capotreno fischia: si parte."),
        ],
    }


# ---------------------------------------------------------------- C4 l'Italia dal finestrino
# solo le tappe confermate in Docs/VERIFICHE_STORICHE.md (Napoli, Roma, Firenze, Bologna, Modena); gli altri cartelli sono cartelli delle ferrovie, non citta'
FERMATE = [
    ("CASELLO 14", False, 0.0, "", ""),
    ("ROMA", True, 0.1, "pin_roma,nome_roma,tratto_1", "Il controllore passa e batte sul vetro: «Roma, guagliu'! Avete visto il Colosseo? No? Pazienza, segnatela lo stesso.»"),
    ("RIFORNIMENTO ACQUA", False, 0.2, "", ""),
    ("PASSAGGIO A LIVELLO", False, 0.35, "", ""),
    ("FIRENZE", True, 0.45, "pin_firenze,nome_firenze,tratto_2", "Il controllore: «Firenze l'abbiamo passata adesso. Le donne del comitato vi hanno portato il latte, ve lo ricordate?» Segnala pure."),
    ("GALLERIA", False, 0.55, "", ""),
    ("DEPOSITO LOCOMOTIVE", False, 0.7, "", ""),
    ("BOLOGNA", True, 0.85, "pin_bologna,nome_bologna,tratto_3,tratto_4", "Il controllore guarda l'orologio: «Bologna. E' gia' notte, eh? Scrivetela, che la prossima e' la vostra.»"),
    ("SCALO MERCI", False, 0.95, "", ""),
]


def c4():
    TAB = 1.5   # centro della carta
    parti = [
        # parete di testa (solo nella vista: nel mondo c'e' la parete vera) e finestrino a sinistra
        P("parete_carta", "cubo", "legno", "#5a4630", x=0.05, y=1.4, z=0.04, sx=1.1, sy=2.0, sz=0.03, nascosta=True, gruppo="vista"),
        P("parete_sx", "cubo", "legno", "#5a4630", x=-1.85, y=1.4, z=0.04, sx=0.8, sy=2.0, sz=0.03, nascosta=True, gruppo="vista"),
        P("parete_sotto", "cubo", "legno", "#5a4630", x=-1.0, y=0.72, z=0.04, sx=0.9, sy=0.64, sz=0.03, nascosta=True, gruppo="vista"),
        P("parete_sopra", "cubo", "legno", "#5a4630", x=-1.0, y=2.12, z=0.04, sx=0.9, sy=0.56, sz=0.03, nascosta=True, gruppo="vista"),
        P("finestra_davanzale", "cubo", "legno", "#3d2c1e", x=-1.0, y=1.06, z=-0.02, sx=0.92, sy=0.04, sz=0.1, nascosta=True, gruppo="vista"),
        P("finestra_stipite_sx", "cubo", "legno", "#3d2c1e", x=-1.43, y=1.42, z=-0.01, sx=0.05, sy=0.72, sz=0.06, nascosta=True, gruppo="vista"),
        P("finestra_stipite_dx", "cubo", "legno", "#3d2c1e", x=-0.57, y=1.42, z=-0.01, sx=0.05, sy=0.72, sz=0.06, nascosta=True, gruppo="vista"),
        P("finestra_traverso", "cubo", "legno", "#3d2c1e", x=-1.0, y=1.78, z=-0.01, sx=0.92, sy=0.05, sz=0.06, nascosta=True, gruppo="vista"),
        P("vano", "vuoto", "legno", "", x=-1.0, y=1.42, z=0.04, sx=0.82, sy=0.68, sz=0.02, nascosta=True, gruppo="vista"),
        # la carta ferroviaria (cartone su listelli), con la linea e le citta' da appuntare
        P("carta", "cubo", "carta", "#dccfae", x=0.05, y=TAB, z=0.0, sx=0.92, sy=0.68, sz=0.012),
        P("carta_cornice_a", "cubo", "legno", "#4a3220", x=0.05, y=TAB + 0.355, z=-0.004, sx=0.96, sy=0.03, sz=0.02),
        P("carta_cornice_b", "cubo", "legno", "#4a3220", x=0.05, y=TAB - 0.355, z=-0.004, sx=0.96, sy=0.03, sz=0.02),
        T("carta_titolo", "LA LINEA DEL TRENO", -0.12, TAB + 0.3, -0.008, 0.5, 0.05, 0.025, "#2a2018"),
        P("mare", "cubo", "carta", "#b9c2bd", x=-0.25, y=TAB - 0.17, z=-0.007, ry=0, rz=-28, sx=0.35, sy=0.22, sz=0.002),
    ]
    # le citta' sulla carta: Napoli in basso a destra, Modena in alto a sinistra (fronte = -z: nella carta la x verso sinistra e' il nord-ovest)
    citta = [("napoli", 0.3, TAB - 0.24), ("roma", 0.12, TAB - 0.08), ("firenze", 0.02, TAB + 0.07), ("bologna", 0.08, TAB + 0.16), ("modena", -0.1, TAB + 0.2)]
    for i, (c, x, y) in enumerate(citta):
        fissa = c in ("napoli", "modena")
        parti.append(P("punto_" + c, "cilindro", "metallo", "#2a2018" if fissa else "#8a7a5a", x=x, y=y, z=-0.008, rx=90, sx=0.026, sy=0.002, sz=0.026))
        if fissa:
            g = dict(clic=True, azione="arrivo") if c == "modena" else {}
            parti.append(T("nome_" + c, c.upper(), x + (0.13 if c == "napoli" else -0.13), y, -0.01, 0.22, 0.04, 0.022, "#2a2018", **g))
        else:
            parti.append(T("casella_" + c, "?", x - 0.06, y, -0.01, 0.06, 0.04, 0.022, "#6a5a45", "mano"))
            parti.append(P("pin_" + c, "sfera", "metallo", "#a3261c", x=x, y=y, z=-0.02, sx=0.022, sy=0.022, sz=0.022, nascosta=True))
            parti.append(T("nome_" + c, c.upper(), x - 0.13, y, -0.012, 0.22, 0.04, 0.02, "#7a1f1a", "mano", nascosta=True))
        if i > 0:
            x0, y0 = citta[i - 1][1], citta[i - 1][2]
            dx, dy = x - x0, y - y0
            L = math.hypot(dx, dy)
            ang = math.degrees(math.atan2(dy, dx))
            parti.append(P("tratto_%d" % i, "cubo", "carta", "#7a1f1a", x=(x + x0) / 2, y=(y + y0) / 2, z=-0.009, rz=ang, sx=L, sy=0.006, sz=0.002,
                           nascosta=True))
    # i tratti rossi della linea compaiono man mano: Roma -> tratto 1, Firenze -> 2, Bologna -> 3 e 4 (fino a Modena)
    # l'orologio del controllore, appeso accanto alla carta
    parti += [
        P("orologio", "cilindro", "ottone", "#b59a3f", x=0.42, y=TAB + 0.22, z=-0.02, rx=90, sx=0.11, sy=0.012, sz=0.11),
        P("orologio_quadrante", "cilindro", "carta", "#efe6cc", x=0.42, y=TAB + 0.22, z=-0.028, rx=90, sx=0.09, sy=0.004, sz=0.09),
        P("orologio_lancetta", "cubo", "metallo", "#1d1a18", x=0.42, y=TAB + 0.235, z=-0.032, sx=0.004, sy=0.035, sz=0.002),
    ]
    fermate = [{"testo": t, "citta": c, "luce": l, "dopo": d, "ripete": r} for t, c, l, d, r in FERMATE]
    return {
        "id": "enigma_finestrino", "tipo": "quiz", "meccanismo": "finestrino", "titolo": "L'Italia dal finestrino", "scena": VAG,
        "consegna": "Il treno corre verso il Nord. Sulla parete c'e' la carta della linea: Napoli e Modena sono gia' stampate, in mezzo tre caselle vuote. Guarda i cartelli che passano nel finestrino: quando passa una citta', premi «Appunta» (o tocca il cartello). Se ne perdi una, il controllore te la ripete. Alla fine tocca Modena sulla carta.",
        "temaCivico": "Un'Italia sola: la Costituzione (art. 5) dice che la Repubblica e' una e indivisibile, e l'art. 3 che tutti sono uguali senza distinzioni. Il viaggio dei bambini del Sud verso il Nord fu anche un modo di conoscersi.",
        "indizi": ["Guarda i cartelli, non il paesaggio: non tutti i cartelli sono citta'.",
                   "La carta ha gia' Napoli e Modena: ti servono le tre citta' in mezzo, nell'ordine in cui passano.",
                   "In ordine: Roma, Firenze, Bologna. Poi tocca Modena sulla carta. Casello, rifornimento, passaggio a livello, galleria, deposito e scalo non sono citta'."],
        "spiegazione": "Roma, Firenze, Bologna: la linea rossa sulla carta arriva fino a Modena. Fuori e' buio; il controllore guarda l'orologio e sorride: «Ci siamo quasi, guagliu'. Mettetevi i cappotti.»",
        "erroreMsg": "Quello non e' il nome di una citta': e' un cartello delle ferrovie. Aspetta il prossimo.",
        "scheda": "Una cronaca dell'epoca racconta un treno di bambini napoletani che passa da Roma, si ferma a Firenze, arriva a Bologna di sera e la mattina dopo scende a Modena; in molte stazioni le donne dei comitati portavano latte, biscotti e piccoli doni. Le fermate intermedie e la durata del viaggio non sono certe: qui la linea e' semplificata [GIOCO].",
        "appunto": "Sulla carta: Napoli, Roma, Firenze, Bologna, Modena.",
        "x": POS_C4[0], "y": POS_C4[1], "z": POS_C4[2], "yaw": 0, "scala": 1, "distanza": 2.0, "inclina": 4,
        "prompt": "la carta della linea",
        "richiede": ["partenza"], "bloccatoMsg": "La carta della linea e' ancora arrotolata: prima bisogna partire.",
        "produce": ["quasi_arrivati"],
        "fermate": fermate, "periodo": 6.5,
        "strumenti": [
            "Appunta",
            "Appuntata sulla carta: {0}.",
            "Quello e' un cartello delle ferrovie, non una citta'. Aspetta il prossimo.",
            "Questa citta' l'hai gia' segnata, oppure non e' ancora il suo turno: guarda la carta.",
            "Il controllore: «Ne avete persa una, guagliu'. Segnatela pure.»",
            "Tutte le citta' in mezzo sono segnate. Manca solo l'ultima: tocca Modena sulla carta.",
            "Nel finestrino adesso non passa nessun cartello: aspetta il prossimo.",
            "Non ancora: prima segna le citta' che stanno in mezzo.",
        ],
        "parti": parti,
        "domande": [
            quiz("Quale citta' passa per prima dopo Napoli?", ["Roma", "Bologna", "Firenze", "Modena"], 0, "Da Napoli il treno sale verso Roma."),
            quiz("Quale ordine e' giusto?", ["Napoli, Roma, Firenze, Bologna, Modena", "Napoli, Firenze, Roma, Modena, Bologna", "Napoli, Bologna, Roma, Firenze, Modena", "Napoli, Modena, Bologna, Firenze, Roma"], 0, "Dal Sud al Nord: Roma, Firenze, Bologna, poi Modena."),
            quiz("«Rifornimento acqua» e «Casello 14» sono...", ["Cartelli delle ferrovie, non citta'", "Due citta' della Toscana", "Due paesi dell'Emilia", "Nomi di treni"], 0, "Servono ai ferrovieri: non sono fermate da segnare."),
        ],
    }


# ---------------------------------------------------------------- C5 i bambini che hanno paura
BAMBINI = [
    # id, nome, x, z (sedile), altezza, abito, capelli, trecce, oggetto, paura, ok, risposte (testo, giusta, richiede, esito), dopo
    ("peppino", "Peppino", -1.15, 0.55, 1.02, "#4b4f3c", "#2a2018", False, None,
     "Peppino, la fronte sul vetro: «Mia zia dice che ci portano in Russia. E che non torniamo piu'.»",
     "Peppino ripiega il volantino e se lo mette in tasca, dentro il cappotto. «Lo tengo io. Lo faccio leggere a mia zia.»",
     [("Mostragli il volantino: c'e' scritto quando si torna, e il registro l'hai visto tu.", True, "volantino",
       "Peppino legge piano, con il dito sotto le parole: «Ritorno... a fine scuola.» Alza gli occhi: «L'hai visto tu, il registro?» «Con questi occhi.»"),
      ("Fai una battuta: «In Russia? Con questo treno arriviamo a malapena a Caserta!»", False, "",
       "Peppino non ride. «Tu scherzi, ma mia zia l'ha detto seria.» Ha bisogno di qualcosa di piu' sicuro di una battuta."),
      ("Consolalo: «Non ci pensare, dormi un poco.»", False, "",
       "Peppino chiude gli occhi, ma li riapre subito. La paura e' ancora li': forse gli serve una prova, non una ninna nanna.")],
     "peppino_volantino"),
    ("rosetta", "Rosetta", -0.7, 0.55, 0.9, "#7a4a3e", "#3a2a1c", True, "fazzoletto",
     "Rosetta stringe un fazzoletto e piange senza rumore: «E se mamma si scorda di me?»",
     "Rosetta si soffia il naso nel fazzoletto ricamato e si appoggia alla tua spalla. Dopo un po' respira piano.",
     [("Mostrale il volantino: c'e' scritto che le lettere arrivano.", False, "volantino",
       "Rosetta guarda il foglio e scuote la testa: le lettere le sa leggere la mamma? Non e' una carta che le serve adesso."),
      ("Consolala: «Quel fazzoletto l'ha ricamato lei. Una mamma che ricama il tuo nome non se lo scorda.»", True, "",
       "Rosetta guarda le lettere storte ricamate sull'angolo. «R. E' la mia R.» Smette di piangere."),
      ("Fai una battuta: «Ti scorda? Con questo naso rosso ti trova pure al buio!»", False, "",
       "Rosetta ride mezzo secondo, poi riprende a piangere. Ha bisogno di sentirsi pensata, non presa in giro.")],
     "rosetta_fazzoletto_su"),
    ("gaetano", "Gaetano", 0.95, 0.55, 1.05, "#3f4a57", "#2e241c", False, None,
     "Gaetano batte i denti apposta: «Al Nord fa un freddo che si gelano le orecchie. Ce le perdiamo per strada, le orecchie.»",
     "Gaetano si tira il bavero fin sopra le orecchie e fa il pinguino per tutto lo scompartimento.",
     [("Consolalo: «Non avere paura, andra' tutto bene.»", False, "",
       "Gaetano alza le spalle: «Lo dicono tutti.» Lui fa il duro: forse gli serve una risata, non una carezza."),
      ("Mostragli il volantino.", False, "volantino",
       "Gaetano lo gira e lo rigira: «E qua dove sta scritto il freddo?» Il volantino non parla di orecchie."),
      ("Fai una battuta: «Con questi cappotti? Siamo piu' imbottiti di una pastiera. Le orecchie le teniamo nel bavero.»", True, "",
       "Gaetano scoppia a ridere e per far vedere si infila tutto nel cappotto, testa compresa.")],
     "gaetano_bavero"),
]


def c5():
    parti = [
        # nella vista: le panche dello scompartimento e il finestrino (nel mondo ci sono quelle vere della carrozza)
        P("panca_seduta_sx", "cubo", "legno", "#7a5a38", x=-0.92, y=0.43, z=0.6, sx=0.95, sy=0.05, sz=0.45, nascosta=True, gruppo="vista"),
        P("panca_schienale_sx", "cubo", "legno", "#6a4c2e", x=-0.92, y=0.8, z=0.86, sx=0.95, sy=0.7, sz=0.05, nascosta=True, gruppo="vista"),
        P("panca_seduta_dx", "cubo", "legno", "#7a5a38", x=0.92, y=0.43, z=0.6, sx=0.95, sy=0.05, sz=0.45, nascosta=True, gruppo="vista"),
        P("panca_schienale_dx", "cubo", "legno", "#6a4c2e", x=0.92, y=0.8, z=0.86, sx=0.95, sy=0.7, sz=0.05, nascosta=True, gruppo="vista"),
        P("pavimento", "cubo", "legno", "#4a3a2a", y=-0.01, z=0.4, sx=2.8, sy=0.02, sz=1.6, nascosta=True, gruppo="vista"),
    ]
    figure = []
    rnd = random.Random(14)
    for i, (id, nome, x, z, h, abito, cap, trecce, ogg, paura, ok, risposte, dopo) in enumerate(BAMBINI):
        pp, _c, _b = figura("fig_" + id, i, x, z, h, abito, cap, paura.split(":")[0] + ".", trecce=trecce, oggetto=ogg, seduta=True)
        parti += pp
        rr = [{"testo": t, "giusta": g, "richiede": rq, "esito": e} for t, g, rq, e in risposte]
        rnd.shuffle(rr)
        figure.append({"id": "fig_" + id, "nome": nome, "paura": paura, "ok": ok, "dopo": dopo, "risposte": rr})
    # i segni della calma (compaiono quando il bambino si e' calmato)
    parti.append(P("peppino_volantino", "cubo", "carta", "#d8c89c", x=-1.15 + 0.12, y=0.84, z=0.47, rx=-20, sx=0.1, sy=0.14, sz=0.004, nascosta=True))
    parti.append(P("rosetta_fazzoletto_su", "cubo", "stoffa", "#e0d8c4", x=-0.7 + 0.04, y=1.02, z=0.46, sx=0.09, sy=0.07, sz=0.01, nascosta=True))
    parti.append(P("gaetano_bavero", "cubo", "stoffa", "#3f4a57", x=0.95, y=1.18, z=0.5, sx=0.26, sy=0.1, sz=0.12, nascosta=True))
    return {
        "id": "enigma_paura", "tipo": "quiz", "meccanismo": "dialogo", "titolo": "I bambini che hanno paura", "scena": VAG,
        "consegna": "Nello scompartimento tre bambini non dormono: hanno paura, ognuno la sua. Tocca un bambino per sentire che cosa lo spaventa, poi scegli come rispondere (pulsanti in alto o tasti 1, 2, 3). Puoi consolare, scherzare o mostrare il volantino. Una risposta che non aiuta non e' un errore: prova un altro modo.",
        "temaCivico": "Solidarieta' tra pari e informazione corretta: aiutare chi ha paura vuol dire ascoltarlo, e a una diceria si risponde con una prova (artt. 2 e 21 della Costituzione).",
        "indizi": ["Ognuno ha una paura diversa: ascoltala bene prima di rispondere.",
                   "Il volantino risponde a una sola di quelle paure: quella delle dicerie. Le altre chiedono una carezza o una risata.",
                   "Peppino (Russia, non tornare): il volantino. Rosetta (la mamma che si scorda): il fazzoletto ricamato, consolala. Gaetano (il freddo): la battuta della pastiera."],
        "spiegazione": "Tre bambini si sono addormentati uno addosso all'altro, dentro i cappotti. Anche tu chiudi gli occhi: il treno rallenta, e dal finestrino si vedono lanterne e persone che aspettano.",
        "erroreMsg": "",
        "scheda": "Sui treni le paure erano tante: le dicerie del quartiere, la nostalgia, il freddo del Nord. I bambini dello scompartimento sono inventati per il gioco; le dicerie sono quelle del ricordo del volantino.",
        "appunto": "Peppino, Rosetta e Gaetano dormono. Siamo arrivati.",
        "x": POS_C5[0], "y": POS_C5[1], "z": POS_C5[2], "yaw": 0, "scala": 1, "distanza": 1.8, "inclina": 10,
        "prompt": "i bambini dello scompartimento",
        "richiede": ["quasi_arrivati"], "bloccatoMsg": "I bambini guardano fuori dal finestrino, zitti. Prima guarda anche tu: segna sulla carta le citta' che passano.",
        "produce": ["bambini_calmi"],
        "figure": figure,
        "strumenti": [
            "Rispondi",
            "Prova un altro modo.",
            "Non hai niente da mostrare: il volantino e' nella latta?",
            "Rimane qualcun altro da tranquillizzare.",
        ],
        "parti": parti,
        "domande": [
            quiz("Peppino ha paura che li portino in Russia e che non tornino. Che cosa lo aiuta?", ["Il volantino: c'e' scritto quando si torna", "Una battuta", "Dirgli di dormire", "Niente"], 0, "A una diceria si risponde con una prova."),
            quiz("Rosetta ha paura che la mamma la dimentichi. Che cosa la aiuta?", ["Il fazzoletto ricamato dalla mamma: consolarla", "Il volantino", "Prenderla in giro", "Ignorarla"], 0, "Le serve sentirsi pensata."),
            quiz("Gaetano fa il duro e ha paura del freddo. Che cosa lo aiuta?", ["Una battuta sui cappotti", "Il volantino", "Dirgli che andra' tutto bene", "Niente"], 0, "Una risata scioglie la paura di chi fa il duro."),
        ],
    }


# i testi qui sopra sono scritti con l'apostrofo al posto dell'accento (e' -> è); nel JSON vanno gli accenti veri
NON_ACCENTI = {"Pasqua'", "Mo'", "mo'", "po'"}
ACUTE = ("perch", "poich")


def accento(m):
    parola = m.group(1) + m.group(2) + "'"
    if parola in NON_ACCENTI:
        return parola
    v = m.group(2)
    if v == "e" and (m.group(1).endswith(ACUTE) or m.group(1) in ("n", "N")):
        return m.group(1) + "é"
    return m.group(1) + {"a": "à", "e": "è", "i": "ì", "o": "ò", "u": "ù", "E": "È", "A": "À"}.get(v, v + "'")


def accenti(x):
    if isinstance(x, str):
        return re.sub(r"\b(\w*?)([aeiouAE])'(?=$|[\s.,;:!?»)\"])", accento, x)
    if isinstance(x, list):
        return [accenti(v) for v in x]
    if isinstance(x, dict):
        return {k: accenti(v) for k, v in x.items()}
    return x


def main():
    fb = {
        "id": "flashback_cappotto", "oggetto": "cappotto", "titolo": "Il cappotto", "scena": STAZ,
        "introduzione": "Fumo, fischi, odore di carbone e di sapone da bucato. La stazione e' piena di madri e di bambini lavati di fresco, con i capelli ancora bagnati. Al tavolo del Comitato una volontaria chiama i numeri; dal binario arriva una folata che fa volare via i cartellini. Prima di salire sul treno ti servono un numero, un cappotto e un saluto.",
        "conclusione": "Il ricordo si ferma su una banchina di notte, sotto le lanterne. Un bambino con un cappotto blu e le scarpe nuove stringe una valigia di cartone. Dentro, una mela che non mangera'.",
        "enigmi": [c1(), c2(), c3(), c4(), c5()],
    }
    fb = accenti(fb)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(fb, f, ensure_ascii=False, indent=2)
    print("scritto", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
