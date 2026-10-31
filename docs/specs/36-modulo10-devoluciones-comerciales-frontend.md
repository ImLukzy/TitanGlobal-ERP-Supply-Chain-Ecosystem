# Especificación: 36 - Pantalla de Modulo10_DevolucionesComerciales (frontend)

## 1. Objetivo
**Problema:** `/modulos/10-devoluciones-comerciales` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de devoluciones contra el contrato de la spec 35 §4: bandeja con estados, detalle con importes, formulario que muestra lo disponible por producto de la orden, acciones según el estado y resumen con tasa de devolución. Patrón de ReclamosPostventa (spec 34).

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 35 §4.
- Cambiar otras pantallas, salvo enlazar la orden y el reclamo de la devolución a sus pantallas.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/devoluciones-comerciales/{tipos,api}.ts` | crear | 10 funciones (`aprobar` y `recibir` sin cuerpo) |
| `frontend/src/modules/devoluciones-comerciales/*.tsx` | crear | partida en componentes (≤ ~300 líneas) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `10-devoluciones-comerciales` |
| `frontend/src/modules/devoluciones-comerciales/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** solicitadas, aprobadas, recibidas, rechazadas, monto reembolsado (PEN), tasa de devolución («1.25 % de las unidades vendidas») y devoluciones por motivo.
- **Filtros:** búsqueda (300 ms), estado, motivo y cliente. Página 1; una sola petición al cargar.
- **Tabla:** número, cliente, orden, reclamo (si lo hay), motivo, unidades, total y estado con pastilla. Paginación de 10. Carga, vacío y error.
- **Detalle:** datos, líneas con importe, total, observaciones, motivo de rechazo y fechas de aprobación, recepción o rechazo; enlaces a la orden y al reclamo. Acciones según el estado: Solicitada → «Editar», «Aprobar», «Rechazar», «Eliminar»; Aprobada → «Marcar recibida», «Rechazar»; Recibida y Rechazada → solo lectura.
- **Formulario:** cliente; orden (solo las del cliente); reclamo opcional (solo los de esa orden); motivo; observaciones; al elegir la orden se carga `ordenes/{id}/disponible` y se muestra una tabla con cada producto (comprado, ya devuelto, disponible, precio) y un campo de cantidad a devolver (0 a disponible); importe y total en vivo. Validación local con los literales de la spec 35 («Solo quedan … por devolver…» si se supera lo disponible); errores de la API conservando lo escrito.
- **Diálogos:** aprobar y recibir con confirmación; rechazar con motivo obligatorio; eliminar con `DialogoBaja`; 409 dentro del diálogo. Tras cada acción se refrescan tabla, detalle y resumen.
- **Accesibilidad y responsive:** como en las pantallas anteriores.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 10 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Bandeja | tests: filtros en la query, página 1, una sola petición al cargar, estado con texto, vacío y error | pasan |
| A4 | Formulario | tests: órdenes del cliente y reclamos de la orden; carga del disponible al elegir la orden; superar lo disponible → error local sin petición; importes y total en vivo; validaciones locales; 409 de la API conservando lo escrito; edición con PUT | pasan |
| A5 | Acciones | tests: acciones correctas por estado; aprobar y recibir sin cuerpo; rechazar con motivo obligatorio; 409 en el diálogo; Escape con foco devuelto | pasan |
| A6 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: formulario con la tabla de disponibles, detalle y diálogo de rechazo; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: resumen, filtros y tabla.
- [ ] Tarea 3: detalle, formulario con disponibles y diálogos.
- [ ] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-31 | Integración en `main` | ✅ | PR #5 · `0941fd8`, `1bb28b9` · gate 585 backend / 293 frontend · QA visual pendiente |
