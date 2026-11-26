using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

namespace TitanGlobal.Api.Tests.Modulo34_OrdenesProduccion;

public sealed class OrdenesProduccionApiTests : IClassFixture<OrdenesProduccionApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/ordenes-produccion";
    private readonly HttpClient _cliente;

    public OrdenesProduccionApiTests(OrdenesProduccionApiFactory fabrica)
    {
        fabrica.PrepararDatos();
        _cliente = fabrica.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, fabrica.Services);
    }

    [Fact]
    public async Task ListadoOrdenaPorFechaDeInicioDescendenteYPagina()
    {
        PaginaOrdenesProduccionDto primera = await ListarAsync("?tamanoPagina=2");
        PaginaOrdenesProduccionDto segunda = await ListarAsync("?pagina=2&tamanoPagina=2");
        PaginaOrdenesProduccionDto vacia = await ListarAsync("?pagina=2147483647&tamanoPagina=100");

        Assert.Equal(4, primera.Total);
        Assert.Equal(new[] { 4, 2 }, primera.Items.Select(orden => orden.Id));
        Assert.Equal(new[] { 1, 3 }, segunda.Items.Select(orden => orden.Id));
        Assert.Equal(2, segunda.Pagina);
        Assert.Equal(2, segunda.TamanoPagina);
        Assert.Equal(4, vacia.Total);
        Assert.Empty(vacia.Items);
    }

    [Fact]
    public async Task ListadoCombinaBusquedaProductoEstadoYFechas()
    {
        PaginaOrdenesProduccionDto porNumero = await ListarAsync("?busqueda=%20op-000002%20");
        PaginaOrdenesProduccionDto porEstado = await ListarAsync("?productoId=1&estado=enPROCESO");
        PaginaOrdenesProduccionDto porFechas = await ListarAsync("?desde=2026-12-01&hasta=2026-12-05");
        PaginaOrdenesProduccionDto porProducto = await ListarAsync("?productoId=2");
        PaginaOrdenesProduccionDto vacia = await ListarAsync("?busqueda=&productoId=1&estado=Cancelada");

        Assert.Equal(
            new OrdenProduccionListadoDto(2, "OP-000002", 1, "Producto uno", 4,
                new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 6), "EnProceso"),
            Assert.Single(porNumero.Items));
        Assert.Equal(2, Assert.Single(porEstado.Items).Id);
        Assert.Equal(new[] { 2, 1 }, porFechas.Items.Select(orden => orden.Id));
        Assert.Equal("Producto dos", Assert.Single(porProducto.Items).ProductoNombre);
        Assert.Equal(0, vacia.Total);
    }

    [Theory]
    [InlineData("?pagina=0&tamanoPagina=0&productoId=0&estado=Otra", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=0&productoId=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?productoId=0&estado=Otra", "El identificador del producto debe ser mayor que 0.")]
    [InlineData("?estado=Otra&desde=2026-12-02&hasta=2026-12-01", "El estado de la orden de producción no es válido.")]
    [InlineData("?desde=2026-12-02&hasta=2026-12-01", "La fecha desde no puede ser posterior a la fecha hasta.")]
    [InlineData("?busqueda=OP-0000000000000000001", "La búsqueda no puede superar 20 caracteres.")]
    public async Task ListadoInvalidoDevuelveMensajeLiteral(string filtros, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + filtros);
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task ResumenAgregaPorEstadoYUnidades()
    {
        ResumenOrdenesProduccionDto? resumen = await _cliente.GetFromJsonAsync<ResumenOrdenesProduccionDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.TotalOrdenes);
        Assert.Equal(
            new[]
            {
                new EstadoOrdenesProduccionDto("Planificada", 1),
                new EstadoOrdenesProduccionDto("Liberada", 0),
                new EstadoOrdenesProduccionDto("EnProceso", 1),
                new EstadoOrdenesProduccionDto("Terminada", 1),
                new EstadoOrdenesProduccionDto("Cancelada", 1)
            },
            resumen.PorEstado);
        Assert.Equal(14, resumen.UnidadesPlanificadasAbiertas);
        Assert.Equal(7, resumen.UnidadesProducidas);
    }

    [Fact]
    public async Task DetalleCalculaMaterialesYTiempoEstimado()
    {
        OrdenProduccionDetalleDto detalle = await ObtenerAsync(1);

        Assert.Equal("OP-000001", detalle.Numero);
        Assert.Equal("Planificada", detalle.Estado);
        Assert.Equal(10, detalle.ListaMaterialesId);
        Assert.Equal(1, detalle.ListaMaterialesVersion);
        Assert.Equal(20, detalle.RutaProduccionId);
        Assert.Equal(1, detalle.RutaProduccionVersion);
        Assert.Null(detalle.CantidadProducida);
        Assert.Null(detalle.FechaInicioReal);
        Assert.Equal(
            new[]
            {
                new MaterialRequeridoDto(3, "Tornillo", 20m, "unidad"),
                new MaterialRequeridoDto(4, "Placa", 5m, "kg")
            },
            detalle.Materiales);
        Assert.Equal(
            new[]
            {
                new OperacionOrdenDto(1, "Cortar", "Corte", 25m),
                new OperacionOrdenDto(2, "Ensamblar", "Ensamblaje", 7.5m)
            },
            detalle.Operaciones);
        Assert.Equal(32.5m, detalle.TiempoEstimadoMinutos);
    }

    [Fact]
    public async Task DetalleMuestraDatosRealesYMotivo()
    {
        OrdenProduccionDetalleDto terminada = await ObtenerAsync(3);
        OrdenProduccionDetalleDto cancelada = await ObtenerAsync(4);

        Assert.Equal(7, terminada.CantidadProducida);
        Assert.Equal(new DateTime(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc), terminada.FechaInicioReal);
        Assert.Equal(new DateTime(2026, 11, 22, 18, 0, 0, DateTimeKind.Utc), terminada.FechaFinReal);
        Assert.Equal("Pedido anulado", cancelada.MotivoCancelacion);
        Assert.Empty(cancelada.Materiales);
        Assert.Equal(0m, cancelada.TiempoEstimadoMinutos);
    }

    [Fact]
    public async Task DetalleInexistenteDevuelve404()
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/99");
        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "La orden de producción 99 no existe.");
    }

    private async Task<PaginaOrdenesProduccionDto> ListarAsync(string filtros)
    {
        PaginaOrdenesProduccionDto? pagina = await _cliente.GetFromJsonAsync<PaginaOrdenesProduccionDto>(Ruta + filtros);
        Assert.NotNull(pagina);
        return pagina;
    }

    private async Task<OrdenProduccionDetalleDto> ObtenerAsync(int id)
    {
        OrdenProduccionDetalleDto? detalle = await _cliente.GetFromJsonAsync<OrdenProduccionDetalleDto>($"{Ruta}/{id}");
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
