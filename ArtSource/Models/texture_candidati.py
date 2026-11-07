# R007 tappa 2: texture dei modelli candidati in Assets/Art/Models/<Oggetto>/Textures/
#   <mat>_albedo.jpg, <mat>_normal.png|jpg, <mat>_mask.png  (mask URP: R metallico, G occlusione, B 0, A levigatezza)
# Legge ArtSource/Models/build/<Oggetto>.json scritto da importa_candidati.py.
# Uso: python -I texture_candidati.py [Nome ...]
import os, sys, json, glob
import numpy as np
from PIL import Image, ImageFilter

QUI = os.path.dirname(os.path.abspath(__file__))
RADICE = os.path.abspath(os.path.join(QUI, '..', '..'))
META = os.path.join(QUI, 'build')
MODELLI = os.path.join(RADICE, 'Assets', 'Art', 'Models')

# lato massimo delle texture (px); standard 1024, solo gli oggetti grandi a 2048
LATO = {'LettoFerro': 2048, 'Credenza': 2048, 'Sacco': 512, 'Valigia': 512, 'Valigia2': 512, 'PentolaRame': 512, 'PadellaRame': 512}
# oggetti senza texture sorgente (colori piatti): materiale gestito da Unity
SENZA = set()
# varianti ricolorate dell'albedo (nome variante -> colore RGB 0..255 medio finale)
VARIANTI = {}
# parte B: pentole e padelle solo di rame (l'ottone e' scartato): albedo ricolorato verso questo colore medio
RICOLORA = {'PentolaRame': (176, 98, 66), 'PadellaRame': (176, 98, 66)}
# oggetti per cui la normal si ricava dalla luminanza dell'albedo (forza)
NORMAL_DA_ALBEDO = {'MadonnaStatuetta': 3.0}
# levigatezza costante quando manca la mask
LEVIG_DEFAULT = {'MadonnaStatuetta': 0.25}


def trova(cartella, mat, chiavi):
    m = mat.lower() + '_'
    cand = []
    for f in os.listdir(cartella):
        b = f.lower()
        if b.startswith(m) and any(k in b for k in chiavi) and b.rsplit('.', 1)[-1] in ('png', 'jpg', 'jpeg'):
            cand.append(f)
    if not cand:
        return None
    cand.sort(key=lambda f: (('_1k' in f.lower()), -os.path.getsize(os.path.join(cartella, f))))
    return os.path.join(cartella, cand[0])


def ridimensiona(im, lato):
    if max(im.size) > lato:
        r = lato / max(im.size)
        im = im.resize((max(1, round(im.size[0] * r)), max(1, round(im.size[1] * r))), Image.LANCZOS)
    return im


def normal_da_altezza(rgb, forza):
    g = np.asarray(rgb.convert('L').filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float32) / 255.0
    dx = np.gradient(g, axis=1) * forza * 8.0
    dy = -np.gradient(g, axis=0) * forza * 8.0
    n = np.stack([-dx, -dy, np.ones_like(g)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8), 'RGB')


def salva_normal(im, base, lato):
    p = base + '_normal.jpg'   # parte B (LFS): normal sempre in JPG q93
    im.save(p, quality=93, subsampling=0)
    return p


def fattori_gltf(cartella_sorgente):
    """metallicFactor/roughnessFactor dei materiali del glTF (le texture MR di Sketchfab hanno spesso il blu a 255 con fattore 0)."""
    out = {}
    for f in os.listdir(cartella_sorgente):
        if f.lower().endswith('.gltf') and '_1k' not in f:
            with open(os.path.join(cartella_sorgente, f), encoding='utf-8') as fh:
                g = json.load(fh)
            for m in g.get('materials', []):
                pbr = m.get('pbrMetallicRoughness', {})
                out[m.get('name', '')] = (pbr.get('metallicFactor', 1.0), pbr.get('roughnessFactor', 1.0))
    return out


# tinta moltiplicata sull'albedo (modelli troppo chiari per un pomeriggio d'autunno del 1946)
TINTA = {'Fontana': (0.80, 0.76, 0.68)}


def lavora(nome):
    with open(os.path.join(META, nome + '.json'), encoding='utf-8') as f:
        meta = json.load(f)
    if nome in SENZA:
        print('SENZA TEXTURE', nome)
        return
    src = os.path.join(meta['sorgente'], 'textures')
    if not os.path.isdir(src):
        src = meta['sorgente']
    out = os.path.join(MODELLI, meta['cartella'], 'Textures')
    os.makedirs(out, exist_ok=True)
    lato = LATO.get(nome, 1024)
    fattori = fattori_gltf(meta['sorgente'])
    mappe = {}
    for mat in meta['materiali']:
        safe = mat.replace('.', '_')
        base = os.path.join(out, safe)
        r = {}
        pa = trova(src, mat, ['_diff', '_basecolor', '_diffuse'])
        if pa:
            alb = ridimensiona(Image.open(pa).convert('RGB'), lato)
            if nome in TINTA:
                alb = Image.fromarray(np.clip(np.asarray(alb, np.float32) * np.array(TINTA[nome], np.float32), 0, 255).astype(np.uint8), 'RGB')
            if nome in RICOLORA:
                a = np.asarray(alb, dtype=np.float32)
                k = np.array(RICOLORA[nome], dtype=np.float32) / np.maximum(a.reshape(-1, 3).mean(axis=0), 1.0)
                alb = Image.fromarray(np.clip(a * k, 0, 255).astype(np.uint8), 'RGB')
            alb.save(base + '_albedo.jpg', quality=88, subsampling=0)
            r['albedo'] = safe + '_albedo.jpg'
            for var, col in VARIANTI.get(nome, {}).items():
                a = np.asarray(alb, dtype=np.float32)
                media = a.reshape(-1, 3).mean(axis=0)
                k = np.array(col, dtype=np.float32) / np.maximum(media, 1.0)
                Image.fromarray(np.clip(a * k, 0, 255).astype(np.uint8), 'RGB').save(base + '_albedo_' + var + '.jpg', quality=88, subsampling=0)
                r['albedo_' + var] = safe + '_albedo_' + var + '.jpg'
        pn = trova(src, mat, ['_nor_gl', '_normal'])
        if pn:
            nor = ridimensiona(Image.open(pn).convert('RGB'), lato)
            r['normal'] = os.path.basename(salva_normal(nor, base, lato))
        elif nome in NORMAL_DA_ALBEDO and pa:
            nor = normal_da_altezza(alb, NORMAL_DA_ALBEDO[nome])
            r['normal'] = os.path.basename(salva_normal(nor, base, lato))
        po = trova(src, mat, ['_arm', '_metallicroughness'])
        if po:
            orm = ridimensiona(Image.open(po).convert('RGB'), lato)
            a = np.asarray(orm, dtype=np.uint8)
            ao = a[..., 0] if '_arm' in os.path.basename(po).lower() else np.full(a.shape[:2], 255, np.uint8)
            pao = trova(src, mat, ['_occlusion'])
            if pao:
                ao = np.asarray(ridimensiona(Image.open(pao).convert('L'), lato).resize(orm.size), dtype=np.uint8)
            mf, rf = fattori.get(mat, (1.0, 1.0))
            metal = np.clip(a[..., 2].astype(np.float32) * mf, 0, 255).astype(np.uint8)
            rough = np.clip(a[..., 1].astype(np.float32) * rf, 0, 255).astype(np.uint8)
            mask = np.stack([metal, ao, np.zeros_like(ao), 255 - rough], axis=-1)
            Image.fromarray(mask, 'RGBA').save(base + '_mask.png', optimize=True)
            r['mask'] = safe + '_mask.png'
        elif trova(src, mat, ['_occlusion']):
            ao = np.asarray(ridimensiona(Image.open(trova(src, mat, ['_occlusion'])).convert('L'), lato), dtype=np.uint8)
            mask = np.stack([np.zeros_like(ao), ao, np.zeros_like(ao), np.full_like(ao, int(LEVIG_DEFAULT.get(nome, 0.3) * 255))], axis=-1)
            Image.fromarray(mask, 'RGBA').save(base + '_mask.png', optimize=True)
            r['mask'] = safe + '_mask.png'
        r['levigatezza'] = LEVIG_DEFAULT.get(nome, 0.3)
        mappe[mat] = r
        print('TEX', nome, mat, r)
    meta['texture'] = mappe
    with open(os.path.join(META, nome + '.json'), 'w', encoding='utf-8') as f:
        json.dump(meta, f, indent=1, ensure_ascii=False)


if __name__ == '__main__':
    nomi = sys.argv[1:]
    if not nomi:
        nomi = [os.path.splitext(f)[0] for f in sorted(os.listdir(META)) if f.endswith('.json') and not f.endswith('.tex.json')]
    for n in nomi:
        lavora(n)
