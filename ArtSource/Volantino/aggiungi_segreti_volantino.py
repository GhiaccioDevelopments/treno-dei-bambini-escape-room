# -*- coding: utf-8 -*-
"""R013: inserisce in ArtSource/Segreti/genera_segreti.py il blocco dei segreti del ricordo "Il volantino del treno" (idempotente).
Il blocco vive in ArtSource/Volantino/segreti_volantino_blocco.py (sorgente); questo script lo incolla prima del commento "ricompense dell'enigma campana".
I punti stanno in Docs/richieste/media/R013/punti_segreti_volantino.md (lo riscrive SceneBuilderVolantino quando si costruisce la scena: stessi nomi e stesse coordinate).
Uso: python aggiungi_segreti_volantino.py, poi python ../Segreti/genera_segreti.py"""
import os

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
g = os.path.join(RADICE, "ArtSource", "Segreti", "genera_segreti.py")
raw = open(g, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
s = raw.replace("\r\n", "\n")
if "VOL = " not in s:
    vecchio = ('    "40_FB_Stracci": "Docs/richieste/media/R012/punti_segreti_stracci.md",\n}\n'
               'HUB, BASSO, RIC, STR = "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe", "40_FB_Stracci"')
    assert vecchio in s
    s = s.replace(vecchio, '    "40_FB_Stracci": "Docs/richieste/media/R012/punti_segreti_stracci.md",\n'
                           '    "50_FB_Volantino": "Docs/richieste/media/R013/punti_segreti_volantino.md",\n}\n'
                           'HUB, BASSO, RIC, STR = "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe", "40_FB_Stracci"\n'
                           'VOL = "50_FB_Volantino"')
    blocco = open(os.path.join(os.path.dirname(__file__), "segreti_volantino_blocco.py"), encoding="utf-8").read().replace("\r\n", "\n")
    marker = "# ricompense dell'enigma campana"
    assert marker in s
    s = s.replace(marker, blocco + "\n" + marker, 1)
    if crlf: s = s.replace("\n", "\r\n")
    open(g, "w", encoding="utf-8", newline="").write(s)
print("ok")
