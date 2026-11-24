# Especificación: modulo70-seguridad-salud-ocupacional-backend — SeguridadSaludOcupacional (backend)

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22 de implementar los módulos 62 a 70 en secuencia y revisarlos al final. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** no se registran EPP, exámenes médicos ni incidentes de seguridad.
**Resultado esperado:** tablas `EquiposProteccion`, `EntregasEpp`, `ExamenesMedicos`, `IncidentesSst` y `AccionesCorrectivasSst` y `Modulo70_SeguridadSaludOcupacional` en `api/v1/equipos-proteccion`, `api/v1/entregas-epp`, `api/v1/examenes-medicos` e `api/v1/incidentes-sst`: catálogo y entrega de EPP con vencimiento (S1), exámenes médicos (S2), incidentes (S3, S4) y su cierre con acción correctiva (S5).

## 2. Fuera de alcance
- Matriz IPERC, comité SST, inspecciones, estadísticas legales (índices de frecuencia y gravedad) y adjuntos.
- Cambios en otros módulos, `Middlewares/`, `Services/`, `Program.cs` y `UnitOfWork`.

**Decisiones de producto (opción recomendada aplicada):**
1. **EPP (S1).** `FechaVencimiento = FechaEntrega + VidaUtilDias` se guarda al entregar; `Vencida` se calcula al leer. Una entrega nueva del mismo equipo no modifica las anteriores. Solo se entrega a empleados `Activo` (T2) y equipos activos.
2. **Exámenes (S2).** `Restricciones` es obligatorio solo con `AptoConRestricciones`. `FechaVencimiento` es obligatoria en `Ingreso` y `Periodico` y no existe en `Retiro`. El examen de `Retiro` se admite para cesados hasta 30 días después del cese **[elegido]**; los demás, solo para `Activo`. `Vencido` se calcula al leer.
3. **Incidentes (S3, S4).** Toda fecha `<= hoy`. `AccidenteConBaja` exige `DiasBaja >= 1`; los demás tipos exigen `DiasBaja = 0`. Severidad `Leve`, `Moderada`, `Grave` o `Mortal` **[elegido]**. Número `INC-{Id:D6}` (patrón `TMP-` del 62).
4. **Cierre (S5).** Exige al menos una acción correctiva registrada y todas las acciones `Completada`. Un incidente cerrado no admite acciones nuevas ni cambios (T3).
5. **Roles.** Escritura `RRHH` o `ADMIN`; lectura autenticado.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `database/041-seguridad-salud-ocupacional.sql` y su fila en `docs/equipo/relevo.md` §8 | crear / modificar |
| entidades y navegaciones regeneradas, `TitanGlobalDbContext.cs`, `PluralizadorEspanol.cs` si hace falta | regenerar |
| `src/TitanGlobal.Application/DTOs/Modulo70_SeguridadSaludOcupacional/*.cs` | crear |
| `src/TitanGlobal.Application/UseCases/Modulo70_SeguridadSaludOcupacional/AdministrarEquiposProteccionUseCase.cs`, `AdministrarExamenesMedicosUseCase.cs`, `AdministrarIncidentesSstUseCase.cs`, `ResumenSstUseCase.cs`, `ValidacionSst.cs` | crear |
| `src/TitanGlobal.Api/Controllers/Modulo70_SeguridadSaludOcupacional/*Controller.cs` | crear |
| `tests/TitanGlobal.Api.Tests/Modulo70_SeguridadSaludOcupacional/*ApiTests.cs` | crear |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar |

## 4. Diseño y lógica
**Tablas:** `EquiposProteccion` (`Id`, `Codigo varchar(20)` único en mayúsculas, `Nombre varchar(100)`, `VidaUtilDias integer` 1–3650, `Activo`); `EntregasEpp` (`Id`, `EquipoProteccionId` FK, `EmpleadoId` FK, `FechaEntrega`, `Cantidad integer` 1–100, `FechaVencimiento date`); `ExamenesMedicos` (`Id`, `EmpleadoId` FK, `Tipo varchar(20)` `Ingreso`/`Periodico`/`Retiro`, `Fecha`, `Resultado varchar(30)` `Apto`/`AptoConRestricciones`/`NoApto`, `Restricciones varchar(300)`, `FechaVencimiento date`, `Clinica varchar(150)`); `IncidentesSst` (`Id`, `Numero varchar(20)` único, `EmpleadoId` FK, `Tipo varchar(30)`, `Severidad varchar(20)`, `Fecha date`, `Lugar varchar(150)`, `Descripcion varchar(1000)`, `DiasBaja integer`, `Estado` `Abierto`/`Cerrado`, `FechaCierre date`); `AccionesCorrectivasSst` (`Id`, `IncidenteSstId` FK, `Descripcion varchar(500)`, `Responsable varchar(150)`, `FechaCompromiso date`, `Completada boolean`, `FechaCompletada date`).

**Contrato HTTP** (escrituras `RRHH`/`ADMIN`):
| Verbo | Ruta |
|---|---|
| GET, POST, PUT | `api/v1/equipos-proteccion?busqueda=&activo=&pagina=&tamanoPagina=`, `api/v1/equipos-proteccion`, `api/v1/equipos-proteccion/{id}`; `POST .../{id}/desactivacion` y `.../activacion` |
| GET, POST | `api/v1/entregas-epp?empleadoId=&equipoId=&vencidas=&pagina=&tamanoPagina=`, `api/v1/entregas-epp` (`{ equipoId, empleadoId, fechaEntrega, cantidad }`) |
| GET, POST | `api/v1/examenes-medicos?empleadoId=&tipo=&resultado=&vencidos=&pagina=&tamanoPagina=`, `api/v1/examenes-medicos` |
| GET, POST | `api/v1/incidentes-sst?busqueda=&tipo=&severidad=&estado=&desde=&hasta=&pagina=&tamanoPagina=`, `api/v1/incidentes-sst` |
| GET | `api/v1/incidentes-sst/{id}` (con acciones) |
| POST | `.../{id}/acciones` (`{ descripcion, responsable, fechaCompromiso }`), `.../{id}/acciones/{accionId}/completar` (`{ fechaCompletada }`), `.../{id}/cierre` (`[PermitirCuerpoVacio]`) |
| GET | `api/v1/incidentes-sst/resumen?anio=` (`{ IncidentesAbiertos, IncidentesAnio, AccidentesConBaja, DiasBajaAnio, EntregasEppVencidas, ExamenesVencidos, EmpleadosNoAptos }`) |

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El código es obligatorio y no puede superar 20 caracteres. |
| 2 | 400 | El nombre es obligatorio y no puede superar 100 caracteres. |
| 3 | 400 | La vida útil debe estar entre 1 y 3650 días. |
| 4 | 409 | Ya existe un equipo de protección con el código {codigo}. |
| 5 | 404 | El equipo de protección {id} no existe. |
| 6 | 409 | El equipo de protección {codigo} ya está {estado}. |
| 7 | 409 | El equipo de protección {codigo} está inactivo y no se entrega. |
| 8 | 400 | El empleado es obligatorio. |
| 9 | 404 | El empleado {empleadoId} no existe. |
| 10 | 409 | El empleado {empleadoId} está cesado. |
| 11 | 400 | La fecha es obligatoria y no puede ser posterior a hoy. |
| 12 | 400 | La cantidad debe estar entre 1 y 100. |
| 13 | 400 | El tipo de examen debe ser Ingreso, Periodico o Retiro. |
| 14 | 400 | El resultado debe ser Apto, AptoConRestricciones o NoApto. |
| 15 | 400 | Las restricciones son obligatorias si el resultado es AptoConRestricciones y no pueden superar 300 caracteres. |
| 16 | 400 | La fecha de vencimiento es obligatoria y posterior al examen en los exámenes de ingreso y periódicos. |
| 17 | 400 | El examen de retiro no tiene fecha de vencimiento. |
| 18 | 409 | El examen de retiro se registra hasta 30 días después del cese del empleado {empleadoId}. |
| 19 | 400 | La clínica no puede superar 150 caracteres. |
| 20 | 400 | El tipo de incidente debe ser AccidenteSinBaja, AccidenteConBaja, IncidenteSinLesion o EnfermedadOcupacional. |
| 21 | 400 | La severidad debe ser Leve, Moderada, Grave o Mortal. |
| 22 | 400 | Un accidente con baja exige al menos 1 día de baja. |
| 23 | 400 | Solo un accidente con baja tiene días de baja. |
| 24 | 400 | La descripción es obligatoria y no puede superar 1000 caracteres. |
| 25 | 400 | El lugar no puede superar 150 caracteres. |
| 26 | 404 | El incidente {id} no existe. |
| 27 | 409 | El incidente {numero} está cerrado y no admite cambios. |
| 28 | 400 | La acción correctiva necesita descripción (máximo 500), responsable (máximo 150) y fecha de compromiso. |
| 29 | 404 | La acción correctiva {id} no existe en el incidente. |
| 30 | 409 | La acción correctiva {id} ya está completada. |
| 31 | 409 | El incidente {numero} necesita al menos una acción correctiva para cerrarse. |
| 32 | 409 | El incidente {numero} tiene {pendientes} acciones correctivas sin completar. |
| 33 | 400 | La página debe ser mayor o igual a 1. |
| 34 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 35 | 400 | El filtro {nombre} no es válido. |

**Seed:** 4 equipos (uno inactivo); entregas a los empleados 1 a 3 (una vencida); exámenes (uno vencido y uno `AptoConRestricciones`, y el de retiro del empleado 4); incidentes 2026: uno cerrado con acción completada y uno `AccidenteConBaja` abierto con una acción pendiente.

**Pruebas:** un caso por mensaje; vencimiento de EPP y exámenes calculado al leer; cese y examen de retiro dentro y fuera de los 30 días; cierre con acciones pendientes, sin acciones y completo; inmutabilidad del cerrado; resumen; roles; auditoría.

**Humo:** OpenAPI; resumen 2026; entrega a un cesado → 409 con el mensaje 10; cerrar el incidente abierto con la acción pendiente → 409 con el mensaje 32; completar la acción y cerrar → 200.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde (salvo el fallo CRLF conocido) |
| A2 | S1–S5 | pruebas | pasan |
| A3 | Roles y auditoría | pruebas | pasan |
| A4 | PostgreSQL | `aplicar-bd.sh`, `scaffold.sh --comprobar`, `smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 041, scaffold y fila en `relevo.md`.
- [x] Tarea 2: DTOs, validación, casos de uso y controladores.
- [x] Tarea 3: pruebas, seed y humo; A1 a A4 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Gate | Verde salvo el fallo CRLF conocido | `./scripts/verify.sh`: compilación sin warnings, formato y `check-rules.sh` correctos; `dotnet test`: 1477 superadas y 1 fallo conocido (`CatalogoCoincideConLosOchentaModulosDeArquitectura`); `npm --prefix frontend run verify` correcto |
| 2026-11-22 | A2 S1–S5, A3 roles y auditoría | Pasan | 35 pruebas nuevas en `tests/TitanGlobal.Api.Tests/Modulo70_SeguridadSaludOcupacional/` |
| 2026-11-22 | A4 PostgreSQL | `SMOKE OK` | `./scripts/aplicar-bd.sh` (script 041), `./scripts/scaffold.sh --comprobar` («SCAFFOLD AL DÍA») y `./scripts/smoke-postgres.sh` |
| 2026-11-23 | A1 a A4 | cumple | Actualizado con main (módulos 12, 41, 42 y 72) y script renumerado a 041: gate y smoke en verde (`dotnet build -warnaserror` 0 advertencias; `dotnet test` 1530 superadas y 1 fallo, el CRLF conocido; `check-rules.sh` sin errores; frontend 1277 pruebas en verde; `./scripts/smoke-postgres.sh` → `SMOKE OK`) |

**Opciones tomadas donde la spec no precisaba (2026-11-22):**
- `equipoId` ausente en una entrega se trata como 0 y devuelve el mensaje 5 con el id 0; el orden de validación de la entrega es 8, 11, 12, 5, 7, 9, 10.
- Los incidentes se registran para cualquier empleado existente (también cesado); solo EPP y exámenes distintos de retiro exigen empleado `Activo`. El examen de retiro se admite también para un empleado `Activo`.
- «Vencida/Vencido» significa `FechaVencimiento < hoy (UTC)`: lo que vence hoy sigue vigente. El resumen cuenta todas las entregas y exámenes vencidos (una entrega nueva no anula la anterior).
- `EmpleadosNoAptos` cuenta empleados `Activo` cuyo examen más reciente (por fecha e id) es `NoApto`.
- `IncidentesAbiertos` no depende del año; `IncidentesAnio`, `AccidentesConBaja` y `DiasBajaAnio` filtran por el año de la fecha del incidente. La respuesta del resumen añade `anio`.
- `desde` posterior a `hasta` en el listado de incidentes devuelve el mensaje 35 con `hasta`.
- Completar una acción exige `fechaCompletada` con el mismo criterio del mensaje 11 (obligatoria y no posterior a hoy); se valida antes de buscar el incidente. En `.../acciones` el cuerpo se valida (mensaje 28) antes del incidente (26) y del estado cerrado (27).
- Los exámenes de ingreso y periódicos con `Restricciones` informadas con otro resultado las guardan tal cual; solo se limitan a 300 caracteres.
- Las entregas de EPP se gestionan desde `AdministrarEquiposProteccionUseCase` (sin caso de uso aparte).
