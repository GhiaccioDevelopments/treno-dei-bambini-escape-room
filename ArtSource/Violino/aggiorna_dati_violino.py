# -*- coding: utf-8 -*-
"""R015: aggiorna oggetti.json, luoghi.json, personaggi.json, audio.json, mappe.json, testi_ambiente.json e crea cutscene_violino.json (idempotente).
Uso: python aggiorna_dati_violino.py   (dopo ArtSource/Cappotto/aggiorna_dati_cappotto.py: lo spartito si trova dopo il ricordo del cappotto)
Dialoghi originali (TRAMA §8), mai frasi del libro o del film. Famiglia Benvenuti come nel libro, senza affermare parentele (Decisione 2).
Derna: ne' vedova ne' partigiana (TRAMA §9 n. 1-2). Bella ciao: non citata e non suonata. La lettera la scrive Derna, Amerigo detta (schema R015).
"""
import json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")
MOD = "70_FB_Modena"


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
# lo spartito di Alcide, in un cassettino di legno sulla credenza della cucina del basso (stessa credenza-modello di quella del volantino:
# piano a 0,965 m; credenza della cucina in (1,85, 7,2) ruotata di 90 gradi, fronte verso +x)
d, nl = load("oggetti")
COL = ["#a3261c", "#c88a2a", "#3a6a3a", "#2e4a7a", "#6a3a7a"]
pallini = []
for k, n in enumerate([0, 2, 3, 2, 3, 4, 3, 2, 1, 0]):
    pallini.append({"id": "pallino_%d" % k, "forma": "cilindro", "materiale": "cera", "colore": COL[n], "x": round(-0.06 + (k % 5) * 0.03, 3),
                    "y": 0.072, "z": round(-0.02 + (k // 5) * 0.035, 3), "sx": 0.016, "sy": 0.001, "sz": 0.016})
upsert(d["oggetti"], {
    "id": "spartito",
    "nome": "Lo spartito di Alcide",
    "descrizione": "Un foglio piegato in quattro, consumato sulle pieghe. Niente note: file di pallini colorati, rosso, verde, blu, e in alto, a matita, «Per Amerigo».",
    "significato": "Lo spartito che Alcide Benvenuti disegnò per Amerigo, a Modena, quando scoprì che il bambino aveva orecchio. Antonietta l'ha tenuto in un cassetto per tutta la vita senza saperlo leggere: i pallini colorati erano musica, e lei lo sapeva lo stesso.",
    "temaCivico": "Diritto allo studio (art. 34: la scuola è aperta a tutti) e promozione della cultura (art. 9): il talento di un bambino povero di Napoli scoperto in una casa di Modena.",
    "icona": "scarpe",
    "flashback": "flashback_violino",
    "cutscene": "cutscene_violino",
    "cutsceneIngresso": "cutscene_violino",
    "luogo": "basso_antonietta",
    "ingresso": {
        "scena": "20_Int_BassoAntonietta", "x": 2.07, "y": 0.965, "z": 6.9, "yaw": 80,
        "richiedeFlashback": "flashback_cappotto",
        "prompt": "Esamina: un cassettino socchiuso sulla credenza",
        "parti": [
            {"id": "cassettino", "forma": "cubo", "materiale": "legno", "colore": "#5a3d26", "y": 0.04, "z": 0.03, "sx": 0.24, "sy": 0.08, "sz": 0.16},
            {"id": "cassetto_aperto", "forma": "cubo", "materiale": "legno", "colore": "#6a4a2e", "y": 0.035, "z": -0.07, "sx": 0.21, "sy": 0.06, "sz": 0.1},
            {"id": "pomello", "forma": "sfera", "materiale": "ottone", "colore": "#b59a3f", "y": 0.04, "z": -0.125, "sx": 0.016, "sy": 0.016, "sz": 0.016},
            {"id": "foglio", "forma": "cubo", "materiale": "carta", "colore": "#d8cba6", "y": 0.068, "z": -0.06, "rx": -12, "sx": 0.17, "sy": 0.004, "sz": 0.1},
        ] + pallini,
    },
})
save("oggetti", d, nl)

# ------------------------------------------------------------------ luoghi
d, nl = load("luoghi")
for l in d["luoghi"]:
    if l["id"] == "modena":
        l["descrizione"] = "Una corte di pianura vicino a Modena, d'inverno: la casa di Derna, il laboratorio di Alcide, la scuola in fondo alla strada. Da questo vicolo ci si arriva solo con un ricordo."
        l["sbloccatoDa"] = "cappotto"
        l["sbloccatoMsg"] = "Modena è lontana da questo vicolo. Ma Antonietta qualcosa di Modena l'aveva tenuto: sulla credenza della cucina c'è un cassettino socchiuso."
upsert(d["luoghi"], {
    "id": "ricordo_violino", "nome": "Il ricordo del violino", "scena": MOD, "spawn": "", "aperto": False,
    "descrizione": "Una corte di pianura vicino a Modena, nell'inverno 1946-47: neve, nebbia bassa, la cucina di Derna con la stufa, il laboratorio dei Benvenuti con il forno e il pianoforte, la scuola in fondo alla strada.",
    "meteo": "sereno",
})
save("luoghi", d, nl)

# ------------------------------------------------------------------ personaggi (colori del box dei dialoghi; ritratti da fare con i modelli)
d, nl = load("personaggi")
for p in [
    {"id": "derna", "nome": "Derna", "ritratto": "", "colore": "#6A5A6E"},
    {"id": "alcide", "nome": "Alcide", "ritratto": "", "colore": "#6E5A3A"},
    {"id": "rosa", "nome": "Rosa", "ritratto": "", "colore": "#9A5A4A"},
    {"id": "rivo", "nome": "Rivo", "ritratto": "", "colore": "#4A6A5A"},
    {"id": "luzio", "nome": "Luzio", "ritratto": "", "colore": "#5A5A7A"},
    {"id": "nario", "nome": "Nario", "ritratto": "", "colore": "#7A6A4A"},
]:
    upsert(d["personaggi"], p)
save("personaggi", d, nl)

# ------------------------------------------------------------------ audio (silenzio ovattato della neve, vento, campane lontane: suoni gia' esistenti)
d, nl = load("audio")
upsert(d["scene"], {"scena": MOD, "musica": "tema_amerigo_ricordo", "ambiente": ["ricordo_tono", "vento_mare"], "sparsi": ["campane"],
                    "intervallo": [30, 70], "superficie": "ovattato"}, "scena")
save("audio", d, nl)

# ------------------------------------------------------------------ cutscene (capitoli: ingresso, meta dopo le impronte, chiusura)
B = []


def b(i, pers, testo, emo, cap, azione="", sfondo="", pos="sx"):
    B.append({"id": "cs_violino_%03d" % i, "personaggio": pers, "testo": testo, "emozione": emo, "audio": "", "azione": azione, "sfondo": sfondo,
              "posizione": pos, "capitolo": cap})


b(1, "amerigo_adulto", "Un cassettino che non avevo mai visto. Dentro, un foglio piegato in quattro: pallini rossi, verdi, blu.", "pensierosa", "ingresso", "dissolvenza", "cs_basso")
b(2, "amerigo_adulto", "Lei non sapeva leggere le lettere. Figuriamoci la musica. E l'ha tenuto lo stesso.", "pensierosa", "ingresso")
b(3, "narratore", "Modena, inverno 1946-47. La neve copre i campi fino all'orizzonte e la nebbia si posa bassa sulle case della corte.", "neutra", "ingresso", "dissolvenza", "cs_ricordo")
b(4, "derna", "Qui la stufa è accesa. Il resto lo impariamo un giorno alla volta.", "neutra", "ingresso", "entra_dx", "", "dx")
b(5, "amerigo", "La neve... è fredda pure dentro le scarpe?", "sorpresa", "ingresso", "entra_sx", "", "sx")
b(6, "derna", "Dentro le scarpe no, se le asciughi vicino alla stufa. Domani te lo faccio vedere.", "felice", "ingresso", "", "", "dx")
b(7, "rosa", "Derna, il bambino ha le guance come due mele. Mandalo da me, che il forno è caldo e c'è da impastare.", "felice", "ingresso", "entra_dx", "", "dx")
b(8, "narratore", "Nella corte ci sono tre porte: la cucina di Derna, il laboratorio dei Benvenuti con il forno e il pianoforte, e in fondo alla strada bianca, la scuola.", "neutra", "ingresso")
b(9, "rivo", "L'ho preso io. Non lo volevo rompere. Volevo solo vedere se lo sapevo suonare anche io.", "triste", "meta", "entra_dx", "cs_ricordo", "dx")
b(10, "amerigo", "E lo sai suonare?", "neutra", "meta", "entra_sx", "", "sx")
b(11, "rivo", "No. Fa un verso da gatto.", "pensierosa", "meta", "", "", "dx")
b(12, "narratore", "I due bambini si guardano le scarpe: quelle nuove della Croce Rossa e quelle col tacco rattoppato. Ridono, prima piano, poi forte.", "neutra", "meta")
b(13, "rivo", "Domani, a scuola, ti batto io il tempo. Così almeno uno dei due fa una cosa giusta.", "felice", "meta", "", "", "dx")
b(14, "narratore", "Una sera di festa in casa Benvenuti: il pane di Rosa sul tavolo, la stufa che borbotta, Luzio e Nario seduti sul pavimento.", "neutra", "chiusura", "dissolvenza", "cs_ricordo")
b(15, "alcide", "Adesso fai sentire a tutti quello che sai. Piano, che la stanza ascolta.", "felice", "chiusura", "entra_dx", "", "dx")
b(16, "narratore", "Amerigo suona le sue due frasi. Nessuno parla. Poi Rosa batte le mani, e tutti dietro.", "neutra", "chiusura")
b(17, "derna", "Adesso scriviamo a tua madre. Tu detti, io scrivo.", "neutra", "chiusura", "entra_dx", "", "dx")
b(18, "amerigo", "Scrivi che mangio. Che ho le scarpe. Che c'è la neve e non è zucchero.", "felice", "chiusura", "entra_sx", "", "sx")
b(19, "amerigo", "E scrivi che... no, questo non lo scrivere. Scrivi solo che sto bene.", "pensierosa", "chiusura", "", "", "sx")
b(20, "derna", "Lo scrivo così. Il resto lo capirà lo stesso.", "neutra", "chiusura", "", "", "dx")
b(21, "amerigo_adulto", "Lei non sapeva leggerla, quella lettera. Qualcuno gliel'avrà letta ad alta voce, nel vicolo.", "pensierosa", "chiusura", "dissolvenza", "cs_basso", "sx")
b(22, "narratore", "Una cucina calda, un foglio di pallini colorati, una lettera dettata. A Modena qualcuno aveva guardato Amerigo e aveva visto un talento: la scuola e la musica, per la prima volta, erano anche sue.", "decisa", "chiusura")
cs = {"id": "cutscene_violino", "titolo": "Il violino", "sfondo": "cs_ricordo", "battute": B}
open(os.path.join(D, "cutscene_violino.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(cs, ensure_ascii=False, indent=2))

# ------------------------------------------------------------------ testi d'ambiente (insegne della corte e della scuola, carte dei segreti)
# lettere ammesse: A-Z, 0-9 e . , ' - ! ? : /  (niente accenti)
d, nl = load("testi_ambiente")
INS = lambda chiave, riga, seed, carta=(0.86, 0.8, 0.62), ink=(0.12, 0.1, 0.08): {
    "chiave": chiave, "gruppo": "ricordo", "tipo": "insegna", "w": 512, "h": 96, "seed": seed, "righe": [riga], "carta": list(carta), "inchiostro": list(ink)}
for t in [
    INS("ScuolaElementare", "SCUOLA ELEMENTARE", 71, (0.84, 0.8, 0.7), (0.16, 0.12, 0.1)),
    INS("Laboratorio", "BENVENUTI - ACCORDATURA PIANOFORTI", 72, (0.3, 0.24, 0.18), (0.88, 0.82, 0.66)),
    INS("Forno", "FORNO", 73),
    {"chiave": "Alfabeto", "gruppo": "ricordo", "tipo": "manifesto", "w": 384, "h": 256, "seed": 74,
     "righe": ["A B C D E", "F G H I L", "M N O P Q", "R S T U V Z"], "carta": [0.88, 0.84, 0.7], "inchiostro": [0.12, 0.1, 0.08]},
    {"chiave": "Lavagna", "gruppo": "ricordo", "tipo": "manifesto", "w": 384, "h": 256, "seed": 75,
     "righe": ["MODENA, 1947", "", "IL PANE", "LA NEVE", "IL TRENO"], "carta": [0.14, 0.16, 0.14], "inchiostro": [0.88, 0.86, 0.8]},
    {"chiave": "CartolinaModena", "gruppo": "segreti", "tipo": "cartolina", "w": 256, "h": 160, "seed": 76,
     "righe": ["SALUTI DA MODENA"], "carta": [0.86, 0.79, 0.6], "inchiostro": [0.27, 0.18, 0.1]},
    {"chiave": "Pagella", "gruppo": "segreti", "tipo": "lista", "w": 256, "h": 320, "seed": 77,
     "righe": ["PAGELLA", "SPERANZA AMERIGO", "CLASSE II", "LETTURA: BUONO", "ARITMETICA: OTTIMO", "CANTO: OTTIMO"], "carta": [0.86, 0.82, 0.68], "inchiostro": [0.12, 0.1, 0.18]},
]:
    upsert(d["testi"], t, "chiave")
save("testi_ambiente", d, nl)

# ------------------------------------------------------------------ mappe
d, nl = load("mappe")


def R(x0, x1, z0, z1):
    return {"x0": x0, "x1": x1, "z0": z0, "z1": z1}


def S(id, nome, tipo, zone, ancore):
    return {"id": id, "nome": nome, "tipo": tipo, "zone": zone, "ancore": [{"x": x, "z": z} for x, z in ancore]}


def scena(sc, titolo, ax0, az0, ax1, az1, ppm, raggio, strade, punti):
    nuova = {"scena": sc, "titolo": titolo, "tema": "ricordo", "ax0": ax0, "az0": az0, "ax1": ax1, "az1": az1, "ppm": ppm, "raggio": raggio,
             "strade": strade, "punti": punti}
    for i, s in enumerate(d["scene"]):
        if s["scena"] == sc:
            nuova["ax0"], nuova["az0"], nuova["ax1"], nuova["az1"] = s["ax0"], s["az0"], s["ax1"], s["az1"]  # MappaBuilder li ha gia' misurati
            d["scene"][i] = nuova
            return
    d["scene"].append(nuova)


# coordinate = SceneBuilderViolino.cs (zone)
scena(MOD, "Modena, inverno 1946-47", -27, -23, 27, 61, 8, 18, [
    S("corte", "La corte dei Benvenuti", "cortile", [R(-6, 6, -12, 12)], [(0, -2)]),
    S("cucina_derna", "La cucina di Derna", "stanza", [R(-15.5, -6.5, -7.5, 1)], [(-11, -3)]),
    S("stanzetta", "La stanzetta di Amerigo", "stanza", [R(-15.5, -6.5, 1, 7.5)], [(-11, 4.2)]),
    S("laboratorio", "Il laboratorio di Alcide", "stanza", [R(6.5, 15.5, -7.5, 7.5)], [(11, 0)]),
    S("strada", "La strada della scuola", "vicolo", [R(-2.5, 2.5, 12, 40)], [(0, 26)]),
    S("scuola", "La scuola", "stanza", [R(-5.5, 5.5, 40.5, 49.5)], [(0, 44)]),
], [
    {"id": "uscita", "glifo": "uscita", "nome": "Il cancello della corte", "x": 0.0, "z": -12.2},
    {"id": "forno", "glifo": "bottega", "nome": "Il forno di Rosa", "x": 9.6, "z": -6.7},
])

P70 = lambda x, z, t: {"scena": MOD, "x": x, "z": z, "testo": t}
SPA = {"scena": "20_Int_BassoAntonietta", "x": 2.07, "z": 6.9, "testo": "Esamina il cassettino sulla credenza della cucina"}
BAS = {"scena": "10_Hub_Vicolo", "x": -9.1, "z": -2.0, "testo": "Torna al basso di Antonietta"}
ob = [
    ("ob_violino_forno", "Aiuta Rosa a infornare il pane", ["enigma_forno"], [P70(9.6, -5.6, "Il forno di Rosa, nel laboratorio dei Benvenuti")]),
    ("ob_violino_attrezzi", "Porta il pane ad Alcide", ["enigma_attrezzi"], [P70(14.0, -1.6, "Il banco dell'accordatore, nel laboratorio")]),
    ("ob_violino_piano", "Accorda il pianoforte", ["enigma_pianoforte"], [P70(14.0, 4.2, "Il pianoforte aperto, nel laboratorio")]),
    ("ob_violino_impronte", "Segui le impronte nella neve", ["enigma_impronte"], [P70(0.0, 8.2, "Le impronte all'imbocco della strada")]),
    ("ob_violino_melodia", "La prima melodia, a scuola", ["enigma_melodia"], [P70(0.0, 46.4, "La cattedra della scuola, in fondo alla strada")]),
]
obs = d["obiettivi"]
for id, testo, comp, pts in ob:
    item = {"id": id, "testo": testo, "capitolo": "flashback_violino", "completo": comp, "punti": pts + [SPA, BAS]}
    for i, o in enumerate(obs):
        if o["id"] == id:
            obs[i] = item
            break
    else:
        obs.append(item)
save("mappe", d, nl)
print("dati aggiornati")
