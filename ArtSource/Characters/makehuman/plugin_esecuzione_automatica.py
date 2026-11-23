# plugin TEMPORANEO: esegue lo script indicato in TRENO_SCRIPT appena MakeHuman e' pronto, poi chiude. Nessun clic.
import os
import traceback
import mh
from core import G
import log

_state = {"id": None, "n": 0}

def _run():
    _state["n"] += 1
    try:
        if G.app.selectedHuman is None or _state["n"] < 3:
            return
        mh.removeTimer(_state["id"])
        src = open(os.environ["TRENO_SCRIPT"], encoding="utf-8").read()
        try:
            exec(src)
        except Exception:
            log.error("autorun: " + traceback.format_exc())
        open(os.environ["TRENO_DONE"], "w").write("ok")
        G.app.stop()
    except Exception:
        log.error("autorun: " + traceback.format_exc())

def load(app):
    _state["id"] = mh.addTimer(4000, _run)

def unload(app):
    pass
