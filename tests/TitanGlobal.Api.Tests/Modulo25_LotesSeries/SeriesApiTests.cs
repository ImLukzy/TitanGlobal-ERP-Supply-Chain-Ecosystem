using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo25_LotesSeries;

[Collection("Modulo04")]
public class SeriesApiTests
{
    private const string Ruta = "/api/v1/lotes-series/series";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public SeriesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task ListarFiltraPorProductoLoteEstadoYBusqueda()
    {
        PaginaSeriesDto? porProducto = await _clienteHttp.GetFromJsonAsync<PaginaSeriesDto>(Ruta + "?productoId=1");
        PaginaSeriesDto? porLote = await _clienteHttp.GetFromJsonAsync<PaginaSeriesDto>(Ruta + "?loteId=1");
        PaginaSeriesDto? porEstado = await _clienteHttp.GetFromJsonAsync<PaginaSeriesDto>(Ruta + "?estado=asignada");
        PaginaSeriesDto? porBusqueda = await _clienteHttp.GetFromJsonAsync<PaginaSeriesDto>(Ruta + "?busqueda=sn-f");

        Assert.NotNull(porProducto);
        Assert.Equal(new[] { "SN-1", "SN-2", "SN-3" }, porProducto.Items.Select(serie => serie.NumeroSerie).ToArray());
        Assert.NotNull(porLote);
        Assert.Equal(new[] { "SN-1", "SN-2" }, porLote.Items.Select(serie => serie.NumeroSerie).ToArray());
        Assert.NotNull(porEstado);
        Assert.Equal("SN-2", Assert.Single(porEstado.Items).NumeroSerie);
        Assert.NotNull(porBusqueda);
        Assert.Equal("SN-F1", Assert.Single(porBusqueda.Items).NumeroSerie);
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?estado=Otro", "El estado de la serie no es válido.")]
    public async Task ListarConFiltrosInvalidosDevuelveBadRequest(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task CrearSerieSinLoteNormalizaElNumero()
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 1, NumeroSerie = "  sn-nueva " };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        PaginaSeriesDto? busqueda = await _clienteHttp.GetFromJsonAsync<PaginaSeriesDto>(Ruta + "?busqueda=sn-nueva");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(busqueda);
        SerieListadoDto creada = Assert.Single(busqueda.Items);
        Assert.Equal("SN-NUEVA", creada.NumeroSerie);
        Assert.Equal("Disponible", creada.Estado);
        Assert.Null(creada.LoteId);
        Assert.Equal(5, await ContarSeriesAsync());
    }

    [Fact]
    public async Task CrearSerieEnUnLoteDelMismoProductoEsValido()
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 1, LoteId = 1, NumeroSerie = "SN-9" };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        PaginaSeriesDto? delLote = await _clienteHttp.GetFromJsonAsync<PaginaSeriesDto>(Ruta + "?loteId=1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(delLote);
        Assert.Equal(3, delLote.Total);
    }

    [Theory]
    [InlineData("", "El número de serie es obligatorio.")]
    [InlineData("SN 9", "El número de serie solo admite letras, números y guiones (1 a 50 caracteres).")]
    public async Task CrearConNumeroInvalidoDevuelveBadRequestSinCambiarDatos(string numero, string mensaje)
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 1, NumeroSerie = numero };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(4, await ContarSeriesAsync());
    }

    [Fact]
    public async Task CrearConProductoInexistenteDevuelveBadRequest()
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 999, NumeroSerie = "SN-9" };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El producto 999 no existe.");
    }

    [Fact]
    public async Task CrearConLoteInexistenteDevuelveBadRequest()
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 1, LoteId = 99, NumeroSerie = "SN-9" };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El lote 99 no existe.");
    }

    [Fact]
    public async Task CrearConLoteDeOtroProductoDevuelveConflicto()
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 1, LoteId = 3, NumeroSerie = "SN-9" };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El lote 3 no pertenece al producto 1.");
    }

    [Fact]
    public async Task CrearEnUnLoteBloqueadoDevuelveConflicto()
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 3, LoteId = 2, NumeroSerie = "SN-9" };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El lote LOT-D está bloqueado.");
    }

    [Fact]
    public async Task CrearEnUnLoteLlenoDevuelveConflicto()
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 2, LoteId = 3, NumeroSerie = "SN-9" };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El lote LOT-F ya tiene registradas todas sus series (1).");
    }

    [Theory]
    [InlineData("SN-1")]
    [InlineData("sn-1")]
    public async Task CrearConNumeroRepetidoDevuelveConflictoSinImportarMayusculas(string numero)
    {
        GuardarSerieRequestDto solicitud = new GuardarSerieRequestDto { ProductoId = 1, NumeroSerie = numero };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe la serie SN-1 para el producto 1.");
        Assert.Equal(4, await ContarSeriesAsync());
    }

    [Fact]
    public async Task CambiarEstadoAplicaUnaTransicionPermitida()
    {
        CambiarEstadoSerieRequestDto solicitud = new CambiarEstadoSerieRequestDto { Estado = "asignada" };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/1/estado", solicitud);
        PaginaSeriesDto? busqueda = await _clienteHttp.GetFromJsonAsync<PaginaSeriesDto>(Ruta + "?busqueda=sn-1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(busqueda);
        Assert.Equal("Asignada", Assert.Single(busqueda.Items).Estado);
    }

    [Fact]
    public async Task CambiarEstadoConValorInvalidoDevuelveBadRequest()
    {
        CambiarEstadoSerieRequestDto solicitud = new CambiarEstadoSerieRequestDto { Estado = "Otro" };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/1/estado", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El estado de la serie no es válido.");
    }

    [Fact]
    public async Task CambiarEstadoDeUnaSerieDeBajaDevuelveConflicto()
    {
        CambiarEstadoSerieRequestDto solicitud = new CambiarEstadoSerieRequestDto { Estado = "Disponible" };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/4/estado", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "No se puede pasar la serie de Baja a Disponible.");
    }

    [Fact]
    public async Task CambiarEstadoDeUnaSerieInexistenteDevuelveNotFound()
    {
        CambiarEstadoSerieRequestDto solicitud = new CambiarEstadoSerieRequestDto { Estado = "Baja" };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/999/estado", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "La serie 999 no existe.");
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.UnidadesSerie.RemoveRange(contexto.UnidadesSerie);
        contexto.Lotes.RemoveRange(contexto.Lotes);
        contexto.Lotes.AddRange(
            new Lote { Id = 1, ProductoId = 1, Codigo = "LOT-A", Cantidad = 100, Estado = "Disponible" },
            new Lote { Id = 2, ProductoId = 3, Codigo = "LOT-D", Cantidad = 20, Estado = "Bloqueado" },
            new Lote { Id = 3, ProductoId = 2, Codigo = "LOT-F", Cantidad = 1, Estado = "Disponible" });
        contexto.UnidadesSerie.AddRange(
            new UnidadSerie { Id = 1, ProductoId = 1, LoteId = 1, NumeroSerie = "SN-1", Estado = "Disponible" },
            new UnidadSerie { Id = 2, ProductoId = 1, LoteId = 1, NumeroSerie = "SN-2", Estado = "Asignada" },
            new UnidadSerie { Id = 3, ProductoId = 2, LoteId = 3, NumeroSerie = "SN-F1", Estado = "Disponible" },
            new UnidadSerie { Id = 4, ProductoId = 1, LoteId = null, NumeroSerie = "SN-3", Estado = "Baja" });
        contexto.SaveChanges();
    }

    private async Task<int> ContarSeriesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.UnidadesSerie.CountAsync();
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
