"""Divide il file dei dati della build WebGL in parti sotto i 100 MB (limite dei file su GitHub/gh-pages)
e modifica index.html perche' le scarichi, le riunisca in memoria e avvii il gioco col file riunito.
Uso: python Tools/WebGL/dividi_data_webgl.py Build/WebGL   (dopo ogni build WebGL, prima della pubblicazione)"""
import json, os, sys

MAX = 90 * 1024 * 1024  # 90 MB per parte: margine sotto il limite di 100 MB

def main(cartella):
    build = os.path.join(cartella, "Build")
    dati = os.path.join(build, "WebGL.data.unityweb")
    html = os.path.join(cartella, "index.html")
    if not os.path.exists(dati):
        print("Niente da dividere (manca %s: gia' diviso?)" % dati); return
    if os.path.getsize(dati) <= MAX:
        print("File dei dati sotto i 90 MB: nessuna divisione"); return
    parti = []
    with open(dati, "rb") as f:
        i = 1
        while True:
            blocco = f.read(MAX)
            if not blocco: break
            nome = "WebGL.data.unityweb.part%d" % i
            with open(os.path.join(build, nome), "wb") as o: o.write(blocco)
            parti.append(nome); i += 1
    os.remove(dati)
    s = open(html, encoding="utf-8").read()
    vecchio = "      document.body.appendChild(script);"
    nuovo = """      // Il file dei dati supera i 100 MB (limite di GitHub): e' diviso in parti, qui si riuniscono prima di avviare il gioco
      var partiDati = %s;
      (async () => {
        const barra = document.querySelector("#unity-progress-bar-full");
        document.querySelector("#unity-loading-container").style.display = "block";
        const pezzi = [];
        for (let i = 0; i < partiDati.length; i++) {
          const r = await fetch(buildUrl + "/" + partiDati[i]);
          if (!r.ok) throw new Error("Download non riuscito: " + partiDati[i] + " (" + r.status + ")");
          pezzi.push(await r.blob());
          barra.style.width = (50 * (i + 1) / partiDati.length) + "%%";
        }
        config.dataUrl = URL.createObjectURL(new Blob(pezzi));
        document.body.appendChild(script);
      })().catch((e) => alert(e));""" % json.dumps(parti)
    if vecchio not in s: sys.exit("index.html: punto di avvio non trovato")
    open(html, "w", encoding="utf-8").write(s.replace(vecchio, nuovo, 1))
    print("Diviso in %d parti: %s" % (len(parti), ", ".join(parti)))

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Build/WebGL")
