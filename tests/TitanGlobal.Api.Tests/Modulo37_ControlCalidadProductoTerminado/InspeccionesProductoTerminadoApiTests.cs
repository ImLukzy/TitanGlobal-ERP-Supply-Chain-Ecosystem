using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

namespace TitanGlobal.Api.Tests.Modulo37_ControlCalidadProductoTerminado;

public sealed class InspeccionesProductoTerminadoApiTests
    : IClassFixture<InspeccionesProductoTerminadoApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/inspecciones-producto-terminado";
    private readonly HttpClient _cliente;

    public InspeccionesProductoTerminadoApiTests(InspeccionesProductoTerminadoApiFactory fabrica)
    {
        fabrica.PrepararDatos();
        _cliente = fabrica.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, fabrica.Services);
    }

    [Fact]
    public async Task ListadoOrdenaPorFechaDescendenteYCalculaElPorcentaje()
    {
        PaginaInspeccionesProductoTerminadoDto primera = await ListarAsync("?tamanoPagina=3");
        PaginaInspeccionesProductoTerminadoDto segunda = await ListarAsync("?pagina=2&tamanoPagina=3");
        PaginaInspeccionesProductoTerminadoDto vacia = await ListarAsync("?pagina=2147483647&tamanoPagina=100");

        Assert.Equal(5, primera.Total);
        Assert.Equal(new[] { 3, 4, 2 }, primera.Items.Select(inspeccion => inspeccion.Id));
        Assert.Equal(new[] { 1, 5 }, segunda.Items.Select(inspeccion => inspeccion.Id));
        Assert.Equal(new decimal?[] { null, 0m, 10m }, primera.Items.Select(inspeccion => inspeccion.PorcentajeNoConforme));
        Assert.Equal(
            new InspeccionProductoTerminadoListadoDto(1, "CCP-000001", 1, "OP-000001", 1, "Producto uno",
                new DateOnly(2026, 11, 21), 40, "Aprobada", 2.5m),
            segunda.Items[0]);
        Assert.Empty(vacia.Items);
    }

    [Theory]
    [InlineData("?busqueda=%20op-000002%20", new[] { 4 })]
    [InlineData("?busqueda=ccp-000001", new[] { 1 })]
    [InlineData("?ordenProduccionId=1", new[] { 3, 2, 1 })]
    [InlineData("?productoId=2", new[] { 4, 5 })]
    [InlineData("?estado=aPROBADA", new[] { 4, 1 })]
    [InlineData("?desde=2026-11-22&hasta=2026-11-22", new[] { 4, 2 })]
    [InlineData("?productoId=1&estado=Anulada", new int[0])]
    public async Task ListadoAplicaCadaFiltro(string filtros, int[] esperados)
    {
        PaginaInspeccionesProductoTerminadoDto pagina = await ListarAsync(filtros);
        Assert.Equal(esperados, pagina.Items.Select(inspeccion => inspeccion.Id));
    }

    [Theory]
    [InlineData("?pagina=0&tamanoPagina=0&busqueda=CCP-00000000000000001", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101&busqueda=CCP-00000000000000001", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?busqueda=CCP-00000000000000001&ordenProduccionId=0", "La búsqueda no puede superar 20 caracteres.")]
    [InlineData("?ordenProduccionId=0&productoId=0", "El identificador de la orden de producción debe ser mayor que 0.")]
    [InlineData("?productoId=0&estado=Otra", "El identificador del producto debe ser mayor que 0.")]
    [InlineData("?estado=Otra&desde=2026-12-02&hasta=2026-12-01", "El estado de la inspección de producto terminado no es válido.")]
    [InlineData("?desde=2026-12-02&hasta=2026-12-01", "La fecha desde no puede ser posterior a la fecha hasta.")]
    public async Task ListadoInvalidoDevuelveMensajeLiteral(string filtros, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + filtros);
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task ResumenAgregaEstadosTasaUnidadesYOrdenesSinInspeccion()
    {
        ResumenInspeccionesProductoTerminadoDto? resumen =
            await _cliente.GetFromJsonAsync<ResumenInspeccionesProductoTerminadoDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.TotalInspecciones);
        Assert.Equal(
            new[]
            {
                new EstadoInspeccionesProductoTerminadoDto("Pendiente", 1),
                new EstadoInspeccionesProductoTerminadoDto("Aprobada", 2),
                new EstadoInspeccionesProductoTerminadoDto("Rechazada", 1),
                new EstadoInspeccionesProductoTerminadoDto("Anulada", 1)
            },
            resumen.PorEstado);
        Assert.Equal(33.33m, resumen.TasaRechazoPorcentaje);
        Assert.Equal(95, resumen.UnidadesInspeccionadas);
        Assert.Equal(4, resumen.UnidadesNoConformes);
        Assert.Equal(3, resumen.OrdenesTerminadasSinInspeccion);
    }

    [Fact]
    public async Task AvanceSumaLasInspeccionesNoAnuladasDeLaOrden()
    {
        AvanceInspeccionOrdenDto? avance =
            await _cliente.GetFromJsonAsync<AvanceInspeccionOrdenDto>($"{Ruta}/ordenes/1/avance");
        AvanceInspeccionOrdenDto? soloAnulada =
            await _cliente.GetFromJsonAsync<AvanceInspeccionOrdenDto>($"{Ruta}/ordenes/5/avance");

        Assert.Equal(new AvanceInspeccionOrdenDto(1, "OP-000001", "Producto uno", "Terminada", 100, 90, 10, 39, 4), avance);
        Assert.Equal(new AvanceInspeccionOrdenDto(5, "OP-000005", "Producto dos", "Terminada", 5, 0, 5, 0, 0), soloAnulada);
    }

    [Fact]
    public async Task AvanceDeOrdenNoTerminadaDevuelveCerosYCantidadNula()
    {
        AvanceInspeccionOrdenDto? avance =
            await _cliente.GetFromJsonAsync<AvanceInspeccionOrdenDto>($"{Ruta}/ordenes/3/avance");
        Assert.Equal(new AvanceInspeccionOrdenDto(3, "OP-000003", "Producto uno", "EnProceso", null, 0, 0, 0, 0), avance);

        using HttpResponseMessage inexistente = await _cliente.GetAsync($"{Ruta}/ordenes/99/avance");
        await VerificarErrorAsync(inexistente, HttpStatusCode.NotFound, "La orden de producción 99 no existe.");
    }

    [Fact]
    public async Task DetalleCalculaConformesYToleranciaSoloConDictamen()
    {
        InspeccionProductoTerminadoDetalleDto rechazada = await ObtenerAsync(2);
        InspeccionProductoTerminadoDetalleDto pendiente = await ObtenerAsync(3);
        InspeccionProductoTerminadoDetalleDto anulada = await ObtenerAsync(5);

        Assert.Equal(10m, rechazada.PorcentajeNoConforme);
        Assert.Equal(3, rechazada.CantidadNoConforme);
        Assert.Equal(27, rechazada.CantidadConforme);
        Assert.True(rechazada.SuperaTolerancia);
        Assert.Equal(5m, rechazada.ToleranciaNoConformesPorcentaje);
        Assert.Equal("María Quispe", rechazada.Inspector);
        Assert.Equal("Rayas en la carcasa", rechazada.Observaciones);
        Assert.Equal(new DateTime(2026, 11, 22, 15, 0, 0, DateTimeKind.Utc), rechazada.FechaDictamen);
        Assert.Null(pendiente.PorcentajeNoConforme);
        Assert.Null(pendiente.CantidadConforme);
        Assert.Null(pendiente.SuperaTolerancia);
        Assert.Null(pendiente.FechaDictamen);
        Assert.Equal("Orden equivocada", anulada.MotivoAnulacion);
        Assert.False((await ObtenerAsync(1)).SuperaTolerancia);
    }

    [Fact]
    public async Task DetalleInexistenteDevuelve404()
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/99");
        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "La inspección de producto terminado 99 no existe.");
    }

    private async Task<PaginaInspeccionesProductoTerminadoDto> ListarAsync(string filtros)
    {
        PaginaInspeccionesProductoTerminadoDto? pagina =
            await _cliente.GetFromJsonAsync<PaginaInspeccionesProductoTerminadoDto>(Ruta + filtros);
        Assert.NotNull(pagina);
        return pagina;
    }

    private async Task<InspeccionProductoTerminadoDetalleDto> ObtenerAsync(int id)
    {
        InspeccionProductoTerminadoDetalleDto? detalle =
            await _cliente.GetFromJsonAsync<InspeccionProductoTerminadoDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(detalle);
        return detalle;
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, cuerpo.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, cuerpo.GetProperty("Message").GetString());
    }

    public void Dispose()
    {
        _cliente.Dispose();
    }
}
