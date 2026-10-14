# Especificación: 07 - Modulo01_Clientes (backend)

## 1. Objetivo
**Problema:** el maestro de clientes (módulo 01) no tiene API. Las órdenes de compra referencian clientes, pero no hay forma de darlos de alta, consultarlos ni mantenerlos.
**Resultado esperado:** `Modules/Modulo01_Clientes/` ofrece en `api/v1/clientes`:
- listado con búsqueda y paginación;
- detalle con indicadores comerciales;
- alta, modificación y baja con reglas de negocio (RUC único y de 11 dígitos; no se borra un cliente con órdenes);
- un resumen analítico.

Sigue el patrón del módulo de referencia (`Modulo04_OrdenesCompra`) y los errores de negocio de la spec 06.

## 2. Fuera de alcance
- Cambiar `Modulo04_OrdenesCompra` o la entidad `OrdenCompra`.
- Nuevos campos en `Cliente` (email, dirección…). Solo `Nombre`, `Ruc` y `Descripcion`.
- Validar el dígito verificador del RUC contra SUNAT; solo el formato.
- El frontend (spec 08).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador, coherentes con la spec 06: RUC único → 409; cliente con órdenes no se borra → 409.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Modules/Modulo01_Clientes/Dtos/*.cs` | crear | un archivo por DTO (§4) |
| `src/TitanGlobal.Api/Modules/Modulo01_Clientes/UseCases/*.cs` | crear | `ListarClientesUseCase`, `ObtenerClienteUseCase`, `CrearClienteUseCase`, `ActualizarClienteUseCase`, `EliminarClienteUseCase`, `ResumenClientesUseCase` |
| `src/TitanGlobal.Api/Modules/Modulo01_Clientes/Controllers/ClientesController.cs` | crear | `[Route("api/v1/clientes")]`, delgado |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | índice único sobre `Cliente.Ruc` |
| `src/TitanGlobal.Api/Migrations/*_ClientesRucUnico*` | crear | `dotnet ef migrations add ClientesRucUnico` (con Designer y el snapshot actualizado) |
| `scripts/smoke-postgres.sh` | modificar | añadir las rutas de clientes (§5 A5) |
| `tests/TitanGlobal.Api.Tests/Modulo01_Clientes/*.cs` | crear | tests de integración con InMemory |

## 4. Diseño y lógica
**Contrato.** La respuesta JSON va en camelCase y los errores con el formato `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/clientes?busqueda=&pagina=1&tamanoPagina=20` | `busqueda` opcional | 200 `PaginaClientesDto { items: ClienteResumenDto[], total, pagina, tamanoPagina }` | 400 si `pagina < 1` o `tamanoPagina` fuera de 1–100 |
| GET | `api/v1/clientes/{id:int}` | — | 200 `ClienteDetalleDto` | 404 «El cliente {id} no existe.» |
| POST | `api/v1/clientes` | `GuardarClienteRequestDto { nombre, ruc, descripcion? }` | 200 `{ id }` | 400 (validación), 409 (RUC repetido) |
| PUT | `api/v1/clientes/{id:int}` | `GuardarClienteRequestDto` | 200 `{ id }` | 400, 404, 409 (RUC de otro cliente) |
| DELETE | `api/v1/clientes/{id:int}` | — | 204 | 404; 409 si tiene órdenes |
| GET | `api/v1/clientes/resumen` | — | 200 `ResumenClientesDto` | — |

**DTOs.**
- `ClienteResumenDto { id, nombre, ruc, totalOrdenes }`
- `ClienteDetalleDto { id, nombre, ruc, descripcion, totalOrdenes, montoTotal, ultimaOrdenFecha? }`
- `ResumenClientesDto { totalClientes, clientesConOrdenes, clientesSinDescripcion, topClientesPorMonto: ClienteMontoDto[] }`
- `ClienteMontoDto { clienteId, nombre, montoTotal, totalOrdenes }`

**Reglas, en `UseCases` con `ReglaNegocioException` (spec 06):**
1. `nombre`: obligatorio tras `Trim()` y como máximo 200 caracteres. Si falta → 400 «El nombre del cliente es obligatorio.»; si es más largo → 400 «El nombre del cliente no puede superar 200 caracteres.»
2. `ruc`: tras `Trim()`, exactamente 11 dígitos (0–9) → si no, 400 «El RUC debe tener 11 dígitos numéricos.»
3. RUC ya usado por otro cliente → 409 «Ya existe un cliente con el RUC {ruc}.» También si salta el índice único en la carrera (`23505`, como en la spec 06).
4. `descripcion`: opcional; si llega en blanco se guarda `null`.
5. Baja de un cliente con órdenes (`AnyAsync`) → 409 «No se puede eliminar el cliente {id} porque tiene órdenes de compra.»
6. Id inexistente en GET, PUT o DELETE → 404 «El cliente {id} no existe.»

**Consultas.** Todas parten de `Repository<T>().AsQueryable()`, son asíncronas y proyectan a DTO en la consulta, sin cargar entidades completas en memoria.
- **Listado:** `busqueda` filtra sin distinguir mayúsculas por `Nombre` o `Ruc`; orden por `Nombre` y después `Id`; `Skip`/`Take`; `total` con `CountAsync` sobre el mismo filtro.
- **Detalle:** `montoTotal` es la suma de `Total` de sus órdenes (0 si no tiene); `ultimaOrdenFecha` es la fecha máxima o `null`.
- **Resumen:** `clientesConOrdenes` con `Any`; `clientesSinDescripcion` con `string.IsNullOrEmpty`; el top 5 por monto se ordena por monto descendente y después `Id`, e incluye solo clientes con órdenes.

**Código.** Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08, sin forzarlos.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código (400, 404, 409), con el texto literal y sin `Detail`; RUC con espacios alrededor se acepta y se guarda sin ellos; RUC de 10 dígitos o con letras → 400; PUT que mantiene su propio RUC → 200 | pasan |
| A3 | Consultas | tests con datos conocidos: búsqueda por parte del nombre y por RUC, paginación (`total` correcto y página 2), detalle con `montoTotal`/`ultimaOrdenFecha` exactos y con 0/`null` para un cliente sin órdenes, resumen con valores exactos y top ordenado | pasan |
| A4 | Migración | `dotnet ef migrations add` generó `ClientesRucUnico`; `dotnet ef database update` sobre la BD con el seed aplica sin errores | evidencia |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` amplíado: listado con `busqueda=acme`, detalle 1, resumen, POST válido, POST con RUC `20111111111` → 409, DELETE de cliente 1 → 409, DELETE de un cliente recién creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 6 operaciones | presentes |

## 6. Checklist de ejecución
- [ ] Tarea 1: DTOs, UseCases y controlador.
- [ ] Tarea 2: índice único y migración.
- [ ] Tarea 3: tests y smoke.
- [ ] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
