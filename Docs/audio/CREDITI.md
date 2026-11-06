# Crediti audio

Tutto l'audio del gioco è **originale e generato da codice** (sintesi in `Assets/_Game/Scripts/Audio/`): nessun file audio
esterno, nessun campione, nessuna licenza da rispettare.

I brani musicali e gli effetti/ambienti più pesanti sono **pre-generati** come file `.ogg` (Vorbis, mono, 22,05 kHz, circa 1 MB in
tutto) con lo strumento `Tools/AudioGen` (`genera.sh`), che compila lo stesso codice di sintesi con gli stessi seed: i file sono il
risultato di `MusicSynth` / `SfxSynth`, non registrazioni. In `Assets/Audio/`: `Musica/` (tema solo, ricordo, scoperta, vicolo
mandolino, stinger risolto), `Ambienti/` (vicolo_brusio, vento_mare, ricordo_tono, stazione_vapore, interno_silenzio),
`Effetti/` (violino_sale, porta_apri, campane, venditore). Gli altri effetti brevi restano sintetizzati a runtime (pochi ms).
La sintesi resta anche come ripiego se un file manca. Il violino resta sintetico (nessuna registrazione dal vivo).

| Contenuto | Origine | Autore | Licenza |
|---|---|---|---|
| Tema di Amerigo (varianti solo, ricordo, scoperta, vicolo) e stinger "enigma risolto" | `MusicSynth.cs` (file `.ogg` pre-generati in `Assets/Audio/Musica`), melodia originale (`Docs/audio/tema_amerigo.abc`) | Thomas Rosania, Luca Arduini / Claude | Originale del progetto |
| Effetti (passi, porte, raccolta, treno, campane, gabbiani, ecc.) e ambienti (vicolo, interno, ricordo, stazione) | `SfxSynth.cs` e `Synth.cs` | Thomas Rosania, Luca Arduini / Claude | Originale del progetto |
| Suoni dell'interfaccia (clic, timbro, valigia, macchina da scrivere) | `UISfx.cs` (R001) | Thomas Rosania, Luca Arduini / Claude | Originale del progetto |

Nessun brano, tema o registrazione del film o della sua colonna sonora è usato, né imitato.

## Come sostituire un suono con una registrazione vera
1. Mettere il file (`.ogg`, mono, 22-32 kHz per effetti e ambienti) in `Assets/Audio/Musica`, `Assets/Audio/Ambienti` o `Assets/Audio/Effetti`.
   Il nome è l'id di `audio.json`; più varianti con suffisso numerico (`passo_pietra_01.ogg`, `passo_pietra_02.ogg`).
2. Menu **Treno > Audio > Aggiorna libreria**.
3. Aggiungere qui sotto una riga con fonte, autore, licenza (CC0, o CC-BY con attribuzione anche in `CREDITS.md`).
