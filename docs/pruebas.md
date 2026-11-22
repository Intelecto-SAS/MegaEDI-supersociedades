# Pruebas

Herramienta: xUnit contra la API.

Cubierto:

- Inicio de sesión.
- Cambio de funcionario solo con perfil de demostración.
- Un radicador no registra providencia.
- Un abogado no entra a contratación/pagos.
- Radicado con número único y persistente; tutela identificada.
- Intendencia y TIC no ven expedientes reservados.
- Borrador de informe y exportación PDF.

Pendiente de automatizar en navegador (Playwright): los ocho recorridos de pantalla. Deben ejecutarse con el guion gerencial.

Los datos sobreviven al reinicio: quedan en `data/*.db` y en el almacenamiento de archivos.
