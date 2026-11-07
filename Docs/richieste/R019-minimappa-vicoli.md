# R019 – Minimappa in alto a sinistra: prossimo punto da raggiungere e nomi dei vicoli

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3. **Il codice attuale vince** |
| **Dipende da** | R006 (mappe grandi, unita). Si integra con R009/R020 (enigmi) per sapere qual è il "prossimo punto" |
| **Priorità** | Media-alta (con le mappe grandi serve per orientarsi) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Metti una minimappa in alto a sinistra che ti dice dove sarà il prossimo punto da interagire, Nomi dei vicoli etc...

## 2. Vincoli da CLAUDE.md
- HUD minimale e **d'epoca**: niente radar moderno. La minimappa è una **pianta di Napoli disegnata a mano** su carta
  invecchiata (stile catasto/pianta topografica anni '40), inchiostro seppia, bordo strappato, ago di una bussola d'ottone.
- Testi (nomi dei vicoli, luoghi, obiettivi) **solo nei JSON**, mai nel codice.
- Didattica: deve aiutare, non togliere il piacere di esplorare (vedi modalità sotto).

## 3. Specifica
1. **Minimappa** in alto a sinistra, piccola (≈ 180-220 px a 1080p, scalabile), circolare o a cartiglio:
   - ruota con la camera (opzione: nord fisso), il giocatore al centro come una freccetta d'inchiostro;
   - disegno della pianta **generato dall'editor** dalla geometria della scena (vista dall'alto ortografica → texture
     ripassata con filtro "inchiostro su carta"), rigenerato da menu `Treno ▸ Mappa ▸ Genera piante`; niente camera
     extra a runtime (peso zero su WebGL);
   - **nomi dei vicoli e delle piazze** scritti lungo le strade, in corsivo d'epoca leggibile, comparendo quando si è vicini;
   - icone d'epoca piccole: porta di casa (basso), bottega, fontana, edicola votiva, uscite.
2. **Prossimo obiettivo**: un segno a inchiostro rosso (una "X" o un cerchietto a matita) sul punto da raggiungere/interagire;
   se è fuori dalla minimappa, una freccetta sul bordo. Il prossimo obiettivo viene dallo stato del gioco (luoghi aperti,
   oggetti non ancora trovati, enigma corrente). In alto, sotto la mappa, una riga breve con l'obiettivo
   (es. "Torna al basso di Antonietta"), testo da JSON.
3. **Mappa grande** con il tasto **M** (e pulsante touch): pianta intera a schermo come un foglio aperto, con legenda,
   nomi di tutti i vicoli e luoghi già visitati/sbloccati (la nebbia di guerra sui luoghi mai visti è facoltativa).
4. **Modalità di aiuto** (Impostazioni ▸ Gioco): *Guidata* (obiettivo sempre segnato) · *Normale* (segnato dopo qualche
   minuto senza progressi o premendo Indizio) · *Esploratore* (solo nomi e posizione, nessun obiettivo). Minimappa
   attivabile/disattivabile. Default: Normale. Le preferenze si salvano con le altre impostazioni.
5. **Interni e flashback**: nel basso una pianta della casa (stanze); nel flashback la strada del ricordo con il look seppia.
6. **Dati**: nuovo `mappe.json` (o estensione di `luoghi.json`): per scena, nomi di strade con polilinea/punti di ancoraggio,
   punti di interesse, testi degli obiettivi. Aggiornare lo schema JSON in CLAUDE.md.
7. Touch/LIM: leggibile, non copre il joystick virtuale né i pulsanti.

## 4. Domande aperte (per l'utente, chiedere con il pannello prima di scrivere i nomi)
- Nomi dei vicoli: **veri** dei Quartieri Spagnoli (es. via Toledo, vico Lungo Gelso, vico Speranzella… da verificare con
  fonti e mappe d'epoca) oppure **inventati** ma verosimili? Raccomandazione: veri per le strade principali, inventati per i vicoletti
  costruiti da noi, con una nota nei crediti.
- Default della modalità di aiuto: Normale va bene?

## 5. Ordine di lavoro consigliato
1. Generatore della pianta e stile carta/inchiostro (screenshot all'utente). 2. Widget minimappa + rotazione + giocatore.
3. Nomi dei vicoli da JSON. 4. Obiettivo e frecce. 5. Mappa grande (M). 6. Impostazioni e touch.

## 6. Criteri di accettazione
- [ ] Minimappa d'epoca in alto a sinistra in hub, basso e flashback, con i nomi delle strade.
- [ ] Il prossimo punto da interagire è segnato correttamente lungo tutto il capitolo "Le scarpe" (prova completa).
- [ ] Tasto M (e pulsante touch) per la mappa grande; modalità di aiuto e interruttore nelle Impostazioni.
- [ ] Testi solo in JSON; schema aggiornato in CLAUDE.md; console pulita; nessun calo di FPS (anche WebGL).

## Decisioni dell'utente (2026-11-07)
- **Nomi misti:** veri per le strade principali, inventati verosimili per i vicoletti.
- **Via Toledo nel 1946** si chiamava "Via Roma (già Toledo)": nel gioco usare questa dicitura per la strada.
- **Proposta dei nomi** in `Docs/richieste/schemi/R019_nomi_vicoli.md`.
- **Nomi inventati:** vanno verificati su OpenStreetMap prima di fissarli nel JSON.
- **Modalità di aiuto di default:** Normale.

## Rapporto
_(da compilare a fine lavoro)_
