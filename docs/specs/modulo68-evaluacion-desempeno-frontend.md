# Especificación: modulo68-evaluacion-desempeno-frontend — Pantalla de EvaluacionDesempeno

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** `/modulos/68-evaluacion-desempeno` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con dos vistas, **Ciclos** y **Evaluaciones** (`?vista=`), contra `modulo68-evaluacion-desempeno-backend.md` §4 y sus mensajes literales.

## 2. Fuera de alcance
- La API, otras pantallas, componentes compartidos y reglas compartidas de `app.css`. Gráficos con librerías.

**Decisiones (opción recomendada aplicada):**
1. **Roles:** sin `RRHH` ni `ADMIN`, las escrituras quedan deshabilitadas con «Solo RRHH o ADMIN pueden modificar evaluaciones.».
2. **Calificar:** el detalle de una evaluación en `Borrador` muestra cada competencia con su peso y cinco botones de opción (1 a 5, con texto) dentro de un `fieldset`, el puntaje estimado en vivo y «Guardar» / «Enviar».
3. **Resultados:** barras horizontales hechas con CSS (ancho proporcional y el número escrito al lado, nunca solo color) para la distribución por etiqueta y el promedio por competencia.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `frontend/src/modules/evaluacion-desempeno/tipos.ts`, `api.ts`, `presentacion.ts`, `validacionEvaluacion.ts`, `useCiclosData.ts`, `useEvaluacionesData.ts` | crear |
| `frontend/src/modules/evaluacion-desempeno/EvaluacionDesempenoPage.tsx` y componentes de cada vista (filtros, tabla, detalle del ciclo con competencias y suma de pesos, formulario de ciclo, formulario de competencia, resultados, detalle de evaluación con calificación, formulario de alta) | crear |
| `frontend/src/modules/evaluacion-desempeno/*.test.ts(x)` y `evaluacion.testUtils.tsx` | crear |
| `frontend/src/modules/pantallas.ts` | modificar: `'68-evaluacion-desempeno': EvaluacionDesempenoPage,` al final |
| `frontend/src/styles/app.css` | modificar: bloque `.evaluacion-desempeno-…` al final |

## 4. Diseño y lógica
- **Ciclos:** filtros (búsqueda, estado); tabla Código, Nombre, Periodo, Fechas, Estado (pastilla), Evaluaciones. Detalle: datos, competencias (nombre, peso) con la suma visible («Suma de pesos: 85,00 de 100»), acciones por estado: editar y gestionar competencias (`Configuracion`), «Abrir ciclo» (deshabilitado con el motivo si la suma no es 100), «Cerrar ciclo» (`Abierto`, con confirmación), y la sección de resultados.
- **Evaluaciones:** filtros ciclo, estado y búsqueda; tabla Empleado, Evaluador, Ciclo, Estado, Puntaje y etiqueta. «Nueva evaluación»: ciclo (solo abiertos), empleado y evaluador (activos, distintos; 17 y 18 en local). Detalle con calificación (decisión 2), «Enviar» (28 del servidor si faltan) y «Cerrar» (`Enviada`).
- **Accesibilidad, respuestas obsoletas y responsive:** como la spec de la pantalla del 62.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API | `api.test.ts` | pasan |
| A3 | Ciclos | tests: competencias, suma de pesos, abrir bloqueado, cerrar, resultados | pasan |
| A4 | Evaluaciones | tests: alta con validación, calificación con puntaje en vivo, envío, cierre | pasan |
| A5 | Roles y pestañas | tests | pasan |
| A6 | Visual | capturas a 375 y 1280 px, claro y oscuro | en `docs/evidencias/modulo68/` |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, api y tests.
- [x] Tarea 2: vista Ciclos y resultados.
- [x] Tarea 3: vista Evaluaciones y calificación.
- [x] Tarea 4: registro, estilos y A1 a A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | cumple | `npm --prefix frontend run verify`: lint con 0 advertencias, `tsc -b`, 108 archivos y 1043 pruebas superadas, y build correcto |
| 2026-11-22 | A2 | cumple | `api.test.ts` (8 pruebas): consultas exactas, filtros vacíos omitidos, POST sin cuerpo y DELETE |
| 2026-11-22 | A3 | cumple | `CiclosVista.test.tsx` (17): competencias, suma de pesos, «Abrir ciclo» bloqueado con su motivo, apertura, cierre con confirmación y resultados; `validacionEvaluacion.test.ts` (18) cubre el puntaje en enteros (pesos no redondos, 3,55 por redondeo hacia arriba, límites de etiqueta) |
| 2026-11-22 | A4 | cumple | `EvaluacionesVista.test.tsx` (14): alta con mensajes 17 y 18, calificación con puntaje estimado en vivo, guardado, envío (con el 28 del servidor), cierre y solo lectura |
| 2026-11-22 | A5 | cumple | `EvaluacionDesempenoPage.test.tsx` (5): pestañas por `?vista=`, flechas y `tabIndex`, y roles RRHH/ADMIN frente a otros; `npx vitest run src/modules/evaluacion-desempeno` ×3: 5 archivos y 62 pruebas superadas cada vez |
| 2026-11-22 | A6 | pendiente: capturas | capturas a 375 y 1280 px, claro y oscuro, aún sin tomar |
| 2026-11-24 | A6 | OK | 12 capturas (3 escenarios a 375 y 1280 px, claro y oscuro) en `docs/evidencias/modulo68/`, contra API y PostgreSQL locales con el seed: sin desplazamiento horizontal de página, sin errores de consola y Escape cierra los diálogos; detalle en su `README.md` y `resultado-visual.json` |

### Opciones tomadas donde la spec no decía nada
- **Columna «Evaluaciones» de la tabla de ciclos:** el listado del backend no trae el conteo (solo el detalle), así que la tabla muestra Código, Nombre, Periodo, Fechas y Estado, y los conteos por estado van en el detalle.
- **Enviar:** guarda primero las calificaciones y el comentario de la pantalla y luego llama a `envio`; si faltan, se muestra el mensaje 28 del servidor.
- **Puntaje estimado:** solo se calcula con todas las competencias calificadas (como el servidor al enviar); antes se indica cuántas faltan.
- **Confirmaciones:** además de cerrar el ciclo, se confirman cerrar una evaluación y quitar una competencia.
- **Resultados:** se piden solo para ciclos `Abierto` o `Cerrado`.
