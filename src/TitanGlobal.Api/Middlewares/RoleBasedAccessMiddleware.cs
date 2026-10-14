using System.Text.Json;
using TitanGlobal.Api.Services;

namespace TitanGlobal.Api.Middlewares;

public class RoleBasedAccessMiddleware
{
    private readonly RequestDelegate _next;

    public RoleBasedAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        IAuthService auth = context.RequestServices.GetRequiredService<IAuthService>();
        string role = auth.ObtenerRolActual(context);

        if (string.Equals(role, "Bloqueado", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "SinPermiso", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            object error = new
            {
                StatusCode = 403,
                Message = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación."
            };
            string responseBody = JsonSerializer.Serialize(error, JsonSerializationOptions.ErrorResponses);

            await context.Response.WriteAsync(responseBody, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
