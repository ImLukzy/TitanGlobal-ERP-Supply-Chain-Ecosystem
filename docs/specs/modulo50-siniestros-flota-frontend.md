# Propuesta: módulo 50 — Siniestros de flota (frontend)

## 1. Objetivo
Pantalla Kelvin `/modulos/50-siniestros-flota` para registrar incidentes, referencias de póliza/reclamo y cierre o anulación. Continuación local autorizada como sus módulos anteriores; revisión de Lukas pendiente.

## 2. Fuera de alcance
Mismo alcance backend: referencias manuales, sin verificación de cobertura/vigencia ni pagos/contabilidad, conversiones, adjuntos o cambios en otros módulos. No abrir enlaces de datos ingresados ni presentar el monto reconocido como pagado. No agregar importes de monedas distintas.

## 3. Archivos
`frontend/src/modules/siniestros-flota/`: tipos, api/api.test, validacion/validacion.test, presentacion, useSiniestrosData, SiniestrosFlotaPage, SiniestrosResumen/Filtros/Tabla, SiniestroDetallePanel/Formulario, DialogoSiniestro, DialogoReclamo, DialogoCierre, DialogoAnularSiniestro, siniestros.testUtils y pruebas temáticas SiniestrosConsultas/Formulario/Estados. Registro propio en pantallas.ts, bloque `.siniestros-flota-` al final de app.css y evidencia propia en docs/evidencias/modulo50/.

## 4. Diseño y lógica
PaginaModulo, TablaPaginada, PanelDetalle, DialogoBaja y CampoConError; tokens y controles comunes. Cinco métricas de estados con cifras grandes/etiquetas debajo, sin totales monetarios. Búsqueda300ms, filtros estado/tipo, páginas10, reinicio al filtrar y corrección tras baja; cargas iniciales únicas, descarte de respuestas obsoletas y reintentos.

Tabla código, placa, tipo, lugar, incidente UTC y estado. Detalle lateral a1280/apilado a375, observaciones, póliza/aseguradora/referencia/moneda/monto y fechas de reclamo/cierre/anulación según etapa. Textos monetarios exactos del servidor sin Number para representación ni cálculos. Estado Registrado permite editar/baja/reclamar/anular, EnReclamo cerrar/anular, terminales solo lectura.

Formulario del incidente con fecha explícita UTC sin desplazamiento de huso ni pérdida de milisegundos, validaciones literales backend y textos largos con textarea. Diálogo de reclamo con cinco campos; cierre con resultado, importe y resolución; anulación con motivo. Cantidades editables como cadenas, máximo12 dígitos significativos; convertir a número de petición solo tras validar escala/rango. Valores de Aceptado/Rechazado visibles como palabras; no calcular una indemnización automáticamente.

Conservar formularios400/409, enfocar primer error con aria-invalid/describedby, bloquear doble envío y cierre mientras se guarda. Foco inicial, Tab/Shift+Tab atrapados, Escape sin mutar y regreso al disparador, conservando foco dentro al deshabilitar controles o fallar la petición.

375/1280 claro/oscuro, margen móvil≥16px, códigos/números/fechas sin partir, tabla con scroll interno y sin desbordamiento de página, fila seleccionada uniforme. Medir textos/enlaces≥4.5:1 y comparar escritorio con Contratos integrado en ambos temas. CSS solo propio.

## 5. Aceptación
A1 contratos HTTP de nueve operaciones. A2 debounce con reloj falso, cargas controladas, filtros/paginación y respuestas obsoletas. A3 límites, escala monetaria, fechaUTC y datos inválidos sin peticiones. A4 flujos, errores, foco, doble envío y baja. A5 gate, diez repeticiones propias y veinte con build simultáneo sin ampliar timeouts. A6 QA real API/PostgreSQL375/1280claro/oscuro, capturas detalle/formulario/reclamo/cierre/anulación/baja, contraste y referencia integrada.

## 6. Checklist
- [ ] Aprobación de Lukas.
- [ ] Backend y datos locales por scripts.
- [ ] Pantalla y pruebas.
- [ ] Gate, estabilidad y QA con evidencia.

## 7. Registro
2026-11-22: propuesta local autorizada; sin pruebas ni QA acreditados todavía. Revisión formal pendiente.
