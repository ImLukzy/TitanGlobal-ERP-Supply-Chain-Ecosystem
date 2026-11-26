# Especificación: módulo 39 — Mantenimiento Preventivo (frontend)

## 1. Objetivo

Habilitar la pantalla de Mantenimiento Preventivo en Manufactura (`/modulos/39-mantenimiento-preventivo`, que hoy muestra «Pantalla pendiente»). El usuario podrá consultar las paradas programadas de los centros de trabajo, ver las vencidas y las de los próximos 7 días, programar y editar una parada, llevarla por sus estados (iniciar, completar con las horas reales o cancelar con motivo) y ver la fecha sugerida de la siguiente parada.

Responsable: Antony Huayhua. Estado: aprobada por Lukas por el chat del equipo (PR #88, integrada el 2026-11-25).

La implementación consume el [contrato del backend del módulo 39](modulo39-mantenimiento-preventivo-backend.md) y, para elegir el centro de trabajo, el listado del módulo 33. Las pruebas de la pantalla simulan la API; la prueba con la API real (A5) requiere el backend del 39 (PR #87) en el entorno de desarrollo.

## 2. Fuera de alcance

- Averías, repuestos y paradas no planificadas (módulo 40), y la generación automática de la siguiente parada.
- Cambios al contrato HTTP, al esquema de base de datos o a la autenticación y los permisos existentes.
- Cambios a otras pantallas, a los componentes compartidos, al cliente HTTP compartido (`frontend/src/api/client.ts`) y a los estilos globales.

**Decisiones de producto que requieren aprobación:** ninguna además de las de la spec del backend.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `frontend/src/modules/mantenimiento-preventivo/tipos.ts` | Definir los estados, filtros y tipos del contrato HTTP. |
| `frontend/src/modules/mantenimiento-preventivo/api.ts` y `api.test.ts` | Implementar y probar las ocho operaciones del módulo y el catálogo de centros de trabajo con el cliente HTTP compartido. |
| `frontend/src/modules/mantenimiento-preventivo/validacion.ts` | Validar localmente el guardado, el cierre y la cancelación con los mensajes literales del backend, y calcular «hoy» en hora de Perú. |
| `frontend/src/modules/mantenimiento-preventivo/MantenimientoPreventivoPage.tsx` y `MantenimientoPreventivoPage.test.tsx` | Coordinar resumen, filtros, listado, detalle, formulario y diálogos. Probar los flujos. |
| `frontend/src/modules/mantenimiento-preventivo/MantenimientosResumen.tsx`, `MantenimientosFiltros.tsx`, `MantenimientosTabla.tsx`, `MantenimientoDetallePanel.tsx`, `MantenimientoFormulario.tsx`, `CompletarMantenimientoDialog.tsx` y `CancelarMantenimientoDialog.tsx` | Separar cada parte de la pantalla (≤ ~300 líneas por archivo). |
| `frontend/src/modules/pantallas.ts` | Registrar la pantalla con la clave `39-mantenimiento-preventivo` del catálogo, al final del objeto. |
| `frontend/src/styles/app.css` | Añadir al final un bloque con el prefijo `mantenimiento-preventivo` que use solo los tokens existentes. |

## 4. Diseño y lógica

### Resumen, filtros y listado

- Usar `PaginaModulo`, `TablaPaginada` y `PanelDetalle`. El resumen (`GET /mantenimientos-preventivos/resumen`) muestra el total, las paradas por estado (en el orden que devuelve la API), las vencidas, las de los próximos 7 días y las horas planificadas pendientes.
- Filtros combinables: búsqueda por número o descripción (espera de 300 ms), centro de trabajo (selector de búsqueda), estado («Todos», «Programado», «En ejecución», «Completado», «Cancelado») y rango de fechas programadas. Si `desde` es posterior a `hasta` se muestra «La fecha desde no puede ser posterior a la fecha hasta.» y no se consulta. Al cambiar un filtro se vuelve a la página 1. Al cargar, el listado se pide **una sola vez**.
- La tabla muestra número, centro (código y nombre), descripción, fecha programada, duración planificada en horas y estado con texto. Una parada vencida lleva además la etiqueta «Vencido» en texto, no solo el color. Tamaño de página 20, orden de la API (fecha ascendente). Cifras, fechas y estados en una sola línea (`white-space: nowrap`, `font-variant-numeric: tabular-nums`). Estados de carga, vacío y error con reintento.

### Detalle, formulario y transiciones

- El detalle muestra todos los campos: frecuencia («Sin frecuencia» si no tiene), responsable («Sin responsable»), inicio y fin reales en hora de Perú, horas reales y observaciones. Si está cancelada, el motivo. Si está completada y tiene frecuencia, la fecha sugerida de la siguiente parada.
- **Nuevo mantenimiento** abre el formulario: centro de trabajo (selector en `GET /centros-trabajo?busqueda=&activo=true`), descripción, fecha programada (por defecto hoy en hora de Perú), duración planificada en horas, frecuencia en días (opcional) y responsable (opcional).
- **Editar** (solo `Programado`) abre el mismo formulario sin el centro, que no se puede cambiar.
- **Iniciar** (solo `Programado`) envía la petición directamente, sin diálogo.
- **Completar** (solo `EnEjecucion`) abre `CompletarMantenimientoDialog` con las horas reales y observaciones (opcional).
- **Cancelar** (solo `Programado`) abre `CancelarMantenimientoDialog` con el motivo.
- Los diálogos usan `role="dialog"`, `aria-modal="true"`, trampa de foco y cierre con Escape sin enviar la petición. Al cerrar, el foco vuelve al botón de origen; si desaparece tras confirmar, va al encabezado del detalle.
- Usar `CampoConError` con `aria-describedby` y `aria-invalid`. Si hay errores locales no se llama a la API. Ante un rechazo de la API se conservan los valores escritos y el mensaje se muestra con `role="alert"`. Deshabilitar el envío mientras la operación está en curso. Tras el éxito se refrescan listado, resumen y detalle.

### Contrato HTTP

El cliente compartido gestiona la autenticación y se usa sin cambios.

| Operación | Petición bajo `/api/v1` | Datos |
|---|---|---|
| Listar | `GET /mantenimientos-preventivos` | `busqueda`, `centroTrabajoId`, `estado`, `desde`, `hasta`, `pagina` y `tamanoPagina`. Omitir los filtros vacíos. |
| Resumen | `GET /mantenimientos-preventivos/resumen` | — |
| Consultar | `GET /mantenimientos-preventivos/{id}` | — |
| Crear | `POST /mantenimientos-preventivos` | `{ centroTrabajoId, descripcion, fechaProgramada, duracionHorasPlanificada, frecuenciaDias?, responsable? }`. |
| Editar | `PUT /mantenimientos-preventivos/{id}` | `{ descripcion, fechaProgramada, duracionHorasPlanificada, frecuenciaDias?, responsable? }`. |
| Iniciar | `POST /mantenimientos-preventivos/{id}/iniciar` | Sin cuerpo. |
| Completar | `POST /mantenimientos-preventivos/{id}/completar` | `{ horasReales, observaciones? }`. |
| Cancelar | `POST /mantenimientos-preventivos/{id}/cancelar` | `{ motivo }` recortado. |
| Centros de trabajo | `GET /centros-trabajo` | `busqueda`, `pagina=1`, `tamanoPagina=20`, y `activo=true` en el formulario. El filtro del listado incluye los inactivos. |

Las fechas programadas se envían como día (`2026-12-01`), sin hora. Los textos se envían recortados y los opcionales vacíos no se envían.

### Validaciones locales y mensajes

Mismos mensajes literales que el backend:

| Condición | Mensaje |
|---|---|
| Centro sin seleccionar | El identificador del centro de trabajo debe ser mayor que 0. |
| Descripción vacía / de más de 250 caracteres | La descripción del mantenimiento es obligatoria. / La descripción del mantenimiento no puede superar 250 caracteres. |
| Fecha vacía / anterior a hoy | La fecha programada es obligatoria. / La fecha programada no puede ser anterior a hoy. |
| Duración fuera de (0, 168] / con más de 2 decimales | La duración debe ser mayor que 0 y como máximo 168 horas. / La duración admite como máximo 2 decimales. |
| Frecuencia fuera de 1–3650 | La frecuencia debe estar entre 1 y 3650 días. |
| Responsable de más de 100 caracteres | El responsable no puede superar 100 caracteres. |
| Horas reales fuera de (0, 168] / con más de 2 decimales | Las horas reales deben ser mayores que 0 y como máximo 168. / Las horas reales admiten como máximo 2 decimales. |
| Observaciones de más de 500 caracteres | Las observaciones no pueden superar 500 caracteres. |
| Motivo vacío / de más de 250 caracteres | El motivo de cancelación es obligatorio. / El motivo de cancelación no puede superar 250 caracteres. |

«Hoy» es el día en hora de Perú (UTC−5, sin horario de verano), igual que en el backend: se calcula restando 5 horas al instante UTC y tomando `yyyy-MM-dd`, no con la fecha local del navegador ni con `toISOString()`, que da el día UTC y a partir de las 19:00 en Perú ya es mañana. Se usa para el valor por defecto de la fecha y para la validación local.

Los mensajes de centro inexistente o inactivo, fecha repetida en el centro, parada inexistente y conflictos de estado se muestran tal como los devuelve la API.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `npm --prefix frontend run verify` y `./scripts/verify.sh` | Ambos en verde y sin advertencias. |
| A2 | Pruebas del cliente HTTP (`api.test.ts`) | Cubren las nueve peticiones de §4 con URL, método, query sin filtros vacíos y cuerpos exactos (sin opcionales vacíos). |
| A3 | Pruebas de resumen, filtros y listado | Carga inicial con `esperarCargaInicial…` y una sola petición del listado; espera de 300 ms con temporizadores falsos; combinación de filtros; reinicio de página; rango de fechas inválido sin petición; estados de carga, vacío y error; etiqueta «Vencido». |
| A4 | Pruebas de detalle, formulario y diálogos | Cada validación local sin petición; «hoy» en hora de Perú a las 19:00 (temporizadores falsos); edición solo en `Programado` y sin centro; acciones según el estado; conservación del formulario ante un 409 (fecha repetida); completar con horas reales; cancelación con motivo; Escape sin petición y devolución del foco. La suite pasa 10 veces seguidas. |
| A5 | Prueba con API real y regresión | La ruta abre desde el catálogo. Con sesión iniciada se programa, edita, inicia y completa una parada, y se cancela otra. Las pruebas existentes pasan. |
| A6 | Control visual | Listado, detalle, formulario y diálogos a 375 px y 1280 px en modo claro y oscuro, sin desbordamiento horizontal de la página. Capturas adjuntas al PR. |

## 6. Checklist de ejecución

- [x] Aprobación de Lukas sobre el alcance y el diseño (chat del equipo, 2026-11-25).
- [ ] Backend del 39 integrado en `main` (PR #87), y rama actualizada.
- [ ] Commit 2: tipos, cliente HTTP y sus pruebas.
- [ ] Commit 3: pantalla, componentes, registro en `pantallas.ts`, estilos y pruebas de la página.
- [ ] Verificar A1–A6 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-25 | A1 | `./scripts/verify.sh` en verde y sin advertencias. | Backend: Superado 1759, Con error 0. Frontend: Test Files 150 passed, Tests 1482 passed. |
| 2026-11-25 | A2 | Nueve peticiones con URL, método, query sin filtros vacíos y cuerpos exactos. | `api.test.ts`: 6 pruebas en verde. |
| 2026-11-25 | A3 | Carga inicial con una sola petición del listado, espera de 300 ms con temporizadores falsos, filtros combinados, reinicio de página, rango inválido sin petición, carga, vacío, error y «Vencido». | `MantenimientoPreventivoPage.test.tsx`: 11 pruebas en verde. |
| 2026-11-25 | A4 | Validaciones locales sin petición, «hoy» en hora de Perú a las 19:00, edición sin centro, acciones por estado, 409 de fecha repetida conservando el formulario, completar, cancelar, Escape y foco. | `MantenimientoPreventivoAcciones.test.tsx`: 10 pruebas en verde. Suite del módulo 10 de 10 ejecuciones seguidas en verde. |
| — | A5 | Pendiente: necesita el backend del PR #87 en `main`. | — |
| — | A6 | Pendiente: capturas a 375 px y 1280 px en claro y oscuro con datos reales. | — |
