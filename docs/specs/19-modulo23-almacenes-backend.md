# Especificación: 19 - Modulo23_Almacenes (backend)

## 1. Objetivo
**Problema:** el dominio de Inventarios y Almacenes (WMS, módulos 21–30) solo tiene el maestro de productos. Sin almacenes no se pueden construir zonas, lotes, kardex, transferencias ni la recepción de mercadería (módulo 19).
**Resultado esperado:** una entidad nueva `Almacen` y `Modules/Modulo23_Almacenes/` en `api/v1/almacenes`: listado con filtros y paginación, detalle, alta, modificación, baja, ciudades y resumen. Sigue el patrón de `Modulo11_Proveedores` (spec 12).

Decisión del humano (2026-10-14): seguir con el siguiente módulo.

## 2. Fuera de alcance
- Stock, zonas y ubicaciones (módulos 24–26), y relaciones con órdenes o recepciones (llegarán con sus specs).
- Cambios en las 4 entidades del enunciado.
- El frontend (spec 20).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador con el criterio de Proveedores: código único; baja física mientras ningún módulo dependa del almacén; `Activo` para retirarlo sin borrarlo.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/Almacen.cs` | crear | entidad `partial` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet<Almacen> Almacenes`; índice único en `Codigo`; longitudes; `decimal(18,2)` en `CapacidadM3` |
| `src/TitanGlobal.Api/Migrations/*_Almacenes*.cs` + snapshot | crear | `dotnet ef migrations add Almacenes -p src/TitanGlobal.Api`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo23_Almacenes/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/almacenes")]`; validación en helper estático `ValidacionAlmacen` |
| `scripts/seed-dev.sql` | modificar | 5 almacenes idempotentes (`ON CONFLICT` sobre `Codigo`) y `setval` |
| `scripts/smoke-postgres.sh` | modificar | rutas de §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo23_Almacenes/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidad `Almacen`:** `Id`, `Codigo` (requerido, máx. 20, único, guardado en mayúsculas), `Nombre` (requerido, máx. 200), `Tipo` (requerido, máx. 20: `Central`, `Regional`, `Transito`), `Direccion` (requerida, máx. 300), `Ciudad` (requerida, máx. 100), `CapacidadM3` (`decimal(18,2)`, > 0), `Responsable` (opcional, máx. 100), `Activo` (bool, por defecto `true`).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/almacenes?busqueda=&ciudad=&tipo=&activo=&pagina=1&tamanoPagina=20` | opcionales salvo paginación; `activo` = `true`/`false` | 200 `PaginaAlmacenesDto { items: AlmacenListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; 400 «El tipo de almacén no es válido.» |
| GET | `api/v1/almacenes/{id:int}` | — | 200 `AlmacenDetalleDto` | 404 «El almacén {id} no existe.» |
| POST | `api/v1/almacenes` | `GuardarAlmacenRequestDto { codigo, nombre, tipo, direccion, ciudad, capacidadM3, responsable?, activo }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/almacenes/{id:int}` | `GuardarAlmacenRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/almacenes/{id:int}` | — | 204 | 404 |
| GET | `api/v1/almacenes/ciudades` | — | 200 `string[]` (distintas, ordenadas) | — |
| GET | `api/v1/almacenes/resumen` | — | 200 `ResumenAlmacenesDto` | — |

**DTOs.**
- `AlmacenListadoDto { id, codigo, nombre, tipo, ciudad, capacidadM3, activo }`
- `AlmacenDetalleDto { id, codigo, nombre, tipo, direccion, ciudad, capacidadM3, responsable, activo }`
- `ResumenAlmacenesDto { totalAlmacenes, activos, inactivos, capacidadTotalM3, capacidadActivaM3, porTipo: TipoAlmacenesDto[], porCiudad: CiudadAlmacenesDto[] }`
- `TipoAlmacenesDto { tipo, totalAlmacenes, capacidadM3 }`, en el orden Central, Regional, Transito (solo tipos con almacenes).
- `CiudadAlmacenesDto { ciudad, totalAlmacenes, activos }`, por `totalAlmacenes` desc y después `ciudad`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código es obligatorio.»; no cumple `^[A-Z0-9-]{2,20}$` → 400 «El código solo admite letras, números y guiones (2 a 20 caracteres).»
2. `nombre` tras `Trim()`: vacío → 400 «El nombre del almacén es obligatorio.»; más de 200 → 400 «El nombre del almacén no puede superar 200 caracteres.»
3. `tipo`: uno de los tres valores, sin distinguir mayúsculas y guardado con su forma canónica → si no, 400 «El tipo de almacén no es válido.»
4. `direccion` tras `Trim()`: vacía → 400 «La dirección es obligatoria.»; más de 300 → 400 «La dirección no puede superar 300 caracteres.»
5. `ciudad` tras `Trim()`: vacía → 400 «La ciudad es obligatoria.»; más de 100 → 400 «La ciudad no puede superar 100 caracteres.»
6. `capacidadM3 <= 0` → 400 «La capacidad debe ser mayor que 0.»
7. `responsable` en blanco → `null`; más de 100 → 400 «El responsable no puede superar 100 caracteres.»
8. Código usado por otro almacén → 409 «Ya existe un almacén con el código {codigo}.» También en la carrera (`23505`). Un PUT que mantiene su propio código es válido.
9. Id inexistente en GET, PUT o DELETE → 404 «El almacén {id} no existe.»

**Consultas.** Desde `Repository<Almacen>().AsQueryable()`, asíncronas y proyectadas a DTO. `busqueda` por código o nombre sin distinguir mayúsculas; `ciudad` exacta sin distinguir mayúsculas; `tipo` validado como en la regla 3. Orden por `Codigo` y después `Id`. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 5 almacenes: `ALM-LIM-01` Central Lima, `ALM-LIM-02` Regional Lima, `ALM-ARE-01` Regional Arequipa, `ALM-TRU-01` Regional Trujillo (inactivo) y `ALM-CAL-TR` Transito Callao (sin responsable).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; código con espacios y minúsculas guardado en mayúsculas; tipo en minúsculas guardado canónico; PUT con su propio código → 200; ningún caso inválido crea o cambia filas | pasan |
| A3 | Consultas | tests con datos conocidos: búsqueda por código y nombre, ciudad, tipo, `activo`, paginación con `total`, ciudades ordenadas, resumen exacto con sus órdenes | pasan |
| A4 | Migración | `Almacenes` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado `ciudad=lima`, `tipo=regional`, `activo=false`, detalle, ciudades, resumen, POST válido, POST con código del seed → 409, DELETE del creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidad, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`9d41044`, mismas líneas que el auditado `64873b3`): backend 199/199, frontend 147/147 |
| 2026-10-15 | A2–A3 | ✅ | `AlmacenesApiTests`: mensajes literales con su código y sin `Detail`, código en mayúsculas, tipo canónico, PUT con su propio código; búsqueda, ciudad, tipo, `activo`, paginación, ciudades y resumen exactos |
| 2026-10-15 | A4 | ✅ | migración `Almacenes` (con `Designer` y snapshot) solo crea la tabla y su índice único; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: detalle `ALM-LIM-01`, ciudades `["Arequipa","Callao","Lima","Trujillo"]`, resumen 5/4/1 y 1025.00 m³, POST → `{id:6}`, código del seed → 409 literal, DELETE del creado → 204 |
| 2026-10-15 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | QA E2E | ✅ | Pam en local (Vite 5174 + API + PostgreSQL) y en producción (solo lectura): resumen 5/4/1 con 1,025.00 y 925.00 m³ en una línea y plurales correctos; filtros por código, nombre, ciudad, tipo y estado, una sola petición del listado al cargar; detalle con pastilla y «Sin responsable»; alta con código en minúsculas guardado en mayúsculas; validaciones locales sin petición; ALM-LIM-01 → 409 conservando lo escrito; edición; baja con Escape; demás pantallas sin regresiones; 9 capturas sin recortes ni scroll horizontal |
