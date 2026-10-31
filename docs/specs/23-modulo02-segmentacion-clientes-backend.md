# Especificación: 23 - Modulo02_SegmentacionClientes (backend)

## 1. Objetivo
**Problema:** Comercial no puede agrupar a los clientes según su comportamiento de compra (cuánto compran y con qué frecuencia) para darles un trato distinto.
**Resultado esperado:** entidad nueva `SegmentoCliente` y `Modules/Modulo02_SegmentacionClientes/` en `api/v1/segmentos-clientes`:
- mantenimiento de segmentos con criterios (monto mínimo y número mínimo de órdenes) y prioridad;
- clasificación automática de cada cliente en el segmento activo de mayor prioridad cuyos criterios cumple, calculada en la consulta a partir de sus órdenes;
- lista de clientes de un segmento y resumen.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Guardar el segmento en `Cliente` (no se modifican las 4 entidades del enunciado): la clasificación siempre se calcula.
- Campañas, precios por segmento (módulo 06) o promociones (07).
- El frontend (spec 24).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: un cliente pertenece como máximo a un segmento (el activo de menor número de prioridad que cumpla todos sus criterios); los criterios se evalúan sobre todas las órdenes del cliente.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/SegmentoCliente.cs` | crear | entidad `partial` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet<SegmentoCliente> SegmentosClientes`; índices únicos en `Codigo` y en `Prioridad`; longitudes; `decimal(18,2)` |
| `src/TitanGlobal.Api/Migrations/*_SegmentosClientes*.cs` + snapshot | crear | `dotnet ef migrations add SegmentosClientes -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo02_SegmentacionClientes/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/segmentos-clientes")]`; helper estático `ValidacionSegmentoCliente` |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo02_SegmentacionClientes/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidad `SegmentoCliente`:** `Id`, `Codigo` (requerido, máx. 20, mayúsculas, único), `Nombre` (requerido, máx. 100), `Descripcion` (opcional, máx. 300), `Prioridad` (int 1–999, única; 1 = más prioritario), `MontoMinimo` (`decimal(18,2)`, ≥ 0), `OrdenesMinimas` (int ≥ 0), `Activo` (bool, por defecto `true`).

**Clasificación.** Para cada cliente se calculan `totalOrdenes` (número de `OrdenCompra`) y `montoTotal` (suma de `Total`, 0 sin órdenes). Su segmento es el **activo** con menor `Prioridad` tal que `montoTotal >= MontoMinimo` y `totalOrdenes >= OrdenesMinimas`. Si ninguno cumple: sin segmento.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/segmentos-clientes?busqueda=&activo=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaSegmentosDto { items: SegmentoListadoDto[], total, pagina, tamanoPagina }` | 400 paginación |
| GET | `api/v1/segmentos-clientes/{id:int}` | — | 200 `SegmentoDetalleDto` | 404 «El segmento {id} no existe.» |
| POST | `api/v1/segmentos-clientes` | `GuardarSegmentoRequestDto { codigo, nombre, descripcion?, prioridad, montoMinimo, ordenesMinimas, activo }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/segmentos-clientes/{id:int}` | `GuardarSegmentoRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/segmentos-clientes/{id:int}` | — | 204 | 404 |
| GET | `api/v1/segmentos-clientes/{id:int}/clientes?pagina=1&tamanoPagina=20` | — | 200 `PaginaClientesSegmentoDto { items: ClienteClasificadoDto[], total, pagina, tamanoPagina }` (clientes cuyo segmento asignado es este) | 404 |
| GET | `api/v1/segmentos-clientes/clasificacion?segmentoId=&sinSegmento=&busqueda=&pagina=1&tamanoPagina=20` | `sinSegmento=true` lista solo los no clasificados | 200 `PaginaClientesSegmentoDto` | 400 paginación; 400 «No se puede filtrar por segmento y sin segmento a la vez.» |
| GET | `api/v1/segmentos-clientes/resumen` | — | 200 `ResumenSegmentacionDto` | — |

**DTOs.**
- `SegmentoListadoDto { id, codigo, nombre, prioridad, montoMinimo, ordenesMinimas, activo, clientes }` (`clientes` = número de clientes asignados ahora; 0 si inactivo)
- `SegmentoDetalleDto` = listado + `descripcion`
- `ClienteClasificadoDto { clienteId, nombre, ruc, totalOrdenes, montoTotal, segmentoId, segmentoCodigo, segmentoNombre }` (segmento `null` si no tiene); orden por `montoTotal` desc, después `clienteId`.
- `ResumenSegmentacionDto { totalClientes, clientesSegmentados, clientesSinSegmento, porSegmento: SegmentoClientesDto[] }`
- `SegmentoClientesDto { segmentoId, codigo, nombre, clientes, montoTotal }`, por `Prioridad`; solo segmentos activos.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código es obligatorio.»; no cumple `^[A-Z0-9-]{2,20}$` → 400 «El código solo admite letras, números y guiones (2 a 20 caracteres).»
2. `nombre` tras `Trim()`: vacío → 400 «El nombre del segmento es obligatorio.»; más de 100 → 400 «El nombre del segmento no puede superar 100 caracteres.»
3. `descripcion` en blanco → `null`; más de 300 → 400 «La descripción no puede superar 300 caracteres.»
4. `prioridad` fuera de 1–999 → 400 «La prioridad debe estar entre 1 y 999.»
5. `montoMinimo < 0` → 400 «El monto mínimo no puede ser negativo.»; `ordenesMinimas < 0` → 400 «Las órdenes mínimas no pueden ser negativas.»
6. Ambos criterios en 0 → 400 «Define al menos un criterio: monto mínimo u órdenes mínimas.»
7. Código repetido → 409 «Ya existe un segmento con el código {codigo}.»; prioridad repetida → 409 «Ya existe un segmento con la prioridad {prioridad}.» (también en la carrera, `23505`; PUT que mantiene los suyos es válido).
8. Id inexistente → 404 «El segmento {id} no existe.»

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas. La clasificación se resuelve en la base de datos o, si EF no puede traducir la elección del segmento, cargando solo los segmentos activos (pocos) y proyectando los totales por cliente en una sola consulta; nunca una consulta por cliente (sin N+1). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (navegación inversa, `Sum`, `Count`, `GroupBy`).

**Seed:** 4 segmentos: `VIP` prioridad 1 (monto ≥ 5000 y ≥ 3 órdenes), `FRECUENTE` 2 (≥ 2 órdenes), `OCASIONAL` 3 (≥ 1 orden), `PREMIUM-OLD` 4 (monto ≥ 1000, inactivo). Con el seed de clientes y órdenes: Acme Industrial → VIP, Beta Comercio → FRECUENTE, Gamma Manufactura y Delta Logística → OCASIONAL, el cliente sin órdenes → sin segmento.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; código en mayúsculas; PUT con su propio código y prioridad → 200; ningún caso inválido crea o cambia filas | pasan |
| A3 | Clasificación | tests con datos conocidos: un cliente que cumple dos segmentos queda en el de menor prioridad; segmento inactivo no clasifica; cliente sin órdenes sin segmento; límite exacto (monto igual al mínimo) clasifica; `{id}/clientes`, `clasificacion` con sus filtros y orden, y resumen exactos | pasan |
| A4 | Migración | `SegmentosClientes` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado, `clasificacion` (Acme → VIP, cliente sin órdenes → `null`), `clasificacion?sinSegmento=true`, `{id}/clientes` de FRECUENTE, resumen, POST válido, prioridad repetida → 409, DELETE del creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 8 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidad, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`359ccff`, mismas líneas que el auditado `433cc63`): backend 247/247, frontend 185/185 |
| 2026-10-15 | A2–A3 | ✅ | `SegmentacionClientesApiTests` (17 tests): mensajes literales, código en mayúsculas, unicidad de código y prioridad, clasificación por prioridad, límite exacto, segmento inactivo, cliente sin órdenes, filtros, paginación y resumen |
| 2026-10-15 | A4 | ✅ | migración `SegmentosClientes` (con `Designer` y snapshot) solo crea la tabla y sus índices únicos; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: Acme → VIP, cliente sin órdenes → `null`, `sinSegmento=true`, clientes de FRECUENTE (Beta Comercio), resumen 5/4/1, POST → `{id:5}`, prioridad repetida → 409 literal, DELETE → 204 |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO. Observación no bloqueante: la clasificación usa 3 consultas fijas (sin N+1) pero pagina en memoria con todos los clientes cargados; suficiente para el volumen actual, a revisar si crece |
| 2026-10-15 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción: resumen 5/4/1 y tarjetas con plurales correctos; pestañas con flechas y `?vista=`; criterios en texto y detalle de VIP con Acme; clasificación Acme → VIP, Beta → FRECUENTE, Gamma y Delta → OCASIONAL, cliente sin órdenes → «Sin segmento» (`sinSegmento=true` en la red); un segmento de menor prioridad no cambia la clasificación; ambos criterios en 0 → error local; prioridad 1 repetida → 409 conservando lo escrito; inactivar OCASIONAL reasigna y se restaura; baja con Escape; sin regresiones; 13 capturas sin recortes ni scroll horizontal |
