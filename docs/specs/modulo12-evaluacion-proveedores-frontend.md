# Especificación: modulo12-evaluacion-proveedores-frontend — Pantalla de evaluación de proveedores

## 1. Objetivo
**Problema:** `/modulos/12-evaluacion-proveedores` muestra «Pantalla pendiente»; la API de evaluaciones (spec `modulo12-evaluacion-proveedores-backend`) no tiene interfaz.
**Resultado esperado:** una pantalla de mantenimiento de evaluaciones con el mismo patrón y los mismos componentes compartidos que Proveedores (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`), contra el contrato de la spec de backend §4: ver el resumen y el ranking, filtrar y consultar evaluaciones, registrar una evaluación mensual, corregir sus puntajes y darla de baja.

## 2. Fuera de alcance
- La API: se trabaja con `fetch` simulado y el contrato de la spec de backend §4.
- Cambiar Proveedores, Clientes u otras pantallas, salvo mejoras de los componentes compartidos que no cambien lo que comprueban sus tests.
- Librerías nuevas.
- Gráficos: el resumen usa cifras y tablas, no una librería de gráficos.
- Editar proveedor o periodo de una evaluación (la API no lo permite).

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Vista previa del cálculo en el formulario:** mientras se escriben los tres puntajes se muestra «Vista previa: 85,50 · Bueno», calculada con la misma fórmula de la API (40 % / 30 % / 30 %, umbrales 90, 75 y 60). Es solo informativa; el valor guardado es el que devuelve el servidor.
2. **El selector de proveedor del filtro y del formulario** lista a todos los proveedores, activos e inactivos (la API acepta ambos), tomados de `GET proveedores` con `tamanoPagina=100`.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/evaluacion-proveedores/{tipos,api}.ts` | crear | 6 funciones: `listarEvaluaciones(filtros)`, `obtenerEvaluacion`, `crearEvaluacion`, `actualizarEvaluacion`, `eliminarEvaluacion`, `obtenerResumenEvaluaciones`; los filtros vacíos o `undefined` no se envían; reutiliza `listarProveedores` de `modules/proveedores/api` para el selector |
| `frontend/src/modules/evaluacion-proveedores/EvaluacionProveedoresPage.tsx` + componentes | crear | §4; usa `PaginaModulo` y los componentes compartidos (resumen, filtros, tabla, detalle, formulario y validación en archivos separados, para no repetir el archivo de 600 líneas que la auditoría marcó en Proveedores) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `12-evaluacion-proveedores` |
| `frontend/src/styles/app.css` | modificar | bloque nuevo al final con prefijo `.evaluaciones-` |
| `frontend/src/modules/evaluacion-proveedores/*.test.ts(x)` | crear | `fetch` simulado |

## 4. Diseño y lógica
- **Resumen:** total de evaluaciones, proveedores evaluados y promedio general; una fila con los 4 conteos por calificación (Excelente, Bueno, Regular, Deficiente, siempre en ese orden); y una tabla de ranking (posición, proveedor, evaluaciones, promedio y última calificación).
- **Filtros:** selector de proveedor («Todos»), periodo (`<input type="month">`, valor `AAAA-MM`, con botón «Quitar» para vaciarlo) y selector de calificación («Todas»). Cualquier cambio vuelve a la página 1. No hay búsqueda de texto, así que no hay espera de 300 ms.
- **Tabla:** periodo, proveedor, puntaje final y calificación como etiqueta con texto (no solo color). Orden y paginación de 10 en 10 tal como los entrega la API. Estados de carga, vacío y error.
- **Detalle (lateral en escritorio, debajo en móvil):** proveedor, periodo, los tres puntajes, puntaje final, calificación, fecha de registro; observaciones ausentes → «Sin observaciones».
- **Formulario:**
  - Alta: proveedor (obligatorio), periodo (`<input type="month">` con máximo el mes actual), tres puntajes enteros 0–100 y observaciones (máximo 500, con contador).
  - Edición: proveedor y periodo se muestran de solo lectura; solo se editan puntajes y observaciones.
  - Validación local en el mismo orden y con los mismos literales de la spec de backend §4: «El proveedor es obligatorio.», «El periodo debe tener el formato AAAA-MM.», «El periodo no puede ser futuro.», «El puntaje de cumplimiento debe estar entre 0 y 100.» (y los de calidad y plazos), «Las observaciones no pueden superar 500 caracteres.». Por campo, con `aria-describedby`.
  - Los errores 400/404/409 de la API (por ejemplo «El proveedor 1 ya tiene una evaluación del periodo 2026-10.») se muestran con su `Message` sin perder lo escrito.
- **Baja:** diálogo accesible con el proveedor y el periodo; Escape cancela y devuelve el foco. Tras una baja o un alta se refrescan lista, resumen y detalle.
- **Accesibilidad y responsive:** como en las specs de Clientes y Proveedores (labels, `aria-live`, `<caption>`, ≥ 16 px de margen, sin scroll horizontal de página ni celdas recortadas a 1280 px; a 375 px el ranking y la tabla se apilan o se desplazan dentro de su propio contenedor).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 6 funciones con URL, método, query y cuerpo exactos; filtros vacíos y `undefined` fuera de la query | pasan |
| A3 | Resumen, filtros y tabla | tests: resumen con los 4 conteos en orden fijo y el ranking; proveedor, periodo y calificación en la query; vuelta a la página 1; estados vacío y error | pasan |
| A4 | Detalle, formulario y baja | tests: «Sin observaciones»; cada validación local con su literal y sin petición; vista previa correcta (90/85/80 → 85,50 · Bueno; límites 90, 75 y 60); 409 de periodo repetido visible conservando lo escrito; edición con proveedor y periodo de solo lectura y PUT con su id; baja OK con refresco; Escape con foco devuelto | pasan |
| A5 | Sin regresiones | tests de Proveedores, Clientes y demás pantallas en verde sin cambiar lo que comprueban | pasan |
| A6 | Visual y E2E | capturas (`page.route`) a 375 y 1280 px, claro y oscuro, con detalle abierto y con el diálogo de baja; márgenes ≥ 16 px medidos; sin scroll horizontal ni celdas recortadas; y una pasada contra la API y PostgreSQL reales con el seed (resumen 6 / 4 / 78,72 y filtro `calificacion=excelente` → 2 filas) | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla.
- [x] Tarea 3: detalle, formulario (con vista previa) y baja.
- [ ] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-25 | A1 | ⚠️ parcial | frontend: `eslint . --max-warnings 0` sin avisos, `tsc -b` limpio, `vite build` correcto y `scripts/check-rules.sh` en 0; `./scripts/verify.sh` completo pendiente de correrlo en macOS antes del PR |
| 2026-11-25 | A2 | ✅ | `api.test.ts`: 6 tests (URL, método, query y cuerpo exactos; filtros vacíos y `undefined` fuera; selector con `proveedores?tamanoPagina=100`) |
| 2026-11-25 | A3 | ✅ | `EvaluacionProveedoresPage.test.tsx`: resumen con los 4 conteos en orden fijo y ranking, tabla, filtros en la query, «Quitar», vuelta a página 1, vacío y error |
| 2026-11-25 | A4 | ✅ | `EvaluacionAcciones.test.tsx` y `puntaje.test.ts`: «Sin observaciones», cada literal de validación sin petición, vista previa 85,50 · Bueno y límites 90/75/60, 409 sin perder lo escrito, edición con PUT y campos de solo lectura, Escape con foco devuelto, baja con refresco |
| 2026-11-25 | A5 | ✅ | suite completa `vitest run`: 1339 tests pasan (los 25 nuevos más los existentes) sin cambiar lo que comprueban |
| — | A6 | pendiente | capturas a 375 y 1280 px (claro/oscuro) y pasada contra la API y PostgreSQL reales con el seed |
