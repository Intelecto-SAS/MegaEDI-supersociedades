# EDI 360

Plataforma de demostración del **Ecosistema Digital Integrado** de la Superintendencia de Sociedades.

El entorno es local, con datos y funcionarios ficticios. No se conecta a sistemas institucionales ni a Azure.

## Cómo iniciar la demostración

Hace falta **.NET 10** y **Node.js 22**. La interfaz sola no basta: sin el servidor las pantallas salen en blanco.

**Windows:** haga doble clic en `scripts\start.cmd`. Se abren dos ventanas; no las cierre. Luego entre a [http://127.0.0.1:5173](http://127.0.0.1:5173).

En el equipo la demostración usa SQLite, aunque exista `DATABASE_URL` de Vercel. Esa variable solo aplica al publicar.

Si prefiere hacerlo a mano, primero el servidor y después la interfaz:

```bat
scripts\start-api.cmd
scripts\start-ui.cmd
```

**macOS / Linux:**

```bash
./scripts/dev.sh
```

Abra [http://127.0.0.1:5173](http://127.0.0.1:5173). Compruebe el servidor en [http://127.0.0.1:5080/api/salud](http://127.0.0.1:5080/api/salud).

La contraseña de todos los funcionarios de demostración es `demo`.

### Si el servidor no arranca

1. En una terminal: `dotnet --version`. Debe empezar por `10.`. Si no, instale el SDK: https://dotnet.microsoft.com/download/dotnet/10.0
2. Deje abierta la ventana de `start-api.cmd` y lea el error (falta SDK, puerto 5080 ocupado, etc.).
3. No abra solo la carpeta `src/frontend`. Los datos viven en el servidor.

## Con qué perfil entrar

| Presentar… | Entre como |
|---|---|
| Vista de alta dirección | Camila Restrepo Duque |
| Correspondencia y contratos | Andrés Palacio Mejía |
| Radicación | Laura Jiménez Soto |
| Coordinación documental | Mauricio Peña Cárdenas |
| Tutelas | Beatriz Molina Ríos / Felipe Ardila Cruz |
| Insolvencia | Ricardo Sáenz Ocampo / Óscar Villamizar León |
| Supervisión de sociedades | Julián Herrera Nieto |
| Intendencia regional | Iván Cárdenas Luján |
| Informes | Claudia Bernal Ospina |
| Modernización GeDeSS y cambio de perfil | Valeria Santamaría López |

Cada funcionario ve un inicio distinto: bandeja operativa, carga del equipo, delegatura o tablero institucional.

La interfaz usa el color institucional `#962D46`, Nunito Sans y el escudo de la Superintendencia, según el manual de identidad visual.

## Recorrido de diez minutos

El guion está en `docs/guion-demo-gerencial.md`. En síntesis:

1. Entre como Valeria y prepare el escenario desde **Centro de demostración**.
2. Pase a Laura: radique una tutela. El motor local sugiere; una persona confirma.
3. Pase a Felipe y Beatriz: respuesta, revisión y salida simulada.
4. Pase a Ricardo: confirme un acuerdo y abra la ficha de la sociedad. La situación cambia; no se declara recuperación automática.
5. Pase a Julián: revise la alerta y registre una actuación.
6. Muestre **Modernización GeDeSS**: inventario, migración de prueba, conciliación y traslado de una capacidad. El histórico heredado se conserva.

## Qué es y qué no es

- Es una aplicación ejecutable, con persistencia en el servidor.
- El “motor de asistencia” usa reglas y plantillas locales. No es un modelo generativo ni un servicio de inteligencia artificial en la nube.
- E6 (contratos) y E7 (informes) son propuestas del informe gerencial, pendientes de validación institucional.
- GeDeSS en esta demo es un repositorio heredado simulado, no el sistema real.

Documentación de apoyo: `docs/`.

## Publicar en Vercel

En local los datos viven en archivos. En Vercel hay que conectar una base Neon para que radicados, expedientes y el cambio de funcionario sobrevivan.

1. En el proyecto de Vercel, **Root Directory vacío** (raíz del repo). Si queda en `src/frontend` solo sale la interfaz y `/api/salud` no responde.
2. **Storage → Create Database → Neon**, para que exista `DATABASE_URL`.
3. Redeploy. El build debe mostrar los servicios `frontend` y `backend`.
4. Compruebe `https://<su-dominio>/api/salud`: debe devolver `estado: ok` y `persistencia: compartida`.

Pasos completos en `docs/despliegue-vercel.md`.
