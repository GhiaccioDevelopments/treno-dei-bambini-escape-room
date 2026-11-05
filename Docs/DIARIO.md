# Diario di sviluppo

Memoria condivisa del progetto tra i vari PC. Le voci più recenti sono in alto.
Regole d'uso: vedi la sezione "Diario e lavoro su più PC" in `CLAUDE.md`.

---

## 2026-11-05 – PC scuola – Protocollo richieste e R001 (UI)
**Branch:** main + `richiesta/R001-ui-spettacolare`

### Fatto
- Creato il protocollo per passare idee tra sessioni/PC: `Docs/richieste/README.md` + regola in `CLAUDE.md`.
- Scritta la richiesta **R001 – UI spettacolare in stile antico** (PR in bozza): UI dinamica e "antica"
  (carta, ottone, ceralacca, timbri, biglietti del treno), effetti su pulsanti, prompt interagisci,
  inventario a valigia, HUD, toast, dialoghi, pausa, titolo, enigmi, transizioni, cutscene.

### Prossimi passi (PC casa)
- Prima chiudere e pubblicare il lavoro in corso (fase 2.5…), poi implementare R001 sul suo branch
  seguendo `Docs/richieste/README.md`. Merge solo con l'ok dell'utente.

---

## 2026-11-05 – PC scuola – Release v0.1 e avvio del diario
**Branch:** main · **Ultimo commit su GitHub prima di oggi:** 4fd38aa (3/11)

### Fatto
- Creata la release GitHub **v0.1 – Fase 1: vertical slice** con l'elenco dei cambiamenti e il link al gioco.
- Creati questo diario e le regole per lavorare da più PC (sezione nuova in `CLAUDE.md`).

### Problemi aperti
- ⚠️ **Lavoro non pubblicato sul PC di casa.** Nella sessione successiva al 3/11 sono state fatte molte
  modifiche sul PC di casa che non sono mai state inviate a GitHub (nessun commit remoto dopo il 3/11 18:30).
  Il lavoro è ancora solo su quel PC.

### Prossimi passi (al PC di casa)
1. Aprire Claude Code nella cartella del progetto.
2. Fare commit di tutto il lavoro locale non salvato.
3. `git pull --rebase` (per ricevere questo diario e le nuove regole), poi `git push`.
4. Aggiungere qui una voce che descrive tutto quello che era stato fatto in quella sessione.
5. Su conferma: nuova build WebGL su `gh-pages` e release (v0.2 o v0.1.1).

---

## 2026-11-03 – PC casa – Fase 1 (vertical slice)
**Branch:** feature/fase1-vertical-slice → main · **Ultimo commit:** 4fd38aa · **Tag:** v0.1
*(voce ricostruita dalla cronologia git)*

### Fatto
- Configurazione iniziale: CLAUDE.md, README, pacchetto MCP for Unity.
- Passaggio da 2D a **3D in terza persona** (decisione dell'utente).
- Sistemi di gioco: GameManager, salvataggio (PlayerPrefs), caricamento scene, transizioni, dati JSON.
- Scene: Boot, Titolo, Hub del vicolo, Basso di Antonietta, Flashback delle scarpe.
- Oggetto **Le scarpe**: 4 enigmi (quiz, lucchetto, oggetti nascosti, ordina/abbina) + cutscene.
- Inventario, pausa, schermata del titolo, controlli touch, predisposizione al doppiaggio (copione CSV).
- Correzioni dalla revisione: cifre del lucchetto, audio cutscene, click negli interni, salvataggio al ritorno.
- Build WebGL con pipeline Mobile pubblicata su GitHub Pages.

### Decisioni
- Gioco 3D, Unity 6000.6.7f1, URP, build WebGL per PC della scuola, Chromebook e LIM.
- Personaggi per ora segnaposto; modellazione in Blender rimandata.

### Prossimi passi
- Oggetti futuri (solo con ok dell'utente): stracci, volantino del treno, cappotto, violino, finale.
