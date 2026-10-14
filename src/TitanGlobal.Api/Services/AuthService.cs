namespace TitanGlobal.Api.Services;

public class AuthService : IAuthService
{
    public string ObtenerRolActual(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-User-Role", out Microsoft.Extensions.Primitives.StringValues role))
        {
            return role.ToString();
        }

        return string.Empty;
    }
}
