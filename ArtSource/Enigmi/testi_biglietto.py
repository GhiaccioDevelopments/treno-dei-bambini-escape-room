# -*- coding: utf-8 -*-
"""R026: etichette del biglietto d'apertura (enigmi_mondo.json, testi) e riga delle Impostazioni (ui_menu.json, enigmiImp). Idempotente, conserva il formato."""
import json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")


def aggiorna(nome, fn):
    path = os.path.join(D, nome + ".json")
    raw = open(path, encoding="utf-8", newline="").read()
    d = json.loads(raw)
    fn(d)
    righe = raw.replace("\r\n", "\n").split("\n")
    ind = (len(righe[1]) - len(righe[1].lstrip(" "))) or 2
    s = json.dumps(d, ensure_ascii=False, indent=ind) + ("\n" if raw.endswith("\n") else "")
    if "\r\n" in raw: s = s.replace("\n", "\r\n")
    open(path, "w", encoding="utf-8", newline="").write(s)


def testi(d):
    d["testi"].update({"bigliettoFare": "Cosa fare", "bigliettoOttieni": "Cosa ottieni",
                       "bigliettoChiudi": "Tocca o premi un tasto per cominciare. Il pulsante ? lo rimostra.", "btnSpiega": "?"})


def menu(d):
    d["enigmiImp"].update({"spiegazioneNome": "Mostra la spiegazione all'apertura",
                           "spiegazioneSpiega": "Quando apri un enigma compare un biglietto con due righe: che cosa fare e che cosa ottieni. Si rilegge con il pulsante ?."})


aggiorna("enigmi_mondo", testi)
aggiorna("ui_menu", menu)
print("ok")
