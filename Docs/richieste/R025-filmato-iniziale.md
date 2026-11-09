# R025 – Filmato iniziale della nuova partita (doppiato dall'utente)

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-09 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity (+ Blender MCP per scene e pose) |
| **Base** | `main` aggiornato. **Il codice attuale vince**. Leggere `Docs/TRAMA.md` prima di scrivere una riga |
| **Dipende da** | R024. Usa Timeline + Cinemachine 3 (sotto-fase 2.6), R019 (minimappa) e R023 (menu) |
| **Priorità** | **Molto alta**: per l'utente è molto importante, "deve essere bellissimo"; si può impiegare tutto il tempo che serve |

## 1. Cosa ha chiesto l'utente (parole sue)
> Quando si crea una nuova partita, come in tutti i giochi, ci deve essere un filmato dove ti spiega tutto: cosa fare, dove sei,
> la storia, e tutto questo doppiato da me. Ci puoi mettere anche un'ora, ma fallo. È molto importante e deve essere bellissimo.

## 2. Obiettivo
Con **Nuova partita** parte un filmato cinematografico in-engine (non un video preregistrato: pesa poco e funziona anche su
WebGL) che porta il giocatore dentro la storia e gli insegna a giocare, senza sembrare un tutorial.

## 3. Specifica
1. **Contenuto** (circa 3–5 minuti, in 5–7 capitoli):
   - *Dove sei*: Napoli, il presente del gioco (Amerigo adulto torna per la morte della madre Antonietta), i Quartieri, il basso.
   - *La storia*: flashback in seppia del 1946, il dopoguerra, la miseria, i treni dei bambini, la solidarietà
     (fedeltà a `TRAMA.md`; dialoghi **originali**, mai frasi del libro o del film).
   - *Cosa fare*: ritrovare gli oggetti di Antonietta; ogni oggetto apre un ricordo con enigmi; il quaderno, l'inventario (latta),
     la minimappa con la X dell'obiettivo, il pulsante Indizio sempre presente, l'errore che non punisce.
   - *Il tema civico*: perché il gioco parla di diritti, Costituzione, solidarietà (secondaria di 2° grado: tono serio, mai infantile).
   - Chiusura che sfuma dentro l'hub: controllo al giocatore e primo obiettivo sulla minimappa.
2. **Regia**: Timeline + Cinemachine 3, camere lente e motivate, luce d'epoca, look neorealista (ricordi in seppia come R007),
   tema del violino (R002), sottotitoli sempre attivi, dissolvenze. Personaggi del gioco esistenti (Amerigo adulto, Amerigo bambino,
   Antonietta se disponibile); niente sembianze degli attori del film.
3. **Controlli**: i tasti si mostrano per il dispositivo in uso (tastiera e mouse, gamepad, touch) come piccole targhette d'epoca,
   non come pannelli moderni.
4. **Doppiaggio**: ogni battuta ha un ID `cs_intro_NNN`, personaggio (Narratore, Amerigo adulto…), testo ed emozione, in
   `Resources/Data/cutscene_intro.json`, e finisce nel copione CSV. Senza audio il filmato funziona lo stesso (testo a tempo
   di lettura, avanza con click/E/Spazio); con audio il testo segue la clip. La lista dei file da registrare la consegna R028 a fine coda.
5. **Salta e rivedi**: tieni premuto per saltare, voce "Rivedi il filmato iniziale" in Pausa o negli Extra del menu.
   Parte da solo solo alla prima Nuova partita.
6. **Leggerezza**: niente video, niente bake extra; ≥60 FPS su Alta, fluido su Bassa e WebGL.
7. Testi solo in JSON. Prova automatica: il filmato parte, finisce, si salta e lascia il giocatore nell'hub col primo obiettivo.

## 4. Ordine di lavoro
1. Copione in italiano, controllato su `TRAMA.md` → **mostrarlo all'utente** prima della regia.
2. Storyboard con screenshot. 3. Timeline. 4. Prova completa + GIF.

## 5. Criteri di accettazione
- [ ] Copione approvato dall'utente.
- [ ] Filmato completo alla prima Nuova partita, saltabile e rivedibile, in Windows e WebGL.
- [ ] Battute con ID `cs_intro_NNN` nel CSV del doppiaggio.
- [ ] GIF/screenshot in `Docs/richieste/media/R025/`; console pulita.

## Rapporto
_(vuoto)_
