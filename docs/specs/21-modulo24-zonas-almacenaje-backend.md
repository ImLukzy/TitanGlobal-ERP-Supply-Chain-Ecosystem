# Especificación: 21 - Modulo24_ZonasAlmacenaje (backend)

## 1. Objetivo
**Problema:** un almacén (spec 19) no tiene divisiones internas: no se puede saber dónde se recibe, se guarda o se despacha la mercadería, ni cuánta capacidad del almacén está asignada.
**Resultado esperado:** entidad nueva `ZonaAlmacenaje` y `Modules/Modulo24_ZonasAlmacenaje/` en `api/v1/zonas-almacenaje`: listado con filtros y paginación, detalle, alta, modificación, baja y resumen de ocupación por almacén. Un almacén con zonas ya no se puede borrar.

Decisión del humano (2026-10-15): seguir con el siguiente módulo.

## 2. Fuera de alcance
- Ubicaciones, stock y movimientos (módulos 25–30).
- Cambios en las 4 entidades del enunciado.
- El frontend (spec 22).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador: el código de zona es único dentro de su almacén; la suma de capacidad de las zonas activas no puede superar la del almacén; no se crean zonas en almacenes inactivos.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/ZonaAlmacenaje.cs` | crear | `Almacen` gana la colección `Zonas` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`, índice único compuesto (`AlmacenId`, `Codigo`), longitudes, `decimal(18,2)`, FK restrict a `Almacen` |
| `src/TitanGlobal.Api/Migrations/*_ZonasAlmacenaje*.cs` + snapshot | crear | `dotnet ef migrations add ZonasAlmacenaje -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo24_ZonasAlmacenaje/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/zonas-almacenaje")]`; helper estático `ValidacionZonaAlmacenaje` |
| `src/TitanGlobal.Api/Modules/Modulo23_Almacenes/UseCases/EliminarAlmacenUseCase.cs` (+ tests) | modificar | 409 si el almacén tiene zonas (§4 regla 10) |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo24_ZonasAlmacenaje/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidad `ZonaAlmacenaje`:** `Id`, `AlmacenId` (requerido), `Codigo` (requerido, máx. 20, en mayúsculas, único por almacén), `Nombre` (requerido, máx. 100), `Tipo` (máx. 20: `Recepcion`, `Almacenamiento`, `Picking`, `Despacho`, `Cuarentena`), `Temperatura` (máx. 20: `Ambiente`, `Refrigerada`, `Congelada`), `CapacidadM3` (`decimal(18,2)`, > 0), `Activa` (bool, por defecto `true`).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/zonas-almacenaje?almacenId=&busqueda=&tipo=&temperatura=&activa=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaZonasDto { items: ZonaListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «El tipo de zona no es válido.»; «La temperatura no es válida.» |
| GET | `api/v1/zonas-almacenaje/{id:int}` | — | 200 `ZonaDetalleDto` | 404 «La zona {id} no existe.» |
| POST | `api/v1/zonas-almacenaje` | `GuardarZonaRequestDto { almacenId, codigo, nombre, tipo, temperatura, capacidadM3, activa }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/zonas-almacenaje/{id:int}` | `GuardarZonaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/zonas-almacenaje/{id:int}` | — | 204 | 404 |
| GET | `api/v1/zonas-almacenaje/resumen?almacenId=` | `almacenId` opcional | 200 `ResumenZonasDto` | 400 si el almacén no existe |

**DTOs.**
- `ZonaListadoDto { id, almacenId, almacenCodigo, codigo, nombre, tipo, temperatura, capacidadM3, activa }`
- `ZonaDetalleDto { id, almacenId, almacenCodigo, almacenNombre, codigo, nombre, tipo, temperatura, capacidadM3, activa }`
- `ResumenZonasDto { totalZonas, activas, porTipo: TipoZonaDto[], ocupacion: OcupacionAlmacenDto[] }`
- `TipoZonaDto { tipo, totalZonas, capacidadM3 }` en el orden de la lista de tipos (solo tipos con zonas).
- `OcupacionAlmacenDto { almacenId, almacenCodigo, capacidadAlmacenM3, capacidadAsignadaM3, porcentajeAsignado }`: capacidad asignada = suma de zonas activas; porcentaje con 2 decimales (0 si el almacén no tiene capacidad asignada); por `almacenCodigo`. Si llega `almacenId`, todo el resumen se limita a ese almacén.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `almacenId` inexistente → 400 «El almacén {almacenId} no existe.»; almacén inactivo al crear o al mover una zona a él → 409 «El almacén {codigo} está inactivo.»
2. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código de zona es obligatorio.»; no cumple `^[A-Z0-9-]{1,20}$` → 400 «El código de zona solo admite letras, números y guiones (1 a 20 caracteres).»
3. `nombre` tras `Trim()`: vacío → 400 «El nombre de la zona es obligatorio.»; más de 100 → 400 «El nombre de la zona no puede superar 100 caracteres.»
4. `tipo` válido sin distinguir mayúsculas y guardado canónico → si no, 400 «El tipo de zona no es válido.»
5. `temperatura` igual → si no, 400 «La temperatura no es válida.»
6. `capacidadM3 <= 0` → 400 «La capacidad debe ser mayor que 0.»
7. Código repetido en el mismo almacén → 409 «Ya existe la zona {codigo} en el almacén {almacenCodigo}.» (también en la carrera, `23505`). El mismo código en otro almacén es válido; un PUT que mantiene su código es válido.
8. Si la zona queda activa: capacidad de las demás zonas activas del almacén + la suya > capacidad del almacén → 409 «La capacidad de las zonas activas ({total} m³) superaría la del almacén {almacenCodigo} ({capacidad} m³).» (importes con 2 decimales y punto).
9. Id inexistente → 404 «La zona {id} no existe.»
10. Borrar un almacén que tiene zonas (activas o no) → 409 «No se puede eliminar el almacén {id} porque tiene zonas.»

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas y proyectadas a DTO. Búsqueda por código o nombre sin distinguir mayúsculas. Orden por código de almacén, código de zona e `Id`. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 8 zonas sobre las capacidades del seed de la spec 19 (`ALM-LIM-01` 500, `ALM-ARE-01` 150, `ALM-CAL-TR` 75 m³): `ALM-LIM-01` con `REC-01` Recepcion 50, `ALM-A` Almacenamiento 300, `PIC-01` Picking 60 y `DES-01` Despacho 40 (450 de 500 = 90.00 %, sirve para ver «Casi lleno»); `ALM-ARE-01` con `ALM-A` Almacenamiento 100 y `FRIO-01` Almacenamiento Refrigerada 30 (130 de 150); `ALM-CAL-TR` con `TRAN-01` Recepcion 60 y `CUA-01` Cuarentena 20 inactiva (60 de 75: la inactiva no cuenta). Ninguno supera su capacidad.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; mismo código en otro almacén válido; capacidad justo igual al límite → 200 y superada → 409; zona inactiva no cuenta para la capacidad; borrar almacén con zonas → 409; ningún caso inválido crea o cambia filas | pasan |
| A3 | Consultas | tests: filtros por almacén, tipo, temperatura, `activa` y búsqueda; orden; paginación; resumen general y por almacén con porcentajes exactos | pasan |
| A4 | Migración | `ZonasAlmacenaje` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado `almacenId=1`, `tipo=picking`, detalle, resumen con `almacenId=1`, POST válido, POST que supera la capacidad → 409, código repetido → 409, DELETE del almacén 1 → 409, DELETE de la zona creada → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 6 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidad, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases, controlador y regla 10 en Almacenes.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`517862c`, mismas líneas que el auditado `fd4e439`): backend 230/230, frontend 169/169 |
| 2026-10-15 | A2–A3 | ✅ | `ZonasAlmacenajeApiTests` y `AlmacenesApiTests`: mensajes literales, código por almacén, capacidad igual al límite → 200 y superada → 409, zona inactiva no cuenta, borrar almacén con zonas → 409, filtros, orden y resumen con porcentajes |
| 2026-10-15 | A4 | ✅ | migración `ZonasAlmacenaje` (con `Designer` y snapshot) solo crea la tabla con FK restrictiva e índice único compuesto; aplicada en Neon antes de subir el código |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: detalle REC-01, resumen del almacén 1 (4 zonas), POST → `{id:9}`, superar capacidad → 409 «La capacidad de las zonas activas (510.00 m³) superaría la del almacén ALM-LIM-01…», código repetido → 409, DELETE almacén 1 → 409, DELETE de la zona creada → 204 |
| 2026-10-15 | Producción | ✅ | `https://titanglobal-erp.vercel.app/api/v1/zonas-almacenaje/resumen?almacenId=1` → 200 (4 zonas); DELETE del almacén 1 → 409 literal sin borrar nada |
| 2026-10-15 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | QA E2E | ✅ | Pam en local (Vite 5174 + API + PostgreSQL) y en producción: ocupación con `role="meter"` (ALM-LIM-01 90.00 % «Casi lleno», ALM-ARE-01 86.67 % sin aviso, ALM-CAL-TR sin contar la inactiva); `?almacen=` y filtros; una sola petición al cargar; alta hasta el 100 % → 200 y 1 m³ más → 409 literal conservando lo escrito; código repetido en el mismo almacén → 409 y permitido en otro; inactivar baja la ocupación; baja con Escape; borrar ALM-LIM-01 en Almacenes → 409 en el diálogo; sin regresiones; 13 capturas sin recortes, palabras partidas ni scroll horizontal |
