# Especificación: módulo 36 — Control de Calidad de Insumos (frontend)

## 1. Objetivo

Habilitar la pantalla de Control de Calidad de Insumos en Manufactura (`/modulos/36-control-calidad-insumos`, que hoy muestra «Pantalla pendiente»). El usuario podrá consultar las inspecciones de lotes de insumos, registrar y editar una inspección pendiente, dictaminarla como aprobada o rechazada viendo si supera la tolerancia, anularla con motivo y ver la tasa de rechazo por proveedor.

Responsable: Antony Huayhua. Estado: aprobada por Lukas (PR #76, fusionado el 2026-11-24).

La implementación consume el [contrato del backend del módulo 36](modulo36-control-calidad-insumos-backend.md) y, para elegir producto y proveedor, los listados de los módulos 21 y 11. Las pruebas de la pantalla simulan la API; la prueba con la API real (A5) requiere el backend del 36 en el entorno de desarrollo.

## 2. Fuera de alcance

- Recepción de mercadería, lotes, bloqueo de stock y planes de muestreo.
- Cambios al contrato HTTP, al esquema de base de datos o a la autenticación y los permisos existentes.
- Cambios a otras pantallas, a los componentes compartidos, al cliente HTTP compartido (`frontend/src/api/client.ts`) y a los estilos globales.

**Decisiones de producto que requieren aprobación:** ninguna además de las de la spec del backend.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `frontend/src/modules/control-calidad-insumos/tipos.ts` | Definir los estados, resultados, filtros y tipos del contrato HTTP. |
| `frontend/src/modules/control-calidad-insumos/api.ts` y `api.test.ts` | Implementar y probar las siete operaciones del módulo y los catálogos de productos y proveedores con el cliente HTTP compartido. |
| `frontend/src/modules/control-calidad-insumos/validacion.ts` | Validar localmente el guardado, el dictamen y la anulación con los mensajes literales del backend, y calcular el porcentaje de defectos. |
| `frontend/src/modules/control-calidad-insumos/ControlCalidadInsumosPage.tsx` y `ControlCalidadInsumosPage.test.tsx` | Coordinar resumen, filtros, listado, detalle, formulario y diálogos. Probar los flujos. |
| `frontend/src/modules/control-calidad-insumos/InspeccionesInsumosResumen.tsx`, `InspeccionesInsumosFiltros.tsx`, `InspeccionesInsumosTabla.tsx`, `InspeccionInsumoDetallePanel.tsx`, `InspeccionInsumoFormulario.tsx`, `DictamenInsumoDialog.tsx` y `AnularInspeccionInsumoDialog.tsx` | Separar cada parte de la pantalla (≤ ~300 líneas por archivo). |
| `frontend/src/modules/pantallas.ts` | Registrar la pantalla con la clave `36-control-calidad-insumos` del catálogo, al final del objeto. |
| `frontend/src/styles/app.css` | Añadir al final un bloque con el prefijo `control-calidad-insumos` que use solo los tokens existentes. |

## 4. Diseño y lógica

### Resumen, filtros y listado

- Usar `PaginaModulo`, `TablaPaginada` y `PanelDetalle`. El resumen (`GET /inspecciones-insumos/resumen`) muestra el total, las inspecciones por estado, la tasa de rechazo global y una tabla por proveedor (aprobadas, rechazadas y tasa), ordenada como la devuelve la API.
- Filtros combinables: búsqueda por número o lote (espera de 300 ms), producto y proveedor (selectores de búsqueda), estado («Todos», «Pendiente», «Aprobada», «Rechazada», «Anulada») y rango de fechas. Si `desde` es posterior a `hasta` se muestra «La fecha desde no puede ser posterior a la fecha hasta.» y no se consulta. Al cambiar un filtro se vuelve a la página 1. Al cargar, el listado se pide **una sola vez**.
- La tabla muestra número, insumo, proveedor, lote, fecha, cantidad recibida con su unidad, estado con texto y % de defectos («—» sin dictamen). Tamaño de página 20, orden de la API. Cifras, fechas y estados en una sola línea (`white-space: nowrap`, `font-variant-numeric: tabular-nums`). Estados de carga, vacío y error con reintento.

### Detalle, formulario y transiciones

- El detalle muestra todos los campos. Con dictamen muestra el % de defectos frente a la tolerancia y el texto «Supera la tolerancia» o «Dentro de la tolerancia», no solo el color. Si está anulada, el motivo.
- **Nueva inspección** y **Editar** (solo `Pendiente`) abren el formulario: producto (selector en `GET /productos?busqueda=`), proveedor (selector en `GET /proveedores?busqueda=&activo=true`), lote (se muestra en mayúsculas al escribir), fecha de inspección (por defecto hoy en hora de Perú), cantidad recibida, muestra, unidad, tolerancia en % e inspector.
- **Dictaminar** (solo `Pendiente`) abre `DictamenInsumoDialog` con la cantidad defectuosa, el resultado («Aprobada» o «Rechazada») y observaciones. Mientras se escribe, el diálogo calcula el % de defectos y avisa si supera la tolerancia; en ese caso la opción «Aprobada» queda deshabilitada con la explicación visible. El backend vuelve a validarlo.
- **Anular** (solo `Pendiente`) abre `AnularInspeccionInsumoDialog` con el motivo.
- Los diálogos usan `role="dialog"`, `aria-modal="true"`, trampa de foco y cierre con Escape sin enviar la petición. Al cerrar, el foco vuelve al botón de origen; si desaparece tras confirmar, va al encabezado del detalle.
- Usar `CampoConError` con `aria-describedby` y `aria-invalid`. Si hay errores locales no se llama a la API. Ante un rechazo de la API se conservan los valores escritos y el mensaje se muestra con `role="alert"`. Deshabilitar el envío mientras la operación está en curso. Tras el éxito se refrescan listado, resumen y detalle.

### Contrato HTTP

El cliente compartido gestiona la autenticación y se usa sin cambios.

| Operación | Petición bajo `/api/v1` | Datos |
|---|---|---|
| Listar | `GET /inspecciones-insumos` | `busqueda`, `productoId`, `proveedorId`, `estado`, `desde`, `hasta`, `pagina` y `tamanoPagina`. Omitir los filtros vacíos. |
| Resumen | `GET /inspecciones-insumos/resumen` | — |
| Consultar | `GET /inspecciones-insumos/{id}` | — |
| Crear | `POST /inspecciones-insumos` | `{ productoId, proveedorId, lote, fechaInspeccion, cantidadRecibida, cantidadMuestra, unidadMedida, toleranciaDefectosPorcentaje, inspector }`. |
| Editar | `PUT /inspecciones-insumos/{id}` | Mismo cuerpo que crear. |
| Dictaminar | `POST /inspecciones-insumos/{id}/dictaminar` | `{ resultado, cantidadDefectuosa, observaciones? }`. |
| Anular | `POST /inspecciones-insumos/{id}/anular` | `{ motivo }` recortado. |
| Productos | `GET /productos` | `busqueda`, `pagina=1`, `tamanoPagina=20`. |
| Proveedores activos | `GET /proveedores` | `busqueda`, `activo=true`, `pagina=1`, `tamanoPagina=20`. |

Las fechas se envían como día (`2026-12-01`), sin hora. Los textos se envían recortados.

### Validaciones locales y mensajes

Mismos mensajes literales que el backend:

| Condición | Mensaje |
|---|---|
| Producto sin seleccionar | El identificador del producto debe ser mayor que 0. |
| Proveedor sin seleccionar | El identificador del proveedor debe ser mayor que 0. |
| Lote vacío / de más de 50 caracteres | El lote es obligatorio. / El lote no puede superar 50 caracteres. |
| Fecha vacía / posterior a hoy | La fecha de inspección es obligatoria. / La fecha de inspección no puede ser posterior a hoy. |
| Cantidad recibida fuera de rango / con más de 3 decimales | La cantidad recibida debe ser mayor que 0 y como máximo 1000000000. / La cantidad recibida admite como máximo 3 decimales. |
| Muestra ≤ 0 / con más de 3 decimales / mayor que lo recibido | La cantidad de la muestra debe ser mayor que 0. / La cantidad de la muestra admite como máximo 3 decimales. / La muestra no puede superar la cantidad recibida. |
| Unidad vacía / de más de 20 caracteres | La unidad de medida es obligatoria. / La unidad de medida no puede superar 20 caracteres. |
| Tolerancia fuera de 0–100 / con más de 2 decimales | La tolerancia de defectos debe estar entre 0 y 100. / La tolerancia de defectos admite como máximo 2 decimales. |
| Inspector vacío / de más de 100 caracteres | El inspector es obligatorio. / El inspector no puede superar 100 caracteres. |
| Defectuosa negativa / con más de 3 decimales / mayor que la muestra | La cantidad defectuosa no puede ser negativa. / La cantidad defectuosa admite como máximo 3 decimales. / La cantidad defectuosa no puede superar la muestra. |
| Observaciones de más de 500 caracteres | Las observaciones no pueden superar 500 caracteres. |
| Motivo vacío / de más de 250 caracteres | El motivo de anulación es obligatorio. / El motivo de anulación no puede superar 250 caracteres. |

«Hoy» es el día en hora de Perú (UTC−5, sin horario de verano), igual que en el backend: se calcula restando 5 horas al instante UTC y tomando `yyyy-MM-dd`, no con la fecha local del navegador ni con `toISOString()`, que da el día UTC y a partir de las 19:00 en Perú ya es mañana. Se usa para el valor por defecto del campo y para la validación local.

Los mensajes de proveedor inactivo, lote repetido, tolerancia superada y conflictos de estado se muestran tal como los devuelve la API.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `npm --prefix frontend run verify` y `./scripts/verify.sh` | Ambos en verde y sin advertencias. |
| A2 | Pruebas del cliente HTTP (`api.test.ts`) | Cubren las nueve peticiones de §4 con URL, método, query sin filtros vacíos y cuerpos exactos. |
| A3 | Pruebas de resumen, filtros y listado | Carga inicial con `esperarCargaInicial…` y una sola petición del listado; espera de 300 ms con temporizadores falsos; combinación de filtros; reinicio de página; rango de fechas inválido sin petición; estados de carga, vacío y error; tabla por proveedor. |
| A4 | Pruebas de detalle, formulario y diálogos | Cada validación local sin petición; edición solo en `Pendiente`; conservación del formulario ante un 409 (lote repetido); cálculo del % en el dictamen y «Aprobada» deshabilitada sobre la tolerancia; anulación con motivo; Escape sin petición y devolución del foco. La suite pasa 10 veces seguidas. |
| A5 | Prueba con API real y regresión | La ruta abre desde el catálogo. Con sesión iniciada se crea, edita, dictamina y anula una inspección. Las pruebas existentes pasan. |
| A6 | Control visual | Listado, detalle, formulario y diálogos a 375 px y 1280 px en modo claro y oscuro, sin desbordamiento horizontal de la página. Capturas adjuntas al PR. |

## 6. Checklist de ejecución

- [x] Aprobación de Lukas sobre el alcance y el diseño (PR #76, 2026-11-24).
- [ ] Backend del 36 integrado en `main` (PR #89), y rama actualizada.
- [x] Commit 2: tipos, cliente HTTP y sus pruebas.
- [x] Commit 3: pantalla, componentes, registro en `pantallas.ts`, estilos y pruebas de la página.
- [ ] Verificar A1–A6 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-25 | A1 | En verde, sin advertencias | `npm --prefix frontend run verify` y `./scripts/verify.sh` (1337 pruebas de frontend) |
| 2026-11-25 | A2 | Las nueve peticiones con URL, método, query sin filtros vacíos y cuerpos exactos | `api.test.ts` (6 pruebas) |
| 2026-11-25 | A3 | Carga inicial con una sola petición, espera de 300 ms con temporizadores falsos, filtros combinados, reinicio de página, rango invertido sin petición, vacío, error con reintento y tabla por proveedor | `ControlCalidadInsumosPage.test.tsx` |
| 2026-11-25 | A4 | Validación local sin petición, «hoy» en hora de Perú a las 19:00, edición solo en `Pendiente`, 409 que conserva el formulario, porcentaje del dictamen con «Aprobada» deshabilitada, anulación, Escape y foco. 10 de 10 ejecuciones seguidas en verde | `ControlCalidadInsumosFormulario.test.tsx` |
| — | A5 | Pendiente: necesita el backend del PR #89 en el entorno | — |
| — | A6 | Pendiente: capturas a 375 px y 1280 px en claro y oscuro | — |
