# -*- coding: utf-8 -*-
"""R028: il copione completo da registrare. Scrive (tutto in Doppiaggio/, riscritto a ogni lancio):
  DA_REGISTRARE.md       guida Audacity + il cast (ogni personaggio: eta', voce, battute, minuti, chi puo' farlo) + tutte le battute PER CAPITOLO
  PER_PERSONAGGIO.md     le stesse battute raggruppate per personaggio (ogni doppiatore stampa solo le sue), con la battuta che precede e la direzione
  copione.csv            stesse colonne di sempre, separatore ';', UTF-8 con BOM per Excel
  copione.json           capitoli, personaggi e battute (per una pagina web)
  etichette/<NN>_<capitolo>.txt                  etichette di Audacity, una sessione per capitolo (tutti i personaggi)
  etichette/per_personaggio/<personaggio>.txt    etichette di Audacity, una sessione per personaggio (tutte le sue battute)
Righe: TUTTE le battute di TUTTE le cutscene (cutscene_*.json, qualunque personaggio) piu' le introduzioni e conclusioni dei ricordi
(nar_<oggetto>_001 / _002, testo da flashback_<oggetto>.json). I JSON di gioco non vengono mai modificati.
Gli ID non cambiano piu' dopo la consegna (R028 §2): sono il nome del file audio.
La direzione ("come leggerla") e' ricavata dal campo emozione, dal personaggio, dal capitolo della scena e da indizi nel testo
(domande, esclamazioni, puntini, vocativi napoletani); niente testi presi da film o libro, solo indicazioni.
Uso: python genera_da_registrare.py   (Python standard, nessuna dipendenza)
"""
import csv, json, os, re, sys, zlib

RADICE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
D = os.path.join(RADICE, "Assets", "_Game", "Resources", "Data")
OUT = os.path.join(RADICE, "Doppiaggio")
VOCI = os.path.join(RADICE, "Assets", "Audio", "Voci")

CAPITOLI = [  # ordine della storia: (cutscene, flashback, titolo)
    ("intro", None, "Il filmato iniziale"),
    ("scarpe", "scarpe", "Le scarpe"),
    ("stracci", "stracci", "Stracci"),
    ("volantino", "volantino", "Il volantino"),
    ("cappotto", "cappotto", "Il cappotto"),
    ("violino", "violino", "Il violino"),
    ("finale", "finale", "Il finale, 1994"),
]

# ---------------------------------------------------------------------------------------------------------------------
# IL CAST: scheda di ogni personaggio (ordine = ordine di stampa). Chi non ha battute non compare.
#   eta/descrizione dalla bibbia narrativa (Docs/TRAMA.md); "voce" = come deve suonare; "consiglio" = chi puo' doppiarlo;
#   "accento" = inflessione (napoletana / emiliana leggera / neutra).
# ---------------------------------------------------------------------------------------------------------------------
CAST = {
    "narratore": dict(
        eta="voce senza eta' (adulta)", descrizione="Racconta la storia e spiega il contesto storico. Non e' nella scena: guida chi gioca.",
        voce="Calda, chiara, posata; mai teatrale. Come leggere una storia a un ragazzo prima di dormire, ma nitida nei dati storici.",
        consiglio="Voce adulta (o ragazzo/ragazza con voce matura); la piu' costante e paziente del gruppo: e' il personaggio con piu' battute.",
        accento="Italiano neutro, pulito, senza inflessione regionale."),
    "amerigo_adulto": dict(
        eta="55 anni (nel 1994)", descrizione="Amerigo violinista, tornato a Napoli per la morte della madre. Ricorda e commenta il suo passato.",
        voce="Uomo maturo, voce pacata e un po' stanca, di chi ricorda; a tratti si incrina. Parla piano, vicino al microfono.",
        consiglio="Voce adulta maschile, oppure un ragazzo con voce bassa e calma. Serve molto controllo: sussurri e silenzi.",
        accento="Italiano quasi pulito: vive al Nord da decenni, ma l'accento napoletano riaffiora nei ricordi (parole come 'mamma', 'vicolo', i nomi)."),
    "amerigo": dict(
        eta="7 anni (1946)", descrizione="Amerigo bambino, scalzo e poverissimo, svelto coi numeri, orgoglioso, sogna un paio di scarpe tutto suo.",
        voce="Voce chiara e un po' acuta, vivace, sveglia; orgogliosa quando conta, fragile quando parla di mamma.",
        consiglio="Ragazzino o ragazzina (10-14 anni) oppure voce giovane e leggera; niente vocetta caricaturale, deve sembrare un bambino vero.",
        accento="Napoletana leggera da ragazzino di vicolo: vocali finali morbide, nomi troncati (mammà, Tommasì)."),
    "antonietta": dict(
        eta="circa 30 anni", descrizione="La madre di Amerigo: analfabeta, ruvida, ama con i fatti e non con le parole. Ha paura di perdere il figlio.",
        voce="Voce di donna, bassa, dura e stanca; poche parole, frasi corte; la tenerezza si sente solo sotto.",
        consiglio="Ragazza o donna con voce piuttosto bassa e ferma; evitare toni dolci o cantilenanti.",
        accento="Napoletana piu' marcata degli altri, da donna del popolo: cadenza spezzata, accenti sulle fini di frase."),
    "tommasino": dict(
        eta="7 anni (1946)", descrizione="Il migliore amico di Amerigo: monello, chiacchierone, prende in giro con affetto.",
        voce="Voce di ragazzino, rapida, scherzosa, un po' sfrontata; vivace, a volte ridacchia.",
        consiglio="Ragazzino o ragazzo con voce giovane; ideale un amico che sappia fare il simpatico.",
        accento="Napoletana da scugnizzo: vocativi lunghi ('Amerì!'), ritmo veloce."),
    "maddalena": dict(
        eta="circa 25 anni", descrizione="Giovane militante del Comitato: organizza i treni e convince i genitori con calma e carte alla mano. Personaggio di finzione.",
        voce="Voce di donna giovane, chiara, sicura; convince senza alzare la voce; rispettosa con le madri.",
        consiglio="Ragazza (o donna) con voce chiara e determinata; deve sembrare credibile e sincera.",
        accento="Napoletana colta e pulita: si capisce bene, con appena la cadenza di Napoli."),
    "capa_e_fierro": dict(
        eta="circa 50 anni", descrizione="Il robivecchi che compra gli stracci: brontolone, taccagno ma non cattivo; in fondo ha un cuore.",
        voce="Voce di uomo maturo, roca e brusca; brontola, ma si ammorbidisce quando e' messo in imbarazzo.",
        consiglio="Voce adulta maschile, oppure un ragazzo che sappia fare la voce roca da vecchio burbero.",
        accento="Napoletana forte, da bottegaio: parlato brusco, vocali larghe."),
    "donna_vicolo": dict(
        eta="circa 40 anni", descrizione="Una donna del vicolo che regala vecchi stracci ad Amerigo.",
        voce="Voce di donna, materna e gentile, un po' stanca.",
        consiglio="Ragazza o donna adulta, voce morbida.",
        accento="Napoletana popolare, bonaria."),
    "pachiochia": dict(
        eta="anziana (circa 70 anni)", descrizione="La vecchia del vicolo che semina paura e dicerie sui treni. Non e' una cattiva: e' la paura fatta persona.",
        voce="Voce rauca di vecchia, sibilante e cupa; frasi da profezia di sventura.",
        consiglio="Voce anziana o adulta; chi sa fare una voce da vecchietta senza caricare.",
        accento="Napoletana stretta, da vecchia del vicolo: lenta, cantilenante, con la fine di frase che cade."),
    "madre_vicolo": dict(
        eta="circa 35 anni", descrizione="Una madre del vicolo che ha letto il registro e smonta le dicerie della Pachiochia.",
        voce="Voce di donna concreta, sbrigativa, stufa delle chiacchiere.",
        consiglio="Ragazza o donna adulta, voce decisa.",
        accento="Napoletana popolare, schietta."),
    "mariuccia": dict(
        eta="7 anni (1946)", descrizione="Una bambina del vicolo che parte con gli altri; seria, puntigliosa: vuole essere chiamata con il nome.",
        voce="Voce di bambina, sottile e seria, che non si lascia convincere con niente.",
        consiglio="Ragazza (o ragazzina) con voce giovane.",
        accento="Napoletana leggera da bambina."),
    "controllore": dict(
        eta="circa 45 anni", descrizione="Il controllore alla stazione di Modena: allegro, abituato a farsi sentire sulla banchina.",
        voce="Voce di uomo, forte, squillante e cordiale; deve coprire il rumore del treno.",
        consiglio="Voce adulta maschile, oppure un ragazzo che sappia fare la voce piena.",
        accento="Emiliana leggera (Modena): cadenza cantilenante, tono cordiale; 'guagliò' lo dice con simpatia, come una parola imparata dai bambini."),
    "derna": dict(
        eta="30-40 anni", descrizione="Derna, donna sola di Modena, militante: accoglie Amerigo con calma, poche parole e tanto fare.",
        voce="Voce di donna calda, pratica, rassicurante; senza smancerie, con una dolcezza che si capisce dai gesti.",
        consiglio="Ragazza o donna adulta con voce calda e sicura.",
        accento="Emiliana leggera (Modena): cadenza piana e cantilenante, vocali un po' allungate, tono schietto."),
    "alcide": dict(
        eta="circa 40 anni", descrizione="Alcide Benvenuti, accordatore di pianoforti: guida musicale e figura paterna per Amerigo.",
        voce="Voce di uomo, calma, bassa e calda; paterna, parla con la lentezza di chi ascolta i suoni.",
        consiglio="Voce adulta maschile, oppure un ragazzo con voce bassa e tranquilla.",
        accento="Emiliana leggera (Modena): cadenza piana, calda."),
    "rosa": dict(
        eta="circa 35-40 anni", descrizione="Rosa Benvenuti, la moglie con il forno: esuberante, accogliente, abituata a dar da mangiare a tutti.",
        voce="Voce di donna, piena e allegra, che riempie la cucina.",
        consiglio="Ragazza o donna con voce solare.",
        accento="Emiliana leggera (Modena): vivace, cordiale."),
    "rivo": dict(
        eta="circa 8 anni", descrizione="Rivo, figlio dei Benvenuti: orgoglioso, un po' geloso dell'ospite; si confessa e poi fa pace.",
        voce="Voce di ragazzino, imbarazzata quando confessa, sfrontata quando scherza.",
        consiglio="Ragazzino o ragazzo con voce giovane.",
        accento="Emiliana leggera (Modena): vocali larghe, tono schietto."),
    "carmine": dict(
        eta="circa 18 anni (nel 1994)", descrizione="Carmine, nipote di Amerigo: giovane napoletano, rispettoso e un po' timido con lo zio.",
        voce="Voce di ragazzo, semplice e pacata; rispettosa e curiosa.",
        consiglio="Ragazzo (o ragazza) con voce giovane e tranquilla.",
        accento="Napoletana leggera, moderna (1994): meno marcata dei bambini del 1946."),
}

# ---------------------------------------------------------------------------------------------------------------------
# LE EMOZIONI: (tono, volume, ritmo). Il campo "emozione" delle battute e' uno di questi.
# ---------------------------------------------------------------------------------------------------------------------
EMOZIONI = {
    "neutra": ("tono naturale, colloquiale, senza enfasi", "volume medio", "ritmo normale"),
    "": ("tono naturale, colloquiale, senza enfasi", "volume medio", "ritmo normale"),
    "pensierosa": ("tono pensieroso, come parlando fra se'", "volume medio-basso", "ritmo lento, con piccole esitazioni"),
    "felice": ("tono sorridente e luminoso (il sorriso si deve sentire)", "volume medio-alto", "ritmo vivace"),
    "decisa": ("tono fermo e sicuro, senza esitazioni", "volume medio-alto", "ritmo scandito, parole ben staccate"),
    "triste": ("tono sommesso, voce che si spegne a fine frase", "volume basso", "ritmo lento"),
    "sorpresa": ("tono stupito, la voce sale sulla parola della sorpresa", "volume medio-alto", "scatto iniziale, poi rallenta"),
    "commossa": ("tono trattenuto, voce che trema appena, mai piagnucolosa", "volume basso (quasi un sussurro)", "ritmo lento, con respiri"),
    "seria": ("tono grave e composto", "volume medio", "ritmo posato"),
    "malinconica": ("tono dolce e lontano, di nostalgia", "volume basso", "ritmo lento"),
    "arrabbiata": ("tono duro, rimprovero stanco (non urlato)", "volume medio-alto", "ritmo secco, frasi a scatti"),
}

# Sfumature per personaggio: sostituiscono il tono dell'emozione base (solo dove serve).
SFUMATURE = {
    "antonietta": {
        "neutra": "tono asciutto, frasi corte: l'affetto e' nascosto sotto la durezza",
        "decisa": "ordini secchi da madre che non ammette repliche, ma senza gridare",
        "arrabbiata": "rimprovero secco da madre stanca e spaventata, non urlato",
        "triste": "voce bassa, trattiene la paura per non farla vedere al figlio",
        "sorpresa": "stupore diffidente, quasi un sospetto"},
    "pachiochia": {
        "triste": "tono cupo e funereo, da profezia di sventura (lei ci crede davvero)",
        "neutra": "tono sospettoso, mormorato"},
    "capa_e_fierro": {
        "neutra": "tono brusco da bottegaio, brontolando",
        "triste": "brontolio imbarazzato, quasi un'ammissione di colpa: si ammorbidisce"},
    "tommasino": {
        "felice": "tono da monello, prendendo in giro con affetto",
        "neutra": "tono furbo, un po' sfrontato",
        "sorpresa": "tono da monello che cambia discorso all'improvviso"},
    "maddalena": {
        "decisa": "tono chiaro e convincente: spiega con calma, senza fare la predica",
        "neutra": "tono paziente di chi spiega le cose due volte",
        "felice": "tono incoraggiante, caldo"},
    "controllore": {"felice": "tono squillante e cordiale, voce da banchina che deve farsi sentire sopra il treno"},
    "mariuccia": {"pensierosa": "tono serio e puntiglioso da bambina che non cede"},
    "madre_vicolo": {"decisa": "tono sbrigativo di chi ha le prove in mano e e' stufa delle chiacchiere"},
    "derna": {"neutra": "tono calmo e pratico, rassicurante senza smancerie", "felice": "tono caldo, sorride mentre parla"},
    "alcide": {"felice": "tono paterno e caldo, invita piano a suonare"},
    "rosa": {"felice": "tono esuberante e materno, voce piena"},
    "rivo": {"triste": "tono imbarazzato di chi confessa, voce che si accorcia", "pensierosa": "tono sincero e un po' goffo",
             "felice": "tono spavaldo, di chi fa pace scherzando"},
    "carmine": {"neutra": "tono pacato, rispettoso", "pensierosa": "tono timido, quasi una domanda sospesa", "sorpresa": "tono curioso di ragazzo"},
    "amerigo": {
        "neutra": "tono da bambino sveglio che parla con naturalezza",
        "felice": "tono vivace da bambino orgoglioso della sua intelligenza",
        "sorpresa": "tono stupito da bambino, a bocca aperta",
        "pensierosa": "tono sognante di bambino che pensa, voce piccola",
        "triste": "tono sommesso di bambino, la voce si assottiglia",
        "decisa": "tono orgoglioso e serio, la voce non trema"},
    "amerigo_adulto": {
        "pensierosa": "tono pensieroso di chi ricorda, quasi un monologo interiore",
        "commossa": "tono trattenuto di uomo che non vuole farsi vedere commosso; la voce si incrina appena",
        "felice": "tono sereno, un mezzo sorriso in voce",
        "decisa": "tono fermo di uomo che ha deciso, senza durezza",
        "malinconica": "tono dolce e lontano, di nostalgia e un po' di rimpianto"},
}

# Contesto per capitolo di scena (cutscene non-intro) per Amerigo adulto.
CONTESTO_SCENA = {"ingresso": "ingresso nel ricordo: voce intima, appena evocata", "meta": "a meta' del ricordo: voce piu' presente",
                  "chiusura": "chiusura del ricordo: lascia un peso e un piccolo silenzio finale"}

# Vocativi e parole napoletane: se compaiono nel testo di un personaggio napoletano, accenno alla pronuncia.
VOCATIVI = re.compile(r"\b(Amer[ìi]|Tommas[ìi]|Pachi[òo]|mamm[àa]|guagli[òoùu]|signo')", re.IGNORECASE)
NAPOLETANI = {"amerigo", "amerigo_adulto", "antonietta", "tommasino", "maddalena", "capa_e_fierro", "donna_vicolo", "pachiochia",
              "madre_vicolo", "mariuccia", "carmine"}


def carica(n):
    """Legge un JSON di gioco (sola lettura; tollera il BOM)."""
    p = os.path.join(D, n + ".json")
    return json.load(open(p, encoding="utf-8-sig")) if os.path.exists(p) else None


def durata(testo):
    parole = len(re.findall(r"\w+", testo))
    return round(0.6 + parole / 2.4, 1)  # circa 2,4 parole al secondo, piu' il respiro


def nomi():
    d = carica("personaggi") or {"personaggi": []}
    return {p["id"]: p.get("nomeCopione") or p["nome"] for p in d["personaggi"]}


def scegli(id, opzioni):
    """Variante stabile (non casuale): la stessa battuta ha sempre la stessa formulazione."""
    return opzioni[zlib.crc32(id.encode("utf-8")) % len(opzioni)]


def pause(testo):
    """Indicazioni di pausa ricavate dalla punteggiatura (puntini, due punti, trattini, elenchi, frasi lunghe)."""
    out = []
    if "..." in testo or "…" in testo:
        prima = re.split(r"\.\.\.|…", testo)[0].strip().split(" ")[-1]
        out.append("pausa lunga sui puntini (dopo «%s»)" % prima)
    if re.search(r"\w: \w", testo):
        out.append("piccola pausa dopo i due punti")
    if " — " in testo or " - " in testo:
        out.append("respiro al trattino")
    if re.search(r"\.\s+\S", testo) and len(re.findall(r"\w+", testo)) > 18:
        out.append("respiro fra una frase e l'altra, senza fretta")
    if testo.count(",") >= 3:
        out.append("elenco: piccola pausa a ogni virgola, la voce scende sull'ultimo elemento")
    return out


def intonazione(testo):
    """Indizi dalla punteggiatura finale e interna."""
    t = testo.strip()
    out = []
    if t.endswith("?"):
        out.append("domanda: la voce sale alla fine")
    if "!" in t:
        out.append("esclamazione: spinta sul punto esclamativo, senza urlare")
    return out


def direzione(id, pers, emozione, testo, capitolo):
    """Costruisce 'come leggerla': tono, volume, ritmo, pause, nota di scena, inflessione. Da emozione, personaggio, scena e testo."""
    tono, vol, rit = EMOZIONI.get(emozione, EMOZIONI["neutra"])
    sfum = SFUMATURE.get(pers, {}).get(emozione or "neutra")
    if sfum:
        tono = sfum
    elif pers == "narratore" and emozione in ("neutra", ""):
        tono = scegli(id, ["voce calda e chiara di chi racconta", "racconto posato, come per accompagnare un ragazzo nella storia",
                           "voce chiara, un passo indietro rispetto ai personaggi"])
    elif pers == "narratore":
        tono += "; sempre voce di narratore, non di personaggio"
    note = []
    if pers == "narratore":  # indizi di contesto dal testo
        if re.match(r"^(Napoli|Modena)\b[^.]{0,40}\d{4}", testo):
            note.append("apertura di scena: stacca luogo e data con una pausa dopo la virgola, poi lascia andare la descrizione")
        if re.search(r"\b(1945|1952|art\.|legge n\.|Costituzione)", testo):
            tono, vol, rit = "tono informativo da insegnante, nitido e rispettoso, senza freddezza", "volume medio", "ritmo posato, numeri scanditi"
            note.append("dati e numeri scanditi con chiarezza, una pausa prima e dopo la cifra")
        if re.search(r"Ora tocca a te|Hai trovato", testo):
            tono = "tono caldo e complice: ti rivolgi direttamente a chi sta giocando"
        if capitolo == "chiusura":
            note.append("frase di chiusura: lascia un piccolo silenzio dopo l'ultima parola")
    if pers == "amerigo_adulto" and capitolo in CONTESTO_SCENA:
        note.append(CONTESTO_SCENA[capitolo])
    if pers in NAPOLETANI:
        voc = VOCATIVI.findall(testo)
        if voc:
            note.append("nome o richiamo napoletano («%s»): parola tronca, accento sulla fine, un po' allungato" % voc[0])
    if pers == "controllore" and re.search(r"guagli[òoùu]", testo):
        note.append("«guagliò» detto con simpatia: e' l'unica parola napoletana")
    p = pause(testo) + intonazione(testo)
    parti = ["Tono: " + tono, "Volume: " + vol, "Ritmo: " + rit]
    if p:
        parti.append("Pause: " + "; ".join(p))
    if note:
        parti.append("Nota: " + "; ".join(note))
    parti.append("Inflessione: " + CAST[pers]["accento"])
    return " · ".join(parti)


def righe():
    """Tutte le battute, per capitolo, nell'ordine di gioco. Ogni riga e' un dizionario."""
    nome = nomi()
    out = []
    for k, (cs, fb, titolo) in enumerate(CAPITOLI):
        sezione = []
        f = carica("flashback_" + fb) if fb else None
        if f and f.get("introduzione"):
            sezione.append(dict(id="nar_%s_001" % fb, pers="narratore", testo=f["introduzione"], emozione="neutra", cap="ingresso", fonte="entrata nel ricordo", scena=""))
        c = carica("cutscene_" + cs)
        if c:
            for b in c["battute"]:
                pers = b.get("personaggio") or "narratore"
                file = os.path.splitext(b.get("audio") or b["id"])[0]
                scena = (b.get("regia") or {}).get("inquadratura", "")
                sezione.append(dict(id=file, pers=pers, testo=b["testo"], emozione=b.get("emozione", ""), cap=b.get("capitolo", ""), fonte="", scena=scena))
        if f and f.get("conclusione"):
            sezione.append(dict(id="nar_%s_002" % fb, pers="narratore", testo=f["conclusione"], emozione="neutra", cap="chiusura", fonte="chiusura del ricordo", scena=""))
        # Nome leggibile, durata, direzione e battuta precedente (l'ultima di un altro personaggio, entro 4 righe).
        for i, r in enumerate(sezione):
            r["nome"] = nome.get(r["pers"], r["pers"].replace("_", " ").capitalize())
            r["durata"] = durata(r["testo"])
            r["come"] = direzione(r["id"], r["pers"], r["emozione"], r["testo"], r["cap"])
            if r["fonte"]:
                r["come"] += " · Scena: " + r["fonte"]
            r["precedente"] = ""
            for j in range(i - 1, max(-1, i - 5), -1):
                if sezione[j]["pers"] != r["pers"]:
                    r["precedente"] = "%s: %s" % (nome.get(sezione[j]["pers"], sezione[j]["pers"]), sezione[j]["testo"])
                    break
        out.append((k, cs, titolo, sezione))
    return out


def gia_registrato(id):
    for ext in (".wav", ".ogg", ".mp3"):
        if os.path.exists(os.path.join(VOCI, id + ext)):
            return True
    return False


def scrivi(percorso, testo):
    open(percorso, "w", encoding="utf-8", newline="\n").write(testo)


def minuti(sec):
    return round(sec / 60.0, 1)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    os.makedirs(os.path.join(OUT, "etichette", "per_personaggio"), exist_ok=True)
    tutte = righe()
    tutte_le_righe = [r for _, _, _, s in tutte for r in s]
    ids = [r["id"] for r in tutte_le_righe]
    doppi = sorted({i for i in ids if ids.count(i) > 1})
    if doppi:
        raise SystemExit("ID duplicati: " + ", ".join(doppi))
    sconosciuti = sorted({r["pers"] for r in tutte_le_righe if r["pers"] not in CAST})
    if sconosciuti:
        raise SystemExit("Personaggi senza scheda nel CAST (aggiungili allo script): " + ", ".join(sconosciuti))
    totale = len(ids)
    fatte = sum(1 for i in ids if gia_registrato(i))
    sec = sum(r["durata"] for r in tutte_le_righe)

    per_pers = {p: [r for r in tutte_le_righe if r["pers"] == p] for p in CAST}  # nell'ordine del CAST
    cast_attivo = [p for p in CAST if per_pers[p]]
    capitolo_di = {r["id"]: (k, cs, titolo) for k, cs, titolo, s in tutte for r in s}

    # ---------------------------------------------------------------- DA_REGISTRARE.md
    md = ["# Battute da registrare (R028)", "",
          "Generato da `ArtSource/Doppiaggio/genera_da_registrare.py`: **non modificare a mano** (rilanciare lo script se cambiano i dialoghi).",
          "Gli ID qui sotto **non cambiano piu'**: il nome del file e' l'ID, con estensione `.wav` (o `.ogg`).", "",
          "**Totale: %d file** (gia' registrati: %d) · durata stimata in tutto: circa %d minuti · %d personaggi." % (totale, fatte, round(sec / 60), len(cast_attivo)), "",
          "## Come registrare con Audacity", "",
          "1. **Impostazioni del progetto**: in basso a sinistra *Frequenza del progetto* **44100 Hz**; *Modifica > Preferenze > Dispositivi*: canali **1 (Mono)**.",
          "2. Registra **un capitolo per sessione**, leggendo le battute nell'ordine della tabella, con **2 secondi di silenzio** fra una battuta e l'altra. "
          "Oppure **una sessione per personaggio** (consigliato se ogni amico fa la sua voce): usa `PER_PERSONAGGIO.md` e le etichette in `etichette/per_personaggio/`.",
          "3. **Riduzione rumore**: seleziona un pezzo di solo silenzio, *Effetti > Riduzione rumore > Ottieni profilo del rumore*; poi seleziona tutto (Ctrl+A) e applica l'effetto (valori predefiniti).",
          "4. **Normalizza**: *Effetti > Normalizza*, picco massimo **-1 dB**.",
          "5. **Etichette**: *File > Importa > Etichette...* e scegli `Doppiaggio/etichette/<capitolo>.txt` (oppure `etichette/per_personaggio/<personaggio>.txt`). Ogni etichetta ha gia' il nome giusto (l'ID): spostane i bordi "
          "trascinandoli in modo che ognuna copra la sua battuta, con circa **0,3 s di silenzio prima e dopo**.",
          "6. **Esporta**: *File > Esporta audio multiplo...* (Esporta multipli): formato **WAV (Microsoft) 16 bit PCM con segno** (oppure Ogg Vorbis qualita' 5), "
          "*Dividi i file in base a*: **Etichette**, *Nomina i file*: **Usando etichetta/nome della traccia**, cartella **`Assets/Audio/Voci/`** del progetto.",
          "   Per una battuta sola: selezionala e usa *File > Esporta audio > Esporta la selezione* con il nome esatto della tabella.",
          "7. In Unity: **Treno > Doppiaggio > Aggiorna libreria voci**. Nel gioco il pulsante altoparlante della battuta diventa nero e la voce parte.", "",
          "## Il cast", "",
          "Chi serve per registrare tutto il gioco. I minuti sono una stima del solo parlato (senza pause e ripetizioni).", "",
          "| Personaggio | Eta' | Chi e' | Come deve suonare la voce | Battute | Minuti | Chi puo' farlo |",
          "|-------------|------|--------|---------------------------|---------|--------|----------------|"]
    for p in cast_attivo:
        c, rs = CAST[p], per_pers[p]
        md.append("| **%s** | %s | %s | %s %s | %d | %.1f | %s |" % (rs[0]["nome"], c["eta"], c["descrizione"], c["voce"], c["accento"], len(rs), minuti(sum(r["durata"] for r in rs)), c["consiglio"]))
    md.append("")
    rows_csv = []
    for k, cs, titolo, sezione in tutte:
        if not sezione:
            continue
        md += ["## %d. %s (%d file)" % (k, titolo, len(sezione)), "", "| # | File | Personaggio | Testo | Come leggerla | Durata |", "|---|------|-------------|-------|---------------|--------|"]
        etichette, t = [], 1.0
        for n, r in enumerate(sezione, 1):
            id = r["id"]
            fatto = " ✓" if gia_registrato(id) else ""
            md.append("| %d | `%s.wav`%s | %s | %s | %s | %.1f s |" % (n, id, fatto, r["nome"], r["testo"].replace("|", "/"), r["come"].replace("|", "/"), r["durata"]))
            rows_csv.append([titolo, id, id + ".wav", r["nome"], r["testo"], r["come"], "%.1f" % r["durata"], "si" if fatto else "no"])
            etichette.append("%.3f\t%.3f\t%s" % (t, t + r["durata"], id))
            t += r["durata"] + 2.0
        md.append("")
        scrivi(os.path.join(OUT, "etichette", "%02d_%s.txt" % (k, cs)), "\n".join(etichette) + "\n")
    scrivi(os.path.join(OUT, "DA_REGISTRARE.md"), "\n".join(md))

    # ---------------------------------------------------------------- copione.csv
    with open(os.path.join(OUT, "copione.csv"), "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(["capitolo", "id", "file", "personaggio", "testo", "come leggerla", "durata (s)", "registrato"])
        w.writerows(rows_csv)

    # ---------------------------------------------------------------- etichette per personaggio (una sessione per personaggio)
    cartella = os.path.join(OUT, "etichette", "per_personaggio")
    for vecchio in os.listdir(cartella):  # niente file di personaggi spariti
        if vecchio.endswith(".txt"):
            os.remove(os.path.join(cartella, vecchio))
    for p in cast_attivo:
        et, t = [], 1.0
        for r in per_pers[p]:
            et.append("%.3f\t%.3f\t%s" % (t, t + r["durata"], r["id"]))
            t += r["durata"] + 2.0
        scrivi(os.path.join(cartella, p + ".txt"), "\n".join(et) + "\n")

    # ---------------------------------------------------------------- PER_PERSONAGGIO.md
    pp = ["# Copione per personaggio (R028)", "",
          "Generato da `ArtSource/Doppiaggio/genera_da_registrare.py`: **non modificare a mano**. Ogni doppiatore stampa solo la sua sezione.",
          "Per ogni battuta: l'ID (nome del file da esportare), il capitolo, **cosa ti dice l'altro personaggio subito prima** (per capire come rispondere) e come leggerla.",
          "Audacity: importa `etichette/per_personaggio/<personaggio>.txt` e registra tutte le tue battute di seguito, 2 secondi di pausa fra l'una e l'altra.", "",
          "**Indice:** " + " · ".join("%s (%d)" % (per_pers[p][0]["nome"], len(per_pers[p])) for p in cast_attivo), ""]
    for p in cast_attivo:
        c, rs = CAST[p], per_pers[p]
        pp += ["---", "", "## %s" % rs[0]["nome"], "",
               "- **Eta':** %s" % c["eta"], "- **Chi e':** %s" % c["descrizione"], "- **Voce:** %s" % c["voce"], "- **Inflessione:** %s" % c["accento"],
               "- **Chi puo' farlo:** %s" % c["consiglio"],
               "- **Battute:** %d · **Durata stimata:** %.1f minuti · **Etichette Audacity:** `etichette/per_personaggio/%s.txt`" % (len(rs), minuti(sum(r["durata"] for r in rs)), p), ""]
        for n, r in enumerate(rs, 1):
            k, cs, titolo = capitolo_di[r["id"]]
            pp += ["### %d. `%s.wav` · %s" % (n, r["id"], titolo), ""]
            pp.append("- **Prima di te:** «%s»" % r["precedente"] if r["precedente"] else "- **Prima di te:** (apri tu la scena)")
            if r["scena"]:
                pp.append("- **Scena:** %s" % r["scena"])
            pp += ["- **Dici:** «%s»" % r["testo"], "- **Come:** %s" % r["come"], "- **Durata:** %.1f s" % r["durata"], ""]
    scrivi(os.path.join(OUT, "PER_PERSONAGGIO.md"), "\n".join(pp))

    # ---------------------------------------------------------------- copione.json (per una pagina web)
    dati = {
        "capitoli": [{"id": cs, "titolo": titolo} for _, cs, titolo, s in tutte if s],
        "personaggi": [{"id": p, "nome": per_pers[p][0]["nome"], "eta": CAST[p]["eta"], "descrizione": CAST[p]["descrizione"],
                        "voce": CAST[p]["voce"] + " " + CAST[p]["accento"], "consiglio": CAST[p]["consiglio"],
                        "battute": len(per_pers[p]), "minuti": minuti(sum(r["durata"] for r in per_pers[p]))} for p in cast_attivo],
        "battute": [{"id": r["id"], "file": r["id"] + ".wav", "capitolo": capitolo_di[r["id"]][1], "personaggio": r["pers"], "nome": r["nome"],
                     "testo": r["testo"], "come": r["come"], "durata": r["durata"], "precedente": r["precedente"]}
                    for _, _, _, s in tutte for r in s],
    }
    open(os.path.join(OUT, "copione.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(dati, ensure_ascii=False, indent=2) + "\n")

    # ---------------------------------------------------------------- riepilogo e controllo dei conteggi
    attese = 0
    for cs, fb, _ in CAPITOLI:
        attese += len(carica("cutscene_" + cs)["battute"])
        f = carica("flashback_" + fb) if fb else None
        if f:
            attese += bool(f.get("introduzione")) + bool(f.get("conclusione"))
    print("battute: %d (attese %d) - ID unici - minuti stimati: %.1f - gia' registrate: %d" % (totale, attese, sec / 60.0, fatte))
    for p in cast_attivo:
        print("  %-16s %3d battute  %5.1f min" % (per_pers[p][0]["nome"], len(per_pers[p]), minuti(sum(r["durata"] for r in per_pers[p]))))
    if totale != attese:
        raise SystemExit("ERRORE: il numero di battute non coincide con i JSON di gioco")


if __name__ == "__main__":
    main()
