using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo16_Importaciones;

[Collection("Modulo04")]
public class ImportacionesApiTests
{
    private const string Ruta = "/api/v1/importaciones";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ImportacionesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task CreateNormalizesDataAndStartsProgrammed()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud("  imp-t-010 ", "fob", "maritimo"));
        int id = await LeerIdAsync(respuesta);
        ExpedienteDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<ExpedienteDetalleDto>($"{Ruta}/{id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("IMP-T-010", detalle.Numero);
        Assert.Equal("FOB", detalle.Incoterm);
        Assert.Equal("Maritimo", detalle.MedioTransporte);
        Assert.Equal("Programado", detalle.Estado);
        Assert.Equal("OA-T-001", detalle.OrdenNumero);
        Assert.Equal("Proveedor Activo", detalle.ProveedorNombre);
        Assert.Null(detalle.DocumentoTransporte);
        Assert.Null(detalle.FechaEmbarque);
        Assert.Null(detalle.FechaLlegadaReal);
    }

    [Fact]
    public async Task CreateRejectsRepeatedNumberCaseInsensitive()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud("imp-t-001", "FOB", "Maritimo"));

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.Conflict, 409, "Ya existe un expediente con el número IMP-T-001.");
    }

    [Fact]
    public async Task CreateValidatesOrderAndFields()
    {
        CrearExpedienteRequestDto anulada = CrearSolicitud("IMP-T-020", "FOB", "Maritimo");
        anulada.OrdenAbastecimientoId = 2;
        CrearExpedienteRequestDto inexistente = CrearSolicitud("IMP-T-021", "FOB", "Maritimo");
        inexistente.OrdenAbastecimientoId = 999;
        CrearExpedienteRequestDto sinOrden = CrearSolicitud("IMP-T-022", "FOB", "Maritimo");
        sinOrden.OrdenAbastecimientoId = 0;
        CrearExpedienteRequestDto aereoConFob = CrearSolicitud("IMP-T-023", "FOB", "Aereo");
        CrearExpedienteRequestDto incotermInvalido = CrearSolicitud("IMP-T-024", "XXX", "Maritimo");
        CrearExpedienteRequestDto medioInvalido = CrearSolicitud("IMP-T-025", "EXW", "Ferroviario");
        CrearExpedienteRequestDto llegadaPasada = CrearSolicitud("IMP-T-026", "FOB", "Maritimo");
        llegadaPasada.FechaLlegadaEstimada = DateTime.UtcNow.Date.AddDays(-1);
        CrearExpedienteRequestDto sinOrigen = CrearSolicitud("IMP-T-027", "FOB", "Maritimo");
        sinOrigen.LugarOrigen = "   ";
        CrearExpedienteRequestDto sinNumero = CrearSolicitud("   ", "FOB", "Maritimo");

        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, anulada), HttpStatusCode.Conflict, 409, "La orden de abastecimiento 2 está anulada.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, inexistente), HttpStatusCode.BadRequest, 400, "La orden de abastecimiento 999 no existe.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, sinOrden), HttpStatusCode.BadRequest, 400, "La orden de abastecimiento es obligatoria.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, aereoConFob), HttpStatusCode.BadRequest, 400, "El Incoterm FOB solo aplica al transporte marítimo.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, incotermInvalido), HttpStatusCode.BadRequest, 400, "El Incoterm no es válido.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, medioInvalido), HttpStatusCode.BadRequest, 400, "El medio de transporte debe ser Maritimo, Aereo o Terrestre.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, llegadaPasada), HttpStatusCode.BadRequest, 400, "La fecha de llegada estimada debe ser futura.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, sinOrigen), HttpStatusCode.BadRequest, 400, "El lugar de origen es obligatorio.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, sinNumero), HttpStatusCode.BadRequest, 400, "El número es obligatorio.");
    }

    [Fact]
    public async Task FullFlowShipArriveAndRelease()
    {
        DateTime hoy = DateTime.UtcNow.Date;
        HttpResponseMessage embarque = await _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/1/embarcar",
            new EmbarcarExpedienteRequestDto { DocumentoTransporte = "  MSKU-0001 ", FechaEmbarque = hoy.AddDays(-2) });
        ExpedienteDetalleDto? enTransito = await _clienteHttp.GetFromJsonAsync<ExpedienteDetalleDto>($"{Ruta}/1");
        HttpResponseMessage arribo = await _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/1/arribar",
            new ArribarExpedienteRequestDto { FechaLlegadaReal = hoy });
        ExpedienteDetalleDto? enAduana = await _clienteHttp.GetFromJsonAsync<ExpedienteDetalleDto>($"{Ruta}/1");
        HttpResponseMessage liberacion = await _clienteHttp.PostAsync($"{Ruta}/1/liberar", null);
        ExpedienteDetalleDto? liberado = await _clienteHttp.GetFromJsonAsync<ExpedienteDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, embarque.StatusCode);
        Assert.NotNull(enTransito);
        Assert.Equal("EnTransito", enTransito.Estado);
        Assert.Equal("MSKU-0001", enTransito.DocumentoTransporte);
        Assert.Equal(HttpStatusCode.OK, arribo.StatusCode);
        Assert.NotNull(enAduana);
        Assert.Equal("EnAduana", enAduana.Estado);
        Assert.NotNull(enAduana.FechaLlegadaReal);
        Assert.Equal(HttpStatusCode.OK, liberacion.StatusCode);
        Assert.NotNull(liberado);
        Assert.Equal("Liberado", liberado.Estado);
    }

    [Fact]
    public async Task TransitionsRespectTheStateMachine()
    {
        DateTime hoy = DateTime.UtcNow.Date;
        EmbarcarExpedienteRequestDto embarque = new EmbarcarExpedienteRequestDto { DocumentoTransporte = "DOC-X", FechaEmbarque = hoy };

        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/arribar", new ArribarExpedienteRequestDto { FechaLlegadaReal = hoy }), HttpStatusCode.Conflict, 409, "El expediente 1 no está en tránsito.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsync($"{Ruta}/1/liberar", null), HttpStatusCode.Conflict, 409, "El expediente 1 no está en aduana.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/2/embarcar", embarque), HttpStatusCode.Conflict, 409, "El expediente 2 no está programado.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsync($"{Ruta}/2/liberar", null), HttpStatusCode.Conflict, 409, "El expediente 2 no está en aduana.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/3/embarcar", embarque), HttpStatusCode.Conflict, 409, "El expediente 3 no está programado.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/999/embarcar", embarque), HttpStatusCode.NotFound, 404, "El expediente 999 no existe.");
    }

    [Fact]
    public async Task ShipAndArriveValidateDocumentAndDates()
    {
        DateTime hoy = DateTime.UtcNow.Date;

        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/embarcar", new EmbarcarExpedienteRequestDto { DocumentoTransporte = "  ", FechaEmbarque = hoy }), HttpStatusCode.BadRequest, 400, "El documento de transporte es obligatorio.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/embarcar", new EmbarcarExpedienteRequestDto { DocumentoTransporte = new string('D', 41), FechaEmbarque = hoy }), HttpStatusCode.BadRequest, 400, "El documento de transporte no puede superar 40 caracteres.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/embarcar", new EmbarcarExpedienteRequestDto { DocumentoTransporte = "DOC-1", FechaEmbarque = hoy.AddDays(3) }), HttpStatusCode.BadRequest, 400, "La fecha de embarque no puede ser futura.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/2/arribar", new ArribarExpedienteRequestDto { FechaLlegadaReal = hoy.AddDays(3) }), HttpStatusCode.BadRequest, 400, "La fecha de llegada no puede ser futura.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/2/arribar", new ArribarExpedienteRequestDto { FechaLlegadaReal = hoy.AddDays(-10) }), HttpStatusCode.BadRequest, 400, "La fecha de llegada no puede ser anterior al embarque.");
    }

    [Fact]
    public async Task CancelRequiresReasonAndFinalStatesAreLocked()
    {
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/cancelar", new CancelarExpedienteRequestDto { Motivo = "   " }), HttpStatusCode.BadRequest, 400, "El motivo de cancelación es obligatorio.");

        HttpResponseMessage cancelado = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/cancelar", new CancelarExpedienteRequestDto { Motivo = "  Cambio de proveedor " });
        ExpedienteDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<ExpedienteDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, cancelado.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Cancelado", detalle.Estado);
        Assert.Equal("Cambio de proveedor", detalle.MotivoCancelacion);
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/cancelar", new CancelarExpedienteRequestDto { Motivo = "Otra vez" }), HttpStatusCode.Conflict, 409, "El expediente 1 no se puede cancelar porque está Cancelado.");
        await AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync($"{Ruta}/3/cancelar", new CancelarExpedienteRequestDto { Motivo = "Tarde" }), HttpStatusCode.Conflict, 409, "El expediente 3 no se puede cancelar porque está Liberado.");
    }

    [Fact]
    public async Task CancelWorksFromInTransitAndCustoms()
    {
        HttpResponseMessage desdeTransito = await _clienteHttp.PostAsJsonAsync($"{Ruta}/2/cancelar", new CancelarExpedienteRequestDto { Motivo = "Naviera quebró" });
        HttpResponseMessage llegada = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/embarcar", new EmbarcarExpedienteRequestDto { DocumentoTransporte = "DOC-A", FechaEmbarque = DateTime.UtcNow.Date });
        HttpResponseMessage arribo = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/arribar", new ArribarExpedienteRequestDto { FechaLlegadaReal = DateTime.UtcNow.Date });
        HttpResponseMessage desdeAduana = await _clienteHttp.PostAsJsonAsync($"{Ruta}/1/cancelar", new CancelarExpedienteRequestDto { Motivo = "Mercadería retenida" });
        ResumenExpedientesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenExpedientesDto>($"{Ruta}/resumen");

        Assert.Equal(HttpStatusCode.OK, desdeTransito.StatusCode);
        Assert.Equal(HttpStatusCode.OK, llegada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, arribo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, desdeAduana.StatusCode);
        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.EnCurso);
        Assert.Equal(2, resumen.PorEstado.Single(item => item.Estado == "Cancelado").Total);
    }

    [Fact]
    public async Task UpdateAndDeleteOnlyApplyToProgrammedShipments()
    {
        CrearExpedienteRequestDto cambio = CrearSolicitud("IGNORADO", "cif", "maritimo");
        cambio.LugarDestino = "Paita";
        HttpResponseMessage actualizado = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", ComoActualizacion(cambio));
        ExpedienteDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<ExpedienteDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("IMP-T-001", detalle.Numero);
        Assert.Equal("CIF", detalle.Incoterm);
        Assert.Equal("Paita", detalle.LugarDestino);
        await AssertBusinessErrorAsync(await _clienteHttp.PutAsJsonAsync($"{Ruta}/2", ComoActualizacion(cambio)), HttpStatusCode.Conflict, 409, "El expediente 2 no está programado.");
        await AssertBusinessErrorAsync(await _clienteHttp.PutAsJsonAsync($"{Ruta}/999", ComoActualizacion(cambio)), HttpStatusCode.NotFound, 404, "El expediente 999 no existe.");
        await AssertBusinessErrorAsync(await _clienteHttp.DeleteAsync($"{Ruta}/2"), HttpStatusCode.Conflict, 409, "El expediente 2 no está programado y no se puede eliminar; cancélelo.");

        HttpResponseMessage eliminado = await _clienteHttp.DeleteAsync($"{Ruta}/1");
        HttpResponseMessage luego = await _clienteHttp.GetAsync($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.NoContent, eliminado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, luego.StatusCode);
    }

    [Fact]
    public async Task ListOrdersByEstimatedArrivalAndFilters()
    {
        PaginaExpedientesDto? todos = await _clienteHttp.GetFromJsonAsync<PaginaExpedientesDto>(Ruta);
        PaginaExpedientesDto? transito = await _clienteHttp.GetFromJsonAsync<PaginaExpedientesDto>($"{Ruta}?estado=entransito");
        PaginaExpedientesDto? terrestre = await _clienteHttp.GetFromJsonAsync<PaginaExpedientesDto>($"{Ruta}?medioTransporte=terrestre&incoterm=exw");
        PaginaExpedientesDto? busqueda = await _clienteHttp.GetFromJsonAsync<PaginaExpedientesDto>($"{Ruta}?busqueda=%20imp-t-003%20");
        PaginaExpedientesDto? segunda = await _clienteHttp.GetFromJsonAsync<PaginaExpedientesDto>($"{Ruta}?pagina=2&tamanoPagina=2");

        Assert.NotNull(todos);
        Assert.Equal(3, todos.Total);
        Assert.Equal(new[] { "IMP-T-003", "IMP-T-002", "IMP-T-001" }, todos.Items.Select(item => item.Numero).ToArray());
        Assert.NotNull(transito);
        Assert.Equal("IMP-T-002", Assert.Single(transito.Items).Numero);
        Assert.NotNull(terrestre);
        Assert.Equal("IMP-T-002", Assert.Single(terrestre.Items).Numero);
        Assert.NotNull(busqueda);
        Assert.Equal("IMP-T-003", Assert.Single(busqueda.Items).Numero);
        Assert.NotNull(segunda);
        Assert.Equal(3, segunda.Total);
        Assert.Equal("IMP-T-001", Assert.Single(segunda.Items).Numero);
    }

    [Fact]
    public async Task ListRejectsInvalidPagingAndFilters()
    {
        await AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?pagina=0"), HttpStatusCode.BadRequest, 400, "La página debe ser mayor o igual a 1.");
        await AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?tamanoPagina=101"), HttpStatusCode.BadRequest, 400, "El tamaño de página debe estar entre 1 y 100.");
        await AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?estado=Perdido"), HttpStatusCode.BadRequest, 400, "El estado debe ser Programado, EnTransito, EnAduana, Liberado o Cancelado.");
        await AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?incoterm=ZZZ"), HttpStatusCode.BadRequest, 400, "El Incoterm no es válido.");
        await AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}/999"), HttpStatusCode.NotFound, 404, "El expediente 999 no existe.");
    }

    [Fact]
    public async Task SummaryCountsEveryStateAndInProgress()
    {
        ResumenExpedientesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenExpedientesDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.TotalExpedientes);
        Assert.Equal(2, resumen.EnCurso);
        Assert.Equal(
            new[] { ("Programado", 1), ("EnTransito", 1), ("EnAduana", 0), ("Liberado", 1), ("Cancelado", 0) },
            resumen.PorEstado.Select(item => (item.Estado, item.Total)).ToArray());
    }

    private static CrearExpedienteRequestDto CrearSolicitud(string numero, string incoterm, string medio)
    {
        return new CrearExpedienteRequestDto
        {
            Numero = numero,
            OrdenAbastecimientoId = 1,
            Incoterm = incoterm,
            MedioTransporte = medio,
            PaisOrigen = "Chile",
            LugarOrigen = "Valparaiso",
            LugarDestino = "Callao",
            FechaLlegadaEstimada = DateTime.UtcNow.Date.AddDays(30)
        };
    }

    private static ActualizarExpedienteRequestDto ComoActualizacion(CrearExpedienteRequestDto origen)
    {
        return new ActualizarExpedienteRequestDto
        {
            Incoterm = origen.Incoterm,
            MedioTransporte = origen.MedioTransporte,
            PaisOrigen = origen.PaisOrigen,
            LugarOrigen = origen.LugarOrigen,
            LugarDestino = origen.LugarDestino,
            FechaLlegadaEstimada = origen.FechaLlegadaEstimada,
            Observaciones = origen.Observaciones
        };
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ExpedientesImportacion.RemoveRange(contexto.ExpedientesImportacion);
        contexto.DetallesOrdenesAbastecimiento.RemoveRange(contexto.DetallesOrdenesAbastecimiento);
        contexto.OrdenesAbastecimiento.RemoveRange(contexto.OrdenesAbastecimiento);
        contexto.Proveedores.RemoveRange(contexto.Proveedores);

        DateTime hoy = DateTime.UtcNow.Date;
        contexto.Proveedores.Add(new Proveedor { Id = 1, RazonSocial = "Proveedor Activo", Ruc = "20611111111", Pais = "Peru", Activo = true });
        contexto.OrdenesAbastecimiento.AddRange(
            new OrdenAbastecimiento
            {
                Id = 1,
                Numero = "OA-T-001",
                Fecha = hoy.AddDays(-10),
                ProveedorId = 1,
                Estado = "Emitida",
                FechaEntregaEstimada = hoy.AddDays(30)
            },
            new OrdenAbastecimiento
            {
                Id = 2,
                Numero = "OA-T-002",
                Fecha = hoy.AddDays(-9),
                ProveedorId = 1,
                Estado = "Anulada",
                FechaEntregaEstimada = hoy.AddDays(30),
                MotivoAnulacion = "Prueba"
            });
        contexto.ExpedientesImportacion.AddRange(
            CrearExpediente(1, "IMP-T-001", "FOB", "Maritimo", "Programado", hoy.AddDays(30), null, null),
            CrearExpediente(2, "IMP-T-002", "EXW", "Terrestre", "EnTransito", hoy.AddDays(5), hoy.AddDays(-3), null),
            CrearExpediente(3, "IMP-T-003", "CIF", "Maritimo", "Liberado", hoy.AddDays(-9), hoy.AddDays(-20), hoy.AddDays(-10)));
        contexto.SaveChanges();
    }

    private static ExpedienteImportacion CrearExpediente(
        int id,
        string numero,
        string incoterm,
        string medio,
        string estado,
        DateTime llegadaEstimada,
        DateTime? embarque,
        DateTime? llegadaReal)
    {
        return new ExpedienteImportacion
        {
            Id = id,
            Numero = numero,
            OrdenAbastecimientoId = 1,
            Incoterm = incoterm,
            MedioTransporte = medio,
            PaisOrigen = "Chile",
            LugarOrigen = "Valparaiso",
            LugarDestino = "Callao",
            DocumentoTransporte = embarque is null ? null : $"DOC-{id}",
            FechaEmbarque = embarque,
            FechaLlegadaEstimada = llegadaEstimada,
            FechaLlegadaReal = llegadaReal,
            Estado = estado,
            FechaRegistro = DateTime.UtcNow
        };
    }

    private static async Task<int> LeerIdAsync(HttpResponseMessage respuesta)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        return documento.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task AssertBusinessErrorAsync(
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
