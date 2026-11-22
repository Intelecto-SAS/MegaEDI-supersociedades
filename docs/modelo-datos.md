# Modelo de datos

Una fuente por dato. Las entidades principales coinciden con el núcleo pedido: usuario, rol, permiso, dependencia, región, sociedad, tercero, asignación, expediente, radicado, documento y versión, actuación, notificación y acuses, acuerdo y cuotas, reporte financiero, alerta, contribución, pago, sanción, auxiliar y designación, contrato y póliza, calidad, fuentes externas, informes, hechos del proceso, auditoría, lote de migración y dueño de cada capacidad.

Las relaciones se guardan con integridad referencial. Las operaciones compuestas van en transacción. Una fusión de terceros reasigna vínculos; una migración no borra el heredado.
