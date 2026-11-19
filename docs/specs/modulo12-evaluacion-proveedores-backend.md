# Especificación: modulo12-evaluacion-proveedores-backend — Evaluación de proveedores

## 1. Objetivo
**Problema:** el dominio de Compras tiene el padrón de proveedores (módulo 11) pero no hay forma de medir qué tan bien cumple cada uno. Sin esa medida no se pueden comparar proveedores ni apoyar las licitaciones (módulo 14) y los acuerdos de abastecimiento (módulo 20).
**Resultado esperado:** una entidad nueva `EvaluacionProveedor` con su tabla y `Modulo12_EvaluacionProveedores` en `api/v1/evaluaciones-proveedores`:
- registrar una evaluación mensual por proveedor, con puntajes de cumplimiento, calidad y plazos;
- calcular y guardar el puntaje final y la calificación;
- listado con filtros y paginación, detalle, modificación de puntajes y baja;
- resumen con el ranking de proveedores.

Sigue el patrón de `Modulo11_Proveedores` (spec 12) y, para la referencia a otro módulo, de `Modulo13_SolicitudesCompra`.

## 2. Fuera de alcance
- Las 4 entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`), los middlewares y el pipeline.
- Cambios en `Modulo11_Proveedores`. En particular, la baja de un proveedor que ya tiene evaluaciones queda bloqueada por la clave foránea `RESTRICT`; el mensaje amable de esa baja se pide a Lukas aparte, porque el módulo 11 no es de este autor.
- Pesos o umbrales configurables por el usuario (son constantes en esta spec).
- Relación con licitaciones o acuerdos (llegará con sus specs).
- El frontend (spec `modulo12-evaluacion-proveedores-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Fórmula del puntaje final:** `cumplimiento × 0,40 + calidad × 0,30 + plazos × 0,30`, redondeado a 2 decimales.
2. **Calificación:** Excelente (≥ 90), Bueno (≥ 75), Regular (≥ 60), Deficiente (< 60).
3. **Una evaluación por proveedor y mes** (periodo `AAAA-MM`).
4. **Un proveedor inactivo sí puede evaluarse** (a diferencia de las solicitudes de compra): el historial no se pierde al retirarlo.
5. **El PUT solo cambia puntajes y observaciones.** Proveedor y periodo no se editan; si se registraron mal, se borra la evaluación y se crea otra.
6. **La baja de una evaluación es física.**

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/005-evaluaciones-proveedor.sql` | crear | `CREATE TABLE "EvaluacionesProveedor"`; número `005` reservado para este módulo en `docs/equipo/relevo.md` §8 |
| `src/TitanGlobal.Domain/Models/EvaluacionProveedor.cs` | regenerar | entidad `partial` generada por `./scripts/scaffold.sh` (no se edita a mano) |
| `src/TitanGlobal.Domain/Models/Proveedor.cs` | regenerar | solo gana la navegación `EvaluacionesProveedor`, generada |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet<EvaluacionProveedor> EvaluacionesProveedor`, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal la entidad (debe quedar `EvaluacionProveedor` / `EvaluacionesProveedor`) |
| `src/TitanGlobal.Application/DTOs/Modulo12_EvaluacionProveedores/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo12_EvaluacionProveedores/*.cs` | crear | un caso de uso por operación y `ValidacionEvaluacionProveedor` |
| `src/TitanGlobal.Api/Controllers/Modulo12_EvaluacionProveedores/EvaluacionesProveedorController.cs` | crear | `[Route("api/v1/evaluaciones-proveedores")]`, controlador delgado |
| `tests/TitanGlobal.Api.Tests/Modulo12_EvaluacionProveedores/EvaluacionesProveedorApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 6 evaluaciones idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A6) |

## 4. Diseño y lógica
**Tabla `EvaluacionesProveedor`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `ProveedorId` | `integer NOT NULL` | clave foránea a `Proveedores("Id")` `ON DELETE RESTRICT` |
| `Periodo` | `character varying(7) NOT NULL` | formato `AAAA-MM` |
| `PuntajeCumplimiento`, `PuntajeCalidad`, `PuntajePlazos` | `integer NOT NULL` | `CHECK` entre 0 y 100 |
| `PuntajeFinal` | `numeric(5,2) NOT NULL` | calculado y guardado |
| `Calificacion` | `character varying(20) NOT NULL` | calculada y guardada |
| `Observaciones` | `character varying(500)` | opcional |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

Índice único `IX_EvaluacionesProveedor_ProveedorId_Periodo` sobre (`ProveedorId`, `Periodo`). Se guardan el puntaje final y la calificación para que el historial no cambie si algún día cambian los pesos.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/evaluaciones-proveedores?proveedorId=&periodo=&calificacion=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaEvaluacionesDto { items: EvaluacionListadoDto[], total, pagina, tamanoPagina }` | 400 |
| GET | `api/v1/evaluaciones-proveedores/{id:int}` | — | 200 `EvaluacionDetalleDto` | 404 |
| POST | `api/v1/evaluaciones-proveedores` | `CrearEvaluacionRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/evaluaciones-proveedores/{id:int}` | `ActualizarEvaluacionRequestDto` | 200 `{ id }` | 400, 404 |
| DELETE | `api/v1/evaluaciones-proveedores/{id:int}` | — | 204 | 404 |
| GET | `api/v1/evaluaciones-proveedores/resumen` | — | 200 `ResumenEvaluacionesDto` | — |

**DTOs.**
- `CrearEvaluacionRequestDto { proveedorId, periodo, puntajeCumplimiento, puntajeCalidad, puntajePlazos, observaciones? }`
- `ActualizarEvaluacionRequestDto { puntajeCumplimiento, puntajeCalidad, puntajePlazos, observaciones? }`
- `EvaluacionListadoDto { id, proveedorId, proveedorNombre, periodo, puntajeFinal, calificacion }`
- `EvaluacionDetalleDto { id, proveedorId, proveedorNombre, periodo, puntajeCumplimiento, puntajeCalidad, puntajePlazos, puntajeFinal, calificacion, observaciones, fechaRegistro }`
- `ResumenEvaluacionesDto { totalEvaluaciones, proveedoresEvaluados, promedioGeneral, porCalificacion: CalificacionConteoDto[], ranking: RankingProveedorDto[] }`
- `CalificacionConteoDto { calificacion, total }`, siempre las 4 calificaciones en el orden Excelente, Bueno, Regular, Deficiente (con 0 si no hay).
- `RankingProveedorDto { proveedorId, proveedorNombre, evaluaciones, promedio, ultimaCalificacion }`, ordenado por `promedio` descendente y después `proveedorNombre`; `ultimaCalificacion` es la del periodo más reciente.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**

*POST*
1. `proveedorId` ≤ 0 → 400 «El proveedor es obligatorio.»
2. `periodo` tras `Trim()` con formato `AAAA-MM` (año 2000–2100, mes 01–12) → si no, 400 «El periodo debe tener el formato AAAA-MM.»; posterior al mes actual (UTC) → 400 «El periodo no puede ser futuro.»
3. Cada puntaje entero entre 0 y 100, en este orden → 400 «El puntaje de cumplimiento debe estar entre 0 y 100.» / «El puntaje de calidad debe estar entre 0 y 100.» / «El puntaje de plazos debe estar entre 0 y 100.»
4. `observaciones` tras `Trim()`, en blanco → `null`; más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»
5. Proveedor inexistente → 400 «El proveedor {id} no existe.» (mismo criterio que el módulo 13; el proveedor inactivo se acepta).
6. Ya existe una evaluación del proveedor en ese periodo → 409 «El proveedor {proveedorId} ya tiene una evaluación del periodo {periodo}.» También en la carrera (`23505`).

*PUT*: reglas 3 y 4 sobre el cuerpo; el recálculo de `PuntajeFinal` y `Calificacion` es obligatorio.

*GET por id, PUT y DELETE*: id inexistente → 404 «La evaluación {id} no existe.»

*GET listado*: `pagina < 1` → 400 «La página debe ser mayor o igual a 1.»; `tamanoPagina` fuera de 1–100 → 400 «El tamaño de página debe estar entre 1 y 100.»; `periodo` con formato inválido → 400 «El periodo debe tener el formato AAAA-MM.»; `calificacion` que no sea una de las 4, sin distinguir mayúsculas → 400 «La calificación debe ser Excelente, Bueno, Regular o Deficiente.»

**Cálculo (`ValidacionEvaluacionProveedor`, función pura y estática):**
- `PuntajeFinal = Math.Round(cumplimiento * 0.40m + calidad * 0.30m + plazos * 0.30m, 2, MidpointRounding.AwayFromZero)`.
- `Calificacion`: ≥ 90 Excelente; ≥ 75 Bueno; ≥ 60 Regular; en otro caso Deficiente.

**Consultas.** Desde `Repository<EvaluacionProveedor>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1). Listado ordenado por `Periodo` descendente, después `ProveedorId` y `Id`. El `promedioGeneral` y los `promedio` del ranking se redondean a 2 decimales. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (`GroupBy`, `Average`, `Join` con `Proveedor`), sin forzarlos.

**Seed de desarrollo** (`seed-dev.sql`, idempotente con `ON CONFLICT ("ProveedorId", "Periodo") DO UPDATE`), con `PuntajeFinal` y `Calificacion` coherentes con la fórmula:

| Proveedor | Periodo | Cumpl. | Calidad | Plazos | Final | Calificación |
|---|---|---|---|---|---|---|
| 1 | 2026-09 | 90 | 85 | 80 | 85.50 | Bueno |
| 1 | 2026-10 | 95 | 92 | 90 | 92.60 | Excelente |
| 2 | 2026-10 | 70 | 65 | 60 | 65.50 | Regular |
| 4 | 2026-09 | 55 | 60 | 50 | 55.00 | Deficiente |
| 4 | 2026-10 | 80 | 78 | 76 | 78.20 | Bueno |
| 6 | 2026-10 | 100 | 90 | 95 | 95.50 | Excelente |

Resumen esperado del seed: 6 evaluaciones, 4 proveedores evaluados, promedio general 78.72, conteo por calificación Excelente 2 / Bueno 2 / Regular 1 / Deficiente 1, y ranking: proveedor 6 (95.50), proveedor 1 (89.05), proveedor 4 (66.60), proveedor 2 (65.50).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; periodo con espacios aceptado y guardado sin ellos; observaciones en blanco → `null`; proveedor inactivo aceptado; PUT sin cambiar proveedor ni periodo | pasan |
| A4 | Cálculo | tests de `ValidacionEvaluacionProveedor`: 90/85/80 → 85.50 Bueno; 100/100/100 → 100.00 Excelente; 0/0/0 → 0.00 Deficiente; límites 90, 75 y 60 → cada calificación; un PUT recalcula puntaje y calificación | pasan |
| A5 | Consultas | tests con datos conocidos: filtros por proveedor, periodo y calificación (sin distinguir mayúsculas), paginación con `total`, orden por periodo descendente, resumen exacto (conteos y ranking del seed) | pasan |
| A6 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias, y `./scripts/smoke-postgres.sh` prueba listado con `proveedorId=1`, filtro `calificacion=excelente`, detalle, POST válido, POST repetido → 409 literal, DELETE del creado → 204 y resumen del seed | `SMOKE OK` |
| A7 | OpenAPI | `/openapi/v1.json` incluye las 6 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [x] Tarea 1: script SQL, aplicarlo y generar entidades con scaffold.
- [x] Tarea 2: DTOs, `ValidacionEvaluacionProveedor`, casos de uso y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: verificar A1–A7 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-15 | A1 Compilación | OK | `./scripts/verify.sh` (macOS): compilación de las 5 capas sin errores |
| 2026-11-15 | A2 Gate | OK | `./scripts/verify.sh` completo en verde: reglas de código y capas (`check-rules.sh`), backend 981 tests, frontend eslint/tsc/497 tests/build. El primer intento falló en las reglas `a` (var) y `b` (cuerpo de expresión) y se corrigió en `c0f21bc` |
| 2026-11-15 | A3 Reglas | OK | `EvaluacionesProveedorApiTests`: mensajes literales 400/404/409 sin `Detail`, observaciones en blanco → `null`, proveedor inactivo aceptado, PUT recalcula sin cambiar proveedor ni periodo |
| 2026-11-15 | A4 Cálculo | OK | `CreateCalculatesFinalScoreAndRating` (10 casos: límites 90, 75 y 60, 100/100/100, 0/0/0) y `UpdateRecalculatesScoreAndKeepsProviderAndPeriod` |
| 2026-11-15 | A5 Consultas | OK | filtros por proveedor, periodo y calificación sin distinguir mayúsculas, paginación con `total`, orden por periodo descendente y resumen exacto del seed (78.72; ranking 6, 1, 4, 2) |
| 2026-11-15 | A6 Datos y PostgreSQL | OK | `./scripts/aplicar-bd.sh` aplica `005`; `./scripts/scaffold.sh --comprobar` → «SCAFFOLD AL DÍA» y `git status` limpio; `./scripts/smoke-postgres.sh` → `SMOKE OK` |
| 2026-11-15 | A7 OpenAPI | OK | `OpenApiContainsSixEvaluationOperations` |

`dotnet test` completo: 981 correctas, 0 con error (942 previas + 39 del módulo 12).

**Notas de entorno (macOS):** `scaffold.sh` y `smoke-postgres.sh` necesitaron rodeos locales en Mac (GNU sed para `\s`, un `ss` de reemplazo y bash 5 por el arreglo vacío con `set -u`). No se modificaron los scripts.
