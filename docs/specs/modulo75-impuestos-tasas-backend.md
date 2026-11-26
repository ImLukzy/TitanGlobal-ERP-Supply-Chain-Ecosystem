# Especificación: modulo75-impuestos-tasas-backend — Impuestos y tasas

> Responsable: Leonel Portillo. Backend integrado en `main`. El ajuste del día de negocio de Perú se presenta para revisión.

## 1. Objetivo

Mantener el catálogo de impuestos, sus periodos de vigencia y el cálculo de una tasa para una fecha indicada. Los comprobantes pueden tomar el porcentaje del catálogo y conservarlo en sus líneas.

## 2. Fuera de alcance

- Determinación automática de obligaciones tributarias, envío a servicios fiscales y reglas de retención por operación.
- Cierre o edición de tasas ya registradas, tasa 0 %, paginación o resumen de impuestos y fórmulas diferenciadas por tipo. Son precisiones pendientes del [relevo](../equipo/relevo.md).
- Pantalla React, especificada aparte. El cálculo y los permisos siguen el contrato integrado.

## 3. Archivos afectados

| Ruta | Alcance |
|---|---|
| `database/001-esquema-inicial.sql` | Esquema integrado de `Impuestos` y `TasasImpuestos`. |
| `src/TitanGlobal.Domain/Models/{Impuesto,TasaImpuesto}.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Modelos generados por `scaffold.sh`. |
| `src/TitanGlobal.Api/Controllers/Modulo75_ImpuestosTasas/ImpuestosController.cs` | Contrato REST integrado. |
| `src/TitanGlobal.Application/DTOs/Modulo75_ImpuestosTasas/` | Entradas y salidas. |
| `src/TitanGlobal.Application/UseCases/Modulo75_ImpuestosTasas/ListarImpuestosUseCase.cs` | Selección de tasa vigente en la fecha de Perú. |
| `src/TitanGlobal.Domain/Common/FechaNegocioPeru.cs` | Conversión del instante UTC a día de negocio. |
| `tests/TitanGlobal.Api.Tests/Modulo75_ImpuestosTasas/ImpuestosApiTests.cs` | Pruebas del contrato y del límite de medianoche. |

## 4. Diseño y lógica

### Persistencia

`Impuestos` tiene Id entero, código único de hasta 30 caracteres, nombre de hasta 120, tipo, país, descripción opcional de hasta 250, indicador de actividad y fecha de creación en UTC. `TasasImpuestos` tiene Id entero y FK a `Impuestos`. Almacena porcentaje `numeric(18,6)`, inicio, fin opcional, actividad y observación opcional de hasta 250 caracteres. El modelo se genera desde PostgreSQL. El backend guarda las vigencias como días de calendario en la representación existente de medianoche UTC.

### Contrato HTTP

Las rutas llevan el prefijo `/api/v1/impuestos`. La política global exige JWT para lectura y escritura, con el control de acceso general. El módulo aplica Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL.

| Método | Ruta | Entrada | Respuesta |
|---|---|---|---|
| GET | `/` | — | 200, lista de `ImpuestoListadoDto` con porcentaje e inicio de vigencia opcionales. |
| GET | `/{id}` | Id | 200, `ImpuestoDetalleDto` con su historial de tasas. |
| POST | `/` | `GuardarImpuestoRequestDto` | 201, `{ id }` y ubicación del recurso. |
| PUT | `/{id}` | `GuardarImpuestoRequestDto` | 204. Puede cambiar la actividad. |
| DELETE | `/{id}` | Id | 204. Desactivación lógica. |
| POST | `/{id}/tasas` | `GuardarTasaImpuestoRequestDto` | 201, `{ id, impuestoId }`. |
| POST | `/calcular` | `CalcularImpuestoRequestDto` | 200, `CalcularImpuestoResponseDto`. |

- Guardar impuesto recibe `codigo`, `nombre`, `tipo`, `descripcion?`, `pais` y `activo`. Se recortan los identificadores y se normalizan código, tipo y país a mayúsculas. El país tiene tres caracteres. Tipos: `IGV`, `IVA`, `RETENCION`, `PERCEPCION` y `DETRACCION`.
- Listado devuelve `id`, `codigo`, `nombre`, `tipo`, `pais`, `activo`, `porcentajeVigente?` y `fechaInicioVigencia?`. Detalle devuelve los campos del impuesto y `tasas` con `id`, `porcentaje`, `fechaInicio`, `fechaFin?`, `activo` y `observacion?`, de más reciente a más antigua.
- Registrar tasa recibe `porcentaje`, `fechaInicio`, `fechaFin?` y `observacion?`. Impuesto activo, porcentaje mayor a 0 y hasta 100. Ambos extremos de la vigencia son inclusivos. El fin admite el mismo día o uno posterior al inicio. Las tasas activas de un impuesto se validan contra periodos superpuestos.
- El listado usa la fecha actual de Perú, que cambia a las **05:00 UTC**. Para cálculo y registro se conserva el día explícito de la solicitud. Los consumidores 51 y 52 tienen su propio contrato de emisión y conservan el porcentaje histórico.
- Calcular recibe `impuestoId`, `baseImponible` y `fechaOperacion`. Redondea la base a dos decimales y selecciona la tasa activa más reciente vigente en ese día. Redondea el importe con `MidpointRounding.AwayFromZero`. Devuelve impuesto, código, nombre, base, porcentaje, importe, total `base + importe` y día normalizado. La fórmula es la operación de cálculo actual de la API.

### Mensajes literales

Los errores de negocio devuelven `StatusCode` y `Message`. El acceso y el cuerpo de solicitud usan los mensajes del pipeline común.

| HTTP | Mensaje |
|---|---|
| 400 | El código del impuesto es obligatorio. |
| 400 | El código del impuesto no puede superar 30 caracteres. |
| 400 | El nombre del impuesto es obligatorio. |
| 400 | El nombre del impuesto no puede superar 120 caracteres. |
| 400 | El tipo de impuesto no es válido. |
| 400 | El país debe indicarse mediante un código de tres caracteres. |
| 400 | La descripción del impuesto no puede superar 250 caracteres. |
| 400 | La fecha de inicio es obligatoria. |
| 400 | El porcentaje debe ser mayor que 0 y no superar 100. |
| 400 | La fecha final no puede ser anterior a la fecha inicial. |
| 400 | La observación de la tasa no puede superar 250 caracteres. |
| 400 | Ya existe una tasa activa para ese periodo. |
| 400 | La base imponible debe ser mayor que cero. |
| 400 | La base imponible es demasiado grande. |
| 400 | La fecha de operación es obligatoria. |
| 400 | No existe una tasa vigente para la fecha indicada. |
| 404 | No se encontró el impuesto solicitado. |
| 404 | El impuesto no existe o está inactivo. |
| 409 | Ya existe un impuesto con el código {codigo}. |

## 5. Criterios de aceptación

| # | Criterio | Evidencia |
|---|---|---|
| A1 | Compila y cumple las reglas de arquitectura | `dotnet build -warnaserror` y `./scripts/check-rules.sh`. |
| A2 | Validación, permisos, errores literales y vigencias | Pruebas HTTP de `ImpuestosApiTests` y smoke existente. |
| A3 | Cambia la vigencia a medianoche de Perú | Pruebas de 23:59:59, 00:00:00, 04:59:59 y 05:00:00 UTC con instantes fijos. |
| A4 | SQL y persistencia reproducibles | Dos cargas del seed y `./scripts/smoke-postgres.sh`, que comprueba también el scaffold. |
| A5 | Gate completo | `./scripts/verify.sh`. |

## 6. Checklist de ejecución

- [x] Contrastar contrato y límites de entrada con el backend integrado.
- [x] Añadir casos deterministas del corte de vigencia.
- [ ] Registrar evidencia de la rama correctiva.

## 7. Registro de verificación

| Fecha | Evidencia | Resultado |
|---|---|---|

2026-11-26. `git diff --check`, `check-rules.sh` y `dotnet build -warnaserror` sin errores. `verify.sh` pasó 1763 pruebas backend (61 del módulo) y 1455 frontend. Se comprobó SQL 001 existente, scaffold al día y el seed en dos cargas en PostgreSQL 16 desechable, con `SMOKE OK`.
