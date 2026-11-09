# Genera cutscene_intro.json e copione_intro.md (R025, fase 1: copione).
import json, os, sys

OUT = sys.argv[1]

# Durata stimata: voce narrata lenta ~15 caratteri/s + 0,6 s di respiro, minimo 3 s.
def durata(t):
    return round(max(3.0, len(t) / 15.0 + 0.6), 1)

CAPITOLI = [
    dict(id="c1_ritorno", titolo="1. Il ritorno", epoca="presente",
         sfondo="cs_vicolo", pausa=5,
         regia=dict(luogo="la strada grande ai piedi dei Quartieri (nella pianta del 1946 è Via Roma; nel 1994 si chiamerebbe di nuovo Via Toledo, nome ripristinato nel 1980: in voce non nominarla), poi le rampe dei Quartieri fino allo slargo del basso (10_Hub_Vicolo)",
                    luce="pomeriggio d'autunno, pioggia fine costante, cielo grigio; look del presente più freddo e neutro, poca grana",
                    camera="apertura dal nero; dolly lento basso sul selciato bagnato, poi carrello dietro Amerigo adulto (cappotto lungo, custodia del violino); nessuno stacco brusco",
                    musica="solo pioggia e ambiente del vicolo; il tema del violino (R002) entra piano, una sola linea, sulla battuta 004",
                    note="sottotitoli sempre attivi; targhetta 'tieni premuto per saltare' nei primi secondi")),
    dict(id="c2_1946", titolo="2. Napoli, 1946", epoca="ricordo",
         sfondo="cs_ricordo", pausa=3,
         regia=dict(luogo="lo stesso vicolo nel 1946 (30_FB_Scarpe): gradino, fontanella, porta del basso",
                    luce="sole basso e aria asciutta, seppia marcato e bordi sfumati come i ricordi (R007)",
                    camera="dissolvenza dalla pioggia al sole; camera all'altezza di un bambino; primo piano dei piedi nudi e sporchi sulla pietra, poi Amerigo bambino e Tommasino sul gradino",
                    musica="il tema del violino si fa più caldo, pizzicato; rumori lontani del vicolo (voci, secchi, un carretto)",
                    note="nessuna immagine di guerra esplicita; la miseria si racconta coi dettagli (piedi, toppe, pane)")),
    dict(id="c3_treni", titolo="3. I treni dei bambini", epoca="ricordo",
         sfondo="cs_ricordo", pausa=3,
         regia=dict(luogo="vicolo 1946, i manifesti incollati al muro; davanti alla sede del Comitato; inserto finale di binari e vapore (stazione, anche solo come piano stretto)",
                    luce="seppia; il pomeriggio scende, ombre lunghe tra i palazzi",
                    camera="panoramica lenta sui manifesti; Maddalena di tre quarti tra le madri; sulle dicerie la camera si stringe sui volti nell'ombra; sui numeri, inquadratura larga del binario con fumo",
                    musica="archi bassi sulle dicerie; il tema torna pieno sui numeri del 1946-47, poi si interrompe sulla voce di Amerigo adulto",
                    note="dicerie solo nominate, senza dettagli macabri (TRAMA Decisione 10); PCI, UDI e Chiesa mai giudicati")),
    dict(id="c4_oggetti", titolo="4. Le cose di Antonietta", epoca="presente",
         sfondo="cs_basso", pausa=2,
         regia=dict(luogo="ritorno nel vicolo sotto la pioggia; inserti come ricordi lampo degli oggetti (scarpe, latta, quaderno) su fondo scuro",
                    luce="presente freddo; gli oggetti negli inserti in luce calda di lampada a petrolio",
                    camera="la camera si avvicina alla porta chiusa del basso; ogni oggetto ruota piano in primo piano (come nella vista ravvicinata EsaminaView)",
                    musica="carillon di poche note sul tema, pioggia sotto",
                    note="le targhette dei controlli compaiono piccole, in basso, come etichette di carta d'epoca, solo durante la battuta che le nomina")),
    dict(id="c5_strada", titolo="5. Se perdi la strada", epoca="presente",
         sfondo="cs_vicolo", pausa=2,
         regia=dict(luogo="vicolo dall'alto: la camera sale sopra i panni stesi e i tetti, poi la vista si appiattisce e diventa la pianta disegnata (Resources/Mappe/10_Hub_Vicolo.jpg)",
                    luce="pioggia, luce grigia uniforme; sulla pianta, carta invecchiata",
                    camera="crane lento verso l'alto; morph dalla città vera alla pianta; la croce dell'obiettivo si disegna a matita",
                    musica="tema sospeso, pianissimo",
                    note="mostrare la minimappa in alto a sinistra mentre la si nomina; poi un pannello d'enigma col pulsante Indizio evidenziato e un errore con il feedback gentile")),
    dict(id="c6_perche", titolo="6. Perché ricordare", epoca="ricordo",
         sfondo="cs_ricordo", pausa=3,
         regia=dict(luogo="sequenza di quadri seppia: bambini in fila con i cartellini, una scheda elettorale del 1946 (inserto), una pagina stampata della Costituzione (1948), una tavola apparecchiata in una cucina del Nord",
                    luce="seppia che a poco a poco si scalda verso colori naturali sull'ultima battuta",
                    camera="inquadrature fisse con lentissimo avvicinamento (effetto Ken Burns in 3D)",
                    musica="il tema completo del violino, una volta sola, senza enfasi",
                    note="date e articoli solo dalla voce narrante o da Amerigo adulto: i personaggi del 1946 non citano la Costituzione")),
    dict(id="c7_porta", titolo="7. La porta", epoca="presente",
         sfondo="cs_vicolo", pausa=4,
         regia=dict(luogo="slargo del basso nel 10_Hub_Vicolo: porte simili, panni stesi con le toppe, punto di spawn del giocatore",
                    luce="pioggia, luce del gioco (stessa del gameplay: niente stacco di look)",
                    camera="la camera cinematografica scivola dietro la spalla di Amerigo e si fonde nella camera di gioco (CameraRig); ultima battuta sopra il gioco già attivo",
                    musica="il tema sfuma nell'ambiente del vicolo (audio.json della scena)",
                    note="fine filmato: controllo al giocatore, compaiono la minimappa con la X dell'obiettivo ob_bucato e la riga 'Trova la tua porta tra i panni stesi' (mappe.json)")),
]

# (capitolo, personaggio, testo, emozione, azione, posizione, controllo, inquadratura)
B = [
 ("c1_ritorno","narratore","Napoli, 1994. Sui Quartieri Spagnoli piove da ore, una pioggia fine d'autunno.","neutra","dissolvenza","sx","salta","totale dall'alto dei tetti dei Quartieri sotto la pioggia, poi la strada bagnata e gli ombrelli scuri"),
 ("c1_ritorno","amerigo_adulto","Mi hanno telefonato ieri sera. Mia madre, Antonietta, se n'è andata.","triste","entra_sx","sx","","primo piano di Amerigo adulto sotto la pioggia, la custodia del violino in mano"),
 ("c1_ritorno","amerigo_adulto","Da questo vicolo sono partito che avevo sette anni. Poi ci sono tornato poco, e sempre di fretta.","malinconica","","sx","","carrello dietro di lui mentre imbocca la rampa dei Quartieri"),
 ("c1_ritorno","amerigo_adulto","I gradini, i panni stesi, l'edicola coi lumini. È tutto com'era. O forse lo guardo ancora con gli occhi di allora.","pensierosa","zoom","sx","","soggettiva che sale le rampe; i lumini dell'edicola votiva"),
 ("c1_ritorno","amerigo_adulto","Il basso di mia madre. Una stanza sola sulla strada: lì si cucinava, si dormiva, si viveva.","malinconica","","sx","","porta chiusa del basso, acqua che cola dall'architrave"),
 ("c1_ritorno","amerigo_adulto","Prima di aprire quella porta, devo ricordarmi chi ero.","decisa","dissolvenza","sx","","primo piano degli occhi; la pioggia si scioglie in luce di sole (passaggio al seppia)"),

 ("c2_1946","narratore","Napoli, autunno 1946. La guerra è finita da poco, ma nei vicoli sono rimasti la fame e il freddo.","neutra","dissolvenza","sx","","lo stesso slargo nel 1946, aria asciutta, panni al sole"),
 ("c2_1946","narratore","Amerigo ha sette anni. Niente scarpe, una salopette piena di toppe, e due occhi che contano tutto.","neutra","","sx","","piedi nudi e sporchi sulla pietra, poi su fino al viso"),
 ("c2_1946","amerigo","Ventitré gradini, quattro lumini, tre gatti. E scarpe buone, oggi, ne sono passate due paia.","felice","entra_sx","sx","","Amerigo bambino sul gradino, conta con le dita"),
 ("c2_1946","tommasino","Amerì, la vuoi smettere di contare? Tanto i numeri non si mangiano.","felice","entra_dx","dx","","Tommasino si siede accanto a lui, controcampo"),
 ("c2_1946","antonietta","Amerigo! Dentro, che fa scuro. E i piedi te li lavi prima di salire sul letto.","neutra","entra_dx","dx","","Antonietta sulla soglia del basso, controluce, le mani nel grembiule"),
 ("c2_1946","narratore","Antonietta non sa leggere e parla poco. L'affetto lo mette nei rammendi, nel pane diviso a metà.","pensierosa","","sx","","dettaglio delle mani di Antonietta che rammendano una toppa a quadri"),

 ("c3_treni","narratore","Un giorno, nel quartiere, compaiono manifesti nuovi: partono treni per il Nord, per i bambini poveri.","neutra","","sx","","panoramica sui manifesti incollati, carta fresca sopra quella vecchia"),
 ("c3_treni","narratore","Lassù ci sono famiglie pronte ad accoglierli per un inverno: un letto caldo, la scuola, un piatto ogni giorno.","neutra","","sx","","inserto: una cucina di campagna, la stufa accesa (ricordo immaginato, sfocato)"),
 ("c3_treni","maddalena","Non è carità, signo'. È gente che lavora e divide quello che ha. Con i vostri figli.","decisa","entra_dx","dx","","Maddalena tra le madri davanti alla sede del Comitato"),
 ("c3_treni","narratore","Ma nei vicoli la paura corre più veloce dei treni. Si dice che quei bambini non torneranno.","seria","scuoti","sx","","volti di donne nell'ombra delle porte, sussurri"),
 ("c3_treni","maddalena","Le chiacchiere non hanno mai sfamato nessuno. Venite al Comitato: guardate le carte, fate domande.","decisa","","dx","","Maddalena alza un foglio; la camera la guarda dal basso"),
 ("c3_treni","antonietta","E chi me lo dice, a me, che me lo ridate?","seria","entra_sx","sx","","Antonietta con le braccia conserte, Amerigo dietro le sue gambe"),
 ("c3_treni","narratore","Tra il 1945 e il 1952, circa settantamila bambini del Sud e del Centro furono ospitati da famiglie del Centro-Nord.","neutra","","sx","","binario, vapore, una fila di cappotti piccoli vista da lontano"),
 ("c3_treni","narratore","Da Napoli, tra il Natale del '46 e l'estate del '47, ne partirono circa diecimila.","neutra","","sx","","cartellini con i nomi che oscillano al vento"),
 ("c3_treni","amerigo_adulto","Su uno di quei treni c'ero anch'io. Il resto, adesso, me lo devono raccontare le cose di mia madre.","malinconica","dissolvenza","sx","","il fumo del treno riempie il quadro e diventa pioggia"),

 ("c4_oggetti","amerigo_adulto","Mia madre non buttava via niente. Ogni cosa che teneva aveva un motivo, anche se non lo diceva.","pensierosa","","sx","","di nuovo nel presente, la mano di Amerigo sulla porta chiusa"),
 ("c4_oggetti","narratore","Ritrova gli oggetti di Antonietta. Ognuno apre un ricordo: il 1946, vissuto di nuovo con i piedi di Amerigo.","neutra","","sx","muovi","Amerigo si volta verso il vicolo; la camera lo segue alle spalle"),
 ("c4_oggetti","narratore","Avvicinati a ciò che ti incuriosisce e prendilo in mano: le cose di casa parlano a chi le guarda.","neutra","zoom","sx","interagisci","inserto: un paio di scarpe consumate che ruota piano nella luce di una lampada"),
 ("c4_oggetti","narratore","Dentro ogni ricordo ti aspettano enigmi: lucchetti, date, documenti, cose da rimettere in ordine.","neutra","","sx","","inserti rapidi ma morbidi: un lucchetto a ruote, un calendario del '46, una fotografia storta"),
 ("c4_oggetti","amerigo_adulto","Quello che trovo lo metto nella latta, come faceva lei coi bottoni. Quello che capisco, lo scrivo nel quaderno.","pensierosa","","sx","inventario","la scatola di latta che si apre; accanto un quaderno di scuola con la copertina nera"),

 ("c5_strada","amerigo_adulto","Qui i vicoli si somigliano tutti. Da bambino li conoscevo a memoria, adesso non più.","pensierosa","","sx","guarda","la camera sale sopra i panni stesi e i tetti bagnati"),
 ("c5_strada","narratore","In un angolo hai una piccola pianta del quartiere. Ti dice dove sei e come si chiama la strada.","neutra","","sx","","la città vista dall'alto diventa la pianta disegnata a inchiostro"),
 ("c5_strada","narratore","Quando non sai più dove andare, sulla pianta compare una croce: è il tuo prossimo passo.","neutra","","sx","mappa","una croce a matita si disegna sulla pianta"),
 ("c5_strada","narratore","In ogni enigma c'è un Indizio. Chiederlo non è barare: nel vicolo, chi è in difficoltà bussa al vicino.","neutra","","sx","indizio","pannello di un enigma, il pulsante Indizio si illumina appena"),
 ("c5_strada","narratore","E se sbagli, nessuno ti punisce. Il ricordo ti spiega perché, e aspetta che tu riprovi.","neutra","","sx","","una risposta sbagliata, un biglietto gentile che spiega; la ruota del lucchetto torna indietro"),

 ("c6_perche","narratore","Nel 1946, per un bambino povero, mangiare ogni giorno, curarsi, andare a scuola non era affatto scontato.","seria","dissolvenza","sx","","bambini in fila, cappotti troppo grandi, cartellini al collo"),
 ("c6_perche","narratore","Quell'anno, il 2 giugno, donne e uomini votarono insieme: nacque la Repubblica e fu eletta l'Assemblea Costituente.","neutra","","sx","","inserto: una scheda elettorale del 1946, una matita copiativa"),
 ("c6_perche","narratore","Dal 1948 la Costituzione chiede alla Repubblica di togliere gli ostacoli che rendono le persone disuguali.","neutra","","sx","","una pagina stampata della Costituzione; la camera si posa sull'articolo 3"),
 ("c6_perche","narratore","Protegge l'infanzia, apre la scuola a tutti, e chiede a ciascuno un dovere di solidarietà.","neutra","","sx","","articoli 31, 34 e 2 che scorrono, senza leggerli tutti"),
 ("c6_perche","narratore","I treni dei bambini vennero prima di quelle parole: associazioni, partiti e famiglie comuni le misero in pratica.","neutra","","sx","","una tavola del Nord apparecchiata per un bambino in più"),
 ("c6_perche","amerigo_adulto","Per me i diritti hanno avuto prima una faccia, un letto, un piatto caldo. Le parole sono venute dopo.","commossa","","sx","","il seppia si scalda verso i colori naturali"),
 ("c6_perche","narratore","Ricordare serve anche a chiedersi: chi resta indietro oggi? E chi gli tende la mano?","seria","dissolvenza","sx","","nero lento"),

 ("c7_porta","amerigo_adulto","Eccomi. Il vicolo, la pioggia... e tutte queste porte uguali. Qual era la nostra?","pensierosa","dissolvenza","sx","","Amerigo adulto nello slargo, si guarda intorno; porte tutte simili"),
 ("c7_porta","amerigo_adulto","Mamma stendeva i panni sopra la porta. E le toppe le cuciva sempre uguali a quelle della mia salopette.","malinconica","","sx","","i panni stesi gocciolano; una toppa a quadri si muove nel vento, appena visibile"),
 ("c7_porta","narratore","Il primo ricordo comincia qui, tra i panni stesi. Tocca a te.","decisa","","sx","muovi","la camera diventa la camera di gioco; compaiono minimappa, X dell'obiettivo e 'Trova la tua porta tra i panni stesi'"),
]

capmap = {c["id"]: c for c in CAPITOLI}
battute = []
for i, (cap, pers, testo, emo, az, pos, ctrl, inq) in enumerate(B, 1):
    assert len(testo) <= 125, (i, len(testo), testo)
    c = capmap[cap]
    battute.append(dict(
        id=f"cs_intro_{i:03d}", capitolo=cap, personaggio=pers, testo=testo, emozione=emo,
        audio="", azione=az, sfondo=(c["sfondo"] if i == 1 or B[i-2][0] != cap else ""),
        posizione=pos, controllo=ctrl, durata=durata(testo),
        regia=dict(inquadratura=inq, epoca=c["epoca"])))

for c in CAPITOLI:
    bs = [b for b in battute if b["capitolo"] == c["id"]]
    c["durata"] = round(sum(b["durata"] for b in bs) + c["pausa"], 1)
    c["battute"] = [bs[0]["id"], bs[-1]["id"]]

totale = round(sum(c["durata"] for c in CAPITOLI), 1)
doc = dict(
    id="intro", titolo="Il ritorno", sfondo="cs_vicolo",
    durataStimata=totale,
    obiettivoFinale="ob_bucato",
    personaggiNuovi=[dict(id="amerigo_adulto", nome="Amerigo", ritratto="amerigo_adulto", colore="#5A4A3C")],
    capitoli=[dict(id=c["id"], titolo=c["titolo"], epoca=c["epoca"], durata=c["durata"],
                   pausaSenzaTesto=c["pausa"], battute=c["battute"], regia=c["regia"]) for c in CAPITOLI],
    battute=battute)

with open(os.path.join(OUT, "cutscene_intro.json"), "w", encoding="utf-8") as f:
    json.dump(doc, f, ensure_ascii=False, indent=2)

nomi = {"narratore":"Narratore","amerigo":"Amerigo bambino","amerigo_adulto":"Amerigo adulto",
        "antonietta":"Antonietta","tommasino":"Tommasino","maddalena":"Maddalena"}
def mmss(s): s=int(round(s)); return f"{s//60}:{s%60:02d}"
L = ["# Copione del filmato iniziale (R025, fase 1)", "",
     f"File dati: `cutscene_intro.json` (id `intro`). **{len(battute)} battute, durata stimata {mmss(totale)}** "
     "(voce a ~15 caratteri/s + 0,6 s di respiro per battuta, più le pause senza testo di ogni capitolo).", "",
     "Targhette dei controlli: chiavi astratte nel campo `controllo` (salta, muovi, guarda, interagisci, inventario, mappa, indizio), "
     "tradotte dal codice per tastiera e mouse, gamepad o touch.", "",
     "| Capitolo | Epoca | Battute | Durata |", "|---|---|---|---|"]
for c in CAPITOLI:
    L.append(f"| {c['titolo']} | {c['epoca']} | {c['battute'][0][-3:]}–{c['battute'][1][-3:]} | {mmss(c['durata'])} |")
L.append(f"| **Totale** | | {len(battute)} | **{mmss(totale)}** |")
L.append("")
for c in CAPITOLI:
    r = c["regia"]
    L += ["", f"## {c['titolo']} ({mmss(c['durata'])})", "",
          f"- **Luogo:** {r['luogo']}", f"- **Luce:** {r['luce']}", f"- **Camera:** {r['camera']}",
          f"- **Musica:** {r['musica']}", f"- **Note:** {r['note']}", f"- **Pausa senza testo:** {c['pausa']} s", ""]
    for b in [b for b in battute if b["capitolo"] == c["id"]]:
        extra = []
        if b["azione"]: extra.append(f"azione `{b['azione']}`")
        if b["controllo"]: extra.append(f"targhetta **{b['controllo']}**")
        L.append(f"**{b['id']}** · {nomi[b['personaggio']]} · _{b['emozione']}_ · {b['durata']} s" + (" · " + ", ".join(extra) if extra else ""))
        L.append(f"> {b['testo']}")
        L.append(f"<small>Regia: {b['regia']['inquadratura']}</small>")
        L.append("")
with open(os.path.join(OUT, "copione_intro.md"), "w", encoding="utf-8") as f:
    f.write("\n".join(L) + "\n")

for c in CAPITOLI: print(c["titolo"], mmss(c["durata"]), c["battute"])
print("battute", len(battute), "totale", mmss(totale), totale)
print("max len", max(len(b["testo"]) for b in battute))
