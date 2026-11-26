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

    [Fact]
    public async Task AltaUsaLasUnidadesLiberadasPorLaAnuladaYAsignaNumero()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearInspeccionProductoTerminadoRequestDto(5, new DateOnly(2026, 11, 25), 5, 2.5m, "  Ana Torres  "));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        InspeccionProductoTerminadoCreadaDto? creada =
            await respuesta.Content.ReadFromJsonAsync<InspeccionProductoTerminadoCreadaDto>();
        Assert.NotNull(creada);
        Assert.Equal($"CCP-{creada.Id:D6}", creada.Numero);
        Assert.EndsWith($"{Ruta}/{creada.Id}", respuesta.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
        InspeccionProductoTerminadoDetalleDto detalle = await ObtenerAsync(creada.Id);
        Assert.Equal("Pendiente", detalle.Estado);
        Assert.Equal("Ana Torres", detalle.Inspector);
        Assert.Equal(2.5m, detalle.ToleranciaNoConformesPorcentaje);
        Assert.Equal(0, (await _cliente.GetFromJsonAsync<AvanceInspeccionOrdenDto>($"{Ruta}/ordenes/5/avance"))?.UnidadesPorInspeccionar);

        using HttpResponseMessage agotada = await _cliente.PostAsJsonAsync(Ruta,
            new CrearInspeccionProductoTerminadoRequestDto(5, new DateOnly(2026, 11, 25), 1, 0m, "Ana Torres"));
        await VerificarErrorAsync(agotada, HttpStatusCode.Conflict, "La orden OP-000005 solo tiene 0 unidades por inspeccionar.");
    }

    [Theory]
    [InlineData(0, null, 0, null, null, "El identificador de la orden de producción debe ser mayor que 0.")]
    [InlineData(99, null, 0, null, null, "La fecha de inspección es obligatoria.")]
    [InlineData(99, "manana", 0, null, null, "La fecha de inspección no puede ser posterior a hoy.")]
    [InlineData(99, "2026-11-25", 0, null, null, "La cantidad inspeccionada debe estar entre 1 y 1000000.")]
    [InlineData(99, "2026-11-25", 1000001, null, null, "La cantidad inspeccionada debe estar entre 1 y 1000000.")]
    [InlineData(99, "2026-11-25", 1, null, null, "La tolerancia de no conformes debe estar entre 0 y 100.")]
    [InlineData(99, "2026-11-25", 1, "100.01", null, "La tolerancia de no conformes debe estar entre 0 y 100.")]
    [InlineData(99, "2026-11-25", 1, "2.555", null, "La tolerancia de no conformes admite como máximo 2 decimales.")]
    [InlineData(99, "2026-11-25", 1, "100", "   ", "El inspector es obligatorio.")]
    [InlineData(99, "2026-11-25", 1, "0", "LARGO", "El inspector no puede superar 100 caracteres.")]
    [InlineData(99, "2026-11-25", 1000000, "0", "Ana", "La orden de producción 99 no existe.")]
    public async Task AltaValidaCamposAntesQueLaOrden(
        int ordenId, string? fecha, int cantidad, string? tolerancia, string? inspector, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearInspeccionProductoTerminadoRequestDto(
                ordenId, Fecha(fecha), cantidad, Decimal(tolerancia), inspector == "LARGO" ? new string('i', 101) : inspector));

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
        Assert.Equal(5, (await ListarAsync(string.Empty)).Total);
    }

    [Theory]
    [InlineData(3, "2026-11-25", 1, HttpStatusCode.Conflict, "Solo se pueden inspeccionar órdenes de producción terminadas.")]
    [InlineData(6, "2026-11-24", 1, HttpStatusCode.BadRequest, "La fecha de inspección no puede ser anterior al fin de la orden OP-000006.")]
    [InlineData(6, "2026-11-25", 1, HttpStatusCode.Conflict, "La orden OP-000006 solo tiene 0 unidades por inspeccionar.")]
    [InlineData(1, "2026-11-25", 11, HttpStatusCode.Conflict, "La orden OP-000001 solo tiene 10 unidades por inspeccionar.")]
    public async Task AltaExigeOrdenTerminadaFechaYUnidadesDisponibles(
        int ordenId, string fecha, int cantidad, HttpStatusCode estado, string mensaje)
    {
        // OP-000006 terminó el 2026-11-26 00:30 UTC, que en Perú es el 2026-11-25.
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearInspeccionProductoTerminadoRequestDto(ordenId, Fecha(fecha), cantidad, 5m, "Ana Torres"));

        await VerificarErrorAsync(respuesta, estado, mensaje);
        Assert.Equal(5, (await ListarAsync(string.Empty)).Total);
    }

    [Fact]
    public async Task EdicionExcluyeLasUnidadesDeLaPropiaInspeccion()
    {
        EditarInspeccionProductoTerminadoRequestDto cambio =
            new EditarInspeccionProductoTerminadoRequestDto(new DateOnly(2026, 11, 24), 30, 1.25m, " Luis Rojas ");
        using HttpResponseMessage editada = await _cliente.PutAsJsonAsync($"{Ruta}/3", cambio);

        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(3, (await editada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        InspeccionProductoTerminadoDetalleDto detalle = await ObtenerAsync(3);
        Assert.Equal(30, detalle.CantidadInspeccionada);
        Assert.Equal(new DateOnly(2026, 11, 24), detalle.FechaInspeccion);
        Assert.Equal(1.25m, detalle.ToleranciaNoConformesPorcentaje);
        Assert.Equal("Luis Rojas", detalle.Inspector);

        using HttpResponseMessage excedida = await _cliente.PutAsJsonAsync($"{Ruta}/3", cambio with { CantidadInspeccionada = 31 });
        await VerificarErrorAsync(excedida, HttpStatusCode.Conflict, "La orden OP-000001 solo tiene 30 unidades por inspeccionar.");
        Assert.Equal(30, (await ObtenerAsync(3)).CantidadInspeccionada);
    }

    [Theory]
    [InlineData(99, HttpStatusCode.NotFound, "La inspección de producto terminado 99 no existe.")]
    [InlineData(2, HttpStatusCode.Conflict, "Solo se pueden editar inspecciones pendientes.")]
    [InlineData(5, HttpStatusCode.Conflict, "Solo se pueden editar inspecciones pendientes.")]
    [InlineData(3, HttpStatusCode.BadRequest, "El inspector es obligatorio.")]
    public async Task EdicionVerificaExistenciaEstadoYCampos(int id, HttpStatusCode estado, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new EditarInspeccionProductoTerminadoRequestDto(new DateOnly(2026, 11, 24), 1, 0m, " "));
        await VerificarErrorAsync(respuesta, estado, mensaje);
    }

    [Fact]
    public async Task AprobarSobreLaToleranciaDa409YRechazarFunciona()
    {
        using HttpResponseMessage aprobar = await DictaminarAsync(3, "aprobada", 1, null);
        await VerificarErrorAsync(aprobar, HttpStatusCode.Conflict,
            "La inspección supera la tolerancia de no conformes (5.00 % > 2.50 %) y solo puede rechazarse.");
        Assert.Equal("Pendiente", (await ObtenerAsync(3)).Estado);

        DateTime antes = DateTime.UtcNow;
        using HttpResponseMessage rechazar = await DictaminarAsync(3, " RECHAZADA ", 1, "  Golpe en la tapa  ");
        Assert.Equal(HttpStatusCode.OK, rechazar.StatusCode);
        InspeccionProductoTerminadoDetalleDto detalle = await ObtenerAsync(3);
        Assert.Equal("Rechazada", detalle.Estado);
        Assert.Equal(1, detalle.CantidadNoConforme);
        Assert.Equal(19, detalle.CantidadConforme);
        Assert.Equal(5m, detalle.PorcentajeNoConforme);
        Assert.True(detalle.SuperaTolerancia);
        Assert.Equal("Golpe en la tapa", detalle.Observaciones);
        Assert.NotNull(detalle.FechaDictamen);
        Assert.InRange(detalle.FechaDictamen.Value, antes, DateTime.UtcNow);

        using HttpResponseMessage otra = await DictaminarAsync(3, "Aprobada", 0, null);
        await VerificarErrorAsync(otra, HttpStatusCode.Conflict, "Solo se pueden dictaminar inspecciones pendientes.");
    }

    [Fact]
    public async Task AprobarJustoEnLaToleranciaSePermite()
    {
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearInspeccionProductoTerminadoRequestDto(2, new DateOnly(2026, 11, 25), 20, 5m, "Ana Torres"));
        InspeccionProductoTerminadoCreadaDto? creada =
            await alta.Content.ReadFromJsonAsync<InspeccionProductoTerminadoCreadaDto>();
        Assert.NotNull(creada);

        using HttpResponseMessage aprobar = await DictaminarAsync(creada.Id, "Aprobada", 1, null);
        Assert.Equal(HttpStatusCode.OK, aprobar.StatusCode);
        InspeccionProductoTerminadoDetalleDto detalle = await ObtenerAsync(creada.Id);
        Assert.Equal("Aprobada", detalle.Estado);
        Assert.False(detalle.SuperaTolerancia);
        Assert.Null(detalle.Observaciones);
        Assert.Equal(new AvanceInspeccionOrdenDto(2, "OP-000002", "Producto dos", "Terminada", 50, 45, 5, 44, 1),
            await _cliente.GetFromJsonAsync<AvanceInspeccionOrdenDto>($"{Ruta}/ordenes/2/avance"));
    }

    [Theory]
    [InlineData(99, "Aprobada", 0, HttpStatusCode.NotFound, "La inspección de producto terminado 99 no existe.")]
    [InlineData(1, "Otro", -1, HttpStatusCode.Conflict, "Solo se pueden dictaminar inspecciones pendientes.")]
    [InlineData(5, "Otro", -1, HttpStatusCode.Conflict, "Solo se pueden dictaminar inspecciones pendientes.")]
    [InlineData(3, "Otro", -1, HttpStatusCode.BadRequest, "El resultado debe ser Aprobada o Rechazada.")]
    [InlineData(3, null, 0, HttpStatusCode.BadRequest, "El resultado debe ser Aprobada o Rechazada.")]
    [InlineData(3, "Rechazada", null, HttpStatusCode.BadRequest, "Las unidades no conformes no pueden ser negativas.")]
    [InlineData(3, "Rechazada", -1, HttpStatusCode.BadRequest, "Las unidades no conformes no pueden ser negativas.")]
    [InlineData(3, "Rechazada", 21, HttpStatusCode.BadRequest, "Las unidades no conformes no pueden superar las inspeccionadas.")]
    public async Task DictamenVerificaExistenciaEstadoYCampos(
        int id, string? resultado, int? noConforme, HttpStatusCode estado, string mensaje)
    {
        using HttpResponseMessage respuesta = await DictaminarAsync(id, resultado, noConforme, null);
        await VerificarErrorAsync(respuesta, estado, mensaje);
    }

    [Fact]
    public async Task DictamenRechazaObservacionesDeMasDe500Caracteres()
    {
        using HttpResponseMessage respuesta = await DictaminarAsync(3, "Rechazada", 20, new string('o', 501));
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, "Las observaciones no pueden superar 500 caracteres.");
        Assert.Equal("Pendiente", (await ObtenerAsync(3)).Estado);
    }

    [Fact]
    public async Task AnulacionGuardaElMotivoYLiberaLasUnidades()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/3/anular", new AnularInspeccionProductoTerminadoRequestDto("  Tanda repetida  "));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        InspeccionProductoTerminadoDetalleDto anulada = await ObtenerAsync(3);
        Assert.Equal("Anulada", anulada.Estado);
        Assert.Equal("Tanda repetida", anulada.MotivoAnulacion);
        AvanceInspeccionOrdenDto? avance =
            await _cliente.GetFromJsonAsync<AvanceInspeccionOrdenDto>($"{Ruta}/ordenes/1/avance");
        Assert.NotNull(avance);
        Assert.Equal(70, avance.UnidadesInspeccionadas);
        Assert.Equal(30, avance.UnidadesPorInspeccionar);
    }

    [Theory]
    [InlineData(99, "Motivo", HttpStatusCode.NotFound, "La inspección de producto terminado 99 no existe.")]
    [InlineData(2, "Motivo", HttpStatusCode.Conflict, "Solo se pueden anular inspecciones pendientes.")]
    [InlineData(5, "Motivo", HttpStatusCode.Conflict, "Solo se pueden anular inspecciones pendientes.")]
    [InlineData(3, null, HttpStatusCode.BadRequest, "El motivo de anulación es obligatorio.")]
    [InlineData(3, "   ", HttpStatusCode.BadRequest, "El motivo de anulación es obligatorio.")]
    [InlineData(3, "LARGO", HttpStatusCode.BadRequest, "El motivo de anulación no puede superar 250 caracteres.")]
    public async Task AnulacionVerificaExistenciaEstadoYMotivo(
        int id, string? motivo, HttpStatusCode estado, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anular",
            new AnularInspeccionProductoTerminadoRequestDto(motivo == "LARGO" ? new string('m', 251) : motivo));
        await VerificarErrorAsync(respuesta, estado, mensaje);
    }

    private Task<HttpResponseMessage> DictaminarAsync(int id, string? resultado, int? noConforme, string? observaciones)
    {
        return _cliente.PostAsJsonAsync($"{Ruta}/{id}/dictaminar",
            new DictaminarInspeccionProductoTerminadoRequestDto(resultado, noConforme, observaciones));
    }

    private static DateOnly? Fecha(string? valor)
    {
        if (valor == null)
        {
            return null;
        }

        if (valor == "manana")
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)).AddDays(1);
        }

        return DateOnly.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static decimal? Decimal(string? valor)
    {
        if (valor == null)
        {
            return null;
        }

        return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
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
