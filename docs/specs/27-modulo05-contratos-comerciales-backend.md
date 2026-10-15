# Especificación: 27 - Modulo05_ContratosComerciales (backend)

## 1. Objetivo
**Problema:** Comercial no registra los acuerdos marco con sus clientes (monto comprometido para un periodo y condiciones de pago), ni puede ver cuánto de ese compromiso se ha consumido con órdenes reales.
**Resultado esperado:** entidad nueva `ContratoComercial` y `Modules/Modulo05_ContratosComerciales/` en `api/v1/contratos-comerciales`:
- contratos con ciclo Borrador → Vigente → Finalizado, o Cancelado con motivo;
- **consumo calculado**: suma de las órdenes de compra del cliente (módulo 04) cuya fecha cae dentro del periodo del contrato;
- listado con filtros, detalle con consumo y órdenes del periodo, y resumen.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Cambios en las 4 entidades del enunciado (relación con `Cliente` sin navegación inversa; el consumo solo lee `OrdenCompra`).
- Facturación, cuentas por cobrar (51, 53) o renovación automática.
- El frontend (spec 28).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador: un cliente no puede tener dos contratos vigentes con periodos que se solapen; solo un borrador se edita o se borra; el consumo es informativo (no bloquea órdenes).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/ContratoComercial.cs` | crear | FK a `Cliente` con `HasOne(...).WithMany()` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`, índice único en `Numero`, longitudes, `decimal(18,2)`, FK restrict |
| `src/TitanGlobal.Api/Migrations/*_ContratosComerciales*.cs` + snapshot | crear | `dotnet ef migrations add ContratosComerciales -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo05_ContratosComerciales/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/contratos-comerciales")]`; helper estático `ValidacionContratoComercial`; `activar` y `finalizar` con `[PermitirCuerpoVacio]` |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo05_ContratosComerciales/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidad `ContratoComercial`:** `Id`, `Numero` (máx. 20, único; `CC-` + 6 dígitos del Id, atómico como en la spec 14), `ClienteId`, `Titulo` (requerido, máx. 200), `FechaInicio` y `FechaFin` (fechas en UTC, sin hora: se guardan a las 00:00 UTC), `MontoComprometido` (`decimal(18,2)`, > 0), `CondicionPago` (máx. 20: `Contado`, `Credito30`, `Credito60`, `Credito90`), `Estado` (máx. 20: `Borrador`, `Vigente`, `Finalizado`, `Cancelado`), `Observaciones` (máx. 500), `MotivoCancelacion` (máx. 500), `FechaActivacion` y `FechaCierre` (UTC, opcionales).

**Consumo.** `ordenesPeriodo` = órdenes del cliente con `Fecha` entre `FechaInicio` (incluida) y el final del día de `FechaFin` (incluido); `montoConsumido` = suma de su `Total` (0 si no hay); `porcentajeConsumido` = `montoConsumido / MontoComprometido × 100` con 2 decimales (puede pasar de 100); `saldo` = `MontoComprometido − montoConsumido` (puede ser negativo). `vencido` = `Estado == Vigente` y `FechaFin` anterior a hoy (UTC).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/contratos-comerciales?busqueda=&estado=&clienteId=&vencidos=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaContratosDto { items: ContratoListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «El estado no es válido.» |
| GET | `api/v1/contratos-comerciales/{id:int}` | — | 200 `ContratoDetalleDto` | 404 «El contrato {id} no existe.» |
| POST | `api/v1/contratos-comerciales` | `GuardarContratoRequestDto { clienteId, titulo, fechaInicio, fechaFin, montoComprometido, condicionPago, observaciones? }` | 200 `{ id, numero }` (estado Borrador) | 400 |
| PUT | `api/v1/contratos-comerciales/{id:int}` | `GuardarContratoRequestDto` | 200 `{ id, numero }` | 400, 404, 409 |
| POST | `api/v1/contratos-comerciales/{id:int}/activar` | sin cuerpo | 200 `ContratoDetalleDto` | 404, 409 |
| POST | `api/v1/contratos-comerciales/{id:int}/finalizar` | sin cuerpo | 200 `ContratoDetalleDto` | 404, 409 |
| POST | `api/v1/contratos-comerciales/{id:int}/cancelar` | `CancelarContratoRequestDto { motivo }` | 200 `ContratoDetalleDto` | 400, 404, 409 |
| DELETE | `api/v1/contratos-comerciales/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/contratos-comerciales/resumen` | — | 200 `ResumenContratosDto` | — |

**DTOs.**
- `ContratoListadoDto { id, numero, clienteId, clienteNombre, titulo, fechaInicio, fechaFin, estado, vencido, montoComprometido, montoConsumido, porcentajeConsumido }`
- `ContratoDetalleDto` = listado + `condicionPago, observaciones, motivoCancelacion, fechaActivacion, fechaCierre, saldo, ordenesPeriodo: OrdenPeriodoDto[]`
- `OrdenPeriodoDto { ordenId, numeroOrden, fecha, total }`, por fecha descendente.
- `ResumenContratosDto { total, borradores, vigentes, vencidos, finalizados, cancelados, montoComprometidoVigente, montoConsumidoVigente, porcentajeConsumoVigente, porCondicion: CondicionContratosDto[] }`: los montos y el porcentaje son solo de los vigentes.
- `CondicionContratosDto { condicionPago, contratos }` en el orden Contado, Credito30, Credito60, Credito90 (solo las que tienen contratos).

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. Cliente inexistente → 400 «El cliente {clienteId} no existe.»
2. `titulo` tras `Trim()`: vacío → 400 «El título es obligatorio.»; más de 200 → 400 «El título no puede superar 200 caracteres.»
3. `fechaFin` anterior o igual a `fechaInicio` → 400 «La fecha de fin debe ser posterior a la de inicio.»
4. `montoComprometido <= 0` → 400 «El monto comprometido debe ser mayor que 0.»
5. `condicionPago` válida sin distinguir mayúsculas y guardada canónica → si no, 400 «La condición de pago no es válida.»
6. `observaciones` en blanco → `null`; más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»
7. Id inexistente → 404 «El contrato {id} no existe.»
8. PUT o DELETE sobre un contrato que no es `Borrador` → 409 «El contrato {numero} ya está {estado en minúsculas}.» Activar solo desde `Borrador`; finalizar solo desde `Vigente`; cancelar desde `Borrador` o `Vigente`; en otro caso, el mismo 409.
9. Activar con `FechaFin` anterior a hoy → 409 «El contrato {numero} ya terminó su periodo.»; si el cliente tiene otro contrato `Vigente` cuyo periodo se solapa → 409 «El cliente ya tiene el contrato vigente {numeroOtro} en ese periodo.»
10. Cancelar: `motivo` vacío → 400 «El motivo de cancelación es obligatorio.»; más de 500 → «El motivo de cancelación no puede superar 500 caracteres.»
11. Activar guarda `FechaActivacion`; finalizar y cancelar guardan `FechaCierre` (UTC).
- Filtros: `estado` sin distinguir mayúsculas; `busqueda` por número, título o nombre de cliente; `vencidos=true`. Orden: `FechaInicio` desc, `Id` desc.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas a DTO. El consumo del listado se calcula en la misma consulta (subconsulta sobre `OrdenCompra` por cliente y periodo), sin una consulta por contrato. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (filtro por fecha, `Sum`, navegación).

**Seed:** 3 contratos (`CC-DEV-00N`): Acme Industrial vigente del 2026-01-01 al 2026-12-31 por 20,000.00 a Credito30 (su consumo sale de sus órdenes de 2026); Beta Comercio en borrador para 2027 por 8,000.00 a Contado; Gamma Manufactura cancelado con motivo.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; transiciones permitidas y prohibidas; solapamiento de vigentes → 409 (y permitido si no se solapan o el otro está cancelado); ningún caso inválido crea o cambia filas | pasan |
| A3 | Consumo y consultas | tests: órdenes dentro y fuera del periodo (incluidos los límites del primer y último día), porcentaje con 2 decimales y por encima de 100, saldo negativo, `vencido`, filtros, orden, detalle con órdenes del periodo y resumen exactos | pasan |
| A4 | Migración | `ContratosComerciales` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado, detalle del vigente de Acme (consumo de sus órdenes de 2026), resumen, POST de un borrador para Acme en 2026 → activar → 409 por solapamiento; activar el de Beta → 200; finalizar un borrador → 409; cancelar sin motivo → 400; DELETE de un vigente → 409 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 9 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidad, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Revisión del integrador | ⚠️ | la primera entrega (`aec2e1a`) traía 5 tests amplios para ~20 mensajes; Claudio la devolvió. Corrección: 32 tests del módulo sin tocar producción (`71dada7`). Lección añadida en `docs/equipo/lecciones.md` |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`723351c`, mismas líneas que el auditado `71dada7`): backend 304/304, frontend 201/201 |
| 2026-10-15 | A2–A3 | ✅ | `ContratosComercialesApiTests` (32 tests): cada mensaje literal, transiciones, solapamiento (un día compartido ya solapa; contiguos y cancelados no), consumo con límites del primer y último día, porcentaje > 100, saldo negativo, filtros, orden y resumen |
| 2026-10-15 | A4 | ✅ | migración `ContratosComerciales` (con `Designer` y snapshot) solo crea la tabla; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: contratos de Acme con consumo, detalle `CC-DEV-001`, resumen, activar uno solapado → 409 «El cliente ya tiene el contrato vigente CC-DEV-001 en ese periodo.», activar el de Beta → 200, finalizar un borrador → 409, cancelar sin motivo → 400, DELETE de un vigente → 409 |
| 2026-10-15 | A6 | ✅ | el smoke comprueba las 9 operaciones en `/openapi/v1.json` |
| 2026-10-15 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO sin hallazgos |
