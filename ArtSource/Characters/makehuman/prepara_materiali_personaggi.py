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


def veste(asset, nome, rgb, sporco=0.2, grana=0.06, mix=0.93, guadagno=1.0, piatto=1.0):
    """Ricolora un vestito MakeHuman: colore medio rgb, macchie d'usura e grana del tessuto. nome = nome del materiale."""
    src = os.path.join(SYS, "clothes", asset)
    a = leggi(os.path.join(src, asset + "_diffuse.png"))
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
    veste("male_elegantsuit01", "controllore_divisa", (40, 44, 54), sporco=0.16)       # divisa blu notte, lisa
    veste("male_casualsuit05", "derna_cappotto", (66, 70, 62), sporco=0.22)            # lana grigio-verde rammendata
    veste("male_casualsuit03", "alcide_abito", (120, 106, 88), sporco=0.2, piatto=0.25)             # camicia e gilet di fustagno
    # scarpe
    scarpe("shoes01", "scarpe_usate", (60, 46, 36))
    scarpe("shoes03", "scarpe_nere_usate", (38, 34, 32))
    print("Fatto.")
