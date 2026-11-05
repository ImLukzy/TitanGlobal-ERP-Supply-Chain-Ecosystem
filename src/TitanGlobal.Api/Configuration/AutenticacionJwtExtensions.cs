using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Infrastructure.Configuration;

namespace TitanGlobal.Api.Configuration;

public static class AutenticacionJwtExtensions
{
    public const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";

    public const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    public static IServiceCollection AddAutenticacionJwt(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment entorno)
    {
        JwtOpciones opciones = ObtenerOpciones(configuration, entorno);
        services.AddSingleton(opciones);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opciones.Emisor,
                    ValidateAudience = true,
                    ValidAudience = opciones.Audiencia,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(opciones.ObtenerBytesClave()),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        await EscribirErrorAsync(contexto.Response, StatusCodes.Status401Unauthorized, MensajeSinSesion);
                    },
                    OnForbidden = async contexto =>
                    {
                        await EscribirErrorAsync(contexto.Response, StatusCodes.Status403Forbidden, MensajeSinPermiso);
                    }
                };
            });

        services.AddAuthorization(autorizacion =>
        {
            autorizacion.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    private static JwtOpciones ObtenerOpciones(IConfiguration configuration, IHostEnvironment entorno)
    {
        JwtOpciones opciones = configuration.GetSection("Jwt").Get<JwtOpciones>() ?? new JwtOpciones();
        if (string.IsNullOrEmpty(opciones.Clave) && entorno.IsDevelopment())
        {
            // En local cada arranque usa una clave nueva: los tokens anteriores dejan de valer.
            opciones.Clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        }

        if (opciones.ObtenerBytesClave().Length < JwtOpciones.LongitudMinimaClave)
        {
            throw new InvalidOperationException(
                "Falta Jwt:Clave o mide menos de 32 bytes. Configúrala con la variable de entorno Jwt__Clave.");
        }

        if (opciones.MinutosExpiracion <= 0)
        {
            throw new InvalidOperationException("Jwt:MinutosExpiracion debe ser mayor que cero.");
        }

        return opciones;
    }

    private static async Task EscribirErrorAsync(HttpResponse respuesta, int codigo, string mensaje)
    {
        respuesta.StatusCode = codigo;
        respuesta.ContentType = "application/json";
        object error = new
        {
            StatusCode = codigo,
            Message = mensaje
        };
        string cuerpo = JsonSerializer.Serialize(error, JsonSerializationOptions.ErrorResponses);
        await respuesta.WriteAsync(cuerpo, respuesta.HttpContext.RequestAborted);
    }
}
