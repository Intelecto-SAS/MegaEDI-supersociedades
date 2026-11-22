@echo off
setlocal
set ROOT=%~dp0..
echo Abriendo el servidor y la interfaz en dos ventanas...
start "EDI 360 servidor" "%~dp0start-api.cmd"
timeout /t 8 /nobreak >nul
start "EDI 360 interfaz" "%~dp0start-ui.cmd"
echo.
echo Cuando el servidor termine de cargar, abra:
echo   http://127.0.0.1:5173
echo.
echo Si la entrada sale en blanco, mire la ventana "EDI 360 servidor".
echo Ahi debe aparecer "Now listening on: http://127.0.0.1:5080"
echo.
pause
