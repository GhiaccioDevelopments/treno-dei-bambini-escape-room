"""
R020 (grafica degli enigmi, 2a passata) - Texture originali degli oggetti degli enigmi, disegnate da codice (PIL + numpy + scipy).
Nessuna immagine esterna, salvo il ritaglio di texture CC0 gia' nel progetto (Poly Haven, vedi CREDITS.md) dove indicato.

Uscite:
  Assets/Art/Textures/Enigmi/*.jpg|png   texture finali (albedo sRGB; normal OpenGL; MS = R metallo, G occlusione, A levigatezza)
  ArtSource/Models/Enigmi/_tmp/*.png     mappe di altezza e maschere per la geometria (le legge genera_enigmi_v2.py in Blender)

Testi stampati sulla latta: ArtSource/Models/Enigmi/testi_modelli.json (mai nel codice).
Uso:  python ArtSource/Models/Enigmi/texture_enigmi.py
"""
import json, math, os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Textures", "Enigmi")
TMP = os.path.join(ROOT, "ArtSource", "Models", "Enigmi", "_tmp")
SRC = os.path.join(ROOT, "Assets", "Art", "Textures")
FONT_STAMPA = os.path.join(ROOT, "Assets", "Art", "Fonts", "OldStandard-Bold.ttf")
FONT_STAMPA_R = os.path.join(ROOT, "Assets", "Art", "Fonts", "OldStandard-Regular.ttf")
TESTI = json.load(open(os.path.join(os.path.dirname(__file__), "testi_modelli.json"), encoding="utf-8"))
os.makedirs(OUT, exist_ok=True); os.makedirs(TMP, exist_ok=True)
rng = np.random.default_rng(1946)
random.seed(1946)


# ----------------------------------------------------------------------------------------------- utilita'
def noise(h, w, beta=2.0, seed=0):
    """Rumore frattale periodico (si ripete senza giunte), 0..1."""
    r = np.random.default_rng(seed)
    f = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None]; fx = np.fft.fftfreq(w)[None, :]
    k = np.sqrt(fx * fx + fy * fy); k[0, 0] = 1.0
    n = np.real(np.fft.ifft2(f / k ** (beta / 2.0)))
    return (n - n.min()) / (n.max() - n.min() + 1e-9)


def blur(a, s, wrap=False):
    return ndimage.gaussian_filter(a, s, mode="wrap" if wrap else "nearest")


def norm01(a):
    return (a - a.min()) / (a.max() - a.min() + 1e-9)


def normal_map(h, forza, wrap=False):
    """Altezza 0..1 -> normal map OpenGL (verde = su in UV), uint8."""
    m = "wrap" if wrap else "nearest"
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5 if wrap else ndimage.sobel(h, 1, mode=m) / 8.0
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5 if wrap else ndimage.sobel(h, 0, mode=m) / 8.0
    nx = -dx * forza; ny = dy * forza; nz = np.ones_like(h)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / l, ny / l, nz / l], -1)
    return ((n * 0.5 + 0.5) * 255).clip(0, 255).astype(np.uint8)


def mescola_normali(a, b):
    """Somma due normal map uint8 (dettaglio b su base a, metodo 'whiteout')."""
    a = a.astype(np.float32) / 127.5 - 1; b = b.astype(np.float32) / 127.5 - 1
    n = np.stack([a[..., 0] + b[..., 0], a[..., 1] + b[..., 1], a[..., 2] * b[..., 2]], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).clip(0, 255).astype(np.uint8)


def rgb(a):
    return (np.clip(a, 0, 1) * 255).astype(np.uint8)


def salva(nome, arr, q=90):
    p = os.path.join(OUT, nome)
    im = Image.fromarray(arr)
    if nome.endswith(".jpg"):
        im.convert("RGB").save(p, quality=q, optimize=True)
    else:
        im.save(p, optimize=True)
    print("TEXTURE", nome, im.size, os.path.getsize(p) // 1024, "KB")


def ms(metal, ao, smooth):
    return np.stack([rgb(metal), rgb(ao), np.zeros_like(rgb(ao)), rgb(smooth)], -1)


def carica(rel, size=None):
    im = Image.open(os.path.join(SRC, rel)).convert("RGB")
    if size: im = im.resize(size, Image.LANCZOS)
    return np.asarray(im).astype(np.float32) / 255.0


def lerp(a, b, t):
    a = np.asarray(a, np.float32); b = np.asarray(b, np.float32); t = np.asarray(t, np.float32)
    if t.ndim == 2 and (a.ndim in (1, 3) or b.ndim in (1, 3)):
        t = t[..., None]
    return a + (b - a) * t


def maschera(w, h, disegna, ss=2):
    """Maschera antialias: disegna(draw, scala) su tela ss volte piu' grande."""
    im = Image.new("L", (w * ss, h * ss), 0)
    disegna(ImageDraw.Draw(im), ss)
    return np.asarray(im.resize((w, h), Image.LANCZOS)).astype(np.float32) / 255.0


def cupola(m, r):
    """Gonfiore a cuscino dentro la maschera (distanza dal bordo)."""
    d = ndimage.distance_transform_edt(m > 0.5)
    t = np.clip(d / max(r, 1e-3), 0, 1)
    return (1 - (1 - t) ** 2) * (m > 0.5)


def bordo_perline(m, d0, d1, passo, rr):
    """Fascia di perline sbalzate lungo il bordo della maschera (fra d0 e d1 px dal bordo)."""
    d = ndimage.distance_transform_edt(m > 0.5)
    fascia = ((d > d0) & (d < d1)).astype(np.float32)
    h, w = m.shape
    yy, xx = np.mgrid[0:h, 0:w]
    gx = (xx % passo) - passo / 2; gy = ((yy + (xx // passo % 2) * passo / 2) % passo) - passo / 2
    punti = np.clip(1 - np.sqrt(gx * gx + gy * gy) / rr, 0, 1)
    return blur(fascia, 1.0) * punti


def linee(w, h, segmenti, larg, ss=2):
    def f(dr, s):
        for seg in segmenti:
            dr.line([(x * s, y * s) for x, y in seg], fill=255, width=max(1, int(larg * s)), joint="curve")
    return maschera(w, h, f, ss)


# ----------------------------------------------------------------------------------------------- ex-voto d'argento
EXV_TILE = (341, 512)  # 3 colonne x 2 righe nell'atlante 1024x1024
EXV_SLOT = {"cuore": (0, 0), "piede": (1, 0), "occhi": (2, 0), "mano": (0, 1), "gamba": (1, 1)}
EXV_ASPETTO = {"cuore": (0.10, 0.10), "piede": (0.11, 0.20), "occhi": (0.11, 0.06), "mano": (0.065, 0.09), "gamba": (0.04, 0.10)}


def rect_exvoto(nome):
    """Rettangolo (x, y, w, h) in pixel dell'atlante occupato dall'ex-voto (stesse proporzioni dell'oggetto)."""
    c, r = EXV_SLOT[nome]; tw, th = EXV_TILE
    aw, ah = EXV_ASPETTO[nome]
    s = min((tw - 16) / aw, (th - 16) / ah)
    w, h = int(aw * s), int(ah * s)
    return c * tw + (tw - w) // 2, r * th + (th - h) // 2, w, h


def exvoto_forme(nome, w, h):
    """Ritorna (maschera, altezza) dell'ex-voto in un rettangolo w x h."""
    P = lambda pts, s: [(x * w * s, y * h * s) for x, y in pts]
    if nome == "cuore":
        t = np.linspace(0, 2 * math.pi, 120)
        hx = 16 * np.sin(t) ** 3; hy = -(13 * np.cos(t) - 5 * np.cos(2 * t) - 2 * np.cos(3 * t) - np.cos(4 * t))
        cuore = [(0.5 + x / 38.0, 0.6 + y / 38.0) for x, y in zip(hx, hy)]
        fiamma = [(0.5, 0.02), (0.56, 0.12), (0.62, 0.07), (0.6, 0.2), (0.66, 0.16), (0.6, 0.3), (0.4, 0.3), (0.34, 0.16), (0.4, 0.2), (0.38, 0.07), (0.44, 0.12)]
        m = maschera(w, h, lambda d, s: (d.polygon(P(cuore, s), fill=255), d.polygon(P(fiamma, s), fill=255)))
        interno = maschera(w, h, lambda d, s: d.polygon(P([(0.5 + (x - 0.5) * 0.62, 0.62 + (y - 0.6) * 0.62) for x, y in cuore], s), fill=255))
        fiam = maschera(w, h, lambda d, s: d.polygon(P(fiamma, s), fill=255))
        H = 0.45 * cupola(m, w * 0.08) + 0.45 * cupola(interno, w * 0.1) + 0.25 * cupola(fiam, w * 0.03)
        H += 0.35 * bordo_perline(m * (1 - fiam), w * 0.025, w * 0.06, max(4, int(w * 0.045)), w * 0.02)
        raggi = []
        for k in range(14):
            a = -math.pi * 0.95 + k * (math.pi * 1.9 / 13)
            raggi.append([(0.5 + 0.2 * math.cos(a) * 0.6, 0.62 + 0.2 * math.sin(a) * 0.6), (0.5 + 0.3 * math.cos(a) * 0.62, 0.62 + 0.3 * math.sin(a) * 0.6)])
        H -= 0.12 * linee(w, h, [[(x * w, y * h) for x, y in r] for r in raggi], 1.2) * (1 - interno)
        croce = [[(0.5 * w, 0.48 * h), (0.5 * w, 0.72 * h)], [(0.42 * w, 0.56 * h), (0.58 * w, 0.56 * h)]]
        H -= 0.25 * linee(w, h, croce, 2.2)
        lingue = [[(0.5 * w, 0.05 * h), (0.5 * w, 0.27 * h)], [(0.42 * w, 0.12 * h), (0.45 * w, 0.27 * h)], [(0.58 * w, 0.12 * h), (0.55 * w, 0.27 * h)]]
        H -= 0.12 * linee(w, h, lingue, 1.2)
    elif nome == "piede":
        parti = [(0.5, 0.86, 0.26, 0.12), (0.52, 0.62, 0.24, 0.2), (0.53, 0.36, 0.36, 0.14)]
        dita = [(0.27, 0.17, 0.12), (0.43, 0.11, 0.1), (0.58, 0.11, 0.09), (0.71, 0.15, 0.08), (0.82, 0.22, 0.07)]
        def f(d, s):
            for x, y, rx, ry in parti: d.ellipse([((x - rx) * w * s, (y - ry) * h * s), ((x + rx) * w * s, (y + ry) * h * s)], fill=255)
            for x, y, r in dita: d.ellipse([((x - r) * w * s, (y - r * 0.85) * h * s), ((x + r) * w * s, (y + r * 0.85) * h * s)], fill=255)
        m = maschera(w, h, f)
        H = 0.55 * cupola(m, w * 0.12)
        for x, y, r in dita:
            dm = maschera(w, h, lambda d, s, x=x, y=y, r=r: d.ellipse([((x - r * 0.82) * w * s, (y - r * 0.7) * h * s), ((x + r * 0.82) * w * s, (y + r * 0.7) * h * s)], fill=255))
            H += 0.3 * cupola(dm, w * 0.035)
            un = maschera(w, h, lambda d, s, x=x, y=y, r=r: d.ellipse([((x - r * 0.4) * w * s, (y - r * 0.55) * h * s), ((x + r * 0.4) * w * s, (y - r * 0.05) * h * s)], outline=255, width=max(1, int(1.2 * s))))
            H -= 0.12 * un
        H += 0.18 * cupola(maschera(w, h, lambda d, s: d.ellipse([(0.62 * w * s, 0.72 * h * s), (0.74 * w * s, 0.8 * h * s)], fill=255)), w * 0.04)
        H += 0.3 * bordo_perline(m, w * 0.02, w * 0.05, max(4, int(w * 0.04)), w * 0.017)
        H -= 0.1 * linee(w, h, [[(0.3 * w, 0.29 * h), (0.5 * w, 0.27 * h), (0.75 * w, 0.31 * h)]], 1.3)
    elif nome == "occhi":
        m = maschera(w, h, lambda d, s: d.ellipse([(0.01 * w * s, 0.02 * h * s), (0.99 * w * s, 0.98 * h * s)], fill=255))
        H = 0.25 * cupola(m, w * 0.05) + 0.35 * bordo_perline(m, w * 0.018, w * 0.045, max(4, int(w * 0.035)), w * 0.014)
        for cx in (0.29, 0.71):
            occhio = maschera(w, h, lambda d, s, cx=cx: d.ellipse([((cx - 0.17) * w * s, 0.33 * h * s), ((cx + 0.17) * w * s, 0.72 * h * s)], fill=255))
            iride = maschera(w, h, lambda d, s, cx=cx: d.ellipse([((cx - 0.065) * w * s, 0.4 * h * s), ((cx + 0.065) * w * s, 0.65 * h * s)], fill=255))
            pup = maschera(w, h, lambda d, s, cx=cx: d.ellipse([((cx - 0.025) * w * s, 0.47 * h * s), ((cx + 0.025) * w * s, 0.58 * h * s)], fill=255))
            H += 0.35 * cupola(occhio, w * 0.04) + 0.25 * cupola(iride, w * 0.02) - 0.25 * pup
            sop = [[((cx - 0.18 + 0.36 * k / 10) * w, (0.26 - 0.07 * math.sin(math.pi * k / 10)) * h) for k in range(11)]]
            H += 0.25 * blur(linee(w, h, sop, 3.0), 1.0)
            ciglia = [[((cx - 0.13 + 0.26 * k / 6) * w, 0.36 * h), ((cx - 0.15 + 0.3 * k / 6) * w, 0.31 * h)] for k in range(7)]
            H -= 0.1 * linee(w, h, ciglia, 1.0)
    elif nome == "mano":
        palmo = [(0.2, 0.42), (0.8, 0.42), (0.82, 0.78), (0.7, 0.86), (0.3, 0.86), (0.18, 0.75)]
        dita = [(0.28, 0.1, 0.38), (0.44, 0.03, 0.4), (0.6, 0.05, 0.4), (0.75, 0.13, 0.4)]
        def f(d, s):
            d.polygon(P(palmo, s), fill=255)
            for x, y0, y1 in dita: d.rounded_rectangle([((x - 0.07) * w * s, y0 * h * s), ((x + 0.07) * w * s, (y1 + 0.06) * h * s)], radius=0.07 * w * s, fill=255)
            d.line(P([(0.22, 0.7), (0.08, 0.5), (0.03, 0.38)], s), fill=255, width=int(0.13 * w * s), joint="curve")
            d.ellipse(P([(0.03 - 0.065, 0.38 - 0.045), (0.03 + 0.065, 0.38 + 0.045)], s), fill=255)
            d.rectangle(P([(0.27, 0.84), (0.73, 0.99)], s), fill=255)
        m = maschera(w, h, f)
        H = 0.5 * cupola(m, w * 0.07)
        H -= 0.18 * linee(w, h, [[((x + 0.075) * w, (y0 + 0.12) * h), ((x + 0.075) * w, 0.45 * h)] for x, y0, y1 in dita[:-1]], 1.6)
        H -= 0.12 * linee(w, h, [[(x * w, (y0 + 0.13) * h), (x * w, (y0 + 0.15) * h)] for x, y0, y1 in dita] + [[((x - 0.04) * w, 0.33 * h), ((x + 0.04) * w, 0.33 * h)] for x, y0, y1 in dita], 1.0)
        H -= 0.12 * linee(w, h, [[(0.28 * w, 0.55 * h), (0.5 * w, 0.6 * h), (0.75 * w, 0.52 * h)], [(0.25 * w, 0.68 * h), (0.45 * w, 0.66 * h), (0.6 * w, 0.75 * h)]], 1.2)
        polso = maschera(w, h, lambda d, s: d.rectangle(P([(0.27, 0.86), (0.73, 0.99)], s), fill=255))
        tratt = linee(w, h, [[((0.28 + k * 0.04) * w, 0.87 * h), ((0.3 + k * 0.04) * w, 0.98 * h)] for k in range(12)], 1.0)
        H += 0.2 * cupola(polso, w * 0.03) - 0.1 * tratt * polso
        H += 0.25 * bordo_perline(m, w * 0.02, w * 0.05, max(4, int(w * 0.05)), w * 0.02) * (1 - polso)
    else:  # gamba vista di fianco, piede in basso verso sinistra
        g = [(0.3, 0.01), (0.88, 0.01), (0.86, 0.2), (0.78, 0.4), (0.72, 0.47), (0.82, 0.6), (0.78, 0.75), (0.66, 0.87), (0.66, 0.93), (0.6, 0.99),
             (0.02, 0.99), (0.04, 0.94), (0.3, 0.9), (0.36, 0.84), (0.34, 0.7), (0.3, 0.56), (0.36, 0.46), (0.26, 0.3), (0.24, 0.12)]
        m = maschera(w, h, lambda d, s: d.polygon(P(g, s), fill=255))
        H = 0.55 * cupola(m, w * 0.18)
        H += 0.2 * cupola(maschera(w, h, lambda d, s: d.ellipse(P([(0.44, 0.42), (0.74, 0.52)], s), fill=255)), w * 0.06)
        H += 0.18 * cupola(maschera(w, h, lambda d, s: d.ellipse(P([(0.5, 0.52), (0.82, 0.75)], s), fill=255)), w * 0.08)
        giarr = maschera(w, h, lambda d, s: d.rectangle(P([(0.2, 0.01), (0.95, 0.09)], s), fill=255)) * m
        H += 0.15 * cupola(giarr, w * 0.04) - 0.1 * linee(w, h, [[((0.25 + k * 0.06) * w, 0.015 * h), ((0.28 + k * 0.06) * w, 0.085 * h)] for k in range(11)], 1.0) * giarr
        H -= 0.12 * linee(w, h, [[(0.1 * w, 0.95 * h), (0.3 * w, 0.93 * h)], [(0.42 * w, 0.48 * h), (0.6 * w, 0.46 * h)]], 1.2)
        H += 0.25 * bordo_perline(m, w * 0.04, w * 0.1, max(4, int(w * 0.11)), w * 0.045) * (1 - giarr)
    # cesello: puntini battuti sul fondo
    pun = (rng.random((h, w)) > 0.985).astype(np.float32)
    H -= 0.05 * blur(pun, 0.6) * m
    H = blur(H, 0.7) * (m > 0.02)
    return m, H


def exvoto_atlante():
    N = 1024
    alb = np.zeros((N, N, 3), np.float32); alpha = np.zeros((N, N), np.float32)
    nrm = np.zeros((N, N, 3), np.uint8); nrm[...] = (128, 128, 255)
    met = np.zeros((N, N), np.float32); occ = np.ones((N, N), np.float32); smo = np.zeros((N, N), np.float32)
    regioni = {}
    for nome in EXV_SLOT:
        x, y, w, h = rect_exvoto(nome)
        m, H = exvoto_forme(nome, w, h)
        H = H - H.min()
        cav = np.clip(blur(H, 3) - H, 0, None); cav = np.clip(cav / (np.percentile(cav[m > 0.5], 98) + 1e-6), 0, 1)
        basso = np.clip(1 - H / (np.percentile(H[m > 0.5], 90) + 1e-6), 0, 1)
        pat = noise(h, w, 2.2, seed=hash(nome) % 1000)
        T = np.clip(0.75 * cav + 0.35 * basso * pat + 0.15 * (pat > 0.7), 0, 1)
        argento = np.array([0.80, 0.785, 0.75]); nero = np.array([0.11, 0.095, 0.08])
        graffi = blur((rng.random((h, w)) > 0.995).astype(np.float32), [0.3, 3.0])
        col = lerp(argento * (0.92 + 0.12 * pat[..., None]), nero, T)
        col = col * (1 + 0.25 * graffi[..., None])
        sl = (slice(y, y + h), slice(x, x + w))
        alb[sl] = col; alpha[sl] = m
        nrm[sl] = normal_map(H, 9.0)
        met[sl] = 0.8 - 0.45 * T; smo[sl] = np.clip(0.72 - 0.45 * T - 0.1 * pat, 0.15, 0.9); occ[sl] = 1 - 0.65 * cav
        regioni[nome] = [x / N, 1 - (y + h) / N, w / N, h / N]  # u, v, larghezza, altezza (UV con v in alto)
        hm = Image.fromarray((norm01(blur(H, 3.0)) * 65535).astype(np.uint16))
        hm.save(os.path.join(TMP, "exvoto_" + nome + "_h.png"))
        Image.fromarray(rgb(blur(m, 1.5))).save(os.path.join(TMP, "exvoto_" + nome + "_m.png"))
    # bordi: estendo il colore fuori dalla maschera (niente aloni neri con il mipmap)
    a = np.dstack([rgb(alb), rgb(alpha)])
    salva("T_ExVoto_A.png", a)
    salva("T_ExVoto_N.jpg", nrm, 92)
    salva("T_ExVoto_MS.png", np.asarray(Image.fromarray(ms(met, occ, smo)).resize((512, 512), Image.LANCZOS)))
    json.dump(regioni, open(os.path.join(TMP, "exvoto_regioni.json"), "w"), indent=1)


# ----------------------------------------------------------------------------------------------- legni periodici
def legno(N, base, scuro, anelli, seed, forza=3.0, nodi=True):
    n1 = noise(N, N, 2.6, seed); n2 = noise(N, N, 3.2, seed + 1); fine = noise(N, N, 1.0, seed + 2)
    xx = np.arange(N)[None, :] / N
    v = np.sin(2 * math.pi * (anelli * xx + 2.2 * n1 + 0.5 * n2)) * 0.5 + 0.5
    v = v ** 1.6
    pori = blur((np.random.default_rng(seed).random((N, N)) > 0.97).astype(np.float32), [2.5, 0.4], wrap=True)
    t = np.clip(0.65 * v + 0.25 * n2 + 0.25 * pori, 0, 1)
    col = lerp(np.array(base), np.array(scuro), t) * (0.92 + 0.12 * fine[..., None])
    h = 0.6 * (1 - v) + 0.3 * n2 - 0.4 * pori
    return np.clip(col, 0, 1), normal_map(blur(norm01(h), 0.6, True), forza, wrap=True), t


def texture_legni():
    c, n, t = legno(512, (0.62, 0.45, 0.28), (0.40, 0.26, 0.14), 9, 11)
    salva("T_LegnoChiaro_A.jpg", rgb(c)); salva("T_LegnoChiaro_N.jpg", n)
    c, n, t = legno(512, (0.24, 0.13, 0.07), (0.09, 0.05, 0.03), 13, 21, 2.5)
    salva("T_LegnoNoce_A.jpg", rgb(c)); salva("T_LegnoNoce_N.jpg", n)


# ----------------------------------------------------------------------------------------------- ottone e oro
def texture_ottone():
    N = 512
    n1 = noise(N, N, 2.4, 31); n2 = noise(N, N, 1.4, 32)
    graffi = np.zeros((N, N), np.float32)
    r = np.random.default_rng(33)
    im = Image.new("L", (N, N), 0); d = ImageDraw.Draw(im)
    for _ in range(260):
        x, y = r.random(2) * N; a = r.random() * math.pi; L = 6 + r.random() * 60
        d.line([(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], fill=int(90 + r.random() * 160), width=1)
    graffi = np.asarray(im).astype(np.float32) / 255
    patina = np.clip((n1 - 0.45) * 2.2, 0, 1)
    ottone = np.array([0.66, 0.50, 0.24]); verde = np.array([0.22, 0.20, 0.12])
    col = lerp(ottone * (0.9 + 0.15 * n2[..., None]), verde, patina * 0.85) + 0.12 * graffi[..., None]
    salva("T_Ottone_A.jpg", rgb(col))
    salva("T_Ottone_N.jpg", normal_map(blur(0.6 * n2 - 0.5 * graffi, 0.6, True), 2.0, True))
    salva("T_Ottone_MS.png", np.asarray(Image.fromarray(ms(0.75 - 0.5 * patina, 1 - 0.3 * patina, np.clip(0.62 - 0.4 * patina + 0.1 * graffi - 0.1 * n2, 0.1, 0.9))).resize((256, 256), Image.LANCZOS)))


# ----------------------------------------------------------------------------------------------- damasco e marmo (edicola)
def texture_damasco():
    N = 512
    yy, xx = np.mgrid[0:N, 0:N] / N
    # ogiva: losanghe curve che si ripetono; fiore al centro e agli angoli
    o1 = np.abs(xx - 0.5 - 0.22 * np.sin(2 * math.pi * yy)); o2 = np.abs(xx - 0.5 + 0.22 * np.sin(2 * math.pi * yy))
    rete = np.clip(1 - np.minimum(o1, o2) / 0.018, 0, 1)
    def fiore(cx, cy, R):
        dx = (xx - cx + 0.5) % 1 - 0.5; dy = (yy - cy + 0.5) % 1 - 0.5
        r = np.sqrt(dx * dx + dy * dy); a = np.arctan2(dy, dx)
        petali = R * (0.55 + 0.45 * np.abs(np.cos(3 * a)))
        f = np.clip((petali - r) / 0.01, 0, 1)
        f -= np.clip((R * 0.22 - r) / 0.01, 0, 1) * 0.6
        return np.clip(f, 0, 1)
    disegno = np.clip(rete + fiore(0.5, 0.5, 0.15) + fiore(0.0, 0.0, 0.15) + 0.7 * fiore(0.5, 0.0, 0.06) + 0.7 * fiore(0.0, 0.5, 0.06), 0, 1)
    disegno = blur(disegno, 0.8, True)
    pelo = noise(N, N, 0.6, 41); macchie = noise(N, N, 2.8, 42)
    velluto = np.array([0.30, 0.045, 0.05]); seta = np.array([0.46, 0.13, 0.10])
    col = lerp(velluto * (0.85 + 0.25 * pelo[..., None]), seta, disegno) * (0.8 + 0.3 * macchie[..., None])
    col = lerp(col, np.array([0.3, 0.22, 0.18]), np.clip(macchie - 0.7, 0, 1) * 1.5)  # polvere e scolorimento
    salva("T_Damasco_A.jpg", rgb(col))
    salva("T_Damasco_N.jpg", normal_map(norm01(0.6 * disegno + 0.25 * pelo), 3.0, True))
    salva("T_Damasco_MS.png", np.asarray(Image.fromarray(ms(np.zeros((N, N)), 1 - 0.3 * (1 - disegno), 0.12 + 0.35 * disegno)).resize((256, 256), Image.LANCZOS)))


def texture_marmo():
    N = 512
    n1 = noise(N, N, 2.0, 51); n2 = noise(N, N, 3.0, 52)
    yy, xx = np.mgrid[0:N, 0:N] / N
    vene = np.abs(np.sin(2 * math.pi * (2 * xx + 1 * yy + 3.0 * n1)))
    vene = np.clip(1 - vene / 0.07, 0, 1) * (0.5 + 0.5 * n2)
    col = lerp(np.array([0.84, 0.82, 0.77]) * (0.93 + 0.1 * n2[..., None]), np.array([0.42, 0.40, 0.38]), vene * 0.8)
    col = lerp(col, np.array([0.55, 0.48, 0.38]), np.clip(n2 - 0.6, 0, 1) * 1.2)  # sporco giallino
    salva("T_Marmo_A.jpg", rgb(col))


# ----------------------------------------------------------------------------------------------- illustrazione d'epoca (golfo con il Vesuvio)
def illustrazione(w, h, seed=7):
    """Paesaggio originale dipinto da codice: cielo caldo, Vesuvio col pennacchio, mare, costa con case e un pino. 0..1 RGB."""
    r = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]; u = xx / w; v = yy / h
    cielo = lerp(np.array([0.93, 0.80, 0.58]), np.array([0.62, 0.70, 0.74]), np.clip(1 - v / 0.55, 0, 1)[..., None] ** 1.2)
    img = cielo.copy()
    nub = noise(h, w, 2.5, seed); img = lerp(img, np.array([0.97, 0.90, 0.78]), (np.clip(nub - 0.55, 0, 1) * 1.8 * (v < 0.4))[..., None])
    # Vesuvio: due cime (Somma e cono)
    prof = 0.52 - 0.3 * np.exp(-((u[0] - 0.62) / 0.16) ** 2) - 0.2 * np.exp(-((u[0] - 0.45) / 0.14) ** 2) + 0.012 * noise(1, w, 1.8, seed + 1)[0]
    monte = (v > prof[None, :]).astype(np.float32)
    ombra = np.clip((u - 0.62) * 3, 0, 1)
    colm = lerp(np.array([0.52, 0.48, 0.55]), np.array([0.36, 0.33, 0.40]), ombra[..., None])
    img = lerp(img, colm, monte * (v < 0.62))
    # pennacchio
    pen = np.exp(-(((u - 0.63 - (0.22 - v) * 0.6) / (0.03 + (0.22 - v) * 0.35)) ** 2)) * (v < 0.22) * (v > 0.02)
    img = lerp(img, np.array([0.86, 0.84, 0.82]), np.clip(pen * (0.6 + 0.6 * nub), 0, 0.85))
    # mare
    mare = (v > 0.6).astype(np.float32)
    onde = noise(h, w, 1.5, seed + 2)
    colmare = lerp(np.array([0.28, 0.45, 0.52]), np.array([0.18, 0.30, 0.38]), np.clip((v - 0.6) / 0.4, 0, 1)[..., None]) + 0.08 * (onde[..., None] - 0.5)
    img = lerp(img, colmare, mare)
    img = lerp(img, np.array([0.95, 0.85, 0.65]), (np.clip(1 - np.abs(u - 0.62) / 0.05, 0, 1) * mare * (onde > 0.55) * 0.5))
    # costa a sinistra che scende nel mare, con case bianche e rosa
    linea = 0.47 + 0.42 * u + 0.02 * np.sin(u * 23) + 0.015 * noise(h, w, 2.0, seed + 5)
    costa = (v > linea) & (u < 0.55)
    img = lerp(img, np.array([0.40, 0.42, 0.25]) * (0.85 + 0.2 * nub[..., None]), blur(costa.astype(np.float32), 1.2))
    for _ in range(30):
        cx = r.uniform(0.0, 0.32); cy = 0.49 + 0.42 * cx + r.uniform(0.01, 0.1); cw = r.uniform(0.012, 0.03); ch = r.uniform(0.015, 0.035)
        c = r.choice([[0.93, 0.88, 0.78], [0.88, 0.70, 0.62], [0.95, 0.85, 0.6]])
        sel = (np.abs(u - cx) < cw) & (v > cy - ch) & (v < cy)
        img[sel] = np.array(c) * (0.85 + 0.12 * (u[sel] < cx)[..., None])
    # pino marittimo in primo piano a destra: tronco curvo e chioma a ombrello fatta di ciuffi
    tr = np.zeros((h, w), np.float32)
    for k in range(60):
        t = k / 59.0; px = 0.9 - 0.06 * t - 0.03 * math.sin(t * 3); py = 1.0 - 0.72 * t
        tr += np.exp(-(((u - px) * w) ** 2 + ((v - py) * h) ** 2) / (2 * (0.006 * w) ** 2))
    img = lerp(img, np.array([0.20, 0.13, 0.09]), np.clip(tr, 0, 1))
    chioma = np.zeros((h, w), np.float32)
    for _ in range(26):
        cx = r.uniform(0.70, 0.98); cy = 0.27 + r.uniform(-0.04, 0.03) - 0.06 * (1 - abs(cx - 0.84) / 0.14) ** 2
        chioma += np.exp(-(((u - cx) / 0.04) ** 2 + ((v - cy) / 0.022) ** 2))
    chioma = np.clip(chioma * 1.5, 0, 1) * (0.7 + 0.3 * noise(h, w, 1.0, seed + 3))
    img = lerp(img, np.array([0.12, 0.19, 0.12]), np.clip(chioma, 0, 1))
    # pennellata e stampa litografica: grana, leggero fuori registro, colori sbiaditi
    pen = noise(h, w, 0.8, seed + 4)
    img = img * (0.92 + 0.12 * pen[..., None])
    img[..., 0] = np.roll(img[..., 0], 1, 1)
    img = lerp(img, np.array([0.80, 0.72, 0.56]), 0.22)
    return np.clip(img, 0, 1)


# ----------------------------------------------------------------------------------------------- carta e calendario
def carta(w, h, seed, colore=(0.86, 0.79, 0.62)):
    n1 = noise(h, w, 2.6, seed); n2 = noise(h, w, 0.9, seed + 1)
    col = np.array(colore) * (0.94 + 0.08 * n1[..., None] + 0.05 * n2[..., None])
    mac = np.zeros((h, w), np.float32)
    r = np.random.default_rng(seed)
    for _ in range(int(w * h / 9000)):
        x, y = r.random() * w, r.random() * h; rr = r.uniform(1, 6)
        yy, xx = np.ogrid[0:h, 0:w]
        mac += np.clip(1 - np.sqrt((xx - x) ** 2 + (yy - y) ** 2) / rr, 0, 1) * r.uniform(0.2, 0.6)
    col = lerp(col, np.array([0.55, 0.40, 0.22]), np.clip(mac, 0, 1) * 0.6)
    yy, xx = np.mgrid[0:h, 0:w]
    bordo = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy)).astype(np.float32)
    col = lerp(col, np.array([0.58, 0.45, 0.28]), np.clip(1 - bordo / (0.04 * min(w, h)), 0, 1) * (0.5 + 0.5 * n1))
    return col, n2


def texture_calendario():
    W, Hh = 512, 1024           # foglio 0,44 x 0,76 m: in alto l'illustrazione, sotto lo spazio per i conti
    col, fibra = carta(W, Hh, 61)
    mm = Hh / 0.76
    top = int(0.035 * mm)        # listello metallico
    ih = int(0.27 * mm)
    ill = illustrazione(W - 40, ih - 10, seed=9)
    col[top + 8: top + 8 + ill.shape[0], 20:20 + ill.shape[1]] = ill
    im = Image.fromarray(rgb(col)); d = ImageDraw.Draw(im)
    inch = (92, 40, 30)
    d.rectangle([16, top + 4, W - 17, top + 12 + ill.shape[0]], outline=inch, width=3)
    d.rectangle([10, top - 2, W - 11, Hh - 12], outline=inch, width=1)
    # righe tenui per i conti (in basso), la data e i mesi li mette il gioco dal JSON
    y0 = top + ih + int(0.11 * mm)
    for k in range(9):
        y = y0 + k * int(0.032 * mm)
        d.line([(24, y), (W - 24, y)], fill=(170, 150, 118), width=1)
    col = np.asarray(im).astype(np.float32) / 255
    # pieghe e strappi dei mesi gia' tolti sotto il listello
    piega = np.zeros((Hh, W), np.float32)
    for yy in (int(Hh * 0.47), int(Hh * 0.71)):
        piega[yy - 1:yy + 2, :] = 1
    piega = blur(piega, 1.5)
    col = col * (1 - 0.12 * piega[..., None])
    strappo = np.zeros((Hh, W), np.float32)
    r = np.random.default_rng(62)
    for x in range(W):
        strappo[top:top + int(6 + 6 * r.random()), x] = 1
    col = lerp(col, np.array([0.93, 0.88, 0.74]), strappo * 0.6)
    salva("T_Calendario_A.jpg", rgb(col))
    salva("T_Calendario_N.jpg", np.asarray(Image.fromarray(normal_map(norm01(0.7 * fibra - 0.8 * piega), 2.0)).resize((256, 512), Image.LANCZOS)))


def texture_segno_matita():
    """Scarpetta disegnata a matita (decal con trasparenza): profilo ripassato due volte e tratteggio."""
    W, H = 256, 128
    im = Image.new("L", (W * 2, H * 2), 0); d = ImageDraw.Draw(im)
    r = random.Random(71)
    prof = [(0.06, 0.62), (0.08, 0.38), (0.2, 0.3), (0.34, 0.33), (0.46, 0.42), (0.62, 0.46), (0.8, 0.5), (0.93, 0.6), (0.94, 0.74), (0.06, 0.74), (0.06, 0.62)]
    for passata in range(2):
        pts = [((x + r.uniform(-0.008, 0.008)) * W * 2, (y + r.uniform(-0.015, 0.015)) * H * 2) for x, y in prof]
        d.line(pts, fill=230 - passata * 40, width=11, joint="curve")
    d.line([(0.06 * W * 2, 0.8 * H * 2), (0.94 * W * 2, 0.8 * H * 2)], fill=220, width=9)
    for k in range(9):
        x = (0.12 + k * 0.07) * W * 2
        d.line([(x, 0.47 * H * 2 + k * 2), (x + 22, 0.7 * H * 2)], fill=170, width=7)
    d.line([(0.32 * W * 2, 0.36 * H * 2), (0.36 * W * 2, 0.5 * H * 2)], fill=210, width=8)
    a = np.asarray(im.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    a = np.clip(a * 1.15, 0, 1) * (0.8 + 0.2 * noise(H, W, 0.5, 72))
    grafite = np.zeros((H, W, 4), np.uint8); grafite[..., :3] = (62, 62, 66); grafite[..., 3] = rgb(a)
    salva("T_SegnoMatita_A.png", grafite)


# ----------------------------------------------------------------------------------------------- latta litografata (cassetta)
LATTA = {  # regioni in pixel dell'atlante 1024 (x, y, w, h)
    "coperchio": (0, 0, 960, 650), "fronte_retro": (0, 650, 1024, 170), "fianchi": (0, 820, 683, 170),
    "nudo": (683, 820, 341, 170), "fascia": (0, 990, 1024, 34),
}


def texture_latta():
    N = 1024
    T = TESTI["latta"]
    fs = lambda s: ImageFont.truetype(FONT_STAMPA, s); fr = lambda s: ImageFont.truetype(FONT_STAMPA_R, s)
    fondo = (38, 62, 58); crema = (226, 210, 168); oro = (196, 156, 72); rosso = (142, 38, 30); nero = (24, 22, 20)
    im = Image.new("RGB", (N, N), (150, 150, 145)); d = ImageDraw.Draw(im)
    hgt = Image.new("L", (N, N), 0); dh = ImageDraw.Draw(hgt)
    # coperchio: fondo verde petrolio, cornice d'oro sbalzata, ovale col golfo, nastro col nome
    x, y, w, h = LATTA["coperchio"]
    d.rectangle([x, y, x + w, y + h], fill=fondo)
    for k, c in ((14, oro), (22, fondo), (28, oro)):
        d.rectangle([x + k, y + k, x + w - k, y + h - k], outline=c, width=4 if k != 22 else 2)
    dh.rectangle([x + 12, y + 12, x + w - 12, y + h - 12], outline=200, width=8)
    for cx, cy in ((x + 60, y + 60), (x + w - 60, y + 60), (x + 60, y + h - 60), (x + w - 60, y + h - 60)):
        for rr, c in ((24, oro), (16, rosso), (8, crema)):
            d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=c)
        dh.ellipse([cx - 24, cy - 24, cx + 24, cy + 24], fill=160)
    ox, oy, ow, oh = x + 250, y + 80, 460, 330
    d.ellipse([ox - 14, oy - 14, ox + ow + 14, oy + oh + 14], fill=oro)
    d.ellipse([ox - 6, oy - 6, ox + ow + 6, oy + oh + 6], fill=rosso)
    ill = Image.fromarray(rgb(illustrazione(ow, oh, seed=5)))
    mo = Image.new("L", (ow, oh), 0); ImageDraw.Draw(mo).ellipse([0, 0, ow - 1, oh - 1], fill=255)
    im.paste(ill, (ox, oy), mo)
    dh.ellipse([ox - 14, oy - 14, ox + ow + 14, oy + oh + 14], outline=230, width=10)
    # nastro
    by = oy + oh + 30
    nastro = [(x + 150, by), (x + w - 150, by), (x + w - 120, by + 45), (x + w - 150, by + 90), (x + 150, by + 90), (x + 120, by + 45)]
    d.polygon(nastro, fill=crema, outline=oro); dh.polygon(nastro, fill=120)
    def centra(testo, font, cy, fill, x0=x, x1=x + w):
        bb = d.textbbox((0, 0), testo, font=font)
        d.text(((x0 + x1 - (bb[2] - bb[0])) / 2, cy - (bb[3] - bb[1]) / 2 - bb[1]), testo, font=font, fill=fill)
    centra(T["marca"], fs(54), by + 45, rosso)
    centra(T["prodotto"], fs(34), y + 45, crema)
    centra(T["sottotitolo"], fr(26), y + h - 46, crema)
    # fronte e retro: fascia a greca, nome ripetuto, piastra della serratura (modello) al centro del fronte
    x, y, w, h = LATTA["fronte_retro"]
    d.rectangle([x, y, x + w, y + h], fill=rosso)
    for yy0 in (y + 6, y + h - 22):
        d.rectangle([x, yy0, x + w, yy0 + 16], fill=nero)
        for k in range(0, w, 32):
            d.line([(x + k + 4, yy0 + 12), (x + k + 4, yy0 + 4), (x + k + 20, yy0 + 4), (x + k + 20, yy0 + 10), (x + k + 12, yy0 + 10)], fill=oro, width=2)
    half = w // 2
    for k in range(2):
        cx0 = x + k * half
        if k == 1:
            centra(T["marca"], fs(38), y + h / 2, crema, cx0, cx0 + half)
        else:
            centra(T["fronte"], fr(22), y + 40, crema, cx0, cx0 + half)
    x, y, w, h = LATTA["fianchi"]
    d.rectangle([x, y, x + w, y + h], fill=fondo)
    for k in range(2):
        cx0 = x + k * (w // 2)
        d.rectangle([cx0 + 14, y + 14, cx0 + w // 2 - 14, y + h - 14], outline=oro, width=3)
        centra(T["fianco"], fr(24), y + h / 2, crema, cx0, cx0 + w // 2)
    # fascia del coperchio: oro con l'etichetta di carta incollata sul davanti (la scritta a mano la mette il gioco)
    x, y, w, h = LATTA["fascia"]
    d.rectangle([x, y, x + w, y + h], fill=oro)
    eti = (x + 25, y + 3, x + 281, y + h - 3)   # davanti, centrata (fronte = primi 0,31 m dei 1,04 del giro)
    # lato nudo (interno, fondo): banda stagnata
    x, y, w, h = LATTA["nudo"]
    d.rectangle([x, y, x + w, y + h], fill=(168, 166, 158))
    col = np.asarray(im).astype(np.float32) / 255
    H = blur(np.asarray(hgt).astype(np.float32) / 255, 2.0)
    # carta dell'etichetta
    e = np.zeros((N, N), np.float32); e[eti[1]:eti[3], eti[0]:eti[2]] = 1
    e = blur(e, 0.8) * (0.8 + 0.2 * noise(N, N, 1.0, 81))
    carta_c = np.array([0.84, 0.77, 0.6]) * (0.9 + 0.12 * noise(N, N, 2.0, 82))[..., None]
    col = lerp(col, carta_c, np.clip(e, 0, 1))
    # usura: vernice sbiadita, graffi fino al metallo, ruggine ai bordi delle facce e nelle ammaccature
    n1 = noise(N, N, 2.4, 83); n2 = noise(N, N, 1.2, 84)
    col = lerp(col, np.array([0.62, 0.58, 0.48]), 0.14 + 0.1 * n1[..., None])
    bordi = np.zeros((N, N), np.float32)
    for (x, y, w, h) in LATTA.values():
        bordi[y:y + h, x:x + w] = 1
        bordi[y + 6:y + h - 6, x + 6:x + w - 6] = 0
    bordi = blur(bordi, 3) * 1.5
    rug = np.clip(bordi * (0.5 + n1) - 0.55 + 0.5 * np.clip(n2 - 0.8, 0, 1) * 3, 0, 1) * (1 - e)
    ruggine = np.array([0.36, 0.17, 0.07]) * (0.7 + 0.5 * n2[..., None])
    gr = Image.new("L", (N, N), 0); dg = ImageDraw.Draw(gr); r = random.Random(85)
    for _ in range(140):
        x0, y0 = r.random() * N, r.random() * N; a = r.random() * math.pi * 2; L = 4 + r.random() * 40
        dg.line([(x0, y0), (x0 + math.cos(a) * L, y0 + math.sin(a) * L)], fill=int(90 + r.random() * 120), width=1)
    graffi = np.asarray(gr).astype(np.float32) / 255 * (n2 > 0.35) * (1 - e)
    stagno = np.array([0.72, 0.71, 0.68])
    col = lerp(col, stagno, graffi * 0.6)
    col = lerp(col, ruggine, rug)
    salva("T_Latta_A.jpg", rgb(col))
    hh = 0.5 * H + 0.15 * n2 - 0.25 * graffi + 0.2 * rug * noise(N, N, 0.5, 86)
    salva("T_Latta_N.jpg", np.asarray(Image.fromarray(normal_map(norm01(hh), 4.0)).resize((512, 512), Image.LANCZOS)))
    metal = np.clip(graffi, 0, 1)
    nudo = np.zeros((N, N), np.float32); x, y, w, h = LATTA["nudo"]; nudo[y:y + h, x:x + w] = 1
    metal = np.clip(metal * 0.8 + nudo * 0.7, 0, 1) * (1 - rug)
    smooth = np.clip(0.5 - 0.15 * n1 + 0.15 * metal - 0.4 * rug - 0.35 * e, 0.08, 0.85)
    salva("T_Latta_MS.png", np.asarray(Image.fromarray(ms(metal, 1 - 0.4 * rug, smooth)).resize((512, 512), Image.LANCZOS)))
    json.dump({k: [v[0] / N, 1 - (v[1] + v[3]) / N, v[2] / N, v[3] / N] for k, v in LATTA.items()} | {"etichetta": [eti[0] / N, 1 - eti[3] / N, (eti[2] - eti[0]) / N, (eti[3] - eti[1]) / N]},
              open(os.path.join(TMP, "latta_regioni.json"), "w"), indent=1)


# ----------------------------------------------------------------------------------------------- stoffe: panno (tintabile), toppe, coperta, carta da fodera
def texture_panno():
    N = 512
    lino = carica("TessutoLino/TessutoLino_Albedo.jpg", (N // 2, N // 2))
    g = lino.mean(-1); g = g / g.mean() * 0.9
    g = np.tile(g, (2, 2))
    n1 = noise(N, N, 2.4, 91)
    g = g * (0.95 + 0.08 * n1) - 0.12 * np.clip(n1 - 0.72, 0, 1) * 3  # aloni di lavaggio
    yy, xx = np.mgrid[0:N, 0:N]
    orlo = ((yy > N - 18) | (xx < 10) | (xx > N - 11)).astype(np.float32)
    cuc = (((yy > N - 14) & (yy < N - 11)) & ((xx // 7) % 2 == 0)).astype(np.float32)
    g = g * (1 - 0.1 * orlo) - 0.2 * cuc
    salva("T_Panno_A.jpg", rgb(np.repeat(np.clip(g, 0, 1)[..., None], 3, -1)))
    nl = np.asarray(Image.open(os.path.join(SRC, "TessutoLino/TessutoLino_Normal.jpg")).convert("RGB").resize((N // 2, N // 2), Image.LANCZOS))
    nl = np.tile(nl, (2, 2, 1))
    salva("T_Panno_N.jpg", mescola_normali(nl, normal_map(blur(orlo - cuc * 0.6, 1.0), 4.0)))


def toppa(w, h, quadri, seed):
    r = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]
    trama = (np.sin(xx * math.pi * 2 / 3.0) * np.sin(yy * math.pi * 2 / 3.0)) * 0.5 + 0.5
    if quadri:
        # quadri scozzesi da salopette: bande di larghezza diversa, fili sottili, un po' storti (toppa cucita di sbieco)
        ang = math.radians(r.uniform(-7, 7))
        xr = xx * math.cos(ang) - yy * math.sin(ang); yr = xx * math.sin(ang) + yy * math.cos(ang)
        def bande(c):
            p = 58.0; t = (c % p) / p
            larga = (t < 0.42).astype(np.float32); stretta = ((t > 0.58) & (t < 0.66)).astype(np.float32); filo = ((t > 0.78) & (t < 0.8)).astype(np.float32)
            return blur(larga, 0.7), blur(stretta, 0.7), blur(filo, 0.5)
        lx, sx_, fx = bande(xr + 3 * noise(h, w, 2.0, seed + 7)); ly, sy_, fy = bande(yr + 3 * noise(h, w, 2.0, seed + 8))
        rosso = np.array([0.56, 0.20, 0.12]); scuro = np.array([0.16, 0.07, 0.05]); chiaro = np.array([0.78, 0.66, 0.48])
        base = np.ones((h, w, 3)) * rosso
        base = lerp(base, scuro, np.clip(0.55 * lx + 0.55 * ly, 0, 1))
        base = lerp(base, np.array([0.30, 0.24, 0.12]), np.clip(0.6 * sx_ + 0.6 * sy_, 0, 1))
        base = lerp(base, chiaro, np.clip(fx + fy, 0, 1) * 0.8)
        base = lerp(base, np.array([0.62, 0.50, 0.40]), 0.18 * noise(h, w, 2.5, seed + 9))  # sbiadita dai lavaggi
        filo_c = (0.82, 0.78, 0.66)
    else:
        base = np.ones((h, w, 3)) * np.array([0.44, 0.43, 0.40])
        filo_c = (0.18, 0.16, 0.14)
    base = base * (0.85 + 0.2 * trama[..., None]) * (0.9 + 0.15 * noise(h, w, 2.0, seed)[..., None])
    # orlo sfilacciato
    bn = noise(h, w, 1.6, seed + 1)
    d = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy)).astype(np.float32)
    alpha = np.clip((d - 3 - 8 * bn) / 3.0, 0, 1)
    # cucitura a punto filza tutto intorno, a 9 px dal bordo
    im = Image.new("L", (w, h), 0); dr = ImageDraw.Draw(im)
    k = 12
    for (x0, y0, x1, y1) in ((k, k, w - k, k), (w - k, k, w - k, h - k), (w - k, h - k, k, h - k), (k, h - k, k, k)):
        L = math.hypot(x1 - x0, y1 - y0); n = int(L / 11)
        for i in range(n):
            if i % 2: continue
            t0, t1 = i / n, (i + 0.8) / n
            j = r.uniform(-1.5, 1.5)
            dr.line([(x0 + (x1 - x0) * t0 + j, y0 + (y1 - y0) * t0 + j), (x0 + (x1 - x0) * t1, y0 + (y1 - y0) * t1)], fill=255, width=3)
    cuc = np.asarray(im).astype(np.float32) / 255
    col = lerp(base, np.array(filo_c), cuc)
    hgt = 0.4 * trama + 0.8 * blur(cuc, 0.8) + 0.3 * np.clip(d / 20, 0, 1)
    return col, alpha, hgt


def texture_toppe():
    W, H = 512, 256
    c1, a1, h1 = toppa(256, 256, True, 101)
    c2, a2, h2 = toppa(256, 256, False, 102)
    col = np.concatenate([c1, c2], 1); alpha = np.concatenate([a1, a2], 1); hg = np.concatenate([h1, h2], 1)
    salva("T_Toppe_A.png", np.dstack([rgb(col), rgb(alpha)]))
    salva("T_Toppe_N.jpg", normal_map(norm01(hg), 3.0))


def texture_coperta():
    N = 512
    yy, xx = np.mgrid[0:N, 0:N]
    pelo = noise(N, N, 0.7, 111); fiocchi = noise(N, N, 1.8, 112)
    twill = (np.sin((xx + yy) * math.pi * 2 / 6.0) * 0.5 + 0.5)
    base = np.array([0.36, 0.30, 0.24]) * (0.8 + 0.25 * pelo[..., None] + 0.08 * twill[..., None]) * (0.9 + 0.15 * fiocchi[..., None])
    riga = ((yy % 256) > 30) & ((yy % 256) < 46) | ((yy % 256) > 54) & ((yy % 256) < 60)
    base = np.where(riga[..., None], base * np.array([0.42, 0.40, 0.42]), base)
    salva("T_Coperta_A.jpg", rgb(base))
    salva("T_Coperta_N.jpg", normal_map(blur(0.7 * pelo + 0.3 * twill, 0.7, True), 3.0, True))


def texture_fodera():
    N = 512
    col, fib = carta(N, N, 121, (0.82, 0.76, 0.62))
    im = Image.fromarray(rgb(col)); d = ImageDraw.Draw(im)
    for j in range(8):
        for i in range(8):
            cx = i * 64 + (32 if j % 2 else 0); cy = j * 64 + 32
            for a in range(5):
                ang = a * math.pi * 2 / 5
                px, py = cx + math.cos(ang) * 7, cy + math.sin(ang) * 7
                d.ellipse([px - 5, py - 5, px + 5, py + 5], fill=(92, 110, 128))
            d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=(176, 124, 64))
            d.line([(cx + 10, cy + 10), (cx + 22, cy + 16)], fill=(98, 120, 82), width=2)
    a = np.asarray(im).astype(np.float32) / 255
    a = lerp(a, np.array([0.70, 0.62, 0.46]), 0.25 + 0.2 * noise(N, N, 2.2, 122)[..., None])
    salva("T_Fodera_A.jpg", rgb(a))


# ----------------------------------------------------------------------------------------------- assi incise del baule
ASSI = {"asse_croce": (0, 0, 341, 540), "asse_treno": (341, 0, 341, 540), "asse_scarpa": (682, 0, 342, 540), "fondo": (0, 560, 1024, 455)}


def texture_assi():
    N = 1024
    alb = carica("AssiConsunte/AssiConsunte_Albedo.jpg", (N, N))
    alb = alb * np.array([0.95, 0.85, 0.72]) * 1.05
    nb = np.asarray(Image.open(os.path.join(SRC, "AssiConsunte/AssiConsunte_Normal.jpg")).convert("RGB").resize((N, N), Image.LANCZOS))
    inc = np.zeros((N, N), np.float32)
    def segno(nome, x, y, w, h):
        S = 3
        im = Image.new("L", (w * S, h * S), 0); d = ImageDraw.Draw(im)
        L = lambda pts, wd=9: d.line([(px * w * S, py * h * S) for px, py in pts], fill=255, width=int(wd * 1.7) * S // 2, joint="curve")
        if nome == "asse_croce":
            L([(0.5, 0.3), (0.5, 0.72)]); L([(0.3, 0.44), (0.7, 0.44)])
        elif nome == "asse_treno":
            L([(0.22, 0.42), (0.22, 0.6), (0.74, 0.6), (0.74, 0.46), (0.42, 0.46), (0.42, 0.36), (0.26, 0.36), (0.26, 0.42), (0.22, 0.42)])
            L([(0.62, 0.46), (0.62, 0.36), (0.68, 0.36), (0.68, 0.46)])
            for cx in (0.32, 0.5, 0.66):
                d.ellipse([(cx - 0.06) * w * S, 0.6 * h * S, (cx + 0.06) * w * S, 0.6 * h * S + 0.12 * w * S], outline=255, width=15 * S // 2)
            L([(0.74, 0.6), (0.82, 0.62)], 6)
        elif nome == "asse_scarpa":
            L([(0.25, 0.62), (0.26, 0.42), (0.38, 0.4), (0.48, 0.48), (0.66, 0.52), (0.78, 0.58), (0.78, 0.64), (0.25, 0.64), (0.25, 0.62)])
            L([(0.2, 0.68), (0.8, 0.68)], 6)
        m = np.asarray(im.resize((w, h), Image.LANCZOS)).astype(np.float32) / 255
        inc[y:y + h, x:x + w] = np.maximum(inc[y:y + h, x:x + w], m)
    for k, (x, y, w, h) in ASSI.items():
        if k != "fondo": segno(k, x, y, w, h)
    sc = blur(inc, 1.2)
    fresco = np.clip(blur(inc, 3.0) - sc, 0, 1) * 2      # legno chiaro appena intagliato sui bordi del solco
    alb = lerp(alb, np.array([0.07, 0.045, 0.03]), np.clip(sc * 1.3, 0, 1))
    alb = lerp(alb, np.array([0.70, 0.55, 0.38]), np.clip(fresco, 0, 1) * 0.5)
    # bordi delle assi piu' scuri (mani, polvere)
    yy, xx = np.mgrid[0:N, 0:N]
    for (x, y, w, h) in ASSI.values():
        d = np.minimum(np.minimum(xx - x, x + w - 1 - xx), np.minimum(yy - y, y + h - 1 - yy)).astype(np.float32)
        dentro = (xx >= x) & (xx < x + w) & (yy >= y) & (yy < y + h)
        alb = np.where(dentro[..., None], lerp(alb, alb * 0.55, np.clip(1 - d / 22, 0, 1) * dentro), alb)
    salva("T_AssiIncise_A.jpg", rgb(alb))
    salva("T_AssiIncise_N.jpg", mescola_normali(nb, normal_map(-sc, 9.0)))
    salva("T_AssiIncise_MS.png", np.asarray(Image.fromarray(ms(np.zeros((N, N)), 1 - 0.7 * sc, 0.2 - 0.1 * sc)).resize((512, 512), Image.LANCZOS)))
    json.dump({k: [v[0] / N, 1 - (v[1] + v[3]) / N, v[2] / N, v[3] / N] for k, v in ASSI.items()}, open(os.path.join(TMP, "assi_regioni.json"), "w"), indent=1)


# ----------------------------------------------------------------------------------------------- fotografia d'epoca e cuoio tintabile
def texture_foto():
    W, H = 384, 512
    yy, xx = np.mgrid[0:H, 0:W]; u = xx / W; v = yy / H
    sfondo = 0.55 + 0.25 * noise(H, W, 2.6, 131) - 0.2 * v
    img = sfondo.copy()
    def poli(pts, ss=2):
        im = Image.new("L", (W * ss, H * ss), 0); ImageDraw.Draw(im).polygon([(x * W * ss, y * H * ss) for x, y in pts], fill=255)
        return np.asarray(im.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    def ovale(cx, cy, rx, ry):
        return np.clip(1 - (((u - cx) / rx) ** 2 + ((v - cy) / ry) ** 2), 0, 1) ** 0.3
    luce = np.clip(1.15 - (u - 0.2) * 0.9, 0.5, 1.1)
    madre = poli([(0.40, 0.25), (0.47, 0.25), (0.56, 0.31), (0.58, 0.48), (0.55, 0.52), (0.6, 0.94), (0.24, 0.94), (0.31, 0.52), (0.29, 0.48), (0.31, 0.31)])
    capo_m = ovale(0.435, 0.19, 0.045, 0.06); capelli = ovale(0.435, 0.165, 0.05, 0.045) * (v < 0.19)
    bimbo = poli([(0.6, 0.6), (0.67, 0.6), (0.7, 0.66), (0.71, 0.8), (0.69, 0.84), (0.69, 0.95), (0.66, 0.95), (0.645, 0.84), (0.63, 0.84), (0.62, 0.95), (0.59, 0.95), (0.59, 0.84), (0.57, 0.8), (0.575, 0.66)])
    capo_b = ovale(0.635, 0.55, 0.035, 0.045)
    braccio = poli([(0.55, 0.5), (0.58, 0.5), (0.6, 0.66), (0.58, 0.67)])
    img = lerp(img, 0.30 * luce, np.clip(madre + braccio, 0, 1))
    img = lerp(img, 0.68 * luce, capo_m); img = lerp(img, 0.16, capelli)
    img = lerp(img, 0.42 * luce, bimbo); img = lerp(img, 0.7 * luce, capo_b); img = lerp(img, 0.22, ovale(0.635, 0.525, 0.037, 0.025) * (v < 0.535))
    pav = (v > 0.94).astype(np.float32); img = lerp(img, 0.48, pav)
    img = blur(img, 2.2)
    img = img * (1 - 0.55 * np.clip(np.sqrt((u - 0.5) ** 2 + (v - 0.5) ** 2) * 1.6 - 0.35, 0, 1))
    img += 0.06 * (np.random.default_rng(134).random((H, W)) - 0.5)
    gr = np.zeros((H, W), np.float32)
    r = random.Random(135); im = Image.new("L", (W, H), 0); d = ImageDraw.Draw(im)
    for _ in range(5):
        x0 = r.random() * W; d.line([(x0, r.random() * H), (x0 + r.uniform(-30, 30), r.random() * H)], fill=255, width=1)
    gr = np.asarray(im).astype(np.float32) / 255
    img = np.clip(img + 0.12 * gr, 0, 1)
    seppia = lerp(np.array([0.12, 0.08, 0.05]), np.array([0.90, 0.80, 0.62]), img)
    # cornicetta bianca con bordo dentellato
    bordo = 22
    dent = ((xx < bordo) | (xx > W - bordo) | (yy < bordo) | (yy > H - bordo))
    seppia = np.where(dent[..., None], np.array([0.86, 0.82, 0.70]) * (0.9 + 0.1 * noise(H, W, 2, 136)[..., None]), seppia)
    salva("T_Foto_A.jpg", rgb(seppia))


def texture_cuoio_tinta():
    """Cuoio neutro (chiaro, quasi grigio) da tingere col colore della parte: lucido, chiodato, militare..."""
    a = carica("Scarpe/T_Cuoio_Scarpe_Albedo.png", (512, 512))
    g = a.mean(-1); g = norm01(g) * 0.5 + 0.5
    g = g * (0.95 + 0.1 * noise(512, 512, 2.0, 141))
    salva("T_CuoioTinta_A.jpg", rgb(np.repeat(g[..., None], 3, -1)))


if __name__ == "__main__":
    exvoto_atlante()
    texture_legni(); texture_ottone(); texture_damasco(); texture_marmo()
    texture_calendario(); texture_segno_matita(); texture_latta()
    texture_panno(); texture_toppe(); texture_coperta(); texture_fodera(); texture_assi()
    texture_foto(); texture_cuoio_tinta()
