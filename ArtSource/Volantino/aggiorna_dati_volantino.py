# -*- coding: utf-8 -*-
"""R013: aggiorna oggetti.json, luoghi.json, personaggi.json, audio.json, mappe.json, testi_ambiente.json e crea cutscene_volantino.json (idempotente).
Uso: python aggiorna_dati_volantino.py   (dopo ArtSource/Stracci/aggiorna_dati_stracci.py: il volantino si trova dopo gli stracci)
Dialoghi originali (TRAMA §8), mai frasi del libro o del film. Maddalena e' un personaggio di finzione (TRAMA, Decisione 4); la Pachiochia e' la paura, non una cattiva.
"""
import json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")
SCENA = "50_FB_Volantino"


def load(n):
    raw = open(os.path.join(D, n + ".json"), encoding="utf-8").read()
    return json.loads(raw), raw.endswith("\n")


def save(n, d, nl, indent=2):
    s = json.dumps(d, ensure_ascii=False, indent=indent) + ("\n" if nl else "")
    open(os.path.join(D, n + ".json"), "w", encoding="utf-8", newline="\n").write(s)


def upsert(lst, item, key="id"):
    for i, x in enumerate(lst):
        if x[key] == item[key]:
            lst[i] = item
            return
    lst.append(item)


# ------------------------------------------------------------------ oggetti
# il volantino ingiallito, piegato in quattro, sulla credenza del basso (fronte della credenza verso +x, piano a 0,96 m)
d, nl = load("oggetti")
upsert(d["oggetti"], {
    "id": "volantino",
    "nome": "Il volantino del treno",
    "descrizione": "Un foglio ciclostilato, ingiallito e piegato in quattro: «Treni per i bambini». Sul retro, una piccola linea del tempo timbrata e una croce a matita al posto di una firma.",
    "significato": "Antonietta non sapeva leggere, eppure quel foglio l'ha conservato per tutta la vita. Nel vicolo correvano voci spaventose sui treni dei bambini; il volantino rispondeva con tre cose che chiunque poteva controllare. Per Amerigo è il ricordo della prima volta che una prova ha battuto una paura.",
    "temaCivico": "Dicerie e fonti, libertà di parola e di stampa (art. 21), associazioni e partiti (artt. 18 e 49), voto alle donne (2 giugno 1946; oggi artt. 3 e 48 della Costituzione).",
    "icona": "scarpe",
    "flashback": "flashback_volantino",
    "cutscene": "cutscene_volantino",
    "cutsceneIngresso": "cutscene_volantino",
    "luogo": "basso_antonietta",
    "ingresso": {
        "scena": "20_Int_BassoAntonietta", "x": -3.98, "y": 0.965, "z": 1.98, "yaw": 80,
        "richiedeFlashback": "flashback_stracci",
        "prompt": "Esamina: un foglio piegato sulla credenza",
        "parti": [
            {"id": "foglio", "forma": "cubo", "materiale": "carta", "colore": "#cbb98a", "y": 0.004, "sx": 0.16, "sy": 0.008, "sz": 0.11},
            {"id": "piega", "forma": "cubo", "materiale": "carta", "colore": "#a8966c", "y": 0.0085, "sx": 0.16, "sy": 0.001, "sz": 0.004},
            {"id": "angolo", "forma": "cubo", "materiale": "carta", "colore": "#d8c89c", "x": 0.05, "y": 0.009, "z": 0.03, "ry": 35, "sx": 0.05, "sy": 0.002, "sz": 0.04},
            {"id": "timbro", "forma": "cubo", "materiale": "carta", "colore": "#7a1f1a", "x": -0.045, "y": 0.0088, "z": -0.025, "sx": 0.035, "sy": 0.0006, "sz": 0.02},
        ],
    },
})
save("oggetti", d, nl)

# ------------------------------------------------------------------ luoghi
d, nl = load("luoghi")
for l in d["luoghi"]:
    if l["id"] == "sede_partito":
        l["descrizione"] = "La sede del Comitato, dove le volontarie organizzano l'accoglienza dei bambini: tavoli, un ciclostile, una bacheca piena di fogli. Nel quartiere si dicono molte cose strane su chi la frequenta. Oggi ci si entra solo nel ricordo del volantino."
        l["sbloccatoDa"] = "stracci"
        l["sbloccatoMsg"] = "La porta della sede è chiusa. Ma Antonietta un foglio del Comitato l'aveva conservato: cercalo nella credenza del basso."
    if l["id"] == "stazione":
        l["sbloccatoDa"] = "volantino"
        l["sbloccatoMsg"] = "Il volantino parlava dei treni: la stazione è il prossimo posto da visitare, ma per ora il cancello resta chiuso. Si aprirà più avanti."
upsert(d["luoghi"], {
    "id": "ricordo_volantino", "nome": "Il ricordo del volantino", "scena": SCENA, "spawn": "", "aperto": False,
    "descrizione": "La stessa strada del ricordo delle scarpe, ma di sera e sotto la pioggia: voci dalle finestre e, in fondo al largo, la sede del Comitato con la luce accesa.",
    "meteo": "pioggia",
})
save("luoghi", d, nl)

# ------------------------------------------------------------------ personaggi
d, nl = load("personaggi")
upsert(d["personaggi"], {"id": "pachiochia", "nome": "La Pachiochia", "ritratto": "", "colore": "#4A4038"})
upsert(d["personaggi"], {"id": "madre_vicolo", "nome": "Una madre del vicolo", "ritratto": "", "colore": "#7A5A48"})
save("personaggi", d, nl)

# ------------------------------------------------------------------ audio
d, nl = load("audio")
upsert(d["scene"], {"scena": SCENA, "musica": "tema_amerigo_ricordo", "ambiente": ["ricordo_tono", "pioggia"], "sparsi": ["finestra", "panni"],
                    "intervallo": [12, 30], "superficie": "ovattato"}, "scena")
upsert(d["eventi"], {"id": "voce_vicolo", "stinger": "", "sfx": "finestra"})
save("audio", d, nl)

# ------------------------------------------------------------------ cutscene
B = []


def b(i, pers, testo, emo, cap, azione="", sfondo="", pos="sx"):
    B.append({"id": "cs_volantino_%03d" % i, "personaggio": pers, "testo": testo, "emozione": emo, "audio": "", "azione": azione, "sfondo": sfondo,
              "posizione": pos, "capitolo": cap})


b(1, "amerigo_adulto", "Un foglio piegato in quattro, giallo come un limone dimenticato. Lo teneva fra i piatti buoni.", "pensierosa", "ingresso", "dissolvenza", "cs_basso")
b(2, "amerigo_adulto", "Mia madre non sapeva leggere. Eppure questo foglio non l'ha mai buttato.", "pensierosa", "ingresso")
b(3, "narratore", "Napoli, autunno 1946. È sera e piove fino. Antonietta attraversa il vicolo con Amerigo per mano.", "neutra", "ingresso", "dissolvenza", "cs_vicolo")
b(4, "antonietta", "Cammina e non guardare le finestre. La gente parla, e la gente non sa niente.", "decisa", "ingresso", "entra_dx", "", "dx")
b(5, "amerigo", "E che dicono, mammà?", "neutra", "ingresso", "entra_sx", "", "sx")
b(6, "antonietta", "Dicono. Basta questo. Tu stammi vicino e leggimi le carte che ci danno.", "neutra", "ingresso", "", "", "dx")
b(7, "amerigo", "Io so leggere tutto, mammà. Quasi tutto. Le lettere grandi di sicuro.", "felice", "ingresso", "", "", "sx")
b(8, "narratore", "Amerigo conosce le lettere dei manifesti e poco altro. Ma stasera ha deciso che sa leggere, e nessuno glielo toglie dalla testa.", "neutra", "ingresso")
b(9, "narratore", "Prima della sede del Comitato bisogna passare sotto i balconi. E dai balconi, la sera, scendono le voci.", "neutra", "ingresso")
b(10, "maddalena", "Questo è per voi, signora Antonietta. Ci sono scritte tre cose, e tutte e tre le potete controllare.", "decisa", "chiusura", "dissolvenza", "cs_ricordo", "dx")
b(11, "antonietta", "Io le carte non le so leggere. Me lo legge lui.", "neutra", "chiusura", "entra_sx", "", "sx")
b(12, "amerigo", "C'è scritto... treni per i bambini. E la visita del dottore. E quando si torna.", "felice", "chiusura", "", "", "sx")
b(13, "maddalena", "Ha letto bene. Non dovete credere a me: le lettere delle famiglie sono qua, venite a vederle quando volete.", "felice", "chiusura", "", "", "dx")
b(14, "pachiochia", "Belle parole. Le parole non costano niente. Quei bambini non li rivedete più, ve lo dico io.", "triste", "chiusura", "entra_dx", "", "dx")
b(15, "madre_vicolo", "Il registro l'ho visto con questi occhi, Pachiò. C'è scritto quando tornano. E la cartolina di quel bambino viene dall'Emilia, non dall'altro mondo.", "decisa", "chiusura", "", "", "sx")
b(16, "pachiochia", "Eh, vedremo. Io però la notte non dormo, per quelle creature.", "triste", "chiusura", "", "", "dx")
b(17, "maddalena", "Nemmeno io, signora. Per questo li voglio vedere partire con le scarpe ai piedi e tornare con le guance rosse.", "neutra", "chiusura", "", "", "dx")
b(18, "narratore", "Antonietta prende la matita. Non sa scrivere il suo nome: sul registro traccia una croce, lenta e decisa.", "neutra", "chiusura")
b(19, "antonietta", "Una croce vale quanto un nome?", "neutra", "chiusura", "", "", "sx")
b(20, "maddalena", "Vale quanto una firma. Ed è la vostra.", "felice", "chiusura", "", "", "dx")
b(21, "amerigo_adulto", "Quella croce l'ho rivista mille volte. Era la firma più lunga che mia madre abbia mai fatto.", "pensierosa", "chiusura", "dissolvenza", "cs_basso", "sx")
b(22, "narratore", "Le voci correvano più veloci dei treni. Ma una prova che chiunque può controllare, prima o poi, le raggiunge.", "decisa", "chiusura")
cs = {"id": "cutscene_volantino", "titolo": "Il volantino del treno", "sfondo": "cs_vicolo", "battute": B}
open(os.path.join(D, "cutscene_volantino.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(cs, ensure_ascii=False, indent=2))

# ------------------------------------------------------------------ testi d'ambiente (manifesti del Comitato nella sede, carte dei segreti)
# lettere ammesse: A-Z, 0-9 e . , ' - ! ? : /  (niente accenti)
d, nl = load("testi_ambiente")
for t in [
    {"chiave": "ComitatoTreni", "gruppo": "ricordo", "tipo": "manifesto", "w": 256, "h": 384, "seed": 41,
     "righe": ["COMITATO", "PER I BAMBINI", "", "OSPITALITA", "PRESSO FAMIGLIE", "DEL CENTRO-NORD", "", "ISCRIZIONI QUI"], "carta": [0.87, 0.8, 0.62], "inchiostro": [0.45, 0.1, 0.08]},
    {"chiave": "ComitatoVisite", "gruppo": "ricordo", "tipo": "manifesto", "w": 256, "h": 384, "seed": 42,
     "righe": ["AVVISO", "", "PRIMA DELLA", "PARTENZA:", "VISITA MEDICA", "VACCINAZIONE", "BAGNO E VESTITI", "", "LE MADRI POSSONO", "ASSISTERE"], "carta": [0.86, 0.8, 0.64], "inchiostro": [0.12, 0.1, 0.08]},
    {"chiave": "ComitatoVoto", "gruppo": "ricordo", "tipo": "manifesto", "w": 256, "h": 384, "seed": 43,
     "righe": ["DONNE!", "", "IL 2 GIUGNO", "ANCHE VOI", "VOTATE", "", "LA SCHEDA SI", "SEGNA CON LA", "MATITA COPIATIVA"], "carta": [0.85, 0.78, 0.6], "inchiostro": [0.12, 0.1, 0.08]},
    {"chiave": "ManifestoElezioni", "gruppo": "segreti", "tipo": "ritaglio", "w": 256, "h": 320, "seed": 44,
     "righe": ["ELEZIONI 1946", "", "PER LA PRIMA", "VOLTA IN TUTTA", "ITALIA VOTANO", "LE DONNE.", "", "2 GIUGNO:", "REFERENDUM E", "COSTITUENTE"], "carta": [0.84, 0.78, 0.6], "inchiostro": [0.14, 0.1, 0.08]},
    {"chiave": "CartolinaNord", "gruppo": "segreti", "tipo": "cartolina", "w": 256, "h": 160, "seed": 45,
     "righe": ["SALUTI DALL'EMILIA"], "carta": [0.86, 0.79, 0.6], "inchiostro": [0.27, 0.18, 0.1]},
]:
    upsert(d["testi"], t, "chiave")
save("testi_ambiente", d, nl)

# ------------------------------------------------------------------ mappe
d, nl = load("mappe")


def R(x0, x1, z0, z1):
    return {"x0": x0, "x1": x1, "z0": z0, "z1": z1}


def S(id, nome, tipo, zone, ancore):
    return {"id": id, "nome": nome, "tipo": tipo, "zone": zone, "ancore": [{"x": x, "z": z} for x, z in ancore]}


scarpe = next(s for s in d["scene"] if s["scena"] == "30_FB_Scarpe")
# il vicolo delle voci e' la variante di sera del set "Le scarpe" (D1): stesse strade e stessi nomi, piu' la soglia e la sede del Comitato
strade = json.loads(json.dumps(scarpe["strade"]))
strade += [
    S("soglia_sede", "Soglia della sede", "vicolo", [R(-1.75, 1.75, 37, 40)], [(0, 38.5)]),
    S("sala_comitato", "La sala del Comitato", "stanza", [R(-8, 8, 40, 50)], [(0, 47)]),
    S("stanza_registri", "La stanza dei registri", "stanza", [R(-8, 8, 50, 60)], [(0, 58)]),
]
punti = [p for p in scarpe["punti"] if p["id"] not in ("uscita", "calzolaio")]
punti += [
    {"id": "sede", "glifo": "porta", "nome": "Sede del Comitato", "x": 0.0, "z": 38.5},
    {"id": "uscita", "glifo": "uscita", "nome": "Porta sul retro", "x": 5.0, "z": 59.5},
]
scena50 = {"scena": SCENA, "titolo": "La stessa strada, una sera del 1946", "tema": "ricordo",
           "ax0": scarpe["ax0"], "az0": scarpe["az0"], "ax1": scarpe["ax1"], "az1": 68, "ppm": 8, "raggio": 20,
           "strade": strade, "punti": punti}
sc = d["scene"]
for i, s in enumerate(sc):
    if s["scena"] == SCENA:
        scena50["ax0"], scena50["az0"], scena50["ax1"], scena50["az1"] = s["ax0"], s["az0"], s["ax1"], s["az1"]  # MappaBuilder li ha gia' misurati
        sc[i] = scena50
        break
else:
    sc.append(scena50)

P50 = lambda x, z, t: {"scena": SCENA, "x": x, "z": z, "testo": t}
CRE = {"scena": "20_Int_BassoAntonietta", "x": -4.0, "z": 1.9, "testo": "Esamina il foglio piegato sulla credenza"}
BAS = {"scena": "10_Hub_Vicolo", "x": -9.1, "z": -2.0, "testo": "Torna al basso di Antonietta"}
ob = [
    ("ob_volantino_voci", "Ascolta le voci del vicolo", ["enigma_voci"], [P50(-3.6, -25.5, "Ascolta le voci sotto i balconi e al lavatoio, poi rileggile nel quaderno")]),
    ("ob_volantino_bacheca", "Collega le voci alle prove", ["enigma_bacheca"], [P50(-7.0, 45.0, "La bacheca della sede: ogni voce ha un foglio che le risponde")]),
    ("ob_volantino_ciclostile", "Stampa il volantino", ["enigma_ciclostile"], [P50(6.0, 44.5, "Il ciclostile: tre frasi chiare, poi matrice, inchiostro, manovella")]),
    ("ob_volantino_ruoli", "Chi fa che cosa", ["enigma_ruoli"], [P50(0.0, 44.3, "Il tavolo dell'accoglienza: a chi serve ogni cosa?")]),
    ("ob_volantino_timbro", "Timbra le date", ["enigma_timbro"], [P50(0.0, 55.2, "Il datario nella stanza dei registri: le date in ordine")]),
]
obs = d["obiettivi"]
for id, testo, comp, pts in ob:
    item = {"id": id, "testo": testo, "capitolo": "flashback_volantino", "completo": comp, "punti": pts + [CRE, BAS]}
    for i, o in enumerate(obs):
        if o["id"] == id:
            obs[i] = item
            break
    else:
        obs.append(item)
save("mappe", d, nl)
print("dati aggiornati")
