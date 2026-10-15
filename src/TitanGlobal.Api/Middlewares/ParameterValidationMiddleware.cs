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
        if (validarCuerpo && !EsAprobacionSinCuerpo(context.Request))
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

    private static bool EsAprobacionSinCuerpo(HttpRequest solicitud)
    {
        if (!HttpMethods.IsPost(solicitud.Method))
        {
            return false;
        }

        string ruta = solicitud.Path.Value ?? string.Empty;
        string prefijo = "/api/v1/solicitudes-compra/";
        string sufijo = "/aprobar";
        if (!ruta.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)
            || !ruta.EndsWith(sufijo, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string idTexto = ruta.Substring(prefijo.Length, ruta.Length - prefijo.Length - sufijo.Length);

        return int.TryParse(idTexto, out int _);
    }
}
