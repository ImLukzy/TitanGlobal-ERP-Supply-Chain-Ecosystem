# Especificación: 45 - Modulo77_AuditoriaTransaccional (backend)

## 1. Objetivo
**Problema:** no queda rastro de quién creó, cambió o borró cada registro del ERP ni de qué valores tenía antes.
**Resultado esperado:**
- un **registro de auditoría automático**: cada alta, modificación y baja que se guarde en la base de datos, de cualquier módulo, deja una fila con la entidad, su Id, la acción, el usuario, la fecha y los cambios campo a campo (antes y después);
- `Modules/Modulo77_AuditoriaTransaccional/` en `api/v1/auditoria`, **de solo lectura**: listado con filtros, detalle, entidades auditadas y resumen.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Cambiar el pipeline de middlewares, `IAuthService` o el 403 del enunciado.
- Editar o borrar registros de auditoría (es de solo anexado; no hay PUT ni DELETE).
- Auditar lecturas (GET) o cambios hechos fuera de EF (SQL manual o el seed).
- El frontend (spec 46).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por el integrador: el usuario sale de la cabecera opcional `X-Usuario` (máx. 30 caracteres; si falta o está vacía, `"sistema"`); se auditan todas las entidades salvo la propia auditoría; los valores se guardan como texto.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/RegistroAuditoria.cs` | crear | entidad `partial` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.Auditoria.cs` | crear | parte `partial` del `DbContext` que sobrescribe `SaveChanges` y `SaveChangesAsync` (así funciona igual en la API y en las fábricas de tests, que registran su propio `DbContext`) |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet<RegistroAuditoria>`; constructor con `IHttpContextAccessor?` opcional (nulo fuera de una petición); índices en `Fecha` y en (`Entidad`, `EntidadId`) |
| `src/TitanGlobal.Api/Configuration/DependencyInjectionExtensions.cs` | modificar | `services.AddHttpContextAccessor()` (si no está) |
| `src/TitanGlobal.Api/Migrations/*_AuditoriaTransaccional*.cs` + snapshot | crear | `dotnet ef migrations add AuditoriaTransaccional -p src/TitanGlobal.Api` |
| `src/TitanGlobal.Api/Modules/Modulo77_AuditoriaTransaccional/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/auditoria")]`, solo GET |
| `scripts/smoke-postgres.sh` | modificar | §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo77_AuditoriaTransaccional/*.cs` | crear | InMemory |

## 4. Diseño y lógica
**Entidad `RegistroAuditoria`:** `Id` (long), `Fecha` (UTC), `Entidad` (máx. 100, nombre de la clase, p. ej. `Cliente`), `EntidadId` (máx. 50, valor de la clave primaria como texto), `Accion` (máx. 20: `Alta`, `Modificacion`, `Baja`), `Usuario` (máx. 30), `Ruta` (máx. 200, método y ruta de la petición, p. ej. `PUT /api/v1/clientes/3`; `null` fuera de una petición), `Cambios` (texto JSON: lista de `{ campo, antes, despues }`).

**Captura** (en `SaveChanges`/`SaveChangesAsync`):
1. Antes de guardar, recorre el `ChangeTracker`: entradas `Added`, `Modified` y `Deleted` de todas las entidades salvo `RegistroAuditoria`.
2. `Alta`: todos los campos con `antes = null` y `despues` = valor. `Modificacion`: solo los campos que cambiaron (antes ≠ después). `Baja`: todos los campos con `despues = null`. Las propiedades de navegación no se guardan; las claves foráneas sí. Fechas en ISO 8601 UTC y decimales con punto.
3. Guarda los cambios del negocio; después completa `EntidadId` de las altas (ya tienen Id) y guarda los registros de auditoría. Si los cambios del negocio fallan, no se guarda auditoría. Dentro de un `TransactionScope` (p. ej. el número atómico de las specs 14 y 16), ambos guardados forman parte de la misma transacción.
4. Una modificación sin cambios reales no genera registro.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`. Solo lectura.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/auditoria?entidad=&entidadId=&accion=&usuario=&fechaDesde=&fechaHasta=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaAuditoriaDto { items: RegistroListadoDto[], total, pagina, tamanoPagina }` | 400 paginación; «La acción no es válida.»; «La fecha desde no puede ser posterior a la fecha hasta.» |
| GET | `api/v1/auditoria/{id:long}` | — | 200 `RegistroDetalleDto` | 404 «El registro de auditoría {id} no existe.» |
| GET | `api/v1/auditoria/entidades` | — | 200 `string[]` (entidades con registros, ordenadas) | — |
| GET | `api/v1/auditoria/resumen` | — | 200 `ResumenAuditoriaDto` | — |

**DTOs.**
- `RegistroListadoDto { id, fecha, entidad, entidadId, accion, usuario, ruta, camposCambiados }` (`camposCambiados` = número de cambios); orden por `Fecha` desc e `Id` desc.
- `RegistroDetalleDto` = listado + `cambios: CambioCampoDto[]` con `{ campo, antes, despues }`.
- `ResumenAuditoriaDto { total, altas, modificaciones, bajas, porEntidad: EntidadAuditoriaDto[], ultimos7Dias: DiaAuditoriaDto[] }`: `porEntidad` top 10 por número de registros (después por nombre); `ultimos7Dias` = los 7 días hasta hoy (UTC) con `{ fecha, registros }`, incluidos los días con 0.

**Reglas.** `accion` sin distinguir mayúsculas entre las tres válidas; `entidad` y `usuario` exactos sin distinguir mayúsculas; fechas con `FechasUtc` y `fechaHasta` inclusiva. Cabecera `X-Usuario` recortada a 30 caracteres tras `Trim()`.

**Consultas.** Desde `Repository<RegistroAuditoria>().AsQueryable()`, asíncronas, proyectadas, sin N+1. Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` (todos los tests existentes siguen en verde: la auditoría no cambia ninguna respuesta) | en verde |
| A2 | Captura | tests: alta, modificación y baja de un cliente generan un registro cada uno con los campos y valores exactos (en la modificación solo los campos cambiados); modificación sin cambios no genera registro; `X-Usuario` y por defecto `"sistema"`; `Ruta` con método y ruta; una operación con varias entidades (p. ej. crear una solicitud con 2 líneas) genera un registro por entidad; si el guardado del negocio falla (409 de una regla o error de base de datos) no queda registro | pasan |
| A3 | Consultas | tests: filtros (entidad, entidadId, acción, usuario, fechas) y orden; detalle; entidades; resumen con los 7 días incluidos los vacíos; acción inválida y rango de fechas inválido → 400 literal | pasan |
| A4 | Sin cambios en seguridad | `git diff` no toca `Middlewares/` ni `Services/`; los tests del 403 siguen idénticos | 0 archivos |
| A5 | PostgreSQL | smoke ampliado: crear, editar y borrar un cliente con `X-Usuario: smoke` → 3 registros visibles con `entidad=Cliente&usuario=smoke` y sus cambios exactos; un número atómico (crear una solicitud de compra) deja la solicitud y sus líneas auditadas; resumen; detalle inexistente → 404 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 4 operaciones (solo GET) | presentes |

## 6. Checklist de ejecución
- [ ] Tarea 1: entidad, captura en el `DbContext`, `HttpContextAccessor` y migración.
- [ ] Tarea 2: DTOs, UseCases y controlador de solo lectura.
- [ ] Tarea 3: tests y smoke.
- [ ] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
