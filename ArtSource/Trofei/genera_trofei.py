# -*- coding: utf-8 -*-
"""R027: genera Assets/_Game/Resources/Data/trofei.json (l'album dei trofei) e aggiunge la voce "Trofei" al menu (ui_menu.json). Idempotente.
Uso: python genera_trofei.py (dopo i generatori di segreti e ricordi: i numeri delle condizioni si contano dai JSON).
Condizioni: vedi Scripts/Trofei/Trofei.cs. Nessun trofeo punitivo; nessuna condizione legata alla velocita'.
Le "missioni secondarie" sono gli enigmi segreti di R011 (gia' nel gioco): un trofeo per ciascuno.
"""
import glob, json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")


def carica(n):
    return json.load(open(os.path.join(D, n + ".json"), encoding="utf-8"))


# conteggi dai dati (cosi' nessuna condizione e' impossibile)
seg = carica("segreti")
n_segreti = len(seg["segreti"])
schede = 0
for p in glob.glob(os.path.join(D, "flashback_*.json")):
    schede += sum(1 for e in json.load(open(p, encoding="utf-8"))["enigmi"] if e.get("scheda"))
SCENE = ["10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe", "40_FB_Stracci", "50_FB_Volantino", "60_FB_Stazione", "61_FB_Vagone", "70_FB_Modena", "80_Basso_1994"]

T = []


def t(id, fam, titolo, descr, indizio, cond, icona, segreto=False):
    T.append({"id": id, "famiglia": fam, "titolo": titolo, "descrizione": descr, "indizio": indizio, "condizione": cond, "icona": icona, "segreto": segreto})


# ---------------------------------------------------------------- storia
t("storia_filmato", "storia", "Il ritorno", "Hai visto Amerigo tornare nel vicolo.", "Comincia una nuova partita.", "filmato", "I")
t("storia_scarpe", "storia", "Le scarpe", "Hai rivissuto il ricordo delle scarpe.", "Nel basso c'è un baule chiuso.", "fb:flashback_scarpe", "II")
t("storia_stracci", "storia", "Stracci", "Hai lavorato con Capa 'e fierro.", "Nel cortile del basso c'è un fagotto.", "fb:flashback_stracci", "III")
t("storia_volantino", "storia", "Il volantino", "Hai battuto le dicerie con le prove.", "Sulla credenza c'è un foglio piegato.", "fb:flashback_volantino", "IV")
t("storia_cappotto", "storia", "Il cappotto", "Sei partito con il numero 112.", "Dietro la porta è appeso qualcosa.", "fb:flashback_cappotto", "V")
t("storia_violino", "storia", "Il violino", "A Modena hai scoperto di avere orecchio.", "Sulla credenza della cucina c'è un cassettino.", "fb:flashback_violino", "VI")
t("storia_finale", "storia", "Il violino ritrovato", "Hai finito la storia di Amerigo.", "Un telefono squilla nel basso.", "fb:flashback_finale", "VII")
# ---------------------------------------------------------------- enigmi
t("enigmi_primo", "enigmi", "Al primo colpo", "Hai risolto un enigma senza chiedere indizi.", "Prova a risolvere un enigma senza il pulsante Indizio.", "conta:senza_indizi:1", "1")
t("enigmi_dieci", "enigmi", "Testa fina", "Dieci enigmi risolti senza indizi.", "Dieci enigmi senza indizi.", "conta:senza_indizi:10", "10")
t("enigmi_scarpe_pulito", "enigmi", "Passo sicuro", "Il ricordo delle scarpe senza un errore.", "Rivivi le scarpe senza sbagliare.", "fb:flashback_scarpe + no:errore:30_FB_Scarpe", "S")
t("enigmi_modena_pulito", "enigmi", "Orecchio fino", "Il ricordo di Modena senza un errore.", "A Modena, ascolta bene prima di toccare.", "fb:flashback_violino + no:errore:70_FB_Modena", "M")
t("enigmi_schede", "enigmi", "Educazione civica", "Hai letto %d schede civiche." % (schede // 2), "Le schede compaiono quando risolvi un enigma.", "conta:scheda:%d" % (schede // 2), "C")
t("enigmi_schede_tutte", "enigmi", "Il quaderno completo", "Hai letto tutte le schede civiche dei ricordi.", "Ogni enigma dei ricordi ha la sua scheda.", "conta:scheda:%d" % schede, "Q")
# ---------------------------------------------------------------- esplorazione
t("esplora_luoghi", "esplorazione", "Da Napoli a Modena", "Sei stato in tutti i luoghi del gioco.", "Vicolo, basso, ogni ricordo, e il basso del 1994.",
  " + ".join("ev:scena:" + s for s in SCENE), "N")
t("esplora_segreto", "esplorazione", "Occhio lungo", "Hai trovato il primo segreto.", "In ogni stanza c'è qualcosa di nascosto.", "segreti:1", "o")
t("esplora_meta", "esplorazione", "Mezzo vicolo", "Hai trovato metà dei segreti.", "Guarda negli angoli, sotto, dietro.", "segreti:%d" % (n_segreti // 2), "½")
t("esplora_tutti", "esplorazione", "Ogni angolo", "Hai trovato tutti i segreti del gioco.", "", "flag:segreti_tutti", "★", segreto=True)
t("esplora_scatola", "esplorazione", "La scatola dei ragazzi", "Hai consegnato a Carmine la scatola dei ragazzi del vicolo.", "", "cutscene:cutscene_finale#segreti", "♥", segreto=True)
# ---------------------------------------------------------------- missioni secondarie (gli enigmi segreti di R011)
t("missione_portella", "missioni", "La filastrocca", "Hai aperto la portella del sottoscala.", "Nel cortile del vicolo, sotto una scala, tre tasselli dipinti.", "ris:segreti/enigma_portella", "a")
t("missione_ripostiglio", "missioni", "Dietro la credenza", "Hai trovato il posto dove dormiva Amerigo.", "Nel basso, una credenza spostata troppe volte.", "ris:segreti/enigma_ripostiglio", "b")
t("missione_carretto", "missioni", "Il biglietto del robivecchi", "Hai trovato il biglietto di Capa 'e fierro.", "Nel ricordo degli stracci c'è un carretto.", "ris:segreti/enigma_carretto", "c")
t("missione_udi", "missioni", "Le impronte d'inchiostro", "Hai aperto il cassetto del leggio.", "Nella sede del Comitato, un registro senza firme.", "ris:segreti/enigma_impronte", "d")
t("missione_valigia", "missioni", "La valigia dimenticata", "Hai aperto la valigia sulla reticella.", "Nel treno, stessa carrozza, stesso numero.", "ris:segreti/enigma_valigia", "e")
t("missione_carta", "missioni", "La puntina di Napoli", "Hai rimesso a posto la carta d'Italia della scuola.", "A scuola, a Modena, manca una puntina.", "ris:segreti/enigma_carta_italia", "f")
t("missione_radio", "missioni", "92,7", "Hai sintonizzato la radio di Antonietta.", "Nel basso del 1994 una radio gracchia.", "ris:segreti/enigma_radio", "g")
# ---------------------------------------------------------------- civici
t("civico_lavoro", "civici", "Il primo soldo", "Lavoro minorile ieri, diritti oggi: hai fatto il conto giusto con Capa 'e fierro.", "Nella bottega degli stracci si fa un conto.", "gettone:soldo", "£")
t("civico_fonti", "civici", "Contro le dicerie", "A una diceria si risponde con una prova (art. 21).", "Nella sede del Comitato c'è una bacheca.", "gettone:parole_giuste", "§")
t("civico_identita", "civici", "Un nome, non un numero", "Ogni bambino ha diritto alla propria identità.", "In stazione volano via i cartellini.", "gettone:numero_112", "#")
t("civico_scuola", "civici", "La scuola è di tutti", "Art. 34: la scuola è aperta a tutti.", "A Modena, dopo le lezioni, Alcide ti aspetta.", "gettone:spartito_alcide", "♪")
t("civico_famiglia", "civici", "Una famiglia", "Art. 30 e legge 184 del 1983: il diritto a una famiglia.", "Alla fine qualcuno bussa alla porta.", "gettone:testimone", "∞")

out = {
    "testi": {
        "titolo": "Trofei", "sottotitolo": "L'album di figurine di Amerigo", "contatore": "{0} su {1}",
        "bloccato": "Un trofeo segreto: si scopre giocando.", "segreto": "Segreto", "notifica": "Nuova figurina nell'album: {0}", "indietro": "Indietro",
        "famigliaStoria": "La storia", "famigliaEnigmi": "Gli enigmi", "famigliaEsplorazione": "Esplorare", "famigliaMissioni": "Missioni secondarie", "famigliaCivici": "Educazione civica",
    },
    "trofei": T,
}
open(os.path.join(D, "trofei.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(out, ensure_ascii=False, indent=2) + "\n")

# voce "Trofei" del menu principale, prima di "Crediti"
path = os.path.join(D, "ui_menu.json")
raw = open(path, encoding="utf-8", newline="").read()
m = json.loads(raw)
if not any(v["id"] == "trofei" for v in m["voci"]):
    i = next((k for k, v in enumerate(m["voci"]) if v["id"] == "crediti"), len(m["voci"]))
    m["voci"].insert(i, {"id": "trofei", "etichetta": "Trofei"})
    righe = raw.replace("\r\n", "\n").split("\n")
    ind = (len(righe[1]) - len(righe[1].lstrip(" "))) or 2
    s = json.dumps(m, ensure_ascii=False, indent=ind) + ("\n" if raw.endswith("\n") else "")
    if "\r\n" in raw: s = s.replace("\n", "\r\n")
    open(path, "w", encoding="utf-8", newline="").write(s)
print("trofei:", len(T), "segreti:", n_segreti, "schede:", schede)
