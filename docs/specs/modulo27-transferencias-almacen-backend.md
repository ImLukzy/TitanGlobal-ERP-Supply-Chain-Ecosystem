# Módulo 27 – Transferencias de Almacén (Backend)

**Estado:** pendiente de aprobación de Lukas. El diseño (una transferencia resuelta con dos movimientos del kardex) se acordó con Lukas en el chat.

## 1. Información general

- **Módulo:** 27 – TransferenciasAlmacen. **Nombre técnico:** `Modulo27_TransferenciasAlmacen`.
- **Responsable:** Maxi. **Revisor:** Lukas.
- **Ruta API:** `api/v1/transferencias-almacen`.
- **Dependencias:** `Producto`, `Almacen` y el kardex del módulo 26.

## 2. Objetivo

Trasladar unidades de un producto de un almacén a otro. Cada transferencia se ejecuta al instante y registra, en una sola transacción, la transferencia, una salida en el origen y una entrada en el destino al costo promedio del origen.

## 3. Base de datos

Script `database/004-transferencias-almacen.sql`: tabla `TransferenciasAlmacen` (`Id`, `ProductoId`, `AlmacenOrigenId`, `AlmacenDestinoId`, `Cantidad`, `CostoUnitario`, `Fecha`, `Observaciones`), con origen distinto de destino, y la columna `MovimientosKardex.TransferenciaId` (FK). Entidades generadas con scaffold. Las transferencias no se editan ni se borran.

## 4. Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/transferencias-almacen` | Listado paginado. Filtros: `productoId`, `almacenOrigenId`, `almacenDestinoId`, `fechaDesde`, `fechaHasta`, `pagina`, `tamanoPagina`. |
| GET | `/api/v1/transferencias-almacen/{id}` | Detalle con los ids de la salida y la entrada. |
| POST | `/api/v1/transferencias-almacen` | Registrar una transferencia (la fecha es siempre la actual). |
| GET | `/api/v1/transferencias-almacen/resumen` | Total de transferencias, unidades y valor transferido. |

## 5. Reglas y mensajes literales

**Registrar, en este orden:**
1. 400 «El producto {id} no existe.»
2. 400 «El almacén {id} no existe.» (origen y luego destino)
3. 400 «El almacén de origen y el de destino deben ser distintos.»
4. 409 «El almacén {codigo} está inactivo.» (origen y luego destino)
5. 400 «La cantidad de la transferencia debe estar entre 1 y 1000000.»
6. 400 «Las observaciones no pueden superar 500 caracteres.»
7. 409 «Stock insuficiente: el saldo es {saldo} y se pidieron {cantidad}.»

El costo unitario es el **costo promedio móvil** del producto en el origen al momento de transferir. La salida no lleva costo y la entrada lo conserva. Los dos movimientos quedan ligados a la transferencia por `TransferenciaId`.

**Detalle:** 404 «La transferencia {id} no existe.»

**Listado (400):** «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.» y «La fecha inicial no puede ser posterior a la final.»

## 6. Pruebas, seed y smoke

`tests/TitanGlobal.Api.Tests/Modulo27_TransferenciasAlmacen/` con una prueba por mensaje, una que comprueba el costo promedio y el kardex de ambos almacenes, y otra que comprueba que los casos inválidos no cambian datos. Seed y smoke en `scripts/`.

## 7. Criterios de aceptación

- [ ] Una transferencia crea la transferencia y exactamente dos movimientos ligados, de forma atómica.
- [ ] Los mensajes de §5 se cumplen.
- [ ] Sin migraciones; `verify.sh` y `smoke-postgres.sh` en verde.

## 8. Restricciones

No modificar entidades ni scripts de otros módulos; la columna `TransferenciaId` se añade con un script nuevo (004) y no editando el 003.