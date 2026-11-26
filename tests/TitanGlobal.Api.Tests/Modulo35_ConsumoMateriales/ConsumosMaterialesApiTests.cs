using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

namespace TitanGlobal.Api.Tests.Modulo35_ConsumoMateriales;

public sealed class ConsumosMaterialesApiTests : IClassFixture<ConsumosMaterialesApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/consumos-materiales";
    private readonly HttpClient _cliente;

    public ConsumosMaterialesApiTests(ConsumosMaterialesApiFactory fabrica)
    {
        fabrica.PrepararDatos();
        _cliente = fabrica.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, fabrica.Services);
    }

    [Fact]
    public async Task ListadoOrdenaPorFechaDeRegistroDescendenteYPagina()
    {
        PaginaConsumosMaterialesDto primera = await ListarAsync("?tamanoPagina=3");
        PaginaConsumosMaterialesDto segunda = await ListarAsync("?pagina=2&tamanoPagina=3");
        PaginaConsumosMaterialesDto vacia = await ListarAsync("?pagina=2147483647&tamanoPagina=100");

        Assert.Equal(5, primera.Total);
        Assert.Equal(new[] { 1, 2, 5 }, primera.Items.Select(consumo => consumo.Id));
        Assert.Equal(new[] { 3, 4 }, segunda.Items.Select(consumo => consumo.Id));
        Assert.Equal(5, vacia.Total);
        Assert.Empty(vacia.Items);
    }

    [Fact]
    public async Task ListadoCombinaBusquedaOrdenComponenteYEstado()
    {
        PaginaConsumosMaterialesDto porNumero = await ListarAsync("?busqueda=%20cm-000004%20");
        PaginaConsumosMaterialesDto combinada = await ListarAsync("?ordenProduccionId=1&componenteProductoId=4&estado=REGISTRADO");
        PaginaConsumosMaterialesDto anulados = await ListarAsync("?estado=anulado");

        ConsumoMaterialListadoDto consumo = Assert.Single(porNumero.Items);
        Assert.Equal("OP-000002", consumo.OrdenProduccionNumero);
        Assert.Equal("Placa", consumo.ComponenteProductoNombre);
        Assert.Equal(2m, consumo.Cantidad);
        Assert.Equal("kg", consumo.UnidadMedida);
        Assert.Equal(new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc), consumo.FechaRegistro);
        Assert.Equal(new[] { 5, 3 }, combinada.Items.Select(item => item.Id));
        Assert.Equal(2, Assert.Single(anulados.Items).Id);
    }

    [Fact]
    public async Task FiltroDeFechasUsaElDiaEnHoraDePeru()
    {
        // CM-000004 se registró el 2026-11-11 00:00 UTC, que es el 2026-11-10 a las 19:00 en Perú.
        PaginaConsumosMaterialesDto diaPeru = await ListarAsync("?desde=2026-11-10&hasta=2026-11-10");
        PaginaConsumosMaterialesDto diaUtc = await ListarAsync("?desde=2026-11-11&hasta=2026-11-11");
        PaginaConsumosMaterialesDto hasta = await ListarAsync("?hasta=2026-11-10");

        Assert.Equal(4, Assert.Single(diaPeru.Items).Id);
        Assert.Empty(diaUtc.Items);
        Assert.Equal(4, Assert.Single(hasta.Items).Id);
    }

    [Theory]
    [InlineData("?pagina=0&tamanoPagina=0&busqueda=CM-0000000000000000001", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101&busqueda=CM-0000000000000000001", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?busqueda=CM-0000000000000000001&ordenProduccionId=0", "La búsqueda no puede superar 20 caracteres.")]
    [InlineData("?ordenProduccionId=0&componenteProductoId=0", "El identificador de la orden de producción debe ser mayor que 0.")]
    [InlineData("?componenteProductoId=0&estado=Otro", "El identificador del componente debe ser mayor que 0.")]
    [InlineData("?estado=Otro&desde=2026-12-02&hasta=2026-12-01", "El estado del consumo de materiales no es válido.")]
    [InlineData("?desde=2026-12-02&hasta=2026-12-01", "La fecha desde no puede ser posterior a la fecha hasta.")]
    public async Task ListadoInvalidoDevuelveMensajeLiteral(string filtros, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + filtros);
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task ResumenCuentaRegistradosAnuladosOrdenesYUltimos7DiasEnHoraDePeru()
    {
        ResumenConsumosMaterialesDto? resumen =
            await _cliente.GetFromJsonAsync<ResumenConsumosMaterialesDto>($"{Ruta}/resumen");

        Assert.Equal(new ResumenConsumosMaterialesDto(4, 1, 2, 2), resumen);
    }

    [Fact]
    public async Task BalanceCalculaRequeridaConsumidaPendienteYDesviacionSinAnulados()
    {
        BalanceConsumoOrdenDto? balance =
            await _cliente.GetFromJsonAsync<BalanceConsumoOrdenDto>($"{Ruta}/ordenes/1/balance");

        Assert.NotNull(balance);
        Assert.Equal(1, balance.OrdenProduccionId);
        Assert.Equal("OP-000001", balance.OrdenProduccionNumero);
        Assert.Equal("Producto uno", balance.ProductoNombre);
        Assert.Equal(10, balance.CantidadPlanificada);
        Assert.Equal("EnProceso", balance.EstadoOrden);
        Assert.Equal(
            new[]
            {
                new LineaBalanceConsumoDto(4, "Placa", "kg", 5m, 7.5m, 0m, 50m),
                new LineaBalanceConsumoDto(3, "Tornillo", "unidad", 20m, 12m, 8m, -40m)
            },
            balance.Lineas);
    }

    [Fact]
    public async Task BalanceDeOrdenSinConsumosMuestraTodoPendiente()
    {
        BalanceConsumoOrdenDto? balance =
            await _cliente.GetFromJsonAsync<BalanceConsumoOrdenDto>($"{Ruta}/ordenes/3/balance");

        Assert.NotNull(balance);
        Assert.Equal(
            new[]
            {
                new LineaBalanceConsumoDto(4, "Placa", "kg", 3m, 0m, 3m, -100m),
                new LineaBalanceConsumoDto(3, "Tornillo", "unidad", 12m, 0m, 12m, -100m)
            },
            balance.Lineas);
    }

    [Fact]
    public async Task BalanceDeOrdenInexistenteDevuelve404()
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/ordenes/99/balance");
        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "La orden de producción 99 no existe.");
    }

    [Fact]
    public async Task DetalleIncluyeProductoFabricadoYDatosDeAnulacion()
    {
        ConsumoMaterialDetalleDto registrado = await ObtenerAsync(1);
        ConsumoMaterialDetalleDto anulado = await ObtenerAsync(2);

        Assert.Equal("Producto uno", registrado.ProductoFabricadoNombre);
        Assert.Equal("Ana Torres", registrado.Responsable);
        Assert.Null(registrado.FechaAnulacion);
        Assert.Equal("Anulado", anulado.Estado);
        Assert.Equal("Error de digitación", anulado.MotivoAnulacion);
        Assert.Equal(anulado.FechaRegistro.AddHours(1), anulado.FechaAnulacion);
    }

    [Fact]
    public async Task DetalleInexistenteDevuelve404()
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/99");
        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "El consumo de materiales 99 no existe.");
    }

    private async Task<PaginaConsumosMaterialesDto> ListarAsync(string filtros)
    {
        PaginaConsumosMaterialesDto? pagina = await _cliente.GetFromJsonAsync<PaginaConsumosMaterialesDto>(Ruta + filtros);
        Assert.NotNull(pagina);
        return pagina;
    }

    private async Task<ConsumoMaterialDetalleDto> ObtenerAsync(int id)
    {
        ConsumoMaterialDetalleDto? detalle = await _cliente.GetFromJsonAsync<ConsumoMaterialDetalleDto>($"{Ruta}/{id}");
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
