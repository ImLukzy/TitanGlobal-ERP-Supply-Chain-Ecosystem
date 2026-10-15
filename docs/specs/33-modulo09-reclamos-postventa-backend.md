# Especificación: 33 - Modulo09_ReclamosPostventa (backend)

## 1. Objetivo
**Problema:** los reclamos de los clientes sobre sus pedidos (producto defectuoso, retraso, faltante, facturación) no se registran ni se les da seguimiento, y no se sabe si se atienden a tiempo.
**Resultado esperado:** entidad nueva `ReclamoPostventa` y `Modules/Modulo09_ReclamosPostventa/` en `api/v1/reclamos-postventa`:
- reclamos ligados a una orden de compra del cliente (módulo 04), con tipo y prioridad;
- ciclo Abierto → EnProceso → Resuelto → Cerrado, o Rechazado con motivo;
- **plazo de atención** según la prioridad y aviso de vencidos;
- listado, detalle, resumen con cumplimiento de plazos.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Devoluciones de mercadería (módulo 10), notas de crédito (52) o reposición de stock.
- Cambios en las 4 entidades del enunciado (relaciones sin navegación inversa).
- El frontend (spec 34).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador: plazo de atención de 2 días para prioridad Alta, 5 para Media y 10 para Baja, contados desde el alta; si se indica un producto, debe estar en la orden reclamada.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/ReclamoPostventa.cs` | crear | FKs a `Cliente`, `OrdenCompra` y opcional a `Producto` con `HasOne(...).WithMany()` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`, índice único en `Numero`, longitudes, FKs restrict |
| `src/TitanGlobal.Api/Migrations/*_ReclamosPostventa*.cs` + snapshot | crear | `dotnet ef migrations add ReclamosPostventa -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo09_ReclamosPostventa/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/reclamos-postventa")]`; helper estático `ValidacionReclamo`; `iniciar` y `cerrar` con `[PermitirCuerpoVacio]` |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo09_ReclamosPostventa/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidad `ReclamoPostventa`:** `Id`, `Numero` (máx. 20, único; `REC-` + 6 dígitos del Id, atómico como en la spec 14), `Fecha` (UTC, del servidor), `ClienteId`, `OrdenCompraId`, `ProductoId` (opcional), `Tipo` (máx. 20: `Defecto`, `Retraso`, `Faltante`, `Facturacion`, `Otro`), `Prioridad` (máx. 10: `Alta`, `Media`, `Baja`), `Descripcion` (requerida, máx. 1000), `Estado` (máx. 20: `Abierto`, `EnProceso`, `Resuelto`, `Cerrado`, `Rechazado`), `FechaLimite` (UTC = `Fecha` + 2, 5 o 10 días según la prioridad; se recalcula si se edita la prioridad), `Resolucion` (máx. 1000), `MotivoRechazo` (máx. 500), `FechaInicio`, `FechaResolucion`, `FechaCierre` (UTC, opcionales).
- **Vencido**: estado `Abierto` o `EnProceso` y `FechaLimite` anterior al momento actual (UTC).
- **Atendido a tiempo**: `FechaResolucion <= FechaLimite` (para Resuelto y Cerrado).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/reclamos-postventa?busqueda=&estado=&tipo=&prioridad=&clienteId=&vencidos=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaReclamosDto { items: ReclamoListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «El estado no es válido.»; «El tipo de reclamo no es válido.»; «La prioridad no es válida.» |
| GET | `api/v1/reclamos-postventa/{id:int}` | — | 200 `ReclamoDetalleDto` | 404 «El reclamo {id} no existe.» |
| POST | `api/v1/reclamos-postventa` | `GuardarReclamoRequestDto { clienteId, ordenCompraId, productoId?, tipo, prioridad, descripcion }` | 200 `{ id, numero }` | 400 |
| PUT | `api/v1/reclamos-postventa/{id:int}` | igual | 200 `{ id, numero }` | 400, 404, 409 |
| POST | `api/v1/reclamos-postventa/{id:int}/iniciar` | sin cuerpo | 200 `ReclamoDetalleDto` | 404, 409 |
| POST | `api/v1/reclamos-postventa/{id:int}/resolver` | `ResolverReclamoRequestDto { resolucion }` | 200 `ReclamoDetalleDto` | 400, 404, 409 |
| POST | `api/v1/reclamos-postventa/{id:int}/cerrar` | sin cuerpo | 200 `ReclamoDetalleDto` | 404, 409 |
| POST | `api/v1/reclamos-postventa/{id:int}/rechazar` | `RechazarReclamoRequestDto { motivo }` | 200 `ReclamoDetalleDto` | 400, 404, 409 |
| GET | `api/v1/reclamos-postventa/resumen` | — | 200 `ResumenReclamosDto` | — |

**DTOs.**
- `ReclamoListadoDto { id, numero, fecha, clienteId, clienteNombre, ordenCompraId, numeroOrden, productoNombre, tipo, prioridad, estado, fechaLimite, vencido }`
- `ReclamoDetalleDto` = listado + `descripcion, resolucion, motivoRechazo, fechaInicio, fechaResolucion, fechaCierre, atendidoATiempo` (`null` si aún no está resuelto)
- `ResumenReclamosDto { total, abiertos, enProceso, resueltos, cerrados, rechazados, vencidos, porTipo: TipoReclamosDto[], horasPromedioResolucion, cumplimientoPlazo }`: `horasPromedioResolucion` = media de `FechaResolucion − Fecha` en horas (2 decimales) de los resueltos y cerrados; `cumplimientoPlazo` = % de resueltos y cerrados atendidos a tiempo (2 decimales; 0 si no hay).
- `TipoReclamosDto { tipo, reclamos }` en el orden de la lista de tipos (solo los que tienen reclamos).

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. Cliente inexistente → 400 «El cliente {clienteId} no existe.»
2. Orden inexistente → 400 «La orden de compra {ordenCompraId} no existe.»; de otro cliente → 400 «La orden {numeroOrden} no pertenece al cliente.»
3. `productoId` indicado: inexistente → 400 «El producto {productoId} no existe.»; que no está en la orden → 400 «El producto {productoId} no está en la orden {numeroOrden}.»
4. `tipo` válido sin distinguir mayúsculas y guardado canónico → si no, 400 «El tipo de reclamo no es válido.»; `prioridad` igual → si no, 400 «La prioridad no es válida.»
5. `descripcion` tras `Trim()`: vacía → 400 «La descripción es obligatoria.»; más de 1000 → 400 «La descripción no puede superar 1000 caracteres.»
6. Id inexistente → 404 «El reclamo {id} no existe.»
7. Transiciones: editar solo `Abierto`; `iniciar` solo `Abierto`; `resolver` desde `Abierto` o `EnProceso`; `cerrar` solo `Resuelto`; `rechazar` desde `Abierto` o `EnProceso`. En otro caso → 409 «El reclamo {numero} está {estado en minúsculas y legible: abierto, en proceso, resuelto, cerrado, rechazado}.» (p. ej. «El reclamo REC-000003 está cerrado.»)
8. Resolver: `resolucion` vacía → 400 «La resolución es obligatoria.»; más de 1000 → «La resolución no puede superar 1000 caracteres.» Rechazar: `motivo` vacío → 400 «El motivo de rechazo es obligatorio.»; más de 500 → «El motivo de rechazo no puede superar 500 caracteres.»
9. Iniciar guarda `FechaInicio`; resolver, `FechaResolucion`; cerrar y rechazar, `FechaCierre` (UTC).
- Filtros sin distinguir mayúsculas; `busqueda` por número de reclamo, número de orden o nombre de cliente; `vencidos=true`. Orden: prioridad (Alta, Media, Baja), después `FechaLimite` y `Id`.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas, sin N+1. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 5 reclamos (`REC-DEV-00N`) sobre órdenes reales del seed y sus clientes: uno abierto de prioridad Alta ya vencido, uno en proceso Media dentro de plazo, uno resuelto a tiempo, uno cerrado fuera de plazo y uno rechazado con motivo.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: un caso por cada mensaje de §4 con su código, literal y sin `Detail`; todas las transiciones permitidas y prohibidas; producto que no está en la orden; orden de otro cliente; ningún caso inválido crea o cambia filas | pasan |
| A3 | Plazos y consultas | tests: `FechaLimite` por prioridad y recalculada al editarla; `vencido` (justo en el límite no está vencido); `atendidoATiempo`; filtros, `vencidos=true`, orden por prioridad; resumen con horas promedio y cumplimiento exactos | pasan |
| A4 | Migración | `ReclamosPostventa` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado, `vencidos=true`, detalle, resumen, POST válido → iniciar → resolver → cerrar (200 cada uno), cerrar otra vez → 409, POST con un producto que no está en la orden → 400, rechazar sin motivo → 400 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 9 operaciones | presentes |

## 6. Checklist de ejecución
- [ ] Tarea 1: entidad, `DbContext` y migración.
- [ ] Tarea 2: DTOs, UseCases y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
