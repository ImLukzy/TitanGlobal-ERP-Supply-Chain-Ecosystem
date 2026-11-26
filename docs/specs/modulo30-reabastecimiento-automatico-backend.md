# Especificación: Modulo30_ReabastecimientoAutomatico (backend)

## 1. Objetivo
**Problema:** cuando el stock de un producto en un almacén cae en o por debajo de su punto de reorden, el área de abastecimiento necesita reglas configurables por producto y almacén que calculen automáticamente las cantidades sugeridas (respetando lotes mínimos de compra) y generen solicitudes de compra trazables.
**Resultado esperado:** entidades `ReglaReabastecimiento` y `EjecucionReabastecimiento` (`database/050-reabastecimiento-automatico.sql`) y controlador `api/v1/reabastecimiento-automatico` siguiendo la arquitectura en 4 capas (`Domain`, `Application`, `Infrastructure`, `Api`):
- gestión de reglas por producto y almacén (`GET`, `GET/{id}`, `POST`, `PUT/{id}`);
- evaluación automática de reglas activas (`POST /evaluar`) con cálculo de cantidad sugerida redondeada a múltiplos de `CantidadLoteCompra`;
- bandeja de sugerencias (`GET /sugerencias`) y conversión atómica en `SolicitudCompra` (`POST /sugerencias/{id}/generar-solicitud`) o descarte (`POST /sugerencias/{id}/descartar`);
- resumen ejecutivo (`GET /resumen`).

## 2. Archivos afectados
| Archivo | Acción |
|---|---|
| `database/050-reabastecimiento-automatico.sql` | tablas `ReglasReabastecimiento` y `EjecucionesReabastecimiento`, restricciones `CHECK` e índices únicos |
| `src/TitanGlobal.Domain/Models/ReglaReabastecimiento.cs` | generado mediante `./scripts/scaffold.sh` |
| `src/TitanGlobal.Domain/Models/EjecucionReabastecimiento.cs` | generado mediante `./scripts/scaffold.sh` |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerado mediante `./scripts/scaffold.sh` |
| `src/TitanGlobal.Application/DTOs/Modulo30_ReabastecimientoAutomatico/` | DTOs de reglas, evaluación, sugerencias y resumen |
| `src/TitanGlobal.Application/UseCases/Modulo30_ReabastecimientoAutomatico/` | validaciones de umbrales, motor de evaluación y generación de `SolicitudCompra` |
| `src/TitanGlobal.Api/Controllers/Modulo30_ReabastecimientoAutomatico/` | controlador REST `ReabastecimientoAutomaticoController` |
| `tests/TitanGlobal.Api.Tests/Modulo30_ReabastecimientoAutomatico/` | pruebas de integración HTTP |
| `scripts/seed-dev.sql` | reglas y sugerencias de ejemplo |
| `scripts/smoke-postgres.sh` | comprobaciones de extremo a extremo sobre PostgreSQL |

## 3. Contrato REST
| Verbo | Ruta | OK | Errores |
|---|---|---|---|
| GET | `api/v1/reabastecimiento-automatico/reglas` | 200 `PaginaReglasReabastecimientoDto` | 400 si filtros o paginación son inválidos |
| GET | `api/v1/reabastecimiento-automatico/reglas/{id:int}` | 200 `ReglaReabastecimientoDetalleDto` | 404 si la regla no existe |
| POST | `api/v1/reabastecimiento-automatico/reglas` | 201 `ReglaReabastecimientoDetalleDto` | 400 validación, 409 código/combinación duplicada o almacén/proveedor inactivo |
| PUT | `api/v1/reabastecimiento-automatico/reglas/{id:int}` | 200 `ReglaReabastecimientoListadoDto` | 400 validación, 404 regla inexistente, 409 proveedor inactivo |
| POST | `api/v1/reabastecimiento-automatico/evaluar` | 200 `ResultadoEvaluacionReabastecimientoDto` | — |
| GET | `api/v1/reabastecimiento-automatico/sugerencias` | 200 `PaginaSugerenciasReabastecimientoDto` | 400 si estado o paginación son inválidos |
| POST | `api/v1/reabastecimiento-automatico/sugerencias/{id:int}/generar-solicitud` | 200 `EjecucionReabastecimientoItemDto` | 404 sugerencia inexistente, 409 ya procesada |
| POST | `api/v1/reabastecimiento-automatico/sugerencias/{id:int}/descartar` | 200 `EjecucionReabastecimientoItemDto` | 400 motivo largo, 404 inexistente, 409 ya procesada |
| GET | `api/v1/reabastecimiento-automatico/resumen` | 200 `ResumenReabastecimientoDto` | — |

## 4. Reglas de negocio principales
1. `Codigo` obligatorio (máx. 20 caracteres, sin espacios), único sin distinguir mayúsculas ni espacios exteriores (`409`).
2. Combinación `(ProductoId, AlmacenId)` única por regla (`409`); `AlmacenId` y `ProveedorPreferidoId` (si se indica) deben existir (`400`) y estar activos (`409`).
3. Jerarquía de umbrales: `0 <= StockMinimo <= PuntoReorden < StockMaximo <= 1000000`, `0 <= StockActual <= 1000000` y `1 <= CantidadLoteCompra <= 1000000` (`400`).
4. Cantidad sugerida: cuando `Activa` y `StockActual <= PuntoReorden`, se calcula `max(1, StockMaximo - StockActual)` redondeado hacia arriba al múltiplo de `CantidadLoteCompra`.
5. Al evaluar (`POST /evaluar`), si ya existe una sugerencia en estado `Pendiente` para la regla, se actualiza en lugar de duplicarla.
6. Al generar solicitud (`POST /sugerencias/{id}/generar-solicitud`), se crea una `SolicitudCompra` en estado `Borrador` con correlativo `SC-REB-00001` y su `DetalleSolicitudCompra` valorizado con `Producto.Precio`, pasando la sugerencia a `SolicitudGenerada`.
