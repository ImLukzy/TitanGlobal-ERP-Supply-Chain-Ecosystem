# Especificación: 47 - Modulo41_VehiculosFlota (backend)

## 1. Objetivo
**Problema:** el dominio de Logística, Flota y Distribución (módulos 41–50) no tiene ningún maestro. Sin vehículos no se pueden construir después rutas de despacho, guías de remisión ni control de combustible.
**Resultado esperado:** una entidad nueva `VehiculoFlota` con su tabla y `Modules/Modulo41_VehiculosFlota/` en `api/v1/vehiculos-flota`:
- listado con búsqueda, filtros y paginación;
- detalle;
- alta, modificación y baja;
- tipos únicos;
- resumen.

Sigue el patrón de `Modulo11_Proveedores` (spec 12). Responsable: Kelvin Pinto.

## 2. Fuera de alcance
- Cambios en las 4 entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`).
- Relaciones con otros módulos (conductores, rutas, guías de remisión): llegarán con sus specs.
- El frontend (spec siguiente).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Tipos válidos fijados: `Camion`, `Furgon`, `Moto`, `Otro`.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Domain/Models/VehiculoFlota.cs` | crear | entidad `partial`, estilo de `Proveedor.cs` |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | modificar | `DbSet<VehiculoFlota> VehiculosFlota`; índice único en `Placa`; longitudes máximas (§4) |
| `src/TitanGlobal.Infrastructure/Migrations/*_VehiculosFlota*.cs` + snapshot | crear | generada con `dotnet ef migrations add VehiculosFlota -p src/TitanGlobal.Api` (nunca a mano) |
| `src/TitanGlobal.Api/Modules/Modulo41_VehiculosFlota/{Controllers,Dtos,UseCases}/` | crear | un archivo por clase; `[Route("api/v1/vehiculos-flota")]`, controlador delgado |
| `scripts/seed-dev.sql` | modificar | 6 vehículos idempotentes (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas de vehiculos-flota (§5 A5) |
| `tests/TitanGlobal.Api.Tests/Modulo41_VehiculosFlota/*.cs` | crear | tests de integración con InMemory |

## 4. Diseño y lógica
**Entidad `VehiculoFlota`:** `Id`, `Placa` (requerida, máx. 20, índice único), `Marca` (requerida, máx. 100), `Modelo` (requerido, máx. 100), `Anio` (int, requerido), `Tipo` (requerido, máx. 50), `CapacidadKg` (decimal(18,2), requerida), `Activo` (bool, por defecto `true`), `Observaciones` (opcional, máx. 500).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/vehiculos-flota?busqueda=&tipo=&activo=&pagina=1&tamanoPagina=20` | todos opcionales salvo paginación; `activo` = `true`/`false` | 200 `PaginaVehiculosFlotaDto { items: VehiculoFlotaListadoDto[], total, pagina, tamanoPagina }` | 400 si `pagina < 1` o `tamanoPagina` fuera de 1–100 |
| GET | `api/v1/vehiculos-flota/{id:int}` | — | 200 `VehiculoFlotaDetalleDto` | 404 «El vehículo {id} no existe.» |
| POST | `api/v1/vehiculos-flota` | `GuardarVehiculoFlotaRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/vehiculos-flota/{id:int}` | `GuardarVehiculoFlotaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/vehiculos-flota/{id:int}` | — | 204 | 404 |
| GET | `api/v1/vehiculos-flota/tipos` | — | 200 `string[]` (tipos distintos, ordenados) | — |
| GET | `api/v1/vehiculos-flota/resumen` | — | 200 `ResumenVehiculosFlotaDto` | — |

**DTOs.**
- `VehiculoFlotaListadoDto { id, placa, marca, modelo, anio, tipo, capacidadKg, activo }`
- `VehiculoFlotaDetalleDto { id, placa, marca, modelo, anio, tipo, capacidadKg, activo, observaciones }`
- `GuardarVehiculoFlotaRequestDto { placa, marca, modelo, anio, tipo, capacidadKg, activo, observaciones? }`
- `ResumenVehiculosFlotaDto { totalVehiculos, activos, inactivos, capacidadTotalKg, porTipo: TipoVehiculoDto[] }`
- `TipoVehiculoDto { tipo, totalVehiculos, activos }`, ordenado por `totalVehiculos` descendente y después `tipo`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `placa` tras `Trim()`: vacía → 400 «La placa es obligatoria.»; más de 20 → 400 «La placa no puede superar 20 caracteres.»
2. `marca` tras `Trim()`: vacía → 400 «La marca es obligatoria.»; más de 100 → 400 «La marca no puede superar 100 caracteres.»
3. `modelo` tras `Trim()`: vacío → 400 «El modelo es obligatorio.»; más de 100 → 400 «El modelo no puede superar 100 caracteres.»
4. `anio`: menor que 1900 o mayor que año actual + 1 → 400 «El año del vehículo no es válido.»
5. `tipo` tras `Trim()`: vacío → 400 «El tipo es obligatorio.»; valor no es `Camion`, `Furgon`, `Moto` u `Otro` → 400 «El tipo debe ser Camion, Furgon, Moto u Otro.»
6. `capacidadKg`: menor o igual a 0 → 400 «La capacidad debe ser mayor que cero.»
7. Placa usada por otro vehículo → 409 «Ya existe un vehículo con la placa {placa}.» También en la carrera (`23505`). Un PUT que mantiene su propia placa es válido.
8. Id inexistente en GET, PUT o DELETE → 404 «El vehículo {id} no existe.»

**Consultas.** Desde `Repository<VehiculoFlota>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta.
- `busqueda` filtra sin distinguir mayúsculas por `Placa`, `Marca` o `Modelo`; `tipo` exacto sin distinguir mayúsculas; `activo` si viene.
- Orden por `Placa` y después `Id`.
- Comentarios `// PATRÓN LINQ N` donde aplique.

**Seed de desarrollo** (`seed-dev.sql`, idempotente con `ON CONFLICT` sobre `Placa`): 6 vehículos de distintos tipos, uno inactivo.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; placa con espacios aceptada y guardada sin ellos; PUT con su propia placa → 200 | pasan |
| A3 | Consultas | tests con datos conocidos: búsqueda por placa, marca y modelo; filtro por tipo y activo; paginación; tipos ordenados; resumen exacto | pasan |
| A4 | Migración | `VehiculosFlota` generada con `dotnet ef`; aplica sin errores; no cambia tablas existentes | evidencia |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` ampliado: listado con `tipo=Camion`, `activo=false`, detalle, tipos, resumen, POST válido, POST con placa del seed → 409, DELETE del recién creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [ ] Tarea 1: entidad, `DbContext` y migración.
- [ ] Tarea 2: DTOs, UseCases y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| AAAA-MM-DD | A1 | ✅ / ❌ | … |
