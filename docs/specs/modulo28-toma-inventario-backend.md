# Módulo 28 – Toma de Inventario (Backend)

**Estado:** listo para revisión de Lukas.

## 1. Información general

- **Módulo:** 28 – TomaInventario. **Nombre técnico:** `Modulo28_TomaInventario`.
- **Responsable:** Maxi. **Revisor:** Lukas.
- **Ruta API:** `api/v1/tomas-inventario`.
- **Dependencias:** `Producto`, `Almacen` (módulo 23) y saldos del kardex del módulo 26.

## 2. Objetivo

Registrar conteos físicos periódicos por almacén y producto, comparando la cantidad contada contra el stock registrado en el kardex (`StockSistema`) para calcular automáticamente las diferencias (`CantidadContada - StockSistema`) antes de generar ajustes de inventario.

## 3. Base de datos

Script `database/048-toma-inventario.sql` (registrado en `docs/equipo/relevo.md`):

- **`TomasInventario`:** `Id`, `Codigo` (único e índice único normalizado `lower(btrim("Codigo"))`), `AlmacenId` (FK `RESTRICT`), `FechaToma`, `Estado` (`Abierta`, `Cerrada`, `Anulada`), `Responsable`, `Observaciones`.
- **`DetallesTomaInventario`:** `Id`, `TomaInventarioId` (FK `CASCADE`), `ProductoId` (FK `RESTRICT`), `StockSistema`, `CantidadContada`, `Diferencia`, `Observacion`, con índice único `(TomaInventarioId, ProductoId)`.

Entidades y `TitanGlobalDbContext` generados con `./scripts/scaffold.sh`.

## 4. Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/tomas-inventario` | Listado paginado. Filtros: `almacenId`, `estado`, `busqueda`, `fechaDesde`, `fechaHasta`, `pagina`, `tamanoPagina`. |
| GET | `/api/v1/tomas-inventario/resumen` | Totales por estado (`Abierta`, `Cerrada`, `Anulada`) y conteo de líneas con diferencia. |
| GET | `/api/v1/tomas-inventario/{id}` | Detalle de la toma con sus líneas de conteo y diferencias. |
| POST | `/api/v1/tomas-inventario` | Registrar una toma de inventario en estado `Abierta`. |
| PUT | `/api/v1/tomas-inventario/{id}` | Actualizar responsable, observaciones y líneas de una toma `Abierta`. |
| POST | `/api/v1/tomas-inventario/{id}/cerrar` | Cerrar una toma `Abierta` (cambia a `Cerrada`, sin cuerpo). |
| POST | `/api/v1/tomas-inventario/{id}/anular` | Anular una toma `Abierta` (cambia a `Anulada`, sin cuerpo). |

## 5. Reglas y mensajes literales

- **Código (400 / 409):** obligatorio («El código de la toma de inventario es obligatorio.»), máximo 20 caracteres («El código de la toma de inventario no puede superar 20 caracteres.»), sin espacios («El código de la toma de inventario no puede contener espacios.») y único sin distinguir mayúsculas («Ya existe una toma de inventario con el código {codigo}.»).
- **Responsable y textos (400):** responsable obligatorio («El responsable de la toma de inventario es obligatorio.») y máximo 100 caracteres («El responsable no puede superar 100 caracteres.»); observaciones generales hasta 500 caracteres («Las observaciones no pueden superar 500 caracteres.»); observación por línea hasta 250 caracteres («La observación de la línea no puede superar 250 caracteres.»).
- **Fecha (400):** opcional (por defecto `DateTime.UtcNow`); no puede ser futura («La fecha de la toma de inventario no puede ser futura.»).
- **Almacén (400 / 409):** «El almacén {id} no existe.» y «El almacén {codigo} está inactivo.».
- **Líneas y productos (400):** al menos un producto («La toma de inventario debe incluir al menos un producto.»), identificador válido («El identificador del producto es obligatorio en cada línea.»), sin duplicados («El producto {id} está duplicado en la toma de inventario.»), producto existente («El producto {id} no existe.») y `CantidadContada` entre 0 y 1000000 («La cantidad contada debe estar entre 0 y 1000000.»).
- **Cálculo por línea:** `StockSistema` se obtiene del saldo promedio vigente del producto en el almacén dentro de `MovimientosKardex` (0 si no hay movimientos) y `Diferencia = CantidadContada - StockSistema`.
- **Ciclo de estado (404 / 409):** si no existe, 404 «La toma de inventario {id} no existe.». Solo las tomas en estado `Abierta` pueden editarse, cerrarse o anularse; en otro estado, 409 «La toma de inventario {codigo} está {estado} y no permite esta operación.».
- **Listado (400):** «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», «La fecha inicial no puede ser posterior a la final.» y «El estado de toma de inventario no es válido.».
