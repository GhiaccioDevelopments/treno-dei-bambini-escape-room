# R027 – Trofei (achievement)

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `main` aggiornato. **Il codice attuale vince** |
| **Dipende da** | **ricordi finiti** (R012–R016), easter egg (R011), poi R026 |
| **Priorità** | Media (in coda) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Metti una sezione ACHIEVEMENT (trofei), dove si possono guadagnare giocando, facendo missioni secondarie ecc.
> (una volta finiti i ricordi, valutando quali mettere).

## 2. Specifica
1. **Sezione Trofei** dal menu principale e dalla Pausa, in stile d'epoca: bacheca o album di medaglie, santini o figurine
   (scegliere e motivare, coerente con R023). Trofei bloccati visibili come sagome con un indizio; quelli segreti nascosti fino allo sblocco.
2. **Notifica** discreta allo sblocco (HUD minimale) con un suono breve.
3. **Elenco dei trofei** (20–35) da proporre all'utente **prima** di implementare, con domanda a pannello. Famiglie suggerite:
   - Storia: uno per ogni ricordo completato e per il finale.
   - Enigmi: un enigma senza indizi, un capitolo intero senza errori, tutte le schede civiche lette.
   - Esplorazione: tutti i vicoli visitati (R019), tutti gli easter egg (R011), tutti i manifesti e le insegne letti.
   - Missioni secondarie: se non esistono, proporne 3–6 brevi e coerenti con `TRAMA.md` (es. aiutare un vicino, ritrovare un
     oggetto perso), con testi originali.
   - Civici: legati ai temi (Costituzione, diritti, solidarietà), mai punitivi.
4. **Dati**: `Resources/Data/trofei.json` (`id, titolo, descrizione, indizio, segreto, icona, condizione`), condizioni calcolate dai
   gettoni di MondoStato e dal salvataggio; stato in `SaveData` (PlayerPrefs `treno_save`), compatibile coi salvataggi vecchi.
   Documentare lo schema in `CLAUDE.md`.
5. Prova automatica: ogni trofeo è ottenibile (nessuna condizione impossibile) e lo sblocco sopravvive a salva/carica.

## 3. Criteri di accettazione
- [ ] Elenco dei trofei approvato dall'utente.
- [ ] Sezione Trofei da menu e pausa, notifica, salvataggio; Windows e WebGL.
- [ ] Self-test PASS, console pulita, screenshot in `Docs/richieste/media/R027/`.

## Rapporto

**Stato: codice e dati completi, da provare in Unity (2026-11-13).** Scritto al PC della scuola senza Unity. Base: `richiesta/R027-trofei` con dentro R026 (e quindi R012-R016).

### Fatto
- **Album di figurine** (`Scripts/UI/TrofeiPanel.cs`), dal menu principale (voce "Trofei" prima di "Crediti") e dalla Pausa. Scelta di stile: figurine di carta in un
  album, coerenti con la latta e il quaderno di R023. Sbloccata = figurina colorata con titolo e descrizione; bloccata = sagoma grigia con l'indizio; segreta = sagoma senza indizio.
- **Motore** (`Scripts/Trofei/Trofei.cs`, namespace `TrenoBambini.Album`): condizioni a token (`fb:`, `ris:`, `gettone:`, `inv:`, `flag:`, `segreto:`, `segreti:N`,
  `cutscene:`, `filmato`, `ev:`, `no:`, `conta:prefisso:N`, uniti da ` + `), controllo ogni secondo (`TrofeiMonitor`), **notifica** discreta (toast + campanella).
  Eventi registrati: enigma risolto senza indizi, errore per scena, scheda civica letta, scena visitata (mai quando si rivive un ricordo).
- **Dati**: `Resources/Data/trofei.json` generato da `ArtSource/Trofei/genera_trofei.py` (i numeri delle condizioni sono contati dai JSON, cosi' nessuna e'
  impossibile); stato in `SaveData.trofei` e `SaveData.trofeiEventi` (salvataggi vecchi: liste vuote).
- Prove: `EnigmiSelfTest` controlla 20-35 trofei con condizioni valide; `FinaleTest` controlla lo sblocco del finale e che sia nel salvataggio.

### Elenco dei trofei (30)
Le regole del progetto dicono di non fare domande: l'elenco è qui, si corregge in `genera_trofei.py`.

| Famiglia | Trofeo | Come si ottiene |
|---|---|---|
| Storia | Il ritorno | Hai visto Amerigo tornare nel vicolo. |
| Storia | Le scarpe | Hai rivissuto il ricordo delle scarpe. |
| Storia | Stracci | Hai lavorato con Capa 'e fierro. |
| Storia | Il volantino | Hai battuto le dicerie con le prove. |
| Storia | Il cappotto | Sei partito con il numero 112. |
| Storia | Il violino | A Modena hai scoperto di avere orecchio. |
| Storia | Il violino ritrovato | Hai finito la storia di Amerigo. |
| Enigmi | Al primo colpo | Hai risolto un enigma senza chiedere indizi. |
| Enigmi | Testa fina | Dieci enigmi risolti senza indizi. |
| Enigmi | Passo sicuro | Il ricordo delle scarpe senza un errore. |
| Enigmi | Orecchio fino | Il ricordo di Modena senza un errore. |
| Enigmi | Educazione civica | Hai letto 14 schede civiche. |
| Enigmi | Il quaderno completo | Hai letto tutte le schede civiche dei ricordi. |
| Esplorazione | Da Napoli a Modena | Sei stato in tutti i luoghi del gioco. |
| Esplorazione | Occhio lungo | Hai trovato il primo segreto. |
| Esplorazione | Mezzo vicolo | Hai trovato metà dei segreti. |
| Esplorazione | Ogni angolo (segreto) | Hai trovato tutti i segreti del gioco. |
| Esplorazione | La scatola dei ragazzi (segreto) | Hai consegnato a Carmine la scatola dei ragazzi del vicolo. |
| Missioni secondarie | La filastrocca | Hai aperto la portella del sottoscala. |
| Missioni secondarie | Dietro la credenza | Hai trovato il posto dove dormiva Amerigo. |
| Missioni secondarie | Il biglietto del robivecchi | Hai trovato il biglietto di Capa 'e fierro. |
| Missioni secondarie | Le impronte d'inchiostro | Hai aperto il cassetto del leggio. |
| Missioni secondarie | La valigia dimenticata | Hai aperto la valigia sulla reticella. |
| Missioni secondarie | La puntina di Napoli | Hai rimesso a posto la carta d'Italia della scuola. |
| Missioni secondarie | 92,7 | Hai sintonizzato la radio di Antonietta. |
| Civici | Il primo soldo | Lavoro minorile ieri, diritti oggi: hai fatto il conto giusto con Capa 'e fierro. |
| Civici | Contro le dicerie | A una diceria si risponde con una prova (art. 21). |
| Civici | Un nome, non un numero | Ogni bambino ha diritto alla propria identità. |
| Civici | La scuola è di tutti | Art. 34: la scuola è aperta a tutti. |
| Civici | Una famiglia | Art. 30 e legge 184 del 1983: il diritto a una famiglia. |

### Scostamenti e decisioni prese da Claude
- **Missioni secondarie = gli enigmi segreti di R011** gia' nel gioco (portella, ripostiglio, carretto, cassetto dell'UDI, valigia, carta d'Italia, radio): un trofeo per
  ciascuno. Missioni nuove con personaggi da aiutare richiederebbero NPC e scene: da proporre in una richiesta a parte.
- "Tutti i manifesti letti" e "tutti i vicoli visitati" non messi: non c'e' un evento per la lettura dei manifesti; al posto dei vicoli, "tutti i luoghi" (9 scene).
- Nessun trofeo di velocita' o punitivo. I trofei "senza errori" valgono anche giocando piu' volte (contano solo gli errori nella scena).

### Da fare a casa con Unity
1. Console senza errori (nuovi `Trofei.cs`, `TrofeiPanel.cs`; toccati `EsaminaView.cs`, `GameData.cs`, `SaveSystem.cs`, `DataClasses.cs`, `TitleScreen.cs`, `PausePanel.cs`, self-test).
2. Album dal menu e dalla Pausa (la Pausa ha una voce in piu': controllare che stia nella pagina), notifica, Esc e gamepad; screenshot in `Docs/richieste/media/R027/`.
3. `-enigmitest`, `-finaletest`; una partita per vedere sbloccarsi i trofei.

### Criteri di accettazione
- [ ] Elenco dei trofei approvato dall'utente (mostrato qui e in chat).
- [ ] Sezione Trofei da menu e pausa, notifica, salvataggio; Windows e WebGL (scritto, da provare).
- [ ] Self-test PASS, console pulita, screenshot in `Docs/richieste/media/R027/`.
