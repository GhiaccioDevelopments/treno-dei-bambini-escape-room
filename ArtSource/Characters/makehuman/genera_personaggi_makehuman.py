# -*- coding: utf-8 -*-
"""
Il Treno dei Bambini - Generatore dei PERSONAGGI SECONDARI per MakeHuman 1.2/1.3 (Utilities > Scripting)
Gemello di genera_capa_e_fierro_makehuman.py, ma dati-guidato: una voce in PERSONAGGI per personaggio.
Antonietta, Maddalena, Pachiochia, controllore, volontaria del Comitato, volontaria della Croce Rossa (Napoli 1946),
Derna, Alcide, Rosa (Modena 1946-47), Carmine (1994). Personaggi ORIGINALI: nessuna sembianza degli attori del film.

PRIMA, una volta per PC:  prepara_materiali_amerigo.py  e poi  prepara_materiali_personaggi.py  (Python di MakeHuman).
USO: Utilities > Scripting > Load > questo file > Execute (circa 1 minuto per personaggio).
Variabili d'ambiente opzionali: TRENO_REPO (radice del repository), TRENO_PERSONAGGI (nomi separati da virgola; vuoto = tutti).

Risultati in  ArtSource/Characters/makehuman/<Nome>/  <Nome>.mhm, <Nome>.fbx (+ textures/), screenshot/*.png
Poi in Unity: Treno > Personaggi > Importa tutti i personaggi MakeHuman.
Asset di partenza: MakeHuman, CC0.
"""


# MakeHuman esegue lo script con globali e locali separati: tutto sta dentro _pers_main().
def _pers_main():
    import os
    import glob

    # ============================================================ impostazioni
    ESPRESSIONI = False        # varianti .mhm + primi piani per ogni emozione dei dialoghi
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
    # variabile d'ambiente opzionale TRENO_REPO (radice del repository): primo candidato, per l'esecuzione automatica
    if os.environ.get("TRENO_REPO"):
        _REPO.insert(0, os.environ["TRENO_REPO"])
    CARTELLA_OUTPUT = None
    for _r in _REPO:
        _c = os.path.join(os.path.expanduser(_r), _SOTTO)
        if os.path.isdir(_c):
            CARTELLA_OUTPUT = _c
            break
    if CARTELLA_OUTPUT is None:
        CARTELLA_OUTPUT = os.path.expanduser("~/Documents/makehuman/v1py3/amerigo")
    print("[Pers] cartella di output: " + CARTELLA_OUTPUT)

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

    ETNIA = {"Caucasian": 0.80, "African": 0.10, "Asian": 0.10}   # tono mediterraneo

    def D(**kw):
        """Voce di personaggio con valori di base; kw sovrascrive."""
        v = {"macro": {}, "dettagli": {}, "etnia": ETNIA, "sopracciglia": "eyebrow001", "occhi": "brown"}
        v.update(kw)
        return v

    def macro(genere, anni, peso, muscoli, altezza, prop=0.5):
        return {
            "macrodetails/Gender": genere,
            "macrodetails/Age": eta_mh(anni),
            "macrodetails-universal/Weight": peso,
            "macrodetails-universal/Muscle": muscoli,
            "macrodetails-height/Height": altezza,
            "macrodetails-proportions/BodyProportions": prop,
        }

    # Un solo posto da toccare per aggiungere un personaggio. vestiti: (asset, materiale .mhmat o None, [file .mhpxy])
    # Napoli 1946 / Modena 1946-47: tessuti spenti e lisi (materiali di prepara_materiali_personaggi.py), niente moderno.
    PERSONAGGI = {
        "Antonietta": D(     # madre di Amerigo, ~32 anni, magra, abito scuro liso (Docs/TRAMA.md)
            macro=macro(0.0, 32, 0.22, 0.35, 0.40),
            dettagli={"cheek/l-cheek-bones-decr|incr": 0.35, "cheek/r-cheek-bones-decr|incr": 0.35,
                      "cheek/l-cheek-volume-decr|incr": -0.35, "cheek/r-cheek-volume-decr|incr": -0.35},
            pelle="skins/middleage_caucasian_female/middleage_caucasian_female.mhmat",
            capelli=("ponytail01", "antonietta_capelli.mhmat"),
            vestiti=[("treno_tailleur_lungo", "antonietta_abito.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat")]),
        "Maddalena": D(      # giovane militante del Comitato, ~25 anni, energica
            macro=macro(0.0, 25, 0.38, 0.45, 0.50),
            dettagli={"eyebrows/eyebrows-angle-down|up": -0.25},
            pelle="skins/young_caucasian_female/young_caucasian_female.mhmat",
            capelli=("braid01", "maddalena_capelli.mhmat"),
            vestiti=[("treno_tailleur_lungo", "maddalena_abito.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat")]),
        "Pachiochia": D(     # vicina del vicolo, ~45 anni, robusta
            macro=macro(0.0, 45, 0.80, 0.45, 0.42),
            dettagli={"stomach/stomach-pregnant-decr|incr": 0.30, "neck/neck-scale-horiz-decr|incr": 0.25},
            pelle="skins/middleage_caucasian_female/middleage_caucasian_female.mhmat",
            capelli=("ponytail01", "pachiochia_capelli.mhmat"),
            vestiti=[("treno_tailleur_lungo", "pachiochia_abito.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat")]),
        "Controllore": D(    # controllore del treno, ~45 anni, divisa scura
            macro=macro(1.0, 45, 0.55, 0.50, 0.55),
            pelle="skins/middleage_caucasian_male/middleage_caucasian_male.mhmat",
            sopracciglia="eyebrow010",
            capelli=("short01", "controllore_capelli.mhmat"),
            vestiti=[("male_elegantsuit01", "controllore_divisa.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat"),
                     ("treno_berretto_ferroviere", "controllore_berretto.mhmat")]),
        "VolontariaComitato": D(   # volontaria del Comitato, ~28 anni, abito semplice grigio-verde
            macro=macro(0.0, 28, 0.40, 0.40, 0.48),
            pelle="skins/young_caucasian_female/young_caucasian_female.mhmat",
            capelli=("braid01", "volontaria_comitato_capelli.mhmat"),
            vestiti=[("treno_tailleur_lungo", "volontaria_comitato_abito.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat")]),
        "VolontariaCroceRossa": D(  # volontaria della Croce Rossa, ~30 anni, camice bianco di cotone
            macro=macro(0.0, 30, 0.40, 0.40, 0.50),
            pelle="skins/young_caucasian_female/young_caucasian_female.mhmat",
            capelli=("ponytail01", "volontaria_cr_capelli.mhmat"),
            vestiti=[("treno_tailleur_lungo", "volontaria_cr_abito.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat")]),
        "Derna": D(          # Modena, ~35 anni, militante, lana grigio-verde rammendata
            macro=macro(0.0, 35, 0.35, 0.50, 0.50),
            dettagli={"eyebrows/eyebrows-angle-down|up": -0.20},
            pelle="skins/middleage_caucasian_female/middleage_caucasian_female.mhmat",
            capelli=("ponytail01", "derna_capelli.mhmat"),
            vestiti=[("treno_tailleur_lungo", "derna_cappotto.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat")]),
        "Alcide": D(         # Modena, ~40 anni, accordatore di pianoforti, camicia e cappello di feltro
            macro=macro(1.0, 40, 0.45, 0.45, 0.52),
            pelle="skins/middleage_caucasian_male/middleage_caucasian_male.mhmat",
            sopracciglia="eyebrow010",
            capelli=("short04", "alcide_capelli.mhmat"),
            vestiti=[("male_casualsuit03", "alcide_abito.mhmat"), ("fedora01", None, "fedora.mhpxy"),
                     ("shoes01", "scarpe_usate.mhmat")]),
        "Rosa": D(           # Modena, ~60 anni, fornaia, abito scuro, capelli bianchi
            macro=macro(0.0, 60, 0.68, 0.38, 0.42),
            pelle="skins/old_caucasian_female/old_caucasian_female.mhmat",
            capelli=("ponytail01", "rosa_capelli.mhmat"),
            vestiti=[("treno_tailleur_lungo", "rosa_abito.mhmat"), ("shoes03", "scarpe_nere_usate.mhmat")]),
        "Carmine": D(        # 1994, ~55 anni, abiti sobri (materiali originali di MakeHuman)
            macro=macro(1.0, 55, 0.55, 0.45, 0.52),
            pelle="skins/middleage_caucasian_male/middleage_caucasian_male.mhmat",
            sopracciglia="eyebrow010",
            capelli=("short03", "carmine_capelli.mhmat"),
            vestiti=[("male_casualsuit05", None), ("shoes03", None)]),
    }
    _scelti = [x.strip() for x in os.environ.get("TRENO_PERSONAGGI", "").split(",") if x.strip()]

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
        print("[Pers] " + msg)

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
        if not os.path.isdir(cartella):
            # abiti d'epoca creati da abiti_epoca/crea_abiti_epoca.py (cartella dati utente di MakeHuman)
            cartella = os.path.join(getpath.getDataPath(tipo), nome)
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
        dati = PERSONAGGI[chiave]
        nome = chiave
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
        for k, v in dati["etnia"].items():
            modificatore("macrodetails/" + k, v)
        for k, v in dati["dettagli"].items():
            modificatore(k, v)
        human.applyAllTargets()

        # 3. pelle e occhi
        pelle = None
        for rel in [dati["pelle"], "skins/default.mhmat"]:
            pelle = carica_materiale(os.path.join(SYS, rel))
            if pelle is not None:
                break
        if dati.get("pelle_amerigo"):
            pelle = materiale_amerigo(dati["pelle_amerigo"]) or pelle
        if pelle is not None:
            human.material = pelle
        scegli_proxy("Eyes", "eyes", "high-poly")
        occhi = carica_materiale(os.path.join(SYS, "eyes", "materials", dati["occhi"] + ".mhmat"))
        if occhi is not None and human.eyesProxy is not None:
            human.eyesProxy.object.material = occhi

        # 4. viso: sopracciglia, ciglia, denti
        scegli_proxy("Eyebrows", "eyebrows", dati["sopracciglia"])
        scegli_proxy("Eyelashes", "eyelashes", "eyelashes01")
        scegli_proxy("Teeth", "teeth", "teeth_base")

        # 5. capelli
        cap_asset, cap_mat = dati["capelli"]
        scegli_proxy("Hair", "hair", cap_asset)
        m = materiale_amerigo(cap_mat)
        if m is not None and human.hairProxy is not None:
            human.hairProxy.object.material = m

        # 6. vestiti
        tv_vestiti = taskview("Geometries", ["Clothes"])
        for voce in dati["vestiti"]:
            asset, mat = voce[0], voce[1]
            try:
                if len(voce) > 2:
                    tv_vestiti.proxyFileSelected(os.path.join(SYS, "clothes", asset, voce[2]))
                else:
                    tv_vestiti.proxyFileSelected(file_asset("clothes", asset))
            except Exception as e:
                avviso("vestito %s non applicato: %s" % (asset, e))
                continue
            m = materiale_amerigo(mat)    # None = materiale originale di MakeHuman
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
    for _n in PERSONAGGI:
        if _scelti and _n not in _scelti:
            continue
        try:
            crea(_n)
        except Exception as _e:
            avviso("%s: errore %s" % (_n, _e))

    G.app.redraw()
    if problemi:
        log("Finito con %d avvisi:" % len(problemi))
        for p in problemi:
            log("  - " + p)
    else:
        log("Finito senza problemi.")


_pers_main()
