# Especificación: 35 - Modulo10_DevolucionesComerciales (backend)

## 1. Objetivo
**Problema:** cuando un cliente devuelve productos de un pedido no hay registro: no se controla cuántas unidades se devuelven frente a las compradas ni cuánto hay que reembolsar.
**Resultado esperado:** entidades nuevas `DevolucionComercial` y `DetalleDevolucion` y `Modules/Modulo10_DevolucionesComerciales/` en `api/v1/devoluciones-comerciales`:
- devoluciones de productos de una orden del cliente (módulo 04), opcionalmente ligadas a un reclamo (módulo 09);
- ciclo Solicitada → Aprobada → Recibida, o Rechazada con motivo;
- **control de cantidades**: nunca más unidades de las compradas, descontando las ya devueltas;
- importe a reembolsar con el precio de la orden; resumen con tasa de devolución.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Notas de crédito (módulo 52), reembolsos reales o movimientos de stock (25–29).
- Cambios en las 4 entidades del enunciado (relaciones sin navegación inversa).
- El frontend (spec 36).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: las unidades «comprometidas» de una orden son las de devoluciones no rechazadas (solicitadas, aprobadas o recibidas); el reembolso solo cuenta las recibidas; un reclamo ligado debe ser de la misma orden.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{DevolucionComercial,DetalleDevolucion}.cs` | crear | FKs a `Cliente`, `OrdenCompra`, `Producto` (sin navegación inversa) y opcional a `ReclamoPostventa` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s, índice único en `Numero` y en (`DevolucionComercialId`, `ProductoId`), longitudes, FKs restrict, cascada a líneas |
| `src/TitanGlobal.Api/Migrations/*_DevolucionesComerciales*.cs` + snapshot | crear | `dotnet ef migrations add DevolucionesComerciales -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo10_DevolucionesComerciales/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/devoluciones-comerciales")]`; helper estático `ValidacionDevolucion`; `aprobar` y `recibir` con `[PermitirCuerpoVacio]` |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo10_DevolucionesComerciales/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidades.**
- `DevolucionComercial`: `Id`, `Numero` (máx. 20, único; `DEV-` + 6 dígitos del Id, atómico como en la spec 14), `Fecha` (UTC, del servidor), `ClienteId`, `OrdenCompraId`, `ReclamoPostventaId` (opcional), `Motivo` (máx. 20: `Defecto`, `ErrorPedido`, `Arrepentimiento`, `Otro`), `Estado` (máx. 20: `Solicitada`, `Aprobada`, `Recibida`, `Rechazada`), `Observaciones` (máx. 500), `MotivoRechazo` (máx. 500), `FechaAprobacion`, `FechaRecepcion`, `FechaRechazo` (UTC, opcionales), `Detalles`.
- `DetalleDevolucion`: `Id`, `DevolucionComercialId`, `ProductoId`, `Cantidad` (int).
- **Importe** de una línea = `Cantidad × PrecioUnitario` del producto en la orden (si el producto aparece en varias líneas de la orden, el precio medio ponderado redondeado a 2 decimales). Total = suma de importes.
- **Disponible** para un producto de una orden = unidades compradas en la orden − unidades en devoluciones no rechazadas de esa orden (excluida la propia devolución al editarla).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/devoluciones-comerciales?busqueda=&estado=&motivo=&clienteId=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaDevolucionesDto { items: DevolucionListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «El estado no es válido.»; «El motivo no es válido.» |
| GET | `api/v1/devoluciones-comerciales/{id:int}` | — | 200 `DevolucionDetalleDto` | 404 «La devolución {id} no existe.» |
| GET | `api/v1/devoluciones-comerciales/ordenes/{ordenId:int}/disponible` | — | 200 `ProductoDisponibleDto[]` | 404 «La orden de compra {ordenId} no existe.» |
| POST | `api/v1/devoluciones-comerciales` | `GuardarDevolucionRequestDto { clienteId, ordenCompraId, reclamoPostventaId?, motivo, observaciones?, lineas: [{ productoId, cantidad }] }` | 200 `{ id, numero }` | 400, 409 |
| PUT | `api/v1/devoluciones-comerciales/{id:int}` | igual (reemplaza las líneas) | 200 `{ id, numero }` | 400, 404, 409 |
| POST | `api/v1/devoluciones-comerciales/{id:int}/aprobar` | sin cuerpo | 200 `DevolucionDetalleDto` | 404, 409 |
| POST | `api/v1/devoluciones-comerciales/{id:int}/recibir` | sin cuerpo | 200 `DevolucionDetalleDto` | 404, 409 |
| POST | `api/v1/devoluciones-comerciales/{id:int}/rechazar` | `RechazarDevolucionRequestDto { motivo }` | 200 `DevolucionDetalleDto` | 400, 404, 409 |
| DELETE | `api/v1/devoluciones-comerciales/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/devoluciones-comerciales/resumen` | — | 200 `ResumenDevolucionesDto` | — |

**DTOs.**
- `DevolucionListadoDto { id, numero, fecha, clienteId, clienteNombre, ordenCompraId, numeroOrden, reclamoNumero, motivo, estado, unidades, total }`
- `DevolucionDetalleDto` = listado + `observaciones, motivoRechazo, fechaAprobacion, fechaRecepcion, fechaRechazo, lineas: LineaDevolucionDto[]` con `{ productoId, productoNombre, cantidad, precioUnitario, importe }`
- `ProductoDisponibleDto { productoId, productoNombre, comprado, devuelto, disponible, precioUnitario }` por nombre de producto (`devuelto` = unidades en devoluciones no rechazadas).
- `ResumenDevolucionesDto { total, solicitadas, aprobadas, recibidas, rechazadas, montoReembolsado, porMotivo: MotivoDevolucionesDto[], tasaDevolucion }`: `montoReembolsado` = total de las recibidas; `tasaDevolucion` = unidades recibidas / unidades vendidas en todas las órdenes × 100 (2 decimales; 0 si no hay ventas).
- `MotivoDevolucionesDto { motivo, devoluciones, unidades }` en el orden de la lista de motivos (solo los que tienen devoluciones).

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. Cliente inexistente → 400 «El cliente {clienteId} no existe.»
2. Orden inexistente → 400 «La orden de compra {ordenCompraId} no existe.»; de otro cliente → 400 «La orden {numeroOrden} no pertenece al cliente.»
3. `reclamoPostventaId` indicado: inexistente → 400 «El reclamo {id} no existe.»; de otra orden → 400 «El reclamo {numeroReclamo} no es de la orden {numeroOrden}.»
4. `motivo` válido sin distinguir mayúsculas y guardado canónico → si no, 400 «El motivo no es válido.»
5. `observaciones` en blanco → `null`; más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»
6. Líneas: vacía → «La devolución debe tener al menos una línea.»; `cantidad <= 0` → «La cantidad debe ser mayor que 0.»; producto repetido → «El producto {productoId} está repetido en la devolución.»; producto inexistente → «El producto {productoId} no existe.»; que no está en la orden → «El producto {productoId} no está en la orden {numeroOrden}.» (todas 400).
7. Cantidad mayor que la disponible → 409 «Solo quedan {disponible} unidades de {productoNombre} por devolver en la orden {numeroOrden}.»
8. Id inexistente → 404 «La devolución {id} no existe.»
9. Transiciones: editar y borrar solo `Solicitada`; `aprobar` solo `Solicitada`; `recibir` solo `Aprobada`; `rechazar` desde `Solicitada` o `Aprobada`. En otro caso → 409 «La devolución {numero} está {estado en minúsculas}.»
10. Rechazar: `motivo` vacío → 400 «El motivo de rechazo es obligatorio.»; más de 500 → «El motivo de rechazo no puede superar 500 caracteres.» Aprobar, recibir y rechazar guardan su fecha (UTC). Al rechazar, sus unidades vuelven a quedar disponibles.
- Filtros sin distinguir mayúsculas; `busqueda` por número de devolución, número de orden o cliente. Orden: `Fecha` desc, `Id` desc.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas, sin N+1 (el disponible de todas las líneas en un número fijo de consultas). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 4 devoluciones (`DEV-DEV-00N`) sobre órdenes del seed: una recibida ligada a un reclamo del seed de la spec 33 de la misma orden, una aprobada, una solicitada y una rechazada con motivo. Ninguna deja un producto con disponible negativo.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: un caso por cada mensaje de §4 con su código, literal y sin `Detail`; todas las transiciones permitidas y prohibidas; ningún caso inválido crea o cambia filas | pasan |
| A3 | Cantidades y consultas | tests: devolver exactamente lo disponible → 200 y una unidad más → 409; una rechazada libera unidades; al editar no cuenta la propia devolución; producto en dos líneas de la orden (precio medio); `disponible` exacto; importes, filtros, orden y resumen (reembolsado solo recibidas, tasa con 2 decimales) | pasan |
| A4 | Migración | `DevolucionesComerciales` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado, detalle, `ordenes/{id}/disponible`, resumen, POST válido → aprobar → recibir (200), recibir otra vez → 409, POST que excede lo disponible → 409, producto que no está en la orden → 400, rechazar sin motivo → 400 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 10 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`efcd203`, mismas líneas que el auditado `a2d2575`): backend 486/486, frontend 222/222 |
| 2026-10-15 | A2–A3 | ✅ | `DevolucionesComercialesApiTests` (64 tests): cada mensaje literal, todas las transiciones, exactamente lo disponible y una unidad más, rechazo que libera unidades, edición sin contar la propia, precio medio, resumen |
| 2026-10-15 | A4 | ✅ | migración `DevolucionesComerciales` (con `Designer` y snapshot) solo crea las 2 tablas; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: listado, detalle `DEV-DEV-001`, disponible de la orden 8, resumen (reembolsado 2500.00), superar lo disponible → 409, rechazar sin motivo → 400, POST → `DEV-000005` → aprobar → recibir (200), recibir otra vez → 409 |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | Pendiente menor | ℹ️ | el literal de la regla 7 dice «Solo quedan {n} unidades…» también con n = 1 («1 unidades»); corregir el plural en la spec, el backend y sus tests en un ciclo de acabado |
