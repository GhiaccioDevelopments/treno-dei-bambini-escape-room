# -*- coding: utf-8 -*-
"""R026: scrive i campi "cosaFare" e "cosaOttieni" (biglietto d'apertura) in TUTTI gli enigmi del gioco (idempotente).
File: flashback_*.json, enigmi_mondo.json, segreti.json. Rilanciare DOPO ogni generatore che riscrive quei file
(genera_flashback_*.py, genera_segreti.py). Uso: python aggiungi_biglietti.py
Regole: "cosaFare" = una frase all'imperativo, breve; "cosaOttieni" = che cosa si ottiene (oggetto, gettone, riga del quaderno, pezzo di storia).
Mai la soluzione nel biglietto (quella sta negli indizi a tre livelli). Un enigma senza riga qui fa fallire lo script.
"""
import glob, json, os

D = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Data")

B = {
    # ---------------------------------------------------------------- hub e basso (enigmi_mondo.json)
    "enigma_bucato": ("Segui le toppe che conosci sui panni stesi.", "La porta di casa: il basso di Antonietta."),
    "enigma_exvoto": ("Guarda gli ex-voto dell'edicola e trova quello che nasconde qualcosa.", "La chiave del basso."),
    "enigma_manifesti": ("Stacca i manifesti uno alla volta, fino al più vecchio.", "L'avviso del Comitato per i treni e una pagina del quaderno."),
    "enigma_foto": ("Raddrizza la fotografia e guarda che cosa c'è dietro.", "Tre disegni in fila: un aiuto per la cassetta di latta."),
    "enigma_calendario": ("Conta le scarpette disegnate mese per mese.", "Le quattro cifre di un codice."),
    "enigma_cassetta": ("Gira le quattro rotelle sul codice giusto.", "Le cose preziose di Antonietta e la chiavetta del baule."),
    "enigma_baule": ("Apri il baule con la chiavetta e cerca sotto la coperta.", "Le scarpe del viaggio: il primo ricordo."),
    # ---------------------------------------------------------------- Le scarpe
    "flashback_scarpe/enigma_passanti": ("Dai a ogni paio di scarpe il mestiere di chi le porta.", "Il calzolaio riconosciuto e la prima cifra del lucchetto."),
    "flashback_scarpe/enigma_nascosto": ("Trova nella strada cinque calzature e provale sulle forme giuste.", "Una cifra del lucchetto, scritta sulla forma più piccola."),
    "flashback_scarpe/enigma_quiz": ("Rispondi alle domande toccando la riga giusta dei documenti.", "Una cifra del lucchetto, sul margine del quaderno."),
    "flashback_scarpe/enigma_ordina": ("Metti i ritagli di giornale in ordine, dal più antico al più recente.", "L'ultima cifra del lucchetto."),
    "flashback_scarpe/enigma_lucchetto": ("Componi le quattro cifre raccolte lungo la strada.", "L'uscita dal ricordo."),
    # ---------------------------------------------------------------- Stracci
    "flashback_stracci/enigma_giro": ("Raccogli i dieci stracci del vicolo e guardali bene al banco.", "Il sacco degli stracci buoni."),
    "flashback_stracci/enigma_ceste": ("Metti ogni straccio nella cesta giusta.", "Le ceste pronte per la pesata."),
    "flashback_stracci/enigma_bilancia": ("Controlla la bilancia con il peso campione, poi pesa le ceste.", "I pesi giusti scritti sul registro."),
    "flashback_stracci/enigma_conto": ("Fai il conto delle ceste e metti sul banco le monete giuste.", "Il primo soldo guadagnato da Amerigo."),
    "flashback_stracci/enigma_mestieri": ("Abbina ogni attrezzo al suo mestiere e al diritto che lo protegge oggi.", "La porta della bottega si apre."),
    # ---------------------------------------------------------------- Il volantino
    "flashback_volantino/enigma_voci": ("Ascolta le voci del vicolo e rileggile nel quaderno.", "Quattro dicerie da controllare."),
    "flashback_volantino/enigma_bacheca": ("Collega ogni diceria al foglio del Comitato che le risponde.", "Le parole giuste per il volantino."),
    "flashback_volantino/enigma_ciclostile": ("Scegli tre frasi, prepara la matrice e stampa a ritmo.", "Un pacco di volantini e le cinque cose dell'accoglienza."),
    "flashback_volantino/enigma_ruoli": ("Dai ogni oggetto alla persona a cui serve.", "Il retro del volantino stampato."),
    "flashback_volantino/enigma_timbro": ("Timbra le quattro date giuste sulla linea del tempo.", "Il volantino timbrato e l'uscita dal ricordo."),
    # ---------------------------------------------------------------- Il cappotto
    "flashback_cappotto/enigma_cartellini": ("Appendi a ogni bambino il cartellino giusto, leggendo il registro.", "Il tuo numero per il viaggio: 112."),
    "flashback_cappotto/enigma_cappotti": ("Fai provare a ogni bambino il cappotto della sua misura.", "Un cappotto per il viaggio, e una sorpresa."),
    "flashback_cappotto/enigma_mela": ("Prendi quello che ti dà Antonietta e mettilo in valigia.", "Il saluto: si può salire sul treno."),
    "flashback_cappotto/enigma_finestrino": ("Segna sulla carta le città che passano nel finestrino.", "L'arrivo vicino: Modena."),
    "flashback_cappotto/enigma_paura": ("Parla con i tre bambini e trova la risposta giusta per ognuno.", "Lo scompartimento tranquillo e la fine del viaggio."),
    # ---------------------------------------------------------------- Il violino
    "flashback_violino/enigma_forno": ("Leggi il forno con la farina, scaldalo giusto, inforna e sforna a tempo.", "Il pane per Alcide."),
    "flashback_violino/enigma_attrezzi": ("Ascolta (o guarda l'onda) e tocca l'attrezzo che fa quel suono.", "Il diapason di Alcide."),
    "flashback_violino/enigma_pianoforte": ("Accorda le tre corde finché il battimento sparisce.", "La promessa di un violino."),
    "flashback_violino/enigma_impronte": ("Confronta scarponi e impronte, poi segui le orme giuste.", "Il violino ritrovato."),
    "flashback_violino/enigma_melodia": ("Ascolta la frase di Alcide e ripetila sui segni colorati.", "Lo spartito di Alcide."),
    # ---------------------------------------------------------------- Il finale
    "flashback_finale/enigma_basso94": ("Guarda il telefono, il calendario e la scatola di latta.", "I cinque posti dei ricordi."),
    "flashback_finale/enigma_posto_scarpe": ("Posa qui il ricordo che abitava questo posto.", "Un frammento di carta."),
    "flashback_finale/enigma_posto_stracci": ("Posa qui il ricordo che abitava questo posto.", "Un frammento di carta."),
    "flashback_finale/enigma_posto_volantino": ("Posa qui il ricordo che abitava questo posto.", "Un frammento di carta."),
    "flashback_finale/enigma_posto_cappotto": ("Posa qui il ricordo che abitava questo posto.", "Un frammento di carta."),
    "flashback_finale/enigma_posto_spartito": ("Posa qui il ricordo che abitava questo posto.", "Un frammento di carta."),
    "flashback_finale/enigma_ricevuta": ("Ricomponi il documento strappato, pezzo per pezzo.", "Un numero che apre qualcosa."),
    "flashback_finale/enigma_custodia": ("Apri la custodia con il numero giusto.", "Il violino, e un segno di tua madre."),
    "flashback_finale/enigma_carmine": ("Scegli che cosa fare con Carmine: ogni scelta va bene.", "La fine della storia."),
    # ---------------------------------------------------------------- segreti (segreti.json)
    "segreti/enigma_portella": ("Apri la portella con la filastrocca dei tre tasselli.", "La scatola dei ragazzi del vicolo."),
    "segreti/enigma_ripostiglio": ("Capisci perché la credenza è stata spostata tante volte.", "Il posto dove dormiva Amerigo."),
    "segreti/enigma_carretto": ("Guarda sotto il sedile del carretto.", "Un biglietto di Capa 'e fierro."),
    "segreti/enigma_impronte": ("Apri il cassetto del leggio con le due rotelle.", "Un ricordo delle donne dell'UDI."),
    "segreti/enigma_valigia": ("Apri il lucchetto della valigia dimenticata.", "Un regalo per i bambini del treno."),
    "segreti/enigma_carta_italia": ("Rimetti al suo posto la puntina che manca.", "Un disegno nascosto."),
    "segreti/enigma_radio": ("Sintonizza la radio sulla frequenza segnata a matita.", "Una musica che conosci."),
}


def chiave(nome_file, d, e):
    if nome_file == "enigmi_mondo":
        return e["id"]
    if nome_file == "segreti":
        return "segreti/" + e["id"]
    return d.get("id", nome_file) + "/" + e["id"]


def main():
    mancano = []
    for path in sorted(glob.glob(os.path.join(D, "flashback_*.json"))) + [os.path.join(D, "enigmi_mondo.json"), os.path.join(D, "segreti.json")]:
        nome = os.path.splitext(os.path.basename(path))[0]
        raw = open(path, encoding="utf-8").read()
        d = json.loads(raw)
        cambiato = False
        for e in d.get("enigmi", []):
            k = chiave(nome, d, e)
            if k not in B:
                mancano.append(k)
                continue
            fare, ottieni = B[k]
            if e.get("cosaFare") != fare or e.get("cosaOttieni") != ottieni:
                e["cosaFare"], e["cosaOttieni"] = fare, ottieni
                cambiato = True
        if cambiato:
            righe = raw.replace("\r\n", "\n").split("\n")
            ind = (len(righe[1]) - len(righe[1].lstrip(" "))) if len(righe) > 1 else 2   # stessa indentazione del file (alcuni usano 1 spazio)
            s = json.dumps(d, ensure_ascii=False, indent=ind or 2) + ("\n" if raw.endswith("\n") else "")
            if "\r\n" in raw: s = s.replace("\n", "\r\n")
            open(path, "w", encoding="utf-8", newline="").write(s)
            print("aggiornato", nome)
    if mancano:
        raise SystemExit("Enigmi senza biglietto: " + ", ".join(mancano))
    print("biglietti:", len(B))


if __name__ == "__main__":
    main()
