# Copione del filmato iniziale (R025, fase 1)

File dati: `cutscene_intro.json` (id `intro`). **41 battute, durata stimata 4:58** (voce a ~15 caratteri/s + 0,6 s di respiro per battuta, più le pause senza testo di ogni capitolo).

Targhette dei controlli: chiavi astratte nel campo `controllo` (salta, muovi, guarda, interagisci, inventario, mappa, indizio), tradotte dal codice per tastiera e mouse, gamepad o touch.

| Capitolo | Epoca | Battute | Durata |
|---|---|---|---|
| 1. Il ritorno | presente | 001–006 | 0:42 |
| 2. Napoli, 1946 | ricordo | 007–012 | 0:42 |
| 3. I treni dei bambini | ricordo | 013–021 | 1:03 |
| 4. Le cose di Antonietta | presente | 022–026 | 0:39 |
| 5. Se perdi la strada | presente | 027–031 | 0:36 |
| 6. Perché ricordare | ricordo | 032–038 | 0:55 |
| 7. La porta | presente | 039–041 | 0:22 |
| **Totale** | | 41 | **4:58** |


## 1. Il ritorno (0:42)

- **Luogo:** la strada grande ai piedi dei Quartieri (nella pianta del 1946 è Via Roma; nel 1994 si chiamerebbe di nuovo Via Toledo, nome ripristinato nel 1980: in voce non nominarla), poi le rampe dei Quartieri fino allo slargo del basso (10_Hub_Vicolo)
- **Luce:** pomeriggio d'autunno, pioggia fine costante, cielo grigio; look del presente più freddo e neutro, poca grana
- **Camera:** apertura dal nero; dolly lento basso sul selciato bagnato, poi carrello dietro Amerigo adulto (cappotto lungo, custodia del violino); nessuno stacco brusco
- **Musica:** solo pioggia e ambiente del vicolo; il tema del violino (R002) entra piano, una sola linea, sulla battuta 004
- **Note:** sottotitoli sempre attivi; targhetta 'tieni premuto per saltare' nei primi secondi
- **Pausa senza testo:** 5 s

**cs_intro_001** · Narratore · _neutra_ · 5.8 s · azione `dissolvenza`, targhetta **salta**
> Napoli, 1994. Sui Quartieri Spagnoli piove da ore, una pioggia fine d'autunno.
<small>Regia: totale dall'alto dei tetti dei Quartieri sotto la pioggia, poi la strada bagnata e gli ombrelli scuri</small>

**cs_intro_002** · Amerigo adulto · _triste_ · 5.1 s · azione `entra_sx`
> Mi hanno telefonato ieri sera. Mia madre, Antonietta, se n'è andata.
<small>Regia: primo piano di Amerigo adulto sotto la pioggia, la custodia del violino in mano</small>

**cs_intro_003** · Amerigo adulto · _malinconica_ · 7.1 s
> Da questo vicolo sono partito che avevo sette anni. Poi ci sono tornato poco, e sempre di fretta.
<small>Regia: carrello dietro di lui mentre imbocca la rampa dei Quartieri</small>

**cs_intro_004** · Amerigo adulto · _pensierosa_ · 8.2 s · azione `zoom`
> I gradini, i panni stesi, l'edicola coi lumini. È tutto com'era. O forse lo guardo ancora con gli occhi di allora.
<small>Regia: soggettiva che sale le rampe; i lumini dell'edicola votiva</small>

**cs_intro_005** · Amerigo adulto · _malinconica_ · 6.7 s
> Il basso di mia madre. Una stanza sola sulla strada: lì si cucinava, si dormiva, si viveva.
<small>Regia: porta chiusa del basso, acqua che cola dall'architrave</small>

**cs_intro_006** · Amerigo adulto · _decisa_ · 4.2 s · azione `dissolvenza`
> Prima di aprire quella porta, devo ricordarmi chi ero.
<small>Regia: primo piano degli occhi; la pioggia si scioglie in luce di sole (passaggio al seppia)</small>


## 2. Napoli, 1946 (0:42)

- **Luogo:** lo stesso vicolo nel 1946 (30_FB_Scarpe): gradino, fontanella, porta del basso
- **Luce:** sole basso e aria asciutta, seppia marcato e bordi sfumati come i ricordi (R007)
- **Camera:** dissolvenza dalla pioggia al sole; camera all'altezza di un bambino; primo piano dei piedi nudi e sporchi sulla pietra, poi Amerigo bambino e Tommasino sul gradino
- **Musica:** il tema del violino si fa più caldo, pizzicato; rumori lontani del vicolo (voci, secchi, un carretto)
- **Note:** nessuna immagine di guerra esplicita; la miseria si racconta coi dettagli (piedi, toppe, pane)
- **Pausa senza testo:** 3 s

**cs_intro_007** · Narratore · _neutra_ · 7.1 s · azione `dissolvenza`
> Napoli, autunno 1946. La guerra è finita da poco, ma nei vicoli sono rimasti la fame e il freddo.
<small>Regia: lo stesso slargo nel 1946, aria asciutta, panni al sole</small>

**cs_intro_008** · Narratore · _neutra_ · 7.1 s
> Amerigo ha sette anni. Niente scarpe, una salopette piena di toppe, e due occhi che contano tutto.
<small>Regia: piedi nudi e sporchi sulla pietra, poi su fino al viso</small>

**cs_intro_009** · Amerigo bambino · _felice_ · 6.7 s · azione `entra_sx`
> Ventitré gradini, quattro lumini, tre gatti. E scarpe buone, oggi, ne sono passate due paia.
<small>Regia: Amerigo bambino sul gradino, conta con le dita</small>

**cs_intro_010** · Tommasino · _felice_ · 5.1 s · azione `entra_dx`
> Amerì, la vuoi smettere di contare? Tanto i numeri non si mangiano.
<small>Regia: Tommasino si siede accanto a lui, controcampo</small>

**cs_intro_011** · Antonietta · _neutra_ · 5.8 s · azione `entra_dx`
> Amerigo! Dentro, che fa scuro. E i piedi te li lavi prima di salire sul letto.
<small>Regia: Antonietta sulla soglia del basso, controluce, le mani nel grembiule</small>

**cs_intro_012** · Narratore · _pensierosa_ · 7.0 s
> Antonietta non sa leggere e parla poco. L'affetto lo mette nei rammendi, nel pane diviso a metà.
<small>Regia: dettaglio delle mani di Antonietta che rammendano una toppa a quadri</small>


## 3. I treni dei bambini (1:03)

- **Luogo:** vicolo 1946, i manifesti incollati al muro; davanti alla sede del Comitato; inserto finale di binari e vapore (stazione, anche solo come piano stretto)
- **Luce:** seppia; il pomeriggio scende, ombre lunghe tra i palazzi
- **Camera:** panoramica lenta sui manifesti; Maddalena di tre quarti tra le madri; sulle dicerie la camera si stringe sui volti nell'ombra; sui numeri, inquadratura larga del binario con fumo
- **Musica:** archi bassi sulle dicerie; il tema torna pieno sui numeri del 1946-47, poi si interrompe sulla voce di Amerigo adulto
- **Note:** dicerie solo nominate, senza dettagli macabri (TRAMA Decisione 10); PCI, UDI e Chiesa mai giudicati
- **Pausa senza testo:** 3 s

**cs_intro_013** · Narratore · _neutra_ · 7.3 s
> Un giorno, nel quartiere, compaiono manifesti nuovi: partono treni per il Nord, per i bambini poveri.
<small>Regia: panoramica sui manifesti incollati, carta fresca sopra quella vecchia</small>

**cs_intro_014** · Narratore · _neutra_ · 7.9 s
> Lassù ci sono famiglie pronte ad accoglierli per un inverno: un letto caldo, la scuola, un piatto ogni giorno.
<small>Regia: inserto: una cucina di campagna, la stufa accesa (ricordo immaginato, sfocato)</small>

**cs_intro_015** · Maddalena · _decisa_ · 6.2 s · azione `entra_dx`
> Non è carità, signo'. È gente che lavora e divide quello che ha. Con i vostri figli.
<small>Regia: Maddalena tra le madri davanti alla sede del Comitato</small>

**cs_intro_016** · Narratore · _seria_ · 6.7 s · azione `scuoti`
> Ma nei vicoli la paura corre più veloce dei treni. Si dice che quei bambini non torneranno.
<small>Regia: volti di donne nell'ombra delle porte, sussurri</small>

**cs_intro_017** · Maddalena · _decisa_ · 7.1 s
> Le chiacchiere non hanno mai sfamato nessuno. Venite al Comitato: guardate le carte, fate domande.
<small>Regia: Maddalena alza un foglio; la camera la guarda dal basso</small>

**cs_intro_018** · Antonietta · _seria_ · 3.3 s · azione `entra_sx`
> E chi me lo dice, a me, che me lo ridate?
<small>Regia: Antonietta con le braccia conserte, Amerigo dietro le sue gambe</small>

**cs_intro_019** · Narratore · _neutra_ · 8.3 s
> Tra il 1945 e il 1952, circa settantamila bambini del Sud e del Centro furono ospitati da famiglie del Centro-Nord.
<small>Regia: binario, vapore, una fila di cappotti piccoli vista da lontano</small>

**cs_intro_020** · Narratore · _neutra_ · 6.1 s
> Da Napoli, tra il Natale del '46 e l'estate del '47, ne partirono circa diecimila.
<small>Regia: cartellini con i nomi che oscillano al vento</small>

**cs_intro_021** · Amerigo adulto · _malinconica_ · 7.2 s · azione `dissolvenza`
> Su uno di quei treni c'ero anch'io. Il resto, adesso, me lo devono raccontare le cose di mia madre.
<small>Regia: il fumo del treno riempie il quadro e diventa pioggia</small>


## 4. Le cose di Antonietta (0:39)

- **Luogo:** ritorno nel vicolo sotto la pioggia; inserti come ricordi lampo degli oggetti (scarpe, latta, quaderno) su fondo scuro
- **Luce:** presente freddo; gli oggetti negli inserti in luce calda di lampada a petrolio
- **Camera:** la camera si avvicina alla porta chiusa del basso; ogni oggetto ruota piano in primo piano (come nella vista ravvicinata EsaminaView)
- **Musica:** carillon di poche note sul tema, pioggia sotto
- **Note:** le targhette dei controlli compaiono piccole, in basso, come etichette di carta d'epoca, solo durante la battuta che le nomina
- **Pausa senza testo:** 2 s

**cs_intro_022** · Amerigo adulto · _pensierosa_ · 6.9 s
> Mia madre non buttava via niente. Ogni cosa che teneva aveva un motivo, anche se non lo diceva.
<small>Regia: di nuovo nel presente, la mano di Amerigo sulla porta chiusa</small>

**cs_intro_023** · Narratore · _neutra_ · 7.8 s · targhetta **muovi**
> Ritrova gli oggetti di Antonietta. Ognuno apre un ricordo: il 1946, vissuto di nuovo con i piedi di Amerigo.
<small>Regia: Amerigo si volta verso il vicolo; la camera lo segue alle spalle</small>

**cs_intro_024** · Narratore · _neutra_ · 7.1 s · azione `zoom`, targhetta **interagisci**
> Avvicinati a ciò che ti incuriosisce e prendilo in mano: le cose di casa parlano a chi le guarda.
<small>Regia: inserto: un paio di scarpe consumate che ruota piano nella luce di una lampada</small>

**cs_intro_025** · Narratore · _neutra_ · 7.1 s
> Dentro ogni ricordo ti aspettano enigmi: lucchetti, date, documenti, cose da rimettere in ordine.
<small>Regia: inserti rapidi ma morbidi: un lucchetto a ruote, un calendario del '46, una fotografia storta</small>

**cs_intro_026** · Amerigo adulto · _pensierosa_ · 8.0 s · targhetta **inventario**
> Quello che trovo lo metto nella latta, come faceva lei coi bottoni. Quello che capisco, lo scrivo nel quaderno.
<small>Regia: la scatola di latta che si apre; accanto un quaderno di scuola con la copertina nera</small>


## 5. Se perdi la strada (0:36)

- **Luogo:** vicolo dall'alto: la camera sale sopra i panni stesi e i tetti, poi la vista si appiattisce e diventa la pianta disegnata (Resources/Mappe/10_Hub_Vicolo.jpg)
- **Luce:** pioggia, luce grigia uniforme; sulla pianta, carta invecchiata
- **Camera:** crane lento verso l'alto; morph dalla città vera alla pianta; la croce dell'obiettivo si disegna a matita
- **Musica:** tema sospeso, pianissimo
- **Note:** mostrare la minimappa in alto a sinistra mentre la si nomina; poi un pannello d'enigma col pulsante Indizio evidenziato e un errore con il feedback gentile
- **Pausa senza testo:** 2 s

**cs_intro_027** · Amerigo adulto · _pensierosa_ · 6.2 s · targhetta **guarda**
> Qui i vicoli si somigliano tutti. Da bambino li conoscevo a memoria, adesso non più.
<small>Regia: la camera sale sopra i panni stesi e i tetti bagnati</small>

**cs_intro_028** · Narratore · _neutra_ · 6.9 s
> In un angolo hai una piccola pianta del quartiere. Ti dice dove sei e come si chiama la strada.
<small>Regia: la città vista dall'alto diventa la pianta disegnata a inchiostro</small>

**cs_intro_029** · Narratore · _neutra_ · 6.5 s · targhetta **mappa**
> Quando non sai più dove andare, sulla pianta compare una croce: è il tuo prossimo passo.
<small>Regia: una croce a matita si disegna sulla pianta</small>

**cs_intro_030** · Narratore · _neutra_ · 7.5 s · targhetta **indizio**
> In ogni enigma c'è un Indizio. Chiederlo non è barare: nel vicolo, chi è in difficoltà bussa al vicino.
<small>Regia: pannello di un enigma, il pulsante Indizio si illumina appena</small>

**cs_intro_031** · Narratore · _neutra_ · 6.4 s
> E se sbagli, nessuno ti punisce. Il ricordo ti spiega perché, e aspetta che tu riprovi.
<small>Regia: una risposta sbagliata, un biglietto gentile che spiega; la ruota del lucchetto torna indietro</small>


## 6. Perché ricordare (0:55)

- **Luogo:** sequenza di quadri seppia: bambini in fila con i cartellini, una scheda elettorale del 1946 (inserto), una pagina stampata della Costituzione (1948), una tavola apparecchiata in una cucina del Nord
- **Luce:** seppia che a poco a poco si scalda verso colori naturali sull'ultima battuta
- **Camera:** inquadrature fisse con lentissimo avvicinamento (effetto Ken Burns in 3D)
- **Musica:** il tema completo del violino, una volta sola, senza enfasi
- **Note:** date e articoli solo dalla voce narrante o da Amerigo adulto: i personaggi del 1946 non citano la Costituzione
- **Pausa senza testo:** 3 s

**cs_intro_032** · Narratore · _seria_ · 7.6 s · azione `dissolvenza`
> Nel 1946, per un bambino povero, mangiare ogni giorno, curarsi, andare a scuola non era affatto scontato.
<small>Regia: bambini in fila, cappotti troppo grandi, cartellini al collo</small>

**cs_intro_033** · Narratore · _neutra_ · 8.3 s
> Quell'anno, il 2 giugno, donne e uomini votarono insieme: nacque la Repubblica e fu eletta l'Assemblea Costituente.
<small>Regia: inserto: una scheda elettorale del 1946, una matita copiativa</small>

**cs_intro_034** · Narratore · _neutra_ · 7.7 s
> Dal 1948 la Costituzione chiede alla Repubblica di togliere gli ostacoli che rendono le persone disuguali.
<small>Regia: una pagina stampata della Costituzione; la camera si posa sull'articolo 3</small>

**cs_intro_035** · Narratore · _neutra_ · 6.6 s
> Protegge l'infanzia, apre la scuola a tutti, e chiede a ciascuno un dovere di solidarietà.
<small>Regia: articoli 31, 34 e 2 che scorrono, senza leggerli tutti</small>

**cs_intro_036** · Narratore · _neutra_ · 8.1 s
> I treni dei bambini vennero prima di quelle parole: associazioni, partiti e famiglie comuni le misero in pratica.
<small>Regia: una tavola del Nord apparecchiata per un bambino in più</small>

**cs_intro_037** · Amerigo adulto · _commossa_ · 7.3 s
> Per me i diritti hanno avuto prima una faccia, un letto, un piatto caldo. Le parole sono venute dopo.
<small>Regia: il seppia si scalda verso i colori naturali</small>

**cs_intro_038** · Narratore · _seria_ · 6.2 s · azione `dissolvenza`
> Ricordare serve anche a chiedersi: chi resta indietro oggi? E chi gli tende la mano?
<small>Regia: nero lento</small>


## 7. La porta (0:22)

- **Luogo:** slargo del basso nel 10_Hub_Vicolo: porte simili, panni stesi con le toppe, punto di spawn del giocatore
- **Luce:** pioggia, luce del gioco (stessa del gameplay: niente stacco di look)
- **Camera:** la camera cinematografica scivola dietro la spalla di Amerigo e si fonde nella camera di gioco (CameraRig); ultima battuta sopra il gioco già attivo
- **Musica:** il tema sfuma nell'ambiente del vicolo (audio.json della scena)
- **Note:** fine filmato: controllo al giocatore, compaiono la minimappa con la X dell'obiettivo ob_bucato e la riga 'Trova la tua porta tra i panni stesi' (mappe.json)
- **Pausa senza testo:** 4 s

**cs_intro_039** · Amerigo adulto · _pensierosa_ · 6.0 s · azione `dissolvenza`
> Eccomi. Il vicolo, la pioggia... e tutte queste porte uguali. Qual era la nostra?
<small>Regia: Amerigo adulto nello slargo, si guarda intorno; porte tutte simili</small>

**cs_intro_040** · Amerigo adulto · _malinconica_ · 7.5 s
> Mamma stendeva i panni sopra la porta. E le toppe le cuciva sempre uguali a quelle della mia salopette.
<small>Regia: i panni stesi gocciolano; una toppa a quadri si muove nel vento, appena visibile</small>

**cs_intro_041** · Narratore · _decisa_ · 4.7 s · targhetta **muovi**
> Il primo ricordo comincia qui, tra i panni stesi. Tocca a te.
<small>Regia: la camera diventa la camera di gioco; compaiono minimappa, X dell'obiettivo e 'Trova la tua porta tra i panni stesi'</small>

