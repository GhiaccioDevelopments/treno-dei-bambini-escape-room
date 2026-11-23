@echo off
rem Il Treno dei Bambini - passo 1 di 2: prepara i materiali d'epoca di Amerigo per MakeHuman.
rem Doppio clic su questo file (una volta sola per PC). Poi, in MakeHuman:
rem   Utilities > Scripting > Load > genera_amerigo_makehuman.py > Execute
setlocal
set "PY="
if exist "%ProgramFiles%\makehuman-community\Python\python.exe" set "PY=%ProgramFiles%\makehuman-community\Python\python.exe"
if not defined PY if exist "%LOCALAPPDATA%\makehuman-community\Python\python.exe" set "PY=%LOCALAPPDATA%\makehuman-community\Python\python.exe"
if not defined PY (
  echo MakeHuman non trovato in "%ProgramFiles%\makehuman-community" ne' in "%LOCALAPPDATA%\makehuman-community".
  pause
  exit /b 1
)
"%PY%" "%~dp0prepara_materiali_amerigo.py"
echo.
echo Fatto. Ora apri MakeHuman: Utilities ^> Scripting ^> Load ^> genera_amerigo_makehuman.py ^> Execute (e genera_capa_e_fierro_makehuman.py per Capa 'e fierro)
pause
