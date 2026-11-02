# Especificación: 48 - Modulo42_Conductores (backend)

## 1. Objetivo
**Problema:** la flota (módulo 41) tiene vehículos, pero sin conductores no se puede saber quién conduce ni si la licencia está al día.
**Resultado esperado:** una entidad nueva `Conductor` con su tabla y `Modules/Modulo42_Conductores/` en `api/v1/conductores`:
- listado con búsqueda, filtro y paginación;
- detalle;
- alta, modificación y baja;
- licencia con fecha de vencimiento y habilitación.

Sigue el patrón de `Modulo41_VehiculosFlota` (spec 47). Responsable: Kelvin Pinto.

## 2. Fuero de alcance
- Cambios en las entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`).
- Relación con `VehiculoFlota`: llega con su propio spec.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `src/TitanGlobal.Domain/Models/Conductor.cs` | crear |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | modificar |
| `src/TitanGlobal.Infrastructure/Migrations/*_Conductores*.cs` + snapshot | crear |
| `src/TitanGlobal.Api/Modules/Modulo42_Conductores/{Controllers,Dtos,UseCases}/` | crear |
| `scripts/seed-dev.sql` | modificar |
| `scripts/smoke-postgres.sh` | modificar |

## 4. Diseño y lógica
**Entidad `Conductor`:** `Id`, `Nombre` (req, máx. 100), `DocumentoLicencia` (req, máx. 20, índice único), `VencimientoLicencia` (DateTime, req), `Habilitado` (bool, por defecto `true`), `Activo` (bool, por defecto `true`), `Observaciones` (opcional, máx. 500).

**Contrato.**
| Verbo | Ruta | OK | Errores |
|---|---|---|---|
| GET | `api/v1/conductores?busqueda=&habilitado=&pagina=1&tamanoPagina=20` | 200 `PaginaConductoresDto` | 400 si paginación inválida |
| GET | `api/v1/conductores/{id:int}` | 200 `ConductorDetalleDto` | 404 |
| POST | `api/v1/conductores` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/conductores/{id:int}` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/conductores/{id:int}` | 204 | 404 |

## 5. Criterios de aceptación
| # | Criterio | Umbral |
|---|---|---|
| A1 | Gate | `./scripts/verify.sh` en verde |
| A2 | Reglas | tests con cada mensaje de §4 |
| A3 | Consultas | tests de listado, detalle, paginación |
| A4 | Migración | `dotnet ef migrations add Conductores`, aplica sin errores |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` → `SMOKE OK` |

## 6. Checklist de ejecución
- [ ] Tarea 1: entidad, `DbContext` y migración.
- [ ] Tarea 2: DTOs, UseCases y controlador.
- [ ] Tarea 3: tests, seed y smoke.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| AAAA-MM-DD | A1 | ✅ / ❌ | … |
