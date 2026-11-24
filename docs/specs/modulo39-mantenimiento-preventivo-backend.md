# Especificación: módulo 39 — Mantenimiento Preventivo (backend)

## 1. Objetivo

**Problema:** Manufactura ya tiene sus centros de trabajo (módulo 33), pero no puede programar las paradas para mantenerlos ni saber cuáles están pendientes, vencidas o terminadas.

**Resultado esperado:** el usuario programa paradas de mantenimiento preventivo para un centro de trabajo, las lleva por sus estados hasta completarlas o cancelarlas, ve las vencidas y las próximas, y obtiene la fecha sugerida de la siguiente parada según la frecuencia.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

**Dependencias:** solo usa `CentrosTrabajo` (módulo 33), que ya está en `main`. No depende de ramas abiertas.

## 2. Fuera de alcance

- Las cuatro entidades base, los middlewares y el pipeline. Los scripts de `database/` ya fusionados.
- Las averías, los repuestos y el downtime no planificado (módulo 40).
- La generación automática de la siguiente parada, los calendarios de turnos y el bloqueo de la capacidad del centro en las órdenes de producción (34).
- Cambios en el contrato del módulo 33, salvo el nuevo 409 al eliminar un centro (§4).
- La pantalla React, que tendrá su propia spec.

**Decisiones de producto que requieren aprobación:**
1. Una sola tabla: cada fila es una parada programada. No hay una tabla de «planes» aparte. La frecuencia en días es opcional y solo sirve para calcular la fecha sugerida de la siguiente parada. No crea filas sola.
2. Los estados son `Programado` → `EnEjecucion` → `Completado`, más `Cancelado` desde `Programado`. Una parada en ejecución no se cancela: se completa con las horas reales.
3. Un centro no puede tener dos paradas no canceladas el mismo día. Lo garantiza un índice único parcial.
4. Solo se programa en centros activos. Las paradas no se borran: se cancelan, con motivo. Solo se edita una parada `Programado`, y no se cambia de centro.
5. La fecha programada no puede ser anterior a hoy en hora de Lima (UTC−5), ni al crear ni al editar. Con UTC, a partir de las 19:00 de Lima ya sería el día siguiente y no se podría programar para hoy.

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/026-mantenimiento-preventivo.sql` | crear | Tabla `MantenimientosPreventivos`. |
| `docs/equipo/relevo.md` | modificar | Añadir solo la fila del 026 en la tabla §8. |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | Entrada `{ "MantenimientosPreventivos", "MantenimientoPreventivo" }`, porque las dos palabras van en plural. |
| `src/TitanGlobal.Domain/Models/MantenimientoPreventivo.cs` y navegación en `CentroTrabajo` | regenerar | Con `./scripts/scaffold.sh`, sin editar a mano. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | Con `./scripts/scaffold.sh`. |
| `src/TitanGlobal.Domain/Models/ValoresIniciales.cs` | modificar | `MantenimientoPreventivo() { Estado = "Programado"; }`. |
| `src/TitanGlobal.Application/DTOs/Modulo39_MantenimientoPreventivo/*.cs` | crear | DTOs de entrada y salida. |
| `src/TitanGlobal.Application/UseCases/Modulo39_MantenimientoPreventivo/*.cs` | crear | Casos de uso, `ValidacionMantenimientoPreventivo` y `PersistenciaMantenimientoPreventivo`. |
| `src/TitanGlobal.Application/UseCases/Modulo33_CentrosTrabajo/EliminarCentroTrabajoUseCase.cs` | modificar | Traducir la FK de mantenimientos a 409. |
| `src/TitanGlobal.Api/Controllers/Modulo39_MantenimientoPreventivo/MantenimientosPreventivosController.cs` | crear | Controlador delgado en `api/v1/mantenimientos-preventivos`. |
| `tests/TitanGlobal.Api.Tests/Modulo39_MantenimientoPreventivo/MantenimientosPreventivosApiTests.cs` | crear | Pruebas de integración con InMemory. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | modificar | Datos de ejemplo y comprobaciones, al final. |

## 4. Diseño y lógica

### Datos

| Columna | Tipo y restricción |
|---|---|
| `Id` | `integer` identidad, clave primaria. |
| `Numero` | `varchar(20)`, único (`IX_MantenimientosPreventivos_Numero`). Formato `MP-000001` a partir del `Id`, como `CTR-000001` del módulo 62. |
| `CentroTrabajoId` | FK a `CentrosTrabajo`, `ON DELETE RESTRICT`, con índice. |
| `Descripcion` | `varchar(250)`. Las tareas de la parada. |
| `FechaProgramada` | `date`. Índice. |
| `DuracionHorasPlanificada` | `numeric(5,2)`, `CHECK > 0 AND <= 168`. |
| `FrecuenciaDias` | `integer` nulo, `CHECK BETWEEN 1 AND 3650`. |
| `Responsable` | `varchar(100)` nulo. |
| `Estado` | `varchar(12)`, `CHECK` en `Programado`, `EnEjecucion`, `Completado` y `Cancelado`. Índice. |
| `FechaInicioReal` y `FechaFinReal` | `timestamptz` nulo. Se asignan en UTC al iniciar y al completar. |
| `HorasReales` | `numeric(5,2)` nulo, `CHECK > 0 AND <= 168`. Se informa al completar. |
| `Observaciones` | `varchar(500)` nulo. Se informa al completar. |
| `MotivoCancelacion` | `varchar(250)` nulo. |

Índice único parcial `IX_MantenimientosPreventivos_CentroTrabajoId_FechaProgramada` sobre (`CentroTrabajoId`, `FechaProgramada`) `WHERE "Estado" <> 'Cancelado'`.

### Contrato HTTP

Base `api/v1/mantenimientos-preventivos`. JWT obligatorio. JSON en camelCase. Errores `{ StatusCode, Message }`.

| Verbo y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /` | `busqueda?` (número o descripción), `centroTrabajoId?`, `estado?`, `desde?`, `hasta?` (sobre `FechaProgramada`), `pagina=1`, `tamanoPagina=20` | 200 `PaginaMantenimientosPreventivosDto` o 400. |
| `GET /resumen` | — | 200 `ResumenMantenimientosPreventivosDto`. |
| `GET /{id:int}` | — | 200 `MantenimientoPreventivoDetalleDto` o 404. |
| `POST /` | `CrearMantenimientoPreventivoRequestDto` | 201 `{ id, numero }` con `Location`, 400 o 409. |
| `PUT /{id:int}` | `EditarMantenimientoPreventivoRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/iniciar` | sin cuerpo (`[PermitirCuerpoVacio]`) | 200 `{ id }`, 404 o 409. |
| `POST /{id:int}/completar` | `CompletarMantenimientoPreventivoRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/cancelar` | `CancelarMantenimientoPreventivoRequestDto` | 200 `{ id }`, 400, 404 o 409. |

- `PaginaMantenimientosPreventivosDto { items: MantenimientoPreventivoListadoDto[], total, pagina, tamanoPagina }`
- `MantenimientoPreventivoListadoDto { id, numero, centroTrabajoId, centroTrabajoCodigo, centroTrabajoNombre, descripcion, fechaProgramada, duracionHorasPlanificada, estado, vencido }`: `vencido` es `true` si está `Programado` y su fecha es anterior a hoy (hora de Lima).
- `MantenimientoPreventivoDetalleDto` = listado + `{ frecuenciaDias, responsable, fechaInicioReal, fechaFinReal, horasReales, observaciones, motivoCancelacion, fechaSiguienteSugerida }`: `fechaSiguienteSugerida` = día de `FechaFinReal` + `FrecuenciaDias` si está `Completado` y tiene frecuencia; si no, `null`.
- `ResumenMantenimientosPreventivosDto { totalMantenimientos, porEstado: { estado, total }[], vencidos, proximos7Dias, horasPlanificadasPendientes }`: `proximos7Dias` son los `Programado` con fecha de hoy a hoy + 6; `horasPlanificadasPendientes` suma `Programado` y `EnEjecucion`. `porEstado` sigue el orden del ciclo y solo incluye estados con filas. Se agrega en la consulta, sin `ToListAsync` previo.
- `CrearMantenimientoPreventivoRequestDto { centroTrabajoId, descripcion, fechaProgramada, duracionHorasPlanificada, frecuenciaDias?, responsable? }`
- `EditarMantenimientoPreventivoRequestDto { descripcion, fechaProgramada, duracionHorasPlanificada, frecuenciaDias?, responsable? }`
- `CompletarMantenimientoPreventivoRequestDto { horasReales, observaciones? }` y `CancelarMantenimientoPreventivoRequestDto { motivo }`

El listado ordena por `FechaProgramada` ascendente y luego por `Id` ascendente. La búsqueda no distingue mayúsculas. El filtro de estado acepta los cuatro valores sin distinguir mayúsculas. Las fechas programadas y los filtros se reciben como día (`2026-12-01`) en `DateOnly`, igual que la columna `date` generada, así que no cambian de día por la zona horaria. `FechaInicioReal` y `FechaFinReal` se asignan con `DateTime.UtcNow`. «Hoy» es el día en hora de Lima: `DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5))` (Perú es UTC−5 y no tiene horario de verano; es la misma regla que `CalculadoraJornada.HoyLocal()` del módulo 63). Se calcula en un único método del módulo y se usa en la validación de la fecha programada, en `vencido` y en `proximos7Dias`. Los textos opcionales en blanco se guardan como `null`.

### Reglas y mensajes literales

Orden de validación: el listado valida página, tamaño, búsqueda, centro, estado y fechas. Las escrituras por ID verifican primero la existencia y luego el estado. El alta y la edición validan los campos en el orden del DTO y después el centro y la fecha repetida.

| Condición | HTTP | Mensaje |
|---|---|---|
| `pagina < 1` | 400 | La página debe ser mayor o igual a 1. |
| `tamanoPagina` fuera de 1–100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| Búsqueda de más de 50 caracteres | 400 | La búsqueda no puede superar 50 caracteres. |
| `centroTrabajoId` no positivo en alta o filtro | 400 | El identificador del centro de trabajo debe ser mayor que 0. |
| Estado de filtro inválido | 400 | El estado del mantenimiento preventivo no es válido. |
| `desde` posterior a `hasta` | 400 | La fecha desde no puede ser posterior a la fecha hasta. |
| Descripción vacía tras recortar espacios | 400 | La descripción del mantenimiento es obligatoria. |
| Descripción de más de 250 caracteres | 400 | La descripción del mantenimiento no puede superar 250 caracteres. |
| Fecha programada ausente | 400 | La fecha programada es obligatoria. |
| Fecha programada anterior a hoy (hora de Lima) | 400 | La fecha programada no puede ser anterior a hoy. |
| Duración fuera de (0, 168] | 400 | La duración debe ser mayor que 0 y como máximo 168 horas. |
| Duración con más de 2 decimales | 400 | La duración admite como máximo 2 decimales. |
| Frecuencia fuera de 1–3650 | 400 | La frecuencia debe estar entre 1 y 3650 días. |
| Responsable de más de 100 caracteres | 400 | El responsable no puede superar 100 caracteres. |
| Centro inexistente en el alta | 400 | El centro de trabajo {centroTrabajoId} no existe. |
| Centro inactivo en el alta | 409 | El centro de trabajo {centroTrabajoId} está inactivo. |
| Otra parada no cancelada del centro en la misma fecha | 409 | El centro de trabajo {codigo} ya tiene un mantenimiento programado el {aaaa-mm-dd}. |
| Mantenimiento inexistente | 404 | El mantenimiento preventivo {id} no existe. |
| Edición fuera de `Programado` | 409 | Solo se pueden editar mantenimientos programados. |
| Iniciar fuera de `Programado` | 409 | Solo se pueden iniciar mantenimientos programados. |
| Completar fuera de `EnEjecucion` | 409 | Solo se pueden completar mantenimientos en ejecución. |
| Horas reales fuera de (0, 168] | 400 | Las horas reales deben ser mayores que 0 y como máximo 168. |
| Horas reales con más de 2 decimales | 400 | Las horas reales admiten como máximo 2 decimales. |
| Observaciones de más de 500 caracteres | 400 | Las observaciones no pueden superar 500 caracteres. |
| Cancelar fuera de `Programado` | 409 | Solo se pueden cancelar mantenimientos programados. |
| Motivo vacío tras recortar espacios | 400 | El motivo de cancelación es obligatorio. |
| Motivo de más de 250 caracteres | 400 | El motivo de cancelación no puede superar 250 caracteres. |

La edición que mantiene su propia fecha es válida. La fecha repetida se comprueba en la consulta y también en la carrera: la `ViolacionRestriccionException` de unicidad sobre `IX_MantenimientosPreventivos_CentroTrabajoId_FechaProgramada` se traduce al mismo 409. Las transiciones y la edición de una misma parada se serializan con un bloqueo de su fila, como en los módulos 31 y 32.

**Cambio en el módulo 33:** el `DELETE /api/v1/centros-trabajo/{id:int}` devuelve 409 con «No se puede eliminar el centro de trabajo {id} porque tiene mantenimientos preventivos.» si alguna parada lo referencia, incluso cancelada o completada. Se traduce la `ViolacionRestriccionException` de la FK `FK_MantenimientosPreventivos_CentrosTrabajo_CentroTrabajoId`. El PR #49 también toca este archivo: al integrar se conservan las dos traducciones.

## 5. Criterios de aceptación

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato y reglas | `./scripts/verify.sh` | en verde |
| A3 | Tests | `dotnet test --filter Modulo39_MantenimientoPreventivo` | Cada mensaje de §4 con su código, las cuatro transiciones, la paginación, los filtros y ningún caso inválido crea ni cambia filas. |
| A4 | Contrato API | `/openapi/v1.json` | Las ocho operaciones presentes y autenticadas. |
| A5 | Comportamiento | Pruebas de integración | `vencido`, `fechaSiguienteSugerida` y el resumen exactos. «Hoy» se toma en hora de Lima: a las 19:00 de Lima (00:00 UTC del día siguiente) se puede programar para ese mismo día. Una parada cancelada libera su fecha. El DELETE de un centro con paradas devuelve el 409 literal. |
| A6 | PostgreSQL | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` y `./scripts/smoke-postgres.sh` | `SMOKE OK`, incluidas fechas programadas enviadas como día que se devuelven sin cambiar de día, el 409 de fecha repetida por el índice parcial y dos transiciones concurrentes de la misma parada que dejan un único cambio de estado. |
| A7 | Seed | `scripts/seed-dev.sql` dos veces | Cuatro paradas (`Programado` ×2, `EnEjecucion` y `Completado`) sobre centros del seed del 33, sin duplicar filas. |

## 6. Checklist de ejecución

- [ ] Aprobación de Lukas sobre el alcance, las decisiones de §2 y el contrato.
- [ ] Commit 1: script SQL 026, fila en `relevo.md`, pluralizador y entidades generadas.
- [ ] Commit 2: DTOs, validaciones y casos de uso.
- [ ] Commit 3: controlador, cambio en el 33, pruebas, seed y smoke.
- [ ] Verificar A1–A7 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
