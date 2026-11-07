# R007 tappa 2: oggetti originali d'epoca (Napoli 1946) modellati da zero.
# Gabbia per galline, lavatoio in pietra, bacinella smaltata, casse di frutta, radio a valvole, panni stesi.
# Uso: blender -b --factory-startup --python crea_oggetti.py -- [Nome ...]   (senza nomi: tutti)
# Le texture le prepara texture_procedurali.py (PIL), poi Treno > R007 > Importa modelli in Unity.
import bpy, bmesh, sys, os, math, random
from mathutils import Matrix
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lib_modelli as L
import panni_def as PD
import crea_oggetti_b as B
from lib_modelli import box, cilindro, trasla, uv_box, smussa, nuovo_oggetto

FERRO = {'colore': [0.10, 0.10, 0.11], 'metallico': 0.85, 'levig': 0.35}


def crea_mats(nomi):
    return [bpy.data.materials.new(n) for n in nomi]


def oggetto_da_bm(nome, bm, mats):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(nome)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    o = bpy.data.objects.new(nome, me)
    bpy.context.collection.objects.link(o)
    return o


def prisma(bm, perfil, x0, x1, mat=0):
    """Prisma con profilo (y,z) estruso lungo x."""
    a = [bm.verts.new((x0, y, z)) for y, z in perfil]
    b = [bm.verts.new((x1, y, z)) for y, z in perfil]
    fs = [bm.faces.new(a), bm.faces.new(b[::-1])]
    n = len(perfil)
    for i in range(n):
        fs.append(bm.faces.new((a[i], a[(i + 1) % n], b[(i + 1) % n], b[i])))
    for f in fs:
        f.material_index = mat


# ------------------------------------------------------------------ gabbia per galline

def gabbia():
    mats = crea_mats(['LegnoGabbia', 'FerroGabbia'])
    bm = bmesh.new()
    W, D, H0, H1 = 0.80, 0.55, 0.20, 0.78
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * (W / 2 - 0.025), sy * (D / 2 - 0.025)
            box(bm, (cx - 0.0225, cy - 0.0225, 0), (cx + 0.0225, cy + 0.0225, H0 + 0.03))       # gamba
            box(bm, (cx - 0.0175, cy - 0.0175, H0 + 0.03), (cx + 0.0175, cy + 0.0175, H1))       # montante
    box(bm, (-W / 2, -D / 2, H0), (W / 2, D / 2, H0 + 0.03))                                         # fondo
    box(bm, (-W / 2 - 0.03, -D / 2 - 0.03, H1), (W / 2 + 0.03, D / 2 + 0.03, H1 + 0.03))           # tetto
    box(bm, (-W / 2 - 0.01, -D / 2 - 0.01, H1 + 0.03), (W / 2 + 0.01, D / 2 + 0.01, H1 + 0.045))   # secondo asse, tetto piu' spesso
    # traverse in basso e in alto, sui quattro lati
    for z0 in (H0 + 0.03, H1 - 0.045):
        for sy in (-1, 1):
            box(bm, (-W / 2 + 0.05, sy * (D / 2 - 0.012) - 0.01, z0), (W / 2 - 0.05, sy * (D / 2 - 0.012) + 0.01, z0 + 0.04))
        for sx in (-1, 1):
            box(bm, (sx * (W / 2 - 0.012) - 0.01, -D / 2 + 0.05, z0), (sx * (W / 2 - 0.012) + 0.01, D / 2 - 0.05, z0 + 0.04))
    zs, ze = H0 + 0.07, H1 - 0.045
    # listelli verticali: dietro, ai lati, davanti con apertura per la porticina
    def listelli(a, b, pos, asse):
        x = a + 0.03
        while x < b - 0.02:
            if asse == 'x':
                box(bm, (x, pos - 0.006, zs), (x + 0.018, pos + 0.006, ze))
            else:
                box(bm, (pos - 0.006, x, zs), (pos + 0.006, x + 0.018, ze))
            x += 0.062
    listelli(-W / 2 + 0.04, W / 2 - 0.04, -D / 2 + 0.012, 'x')
    listelli(-D / 2 + 0.04, D / 2 - 0.04, -W / 2 + 0.012, 'y')
    listelli(-D / 2 + 0.04, D / 2 - 0.04, W / 2 - 0.012, 'y')
    listelli(-W / 2 + 0.04, -0.17, D / 2 - 0.012, 'x')
    listelli(0.17, W / 2 - 0.04, D / 2 - 0.012, 'x')
    # porticina davanti: telaio, due listelli, traversa a croce, cerniere e chiavistello di ferro
    py = D / 2 - 0.004
    box(bm, (-0.165, py - 0.01, zs - 0.01), (-0.145, py + 0.01, ze + 0.01))
    box(bm, (0.145, py - 0.01, zs - 0.01), (0.165, py + 0.01, ze + 0.01))
    box(bm, (-0.165, py - 0.01, zs - 0.01), (0.165, py + 0.01, zs + 0.025))
    box(bm, (-0.165, py - 0.01, ze - 0.025), (0.165, py + 0.01, ze + 0.01))
    for x in (-0.09, -0.03, 0.03, 0.09):
        box(bm, (x - 0.009, py - 0.006, zs + 0.02), (x + 0.009, py + 0.006, ze - 0.02))
    for z in (zs + 0.06, ze - 0.09):
        box(bm, (-0.165, py + 0.01, z), (-0.07, py + 0.018, z + 0.018), 1)     # cerniera
    box(bm, (0.04, py + 0.01, (zs + ze) / 2 - 0.008), (0.19, py + 0.02, (zs + ze) / 2 + 0.008), 1)  # chiavistello
    box(bm, (0.15, py + 0.01, (zs + ze) / 2 - 0.02), (0.2, py + 0.02, (zs + ze) / 2 + 0.02), 1)
    # posatoio all'interno
    v, _ = cilindro(bm, -W / 2 + 0.05, W / 2 - 0.05, 0.012, 8, 0, asse='x')
    trasla(bm, v, (0, 0, H0 + 0.2))
    o = oggetto_da_bm('Gabbia', bm, mats)
    o.data.transform(Matrix.Rotation(math.pi, 4, 'Z'))   # fronte (porticina) a -Y Blender = +Z Unity, come tutti gli altri modelli
    uv_box(o.data, 0.8)
    return o, {'piatti': {'FerroGabbia': FERRO}}


# ------------------------------------------------------------------ lavatoio

def lavatoio():
    mats = crea_mats(['PietraLavatoio', 'FerroTubo'])
    bm = bmesh.new()
    Lg, Wd, H = 2.6, 0.8, 0.85
    box(bm, (-Lg / 2, -Wd / 2, 0), (Lg / 2, Wd / 2, 0.40))                                   # base piena fino al fondo vasca
    box(bm, (-Lg / 2, Wd / 2 - 0.14, 0.40), (Lg / 2, Wd / 2, H))                              # muro dietro
    for sx in (-1, 1):
        a, b = (sx * Lg / 2 - 0.0, sx * (Lg / 2 - 0.17))
        box(bm, (min(a, b), -Wd / 2, 0.40), (max(a, b), Wd / 2, H))                           # testate
    box(bm, (-Lg / 2, -Wd / 2, 0.40), (Lg / 2, -Wd / 2 + 0.14, 0.62))                        # muretto davanti
    prisma(bm, [(-Wd / 2 + 0.14, 0.40), (-Wd / 2 + 0.14, 0.62), (-0.02, 0.42), (-0.02, 0.40)], -Lg / 2 + 0.17, Lg / 2 - 0.17)  # piano inclinato
    box(bm, (-Lg / 2 - 0.03, Wd / 2 - 0.17, H), (Lg / 2 + 0.03, Wd / 2 + 0.03, H + 0.05))   # coperta del muro
    box(bm, (-Lg / 2 - 0.03, -Wd / 2 - 0.03, H), (-Lg / 2 + 0.19, Wd / 2 + 0.03, H + 0.05))   # coperte delle testate
    box(bm, (Lg / 2 - 0.19, -Wd / 2 - 0.03, H), (Lg / 2 + 0.03, Wd / 2 + 0.03, H + 0.05))
    # tubo di ferro: colonna lungo il muro dietro e braccio che entra nella vasca
    px = -Lg / 2 + 0.55
    v, _ = cilindro(bm, 0.15, 0.74, 0.024, 10, 1)
    trasla(bm, v, (px, Wd / 2 + 0.045, 0))
    v, _ = cilindro(bm, Wd / 2 - 0.22, Wd / 2 + 0.05, 0.022, 10, 1, asse='y')
    trasla(bm, v, (px, 0, 0.72))
    v, _ = cilindro(bm, 0.66, 0.74, 0.027, 10, 1, asse='z')
    trasla(bm, v, (px, Wd / 2 - 0.22, 0))
    o = oggetto_da_bm('Lavatoio', bm, mats)
    smussa(o, 0.012, 1)
    uv_box(o.data, 1.0)
    return o, {'piatti': {'FerroTubo': FERRO}}


# ------------------------------------------------------------------ bacinella smaltata

def bacinella():
    mats = crea_mats(['SmaltoBianco', 'BordoSmalto'])
    bm = bmesh.new()
    prof = [(0.0, 0.0), (0.11, 0.0), (0.165, 0.012), (0.2, 0.055), (0.215, 0.10), (0.222, 0.108), (0.214, 0.114), (0.205, 0.105),
            (0.19, 0.055), (0.155, 0.026), (0.0, 0.020)]
    vs = [bm.verts.new((r, 0, z)) for r, z in prof]
    es = [bm.edges.new((vs[i], vs[i + 1])) for i in range(len(vs) - 1)]
    bmesh.ops.spin(bm, geom=vs + es, cent=(0, 0, 0), axis=(0, 0, 1), angle=2 * math.pi, steps=24, use_merge=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    for f in bm.faces:
        f.material_index = 1 if min(v.co.z for v in f.verts) >= 0.095 else 0
    o = oggetto_da_bm('Bacinella', bm, mats)
    me = o.data
    b2 = bmesh.new()
    b2.from_mesh(me)
    uvl = b2.loops.layers.uv.verify()
    for f in b2.faces:
        for l in f.loops:
            l[uvl].uv = (l.vert.co.x / 0.46 + 0.5, l.vert.co.y / 0.46 + 0.5)
    for f in b2.faces:
        f.smooth = True
    b2.to_mesh(me)
    b2.free()
    return o, {'piatti': {'BordoSmalto': {'colore': [0.07, 0.09, 0.13], 'metallico': 0.3, 'levig': 0.55}}}


# ------------------------------------------------------------------ casse di frutta

def cassa_frutta(tipo):
    mats = crea_mats(['LegnoCassa', 'Frutta_' + tipo])
    bm = bmesh.new()
    Lc, Dc, Hc = 0.52, 0.36, 0.20
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * (Lc / 2 - 0.0175), sy * (Dc / 2 - 0.0175)
            box(bm, (cx - 0.0175, cy - 0.0175, 0), (cx + 0.0175, cy + 0.0175, Hc))
    for k in range(3):                                                                         # fondo a tavole
        y0 = -Dc / 2 + k * Dc / 3
        box(bm, (-Lc / 2 + 0.035, y0 + 0.004, 0), (Lc / 2 - 0.035, y0 + Dc / 3 - 0.004, 0.015))
    for zz in (0.035, 0.115):                                                                  # listelli dei lati
        for sy in (-1, 1):
            box(bm, (-Lc / 2 + 0.035, (Dc / 2 - 0.012) * sy - 0.006, zz), (Lc / 2 - 0.035, (Dc / 2 - 0.012) * sy + 0.006, zz + 0.07))
        for sx in (-1, 1):
            box(bm, ((Lc / 2 - 0.012) * sx - 0.006, -Dc / 2 + 0.035, zz), ((Lc / 2 - 0.012) * sx + 0.006, Dc / 2 - 0.035, zz + 0.07))
    rnd = random.Random(7 + len(tipo))
    ri = 0.04 if tipo == 'Arance' else (0.032 if tipo == 'Pomodori' else 0.055)
    nx = int((Lc - 0.08) // (ri * 2)); ny = int((Dc - 0.08) // (ri * 2))
    for ix in range(nx):
        for iy in range(ny):
            x = -(nx - 1) * ri + ix * 2 * ri + rnd.uniform(-0.004, 0.004)
            y = -(ny - 1) * ri + iy * 2 * ri + rnd.uniform(-0.004, 0.004)
            for strato in range(2 if tipo != 'Verdura' else 1):
                z = 0.12 + ri * 0.6 + strato * ri * 1.5 + rnd.uniform(0, 0.01)
                if strato == 1 and (ix + iy) % 2:
                    continue
                r = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=ri * rnd.uniform(0.93, 1.05))
                vv = r['verts']
                sch = 0.8 if tipo == 'Verdura' else 0.92
                for v in vv:
                    v.co.z *= sch
                    if tipo == 'Verdura':
                        v.co += v.co.normalized() * rnd.uniform(0, 0.012)
                trasla(bm, vv, (x, y, z))
                for f in {f for v in vv for f in v.link_faces}:
                    f.material_index = 1
    o = oggetto_da_bm('CassaFrutta_' + tipo, bm, mats)
    uv_box(o.data, 0.5, {1: 0.12})
    for p in o.data.polygons:
        if p.material_index == 1:
            p.use_smooth = True
    return o, {}


# ------------------------------------------------------------------ radio a valvole

def radio():
    mats = crea_mats(['LegnoRadio', 'TelaRadio', 'Bachelite', 'ScalaRadio', 'OttoneRadio'])
    W, D, H = 0.42, 0.22, 0.27
    bm1 = bmesh.new()
    box(bm1, (-W / 2, -D / 2, 0.012), (W / 2, D / 2, H))
    cab = oggetto_da_bm('RadioCassa', bm1, mats)
    smussa(cab, 0.022, 3)
    uv_box(cab.data, 0.5)
    bm = bmesh.new()
    fy = -D / 2
    # griglia con tela: cornice in legno rialzata + tela leggermente incassata
    gx0, gx1, gz0, gz1 = -0.185, 0.045, 0.045, 0.235
    for (a, b, c, d) in [(gx0 - 0.014, gx1 + 0.014, gz1, gz1 + 0.014), (gx0 - 0.014, gx1 + 0.014, gz0 - 0.014, gz0),
                         (gx0 - 0.014, gx0, gz0, gz1), (gx1, gx1 + 0.014, gz0, gz1)]:
        box(bm, (a, fy - 0.007, c), (b, fy + 0.01, d), 0)
    box(bm, (gx0, fy - 0.001, gz0), (gx1, fy + 0.01, gz1), 1)
    # finestra della scala parlante, con cornice di ottone
    sx0, sx1, sz0, sz1 = 0.075, 0.19, 0.12, 0.235
    box(bm, (sx0, fy - 0.002, sz0), (sx1, fy + 0.01, sz1), 3)
    for (a, b, c, d) in [(sx0 - 0.008, sx1 + 0.008, sz1, sz1 + 0.008), (sx0 - 0.008, sx1 + 0.008, sz0 - 0.008, sz0),
                         (sx0 - 0.008, sx0, sz0, sz1), (sx1, sx1 + 0.008, sz0, sz1)]:
        box(bm, (a, fy - 0.006, c), (b, fy + 0.01, d), 4)
    # manopole di bachelite
    for kx in (0.105, 0.16):
        v, _ = cilindro(bm, fy - 0.034, fy, 0.019, 14, 2, asse='y', raggio2=0.017)
        trasla(bm, v, (kx, 0, 0.068))
        v, _ = cilindro(bm, fy - 0.004, fy + 0.002, 0.026, 14, 4, asse='y')
        trasla(bm, v, (kx, 0, 0.068))
    # piedini
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (sx * (W / 2 - 0.04) - 0.02, sy * (D / 2 - 0.035) - 0.02, 0), (sx * (W / 2 - 0.04) + 0.02, sy * (D / 2 - 0.035) + 0.02, 0.014), 2)
    # dorso con feritoie di aerazione
    for k in range(7):
        box(bm, (-W / 2 + 0.05 + k * 0.05, D / 2 - 0.002, 0.05), (-W / 2 + 0.065 + k * 0.05, D / 2 + 0.004, H - 0.05), 2)
    acc = oggetto_da_bm('RadioAccessori', bm, mats)
    uv_box(acc.data, 0.5, {1: 0.25, 3: 0.12})
    # la scala parlante usa l'UV 0..1 sul suo rettangolo
    b3 = bmesh.new(); b3.from_mesh(acc.data); uvl = b3.loops.layers.uv.verify()
    for f in b3.faces:
        if f.material_index == 3:
            for l in f.loops:
                l[uvl].uv = ((l.vert.co.x - sx0) / (sx1 - sx0), (l.vert.co.z - sz0) / (sz1 - sz0))
    b3.to_mesh(acc.data); b3.free()
    o = L.unisci([cab, acc])
    o.name = 'Radio'
    return o, {'piatti': {'Bachelite': {'colore': [0.07, 0.04, 0.03], 'metallico': 0.0, 'levig': 0.7},
                          'OttoneRadio': {'colore': [0.62, 0.46, 0.2], 'metallico': 0.9, 'levig': 0.6}}}


# ------------------------------------------------------------------ panni stesi

def panno(nome, mult):
    x0, y0, cw, ch, poly, col, stile = PD.PANNI[nome]
    w, h = PD.dim_m(nome)
    amp, freq, drift, res = PD.VENTO[nome]
    res *= mult
    nx = max(2, round(w / res)); nz = max(2, round(h / res))
    ph = (sum(ord(c) for c in nome) % 7) * 0.9
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.verify()
    g = {}
    for j in range(nz + 1):
        t = j / nz
        for i in range(nx + 1):
            u = i / nx
            x = (u - 0.5) * w + drift * t ** 1.6
            z = -h * t + 0.012 * math.sin(u * 7 + ph) * t
            y = amp * (0.12 + 0.88 * t) * math.sin(u * math.pi * freq * 2 + ph + t * 1.3) + 0.04 * math.sin(u * math.pi) * t + abs(drift) * 0.5 * t ** 1.5
            g[(i, j)] = bm.verts.new((x, y, z))

    def uv(i, j):
        fx, fy = i / nx, j / nz
        return ((x0 + fx * cw) / PD.ATLANTE[0], 1.0 - (y0 + fy * ch) / PD.ATLANTE[1])

    for j in range(nz):
        for i in range(nx):
            cs = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
            if not any(PD.dentro(poly, a / nx, b / nz) for a, b in cs) and not PD.dentro(poly, (i + 0.5) / nx, (j + 0.5) / nz):
                continue
            f = bm.faces.new([g[c] for c in cs])
            for l, c in zip(f.loops, cs):
                l[uvl].uv = uv(*c)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        f.smooth = True
    me = bpy.data.meshes.new(nome)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(nome, me)
    bpy.context.collection.objects.link(o)
    return o


def crea_panni():
    for nome in PD.PANNI:
        oggetti = []
        for lod, mult in enumerate((1.0, 2.0, 4.0)):
            o = panno(nome, mult)
            o.name = 'Panno_%s_LOD%d' % (nome, lod)
            o.data.name = o.name
            oggetti.append(o)
        for m in list(bpy.data.materials):
            bpy.data.materials.remove(m)
        mat = bpy.data.materials.new('PanniTessuto')   # materiale vuoto condiviso: il nome deve restare esatto (remap in Unity)
        for o in oggetti:
            o.data.materials.append(mat)
        mn, mx = L.dimensioni(oggetti[0])
        L.esporta_fbx(oggetti, os.path.join(L.USCITA_MODELLI, 'Panni', 'Panno_%s.fbx' % nome))
        L.scrivi_meta('Panno_' + nome, {'nome': 'Panno_' + nome, 'cartella': 'Panni', 'sorgente': '', 'materiali': ['PanniTessuto'],
                                        'tri': [L.n_tri(o) for o in oggetti], 'dim': [round(mx.x - mn.x, 3), round(mx.y - mn.y, 3), round(mx.z - mn.z, 3)],
                                        'doppia_faccia': ['PanniTessuto'], 'alpha_clip': ['PanniTessuto'], 'pivot': 'bordo superiore'})
        print('FINALIZZATO Panno_' + nome, [L.n_tri(o) for o in oggetti])
        for o in oggetti:
            bpy.data.objects.remove(o, do_unlink=True)
    L.scrivi_meta('Panni', {'nome': 'Panni', 'cartella': 'Panni', 'sorgente': '', 'materiali': ['PanniTessuto'], 'tri': [], 'dim': [],
                            'doppia_faccia': ['PanniTessuto'], 'alpha_clip': ['PanniTessuto']})


COSTRUTTORI = {
    'Bancarella': (B.bancarella, dict()),
    'PanchinaPopolare': (B.panchina, dict()),
    'SediaPaglia': (B.sedia, dict()),
    'GabbiaGalline': (gabbia, dict()),
    'Lavatoio': (lavatoio, dict()),
    'Bacinella': (bacinella, dict()),
    'CassaFrutta_Arance': (lambda: cassa_frutta('Arance'), dict()),
    'CassaFrutta_Pomodori': (lambda: cassa_frutta('Pomodori'), dict()),
    'CassaFrutta_Verdura': (lambda: cassa_frutta('Verdura'), dict()),
    'Radio': (radio, dict()),
}


def lavora(nome):
    L.azzera_scena()
    if nome == 'Panni':
        crea_panni()
        return
    fn, kw = COSTRUTTORI[nome]
    obj, extra = fn()
    bpy.context.view_layer.objects.active = obj
    cartella = 'CassaFrutta' if nome.startswith('CassaFrutta_') else nome
    L.finalizza(nome, obj, lod=True, sorgente='', extra=extra, cartella=cartella, lisci=False)


if __name__ == '__main__':
    nomi = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for n in (nomi or list(COSTRUTTORI) + ['Panni']):
        lavora(n)
