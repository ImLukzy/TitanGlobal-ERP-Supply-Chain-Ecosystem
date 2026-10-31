# Especificación: 25 - Modulo03_Cotizaciones (backend)

## 1. Objetivo
**Problema:** Comercial no puede enviar presupuestos a sus clientes. Hoy las órdenes de compra de clientes (módulo 04) se registran sin una cotización previa.
**Resultado esperado:** entidades nuevas `Cotizacion` y `DetalleCotizacion` y `Modules/Modulo03_Cotizaciones/` en `api/v1/cotizaciones`:
- listado con filtros, detalle con líneas y totales (con descuento por línea);
- alta y edición mientras está emitida;
- **aceptar** (genera la orden de compra del cliente del módulo 04) y rechazar con motivo;
- baja mientras está emitida; resumen.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Cambios en las 4 entidades del enunciado: aceptar **crea** una fila de `OrdenCompra` con sus `DetalleOrdenCompra` usando el modelo tal cual; la relación se guarda solo en `Cotizacion` (`OrdenCompraId`).
- Impuestos (módulo 75), listas de precios (06) y promociones (07).
- El frontend (spec 26).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: flujo Emitida → Aceptada | Rechazada; una cotización emitida con la fecha de validez pasada está **vencida** (dato calculado) y no se puede aceptar; aceptar es atómico (la cotización y la orden se guardan juntas o ninguna).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{Cotizacion,DetalleCotizacion}.cs` | crear | `Cotizacion` con FK a `Cliente` (sin navegación inversa en `Cliente`) y FK opcional a `OrdenCompra` (sin navegación inversa) |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s, índice único en `Numero`, longitudes, `decimal(18,2)` y `decimal(5,2)`, FKs restrict, cascada a líneas, `HasOne(...).WithMany()` para no tocar las entidades del enunciado |
| `src/TitanGlobal.Api/Migrations/*_Cotizaciones*.cs` + snapshot | crear | `dotnet ef migrations add Cotizaciones -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo03_Cotizaciones/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/cotizaciones")]`; helper estático `ValidacionCotizacion`; `aceptar` con `[PermitirCuerpoVacio]` |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo03_Cotizaciones/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidades.**
- `Cotizacion`: `Id`, `Numero` (máx. 20, único; `COT-` + 6 dígitos del Id, asignado de forma atómica como en la spec 14), `Fecha` (UTC, del servidor), `ClienteId`, `FechaValidez` (UTC), `Estado` (máx. 20: `Emitida`, `Aceptada`, `Rechazada`), `Observaciones` (máx. 500), `MotivoRechazo` (máx. 500), `FechaDecision` (UTC, opcional), `OrdenCompraId` (opcional), `Detalles`.
- `DetalleCotizacion`: `Id`, `CotizacionId`, `ProductoId`, `Cantidad` (int), `PrecioUnitario` (`decimal(18,2)`), `DescuentoPorcentaje` (`decimal(5,2)`, 0–100).
- Importes por línea: `subtotal = Cantidad × PrecioUnitario`, `descuento = round(subtotal × DescuentoPorcentaje / 100, 2)`, `importe = subtotal − descuento`. Total de la cotización = suma de importes.
- `vencida` = `Estado == Emitida` y `FechaValidez` anterior al día de hoy en UTC.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/cotizaciones?busqueda=&estado=&clienteId=&vencidas=&fechaDesde=&fechaHasta=&pagina=1&tamanoPagina=20` | opcionales salvo paginación; `vencidas=true` solo vencidas | 200 `PaginaCotizacionesDto { items: CotizacionListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «El estado no es válido.»; «La fecha desde no puede ser posterior a la fecha hasta.» |
| GET | `api/v1/cotizaciones/{id:int}` | — | 200 `CotizacionDetalleDto` | 404 «La cotización {id} no existe.» |
| POST | `api/v1/cotizaciones` | `GuardarCotizacionRequestDto { clienteId, fechaValidez, observaciones?, lineas: [{ productoId, cantidad, precioUnitario, descuentoPorcentaje }] }` | 200 `{ id, numero }` | 400 |
| PUT | `api/v1/cotizaciones/{id:int}` | igual (reemplaza las líneas) | 200 `{ id, numero }` | 400, 404, 409 |
| POST | `api/v1/cotizaciones/{id:int}/aceptar` | sin cuerpo | 200 `CotizacionDetalleDto` (con `ordenCompraId` y `ordenCompraNumero`) | 404, 409 |
| POST | `api/v1/cotizaciones/{id:int}/rechazar` | `RechazarCotizacionRequestDto { motivo }` | 200 `CotizacionDetalleDto` | 400, 404, 409 |
| DELETE | `api/v1/cotizaciones/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/cotizaciones/resumen` | — | 200 `ResumenCotizacionesDto` | — |

**DTOs.**
- `CotizacionListadoDto { id, numero, fecha, clienteId, clienteNombre, fechaValidez, estado, vencida, total, ordenCompraNumero }`
- `CotizacionDetalleDto { id, numero, fecha, clienteId, clienteNombre, fechaValidez, estado, vencida, observaciones, motivoRechazo, fechaDecision, ordenCompraId, ordenCompraNumero, subtotal, descuento, total, lineas: LineaCotizacionDto[] }`
- `LineaCotizacionDto { productoId, productoNombre, cantidad, precioUnitario, descuentoPorcentaje, subtotal, descuento, importe }`
- `ResumenCotizacionesDto { total, emitidas, vencidas, aceptadas, rechazadas, montoVigente, montoAceptado, tasaAceptacion, topClientes: ClienteCotizacionesDto[] }`: `montoVigente` = total de emitidas no vencidas; `tasaAceptacion` = aceptadas / (aceptadas + rechazadas) × 100 con 2 decimales (0 si no hay decididas).
- `ClienteCotizacionesDto { clienteId, nombre, cotizaciones, montoAceptado }`: top 5 por monto aceptado y después `clienteId`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. Cliente inexistente → 400 «El cliente {clienteId} no existe.»
2. `fechaValidez` (con `FechasUtc`) anterior al día de hoy en UTC al crear o editar → 400 «La fecha de validez no puede ser anterior a hoy.»
3. `observaciones` en blanco → `null`; más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»
4. Líneas: vacía → «La cotización debe tener al menos una línea.»; `cantidad <= 0` → «La cantidad debe ser mayor que 0.»; `precioUnitario <= 0` → «El precio unitario debe ser mayor que 0.»; descuento fuera de 0–100 → «El descuento debe estar entre 0 y 100.»; producto repetido → «El producto {productoId} está repetido en la cotización.»; producto inexistente → «El producto {productoId} no existe.» (todas 400).
5. Id inexistente → 404 «La cotización {id} no existe.»
6. PUT, aceptar, rechazar o DELETE sobre una cotización que no está `Emitida` → 409 «La cotización {numero} ya está {estado en minúsculas}.»
7. Aceptar una cotización vencida → 409 «La cotización {numero} está vencida.»
8. Rechazar: `motivo` vacío → 400 «El motivo de rechazo es obligatorio.»; más de 500 → «El motivo de rechazo no puede superar 500 caracteres.»
9. **Aceptar**, en una transacción (`TransactionScope`, como la spec 14): crea `OrdenCompra` con `NumeroOrden = "OC-" + Numero` (p. ej. `OC-COT-000007`), `Fecha` = ahora UTC, `ClienteId` de la cotización, `Total` = total de la cotización, `Descripcion` = «Generada desde la cotización {numero}»; una `DetalleOrdenCompra` por línea con `PrecioUnitario` = importe de la línea / cantidad (2 decimales); guarda `OrdenCompraId`, `Estado = Aceptada` y `FechaDecision`. Si `NumeroOrden` ya existe → 409 «Ya existe una orden de compra con el número {numeroOrden}.» y no se cambia nada.

Filtros como en la spec 14 (estado sin distinguir mayúsculas, búsqueda por número o nombre de cliente, fechas con `FechasUtc`, `fechaHasta` inclusiva). Orden: `Fecha` desc, `Id` desc.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas y proyectadas a DTO (sin N+1). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 4 cotizaciones (`COT-DEV-00N`, para no chocar con los números generados) sobre clientes y productos del seed: una emitida vigente (validez en 2027), una emitida vencida (validez pasada), una rechazada con motivo y una aceptada sin orden asociada (dato histórico: `OrdenCompraId` nulo).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; transiciones prohibidas → 409; aceptar vencida → 409; ningún caso inválido crea o cambia filas (tampoco órdenes) | pasan |
| A3 | Flujo y consultas | tests: importes con descuento exactos (redondeo a 2 decimales); alta con número `COT-` atómico; edición; aceptar crea la orden con número, total, líneas y precios correctos y la enlaza; rechazar; `vencida` y filtro `vencidas`; orden; resumen con tasa de aceptación exacta | pasan |
| A4 | Migración | `Cotizaciones` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado, `vencidas=true`, detalle, resumen, POST válido, aceptar el creado → 200 con `ordenCompraNumero` y la orden aparece en `api/v1/ordenes-compra` (consulta de jerarquía del cliente), aceptarlo otra vez → 409, aceptar la vencida → 409, rechazar sin motivo → 400 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 8 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases (incluido aceptar transaccional) y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`3cfef3b`, mismas líneas que el auditado `a27b367`): backend 272/272, frontend 185/185. `Cliente.cs`, `Producto.cs`, `OrdenCompra.cs` y `DetalleOrdenCompra.cs` sin cambios |
| 2026-10-15 | A2–A3 | ✅ | `CotizacionesApiTests`: mensajes literales, transiciones prohibidas, aceptar vencida → 409, importes con descuento y redondeo, número `COT-` atómico, aceptar crea y enlaza la orden con total y líneas correctos, número de orden repetido → 409 sin cambios, vencidas, filtros y resumen |
| 2026-10-15 | A4 | ✅ | migración `Cotizaciones` (con `Designer` y snapshot) solo crea las 2 tablas nuevas; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: emitidas, vencidas, detalle `COT-DEV-001`, resumen 4/2/1/1/1 con tasa 50, rechazar sin motivo → 400, aceptar vencida → 409, POST → `COT-000005`, aceptar → crea `OC-COT-000005` visible en la jerarquía del cliente, aceptarla otra vez → 409 |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción: resumen del seed con tasa 50.00 % y subtotal − descuento = total en cada detalle; filtros, «Solo vencidas», rango inválido sin petición; vencida sin «Aceptar» y rechazada sin acciones; alta con 3 líneas y descuentos con importes en vivo iguales a la API; validaciones locales; edición; aceptar con aviso «Se generará la orden de compra OC-COT-000005.», orden enlazada y visible en la jerarquía del cliente; aceptar de nuevo → 409 en el diálogo; rechazar con y sin motivo; eliminar con Escape; sin regresiones; 16 capturas sin recortes |
