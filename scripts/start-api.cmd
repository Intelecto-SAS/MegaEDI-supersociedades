@echo off
setlocal
set ROOT=%~dp0..
cd /d "%ROOT%"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo No se encontro el SDK de .NET.
  echo Instale .NET 10: https://dotnet.microsoft.com/download/dotnet/10.0
  echo Luego vuelva a ejecutar este archivo.
  pause
  exit /b 1
)

echo EDI 360 - servidor
echo Si aparece un error, dejelo visible y no cierre esta ventana.
echo.
set EDI_DATA_ROOT=%ROOT%\data
if not exist "%EDI_DATA_ROOT%" mkdir "%EDI_DATA_ROOT%"
cd /d "%ROOT%\src\backend\Edi360.Api"
dotnet run --urls http://127.0.0.1:5080
echo.
echo El servidor se detuvo.
pause
