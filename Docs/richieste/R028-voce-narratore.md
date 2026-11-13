# R028 – Pulsante altoparlante sotto i dialoghi del Narratore e lista dei file da doppiare

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `main` aggiornato. **Il codice attuale vince** |
| **Dipende da** | **tutti i dialoghi finiti**: ricordi R012–R016, filmato R025, enigmi R026, trofei R027. Penultima della coda |
| **Priorità** | In coda (fine) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Sotto ogni dialogo del Narratore aggiungi un pulsante con icona l'altoparlante. Io doppierò: dimmi poi come rinominare il file
> di ogni dialogo e lo registro con Audacity (una volta che hai finito tutti i dialoghi, quindi alla fine della coda delle PR).

## 2. Specifica
1. **Pulsante altoparlante** (icona d'epoca disegnata, non un'emoji moderna) sotto ogni battuta del **Narratore**: cutscene,
   introduzioni e conclusioni dei ricordi, schede civiche lette dal narratore, filmato iniziale (R025). Clic, E o tasto del gamepad
   → riproduce (o ripete) la voce di quella battuta; mentre suona l'icona mostra le onde; secondo clic = ferma.
   Se il file audio non c'è, il pulsante resta visibile ma grigio, con il suggerimento "voce non ancora registrata".
   Opzione in Impostazioni > Audio: "Leggi in automatico le battute del Narratore" (sì di default), oltre al pulsante.
2. **ID per tutto**: ogni battuta del Narratore, ovunque sia, ha un ID stabile (`cs_<oggetto>_NNN` come ora; per i testi fuori dalle
   cutscene proporre `nar_<luogo>_NNN`) e un campo `audio`. Dopo la consegna della lista nessun ID cambia più.
3. **Collegamento**: il menu esistente **Treno ▸ Doppiaggio ▸ Aggiorna libreria voci** riconosce anche i nuovi ID.
4. **Lista per l'utente** (consegna principale): `Doppiaggio/DA_REGISTRARE.md` più `copione.csv` aggiornato, ordinati per scena;
   per ogni riga: nome file esatto (`<id>.wav`), personaggio, testo, emozione o indicazione di lettura, durata stimata.
   Più una mini-guida Audacity: mono, 44.100 Hz, WAV 16 bit (o OGG qualità 5), normalizza a -1 dB, riduzione rumore,
   0,3 s di silenzio in testa e in coda, **File ▸ Esporta audio** col nome esatto nella cartella `Assets/Audio/Voci/`, poi il menu
   Aggiorna libreria voci. Proporre anche **File ▸ Esporta multipli** con le etichette (un'etichetta = un file col nome dell'ID),
   per registrare tutto in una sessione: fornire il file di etichette o le istruzioni. Le battute di altri personaggi doppiate
   dall'utente (filmato iniziale) vanno nella stessa lista.
5. Messaggio in chat all'utente con la lista e il numero totale di file.
6. Prova automatica: ogni battuta del Narratore ha ID unico e pulsante; un file di prova collegato suona e il testo avanza a fine clip.

## 3. Criteri di accettazione
- [ ] Pulsante altoparlante sotto ogni battuta del Narratore, con stato "manca la voce".
- [ ] `Doppiaggio/DA_REGISTRARE.md` e `copione.csv` completi e consegnati all'utente.
- [ ] Self-test PASS, console pulita, Windows e WebGL.

## Rapporto

**Stato: codice e lista completi, da provare in Unity (2026-11-13).** Scritto al PC della scuola senza Unity. Base: `richiesta/R028-voce-narratore` con dentro R027 (-> R012).

### Fatto
- **Pulsante altoparlante** (`Scripts/UI/Altoparlante.cs`): icona d'epoca disegnata da codice (tromba da grammofono a china), sotto ogni battuta del **Narratore**
  nelle cutscene (`CutscenePlayer`) e nei messaggi d'introduzione e conclusione dei ricordi (`UIRoot.ShowMessage` con il nuovo parametro `voce`). Clic, **tasto V** o
  **gamepad Nord** = riproduce o ripete; onde animate mentre suona; secondo clic = ferma. Senza file: icona grigia e "voce non ancora registrata".
- **Impostazione** "Leggi in automatico il Narratore" (sì di default, `SaveData.narratoreManuale`), nella scheda Accessibilità con le opzioni degli enigmi.
- **ID**: le battute delle cutscene restano `cs_<oggetto>_NNN`; introduzioni e conclusioni dei ricordi sono `nar_<oggetto>_001` e `nar_<oggetto>_002`
  (ricavati dal flashback, stabili). Il menu **Treno > Doppiaggio > Aggiorna libreria voci** li riconosce già (usa il nome del file).
- **Lista per l'utente**: `Doppiaggio/DA_REGISTRARE.md`, `Doppiaggio/copione.csv` (`;`, apribile con Excel) e i file di etichette di Audacity
  `Doppiaggio/etichette/00_intro.txt ... 06_finale.txt`, generati da `ArtSource/Doppiaggio/genera_da_registrare.py`. **83 file**, circa 15 minuti di parlato:
  tutte le battute del Narratore, le 12 introduzioni/conclusioni dei ricordi e tutte le battute del filmato iniziale (anche Amerigo adulto e bambino). Guida Audacity
  nel file (44.100 Hz mono, riduzione rumore, normalizza a -1 dB, 0,3 s di silenzio, etichette + Esporta multipli in `Assets/Audio/Voci/`).
- Prova: `EnigmiSelfTest` controlla che gli ID delle battute siano unici.

### Scostamenti
- Il tasto per l'altoparlante è **V** (E e Spazio fanno già avanzare il dialogo).
- L'opzione sta in Accessibilità, non in Audio: la scheda Audio delle Impostazioni è generata dalle opzioni grafiche/audio esistenti.
- Il filmato iniziale (R025) ha il suo regista (`FilmatoIniziale`): le sue voci partono già da sole se il file c'è; il pulsante altoparlante lì non è stato aggiunto.
- Le schede civiche lette dal narratore non hanno ancora una voce (sono nel riquadro della vista ravvicinata): da decidere se registrarle.

### Da fare a casa con Unity
1. Console senza errori (nuovo `Altoparlante.cs`; toccati `CutscenePlayer.cs`, `UIRoot.cs`, `EscapeRoomController.cs`, `EscapeExit.cs`, `SettingsPanel.cs`, `SaveSystem.cs`, `DataClasses.cs`).
2. Registrare un file di prova (es. `nar_scarpe_001.wav`), Aggiorna libreria voci, entrare nel ricordo: pulsante nero, voce, onde, testo che avanza a fine clip.
3. `-enigmitest`; screenshot.

### Criteri di accettazione
- [ ] Pulsante altoparlante sotto ogni battuta del Narratore, con stato "manca la voce" (scritto, da provare; manca nel filmato iniziale).
- [x] `Doppiaggio/DA_REGISTRARE.md` e `copione.csv` completi e consegnati all'utente.
- [ ] Self-test PASS, console pulita, Windows e WebGL.
