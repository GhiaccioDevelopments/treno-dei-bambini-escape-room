# -*- coding: utf-8 -*-
"""R028: testi del pulsante altoparlante e della riga delle Impostazioni (ui_menu.json, blocco "voce"). Idempotente, conserva il formato."""
import json, os

p = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data", "ui_menu.json")
raw = open(p, encoding="utf-8", newline="").read()
d = json.loads(raw)
d["voce"] = {"mancante": "voce non ancora registrata", "autoNome": "Leggi in automatico il Narratore",
             "autoSpiega": "Le battute del Narratore partono da sole quando la voce è registrata. Spento: si ascoltano solo con il pulsante altoparlante (o il tasto V)."}
righe = raw.replace("\r\n", "\n").split("\n")
ind = (len(righe[1]) - len(righe[1].lstrip(" "))) or 2
s = json.dumps(d, ensure_ascii=False, indent=ind) + ("\n" if raw.endswith("\n") else "")
if "\r\n" in raw: s = s.replace("\n", "\r\n")
open(p, "w", encoding="utf-8", newline="").write(s)
print("ok")
