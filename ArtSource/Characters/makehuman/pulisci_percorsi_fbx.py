# -*- coding: utf-8 -*-
r"""
Rimuove dagli FBX i percorsi personali (cartella home dell'utente, che contiene il nome utente di Windows).

Gli FBX esportati da MakeHuman memorizzano i percorsi ASSOLUTI delle texture. Il repository e' pubblico:
questo script sostituisce nei byte del file la cartella home con un segnaposto della STESSA lunghezza in byte
(le stringhe dell'FBX binario hanno la lunghezza come prefisso, quindi la lunghezza non deve cambiare).

Uso (solo libreria standard, va bene qualsiasi Python 3, anche quello di MakeHuman):
    python pulisci_percorsi_fbx.py file1.fbx [file2.fbx ...]
    python pulisci_percorsi_fbx.py --home "C:\Users\altro"  file.fbx     (cartella home diversa da quella corrente)
"""
import os
import re
import sys


def varianti_home(home):
    """Coppie (cercata, sostituita) in byte, della stessa lunghezza, per tutte le varianti della cartella home."""
    home = home.rstrip("/" + chr(92))
    nome = os.path.basename(home.replace("\\", "/"))
    if not nome:
        return []
    padre = home.replace("\\", "/")[: -len(nome)]          # es. "C:/Users/"
    segnaposto_nome = "x" * len(nome)
    coppie = []
    visti = set()
    for sep in ("/", "\\"):
        for maius in (False, True):
            h = (padre + nome).replace("/", sep)
            s = (padre + segnaposto_nome).replace("/", sep)
            if maius:
                h, s = h.upper(), s.upper()
            for codifica in ("utf-8", "utf-16-le"):
                a, b = h.encode(codifica), s.encode(codifica)
                if a not in visti and len(a) == len(b):
                    visti.add(a)
                    coppie.append((a, b))
    return coppie


def pulisci(percorso, home=None):
    """Sostituisce i percorsi personali nel file. Restituisce il numero di sostituzioni."""
    home = home or os.path.expanduser("~")
    with open(percorso, "rb") as f:
        dati = f.read()
    totale = 0
    for cercata, sostituita in varianti_home(home):
        # insensibile alle maiuscole (Windows) ma stessa lunghezza
        rx = re.compile(re.escape(cercata), re.IGNORECASE)
        dati, n = rx.subn(lambda m: sostituita, dati)
        totale += n
    # il solo nome utente (es. nelle cartelle fuori da \Users\) non e' coperto: lo segnala il controllo finale
    if totale:
        with open(percorso, "wb") as f:
            f.write(dati)
    return totale


def contiene_nome_utente(percorso, nome=None):
    nome = nome or os.path.basename(os.path.expanduser("~"))
    with open(percorso, "rb") as f:
        dati = f.read().lower()
    return nome.lower().encode("utf-8") in dati or nome.lower().encode("utf-16-le") in dati


def main(argv):
    home = None
    if len(argv) >= 2 and argv[0] == "--home":
        home, argv = argv[1], argv[2:]
    if not argv:
        print(__doc__)
        return 2
    codice = 0
    for p in argv:
        n = pulisci(p, home)
        resto = contiene_nome_utente(p, os.path.basename((home or os.path.expanduser("~")).replace("\\", "/").rstrip("/")))
        print("%s: %d sostituzioni%s" % (p, n, "  ATTENZIONE: il nome utente compare ancora" if resto else ""))
        if resto:
            codice = 1
    return codice


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
