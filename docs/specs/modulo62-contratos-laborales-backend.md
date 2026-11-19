# Especificación: modulo62-contratos-laborales-backend — ContratosLaborales (backend)

> Estado: **aprobada** por Lukas el 2026-11-18, con la opción A (recomendada) en las 8 decisiones de §2.

## 1. Objetivo
**Problema:** no existe el contrato laboral de los empleados. Los módulos 63 (asistencia, A1), 65 (vacaciones), 66 (planillas, sueldo básico), 67 y 68 (D5) dependen de saber qué contrato está vigente y con qué cargo y sueldo (`docs/hcm-reglas-negocio.md` §2, módulo 62).
**Resultado esperado:** tablas `ContratosLaborales` y `AdendasContrato` y `Modulo62_ContratosLaborales` en `api/v1/contratos-laborales`:
- alta de contrato con número autogenerado, modalidad, fechas, cargo y sueldo básico, con un único contrato vigente por empleado (C1, C2, C3);
- renovación (C4) y adendas de cargo o sueldo (C5) sin sobrescribir el historial (T3);
- estado `Vencido` calculado al consultar (C6, decisión 1);
- listado paginado con filtros y detalle con su historial de adendas.

Reglas aplicadas: C1 a C6, T1, T3, T4, T5, T6 y E4 de `docs/hcm-reglas-negocio.md`. Todo valor que esas reglas no fijan se marca **[elegido]** en §4; lo que además cambia el producto está en las decisiones de §2.

## 2. Fuera de alcance
- El frontend de contratos (spec propia posterior, `modulo62-contratos-laborales-frontend.md`).
- Planillas y cálculo de sueldos (módulo 66), asistencia (63), vacaciones (65) y evaluaciones (68): solo consumirán este módulo.
- Edición libre de un contrato ya registrado y borrado físico: no hay `PUT` ni `DELETE` (T3). Los cambios de cargo o sueldo van por adenda (C5); los de modalidad o fechas, por renovación o anulación (decisión 5).
- Sincronizar `Empleados.Cargo` con el cargo del contrato o de sus adendas: el 61 conserva su propio cargo.
- Gratificaciones, CTS, beneficios, periodo de prueba, moneda distinta de PEN (S1, S2), documentos adjuntos y firma digital.
- Un proceso programado (job) que cambie estados (decisión 1).
- Cambios en `Middlewares/`, `Services/`, `Program.cs`, `Configuration/`, `Models/Rol.cs` y `Models/Usuario.cs`, en las cuatro entidades base y en `UnitOfWork`.
- Modificar el módulo 61 salvo lo que apruebe la decisión 2.

**Decisiones de producto, aprobadas por Lukas el 2026-11-18 con la opción A (recomendada) en todas:**

1. **C6, estado `Vencido` sin proceso diario.** La regla C6 pide un «proceso diario», pero el proyecto no tiene ningún mecanismo de tareas programadas (búsqueda de `BackgroundService`, `IHostedService`, Hangfire y Quartz en `src/`: sin resultados). Los módulos 03, 05 y 09 resuelven «vencida» calculándolo al leer.
   - **A (recomendada):** `Vencido` se calcula al leer: un contrato con `Estado = Vigente` guardado y `FechaFin` anterior a hoy se devuelve como `Vencido` (filtro y listado incluidos). El estado guardado sigue `Vigente`. Además, cada escritura sobre los contratos de un empleado (alta, renovación) guarda como `Vencido` los contratos de ese empleado que ya pasaron su fecha de fin, para liberar la unicidad de C2 (decisión 7).
   - B: un endpoint o job que recorra y actualice. Exige crear el primer `BackgroundService` del proyecto o un endpoint operado a mano, y `Program.cs` queda fuera de alcance.
2. **E4, efecto del cese del 61.** E4 exige cerrar los contratos vigentes en la fecha de cese, y hoy `AdministrarEmpleadosUseCase.CesarAsync` solo cambia estado, fecha y motivo (spec 61 §4, «Nada más: E4 queda pendiente»).
   - **A (recomendada):** esta spec modifica `CesarAsync` del 61 (mismo dueño): dentro de una `TransactionScope` los contratos con `Estado = Vigente` del empleado y `FechaFin` nula o `>= fechaCese` pasan a `Cerrado` con `FechaCierre = fechaCese` y `MotivoCierre = "Cese del empleado"`. Los ya vencidos (`FechaFin < fechaCese`) no se tocan. `FechaFin` no se modifica (T3). Si un contrato vigente empieza después de la fecha de cese, el cese se rechaza con 409 (mensaje 33). Afecta: `AdministrarEmpleadosUseCase.cs` (un método), `ValidacionEmpleado.cs` solo si hace falta, las pruebas T7 del 61 (se añaden casos, no se tocan los existentes), el bloque de cese del smoke y la nota «E4 queda pendiente» de la spec 61. El 61 pasa a depender de la tabla `ContratosLaborales`.
   - B: dejar E4 fuera de la v0. Entonces no existe el estado `Cerrado`, y un empleado cesado conserva su contrato `Vigente` hasta que se resuelva; los módulos 63 y 66 tendrían que filtrar por `Empleados.Estado`.
3. **Numeración del contrato.**
   - **A (recomendada):** `Numero` autogenerado `CTR-000001` en el servidor, con el mismo patrón del legajo `EMP-` y de `CC-`/`SC-`/`OA-`: se inserta con `TMP-` + 16 hexadecimales y, en la misma transacción, se actualiza a `CTR-{Id:D6}`. Único (`IX_ContratosLaborales_Numero`).
   - B: sin número; el contrato se identifica por `Id`. Menos columnas, pero los mensajes y la UI de otros módulos tendrían que citar el `Id`.
4. **Adendas (C5).**
   - **A (recomendada):** tabla propia `AdendasContrato`, una fila por adenda, que guarda el cargo y el sueldo resultantes desde su fecha de vigencia. El contrato conserva sus valores originales. Respeta T3: el historial no se sobrescribe y 66 podrá prorratear el sueldo por tramos.
   - B: actualizar `Cargo` y `SueldoBasico` del contrato. Más simple, pero pierde el historial y viola T3.
   - Sub-decisión **[elegido]**: la fecha de vigencia puede ser pasada (no hay periodo cerrado aún, porque 66 no existe) y debe ser posterior a la de la última adenda del contrato. Cuando exista 66, habrá que impedir adendas con vigencia en un periodo ya aprobado (T3).
5. **Estados y transiciones.** C1 a C6 solo nombran `Vigente`, `Renovado` y `Vencido`. Para cumplir E4 y poder corregir errores de captura hacen falta dos más, que **no están en C1 a C6**:
   - `Cerrado`: lo asigna solo el cese del empleado (decisión 2 A). Si se elige 2 B, no existe.
   - `Anulado`: contrato registrado por error, con motivo obligatorio; sin él, un contrato mal capturado bloquearía para siempre al empleado (C2) porque no hay edición.
   - **Opciones:** **A (recomendada)** `Vigente`, `Renovado`, `Vencido` (calculado), `Cerrado` y `Anulado`. B: sin `Anulado` (el error de captura solo se corrige con una adenda o renovando). C: sin `Anulado` pero con `PUT` mientras el contrato esté `Vigente` y sin adendas (rompe T3 si luego hay planilla).
   - Transiciones propuestas (A): `Vigente` → `Renovado` (renovación), `Vigente` → `Cerrado` (cese), `Vigente` → `Anulado` (anulación), `Vigente` → `Vencido` (solo por fecha, calculado y, tras una escritura del empleado, guardado). `Renovado`, `Vencido` guardado, `Cerrado` y `Anulado` son finales.
   - Sub-decisión **[elegido]**: se puede renovar mientras el estado guardado sea `Vigente`, aunque su fecha de fin ya haya pasado (renovación tardía): el nuevo contrato empieza el día siguiente al fin, así no queda hueco.
6. **Roles que pueden escribir.** La decisión 4 de §3 de `docs/hcm-reglas-negocio.md` fija `RRHH` o `ADMIN` solo para A7 y V5; para contratos no está decidido. Toda la API exige token (spec 51: `FallbackPolicy` autenticado).
   - **A (recomendada):** escritura (`POST` de alta, renovación, adenda y anulación) con `[Authorize(Roles = "RRHH,ADMIN")]`; lectura para cualquier usuario autenticado, como el 61. El rol `RRHH` **no existe en el seed**: se añade a `Roles` (Id 6) con este módulo, tal como prevé la decisión 4. Efecto: hay que actualizar dos comprobaciones existentes de `smoke-postgres.sh` (`roles-list` y `roles-matrix`, hoy con 5 roles) y las pruebas que cuenten roles.
   - B: lectura también restringida a `RRHH,ADMIN` (el sueldo es dato confidencial).
   - C: solo `ADMIN`; no hace falta crear el rol `RRHH` todavía.
7. **C2, cómo se garantiza «un solo contrato vigente».** `UnitOfWork` solo traduce `23505` y `23503` a `ViolacionRestriccionException`; una restricción de exclusión (`23P01`) acabaría en 500 y traducirla exige tocar `Infrastructure`.
   - **A (recomendada):** índice único parcial `("EmpleadoId") WHERE "Estado" = 'Vigente'`, más comprobación previa en `Application`. La carrera se traduce a 409 con `ViolacionRestriccionException` (`Unicidad`, `IX_ContratosLaborales_EmpleadoId_Vigente`), como el documento único del 61. Combina con la decisión 1 A (los vencidos se materializan antes de insertar). Es **más estricto que C2**: no se puede registrar por adelantado un contrato futuro mientras haya otro `Vigente` (se usa la renovación), ni cargar contratos históricos de periodos anteriores a uno vigente.
   - B: comprobar solapamiento por fechas en `Application` y añadir `EXCLUDE USING gist` con `btree_gist` como red de seguridad. Exacto a C2, pero una carrera responde 500 hasta que se traduzca `23P01`, y hay que comprobar que `./scripts/scaffold.sh --comprobar` tolera la restricción.
   - C: solo comprobación en `Application` (ventana de carrera sin protección).
8. **Vínculo con el empleado.** C1 a C6 no dicen nada sobre el empleado del contrato. Propuesta (derivada de T1/T2 y de E2, **no escrita en C1 a C6**): **A (recomendada)** no se registran ni renuevan contratos de un empleado `Cesado` (409, mensaje 14) y la fecha de inicio no puede ser anterior a la fecha de ingreso del empleado (400, mensaje 15). B: ninguna de las dos validaciones.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/008-contratos-laborales.sql` | crear | `CREATE TABLE "ContratosLaborales"` y `"AdendasContrato"` con sus índices (§4). Número 008 asignado por Lukas el 2026-11-18 para ordenar los scripts de las ramas abiertas |
| `src/TitanGlobal.Domain/Models/ContratoLaboral.cs` | regenerar | entidad `partial` generada por `./scripts/scaffold.sh` (no se edita) |
| `src/TitanGlobal.Domain/Models/AdendaContrato.cs` | regenerar | ídem |
| `src/TitanGlobal.Domain/Models/Empleado.cs` | regenerar | solo gana la navegación `ContratosLaborales` al regenerar; no se edita a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | dos `DbSet` y su configuración, generados |
| `src/TitanGlobal.Domain/Models/ValoresIniciales.cs` | no cambia | el estado inicial lo fija el `UseCase` |
| `src/TitanGlobal.Application/DTOs/Modulo62_ContratosLaborales/*.cs` | crear | DTOs de §4, un archivo por clase |
| `src/TitanGlobal.Application/UseCases/Modulo62_ContratosLaborales/AdministrarContratosLaboralesUseCase.cs` | crear | `UseCase` concreto, sin interfaz |
| `src/TitanGlobal.Application/UseCases/Modulo62_ContratosLaborales/ValidacionContratoLaboral.cs` | crear | helper estático interno |
| `src/TitanGlobal.Api/Controllers/Modulo62_ContratosLaborales/ContratosLaboralesController.cs` | crear | `[Route("api/v1/contratos-laborales")]` |
| `src/TitanGlobal.Application/UseCases/Modulo61_Empleados/AdministrarEmpleadosUseCase.cs` | modificar | **solo si se aprueba la decisión 2 A**: `CesarAsync` cierra los contratos vigentes (E4) |
| `docs/specs/modulo61-empleados-backend.md` | modificar | **solo con la decisión 2 A**: sustituye «E4 queda pendiente» por la referencia a esta spec |
| `tests/TitanGlobal.Api.Tests/Modulo62_ContratosLaborales/ContratosLaboralesApiTests.cs` | crear | pruebas de §5 |
| `tests/TitanGlobal.Api.Tests/Modulo61_Empleados/EmpleadosApiTests.cs` | modificar | **solo con la decisión 2 A**: casos nuevos de cese con contratos; no se modifican los existentes |
| `scripts/seed-dev.sql` | modificar | bloque nuevo justo antes de `COMMIT;`, después del bloque de `Empleados` (§4). Con la decisión 6 A, una fila más en el `INSERT` de `Roles` |
| `scripts/smoke-postgres.sh` | modificar | variables, claves nuevas del diccionario y `request_check` nuevos antes de `printf 'SMOKE OK\n'`; con la decisión 6 A, `roles-list` y `roles-matrix` pasan de 5 a 6 roles |

Verificado en el repositorio: existen `database/001-esquema-inicial.sql`, `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh`, `scripts/aplicar-bd.sh`, `scripts/scaffold.sh`, `src/TitanGlobal.Domain/Models/ValoresIniciales.cs`, `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs`, `src/TitanGlobal.Domain/Models/Empleado.cs`, `src/TitanGlobal.Application/UseCases/Modulo61_Empleados/AdministrarEmpleadosUseCase.cs`, `tests/TitanGlobal.Api.Tests/Modulo61_Empleados/EmpleadosApiTests.cs` y `docs/specs/modulo61-empleados-backend.md`. Las carpetas `Modulo62_ContratosLaborales` de DTOs, UseCases, Controllers y tests, y los archivos marcados «crear», no existen todavía. Los `UseCase` se registran por reflexión, así que `Program.cs` no cambia. `CatalogoModulos` ya incluye el módulo 62 (`ContratosLaborales`).

## 4. Diseño y lógica
**Convenciones.** Sin `var`, sin miembros con `=>` y sin interfaces de `UseCase` (`scripts/check-rules.sh`). Controlador delgado: solo invoca al `UseCase`. Fechas de negocio `DateOnly` mapeadas a `date` (T5); importes `decimal` con `numeric(12,2)` (T4, ningún `double`). «Hoy» es `DateOnly.FromDateTime(DateTime.UtcNow)` **[elegido]** (fecha UTC, no hora de Lima). Sin auditoría propia: el módulo 77 ya registra altas y modificaciones.

**Autenticación.** Toda la API exige JWT (`FallbackPolicy`, spec 51): sin token o con token inválido, 401 «Debe iniciar sesión para acceder a este recurso.». Las escrituras llevan además el rol de la decisión 6: sin el rol, 403 «Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.». Ambos textos los escribe JwtBearer; esta spec no los redefine.

**Tabla `ContratosLaborales`** (`database/008-contratos-laborales.sql`; entidad `ContratoLaboral`, generada):
| Columna | Tipo | Restricción |
|---|---|---|
| `Id` | `integer` identidad | PK `PK_ContratosLaborales` |
| `Numero` | `character varying(20)` | `NOT NULL`, único `IX_ContratosLaborales_Numero` (decisión 3 A). Valor final `CTR-{Id:D6}` |
| `EmpleadoId` | `integer` | `NOT NULL`, FK a `Empleados("Id")` `ON DELETE RESTRICT`, índice `IX_ContratosLaborales_EmpleadoId` |
| `Modalidad` | `character varying(20)` | `NOT NULL`; `Indeterminado`, `PlazoFijo` o `Formativo` (C1) |
| `FechaInicio` | `date` | `NOT NULL` |
| `FechaFin` | `date` | nula solo en `Indeterminado` (C1) |
| `SueldoBasico` | `numeric(12,2)` | `NOT NULL`, `> 0` (C3, T4). Máximo 9999999999.99 |
| `Cargo` | `character varying(100)` | `NOT NULL`. Cargo original del contrato; las adendas lo cambian sin sobrescribirlo |
| `Estado` | `character varying(20)` | `NOT NULL`; `Vigente`, `Renovado`, `Vencido`, `Cerrado` o `Anulado` (decisión 5) |
| `ContratoAnteriorId` | `integer` | nula; FK a `ContratosLaborales("Id")` `ON DELETE RESTRICT`, índice. Lo fija la renovación (C4) |
| `FechaCierre` | `date` | nula; solo con `Cerrado` o `Anulado` |
| `MotivoCierre` | `character varying(300)` | nula; `Cese del empleado` en `Cerrado`; el motivo del usuario en `Anulado` |

Índices y guardas de base de datos (decisión 7 A):
- `CREATE UNIQUE INDEX "IX_ContratosLaborales_EmpleadoId_Vigente" ON "ContratosLaborales" ("EmpleadoId", "Estado") WHERE "Estado" = 'Vigente';` es la guarda de C2 ante carreras. Incluye `Estado`, constante dentro del filtro, porque con solo `EmpleadoId` el scaffold genera una relación 1 a 1 con `Empleados`.
- `CHECK` de red de seguridad (la validación previa de `Application` es la que responde): `"SueldoBasico" > 0`, `("Modalidad" = 'Indeterminado') = ("FechaFin" IS NULL)` y `"FechaFin" IS NULL OR "FechaFin" > "FechaInicio"`. Una violación de `CHECK` (`23514`) no la traduce `UnitOfWork`, por eso las reglas se validan antes de guardar.

**Tabla `AdendasContrato`** (entidad `AdendaContrato`, generada):
| Columna | Tipo | Restricción |
|---|---|---|
| `Id` | `integer` identidad | PK `PK_AdendasContrato` |
| `ContratoLaboralId` | `integer` | `NOT NULL`, FK a `ContratosLaborales("Id")` `ON DELETE RESTRICT` |
| `FechaVigencia` | `date` | `NOT NULL` |
| `Cargo` | `character varying(100)` | `NOT NULL`. Cargo resultante desde `FechaVigencia` |
| `SueldoBasico` | `numeric(12,2)` | `NOT NULL`, `> 0`. Sueldo resultante desde `FechaVigencia` |

Índice único `IX_AdendasContrato_ContratoLaboralId_FechaVigencia` sobre (`ContratoLaboralId`, `FechaVigencia`). Cargo y sueldo vigentes en una fecha `f` = los de la adenda con mayor `FechaVigencia <= f`; si no hay, los del contrato.

**Contrato HTTP.** JSON en camelCase, fechas `yyyy-MM-dd`. Errores `{ StatusCode, Message }` (`ErrorHandlingMiddleware`); `Detail` solo en 500.

| Verbo | Ruta | Entrada | Respuesta OK | Errores | Acceso |
|---|---|---|---|---|---|
| GET | `api/v1/contratos-laborales?busqueda=&estado=&modalidad=&empleadoId=&pagina=1&tamanoPagina=10` | query opcional | 200 `PaginaContratosLaboralesDto` | 400, 401 | autenticado (6 A) |
| GET | `api/v1/contratos-laborales/{id:int}` | — | 200 `ContratoLaboralDetalleDto` | 401, 404 | autenticado (6 A) |
| POST | `api/v1/contratos-laborales` | `GuardarContratoLaboralRequestDto` | 200 `ContratoLaboralCreadoDto` | 400, 401, 403, 404, 409 | `RRHH`, `ADMIN` |
| POST | `api/v1/contratos-laborales/{id:int}/renovacion` | `RenovarContratoLaboralRequestDto` | 200 `ContratoLaboralCreadoDto` (el contrato nuevo) | 400, 401, 403, 404, 409 | `RRHH`, `ADMIN` |
| POST | `api/v1/contratos-laborales/{id:int}/adendas` | `AdendaContratoRequestDto` | 200 `{ id }` (id de la adenda) | 400, 401, 403, 404, 409 | `RRHH`, `ADMIN` |
| POST | `api/v1/contratos-laborales/{id:int}/anulacion` | `AnularContratoLaboralRequestDto` | 200 `{ id }` | 400, 401, 403, 404, 409 | `RRHH`, `ADMIN` (solo con la decisión 5 A) |

- Todas las operaciones llevan cuerpo: sin `[PermitirCuerpoVacio]`. Un cuerpo vacío responde 400 con el literal del middleware.
- Sin `PUT` ni `DELETE`: esas rutas no existen (T3). Cada contrato de un empleado se consulta con `?empleadoId=`; no hay ruta anidada en `empleados`.

**DTOs** (`src/TitanGlobal.Application/DTOs/Modulo62_ContratosLaborales/`):
- `GuardarContratoLaboralRequestDto { EmpleadoId (int?), Modalidad, FechaInicio (DateOnly?), FechaFin (DateOnly?), SueldoBasico (decimal?), Cargo }`. Los campos obligatorios numéricos y de fecha son nulables para devolver el mensaje propio cuando faltan. No incluye `Numero`, `Estado` ni campos de cierre.
- `RenovarContratoLaboralRequestDto { Modalidad, FechaFin (DateOnly?), SueldoBasico (decimal?), Cargo }`. La fecha de inicio la calcula el servidor (C4). Cargo y sueldo son obligatorios, sin heredar los del contrato anterior **[elegido]**.
- `AdendaContratoRequestDto { FechaVigencia (DateOnly?), Cargo (string?), SueldoBasico (decimal?) }`. Al menos uno de `Cargo` y `SueldoBasico` (C5); el que no llega conserva el valor vigente en esa fecha.
- `AnularContratoLaboralRequestDto { Motivo }`.
- `ContratoLaboralCreadoDto { Id, Numero }`.
- `PaginaContratosLaboralesDto { Items, Total, Pagina, TamanoPagina }`.
- `ContratoLaboralListadoDto { Id, Numero, EmpleadoId, EmpleadoLegajo, EmpleadoNombre, Modalidad, FechaInicio, FechaFin, SueldoBasico, Cargo, Estado }`. `EmpleadoNombre` es `Apellidos, Nombres`. `Estado` es el **efectivo** (decisión 1 A: `Vencido` si corresponde). `SueldoBasico` y `Cargo` son los originales del contrato.
- `ContratoLaboralDetalleDto : ContratoLaboralListadoDto` con `ContratoAnteriorId, ContratoAnteriorNumero, FechaCierre, MotivoCierre, CargoVigente, SueldoVigente, Adendas`. `CargoVigente` y `SueldoVigente` se evalúan a hoy. `Adendas` es una lista de `AdendaContratoDto { Id, FechaVigencia, Cargo, SueldoBasico }` ordenada por `FechaVigencia`.

**Estado efectivo.** Un registro con `Estado = Vigente` y `FechaFin` no nula y menor que hoy se devuelve como `Vencido`. El filtro `estado=Vigente` exige `FechaFin` nula o `>= hoy`; `estado=Vencido` exige `Vigente` guardado con `FechaFin < hoy` o `Vencido` guardado. Nunca se lee un contrato con `FechaFin` pasada como `Vigente`.

**Reglas**, en el `UseCase` con `ReglaNegocioException` y `CodigosEstado`. Gana el primer fallo. Textos en la tabla de mensajes.

*Validación de campos* (`ValidacionContratoLaboral`; en alta, renovación y adenda, en este orden, saltando lo que no aplica):
1. `empleadoId` presente (1). Solo alta.
2. `modalidad`, tras `Trim()`, es `Indeterminado`, `PlazoFijo` o `Formativo`, distinguiendo mayúsculas (2).
3. `fechaInicio` presente (3). Solo alta.
4. `sueldoBasico`: presente (4); `> 0` (5); `<= 9999999999.99` (6); como máximo 2 decimales (7). En la adenda solo se valida si llega.
5. `cargo`, tras `Trim()`: obligatorio (8), máximo 100 (9). En la adenda solo se valida si llega con texto.
6. Fechas de fin (C1, C3): `Indeterminado` con `fechaFin` (10); `PlazoFijo` o `Formativo` sin `fechaFin` (11); `fechaFin <= fechaInicio` (12). En la renovación la fecha de inicio es la derivada (C4), así que la regla 12 se evalúa tras cargar el contrato.

*Alta* (`CrearAsync`):
1. Validación de campos (1 a 12).
2. Empleado inexistente, 404 (13).
3. Si el empleado está `Cesado`, 409 (14) y, si `fechaInicio < FechaIngreso`, 400 (15). Ambas, solo con la decisión 8 A.
4. Transacción: (a) se guardan como `Vencido` los contratos `Vigente` del empleado con `FechaFin < hoy` (decisión 1 A); (b) si queda un contrato `Vigente` del empleado, 409 (16); (c) se inserta con `Numero` temporal `TMP-` + 16 hexadecimales y `Estado = Vigente`; (d) con el `Id` se actualiza `Numero = CTR-{Id:D6}`. Es el mismo patrón que `AdministrarEmpleadosUseCase.CrearAsync`.
5. Si PostgreSQL responde `23505` sobre `IX_ContratosLaborales_EmpleadoId_Vigente` (carrera), mismo 409 (16), detectado con `ViolacionRestriccionException` (`TipoViolacionRestriccion.Unicidad`, `Restriccion`). Cualquier otro `23505` se propaga. Un `23503` de `EmpleadoId` (empleado eliminado en paralelo) se traduce a 404 (13).

*Renovación* (`RenovarAsync(id, solicitud)`, C4):
1. Validación de campos (2, 4, 5 y 6 con `fechaFin`).
2. Contrato inexistente, 404 (17).
3. `Estado` guardado distinto de `Vigente`, 409 (18). Un contrato `Vigente` cuya fecha de fin ya pasó **sí** se puede renovar (decisión 5, sub-decisión).
4. Contrato `Indeterminado` (sin fecha de fin), 409 (19).
5. Empleado `Cesado`, 409 (14) (decisión 8 A).
6. `fechaInicio = contrato.FechaFin + 1 día`. Si `fechaFin <= fechaInicio`, 400 (12).
7. Transacción en dos guardados: primero el contrato anterior pasa a `Renovado` (libera la unicidad parcial); después se inserta el nuevo con `ContratoAnteriorId = id`, `Estado = Vigente` y el número `CTR-`. Si algo falla, se revierte todo. `23505` sobre el índice vigente se traduce a 409 (16).

*Adenda* (`AgregarAdendaAsync(id, solicitud)`, C5):
1. `fechaVigencia` presente (20).
2. `cargo` o `sueldoBasico` presente (21); validación de campos 4 y 5 de lo que llegue.
3. Contrato inexistente, 404 (17).
4. `Estado` guardado distinto de `Vigente`, 409 (22).
5. `fechaVigencia` fuera de [`FechaInicio`, `FechaFin`] (sin tope superior en `Indeterminado`), 400 (23).
6. Existe una adenda con `FechaVigencia >=` la recibida, 409 (24) (sub-decisión de la decisión 4). La carrera sobre `IX_AdendasContrato_ContratoLaboralId_FechaVigencia` se traduce al mismo 409 (24).
7. Si, resueltos cargo y sueldo (lo no enviado = vigente en `fechaVigencia`), ninguno cambia, 400 (25).
8. Se inserta la adenda con cargo y sueldo resultantes. El contrato no se modifica: modalidad y fechas no cambian (C5).

*Anulación* (`AnularAsync(id, solicitud)`; solo con la decisión 5 A):
1. `motivo`, tras `Trim()`: obligatorio (26), máximo 300 (27).
2. Contrato inexistente, 404 (17).
3. `Estado` guardado distinto de `Vigente`, 409 (28).
4. `Estado = Anulado`, `MotivoCierre = motivo`, `FechaCierre = hoy`. Las adendas se conservan como historial.

*Consulta por id* (`ObtenerAsync`): 404 (17).

*Listado* (`ListarAsync(busqueda, estado, modalidad, empleadoId, pagina, tamanoPagina)`):
1. `pagina >= 1` (29).
2. `tamanoPagina` entre 1 y 100 (30); por defecto 10.
3. `estado`, si llega con valor, es uno de los cinco (31). `modalidad`, si llega con valor, es una de las tres (32). Un valor vacío se trata como omitido.

*Efecto en el módulo 61 (E4, solo decisión 2 A).* En `CesarAsync`, tras validar el cese (mensajes 20 a 24 del 61) y antes de guardar, dentro de la misma `TransactionScope`: (a) si el empleado tiene un contrato `Vigente` con `FechaInicio > fechaCese`, 409 (33), sin cambiar nada; (b) los contratos `Vigente` con `FechaFin` nula o `>= fechaCese` pasan a `Cerrado`, `FechaCierre = fechaCese`, `MotivoCierre = "Cese del empleado"`. Un solo guardado (`CompleteAsync`) para empleado y contratos.

**Mensajes literales** (contrato; las pruebas comparan el texto exacto):
| # | Regla | Código | Mensaje |
|---|---|---|---|
| 1 | falta `empleadoId` | 400 | El empleado es obligatorio. |
| 2 | modalidad fuera de las tres (C1) | 400 | La modalidad no es válida. |
| 3 | falta fecha de inicio | 400 | La fecha de inicio es obligatoria. |
| 4 | falta sueldo básico | 400 | El sueldo básico es obligatorio. |
| 5 | sueldo básico menor o igual a 0 (C3) | 400 | El sueldo básico debe ser mayor que cero. |
| 6 | sueldo básico mayor que 9999999999.99 | 400 | El sueldo básico no puede superar 9999999999.99. |
| 7 | sueldo básico con más de 2 decimales | 400 | El sueldo básico admite como máximo 2 decimales. |
| 8 | cargo vacío | 400 | El cargo es obligatorio. |
| 9 | cargo de más de 100 caracteres | 400 | El cargo no puede superar 100 caracteres. |
| 10 | `Indeterminado` con fecha de fin (C1) | 400 | El contrato indeterminado no tiene fecha de fin. |
| 11 | `PlazoFijo` o `Formativo` sin fecha de fin (C1) | 400 | La fecha de fin es obligatoria para esta modalidad. |
| 12 | fecha de fin no posterior a la de inicio (C3) | 400 | La fecha de fin debe ser posterior a la fecha de inicio. |
| 13 | empleado inexistente | 404 | El empleado {empleadoId} no existe. |
| 14 | empleado cesado (decisión 8 A) | 409 | El empleado {empleadoId} está cesado y no admite contratos nuevos. |
| 15 | inicio anterior al ingreso del empleado (decisión 8 A) | 400 | La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado. |
| 16 | el empleado ya tiene un contrato vigente (C2) | 409 | El empleado {empleadoId} ya tiene un contrato vigente. |
| 17 | contrato inexistente | 404 | El contrato laboral {id} no existe. |
| 18 | renovar un contrato no vigente | 409 | El contrato {numero} no está vigente y no se puede renovar. |
| 19 | renovar un contrato indeterminado | 409 | El contrato {numero} es indeterminado y no se renueva. |
| 20 | falta fecha de vigencia de la adenda | 400 | La fecha de vigencia es obligatoria. |
| 21 | adenda sin cargo ni sueldo (C5) | 400 | La adenda debe cambiar el cargo o el sueldo básico. |
| 22 | adenda sobre un contrato no vigente | 409 | El contrato {numero} no está vigente y no admite adendas. |
| 23 | vigencia fuera del periodo del contrato (C5) | 400 | La fecha de vigencia debe estar dentro del periodo del contrato. |
| 24 | adenda con vigencia no posterior a la última | 409 | Ya existe una adenda con fecha de vigencia igual o posterior. |
| 25 | adenda que no cambia cargo ni sueldo vigentes | 400 | La adenda no cambia el cargo ni el sueldo vigentes en esa fecha. |
| 26 | motivo de anulación vacío (decisión 5 A) | 400 | El motivo de anulación es obligatorio. |
| 27 | motivo de anulación de más de 300 caracteres | 400 | El motivo de anulación no puede superar 300 caracteres. |
| 28 | anular un contrato no vigente (decisión 5 A) | 409 | El contrato {numero} no está vigente y no se puede anular. |
| 29 | página menor que 1 | 400 | La página debe ser mayor o igual a 1. |
| 30 | tamaño de página fuera de 1 a 100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 31 | estado fuera de los cinco | 400 | El estado debe ser Vigente, Renovado, Vencido, Cerrado o Anulado. |
| 32 | modalidad fuera de las tres en el listado | 400 | La modalidad debe ser Indeterminado, PlazoFijo o Formativo. |
| 33 | cese con contrato vigente que empieza después (E4, decisión 2 A) | 409 | No se puede cesar al empleado {empleadoId}: el contrato {numero} empieza después de la fecha de cese. |

Los mensajes 13, 29 y 30 son los literales del 61 (13 es su mensaje 28; 29 y 30, sus 25 y 26). Los 400 de cuerpo vacío son los del middleware. Si una decisión elimina una regla, se eliminan su fila y sus pruebas.

**Consultas.**
- Listado: `Repository<ContratoLaboral>().AsQueryable()` unido a `Empleado` en la propia consulta, proyectada a `ContratoLaboralListadoDto`, con `CountAsync` y consulta paginada; sin cargar entidades ni N+1. `busqueda` con `Trim().ToLowerInvariant()` y `ToLower().Contains(termino)` sobre `Numero`, `Cargo`, y `Nombres`, `Apellidos` y `Legajo` del empleado (`// PATRÓN LINQ 1`). Orden: `FechaInicio` descendente, `Id` descendente **[elegido]**. El `Estado` efectivo se calcula en la proyección con el `hoy` pasado como parámetro.
- Detalle: una consulta por `Id` con `SingleOrDefaultAsync` y una segunda para las adendas ordenadas.
- Todas las consultas son asíncronas.

**Seed (`scripts/seed-dev.sql`).** Bloque nuevo después del de `Empleados` y antes de `COMMIT;`, sin caracteres no ASCII. Como el bloque de `Empleados` hace `TRUNCATE ... CASCADE`, vacía también estas tablas; el bloque propio las trunca de nuevo y las carga. Fechas fijas elegidas para que cada estado se mantenga con el tiempo.
```sql
TRUNCATE TABLE "AdendasContrato", "ContratosLaborales" RESTART IDENTITY CASCADE;

INSERT INTO "ContratosLaborales" ("Id", "Numero", "EmpleadoId", "Modalidad", "FechaInicio", "FechaFin", "SueldoBasico", "Cargo", "Estado", "ContratoAnteriorId", "FechaCierre", "MotivoCierre") VALUES
    (1, 'CTR-000001', 1, 'Indeterminado', DATE '2021-03-01', NULL, 8500.00, 'Gerente de Ventas', 'Vigente', NULL, NULL, NULL),
    (2, 'CTR-000002', 2, 'PlazoFijo', DATE '2019-08-16', DATE '2020-08-15', 3200.00, 'Analista de Compras', 'Renovado', NULL, NULL, NULL),
    (3, 'CTR-000003', 2, 'PlazoFijo', DATE '2020-08-16', DATE '2021-08-15', 3400.00, 'Analista de Compras', 'Renovado', 2, NULL, NULL),
    (4, 'CTR-000004', 2, 'Indeterminado', DATE '2021-08-16', NULL, 4200.00, 'Analista de Compras', 'Vigente', 3, NULL, NULL),
    (5, 'CTR-000005', 3, 'Formativo', DATE '2024-01-08', DATE '2024-12-31', 1500.00, 'Almacenero', 'Vigente', NULL, NULL, NULL),
    (6, 'CTR-000006', 4, 'PlazoFijo', DATE '2024-05-02', DATE '2027-05-01', 2600.00, 'Auxiliar de Logistica', 'Cerrado', NULL, DATE '2026-09-30', 'Cese del empleado'),
    (7, 'CTR-000007', 1, 'PlazoFijo', DATE '2026-01-01', DATE '2026-12-31', 8500.00, 'Gerente de Ventas', 'Anulado', NULL, DATE '2026-01-05', 'Registro duplicado por error');

INSERT INTO "AdendasContrato" ("Id", "ContratoLaboralId", "FechaVigencia", "Cargo", "SueldoBasico") VALUES
    (1, 1, DATE '2024-01-01', 'Gerente Comercial', 9500.00);

SELECT setval(pg_get_serial_sequence('"ContratosLaborales"', 'Id'), (SELECT MAX("Id") FROM "ContratosLaborales"));
SELECT setval(pg_get_serial_sequence('"AdendasContrato"', 'Id'), (SELECT MAX("Id") FROM "AdendasContrato"));
```
Cubre: `Vigente` (1, 4), `Renovado` (2, 3, con cadena de renovación), `Vencido` calculado (5: guardado `Vigente`, fin pasado), `Cerrado` (6, empleado 4 cesado) y `Anulado` (7), las tres modalidades y una adenda (contrato 1). Si la decisión 2 es B se retira el contrato 6 o se deja `Vigente`; si la 5 es B se retira el 7. Con la decisión 6 A, el `INSERT` de `Roles` gana `(6, 'RRHH', 'Recursos Humanos', 'Gestión de capital humano', FALSE)`.

**Pruebas** (`tests/TitanGlobal.Api.Tests/Modulo62_ContratosLaborales/ContratosLaboralesApiTests.cs`). Mismo patrón que `EmpleadosApiTests`: `[Collection("Modulo04")]`, fábrica compartida, `SeedDatabase()` y `RestablecerDatos()` que vacía `AdendasContrato`, `ContratosLaborales` y `Empleados`. Cada prueba crea sus propios datos.
- T1: `[Theory]` con un caso por cada mensaje 1 a 12 en el alta: código y texto exactos, sin `Detail`, ninguna fila creada.
- T2: alta feliz por modalidad: `Estado == "Vigente"`, `Numero == "CTR-" + Id.ToString("D6")`, fechas y sueldo ida y vuelta (`yyyy-MM-dd`, 2 decimales). Sueldo `0.001` y 12 dígitos enteros → 400 (7 y 6), no se redondea.
- T3: C2. Segundo alta del mismo empleado con uno vigente → 409 (16), sin filas nuevas. Otro empleado → 200. Tras `Anulado` o `Renovado`, nuevo alta → 200. Contrato vigente con fin pasado: el alta lo deja `Vencido` guardado y crea el nuevo.
- T4: empleado inexistente → 404 (13); cesado → 409 (14); inicio anterior al ingreso → 400 (15).
- T5: renovación. `PlazoFijo` → 200, el nuevo empieza al día siguiente del fin, `ContratoAnteriorId` correcto y el anterior queda `Renovado`. Indeterminado → 409 (19). Ya renovado → 409 (18). Fin menor o igual al inicio derivado → 400 (12). Renovación tardía de un contrato vigente con fin pasado → 200. Atomicidad: con fallo forzado, el anterior sigue `Vigente`.
- T6: adenda. Cambia solo cargo, solo sueldo y ambos; `cargoVigente` y `sueldoVigente` del detalle. Mensajes 20, 21, 23, 24, 25 y 22 con código y texto exactos y sin cambios de filas; el contrato conserva modalidad y fechas (C5).
- T7: anulación (decisión 5 A): mensajes 26, 27, 28 y caso feliz; las adendas quedan.
- T8: listado. Búsqueda por número, cargo, nombre, apellido y legajo; filtros `estado` (incluido `Vencido` calculado y `Vigente` excluyendo vencidos), `modalidad` y `empleadoId`; tamaño por defecto 10; orden; `total`; mensajes 29 a 32.
- T9: detalle: 404 (17), adendas ordenadas y `estado` efectivo `Vencido`.
- T10: sin `PUT` ni `DELETE` (no responden 2xx) y la fila persiste.
- T11: autenticación. Sin token → 401; token sin rol de escritura (por ejemplo `CONSULTA`) en `POST` → 403 y sin filas nuevas; `RRHH` y `ADMIN` → 200; lectura con cualquier token → 200 (decisión 6 A). `TokenPruebas` debe poder emitir un token con rol `RRHH`.
- T12: auditoría: alta, renovación, adenda y anulación generan `RegistroAuditoria` con `Entidad` `ContratoLaboral` o `AdendaContrato`.
- T13 (decisión 2 A), en `EmpleadosApiTests`: cese con contrato vigente → `Cerrado`, `FechaCierre == fechaCese`, `MotivoCierre == "Cese del empleado"`; contrato con fin anterior al cese sin tocar; contrato vigente que empieza después del cese → 409 (33) y el empleado no queda cesado; un cese sin contratos sigue en 200.

**Gate y humo.**
- `./scripts/verify.sh`.
- `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh` y `./scripts/scaffold.sh --comprobar` sin diferencias (incluido el índice parcial; si el scaffold no lo representa, se revisa antes de aprobar la decisión 7).
- `./scripts/smoke-postgres.sh`, obligatorio. Añade variables `CONTRATO_CREADO_ID` y `CONTRATO_RENOVADO_ID`; `openapi-contratos-laborales` (`/api/v1/contratos-laborales`, `/{id}`, `/{id}/renovacion`, `/{id}/adendas`, y `/{id}/anulacion` con la decisión 5 A; sin `put` ni `delete`); y `request_check` (el token del humo es `admin`):
  - `GET /api/v1/contratos-laborales?estado=Vencido` → 200, `total == 1`, `items[0].numero == "CTR-000005"`.
  - `GET /api/v1/contratos-laborales?empleadoId=2` → 200, `total == 3`.
  - `GET /api/v1/contratos-laborales/1` → 200, `cargoVigente == "Gerente Comercial"`, `sueldoVigente == 9500.00`, una adenda.
  - Insertado **antes** de «Cesar empleado creado»: `POST /api/v1/contratos-laborales` para `EMPLEADO_CREADO_ID` (inicio 2026-10-01, fin 2027-09-30) → 200; y tras el cese (decisión 2 A) `GET ...?empleadoId={EMPLEADO_CREADO_ID}` → `estado == "Cerrado"` y `fechaCierre == "2026-10-31"`; `POST` de otro contrato a ese empleado → 409 con el mensaje 14 literal (decisión 8 A).
  - `POST` para el empleado 1 (ya vigente) → 409 con el mensaje 16 literal.
  - `POST` para el empleado 3 (vencido calculado) con inicio 2027-01-01 y fin 2027-12-31 → 200, guarda `CONTRATO_CREADO_ID`; `GET ...?empleadoId=3&estado=Vencido` sigue devolviendo el CTR-000005.
  - `POST .../{CONTRATO_CREADO_ID}/adendas` (vigencia 2027-03-01, sueldo 1800.00) → 200; la misma otra vez → 409 con el mensaje 24 literal.
  - `POST .../{CONTRATO_CREADO_ID}/renovacion` → 200, guarda `CONTRATO_RENOVADO_ID`; repetirla → 409 con el mensaje 18 literal.
  - `POST .../{CONTRATO_RENOVADO_ID}/anulacion` → 200; anularlo otra vez → 409 con el mensaje 28 literal (decisión 5 A).
- Los cuerpos de adenda, renovación y anulación deben ser válidos: la validación de campos va antes de los 409 de estado.
- El humo no borra lo creado (no hay `DELETE`); el siguiente seed lo elimina con `TRUNCATE`. No se ejecutan dos `smoke-postgres.sh` a la vez.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato, reglas y capas | `./scripts/verify.sh` | en verde, 0 warnings |
| A3 | Campos, modalidad y fechas (C1, C3, T4, T5) | T1 y T2: un caso por mensaje 1 a 12 con código y texto exactos; sueldo con más de 2 decimales o desbordado no se guarda ni se redondea | pasan |
| A4 | Un contrato vigente por empleado (C2) | T3 y T4: mensajes 13 a 16; la carrera concurrente responde 409 y no 500 | pasan |
| A5 | Renovación (C4) | T5: inicio = fin + 1 día, anterior `Renovado`, mensajes 18 y 19, atomicidad | pasan |
| A6 | Adendas (C5) | T6: mensajes 20 a 25; el contrato no cambia y las adendas conservan el historial | pasan |
| A7 | Estado `Vencido` (C6) | T3, T8 y T9: contrato `Vigente` con fin pasado se lee `Vencido`, entra en el filtro `Vencido` y no en `Vigente` | pasan |
| A8 | Anulación y cierre (decisiones 5 y 2) | T7 y T13, mensajes 26 a 28 y 33 | pasan |
| A9 | Listado, detalle y sin borrado (T1, T3) | T8, T9 y T10: búsqueda, filtros, paginación, orden, `total`, mensajes 29 a 32 y 17; `PUT`/`DELETE` sin 2xx | pasan |
| A10 | Autenticación y roles | T11: 401 sin token, 403 sin rol de escritura, 200 con `RRHH` y `ADMIN` | pasa |
| A11 | Auditoría | T12 | pasa |
| A12 | Datos y PostgreSQL | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` sin diferencias y `./scripts/smoke-postgres.sh` con los checks de §4 | `SMOKE OK` |
| A13 | OpenAPI | `/openapi/v1.json` incluye las rutas de §4 y no incluye `PUT` ni `DELETE` de `contratos-laborales` | presentes |
| A14 | Alcance | en el diff contra `main`, solo los archivos de §3; sin cambios en `Middlewares/`, `Services/`, `Program.cs`, `Configuration/`, `UnitOfWork`, entidades base ni archivos generados editados a mano | 0 archivos fuera de §3 |

## 6. Checklist de ejecución
- [x] Decisiones 1 a 8 de §2 resueltas por Lukas y spec aprobada (retirar de §4 lo que no aplique).
- [ ] Tarea 1: script `database/008-contratos-laborales.sql`, `./scripts/aplicar-bd.sh` y `./scripts/scaffold.sh`; revisar el `git diff` de lo generado.
- [ ] Tarea 2: DTOs, `ValidacionContratoLaboral`, `AdministrarContratosLaboralesUseCase` y controlador con roles.
- [ ] Tarea 3: efecto E4 en `CesarAsync` del 61 y su prueba (solo con la decisión 2 A).
- [ ] Tarea 4: pruebas T1 a T13.
- [ ] Tarea 5: seed (y rol `RRHH` con la decisión 6 A) y `smoke-postgres.sh`, incluidas las comprobaciones de roles actualizadas.
- [ ] Tarea 6: verificar A1 a A14 y anotar los resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
