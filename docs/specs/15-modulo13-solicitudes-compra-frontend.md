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
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla.
- [x] Tarea 3: detalle, formulario de líneas y diálogos.
- [x] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | Corrección 1 | ✅ | Claudio devolvió la entrega: «El producto 0 no existe.» para una línea sin producto (ahora «Selecciona un producto.»), columna Subtotal recortada a 1280 en el detalle (ahora 323/323 px) y captura de 3 líneas vacías (ahora con total S/ 4,800.00) |
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`4b2e774`, mismas líneas que el auditado `0b1a22d`): backend 151/151, frontend 120/120 |
| 2026-10-14 | A2–A6 | ✅ | tests de `solicitudes-compra/` (api, página, formulario y decisiones) con temporizadores falsos; la suite pasó 10/10 seguidas según Dwight |
| 2026-10-14 | A7 | ✅ | `/tmp/titan-spec15/` (12 capturas) revisadas por Claudio: detalle sin recortes a 1280, formulario con 3 líneas y total, diálogo de rechazo; márgenes 16 px |
| 2026-10-14 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO |
| 2026-10-14 | QA E2E | ✅ | Pam en local (Vite 5174 + API + PostgreSQL, `9e21623`) y en producción (https://titanglobal-erp.vercel.app, solo lectura): resumen del seed 2/2/1; filtros por número, solicitante, estado, proveedor y fechas; detalle con subtotales y rechazada sin acciones; alta con 3 líneas → `SC-000NNN`, validaciones locales sin petición, proveedor inactivo fuera del selector; edición quitando una línea; aprobar; rechazar sin motivo (error local) y con motivo; eliminar con Escape; demás pantallas sin regresiones; 13 capturas sin recortes ni scroll horizontal |
| 2026-10-14 | Pendiente menor | ℹ️ | visto por Claudio en la captura de producción: el estado vacío del detalle («Selecciona una solicitud para ver el detalle.») usa un tamaño de letra mayor que el resto de la pantalla |
