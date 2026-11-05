using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.Services;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Configuration;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo00_Autenticacion;

public class AutenticacionApiTests : IClassFixture<AutenticacionApiFactory>
{
    private const string MensajeCredenciales = "Usuario o contraseña incorrectos.";
    private const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";
    private const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    private readonly AutenticacionApiFactory _fabrica;

    public AutenticacionApiTests(AutenticacionApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.RestablecerDatos();
    }

    [Fact]
    public async Task LoginCorrectoDevuelveTokenQueAbreElPerfil()
    {
        HttpClient cliente = ClienteAnonimo();
        HttpResponseMessage respuesta = await IniciarSesionAsync(cliente, " Admin ", "Admin2026!");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        SesionDto? sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>();
        Assert.NotNull(sesion);
        Assert.False(string.IsNullOrWhiteSpace(sesion.Token));
        Assert.True(sesion.ExpiraEn > DateTime.UtcNow);
        Assert.Equal("admin", sesion.Usuario.NombreUsuario);
        Assert.Equal(new List<string> { "ADMIN" }, sesion.Roles);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Token);
        PerfilDto? perfil = await cliente.GetFromJsonAsync<PerfilDto>("/api/v1/autenticacion/perfil");
        Assert.NotNull(perfil);
        Assert.Equal(sesion.Usuario.Id, perfil.Id);
        Assert.Equal("Administrador del sistema", perfil.NombreCompleto);
    }

    [Theory]
    [InlineData("admin", "Incorrecta1")]
    [InlineData("noexiste", "Admin2026!")]
    [InlineData("inactivo", "Inactivo2026!")]
    [InlineData("sinclave", "Cualquiera1")]
    public async Task CredencialesInvalidasDevuelven401Literal(string nombreUsuario, string contrasena)
    {
        HttpResponseMessage respuesta = await IniciarSesionAsync(ClienteAnonimo(), nombreUsuario, contrasena);
        await AssertErrorAsync(respuesta, 401, MensajeCredenciales);
    }

    [Theory]
    [InlineData("", "Admin2026!")]
    [InlineData("admin", "")]
    [InlineData("   ", "   ")]
    public async Task UsuarioOContrasenaVaciosDevuelven400Literal(string nombreUsuario, string contrasena)
    {
        HttpResponseMessage respuesta = await IniciarSesionAsync(ClienteAnonimo(), nombreUsuario, contrasena);
        await AssertErrorAsync(respuesta, 400, "El usuario y la contraseña son obligatorios.");
    }

    [Fact]
    public async Task CuerpoNullDevuelve400SinLlegarAlCasoDeUso()
    {
        StringContent nulo = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage login = await ClienteAnonimo().PostAsync("/api/v1/autenticacion/login", nulo);
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);

        StringContent nuloContrasena = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage cambio = await ClienteConRoles("ADMIN").PutAsync("/api/v1/usuarios/2/contrasena", nuloContrasena);
        Assert.Equal(HttpStatusCode.BadRequest, cambio.StatusCode);
    }

    [Fact]
    public async Task SinTokenLaApiDevuelve401YLaSaludSigueAbierta()
    {
        HttpClient cliente = ClienteAnonimo();

        await AssertErrorAsync(await cliente.GetAsync("/api/v1/clientes"), 401, MensajeSinSesion);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/sistema/salud")).StatusCode);
    }

    [Fact]
    public async Task TokenConFirmaInvalidaVencidoODeOtroEmisorDevuelve401()
    {
        JwtOpciones opciones = _fabrica.Services.GetRequiredService<JwtOpciones>();
        string firmaAjena = CrearToken(new string('x', 48), opciones.Emisor, DateTime.UtcNow.AddHours(1));
        string vencido = CrearToken(opciones.Clave, opciones.Emisor, DateTime.UtcNow.AddHours(-1));
        string otroEmisor = CrearToken(opciones.Clave, "otro-emisor", DateTime.UtcNow.AddHours(1));

        foreach (string token in new[] { firmaAjena, vencido, otroEmisor, "no-es-un-token" })
        {
            HttpClient cliente = ClienteAnonimo();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await AssertErrorAsync(await cliente.GetAsync("/api/v1/clientes"), 401, MensajeSinSesion);
        }
    }

    [Fact]
    public async Task SinRolAdminNoSePuedenAdministrarUsuariosRolesNiContrasenas()
    {
        HttpClient cliente = ClienteConRoles("VENTAS");

        await AssertErrorAsync(
            await cliente.PutAsJsonAsync("/api/v1/usuarios/1/contrasena", new CambiarContrasenaRequestDto { Contrasena = "Nueva2026!" }),
            403,
            MensajeSinPermiso);
        await AssertErrorAsync(
            await cliente.PostAsJsonAsync("/api/v1/usuarios", new { NombreUsuario = "nuevo", NombreCompleto = "Nuevo", Email = "nuevo@ejemplo.com", Activo = true }),
            403,
            MensajeSinPermiso);
        await AssertErrorAsync(
            await cliente.PostAsJsonAsync("/api/v1/roles", new { Codigo = "NUEVO", Nombre = "Nuevo" }),
            403,
            MensajeSinPermiso);
        await AssertErrorAsync(await cliente.DeleteAsync("/api/v1/usuarios/2"), 403, MensajeSinPermiso);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/usuarios")).StatusCode);
    }

    [Fact]
    public async Task AdminCambiaLaContrasenaYSoloValeLaNueva()
    {
        HttpClient administrador = ClienteConRoles("ADMIN");
        HttpResponseMessage respuesta = await administrador.PutAsJsonAsync(
            "/api/v1/usuarios/2/contrasena",
            new CambiarContrasenaRequestDto { Contrasena = "Nueva2026!" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await IniciarSesionAsync(ClienteAnonimo(), "demo", "Nueva2026!")).StatusCode);
        await AssertErrorAsync(await IniciarSesionAsync(ClienteAnonimo(), "demo", "Demo2026!"), 401, MensajeCredenciales);
    }

    [Theory]
    [InlineData(2, 7, 400, "La contraseña debe tener entre 8 y 72 caracteres.")]
    [InlineData(2, 73, 400, "La contraseña debe tener entre 8 y 72 caracteres.")]
    [InlineData(999, 10, 404, "El usuario 999 no existe.")]
    public async Task CambioDeContrasenaInvalidoDevuelveLiteralSinCambiarla(int usuarioId, int longitud, int estado, string mensaje)
    {
        HttpResponseMessage respuesta = await ClienteConRoles("ADMIN").PutAsJsonAsync(
            $"/api/v1/usuarios/{usuarioId}/contrasena",
            new CambiarContrasenaRequestDto { Contrasena = new string('a', longitud) });

        await AssertErrorAsync(respuesta, estado, mensaje);
        Assert.Equal(HttpStatusCode.OK, (await IniciarSesionAsync(ClienteAnonimo(), "demo", "Demo2026!")).StatusCode);
    }

    [Fact]
    public async Task ContrasenaDeMasDe72BytesSeRechaza()
    {
        HttpResponseMessage respuesta = await ClienteConRoles("ADMIN").PutAsJsonAsync(
            "/api/v1/usuarios/2/contrasena",
            new CambiarContrasenaRequestDto { Contrasena = new string('ñ', 40) });

        await AssertErrorAsync(respuesta, 400, "La contraseña debe tener entre 8 y 72 caracteres.");
    }

    [Fact]
    public async Task ElRolBloqueadoDelEnunciadoSigueDevolviendo403ConToken()
    {
        HttpClient cliente = ClienteConRoles("ADMIN");
        cliente.DefaultRequestHeaders.Add("X-User-Role", "Bloqueado");

        await AssertErrorAsync(await cliente.GetAsync("/api/v1/clientes"), 403, MensajeSinPermiso);
    }

    [Fact]
    public async Task ElDetalleDelUsuarioNoExponeElHash()
    {
        string cuerpo = await ClienteConRoles("ADMIN").GetStringAsync("/api/v1/usuarios/1");

        Assert.DoesNotContain("contrasena", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$2", cuerpo, StringComparison.Ordinal);
    }

    private HttpClient ClienteAnonimo()
    {
        HttpClient cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = null;
        return cliente;
    }

    private HttpClient ClienteConRoles(params string[] roles)
    {
        HttpClient cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_fabrica.Services, roles));
        return cliente;
    }

    private static Task<HttpResponseMessage> IniciarSesionAsync(HttpClient cliente, string nombreUsuario, string contrasena)
    {
        return cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/login",
            new IniciarSesionRequestDto { NombreUsuario = nombreUsuario, Contrasena = contrasena });
    }

    private static string CrearToken(string clave, string emisor, DateTime expira)
    {
        SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", "1"), new Claim("name", "admin"), new Claim("role", "ADMIN") }),
            Issuer = emisor,
            Audience = "titanglobal-web",
            IssuedAt = expira.AddHours(-2),
            NotBefore = expira.AddHours(-2),
            Expires = expira,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage respuesta, int estado, string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
    }
}

public sealed class AutenticacionApiFactory : WebApplicationFactory<Program>
{
    private readonly string _nombreBaseDatos = "Modulo00AutenticacionTests-" + Guid.NewGuid();

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        TokenPruebas.AutenticarComoAdministrador(client, Services);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            services.AddDbContext<TitanGlobalDbContext>(options => options.UseInMemoryDatabase(_nombreBaseDatos));
        });
    }

    public void RestablecerDatos()
    {
        using IServiceScope alcance = Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        IServicioContrasenas contrasenas = alcance.ServiceProvider.GetRequiredService<IServicioContrasenas>();
        contexto.Database.EnsureDeleted();
        contexto.Database.EnsureCreated();

        contexto.Roles.Add(new Rol { Id = 1, Codigo = "ADMIN", Nombre = "Administrador", EsSistema = true });
        contexto.Usuarios.AddRange(
            CrearUsuario(1, "admin", "Administrador del sistema", true, contrasenas.Encriptar("Admin2026!")),
            CrearUsuario(2, "demo", "Usuario de demostración", true, contrasenas.Encriptar("Demo2026!")),
            CrearUsuario(3, "inactivo", "Usuario inactivo", false, contrasenas.Encriptar("Inactivo2026!")),
            CrearUsuario(4, "sinclave", "Usuario sin contraseña", true, null));
        contexto.UsuariosRoles.Add(new UsuarioRol { Id = 1, UsuarioId = 1, RolId = 1 });
        contexto.SaveChanges();
    }

    private static Usuario CrearUsuario(int id, string nombreUsuario, string nombreCompleto, bool activo, string? hash)
    {
        return new Usuario
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto,
            Email = nombreUsuario + "@titanglobal.example",
            Activo = activo,
            FechaAlta = DateTime.UtcNow,
            ContrasenaHash = hash
        };
    }
}
