# Especificación: modulo16-importaciones-backend — Importaciones

## 1. Objetivo
**Problema:** el dominio de Compras tiene proveedores (módulo 11), licitaciones (14) y órdenes de abastecimiento (15), pero no hay forma de seguir una compra que llega del exterior: qué Incoterm se pactó, por qué medio viaja, cuándo embarcó, si ya llegó a puerto y si ya salió de aduana.
**Resultado esperado:** una entidad nueva, `ExpedienteImportacion`, con su tabla y `Modulo16_Importaciones` en `api/v1/importaciones`:
- registrar el expediente de una orden de abastecimiento con su Incoterm, medio de transporte y fechas;
- avanzar el expediente por sus estados: embarque, llegada a puerto o frontera, y liberación;
- cancelar con motivo;
- listado con filtros y paginación, detalle con los datos de la orden y del proveedor, y resumen.

Sigue el patrón de `Modulo14_Licitaciones` (estados con acciones explícitas) y de `Modulo15_OrdenesAbastecimiento` (cabecera ligada a una orden).

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en `Modulo11_Proveedores` ni en `Modulo15_OrdenesAbastecimiento`; la baja de una orden con expedientes queda bloqueada por la clave foránea.
- Agentes aduaneros, DUA/DAM y aranceles: módulo 17 (llegará con su propia spec).
- Prorrateo de fletes, seguros y costo de nacionalización: módulo 18.
- Ingreso de la mercadería al almacén: módulo 19. Liberar un expediente no mueve inventario ni cambia la orden.
- Productos o líneas dentro del expediente: se sigue por la orden.
- Alertas automáticas de retraso: el estado solo cambia por acciones explícitas.
- El frontend (spec `modulo16-importaciones-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Un expediente por embarque, no por orden:** una orden puede tener varios expedientes (embarques parciales). No hay índice único sobre la orden.
2. **Órdenes elegibles:** cualquier orden que no esté `Anulada`. No se exige que el proveedor sea extranjero: el país de salida se escribe en el expediente (`paisOrigen`), porque el proveedor puede despachar desde un tercer país.
3. **Incoterms 2020:** `EXW`, `FCA`, `FAS`, `FOB`, `CFR`, `CIF`, `CPT`, `CIP`, `DAP`, `DPU`, `DDP`. `FAS`, `FOB`, `CFR` y `CIF` solo valen con transporte marítimo.
4. **Medios de transporte:** `Maritimo`, `Aereo` y `Terrestre`.
5. **Estados:** `Programado` → `EnTransito` → `EnAduana` → `Liberado`; `Programado`, `EnTransito` o `EnAduana` → `Cancelado`. `Liberado` y `Cancelado` son finales.
6. **El número lo escribe el usuario** (por ejemplo `IMP-2026-001`), único, en mayúsculas.
7. **El documento de transporte** (BL, AWB o carta de porte) y la fecha de embarque se piden al embarcar, no al crear.
8. **Solo se elimina un expediente `Programado`;** en cualquier otro estado se cancela (o ya es final).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/027-importaciones.sql` | crear | `CREATE TABLE "ExpedientesImportacion"`; número `027` reservado para este módulo en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo añadir la fila 027 de este módulo en §8, sin reescribir la tabla |
| `src/TitanGlobal.Domain/Models/ExpedienteImportacion.cs`, `OrdenAbastecimiento.cs` | regenerar | entidades `partial` generadas por `./scripts/scaffold.sh`, sin editarlas a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet<ExpedienteImportacion>`, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal la entidad (debe quedar `ExpedienteImportacion` / `ExpedientesImportacion`) |
| `src/TitanGlobal.Application/DTOs/Modulo16_Importaciones/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo16_Importaciones/*.cs` | crear | un caso de uso por operación y `ValidacionExpedienteImportacion` |
| `src/TitanGlobal.Api/Controllers/Modulo16_Importaciones/ImportacionesController.cs` | crear | `[Route("api/v1/importaciones")]`, controlador delgado; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` |
| `tests/TitanGlobal.Api.Tests/Modulo16_Importaciones/ImportacionesApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 5 expedientes idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A7) |

## 4. Diseño y lógica
**Tabla `ExpedientesImportacion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Numero` | `character varying(20) NOT NULL` | único (`IX_ExpedientesImportacion_Numero`) |
| `OrdenAbastecimientoId` | `integer NOT NULL` | clave foránea a `OrdenesAbastecimiento("Id")` `ON DELETE RESTRICT`; índice `IX_ExpedientesImportacion_OrdenAbastecimientoId` |
| `Incoterm` | `character varying(3) NOT NULL` | `CHECK` en los 11 valores de la decisión 3 |
| `MedioTransporte` | `character varying(20) NOT NULL` | `CHECK` en (`Maritimo`, `Aereo`, `Terrestre`) |
| `PaisOrigen` | `character varying(100) NOT NULL` | |
| `LugarOrigen` | `character varying(100) NOT NULL` | puerto, aeropuerto o frontera de salida |
| `LugarDestino` | `character varying(100) NOT NULL` | puerto, aeropuerto o frontera de llegada |
| `DocumentoTransporte` | `character varying(40)` | BL, AWB o carta de porte; se llena al embarcar |
| `FechaEmbarque` | `timestamp with time zone` | UTC; se llena al embarcar |
| `FechaLlegadaEstimada` | `timestamp with time zone NOT NULL` | UTC |
| `FechaLlegadaReal` | `timestamp with time zone` | UTC; se llena al llegar a aduana |
| `Estado` | `character varying(20) NOT NULL` | `CHECK` en (`Programado`, `EnTransito`, `EnAduana`, `Liberado`, `Cancelado`) |
| `MotivoCancelacion` | `character varying(500)` | solo con estado `Cancelado` |
| `Observaciones` | `character varying(500)` | opcional |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

`CHECK` adicional `CK_ExpedientesImportacion_LlegadaRealPosteriorEmbarque`: si hay `FechaEmbarque` y `FechaLlegadaReal`, la llegada no es anterior al embarque. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/importaciones?busqueda=&estado=&incoterm=&medioTransporte=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaExpedientesDto` | 400 |
| GET | `api/v1/importaciones/{id:int}` | — | 200 `ExpedienteDetalleDto` | 404 |
| POST | `api/v1/importaciones` | `CrearExpedienteRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/importaciones/{id:int}` | `ActualizarExpedienteRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/importaciones/{id:int}` | — | 204 | 404, 409 |
| POST | `api/v1/importaciones/{id:int}/embarcar` | `EmbarcarExpedienteRequestDto` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/importaciones/{id:int}/arribar` | `ArribarExpedienteRequestDto` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/importaciones/{id:int}/liberar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| POST | `api/v1/importaciones/{id:int}/cancelar` | `CancelarExpedienteRequestDto` | 200 `{ id }` | 400, 404, 409 |
| GET | `api/v1/importaciones/resumen` | — | 200 `ResumenExpedientesDto` | — |

**DTOs:**
- `CrearExpedienteRequestDto { numero, ordenAbastecimientoId, incoterm, medioTransporte, paisOrigen, lugarOrigen, lugarDestino, fechaLlegadaEstimada, observaciones? }`
- `ActualizarExpedienteRequestDto { incoterm, medioTransporte, paisOrigen, lugarOrigen, lugarDestino, fechaLlegadaEstimada, observaciones? }` (número y orden no se editan)
- `EmbarcarExpedienteRequestDto { documentoTransporte, fechaEmbarque }`
- `ArribarExpedienteRequestDto { fechaLlegadaReal }`
- `CancelarExpedienteRequestDto { motivo }`
- `ExpedienteListadoDto { id, numero, ordenNumero, proveedorNombre, incoterm, medioTransporte, estado, fechaLlegadaEstimada }`
- `ExpedienteDetalleDto { id, numero, ordenAbastecimientoId, ordenNumero, proveedorId, proveedorNombre, incoterm, medioTransporte, paisOrigen, lugarOrigen, lugarDestino, documentoTransporte, fechaEmbarque, fechaLlegadaEstimada, fechaLlegadaReal, estado, motivoCancelacion, observaciones, fechaRegistro }`
- `PaginaExpedientesDto { items, total, pagina, tamanoPagina }`
- `ResumenExpedientesDto { totalExpedientes, enCurso, porEstado: EstadoConteoDto[] { estado, total } }` (`enCurso` suma `Programado`, `EnTransito` y `EnAduana`; `porEstado` siempre trae los 5 estados, en el orden de la decisión 5).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos de estado). La búsqueda del listado es sobre número y número de orden, sin distinguir mayúsculas.

*POST expediente*, en este orden:
1. `numero` en blanco → 400 «El número es obligatorio.»; más de 20 caracteres → 400 «El número no puede superar 20 caracteres.» (se guarda con `Trim()` y en mayúsculas).
2. `incoterm` fuera de los 11 valores (sin distinguir mayúsculas) → 400 «El Incoterm no es válido.» (se guarda en mayúsculas).
3. `medioTransporte` distinto de los tres valores (sin distinguir mayúsculas) → 400 «El medio de transporte debe ser Maritimo, Aereo o Terrestre.»
4. `incoterm` `FAS`, `FOB`, `CFR` o `CIF` con medio distinto de `Maritimo` → 400 «El Incoterm {incoterm} solo aplica al transporte marítimo.»
5. `paisOrigen`, `lugarOrigen` o `lugarDestino` en blanco → 400 «El país de origen es obligatorio.» / «El lugar de origen es obligatorio.» / «El lugar de destino es obligatorio.»; más de 100 caracteres → 400 «El país de origen no puede superar 100 caracteres.» (y su equivalente para los otros dos).
6. `fechaLlegadaEstimada` no futura → 400 «La fecha de llegada estimada debe ser futura.»
7. `observaciones` más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.» (en blanco → `null`).
8. `ordenAbastecimientoId` ≤ 0 → 400 «La orden de abastecimiento es obligatoria.»; inexistente → 400 «La orden de abastecimiento {id} no existe.»; orden `Anulada` → 409 «La orden de abastecimiento {id} está anulada.»
9. número repetido → 409 «Ya existe un expediente con el número {numero}.» (también ante `23505`).
El expediente nace `Programado`.

*PUT expediente:* id inexistente → 404 «El expediente {id} no existe.»; estado distinto de `Programado` → 409 «El expediente {id} no está programado.»; luego las reglas 2 a 7 del alta.

*DELETE:* 404 si no existe; estado distinto de `Programado` → 409 «El expediente {id} no está programado y no se puede eliminar; cancélelo.»

*POST embarcar:* 404; estado distinto de `Programado` → 409 «El expediente {id} no está programado.»; `documentoTransporte` en blanco → 400 «El documento de transporte es obligatorio.»; más de 40 → 400 «El documento de transporte no puede superar 40 caracteres.»; `fechaEmbarque` futura → 400 «La fecha de embarque no puede ser futura.» Guarda documento y fecha y pasa a `EnTransito`.

*POST arribar:* 404; estado distinto de `EnTransito` → 409 «El expediente {id} no está en tránsito.»; `fechaLlegadaReal` futura → 400 «La fecha de llegada no puede ser futura.»; anterior a `FechaEmbarque` → 400 «La fecha de llegada no puede ser anterior al embarque.» Guarda la fecha y pasa a `EnAduana`.

*POST liberar:* 404; estado distinto de `EnAduana` → 409 «El expediente {id} no está en aduana.» Pasa a `Liberado`.

*POST cancelar:* 404; `motivo` en blanco → 400 «El motivo de cancelación es obligatorio.»; más de 500 → 400 «El motivo no puede superar 500 caracteres.»; estado `Liberado` o `Cancelado` → 409 «El expediente {id} no se puede cancelar porque está {Liberado|Cancelado}.» Guarda el motivo y pasa a `Cancelado`.

*GET detalle:* 404 «El expediente {id} no existe.» *Listado:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», 400 «El estado debe ser Programado, EnTransito, EnAduana, Liberado o Cancelado.», 400 «El Incoterm no es válido.», 400 «El medio de transporte debe ser Maritimo, Aereo o Terrestre.» Orden: `FechaLlegadaEstimada` ascendente y luego `Id`.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1); número de orden y nombre del proveedor salen del `Include` proyectado en la misma consulta. Las acciones de estado cargan el expediente, validan y guardan con `CompleteAsync`.

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY` y `INSERT` que busca la orden por su número, igual que el seed de órdenes; va después del bloque de órdenes de abastecimiento):

| Id | Número | Orden | Incoterm | Medio | Origen → Destino | Estado |
|---|---|---|---|---|---|---|
| 1 | IMP-2026-001 | OA-DEV-001 | CIF | Maritimo | China, Shanghái → Callao | EnTransito (embarque 2026-11-01, documento `MSKU1234567`) |
| 2 | IMP-2026-002 | OA-DEV-001 | FOB | Maritimo | Chile, Valparaíso → Callao | Programado (llegada estimada 2099-01-15) |
| 3 | IMP-2026-003 | OA-DEV-002 | DAP | Terrestre | Chile, Arica → Tacna | EnAduana (embarque 2026-11-02, llegada real 2026-11-05, documento `CRT-0045`) |
| 4 | IMP-2026-004 | OA-DEV-003 | FCA | Aereo | China, Shenzhen → Lima | Liberado (embarque 2026-02-10, llegada real 2026-02-14, documento `AWB-77701`) |
| 5 | IMP-2026-005 | OA-DEV-002 | EXW | Maritimo | China, Ningbó → Callao | Cancelado (motivo «Proveedor no pudo despachar») |

Resumen esperado del seed: 5 expedientes, 3 en curso y conteo por estado Programado 1 / EnTransito 1 / EnAduana 1 / Liberado 1 / Cancelado 1.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas del expediente | tests: cada mensaje de alta, edición y baja de §4 con su código, literal y sin `Detail`; número e Incoterm guardados en mayúsculas; observaciones en blanco → `null`; Incoterms marítimos rechazados con transporte aéreo y terrestre | pasan |
| A4 | Orden asociada | tests: orden inexistente, orden anulada, varias órdenes con expediente propio y dos expedientes sobre la misma orden | pasan |
| A5 | Estados | tests: programado → embarcado → en aduana → liberado, cancelar desde los tres estados abiertos, y cada transición inválida con su 409 literal; fechas y documento guardados | pasan |
| A6 | Consultas | tests con datos conocidos: filtros por estado, Incoterm, medio y búsqueda, paginación con `total`, orden de la lista y resumen exacto del seed | pasan |
| A7 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listado, filtro por estado, detalle, POST, embarque, llegada, liberación, cancelación y un número repetido | `SMOKE OK` |
| A8 | OpenAPI | `/openapi/v1.json` incluye las 10 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [x] Tarea 1: script SQL, aplicarlo y generar entidades con scaffold.
- [x] Tarea 2: DTOs, `ValidacionExpedienteImportacion`, casos de uso y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: verificar A1–A8 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-25 | A1, A2 | cumple | `./scripts/verify.sh` en verde (formato, reglas de código, compilación con `-warnaserror`, tests de backend y frontend) |
| 2026-11-25 | A3, A4, A5, A6 | cumple | `ImportacionesApiTests` dentro de `./scripts/verify.sh` |
| 2026-11-25 | A7 | cumple | `./scripts/aplicar-bd.sh --reiniciar`, `./scripts/scaffold.sh` y `./scripts/smoke-postgres.sh` terminan en `SMOKE OK` |
| 2026-11-25 | A8 | cumple | el humo valida las 10 operaciones en `/openapi/v1.json` (`openapi-importaciones`) |
