# R007 tappa 2: texture degli oggetti modellati da zero (crea_oggetti.py).
# Parte da texture CC0 Poly Haven (legno, pietra, lino) e genera a mano smalto, frutta, scala della radio e atlante dei panni.
# Scrive in Assets/Art/Models/<Cartella>/Textures/ e aggiunge la chiave "texture" ai json di ArtSource/Models/build.
# Uso: python texture_procedurali.py [Nome ...]
import os, sys, json, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

QUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, QUI)
import panni_def as PD

RADICE = os.path.abspath(os.path.join(QUI, '..', '..'))
META = os.path.join(QUI, 'build')
MODELLI = os.path.join(RADICE, 'Assets', 'Art', 'Models')
PH = os.path.join(os.path.expanduser('~'), 'NovaStrikeAssets', 'downloads', 'polyhaven')

rnd = np.random.default_rng(1946)


def ph(asset, tipo):
    return os.path.join(PH, asset, '%s_%s_1k.jpg' % (asset, tipo))


def rumore(w, h, sigma, ampiezza=1.0, seed=None):
    r = np.random.default_rng(seed) if seed is not None else rnd
    n = r.random((h, w)).astype(np.float32)
    im = Image.fromarray((n * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(sigma))
    a = np.asarray(im, dtype=np.float32) / 255.0
    a = (a - a.mean()) / (a.std() + 1e-6)
    return a * ampiezza


def salva_rgb(arr, percorso, q=88):
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), 'RGB').save(percorso, quality=q, subsampling=0)


def normal_da_altezza(h, forza):
    dx = np.gradient(h, axis=1) * forza
    dy = -np.gradient(h, axis=0) * forza
    n = np.stack([-dx, -dy, np.ones_like(h)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)


def mask_png(percorso, metallico, ao, levig):
    m = np.stack([metallico, ao, np.zeros_like(ao), levig], axis=-1).astype(np.uint8)
    Image.fromarray(m, 'RGBA').save(percorso, optimize=True)


def da_tile(asset, base, tinta=None, levig_extra=0.0):
    """Texture Poly Haven 1k copiate nel progetto (albedo jpg, normal png, mask png)."""
    out = {}
    alb = np.asarray(Image.open(ph(asset, 'diff')).convert('RGB'), dtype=np.float32)
    if tinta is not None:
        lum = alb.mean(axis=2, keepdims=True) / max(alb.mean(), 1.0)
        alb = lum * np.array(tinta, dtype=np.float32)
    salva_rgb(alb, base + '_albedo.jpg')
    out['albedo'] = os.path.basename(base) + '_albedo.jpg'
    Image.open(ph(asset, 'nor_gl')).convert('RGB').save(base + '_normal.jpg', quality=93, subsampling=0)
    out['normal'] = os.path.basename(base) + '_normal.jpg'
    ro = np.asarray(Image.open(ph(asset, 'rough')).convert('L'), dtype=np.float32)
    sm = np.clip(255 - ro + levig_extra * 255, 0, 255)
    mask_png(base + '_mask.png', np.zeros_like(ro), np.full_like(ro, 255), sm)
    out['mask'] = os.path.basename(base) + '_mask.png'
    return out


# ------------------------------------------------------------------ smalto

def smalto(base, nome):
    S = 512
    out = {}
    for var, col in (('', (236, 232, 218)), ('azzurro', (168, 192, 200))):
        r = np.random.default_rng(11)
        alb = np.zeros((S, S, 3), np.float32) + np.array(col, np.float32)
        alb *= (1.0 + rumore(S, S, 18, 0.025, 3)[..., None]) * (1.0 + rumore(S, S, 2, 0.012, 4)[..., None])
        chip = np.zeros((S, S), np.float32)
        yy, xx = np.mgrid[0:S, 0:S]
        cx = cy = S / 2
        for k in range(46):
            ang = r.uniform(0, 2 * math.pi)
            rad = (r.uniform(150, 245) if k < 36 else r.uniform(0, 110))
            px, py = cx + rad * math.cos(ang), cy + rad * math.sin(ang)
            rr = r.uniform(3, 11)
            d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2) + rumore(S, S, 3, 3.0, 100 + k)
            chip = np.maximum(chip, (d < rr).astype(np.float32))
        chipb = np.asarray(Image.fromarray((chip * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8)), np.float32) / 255.0
        ring = np.clip(np.asarray(Image.fromarray((chip * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5)), np.float32) / 255.0 - chipb, 0, 1)
        ferro = np.array([48, 44, 42], np.float32)
        ruggine = np.array([138, 78, 42], np.float32)
        alb = alb * (1 - chipb[..., None]) + ferro * chipb[..., None]
        alb = alb * (1 - ring[..., None] * 0.7) + ruggine * ring[..., None] * 0.7
        # usura leggera all'interno del fondo
        alb *= (1.0 - 0.06 * np.clip(rumore(S, S, 40, 1.0, 9), 0, 2))[..., None]
        suf = '_albedo_' + var if var else '_albedo'
        salva_rgb(alb, base + suf + '.jpg')
        out['albedo' + ('_' + var if var else '')] = os.path.basename(base) + suf + '.jpg'
        if not var:
            h = 1.0 - chipb * 0.8 + rumore(S, S, 1.2, 0.01, 5)
            Image.fromarray(normal_da_altezza(h, 6.0), 'RGB').save(base + '_normal.jpg', quality=93, subsampling=0)
            out['normal'] = os.path.basename(base) + '_normal.jpg'
            mask_png(base + '_mask.png', (chipb * 200), np.full((S, S), 255), np.clip(200 - chipb * 150, 0, 255))
            out['mask'] = os.path.basename(base) + '_mask.png'
    return out


# ------------------------------------------------------------------ frutta e verdura

def frutta(base, tipo):
    S = 256
    cfg = {'Arance': ((226, 126, 34), 0.5, 4.0), 'Pomodori': ((176, 38, 28), 0.62, 2.0), 'Verdura': ((98, 128, 54), 0.35, 5.0)}[tipo]
    col, sm, forza = cfg
    n = rumore(S, S, 6, 0.06, 21)[..., None] + rumore(S, S, 1.2, 0.04, 22)[..., None]
    alb = np.array(col, np.float32) * (1.0 + n)
    if tipo == 'Verdura':
        yy, xx = np.mgrid[0:S, 0:S]
        vene = np.abs(np.sin((xx + rumore(S, S, 8, 12, 23)) * 0.22)) ** 6
        alb *= (1.0 + 0.35 * vene[..., None])
    if tipo == 'Pomodori':
        alb *= (1.0 - 0.1 * np.clip(rumore(S, S, 20, 1, 24), 0, 2))[..., None]
    salva_rgb(alb, base + '_albedo.jpg')
    h = rumore(S, S, 1.0, 0.02, 25) + (rumore(S, S, 5, 0.03, 26) if tipo == 'Verdura' else 0)
    Image.fromarray(normal_da_altezza(h, forza), 'RGB').save(base + '_normal.jpg', quality=93, subsampling=0)
    mask_png(base + '_mask.png', np.zeros((S, S)), np.full((S, S), 255), np.full((S, S), int(sm * 255)))
    nome = os.path.basename(base)
    return {'albedo': nome + '_albedo.jpg', 'normal': nome + '_normal.jpg', 'mask': nome + '_mask.png'}


# ------------------------------------------------------------------ scala parlante della radio

def scala(base):
    S = 512
    img = Image.new('RGB', (S, S), (222, 210, 176))
    a = np.asarray(img, np.float32)
    yy, xx = np.mgrid[0:S, 0:S]
    d = np.sqrt(((xx - S / 2) / S) ** 2 + ((yy - S / 2) / S) ** 2)
    a *= (1.0 - 0.45 * np.clip(d - 0.2, 0, 1))[..., None] * (1.0 + rumore(S, S, 30, 0.03, 31))[..., None]
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGB')
    dr = ImageDraw.Draw(img)
    ink = (60, 38, 26)
    for band, y0 in enumerate((70, 215, 360)):
        dr.line([(30, y0), (S - 30, y0)], fill=ink, width=3)
        dr.line([(30, y0 + 78), (S - 30, y0 + 78)], fill=ink, width=3)
        n = 34 + band * 8
        for k in range(n + 1):
            x = 30 + k * (S - 60) / n
            lung = 38 if k % 5 == 0 else 20
            dr.line([(x, y0 + 3), (x, y0 + 3 + lung)], fill=ink, width=2 if k % 5 == 0 else 1)
        for k in range(0, n, 5):
            x = 30 + k * (S - 60) / n
            dr.ellipse([x - 3, y0 + 52, x + 3, y0 + 58], fill=ink)
    dr.line([(S * 0.38, 20), (S * 0.38, S - 20)], fill=(176, 38, 28), width=5)
    img.save(base + '_albedo.jpg', quality=90)
    nome = os.path.basename(base)
    return {'albedo': nome + '_albedo.jpg', 'levigatezza': 0.5}


# ------------------------------------------------------------------ atlante dei panni

def panni(base, var=''):
    AW, AH = PD.ATLANTE
    lin = Image.open(ph('rough_linen', 'diff')).convert('L')
    lin_a = np.asarray(lin.resize((256, 256), Image.LANCZOS), np.float32)
    lin_a /= lin_a.mean()
    nor = np.asarray(Image.open(ph('rough_linen', 'nor_gl')).convert('RGB').resize((256, 256), Image.LANCZOS), np.uint8)
    alb = np.zeros((AH, AW, 4), np.float32)
    nrm = np.zeros((AH, AW, 3), np.uint8)
    nrm[...] = (128, 128, 255)
    M = 6  # margine per sfilacciatura a sinistra, destra e in basso
    for nome, (x0, y0, cw, ch, poly, col, stile) in PD.PANNI.items():
        r = np.random.default_rng(sum(ord(c) for c in nome))
        reps = (ch // 256 + 1, cw // 256 + 1)
        tex = np.tile(lin_a, reps)[:ch, :cw]
        if var:
            col = PD.VARIANTI[var][nome]
        colore = np.array(col, np.float32) * 0.80   # colori spenti: nella luce coperta dell'hub il bianco pieno brucia
        c = colore[None, None, :] * (0.72 + 0.28 * tex[..., None])
        c *= (1.0 + rumore(cw, ch, 25, 0.035, int(r.integers(1000))))[..., None]
        yy, xx = np.mgrid[0:ch, 0:cw]
        if stile == 'quadri':
            rosso = np.array(PD.VARIANTI[var]['quadri'] if var else [140, 80, 72], np.float32)
            for dirz in (xx, yy):
                banda = ((dirz // 48) % 2 == 0).astype(np.float32) * 0.38
                c = c * (1 - banda[..., None]) + (c * 0.55 + rosso * 0.45) * banda[..., None]
        if stile in ('lenzuolo', 'rammendato', 'quadri'):
            for y in (10, ch - 18):
                c[y:y + 2] *= 0.9
            c[:, 10:12] *= 0.94
        ntoppe = {'rammendato': 5, 'lenzuolo': 2, 'camicia': 2, 'mutandoni': 2, 'quadri': 1, 'straccio': 3}[stile]
        if ntoppe:
            for k in range(ntoppe):
                pw, ph_ = int(r.integers(40, 80)) if stile in ('camicia', 'mutandoni', 'straccio') else int(r.integers(55, 100)), int(r.integers(40, 80)) if stile in ('camicia', 'mutandoni', 'straccio') else int(r.integers(55, 100))
                px, py = int(r.integers(20, cw - pw - 20)), int(r.integers(40, ch - ph_ - 40))
                tinta = colore * r.uniform(0.78, 1.12) + r.uniform(-6, 6, 3)
                c[py:py + ph_, px:px + pw] = tinta * (0.8 + 0.2 * tex[py:py + ph_, px:px + pw, None])
                for t in range(0, pw, 9):
                    c[py:py + 2, px + t:px + t + 5] *= 0.55
                    c[py + ph_ - 2:py + ph_, px + t:px + t + 5] *= 0.55
                for t in range(0, ph_, 9):
                    c[py + t:py + t + 5, px:px + 2] *= 0.55
                    c[py + t:py + t + 5, px + pw - 2:px + pw] *= 0.55
        if stile == 'camicia':
            c[:, cw // 2 - 1:cw // 2 + 1] *= 0.82                                # abbottonatura
            for k in range(5):
                yb = int(ch * (0.14 + 0.15 * k))
                c[yb - 3:yb + 3, cw // 2 - 3:cw // 2 + 3] = (230, 226, 210)
            c[:int(ch * 0.07)] *= 0.9                                              # colletto
            c[int(ch * 0.30):int(ch * 0.32), :] *= 0.95
            c[int(ch * 0.30):int(ch * 0.46), int(cw * 0.27):int(cw * 0.40)] *= 0.93  # taschino
        if stile == 'mutandoni':
            c[:int(ch * 0.12)] *= 0.9                                              # fascia in vita
            c[:, cw // 2 - 1:cw // 2 + 1] *= 0.9
            for dx in (-14, 14):
                c[int(ch * 0.05) - 3:int(ch * 0.05) + 3, cw // 2 + dx - 3:cw // 2 + dx + 3] = (214, 210, 196)
            c[ch - 18:ch - 15] *= 0.88
        if stile == 'straccio':
            c *= (1.0 - 0.25 * np.clip(rumore(cw, ch, 10, 1, 77), 0, 3))[..., None]
        # macchie morbide, piu' fitte verso il basso
        macchie = np.zeros((ch, cw), np.float32)
        for k in range(14 if stile != 'straccio' else 18):
            px, py = r.uniform(0, cw), ch * r.beta(3, 1.4)
            rr = r.uniform(12, 40)
            macchie = np.maximum(macchie, np.clip(1 - np.sqrt((xx - px) ** 2 + (yy - py) ** 2) / rr, 0, 1) * r.uniform(0.08, 0.30))
        c = c * (1 - macchie[..., None]) + np.array([112, 92, 62], np.float32) * macchie[..., None]
        # alpha: forma del panno con bordo sfilacciato (in alto resta dritto: e' pinzato alla corda)
        sup = 4
        mk = Image.new('L', (cw * sup, ch * sup), 0)
        dr = ImageDraw.Draw(mk)
        pts = [(M * sup + fx * (cw - 2 * M) * sup, fy * (ch - M) * sup) for fx, fy in poly]
        pts = [(x, y) for x, y in pts]
        # estendi i punti sul bordo superiore oltre il rettangolo: nessuna sfilacciatura in alto
        pts = [(x, y - 40 * sup if y <= 0.5 else y) for x, y in pts]
        dr.polygon(pts, fill=255)
        mk = mk.resize((cw, ch), Image.LANCZOS).filter(ImageFilter.GaussianBlur(2.2))
        bl = np.asarray(mk, np.float32) / 255.0
        fr = rumore(cw, ch, 4, 0.17, int(r.integers(1000))) + rumore(cw, ch, 1.0, 0.09, int(r.integers(1000)))
        pesobordo = np.clip(1 - np.abs(bl - 0.5) * 2.2, 0, 1)
        al = np.clip((bl + fr * pesobordo - 0.5) * 7.0 + 0.5, 0, 1)
        # fili piu' chiari sul bordo
        bordo = np.clip(1 - np.abs(bl - 0.5) * 4, 0, 1) * 0.12
        c = c * (1 - bordo[..., None]) + 245 * bordo[..., None]
        # fuori dal ritaglio: colore pieno (niente aloni neri nei mip)
        alb[y0:y0 + ch, x0:x0 + cw, :3] = c
        alb[y0:y0 + ch, x0:x0 + cw, 3] = al * 255
        nrm[y0:y0 + ch, x0:x0 + cw] = np.tile(nor, (reps[0], reps[1], 1))[:ch, :cw]
    # riempi le zone vuote dell'atlante col colore medio per evitare aloni nei mip
    vuoto = alb[..., 3] == 0
    alb[vuoto, :3] = 200
    suf = '_albedo_' + var if var else '_albedo'
    Image.fromarray(np.clip(alb, 0, 255).astype(np.uint8), 'RGBA').save(base + suf + '.png', optimize=True)
    nome = os.path.basename(base)
    if var:
        return {'albedo_' + var: nome + suf + '.png'}
    Image.fromarray(nrm, 'RGB').save(base + '_normal.jpg', quality=93, subsampling=0)
    return {'albedo': nome + suf + '.png', 'normal': nome + '_normal.jpg', 'levigatezza': 0.12}


def paglia(base):
    S = 256
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    a = np.sin((xx + yy) * 0.35) * 0.5 + 0.5
    b = np.sin((xx - yy) * 0.35) * 0.5 + 0.5
    trama = np.maximum(a, b) ** 1.5
    fibra = rumore(S, S, 0.8, 0.06, 41)
    alb = np.array([196, 156, 84], np.float32) * (0.55 + 0.45 * trama[..., None]) * (1.0 + fibra[..., None])
    alb *= (1.0 + rumore(S, S, 12, 0.08, 42))[..., None]
    salva_rgb(alb, base + '_albedo.jpg')
    Image.fromarray(normal_da_altezza(trama + fibra * 0.3, 3.0), 'RGB').save(base + '_normal.jpg', quality=93, subsampling=0)
    mask_png(base + '_mask.png', np.zeros((S, S)), np.full((S, S), 255), np.full((S, S), 80))
    nome = os.path.basename(base)
    return {'albedo': nome + '_albedo.jpg', 'normal': nome + '_normal.jpg', 'mask': nome + '_mask.png'}


RICETTE = {
    'LegnoBanco': lambda b: da_tile('weathered_planks', b),
    'TeloBanco': lambda b: da_tile('rough_linen', b, (138, 122, 94)),
    'LegnoPanchina': lambda b: da_tile('weathered_planks', b),
    'PietraPanchina': lambda b: da_tile('sandstone_cracks', b, (104, 94, 80)),
    'LegnoSedia': lambda b: da_tile('dark_wood', b, (104, 76, 54)),
    'Paglia': lambda b: paglia(b),
    'LegnoGabbia': lambda b: da_tile('weathered_planks', b),
    'LegnoCassa': lambda b: da_tile('weathered_planks', b),
    'PietraLavatoio': lambda b: da_tile('sandstone_cracks', b, (112, 102, 90)),
    'LegnoRadio': lambda b: da_tile('dark_wood', b, None, 0.2),
    'TelaRadio': lambda b: da_tile('rough_linen', b, (150, 120, 82)),
    'SmaltoBianco': lambda b: smalto(b, 'SmaltoBianco'),
    'Frutta_Arance': lambda b: frutta(b, 'Arance'),
    'Frutta_Pomodori': lambda b: frutta(b, 'Pomodori'),
    'Frutta_Verdura': lambda b: frutta(b, 'Verdura'),
    'ScalaRadio': lambda b: scala(b),
    'PanniTessuto': lambda b: {**panni(b), **panni(b, 'B'), **panni(b, 'C')},
}
DONE = {}


def lavora(nome):
    with open(os.path.join(META, nome + '.json'), encoding='utf-8') as f:
        meta = json.load(f)
    out = os.path.join(MODELLI, meta['cartella'], 'Textures')
    os.makedirs(out, exist_ok=True)
    mappe = {}
    for mat in meta['materiali']:
        if mat in RICETTE:
            if (meta['cartella'], mat) not in DONE:
                DONE[(meta['cartella'], mat)] = RICETTE[mat](os.path.join(out, mat))
            r = dict(DONE[(meta['cartella'], mat)])
            r.setdefault('levigatezza', 0.3)
            mappe[mat] = r
            print('TEX', nome, mat, r)
        else:
            print('PIATTO', nome, mat)
    meta['texture'] = mappe
    with open(os.path.join(META, nome + '.json'), 'w', encoding='utf-8') as f:
        json.dump(meta, f, indent=1, ensure_ascii=False)


if __name__ == '__main__':
    nomi = sys.argv[1:] or ['Bancarella', 'PanchinaPopolare', 'SediaPaglia', 'GabbiaGalline', 'Lavatoio', 'Bacinella', 'CassaFrutta_Arance', 'CassaFrutta_Pomodori', 'CassaFrutta_Verdura',
                            'Radio', 'Panni'] + ['Panno_' + n for n in PD.PANNI]
    for n in nomi:
        lavora(n)
