using System.Text.Json;

namespace TitanGlobal.Api.Middlewares;

public class ParameterValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ParameterValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        bool validarCuerpo = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method);
        bool permiteCuerpoVacio = context.GetEndpoint()?
            .Metadata.GetMetadata<PermitirCuerpoVacioAttribute>() != null;
        if (validarCuerpo && !permiteCuerpoVacio)
        {
            context.Request.EnableBuffering();

            using StreamReader reader = new StreamReader(context.Request.Body, leaveOpen: true);
            string body = await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                object error = new
                {
                    StatusCode = 400,
                    Message = "El cuerpo de la petición (Body) no puede estar vacío."
                };
                string responseBody = JsonSerializer.Serialize(error, JsonSerializationOptions.ErrorResponses);

                await context.Response.WriteAsync(responseBody, context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }

}
