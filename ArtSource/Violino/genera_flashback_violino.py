# -*- coding: utf-8 -*-
"""R015: genera Assets/_Game/Resources/Data/flashback_violino.json (i cinque enigmi del ricordo "Il violino": Modena, casa Benvenuti).
I testi del gioco stanno nel JSON generato; questo script e' solo la sorgente riproducibile. Uso: python genera_flashback_violino.py
Coordinate = quelle di SceneBuilderViolino.cs (devono coincidere). Una sola scena (D1): 70_FB_Modena, corte con casa di Derna, casa-laboratorio
dei Benvenuti e scuola in fondo alla strada innevata.
Trama: TRAMA Decisione 2 (versione del libro, nei testi solo "la famiglia Benvenuti", nessuna parentela affermata), §2 sezione 2, §4, §8, §9 n. 1-2
(Derna: ne' vedova ne' partigiana nei testi). Il ragazzo geloso e' Rivo [GIOCO]: le fonti non dicono quale dei tre (il film usa Lucio, qui no).
Melodia: originale (pentatonica in sol), non Bella ciao e non musiche del film. Oggetto del capitolo: lo spartito di Alcide (D2), il violino resta a Modena.
"""
import json, os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "Cappotto"))
from genera_flashback_cappotto import P, T, quiz, tavolo, accenti  # stessi mattoni del ricordo del cappotto

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data", "flashback_violino.json")
MOD = "70_FB_Modena"

# ---------------------------------------------------------------- posizioni nel mondo (= SceneBuilderViolino.cs)
POS_V1 = (9.6, 0.0, -6.7)     # laboratorio: il forno di Rosa contro il muro sud, fronte verso nord (yaw 180)
POS_V2 = (15.1, 0.0, -1.6)    # laboratorio: banco dell'accordatore contro il muro est, fronte verso ovest (yaw 90)
POS_V3 = (15.1, 0.0, 4.2)     # laboratorio: pianoforte verticale contro il muro est, fronte verso ovest (yaw 90)
POS_V4 = (0.0, 0.0, 9.0)      # corte: le impronte nella neve all'imbocco della strada, fronte verso sud (yaw 0)
POS_V5 = (0.0, 0.0, 47.4)     # scuola: la cattedra con il violino e lo spartito, fronte verso sud (yaw 0)


# ---------------------------------------------------------------- V1 la prova della farina (forno a legna di Rosa)
def v1():
    parti = [
        # il forno: base di mattoni, cupola, bocca con l'arco e la pietra; dentro la brace (si accende col calore)
        P("base", "cubo", "pietra", "#8a5a42", y=0.42, sx=1.5, sy=0.84, sz=1.3),
        P("cupola", "sfera", "pietra", "#9a6a4e", y=0.95, z=0.1, sx=1.36, sy=0.62, sz=1.16),
        P("pietra_forno", "cubo", "pietra", "#6a5a4c", y=0.86, z=-0.05, sx=0.9, sy=0.03, sz=0.7),
        P("bocca_arco", "cubo", "pietra", "#5a3a2a", y=1.06, z=-0.56, sx=0.62, sy=0.08, sz=0.12),
        P("bocca_sx", "cubo", "pietra", "#5a3a2a", x=-0.33, y=0.95, z=-0.56, sx=0.06, sy=0.22, sz=0.12),
        P("bocca_dx", "cubo", "pietra", "#5a3a2a", x=0.33, y=0.95, z=-0.56, sx=0.06, sy=0.22, sz=0.12),
        P("bocca_buio", "cubo", "pietra", "#120c08", y=0.95, z=-0.5, sx=0.6, sy=0.2, sz=0.02),
        P("brace", "cilindro", "cera", "#d2561c", x=-0.15, y=0.89, z=0.15, sx=0.34, sy=0.02, sz=0.26),
        P("fiamma", "sfera", "cera", "#f0a040", x=-0.15, y=0.96, z=0.15, sx=0.12, sy=0.12, sz=0.1),
        # la serranda di ferro (aperta = il calore scappa) e la catasta di legna
        P("serranda", "cubo", "metallo", "#2e2a26", x=0.0, y=0.95, z=-0.66, sx=0.56, sy=0.24, sz=0.02, ry=0, clic=True, azione="serranda",
          msg="La serranda di ferro della bocca: chiusa tiene il calore, aperta lo lascia scappare."),
        P("serranda_maniglia", "cubo", "metallo", "#1e1a16", x=0.0, y=0.95, z=-0.68, sx=0.12, sy=0.02, sz=0.02, genitore="serranda"),
        P("catasta", "cubo", "legno", "#6a4a2c", x=1.05, y=0.2, z=-0.2, sx=0.45, sy=0.4, sz=0.5, clic=True, azione="legna",
          msg="Ciocchi di quercia, secchi. Ognuno alza il calore del forno."),
        P("ciocco_a", "cilindro", "legno", "#5a3d22", x=1.0, y=0.45, z=-0.35, rz=90, sx=0.09, sy=0.22, sz=0.09),
        P("ciocco_b", "cilindro", "legno", "#4e3420", x=1.12, y=0.45, z=-0.15, rz=90, ry=12, sx=0.08, sy=0.21, sz=0.08),
        # il tavolo della farina con la madia, la pala e le pagnotte crude
    ]
    parti += tavolo(0.9, 0.55, 0.78, cx=-1.25, cz=-0.35, pre="tavolo_")
    parti += [
        P("sacchetto_farina", "cilindro", "stoffa", "#e0d6c0", x=-1.45, y=0.9, z=-0.42, sx=0.16, sy=0.12, sz=0.16, clic=True, azione="farina",
          msg="Farina di grano. Rosa ne prende un pizzico fra le dita e lo getta sulla pietra del forno: e' cosi' che il forno si legge."),
        P("pizzico_bianco", "cilindro", "gesso", "#efe9da", x=0.12, y=0.885, z=-0.2, sx=0.12, sy=0.004, sz=0.1, nascosta=True),
        P("pizzico_nocciola", "cilindro", "cera", "#a9743e", x=0.12, y=0.885, z=-0.2, sx=0.12, sy=0.004, sz=0.1, nascosta=True),
        P("pizzico_nero", "cilindro", "metallo", "#1a1410", x=0.12, y=0.885, z=-0.2, sx=0.12, sy=0.004, sz=0.1, nascosta=True),
        P("pala_manico", "cilindro", "legno", "#7a5a3a", x=-1.05, y=0.84, z=-0.48, rz=90, ry=-8, sx=0.03, sy=0.45, sz=0.03),
        P("pala", "cubo", "legno", "#8a6a44", x=-1.42, y=0.81, z=-0.25, sx=0.3, sy=0.012, sz=0.3, clic=True, azione="inforna",
          msg="La pala con le pagnotte: si infornano quando il forno e' giusto."),
        P("pane_crudo", "sfera", "cera", "#e8dcc0", x=-1.42, y=0.85, z=-0.25, sx=0.2, sy=0.08, sz=0.16),
        P("pane_forno", "sfera", "cera", "#d8b880", x=0.05, y=0.92, z=0.05, sx=0.2, sy=0.08, sz=0.16, nascosta=True, clic=True, azione="sforna",
          msg="Il pane e' dentro: si sforna quando la clessidra e' vuota, non prima e non troppo dopo."),
        P("pane_cotto", "sfera", "cera", "#b5793a", x=-1.1, y=0.83, z=-0.2, sx=0.2, sy=0.09, sz=0.16, nascosta=True),
        P("pane_cotto_b", "sfera", "cera", "#a86e34", x=-1.3, y=0.83, z=-0.4, sx=0.18, sy=0.085, sz=0.15, nascosta=True),
        # la clessidra di Rosa (la sabbia di sopra cala mentre il pane cuoce)
        P("clessidra_telaio", "cubo", "legno", "#4a3220", x=-0.95, y=0.95, z=-0.05, sx=0.1, sy=0.01, sz=0.1),
        P("clessidra_vetro", "capsula", "vetro", "#c8d0c8", x=-0.95, y=0.88, z=-0.05, sx=0.07, sy=0.07, sz=0.07),
        P("clessidra_sopra", "cilindro", "gesso", "#d9c48c", x=-0.95, y=0.9, z=-0.05, sx=0.05, sy=0.02, sz=0.05),
        P("clessidra_sotto", "cilindro", "gesso", "#d9c48c", x=-0.95, y=0.82, z=-0.05, sx=0.05, sy=0.002, sz=0.05),
        P("clessidra_base", "cubo", "legno", "#4a3220", x=-0.95, y=0.785, z=-0.05, sx=0.1, sy=0.01, sz=0.1),
    ]
    return {
        "id": "enigma_forno", "tipo": "quiz", "meccanismo": "forno", "titolo": "La prova della farina", "scena": MOD,
        "consegna": "Rosa deve infornare il pane e ti chiede una mano. Nel suo forno non c'e' un termometro: si getta un pizzico di farina sulla pietra. Se resta bianca il forno e' freddo, se diventa nera brucia; se si fa color nocciola in un respiro, e' giusto. Regola il calore con la legna e con la serranda, inforna, e sforna quando la clessidra e' vuota.",
        "temaCivico": "Accogliere e' anche lavoro di tutti i giorni: le famiglie che ospitavano i bambini dividevano pane, letti e fatica (art. 2 della Costituzione: solidarieta').",
        "indizi": ["Il forno si legge con la farina: tocca il sacchetto e guarda di che colore diventa il pizzico sulla pietra.",
                   "Bianca = freddo, aggiungi legna. Nera = troppo caldo, apri la serranda e aspetta. Nocciola = giusto: inforna subito.",
                   "Legna fino al nocciola, serranda chiusa, pala nel forno; poi aspetta che la sabbia scenda tutta e tocca il pane."],
        "spiegazione": "Le pagnotte escono dorate, con la crosta che canta quando si raffredda. Rosa te ne mette una calda fra le mani, avvolta in un tovagliolo: «Questa portala ad Alcide, che quando lavora si scorda di mangiare.»",
        "erroreMsg": "",
        "scheda": "Nelle case di campagna il forno a legna si scaldava una volta per tutta la settimana. Nel romanzo Rosa ha un forno; la prova della farina e' un vecchio sistema dei fornai, qui usato per il gioco.",
        "appunto": "Il forno di Rosa: farina nocciola = forno giusto.",
        "x": POS_V1[0], "y": POS_V1[1], "z": POS_V1[2], "yaw": 180, "scala": 1, "distanza": 2.3, "inclina": 18,
        "prompt": "il forno di Rosa",
        "produce": ["pane_alcide"],
        "forno": {"inizio": 110, "giusto0": 220, "giusto1": 265, "massimo": 340, "legna": 42, "raffredda": 1.2, "serranda": 7.0, "cottura": 12, "presto": 2.5, "tardi": 4.0},
        "strumenti": [
            "Bianca. La farina resta bianca: il forno e' ancora freddo.",                                          # 0
            "Nocciola, in un respiro: il forno e' giusto. Inforna adesso.",                                         # 1
            "Nera e fumante: il forno brucia. Apri la serranda e lascia scappare un po' di calore.",               # 2
            "Un ciocco nel fuoco: la fiamma si alza.",                                                              # 3
            "Serranda aperta: il calore esce dalla bocca.",                                                          # 4
            "Serranda chiusa.",                                                                                      # 5
            "Rosa ti ferma la mano: «Prima leggi il forno con la farina, poi inforni.»",                            # 6
            "Rosa scuote la testa: «Con questo forno il pane non cuoce, o si brucia. Rileggilo con la farina.»",     # 7
            "Le pagnotte sono dentro. Ora conta la sabbia della clessidra.",                                        # 8
            "Ancora pallido: Rosa lo rimette dentro con la pala. «Pazienza, che il pane non ha fretta.»",           # 9
            "Troppo tardi: sotto e' nero. Rosa ride: «Questo lo mangiano le galline. Ne inforniamo un altro.»",     # 10
            "La serranda chiusa per infornare: il forno tiene il calore.",                                          # 11
        ],
        "parti": parti,
        "domande": [
            quiz("Come si capisce se il forno e' pronto?", ["Un pizzico di farina diventa nocciola in un respiro", "Si mette la mano dentro", "Si conta fino a cento", "Quando esce fumo nero"], 0, "La farina e' il termometro dei fornai di una volta."),
            quiz("Il pizzico diventa nero subito. Che cosa fai?", ["Apro la serranda e aspetto", "Aggiungo legna", "Inforno lo stesso", "Butto acqua sul fuoco"], 0, "Il forno e' troppo caldo: si lascia scappare il calore."),
            quiz("Quando si sforna?", ["Quando la clessidra e' vuota", "Subito", "Il giorno dopo", "Quando lo dice il gatto"], 0, "Ne' prima (pallido) ne' troppo dopo (bruciato)."),
        ],
    }


# ---------------------------------------------------------------- V2 gli attrezzi che suonano (banco dell'accordatore)
ATTREZZI = [
    # id, nome, tipo di suono, descrizione dell'onda (alternativa visiva), messaggio
    ("diapason", "il diapason", "diapason", "un'onda lunga e liscia, tutta uguale, che si spegne piano piano",
     "Una forchetta d'acciaio a due rebbi: battuta sul ginocchio canta sempre la stessa nota, il La."),
    ("chiave", "la chiave d'accordatura", "chiave", "una fila di scatti piccoli e regolari, come denti di un pettine",
     "La chiave a T per girare le caviglie del pianoforte: fa tic-tic-tic quando gira."),
    ("feltri", "i feltri", "feltro", "una gobba sola, bassa e morbida, che finisce subito",
     "Cunei di feltro per zittire le corde vicine: se ci batti sopra fanno solo un tonfo sordo."),
    ("martelletto", "il martelletto", "martelletto", "un picco alto e secco che poi scende veloce",
     "Un martelletto del pianoforte, con la testa di feltro duro: colpisce la corda e la fa suonare."),
    ("corda", "la corda", "corda", "un'onda che trema e ronza, larga all'inizio e poi sempre piu' stretta",
     "Una corda d'acciaio arrotolata: pizzicata ronza a lungo."),
]


def v2():
    parti = tavolo(1.6, 0.7, 0.86, "#5a4030", "#3e2c1e", cz=0.1, pre="banco_")
    parti += [
        P("panno_banco", "cubo", "stoffa", "#4a3a2c", y=0.863, z=0.1, sx=1.5, sy=0.004, sz=0.62),
        # la lavagnetta dove Alcide disegna i suoni (le onde le disegna il codice sopra questa tavoletta)
        P("lavagnetta", "cubo", "legno", "#1e221e", y=1.32, z=0.42, sx=1.2, sy=0.5, sz=0.02),
        P("lavagnetta_cornice", "cubo", "legno", "#5a4030", y=1.32, z=0.43, sx=1.26, sy=0.56, sz=0.015),
        P("onde", "vuoto", "legno", "", y=1.32, z=0.4, sx=1.1, sy=0.4, sz=0.01),
        T("lavagnetta_titolo", "COME SUONA", -0.42, 1.55, 0.405, 0.3, 0.04, 0.022, "#d8d0bc", "mano"),
    ]
    xs = [-0.6, -0.3, 0.0, 0.3, 0.6]
    for i, (id, nome, tipo, onda, msg) in enumerate(ATTREZZI):
        x = xs[i]
        g = dict(clic=True, azione="attrezzo", valore=i, msg=msg)
        parti.append(P(id, "vuoto", "legno", "", x=x, y=0.92, z=-0.05, sx=0.22, sy=0.12, sz=0.3, **g))
        G = dict(genitore=id)
        if id == "diapason":
            parti += [P("diapason_gambo", "cilindro", "argento", "#c8c8c0", z=0.06, rx=90, sx=0.012, sy=0.05, sz=0.012, **G),
                      P("diapason_rebbio_a", "cubo", "argento", "#c8c8c0", x=-0.012, z=-0.05, sx=0.008, sy=0.008, sz=0.12, **G),
                      P("diapason_rebbio_b", "cubo", "argento", "#c8c8c0", x=0.012, z=-0.05, sx=0.008, sy=0.008, sz=0.12, **G)]
        elif id == "chiave":
            parti += [P("chiave_asta", "cilindro", "metallo", "#3a3632", rx=90, sx=0.018, sy=0.08, sz=0.018, **G),
                      P("chiave_manico", "cilindro", "legno", "#5a3a22", z=0.08, rz=90, sx=0.03, sy=0.07, sz=0.03, **G)]
        elif id == "feltri":
            for k in range(3):
                parti.append(P("feltro_%d" % k, "cubo", "stoffa", "#b9a988", x=-0.05 + k * 0.05, y=-0.02, z=k * 0.03, ry=k * 12, sx=0.04, sy=0.03, sz=0.08, **G))
        elif id == "martelletto":
            parti += [P("martelletto_asta", "cilindro", "legno", "#8a6a44", rx=90, sx=0.01, sy=0.09, sz=0.01, **G),
                      P("martelletto_testa", "capsula", "stoffa", "#e0d6c0", z=-0.09, sx=0.04, sy=0.025, sz=0.025, rz=90, **G)]
        else:
            parti += [P("corda_rotolo", "cilindro", "ottone", "#b59a3f", y=-0.03, sx=0.12, sy=0.006, sz=0.12, **G),
                      P("corda_capo", "cilindro", "ottone", "#b59a3f", x=0.07, y=-0.03, z=-0.04, rx=90, ry=30, sx=0.004, sy=0.06, sz=0.004, **G)]
        parti.append(T("etichetta_" + id, str(i + 1), x, 0.868, -0.21, 0.04, 0.04, 0.02, "#d8d0bc", "mano", rx=90))
    suoni = [{"attrezzo": id, "nome": nome, "tipo": tipo, "onda": onda} for id, nome, tipo, onda, msg in ATTREZZI]
    # l'ordine dei suoni da riconoscere (Alcide li fa sentire uno alla volta, dietro la schiena)
    ordine = ["martelletto", "diapason", "chiave", "corda", "feltri"]
    suoni = sorted(suoni, key=lambda s: ordine.index(s["attrezzo"]))
    oks = {
        "martelletto": "«Bravo. Il martelletto: e' lui che fa suonare il pianoforte, non le dita.»",
        "diapason": "«Il diapason. Lui non sbaglia mai: e' il La di tutto il mondo.»",
        "chiave": "«La chiave. Tic, tic: ogni scatto e' un capello di nota.»",
        "corda": "«La corda. Senti come ronza? Ha ancora voglia di cantare.»",
        "feltri": "«I feltri: il suono del silenzio. Anche quello serve, a un accordatore.»",
    }
    for s in suoni:
        s["ok"] = oks[s["attrezzo"]]
    return {
        "id": "enigma_attrezzi", "tipo": "quiz", "meccanismo": "suoni", "titolo": "Gli attrezzi che suonano", "scena": MOD,
        "consegna": "Alcide prende il pane, poi ti mette alla prova: tiene un attrezzo dietro la schiena e lo fa suonare. Premi Ascolta, guarda l'onda che compare sulla lavagnetta e tocca sul banco l'attrezzo che fa quel suono. Anche gli attrezzi, se li tocchi, suonano e disegnano la loro onda.",
        "temaCivico": "Imparare un mestiere guardando e ascoltando chi lo sa fare: anche questa e' istruzione (art. 34) e cultura (art. 9).",
        "indizi": ["Ascolta piu' volte, poi tocca gli attrezzi uno per uno: ognuno disegna la sua onda. Confronta i disegni.",
                   "Onda liscia e lunghissima = diapason; scatti in fila = chiave; gobba bassa e corta = feltri; picco secco = martelletto; onda che trema e ronza = corda.",
                   "L'ordine di Alcide: martelletto, diapason, chiave, corda, feltri."],
        "spiegazione": "Cinque suoni, cinque attrezzi. Alcide si toglie il cappello e ti guarda come si guarda una cosa rara: «Tu hai l'orecchio, Amerigo. E non lo sapevi.» Ti mette in mano il diapason.",
        "erroreMsg": "",
        "scheda": "Nel romanzo Alcide Benvenuti e' un accordatore di pianoforti e scopre che Amerigo ha orecchio. Le prove con gli attrezzi sono inventate per il gioco. L'onda disegnata accanto a ogni suono serve a chi sente poco: il gioco si puo' risolvere anche solo con gli occhi.",
        "appunto": "Il diapason di Alcide: il La di tutto il mondo.",
        "x": POS_V2[0], "y": POS_V2[1], "z": POS_V2[2], "yaw": 90, "scala": 1, "distanza": 2.0, "inclina": 14,
        "prompt": "il banco dell'accordatore",
        "richiede": ["pane_alcide"], "bloccatoMsg": "Alcide e' chino sul banco e non alza la testa. Rosa dalla porta: «Prima gli porti il pane, che se no non ti sente nemmeno.»",
        "produce": ["diapason"],
        "suoni": suoni,
        "strumenti": [
            "Ascolta",                                                                                     # 0 pulsante
            "Alcide fa suonare l'attrezzo dietro la schiena. Sulla lavagnetta: {0}.",                        # 1
            "Questo e' {0}, e suona cosi': {1}. Non e' quello che hai sentito: riascolta e confronta.",      # 2
            "Prima premi Ascolta: Alcide deve ancora far suonare qualcosa.",                                # 3
            "Ancora {0}.",                                                                                   # 4
        ],
        "parti": parti,
        "domande": [
            quiz("Quale attrezzo canta sempre la stessa nota, il La?", ["Il diapason", "La chiave", "I feltri", "Il martelletto"], 0, "Il diapason serve a dare la nota giusta."),
            quiz("Che cosa fa tic-tic-tic quando gira?", ["La chiave d'accordatura", "La corda", "Il diapason", "I feltri"], 0, "La chiave gira le caviglie del pianoforte."),
            quiz("Che cosa fa suonare la corda del pianoforte?", ["Il martelletto", "Il feltro", "La chiave", "Il diapason"], 0, "I tasti muovono i martelletti, che colpiscono le corde."),
        ],
    }


# ---------------------------------------------------------------- V3 il pianoforte stonato (battimenti)
CORDE = [
    # id, nome, nota (midi), scarto iniziale (Hz), x sul pianoforte
    ("corda_la", "il La", 69, 3.0, -0.12),
    ("corda_re", "il Re", 62, -2.5, 0.0),
    ("corda_mi", "il Mi", 64, 4.0, 0.12),
]


def v3():
    parti = [
        P("piano_cassa", "cubo", "legno", "#2a1a12", y=0.65, z=0.12, sx=1.45, sy=1.3, sz=0.4),
        P("piano_tastiera", "cubo", "legno", "#3a2418", y=0.72, z=-0.18, sx=1.4, sy=0.08, sz=0.28),
        P("piano_tasti", "cubo", "gesso", "#e8e0cc", y=0.765, z=-0.2, sx=1.3, sy=0.012, sz=0.2),
        P("piano_gamba_sx", "cubo", "legno", "#2a1a12", x=-0.62, y=0.36, z=-0.26, sx=0.06, sy=0.72, sz=0.06),
        P("piano_gamba_dx", "cubo", "legno", "#2a1a12", x=0.62, y=0.36, z=-0.26, sx=0.06, sy=0.72, sz=0.06),
        # il coperchio aperto: si vedono il telaio, le caviglie e le corde
        P("piano_telaio", "cubo", "ottone", "#a88a3e", y=1.02, z=-0.08, sx=1.2, sy=0.42, sz=0.01),
        P("piano_coperchio", "cubo", "legno", "#2a1a12", y=1.38, z=0.0, rx=-24, sx=1.45, sy=0.02, sz=0.42),
        P("diapason_banco", "cubo", "argento", "#c8c8c0", x=-0.52, y=0.79, z=-0.22, sx=0.012, sy=0.012, sz=0.14, ry=20,
          msg="Il diapason di Alcide, sul leggio: canta il La giusto."),
        # la lancetta: Alcide la appoggia al telaio, oscilla con il battimento (alternativa visiva)
        P("quadrante", "cilindro", "carta", "#e3d9bd", x=0.42, y=1.08, z=-0.1, rx=90, sx=0.18, sy=0.004, sz=0.18),
        T("quadrante_scritta", "UA-UA", 0.42, 1.0, -0.106, 0.12, 0.03, 0.016, "#3b2a1a"),
        P("lancetta", "cubo", "metallo", "#7a1f1a", x=0.42, y=1.12, z=-0.108, sx=0.008, sy=0.075, sz=0.004),
        P("perno", "cilindro", "ottone", "#b59a3f", x=0.42, y=1.08, z=-0.11, rx=90, sx=0.018, sy=0.004, sz=0.018),
        # la chiave infilata nella caviglia (giri a sinistra e a destra)
        P("chiave_su_caviglia", "cilindro", "metallo", "#3a3632", x=-0.25, y=1.2, z=-0.14, rx=90, sx=0.02, sy=0.06, sz=0.02),
        P("giro_sinistra", "cubo", "legno", "#5a3a22", x=-0.36, y=1.2, z=-0.2, sx=0.12, sy=0.03, sz=0.03, clic=True, azione="giro", valore=-1,
          msg="Mezzo scatto della chiave verso sinistra."),
        P("giro_destra", "cubo", "legno", "#5a3a22", x=-0.14, y=1.2, z=-0.2, sx=0.12, sy=0.03, sz=0.03, clic=True, azione="giro", valore=1,
          msg="Mezzo scatto della chiave verso destra."),
        T("giro_sx_freccia", "<", -0.36, 1.2, -0.218, 0.05, 0.03, 0.03, "#e8e0cc"),
        T("giro_dx_freccia", ">", -0.14, 1.2, -0.218, 0.05, 0.03, 0.03, "#e8e0cc"),
    ]
    for i, (id, nome, nota, scarto, x) in enumerate(CORDE):
        parti.append(P(id, "cilindro", "metallo", "#8a8a84", x=x, y=1.02, z=-0.1, sx=0.008, sy=0.2, sz=0.008, clic=True, azione="corda", valore=i,
                       msg="La corda del %s." % nome.split(" ")[1]))
        parti.append(P(id + "_feltro", "cubo", "stoffa", "#b9a988", x=x + 0.05, y=0.88, z=-0.1, sx=0.03, sy=0.03, sz=0.04, nascosta=True))
        parti.append(T(id + "_nome", nome.split(" ")[1].upper(), x, 1.26, -0.106, 0.08, 0.03, 0.016, "#3b2a1a", "mano"))
    corde = [{"id": id, "nome": nome, "nota": nota, "scarto": scarto, "passo": 0.5, "dopo": id + "_feltro"} for id, nome, nota, scarto, x in CORDE]
    return {
        "id": "enigma_pianoforte", "tipo": "quiz", "meccanismo": "accordatura", "titolo": "Il pianoforte stonato", "scena": MOD,
        "consegna": "Tre corde del pianoforte sono stonate. Scegli una corda, premi Ascolta: senti la corda e il diapason insieme. Finche' sono diversi fanno «ua-ua-ua» e la lancetta sul telaio trema. Gira la chiave a mezzi scatti: se il battimento rallenta sei sulla strada giusta; quando sparisce e la lancetta sta ferma, la corda e' accordata.",
        "temaCivico": "Il talento e' di tutti, ma va coltivato: la Repubblica promuove lo sviluppo della cultura (art. 9) e chi e' capace e meritevole ha diritto di arrivare in alto negli studi (art. 34).",
        "indizi": ["Non cercare la nota: ascolta l'onda, il «ua-ua». O guarda la lancetta: trema tanto quanto e' veloce il battimento.",
                   "Se dopo uno scatto il battimento accelera, giri dalla parte sbagliata: torna indietro. Continua finche' sparisce.",
                   "La: sei mezzi scatti verso sinistra. Re: cinque verso destra. Mi: otto verso sinistra."],
        "spiegazione": "Le tre corde cantano pulite insieme al diapason. Alcide suona un accordo, lo lascia andare fino in fondo e non dice niente per un po'. Poi: «Prima di suonare, ascolta come suona la stanza. Tu lo fai gia'. A te ci vuole un violino.»",
        "erroreMsg": "",
        "scheda": "Quando due suoni sono quasi uguali si sente un'onda lenta, il battimento: gli accordatori lo usano per trovare la nota giusta. Nel gioco la lancetta mostra il battimento anche a chi non lo sente.",
        "appunto": "Accordare: il battimento rallenta, poi sparisce.",
        "x": POS_V3[0], "y": POS_V3[1], "z": POS_V3[2], "yaw": 90, "scala": 1, "distanza": 1.9, "inclina": 12,
        "prompt": "il pianoforte aperto",
        "richiede": ["diapason"], "bloccatoMsg": "Il pianoforte e' aperto, ma per accordarlo serve il diapason di Alcide: e' sul suo banco.",
        "produce": ["violino_promesso"],
        "corde": corde,
        "strumenti": [
            "Ascolta",                                                                         # 0 pulsante
            "Gira a sinistra",                                                                 # 1
            "Gira a destra",                                                                   # 2
            "Il battimento rallenta: sei sulla strada giusta.",                                # 3
            "Il battimento accelera: giri dalla parte sbagliata.",                             # 4
            "Il «ua-ua» e' sparito e la lancetta sta ferma: {0} e' accordato.",                 # 5
            "Scegli prima una corda da accordare.",                                            # 6
            "Questa corda e' gia' accordata: scegline un'altra.",                              # 7
            "Hai scelto la corda del {0}. Premi Ascolta.",                                     # 8
        ],
        "parti": parti,
        "domande": [
            quiz("Che cos'e' il battimento?", ["L'onda lenta fra due suoni quasi uguali", "Il rumore del martello", "Una nota alta", "Il suono del diapason"], 0, "Due suoni quasi uguali fanno ua-ua."),
            quiz("Giri la chiave e il battimento accelera. Che fai?", ["Giro dall'altra parte", "Continuo cosi'", "Smetto", "Cambio pianoforte"], 0, "Piu' il battimento e' veloce, piu' la corda e' lontana."),
            quiz("Quando la corda e' accordata?", ["Quando il battimento sparisce", "Quando suona forte", "Quando e' tesa al massimo", "Mai"], 0, "Senza battimento le due note sono uguali."),
        ],
    }


# ---------------------------------------------------------------- V4 le impronte nella neve
RAGAZZI = [
    # nome, misura (numero di scarpa), chiodi sulla suola, tacco
    ("Rivo", 33, 12, "rattoppato"),
    ("Luzio", 35, 12, "intero"),
    ("Nario", 31, 8, "intero"),
]


def scarpone(id, idx, x, z, misura, chiodi, tacco, nome):
    L = 0.12 + (misura - 30) * 0.008
    g = dict(clic=True, azione="scarpone", valore=idx, msg="Gli scarponi di %s: numero %d, %d chiodi sulla suola, tacco %s." % (nome, misura, chiodi, tacco))
    parti = [P(id, "vuoto", "cuoio", "", x=x, y=0.06, z=z, sx=0.12, sy=0.1, sz=L + 0.04, **g)]
    G = dict(genitore=id)
    parti.append(P(id + "_tomaia", "scarpa", "cuoio", "#3a2418", sx=L * 5.2, sy=L * 5.2, sz=L * 5.2, ry=-90, testo="scarponi", **G))
    parti.append(T(id + "_nome", nome.upper(), 0, -0.05, -L / 2 - 0.04, 0.12, 0.03, 0.02, "#e8e0cc", "mano", rx=60, **G))
    if tacco == "rattoppato":
        parti.append(P(id + "_toppa", "cubo", "cuoio", "#7a5a3a", y=-0.03, z=L * 0.38, sx=0.05, sy=0.012, sz=0.04, **G))
    return parti


def orma(id, idx, x, z, misura, chiodi, tacco, ry=0, nascosta=False, gruppo="", clic=True, msg="", evento=""):
    """Impronta di uno scarpone nella neve: pianta (ovale scuro), chiodi (puntini) e tacco (rattoppato = segno a croce)."""
    L = 0.12 + (misura - 30) * 0.008
    d = dict(ry=ry)
    if nascosta: d["nascosta"] = True
    if gruppo: d["gruppo"] = gruppo
    g = dict(clic=True, azione="orma", valore=idx, msg=msg, evento=evento) if clic else {}
    parti = [P(id, "vuoto", "gesso", "", x=x, y=0.012, z=z, sx=0.1, sy=0.02, sz=L + 0.02, **d, **g)]
    G = dict(genitore=id)
    if nascosta: G["nascosta"] = True
    if gruppo: G["gruppo"] = gruppo
    parti.append(P(id + "_pianta", "cubo", "gesso", "#b8bec4", y=-0.004, sx=0.065, sy=0.004, sz=L * 0.66, **G))
    parti.append(P(id + "_tacco", "cubo", "gesso", "#aab0b6", y=-0.004, z=L * 0.4, sx=0.05, sy=0.004, sz=L * 0.22, **G))
    n = min(chiodi, 12)
    for k in range(n // 2):
        for s in (-1, 1):
            parti.append(P(id + "_c%d%s" % (k, "a" if s < 0 else "b"), "cubo", "metallo", "#5a5e62", x=s * 0.022, y=-0.001,
                           z=-L * 0.3 + k * (L * 0.5 / max(1, n // 2 - 1)), sx=0.006, sy=0.002, sz=0.006, **G))
    if tacco == "rattoppato":
        parti.append(P(id + "_croce_a", "cubo", "metallo", "#4a4e52", y=-0.001, z=L * 0.4, ry=45, sx=0.045, sy=0.002, sz=0.006, **G))
        parti.append(P(id + "_croce_b", "cubo", "metallo", "#4a4e52", y=-0.001, z=L * 0.4, ry=-45, sx=0.045, sy=0.002, sz=0.006, **G))
    return parti


def v4():
    parti = [
        # il diorama: la neve della corte fra la porta del laboratorio (a destra) e la strada della scuola (in fondo)
        P("neve", "cubo", "gesso", "#e8ecef", y=-0.02, sx=2.4, sy=0.04, sz=1.6),
        P("porta_laboratorio", "cubo", "legno", "#4a3220", x=1.15, y=0.15, z=-0.62, sx=0.06, sy=0.3, sz=0.22),
        T("porta_laboratorio_scritta", "LABORATORIO", 1.06, 0.33, -0.62, 0.2, 0.03, 0.016, "#3b2a1a", "mano", ry=-90),
        P("pupazzo_a", "sfera", "gesso", "#eef0f2", x=-0.85, y=0.06, z=-0.45, sx=0.12, sy=0.12, sz=0.12),
        P("pupazzo_b", "sfera", "gesso", "#eef0f2", x=-0.85, y=0.15, z=-0.45, sx=0.08, sy=0.08, sz=0.08),
        P("pupazzo_naso", "cilindro", "legno", "#b8662c", x=-0.85, y=0.15, z=-0.5, rx=90, sx=0.01, sy=0.02, sz=0.01),
        T("strada_scritta", "VERSO LA SCUOLA", 0.0, 0.002, 0.74, 0.4, 0.04, 0.022, "#5a5e62", "mano", rx=90),
        # la panca con gli scarponi dei tre ragazzi (davanti alla stufa ad asciugare)
        P("panca", "cubo", "legno", "#5a4030", x=-0.95, y=0.08, z=-0.72, sx=0.5, sy=0.04, sz=0.16),
    ]
    for i, (nome, mis, ch, tacco) in enumerate(RAGAZZI):
        parti += scarpone("scarpone_%s" % nome.lower(), i, -1.1 + i * 0.15, -0.72, mis, ch, tacco, nome)
    # le orme davanti alla porta del laboratorio (di Rivo: e' lui che ha preso il violino) e le orme di tutti vicino al pupazzo di neve
    parti += orma("orma_porta", 0, 0.85, -0.5, 33, 12, "rattoppato", ry=-60, msg="Le impronte che escono dalla porta del laboratorio, fresche. Di chi sono?", evento="porta")
    parti += orma("orma_luzio", 1, -0.55, -0.35, 35, 12, "intero", ry=20, msg="Impronte grandi vicino al pupazzo di neve, di questa mattina.")
    parti += orma("orma_nario", 2, -0.62, -0.18, 31, 8, "intero", ry=-10, msg="Impronte piccole vicino al pupazzo di neve.")
    # la pista: si rivela quando si riconoscono le orme della porta; a ogni bivio due tracce, una sola e' quella giusta
    passi = [
        # (x, z dei due rami), ramo giusto (0/1), messaggi del ramo sbagliato
        ((0.55, -0.2), (0.95, -0.05), 0, "Qui le orme sono di un gatto: quattro zampe piccole, niente chiodi."),
        ((0.15, 0.05), (0.62, 0.3), 1, "Queste sono orme grandi senza chiodi: la maestra, stamattina, con gli stivali."),
        ((-0.35, 0.45), (0.1, 0.6), 0, "Qui la neve e' liscia: nessuno e' passato."),
    ]
    prima = "g:pista_1"
    for k, (a, b, giusto, sbagliato) in enumerate(passi):
        grp = "pista_%d" % (k + 1)
        for j, (x, z) in enumerate((a, b)):
            ok = j == giusto
            idr = "ramo_%d_%s" % (k + 1, "ab"[j])
            dopo = ("g:pista_%d" % (k + 2)) if k + 1 < len(passi) else "legnaia_porta_aperta,violino_custodia,violino_corpo,violino_manico,g:pista_fine"
            parti.append(P(idr, "vuoto", "gesso", "", x=x, y=0.012, z=z, sx=0.22, sy=0.03, sz=0.22, nascosta=True, gruppo=grp, clic=True, azione="ramo", valore=k,
                           evento="giusto" if ok else "", dopo=dopo if ok else "", msg=("" if ok else sbagliato)))
            # le orme del ramo (giusto: tacco rattoppato; sbagliato: altre)
            if ok:
                parti += orma(idr + "_o1", 0, x - 0.04, z - 0.05, 33, 12, "rattoppato", ry=10, nascosta=True, gruppo=grp, clic=False)
                parti += orma(idr + "_o2", 0, x + 0.04, z + 0.06, 33, 12, "rattoppato", ry=10, nascosta=True, gruppo=grp, clic=False)
            else:
                parti.append(P(idr + "_segno", "cubo", "gesso", "#c4c9ce", x=x, y=0.002, z=z, sx=0.06, sy=0.004, sz=0.06, ry=30, nascosta=True, gruppo=grp))
    # la legnaia della scuola, in fondo a sinistra, e il violino nascosto fra i ciocchi
    parti += [
        P("legnaia", "cubo", "legno", "#5a4030", x=-0.75, y=0.14, z=0.66, sx=0.5, sy=0.28, sz=0.2),
        P("legnaia_tetto", "cubo", "legno", "#3e2c1e", x=-0.75, y=0.3, z=0.66, rx=-12, sx=0.56, sy=0.02, sz=0.26),
        P("legnaia_porta", "cubo", "legno", "#4a3220", x=-0.75, y=0.13, z=0.55, sx=0.2, sy=0.22, sz=0.012),
        P("legnaia_porta_aperta", "cubo", "legno", "#4a3220", x=-0.6, y=0.13, z=0.48, ry=-70, sx=0.2, sy=0.22, sz=0.012, nascosta=True),
        P("violino_custodia", "cubo", "cuoio", "#2a1a12", x=-0.75, y=0.07, z=0.58, sx=0.3, sy=0.04, sz=0.1, nascosta=True),
        P("violino_corpo", "sfera", "legno", "#8a4a1e", x=-0.78, y=0.1, z=0.58, sx=0.14, sy=0.025, sz=0.08, nascosta=True),
        P("violino_manico", "cubo", "legno", "#2a1a12", x=-0.65, y=0.1, z=0.58, sx=0.12, sy=0.012, sz=0.02, nascosta=True),
    ]
    impronte = [{"nome": n, "misura": m, "chiodi": c, "tacco": t} for n, m, c, t in RAGAZZI]
    return {
        "id": "enigma_impronte", "tipo": "quiz", "meccanismo": "impronte", "titolo": "Le impronte nella neve", "scena": MOD,
        "consegna": "Il violino che Alcide ti ha preparato non e' piu' sul banco. Fuori, nella neve, ci sono impronte fresche. Tu le scarpe le conosci tutte: tocca uno scarpone dei ragazzi, poi un'impronta, e confrontali (numero, chiodi, tacco). Quando sai di chi sono le orme che escono dal laboratorio, seguile: a ogni bivio tocca la traccia giusta.",
        "temaCivico": "Nessuno e' di troppo: la gelosia verso chi arriva da lontano si scioglie conoscendosi. La Costituzione (art. 3) chiede pari dignita' senza distinzioni di provenienza.",
        "indizi": ["Tocca uno scarpone e poi le orme davanti alla porta: il gioco ti dice che cosa combacia e che cosa no.",
                   "Tre cose da guardare: il numero (la lunghezza), i chiodi sulla suola, il tacco. Uno dei tre ha il tacco rattoppato.",
                   "Le orme della porta sono di Rivo (tacco rattoppato). Poi: primo bivio a sinistra, secondo a destra, terzo a sinistra, fino alla legnaia della scuola."],
        "spiegazione": "Dietro la legnaia della scuola, avvolto in una coperta, c'e' il violino. Rivo e' li', con il naso rosso e gli occhi bassi. Non scappa.",
        "erroreMsg": "",
        "scheda": "Nel romanzo uno dei tre ragazzi Benvenuti all'inizio e' geloso di Amerigo; le fonti non dicono quale: nel gioco e' Rivo. Il violino sparito e le impronte sono inventati per il gioco. Amerigo che riconosce le scarpe di tutti viene dal ricordo delle scarpe.",
        "appunto": "Le orme col tacco rattoppato portavano alla legnaia della scuola.",
        "x": POS_V4[0], "y": POS_V4[1], "z": POS_V4[2], "yaw": 0, "scala": 1, "distanza": 2.4, "inclina": 48,
        "prompt": "le impronte nella neve",
        "richiede": ["violino_promesso"], "bloccatoMsg": "Impronte dappertutto nella neve della corte: i ragazzi ci hanno giocato tutta la mattina. Per ora non c'e' niente da cercare.",
        "produce": ["violino_ritrovato"],
        "cutscene": "cutscene_violino", "cutsceneCapitolo": "meta",
        "impronte": impronte,
        "strumenti": [
            "Ora tocca un'impronta nella neve per confrontarla.",                                   # 0
            "Prima tocca uno scarpone dei ragazzi, poi l'impronta.",                                  # 1
            "Combaciano: numero {1}, {2} chiodi, tacco {3}. Queste orme sono di {0}.",                # 2
            "Non combacia: lo scarpone ha il numero {0}, l'impronta e' piu' {1}.",                    # 3
            "Non combacia: lo scarpone ha {0} chiodi, nell'impronta ne conti {1}.",                   # 4
            "Non combacia: il tacco dello scarpone e' {0}, quello dell'impronta e' {1}.",             # 5
            "Ora segui le orme di {0}: a ogni bivio tocca la traccia giusta.",                       # 6
            "lunga", "corta",                                                                         # 7, 8
            "Prima scopri di chi sono le orme che escono dal laboratorio.",                          # 9
            "Le orme proseguono. Avanti.",                                                            # 10
        ],
        "parti": parti,
        "domande": [
            quiz("Come si riconosce un'impronta?", ["Dal numero, dai chiodi e dal tacco", "Dal colore della neve", "Dall'ora", "Non si puo'"], 0, "Ogni scarpa lascia la sua firma."),
            quiz("Le orme del laboratorio hanno il tacco rattoppato. Di chi sono?", ["Di Rivo", "Di Luzio", "Di Nario", "Di Alcide"], 0, "Solo Rivo ha il tacco rattoppato."),
            quiz("Dove portano le orme?", ["Alla legnaia della scuola", "Alla stazione", "Al forno", "Al fiume"], 0, "Il violino era nascosto nella legnaia."),
        ],
    }


# ---------------------------------------------------------------- V5 la prima melodia (ascolta e ripeti)
# melodia ORIGINALE, pentatonica di sol (sol, si, re, mi, la): nessuna musica del film ne' Bella ciao
NOTE = [  # midi, colore del segno sulla tastiera e sullo spartito, nome del colore
    (67, "#a3261c", "rosso"),
    (69, "#c88a2a", "giallo"),
    (71, "#3a6a3a", "verde"),
    (74, "#2e4a7a", "blu"),
    (76, "#6a3a7a", "viola"),
]
FRASI = [
    ([0, 2, 3, 2], 0.55, "Alcide annuisce: «Ancora una, piu' lunga.»"),
    ([3, 4, 3, 2, 1, 0], 0.5, "Alcide abbassa l'archetto e sorride sotto i baffi."),
]


def v5():
    parti = tavolo(1.4, 0.7, 0.78, "#5a4030", "#3e2c1e", cz=0.05, pre="cattedra_")
    parti += [
        # lo spartito disegnato da Alcide: pallini colorati al posto delle note
        P("spartito_foglio", "cubo", "carta", "#e3d9bd", x=0.3, y=0.785, z=0.1, sx=0.42, sy=0.004, sz=0.3),
        T("spartito_titolo", "PER AMERIGO", 0.3, 0.79, 0.21, 0.3, 0.04, 0.022, "#3b2a1a", "mano", rx=90),
        # il violino: cassa, manico, tastiera con i cinque segni colorati (cliccabili), archetto
        P("violino_cassa", "sfera", "legno", "#8a4a1e", x=-0.3, y=0.81, z=0.05, rz=0, sx=0.2, sy=0.04, sz=0.36),
        P("violino_vita", "cubo", "legno", "#7a3e18", x=-0.3, y=0.81, z=0.05, sx=0.14, sy=0.038, sz=0.08),
        P("violino_manico", "cubo", "legno", "#2a1a12", x=-0.3, y=0.83, z=-0.25, sx=0.04, sy=0.02, sz=0.3),
        P("violino_riccio", "sfera", "legno", "#5a2e12", x=-0.3, y=0.83, z=-0.42, sx=0.04, sy=0.04, sz=0.05),
        P("archetto", "cilindro", "legno", "#4a2a12", x=-0.05, y=0.795, z=0.0, rx=90, ry=12, sx=0.008, sy=0.3, sz=0.008),
        P("archetto_crini", "cubo", "stoffa", "#e8e0cc", x=-0.03, y=0.792, z=0.0, ry=12, sx=0.006, sy=0.002, sz=0.56),
    ]
    for i, (midi, col, nome) in enumerate(NOTE):
        z = -0.12 - i * 0.045
        parti.append(P("segno_%d" % i, "cilindro", "cera", col, x=-0.3, y=0.842, z=z, sx=0.03, sy=0.003, sz=0.03, clic=True, azione="nota", valore=i,
                       msg="Il segno %s sulla tastiera." % nome))
    # gli spartiti delle due frasi (pallini) e i segni di "fatto"
    for f, (seq, tempo, ok) in enumerate(FRASI):
        grp = "frase_%d" % f
        for k, n in enumerate(seq):
            parti.append(P("frase_%d_%d" % (f, k), "cilindro", "cera", NOTE[n][1], x=0.14 + k * 0.055, y=0.789, z=0.12 - f * 0.1, sx=0.03, sy=0.002, sz=0.03,
                           nascosta=f > 0, gruppo=grp))
        parti.append(T("frase_%d_ok" % f, "BENE", 0.54, 0.79, 0.12 - f * 0.1, 0.08, 0.03, 0.018, "#3a6a3a", "mano", rx=90, nascosta=True))
    # Rivo, in fondo, batte il tempo sul banco (pezzo decorativo: la sua mano)
    parti += [P("rivo_mano", "sfera", "stoffa", "#c99b78", x=0.62, y=0.8, z=-0.25, sx=0.06, sy=0.04, sz=0.07, gruppo="tempo")]
    frasi = [{"note": seq, "tempo": tempo, "ok": ok} for seq, tempo, ok in FRASI]
    return {
        "id": "enigma_melodia", "tipo": "quiz", "meccanismo": "melodia", "titolo": "La prima melodia", "scena": MOD,
        "consegna": "In classe, dopo le lezioni, Alcide ti fa sentire una frase sul violino. Premi Ascolta: le note si accendono anche sulla tastiera e sullo spartito, con i loro colori. Poi ripetila toccando i segni colorati nello stesso ordine. Rivo, dal banco, batte il tempo.",
        "temaCivico": "La scuola e' aperta a tutti (art. 34): in Emilia i bambini ospiti andavano a scuola con i figli delle famiglie. E la musica e' cultura da imparare insieme (art. 9).",
        "indizi": ["Premi Ascolta tutte le volte che vuoi. Guarda l'ordine dei colori sullo spartito.",
                   "Prima frase: quattro note. Seconda frase: sei. Se sbagli, Alcide la rifa' piu' lenta.",
                   "Prima frase: rosso, verde, blu, verde. Seconda: blu, viola, blu, verde, giallo, rosso."],
        "spiegazione": "Le due frasi, una dopo l'altra, senza fermarti. Rivo smette di battere il tempo e applaude, piano, poi forte. Alcide stacca il foglio dei pallini colorati, lo piega in quattro e te lo infila nella tasca del cappotto: «Questo e' tuo. La musica si porta addosso.»",
        "erroreMsg": "",
        "scheda": "Nel romanzo Alcide scopre il talento di Amerigo e gli fa avere un violino. La melodia del gioco e' originale; lo spartito a pallini colorati e' inventato per il gioco, ed e' l'oggetto che Antonietta conservera' senza saperlo leggere.",
        "appunto": "Lo spartito di Alcide: pallini colorati al posto delle note.",
        "x": POS_V5[0], "y": POS_V5[1], "z": POS_V5[2], "yaw": 0, "scala": 1, "distanza": 1.5, "inclina": 34,
        "prompt": "il violino sulla cattedra",
        "richiede": ["violino_ritrovato"], "bloccatoMsg": "La cattedra della maestra e' vuota. Alcide aveva detto: «Quando ritrovi il violino, ti aspetto a scuola.»",
        "produce": ["spartito_alcide"],
        "note": [n[0] for n in NOTE],
        "frasi": frasi,
        "strumenti": [
            "Ascolta",                                                                                 # 0 pulsante
            "Alcide suona la frase. Ora tocca a te.",                                                  # 1
            "Non era quella nota. Alcide la rifa' piu' lenta: ascolta di nuovo.",                       # 2
            "Ancora {0} note.",                                                                        # 3
            "Prima ascolta la frase di Alcide.",                                                       # 4
            "Alcide sta ancora suonando: aspetta la fine della frase.",                                # 5
        ],
        "parti": parti,
        "domande": [
            quiz("Che cosa c'e' sullo spartito di Alcide al posto delle note?", ["Pallini colorati", "Lettere", "Numeri romani", "Niente"], 0, "Un segno colorato per ogni nota."),
            quiz("Se sbagli una nota, che cosa fa Alcide?", ["Rifa' la frase piu' lenta", "Si arrabbia", "Smette di insegnare", "Ti toglie il violino"], 0, "Errore = si riprova, piu' piano."),
            quiz("Quale articolo della Costituzione dice che la scuola e' aperta a tutti?", ["Art. 34", "Art. 1", "Art. 12", "Art. 139"], 0, "Art. 34: la scuola e' aperta a tutti."),
        ],
    }


def main():
    fb = {
        "id": "flashback_violino", "oggetto": "spartito", "titolo": "Il violino", "scena": MOD,
        "introduzione": "Neve sui campi fino all'orizzonte, nebbia bassa, odore di legna e di pane. La corte dei Benvenuti ha tre porte: la cucina di Derna con la stufa accesa, il laboratorio dove Alcide accorda pianoforti, e in fondo alla strada bianca la scuola. Qui nessuno ti chiama con il numero.",
        "conclusione": "Il ricordo si ferma in una cucina calda di Modena: un bambino detta, una donna scrive. In tasca, piegato in quattro, un foglio di pallini colorati.",
        "enigmi": [v1(), v2(), v3(), v4(), v5()],
    }
    fb = accenti(fb)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(fb, f, ensure_ascii=False, indent=2)
    print("scritto", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
