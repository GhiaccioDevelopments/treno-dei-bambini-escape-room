# -*- coding: utf-8 -*-
"""
Il Treno dei Bambini - prepara i materiali d'epoca per Amerigo in MakeHuman.
Si esegue UNA volta (fuori da MakeHuman) con il Python di MakeHuman, che ha numpy e PyQt5:

  "%LOCALAPPDATA%\\makehuman-community\\Python\\python.exe" prepara_materiali_amerigo.py

Crea in  Documents\\makehuman\\v1py3\\data\\amerigo\\  texture + .mhmat ricolorate a partire
dagli asset CC0 di MakeHuman:
  - capelli castani (bambino da short02, adulto da short04)
  - salopette sporca, piena di toppe e con strappi del bambino (da male_worksuit01)
  - pelle sporca del bambino scalzo (da young_caucasian_male; scarpe_bambino() resta ma non si usa)
L'adulto usa l'abito scuro e le scarpe nere originali (male_elegantsuit01, shoes03).
"""
import os
import sys
import shutil

def trova_makehuman():
    """Cartella di installazione di MakeHuman: installer 1.3 (Program Files), 1.2 (AppData) o accanto al Python in uso."""
    candidati = [
        os.path.dirname(os.path.dirname(os.path.abspath(sys.executable))),   # ...\makehuman-community\Python\python.exe
        os.path.join(os.environ.get("ProgramFiles", r"C:\Program Files"), "makehuman-community"),
        os.path.join(os.environ.get("LOCALAPPDATA", os.path.expanduser("~/AppData/Local")), "makehuman-community"),
    ]
    for c in candidati:
        if os.path.isdir(os.path.join(c, "makehuman", "data")):
            return c
    raise SystemExit("MakeHuman non trovato. Cercato in:\n  " + "\n  ".join(candidati))


MH = trova_makehuman()
print("MakeHuman: " + MH)
sys.path.insert(0, os.path.join(MH, "pkgs"))

import numpy as np
from PyQt5.QtGui import QImage, QPainter, QColor, QPen
from PyQt5.QtWidgets import QApplication
from PyQt5.QtCore import Qt, QRectF

SYS = os.path.join(MH, "makehuman", "data")
OUT = os.path.join(os.path.expanduser("~"), "Documents", "makehuman", "v1py3", "data", "amerigo")
RNG = np.random.RandomState(1946)

app = QApplication(sys.argv[:1])


def leggi(path):
    img = QImage(path).convertToFormat(QImage.Format_ARGB32)
    if img.isNull():
        raise IOError("texture non trovata: " + path)
    ptr = img.bits()
    ptr.setsize(img.byteCount())
    return np.frombuffer(ptr, np.uint8).reshape(img.height(), img.width(), 4).astype(np.float32)


def scrivi(arr, path):
    a = np.ascontiguousarray(np.clip(arr, 0, 255).astype(np.uint8))
    h, w = a.shape[:2]
    QImage(a.data, w, h, 4 * w, QImage.Format_ARGB32).copy().save(path)
    print("  scritto " + path)


def luminanza(a):  # canali BGRA
    return 0.114 * a[..., 0] + 0.587 * a[..., 1] + 0.299 * a[..., 2]


def rumore(h, w, celle=48, forza=0.18):
    """Macchie morbide (sporco, usura) tra 1-forza e 1+forza."""
    small = (RNG.rand(celle, celle) * 255).astype(np.uint8)
    q = QImage(small.data, celle, celle, celle, QImage.Format_Grayscale8).copy()
    q = q.scaled(w, h, Qt.IgnoreAspectRatio, Qt.SmoothTransformation).convertToFormat(QImage.Format_Grayscale8)
    ptr = q.bits()
    ptr.setsize(q.byteCount())
    n = np.frombuffer(ptr, np.uint8).reshape(h, q.bytesPerLine())[:, :w].astype(np.float32) / 255.0
    return 1.0 + (n - 0.5) * 2 * forza


def tinta(a, rgb, mix=1.0, guadagno=1.0, maschera=None):
    """Ricolora mantenendo il dettaglio (luminanza). rgb = colore medio voluto (0-255)."""
    L = luminanza(a)
    m = L[maschera] if maschera is not None else L
    media = max(float(m.mean()), 1.0)
    rel = (L / media)[..., None] * guadagno
    nuovo = np.stack([rgb[2] * rel[..., 0], rgb[1] * rel[..., 0], rgb[0] * rel[..., 0]], -1)
    out = a.copy()
    out[..., :3] = a[..., :3] * (1 - mix) + nuovo * mix
    return out


def mhmat(origine, nome, diffuse, capelli=False):
    """Copia il .mhmat originale cambiando nome e texture diffuse; copia normal/AO accanto."""
    righe = []
    cartella = os.path.dirname(origine)
    for r in open(origine, encoding="utf-8"):
        k = r.split()[0] if r.split() else ""
        if k == "name":
            r = "name " + nome + "\n"
        elif k == "diffuseTexture":
            r = "diffuseTexture " + diffuse + "\n"
        elif k in ("normalmapTexture", "aomapTexture", "bumpmapTexture", "specularmapTexture"):
            tex = r.split()[1]
            src = os.path.join(cartella, tex)
            if os.path.isfile(src):
                shutil.copy(src, os.path.join(OUT, os.path.basename(tex)))
                r = k + " " + os.path.basename(tex) + "\n"
        elif k == "shaderParam" and "litsphereTexture" in r and capelli:
            r = "shaderParam litsphereTexture data/litspheres/lit_hair.png\n"
        elif k == "shader" and not r.split()[1].startswith("data/"):
            r = "shader data/" + r.split()[1] + "\n"
        righe.append(r)
    dest = os.path.join(OUT, nome + ".mhmat")
    open(dest, "w", encoding="utf-8").write(
        "# Il Treno dei Bambini: derivato da asset CC0 di MakeHuman (" + os.path.basename(origine) + ")\n" + "".join(righe))
    print("  scritto " + dest)


def capelli(src_dir, nome, rgb):
    base = os.path.basename(src_dir)
    a = leggi(os.path.join(src_dir, base + "_diffuse.png"))
    visibile = a[..., 3] > 40
    a = tinta(a, rgb, mix=1.0, maschera=visibile)
    scrivi(a, os.path.join(OUT, nome + "_diffuse.png"))
    mhmat(os.path.join(src_dir, base + ".mhmat"), nome, nome + "_diffuse.png", capelli=True)


def salopette_bambino():
    """Salopette di un bambino poverissimo: sbiadita, sporca, piena di toppe e con qualche strappo."""
    src = os.path.join(SYS, "clothes", "male_worksuit01")
    a = leggi(os.path.join(src, "male_worksuit01_diffuse.png"))
    h, w = a.shape[:2]
    stoffa = luminanza(a) > 12                                # esclude lo sfondo nero dell'atlante
    a = tinta(a, (66, 57, 46), mix=0.95, guadagno=1.0, maschera=stoffa)
    a[..., :3] *= rumore(h, w, 40, 0.30)[..., None]          # macchie di sporco grandi
    a[..., :3] *= rumore(h, w, 110, 0.14)[..., None]         # sporco medio
    a[..., :3] *= rumore(h, w, 220, 0.08)[..., None]         # grana del tessuto liso
    # orli in basso (caviglie) piu' sporchi di fango e polvere
    fango = np.clip((np.arange(h)[:, None] / float(h) - 0.80) / 0.15, 0, 1) * 0.35
    a[..., :3] *= (1.0 - fango)[..., None]
    scrivi(a, os.path.join(OUT, "_tmp.png"))
    img = QImage(os.path.join(OUT, "_tmp.png"))
    p = QPainter(img)
    p.setRenderHint(QPainter.Antialiasing)
    # toppe di stoffe diverse (posizioni UV approssimative: ginocchia, cosce, petto, fondo schiena)
    toppe = [(0.05, 0.63, 0.075, 0.07, QColor(62, 50, 38)),
             (0.21, 0.70, 0.065, 0.06, QColor(104, 92, 72)),
             (0.79, 0.44, 0.09, 0.07, QColor(74, 58, 46)),
             (0.12, 0.30, 0.05, 0.045, QColor(92, 80, 62)),
             (0.07, 0.48, 0.05, 0.06, QColor(58, 62, 66)),
             (0.23, 0.52, 0.045, 0.05, QColor(96, 70, 50)),
             (0.67, 0.68, 0.06, 0.07, QColor(70, 66, 56)),
             (0.86, 0.72, 0.05, 0.05, QColor(110, 96, 78))]
    for (u, v, du, dv, col) in toppe:
        r = QRectF(u * w, v * h, du * w, dv * h)
        p.fillRect(r, col)
        pen = QPen(QColor(190, 175, 140, 200), max(2, w // 900), Qt.DashLine)
        p.setPen(pen)
        p.drawRect(r.adjusted(w * 0.004, h * 0.004, -w * 0.004, -h * 0.004))
    # strappi e buchi (scuri, irregolari) dove la stoffa e' consumata
    from PyQt5.QtGui import QPolygonF
    from PyQt5.QtCore import QPointF
    p.setPen(Qt.NoPen)
    for (u, v, raggio) in [(0.16, 0.76, 0.018), (0.27, 0.82, 0.014), (0.72, 0.80, 0.016),
                           (0.09, 0.86, 0.012), (0.90, 0.60, 0.011)]:
        punti = []
        for k in range(9):
            ang = k / 9.0 * 6.2832
            rr = raggio * (0.55 + RNG.rand() * 0.8)
            punti.append(QPointF((u + rr * np.cos(ang)) * w, (v + rr * np.sin(ang) * 0.7) * h))
        p.setBrush(QColor(20, 16, 12))
        p.drawPolygon(QPolygonF(punti))
    p.end()
    img.save(os.path.join(OUT, "amerigo_bambino_salopette_diffuse.png"))
    os.remove(os.path.join(OUT, "_tmp.png"))
    print("  scritto salopette con toppe e strappi")
    mhmat(os.path.join(src, "male_worksuit01.mhmat"), "amerigo_bambino_salopette",
          "amerigo_bambino_salopette_diffuse.png")


def pelle_bambino():
    """Pelle del bambino scalzo: sporca su piedi e gambe, un po' sulle mani, appena sul resto."""
    src_mat = os.path.join(SYS, "skins", "young_caucasian_male", "young_caucasian_male.mhmat")
    a = leggi(os.path.join(SYS, "skins", "textures", "young_lightskinned_male_diffuse.png"))
    h, w = a.shape[:2]
    # maschera dello sporco disegnata sulle UV del corpo base (coordinate in decimetri, y = altezza)
    z = np.load(os.path.join(SYS, "3dobjs", "base.npz"))
    fg = z["fgstr"].tobytes().decode()
    idx = list(z["fgidx"])
    nomi = [fg[idx[i]:(idx[i + 1] if i + 1 < len(idx) else len(fg))] for i in range(len(idx))]
    corpo = z["group"] == nomi.index("body")
    fv, fu, co, uv = z["fvert"][corpo], z["fuvs"][corpo], z["coord"], z["texco"]
    ymin, ymax = co[:, 1].min(), co[:, 1].max()
    H = ymax - ymin
    xmax = np.abs(co[:, 0]).max()
    cy = co[fv][:, :, 1].mean(1)
    cx = np.abs(co[fv][:, :, 0]).mean(1)
    sporco = np.zeros(len(fv))
    sporco = np.maximum(sporco, np.clip(1.0 - (cy - ymin) / (0.09 * H), 0, 1) * 0.85)    # piedi
    sporco = np.maximum(sporco, np.clip(1.0 - (cy - ymin) / (0.32 * H), 0, 1) * 0.55)    # gambe
    sporco = np.maximum(sporco, np.clip((cx - 0.74 * xmax) / (0.12 * xmax), 0, 1) * 0.45)  # mani
    sporco = np.maximum(sporco, 0.12)                                                  # ovunque, poco
    m = QImage(w, h, QImage.Format_Grayscale8)
    m.fill(0)
    from PyQt5.QtGui import QPolygonF
    from PyQt5.QtCore import QPointF
    p = QPainter(m)
    p.setPen(Qt.NoPen)
    for f in range(len(fv)):
        val = int(sporco[f] * 255)
        p.setBrush(QColor(val, val, val))
        p.drawPolygon(QPolygonF([QPointF(uv[t, 0] * w, (1.0 - uv[t, 1]) * h) for t in fu[f]]))
    p.end()
    m = m.scaled(w // 32, h // 32, Qt.IgnoreAspectRatio, Qt.SmoothTransformation)          .scaled(w, h, Qt.IgnoreAspectRatio, Qt.SmoothTransformation).convertToFormat(QImage.Format_Grayscale8)
    ptr = m.bits()
    ptr.setsize(m.byteCount())
    mask = np.frombuffer(ptr, np.uint8).reshape(h, m.bytesPerLine())[:, :w].astype(np.float32) / 255.0
    mask *= rumore(h, w, 60, 0.45)                           # sporco a chiazze, non uniforme
    mask = np.clip(mask, 0, 1)[..., None]
    terra = np.array([38, 52, 70], np.float32)               # BGR: terra/polvere scura
    a[..., :3] = a[..., :3] * (1 - mask * 0.75) + terra * (mask * 0.75) * (luminanza(a)[..., None] / 160.0)
    scrivi(a, os.path.join(OUT, "amerigo_bambino_pelle_diffuse.png"))
    mhmat(src_mat, "amerigo_bambino_pelle", "amerigo_bambino_pelle_diffuse.png")


def scarpe_bambino():
    src = os.path.join(SYS, "clothes", "shoes01")
    a = leggi(os.path.join(src, "shoes01_diffuse.png"))
    h, w = a.shape[:2]
    a = tinta(a, (62, 45, 33), mix=0.75, guadagno=1.0, maschera=luminanza(a) > 12)       # cuoio scurito e opaco
    a[..., :3] *= rumore(h, w, 30, 0.25)[..., None]           # graffi, polvere
    scrivi(a, os.path.join(OUT, "amerigo_bambino_scarpe_diffuse.png"))
    mhmat(os.path.join(src, "shoes01.mhmat"), "amerigo_bambino_scarpe", "amerigo_bambino_scarpe_diffuse.png")


if __name__ == "__main__":
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    print("Cartella: " + OUT)
    capelli(os.path.join(SYS, "hair", "short02"), "amerigo_bambino_capelli", (78, 52, 34))   # castano
    capelli(os.path.join(SYS, "hair", "short04"), "amerigo_adulto_capelli", (70, 48, 34))    # castano scuro (taglio scelto dall utente)
    salopette_bambino()
    pelle_bambino()          # scalzo: niente scarpe, piedi e gambe sporchi
    print("Fatto.")
