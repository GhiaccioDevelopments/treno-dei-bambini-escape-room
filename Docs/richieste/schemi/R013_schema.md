# R013 – Ricordo "Il volantino del treno": la sede del Comitato e le voci del vicolo · SCHEMA APPROVATO (2026-11-07)

Base: regole comuni R012 §2; TRAMA Decisioni 4 (solo "Maddalena", finzione), 5 (persone reali mai con battute),
10 (dicerie nominate e smontate, senza immagini); §3, §6, §7.2, §8. Emozione: paura e speranza.
Tema: dicerie e fonti, informazione (art. 21), associazioni (artt. 18, 49), UDI, voto alle donne.
**Meccanismi nuovi:** ascolto nello spazio, bacheca a fili (deduzione a gruppi), ciclostile, timbro datario.
PCI, UDI e Chiesa raccontati come attori storici, senza giudizio (§8).

## Luoghi e ambienti
- **Vicolo delle voci**: variante sera/pioggia del set del ricordo "Le scarpe" (D1 approvata): balconi,
  lavatoio, porte socchiuse da cui escono voci.
- **Sede del Comitato** (`sede_partito`, scena nuova `50_FB_Volantino`): due stanze al pianterreno con tavoli,
  sedie spaiate, ciclostile, bacheca, registri delle iscrizioni, armadio delle visite, manifesti nostri
  (`testi_ambiente` "Comitato"), stufa. Nome nei testi: "sede del Comitato" (coerente con `luoghi.json`).

## Ingresso
Oggetto nel presente: un **volantino ingiallito** piegato in quattro nella credenza → `cutscene_volantino`.

## Catena di enigmi
**V1 – Le voci del vicolo** · finestre, balconi, lavatoio · *ascolto nello spazio (nuovo)*
- Consegna: avvicinarsi e ascoltare (sottotitoli sempre attivi) per raccogliere 4 dicerie, che vanno nel quaderno:
  "li mandano in Russia", "non tornano più", "gli fanno del male", "li mettono nei forni" (solo nominate, mai mostrate).
- Trama: dicerie dell'epoca attestate (§6; "non tornano più" è la paura che le riassume [GIOCO]); la Pachiochia figura della paura, non una cattiva (§4).
- Indizi: 1) Le voci vengono dall'alto e dall'acqua. 2) Balconi e lavatoio. 3) Elenco delle 4 posizioni.
- Produce: 4 schede-diceria. Civico: come nasce e si diffonde una voce.

**V2 – La bacheca delle prove** · bacheca di sughero della sede, spago e puntine · *deduzione a gruppi (nuovo)*
- Consegna: collegare con lo spago ogni diceria al documento che la smentisce, poi dire vero/falso. Documenti nostri:
  registro con la data di ritorno prevista, cartolina di un bambino già ospitato al Nord (finzione), avviso delle visite
  mediche, lettera di una famiglia ospitante. Conferma a coppie di collegamenti giusti (alla Obra Dinn).
- Trama: Maddalena "venite a vedere" (§8, esempio nostro); `cs_scarpe_015`. Civico: verificare le fonti.
- Indizi: 1) Ogni voce ha una paura; cerca il foglio che risponde a quella paura. 2) Guarda le date sul registro.
  3) Collegamenti completi.
- Produce: le parole giuste per il volantino.

**V3 – Il ciclostile** · ciclostile a manovella · *meccanismo fisico a fasi (nuovo)*
- Fasi: scegliere 3 frasi per la matrice tra 6 (giudicate per chiarezza e verificabilità, mai per la parte politica) → stendere la
  matrice sul tamburo → inchiostrare → girare la manovella con ritmo regolare (troppo veloce sbava, si riprova).
- Indizi: 1) Un volantino deve far capire, non spaventare. 2) Scegli le frasi che si possono controllare. 3) Soluzione e ordine delle fasi.
- Civico: libertà di stampa e responsabilità di chi informa (art. 21, interfaccia). Produce: pacco di volantini (senza data).

**V4 – Chi fa che cosa** · tavolo dell'accoglienza con oggetti dei ruoli · *abbina con oggetti*
- Fascia della Croce Rossa → vestiti; stetoscopio → visita medica; registro → iscrizione; lettera dal Nord → famiglie
  ospitanti; berretto da ferroviere → viaggio. Persone di finzione; schede con UDI, Croce Rossa, Comitato (§6).
  Teresa Noce citata solo nella scheda, con date verificate (Decisione 5).
- Indizi: 1) Ogni oggetto è un mestiere. 2) Chi veste, chi cura, chi accoglie. 3) Soluzione.
- Produce: elenco "chi fa che cosa" da stampare sul retro.

**V5 – Il timbro della data** · timbro datario a nastri girevoli · *lucchetto a date (nuovo)*
- Consegna: per uscire, timbrare i volantini; il datario va girato su 4 date di una piccola linea del tempo e infine sul
  2 giugno 1946. Cronologia (solo date verificate §3): 16/12/1945 primo convoglio · gen 1946 V Congresso PCI ·
  2-3/6/1946 referendum e primo voto nazionale delle donne. Data del volantino: 2 giugno, giorno in cui "anche le madri
  hanno votato" [GIOCO, il volantino è d'autunno ma lo ricorda].
- Indizi: 1) Prima i treni del Nord, poi il voto. 2) Il voto delle donne in tutta Italia è d'estate. 3) 02-06-1946.
- Civico: voto alle donne (artt. 3, 48, interfaccia). Produce: **il volantino del treno**.

## Cutscene (scaletta, `cs_volantino_NNN`, battute originali e brevi)
- Ingresso: Amerigo adulto legge il volantino; dissolvenza: Amerigo bambino accompagna Antonietta in sede, lei non
  sa leggere e lui fa finta di saper leggere tutto.
- Chiusura: Maddalena consegna il volantino ad Antonietta; la Pachiochia dalla porta ripete una diceria, una madre le
  risponde con quello che ha visto in sede; Antonietta firma l'iscrizione con una croce.
- Oggetto: **il volantino** → si apre `stazione` (capitolo Cappotto).

## Segreti (R011)
Enigma segreto: nel registro, una pagina con le impronte di inchiostro delle dita dei bambini; contarle apre il
cassetto della scrivania (dentro: una spilla dell'UDI, scheda storica). Collezionabili: matrice rovinata, timbro
vecchio, manifesto delle amministrative, cartolina dal Nord, matita copiativa.

## Personaggi e asset nuovi
Maddalena (in `personaggi.json`, serve il modello 3D) · la Pachiochia (anziana, scialle nero) · 2 madri (varianti) ·
1 volontaria · ciclostile e timbro datario (da modellare in Blender se mancano CC0) · bacheca con spago dinamico ·
registri, manifesti e documenti (texture da `testi_ambiente`).

## Stima: **L** (scena nuova, 2–4 personaggi, 4 meccanismi nuovi; il vicolo riusato fa risparmiare).

## Rischi
- Equilibrio politico: neutralità e fonti (§8). Dicerie cupe: solo nominate (Decisione 10).
- Sovrapposizione con R020-F4 (calendario 1943-1948) e R020-B2 (2 giugno): qui linea del tempo corta e meccanismo diverso.

## Decisioni approvate (2026-11-07)
- **D1 – Vicolo delle voci:** variante sera/pioggia del set del ricordo "Le scarpe".
- **D2 – Frasi del volantino:** si giudicano chiarezza e verificabilità, mai la parte politica.
