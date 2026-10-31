# Especificación: 14 - Modulo13_SolicitudesCompra (backend)

## 1. Objetivo
**Problema:** Compras solo tiene el maestro de proveedores (spec 12). No hay forma de pedir la compra de productos ni de aprobarla.
**Resultado esperado:** entidades nuevas `SolicitudCompra` y `DetalleSolicitudCompra` y `Modules/Modulo13_SolicitudesCompra/` en `api/v1/solicitudes-compra`:
- listado con filtros y paginación;
- detalle con líneas y total;
- alta y modificación mientras está pendiente;
- aprobación y rechazo;
- baja mientras está pendiente;
- resumen.

Decisión del humano (2026-10-14): seguir con el módulo recomendado, 13 SolicitudesCompra.

## 2. Fuera de alcance
- Cambios en las 4 entidades del enunciado. La relación con `Producto` se configura sin añadir navegación a `Producto` (`HasOne(...).WithMany()`).
- Convertir la solicitud en orden de abastecimiento (módulo 15), presupuestos, usuarios o firmas. El solicitante es texto.
- El frontend (spec 15).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: flujo Pendiente → Aprobada | Rechazada; solo una solicitud Pendiente se edita o se borra; el proveedor sugerido es opcional pero, si se indica, debe existir y estar activo.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{SolicitudCompra,DetalleSolicitudCompra}.cs` | crear | entidades `partial`; `Proveedor.cs` gana la colección `SolicitudesCompra` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s, índice único en `Numero`, longitudes, `decimal(18,2)`, FK a `Proveedor` (restrict) y a `Producto` (restrict, sin navegación inversa), cascada de la solicitud a sus líneas |
| `src/TitanGlobal.Api/Migrations/*_SolicitudesCompra*.cs` + snapshot | crear | `dotnet ef migrations add SolicitudesCompra -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo13_SolicitudesCompra/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/solicitudes-compra")]`, controlador delgado |
| `scripts/seed-dev.sql` | modificar | 5 solicitudes idempotentes (§4) y `setval` de sus secuencias |
| `scripts/smoke-postgres.sh` | modificar | rutas de §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo13_SolicitudesCompra/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidades.**
- `SolicitudCompra`: `Id`, `Numero` (requerido, máx. 20, único; lo genera el servidor como `SC-` + 6 dígitos del Id, p. ej. `SC-000007`), `Fecha` (UTC, la pone el servidor al crear), `Solicitante` (requerido, máx. 100), `ProveedorId` (opcional), `Estado` (texto máx. 20: `Pendiente`, `Aprobada`, `Rechazada`), `Observaciones` (opcional, máx. 500), `MotivoRechazo` (opcional, máx. 500), `FechaDecision` (UTC, opcional), `Detalles`.
- `DetalleSolicitudCompra`: `Id`, `SolicitudCompraId`, `ProductoId`, `Cantidad` (int), `PrecioEstimado` (`decimal(18,2)`).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/solicitudes-compra?busqueda=&estado=&proveedorId=&fechaDesde=&fechaHasta=&pagina=1&tamanoPagina=20` | todos opcionales salvo paginación | 200 `PaginaSolicitudesDto { items: SolicitudListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; 400 «El estado no es válido.»; 400 «La fecha desde no puede ser posterior a la fecha hasta.» |
| GET | `api/v1/solicitudes-compra/{id:int}` | — | 200 `SolicitudDetalleDto` | 404 «La solicitud de compra {id} no existe.» |
| POST | `api/v1/solicitudes-compra` | `GuardarSolicitudRequestDto { solicitante, proveedorId?, observaciones?, lineas: [{ productoId, cantidad, precioEstimado }] }` | 200 `{ id, numero }` | 400 |
| PUT | `api/v1/solicitudes-compra/{id:int}` | `GuardarSolicitudRequestDto` (reemplaza las líneas) | 200 `{ id, numero }` | 400, 404, 409 |
| POST | `api/v1/solicitudes-compra/{id:int}/aprobar` | — | 200 `SolicitudDetalleDto` | 404, 409 |
| POST | `api/v1/solicitudes-compra/{id:int}/rechazar` | `RechazarSolicitudRequestDto { motivo }` | 200 `SolicitudDetalleDto` | 400, 404, 409 |
| DELETE | `api/v1/solicitudes-compra/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/solicitudes-compra/resumen` | — | 200 `ResumenSolicitudesDto` | — |

**DTOs.**
- `SolicitudListadoDto { id, numero, fecha, solicitante, proveedorId, proveedorNombre, estado, totalLineas, montoEstimado }`
- `SolicitudDetalleDto { id, numero, fecha, solicitante, proveedorId, proveedorNombre, estado, observaciones, motivoRechazo, fechaDecision, montoEstimado, lineas: LineaSolicitudDto[] }`
- `LineaSolicitudDto { productoId, productoNombre, cantidad, precioEstimado, subtotal }`
- `ResumenSolicitudesDto { total, pendientes, aprobadas, rechazadas, montoPendiente, montoAprobado, topProductosSolicitados: ProductoSolicitadoDto[] }`
- `ProductoSolicitadoDto { productoId, nombre, unidades }`: top 5 por unidades (solo solicitudes no rechazadas), después por `productoId`.
- `montoEstimado` = suma de `Cantidad × PrecioEstimado`. `proveedorNombre` = razón social o `null`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `solicitante` tras `Trim()`: vacío → 400 «El solicitante es obligatorio.»; más de 100 → 400 «El solicitante no puede superar 100 caracteres.»
2. `observaciones` en blanco → `null`; más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»
3. `lineas` vacía o nula → 400 «La solicitud debe tener al menos una línea.»
4. Cada línea: `cantidad <= 0` → 400 «La cantidad debe ser mayor que 0.»; `precioEstimado < 0` → 400 «El precio estimado no puede ser negativo.»; producto repetido en dos líneas → 400 «El producto {productoId} está repetido en la solicitud.»
5. Producto inexistente → 400 «El producto {productoId} no existe.»
6. `proveedorId` indicado e inexistente → 400 «El proveedor {proveedorId} no existe.»; inactivo → 400 «El proveedor {proveedorId} está inactivo.»
7. Id inexistente → 404 «La solicitud de compra {id} no existe.»
8. PUT, DELETE, aprobar o rechazar sobre una solicitud que no está `Pendiente` → 409 «La solicitud {numero} ya está {estado en minúsculas}.» (p. ej. «La solicitud SC-000001 ya está aprobada.»)
9. Rechazar: `motivo` tras `Trim()` vacío → 400 «El motivo de rechazo es obligatorio.»; más de 500 → 400 «El motivo de rechazo no puede superar 500 caracteres.»
10. Aprobar o rechazar guardan `FechaDecision` en UTC. Al crear, `Estado = Pendiente` y `Fecha` = ahora en UTC; el `Numero` se asigna tras obtener el Id, en la misma operación de negocio.
- Filtros: `estado` sin distinguir mayúsculas entre los 3 válidos; `busqueda` por número o solicitante sin distinguir mayúsculas; `fechaDesde`/`fechaHasta` con `FechasUtc.Normalizar` (spec 05), `fechaHasta` inclusiva hasta el final del día.
- Orden del listado: `Fecha` descendente y después `Id` descendente.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (`SelectMany`, `GroupBy`, `Sum`, navegación).

**Seed** (idempotente): 5 solicitudes con fechas fijas de 2026: 2 pendientes (una sin proveedor), 2 aprobadas y 1 rechazada con motivo; líneas sobre productos del seed; proveedores activos del seed.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; las 3 transiciones prohibidas (editar, borrar, decidir) sobre aprobada y rechazada → 409; ningún caso inválido crea o cambia filas | pasan |
| A3 | Flujo y consultas | tests: alta → número `SC-` con el Id, estado Pendiente y fecha UTC; edición reemplaza las líneas; aprobar y rechazar con `fechaDecision`; filtros por estado, proveedor, búsqueda y rango de fechas; orden; detalle con subtotales y monto exactos; resumen exacto | pasan |
| A4 | Migración | `SolicitudesCompra` generada con `dotnet ef`; aplica sobre la BD con el seed sin errores; no altera tablas existentes ni la entidad `Producto` | evidencia |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` ampliado: listado `estado=pendiente`, rango de fechas, detalle, resumen, POST válido, aprobar el creado → 200, aprobarlo otra vez → 409, POST con proveedor inactivo → 400, DELETE de una aprobada → 409 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 8 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | Auditoría ciclo 1 | ⚠️ | Auditoría: APROBADO con un hallazgo medio (doble guardado al crear sin transacción: podía quedar una fila `TMP-…`) y uno bajo (validador como UseCase inyectado). La integración pidió corregir ambos antes de integrar |
| 2026-10-14 | Auditoría ciclo 2 | ✅ | Auditoría: APROBADO sobre `7799957` (`TransactionScope` con flujo asíncrono, `ValidacionSolicitudCompra` estático, test de número `SC-000NNN` sin `TMP-`); menor: comentario en inglés, traducido por la integración en `12c1471` |
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`ade661d`, idéntico a `7799957` según `git patch-id`): backend 151/151, frontend 96/96 |
| 2026-10-14 | A2–A3 | ✅ | `SolicitudesCompraApiTests`: mensajes literales con su código y sin `Detail`, transiciones prohibidas → 409, sin cambios de filas en casos inválidos; alta con número y fecha UTC, edición que reemplaza líneas, aprobar y rechazar, filtros, orden, detalle y resumen exactos |
| 2026-10-14 | A4 | ✅ | `20261015031106_SolicitudesCompra` (con `Designer` y snapshot) crea solo `SolicitudesCompra` y `DetallesSolicitudCompra`; `Producto` sin cambios; aplicada en el smoke y en Neon |
| 2026-10-14 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: pendientes, rango de fechas, detalle `SC-DEV-001` (4760.00), resumen 5/2/2/1, POST → `SC-000006`, aprobar → 200, otra vez → 409 «La solicitud SC-000006 ya está aprobada.», proveedor inactivo → 400, DELETE de aprobada → 409 |
| 2026-10-14 | Nota | ℹ️ | El seed numera `SC-DEV-00N` a propósito: sus Id no son fijos y un `SC-00000N` sembrado podría chocar con el número que el servidor genera a partir del Id |
| 2026-10-14 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL, `9e21623`) y en producción (https://titanglobal-erp.vercel.app, solo lectura): resumen del seed 2/2/1; filtros por número, solicitante, estado, proveedor y fechas; detalle con subtotales y rechazada sin acciones; alta con 3 líneas → `SC-000NNN`, validaciones locales sin petición, proveedor inactivo fuera del selector; edición quitando una línea; aprobar; rechazar sin motivo (error local) y con motivo; eliminar con Escape; demás pantallas sin regresiones; 13 capturas sin recortes ni scroll horizontal |
