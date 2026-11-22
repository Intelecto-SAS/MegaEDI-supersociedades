@echo off
setlocal
set ROOT=%~dp0..
cd /d "%ROOT%\src\frontend"

where node >nul 2>&1
if errorlevel 1 (
  echo No se encontro Node.js. Instale la version 22: https://nodejs.org
  pause
  exit /b 1
)

if not exist node_modules (
  echo Instalando la interfaz...
  call npm install
)
echo Interfaz: http://127.0.0.1:5173
echo Deje esta ventana abierta. El servidor debe estar en otra ventana (start-api.cmd).
echo.
call npm run dev -- --host 127.0.0.1 --port 5173
pause
