# -*- coding: utf-8 -*-
"""R016: aggiorna oggetti.json, luoghi.json, personaggi.json, audio.json, mappe.json, ui_menu.json e crea cutscene_finale.json (idempotente).
Uso: python aggiorna_dati_finale.py   (dopo ArtSource/Violino/aggiorna_dati_violino.py: il finale si apre dopo il ricordo del violino)
Testo scritto da zero (TRAMA §8). Niente lettera del film (Decisione 6), niente frase sull'amore che "lascia andare". Carmine breve, nessun dettaglio
giudiziario sui genitori (Decisione 7). Antonietta non scrive e non parla: nel 1994 c'e' solo quello che ha lasciato.
"""
import json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")
FIN = "80_Basso_1994"


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


# ------------------------------------------------------------------ oggetti: il violino (oggetto del finale) e il telefono che lo apre nel basso
d, nl = load("oggetti")
upsert(d["oggetti"], {
    "id": "violino",
    "nome": "Il violino",
    "descrizione": "Un violino piccolo, da bambino, con le corde allentate. Nella custodia, piegato con cura, un ritaglio di giornale di un tuo concerto.",
    "significato": "Il violino che Alcide aveva dato ad Amerigo a Modena, impegnato al Monte dei pegni e poi riscattato da Antonietta, che lo ha tenuto sotto il letto per tutta la vita. Non c'è una lettera: c'è una polizza firmata con una croce e un ritaglio di giornale che lei non sapeva leggere.",
    "temaCivico": "Famiglia, affido e genitorialità scelta: art. 30 della Costituzione e legge 4 maggio 1983 n. 184 «Diritto del minore ad una famiglia» (modificata dalla legge 149 del 2001).",
    "icona": "scarpe",
    "flashback": "flashback_finale",
    "cutscene": "cutscene_finale",
    "cutsceneIngresso": "cutscene_finale",
    "luogo": "basso_antonietta",
    "titoliDiCoda": True,
    "ingresso": {
        "scena": "20_Int_BassoAntonietta", "x": 3.6, "y": 0.0, "z": -1.95, "yaw": 180,
        "richiedeFlashback": "flashback_violino",
        "prompt": "Esamina: il telefono che squilla",
        "parti": [
            {"id": "mobiletto", "forma": "cubo", "materiale": "legno", "colore": "#4a3a2c", "y": 0.35, "sx": 0.6, "sy": 0.7, "sz": 0.4},
            {"id": "telefono", "forma": "cubo", "materiale": "metallo", "colore": "#5a5e62", "x": -0.12, "y": 0.76, "sx": 0.2, "sy": 0.1, "sz": 0.18},
            {"id": "cornetta", "forma": "capsula", "materiale": "metallo", "colore": "#4a4e52", "x": -0.12, "y": 0.83, "rz": 90, "sx": 0.05, "sy": 0.1, "sz": 0.05},
            {"id": "disco", "forma": "cilindro", "materiale": "metallo", "colore": "#d8d4c8", "x": -0.12, "y": 0.812, "z": -0.05, "rx": -20, "sx": 0.08, "sy": 0.004, "sz": 0.08},
        ],
    },
})
save("oggetti", d, nl)

# ------------------------------------------------------------------ luoghi
d, nl = load("luoghi")
upsert(d["luoghi"], {
    "id": "ricordo_finale", "nome": "Il basso, 1994", "scena": FIN, "spawn": "", "aperto": False,
    "descrizione": "Il basso di Antonietta nel marzo 1994, dopo la sua morte: mobili sotto le lenzuola, una lampadina nuda, scatoloni, un telefono che squilla.",
    "meteo": "",
})
save("luoghi", d, nl)

# ------------------------------------------------------------------ personaggi
d, nl = load("personaggi")
upsert(d["personaggi"], {"id": "carmine", "nome": "Carmine", "ritratto": "", "colore": "#4A5A6A"})
save("personaggi", d, nl)

# ------------------------------------------------------------------ audio (il presente: silenzio del basso, il vicolo fuori)
d, nl = load("audio")
upsert(d["scene"], {"scena": FIN, "musica": "", "ambiente": ["interno_silenzio", "vicolo_brusio"], "sparsi": ["campane"],
                    "intervallo": [40, 90], "superficie": "pietra"}, "scena")
save("audio", d, nl)

# ------------------------------------------------------------------ cutscene (capitoli: ingresso, meta dopo la custodia, chiusura, segreti = scena in piu')
B = []


def b(i, pers, testo, emo, cap, azione="", sfondo="", pos="sx"):
    B.append({"id": "cs_finale_%03d" % i, "personaggio": pers, "testo": testo, "emozione": emo, "audio": "", "azione": azione, "sfondo": sfondo,
              "posizione": pos, "capitolo": cap})


b(1, "amerigo_adulto", "Il telefono squilla. In un basso dove non abita più nessuno.", "pensierosa", "ingresso", "dissolvenza", "cs_basso")
b(2, "amerigo_adulto", "Così era cominciato, a marzo: una telefonata, una voce che non conoscevo, il primo treno per Napoli.", "pensierosa", "ingresso")
b(3, "narratore", "Napoli, 1994. Lo stesso basso, cinquant'anni dopo: le lenzuola sui mobili, una lampadina nuda, gli scatoloni da riempire.", "neutra", "ingresso", "dissolvenza", "cs_basso")
b(4, "amerigo_adulto", "Ho suonato in teatri dove c'era più gente che in tutto il vicolo. Qui dentro non so da dove cominciare.", "pensierosa", "ingresso")
b(5, "narratore", "Su un mobiletto, accanto alla porta, le ultime cose di Antonietta. Il telefono ha smesso di squillare.", "neutra", "ingresso")
b(6, "amerigo_adulto", "Ha suonato con le corde allentate da cinquant'anni. Male. Bene.", "commossa", "meta", "dissolvenza", "cs_basso")
b(7, "narratore", "Amerigo accorda il violino a mezzi scatti, come gli aveva insegnato Alcide, e suona le due frasi dei pallini colorati.", "neutra", "meta")
b(8, "narratore", "Per un attimo la stanza è tutta 1946: il lume a petrolio, i panni stesi, una donna di spalle che si ferma ad ascoltare.", "neutra", "meta", "dissolvenza", "cs_ricordo")
b(9, "amerigo_adulto", "Non si è voltata. Nemmeno allora si voltava, quando le volevo bene di più.", "commossa", "meta", "dissolvenza", "cs_basso")
b(10, "carmine", "Zio. La porta era aperta.", "neutra", "chiusura", "entra_dx", "cs_basso", "dx")
b(11, "amerigo_adulto", "Lo è sempre stata. Entra.", "felice", "chiusura", "entra_sx", "", "sx")
b(12, "narratore", "Si siedono sul gradino, uno accanto all'altro. Il vicolo fa il suo rumore: i motorini, una radio, qualcuno che chiama un nome dalla finestra.", "neutra", "chiusura")
b(13, "carmine", "Domani torni al Nord?", "pensierosa", "chiusura", "", "", "dx")
b(14, "amerigo_adulto", "Domani sì. Ma questo basso non lo vendo. E tu, se vuoi, il violino lo puoi venire a provare qui. Ogni volta che torno.", "decisa", "chiusura", "", "", "sx")
b(15, "amerigo_adulto", "Cinquant'anni fa una donna di Modena ha guardato un bambino scalzo e ha deciso di tenerlo con sé. Mia madre lo ha lasciato partire, e poi ha aspettato.", "commossa", "chiusura", "dissolvenza", "cs_basso", "sx")
b(16, "amerigo_adulto", "Ho avuto due madri. Nessuna delle due mi ha chiesto di scegliere. L'ho fatto io, tante volte, e ogni volta era giusto.", "commossa", "chiusura")
b(17, "narratore", "Tra il 1945 e il 1952 circa settantamila bambini del Sud e del Centro viaggiarono sui treni della felicità, accolti da famiglie che spesso non avevano molto più di loro.", "neutra", "chiusura", "dissolvenza", "cs_ricordo")
b(18, "narratore", "Oggi la Costituzione (art. 30) e la legge n. 184 del 1983 dicono che ogni bambino ha diritto a crescere in una famiglia: la sua, o un'altra che lo accolga.", "decisa", "chiusura")
b(19, "amerigo_adulto", "Carmine. C'è una scatola che voglio darti. L'ho riempita girando per il vicolo, un pezzetto alla volta.", "felice", "segreti", "dissolvenza", "cs_basso", "sx")
b(20, "carmine", "Che c'è dentro?", "sorpresa", "segreti", "entra_dx", "", "dx")
b(21, "amerigo_adulto", "I ragazzi del vicolo. Quelli che sono partiti e quelli che sono restati. Adesso la tieni tu.", "felice", "segreti", "", "", "sx")
b(22, "narratore", "Hai trovato tutti i segreti del gioco. Grazie per aver guardato in ogni angolo.", "felice", "segreti")
cs = {"id": "cutscene_finale", "titolo": "Il violino ritrovato", "sfondo": "cs_basso", "battute": B}
open(os.path.join(D, "cutscene_finale.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(cs, ensure_ascii=False, indent=2))

# ------------------------------------------------------------------ titoli di coda (ui_menu.json, blocco "finale")
d, nl = load("ui_menu")
d["finale"] = {
    "titolo": "Il treno dei bambini",
    "sottotitolo": "Napoli, 1946 - Modena, 1947 - Napoli, 1994",
    "righe": [
        "Ispirato al romanzo di Viola Ardone (2019) e al film di Cristina Comencini (2024).",
        "Personaggi, dialoghi, enigmi e melodie sono originali, scritti per questo gioco.",
        "Ai bambini dei treni della felicità, e alle famiglie che li hanno accolti.",
    ],
    "fine": "Fine",
    "salta": "Tieni premuto per saltare",
    "velocita": 55,
}
save("ui_menu", d, nl)

# ------------------------------------------------------------------ mappe (stesso impianto del basso)
d, nl = load("mappe")


def R(x0, x1, z0, z1):
    return {"x0": x0, "x1": x1, "z0": z0, "z1": z1}


def S(id, nome, tipo, zone, ancore):
    return {"id": id, "nome": nome, "tipo": tipo, "zone": zone, "ancore": [{"x": x, "z": z} for x, z in ancore]}


nuova = {"scena": FIN, "titolo": "Il basso, 1994", "tema": "normale", "ax0": -8, "az0": -6, "ax1": 10, "az1": 18, "ppm": 24, "raggio": 8,
         "strade": [S("stanza", "La stanza", "stanza", [R(-4.5, 4.5, -2.5, 3.5)], [(0, 0.5)]),
                    S("alcova", "L'alcova", "stanza", [R(-4.5, -1.5, 3.8, 7.8)], [(-3, 5.8)]),
                    S("cucina", "La cucina", "stanza", [R(1.5, 4.5, 3.8, 7.8)], [(3, 5.8)]),
                    S("cortile", "Il cortile", "cortile", [R(1.2, 6.3, 8.1, 14.1)], [(3.7, 11)])],
         "punti": [{"id": "uscita", "glifo": "uscita", "nome": "La porta sul vicolo", "x": 0.0, "z": -2.4}]}
for i, s in enumerate(d["scene"]):
    if s["scena"] == FIN:
        for k in ("ax0", "az0", "ax1", "az1"): nuova[k] = s[k]
        d["scene"][i] = nuova
        break
else:
    d["scene"].append(nuova)

P80 = lambda x, z, t: {"scena": FIN, "x": x, "z": z, "testo": t}
TEL = {"scena": "20_Int_BassoAntonietta", "x": 3.6, "z": -1.5, "testo": "Il telefono che squilla, accanto alla porta"}
BAS = {"scena": "10_Hub_Vicolo", "x": -9.1, "z": -2.0, "testo": "Torna al basso di Antonietta"}
ob = [
    ("ob_finale_basso", "Guarda le cose di tua madre", ["enigma_basso94"], [P80(3.6, -1.5, "Il telefono e la scatola di latta, accanto alla porta")]),
    ("ob_finale_posti", "Rimetti i ricordi al loro posto", ["enigma_posto_scarpe", "enigma_posto_stracci", "enigma_posto_volantino", "enigma_posto_cappotto", "enigma_posto_spartito"],
     [P80(0.6, -1.6, "La soglia"), P80(-3.6, 2.0, "La credenza"), P80(-3.85, 6.6, "Il letto dell'alcova"), P80(3.7, 9.8, "La cesta del cortile")]),
    ("ob_finale_ricevuta", "Ricomponi la carta strappata", ["enigma_ricevuta"], [P80(2.3, 0.6, "Il tavolo sotto la lampadina")]),
    ("ob_finale_custodia", "Guarda sotto il letto", ["enigma_custodia"], [P80(-3.0, 5.6, "Il letto di ferro dell'alcova")]),
    ("ob_finale_carmine", "Apri a chi bussa", ["enigma_carmine"], [P80(-1.3, -1.5, "La porta sul vicolo")]),
]
obs = d["obiettivi"]
for id, testo, comp, pts in ob:
    item = {"id": id, "testo": testo, "capitolo": "flashback_finale", "completo": comp, "punti": pts + [TEL, BAS]}
    for i, o in enumerate(obs):
        if o["id"] == id:
            obs[i] = item
            break
    else:
        obs.append(item)
save("mappe", d, nl)
print("dati aggiornati")
