using System.Text.Json;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Api.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ReglaNegocioException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogWarning(exception, "La petición incumplió una regla de negocio.");
            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";

            object error = new
            {
                StatusCode = exception.StatusCode,
                Message = exception.Message
            };
            string responseBody = JsonSerializer.Serialize(error, JsonSerializationOptions.ErrorResponses);

            await context.Response.WriteAsync(responseBody);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogError(exception, "Error no controlado durante la petición.");
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            object error = new
            {
                StatusCode = 500,
                Message = "Ocurrió un error interno en el servidor.",
                Detail = exception.Message
            };
            string responseBody = JsonSerializer.Serialize(error, JsonSerializationOptions.ErrorResponses);

            await context.Response.WriteAsync(responseBody);
        }
    }
}
