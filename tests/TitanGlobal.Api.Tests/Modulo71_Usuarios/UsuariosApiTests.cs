using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo71_Usuarios.Dtos;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;

namespace TitanGlobal.Api.Tests.Modulo71_Usuarios;

[Collection("Modulo04")]
public class UsuariosApiTests
{
    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public UsuariosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Theory]
    [InlineData("", "Nombre válido", "persona@ejemplo.com", 400, "El nombre de usuario es obligatorio.")]
    [InlineData("a!b", "Nombre válido", "persona@ejemplo.com", 400, "El nombre de usuario solo admite letras minúsculas, números, punto, guion y guion bajo (3 a 30 caracteres).")]
    [InlineData("usuario", " ", "persona@ejemplo.com", 400, "El nombre completo es obligatorio.")]
    [InlineData("usuario", "NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN", "persona@ejemplo.com", 400, "El nombre completo no puede superar 100 caracteres.")]
    [InlineData("usuario", "Nombre válido", "", 400, "El email es obligatorio.")]
    [InlineData("usuario", "Nombre válido", "no-valido", 400, "El email no tiene un formato válido.")]
    [InlineData("usuario", "Nombre válido", "duplicado@ejemplo.com", 409, "Ya existe un usuario con el email admin@titanglobal.example.")]
    public async Task CrearUsuarioConDatosInvalidosDevuelveMensajeLiteralSinMutar(
        string nombreUsuario,
        string nombreCompleto,
        string email,
        int estado,
        string mensaje)
    {
        if (mensaje.StartsWith("Ya existe un usuario", StringComparison.Ordinal))
        {
            email = "admin@titanglobal.example";
        }

        string[] antes = await LeerUsuariosAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/usuarios", CrearSolicitud(nombreUsuario, nombreCompleto, email));
        await AssertErrorAsync(respuesta, estado, mensaje);
        Assert.Equal(antes, await LeerUsuariosAsync());
    }

    [Fact]
    public async Task EmailMayorA150CaracteresDevuelveFormatoInvalidoSinMutar()
    {
        string[] antes = await LeerUsuariosAsync();
        GuardarUsuarioRequestDto solicitud = CrearSolicitud(
            "usuario.largo",
            "Usuario Largo",
            $"{new string('a', 140)}@ejemplo.com");
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/usuarios", solicitud);
        await AssertErrorAsync(respuesta, 400, "El email no tiene un formato válido.");
        Assert.Equal(antes, await LeerUsuariosAsync());
    }

    [Theory]
    [InlineData("2,2", "El rol 2 está repetido.")]
    [InlineData("999", "El rol 999 no existe.")]
    public async Task RolesInvalidosDevuelvenLiteralSinMutar(string roles, string mensaje)
    {
        GuardarUsuarioRequestDto solicitud = CrearSolicitud("usuario.nuevo", "Usuario Nuevo", "nuevo@ejemplo.com");
        solicitud.RolIds = roles.Split(',').Select(int.Parse).ToList();
        string[] antes = await LeerUsuariosAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/usuarios", solicitud);
        await AssertErrorAsync(respuesta, 400, mensaje);
        Assert.Equal(antes, await LeerUsuariosAsync());
    }

    [Fact]
    public async Task NombreDeUsuarioDuplicadoDevuelveLiteralSinMutar()
    {
        string[] antes = await LeerUsuariosAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/usuarios",
            CrearSolicitud("admin", "Otro nombre", "otro@ejemplo.com"));
        await AssertErrorAsync(respuesta, 409, "Ya existe el usuario admin.");
        Assert.Equal(antes, await LeerUsuariosAsync());
    }

    [Fact]
    public async Task IdInexistenteDevuelveLiteralEnDetalleYNoCambiaFilas()
    {
        string[] antes = await LeerUsuariosAsync();
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/usuarios/9999");
        await AssertErrorAsync(respuesta, 404, "El usuario 9999 no existe.");
        Assert.Equal(antes, await LeerUsuariosAsync());
    }

    [Fact]
    public async Task PaginaMenorAUnoDevuelveLiteral()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/usuarios?pagina=0");
        await AssertErrorAsync(respuesta, 400, "La página debe ser mayor o igual a 1.");
    }

    [Fact]
    public async Task TamanoPaginaMayorACienDevuelveLiteral()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/usuarios?tamanoPagina=101");
        await AssertErrorAsync(respuesta, 400, "El tamaño de página debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task NoPermiteDesactivarUltimoAdmin()
    {
        await AssertCambioAdminInvalidoAsync(1, false, new List<int> { 1 }, "PUT");
    }

    [Fact]
    public async Task NoPermiteQuitarRolAlUltimoAdmin()
    {
        await AssertCambioAdminInvalidoAsync(1, true, new List<int>(), "PUT");
    }

    [Fact]
    public async Task NoPermiteEliminarUltimoAdmin()
    {
        string[] antes = await LeerUsuariosAsync();
        HttpResponseMessage respuesta = await _cliente.DeleteAsync("/api/v1/usuarios/1");
        await AssertErrorAsync(respuesta, 409, "Debe quedar al menos un usuario activo con el rol ADMIN.");
        Assert.Equal(antes, await LeerUsuariosAsync());
    }

    [Fact]
    public async Task PermiteCambiosAdminSiQuedaOtroActivo()
    {
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            contexto.Usuarios.Add(new Usuario
            {
                NombreUsuario = "admin.secundario",
                NombreCompleto = "Administradora Secundaria",
                Email = "admin.secundario@titanglobal.example",
                Activo = true,
                FechaAlta = DateTime.UtcNow,
                Roles = new List<UsuarioRol> { new UsuarioRol { RolId = 1 } }
            });
            await contexto.SaveChangesAsync();
        }

        HttpResponseMessage respuesta = await _cliente.DeleteAsync("/api/v1/usuarios/1");
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(4, (await LeerUsuariosAsync()).Length);
    }

    [Fact]
    public async Task NormalizaNombreYCorreoYPermiteConservarLosPropiosEnPut()
    {
        GuardarUsuarioRequestDto solicitud = CrearSolicitud(" Admin ", " Administrador Principal ", " ADMIN@TITANGLOBAL.EXAMPLE ");
        solicitud.RolIds = new List<int> { 1 };
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/usuarios/1", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Usuario? usuario = await ObtenerUsuarioAsync(1);
        Assert.NotNull(usuario);
        Assert.Equal("admin", usuario.NombreUsuario);
        Assert.Equal("admin@titanglobal.example", usuario.Email);
        Assert.Equal("Administrador Principal", usuario.NombreCompleto);
    }

    [Fact]
    public async Task PermisosEfectivosUnenRolesYListanQuienesOtorganCadaNivel()
    {
        List<PermisoEfectivoDto>? permisos = await _cliente.GetFromJsonAsync<List<PermisoEfectivoDto>>("/api/v1/usuarios/2/permisos");
        Assert.NotNull(permisos);
        Assert.Equal(80, permisos.Count);
        Assert.Equal("Escritura", permisos[0].Permiso);
        Assert.Equal(new[] { "VENTAS" }, permisos[0].OtorgadoPor);
        Assert.Equal("Lectura", permisos[1].Permiso);
        Assert.Equal(new[] { "CONSULTA" }, permisos[1].OtorgadoPor);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task UsuarioSinRolesOInactivoNoTienePermisos(int id)
    {
        List<PermisoEfectivoDto>? permisos = await _cliente.GetFromJsonAsync<List<PermisoEfectivoDto>>($"/api/v1/usuarios/{id}/permisos");
        Assert.NotNull(permisos);
        Assert.Equal(80, permisos.Count);
        Assert.All(permisos, permiso =>
        {
            Assert.Equal("Ninguno", permiso.Permiso);
            Assert.Empty(permiso.OtorgadoPor);
        });
    }

    [Fact]
    public async Task ListaFiltraPorRolEstadoBusquedaOrdenYTotalPaginado()
    {
        PaginaUsuariosDto? rol = await _cliente.GetFromJsonAsync<PaginaUsuariosDto>("/api/v1/usuarios?rolId=2&pagina=1&tamanoPagina=1");
        Assert.NotNull(rol);
        Assert.Equal(1, rol.Total);
        Assert.Equal("ventas", Assert.Single(rol.Items).NombreUsuario);
        PaginaUsuariosDto? estado = await _cliente.GetFromJsonAsync<PaginaUsuariosDto>("/api/v1/usuarios?activo=false");
        Assert.NotNull(estado);
        Assert.Equal("inactivo", Assert.Single(estado.Items).NombreUsuario);
        PaginaUsuariosDto? busqueda = await _cliente.GetFromJsonAsync<PaginaUsuariosDto>("/api/v1/usuarios?busqueda=ADMINISTRADOR");
        Assert.NotNull(busqueda);
        Assert.Equal("admin", Assert.Single(busqueda.Items).NombreUsuario);
        PaginaUsuariosDto? pagina = await _cliente.GetFromJsonAsync<PaginaUsuariosDto>("/api/v1/usuarios?pagina=2&tamanoPagina=2");
        Assert.NotNull(pagina);
        Assert.Equal(4, pagina.Total);
        Assert.Equal(2, pagina.Items.Count);
        Assert.Equal(new[] { "sin.roles", "ventas" }, pagina.Items.Select(usuario => usuario.NombreUsuario));
        PaginaUsuariosDto? porUsuario = await _cliente.GetFromJsonAsync<PaginaUsuariosDto>("/api/v1/usuarios?busqueda=sin.roles");
        PaginaUsuariosDto? porCorreo = await _cliente.GetFromJsonAsync<PaginaUsuariosDto>("/api/v1/usuarios?busqueda=inactivo%40titanglobal.example");
        Assert.NotNull(porUsuario);
        Assert.NotNull(porCorreo);
        Assert.Equal("sin.roles", Assert.Single(porUsuario.Items).NombreUsuario);
        Assert.Equal("inactivo", Assert.Single(porCorreo.Items).NombreUsuario);
    }

    [Fact]
    public async Task ResumenCuentaUsuariosActivosSinRolYPorRol()
    {
        ResumenUsuariosDto? resumen = await _cliente.GetFromJsonAsync<ResumenUsuariosDto>("/api/v1/usuarios/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.Total);
        Assert.Equal(3, resumen.Activos);
        Assert.Equal(1, resumen.Inactivos);
        Assert.Equal(1, resumen.SinRol);
        Assert.Equal(new[] { "ADMIN", "CONSULTA", "VENTAS" }, resumen.PorRol.Select(rol => rol.Codigo));
        Assert.Equal(new[] { 1, 2, 1 }, resumen.PorRol.Select(rol => rol.Usuarios));
    }

    [Fact]
    public async Task OpenApiIncluyeLasSieteOperacionesDeUsuarios()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");
        Assert.True(rutas.GetProperty("/api/v1/usuarios").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/usuarios").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/usuarios/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/usuarios/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/usuarios/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/usuarios/{id}/permisos").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/usuarios/resumen").TryGetProperty("get", out _));
    }

    private async Task AssertCambioAdminInvalidoAsync(int id, bool activo, List<int> rolIds, string metodo)
    {
        string[] antes = await LeerUsuariosAsync();
        GuardarUsuarioRequestDto solicitud = CrearSolicitud("admin", "Administrador", "admin@titanglobal.example");
        solicitud.Activo = activo;
        solicitud.RolIds = rolIds;
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"/api/v1/usuarios/{id}", solicitud);
        Assert.Equal("PUT", metodo);
        await AssertErrorAsync(respuesta, 409, "Debe quedar al menos un usuario activo con el rol ADMIN.");
        Assert.Equal(antes, await LeerUsuariosAsync());
    }

    private async Task<string[]> LeerUsuariosAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        List<Usuario> usuarios = await contexto.Usuarios.AsNoTracking().OrderBy(usuario => usuario.Id).ToListAsync();
        List<UsuarioRol> relaciones = await contexto.UsuariosRoles.AsNoTracking().OrderBy(enlace => enlace.UsuarioId)
            .ThenBy(enlace => enlace.RolId).ToListAsync();
        return usuarios
            .OrderBy(usuario => usuario.Id)
            .Select(usuario => $"{usuario.Id}|{usuario.NombreUsuario}|{usuario.NombreCompleto}|{usuario.Email}|{usuario.Activo}|{usuario.FechaAlta:o}|{string.Join(',', relaciones.Where(enlace => enlace.UsuarioId == usuario.Id).Select(enlace => enlace.RolId))}")
            .ToArray();
    }

    private async Task<Usuario?> ObtenerUsuarioAsync(int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Usuarios.AsNoTracking().SingleOrDefaultAsync(usuario => usuario.Id == id);
    }

    private void RestablecerDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.UsuariosRoles.RemoveRange(contexto.UsuariosRoles);
        contexto.Usuarios.RemoveRange(contexto.Usuarios);
        contexto.PermisosRoles.RemoveRange(contexto.PermisosRoles);
        contexto.Roles.RemoveRange(contexto.Roles);
        contexto.SaveChanges();
        contexto.Roles.AddRange(
            new Rol { Id = 1, Codigo = "ADMIN", Nombre = "Administrador", EsSistema = true },
            new Rol { Id = 2, Codigo = "VENTAS", Nombre = "Ventas" },
            new Rol { Id = 3, Codigo = "CONSULTA", Nombre = "Consulta" });
        List<PermisoRol> permisos = new List<PermisoRol>();
        for (int numero = 1; numero <= 80; numero++)
        {
            permisos.Add(new PermisoRol { RolId = 1, ModuloNumero = numero, PuedeLeer = true, PuedeEscribir = true });
        }

        permisos.Add(new PermisoRol { RolId = 2, ModuloNumero = 1, PuedeLeer = true, PuedeEscribir = true });
        permisos.Add(new PermisoRol { RolId = 3, ModuloNumero = 2, PuedeLeer = true, PuedeEscribir = false });
        contexto.PermisosRoles.AddRange(permisos);
        contexto.Usuarios.AddRange(
            new Usuario
            {
                Id = 1,
                NombreUsuario = "admin",
                NombreCompleto = "Administrador",
                Email = "admin@titanglobal.example",
                Activo = true,
                FechaAlta = DateTime.UtcNow,
                Roles = new List<UsuarioRol> { new UsuarioRol { RolId = 1 } }
            },
            new Usuario
            {
                Id = 2,
                NombreUsuario = "ventas",
                NombreCompleto = "Usuario de Ventas",
                Email = "ventas@titanglobal.example",
                Activo = true,
                FechaAlta = DateTime.UtcNow,
                Roles = new List<UsuarioRol> { new UsuarioRol { RolId = 2 }, new UsuarioRol { RolId = 3 } }
            },
            new Usuario
            {
                Id = 3,
                NombreUsuario = "sin.roles",
                NombreCompleto = "Usuario sin roles",
                Email = "sin.roles@titanglobal.example",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            },
            new Usuario
            {
                Id = 4,
                NombreUsuario = "inactivo",
                NombreCompleto = "Usuario Inactivo",
                Email = "inactivo@titanglobal.example",
                Activo = false,
                FechaAlta = DateTime.UtcNow,
                Roles = new List<UsuarioRol> { new UsuarioRol { RolId = 3 } }
            });
        contexto.SaveChanges();
    }

    private static GuardarUsuarioRequestDto CrearSolicitud(string nombreUsuario, string nombreCompleto, string email)
    {
        return new GuardarUsuarioRequestDto
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto,
            Email = email,
            Activo = true
        };
    }

    private static async Task AssertErrorAsync(HttpResponseMessage respuesta, int estado, string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
