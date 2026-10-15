# Especificación: 29 - Modulo06_ListasPrecios (backend)

## 1. Objetivo
**Problema:** todos los clientes pagan el precio base de `Producto`. Comercial no puede tener precios distintos por periodo ni por segmento de clientes (módulo 02).
**Resultado esperado:** entidades nuevas `ListaPrecios` e `ItemListaPrecios` y `Modules/Modulo06_ListasPrecios/` en `api/v1/listas-precios`:
- listas con vigencia (fecha de inicio y fin opcional), prioridad y, opcionalmente, un segmento de clientes;
- **resolución de precio**: el precio de un producto para un cliente en una fecha;
- listado, detalle con sus precios, alta, edición, baja y resumen.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Cambiar `Producto.Precio` o aplicar las listas automáticamente al crear órdenes o cotizaciones (se hará si se pide; hoy la resolución es una consulta).
- Monedas (módulo 74): todo en PEN.
- El frontend (spec 30).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador: una lista de segmento gana a una general; entre listas del mismo tipo gana la de menor número de prioridad; si ninguna lista vigente tiene el producto, se usa el precio base.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{ListaPrecios,ItemListaPrecios}.cs` | crear | FK opcional a `SegmentoCliente`; FK a `Producto` con `HasOne(...).WithMany()` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s, índices únicos en `Codigo`, en `Prioridad` y en (`ListaPreciosId`, `ProductoId`), longitudes, `decimal(18,2)`, FKs restrict, cascada a ítems |
| `src/TitanGlobal.Api/Migrations/*_ListasPrecios*.cs` + snapshot | crear | `dotnet ef migrations add ListasPrecios -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo06_ListasPrecios/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/listas-precios")]`; helper estático `ValidacionListaPrecios` |
| `src/TitanGlobal.Api/Modules/Modulo02_SegmentacionClientes/UseCases/ConsultaSegmentacionClientes.cs` | modificar (solo si hace falta) | exponer un método para obtener el segmento de **un** cliente reutilizando la misma lógica, sin duplicarla en el módulo 06 |

| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo06_ListasPrecios/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidades.**
- `ListaPrecios`: `Id`, `Codigo` (requerido, máx. 20, mayúsculas, único), `Nombre` (requerido, máx. 100), `SegmentoClienteId` (opcional), `Prioridad` (int 1–999, única), `FechaInicio` (UTC, sin hora), `FechaFin` (UTC, sin hora, opcional = sin fin), `Activa` (bool), `Items`.
- `ItemListaPrecios`: `Id`, `ListaPreciosId`, `ProductoId`, `Precio` (`decimal(18,2)`, > 0). Un producto aparece una sola vez por lista.
- Una lista está **vigente** en una fecha si está activa, `FechaInicio <= fecha` y (`FechaFin` es nula o `fecha <= FechaFin`).

**Resolución de precio** para (`productoId`, `clienteId`, `fecha`; `fecha` por defecto hoy en UTC):
1. Segmento del cliente según el módulo 02 (reutilizando su lógica).
2. Candidatas: listas vigentes en la fecha que contienen el producto y que son de su segmento o generales (`SegmentoClienteId` nulo).
3. Gana una lista del segmento del cliente sobre una general; dentro del mismo grupo, la de menor `Prioridad`.
4. Sin candidatas: precio base `Producto.Precio` con `origen = "Base"`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/listas-precios?busqueda=&segmentoId=&vigentes=&pagina=1&tamanoPagina=20` | opcionales salvo paginación; `vigentes=true` = vigentes hoy; `segmentoId=0` = solo listas generales (sin segmento; añadido el 2026-10-15 tras el QA de la spec 30) | 200 `PaginaListasDto { items: ListaListadoDto[], total, pagina, tamanoPagina }` | 400 paginación |
| GET | `api/v1/listas-precios/{id:int}` | — | 200 `ListaDetalleDto` | 404 «La lista de precios {id} no existe.» |
| POST | `api/v1/listas-precios` | `GuardarListaRequestDto { codigo, nombre, segmentoClienteId?, prioridad, fechaInicio, fechaFin?, activa, items: [{ productoId, precio }] }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/listas-precios/{id:int}` | igual (reemplaza los ítems) | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/listas-precios/{id:int}` | — | 204 | 404 |
| GET | `api/v1/listas-precios/precio?productoId=&clienteId=&fecha=` | `productoId` y `clienteId` obligatorios | 200 `PrecioResueltoDto` | 400 «El producto {id} no existe.» / «El cliente {id} no existe.» |
| GET | `api/v1/listas-precios/resumen` | — | 200 `ResumenListasDto` | — |

**DTOs.**
- `ListaListadoDto { id, codigo, nombre, segmentoId, segmentoCodigo, prioridad, fechaInicio, fechaFin, activa, vigente, productos }` (`vigente` hoy; `productos` = nº de ítems)
- `ListaDetalleDto` = listado + `items: ItemListaDto[]` con `{ productoId, productoNombre, precioBase, precio, diferenciaPorcentaje }` (`(precio − precioBase) / precioBase × 100`, 2 decimales, negativo = descuento), por nombre de producto.
- `PrecioResueltoDto { productoId, productoNombre, clienteId, clienteNombre, segmentoCodigo, fecha, precio, precioBase, origen, listaId, listaCodigo }`: `origen` = `"Segmento"`, `"General"` o `"Base"`.
- `ResumenListasDto { total, activas, vigentes, deSegmento, generales, productosConPrecioEspecial }` (productos distintos presentes en alguna lista vigente).

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código es obligatorio.»; no cumple `^[A-Z0-9-]{2,20}$` → 400 «El código solo admite letras, números y guiones (2 a 20 caracteres).»
2. `nombre` tras `Trim()`: vacío → 400 «El nombre de la lista es obligatorio.»; más de 100 → 400 «El nombre de la lista no puede superar 100 caracteres.»
3. `segmentoClienteId` indicado e inexistente → 400 «El segmento {id} no existe.»
4. `prioridad` fuera de 1–999 → 400 «La prioridad debe estar entre 1 y 999.»
5. `fechaFin` anterior a `fechaInicio` → 400 «La fecha de fin no puede ser anterior a la de inicio.»
6. Ítems: vacío → «La lista debe tener al menos un producto.»; `precio <= 0` → «El precio debe ser mayor que 0.»; producto repetido → «El producto {productoId} está repetido en la lista.»; producto inexistente → «El producto {productoId} no existe.» (todas 400).
7. Código repetido → 409 «Ya existe una lista de precios con el código {codigo}.»; prioridad repetida → 409 «Ya existe una lista de precios con la prioridad {prioridad}.» (también en la carrera, `23505`; un PUT que mantiene los suyos es válido).
8. Id inexistente → 404 «La lista de precios {id} no existe.»

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas a DTO, sin N+1 (la resolución hace un número fijo de consultas). Orden del listado: `Prioridad`. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 3 listas: `VIP-2026` (segmento VIP, prioridad 1, 2026-01-01 → 2026-12-31) con Laptop Pro a 2,300.00 y Monitor 27 a 820.00; `GENERAL-2026` (general, prioridad 10, desde 2026-01-01 sin fin) con Laptop Pro a 2,450.00 y Mouse Óptico a 45.00; `PROMO-2025` (general, prioridad 5, 2025-01-01 → 2025-12-31, no vigente) con Laptop Pro a 2,000.00. Así: Acme (VIP) paga Laptop Pro 2,300.00 (Segmento); Beta paga 2,450.00 (General); cualquiera paga Teclado Mecánico a su precio base (Base).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: un caso por cada mensaje de §4 con su código, literal y sin `Detail`; código en mayúsculas; PUT con su propio código y prioridad; ningún caso inválido crea o cambia filas | pasan |
| A3 | Resolución y consultas | tests: segmento gana a general aunque la general tenga menor prioridad; entre dos generales gana la de menor prioridad; lista inactiva, fuera de fecha (día anterior al inicio y posterior al fin) o sin el producto no cuenta; límites de fecha incluidos; sin listas → Base; cliente sin segmento solo ve generales; `vigentes=true`, `segmentoId`, diferencia porcentual y resumen exactos | pasan |
| A4 | Migración | `ListasPrecios` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado, `vigentes=true`, detalle de VIP-2026, `precio` de Laptop Pro para Acme → 2300.00 Segmento, para Beta → 2450.00 General, Teclado Mecánico → Base, resumen, POST válido, prioridad repetida → 409, DELETE → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases (incluida la resolución) y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`09afa9e`, mismas líneas que el auditado `50c93df`): backend 329/329, frontend 201/201 |
| 2026-10-15 | A2–A3 | ✅ | `ListasPreciosApiTests` (25 tests): cada mensaje literal, código en mayúsculas, unicidad de código y prioridad; resolución segmento > general > base, prioridad dentro del grupo, lista inactiva, fuera de fecha o sin el producto, límites de fecha, cliente sin segmento; diferencia porcentual y resumen |
| 2026-10-15 | A4 | ✅ | migración `ListasPrecios` (con `Designer` y snapshot) solo crea las 2 tablas; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: vigentes, detalle de VIP-2026 (Laptop Pro −8.00 %), precio de Laptop Pro para Acme 2300.00 «Segmento», para Beta 2450.00 «General», Teclado Mecánico 200.00 «Base», resumen, POST → `{id:4}`, prioridad repetida → 409 literal, DELETE → 204 |
| 2026-10-15 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO. Observaciones no bloqueantes: obtener el segmento de un cliente clasifica a todos (3 consultas fijas, aceptable a esta escala; Kelly propone filtrar por `ClienteId` si crece) y `ConsultaSegmentacionClientes` pasó a `public` sin necesidad |
| 2026-10-15 | Ajuste `segmentoId=0` | ✅ | `f0f6a75` (Michael) integrado como `c48b5ec`: `segmentoId=0` filtra las listas generales; 3 tests nuevos (con y sin `vigentes`, total exacto, VIP sin cambios); auditoría de Kelly APROBADO sin hallazgos; producción → 2 generales y 1 vigente |
