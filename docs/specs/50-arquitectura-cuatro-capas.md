# Especificación: 50 - Arquitectura en cuatro capas

## 1. Objetivo
**Problema:** todo el backend vive en un solo proyecto (`src/TitanGlobal.Api`), con cada módulo en `Modules/ModuloNN_Nombre/{Controllers,Dtos,UseCases}`. Los casos de uso conocen detalles técnicos (`Npgsql`, `StatusCodes` de ASP.NET) y el `DbContext` lee la petición HTTP directamente.

**Resultado esperado:** el backend queda en cuatro proyectos con dependencias en una sola dirección, según el modelo del Lab 10. La API se comporta igual que antes: mismas rutas, mismos DTOs, mismos mensajes y misma base de datos. El gate impide romper las capas y las ramas en curso se adaptan con un comando.

## 2. Fuera de alcance
- **Login, JWT y BCrypt:** segunda etapa, en otra spec. Sigue `X-User-Role` tal como pide el enunciado.
- **Repositorios por entidad:** se conservan `IUnitOfWork`, `IGenericRepository<T>` y `AsQueryable`, y con ellos los 12 patrones LINQ. Application mantiene `Microsoft.EntityFrameworkCore` solo para los operadores asíncronos.
- **Cambios de esquema:** ninguna migración nueva; las existentes solo cambian de carpeta y de namespace.
- **Frontend:** no cambia. Las rutas `api/v1/...` son las mismas.
- **Lógica de negocio:** solo cambia cómo se detectan las violaciones de restricción de la base de datos.
- **Las ramas de los compañeros:** el script no las modifica ni las sube.

**Decisiones del humano (2026-11-02):** cuatro capas con las mismas consultas; JWT en una segunda etapa; migrar ya y adaptar las ramas con un script.

## 3. Archivos afectados
| Origen (`src/TitanGlobal.Api/`) | Destino (`src/`) | Namespace |
|---|---|---|
| `Models/*.cs` | `TitanGlobal.Domain/Models/` | `TitanGlobal.Domain.Models` |
| `Repositories/Interfaces/` | `TitanGlobal.Domain/Repositories/` | `TitanGlobal.Domain.Repositories` |
| `Configuration/FechasUtc.cs` | `TitanGlobal.Domain/Common/` | `TitanGlobal.Domain.Common` |
| `Modules/ModuloNN_X/Dtos/` | `TitanGlobal.Application/DTOs/ModuloNN_X/` | `TitanGlobal.Application.DTOs.ModuloNN_X` |
| `Modules/ModuloNN_X/UseCases/` y otras clases del módulo | `TitanGlobal.Application/UseCases/ModuloNN_X/` | `TitanGlobal.Application.UseCases.ModuloNN_X` |
| `Middlewares/ReglaNegocioException.cs` | `TitanGlobal.Application/Exceptions/` | `TitanGlobal.Application.Exceptions` |
| `Models/TitanGlobalDbContext*.cs` | `TitanGlobal.Infrastructure/Persistence/` | `TitanGlobal.Infrastructure.Persistence` |
| `Migrations/` | `TitanGlobal.Infrastructure/Migrations/` | `TitanGlobal.Infrastructure.Migrations` |
| `Repositories/Implementations/` | `TitanGlobal.Infrastructure/Repositories/Implementation/` | `TitanGlobal.Infrastructure.Repositories.Implementation` |
| `Modules/ModuloNN_X/Controllers/` | `TitanGlobal.Api/Controllers/ModuloNN_X/` | `TitanGlobal.Api.Controllers.ModuloNN_X` |

Se quedan en `TitanGlobal.Api`: `Program.cs`, `Middlewares/`, `Configuration/` y `Services/`.

**Otros archivos:** `Dockerfile`, `scripts/smoke-postgres.sh`, `scripts/verify.sh`, `scripts/check-rules.sh` y sus fixtures, `scripts/adaptar-rama-capas.py` (nuevo), `tests/` (namespaces y pruebas nuevas), `README.md`, `CONTRIBUTING.md` y las guías de `docs/`. Las specs anteriores no se reescriben: son historia y citan las rutas de su momento.

## 4. Diseño y lógica

### Dependencias
```
Api ──────────► Application ──► Domain
 │                  ▲             ▲
 └─► Infrastructure ┴─────────────┘
```
| Capa | Contiene | No puede usar |
|---|---|---|
| Domain | entidades, contratos de persistencia, utilidades puras | EF Core, Npgsql, ASP.NET, otras capas |
| Application | casos de uso, DTOs, excepciones, puertos | Npgsql, ASP.NET, Infrastructure, Api |
| Infrastructure | `DbContext`, migraciones, repositorios, traducción de errores de PostgreSQL | ASP.NET, Api |
| Api | controladores, middlewares, composición, Swagger | lógica de negocio |

### Módulos dentro de cada capa
Lab 10 tiene cinco entidades; TitanGlobal tiene 80 módulos. Por eso cada capa conserva una carpeta por módulo (`UseCases/Modulo01_Clientes/`). Así se mantiene la numeración del enunciado y un módulo sigue siendo fácil de localizar y de asignar a una persona.

### Sustituciones que dejan fuera los detalles técnicos
- **`DbUpdateException` + `PostgresException` → `ViolacionRestriccionException`** (Application). `UnitOfWork.CompleteAsync` traduce las violaciones de unicidad (`23505`) y de clave foránea (`23503`). Cualquier otro error sigue propagándose sin cambios hasta `ErrorHandlingMiddleware`, con el mismo mensaje.
- **`StatusCodes` → `CodigosEstado`** (Application): constantes con los mismos valores.
- **`IHttpContextAccessor` en el `DbContext` → puerto `IContextoSolicitud`** (Application), implementado por `ContextoSolicitud` en Api. La auditoría sigue registrando `X-Usuario` y la ruta igual que antes.

### Composición (como Lab 10)
- `AddApplicationServices` (Api/Configuration): controladores, OpenAPI, Swagger, auth y casos de uso por reflexión sobre el ensamblado de Application.
- `AddInfrastructureServices` (Infrastructure/Configuration): `DbContext`, `IUnitOfWork` e `IGenericRepository<>`.
- `Program.cs`: una sola llamada, con el pipeline obligatorio intacto.

### Swagger
Se añade Swagger en desarrollo (`/swagger`) junto al `/openapi/v1.json` que ya existía, con `X-User-Role` como cabecera autorizable. Es solo documentación: no cambia ningún endpoint.

### Migraciones
Los identificadores de migración no cambian, así que `__EFMigrationsHistory` sigue siendo válido en local y en Neon. No hay que aplicar nada en producción.
```sh
dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
```

### Ramas en curso
`scripts/adaptar-rama-capas.py rama <rama>` trabaja sobre una rama nueva creada desde `main`. Reproduce los cambios de la rama de origen en las rutas nuevas, con fusión a tres bandas en los archivos compartidos, y regenera la migración. La rama original no se toca.

### Numeración de specs
Las specs nuevas se nombran `moduloNN-nombre-{backend|frontend}.md`, sin número global. Cuatro ramas habían elegido la 47 a la vez; el número de módulo no choca.

## 5. Criterios de aceptación
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` | en verde, con las reglas nuevas f y g |
| A3 | Tests | `dotnet test` | los 585 anteriores sin debilitar, más los nuevos de `ArquitecturaTests`, `UnitOfWorkTests` y Swagger |
| A4 | Capas | `ArquitecturaTests` | Domain sin EF ni capas; Application sin Npgsql, ASP.NET, Infrastructure ni Api |
| A5 | Contrato API | diff de `/openapi/v1.json` entre `main` y esta rama | sin cambios |
| A6 | Modelo | `dotnet ef migrations has-pending-model-changes` | sin cambios pendientes |
| A7 | PostgreSQL real | `./scripts/smoke-postgres.sh` | `SMOKE OK`, incluidas las violaciones de restricción |
| A8 | Despliegue | `docker build .` | la imagen se construye |
| A9 | Ramas | el script sobre las 6 ramas, en copias | cada una compila y pasa sus tests |
| A10 | Guías | las rutas y los comandos de las guías existen | sin rutas antiguas fuera de las specs históricas |

## 6. Checklist de ejecución
- [ ] Script y movimiento mecánico.
- [ ] Puertos, excepciones y composición.
- [ ] Swagger.
- [ ] Reglas del gate y pruebas de arquitectura.
- [ ] Prueba del script sobre las 6 ramas.
- [ ] Guías del equipo.
- [ ] Auditoría y registro de A1–A10.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
