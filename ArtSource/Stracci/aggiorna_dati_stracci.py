# -*- coding: utf-8 -*-
"""R012: aggiorna oggetti.json, luoghi.json, personaggi.json, audio.json, mappe.json e crea cutscene_stracci.json (idempotente).
Uso: python aggiorna_dati_stracci.py"""
import json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")


def load(n):
    raw = open(os.path.join(D, n + ".json"), encoding="utf-8").read()
    return json.loads(raw), raw.endswith("\n")


def save(n, d, nl):
    s = json.dumps(d, ensure_ascii=False, indent=2) + ("\n" if nl else "")
    open(os.path.join(D, n + ".json"), "w", encoding="utf-8", newline="\n").write(s)


def upsert(lst, item, key="id"):
    for i, x in enumerate(lst):
        if x[key] == item[key]:
            lst[i] = item
            return
    lst.append(item)


# ------------------------------------------------------------------ oggetti
d, nl = load("oggetti")
upsert(d["oggetti"], {
    "id": "stracci",
    "nome": "Gli stracci",
    "descrizione": "Un fagotto di stracci legato con lo spago. Per Amerigo bambino erano il modo di portare a casa un soldo: li raccoglieva per le case e li portava a Capa 'e fierro, che li pesava e li pagava.",
    "significato": "Amerigo ha sette anni e il suo lavoro è un sacco troppo grande per lui. Gli stracci non valgono niente per chi li butta e valgono qualcosa per chi li ricicla: così si campava nel dopoguerra, con il riuso e i piccoli scambi, e molti bambini davano una mano alla famiglia. Il soldo guadagnato è un orgoglio e anche un peso.",
    "temaCivico": "Lavoro minorile ed economia di sopravvivenza: oggi la Costituzione (art. 37: età minima e tutela dei minori; art. 34: scuola; art. 31: protezione dell'infanzia) e la Convenzione ONU sui diritti dell'infanzia (1989, art. 32) proteggono i bambini dallo sfruttamento.",
    "icona": "scarpe",
    "flashback": "flashback_stracci",
    "cutscene": "cutscene_stracci",
    "cutsceneIngresso": "cutscene_stracci",
    "luogo": "basso_antonietta",
    "ingresso": {
        "scena": "20_Int_BassoAntonietta", "x": 3.7, "y": 0.0, "z": 10.3, "yaw": 20,
        "richiedeFlashback": "flashback_scarpe",
        "prompt": "Esamina: il fagotto di stracci",
        "parti": [
            {"id": "straccio_a", "forma": "cubo", "materiale": "stoffa", "colore": "#8a7a58", "y": 0.1, "sx": 0.5, "sy": 0.2, "sz": 0.38},
            {"id": "straccio_b", "forma": "cubo", "materiale": "stoffa", "colore": "#6b5642", "x": 0.06, "y": 0.24, "z": 0.02, "ry": 24, "sx": 0.38, "sy": 0.12, "sz": 0.3},
            {"id": "straccio_c", "forma": "cubo", "materiale": "stoffa", "colore": "#a8584a", "x": -0.1, "y": 0.2, "z": -0.1, "ry": -18, "sx": 0.22, "sy": 0.08, "sz": 0.2},
            {"id": "spago_a", "forma": "cubo", "materiale": "stoffa", "colore": "#cbb98a", "y": 0.17, "sx": 0.54, "sy": 0.012, "sz": 0.012},
            {"id": "spago_b", "forma": "cubo", "materiale": "stoffa", "colore": "#cbb98a", "x": 0.05, "y": 0.15, "sx": 0.012, "sy": 0.012, "sz": 0.42}
        ]
    }
})
save("oggetti", d, nl)

# ------------------------------------------------------------------ luoghi
d, nl = load("luoghi")
for l in d["luoghi"]:
    if l["id"] == "casa_capa_e_fierro":
        l["descrizione"] = "La bottega di robivecchi (stracci e ferri vecchi) di Capa 'e fierro: brontolone ma in fondo buono, compra gli stracci che i bambini del quartiere raccolgono per le case. Si entra solo nel ricordo degli stracci."
    if l["id"] == "vicolo_stracci":
        l["descrizione"] = "Un vicolo stretto dove si raccolgono e si vendono stracci e panni usati. Oggi si rivive nel ricordo degli stracci."
    if l["id"] == "sede_partito":
        l["sbloccatoDa"] = "stracci"
        l["sbloccatoMsg"] = "Dagli stracci hai imparato dove cercare: la sede del Comitato è il prossimo posto da visitare, ma oggi la porta è ancora chiusa. Ne riparleremo più avanti."
upsert(d["luoghi"], {
    "id": "ricordo_stracci", "nome": "Il ricordo degli stracci", "scena": "40_FB_Stracci", "spawn": "", "aperto": False,
    "descrizione": "Il vicolo degli stracci e la bottega di Capa 'e fierro nel 1946: polvere, panni stesi e luce di lampada a petrolio.", "meteo": "sereno"
})
save("luoghi", d, nl)

# ------------------------------------------------------------------ personaggi
d, nl = load("personaggi")
upsert(d["personaggi"], {"id": "capa_e_fierro", "nome": "Capa 'e fierro", "ritratto": "capa_e_fierro", "colore": "#6E5A3A"})
upsert(d["personaggi"], {"id": "donna_vicolo", "nome": "Una donna del vicolo", "ritratto": "", "colore": "#8C5A4A"})
save("personaggi", d, nl)

# ------------------------------------------------------------------ audio
d, nl = load("audio")
upsert(d["scene"], {"scena": "40_FB_Stracci", "musica": "tema_amerigo_ricordo", "ambiente": ["ricordo_tono"], "sparsi": ["carretto", "panni"],
                    "intervallo": [10, 28], "superficie": "ovattato"}, "scena")
save("audio", d, nl)

# ------------------------------------------------------------------ cutscene
B = []


def b(i, pers, testo, emo, cap, azione="", sfondo="", pos="sx"):
    B.append({"id": "cs_stracci_%03d" % i, "personaggio": pers, "testo": testo, "emozione": emo, "audio": "", "azione": azione, "sfondo": sfondo,
              "posizione": pos, "capitolo": cap})


b(1, "amerigo_adulto", "Lo spago è ancora quello. Ha tenuto più a lungo di certe promesse.", "pensierosa", "ingresso", "dissolvenza", "cs_basso")
b(2, "amerigo_adulto", "Stracci. Tutti insieme sembrano niente. Eppure ognuno veniva da una porta a cui avevo bussato.", "pensierosa", "ingresso")
b(3, "narratore", "Napoli, 1946. Un bambino scalzo con un sacco più grande di lui bussa a un portone dopo l'altro.", "neutra", "ingresso", "dissolvenza", "cs_vicolo")
b(4, "amerigo", "Permesso! Avete degli stracci da darmi? Non vi chiedo niente, solo quello che non vi serve più.", "felice", "ingresso", "entra_sx", "", "sx")
b(5, "donna_vicolo", "Vieni qua, guagliò. Questo era di mio figlio, ormai gli sta corto. È liso, ma tiene ancora caldo.", "neutra", "ingresso", "", "", "dx")
b(6, "amerigo", "Grazie, signo'. Questi non sono stracci: ognuno ha una casa da cui è venuto.", "felice", "ingresso", "", "", "sx")
b(7, "tommasino", "Amerigo! Che fai, il facchino? Scalzo e con quel sacco sembri una lumaca con la casa addosso.", "felice", "ingresso", "entra_dx", "", "dx")
b(8, "amerigo", "Una lumaca che alla sera ha i soldi per il pane, Tommasì. Tu sei più svelto a parlare che a lavorare.", "decisa", "ingresso", "", "", "sx")
b(9, "tommasino", "Eh, non si può avere tutto. Vai, vai, che Capa 'e fierro non aspetta nessuno.", "neutra", "ingresso", "", "", "dx")
b(10, "narratore", "Capa 'e fierro compra stracci e ferri vecchi in una bottega in fondo al vicolo. Prima di arrivarci bisogna raccogliere quelli giusti: non tutto quello che è vecchio vale qualcosa.", "neutra", "ingresso")
b(11, "capa_e_fierro", "Ottantatré lire. Non una di più, e tienile strette: in questo quartiere i soldi hanno le gambe.", "neutra", "chiusura", "dissolvenza", "cs_ricordo", "dx")
b(12, "amerigo", "Ottantatré. Il conto l'avevo già fatto prima di voi.", "felice", "chiusura", "entra_sx", "", "sx")
b(13, "capa_e_fierro", "Lo so, lo so. E la bilancia... mi hai fatto vergognare, guagliò. Un chiodo! Chissà da quanto stava là sotto.", "triste", "chiusura", "", "", "dx")
b(14, "capa_e_fierro", "Tieni, questa lira è mia, non è del conto. Per il chiodo, e per non dirlo in giro.", "neutra", "chiusura", "", "", "dx")
b(15, "amerigo", "Grazie. Non lo dico a nessuno... a nessuno che non sia un cliente.", "felice", "chiusura", "", "", "sx")
b(16, "narratore", "Amerigo torna da Antonietta con il soldo stretto in pugno. Lo mette nella cassetta di latta, vicino a tutto quello che non si deve perdere.", "neutra", "chiusura", "dissolvenza", "cs_basso")
b(17, "antonietta", "Dove li hai presi? ... Va bene. Mettili nella latta e non farli vedere. E lavati quei piedi, che sono neri come il carbone.", "neutra", "chiusura", "entra_dx", "", "dx")
b(18, "amerigo_adulto", "A sette anni un soldo pesa più di un soldo. Oggi un bambino di sette anni dovrebbe portare altri pesi: un quaderno, un pallone, il sonno.", "pensierosa", "chiusura", "dissolvenza", "cs_ricordo", "sx")
b(19, "narratore", "Per Amerigo c'era un soldo. Nel nostro Paese di oggi ci sono i diritti che quel soldo non comprava: la scuola, la salute, un'età giusta per lavorare.", "decisa", "chiusura")
cs = {"id": "cutscene_stracci", "titolo": "Gli stracci", "sfondo": "cs_vicolo", "battute": B}
open(os.path.join(D, "cutscene_stracci.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(cs, ensure_ascii=False, indent=2))

# ------------------------------------------------------------------ mappe
d, nl = load("mappe")


def R(x0, x1, z0, z1):
    return {"x0": x0, "x1": x1, "z0": z0, "z1": z1}


def S(id, nome, tipo, zone, ancore):
    return {"id": id, "nome": nome, "tipo": tipo, "zone": zone, "ancore": [{"x": x, "z": z} for x, z in ancore]}


scena40 = {
    "scena": "40_FB_Stracci", "titolo": "Il vicolo degli stracci, nel 1946", "tema": "ricordo",
    "ax0": -33, "az0": -37, "ax1": 33, "az1": 55, "ppm": 8, "raggio": 20,
    "strade": [
        S("largo_stracci", "Largo degli Stracci", "largo", [R(-6, 6, -30, -22)], [(0, -26)]),
        S("vico_stracci", "Vico degli Stracci", "vicolo", [R(-3, 3, -22, 12)], [(0, -3)]),
        S("scala_carraio", "Scalinata del Carraio", "rampa", [R(-3, 3, 12, 15)], [(0, 13.5)]),
        S("vico_alto", "Vico Alto", "vicolo", [R(-3, 3, 15, 24)], [(0, 19.5)]),
        S("largo_bottega", "Largo della Bottega", "largo", [R(-6, 6, 24, 32)], [(0, 28)]),
        S("soglia_bottega", "Soglia della bottega", "vicolo", [R(-1.75, 1.75, 32, 35)], [(0, 33.5)]),
        S("bottega", "La bottega di Capa 'e fierro", "stanza", [R(-8, 8, 35, 48)], [(0, 41.5)]),
        S("vico_panni", "Vico dei Panni", "vicolo", [R(-16, -3, -9, -5)], [(-9.5, -7)]),
        S("cortile_bucato", "Cortile del Bucato", "cortile", [R(-26, -16, -14, -1)], [(-21, -7.5)]),
        S("lotto_macerie", "Il Lotto delle Macerie", "largo", [R(3, 15, -20, -8)], [(9, -14)]),
        S("vico_carrettieri", "Vico dei Carrettieri", "vicolo", [R(3, 14, 4, 8)], [(8.5, 6)]),
        S("cortile_carrettieri", "Cortile dei Carrettieri", "cortile", [R(14, 24, -2, 12)], [(19, 5)]),
        S("scala_terrazzo", "Scala del Terrazzino", "rampa", [R(18, 22, 12, 15)], [(20, 13.5)]),
        S("terrazzino", "Il Terrazzino", "cortile", [R(14, 26, 15, 23)], [(20, 19)]),
        S("passaggio_alto", "Passaggio Alto", "vicolo", [R(3, 14, 17, 21)], [(8.5, 19)]),
    ],
    "punti": [
        {"id": "uscita", "glifo": "uscita", "nome": "Porta della bottega", "x": 5.8, "z": 47.0},
        {"id": "bottega", "glifo": "bottega", "nome": "Bottega di Capa 'e fierro", "x": 0.0, "z": 34.0},
    ],
}
sc = d["scene"]
for i, s in enumerate(sc):
    if s["scena"] == "40_FB_Stracci":
        scena40["ax0"], scena40["az0"], scena40["ax1"], scena40["az1"] = s["ax0"], s["az0"], s["ax1"], s["az1"]
        sc[i] = scena40
        break
else:
    sc.append(scena40)

P40 = lambda x, z, t: {"scena": "40_FB_Stracci", "x": x, "z": z, "testo": t}
FAG = {"scena": "20_Int_BassoAntonietta", "x": 3.7, "z": 10.3, "testo": "Esamina il fagotto di stracci nel cortile"}
BAS = {"scena": "10_Hub_Vicolo", "x": -9.1, "z": -2.0, "testo": "Torna al basso di Antonietta"}
ob = [
    ("ob_stracci_giro", "Raccogli gli stracci nel vicolo", ["enigma_giro"], [P40(-4.0, -25.4, "Guarda e prova gli stracci al banco, poi cercane altri nel vicolo")]),
    ("ob_stracci_ceste", "Smista gli stracci nelle ceste", ["enigma_ceste"], [P40(-6.6, 41.0, "Metti ogni straccio nella sua cesta")]),
    ("ob_stracci_bilancia", "Pesa le ceste sulla bilancia", ["enigma_bilancia"], [P40(6.6, 39.6, "La bilancia di Capa 'e fierro pende: guarda bene")]),
    ("ob_stracci_conto", "Fai il conto e riscuoti", ["enigma_conto"], [P40(6.6, 43.6, "Leggi il registro e conta le monete")]),
    ("ob_stracci_mestieri", "Guarda il muro dei mestieri", ["enigma_mestieri"], [P40(-1.0, 47.0, "Abbina gli attrezzi ai mestieri e ai diritti")]),
]
obs = d["obiettivi"]
for id, testo, comp, pts in ob:
    item = {"id": id, "testo": testo, "capitolo": "flashback_stracci", "completo": comp, "punti": pts + [FAG, BAS]}
    for i, o in enumerate(obs):
        if o["id"] == id:
            obs[i] = item
            break
    else:
        obs.append(item)
save("mappe", d, nl)
print("dati aggiornati")
