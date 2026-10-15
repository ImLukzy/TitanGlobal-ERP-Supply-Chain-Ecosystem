# Especificación: 17 - Pantalla de Modulo15_OrdenesAbastecimiento (frontend)

## 1. Objetivo
**Problema:** `/modulos/15-ordenes-abastecimiento` muestra «Pantalla pendiente», y desde una solicitud aprobada no se puede generar su orden.
**Resultado esperado:** pantalla de órdenes de abastecimiento contra el contrato de la spec 16 §4, con el patrón y los componentes de SolicitudesCompra (spec 15), y un acceso «Generar orden» en el detalle de una solicitud aprobada.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 16 §4.
- Cambiar otras pantallas salvo el botón «Generar orden» en el detalle de SolicitudesCompra.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/ordenes-abastecimiento/{tipos,api}.ts` | crear | 8 funciones del contrato (`listarOrdenesAbastecimiento`, `obtenerOrdenAbastecimiento`, `crearOrdenAbastecimiento`, `generarOrdenDesdeSolicitud`, `actualizarOrdenAbastecimiento`, `recibirOrdenAbastecimiento`, `anularOrdenAbastecimiento`, `obtenerResumenOrdenesAbastecimiento`) |
| `frontend/src/modules/ordenes-abastecimiento/*.tsx` | crear | partida en componentes (≤ ~300 líneas por archivo); reutiliza el formulario de líneas de SolicitudesCompra si puede compartirse sin cambiar sus tests |
| `frontend/src/modules/solicitudes-compra/SolicitudDetallePanel.tsx` (+ test) | modificar | botón «Generar orden» solo en solicitudes aprobadas: abre un diálogo (proveedor preseleccionado con el sugerido, fecha de entrega, observaciones) y, al confirmar, navega a `/modulos/15-ordenes-abastecimiento` con la orden nueva abierta |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `15-ordenes-abastecimiento` |
| `frontend/src/modules/ordenes-abastecimiento/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** emitidas, atrasadas, recibidas, anuladas, monto por recibir y monto recibido (PEN); top de proveedores por monto.
- **Filtros:** búsqueda por número o proveedor (300 ms), estado, proveedor y rango de fechas (validación local sin petición). Cualquier cambio vuelve a la página 1. Se puede abrir una orden concreta por la query `?orden={id}`.
- **Tabla:** número, fecha, proveedor, solicitud de origen («Manual» si no tiene), entrega estimada con etiqueta «Atrasada» (texto, no solo color), estado y monto. Paginación de 10. Carga, vacío y error.
- **Detalle:** cabecera, líneas con subtotales, total, observaciones, recepción (fecha) o anulación (motivo). Si está Emitida: «Editar», «Marcar recibida», «Anular»; si no, solo lectura.
- **Formulario (alta manual y edición):** proveedor (activos), fecha de entrega (no anterior a hoy), observaciones y líneas dinámicas con total en vivo. Validación local con los literales de la spec 16 §4; errores 400/409 de la API visibles conservando lo escrito.
- **Recibir:** confirmación accesible. **Anular:** diálogo con motivo obligatorio. 409 dentro del diálogo. Tras cada acción se refrescan tabla, detalle y resumen.
- **Accesibilidad y responsive:** como en la spec 15.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 8 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Filtros y tabla | tests: espera con temporizadores falsos, filtros en la query, rango inválido sin petición, página 1, etiqueta «Atrasada», `?orden=` abre el detalle, vacío y error | pasan |
| A4 | Formulario y acciones | tests: líneas con total, validaciones locales sin petición, 400 conservando lo escrito, edición con PUT, recibir, anular sin motivo (error local) y con motivo, 409 en el diálogo, orden no emitida sin acciones, Escape con foco devuelto | pasan |
| A5 | Generar desde solicitud | tests en SolicitudesCompra: el botón solo aparece en aprobadas; POST `desde-solicitud/{id}` con el cuerpo exacto; 409 visible en el diálogo; navegación a la orden creada | pasan |
| A6 | Sin regresiones | tests de las demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: detalle de una orden emitida atrasada, formulario con 3 líneas, diálogo de anulación y diálogo «Generar orden» en SolicitudesCompra; márgenes ≥ 16 px, sin scroll horizontal ni celdas recortadas | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla.
- [x] Tarea 3: detalle, formulario y diálogos.
- [x] Tarea 4: «Generar orden» en SolicitudesCompra.
- [x] Tarea 5: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | Revisión del integrador | ⚠️ | Claudio vio en la entrega «1 órdenes» en el resumen y que `limpiarCargaInicial` de Productos esperaba 2 peticiones del listado al cargar: consolidaba un bug real (petición duplicada en Productos y SolicitudesCompra por el efecto de búsqueda) |
| 2026-10-14 | Auditoría ciclo 1 | ⚠️ | Kelly: CAMBIOS por esos dos puntos |
| 2026-10-14 | Correcciones | ✅ | Dwight: `425f509` (una sola petición al cargar, con test por pantalla; 10/10 sin carga y 10/10 con carga) y `60de5db` (plurales con `formatearCantidad`) |
| 2026-10-14 | Auditoría ciclo 2 | ✅ | Kelly: APROBADO |
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`1e5611c`, `425f509`, `60de5db`): backend 175/175, frontend 147/147 |
| 2026-10-14 | A2–A6 | ✅ | tests de `ordenes-abastecimiento/` (página, formulario, acciones) y `SolicitudesGenerarOrden.test.tsx` (botón solo en aprobadas, POST `desde-solicitud`, 409, navegación) |
| 2026-10-14 | A7 | ✅ | `/tmp/titan-spec17/` (16 capturas) revisadas por Claudio: etiqueta «Atrasada» con texto, sin celdas recortadas a 1280, márgenes 16 px |
