# Il Treno dei Bambini – Escape Room

Gioco educativo per la scuola secondaria di 2° grado (**Educazione Civica**), ispirato al romanzo
*Il treno dei bambini* di Viola Ardone (2019) e al film omonimo di Cristina Comencini (2024).

Napoli, 1946. Il giocatore guida **Amerigo Speranza** tra i vicoli del dopoguerra. Entrando nelle case
trova oggetti legati alla sua vita: ognuno apre un **flashback in stile escape room**, con enigmi che
collegano la storia ai temi della cittadinanza (diritti dell'infanzia, lavoro minorile, diritto
all'istruzione, voto alle donne, fake news e pregiudizi, uguaglianza). Risolti gli enigmi, l'oggetto
entra nell'inventario e una cutscene racconta la sua storia.

## Giocare
- **Online**: https://ghiacciodevelopments.github.io/treno-dei-bambini-escape-room/ (build WebGL 3D, nessuna installazione)
- Comandi: **WASD / frecce** per muoversi, **mouse** per la camera, **E** per interagire, **I** inventario, **Esc** pausa.
  Su LIM, Chromebook e touch: joystick e pulsanti a schermo.
- I progressi vengono salvati automaticamente nel browser.

## Aprire il progetto
1. Installa **Unity 6000.6.7f1** (con modulo *WebGL Build Support*) da Unity Hub.
2. Clona il repository con Git LFS attivo: `git lfs install` e poi `git clone https://github.com/GhiaccioDevelopments/treno-dei-bambini-escape-room.git`
3. In Unity Hub: *Add project from disk* e scegli la cartella clonata.
4. Apri la scena `Assets/_Game/Scenes/00_Boot.unity` e premi Play.

Menu utili dell'editor (**Treno ▸ …**): rigenerazione di grafica e scene, esportazione del copione per il
doppiaggio (CSV), build WebGL.

## Modificare i contenuti
Testi, enigmi, oggetti e dialoghi sono file JSON in `Assets/_Game/Resources/Data/`: si possono modificare
senza toccare il codice. Le voci del doppiaggio vanno in `Assets/Audio/Voci/` con il nome uguale all'ID
della battuta (es. `cs_scarpe_001.ogg`).

## Crediti
- Ispirato al romanzo *Il treno dei bambini* di **Viola Ardone** (Einaudi, 2019) e al film di
  **Cristina Comencini** (2024). Il gioco non usa immagini, musiche né dialoghi dell'opera originale:
  testi, grafica e personaggi sono una rielaborazione originale a scopo didattico.
- Contesto storico: i "treni della felicità" organizzati dal PCI e dall'UDI (1945–1952).
- Sviluppo principale: **Thomas Rosania** e **Luca Arduini**.
- Collaboratori: **Zaim Halili** e **Renis Caushi**.
- Con il supporto di Claude Code.
