# -*- coding: utf-8 -*-
"""R012: genera Assets/_Game/Resources/Data/flashback_stracci.json (i cinque enigmi del ricordo "Stracci").
I testi del gioco stanno nel JSON generato; questo script e' solo la sorgente riproducibile. Uso: python genera_flashback_stracci.py
Coordinate = quelle di SceneBuilderStracci.cs (devono coincidere).
"""
import json, os, sys

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data", "flashback_stracci.json")


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


# ---------------------------------------------------------------- posizioni nel mondo
# S1: banco d'esame nel largo d'ingresso; i dieci pezzi sparsi per il vicolo
POS_S1 = (-4.0, 0.0, -25.4)
# bottega (quota 1.2)
Q = 1.2
POS_S2 = (-6.6, Q, 41.0)     # ceste, parete ovest, fronte verso est
POS_S3 = (6.6, Q, 39.6)      # bilancia, parete est, fronte verso ovest
POS_S4 = (6.6, Q, 43.6)      # registro e monete
POS_S5 = (-1.0, Q, 47.35)    # muro dei mestieri, parete nord, fronte verso sud

STRACCI = [
    # id, nome, x, y, z, colore, fibra
    ("straccio_1", "Pezzo grigio ruvido", -1.6, 0.0, -14.5, "#5b5a4c", "lana"),
    ("straccio_2", "Pezzo chiaro e liscio", 1.9, 0.0, -17.5, "#c9bfa6", "cotone"),
    ("straccio_3", "Pezzo beige a nodini", 8.0, 0.0, -14.2, "#bfae8a", "lino"),
    ("straccio_4", "Pezzo marrone peloso", 12.2, 0.0, -11.0, "#6b5642", "lana"),
    ("straccio_5", "Pezzo rosso mattone", -9.6, 0.0, -7.0, "#a8584a", "cotone"),
    ("straccio_6", "Pezzo verdolino a trama larga", -21.0, 0.0, -4.5, "#8f9a7c", "lino"),
    ("straccio_7", "Pezzo blu scuro spesso", -20.0, 0.0, -12.0, "#3d4756", "lana"),
    ("straccio_8", "Pezzo azzurro sbiadito", 10.0, 0.0, 6.2, "#7d8da0", "cotone"),
    ("straccio_9", "Pezzo viola sfilacciato", 19.0, 0.0, 9.0, "#7a5d6b", "seta"),
    ("straccio_10", "Pezzo scuro e lucido", 22.0, Q, 20.0, "#2f3a2f", "tela"),
]

FIBRE = {
    "lana": ("Lana",
             "In controluce la trama è fitta e un po' pelosa: la luce passa a stento.",
             "Tiri un filo: si arriccia come una molla e torna indietro da solo.",
             "Lo spessore è buono, morbido e caldo tra le dita."),
    "cotone": ("Cotone",
               "In controluce la trama è fitta e regolare, come una grata ordinata.",
               "Tiri un filo: si spezza netto, corto e pulito.",
               "Spessore medio, liscio e asciutto."),
    "lino": ("Lino",
             "In controluce i fili hanno grossezze diverse, con qualche nodino: la luce passa a chiazze.",
             "Tiri un filo: è lungo e rigido, resiste e non si arriccia.",
             "Spessore irregolare, fresco e un po' rigido: si piega con una piega netta."),
    "seta": ("Da scartare",
             "In controluce la trama è sfilacciata e piena di buchi, con macchie scure di umido.",
             "Tiri un filo: si sfarina e ti resta in mano una polvere.",
             "È sottile come carta bagnata e non regge: puzza di muffa."),
    "tela": ("Da scartare",
             "In controluce non passa niente: è una crosta scura e lucida.",
             "Non riesci a tirare nessun filo: la cera tiene tutto incollato.",
             "Rigida, liscia e appiccicosa: sembra un guscio, non una stoffa."),
}
CATEGORIE = ["Lana", "Cotone", "Lino", "Da scartare"]

# ceste del S2: quali pezzi accetta ciascuna
CESTE = {
    "cesta_lana": ["straccio_1", "straccio_4", "straccio_7"],
    "cesta_cotone": ["straccio_2", "straccio_5", "straccio_8"],
    "cesta_lino": ["straccio_3", "straccio_6"],
    "cesta_scarto": ["straccio_9", "straccio_10"],
}

# carichi del S3 (grammi) e prezzi del S4 (lire al chilo, INVENTATI [GIOCO])
CARICHI = [("lana", 1300, 40), ("cotone", 800, 20), ("lino", 600, 25)]
TOTALE = sum(g * p // 1000 if (g * p) % 1000 == 0 else None for _, g, p in CARICHI)  # 52 + 16 + 15
assert TOTALE == 83, TOTALE


def quiz(testo, opz, corretta, spieg):
    return {"testo": testo, "opzioni": opz, "corretta": corretta, "spiegazione": spieg}


# ---------------------------------------------------------------- S1
def s1():
    oggetti = []
    for id, nome, x, y, z, col, fibra in STRACCI:
        oggetti.append({"id": id, "nome": nome, "descrizione": "Lo raccogli e lo metti nel sacco: poi lo guarderai bene al banco di Capa 'e fierro.",
                        "x": x, "y": y, "z": z, "modello": "straccio", "colore": col})
    passanti = []
    for id, nome, x, y, z, col, fibra in STRACCI:
        cat, c, f, s = FIBRE[fibra]
        passanti.append({"id": id, "nome": nome, "descrizione": nome + ". Provalo: controluce, filo, spessore.",
                         "stile": fibra, "mestiere": cat, "colore": col, "controluce": c, "filo": f, "spessore": s})
    parti = [
        P("piano", "cubo", "legno", "#5a3d26", y=0.78, sx=1.3, sy=0.05, sz=0.7),
        P("gamba_a", "cubo", "legno", "#4a3220", x=-0.58, y=0.38, z=-0.28, sx=0.06, sy=0.76, sz=0.06),
        P("gamba_b", "cubo", "legno", "#4a3220", x=0.58, y=0.38, z=-0.28, sx=0.06, sy=0.76, sz=0.06),
        P("gamba_c", "cubo", "legno", "#4a3220", x=-0.58, y=0.38, z=0.28, sx=0.06, sy=0.76, sz=0.06),
        P("gamba_d", "cubo", "legno", "#4a3220", x=0.58, y=0.38, z=0.28, sx=0.06, sy=0.76, sz=0.06),
        P("sacco", "cubo", "stoffa", "#8a7a58", x=-0.42, y=0.98, z=0.05, ry=12, sx=0.34, sy=0.34, sz=0.26),
        P("sacco_collo", "cubo", "stoffa", "#6f6044", x=-0.42, y=1.2, z=0.05, ry=12, sx=0.18, sy=0.1, sz=0.14),
        P("pila_a", "cubo", "stoffa", "#6b5642", x=0.22, y=0.83, z=0.0, ry=8, sx=0.4, sy=0.05, sz=0.3),
        P("pila_b", "cubo", "stoffa", "#a8584a", x=0.25, y=0.88, z=0.02, ry=-6, sx=0.36, sy=0.05, sz=0.28),
        P("pila_c", "cubo", "stoffa", "#c9bfa6", x=0.2, y=0.93, z=-0.01, ry=14, sx=0.34, sy=0.05, sz=0.26),
        P("lanterna", "cilindro", "metallo", "#3a3530", x=0.5, y=0.9, z=0.22, sx=0.08, sy=0.06, sz=0.08),
    ]
    return {
        "id": "enigma_giro", "tipo": "nascosto", "meccanismo": "tessuti", "titolo": "Il giro degli stracci",
        "consegna": "Capa 'e fierro compra gli stracci buoni e non vuole la roba marcita. Nel vicolo ci sono dieci pezzi gettati tra cortili, macerie e terrazzini: trovali tutti, senza fretta. Poi torna al banco e guardali bene: in controluce, tirando un filo, sentendo lo spessore. Decidi che cos'è ciascuno.",
        "temaCivico": "Il lavoro dei bambini nel 1946: raccogliere materiali di recupero per aiutare la famiglia. Oggi la Costituzione (art. 37) fissa un'età minima per il lavoro e tutela i minori; la Convenzione ONU del 1989 (art. 32) li protegge dallo sfruttamento economico.",
        "indizi": ["Non tutto quello che è vecchio vale qualcosa: un pezzo marcio o incerato non si compra.",
                   "Guarda l'insegna disegnata della bottega, in fondo al vicolo: mostra che cosa compra Capa 'e fierro.",
                   "Lana, cotone e lino sì (tre, tre e due); la seta marcita e la tela cerata no."],
        "spiegazione": "Tre pezzi di lana, tre di cotone, due di lino: questi Capa 'e fierro li compra. La seta marcita e la tela cerata restano fuori. Hai imparato a distinguere una fibra dall'altra con le mani e con la luce.",
        "erroreMsg": "Non tutti i pezzi hanno ancora l'etichetta giusta, ma non preoccuparti: rifai le prove con calma e riprova.",
        "scheda": "Raccogliere stracci per le case e per le strade era un lavoro da bambini a Napoli, come in tante città del dopoguerra. Nessuno lo chiamava lavoro: era darsi da fare. Oggi sappiamo che a un bambino servono la scuola e il tempo per giocare, e la legge lo dice.",
        "appunto": "Nel sacco: tre di lana, tre di cotone, due di lino; da buttare seta marcita e tela cerata.",
        "x": POS_S1[0], "y": POS_S1[1], "z": POS_S1[2], "yaw": 0, "scala": 1, "distanza": 1.9, "inclina": 26,
        "prompt": "il banco degli stracci",
        "produce": ["sacco_stracci"],
        "oggetti": oggetti, "passanti": passanti, "mestieri": CATEGORIE,
        "strumenti": ["Controluce", "Tira il filo", "Spessore", "Etichetta"],
        "completoMsg": "Ora torna al banco davanti al sacco e prova ogni pezzo: controluce, filo, spessore.",
        "parti": parti,
        "domande": [
            quiz("Un pezzo di stoffa spesso e caldo, con il filo che si arriccia come una molla: che fibra è?", ["Lana", "Cotone", "Lino", "Seta"], 0, "Il filo che si arriccia è tipico della lana."),
            quiz("Il filo si spezza netto e in controluce la trama è fitta e regolare: che fibra è?", ["Lana", "Cotone", "Lino", "Tela cerata"], 1, "Il cotone si spezza corto e ha una trama regolare."),
            quiz("Fili di grossezza diversa, con nodini, e una stoffa che si piega rigida: che fibra è?", ["Lana", "Cotone", "Lino", "Seta"], 2, "Il lino ha fili irregolari e fresco al tatto."),
            quiz("Quali pezzi Capa 'e fierro non compra?", ["Lana e cotone", "Cotone e lino", "La seta marcita e la tela cerata", "Nessuno: compra tutto"], 2, "Compra stracci buoni: la roba marcita o incerata non si può riciclare."),
        ],
    }


# ---------------------------------------------------------------- S2
def cesta(id, x, tokens, msg, err, disegno, etichetta):
    gruppo = id
    parti = [
        P(id, "vuoto", "legno", x=x, y=0.27, sx=0.56, sy=0.54, sz=0.56, clic=True, azione="usa", richiede=",".join(tokens),
          evento=id, msg=msg, errore=err),
        P(id + "_corpo", "cilindro", "cuoio", "#8a6a3c", x=0, y=-0.07, sx=0.54, sy=0.2, sz=0.54, genitore=id),
        P(id + "_bocca", "cilindro", "cuoio", "#6a4d2a", x=0, y=0.13, sx=0.57, sy=0.025, sz=0.57, genitore=id),
        P(id + "_cartello", "cubo", "carta", "#d9ccaa", x=0, y=0.62, z=-0.02, sx=0.5, sy=0.4, sz=0.01, gruppo=gruppo),
    ]
    px = x
    if disegno == "lana":      # tre batuffoli
        for i, (dx, dy) in enumerate([(-0.1, -0.05), (0.08, -0.05), (0.0, 0.08)]):
            parti.append(P(id + "_d%d" % i, "sfera", "stoffa", "#e9e3d2", x=px + dx, y=0.62 + dy, z=-0.04, sx=0.17, sy=0.17, sz=0.06))
    elif disegno == "cotone":  # una capsula aperta con calice verde
        parti.append(P(id + "_d0", "sfera", "stoffa", "#f2eee2", x=px, y=0.66, z=-0.04, sx=0.2, sy=0.2, sz=0.06))
        parti.append(P(id + "_d1", "sfera", "stoffa", "#f2eee2", x=px - 0.1, y=0.6, z=-0.04, sx=0.13, sy=0.13, sz=0.06))
        parti.append(P(id + "_d2", "sfera", "stoffa", "#f2eee2", x=px + 0.1, y=0.6, z=-0.04, sx=0.13, sy=0.13, sz=0.06))
        parti.append(P(id + "_d3", "cubo", "legno", "#5d6b3a", x=px, y=0.52, z=-0.035, sx=0.22, sy=0.05, sz=0.03))
    elif disegno == "lino":    # fiore azzurro su stelo
        parti.append(P(id + "_d0", "cubo", "legno", "#5d6b3a", x=px, y=0.58, z=-0.035, sx=0.025, sy=0.3, sz=0.02))
        parti.append(P(id + "_d1", "sfera", "stoffa", "#6a86b5", x=px, y=0.72, z=-0.04, sx=0.14, sy=0.14, sz=0.04))
        parti.append(P(id + "_d2", "sfera", "stoffa", "#e9dcb0", x=px, y=0.72, z=-0.062, sx=0.04, sy=0.04, sz=0.02))
    else:                      # croce
        parti.append(P(id + "_d0", "cubo", "legno", "#4a2a22", x=px, y=0.64, z=-0.035, rz=45, sx=0.34, sy=0.05, sz=0.02))
        parti.append(P(id + "_d1", "cubo", "legno", "#4a2a22", x=px, y=0.64, z=-0.035, rz=-45, sx=0.34, sy=0.05, sz=0.02))
    parti.append(T(id + "_nome", etichetta, px, 0.44, -0.03, 0.5, 0.08, 0.04, "#3b2a1a", "mano", gruppo=gruppo))
    return parti


def s2():
    parti = [
        P("fondo", "cubo", "legno", "#4a3623", y=0.62, z=0.42, sx=2.8, sy=1.4, sz=0.05),
        P("base", "cubo", "legno", "#5a4a38", y=-0.01, z=0.0, sx=2.8, sy=0.03, sz=0.8),
    ]
    parti += cesta("cesta_lana", -0.9, CESTE["cesta_lana"], "Capa 'e fierro, brontolando: «Va bene, la lana va con la lana.»",
                   "Capa 'e fierro scuote la testa: «No, guagliò, guarda il disegno. Questo non va qui.» Riprova con calma.", "lana", "lana")
    parti += cesta("cesta_cotone", -0.3, CESTE["cesta_cotone"], "«Cotone nel cotone. Vedi che quando vuoi, ci arrivi.»",
                   "«Eh no, questo sta in un'altra cesta. Guarda il disegno e ripensa alle prove.» Riprova.", "cotone", "cotone")
    parti += cesta("cesta_lino", 0.3, CESTE["cesta_lino"], "«Il lino: lo riconosco anch'io a occhi chiusi. Bravo.»",
                   "«Quasi. Ma il lino ha i nodini: ricordati la prova in controluce.» Riprova.", "lino", "lino")
    parti += cesta("cesta_scarto", 0.9, CESTE["cesta_scarto"], "«Questa roba la butto: non la compra nessuno.»",
                   "«Questo si può ancora vendere! Mettilo dove va.» Riprova.", "scarto", "da buttare")
    return {
        "id": "enigma_ceste", "tipo": "quiz", "meccanismo": "oggetto", "titolo": "Le ceste della bottega",
        "consegna": "Nella bottega ci sono quattro ceste, ognuna con un disegno. Prendi in mano un pezzo dal sacco (in alto a destra) e mettilo nella cesta giusta. Capa 'e fierro brontola, ma ti corregge senza arrabbiarsi.",
        "temaCivico": "Gli stracci diventavano carta e stoffa nuova: un'economia del recupero che nel dopoguerra serviva a tutti. Il lavoro di chi li raccoglieva, però, era spesso lavoro di bambini.",
        "indizi": ["Ogni cesta ha il suo disegno: un batuffolo, un fiore di cotone, un fiore azzurro, una croce.",
                   "Ricorda le prove: il filo che si arriccia, il filo che si spezza, i nodini in controluce.",
                   "Lana: grigio ruvido, marrone peloso, blu scuro. Cotone: chiaro, rosso, azzurro. Lino: beige e verdolino. Il resto va nella cesta con la croce."],
        "spiegazione": "Ogni pezzo nella sua cesta: lana, cotone, lino e il resto da buttare. In una bottega di robivecchi l'ordine è già metà del guadagno.",
        "scheda": "Nel dopoguerra gli stracci di lana e di cotone venivano rimessi in circolo: venduti a chi li lavorava per farne filati e carta, in un riuso che già allora era normale (i dettagli di quali fabbriche e quali prezzi variavano da città a città).",
        "erroreMsg": "Non è quella la cesta giusta, ma non c'è nessuna fretta: guarda il disegno e riprova.",
        "appunto": "Gli stracci sono nelle ceste, pronti per la pesata.",
        "x": POS_S2[0], "y": POS_S2[1], "z": POS_S2[2], "yaw": -90, "scala": 1, "distanza": 1.7, "inclina": 14,
        "prompt": "le ceste di Capa 'e fierro",
        "richiede": ["sacco_stracci"], "bloccatoMsg": "Prima raccogli gli stracci nel vicolo e provali al banco: senza il sacco non c'è niente da mettere nelle ceste.",
        "produce": ["ceste_pronte"],
        "soluzione": ["%s:%s" % (c, t) for c, ts in CESTE.items() for t in ts],
        "parti": parti,
        "domande": [
            quiz("Un pezzo di lana va nella cesta con...", ["il disegno dei batuffoli", "il fiore azzurro", "la croce", "il fiore di cotone"], 0, "La lana ha il disegno dei batuffoli."),
            quiz("La seta marcita e la tela cerata vanno...", ["nella cesta con la croce", "nella cesta del lino", "nella cesta della lana", "nel sacco di Capa 'e fierro"], 0, "La roba marcita o incerata non si compra: va buttata."),
            quiz("Il pezzo con i nodini e la trama irregolare va nella cesta...", ["del fiore azzurro (lino)", "dei batuffoli (lana)", "del cotone", "con la croce"], 0, "I nodini sono tipici del lino."),
        ],
    }


# ---------------------------------------------------------------- S3
def s3():
    TOP = 0.93
    parti = [
        P("piano", "cubo", "legno", "#5a3d26", y=0.9, sx=1.9, sy=0.06, sz=0.7),
        P("gamba_a", "cubo", "legno", "#4a3220", x=-0.88, y=0.44, z=-0.3, sx=0.07, sy=0.88, sz=0.07),
        P("gamba_b", "cubo", "legno", "#4a3220", x=0.88, y=0.44, z=-0.3, sx=0.07, sy=0.88, sz=0.07),
        P("gamba_c", "cubo", "legno", "#4a3220", x=-0.88, y=0.44, z=0.3, sx=0.07, sy=0.88, sz=0.07),
        P("gamba_d", "cubo", "legno", "#4a3220", x=0.88, y=0.44, z=0.3, sx=0.07, sy=0.88, sz=0.07),
        P("base_bilancia", "cilindro", "ottone", "#a9873f", y=0.94, z=0.25, sx=0.26, sy=0.02, sz=0.26),
        P("colonna", "cilindro", "ottone", "#a9873f", y=1.24, z=0.25, sx=0.06, sy=0.3, sz=0.06),
        P("trave", "vuoto", "ottone", y=1.55, z=0.25, sx=0.05, sy=0.05, sz=0.05),
        P("asta", "cubo", "ottone", "#a9873f", sx=0.92, sy=0.028, sz=0.03, genitore="trave"),
        P("indice", "cubo", "ottone", "#a9873f", y=0.07, sx=0.012, sy=0.14, sz=0.012, genitore="trave"),
        P("piatto_sx", "vuoto", "ottone", x=-0.43, y=-0.3, sx=0.05, sy=0.05, sz=0.05, genitore="trave"),
        P("disco_sx", "cilindro", "ottone", "#b8964a", sx=0.32, sy=0.013, sz=0.32, genitore="piatto_sx"),
        P("filo_sx_a", "cubo", "metallo", "#6a5a35", x=-0.11, y=0.15, sx=0.008, sy=0.3, sz=0.008, genitore="piatto_sx"),
        P("filo_sx_b", "cubo", "metallo", "#6a5a35", x=0.11, y=0.15, sx=0.008, sy=0.3, sz=0.008, genitore="piatto_sx"),
        P("chiodo", "cilindro", "metallo", "#5a4a42", x=0.02, y=-0.032, z=-0.13, rx=90, sx=0.014, sy=0.12, sz=0.014, genitore="piatto_sx", clic=True, azione="chiodo", evento="chiodo_tolto",
          msg="Un chiodo, incastrato sotto il piatto! Ecco perché pendeva. Capa 'e fierro lo guarda e brontola: «E chi ce l'ha messo...»"),
        P("chiodo_testa", "cilindro", "metallo", "#5a4a42", x=0.02, y=-0.032, z=-0.25, rx=90, sx=0.03, sy=0.006, sz=0.03, genitore="piatto_sx"),
        P("piatto_dx", "vuoto", "ottone", x=0.43, y=-0.3, sx=0.05, sy=0.05, sz=0.05, genitore="trave"),
        P("disco_dx", "cilindro", "ottone", "#b8964a", sx=0.32, sy=0.013, sz=0.32, genitore="piatto_dx"),
        P("filo_dx_a", "cubo", "metallo", "#6a5a35", x=-0.11, y=0.15, sx=0.008, sy=0.3, sz=0.008, genitore="piatto_dx"),
        P("filo_dx_b", "cubo", "metallo", "#6a5a35", x=0.11, y=0.15, sx=0.008, sy=0.3, sz=0.008, genitore="piatto_dx"),
    ]
    # il peso campione e le tre ceste (piatto sinistro) sul piano a sinistra
    parti.append(P("campione", "cilindro", "ottone", "#caa24c", x=-0.78, y=TOP + 0.05, z=-0.15, sx=0.13, sy=0.05, sz=0.13, clic=True, azione="peso", gruppo="sx", valore=1000, msg="il peso campione da 1 kg"))
    parti.append(T("campione_t", "1 kg", -0.78, TOP + 0.002, -0.28, 0.2, 0.05, 0.03, "#3b2a1a", "mano", rx=90))
    for i, (nome, g, prezzo) in enumerate(CARICHI):
        col = {"lana": "#5b5a4c", "cotone": "#c9bfa6", "lino": "#bfae8a"}[nome]
        x = -0.55 + i * 0.22
        parti.append(P("carico_" + nome, "cilindro", "cuoio", "#8a6a3c", x=x, y=TOP + 0.06, z=0.02, sx=0.17, sy=0.06, sz=0.17, clic=True, azione="peso", gruppo="sx", valore=g,
                       evento="pesata_" + nome, msg="la cesta del " + nome if nome != "lana" else "la cesta della lana"))
        parti.append(P("carico_" + nome + "_stoffa", "cubo", "stoffa", col, x=x, y=TOP + 0.125, z=0.02, ry=15, sx=0.15, sy=0.04, sz=0.12, genitore="carico_" + nome))
    # pesi di ottone a destra
    for i, (nome, g, r, h) in enumerate([("p1000", 1000, 0.12, 0.04), ("p500", 500, 0.095, 0.034), ("p200", 200, 0.07, 0.03), ("p100", 100, 0.055, 0.026)]):
        x = 0.35 + i * 0.2
        parti.append(P(nome, "cilindro", "ottone", "#caa24c", x=x, y=TOP + h, z=-0.12, sx=r, sy=h, sz=r, clic=True, azione="peso", gruppo="dx", valore=g, msg=str(g) + " g"))
        parti.append(T(nome + "_t", ("1 kg" if g == 1000 else "%d g" % g), x, TOP + 0.002, -0.27, 0.2, 0.05, 0.03, "#3b2a1a", "mano", rx=90))
    return {
        "id": "enigma_bilancia", "tipo": "quiz", "meccanismo": "bilancia", "titolo": "La bilancia che sbaglia",
        "consegna": "Il conto non torna: la bilancia di Capa 'e fierro pende anche quando non dovrebbe. Prima verifica la bilancia con il peso campione da un chilo (a sinistra) contro il peso da un chilo (a destra). Poi pesa le tre ceste, una alla volta, sul piatto di sinistra, scegliendo i pesi giusti sul piatto di destra fino all'equilibrio. Tocca un peso per metterlo sul piatto, toccalo ancora per toglierlo.",
        "temaCivico": "Una paga giusta comincia da una misura giusta: il diritto di ogni lavoratore, grande o piccolo, a non essere sfruttato (art. 36 della Costituzione: retribuzione proporzionata al lavoro; art. 37: parità di retribuzione per i minori).",
        "indizi": ["Una bilancia giusta sta in equilibrio con le stesse cose da una parte e dall'altra: la tua sta ferma anche con un chilo contro un chilo?",
                   "Prova il peso campione da un chilo contro il peso da un chilo: se pende, non è colpa dei pesi.",
                   "Guarda sotto il piatto di sinistra, dalla parte davanti: qualcosa è incastrato."],
        "spiegazione": "Un chiodo, finito per caso sotto il piatto, falsava ogni pesata. Tolto quello, le tre ceste pesano 1300 g (lana), 800 g (cotone) e 600 g (lino). Capa 'e fierro non è un imbroglione: se ne accorge, brontola e ti dà ragione.",
        "erroreMsg": "Non è ancora in equilibrio: guarda da che parte pende e aggiungi o togli un peso.",
        "scheda": "Pesi e misure giusti sono la base di ogni scambio onesto. I prezzi e i pesi di questo gioco sono inventati [GIOCO]: servono a fare un conto, non a dire quanto valeva uno straccio nel 1946.",
        "appunto": "Sul registro: lana 1300 g, cotone 800 g, lino 600 g.",
        "x": POS_S3[0], "y": POS_S3[1], "z": POS_S3[2], "yaw": 90, "scala": 1, "distanza": 1.9, "inclina": 6,
        "prompt": "la bilancia a piatti",
        "richiede": ["ceste_pronte"], "bloccatoMsg": "Prima le ceste: la bilancia serve per pesare gli stracci già smistati.",
        "produce": ["pesi_scritti"], "squilibrio": 150,
        "soluzione": ["chiodo_tolto", "pesata_lana", "pesata_cotone", "pesata_lino"],
        "strumenti": [
            "Un chilo contro un chilo, e la bilancia pende a sinistra. Una bilancia giusta non dovrebbe: guarda meglio sotto il piatto di sinistra.",
            "Equilibrio! Per {0} servono {1} grammi.",
            "Un chiodo incastrato sotto il piatto: ecco perché pendeva.",
            "Si è fermata, ma con quel chiodo sotto il piatto il conto non è giusto: cerca che cosa la disturba.",
        ],
        "parti": parti,
        "domande": [
            quiz("Un chilo contro un chilo e la bilancia pende da una parte. Che cosa significa?", ["Che la bilancia è starata", "Che i pesi sono finti", "Che un chilo pesa di più a sinistra", "Che bisogna comprare più stracci"], 0, "Una bilancia giusta sta ferma con pesi uguali: se pende, qualcosa non va."),
            quiz("Che cosa c'era sotto il piatto di sinistra?", ["Un chiodo", "Una moneta", "Un pezzo di stoffa", "Niente"], 0, "Un chiodo incastrato falsava ogni pesata."),
            quiz("Con 1000 g, 200 g e 100 g sul piatto destro, quanto pesa la cesta della lana (senza chiodo)?", ["1300 g", "1000 g", "1200 g", "1500 g"], 0, "1000 + 200 + 100 = 1300 grammi."),
        ],
    }


# ---------------------------------------------------------------- S4
def s4():
    TOP = 0.93
    parti = [
        P("piano", "cubo", "legno", "#5a3d26", y=0.9, sx=2.2, sy=0.06, sz=0.8),
        P("gamba_a", "cubo", "legno", "#4a3220", x=-1.02, y=0.44, z=-0.34, sx=0.07, sy=0.88, sz=0.07),
        P("gamba_b", "cubo", "legno", "#4a3220", x=1.02, y=0.44, z=-0.34, sx=0.07, sy=0.88, sz=0.07),
        P("gamba_c", "cubo", "legno", "#4a3220", x=-1.02, y=0.44, z=0.34, sx=0.07, sy=0.88, sz=0.07),
        P("gamba_d", "cubo", "legno", "#4a3220", x=1.02, y=0.44, z=0.34, sx=0.07, sy=0.88, sz=0.07),
        # il registro dei prezzi, aperto a destra
        P("registro", "cubo", "carta", "#d9ccaa", x=0.62, y=TOP + 0.012, z=0.05, rx=0, sx=0.52, sy=0.024, sz=0.38, clic=True, azione="leggi", evento="registro",
          msg="Il registro: prezzi al chilo, lana 40 lire, cotone 20 lire, lino 25 lire. Pesate di oggi: lana 1300 g, cotone 800 g, lino 600 g."),
        T("reg_titolo", "REGISTRO DEI PREZZI", 0.62, TOP + 0.03, 0.2, 0.46, 0.05, 0.034, "#3b2a1a", "", rx=90),
        T("reg_lana", "Lana  ·  40 lire al chilo", 0.62, TOP + 0.03, 0.12, 0.46, 0.045, 0.03, "#3b2a1a", "mano", rx=90),
        T("reg_cotone", "Cotone  ·  20 lire al chilo", 0.62, TOP + 0.03, 0.05, 0.46, 0.045, 0.03, "#3b2a1a", "mano", rx=90),
        T("reg_lino", "Lino  ·  25 lire al chilo", 0.62, TOP + 0.03, -0.02, 0.46, 0.045, 0.03, "#3b2a1a", "mano", rx=90),
        T("reg_pesate", "oggi: lana 1300 g · cotone 800 g · lino 600 g", 0.62, TOP + 0.03, -0.11, 0.48, 0.07, 0.024, "#7a1f1a", "mano", rx=90),
        # il banco: segnaposto per le monete e il totale
        P("banco", "vuoto", "legno", x=-0.1, y=TOP, z=0.05, sx=0.02, sy=0.02, sz=0.02),
        P("banco_panno", "cubo", "stoffa", "#3d5a3a", x=-0.1, y=TOP + 0.004, z=0.1, sx=0.95, sy=0.008, sz=0.36),
        T("totale", "Sul banco: 0 lire", -0.1, TOP + 0.03, 0.34, 0.7, 0.06, 0.04, "#e6dcc0", "", rx=90),
    ]
    # le monete della cassetta (davanti): 10 lire x6, 5 lire x4, 2 lire x4, 1 lira x6
    parti.append(P("cassetta", "cubo", "legno", "#4a3220", x=-0.0, y=TOP + 0.02, z=-0.31, sx=1.3, sy=0.04, sz=0.32))
    righe = [("m10", 10, 6, 0.13, "#b0a99a"), ("m5", 5, 4, 0.11, "#9a9382"), ("m2", 2, 4, 0.095, "#a58a5a"), ("m1", 1, 6, 0.085, "#9c8f6e")]
    idx = 0
    for pre, val, n, d, col in righe:
        for k in range(n):
            parti.append(P("%s_%d" % (pre, k), "cilindro", "argento" if val >= 5 else "ottone", col, x=round(-0.52 + (idx % 10) * 0.115, 3),
                           y=TOP + 0.045, z=round(-0.37 + 0.12 * (idx // 10), 3), sx=d, sy=0.006, sz=d, clic=True, azione="moneta", valore=val, msg="%d lire" % val))
            idx += 1
    return {
        "id": "enigma_conto", "tipo": "quiz", "meccanismo": "monete", "titolo": "Il conto",
        "consegna": "Capa 'e fierro ti deve pagare. Leggi il registro: i prezzi al chilo e le pesate di oggi. Fai un conto per ogni cesta (chili per prezzo), poi somma. Metti sul banco, toccandole, le monete della somma giusta e premi Paga. Toccando una moneta sul banco la riprendi.",
        "temaCivico": "Economia di sopravvivenza e borsa nera: nel 1946 si campava di piccoli scambi e di lavoretti. Il valore del lavoro dei più piccoli oggi è tutelato dalla legge: parità di retribuzione (art. 37) e protezione dallo sfruttamento (Convenzione ONU, art. 32).",
        "indizi": ["Ogni cesta ha il suo prezzo al chilo: lana, cotone e lino non costano uguale.",
                   "Fai un conto per cesta: pesi in chili per prezzo al chilo (1,3 per 40; 0,8 per 20; 0,6 per 25). Poi somma.",
                   "Le tre cifre sono 52, 16 e 15: in tutto 83 lire. Con le monete da 10, 5, 2 e 1 lira si può fare in più modi."],
        "spiegazione": "52 lire per la lana, 16 per il cotone, 15 per il lino: 83 lire in tutto. Capa 'e fierro paga contando le monete sul banco, una per una, e tu hai controllato che il conto fosse giusto.",
        "erroreMsg": "Il conto non torna ancora: rifai la somma per ogni cesta.",
        "scheda": "Prezzi in lire inventati per il gioco [GIOCO]: il conto è un esercizio, non il valore vero degli stracci nel 1946. Saper controllare un conto è una difesa contro chi approfitta di chi non sa fare i calcoli.",
        "appunto": "Capa 'e fierro ha pagato: 83 lire. Il primo soldo guadagnato.",
        "x": POS_S4[0], "y": POS_S4[1], "z": POS_S4[2], "yaw": 90, "scala": 1, "distanza": 2.0, "inclina": 34,
        "prompt": "il banco con il registro",
        "richiede": ["pesi_scritti"], "bloccatoMsg": "Il registro è ancora vuoto: prima pesa le ceste sulla bilancia.",
        "produce": ["soldo"], "codice": "83",
        "strumenti": ["Paga", "Sul banco: {0} lire", "Sul banco ci sono {0} lire: sono troppo poche. Rifai il conto, cesta per cesta.", "Sul banco ci sono {0} lire: sono troppe. Rifai il conto, cesta per cesta."],
        "parti": parti,
        "domande": [
            quiz("Quanto vale la lana: 1,3 kg a 40 lire al chilo?", ["52 lire", "40 lire", "65 lire", "13 lire"], 0, "1,3 per 40 fa 52 lire."),
            quiz("Quanto vale il cotone: 0,8 kg a 20 lire al chilo?", ["16 lire", "20 lire", "8 lire", "25 lire"], 0, "0,8 per 20 fa 16 lire."),
            quiz("Lana 52, cotone 16, lino 15 lire: quanto in tutto?", ["83 lire", "71 lire", "93 lire", "80 lire"], 0, "52 + 16 + 15 = 83 lire."),
        ],
    }


# ---------------------------------------------------------------- S5
def s5():
    parti = [
        P("parete", "cubo", "legno", "#4a3623", y=0.95, z=0.1, sx=3.0, sy=1.9, sz=0.06),
        P("trave_alta", "cubo", "legno", "#3a2a1a", y=1.78, z=0.04, sx=2.9, sy=0.08, sz=0.08),
        # cassetta da lustrascarpe
        P("lustra_cassa", "cubo", "legno", "#6a4528", x=-1.05, y=1.15, z=0.0, sx=0.34, sy=0.2, sz=0.22),
        P("lustra_pedana", "cubo", "legno", "#5a3d26", x=-1.05, y=1.28, z=-0.02, rx=-20, sx=0.3, sy=0.03, sz=0.16),
        P("lustra_spazzola", "cubo", "cuoio", "#2f2218", x=-1.18, y=1.0, z=-0.05, sx=0.1, sy=0.04, sz=0.05),
        # secchio dell'acquaiolo
        P("secchio_corpo", "cilindro", "metallo", "#6a6e70", x=-0.35, y=1.2, z=-0.02, sx=0.3, sy=0.14, sz=0.3),
        P("secchio_manico", "cilindro", "metallo", "#4a4c4e", x=-0.35, y=1.43, z=-0.02, rz=90, sx=0.02, sy=0.15, sz=0.02),
        P("secchio_corda", "cubo", "stoffa", "#8a7a58", x=-0.35, y=0.9, z=-0.02, sx=0.02, sy=0.28, sz=0.02),
        # cesto del venditore ambulante
        P("cesto", "cilindro", "cuoio", "#8a6a3c", x=0.4, y=1.15, z=-0.03, sx=0.38, sy=0.12, sz=0.3),
        P("cesto_manico", "cilindro", "cuoio", "#6a4d2a", x=0.4, y=1.37, z=-0.03, rz=90, sx=0.025, sy=0.17, sz=0.025),
        P("cesto_merce", "cubo", "stoffa", "#a8584a", x=0.4, y=1.29, z=-0.03, sx=0.3, sy=0.05, sz=0.2),
        # sacco degli stracci
        P("sacco", "cubo", "stoffa", "#8a7a58", x=1.1, y=1.1, z=0.0, ry=0, sx=0.3, sy=0.45, sz=0.2),
        P("sacco_collo", "cubo", "stoffa", "#6f6044", x=1.1, y=1.38, z=0.0, sx=0.14, sy=0.1, sz=0.12),
        P("sacco_straccio", "cubo", "stoffa", "#6b5642", x=1.18, y=0.95, z=-0.04, rz=20, sx=0.18, sy=0.05, sz=0.03),
        T("cartello", "I mestieri dei bambini", 0.0, 1.62, -0.03, 1.8, 0.1, 0.06, "#e6dcc0", "mano"),
    ]
    righe = [
        {"oggetto": "La cassetta del lustrascarpe", "mestiere": "Il lustrascarpe", "diritto": "Art. 34 Costituzione: la scuola è aperta a tutti",
         "nota": "Il bambino che lucida scarpe per strada è un bambino che non è a scuola: la scuola aperta a tutti (art. 34) e obbligatoria e gratuita per almeno otto anni."},
        {"oggetto": "Il secchio dell'acquaiolo", "mestiere": "L'acquaiolo", "diritto": "Convenzione ONU 1989, art. 32: protezione dai lavori pericolosi",
         "nota": "Portare secchi pesanti stanca e fa male: la Convenzione ONU (art. 32) protegge i bambini dai lavori pericolosi o dannosi per la salute."},
        {"oggetto": "Il cesto del venditore ambulante", "mestiere": "Il venditore ambulante", "diritto": "Art. 37 Costituzione: età minima e tutela del lavoro dei minori",
         "nota": "Vendere per strada era un lavoro vero: l'art. 37 fissa un'età minima per il lavoro e vuole una tutela speciale per i minori, con parità di paga a parità di lavoro."},
        {"oggetto": "Il sacco degli stracci", "mestiere": "Il raccoglitore di stracci", "diritto": "Art. 31 Costituzione: la Repubblica protegge l'infanzia",
         "nota": "Quando la famiglia è povera, un bambino finisce a raccogliere stracci: l'art. 31 impegna la Repubblica a proteggere l'infanzia e ad aiutare le famiglie."},
    ]
    return {
        "id": "enigma_mestieri", "tipo": "quiz", "meccanismo": "colonne", "titolo": "Il muro dei mestieri",
        "consegna": "Sul muro della bottega sono appesi quattro oggetti: sono gli attrezzi di mestieri che facevano i bambini nel 1946. Per ogni oggetto, tocca la colonna di mezzo per scegliere il mestiere e la colonna di destra per scegliere il diritto di oggi che a quel bambino mancava. Poi premi Conferma.",
        "temaCivico": "Lavoro minorile e diritti dell'infanzia: Costituzione, artt. 31, 34 e 37 (in vigore dal 1948) e Convenzione ONU sui diritti dell'infanzia (1989), art. 32.",
        "indizi": ["Pensa a dove si usava ogni oggetto: in strada, alla fontana, al mercato, per le case.",
                   "Poi chiediti che cosa mancava a quel bambino: la scuola, la salute, un'età giusta per lavorare, una famiglia aiutata.",
                   "Lustrascarpe: scuola (34). Acquaiolo: lavori pericolosi (ONU 32). Venditore ambulante: età minima (37). Stracci: protezione dell'infanzia (31)."],
        "spiegazione": "Quattro mestieri, quattro diritti. Gli articoli sono quelli di oggi: nel 1946 la Costituzione non c'era ancora, ma i bambini c'erano già.",
        "scheda": "I collegamenti sono didattici: più diritti proteggono lo stesso bambino, e qui ne abbiamo scelto uno per ciascun mestiere. Art. 31: la Repubblica protegge l'infanzia. Art. 34: la scuola è aperta a tutti. Art. 37: età minima e tutela del lavoro dei minori. Convenzione ONU 1989, art. 32: protezione dallo sfruttamento economico e dai lavori pericolosi.",
        "erroreMsg": "Non tutti i collegamenti sono giusti: ripensa a che cosa mancava a ciascun bambino.",
        "appunto": "Il lavoro dei bambini oggi ha le sue regole: scuola, salute, età minima, protezione.",
        "x": POS_S5[0], "y": POS_S5[1], "z": POS_S5[2], "yaw": 0, "scala": 1, "distanza": 2.2, "inclina": 4,
        "prompt": "il muro dei mestieri",
        "richiede": ["soldo"], "bloccatoMsg": "Capa 'e fierro non ti ha ancora pagato il conto: prima chiudi il conto sul banco.",
        "produce": ["porta_bottega"],
        "mestieri": ["Il lustrascarpe", "L'acquaiolo", "Il venditore ambulante", "Il raccoglitore di stracci"],
        "diritti": ["Art. 31 Costituzione: la Repubblica protegge l'infanzia", "Art. 34 Costituzione: la scuola è aperta a tutti",
                    "Art. 37 Costituzione: età minima e tutela del lavoro dei minori", "Convenzione ONU 1989, art. 32: protezione dai lavori pericolosi"],
        "strumenti": ["Oggetto", "Mestiere del 1946", "Diritto di oggi", "Hai collegato bene {0} mestieri e {1} diritti su {2}. Ripensa a che cosa mancava a ciascun bambino e riprova."],
        "righe": righe,
        "parti": parti,
        "domande": [
            quiz("A un bambino lustrascarpe, che passa la giornata in strada, che cosa mancava soprattutto?", ["La scuola (art. 34)", "Una bicicletta", "Un cappotto nuovo", "Il telefono"], 0, "L'art. 34 dice che la scuola è aperta a tutti."),
            quiz("Quale articolo fissa un'età minima per il lavoro e tutela i minori?", ["Art. 37", "Art. 12", "Art. 139", "Art. 1"], 0, "L'art. 37 della Costituzione."),
            quiz("Quale documento del 1989 protegge i bambini dai lavori pericolosi?", ["La Convenzione ONU sui diritti dell'infanzia", "Lo Statuto Albertino", "Il Trattato di Roma", "Il Codice della strada"], 0, "La Convenzione ONU, art. 32."),
        ],
    }


def main():
    fb = {
        "id": "flashback_stracci", "oggetto": "stracci", "titolo": "Gli stracci", "scena": "40_FB_Stracci",
        "introduzione": "Chiudi gli occhi e senti la polvere sotto i piedi nudi. Sei di nuovo bambino, con un sacco più grosso di te sulla spalla. Nel vicolo degli stracci ogni cortile ha qualcosa da dare e nessuno lo regala: bisogna chiedere, frugare, scegliere. In fondo c'è la bottega di Capa 'e fierro, dove tutto si pesa e si paga. Prova a rimettere insieme il giro degli stracci.",
        "conclusione": "Il ricordo si spegne come una lampada a petrolio. Per Amerigo quel giro era un modo di darsi da fare, e un soldo guadagnato valeva più di mille parole. Ma a sette anni si dovrebbe stare a scuola, a giocare, a crescere: è questo che molti anni dopo la Repubblica ha messo nero su bianco, perché nessun bambino debba più contare monete al posto di imparare a leggere.",
        "enigmi": [s1(), s2(), s3(), s4(), s5()],
    }
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(fb, f, ensure_ascii=False, indent=2)
    print("scritto", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
