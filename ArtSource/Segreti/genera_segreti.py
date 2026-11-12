# -*- coding: utf-8 -*-
"""
R011: genera Assets/_Game/Resources/Data/segreti.json.

I segreti si agganciano per NOME del punto nei rapporti R006 (Docs/richieste/media/R006/punti_segreti_*.md): se la scena
viene rigenerata e le coordinate cambiano, basta rilanciare questo script (python ArtSource/Segreti/genera_segreti.py) e
x,y,z si aggiornano da soli. Il codice del gioco non contiene testi: tutto sta nel JSON.
"""
import json
import os
import re
import sys

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MD = {
    "10_Hub_Vicolo": "Docs/richieste/media/R006/punti_segreti_hub.md",
    "20_Int_BassoAntonietta": "Docs/richieste/media/R006/punti_segreti_basso.md",
    "30_FB_Scarpe": "Docs/richieste/media/R006/punti_segreti_flashback.md",
    "40_FB_Stracci": "Docs/richieste/media/R012/punti_segreti_stracci.md",
    "50_FB_Volantino": "Docs/richieste/media/R013/punti_segreti_volantino.md",
}
HUB, BASSO, RIC, STR = "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe", "40_FB_Stracci"
VOL = "50_FB_Volantino"


def leggi_punti():
    res = {}
    for scena, path in MD.items():
        punti = {}
        conta = {}
        for riga in open(os.path.join(RADICE, path), encoding="utf-8"):
            m = re.match(r"\|\s*(\d+)\s*\|\s*(.+?)\s*\|\s*(-?[\d,\.]+)\s*\|\s*(-?[\d,\.]+)\s*\|\s*(-?[\d,\.]+)\s*\|", riga)
            if not m:
                continue
            nome = m.group(2)
            conta[nome] = conta.get(nome, 0) + 1
            chiave = nome if conta[nome] == 1 else "%s #%d" % (nome, conta[nome])
            f = lambda s: float(s.replace(",", "."))
            punti[chiave] = (f(m.group(3)), f(m.group(4)), f(m.group(5)))
        res[scena] = punti
    return res


PUNTI = leggi_punti()


def pos(scena, punto, dx=0.0, dy=0.0, dz=0.0):
    if punto not in PUNTI[scena]:
        sys.exit("Punto inesistente in %s: %s" % (scena, punto))
    x, y, z = PUNTI[scena][punto]
    return round(x + dx, 3), round(y + dy, 3), round(z + dz, 3)


# ----------------------------------------------------------------------------- parti (pezzi di primitive)
def scuro(col, k):
    if not col or not col.startswith("#") or len(col) != 7:
        return col
    r, g, b = int(col[1:3], 16), int(col[3:5], 16), int(col[5:7], 16)
    return "#%02x%02x%02x" % (int(r * k), int(g * k), int(b * k))


def parte(id, forma="cubo", mat="legno", col="", x=0, y=0, z=0, sx=1, sy=1, sz=1, **k):
    if mat in ("carta", "gesso") and forma != "testo":
        col = scuro(col, 0.74)   # la carta d'epoca e' ingiallita, non bianca
    d = {"id": id, "forma": forma, "materiale": mat, "colore": col, "x": x, "y": y, "z": z, "sx": sx, "sy": sy, "sz": sz}
    d.update(k)
    return d


def testo_piano(id, t, dim, x=0, y=0.006, z=0, col="#2a2018", larg=0.2, alt=0.1, font="mano"):
    return parte(id, "testo", "carta", col, x, y, z, larg, alt, 1, testo=t, dimensione=dim, rx=90, font=font)


def pezzo(id, scena, punto, parti, ruolo="raccogli", segreto="", prompt="", msg="", titolo="", dx=0, dy=0, dz=0, yaw=0, rx=0, rz=0,
          appoggio="suolo", scala=1.0, produce=None, richiede=None, richiedeMsg="", appare=None, nascondi="", scorri="", slide=(0, 0, 0), raggio=0.45, altro=""):
    x, y, z = pos(scena, punto, dx, dy, dz)
    return {"id": id, "scena": scena, "segreto": segreto, "ruolo": ruolo, "punto": punto, "dx": dx, "dy": dy, "dz": dz,
            "x": x, "y": y, "z": z, "yaw": yaw, "rx": rx, "rz": rz, "appoggio": appoggio, "scala": scala, "prompt": prompt, "titolo": titolo,
            "msg": msg, "produce": produce or [], "richiede": richiede or [], "richiedeMsg": richiedeMsg,
            "appareQuando": appare or [], "nascondiQuando": nascondi, "scorriQuando": scorri, "sx": slide[0], "sy": slide[1], "sz": slide[2],
            "raggio": raggio, "scorriOggetto": altro, "parti": parti}


def segreto(id, scena, tipo, nome, scheda, ordine, punto, ricompensa="", dx=0, dy=0, dz=0):
    x, y, z = pos(scena, punto, dx, dy, dz)
    return {"id": id, "scena": scena, "tipo": tipo, "nome": nome, "scheda": scheda, "ricompensa": ricompensa,
            "punto": punto, "x": x, "y": y, "z": z, "ordine": ordine}


# ----------------------------------------------------------------------------- modelli dei collezionabili (primitive, pochissimi poligoni)
def cartolina():
    return [parte("cartone", "cubo", "tex:cartolina", "", 0, 0.003, 0, 0.15, 0.004, 0.10)]


def carta_napoletana():
    return [parte("carta", "cubo", "tex:settebello", "", 0, 0.002, 0, 0.075, 0.003, 0.125)]


def trottola():
    return [parte("corpo", "sfera", "legno", "#3c5c8c", 0, 0.045, 0, 0.07, 0.07, 0.07),
            parte("punta", "cilindro", "metallo", "#303030", 0, 0.01, 0, 0.012, 0.012, 0.012),
            parte("manico", "cilindro", "legno", "#8a6a44", 0, 0.095, 0, 0.012, 0.02, 0.012),
            parte("fascia", "cilindro", "legno", "#a43c2f", 0, 0.045, 0, 0.074, 0.006, 0.074)]


def banconota_am():
    return [parte("biglietto", "cubo", "tex:amlire", "", 0, 0.002, 0, 0.16, 0.003, 0.075)]


def ritaglio():
    return [parte("foglio", "cubo", "tex:ritaglio", "", 0, 0.002, 0, 0.12, 0.003, 0.15, ry=8)]


def santino():
    return [parte("cartoncino", "cubo", "carta", "#dccb9d", 0, 0.002, 0, 0.075, 0.003, 0.115),
            parte("cuore", "sfera", "stoffa", "#9e3a2e", 0, 0.004, 0.01, 0.025, 0.006, 0.025),
            parte("aureola", "cilindro", "ottone", "#c9a24a", 0, 0.004, -0.03, 0.03, 0.0008, 0.03)]


def ditale():
    return [parte("corpo", "cilindro", "ottone", "#b58a38", 0, 0.014, 0, 0.032, 0.014, 0.032),
            parte("cima", "sfera", "ottone", "#b58a38", 0, 0.028, 0, 0.032, 0.014, 0.032),
            parte("bordo", "cilindro", "ottone", "#9a7430", 0, 0.002, 0, 0.038, 0.002, 0.038)]


def bottone_ferro(col="#585858"):
    p = [parte("disco", "cilindro", "metallo", col, 0, 0.004, 0, 0.032, 0.004, 0.032),
         parte("rilievo", "cilindro", "metallo", "#6a6a6a", 0, 0.0085, 0, 0.026, 0.0012, 0.026)]
    for i, (fx, fz) in enumerate(((-0.0045, -0.0045), (0.0045, -0.0045), (-0.0045, 0.0045), (0.0045, 0.0045))):
        p.append(parte("foro%d" % i, "cilindro", "metallo", "#1c1c1c", fx, 0.0099, fz, 0.0042, 0.0004, 0.0042))
    return p


def biglia():
    return [parte("vetro", "sfera", "vetro", "#4d7f88", 0, 0.016, 0, 0.032, 0.032, 0.032),
            parte("anima", "sfera", "vetro", "#e8e0c8", 0, 0.016, 0, 0.012, 0.012, 0.012)]


def moneta(col="#8a5a36", d=0.03):
    return [parte("disco", "cilindro", "ottone", col, 0, 0.002, 0, d, 0.002, d),
            parte("rilievo", "cilindro", "ottone", "#6d4529", 0, 0.0042, 0, d * 0.7, 0.0006, d * 0.7)]


def pallone_stracci():
    return [parte("palla", "sfera", "stoffa", "#8a7054", 0, 0.1, 0, 0.2, 0.2, 0.2),
            parte("fascia1", "cilindro", "stoffa", "#5f4a38", 0, 0.1, 0, 0.205, 0.012, 0.205),
            parte("fascia2", "cilindro", "stoffa", "#a68c6a", 0, 0.1, 0, 0.205, 0.012, 0.205, rx=90),
            parte("spago", "cilindro", "stoffa", "#3b3026", 0, 0.1, 0, 0.206, 0.004, 0.206, rz=90)]


def biglietto_spesa():
    return [parte("foglio", "cubo", "tex:listaspesa", "", 0, 0.002, 0, 0.10, 0.003, 0.125, ry=-6)]


def uovo_paglia():
    return [parte("paglia", "cubo", "stoffa", "#bfa563", 0, 0.004, 0, 0.18, 0.012, 0.12),
            parte("paglia2", "cubo", "stoffa", "#a9904f", 0.01, 0.009, 0.01, 0.15, 0.008, 0.1, ry=33),
            parte("uovo", "sfera", "gesso", "#e6dac0", 0, 0.03, 0, 0.05, 0.066, 0.05),
            parte("biglietto", "cubo", "carta", "#d6c8a2", 0.07, 0.007, -0.03, 0.06, 0.002, 0.045, ry=25)]


def materassino():
    return [parte("rotolo", "cilindro", "stoffa", "#8d7b5d", 0, 0.1, 0, 0.2, 0.3, 0.2, rz=90),
            parte("fascia1", "cilindro", "stoffa", "#5a4a38", -0.1, 0.1, 0, 0.206, 0.012, 0.206, rz=90),
            parte("fascia2", "cilindro", "stoffa", "#5a4a38", 0.1, 0.1, 0, 0.206, 0.012, 0.206, rz=90)]


def nicchia():
    """Fondo di tufo con filari di mattoni, ombra finta in alto e ai lati, stipiti e architrave di pietra."""
    p = [parte("fondo", "cubo", "pietra", "#8a7358", 0.0, 0.55, 0.0, 0.04, 1.1, 0.8)]
    for i in range(7):  # filari
        p.append(parte("filare%d" % i, "cubo", "pietra", "#5a4733", -0.022, 0.08 + i * 0.15, 0.0, 0.006, 0.012, 0.8))
        for k in range(3):  # giunti verticali sfalsati
            p.append(parte("giunto%d_%d" % (i, k), "cubo", "pietra", "#5a4733", -0.022, 0.155 + i * 0.15, -0.28 + k * 0.28 + (0.14 if i % 2 else 0), 0.006, 0.14, 0.012))
    p += [parte("ombra_alto", "cubo", "pietra", "#241a12", -0.026, 1.02, 0.0, 0.008, 0.16, 0.8),
          parte("ombra_sx", "cubo", "pietra", "#2a1e15", -0.026, 0.5, -0.33, 0.008, 1.0, 0.14),
          parte("ombra_dx", "cubo", "pietra", "#2a1e15", -0.026, 0.5, 0.33, 0.008, 1.0, 0.14),
          parte("stipite_a", "cubo", "pietra", "#8a7a64", 0.0, 0.55, -0.42, 0.07, 1.14, 0.06),
          parte("stipite_b", "cubo", "pietra", "#8a7a64", 0.0, 0.55, 0.42, 0.07, 1.14, 0.06),
          parte("architrave", "cubo", "pietra", "#8a7a64", 0.0, 1.12, 0.0, 0.07, 0.06, 0.92)]
    return p


NICCHIA = nicchia()


def graffi():
    """Archi sul cotto: la credenza e' stata fatta ruotare tante volte attorno al suo spigolo sud (perno a -0,19 / -0,58 dal punto)."""
    import math
    px, pz = -0.19, -0.58
    parti = []
    n = 0
    for r, a0, a1, k in ((1.16, 6, 78, 9), (1.30, 14, 70, 7), (0.78, 10, 60, 5)):
        for i in range(k):
            f = math.radians(a0 + (a1 - a0) * (i + 0.5) / k)
            lung = r * math.radians((a1 - a0) / k) * 0.8
            x = px + r * math.sin(f)
            z = pz + r * math.cos(f)
            parti.append(parte("g%d" % n, "cubo", "legno", "#2f251d", round(x, 3), 0.002, round(z, 3), 0.014, 0.002, round(lung, 3), ry=round(90 + math.degrees(f), 1)))
            n += 1
    return parti


GRAFFI = graffi()


def biglietto(simbolo):
    nome = {"scarpa": "bigliettoscarpa", "mela": "bigliettomela", "treno": "bigliettotreno"}[simbolo]
    return [parte("carta", "cubo", "tex:" + nome, "", 0, 0.002, 0, 0.12, 0.003, 0.09)]


# ----------------------------------------------------------------------------- testi
T = {
    "contatoreFmt": "{0}/{1}",
    "contatoreEtichetta": "Segreti trovati",
    "stanzaFmt": "{0}   {1}/{2}",
    "casellaPiena": "[x]",
    "casellaVuota": "[  ]",
    "casellaIgnota": "ancora da trovare",
    "quadernoRiga": "Aggiunto ai segreti del quaderno.",
    "dietroQuinteTitolo": "Dietro le quinte",
    "aiutoLieve": "In questa stanza c'è ancora qualcosa…",
    "richiedeGenerico": "",
    "campanaSbagliata": "Non è questo il giro. Si ricomincia da capo: nessuna fretta.",
    "campanaFatta": "In fondo alla piazzetta, qualcosa si è mosso con un fruscio di pietra.",
    "raccogli": "{0}",
    "guarda": "{0}",
    "quadernoVuoto": "",
}

STANZE = [
    {"scena": HUB, "nome": "Il vicolo", "completo": ["fb:flashback_scarpe"]},
    {"scena": BASSO, "nome": "Il basso di Antonietta", "completo": ["ris:mondo/enigma_baule"]},
    {"scena": RIC, "nome": "Il ricordo delle scarpe", "completo": [
        "ris:flashback_scarpe/enigma_passanti", "ris:flashback_scarpe/enigma_nascosto", "ris:flashback_scarpe/enigma_quiz",
        "ris:flashback_scarpe/enigma_ordina", "ris:flashback_scarpe/enigma_lucchetto"]},
]

BQ_HUB = ("Il primo convoglio dei «treni della felicità» partì da Milano il 16 dicembre 1945 con circa 1.800 bambini, diretto a Reggio Emilia. "
          "L'iniziativa fu ideata da Teresa Noce e dall'UDI e promossa dal PCI, con il sostegno di ANPI, CGIL e Croce Rossa, e durò fino al 1952: "
          "in tutto circa 70.000 bambini del Sud e del Centro furono ospitati da famiglie dell'Emilia-Romagna, della Toscana, delle Marche, dell'Umbria, "
          "del Veneto e della Lombardia, di solito durante i mesi freddi.")
BQ_BASSO = ("Un basso è una casa al piano della strada, che si apre direttamente sul vicolo. Da Napoli, tra il Natale del 1946 e l'estate del 1947, partirono "
            "circa 10.000-12.000 bambini (le fonti non coincidono sul numero). Prima di partire venivano lavati, vaccinati, nutriti, schedati e vestiti. "
            "L'organizzazione era del Comitato per la salvezza dei bambini di Napoli, attivo dal 1946 al 1954.")
BQ_RIC = ("Le famiglie che accoglievano i bambini erano spesso contadine e operaie, non ricche. L'accoglienza era organizzata dalle sezioni dell'UDI e del PCI, "
          "dalle amministrazioni locali e dalle scuole; a Modena i bambini napoletani arrivarono dal 1946 al 1948. "
          "Non tutti si fidavano: circolavano voci false su cosa sarebbe successo ai bambini, e molte madri dovettero decidere comunque.")

SEGRETI = []
PEZZI = []
ENIGMI = []
CAMPANE = []
UOVA = []
GETTONI = []

# ============================================================================= HUB
SEGRETI += [
    segreto("hub_edicole", HUB, "enigma", "Le tre edicole",
            "Tre biglietti nelle edicole del vicolo, una filastrocca da rimettere in ordine, una portella nel sottoscala: la scatola dei ragazzi del vicolo.",
            1, "Sottoscala del cortile C3", BQ_HUB),
    segreto("hub_cartolina", HUB, "collezionabile", "Cartolina del golfo",
            "Una cartolina del golfo di Napoli con il Vesuvio sullo sfondo, mai spedita: manca il francobollo e non c'è nessun indirizzo. "
            "Qualcuno la teneva in una valigia per ricordarsi la città: chi partiva portava con sé un pezzo di casa.",
            2, "Valigie dimenticate al largo della stazione"),
    segreto("hub_settebello", HUB, "collezionabile", "Il settebello",
            "Le carte napoletane sono un mazzo di quaranta carte con quattro semi: denari, coppe, spade e bastoni. Il sette di denari, il «settebello», "
            "è la carta che conta di più nella scopa, il gioco che si faceva sui tavoli dei cortili. Qualcuno ha lasciato la partita a metà.",
            3, "Tavolo da gioco del cortile C3"),
    segreto("hub_trottola", HUB, "collezionabile", "Una trottola di legno",
            "Un pezzo di legno, un chiodo e un po' di spago: la trottola si faceva in casa o costava pochi soldi. Quando i giocattoli veri erano troppo cari, "
            "i bambini ne inventavano di propri con quello che trovavano.",
            4, "Pozzo del cortile C1"),
    segreto("hub_amlire", HUB, "collezionabile", "Una banconota AM-lire",
            "Dal 1943 le truppe alleate misero in circolazione in Italia banconote proprie, le AM-lire (Allied Military Currency). Nel Sud circolavano "
            "insieme alle lire italiane. Il fregio è fitto di rosoni e righe sottili, e non porta nessuna cifra leggibile. Ripiegata in una nicchia: qualcuno nascondeva i risparmi dove poteva.",
            5, "Sotto il grande arco"),
    segreto("hub_ritaglio", HUB, "collezionabile", "Ritaglio: partono i bambini",
            "Un ritaglio scritto alla maniera dei giornali di allora (il testo è nostro): racconta il primo convoglio del 16 dicembre 1945, da Milano a Reggio Emilia, "
            "e i circa 70.000 bambini che nei sette anni seguenti partirono per il Nord.",
            6, "Dietro le macerie del lotto"),
]
GETTONI += [
    {"id": "bigl_scarpa", "nome": "Biglietto della scarpa", "descrizione": "Trovato nell'edicola della traversa bassa: «Prima le scarpe, che sanno la strada.»"},
    {"id": "bigl_mela", "nome": "Biglietto della mela", "descrizione": "Trovato nell'edicola della traversa alta: «Poi la mela, rossa sul davanzale.»"},
    {"id": "bigl_treno", "nome": "Biglietto del treno", "descrizione": "Trovato nell'edicola grande: «E alla fine il treno, che parte all'alba.»"},
    {"id": "numero_campana", "nome": "Il numero sul graffito", "descrizione": "Sotto «C'ero anch'io», inciso con un chiodo: 4 2 7 1."},
]
PEZZI += [
    pezzo("bigl_scarpa", HUB, "Edicola piccola (traversa T3)", biglietto("scarpa"), "indizio", prompt="Un biglietto fra i fiori", titolo="Un biglietto fra i fiori", appoggio="fisso", dx=0.15, dy=1.203, dz=0.98,
          msg="Un biglietto piegato in quattro, infilato fra i fiori secchi. Sopra, una scarpa disegnata a carboncino e due righe:\n\n«PRIMA LE SCARPE\nche sanno la strada.»",
          scala=1.5, raggio=0.4),
    pezzo("bigl_mela", HUB, "Edicola piccola (traversa T5)", biglietto("mela"), "indizio", prompt="Un biglietto fra i fiori", titolo="Un biglietto fra i fiori", appoggio="fisso", dx=-0.25, dy=1.203, dz=-1.0,
          msg="Un biglietto piegato in quattro, nascosto dietro il lumino. Sopra, una mela disegnata a carboncino e due righe:\n\n«POI LA MELA\nrossa sul davanzale.»",
          scala=1.5, raggio=0.4),
    pezzo("bigl_treno", HUB, "Edicola votiva grande", biglietto("treno"), "indizio", prompt="Un biglietto fra i fiori", titolo="Un biglietto fra i fiori", appoggio="suolo", dx=-0.25, dy=0.0, dz=-1.6,
          msg="Un biglietto piegato in quattro, caduto fra i fiori secchi ai piedi dell'edicola grande. Sopra, un trenino disegnato a carboncino e due righe:\n\n«E ALLA FINE IL TRENO\nche parte all'alba.»",
          scala=1.5, raggio=0.4),
    pezzo("hub_cartolina", HUB, "Valigie dimenticate al largo della stazione", cartolina(), "raccogli", "hub_cartolina", "Una cartolina", dx=0.3, scala=1.6),
    pezzo("hub_settebello", HUB, "Tavolo da gioco del cortile C3", carta_napoletana(), "raccogli", "hub_settebello", "Una carta per terra", dx=0.2, scala=1.8, raggio=0.4),
    pezzo("hub_trottola", HUB, "Pozzo del cortile C1", trottola(), "raccogli", "hub_trottola", "Una trottola", dx=1.05, scala=1.5),
    pezzo("hub_amlire", HUB, "Sotto il grande arco", banconota_am(), "raccogli", "hub_amlire", "Una banconota ripiegata", dx=-0.4, scala=1.6),
    pezzo("hub_ritaglio", HUB, "Dietro le macerie del lotto", ritaglio(), "raccogli", "hub_ritaglio", "Un ritaglio di giornale", dx=-1.5, dz=3.8, scala=1.6,
          msg="PARTONO I BAMBINI\n\nIl 16 dicembre 1945 un treno carico di bambini lascia Milano e arriva a Reggio Emilia: sono circa 1.800. "
              "È il primo dei «treni della felicità». A Napoli un Comitato per la salvezza dei bambini prepara le partenze."),
]
ENIGMI.append({
    "id": "enigma_portella", "tipo": "oggetto", "titolo": "La portella del sottoscala", "meccanismo": "oggetto", "scena": HUB, "segreto": "hub_edicole",
    "consegna": "Nel fianco della scala esterna c'è una portella di assi, chiusa da tre tasselli dipinti. Non è una serratura: è una filastrocca.",
    "temaCivico": "Memoria collettiva: i ricordi dei bambini sono documenti anche loro",
    "punto": "Sottoscala del cortile C3", "dx": 0, "dy": 0, "dz": 0.12, "yaw": 0, "scala": 1.0, "distanza": 1.3, "inclina": 6,
    "prompt": "la portella del sottoscala", "ordinata": True, "soluzione": ["scarpa", "mela", "treno"],
    "richiede": ["bigl_scarpa", "bigl_mela", "bigl_treno"],
    "bloccatoMsg": "Tre tasselli dipinti: una scarpa, una mela, un treno. Ma in che ordine? Forse qualcuno, in giro per il vicolo, ha lasciato scritto come va la filastrocca.",
    "erroreMsg": "I tasselli tornano su da soli. Non è questo l'ordine della filastrocca: riprova con calma.",
    "indizi": [],
    "spiegazione": "I tasselli scattano uno dopo l'altro e la portella si apre. Dentro c'è la scatola dei ragazzi del vicolo: un treno disegnato col carbone su un foglio, "
                   "e sotto, a matita, tanti nomi di bambini. Giocavano a «partire» molto prima del treno vero.",
    "scheda": "Quello che i bambini lasciano di sé (disegni, filastrocche, nomi) è una fonte storica: ci racconta come vivevano e cosa speravano, quando gli adulti scrivevano d'altro.",
    "appunto": "La scatola dei ragazzi del vicolo: un treno disegnato col carbone e una fila di nomi.",
    "parti": [
        parte("telaio", "cubo", "legno", "#3b2d20", 0, 0.36, 0.02, 0.66, 0.74, 0.04),
        parte("asse1", "cubo", "legno", "#5a4531", -0.2, 0.36, -0.01, 0.2, 0.7, 0.03),
        parte("asse2", "cubo", "legno", "#4e3b2a", 0.0, 0.36, -0.012, 0.2, 0.7, 0.03),
        parte("asse3", "cubo", "legno", "#604a35", 0.2, 0.36, -0.01, 0.2, 0.7, 0.03),
        parte("scarpa", "vuoto", "", "", -0.2, 0.42, -0.04, 0.12, 0.12, 0.04, clic=True, azione="premi", evento="scarpa", asse="-z", msg="Il tassello con la scarpa scatta."),
        parte("scarpa_base", "cubo", "gesso", "#cfc7b0", 0.0, 0.0, 0.0, 0.12, 0.12, 0.025, genitore="scarpa"),
        parte("scarpa_segno", "scarpa", "", "#2a1f16", 0.0, -0.03, -0.014, 0.3, 0.3, 0.3, testo="lucide", genitore="scarpa"),
        parte("mela", "vuoto", "", "", 0.0, 0.42, -0.04, 0.12, 0.12, 0.04, clic=True, azione="premi", evento="mela", asse="-z", msg="Il tassello con la mela scatta."),
        parte("mela_base", "cubo", "gesso", "#cfc7b0", 0.0, 0.0, 0.0, 0.12, 0.12, 0.025, genitore="mela"),
        parte("mela_segno", "sfera", "stoffa", "#9c2f26", 0.0, 0.0, -0.02, 0.07, 0.07, 0.03, genitore="mela"),
        parte("treno", "vuoto", "", "", 0.2, 0.42, -0.04, 0.12, 0.12, 0.04, clic=True, azione="premi", evento="treno", asse="-z", msg="Il tassello con il treno scatta."),
        parte("treno_base", "cubo", "gesso", "#cfc7b0", 0.0, 0.0, 0.0, 0.12, 0.12, 0.025, genitore="treno"),
        parte("treno_segno", "cubo", "metallo", "#262626", 0.0, 0.0, -0.016, 0.085, 0.04, 0.012, genitore="treno"),
        parte("treno_camino", "cilindro", "metallo", "#262626", -0.028, 0.026, -0.016, 0.014, 0.012, 0.014, genitore="treno"),
        parte("scatola", "cubo", "legno", "#4a3a2a", 0.0, 0.12, -0.18, 0.34, 0.2, 0.24, nascosta=True, quando="risolto"),
        parte("foglio", "cubo", "carta", "#d3c8a8", 0.0, 0.225, -0.18, 0.3, 0.006, 0.2, nascosta=True, quando="risolto"),
        parte("disegno", "vuoto", "", "", 0.0, 0.232, -0.18, 0.1, 0.1, 0.1, nascosta=True, quando="risolto"),
        parte("carbone_a", "cubo", "metallo", "#1a1a1a", -0.08, 0.0, 0.0, 0.07, 0.004, 0.04, genitore="disegno"),
        parte("carbone_b", "cubo", "metallo", "#1a1a1a", 0.0, 0.0, 0.0, 0.07, 0.004, 0.04, genitore="disegno"),
        parte("carbone_c", "cubo", "metallo", "#1a1a1a", 0.09, 0.0, 0.0, 0.08, 0.004, 0.05, genitore="disegno"),
    ],
})
UOVA += [
    {"id": "violino_chiesetta", "scena": HUB, "tipo": "violino", "punto": "Gradino della chiesetta", "dx": 0.0, "dy": 0.8, "dz": 0.0,
     "x": pos(HUB, "Gradino della chiesetta", 0, 0.8, 0)[0], "y": pos(HUB, "Gradino della chiesetta", 0, 0.8, 0)[1], "z": pos(HUB, "Gradino della chiesetta", 0, 0.8, 0)[2],
     "quando": ["flag:finale_visto"], "raggio": 16.0, "volume": 0.28,
     "nota": "Aggancio per R016: a fine gioco chiamare TrenoBambini.Segreti.SegretiStato.ImpostaFlag(\"finale_visto\"). Da quel momento, tornando al gradino della chiesetta, si sente da lontano il tema del violino."},
]

# ============================================================================= BASSO
SEGRETI += [
    segreto("basso_ripostiglio", BASSO, "enigma", "Il ripostiglio murato",
            "Dietro i piatti della credenza un fondo finto, una leva, e la credenza che si sposta: un ripostiglio murato con il materassino dove dormiva Amerigo d'estate.",
            1, "Credenza della stanza principale", BQ_BASSO),
    segreto("basso_santino", BASSO, "collezionabile", "Un santino",
            "Un santino della Madonna, ingiallito e piegato a metà, appuntato dietro la tenda dell'alcova. Nelle case dei bassi le immagini sacre stavano accanto al letto: "
            "erano compagnia e protezione.", 2, "Retro della tenda dell'alcova"),
    segreto("basso_ditale", BASSO, "collezionabile", "Il ditale di Antonietta",
            "Un ditale d'ottone consumato dall'uso, nascosto sotto una mattonella. Rammendare era un lavoro di tutti i giorni: con un ditale, un ago e molta pazienza "
            "un vestito poteva durare molto più di quanto sembrasse.", 3, "Mattonella sconnessa sotto il tavolo"),
    segreto("basso_bottone", BASSO, "collezionabile", "Il bottone della salopette",
            "Un bottone di metallo, staccato da una salopette ormai liscia di rattoppi. Chi aveva pochi vestiti li aggiustava all'infinito: un bottone perso era un guaio vero, "
            "e qualcuno lo ha messo da parte per riattaccarlo.", 4, "Pentole appese della cucina"),
    segreto("basso_biglia", BASSO, "collezionabile", "Una biglia di vetro",
            "Una biglia di vetro azzurrata, finita dietro il lavatoio del cortile. Le biglie erano il gioco di strada per eccellenza: bastavano una buca, una riga e tanta pazienza.",
            5, "Lavatoio del cortile"),
]
PEZZI += [
    pezzo("basso_santino", BASSO, "Retro della tenda dell'alcova", santino(), "raccogli", "basso_santino", "Un santino appuntato", appoggio="fisso", dx=-0.35, dy=0.15, dz=-0.165, rx=-90, scala=1.8, raggio=0.45),
    pezzo("basso_mattonella", BASSO, "Mattonella sconnessa sotto il tavolo",
          [parte("sporco", "cubo", "pietra", "#241812", 0, 0.002, 0, 0.318, 0.004, 0.318, ry=2),
           parte("lastra", "cubo", "pietra", "#6e3428", 0, 0.007, 0, 0.30, 0.008, 0.30, ry=2),
           parte("crepa", "cubo", "pietra", "#2c1c16", 0.03, 0.0115, 0.02, 0.1, 0.001, 0.004, ry=35)],
          "apri", "", "Una mattonella sollevata", msg="La mattonella è sciolta: sotto c'è una piccola cavità.", raggio=0.4, appoggio="suolo"),
    pezzo("basso_ditale", BASSO, "Mattonella sconnessa sotto il tavolo", ditale(), "raccogli", "basso_ditale", "Un ditale", appare=["basso_mattonella"], scala=1.8, raggio=0.4),
    pezzo("basso_bottone", BASSO, "Pentole appese della cucina", bottone_ferro(), "raccogli", "basso_bottone", "Un bottone sotto le pentole", dx=-0.25, dy=-1.6, dz=0.85, scala=1.8, raggio=0.4),
    pezzo("basso_biglia", BASSO, "Lavatoio del cortile", biglia(), "raccogli", "basso_biglia", "Una biglia", dx=-0.15, dy=-0.5, dz=-1.45, scala=1.8, raggio=0.4),
    # graffi a semicerchio sul pavimento, davanti alla credenza: la credenza e' stata spostata tante volte
    pezzo("basso_graffi", BASSO, "Credenza della stanza principale", GRAFFI, "decoro", dy=-1.0, appoggio="suolo"),
    pezzo("basso_nicchia", BASSO, "Credenza della stanza principale", NICCHIA, "decoro", dx=-0.17, dy=-1.0, dz=0.2, appoggio="fisso", appare=["enigma_ripostiglio"]),
    pezzo("basso_materassino", BASSO, "Credenza della stanza principale", materassino(), "lettura", "", "Un materassino arrotolato", titolo="Il materassino",
          msg="Un materassino di crine, arrotolato e legato con lo spago, nascosto dietro la credenza. D'estate, quando nel basso faceva troppo caldo, Amerigo dormiva qui, "
              "in questo cantuccio di muro fresco. (Un'invenzione del gioco: il resto è storia vera.)",
          dx=0.0, dy=-0.997, dz=0.25, appoggio="fisso", appare=["enigma_ripostiglio"], raggio=0.35),
    pezzo("basso_credenza_scorre", BASSO, "Credenza della stanza principale", [], "decoro", dx=0, dz=0, appoggio="fisso",
          scorri="enigma_ripostiglio", slide=(0.0, 0.0, -0.8), altro="Credenza,LampadaCredenza,FiammaLampada,SvegliaRotta"),
]
ENIGMI.append({
    "id": "enigma_ripostiglio", "tipo": "oggetto", "titolo": "I piatti della credenza", "meccanismo": "oggetto", "scena": BASSO, "segreto": "basso_ripostiglio",
    "consegna": "Sulla parete, fra il calendario e l'angolo, una mensola con tre piatti in piedi. Sul pavimento, davanti alla credenza, graffi a semicerchio: qualcuno l'ha spostata molte volte.",
    "temaCivico": "Le case dei bassi: come si viveva, e cosa si custodiva",
    "punto": "Credenza della stanza principale", "dx": -0.17, "dy": 0.45, "dz": 1.67, "yaw": -90, "scala": 1.0, "distanza": 1.3, "inclina": 8,
    "prompt": "i piatti sulla mensola", "soluzione": ["piatto_1", "piatto_2", "piatto_3", "leva"], "ordinata": False,
    "bloccatoMsg": "", "erroreMsg": "Niente da fare: il legno non si muove. Guarda meglio.", "indizi": [],
    "spiegazione": "Dietro i piatti il fondo della mensola è finto. Una leva di ferro, e un colpo secco: la credenza scivola via lungo i graffi e lascia scoperto un ripostiglio murato. "
                   "Dentro c'è il materassino dove Amerigo dormiva d'estate.",
    "scheda": "Nei bassi ogni angolo serviva: il ripostiglio murato racconta la fatica di far stare in poco spazio un'intera famiglia e quello che si voleva tenere al sicuro.",
    "appunto": "Un ripostiglio murato dietro la credenza: il materassino dove dormiva Amerigo d'estate.",
    "parti": [
        parte("mensola", "cubo", "legno", "#4b3828", 0.0, -0.01, -0.06, 0.5, 0.03, 0.14),
        parte("staffa_a", "cubo", "legno", "#3a2c20", -0.2, -0.05, -0.04, 0.03, 0.08, 0.1),
        parte("staffa_b", "cubo", "legno", "#3a2c20", 0.2, -0.05, -0.04, 0.03, 0.08, 0.1),
        parte("fondo", "cubo", "legno", "#3a2c20", 0.0, 0.25, 0.02, 0.5, 0.5, 0.02),
        parte("asse_finta", "cubo", "legno", "#46362a", 0.0, 0.25, 0.008, 0.16, 0.46, 0.008),
        parte("gesso_croce", "cubo", "gesso", "#cfc7b0", 0.0, 0.25, 0.002, 0.05, 0.004, 0.003),
        parte("piatto_1", "vuoto", "", "", -0.155, 0.1, -0.06, 0.15, 0.03, 0.15, rx=80, clic=True, azione="nascondi", evento="piatto_1", msg="Un piatto d'altri tempi, scheggiato sul bordo. Dietro, il legno suona un po' vuoto."),
        parte("bordo_1", "cilindro", "stoffa", "#3a5578", 0.0, 0.003, 0.0, 0.162, 0.006, 0.162, genitore="piatto_1"),
        parte("disco_1", "cilindro", "gesso", "#d9d6c8", 0.0, 0.0, 0.0, 0.15, 0.012, 0.15, genitore="piatto_1"),
        parte("anello_1", "cilindro", "stoffa", "#3f5f86", 0.0, -0.007, 0.0, 0.1, 0.004, 0.1, genitore="piatto_1"),
        parte("piatto_2", "vuoto", "", "", 0.0, 0.1, -0.06, 0.15, 0.03, 0.15, rx=80, clic=True, azione="nascondi", evento="piatto_2", msg="Anche questo è scheggiato. Pesa poco."),
        parte("bordo_2", "cilindro", "stoffa", "#3a5578", 0.0, 0.003, 0.0, 0.162, 0.006, 0.162, genitore="piatto_2"),
        parte("disco_2", "cilindro", "gesso", "#d9d6c8", 0.0, 0.0, 0.0, 0.15, 0.012, 0.15, genitore="piatto_2"),
        parte("anello_2", "cilindro", "stoffa", "#3f5f86", 0.0, -0.007, 0.0, 0.1, 0.004, 0.1, genitore="piatto_2"),
        parte("piatto_3", "vuoto", "", "", 0.155, 0.1, -0.06, 0.15, 0.03, 0.15, rx=80, clic=True, azione="nascondi", evento="piatto_3", msg="Ora la parete dietro è libera: c'è un'asse che sembra di un altro legno."),
        parte("bordo_3", "cilindro", "stoffa", "#3a5578", 0.0, 0.003, 0.0, 0.162, 0.006, 0.162, genitore="piatto_3"),
        parte("disco_3", "cilindro", "gesso", "#d9d6c8", 0.0, 0.0, 0.0, 0.15, 0.012, 0.15, genitore="piatto_3"),
        parte("anello_3", "cilindro", "stoffa", "#3f5f86", 0.0, -0.007, 0.0, 0.1, 0.004, 0.1, genitore="piatto_3"),
        parte("leva", "cilindro", "metallo", "#2d2a27", 0.0, 0.25, -0.02, 0.03, 0.05, 0.03, rx=90, clic=True, azione="gira", evento="leva", asse="y", nascosta=True, quando="piatto_3",
              msg="La leva cede con un colpo secco. Si sente la credenza muoversi, di là."),
    ],
})
UOVA_BASSO = []

# ============================================================================= RICORDO
SEGRETI += [
    segreto("ric_campana", RIC, "enigma", "La campana di gesso",
            "Un graffito inciso con un chiodo dà una sequenza di numeri; la campana disegnata col gesso si salta in quell'ordine; la pietra della fontanella scivola via e il soldo promesso compare.",
            1, "Campana disegnata col gesso", BQ_RIC),
    segreto("ric_moneta", RIC, "collezionabile", "Una moneta di rame",
            "Una moneta di rame consumata, di quelle di prima della guerra: il valore quasi non si legge più. Era rimasta fra le assi della soglia del barbiere, persa da qualcuno che non aveva tempo di cercarla.",
            2, "Soglia del barbiere"),
    segreto("ric_pallone", RIC, "collezionabile", "Il pallone di stracci",
            "Un pallone cucito con stracci e spago. Senza i soldi per un pallone vero i ragazzi se lo facevano così: pesava, rimbalzava male, ma bastava per giocare fino a sera.",
            3, "Pallone di stracci"),
    segreto("ric_lista", RIC, "collezionabile", "La lista della spesa",
            "Un biglietto scritto a matita, nascosto sotto la verdura di una cassa: una lista di scarpe, con i numeri e i nomi di alcuni bambini del vicolo. Qualcuno stava facendo i conti di quante paia ne servissero.",
            4, "Cassa di frutta del fruttivendolo"),
    segreto("ric_uovo", RIC, "collezionabile", "L'uovo e il biglietto",
            "Sotto la paglia delle galline, un uovo ancora tiepido e un biglietto: «Per chi ha fame e non lo dice.» Quando mangiare era un problema, un uovo valeva più di quanto si pensi.",
            5, "Gabbie delle galline"),
    segreto("ric_bottone", RIC, "collezionabile", "Il bottone d'oro",
            "Nella tasca del cappotto sul manichino, un bottone dorato. Il sarto lavorava su misura: per i clienti ricchi anche i bottoni erano un lusso, e lì il cappotto aspettava il suo padrone.",
            6, "Manichino del sarto"),
]
GETTONI += [{"id": "campana_ok", "nome": "La campana", "descrizione": ""}]
PEZZI += [
    pezzo("ric_graffito", RIC, "Graffito 'C'ero anch'io'",
          [parte("tavola", "cubo", "pietra", "#7a6e5c", 0, 0.0, 0.0, 0.7, 0.34, 0.01, ),
           parte("scritta", "testo", "gesso", "#e9e5d6", 0, 0.07, -0.01, 0.62, 0.14, 1, testo="C'ERO ANCH'IO", dimensione=0.05, font="mano"),
           parte("incisione", "testo", "pietra", "#2a241c", 0, -0.07, -0.01, 0.62, 0.1, 1, testo="T.A. 1946   4 2 7 1", dimensione=0.032, font="mano")],
          "lettura", "", "Un graffito sul muro", titolo="C'ero anch'io", appoggio="fisso", yaw=90, dx=0.0, dy=0.0, dz=0.0, raggio=0.7,
          msg="Sul muro, scritto col gesso: «C'ERO ANCH'IO». Sotto, inciso con la punta di un chiodo, quasi invisibile: «T.A. 1946» e quattro numeri: 4 2 7 1.",
          produce=["numero_campana"]),
    pezzo("ric_moneta", RIC, "Soglia del barbiere", moneta(), "raccogli", "ric_moneta", "Una moneta fra le assi", scala=1.8, raggio=0.4),
    pezzo("ric_pallone", RIC, "Pallone di stracci", pallone_stracci(), "raccogli", "ric_pallone", "Un pallone di stracci", dx=1.5, scala=1.0, raggio=0.55),
    pezzo("ric_lista", RIC, "Cassa di frutta del fruttivendolo", biglietto_spesa(), "raccogli", "ric_lista", "Un biglietto sotto la verdura", appoggio="fisso", dy=0.225, scala=1.7, raggio=0.4),
    pezzo("ric_uovo", RIC, "Gabbie delle galline", uovo_paglia(), "raccogli", "ric_uovo", "Un uovo sotto la paglia", scala=1.5, raggio=0.4),
    pezzo("ric_bottone", RIC, "Manichino del sarto", moneta("#d2a93e", 0.026), "raccogli", "ric_bottone", "Un bottone dorato", appoggio="fisso", dx=0.28, dy=-0.4, dz=-0.15, rz=-90, scala=1.8, raggio=0.4),
    pezzo("ric_cavita", RIC, "Fontanella della piazzetta",
          [parte("buio", "cubo", "pietra", "#15130f", 0.0, 0.004, 0.0, 0.40, 0.008, 0.30),
           parte("orlo_a", "cubo", "pietra", "#5a5347", 0.0, 0.012, -0.155, 0.43, 0.02, 0.025),
           parte("orlo_b", "cubo", "pietra", "#5a5347", 0.0, 0.012, 0.155, 0.43, 0.02, 0.025),
           parte("orlo_c", "cubo", "pietra", "#5a5347", -0.205, 0.012, 0.0, 0.025, 0.02, 0.28),
           parte("orlo_d", "cubo", "pietra", "#5a5347", 0.205, 0.012, 0.0, 0.025, 0.02, 0.28)],
          "decoro", appoggio="fisso", dy=0.0, dz=-1.6),
    pezzo("ric_pietra", RIC, "Fontanella della piazzetta",
          [parte("pietra", "cubo", "pietra", "#6f675a", 0.0, 0.06, 0.0, 0.36, 0.10, 0.26, ry=3),
           parte("smusso", "cubo", "pietra", "#7d7567", 0.0, 0.112, 0.0, 0.33, 0.014, 0.23, ry=3),
           parte("segno", "cubo", "gesso", "#b9b3a2", 0.04, 0.121, 0.02, 0.07, 0.002, 0.006, ry=3)],
          "decoro", appoggio="fisso", dy=0.0, dz=-1.6, scorri="campana_ok", slide=(0.5, 0.0, 0.0)),
    pezzo("ric_soldo", RIC, "Fontanella della piazzetta", moneta("#9a6a3a", 0.034), "raccogli", "ric_campana_soldo", "Un soldo nella cavità", appoggio="fisso", dz=-1.6, dy=0.005,
          appare=["campana_ok"], scala=1.8, raggio=0.45, msg="Il soldo che Tommasino aveva promesso ad Amerigo. (Un'invenzione del gioco.)", titolo="Il soldo promesso"),
]
CAMPANE.append({
    "id": "campana_gesso", "scena": RIC, "punto": "Campana disegnata col gesso", "dx": 0.0, "dz": 0.0,
    "x": pos(RIC, "Campana disegnata col gesso")[0], "y": pos(RIC, "Campana disegnata col gesso")[1], "z": pos(RIC, "Campana disegnata col gesso")[2],
    "yaw": 0.0, "sequenza": "4271", "produce": "campana_ok", "richiede": "numero_campana", "sosta": 0.35,
    "caselle": [
        {"n": 1, "x": 0.0, "z": 0.0, "w": 0.8, "d": 0.8},
        {"n": 2, "x": 0.0, "z": 0.85, "w": 0.8, "d": 0.8},
        {"n": 3, "x": -0.42, "z": 1.7, "w": 0.8, "d": 0.8},
        {"n": 4, "x": 0.42, "z": 1.7, "w": 0.8, "d": 0.8},
        {"n": 5, "x": 0.0, "z": 2.55, "w": 0.8, "d": 0.8},
        {"n": 6, "x": -0.42, "z": 3.4, "w": 0.8, "d": 0.8},
        {"n": 7, "x": 0.42, "z": 3.4, "w": 0.8, "d": 0.8},
        {"n": 8, "x": 0.0, "z": 4.25, "w": 0.8, "d": 0.8},
    ],
})
# il soldo e' il premio dell'enigma: trovarlo completa il segreto
for p in PEZZI:
    if p["id"] == "ric_soldo":
        p["segreto"] = "ric_campana"
UOVA += [
    {"id": "scarpine_filo", "scena": RIC, "tipo": "scarpe", "punto": "Scarpe appese al filo della luce", "dx": 0.6, "dy": 0.05, "dz": 0.0,
     "x": pos(RIC, "Scarpe appese al filo della luce", 0.6, 0.05, 0)[0], "y": pos(RIC, "Scarpe appese al filo della luce", 0.6, 0.05, 0)[1], "z": pos(RIC, "Scarpe appese al filo della luce", 0.6, 0.05, 0)[2],
     "quando": ["fb:flashback_cappotto"], "raggio": 0.0, "volume": 0.0,
     "nota": "Aggancio per il capitolo Cappotto (R014): quando il suo flashback e' completato (flashbackCompletati contiene l'id), nel ricordo delle scarpe compare un paio piccolo in piu' sul filo."},
]


# ============================================================================= STRACCI (R012)
STANZE.append({"scena": STR, "nome": "Il ricordo degli stracci", "completo": [
    "ris:flashback_stracci/enigma_giro", "ris:flashback_stracci/enigma_ceste", "ris:flashback_stracci/enigma_bilancia",
    "ris:flashback_stracci/enigma_conto", "ris:flashback_stracci/enigma_mestieri"]})
BQ_STR = ("Nel dopoguerra molti bambini delle città italiane aiutavano le famiglie con piccoli lavori, come raccogliere materiali di recupero (stracci, ferri vecchi, carta) "
          "da rivendere. Oggi la legge italiana fissa l'età minima per lavorare a 16 anni (legge n. 977 del 1967 e successive modifiche) e la Costituzione (art. 37) tutela il lavoro dei minori.")


def tappo():
    return [parte("disco", "cilindro", "metallo", "#8c8a80", 0, 0.004, 0, 0.034, 0.004, 0.034),
            parte("bordo", "cilindro", "metallo", "#6a685f", 0, 0.002, 0, 0.038, 0.002, 0.038),
            parte("cuore", "cilindro", "metallo", "#a3402f", 0, 0.0085, 0, 0.022, 0.0008, 0.022)]


def bottone_militare():
    return [parte("disco", "cilindro", "ottone", "#b59a3f", 0, 0.004, 0, 0.03, 0.004, 0.03),
            parte("stella", "cilindro", "ottone", "#8a7430", 0, 0.0085, 0, 0.018, 0.0012, 0.018),
            parte("anello", "cilindro", "ottone", "#c9ac4a", 0, 0.0075, 0, 0.028, 0.0006, 0.028)]


def carta_gioco():
    return [parte("cartone", "cubo", "carta", "#d8cba6", 0, 0.002, 0, 0.065, 0.003, 0.10, ry=14),
            parte("seme", "sfera", "stoffa", "#9a3a2e", 0, 0.0045, 0, 0.026, 0.004, 0.026, ry=14)]


def biglia_rossa():
    return [parte("vetro", "sfera", "vetro", "#a85a3a", 0, 0.016, 0, 0.032, 0.032, 0.032),
            parte("anima", "sfera", "vetro", "#e8dcc0", 0, 0.016, 0, 0.012, 0.012, 0.012)]


def ritaglio_stracci():
    return [parte("foglio", "cubo", "tex:ritagliostracci", "", 0, 0.002, 0, 0.12, 0.003, 0.15, ry=-8)]


SEGRETI += [
    segreto("str_carretto", STR, "enigma", "Il cassetto del carretto",
            "Sotto il sedile del carretto di Capa 'e fierro c'è un cassetto che si apre girando la ruota: dentro, un foglietto con i conti scritti a matita.",
            1, "Carretto di Capa 'e fierro", BQ_STR),
    segreto("str_tappo", STR, "collezionabile", "Un tappo a corona",
            "Un tappo di bottiglia schiacciato dai passi, fra i mattoni del lotto bombardato. Anche i tappi servivano: schiacciati e forati, diventavano pedine e rotelle dei giochi di strada.",
            2, "Lotto, tra i mattoni"),
    segreto("str_bottone", STR, "collezionabile", "Un bottone militare",
            "Un bottone di divisa, finito nel mucchio degli stracci: nel dopoguerra i vestiti militari venivano disfatti e cuciti di nuovo, e un bottone buono si riusava.",
            3, "Mucchio di stracci della bottega"),
    segreto("str_ritaglio", STR, "collezionabile", "Ritaglio: piccoli lavori",
            "Un ritaglio scritto alla maniera dei giornali di allora (il testo è nostro): racconta i piccoli lavori dei bambini che aiutavano le famiglie.",
            4, "Cortile dei carrettieri, sotto il carro"),
    segreto("str_carta", STR, "collezionabile", "Una carta da gioco",
            "Una carta da gioco dimenticata sul davanzale del terrazzino: una partita lasciata a metà, tra un giro di stracci e l'altro.",
            5, "Terrazzino, sul davanzale"),
    segreto("str_biglia", STR, "collezionabile", "Una biglia di vetro",
            "Una biglia di vetro rossastra, rotolata vicino al lavatoio del cortile del bucato: il gioco dei bambini che, per un po', smettevano di lavorare.",
            6, "Cortile del bucato, vicino al lavatoio"),
]
PEZZI += [
    pezzo("str_tappo", STR, "Lotto, tra i mattoni", tappo(), "raccogli", "str_tappo", "Un tappo schiacciato", appoggio="suolo", dy=-0.1, scala=1.8, raggio=0.4),
    pezzo("str_bottone", STR, "Mucchio di stracci della bottega", bottone_militare(), "raccogli", "str_bottone", "Un bottone nel mucchio", appoggio="fisso", scala=1.8, raggio=0.4),
    pezzo("str_ritaglio", STR, "Cortile dei carrettieri, sotto il carro", ritaglio_stracci(), "raccogli", "str_ritaglio", "Un ritaglio di giornale", appoggio="suolo", dy=-0.1, scala=1.6, raggio=0.4),
    pezzo("str_carta", STR, "Terrazzino, sul davanzale", carta_gioco(), "raccogli", "str_carta", "Una carta da gioco", appoggio="fisso", scala=1.8, raggio=0.4),
    pezzo("str_biglia", STR, "Cortile del bucato, vicino al lavatoio", biglia_rossa(), "raccogli", "str_biglia", "Una biglia", appoggio="suolo", dy=-0.1, scala=1.8, raggio=0.4),
]
ENIGMI.append({
    "id": "enigma_carretto", "tipo": "oggetto", "titolo": "Il carretto di Capa 'e fierro", "meccanismo": "oggetto", "scena": STR, "segreto": "str_carretto",
    "consegna": "Un carretto da robivecchi, con due ruote grandi e un sedile di tavole. Sotto il sedile c'è qualcosa che non si vede bene.",
    "temaCivico": "Il lavoro dei bambini nel dopoguerra: conti scritti a matita, soldi contati con cura",
    "punto": "Carretto di Capa 'e fierro", "dx": 0.0, "dy": -0.4, "dz": 0.0, "yaw": 90, "scala": 1.5, "sostituisce": "CarrettoCapa", "distanza": 1.6, "inclina": 10,
    "prompt": "il carretto", "soluzione": ["ruota", "cassetto"], "ordinata": True,
    "bloccatoMsg": "", "erroreMsg": "Il cassetto non si muove: prima guarda la ruota.", "indizi": [],
    "spiegazione": "Girando la ruota, un fermo scatta e il cassetto sotto il sedile scorre fuori. Dentro, un foglietto con i conti di Capa 'e fierro: «Oggi tre chili di lana, due di cotone. Il ragazzo conta meglio di me.»",
    "scheda": "Saper contare era una difesa: chi sa fare i conti non si fa imbrogliare, e Amerigo, a sette anni, controllava anche quelli degli adulti.",
    "appunto": "Un foglietto di Capa 'e fierro: «Il ragazzo conta meglio di me.»",
    "parti": [
        parte("pianale", "cubo", "legno", "#5a3d26", 0.0, 0.0, 0.0, 1.1, 0.06, 0.6),
        parte("sponda_a", "cubo", "legno", "#4a3220", 0.0, 0.12, 0.3, 1.1, 0.2, 0.04),
        parte("sponda_b", "cubo", "legno", "#4a3220", -0.55, 0.12, 0.0, 0.04, 0.2, 0.6),
        parte("sedile", "cubo", "legno", "#6a4528", 0.3, 0.2, 0.0, 0.4, 0.04, 0.5),
        parte("sostegno_a", "cubo", "legno", "#4a3220", 0.12, 0.1, 0.0, 0.04, 0.2, 0.5),
        parte("ruota", "cilindro", "legno", "#3a2a1a", 0.0, -0.05, -0.34, 0.5, 0.025, 0.5, rx=90, clic=True, azione="gira", evento="ruota", asse="z", dopo="cassetto,foglio,conti",
              msg="La ruota gira con uno scatto: sotto il sedile qualcosa si sblocca."),
        parte("cerchio", "cilindro", "metallo", "#2d2a27", 0.0, -0.05, -0.34, 0.52, 0.012, 0.52, rx=90),
        parte("mozzo", "cilindro", "metallo", "#2d2a27", 0.0, -0.05, -0.36, 0.08, 0.03, 0.08, rx=90),
        parte("cassetto", "cubo", "legno", "#46362a", 0.3, 0.12, -0.26, 0.3, 0.08, 0.05, nascosta=True, clic=True, azione="muovi", evento="cassetto", asse="-z", valore=0.2,
              msg="Il cassetto scorre fuori: dentro c'è un foglietto."),
        parte("foglio", "cubo", "carta", "#d3c8a8", 0.3, 0.165, -0.28, 0.14, 0.004, 0.1, nascosta=True),
        parte("conti", "testo", "carta", "#2a2018", 0.3, 0.172, -0.28, 0.13, 0.1, 1, testo="3 lana\n2 cotone\nil ragazzo\nconta meglio\ndi me", dimensione=0.016, rx=90, font="mano", nascosta=True),
    ],
})


# ============================================================================= VOLANTINO (R013)
STANZE.append({"scena": VOL, "nome": "Il ricordo del volantino", "completo": [
    "ris:flashback_volantino/enigma_voci", "ris:flashback_volantino/enigma_bacheca", "ris:flashback_volantino/enigma_ciclostile",
    "ris:flashback_volantino/enigma_ruoli", "ris:flashback_volantino/enigma_timbro"]})
BQ_VOL = ("Le impronte d'inchiostro dei bambini sul registro sono un'invenzione del gioco; vero è che i bambini, prima di partire, venivano schedati uno per uno, "
          "oltre che lavati, vaccinati e vestiti. L'UDI (Unione Donne Italiane) fu fra le promotrici dei treni insieme al PCI, con l'aiuto della Croce Rossa e di altre associazioni.")


def matrice_rovinata():
    return [parte("foglio", "cubo", "carta", "#e3d9bd", 0, 0.003, 0, 0.16, 0.004, 0.2, ry=12),
            parte("piega", "cubo", "carta", "#d2c6a4", 0.03, 0.012, 0.02, 0.09, 0.004, 0.1, ry=-20, rz=18),
            parte("macchia", "cubo", "carta", "#1d1a18", -0.03, 0.006, -0.04, 0.06, 0.001, 0.05, ry=30)]


def timbro_vecchio():
    return [parte("gomma", "cubo", "cuoio", "#5a1a16", 0, 0.008, 0, 0.07, 0.016, 0.04),
            parte("base", "cubo", "legno", "#6a4528", 0, 0.024, 0, 0.074, 0.018, 0.044),
            parte("manico", "cilindro", "legno", "#4a3220", 0, 0.05, 0, 0.026, 0.024, 0.026),
            parte("pomo", "sfera", "legno", "#4a3220", 0, 0.078, 0, 0.034, 0.03, 0.034)]


def manifesto_elezioni():
    return [parte("foglio", "cubo", "tex:manifestoelezioni", "", 0, 0.002, 0, 0.13, 0.003, 0.17, ry=-14)]


def cartolina_nord():
    return [parte("cartone", "cubo", "tex:cartolinanord", "", 0, 0.003, 0, 0.15, 0.004, 0.10, ry=20)]


def matita_copiativa():
    return [parte("legno", "cilindro", "legno", "#5b3a6e", 0, 0.006, 0, 0.012, 0.07, 0.012, rz=90, ry=25),
            parte("punta", "cilindro", "legno", "#2e1f3a", 0.072, 0.006, 0.034, 0.008, 0.008, 0.008, rz=90, ry=25)]


SEGRETI += [
    segreto("vol_impronte", VOL, "enigma", "Il cassetto delle impronte",
            "Sul leggio della stanza dei registri c'è un registro aperto con le impronte d'inchiostro delle dita dei bambini. Contarle apre il cassetto: dentro, una spilla dell'UDI.",
            1, "Leggio dei registri", BQ_VOL),
    segreto("vol_matrice", VOL, "collezionabile", "Una matrice rovinata",
            "Una matrice di carta cerata, strappata e macchiata: la prova sbagliata finita nel cestino. Con il ciclostile si sbagliava spesso, e si ricominciava.",
            2, "Cestino del ciclostile"),
    segreto("vol_timbro", VOL, "collezionabile", "Un timbro vecchio",
            "Un timbro di gomma consumato, con le lettere al contrario: serviva a mettere il nome del Comitato sui fogli. Le lettere sono al contrario perché sul foglio escano dritte.",
            3, "Scaffale dei registri"),
    segreto("vol_manifesto", VOL, "collezionabile", "Un manifesto delle elezioni",
            "Un manifesto strappato dal muro del largo. Nel 1946 si votò più volte; il 2 giugno, per la prima volta in tutta Italia, votarono anche le donne.",
            4, "Largo, sotto il manifesto strappato"),
    segreto("vol_cartolina", VOL, "collezionabile", "Una cartolina dal Nord",
            "Una cartolina con i saluti dall'Emilia, caduta vicino al lavatoio: un bambino ospitato scrive che sta bene. È inventata per il gioco, come le altre lettere di questo ricordo.",
            5, "Cortile, accanto al lavatoio"),
    segreto("vol_matita", VOL, "collezionabile", "Una matita copiativa",
            "Una matita copiativa: la mina, bagnata, scrive viola e non si cancella. Il 2 giugno 1946 si votò proprio con una matita così.",
            6, "Sala, davanti alla panca"),
]
PEZZI += [
    pezzo("vol_matrice", VOL, "Cestino del ciclostile", matrice_rovinata(), "raccogli", "vol_matrice", "Una matrice strappata", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("vol_timbro", VOL, "Scaffale dei registri", timbro_vecchio(), "raccogli", "vol_timbro", "Un timbro vecchio", appoggio="fisso", scala=1.6, raggio=0.45),
    pezzo("vol_manifesto", VOL, "Largo, sotto il manifesto strappato", manifesto_elezioni(), "raccogli", "vol_manifesto", "Un manifesto strappato", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("vol_cartolina", VOL, "Cortile, accanto al lavatoio", cartolina_nord(), "raccogli", "vol_cartolina", "Una cartolina", appoggio="suolo", scala=1.6, raggio=0.45),
    pezzo("vol_matita", VOL, "Sala, davanti alla panca", matita_copiativa(), "raccogli", "vol_matita", "Una matita", appoggio="suolo", scala=1.8, raggio=0.4),
]
IMPRONTE = [(-0.2, -0.1), (-0.12, 0.02), (-0.22, 0.09), (-0.07, -0.12), (-0.05, 0.12), (-0.16, -0.02),
            (0.07, -0.08), (0.15, 0.04), (0.22, -0.11), (0.1, 0.11), (0.2, 0.12)]   # 11 impronte (codice "11")
ENIGMI.append({
    "id": "enigma_impronte", "tipo": "oggetto", "titolo": "Il registro delle impronte", "meccanismo": "ruote", "scena": VOL, "segreto": "vol_impronte",
    "consegna": "Un registro aperto sul leggio. Al posto delle firme, impronte d'inchiostro: dita di bambini che non sapevano ancora scrivere. Sotto, un cassetto con due rotelle di ottone.",
    "temaCivico": "Ogni bambino iscritto è una persona con un nome: contare le impronte vuol dire contare loro.",
    "punto": "Leggio dei registri", "dx": 0.0, "dy": 0.0, "dz": 0.0, "yaw": -90, "scala": 1.0, "distanza": 1.5, "inclina": 30,
    "prompt": "il registro sul leggio", "codice": "11", "soluzione": ["apri_cassetto"],
    "ruote": {"n": 2, "x": -0.04, "y": 0.84, "z": -0.215, "passo": 0.1, "larghezza": 0.06, "raggio": 0.045, "colore": "#9b7a35"},
    "bloccatoMsg": "", "erroreMsg": "Il cassetto non si apre: conta di nuovo le impronte, su tutte e due le pagine.", "indizi": [],
    "spiegazione": "Undici impronte, undici bambini iscritti in una sera. Il cassetto scorre fuori: dentro c'è una spilla smaltata dell'UDI, dimenticata da una volontaria.",
    "scheda": "Chi non sapeva scrivere firmava con una croce o con un'impronta: nel 1946 in Italia erano ancora in tanti a non saper leggere e scrivere.",
    "appunto": "Nel cassetto del leggio: una spilla dell'UDI.",
    "parti": [
        parte("gamba", "cubo", "legno", "#4a3220", 0.0, 0.45, 0.05, 0.08, 0.9, 0.08),
        parte("piede", "cubo", "legno", "#4a3220", 0.0, 0.03, 0.05, 0.5, 0.06, 0.4),
        parte("cassetto", "cubo", "legno", "#5a3d26", 0.0, 0.84, 0.0, 0.5, 0.14, 0.4),
        parte("piano", "cubo", "legno", "#5a3d26", 0.0, 0.93, 0.0, 0.62, 0.04, 0.44),
        parte("pagina_sx", "cubo", "carta", "#d8cba6", -0.14, 0.955, 0.0, 0.27, 0.008, 0.36),
        parte("pagina_dx", "cubo", "carta", "#d8cba6", 0.14, 0.955, 0.0, 0.27, 0.008, 0.36),
        parte("costola", "cubo", "cuoio", "#4a2a22", 0.0, 0.955, 0.0, 0.02, 0.012, 0.37),
    ] + [parte("impronta_%d" % i, "sfera", "cera", "#3a2e4a", x, 0.961, z, 0.026, 0.003, 0.036, ry=(i * 37) % 60 - 30) for i, (x, z) in enumerate(IMPRONTE)] + [
        parte("maniglia", "cilindro", "ottone", "#d6b25a", 0.18, 0.84, -0.21, 0.035, 0.012, 0.035, rx=90, clic=True, azione="verifica", evento="apri_cassetto",
              dopo="cassetto_aperto,spilla,spilla_smalto", msg="Clic! Il cassetto scorre fuori."),
        parte("cassetto_aperto", "cubo", "legno", "#46362a", 0.0, 0.74, -0.3, 0.44, 0.05, 0.22, nascosta=True),
        parte("spilla", "cilindro", "ottone", "#c9ac4a", 0.05, 0.775, -0.3, 0.04, 0.004, 0.04, nascosta=True),
        parte("spilla_smalto", "cilindro", "vetro", "#a3261c", 0.05, 0.78, -0.3, 0.024, 0.002, 0.024, nascosta=True),
    ],
})

# ricompense dell'enigma campana: la pagina "dietro le quinte" dipende dal soldo; il segreto vale al ritrovamento del soldo

# x,y,z degli enigmi dal punto (per nome) + spostamento
for e in ENIGMI:
    e["x"], e["y"], e["z"] = pos(e["scena"], e["punto"], e.get("dx", 0), e.get("dy", 0), e.get("dz", 0))

# ----------------------------------------------------------------------------- scrittura
# le ricompense "dietro le quinte" delle tre enigmi hanno testo nel segreto (ricompensa)
f = {
    "testi": T,
    "stanze": STANZE,
    "segreti": SEGRETI,
    "pezzi": PEZZI,
    "enigmi": ENIGMI,
    "campane": CAMPANE,
    "uova": UOVA,
    "gettoni": GETTONI,
}
out = os.path.join(RADICE, "Assets", "_Game", "Resources", "Data", "segreti.json")
with open(out, "w", encoding="utf-8", newline="\n") as fh:
    json.dump(f, fh, ensure_ascii=False, indent=1)
print("segreti.json scritto:", len(SEGRETI), "segreti,", len(PEZZI), "pezzi,", len(ENIGMI), "enigmi")
