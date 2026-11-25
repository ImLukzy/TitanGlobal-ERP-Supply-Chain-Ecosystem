# Especificación: modulo66-planillas-frontend — Pantalla de Planillas

> Estado: **aprobada** por Lukas el 2026-11-24 en la revisión final, con las opciones recomendadas de §2; integrada en `main` por el PR #69 y desplegada en la demo.

## 1. Objetivo
**Problema:** `/modulos/66-planillas` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con tres vistas, **Planillas**, **Bonos** y **Afiliaciones** (`?vista=`), contra `modulo66-planillas-backend.md` §4 y sus mensajes literales: crear, calcular, aprobar y pagar planillas, ver el detalle por empleado con incidencias, imprimir la boleta, registrar bonos y afiliaciones, y consultar los parámetros.

## 2. Fuera de alcance
- La API, otras pantallas, componentes compartidos y reglas compartidas de `app.css`. PDF y librerías nuevas.

**Decisiones (opción recomendada aplicada):**
1. **Roles:** sin `RRHH` ni `ADMIN`, las escrituras quedan deshabilitadas con «Solo RRHH o ADMIN pueden modificar planillas.».
2. **Boleta:** un diálogo con la boleta (`GET .../boletas/{detalleId}`) maquetada para imprimir y un botón «Imprimir» (`window.print()`), con reglas `@media print` propias del módulo que ocultan el resto de la aplicación solo mientras la boleta está abierta.
3. **Confirmaciones:** aprobar y registrar el pago piden confirmación en un diálogo («La planilla aprobada no se puede modificar.»).

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/planillas/tipos.ts`, `api.ts`, `presentacion.ts`, `validacionPlanilla.ts` | crear |
| `frontend/src/modules/planillas/usePlanillasData.ts`, `useBonosData.ts`, `useAfiliacionesData.ts` | crear |
| `frontend/src/modules/planillas/PlanillasPage.tsx` y componentes de cada vista (resumen, filtros, tabla, detalle con tabla de empleados, diálogo de creación, diálogo de confirmación, diálogo de pago, boleta, formularios de bono y de afiliación, panel de parámetros) | crear |
| `frontend/src/modules/planillas/*.test.ts(x)` y `planillas.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'66-planillas': PlanillasPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.planillas-…` al final (incluido `@media print` con prefijo del módulo) |

## 4. Diseño y lógica
- **Vista Planillas:** resumen (último periodo, estado, neto, empleados, incidencias, pendientes de aprobar); filtros año, tipo y estado; tabla Número, Periodo («11/2026»), Tipo, Estado (pastilla), Empleados, Neto. Detalle: cabecera con totales (ingresos, descuentos, neto, aportes del empleador) y acciones según estado: «Calcular» / «Recalcular» (`Borrador`, `Calculada`), «Aprobar» (`Calculada`), «Registrar pago» (`Aprobada`, con fecha). Debajo, tabla paginada de empleados (Legajo, Empleado, Días, Ingresos, Pensión, Quinta, Préstamos, Neto, Incidencia) con búsqueda y «Solo incidencias», y «Ver boleta» en las filas con número de boleta. «Nueva planilla»: año, mes y tipo (mensajes 1 a 6). «Parámetros»: panel de solo lectura con UIT, tasas, pensiones y tramos.
- **Vista Bonos:** filtros año, mes y empleado; tabla y alta (empleado, año, mes, tipo, concepto, monto; mensajes 16 a 19); «Eliminar» con `DialogoBaja` (21 del servidor).
- **Vista Afiliaciones:** tabla de empleados con su sistema (o «Sin afiliación» destacado con texto) y formulario de alta o cambio (sistema, CUSPP solo para AFP, fecha; mensajes 22 a 25).
- **Importes** con `formateadorMoneda`, validados como texto antes de convertir. **Accesibilidad, respuestas obsoletas y responsive:** como la spec de la pantalla del 62; las tablas anchas usan el desplazamiento interno de `TablaPaginada`.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API | `api.test.ts`: todas las funciones | pasan |
| A3 | Planillas | tests: crear, calcular, aprobar con confirmación, pago, acciones según estado, detalle de empleados con incidencias, boleta e impresión | pasan |
| A4 | Bonos y afiliaciones | tests: validación local, alta, eliminación, CUSPP solo en AFP | pasan |
| A5 | Roles y pestañas | tests | pasan |
| A6 | Visual | capturas a 375 y 1280 px, claro y oscuro, incluida la boleta | en `docs/evidencias/modulo66/` |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api y tests.
- [x] Tarea 2: vista Planillas, detalle y boleta.
- [x] Tarea 3: vistas Bonos y Afiliaciones.
- [x] Tarea 4: registro, estilos y A1 a A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | `npm --prefix frontend run verify` en verde: lint con 0 warnings, tipos, 981 pruebas en 103 archivos (69 en 7 archivos del módulo) y build | `npm --prefix frontend run verify` |
| 2026-11-22 | A2 | `api.test.ts`: las 15 funciones de `api.ts` con ruta, método, cuerpo y query exactos | `npx vitest run src/modules/planillas` (3 ejecuciones seguidas, 69 de 69) |
| 2026-11-22 | A3 | pruebas de crear (mensajes 1 a 4, 7 y errores del servidor), calcular, aprobar con confirmación, pago, acciones por estado, detalle de empleados con incidencias, boleta e impresión (`window.print`, clase de impresión solo con la boleta abierta) | `PlanillasVista.test.tsx`, `PlanillaDetalle.test.tsx` |
| 2026-11-22 | A4 | pruebas de validación local (mensajes 16, 1, 2, 3, 18, 19, 22, 24, 25), alta, eliminación con `DialogoBaja` y CUSPP solo en AFP (no se envía en ONP) | `BonosVista.test.tsx`, `AfiliacionesVista.test.tsx`, `validacionPlanilla.test.ts` |
| 2026-11-22 | A5 | pestañas `?vista=` (clic, flechas, valor desconocido) y roles RRHH, ADMIN, otro rol y sin roles | `PlanillasPage.test.tsx` |
| 2026-11-22 | A6 | pendiente: capturas | a 375 y 1280 px, claro y oscuro, incluida la boleta |
| 2026-11-24 | A6 | OK | 16 capturas (4 escenarios, incluida la boleta, a 375 y 1280 px, claro y oscuro) en `docs/evidencias/modulo66/`, contra API y PostgreSQL locales con el seed: sin desplazamiento horizontal de página, sin errores de consola y Escape cierra los diálogos; detalle en su `README.md` y `resultado-visual.json` |
| 2026-11-24 | Integración y producción | Aprobado | Revisión final de Lukas. `038` aplicado en la demo con copia y ensayo (11 scripts registrados, filas de las 51 tablas existentes sin cambios). PR #69 fusionado con merge commit (`bc93887`); gate de GitHub y local, smoke, Vercel y Render correctos; en producción la pantalla está publicada y `/api/v1/planillas` responde 200 y `/api/v1/planillas/parametros` devuelve los 7 parámetros, los 5 sistemas de pensiones y los 5 tramos cargados por el script |
