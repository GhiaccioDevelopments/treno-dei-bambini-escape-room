# -*- coding: utf-8 -*-
"""
Il Treno dei Bambini - crea gli abiti d'epoca derivati da asset CC0 di MakeHuman.
Si lancia col Python di MakeHuman (numpy incluso):
  "%ProgramFiles%\makehuman-community\Python\python.exe" -I crea_abiti_epoca.py
Legge gli asset di sistema (data/clothes) e scrive in <Documenti>/makehuman/v1py3/data/clothes/<nome>/
una copia con la gonna allungata (sotto il ginocchio / a meta' polpaccio). Il generatore dei personaggi
(genera_personaggi_makehuman.py) cerca gli asset anche li'. Asset di partenza: female_elegantsuit01 (CC0).
Nessuna dipendenza esterna: solo lo spostamento dei vertici della gonna (offset rispetto al corpo).
"""
import os
import shutil
import sys

import numpy as np

SYS_CLOTHES = None
for _p in (os.path.join(os.environ.get("ProgramFiles", ""), "makehuman-community", "makehuman", "data", "clothes"),
           os.path.join(os.environ.get("LOCALAPPDATA", ""), "makehuman-community", "makehuman", "data", "clothes")):
    if os.path.isdir(_p):
        SYS_CLOTHES = _p
        break
if SYS_CLOTHES is None:
    sys.exit("MakeHuman non trovato")
USER_CLOTHES = os.path.join(os.path.expanduser("~"), "Documents", "makehuman", "v1py3", "data", "clothes")


def _s(testo):
    return np.frombuffer(testo.encode("utf8"), dtype="S1")


def gonna_lunga(origine, nuovo, allungamento, y_alto=0.4, y_orlo=-2.99, esponente=1.0, svasatura=0.0, gonfio_z=0.0):
    """Copia l'asset 'origine' come 'nuovo' abbassando l'orlo della gonna di 'allungamento' unita' MakeHuman
    (1 unita' = 10 cm circa). I vertici sopra y_alto non si muovono: giacca e busto restano uguali."""
    src = os.path.join(SYS_CLOTHES, origine)
    dst = os.path.join(USER_CLOTHES, nuovo)
    os.makedirs(dst, exist_ok=True)
    pxy = dict(np.load(os.path.join(src, origine + ".mhpxy")))
    msh = dict(np.load(os.path.join(src, origine + ".npz")))
    y = msh["coord"][:, 1]
    t = np.clip((y_alto - y) / (y_alto - y_orlo), 0.0, 1.0) ** esponente
    spost = (allungamento * t).astype(np.float32)
    pxy["offsets"] = pxy["offsets"].copy()
    pxy["offsets"][:, 1] -= spost
    # svasatura laterale: in posa A le gambe divergono, la gonna scende larga per non lasciarle fuori
    dx = (msh["coord"][:, 0] * svasatura * t).astype(np.float32)
    pxy["offsets"][:, 0] += dx
    # le cosce spingono in avanti: la gonna si gonfia davanti e dietro nel tratto di mezzo (centro in z della gonna)
    gonna = y < y_alto
    zc = float(msh["coord"][gonna, 2].mean())
    fz = gonfio_z * np.sin(np.pi * np.clip(t / 0.8, 0.0, 1.0)) + 0.4 * gonfio_z * t
    dz = ((msh["coord"][:, 2] - zc) * fz).astype(np.float32)
    pxy["offsets"][:, 2] += dz
    msh["coord"] = msh["coord"].copy()
    msh["coord"][:, 1] -= spost
    msh["coord"][:, 0] += dx
    msh["coord"][:, 2] += dz
    pxy["name"] = _s(nuovo)
    pxy["obj_file"] = _s(nuovo + ".obj")
    pxy["material_file"] = _s(nuovo + ".mhmat")
    # uuid nuovo: uguale a quello vecchio ma con suffisso fisso (stessa lunghezza: 36 caratteri)
    import uuid as _u
    pxy["uuid"] = _s(str(_u.uuid5(_u.NAMESPACE_DNS, "treno.bambini." + nuovo)))
    np.savez(os.path.join(dst, nuovo + ".mhpxy"), **pxy)
    os.replace(os.path.join(dst, nuovo + ".mhpxy.npz"), os.path.join(dst, nuovo + ".mhpxy")) \
        if os.path.exists(os.path.join(dst, nuovo + ".mhpxy.npz")) else None
    np.savez(os.path.join(dst, nuovo + ".npz"), **msh)
    for ext in ("_ao.png", "_normal.png", "_diffuse.png"):
        f = os.path.join(src, origine + ext)
        if os.path.isfile(f):
            shutil.copyfile(f, os.path.join(dst, nuovo + ext))
    with open(os.path.join(src, origine + ".mhmat"), "r", encoding="utf8") as f:
        mat = f.read().replace(origine, nuovo)
    with open(os.path.join(dst, nuovo + ".mhmat"), "w", encoding="utf8") as f:
        f.write("# Il Treno dei Bambini: derivato da asset CC0 di MakeHuman (%s), gonna allungata\n" % origine + mat)
    print("creato", dst, "spostamento max", float(spost.max()))


def berretto_ferroviere(nome="treno_berretto_ferroviere"):
    """Berretto da ferroviere 1946 (kepi con fascia e visiera), generato da codice: nessun asset esterno.
    Vertici legati ai vertici della testa della base MakeHuman (hm08) come un normale .mhclo."""
    base = np.load(os.path.join(os.path.dirname(SYS_CLOTHES), "3dobjs", "base.npz"))["coord"][:13380]
    testa = np.where(base[:, 1] > 7.0)[0]
    zc = 0.58
    N = 28
    ang = np.linspace(0, 2 * np.pi, N, endpoint=False)

    def anello(y, rx, rz):
        return np.stack([rx * np.sin(ang), np.full(N, y), zc + rz * np.cos(ang)], axis=1)

    # kepi FS anni '40: fascia scura (7.50-7.72), calotta che si svasa verso l'alto e poi si schiaccia (altezza ~8 cm),
    # nessuna tesa laterale o dietro; solo una visiera rigida davanti inclinata in basso
    U_CALOTTA, U_FASCIA, U_VISIERA = 0.16, 0.50, 0.85
    sezioni = [(anello(7.50, 0.82, 1.04), U_FASCIA), (anello(7.72, 0.84, 1.06), U_FASCIA),
               (anello(7.72, 0.84, 1.06), U_CALOTTA), (anello(7.98, 0.91, 1.13), U_CALOTTA),
               (anello(8.20, 0.99, 1.22), U_CALOTTA), (anello(8.26, 0.88, 1.08), U_CALOTTA)]
    anelli = [s[0] for s in sezioni]
    v = np.vstack(anelli + [np.array([[0.0, 8.28, zc + 0.10]])])
    uv = []
    for _a, _u in sezioni:
        uv += [(_u, 0.5)] * N
    uv += [(U_CALOTTA, 0.5)]
    facce = []
    for r in range(len(anelli) - 1):
        if r == 1:
            continue        # fra fascia e calotta i vertici coincidono (cambia solo l'UV)
        for i in range(N):
            j = (i + 1) % N
            facce.append((r * N + i, r * N + j, (r + 1) * N + j, (r + 1) * N + i))
    punta = len(v) - 1
    ultimo = (len(anelli) - 1) * N
    for i in range(N):
        facce.append((ultimo + i, ultimo + (i + 1) % N, punta))
    # visiera: arco stretto sul davanti (+-42 gradi), sporge di ~6 cm e scende, faccia di sopra e di sotto
    archi = np.linspace(-0.75, 0.75, 9)
    base_v = len(v)
    pv = [[0.86 * np.sin(a), 7.66, zc + 1.06 * np.cos(a)] for a in archi]
    pv += [[1.02 * np.sin(a), 7.30, zc + 1.78 * np.cos(a)] for a in archi]
    pv = np.array(pv)
    v = np.vstack([v, pv, pv + np.array([0.0, -0.03, 0.0])])    # sopra e sotto: vertici distinti (niente bordi condivisi)
    uv = uv + [(U_VISIERA, 0.5)] * (2 * len(pv))
    n = len(archi)
    for i in range(n - 1):
        a, b, c, d = base_v + i, base_v + i + 1, base_v + n + i + 1, base_v + n + i
        e, f_, g, h = a + 2 * n, b + 2 * n, c + 2 * n, d + 2 * n
        facce.append((a, d, c, b))
        facce.append((e, f_, g, h))
    v = v + np.array([0.0, 0.34, 0.0])      # il berretto sta sopra la fronte, non sugli occhi
    # legami con la testa: 3 vertici vicini, pesi inversi alla distanza
    righe = []
    for p in v:
        d = np.linalg.norm(base[testa] - p, axis=1)
        k = np.argsort(d)[:3]
        w = 1.0 / np.maximum(d[k], 1e-4)
        w /= w.sum()
        rif = testa[k]
        off = p - (w[:, None] * base[rif]).sum(axis=0)
        righe.append("%d %d %d %.5f %.5f %.5f %.5f %.5f %.5f" % (
            rif[0], rif[1], rif[2], w[0], w[1], w[2], off[0], off[1], off[2]))
    import uuid as _u
    dst = os.path.join(USER_CLOTHES, nome)
    os.makedirs(dst, exist_ok=True)
    with open(os.path.join(dst, nome + ".obj"), "w") as f:
        f.write("# berretto da ferroviere generato da codice (Il Treno dei Bambini), CC0\n")
        for p in v:
            f.write("v %.5f %.5f %.5f\n" % tuple(p))
        for u in uv:
            f.write("vt %.4f %.4f\n" % u)
        f.write("usemtl %s\ng %s\n" % (nome, nome))
        for q in facce:
            f.write("f " + " ".join("%d/%d" % (i + 1, i + 1) for i in q) + "\n")
    with open(os.path.join(dst, nome + ".mhclo"), "w") as f:
        f.write("# Il Treno dei Bambini - berretto da ferroviere generato da codice.\n")
        f.write("# license CC0\n# author Il Treno dei Bambini (generato)\n")
        f.write("name %s\nuuid %s\nbasemesh hm08\ntag cap\ntag railway\n" % (
            nome, _u.uuid5(_u.NAMESPACE_DNS, "treno.bambini." + nome)))
        f.write("obj_file %s.obj\nmaterial %s.mhmat\nz_depth 60\nmax_pole 32\n" % (nome, nome))
        f.write("x_scale 5399 11998 1.4800\ny_scale 791 881 2.3298\nz_scale 962 5320 1.9221\n")
        f.write("verts 0\n" + "\n".join(righe) + "\n")
    with open(os.path.join(dst, nome + ".mhmat"), "w") as f:
        f.write("name %s\ntag MakeHuman\nambientColor 1.0 1.0 1.0\ndiffuseColor 1.0 1.0 1.0\n"
                "specularColor 0.03 0.03 0.03\nshininess 0.2\nopacity 1.0\nbackfaceCull False\n"
                "diffuseTexture %s_diffuse.png\n"
                "shader data/shaders/glsl/litsphere\nshaderParam litsphereTexture data/litspheres/lit_matte.png\n"
                "shaderConfig diffuse True\n" % (nome, nome))
    print("creato", dst, len(v), "vertici", len(facce), "facce")


if __name__ == "__main__":
    gonna_lunga("female_elegantsuit01", "treno_tailleur_lungo", 2.8, esponente=0.8, svasatura=0.34, gonfio_z=0.35)
    berretto_ferroviere()
