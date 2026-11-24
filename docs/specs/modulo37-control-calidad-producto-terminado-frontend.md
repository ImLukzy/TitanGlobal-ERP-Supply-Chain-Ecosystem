# Especificación: módulo 37 — Control de Calidad de Producto Terminado (frontend)

## 1. Objetivo

Habilitar la pantalla de Control de Calidad de Producto Terminado en Manufactura (`/modulos/37-control-calidad-producto-terminado`, que hoy muestra «Pantalla pendiente»). El usuario podrá consultar las inspecciones de las órdenes terminadas, registrar y editar una inspección pendiente, dictaminarla como aprobada o rechazada viendo si supera la tolerancia, anularla con motivo y ver el avance de inspección de cada orden.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

La implementación consume el [contrato del backend del módulo 37](modulo37-control-calidad-producto-terminado-backend.md) y, para elegir la orden, el listado del módulo 34 (`GET /ordenes-produccion`). Las pruebas de la pantalla simulan la API; la prueba con la API real (A5) requiere ambos backends en el entorno de desarrollo.

## 2. Fuera de alcance

- Ingreso a almacén, bloqueo de stock, registro de mermas y planes de muestreo.
- Cambios al contrato HTTP, al esquema de base de datos o a la autenticación y los permisos existentes.
- Cambios a otras pantallas, a los componentes compartidos, al cliente HTTP compartido (`frontend/src/api/client.ts`) y a los estilos globales.

**Decisiones de producto que requieren aprobación:** ninguna además de las de la spec del backend.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `frontend/src/modules/control-calidad-producto-terminado/tipos.ts` | Definir los estados, resultados, filtros y tipos del contrato HTTP. |
| `frontend/src/modules/control-calidad-producto-terminado/api.ts` y `api.test.ts` | Implementar y probar las ocho operaciones del módulo y la búsqueda de órdenes terminadas con el cliente HTTP compartido. |
| `frontend/src/modules/control-calidad-producto-terminado/validacion.ts` | Validar localmente el guardado, el dictamen y la anulación con los mensajes literales del backend, y calcular el porcentaje de no conformes. |
| `frontend/src/modules/control-calidad-producto-terminado/ControlCalidadProductoTerminadoPage.tsx` y `ControlCalidadProductoTerminadoPage.test.tsx` | Coordinar resumen, filtros, listado, detalle, avance, formulario y diálogos. Probar los flujos. |
| `frontend/src/modules/control-calidad-producto-terminado/InspeccionesProductoResumen.tsx`, `InspeccionesProductoFiltros.tsx`, `InspeccionesProductoTabla.tsx`, `InspeccionProductoDetallePanel.tsx`, `AvanceInspeccionOrdenPanel.tsx`, `InspeccionProductoFormulario.tsx`, `DictamenProductoDialog.tsx` y `AnularInspeccionProductoDialog.tsx` | Separar cada parte de la pantalla (≤ ~300 líneas por archivo). |
| `frontend/src/modules/pantallas.ts` | Registrar la pantalla con la clave `37-control-calidad-producto-terminado` del catálogo, al final del objeto. |
| `frontend/src/styles/app.css` | Añadir al final un bloque con el prefijo `control-calidad-producto` que use solo los tokens existentes. |

## 4. Diseño y lógica

### Resumen, filtros y listado

- Usar `PaginaModulo`, `TablaPaginada` y `PanelDetalle`. El resumen (`GET /inspecciones-producto-terminado/resumen`) muestra el total, las inspecciones por estado, la tasa de rechazo, las unidades inspeccionadas y no conformes, y las órdenes terminadas sin inspección.
- Filtros combinables: búsqueda por número de inspección u orden (espera de 300 ms), orden de producción y producto (selectores de búsqueda), estado («Todos», «Pendiente», «Aprobada», «Rechazada», «Anulada») y rango de fechas. Si `desde` es posterior a `hasta` se muestra «La fecha desde no puede ser posterior a la fecha hasta.» y no se consulta. Al cambiar un filtro se vuelve a la página 1. Al cargar, el listado se pide **una sola vez**.
- La tabla muestra número, orden, producto, fecha, unidades inspeccionadas, estado con texto y % no conforme («—» sin dictamen). Tamaño de página 20, orden de la API. Cifras, fechas y estados en una sola línea (`white-space: nowrap`, `font-variant-numeric: tabular-nums`). Estados de carga, vacío y error con reintento.

### Detalle y avance de la orden

- El detalle muestra todos los campos. Con dictamen muestra unidades conformes y no conformes, el % frente a la tolerancia y el texto «Supera la tolerancia» o «Dentro de la tolerancia», no solo el color. Si está anulada, el motivo.
- El panel de **avance de la orden** (`GET /inspecciones-producto-terminado/ordenes/{id}/avance`) muestra cantidad producida, unidades inspeccionadas, por inspeccionar, conformes aprobadas y no conformes, con una barra de progreso accesible (`role="progressbar"` con `aria-valuenow`, `aria-valuemin` y `aria-valuemax`). Si cambia la selección durante una consulta, el panel muestra solo el resultado de la selección vigente.

### Formulario y transiciones

- **Nueva inspección**: se elige una orden `Terminada` en el selector (`GET /ordenes-produccion?estado=Terminada&busqueda=`). Al elegirla se carga su avance y se muestran las unidades por inspeccionar como ayuda del campo. Se indican fecha de inspección (por defecto hoy en hora de Lima), unidades inspeccionadas, tolerancia en % e inspector.
- **Editar** (solo `Pendiente`) muestra la orden como dato de lectura y permite cambiar el resto.
- **Dictaminar** (solo `Pendiente`) abre `DictamenProductoDialog` con las unidades no conformes, el resultado («Aprobada» o «Rechazada») y observaciones. Mientras se escribe, el diálogo calcula el % y avisa si supera la tolerancia; en ese caso la opción «Aprobada» queda deshabilitada con la explicación visible. El backend vuelve a validarlo.
- **Anular** (solo `Pendiente`) abre `AnularInspeccionProductoDialog` con el motivo.
- Los diálogos usan `role="dialog"`, `aria-modal="true"`, trampa de foco y cierre con Escape sin enviar la petición. Al cerrar, el foco vuelve al botón de origen; si desaparece tras confirmar, va al encabezado del detalle.
- Usar `CampoConError` con `aria-describedby` y `aria-invalid`. Si hay errores locales no se llama a la API. Ante un rechazo de la API se conservan los valores escritos y el mensaje se muestra con `role="alert"`. Deshabilitar el envío mientras la operación está en curso. Tras el éxito se refrescan listado, resumen, detalle y avance.

### Contrato HTTP

El cliente compartido gestiona la autenticación y se usa sin cambios.

| Operación | Petición bajo `/api/v1` | Datos |
|---|---|---|
| Listar | `GET /inspecciones-producto-terminado` | `busqueda`, `ordenProduccionId`, `productoId`, `estado`, `desde`, `hasta`, `pagina` y `tamanoPagina`. Omitir los filtros vacíos. |
| Resumen | `GET /inspecciones-producto-terminado/resumen` | — |
| Avance | `GET /inspecciones-producto-terminado/ordenes/{ordenProduccionId}/avance` | — |
| Consultar | `GET /inspecciones-producto-terminado/{id}` | — |
| Crear | `POST /inspecciones-producto-terminado` | `{ ordenProduccionId, fechaInspeccion, cantidadInspeccionada, toleranciaNoConformesPorcentaje, inspector }`. |
| Editar | `PUT /inspecciones-producto-terminado/{id}` | `{ fechaInspeccion, cantidadInspeccionada, toleranciaNoConformesPorcentaje, inspector }`. |
| Dictaminar | `POST /inspecciones-producto-terminado/{id}/dictaminar` | `{ resultado, cantidadNoConforme, observaciones? }`. |
| Anular | `POST /inspecciones-producto-terminado/{id}/anular` | `{ motivo }` recortado. |
| Órdenes terminadas | `GET /ordenes-produccion` | `estado=Terminada`, `busqueda`, `pagina=1`, `tamanoPagina=20`. |
| Productos (filtro) | `GET /productos` | `busqueda`, `pagina=1`, `tamanoPagina=20`. |

Las fechas se envían como día (`2026-12-01`), sin hora. Los textos se envían recortados.

### Validaciones locales y mensajes

Mismos mensajes literales que el backend:

| Condición | Mensaje |
|---|---|
| Orden sin seleccionar | El identificador de la orden de producción debe ser mayor que 0. |
| Fecha vacía / posterior a hoy | La fecha de inspección es obligatoria. / La fecha de inspección no puede ser posterior a hoy. |

«Hoy» es el día en hora de Lima (UTC−5, sin horario de verano), igual que en el backend: se calcula restando 5 horas al instante UTC y tomando `yyyy-MM-dd`, no con la fecha local del navegador ni con `toISOString()`, que da el día UTC y a partir de las 19:00 de Lima ya es mañana. Se usa para el valor por defecto del campo y para la validación local.
| Unidades inspeccionadas vacías, no enteras o fuera de 1–1000000 | La cantidad inspeccionada debe estar entre 1 y 1000000. |
| Tolerancia fuera de 0–100 / con más de 2 decimales | La tolerancia de no conformes debe estar entre 0 y 100. / La tolerancia de no conformes admite como máximo 2 decimales. |
| Inspector vacío / de más de 100 caracteres | El inspector es obligatorio. / El inspector no puede superar 100 caracteres. |
| No conformes negativas o no enteras / mayores que las inspeccionadas | Las unidades no conformes no pueden ser negativas. / Las unidades no conformes no pueden superar las inspeccionadas. |
| Observaciones de más de 500 caracteres | Las observaciones no pueden superar 500 caracteres. |
| Motivo vacío / de más de 250 caracteres | El motivo de anulación es obligatorio. / El motivo de anulación no puede superar 250 caracteres. |

Los mensajes de orden no terminada, fecha anterior al fin de la orden, unidades por inspeccionar insuficientes, tolerancia superada y conflictos de estado se muestran tal como los devuelve la API.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `npm --prefix frontend run verify` y `./scripts/verify.sh` | Ambos en verde y sin advertencias. |
| A2 | Pruebas del cliente HTTP (`api.test.ts`) | Cubren las diez peticiones de §4 con URL, método, query sin filtros vacíos y cuerpos exactos. |
| A3 | Pruebas de resumen, filtros y listado | Carga inicial con `esperarCargaInicial…` y una sola petición del listado; espera de 300 ms con temporizadores falsos; combinación de filtros; reinicio de página; rango de fechas inválido sin petición; estados de carga, vacío y error. |
| A4 | Pruebas de detalle, avance, formulario y diálogos | Avance con barra de progreso accesible; cada validación local sin petición; edición solo en `Pendiente`; conservación del formulario ante un 409 (unidades insuficientes); cálculo del % en el dictamen y «Aprobada» deshabilitada sobre la tolerancia; anulación con motivo; Escape sin petición y devolución del foco. La suite pasa 10 veces seguidas. |
| A5 | Prueba con API real y regresión | La ruta abre desde el catálogo. Con sesión iniciada se crea, edita, dictamina y anula una inspección de una orden terminada, y el avance se actualiza. Las pruebas existentes pasan. |
| A6 | Control visual | Listado, detalle, avance, formulario y diálogos a 375 px y 1280 px en modo claro y oscuro, sin desbordamiento horizontal de la página. Capturas adjuntas al PR. |

## 6. Checklist de ejecución

- [ ] Aprobación de Lukas sobre el alcance y el diseño.
- [ ] Backend del 37 integrado en `main`, y rama actualizada.
- [ ] Commit 2: tipos, cliente HTTP y sus pruebas.
- [ ] Commit 3: pantalla, componentes, registro en `pantallas.ts`, estilos y pruebas de la página.
- [ ] Verificar A1–A6 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
