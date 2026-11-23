# -*- coding: utf-8 -*-
"""
Il Treno dei Bambini - materiali d'epoca per gli altri personaggi (Antonietta, Maddalena, Pachiochia, controllore,
volontarie, Derna, Alcide, Rosa, Carmine). Gemello di prepara_materiali_amerigo.py: riusa le sue funzioni.
Si esegue una volta con il Python di MakeHuman (dopo prepara_materiali_amerigo.py):

  "%ProgramFiles%\\makehuman-community\\Python\\python.exe" prepara_materiali_personaggi.py

Scrive in Documents\\makehuman\\v1py3\\data\\amerigo\\ texture + .mhmat ricolorati dagli asset CC0 di MakeHuman:
tessuti spenti, lisi, con macchie e usura (nessun colore moderno: niente stampe, jeans blu, bianco ottico).
"""
import os
import sys

sys.argv = sys.argv[:1]
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import prepara_materiali_amerigo as base      # noqa: E402  (trova MakeHuman, crea la QApplication)
from prepara_materiali_amerigo import SYS, OUT, leggi, scrivi, tinta, rumore, luminanza, mhmat, capelli   # noqa: E402

import numpy as np                            # noqa: E402


def veste(asset, nome, rgb, sporco=0.2, grana=0.06, mix=0.93, guadagno=1.0, piatto=1.0, ritocco=None):
    """Ricolora un vestito MakeHuman: colore medio rgb, macchie d'usura e grana del tessuto. nome = nome del materiale."""
    src = os.path.join(SYS, "clothes", asset)
    a = leggi(os.path.join(src, asset + "_diffuse.png"))
    if ritocco is not None:
        ritocco(a)
    h, w = a.shape[:2]
    stoffa = luminanza(a) > 12
    if piatto < 1.0:       # attenua le righe e i motivi moderni della texture originale: tessuto in tinta unita
        L = luminanza(a)
        media = max(float(L[stoffa].mean()), 1.0)
        k = (media + (L - media) * piatto) / np.maximum(L, 1.0)
        a[..., :3] *= k[..., None]
    a = tinta(a, rgb, mix=mix, guadagno=guadagno, maschera=stoffa)
    a[..., :3] *= rumore(h, w, 34, sporco)[..., None]
    a[..., :3] *= rumore(h, w, 220, grana)[..., None]
    scrivi(a, os.path.join(OUT, nome + "_diffuse.png"))
    mhmat(os.path.join(src, asset + ".mhmat"), nome, nome + "_diffuse.png")


def senza_cravatta(a):
    """Copre con il petto della camicia la cravatta moderna (a righe) di male_elegantsuit01: nel 1946 il
    controllore ferroviario ha colletto e bottoni, non una cravatta da impiegato. Coordinate nella texture 2048x2048."""
    from PyQt5.QtGui import QImage, QPainter, QColor, QPolygonF
    from PyQt5.QtCore import QPointF
    punti = [(165, 350), (275, 295), (290, 380), (245, 470), (195, 585), (135, 690), (72, 724), (88, 650),
             (105, 530), (125, 430)]
    h, w = a.shape[:2]
    m = QImage(w, h, QImage.Format_Grayscale8)
    m.fill(0)
    p = QPainter(m)
    p.setPen(QColor(255, 255, 255))
    p.setBrush(QColor(255, 255, 255))
    p.drawPolygon(QPolygonF([QPointF(700 + x / 2.0, 20 + y / 2.0) for x, y in punti]))
    p.end()
    ptr = m.bits()
    ptr.setsize(m.byteCount())
    mask = np.frombuffer(ptr, np.uint8).reshape(h, m.bytesPerLine())[:, :w] > 128
    a[mask, 0] = 196
    a[mask, 1] = 196
    a[mask, 2] = 192


def berretto(nome, rgb, rgb_fascia, rgb_visiera):
    """Texture e materiale del berretto da ferroviere (abiti_epoca/crea_abiti_epoca.py): meta' sinistra = stoffa
    della fascia e della calotta, meta' destra = visiera di cuoio nero."""
    bs = chr(10)
    w = h = 128
    a = np.zeros((h, w, 4), np.float32)
    a[..., 3] = 255
    a[:, : w // 3, :3] = rgb[::-1]      # la texture e' BGR
    a[:, w // 3: 2 * w // 3, :3] = rgb_fascia[::-1]
    a[:, 2 * w // 3:, :3] = rgb_visiera[::-1]
    a[..., :3] *= rumore(h, w, 20, 0.12)[..., None]
    a[..., :3] *= rumore(h, w, 60, 0.05)[..., None]
    scrivi(a, os.path.join(OUT, nome + "_diffuse.png"))
    with open(os.path.join(OUT, nome + ".mhmat"), "w") as f:
        f.write("# Il Treno dei Bambini: materiale generato (CC0)" + bs)
        f.write(bs.join(["name " + nome, "tag MakeHuman", "ambientColor 1.0 1.0 1.0", "diffuseColor 1.0 1.0 1.0",
                         "specularColor 0.03 0.03 0.03", "shininess 0.2", "opacity 1.0", "backfaceCull False",
                         "diffuseTexture " + nome + "_diffuse.png", "shader data/shaders/glsl/litsphere",
                         "shaderParam litsphereTexture data/litspheres/lit_matte.png", "shaderConfig diffuse True", ""]))


def scarpe(asset, nome, rgb):
    veste(asset, nome, rgb, sporco=0.28, grana=0.05, mix=0.85)


if __name__ == "__main__":
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    print("Cartella: " + OUT)
    D = os.path.join(SYS, "hair")
    # capelli (colore medio)
    capelli(os.path.join(D, "ponytail01"), "antonietta_capelli", (46, 32, 26))        # bruno quasi nero
    capelli(os.path.join(D, "braid01"), "maddalena_capelli", (84, 56, 38))              # castano
    capelli(os.path.join(D, "ponytail01"), "pachiochia_capelli", (112, 106, 100))          # brizzolati
    capelli(os.path.join(D, "short01"), "controllore_capelli", (40, 34, 30))
    capelli(os.path.join(D, "braid01"), "volontaria_comitato_capelli", (62, 44, 32))
    capelli(os.path.join(D, "ponytail01"), "volontaria_cr_capelli", (118, 90, 62))    # castano chiaro
    capelli(os.path.join(D, "ponytail01"), "derna_capelli", (52, 38, 30))
    capelli(os.path.join(D, "short04"), "alcide_capelli", (62, 46, 34))
    capelli(os.path.join(D, "ponytail01"), "rosa_capelli", (156, 152, 146))                # bianchi
    capelli(os.path.join(D, "short03"), "carmine_capelli", (98, 94, 88))              # sale e pepe
    # abiti
    veste("female_elegantsuit01", "antonietta_abito", (52, 46, 44), sporco=0.26, piatto=0.2)       # nero liso, scolorito
    veste("female_elegantsuit01", "maddalena_abito", (104, 74, 62), sporco=0.16, piatto=0.2)       # rosso mattone spento
    veste("female_elegantsuit01", "pachiochia_abito", (64, 54, 48), sporco=0.24, piatto=0.2)
    veste("female_elegantsuit01", "volontaria_comitato_abito", (92, 92, 84), sporco=0.16, piatto=0.2)  # grigio-verde
    veste("female_elegantsuit01", "volontaria_cr_abito", (214, 210, 198), sporco=0.10, guadagno=1.0, piatto=0.2)  # camice di cotone, avorio
    veste("female_elegantsuit01", "rosa_abito", (66, 56, 60), sporco=0.2, piatto=0.2)
    veste("male_elegantsuit01", "controllore_divisa", (40, 44, 54), sporco=0.16, ritocco=senza_cravatta)       # divisa blu notte, lisa
    veste("female_elegantsuit01", "derna_cappotto", (66, 70, 62), sporco=0.22, piatto=0.2)            # lana grigio-verde rammendata
    veste("male_casualsuit03", "alcide_abito", (120, 106, 88), sporco=0.2, piatto=0.25)             # camicia e gilet di fustagno
    berretto("controllore_berretto", (58, 66, 84), (92, 30, 34), (30, 30, 34))
    # scarpe
    scarpe("shoes01", "scarpe_usate", (60, 46, 36))
    scarpe("shoes03", "scarpe_nere_usate", (38, 34, 32))
    print("Fatto.")
