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

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `nombre` tras `Trim()`: vacío → 400 «El nombre es obligatorio.»; más de 100 → 400 «El nombre no puede superar 100 caracteres.»
2. `documentoLicencia` tras `Trim()`: vacío → 400 «El documento de licencia es obligatorio.»; más de 20 → 400 «El documento de licencia no puede superar 20 caracteres.»
3. Id inexistente en PUT (se comprueba antes de la fecha, que en la modificación depende de lo guardado), GET o DELETE → 404 «El conductor {id} no existe.»
4. `vencimientoLicencia`: se normaliza a UTC con `FechasUtc.Normalizar` (una fecha sin zona como `2027-12-31` se interpreta como UTC y no cambia de día). Anterior al día de hoy (UTC) → 400 «La fecha de vencimiento de la licencia no es válida.» En el alta siempre se exige; en la modificación solo si la fecha **cambia** respecto a la guardada, de modo que un conductor con la licencia vencida se puede editar o desactivar conservando su fecha.
5. `observaciones` tras `Trim()`: vacías → `null`; más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.»
6. Documento usado por otro conductor → 409 «Ya existe un conductor con el documento de licencia {documentoLicencia}.» También en la carrera (`23505`). Un PUT que mantiene su propio documento es válido.

**Paginación inválida** (`GET` de listado): `pagina < 1` → 400 «El número de página debe ser mayor que cero.»; `tamanoPagina` fuera de 1–100 → 400 «El tamaño de página debe estar entre 1 y 100.»

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
