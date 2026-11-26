# Módulo 25 – Lotes y Series (Backend)

**Estado:** pendiente de aprobación de Lukas.

## 1. Información general

- **Módulo:** 25 – LotesSeries. **Nombre técnico:** `Modulo25_LotesSeries`.
- **Responsable:** Maxi. **Revisor:** Lukas.
- **Ruta API:** `api/v1/lotes-series`.
- **Arquitectura:** cuatro capas, UseCases concretos y base de datos primero (sin migraciones).
- **Dependencia:** `Producto` (módulo 21), solo lectura por clave foránea.

## 2. Objetivo

Registrar los lotes de un producto (código, fechas, cantidad y estado) y sus números de serie, opcionalmente ligados a un lote, para dar trazabilidad.

## 3. Fuera de alcance

Movimientos y saldos (módulo 26), transferencias (27) y pantalla.

## 4. Base de datos

Script `database/002-lotes-series.sql`. Las entidades `Lote` y `UnidadSerie` se generan con `./scripts/scaffold.sh`. La tabla de series se llama `UnidadesSerie` porque `Series` no tiene singular y el scaffold generaría una entidad con el mismo nombre que su colección.

- **Lotes:** `Id`, `ProductoId` (FK a `Productos`, `RESTRICT`), `Codigo` (30), `FechaFabricacion` y `FechaVencimiento` (`timestamptz`, opcionales), `Cantidad` (1 a 1 000 000) y `Estado` (`Disponible`, `Cuarentena` o `Bloqueado`). Índice único `(ProductoId, Codigo)`.
- **UnidadesSerie:** `Id`, `ProductoId` (FK), `LoteId` (FK opcional a `Lotes`), `NumeroSerie` (50) y `Estado` (`Disponible`, `Asignada` o `Baja`). Índice único `(ProductoId, NumeroSerie)`.

Los códigos y números de serie se normalizan a mayúsculas, por lo que la unicidad no distingue mayúsculas.

## 5. Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/lotes-series/lotes` | Listado paginado. Filtros: `productoId`, `estado`, `vencidos`, `busqueda`, `pagina`, `tamanoPagina`. |
| GET | `/api/v1/lotes-series/lotes/{id}` | Detalle con el total de series del lote. |
| POST | `/api/v1/lotes-series/lotes` | Registrar un lote. |
| PUT | `/api/v1/lotes-series/lotes/{id}` | Actualizar fechas, cantidad y estado. |
| GET | `/api/v1/lotes-series/lotes/resumen` | Totales por estado, vencidos y por vencer en 30 días. |
| GET | `/api/v1/lotes-series/series` | Listado paginado. Filtros: `productoId`, `loteId`, `estado`, `busqueda`, `pagina`, `tamanoPagina`. |
| POST | `/api/v1/lotes-series/series` | Registrar una serie. |
| PUT | `/api/v1/lotes-series/series/{id}/estado` | Cambiar el estado de una serie. |

## 6. Reglas y mensajes literales (en este orden)

Paginación en ambos listados (400): «La página debe ser mayor o igual a 1.» y «El tamaño de página debe estar entre 1 y 100.»

**Crear lote:**
1. 400 «El producto {id} no existe.»
2. 400 «El código del lote es obligatorio.» y «El código del lote solo admite letras, números y guiones (1 a 30 caracteres).»
3. 400 «La cantidad del lote debe estar entre 1 y 1000000.»
4. 400 «El estado del lote no es válido.»
5. 400 «La fecha de vencimiento no puede ser anterior a la de fabricación.»
6. 409 «Ya existe el lote {codigo} para el producto {productoId}.»

**Actualizar lote:** 404 «El lote {id} no existe.»; después los puntos 3, 4 y 5 anteriores; y 409 «La cantidad del lote no puede ser menor que sus series registradas ({n}).»

**Detalle:** 404 «El lote {id} no existe.»

**Crear serie:**
1. 400 «El producto {id} no existe.»
2. 400 «El número de serie es obligatorio.» y «El número de serie solo admite letras, números y guiones (1 a 50 caracteres).»
3. 400 «El lote {loteId} no existe.»
4. 409 «El lote {loteId} no pertenece al producto {productoId}.»
5. 409 «El lote {codigo} está bloqueado.»
6. 409 «El lote {codigo} ya tiene registradas todas sus series ({cantidad}).»
7. 409 «Ya existe la serie {numero} para el producto {productoId}.»

**Cambiar estado de serie:** 404 «La serie {id} no existe.»; 400 «El estado de la serie no es válido.»; 409 «No se puede pasar la serie de {actual} a {nuevo}.» Transiciones permitidas: `Disponible → Asignada`, `Disponible → Baja`, `Asignada → Disponible` y `Asignada → Baja`. `Baja` es final.

Las fechas se normalizan a UTC con `FechasUtc` y se conserva el día.

## 7. Resumen de lotes

`TotalLotes`, `Disponibles`, `EnCuarentena`, `Bloqueados`, `Vencidos` (vencimiento anterior a hoy UTC) y `PorVencer` (vence en los próximos 30 días).

## 8. Pruebas, seed y smoke

Pruebas de integración en `tests/TitanGlobal.Api.Tests/Modulo25_LotesSeries/` (`LotesApiTests` y `SeriesApiTests`), una por cada mensaje literal de §6. Datos de ejemplo idempotentes en `scripts/seed-dev.sql` y comprobaciones en `scripts/smoke-postgres.sh`.

## 9. Criterios de aceptación

- [ ] Los endpoints de §5 responden con los contratos y mensajes de §6.
- [ ] Las tablas se crean con `database/002-lotes-series.sql` y las entidades con scaffold, sin migraciones.
- [ ] Hay una prueba por cada mensaje literal.
- [ ] `./scripts/verify.sh` en verde y `./scripts/smoke-postgres.sh` termina en `SMOKE OK`.

## 10. Restricciones

No modificar `Producto`, middlewares ni módulos de otras personas, y no implementar movimientos de inventario.