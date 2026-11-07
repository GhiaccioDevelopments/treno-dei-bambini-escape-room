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
    'Lenzuolo': (0, 0, 528, 672, RETT, (224, 221, 212), 'lenzuolo'),
    'LenzuoloRammendato': (528, 0, 480, 624, RETT, (221, 213, 189), 'rammendato'),
    'Tovaglia': (0, 688, 576, 432, RETT, (220, 214, 198), 'quadri'),
    'Straccio': (592, 688, 240, 312, [(0.05, 0), (0.95, 0), (1.0, 0.85), (0.6, 1.0), (0.0, 0.9)], (112, 102, 88), 'straccio'),
    'Camicia': (0, 1136, 384, 360, _norm([(-0.21, 0), (-0.08, 0), (0, 0.05), (0.08, 0), (0.21, 0), (0.40, 0.28), (0.31, 0.36), (0.22, 0.20),
                                           (0.22, 0.75), (-0.22, 0.75), (-0.22, 0.20), (-0.31, 0.36), (-0.40, 0.28)], 0.8, 0.75, 0.4), (150, 170, 186), 'camicia'),
    'Mutandoni': (400, 1136, 288, 336, _norm([(-0.25, 0), (0.25, 0), (0.27, 0.70), (0.03, 0.70), (0.0, 0.30), (-0.03, 0.70), (-0.27, 0.70)], 0.6, 0.70, 0.3),
                  (226, 223, 212), 'mutandoni'),
}

# parametri di piegatura per il vento: (ampiezza pieghe m, frequenza pieghe, deriva laterale in basso m, risoluzione griglia m)
VENTO = {
    'Lenzuolo': (0.035, 3.0, 0.10, 0.075), 'LenzuoloRammendato': (0.030, 2.5, -0.08, 0.075),
    'Tovaglia': (0.030, 3.5, 0.06, 0.075), 'Straccio': (0.020, 2.0, 0.05, 0.06),
    'Camicia': (0.018, 2.0, 0.04, 0.06), 'Mutandoni': (0.016, 2.0, -0.04, 0.06),
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
