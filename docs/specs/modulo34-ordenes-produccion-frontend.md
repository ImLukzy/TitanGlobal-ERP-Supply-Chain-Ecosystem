# Especificación: módulo 34 — Órdenes de Producción (frontend)

## 1. Objetivo

Habilitar la pantalla de Órdenes de Producción en Manufactura (`/modulos/34-ordenes-produccion`, que hoy muestra «Pantalla pendiente»). El usuario podrá consultar las órdenes, registrar una orden para fabricar una cantidad de un producto, ver los materiales y el tiempo que requiere, editarla mientras está planificada y llevarla por sus estados: liberar, iniciar, terminar con la cantidad producida o cancelar con motivo.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

La implementación consume el [contrato del backend del módulo 34](modulo34-ordenes-produccion-backend.md) (PR #75) y, para elegir el producto, el listado del módulo 21. Las pruebas de la pantalla simulan la API; la prueba con la API real (A5) requiere el backend del 34 en el entorno de desarrollo, que a su vez depende de los módulos 31 y 32.

## 2. Fuera de alcance

- El consumo real de materiales (módulo 35), los controles de calidad (36 y 37), las mermas (38) y la programación de capacidad.
- La gestión de listas de materiales y rutas: la orden usa las activas del producto, que se administran en las pantallas del 31 y del 32.
- Cambios al contrato HTTP, al esquema de base de datos o a la autenticación y los permisos existentes.
- Cambios a otras pantallas, a los componentes compartidos, al cliente HTTP compartido (`frontend/src/api/client.ts`) y a los estilos globales.

**Decisiones de producto que requieren aprobación:** ninguna además de las de la spec del backend.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `frontend/src/modules/ordenes-produccion/tipos.ts` | Definir los estados, filtros y tipos del contrato HTTP. |
| `frontend/src/modules/ordenes-produccion/api.ts` y `api.test.ts` | Implementar y probar las nueve operaciones del módulo y el catálogo de productos con el cliente HTTP compartido. |
| `frontend/src/modules/ordenes-produccion/validacion.ts` | Validar localmente el guardado, el cierre y la cancelación con los mensajes literales del backend. |
| `frontend/src/modules/ordenes-produccion/OrdenesProduccionPage.tsx` y `OrdenesProduccionPage.test.tsx` | Coordinar resumen, filtros, listado, detalle, formulario y diálogos. Probar los flujos. |
| `frontend/src/modules/ordenes-produccion/OrdenesProduccionResumen.tsx`, `OrdenesProduccionFiltros.tsx`, `OrdenesProduccionTabla.tsx`, `OrdenProduccionDetallePanel.tsx`, `OrdenProduccionFormulario.tsx`, `TerminarOrdenDialog.tsx` y `CancelarOrdenDialog.tsx` | Separar cada parte de la pantalla (≤ ~300 líneas por archivo). |
| `frontend/src/modules/pantallas.ts` | Registrar la pantalla con la clave `34-ordenes-produccion` del catálogo, al final del objeto. |
| `frontend/src/styles/app.css` | Añadir al final un bloque con el prefijo `ordenes-produccion` que use solo los tokens existentes. |

## 4. Diseño y lógica

### Resumen, filtros y listado

- Usar `PaginaModulo`, `TablaPaginada` y `PanelDetalle`. El resumen (`GET /ordenes-produccion/resumen`) muestra el total, las órdenes por estado (en el orden que devuelve la API), las unidades planificadas en órdenes abiertas y las unidades producidas.
- Filtros combinables: búsqueda por número (espera de 300 ms), producto (selector de búsqueda), estado («Todos», «Planificada», «Liberada», «En proceso», «Terminada», «Cancelada») y rango de fechas de inicio planificado. Si `desde` es posterior a `hasta` se muestra «La fecha desde no puede ser posterior a la fecha hasta.» y no se consulta. Al cambiar un filtro se vuelve a la página 1. Al cargar, el listado se pide **una sola vez**.
- La tabla muestra número, producto, cantidad planificada, fechas de inicio y fin planificadas y estado con texto. Tamaño de página 20, orden de la API (inicio descendente). Cifras, fechas y estados en una sola línea (`white-space: nowrap`, `font-variant-numeric: tabular-nums`). Estados de carga, vacío y error con reintento.

### Detalle, formulario y transiciones

- El detalle muestra el producto, la cantidad planificada y la producida («—» si aún no se termina), las fechas planificadas, el inicio y el fin reales en hora de Perú, las versiones de la lista de materiales y de la ruta que la orden tiene fijas, y el motivo si está cancelada.
- El detalle incluye dos tablas: **materiales requeridos** (componente, cantidad requerida y unidad) y **operaciones** (secuencia, operación, centro de trabajo y minutos estimados), con el **tiempo estimado total** en minutos y en horas. Las tablas largas se desplazan dentro de su propio contenedor.
- **Nueva orden** abre el formulario: producto (selector en `GET /productos?busqueda=`), cantidad planificada en unidades enteras y fechas de inicio y fin planificadas (la de inicio, por defecto hoy en hora de Perú).
- **Editar** (solo `Planificada`) abre el mismo formulario sin el producto, que no se puede cambiar.
- **Liberar** (solo `Planificada`) e **Iniciar** (solo `Liberada`) envían la petición directamente, sin diálogo. El botón se deshabilita mientras la operación está en curso.
- **Terminar** (solo `EnProceso`) abre `TerminarOrdenDialog` con la cantidad producida (por defecto, la planificada).
- **Cancelar** (solo `Planificada` o `Liberada`) abre `CancelarOrdenDialog` con el motivo.
- Los diálogos usan `role="dialog"`, `aria-modal="true"`, trampa de foco y cierre con Escape sin enviar la petición. Al cerrar, el foco vuelve al botón de origen; si desaparece tras confirmar, va al encabezado del detalle.
- Usar `CampoConError` con `aria-describedby` y `aria-invalid`. Si hay errores locales no se llama a la API. Ante un rechazo de la API se conservan los valores escritos y el mensaje se muestra con `role="alert"`. Deshabilitar el envío mientras la operación está en curso. Tras el éxito se refrescan listado, resumen y detalle.

### Contrato HTTP

El cliente compartido gestiona la autenticación y se usa sin cambios.

| Operación | Petición bajo `/api/v1` | Datos |
|---|---|---|
| Listar | `GET /ordenes-produccion` | `busqueda`, `productoId`, `estado`, `desde`, `hasta`, `pagina` y `tamanoPagina`. Omitir los filtros vacíos. |
| Resumen | `GET /ordenes-produccion/resumen` | — |
| Consultar | `GET /ordenes-produccion/{id}` | — |
| Crear | `POST /ordenes-produccion` | `{ productoId, cantidadPlanificada, fechaInicioPlanificada, fechaFinPlanificada }`. |
| Editar | `PUT /ordenes-produccion/{id}` | `{ cantidadPlanificada, fechaInicioPlanificada, fechaFinPlanificada }`. |
| Liberar | `POST /ordenes-produccion/{id}/liberar` | Sin cuerpo. |
| Iniciar | `POST /ordenes-produccion/{id}/iniciar` | Sin cuerpo. |
| Terminar | `POST /ordenes-produccion/{id}/terminar` | `{ cantidadProducida }`. |
| Cancelar | `POST /ordenes-produccion/{id}/cancelar` | `{ motivo }` recortado. |
| Productos | `GET /productos` | `busqueda`, `pagina=1`, `tamanoPagina=20`. |

Las fechas planificadas se envían como día (`2026-12-01`), sin hora. Las cantidades se envían como enteros.

### Validaciones locales y mensajes

Mismos mensajes literales que el backend:

| Condición | Mensaje |
|---|---|
| Producto sin seleccionar | El identificador del producto debe ser mayor que 0. |
| Cantidad planificada vacía, no entera o fuera de 1–1000000 | La cantidad planificada debe estar entre 1 y 1000000. |
| Fecha de inicio o de fin vacía | Las fechas planificadas son obligatorias. |
| Fin anterior al inicio | La fecha de fin no puede ser anterior a la fecha de inicio. |
| Cantidad producida vacía, no entera o fuera de 0–1000000 | La cantidad producida debe estar entre 0 y 1000000. |
| Motivo vacío / de más de 250 caracteres | El motivo de cancelación es obligatorio. / El motivo de cancelación no puede superar 250 caracteres. |
| Búsqueda de más de 20 caracteres | El campo no admite más de 20 caracteres (`maxLength`), así que no se envía. |

«Hoy» es el día en hora de Perú (UTC−5, sin horario de verano): se calcula restando 5 horas al instante UTC y tomando `yyyy-MM-dd`, no con la fecha local del navegador ni con `toISOString()`, que da el día UTC y a partir de las 19:00 en Perú ya es mañana. Solo se usa para el valor por defecto de la fecha de inicio; el backend no limita las fechas planificadas respecto a hoy.

Los mensajes de producto inexistente, producto sin lista de materiales o sin ruta de producción activa, orden inexistente y conflictos de estado se muestran tal como los devuelve la API.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `npm --prefix frontend run verify` y `./scripts/verify.sh` | Ambos en verde y sin advertencias. |
| A2 | Pruebas del cliente HTTP (`api.test.ts`) | Cubren las diez peticiones de §4 con URL, método, query sin filtros vacíos y cuerpos exactos (liberar e iniciar sin cuerpo). |
| A3 | Pruebas de resumen, filtros y listado | Carga inicial con `esperarCargaInicial…` y una sola petición del listado; espera de 300 ms con temporizadores falsos; combinación de filtros; reinicio de página; rango de fechas inválido sin petición; estados de carga, vacío y error. |
| A4 | Pruebas de detalle, formulario y diálogos | Materiales y operaciones del detalle con el tiempo total; cada validación local sin petición; edición solo en `Planificada` y sin producto; acciones según el estado; conservación del formulario ante un 409 (producto sin lista o sin ruta activa); terminar con la cantidad producida; cancelación con motivo; Escape sin petición y devolución del foco. La suite pasa 10 veces seguidas. |
| A5 | Prueba con API real y regresión | La ruta abre desde el catálogo. Con sesión iniciada se crea, edita, libera, inicia y termina una orden, y se cancela otra. Las pruebas existentes pasan. |
| A6 | Control visual | Listado, detalle (con sus dos tablas), formulario y diálogos a 375 px y 1280 px en modo claro y oscuro, sin desbordamiento horizontal de la página. Capturas adjuntas al PR. |

## 6. Checklist de ejecución

- [ ] Aprobación de Lukas sobre el alcance y el diseño.
- [ ] Backend del 34 integrado en `main`, y rama actualizada.
- [ ] Commit 2: tipos y cliente HTTP.
- [ ] Commit 3: pantalla, componentes, registro en `pantallas.ts` y estilos.
- [ ] Verificar A1–A6 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
