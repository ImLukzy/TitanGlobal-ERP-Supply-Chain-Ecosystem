# Propuesta: módulo 46 — Monitoreo de entregas (frontend)

## 1. Objetivo
Pantalla de Kelvin `/modulos/46-monitoreo-entregas`, según contrato backend propuesto. Programar despachos, marcar salida, registrar POD manual, cancelar y consultar historial. Continuación local autorizada por Kelvin; revisión de Lukas pendiente, sin integración ni despliegue.

## 2. Fuera de alcance
Aplican las decisiones backend: sin GPS, mapas, incidencias/reintentos, inventario, adjuntos ni certificación del POD. Mostrar `Seguimiento manual · Constancia de recepción textual`. No abrir referencias como enlaces. Sin cambios en componentes/pantallas ajenos ni nuevas dependencias.

## 3. Archivos
Carpeta `frontend/src/modules/monitoreo-entregas/`: tipos.ts, api.ts/api.test.ts, validacion.ts/validacion.test.ts, presentacion.ts, useEntregasData.ts, MonitoreoEntregasPage.tsx, EntregasResumen.tsx, EntregasFiltros.tsx, EntregasTabla.tsx, EntregaDetallePanel.tsx, EntregaFormulario.tsx, DialogoEntrega.tsx (contenedor accesible propio), DialogoPodEntrega.tsx, DialogoCancelarEntrega.tsx y DialogoSalidaEntrega.tsx. Pruebas temáticas EntregasConsultas, EntregasFormulario y EntregasEstados, utilidades entregas.testUtils.tsx. Añadir solo registro46 en pantallas.ts y bloque final `.monitoreo-entregas-` en app.css. Evidencia en docs/evidencias/modulo46/.

## 4. Diseño y lógica
- PaginaModulo, TablaPaginada, PanelDetalle, CampoConError y DialogoBaja compartidos. Tokens semánticos, métricas con cifra grande/etiqueta debajo, estados con texto y color, foco visible.
- Resumen total/programadas/enRuta/entregadas/canceladas. Lista y resumen cargan una vez inicialmente; errores independientes y reintento. Búsqueda300ms, estado, tamaño10 y página1 al cambiar filtros; descartar respuestas antiguas y corregir página vacía tras baja.
- Tabla: código, referencia/destinatario, fecha programada UTC, estado y detalle. Códigos, fechas y cabeceras sin partir; selección homogénea. Scroll de tabla interno accesible.
- Detalle: datos programados, salida, POD y motivo/fechas de cancelación según corresponda. Historial de estados con fecha UTC, descripción y orden de registro; nunca editable directamente. No perder POD ni historial al refrescar.
- Formulario: campos backend, máximos/mensajes literales. Fecha/hora explícitamente UTC; convertir y editar sin depender del huso del navegador, preservando milisegundos. Primer error con aria-invalid/aria-describedby y foco, sin petición inválida. Mantener valores ante400/409.
- Programada permite editar/baja/salida/cancelación; EnRuta permite POD/cancelación; terminales solo lectura. Salida con confirmación accesible. POD con receptor/documento/referencia/observaciones y confirmación inequívoca; no prellenar como recepción realizada.
- Cancelación con motivo1–500, baja mediante DialogoBaja. Todos los diálogos con foco inicial, Tab/Shift+Tab atrapados, Escape sin escribir y retorno al disparador; durante petición bloquear cierre y doble envío. Un 409 permanece visible sin borrar campos.
- Escritorio1280: listado y detalle/formulario en dos columnas. Móvil375: apilado, márgenes mínimo16px, campos adaptados y scroll interno, sin overflow de página. Claro/oscuro y contraste≥4.5:1; comparar con pantalla integrada en ambos temas.

## 5. Aceptación
A1 cliente de nueve operaciones con URLs/payload/query exactos. A2 consultas, temporizadores falsos y esperarCargaInicialEntregas antes de mockClear. A3 alta/edición/UTC/límites, errores400/409, estados y bloqueo de doble envío. A4 POD, historial y accesibilidad de diálogos. A5 gate completo y frontend verify, diez repeticiones de suite propia más carga, sin omitir ni ampliar timeouts. A6 QA con API/PostgreSQL reales375/1280claro/oscuro; capturas de detalle/formulario/POD/salida/cancelación/baja, cifras/fechas sin partir y contraste medido.

## 6. Checklist
- [ ] Revisión de Lukas.
- [x] API y datos locales por scripts.
- [x] Pantalla y pruebas temáticas.
- [x] Estabilidad, gates y QA real documentados, con salvedades globales.

## 7. Registro
2026-11-21: implementación local; revisión de Lukas pendiente. 40 pruebas propias en cinco archivos, 583 globales, diez repeticiones más una bajo carga, gate salida0 y SMOKE OK. QA con API/PostgreSQL reales a375/1280claro/oscuro, 24 capturas propias y dos de referencia. [Evidencia y salvedades ESLint/Vite](../evidencias/modulo46/README.md). Sin integración ni despliegue.
