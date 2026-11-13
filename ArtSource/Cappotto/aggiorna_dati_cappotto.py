# -*- coding: utf-8 -*-
"""R014: aggiorna oggetti.json, luoghi.json, personaggi.json, audio.json, mappe.json, testi_ambiente.json e crea cutscene_cappotto.json (idempotente).
Uso: python aggiorna_dati_cappotto.py   (dopo ArtSource/Volantino/aggiorna_dati_volantino.py: il cappottino si trova dopo il volantino)
Dialoghi originali (TRAMA §8), mai frasi del libro o del film. La mela annurca e' solo un dettaglio (Decisione 9). Chi da' le scarpe non si dice (Decisione 12).
La neve "scambiata per ricotta" NON e' usata (incerta, TRAMA §4 e VERIFICHE_STORICHE §5).
"""
import json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")
STAZ, VAG = "60_FB_Stazione", "61_FB_Vagone"


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
# il cappottino di lana appeso a un chiodo dietro la porta del basso (muro sud, lato interno, a destra della porta guardando il vicolo)
d, nl = load("oggetti")
upsert(d["oggetti"], {
    "id": "cappotto",
    "nome": "Il cappotto",
    "descrizione": "Un cappottino di lana blu, lavato tante volte, con una toppa sul gomito. Su un talloncino cucito dentro, a matita: 112. Le maniche arrivano appena al polso di un bambino di sette anni.",
    "significato": "Il cappotto della Croce Rossa che Amerigo indossò per il viaggio verso il Nord. Antonietta l'ha tenuto appeso dietro la porta per tutta la vita, come se il figlio potesse tornare a prenderlo da un momento all'altro. È il ricordo di una solidarietà fatta di cose concrete: un numero, un cappotto, un paio di scarpe, una mela.",
    "temaCivico": "Solidarietà (art. 2 della Costituzione) e accoglienza: i bambini partivano lavati, visitati, vestiti e schedati; il viaggio unì il Sud e il Nord di un'Italia sola (artt. 3 e 5).",
    "icona": "scarpe",
    "flashback": "flashback_cappotto",
    "cutscene": "cutscene_cappotto",
    "cutsceneIngresso": "cutscene_cappotto",
    "luogo": "basso_antonietta",
    "ingresso": {
        "scena": "20_Int_BassoAntonietta", "x": 1.15, "y": 1.1, "z": -2.43, "yaw": 180,
        "richiedeFlashback": "flashback_volantino",
        "prompt": "Esamina: un cappottino appeso dietro la porta",
        "parti": [
            {"id": "chiodo", "forma": "cilindro", "materiale": "metallo", "colore": "#3a3632", "y": 0.62, "z": 0.02, "rx": 90, "sx": 0.012, "sy": 0.03, "sz": 0.012},
            {"id": "corpo", "forma": "cubo", "materiale": "stoffa", "colore": "#3f4a57", "y": 0.3, "sx": 0.3, "sy": 0.44, "sz": 0.05},
            {"id": "bavero", "forma": "cubo", "materiale": "stoffa", "colore": "#36404c", "y": 0.5, "z": -0.028, "sx": 0.18, "sy": 0.06, "sz": 0.01},
            {"id": "manica_sx", "forma": "cilindro", "materiale": "stoffa", "colore": "#3f4a57", "x": -0.17, "y": 0.29, "rz": -8, "sx": 0.065, "sy": 0.17, "sz": 0.065},
            {"id": "manica_dx", "forma": "cilindro", "materiale": "stoffa", "colore": "#3f4a57", "x": 0.17, "y": 0.29, "rz": 8, "sx": 0.065, "sy": 0.17, "sz": 0.065},
            {"id": "toppa", "forma": "cubo", "materiale": "stoffa", "colore": "#7a6a52", "x": 0.17, "y": 0.2, "z": -0.035, "sx": 0.05, "sy": 0.05, "sz": 0.005},
            {"id": "bottone_a", "forma": "sfera", "materiale": "legno", "colore": "#2a2018", "x": -0.02, "y": 0.4, "z": -0.028, "sx": 0.018, "sy": 0.018, "sz": 0.008},
            {"id": "bottone_b", "forma": "sfera", "materiale": "legno", "colore": "#2a2018", "x": -0.02, "y": 0.28, "z": -0.028, "sx": 0.018, "sy": 0.018, "sz": 0.008},
            {"id": "bottone_c", "forma": "sfera", "materiale": "legno", "colore": "#2a2018", "x": -0.02, "y": 0.16, "z": -0.028, "sx": 0.018, "sy": 0.018, "sz": 0.008},
        ],
    },
})
save("oggetti", d, nl)

# ------------------------------------------------------------------ luoghi
d, nl = load("luoghi")
for l in d["luoghi"]:
    if l["id"] == "stazione":
        l["descrizione"] = "La stazione da cui, nell'inverno 1946-47, partivano i treni dei bambini verso il Nord. Oggi il cancello è chiuso: quella partenza si ritrova solo nel ricordo del cappotto."
        l["sbloccatoDa"] = "volantino"
        l["sbloccatoMsg"] = "Il cancello della stazione è chiuso: i treni del 1946 si ritrovano solo nei ricordi. Il viaggio, però, Antonietta l'aveva preparato in casa: dietro la porta del basso c'è ancora appeso un cappottino."
upsert(d["luoghi"], {
    "id": "ricordo_cappotto", "nome": "Il ricordo del cappotto", "scena": STAZ, "spawn": "", "aperto": False,
    "descrizione": "Una grande stazione nell'inverno 1946-47, ispirata alla vecchia stazione di Napoli: tavoli del Comitato e della Croce Rossa, montagne di cappotti, fumo e una carrozza di terza classe pronta a partire.",
    "meteo": "sereno",
})
upsert(d["luoghi"], {
    "id": "ricordo_vagone", "nome": "Il treno per il Nord", "scena": VAG, "spawn": "salita", "aperto": True,
    "descrizione": "La carrozza di terza classe numero 214: panche di legno, reticelle piene di valigie di cartone, finestrini da cui scorre l'Italia.",
    "richiede": ["partenza"],
    "richiedeMsg": ["Il capotreno ti ferma sul predellino: «Prima saluta tua madre, guagliò.» Antonietta ti aspetta sulla banchina, con qualcosa in mano."],
    "aperturaMsg": "Sali sul predellino: il treno sta per partire.",
    "meteo": "",
})
upsert(d["luoghi"], {
    "id": "modena", "nome": "Modena, casa Benvenuti", "scena": "", "spawn": "", "aperto": False,
    "descrizione": "Una corte di pianura vicino a Modena, d'inverno: la casa di Derna, il laboratorio di Alcide, la scuola in fondo alla strada.",
    "sbloccatoDa": "cappotto",
    "sbloccatoMsg": "Modena è lontana da questo vicolo. Il ricordo dell'inverno al Nord comincia da un'altra cosa che Antonietta ha conservato: si aprirà più avanti.",
})
save("luoghi", d, nl)

# ------------------------------------------------------------------ personaggi
d, nl = load("personaggi")
upsert(d["personaggi"], {"id": "mariuccia", "nome": "Mariuccia", "ritratto": "", "colore": "#9A6A58"})
upsert(d["personaggi"], {"id": "controllore", "nome": "Il controllore", "ritratto": "", "colore": "#3E4652"})
save("personaggi", d, nl)

# ------------------------------------------------------------------ audio
d, nl = load("audio")
upsert(d["scene"], {"scena": STAZ, "musica": "tema_amerigo_ricordo", "ambiente": ["ricordo_tono", "stazione_vapore"], "sparsi": ["treno_fischio"],
                    "intervallo": [14, 32], "superficie": "ovattato"}, "scena")
upsert(d["scene"], {"scena": VAG, "musica": "tema_amerigo_ricordo", "ambiente": ["ricordo_tono", "stazione_vapore"], "sparsi": ["treno_fischio"],
                    "intervallo": [35, 70], "superficie": "ovattato"}, "scena")
save("audio", d, nl)

# ------------------------------------------------------------------ cutscene (capitoli: ingresso, meta dopo i cappotti, chiusura)
B = []


def b(i, pers, testo, emo, cap, azione="", sfondo="", pos="sx"):
    B.append({"id": "cs_cappotto_%03d" % i, "personaggio": pers, "testo": testo, "emozione": emo, "audio": "", "azione": azione, "sfondo": sfondo,
              "posizione": pos, "capitolo": cap})


b(1, "amerigo_adulto", "Dietro la porta, su un chiodo, un cappottino blu. Ci entrerebbe a stento il mio braccio.", "pensierosa", "ingresso", "dissolvenza", "cs_basso")
b(2, "amerigo_adulto", "Provo a infilare la mano nella manica. Si ferma al polso. Allora ci stavo dentro tutto.", "pensierosa", "ingresso")
b(3, "narratore", "Napoli, inverno 1946-47. In stazione il fumo delle locomotive si mescola all'odore di sapone: i bambini sono stati lavati, visitati e vestiti, uno per uno.", "neutra", "ingresso", "dissolvenza", "cs_ricordo")
b(4, "antonietta", "Stai dritto e non fare lo scemo. Quando chiamano il tuo numero, rispondi forte.", "decisa", "ingresso", "entra_dx", "", "dx")
b(5, "amerigo", "E se il numero me lo scordo, mammà?", "neutra", "ingresso", "entra_sx", "", "sx")
b(6, "antonietta", "Il numero non si scorda. Si porta al collo.", "neutra", "ingresso", "", "", "dx")
b(7, "tommasino", "Amerì! Hanno dato i cartellini, ma è passato il vento dal binario e li ha fatti volare tutti!", "felice", "ingresso", "entra_dx", "", "dx")
b(8, "narratore", "Ai tavoli del Comitato e della Croce Rossa le volontarie chiamano, scrivono, piegano. Prima di salire servono un numero, un cappotto e un saluto.", "neutra", "ingresso")
b(9, "amerigo", "Mammà... guarda qua sotto. Sono scarpe. Sono mie.", "sorpresa", "meta", "zoom", "cs_ricordo", "sx")
b(10, "narratore", "Amerigo non cammina: prova il pavimento un passo alla volta, come se le scarpe potessero scappare.", "neutra", "meta")
b(11, "pachiochia", "Partite, partite! Tanto quei bambini non li rivedete più!", "triste", "meta", "entra_dx", "", "dx")
b(12, "madre_vicolo", "Pachiò, l'hai detto pure la settimana scorsa. Io il registro l'ho letto.", "decisa", "meta", "", "", "sx")
b(13, "narratore", "Nessuno si gira. La voce della Pachiochia si perde nel fischio di una locomotiva.", "neutra", "meta")
b(14, "narratore", "È ancora buio quando il treno si ferma, l'ultima volta. Sulla banchina, sotto le lanterne, aspettano famiglie con le sciarpe fino agli occhi.", "neutra", "chiusura", "dissolvenza", "cs_ricordo")
b(15, "controllore", "Modena! Scendete piano, guagliù, e tenete stretti i cappotti.", "felice", "chiusura", "entra_dx", "", "dx")
b(16, "amerigo", "Tommasì, guarda fuori. È tutto bianco.", "sorpresa", "chiusura", "entra_sx", "", "sx")
b(17, "tommasino", "Pare zucchero. Ma io non lo assaggio prima di te.", "felice", "chiusura", "", "", "dx")
b(18, "mariuccia", "Io non scendo se non mi chiamano con il nome. Il numero non mi piace.", "pensierosa", "chiusura", "entra_dx", "", "dx")
b(19, "narratore", "Una donna con il fazzoletto in testa legge un foglio. Chiama i nomi, non i numeri.", "neutra", "chiusura")
b(20, "amerigo", "Amerigo Speranza. Sono io.", "decisa", "chiusura", "", "", "sx")
b(21, "amerigo_adulto", "Quel cappotto l'ho stretto per tutto il viaggio. Non perché avevo freddo.", "pensierosa", "chiusura", "dissolvenza", "cs_basso", "sx")
b(22, "narratore", "Un cappotto di sconosciuti, un paio di scarpe senza biglietto, una mela da tenere. Il viaggio verso il Nord comincia così: con quello che altri avevano messo da parte per te.", "decisa", "chiusura")
cs = {"id": "cutscene_cappotto", "titolo": "Il cappotto", "sfondo": "cs_ricordo", "battute": B}
open(os.path.join(D, "cutscene_cappotto.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(cs, ensure_ascii=False, indent=2))

# ------------------------------------------------------------------ testi d'ambiente (insegne della stazione e della carrozza, carte dei segreti)
# lettere ammesse: A-Z, 0-9 e . , ' - ! ? : /  (niente accenti)
d, nl = load("testi_ambiente")
INS = lambda chiave, riga, seed, carta=(0.86, 0.8, 0.62), ink=(0.12, 0.1, 0.08): {
    "chiave": chiave, "gruppo": "ricordo", "tipo": "insegna", "w": 512, "h": 96, "seed": seed, "righe": [riga], "carta": list(carta), "inchiostro": list(ink)}
for t in [
    INS("StazioneNapoli", "NAPOLI", 61, (0.2, 0.22, 0.24), (0.88, 0.84, 0.72)),
    INS("Binario1", "BINARIO 1", 62, (0.2, 0.22, 0.24), (0.88, 0.84, 0.72)),
    INS("Binario2", "BINARIO 2", 63, (0.2, 0.22, 0.24), (0.88, 0.84, 0.72)),
    INS("SalaAspetto", "SALA D'ASPETTO - III CLASSE", 64, (0.2, 0.22, 0.24), (0.88, 0.84, 0.72)),
    INS("ComitatoBambini", "COMITATO PER I BAMBINI", 65),
    INS("CroceRossa", "CROCE ROSSA - VESTIARIO", 66, (0.9, 0.88, 0.82), (0.6, 0.12, 0.1)),
    INS("Carrozza214", "III CLASSE   N. 214", 67, (0.24, 0.26, 0.22), (0.86, 0.82, 0.68)),
    INS("PericoloSporgersi", "E' PERICOLOSO SPORGERSI", 68, (0.86, 0.82, 0.7), (0.16, 0.12, 0.1)),
    {"chiave": "AvvisoPartenza", "gruppo": "ricordo", "tipo": "manifesto", "w": 256, "h": 384, "seed": 69,
     "righe": ["AVVISO", "", "I BAMBINI", "IN PARTENZA", "SI PRESENTINO", "AL TAVOLO", "DEL COMITATO", "CON LA MADRE", "E IL CARTELLINO"], "carta": [0.87, 0.81, 0.63], "inchiostro": [0.12, 0.1, 0.08]},
    {"chiave": "CartolinaFirenze", "gruppo": "segreti", "tipo": "cartolina", "w": 256, "h": 160, "seed": 70,
     "righe": ["SALUTI DA FIRENZE"], "carta": [0.86, 0.79, 0.6], "inchiostro": [0.27, 0.18, 0.1]},
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


scena(STAZ, "La stazione, inverno 1946-47", -26, -27, 26, 62, 8, 18, [
    S("atrio", "L'atrio della stazione", "stanza", [R(-15, 15, -16, 0)], [(0, -8)]),
    S("testata", "La testata dei binari", "largo", [R(-11, 11, 0, 4)], [(0, 2)]),
    S("banchina", "La banchina del binario 1", "vicolo", [R(-4, 4, 4, 44)], [(0, 24)]),
], [
    {"id": "treno", "glifo": "stazione", "nome": "Carrozza 214", "x": -4.2, "z": 17.6},
])
scena(VAG, "La carrozza 214", -6, -6, 6, 24, 24, 8, [
    S("vestibolo_sud", "Il vestibolo", "stanza", [R(-1.4, 1.4, -2.5, 0)], [(0, -1.2)]),
    S("carrozza", "La carrozza di terza classe", "stanza", [R(-1.4, 1.4, 0, 18)], [(0, 9)]),
    S("vestibolo_nord", "Il vestibolo della discesa", "stanza", [R(-1.4, 1.4, 18, 20.5)], [(0, 19.3)]),
], [
    {"id": "uscita", "glifo": "uscita", "nome": "La porta della discesa", "x": 0.0, "z": 20.2},
])

P60 = lambda x, z, t: {"scena": STAZ, "x": x, "z": z, "testo": t}
P61 = lambda x, z, t: {"scena": VAG, "x": x, "z": z, "testo": t}
CAP = {"scena": "20_Int_BassoAntonietta", "x": 1.15, "z": -2.1, "testo": "Esamina il cappottino appeso dietro la porta"}
BAS = {"scena": "10_Hub_Vicolo", "x": -9.1, "z": -2.0, "testo": "Torna al basso di Antonietta"}
ob = [
    ("ob_cappotto_cartellini", "Ritrova i cartellini", ["enigma_cartellini"], [P60(-8.0, -7.9, "Il tavolo del Comitato: il registro e i cartellini da appendere")]),
    ("ob_cappotto_cappotti", "Un cappotto per ognuno", ["enigma_cappotti"], [P60(8.0, -7.9, "Il mucchio di cappotti della Croce Rossa")]),
    ("ob_cappotto_mela", "Saluta Antonietta", ["enigma_mela"], [P60(-2.4, 13.2, "Antonietta ti aspetta sulla banchina, accanto alla carrozza 214")]),
    ("ob_cappotto_finestrino", "Segna le città dal finestrino", ["enigma_finestrino"], [P61(-0.9, 17.2, "La carta della linea, in fondo alla carrozza"),
                                                                                        P60(-3.9, 17.6, "Sali sulla carrozza 214")]),
    ("ob_cappotto_paura", "Tranquillizza i bambini", ["enigma_paura"], [P61(0.0, 8.4, "I bambini dello scompartimento, a metà carrozza")]),
]
obs = d["obiettivi"]
for id, testo, comp, pts in ob:
    item = {"id": id, "testo": testo, "capitolo": "flashback_cappotto", "completo": comp, "punti": pts + [CAP, BAS]}
    for i, o in enumerate(obs):
        if o["id"] == id:
            obs[i] = item
            break
    else:
        obs.append(item)
save("mappe", d, nl)
print("dati aggiornati")
