# Especificación: 52 - Base de datos primero y entidades con scaffold

## 1. Objetivo
**Problema:** el curso trabaja primero la base de datos y después genera las entidades con scaffold (Lab 08 y Lab 10), pero el proyecto escribía las entidades a mano y creaba las tablas con 25 migraciones de EF Core (code first).

**Resultado esperado:** la base de datos es la fuente de verdad. El esquema vive en scripts SQL (`database/NNN-*.sql`); las 37 entidades de `src/TitanGlobal.Domain/Models` y el `TitanGlobalDbContext` se generan con `dotnet ef dbcontext scaffold` desde esa base, y el smoke falla si el código generado no coincide con ella.

## 2. Fuera de alcance
- **Fase 2 (otra spec):** mejoras del esquema (reglas `CHECK`, longitudes en las tablas del enunciado, revisión de claves foráneas). Esta fase no cambia ni una columna.
- **La base de la demo (Neon):** no se toca. Ya tiene exactamente este esquema; el registro `control."ScriptsAplicados"` se creará en producción con la fase 2, antes del primer script nuevo.
- **Casos de uso, controladores, DTOs, frontend y pruebas:** no cambian.

**Decisiones del humano (2026-11-05):** conservar los nombres de navegación con plantillas; hacerlo en dos fases; convertir a SQL la migración de la rama de Maxi al integrarla.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/001-esquema-inicial.sql` | crear | las 37 tablas, ordenadas por dependencias, con claves e índices |
| `scripts/aplicar-bd.sh` | crear | aplica los scripts pendientes a la base local; `--estado` y `--reiniciar` |
| `scripts/scaffold.sh` | crear | regenera entidades y DbContext; `--comprobar` falla si no están al día |
| `src/TitanGlobal.Infrastructure/CodeTemplates/EFCore/*.t4` | crear | plantillas del scaffold: `string.Empty`, sin constructores, cabecera «Generado por» |
| `src/TitanGlobal.Api/Configuration/Scaffolding/*.cs` | crear | pluralizador en español y nombres de colecciones (solo en tiempo de diseño) |
| `src/TitanGlobal.Domain/Models/*.cs` | regenerar | 37 entidades generadas; ganan navegaciones donde ya había clave foránea |
| `src/TitanGlobal.Domain/Models/ValoresIniciales.cs` | crear | valores iniciales en C# (`Activo = true`, `Estado = "Emitida"`…) que el scaffold no genera |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet` y configuración generados |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.Constructor.cs` | crear | constructor con `IContextoSolicitud`, escrito a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.ImpuestosTasas.cs` | borrar | sus `DbSet` ahora se generan |
| `src/TitanGlobal.Infrastructure/Migrations/` | borrar | sustituida por `database/` |
| `src/TitanGlobal.Api/TitanGlobal.Api.csproj` | modificar | `Microsoft.EntityFrameworkCore.Design` también en compilación (para el pluralizador) |
| `scripts/verify.sh` | modificar | sin `has-pending-model-changes` |
| `scripts/smoke-postgres.sh` | modificar | crea la base con `aplicar-bd.sh --reiniciar` y ejecuta `scaffold.sh --comprobar` |
| `AGENTS.md`, `CONTRIBUTING.md`, `README.md`, `docs/` | modificar | flujo nuevo |

## 4. Diseño y lógica

### Flujo para una tabla nueva
1. Crear `database/NNN-descripcion.sql` (siguiente número libre) con el `CREATE TABLE` o `ALTER TABLE`.
2. `./scripts/aplicar-bd.sh` → lo aplica a la base local y lo anota en `control."ScriptsAplicados"`.
3. `./scripts/scaffold.sh` → regenera las entidades y el `TitanGlobalDbContext`.
4. Escribir DTOs, casos de uso y controlador como siempre.

Reglas:
- Un script aplicado (y fusionado en `main`) nunca se edita: los cambios van en un script nuevo.
- Los archivos con la cabecera «Generado por scripts/scaffold.sh» y `TitanGlobalDbContext.cs` no se editan a mano. El código propio va en archivos `partial` aparte (`ValoresIniciales.cs`, `TitanGlobalDbContext.Constructor.cs`, `TitanGlobalDbContext.Auditoria.cs`).
- Si la tabla nueva no sigue la regla del plural (solo cambia la primera palabra: `MovimientosInventario` → `MovimientoInventario`), se añade a `PluralizadorEspanol`. Si una colección necesita un nombre corto (`Detalles`), se añade a `NombresNavegacion`.
- Dos personas que añaden script a la vez pueden elegir el mismo número: quien integra después renombra el suyo al siguiente libre antes de fusionar.

### Nombres
- El scaffold de EF usa reglas de plural del inglés (`Proveedores` → `Proveedore`). `PluralizadorEspanol` (`IPluralizer`) traduce las 37 tablas a sus entidades y aplica la regla general a las nuevas.
- `NombresNavegacion` (`CandidateNamingService`) conserva 10 colecciones con nombre propio: `Detalles` (5), `Zonas`, `Tasas`, `Items`, `Roles` y `Usuarios`.
- Ambos viven en `Api` porque `dotnet ef` busca los servicios de diseño en el proyecto de arranque. No se usan en ejecución.

### Base local y producción
- `aplicar-bd.sh --reiniciar` borra y recrea la base local. Una base creada antes con migraciones se detecta y se pide recrearla.
- Producción (fase 2): los scripts nuevos los aplica Lukas con copia, ensayo y OK. El historial `__EFMigrationsHistory` de Neon se queda como está.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Mismo modelo | antes de borrar las migraciones, `dotnet ef migrations has-pending-model-changes` con el modelo generado por scaffold | «No changes have been made» |
| A2 | Mismo esquema | `pg_dump --schema-only` de la base creada con `001-esquema-inicial.sql` contra la creada con las 25 migraciones | 0 líneas distintas |
| A3 | Nombres | las 37 entidades y las 10 colecciones con nombre propio se generan con el nombre de antes | sin cambios en casos de uso |
| A4 | Gate | `./scripts/verify.sh` | en verde, 927 + 497 pruebas sin tocar |
| A5 | Smoke | `./scripts/smoke-postgres.sh` | `SCAFFOLD AL DÍA` y `SMOKE OK` |
| A6 | Detección | entidad editada a mano → `scaffold.sh --comprobar` sale con 1; base con scripts pendientes → `scaffold.sh` se niega; base creada con migraciones → `aplicar-bd.sh` pide `--reiniciar` | los tres fallan con su mensaje |

## 6. Checklist de ejecución
- [x] Plantillas, pluralizador y nombres de navegación.
- [x] Scaffold de las 37 entidades y del DbContext; archivos partial a mano.
- [x] `database/001-esquema-inicial.sql`, `aplicar-bd.sh` y `scaffold.sh`.
- [x] Sin migraciones; gate y smoke adaptados.
- [x] Guías del equipo.
- [x] Verificar A1–A6 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-05 | A1 | ✅ | `has-pending-model-changes` con el modelo generado y las 25 migraciones: «No changes have been made to the model since the last migration.» |
| 2026-11-05 | A2 | ✅ | `diff` de los dos `pg_dump --schema-only`: 0 líneas distintas |
| 2026-11-05 | A3 | ✅ | 37 entidades con el mismo nombre; compilación sin cambiar ningún caso de uso; 927 pruebas en verde |
| 2026-11-05 | A5 | ✅ | `smoke-postgres.sh`: «Aplicando 001-esquema-inicial.sql», «SCAFFOLD AL DÍA», 272 comprobaciones HTTP y `SMOKE OK` |
| 2026-11-05 | A6 | ✅ | `Cliente.cs` con una propiedad añadida a mano → «Desactualizado: …/Cliente.cs», salida 1; base vacía → «la base local tiene scripts sin aplicar», salida 1; base sin registro con tablas → «se creó con las migraciones antiguas», salida 1 |
| 2026-11-05 | Revisión de código | ✅ | Revisión independiente: 9 hallazgos; 8 corregidos (salida de `psql` con `\r` en Git Bash de Windows, entidades huérfanas al borrar una tabla, `--estado` que creaba el registro, scripts con `BEGIN`/`COMMIT` o sin `;` final, plural de `Unidades`/`Perfiles`/`Talleres`, contraseña entre comillas simples, comparación tolerante a espacios y clases escritas a mano en `Models`) y 1 descartado (`DateTime.UtcNow` en `ValoresIniciales` ya es UTC y es el valor que tenía la entidad). Pruebas nuevas `PluralizadorEspanolTests` (10) |
