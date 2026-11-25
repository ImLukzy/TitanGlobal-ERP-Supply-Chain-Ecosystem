# Especificación: modulo69-capacitaciones-frontend — Pantalla de Capacitaciones

> Estado: **aprobada** por Lukas el 2026-11-24 en la revisión final, con las opciones recomendadas de §2; integrada en `main` por el PR #71 y desplegada en la demo.

## 1. Objetivo
**Problema:** `/modulos/69-capacitaciones` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con tres vistas, **Programaciones**, **Cursos** y **Horas por empleado** (`?vista=`), contra `modulo69-capacitaciones-backend.md` §4 y sus mensajes literales.

## 2. Fuera de alcance
- La API, otras pantallas, componentes compartidos y reglas compartidas de `app.css`. Librerías nuevas.

**Decisiones (opción recomendada aplicada):**
1. **Roles:** sin `RRHH` ni `ADMIN`, las escrituras quedan deshabilitadas con «Solo RRHH o ADMIN pueden modificar capacitaciones.».
2. **Año:** selector de año (por defecto el actual, en UTC) que filtra el resumen, las programaciones y las horas.
3. **Resultados:** en una programación `Realizada`, una tabla editable con «Aprobado» / «Desaprobado» y la nota por inscrito, y un único «Guardar resultados».

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/capacitaciones/tipos.ts`, `api.ts`, `presentacion.ts`, `validacionCapacitacion.ts`, `useProgramacionesData.ts`, `useCursosData.ts`, `useHorasData.ts` | crear |
| `frontend/src/modules/capacitaciones/CapacitacionesPage.tsx` y componentes de cada vista (resumen, filtros, tablas, detalle de programación con inscritos y cupo, formularios de curso y de programación, diálogo de inscripción, tabla de resultados, diálogo de certificación, ficha de empleado) | crear |
| `frontend/src/modules/capacitaciones/*.test.ts(x)` y `capacitaciones.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'69-capacitaciones': CapacitacionesPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.capacitaciones-…` al final |

## 4. Diseño y lógica
- **Resumen** del año. **Programaciones:** filtros curso y estado; tabla Curso, Fechas, Lugar, Inscritos / Cupo, Estado. Detalle con inscritos (estado, nota, certificación y «Vencida» con texto), acciones por estado: «Inscribir» (empleados activos; deshabilitado sin cupo con el motivo), «Retirar», «Marcar realizada», «Cancelar» (con confirmación), resultados (decisión 3) y «Emitir certificado» por inscrito aprobado.
- **Cursos:** filtros (búsqueda, tipo, activo), tabla, alta y edición, activar y desactivar.
- **Horas por empleado:** tabla Legajo, Empleado, Cursos aprobados, Horas del año, con búsqueda; al elegir uno, su ficha con cursos y certificaciones.
- **Accesibilidad, respuestas obsoletas y responsive:** como la spec de la pantalla del 62.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API | `api.test.ts` | pasan |
| A3 | Programaciones | tests: inscripción con cupo, retiro, realizada, resultados, certificación, cancelación | pasan |
| A4 | Cursos y horas | tests | pasan |
| A5 | Roles y pestañas | tests | pasan |
| A6 | Visual | capturas a 375 y 1280 px, claro y oscuro | en `docs/evidencias/modulo69/` |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api y tests.
- [x] Tarea 2: vista Programaciones.
- [x] Tarea 3: vistas Cursos y Horas.
- [x] Tarea 4: registro, estilos y A1 a A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Gate | Verde, 0 warnings | `npm --prefix frontend run verify`: lint, typecheck y build correctos; 114 archivos y 1102 pruebas superadas |
| 2026-11-22 | A2 API | Pasan | `api.test.ts` (URL, método, query sin filtros vacíos y cuerpos exactos de las 17 funciones) y `validacionCapacitacion.test.ts` |
| 2026-11-22 | A3 Programaciones | Pasan | `ProgramacionesVista.test.tsx` (21 pruebas): inscripción con cupo y sin cupo, retiro, realizada, cancelación con confirmación, resultados, certificación y alta |
| 2026-11-22 | A4 Cursos y horas | Pasan | `CursosVista.test.tsx` (10) y `HorasVista.test.tsx` (5): filtros, búsqueda a 300 ms, alta, edición, activar/desactivar y ficha del empleado |
| 2026-11-22 | A5 Roles y pestañas | Pasan | `CapacitacionesPage.test.tsx` (7): `?vista=`, flechas, selector de año, resumen y escrituras deshabilitadas sin RRHH ni ADMIN; `npx vitest run src/modules/capacitaciones` ×3 (59 pruebas cada vez) |
| 2026-11-22 | A6 Visual | pendiente: capturas | capturas a 375 y 1280 px, claro y oscuro, por adjuntar al PR |
| 2026-11-24 | A6 | OK | 12 capturas (3 escenarios a 375 y 1280 px, claro y oscuro) en `docs/evidencias/modulo69/`, contra API y PostgreSQL locales con el seed: sin desplazamiento horizontal de página, sin errores de consola y Escape cierra los diálogos; detalle en su `README.md` y `resultado-visual.json` |
| 2026-11-24 | Integración y producción | Aprobado | Revisión final de Lukas. `040` aplicado en la demo con copia y ensayo (13 scripts registrados, filas de las 62 tablas existentes sin cambios). PR #71 fusionado con merge commit (`c8e197d`); gate de GitHub y local, smoke, Vercel y Render correctos; en producción la pantalla está publicada y `/api/v1/cursos`, `/api/v1/programaciones-curso` y `/api/v1/capacitaciones/resumen` responden 200 |
