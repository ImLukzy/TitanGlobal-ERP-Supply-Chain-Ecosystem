# Especificación: 15 - Pantalla de Modulo13_SolicitudesCompra (frontend)

## 1. Objetivo
**Problema:** `/modulos/13-solicitudes-compra` muestra «Pantalla pendiente».
**Resultado esperado:** una pantalla para registrar solicitudes de compra con varias líneas, consultarlas y aprobarlas o rechazarlas, contra el contrato de la spec 14 §4, con los componentes compartidos (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`, `formatearCantidad`, `lib/formato.ts`).

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 14 §4.
- Cambiar otras pantallas, salvo mejoras de componentes compartidos sin cambiar lo que comprueban sus tests.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/solicitudes-compra/{tipos,api}.ts` | crear | 8 funciones del contrato (`listarSolicitudes(filtros)`, `obtenerSolicitud`, `crearSolicitud`, `actualizarSolicitud`, `aprobarSolicitud`, `rechazarSolicitud`, `eliminarSolicitud`, `obtenerResumenSolicitudes`); filtros vacíos o `undefined` fuera de la query |
| `frontend/src/modules/solicitudes-compra/*.tsx` | crear | página partida en componentes (resumen, filtros, tabla, detalle, formulario de líneas, diálogos); ningún archivo de más de ~300 líneas |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `13-solicitudes-compra` |
| `frontend/src/modules/solicitudes-compra/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos para las esperas |

## 4. Diseño y lógica
- **Resumen:** pendientes, aprobadas, rechazadas, monto pendiente y monto aprobado (PEN); top de productos solicitados.
- **Filtros:** búsqueda por número o solicitante (300 ms), estado («Todos», «Pendiente», «Aprobada», «Rechazada»), proveedor (de `GET proveedores?activo=true&tamanoPagina=100`, con «Todos») y rango de fechas (validación local «La fecha desde no puede ser posterior a la fecha hasta.» sin petición). Cualquier cambio vuelve a la página 1.
- **Tabla:** número, fecha (UTC, `formatearFecha`), solicitante, proveedor («Sin proveedor»), estado como etiqueta con texto, monto estimado. Paginación de 10. Estados de carga, vacío y error.
- **Detalle:** cabecera, líneas (producto, cantidad, precio estimado, subtotal), monto total, observaciones, decisión (fecha y motivo si fue rechazada). Si está Pendiente: botones «Editar», «Aprobar», «Rechazar» y «Eliminar»; si no, solo lectura.
- **Formulario (alta y edición):** solicitante, proveedor opcional (activos), observaciones y líneas dinámicas (producto de `GET productos?tamanoPagina=100`, cantidad, precio estimado; añadir y quitar líneas; total calculado en vivo). Validación local con los literales de la spec 14 §4 por campo y por línea (`aria-describedby`). Errores 400/409 de la API visibles sin perder lo escrito.
- **Aprobar:** confirmación accesible. **Rechazar:** diálogo con motivo obligatorio (validado en local). **Eliminar:** `DialogoBaja`. Un 409 se muestra dentro del diálogo. Tras cada acción se refrescan tabla, detalle y resumen.
- **Accesibilidad y responsive:** como en las specs 08, 10 y 13; a 375 px las líneas del formulario se apilan.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 8 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Filtros y tabla | tests: espera con temporizadores falsos, estado, proveedor y fechas en la query, rango inválido sin petición, vuelta a la página 1, vacío y error | pasan |
| A4 | Formulario | tests: añadir y quitar líneas con total en vivo; cada validación local sin petición; 400 de la API conservando lo escrito; edición precargada y PUT con su id | pasan |
| A5 | Decisiones | tests: aprobar → POST `/aprobar` y refresco; rechazar sin motivo → error local; con motivo → POST `/rechazar` con `{ motivo }`; 409 en el diálogo; solicitud aprobada sin botones de acción; Escape con foco devuelto | pasan |
| A6 | Sin regresiones | tests de las demás pantallas en verde; la suite pasa 10 veces seguidas | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: detalle pendiente, formulario con 3 líneas y diálogo de rechazo; márgenes ≥ 16 px, sin scroll horizontal ni celdas recortadas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: resumen, filtros y tabla.
- [ ] Tarea 3: detalle, formulario de líneas y diálogos.
- [ ] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
