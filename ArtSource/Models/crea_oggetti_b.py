# R007 tappa 2 parte B: oggetti rifatti dopo i confronti con l'utente (bancarella da mercato, panchina popolare, sedia impagliata napoletana).
# Importato da crea_oggetti.py. Fronte dei modelli a -Y Blender (= +Z Unity).
import bpy, bmesh, math, random
from mathutils import Matrix
import lib_modelli as L
from lib_modelli import box, cilindro, trasla, uv_box, smussa


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


def cassetta(bm, cx, cy, z0, w, d, h, mat=0):
    t = 0.014
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (cx + sx * (w / 2 - t) - t, cy + sy * (d / 2 - t) - t, z0), (cx + sx * (w / 2 - t) + t, cy + sy * (d / 2 - t) + t, z0 + h), mat)
    box(bm, (cx - w / 2 + 0.02, cy - d / 2 + 0.02, z0), (cx + w / 2 - 0.02, cy + d / 2 - 0.02, z0 + 0.012), mat)
    for zz in (z0 + 0.025, z0 + h * 0.55):
        for sy in (-1, 1):
            box(bm, (cx - w / 2 + 0.03, cy + sy * (d / 2 - 0.007) - 0.007, zz), (cx + w / 2 - 0.03, cy + sy * (d / 2 - 0.007) + 0.007, zz + h * 0.35), mat)
        for sx in (-1, 1):
            box(bm, (cx + sx * (w / 2 - 0.007) - 0.007, cy - d / 2 + 0.03, zz), (cx + sx * (w / 2 - 0.007) + 0.007, cy + d / 2 - 0.03, zz + h * 0.35), mat)


def bancarella():
    """Banco da mercato largo 2 m: telaio di pali, piano di assi, cassette, ripiano basso, tendone di tela con risvolto."""
    mats = crea_mats(['LegnoBanco', 'TeloBanco'])
    bm = bmesh.new()
    W, D = 2.0, 0.9
    hf, hb = 2.25, 1.95
    for sx in (-1, 1):
        box(bm, (sx * (W / 2 - 0.03) - 0.03, -D / 2 - 0.03, 0), (sx * (W / 2 - 0.03) + 0.03, -D / 2 + 0.03, hf))
        box(bm, (sx * (W / 2 - 0.03) - 0.03, D / 2 - 0.03, 0), (sx * (W / 2 - 0.03) + 0.03, D / 2 + 0.03, hb))
        box(bm, (sx * (W / 2 - 0.03) - 0.02, -D / 2, hb - 0.12), (sx * (W / 2 - 0.03) + 0.02, D / 2, hb - 0.06))
        box(bm, (sx * (W / 2 - 0.03) - 0.02, -D / 2, 0.25), (sx * (W / 2 - 0.03) + 0.02, D / 2, 0.30))
    for k in range(5):
        y0 = -D / 2 + 0.02 + k * (D - 0.04) / 5
        box(bm, (-W / 2 + 0.02, y0 + 0.004, 0.84), (W / 2 - 0.02, y0 + (D - 0.04) / 5 - 0.004, 0.89))
    box(bm, (-W / 2 + 0.03, -D / 2 - 0.02, 0.62), (W / 2 - 0.03, -D / 2 + 0.02, 0.84))
    box(bm, (-W / 2 + 0.05, -D / 2 + 0.08, 0.28), (W / 2 - 0.05, D / 2 - 0.08, 0.31))
    box(bm, (-W / 2, D / 2 - 0.02, 0.30), (W / 2, D / 2 + 0.01, 1.35))
    cassetta(bm, -0.62, -0.05, 0.89, 0.52, 0.36, 0.17)
    cassetta(bm, -0.05, 0.02, 0.89, 0.48, 0.34, 0.15)
    cassetta(bm, 0.58, -0.04, 0.89, 0.50, 0.36, 0.17)
    cassetta(bm, -0.3, 0.0, 0.31, 0.5, 0.36, 0.17)
    cassetta(bm, 0.45, 0.02, 0.31, 0.5, 0.36, 0.17)
    box(bm, (-W / 2 - 0.1, -D / 2 - 0.03, hf - 0.04), (W / 2 + 0.1, -D / 2 + 0.03, hf + 0.02))
    box(bm, (-W / 2 - 0.1, D / 2 - 0.03, hb - 0.04), (W / 2 + 0.1, D / 2 + 0.03, hb + 0.02))
    o = oggetto_da_bm('BancarellaLegno', bm, mats)
    uv_box(o.data, 0.8)
    bt = bmesh.new()
    uvl = bt.loops.layers.uv.verify()
    nx, ny = 14, 6
    g = {}
    for j in range(ny + 1):
        t = j / ny
        for i in range(nx + 1):
            u = i / nx
            x = (u - 0.5) * (W + 0.3)
            y = -D / 2 - 0.12 + t * (D + 0.24)
            z = (hf + 0.05) * (1 - t) + (hb + 0.05) * t - 0.07 * math.sin(math.pi * t) * (0.6 + 0.4 * math.sin(u * math.pi * 3))
            g[(i, j)] = bt.verts.new((x, y, z))
    for j in range(ny):
        for i in range(nx):
            f = bt.faces.new((g[(i, j)], g[(i + 1, j)], g[(i + 1, j + 1)], g[(i, j + 1)]))
            for l, c in zip(f.loops, [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]):
                l[uvl].uv = (c[0] / nx * 2.0, c[1] / ny)
            f.material_index = 1
    h0 = {}
    for j in range(3):
        for i in range(nx + 1):
            u = i / nx
            x = (u - 0.5) * (W + 0.3)
            h0[(i, j)] = bt.verts.new((x, -D / 2 - 0.12 - 0.02 * j, hf + 0.05 - 0.09 * j - 0.02 * math.sin(u * 20)))
    for j in range(2):
        for i in range(nx):
            f = bt.faces.new((h0[(i, j)], h0[(i + 1, j)], h0[(i + 1, j + 1)], h0[(i, j + 1)]))
            for l, c in zip(f.loops, [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]):
                l[uvl].uv = (c[0] / nx * 2.0, 0.9 + c[1] * 0.05)
            f.material_index = 1
    bmesh.ops.recalc_face_normals(bt, faces=bt.faces)
    me2 = bpy.data.meshes.new('Telo')
    bt.to_mesh(me2)
    bt.free()
    for m in mats:
        me2.materials.append(m)
    tel = bpy.data.objects.new('Telo', me2)
    bpy.context.collection.objects.link(tel)
    obj = L.unisci([o, tel])
    obj.name = 'Bancarella'
    return obj, {'doppia_faccia': ['TeloBanco']}


def panchina():
    """Panchina popolare: due ceppi di pietra grezza e due tavole di legno vissuto, niente schienale."""
    mats = crea_mats(['PietraPanchina', 'LegnoPanchina'])
    bm = bmesh.new()
    Wd, Dp = 1.8, 0.42
    for sx in (-1, 1):
        cx = sx * 0.68
        box(bm, (cx - 0.20, -Dp / 2 + 0.02, 0), (cx + 0.20, Dp / 2 - 0.02, 0.38), 0)
        box(bm, (cx - 0.17, -Dp / 2 + 0.05, 0.38), (cx + 0.17, Dp / 2 - 0.05, 0.40), 0)
    o1 = oggetto_da_bm('PanchinaPietra', bm, mats)
    smussa(o1, 0.025, 2)
    uv_box(o1.data, 1.0)
    b2 = bmesh.new()
    for k in range(2):
        y0 = -Dp / 2 + k * (Dp / 2)
        box(b2, (-Wd / 2, y0 + 0.006, 0.40), (Wd / 2, y0 + Dp / 2 - 0.006, 0.46), 1)
    o2 = oggetto_da_bm('PanchinaLegno', b2, mats)
    smussa(o2, 0.006, 1)
    uv_box(o2.data, 0.8)
    obj = L.unisci([o1, o2])
    obj.name = 'PanchinaPopolare'
    return obj, {}


def sedia():
    """Sedia impagliata napoletana: legno scuro, gambe anteriori tornite, spalliera a tre traverse, seduta di paglia bombata."""
    mats = crea_mats(['LegnoSedia', 'Paglia'])
    bm = bmesh.new()
    W, D, Hs, Ht = 0.42, 0.40, 0.45, 0.95
    fx, fy = W / 2 - 0.02, D / 2 - 0.02
    for sx in (-1, 1):
        v, _ = cilindro(bm, 0, Hs, 0.019, 8, 0, raggio2=0.014)
        trasla(bm, v, (sx * fx, -fy, 0))
        box(bm, (sx * fx - 0.018, fy - 0.018, 0), (sx * fx + 0.018, fy + 0.018, Ht))
    for z in (0.17, 0.30):
        for sy in (-1, 1):
            box(bm, (-fx, sy * fy - 0.009, z), (fx, sy * fy + 0.009, z + 0.022))
        for sx in (-1, 1):
            box(bm, (sx * fx - 0.009, -fy, z - 0.01), (sx * fx + 0.009, fy, z + 0.012))
    for sy in (-1, 1):
        box(bm, (-fx - 0.012, sy * fy - 0.02, Hs - 0.04), (fx + 0.012, sy * fy + 0.02, Hs))
    for sx in (-1, 1):
        box(bm, (sx * fx - 0.02, -fy, Hs - 0.04), (sx * fx + 0.02, fy, Hs))
    for k, z in enumerate((0.62, 0.74, 0.86)):
        box(bm, (-fx, fy - 0.012, z), (fx, fy + 0.012, z + (0.075 if k < 2 else 0.09)))
    o = oggetto_da_bm('SediaLegno', bm, mats)
    smussa(o, 0.004, 1)
    uv_box(o.data, 0.6)
    b2 = bmesh.new()
    r = bmesh.ops.create_cube(b2, size=1.0)
    for v in r['verts']:
        v.co.x *= (W - 0.03)
        v.co.y *= (D - 0.03)
        v.co.z = Hs - 0.035 + (v.co.z + 0.5) * 0.045
    r2 = bmesh.ops.create_cube(b2, size=1.0)
    for v in r2['verts']:
        v.co.x *= (W - 0.09)
        v.co.y *= (D - 0.09)
        v.co.z = Hs + (v.co.z + 0.5) * 0.018
    for f in b2.faces:
        f.material_index = 1
    o2 = oggetto_da_bm('SediaPaglia', b2, mats)
    uv_box(o2.data, 0.25)
    obj = L.unisci([o, o2])
    obj.name = 'SediaPaglia'
    return obj, {}


def gabbia_uccellino():
    """Gabbietta da appendere (R007 t3B): fili di ferro, base e cupola, anello, un uccellino giallo-bruno sul posatoio. Origine all'anello (pende verso il basso)."""
    mats = crea_mats(['FerroGabbia', 'Uccellino'])
    bm = bmesh.new()
    R0, H0 = 0.13, 0.28
    n = 18
    for i in range(n):
        a = i / n * 2 * math.pi
        cx, cy = R0 * math.cos(a), R0 * math.sin(a)
        box(bm, (cx - 0.0035, cy - 0.0035, 0.02), (cx + 0.0035, cy + 0.0035, H0), 0)
    v, _ = cilindro(bm, 0.0, 0.025, R0 + 0.01, 18, 0)
    v, _ = cilindro(bm, H0, H0 + 0.07, R0 + 0.004, 18, 0, raggio2=0.02)
    v, _ = cilindro(bm, H0, H0 + 0.012, R0 + 0.006, 18, 0)
    # anello e gancio
    v, _ = cilindro(bm, H0 + 0.07, H0 + 0.12, 0.006, 8, 0)
    box(bm, (-0.02, -0.004, H0 + 0.12), (0.02, 0.004, H0 + 0.135), 0)
    # posatoio e uccellino
    v, _ = cilindro(bm, -0.1, 0.1, 0.005, 6, 0, asse='x')
    trasla(bm, v, (0, 0, 0.09))
    r = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.035)
    for vv in r['verts']:
        vv.co.y *= 0.7
        vv.co.z *= 0.9
    trasla(bm, r['verts'], (0, 0, 0.135))
    for f in {f for vv in r['verts'] for f in vv.link_faces}:
        f.material_index = 1
    r2 = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.018)
    trasla(bm, r2['verts'], (0, -0.03, 0.165))
    for f in {f for vv in r2['verts'] for f in vv.link_faces}:
        f.material_index = 1
    o = oggetto_da_bm('GabbiaUccellino', bm, mats)
    o.data.transform(Matrix.Translation((0, 0, -(H0 + 0.135))))
    for p in o.data.polygons:
        p.use_smooth = True
    return o, {'piatti': {'FerroGabbia': {'colore': [0.10, 0.10, 0.11], 'metallico': 0.85, 'levig': 0.35},
                          'Uccellino': {'colore': [0.62, 0.48, 0.14], 'metallico': 0.0, 'levig': 0.25}}}
