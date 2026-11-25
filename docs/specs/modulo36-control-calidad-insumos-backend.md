# Especificación: módulo 36 — Control de Calidad de Insumos (backend)

## 1. Objetivo

**Problema:** los insumos llegan de los proveedores y pasan a producción sin que quede registrada ninguna inspección de calidad, ni quién la hizo, ni por qué se rechazó un lote.

**Resultado esperado:** el usuario registra la inspección de un lote de insumo de un proveedor, la dictamina como aprobada o rechazada según el porcentaje de defectos y la tolerancia, la anula si fue un error, y consulta la tasa de rechazo por proveedor.

Responsable: Antony Huayhua. Estado: aprobada por Lukas (PR #77, fusionado el 2026-11-24).

**Dependencias:** solo usa `Productos` y `Proveedores`, que ya están en `main`. No depende de ramas abiertas.

## 2. Fuera de alcance

- Las cuatro entidades base, los middlewares y el pipeline. Los scripts de `database/` ya fusionados.
- La recepción de mercadería y las órdenes de abastecimiento (módulo 15), los lotes y series (módulo 25) y el bloqueo del stock rechazado.
- Planes de muestreo, normas (AQL, ISO) y características medidas una por una.
- La tolerancia por defecto de la empresa (irá en Parámetros del Sistema, módulo 79). Aquí se informa en cada inspección.
- Cambios en los contratos de los módulos 11 y 21.
- La pantalla React, que tiene su propia spec (`modulo36-control-calidad-insumos-frontend.md`).

**Decisiones de producto que requieren aprobación:**
1. Una inspección por lote: un mismo producto y proveedor no puede tener dos inspecciones no anuladas con el mismo lote. Lo garantiza un índice único parcial.
2. Los estados son `Pendiente` → `Aprobada` o `Rechazada`, más `Anulada` desde `Pendiente`. Una inspección dictaminada no cambia.
3. Al dictaminar se informa la cantidad defectuosa de la muestra. Si el porcentaje de defectos supera la tolerancia, la inspección solo puede rechazarse. Si no la supera, el inspector puede aprobarla o rechazarla.
4. Solo se inspeccionan insumos de proveedores activos. Solo se edita una inspección `Pendiente`.
5. Borrar un proveedor con inspecciones queda bloqueado por la FK. Hoy el borrado de proveedores (módulo 11) no traduce esa FK a 409, igual que ya pasa con las evaluaciones del módulo 12. Este módulo no modifica el 11: se propone a su responsable que lo traduzca.

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/043-control-calidad-insumos.sql` | crear | Tabla `InspeccionesInsumos`. Número reservado en `docs/equipo/relevo.md` §8. |
| `docs/equipo/relevo.md` | modificar | Añadir solo la fila del 043 en la tabla §8. |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | Entrada `{ "InspeccionesInsumos", "InspeccionInsumo" }`, porque las dos palabras van en plural. |
| `src/TitanGlobal.Domain/Models/InspeccionInsumo.cs` y navegaciones en `Producto` y `Proveedor` | regenerar | Con `./scripts/scaffold.sh`, sin editar a mano. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | Con `./scripts/scaffold.sh`. |
| `src/TitanGlobal.Domain/Models/ValoresIniciales.cs` | modificar | `InspeccionInsumo() { Estado = "Pendiente"; }`. |
| `src/TitanGlobal.Application/DTOs/Modulo36_ControlCalidadInsumos/*.cs` | crear | DTOs de entrada y salida. |
| `src/TitanGlobal.Application/UseCases/Modulo36_ControlCalidadInsumos/*.cs` | crear | Casos de uso y `ValidacionInspeccionInsumo`. |
| `src/TitanGlobal.Api/Controllers/Modulo36_ControlCalidadInsumos/InspeccionesInsumosController.cs` | crear | Controlador delgado en `api/v1/inspecciones-insumos`. |
| `tests/TitanGlobal.Api.Tests/Modulo36_ControlCalidadInsumos/InspeccionesInsumosApiTests.cs` | crear | Pruebas de integración con InMemory. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | modificar | Datos de ejemplo y comprobaciones, al final. |

## 4. Diseño y lógica

### Datos

| Columna | Tipo y restricción |
|---|---|
| `Id` | `integer` identidad, clave primaria. |
| `Numero` | `varchar(20)`, único (`IX_InspeccionesInsumos_Numero`). Formato `CCI-000001` a partir del `Id`. |
| `ProductoId` | FK a `Productos`, `ON DELETE RESTRICT`, con índice. El insumo inspeccionado. |
| `ProveedorId` | FK a `Proveedores`, `ON DELETE RESTRICT`, con índice. |
| `Lote` | `varchar(50)`, `CHECK (btrim("Lote") <> '')`. Se guarda en mayúsculas y sin espacios en los extremos. |
| `FechaInspeccion` | `date`. Índice. |
| `CantidadRecibida` | `numeric(18,3)`, `CHECK > 0`. |
| `CantidadMuestra` | `numeric(18,3)`, `CHECK > 0 AND <= "CantidadRecibida"`. |
| `UnidadMedida` | `varchar(20)`, `CHECK (btrim("UnidadMedida") <> '')`. |
| `ToleranciaDefectosPorcentaje` | `numeric(5,2)`, `CHECK BETWEEN 0 AND 100`. |
| `Inspector` | `varchar(100)`. |
| `Estado` | `varchar(10)`, `CHECK` en `Pendiente`, `Aprobada`, `Rechazada` y `Anulada`. Índice. |
| `CantidadDefectuosa` | `numeric(18,3)` nulo, `CHECK >= 0 AND <= "CantidadMuestra"`. Se informa al dictaminar. |
| `FechaDictamen` | `timestamptz` nulo. Se asigna en UTC al dictaminar. |
| `Observaciones` | `varchar(500)` nulo. |
| `MotivoAnulacion` | `varchar(250)` nulo. |

Índice único parcial `IX_InspeccionesInsumos_ProductoId_ProveedorId_Lote` sobre (`ProductoId`, `ProveedorId`, `Lote`) `WHERE "Estado" <> 'Anulada'`.

### Contrato HTTP

Base `api/v1/inspecciones-insumos`. JWT obligatorio. JSON en camelCase. Errores `{ StatusCode, Message }`.

| Verbo y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /` | `busqueda?` (número o lote), `productoId?`, `proveedorId?`, `estado?`, `desde?`, `hasta?` (sobre `FechaInspeccion`), `pagina=1`, `tamanoPagina=20` | 200 `PaginaInspeccionesInsumosDto` o 400. |
| `GET /resumen` | — | 200 `ResumenInspeccionesInsumosDto`. |
| `GET /{id:int}` | — | 200 `InspeccionInsumoDetalleDto` o 404. |
| `POST /` | `GuardarInspeccionInsumoRequestDto` | 201 `{ id, numero }` con `Location`, 400 o 409. |
| `PUT /{id:int}` | `GuardarInspeccionInsumoRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/dictaminar` | `DictaminarInspeccionInsumoRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/anular` | `AnularInspeccionInsumoRequestDto` | 200 `{ id }`, 400, 404 o 409. |

- `PaginaInspeccionesInsumosDto { items: InspeccionInsumoListadoDto[], total, pagina, tamanoPagina }`
- `InspeccionInsumoListadoDto { id, numero, productoId, productoNombre, proveedorId, proveedorRazonSocial, lote, fechaInspeccion, cantidadRecibida, unidadMedida, estado, porcentajeDefectos }`: `porcentajeDefectos` = `CantidadDefectuosa / CantidadMuestra × 100` con 2 decimales, o `null` si aún no se dictaminó.
- `InspeccionInsumoDetalleDto` = listado + `{ cantidadMuestra, toleranciaDefectosPorcentaje, inspector, cantidadDefectuosa, fechaDictamen, observaciones, motivoAnulacion, superaTolerancia }`: `superaTolerancia` es `null` sin dictamen.
- `ResumenInspeccionesInsumosDto { totalInspecciones, porEstado: { estado, total }[], tasaRechazoPorcentaje, porProveedor: { proveedorId, proveedorRazonSocial, aprobadas, rechazadas, tasaRechazoPorcentaje }[] }`: la tasa = rechazadas / (aprobadas + rechazadas) × 100 con 2 decimales, 0 si no hay dictámenes. `porEstado` sigue el orden del ciclo y solo incluye estados con filas. `porProveedor` solo incluye proveedores con dictámenes y se ordena por tasa descendente y luego por razón social. Se agrega en la consulta, sin `ToListAsync` previo.
- `GuardarInspeccionInsumoRequestDto { productoId, proveedorId, lote, fechaInspeccion, cantidadRecibida, cantidadMuestra, unidadMedida, toleranciaDefectosPorcentaje, inspector }`
- `DictaminarInspeccionInsumoRequestDto { resultado, cantidadDefectuosa, observaciones? }`: `resultado` es `Aprobada` o `Rechazada`.
- `AnularInspeccionInsumoRequestDto { motivo }`

El listado ordena por `FechaInspeccion` descendente y luego por `Id` descendente. La búsqueda no distingue mayúsculas. Los filtros de estado y el resultado aceptan sus valores sin distinguir mayúsculas. `FechaInspeccion` y los filtros se reciben como día (`2026-12-01`) en `DateOnly`, igual que la columna `date` generada. `FechaDictamen` se asigna con `DateTime.UtcNow`. «Hoy» es el día en hora de Perú: `DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5))` (Perú es UTC−5 y no tiene horario de verano; es la misma regla que `CalculadoraJornada.HoyLocal()` del módulo 63). Con UTC, a partir de las 19:00 en Perú ya sería el día siguiente y se aceptaría una fecha de inspección de mañana. Los textos opcionales en blanco se guardan como `null`.

### Reglas y mensajes literales

Orden de validación: el listado valida página, tamaño, búsqueda, producto, proveedor, estado y fechas. El alta y la edición validan los campos en el orden del DTO y después las referencias y el lote repetido. Las escrituras por ID verifican primero la existencia y luego el estado.

| Condición | HTTP | Mensaje |
|---|---|---|
| `pagina < 1` | 400 | La página debe ser mayor o igual a 1. |
| `tamanoPagina` fuera de 1–100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| Búsqueda de más de 50 caracteres | 400 | La búsqueda no puede superar 50 caracteres. |
| `productoId` no positivo en guardado o filtro | 400 | El identificador del producto debe ser mayor que 0. |
| `proveedorId` no positivo en guardado o filtro | 400 | El identificador del proveedor debe ser mayor que 0. |
| Estado de filtro inválido | 400 | El estado de la inspección de insumos no es válido. |
| `desde` posterior a `hasta` | 400 | La fecha desde no puede ser posterior a la fecha hasta. |
| Lote vacío tras recortar espacios | 400 | El lote es obligatorio. |
| Lote de más de 50 caracteres | 400 | El lote no puede superar 50 caracteres. |
| Fecha de inspección ausente | 400 | La fecha de inspección es obligatoria. |
| Fecha de inspección posterior a hoy (hora de Perú) | 400 | La fecha de inspección no puede ser posterior a hoy. |
| Cantidad recibida fuera de (0, 1000000000] | 400 | La cantidad recibida debe ser mayor que 0 y como máximo 1000000000. |
| Cantidad recibida con más de 3 decimales | 400 | La cantidad recibida admite como máximo 3 decimales. |
| Muestra ≤ 0 | 400 | La cantidad de la muestra debe ser mayor que 0. |
| Muestra con más de 3 decimales | 400 | La cantidad de la muestra admite como máximo 3 decimales. |
| Muestra mayor que lo recibido | 400 | La muestra no puede superar la cantidad recibida. |
| Unidad vacía tras recortar espacios | 400 | La unidad de medida es obligatoria. |
| Unidad de más de 20 caracteres | 400 | La unidad de medida no puede superar 20 caracteres. |
| Tolerancia fuera de 0–100 | 400 | La tolerancia de defectos debe estar entre 0 y 100. |
| Tolerancia con más de 2 decimales | 400 | La tolerancia de defectos admite como máximo 2 decimales. |
| Inspector vacío tras recortar espacios | 400 | El inspector es obligatorio. |
| Inspector de más de 100 caracteres | 400 | El inspector no puede superar 100 caracteres. |
| Producto inexistente | 400 | El producto {productoId} no existe. |
| Proveedor inexistente | 400 | El proveedor {proveedorId} no existe. |
| Proveedor inactivo | 409 | El proveedor {proveedorId} está inactivo. |
| Otra inspección no anulada del mismo producto, proveedor y lote | 409 | Ya existe una inspección del lote {lote} para ese producto y proveedor. |
| Inspección inexistente | 404 | La inspección de insumos {id} no existe. |
| Edición fuera de `Pendiente` | 409 | Solo se pueden editar inspecciones pendientes. |
| Dictaminar fuera de `Pendiente` | 409 | Solo se pueden dictaminar inspecciones pendientes. |
| Resultado distinto de `Aprobada` o `Rechazada` | 400 | El resultado debe ser Aprobada o Rechazada. |
| Cantidad defectuosa negativa | 400 | La cantidad defectuosa no puede ser negativa. |
| Cantidad defectuosa con más de 3 decimales | 400 | La cantidad defectuosa admite como máximo 3 decimales. |
| Cantidad defectuosa mayor que la muestra | 400 | La cantidad defectuosa no puede superar la muestra. |
| Observaciones de más de 500 caracteres | 400 | Las observaciones no pueden superar 500 caracteres. |
| Aprobar con defectos por encima de la tolerancia | 409 | La inspección supera la tolerancia de defectos ({porcentaje} % > {tolerancia} %) y solo puede rechazarse. |
| Anular fuera de `Pendiente` | 409 | Solo se pueden anular inspecciones pendientes. |
| Motivo vacío tras recortar espacios | 400 | El motivo de anulación es obligatorio. |
| Motivo de más de 250 caracteres | 400 | El motivo de anulación no puede superar 250 caracteres. |

En el mensaje de tolerancia, los porcentajes se escriben con 2 decimales y punto decimal (`12.50 % > 5.00 %`). La edición que mantiene su propio lote es válida. El lote repetido se comprueba en la consulta y también en la carrera: la `ViolacionRestriccionException` de unicidad sobre `IX_InspeccionesInsumos_ProductoId_ProveedorId_Lote` se traduce al mismo 409. La edición, el dictamen y la anulación de una misma inspección se serializan con un bloqueo de su fila.

## 5. Criterios de aceptación

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato y reglas | `./scripts/verify.sh` | en verde |
| A3 | Tests | `dotnet test --filter Modulo36_ControlCalidadInsumos` | Cada mensaje de §4 con su código, las tres transiciones, la paginación, los filtros y ningún caso inválido crea ni cambia filas. |
| A4 | Contrato API | `/openapi/v1.json` | Las siete operaciones presentes y autenticadas. |
| A5 | Comportamiento | Pruebas de integración | `porcentajeDefectos`, `superaTolerancia` y el resumen por proveedor exactos. Con defectos sobre la tolerancia, aprobar da 409 y rechazar funciona. Una inspección anulada libera su lote. |
| A6 | PostgreSQL | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` y `./scripts/smoke-postgres.sh` | `SMOKE OK`, incluidos el 409 de lote repetido por el índice parcial y dos dictámenes concurrentes de la misma inspección que dejan uno solo. |
| A7 | Seed | `scripts/seed-dev.sql` dos veces | Cuatro inspecciones (`Pendiente`, `Aprobada`, `Rechazada` y `Anulada`) sobre productos y proveedores del seed, sin duplicar filas. |

## 6. Checklist de ejecución

- [x] Aprobación de Lukas sobre el alcance, las decisiones de §2 y el contrato (PR #77, 2026-11-24).
- [x] Commit 1: script SQL 043, pluralizador y entidades generadas.
- [x] Commit 2: DTOs, validaciones y casos de uso.
- [x] Commit 3: controlador, pruebas, seed y smoke.
- [x] Verificar A1–A7 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-25 | A1 | 0 errores, 0 warnings | `dotnet build -warnaserror` dentro de `./scripts/verify.sh` |
| 2026-11-25 | A2 | En verde | `./scripts/verify.sh` |
| 2026-11-25 | A3 | 57 de 57 en verde: cada mensaje de §4, las tres transiciones, filtros, paginación y el cambio de día a las 19:00 en Perú | `dotnet test --filter Modulo36_ControlCalidadInsumos` (1669 en total) |
| 2026-11-25 | A4 | Las siete operaciones presentes y autenticadas | `openapi-inspecciones-insumos` en el smoke |
| 2026-11-25 | A5 | `porcentajeDefectos`, `superaTolerancia` y resumen por proveedor exactos; aprobar sobre la tolerancia da 409 y rechazar funciona; la anulación libera el lote | Pruebas de integración y smoke |
| 2026-11-25 | A6 | `SMOKE OK`, con el 409 del índice parcial en dos altas a la vez, dos dictámenes concurrentes que dejan uno solo y las fechas de hoy y mañana en Perú | `./scripts/smoke-postgres.sh` |
| 2026-11-25 | A7 | Cuatro inspecciones (`Pendiente`, `Aprobada`, `Rechazada` y `Anulada`) tras dos cargas seguidas | `scripts/seed-dev.sql` cargado dos veces con `psql` sobre la base local |
