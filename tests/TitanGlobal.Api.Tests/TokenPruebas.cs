using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.Services;

namespace TitanGlobal.Api.Tests;

public static class TokenPruebas
{
    public const int UsuarioId = 9001;

    public const string NombreUsuario = "pruebas";

    public static string Emitir(IServiceProvider servicios, params string[] roles)
    {
        IServicioTokens tokens = servicios.GetRequiredService<IServicioTokens>();
        return tokens.Emitir(UsuarioId, NombreUsuario, roles).Token;
    }

    public static void AutenticarComoAdministrador(HttpClient cliente, IServiceProvider servicios)
    {
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Emitir(servicios, "ADMIN"));
    }
}
