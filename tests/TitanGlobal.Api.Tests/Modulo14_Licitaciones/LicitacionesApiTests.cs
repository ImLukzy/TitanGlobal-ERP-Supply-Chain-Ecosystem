using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo14_Licitaciones;

[Collection("Modulo04")]
public class LicitacionesApiTests
{
    private const string Ruta = "/api/v1/licitaciones";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public LicitacionesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerLicitaciones();
    }

    [Fact]
    public async Task ListReturnsLicitacionesNewestFirstWithOfferCounts()
    {
        PaginaLicitacionesDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaLicitacionesDto>(Ruta);

        Assert.NotNull(pagina);
        Assert.Equal(5, pagina.Total);
        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, pagina.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 0, 1, 2, 2, 2 }, pagina.Items.Select(item => item.TotalOfertas).ToArray());
    }

    [Fact]
    public async Task ListFiltersBySearchStateAndTypeWithoutCaseAndPaginates()
    {
        PaginaLicitacionesDto? porTitulo = await _clienteHttp.GetFromJsonAsync<PaginaLicitacionesDto>(
            $"{Ruta}?busqueda=EMPAQUES");
        PaginaLicitacionesDto? porNumero = await _clienteHttp.GetFromJsonAsync<PaginaLicitacionesDto>(
            $"{Ruta}?busqueda=lic-2026-003");
        PaginaLicitacionesDto? abiertas = await _clienteHttp.GetFromJsonAsync<PaginaLicitacionesDto>(
            $"{Ruta}?estado=abierta");
        PaginaLicitacionesDto? subastas = await _clienteHttp.GetFromJsonAsync<PaginaLicitacionesDto>(
            $"{Ruta}?tipo=subastainversa");
        PaginaLicitacionesDto? segundaPagina = await _clienteHttp.GetFromJsonAsync<PaginaLicitacionesDto>(
            $"{Ruta}?pagina=2&tamanoPagina=2");

        Assert.Equal(new[] { 1 }, porTitulo?.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 3 }, porNumero?.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 2, 1 }, abiertas?.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 2 }, subastas?.Items.Select(item => item.Id).ToArray());
        Assert.NotNull(segundaPagina);
        Assert.Equal(5, segundaPagina.Total);
        Assert.Equal(new[] { 3, 2 }, segundaPagina.Items.Select(item => item.Id).ToArray());
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?estado=Perdida", "El estado debe ser Abierta, Cerrada, Adjudicada o Cancelada.")]
    [InlineData("?tipo=Remate", "El tipo debe ser ConcursoPrecios o SubastaInversa.")]
    public async Task InvalidListFiltersReturnBadRequestWithLiteralMessage(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailOfOpenAuctionOrdersOffersByAmount()
    {
        LicitacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/2");

        Assert.NotNull(detalle);
        Assert.Equal("LIC-2026-002", detalle.Numero);
        Assert.Equal("SubastaInversa", detalle.Tipo);
        Assert.Equal("Abierta", detalle.Estado);
        Assert.Null(detalle.ProveedorAdjudicadoId);
        Assert.Equal(new[] { 6, 3 }, detalle.Ofertas.Select(oferta => oferta.ProveedorId).ToArray());
        Assert.All(detalle.Ofertas, oferta => Assert.False(oferta.EsGanadora));
    }

    [Fact]
    public async Task DetailOfAwardedTenderMarksTheWinningOffer()
    {
        LicitacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/3");

        Assert.NotNull(detalle);
        Assert.Equal("Adjudicada", detalle.Estado);
        Assert.Equal(1, detalle.ProveedorAdjudicadoId);
        Assert.Equal("Andes Suministros S.A.C.", detalle.ProveedorAdjudicadoNombre);
        Assert.Equal(28000m, detalle.MontoAdjudicado);
        Assert.Equal(new[] { true, false }, detalle.Ofertas.Select(oferta => oferta.EsGanadora).ToArray());
    }

    [Fact]
    public async Task DetailOfMissingTenderReturnsNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync($"{Ruta}/9999");

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "La licitación 9999 no existe.");
    }

    [Fact]
    public async Task CreateStoresOpenTenderWithNormalizedFields()
    {
        int id = await CrearLicitacionAsync("  lic-2026-100  ", "  Compra de pallets  ", "concursoprecios", 5000, 30, "   ");

        LicitacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/{id}");

        Assert.NotNull(detalle);
        Assert.Equal("LIC-2026-100", detalle.Numero);
        Assert.Equal("Compra de pallets", detalle.Titulo);
        Assert.Equal("ConcursoPrecios", detalle.Tipo);
        Assert.Equal("Abierta", detalle.Estado);
        Assert.Null(detalle.Descripcion);
        Assert.Empty(detalle.Ofertas);
    }

    [Theory]
    [InlineData("", "Titulo", "ConcursoPrecios", 100.0, 30, 0, "El número es obligatorio.")]
    [InlineData("LIC-123456789012345678", "Titulo", "ConcursoPrecios", 100.0, 30, 0, "El número no puede superar 20 caracteres.")]
    [InlineData("LIC-X", "", "ConcursoPrecios", 100.0, 30, 0, "El título es obligatorio.")]
    [InlineData("LIC-X", "Titulo", "Remate", 100.0, 30, 0, "El tipo debe ser ConcursoPrecios o SubastaInversa.")]
    [InlineData("LIC-X", "Titulo", "ConcursoPrecios", 0.0, 30, 0, "El presupuesto referencial debe ser mayor a 0.")]
    [InlineData("LIC-X", "Titulo", "ConcursoPrecios", 100.123, 30, 0, "El presupuesto referencial no es válido.")]
    [InlineData("LIC-X", "Titulo", "ConcursoPrecios", 100.0, -1, 0, "La fecha de cierre debe ser futura.")]
    [InlineData("LIC-X", "Titulo", "ConcursoPrecios", 100.0, 30, 501, "La descripción no puede superar 500 caracteres.")]
    public async Task CreateWithInvalidDataReturnsBadRequestWithLiteralMessage(
        string numero,
        string titulo,
        string tipo,
        double presupuesto,
        int diasCierre,
        int largoDescripcion,
        string mensaje)
    {
        string? descripcion = largoDescripcion > 0 ? new string('x', largoDescripcion) : null;

        HttpResponseMessage respuesta = await PublicarLicitacionAsync(
            numero,
            titulo,
            tipo,
            (decimal)presupuesto,
            diasCierre,
            descripcion);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task CreateWithLongTitleReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await PublicarLicitacionAsync(
            "LIC-X",
            new string('t', 151),
            "ConcursoPrecios",
            100m,
            30,
            null);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El título no puede superar 150 caracteres.");
    }

    [Fact]
    public async Task CreateWithExistingNumberReturnsConflict()
    {
        HttpResponseMessage respuesta = await PublicarLicitacionAsync(
            "lic-2026-001",
            "Otra",
            "ConcursoPrecios",
            100m,
            30,
            null);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe una licitación con el número LIC-2026-001.");
    }

    [Fact]
    public async Task UpdateChangesEditableFieldsAndKeepsNumberAndType()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            $"{Ruta}/1",
            new ActualizarLicitacionRequestDto
            {
                Titulo = "  Empaques ampliados  ",
                Descripcion = "Nueva descripción",
                PresupuestoReferencial = 25000m,
                FechaCierre = DateTime.UtcNow.AddDays(60)
            });
        LicitacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("LIC-2026-001", detalle.Numero);
        Assert.Equal("ConcursoPrecios", detalle.Tipo);
        Assert.Equal("Empaques ampliados", detalle.Titulo);
        Assert.Equal(25000m, detalle.PresupuestoReferencial);
    }

    [Fact]
    public async Task UpdateOfMissingOrNotOpenTenderReturnsLiteralErrors()
    {
        HttpResponseMessage inexistente = await ActualizarAsync(9999, "Titulo", 100m, 30);
        HttpResponseMessage adjudicada = await ActualizarAsync(3, "Titulo", 100m, 30);

        await AssertBusinessErrorAsync(inexistente, HttpStatusCode.NotFound, 404, "La licitación 9999 no existe.");
        await AssertBusinessErrorAsync(adjudicada, HttpStatusCode.Conflict, 409, "La licitación 3 no está abierta.");
    }

    [Fact]
    public async Task UpdateWithBudgetBelowAnExistingOfferReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await ActualizarAsync(1, "Titulo", 19000m, 30);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El presupuesto no puede ser menor a una oferta ya presentada.");
    }

    [Fact]
    public async Task DeleteWithoutOffersReturnsNoContentAndWithOffersReturnsConflict()
    {
        HttpResponseMessage borrada = await _clienteHttp.DeleteAsync($"{Ruta}/5");
        HttpResponseMessage conOfertas = await _clienteHttp.DeleteAsync($"{Ruta}/1");
        HttpResponseMessage inexistente = await _clienteHttp.DeleteAsync($"{Ruta}/9999");

        Assert.Equal(HttpStatusCode.NoContent, borrada.StatusCode);
        await AssertBusinessErrorAsync(
            conOfertas,
            HttpStatusCode.Conflict,
            409,
            "La licitación 1 tiene ofertas y no se puede eliminar; cancélela.");
        await AssertBusinessErrorAsync(inexistente, HttpStatusCode.NotFound, 404, "La licitación 9999 no existe.");
    }

    [Fact]
    public async Task CreateOfferAddsItToTheTender()
    {
        HttpResponseMessage respuesta = await PublicarOfertaAsync(1, 3, 19000m, 20, "  Entrega en planta  ");
        LicitacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(new[] { 1, 3, 2 }, detalle.Ofertas.Select(oferta => oferta.ProveedorId).ToArray());
        Assert.Equal("Entrega en planta", detalle.Ofertas[1].Observaciones);
    }

    [Theory]
    [InlineData(9999, 3, 1000.0, 20, 404, "La licitación 9999 no existe.")]
    [InlineData(3, 3, 1000.0, 20, 409, "La licitación 3 no está abierta.")]
    [InlineData(1, 0, 1000.0, 20, 400, "El proveedor es obligatorio.")]
    [InlineData(1, 3, 0.0, 20, 400, "El monto debe ser mayor a 0.")]
    [InlineData(1, 3, 20000.01, 20, 400, "El monto no puede superar el presupuesto referencial.")]
    [InlineData(1, 3, 1000.0, 0, 400, "El plazo de entrega debe estar entre 1 y 365 días.")]
    [InlineData(1, 3, 1000.0, 366, 400, "El plazo de entrega debe estar entre 1 y 365 días.")]
    [InlineData(1, 999, 1000.0, 20, 400, "El proveedor 999 no existe.")]
    [InlineData(1, 5, 1000.0, 20, 400, "El proveedor 5 está inactivo.")]
    [InlineData(1, 1, 1000.0, 20, 409, "El proveedor 1 ya presentó una oferta en la licitación 1.")]
    public async Task CreateOfferWithInvalidDataReturnsLiteralError(
        int licitacionId,
        int proveedorId,
        double monto,
        int plazo,
        int estado,
        string mensaje)
    {
        HttpResponseMessage respuesta = await PublicarOfertaAsync(licitacionId, proveedorId, (decimal)monto, plazo, null);

        await AssertBusinessErrorAsync(respuesta, (HttpStatusCode)estado, estado, mensaje);
    }

    [Fact]
    public async Task CreateOfferWithLongObservationsReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await PublicarOfertaAsync(1, 3, 1000m, 20, new string('x', 501));

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "Las observaciones no pueden superar 500 caracteres.");
    }

    [Fact]
    public async Task CreateOfferAfterTheClosingDateReturnsConflict()
    {
        VencerLicitacion(1);

        HttpResponseMessage respuesta = await PublicarOfertaAsync(1, 3, 1000m, 20, null);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "La licitación 1 ya venció su plazo de ofertas.");
    }

    [Fact]
    public async Task ImproveOfferLowersTheAmountInAnInverseAuction()
    {
        HttpResponseMessage respuesta = await MejorarAsync(2, 3, 44000m);
        LicitacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/2");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(new[] { 3, 6 }, detalle.Ofertas.Select(oferta => oferta.ProveedorId).ToArray());
        Assert.Equal(44000m, detalle.Ofertas[0].Monto);
    }

    [Fact]
    public async Task ImproveOfferWithInvalidStateReturnsLiteralErrors()
    {
        HttpResponseMessage concurso = await MejorarAsync(1, 1, 1000m);
        HttpResponseMessage igual = await MejorarAsync(2, 3, 47000m);
        HttpResponseMessage mayor = await MejorarAsync(2, 3, 48000m);
        HttpResponseMessage cero = await MejorarAsync(2, 3, 0m);
        HttpResponseMessage ajena = await MejorarAsync(2, 1, 1000m);
        HttpResponseMessage inexistente = await MejorarAsync(9999, 3, 1000m);

        await AssertBusinessErrorAsync(
            concurso,
            HttpStatusCode.Conflict,
            409,
            "Solo las subastas inversas admiten mejorar la oferta.");
        await AssertBusinessErrorAsync(
            igual,
            HttpStatusCode.BadRequest,
            400,
            "La nueva oferta debe ser menor a la actual.");
        await AssertBusinessErrorAsync(
            mayor,
            HttpStatusCode.BadRequest,
            400,
            "La nueva oferta debe ser menor a la actual.");
        await AssertBusinessErrorAsync(cero, HttpStatusCode.BadRequest, 400, "El monto debe ser mayor a 0.");
        await AssertBusinessErrorAsync(
            ajena,
            HttpStatusCode.NotFound,
            404,
            "La oferta 1 no existe en la licitación 2.");
        await AssertBusinessErrorAsync(inexistente, HttpStatusCode.NotFound, 404, "La licitación 9999 no existe.");
    }

    [Fact]
    public async Task CloseThenAwardStoresTheWinningProviderAndAmount()
    {
        HttpResponseMessage cierre = await _clienteHttp.PostAsync($"{Ruta}/1/cerrar", null);
        HttpResponseMessage adjudicacion = await _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/1/adjudicar",
            new AdjudicarLicitacionRequestDto { OfertaId = 2 });
        LicitacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, cierre.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adjudicacion.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Adjudicada", detalle.Estado);
        Assert.Equal(2, detalle.ProveedorAdjudicadoId);
        Assert.Equal(19200m, detalle.MontoAdjudicado);
        Assert.Equal(new[] { false, true }, detalle.Ofertas.Select(oferta => oferta.EsGanadora).ToArray());
    }

    [Fact]
    public async Task InvalidStateTransitionsReturnLiteralConflicts()
    {
        HttpResponseMessage cerrarAdjudicada = await _clienteHttp.PostAsync($"{Ruta}/3/cerrar", null);
        HttpResponseMessage adjudicarAbierta = await _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/1/adjudicar",
            new AdjudicarLicitacionRequestDto { OfertaId = 1 });
        HttpResponseMessage cancelarAdjudicada = await _clienteHttp.PostAsync($"{Ruta}/3/cancelar", null);
        HttpResponseMessage cancelarCancelada = await _clienteHttp.PostAsync($"{Ruta}/5/cancelar", null);
        HttpResponseMessage cerrarInexistente = await _clienteHttp.PostAsync($"{Ruta}/9999/cerrar", null);

        await AssertBusinessErrorAsync(cerrarAdjudicada, HttpStatusCode.Conflict, 409, "La licitación 3 no está abierta.");
        await AssertBusinessErrorAsync(
            adjudicarAbierta,
            HttpStatusCode.Conflict,
            409,
            "La licitación 1 debe estar cerrada para adjudicarla.");
        await AssertBusinessErrorAsync(
            cancelarAdjudicada,
            HttpStatusCode.Conflict,
            409,
            "La licitación 3 no se puede cancelar porque está Adjudicada.");
        await AssertBusinessErrorAsync(
            cancelarCancelada,
            HttpStatusCode.Conflict,
            409,
            "La licitación 5 no se puede cancelar porque está Cancelada.");
        await AssertBusinessErrorAsync(cerrarInexistente, HttpStatusCode.NotFound, 404, "La licitación 9999 no existe.");
    }

    [Fact]
    public async Task AwardWithAnOfferFromAnotherTenderReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/4/adjudicar",
            new AdjudicarLicitacionRequestDto { OfertaId = 1 });

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La oferta 1 no pertenece a la licitación 4.");
    }

    [Fact]
    public async Task CancelWorksFromOpenAndClosedStates()
    {
        HttpResponseMessage abierta = await _clienteHttp.PostAsync($"{Ruta}/1/cancelar", null);
        HttpResponseMessage cerrada = await _clienteHttp.PostAsync($"{Ruta}/4/cancelar", null);
        LicitacionDetalleDto? primera = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/1");
        LicitacionDetalleDto? segunda = await _clienteHttp.GetFromJsonAsync<LicitacionDetalleDto>($"{Ruta}/4");

        Assert.Equal(HttpStatusCode.OK, abierta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cerrada.StatusCode);
        Assert.Equal("Cancelada", primera?.Estado);
        Assert.Equal("Cancelada", segunda?.Estado);
    }

    [Fact]
    public async Task SummaryReturnsTotalsAmountsAndStateCounts()
    {
        ResumenLicitacionesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenLicitacionesDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.TotalLicitaciones);
        Assert.Equal(7, resumen.TotalOfertas);
        Assert.Equal(70000m, resumen.PresupuestoAbierto);
        Assert.Equal(28000m, resumen.MontoAdjudicado);
        Assert.Equal(
            new[] { "Abierta:2", "Cerrada:1", "Adjudicada:1", "Cancelada:1" },
            resumen.PorEstado.Select(conteo => $"{conteo.Estado}:{conteo.Total}").ToArray());
    }

    [Fact]
    public async Task SummaryWithoutTendersReturnsZerosAndAllStates()
    {
        LimpiarLicitaciones();

        ResumenLicitacionesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenLicitacionesDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.TotalLicitaciones);
        Assert.Equal(0m, resumen.PresupuestoAbierto);
        Assert.Equal(0m, resumen.MontoAdjudicado);
        Assert.Equal(4, resumen.PorEstado.Count);
    }

    [Fact]
    public async Task OpenApiContainsElevenTenderOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/ofertas").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/ofertas/{ofertaId}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/cerrar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/adjudicar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/cancelar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/resumen").TryGetProperty("get", out _));
    }

    private Task<HttpResponseMessage> PublicarLicitacionAsync(
        string numero,
        string titulo,
        string tipo,
        decimal presupuesto,
        int diasCierre,
        string? descripcion)
    {
        return _clienteHttp.PostAsJsonAsync(
            Ruta,
            new CrearLicitacionRequestDto
            {
                Numero = numero,
                Titulo = titulo,
                Descripcion = descripcion,
                Tipo = tipo,
                PresupuestoReferencial = presupuesto,
                FechaCierre = DateTime.UtcNow.AddDays(diasCierre)
            });
    }

    private async Task<int> CrearLicitacionAsync(
        string numero,
        string titulo,
        string tipo,
        decimal presupuesto,
        int diasCierre,
        string? descripcion)
    {
        HttpResponseMessage respuesta = await PublicarLicitacionAsync(
            numero,
            titulo,
            tipo,
            presupuesto,
            diasCierre,
            descripcion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        IdCreado? creado = await respuesta.Content.ReadFromJsonAsync<IdCreado>();

        Assert.NotNull(creado);
        return creado.Id;
    }

    private Task<HttpResponseMessage> ActualizarAsync(int id, string titulo, decimal presupuesto, int diasCierre)
    {
        return _clienteHttp.PutAsJsonAsync(
            $"{Ruta}/{id}",
            new ActualizarLicitacionRequestDto
            {
                Titulo = titulo,
                PresupuestoReferencial = presupuesto,
                FechaCierre = DateTime.UtcNow.AddDays(diasCierre)
            });
    }

    private Task<HttpResponseMessage> PublicarOfertaAsync(
        int licitacionId,
        int proveedorId,
        decimal monto,
        int plazo,
        string? observaciones)
    {
        return _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/{licitacionId}/ofertas",
            new CrearOfertaRequestDto
            {
                ProveedorId = proveedorId,
                Monto = monto,
                PlazoEntregaDias = plazo,
                Observaciones = observaciones
            });
    }

    private Task<HttpResponseMessage> MejorarAsync(int licitacionId, int ofertaId, decimal monto)
    {
        return _clienteHttp.PutAsJsonAsync(
            $"{Ruta}/{licitacionId}/ofertas/{ofertaId}",
            new MejorarOfertaRequestDto { Monto = monto });
    }

    private void VencerLicitacion(int id)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Licitacion licitacion = contexto.Licitaciones.Single(entidad => entidad.Id == id);
        licitacion.FechaCierre = DateTime.UtcNow.AddDays(-1);
        contexto.SaveChanges();
    }

    private void LimpiarLicitaciones()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.OfertasLicitacion.RemoveRange(contexto.OfertasLicitacion);
        contexto.Licitaciones.RemoveRange(contexto.Licitaciones);
        contexto.SaveChanges();
    }

    private void RestablecerLicitaciones()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.OfertasLicitacion.RemoveRange(contexto.OfertasLicitacion);
        contexto.Licitaciones.RemoveRange(contexto.Licitaciones);
        contexto.Proveedores.RemoveRange(contexto.Proveedores);
        contexto.Proveedores.AddRange(
            CrearProveedor(1, "Andes Suministros S.A.C.", "20600000001", "Peru", true),
            CrearProveedor(2, "Minerales del Sur S.A.C.", "20600000002", "Peru", true),
            CrearProveedor(3, "Logística Pacífico S.A.C.", "20600000003", "Peru", true),
            CrearProveedor(4, "Cobre Austral SpA", "76000000004", "Chile", true),
            CrearProveedor(5, "Servicios Andinos SpA", "76000000005", "Chile", false),
            CrearProveedor(6, "Shenzhen Parts Ltd.", "91000000006", "China", true));
        DateTime cierreLejano = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        contexto.Licitaciones.AddRange(
            CrearLicitacion(1, "LIC-2026-001", "Suministro de empaques", "ConcursoPrecios", "Abierta", 20000m, cierreLejano),
            CrearLicitacion(2, "LIC-2026-002", "Flete marítimo Callao-Shanghái", "SubastaInversa", "Abierta", 50000m, cierreLejano),
            CrearLicitacion(
                3,
                "LIC-2026-003",
                "Repuestos de maquinaria",
                "ConcursoPrecios",
                "Adjudicada",
                30000m,
                new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                1,
                28000m),
            CrearLicitacion(
                4,
                "LIC-2026-004",
                "Uniformes de planta",
                "ConcursoPrecios",
                "Cerrada",
                8000m,
                new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)),
            CrearLicitacion(
                5,
                "LIC-2026-005",
                "Servicio de limpieza",
                "ConcursoPrecios",
                "Cancelada",
                12000m,
                new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc)));
        contexto.OfertasLicitacion.AddRange(
            CrearOferta(1, 1, 1, 18500m, 15),
            CrearOferta(2, 1, 2, 19200m, 10),
            CrearOferta(3, 2, 3, 47000m, 30),
            CrearOferta(4, 2, 6, 45500m, 25),
            CrearOferta(5, 3, 1, 28000m, 20),
            CrearOferta(6, 3, 4, 29500m, 18),
            CrearOferta(7, 4, 2, 7500m, 12));
        contexto.SaveChanges();
    }

    private static Proveedor CrearProveedor(int id, string razonSocial, string ruc, string pais, bool activo)
    {
        return new Proveedor { Id = id, RazonSocial = razonSocial, Ruc = ruc, Pais = pais, Activo = activo };
    }

    private static Licitacion CrearLicitacion(
        int id,
        string numero,
        string titulo,
        string tipo,
        string estado,
        decimal presupuesto,
        DateTime cierre,
        int? proveedorAdjudicadoId = null,
        decimal? montoAdjudicado = null)
    {
        return new Licitacion
        {
            Id = id,
            Numero = numero,
            Titulo = titulo,
            Tipo = tipo,
            Estado = estado,
            PresupuestoReferencial = presupuesto,
            FechaCierre = cierre,
            ProveedorAdjudicadoId = proveedorAdjudicadoId,
            MontoAdjudicado = montoAdjudicado,
            FechaRegistro = new DateTime(2026, 10, id, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private static OfertaLicitacion CrearOferta(int id, int licitacionId, int proveedorId, decimal monto, int plazo)
    {
        return new OfertaLicitacion
        {
            Id = id,
            LicitacionId = licitacionId,
            ProveedorId = proveedorId,
            Monto = monto,
            PlazoEntregaDias = plazo,
            FechaRegistro = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc)
        };
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

    private sealed class IdCreado
    {
        public int Id { get; set; }
    }
}
