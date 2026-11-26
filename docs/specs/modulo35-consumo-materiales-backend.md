# Especificación: módulo 35 — Consumo de Materiales (backend)

## 1. Objetivo

**Problema:** una orden de producción (módulo 34) calcula los materiales que requiere, pero no hay dónde registrar lo que realmente se consume ni compararlo con lo planificado.

**Resultado esperado:** el usuario registra los consumos de cada componente de la lista de materiales en una orden en proceso, anula los registros equivocados con un motivo y consulta, por orden, el balance entre lo requerido, lo consumido y lo pendiente, con su desviación.

Responsable: Antony Huayhua. Estado: aprobada por Lukas (PR #79, fusionado el 2026-11-25).

**Dependencias:** usa `OrdenesProduccion` (módulo 34, script 025) y `DetallesListaMaterial` (módulo 31, PR #40, script 023). Actualizado el 2026-11-26: el 31 ya está en `main`; el 34 aún no, así que la rama parte de `antony/modulo34-ordenes-produccion` y se actualiza con `main` cuando el 34 se integre.

## 2. Fuera de alcance

- Las cuatro entidades base, los middlewares y el pipeline. Los scripts de `database/` ya fusionados.
- Los movimientos de inventario, el stock por almacén y el kardex (módulos 25 a 27). Este módulo registra el consumo, no descuenta existencias.
- Las mermas (módulo 38), los costos y la valorización.
- Consumir componentes que no están en la lista de materiales de la orden (sustitutos).
- Cambios en los contratos de los módulos 31 y 34.
- La pantalla React, que tiene su propia spec (`modulo35-consumo-materiales-frontend.md`).

**Decisiones de producto que requieren aprobación:**
1. Solo se registran consumos en órdenes `EnProceso`, y solo de componentes de la lista de materiales que la orden tiene fija.
2. La unidad de medida no se escribe: se copia de la línea de la lista de materiales.
3. Se permite consumir más de lo requerido. El exceso no se bloquea: se muestra como desviación positiva en el balance.
4. Los consumos no se editan ni se borran. Un error se corrige anulando el registro (con motivo) y registrando otro. Solo se anula mientras la orden siga `EnProceso`.

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/042-consumo-materiales.sql` | crear | Tabla `ConsumosMateriales`. Número reservado en `docs/equipo/relevo.md` §8. |
| `docs/equipo/relevo.md` | modificar | Añadir solo la fila del 042 en la tabla §8. |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | Entrada `{ "ConsumosMateriales", "ConsumoMaterial" }`, porque las dos palabras van en plural. |
| `src/TitanGlobal.Domain/Models/ConsumoMaterial.cs` y navegaciones en `OrdenProduccion` y `Producto` | regenerar | Con `./scripts/scaffold.sh`, sin editar a mano. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | Con `./scripts/scaffold.sh`. |
| `src/TitanGlobal.Domain/Models/ValoresIniciales.cs` | modificar | `ConsumoMaterial() { Estado = "Registrado"; }`. |
| `src/TitanGlobal.Application/DTOs/Modulo35_ConsumoMateriales/*.cs` | crear | DTOs de entrada y salida. |
| `src/TitanGlobal.Application/UseCases/Modulo35_ConsumoMateriales/*.cs` | crear | Casos de uso y `ValidacionConsumoMaterial`. |
| `src/TitanGlobal.Api/Controllers/Modulo35_ConsumoMateriales/ConsumosMaterialesController.cs` | crear | Controlador delgado en `api/v1/consumos-materiales`. |
| `tests/TitanGlobal.Api.Tests/Modulo35_ConsumoMateriales/ConsumosMaterialesApiTests.cs` | crear | Pruebas de integración con InMemory. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | modificar | Datos de ejemplo y comprobaciones, al final. |

## 4. Diseño y lógica

### Datos

| Columna | Tipo y restricción |
|---|---|
| `Id` | `integer` identidad, clave primaria. |
| `Numero` | `varchar(20)`, único (`IX_ConsumosMateriales_Numero`). Formato `CM-000001` a partir del `Id`, como `CTR-000001` del módulo 62. |
| `OrdenProduccionId` | FK a `OrdenesProduccion`, `ON DELETE RESTRICT`, con índice. |
| `ComponenteProductoId` | FK a `Productos`, `ON DELETE RESTRICT`, con índice. |
| `Cantidad` | `numeric(18,6)`, `CHECK > 0`. Misma precisión que `DetallesListaMaterial.Cantidad`. |
| `UnidadMedida` | `varchar(20)`. Copiada de la línea de la lista de materiales. |
| `FechaRegistro` | `timestamptz`. Se asigna en UTC al registrar. Índice. |
| `Responsable` | `varchar(100)` nulo. |
| `Observaciones` | `varchar(250)` nulo. |
| `Estado` | `varchar(10)`, `CHECK` en `Registrado` y `Anulado`. Índice. |
| `FechaAnulacion` | `timestamptz` nulo. Se asigna en UTC al anular. |
| `MotivoAnulacion` | `varchar(250)` nulo. |

### Contrato HTTP

Base `api/v1/consumos-materiales`. JWT obligatorio. JSON en camelCase. Errores `{ StatusCode, Message }`.

| Verbo y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /` | `busqueda?` (número), `ordenProduccionId?`, `componenteProductoId?`, `estado?`, `desde?`, `hasta?` (día de `FechaRegistro` en hora de Perú), `pagina=1`, `tamanoPagina=20` | 200 `PaginaConsumosMaterialesDto` o 400. |
| `GET /resumen` | — | 200 `ResumenConsumosMaterialesDto`. |
| `GET /ordenes/{ordenProduccionId:int}/balance` | — | 200 `BalanceConsumoOrdenDto` o 404. |
| `GET /{id:int}` | — | 200 `ConsumoMaterialDetalleDto` o 404. |
| `POST /` | `RegistrarConsumoMaterialRequestDto` | 201 `{ id, numero }` con `Location`, 400 o 409. |
| `POST /{id:int}/anular` | `AnularConsumoMaterialRequestDto` | 200 `{ id }`, 400, 404 o 409. |

- `PaginaConsumosMaterialesDto { items: ConsumoMaterialListadoDto[], total, pagina, tamanoPagina }`
- `ConsumoMaterialListadoDto { id, numero, ordenProduccionId, ordenProduccionNumero, componenteProductoId, componenteProductoNombre, cantidad, unidadMedida, fechaRegistro, estado }`
- `ConsumoMaterialDetalleDto` = listado + `{ productoFabricadoNombre, responsable, observaciones, fechaAnulacion, motivoAnulacion }`
- `BalanceConsumoOrdenDto { ordenProduccionId, ordenProduccionNumero, productoNombre, cantidadPlanificada, estadoOrden, lineas: LineaBalanceConsumoDto[] }`
- `LineaBalanceConsumoDto { componenteProductoId, componenteProductoNombre, unidadMedida, cantidadRequerida, cantidadConsumida, cantidadPendiente, desviacionPorcentaje }`: requerida = cantidad de la lista × `cantidadPlanificada`; consumida = suma de los `Registrado`; pendiente = `max(requerida − consumida, 0)`; desviación = `(consumida − requerida) / requerida × 100`, redondeada a 2 decimales. Ordenado por nombre del componente.
- `ResumenConsumosMaterialesDto { totalRegistrados, totalAnulados, ordenesConConsumo, consumosUltimos7Dias }`: los dos últimos cuentan solo `Registrado`; «últimos 7 días» = día de `FechaRegistro` en hora de Perú de hoy − 6 a hoy, con «hoy» también en hora de Perú. Se agrega en la consulta, sin `ToListAsync` previo.
- `RegistrarConsumoMaterialRequestDto { ordenProduccionId, componenteProductoId, cantidad, responsable?, observaciones? }`
- `AnularConsumoMaterialRequestDto { motivo }`

El listado ordena por `FechaRegistro` descendente y luego por `Id` descendente. La búsqueda no distingue mayúsculas. El filtro de estado acepta los dos valores sin distinguir mayúsculas. Los filtros `desde` y `hasta` se reciben como día (`2026-12-01`) en `DateOnly` y comparan el día de `FechaRegistro` en hora de Perú. Las fechas se asignan con `DateTime.UtcNow`. «Hoy» es el día en hora de Perú: `DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5))` (Perú es UTC−5 y no tiene horario de verano; es la misma regla que `CalculadoraJornada.HoyLocal()` del módulo 63 y que los módulos 36, 37 y 39). Con UTC, un consumo registrado a partir de las 19:00 en Perú contaría como del día siguiente. Para que la consulta use el índice de `FechaRegistro`, un día D de Perú se filtra como el rango UTC `[D 05:00, D + 1 05:00)`: `desde` toma `desde 05:00 UTC` y `hasta` toma `hasta + 1 05:00 UTC` como límite exclusivo. Los textos opcionales en blanco se guardan como `null`.

### Reglas y mensajes literales

Orden de validación: el listado valida página, tamaño, búsqueda, orden, componente, estado y fechas. El registro valida los campos en el orden del DTO, después la orden (existencia y estado) y por último que el componente esté en su lista. La anulación verifica la existencia, el estado del consumo, el estado de la orden y el motivo.

| Condición | HTTP | Mensaje |
|---|---|---|
| `pagina < 1` | 400 | La página debe ser mayor o igual a 1. |
| `tamanoPagina` fuera de 1–100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| Búsqueda de más de 20 caracteres | 400 | La búsqueda no puede superar 20 caracteres. |
| `ordenProduccionId` no positivo en registro o filtro | 400 | El identificador de la orden de producción debe ser mayor que 0. |
| `componenteProductoId` no positivo en registro o filtro | 400 | El identificador del componente debe ser mayor que 0. |
| Estado de filtro inválido | 400 | El estado del consumo de materiales no es válido. |
| `desde` posterior a `hasta` | 400 | La fecha desde no puede ser posterior a la fecha hasta. |
| Cantidad fuera de (0, 1000000] | 400 | La cantidad consumida debe ser mayor que 0 y como máximo 1000000. |
| Cantidad con más de 6 decimales | 400 | La cantidad consumida admite como máximo 6 decimales. |
| Responsable de más de 100 caracteres | 400 | El responsable no puede superar 100 caracteres. |
| Observaciones de más de 250 caracteres | 400 | Las observaciones no pueden superar 250 caracteres. |
| Orden inexistente en el registro | 400 | La orden de producción {ordenProduccionId} no existe. |
| Orden fuera de `EnProceso` en el registro | 409 | Solo se pueden registrar consumos en órdenes de producción en proceso. |
| Componente fuera de la lista de materiales de la orden | 409 | El producto {componenteProductoId} no forma parte de la lista de materiales de la orden {numero}. |
| Orden inexistente en el balance | 404 | La orden de producción {ordenProduccionId} no existe. |
| Consumo inexistente | 404 | El consumo de materiales {id} no existe. |
| Anular un consumo ya `Anulado` | 409 | El consumo de materiales {numero} ya está anulado. |
| Anular con la orden fuera de `EnProceso` | 409 | Solo se pueden anular consumos de órdenes de producción en proceso. |
| Motivo vacío tras recortar espacios | 400 | El motivo de anulación es obligatorio. |
| Motivo de más de 250 caracteres | 400 | El motivo de anulación no puede superar 250 caracteres. |

El registro y la anulación bloquean la fila de la orden de producción, igual que las transiciones del módulo 34, para que no se registre un consumo mientras la orden pasa a `Terminada`. Una violación concurrente de una FK o de la unicidad del número se traduce con `ViolacionRestriccionException` al mensaje equivalente de la tabla.

El módulo 34 no cambia su contrato: las órdenes no se borran, así que la FK de consumos no necesita un nuevo 409. El borrado de productos del módulo 21 ya traduce cualquier FK a 409.

## 5. Criterios de aceptación

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato y reglas | `./scripts/verify.sh` | en verde |
| A3 | Tests | `dotnet test --filter Modulo35_ConsumoMateriales` | Cada mensaje de §4 con su código, la paginación, los filtros y ningún caso inválido crea ni cambia filas. |
| A4 | Contrato API | `/openapi/v1.json` | Las seis operaciones presentes y autenticadas. |
| A5 | Comportamiento | Pruebas de integración | El balance calcula requerida, consumida, pendiente y desviación exactas, y no suma los anulados. La unidad se copia de la lista. Un consumo mayor que lo requerido se acepta y da desviación positiva. Un consumo registrado a las 19:00 en Perú (00:00 UTC del día siguiente) cuenta para ese día en los filtros `desde` y `hasta` y en «últimos 7 días». |
| A6 | PostgreSQL | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` y `./scripts/smoke-postgres.sh` | `SMOKE OK`, incluidas una cantidad con 6 decimales guardada sin redondeo y una cantidad con 7 rechazada con 400. |
| A7 | Seed | `scripts/seed-dev.sql` dos veces | Cuatro consumos (tres `Registrado` y uno `Anulado`) sobre la orden `EnProceso` del seed del 34, sin duplicar filas. |

## 6. Checklist de ejecución

- [x] Aprobación de Lukas sobre el alcance, las decisiones de §2 y el contrato (PR #79, 2026-11-25).
- [ ] Módulos 31 y 34 integrados en `main`, y rama actualizada (el 31 sí; el 34 está pendiente y la rama parte de la suya).
- [x] Commit 1: script SQL 042, pluralizador y entidades generadas (la fila del 042 en `relevo.md` ya estaba en `main`).
- [x] Commit 2: UseCase de consulta (listado, resumen, balance y detalle) y sus endpoints GET.
- [x] Commit 3: registro con bloqueo de la fila de la orden.
- [x] Commit 4: anulación de consumos.
- [x] Commit 5: seed y comprobaciones del smoke.
- [x] Verificar A1–A7 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-26 | A1 | 0 errores, 0 warnings | `dotnet build -warnaserror` dentro de `./scripts/verify.sh` |
| 2026-11-26 | A2 | En verde | `./scripts/verify.sh` |
| 2026-11-26 | A3 | 34 de 34 en verde: cada mensaje de §4, la paginación, los filtros y ningún caso inválido crea ni cambia filas | `dotnet test --filter Modulo35_ConsumoMateriales` (1844 en total) |
| 2026-11-26 | A4 | Las seis operaciones presentes y autenticadas | `openapi-consumos-materiales` en el smoke |
| 2026-11-26 | A5 | Balance exacto sin anulados, unidad copiada de la lista, exceso con desviación positiva y consumo de las 19:00 en Perú contado en su día | Pruebas de integración y smoke |
| 2026-11-26 | A6 | `SMOKE OK`, con 0.123456 guardado sin redondeo y 0.1234567 rechazado con 400 | `./scripts/smoke-postgres.sh` |
| 2026-11-26 | A7 | Cuatro consumos (tres `Registrado` y uno `Anulado`) de `OP-000002` tras dos cargas seguidas | `scripts/seed-dev.sql` cargado dos veces con `psql` sobre la base local |
