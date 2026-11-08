# R007 tappa 3 parte B: texture dei decal di umidita', salnitro e macchie d'acqua alla base dei muri (originali, generate).
# Escono in Assets/Art/Textures/Decal/: DecalUmido.png (fascia scura che sale dal basso con crosta di salnitro) e DecalMacchie.png
# (colature e macchie d'acqua verticali). 512x256 RGBA, ripetibili in orizzontale. Uso: python decal_muri.py
import os
import numpy as np
from PIL import Image, ImageFilter

QUI = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(QUI, '..', '..', 'Assets', 'Art', 'Textures', 'Decal'))
os.makedirs(OUT, exist_ok=True)
W, H = 512, 256


def rumore(sigma, seed, w=W, h=H):
    """Rumore ripetibile in orizzontale: si sfoca una copia a 3 tile e si ritaglia quella centrale."""
    r = np.random.default_rng(seed)
    n = r.random((h, w * 3)).astype(np.float32)
    im = Image.fromarray((n * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(sigma))
    a = np.asarray(im, np.float32)[:, w:2 * w] / 255.0
    return (a - a.mean()) / (a.std() + 1e-6)


def salva(rgb, alfa, nome):
    a = np.clip(alfa, 0, 1)
    img = np.dstack([np.clip(rgb, 0, 255), a * 255]).astype(np.uint8)
    Image.fromarray(img, 'RGBA').save(os.path.join(OUT, nome), optimize=True)


yy = np.mgrid[0:H, 0:W][0].astype(np.float32) / H      # 0 in alto, 1 in basso

# --- umido + salnitro: scuro e opaco alla base, bordo superiore irregolare e sfumato; crosta chiara a chiazze
bordo = 0.55 + 0.12 * rumore(14, 1) + 0.05 * rumore(3, 2)          # quota (frazione dal basso) della linea di risalita
risalita = np.clip((yy - (1 - bordo)) / 0.28, 0, 1)
umido = np.clip(risalita ** 0.8, 0, 1) * (0.78 + 0.1 * rumore(5, 3))
sal = np.clip(rumore(4, 4) * 0.8 + 0.2 + (rumore(1.2, 5) * 0.25), 0, 1) * np.clip(1 - np.abs(yy - (1 - bordo + 0.08)) / 0.1, 0, 1)
col = np.zeros((H, W, 3), np.float32)
col[...] = np.array([46, 38, 30], np.float32)
col = col * (1 - sal[..., None] * 0.85) + np.array([205, 200, 185], np.float32) * sal[..., None] * 0.85
alfa = np.clip(umido * 0.62 + sal * 0.5, 0, 0.8)
salva(col, alfa, 'DecalUmido.png')

# --- macchie d'acqua: colature verticali scure con bordo piu' chiaro, a chiazze
colate = np.clip(rumore(1.0, 6)[:, :] * 0 + 0, 0, 1)
r = np.random.default_rng(7)
colate = np.zeros((H, W), np.float32)
for _ in range(26):
    x = int(r.integers(0, W)); larg = int(r.integers(3, 12)); alt = int(r.integers(60, 240))
    y0 = int(r.integers(0, 60)); alt = min(alt, H - y0)
    prof = np.linspace(1, 0.15, alt)[:, None] * (0.6 + 0.4 * r.random())
    for dx in range(-larg, larg + 1):
        f = max(0.0, 1 - abs(dx) / (larg + 0.01))
        colate[y0:y0 + alt, (x + dx) % W] = np.maximum(colate[y0:y0 + alt, (x + dx) % W], prof[:, 0] * f)
colate = np.asarray(Image.fromarray((colate * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6)), np.float32) / 255.0
macchie = np.clip(rumore(18, 8) * 0.35 + 0.15, 0, 1) * np.clip(1.4 - yy, 0, 1)
tot = np.clip(colate * 0.9 + macchie * 0.5, 0, 1)
col2 = np.zeros((H, W, 3), np.float32)
col2[...] = np.array([58, 50, 40], np.float32)
col2 += (rumore(2, 9)[..., None] * 6)
salva(col2, tot * 0.55, 'DecalMacchie.png')
print('ok', OUT)
