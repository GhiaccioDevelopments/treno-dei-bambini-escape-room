# R017 – Mappe pulite: niente sfarfallii tra superfici sovrapposte, niente buchi sul vuoto

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3 (dopo il merge di R006). **Il codice attuale vince** |
| **Dipende da** | R006 (mappe grandi, già unita). Da fare **prima** di R018 (camera) e prima delle tappe 2-3 di R007 |
| **Priorità** | Alta (difetto visibile in tutte le scene) |

## 1. Cosa ha chiesto l'utente (parole sue)
> Migliora molto la mappa, ci sono punti dove sono unite e sfarfallano le mappe oppure punti vuoti dove si vede il vuoto sotto la mappa.

Due difetti distinti:
- **Sfarfallio (z-fighting)**: due superfici complanari o quasi (pavimento su pavimento, muro su muro, decal/manifesto
  appoggiato a filo, pezzi di mappa giuntati che si sovrappongono) lottano per la profondità e "tremolano" muovendo la camera.
- **Buchi sul vuoto**: fessure tra pezzi di pavimento/muro, bordi della mappa aperti, pezzi che non arrivano a terra,
  facce mancanti o normali invertite (backface culling) da cui si vede lo sfondo/skybox sotto o dietro la mappa.

## 2. Situazione di partenza
- Le scene sono generate dagli script editor (`Editor/SceneBuilder*.cs`, `SceneBuilderAmbienti`) con R006: hub
  "Quartieri" molto più grande, basso di Antonietta a più stanze, strada del flashback "Le scarpe" allargata.
- R006 ha già fatto un controllo delle **compenetrazioni** tra oggetti; questa richiesta riguarda le **superfici della mappa**
  (pavimenti, selciato, muri, facciate, soffitti, giunzioni tra blocchi), non i props.

## 3. Specifica
1. **Strumento di diagnosi riproducibile** (menu `Treno ▸ Verifica ▸ Superfici e buchi`), che per ogni scena:
   - trova coppie di renderer con facce complanari sovrapposte (stessa normale, distanza < 2 mm, aree che si intersecano)
     e le elenca con posizione e nomi;
   - cerca **buchi**: griglia di raggi verticali dall'alto su tutta l'area calpestabile e visibile (passo 0,25 m) e raggi
     orizzontali dalla posizione della camera verso i muri; ogni raggio che non colpisce nulla dove ci si aspetta terreno/muro
     è un buco. Elenco + screenshot di posizione;
   - controlla mesh con facce a normale invertita visibili dalla zona giocabile.
   Il rapporto (testo) va in `Docs/richieste/media/R017/`.
2. **Correzioni nei generatori, non a mano**: ogni fix entra nello script che costruisce la scena (offset, misure, fusione
   dei pezzi), così la scena resta riproducibile con `Treno ▸ Costruisci tutte le scene`.
   - Pavimenti/selciato: un solo piano continuo per zona (o pezzi che si toccano bordo a bordo, senza sovrapposizione);
     marciapiedi e gradini con altezza vera, non piani sovrapposti a 1 mm.
   - Decal, manifesti, insegne, macchie: offset di almeno 5-10 mm dalla parete **oppure** URP Decal Projector / shader con
     depth offset; mai complanari.
   - Giunzioni tra blocchi di edifici: sovrapposizione nascosta *dentro* il muro (non sulla faccia visibile) o snap esatto.
   - Bordi del mondo: nessun bordo aperto visibile. Chiusura con facciate, fondali (backdrop) dipinti/low-poly coerenti
     con Napoli 1946, terreno che prosegue oltre i limiti percorribili e collisori invisibili ai margini.
   - Sotto la mappa: un piano "di sicurezza" scuro sotto ogni scena (non visibile in condizioni normali), così un buco
     residuo non mostra mai il cielo; e un **kill-plane** che riporta il giocatore allo SpawnPoint se cade.
3. **Camera far/near**: verificare `near clip plane` (non troppo basso, es. ≥ 0,1) e `far`, perché un rapporto near/far
   esagerato peggiora lo z-fighting; coordinare con R018.
4. Ripetere la diagnosi dopo le correzioni: **zero** sovrapposizioni complanari visibili e **zero** buchi nell'area giocabile.

## 4. Ordine di lavoro consigliato
1. Strumento di diagnosi + primo rapporto (prima/dopo). 2. Fix nei generatori, scena per scena (hub, basso, flashback).
3. Bordi del mondo e piano di sicurezza/kill-plane. 4. Ricostruzione scene, **un solo bake finale**, screenshot.

## 5. Criteri di accettazione
- [x] Menu di diagnosi presente e documentato; rapporto prima/dopo in `Docs/richieste/media/R017/`.
- [ ] Nessuno sfarfallio visibile girando la camera in hub, basso e flashback (GIF o 3-4 screenshot nei punti segnalati).
- [x] Nessun punto da cui si veda il vuoto/cielo sotto o dietro la mappa, anche camminando lungo tutti i bordi (verificato con 107.616 raggi da ogni punto calpestabile a passo 1 m: 0 escono nel vuoto; non a occhio camminando).
- [x] Kill-plane funzionante (cadere = ritorno allo spawn, con dissolvenza): ritorno verificato in Play; la dissolvenza usa Fader.Out/In come SceneLoader, non vista a occhio.
- [ ] Scene rigenerate dai generatori, console pulita, FPS invariati o migliori (preset Alta e Bassa).

## Rapporto
Compilato da Claude, PC casa, 2026-11-07. Branch `richiesta/R017-mappe-senza-buchi-sfarfallii` (unito a `feature/upgrade-realistico` dopo R022). Stato: **corretto e verificato con la diagnosi (criterio severo dopo la review); restano aperte la bicicletta, la verifica a occhio in gioco e gli FPS**.

### Strumento di diagnosi
Menu **Treno > Verifica > Superfici e buchi** (`Assets/_Game/Editor/VerificaSuperfici.cs`; in batch `VerificaSuperfici.Esegui("etichetta")`, rapporto in `Docs/richieste/media/R017/rapporto_<etichetta>.txt`).
Per ogni scena (01_Titolo, 10_Hub_Vicolo, 20_Int_BassoAntonietta, 30_FB_Scarpe):
1. **Sfarfallii**: coppie di facce con stessa normale, a meno di 2 mm lungo la normale e con sovrapposizione di almeno 3 mm, tra renderer diversi o nello stesso mesh (griglia spaziale 2 m, SAT nel piano).
   Una coppia e' **VISIBILE** se i materiali differiscono e almeno una camera di prova (500 punti dell'area calpestabile a 2 m d'altezza, entro 45 m, davanti alla faccia) vede il baricentro con un segmento libero
   (qualunque faccia, anche posteriore, lo ostruisce). I livelli dello stesso LODGroup non si confrontano. Le coppie con stesso materiale o coperte sono contate ma non visibili.
2. **Buchi**: area calpestabile = riempimento a 25 cm dallo SpawnPoint con la capsula del giocatore (stesso metodo di Raggiungibilita'); per ogni cella (e un punto casuale per cella, solo dove il suolo di gioco esiste)
   un raggio verticale su MeshCollider temporanei dei soli renderer visibili (solo facce frontali). Nessuna superficie = buco; solo faccia posteriore = normale invertita; un oggetto che copre il suolo non e' un buco.
3. **Bordi aperti**: da ogni punto calpestabile a passo 1 m, altezza camera 1,7 m, 16 azimut x 2 inclinazioni (0 e -12 gradi), 150 m (far): un raggio che non colpisce nulla e' un vuoto visibile.
La scena non viene modificata (collisori temporanei rimossi); il piano di sicurezza e' escluso dalla diagnosi.

### Numeri prima / dopo (rapporto_prima.txt / rapporto_dopo.txt, stesso strumento)
Il criterio di visibilita' e' stato reso piu' severo dopo la review: il materiale non conta piu' (anche stesso materiale e stesso mesh con tile o lightmap diversi), la faccia e' visibile se
**almeno un punto campionato nella zona sovrapposta** (7 punti baricentrici, non solo il baricentro) e' visto da una delle 800 camere di prova entro **120 m**, 120 tentativi per punto. Il "prima" qui sotto e' il rapporto
ottenuto col criterio precedente (rapporto_prima.txt, stesso strumento a scene originali) e non e' direttamente confrontabile con il dopo; i veri conteggi severi del dopo sono sotto.
| | Prima (criterio vecchio) | Dopo (criterio severo) |
|---|---|---|
| Coppie complanari (totale) | 485 | 67 |
| **Coppie complanari VISIBILI** | **195** (Titolo 5, Hub 137, Basso 9, Flashback 44) | **8** (Titolo 0, Hub 7, Basso 0, Flashback 1) |
| Buchi / facce invertite sul suolo | 1 | 0 |
| Raggi che escono nel vuoto | **45** (flashback, cortile est) | **0** su 107.616 |
Le 8 coppie residue sono **tutte la stessa mesh della bicicletta** (`Pezzo` "Bicicletta", 8 istanze: pneumatico e cerchio, tubi del telaio), sovrapposizioni di 1-3 cm tra tubi sottili.
Provati: tubi a 5 lati, raggi sfalsati di 2,2 mm per tubo e per segmento del toro; resta non azzerato. Non e' una facciata ne' una superficie di mappa; da decidere se rifare la bicicletta con facce quad/
mesh importata. Le mesh importate da FBX (Carretto, Botte...) sono escluse dal confronto interno (non dipendono dai generatori).

### Cause trovate e correzioni (tutte nei generatori SceneBuilder*, nessuna a mano)
- **Varchi nei muri del flashback (45 raggi nel vuoto)**: `Gap()` in `SceneBuilderQuartiere.cs` arrotondava il tratto di muro libero a un intero di metri e lasciava varchi di 0,5-1 m accanto alle feature a mezzo metro
  (cortile est, x=19). Ora il tratto e' coperto per intero (il resto frazionario va all'ultimo modulo, tratti sotto 5 cm ignorati); chiave mesh con la lunghezza reale.
- **Facce complanari tra moduli di facciata vicini** (zoccoli, fasce marcapiano, cornicioni sovrapposti di 2 cm ai giunti e agli spigoli): i moduli ricevono una **fascia di quota** (colorazione a grafo, passo **12 mm**, mesh distinta per fascia con i profili orizzontali alzati di fascia x 12 mm; i moduli non si alzano piu': niente fessure alla base)
  diversa da quella dei vicini. Alle testate i profili rientrano di 12 mm e una **linguetta** piu' bassa (3 cm / 4,5 cm sul fronte, 4 mm sopra e sotto) li prolunga dentro il profilo del vicino: nessuna tacca aperta e nessuna faccia visibile complanare. Lo zoccolo scende di 30 cm sotto il suolo. Il dado dei muri interni ha quote diverse per asse e nome.
- **Facce complanari dentro il mesh** (cornici, stipiti, davanzali, architravi, vetri, battenti contro intonaco e legno; centinaia per facciata): `KitBuilder.Box` registra le facce emesse e, se una scatola nuova ha una faccia
  complanare (2 mm) e sovrapposta di materiale diverso a una precedente, la **allarga di 12 mm sulla faccia in conflitto** (`Sporgenza`; il materiale non conta piu': UV e lightmap differiscono comunque). Non si spostano mai le scatole dell'intonaco (SubFisso) ne' le facce
  alle testate della parete (`ProteggiTestate`). Stipiti e architravi delle aperture rientrano di 12 mm nel vano (`Riporto`), il vetro della porta generica sta dietro il battente, la faccia superiore dell'ultimo piano non si disegna (coperta dal parapetto).
- **Alzata della prima scala** sporge di 12 mm dalle testate dei moduli; fondo dell'edicola grande a 7 cm (la fascia della facciata sporge 5 cm); cartello del basso 1,5 cm davanti al dado; soffitto della stanza accorciato di 4 cm; fronte del selciato del Titolo +5 cm; zoccolo a 0,93 m (non coincide con i gradini); cintura dei pantaloni stesi a 2 cm.
- **Bordi del mondo**: dopo la correzione dei varchi ogni bordo calpestabile e' chiuso da un modulo di facciata (verificato: 0 raggi nel vuoto, anche a -12 gradi), quindi non servono fondali dipinti aggiuntivi: non ho creato asset nuovi
  che non migliorerebbero la scena. Se si vorra' un fondale (sagoma del Vesuvio o palazzi lontani) andra' deciso con l'utente (coerenza storica, Napoli 1946).
- **Rete di sicurezza**: `SceneBuilderSicurezza.cs` aggiunge in ogni scena `Sicurezza/PianoSicurezza` (piano scuro `M_PianoSicurezza` 2,5 m sotto il punto piu' basso, 300 m di margine, senza collider ne' ombre) e, nelle scene giocabili,
  `Sicurezza/KillPlane` (`Scripts/World/KillPlane.cs`): sotto la quota (piano - 3 m) fa Fader.Out, riporta il giocatore allo **SpawnPoint piu' vicino**, azzera il moto, riaggancia la camera, Fader.In (input bloccato durante il recupero).
- **WebGL (depth 24 bit, niente Z invertito)**: tutti gli offset visibili sono ora 1-2 cm (prima 3,5-4 mm).
- **Camera**: near 0,1 m e far 150 m gia' in `MakeCamera` e nella Lens di Cinemachine (`SceneBuilderCamera.cs`, rapporto 1:1500): non modificati, nessuna interferenza con R018.

### Prove
- Foto prima/dopo in `Docs/richieste/media/R017/` (1280x720 JPG): `fb_bordo_vuoto_prima.jpg` / `fb_bordo_chiuso_dopo.jpg` (il varco azzurro nel muro del cortile est e la facciata che ora lo chiude, da (14,6; 1,7; -11,3) verso +x),
  `hub_giunto_prima.jpg` / `hub_giunto_dopo.jpg` (giunto tra moduli S_54 e S_55, da (12; 1,7; -13,4)) e `hub_giunto2_*.jpg` (arco sul vico, da (-3; 1,7; 6,5)). Il tremolio non si vede in una foto fissa: la prova e' numerica.
- Console: nessun errore ne' avviso dei miei script (solo il promemoria "Luci da rifare" dei generatori, prima del bake).
- **Un solo bake** dopo le correzioni della review (Treno > Look > Bake luci di tutte le scene, 153 s). **LFS aggiunto in questo secondo giro: circa 27,6 MB** (lightmap e reflection probe rigenerate); in tutto la richiesta aggiunge circa 55 MB (due bake): totale stimato circa 645 MB su 1 GB, sotto la soglia di 850.
- Kill-plane in Play (da 00_Boot, caricato 10_Hub_Vicolo): teletrasporto del giocatore a y=-9 (soglia -6,8) -> tornato a (0; 0,03; -40) = SpawnPoint "inizio".

### Non verificato / aperti
- **FPS preset Alta e Bassa prima/dopo non misurati**: con l'editor non in primo piano il gioco non avanza frame (0 frame in 11 s) e `EditorApplication.update` e' limitato a circa 10 Hz. Proxy: triangoli e renderer invariati (vedi sopra).
- Nessuno sfarfallio e nessun vuoto sono stati verificati **solo con la diagnosi** (e foto fisse), non girando la camera a mano in Play.
- La dissolvenza del kill-plane non e' stata vista a occhio (usa lo stesso Fader di SceneLoader).
- Restano 59 coppie complanari non visibili da nessuna camera di prova (dentro i muri), non da correggere.
- Bicicletta: 8 coppie visibili residue (vedi sopra).
- `ScalaLegno` nel cortile del basso: ora ha il collider (ostacolo appoggiato al muro, non una scala calpestabile). Raggiungibilita': 0 problemi.
- KillPlane senza SpawnPoint: LogWarning, nessun teletrasporto, il piano si disattiva (niente ciclo).
