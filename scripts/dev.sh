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

cd "$ROOT/src/backend/Edi360.Api"
dotnet run --urls http://127.0.0.1:5080 &
API_PID=$!

cleanup() {
  kill "$API_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

until curl -sf http://127.0.0.1:5080/api/salud >/dev/null 2>&1; do
  sleep 1
done

cd "$ROOT/src/frontend"
npm run dev -- --host 127.0.0.1 --port 5173
