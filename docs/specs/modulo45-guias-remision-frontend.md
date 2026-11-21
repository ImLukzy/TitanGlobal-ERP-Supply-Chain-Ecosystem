# Propuesta: módulo 45 — Guías de remisión (frontend)

## 1. Objetivo

Pantalla de Kelvin Pinto en `/modulos/45-guias-remision` para crear documentos internos de transporte con mercancías, consultarlos, emitirlos internamente y anularlos conservando sus datos. Depende del contrato [backend](modulo45-guias-remision-backend.md), aún propuesto. Kelvin autoriza expresamente desarrollarlo y probarlo localmente el 2026-11-21; queda pendiente la revisión de Lukas del flujo normal de crear-pantalla.md §1.

## 2. Fuera de alcance

Las decisiones backend §2 siguen pendientes. Sin cambios en pantallas/componentes compartidos, login, dependencias, inventarios ni módulos ajenos. Sin PDF, XML, QR, firma o envío fiscal. La pantalla debe mostrar «Documento interno de transporte» para evitar atribuir validez tributaria o conexión con terceros. No presentar «Emitida» como aceptación de una autoridad.

## 3. Archivos previstos

| Ruta | Responsabilidad |
|---|---|
| `frontend/src/modules/guias-remision/tipos.ts` | Contratos alineados con los DTOs y OpenAPI |
| `frontend/src/modules/guias-remision/api.ts` y `api.test.ts` | Ocho operaciones mediante cliente compartido; query limpia y payload exacto |
| `frontend/src/modules/guias-remision/validacion.ts` y `validacion.test.ts` | Límites, escala y mensajes idénticos a backend |
| `frontend/src/modules/guias-remision/presentacion.ts` | Fechas UTC, número y decimales sin pérdida de precisión |
| `frontend/src/modules/guias-remision/useGuiasData.ts` | Lista, detalle, resumen, búsqueda y respuestas obsoletas |
| `frontend/src/modules/guias-remision/GuiasRemisionPage.tsx` | Coordinación y bloqueo de doble envío |
| `frontend/src/modules/guias-remision/GuiasResumen.tsx`, `GuiasFiltros.tsx`, `GuiasTabla.tsx` | Resumen, búsqueda/estado y paginación |
| `frontend/src/modules/guias-remision/GuiaDetallePanel.tsx` | Cabecera, mercancías, estados e historial UTC |
| `frontend/src/modules/guias-remision/GuiaFormulario.tsx` y `LineasGuiaEditor.tsx` | Cabecera y 1–100 líneas con claves de interfaz estables |
| `frontend/src/modules/guias-remision/DialogoAnularGuia.tsx` | Motivo obligatorio, foco atrapado y retorno |
| `frontend/src/modules/guias-remision/DialogoEmitirGuia.tsx` | Confirmación interna explícita, foco atrapado y retorno |
| `frontend/src/modules/guias-remision/guias.testUtils.tsx` | Respuestas controladas y esperarCargaInicialGuias |
| `frontend/src/modules/guias-remision/GuiasConsultas.test.tsx`, `GuiasFormulario.test.tsx`, `GuiasEstados.test.tsx` | Pruebas separadas por flujo, sin temporizadores reales |
| `frontend/src/modules/pantallas.ts` | Añadir exclusivamente registro de 45 |
| `frontend/src/styles/app.css` | Añadir bloque final `.guias-remision-`, sin alterar selectores compartidos |
| `docs/evidencias/modulo45/README.md` | Resultados reales y capturas tras ejecutar QA |

## 4. Diseño y lógica propuestos

- PaginaModulo 45; TablaPaginada para listado; PanelDetalle, CampoConError y DialogoBaja. Colores, estados, campos y superficies con tokens.css.
- Cuatro tarjetas: total/borradores/emitidas/anuladas, cifra grande y etiqueta debajo. Errores y reintentos independientes de lista, detalle y resumen.
- Búsqueda de serie/destinatario con debounce de 300 ms, filtro de estado y tamaño10. Página1 al cambiar filtros; orden del servidor; ninguna petición con filtros vacíos. Descartar respuestas antiguas y evitar doble carga inicial. Corregir página vacía al borrar el último registro.
- Tabla: serie/número, destinatario, inicio del traslado UTC, estado, peso bruto kg y ver detalle. Cifras, fechas, encabezados y unidades sin partir; scroll horizontal interno accesible si es necesario. Selección homogénea en todas las celdas.
- Detalle con remitente/destinatario/documentos, direcciones, motivo, transporte/conductor/placa, peso declarado, observaciones, subtabla de mercancías, fechas UTC y motivo de anulación. Mostrar cantidad/unidad por línea; no sumar unidades distintas.
- Borrador: editar, emitir internamente, eliminar y anular. Emitida: solo anular. Anulada: lectura. Emisión con confirmación accesible que explique su alcance interno; no simular generación ni descarga de un documento fiscal.
- Formulario: todos los campos y validaciones backend. Textos recortados al enviar; serie normalizada a mayúsculas; serie/número manuales. Fecha y hora etiquetadas explícitamente UTC, conversión que no dependa de la zona del navegador y que preserve instantes en edición. Peso/cantidad se editan como cadenas, admiten el punto decimal y nunca se redondean silenciosamente.
- Mercancías: 1–100 líneas, SKU, descripción, unidad de medida y cantidad. Agregar/quitar con foco en campo de línea; conservar claves de interfaz estables, sin enviarlas al servidor. Máximo tres decimales significativos en cantidad y dos en peso; mostrar los decimales de respuesta textuales sin convertir a Number para representarlos.
- Primer error local con aria-invalid, aria-describedby y foco, sin petición HTTP. Un error 400/409 permanece visible con role=alert conservando los valores. Bloqueo sincrónico de doble envío en operaciones; restablecer controles tras fallo.
- Baja mediante DialogoBaja. Anulación con motivo 1–500 y confirmación de emisión mediante diálogo accesible: foco inicial, Tab/Shift+Tab atrapados, Escape sin escritura y retorno al disparador. Durante petición, evitar cerrar o repetir; tras eliminar, foco al botón nuevo. Mantener la ficha en error de transición.
- 1280 px: lista y detalle/formulario en dos columnas. 375 px: apilado, margen mínimo16px, campos adaptados y scroll de tabla interno, sin desbordamiento de página. Claro/oscuro, contraste al menos4.5:1 y foco visible.

## 5. Criterios de aceptación

| # | Criterio | Comprobación |
|---|---|---|
| A1 | Contrato HTTP | Pruebas del cliente: ocho operaciones, URLs, payloads, auth compartida y filtros omitidos |
| A2 | Consultas estables | esperarCargaInicialGuias espera lista/resumen y exige una carga; debounce con fake timers; filtros, paginación y respuestas antiguas |
| A3 | Formulario sin pérdida de datos | Límites backend, fecha UTC en otra zona de navegador, 1/100 líneas, escala, 400/409, primer error y doble envío |
| A4 | Estados y accesibilidad | Emisión interna, anulación con historial, baja de borrador, terminales sin edición; Tab, Escape y retorno del foco |
| A5 | Gate y estabilidad | Frontend verify y gate completo en verde, sin omisiones ni ampliación de timeouts; suite propia diez veces y prueba con carga |
| A6 | QA con datos reales | 375/1280 claro/oscuro; crear/editar dos líneas, emitir, anular y borrar otro borrador; capturas, contraste y ausencia de overflow |

Comparar con una pantalla integrada de referencia en claro y oscuro. Capturas de detalle, formulario y diálogos únicamente tras QA contra API y PostgreSQL reales. Registrar resultados y advertencias, sin fabricar evidencia.

## 6. Checklist de ejecución

- [ ] Revisión de Lukas de este diseño y del contrato backend.
- [x] Backend, SQL, seed y API locales preparados según scripts.
- [x] Cliente, consultas, formulario y transiciones implementados.
- [x] Pruebas temáticas y diez repeticiones sin fallos, más una bajo carga.
- [x] Gates y QA real con evidencia; salvedad Vite documentada.

## 7. Registro

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-21 | Diseño | Propuesta pendiente de revisión | Este documento; asignación de Kelvin comprobada |
| 2026-11-21 | A1–A6 | Verificados localmente, con salvedades | 61 pruebas propias, 604 globales, diez repeticiones más una bajo carga, frontend verify final salida 0 y QA real 375/1280 claro/oscuro, 20 capturas propias y dos de referencia; [evidencia](../evidencias/modulo45/README.md). Revisión de Lukas y advertencia Vite pendientes |


2026-11-21: Kelvin autoriza expresamente continuar con esta propuesta como implementación local, igual que los módulos anteriores. Esta autorización permite desarrollar y probar en su rama; no sustituye la revisión de Lukas ni autoriza publicación, integración o despliegue.
