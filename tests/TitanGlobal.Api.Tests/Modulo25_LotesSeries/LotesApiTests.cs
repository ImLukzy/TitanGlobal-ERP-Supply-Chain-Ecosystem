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
public class LotesApiTests
{
    private const string Ruta = "/api/v1/lotes-series/lotes";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public LotesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task ListarFiltraPorProductoEstadoVencimientoYBusqueda()
    {
        PaginaLotesDto? porProducto = await _clienteHttp.GetFromJsonAsync<PaginaLotesDto>(Ruta + "?productoId=1");
        PaginaLotesDto? porEstado = await _clienteHttp.GetFromJsonAsync<PaginaLotesDto>(Ruta + "?estado=cuarentena");
        PaginaLotesDto? vencidos = await _clienteHttp.GetFromJsonAsync<PaginaLotesDto>(Ruta + "?vencidos=true");
        PaginaLotesDto? vigentes = await _clienteHttp.GetFromJsonAsync<PaginaLotesDto>(Ruta + "?vencidos=false");
        PaginaLotesDto? porBusqueda = await _clienteHttp.GetFromJsonAsync<PaginaLotesDto>(Ruta + "?busqueda=lot-d");

        Assert.NotNull(porProducto);
        Assert.Equal(new[] { "LOT-A", "LOT-B" }, porProducto.Items.Select(lote => lote.Codigo).ToArray());
        Assert.NotNull(porEstado);
        Assert.Equal("LOT-C", Assert.Single(porEstado.Items).Codigo);
        Assert.NotNull(vencidos);
        Assert.Equal("LOT-B", Assert.Single(vencidos.Items).Codigo);
        Assert.True(vencidos.Items[0].Vencido);
        Assert.NotNull(vigentes);
        Assert.Equal(new[] { "LOT-A", "LOT-C", "LOT-D" }, vigentes.Items.Select(lote => lote.Codigo).ToArray());
        Assert.NotNull(porBusqueda);
        Assert.Equal("LOT-D", Assert.Single(porBusqueda.Items).Codigo);
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?estado=Otro", "El estado del lote no es válido.")]
    public async Task ListarConFiltrosInvalidosDevuelveBadRequest(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ObtenerDevuelveElDetalleConElTotalDeSeries()
    {
        LoteDetalleDto? lote = await _clienteHttp.GetFromJsonAsync<LoteDetalleDto>(Ruta + "/1");

        Assert.NotNull(lote);
        Assert.Equal("LOT-A", lote.Codigo);
        Assert.Equal(1, lote.ProductoId);
        Assert.Equal(100, lote.Cantidad);
        Assert.Equal(2, lote.TotalSeries);
        Assert.False(lote.Vencido);
        Assert.False(string.IsNullOrEmpty(lote.ProductoNombre));
    }

    [Fact]
    public async Task ObtenerLoteInexistenteDevuelveNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + "/999");

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "El lote 999 no existe.");
    }

    [Fact]
    public async Task CrearNormalizaElCodigoYRegistraElLote()
    {
        GuardarLoteRequestDto solicitud = CrearSolicitud(1, "  lot-z  ", 10, "disponible");

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        int id = cuerpo.GetProperty("id").GetInt32();
        LoteDetalleDto? creado = await _clienteHttp.GetFromJsonAsync<LoteDetalleDto>(Ruta + "/" + id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        Assert.Equal("LOT-Z", creado.Codigo);
        Assert.Equal("Disponible", creado.Estado);
        Assert.Equal(5, await ContarLotesAsync());
    }

    [Theory]
    [InlineData("", 10, "Disponible", "El código del lote es obligatorio.")]
    [InlineData("LOT Z", 10, "Disponible", "El código del lote solo admite letras, números y guiones (1 a 30 caracteres).")]
    [InlineData("LOT-Z", 0, "Disponible", "La cantidad del lote debe estar entre 1 y 1000000.")]
    [InlineData("LOT-Z", 1000001, "Disponible", "La cantidad del lote debe estar entre 1 y 1000000.")]
    [InlineData("LOT-Z", 10, "Otro", "El estado del lote no es válido.")]
    public async Task CrearConDatosInvalidosDevuelveBadRequestSinCambiarDatos(
        string codigo,
        int cantidad,
        string estado,
        string mensaje)
    {
        GuardarLoteRequestDto solicitud = CrearSolicitud(1, codigo, cantidad, estado);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(4, await ContarLotesAsync());
    }

    [Fact]
    public async Task CrearConProductoInexistenteDevuelveBadRequest()
    {
        GuardarLoteRequestDto solicitud = CrearSolicitud(999, "LOT-Z", 10, "Disponible");

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El producto 999 no existe.");
    }

    [Fact]
    public async Task CrearConVencimientoAnteriorALaFabricacionDevuelveBadRequest()
    {
        GuardarLoteRequestDto solicitud = CrearSolicitud(1, "LOT-Z", 10, "Disponible");
        solicitud.FechaFabricacion = DateTime.UtcNow.Date.AddDays(-1);
        solicitud.FechaVencimiento = DateTime.UtcNow.Date.AddDays(-5);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La fecha de vencimiento no puede ser anterior a la de fabricación.");
    }

    [Theory]
    [InlineData("LOT-A")]
    [InlineData("lot-a")]
    public async Task CrearConCodigoRepetidoDevuelveConflictoSinImportarMayusculas(string codigo)
    {
        GuardarLoteRequestDto solicitud = CrearSolicitud(1, codigo, 10, "Disponible");

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe el lote LOT-A para el producto 1.");
        Assert.Equal(4, await ContarLotesAsync());
    }

    [Fact]
    public async Task ActualizarModificaCantidadEstadoYFechas()
    {
        ActualizarLoteRequestDto solicitud = CrearActualizacion(200, "Cuarentena");

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/1", solicitud);
        LoteDetalleDto? actualizado = await _clienteHttp.GetFromJsonAsync<LoteDetalleDto>(Ruta + "/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(actualizado);
        Assert.Equal(200, actualizado.Cantidad);
        Assert.Equal("Cuarentena", actualizado.Estado);
        Assert.Equal("LOT-A", actualizado.Codigo);
    }

    [Fact]
    public async Task ActualizarLoteInexistenteDevuelveNotFound()
    {
        ActualizarLoteRequestDto solicitud = CrearActualizacion(10, "Disponible");

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/999", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "El lote 999 no existe.");
    }

    [Theory]
    [InlineData(0, "Disponible", "La cantidad del lote debe estar entre 1 y 1000000.")]
    [InlineData(10, "Otro", "El estado del lote no es válido.")]
    public async Task ActualizarConDatosInvalidosDevuelveBadRequest(int cantidad, string estado, string mensaje)
    {
        ActualizarLoteRequestDto solicitud = CrearActualizacion(cantidad, estado);

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/1", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ActualizarConCantidadMenorQueSusSeriesDevuelveConflicto()
    {
        ActualizarLoteRequestDto solicitud = CrearActualizacion(1, "Disponible");

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/1", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "La cantidad del lote no puede ser menor que sus series registradas (2).");
    }

    [Fact]
    public async Task ResumenDevuelveTotalesPorEstadoVencidosYPorVencer()
    {
        ResumenLotesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenLotesDto>(Ruta + "/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.TotalLotes);
        Assert.Equal(2, resumen.Disponibles);
        Assert.Equal(1, resumen.EnCuarentena);
        Assert.Equal(1, resumen.Bloqueados);
        Assert.Equal(1, resumen.Vencidos);
        Assert.Equal(1, resumen.PorVencer);
    }

    private static GuardarLoteRequestDto CrearSolicitud(int productoId, string codigo, int cantidad, string estado)
    {
        return new GuardarLoteRequestDto
        {
            ProductoId = productoId,
            Codigo = codigo,
            Cantidad = cantidad,
            Estado = estado,
            FechaFabricacion = DateTime.UtcNow.Date.AddDays(-10),
            FechaVencimiento = DateTime.UtcNow.Date.AddDays(100)
        };
    }

    private static ActualizarLoteRequestDto CrearActualizacion(int cantidad, string estado)
    {
        return new ActualizarLoteRequestDto
        {
            Cantidad = cantidad,
            Estado = estado,
            FechaFabricacion = DateTime.UtcNow.Date.AddDays(-10),
            FechaVencimiento = DateTime.UtcNow.Date.AddDays(100)
        };
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.UnidadesSerie.RemoveRange(contexto.UnidadesSerie);
        contexto.Lotes.RemoveRange(contexto.Lotes);
        DateTime hoy = DateTime.UtcNow.Date;
        contexto.Lotes.AddRange(
            new Lote { Id = 1, ProductoId = 1, Codigo = "LOT-A", FechaFabricacion = hoy.AddDays(-60), FechaVencimiento = hoy.AddDays(300), Cantidad = 100, Estado = "Disponible" },
            new Lote { Id = 2, ProductoId = 1, Codigo = "LOT-B", FechaFabricacion = hoy.AddDays(-90), FechaVencimiento = hoy.AddDays(-10), Cantidad = 50, Estado = "Disponible" },
            new Lote { Id = 3, ProductoId = 2, Codigo = "LOT-C", FechaFabricacion = hoy.AddDays(-30), FechaVencimiento = hoy.AddDays(20), Cantidad = 80, Estado = "Cuarentena" },
            new Lote { Id = 4, ProductoId = 3, Codigo = "LOT-D", Cantidad = 20, Estado = "Bloqueado" });
        contexto.UnidadesSerie.AddRange(
            new UnidadSerie { Id = 1, ProductoId = 1, LoteId = 1, NumeroSerie = "SN-1", Estado = "Disponible" },
            new UnidadSerie { Id = 2, ProductoId = 1, LoteId = 1, NumeroSerie = "SN-2", Estado = "Asignada" });
        contexto.SaveChanges();
    }

    private async Task<int> ContarLotesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Lotes.CountAsync();
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
