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
_(vuoto)_
