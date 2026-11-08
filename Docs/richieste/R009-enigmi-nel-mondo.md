# R009 – Enigmi dentro il mondo: niente più oggetti viola, interazioni da grande gioco di enigmi

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ fdeaedc. **Il codice attuale vince** |
| **Dipende da** | R006 (mappe). R010 usa il sistema costruito qui |
| **Priorità** | Alta |

## 1. Cosa ha chiesto l'utente (parole sue)
> Gli enigmi strutturali in modo molto diverso, non ci possono essere oggetti viola in mezzo così.
> Prendi spunto dai più grandi giochi ad indovinelli.

Obiettivo: gli enigmi non sono più "stazioni" colorate piantate nella stanza che aprono un pannello. Nascono dagli
**oggetti dell'ambiente** (un baule, una cassetta, un calendario appeso, una porta col lucchetto), da esaminare e
manipolare in 3D, come nei grandi giochi di enigmi.

## 2. Situazione di partenza
- `EscapeRoomController` crea per ogni enigma un marcatore colorato (`ColorFor(tipo)`) alle coordinate del JSON; `Glow` lo
  fa pulsare; l'interazione apre un pannello 2D (`QuizPanel`, `LucchettoPanel`, `OrdinaPanel`, `AbbinaPanel`, `NascostoPuzzle`).
- **Oggetti viola**: per prima cosa capire con screenshot che cosa sono (marcatori degli enigmi? materiali mancanti che
  diventano magenta? emissione del `Glow`?) e scriverlo nel Rapporto. In ogni caso devono sparire.

## 3. Riferimenti (prendere spunto dai meccanismi, non copiare contenuti)
- **The Room**: oggetti da esaminare da vicino, ruotare, cassetti segreti, meccanismi a più strati.
- **Myst / Riven**: indizi nell'ambiente e nei documenti, la soluzione si deduce osservando il mondo.
- **The Witness**: l'ambiente insegna la regola senza parole.
- **Return of the Obra Dinn**: deduzione da indizi sparsi, conferma a gruppi.
- **Professor Layton**: enigmi brevi e leggibili, indizi a più livelli.

## 4. Specifica del sistema
- **Niente marcatori artificiali.** Gli enigmi sono oggetti d'epoca veri (modello 3D) piazzati in modo credibile.
  Si riconoscono dalla luce di scena che li valorizza e da un prompt discreto "E – Esamina" quando ci si avvicina.
  Contorno/brillio leggero **solo** con l'impostazione "Evidenzia oggetti interattivi" (spenta di default), oppure col
  pulsante Indizio, oppure dopo molto tempo senza progressi.
- **Vista ravvicinata (ispezione)**: interagendo, la camera passa (Cinemachine) a una vista dell'oggetto; si può ruotarlo,
  zoomare, cliccare le parti (cassetti, coperchi, rotelle, fogli), girare un foglio per leggerne il retro.
  Mouse, touch e tastiera. Esc o tasto destro per uscire.
- **Meccanismi fisici al posto dei pannelli 2D**: lucchetto a rotelle 3D, cassetto con doppio fondo, foglietti da disporre
  su un tavolo, spago da tendere tra due chiodi… I pannelli 2D restano come **fallback** (impostazione "Enigmi semplificati",
  utile alla LIM e per l'accessibilità).
- **Indizi nell'ambiente e nel quaderno**: scritte, oggetti, documenti trovati finiscono nel quaderno/inventario e servono
  ad altri enigmi (catene tra enigmi, non enigmi isolati).
- **Inventario attivo**: usare un oggetto su un altro o nel mondo (chiave nel lucchetto, scarpa sulla forma del calzolaio).
- **Didattica non punitiva invariata**: errore = feedback gentile + spiegazione + riprova; pulsante Indizio sempre presente,
  con indizi a 3 livelli (spinta → direzione → soluzione).
- **Dati nei JSON**: estendere lo schema `Enigma` (es. `modello`, `vista`, `meccanismo`, `richiede` = oggetti/enigmi
  necessari, `produce` = oggetti/indizi ottenuti) senza rompere i tipi esistenti; aggiornare lo schema in CLAUDE.md.

## 5. Ordine di lavoro consigliato
1. Trovare gli oggetti viola, screenshot, toglierli. 2. Interazione "Esamina" + vista ravvicinata generica.
3. Un meccanismo 3D d'esempio (lucchetto a rotelle) sull'enigma esistente. 4. Catene `richiede/produce` e inventario attivo.
5. Impostazioni "Evidenzia oggetti interattivi" e "Enigmi semplificati".

## 6. Criteri di accettazione
- [ ] Nessun oggetto viola né marcatore colorato in nessuna scena (screenshot).
- [ ] Almeno un enigma esistente giocabile con vista ravvicinata e meccanismo 3D, con mouse, tastiera e touch.
- [ ] Schema JSON esteso e documentato; vecchi tipi ancora funzionanti; console pulita.
- [ ] GIF di un enigma in `Docs/richieste/media/R009/`.

## Rapporto
_(da compilare a fine lavoro)_
