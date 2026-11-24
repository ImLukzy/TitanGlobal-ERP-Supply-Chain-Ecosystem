# Especificación: modulo68-evaluacion-desempeno-backend — EvaluacionDesempeno (backend)

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22 de implementar los módulos 62 a 70 en secuencia y revisarlos al final. Pendiente de revisión final por Lukas.

## 1. Objetivo
**Problema:** no hay evaluación de desempeño.
**Resultado esperado:** tablas `CiclosEvaluacion`, `CompetenciasEvaluacion`, `Evaluaciones` y `CalificacionesEvaluacion` y `Modulo68_EvaluacionDesempeno` en `api/v1/ciclos-evaluacion` y `api/v1/evaluaciones`: ciclos con periodo y fechas (D1), competencias con pesos que suman 100 % (D2), calificación de 1 a 5 y puntaje ponderado (D3), una evaluación por empleado y ciclo con evaluador distinto (D4) solo para empleados con contrato en el periodo (D5), y estados `Borrador` → `Enviada` → `Cerrada` (D6).

## 2. Fuera de alcance
- Evaluación 360, autoevaluación, planes de mejora y vínculo con planillas.
- Cambios en otros módulos, `Middlewares/`, `Services/`, `Program.cs` y `UnitOfWork`.

**Decisiones de producto (opción recomendada aplicada):**
1. **Estados del ciclo.** `Configuracion` (se editan datos y competencias) → `Abierto` (se evalúa; exige pesos que sumen exactamente 100,00) → `Cerrado`. Cerrar el ciclo cierra sus evaluaciones `Enviada`; las que siguen en `Borrador` quedan congeladas (el ciclo cerrado no admite cambios, T3).
2. **Fechas (D1).** `FechaInicioPeriodo < FechaFinPeriodo`, con una duración (fin − inicio + 1) entre 170 y 190 días en `Semestral` o entre 360 y 370 en `Anual` **[elegido]**, `FechaApertura >= FechaFinPeriodo − 30 días` y `FechaCierre > FechaApertura`. Las evaluaciones se registran y se envían solo con el ciclo `Abierto` y hoy entre `FechaApertura` y `FechaCierre`.
3. **D5.** El evaluado tiene al menos un día de contrato (regla de vigencia del 62) dentro del periodo.
4. **Evaluador (D4).** Es otro empleado `Activo` (no hay jefe de área en el modelo, decisión 4 de `hcm-reglas-negocio.md`).
5. **Puntaje (D3).** `Σ(peso / 100 × calificación)` con 2 decimales; escala del resultado 1,00 a 5,00. Se calcula al enviar. Etiqueta: `< 2` «Insuficiente», `< 3` «En desarrollo», `< 4` «Competente», `< 4,5` «Destacado» y si no «Excepcional» **[elegido]**.
6. **Roles.** Toda escritura `RRHH` o `ADMIN` (los empleados no tienen usuario propio); lectura autenticado.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `database/039-evaluacion-desempeno.sql` y su fila en `docs/equipo/relevo.md` §8 | crear / modificar |
| entidades y navegaciones regeneradas, `TitanGlobalDbContext.cs`, `PluralizadorEspanol.cs` si hace falta | regenerar |
| `src/TitanGlobal.Application/DTOs/Modulo68_EvaluacionDesempeno/*.cs` | crear |
| `src/TitanGlobal.Application/UseCases/Modulo68_EvaluacionDesempeno/AdministrarCiclosEvaluacionUseCase.cs`, `AdministrarEvaluacionesUseCase.cs`, `ValidacionEvaluacion.cs` | crear |
| `src/TitanGlobal.Api/Controllers/Modulo68_EvaluacionDesempeno/CiclosEvaluacionController.cs`, `EvaluacionesController.cs` | crear |
| `tests/TitanGlobal.Api.Tests/Modulo68_EvaluacionDesempeno/*ApiTests.cs` | crear |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar |

## 4. Diseño y lógica
**Tablas:** `CiclosEvaluacion` (`Id`, `Codigo varchar(20)` único en mayúsculas, `Nombre varchar(100)`, `Periodo varchar(20)`, `FechaInicioPeriodo`, `FechaFinPeriodo`, `FechaApertura`, `FechaCierre date`, `Estado varchar(20)`); `CompetenciasEvaluacion` (`Id`, `CicloEvaluacionId` FK, `Nombre varchar(100)`, `Descripcion varchar(300)`, `Peso numeric(5,2)` > 0 y ≤ 100; único (`CicloEvaluacionId`, `Nombre`)); `Evaluaciones` (`Id`, `CicloEvaluacionId` FK, `EmpleadoId` FK, `EvaluadorId` FK a `Empleados`, `Estado`, `Comentario varchar(500)`, `PuntajeFinal numeric(4,2)`, `FechaEnvio`, `FechaCierre timestamptz`; único (`CicloEvaluacionId`, `EmpleadoId`); `CHECK EmpleadoId <> EvaluadorId`); `CalificacionesEvaluacion` (`Id`, `EvaluacionId` FK, `CompetenciaEvaluacionId` FK, `Calificacion integer` 1–5; único (`EvaluacionId`, `CompetenciaEvaluacionId`)).

**Contrato HTTP** (escrituras `RRHH`/`ADMIN`):
| Verbo | Ruta |
|---|---|
| GET | `api/v1/ciclos-evaluacion?busqueda=&estado=&pagina=&tamanoPagina=` |
| GET | `api/v1/ciclos-evaluacion/{id}` (con competencias, suma de pesos y conteo de evaluaciones por estado) |
| GET | `api/v1/ciclos-evaluacion/{id}/resultados` (`{ Evaluadas, PuntajePromedio, Distribucion: [{ Etiqueta, Cantidad }], PorCompetencia: [{ Competencia, Promedio }] }` con las `Enviada` y `Cerrada`) |
| POST, PUT | `api/v1/ciclos-evaluacion`, `api/v1/ciclos-evaluacion/{id}` (solo en `Configuracion`) |
| POST, PUT, DELETE | `api/v1/ciclos-evaluacion/{id}/competencias`, `.../competencias/{competenciaId}` (solo en `Configuracion`) |
| POST | `api/v1/ciclos-evaluacion/{id}/apertura`, `.../cierre` (`[PermitirCuerpoVacio]`) |
| GET | `api/v1/evaluaciones?cicloId=&empleadoId=&estado=&busqueda=&pagina=&tamanoPagina=` y `api/v1/evaluaciones/{id}` (con calificaciones y etiqueta) |
| POST | `api/v1/evaluaciones` (`{ cicloId, empleadoId, evaluadorId }`) |
| PUT | `api/v1/evaluaciones/{id}/calificaciones` (`{ calificaciones: [{ competenciaId, calificacion }], comentario }`, solo `Borrador`; reemplaza las existentes) |
| POST | `api/v1/evaluaciones/{id}/envio`, `.../cierre` (`[PermitirCuerpoVacio]`) |

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El código es obligatorio y no puede superar 20 caracteres. |
| 2 | 400 | El nombre es obligatorio y no puede superar 100 caracteres. |
| 3 | 400 | El periodo debe ser Semestral o Anual. |
| 4 | 400 | Las fechas del periodo, de apertura y de cierre son obligatorias. |
| 5 | 400 | La duración del periodo no corresponde a un ciclo {periodo}. |
| 6 | 400 | La apertura no puede ser anterior a 30 días antes del fin del periodo. |
| 7 | 400 | La fecha de cierre debe ser posterior a la de apertura. |
| 8 | 409 | Ya existe un ciclo con el código {codigo}. |
| 9 | 404 | El ciclo de evaluación {id} no existe. |
| 10 | 409 | El ciclo {codigo} ya no está en configuración. |
| 11 | 400 | El peso debe ser mayor que 0 y como máximo 100, con 2 decimales. |
| 12 | 409 | Ya existe la competencia {nombre} en el ciclo. |
| 13 | 404 | La competencia {id} no existe en el ciclo. |
| 14 | 409 | Los pesos de las competencias suman {suma} y deben sumar 100. |
| 15 | 409 | El ciclo {codigo} no está abierto. |
| 16 | 409 | El ciclo {codigo} solo admite evaluaciones entre el {apertura} y el {cierre}. |
| 17 | 400 | El ciclo, el empleado y el evaluador son obligatorios. |
| 18 | 400 | El evaluador no puede ser el propio evaluado. |
| 19 | 404 | El empleado {empleadoId} no existe. |
| 20 | 409 | El empleado {empleadoId} no tuvo contrato vigente durante el periodo del ciclo. |
| 21 | 409 | El evaluador {evaluadorId} no está activo. |
| 22 | 409 | El empleado {empleadoId} ya tiene una evaluación en el ciclo {codigo}. |
| 23 | 404 | La evaluación {id} no existe. |
| 24 | 409 | La evaluación {id} no está en borrador. |
| 25 | 400 | La calificación debe ser un número entero de 1 a 5. |
| 26 | 400 | La competencia {competenciaId} no pertenece al ciclo de la evaluación. |
| 27 | 400 | El comentario no puede superar 500 caracteres. |
| 28 | 409 | Faltan calificar {faltan} competencias para enviar la evaluación. |
| 29 | 409 | La evaluación {id} debe estar enviada para cerrarse. |
| 30 | 409 | El ciclo {codigo} ya está cerrado. |
| 31 | 400 | La página debe ser mayor o igual a 1. |
| 32 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 33 | 400 | El filtro {nombre} no es válido. |
| 34 | 400 | La descripción no puede superar 300 caracteres. |

En el 14, la suma con 2 decimales y punto. Fechas en los mensajes `yyyy-MM-dd`.

**Seed:** ciclo `2026-S1` cerrado (4 competencias, evaluaciones cerradas de los empleados 1, 2 y 3) y ciclo `2026-S2` abierto con apertura 2026-11-01 y cierre 2026-12-31 (una evaluación enviada y otra en borrador).

**Pruebas:** un caso por mensaje; puntaje ponderado exacto con pesos no redondos; flujo ciclo → competencias → apertura → evaluación → calificaciones → envío → cierre; cierre del ciclo que cierra las enviadas; D5 con contrato fuera del periodo; resultados; roles; auditoría.

**Humo:** OpenAPI; resultados del `2026-S1`; abrir un ciclo con pesos que no suman 100 → 409 con el mensaje 14; segunda evaluación del mismo empleado en `2026-S2` → 409 con el mensaje 22; enviar la evaluación en borrador con calificaciones completas → 200.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde (salvo el fallo CRLF conocido) |
| A2 | D1–D6 | pruebas | pasan |
| A3 | Roles y auditoría | pruebas | pasan |
| A4 | PostgreSQL | `aplicar-bd.sh`, `scaffold.sh --comprobar`, `smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 039, scaffold y fila en `relevo.md`.
- [x] Tarea 2: DTOs, validación, casos de uso y controladores.
- [x] Tarea 3: pruebas, seed y humo; A1 a A4 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | cumple | `./scripts/verify.sh`: compila con 0 advertencias y 0 errores; `dotnet test`: 1411 superadas y 1 fallo, el CRLF conocido `CatalogoCoincideConLosOchentaModulosDeArquitectura`; `npm --prefix frontend run verify` en verde |
| 2026-11-22 | A2, A3 | cumple | 39 pruebas del módulo 68 (`CiclosEvaluacionApiTests`, `EvaluacionesApiTests`): un caso por mensaje, puntaje con pesos no redondos (33,33/33,33/33,34 → 4,00; 45,5/54,5 con 3 y 4 → 3,55 por redondeo hacia arriba), límites de etiqueta, D5, flujo completo, cierre del ciclo, resultados, roles y auditoría |
| 2026-11-22 | A4 | cumple | `./scripts/aplicar-bd.sh` aplica 039; `./scripts/scaffold.sh --comprobar` → `SCAFFOLD AL DÍA`; `./scripts/smoke-postgres.sh` → `SMOKE OK` |
| 2026-11-23 | A1 a A4 | cumple | Actualizado con main (módulos 12, 41, 42 y 72) y script renumerado a 039: gate en verde (`dotnet build -warnaserror` 0 advertencias; `dotnet test` 1464 superadas y 1 fallo, el CRLF conocido; `check-rules.sh` sin errores; frontend 1158 pruebas en verde); smoke en la rama final (alexander/modulo70-seguridad-salud-ocupacional) |

### Opciones tomadas donde la spec no decía nada
- **Mensaje 34** (nuevo): la descripción de la competencia supera 300 caracteres. El orden de validación de una competencia es nombre (2), descripción (34), peso (11); después ciclo (9), estado (10) y duplicado (12).
- **Nombre de competencia duplicado** se compara sin distinguir mayúsculas; el índice único de la base distingue y es la guarda ante carreras.
- **Evaluador inexistente:** 404 con el mensaje 19 y el id del evaluador. El mensaje 21 solo cubre al evaluador existente que no está `Activo`.
- **Orden de reglas al crear una evaluación:** 17, 18, ciclo (9), ciclo abierto (15), ventana de fechas (16), evaluado (19), D5 (20), evaluador (19 o 21) y duplicado (22).
- **Calificaciones:** `PUT .../calificaciones` exige borrador (24) y ciclo `Abierto` (15), pero no la ventana de fechas (esa solo se exige al crear y enviar). Acepta una lista parcial o vacía; si repite una competencia gana la última. `calificacion` nula, fuera de 1 a 5 o con decimales da el mensaje 25; una `competenciaId` nula se trata como 0 y da el 26. El detalle de la evaluación devuelve todas las competencias del ciclo con `calificacion` nula si falta.
- **Cierre del ciclo** en `Configuracion` da el mensaje 15; en `Cerrado`, el 30. El cierre de una evaluación que no está `Enviada` da el 29 (también si su ciclo ya está cerrado).
- **Resultados:** `Distribucion` devuelve siempre las cinco etiquetas en orden de menor a mayor, con 0 si no hay; con cero evaluaciones, `PuntajePromedio` y cada `Promedio` son 0. Los promedios llevan 2 decimales con redondeo hacia arriba en el punto medio.
- **D5:** cuenta todo contrato no `Anulado` con `FechaInicio <= fin del periodo` y `(FechaCierre ?? FechaFin ?? infinito) >= inicio del periodo`. No exige que el evaluado esté `Activo`.
- **DELETE de competencia** devuelve 204 como el resto de los borrados del repositorio.
- **Seed:** la spec decía evaluaciones cerradas de los empleados 1, 2 y 3, pero el empleado 3 no tiene contrato en 2026 (D5), por lo que se usan los empleados 1, 2 y 4 (el 4 tenía contrato hasta su cese el 2026-09-30). El ciclo `2026-S2` usa el periodo 2026-06-01 a 2026-11-30 (183 días) para cumplir el mensaje 6 con su apertura del 2026-11-01; los empleados 1 y 2 tienen contrato indeterminado en ese periodo (D5).
- **Humo:** el envío en borrador y la segunda evaluación del mismo empleado se comprueban sobre un ciclo nuevo creado con fechas calculadas desde hoy (`date`), porque el mensaje 16 haría depender del calendario los del `2026-S2` (cierra el 2026-12-31).
