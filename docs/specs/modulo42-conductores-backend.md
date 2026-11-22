# Especificación: 48 - Modulo42_Conductores (backend)

**Estado de revisión: propuesta, pendiente de aprobación de Lukas.**

## 1. Objetivo
**Problema:** la flota (módulo 41) tiene vehículos, pero sin conductores no se puede saber quién conduce ni si la licencia está al día.
**Resultado esperado:** una entidad nueva `Conductor` con su tabla y `Modules/Modulo42_Conductores/` en `api/v1/conductores`:
- listado con búsqueda, filtro y paginación;
- detalle;
- alta, modificación y baja;
- licencia con fecha de vencimiento y habilitación.

Sigue el patrón de `Modulo41_VehiculosFlota` (spec 47). Responsable: Kelvin Pinto.

## 2. Fuera de alcance
- Cambios en las entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`).
- Relación con `VehiculoFlota`: llega con su propio spec.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `src/TitanGlobal.Domain/Models/Conductor.cs` | regenerar mediante scaffold, sin edición manual |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar mediante scaffold |
| `database/001-esquema-inicial.sql` | referencia integrada: no se modifica |
| `database/022-conductores-licencia-unica.sql` | índice único normalizado; ampliación en `modulo42-conductores-integridad.md` |
| `src/TitanGlobal.Application/DTOs/Modulo42_Conductores/` | DTOs |
| `src/TitanGlobal.Application/UseCases/Modulo42_Conductores/` | reglas y casos de uso |
| `src/TitanGlobal.Api/Controllers/Modulo42_Conductores/` | controlador delgado |
| `scripts/seed-dev.sql` | modificar |
| `scripts/smoke-postgres.sh` | modificar |

## 4. Diseño y lógica
**Entidad `Conductor`:** `Id`, `Nombre` (req, máx. 100), `DocumentoLicencia` (req, máx. 20, índice único), `VencimientoLicencia` (DateTime, req), `Habilitado` (bool, por defecto `true`), `Activo` (bool, por defecto `true`), `Observaciones` (opcional, máx. 500).

`Habilitado` conserva el permiso administrativo que ya usa el contrato y el smoke. La vigencia de la licencia se muestra por separado, calculada por día UTC. Editar/reactivar conservando una fecha vencida sigue permitido por la regla 4; la pantalla advierte que el permiso administrativo no acredita una licencia vigente. No se añade un estado efectivo nuevo al contrato sin una decisión aprobada.

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
6. Documento usado por otro conductor, sin distinguir mayúsculas y recortando espacios exteriores → 409 «Ya existe un conductor con el documento de licencia {documentoLicencia}.» También en la carrera (`23505`). Un PUT que mantiene su propio documento o cambia solo sus mayúsculas es válido; conserva el formato solicitado tras Trim.

**Paginación inválida** (`GET` de listado): `pagina < 1` → 400 «El número de página debe ser mayor que cero.»; `tamanoPagina` fuera de 1–100 → 400 «El tamaño de página debe estar entre 1 y 100.»

## 5. Criterios de aceptación
| # | Criterio | Umbral |
|---|---|---|
| A1 | Gate | `./scripts/verify.sh` en verde |
| A2 | Reglas | tests con cada mensaje de §4 |
| A3 | Consultas | tests de listado, detalle, paginación |
| A4 | Esquema y scaffold | scripts aplicados con `aplicar-bd.sh`; scaffold al día y sin edición manual |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` → `SMOKE OK` |
| A6 | OpenAPI | las cinco operaciones del contrato documentadas |

## 6. Checklist de ejecución
- [x] Tarea 1: SQL aplicado y entidad/DbContext regenerados por scaffold.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.

El backend original estaba integrado. La ampliación y la pantalla están verificadas localmente en la rama de Kelvin; quedan pendientes revisión de Lukas, PR, integración y despliegue.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-21 | A1–A3 | Aprobados | `verify.sh`: 1090 pruebas backend y 582 frontend, compilación .NET sin errores/advertencias; 39 pruebas frontend del módulo y siete casos nuevos backend. Vite mantiene el aviso existente de tamaño de bundle |
| 2026-11-21 | A4 | Aprobado | scripts 001/008/011 aplicados en PostgreSQL 16 local, `scaffold.sh` ejecutado y `SCAFFOLD AL DÍA`; sin cambios de modelo generados |
| 2026-11-21 | A5 | Aprobado | `smoke-postgres.sh` → `SMOKE OK`; índice de licencias rechaza escritura directa duplicada con SQLSTATE 23505; seed probado tras cambiar mayúsculas en una transacción revertida, sin duplicar conductores |
| 2026-11-21 | A6 | Aprobado | OpenAPI local: GET/POST de `/api/v1/conductores`, GET/PUT/DELETE de `/api/v1/conductores/{id}` |

### Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/022-conductores-licencia-unica.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1090 pruebas backend y 582 frontend en 74 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales. El primer smoke se detuvo en su precomprobación porque el contenedor local de preparación ocupaba5433. Se cerró con docker compose down y el smoke completo pasó al repetirlo; se conserva el registro inicial.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

## Numeración vigente

El script actual es `database/022-conductores-licencia-unica.sql`. Las verificaciones históricas con011 corresponden al nombre anterior. Preparación local con main actual; propuesta, pendiente de aprobación de Lukas.
