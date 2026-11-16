# Especificación: modulo14-licitaciones-backend — Licitaciones

## 1. Objetivo
**Problema:** el dominio de Compras tiene proveedores (módulo 11), su evaluación (módulo 12) y las órdenes de abastecimiento (módulo 15), pero no hay forma de convocar a varios proveedores, comparar sus ofertas y elegir con criterio a quién comprar.
**Resultado esperado:** dos entidades nuevas, `Licitacion` y `OfertaLicitacion`, con sus tablas y `Modulo14_Licitaciones` en `api/v1/licitaciones`:
- crear y editar licitaciones de dos tipos: concurso de precios y subasta inversa;
- registrar las ofertas de los proveedores y, en la subasta inversa, permitir que mejoren su oferta;
- cerrar la licitación, adjudicarla a una oferta o cancelarla;
- listado con filtros y paginación, detalle con las ofertas comparadas, y resumen.

Sigue el patrón de `Modulo13_SolicitudesCompra` (cabecera con detalle y cambios de estado) y de `Modulo11_Proveedores`.

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en `Modulo11_Proveedores` o `Modulo12_EvaluacionProveedores`; la baja de un proveedor con licitaciones u ofertas queda bloqueada por la clave foránea `RESTRICT` (se avisa a Lukas aparte, igual que con las evaluaciones).
- Generar una orden de abastecimiento a partir de la adjudicación (módulo 15; llegará con su propia spec).
- Productos o líneas dentro de la licitación: se licita un objeto descrito en texto y un monto total.
- Cierre automático por fecha: el estado solo cambia por acciones explícitas.
- El frontend (spec `modulo14-licitaciones-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Tipos:** `ConcursoPrecios` (cada proveedor presenta una oferta y no puede cambiarla) y `SubastaInversa` (cada proveedor puede bajar su oferta mientras la licitación esté abierta).
2. **Estados:** `Abierta` → `Cerrada` → `Adjudicada`; `Abierta` o `Cerrada` → `Cancelada`. `Adjudicada` y `Cancelada` son finales.
3. **El número lo escribe el usuario** (por ejemplo `LIC-2026-001`), único, en mayúsculas.
4. **Una oferta por proveedor y licitación;** el monto no puede superar el presupuesto referencial.
5. **Solo proveedores activos** pueden ofertar (mismo criterio que las solicitudes de compra).
6. **Al adjudicar se puede elegir cualquier oferta,** no solo la más baja (el plazo de entrega o la evaluación del proveedor también pesan). La adjudicación guarda proveedor y monto.
7. **No hay cierre automático:** pasada la fecha de cierre ya no se aceptan ofertas, pero la licitación sigue `Abierta` hasta que alguien la cierre.
8. **Solo se elimina una licitación sin ofertas;** si tiene ofertas, se cancela.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-licitaciones.sql` | crear | `CREATE TABLE "Licitaciones"` y `"OfertasLicitacion"`; usar `005` (el `004` es del módulo 12) y que Lukas renumere si hay choque |
| `src/TitanGlobal.Domain/Models/Licitacion.cs`, `OfertaLicitacion.cs`, `Proveedor.cs` | regenerar | entidades `partial` generadas por `./scripts/scaffold.sh` (no se editan a mano) |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet<Licitacion>` y `DbSet<OfertaLicitacion>`, generados |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal las entidades (deben quedar `Licitacion` / `Licitaciones` y `OfertaLicitacion` / `OfertasLicitacion`) |
| `src/TitanGlobal.Application/DTOs/Modulo14_Licitaciones/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo14_Licitaciones/*.cs` | crear | un caso de uso por operación y `ValidacionLicitacion` |
| `src/TitanGlobal.Api/Controllers/Modulo14_Licitaciones/LicitacionesController.cs` | crear | `[Route("api/v1/licitaciones")]`, controlador delgado; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` (spec 18) |
| `tests/TitanGlobal.Api.Tests/Modulo14_Licitaciones/LicitacionesApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 5 licitaciones y 7 ofertas idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A6) |

## 4. Diseño y lógica
**Tabla `Licitaciones`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Numero` | `character varying(20) NOT NULL` | único (`IX_Licitaciones_Numero`) |
| `Titulo` | `character varying(150) NOT NULL` | |
| `Descripcion` | `character varying(500)` | opcional |
| `Tipo` | `character varying(20) NOT NULL` | `CHECK` en (`ConcursoPrecios`, `SubastaInversa`) |
| `Estado` | `character varying(20) NOT NULL` | `CHECK` en (`Abierta`, `Cerrada`, `Adjudicada`, `Cancelada`) |
| `PresupuestoReferencial` | `numeric(18,2) NOT NULL` | `CHECK` > 0 |
| `FechaCierre` | `timestamp with time zone NOT NULL` | UTC |
| `ProveedorAdjudicadoId` | `integer` | clave foránea a `Proveedores("Id")` `ON DELETE RESTRICT`; solo con estado `Adjudicada` |
| `MontoAdjudicado` | `numeric(18,2)` | solo con estado `Adjudicada` |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

**Tabla `OfertasLicitacion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `LicitacionId` | `integer NOT NULL` | clave foránea a `Licitaciones("Id")` `ON DELETE RESTRICT` |
| `ProveedorId` | `integer NOT NULL` | clave foránea a `Proveedores("Id")` `ON DELETE RESTRICT` |
| `Monto` | `numeric(18,2) NOT NULL` | `CHECK` > 0 |
| `PlazoEntregaDias` | `integer NOT NULL` | `CHECK` entre 1 y 365 |
| `Observaciones` | `character varying(500)` | opcional |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC |

Índice único `IX_OfertasLicitacion_LicitacionId_ProveedorId`. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/licitaciones?busqueda=&estado=&tipo=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaLicitacionesDto` | 400 |
| GET | `api/v1/licitaciones/{id:int}` | — | 200 `LicitacionDetalleDto` (con ofertas) | 404 |
| POST | `api/v1/licitaciones` | `CrearLicitacionRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/licitaciones/{id:int}` | `ActualizarLicitacionRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/licitaciones/{id:int}` | — | 204 | 404, 409 |
| POST | `api/v1/licitaciones/{id:int}/ofertas` | `CrearOfertaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| PUT | `api/v1/licitaciones/{id:int}/ofertas/{ofertaId:int}` | `MejorarOfertaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/licitaciones/{id:int}/cerrar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| POST | `api/v1/licitaciones/{id:int}/adjudicar` | `AdjudicarLicitacionRequestDto { ofertaId }` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/licitaciones/{id:int}/cancelar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| GET | `api/v1/licitaciones/resumen` | — | 200 `ResumenLicitacionesDto` | — |

**DTOs:**
- `CrearLicitacionRequestDto { numero, titulo, descripcion?, tipo, presupuestoReferencial, fechaCierre }`
- `ActualizarLicitacionRequestDto { titulo, descripcion?, presupuestoReferencial, fechaCierre }` (número y tipo no se editan)
- `CrearOfertaRequestDto { proveedorId, monto, plazoEntregaDias, observaciones? }`
- `MejorarOfertaRequestDto { monto }`
- `LicitacionListadoDto { id, numero, titulo, tipo, estado, presupuestoReferencial, fechaCierre, totalOfertas }`
- `LicitacionDetalleDto { id, numero, titulo, descripcion, tipo, estado, presupuestoReferencial, fechaCierre, proveedorAdjudicadoId, proveedorAdjudicadoNombre, montoAdjudicado, fechaRegistro, ofertas: OfertaDto[] }`, con `OfertaDto { id, proveedorId, proveedorNombre, monto, plazoEntregaDias, observaciones, fechaRegistro, esGanadora }` ordenadas por `monto` ascendente y luego `id`.
- `PaginaLicitacionesDto { items, total, pagina, tamanoPagina }`
- `ResumenLicitacionesDto { totalLicitaciones, totalOfertas, presupuestoAbierto, montoAdjudicado, porEstado: EstadoConteoDto[] { estado, total } }` (siempre Abierta, Cerrada, Adjudicada, Cancelada, en ese orden).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos de estado). La búsqueda del listado es sobre número y título, sin distinguir mayúsculas.

*POST licitación*, en este orden:
1. `numero` en blanco → 400 «El número es obligatorio.»; más de 20 caracteres → 400 «El número no puede superar 20 caracteres.» (se guarda con `Trim()` y en mayúsculas).
2. `titulo` en blanco → 400 «El título es obligatorio.»; más de 150 → 400 «El título no puede superar 150 caracteres.»
3. `tipo` distinto de los dos valores (sin distinguir mayúsculas) → 400 «El tipo debe ser ConcursoPrecios o SubastaInversa.»
4. `presupuestoReferencial` ≤ 0 → 400 «El presupuesto referencial debe ser mayor a 0.»; con más de 2 decimales o más de 16 dígitos enteros → 400 «El presupuesto referencial no es válido.»
5. `fechaCierre` no futura → 400 «La fecha de cierre debe ser futura.»
6. `descripcion` más de 500 → 400 «La descripción no puede superar 500 caracteres.» (en blanco → `null`).
7. número repetido → 409 «Ya existe una licitación con el número {numero}.» (también ante `23505`).
La licitación nace `Abierta`.

*PUT licitación:* id inexistente → 404 «La licitación {id} no existe.»; estado distinto de `Abierta` → 409 «La licitación {id} no está abierta.»; luego las reglas 2, 4, 5 y 6 del alta; si el nuevo presupuesto es menor que alguna oferta ya presentada → 400 «El presupuesto no puede ser menor a una oferta ya presentada.»

*DELETE:* 404 si no existe; con ofertas → 409 «La licitación {id} tiene ofertas y no se puede eliminar; cancélela.»

*POST oferta:* 404 «La licitación {id} no existe.»; estado distinto de `Abierta` → 409 «La licitación {id} no está abierta.»; `fechaCierre` ya pasada → 409 «La licitación {id} ya venció su plazo de ofertas.»; `proveedorId` ≤ 0 → 400 «El proveedor es obligatorio.»; monto ≤ 0 → 400 «El monto debe ser mayor a 0.»; monto mayor al presupuesto → 400 «El monto no puede superar el presupuesto referencial.»; plazo fuera de 1–365 → 400 «El plazo de entrega debe estar entre 1 y 365 días.»; observaciones más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»; proveedor inexistente → 400 «El proveedor {id} no existe.»; proveedor inactivo → 400 «El proveedor {id} está inactivo.»; proveedor ya ofertó → 409 «El proveedor {id} ya presentó una oferta en la licitación {licitacionId}.» (también ante `23505`).

*PUT oferta:* 404 «La licitación {id} no existe.» / 404 «La oferta {ofertaId} no existe en la licitación {id}.»; tipo `ConcursoPrecios` → 409 «Solo las subastas inversas admiten mejorar la oferta.»; estado distinto de `Abierta` → 409 «La licitación {id} no está abierta.»; plazo vencido → 409 «La licitación {id} ya venció su plazo de ofertas.»; monto ≤ 0 → 400 «El monto debe ser mayor a 0.»; monto igual o mayor al actual → 400 «La nueva oferta debe ser menor a la actual.»

*POST cerrar:* 404; estado distinto de `Abierta` → 409 «La licitación {id} no está abierta.»

*POST adjudicar:* 404; estado distinto de `Cerrada` → 409 «La licitación {id} debe estar cerrada para adjudicarla.»; `ofertaId` que no es de esa licitación → 400 «La oferta {ofertaId} no pertenece a la licitación {id}.» Guarda `ProveedorAdjudicadoId`, `MontoAdjudicado` y pasa a `Adjudicada`.

*POST cancelar:* 404; estado `Adjudicada` o `Cancelada` → 409 «La licitación {id} no se puede cancelar porque está {Adjudicada|Cancelada}.»

*GET detalle:* 404 «La licitación {id} no existe.» *Listado:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», 400 «El estado debe ser Abierta, Cerrada, Adjudicada o Cancelada.», 400 «El tipo debe ser ConcursoPrecios o SubastaInversa.» (filtros sin distinguir mayúsculas). Orden: `FechaRegistro` descendente, después `Id` descendente.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1); `totalOfertas` del listado sale de un conteo en la misma consulta. Las acciones de estado cargan la licitación, validan y guardan con `CompleteAsync()` (un solo guardado por acción).

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY` con ids explícitos y `setval`, igual que el resto del archivo):

| Id | Número | Título | Tipo | Estado | Presupuesto | Cierre |
|---|---|---|---|---|---|---|
| 1 | LIC-2026-001 | Suministro de empaques | ConcursoPrecios | Abierta | 20000.00 | 2099-12-31 |
| 2 | LIC-2026-002 | Flete marítimo Callao–Shanghái | SubastaInversa | Abierta | 50000.00 | 2099-12-31 |
| 3 | LIC-2026-003 | Repuestos de maquinaria | ConcursoPrecios | Adjudicada (proveedor 1, 28000.00) | 30000.00 | 2026-10-15 |
| 4 | LIC-2026-004 | Uniformes de planta | ConcursoPrecios | Cerrada | 8000.00 | 2026-10-30 |
| 5 | LIC-2026-005 | Servicio de limpieza | ConcursoPrecios | Cancelada | 12000.00 | 2026-11-30 |

| Licitación | Proveedor | Monto | Plazo (días) |
|---|---|---|---|
| 1 | 1 | 18500.00 | 15 |
| 1 | 2 | 19200.00 | 10 |
| 2 | 3 | 47000.00 | 30 |
| 2 | 6 | 45500.00 | 25 |
| 3 | 1 | 28000.00 | 20 |
| 3 | 4 | 29500.00 | 18 |
| 4 | 2 | 7500.00 | 12 |

Resumen esperado del seed: 5 licitaciones, 7 ofertas, presupuesto abierto 70000.00, monto adjudicado 28000.00 y conteo por estado Abierta 2 / Cerrada 1 / Adjudicada 1 / Cancelada 1.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas de licitación | tests: cada mensaje de alta, edición y baja de §4 con su código, literal y sin `Detail`; número guardado en mayúsculas y sin espacios; descripción en blanco → `null` | pasan |
| A4 | Reglas de ofertas | tests: oferta válida, proveedor inactivo, proveedor repetido, monto sobre el presupuesto, plazo vencido, licitación no abierta; mejora de oferta solo en subasta inversa y solo a la baja | pasan |
| A5 | Estados | tests: abrir→cerrar→adjudicar, cancelar desde `Abierta` y `Cerrada`, y cada transición inválida con su 409 literal; la adjudicación guarda proveedor y monto y marca `esGanadora` | pasan |
| A6 | Consultas | tests con datos conocidos: filtros por estado, tipo y búsqueda, paginación con `total`, `totalOfertas`, ofertas del detalle ordenadas por monto, resumen exacto del seed | pasan |
| A7 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listado, filtro por estado, detalle, POST de licitación y de oferta, cierre, adjudicación, un 409 literal, DELETE de la licitación creada y resumen del seed | `SMOKE OK` |
| A8 | OpenAPI | `/openapi/v1.json` incluye las 11 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [x] Tarea 1: script SQL, aplicarlo y generar entidades con scaffold.
- [x] Tarea 2: DTOs, `ValidacionLicitacion`, casos de uso y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: verificar A1–A8 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-16 | A1 Compilación | OK | `dotnet build` y `./scripts/verify.sh` (macOS): las 5 capas compilan sin errores |
| 2026-11-16 | A2 Gate | OK | `./scripts/verify.sh` completo en verde: reglas de código y capas (`check-rules.sh`), backend, frontend eslint/tsc/497 tests/build |
| 2026-11-16 | A3 Reglas de licitación | OK | `LicitacionesApiTests`: mensajes literales 400/404/409 de alta, edición y baja sin `Detail`; número guardado en mayúsculas y sin espacios; descripción en blanco → `null` |
| 2026-11-16 | A4 Reglas de ofertas | OK | oferta válida, proveedor inexistente, inactivo y repetido, monto sobre el presupuesto, plazo y observaciones, licitación no abierta y plazo vencido; mejora de oferta solo en subasta inversa y solo a la baja |
| 2026-11-16 | A5 Estados | OK | abrir → cerrar → adjudicar con proveedor y monto guardados y `esGanadora`; cancelar desde `Abierta` y `Cerrada`; cada transición inválida con su 409 literal |
| 2026-11-16 | A6 Consultas | OK | filtros por estado, tipo y búsqueda sin distinguir mayúsculas, paginación con `total`, `totalOfertas`, ofertas del detalle ordenadas por monto y resumen exacto del seed (5 / 7 / 70000 / 28000) |
| 2026-11-16 | A7 Datos y PostgreSQL | OK | `./scripts/aplicar-bd.sh` aplica `005`; `./scripts/scaffold.sh --comprobar` → «SCAFFOLD AL DÍA» y `git status` limpio; `./scripts/smoke-postgres.sh` → `SMOKE OK` |
| 2026-11-16 | A8 OpenAPI | OK | `OpenApiContainsElevenTenderOperations` |

`dotnet test` completo: 988 correctas, 0 con error.

**Notas de entorno (macOS):** `scaffold.sh` y `smoke-postgres.sh` necesitaron rodeos locales en Mac (GNU sed para `\s`, un `ss` de reemplazo y bash 5 por el arreglo vacío con `set -u`). No se modificaron los scripts.
