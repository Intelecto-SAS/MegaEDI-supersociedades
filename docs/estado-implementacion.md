# Estado de implementación

Fuente del informe: `docs/fuentes/Informe_gerencial_EDI_esquema_integral_v1.docx` (presente).

Leyenda: **Documento** = requisito del informe. **Demo** = capacidad construida para la presentación. **Pendiente de validación** = E6 y E7.

| Código | Módulo | Origen | Profundidad en la demo |
|---|---|---|---|
| E1 | Portal e identidad | Documento | Inicio de sesión interno por perfil, menú según permisos |
| E2 | Tableros por líder | Documento | Indicadores calculados, filtros, detalle |
| E3 | Integración y eventos | Documento | Hechos del proceso, bandeja de entregas, reintentos |
| E4 | Seguridad y operación | Documento | Permisos en API, auditoría, incidentes, descarga controlada |
| E5 | XBRL propio | Documento | Cargue y ecuación patrimonial; no es validador oficial |
| E6 | Gestión contractual | Pendiente de validación | Ciclo póliza → acta → pago con bloqueos |
| E7 | Informes especializados | Pendiente de validación | Nueve informes, borrador local, PDF |
| D1 | Sociedades | Documento | Ficha 360 actualizada por eventos |
| D2 | Terceros | Documento | Alta, duplicados, fusión humana |
| D3 | Asignaciones | Documento | Responsable de dependencia y funcionario |
| D4 | Parámetros | Documento | Calendario, UVT, términos |
| D5 | Información financiera | Documento | Cifras únicas para riesgo y tableros |
| D6 | Alertas | Documento | Motor de reglas, revisión humana |
| D7 | Cartera | Documento | Obligaciones, mora, pagos |
| D8 | Sanciones | Documento | Consulta y firmeza; sin cambio libre desde supervisión |
| D9 | Calidad | Documento | Reglas e incidencias |
| D10 | Analítica | Documento | Agregados internos, sin publicación |
| D11 | Interoperabilidad | Documento | 23 filas con adaptador simulado |
| J1 | Radicación | Documento | Flujo completo con tres fechas y respuesta vinculada |
| J2 | Expediente | Documento | Actuaciones, documentos, agenda, reservas |
| J3 | Insolvencia | Documento | Acuerdo, cuotas, incumplimiento, cierre con desenlace |
| J4 | Auxiliares | Documento | Bloqueo por inconsistencia y designación |
| J5 | Páginas de procesos | Documento | Páginas locales correctas, incompletas y desactualizadas |
| J6 | Trámites societarios | Documento | Tres aprobaciones y cierre |

## Pruebas

Automatizadas (xUnit): inicio de sesión, cambio de perfil, restricción de radicador y abogado, radicado único, reservas, PDF.

Manuales de presentación: los ocho recorridos del centro de demostración.

No ejecutado en este entregable: Playwright de interfaz. La verificación visual se hace en el navegador de la demo.
