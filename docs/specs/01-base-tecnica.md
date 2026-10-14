# Especificación: 01 - Base técnica de la API

## 1. Objetivo
**Problema:** el repositorio solo tiene documentación. No hay solución .NET, ni pipeline, ni persistencia, ni un gate de calidad que haga cumplir las reglas de código de `CLAUDE.md`.
**Resultado esperado:** existe la solución `TitanGlobal.sln` con:
- La API `src/TitanGlobal.Api` en .NET 10, que arranca con el pipeline institucional de 3 middlewares, persistencia EF Core/PostgreSQL mediante repositorio genérico y unidad de trabajo, `AuthService` técnico y registro de UseCases por reflexión.
- El proyecto de tests `tests/TitanGlobal.Api.Tests`.
- El gate `./scripts/verify.sh`, que compila, revisa formato y reglas de código y ejecuta los tests.

> **Enmienda 2026-10-14 (ciclo de corrección 1):** el humano entregó el enunciado (`docs/enunciado.md`). Esta spec se alinea con él: contratos de repositorio y entidades de §3, mensajes de error literales de §4 del enunciado. Además: código con una sentencia por línea, migración generada por EF, `check-rules.sh` sin huecos y tests con BD InMemory. La API escucha en desarrollo en `http://localhost:5080` (5173 es del frontend).

## 2. Fuera de alcance
- Los 80 módulos de negocio. El módulo de referencia OrdenesCompra va en la spec 02, cuando el humano confirme su número.
- Autenticación real (JWT, usuarios en BD). `AuthService` solo resuelve el rol de la petición.
- Despliegue, CI en GitHub y repositorio remoto.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna (el humano entregó la arquitectura el 2026-10-14).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `TitanGlobal.sln` | crear | incluye la API y los tests |
| `src/TitanGlobal.Api/TitanGlobal.Api.csproj` | crear | `net10.0`, `Nullable` y `ImplicitUsings` activados, `TreatWarningsAsErrors` |
| `src/TitanGlobal.Api/Program.cs` | crear | como en §4: reflexión y pipeline estricto. Declarar `public partial class Program { }` para `WebApplicationFactory` |
| `src/TitanGlobal.Api/Configuration/DependencyInjectionExtensions.cs` | crear | DbContext, UnitOfWork, repositorio genérico, Services y registro de UseCases por reflexión, en métodos de extensión; `Program.cs` los llama |
| `src/TitanGlobal.Api/Middlewares/{ErrorHandling,ParameterValidation,RoleBasedAccess}Middleware.cs` | crear | comportamiento de §4 |
| `src/TitanGlobal.Api/Models/{TitanGlobalDbContext,Cliente,Producto,OrdenCompra,DetalleOrdenCompra}.cs` | crear | entidades `partial` con exactamente los campos de `docs/enunciado.md` §3.1, con las relaciones configuradas en `OnModelCreating`, `decimal(18,2)` en importes y `NumeroOrden` único |
| `src/TitanGlobal.Api/Repositories/Interfaces/{IGenericRepository,IUnitOfWork}.cs` | crear | contratos de `docs/enunciado.md` §3.2: `AsQueryable`, `AddAsync`, `Update`, `Remove`; `Repository<T>()`, `CompleteAsync()` |
| `src/TitanGlobal.Api/Repositories/Implementations/{GenericRepository,UnitOfWork}.cs` | crear | `UnitOfWork` cachea los repositorios por tipo |
| `src/TitanGlobal.Api/Services/{IAuthService,AuthService}.cs` | crear | `string ObtenerRolActual(HttpContext)` lee la cabecera `X-User-Role` (vacío si no viene). Es lo único que hace |
| `src/TitanGlobal.Api/Migrations/*` | crear | migración inicial `Inicial` generada con `dotnet ef migrations add Inicial` (incluye `Designer` y `TitanGlobalDbContextModelSnapshot`) |
| `src/TitanGlobal.Api/appsettings.json` | crear | `ConnectionStrings:DefaultConnection` solo con valores de desarrollo local (docker compose); nada real |
| `docker-compose.yml` | crear | servicio `db`: postgres:16, puerto 5433, credenciales de desarrollo |
| `tests/TitanGlobal.Api.Tests/*` | crear | xUnit + `Microsoft.AspNetCore.Mvc.Testing`; BD EF InMemory en los tests |
| `scripts/verify.sh` | crear | gate de §4 |
| `scripts/check-rules.sh` | crear | reglas de código de §4 |
| `.editorconfig` | crear | estilo C#: llaves, `var` prohibido (`csharp_style_var_* = false:error`), miembros con cuerpo de expresión prohibidos (`csharp_style_expression_bodied_* = false:error`) |

## 4. Diseño y lógica
- **Reglas de código** (`CLAUDE.md`):
  - Sin `=>` en miembros, salvo lambdas.
  - Sin `var`.
  - UseCases concretos sin interfaz.
  - Controladores delgados.
  - `private readonly _camelCase`.
  - Llaves `{ }` y `return` explícito.
- **Pipeline exacto:** `UseMiddleware<ErrorHandlingMiddleware>()` → `UseRouting()` → `UseMiddleware<ParameterValidationMiddleware>()` → `UseMiddleware<RoleBasedAccessMiddleware>()` → `UseAuthorization()` → `MapControllers()`. En desarrollo, además, `MapOpenApi()`.
- **ErrorHandlingMiddleware:** ante una excepción no controlada responde 500 con JSON `{ StatusCode: 500, Message, Detail }` y la registra en el log.
- **ParameterValidationMiddleware:** en POST y PUT, `EnableBuffering()`, lee el cuerpo, `Position = 0`. Si el cuerpo está vacío o en blanco, responde 400 con `{ StatusCode: 400, Message: "El cuerpo de la petición (Body) no puede estar vacío." }`. El controlador debe poder leer el cuerpo después.
- **RoleBasedAccessMiddleware:** obtiene el rol vía `IAuthService` (resuelto desde `context.RequestServices`). Si es `Bloqueado` o `SinPermiso` (sin distinguir mayúsculas), responde 403 con `{ StatusCode: 403, Message: "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación." }`.
- **Registro por reflexión:** toda clase concreta, no abstracta, cuyo nombre termine en `UseCase` se registra con `AddScoped(tipo)`. Test: una clase `PruebaUseCase`, solo del proyecto de tests o un `HealthUseCase` mínimo en la API, debe resolverse desde el contenedor.
- **Salud:** `Modules/Modulo00_Sistema/` con `HealthController` (`GET api/v1/sistema/salud` → 200 `{ Estado: "OK" }`) y su `ConsultarSaludUseCase`. Sirve para probar el pipeline sin módulos de negocio. Si el humano no lo quiere, se quita en la spec 02.
- **Gate `scripts/verify.sh`** (`set -euo pipefail`):
  1. `dotnet restore`.
  2. `dotnet build -warnaserror --no-restore`.
  3. `dotnet format --verify-no-changes --no-restore`.
  4. `./scripts/check-rules.sh`.
  5. `dotnet test --no-build`.
- **`scripts/check-rules.sh`:** analiza `src/` y falla con archivo y línea si encuentra:
  - (a) `var` usado como tipo;
  - (b) miembros con cuerpo de expresión: líneas de declaración de propiedad, método o constructor seguidas de `=>`, sin contar lambdas dentro de llamadas;
  - (c) una interfaz `I*UseCase`;
  - (d) un `UseCase` dentro de `Services/`;
  - (e) un controlador que inyecte `IUnitOfWork`, `DbContext` o repositorios.

  Debe tener su propia prueba: un directorio de fixtures con un caso bueno y uno malo de cada regla, comprobado desde `verify.sh` o como test.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` | en verde |
| A3 | Tests de middlewares | `WebApplicationFactory`: 500 JSON ante una excepción, 400 con cuerpo vacío en POST/PUT, el cuerpo sigue legible tras validarlo, 403 con `X-User-Role: Bloqueado` y con `SinPermiso`, 200 sin cabecera | pasan |
| A4 | Orden del pipeline | test: con rol `Bloqueado` y cuerpo vacío en un POST → 400 (la validación va antes que el rol); una excepción dentro de un controlador → 500 JSON | pasan |
| A5 | Reflexión | test: un UseCase se resuelve del contenedor sin registro manual | pasa |
| A6 | Reglas | `check-rules.sh` falla con cada fixture malo y pasa con los buenos | pasa |
| A7 | Datos | `docker compose up -d db` + `dotnet ef database update` aplica la migración inicial | sin errores |
| A8 | OpenAPI | en desarrollo, `GET /openapi/v1.json` responde 200 e incluye `/api/v1/sistema/salud` | presente |

## 6. Checklist de ejecución
- [ ] Tarea 1: solución, proyectos, `.editorconfig`, `docker-compose.yml`.
- [ ] Tarea 2: Models, DbContext, Repositories, Services y migración inicial.
- [ ] Tarea 3: middlewares, `Program.cs` y DI por reflexión; módulo de salud.
- [ ] Tarea 4: tests y scripts del gate.
- [ ] Tarea 5: A1–A8.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
