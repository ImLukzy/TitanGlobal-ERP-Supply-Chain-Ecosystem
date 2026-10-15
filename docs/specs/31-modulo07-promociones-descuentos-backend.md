# Especificación: 31 - Modulo07_PromocionesDescuentos (backend)

## 1. Objetivo
**Problema:** Comercial no puede definir campañas de descuento (por producto o por compra mínima), limitarlas en el tiempo o a un segmento, ni saber qué descuento le corresponde a una compra concreta.
**Resultado esperado:** entidad nueva `Promocion` y `Modules/Modulo07_PromocionesDescuentos/` en `api/v1/promociones`:
- promociones de dos tipos con vigencia, segmento opcional y estado activo;
- **evaluación**: dada una compra (cliente, fecha y líneas), qué promociones se aplican y con qué importes;
- listado, detalle, alta, edición, baja y resumen.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Aplicar las promociones automáticamente al crear órdenes o cotizaciones (la evaluación es una consulta).
- Cupones, límites de usos y combinaciones complejas.
- Cambios en las 4 entidades del enunciado. El frontend (spec 32).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador: a cada línea se le aplica como máximo una promoción de producto (la de mayor porcentaje); a la compra, como máximo una promoción por monto mínimo (la de mayor porcentaje), calculada sobre el total ya descontado por línea; las promociones de segmento solo aplican a clientes de ese segmento (módulo 02).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/Promocion.cs` | crear | FK opcional a `Producto` (`HasOne(...).WithMany()`) y a `SegmentoCliente` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`, índice único en `Codigo`, longitudes, `decimal(18,2)` y `decimal(5,2)`, FKs restrict |
| `src/TitanGlobal.Api/Migrations/*_Promociones*.cs` + snapshot | crear | `dotnet ef migrations add Promociones -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo07_PromocionesDescuentos/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/promociones")]`; helper estático `ValidacionPromocion`; el segmento del cliente se obtiene con la lógica del módulo 02 |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo07_PromocionesDescuentos/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidad `Promocion`:** `Id`, `Codigo` (requerido, máx. 20, mayúsculas, único), `Nombre` (requerido, máx. 100), `Tipo` (máx. 20: `Producto` o `MontoMinimo`), `ProductoId` (requerido si `Tipo = Producto`, nulo si no), `MontoMinimo` (`decimal(18,2)`, requerido y > 0 si `Tipo = MontoMinimo`, nulo si no), `PorcentajeDescuento` (`decimal(5,2)`, mayor que 0 y como máximo 100), `SegmentoClienteId` (opcional), `FechaInicio` y `FechaFin` (UTC, sin hora; fin obligatorio), `Activa` (bool).
- **Vigente** en una fecha: activa y `FechaInicio <= fecha <= FechaFin` (días completos).

**Evaluación** de una compra (`clienteId`, `fecha` por defecto hoy UTC, líneas `productoId`, `cantidad`, `precioUnitario`):
1. Segmento del cliente según el módulo 02.
2. Promociones aplicables: vigentes en la fecha y generales o del segmento del cliente.
3. Por línea: `subtotal = cantidad × precioUnitario`; entre las promociones `Producto` de ese producto, la de mayor porcentaje (empate: menor `Id`); `descuento = round(subtotal × % / 100, 2)`.
4. `totalTrasLineas = Σ (subtotal − descuento)`. Entre las promociones `MontoMinimo` con `MontoMinimo <= totalTrasLineas`, la de mayor porcentaje (empate: menor `Id`); `descuentoCompra = round(totalTrasLineas × % / 100, 2)`.
5. `total = totalTrasLineas − descuentoCompra`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/promociones?busqueda=&tipo=&segmentoId=&vigentes=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaPromocionesDto { items: PromocionListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «El tipo de promoción no es válido.» |
| GET | `api/v1/promociones/{id:int}` | — | 200 `PromocionDetalleDto` | 404 «La promoción {id} no existe.» |
| POST | `api/v1/promociones` | `GuardarPromocionRequestDto { codigo, nombre, tipo, productoId?, montoMinimo?, porcentajeDescuento, segmentoClienteId?, fechaInicio, fechaFin, activa }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/promociones/{id:int}` | igual | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/promociones/{id:int}` | — | 204 | 404 |
| POST | `api/v1/promociones/evaluar` | `EvaluarCompraRequestDto { clienteId, fecha?, lineas: [{ productoId, cantidad, precioUnitario }] }` | 200 `EvaluacionCompraDto` | 400 |
| GET | `api/v1/promociones/resumen` | — | 200 `ResumenPromocionesDto` | — |

**DTOs.**
- `PromocionListadoDto { id, codigo, nombre, tipo, productoId, productoNombre, montoMinimo, porcentajeDescuento, segmentoId, segmentoCodigo, fechaInicio, fechaFin, activa, vigente }` (`vigente` hoy); `PromocionDetalleDto` = listado.
- `EvaluacionCompraDto { clienteId, clienteNombre, segmentoCodigo, fecha, lineas: LineaEvaluadaDto[], totalTrasLineas, promocionCompra: PromocionAplicadaDto?, descuentoCompra, subtotal, descuentoTotal, total }`
- `LineaEvaluadaDto { productoId, productoNombre, cantidad, precioUnitario, subtotal, promocion: PromocionAplicadaDto?, descuento, importe }`
- `PromocionAplicadaDto { id, codigo, nombre, porcentajeDescuento }`
- `ResumenPromocionesDto { total, activas, vigentes, deProducto, deMontoMinimo, deSegmento, porcentajePromedioVigentes }` (2 decimales; 0 si no hay vigentes).

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden.** Alta y edición:
1. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código es obligatorio.»; no cumple `^[A-Z0-9-]{2,20}$` → 400 «El código solo admite letras, números y guiones (2 a 20 caracteres).»
2. `nombre` tras `Trim()`: vacío → 400 «El nombre de la promoción es obligatorio.»; más de 100 → 400 «El nombre de la promoción no puede superar 100 caracteres.»
3. `tipo` válido sin distinguir mayúsculas y guardado canónico → si no, 400 «El tipo de promoción no es válido.»
4. `Producto`: sin `productoId` → 400 «La promoción de producto requiere un producto.»; producto inexistente → 400 «El producto {id} no existe.»; con `montoMinimo` → 400 «Una promoción de producto no lleva monto mínimo.»
5. `MontoMinimo`: `montoMinimo` nulo o ≤ 0 → 400 «El monto mínimo debe ser mayor que 0.»; con `productoId` → 400 «Una promoción por monto mínimo no lleva producto.»
6. `porcentajeDescuento` ≤ 0 o > 100 → 400 «El descuento debe ser mayor que 0 y como máximo 100.»
7. `segmentoClienteId` indicado e inexistente → 400 «El segmento {id} no existe.»
8. `fechaFin` anterior a `fechaInicio` → 400 «La fecha de fin no puede ser anterior a la de inicio.»
9. Código repetido → 409 «Ya existe una promoción con el código {codigo}.» (también en la carrera, `23505`; PUT con su propio código válido).
10. Id inexistente → 404 «La promoción {id} no existe.»

Evaluación:
11. Cliente inexistente → 400 «El cliente {id} no existe.»; sin líneas → 400 «La compra debe tener al menos una línea.»; `cantidad <= 0` → 400 «La cantidad debe ser mayor que 0.»; `precioUnitario <= 0` → 400 «El precio unitario debe ser mayor que 0.»; producto inexistente → 400 «El producto {id} no existe.»

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas, sin N+1 (la evaluación hace un número fijo de consultas). Orden del listado: `FechaInicio` desc, `Codigo`. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 4 promociones (2026-01-01 → 2026-12-31 salvo indicación): `LAPTOP-10` producto Laptop Pro 10 %; `LAPTOP-VIP-15` producto Laptop Pro 15 % solo segmento VIP; `COMPRA-3000` monto mínimo 3,000.00 al 5 %; `VERANO-2025` producto Mouse Óptico 20 % del 2025 (no vigente). Así, una compra de 2 Laptop Pro a 2,500.00: para Acme (VIP) aplica LAPTOP-VIP-15 (descuento 750.00; total tras líneas 4,250.00) y después COMPRA-3000 (212.50), total 4,037.50; para Beta aplica LAPTOP-10 (500.00; 4,500.00) y COMPRA-3000 (225.00), total 4,275.00.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: un caso por cada mensaje de §4 con su código, literal y sin `Detail`; código y tipo canónicos; PUT con su propio código; ningún caso inválido crea o cambia filas | pasan |
| A3 | Evaluación y consultas | tests: los dos ejemplos del seed con importes exactos; la promoción de mayor porcentaje gana por línea y por compra (y el empate por `Id`); la de segmento no aplica a otros clientes; inactiva o fuera de fechas (día anterior y posterior) no cuenta, los límites sí; el monto mínimo se mide después de los descuentos por línea (justo en el mínimo aplica, un céntimo menos no); sin promociones → total = subtotal; filtros, `vigentes`, orden y resumen exactos | pasan |
| A4 | Migración | `Promociones` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado, `vigentes=true`, detalle, evaluar la compra del ejemplo para Acme (4037.50) y para Beta (4275.00), evaluar sin líneas → 400, POST válido, código repetido → 409, tipo Producto sin producto → 400, DELETE → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [ ] Tarea 1: entidad, `DbContext` y migración.
- [ ] Tarea 2: DTOs, UseCases (incluida la evaluación) y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`ad27d0d`, mismas líneas que el auditado `e8492fd`): backend 365/365, frontend 222/222 |
| 2026-10-15 | A2–A3 | ✅ | `PromocionesDescuentosApiTests` (36 tests): cada mensaje literal, combinaciones tipo/producto/monto, evaluación con desempate por Id, monto mínimo tras descuentos (justo en el mínimo y un céntimo menos), segmento ajeno, inactiva y fechas límite, ejemplos Acme 4037.50 y Beta 4275.00 |
| 2026-10-15 | A4 | ✅ | migración `Promociones` (con `Designer` y snapshot) solo crea la tabla; aplicada en Neon |
| 2026-10-15 | Producción | ✅ | `POST https://titanglobal-erp.vercel.app/api/v1/promociones/evaluar` con 2 Laptop Pro a 2500 para Acme → 200, total 4037.50 |
| 2026-10-15 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO sin hallazgos |
