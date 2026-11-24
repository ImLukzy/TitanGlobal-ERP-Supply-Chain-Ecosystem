# Especificación: modulo69-capacitaciones-backend — Capacitaciones (backend)

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22 de implementar los módulos 62 a 70 en secuencia y revisarlos al final. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** no se gestionan los cursos ni la formación del personal.
**Resultado esperado:** tablas `Cursos`, `ProgramacionesCurso`, `InscripcionesCurso` y `CertificacionesCurso` y `Modulo69_Capacitaciones` en `api/v1/cursos`, `api/v1/programaciones-curso` y `api/v1/capacitaciones`: catálogo (K1), plan anual con cupo (K2), inscripción sin cesados (K3), certificaciones con vencimiento (K4) y horas acumuladas por empleado y año (K5).

## 2. Fuera de alcance
- Costos, presupuestos, encuestas de satisfacción y adjuntos.
- Cambios en otros módulos, `Middlewares/`, `Services/`, `Program.cs` y `UnitOfWork`.

**Decisiones de producto (opción recomendada aplicada):**
1. **Estados de la programación.** `Programada` → `Realizada` (solo con `FechaFin <= hoy`) o `Cancelada` (solo `Programada`; sus inscripciones pasan a `Retirado`).
2. **Inscripción.** Estados `Inscrito` → `Aprobado` | `Desaprobado` (al registrar resultados de una programación `Realizada`) o `Retirado` (solo antes de `FechaInicio`). El cupo cuenta las inscripciones no `Retirado` (K2). Un empleado no se inscribe dos veces en la misma programación.
3. **K3 y T2.** Solo empleados `Activo`.
4. **Certificación (K4).** Una por inscripción `Aprobado`, con código único, emisión (no anterior al fin del curso) y vencimiento opcional (no anterior a la emisión). `Vencida` se calcula al leer (vencimiento anterior a hoy).
5. **Horas (K5).** Suma de `Cursos.Horas` de las inscripciones `Aprobado` cuya programación terminó en el año consultado.
6. **Roles.** Escritura `RRHH` o `ADMIN`; lectura autenticado.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `database/040-capacitaciones.sql` y su fila en `docs/equipo/relevo.md` §8 | crear / modificar |
| entidades y navegaciones regeneradas, `TitanGlobalDbContext.cs`, `PluralizadorEspanol.cs` si hace falta | regenerar |
| `src/TitanGlobal.Application/DTOs/Modulo69_Capacitaciones/*.cs` | crear |
| `src/TitanGlobal.Application/UseCases/Modulo69_Capacitaciones/AdministrarCursosUseCase.cs`, `AdministrarProgramacionesCursoUseCase.cs`, `ConsultarCapacitacionesUseCase.cs`, `ValidacionCapacitacion.cs` | crear |
| `src/TitanGlobal.Api/Controllers/Modulo69_Capacitaciones/CursosController.cs`, `ProgramacionesCursoController.cs`, `CapacitacionesController.cs` | crear |
| `tests/TitanGlobal.Api.Tests/Modulo69_Capacitaciones/*ApiTests.cs` | crear |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar |

## 4. Diseño y lógica
**Tablas:** `Cursos` (`Id`, `Codigo varchar(20)` único en mayúsculas, `Nombre varchar(150)`, `Horas numeric(5,1)` > 0 y ≤ 500, `Tipo varchar(20)` `Interno`/`Externo`/`Virtual`, `Proveedor varchar(150)`, `Activo`); `ProgramacionesCurso` (`Id`, `CursoId` FK, `Anio`, `FechaInicio`, `FechaFin`, `Cupo integer` 1–500, `Lugar varchar(150)`, `Estado`; `CHECK FechaFin >= FechaInicio`); `InscripcionesCurso` (`Id`, `ProgramacionCursoId` FK, `EmpleadoId` FK, `Estado`, `Nota numeric(4,1)` 0–20 nula, `FechaInscripcion date`; único (`ProgramacionCursoId`, `EmpleadoId`)); `CertificacionesCurso` (`Id`, `InscripcionCursoId` FK único, `Codigo varchar(40)` único, `FechaEmision`, `FechaVencimiento date` nula).

**Contrato HTTP** (escrituras `RRHH`/`ADMIN`):
| Verbo | Ruta |
|---|---|
| GET, POST | `api/v1/cursos?busqueda=&tipo=&activo=&pagina=&tamanoPagina=`, `api/v1/cursos` |
| GET, PUT | `api/v1/cursos/{id}` |
| POST | `api/v1/cursos/{id}/desactivacion`, `.../activacion` |
| GET, POST | `api/v1/programaciones-curso?anio=&cursoId=&estado=&pagina=&tamanoPagina=`, `api/v1/programaciones-curso` (`{ cursoId, fechaInicio, fechaFin, cupo, lugar }`; `Anio` = año de inicio) |
| GET | `api/v1/programaciones-curso/{id}` (con inscritos, cupo libre y certificaciones) |
| POST | `.../{id}/inscripciones` (`{ empleadoId }`), `.../{id}/inscripciones/{inscripcionId}/retiro`, `.../{id}/realizacion`, `.../{id}/cancelacion`, `.../{id}/resultados` (`{ resultados: [{ inscripcionId, aprobado, nota }] }`), `.../{id}/inscripciones/{inscripcionId}/certificacion` (`{ codigo, fechaEmision, fechaVencimiento }`) |
| GET | `api/v1/capacitaciones/horas?anio=&busqueda=&pagina=&tamanoPagina=` (horas por empleado activo, K5) |
| GET | `api/v1/capacitaciones/empleados/{empleadoId}?anio=` (cursos, horas y certificaciones con `Vencida`) |
| GET | `api/v1/capacitaciones/resumen?anio=` (`{ CursosActivos, Programadas, Realizadas, Inscritos, HorasTotales, CertificacionesVencidas }`) |

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El código es obligatorio y no puede superar 20 caracteres. |
| 2 | 400 | El nombre es obligatorio y no puede superar 150 caracteres. |
| 3 | 400 | Las horas deben ser mayores que 0 y como máximo 500, con un decimal. |
| 4 | 400 | El tipo debe ser Interno, Externo o Virtual. |
| 5 | 400 | El proveedor no puede superar 150 caracteres. |
| 6 | 409 | Ya existe un curso con el código {codigo}. |
| 7 | 404 | El curso {id} no existe. |
| 8 | 409 | El curso {codigo} está inactivo y no admite programaciones. |
| 9 | 409 | El curso {codigo} ya está {estado}. |
| 10 | 400 | Las fechas de inicio y fin son obligatorias. |
| 11 | 400 | La fecha de fin no puede ser anterior a la de inicio. |
| 12 | 400 | El cupo debe estar entre 1 y 500. |
| 13 | 400 | El lugar no puede superar 150 caracteres. |
| 14 | 404 | La programación {id} no existe. |
| 15 | 409 | La programación {id} no está programada. |
| 16 | 404 | El empleado {empleadoId} no existe. |
| 17 | 409 | El empleado {empleadoId} está cesado y no se puede inscribir. |
| 18 | 409 | El empleado {empleadoId} ya está inscrito en esta programación. |
| 19 | 409 | La programación {id} no tiene cupo libre. |
| 20 | 404 | La inscripción {id} no existe en esta programación. |
| 21 | 409 | Solo se puede retirar una inscripción antes del inicio del curso. |
| 22 | 409 | La programación {id} termina el {fechaFin} y aún no se puede marcar como realizada. |
| 23 | 409 | La programación {id} debe estar realizada para registrar resultados. |
| 24 | 400 | La nota debe estar entre 0 y 20, con un decimal. |
| 25 | 409 | La inscripción {id} no está aprobada y no admite certificación. |
| 26 | 409 | La inscripción {id} ya tiene certificación. |
| 27 | 400 | El código de certificación es obligatorio y no puede superar 40 caracteres. |
| 28 | 400 | La fecha de emisión es obligatoria y no puede ser anterior al fin del curso. |
| 29 | 400 | La fecha de vencimiento no puede ser anterior a la de emisión. |
| 30 | 409 | Ya existe una certificación con el código {codigo}. |
| 31 | 400 | La página debe ser mayor o igual a 1. |
| 32 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 33 | 400 | El filtro {nombre} no es válido. |

En el 9, `{estado}` es «activo» o «inactivo».

**Seed:** 4 cursos (uno inactivo); programaciones 2026: una realizada con resultados y certificaciones (una vencida), una programada futura con 2 inscritos y cupo 3, y una cancelada.

**Pruebas:** un caso por mensaje; cupo exacto (el siguiente → 409, tras un retiro → 200); resultados y certificación; `Vencida` calculada; horas por año (K5) con cursos de dos años; roles; auditoría.

**Humo:** OpenAPI; resumen 2026; horas del empleado con el curso realizado; inscripción que supera el cupo → 409 con el mensaje 19; inscripción de un cesado → 409 con el mensaje 17.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde (salvo el fallo CRLF conocido) |
| A2 | K1–K5 | pruebas | pasan |
| A3 | Roles y auditoría | pruebas | pasan |
| A4 | PostgreSQL | `aplicar-bd.sh`, `scaffold.sh --comprobar`, `smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 040, scaffold y fila en `relevo.md`.
- [x] Tarea 2: DTOs, validación, casos de uso y controladores.
- [x] Tarea 3: pruebas, seed y humo; A1 a A4 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Gate | Verde salvo el fallo CRLF conocido | `./scripts/verify.sh`: compilación sin advertencias; 1442 superadas, 1 fallo (`RolesPermisosApiTests.CatalogoCoincideConLosOchentaModulosDeArquitectura`, CRLF); `npm --prefix frontend run verify` correcto |
| 2026-11-22 | A2 K1–K5 | Pasan | 31 pruebas de `tests/TitanGlobal.Api.Tests/Modulo69_Capacitaciones/` (un caso por mensaje, cupo exacto, resultados, certificación, `Vencida`, horas por año) |
| 2026-11-22 | A3 Roles y auditoría | Pasan | `EscriturasExigenRolRrhhOAdmin...`, `RolesRrhhYAdmin...` y las pruebas de auditoría de cada controlador |
| 2026-11-22 | A4 PostgreSQL | `SMOKE OK` | `./scripts/aplicar-bd.sh` (040 aplicado), `./scripts/scaffold.sh --comprobar` («SCAFFOLD AL DÍA»), `./scripts/smoke-postgres.sh` termina en `SMOKE OK`; base local restaurada después |
| 2026-11-23 | A1 a A4 | cumple | Actualizado con main (módulos 12, 41, 42 y 72) y script renumerado a 040: gate en verde (`dotnet build -warnaserror` 0 advertencias; `dotnet test` 1495 superadas y 1 fallo, el CRLF conocido; `check-rules.sh` sin errores; frontend 1217 pruebas en verde); smoke en la rama final (alexander/modulo70-seguridad-salud-ocupacional) |
| 2026-11-24 | Actualización con main | OK (fallo ambiental conocido en `CatalogoCoincideConLosOchentaModulosDeArquitectura`) | Actualizado con main (módulo 48): build sin warnings, 1550 pruebas backend y 1254 frontend |

**Detalles resueltos al implementar (opción más simple):**
- `PUT` de curso permite editar siempre (también con programaciones); el código se guarda en mayúsculas y se valida como en el alta.
- `cursoId` o `empleadoId` ausentes en los cuerpos se tratan como 0 y devuelven los mensajes 7 o 16 (`El curso 0 no existe.`).
- Orden de validación de la programación: mensajes 10 a 13, luego 7 y 8.
- Inscripción: orden 14, 15, 16, 17, 18, 19. «Solo empleados Activo» se aplica como `Estado != Activo` → mensaje 17. Un empleado ya `Retirado` en esa programación se reinscribe reactivando su fila (por el índice único), sin mensaje 18; los demás estados dan el 18.
- Retiro: orden 14, 15, 20, 21. Una inscripción que no esté `Inscrito` (ya retirada o con resultados) o con `hoy >= FechaInicio` da el mensaje 21.
- Realización: `FechaFin <= hoy` (comparación en UTC). Cancelación: pasa a `Retirado` todas las inscripciones no retiradas.
- Resultados: todo se valida antes de guardar (atómico). Orden por fila: 20 (también si está `Retirado`), 24, 26 (la inscripción ya tiene certificación). La lista vacía o ausente es una operación sin cambios. Se puede corregir un resultado ya registrado si no hay certificación. `Nota` es opcional.
- Certificación: orden 20, 25, 26, 27, 28, 29, 30; el código se guarda en mayúsculas. No exige que la programación esté `Realizada` porque `Aprobado` ya lo implica.
- Listados: `anio` no se valida en `GET programaciones-curso`; en `capacitaciones/*` el rango válido es 1900–2200 (`El filtro anio no es válido.`, mensaje 33) y por defecto es el año actual. `tipo` y `estado` inválidos dan el 33; `cursoId` inexistente da el 7.
- K5 y `empleados/{id}`: las horas y la lista de cursos usan el año de `FechaFin` de la programación; los cursos excluyen las inscripciones `Retirado`; `certificaciones` devuelve todas las del empleado (no solo las del año) con `Vencida`; el empleado cesado se puede consultar, pero `horas` solo lista empleados `Activo`.
- Resumen del año: `Programadas`, `Realizadas` e `Inscritos` usan `Anio` de la programación (`Inscritos` = no `Retirado`); `HorasTotales` suma los aprobados cuya programación terminó en el año (todos los empleados); `CursosActivos` y `CertificacionesVencidas` no dependen del año.
- Seed: la programación futura inicia el 2027-03-15 (`Anio` 2027, porque `Anio` es el año de inicio) para que siga siendo futura; la realizada es de 2026 y la cancelada de 2026. La certificación vigente no tiene vencimiento para que el humo no dependa de la fecha. El humo usa fechas de 2099 para las programaciones nuevas.
- Se añadieron las entradas `CertificacionesCurso`, `InscripcionesCurso` y `ProgramacionesCurso` a `PluralizadorEspanol.cs`.
