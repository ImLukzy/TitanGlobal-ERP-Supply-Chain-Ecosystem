# Especificación: módulo 32 — Rutas de Producción (frontend)

## 1. Objetivo

Habilitar la pantalla de Rutas de Producción en Manufactura. El usuario podrá consultar las versiones de la ruta de cada producto con sus operaciones, crear y editar borradores, activar una versión y retirar la versión activa conservando su historial.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

La implementación consume el [contrato del backend del módulo 32](modulo32-rutas-produccion-backend.md) (PR #49). Las pruebas de la pantalla simulan la API; la prueba con la API real (A5) requiere ese backend en el entorno de desarrollo.

## 2. Fuera de alcance

- Programación de capacidad, costos, rutas alternativas e integración con Órdenes de Producción o Listas de Materiales.
- Cambios al contrato HTTP, al esquema de base de datos o a la autenticación y los permisos existentes.
- Cambios a otras pantallas, a los componentes compartidos, al cliente HTTP compartido (`frontend/src/api/client.ts`) y a los estilos globales.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `frontend/src/modules/rutas-produccion/tipos.ts` | Definir los estados, filtros y tipos del contrato HTTP. |
| `frontend/src/modules/rutas-produccion/api.ts` y `api.test.ts` | Implementar y probar las seis operaciones con el cliente HTTP compartido. |
| `frontend/src/modules/rutas-produccion/validacion.ts` | Validar localmente las operaciones con los mensajes literales del backend. |
| `frontend/src/modules/rutas-produccion/RutasProduccionPage.tsx` y `RutasProduccionPage.test.tsx` | Coordinar listado, detalle, formulario y transiciones. Probar los flujos de la pantalla. |
| `frontend/src/modules/rutas-produccion/RutasProduccionFormulario.tsx`, `RutaProduccionDecisionDialog.tsx` y `SelectorCatalogo.tsx` | Separar la edición de operaciones, la confirmación de cambios de estado y la selección de producto y centro de trabajo. |
| `frontend/src/modules/pantallas.ts` | Registrar la pantalla con la clave `32-rutas-produccion` del catálogo, al final del objeto. |
| `frontend/src/styles/app.css` | Añadir al final un bloque con el prefijo `rutas-produccion` que use solo los tokens existentes. |

## 4. Diseño y lógica

### Listado y detalle

- Usar `PaginaModulo`, `TablaPaginada` y `PanelDetalle`. La tabla muestra producto, versión, estado y número de operaciones. El orden viene de la API (producto ascendente y versión descendente) y el tamaño de página es 20.
- Filtros combinables: producto fabricado y estado (`Borrador`, `Activa`, `Retirada`). Al cambiar un filtro se vuelve a la primera página.
- El detalle muestra las operaciones ordenadas por secuencia, con nombre, centro de trabajo, minutos de preparación y minutos por unidad. Las cifras usan `white-space: nowrap` y `font-variant-numeric: tabular-nums`.
- Mostrar estados de carga, lista vacía y error con reintento. Si cambia la selección durante una consulta, el panel muestra solo el resultado de la selección vigente.

### Formulario y transiciones

- El alta permite elegir el producto fabricado, la versión y las operaciones. La edición de un borrador muestra producto y versión como datos de lectura y envía todas las operaciones.
- Cada operación permite indicar nombre, centro de trabajo, minutos de preparación y minutos por unidad, quitarla y moverla arriba o abajo. La secuencia no se escribe a mano: se asigna por la posición en el formulario (1, 2, 3…), así que el envío siempre es consecutivo y empieza en 1.
- El producto se busca en el catálogo paginado de Productos (`GET /productos?busqueda=`) y el centro en el de Centros de trabajo (`GET /centros-trabajo?busqueda=`). El selector muestra el nombre y envía el identificador. Los centros inactivos se marcan «(inactivo)»: se admiten en un borrador, y al activar la API responde 409 con su mensaje.
- En `Borrador`, mostrar **Editar** y **Activar**. En `Activa`, mostrar **Retirar**. En `Retirada`, solo consulta.
- Usar `CampoConError` y asociar cada error a su campo con `aria-describedby` y `aria-invalid`. Si hay errores locales no se llama a la API. Ante un rechazo de la API se conservan todos los valores escritos y el mensaje se muestra con `role="alert"`. Tras el éxito se refrescan el listado y el detalle.
- Confirmar las transiciones con un diálogo del módulo (`role="dialog"`, `aria-modal="true"`, trampa de foco y cierre con Escape sin enviar la petición). El diálogo muestra la acción y la versión afectada, y en el retiro indica que la versión queda disponible en el historial. Al cancelar, el foco vuelve al botón de origen. Si ese botón desaparece tras confirmar, el foco va al encabezado del detalle.
- Deshabilitar el envío mientras la operación está en curso. Ante un 404 o 409 se muestra el mensaje de la API y se ofrece volver a consultar el estado vigente.

### Contrato HTTP

El cliente compartido gestiona la autenticación y se usa sin cambios. Las transiciones usan `apiPostSinCuerpo`.

| Operación | Petición bajo `/api/v1` | Datos |
|---|---|---|
| Listar | `GET /rutas-produccion` | `productoId`, `estado`, `pagina` y `tamanoPagina`. Omitir los filtros vacíos. |
| Consultar | `GET /rutas-produccion/{id}` | Identificador de la versión. |
| Crear | `POST /rutas-produccion` | `{ productoId, version, operaciones }`. |
| Editar | `PUT /rutas-produccion/{id}` | `{ operaciones }`. |
| Activar | `POST /rutas-produccion/{id}/activar` | Sin cuerpo. |
| Retirar | `POST /rutas-produccion/{id}/retirar` | Sin cuerpo. |

Cada operación enviada contiene `secuencia`, `nombre` recortado, `centroTrabajoId`, `minutosPreparacion` y `minutosPorUnidad` como números JSON. Los minutos caben en `numeric(10,2)` (como máximo 10 dígitos), así que un `number` de JavaScript los conserva sin redondeo.

### Validaciones locales y mensajes

Mismos mensajes literales que el backend:

| Condición | Mensaje |
|---|---|
| Producto fabricado sin seleccionar | El identificador del producto debe ser mayor que 0. |
| Versión ausente o fuera del rango de enteros positivos | La versión debe ser mayor que 0. |
| Menos de 1 o más de 100 operaciones | La ruta de producción debe tener entre 1 y 100 operaciones. |
| Nombre vacío tras recortar espacios | El nombre de la operación es obligatorio. |
| Nombre de más de 120 caracteres tras recortar espacios | El nombre de la operación no puede superar 120 caracteres. |
| Centro de trabajo sin seleccionar | El identificador del centro de trabajo debe ser mayor que 0. |
| Preparación vacía o fuera de 0–99999999.99 | Los minutos de preparación deben estar entre 0 y 99999999.99. |
| Preparación con más de 2 decimales significativos | Los minutos de preparación admiten como máximo 2 decimales. |
| Ejecución por unidad vacía o fuera de (0, 99999999.99] | Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99. |
| Ejecución por unidad con más de 2 decimales significativos | Los minutos por unidad admiten como máximo 2 decimales. |

Los ceros del extremo derecho no cuentan como decimales. La preparación admite cero, pero el campo no puede quedar vacío. Los mensajes de referencias inexistentes, versión duplicada, centro inactivo y conflictos de estado se muestran tal como los devuelve la API.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `npm --prefix frontend run verify` y `./scripts/verify.sh` | Ambos en verde y sin advertencias propias del módulo. |
| A2 | Pruebas del cliente HTTP (`api.test.ts`) | Cubren las seis rutas, los filtros omitidos, los cuerpos exactos de alta y edición y las transiciones sin cuerpo. |
| A3 | Pruebas de listado y detalle | Cubren la carga inicial con `esperarCargaInicial…`, la combinación de filtros, el reinicio de página, los estados de carga, vacío y error, y las operaciones ordenadas en el detalle. |
| A4 | Pruebas del formulario y transiciones | Cubren las validaciones locales sin petición, la secuencia por posición al mover operaciones, la edición solo en borrador, la conservación del formulario ante un 409, la activación y el retiro, la cancelación con Escape sin petición y la devolución del foco. La suite pasa 10 veces seguidas. |
| A5 | Prueba con API real y regresión | La ruta abre desde el catálogo. Con sesión iniciada se crea, edita, activa y retira una versión, y la retirada sigue en el historial. Las pruebas existentes pasan. |
| A6 | Control visual | Listado, detalle, formulario y diálogo a 375 px y 1280 px en modo claro y oscuro, sin desbordamiento horizontal de la página. Capturas adjuntas al PR. |

## 6. Checklist de ejecución

- [ ] Aprobación de Lukas sobre el alcance y el diseño.
- [ ] Commit 2: tipos, cliente HTTP y sus pruebas.
- [ ] Commit 3: pantalla, formulario, diálogo, registro en `pantallas.ts`, estilos y pruebas de la página.
- [ ] Verificar A1–A6 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
