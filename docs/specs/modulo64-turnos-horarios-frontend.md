# Especificación: modulo64-turnos-horarios-frontend — Pantalla de TurnosHorarios

> Estado: **aprobada** por Lukas el 2026-11-24 en la revisión final, con las opciones recomendadas de §2; integrada en `main` por el PR #65 y desplegada en la demo.

## 1. Objetivo
**Problema:** `/modulos/64-turnos-horarios` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con dos vistas, **Turnos** y **Asignaciones** (`?vista=turnos|asignaciones`, como Roles y Permisos), contra el contrato de `modulo64-turnos-horarios-backend.md` §4 y con sus mensajes literales. Mismo diseño común que Empleados y Contratos laborales.

## 2. Fuera de alcance
- La API (spec backend). Marcaciones (63). Cambios en otras pantallas, componentes compartidos o reglas compartidas de `app.css`. Librerías nuevas.

**Decisiones (opción recomendada aplicada):**
1. **Acciones según el rol:** como en el 62, sin `RRHH` ni `ADMIN` en `sesion.roles` los botones de escritura quedan deshabilitados con el texto visible «Solo RRHH o ADMIN pueden modificar turnos.».
2. **Programación:** el detalle de una asignación muestra los próximos 14 días del empleado (`GET .../programacion` desde hoy), con fecha, día de la semana, turno, horario y «Laborable» / «Descanso» / «Sin turno» / «Cesado» como texto.
3. **Selector de empleado:** el mismo de la spec del 62 (activos, hasta 100, con aviso si hay más).

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/turnos-horarios/tipos.ts`, `api.ts`, `presentacion.ts`, `validacionTurno.ts` | crear |
| `frontend/src/modules/turnos-horarios/useTurnosData.ts`, `useAsignacionesTurnoData.ts` | crear |
| `frontend/src/modules/turnos-horarios/TurnosHorariosPage.tsx` (pestañas) | crear |
| `frontend/src/modules/turnos-horarios/TurnosVista.tsx`, `TurnosResumen.tsx`, `TurnosFiltros.tsx`, `TurnosTabla.tsx`, `TurnoDetallePanel.tsx`, `TurnoFormulario.tsx` | crear |
| `frontend/src/modules/turnos-horarios/AsignacionesVista.tsx`, `AsignacionesFiltros.tsx`, `AsignacionesTabla.tsx`, `AsignacionDetallePanel.tsx`, `AsignacionFormulario.tsx`, `DialogoFinalizacion.tsx` | crear |
| `frontend/src/modules/turnos-horarios/*.test.ts(x)` y `turnosHorarios.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'64-turnos-horarios': TurnosHorariosPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.turnos-horarios-…` al final |

## 4. Diseño y lógica
- **api.ts:** una función por ruta de la spec backend (11), omitiendo filtros vacíos; `eliminarAsignacionTurno` usa `apiDelete`; activar/desactivar usan `apiPostSinCuerpo`.
- **Vista Turnos:** resumen (`/turnos/resumen`: Turnos activos, Fijos, Rotativos, Empleados sin turno hoy); filtros (búsqueda 300 ms, tipo, `Activos`/`Inactivos`/`Todos`); tabla Código, Nombre, Tipo, Horario («22:00 – 06:00 (+1)» si cruza medianoche), Tolerancia, Días o ciclo («L M X J V» o «4 x 2»), Estado (pastilla con texto). Detalle con todos los campos, asignaciones totales y empleados con turno hoy; botones «Editar» (deshabilitado con «El turno tiene asignaciones: crea uno nuevo.» si `totalAsignaciones > 0`) y «Desactivar»/«Activar».
- **Formulario de turno:** código, nombre, tipo (radio), entrada y salida (`type="time"`), casilla «Cruza medianoche» (se marca sola si la salida es anterior a la entrada y se puede revisar), tolerancia; si `Fijo`, siete casillas de días; si `Rotativo`, días de trabajo, días de descanso e inicio de ciclo. Validación local con los mensajes 1 a 19; 409 (20, 22) junto al formulario conservando lo escrito.
- **Vista Asignaciones:** filtros por empleado, turno y estado (`Vigente`, `Futura`, `Finalizada`); tabla Empleado, Turno, Inicio, Fin («Sin fin»), Estado. Detalle con la programación (decisión 2) y botones «Fijar fin» (diálogo con fecha; 28, 36, 37) y «Eliminar» (solo `Futura`, con `DialogoBaja`). Formulario de alta: empleado, turno (solo activos), inicio y fin opcional; validación local 25 a 28; avisa con texto que la asignación abierta anterior se cerrará el día antes.
- **Fechas y horas:** fechas `yyyy-MM-dd` con `formatearFecha`; horas mostradas `HH:mm` y enviadas `HH:mm:ss`; sin `Date` local.
- **Accesibilidad, respuestas obsoletas y responsive:** como la spec de la pantalla del 62.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API | `api.test.ts`: URL, método, query y cuerpo de las 11 funciones | pasan |
| A3 | Turnos | tests: filtros, resumen, horario nocturno «(+1)», validación 1 a 19 sin petición, 409 conservando lo escrito, edición bloqueada con asignaciones, activar/desactivar | pasan |
| A4 | Asignaciones | tests: filtros, programación de 14 días con textos, alta (25 a 28), fijar fin, eliminar solo futuras | pasan |
| A5 | Roles y pestañas | tests: botones deshabilitados sin rol; `?vista=` cambia de vista | pasan |
| A6 | Sin regresiones | suite completa 3 veces seguidas | pasa |
| A7 | Visual | capturas a 375 y 1280 px, claro y oscuro | en `docs/evidencias/modulo64/` |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api y tests de api.
- [x] Tarea 2: vista Turnos.
- [x] Tarea 3: vista Asignaciones y programación.
- [x] Tarea 4: registro, estilos y A1 a A7 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | OK | `npm --prefix frontend run verify`: lint 0 warnings, typecheck, 79 archivos y 695 pruebas superadas, build en verde |
| 2026-11-22 | A2 | OK | `api.test.ts`: URL, método, query y cuerpo de las 12 funciones (11 rutas) pasan |
| 2026-11-22 | A3 | OK | `TurnosVista.test.tsx`, `TurnoFormulario.test.tsx` y `validacionTurno.test.ts`: filtros, resumen, «(+1)», mensajes 1 a 19 sin petición, 409 conservando lo escrito, edición bloqueada con asignaciones, activar/desactivar |
| 2026-11-22 | A4 | OK | `AsignacionesVista.test.tsx`: filtros, programación de 14 días con textos, alta (25 a 28), fijar fin (36, 28, 37), eliminar solo futuras |
| 2026-11-22 | A5 | OK | pruebas de rol (`CONSULTA` deshabilita; `RRHH` y `ADMIN` habilitan) y de pestañas (`?vista=asignaciones`, clic y flechas) |
| 2026-11-22 | A6 | OK | `npx vitest run src/modules/turnos-horarios` 3 veces seguidas: 5 archivos y 95 pruebas superadas en cada una; suite completa 695 superadas en `verify` |
| 2026-11-22 | A7 | pendiente: capturas | capturas a 375 y 1280 px, claro y oscuro, por adjuntar al PR |
| 2026-11-24 | A7 | OK | 12 capturas (3 escenarios a 375 y 1280 px, claro y oscuro) en `docs/evidencias/modulo64/`, contra API y PostgreSQL locales con el seed: sin desplazamiento horizontal de página, sin errores de consola y Escape cierra los diálogos; detalle en su `README.md` y `resultado-visual.json` |
| 2026-11-24 | Integración y producción | Aprobado | Revisión final de Lukas. `034` aplicado en la demo con copia y ensayo (7 scripts registrados, filas de las 43 tablas existentes sin cambios). PR #65 fusionado con merge commit (`5b729b1`); gate de GitHub y local, smoke, Vercel y Render correctos; en producción la pantalla está publicada y `/api/v1/turnos` y `/api/v1/asignaciones-turno` responden 200 |
