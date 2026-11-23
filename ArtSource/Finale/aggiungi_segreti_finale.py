# -*- coding: utf-8 -*-
"""R016: inserisce in ArtSource/Segreti/genera_segreti.py il blocco dei segreti del finale (il basso nel 1994) (idempotente).
Il blocco vive in ArtSource/Finale/segreti_finale_blocco.py; i punti in Docs/richieste/media/R016/punti_segreti_finale.md (li riscrive SceneBuilderFinale).
Uso: python aggiungi_segreti_finale.py, poi python ../Segreti/genera_segreti.py"""
import os

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
g = os.path.join(RADICE, "ArtSource", "Segreti", "genera_segreti.py")
raw = open(g, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
s = raw.replace("\r\n", "\n")
if 'FIN = "80_Basso_1994"' not in s:
    vecchio = '    "70_FB_Modena": "Docs/richieste/media/R015/punti_segreti_modena.md",\n}\n'
    assert vecchio in s
    s = s.replace(vecchio, '    "70_FB_Modena": "Docs/richieste/media/R015/punti_segreti_modena.md",\n'
                           '    "80_Basso_1994": "Docs/richieste/media/R016/punti_segreti_finale.md",\n}\n')
    vecchio = 'MOD = "70_FB_Modena"\n'
    assert vecchio in s
    s = s.replace(vecchio, vecchio + 'FIN = "80_Basso_1994"\n')
    blocco = open(os.path.join(os.path.dirname(__file__), "segreti_finale_blocco.py"), encoding="utf-8").read().replace("\r\n", "\n")
    marker = "# ricompense dell'enigma campana"
    assert marker in s
    s = s.replace(marker, blocco + "\n" + marker, 1)
    if crlf: s = s.replace("\n", "\r\n")
    open(g, "w", encoding="utf-8", newline="").write(s)
print("ok")
