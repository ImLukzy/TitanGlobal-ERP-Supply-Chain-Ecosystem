# Adaptar una rama en curso a las cuatro capas

Las ramas de módulos 22, 33, 41, 42, 61, 71, 72 y 75 ya están integradas ([`relevo.md`](relevo.md) §1): no tienes que adaptarlas. Esta guía es para otro trabajo que tengas con la estructura anterior.

Si empezaste una rama antes de que `main` pasara a las cuatro capas (2026-11-02, spec 50), **no hagas `git merge` ni `git rebase` a mano**: como todos los archivos de `src/TitanGlobal.Api/Modules/...` y `Models/...` cambiaron de proyecto y de namespace, un rebase normal produce conflictos en cada archivo.

Usa el script `scripts/adaptar-rama-capas.py`, que reproduce tus cambios sobre una rama limpia creada desde `origin/main`.

> **Aviso (2026-11-05, spec 52):** `main` ya no usa migraciones de EF Core: el esquema vive en `database/NNN-*.sql` y las entidades se generan con `./scripts/scaffold.sh`. El script de esta guía adapta ramas **escritas con la estructura anterior a las cuatro capas** y todavía trata las migraciones. Si tu rama trae una migración o entidades escritas a mano, después de adaptarla **conviértela al flujo nuevo**: escribe el `CREATE TABLE` o `ALTER TABLE` en un `database/NNN-descripcion.sql` con el siguiente número libre, aplícalo con `./scripts/aplicar-bd.sh`, regenera con `./scripts/scaffold.sh` y descarta la migración y las entidades de tu rama (ver [`crear-modulo.md`](crear-modulo.md) §2 a §4). Los pasos de abajo que hablan de migraciones son historia.

## 1. Pasos exactos

Desde la raíz del repositorio, sin cambios pendientes en tu copia local:

```sh
git fetch origin
git switch -c tu-nombre/moduloXX-nombre-capas origin/main
python3 scripts/adaptar-rama-capas.py rama origin/tu-nombre/moduloXX-nombre
```

> **En Windows** ejecútalo desde **Git Bash**, porque el script llama a `bash` para comprobar las reglas. Si `python3` no está definido, usa `python`:
> ```sh
> python scripts/adaptar-rama-capas.py rama origin/tu-nombre/moduloXX-nombre
> ```
> Si tu rama solo existe en tu máquina local y nunca la subiste con `git push`, pasa el nombre local (`tu-nombre/moduloXX-nombre` sin `origin/`).

## 2. Qué hace el script

Al ejecutar `scripts/adaptar-rama-capas.py rama <rama>`:

1. **No toca tu rama original:** `origin/tu-nombre/moduloXX-nombre` no se modifica ni se borra; queda intacta como respaldo.
2. **Calcula los cambios reales de tu rama:** busca la base común con `git merge-base <rama> HEAD` y obtiene la lista de archivos añadidos, modificados o eliminados en tu rama.
3. **Mueve cada archivo a su capa nueva y actualiza el código:**
   - Lleva modelos, DTOs, casos de uso, validaciones y controladores a sus rutas en `Domain`, `Application`, `Infrastructure` y `Api`.
   - Actualiza los `namespace` y `using` en todos los `.cs` (incluidos los tests en `tests/TitanGlobal.Api.Tests/`).
   - En `Application`, cambia `StatusCodes.Status...` por `CodigosEstado.Status...` y traduce los bloques `catch (DbUpdateException)` / `PostgresException` (`23505`, `23503`, `ConstraintName`) a `ViolacionRestriccionException` (`TipoViolacionRestriccion.Unicidad`, `TipoViolacionRestriccion.ReferenciaForanea`, `Restriccion`).
   - Quita el número global a las specs nuevas del módulo (`docs/specs/NN-moduloXX-nombre-backend.md` → `docs/specs/moduloXX-nombre-backend.md`) y actualiza las rutas citadas dentro del markdown.
4. **Fusiona archivos compartidos a tres bandas:** en archivos que tocaron tanto `main` como tu rama (`TitanGlobalDbContext.cs`, `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh`, `frontend/src/modules/pantallas.ts`, `frontend/src/styles/app.css`), adapta primero la versión base y la tuya a las cuatro capas y ejecuta `git merge-file` (`main` vs. `base` vs. `tu rama`).
5. **Regenera la migración limpia (el script aún lo hace; ahora conviértela a un script de `database/`, ver el aviso del inicio):** descarta los archivos de migración antiguos de `src/TitanGlobal.Api/Migrations/` (que tenían el snapshot anterior) y, si no hubo conflictos, compila (`dotnet build`), restaura herramientas (`dotnet tool restore`), genera de nuevo la migración con el mismo nombre en `Infrastructure`:
   ```sh
   dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
   ```
   y formatea con `dotnet format`.
6. **No hace commit ni push:** deja todos los cambios en tu árbol de trabajo para que los revises con `git status` y `git diff`.

Termina con código de salida distinto de 0 si queda algo por resolver, e imprime en «Siguientes pasos» exactamente lo que falta. Sigue esa lista: si la migración no se pudo generar, aparece ahí.

## 3. Equivalencia de rutas (antigua → nueva)

| Ruta en tu rama antigua | Ruta nueva en cuatro capas | Namespace nuevo |
|---|---|---|
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext*.cs` | `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext*.cs` | `TitanGlobal.Infrastructure.Persistence` |
| `src/TitanGlobal.Api/Models/<Entidad>.cs` | `src/TitanGlobal.Domain/Models/<Entidad>.cs` | `TitanGlobal.Domain.Models` |
| `src/TitanGlobal.Api/Repositories/Interfaces/*.cs` | `src/TitanGlobal.Domain/Repositories/*.cs` | `TitanGlobal.Domain.Repositories` |
| `src/TitanGlobal.Api/Repositories/Implementations/*.cs` | `src/TitanGlobal.Infrastructure/Repositories/Implementation/*.cs` | `TitanGlobal.Infrastructure.Repositories.Implementation` |
| `src/TitanGlobal.Api/Migrations/*` | *(ya no existe: se sustituye por un script `database/NNN-*.sql`)* | — |
| `src/TitanGlobal.Api/Middlewares/ReglaNegocioException.cs` | `src/TitanGlobal.Application/Exceptions/ReglaNegocioException.cs` | `TitanGlobal.Application.Exceptions` |
| `src/TitanGlobal.Api/Configuration/FechasUtc.cs` | `src/TitanGlobal.Domain/Common/FechasUtc.cs` | `TitanGlobal.Domain.Common` |
| `src/TitanGlobal.Api/Modules/ModuloNN_X/Controllers/*.cs` | `src/TitanGlobal.Api/Controllers/ModuloNN_X/*.cs` | `TitanGlobal.Api.Controllers.ModuloNN_X` |
| `src/TitanGlobal.Api/Modules/ModuloNN_X/Dtos/*.cs` | `src/TitanGlobal.Application/DTOs/ModuloNN_X/*.cs` | `TitanGlobal.Application.DTOs.ModuloNN_X` |
| `src/TitanGlobal.Api/Modules/ModuloNN_X/UseCases/*.cs` (y helpers `Validacion*`) | `src/TitanGlobal.Application/UseCases/ModuloNN_X/*.cs` | `TitanGlobal.Application.UseCases.ModuloNN_X` |
| `docs/specs/NN-moduloXX-nombre-{backend\|frontend}.md` | `docs/specs/moduloXX-nombre-{backend\|frontend}.md` | *(sin número global)* |

Las pruebas siguen en `tests/TitanGlobal.Api.Tests/ModuloNN_X/`, con sus `using` actualizados a `TitanGlobal.Domain.Models`, `TitanGlobal.Application.DTOs.ModuloNN_X` y `TitanGlobal.Infrastructure.Persistence`.

## 4. Resolver conflictos y avisos

Si al terminar el script muestra **conflictos** o **avisos**:

1. **Conflictos en archivos compartidos (`CONFLICTO ...`):**
   - Abre cada archivo indicado y busca las marcas `<<<<<<< main`, `=======` y `>>>>>>> tu rama`.
   - En `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs`, `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh`, `frontend/src/modules/pantallas.ts` o `frontend/src/styles/app.css`, conserva ambos bloques (primero lo que ya estaba en `main` y al final lo de tu módulo) y borra las líneas `<<<<<<<`, `=======` y `>>>>>>>`.
   - **Cuidado con la última línea:** si los dos bloques terminan igual, git la deja fuera del conflicto y el primer bloque queda cortado. Por ejemplo, justo después de `>>>>>>>` aparece `.IsUnique();`, `fi` o `COMMIT;`. Si el primer bloque la necesita, repítela. Comprueba siempre con `dotnet build` y `./scripts/smoke-postgres.sh`.
2. **Aviso de acceso a PostgreSQL en `Application`:**
   - Si el script imprime `Hay que revisar a mano el acceso a PostgreSQL en:`, abre esos archivos en `src/TitanGlobal.Application/UseCases/...` y asegúrate de capturar `ViolacionRestriccionException` (`excepcion.Tipo == TipoViolacionRestriccion.Unicidad` o `ReferenciaForanea`, y `excepcion.Restriccion`) en lugar de `DbUpdateException` o `PostgresException`.
3. **Entidades con atributos de EF Core (`f: dependencia prohibida en TitanGlobal.Domain`):**
   - Una entidad de `Domain` no puede usar `[Index]` ni `[Precision]`, porque vienen de EF Core. Quita esos atributos y `using Microsoft.EntityFrameworkCore;` de la entidad. `[Required]` y `[MaxLength]` sí se pueden quedar: son de .NET.
   - Añade la configuración al final de `OnModelCreating`, en `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs`. Por ejemplo:
     ```csharp
     modelBuilder.Entity<Impuesto>()
         .HasIndex(impuesto => impuesto.Codigo)
         .IsUnique();

     modelBuilder.Entity<TasaImpuesto>()
         .HasIndex(tasa => new { tasa.ImpuestoId, tasa.FechaInicio });

     modelBuilder.Entity<TasaImpuesto>()
         .Property(tasa => tasa.Porcentaje)
         .HasPrecision(18, 6);
     ```
   - La base de datos resultante tiene que ser la misma: ponla en el script SQL (longitudes, decimales, índices) y compara con la migración de tu rama antigua.
4. **Pasar el esquema al flujo nuevo después de corregir:**
   - Con conflictos o reglas incumplidas, el script no termina la adaptación. Cuando lo resuelvas, escribe tu `database/NNN-descripcion.sql` y ejecuta:
     ```sh
     ./scripts/aplicar-bd.sh
     ./scripts/scaffold.sh
     ```
   - Si olvidas este paso, `./scripts/smoke-postgres.sh` falla con «ERROR: el código generado no coincide…» o las tablas no existen.
5. **Otros avisos (`Aviso: ...`): léelos todos.**

   | Aviso | Qué hacer |
   |---|---|
   | La migración se descarta y se regenera | Ya no se usan migraciones: pasa su contenido a un script de `database/`. Si tenía SQL o datos escritos a mano, el aviso lo indica: inclúyelos en el script. |
   | Tu rama cambió una migración antigua | No se aplica: nunca se edita un script o migración ya fusionado. Pasa ese cambio a un script nuevo de `database/`. |
   | `DependencyInjectionExtensions.cs` ya no existe | Lleva tu registro a `AddApplicationServices` o a `AddInfrastructureServices`. |
   | Tu rama lo borró, pero main lo cambió | No se borra. Decide tú si sigue haciendo falta. |
   | `catch` de `DbUpdateException` sin filtro, o lectura del texto del error de PostgreSQL | Compila, pero puede comportarse distinto: ahora solo se capturan unicidad y clave foránea, y el texto ya no es el de PostgreSQL. Reescríbelo con `excepcion.Tipo` y `excepcion.Restriccion`. |

## 5. Comprobar, confirmar y abrir el pull request

Cuando todo compile y el script de `database/` esté aplicado y las entidades regeneradas:

1. **Formatear y pasar el gate completo:**
   ```sh
   dotnet format
   ./scripts/verify.sh
   ```
2. **Si tu rama tiene cambios de base de datos, pasar el smoke contra PostgreSQL real:**
   ```sh
   ./scripts/smoke-postgres.sh   # debe terminar en SMOKE OK
   ```
3. **Confirmar y subir la rama nueva:**
   ```sh
   git status
   git add -A
   git commit -m "feat: ModuloXX_Nombre backend en cuatro capas"
   git push -u origin tu-nombre/moduloXX-nombre-capas
   ```
4. Abre el **pull request contra `main`** desde la rama nueva (`tu-nombre/moduloXX-nombre-capas`) con la plantilla del repositorio. Si ya tenías un pull request abierto con la rama antigua, ciérralo indicando el enlace al nuevo.

## 6. Problemas frecuentes

| Problema | Causa y solución |
|---|---|
| `Ejecuta esto en una rama nueva creada desde origin/main (ya en cuatro capas).` | Estás situado en tu rama antigua o tu `main` local no está actualizado. Haz `git fetch origin` y `git switch -c tu-nombre/moduloXX-nombre-capas origin/main`. |
| `Tienes cambios sin confirmar. Confírmalos o guárdalos antes de empezar.` | Hay archivos modificados o sin seguimiento en tu copia local. Confírmalos en tu rama anterior o guárdalos con `git stash` antes de cambiar a la rama nueva. |
| `git merge-base ...: fatal: Not a valid object name` | El nombre de la rama no existe o falta `git fetch origin`. Comprueba el nombre exacto con `git branch -a`. |
| `f: dependencia prohibida en TitanGlobal.Application` al correr `./scripts/verify.sh` | Quedó un `using Npgsql;`, `using Microsoft.AspNetCore.Http;`, `StatusCodes` o `DbUpdateException` en `Application`. Sustitúyelo por `using TitanGlobal.Application.Exceptions;`, `CodigosEstado` y `ViolacionRestriccionException`. |
| El scaffold falla al compilar antes de ejecutarse | Resuelve primero los conflictos (`<<<<<<<`) y verifica con `dotnet build` antes de lanzar `./scripts/scaffold.sh`. |
| La spec cambió de nombre | Es esperado: las specs nuevas de módulo pierden el número global (`docs/specs/moduloXX-nombre-backend.md`) para no chocar con las de otros compañeros. Actualiza el enlace en tu pull request. |
