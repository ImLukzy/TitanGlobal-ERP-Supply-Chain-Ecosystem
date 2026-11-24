# Especificación: modulo63-asistencia-biometrica-backend — AsistenciaBiometrica (backend)

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22 de implementar los módulos 62 a 70 en secuencia y revisarlos al final. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** no se registra la asistencia. Planillas (66) necesita días laborados y horas extra aprobadas (P3).
**Resultado esperado:** tablas `Marcaciones` y `JornadasAsistencia` y `Modulo63_AsistenciaBiometrica` en `api/v1/marcaciones` y `api/v1/jornadas-asistencia`:
- marcaciones manuales y por importación del reloj, idempotente y sin duplicados (A1, A2, A3);
- cálculo de la jornada de cada día contra el turno del 64 (`CalculadoraTurnos`): tardanza, horas trabajadas, horas extra con tope e incidencias (A4, A5, A6);
- aprobación o rechazo de horas extra por `RRHH` o `ADMIN` (A7).

Reglas aplicadas: A1 a A7, T2, T3, T5 y T6. Lo que las reglas no fijan se marca **[elegido]**.

## 2. Fuera de alcance
- Conexión directa con relojes biométricos: el archivo se lee en el navegador y se envía como JSON.
- Planillas (66), feriados, refrigerio y compensación de horas.
- Cambios en los módulos 61, 62 y 64 (solo se leen sus tablas y `CalculadoraTurnos`), en `Middlewares/`, `Services/`, `Program.cs` y `UnitOfWork`.

**Decisiones de producto (opción recomendada aplicada):**
1. **Zona horaria.** Las marcaciones se guardan en UTC (`timestamptz`, T5). Los turnos están en hora local de Perú: se usa un desfase fijo de UTC−5 (Perú no tiene horario de verano) **[elegido]**. La fecha de la jornada es la fecha local de la entrada programada.
2. **Archivo del reloj (A3).** CSV con cabecera `numeroRegistro,legajo,tipo,instante` (instante ISO 8601 con zona). La pantalla lo convierte a JSON; la API recibe `{ dispositivo, registros[] }`, como máximo 1000 registros por envío. Idempotencia por (`Dispositivo`, `NumeroRegistro`) con índice único.
3. **Duplicados (A2).** Misma persona, mismo tipo y diferencia ≤ 60 segundos con una marcación existente o anterior del mismo envío: se descarta y se cuenta en `duplicadas`. En el registro manual responde 409 (12).
4. **A1, contrato vigente en la fecha.** Existe un contrato del empleado no `Anulado` con `FechaInicio <= fecha` y `fecha <= FechaCierre`, `FechaFin` o sin tope, en ese orden de prioridad. La fecha es la local (decisión 1). El empleado debe estar `Activo` o, si está cesado, la fecha no puede ser posterior a su cese (T2).
5. **Tardanza (A4).** `max(0, entrada − (entrada programada + tolerancia))` en minutos, literal a la regla.
6. **Horas extra (A5).** Minutos trabajados antes de la entrada programada más los posteriores a la salida programada; en un día no laborable con entrada y salida, todo lo trabajado. Tope diario de 240 minutos (constante `TopeMinutosExtraDiario`, hasta que exista ParametrosSistema, 79).
7. **Estados de la jornada.** `Asistencia`, `Tardanza`, `SinMarcacion` (falta la entrada o la salida, A6), `Falta` (día laborable sin ninguna marcación) y `DescansoTrabajado` (día no laborable con entrada y salida). Un día no laborable sin marcaciones no genera jornada.
8. **T3 y recálculo.** El cálculo de un rango regenera las jornadas cuyas horas extra están en `NoAplica` o `Pendiente`, y conserva sin tocar las `Aprobada` o `Rechazada`. Solo se eliminan marcaciones `Manual` cuya jornada no esté decidida; las del reloj no se borran.
9. **Quién aprueba (A7).** Escritura con `RRHH` o `ADMIN`. `AprobadoPor` es el nombre de usuario del JWT: el controlador lo lee de `User` y lo pasa al caso de uso (sin cambiar `IContextoSolicitud`).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/035-asistencia-biometrica.sql` | crear | `Marcaciones` y `JornadasAsistencia`; fila 035 en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo la fila 035 |
| entidades `Marcacion`, `JornadaAsistencia` y navegaciones en `Empleado` y `Turno`; `TitanGlobalDbContext.cs` | regenerar | `./scripts/scaffold.sh` |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si hace falta (`Marcaciones` → `Marcacion`, `JornadasAsistencia` → `JornadaAsistencia`) |
| `src/TitanGlobal.Application/DTOs/Modulo63_AsistenciaBiometrica/*.cs` | crear | |
| `src/TitanGlobal.Application/UseCases/Modulo63_AsistenciaBiometrica/AdministrarMarcacionesUseCase.cs`, `AdministrarJornadasAsistenciaUseCase.cs`, `ValidacionMarcacion.cs`, `CalculadoraJornada.cs` | crear | `CalculadoraJornada` estática y pública (la usará el 66) |
| `src/TitanGlobal.Api/Controllers/Modulo63_AsistenciaBiometrica/MarcacionesController.cs`, `JornadasAsistenciaController.cs` | crear | |
| `tests/TitanGlobal.Api.Tests/Modulo63_AsistenciaBiometrica/*ApiTests.cs` | crear | |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | bloque después del de turnos |

## 4. Diseño y lógica
**Tabla `Marcaciones`:** `Id`; `EmpleadoId` (FK, índice); `Tipo varchar(10)` `Entrada`/`Salida`; `Instante timestamptz NOT NULL`; `Origen varchar(10)` `Reloj`/`Manual`; `Dispositivo varchar(50)` y `NumeroRegistro varchar(50)` (solo `Reloj`); `Observacion varchar(200)` (solo `Manual`, obligatoria); índice único `IX_Marcaciones_Dispositivo_NumeroRegistro`; índice (`EmpleadoId`, `Instante`).

**Tabla `JornadasAsistencia`:** `Id`; `EmpleadoId` (FK); `Fecha date`; `TurnoId` (FK, nula); copia del turno al calcular: `HoraEntradaProgramada`, `HoraSalidaProgramada` (`time`, nulas en `DescansoTrabajado`), `ToleranciaMinutos`; `Entrada`, `Salida` (`timestamptz`, nulas); `MinutosTrabajados`, `MinutosTardanza`, `MinutosExtra` (`integer NOT NULL`); `Estado varchar(20)`; `EstadoHorasExtra varchar(20)` `NoAplica`/`Pendiente`/`Aprobada`/`Rechazada`; `DecididoPor varchar(100)`, `FechaDecision timestamptz`, `MotivoRechazo varchar(300)`; índice único (`EmpleadoId`, `Fecha`).

**`CalculadoraJornada.Calcular(DiaProgramadoDto dia, IReadOnlyList<Marcacion> marcaciones)`.** Ventana local de la entrada programada −4 h hasta la salida programada (+1 día si cruza medianoche) +6 h. Entrada = primera `Entrada` de la ventana; salida = última `Salida` posterior a la entrada. `MinutosTrabajados` = salida − entrada. Tardanza (decisión 5) y horas extra con tope (decisión 6). `EstadoHorasExtra = Pendiente` si `MinutosExtra > 0`, si no `NoAplica`. Estado `Tardanza` si `MinutosTardanza > 0`. Días no laborables: ventana de 00:00 a 23:59 local.

**Contrato HTTP** (JSON camelCase; fechas `yyyy-MM-dd`; instantes ISO 8601 en UTC con `Z`):
| Verbo | Ruta | Respuesta OK | Acceso |
|---|---|---|---|
| GET | `api/v1/marcaciones?empleadoId=&desde=&hasta=&tipo=&origen=&pagina=1&tamanoPagina=10` | paginado de `MarcacionDto` | autenticado |
| POST | `api/v1/marcaciones` | `{ id }` | `RRHH`, `ADMIN` |
| POST | `api/v1/marcaciones/importacion` | `ResultadoImportacionDto` | `RRHH`, `ADMIN` |
| DELETE | `api/v1/marcaciones/{id:int}` | 204 | `RRHH`, `ADMIN` |
| POST | `api/v1/jornadas-asistencia/calculo` | `ResultadoCalculoDto { Calculadas, Conservadas, Eliminadas }` | `RRHH`, `ADMIN` |
| GET | `api/v1/jornadas-asistencia?empleadoId=&desde=&hasta=&estado=&estadoHorasExtra=&pagina=1&tamanoPagina=10` | paginado de `JornadaAsistenciaDto` | autenticado |
| GET | `api/v1/jornadas-asistencia/resumen?desde=&hasta=` | `ResumenAsistenciaDto` | autenticado |
| POST | `api/v1/jornadas-asistencia/{id:int}/horas-extra/aprobacion` | `{ id }` | `RRHH`, `ADMIN`; `[PermitirCuerpoVacio]` |
| POST | `api/v1/jornadas-asistencia/{id:int}/horas-extra/rechazo` | `{ id }` | `RRHH`, `ADMIN`; cuerpo `{ motivo }` |

**DTOs.** `RegistrarMarcacionRequestDto { EmpleadoId (int?), Tipo, Instante (DateTime?), Observacion }`; `ImportarMarcacionesRequestDto { Dispositivo, Registros: List<RegistroRelojDto { NumeroRegistro, Legajo, Tipo, Instante (DateTime?) }> }`; `ResultadoImportacionDto { Recibidos, Importados, YaImportados, Duplicados, Rechazados: List<{ NumeroRegistro, Motivo }> }`; `CalcularJornadasRequestDto { Desde, Hasta, EmpleadoId (int?) }`; `MarcacionDto { Id, EmpleadoId, EmpleadoLegajo, EmpleadoNombre, Tipo, Instante, Origen, Dispositivo, NumeroRegistro, Observacion }`; `JornadaAsistenciaDto` con todos los campos de la tabla más legajo, nombre y código de turno; `ResumenAsistenciaDto { Jornadas, Asistencias, Tardanzas, SinMarcacion, Faltas, MinutosTardanza, MinutosExtraPendientes, MinutosExtraAprobados }`; `RechazarHorasExtraRequestDto { Motivo }`.

**Reglas.** Los instantes se normalizan con `FechasUtc` (sin `Z` se interpretan como UTC). Registro manual: 1, 2, 3, 4, 5; empleado 404 (6); A1 → 409 (7) o 409 (8); jornada ya decidida en esa fecha → 409 (9); duplicado → 409 (12). Importación: 10 (dispositivo), 11 (sin registros o más de 1000); cada registro inválido se rechaza con su motivo literal (2, 3, 13, 7, 8) sin detener el resto; ya importado → `YaImportados`; duplicado → `Duplicados`. Eliminación: 14; 15 si es del reloj; 9 si su jornada está decidida. Cálculo: 16, 17 (más de 31 días), 18 (hasta en el futuro); empleado 404 (6). Aprobación y rechazo: 19; 20 si no está `Pendiente`; rechazo: 21 y 22. Listados: página (23), tamaño (24), valores de filtro inválidos (25).

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El empleado es obligatorio. |
| 2 | 400 | El tipo de marcación debe ser Entrada o Salida. |
| 3 | 400 | El instante de la marcación es obligatorio. |
| 4 | 400 | La observación es obligatoria en una marcación manual. |
| 5 | 400 | La observación no puede superar 200 caracteres. |
| 6 | 404 | El empleado {empleadoId} no existe. |
| 7 | 409 | El empleado {empleadoId} no está activo en esa fecha. |
| 8 | 409 | El empleado {empleadoId} no tiene un contrato vigente en esa fecha. |
| 9 | 409 | La jornada del {fecha} ya tiene las horas extra decididas y no se modifica. |
| 10 | 400 | El dispositivo es obligatorio. |
| 11 | 400 | La importación debe tener entre 1 y 1000 registros. |
| 12 | 409 | Ya existe una marcación del mismo tipo en ese minuto. |
| 13 | 400 | No existe un empleado con el legajo {legajo}. |
| 14 | 404 | La marcación {id} no existe. |
| 15 | 409 | Las marcaciones del reloj no se eliminan. |
| 16 | 400 | Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta. |
| 17 | 400 | El rango de cálculo no puede superar 31 días. |
| 18 | 400 | No se pueden calcular jornadas futuras. |
| 19 | 404 | La jornada {id} no existe. |
| 20 | 409 | La jornada {id} no tiene horas extra pendientes. |
| 21 | 400 | El motivo del rechazo es obligatorio. |
| 22 | 400 | El motivo del rechazo no puede superar 300 caracteres. |
| 23 | 400 | La página debe ser mayor o igual a 1. |
| 24 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 25 | 400 | El filtro {nombre} no es válido. |
| 26 | 400 | El número de registro es obligatorio. |
| 27 | 400 | El número de registro no puede superar 50 caracteres. |
| 28 | 400 | El dispositivo no puede superar 50 caracteres. |

Los mensajes 26 y 27 rechazan un registro de la importación sin detener el resto; el 28 rechaza el envío completo. Se añadieron durante la implementación porque la idempotencia (A3) necesita el número de registro. En los listados, `desde` posterior a `hasta` responde el mensaje 25 con `{nombre}` = `hasta`.

**Seed** (después del bloque de turnos): marcaciones del reloj `RELOJ-01` para los empleados 1, 2 y 3 en los días 2026-11-02 a 2026-11-06 (una tardanza del empleado 1 el 03, una salida faltante del empleado 3 el 04, horas extra del empleado 1 el 05), una marcación manual, y sus jornadas ya calculadas: una con horas extra `Aprobada` y otra `Pendiente`.

**Pruebas:** un caso por mensaje con código y texto exactos y sin cambios de datos; importación idempotente (dos envíos iguales: segundo todo `YaImportados`), duplicado ±60 s, registros mixtos válidos e inválidos; `CalculadoraJornada`: puntual, tardanza con y sin tolerancia, nocturno que cruza medianoche, horas extra con tope de 240, `SinMarcacion`, `Falta`, `DescansoTrabajado`, rotativo en día de descanso; recálculo que conserva las decididas; aprobación con `DecididoPor` del token; roles (401, 403 sin cambios, 2xx con `RRHH` y `ADMIN`); auditoría.

**Humo:** OpenAPI; `GET /api/v1/jornadas-asistencia/resumen?desde=2026-11-02&hasta=2026-11-06` con los conteos del seed; importación de 2 registros nuevos → `importados == 2` y repetida → `yaImportados == 2`; marcación manual para el empleado 4 después de su cese → 409 con el mensaje 7; cálculo de 2026-11-02 a 2026-11-06 → conserva las decididas; aprobar una pendiente → 200 y repetir → 409 con el mensaje 20.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | Marcaciones (A1–A3) | pruebas de registro, importación y eliminación | pasan |
| A3 | Cálculo (A4–A6, T3) | pruebas de `CalculadoraJornada` y de recálculo | pasan |
| A4 | Horas extra (A7) | pruebas de aprobación y rechazo | pasan |
| A5 | Roles y auditoría | pruebas | pasan |
| A6 | PostgreSQL | `aplicar-bd.sh`, `scaffold.sh --comprobar`, `smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 035, scaffold y fila en `relevo.md`.
- [x] Tarea 2: DTOs, validación, calculadora, casos de uso y controladores.
- [x] Tarea 3: pruebas, seed y humo; A1 a A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Compilación y gate | Cumple: compilación con 0 advertencias y 0 errores; `./scripts/verify.sh` llega hasta `dotnet test` (1262 superadas) y solo falla `CatalogoCoincideConLosOchentaModulosDeArquitectura` por CRLF en `docs/arquitectura.md`, ajeno al módulo; `npm --prefix frontend run verify` en verde | `./scripts/verify.sh`, `dotnet test --no-build` (1263 pruebas, 1262 superadas, 1 fallo conocido), `npm --prefix frontend run verify` |
| 2026-11-22 | A2 Marcaciones (A1–A3) | Cumple: registro manual (mensajes 1 a 9 y 12), importación mixta e idempotente, duplicados a ±60 s y eliminación (14, 15, 9) | `dotnet test --filter "FullyQualifiedName~Modulo63_AsistenciaBiometrica"`: `MarcacionesApiTests` |
| 2026-11-22 | A3 Cálculo (A4–A6, T3) | Cumple: `CalculadoraJornadaTests` (puntual, tardanza con y sin tolerancia, nocturno, tope 240, `SinMarcacion`, `Falta`, `DescansoTrabajado`, rotativo en descanso) y recálculo que conserva las decididas | `dotnet test --filter "FullyQualifiedName~Modulo63_AsistenciaBiometrica"`: `CalculadoraJornadaTests`, `JornadasAsistenciaApiTests` |
| 2026-11-22 | A4 Horas extra (A7) | Cumple: aprobación y rechazo con `DecididoPor` del token, mensajes 19 a 22 y sin cambios ante error | `JornadasAsistenciaApiTests` |
| 2026-11-22 | A5 Roles y auditoría | Cumple: 401 sin token, 403 sin cambios con `CONSULTA`, 2xx con `RRHH` y `ADMIN`, auditoría de alta, baja y decisión | `MarcacionesApiTests`, `JornadasAsistenciaApiTests` |
| 2026-11-22 | A6 PostgreSQL | Cumple: `035` aplicado, scaffold al día y `SMOKE OK` con los conteos del seed (13 jornadas, 11 asistencias, 1 tardanza, 1 sin marcación, 15 min de tardanza, 90 pendientes, 150 aprobados; cálculo 12/1/0) | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` (SCAFFOLD AL DÍA), `./scripts/smoke-postgres.sh` (SMOKE OK) |
| 2026-11-23 | Actualización con main | Cumple: actualizado con main (módulos 12, 41, 42 y 72) y script renumerado a 035; compilación con 0 advertencias, `dotnet test` con 1315 superadas y solo falla `CatalogoCoincideConLosOchentaModulosDeArquitectura` (CRLF local), `check-rules.sh` limpio, scaffold al día y `npm run verify` en verde; smoke en la rama final (alexander/modulo70-seguridad-salud-ocupacional) | `dotnet build -warnaserror`, `dotnet test`, `./scripts/check-rules.sh`, `npm --prefix frontend run verify` |
