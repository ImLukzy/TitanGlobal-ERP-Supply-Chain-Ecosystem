using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.UseCases.Modulo72_RolesPermisos;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo72_RolesPermisos;

[Collection("Modulo04")]
public class RolesPermisosApiTests
{
    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public RolesPermisosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerRoles();
    }

    [Fact]
    public async Task CatalogoCoincideConLosOchentaModulosDeArquitectura()
    {
        string? directorio = AppContext.BaseDirectory;
        while (directorio is not null && !File.Exists(Path.Combine(directorio, "docs", "arquitectura.md")))
        {
            directorio = Directory.GetParent(directorio)?.FullName;
        }

        Assert.NotNull(directorio);
        string arquitectura = await File.ReadAllTextAsync(Path.Combine(directorio!, "docs", "arquitectura.md"));
        List<string> nombres = arquitectura.Split('\n')
            .Select(linea => System.Text.RegularExpressions.Regex.Match(linea, @"^- \d{2} (.+)$"))
            .Where(coincidencia => coincidencia.Success)
            .Select(coincidencia => coincidencia.Groups[1].Value)
            .ToList();

        Assert.Equal(80, CatalogoModulos.Todos.Count);
        Assert.Equal(Enumerable.Range(1, 80), CatalogoModulos.Todos.Select(modulo => modulo.ModuloNumero));
        Assert.Equal(nombres, CatalogoModulos.Todos.Select(modulo => modulo.ModuloNombre));
    }

    [Fact]
    public async Task NoPermiteEliminarRolAsignadoAUsuarios()
    {
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            contexto.Usuarios.Add(new Usuario
            {
                NombreUsuario = "usuario.ventas",
                NombreCompleto = "Usuario de Ventas",
                Email = "usuario.ventas@titanglobal.example",
                Activo = true,
                FechaAlta = DateTime.UtcNow,
                Roles = new List<UsuarioRol> { new UsuarioRol { RolId = 2 } }
            });
            await contexto.SaveChangesAsync();
        }

        HttpResponseMessage respuesta = await _cliente.DeleteAsync("/api/v1/roles/2");
        await AssertErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "No se puede eliminar el rol VENTAS porque tiene usuarios.");
        Assert.Equal("VENTAS", await ObtenerCodigoAsync(2));
    }

    [Fact]
    public async Task ObtenerPermisosDevuelveOchentaModulosYFalsosSiFaltaFila()
    {
        Assert.Equal(2, await ContarPermisosAsync(2));
        List<PermisoModuloDto>? permisos = await _cliente.GetFromJsonAsync<List<PermisoModuloDto>>("/api/v1/roles/2/permisos");

        Assert.NotNull(permisos);
        Assert.Equal(80, permisos.Count);
        Assert.True(permisos[0].PuedeLeer && permisos[0].PuedeEscribir);
        Assert.True(permisos[20].PuedeLeer && !permisos[20].PuedeEscribir);
        Assert.False(permisos[1].PuedeLeer || permisos[1].PuedeEscribir);
        Assert.Equal(80, permisos[79].ModuloNumero);
    }

    [Fact]
    public async Task ReemplazarPermisosQuitaOmitidosYCuentaEscrituraComoLectura()
    {
        GuardarPermisosRequestDto solicitud = new GuardarPermisosRequestDto
        {
            Permisos = new List<PermisoSolicitadoDto>
            {
                new PermisoSolicitadoDto { ModuloNumero = 1, PuedeLeer = true, PuedeEscribir = true },
                new PermisoSolicitadoDto { ModuloNumero = 2, PuedeLeer = true, PuedeEscribir = false }
            }
        };
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/roles/2/permisos", solicitud);
        List<PermisoModuloDto>? resultado = await respuesta.Content.ReadFromJsonAsync<List<PermisoModuloDto>>();
        PaginaRolesDto? listado = await _cliente.GetFromJsonAsync<PaginaRolesDto>("/api/v1/roles?busqueda=ventas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(80, resultado.Count);
        Assert.True(resultado[0].PuedeLeer && resultado[0].PuedeEscribir);
        Assert.True(resultado[1].PuedeLeer && !resultado[1].PuedeEscribir);
        Assert.False(resultado[2].PuedeLeer || resultado[2].PuedeEscribir);
        Assert.NotNull(listado);
        Assert.Equal(2, Assert.Single(listado.Items).ModulosConLectura);
        Assert.Equal(1, Assert.Single(listado.Items).ModulosConEscritura);
    }

    [Fact]
    public async Task MatrizDevuelveEstadosExactosEnOrdenDeRolYModulo()
    {
        MatrizPermisosDto? matriz = await _cliente.GetFromJsonAsync<MatrizPermisosDto>("/api/v1/roles/matriz");

        Assert.NotNull(matriz);
        Assert.Equal(new[] { "ADMIN", "CONSULTA", "VENTAS" }, matriz.Roles.Select(rol => rol.Codigo));
        Assert.Equal(80, matriz.Modulos.Count);
        Assert.Equal(new[] { "Escritura", "Ninguno", "Escritura" }, matriz.Modulos[0].Permisos);
        Assert.Equal(new[] { "Escritura", "Ninguno", "Ninguno" }, matriz.Modulos[1].Permisos);
        Assert.Equal(new[] { "Escritura", "Ninguno", "Lectura" }, matriz.Modulos[20].Permisos);
    }

    [Theory]
    [InlineData("", "Nombre", null, 400, "El código es obligatorio.")]
    [InlineData("A", "Nombre", null, 400, "El código solo admite letras, números, guiones y guion bajo (2 a 30 caracteres).")]
    [InlineData("VENTAS", "Nombre", null, 409, "Ya existe un rol con el código VENTAS.")]
    [InlineData("NUEVO", " ", null, 400, "El nombre del rol es obligatorio.")]
    [InlineData("NUEVO", "NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN", null, 400, "El nombre del rol no puede superar 100 caracteres.")]
    [InlineData("NUEVO", "Nombre", "Descripción larga", 400, "La descripción no puede superar 300 caracteres.")]
    public async Task CrearRolInvalidoDevuelveLiteralSinCambiarFilas(string codigo, string nombre, string? descripcion, int estado, string mensaje)
    {
        if (mensaje == "La descripción no puede superar 300 caracteres.")
        {
            descripcion = new string('D', 301);
        }

        int totalAntes = await ContarRolesAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/roles", new GuardarRolRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = descripcion
        });

        await AssertErrorAsync(respuesta, estado == 409 ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest, estado, mensaje);
        Assert.Equal(totalAntes, await ContarRolesAsync());
    }

    [Theory]
    [InlineData(0, "El módulo 0 no existe.")]
    [InlineData(81, "El módulo 81 no existe.")]
    public async Task ModuloFueraDelCatalogoSeRechazaSinCambiarFilas(int numero, string mensaje)
    {
        await AssertPermissionErrorWithoutChangesAsync(new List<PermisoSolicitadoDto>
        {
            new PermisoSolicitadoDto { ModuloNumero = numero, PuedeLeer = true }
        }, mensaje);
    }

    [Fact]
    public async Task ModuloRepetidoSeRechazaSinCambiarFilas()
    {
        await AssertPermissionErrorWithoutChangesAsync(new List<PermisoSolicitadoDto>
        {
            new PermisoSolicitadoDto { ModuloNumero = 1, PuedeLeer = true },
            new PermisoSolicitadoDto { ModuloNumero = 1, PuedeLeer = true }
        }, "El módulo 1 está repetido.");
    }

    [Fact]
    public async Task EscrituraSinLecturaSeRechazaSinCambiarFilas()
    {
        await AssertPermissionErrorWithoutChangesAsync(new List<PermisoSolicitadoDto>
        {
            new PermisoSolicitadoDto { ModuloNumero = 3, PuedeLeer = false, PuedeEscribir = true }
        }, "El módulo 3 no puede tener escritura sin lectura.");
    }

    [Fact]
    public async Task RolInexistenteDevuelveNotFoundSinCambiarFilas()
    {
        int total = await ContarRolesAsync();
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/roles/9999");
        await AssertErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El rol 9999 no existe.");
        Assert.Equal(total, await ContarRolesAsync());
    }

    [Fact]
    public async Task RolDeSistemaNoPuedeCambiarCodigoNiEliminarse()
    {
        HttpResponseMessage actualizar = await _cliente.PutAsJsonAsync("/api/v1/roles/1", new GuardarRolRequestDto
        {
            Codigo = "SUPERADMIN",
            Nombre = "Administrador",
            Descripcion = "Actualizado"
        });
        await AssertErrorAsync(actualizar, HttpStatusCode.Conflict, 409,
            "El rol ADMIN es de sistema y no se puede modificar de esa forma.");
        HttpResponseMessage eliminar = await _cliente.DeleteAsync("/api/v1/roles/1");
        await AssertErrorAsync(eliminar, HttpStatusCode.Conflict, 409,
            "El rol ADMIN es de sistema y no se puede modificar de esa forma.");
        Assert.Equal("ADMIN", await ObtenerCodigoAsync(1));
    }

    [Fact]
    public async Task RolDeSistemaPuedeCambiarNombreSinCambiarCodigo()
    {
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/roles/1", new GuardarRolRequestDto
        {
            Codigo = "ADMIN",
            Nombre = "Administrador actualizado",
            Descripcion = "Permanece de sistema"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Administrador actualizado", (await _cliente.GetFromJsonAsync<RolDetalleDto>("/api/v1/roles/1"))?.Nombre);
    }

    [Fact]
    public async Task RolDeSistemaNoPuedePerderPermisos()
    {
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/roles/1/permisos", new GuardarPermisosRequestDto
        {
            Permisos = new List<PermisoSolicitadoDto>()
        });

        await AssertErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            "El rol ADMIN es de sistema y no se puede modificar de esa forma.");
        List<PermisoRol> permisos = await ObtenerPermisosGuardadosAsync(1);
        Assert.Equal(80, permisos.Count);
        Assert.All(permisos, permiso => Assert.True(permiso.PuedeLeer && permiso.PuedeEscribir));
    }

    [Fact]
    public async Task ListadoAplicaPaginacionYDevuelveTotal()
    {
        PaginaRolesDto? pagina = await _cliente.GetFromJsonAsync<PaginaRolesDto>("/api/v1/roles?pagina=2&tamanoPagina=1");

        Assert.NotNull(pagina);
        Assert.Equal(3, pagina.Total);
        Assert.Equal(2, pagina.Pagina);
        Assert.Equal(1, pagina.TamanoPagina);
        Assert.Single(pagina.Items);
        HttpResponseMessage errorPagina = await _cliente.GetAsync("/api/v1/roles?pagina=0");
        await AssertErrorAsync(errorPagina, HttpStatusCode.BadRequest, 400, "La página debe ser mayor o igual a 1.");
        HttpResponseMessage errorTamano = await _cliente.GetAsync("/api/v1/roles?tamanoPagina=101");
        await AssertErrorAsync(errorTamano, HttpStatusCode.BadRequest, 400, "El tamaño de página debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task OpenApiIncluyeLasOchoOperacionesDeRoles()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/roles").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/roles").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/roles/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/roles/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/roles/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/roles/{id}/permisos").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/roles/{id}/permisos").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/roles/matriz").TryGetProperty("get", out _));
    }

    private async Task AssertPermissionErrorWithoutChangesAsync(List<PermisoSolicitadoDto> permisos, string mensaje)
    {
        int cantidadAntes = await ContarPermisosAsync(2);
        List<PermisoRol> antes = await ObtenerPermisosGuardadosAsync(2);
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/roles/2/permisos",
            new GuardarPermisosRequestDto { Permisos = permisos });

        await AssertErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        List<PermisoRol> despues = await ObtenerPermisosGuardadosAsync(2);
        Assert.Equal(cantidadAntes, despues.Count);
        Assert.Equal(antes.OrderBy(fila => fila.ModuloNumero).Select(fila => (fila.ModuloNumero, fila.PuedeLeer, fila.PuedeEscribir)),
            despues.OrderBy(fila => fila.ModuloNumero).Select(fila => (fila.ModuloNumero, fila.PuedeLeer, fila.PuedeEscribir)));
    }

    private void RestablecerRoles()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.UsuariosRoles.RemoveRange(contexto.UsuariosRoles);
        contexto.Usuarios.RemoveRange(contexto.Usuarios);
        contexto.SaveChanges();
        contexto.PermisosRoles.RemoveRange(contexto.PermisosRoles);
        contexto.Roles.RemoveRange(contexto.Roles);
        contexto.SaveChanges();
        List<Rol> roles = new List<Rol>
        {
            new Rol { Id = 1, Codigo = "ADMIN", Nombre = "Administrador", EsSistema = true },
            new Rol { Id = 2, Codigo = "VENTAS", Nombre = "Ventas" },
            new Rol { Id = 3, Codigo = "CONSULTA", Nombre = "Rol Consulta" }
        };
        contexto.Roles.AddRange(roles);
        List<PermisoRol> permisos = new List<PermisoRol>();
        for (int numero = 1; numero <= 80; numero++)
        {
            permisos.Add(new PermisoRol { RolId = 1, ModuloNumero = numero, PuedeLeer = true, PuedeEscribir = true });
        }

        permisos.Add(new PermisoRol { RolId = 2, ModuloNumero = 1, PuedeLeer = true, PuedeEscribir = true });
        permisos.Add(new PermisoRol { RolId = 2, ModuloNumero = 21, PuedeLeer = true, PuedeEscribir = false });
        contexto.PermisosRoles.AddRange(permisos);
        contexto.SaveChanges();
    }

    private async Task<int> ContarRolesAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Roles.CountAsync();
    }

    private async Task<int> ContarPermisosAsync(int rolId)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.PermisosRoles.CountAsync(permiso => permiso.RolId == rolId);
    }

    private async Task<List<PermisoRol>> ObtenerPermisosGuardadosAsync(int rolId)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.PermisosRoles.Where(permiso => permiso.RolId == rolId).AsNoTracking().ToListAsync();
    }

    private async Task<string?> ObtenerCodigoAsync(int rolId)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Roles.Where(rol => rol.Id == rolId).Select(rol => rol.Codigo).SingleOrDefaultAsync();
    }

    private static async Task AssertErrorAsync(HttpResponseMessage respuesta, HttpStatusCode http, int estado, string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal(http, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
