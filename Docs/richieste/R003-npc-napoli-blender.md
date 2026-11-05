# R003 – Personaggi modellati in Blender: la gente di Napoli (NPC)

| | |
|---|---|
| **Scritta da** | Claude, PC scuola (senza Unity né Blender), 2026-11-05 |
| **Per** | Claude, PC casa (Unity 6000.6.7f1 + MCP for Unity + **MCP Blender**) |
| **Quando** | Dopo aver chiuso il lavoro in corso. Indipendente da R001/R002 (vedi §9) |
| **Base** | `main` @ 7bd26c0. Le scene potrebbero essere cambiate dopo: **il codice attuale vince** |
| **Priorità** | Alta: il vicolo oggi è vuoto, e i personaggi sono capsule |

## 1. Cosa ha chiesto l'utente (parole sue)
> aggiungi anche nuovi personaggi modellati in Blender (NPC in mezzo a Napoli)

Obiettivo: **riempire Napoli di vita**. Il vicolo deve sembrare un quartiere popolare del 1946, con gente che
lavora, chiacchiera, gioca e vende. I personaggi sono **modellati in Blender** con uno stile coerente,
animati, e con battute originali che raccontano il dopoguerra e i temi di Educazione Civica.

## 2. Situazione di partenza (al 5/11)
- `CLAUDE.md`: personaggi da modellare in **Blender (MCP blender)** ed esportare in FBX in
  `Assets/_Game/Art/Characters/`. Fase personaggi **rimandata**: oggi ci sono solo segnaposto. Personaggi **originali**,
  ispirati alle descrizioni del romanzo, **mai** con le sembianze degli attori del film.
- Amerigo è una capsula da 1,2 m con coppola (`SceneBuilder.cs`, ~riga 206).
- `World/NpcInfo` (Interactable): mostra **una** battuta in un toast; la battuta è scritta **nella scena**
  (campo `battuta`), contro la regola "testi narrativi in JSON". Questa richiesta la sistema.
- `personaggi.json`: amerigo, antonietta, tommasino, maddalena, narratore (usati da cutscene e ritratti).
- Scene costruite da codice (`Editor/SceneBuilder.cs`, menu `Treno ▸ Costruisci tutte le scene`).

## 3. Vincoli (non negoziabili)
1. **Riproducibilità:** i modelli si generano con **script Python per Blender** salvati nel progetto
   (es. `Tools/Blender/*.py`), eseguiti tramite MCP Blender. Si salvano anche i `.blend` sorgente
   (con Git LFS) e gli FBX esportati. Rigenerare un personaggio deve voler dire rilanciare uno script,
   non rifare tutto a mano.
2. **Originalità e rispetto:** personaggi inventati. Niente somiglianze con attori o persone reali e
   **niente caricature o stereotipi offensivi sui napoletani o sul Sud**. Povertà raccontata con dignità.
   Varietà di età, corporatura e genere.
3. **Coerenza storica (Napoli 1946):** abiti, mestieri e oggetti dell'epoca (vedi §5). Niente elementi
   moderni. In caso di dubbio, scegliere il più generico, senza inventare dettagli storici.
4. **Prestazioni WebGL (Chromebook/LIM):**
   - **800–2.500 triangoli** per NPC di contorno; fino a ~5.000 per i personaggi principali;
   - **un solo materiale e una sola texture atlante (palette)** condivisi da tutti gli NPC, con UV sui
     colori della palette. Ogni personaggio è un'unica `SkinnedMeshRenderer` (parti unite prima dell'export);
   - scheletro semplice, **≤ 30 ossa**, max 2–4 influenze per vertice;
   - Animator con `cullingMode = CullUpdateTransforms` e LOD o disattivazione oltre ~25 m;
   - **massimo ~15 NPC animati visibili** contemporaneamente nel vicolo; più lontano solo figure statiche;
   - misurare gli fps in build WebGL prima e dopo e annotarli nel Rapporto.
5. **Stile:** low-poly **stilizzato** e caldo, coerente con gli ambienti in tufo e con la palette
   seppia/anni '40. Proporzioni leggermente caricate (teste un po' grandi, mani semplici), **senza volti
   realistici**: occhi e sopracciglia semplici, dipinti sulla palette o fatti con piccole geometrie.
   Ombreggiatura piatta o molto morbida.
6. **Contenuti fuori dal codice:** battute, nomi, mestieri e posizioni degli NPC in JSON (vedi §7).
7. **Non introdurre nuovi oggetti, flashback o enigmi** (restano su ok dell'utente). Gli NPC fanno
   atmosfera e danno informazioni o indizi gentili, ma non aprono nuovi contenuti di trama.
8. **Licenze:** animazioni o basi esterne solo **CC0** (es. Quaternius, Kenney) o con licenza
   chiaramente compatibile con un gioco pubblico gratuito, elencate in `Assets/_Game/Art/Characters/CREDITI.md`.
   Se si usa Mixamo, verificare e citare i termini d'uso Adobe; meglio CC0.

## 4. Pipeline proposta
1. **Base umana parametrica** in Blender (script): corpo low-poly a blocchi con parametri
   (altezza, corporatura, età: bambino ~1,1–1,3 m, adulto ~1,6–1,75 m, anziano un po' curvo) e **un
   unico scheletro compatibile con Unity Humanoid** (Hips, Spine, Chest, Neck, Head, braccia, gambe), così le
   animazioni si condividono tra tutti.
2. **Parti modulari** (script): teste/volti (4–6), capigliature (6–8), copricapi (coppola, fazzoletto
   annodato, cappello di feltro, berretto da marinaio, velo), busti/abiti (camicia con bretelle, gilet,
   giacca rattoppata, grembiule, scialle, abito a fiori sbiadito, maglione), pantaloni e gonne, scarpe, zoccoli o piedi nudi.
3. **Generatore di varianti:** script che combina parti + colori della palette da un file di configurazione
   (es. `Tools/Blender/npc_config.json`) → N personaggi diversi con un seed. Unisce le mesh, applica
   lo skinning ed esporta l'FBX (scala 1 = 1 m, asse Y su, `Apply Transform`, solo deform bones).
4. **Animazioni** condivise sullo scheletro Humanoid (vedi §6), esportate in FBX separati o nello stesso
   file con clip nominate, poi importate in Unity come Humanoid.
5. **Import in Unity** da menu editor (es. `Treno ▸ Personaggi ▸ Importa e configura`): impostazioni di
   import (Humanoid, compressione animazioni, niente materiali importati → materiale palette condiviso),
   creazione dei prefab `NPC_<id>` con Animator Controller generato da codice.
6. **Ritratti:** aggiornare i ritratti 2D usati nelle cutscene (`Resources/Sprites/ritratti`) con un render
   dei nuovi modelli (camera di Blender a mezzo busto, sfondo trasparente), per tenere tutto coerente.

## 5. Cast degli NPC del vicolo (proposta, 12–16 figure)
Ogni figura ha **un'attività**: nessuno sta fermo a fare niente.

| Id proposto | Chi | Aspetto (1946) | Attività / animazione |
|---|---|---|---|
| `lavandaia` | donna adulta | fazzoletto in testa, maniche arrotolate, grembiule | stende i panni tra i balconi o strizza in una tinozza |
| `venditrice_frutta` | donna anziana | scialle nero, gonna lunga | seduta dietro una cesta, richiama i passanti |
| `acquaiolo` | uomo anziano | gilet, coppola | banchetto dell'acqua con limone, versa da una brocca |
| `ciabattino` | uomo adulto | grembiule di cuoio, occhiali | martella una suola su uno sgabello (legame tematico con *Le scarpe*) |
| `scugnizzo_1/2/3` | bambini | vestiti rattoppati, alcuni scalzi, pantaloni corti | corrono, giocano a palla di stracci o con le biglie, si rincorrono |
| `bambina_corda` | bambina | vestitino sbiadito, trecce | salta la corda o gioca a campana |
| `nonna_finestra` | anziana | vestito scuro | affacciata a un balcone o basso, sgrana piselli |
| `uomo_carretto` | uomo adulto | camicia, bretelle | spinge un carretto di legno lungo un percorso |
| `donne_chiacchiere` | 2 donne | abiti a fiori sbiaditi, borse a rete | chiacchierano gesticolando davanti a un basso |
| `reduce` | uomo giovane | giacca militare senza mostrine, stampella o braccio al collo | seduto sui gradini, assorto (il dopoguerra) |
| `prete` | uomo | tonaca nera, cappello | cammina lento, saluta |
| `vigile` | uomo | uniforme scura generica d'epoca, senza stemmi reali precisi | sta all'incrocio, fa un gesto |
| `attacchina` | donna giovane | tailleur semplice | attacca un manifesto (testo generico sul voto del 2 giugno 1946, *senza* simboli di partito) |

Note:
- I personaggi **con nome della storia** (Antonietta, Tommasino, Maddalena) **non** fanno parte di questo
  cast: vedi §8.
- Il manifesto dell'`attacchina` è un aggancio civico (prime elezioni con il **voto alle donne**, referendum
  istituzionale). Il testo va in JSON ed è neutrale.

## 6. Animazioni (set condiviso)
`idle` (2 varianti, respiro e peso che si sposta), `cammina`, `corsa` (bambini), `parla_gesti` (mani
napoletane, ma con misura), `saluta`, `seduto_idle`, `seduto_lavoro` (sgranare, martellare),
`stendere_panni`, `richiamo` (mano alla bocca), `spingere_carretto`, `gioco_palla`, `salto_corda`,
`affacciato`, `guarda_verso` (testa che segue Amerigo con **IK della testa** di Unity, entro un angolo
limitato). Transizioni morbide (0,2–0,3 s). Velocità di riproduzione leggermente casuale (±10%) per evitare
che tutti si muovano in sincronia.

## 7. Comportamento in Unity
Nuovo componente (es. `World/Npc`) che sostituisce/estende `NpcInfo`:
- **Tipi:** `statico` (fa la sua attività sul posto), `percorso` (cammina tra waypoint in loop, con pause),
  `gruppo` (bambini che si rincorrono in un'area). Movimento con waypoint semplici; NavMesh solo se
  già presente o se il pacchetto AI Navigation è già nel progetto (altrimenti chiedere prima di aggiungerlo).
- **Reazione al giocatore:** entro ~4 m girano la testa verso Amerigo (IK). I bambini a volte gli corrono
  intorno. Chi cammina si ferma un attimo se Amerigo gli blocca la strada (niente spinte fisiche).
- **Interazione (E):** l'NPC si gira verso Amerigo, fa `saluta`/`parla_gesti` e mostra una battuta nel
  dialogo/toast (se R001 è fatta, nel nuovo stile). **Battute diverse a ogni interazione** (pool, senza
  ripetere la stessa due volte di fila), e battute che **cambiano in base ai progressi** (es. dopo aver
  completato il flashback delle scarpe, il ciabattino commenta).
- **Audio** (se R002 è fatta): passi 3D, versi o vocalizzi brevi, suoni delle attività (martello,
  panni, palla) dal canale Effetti.

### Schema JSON proposto (`Resources/Data/npc.json`)
```json
{
  "npc": [
    {
      "id": "ciabattino",
      "nome": "Il ciabattino",
      "modello": "NPC_ciabattino",
      "scena": "10_Hub_Vicolo",
      "tipo": "statico",
      "posizione": [3.5, 0, 12.0], "rotazioneY": 200,
      "animazione": "seduto_lavoro",
      "percorso": [],
      "battute": [
        { "testo": "Le scarpe si aggiustano, guagliò. Le persone pure, se qualcuno ci mette le mani.", "condizione": "" },
        { "testo": "Hai visto? Pure le scarpe vecchie hanno una storia da raccontare.", "condizione": "flashback:flashback_scarpe" }
      ]
    }
  ]
}
```
- `condizione`: vuota oppure `flashback:<id>`, `oggetto:<id>`, `!oggetto:<id>` (valutata sul `SaveData`).
- Il `SceneBuilder` legge `npc.json` per piazzare gli NPC quando ricostruisce le scene; in alternativa uno
  spawner a runtime li crea all'avvio della scena. Scegliere la soluzione più coerente con il codice attuale.
- Aggiornare `CLAUDE.md` (Schema JSON e Struttura) con il formato definitivo.

### Battute (linee guida)
- **Originali**, in italiano comprensibile con **qualche espressione napoletana** facile da capire dal
  contesto (es. *guagliò*, *jamme*, *mannaggia*), mai dialetto stretto incomprensibile per la classe.
- 2–4 battute per NPC, brevi (max ~140 caratteri), che raccontano il **dopoguerra** (fame, macerie,
  lavoro dei bambini, scuola, voto alle donne, speranza nella ricostruzione), senza spiegoni e senza
  anticipare la trama dei prossimi oggetti.
- Nessuna battuta presa dal film o dal libro.

## 8. Amerigo e i personaggi della storia (da confermare con l'utente)
La stessa pipeline può (e prima o poi deve) produrre **Amerigo** (~1,2 m, coppola, vestiti rattoppati,
scarpe – o piedi nudi prima di avere *Le scarpe*?) e i personaggi con nome (**Antonietta, Tommasino,
Maddalena**), con più cura (fino a ~5.000 triangoli, espressioni base per le cutscene).
Con NPC veri intorno, un Amerigo-capsula stona molto. **Proposta:** fare Amerigo come **primo modello**,
subito dopo la base, se l'utente è d'accordo (vedi Domande aperte). Gli altri personaggi della storia restano
per una richiesta successiva.

## 9. Rapporto con R001 e R002
- R001 (UI): il prompt "Parla con …" e i dialoghi degli NPC useranno il nuovo stile; se R001 non è
  fatta, usare la UI attuale.
- R002 (audio): passi e suoni delle attività degli NPC dal canale Effetti; se R002 non è fatta, lasciare
  dei punti di aggancio (`Sfx.Play` vuoto o evento) senza audio.

## 10. Ordine di lavoro consigliato
1. Verificare che l'MCP Blender funzioni; creare `Tools/Blender/` con lo script della **base umana + scheletro**.
2. Prova di pipeline con **un solo NPC** (es. `ciabattino`): modello → animazioni `idle`/`seduto_lavoro` →
   FBX → import Humanoid → prefab → in scena → **build WebGL di prova** e misura fps/peso.
3. (Se l'utente approva) **Amerigo** sulla stessa base, sostituendo la capsula senza rompere
   `ThirdPersonController`/`CharacterController`: animazioni `idle`/`cammina`/`corsa` guidate dalla velocità.
4. Parti modulari + generatore di varianti + atlante palette condiviso.
5. Set di animazioni condiviso.
6. Componente `Npc` + `npc.json` + condizioni + IK della testa.
7. Popolare il vicolo (12–16 NPC), 1–2 NPC nel basso di Antonietta se ha senso (es. vicina che passa).
8. Ritratti aggiornati dai render.
9. Ottimizzazione (LOD/culling), build WebGL finale, Rapporto con screenshot.

Dopo ogni punto: `read_console`, zero errori.

## 11. Criteri di accettazione
- [ ] Script Blender nel progetto che rigenerano base, parti, varianti ed export (con `.blend` e FBX in LFS).
- [ ] Almeno **12 NPC** diversi nel vicolo, ognuno con un'attività animata e niente due identici vicini.
- [ ] Un solo materiale/atlante per gli NPC; triangoli e ossa entro i limiti del §3.
- [ ] Animazioni condivise su scheletro Humanoid, desincronizzate tra loro.
- [ ] NPC che girano la testa verso Amerigo e reagiscono all'interazione con gesto + battuta.
- [ ] Battute in `npc.json` con condizioni sui progressi; `NpcInfo` non ha più testi scritti nella scena.
- [ ] Nessuna somiglianza con attori, nessuno stereotipo offensivo, abiti coerenti con il 1946.
- [ ] Build WebGL fluida (≥ 30 fps nel vicolo affollato su PC scolastico/Chromebook) e peso annotato.
- [ ] `CREDITI.md` per eventuali risorse esterne (solo CC0 o licenza compatibile).
- [ ] Zero errori in console; il flashback delle scarpe resta giocabile dall'inizio alla fine.
- [ ] (Se approvato) Amerigo modellato e animato al posto della capsula.

## 12. Domande aperte (per l'utente, da chiedere a casa)
1. Facciamo **Amerigo** come primo modello (consigliato), o per ora solo gli NPC di contorno?
2. Gli NPC possono avere qualche battuta con **espressioni napoletane** (comprensibili), o solo italiano?
3. Ti piacciono le figure del cast (§5)? Ne vuoi aggiungere o togliere qualcuna?

## 13. Rapporto (da compilare da chi implementa)
**Implementata da:** … · **Date:** … · **Commit finali:** …

### Fatto
- …

### Scostamenti dalla specifica (e perché)
- …

### Risorse esterne usate
- (elenco completo in `Assets/_Game/Art/Characters/CREDITI.md`)

### Domande aperte per l'utente
- …

### Come testare
1. …

### Misure
- fps nel vicolo (PC / Chromebook / LIM): …
- triangoli medi per NPC, numero di NPC visibili: …
- dimensione build prima → dopo: …

### Media
- `Docs/richieste/media/R003/…`
