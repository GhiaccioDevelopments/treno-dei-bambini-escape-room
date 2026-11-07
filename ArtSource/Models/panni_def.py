# Definizione condivisa dei panni stesi (R007 tappa 2): atlante, ritagli e forme.
# Usata da crea_oggetti.py (Blender: mesh e UV) e texture_procedurali.py (PIL: texture).
# Coordinate delle forme normalizzate nel rettangolo del panno: fx 0..1 (sinistra-destra), fy 0..1 (0 = bordo
# appeso alla corda, 1 = orlo in basso).
ATLANTE = (1024, 2048)
DENSITA = 480.0  # pixel per metro: la dimensione fisica del panno e' (larghezza px, altezza px) / 480


def _norm(pts, w, h, ox):
    return [((x + ox) / w, y / h) for x, y in pts]


RETT = [(0, 0), (1, 0), (1, 1), (0, 1)]

PANNI = {
    # nome: (x0, y0, larghezza px, altezza px, forma, colore base RGB, stile)
    'Lenzuolo': (0, 0, 528, 672, RETT, (206, 203, 194), 'lenzuolo'),
    'LenzuoloRammendato': (528, 0, 480, 624, RETT, (196, 188, 168), 'rammendato'),
    'Tovaglia': (0, 688, 576, 432, RETT, (200, 194, 182), 'quadri'),
    'Straccio': (592, 688, 240, 312, [(0.05, 0), (0.95, 0), (1.0, 0.85), (0.6, 1.0), (0.0, 0.9)], (100, 92, 80), 'straccio'),
    'Camicia': (0, 1136, 384, 360, _norm([(-0.21, 0), (-0.08, 0), (0, 0.05), (0.08, 0), (0.21, 0), (0.40, 0.28), (0.31, 0.36), (0.22, 0.20),
                                           (0.22, 0.75), (-0.22, 0.75), (-0.22, 0.20), (-0.31, 0.36), (-0.40, 0.28)], 0.8, 0.75, 0.4), (128, 144, 156), 'camicia'),
    'Mutandoni': (400, 1136, 288, 336, _norm([(-0.25, 0), (0.25, 0), (0.27, 0.70), (0.03, 0.70), (0.0, 0.30), (-0.03, 0.70), (-0.27, 0.70)], 0.6, 0.70, 0.3),
                  (226, 223, 212), 'mutandoni'),
}

# parametri di piegatura per il vento: (ampiezza pieghe m, frequenza pieghe, deriva laterale in basso m, risoluzione griglia m)
VENTO = {
    'Lenzuolo': (0.065, 3.5, 0.14, 0.07), 'LenzuoloRammendato': (0.055, 3.0, -0.12, 0.07),
    'Tovaglia': (0.055, 4.0, 0.08, 0.07), 'Straccio': (0.040, 2.5, 0.07, 0.06),
    'Camicia': (0.035, 2.5, 0.06, 0.055), 'Mutandoni': (0.032, 2.5, -0.06, 0.055),
}


def dim_m(nome):
    x0, y0, w, h = PANNI[nome][:4]
    return w / DENSITA, h / DENSITA


def dentro(poly, fx, fy):
    """Punto nel poligono (ray casting)."""
    ins = False
    n = len(poly)
    j = n - 1
    for i in range(n):
        xi, yi = poly[i]
        xj, yj = poly[j]
        if (yi > fy) != (yj > fy) and fx < (xj - xi) * (fy - yi) / (yj - yi + 1e-12) + xi:
            ins = not ins
        j = i
    return ins


# varianti di colore dell'atlante (stesse UV): A e' quella base; ogni istanza nel gioco sceglie a caso una variante di materiale
# nome panno -> (colore base, colore a quadri per la tovaglia)
VARIANTI = {
    'B': {'Lenzuolo': (192, 186, 170), 'LenzuoloRammendato': (186, 180, 158), 'Tovaglia': (192, 196, 188), 'Straccio': (88, 84, 76),
          'Camicia': (156, 150, 128), 'Mutandoni': (176, 172, 160), 'quadri': (96, 118, 104)},
    'C': {'Lenzuolo': (176, 182, 186), 'LenzuoloRammendato': (170, 160, 140), 'Tovaglia': (208, 200, 186), 'Straccio': (120, 104, 90),
          'Camicia': (108, 118, 130), 'Mutandoni': (200, 198, 190), 'quadri': (96, 110, 132)},
}
