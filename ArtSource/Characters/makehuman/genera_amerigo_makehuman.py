# -*- coding: utf-8 -*-
"""
Il Treno dei Bambini - Generatore COMPLETO di Amerigo per MakeHuman 1.2 (Utilities > Scripting)

Crea i due Amerigo (vedi CLAUDE.md e Docs/TRAMA.md) con:
  corpo, pelle, occhi castani, sopracciglia, ciglia, denti, capelli CASTANI (decisione 2026-11-06),
  vestiti, scheletro "game_engine" (adatto a Unity Humanoid), espressioni per i dialoghi,
  salvataggio .mhm, esportazione FBX e screenshot di controllo.

  - Amerigo bambino (flashback, Napoli 1946): 7 anni quasi 8, poverissimo: molto magro,
    SCALZO con piedi e gambe sporchi, salopette lisa, sporca, piena di toppe e strappata.
  - Amerigo adulto (1994): ~55 anni, violinista affermato, abito scuro sobrio, scarpe nere.

PRIMA di usarlo, una volta sola, prepara i materiali d'epoca:
  "%LOCALAPPDATA%\\makehuman-community\\Python\\python.exe" prepara_materiali_amerigo.py
(se mancano, lo script usa i materiali originali di MakeHuman e lo scrive nel log)

USO: Utilities > Scripting > Load > questo file > (scegli VERSIONE) > Execute.
Ci vogliono 1-3 minuti: MakeHuman sembra bloccato ma sta lavorando.

Risultati in  ArtSource/Characters/makehuman/<Amerigo_bambino|Amerigo_adulto>/
  <nome>.mhm                 modello neutro (si riapre con Files > Load)
  <nome>.fbx (+ textures/)   per Blender/Unity
  espressioni/<nome>_<emozione>.mhm   varianti per i ritratti dei dialoghi
  screenshot/*.png           corpo intero e un primo piano per ogni espressione

Personaggi ORIGINALI: nessun riferimento alle sembianze degli attori del film.
Asset di partenza: MakeHuman, CC0.
"""


# MakeHuman esegue lo script con globali e locali separati: tutto sta dentro _amerigo_main().
def _amerigo_main():
    import os
    import glob

    # ============================================================ impostazioni
    VERSIONE = "entrambi"      # "bambino" | "adulto" | "entrambi"
    ESPRESSIONI = True         # varianti .mhm + primi piani per ogni emozione dei dialoghi
    ESPORTA_FBX = True
    SCREENSHOT = True
    SCHELETRO = "game_engine"  # "game_engine" per Unity, "default", oppure None (nessuno, es. per Mixamo)

    CARTELLA_OUTPUT = os.path.expanduser(
        "~/Documents/treno-dei-bambini-escape-room/ArtSource/Characters/makehuman")

    # emozioni usate nei dialoghi del gioco (campo "emozione" dei cutscene_*.json)
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

    ETNIA = {"Caucasian": 0.80, "African": 0.10, "Asian": 0.10}   # tono mediterraneo

    AMERIGO = {
        "bambino": {
            "nome": "Amerigo_bambino",
            "macro": {
                "macrodetails/Gender": 1.0,
                "macrodetails/Age": eta_mh(7.5),
                "macrodetails-universal/Weight": 0.10,      # molto magro: fame del dopoguerra
                "macrodetails-universal/Muscle": 0.28,
                "macrodetails-height/Height": 0.34,         # piccolo per l'eta' (malnutrizione)
                "macrodetails-proportions/BodyProportions": 0.50,
            },
            "dettagli": {
                "cheek/l-cheek-volume-decr|incr": -0.45,    # guance scavate
                "cheek/r-cheek-volume-decr|incr": -0.45,
                "cheek/l-cheek-bones-decr|incr": 0.20,      # zigomi in evidenza
                "cheek/r-cheek-bones-decr|incr": 0.20,
                "eyes/l-eye-scale-decr|incr": 0.15,
                "eyes/r-eye-scale-decr|incr": 0.15,
                "neck/neck-scale-horiz-decr|incr": -0.25,   # collo sottile
            },
            "pelle": ["skins/young_caucasian_male/young_caucasian_male.mhmat"],
            "pelle_amerigo": "amerigo_bambino_pelle.mhmat",             # sporca su piedi, gambe e mani
            "sopracciglia": "eyebrow001",
            "capelli": ("short02", "amerigo_bambino_capelli.mhmat"),     # corti e spettinati, castani
            "vestiti": [("male_worksuit01", "amerigo_bambino_salopette.mhmat")],   # scalzo: niente scarpe
        },
        "adulto": {
            "nome": "Amerigo_adulto",
            "macro": {
                "macrodetails/Gender": 1.0,
                "macrodetails/Age": eta_mh(55),
                "macrodetails-universal/Weight": 0.42,
                "macrodetails-universal/Muscle": 0.40,
                "macrodetails-height/Height": 0.58,
                "macrodetails-proportions/BodyProportions": 0.60,
            },
            "dettagli": {
                "cheek/l-cheek-volume-decr|incr": -0.15,
                "cheek/r-cheek-volume-decr|incr": -0.15,
                "chin/chin-prominent-decr|incr": 0.15,
                "nose/nose-scale-vert-decr|incr": 0.10,
            },
            "pelle": ["skins/middleage_caucasian_male/middleage_caucasian_male.mhmat"],
            "sopracciglia": "eyebrow002",
            "capelli": ("short04", "amerigo_adulto_capelli.mhmat"),      # corti (scelti dall utente il 2026-11-06), castano scuro
            "vestiti": [("male_elegantsuit01", None),                   # abito scuro sobrio
                        ("shoes03", None)],                             # scarpe nere
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
        print("[Amerigo] " + msg)

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
    if VERSIONE == "entrambi":
        crea("bambino")
        crea("adulto")             # a schermo resta l'adulto
    elif VERSIONE in AMERIGO:
        crea(VERSIONE)
    else:
        raise ValueError("VERSIONE non valida: usa 'bambino', 'adulto' o 'entrambi'")

    G.app.redraw()
    if problemi:
        log("Finito con %d avvisi:" % len(problemi))
        for p in problemi:
            log("  - " + p)
    else:
        log("Finito senza problemi.")


_amerigo_main()
