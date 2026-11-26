# Especificación: módulo 35 — Consumo de Materiales (frontend)

## 1. Objetivo

Habilitar la pantalla de Consumo de Materiales en Manufactura (`/modulos/35-consumo-materiales`, que hoy muestra «Pantalla pendiente»). El usuario podrá consultar los consumos registrados, ver el balance de una orden de producción (requerido, consumido, pendiente y desviación), registrar un consumo de un componente de la orden y anular un consumo con su motivo.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

La implementación consume el [contrato del backend del módulo 35](modulo35-consumo-materiales-backend.md) y, para elegir la orden, el listado del módulo 34 (`GET /ordenes-produccion`). Las pruebas de la pantalla simulan la API; la prueba con la API real (A5) requiere ambos backends en el entorno de desarrollo.

## 2. Fuera de alcance

- Stock, movimientos de inventario, mermas y costos.
- Cambios al contrato HTTP, al esquema de base de datos o a la autenticación y los permisos existentes.
- Cambios a otras pantallas, a los componentes compartidos, al cliente HTTP compartido (`frontend/src/api/client.ts`) y a los estilos globales.

**Decisiones de producto que requieren aprobación:** ninguna además de las de la spec del backend.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `frontend/src/modules/consumo-materiales/tipos.ts` | Definir los estados, filtros y tipos del contrato HTTP. |
| `frontend/src/modules/consumo-materiales/api.ts` y `api.test.ts` | Implementar y probar las seis operaciones del módulo y la búsqueda de órdenes en proceso con el cliente HTTP compartido. |
| `frontend/src/modules/consumo-materiales/validacion.ts` | Validar localmente el registro y la anulación con los mensajes literales del backend. |
| `frontend/src/modules/consumo-materiales/ConsumoMaterialesPage.tsx` y `ConsumoMaterialesPage.test.tsx` | Coordinar resumen, filtros, listado, detalle, balance, registro y anulación. Probar los flujos. |
| `frontend/src/modules/consumo-materiales/ConsumoMaterialesResumen.tsx`, `ConsumoMaterialesFiltros.tsx`, `ConsumoMaterialesTabla.tsx`, `ConsumoMaterialDetallePanel.tsx`, `BalanceOrdenPanel.tsx`, `ConsumoMaterialFormulario.tsx` y `AnularConsumoDialog.tsx` | Separar cada parte de la pantalla (≤ ~300 líneas por archivo). |
| `frontend/src/modules/pantallas.ts` | Registrar la pantalla con la clave `35-consumo-materiales` del catálogo, al final del objeto. |
| `frontend/src/styles/app.css` | Añadir al final un bloque con el prefijo `consumo-materiales` que use solo los tokens existentes. |

## 4. Diseño y lógica

### Resumen, filtros y listado

- Usar `PaginaModulo`, `TablaPaginada` y `PanelDetalle`. El resumen (`GET /consumos-materiales/resumen`) muestra consumos registrados, anulados, órdenes con consumo y consumos de los últimos 7 días.
- Filtros combinables: búsqueda por número (espera de 300 ms), orden de producción (selector de búsqueda), componente (selector de búsqueda en Productos), estado («Todos», «Registrado», «Anulado») y rango de fechas. Si `desde` es posterior a `hasta` se muestra «La fecha desde no puede ser posterior a la fecha hasta.» y no se consulta. Al cambiar un filtro se vuelve a la página 1. Al cargar, el listado se pide **una sola vez**.
- La tabla muestra número, orden, componente, cantidad con su unidad, fecha de registro y estado con texto. Tamaño de página 20, orden de la API. Las cifras y fechas usan `white-space: nowrap` y `font-variant-numeric: tabular-nums`. Estados de carga, vacío y error con reintento.

### Detalle y balance

- El detalle muestra todos los campos del consumo; sin responsable → «Sin responsable»; si está anulado, la fecha y el motivo de anulación.
- Desde el detalle, o al elegir una orden en el filtro, se abre el **balance de la orden** (`GET /consumos-materiales/ordenes/{id}/balance`): tabla con componente, unidad, requerido, consumido, pendiente y desviación. La desviación positiva se marca como «Exceso» y la negativa como «Por consumir», con texto además del color. Si cambia la selección durante una consulta, el panel muestra solo el resultado de la selección vigente.

### Registro y anulación

- **Registrar consumo** (botón principal): se elige una orden `EnProceso` en el selector (`GET /ordenes-produccion?estado=EnProceso&busqueda=`). Al elegirla, el componente se selecciona entre las líneas de su balance, que muestran la unidad y lo pendiente. La unidad no se escribe. Se indican cantidad, responsable y observaciones opcionales.
- **Anular** solo aparece en consumos `Registrado` de una orden `EnProceso`. Abre `AnularConsumoDialog` (`role="dialog"`, `aria-modal="true"`, trampa de foco y cierre con Escape sin enviar la petición) con el campo del motivo. Al cerrar, el foco vuelve al botón **Anular**; si desaparece tras confirmar, va al encabezado del detalle.
- Usar `CampoConError` con `aria-describedby` y `aria-invalid`. Si hay errores locales no se llama a la API. Ante un rechazo de la API se conservan los valores escritos y el mensaje se muestra con `role="alert"`. Deshabilitar el envío mientras la operación está en curso. Tras el éxito se refrescan listado, resumen, detalle y balance.

### Contrato HTTP

El cliente compartido gestiona la autenticación y se usa sin cambios.

| Operación | Petición bajo `/api/v1` | Datos |
|---|---|---|
| Listar | `GET /consumos-materiales` | `busqueda`, `ordenProduccionId`, `componenteProductoId`, `estado`, `desde`, `hasta`, `pagina` y `tamanoPagina`. Omitir los filtros vacíos. |
| Resumen | `GET /consumos-materiales/resumen` | — |
| Balance | `GET /consumos-materiales/ordenes/{ordenProduccionId}/balance` | — |
| Consultar | `GET /consumos-materiales/{id}` | — |
| Registrar | `POST /consumos-materiales` | `{ ordenProduccionId, componenteProductoId, cantidad, responsable?, observaciones? }`; los opcionales vacíos no se envían. |
| Anular | `POST /consumos-materiales/{id}/anular` | `{ motivo }` recortado. |
| Órdenes en proceso | `GET /ordenes-produccion` | `estado=EnProceso`, `busqueda`, `pagina=1`, `tamanoPagina=20`. |
| Componentes (filtro) | `GET /productos` | `busqueda`, `pagina=1`, `tamanoPagina=20`. |

La cantidad admite hasta 1000000 con 6 decimales (13 dígitos significativos), así que un `number` de JavaScript la conserva sin redondeo.

### Validaciones locales y mensajes

Mismos mensajes literales que el backend:

| Condición | Mensaje |
|---|---|
| Orden sin seleccionar | El identificador de la orden de producción debe ser mayor que 0. |
| Componente sin seleccionar | El identificador del componente debe ser mayor que 0. |
| Cantidad vacía o fuera de (0, 1000000] | La cantidad consumida debe ser mayor que 0 y como máximo 1000000. |
| Cantidad con más de 6 decimales significativos | La cantidad consumida admite como máximo 6 decimales. |
| Responsable de más de 100 caracteres | El responsable no puede superar 100 caracteres. |
| Observaciones de más de 250 caracteres | Las observaciones no pueden superar 250 caracteres. |
| Motivo vacío tras recortar espacios | El motivo de anulación es obligatorio. |
| Motivo de más de 250 caracteres | El motivo de anulación no puede superar 250 caracteres. |

Los mensajes de orden fuera de proceso, componente fuera de la lista y consumo ya anulado se muestran tal como los devuelve la API.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `npm --prefix frontend run verify` y `./scripts/verify.sh` | Ambos en verde y sin advertencias. |
| A2 | Pruebas del cliente HTTP (`api.test.ts`) | Cubren las ocho peticiones de §4 con URL, método, query sin filtros vacíos y cuerpos exactos. |
| A3 | Pruebas de resumen, filtros y listado | Carga inicial con `esperarCargaInicial…` y una sola petición del listado; espera de 300 ms con temporizadores falsos; combinación de filtros; reinicio de página; rango de fechas inválido sin petición; estados de carga, vacío y error. |
| A4 | Pruebas de detalle, balance, registro y anulación | Balance con exceso y por consumir; componentes tomados del balance de la orden elegida; cada validación local sin petición; conservación del formulario ante un 409; anulación con motivo; Escape sin petición y devolución del foco. La suite pasa 10 veces seguidas. |
| A5 | Prueba con API real y regresión | La ruta abre desde el catálogo. Con sesión iniciada se registra un consumo, se ve en el balance y se anula. Las pruebas existentes pasan. |
| A6 | Control visual | Listado, detalle, balance, formulario y diálogo a 375 px y 1280 px en modo claro y oscuro, sin desbordamiento horizontal de la página. Capturas adjuntas al PR. |

## 6. Checklist de ejecución

- [ ] Aprobación de Lukas sobre el alcance y el diseño.
- [ ] Backend del 35 integrado en `main`, y rama actualizada.
- [ ] Commit 2: tipos, cliente HTTP y sus pruebas.
- [ ] Commit 3: pantalla, componentes, registro en `pantallas.ts`, estilos y pruebas de la página.
- [ ] Verificar A1–A6 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
