# -*- coding: utf-8 -*-
"""R025: genera Assets/_Game/Resources/Data/filmato_intro.json (regia del filmato iniziale).
Il copione (battute e ID del doppiaggio) e' in cutscene_intro.json (generato da genera_intro.py); qui ci sono
inquadrature, inserti e testi dell'interfaccia del filmato. Uso: python genera_filmato.py
Sistema di riferimento: mondo (x, y, z) oppure, con "segui", nel sistema dell'attore (x destra, y su, z avanti)."""
import json, os

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data", "filmato_intro.json")
HUB, FB = "10_Hub_Vicolo", "30_FB_Scarpe"


def p(id, forma="cubo", materiale="legno", colore="", x=0, y=0, z=0, rx=0, ry=0, rz=0, sx=1, sy=1, sz=1, testo="", dim=0.05, modello="", **k):
    d = {"id": id, "forma": forma, "materiale": materiale, "colore": colore, "x": x, "y": y, "z": z, "rx": rx, "ry": ry, "rz": rz,
         "sx": sx, "sy": sy, "sz": sz, "testo": testo, "dimensione": dim, "modello": modello}
    d.update(k)
    return d


def shot(da="", p0=None, p1=None, m0=None, m1=None, fov0=45, fov1=0, segui="", ease="dolce", taglio="", scossa=0, consegna=False,
         musica="", attore=None, inserto=None):
    d = {"da": da, "taglio": taglio, "fov0": fov0, "fov1": fov1, "segui": segui, "ease": ease, "scossa": scossa, "consegna": consegna, "musica": musica}
    if p0: d["p0"] = p0
    if p1: d["p1"] = p1
    if m0: d["m0"] = m0
    if m1: d["m1"] = m1
    if attore: d["attore"] = attore
    if inserto: d["inserto"] = inserto
    return d


def attore(pos=None, yaw=9999, percorso=None, velocita=0, nascondi=False):
    d = {"presente": True, "yaw": yaw, "percorso": percorso or [], "velocita": velocita, "nascondi": nascondi}
    if pos: d["pos"] = pos
    return d


def ins(parti, distanza=1, giraAlSecondo=14, inclina=12, alto=0.25, fov=32, luce="calda", **k):
    d = {"tipo": "pezzi", "parti": parti, "distanza": distanza, "giraAlSecondo": giraAlSecondo, "inclina": inclina, "alto": alto, "fov": fov, "luce": luce}
    d.update(k)
    return d


# ------------------------------------------------------------------ inserti (pezzi)
cucina = [
    p("corpo", materiale="metallo", colore="#2b2926", y=0.45, sx=0.6, sy=0.9, sz=0.45),
    p("piano", materiale="metallo", colore="#1f1d1b", y=0.92, sx=0.64, sy=0.03, sz=0.49),
    p("tubo", forma="cilindro", materiale="metallo", colore="#2b2926", x=0.2, y=1.2, z=0.12, sx=0.11, sy=0.55, sz=0.11),
    p("sportello", materiale="ottone", colore="#8a6a2c", y=0.5, z=-0.23, sx=0.28, sy=0.2, sz=0.02),
    p("brace", materiale="cera", colore="#d4752b", y=0.5, z=-0.245, sx=0.18, sy=0.07, sz=0.01),
    p("pentola", forma="cilindro", materiale="metallo", colore="#5b5750", x=-0.12, y=1.04, sx=0.26, sy=0.18, sz=0.26),
    p("coperchio", forma="cilindro", materiale="ottone", colore="#8a6a2c", x=-0.12, y=1.14, sx=0.27, sy=0.02, sz=0.27),
    p("pane", forma="sfera", materiale="cuoio", colore="#9a6a35", x=0.17, y=0.99, z=-0.08, sx=0.17, sy=0.08, sz=0.12),
]
binari = [
    p("rotaia_s", materiale="metallo", colore="#3a3733", x=-0.37, y=0.1, sx=0.06, sy=0.1, sz=3.2),
    p("rotaia_d", materiale="metallo", colore="#3a3733", x=0.37, y=0.1, sx=0.06, sy=0.1, sz=3.2),
] + [p("traversina%d" % i, materiale="legno", colore="#3b2a1c", y=0.03, z=-1.4 + i * 0.35, sx=1.1, sy=0.06, sz=0.16) for i in range(9)] + [
]
nomi = ["AMERIGO", "TOMMASINO", "CARMELA", "GENNARO", "ROSETTA", "PEPPINO"]
cartellini = [p("cordino", materiale="stoffa", colore="#4a3a2a", y=0.5, sx=1.25, sy=0.01, sz=0.01)]
for i, n in enumerate(nomi):
    x = -0.5 + i * 0.2
    cartellini.append(p("filo%d" % i, materiale="stoffa", colore="#4a3a2a", x=x, y=0.44, sx=0.004, sy=0.1, sz=0.004))
    cartellini.append(p("cartellino%d" % i, materiale="carta", colore="#cdbb8f", x=x, y=0.33, rz=(-7 if i % 2 else 6), sx=0.15, sy=0.2, sz=0.004, testo=n, dim=0.017))
toppa = [
    p("panno", materiale="stoffa", colore="#8e8c84", sx=0.36, sy=0.5, sz=0.05, modello="panno_quadri", msx=0.3431, msy=0.5034, msz=0.0787, mox=-0.0027, moy=-0.0016, moz=0.0067),
    p("toppa", materiale="stoffa", colore="#6a5a3e", x=0.04, y=-0.05, z=-0.045, sx=0.14, sy=0.14, sz=0.004),
    p("ago", forma="cilindro", materiale="metallo", colore="#b9b5ae", x=0.13, y=-0.02, z=-0.055, rz=55, sx=0.004, sy=0.16, sz=0.004),
    p("molletta", materiale="legno", colore="#6b5a3e", x=-0.1, y=0.25, z=-0.01, sx=0.0135, sy=0.072, sz=0.024, modello="molletta"),
]
scarpe = [
    p("asse", materiale="legno", colore="#4a3220", y=-0.02, sx=0.7, sy=0.04, sz=0.35),
    p("scarpa_s", forma="scarpa", materiale="", x=-0.1, z=-0.03, ry=20, sx=1.5, sy=1.5, sz=1.5, testo="militari"),
    p("scarpa_d", forma="scarpa", materiale="", x=0.1, z=0.04, ry=5, sx=1.5, sy=1.5, sz=1.5, testo="militari"),
]
enigmi = [
    p("lucchetto", materiale="ottone", colore="#8a6a2c", x=-0.55, y=0.08, sx=0.32, sy=0.38, sz=0.1, modello="lucchetto", msx=0.32, msy=0.469, msz=0.107, mox=0.0, moy=-0.0055, moz=-0.0015),
    p("foglio", materiale="carta", colore="#d3c49a", x=0, y=0.195, sx=0.45, sy=0.85, sz=0.0154, modello="calendario"),
    p("placca", materiale="ottone", colore="#8a6a2c", x=0, y=-0.115, z=-0.012, sx=0.46, sy=0.16, sz=0.02, modello="placca_ottone"),
    p("cornice", materiale="legno", colore="#3a2616", x=0.56, y=0.1, sx=0.36, sy=0.46, sz=0.045, modello="cornice", msx=0.36, msy=0.46, msz=0.1198, mox=0.0, moy=0.0, moz=0.0314),
    p("foto", materiale="carta", colore="#7b6a52", x=0.56, y=0.1, z=-0.0165, sx=0.22, sy=0.3, sz=0.004, modello="fotografia"),
]
latta = [
    p("base", materiale="metallo", colore="#6e6a5a", x=-0.2, y=0.05, sx=0.302, sy=0.1, sz=0.2098, modello="cassetta_corpo"),
    p("coperchio", materiale="metallo", colore="#7a7664", x=-0.2, y=0.2, z=0.1, rx=-100, sx=0.31, sy=0.032, sz=0.21, modello="cassetta_coperchio"),
    p("bottoni", forma="cilindro", materiale="ottone", colore="#9a7a3c", x=-0.2, y=0.095, sx=0.08, sy=0.01, sz=0.08),
    p("quaderno", materiale="carta", colore="#1b1815", x=0.27, y=0.012, z=-0.02, ry=-12, sx=0.2, sy=0.024, sz=0.28),
    p("pagine", materiale="carta", colore="#e3d8bc", x=0.27, y=0.026, z=-0.02, ry=-12, sx=0.18, sy=0.01, sz=0.26),
    p("matita", forma="cilindro", materiale="legno", colore="#c9a24a", x=0.1, y=0.03, z=0.1, rz=90, ry=25, sx=0.008, sy=0.18, sz=0.008),
]
tavola = [p("piano", materiale="legno", colore="#5a3d26", y=0.38, sx=1.1, sy=0.04, sz=0.7)] + [
    p("gamba%d" % i, materiale="legno", colore="#4a3220", x=(-0.5 if i % 2 == 0 else 0.5), y=0.18, z=(-0.28 if i < 2 else 0.28), sx=0.05, sy=0.36, sz=0.05) for i in range(4)] + [
    p("piatto%d" % i, forma="cilindro", materiale="gesso", colore="#d8d2c4", x=-0.33 + i * 0.33, y=0.41, z=-0.12, sx=0.2, sy=0.012, sz=0.2) for i in range(3)] + [
    p("piatto_in_piu", forma="cilindro", materiale="gesso", colore="#e6dfd0", x=0.0, y=0.41, z=0.14, sx=0.2, sy=0.012, sz=0.2),
    p("pane", forma="sfera", materiale="cuoio", colore="#9a6a35", x=0.0, y=0.45, z=0.14, sx=0.13, sy=0.07, sz=0.1),
    p("candela", forma="cilindro", materiale="cera", colore="#e8dcc0", x=0.42, y=0.47, z=0.2, sx=0.035, sy=0.12, sz=0.035),
    p("brocca", forma="cilindro", materiale="gesso", colore="#c9b89a", x=-0.42, y=0.5, z=0.2, sx=0.1, sy=0.22, sz=0.1),
]
lucchetto = [p("corpo", materiale="ottone", colore="#8a6a2c", y=0.08, sx=0.32, sy=0.38, sz=0.1, modello="lucchetto", msx=0.32, msy=0.469, msz=0.107, mox=0.0, moy=-0.0055, moz=-0.0015)]

# ------------------------------------------------------------------ capitoli
capitoli = []

# 1. Il ritorno: presente, pioggia, hub. Amerigo adulto cammina da Largo della Fontanella verso la rampa e il basso.
capitoli.append({"id": "c1_ritorno", "scena": HUB, "stile": "fade", "musica": "-", "tinta": "#7A93B02A", "tintaFine": "", "inquadrature": [
    shot(p0=[0, 17, -53], p1=[0, 14, -50], m0=[0, 4, -22], m1=[0, 3, -26], fov0=50, fov1=46, attore=attore(pos=[1.4, -31], yaw=0, nascondi=True)),
    shot(da="cs_intro_002", p0=[0.3, 1.62, 1.7], p1=[0.12, 1.6, 1.15], m0=[0, 1.6, 0], m1=[0, 1.6, 0], fov0=38, fov1=32, segui="adulto",
         attore=attore(pos=[1.4, -30], yaw=180)),
    shot(da="cs_intro_003", p0=[0.9, 1.9, -3.6], p1=[0.7, 1.65, -2.9], m0=[0, 1.5, 1.8], m1=[0, 1.45, 2.2], fov0=48, segui="adulto",
         attore=attore(pos=[1.4, -31], yaw=0, percorso=[1.4, -9.5], velocita=1.35)),
    shot(da="cs_intro_004", p0=[-1.1, 1.35, -2.0], p1=[-0.8, 1.55, -2.6], m0=[0.4, 1.9, 2.4], m1=[0.2, 2.2, 3.2], fov0=46, segui="adulto", musica="tema_amerigo_solo"),
    shot(da="cs_intro_005", p0=[-4.8, 1.55, -2.9], p1=[-5.8, 1.75, -2.6], m0=[-9.1, 2.0, -2.0], m1=[-9.1, 2.1, -2.0], fov0=42, fov1=34,
         attore=attore(nascondi=True)),
    shot(da="cs_intro_006", p0=[0.22, 1.68, 0.95], p1=[0.1, 1.67, 0.6], m0=[0, 1.67, 0], m1=[0, 1.67, 0], fov0=30, fov1=24, segui="adulto",
         attore=attore(pos=[-3.4, -2.0], yaw=90)),
]})

# 2. Napoli, 1946: ricordo, 30_FB_Scarpe. Piedi nudi, Amerigo bambino, Tommasino e Antonietta (ritratti), rammendo.
capitoli.append({"id": "c2_1946", "scena": FB, "stile": "ricordo", "musica": "tema_amerigo_ricordo", "tinta": "#8A5A2B18", "inquadrature": [
    shot(p0=[-3.6, 1.35, -31.2], p1=[-2.2, 1.5, -30.2], m0=[0, 2.2, -14], m1=[0, 2.0, -14], fov0=52, fov1=48, attore=attore(nascondi=True)),
    shot(da="cs_intro_008", p0=[0.45, 0.16, 1.15], p1=[0.5, 0.8, 1.55], m0=[0, 0.06, 0], m1=[0, 0.78, 0], fov0=38, fov1=34, segui="bambino",
         attore=attore(pos=[-1.0, -25.5], yaw=200)),
    shot(da="cs_intro_009", p0=[1.3, 0.75, -1.9], p1=[1.1, 0.72, -1.45], m0=[0, 0.6, 0.3], m1=[0, 0.62, 0.3], fov0=42, segui="bambino",
         attore=attore(pos=[-1.0, -25.5], yaw=20)),
    shot(da="cs_intro_011", p0=[-0.2, 1.5, -8.6], p1=[-2.8, 1.5, -7.2], m0=[-6.0, 1.7, -3.8], m1=[-8.4, 1.7, -3.9], fov0=48, attore=attore(nascondi=True)),
    shot(da="cs_intro_012", inserto=ins(toppa, distanza=0.7, giraAlSecondo=9, inclina=8, luce="lampada"), attore=attore(nascondi=True)),
]})

# 3. I treni dei bambini: ricordo, manifesti, dicerie, numeri.
capitoli.append({"id": "c3_treni", "scena": FB, "stile": "ricordo", "musica": "", "tinta": "#8A5A2B14", "tintaFine": "#B4B4B4C0", "inquadrature": [
    shot(p0=[-1.9, 1.7, -30.4], p1=[1.9, 1.7, -30.4], m0=[-1.2, 1.72, -32], m1=[1.2, 1.72, -32], fov0=38, fov1=36, ease="lineare", attore=attore(pos=[0, -26], yaw=0, nascondi=True)),
    shot(da="cs_intro_014", inserto=ins(cucina, distanza=0.85, giraAlSecondo=10, inclina=10, luce="lampada")),
    shot(da="cs_intro_015", p0=[2.4, 1.5, -13.0], p1=[1.6, 1.55, -11.0], m0=[-1.0, 1.7, -4.0], m1=[-1.4, 1.7, -4.0], fov0=46, attore=attore(nascondi=True)),
    shot(da="cs_intro_016", p0=[0.5, 1.35, -18.0], p1=[0.2, 1.4, -17.2], m0=[-3.0, 1.5, -15.0], m1=[-3.1, 1.5, -15.0], fov0=36, fov1=30, scossa=0.035),
    shot(da="cs_intro_017", p0=[1.0, 0.55, -9.0], p1=[0.6, 0.7, -7.6], m0=[-0.5, 2.3, -3.0], m1=[-0.6, 2.4, -3.0], fov0=52, fov1=46),
    shot(da="cs_intro_018", p0=[0.7, 0.55, -1.6], p1=[0.5, 0.5, -1.2], m0=[0, 0.7, 0.3], m1=[0, 0.7, 0.3], fov0=40, segui="bambino",
         attore=attore(pos=[1.0, -9.0], yaw=0)),
    shot(da="cs_intro_019", inserto=ins(binari, distanza=0.7, giraAlSecondo=6, inclina=24, alto=0.1, luce="calda")),
    shot(da="cs_intro_020", inserto=ins(cartellini, distanza=0.9, giraAlSecondo=-8, inclina=5, luce="lampada")),
    shot(da="cs_intro_021", p0=[1.4, 1.25, -2.4], p1=[1.2, 1.15, -1.8], m0=[0, 0.8, 0.5], m1=[0, 0.85, 0.5], fov0=44, segui="bambino",
         musica="-", attore=attore(pos=[0.0, -12.0], yaw=0)),
]})

# 4. Le cose di Antonietta: presente, hub, davanti al basso, inserti degli oggetti.
capitoli.append({"id": "c4_oggetti", "scena": HUB, "stile": "fade", "musica": "tema_amerigo_scoperta", "tinta": "#7A93B02A", "inquadrature": [
    shot(p0=[0.55, 1.55, -1.7], p1=[0.32, 1.52, -1.2], m0=[0, 1.25, 1.0], m1=[0, 1.2, 1.0], fov0=44, fov1=38, segui="adulto",
         attore=attore(pos=[-8.3, -2.0], yaw=270)),
    shot(da="cs_intro_023", p0=[0.35, 1.65, -1.5], p1=[0.25, 1.65, -1.9], m0=[0, 1.5, 2.2], m1=[0, 1.5, 2.6], fov0=46, segui="adulto",
         attore=attore(pos=[-7.0, -2.0], yaw=90, percorso=[-3.6, -2.0], velocita=0.9)),
    shot(da="cs_intro_024", inserto=ins(scarpe, distanza=0.9, giraAlSecondo=16, inclina=18, luce="lampada")),
    shot(da="cs_intro_025", inserto=ins(enigmi, distanza=1.0, giraAlSecondo=12, inclina=8)),
    shot(da="cs_intro_026", inserto=ins(latta, distanza=0.9, giraAlSecondo=12, inclina=24, luce="lampada")),
]})

# 5. Se perdi la strada: presente, crane sopra i tetti, pianta con la croce, pannello con Indizio, errore gentile.
capitoli.append({"id": "c5_strada", "scena": HUB, "stile": "fade", "musica": "tema_amerigo_solo", "tinta": "#7A93B02A", "inquadrature": [
    shot(p0=[0.5, 2.2, -12], p1=[0, 18, -16], m0=[-4, 3.5, -3], m1=[-6, 0, -2], fov0=52, fov1=48, attore=attore(nascondi=True)),
    shot(da="cs_intro_028", p0=[0, 18, -16], p1=[0, 30, -18], m0=[-6, 0, -2], m1=[-6, 0, -2], fov0=48, fov1=44,
         inserto={"tipo": "pianta", "scena": HUB, "croce": "si"}),
    shot(da="cs_intro_030", inserto=ins(lucchetto, distanza=1.05, giraAlSecondo=14, inclina=10, luce="lampada", indizio="si")),
    shot(da="cs_intro_031", inserto=ins(lucchetto, distanza=1.05, giraAlSecondo=-14, inclina=10, luce="lampada", errore="si")),
]})

# 6. Perche' ricordare: ricordo, schede d'epoca e articoli, tavola apparecchiata; il seppia si scalda.
capitoli.append({"id": "c6_perche", "scena": FB, "stile": "ricordo", "musica": "tema_amerigo_ricordo", "tinta": "#8A5A2B60", "tintaFine": "#8A5A2B00", "inquadrature": [
    shot(p0=[0.9, 0.95, -3.0], p1=[0.6, 0.82, -2.3], m0=[0, 0.7, 3.5], m1=[0, 0.7, 3.5], fov0=44, segui="bambino",
         attore=attore(pos=[0, -17.5], yaw=0, percorso=[0, -12.5], velocita=0.6)),
    shot(da="cs_intro_033", inserto={"tipo": "carta", "carta": "scheda"}, p0=[1.0, 1.0, -2.8], m0=[0, 0.8, 3], segui="bambino"),
    shot(da="cs_intro_034", inserto={"tipo": "carta", "carta": "art3"}, p0=[1.0, 1.0, -2.8], m0=[0, 0.8, 3], segui="bambino"),
    shot(da="cs_intro_035", inserto={"tipo": "carta", "carta": "art2_31_34"}, p0=[1.0, 1.0, -2.8], m0=[0, 0.8, 3], segui="bambino"),
    shot(da="cs_intro_036", inserto=ins(tavola, distanza=0.95, giraAlSecondo=7, inclina=24, luce="lampada")),
    shot(da="cs_intro_037", p0=[1.3, 1.0, -2.7], p1=[1.0, 0.95, -2.1], m0=[0, 0.75, 1.0], m1=[0, 0.75, 1.0], fov0=42, segui="bambino"),
]})

# 7. La porta: presente, slargo, consegna al gioco.
capitoli.append({"id": "c7_porta", "scena": HUB, "stile": "fade", "musica": "tema_amerigo_solo", "tinta": "#7A93B020", "inquadrature": [
    shot(p0=[0.9, 1.75, -2.6], p1=[0.55, 1.7, -2.1], m0=[0, 1.7, 3.2], m1=[0, 1.7, 3.2], fov0=46, segui="adulto",
         attore=attore(pos=[-2.2, -2.0], yaw=270)),
    shot(da="cs_intro_040", p0=[0.0, 2.2, -9.0], p1=[0.3, 1.9, -8.4], m0=[5.0, 6.3, -11.4], m1=[5.4, 6.7, -11.4], fov0=52, fov1=46, attore=attore(nascondi=True)),
    shot(da="cs_intro_041", consegna=True, musica="-", attore=attore(pos=[-2.2, -2.0], yaw=270)),
]})

controlli = [
    {"id": "salta", "titolo": "Per saltare, tieni premuto", "tastiera": ["Esc"], "gamepad": ["B"], "touch": ["Tocco lungo"]},
    {"id": "muovi", "titolo": "Per muoverti", "tastiera": ["W", "A", "S", "D"], "gamepad": ["Leva sinistra"], "touch": ["Joystick a sinistra"]},
    {"id": "guarda", "titolo": "Per guardarti intorno", "tastiera": ["Mouse"], "gamepad": ["Leva destra"], "touch": ["Trascina a destra"]},
    {"id": "interagisci", "titolo": "Per esaminare e prendere", "tastiera": ["E"], "gamepad": ["A"], "touch": ["Pulsante Interagisci"]},
    {"id": "inventario", "titolo": "La latta e il quaderno", "tastiera": ["I"], "gamepad": ["Pulsante Latta"], "touch": ["Pulsante Latta"]},
    {"id": "mappa", "titolo": "La pianta grande", "tastiera": ["M"], "gamepad": ["Select"], "touch": ["Tocca la minimappa"]},
    {"id": "indizio", "titolo": "Se ti serve una mano", "tastiera": ["Pulsante Indizio"], "gamepad": ["Pulsante Indizio"], "touch": ["Pulsante Indizio"]},
]

carte = [
    {"id": "scheda", "titolo": "2 giugno 1946", "stile": "scheda", "larghezza": 600, "altezza": 400, "inclina": -2.5,
     "righe": ["Repubblica o Monarchia?", "", "Per la prima volta in Italia", "votano insieme donne e uomini.", "", "Si elegge anche l'Assemblea Costituente."]},
    {"id": "art3", "titolo": "Art. 3", "stile": "pagina", "larghezza": 760, "altezza": 520, "inclina": 1.5,
     "righe": ["Tutti i cittadini hanno pari dignità sociale e sono eguali davanti alla legge, senza distinzione di sesso, di razza, di lingua, di religione, di opinioni politiche, di condizioni personali e sociali.",
               "", "È compito della Repubblica rimuovere gli ostacoli di ordine economico e sociale, che, limitando di fatto la libertà e l'eguaglianza dei cittadini, impediscono il pieno sviluppo della persona umana."]},
    {"id": "art2_31_34", "titolo": "Articoli 2, 31 e 34", "stile": "pagina", "larghezza": 760, "altezza": 520, "inclina": -1.5,
     "righe": ["Art. 2: la Repubblica richiede l'adempimento dei doveri inderogabili di solidarietà politica, economica e sociale.",
               "", "Art. 31: protegge la maternità, l'infanzia e la gioventù, favorendo gli istituti necessari a tale scopo.",
               "", "Art. 34: la scuola è aperta a tutti."]},
]

out = {
    "parametri": {"tenutaSalta": 1.2, "velocitaTesto": 45, "obiettivoForzatoSecondi": 30, "guardiaSecondi": 0.35, "pausaFraBattute": 0.3,
                  "velocitaCammino": 1.35, "conRitratto": ["antonietta", "tommasino", "maddalena"]},
    "testi": {"saltaTitolo": "Per saltare, tieni premuto", "rivedi": "Rivedi il filmato iniziale", "continuaFilmato": "",
              "indizio": "Indizio", "erroreTitolo": "Quasi!",
              "erroreTesto": "Non è la combinazione giusta, ma niente paura: il ricordo ti spiega perché e aspetta che tu riprovi.",
              "piantaTitolo": "La pianta del quartiere", "piantaNota": "", "latta": "La latta", "quaderno": "Il quaderno"},
    "controlli": controlli, "carte": carte, "capitoli": capitoli,
    "finale": {"scena": HUB, "pos": [-2.2, -2.0], "yaw": 270},
}
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(out, f, ensure_ascii=False, indent=1)
print("scritto", os.path.abspath(OUT))
