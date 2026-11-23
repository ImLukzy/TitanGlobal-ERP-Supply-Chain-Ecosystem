# Especificación: módulo 34 — Órdenes de Producción (backend)

## 1. Objetivo

**Problema:** Manufactura ya define qué materiales lleva un producto (módulo 31), cómo se fabrica (módulo 32) y dónde (módulo 33), pero no puede ordenar su fabricación ni seguir su avance.

**Resultado esperado:** el usuario registra una orden para fabricar una cantidad de un producto con la lista de materiales y la ruta activas, consulta los materiales y el tiempo que requiere, y la lleva por sus estados hasta terminarla o cancelarla.

Responsable: Antony Huayhua. Estado: propuesta pendiente de aprobación de Lukas.

**Dependencias:** usa las tablas `ListasMateriales` (PR #40, script 023) y `RutasProduccion` (PR #49, script 011). La implementación empieza cuando ambos PR estén integrados en `main`.

## 2. Fuera de alcance

- Las cuatro entidades base, los middlewares y el pipeline. Los scripts de `database/` ya fusionados.
- El consumo real de materiales y los movimientos de inventario (módulo 35), los controles de calidad (36 y 37), las mermas (38) y la programación de capacidad de los centros.
- Órdenes con varios productos, órdenes parciales o divididas, y costos.
- Cambios en los contratos de los módulos 31, 32 y 33.
- La pantalla React, que tendrá su propia spec.

**Decisiones de producto que requieren aprobación:**
1. Al crear la orden se toman la lista de materiales y la ruta **activas** del producto en ese momento. Quedan fijas en la orden aunque después se retiren.
2. La cantidad se mide en unidades enteras.
3. Los estados son `Planificada` → `Liberada` → `EnProceso` → `Terminada`, más `Cancelada` desde `Planificada` o `Liberada`. Una orden en proceso no se cancela: se termina con la cantidad realmente producida.
4. Solo se edita una orden `Planificada`. No hay borrado: se cancela, con motivo.

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/025-ordenes-produccion.sql` | crear | Tabla `OrdenesProduccion`. Número reservado en `docs/equipo/relevo.md` §8 (el 024 ya lo usa Leonel). |
| `docs/equipo/relevo.md` | modificar | Añadir solo la fila del 025 en la tabla §8. |
| `src/TitanGlobal.Domain/Models/OrdenProduccion.cs` y navegaciones en `Producto`, `ListaMateriales` y `RutaProduccion` | regenerar | Con `./scripts/scaffold.sh`, sin editar a mano. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | Con `./scripts/scaffold.sh`. |
| `src/TitanGlobal.Domain/Models/ValoresIniciales.cs` | modificar | `OrdenProduccion() { Estado = "Planificada"; }`. |
| `src/TitanGlobal.Application/DTOs/Modulo34_OrdenesProduccion/*.cs` | crear | DTOs de entrada y salida. |
| `src/TitanGlobal.Application/UseCases/Modulo34_OrdenesProduccion/*.cs` | crear | Casos de uso y `ValidacionOrdenProduccion`. |
| `src/TitanGlobal.Api/Controllers/Modulo34_OrdenesProduccion/OrdenesProduccionController.cs` | crear | Controlador delgado en `api/v1/ordenes-produccion`. |
| `tests/TitanGlobal.Api.Tests/Modulo34_OrdenesProduccion/OrdenesProduccionApiTests.cs` | crear | Pruebas de integración con InMemory. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | modificar | Datos de ejemplo y comprobaciones, al final. |

## 4. Diseño y lógica

### Datos

| Columna | Tipo y restricción |
|---|---|
| `Id` | `integer` identidad, clave primaria. |
| `Numero` | `varchar(20)`, único (`IX_OrdenesProduccion_Numero`). Formato `OP-000001` a partir del `Id`, como `CTR-000001` del módulo 62. |
| `ProductoId` | FK a `Productos`, `ON DELETE RESTRICT`, con índice. |
| `ListaMaterialesId` | FK a `ListasMateriales`, `ON DELETE RESTRICT`, con índice. |
| `RutaProduccionId` | FK a `RutasProduccion`, `ON DELETE RESTRICT`, con índice. |
| `CantidadPlanificada` | `integer`, `CHECK > 0`. |
| `CantidadProducida` | `integer` nulo, `CHECK >= 0`. Se informa al terminar. |
| `FechaInicioPlanificada` y `FechaFinPlanificada` | `date`. `CHECK` fin ≥ inicio. |
| `FechaInicioReal` y `FechaFinReal` | `timestamptz` nulo. Se asignan en UTC al iniciar y al terminar. |
| `Estado` | `varchar(10)`, `CHECK` en `Planificada`, `Liberada`, `EnProceso`, `Terminada` y `Cancelada`. Índice. |
| `MotivoCancelacion` | `varchar(250)` nulo. |

### Contrato HTTP

Base `api/v1/ordenes-produccion`. JWT obligatorio. JSON en camelCase. Errores `{ StatusCode, Message }`.

| Verbo y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /` | `busqueda?` (número), `productoId?`, `estado?`, `desde?`, `hasta?` (sobre `FechaInicioPlanificada`), `pagina=1`, `tamanoPagina=20` | 200 `PaginaOrdenesProduccionDto` o 400. |
| `GET /resumen` | — | 200 `ResumenOrdenesProduccionDto`. |
| `GET /{id:int}` | — | 200 `OrdenProduccionDetalleDto` o 404. |
| `POST /` | `CrearOrdenProduccionRequestDto` | 201 `{ id, numero }` con `Location`, 400 o 409. |
| `PUT /{id:int}` | `EditarOrdenProduccionRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/liberar` | sin cuerpo | 200 `{ id }`, 404 o 409. |
| `POST /{id:int}/iniciar` | sin cuerpo | 200 `{ id }`, 404 o 409. |
| `POST /{id:int}/terminar` | `TerminarOrdenProduccionRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/cancelar` | `CancelarOrdenProduccionRequestDto` | 200 `{ id }`, 400, 404 o 409. |

- `PaginaOrdenesProduccionDto { items: OrdenProduccionListadoDto[], total, pagina, tamanoPagina }`
- `OrdenProduccionListadoDto { id, numero, productoId, productoNombre, cantidadPlanificada, fechaInicioPlanificada, fechaFinPlanificada, estado }`
- `OrdenProduccionDetalleDto` = listado + `{ listaMaterialesId, listaMaterialesVersion, rutaProduccionId, rutaProduccionVersion, cantidadProducida, fechaInicioReal, fechaFinReal, motivoCancelacion, tiempoEstimadoMinutos, materiales: MaterialRequeridoDto[], operaciones: OperacionOrdenDto[] }`
- `MaterialRequeridoDto { componenteProductoId, componenteProductoNombre, cantidadRequerida, unidadMedida }`: cantidad de la lista × `cantidadPlanificada`, ordenado por componente.
- `OperacionOrdenDto { secuencia, nombre, centroTrabajoNombre, minutosEstimados }`: `minutosPreparacion + minutosPorUnidad × cantidadPlanificada`, ordenado por secuencia. `tiempoEstimadoMinutos` es la suma de todas.
- `ResumenOrdenesProduccionDto { totalOrdenes, porEstado: { estado, total }[], unidadesPlanificadasAbiertas, unidadesProducidas }`: abiertas = `Planificada`, `Liberada` y `EnProceso`. Se agrega en la consulta, sin `ToListAsync` previo.
- `CrearOrdenProduccionRequestDto { productoId, cantidadPlanificada, fechaInicioPlanificada, fechaFinPlanificada }`
- `EditarOrdenProduccionRequestDto { cantidadPlanificada, fechaInicioPlanificada, fechaFinPlanificada }`
- `TerminarOrdenProduccionRequestDto { cantidadProducida }` y `CancelarOrdenProduccionRequestDto { motivo }`

El listado ordena por `FechaInicioPlanificada` descendente y luego por `Id` descendente. La búsqueda por número no distingue mayúsculas. El filtro de estado acepta los cinco valores sin distinguir mayúsculas. Las fechas se reciben como día (`2026-12-01`) y se normalizan con `FechasUtc`.

### Reglas y mensajes literales

Orden de validación: el listado valida página, tamaño, producto, estado y fechas. Las escrituras por ID verifican primero la existencia y luego el estado. El alta valida los campos y después las referencias activas.

| Condición | HTTP | Mensaje |
|---|---|---|
| `pagina < 1` | 400 | La página debe ser mayor o igual a 1. |
| `tamanoPagina` fuera de 1–100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| `productoId` no positivo en alta o filtro | 400 | El identificador del producto debe ser mayor que 0. |
| Estado de filtro inválido | 400 | El estado de la orden de producción no es válido. |
| `desde` posterior a `hasta` | 400 | La fecha desde no puede ser posterior a la fecha hasta. |
| Búsqueda de más de 20 caracteres | 400 | La búsqueda no puede superar 20 caracteres. |
| Cantidad planificada fuera de 1–1000000 | 400 | La cantidad planificada debe estar entre 1 y 1000000. |
| Fecha de inicio o de fin ausente | 400 | Las fechas planificadas son obligatorias. |
| Fin anterior al inicio | 400 | La fecha de fin no puede ser anterior a la fecha de inicio. |
| Producto inexistente en el alta | 400 | El producto {productoId} no existe. |
| Producto sin lista de materiales activa | 409 | El producto {productoId} no tiene una lista de materiales activa. |
| Producto sin ruta de producción activa | 409 | El producto {productoId} no tiene una ruta de producción activa. |
| Orden inexistente | 404 | La orden de producción {id} no existe. |
| Edición fuera de `Planificada` | 409 | Solo se pueden editar órdenes de producción planificadas. |
| Liberar fuera de `Planificada` | 409 | Solo se pueden liberar órdenes de producción planificadas. |
| Iniciar fuera de `Liberada` | 409 | Solo se pueden iniciar órdenes de producción liberadas. |
| Terminar fuera de `EnProceso` | 409 | Solo se pueden terminar órdenes de producción en proceso. |
| Cantidad producida fuera de 0–1000000 | 400 | La cantidad producida debe estar entre 0 y 1000000. |
| Cancelar fuera de `Planificada` o `Liberada` | 409 | Solo se pueden cancelar órdenes de producción planificadas o liberadas. |
| Motivo vacío tras recortar espacios | 400 | El motivo de cancelación es obligatorio. |
| Motivo de más de 250 caracteres | 400 | El motivo de cancelación no puede superar 250 caracteres. |

Las transiciones y la edición de una misma orden se serializan con un bloqueo de su fila, como en los módulos 31 y 32. Una violación concurrente de la FK o de la unicidad del número se traduce con `ViolacionRestriccionException` al mensaje equivalente de la tabla.

Los módulos 31 y 32 no cambian su contrato. Una lista de materiales o ruta usada por una orden queda protegida por la FK. Como ninguno de los dos expone borrado, no hace falta traducir un nuevo 409.

## 5. Criterios de aceptación

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato y reglas | `./scripts/verify.sh` | en verde |
| A3 | Tests | `dotnet test --filter Modulo34_OrdenesProduccion` | Cada mensaje de §4, las cinco transiciones y la paginación. |
| A4 | Contrato API | `/openapi/v1.json` | Las nueve operaciones presentes y autenticadas. |
| A5 | Comportamiento | Pruebas de integración | El detalle calcula los materiales (lista × cantidad) y el tiempo estimado. La orden conserva su versión de lista y ruta aunque después se retiren. El resumen agrega por estado. |
| A6 | PostgreSQL | `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` y `./scripts/smoke-postgres.sh` | `SMOKE OK`, incluidas las fechas sin `Z` normalizadas y dos transiciones concurrentes de la misma orden que dejan un único cambio de estado. |
| A7 | Seed | `scripts/seed-dev.sql` dos veces | Tres órdenes del producto 1 (`Planificada`, `EnProceso` y `Terminada`) sin duplicar filas. |

## 6. Checklist de ejecución

- [ ] Aprobación de Lukas sobre el alcance, las decisiones de §2 y el contrato.
- [ ] PR #40 (31) y #49 (32) integrados en `main`, y rama actualizada.
- [ ] Commit 1: script SQL 025, fila en `relevo.md` y entidades generadas.
- [ ] Commit 2: DTOs, validaciones y casos de uso.
- [ ] Commit 3: controlador, pruebas, seed y smoke.
- [ ] Verificar A1–A7 y registrar los resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
