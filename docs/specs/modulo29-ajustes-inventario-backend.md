# Especificación: Modulo29_AjustesInventario (backend)

## 1. Objetivo
**Problema:** las diferencias de inventario físico, mermas, daños o caducidades requieren ajustar el stock en almacén dejando trazabilidad contable y sincronizando automáticamente el Kardex valorizado (módulo 26).
**Resultado esperado:** entidad `AjusteInventario` (`database/049-ajustes-inventario.sql`) y controlador `api/v1/ajustes-inventario` siguiendo la arquitectura en 4 capas (`Domain`, `Application`, `Infrastructure`, `Api`):
- listado paginado con filtros (`almacenId`, `productoId`, `tipo`, `motivo`, `busqueda`, `fechaDesde`, `fechaHasta`);
- consulta de detalle por identificador;
- registro de ajuste (`Incremento` / `Decremento`) con generación atómica de `MovimientoKardex` asociado (`Entrada` / `Salida`);
- resumen valorizado de incrementos, decrementos y saldo neto;
- catálogos iniciales de tipos y motivos válidos.

## 2. Archivos afectados
| Archivo | Acción |
|---|---|
| `database/049-ajustes-inventario.sql` | tabla `AjustesInventario`, restricciones `CHECK`, índice único normalizado y FK `AjusteInventarioId` en `MovimientosKardex` |
| `src/TitanGlobal.Domain/Models/AjusteInventario.cs` | generado mediante `./scripts/scaffold.sh` |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerado mediante `./scripts/scaffold.sh` |
| `src/TitanGlobal.Application/DTOs/Modulo29_AjustesInventario/` | DTOs de solicitud, ítem, página, resumen y valores iniciales |
| `src/TitanGlobal.Application/UseCases/Modulo29_AjustesInventario/` | validaciones de negocio y casos de uso ligados al Kardex |
| `src/TitanGlobal.Api/Controllers/Modulo29_AjustesInventario/` | controlador REST `AjustesInventarioController` |
| `tests/TitanGlobal.Api.Tests/Modulo29_AjustesInventario/` | pruebas de integración HTTP |
| `scripts/seed-dev.sql` | ajustes de ejemplo y sus movimientos de Kardex |
| `scripts/smoke-postgres.sh` | comprobaciones de extremo a extremo sobre PostgreSQL |

## 3. Contrato REST
| Verbo | Ruta | OK | Errores |
|---|---|---|---|
| GET | `api/v1/ajustes-inventario` | 200 `PaginaAjustesInventarioDto` | 400 si filtros o paginación son inválidos |
| GET | `api/v1/ajustes-inventario/{id:int}` | 200 `AjusteInventarioItemDto` | 404 si el ajuste no existe |
| POST | `api/v1/ajustes-inventario` | 201 `AjusteInventarioItemDto` | 400 validación, 409 código duplicado, almacén inactivo o stock insuficiente |
| GET | `api/v1/ajustes-inventario/resumen` | 200 `ResumenAjustesInventarioDto` | 400 si el rango de fechas es inválido |
| GET | `api/v1/ajustes-inventario/valores-iniciales` | 200 `ValoresInicialesAjustesDto` | — |

## 4. Reglas de negocio principales
1. `Codigo` obligatorio (máx. 20 caracteres), único sin distinguir mayúsculas ni espacios exteriores (`409`).
2. `AlmacenId` existente (`400`) y activo (`409`); `ProductoId` existente (`400`).
3. `Tipo` válido: `Incremento` o `Decremento` (`400`).
4. `Motivo` válido: `ConteoFisico`, `Merma`, `Dano`, `Caducidad` o `Correccion` (`400`).
5. `Cantidad` entre `1` y `1000000` (`400`). En `Decremento`, la cantidad no puede superar el saldo actual del producto en el almacén (`409`).
6. Valorización automática con Kardex:
   - `Incremento`: acepta `CostoUnitario` explícito (`> 0`, `<= 1000000`, hasta 4 decimales) o hereda el costo promedio actual del Kardex (o `Producto.Precio` si aún no existe saldo valorizado). Registra `MovimientoKardex` tipo `Entrada`.
   - `Decremento`: no acepta `CostoUnitario` explícito (`400`); calcula automáticamente el costo promedio móvil del Kardex en el momento del ajuste y registra `MovimientoKardex` tipo `Salida`.
7. `Responsable` obligatorio (máx. 100 caracteres); `Observaciones` opcional (máx. 500 caracteres).
