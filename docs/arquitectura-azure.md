# Arquitectura objetivo en Azure

La demo no incluye clientes obligatorios de Azure. Las piezas locales se diseñaron para sustituirse:

| Función | Hoy | Evolución |
|---|---|---|
| Identidad | Sesión local | Microsoft Entra ID |
| Base | SQLite | Azure SQL Database |
| Documentos | Disco local | Azure Blob Storage |
| Eventos | Outbox local | Azure Service Bus |
| API | Proceso local | Azure Container Apps |
| Observación | Bitácora y auditoría | Azure Monitor |
| Secretos | Configuración de desarrollo | Azure Key Vault |
| Asistencia | Motor local | Azure OpenAI, si se autoriza |
| Integraciones | Adaptadores simulados | API Management, Functions, X-Road |

## De SQLite a Azure SQL

No es un cambio automático. Hay que:

1. Cambiar `Database:Provider` a `SqlServer` y las cadenas de conexión.
2. Generar migraciones de EF Core sobre SQL Server (tipos, índices, collations).
3. Revisar `DateTimeOffset` (en SQLite se guarda como binario para poder ordenar).
4. Probar concurrencia, tamaños y copias de seguridad.
5. Cargar datos con un procedimiento de ensayo, no con el generador de demostración.

Interfaces previstas para el reemplazo: almacenamiento de archivos, identidad, mensajería, fuentes externas y generación de borradores.
