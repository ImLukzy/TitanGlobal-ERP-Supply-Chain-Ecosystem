using TitanGlobal.Application.Services;

namespace TitanGlobal.Api.Services;

public class ContextoSolicitud : IContextoSolicitud
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContextoSolicitud(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? ObtenerUsuario()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["X-Usuario"].ToString();
    }

    public string? ObtenerRuta()
    {
        HttpContext? contexto = _httpContextAccessor.HttpContext;
        if (contexto == null)
        {
            return null;
        }

        return contexto.Request.Method + " " + contexto.Request.Path;
    }
}
