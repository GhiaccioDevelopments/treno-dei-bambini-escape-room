"""
R024 - Le cinque calzature da raccogliere nel ricordo "Le scarpe" (enigma "Il baule delle scarpe"), originali, fatte da codice in Blender.
Prima erano pezzi di primitive (o la stessa scarpa R023 ridimensionata): piccole, scure, uguali al selciato. Ora cinque modelli diversi e riconoscibili:
  calz_zoccoli   zoccolo di legno con fascia di cuoio e due chiodi d'ottone
  calz_militari  scarpone militare alto, infangato, con i lacci e il bordo del collo
  calz_cartone   scarpa di cuoio con la suola bucata: il cartone esce di lato e si vede nell'apertura
  calz_tacchi    scarpa da signora col tacco dritto (decolte' aperta con fiocchetto)
  calz_pezze     le pezze per i piedi: due rotoli di stoffa legati, una striscia sciolta e lo spago
Tutti i modelli sono IN PIEDI (come servono alla deduzione e alle forme del calzolaio): la posa 'abbandonata su un fianco' dei raccoglibili
sta nel JSON (campo rx di oggetti[] dell'enigma nascosto).
Stesse convenzioni di genera_enigmi_v2.py (di cui riusa le funzioni): lunghezza 1 su X (esportazione uniforme), punta verso +X, su = +Z, misure vere in metri
nel modello prima della normalizzazione; Unity le riporta a misura con MondoFab.Scarpa. Materiali: solo quelli gia' in Unity (ME_*, MT_Cuoio) piu' ME_Fango,
ME_Interno e ME_Cartone (senza texture: solo colore, vedi EnigmiModelli.cs).

Uso (headless):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ArtSource/Models/Enigmi/genera_calzature.py
"""
import bpy, bmesh, math, os, sys, json, random
from mathutils import Vector, Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import genera_enigmi_v2 as g

random.seed(1946)


# ============================================================================================ utilita'
def loft(nome, st, materiale, N=16, esp=2.2, uv=0.25):
    """Corpo ottenuto da sezioni lungo X: st = [(x, semilarghezza, z_fondo, z_cima)], sezione a superellisse (esp: 2 = ellisse, piu' alto = piu' squadrata)."""
    bm = bmesh.new()
    anelli = []
    for sez in st:
        x, hw, zb, zt = sez[:4]
        yo = sez[4] if len(sez) > 4 else 0.0
        zc, hh = (zb + zt) / 2, (zt - zb) / 2
        r = []
        for k in range(N):
            a = 2 * math.pi * k / N
            c, s = math.cos(a), math.sin(a)
            r.append(bm.verts.new((x, yo + hw * math.copysign(abs(c) ** (2 / esp), c), zc + hh * math.copysign(abs(s) ** (2 / esp), s))))
        anelli.append(r)
    for i in range(len(st) - 1):
        for k in range(N):
            bm.faces.new((anelli[i][k], anelli[i][(k + 1) % N], anelli[i + 1][(k + 1) % N], anelli[i + 1][k]))
    bm.faces.new(anelli[0][::-1]); bm.faces.new(anelli[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    g.uv_scatola(bm, uv)
    o = g.oggetto(nome, bm)
    o.data.materials.append(g.mat(materiale))
    return o


def livello(st, x, idx):
    """Valore interpolato (idx: 1 semilarghezza, 2 fondo, 3 cima) alla coordinata x."""
    for a, b in zip(st, st[1:]):
        if a[0] <= x <= b[0]:
            t = (x - a[0]) / max(1e-9, b[0] - a[0])
            return a[idx] * (1 - t) + b[idx] * t
    return st[-1][idx]


def ruota_x(o, gradi):
    o.rotation_euler = (math.radians(gradi), 0, 0)
    g.attiva(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def fango(n, area, quota, mat="ME_Fango", dim=(0.02, 0.03)):
    """Schizzi di fango sparsi: sfere schiacciate in un'area (xmin, xmax, semilarghezza)."""
    P = []
    for i in range(n):
        x = random.uniform(area[0], area[1]); lato = random.choice((-1, 1))
        s = random.uniform(*dim)
        P.append(g.pezzo("fango%d" % i, (s * 1.3, s * 0.8, s * 0.7), (x, lato * area[2] * random.uniform(0.92, 1.0), quota(x) + random.uniform(-0.01, 0.015)), mat, 0.1, forma="sfera", vert=8))
    return P


# ============================================================================================ le cinque calzature
def zoccoli():
    g.pulisci()
    st = [(-0.135, 0.020, 0.004, 0.070), (-0.120, 0.038, 0.0, 0.088), (-0.080, 0.046, 0.0, 0.092), (-0.020, 0.044, 0.0, 0.088),
          (0.030, 0.047, 0.008, 0.074), (0.080, 0.046, 0.016, 0.058), (0.120, 0.034, 0.026, 0.056), (0.135, 0.016, 0.034, 0.050)]
    P = [loft("blocco", st, "ME_LegnoNoce", N=18, esp=2.4, uv=0.3),
         g.pezzo("apertura", (0.15, 0.062, 0.004), (-0.065, 0, 0.091), "ME_Interno", 0.1, forma="cilindro", vert=16),
         g.pezzo("fascia", (0.034, 0.098, 0.014), (0.045, 0, 0.074), "ME_CuoioCinghia", 0.2, smusso=0.003),
         g.pezzo("fasciaL", (0.034, 0.012, 0.05), (0.045, 0.050, 0.052), "ME_CuoioCinghia", 0.2, smusso=0.002),
         g.pezzo("fasciaR", (0.034, 0.012, 0.05), (0.045, -0.050, 0.052), "ME_CuoioCinghia", 0.2, smusso=0.002),
         g.pezzo("chiodo1", (0.012, 0.010, 0.010), (0.045, 0.058, 0.060), "ME_Ottone", 0.05, forma="sfera", vert=8),
         g.pezzo("chiodo2", (0.012, 0.010, 0.010), (0.045, -0.058, 0.060), "ME_Ottone", 0.05, forma="sfera", vert=8)]
    o = g.unisci(P, "calz_zoccoli"); g.spigoli(o, 50)
    g.esporta([o], "calz_zoccoli", uniforme=True)


def militari():
    g.pulisci()
    sup = [(-0.145, 0.030, 0.028, 0.150), (-0.125, 0.045, 0.028, 0.170), (-0.080, 0.046, 0.028, 0.180), (-0.030, 0.044, 0.028, 0.175),
           (0.000, 0.047, 0.028, 0.120), (0.040, 0.050, 0.028, 0.085), (0.090, 0.049, 0.028, 0.068), (0.130, 0.035, 0.028, 0.052), (0.148, 0.014, 0.032, 0.042)]
    sola = [(-0.150, 0.030, 0.0, 0.030), (-0.130, 0.050, 0.0, 0.030), (0.0, 0.054, 0.0, 0.030), (0.100, 0.054, 0.0, 0.030), (0.150, 0.020, 0.004, 0.032)]
    P = [loft("tomaia", sup, "MT_Cuoio", N=16, esp=2.2, uv=0.2),
         loft("suola", sola, "ME_Suola", N=14, esp=3.5, uv=0.2),
         g.pezzo("tacco", (0.07, 0.10, 0.02), (-0.108, 0, 0.012), "ME_Suola", 0.2, smusso=0.004),
         g.pezzo("collo", (0.10, 0.092, 0.012), (-0.085, 0, 0.178), "MT_Cuoio", 0.2, forma="cilindro", vert=16),
         g.pezzo("bocca", (0.080, 0.066, 0.004), (-0.085, 0, 0.186), "ME_Interno", 0.1, forma="cilindro", vert=16),
         g.pezzo("passante", (0.012, 0.03, 0.05), (-0.150, 0, 0.12), "ME_Lacci", 0.1, smusso=0.002)]
    # lacci sulla linguetta, seguono il pendio del collo del piede
    for i, x in enumerate((-0.018, 0.0, 0.016, 0.032, 0.048)):
        z = livello(sup, x, 3) + 0.002
        pend = math.degrees(math.atan2(livello(sup, x + 0.01, 3) - livello(sup, x - 0.01, 3), 0.02))
        P.append(g.pezzo("laccio%d" % i, (0.007, 0.085, 0.005), (x, 0, z), "ME_Lacci", 0.1, rot=(0, -pend, 0)))
    P += fango(11, (-0.13, 0.14, 0.047), lambda x: 0.045, dim=(0.016, 0.034))
    P += fango(3, (0.02, 0.14, 0.0), lambda x: 0.07, dim=(0.014, 0.024))
    o = g.unisci(P, "calz_militari"); g.spigoli(o, 50)
    g.esporta([o], "calz_militari", uniforme=True)


def cartone():
    g.pulisci()
    sup = [(-0.130, 0.030, 0.020, 0.075), (-0.110, 0.042, 0.020, 0.085), (-0.060, 0.044, 0.020, 0.085), (-0.010, 0.044, 0.020, 0.080),
           (0.040, 0.048, 0.020, 0.066), (0.090, 0.046, 0.020, 0.056), (0.125, 0.032, 0.020, 0.044), (0.140, 0.014, 0.020, 0.036)]
    sola = [(-0.135, 0.030, 0.0, 0.022), (-0.115, 0.047, 0.0, 0.022), (0.0, 0.050, 0.0, 0.022), (0.100, 0.052, 0.0, 0.022), (0.145, 0.018, 0.0, 0.022)]
    P = [loft("tomaia", sup, "MT_Cuoio", N=16, esp=2.2, uv=0.2),
         loft("suola", sola, "ME_Suola", N=14, esp=3.5, uv=0.2),
         g.pezzo("tacco", (0.06, 0.092, 0.016), (-0.105, 0, -0.004), "ME_Suola", 0.2, smusso=0.003),
         g.pezzo("bocca", (0.10, 0.060, 0.004), (-0.055, 0, 0.0855), "ME_Interno", 0.1, forma="cilindro", vert=16),
         # il cartone infilato nella scarpa: spunta dall'apertura e dalla suola bucata, lembi che escono ai lati e sotto la punta
         g.pezzo("cartone_dentro", (0.085, 0.052, 0.004), (-0.050, 0, 0.0885), "ME_Cartone", 0.1, forma="cilindro", vert=14, rot=(0, -8, 0)),
         g.pezzo("lembo_lato", (0.075, 0.05, 0.005), (-0.030, 0.062, 0.030), "ME_Cartone", 0.1, rot=(0, 0, -62)),
         g.pezzo("lembo_punta", (0.060, 0.050, 0.005), (0.150, 0.0, 0.012), "ME_Cartone", 0.1, rot=(0, -22, 0)),
         g.pezzo("lembo_tallone", (0.050, 0.045, 0.005), (-0.150, -0.020, 0.012), "ME_Cartone", 0.1, rot=(0, 25, 12))]
    for i, x in enumerate((-0.012, 0.012, 0.036)):
        P.append(g.pezzo("laccio%d" % i, (0.006, 0.075, 0.004), (x, 0, livello(sup, x, 3) + 0.002), "ME_Lacci", 0.1))
    P += fango(4, (0.04, 0.13, 0.045), lambda x: 0.03, dim=(0.012, 0.02))
    o = g.unisci(P, "calz_cartone"); g.spigoli(o, 50)
    g.esporta([o], "calz_cartone", uniforme=True)


def tacchi():
    g.pulisci()
    cal = [(-0.118, 0.018, 0.078, 0.128), (-0.104, 0.032, 0.074, 0.134), (-0.082, 0.036, 0.070, 0.118)]          # contrafforte del tallone
    van = [(0.036, 0.040, 0.026, 0.066), (0.075, 0.038, 0.014, 0.056), (0.110, 0.026, 0.008, 0.040), (0.124, 0.010, 0.006, 0.030)]    # mascherina sulla punta
    sola = [(-0.118, 0.018, 0.070, 0.078), (-0.100, 0.030, 0.066, 0.074), (-0.040, 0.034, 0.050, 0.058), (0.030, 0.038, 0.020, 0.028), (0.090, 0.034, 0.006, 0.014), (0.126, 0.010, 0.002, 0.010)]
    P = [loft("calcagno", cal, "MT_Cuoio", N=14, esp=2.1, uv=0.2),
         loft("mascherina", van, "MT_Cuoio", N=14, esp=2.1, uv=0.2),
         loft("soletta", sola, "ME_Suola", N=12, esp=3.0, uv=0.2),
         g.pezzo("tacco", (0.028, 0.028, 0.072), (-0.108, 0, 0.036), "ME_Suola", 0.1, forma="cilindro", vert=12),
         g.pezzo("punta_tacco", (0.020, 0.020, 0.008), (-0.108, 0, 0.004), "ME_Interno", 0.1, forma="cilindro", vert=10),
         g.pezzo("nodo", (0.014, 0.020, 0.012), (0.040, 0, 0.066), "MT_Cuoio", 0.1, forma="sfera", vert=8),
         g.pezzo("fiocco1", (0.030, 0.030, 0.012), (0.040, 0.020, 0.064), "MT_Cuoio", 0.1, forma="sfera", vert=8, rot=(0, 0, 25)),
         g.pezzo("fiocco2", (0.030, 0.030, 0.012), (0.040, -0.020, 0.064), "MT_Cuoio", 0.1, forma="sfera", vert=8, rot=(0, 0, -25)),
         g.pezzo("soletta_chiara", (0.15, 0.05, 0.003), (-0.030, 0, 0.0545), "ME_CuoioCinghia", 0.2, forma="cilindro", vert=16, rot=(0, -10, 0))]
    # fianchi bassi della decolte' aperta: due listelli sottili che seguono l'arcata tra tallone e mascherina
    zb = lambda x: 0.070 - 0.044 * (x + 0.084) / 0.124
    for lato in (1, -1):
        st = [(x, 0.004, zb(x), zb(x) + 0.030, lato * 0.033) for x in (-0.084, -0.060, -0.030, 0.0, 0.030, 0.040)]
        P.append(loft("fianco%d" % lato, st, "MT_Cuoio", N=8, esp=2.0, uv=0.2))
    o = g.unisci(P, "calz_tacchi"); g.spigoli(o, 50)
    g.esporta([o], "calz_tacchi", uniforme=True)


def striscia(nome, lung, larg, pos, rot_z, mat, onda=0.01):
    """Striscia di stoffa sciolta, a terra, con un'onda lungo la lunghezza."""
    bm = bmesh.new()
    nx, ny = 16, 2
    vv = []
    for j in range(ny + 1):
        r = []
        for i in range(nx + 1):
            s = i / nx - 0.5
            r.append(bm.verts.new((s * lung, (j / ny - 0.5) * larg + 0.01 * math.sin(i * 0.9), 0.004 + onda * (0.5 + 0.5 * math.sin(i * 1.3 + j)) * (1 if (i % 4) else 0.5))))
        vv.append(r)
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((vv[j][i], vv[j][i + 1], vv[j + 1][i + 1], vv[j + 1][i]))
    bmesh.ops.solidify(bm, geom=bm.faces[:] + bm.edges[:] + bm.verts[:], thickness=0.003)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    R = Matrix.Rotation(math.radians(rot_z), 4, "Z")
    bmesh.ops.transform(bm, matrix=R, verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(pos), verts=bm.verts)
    bm.normal_update(); g.uv_scatola(bm, 0.12)
    o = g.oggetto(nome, bm); o.data.materials.append(g.mat(mat))
    return o


def pezze():
    """Fagotto di stoffa avvolta con il legaccio e due code: si legge come un fagotto di pezze."""
    g.pulisci()
    P = []
    for k, (cx, cy, rz, sc, qt) in enumerate(((0.0, 0.0, 0, 1.0, 0.0), (0.02, 0.025, 25, 0.78, 0.030), (-0.025, -0.02, -30, 0.7, 0.026))):
        st = [(-0.075 * sc, 0.020 * sc, qt, qt + 0.040 * sc), (-0.050 * sc, 0.050 * sc, qt, qt + 0.085 * sc), (0.0, 0.062 * sc, qt, qt + 0.100 * sc),
              (0.050 * sc, 0.052 * sc, qt, qt + 0.085 * sc), (0.075 * sc, 0.020 * sc, qt, qt + 0.040 * sc)]
        lump = loft("massa%d" % k, st, "MT_Panno", N=14, esp=2.0, uv=0.12)
        Rz = Matrix.Rotation(math.radians(rz), 3, "Z")
        for v in lump.data.vertices:
            v.co = Rz @ v.co + Vector((cx, cy, 0))
        P.append(lump)
    P.append(loft("legaccio", [(0.018, 0.0645, 0.0, 0.1035), (0.026, 0.0645, 0.0, 0.1035)], "ME_Lacci", N=16, esp=2.0, uv=0.1))
    P.append(g.pezzo("nodo", (0.022, 0.022, 0.018), (0.022, -0.036, 0.098), "ME_Lacci", 0.1, forma="sfera", vert=8))
    P.append(striscia("coda1", 0.16, 0.04, (0.095, -0.075, 0.0), -50, "MT_Panno", onda=0.010))
    P.append(striscia("coda2", 0.13, 0.036, (0.07, 0.085, 0.0), 40, "MT_Panno", onda=0.008))
    P += fango(3, (-0.06, 0.06, 0.05), lambda x: 0.03, dim=(0.012, 0.02))
    o = g.unisci(P, "calz_pezze"); g.spigoli(o, 50)
    g.esporta([o], "calz_pezze", uniforme=True)


TUTTI = {"zoccoli": zoccoli, "militari": militari, "cartone": cartone, "tacchi": tacchi, "pezze": pezze}

if __name__ == "__main__":
    for k in (g.argv or TUTTI.keys()):
        TUTTI[k]()
    json.dump(g.misure, open(g.MISURE, "w"), indent=1, sort_keys=True)
