using System.Text.Json;

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

            string responseBody = JsonSerializer.Serialize(new
            {
                StatusCode = 500,
                Message = "Ocurrió un error interno en el servidor.",
                Detail = exception.Message
            });

            await context.Response.WriteAsync(responseBody);
        }
    }
}
