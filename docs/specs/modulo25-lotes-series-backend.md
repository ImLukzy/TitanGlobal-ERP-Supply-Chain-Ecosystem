# Módulo 25 – Lotes y Series (Backend)

## 1. Información general

- **Módulo:** 25 – LotesSeries. **Nombre técnico:** `Modulo25_LotesSeries`.
- **Responsable:** Maxi. **Revisor:** Lukas.
- **Ruta API:** `api/v1/lotes-series`.
- **Arquitectura:** cuatro capas (`Domain`, `Application`, `Infrastructure`, `Api`), UseCases concretos, DTOs y mensajes literales.
- **Dependencia:** `Producto` (módulo 21), solo lectura mediante clave foránea; no se modifica la entidad.

## 2. Objetivo

Registrar los lotes de un producto (código, fechas de fabricación y vencimiento, cantidad y estado) y los números de serie, opcionalmente asociados a un lote, para dar trazabilidad.

## 3. Fuera de alcance

Movimientos y saldos de inventario (módulo 26), almacenes y zonas (23, 24, 27, 28), valorización y pantalla.

## 4. Modelo de datos

### 4.1. Lote

| Campo | Tipo | Regla |
|---|---|---|
| Id | int | Identificador. |
| ProductoId | int | Clave foránea a `Producto`, `Restrict`. |
| Codigo | string(30) | Obligatorio. Se normaliza a mayúsculas. |
| FechaFabricacion | DateTime? | Opcional. UTC (`timestamptz`). |
| FechaVencimiento | DateTime? | Opcional. UTC. |
| Cantidad | int | Entre 1 y 1 000 000. |
| Estado | string(20) | `Disponible`, `Cuarentena` o `Bloqueado`. |

Índice único `(ProductoId, Codigo)`.

### 4.2. Serie

| Campo | Tipo | Regla |
|---|---|---|
| Id | int | Identificador. |
| ProductoId | int | Clave foránea a `Producto`, `Restrict`. |
| LoteId | int? | Clave foránea opcional a `Lote`, `Restrict`. |
| NumeroSerie | string(50) | Obligatorio. Se normaliza a mayúsculas. |
| Estado | string(20) | `Disponible`, `Asignada` o `Baja`. |

Índice único `(ProductoId, NumeroSerie)`. Como los códigos se guardan en mayúsculas, la unicidad no distingue mayúsculas también en la base.

## 5. Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/lotes-series/lotes` | Listado paginado. Filtros: `productoId`, `estado`, `vencidos`, `busqueda` (código), `pagina`, `tamanoPagina`. |
| GET | `/api/v1/lotes-series/lotes/{id}` | Detalle de un lote con el total de series registradas. |
| POST | `/api/v1/lotes-series/lotes` | Registrar un lote. Estado inicial indicado en la solicitud. |
| PUT | `/api/v1/lotes-series/lotes/{id}` | Actualizar fechas, cantidad y estado. No cambia producto ni código. |
| GET | `/api/v1/lotes-series/lotes/resumen` | Totales por estado, vencidos y por vencer en 30 días. |
| GET | `/api/v1/lotes-series/series` | Listado paginado. Filtros: `productoId`, `loteId`, `estado`, `busqueda`, `pagina`, `tamanoPagina`. |
| POST | `/api/v1/lotes-series/series` | Registrar una serie. |
| PUT | `/api/v1/lotes-series/series/{id}/estado` | Cambiar el estado de una serie. |

## 6. Reglas y mensajes literales (en este orden)

Paginación (ambos listados), 400:
1. «La página debe ser mayor o igual a 1.»
2. «El tamaño de página debe estar entre 1 y 100.»

Crear y actualizar lote:
1. 404 al actualizar: «El lote {id} no existe.»
2. 400: «El producto {productoId} no existe.»
3. 400: «El código del lote es obligatorio.» y «El código del lote solo admite letras, números y guiones (1 a 30 caracteres).»
4. 400: «La cantidad del lote debe estar entre 1 y 1000000.»
5. 400: «El estado del lote no es válido.»
6. 400: «La fecha de vencimiento no puede ser anterior a la de fabricación.»
7. 409 al actualizar: «La cantidad del lote no puede ser menor que sus series registradas ({n}).»
8. 409 al crear: «Ya existe el lote {codigo} para el producto {productoId}.»

Fechas: se normalizan a UTC con `FechasUtc`; si el día es solo fecha, se conserva el día.

Crear serie:
1. 400: «El producto {productoId} no existe.»
2. 400: «El número de serie es obligatorio.» y «El número de serie solo admite letras, números y guiones (1 a 50 caracteres).»
3. 400: «El lote {loteId} no existe.»
4. 409: «El lote {loteId} no pertenece al producto {productoId}.»
5. 409: «El lote {codigo} está bloqueado.»
6. 409: «El lote {codigo} ya tiene registradas todas sus series ({cantidad}).»
7. 409: «Ya existe la serie {numero} para el producto {productoId}.»

Cambiar estado de una serie:
1. 404: «La serie {id} no existe.»
2. 400: «El estado de la serie no es válido.»
3. 409: «No se puede pasar la serie de {actual} a {nuevo}.»

Transiciones permitidas: `Disponible → Asignada`, `Disponible → Baja`, `Asignada → Disponible`, `Asignada → Baja`. `Baja` es final.

## 7. Resumen de lotes

`TotalLotes`, `Disponibles`, `EnCuarentena`, `Bloqueados`, `Vencidos` (vencimiento anterior a hoy UTC) y `PorVencer` (vence en los próximos 30 días).

## 8. Organización

- Domain: `Models/Lote.cs`, `Models/Serie.cs`.
- Application: `DTOs/Modulo25_LotesSeries/` (un archivo por DTO) y `UseCases/Modulo25_LotesSeries/` (casos de uso y `ValidacionLoteSerie.cs`).
- Infrastructure: `DbSet` y configuración en `TitanGlobalDbContext`; migración `Modulo25_LotesSeries` generada con `dotnet ef`.
- Api: `Controllers/Modulo25_LotesSeries/LotesSeriesController.cs` delgado.

## 9. Pruebas, seed y smoke

Una prueba de integración por cada mensaje literal de §6, por los filtros y por el resumen, comprobando que los casos inválidos no cambian datos. Datos de ejemplo idempotentes en `scripts/seed-dev.sql` y comprobaciones en `scripts/smoke-postgres.sh`.

## 10. Criterios de aceptación

- [ ] Los endpoints de §5 responden con los contratos y mensajes de §6.
- [ ] La migración se genera con `dotnet ef` y se aplica en PostgreSQL.
- [ ] Hay una prueba por cada mensaje literal.
- [ ] `./scripts/verify.sh` en verde y `./scripts/smoke-postgres.sh` termina en `SMOKE OK`.

## 11. Restricciones

No modificar `Producto`, middlewares ni módulos de otras personas. No implementar movimientos de stock.

## 12. Revisión

Lukas revisa esta spec antes de implementar. Cualquier cambio de alcance se aprueba antes de tocar el código.