# -*- coding: utf-8 -*-
"""R028: la lista dei file da doppiare. Scrive:
  Doppiaggio/DA_REGISTRARE.md   (ordinata per scena: nome file esatto, personaggio, testo, indicazione di lettura, durata stimata; guida Audacity)
  Doppiaggio/copione.csv        (stesse righe, separatore ';', UTF-8 con BOM per Excel)
  Doppiaggio/etichette/<NN>_<capitolo>.txt  (file di etichette di Audacity: una sessione per capitolo, poi File > Esporta multipli)
Righe: tutte le battute del Narratore delle cutscene, le introduzioni e conclusioni dei ricordi (nar_<oggetto>_001 / _002, testo da flashback_<oggetto>.json),
e tutte le battute del filmato iniziale (cs_intro_*, doppiate dall'utente anche per gli altri personaggi).
Gli ID non cambiano piu' dopo la consegna (R028 §2). Uso: python genera_da_registrare.py
"""
import csv, json, os, re

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
LETTURA = {
    "neutra": "voce calma, racconto", "pensierosa": "piano, pensieroso, con pause", "decisa": "ferma, chiara", "triste": "sommessa",
    "felice": "sorridente", "sorpresa": "stupita", "commossa": "trattenuta, commossa", "": "voce calma, racconto",
}


def carica(n):
    p = os.path.join(D, n + ".json")
    return json.load(open(p, encoding="utf-8")) if os.path.exists(p) else None


def durata(testo):
    parole = len(re.findall(r"\w+", testo))
    return round(0.6 + parole / 2.4, 1)  # circa 2,4 parole al secondo, piu' il respiro


def nomi():
    d = carica("personaggi") or {"personaggi": []}
    return {p["id"]: p.get("nomeCopione") or p["nome"] for p in d["personaggi"]}


def righe():
    nome = nomi()
    out = []
    for k, (cs, fb, titolo) in enumerate(CAPITOLI):
        sezione = []
        f = carica("flashback_" + fb) if fb else None
        if f and f.get("introduzione"):
            sezione.append(("nar_%s_001" % fb, "Narratore", f["introduzione"], "voce calma, racconto (entrata nel ricordo)"))
        c = carica("cutscene_" + cs)
        if c:
            for b in c["battute"]:
                pers = b.get("personaggio") or "narratore"
                if cs != "intro" and pers != "narratore":
                    continue
                file = os.path.splitext(b.get("audio") or b["id"])[0]
                sezione.append((file, nome.get(pers, pers.capitalize()), b["testo"], LETTURA.get(b.get("emozione", ""), b.get("emozione", ""))))
        if f and f.get("conclusione"):
            sezione.append(("nar_%s_002" % fb, "Narratore", f["conclusione"], "voce calma, chiusura del ricordo"))
        out.append((k, cs, titolo, sezione))
    return out


def gia_registrato(id):
    for ext in (".wav", ".ogg", ".mp3"):
        if os.path.exists(os.path.join(VOCI, id + ext)):
            return True
    return False


def main():
    os.makedirs(os.path.join(OUT, "etichette"), exist_ok=True)
    tutte = righe()
    ids = [r[0] for _, _, _, s in tutte for r in s]
    doppi = sorted({i for i in ids if ids.count(i) > 1})
    if doppi:
        raise SystemExit("ID duplicati: " + ", ".join(doppi))
    totale = len(ids)
    fatte = sum(1 for i in ids if gia_registrato(i))
    sec = sum(durata(r[2]) for _, _, _, s in tutte for r in s)

    md = ["# Battute da registrare (R028)", "",
          "Generato da `ArtSource/Doppiaggio/genera_da_registrare.py`: **non modificare a mano** (rilanciare lo script se cambiano i dialoghi).",
          "Gli ID qui sotto **non cambiano piu'**: il nome del file e' l'ID, con estensione `.wav` (o `.ogg`).", "",
          "**Totale: %d file** (gia' registrati: %d) · durata stimata in tutto: circa %d minuti." % (totale, fatte, round(sec / 60)), "",
          "## Come registrare con Audacity", "",
          "1. **Impostazioni del progetto**: in basso a sinistra *Frequenza del progetto* **44100 Hz**; *Modifica > Preferenze > Dispositivi*: canali **1 (Mono)**.",
          "2. Registra **un capitolo per sessione**, leggendo le battute nell'ordine della tabella, con **2 secondi di silenzio** fra una battuta e l'altra.",
          "3. **Riduzione rumore**: seleziona un pezzo di solo silenzio, *Effetti > Riduzione rumore > Ottieni profilo del rumore*; poi seleziona tutto (Ctrl+A) e applica l'effetto (valori predefiniti).",
          "4. **Normalizza**: *Effetti > Normalizza*, picco massimo **-1 dB**.",
          "5. **Etichette**: *File > Importa > Etichette...* e scegli `Doppiaggio/etichette/<capitolo>.txt`. Ogni etichetta ha gia' il nome giusto (l'ID): spostane i bordi "
          "trascinandoli in modo che ognuna copra la sua battuta, con circa **0,3 s di silenzio prima e dopo**.",
          "6. **Esporta**: *File > Esporta audio multiplo...* (Esporta multipli): formato **WAV (Microsoft) 16 bit PCM con segno** (oppure Ogg Vorbis qualita' 5), "
          "*Dividi i file in base a*: **Etichette**, *Nomina i file*: **Usando etichetta/nome della traccia**, cartella **`Assets/Audio/Voci/`** del progetto.",
          "   Per una battuta sola: selezionala e usa *File > Esporta audio > Esporta la selezione* con il nome esatto della tabella.",
          "7. In Unity: **Treno > Doppiaggio > Aggiorna libreria voci**. Nel gioco il pulsante altoparlante della battuta diventa nero e la voce parte.", ""]
    rows_csv = []
    for k, cs, titolo, sezione in tutte:
        if not sezione:
            continue
        md += ["## %d. %s (%d file)" % (k, titolo, len(sezione)), "", "| # | File | Personaggio | Testo | Come leggerla | Durata |", "|---|------|-------------|-------|---------------|--------|"]
        etichette, t = [], 1.0
        for n, (id, pers, testo, lettura) in enumerate(sezione, 1):
            dur = durata(testo)
            fatto = " ✓" if gia_registrato(id) else ""
            md.append("| %d | `%s.wav`%s | %s | %s | %s | %.1f s |" % (n, id, fatto, pers, testo.replace("|", "/"), lettura, dur))
            rows_csv.append([titolo, id, id + ".wav", pers, testo, lettura, "%.1f" % dur, "si" if fatto else "no"])
            etichette.append("%.3f\t%.3f\t%s" % (t, t + dur, id))
            t += dur + 2.0
        md.append("")
        open(os.path.join(OUT, "etichette", "%02d_%s.txt" % (k, cs)), "w", encoding="utf-8", newline="\n").write("\n".join(etichette) + "\n")
    open(os.path.join(OUT, "DA_REGISTRARE.md"), "w", encoding="utf-8", newline="\n").write("\n".join(md))
    with open(os.path.join(OUT, "copione.csv"), "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(["capitolo", "id", "file", "personaggio", "testo", "come leggerla", "durata (s)", "registrato"])
        w.writerows(rows_csv)
    print("file da registrare:", totale, "- minuti stimati:", round(sec / 60))


if __name__ == "__main__":
    main()
