# -*- coding: utf-8 -*-
"""R016: genera Assets/_Game/Resources/Data/flashback_finale.json (il finale: il basso nel 1994).
Uso: python genera_flashback_finale.py. Coordinate = SceneBuilderFinale.cs (scena 80_Basso_1994, stesso impianto del basso di Antonietta).
Trama: TRAMA Decisioni 1 (cornice 1994), 6 (niente lettera del film: violino sotto il letto + oggetto nostro), 7 (Carmine breve, nessun dettaglio giudiziario);
schema D1 (nella custodia il ritaglio di giornale di un concerto di Amerigo), D2 (Carmine con un modello semplice). Antonietta non sapeva scrivere: una croce, nessun testo suo.
La frase del film sull'amore che "lascia andare" NON compare. Numeri (polizza 4712, frequenza 92,7) e date del riscatto inventati per il gioco.
"""
import json, os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "Cappotto"))
from genera_flashback_cappotto import P, T, quiz, figura, accenti

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data", "flashback_finale.json")
FIN = "80_Basso_1994"

# ---------------------------------------------------------------- posizioni (= SceneBuilderFinale.cs)
POS_N1 = (3.6, 0.0, -1.95)     # telefono, calendario e latta nell'angolo sud-est (fronte verso nord)
POS_SCARPE = (0.6, 0.0, -2.0)  # la soglia, accanto alla porta
POS_STRACCI = (3.7, 0.0, 10.3) # la cesta del cortile
POS_VOLANTINO = (-3.98, 0.965, 1.98)  # la credenza della stanza principale
POS_CAPPOTTO = (1.15, 1.1, -2.3)      # il chiodo dietro la porta
POS_SPARTITO = (-3.85, 0.62, 7.35)    # il cuscino del letto di ferro (alcova)
POS_N3 = (2.3, 0.66, 1.2)      # il tavolo sotto la lampadina
POS_N4 = (-3.85, 0.0, 6.0)     # sotto il letto, dal lato del corridoio dell'alcova
POS_N5 = (-1.3, 0.0, -1.95)    # la porta sul vicolo: Carmine


def e_base(id, titolo, mec, pos, yaw, **kw):
    d = {"id": id, "tipo": "quiz", "meccanismo": mec, "titolo": titolo, "scena": FIN, "x": pos[0], "y": pos[1], "z": pos[2], "yaw": yaw, "scala": 1}
    d.update(kw)
    return d


# ---------------------------------------------------------------- N1 il basso svuotato
def n1():
    parti = [
        P("mobiletto", "cubo", "legno", "#4a3a2c", y=0.35, sx=0.6, sy=0.7, sz=0.4),
        P("centrino", "cubo", "stoffa", "#e6e0d0", y=0.703, sx=0.45, sy=0.004, sz=0.3),
        # telefono a disco grigio
        P("telefono", "cubo", "metallo", "#5a5e62", x=-0.12, y=0.76, sx=0.2, sy=0.1, sz=0.18, clic=True, azione="verifica", evento="telefono",
          msg="Il telefono squilla. Rispondi: niente, solo il fruscio della linea. Poi riattaccano. Chi chiama ancora questo numero?"),
        P("cornetta", "capsula", "metallo", "#4a4e52", x=-0.12, y=0.83, rz=90, sx=0.05, sy=0.1, sz=0.05),
        P("disco", "cilindro", "metallo", "#d8d4c8", x=-0.12, y=0.812, z=-0.05, rx=-20, sx=0.08, sy=0.004, sz=0.08),
        # la scatola di latta (aggancio al quaderno/latta del gioco) con il cartellino del treno
        P("latta", "cilindro", "metallo", "#7a6a4a", x=0.14, y=0.75, sx=0.16, sy=0.05, sz=0.16),
        P("latta_coperchio", "cilindro", "metallo", "#8a7a5a", x=0.2, y=0.71, z=-0.12, rx=70, sx=0.165, sy=0.006, sz=0.165),
        P("cartellino", "cubo", "carta", "#e6dcc0", x=0.14, y=0.806, rx=0, sx=0.09, sy=0.004, sz=0.06, testo="N. 112", dimensione=0.022,
          clic=True, azione="verifica", evento="cartellino",
          msg="Nella latta, sopra tutto, un cartellino di cartone con lo spago: N. 112. Il tuo numero del treno. Lei l'aveva tenuto. Sul fondo della latta, cinque segni a matita: cinque posti della casa."),
        # il calendario del 1994 appeso al muro, con un giorno cerchiato
        P("calendario", "cubo", "carta", "#e8e2d0", y=1.5, z=0.22, sx=0.36, sy=0.48, sz=0.006, testo="1994", dimensione=0.06,
          clic=True, azione="verifica", evento="calendario",
          msg="Un calendario del 1994, fermo a marzo. Un giorno e' cerchiato a penna: «Amerigo, teatro». La data di un tuo concerto, in un'altra citta'."),
        P("calendario_cerchio", "cilindro", "carta", "#a3261c", x=0.06, y=1.38, z=0.215, rx=90, sx=0.05, sy=0.002, sz=0.04),
        P("chiodo_cal", "cilindro", "metallo", "#3a3632", y=1.76, z=0.215, rx=90, sx=0.01, sy=0.02, sz=0.01),
    ]
    return e_base("enigma_basso94", "Il basso svuotato", "oggetto", POS_N1, 180,
        consegna="Il basso e' vuoto e freddo, i mobili sotto le lenzuola. Nell'angolo, accanto alla porta, le ultime cose di tua madre: il telefono, un calendario, la scatola di latta. Guardale tutte e tre.",
        temaCivico="La memoria di una famiglia sta negli oggetti che si conservano: anche loro sono documenti.",
        indizi=["Tocca il telefono, il calendario e la scatola di latta.", "Il calendario ha un giorno cerchiato; nella latta c'e' qualcosa con un numero.", "Telefono, calendario, cartellino: tutti e tre."],
        spiegazione="Cinque segni a matita sul fondo della latta, uno per ogni posto della casa. Tua madre non sapeva scrivere, ma sapeva dove aveva messo le cose. Rimettile al loro posto.",
        erroreMsg="", scheda="Napoli, marzo 1994 [dal film, da verificare]: un basso dei Quartieri Spagnoli dopo la morte di chi ci ha vissuto tutta la vita.",
        appunto="Nella latta: il cartellino 112 e cinque posti segnati.",
        distanza=1.4, inclina=10, prompt="il telefono e la scatola di latta",
        soluzione=["telefono", "calendario", "cartellino"], ordinata=False,
        produce=["elenco_posti", "cartellino_112"], parti=parti,
        domande=[quiz("Che cosa c'e' nella scatola di latta?", ["Il cartellino del treno, n. 112", "Un violino", "Un biglietto da mille lire", "Niente"], 0, "Il numero del viaggio verso Modena."),
                 quiz("Che giorno e' cerchiato sul calendario?", ["Il giorno di un concerto di Amerigo", "Natale", "Il compleanno di Antonietta", "Nessuno"], 0, "Lei sapeva dei suoi concerti."),
                 quiz("Perche' i posti sono segnati con segni e non con parole?", ["Antonietta non sapeva scrivere", "Per gioco", "Per non farli trovare", "Per errore"], 0, "Era analfabeta, come molte donne della sua generazione.")])


# ---------------------------------------------------------------- N2 cinque ricordi al loro posto (stanza a due tempi)
OGGETTI = [
    # id, nome sul vassoio, colore, forma del segnaposto
    ("scarpe", "le scarpe", "#3a2418"),
    ("stracci", "gli stracci", "#8a6a4a"),
    ("volantino", "il volantino", "#d8cba6"),
    ("cappotto", "il cappotto", "#3f4a57"),
    ("spartito", "lo spartito", "#e3d9bd"),
]
POSTI = {
    "scarpe": ("La soglia", "Sulla soglia, dove le scarpe si lasciavano entrando: le scarpe al loro posto.", POS_SCARPE, 180, "il posto delle scarpe sulla soglia",
               "Le scarpe si lasciavano entrando, sulla soglia."),
    "stracci": ("La cesta del cortile", "Nella cesta del cortile, dove Antonietta teneva le pezze da vendere: gli stracci al loro posto.", POS_STRACCI, 180, "la cesta del cortile",
                "Gli stracci stavano nella cesta del cortile, pronti per Capa 'e fierro."),
    "volantino": ("La credenza", "Sulla credenza, sotto il piatto buono, dove stava il volantino: al suo posto.", POS_VOLANTINO, 270, "la credenza",
                  "Il volantino stava sulla credenza, sotto il piatto buono."),
    "cappotto": ("Il chiodo della porta", "Al chiodo dietro la porta, dove e' rimasto appeso per quasi cinquant'anni: il cappotto.", POS_CAPPOTTO, 180, "il chiodo dietro la porta",
                 "Il cappotto si appende al chiodo dietro la porta."),
    "spartito": ("Il cuscino", "Sotto il cuscino del letto di ferro: lo spartito, piegato in quattro.", POS_SPARTITO, 0, "il cuscino del letto",
                 "Lo spartito lei lo teneva sotto il cuscino."),
}


def vassoio(giusto):
    """Il vassoio della latta con i cinque ricordi: quello giusto si posa, gli altri dicono "Qui non c'era" e dove andavano."""
    V = dict(nascosta=True, gruppo="vista")  # il vassoio c'e' solo nella vista ravvicinata (nel mondo il posto resta vuoto)
    parti = [P("vassoio", "cubo", "metallo", "#7a6a4a", x=0.0, y=0.02, z=-0.32, sx=0.62, sy=0.02, sz=0.16, **V)]
    for k, (id, nome, col) in enumerate(OGGETTI):
        x = -0.24 + k * 0.12
        if id == giusto:
            g = dict(clic=True, azione="verifica", evento="posato", dopo=id + "_posato,frammento,-ricordo_" + id,
                     msg="")
        else:
            g = dict(clic=True, azione="errore", msg="Qui non c'era. " + POSTI[id][5])
        parti.append(P("ricordo_" + id, "cubo", "carta" if id in ("volantino", "spartito") else "stoffa", col, x=x, y=0.05, z=-0.32,
                       sx=0.09, sy=0.03, sz=0.08, testo=nome.split(" ")[1].upper(), dimensione=0.012, **V, **g))
    return parti


def posto(id):
    titolo, ok, pos, yaw, prompt, _ = POSTI[id]
    col = dict((o[0], o[2]) for o in OGGETTI)[id]
    parti = vassoio(id)
    # il posto (contorno chiaro a gesso) e l'oggetto posato
    if id == "scarpe":
        parti += [P("soglia", "cubo", "pietra", "#6a5a4c", y=0.01, sx=0.7, sy=0.02, sz=0.3),
                  P("scarpe_posato", "scarpa", "cuoio", col, x=-0.06, y=0.06, ry=10, sx=0.8, sy=0.8, sz=0.8, testo="lucide", nascosta=True)]
    elif id == "stracci":
        parti += [P("cesta", "cilindro", "legno", "#a8895a", y=0.2, sx=0.5, sy=0.2, sz=0.5),
                  P("stracci_posato", "sfera", "stoffa", col, y=0.38, sx=0.42, sy=0.14, sz=0.42, nascosta=True)]
    elif id == "volantino":
        parti += [P("piatto", "cilindro", "gesso", "#e8e2d0", y=0.01, sx=0.24, sy=0.01, sz=0.24),
                  P("volantino_posato", "cubo", "carta", col, y=0.004, z=0.04, sx=0.16, sy=0.004, sz=0.11, nascosta=True)]
    elif id == "cappotto":
        parti += [P("chiodo", "cilindro", "metallo", "#3a3632", y=0.62, z=0.02, rx=90, sx=0.012, sy=0.03, sz=0.012),
                  P("cappotto_posato", "cubo", "stoffa", col, y=0.3, sx=0.3, sy=0.44, sz=0.05, nascosta=True)]
    else:
        parti += [P("cuscino", "cubo", "stoffa", "#e0d8c4", y=0.06, sx=0.6, sy=0.12, sz=0.3),
                  P("spartito_posato", "cubo", "carta", col, y=0.125, z=-0.08, sx=0.17, sy=0.004, sz=0.1, nascosta=True)]
    # il frammento di carta lasciato dal ricordo (lo raccoglie l'enigma: gettone frammento_<id>)
    parti.append(P("frammento", "cubo", "carta", "#d8cba6", x=0.18, y=0.012 if id != "cappotto" else 0.02, z=-0.12, ry=24, sx=0.07, sy=0.003, sz=0.05, nascosta=True))
    return e_base("enigma_posto_" + id, titolo, "oggetto", pos, yaw,
        consegna="Uno dei cinque posti segnati nella latta. Quale ricordo abitava qui? Posalo dal vassoio.",
        temaCivico="Memoria e legami familiari: la Costituzione (art. 30) parla del dovere dei genitori verso i figli; qui e' una madre che ha conservato tutto.",
        indizi=["Ogni ricordo ha un posto in cui lo usavi.", "Il cappotto si appende, le scarpe si lasciano entrando, gli stracci vanno nella cesta, il volantino sulla credenza, lo spartito sotto il cuscino.", "Qui va " + dict((o[0], o[1]) for o in OGGETTI)[id] + "."],
        spiegazione=ok + " Per un momento l'angolo torna com'era nel 1946: luce di lume a petrolio, voci dal vicolo, qualcuno di spalle. Poi a terra resta un frammento di carta.",
        erroreMsg="Qui non c'era.", scheda="", appunto="Un frammento di carta: " + titolo.lower() + ".",
        distanza=1.3, inclina=18, prompt=prompt, soluzione=["posato"],
        richiede=["elenco_posti"], bloccatoMsg="Un posto come un altro, per ora. Prima guarda le cose di tua madre nell'angolo accanto alla porta.",
        produce=["frammento_" + id], parti=parti,
        domande=[quiz("Dove va " + dict((o[0], o[1]) for o in OGGETTI)[id] + "?", [titolo, "Nella spazzatura", "In valigia", "Al Monte dei pegni"], 0, ok),
                 quiz("Che cosa succede quando un ricordo torna al suo posto?", ["L'angolo torna per un momento al 1946", "Niente", "Suona il telefono", "Si spegne la luce"], 0, "La stanza ha due tempi."),
                 quiz("Che cosa resta a terra?", ["Un frammento di carta", "Una moneta", "Una chiave", "Niente"], 0, "Cinque frammenti: si ricompongono sul tavolo.")])


# ---------------------------------------------------------------- N3 la ricevuta del Monte dei pegni (ricomporre)
# griglia 3 x 2 sul tavolo: ogni pezzo ha il suo pezzo di testo; i pezzi stanno sparsi a destra
GRIGLIA = [  # (riga, colonna, testo del pezzo)
    (0, 0, "MONTE DEI"), (0, 1, "PEGNI"), (0, 2, "N. 4712"),
    (1, 0, "VIOLINO CON"), (1, 1, "CUSTODIA"), (1, 2, "RISCATTATO"),
]


def n3():
    W, H = 0.12, 0.08
    parti = [
        P("lampadina_ombra", "cilindro", "stoffa", "#2a2018", y=0.002, sx=0.9, sy=0.002, sz=0.6),
        P("sottomano", "cubo", "cuoio", "#3a2a1e", x=-0.12, y=0.004, sx=0.44, sy=0.004, sz=0.26),
    ]
    sparsi = [(0.26, 0.06, 12), (0.33, -0.08, -20), (0.2, -0.1, 30), (0.38, 0.1, -8), (0.24, 0.16, 18), (0.36, -0.18, 26)]
    for k, (r, c, testo) in enumerate(GRIGLIA):
        sx, sz = -0.12 + (c - 1) * W, 0.04 - r * H
        parti.append(P("slot_%d" % k, "cubo", "carta", "#5a4a3a", x=sx, y=0.007, z=sz, sx=W * 0.96, sy=0.002, sz=H * 0.94, clic=True, azione="slot", valore=k, msg=""))
        x, z, ry = sparsi[k]
        parti.append(P("pezzo_%d" % k, "cubo", "carta", "#e3d9bd", x=x, y=0.012, z=z, ry=ry, sx=W * 0.95, sy=0.003, sz=H * 0.93, testo=testo, dimensione=0.014,
                       clic=True, azione="pezzo", valore=k, msg="Un pezzo di carta spessa, ingiallita: «%s»." % testo))
    parti += [
        P("croce", "cubo", "carta", "#2a2018", x=-0.02, y=0.016, z=-0.075, ry=45, sx=0.03, sy=0.001, sz=0.004, nascosta=True),
        P("croce_b", "cubo", "carta", "#2a2018", x=-0.02, y=0.016, z=-0.075, ry=-45, sx=0.03, sy=0.001, sz=0.004, nascosta=True),
        P("timbro", "cilindro", "carta", "#7a1f1a", x=0.08, y=0.016, z=-0.05, rx=0, sx=0.07, sy=0.001, sz=0.05, nascosta=True),
    ]
    return e_base("enigma_ricevuta", "La ricevuta", "ricomponi", POS_N3, 0,
        consegna="Sotto la lampadina, sul tavolo, i cinque frammenti e un pezzo che c'era gia': e' un documento strappato. Tocca un pezzo, poi il punto del sottomano dove va. I bordi devono combaciare e le parole avere senso.",
        temaCivico="Il credito su pegno: chi era povero portava al Monte i suoi oggetti per avere soldi in prestito, e poteva riscattarli pagando. Ricomprare il violino fu il gesto di una madre.",
        indizi=["I bordi dei pezzi combaciano: in alto il nome del Monte, in basso che cosa e' stato impegnato.", "Si legge da sinistra a destra: MONTE DEI - PEGNI - N. 4712, poi VIOLINO CON - CUSTODIA - RISCATTATO.", "Il timbro RISCATTATO sta in basso a destra; il numero in alto a destra."],
        spiegazione="E' una polizza del Monte dei pegni: un violino con custodia, numero 4712. Sopra, un timbro rosso: RISCATTATO, qualche anno dopo. Al posto della firma, una croce. Tua madre era tornata a riprenderlo, da sola, e non te l'aveva mai detto.",
        erroreMsg="", scheda="I Monti dei pegni (o di pieta') prestavano denaro in cambio di un oggetto in garanzia; molte famiglie povere ci portavano lenzuola, ori e strumenti. Numero e date della polizza sono inventati per il gioco.",
        appunto="Polizza n. 4712: violino con custodia, RISCATTATO.",
        distanza=1.1, inclina=62, prompt="i frammenti sul tavolo",
        richiede=["frammento_scarpe", "frammento_stracci", "frammento_volantino", "frammento_cappotto", "frammento_spartito"],
        bloccatoMsg="Sul tavolo c'e' un solo pezzo di carta strappata. Gli altri, dice la latta, sono nei cinque posti della casa.",
        produce=["numero_polizza"],
        strumenti=["Ora tocca il punto del sottomano dove va.", "Prima prendi un pezzo di carta.", "I bordi non combaciano, e le parole non tornano: prova un altro punto.", "Ancora {0} pezzi.", "Il pezzo e' gia' al suo posto."],
        dopoRicomposto="croce,croce_b,timbro",
        parti=parti,
        domande=[quiz("Che cos'e' il documento ricomposto?", ["Una polizza del Monte dei pegni", "Una lettera", "Un biglietto del treno", "Una pagella"], 0, "Un violino impegnato e poi riscattato."),
                 quiz("Che cosa vuol dire RISCATTATO?", ["Che il violino e' stato ripreso pagando", "Che e' stato venduto", "Che e' stato perso", "Che e' rotto"], 0, "Antonietta l'ha ricomprato."),
                 quiz("Perche' al posto della firma c'e' una croce?", ["Antonietta non sapeva scrivere", "Per scherzo", "Era vietato firmare", "Per errore"], 0, "Chi era analfabeta firmava con una croce.")])


# ---------------------------------------------------------------- N4 sotto il letto (la custodia con il lucchetto a numeri)
def n4():
    parti = [
        P("custodia", "cubo", "cuoio", "#2a1a12", y=0.06, sx=0.75, sy=0.11, sz=0.26),
        P("custodia_bordo", "cubo", "cuoio", "#1e120c", y=0.115, sx=0.76, sy=0.01, sz=0.27),
        P("etichetta", "cubo", "carta", "#e3d9bd", x=-0.2, y=0.122, sx=0.1, sy=0.002, sz=0.05, testo="4712", dimensione=0.016,
          clic=True, azione="leggi", msg="Un'etichetta del Monte dei pegni incollata sulla custodia: 4712. Lo stesso numero della polizza."),
        P("lucchetto", "cubo", "ottone", "#b59a3f", x=0.12, y=0.06, z=-0.135, sx=0.24, sy=0.06, sz=0.012),
        P("fermo", "cilindro", "ottone", "#b59a3f", x=0.27, y=0.06, z=-0.14, rx=90, sx=0.03, sy=0.012, sz=0.03, clic=True, azione="verifica", evento="apri_custodia",
          dopo="coperchio_aperto,violino,violino_manico,ritaglio,-custodia_bordo", msg="Clic. Il fermo della custodia si apre."),
        P("coperchio_aperto", "cubo", "cuoio", "#2a1a12", y=0.2, z=0.13, rx=-70, sx=0.75, sy=0.02, sz=0.26, nascosta=True),
        P("violino", "sfera", "legno", "#8a4a1e", x=-0.1, y=0.1, sx=0.36, sy=0.05, sz=0.2, nascosta=True),
        P("violino_manico", "cubo", "legno", "#2a1a12", x=0.18, y=0.105, sx=0.28, sy=0.02, sz=0.04, nascosta=True),
        P("ritaglio", "cubo", "carta", "#d8d0b8", x=-0.12, y=0.13, z=0.04, ry=-14, sx=0.1, sy=0.003, sz=0.14, nascosta=True, testo="CONCERTO", dimensione=0.012,
          clic=True, azione="leggi", msg="Un ritaglio di giornale, piegato con cura lungo le pieghe vecchie: la fotografia di un concerto, un violinista in frac. Il tuo nome in grassetto. Lei non sapeva leggerlo, ma la fotografia l'aveva riconosciuta."),
    ]
    return e_base("enigma_custodia", "Sotto il letto", "ruote", POS_N4, 0,
        consegna="Sotto il letto di ferro, dietro una coperta piegata, c'e' una custodia da violino. E' chiusa da un lucchetto a quattro rotelle.",
        temaCivico="Una famiglia sono anche i gesti che non si dicono: tenere, conservare, ricomprare.",
        indizi=["Sulla custodia c'e' un'etichetta con un numero.", "E' lo stesso numero della polizza ricomposta sul tavolo.", "4712."],
        spiegazione="Dentro, il violino che Alcide ti aveva dato, con le corde allentate da cinquant'anni. Accanto, piegato con cura, un ritaglio di giornale: un tuo concerto. Nessuna lettera. Non serve.",
        erroreMsg="Il lucchetto non si apre: che numero c'era sulla polizza?",
        scheda="Nel romanzo e nel film Amerigo ritrova il violino dopo la morte della madre; il ritaglio di giornale e la polizza sono inventati per il gioco (nel film c'e' una lettera: qui no).",
        appunto="Il violino era sotto il letto, con un ritaglio di giornale.",
        distanza=1.3, inclina=40, prompt="qualcosa sotto il letto",
        codice="4712", soluzione=["apri_custodia"],
        ruote={"n": 4, "x": 0.12, "y": 0.06, "z": -0.142, "passo": 0.05, "larghezza": 0.035, "raggio": 0.03, "colore": "#9b7a35"},
        richiede=["numero_polizza"], bloccatoMsg="Sotto il letto c'e' qualcosa di lungo e scuro, avvolto in una coperta. Prima capisci che cosa c'e' sul tavolo.",
        produce=["violino_1994"], cutscene="cutscene_finale", cutsceneCapitolo="meta",
        parti=parti,
        domande=[quiz("Quale numero apre la custodia?", ["4712, come la polizza", "112", "1946", "214"], 0, "Lo stesso numero del Monte dei pegni."),
                 quiz("Che cosa c'e' nella custodia, oltre al violino?", ["Un ritaglio di giornale di un concerto di Amerigo", "Una lettera lunga", "Una mela", "Soldi"], 0, "Un segno, non un testo."),
                 quiz("Chi aveva ricomprato il violino?", ["Antonietta", "Alcide", "Carmine", "Nessuno"], 0, "La polizza dice RISCATTATO.")])


# ---------------------------------------------------------------- N5 Carmine (scelta senza errore)
def n5():
    pp, collo, busto = figura("fig_carmine", 0, 0.0, 0.05, 1.42, "#3a4a5a", "#2a2018", "Carmine, sulla porta: un ragazzino con un giubbotto troppo grande e le scarpe da ginnastica slacciate.")
    parti = pp + [
        P("stipite_sx", "cubo", "legno", "#4a3220", x=-0.5, y=1.05, z=0.2, sx=0.08, sy=2.1, sz=0.12),
        P("stipite_dx", "cubo", "legno", "#4a3220", x=0.5, y=1.05, z=0.2, sx=0.08, sy=2.1, sz=0.12),
    ]
    figure = [{"id": "fig_carmine", "nome": "Carmine", "collo": collo, "busto": busto,
               "paura": "«Tu sei lo zio che suona? Papa' dice che te ne sei andato e non sei tornato piu'. Io non me ne vado mai da qua.»",
               "ok": "Carmine resta sulla soglia, ma non scappa.",
               "risposte": [
                   {"testo": "Dargli il cartellino del treno: «Questo era il mio numero. Tienilo tu.»", "richiede": "cartellino_112", "giusta": True,
                    "esito": "Carmine gira il cartellino fra le dita. «Centododici. Ma eri un pacco?» Ride. Poi se lo mette in tasca, con attenzione."},
                   {"testo": "Accordare il violino davanti a lui: «Senti. Prima ua-ua, poi niente.»", "richiede": "", "giusta": True,
                    "esito": "Giri la chiave a mezzi scatti. Carmine si avvicina di un passo, poi di un altro. «Mo' e' sparito.» «Si'. Mo' e' giusto.»"},
                   {"testo": "Suonare le due frasi di Alcide.", "richiede": "", "giusta": True,
                    "esito": "Le due frasi riempiono il basso vuoto. Carmine non dice niente. Quando finisci, batte il tempo sullo stipite, sbagliato, e sorride."},
               ]}]
    return e_base("enigma_carmine", "Carmine", "dialogo", POS_N5, 180,
        consegna="Qualcuno bussa alla porta sul vicolo. E' un ragazzino: Carmine, il figlio di tuo fratello Agostino. Scegli che cosa fare con lui: ogni scelta va bene.",
        temaCivico="Una famiglia si puo' anche scegliere: la legge 4 maggio 1983 n. 184 riconosce il diritto del minore a una famiglia, anche attraverso l'affido.",
        indizi=["Non ci sono risposte sbagliate: scegli quella che senti tua.", "Puoi dargli il cartellino, accordare il violino o suonare.", "Una qualunque delle tre."],
        spiegazione="Carmine si siede sul gradino della porta, accanto a te. Fuori, il vicolo fa il suo rumore di sempre.",
        erroreMsg="", scheda="Nel romanzo Amerigo, nel 1994, conosce Carmine, figlio di Agostino, il fratellino nato dopo la sua partenza: per lui diventa l'occasione di restituire la solidarieta' ricevuta. Le parole sono scritte per il gioco.",
        appunto="Carmine, sulla soglia.",
        distanza=2.0, inclina=6, prompt="la porta sul vicolo",
        richiede=["violino_1994"], bloccatoMsg="La porta sul vicolo e' chiusa. Fuori, ogni tanto, passa qualcuno.",
        produce=["testimone"], figure=figure,
        strumenti=["Scegli", "Prova un altro modo.", "Non hai il cartellino: e' nella scatola di latta.", ""],
        parti=parti,
        domande=[quiz("Chi e' Carmine?", ["Il figlio di Agostino, fratello di Amerigo", "Un vicino", "Un postino", "Il figlio di Alcide"], 0, "Il nipote di Amerigo."),
                 quiz("Che cosa puo' fare Amerigo con lui?", ["Una qualunque delle tre scelte", "Solo suonare", "Solo mandarlo via", "Niente"], 0, "Nessuna scelta e' sbagliata."),
                 quiz("Quale legge parla del diritto del minore a una famiglia?", ["La legge n. 184 del 1983", "Lo Statuto albertino", "Il codice della strada", "Nessuna"], 0, "Modificata dalla legge 149 del 2001.")])


def main():
    fb = {
        "id": "flashback_finale", "oggetto": "violino", "titolo": "Il violino ritrovato", "scena": FIN,
        "introduzione": "Napoli, 1994. Il basso di tua madre: mobili sotto le lenzuola, una lampadina nuda, scatoloni. Dal vicolo arrivano la radio di un vicino e il rumore dei motorini. La stanza e' la stessa di cinquant'anni fa, e non e' piu' la stessa.",
        "conclusione": "Una custodia aperta su un letto di ferro, un ragazzino sulla soglia. Il treno dei bambini e' partito nel 1946; qualcuno, oggi, lo fa ripartire.",
        "enigmi": [n1()] + [posto(o[0]) for o in OGGETTI] + [n3(), n4(), n5()],
    }
    fb = accenti(fb)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(fb, f, ensure_ascii=False, indent=2)
    print("scritto", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
