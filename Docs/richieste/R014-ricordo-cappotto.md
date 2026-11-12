# R014 – Nuovo ricordo "Il cappotto": la stazione e il treno

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 605f952. **Il codice attuale vince** |
| **Dipende da** | R013 (regole comuni nella sezione 2 di `R012-ricordo-stracci.md`) |
| **Ok dell'utente** | **Sì (2026-11-06)** |

## 1. Cosa ha chiesto l'utente
> FAI TUTTE LE ALTRE STANZE, CON ENIGMI SEMPRE PIÙ DIVERSI E BELLI, DEVE SPACCARE IL GIOCO

## 2. Il capitolo
- **Luoghi**: `stazione` (bambini lavati, vestiti, schedati prima della partenza, TRAMA §6) e il **vagone** del treno
  in viaggio verso il Nord (scena nuova, spazio stretto ma pieno di vita: finestrini, valigie di cartone, bambini).
- **Personaggi**: Antonietta (saluto e **mela annurca**, Decisione 9), Tommasino, Mariuccia, volontarie, la vecchia che semina paura.
- **Emozione / tema**: sorpresa e nostalgia; solidarietà e accoglienza.
- Il primo paio di scarpe di Amerigo arriva con il viaggio (Decisione 12), senza dire nei testi chi glielo dà (TRAMA §9).
- **Idee di enigmi** (da rendere nuovi): cartellini con i nomi da ritrovare tra la folla della stazione; abbinare
  bambino–cappotto con indizi di taglia e dettagli (deduzione); **enigma in movimento sul treno** (meccanismo nuovo:
  il paesaggio che scorre dal finestrino, stazioni che passano, una mappa ferroviaria d'epoca da ricostruire); quiz sulla
  solidarietà trasformato in scelte nei dialoghi con i bambini spaventati (consolare, spiegare le dicerie false).
- La neve scambiata per ricotta è "da verificare" (TRAMA §4): non usarla come fatto del libro; se piace, va come idea originale del gioco.
- Oggetto ottenuto: **il cappotto** → apre il capitolo "Violino".

## 3. Criteri di accettazione
Come R012 (schema approvato prima, capitolo completo, meccanismo nuovo, coerenza TRAMA, media in `Docs/richieste/media/R014/`).

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R014_schema.md`](schemi/R014_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 stazione generica, ispirata alla vecchia stazione di Napoli, senza ricostruzione fedele.
  - D2 stazione e vagone entrambi giocabili.

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto

**Stato: codice completo, da provare in Unity (2026-11-12).** Lavoro fatto al PC della scuola **senza Unity né MCP**: codice e dati scritti a mano, controllati
leggendo i file esistenti (tipi, firme, namespace), **mai compilati né provati**. Le scene `60_FB_Stazione` e `61_FB_Vagone` non esistono ancora: le costruisce il menu a casa.
Base: `richiesta/R014-ricordo-cappotto` con dentro `richiesta/R013-ricordo-volantino` (che contiene R012); merge senza conflitti, il codice di R012/R013 vince.

### Fatto
- **Due scene giocabili** (D2) da `Assets/_Game/Editor/SceneBuilderCappotto.cs` (menu **Treno > Scene > Costruisci ricordo Cappotto (R014)**, incluse in "Costruisci tutte le scene", ora 9):
  - `60_FB_Stazione`: stazione generica ispirata alla vecchia stazione di Napoli (D1, nessun edificio vero): atrio 30x16 m con soffitto a travi, tre arcate, orologio,
    insegne nostre (`testi_ambiente.json`), tavoli del Comitato (C1) e della Croce Rossa (C2), montagna di cappotti, panche, valigie, folla di figure semplici
    (madri, bambini, volontarie, la Pachiochia); pensilina con lucernario e colonne di ghisa, due binari, treno con tre carrozze di terza (la 214 al centro, numero
    dipinto sulla fiancata), tender e locomotiva; Antonietta (segnaposto) sulla banchina con la valigia (C3); muretti invisibili ai bordi della banchina.
  - `61_FB_Vagone`: carrozza 214 stretta e piena: nove scompartimenti con panche, reticelle con valigie, lanterne, finestrini veri da cui scorre il paesaggio
    (`PaesaggioTreno`: pali, alberi, colline a velocità diverse; la luce va verso la notte dopo C4), bambini seduti, un'accompagnatrice, la carta della linea (C4),
    lo scompartimento dei tre bambini (C5) e la porta della discesa (uscita).
- **Ricordo su due scene**: gli enigmi hanno il campo `scena`; `EscapeRoomController` crea solo quelli della scena e conta gli altri per il totale e l'uscita.
  Dalla stazione al vagone si passa con una `Door` (luogo `ricordo_vagone`, gettone `partenza` dato da C3).
- **Cinque enigmi** in `flashback_cappotto.json` (generato da `ArtSource/Cappotto/genera_flashback_cappotto.py`): catena `richiede/produce`, indizi a 3 livelli,
  errori gentili, domande per "Enigmi semplificati", schede civiche:
  - **C1 I cartellini scambiati** – *deduzione su figure vive (nuovo)*: i bambini della fila e il riflesso di Amerigo nel vetro della biglietteria si muovono appena;
    i cartellini hanno solo il numero; dal registro si capisce di chi è ognuno e lo si appende al collo; si dice quanti sono sbagliati, non quali.
  - **C2 I cappotti della Croce Rossa** – *abbinare per taglia e dettagli (nuovo)*: sei misure (una da uomo); troppo grande, troppo piccolo, misura giusta ma numero di un
    altro hanno messaggi diversi; quello di Amerigo ha il 112; poi la scatola con le scarpe, **senza dire chi le dà** (Decisione 12). Parte la cutscene di metà.
  - **C3 La mela** – gesto senza errore: la mela annurca dalla mano di Antonietta alla valigia (Decisione 9).
  - **C4 L'Italia dal finestrino** – *enigma in movimento (nuovo)*: nella vista ravvicinata scorrono campi, colline, pali e cartelli; si appuntano Roma, Firenze, Bologna
    quando passano (i cartelli delle ferrovie no); una città persa la ripete il controllore; la luce va dal giorno alla notte; alla fine si tocca Modena.
  - **C5 I bambini che hanno paura** – *dialoghi a scelta (nuovo)*: Peppino, Rosetta, Gaetano (finzione), tre paure; consolare, scherzare o mostrare il volantino
    (dalla latta); una risposta che non aiuta non è un errore. Produce il cappotto.
- Codice: `EsaminaViewCappotto.cs` (agganci in `EsaminaView.cs` ed `EsaminaViewStracci.ConfermaStracci`: C / gamepad Nord = Appunta o Rispondi, tasti 1-3), campi nuovi
  in `DataClasses.cs` (`figure`, `fermate`, `periodo`, `cutscene`, `cutsceneCapitolo`; `FiguraDef`, `RispostaDef`, `FermataDef`), gruppo di pezzi `vista` (solo nella
  vista ravvicinata), `EnigmaMondo` fa partire un capitolo di cutscene a enigma risolto, `PaesaggioTreno.cs`.
- **Cutscene** `cutscene_cappotto.json`: 22 battute `cs_cappotto_001..022` (ingresso 001-008, `meta` 009-013, chiusura 014-022), dialoghi originali; personaggi nuovi
  `mariuccia`, `controllore` (senza ritratto); riusati `pachiochia` e `madre_vicolo`.
- **Oggetto** `cappotto`: cappottino blu appeso dietro la porta del basso (x 1,15; y 1,1; z -2,43), creato da `IngressoRicordo` dopo `flashback_volantino`.
- **Luoghi**: `stazione` (rimanda al cappottino), `ricordo_cappotto`, `ricordo_vagone`, **`modena` sbloccata dal cappotto** (`sbloccatoDa`, scena vuota finché R015).
  **Mappe e obiettivi** (piante delle due scene, cinque `ob_cappotto_*`), **audio** delle due scene.
- **Segreti R011**: enigma segreto **la valigia dimenticata** sulla reticella (lucchetto 214 = numero della carrozza) e 5 collezionabili (biglietto di terza, bottone della
  Croce Rossa, gessetto, cartolina da Firenze, fazzoletto ricamato). Punti in `Docs/richieste/media/R014/punti_segreti_stazione.md` e `punti_segreti_vagone.md`.
- **Prova automatica** `CappottoTest` (`-cappottotest [-cappottotestout cartella]`, menu **Treno > Scene > Prova automatica ricordo Cappotto (Play)**).
- **Hub, `Porta_stazione`**: le porte del quartiere avevano il pivot nell'origine del mondo; le prove che misurano `transform.position` vedevano la porta della
  stazione a quota 0, "a 28 m dal percorribile" (la verifica dell'editor la dava già a 0,40 m). Ora `MakeDoor` costruisce la mesh attorno al punto della parete e
  posa lì l'oggetto (stesso aspetto e collider). **Da riprovare** con `-segretitest` dopo aver ricostruito l'hub.

### Scostamenti dalla specifica
- Build Settings non toccate a mano: le due scene le aggiunge il menu.
- Il riflesso di Amerigo nel vetro della biglietteria fa da "Amerigo" in C1 e C2; il terzo bambino di C1 è Pasqualino (finzione); in C2 i bambini ci sono solo nella vista.
- C4: nel mondo la carta è sulla parete di testa; il finestrino che scorre è nella vista ravvicinata (nel mondo scorrono i finestrini veri).
- La vecchia della paura è la **Pachiochia** di R013; volontarie, Antonietta, controllore e folla sono **figure di primitive**.
- **Amerigo con cappotto e scarpe** (variante del modello) non fatto: serve MakeHuman. Treno di primitive (nessun CC0 cercato), niente fumo a particelle (nebbia).

### Fatti storici: versione prudente scelta
- Linea Napoli, Roma, Firenze, Bologna, Modena; **nessuna fermata intermedia nominata** (gli altri cartelli sono cartelli delle ferrovie); durata non scritta.
- Data "inverno 1946-47"; vestiti "in gran parte" dalla Croce Rossa; doni delle donne dei comitati solo in scheda e segreto.
- Neve "ricotta" **non usata** (Tommasino: "pare zucchero", battuta nostra). Chi dà le scarpe non si dice. Mariuccia e Tommasino senza cognome; numeri, nomi dei
  bambini e carrozza 214 inventati. Chiusura: "è ancora buio" all'arrivo a Modena (la cronaca racconta un arrivo di mattina).

### Decisioni prese da Claude
- Due scene unite da una porta del treno; uscita solo nel vagone; "Rivivi il ricordo" riparte dalla stazione.
- Cappottino dietro la porta del basso (da controllare a occhio); porta della stazione nell'hub chiusa con un messaggio che rimanda al cappottino.
- Amerigo n. 112, carrozza 214, cartelli ogni 6,5 s; nel JSON accenti veri (il generatore converte gli apostrofi).

### Da fare a casa con Unity (in quest'ordine)
1. Aprire il progetto: **console senza errori** (nuovi o toccati: `EsaminaViewCappotto.cs`, `EsaminaView.cs`, `EsaminaViewStracci.cs`, `EnigmaMondo.cs`,
   `EscapeRoomController.cs`, `DataClasses.cs`, `PaesaggioTreno.cs`, `CappottoTest.cs`, `SceneBuilderCappotto.cs`, `SceneBuilder.cs`, `SceneBuilderQuartiere.cs`,
   `SceneBuilderAmbienti.cs`, `LookBuilder.cs`, `MappaBuilder.cs`, le verifiche, `SegretiTools.cs`, `BenchmarkRunner.cs`). I `.meta` nuovi hanno GUID scritti a mano.
2. Prima chiudere i punti aperti di R012 e R013.
3. **Treno > Segreti > Genera texture dei pezzi** (`cartolinafirenze`).
4. **Treno > Scene > Costruisci ricordo Cappotto (R014)**: guardare atrio, banchina, treno, carrozza e le postazioni degli enigmi.
5. **Ricostruire l'hub** (correzione di `Porta_stazione`), poi `-segretitest`: la porta non deve più risultare irraggiungibile.
6. Basso: controllare il cappottino dietro la porta (ritoccare `ingresso` in `aggiorna_dati_cappotto.py`).
7. **Treno > Mappa > Genera piante**; **Treno > Segreti > Verifica punti** (e se serve `python ArtSource/Segreti/genera_segreti.py`).
8. Verifiche: Compenetrazioni, Superfici e buchi, Camera (carrozza stretta), Raggiungibilità.
9. Un solo **bake** delle due scene.
10. Prove: `-cappottotest` (atteso 0 FAIL), poi `-volantinotest`, `-straccitest`, `-enigmitest`, `-segretitest`; a mano cartellini, cappotti, finestrino (il paesaggio
    non deve vedersi oltre la parete) e dialogo, anche col gamepad.
11. FPS Alta/Bassa, screenshot e GIF in `Docs/richieste/media/R014/`.
12. Personaggi 3D (Antonietta, Mariuccia, Tommasino, volontarie, controllore) e Amerigo con cappotto e scarpe; ritratti di `mariuccia` e `controllore`.
13. Aggiornare questo Rapporto, i criteri e la riga nel README.

### Criteri di accettazione
- [x] Schema approvato prima dello sviluppo (2026-11-07).
- [ ] Capitolo completo e giocabile, salvataggio compreso, con meccanismi nuovi (scritto e coperto dalla prova automatica, **non ancora eseguito**).
- [ ] Coerenza con TRAMA.md, testi originali, ID battute, console pulita, FPS per preset (testi e ID fatti; console e FPS da verificare).
- [ ] Media in `Docs/richieste/media/R014/` (per ora solo i rapporti dei punti segreti).
