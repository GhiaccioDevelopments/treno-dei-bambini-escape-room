#!/bin/sh
# Rigenera i file audio pre-generati (R002) in Assets/Audio/{Musica,Ambienti,Effetti}. Richiede dotnet SDK e python con "soundfile".
# Va rieseguito se cambia MusicSynth.cs / Synth.cs / SfxSynth.cs (la sintesi di ripiego e i file devono restare identici).
set -e
cd "$(dirname "$0")"
TMP="${TMPDIR:-/tmp}/audiogen_$$"
dotnet run -c Release -- "$TMP/wav"
MUS="tema_amerigo_solo tema_amerigo_ricordo tema_amerigo_scoperta vicolo_mandolino stinger_risolto"
AMB="vicolo_brusio vento_mare ricordo_tono stazione_vapore interno_silenzio"
SFX="violino_sale porta_apri campane venditore"
python encode_ogg.py "$TMP/wav" ../../Assets/Audio/Musica 0.8 $MUS
python encode_ogg.py "$TMP/wav" ../../Assets/Audio/Ambienti 0.8 $AMB
python encode_ogg.py "$TMP/wav" ../../Assets/Audio/Effetti 0.8 $SFX
rm -rf "$TMP"
