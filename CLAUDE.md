# CLAUDE.md — TitanGlobal ERP & Supply Chain Ecosystem
ERP transaccional para una corporación manufacturera y logística: comercial/CRM, compras e importaciones, inventarios (WMS), manufactura (MRP), logística (TMS), finanzas, capital humano (HCM) y gobierno/seguridad. 80 módulos en 8 dominios (`docs/arquitectura.md`).

## Trabajo
- Responde en español. Al terminar: «Hecho» + resultado verificable.
- Tarea = spec en `docs/specs/NN-*.md` (plantilla `_TEMPLATE.md`); marca `[x]` y registra evidencia en §7.
- Antes de reportar: `./scripts/verify.sh` en verde.
- No commit/push sin pedirlo. Nunca commitear `appsettings.*.json` con secretos, `.env` ni cadenas de conexión reales.
- Equipo y flujo: `docs/equipo/equipo.md`. Lecciones: `docs/equipo/lecciones.md`.

## Stack
.NET 10 (ASP.NET Core Web API, controladores) · EF Core + Npgsql (PostgreSQL 16) · OpenAPI nativo (`AddOpenApi`) · xUnit + `WebApplicationFactory` para tests.

## Árbol
```
src/TitanGlobal.Api/
  Configuration/   DependencyInjectionExtensions.cs
  Middlewares/     ErrorHandling (500) · ParameterValidation (400, EnableBuffering + Position = 0) · RoleBasedAccess (403)
  Models/          entidades partial (estilo scaffolding) + TitanGlobalDbContext
  Repositories/    Interfaces/ (IGenericRepository<T>, IUnitOfWork) · Implementations/
  Services/        SOLO servicios técnicos transversales (IAuthService/AuthService)
  Modules/ModuloNN_Nombre/{Controllers,Dtos,UseCases}/   vertical slices
  Program.cs       registro de UseCases por reflexión + pipeline estricto
tests/TitanGlobal.Api.Tests/
```

## Reglas de código (estrictas, las revisa la auditoría)
- **Sin `=>` en miembros**: constructores, métodos, propiedades y acciones usan bloques `{ }` y `return` explícito. `=>` solo dentro de lambdas (LINQ, opciones de configuración).
- **Tipos explícitos**: nada de `var`.
- **UseCases = clases concretas SIN interfaz**, nombre terminado en `UseCase`, registradas por reflexión en `Program.cs`. Nunca registrar un UseCase a mano.
- **Controladores delgados**: cero lógica de negocio; solo llaman a UseCases. Campos `private readonly _camelCase`.
- **Services/** solo para servicios técnicos transversales (auth, etc.). La lógica de negocio va en `Modules/*/UseCases`.
- **Persistencia** solo vía `IUnitOfWork` / `IGenericRepository<T>`; consultas asíncronas (`ToListAsync`, `SumAsync`, `AnyAsync`…).
- **DTOs** por módulo en `Modules/*/Dtos`; nunca exponer entidades en controladores.
- **Pipeline fijo** en `Program.cs`, en este orden: `ErrorHandlingMiddleware` → `UseRouting` → `ParameterValidationMiddleware` → `RoleBasedAccessMiddleware` → `UseAuthorization` → `MapControllers`.
- Rutas `api/v1/<recurso-en-kebab-case>`.
- Errores en JSON `{ StatusCode, Message }` (más `Detail` solo en 500).

## Comandos
`dotnet build` · `dotnet test` · `./scripts/verify.sh` (gate completo) · BD local: `docker compose up -d db` · migraciones: `dotnet ef migrations add <Nombre> -p src/TitanGlobal.Api` / `dotnet ef database update -p src/TitanGlobal.Api`.
