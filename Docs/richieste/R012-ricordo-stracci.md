# R012 – Nuovo ricordo "Stracci": il vicolo degli stracci e la bottega di Capa 'e fierro

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-06 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 605f952. **Il codice attuale vince** |
| **Dipende da** | R006, R007, R009 (e R011 per i segreti). Prima delle stanze successive (R013–R016) |
| **Ok dell'utente** | **Sì (2026-11-06)**: "Fai tutte le altre stanze, con enigmi sempre più diversi e belli, deve spaccare il gioco" |

## 1. Cosa ha chiesto l'utente (parole sue)
> FAI TUTTE LE ALTRE STANZE, CON ENIGMI SEMPRE PIÙ DIVERSI E BELLI, DEVE SPACCARE IL GIOCO

Questa è la prima di cinque richieste-capitolo (R012 Stracci, R013 Volantino, R014 Cappotto, R015 Violino, R016 Finale),
nell'ordine della storia. **Regole comuni a tutte e cinque** (scritte qui e richiamate dalle altre):

## 2. Regole comuni ai capitoli R012–R016
- **`Docs/TRAMA.md` vincola tutto** (§7.2 per luogo, emozione, tema e idee di enigmi; §8 guida ai dialoghi; §9 punti
  incerti: niente fatti "da verificare" presentati come certi). Contraddizioni → segnalarle all'utente.
- Struttura di ogni capitolo, come "Le scarpe": oggetto trovato nel basso/hub → cutscene d'ingresso → stanza/flashback
  esplorabile → catena di enigmi → cutscene di chiusura → oggetto in inventario e luogo successivo "aperto".
- **Enigmi sempre più diversi e belli**: ogni capitolo introduce **almeno un meccanismo mai visto prima** nel gioco e
  non ripete la stessa combinazione di tipi. Difficoltà che sale piano piano. Sistema di R009 (oggetti del mondo, vista
  ravvicinata, catene `richiede/produce`, inventario attivo), indizi a 3 livelli, didattica non punitiva, fallback
  "Enigmi semplificati".
- Ogni stanza: grande e dettagliata (standard R006), grafica R007, segreti R011, nessuna compenetrazione (verifica R006).
- Contenuti nei JSON (`oggetti.json`, `luoghi.json`, `flashback_<id>.json`, `cutscene_<id>.json`, `audio.json`),
  battute con ID `cs_<oggetto>_NNN` per il doppiaggio, dialoghi originali, nessun testo nel codice.
- Personaggi nuovi: basi MakeHuman/MPFB in Blender come da CLAUDE.md, screenshot all'utente prima di considerarli finiti,
  sembianze originali (non degli attori del film).
- **Prima di sviluppare ogni capitolo**: presentare all'utente (pannello domande) lo schema della catena di enigmi e la
  bozza della mappa, e aspettare l'ok.
- Scene nuove generate da script (menu **Treno ▸ …**), aggiunte alle Build Settings, bake luci, console pulita, FPS misurati.

## 3. Questo capitolo: "Stracci"
- **Luoghi**: `vicolo_stracci` (vicolo dove Amerigo raccoglie stracci tra macerie e cortili) e `casa_capa_e_fierro`
  (bottega del robivecchi). Capa 'e fierro: brontolone ma non cattivo (TRAMA, Decisione 3).
- **Emozione / tema**: ingegno e imbarazzo; lavoro minorile ed economia di sopravvivenza.
- **Idee di enigmi** (da TRAMA §7.2, da rendere nuovi e "fisici"): raccolta degli stracci giusti nel vicolo (esplorazione a
  tempo libero, non a cronometro); **bilancia a piatti** della bottega con pesi e conti leggeri sul prezzo (meccanismo nuovo);
  ordinare le fasi del baratto; abbinare "mestiere di bambino del 1946 – diritto di oggi" (il collegamento agli articoli lo fa
  l'interfaccia didattica, non i personaggi); attrezzi del robivecchi nascosti.
- Oggetto ottenuto: **gli stracci** (o il soldo guadagnato) → apre il capitolo "Volantino".

## 4. Criteri di accettazione
- [ ] Schema approvato dall'utente prima dello sviluppo.
- [ ] Capitolo giocabile dall'inizio alla fine, salvataggio compreso, con almeno un meccanismo nuovo.
- [ ] Coerenza con TRAMA.md, testi originali, ID battute, console pulita, FPS per preset.
- [ ] Video/GIF in `Docs/richieste/media/R012/`.

## Rapporto
_(da compilare a fine lavoro)_
