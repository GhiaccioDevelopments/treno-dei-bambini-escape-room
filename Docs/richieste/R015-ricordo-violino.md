# R015 – Nuovo ricordo "Il violino": Modena, casa Benvenuti

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 605f952. **Il codice attuale vince** |
| **Dipende da** | R014 (regole comuni nella sezione 2 di `R012-ricordo-stracci.md`) |
| **Ok dell'utente** | **Sì (2026-11-06)** |

## 1. Cosa ha chiesto l'utente
> FAI TUTTE LE ALTRE STANZE, CON ENIGMI SEMPRE PIÙ DIVERSI E BELLI, DEVE SPACCARE IL GIOCO

## 2. Il capitolo
- **Luoghi** (nuovi, look diverso da Napoli: inverno emiliano, neve o nebbia, interni caldi): casa di Derna, casa e
  laboratorio dei Benvenuti (Alcide accordatore di pianoforti, il forno di Rosa), la scuola.
  Abiti d'epoca di Modena 1946 (CLAUDE.md).
- **Personaggi**: Derna, Alcide, Rosa, Rivo, Luzio, Nario (uno dei ragazzi all'inizio geloso). Dati incerti (es. Derna
  vedova) non presentati come certi (TRAMA §9).
- **Emozione / tema**: meraviglia e orgoglio; istruzione, pregiudizio Nord-Sud, cultura.
- **Idee di enigmi** (i più "belli" del gioco, sul suono): **enigma musicale** – ascoltare e riprodurre una melodia semplice
  originale (non *Bella ciao* né musiche del film) sul violino o sul pianoforte da accordare (meccanismo nuovo: suono e
  intonazione, con alternativa visiva per chi non sente bene); abbinare strumento–suono nel laboratorio di Alcide; il forno di
  Rosa (ricetta/tempi, meccanismo fisico); a scuola, enigma sul pregiudizio trasformato in dialogo con il ragazzo geloso.
- Musica e suoni: seguire `audio.json` e il tema del violino già esistente (R002).
- Oggetto ottenuto: **il violino** → apre il "Finale".

## 3. Criteri di accettazione
Come R012 (schema approvato prima, capitolo completo, meccanismo nuovo, accessibilità dell'enigma musicale, coerenza TRAMA,
media in `Docs/richieste/media/R015/`).

## Schema approvato (2026-11-07)

- Schema: [`Docs/richieste/schemi/R015_schema.md`](schemi/R015_schema.md), approvato con le correzioni del lead.
- Decisioni che riguardano questa richiesta:
  - D1 una sola scena a Modena: corte con casa di Derna, casa Benvenuti e scuola.
  - D2 l'oggetto del capitolo è lo spartito di Alcide. Il violino si ritrova solo nel finale (R016).

Schema approvato dall'utente: sviluppo autorizzato.

## Rapporto

**Stato: codice completo, da provare in Unity (2026-11-13).** Lavoro fatto al PC della scuola **senza Unity né MCP**: codice e dati scritti a mano, controllati
leggendo i file esistenti (tipi, firme, namespace, nomi già usati nelle classi parziali), **mai compilati né provati**. La scena `70_FB_Modena` non esiste ancora:
la costruisce il menu a casa. Base: `richiesta/R015-ricordo-violino` con dentro R014, R013 e R012 (il loro codice vince).

### Fatto
- **Una scena giocabile** (D1) da `Assets/_Game/Editor/SceneBuilderViolino.cs` (menu **Treno > Scene > Costruisci ricordo Violino (R015)**, inclusa in "Costruisci tutte
  le scene", ora 10): `70_FB_Modena`, corte di pianura nell'inverno 1946-47, look diverso da Napoli (neve, nebbia lattea, luce fredda fuori, stufe e lampade calde dentro):
  - **corte** innevata chiusa da un muretto, cancello a sud (uscita dal ricordo), pozzo, pagliaio, carretto, legnaia, alberi spogli, pupazzo di neve, campi bianchi intorno;
  - **casa di Derna** (ovest): cucina con la stufa accesa, tavolo, credenza, lampada; **stanzetta di Amerigo** con letto vero, cuscino, coperta, finestra sulla neve;
  - **casa-laboratorio dei Benvenuti** (est): forno di Rosa (V1), banco dell'accordatore (V2), pianoforte verticale (V3), madia, sacchi, scaffale, insegna d'epoca;
  - **strada** fra due staccionate (V4 all'imbocco) fino alla **scuola**: un'aula sola con banchi a due posti, lavagna, alfabeto, stufa, cattedra (V5), legnaia fuori;
  - **neve che cade**: un sistema di particelle leggero (al massimo 900 fiocchi; materiale `Assets/_Game/Generated/M_FiocchiNeve.mat` creato dal menu);
  - materiale nuovo **`Neve`** (`M_Neve`, tinta del lino) e suoli `neve` e `cotto`; volumi camera negli interni (muri che si dissolvono).
- **Cinque enigmi sul suono** in `flashback_violino.json` (generato da `ArtSource/Violino/genera_flashback_violino.py`): catena `richiede/produce`, indizi a 3 livelli,
  errori gentili, domande per "Enigmi semplificati", schede civiche. **Ogni suono ha un'alternativa visiva**: si risolvono tutti anche senza sentire.
  - **V1 La prova della farina** – *prova fisica e tempo (nuovo)*: il forno ha un calore che scende da solo; la legna lo alza, la serranda aperta lo fa scappare; un pizzico
    di farina sulla pietra resta bianco, diventa nocciola o nero; si inforna solo dopo una lettura giusta e si sforna a clessidra vuota (presto = si rimette dentro,
    tardi = si ricomincia). Produce il pane per Alcide.
  - **V2 Gli attrezzi che suonano** – *abbina suono e attrezzo*: Alcide fa suonare un attrezzo nascosto (diapason, chiave, feltri, martelletto, corda: suoni sintetizzati);
    l'onda compare disegnata col gesso sulla lavagnetta; toccando un attrezzo lo si sente e si vede la sua onda. Tasti 1-5. Produce il diapason.
  - **V3 Il pianoforte stonato** – *intonazione con i battimenti (nuovo)*: corda e riferimento insieme fanno "ua-ua" con tanti battiti al secondo quanti Hz di scarto;
    la lancetta sul telaio trema allo stesso ritmo; la chiave gira a mezzi scatti (pulsanti, frecce sul pianoforte, tasti Z/X); se il battimento accelera si gira dalla
    parte sbagliata; ferma = accordata. Tre corde. Produce la promessa del violino.
  - **V4 Le impronte nella neve** – *tracce (nuovo)*: si confronta uno scarpone dei ragazzi con un'impronta (numero, chiodi, tacco: il gioco dice la prima cosa che non
    combacia); riconosciute le orme di Rivo (tacco rattoppato) che escono dal laboratorio, si seguono scegliendo a ogni bivio la traccia giusta (le altre: un gatto, la
    maestra, neve liscia) fino alla legnaia della scuola. Parte la cutscene di metà (Rivo restituisce il violino, le scarpe, la risata).
  - **V5 La prima melodia** – *ascolta e ripeti (nuovo)*: Alcide suona due frasi (4 e 6 note) di una melodia **originale** (pentatonica di sol); le note si accendono sulla
    tastiera e sullo spartito a pallini colorati; si ripetono toccando i segni (tasti 1-5); una nota sbagliata = Alcide la rifà più lenta; Rivo batte il tempo. Produce lo spartito.
- Codice: `EsaminaViewViolino.cs` (agganci in `EsaminaView.cs` ed `EsaminaViewStracci.ConfermaStracci`: C / gamepad Nord = Ascolta), campi nuovi in `DataClasses.cs`
  (`forno`, `suoni`, `corde`, `impronte`, `note`, `frasi`; `FornoDef`, `SuonoDef`, `CordaDef`, `ImprontaDef`, `FraseDef`), componente `MusicaSospesa` (la musica di scena
  si ferma mentre si ascolta). Suoni sintetizzati con `Synth` (stesso violino del tema R002), identici in WebGL.
- **Cutscene** `cutscene_violino.json`: 22 battute `cs_violino_001..022` (ingresso 001-008, `meta` 009-013, chiusura 014-022), dialoghi originali (per Derna e Alcide le due
  frasi d'esempio di TRAMA §8); personaggi nuovi `derna`, `alcide`, `rosa`, `rivo`, `luzio`, `nario` (senza ritratto).
- **Oggetto** `spartito` (D2, "Lo spartito di Alcide"): cassettino socchiuso sulla credenza della cucina del basso (x 2,07; y 0,965; z 6,9), creato da `IngressoRicordo`
  dopo `flashback_cappotto`. **Luoghi**: `modena` (rimanda al cassettino), `ricordo_violino`. **Mappe e obiettivi** (pianta, sei aree, cinque `ob_violino_*`), **audio**
  della scena (vento e campane già esistenti), **insegne** (scuola, laboratorio, forno, lavagna, alfabeto).
- **Segreti R011**: enigma segreto **la carta d'Italia** (le puntine dei bambini ospiti fanno un treno; la puntina di Napoli è caduta sulla stufa spenta: rimessa al suo posto
  apre lo sportello, dentro c'è il disegno di Amerigo) e 5 collezionabili (pagella, quaderno a righe, pupazzetto di neve, corda di violino, cartolina di Modena).
  Punti in `Docs/richieste/media/R015/punti_segreti_modena.md`; blocco in `ArtSource/Violino/segreti_violino_blocco.py`; `segreti.json` rigenerato (segreti vecchi identici).
- **Prova automatica** `ViolinoTest` (`-violinotest [-violinotestout cartella]`, menu **Treno > Scene > Prova automatica ricordo Violino (Play)**): spartito solo dopo il
  cappotto, ingresso, i cinque meccanismi con i casi sbagliati (forno freddo, troppo caldo, pallido, bruciato; attrezzo sbagliato; giro sbagliato; scarpone e ramo
  sbagliati; nota sbagliata), ricarica a metà, uscita, versione semplificata, mappa, obiettivi, segreti.

### Scostamenti dalla specifica
- Build Settings non toccate a mano: la scena la aggiunge il menu.
- I sei personaggi (Derna, Alcide, Rosa, Rivo, Luzio, Nario) **non ci sono ancora in 3D**: servono MakeHuman e Blender. Nella scena niente figure di primitive (negli
  interni, in primo piano, sarebbero brutte): i personaggi parlano nelle cutscene; in V5 di Rivo si vede solo la mano che batte il tempo.
- V4 si gioca nella vista ravvicinata (diorama della corte con la pista), non camminando sulle orme nel mondo.
- La neve non passa da `MeteoController` (che conosce solo la pioggia): è un sistema di particelle della scena.
- La lettera ad Antonietta è solo in cutscene (Derna scrive, Amerigo detta).

### Decisioni prese da Claude
- **Il ragazzo geloso è Rivo** [GIOCO]: le fonti non dicono quale; il film usa "Lucio", quindi non Luzio.
- L'oggetto del capitolo ha id **`spartito`** (non `violino`, che resta libero per il finale R016); il flashback si chiama comunque `flashback_violino`.
- Lo spartito sta in un cassettino sulla credenza della **cucina** del basso (il volantino è sull'altra credenza): posizione da controllare a occhio.
- Melodia: sol, si, re, si / re, mi, re, si, la, sol (pentatonica, originale). Accordatura: scarti 3, -2,5 e 4 Hz, mezzo scatto = 0,5 Hz. Forno: 110 gradi all'inizio,
  giusto 220-265, un ciocco +42, cottura 12 s (buono da 2,5 s prima a 4 s dopo).
- Nessun termometro nel forno: l'unica lettura è la farina (e la fiamma che cresce), come nello schema.
- Nel JSON accenti veri (il generatore riusa quello del cappotto).

### Da fare a casa con Unity (in quest'ordine)
1. Prima chiudere R012, R013 e R014 (liste nei loro Rapporti).
2. Aprire il progetto: **console senza errori** (nuovi o toccati: `EsaminaViewViolino.cs`, `ViolinoTest.cs`, `EsaminaView.cs`, `EsaminaViewStracci.cs`, `DataClasses.cs`,
   `SceneBuilderViolino.cs`, `SceneBuilder.cs`, `SceneBuilderQuartiere.cs`, `SceneBuilderAmbienti.cs`, `LookBuilder.cs`, `MappaBuilder.cs`, le verifiche, `SegretiTools.cs`,
   `BenchmarkRunner.cs`). I `.meta` nuovi hanno GUID generati al PC della scuola.
3. **Treno > Segreti > Genera texture dei pezzi** (`pagella`, `cartolinamodena`).
4. **Treno > Scene > Costruisci ricordo Violino (R015)**: guardare corte, interni, tetti innevati, neve che cade (shader delle particelle URP), postazioni degli enigmi
   (forno e banco contro i muri, carta d'Italia sulla parete ovest della scuola).
5. Basso: controllare il cassettino sulla credenza della cucina (ritoccare `ingresso` in `aggiorna_dati_violino.py`).
6. **Treno > Mappa > Genera piante**; **Treno > Segreti > Verifica punti** (pagella, quaderno e cartolina sono "fissi" su banco, davanzale e tavolo: quote da controllare).
7. Verifiche: Compenetrazioni, Superfici e buchi, Camera (interni), Raggiungibilità (soglie delle tre porte).
8. Un solo **bake** della scena.
9. Prove: `-violinotest` (atteso 0 FAIL), poi `-cappottotest`, `-segretitest`; a mano i cinque enigmi **ascoltando** (volume, battimenti, onde sulla lavagnetta, lancetta)
   e **senza audio** (solo con gli occhi), anche col gamepad.
10. FPS Alta/Bassa (neve), screenshot e GIF in `Docs/richieste/media/R015/`.
11. Personaggi 3D (Derna, Alcide, Rosa, i tre ragazzi come varianti di una base) e Amerigo con cappotto e scarpe; ritratti.
12. Aggiornare questo Rapporto, i criteri e la riga nel README.

### Criteri di accettazione
- [x] Schema approvato prima dello sviluppo (2026-11-07).
- [ ] Capitolo completo e giocabile, salvataggio compreso, con meccanismi nuovi (scritto e coperto dalla prova automatica, **non ancora eseguito**).
- [ ] Accessibilità dell'enigma musicale: alternativa visiva per ogni suono (fatta nel codice, da provare).
- [ ] Coerenza con TRAMA.md, testi originali, ID battute, console pulita, FPS per preset (testi e ID fatti; console e FPS da verificare).
- [ ] Media in `Docs/richieste/media/R015/` (per ora solo il rapporto dei punti segreti).
