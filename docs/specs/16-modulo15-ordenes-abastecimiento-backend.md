# Especificación: 16 - Modulo15_OrdenesAbastecimiento (backend)

## 1. Objetivo
**Problema:** una solicitud de compra aprobada (spec 14) no se puede convertir en un pedido formal al proveedor, ni registrar su recepción.
**Resultado esperado:** entidades nuevas `OrdenAbastecimiento` y `DetalleOrdenAbastecimiento` y `Modules/Modulo15_OrdenesAbastecimiento/` en `api/v1/ordenes-abastecimiento`:
- listado con filtros y paginación;
- detalle;
- emisión manual o a partir de una solicitud aprobada;
- edición mientras está emitida;
- recepción y anulación;
- resumen.

Decisión del humano (2026-10-14): continuar con el módulo recomendado.

## 2. Fuera de alcance
- Cambios en las 4 entidades del enunciado (`Producto` sin navegación nueva: `HasOne(...).WithMany()`).
- Stock, kardex, recepciones parciales y facturas (módulos 19, 23–26, 51): la recepción solo cambia el estado.
- Borrado físico: una orden se anula, no se borra.
- El frontend (spec 17).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: flujo Emitida → Recibida | Anulada; una solicitud aprobada genera como máximo una orden no anulada; el proveedor de la orden es obligatorio y debe estar activo.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{OrdenAbastecimiento,DetalleOrdenAbastecimiento}.cs` | crear | `Proveedor` y `SolicitudCompra` ganan su colección de órdenes |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s, índice único en `Numero`, longitudes, `decimal(18,2)`, FKs restrict a `Proveedor`, `SolicitudCompra` y `Producto`, cascada a las líneas |
| `src/TitanGlobal.Api/Migrations/*_OrdenesAbastecimiento*.cs` + snapshot | crear | `dotnet ef migrations add OrdenesAbastecimiento -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo15_OrdenesAbastecimiento/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/ordenes-abastecimiento")]`; validación en helper estático `ValidacionOrdenAbastecimiento` (patrón de los módulos anteriores) |
| `scripts/seed-dev.sql` | modificar | 4 órdenes idempotentes (números `OA-DEV-00N`, como en la spec 14) |
| `scripts/smoke-postgres.sh` | modificar | rutas de §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo15_OrdenesAbastecimiento/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidades.**
- `OrdenAbastecimiento`: `Id`, `Numero` (máx. 20, único; `OA-` + 6 dígitos del Id, asignado de forma atómica como en la spec 14), `Fecha` (UTC, del servidor), `ProveedorId` (requerido), `SolicitudCompraId` (opcional), `Estado` (máx. 20: `Emitida`, `Recibida`, `Anulada`), `FechaEntregaEstimada` (UTC, requerida), `FechaRecepcion` (UTC, opcional), `Observaciones` (máx. 500, opcional), `MotivoAnulacion` (máx. 500, opcional), `Detalles`.
- `DetalleOrdenAbastecimiento`: `Id`, `OrdenAbastecimientoId`, `ProductoId`, `Cantidad` (int), `PrecioUnitario` (`decimal(18,2)`).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/ordenes-abastecimiento?busqueda=&estado=&proveedorId=&fechaDesde=&fechaHasta=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaOrdenesAbastecimientoDto { items: OrdenAbastecimientoListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «El estado no es válido.»; «La fecha desde no puede ser posterior a la fecha hasta.» |
| GET | `api/v1/ordenes-abastecimiento/{id:int}` | — | 200 `OrdenAbastecimientoDetalleDto` | 404 «La orden de abastecimiento {id} no existe.» |
| POST | `api/v1/ordenes-abastecimiento` | `GuardarOrdenAbastecimientoRequestDto { proveedorId, fechaEntregaEstimada, observaciones?, lineas: [{ productoId, cantidad, precioUnitario }] }` | 200 `{ id, numero }` | 400 |
| POST | `api/v1/ordenes-abastecimiento/desde-solicitud/{solicitudId:int}` | `GenerarDesdeSolicitudRequestDto { proveedorId?, fechaEntregaEstimada, observaciones? }` | 200 `{ id, numero }` | 400, 404, 409 |
| PUT | `api/v1/ordenes-abastecimiento/{id:int}` | `GuardarOrdenAbastecimientoRequestDto` (reemplaza las líneas) | 200 `{ id, numero }` | 400, 404, 409 |
| POST | `api/v1/ordenes-abastecimiento/{id:int}/recibir` | — | 200 `OrdenAbastecimientoDetalleDto` | 404, 409 |
| POST | `api/v1/ordenes-abastecimiento/{id:int}/anular` | `AnularOrdenRequestDto { motivo }` | 200 `OrdenAbastecimientoDetalleDto` | 400, 404, 409 |
| GET | `api/v1/ordenes-abastecimiento/resumen` | — | 200 `ResumenOrdenesAbastecimientoDto` | — |

**DTOs.**
- `OrdenAbastecimientoListadoDto { id, numero, fecha, proveedorId, proveedorNombre, solicitudNumero, estado, fechaEntregaEstimada, atrasada, montoTotal }`
- `OrdenAbastecimientoDetalleDto { id, numero, fecha, proveedorId, proveedorNombre, solicitudCompraId, solicitudNumero, estado, fechaEntregaEstimada, fechaRecepcion, atrasada, observaciones, motivoAnulacion, montoTotal, lineas: LineaOrdenAbastecimientoDto[] }`
- `LineaOrdenAbastecimientoDto { productoId, productoNombre, cantidad, precioUnitario, subtotal }`
- `ResumenOrdenesAbastecimientoDto { total, emitidas, recibidas, anuladas, atrasadas, montoPorRecibir, montoRecibido, topProveedores: ProveedorMontoDto[] }`
- `ProveedorMontoDto { proveedorId, razonSocial, ordenes, monto }`: top 5 por monto de órdenes no anuladas, después por `proveedorId`.
- `atrasada` = `Estado == Emitida` y `FechaEntregaEstimada` anterior al día de hoy en UTC.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `proveedorId` inexistente → 400 «El proveedor {proveedorId} no existe.»; inactivo → 400 «El proveedor {proveedorId} está inactivo.»
2. `fechaEntregaEstimada` (normalizada con `FechasUtc`) anterior al día de hoy en UTC al emitir o editar → 400 «La fecha de entrega estimada no puede ser anterior a hoy.»
3. `observaciones` en blanco → `null`; más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»
4. Líneas: vacía → 400 «La orden debe tener al menos una línea.»; `cantidad <= 0` → «La cantidad debe ser mayor que 0.»; `precioUnitario <= 0` → «El precio unitario debe ser mayor que 0.»; producto repetido → «El producto {productoId} está repetido en la orden.»; producto inexistente → «El producto {productoId} no existe.» (todas 400).
5. Desde solicitud: inexistente → 404 «La solicitud de compra {solicitudId} no existe.»; no aprobada → 409 «La solicitud {numero} no está aprobada.»; ya tiene una orden no anulada → 409 «La solicitud {numero} ya tiene la orden {numeroOrden}.»; proveedor = el del cuerpo o, si no viene, el sugerido en la solicitud; si no hay ninguno → 400 «Indica el proveedor de la orden.» (y después la regla 1). Las líneas se copian de la solicitud con `PrecioUnitario = PrecioEstimado` (si alguna es 0 → regla 4).
6. Id inexistente → 404 «La orden de abastecimiento {id} no existe.»
7. PUT, recibir o anular sobre una orden que no está `Emitida` → 409 «La orden {numero} ya está {estado en minúsculas}.»
8. Anular: `motivo` tras `Trim()` vacío → 400 «El motivo de anulación es obligatorio.»; más de 500 → «El motivo de anulación no puede superar 500 caracteres.»
9. Recibir guarda `FechaRecepcion` (UTC). Al crear, `Estado = Emitida`, `Fecha` = ahora (UTC).
- Filtros como en la spec 14 (estado sin distinguir mayúsculas, búsqueda por número de orden o razón social, fechas con `FechasUtc` y `fechaHasta` inclusiva). Orden: `Fecha` desc, `Id` desc.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas y proyectadas a DTO (sin N+1). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 4 órdenes con fechas fijas de 2026 sobre proveedores activos: una emitida a partir de la solicitud aprobada `SC-DEV-003`, una emitida con fecha de entrega pasada (atrasada), una recibida y una anulada con motivo.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; transiciones prohibidas → 409; segunda orden desde la misma solicitud → 409 (y permitida si la primera se anuló); ningún caso inválido crea o cambia filas | pasan |
| A3 | Flujo y consultas | tests: alta manual y desde solicitud (líneas y proveedor copiados), número `OA-` atómico sin `OA` temporal, editar, recibir, anular; `atrasada`; filtros, orden, detalle y resumen exactos | pasan |
| A4 | Migración | `OrdenesAbastecimiento` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado `estado=emitida`, detalle, resumen, POST desde `SC-DEV-004` (aprobada sin orden) → 200, repetirlo → 409, recibir la creada → 200 y otra vez → 409, POST desde una solicitud pendiente → 409, anular sin motivo → 400 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 8 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`dae900d`, mismas líneas que el auditado `0139c8e`): backend 172/172, frontend 120/120 |
| 2026-10-14 | A2–A3 | ✅ | `OrdenesAbastecimientoApiTests` (21 casos nuevos): mensajes literales, transiciones prohibidas → 409, segunda orden desde la misma solicitud → 409, alta manual y desde solicitud, número `OA-` atómico, `atrasada`, filtros, orden, detalle y resumen |
| 2026-10-14 | A4 | ✅ | `20261015035644_OrdenesAbastecimiento` (con `Designer` y snapshot) crea solo las 2 tablas nuevas; aplicada en el smoke y en Neon |
| 2026-10-14 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: emitidas, detalle `OA-DEV-001`, resumen 4/2/1/1 con 1 atrasada, generar desde solicitud, repetir → 409, recibir `OA-000005` → 200 y otra vez → 409, errores esperados |
| 2026-10-14 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO; hallazgo de diseño: rutas de acciones sin cuerpo escritas en el middleware → resuelto en la spec 18 |
| 2026-10-14 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción (solo lectura): resumen del seed con plurales correctos; filtros y rango inválido sin petición; atrasada con etiqueta y anulada sin acciones; alta con 3 líneas → `OA-000NNN` y validaciones locales; edición; recibir sin cuerpo → 200; anular con y sin motivo; «Generar orden» desde SC-DEV-004 → navega, repetir → 409, sin botón en pendientes y rechazadas; aprobar sin cuerpo → 200; Productos pide el listado una sola vez; 17 capturas sin recortes ni scroll horizontal |
