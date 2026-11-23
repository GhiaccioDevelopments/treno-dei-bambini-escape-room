# -*- coding: utf-8 -*-
"""R014: inserisce in ArtSource/Segreti/genera_segreti.py il blocco dei segreti del ricordo "Il cappotto" (idempotente).
Il blocco vive in ArtSource/Cappotto/segreti_cappotto_blocco.py (sorgente); questo script lo incolla prima del commento "ricompense dell'enigma campana".
I punti stanno in Docs/richieste/media/R014/punti_segreti_stazione.md e punti_segreti_vagone.md (li riscrive SceneBuilderCappotto quando si costruiscono
le scene: stessi nomi e stesse coordinate).
Uso: python aggiungi_segreti_cappotto.py, poi python ../Segreti/genera_segreti.py"""
import os

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
g = os.path.join(RADICE, "ArtSource", "Segreti", "genera_segreti.py")
raw = open(g, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
s = raw.replace("\r\n", "\n")
if "STAZ, VAG = " not in s:
    vecchio = ('    "50_FB_Volantino": "Docs/richieste/media/R013/punti_segreti_volantino.md",\n}\n')
    assert vecchio in s
    s = s.replace(vecchio, '    "50_FB_Volantino": "Docs/richieste/media/R013/punti_segreti_volantino.md",\n'
                           '    "60_FB_Stazione": "Docs/richieste/media/R014/punti_segreti_stazione.md",\n'
                           '    "61_FB_Vagone": "Docs/richieste/media/R014/punti_segreti_vagone.md",\n}\n')
    vecchio = 'VOL = "50_FB_Volantino"\n'
    assert vecchio in s
    s = s.replace(vecchio, vecchio + 'STAZ, VAG = "60_FB_Stazione", "61_FB_Vagone"\n')
    blocco = open(os.path.join(os.path.dirname(__file__), "segreti_cappotto_blocco.py"), encoding="utf-8").read().replace("\r\n", "\n")
    marker = "# ricompense dell'enigma campana"
    assert marker in s
    s = s.replace(marker, blocco + "\n" + marker, 1)
    if crlf: s = s.replace("\n", "\r\n")
    open(g, "w", encoding="utf-8", newline="").write(s)
print("ok")
