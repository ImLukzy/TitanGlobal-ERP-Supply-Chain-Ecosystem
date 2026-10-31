# Especificación: 34 - Pantalla de Modulo09_ReclamosPostventa (frontend)

## 1. Objetivo
**Problema:** `/modulos/09-reclamos-postventa` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de reclamos contra el contrato de la spec 33 §4: bandeja con prioridad, plazo y vencidos; detalle con el seguimiento por fechas; acciones según el estado; resumen con cumplimiento de plazos. Patrón y componentes de SolicitudesCompra y OrdenesAbastecimiento.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 33 §4.
- Cambiar otras pantallas, salvo enlazar la orden del reclamo a `/modulos/04-ordenes-compra`.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/reclamos-postventa/{tipos,api}.ts` | crear | 9 funciones (`iniciar` y `cerrar` sin cuerpo) |
| `frontend/src/modules/reclamos-postventa/*.tsx` | crear | partida en componentes (≤ ~300 líneas) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `09-reclamos-postventa` |
| `frontend/src/modules/reclamos-postventa/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** abiertos, en proceso, resueltos, cerrados, rechazados y **vencidos** (destacado con texto); reclamos por tipo; horas promedio de resolución y cumplimiento de plazos («80.00 % atendidos a tiempo»).
- **Filtros:** búsqueda (300 ms), estado, tipo, prioridad, cliente y «Solo vencidos». Página 1; una sola petición al cargar.
- **Tabla (bandeja):** número, cliente, orden, tipo, prioridad con pastilla («Alta», «Media», «Baja» con texto), estado con pastilla, plazo («vence el 17/10/2026» o «Vencido» con texto). Paginación de 10. Carga, vacío y error.
- **Detalle:** datos, descripción, producto si lo hay, enlace a la orden; **línea de tiempo** accesible (lista ordenada) con alta, inicio, resolución y cierre o rechazo con sus fechas (UTC); resolución o motivo; «Atendido a tiempo» / «Atendido fuera de plazo» con texto. Acciones según el estado: Abierto → «Editar», «Iniciar», «Resolver», «Rechazar»; En proceso → «Resolver», «Rechazar»; Resuelto → «Cerrar»; Cerrado y Rechazado → solo lectura.
- **Formulario (alta y edición):** cliente; orden (solo las del cliente elegido, de `GET ordenes-compra/clientes/{id}/jerarquia`); producto opcional (solo los de la orden elegida); tipo; prioridad (muestra el plazo resultante: «Plazo: 2 días»); descripción. Validación local con los literales de la spec 33; errores de la API conservando lo escrito.
- **Diálogos:** iniciar y cerrar con confirmación; resolver con resolución obligatoria; rechazar con motivo obligatorio; 409 dentro del diálogo. Tras cada acción se refrescan tabla, detalle y resumen.
- **Accesibilidad y responsive:** como en las pantallas anteriores.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 9 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Bandeja | tests: filtros y «Solo vencidos» en la query, página 1, una sola petición al cargar, prioridad y «Vencido» con texto, vacío y error | pasan |
| A4 | Formulario | tests: las órdenes se limitan al cliente y los productos a la orden; el plazo cambia con la prioridad; validaciones locales sin petición; 400 conservando lo escrito; edición con PUT | pasan |
| A5 | Acciones y detalle | tests: acciones correctas por estado; iniciar y cerrar sin cuerpo; resolver y rechazar con texto obligatorio; 409 en el diálogo; línea de tiempo y «Atendido a tiempo»; Escape con foco devuelto | pasan |
| A6 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: bandeja con un vencido, detalle con línea de tiempo y diálogo de resolución; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: resumen, filtros y bandeja.
- [ ] Tarea 3: detalle con línea de tiempo, formulario y diálogos.
- [ ] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-31 | Integración en `main` | ✅ | PR #4 · `c2c27fd`, `3994624` · gate 585 backend / 280 frontend · QA visual pendiente |
