# -*- coding: utf-8 -*-
"""R015: inserisce in ArtSource/Segreti/genera_segreti.py il blocco dei segreti del ricordo "Il violino" (idempotente).
Il blocco vive in ArtSource/Violino/segreti_violino_blocco.py (sorgente); questo script lo incolla prima del commento "ricompense dell'enigma campana".
I punti stanno in Docs/richieste/media/R015/punti_segreti_modena.md (lo riscrive SceneBuilderViolino quando si costruisce la scena:
stessi nomi e stesse coordinate).
Uso: python aggiungi_segreti_violino.py, poi python ../Segreti/genera_segreti.py"""
import os

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
g = os.path.join(RADICE, "ArtSource", "Segreti", "genera_segreti.py")
raw = open(g, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
s = raw.replace("\r\n", "\n")
if 'MOD = "70_FB_Modena"' not in s:
    vecchio = '    "61_FB_Vagone": "Docs/richieste/media/R014/punti_segreti_vagone.md",\n}\n'
    assert vecchio in s
    s = s.replace(vecchio, '    "61_FB_Vagone": "Docs/richieste/media/R014/punti_segreti_vagone.md",\n'
                           '    "70_FB_Modena": "Docs/richieste/media/R015/punti_segreti_modena.md",\n}\n')
    vecchio = 'STAZ, VAG = "60_FB_Stazione", "61_FB_Vagone"\n'
    assert vecchio in s
    s = s.replace(vecchio, vecchio + 'MOD = "70_FB_Modena"\n')
    blocco = open(os.path.join(os.path.dirname(__file__), "segreti_violino_blocco.py"), encoding="utf-8").read().replace("\r\n", "\n")
    marker = "# ricompense dell'enigma campana"
    assert marker in s
    s = s.replace(marker, blocco + "\n" + marker, 1)
    if crlf: s = s.replace("\n", "\r\n")
    open(g, "w", encoding="utf-8", newline="").write(s)
print("ok")
