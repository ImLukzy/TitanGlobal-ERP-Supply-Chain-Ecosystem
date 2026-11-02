# Crear un módulo backend en cuatro capas

Guía paso a paso para implementar un módulo nuevo del backend en las cuatro capas (`Domain`, `Application`, `Infrastructure`, `Api`). Sigue como ejemplo real `Modulo11_Proveedores` (para módulos con líneas y máquina de estados, consulta también `Modulo13_SolicitudesCompra`; la plantilla del enunciado con los 12 patrones LINQ es `Modulo04_OrdenesCompra`).

## Qué va en cada capa

La dirección de dependencias es estricta: `Api` → `Application` e `Infrastructure`; `Infrastructure` → `Application` y `Domain`; `Application` → `Domain`. `Domain` no depende de nadie.

| Capa / Proyecto | Carpetas | Qué contiene | Prohibido usar |
|---|---|---|---|
| `src/TitanGlobal.Domain` | `Models/`, `Repositories/`, `Common/` | Entidades (`partial`), contratos `IGenericRepository<T>` e `IUnitOfWork`, utilidades puras (`FechasUtc`) | EF Core, Npgsql, ASP.NET, otras capas |
| `src/TitanGlobal.Application` | `DTOs/ModuloNN_Nombre/`, `UseCases/ModuloNN_Nombre/`, `Exceptions/`, `Services/` | DTOs, casos de uso concretos, validaciones, `ReglaNegocioException`, `CodigosEstado`, `ViolacionRestriccionException`, `TipoViolacionRestriccion`, puerto `IContextoSolicitud` | Npgsql, ASP.NET (`StatusCodes`), `Infrastructure`, `Api` *(conserva `Microsoft.EntityFrameworkCore` solo para los operadores asíncronos de LINQ)* |
| `src/TitanGlobal.Infrastructure` | `Persistence/`, `Migrations/`, `Repositories/Implementation/`, `Configuration/` | `TitanGlobalDbContext` y auditoría, migraciones EF Core, `GenericRepository<T>`, `UnitOfWork` (traduce `23505`/`23503` a `ViolacionRestriccionException`), `InfrastructureServicesExtensions` | ASP.NET, `Api` |
| `src/TitanGlobal.Api` | `Controllers/ModuloNN_Nombre/`, `Middlewares/`, `Configuration/`, `Services/`, `Program.cs` | Controladores delgados, pipeline de middlewares, `ServiceRegistrationExtensions`, `AuthService`, `ContextoSolicitud`, Swagger (`/swagger`) y OpenAPI (`/openapi/v1.json`) | Lógica de negocio o acceso directo a persistencia en controladores |
| `tests/TitanGlobal.Api.Tests` | `ModuloNN_Nombre/`, `Arquitectura/` | Pruebas de integración por módulo (`WebApplicationFactory` + EF InMemory) y pruebas de arquitectura (`ArquitecturaTests`, `UnitOfWorkTests`) | — |

El gate comprueba estas fronteras con las reglas `f` y `g` de `scripts/check-rules.sh` y con `tests/TitanGlobal.Api.Tests/Arquitectura/ArquitecturaTests.cs`.

---

## Paso a paso

### 1. Spec

1. Copia [`docs/specs/_TEMPLATE.md`](../specs/_TEMPLATE.md) a `docs/specs/moduloNN-nombre-backend.md`, **sin número global** (varias personas elegían el mismo número; solo las specs transversales llevan número global).
2. Define el contrato exacto: rutas `api/v1/...`, DTOs de entrada y salida, **mensajes de error literales**, reglas de negocio en orden y criterios de aceptación medibles.
3. Pide revisión a Lukas antes de programar y crea tu rama desde `main` actualizado:
   ```sh
   git fetch origin
   git switch -c tu-nombre/moduloNN-nombre origin/main
   ```

### 2. Entidad en Domain

Crea la entidad en `src/TitanGlobal.Domain/Models/<Entidad>.cs` dentro del namespace `TitanGlobal.Domain.Models`. Las propiedades `string` no nulas se inicializan con `string.Empty` y las colecciones con `new List<...>()` (`src/TitanGlobal.Domain/Models/Proveedor.cs`):

```csharp
namespace TitanGlobal.Domain.Models;

public partial class Proveedor
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; } = true;

    public virtual ICollection<SolicitudCompra> SolicitudesCompra { get; set; } = new List<SolicitudCompra>();

    public virtual ICollection<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = new List<OrdenAbastecimiento>();
}
```

### 3. `DbSet` y configuración en Persistence

En `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs`, añade la propiedad `DbSet` y configura longitudes, tipos decimales, índices únicos y claves foráneas al final de `OnModelCreating(ModelBuilder modelBuilder)` (para evitar conflictos con otros compañeros):

```csharp
public DbSet<Proveedor> Proveedores { get; set; } = null!;
```

```csharp
modelBuilder.Entity<Proveedor>()
    .Property(proveedor => proveedor.RazonSocial)
    .HasMaxLength(200);

modelBuilder.Entity<Proveedor>()
    .Property(proveedor => proveedor.Ruc)
    .HasMaxLength(11);

modelBuilder.Entity<Proveedor>()
    .Property(proveedor => proveedor.Pais)
    .HasMaxLength(100);

modelBuilder.Entity<Proveedor>()
    .Property(proveedor => proveedor.Email)
    .HasMaxLength(150);

modelBuilder.Entity<Proveedor>()
    .Property(proveedor => proveedor.Telefono)
    .HasMaxLength(20);

modelBuilder.Entity<Proveedor>()
    .HasIndex(proveedor => proveedor.Ruc)
    .IsUnique();
```

### 4. Migración

Las migraciones viven en `src/TitanGlobal.Infrastructure/Migrations/` y usan `src/TitanGlobal.Api` como proyecto de arranque. Genera y aplica la migración en tu base local con:

```sh
dotnet tool restore
dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
```

Nunca escribas una migración ni edites `TitanGlobalDbContextModelSnapshot.cs` a mano.

### 5. DTOs

Crea los DTOs del módulo en `src/TitanGlobal.Application/DTOs/ModuloNN_Nombre/`, un archivo por clase y en el namespace `TitanGlobal.Application.DTOs.ModuloNN_Nombre`. Ejemplo real (`src/TitanGlobal.Application/DTOs/Modulo11_Proveedores/GuardarProveedorRequestDto.cs`):

```csharp
namespace TitanGlobal.Application.DTOs.Modulo11_Proveedores;

public class GuardarProveedorRequestDto
{
    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; } = true;
}
```

### 6. UseCases

Crea los casos de uso y su helper de validación en `src/TitanGlobal.Application/UseCases/ModuloNN_Nombre/` (`namespace TitanGlobal.Application.UseCases.ModuloNN_Nombre;`).

Reglas obligatorias en `Application`:
- **Clases concretas sin interfaz**, con nombre terminado en `UseCase`. `ServiceRegistrationExtensions.AddApplicationServices` (`src/TitanGlobal.Api/Configuration/ServiceRegistrationExtensions.cs`) las registra por reflexión automáticamente.
- **Sin `var`** y **sin `=>` en miembros** (constructores, métodos y propiedades llevan bloque `{ }` y `return` explícito; `=>` solo en lambdas LINQ).
- **Códigos y excepciones de negocio:** usa `using TitanGlobal.Application.Exceptions;`, `ReglaNegocioException` y `CodigosEstado.Status400BadRequest` / `Status404NotFound` / `Status409Conflict` (nunca `StatusCodes` de ASP.NET).
- **Conflictos de base de datos:** captura `ViolacionRestriccionException` (nunca `DbUpdateException` ni `PostgresException`):
  - Unicidad (`23505`): `catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)`
  - Clave foránea (`23503`): `catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)`
  - Si una tabla tiene varias restricciones únicas, distingue cuál falló leyendo `excepcion.Restriccion`.

Ejemplo de validación (`src/TitanGlobal.Application/UseCases/Modulo11_Proveedores/ValidacionProveedor.cs`):

```csharp
using System.Text.RegularExpressions;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

internal static class ValidacionProveedor
{
    public static string NormalizarRazonSocial(string? razonSocial)
    {
        string valor = razonSocial?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La razón social es obligatoria.");
        }

        if (valor.Length > 200)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La razón social no puede superar 200 caracteres.");
        }

        return valor;
    }
}
```

Ejemplo de caso de uso de escritura (`src/TitanGlobal.Application/UseCases/Modulo11_Proveedores/CrearProveedorUseCase.cs`):

```csharp
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

public class CrearProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarProveedorRequestDto solicitud)
    {
        string razonSocial = ValidacionProveedor.NormalizarRazonSocial(solicitud.RazonSocial);
        string ruc = ValidacionProveedor.NormalizarRuc(solicitud.Ruc);
        string pais = ValidacionProveedor.NormalizarPais(solicitud.Pais);
        string? email = ValidacionProveedor.NormalizarEmail(solicitud.Email);
        string? telefono = ValidacionProveedor.NormalizarTelefono(solicitud.Telefono);
        bool rucExiste = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .AnyAsync(proveedor => proveedor.Ruc == ruc);
        if (rucExiste)
        {
            throw CrearConflictoRucException(ruc);
        }

        Proveedor proveedorNuevo = new Proveedor
        {
            RazonSocial = razonSocial,
            Ruc = ruc,
            Pais = pais,
            Email = email,
            Telefono = telefono,
            Activo = solicitud.Activo
        };
        await _unitOfWork.Repository<Proveedor>().AddAsync(proveedorNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoRucException(ruc);
        }

        return proveedorNuevo.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    private static ReglaNegocioException CrearConflictoRucException(string ruc)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un proveedor con el RUC {ruc}.");
    }
}
```

### 7. Controlador

Crea el controlador en `src/TitanGlobal.Api/Controllers/ModuloNN_Nombre/<Recurso>Controller.cs` (`namespace TitanGlobal.Api.Controllers.ModuloNN_Nombre;`). Debe ser delgado: solo recibe la petición, llama al `UseCase` y devuelve el resultado. Si una acción `POST` o `PUT` no lleva cuerpo (por ejemplo, `aprobar` o `recibir`), decórala con `[PermitirCuerpoVacio]`.

Ejemplo real (`src/TitanGlobal.Api/Controllers/Modulo11_Proveedores/ProveedoresController.cs`):

```csharp
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Application.UseCases.Modulo11_Proveedores;

namespace TitanGlobal.Api.Controllers.Modulo11_Proveedores;

[ApiController]
[Route("api/v1/proveedores")]
public class ProveedoresController : ControllerBase
{
    private readonly ListarProveedoresUseCase _listarProveedoresUseCase;

    private readonly ObtenerProveedorUseCase _obtenerProveedorUseCase;

    private readonly CrearProveedorUseCase _crearProveedorUseCase;

    private readonly ActualizarProveedorUseCase _actualizarProveedorUseCase;

    private readonly EliminarProveedorUseCase _eliminarProveedorUseCase;

    private readonly ListarPaisesProveedorUseCase _listarPaisesProveedorUseCase;

    private readonly ResumenProveedoresUseCase _resumenProveedoresUseCase;

    public ProveedoresController(
        ListarProveedoresUseCase listarProveedoresUseCase,
        ObtenerProveedorUseCase obtenerProveedorUseCase,
        CrearProveedorUseCase crearProveedorUseCase,
        ActualizarProveedorUseCase actualizarProveedorUseCase,
        EliminarProveedorUseCase eliminarProveedorUseCase,
        ListarPaisesProveedorUseCase listarPaisesProveedorUseCase,
        ResumenProveedoresUseCase resumenProveedoresUseCase)
    {
        _listarProveedoresUseCase = listarProveedoresUseCase;
        _obtenerProveedorUseCase = obtenerProveedorUseCase;
        _crearProveedorUseCase = crearProveedorUseCase;
        _actualizarProveedorUseCase = actualizarProveedorUseCase;
        _eliminarProveedorUseCase = eliminarProveedorUseCase;
        _listarPaisesProveedorUseCase = listarPaisesProveedorUseCase;
        _resumenProveedoresUseCase = resumenProveedoresUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarProveedorRequestDto solicitud)
    {
        int id = await _crearProveedorUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarProveedorUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
```

Con la API arrancada en desarrollo (`dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080`), puedes probar los endpoints en `http://localhost:5080/swagger` (incluye el botón **Authorize** para enviar la cabecera `X-User-Role`) y verificar el contrato en `http://localhost:5080/openapi/v1.json`.

### 8. Pruebas

Añade las pruebas de integración en `tests/TitanGlobal.Api.Tests/ModuloNN_Nombre/<Recurso>ApiTests.cs`. Cubre todos los mensajes de error literales de la spec, los límites de validación, los filtros y el resumen (`tests/TitanGlobal.Api.Tests/Modulo11_Proveedores/ProveedoresApiTests.cs`):

```csharp
using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo11_Proveedores;

[Collection("Modulo04")]
public class ProveedoresApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ProveedoresApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerProveedores();
    }

    [Theory]
    [InlineData("/api/v1/proveedores?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("/api/v1/proveedores?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task InvalidPaginationReturnsBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }
}
```

### 9. Seed y smoke

1. **Seed (`scripts/seed-dev.sql`):** añade al final del archivo los datos de ejemplo de tu módulo de forma idempotente (`ON CONFLICT ... DO UPDATE`) y actualiza la secuencia si insertas con `Id` explícito:
   ```sql
   INSERT INTO "Proveedores" ("Id", "RazonSocial", "Ruc", "Pais", "Email", "Telefono", "Activo") VALUES
       (1, 'Andes Suministros S.A.C.', '20600000001', 'Peru', 'ventas@andes.example', '+51 1 555-0101', TRUE)
   ON CONFLICT ("Ruc") DO UPDATE SET
       "RazonSocial" = EXCLUDED."RazonSocial",
       "Pais" = EXCLUDED."Pais",
       "Email" = EXCLUDED."Email",
       "Telefono" = EXCLUDED."Telefono",
       "Activo" = EXCLUDED."Activo";
   ```
2. **Smoke (`scripts/smoke-postgres.sh`):** añade las comprobaciones contra PostgreSQL real al final del script y ejecútalo:
   ```sh
   request_check 'Proveedores por país peru' GET '/api/v1/proveedores?pais=peru' 200 providers-list
   request_check 'POST proveedor válido' POST '/api/v1/proveedores' 200 provider-created '{"razonSocial":"Proveedor Smoke","ruc":"20987654321","pais":"Peru","email":"smoke@proveedor.example","telefono":"+51 1 555-0199","activo":true}'
   request_check 'POST proveedor RUC del seed' POST '/api/v1/proveedores' 409 provider-duplicate '{"razonSocial":"Proveedor duplicado","ruc":"20600000001","pais":"Peru","activo":true}'
   request_check 'DELETE proveedor recién creado' DELETE "/api/v1/proveedores/${PROVEEDOR_CREADO_ID}" 204 ''
   ```
   ```sh
   ./scripts/smoke-postgres.sh   # debe terminar en SMOKE OK
   ```

### 10. Gate

Ejecuta el gate completo antes de subir tu rama (en Windows, desde Git Bash):

```sh
./scripts/verify.sh
```

Verifica compilación sin warnings (`-warnaserror`), formato (`dotnet format`), reglas de código y de capas (`scripts/check-rules.sh`, reglas `a`–`g`), pruebas de backend (incluidos `ArquitecturaTests` y `UnitOfWorkTests`), que el modelo no tenga cambios sin migración y verificación del frontend.

### 11. Pull request

Si el módulo lleva pantalla, sigue después [`crear-pantalla.md`](crear-pantalla.md).

Confirma los cambios con tu identidad de git, sube tu rama y abre el pull request contra `main` completando `.github/pull_request_template.md` con la salida de `./scripts/verify.sh` y de `./scripts/smoke-postgres.sh`.

---

## Errores típicos y solución

| Error / Síntoma | Causa | Solución |
|---|---|---|
| `f: dependencia prohibida en TitanGlobal.Application` por `Microsoft.AspNetCore` o `StatusCodes` | Se usó `StatusCodes.Status409Conflict` o `using Microsoft.AspNetCore.Http;` en un caso de uso | Sustituir por `CodigosEstado.Status409Conflict` y `using TitanGlobal.Application.Exceptions;` |
| `f: dependencia prohibida en TitanGlobal.Application` por `Npgsql` o fallo al compilar `PostgresException` | Se capturó `DbUpdateException` / `PostgresException` en `Application` | Capturar `ViolacionRestriccionException` y filtrar con `excepcion.Tipo == TipoViolacionRestriccion.Unicidad` (o `ReferenciaForanea`) y `excepcion.Restriccion` |
| `f: dependencia prohibida en TitanGlobal.Domain` | Se añadió `using Microsoft.EntityFrameworkCore;` o referencia a otra capa en una entidad de `Domain` | Dejar la entidad limpia en `Domain/Models` y llevar la configuración a `TitanGlobalDbContext.OnModelCreating` en `Infrastructure/Persistence` |
| `g: UseCase fuera de Application` o el contenedor DI no resuelve el `UseCase` | La clase `*UseCase` se creó en `Api` o `Infrastructure`, o no termina en `UseCase` | Moverla a `src/TitanGlobal.Application/UseCases/ModuloNN_Nombre/` y asegurar que sea `public class ...UseCase` concreta sin interfaz |
| `e: controlador con dependencia de persistencia` | El controlador recibe `IUnitOfWork`, `IGenericRepository<T>` o `TitanGlobalDbContext` | Inyectar solo casos de uso en el controlador; llevar las consultas a un `UseCase` en `Application` |
| `a: var usado como tipo` o `b: miembro con cuerpo de expresión` | Se usó `var` o `=>` en un método, constructor o propiedad | Declarar el tipo explícito y usar bloque `{ }` con `return` (`=>` solo vale dentro de lambdas LINQ o de configuración) |
| `Your target project 'TitanGlobal.Api' doesn't match your migrations assembly 'TitanGlobal.Infrastructure'` | Se ejecutó `dotnet ef` con `-p src/TitanGlobal.Api` | Usar siempre `-p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api` |
