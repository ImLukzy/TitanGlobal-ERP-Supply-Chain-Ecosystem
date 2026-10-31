# Especificación: 39 - Modulo08_ComisionesVendedores (backend)

## 1. Objetivo
**Problema:** no se sabe qué vendedor cerró cada pedido ni cuánto se le debe pagar de comisión.
**Resultado esperado:** entidades nuevas `Vendedor`, `AsignacionVenta` y `LiquidacionComision` y `Modules/Modulo08_ComisionesVendedores/` en `api/v1/comisiones-vendedores`:
- maestro de vendedores con su porcentaje de comisión;
- asignación de cada orden de compra (módulo 04) a un vendedor;
- comisiones del mes calculadas con las órdenes asignadas;
- liquidación mensual (se genera y después se marca como pagada).

Decisión de Lukas (2026-10-15): el módulo tiene su propio maestro de vendedores y una asignación de órdenes; más adelante se podrá enlazar un vendedor con un empleado (módulo 61).

## 2. Fuera de alcance
- Cambios en las 4 entidades del enunciado: la relación orden–vendedor vive solo en `AsignacionVenta`.
- Empleados (61), planillas (66) y pagos reales.
- El frontend (spec 40).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: una orden se asigna a un solo vendedor; la comisión de un mes se calcula sobre las órdenes asignadas cuya fecha cae en ese mes (UTC) con el porcentaje vigente del vendedor al liquidar; una liquidación guarda los importes y no cambia aunque luego cambien el porcentaje o las asignaciones; un mes pagado no se puede volver a liquidar ni modificar sus asignaciones.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{Vendedor,AsignacionVenta,LiquidacionComision}.cs` | crear | FK de `AsignacionVenta` a `OrdenCompra` con `HasOne(...).WithMany()` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s; índices únicos: `Vendedor.Codigo`, `AsignacionVenta.OrdenCompraId`, (`LiquidacionComision.VendedorId`, `Anio`, `Mes`); `decimal(18,2)` y `decimal(5,2)`; FKs restrict |
| `src/TitanGlobal.Api/Migrations/*_ComisionesVendedores*.cs` + snapshot | crear | `dotnet ef migrations add ComisionesVendedores -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo08_ComisionesVendedores/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/comisiones-vendedores")]`; helper estático `ValidacionComisiones`; `pagar` con `[PermitirCuerpoVacio]` |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo08_ComisionesVendedores/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidades.**
- `Vendedor`: `Id`, `Codigo` (requerido, máx. 20, mayúsculas, único), `Nombre` (requerido, máx. 100), `Email` (opcional, máx. 150), `PorcentajeComision` (`decimal(5,2)`, de 0 a 20), `Activo` (bool).
- `AsignacionVenta`: `Id`, `OrdenCompraId` (única), `VendedorId`, `FechaAsignacion` (UTC).
- `LiquidacionComision`: `Id`, `VendedorId`, `Anio`, `Mes`, `Ordenes` (int), `Ventas` (`decimal(18,2)`), `Porcentaje` (`decimal(5,2)`), `Comision` (`decimal(18,2)` = round(Ventas × Porcentaje / 100, 2)), `Estado` (máx. 20: `Calculada`, `Pagada`), `FechaCalculo`, `FechaPago` (UTC, opcional).

**Mes de una orden:** año y mes de `OrdenCompra.Fecha` en UTC.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`. Rutas relativas a `api/v1/comisiones-vendedores`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `vendedores?busqueda=&activo=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaVendedoresDto { items: VendedorListadoDto[], total, pagina, tamanoPagina }` | 400 paginación |
| GET | `vendedores/{id:int}` | — | 200 `VendedorDetalleDto` | 404 «El vendedor {id} no existe.» |
| POST | `vendedores` | `GuardarVendedorRequestDto { codigo, nombre, email?, porcentajeComision, activo }` | 200 `{ id }` | 400, 409 |
| PUT | `vendedores/{id:int}` | igual | 200 `{ id }` | 400, 404, 409 |
| DELETE | `vendedores/{id:int}` | — | 204 | 404, 409 |
| GET | `ordenes?vendedorId=&sinAsignar=&anio=&mes=&pagina=1&tamanoPagina=20` | órdenes de compra con su vendedor (o sin él) | 200 `PaginaOrdenesVendedorDto { items: OrdenVendedorDto[], total, pagina, tamanoPagina }` | 400 paginación; 400 mes fuera de 1–12 |
| PUT | `ordenes/{ordenCompraId:int}/vendedor` | `AsignarVendedorRequestDto { vendedorId }` (asigna o reasigna) | 200 `OrdenVendedorDto` | 400, 404, 409 |
| DELETE | `ordenes/{ordenCompraId:int}/vendedor` | — | 204 | 404, 409 |
| GET | `comisiones?anio=&mes=` | obligatorios | 200 `ComisionesMesDto` | 400 |
| POST | `liquidaciones` | `LiquidarMesRequestDto { anio, mes }` | 200 `ComisionesMesDto` | 400, 409 |
| POST | `liquidaciones/{id:int}/pagar` | sin cuerpo | 200 `LiquidacionDto` | 404, 409 |

**DTOs.**
- `VendedorListadoDto { id, codigo, nombre, porcentajeComision, activo, ordenesAsignadas }`; `VendedorDetalleDto` = listado + `email, ventasTotales` (suma de los totales de sus órdenes).
- `OrdenVendedorDto { ordenCompraId, numeroOrden, fecha, clienteNombre, total, vendedorId, vendedorCodigo, vendedorNombre }` (vendedor `null` si no tiene); orden por fecha desc e Id desc.
- `ComisionesMesDto { anio, mes, liquidado, filas: ComisionVendedorDto[], totalVentas, totalComision }`: una fila por vendedor con órdenes en el mes (activos o no). Si el mes no está liquidado, los importes se calculan con el porcentaje actual (`estado = "Pendiente"`); si lo está, salen de las liquidaciones guardadas.
- `ComisionVendedorDto { vendedorId, codigo, nombre, ordenes, ventas, porcentaje, comision, estado, liquidacionId }` (`estado` = `Pendiente`, `Calculada` o `Pagada`), por código.
- `LiquidacionDto { id, vendedorId, codigo, nombre, anio, mes, ordenes, ventas, porcentaje, comision, estado, fechaCalculo, fechaPago }`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código es obligatorio.»; no cumple `^[A-Z0-9-]{2,20}$` → 400 «El código solo admite letras, números y guiones (2 a 20 caracteres).»
2. `nombre` tras `Trim()`: vacío → 400 «El nombre del vendedor es obligatorio.»; más de 100 → 400 «El nombre del vendedor no puede superar 100 caracteres.»
3. `email` en blanco → `null`; más de 150 o sin formato `texto@texto.texto` → 400 «El email no tiene un formato válido.»
4. `porcentajeComision` fuera de 0–20 → 400 «La comisión debe estar entre 0 y 20 %.»
5. Código repetido → 409 «Ya existe un vendedor con el código {codigo}.» (también en la carrera, `23505`; PUT con su propio código válido).
6. Vendedor inexistente → 404 «El vendedor {id} no existe.»; borrar un vendedor con órdenes asignadas o liquidaciones → 409 «No se puede eliminar el vendedor {codigo} porque tiene ventas asignadas.»
7. Asignar: orden inexistente → 404 «La orden de compra {id} no existe.»; vendedor inexistente → 400 «El vendedor {id} no existe.»; vendedor inactivo → 400 «El vendedor {codigo} está inactivo.»; quitar la asignación de una orden sin vendedor → 404 «La orden {numeroOrden} no tiene vendedor.»
8. Asignar, reasignar o quitar el vendedor de una orden cuyo mes ya está liquidado (con cualquier liquidación, calculada o pagada) → 409 «El mes {mm}/{aaaa} ya está liquidado; no se pueden cambiar sus asignaciones.»
9. `anio` fuera de 2000–2100 → 400 «El año no es válido.»; `mes` fuera de 1–12 → 400 «El mes no es válido.»
10. Liquidar un mes que ya tiene liquidaciones → 409 «El mes {mm}/{aaaa} ya está liquidado.»; un mes sin órdenes asignadas → 400 «No hay ventas asignadas en {mm}/{aaaa}.» La liquidación crea una fila por vendedor con órdenes en el mes, todas en la misma operación.
11. Pagar: liquidación inexistente → 404 «La liquidación {id} no existe.»; ya pagada → 409 «La liquidación {id} ya está pagada.»; guarda `FechaPago`.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas, sin N+1 (comisiones del mes con un número fijo de consultas). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (`GroupBy`, `Sum`, filtro por fecha).

**Seed:** 3 vendedores (`V-ANA` 5 %, `V-LUIS` 3 %, `V-OLD` 4 % inactivo) y asignaciones de las órdenes del seed de enero a marzo de 2026 repartidas entre `V-ANA` y `V-LUIS` (algunas sin asignar); enero de 2026 liquidado y pagado, febrero liquidado sin pagar, marzo sin liquidar.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: un caso por cada mensaje de §4 con su código, literal y sin `Detail`; ningún caso inválido crea o cambia filas | pasan |
| A3 | Cálculo y consultas | tests: orden del último día del mes a las 23:59 UTC cuenta en ese mes y la del día siguiente no; comisión con redondeo a 2 decimales; mes no liquidado usa el porcentaje actual y el liquidado conserva los importes aunque luego cambie el porcentaje; reasignar dentro de un mes no liquidado mueve la venta; liquidar dos veces → 409; pagar y pagar otra vez → 409; filtros de órdenes (`sinAsignar`, mes) y totales exactos | pasan |
| A4 | Migración | `ComisionesVendedores` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: vendedores, órdenes sin asignar, comisiones de enero (pagadas) y de marzo (pendientes), asignar una orden de marzo → 200, asignar una de enero → 409, liquidar marzo → 200 y otra vez → 409, pagar una liquidación de marzo → 200, borrar `V-ANA` → 409 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 11 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`d823134`, mismas líneas que el auditado `7a591fe`): backend 523/523, frontend 222/222 |
| 2026-10-15 | A2–A3 | ✅ | `ComisionesVendedoresApiTests` (27 tests): cada mensaje literal, límites de mes en UTC, redondeo, mes pendiente con el porcentaje actual y liquidado con los importes guardados, reasignación, liquidar y pagar dos veces → 409 |
| 2026-10-15 | A4 | ✅ | migración `ComisionesVendedores` (con `Designer` y snapshot) solo crea las 3 tablas; `OrdenCompra` sin cambios; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: vendedores, órdenes sin asignar, comisiones de enero (pagadas) y marzo (pendientes), reasignar OC-004 → 200, cambiar una de enero → 409 «El mes 01/2026 ya está liquidado; …», liquidar marzo → 200 y otra vez → 409, pagar → 200, borrar V-ANA → 409 |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
