# Arquitectura local

Monolito modular:

- Interfaz: React, TypeScript, Vite.
- API: ASP.NET Core 10, una sola aplicación.
- Datos: SQLite en `data/edi360.db` y `data/gedess-legacy.db`.
- Documentos: archivos en `data/storage` y `data/legacy-storage`.
- Eventos: tabla de salida (outbox) y un procesador en segundo plano.
- Identidad: sesión en cookie httpOnly. El cambio de funcionario del centro de demostración crea una sesión nueva en el servidor.

Inicio: `./scripts/dev.sh` aplica la base, carga datos ficticios y levanta interfaz y API. No usa Docker.

En Vercel la misma aplicación se publica con `vercel.json` (interfaz Vite + API en contenedor). Ahí SQLite no persiste: se usa Postgres (`DATABASE_URL` de Neon) y los documentos van en la base. Ver `docs/despliegue-vercel.md`.

Los dominios (documental, justicia, supervisión, datos, informes, modernización) se separan por servicios. Ningún módulo escribe en “la base del otro”: los cambios cruzados salen por hechos del proceso.
