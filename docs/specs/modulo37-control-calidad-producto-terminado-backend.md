# Especificación: módulo 37 — Control de Calidad de Producto Terminado (backend)

## 1. Objetivo

**Problema:** una orden de producción (módulo 34) termina con una cantidad producida, pero no queda registrado si esas unidades pasaron la inspección final ni cuántas resultaron no conformes.

**Resultado esperado:** el usuario inspecciona las unidades producidas de una orden terminada, en una o varias inspecciones, las dictamina como aprobadas o rechazadas según las unidades no conformes y la tolerancia, las anula si fueron un error, y consulta por orden cuántas unidades se inspeccionaron, cuántas resultaron conformes y cuántas faltan por inspeccionar.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

**Dependencias:** usa `OrdenesProduccion` (módulo 34, script 025). La implementación empieza cuando el 34 esté integrado en `main`.

## 2. Fuera de alcance

- Las cuatro entidades base, los middlewares y el pipeline. Los scripts de `database/` ya fusionados.
- El ingreso del producto terminado al almacén y el bloqueo del stock rechazado (módulos 25 a 27).
- El registro de las mermas y su causa (módulo 38). Las unidades no conformes se informan aquí, pero la merma se registra en el 38.
- Planes de muestreo, normas y características medidas una por una.
- Cambios en el contrato del módulo 34.
- La pantalla React, que tiene su propia spec (`modulo37-control-calidad-producto-terminado-frontend.md`).

**Decisiones de producto que requieren aprobación:**
1. Solo se inspeccionan órdenes `Terminada`. Una orden puede tener varias inspecciones (por tandas), pero la suma de unidades de las inspecciones no anuladas no puede superar su `CantidadProducida`.
2. Las cantidades se miden en unidades enteras, como en el módulo 34.
3. Los estados son `Pendiente` → `Aprobada` o `Rechazada`, más `Anulada` desde `Pendiente`. Una inspección dictaminada no cambia.
4. Al dictaminar se informan las unidades no conformes. Si su porcentaje supera la tolerancia, la inspección solo puede rechazarse. Si no la supera, el inspector puede aprobarla o rechazarla.
5. Solo se edita una inspección `Pendiente`, y no se cambia de orden.

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/044-control-calidad-producto-terminado.sql` | crear | Tabla `InspeccionesProductoTerminado`. Número reservado en `docs/equipo/relevo.md` §8. |
| `docs/equipo/relevo.md` | modificar | Añadir solo la fila del 044 en la tabla §8. |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | Entrada `{ "InspeccionesProductoTerminado", "InspeccionProductoTerminado" }` si el scaffold no la singulariza bien (solo cambia la primera palabra). |
| `src/TitanGlobal.Domain/Models/InspeccionProductoTerminado.cs` y navegación en `OrdenProduccion` | regenerar | Con `./scripts/scaffold.sh`, sin editar a mano. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | Con `./scripts/scaffold.sh`. |
| `src/TitanGlobal.Domain/Models/ValoresIniciales.cs` | modificar | `InspeccionProductoTerminado() { Estado = "Pendiente"; }`. |
| `src/TitanGlobal.Application/DTOs/Modulo37_ControlCalidadProductoTerminado/*.cs` | crear | DTOs de entrada y salida. |
| `src/TitanGlobal.Application/UseCases/Modulo37_ControlCalidadProductoTerminado/*.cs` | crear | Casos de uso y `ValidacionInspeccionProductoTerminado`. |
| `src/TitanGlobal.Api/Controllers/Modulo37_ControlCalidadProductoTerminado/InspeccionesProductoTerminadoController.cs` | crear | Controlador delgado en `api/v1/inspecciones-producto-terminado`. |
| `tests/TitanGlobal.Api.Tests/Modulo37_ControlCalidadProductoTerminado/InspeccionesProductoTerminadoApiTests.cs` | crear | Pruebas de integración con InMemory. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | modificar | Datos de ejemplo y comprobaciones, al final. |

## 4. Diseño y lógica

### Datos

| Columna | Tipo y restricción |
|---|---|
| `Id` | `integer` identidad, clave primaria. |
| `Numero` | `varchar(20)`, único (`IX_InspeccionesProductoTerminado_Numero`). Formato `CCP-000001` a partir del `Id`. |
| `OrdenProduccionId` | FK a `OrdenesProduccion`, `ON DELETE RESTRICT`, con índice. |
| `FechaInspeccion` | `date`. Índice. |
| `CantidadInspeccionada` | `integer`, `CHECK > 0`. |
| `ToleranciaNoConformesPorcentaje` | `numeric(5,2)`, `CHECK BETWEEN 0 AND 100`. |
| `Inspector` | `varchar(100)`. |
| `Estado` | `varchar(10)`, `CHECK` en `Pendiente`, `Aprobada`, `Rechazada` y `Anulada`. Índice. |
| `CantidadNoConforme` | `integer` nulo, `CHECK >= 0 AND <= "CantidadInspeccionada"`. Se informa al dictaminar. |
| `FechaDictamen` | `timestamptz` nulo. Se asigna en UTC al dictaminar. |
| `Observaciones` | `varchar(500)` nulo. |
| `MotivoAnulacion` | `varchar(250)` nulo. |

### Contrato HTTP

Base `api/v1/inspecciones-producto-terminado`. JWT obligatorio. JSON en camelCase. Errores `{ StatusCode, Message }`.

| Verbo y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /` | `busqueda?` (número de inspección o de orden), `ordenProduccionId?`, `productoId?`, `estado?`, `desde?`, `hasta?` (sobre `FechaInspeccion`), `pagina=1`, `tamanoPagina=20` | 200 `PaginaInspeccionesProductoTerminadoDto` o 400. |
| `GET /resumen` | — | 200 `ResumenInspeccionesProductoTerminadoDto`. |
| `GET /ordenes/{ordenProduccionId:int}/avance` | — | 200 `AvanceInspeccionOrdenDto` o 404. |
| `GET /{id:int}` | — | 200 `InspeccionProductoTerminadoDetalleDto` o 404. |
| `POST /` | `CrearInspeccionProductoTerminadoRequestDto` | 201 `{ id, numero }` con `Location`, 400 o 409. |
| `PUT /{id:int}` | `EditarInspeccionProductoTerminadoRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/dictaminar` | `DictaminarInspeccionProductoTerminadoRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/anular` | `AnularInspeccionProductoTerminadoRequestDto` | 200 `{ id }`, 400, 404 o 409. |

- `PaginaInspeccionesProductoTerminadoDto { items: InspeccionProductoTerminadoListadoDto[], total, pagina, tamanoPagina }`
- `InspeccionProductoTerminadoListadoDto { id, numero, ordenProduccionId, ordenProduccionNumero, productoId, productoNombre, fechaInspeccion, cantidadInspeccionada, estado, porcentajeNoConforme }`: `porcentajeNoConforme` = `CantidadNoConforme / CantidadInspeccionada × 100` con 2 decimales, o `null` sin dictamen.
- `InspeccionProductoTerminadoDetalleDto` = listado + `{ toleranciaNoConformesPorcentaje, inspector, cantidadNoConforme, cantidadConforme, fechaDictamen, observaciones, motivoAnulacion, superaTolerancia }`: `cantidadConforme` = inspeccionada − no conforme; `cantidadConforme` y `superaTolerancia` son `null` sin dictamen.
- `AvanceInspeccionOrdenDto { ordenProduccionId, ordenProduccionNumero, productoNombre, estadoOrden, cantidadProducida, unidadesInspeccionadas, unidadesPorInspeccionar, unidadesConformesAprobadas, unidadesNoConformes }`: cuentan las inspecciones no anuladas; `unidadesConformesAprobadas` suma las conformes de las `Aprobada`; `unidadesNoConformes` suma las no conformes de las dictaminadas. Si la orden no está `Terminada`, `cantidadProducida` es `null` y las unidades son 0.
- `ResumenInspeccionesProductoTerminadoDto { totalInspecciones, porEstado: { estado, total }[], tasaRechazoPorcentaje, unidadesInspeccionadas, unidadesNoConformes, ordenesTerminadasSinInspeccion }`: la tasa = rechazadas / (aprobadas + rechazadas) × 100 con 2 decimales, 0 si no hay dictámenes; las unidades cuentan las inspecciones dictaminadas; la última cifra cuenta las órdenes `Terminada` sin ninguna inspección no anulada. `porEstado` sigue el orden del ciclo y solo incluye estados con filas. Se agrega en la consulta, sin `ToListAsync` previo.
- `CrearInspeccionProductoTerminadoRequestDto { ordenProduccionId, fechaInspeccion, cantidadInspeccionada, toleranciaNoConformesPorcentaje, inspector }`
- `EditarInspeccionProductoTerminadoRequestDto { fechaInspeccion, cantidadInspeccionada, toleranciaNoConformesPorcentaje, inspector }`
- `DictaminarInspeccionProductoTerminadoRequestDto { resultado, cantidadNoConforme, observaciones? }`: `resultado` es `Aprobada` o `Rechazada`.
- `AnularInspeccionProductoTerminadoRequestDto { motivo }`

El listado ordena por `FechaInspeccion` descendente y luego por `Id` descendente. La búsqueda no distingue mayúsculas. Los filtros de estado y el resultado aceptan sus valores sin distinguir mayúsculas. `FechaInspeccion` y los filtros se reciben como día (`2026-12-01`) en `DateOnly`, igual que la columna `date` generada. `FechaDictamen` se asigna con `DateTime.UtcNow`. «Hoy» es el día en hora de Lima: `DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5))` (Perú es UTC−5 y no tiene horario de verano; es la misma regla que `CalculadoraJornada.HoyLocal()` del módulo 63). Con UTC, a partir de las 19:00 de Lima ya sería el día siguiente y se aceptaría una fecha de inspección de mañana. Los textos opcionales en blanco se guardan como `null`.

### Reglas y mensajes literales

Orden de validación: el listado valida página, tamaño, búsqueda, orden, producto, estado y fechas. El alta y la edición validan los campos en el orden del DTO, después la orden (existencia, estado y fecha) y por último las unidades disponibles. Las escrituras por ID verifican primero la existencia y luego el estado.

| Condición | HTTP | Mensaje |
|---|---|---|
| `pagina < 1` | 400 | La página debe ser mayor o igual a 1. |
| `tamanoPagina` fuera de 1–100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| Búsqueda de más de 20 caracteres | 400 | La búsqueda no puede superar 20 caracteres. |
| `ordenProduccionId` no positivo en alta o filtro | 400 | El identificador de la orden de producción debe ser mayor que 0. |
| `productoId` no positivo en filtro | 400 | El identificador del producto debe ser mayor que 0. |
| Estado de filtro inválido | 400 | El estado de la inspección de producto terminado no es válido. |
| `desde` posterior a `hasta` | 400 | La fecha desde no puede ser posterior a la fecha hasta. |
| Fecha de inspección ausente | 400 | La fecha de inspección es obligatoria. |
| Fecha de inspección posterior a hoy (hora de Lima) | 400 | La fecha de inspección no puede ser posterior a hoy. |
| Cantidad inspeccionada fuera de 1–1000000 | 400 | La cantidad inspeccionada debe estar entre 1 y 1000000. |
| Tolerancia fuera de 0–100 | 400 | La tolerancia de no conformes debe estar entre 0 y 100. |
| Tolerancia con más de 2 decimales | 400 | La tolerancia de no conformes admite como máximo 2 decimales. |
| Inspector vacío tras recortar espacios | 400 | El inspector es obligatorio. |
| Inspector de más de 100 caracteres | 400 | El inspector no puede superar 100 caracteres. |
| Orden inexistente en el alta | 400 | La orden de producción {ordenProduccionId} no existe. |
| Orden fuera de `Terminada` en el alta | 409 | Solo se pueden inspeccionar órdenes de producción terminadas. |
| Fecha de inspección anterior al día de `FechaFinReal` de la orden | 400 | La fecha de inspección no puede ser anterior al fin de la orden {numero}. |
| Unidades de las inspecciones no anuladas por encima de la cantidad producida | 409 | La orden {numero} solo tiene {disponibles} unidades por inspeccionar. |
| Orden inexistente en el avance | 404 | La orden de producción {ordenProduccionId} no existe. |
| Inspección inexistente | 404 | La inspección de producto terminado {id} no existe. |
| Edición fuera de `Pendiente` | 409 | Solo se pueden editar inspecciones pendientes. |
| Dictaminar fuera de `Pendiente` | 409 | Solo se pueden dictaminar inspecciones pendientes. |
| Resultado distinto de `Aprobada` o `Rechazada` | 400 | El resultado debe ser Aprobada o Rechazada. |
| Unidades no conformes negativas | 400 | Las unidades no conformes no pueden ser negativas. |
| Unidades no conformes mayores que las inspeccionadas | 400 | Las unidades no conformes no pueden superar las inspeccionadas. |
| Observaciones de más de 500 caracteres | 400 | Las observaciones no pueden superar 500 caracteres. |
| Aprobar con no conformes por encima de la tolerancia | 409 | La inspección supera la tolerancia de no conformes ({porcentaje} % > {tolerancia} %) y solo puede rechazarse. |
| Anular fuera de `Pendiente` | 409 | Solo se pueden anular inspecciones pendientes. |
| Motivo vacío tras recortar espacios | 400 | El motivo de anulación es obligatorio. |
| Motivo de más de 250 caracteres | 400 | El motivo de anulación no puede superar 250 caracteres. |

En la edición, las unidades disponibles excluyen las de la propia inspección. En el mensaje de tolerancia, los porcentajes se escriben con 2 decimales y punto decimal (`12.50 % > 5.00 %`). El alta y la edición bloquean la fila de la orden de producción para que dos inspecciones concurrentes no superen juntas la cantidad producida. El dictamen y la anulación de una misma inspección se serializan con un bloqueo de su fila.

El módulo 34 no cambia su contrato: las órdenes no se borran, así que la FK de inspecciones no necesita un nuevo 409.

## 5. Criterios de aceptación

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato y reglas | `./scripts/verify.sh` | en verde |
| A3 | Tests | `dotnet test --filter Modulo37_ControlCalidadProductoTerminado` | Cada mensaje de §4 con su código, las tres transiciones, la paginación, los filtros y ningún caso inválido crea ni cambia filas. |
| A4 | Contrato API | `/openapi/v1.json` | Las ocho operaciones presentes y autenticadas. |
| A5 | Comportamiento | Pruebas de integración | `porcentajeNoConforme`, `cantidadConforme`, el avance por orden y el resumen exactos. Una inspección anulada libera sus unidades. Con no conformes sobre la tolerancia, aprobar da 409 y rechazar funciona. |
| A6 | PostgreSQL | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` y `./scripts/smoke-postgres.sh` | `SMOKE OK`, incluidas dos altas concurrentes sobre la misma orden que no superan juntas la cantidad producida. |
| A7 | Seed | `scripts/seed-dev.sql` dos veces | Tres inspecciones (`Pendiente`, `Aprobada` y `Rechazada`) sobre la orden `Terminada` del seed del 34, sin duplicar filas. |

## 6. Checklist de ejecución

- [ ] Aprobación de Lukas sobre el alcance, las decisiones de §2 y el contrato.
- [ ] Módulo 34 integrado en `main`, y rama actualizada.
- [ ] Commit 1: script SQL 044, pluralizador (si hace falta) y entidades generadas.
- [ ] Commit 2: DTOs, validaciones y casos de uso.
- [ ] Commit 3: controlador, pruebas, seed y smoke.
- [ ] Verificar A1–A7 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
