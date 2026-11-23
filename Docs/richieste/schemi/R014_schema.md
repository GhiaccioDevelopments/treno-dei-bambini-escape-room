# R014 – Ricordo "Il cappotto": la stazione e il treno · SCHEMA APPROVATO (2026-11-07)

Base: regole comuni R012 §2; TRAMA Decisioni 9 (mela annurca solo dettaglio), 12 (primo paio di scarpe col viaggio,
senza dire chi lo dà: §9 n. 14); §6 (bambini lavati, vaccinati, schedati, vestiti; vestiti in gran parte dalla Croce Rossa).
Emozione: sorpresa e nostalgia. Tema: solidarietà (art. 2), accoglienza.
**Meccanismi nuovi:** deduzione su figure vive in una folla, enigma in movimento dal finestrino, dialoghi a scelta.

## Luoghi e ambienti
- **Stazione** (`stazione`, scena nuova `60_FB_Stazione`): atrio e pensilina di una grande stazione del 1946 ispirata
  a quella di Napoli dell'epoca, senza nome di edifici veri (D1 approvata); tavoli del Comitato e della Croce Rossa, montagne
  di cappotti, panche, valigie di cartone, locomotiva a vapore e vagoni di terza classe, fumo, folla di madri e bambini.
- **Vagone** (scena `61_FB_Vagone`): carrozza di terza con panche di legno, reticelle, finestrini; fuori il paesaggio
  che scorre (fondali a strati e cartelli delle stazioni), luce che cambia dal giorno alla notte. Spazio stretto ma pieno.

## Ingresso
Oggetto nel presente: un **cappottino di lana** da bambino, troppo piccolo, appeso dietro la porta → `cutscene_cappotto`.

## Catena di enigmi
**C1 – I cartellini scambiati** · registro della volontaria, bambini in fila · *deduzione su figure vive (nuovo)*
- Consegna: il vento ha fatto volare i cartellini col nome di tre bambini (uno è Amerigo, gli altri Mariuccia e un
  piccolo sconosciuto). Dal registro (età, quartiere, segni particolari) e da ciò che i bambini tengono in mano capire
  di chi è ogni cartellino e appenderlo al collo giusto.
- Trama: bambini schedati alla partenza (§6); Mariuccia parte con Amerigo (§4). Civico: identità e diritti di ogni bambino.
- Indizi: 1) Il registro dice più del nome. 2) Guarda età e cosa portano. 3) Abbinamenti.
- Produce: il numero di Amerigo sul cartellino (servirà in C2).

**C2 – I cappotti della Croce Rossa** · mucchio di cappotti con talloncini · *abbina per taglia e dettagli*
- Consegna: dare a ogni bambino della fila il cappotto giusto (taglia sul talloncino, maniche, bottoni, toppe); quello di
  Amerigo ha il suo numero. Le scarpe arrivano insieme, in una scatola, senza dire da chi (Decisione 12).
- Civico: solidarietà organizzata (art. 2). Indizi: 1) Misura a occhio: spalle e maniche. 2) Il talloncino ha un numero.
  3) Abbinamenti. Produce: cappotto e scarpe per Amerigo (cambio di modello: da qui Amerigo non è più scalzo).

**C3 – La mela** · Antonietta al finestrino · *gesto, nessun errore possibile*
- Momento interattivo breve: Antonietta porge la mela annurca, il giocatore la mette nella valigia (inventario attivo).
  Solo dettaglio (Decisione 9), poi il treno parte.

**C4 – L'Italia dal finestrino** · finestrino, carta ferroviaria appesa, orologio del controllore · *enigma in movimento (nuovo)*
- Consegna: mentre il treno corre passano i cartelli delle stazioni; appuntare sulla carta, in ordine, le città
  attraversate e capire quando si arriva. Se ne perdi una, il controllore la ripete (nessuna penalità).
- Soluzione: Napoli → Roma → Firenze → Bologna → Modena (percorso semplificato: la linea esatta del 1946 è da verificare).
- Trama: viaggio verso il Nord (§3). Civico: un'Italia sola, pregiudizio Nord-Sud.
- Indizi: 1) Guarda i cartelli, non il paesaggio. 2) La carta ha già Napoli e Modena. 3) Elenco completo.
- Produce: "siamo quasi arrivati" (sblocca C5).

**C5 – I bambini che hanno paura** · bambini nello scompartimento · *dialoghi a scelta (nuovo)*
- Consegna: tre bambini ripetono le dicerie (aggancio a R013). Amerigo sceglie come rispondere: consolare, scherzare,
  mostrare il volantino (inventario attivo). Una risposta che non aiuta non è un errore: il bambino resta triste e
  l'indizio suggerisce un altro modo. Si chiude quando tutti e tre si calmano.
- Civico: solidarietà tra pari, contrastare la disinformazione. Indizi: 1) Ognuno ha una paura diversa. 2) Il
  volantino risponde a una di quelle paure. 3) Risposta giusta per ciascuno.
- Produce: **il cappotto** (oggetto del capitolo).

## Cutscene (scaletta, `cs_cappotto_NNN`)
- Ingresso: Amerigo adulto prova a infilare la mano nella manica minuscola; dissolvenza sulla stazione.
- Metà (dopo C2): Amerigo guarda le scarpe nuove ai suoi piedi (chiusura dell'arco "Le scarpe"); la vecchia della
  paura grida una diceria alla folla, nessuno la segue.
- Chiusura: arrivo di notte, banchina con famiglie che aspettano; Amerigo stringe il cappotto. Look più freddo.
- Oggetto: **il cappotto** → si apre il capitolo "Violino".

## Segreti (R011)
Enigma segreto: nella reticella del vagone una valigia con un lucchetto a combinazione = numero del vagone visto
salendo. Collezionabili: biglietto del treno, cartolina di una città lungo la linea, gessetto col numero di vagone,
bottone della Croce Rossa, fazzoletto ricamato.

## Personaggi e asset nuovi
Antonietta (se non ancora fatta in 3D), Tommasino, Mariuccia, 3–4 bambini (varianti), 2 volontarie, controllore,
la vecchia della paura · Amerigo bambino **con cappotto e scarpe** (variante del modello) · locomotiva e carrozza
di terza 1946 (CC0 se esiste, altrimenti modellazione), fondali del paesaggio, cartelli di stazione, carta ferroviaria.

## Stima: **L** (due scene nuove, molti personaggi, fondale in movimento).

## Rischi
- Folla: tanti personaggi pesano su FPS e LFS; usare impostori o figure lontane semplici.
- La neve "scambiata per ricotta" è da verificare (TRAMA §4): non usarla come fatto del libro.

## Decisioni approvate (2026-11-07)
- **D1 – Stazione:** generica, ispirata alla vecchia stazione di Napoli (piazza Garibaldi), senza ricostruzione fedele.
- **D2 – Scene:** stazione e vagone entrambi giocabili.
