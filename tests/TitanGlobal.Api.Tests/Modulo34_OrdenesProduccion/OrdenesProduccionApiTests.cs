using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

namespace TitanGlobal.Api.Tests.Modulo34_OrdenesProduccion;

public sealed class OrdenesProduccionApiTests : IClassFixture<OrdenesProduccionApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/ordenes-produccion";
    private static readonly DateOnly Inicio = new DateOnly(2027, 1, 4);
    private static readonly DateOnly Fin = new DateOnly(2027, 1, 8);
    private readonly OrdenesProduccionApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public OrdenesProduccionApiTests(OrdenesProduccionApiFactory fabrica)
    {
        _fabrica = fabrica;
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

    [Fact]
    public async Task AltaAsignaNumeroListaYRutaActivasYConservaLasVersiones()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            Ruta, new CrearOrdenProduccionRequestDto(1, 6, Inicio, Inicio));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        OrdenProduccionCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<OrdenProduccionCreadaDto>();
        Assert.NotNull(creada);
        Assert.Equal($"OP-{creada.Id:D6}", creada.Numero);
        Assert.EndsWith($"{Ruta}/{creada.Id}", respuesta.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        _fabrica.RetirarListaYRutaDelProductoUno();
        OrdenProduccionDetalleDto detalle = await ObtenerAsync(creada.Id);
        Assert.Equal("Planificada", detalle.Estado);
        Assert.Equal(6, detalle.CantidadPlanificada);
        Assert.Equal(Inicio, detalle.FechaInicioPlanificada);
        Assert.Equal(Inicio, detalle.FechaFinPlanificada);
        Assert.Equal(10, detalle.ListaMaterialesId);
        Assert.Equal(20, detalle.RutaProduccionId);
        Assert.Equal(new[] { 12m, 3m }, detalle.Materiales.Select(material => material.CantidadRequerida));
        Assert.Equal(25.5m, detalle.TiempoEstimadoMinutos);

        using HttpResponseMessage sinLista = await _cliente.PostAsJsonAsync(
            Ruta, new CrearOrdenProduccionRequestDto(1, 6, Inicio, Fin));
        await VerificarErrorAsync(sinLista, HttpStatusCode.Conflict, "El producto 1 no tiene una lista de materiales activa.");
    }

    [Theory]
    [InlineData(0, 0, null, "2027-01-01", "El identificador del producto debe ser mayor que 0.")]
    [InlineData(99, 0, null, "2027-01-01", "La cantidad planificada debe estar entre 1 y 1000000.")]
    [InlineData(1, 1000001, null, null, "La cantidad planificada debe estar entre 1 y 1000000.")]
    [InlineData(99, 1000000, null, "2027-01-01", "Las fechas planificadas son obligatorias.")]
    [InlineData(99, 1, "2027-01-02", "2027-01-01", "La fecha de fin no puede ser anterior a la fecha de inicio.")]
    [InlineData(99, 1, "2027-01-01", "2027-01-01", "El producto 99 no existe.")]
    public async Task AltaValidaCamposAntesQueReferencias(
        int productoId, int cantidad, string? inicio, string? fin, string mensaje)
    {
        CrearOrdenProduccionRequestDto solicitud = new CrearOrdenProduccionRequestDto(
            productoId, cantidad, Fecha(inicio), Fecha(fin));
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [InlineData(2, "El producto 2 no tiene una lista de materiales activa.")]
    [InlineData(5, "El producto 5 no tiene una ruta de producción activa.")]
    public async Task AltaSinListaORutaActivaDevuelve409(int productoId, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            Ruta, new CrearOrdenProduccionRequestDto(productoId, 1, Inicio, Fin));
        await VerificarErrorAsync(respuesta, HttpStatusCode.Conflict, mensaje);
        Assert.Equal(4, (await ListarAsync(string.Empty)).Total);
    }

    [Fact]
    public async Task EdicionCambiaCantidadYFechasDeUnaOrdenPlanificada()
    {
        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync(
            $"{Ruta}/1", new EditarOrdenProduccionRequestDto(1000000, Inicio, Fin));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(1, (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        OrdenProduccionDetalleDto detalle = await ObtenerAsync(1);
        Assert.Equal(1000000, detalle.CantidadPlanificada);
        Assert.Equal(Inicio, detalle.FechaInicioPlanificada);
        Assert.Equal(Fin, detalle.FechaFinPlanificada);
        Assert.Equal("OP-000001", detalle.Numero);
        Assert.Equal(2000000m, detalle.Materiales[0].CantidadRequerida);
    }

    [Theory]
    [InlineData(99, 0, HttpStatusCode.NotFound, "La orden de producción 99 no existe.")]
    [InlineData(2, 0, HttpStatusCode.Conflict, "Solo se pueden editar órdenes de producción planificadas.")]
    [InlineData(4, 0, HttpStatusCode.Conflict, "Solo se pueden editar órdenes de producción planificadas.")]
    [InlineData(1, 0, HttpStatusCode.BadRequest, "La cantidad planificada debe estar entre 1 y 1000000.")]
    public async Task EdicionVerificaExistenciaEstadoYCampos(int id, int cantidad, HttpStatusCode estado, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync(
            $"{Ruta}/{id}", new EditarOrdenProduccionRequestDto(cantidad, null, null));
        await VerificarErrorAsync(respuesta, estado, mensaje);
    }

    [Fact]
    public async Task EdicionRechazaFechasAusentesOInvertidasSinCambiarLaOrden()
    {
        using HttpResponseMessage sinFechas = await _cliente.PutAsJsonAsync(
            $"{Ruta}/1", new EditarOrdenProduccionRequestDto(5, Inicio, null));
        using HttpResponseMessage invertidas = await _cliente.PutAsJsonAsync(
            $"{Ruta}/1", new EditarOrdenProduccionRequestDto(5, Fin, Inicio));

        await VerificarErrorAsync(sinFechas, HttpStatusCode.BadRequest, "Las fechas planificadas son obligatorias.");
        await VerificarErrorAsync(invertidas, HttpStatusCode.BadRequest, "La fecha de fin no puede ser anterior a la fecha de inicio.");
        Assert.Equal(10, (await ObtenerAsync(1)).CantidadPlanificada);
    }

    [Fact]
    public async Task CicloCompletoRegistraFechasRealesYCantidadProducida()
    {
        DateTime antes = DateTime.UtcNow;
        await VerificarTransicionAsync(1, "liberar");
        Assert.Equal("Liberada", (await ObtenerAsync(1)).Estado);
        await VerificarTransicionAsync(1, "iniciar");
        OrdenProduccionDetalleDto enProceso = await ObtenerAsync(1);
        Assert.Equal("EnProceso", enProceso.Estado);
        Assert.NotNull(enProceso.FechaInicioReal);
        Assert.InRange(enProceso.FechaInicioReal.Value, antes, DateTime.UtcNow);

        using HttpResponseMessage terminada = await _cliente.PostAsJsonAsync(
            $"{Ruta}/1/terminar", new TerminarOrdenProduccionRequestDto(0));
        Assert.Equal(HttpStatusCode.OK, terminada.StatusCode);
        Assert.Equal(1, (await terminada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        OrdenProduccionDetalleDto final = await ObtenerAsync(1);
        Assert.Equal("Terminada", final.Estado);
        Assert.Equal(0, final.CantidadProducida);
        Assert.NotNull(final.FechaFinReal);
        Assert.True(final.FechaFinReal.Value >= final.FechaInicioReal!.Value);
    }

    [Theory]
    [InlineData(1, "iniciar", "Solo se pueden iniciar órdenes de producción liberadas.")]
    [InlineData(2, "liberar", "Solo se pueden liberar órdenes de producción planificadas.")]
    [InlineData(2, "iniciar", "Solo se pueden iniciar órdenes de producción liberadas.")]
    [InlineData(3, "liberar", "Solo se pueden liberar órdenes de producción planificadas.")]
    [InlineData(4, "iniciar", "Solo se pueden iniciar órdenes de producción liberadas.")]
    public async Task TransicionFueraDeEstadoDevuelve409(int id, string accion, string mensaje)
    {
        await VerificarTransicionAsync(id, accion, HttpStatusCode.Conflict, mensaje);
    }

    [Theory]
    [InlineData("liberar")]
    [InlineData("iniciar")]
    public async Task TransicionDeOrdenInexistenteDevuelve404(string accion)
    {
        await VerificarTransicionAsync(99, accion, HttpStatusCode.NotFound, "La orden de producción 99 no existe.");
    }

    [Theory]
    [InlineData(99, null, HttpStatusCode.NotFound, "La orden de producción 99 no existe.")]
    [InlineData(1, null, HttpStatusCode.Conflict, "Solo se pueden terminar órdenes de producción en proceso.")]
    [InlineData(3, 5, HttpStatusCode.Conflict, "Solo se pueden terminar órdenes de producción en proceso.")]
    [InlineData(2, null, HttpStatusCode.BadRequest, "La cantidad producida debe estar entre 0 y 1000000.")]
    [InlineData(2, -1, HttpStatusCode.BadRequest, "La cantidad producida debe estar entre 0 y 1000000.")]
    [InlineData(2, 1000001, HttpStatusCode.BadRequest, "La cantidad producida debe estar entre 0 y 1000000.")]
    public async Task TerminarVerificaExistenciaEstadoYCantidad(
        int id, int? cantidad, HttpStatusCode estado, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{id}/terminar", new TerminarOrdenProduccionRequestDto(cantidad));
        await VerificarErrorAsync(respuesta, estado, mensaje);
        if (id == 2)
        {
            Assert.Equal("EnProceso", (await ObtenerAsync(2)).Estado);
        }
    }

    [Fact]
    public async Task TerminarAceptaElMaximoDeUnidades()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/2/terminar", new TerminarOrdenProduccionRequestDto(1000000));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(1000000, (await ObtenerAsync(2)).CantidadProducida);

        ResumenOrdenesProduccionDto? resumen = await _cliente.GetFromJsonAsync<ResumenOrdenesProduccionDto>($"{Ruta}/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(1000007, resumen.UnidadesProducidas);
        Assert.Equal(10, resumen.UnidadesPlanificadasAbiertas);
    }

    [Fact]
    public async Task CancelarPlanificadaOLiberadaGuardaElMotivoRecortado()
    {
        await VerificarTransicionAsync(1, "liberar");
        using HttpResponseMessage liberada = await _cliente.PostAsJsonAsync(
            $"{Ruta}/1/cancelar", new CancelarOrdenProduccionRequestDto("  Falta de insumos  "));
        Assert.Equal(HttpStatusCode.OK, liberada.StatusCode);
        OrdenProduccionDetalleDto cancelada = await ObtenerAsync(1);
        Assert.Equal("Cancelada", cancelada.Estado);
        Assert.Equal("Falta de insumos", cancelada.MotivoCancelacion);

        int nueva = await CrearOrdenAsync();
        using HttpResponseMessage planificada = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{nueva}/cancelar", new CancelarOrdenProduccionRequestDto(new string('m', 250)));
        Assert.Equal(HttpStatusCode.OK, planificada.StatusCode);
        Assert.Equal(250, (await ObtenerAsync(nueva)).MotivoCancelacion?.Length);
        await VerificarTransicionAsync(nueva, "liberar", HttpStatusCode.Conflict,
            "Solo se pueden liberar órdenes de producción planificadas.");
    }

    [Theory]
    [InlineData(99, "", HttpStatusCode.NotFound, "La orden de producción 99 no existe.")]
    [InlineData(2, "", HttpStatusCode.Conflict, "Solo se pueden cancelar órdenes de producción planificadas o liberadas.")]
    [InlineData(3, "Motivo", HttpStatusCode.Conflict, "Solo se pueden cancelar órdenes de producción planificadas o liberadas.")]
    [InlineData(4, "Motivo", HttpStatusCode.Conflict, "Solo se pueden cancelar órdenes de producción planificadas o liberadas.")]
    [InlineData(1, null, HttpStatusCode.BadRequest, "El motivo de cancelación es obligatorio.")]
    [InlineData(1, "   ", HttpStatusCode.BadRequest, "El motivo de cancelación es obligatorio.")]
    public async Task CancelarVerificaExistenciaEstadoYMotivo(
        int id, string? motivo, HttpStatusCode estado, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{id}/cancelar", new CancelarOrdenProduccionRequestDto(motivo));
        await VerificarErrorAsync(respuesta, estado, mensaje);
    }

    [Fact]
    public async Task CancelarRechazaMotivoDeMasDe250Caracteres()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/1/cancelar", new CancelarOrdenProduccionRequestDto(new string('m', 251)));
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, "El motivo de cancelación no puede superar 250 caracteres.");
        Assert.Equal("Planificada", (await ObtenerAsync(1)).Estado);
    }

    private async Task<int> CrearOrdenAsync()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            Ruta, new CrearOrdenProduccionRequestDto(1, 2, Inicio, Fin));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        OrdenProduccionCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<OrdenProduccionCreadaDto>();
        Assert.NotNull(creada);
        return creada.Id;
    }

    private async Task VerificarTransicionAsync(
        int id, string accion, HttpStatusCode estado = HttpStatusCode.OK, string? mensaje = null)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsync($"{Ruta}/{id}/{accion}", null);
        if (mensaje == null)
        {
            Assert.Equal(estado, respuesta.StatusCode);
            return;
        }

        await VerificarErrorAsync(respuesta, estado, mensaje);
    }

    private static DateOnly? Fecha(string? valor)
    {
        if (valor == null)
        {
            return null;
        }

        return DateOnly.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
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
