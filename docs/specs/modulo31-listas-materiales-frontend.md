# Especificación: módulo 31 — Listas de Materiales (frontend)

## 1. Objetivo

Habilitar la pantalla de Listas de Materiales en Manufactura. El usuario podrá consultar las versiones y sus componentes, crear y editar borradores, activar una versión y retirar la versión activa conservando su historial.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

La implementación consume el [contrato del backend del módulo 31](modulo31-listas-materiales-backend.md). Las pruebas contra la API real requieren que ese backend esté disponible en el entorno de desarrollo.

## 2. Fuera de alcance

- Explosión multinivel, costos, conversiones de unidades, inventario y órdenes de producción.
- Cambios al contrato HTTP, al esquema de base de datos o a la autenticación y los permisos existentes.
- Cambios a otras pantallas y a los estilos globales compartidos.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `frontend/src/modules/listas-materiales/tipos.ts` | Definir los estados, filtros y tipos del contrato HTTP. |
| `frontend/src/modules/listas-materiales/api.ts` y `api.test.ts` | Implementar y probar las seis operaciones con el cliente HTTP compartido. |
| `frontend/src/modules/listas-materiales/ListasMaterialesPage.tsx` y `ListasMaterialesPage.test.tsx` | Coordinar listado, detalle, formulario y transiciones. Probar los flujos de la pantalla. |
| `frontend/src/modules/listas-materiales/ListasMaterialesFormulario.tsx` y `ListaMaterialDecisionDialog.tsx` | Separar la edición de líneas y la confirmación de cambios de estado. |
| `frontend/src/modules/pantallas.ts` | Registrar la pantalla con la clave `31-listas-materiales` del catálogo. |
| `frontend/src/styles/app.css` | Añadir estilos de la pantalla con el prefijo `listas-materiales` y los tokens existentes. |

## 4. Diseño y lógica

### Listado y detalle

- Usar `PaginaModulo`, `TablaPaginada` y `PanelDetalle`. Mostrar producto, versión, estado y número de componentes. El detalle presenta los nombres de los componentes, sus cantidades y unidades de medida.
- Combinar los filtros de producto fabricado y estado (`Borrador`, `Activa`, `Retirada`). Al cambiar un filtro, volver a la primera página. El tamaño inicial es 20 y el orden viene de la API.
- Reutilizar el catálogo paginado de Productos para seleccionar por nombre el producto fabricado y cada componente. La selección envía el identificador correspondiente.
- Mostrar estados de carga, lista vacía y error. Si cambia la selección durante una consulta, el panel muestra únicamente el resultado de la selección vigente.

### Formulario y transiciones

- El alta permite escoger el producto fabricado, la versión y las líneas. La edición de un borrador muestra producto y versión como datos de lectura y envía todas las líneas. Cada línea permite elegir el componente, indicar cantidad y unidad, y quitarla del formulario.
- En `Borrador`, mostrar **Editar** y **Activar**. En `Activa`, mostrar **Retirar**. En `Retirada`, permitir la consulta del detalle y las líneas.
- Usar `CampoConError` y asociar cada error al campo correspondiente. Conservar lo ingresado si la API rechaza una escritura. Tras el éxito, refrescar el listado y el detalle. Si el nuevo estado deja la versión fuera del filtro activo, actualizar el listado y anunciar el resultado.
- Confirmar las transiciones con un diálogo del módulo que usa el patrón accesible de Solicitudes de Compra. Mostrar la acción real y la versión afectada. Indicar en el retiro que la versión queda disponible en el historial. Al cancelar, devolver el foco al botón de origen. Si ese botón desaparece tras confirmar, devolverlo al encabezado del detalle o del listado.
- Deshabilitar el envío mientras la operación está en curso. Ante un `404` o `409`, mostrar el mensaje de la API y permitir volver a consultar el estado vigente.

### Contrato HTTP

El cliente compartido (`frontend/src/api/client.ts`) gestiona la autenticación y se usa sin cambios: `apiGet`, `apiPost` y `apiPut` serializan con `JSON.stringify`. Las transiciones usan `apiPostSinCuerpo`.

| Operación | Petición bajo `/api/v1` | Datos |
|---|---|---|
| Listar | `GET /listas-materiales` | `productoId`, `estado`, `pagina` y `tamanoPagina`. Omitir los filtros vacíos. |
| Consultar | `GET /listas-materiales/{id}` | Identificador de la versión. |
| Crear | `POST /listas-materiales` | `{ productoId, version, componentes }`. |
| Editar | `PUT /listas-materiales/{id}` | `{ componentes }`. |
| Activar | `POST /listas-materiales/{id}/activar` | Sin cuerpo. |
| Retirar | `POST /listas-materiales/{id}/retirar` | Sin cuerpo. |

Cada línea enviada contiene `componenteProductoId`, `cantidad` numérica y `unidadMedida` sin espacios al inicio o al final.

### Validaciones locales y mensajes

| Condición | Mensaje |
|---|---|
| Producto fabricado sin seleccionar | El identificador del producto debe ser mayor que 0. |
| Versión ausente o fuera del rango de enteros positivos del contrato | La versión debe ser mayor que 0. |
| Menos de 1 o más de 100 componentes | La lista de materiales debe tener entre 1 y 100 componentes. |
| Componente sin seleccionar | El identificador del componente debe ser mayor que 0. |
| Componente igual al producto fabricado | El producto fabricado no puede ser componente de su propia lista. |
| Componente repetido | El componente {componenteProductoId} está repetido. |
| Cantidad fuera del rango permitido | La cantidad debe ser mayor que 0 y no superar 999999999999.999999. |
| Cantidad con más de seis decimales significativos | La cantidad admite como máximo 6 decimales. |
| Cantidad con más de 15 dígitos significativos (solo validación local) | La cantidad admite como máximo 15 dígitos significativos. |
| Unidad de medida en blanco | La unidad de medida es obligatoria. |
| Unidad de medida de más de 20 caracteres tras recortar espacios | La unidad de medida no puede superar 20 caracteres. |

Validar la cantidad sobre el texto ingresado y enviarla como número JSON. Un `number` de JavaScript conserva sin redondeo hasta 15 dígitos significativos, por eso la pantalla limita la cantidad a 15 dígitos aunque el backend admita hasta 18 (`numeric(18,6)`). Así el valor enviado es exactamente el escrito y no hace falta modificar el cliente compartido. Una cantidad de más de 15 dígitos registrada por otra vía se muestra redondeada en el detalle. Cubrir en las pruebas del cliente el envío exacto de 15 dígitos, el rechazo local de 16 y las seis posiciones decimales. Mostrar los mensajes literales del backend para referencias inexistentes, versiones duplicadas y conflictos de estado.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `npm --prefix frontend run verify` y `./scripts/verify.sh` | Ambos terminan en verde y sin advertencias. |
| A2 | Pruebas del cliente HTTP | Cubren las seis rutas, filtros omitidos, cuerpos de alta y edición y transiciones sin cuerpo. |
| A3 | Pruebas de listado y detalle | Cubren combinación de filtros, reinicio de página, estados de carga, vacío y error, y nombres y cantidades del detalle. |
| A4 | Pruebas del formulario y transiciones | Cubren validaciones locales, edición solo en borrador, activación y retiro, conservación del formulario ante `409`, cancelación con Escape y devolución de foco. |
| A5 | Prueba con API real y regresión | La ruta del módulo abre desde el catálogo. Se crea, edita, activa y retira una versión con sesión autenticada. La versión retirada sigue disponible en el historial y las pruebas existentes pasan. |
| A6 | Control visual | Revisar listado, detalle, formulario y diálogo a 375 px y 1280 px en modo claro y oscuro. Adjuntar capturas con controles legibles y sin desbordamiento horizontal de la página. |

## 6. Checklist de ejecución

- [ ] Aprobar la spec con Lukas.
- [ ] Implementar tipos y cliente HTTP con sus pruebas.
- [ ] Implementar pantalla, formulario, transiciones y pruebas de interacción.
- [ ] Registrar la ruta, verificar A1–A6 y documentar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
