#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="${HOME}/.dotnet:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
export EDI_DATA_ROOT="${EDI_DATA_ROOT:-$ROOT/data}"
mkdir -p "$EDI_DATA_ROOT"

echo "EDI 360 · iniciando entorno local"
echo "  Interfaz:  http://127.0.0.1:5173"
echo "  Servicios: http://127.0.0.1:5080"
echo "  Contraseña de demostración: demo"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "No se encontró el SDK de .NET 10."
  echo "Instálelo en https://dotnet.microsoft.com/download/dotnet/10.0"
  exit 1
fi

cd "$ROOT/src/backend/Edi360.Api"
dotnet run --urls http://127.0.0.1:5080 &
API_PID=$!

cleanup() {
  kill "$API_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

echo "Esperando al servidor en http://127.0.0.1:5080 …"
ok=0
i=0
while [ "$i" -lt 90 ]; do
  i=$((i + 1))
  if curl -sf http://127.0.0.1:5080/api/salud >/dev/null 2>&1; then
    ok=1
    break
  fi
  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "El servidor se detuvo. Revise el mensaje de error de dotnet arriba."
    exit 1
  fi
  sleep 1
done
if [ "$ok" -ne 1 ]; then
  echo "El servidor no respondió a tiempo. Compruebe que el puerto 5080 esté libre y que tenga .NET 10 (dotnet --version)."
  exit 1
fi

cd "$ROOT/src/frontend"
npm run dev -- --host 127.0.0.1 --port 5173
