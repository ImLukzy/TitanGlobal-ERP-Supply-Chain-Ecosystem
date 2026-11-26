# Módulo 26 – Kardex Valorizado (Backend)

**Estado:** pendiente de aprobación de Lukas.

## 1. Información general

- **Módulo:** 26 – KardexValorizado. **Nombre técnico:** `Modulo26_KardexValorizado`.
- **Responsable:** Maxi. **Revisor:** Lukas.
- **Ruta API:** `api/v1/kardex-valorizado`.
- **Dependencias:** `Producto` (21), `Almacen` (23) y, opcionalmente, `Lote` (25).

## 2. Objetivo

Registrar entradas y salidas de inventario por producto y almacén y consultar el kardex valorizado con PEPS o costo promedio.

## 3. Base de datos

Script `database/003-kardex-valorizado.sql`; la entidad `MovimientoKardex` se genera con scaffold.

**MovimientosKardex:** `Id`, `ProductoId`, `AlmacenId`, `LoteId` (opcional), `Fecha`, `Tipo` (`Entrada` o `Salida`), `Cantidad` (1 a 1 000 000), `CostoUnitario` (`numeric(18,4)`) y `Referencia` (100). Una entrada exige costo mayor que 0 y una salida no lleva costo. Los movimientos **no se modifican ni se borran**. Índice por `(ProductoId, AlmacenId, Fecha)`.

## 4. Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/movimientos` | Listado paginado de más nuevo a más antiguo. Filtros: `productoId`, `almacenId`, `tipo`, `fechaDesde`, `fechaHasta`, `pagina`, `tamanoPagina`. |
| POST | `/movimientos` | Registrar una entrada o salida. |
| GET | `/kardex` | Kardex de un producto en un almacén. Parámetros: `productoId`, `almacenId` y `metodo` (`Promedio` por defecto o `PEPS`). |
| GET | `/saldos` | Saldo por producto y almacén. Filtro opcional: `almacenId`. |
| GET | `/resumen` | Movimientos y unidades de entradas y salidas. |

## 5. Reglas y mensajes literales

**Listado (400):** «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», «La fecha inicial no puede ser posterior a la final.» y «El tipo de movimiento no es válido.»

**Registrar, en este orden:**
1. 400 «El producto {id} no existe.»
2. 400 «El almacén {id} no existe.»
3. 409 «El almacén {codigo} está inactivo.»
4. 400 «El tipo de movimiento no es válido.»
5. 400 «La cantidad del movimiento debe estar entre 1 y 1000000.»
6. 400 «Las salidas no llevan costo unitario.»; 400 «El costo unitario es obligatorio en las entradas.»; 400 «El costo unitario debe ser mayor que 0, no superar 1000000 y tener hasta 4 decimales.»
7. 400 «La fecha del movimiento no puede ser futura.» (si no se indica, es la fecha actual UTC)
8. 400 «La referencia no puede superar 100 caracteres.»
9. 400 «El lote {id} no existe.» y 409 «El lote {loteId} no pertenece al producto {productoId}.»
10. 409 «Stock insuficiente: el saldo es {saldo} y se pidieron {cantidad}.» (una salida no deja el saldo en negativo)

**Kardex (400):** «El producto {id} no existe.», «El almacén {id} no existe.» y «El método de valoración no es válido.»

## 6. Valorización

Los movimientos se recorren por fecha e `Id`.
- **Promedio móvil:** una entrada suma cantidad y valor; una salida se valora al costo promedio vigente (saldo de valor entre saldo de cantidad).
- **PEPS:** una salida consume las entradas más antiguas primero y su costo es el de esas capas.

Cada línea devuelve costo unitario (4 decimales), valor del movimiento (2), saldo en cantidad, saldo en valor (2) y costo promedio (4).

## 7. Pruebas, seed y smoke

`tests/TitanGlobal.Api.Tests/Modulo26_KardexValorizado/` con una prueba por mensaje y casos numéricos para Promedio y PEPS; seed y smoke en `scripts/`.

## 8. Criterios de aceptación

- [ ] Los endpoints y mensajes de §4 y §5 se cumplen.
- [ ] El cálculo de Promedio y PEPS coincide con los casos de prueba.
- [ ] Sin migraciones; `verify.sh` y `smoke-postgres.sh` en verde.

## 9. Restricciones

No modificar entidades de otros módulos ni permitir editar o borrar movimientos.