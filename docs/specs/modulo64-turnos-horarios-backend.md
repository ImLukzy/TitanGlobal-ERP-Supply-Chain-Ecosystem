# Especificación: modulo64-turnos-horarios-backend — TurnosHorarios (backend)

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22 de implementar los módulos 62 a 70 en secuencia y revisarlos al final. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** no existen turnos ni horarios. El módulo 63 (asistencia) necesita saber, para cada empleado y fecha, si le toca trabajar, a qué hora entra y sale y con cuánta tolerancia (A4, A5, A6 de `docs/hcm-reglas-negocio.md`).
**Resultado esperado:** tablas `Turnos` y `AsignacionesTurno` y `Modulo64_TurnosHorarios` en `api/v1/turnos` y `api/v1/asignaciones-turno`:
- maestro de turnos `Fijo` (días de la semana) y `Rotativo` (ciclo de N días de trabajo y M de descanso), con tolerancia y turnos que cruzan medianoche (H1, H2, H3);
- asignación de turnos a empleados con un solo turno por fecha (H4);
- programación calculada día a día de un empleado, reutilizable por el 63 (`CalculadoraTurnos`);
- resumen para la portada.

Reglas aplicadas: H1 a H5, T2, T5 y T6. Lo que las reglas no fijan se marca **[elegido]**.

## 2. Fuera de alcance
- Marcaciones, tardanzas y horas extra (63): solo consumirán `CalculadoraTurnos`.
- Turnos partidos (dos tramos en un día), refrigerio y feriados.
- Modificar el módulo 61 (el cese no cierra asignaciones; ver decisión 4) o el 62.
- Cambios en `Middlewares/`, `Services/`, `Program.cs`, `UnitOfWork`, entidades base y archivos generados editados a mano.

**Decisiones de producto (opción recomendada aplicada):**
1. **H5, cambios de un turno.** Un turno con asignaciones no se edita (409, mensaje 22): se crea otro y se reasigna. Así un cambio nunca altera lo ya programado o calculado (T3). Activar y desactivar sí se permite siempre; un turno inactivo no admite asignaciones nuevas.
2. **Días laborables del turno fijo.** Lista de números ISO 1 (lunes) a 7 (domingo) en el DTO; se guarda como texto ordenado `"1,2,3,4,5"` en `character varying(13)`.
3. **H4, un turno por fecha.** Comprobación en `Application` dentro de una transacción, más índice único (`EmpleadoId`, `FechaInicio`) como guarda mínima. Una asignación nueva desde `D` cierra automáticamente en `D − 1` la asignación abierta (sin fecha de fin) que empezó antes; cualquier otro cruce responde 409 (33). Una exclusión por rangos en PostgreSQL (`23P01`) acabaría en 500 porque `UnitOfWork` no la traduce.
4. **T2 y el cese.** No se asignan turnos a empleados cesados (409, 30). Las asignaciones existentes no se modifican al cesar: la programación devuelve los días posteriores a `FechaCese` como no laborables con observación `Cesado`.
5. **Corrección de asignaciones.** Sin `PUT`. Se puede fijar o adelantar la fecha de fin (`/finalizacion`) y eliminar una asignación que todavía no empezó (`FechaInicio > hoy`). Lo ya transcurrido no se borra (T3).
6. **Roles.** Escritura con `[Authorize(Roles = "RRHH,ADMIN")]` (el rol `RRHH` ya existe en el seed por el 62); lectura para cualquier usuario autenticado.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/024-turnos-horarios.sql` | crear | `Turnos` y `AsignacionesTurno` (§4); número 024 añadido a `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo la fila 024 de la tabla de §8 |
| `src/TitanGlobal.Domain/Models/Turno.cs`, `AsignacionTurno.cs`, `Empleado.cs` | regenerar | `./scripts/scaffold.sh`; `Empleado` solo gana la navegación |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold no singulariza bien `AsignacionesTurno` → `AsignacionTurno` |
| `src/TitanGlobal.Application/DTOs/Modulo64_TurnosHorarios/*.cs` | crear | un archivo por clase |
| `src/TitanGlobal.Application/UseCases/Modulo64_TurnosHorarios/AdministrarTurnosUseCase.cs` | crear | turnos y resumen |
| `src/TitanGlobal.Application/UseCases/Modulo64_TurnosHorarios/AdministrarAsignacionesTurnoUseCase.cs` | crear | asignaciones y programación |
| `src/TitanGlobal.Application/UseCases/Modulo64_TurnosHorarios/ValidacionTurno.cs` | crear | helper estático interno |
| `src/TitanGlobal.Application/UseCases/Modulo64_TurnosHorarios/CalculadoraTurnos.cs` | crear | clase estática **pública** con el cálculo de un día (la usará el 63) |
| `src/TitanGlobal.Api/Controllers/Modulo64_TurnosHorarios/TurnosController.cs`, `AsignacionesTurnoController.cs` | crear | controladores delgados |
| `tests/TitanGlobal.Api.Tests/Modulo64_TurnosHorarios/TurnosApiTests.cs`, `AsignacionesTurnoApiTests.cs` | crear | pruebas de §4 |
| `scripts/seed-dev.sql` | modificar | bloque nuevo después del de contratos laborales, antes de `COMMIT;` |
| `scripts/smoke-postgres.sh` | modificar | comprobaciones nuevas antes de `SMOKE OK` |

## 4. Diseño y lógica
**Convenciones.** Las de `AGENTS.md` §3 y `CONTRIBUTING.md`: sin `var`, sin `=>` en miembros, `UseCase` sin interfaz, `ReglaNegocioException` con `CodigosEstado`, `ViolacionRestriccionException` para `23505`/`23503`. «Hoy» es `DateOnly.FromDateTime(DateTime.UtcNow)` **[elegido]**, como el 62. Horas como `TimeOnly` (`time without time zone`), serializadas `HH:mm:ss`.

**Tabla `Turnos`** (entidad `Turno`):
| Columna | Tipo | Restricción |
|---|---|---|
| `Id` | `integer` identidad | PK |
| `Codigo` | `character varying(20)` | `NOT NULL`, único `IX_Turnos_Codigo`; se guarda en mayúsculas tras `Trim()` |
| `Nombre` | `character varying(100)` | `NOT NULL` |
| `Tipo` | `character varying(20)` | `NOT NULL`; `Fijo` o `Rotativo` |
| `HoraEntrada`, `HoraSalida` | `time` | `NOT NULL` |
| `CruzaMedianoche` | `boolean` | `NOT NULL`; verdadero si y solo si `HoraSalida < HoraEntrada` (H3) |
| `ToleranciaMinutos` | `integer` | `NOT NULL`, `CHECK` 0 a 30 |
| `DiasLaborables` | `character varying(13)` | solo `Fijo`, p. ej. `1,2,3,4,5` |
| `DiasTrabajo`, `DiasDescanso` | `integer` | solo `Rotativo`, 1 a 30 |
| `FechaInicioCiclo` | `date` | solo `Rotativo` |
| `Activo` | `boolean` | `NOT NULL`, verdadero al crear (`ValoresIniciales.cs` o el `UseCase`) |

**Tabla `AsignacionesTurno`** (entidad `AsignacionTurno`): `Id`; `EmpleadoId` (FK `Empleados`, `ON DELETE RESTRICT`, índice); `TurnoId` (FK `Turnos`, `ON DELETE RESTRICT`, índice); `FechaInicio date NOT NULL`; `FechaFin date` nula (`CHECK "FechaFin" IS NULL OR "FechaFin" >= "FechaInicio"`); índice único `IX_AsignacionesTurno_EmpleadoId_FechaInicio`.

**Contrato HTTP.** JSON camelCase, fechas `yyyy-MM-dd`, horas `HH:mm:ss`, errores `{ StatusCode, Message }`.
| Verbo | Ruta | Respuesta OK | Acceso |
|---|---|---|---|
| GET | `api/v1/turnos?busqueda=&tipo=&activo=&pagina=1&tamanoPagina=10` | `PaginaTurnosDto` | autenticado |
| GET | `api/v1/turnos/resumen` | `ResumenTurnosDto` | autenticado |
| GET | `api/v1/turnos/{id:int}` | `TurnoDetalleDto` | autenticado |
| POST | `api/v1/turnos` | `{ id }` | `RRHH`, `ADMIN` |
| PUT | `api/v1/turnos/{id:int}` | `{ id }` | `RRHH`, `ADMIN` |
| POST | `api/v1/turnos/{id:int}/desactivacion` y `/activacion` | `{ id }` | `RRHH`, `ADMIN`; sin cuerpo, `[PermitirCuerpoVacio]` |
| GET | `api/v1/asignaciones-turno?empleadoId=&turnoId=&estado=&pagina=1&tamanoPagina=10` | `PaginaAsignacionesTurnoDto` | autenticado |
| GET | `api/v1/asignaciones-turno/programacion?empleadoId=&desde=&hasta=` | `List<DiaProgramadoDto>` | autenticado |
| POST | `api/v1/asignaciones-turno` | `{ id }` | `RRHH`, `ADMIN` |
| POST | `api/v1/asignaciones-turno/{id:int}/finalizacion` | `{ id }` | `RRHH`, `ADMIN` |
| DELETE | `api/v1/asignaciones-turno/{id:int}` | 204 | `RRHH`, `ADMIN` |

**DTOs.**
- `GuardarTurnoRequestDto { Codigo, Nombre, Tipo, HoraEntrada (TimeOnly?), HoraSalida (TimeOnly?), CruzaMedianoche (bool), ToleranciaMinutos (int?), DiasLaborables (List<int>?), DiasTrabajo (int?), DiasDescanso (int?), FechaInicioCiclo (DateOnly?) }`.
- `TurnoListadoDto { Id, Codigo, Nombre, Tipo, HoraEntrada, HoraSalida, CruzaMedianoche, ToleranciaMinutos, DiasLaborables (List<int>), DiasTrabajo, DiasDescanso, FechaInicioCiclo, Activo }`; `TurnoDetalleDto : TurnoListadoDto` con `TotalAsignaciones` y `EmpleadosConTurnoHoy`.
- `ResumenTurnosDto { TotalTurnos, Activos, Fijos, Rotativos, EmpleadosConTurnoHoy, EmpleadosActivosSinTurnoHoy }`.
- `GuardarAsignacionTurnoRequestDto { EmpleadoId (int?), TurnoId (int?), FechaInicio (DateOnly?), FechaFin (DateOnly?) }`; `FinalizarAsignacionTurnoRequestDto { FechaFin (DateOnly?) }`.
- `AsignacionTurnoListadoDto { Id, EmpleadoId, EmpleadoLegajo, EmpleadoNombre, TurnoId, TurnoCodigo, TurnoNombre, FechaInicio, FechaFin, Estado }`. `Estado` calculado: `Futura` (`FechaInicio > hoy`), `Finalizada` (`FechaFin < hoy`) o `Vigente`.
- `DiaProgramadoDto { Fecha, TurnoId, TurnoCodigo, Laborable, HoraEntrada, HoraSalida, CruzaMedianoche, ToleranciaMinutos, Observacion }`. `Observacion`: `null` si es laborable; si no, `SinTurno`, `Descanso` o `Cesado`.
- Paginados `{ Items, Total, Pagina, TamanoPagina }`; `{ id }` con un DTO propio.

**`CalculadoraTurnos.CalcularDia(Turno? turno, DateOnly fecha, DateOnly? fechaCese)`** → `DiaProgramadoDto`. Sin turno: `SinTurno`. `fechaCese` no nula y `fecha > fechaCese`: `Cesado`. `Fijo`: laborable si el día ISO de `fecha` está en `DiasLaborables`. `Rotativo`: `d = fecha.DayNumber − FechaInicioCiclo.DayNumber`; si `d < 0`, `Descanso`; si no, laborable cuando `d % (DiasTrabajo + DiasDescanso) < DiasTrabajo`. No laborable con turno: `Descanso`.

**Reglas del turno** (alta y edición; gana el primer fallo): 1 a 19 en el orden de la tabla. Edición: 21, después 22 si `TotalAsignaciones > 0`. Unicidad del código: comprobación previa y `ViolacionRestriccionException` (`Unicidad`) → 409 (20). Desactivar/activar: 21; 23 si ya está inactivo; 24 si ya está activo.

**Reglas de la asignación.** Alta: 25, 26, 27, 28; empleado inexistente 404 (29); cesado 409 (30); inicio anterior al ingreso 400 (31); turno inexistente 404 (21); turno inactivo 409 (32); en transacción: se cierra en `D − 1` la abierta que empezó antes de `D`; si queda otra que se cruce con [`FechaInicio`, `FechaFin` o infinito], 409 (33); `23505` del índice → 409 (33); `23503` del empleado → 404 (29). Finalización: 34; fecha 36; 28 si es anterior al inicio; 37 si la asignación ya tiene fin igual o anterior. Eliminación: 34; 35 si `FechaInicio <= hoy`. Listado: 38, 39; `estado` fuera de `Vigente`, `Futura`, `Finalizada` → 40. Turnos: `tipo` inválido → 41. Programación: `empleadoId`, `desde`, `hasta` obligatorios (42); `hasta < desde` (43); más de 31 días (44); empleado inexistente (29).

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El código es obligatorio. |
| 2 | 400 | El código no puede superar 20 caracteres. |
| 3 | 400 | El nombre es obligatorio. |
| 4 | 400 | El nombre no puede superar 100 caracteres. |
| 5 | 400 | El tipo de turno debe ser Fijo o Rotativo. |
| 6 | 400 | La hora de entrada es obligatoria. |
| 7 | 400 | La hora de salida es obligatoria. |
| 8 | 400 | La hora de salida no puede ser igual a la hora de entrada. |
| 9 | 400 | Si la hora de salida es anterior a la de entrada, el turno debe cruzar medianoche. |
| 10 | 400 | El turno solo cruza medianoche si la hora de salida es anterior a la de entrada. |
| 11 | 400 | La tolerancia es obligatoria. |
| 12 | 400 | La tolerancia debe estar entre 0 y 30 minutos. |
| 13 | 400 | El turno fijo debe tener al menos un día laborable. |
| 14 | 400 | Los días laborables deben ser números del 1 (lunes) al 7 (domingo) sin repetir. |
| 15 | 400 | El turno rotativo no usa días laborables de la semana. |
| 16 | 400 | El turno rotativo necesita días de trabajo y días de descanso. |
| 17 | 400 | Los días de trabajo y de descanso deben estar entre 1 y 30. |
| 18 | 400 | La fecha de inicio del ciclo es obligatoria en el turno rotativo. |
| 19 | 400 | El turno fijo no usa ciclo de trabajo y descanso. |
| 20 | 409 | Ya existe un turno con el código {codigo}. |
| 21 | 404 | El turno {id} no existe. |
| 22 | 409 | El turno {codigo} tiene asignaciones y no se puede modificar. Crea un turno nuevo. |
| 23 | 409 | El turno {codigo} ya está inactivo. |
| 24 | 409 | El turno {codigo} ya está activo. |
| 25 | 400 | El empleado es obligatorio. |
| 26 | 400 | El turno es obligatorio. |
| 27 | 400 | La fecha de inicio es obligatoria. |
| 28 | 400 | La fecha de fin no puede ser anterior a la fecha de inicio. |
| 29 | 404 | El empleado {empleadoId} no existe. |
| 30 | 409 | El empleado {empleadoId} está cesado y no admite turnos. |
| 31 | 400 | La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado. |
| 32 | 409 | El turno {codigo} está inactivo y no admite asignaciones. |
| 33 | 409 | El empleado {empleadoId} ya tiene un turno asignado que se cruza con esas fechas. |
| 34 | 404 | La asignación de turno {id} no existe. |
| 35 | 409 | Solo se puede eliminar una asignación que todavía no empezó. |
| 36 | 400 | La fecha de fin es obligatoria. |
| 37 | 409 | La asignación ya termina en esa fecha o antes. |
| 38 | 400 | La página debe ser mayor o igual a 1. |
| 39 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 40 | 400 | El estado debe ser Vigente, Futura o Finalizada. |
| 41 | 400 | El tipo debe ser Fijo o Rotativo. |
| 42 | 400 | El empleado y el rango de fechas son obligatorios. |
| 43 | 400 | La fecha hasta no puede ser anterior a la fecha desde. |
| 44 | 400 | El rango de la programación no puede superar 31 días. |

**Consultas.** Listados proyectados a DTO con `CountAsync` y paginación; búsqueda de turnos por código y nombre; asignaciones unidas a `Empleado` y `Turno` en la misma consulta. Orden: turnos por `Codigo`; asignaciones por `FechaInicio` descendente e `Id` descendente **[elegido]**. La programación carga las asignaciones del empleado que tocan el rango con sus turnos en una consulta y calcula en memoria.

**Seed** (después del bloque de contratos laborales, sin caracteres no ASCII, con `TRUNCATE ... RESTART IDENTITY CASCADE` y `setval`):
- Turnos: 1 `DIA` «Turno dia» Fijo 08:00–17:00, tolerancia 10, días `1,2,3,4,5`; 2 `NOCHE` «Turno noche» Fijo 22:00–06:00, cruza medianoche, tolerancia 5, días `1,2,3,4,5,6`; 3 `ROT4X2` «Rotativo 4x2» Rotativo 07:00–19:00, tolerancia 15, 4 de trabajo y 2 de descanso desde 2026-01-05; 4 `SABADO` «Sabado medio dia» Fijo 08:00–13:00, días `6`, inactivo.
- Asignaciones: empleado 1 `DIA` desde 2021-03-01; empleado 2 `NOCHE` 2019-08-16 a 2025-12-31 y `ROT4X2` desde 2026-01-01; empleado 3 `DIA` desde 2024-01-08; empleado 4 `DIA` desde 2018-05-02 (cesado el 2026-09-30).

**Pruebas** (mismo patrón que `ContratosLaboralesApiTests`, `[Collection("Modulo04")]`, cada prueba crea sus datos):
- Turnos: un caso por mensaje 1 a 19 con código y texto exactos y sin filas nuevas; alta feliz fijo, nocturno y rotativo (código en mayúsculas, días ordenados); 20 (también con distinta capitalización); edición feliz sin asignaciones y 22 con asignaciones; 21; activar/desactivar con 23 y 24; listado con filtros, `tipo` inválido (41), 38 y 39; resumen.
- Asignaciones: 25 a 33 en el alta; cierre automático de la abierta en `D − 1`; cruce con una cerrada → 33; finalización (34, 36, 28, 37 y caso feliz); eliminación de futura (204) y de una ya empezada (35); listado con `estado` calculado y 40.
- Programación: fijo (fin de semana `Descanso`), nocturno (`cruzaMedianoche`), rotativo (ciclo 4x2 exacto en 12 días y días antes del inicio de ciclo), cambio de turno a mitad de rango, `SinTurno`, `Cesado` tras la fecha de cese, 42, 43 y 44.
- Roles: 401 sin token; 403 con un rol sin escritura en `POST`, `PUT` y `DELETE`, sin cambios; `RRHH` y `ADMIN` → 2xx.
- Auditoría: alta de turno y de asignación generan `RegistroAuditoria`.

**Humo** (`smoke-postgres.sh`, token `admin`): OpenAPI con las rutas; `GET /api/v1/turnos?tipo=Rotativo` → `total == 1`; `GET /api/v1/turnos/resumen` → `totalTurnos == 4`, `activos == 3`; `GET .../programacion?empleadoId=2&desde=2026-01-05&hasta=2026-01-10` → 6 días, los 4 primeros laborables y los 2 últimos `Descanso`; `POST /api/v1/turnos` con `DIA` → 409 con el mensaje 20; alta de un turno y de una asignación futura para el empleado 3 → 200 y su eliminación → 204; asignación al empleado 4 → 409 con el mensaje 30.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` | en verde |
| A3 | Turnos (H1–H3, H5) | pruebas de turnos: mensajes 1 a 24 | pasan |
| A4 | Asignaciones (H4, T2) | pruebas de asignaciones: mensajes 25 a 40, cierre automático | pasan |
| A5 | Programación | pruebas de programación y `CalculadoraTurnos` | pasan |
| A6 | Roles y auditoría | pruebas de roles y auditoría | pasan |
| A7 | PostgreSQL | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` sin diferencias, `./scripts/smoke-postgres.sh` | `SMOKE OK` |
| A8 | Alcance | diff contra la rama base: solo archivos de §3 | 0 fuera de §3 |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 024, `aplicar-bd.sh`, `scaffold.sh` y fila en `relevo.md` §8.
- [x] Tarea 2: DTOs, validación, `CalculadoraTurnos`, casos de uso y controladores.
- [x] Tarea 3: pruebas.
- [x] Tarea 4: seed y humo.
- [x] Tarea 5: A1 a A8 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | OK | `dotnet build -warnaserror`: 0 errores, 0 advertencias |
| 2026-11-22 | A2 | OK con excepción conocida | `./scripts/verify.sh`: restore, build, `dotnet format --verify-no-changes` y `check-rules.sh` en verde; `dotnet test --no-build`: 1196 superadas, 1 fallida (`Modulo72 CatalogoCoincideConLosOchentaModulosDeArquitectura`, CRLF de `docs/arquitectura.md` en Windows, ajena al módulo); `npm --prefix frontend run verify` en verde |
| 2026-11-22 | A3 | OK | pruebas de turnos (mensajes 1 a 24, alta y edición): `dotnet test --filter Modulo64`, 0 fallos |
| 2026-11-22 | A4 | OK | pruebas de asignaciones (mensajes 25 a 40 y cierre automático): 0 fallos |
| 2026-11-22 | A5 | OK | pruebas de programación y de `CalculadoraTurnos`: 0 fallos |
| 2026-11-22 | A6 | OK | pruebas de roles (401, 403, RRHH, ADMIN) y de auditoría: 0 fallos |
| 2026-11-22 | A7 | OK | `./scripts/aplicar-bd.sh` aplica 024; `./scripts/scaffold.sh --comprobar`: SCAFFOLD AL DÍA; `./scripts/smoke-postgres.sh`: `SMOKE OK` |
| 2026-11-22 | A8 | OK | `git diff` y `git status`: solo archivos de §3 (más `Empleado.cs` y `ValoresIniciales.cs`, ver nota) |

Notas de la ejecución:
- `ValoresIniciales.cs` (Domain) gana la clase parcial `Turno` con `Activo = true`, como indica el criterio de la tabla `Turnos`.
- `PluralizadorEspanol.cs` necesitó la entrada `AsignacionesTurno` → `AsignacionTurno`.
- El humo asigna un turno futuro al empleado 3, lo que cierra su asignación abierta en 2029-12-31 (cierre automático de la decisión 3); el humo recrea la base desde cero, así que no deja rastro.
