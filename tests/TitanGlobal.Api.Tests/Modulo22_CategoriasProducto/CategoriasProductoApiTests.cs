using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo22_CategoriasProducto;

[Collection("Modulo04")]
public class CategoriasProductoApiTests
{
    private const string Ruta = "/api/v1/categorias-producto";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public CategoriasProductoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task ListarPorDefectoDevuelveSoloActivasOrdenadasPorNombre()
    {
        List<CategoriaProductoDto>? categorias = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(Ruta);

        Assert.NotNull(categorias);
        Assert.Equal(new[] { "Electronica", "Hogar" }, categorias.Select(categoria => categoria.Nombre).ToArray());
        Assert.All(categorias, categoria => Assert.True(categoria.Activa));
    }

    [Fact]
    public async Task ListarIncluyendoInactivasDevuelveTodas()
    {
        List<CategoriaProductoDto>? categorias = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(
            Ruta + "?incluirInactivas=true");

        Assert.NotNull(categorias);
        Assert.Equal(
            new[] { "Electronica", "Ferreteria", "Hogar" },
            categorias.Select(categoria => categoria.Nombre).ToArray());
    }

    [Fact]
    public async Task BuscarFiltraPorNombreSinDistinguirMayusculas()
    {
        List<CategoriaProductoDto>? porHogar = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(
            Ruta + "?buscar=HOG");
        List<CategoriaProductoDto>? porInactiva = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(
            Ruta + "?buscar=ferre&incluirInactivas=true");
        List<CategoriaProductoDto>? sinResultados = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(
            Ruta + "?buscar=noexiste");

        Assert.NotNull(porHogar);
        Assert.Equal("Hogar", Assert.Single(porHogar).Nombre);
        Assert.NotNull(porInactiva);
        Assert.Equal("Ferreteria", Assert.Single(porInactiva).Nombre);
        Assert.NotNull(sinResultados);
        Assert.Empty(sinResultados);
    }

    [Fact]
    public async Task ObtenerDevuelveElDetalleDeLaCategoria()
    {
        CategoriaProductoDto? categoria = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/2");

        Assert.NotNull(categoria);
        Assert.Equal(2, categoria.Id);
        Assert.Equal("Hogar", categoria.Nombre);
        Assert.Equal("Articulos para el hogar", categoria.Descripcion);
        Assert.True(categoria.Activa);
    }

    [Fact]
    public async Task ObtenerCategoriaInexistenteDevuelveNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + "/999");

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "La categoría 999 no existe.");
    }

    [Fact]
    public async Task CrearRegistraCategoriaActivaYSePuedeConsultar()
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto
        {
            Nombre = "  Jardineria  ",
            Descripcion = "Herramientas de jardin"
        };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        int id = cuerpo.GetProperty("id").GetInt32();
        CategoriaProductoDto? creada = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/" + id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Equal("Jardineria", creada.Nombre);
        Assert.Equal("Herramientas de jardin", creada.Descripcion);
        Assert.True(creada.Activa);
        Assert.Equal(4, await ContarCategoriasAsync());
    }

    [Fact]
    public async Task CrearConNombreVacioDevuelveBadRequest()
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto { Nombre = "   " };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre de la categoría es obligatorio.");
        Assert.Equal(3, await ContarCategoriasAsync());
    }

    [Fact]
    public async Task CrearConDescripcionMuyLargaDevuelveBadRequest()
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto
        {
            Nombre = "Larga",
            Descripcion = new string('x', 251)
        };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La descripción de la categoría no puede superar 250 caracteres.");
    }

    [Fact]
    public async Task CrearConNombreDeMasDeCienCaracteresDevuelveBadRequest()
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto { Nombre = new string('n', 101) };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre de la categoría no puede superar 100 caracteres.");
        Assert.Equal(3, await ContarCategoriasAsync());
    }

    [Fact]
    public async Task CrearConNombreDeExactamenteCienCaracteresEsValido()
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto { Nombre = new string('n', 100) };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(4, await ContarCategoriasAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearConDescripcionVaciaLaGuardaComoNull(string descripcion)
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto
        {
            Nombre = "Sin descripcion",
            Descripcion = descripcion
        };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        int id = cuerpo.GetProperty("id").GetInt32();
        CategoriaProductoDto? creada = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/" + id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Null(creada.Descripcion);
    }

    [Fact]
    public async Task CrearConNombreDeCategoriaInactivaDevuelveConflicto()
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto { Nombre = "FERRETERIA" };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe una categoría con el nombre FERRETERIA.");
        Assert.Equal(3, await ContarCategoriasAsync());
    }

    [Theory]
    [InlineData("Electronica")]
    [InlineData("ELECTRONICA")]
    [InlineData("electronica")]
    public async Task CrearConNombreDuplicadoDevuelveConflictoSinImportarMayusculas(string nombre)
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto { Nombre = nombre };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe una categoría con el nombre " + nombre + ".");
        Assert.Equal(3, await ContarCategoriasAsync());
    }

    [Fact]
    public async Task ActualizarModificaNombreYDescripcion()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto
        {
            Nombre = "Hogar y Cocina",
            Descripcion = "Nueva descripcion"
        };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);
        CategoriaProductoDto? actualizada = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/2");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(actualizada);
        Assert.Equal("Hogar y Cocina", actualizada.Nombre);
        Assert.Equal("Nueva descripcion", actualizada.Descripcion);
        Assert.True(actualizada.Activa);
    }

    [Fact]
    public async Task ActualizarConElMismoNombreDeLaMismaCategoriaEsValido()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto
        {
            Nombre = "HOGAR",
            Descripcion = "Solo cambia la descripcion"
        };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task ActualizarConNombreDeOtraCategoriaDevuelveConflicto()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto { Nombre = "electronica" };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe una categoría con el nombre electronica.");
    }

    [Fact]
    public async Task ActualizarConNombreVacioDevuelveBadRequest()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto { Nombre = "   " };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre de la categoría es obligatorio.");
    }

    [Fact]
    public async Task ActualizarConNombreDeMasDeCienCaracteresDevuelveBadRequest()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto { Nombre = new string('n', 101) };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre de la categoría no puede superar 100 caracteres.");
    }

    [Fact]
    public async Task ActualizarConDescripcionMuyLargaDevuelveBadRequest()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto
        {
            Nombre = "Hogar",
            Descripcion = new string('x', 251)
        };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La descripción de la categoría no puede superar 250 caracteres.");
    }

    [Fact]
    public async Task ActualizarConDescripcionVaciaLaGuardaComoNull()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto
        {
            Nombre = "Hogar",
            Descripcion = "   "
        };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);
        CategoriaProductoDto? actualizada = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/2");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(actualizada);
        Assert.Null(actualizada.Descripcion);
    }

    [Fact]
    public async Task ActualizarConNombreDeCategoriaInactivaDevuelveConflicto()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto { Nombre = "ferreteria" };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/2", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe una categoría con el nombre ferreteria.");
    }

    [Fact]
    public async Task ActualizarCategoriaInexistenteDevuelveNotFound()
    {
        ActualizarCategoriaProductoDto solicitud = new ActualizarCategoriaProductoDto { Nombre = "Cualquiera" };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/999", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "La categoría 999 no existe.");
    }

    [Fact]
    public async Task DesactivarMarcaComoInactivaSinBorrarElRegistro()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync(Ruta + "/1");
        CategoriaProductoDto? desactivada = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/1");
        List<CategoriaProductoDto>? activas = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(Ruta);
        List<CategoriaProductoDto>? todas = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(
            Ruta + "?incluirInactivas=true");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.NotNull(desactivada);
        Assert.False(desactivada.Activa);
        Assert.NotNull(activas);
        Assert.DoesNotContain(activas, categoria => categoria.Id == 1);
        Assert.NotNull(todas);
        Assert.Contains(todas, categoria => categoria.Id == 1);
        Assert.Equal(3, await ContarCategoriasAsync());
    }

    [Fact]
    public async Task DesactivarDosVecesDevuelveNoContentYDejaLaCategoriaInactiva()
    {
        HttpResponseMessage primera = await _clienteHttp.DeleteAsync(Ruta + "/1");
        HttpResponseMessage segunda = await _clienteHttp.DeleteAsync(Ruta + "/1");
        CategoriaProductoDto? categoria = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/1");

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
        Assert.NotNull(categoria);
        Assert.False(categoria.Activa);
        Assert.Equal(3, await ContarCategoriasAsync());
    }

    [Fact]
    public async Task DesactivarCategoriaInexistenteDevuelveNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync(Ruta + "/999");

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "La categoría 999 no existe.");
    }

    [Fact]
    public async Task ResumenDevuelveTotalesDeActivasEInactivas()
    {
        ResumenCategoriasProductoDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenCategoriasProductoDto>(
            Ruta + "/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.TotalCategorias);
        Assert.Equal(2, resumen.CategoriasActivas);
        Assert.Equal(1, resumen.CategoriasInactivas);
    }

    [Fact]
    public async Task ResumenSinCategoriasDevuelveCeros()
    {
        using (IServiceScope scope = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            contexto.CategoriasProducto.RemoveRange(contexto.CategoriasProducto);
            contexto.SaveChanges();
        }

        ResumenCategoriasProductoDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenCategoriasProductoDto>(
            Ruta + "/resumen");
        List<CategoriaProductoDto>? categorias = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(Ruta);

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.TotalCategorias);
        Assert.Equal(0, resumen.CategoriasActivas);
        Assert.Equal(0, resumen.CategoriasInactivas);
        Assert.NotNull(categorias);
        Assert.Empty(categorias);
    }

    [Fact]
    public async Task OpenApiContieneLasSeisOperacionesDeCategoriasProducto()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/categorias-producto").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/categorias-producto").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/categorias-producto/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/categorias-producto/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/categorias-producto/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/categorias-producto/resumen").TryGetProperty("get", out _));
    }
    
    [Fact]
    public async Task ReactivarMarcaComoActivaUnaCategoriaInactiva()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(Ruta + "/3/reactivar", null);
        CategoriaProductoDto? reactivada = await _clienteHttp.GetFromJsonAsync<CategoriaProductoDto>(Ruta + "/3");
        List<CategoriaProductoDto>? activas = await _clienteHttp.GetFromJsonAsync<List<CategoriaProductoDto>>(Ruta);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.NotNull(reactivada);
        Assert.True(reactivada.Activa);
        Assert.NotNull(activas);
        Assert.Contains(activas, categoria => categoria.Id == 3);
    }

    [Fact]
    public async Task ReactivarCategoriaActivaDevuelveConflicto()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(Ruta + "/1/reactivar", null);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.Conflict, 409, "La categoría 1 ya está activa.");
    }

    [Fact]
    public async Task ReactivarCategoriaInexistenteDevuelveNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(Ruta + "/999/reactivar", null);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "La categoría 999 no existe.");
    }

    [Fact]
    public async Task ReactivarActualizaElResumen()
    {
        await _clienteHttp.PostAsync(Ruta + "/3/reactivar", null);

        ResumenCategoriasProductoDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenCategoriasProductoDto>(
            Ruta + "/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.TotalCategorias);
        Assert.Equal(3, resumen.CategoriasActivas);
        Assert.Equal(0, resumen.CategoriasInactivas);
    }
    
    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.CategoriasProducto.RemoveRange(contexto.CategoriasProducto);
        contexto.CategoriasProducto.AddRange(
            new CategoriaProducto { Id = 1, Nombre = "Electronica", Descripcion = "Equipos electronicos", Activa = true },
            new CategoriaProducto { Id = 2, Nombre = "Hogar", Descripcion = "Articulos para el hogar", Activa = true },
            new CategoriaProducto { Id = 3, Nombre = "Ferreteria", Descripcion = null, Activa = false });
        contexto.SaveChanges();
    }

    private async Task<int> ContarCategoriasAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.CategoriasProducto.CountAsync();
    }

    private static async Task AfirmarErrorNegocioAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode codigoHttp,
        int estado,
        string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal(codigoHttp, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
