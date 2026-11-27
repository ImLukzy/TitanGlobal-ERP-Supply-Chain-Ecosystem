using Microsoft.OpenApi;
using TitanGlobal.Api.Services;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.Services;
using TitanGlobal.Infrastructure.Configuration;

namespace TitanGlobal.Api.Configuration;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment entorno)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddAutenticacionJwt(configuration, entorno);
        services.AddHttpContextAccessor();
        services.AddInfrastructureServices(configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IContextoSolicitud, ContextoSolicitud>();
        AddMediatR(services);
        AddUseCases(services);
        AddSwagger(services);

        return services;
    }

    // Registra las queries, los commands y sus handlers de Application (CQRS con MediatR).
    private static void AddMediatR(IServiceCollection services)
    {
        services.AddMediatR(configuracion =>
        {
            configuracion.RegisterServicesFromAssembly(typeof(ReglaNegocioException).Assembly);
        });
    }

    // Registro de los *UseCase que aún no se migran (ramas y PR abiertos del equipo).
    private static void AddUseCases(IServiceCollection services)
    {
        foreach (Type type in typeof(ReglaNegocioException).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("UseCase", StringComparison.Ordinal)))
        {
            services.AddScoped(type);
        }
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TitanGlobal ERP API",
                Version = "v1",
                Description = "API del ERP en cuatro capas: Domain, Application, Infrastructure y Api."
            });
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
            options.AddSecurityDefinition("X-User-Role", new OpenApiSecurityScheme
            {
                Name = "X-User-Role",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Rol de la petición. Bloqueado o SinPermiso responden 403."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token de POST /api/v1/autenticacion/login. Pega solo el token, sin «Bearer»."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("X-User-Role", document),
                    new List<string>()
                }
                ,
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });
        });
    }
}
