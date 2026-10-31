# Especificación: 40 - Pantalla de Modulo08_ComisionesVendedores (frontend)

## 1. Objetivo
**Problema:** `/modulos/08-comisiones-vendedores` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con tres pestañas contra el contrato de la spec 39 §4: **Vendedores** (mantenimiento), **Asignación de ventas** (órdenes con su vendedor, asignar y reasignar) y **Comisiones del mes** (cálculo, liquidar y pagar), con los componentes compartidos y el patrón de SegmentacionClientes (pestañas, spec 24).

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 39 §4.
- Cambiar otras pantallas o librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/comisiones-vendedores/{tipos,api}.ts` | crear | 11 funciones del contrato (`pagar` sin cuerpo) |
| `frontend/src/modules/comisiones-vendedores/*.tsx` | crear | partida en componentes (≤ ~300 líneas) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `08-comisiones-vendedores` |
| `frontend/src/modules/comisiones-vendedores/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos, función `esperarCargaInicial` |

## 4. Diseño y lógica
- **Pestañas accesibles** (`role="tablist"`, flechas) reflejadas en la URL (`?vista=`).
- **Vendedores:** búsqueda (300 ms) y estado; tabla con código, nombre, comisión («5.00 %»), órdenes asignadas y estado con pastilla; detalle con email y ventas totales; formulario con los literales de la spec 39; baja con `DialogoBaja` (409 «tiene ventas asignadas» en el diálogo).
- **Asignación de ventas:** filtros por mes (selector año/mes), vendedor y «Solo sin asignar»; tabla con orden, fecha, cliente, total y vendedor («Sin asignar» con texto); en cada fila un selector de vendedor (solo activos) para asignar o reasignar y una acción «Quitar»; el 409 de mes liquidado se muestra en la fila o en un aviso accesible sin perder el resto de la tabla.
- **Comisiones del mes:** selector año/mes; tabla por vendedor (órdenes, ventas, porcentaje, comisión, estado con pastilla «Pendiente», «Calculada», «Pagada») y totales; botón «Liquidar mes» (solo si no está liquidado, con confirmación) y «Marcar pagada» por fila calculada; 409 dentro del diálogo; aviso «Mes liquidado: los importes ya no cambian» cuando corresponda.
- Una sola petición por listado al cargar cada pestaña; estados de carga, vacío y error.
- **Accesibilidad y responsive:** como en las pantallas anteriores (estilos y componentes compartidos; captions pequeños; sin controles con el aspecto por defecto del navegador).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 11 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Vendedores | tests: filtros, formulario con validaciones locales, 409 de código conservando lo escrito, PUT, baja con 409 en el diálogo, Escape | pasan |
| A4 | Asignación | tests: filtros de mes, vendedor y sin asignar en la query; asignar y reasignar con PUT y el cuerpo exacto; quitar; solo vendedores activos en el selector; 409 de mes liquidado visible | pasan |
| A5 | Comisiones | tests: tabla y totales del mes; «Liquidar mes» solo si no está liquidado y con confirmación; liquidar dos veces → 409 en el diálogo; «Marcar pagada» sin cuerpo y refresco | pasan |
| A6 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas con carga | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: las tres pestañas y el diálogo de liquidar; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: pestaña Vendedores.
- [ ] Tarea 3: pestañas Asignación y Comisiones.
- [ ] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-31 | Integración en `main` | ✅ | PR #3 · `0772733` · gate 585 backend / 270 frontend · QA visual pendiente |
