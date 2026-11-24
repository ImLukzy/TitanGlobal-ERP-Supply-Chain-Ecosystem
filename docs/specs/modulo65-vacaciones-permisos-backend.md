# Especificación: modulo65-vacaciones-permisos-backend — VacacionesPermisos (backend)

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22 de implementar los módulos 62 a 70 en secuencia y revisarlos al final. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** no se registran vacaciones, permisos ni licencias. Planillas (66) necesita los días sin goce aprobados (V6, P4).
**Resultado esperado:** tabla `Ausencias` y `Modulo65_VacacionesPermisos` en `api/v1/ausencias`: solicitudes por tipo con su flujo `Pendiente` → `Aprobada` | `Rechazada` (y `Cancelada`), sin solapes entre aprobadas, y saldo vacacional calculado (devengado, gozado, programado).

Reglas aplicadas: V1 a V6, T2, T5 y T6. Lo que las reglas no fijan se marca **[elegido]**.

## 2. Fuera de alcance
- Descuento en planilla (66): solo leerá las ausencias `Aprobada` de tipo `PermisoSinGoce`.
- Venta de vacaciones, fraccionamiento mínimo legal, feriados y adjuntos (certificado médico).
- Cambios en otros módulos, `Middlewares/`, `Services/`, `Program.cs` y `UnitOfWork`.

**Decisiones de producto (opción recomendada aplicada):**
1. **Días.** Siempre días calendario, `FechaFin − FechaInicio + 1` (V2 habla de días calendario).
2. **Devengo (V2).** 2,5 días por mes completo de servicio desde `FechaIngreso` hasta la fecha de inicio de la solicitud (o hasta la fecha consultada en el saldo). Un mes completo se cuenta cuando se alcanza el mismo día del mes siguiente **[elegido]**.
3. **Saldo (V3).** `Saldo = Devengado − Gozado − Programado`. `Gozado`: días de vacaciones `Aprobada` ya transcurridos (hasta la fecha consultada inclusive); `Programado`: días `Aprobada` aún no transcurridos más los `Pendiente`. Incluir las pendientes evita aprobar dos solicitudes que juntas superan el saldo. El saldo se comprueba al registrar y otra vez al aprobar.
4. **Solapes (V4).** Una solicitud no se cruza con otra ausencia `Aprobada` ni `Pendiente` del mismo empleado (se comprueba al registrar y al aprobar).
5. **Topes por tipo [elegido].** `LicenciaMaternidad` hasta 98 días y `LicenciaPaternidad` hasta 10 días (Perú). `PermisoConGoce`, `PermisoSinGoce` y `DescansoMedico` exigen motivo. Solo `Vacaciones` consume saldo.
6. **Quién hace qué.** Registrar y cancelar: cualquier usuario autenticado (los empleados no tienen usuario propio, U2). Aprobar y rechazar: `RRHH` o `ADMIN` (V5, decisión 4 de `hcm-reglas-negocio.md`). `DecididoPor` es el usuario del JWT, que el controlador pasa al caso de uso.
7. **T2.** Solo se registran ausencias de empleados `Activo`. Sin `PUT` ni `DELETE`: una solicitud errónea se cancela o se rechaza y se registra otra.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/036-vacaciones-permisos.sql` | crear | tabla `Ausencias`; fila 036 en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo la fila 036 |
| entidad `Ausencia`, navegación en `Empleado`, `TitanGlobalDbContext.cs` | regenerar | `./scripts/scaffold.sh` |
| `src/TitanGlobal.Application/DTOs/Modulo65_VacacionesPermisos/*.cs` | crear | |
| `src/TitanGlobal.Application/UseCases/Modulo65_VacacionesPermisos/AdministrarAusenciasUseCase.cs`, `ValidacionAusencia.cs`, `CalculadoraSaldoVacacional.cs` | crear | la calculadora es estática y pública |
| `src/TitanGlobal.Api/Controllers/Modulo65_VacacionesPermisos/AusenciasController.cs` | crear | |
| `tests/TitanGlobal.Api.Tests/Modulo65_VacacionesPermisos/AusenciasApiTests.cs` | crear | |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | bloque después del de asistencia |

## 4. Diseño y lógica
**Tabla `Ausencias`:** `Id`; `Numero varchar(20)` único `AUS-{Id:D6}` (mismo patrón `TMP-` que el 62); `EmpleadoId` (FK, índice); `Tipo varchar(30)`; `FechaInicio`, `FechaFin date NOT NULL` (`CHECK FechaFin >= FechaInicio`); `Dias integer NOT NULL`; `Motivo varchar(300)`; `Estado varchar(20)` `Pendiente`/`Aprobada`/`Rechazada`/`Cancelada`; `FechaSolicitud date NOT NULL`; `DecididoPor varchar(100)`; `FechaDecision timestamptz`; `MotivoRechazo varchar(300)`; `FechaCancelacion date`; índice (`EmpleadoId`, `FechaInicio`).

**Contrato HTTP:**
| Verbo | Ruta | Respuesta OK | Acceso |
|---|---|---|---|
| GET | `api/v1/ausencias?busqueda=&empleadoId=&tipo=&estado=&desde=&hasta=&pagina=1&tamanoPagina=10` | paginado de `AusenciaListadoDto` | autenticado |
| GET | `api/v1/ausencias/resumen` | `ResumenAusenciasDto { Pendientes, AusentesHoy, VacacionesHoy, DescansosMedicosHoy, AprobadasMes }` | autenticado |
| GET | `api/v1/ausencias/{id:int}` | `AusenciaDetalleDto` | autenticado |
| GET | `api/v1/ausencias/saldo-vacacional/{empleadoId:int}?fecha=` | `SaldoVacacionalDto { EmpleadoId, FechaIngreso, Fecha, MesesServicio, DiasDevengados, DiasGozados, DiasProgramados, Saldo }` (decimales con 1 decimal) | autenticado |
| POST | `api/v1/ausencias` | `{ id, numero }` | autenticado |
| POST | `api/v1/ausencias/{id:int}/aprobacion` | `{ id }` | `RRHH`, `ADMIN`; `[PermitirCuerpoVacio]` |
| POST | `api/v1/ausencias/{id:int}/rechazo` | `{ id }` | `RRHH`, `ADMIN`; `{ motivo }` |
| POST | `api/v1/ausencias/{id:int}/cancelacion` | `{ id }` | autenticado; `[PermitirCuerpoVacio]` |

`RegistrarAusenciaRequestDto { EmpleadoId (int?), Tipo, FechaInicio (DateOnly?), FechaFin (DateOnly?), Motivo }`. `AusenciaListadoDto { Id, Numero, EmpleadoId, EmpleadoLegajo, EmpleadoNombre, Tipo, FechaInicio, FechaFin, Dias, Estado, FechaSolicitud }`; el detalle añade `Motivo`, `DecididoPor`, `FechaDecision`, `MotivoRechazo`, `FechaCancelacion`.

**Reglas** (gana el primer fallo). Registro: 1, 2, 3, 4, 5; motivo obligatorio según el tipo (6) y longitud (7); topes por tipo (8, 9); empleado 404 (10); no activo 409 (11); inicio anterior al ingreso 400 (12); solape 409 (13); vacaciones sin saldo suficiente 409 (14). Aprobación: 15; no `Pendiente` 409 (16); se repiten 13 y 14. Rechazo: 15, 16, 17, 18. Cancelación: 15; `Rechazada` o `Cancelada` 409 (19); ya empezó (`FechaInicio <= hoy`) 409 (20). Listado y saldo: 21, 22, 23 (filtro inválido), 10.

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El empleado es obligatorio. |
| 2 | 400 | El tipo debe ser Vacaciones, PermisoConGoce, PermisoSinGoce, LicenciaMaternidad, LicenciaPaternidad o DescansoMedico. |
| 3 | 400 | La fecha de inicio es obligatoria. |
| 4 | 400 | La fecha de fin es obligatoria. |
| 5 | 400 | La fecha de fin no puede ser anterior a la fecha de inicio. |
| 6 | 400 | El motivo es obligatorio para este tipo de ausencia. |
| 7 | 400 | El motivo no puede superar 300 caracteres. |
| 8 | 400 | La licencia de maternidad no puede superar 98 días. |
| 9 | 400 | La licencia de paternidad no puede superar 10 días. |
| 10 | 404 | El empleado {empleadoId} no existe. |
| 11 | 409 | El empleado {empleadoId} está cesado y no admite ausencias. |
| 12 | 400 | La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado. |
| 13 | 409 | La ausencia se cruza con otra ausencia pendiente o aprobada del empleado. |
| 14 | 409 | El saldo vacacional es de {saldo} días y la solicitud pide {dias}. |
| 15 | 404 | La ausencia {id} no existe. |
| 16 | 409 | La ausencia {numero} no está pendiente. |
| 17 | 400 | El motivo del rechazo es obligatorio. |
| 18 | 400 | El motivo del rechazo no puede superar 300 caracteres. |
| 19 | 409 | La ausencia {numero} ya está rechazada o cancelada. |
| 20 | 409 | La ausencia {numero} ya empezó y no se puede cancelar. |
| 21 | 400 | La página debe ser mayor o igual a 1. |
| 22 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 23 | 400 | El filtro {nombre} no es válido. |

En el mensaje 14 el saldo se escribe con un decimal y punto (`12.5`).

**Seed:** ausencias del empleado 1 (vacaciones aprobadas ya gozadas en 2026-02, vacaciones aprobadas futuras en 2027-01, permiso con goce pendiente), del empleado 2 (descanso médico aprobado, permiso sin goce aprobado en 2026-11) y del empleado 3 (vacaciones rechazadas y una cancelada).

**Pruebas:** un caso por mensaje con código y texto exactos y sin filas nuevas; número `AUS-`; `CalculadoraSaldoVacacional` (meses completos con fin de mes, gozado y programado, pendientes que restan); aprobación con `DecididoPor`; doble aprobación que supera el saldo → 409 (14); cancelación antes y después del inicio; roles (401; 403 al aprobar con un rol sin permiso; 2xx con `RRHH` y `ADMIN`; registro con cualquier token); auditoría.

**Humo:** OpenAPI; resumen; saldo del empleado 1 con valores del seed; solicitud de vacaciones que supera el saldo → 409 con el mensaje 14; registrar, aprobar y repetir la aprobación → 409 con el mensaje 16; cancelar una futura → 200.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | Registro y flujo (V1, V4, V5) | pruebas de los mensajes 1 a 20 | pasan |
| A3 | Saldo (V2, V3) | pruebas de la calculadora y del endpoint | pasan |
| A4 | Roles y auditoría | pruebas | pasan |
| A5 | PostgreSQL | `aplicar-bd.sh`, `scaffold.sh --comprobar`, `smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 036, scaffold y fila en `relevo.md`.
- [x] Tarea 2: DTOs, validación, calculadora, caso de uso y controlador.
- [x] Tarea 3: pruebas, seed y humo; A1 a A5 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | `verify.sh` llega al `dotnet test`: 1309 de 1310 pruebas en verde; solo falla `CatalogoCoincideConLosOchentaModulosDeArquitectura` (CRLF del entorno, ajeno al módulo) | `PATH=shim ./scripts/verify.sh`; `npm --prefix frontend run verify` en verde |
| 2026-11-22 | A2, A3, A4 | 47 pruebas del módulo 65 en verde (mensajes 1 a 23, calculadora con fin de mes, roles, auditoría) | `dotnet test --filter FullyQualifiedName~Modulo65` |
| 2026-11-22 | A5 | `SMOKE OK` con `036` aplicado; `scaffold.sh --comprobar`: SCAFFOLD AL DÍA | `./scripts/smoke-postgres.sh`, `./scripts/scaffold.sh --comprobar` |
| 2026-11-23 | Actualización con main | Actualizado con main (módulos 12, 41, 42 y 72) y script renumerado a 036: gate y smoke en verde (compilación con 0 advertencias, `dotnet test` con 1362 superadas y solo falla `CatalogoCoincideConLosOchentaModulosDeArquitectura` por CRLF local, `check-rules.sh` limpio, scaffold al día, `npm run verify` con 961 pruebas); smoke en la rama final (alexander/modulo70-seguridad-salud-ocupacional) | `dotnet build -warnaserror`, `dotnet test`, `./scripts/check-rules.sh`, `npm --prefix frontend run verify` |
| 2026-11-24 | Actualización con main | OK (fallo ambiental conocido en `CatalogoCoincideConLosOchentaModulosDeArquitectura`) | Actualizado con main (módulo 48): build sin warnings, 1417 pruebas backend y 998 frontend |

**Detalles resueltos al implementar (opción más simple):**
- `AprobadasMes`: ausencias `Aprobada` cuya `FechaInicio` cae en el mes calendario actual (UTC). `AusentesHoy` cuenta ausencias (no hay solapes entre aprobadas), no empleados.
- El listado con `empleadoId` inexistente responde 404 (mensaje 10), tras validar página y filtros (21, 22, 23).
- `desde` y `hasta` del listado filtran por solape con el periodo de la ausencia; `desde > hasta` es el filtro `hasta` inválido (mensaje 23).
- Rechazo: el orden es 15, 16, 17, 18 (el estado se comprueba antes que el motivo). El saldo comprobado es el de la fecha de inicio de la solicitud; `Gozado` + `Programado` suman todas las vacaciones `Aprobada` y `Pendiente` (distintas de la que se aprueba), así que el total no depende de la fecha.
- Una pendiente de fechas posteriores también resta saldo al aprobar una anterior (regla de decisión 3); se libera cancelándola o rechazándola.
- El motivo vacío se guarda como nulo (vacaciones). `Numero` usa `TMP-` y pasa a `AUS-{Id:D6}` en la misma transacción.
- No se añadió ningún mensaje literal fuera de la tabla de §4.
