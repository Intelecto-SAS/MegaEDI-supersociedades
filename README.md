# EDI 360

Plataforma de demostración del **Ecosistema Digital Integrado** de la Superintendencia de Sociedades.

El entorno es local, con datos y funcionarios ficticios. No se conecta a sistemas institucionales ni a Azure.

## Cómo iniciar la demostración

En un equipo con .NET 10 y Node.js 22:

```bash
./scripts/dev.sh
```

Abra [http://127.0.0.1:5173](http://127.0.0.1:5173).

La contraseña de todos los funcionarios de demostración es `demo`.

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

En local los datos viven en archivos. En Vercel hay que conectar una base Neon para que radicados, expedientes y el cambio de funcionario sobrevivan. Pasos en `docs/despliegue-vercel.md`.
