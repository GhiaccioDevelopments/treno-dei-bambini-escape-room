# R015 – Ricordo "Il violino": Modena, casa Benvenuti · SCHEMA APPROVATO (2026-11-07)

Base: regole comuni R012 §2; TRAMA Decisione 2 (versione del libro, nei testi solo "la famiglia Benvenuti"),
§2 sezione 2, §4 (Derna, Rosa con il forno, Alcide accordatore, Rivo/Luzio/Nario, uno geloso), §8, §9 n. 1-2
(Derna vedova o partigiana: non dirlo). Emozione: meraviglia e orgoglio. Tema: istruzione (art. 34), cultura (art. 9),
pregiudizio Nord-Sud. Musica: melodia **originale** (non *Bella ciao*, non Piovani), tema del violino R002.
**Meccanismi nuovi:** prova fisica del forno, intonazione con i battimenti, tracce nella neve, melodia da ripetere.

## Luoghi e ambienti (inverno emiliano, nebbia o neve, interni caldi; look diverso da Napoli)
Scena nuova `70_FB_Modena`, **un solo set** (D1 approvata): una corte di pianura con
- **casa di Derna** (cucina con stufa, la stanzetta di Amerigo: letto vero, cuscino, finestra sulla neve);
- **casa-laboratorio dei Benvenuti**: pianoforte verticale aperto, banco degli attrezzi d'accordatore, il **forno** di Rosa;
- **scuola** (un'aula sola: banchi, lavagna, stufa, carta d'Italia) in fondo alla strada innevata.

## Ingresso
Oggetto nel presente: uno **spartito scritto a mano**, con segni colorati al posto delle note, nascosto nel cassetto → `cutscene_violino`.

## Catena di enigmi (difficoltà che sale)
**M1 – La prova della farina** · forno a legna di Rosa · *meccanismo fisico e tempo (nuovo)*
- Consegna: aiutare Rosa a infornare. Si gettano pizzichi di farina sulla pietra: se brucia subito è troppo caldo,
  se resta bianca è freddo; si regola con legna e serranda, poi si inforna e si sforna al momento giusto (clessidra).
- Trama: Rosa ha un forno (§4); accoglienza con il cibo (§8: "battute su pane e musica").
- Indizi: 1) Il forno si "legge" con la farina. 2) Deve diventare nocciola in un respiro. 3) Sequenza giusta.
- Civico: accoglienza e lavoro delle famiglie che ospitavano (§6). Produce: pane per Alcide (porta del laboratorio).

**M2 – Gli attrezzi che suonano** · banco dell'accordatore · *abbina suono-oggetto*
- Diapason, chiave d'accordatura, feltri, martelletto, corda: si ascolta un suono e si trova l'attrezzo che lo fa.
  Alternativa visiva sempre presente (onda o vibrazione disegnata). Indizi a 3 livelli. Produce: il diapason.

**M3 – Il pianoforte stonato** · pianoforte verticale · *intonazione con i battimenti (nuovo)*
- Consegna: accordare tre corde sul diapason girando la chiave: finché sono stonate si sente un "uà-uà" (battimento)
  e una lancella del gioco oscilla; quando il battimento sparisce la corda è giusta. Accessibile senza udito.
- Trama: Alcide accordatore che scopre l'orecchio di Amerigo (§1); "prima di suonare ascolta" (§8, esempio nostro).
- Civico: cultura e talento come diritto (art. 9). Indizi: 1) Ascolta l'onda, non la nota. 2) Più giri, più lento il battimento: continua finché sparisce. 3) Posizione giusta della chiave.
- Produce: Alcide promette ad Amerigo un violino.

**M4 – Le impronte nella neve** · cortile e strada innevata fino alla scuola · *tracce (nuovo)*
- Consegna: il violino è sparito: lo ha nascosto per gelosia uno dei tre ragazzi. Amerigo, che conosce le scarpe
  di tutti, legge le impronte (misura, chiodi, tacco rattoppato) per capire chi è stato e dove le tracce portano.
- Trama: uno dei ragazzi è geloso all'inizio (§4); richiamo al gioco delle scarpe (Decisione 8) e alle scarpe nuove.
- Civico: pregiudizio e appartenenza; nessuno è "di troppo". Indizi: 1) Tre paia di scarponi, tre misure. 2) Uno ha
  il tacco rattoppato. 3) Le impronte portano alla legnaia della scuola.
- Produce: il violino ritrovato nella scena, che resta a casa Benvenuti e non entra nell'inventario, e un chiarimento a parole (scaletta sotto, nessuna predica).

**M5 – La prima melodia** · violino e spartito disegnato da Alcide · *ascolta e ripeti (nuovo)*
- Alcide suona una frase di 4, poi 6 note (melodia nostra); Amerigo la ripete. Alternativa visiva: segni colorati
  sulla tastiera e sullo spartito. Errore = Alcide la rifà più lenta. A scuola il ragazzo geloso batte il tempo.
- Civico: diritto allo studio (art. 34). Produce: **lo spartito** di Alcide (oggetto del capitolo).

## Cutscene (scaletta, `cs_violino_NNN`)
- Ingresso: Amerigo adulto riconosce i segni colorati dello spartito; dissolvenza: arrivo nella neve, Derna lo accoglie con la stufa accesa.
- Dopo M4: il ragazzo restituisce il violino, i due si guardano le scarpe, ridono.
- Chiusura: festa piccola in casa Benvenuti, Amerigo suona; la lettera da scrivere ad Antonietta (scrive Derna, lui detta).

## Segreti (R011)
Enigma segreto: nella carta d'Italia della scuola le puntine dei bambini ospiti formano un treno; spostare quella di
Napoli apre lo sportello della stufa spenta (dentro, un disegno di Amerigo). Collezionabili: pagella, quaderno a
righe, pupazzo di neve, corda di violino, cartolina di Modena.

## Personaggi e asset nuovi
Derna, Alcide, Rosa, Rivo, Luzio, Nario (abiti di Modena 1946, sembianze originali) · Amerigo con cappotto e scarpe ·
pianoforte verticale, violino e archetto, attrezzi d'accordatore, forno a legna, banchi di scuola, neve e nebbia.
Audio: corde stonate/accordate, frasi del violino (sintesi o registrazione nostra).

## Stima: **L** (il più grande: 6 personaggi, set nuovo, audio interattivo e accessibilità).

## Rischi
- Sei personaggi nuovi: tempo di modellazione e peso LFS. Ridurre: i tre ragazzi come varianti di una base.
- Chi dei tre è geloso: le fonti non lo dicono (§4); il film usa "Lucio". Nel gioco scegliere un ragazzo e segnarlo [GIOCO].

## Decisioni approvate (2026-11-07)
- **D1 – Set:** una sola scena (corte con casa di Derna, casa Benvenuti e scuola vicine).
- **D2 – Oggetto del capitolo:** lo **spartito** di Alcide (conservato da Antonietta, che non sapeva leggerlo). Il violino
  non è un oggetto del capitolo: si ritrova solo nel finale (R016, N4).
