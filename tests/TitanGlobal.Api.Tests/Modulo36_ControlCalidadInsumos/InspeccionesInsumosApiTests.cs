using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo36_ControlCalidadInsumos;

public sealed class InspeccionesInsumosApiTests : IClassFixture<InspeccionesInsumosApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/inspecciones-insumos";
    private static readonly DateOnly Hoy = FechaPeru.Hoy();
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);
    private readonly InspeccionesInsumosApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public InspeccionesInsumosApiTests(InspeccionesInsumosApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.PrepararDatos();
        _cliente = fabrica.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, fabrica.Services);
    }

    // «Hoy» va en hora de Perú (UTC-5): 00:00 UTC del 25 son las 19:00 del 24 en Perú, que todavía es el 24.
    [Theory]
    [InlineData(24, 23, 59, 24)]
    [InlineData(25, 0, 0, 24)]
    [InlineData(25, 4, 59, 24)]
    [InlineData(25, 5, 0, 25)]
    public void HoyCambiaDeDiaALaMedianocheDePeru(int diaUtc, int horaUtc, int minutoUtc, int diaEsperado)
    {
        DateTime instanteUtc = new DateTime(2026, 11, diaUtc, horaUtc, minutoUtc, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 11, diaEsperado), FechaPeru.DiaDe(instanteUtc));
    }

    [Fact]
    public async Task SinTokenDevuelveNoAutorizado()
    {
        _cliente.DefaultRequestHeaders.Authorization = null;

        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task AltaNormalizaLosCamposYAsignaElNumero()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new GuardarInspeccionInsumoRequestDto(1, 1, "  lt-2026-a  ", Ayer, 250.5m, 12.5m, "  kg  ", 4.5m, "  Rosa Díaz  "));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        InspeccionInsumoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<InspeccionInsumoCreadoDto>();
        Assert.NotNull(creado);
        Assert.Equal($"CCI-{creado.Id:D6}", creado.Numero);
        Assert.Equal($"{Ruta}/{creado.Id}", respuesta.Headers.Location?.AbsolutePath);
        InspeccionInsumoDetalleDto detalle = await ObtenerAsync(creado.Id);
        Assert.Equal(new InspeccionInsumoDetalleDto(
            creado.Id, creado.Numero, 1, "Resina epóxica", 1, "Andes Suministros S.A.C.", "LT-2026-A", Ayer, 250.5m,
            "kg", "Pendiente", null, 12.5m, 4.5m, "Rosa Díaz", null, null, null, null, null), detalle);
    }

    [Theory]
    [InlineData("\"productoId\":0", 400, "El identificador del producto debe ser mayor que 0.")]
    [InlineData("\"proveedorId\":0", 400, "El identificador del proveedor debe ser mayor que 0.")]
    [InlineData("\"lote\":\"   \"", 400, "El lote es obligatorio.")]
    [InlineData("\"lote\":\"LARGA51\"", 400, "El lote no puede superar 50 caracteres.")]
    [InlineData("\"fechaInspeccion\":null", 400, "La fecha de inspección es obligatoria.")]
    [InlineData("\"fechaInspeccion\":\"MANANA\"", 400, "La fecha de inspección no puede ser posterior a hoy.")]
    [InlineData("\"cantidadRecibida\":0", 400, "La cantidad recibida debe ser mayor que 0 y como máximo 1000000000.")]
    [InlineData("\"cantidadRecibida\":1000000000.001", 400, "La cantidad recibida debe ser mayor que 0 y como máximo 1000000000.")]
    [InlineData("\"cantidadRecibida\":100.0001", 400, "La cantidad recibida admite como máximo 3 decimales.")]
    [InlineData("\"cantidadMuestra\":0", 400, "La cantidad de la muestra debe ser mayor que 0.")]
    [InlineData("\"cantidadMuestra\":1.0001", 400, "La cantidad de la muestra admite como máximo 3 decimales.")]
    [InlineData("\"cantidadMuestra\":100.001", 400, "La muestra no puede superar la cantidad recibida.")]
    [InlineData("\"unidadMedida\":\"  \"", 400, "La unidad de medida es obligatoria.")]
    [InlineData("\"unidadMedida\":\"LARGA21\"", 400, "La unidad de medida no puede superar 20 caracteres.")]
    [InlineData("\"toleranciaDefectosPorcentaje\":-0.01", 400, "La tolerancia de defectos debe estar entre 0 y 100.")]
    [InlineData("\"toleranciaDefectosPorcentaje\":100.01", 400, "La tolerancia de defectos debe estar entre 0 y 100.")]
    [InlineData("\"toleranciaDefectosPorcentaje\":2.555", 400, "La tolerancia de defectos admite como máximo 2 decimales.")]
    [InlineData("\"inspector\":\" \"", 400, "El inspector es obligatorio.")]
    [InlineData("\"inspector\":\"LARGA101\"", 400, "El inspector no puede superar 100 caracteres.")]
    [InlineData("\"productoId\":99", 400, "El producto 99 no existe.")]
    [InlineData("\"proveedorId\":99", 400, "El proveedor 99 no existe.")]
    [InlineData("\"proveedorId\":3", 409, "El proveedor 3 está inactivo.")]
    public async Task AltaInvalidaDevuelveElMensajeLiteralSinCrearFilas(string campo, int estado, string mensaje)
    {
        using HttpResponseMessage respuesta = await EnviarJsonAsync(HttpMethod.Post, Ruta, CuerpoCon(campo));

        await VerificarErrorAsync(respuesta, (HttpStatusCode)estado, mensaje);
        Assert.Equal(0, _fabrica.ContarInspecciones());
    }

    [Fact]
    public async Task AltaAceptaLosLimitesYLaFechaDeHoy()
    {
        int id = await CrearAsync(1, 1, new string('l', 50), Hoy, 1000000000m, 1000000000m, 0m,
            new string('u', 20), new string('i', 100));
        InspeccionInsumoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal(Hoy, detalle.FechaInspeccion);
        Assert.Equal(new string('L', 50), detalle.Lote);
        Assert.Equal(1000000000m, detalle.CantidadMuestra);
        await CrearAsync(1, 1, "MIN", Hoy, 0.001m, 0.001m, 100m);
    }

    [Fact]
    public async Task LoteRepetidoDevuelveConflictoYUnaAnulacionLoLibera()
    {
        int primera = await CrearAsync(1, 1, "L-01");
        using HttpResponseMessage repetida = await _cliente.PostAsJsonAsync(Ruta, Solicitud(1, 1, "  l-01 "));
        await VerificarErrorAsync(repetida, HttpStatusCode.Conflict,
            "Ya existe una inspección del lote L-01 para ese producto y proveedor.");
        Assert.Equal(1, _fabrica.ContarInspecciones());

        await CrearAsync(1, 2, "L-01");
        await CrearAsync(2, 1, "L-01");

        using HttpResponseMessage anulada = await _cliente.PostAsJsonAsync($"{Ruta}/{primera}/anular",
            new AnularInspeccionInsumoRequestDto("  Lote mal digitado  "));
        Assert.Equal(HttpStatusCode.OK, anulada.StatusCode);
        Assert.Equal("Lote mal digitado", (await ObtenerAsync(primera)).MotivoAnulacion);
        await CrearAsync(1, 1, "L-01");
    }

    [Fact]
    public async Task EdicionReemplazaLosCamposYConservaSuPropioLote()
    {
        int id = await CrearAsync(1, 1, "L-10");
        int otra = await CrearAsync(1, 1, "L-11");
        using HttpResponseMessage editada = await _cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new GuardarInspeccionInsumoRequestDto(2, 2, " l-10 ", Hoy, 80m, 8m, " un ", 2.25m, " Luis "));

        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(id, (await editada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        InspeccionInsumoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal((2, 2, "L-10", Hoy, 80m, 8m, "un", 2.25m, "Luis"), (detalle.ProductoId, detalle.ProveedorId,
            detalle.Lote, detalle.FechaInspeccion, detalle.CantidadRecibida, detalle.CantidadMuestra, detalle.UnidadMedida,
            detalle.ToleranciaDefectosPorcentaje, detalle.Inspector));

        using HttpResponseMessage mismoLote = await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Solicitud(2, 2, "L-10"));
        Assert.Equal(HttpStatusCode.OK, mismoLote.StatusCode);

        using HttpResponseMessage ocupado = await _cliente.PutAsJsonAsync($"{Ruta}/{otra}", Solicitud(2, 2, "L-10"));
        await VerificarErrorAsync(ocupado, HttpStatusCode.Conflict,
            "Ya existe una inspección del lote L-10 para ese producto y proveedor.");

        using HttpResponseMessage inactivo = await _cliente.PutAsJsonAsync($"{Ruta}/{otra}", Solicitud(1, 3, "L-11"));
        await VerificarErrorAsync(inactivo, HttpStatusCode.Conflict, "El proveedor 3 está inactivo.");

        using HttpResponseMessage invalida = await _cliente.PutAsJsonAsync($"{Ruta}/{otra}",
            new GuardarInspeccionInsumoRequestDto(1, 1, "L-11", Hoy.AddDays(1), 100m, 10m, "kg", 5m, "Rosa"));
        await VerificarErrorAsync(invalida, HttpStatusCode.BadRequest, "La fecha de inspección no puede ser posterior a hoy.");
        Assert.Equal("L-11", _fabrica.Leer(otra).Lote);
        Assert.Equal(1, _fabrica.Leer(otra).ProveedorId);

        using HttpResponseMessage inexistente = await _cliente.PutAsJsonAsync($"{Ruta}/999", Solicitud(1, 1, "X"));
        await VerificarErrorAsync(inexistente, HttpStatusCode.NotFound, "La inspección de insumos 999 no existe.");
    }

    [Fact]
    public async Task AprobarDentroDeLaToleranciaCalculaElPorcentaje()
    {
        int id = await CrearAsync(1, 1, "L-20", Ayer, 100m, 10m, 5m);
        await DictaminarAsync(id, "aprobada", 0.5m, "  Sin observaciones graves  ");

        InspeccionInsumoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal("Aprobada", detalle.Estado);
        Assert.Equal(5.00m, detalle.PorcentajeDefectos);
        Assert.Equal(0.5m, detalle.CantidadDefectuosa);
        Assert.False(detalle.SuperaTolerancia);
        Assert.Equal("Sin observaciones graves", detalle.Observaciones);
        Assert.NotNull(detalle.FechaDictamen);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaDictamen.Value.Kind);
    }

    [Fact]
    public async Task AprobarSobreLaToleranciaDaConflictoYRechazarFunciona()
    {
        int id = await CrearAsync(1, 1, "L-21", Ayer, 100m, 8m, 5m);
        using HttpResponseMessage aprobar = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/dictaminar",
            new DictaminarInspeccionInsumoRequestDto("Aprobada", 1m, null));
        await VerificarErrorAsync(aprobar, HttpStatusCode.Conflict,
            "La inspección supera la tolerancia de defectos (12.50 % > 5.00 %) y solo puede rechazarse.");
        Assert.Equal("Pendiente", _fabrica.Leer(id).Estado);

        await DictaminarAsync(id, "Rechazada", 1m, "   ");
        InspeccionInsumoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal(("Rechazada", (decimal?)12.50m, (bool?)true, (string?)null),
            (detalle.Estado, detalle.PorcentajeDefectos, detalle.SuperaTolerancia, detalle.Observaciones));
    }

    [Fact]
    public async Task RechazarDentroDeLaToleranciaTambienSePermite()
    {
        int id = await CrearAsync(1, 1, "L-22", Ayer, 100m, 10m, 5m);
        await DictaminarAsync(id, "RECHAZADA", 0m, null);

        InspeccionInsumoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal(("Rechazada", (decimal?)0.00m, (bool?)false), (detalle.Estado, detalle.PorcentajeDefectos, detalle.SuperaTolerancia));
    }

    [Theory]
    [InlineData("{\"resultado\":\"Otro\",\"cantidadDefectuosa\":0}", "El resultado debe ser Aprobada o Rechazada.")]
    [InlineData("{\"cantidadDefectuosa\":0}", "El resultado debe ser Aprobada o Rechazada.")]
    [InlineData("{\"resultado\":\"Rechazada\",\"cantidadDefectuosa\":-0.001}", "La cantidad defectuosa no puede ser negativa.")]
    [InlineData("{\"resultado\":\"Rechazada\",\"cantidadDefectuosa\":0.0001}", "La cantidad defectuosa admite como máximo 3 decimales.")]
    [InlineData("{\"resultado\":\"Rechazada\",\"cantidadDefectuosa\":10.001}", "La cantidad defectuosa no puede superar la muestra.")]
    [InlineData("{\"resultado\":\"Rechazada\",\"cantidadDefectuosa\":1,\"observaciones\":\"LARGA501\"}", "Las observaciones no pueden superar 500 caracteres.")]
    public async Task DictamenInvalidoDevuelveElMensajeLiteralSinCambiarElEstado(string json, string mensaje)
    {
        int id = await CrearAsync(1, 1, "L-30", Ayer, 100m, 10m, 5m);
        using HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Post, $"{Ruta}/{id}/dictaminar", json.Replace("LARGA501", new string('o', 501)));

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
        InspeccionInsumo guardada = _fabrica.Leer(id);
        Assert.Equal(("Pendiente", (decimal?)null, (DateTime?)null), (guardada.Estado, guardada.CantidadDefectuosa, guardada.FechaDictamen));
    }

    [Fact]
    public async Task LasTransicionesSoloSalenDePendiente()
    {
        int aprobada = await CrearAsync(1, 1, "L-40");
        await DictaminarAsync(aprobada, "Aprobada", 0m, null);
        int anulada = await CrearAsync(1, 1, "L-41");
        await AnularAsync(anulada, "Error de registro");

        foreach (int id in new[] { aprobada, anulada })
        {
            using HttpResponseMessage dictamen = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/dictaminar",
                new DictaminarInspeccionInsumoRequestDto("Rechazada", 0m, null));
            await VerificarErrorAsync(dictamen, HttpStatusCode.Conflict, "Solo se pueden dictaminar inspecciones pendientes.");
            using HttpResponseMessage anulacion = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anular",
                new AnularInspeccionInsumoRequestDto("Otra vez"));
            await VerificarErrorAsync(anulacion, HttpStatusCode.Conflict, "Solo se pueden anular inspecciones pendientes.");
            using HttpResponseMessage edicion = await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Solicitud(1, 1, "L-99"));
            await VerificarErrorAsync(edicion, HttpStatusCode.Conflict, "Solo se pueden editar inspecciones pendientes.");
        }

        Assert.Equal("Aprobada", _fabrica.Leer(aprobada).Estado);
        Assert.Equal("Anulada", _fabrica.Leer(anulada).Estado);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("POST", "/dictaminar")]
    [InlineData("POST", "/anular")]
    public async Task InspeccionInexistenteDevuelveNoEncontrada(string metodo, string accion)
    {
        string cuerpo = accion == "/dictaminar"
            ? "{\"resultado\":\"Aprobada\",\"cantidadDefectuosa\":0}"
            : "{\"motivo\":\"x\"}";
        using HttpResponseMessage respuesta = metodo == "GET"
            ? await _cliente.GetAsync($"{Ruta}/999")
            : await EnviarJsonAsync(HttpMethod.Post, $"{Ruta}/999{accion}", cuerpo);

        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "La inspección de insumos 999 no existe.");
    }

    [Theory]
    [InlineData("{\"motivo\":\"   \"}", "El motivo de anulación es obligatorio.")]
    [InlineData("{}", "El motivo de anulación es obligatorio.")]
    [InlineData("{\"motivo\":\"LARGA251\"}", "El motivo de anulación no puede superar 250 caracteres.")]
    public async Task AnulacionSinMotivoValidoNoCambiaElEstado(string json, string mensaje)
    {
        int id = await CrearAsync(1, 1, "L-50");
        using HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Post, $"{Ruta}/{id}/anular", json.Replace("LARGA251", new string('m', 251)));

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
        Assert.Equal("Pendiente", _fabrica.Leer(id).Estado);
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?busqueda=LARGA51", "La búsqueda no puede superar 50 caracteres.")]
    [InlineData("?productoId=0", "El identificador del producto debe ser mayor que 0.")]
    [InlineData("?proveedorId=-1", "El identificador del proveedor debe ser mayor que 0.")]
    [InlineData("?estado=Cerrada", "El estado de la inspección de insumos no es válido.")]
    [InlineData("?desde=2026-11-02&hasta=2026-11-01", "La fecha desde no puede ser posterior a la fecha hasta.")]
    public async Task ListadoConFiltrosInvalidosDevuelveElMensajeLiteral(string consulta, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + consulta.Replace("LARGA51", new string('b', 51)));

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task ListadoFiltraOrdenaYPagina()
    {
        int a = _fabrica.AgregarDirecto(Directo(1, 1, "LOTE-A", new DateOnly(2026, 10, 1), "Aprobada", 0m));
        int b = _fabrica.AgregarDirecto(Directo(1, 2, "LOTE-B", new DateOnly(2026, 10, 5), "Rechazada", 3m));
        int c = _fabrica.AgregarDirecto(Directo(2, 1, "OTRO-C", new DateOnly(2026, 10, 5), "Pendiente", null));
        int d = _fabrica.AgregarDirecto(Directo(2, 2, "LOTE-D", new DateOnly(2026, 9, 20), "Anulada", null));

        Assert.Equal(new[] { c, b, a, d }, await IdsAsync(""));
        Assert.Equal(new[] { b, a, d }, await IdsAsync("?busqueda=lote"));
        Assert.Equal(new[] { c }, await IdsAsync($"?busqueda=cci-{c:D6}"));
        Assert.Equal(new[] { b, a }, await IdsAsync("?productoId=1"));
        Assert.Equal(new[] { b, d }, await IdsAsync("?proveedorId=2"));
        Assert.Equal(new[] { b }, await IdsAsync("?estado=rechazada"));
        Assert.Equal(new[] { c, b, a }, await IdsAsync("?desde=2026-10-01&hasta=2026-10-05"));
        Assert.Equal(new[] { a }, await IdsAsync("?productoId=1&proveedorId=1&estado=Aprobada&desde=2026-10-01"));

        PaginaInspeccionesInsumosDto segunda = await ListarAsync("?pagina=2&tamanoPagina=3");
        Assert.Equal((4, 2, 3), (segunda.Total, segunda.Pagina, segunda.TamanoPagina));
        Assert.Equal(new[] { d }, segunda.Items.Select(item => item.Id));
        PaginaInspeccionesInsumosDto vacia = await ListarAsync("?pagina=9");
        Assert.Equal((4, 0), (vacia.Total, vacia.Items.Count));

        InspeccionInsumoListadoDto rechazada = (await ListarAsync("?estado=Rechazada")).Items.Single();
        Assert.Equal(new InspeccionInsumoListadoDto(b, $"CCI-{b:D6}", 1, "Resina epóxica", 2, "Bolivar Químicos S.A.",
            "LOTE-B", new DateOnly(2026, 10, 5), 100m, "kg", "Rechazada", 30.00m), rechazada);
    }

    [Fact]
    public async Task ResumenAgregaPorEstadoYPorProveedor()
    {
        ResumenInspeccionesInsumosDto vacio = await ResumenAsync();
        Assert.Equal((0, 0m), (vacio.TotalInspecciones, vacio.TasaRechazoPorcentaje));
        Assert.Empty(vacio.PorEstado);
        Assert.Empty(vacio.PorProveedor);

        _fabrica.AgregarDirecto(Directo(1, 1, "R1", Ayer, "Aprobada", 0m));
        _fabrica.AgregarDirecto(Directo(1, 1, "R2", Ayer, "Aprobada", 0m));
        _fabrica.AgregarDirecto(Directo(1, 1, "R3", Ayer, "Rechazada", 2m));
        _fabrica.AgregarDirecto(Directo(1, 2, "R4", Ayer, "Rechazada", 2m));
        _fabrica.AgregarDirecto(Directo(2, 2, "R5", Ayer, "Pendiente", null));
        _fabrica.AgregarDirecto(Directo(2, 3, "R6", Ayer, "Anulada", null));
        _fabrica.AgregarDirecto(Directo(2, 3, "R7", Ayer, "Pendiente", null));

        ResumenInspeccionesInsumosDto resumen = await ResumenAsync();
        Assert.Equal(7, resumen.TotalInspecciones);
        Assert.Equal(new[] { ("Pendiente", 2), ("Aprobada", 2), ("Rechazada", 2), ("Anulada", 1) },
            resumen.PorEstado.Select(fila => (fila.Estado, fila.Total)));
        Assert.Equal(50.00m, resumen.TasaRechazoPorcentaje);
        Assert.Equal(new[]
        {
            new ProveedorInspeccionesInsumosDto(2, "Bolivar Químicos S.A.", 0, 1, 100.00m),
            new ProveedorInspeccionesInsumosDto(1, "Andes Suministros S.A.C.", 2, 1, 33.33m)
        }, resumen.PorProveedor);
    }

    private static GuardarInspeccionInsumoRequestDto Solicitud(int productoId, int proveedorId, string lote)
    {
        return new GuardarInspeccionInsumoRequestDto(productoId, proveedorId, lote, Ayer, 100m, 10m, "kg", 5m, "Rosa");
    }

    private static string CuerpoCon(string campo)
    {
        Dictionary<string, string> campos = new Dictionary<string, string>
        {
            ["productoId"] = "1",
            ["proveedorId"] = "1",
            ["lote"] = "\"L-1\"",
            ["fechaInspeccion"] = $"\"{Ayer:yyyy-MM-dd}\"",
            ["cantidadRecibida"] = "100",
            ["cantidadMuestra"] = "10",
            ["unidadMedida"] = "\"kg\"",
            ["toleranciaDefectosPorcentaje"] = "5",
            ["inspector"] = "\"Rosa\""
        };
        string nombre = campo.Substring(1, campo.IndexOf('"', 1) - 1);
        campos[nombre] = campo.Substring(campo.IndexOf(':') + 1)
            .Replace("MANANA", Hoy.AddDays(1).ToString("yyyy-MM-dd"))
            .Replace("LARGA101", new string('a', 101))
            .Replace("LARGA51", new string('a', 51))
            .Replace("LARGA21", new string('a', 21));
        return "{" + string.Join(",", campos.Select(par => $"\"{par.Key}\":{par.Value}")) + "}";
    }

    private static InspeccionInsumo Directo(
        int productoId, int proveedorId, string lote, DateOnly fecha, string estado, decimal? defectuosa)
    {
        return new InspeccionInsumo
        {
            Numero = $"TMP-{Guid.NewGuid():N}".Substring(0, 20),
            ProductoId = productoId,
            ProveedorId = proveedorId,
            Lote = lote,
            FechaInspeccion = fecha,
            CantidadRecibida = 100m,
            CantidadMuestra = 10m,
            UnidadMedida = "kg",
            ToleranciaDefectosPorcentaje = 5m,
            Inspector = "Rosa",
            Estado = estado,
            CantidadDefectuosa = defectuosa,
            FechaDictamen = defectuosa.HasValue ? DateTime.UtcNow : null
        };
    }

    private async Task<int> CrearAsync(
        int productoId, int proveedorId, string lote, DateOnly? fecha = null, decimal recibida = 100m,
        decimal muestra = 10m, decimal tolerancia = 5m, string unidad = "kg", string inspector = "Rosa")
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, new GuardarInspeccionInsumoRequestDto(
            productoId, proveedorId, lote, fecha ?? Ayer, recibida, muestra, unidad, tolerancia, inspector));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        InspeccionInsumoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<InspeccionInsumoCreadoDto>();
        Assert.NotNull(creado);
        return creado.Id;
    }

    private async Task DictaminarAsync(int id, string resultado, decimal defectuosa, string? observaciones)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/dictaminar",
            new DictaminarInspeccionInsumoRequestDto(resultado, defectuosa, observaciones));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(id, (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    private async Task AnularAsync(int id, string motivo)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anular",
            new AnularInspeccionInsumoRequestDto(motivo));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    private async Task<InspeccionInsumoDetalleDto> ObtenerAsync(int id)
    {
        InspeccionInsumoDetalleDto? detalle = await _cliente.GetFromJsonAsync<InspeccionInsumoDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(detalle);
        return detalle;
    }

    private async Task<PaginaInspeccionesInsumosDto> ListarAsync(string consulta)
    {
        PaginaInspeccionesInsumosDto? pagina = await _cliente.GetFromJsonAsync<PaginaInspeccionesInsumosDto>(Ruta + consulta);
        Assert.NotNull(pagina);
        return pagina;
    }

    private async Task<int[]> IdsAsync(string consulta)
    {
        PaginaInspeccionesInsumosDto pagina = await ListarAsync(consulta);
        return pagina.Items.Select(item => item.Id).ToArray();
    }

    private async Task<ResumenInspeccionesInsumosDto> ResumenAsync()
    {
        ResumenInspeccionesInsumosDto? resumen = await _cliente.GetFromJsonAsync<ResumenInspeccionesInsumosDto>($"{Ruta}/resumen");
        Assert.NotNull(resumen);
        return resumen;
    }

    private async Task<HttpResponseMessage> EnviarJsonAsync(HttpMethod metodo, string ruta, string json)
    {
        using HttpRequestMessage solicitud = new HttpRequestMessage(metodo, ruta)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await _cliente.SendAsync(solicitud);
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, cuerpo.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, cuerpo.GetProperty("Message").GetString());
        Assert.False(cuerpo.TryGetProperty("Detail", out JsonElement _));
    }

    public void Dispose()
    {
        _cliente.Dispose();
    }
}
