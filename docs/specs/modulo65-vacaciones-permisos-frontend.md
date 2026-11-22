# Especificación: modulo65-vacaciones-permisos-frontend — Pantalla de VacacionesPermisos

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** `/modulos/65-vacaciones-permisos` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de ausencias contra `modulo65-vacaciones-permisos-backend.md` §4 y sus mensajes literales: resumen, listado con filtros, detalle, registro con saldo vacacional a la vista, aprobación, rechazo y cancelación. Diseño común de Empleados y Contratos laborales.

## 2. Fuera de alcance
- La API, otras pantallas, componentes compartidos y reglas compartidas de `app.css`. Calendarios gráficos y librerías nuevas.

**Decisiones (opción recomendada aplicada):**
1. **Roles:** «Aprobar» y «Rechazar» deshabilitados sin `RRHH` ni `ADMIN`, con «Solo RRHH o ADMIN pueden aprobar o rechazar ausencias.». Registrar y cancelar, habilitados para cualquier sesión.
2. **Saldo en el formulario:** al elegir empleado y tipo `Vacaciones`, el formulario muestra el saldo a la fecha de inicio (`GET .../saldo-vacacional/{id}?fecha=`) con devengado, gozado, programado y saldo, y los días que pide la solicitud.
3. **Selector de empleado:** el de la spec de la pantalla del 62 (activos, hasta 100).

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/vacaciones-permisos/tipos.ts`, `api.ts`, `presentacion.ts`, `validacionAusencia.ts`, `useAusenciasData.ts` | crear |
| `frontend/src/modules/vacaciones-permisos/VacacionesPermisosPage.tsx`, `AusenciasResumen.tsx`, `AusenciasFiltros.tsx`, `AusenciasTabla.tsx`, `AusenciaDetallePanel.tsx`, `AusenciaFormulario.tsx`, `SaldoVacacional.tsx`, `DialogoRechazo.tsx` | crear |
| `frontend/src/modules/vacaciones-permisos/*.test.ts(x)` y `ausencias.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'65-vacaciones-permisos': VacacionesPermisosPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.vacaciones-permisos-…` al final |

## 4. Diseño y lógica
- **api.ts:** una función por ruta (8), filtros vacíos omitidos; aprobación y cancelación con `apiPostSinCuerpo`.
- **Resumen:** Pendientes, Ausentes hoy, De vacaciones hoy, Descanso médico hoy, Aprobadas este mes.
- **Filtros:** búsqueda (300 ms), tipo, estado, rango de fechas. **Tabla:** Número, Empleado, Tipo (texto legible: «Permiso con goce»…), Desde, Hasta, Días, Estado (pastilla con texto).
- **Detalle:** todos los campos; si es `Vacaciones`, el saldo del empleado; botones «Aprobar», «Rechazar» (diálogo con motivo, 17 y 18) solo en `Pendiente`, y «Cancelar solicitud» solo si no empezó y no está rechazada ni cancelada (con `DialogoBaja` adaptado por texto, sin borrar).
- **Formulario:** empleado, tipo, desde, hasta, motivo (obligatorio según tipo, contador «n/300»), días calculados en vivo; validación local 1 a 9; 409 del servidor (11, 13, 14) junto al formulario conservando lo escrito.
- **Accesibilidad, fechas, respuestas obsoletas y responsive:** como la spec de la pantalla del 62.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API | `api.test.ts`: las 8 funciones | pasan |
| A3 | Listado, resumen y detalle | tests | pasan |
| A4 | Formulario y saldo | tests: validación local sin petición, días en vivo, saldo al elegir vacaciones, 409 conservando lo escrito | pasan |
| A5 | Flujo y roles | tests: aprobar, rechazar, cancelar según estado y fecha; botones según rol | pasan |
| A6 | Visual | capturas a 375 y 1280 px, claro y oscuro | adjuntas al PR (pendiente) |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api y tests.
- [x] Tarea 2: resumen, filtros, tabla y detalle.
- [x] Tarea 3: formulario, saldo y diálogos.
- [x] Tarea 4: registro, estilos y A1 a A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | `verify` en verde: lint 0 warnings, typecheck, 91 archivos y 846 pruebas, build | `npm --prefix frontend run verify` |
| 2026-11-22 | A2 | `api.test.ts`: las 8 funciones (filtros vacíos omitidos, aprobación y cancelación sin cuerpo) | `npx vitest run src/modules/vacaciones-permisos` |
| 2026-11-22 | A3 | Listado, filtros con temporizadores falsos, resumen de 5 tarjetas y detalle | `VacacionesPermisosPage.test.tsx` |
| 2026-11-22 | A4 | Formulario: validación local sin petición, días en vivo, saldo al elegir vacaciones, 409 conservando lo escrito | `AusenciaFormulario.test.tsx`, `validacionAusencia.test.ts`, `presentacion.test.ts` |
| 2026-11-22 | A5 | Aprobar, rechazar y cancelar según estado, fecha y rol | `VacacionesPermisosPage.test.tsx` (70 pruebas del módulo, 3 ejecuciones seguidas en verde) |
| 2026-11-22 | A6 | pendiente: capturas a 375 y 1280 px, claro y oscuro | — |
