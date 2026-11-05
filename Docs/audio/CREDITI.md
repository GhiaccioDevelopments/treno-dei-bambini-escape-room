# Crediti audio

Tutto l'audio del gioco è **originale e generato da codice** (sintesi in `Assets/_Game/Scripts/Audio/`): nessun file audio
esterno, nessun campione, nessuna licenza da rispettare, peso aggiunto alla build pari a zero byte di asset.

| Contenuto | Origine | Autore | Licenza |
|---|---|---|---|
| Tema di Amerigo (varianti solo, ricordo, scoperta, vicolo) e stinger "enigma risolto" | `MusicSynth.cs`, melodia originale (`Docs/audio/tema_amerigo.abc`) | Ghiaccio Developments / Claude | Originale del progetto |
| Effetti (passi, porte, raccolta, treno, campane, gabbiani, ecc.) e ambienti (vicolo, interno, ricordo, stazione) | `SfxSynth.cs` e `Synth.cs` | Ghiaccio Developments / Claude | Originale del progetto |
| Suoni dell'interfaccia (clic, timbro, valigia, macchina da scrivere) | `UISfx.cs` (R001) | Ghiaccio Developments / Claude | Originale del progetto |

Nessun brano, tema o registrazione del film o della sua colonna sonora è usato, né imitato.

## Come sostituire un suono con una registrazione vera
1. Mettere il file (`.ogg`, mono, 22-32 kHz per effetti e ambienti) in `Assets/Audio/Musica`, `Assets/Audio/Ambienti` o `Assets/Audio/Effetti`.
   Il nome è l'id di `audio.json`; più varianti con suffisso numerico (`passo_pietra_01.ogg`, `passo_pietra_02.ogg`).
2. Menu **Treno > Audio > Aggiorna libreria**.
3. Aggiungere qui sotto una riga con fonte, autore, licenza (CC0, o CC-BY con attribuzione anche in `CREDITS.md`).
