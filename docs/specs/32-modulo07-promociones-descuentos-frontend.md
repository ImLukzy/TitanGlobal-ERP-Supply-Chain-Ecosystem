# Especificación: 32 - Pantalla de Modulo07_PromocionesDescuentos (frontend)

## 1. Objetivo
**Problema:** `/modulos/07-promociones-descuentos` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de promociones contra el contrato de la spec 31 §4: mantenimiento (con campos que cambian según el tipo) y un **evaluador de compra** (cliente, fecha y líneas → desglose de descuentos por línea y de compra), con los componentes compartidos y el patrón de ListasPrecios (spec 30).

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 31 §4.
- Cambiar otras pantallas o librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/promociones-descuentos/{tipos,api}.ts` | crear | 7 funciones del contrato (`evaluarCompra` es POST con cuerpo) |
| `frontend/src/modules/promociones-descuentos/*.tsx` | crear | partida en componentes (≤ ~300 líneas); reutiliza el formulario de líneas si encaja |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `07-promociones-descuentos` |
| `frontend/src/modules/promociones-descuentos/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** total, activas, vigentes, de producto, por monto mínimo, de segmento y descuento promedio de las vigentes.
- **Evaluador** (arriba o en una pestaña): cliente, fecha (hoy por defecto) y líneas (producto que precarga su precio, cantidad, precio). Se envía al pulsar «Evaluar». Resultado: tabla por línea (subtotal, promoción aplicada con código, descuento e importe), promoción de compra si aplica, y totales (subtotal, descuento total, total). Sin promociones: mensaje «No hay promociones aplicables» con el total igual al subtotal.
- **Filtros:** búsqueda (300 ms), tipo, segmento y «Solo vigentes». Página 1; una sola petición al cargar.
- **Tabla:** código, nombre, tipo («Producto: Laptop Pro» o «Compra desde S/ 3,000.00»), descuento («10.00 %»), segmento («Todos los clientes» si no tiene), vigencia y estado con pastilla («Vigente», «No vigente», «Inactiva», con texto). Paginación de 10. Carga, vacío y error.
- **Formulario:** código (mayúsculas), nombre, tipo (al cambiar se muestran producto **o** monto mínimo y se limpia el otro), porcentaje, segmento opcional, fechas y «Activa». Validación local con los literales de la spec 31; 409 de código conservando lo escrito. Baja con `DialogoBaja`.
- **Accesibilidad y responsive:** como en las pantallas anteriores.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 7 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Evaluador | tests: no pide nada hasta «Evaluar»; envía cliente, fecha y líneas; muestra el desglose del ejemplo de la spec 31 (Acme 4,037.50) y el caso sin promociones; validaciones locales sin petición; error de la API visible | pasan |
| A4 | Listado, formulario y baja | tests: filtros en la query, página 1, una sola petición al cargar, descripción del tipo y estado con texto; el formulario alterna producto y monto mínimo y limpia el otro; validaciones locales; 409 conservando lo escrito; PUT; baja con Escape | pasan |
| A5 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: evaluador con resultado, formulario de cada tipo y diálogo de baja; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen y evaluador.
- [x] Tarea 3: filtros, tabla, formulario y baja.
- [x] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Integración | ✅ | `d95d88a` + `25a9512` + `27d7ce1` integrados como `44fac5d`, `b0a9ef7` y `94f1d09` (conflicto de registro en `pantallas.ts` con el 06 resuelto conservando ambos); líneas cambiadas idénticas a las revisadas |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` → exit 0: 585 tests del backend y 253 del frontend en verde |
| 2026-10-15 | A2–A4 | ✅ | `api.test.ts` (7 funciones), tests del evaluador (sin petición hasta «Evaluar», Acme → total 4037.50, sin promociones, validaciones y error visible) y del listado, formulario por producto y por monto mínimo, 409 conservando datos, PUT y baja con Escape |
| 2026-10-15 | A5 | ✅ | suite del frontend 10/10 con 4 procesos `yes`; un fallo intermitente ajeno (`AlmacenesPage`) en una serie previa, anotado como pendiente |
| 2026-10-15 | A6 | ✅ | dos correcciones: (1) selects comunes, fechas sin partir, casilla en móvil y título; (2) el cambio de `.tabla-paginada` subía la especificidad y partía cabeceras de otras pantallas («Consum / o» en Contratos): se restauró la regla y la franja oscura de la primera columna se quita con `.tabla-paginada tbody th { background: transparent; }`. Capturas `/tmp/titan-spec32/c1/` y `c2/` (6 pantallas a 1280 claro y oscuro) |
| 2026-10-15 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción: resumen, filtros, evaluador Acme VIP S/ 4,037.50, detalle, CRUD con 409/400 en el diálogo; Contratos, Cotizaciones, ListasPrecios, SolicitudesCompra y Almacenes sin cambios salvo la franja → OK |
| 2026-10-15 | Producción | ✅ | Render `live 94f1d09` y Vercel: `/modulos/07-promociones-descuentos` → 200 con la pantalla registrada en el bundle; `GET /api/v1/promociones/resumen` → 200 |
