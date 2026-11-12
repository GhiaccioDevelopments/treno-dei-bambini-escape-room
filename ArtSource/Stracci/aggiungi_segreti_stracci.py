# -*- coding: utf-8 -*-
"""R012: inserisce in ArtSource/Segreti/genera_segreti.py il blocco dei segreti del ricordo "Stracci" e il testo del ritaglio in testi_ambiente.json (idempotente).
Il blocco vive in ArtSource/Stracci/segreti_stracci_blocco.py (sorgente); questo script lo incolla prima del commento "ricompense dell'enigma campana".
Uso: python aggiungi_segreti_stracci.py, poi python ../Segreti/genera_segreti.py"""
import json, os

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
p = os.path.join(RADICE, "Assets", "_Game", "Resources", "Data", "testi_ambiente.json")
raw = open(p, encoding="utf-8").read()
d = json.loads(raw)
if not any(t["chiave"] == "RitaglioStracci" for t in d["testi"]):
    d["testi"].append({"chiave": "RitaglioStracci", "gruppo": "segreti", "tipo": "ritaglio", "w": 256, "h": 320, "seed": 17,
                       "righe": ["PICCOLI LAVORI", "", "NELLE CITTA DEL", "DOPOGUERRA TANTI", "BAMBINI AIUTANO", "LE FAMIGLIE:", "RACCOLGONO", "STRACCI E FERRI", "VECCHI E LI",
                                 "PORTANO A CHI", "LI COMPRA.", "", "SCUOLA? A VOLTE", "NON C'E TEMPO."],
                       "carta": [0.84, 0.79, 0.62], "inchiostro": [0.12, 0.1, 0.08]})
    open(p, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, indent=2) + ("\n" if raw.endswith("\n") else ""))

g = os.path.join(RADICE, "ArtSource", "Segreti", "genera_segreti.py")
s = open(g, encoding="utf-8").read()
if "STR = " not in s:
    s = s.replace('''    "30_FB_Scarpe": "Docs/richieste/media/R006/punti_segreti_flashback.md",
}
HUB, BASSO, RIC = "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe"''', '''    "30_FB_Scarpe": "Docs/richieste/media/R006/punti_segreti_flashback.md",
    "40_FB_Stracci": "Docs/richieste/media/R012/punti_segreti_stracci.md",
}
HUB, BASSO, RIC, STR = "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe", "40_FB_Stracci"''')
    blocco = open(os.path.join(os.path.dirname(__file__), "segreti_stracci_blocco.py"), encoding="utf-8").read()
    marker = "# ricompense dell'enigma campana"
    assert marker in s
    s = s.replace(marker, blocco + "\n" + marker, 1)
    open(g, "w", encoding="utf-8").write(s)
print("ok")
