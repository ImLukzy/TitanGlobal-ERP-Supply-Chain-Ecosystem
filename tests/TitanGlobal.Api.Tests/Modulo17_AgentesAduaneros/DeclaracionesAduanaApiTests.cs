using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

namespace TitanGlobal.Api.Tests.Modulo17_AgentesAduaneros;

[Collection("Modulo04")]
public class DeclaracionesAduanaApiTests
{
    private const string Ruta = "/api/v1/declaraciones-aduana";

    private readonly HttpClient _clienteHttp;

    public DeclaracionesAduanaApiTests(OrdenesCompraApiFactory fabrica)
    {
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        Modulo17Datos.Restablecer(fabrica);
    }

    [Fact]
    public async Task CreateComputesDutiesOnTheServerAndStartsRegistered()
    {
        await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/anular", new AnularDeclaracionRequestDto { Motivo = "Libera el expediente" });
        CrearDeclaracionRequestDto solicitud = CrearSolicitud(" dam-t-010 ", 1, 1);
        solicitud.ValorAduanero = 10000.50m;
        solicitud.TasaArancelaria = 12.5m;
        solicitud.Regimen = "deposito";

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        int id = await Modulo17Datos.LeerIdAsync(respuesta);
        DeclaracionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<DeclaracionDetalleDto>($"{Ruta}/{id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("DAM-T-010", detalle.Numero);
        Assert.Equal("Deposito", detalle.Regimen);
        Assert.Equal(1250.06m, detalle.DerechosArancel);
        Assert.Equal("Registrada", detalle.Estado);
        Assert.Null(detalle.Canal);
        Assert.Equal("IMP-T-001", detalle.ExpedienteNumero);
        Assert.Equal("Aduanas del Pacifico", detalle.AgenteRazonSocial);
    }

    [Fact]
    public async Task CreateOnlyAcceptsEligibleShipmentsAndActiveAgents()
    {
        CrearDeclaracionRequestDto agenteInactivo = CrearSolicitud("DAM-T-020", 2, 3);
        CrearDeclaracionRequestDto programado = CrearSolicitud("DAM-T-021", 3, 1);
        CrearDeclaracionRequestDto liberado = CrearSolicitud("DAM-T-022", 4, 1);
        CrearDeclaracionRequestDto expedienteInexistente = CrearSolicitud("DAM-T-023", 999, 1);
        CrearDeclaracionRequestDto agenteInexistente = CrearSolicitud("DAM-T-024", 2, 999);

        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, agenteInactivo), HttpStatusCode.Conflict, "El agente aduanero 3 está inactivo.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, programado), HttpStatusCode.Conflict, "El expediente 3 no admite declaraciones en estado Programado.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, liberado), HttpStatusCode.Conflict, "El expediente 4 no admite declaraciones en estado Liberado.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, expedienteInexistente), HttpStatusCode.NotFound, "El expediente 999 no existe.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, agenteInexistente), HttpStatusCode.NotFound, "El agente aduanero 999 no existe.");
    }

    [Fact]
    public async Task CreateValidatesEveryFieldInOrder()
    {
        CrearDeclaracionRequestDto sinNumero = CrearSolicitud("  ", 2, 1);
        CrearDeclaracionRequestDto numeroLargo = CrearSolicitud(new string('N', 31), 2, 1);
        CrearDeclaracionRequestDto regimen = CrearSolicitud("DAM-T-030", 2, 1);
        regimen.Regimen = "Otro";
        CrearDeclaracionRequestDto valorCero = CrearSolicitud("DAM-T-031", 2, 1);
        valorCero.ValorAduanero = 0m;
        CrearDeclaracionRequestDto valorDecimales = CrearSolicitud("DAM-T-032", 2, 1);
        valorDecimales.ValorAduanero = 10.005m;
        CrearDeclaracionRequestDto valorEnorme = CrearSolicitud("DAM-T-033", 2, 1);
        valorEnorme.ValorAduanero = 10_000_000_000_000_000m;
        CrearDeclaracionRequestDto tasaAlta = CrearSolicitud("DAM-T-034", 2, 1);
        tasaAlta.TasaArancelaria = 100.01m;
        CrearDeclaracionRequestDto tasaNegativa = CrearSolicitud("DAM-T-035", 2, 1);
        tasaNegativa.TasaArancelaria = -1m;
        CrearDeclaracionRequestDto tasaDecimales = CrearSolicitud("DAM-T-036", 2, 1);
        tasaDecimales.TasaArancelaria = 5.123m;

        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, sinNumero), HttpStatusCode.BadRequest, "El número es obligatorio.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, numeroLargo), HttpStatusCode.BadRequest, "El número no puede superar 30 caracteres.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, regimen), HttpStatusCode.BadRequest, "El régimen debe ser ImportacionConsumo, AdmisionTemporal o Deposito.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, valorCero), HttpStatusCode.BadRequest, "El valor aduanero debe ser mayor a 0.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, valorDecimales), HttpStatusCode.BadRequest, "El valor aduanero no es válido.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, valorEnorme), HttpStatusCode.BadRequest, "El valor aduanero no es válido.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, tasaAlta), HttpStatusCode.BadRequest, "La tasa arancelaria debe estar entre 0 y 100.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, tasaNegativa), HttpStatusCode.BadRequest, "La tasa arancelaria debe estar entre 0 y 100.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, tasaDecimales), HttpStatusCode.BadRequest, "La tasa arancelaria no es válida.");
    }

    [Fact]
    public async Task OnlyOneActiveDeclarationPerShipmentAndNewOneAfterCancelling()
    {
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud("dam-t-001", 2, 1)),
            HttpStatusCode.Conflict,
            "Ya existe una declaración con el número DAM-T-001.");
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud("DAM-T-040", 1, 1)),
            HttpStatusCode.Conflict,
            "El expediente 1 ya tiene la declaración DAM-T-001.");

        HttpResponseMessage anulada = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/anular", new AnularDeclaracionRequestDto { Motivo = "Valor mal digitado" });
        HttpResponseMessage nueva = await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud("DAM-T-040", 1, 1));

        Assert.Equal(HttpStatusCode.OK, anulada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nueva.StatusCode);
    }

    [Fact]
    public async Task StateMachineRegisteredNumberedLifted()
    {
        HttpResponseMessage sinNumerar = await _clienteHttp.PostAsync($"{Ruta}/1/levantar", null);
        HttpResponseMessage canalInvalido = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/numerar", new NumerarDeclaracionRequestDto { Canal = "Azul" });
        HttpResponseMessage numerada = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/numerar", new NumerarDeclaracionRequestDto { Canal = "rojo" });
        DeclaracionDetalleDto? trasNumerar = await _clienteHttp.GetFromJsonAsync<DeclaracionDetalleDto>($"{Ruta}/1");
        HttpResponseMessage editar = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", CrearActualizacion());
        HttpResponseMessage numerarOtraVez = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/numerar", new NumerarDeclaracionRequestDto { Canal = "Verde" });
        HttpResponseMessage levantada = await _clienteHttp.PostAsync($"{Ruta}/1/levantar", null);
        DeclaracionDetalleDto? trasLevantar = await _clienteHttp.GetFromJsonAsync<DeclaracionDetalleDto>($"{Ruta}/1");
        HttpResponseMessage anularLevantada = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/anular", new AnularDeclaracionRequestDto { Motivo = "Tarde" });

        await Modulo17Datos.AssertBusinessErrorAsync(sinNumerar, HttpStatusCode.Conflict, "La declaración 1 no está numerada.");
        await Modulo17Datos.AssertBusinessErrorAsync(canalInvalido, HttpStatusCode.BadRequest, "El canal debe ser Verde, Naranja o Rojo.");
        Assert.Equal(HttpStatusCode.OK, numerada.StatusCode);
        Assert.NotNull(trasNumerar);
        Assert.Equal("Numerada", trasNumerar.Estado);
        Assert.Equal("Rojo", trasNumerar.Canal);
        Assert.NotNull(trasNumerar.FechaNumeracion);
        await Modulo17Datos.AssertBusinessErrorAsync(editar, HttpStatusCode.Conflict, "La declaración 1 no está registrada.");
        await Modulo17Datos.AssertBusinessErrorAsync(numerarOtraVez, HttpStatusCode.Conflict, "La declaración 1 no está registrada.");
        Assert.Equal(HttpStatusCode.OK, levantada.StatusCode);
        Assert.NotNull(trasLevantar);
        Assert.Equal("Levantada", trasLevantar.Estado);
        Assert.NotNull(trasLevantar.FechaLevante);
        await Modulo17Datos.AssertBusinessErrorAsync(anularLevantada, HttpStatusCode.Conflict, "La declaración 1 no se puede anular porque está Levantada.");
    }

    [Fact]
    public async Task CancelFromRegisteredAndNumberedRequiresReason()
    {
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/anular", new AnularDeclaracionRequestDto { Motivo = "  " }),
            HttpStatusCode.BadRequest,
            "El motivo de anulación es obligatorio.");
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/anular", new AnularDeclaracionRequestDto { Motivo = new string('M', 501) }),
            HttpStatusCode.BadRequest,
            "El motivo no puede superar 500 caracteres.");

        HttpResponseMessage desdeRegistrada = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/anular", new AnularDeclaracionRequestDto { Motivo = "  Error  " });
        HttpResponseMessage desdeNumerada = await _clienteHttp.PostAsJsonAsync($"{Ruta}/2/anular", new AnularDeclaracionRequestDto { Motivo = "Cambio de agente" });
        DeclaracionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<DeclaracionDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, desdeRegistrada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, desdeNumerada.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Anulada", detalle.Estado);
        Assert.Equal("Error", detalle.MotivoAnulacion);
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/anular", new AnularDeclaracionRequestDto { Motivo = "Otra vez" }),
            HttpStatusCode.Conflict,
            "La declaración 1 no se puede anular porque está Anulada.");
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync($"{Ruta}/999/anular", new AnularDeclaracionRequestDto { Motivo = "x" }),
            HttpStatusCode.NotFound,
            "La declaración 999 no existe.");
    }

    [Fact]
    public async Task UpdateRecomputesDutiesAndValidatesAgent()
    {
        ActualizarDeclaracionRequestDto cambio = CrearActualizacion();
        cambio.ValorAduanero = 20000m;
        cambio.TasaArancelaria = 10m;
        HttpResponseMessage actualizada = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", cambio);
        DeclaracionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<DeclaracionDetalleDto>($"{Ruta}/1");
        ActualizarDeclaracionRequestDto conInactivo = CrearActualizacion();
        conInactivo.AgenteAduaneroId = 3;

        Assert.Equal(HttpStatusCode.OK, actualizada.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(2000m, detalle.DerechosArancel);
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", conInactivo), HttpStatusCode.Conflict, "El agente aduanero 3 está inactivo.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PutAsJsonAsync($"{Ruta}/999", cambio), HttpStatusCode.NotFound, "La declaración 999 no existe.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}/999"), HttpStatusCode.NotFound, "La declaración 999 no existe.");
    }

    [Fact]
    public async Task ListFiltersSearchesPagesAndSummaryMatchKnownData()
    {
        PaginaDeclaracionesDto? todas = await _clienteHttp.GetFromJsonAsync<PaginaDeclaracionesDto>(Ruta);
        PaginaDeclaracionesDto? numeradas = await _clienteHttp.GetFromJsonAsync<PaginaDeclaracionesDto>($"{Ruta}?estado=numerada");
        PaginaDeclaracionesDto? verdes = await _clienteHttp.GetFromJsonAsync<PaginaDeclaracionesDto>($"{Ruta}?canal=verde");
        PaginaDeclaracionesDto? porExpediente = await _clienteHttp.GetFromJsonAsync<PaginaDeclaracionesDto>($"{Ruta}?busqueda=imp-t-002");
        PaginaDeclaracionesDto? segunda = await _clienteHttp.GetFromJsonAsync<PaginaDeclaracionesDto>($"{Ruta}?pagina=2&tamanoPagina=3");
        ResumenDeclaracionesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenDeclaracionesDto>($"{Ruta}/resumen");

        Assert.NotNull(todas);
        Assert.Equal(4, todas.Total);
        Assert.Equal(new[] { "DAM-T-004", "DAM-T-003", "DAM-T-002", "DAM-T-001" }, todas.Items.Select(item => item.Numero).ToArray());
        Assert.NotNull(numeradas);
        Assert.Equal("DAM-T-002", Assert.Single(numeradas.Items).Numero);
        Assert.NotNull(verdes);
        Assert.Equal("DAM-T-003", Assert.Single(verdes.Items).Numero);
        Assert.NotNull(porExpediente);
        Assert.Equal("DAM-T-002", Assert.Single(porExpediente.Items).Numero);
        Assert.NotNull(segunda);
        Assert.Equal("DAM-T-001", Assert.Single(segunda.Items).Numero);
        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.TotalDeclaraciones);
        Assert.Equal(4320m, resumen.DerechosArancelVigentes);
        Assert.Equal(
            new[] { ("Registrada", 1), ("Numerada", 1), ("Levantada", 1), ("Anulada", 1) },
            resumen.PorEstado.Select(item => (item.Estado, item.Total)).ToArray());
    }

    [Fact]
    public async Task ListRejectsInvalidPagingAndFilters()
    {
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?pagina=0"), HttpStatusCode.BadRequest, "La página debe ser mayor o igual a 1.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?tamanoPagina=0"), HttpStatusCode.BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?estado=Perdida"), HttpStatusCode.BadRequest, "El estado debe ser Registrada, Numerada, Levantada o Anulada.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?canal=Azul"), HttpStatusCode.BadRequest, "El canal debe ser Verde, Naranja o Rojo.");
    }

    private static CrearDeclaracionRequestDto CrearSolicitud(string numero, int expedienteId, int agenteId)
    {
        return new CrearDeclaracionRequestDto
        {
            Numero = numero,
            ExpedienteImportacionId = expedienteId,
            AgenteAduaneroId = agenteId,
            Regimen = "ImportacionConsumo",
            ValorAduanero = 1000m,
            TasaArancelaria = 5m
        };
    }

    private static ActualizarDeclaracionRequestDto CrearActualizacion()
    {
        return new ActualizarDeclaracionRequestDto
        {
            AgenteAduaneroId = 1,
            Regimen = "ImportacionConsumo",
            ValorAduanero = 1000m,
            TasaArancelaria = 5m
        };
    }
}
