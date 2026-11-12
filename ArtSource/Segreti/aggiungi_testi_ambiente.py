# -*- coding: utf-8 -*-
"""
R011: aggiunge (o rinnova) il gruppo "segreti" in Assets/_Game/Resources/Data/testi_ambiente.json.
Sono i testi e i parametri delle texture dei pezzi di carta dei segreti (ritaglio, cartolina, AM-lire, settebello, biglietti, lista).
Le texture si disegnano con Treno > Segreti > Genera texture dei pezzi (SceneBuilderSegreti.cs), che usa lo stesso carattere a matrice 5x7 degli altri testi d'ambiente.
Lettere ammesse: A-Z, 0-9 e . , ' - ! ? : /
"""
import json
import os

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
P = os.path.join(RADICE, "Assets", "_Game", "Resources", "Data", "testi_ambiente.json")

NUOVI = [
    {"chiave": "Ritaglio", "gruppo": "segreti", "tipo": "ritaglio", "w": 256, "h": 320, "seed": 11,
     "righe": ["PARTONO I BAMBINI", "16 DICEMBRE 1945",
               "UN TRENO CARICO", "DI BAMBINI LASCIA", "MILANO E ARRIVA", "A REGGIO EMILIA.", "SONO CIRCA 1800", "I PICCOLI VIAG-", "GIATORI, ACCOLTI", "DA FAMIGLIE.",
               "A NAPOLI UN", "COMITATO PER LA", "SALVEZZA DEI", "BAMBINI PREPARA", "LE PARTENZE:", "LISTE, VISITE,", "VESTITI CALDI."],
     "carta": [0.84, 0.79, 0.62], "inchiostro": [0.12, 0.1, 0.08]},
    {"chiave": "Cartolina", "gruppo": "segreti", "tipo": "cartolina", "w": 256, "h": 160, "seed": 12, "righe": ["NAPOLI"],
     "carta": [0.86, 0.77, 0.58], "inchiostro": [0.27, 0.18, 0.1]},
    {"chiave": "AmLire", "gruppo": "segreti", "tipo": "amlire", "w": 256, "h": 112, "seed": 13, "righe": ["AM LIRE", "ALLIED MILITARY CURRENCY"],
     "carta": [0.66, 0.74, 0.58], "inchiostro": [0.16, 0.27, 0.17]},
    {"chiave": "Settebello", "gruppo": "segreti", "tipo": "settebello", "w": 96, "h": 160, "seed": 14, "righe": ["7"],
     "carta": [0.86, 0.8, 0.65], "inchiostro": [0.5, 0.13, 0.1]},
    {"chiave": "BigliettoScarpa", "gruppo": "segreti", "tipo": "biglietto_scarpa", "w": 128, "h": 96, "seed": 15, "righe": [],
     "carta": [0.82, 0.74, 0.58], "inchiostro": [0.13, 0.12, 0.11]},
    {"chiave": "BigliettoMela", "gruppo": "segreti", "tipo": "biglietto_mela", "w": 128, "h": 96, "seed": 16, "righe": [],
     "carta": [0.82, 0.74, 0.58], "inchiostro": [0.13, 0.12, 0.11]},
    {"chiave": "BigliettoTreno", "gruppo": "segreti", "tipo": "biglietto_treno", "w": 128, "h": 96, "seed": 17, "righe": [],
     "carta": [0.82, 0.74, 0.58], "inchiostro": [0.13, 0.12, 0.11]},
    {"chiave": "ListaSpesa", "gruppo": "segreti", "tipo": "lista", "w": 128, "h": 160, "seed": 18,
     "righe": ["SCARPE", "6 - 7 - 8", "COLOMBA", "PEPPINO", "NUNZIA", "4 PAIA", "(ANCHE USATE)"],
     "carta": [0.8, 0.76, 0.62], "inchiostro": [0.2, 0.2, 0.28]},
]

d = json.load(open(P, encoding="utf-8"))
d["testi"] = [t for t in d["testi"] if t.get("gruppo") != "segreti"] + NUOVI
s = json.dumps(d, ensure_ascii=False, indent=2).replace("\n", "\r\n") + "\r\n"
open(P, "w", encoding="utf-8", newline="").write(s)
print("testi_ambiente.json aggiornato:", len(d["testi"]), "testi")
