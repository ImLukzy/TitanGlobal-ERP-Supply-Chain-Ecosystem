# Especificación: modulo70-seguridad-salud-ocupacional-frontend — Pantalla de SeguridadSaludOcupacional

> Estado: **aprobada** por Lukas el 2026-11-24 en la revisión final, con las opciones recomendadas de §2; integrada en `main` por el PR #72 y desplegada en la demo.

## 1. Objetivo
**Problema:** `/modulos/70-seguridad-salud-ocupacional` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con tres vistas, **Incidentes**, **EPP** y **Exámenes médicos** (`?vista=`), contra `modulo70-seguridad-salud-ocupacional-backend.md` §4 y sus mensajes literales.

## 2. Fuera de alcance
- La API, otras pantallas, componentes compartidos y reglas compartidas de `app.css`. Librerías nuevas.

**Decisiones (opción recomendada aplicada):**
1. **Roles:** sin `RRHH` ni `ADMIN`, las escrituras quedan deshabilitadas con «Solo RRHH o ADMIN pueden modificar seguridad y salud.».
2. **Avisos:** el resumen destaca con texto (no solo color) los EPP vencidos, los exámenes vencidos y los empleados no aptos, y cada número lleva a su vista con el filtro aplicado.
3. **Severidad:** pastilla con texto; `Grave` y `Mortal` con el estilo de alerta del sistema de diseño.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/seguridad-salud-ocupacional/tipos.ts`, `api.ts`, `presentacion.ts`, `validacionSst.ts`, `useIncidentesData.ts`, `useEppData.ts`, `useExamenesData.ts` | crear |
| `frontend/src/modules/seguridad-salud-ocupacional/SeguridadSaludOcupacionalPage.tsx` y componentes de cada vista (resumen, filtros, tablas, detalle del incidente con acciones, formularios de incidente, acción, equipo, entrega y examen, diálogo de completar acción) | crear |
| `frontend/src/modules/seguridad-salud-ocupacional/*.test.ts(x)` y `sst.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'70-seguridad-salud-ocupacional': SeguridadSaludOcupacionalPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.sst-…` al final |

## 4. Diseño y lógica
- **Incidentes:** filtros (búsqueda, tipo, severidad, estado, rango); tabla Número, Fecha, Empleado, Tipo, Severidad, Días de baja, Estado. Detalle con descripción y acciones correctivas (descripción, responsable, compromiso, completada); «Agregar acción», «Completar» y «Cerrar incidente» (deshabilitado con el motivo visible si no hay acciones o hay pendientes). Formulario: empleado, tipo, severidad, fecha (no futura), días de baja (solo con `AccidenteConBaja`), lugar y descripción; mensajes 20 a 25 en local.
- **EPP:** catálogo de equipos (alta, edición, activar y desactivar) y entregas (filtros empleado, equipo y «Solo vencidas»; tabla con vencimiento y «Vencida» con texto; alta de entrega con el vencimiento calculado a la vista).
- **Exámenes médicos:** filtros empleado, tipo, resultado y «Solo vencidos»; tabla; alta con restricciones solo en `AptoConRestricciones` y vencimiento oculto en `Retiro`.
- **Accesibilidad, respuestas obsoletas y responsive:** como la spec de la pantalla del 62.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API | `api.test.ts` | pasan |
| A3 | Incidentes | tests: alta con validación, acciones, cierre bloqueado y permitido | pasan |
| A4 | EPP y exámenes | tests: entregas con vencimiento, campos condicionales de exámenes | pasan |
| A5 | Roles, resumen y pestañas | tests | pasan |
| A6 | Visual | capturas a 375 y 1280 px, claro y oscuro | en `docs/evidencias/modulo70/` |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api y tests.
- [x] Tarea 2: vista Incidentes.
- [x] Tarea 3: vistas EPP y Exámenes.
- [x] Tarea 4: registro, estilos y A1 a A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Gate | Verde, 0 warnings | `npm --prefix frontend run verify`: lint y typecheck sin avisos, 120 archivos y 1162 pruebas superadas, build correcto |
| 2026-11-22 | A2 API | Pasan | `api.test.ts` (8 pruebas) |
| 2026-11-22 | A3 Incidentes | Pasan | `IncidentesVista.test.tsx` (alta con validación, acciones, cierre bloqueado y permitido) y `validacionSst.test.ts` |
| 2026-11-22 | A4 EPP y exámenes | Pasan | `EppVista.test.tsx` (vencimiento calculado, catálogo) y `ExamenesVista.test.tsx` (restricciones y vencimiento condicionales) |
| 2026-11-22 | A5 Roles, resumen y pestañas | Pasan | `SeguridadSaludOcupacionalPage.test.tsx`; el módulo, 3 ejecuciones seguidas con 60 pruebas superadas cada una |
| 2026-11-22 | A6 Visual | pendiente: capturas | capturas a 375 y 1280 px, claro y oscuro |
| 2026-11-24 | A6 | OK | 12 capturas (3 escenarios a 375 y 1280 px, claro y oscuro) en `docs/evidencias/modulo70/`, contra API y PostgreSQL locales con el seed: sin desplazamiento horizontal de página, sin errores de consola y Escape cierra los diálogos; detalle en su `README.md` y `resultado-visual.json` |
| 2026-11-24 | Integración y producción | Aprobado | Revisión final de Lukas. `041` aplicado en la demo con copia y ensayo (14 scripts registrados, filas de las 66 tablas existentes sin cambios). PR #72 fusionado con merge commit (`7f2b898`); gate de GitHub y local, smoke, Vercel y Render correctos; en producción la pantalla está publicada y `/api/v1/equipos-proteccion`, `/api/v1/entregas-epp`, `/api/v1/examenes-medicos` y `/api/v1/incidentes-sst` responden 200 |
