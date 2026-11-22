# Cómo publicar EDI 360 en Vercel

En el equipo el sistema guarda todo en archivos locales. En Vercel **no hay disco permanente**: si se deja SQLite, los radicados y las sesiones desaparecen cuando el servicio se apaga.

Para que se comporte como en local hace falta una base Postgres (Neon, desde el Marketplace de Vercel). La interfaz y los flujos no cambian: misma URL, mismos funcionarios, contraseña `demo`.

Los servicios combinados (interfaz + API) están en beta en Vercel.

## Una sola vez

1. Cuenta en [Vercel](https://vercel.com) con el repositorio GitHub conectado.
2. Importe el proyecto. Vercel lee `vercel.json`: la interfaz (Vite) y la API (.NET en contenedor) salen juntas.
3. En el proyecto: **Storage → Create Database → Neon**. Deje que Vercel cree `DATABASE_URL` (Production, Preview y Development).
4. Vuelva a desplegar.

Sin `DATABASE_URL` la demo abre, pero los datos no sobreviven al siguiente arranque.

## Cada vez

- Un `git push` a la rama genera una URL de vista previa.
- La rama de producción (por defecto `main`) publica el entorno estable.

La dirección es una sola: `/api` va a la API y el resto a la interfaz. La sesión en cookie funciona igual que en local.

## Primera visita

La primera petición crea las tablas y carga los datos ficticios. Puede tardar medio minuto. Las siguientes son rápidas.

Si cambia de funcionario, radica, confirma un acuerdo o restablece el escenario, eso queda en Neon: otro navegador ve lo mismo.

## Qué no hace falta

- Docker en su computador para el recorrido local (`./scripts/dev.sh` sigue igual).
- Azure, Entra ID ni un modelo de IA en la nube.
- Un dominio propio (puede usar el `*.vercel.app`).

## Variables

| Nombre | Para qué |
|---|---|
| `DATABASE_URL` | La crea Neon. Activa Postgres y documentos persistentes. |
| `PORT` | La pone Vercel. La API escucha ahí. |
| `CRON_SECRET` | Opcional. Si existe, `/api/eventos/procesar` exige `Authorization: Bearer …`. |

No copie cadenas de conexión en el código ni en capturas de la presentación.

## Si algo falla

- **Entra y al recargar pide de nuevo la clave.** Falta Neon, o el despliegue anterior no tenía `DATABASE_URL`.
- **404 en una ruta como `/documental` al recargar.** El servicio de interfaz debe reescribir a `index.html` (ya está en `vercel.json`).
- **La API no responde.** En Logs del despliegue, el contenedor debe escuchar el puerto de Vercel.
- **Error al crear tablas.** Revise que `DATABASE_URL` tenga `sslmode=require` (Neon lo trae).

Local no usa estas variables: sigue en SQLite.
