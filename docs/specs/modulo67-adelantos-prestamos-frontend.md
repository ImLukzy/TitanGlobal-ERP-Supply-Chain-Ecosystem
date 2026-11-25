# Especificación: modulo67-adelantos-prestamos-frontend — Pantalla de AdelantosPrestamos

> Estado: **aprobada** por Lukas el 2026-11-24 en la revisión final, con las opciones recomendadas de §2; integrada en `main` por el PR #68 y desplegada en la demo.

## 1. Objetivo
**Problema:** `/modulos/67-adelantos-prestamos` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de adelantos y préstamos contra `modulo67-adelantos-prestamos-backend.md` §4 y sus mensajes literales: resumen, listado con filtros, detalle con cronograma de cuotas, alta con capacidad de descuento a la vista y anulación.

## 2. Fuera de alcance
- La API, otras pantallas, componentes compartidos y reglas compartidas de `app.css`. Librerías nuevas.

**Decisiones (opción recomendada aplicada):**
1. **Roles:** sin `RRHH` ni `ADMIN`, «Nuevo préstamo» y «Anular» quedan deshabilitados con «Solo RRHH o ADMIN pueden registrar o anular préstamos.».
2. **Vista previa del cronograma:** el formulario calcula en el navegador las cuotas con la misma regla del backend (redondeo a 2 decimales y última cuota que absorbe la diferencia) y las muestra junto a la cuota máxima de `GET .../capacidad/{empleadoId}`. Si alguna supera el máximo, lo avisa antes de enviar; el servidor sigue siendo quien decide (mensaje 16).
3. **Selector de empleado:** el de la spec de la pantalla del 62.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/adelantos-prestamos/tipos.ts`, `api.ts`, `presentacion.ts`, `validacionPrestamo.ts`, `cronograma.ts`, `usePrestamosData.ts` | crear |
| `frontend/src/modules/adelantos-prestamos/AdelantosPrestamosPage.tsx`, `PrestamosResumen.tsx`, `PrestamosFiltros.tsx`, `PrestamosTabla.tsx`, `PrestamoDetallePanel.tsx`, `PrestamoFormulario.tsx`, `DialogoAnulacionPrestamo.tsx` | crear |
| `frontend/src/modules/adelantos-prestamos/*.test.ts(x)` y `prestamos.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'67-adelantos-prestamos': AdelantosPrestamosPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.adelantos-prestamos-…` al final |

## 4. Diseño y lógica
- **Resumen:** Préstamos activos, Saldo pendiente (moneda PEN), Otorgado este mes, Cuotas del mes.
- **Filtros y tabla:** búsqueda (300 ms), tipo y estado; columnas Número, Empleado, Tipo, Monto, Cuotas, Otorgado, Saldo, Estado (pastilla con texto).
- **Detalle:** datos, motivo, anulación si aplica y tabla de cuotas (N.º, Mes «11/2026», Monto, Descontado, Estado). «Anular» solo si `Activo` y sin descuentos (si no, deshabilitado con el motivo visible); diálogo con motivo (11 y 12 en local, 18 y 19 del servidor).
- **Formulario:** empleado, tipo (con `Adelanto` fija 1 cuota), monto, cuotas, fecha de otorgamiento, primer mes (mes y año), motivo; capacidad y cronograma en vivo (decisión 2); validación local 1 a 12; 409 del servidor junto al formulario conservando lo escrito.
- **Importes:** `formateadorMoneda`; el monto se valida como texto (2 decimales, tope) antes de convertirlo. **Accesibilidad, respuestas obsoletas y responsive:** como la spec de la pantalla del 62.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API y cronograma | tests: las 6 funciones; `cronograma.ts` con redondeo y última cuota | pasan |
| A3 | Pantalla | tests: resumen, filtros, detalle con cuotas, alta con aviso de tope, anulación según estado, roles | pasan |
| A4 | Visual | capturas a 375 y 1280 px, claro y oscuro | en `docs/evidencias/modulo67/` |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api, cronograma y tests.
- [x] Tarea 2: pantalla, formulario y anulación.
- [x] Tarea 3: registro, estilos y A1 a A4 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Gate | lint (0 warnings), typecheck, 912 pruebas en verde y build OK | `npm --prefix frontend run verify` |
| 2026-11-22 | A2 API y cronograma | 6 funciones de `api.ts` y `cronograma.ts` (casos 1000/3, 100/6 y 0.10/4 del backend, cambio de año, suma = monto, tope por mes) | `npx vitest run src/modules/adelantos-prestamos`: 66 de 66, tres ejecuciones seguidas |
| 2026-11-22 | A3 Pantalla | resumen, filtros, detalle con cuotas, alta con aviso de tope, anulación según estado y roles | mismos 66 tests (`AdelantosPrestamosPage.test.tsx`, `PrestamoFormulario.test.tsx`) |
| 2026-11-22 | A4 Visual | pendiente: capturas a 375 y 1280 px, claro y oscuro | adjuntas al PR |
| 2026-11-24 | A4 | OK | 12 capturas (3 escenarios a 375 y 1280 px, claro y oscuro) en `docs/evidencias/modulo67/`, contra API y PostgreSQL locales con el seed: sin desplazamiento horizontal de página, sin errores de consola y Escape cierra los diálogos; detalle en su `README.md` y `resultado-visual.json` |
| 2026-11-24 | Integración y producción | Aprobado | Revisión final de Lukas. `037` aplicado en la demo con copia y ensayo (10 scripts registrados, filas de las 48 tablas existentes sin cambios). PR #68 fusionado con merge commit (`43581cf`); gate de GitHub y local, smoke, Vercel y Render correctos; en producción la pantalla está publicada y `/api/v1/prestamos` responde 200 |
