# Especificación: modulo63-asistencia-biometrica-frontend — Pantalla de AsistenciaBiometrica

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** `/modulos/63-asistencia-biometrica` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con dos vistas, **Jornadas** y **Marcaciones** (`?vista=jornadas|marcaciones`), contra `modulo63-asistencia-biometrica-backend.md` §4 y sus mensajes literales, con el diseño común de Empleados y Turnos.

## 2. Fuera de alcance
- La API, otras pantallas, componentes compartidos y reglas compartidas de `app.css`. Librerías nuevas (el CSV se lee con `File.text()` y un analizador propio de pocas líneas).

**Decisiones (opción recomendada aplicada):**
1. **Roles:** sin `RRHH` ni `ADMIN`, los botones de escritura (registrar, importar, calcular, aprobar, rechazar, eliminar) quedan deshabilitados con «Solo RRHH o ADMIN pueden modificar la asistencia.».
2. **Rango por defecto:** la semana actual (lunes a hoy, en UTC−5), editable con «Desde» y «Hasta».
3. **Horas:** los instantes se muestran en hora de Perú (UTC−5) con formato `HH:mm`, y los minutos como «1 h 25 min».

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/asistencia-biometrica/tipos.ts`, `api.ts`, `presentacion.ts`, `csvReloj.ts`, `validacionMarcacion.ts` | crear |
| `frontend/src/modules/asistencia-biometrica/useJornadasData.ts`, `useMarcacionesData.ts` | crear |
| `frontend/src/modules/asistencia-biometrica/AsistenciaBiometricaPage.tsx` y componentes de cada vista (resumen, filtros, tabla, detalle, formulario manual, diálogo de importación, diálogo de cálculo, diálogo de rechazo) | crear |
| `frontend/src/modules/asistencia-biometrica/*.test.ts(x)` y `asistencia.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'63-asistencia-biometrica': AsistenciaBiometricaPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.asistencia-…` al final |

## 4. Diseño y lógica
- **Vista Jornadas:** resumen del rango (Asistencias, Tardanzas, Sin marcación, Faltas, Extra pendiente, Extra aprobada); filtros empleado, rango, estado y estado de horas extra; tabla Fecha, Empleado, Turno, Entrada, Salida, Trabajado, Tardanza, Extra, Estado (pastilla con texto) y Horas extra (pastilla). Detalle con los datos programados y las marcaciones del día, y botones «Aprobar horas extra» y «Rechazar» (diálogo con motivo, 21 y 22) solo si `Pendiente`. Botón «Calcular jornadas» (diálogo con rango y empleado opcional; muestra calculadas, conservadas y eliminadas; mensajes 16 a 18).
- **Vista Marcaciones:** filtros empleado, rango, tipo y origen; tabla Instante, Empleado, Tipo, Origen, Dispositivo o Observación. «Registrar marcación» (empleado, tipo, fecha y hora local convertida a UTC, observación; mensajes 1 a 5 en local; 7, 8, 9 y 12 del servidor). «Importar archivo del reloj»: elige un `.csv`, valida la cabecera (`numeroRegistro,legajo,tipo,instante`) y muestra la vista previa de las primeras 5 filas y el total antes de enviar (máximo 1000); al terminar muestra recibidos, importados, ya importados, duplicados y la lista de rechazados con su motivo. «Eliminar» solo en marcaciones `Manual` (`DialogoBaja`).
- **Accesibilidad, respuestas obsoletas y responsive:** como la spec de la pantalla del 62.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API y CSV | tests: las 9 funciones con URL, método, query y cuerpo; `csvReloj` con cabecera válida, inválida, filas vacías y más de 1000 | pasan |
| A3 | Jornadas | tests: resumen, filtros, formato de horas UTC−5, aprobar, rechazar con validación, cálculo | pasan |
| A4 | Marcaciones | tests: registro manual con validación y conversión a UTC, importación con resultado, eliminar solo manuales | pasan |
| A5 | Roles y pestañas | tests | pasan |
| A6 | Visual | capturas a 375 y 1280 px, claro y oscuro | adjuntas al PR (pendiente) |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api, CSV y tests.
- [x] Tarea 2: vista Jornadas.
- [x] Tarea 3: vista Marcaciones e importación.
- [x] Tarea 4: registro, estilos y A1 a A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Gate | Cumple: lint 0 warnings, typecheck, 86 archivos y 776 pruebas superadas, build correcto | `npm --prefix frontend run verify` |
| 2026-11-22 | A2 API y CSV | Cumple: las 9 funciones con URL, método, query y cuerpo; `csvReloj` con cabecera válida e inválida, BOM, filas vacías, 1000 y 1001 registros | `npx vitest run src/modules/asistencia-biometrica`: `api.test.ts`, `csvReloj.test.ts` |
| 2026-11-22 | A3 Jornadas | Cumple: resumen, filtros, horas en UTC−5 (con «(+1)» en el turno nocturno), detalle con marcaciones, aprobar, rechazar con mensajes 21 y 22, cálculo con mensajes 16 y 18 | `JornadasVista.test.tsx`, `presentacion.test.ts`, `validacionMarcacion.test.ts` |
| 2026-11-22 | A4 Marcaciones | Cumple: registro manual con mensajes 1 a 5 y conversión a UTC, importación con vista previa y resultado (rechazados incluidos), eliminar solo manuales | `MarcacionesVista.test.tsx` |
| 2026-11-22 | A5 Roles y pestañas | Cumple: sin `RRHH` ni `ADMIN` quedan deshabilitados los seis botones de escritura con el aviso; `?vista=`, clic y flechas | `AsistenciaBiometricaPage.test.tsx`, `JornadasVista.test.tsx`, `MarcacionesVista.test.tsx` (81 pruebas, 3 ejecuciones seguidas sin fallos) |
| 2026-11-22 | A6 Visual | pendiente: capturas | — |
