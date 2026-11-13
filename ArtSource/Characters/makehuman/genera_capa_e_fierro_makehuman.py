# -*- coding: utf-8 -*-
"""
Il Treno dei Bambini - Generatore di CAPA 'E FIERRO per MakeHuman 1.2/1.3 (Utilities > Scripting)
Gemello di genera_amerigo_makehuman.py (stessa pipeline, stesse funzioni): R012, capitolo "Stracci".

Capa 'e fierro (Docs/TRAMA.md, Decisione 3): robivecchi del vicolo degli stracci, Napoli 1946, sulla cinquantina,
robusto e un po' appesantito, spalle larghe da chi carica il carretto; brontolone ma in fondo buono.
Testa grossa quasi rasata con i capelli grigi corti (il soprannome), sopracciglia folte, viso largo e segnato.
Abiti da robivecchi: tuta da lavoro scurita, lisa e rattoppata, scarpe consumate.
PERSONAGGIO ORIGINALE: nessun riferimento alle sembianze degli attori del film.

PRIMA di usarlo, una volta sola per PC, prepara i materiali d'epoca (contiene anche quelli di Capa 'e fierro):
  doppio clic su 1_prepara_materiali.bat
(se mancano, lo script usa i materiali originali di MakeHuman e lo scrive nel log)

USO: Utilities > Scripting > Load > questo file > Execute (1-2 minuti).

Risultati in  ArtSource/Characters/makehuman/Capa_e_fierro/
  Capa_e_fierro.mhm (+ espressioni/), Capa_e_fierro.fbx (+ textures/), screenshot/*.png
Poi in Unity: Treno > Personaggi > Importa Capa 'e fierro MakeHuman, e Treno > Scene > Costruisci ricordo Stracci.
Il ritratto per la cutscene: Treno > Personaggi > Genera ritratto Capa 'e fierro (ripiego a cartone; si puo' sostituire
con un primo piano di screenshot/).
Asset di partenza: MakeHuman, CC0.
"""


# MakeHuman esegue lo script con globali e locali separati: tutto sta dentro _capa_main().
def _capa_main():
    import os
    import glob

    # ============================================================ impostazioni
    VERSIONE = "capa"
    ESPRESSIONI = True         # varianti .mhm + primi piani per ogni emozione dei dialoghi
    ESPORTA_FBX = True
    SCREENSHOT = True
    SCHELETRO = "game_engine"  # come Amerigo: mappa Humanoid esplicita in PersonaggiMakeHumanBuilder.cs

    _SOTTO = os.path.join("ArtSource", "Characters", "makehuman")
    _REPO = [
        "~/Documents/treno-dei-bambini-escape-room",
        "~/Il Treno dei Bambini",
        "~/Documents/Il Treno dei Bambini",
        "~/treno-dei-bambini-escape-room",
    ]
    CARTELLA_OUTPUT = None
    for _r in _REPO:
        _c = os.path.join(os.path.expanduser(_r), _SOTTO)
        if os.path.isdir(_c):
            CARTELLA_OUTPUT = _c
            break
    if CARTELLA_OUTPUT is None:
        CARTELLA_OUTPUT = os.path.expanduser("~/Documents/makehuman/v1py3/amerigo")
    print("[Capa] cartella di output: " + CARTELLA_OUTPUT)

    # emozioni usate nei dialoghi (campo "emozione" di cutscene_stracci.json); stesse pose di Amerigo
    EMOZIONI = {
        "felice": "smile02",
        "triste": "sad01",
        "sorpresa": "surprise01",
        "decisa": "determination01",
        "pensierosa": "doubt01",
        "arrabbiata": "anger01",
        "paura": "fear01",
    }

    def eta_mh(anni):
        """Anni reali -> valore 0..1 di MakeHuman (0=1 anno, 0.1875=11, 0.5=25, 1=90)."""
        if anni <= 11:
            return max(0.0, (anni - 1.0) / 10.0 * 0.1875)
        if anni <= 25:
            return 0.1875 + (anni - 11.0) / 14.0 * (0.5 - 0.1875)
        return min(1.0, 0.5 + (anni - 25.0) / 65.0 * 0.5)

    ETNIA = {"Caucasian": 0.78, "African": 0.12, "Asian": 0.10}   # tono mediterraneo, abbronzato dal lavoro in strada

    # un solo personaggio, ma con la stessa struttura del generatore di Amerigo (cosi' crea() resta identica)
    AMERIGO = {
        "capa": {
            "nome": "Capa_e_fierro",
            "macro": {
                "macrodetails/Gender": 1.0,
                "macrodetails/Age": eta_mh(50),
                "macrodetails-universal/Weight": 0.72,      # robusto, un po' di pancia
                "macrodetails-universal/Muscle": 0.62,      # braccia da chi spinge il carretto
                "macrodetails-height/Height": 0.52,         # ~1,70 m
                "macrodetails-proportions/BodyProportions": 0.45,
            },
            "dettagli": {
                "head/head-square": 0.45,                   # testa grossa e squadrata ("capa 'e fierro")
                "head/head-scale-horiz-decr|incr": 0.12,
                "neck/neck-scale-horiz-decr|incr": 0.35,    # collo taurino
                "chin/chin-width-decr|incr": 0.30,
                "nose/nose-scale-horiz-decr|incr": 0.25,    # naso largo
                "nose/nose-scale-vert-decr|incr": 0.15,
                "eyebrows/eyebrows-trans-down|up": -0.25,   # sopracciglia basse: sembra sempre accigliato
                "eyes/l-eye-scale-decr|incr": -0.10,
                "eyes/r-eye-scale-decr|incr": -0.10,
                "stomach/stomach-pregnant-decr|incr": 0.30,
            },
            "pelle": ["skins/middleage_caucasian_male/middleage_caucasian_male.mhmat"],
            "sopracciglia": "eyebrow010",
            "capelli": ("short02", "capa_e_fierro_capelli.mhmat"),       # corti, grigi
            "vestiti": [("male_worksuit01", "capa_e_fierro_abito.mhmat"),  # tuta scurita, lisa, rattoppata
                        ("shoes01", "capa_e_fierro_scarpe.mhmat")],        # scarpe consumate
        },
    }

    # ============================================================ MakeHuman
    from core import G
    import getpath
    import material
    import mh

    human = G.app.selectedHuman
    SYS = getpath.getSysDataPath()
    MATERIALI = getpath.getDataPath("amerigo")
    problemi = []

    def log(msg):
        print("[Capa] " + msg)

    def avviso(msg):
        problemi.append(msg)
        log("ATTENZIONE: " + msg)

    def taskview(categoria, nomi):
        """Trova una scheda di MakeHuman (es. Geometries > Clothes) anche se il nome cambia un po'."""
        cats = G.app.categories
        if categoria in cats:
            for n in nomi:
                if n in cats[categoria].tasksByName:
                    return cats[categoria].tasksByName[n]
        for cat in cats.values():
            for k, tv in cat.tasksByName.items():
                if k.lower() in [n.lower() for n in nomi]:
                    return tv
        raise KeyError("scheda non trovata: %s > %s" % (categoria, nomi))

    def file_asset(tipo, nome):
        """Percorso del file .mhclo/.mhpxy di un asset di sistema (es. clothes/shoes01)."""
        cartella = os.path.join(SYS, tipo, nome)
        for ext in (".mhclo", ".mhpxy"):
            f = os.path.join(cartella, nome + ext)
            if os.path.isfile(f):
                return f
        trovati = glob.glob(os.path.join(cartella, "*.mhclo")) + glob.glob(os.path.join(cartella, "*.mhpxy"))
        if trovati:
            return trovati[0]
        raise IOError("asset non trovato: %s/%s" % (tipo, nome))

    def carica_materiale(percorso):
        if percorso and os.path.isfile(percorso):
            return material.fromFile(percorso)
        return None

    def materiale_amerigo(nome_file):
        if not nome_file:
            return None
        m = carica_materiale(os.path.join(MATERIALI, nome_file))
        if m is None:
            avviso("materiale %s mancante: esegui prima prepara_materiali_amerigo.py" % nome_file)
        return m

    def modificatore(nome, valore):
        try:
            mod = human.getModifier(nome)
        except Exception:
            mod = None
        if mod is None:
            avviso("modificatore inesistente, saltato: " + nome)
            return
        mod.setValue(valore)

    def scegli_proxy(scheda, tipo, nome):
        try:
            tv = taskview("Geometries", [scheda])
            tv.proxyFileSelected(file_asset(tipo, nome))
        except Exception as e:
            avviso("%s '%s' non applicato: %s" % (scheda, nome, e))

    def togli_vestiti():
        try:
            tv = taskview("Geometries", ["Clothes"])
            for pxy in list(human.clothesProxies.values()):
                tv.proxyFileDeselected(pxy.file)
        except Exception as e:
            avviso("rimozione vestiti: %s" % e)

    def espressione(nome_file):
        tv = taskview("Pose/Animate", ["Expression", "Expressions"])
        if getattr(tv, "base_anim", None) is None and hasattr(tv, "_load_pose_units"):
            tv._load_pose_units()      # MakeHuman li carica solo aprendo la scheda Expression a mano
        if nome_file is None:
            tv.chooseExpression(None)
        else:
            tv.chooseExpression(os.path.join(SYS, "expressions", nome_file + ".mhpose"))
        G.app.redraw()

    def inquadra_corpo():
        co = human.meshData.coord
        centro = (co.min(axis=0) + co.max(axis=0)) / 2.0
        G.app.modelCamera.focusOn(centro, [0.0, 0.0, 1.0], 1.0, animate=False)

    def screenshot(percorso, primo_piano):
        try:
            if primo_piano:
                G.app.setTargetCamera(132, 8.7, animate=False)      # stessa inquadratura di "Face camera"
            else:
                inquadra_corpo()
            canvas = getattr(G, "canvas", None) or getattr(getattr(G.app, "mainwin", None), "canvas", None)
            if canvas is not None and hasattr(canvas, "grabFramebuffer"):
                # QOpenGLWidget: ridisegna la scena nel suo framebuffer (con profondita') e la cattura
                canvas.grabFramebuffer().save(percorso)
            else:
                G.app.redraw()
                mh.grabScreen(0, 0, G.windowWidth, G.windowHeight, percorso)
            log("screenshot: " + percorso)
        except Exception as e:
            avviso("screenshot non riuscito (%s): %s" % (os.path.basename(percorso), e))

    def esporta_fbx(percorso):
        import shutil
        vecchie = os.path.join(os.path.dirname(percorso), "textures")
        if os.path.isdir(vecchie):
            shutil.rmtree(vecchie, ignore_errors=True)     # niente texture rimaste da versioni precedenti
        try:
            exp = G.app.mhapi.exports.getFBXExporter()
            exp.export(human, lambda ext: percorso)
            log("FBX esportato: " + percorso)
            # il repository e' pubblico: l'FBX contiene i percorsi assoluti delle texture (con il nome utente del PC)
            try:
                import importlib.util
                _p = os.path.join(CARTELLA_OUTPUT, "pulisci_percorsi_fbx.py")
                _sp = importlib.util.spec_from_file_location("pulisci_percorsi_fbx", _p)
                _m = importlib.util.module_from_spec(_sp)
                _sp.loader.exec_module(_m)
                n = _m.pulisci(percorso)
                log("FBX: %d percorsi personali sostituiti con un segnaposto" % n)
                if _m.contiene_nome_utente(percorso):
                    avviso("l'FBX contiene ancora il nome utente: esegui pulisci_percorsi_fbx.py a mano")
            except Exception as e2:
                avviso("pulizia dei percorsi personali nell'FBX non riuscita: %s (esegui pulisci_percorsi_fbx.py)" % e2)
        except Exception as e:
            avviso("export FBX non riuscito: %s (fallo a mano: Files > Export > Filmbox)" % e)

    def salva(percorso):
        d = os.path.dirname(percorso)
        if not os.path.isdir(d):
            os.makedirs(d)
        human.save(percorso)
        log("salvato: " + percorso)

    # ------------------------------------------------------------ costruzione
    def crea(chiave):
        dati = AMERIGO[chiave]
        nome = dati["nome"]
        cartella = os.path.join(CARTELLA_OUTPUT, nome)
        log("==== creo %s ====" % nome)

        # 1. umano pulito (senza la finestra di conferma di "Reset")
        try:
            espressione(None)
        except Exception:
            pass
        togli_vestiti()
        G.app._resetHuman()

        # 2. corpo
        for k, v in dati["macro"].items():
            modificatore(k, v)
        for k, v in ETNIA.items():
            modificatore("macrodetails/" + k, v)
        for k, v in dati["dettagli"].items():
            modificatore(k, v)
        human.applyAllTargets()

        # 3. pelle e occhi
        pelle = None
        for rel in dati["pelle"] + ["skins/default.mhmat"]:
            pelle = carica_materiale(os.path.join(SYS, rel))
            if pelle is not None:
                break
        if dati.get("pelle_amerigo"):
            pelle = materiale_amerigo(dati["pelle_amerigo"]) or pelle
        if pelle is not None:
            human.material = pelle
        scegli_proxy("Eyes", "eyes", "high-poly")
        occhi = carica_materiale(os.path.join(SYS, "eyes", "materials", "brown.mhmat"))
        if occhi is not None and human.eyesProxy is not None:
            human.eyesProxy.object.material = occhi

        # 4. viso: sopracciglia, ciglia, denti
        scegli_proxy("Eyebrows", "eyebrows", dati["sopracciglia"])
        scegli_proxy("Eyelashes", "eyelashes", "eyelashes01")
        scegli_proxy("Teeth", "teeth", "teeth_base")

        # 5. capelli castani
        cap_asset, cap_mat = dati["capelli"]
        scegli_proxy("Hair", "hair", cap_asset)
        m = materiale_amerigo(cap_mat)
        if m is not None and human.hairProxy is not None:
            human.hairProxy.object.material = m

        # 6. vestiti
        tv_vestiti = taskview("Geometries", ["Clothes"])
        for asset, mat in dati["vestiti"]:
            try:
                tv_vestiti.proxyFileSelected(file_asset("clothes", asset))
            except Exception as e:
                avviso("vestito %s non applicato: %s" % (asset, e))
                continue
            m = materiale_amerigo(mat)
            if m is not None:
                for pxy in human.clothesProxies.values():
                    if os.path.basename(os.path.dirname(pxy.file)) == asset:
                        pxy.object.material = m

        # 7. scheletro per l'animazione
        try:
            tv_sk = taskview("Pose/Animate", ["Skeleton"])
            tv_sk.chooseSkeleton(os.path.join(SYS, "rigs", SCHELETRO + ".mhskel") if SCHELETRO else None)
        except Exception as e:
            avviso("scheletro non applicato: %s" % e)

        G.app.redraw()
        try:
            log("altezza circa %.0f cm" % human.getHeightCm())
        except Exception:
            pass

        # 8. salvataggio e screenshot del modello neutro
        #    (l'FBX si esporta per ultimo: l'esportazione lascia la scena in disordine per gli screenshot)
        salva(os.path.join(cartella, nome + ".mhm"))
        if SCREENSHOT:
            sdir = os.path.join(cartella, "screenshot")
            if not os.path.isdir(sdir):
                os.makedirs(sdir)
            screenshot(os.path.join(sdir, nome + "_corpo.png"), False)
            screenshot(os.path.join(sdir, nome + "_neutra.png"), True)

        # 9. espressioni per i dialoghi
        if ESPRESSIONI:
            for emo, pose in sorted(EMOZIONI.items()):
                try:
                    espressione(pose)
                except Exception as e:
                    avviso("espressione %s non applicata: %s" % (emo, e))
                    continue
                salva(os.path.join(cartella, "espressioni", "%s_%s.mhm" % (nome, emo)))
                if SCREENSHOT:
                    screenshot(os.path.join(cartella, "screenshot", "%s_%s.png" % (nome, emo)), True)
            espressione(None)          # il modello resta neutro

        # 10. FBX del modello neutro
        if ESPORTA_FBX:
            esporta_fbx(os.path.join(cartella, nome + ".fbx"))
            try:
                human.refreshPose()
            except Exception:
                pass

        try:
            inquadra_corpo()
        except Exception:
            pass

    # ------------------------------------------------------------ avvio
    crea("capa")

    G.app.redraw()
    if problemi:
        log("Finito con %d avvisi:" % len(problemi))
        for p in problemi:
            log("  - " + p)
    else:
        log("Finito senza problemi.")


_capa_main()
