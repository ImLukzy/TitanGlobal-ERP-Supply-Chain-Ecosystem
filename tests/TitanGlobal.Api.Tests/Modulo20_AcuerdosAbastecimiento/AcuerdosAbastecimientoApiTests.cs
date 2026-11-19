using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo20_AcuerdosAbastecimiento;

[Collection("Modulo04")]
public class AcuerdosAbastecimientoApiTests
{
    private const string Ruta = "/api/v1/acuerdos-abastecimiento";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public AcuerdosAbastecimientoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerAcuerdos();
    }

    [Fact]
    public async Task ListReturnsAgreementsNewestFirstWithCommittedAndConsumedAmounts()
    {
        PaginaAcuerdosDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaAcuerdosDto>(Ruta);

        Assert.NotNull(pagina);
        Assert.Equal(4, pagina.Total);
        Assert.Equal(new[] { 4, 3, 2, 1 }, pagina.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 15000m, 11000m, 36000m, 90000m }, pagina.Items.Select(item => item.MontoComprometido).ToArray());
        Assert.Equal(new[] { 3000m, 2750m, 36000m, 20000m }, pagina.Items.Select(item => item.MontoConsumido).ToArray());
        Assert.Equal("Andes Suministros S.A.C.", pagina.Items.Single(item => item.Id == 1).ProveedorNombre);
    }

    [Fact]
    public async Task ListFiltersBySearchStateAndSupplierWithoutCaseAndPaginates()
    {
        PaginaAcuerdosDto? porProveedor = await _clienteHttp.GetFromJsonAsync<PaginaAcuerdosDto>(
            $"{Ruta}?busqueda=ANDES");
        PaginaAcuerdosDto? porNumero = await _clienteHttp.GetFromJsonAsync<PaginaAcuerdosDto>(
            $"{Ruta}?busqueda=am-2026-003");
        PaginaAcuerdosDto? suspendidos = await _clienteHttp.GetFromJsonAsync<PaginaAcuerdosDto>(
            $"{Ruta}?estado=suspendido");
        PaginaAcuerdosDto? delProveedorDos = await _clienteHttp.GetFromJsonAsync<PaginaAcuerdosDto>(
            $"{Ruta}?proveedorId=2");
        PaginaAcuerdosDto? segundaPagina = await _clienteHttp.GetFromJsonAsync<PaginaAcuerdosDto>(
            $"{Ruta}?pagina=2&tamanoPagina=2");

        Assert.Equal(new[] { 1 }, porProveedor?.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 3 }, porNumero?.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 3 }, suspendidos?.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { 2 }, delProveedorDos?.Items.Select(item => item.Id).ToArray());
        Assert.NotNull(segundaPagina);
        Assert.Equal(4, segundaPagina.Total);
        Assert.Equal(new[] { 2, 1 }, segundaPagina.Items.Select(item => item.Id).ToArray());
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?estado=Perdido", "El estado debe ser Vigente, Suspendido o Cancelado.")]
    public async Task InvalidListFiltersReturnBadRequestWithLiteralMessage(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsLinesWithBalances()
    {
        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");

        Assert.NotNull(detalle);
        Assert.Equal("AM-2026-001", detalle.Numero);
        Assert.Equal("Vigente", detalle.Estado);
        Assert.Equal(1, detalle.ProveedorId);
        Assert.Equal(90000m, detalle.MontoComprometido);
        Assert.Equal(20000m, detalle.MontoConsumido);
        Assert.Equal(new[] { 1, 2 }, detalle.Lineas.Select(linea => linea.ProductoId).ToArray());
        Assert.Equal(new[] { "Laptop Pro", "Laptop Básica" }, detalle.Lineas.Select(linea => linea.ProductoNombre).ToArray());
        Assert.Equal(new[] { 600, 500 }, detalle.Lineas.Select(linea => linea.Saldo).ToArray());
    }

    [Fact]
    public async Task DetailOfMissingAgreementReturnsNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync($"{Ruta}/9999");

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
    }

    [Fact]
    public async Task CreateStoresValidAgreementWithNormalizedFields()
    {
        int id = await CrearAcuerdoAsync("  am-2026-100  ", 6, 50000m, -5, 365, "   ");

        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/{id}");

        Assert.NotNull(detalle);
        Assert.Equal("AM-2026-100", detalle.Numero);
        Assert.Equal("Vigente", detalle.Estado);
        Assert.Equal(6, detalle.ProveedorId);
        Assert.Equal(50000m, detalle.MontoMaximo);
        Assert.Null(detalle.Descripcion);
        Assert.Empty(detalle.Lineas);
    }

    [Theory]
    [InlineData("", 1, 1000.0, -5, 365, 0, "El número es obligatorio.")]
    [InlineData("AM-123456789012345678", 1, 1000.0, -5, 365, 0, "El número no puede superar 20 caracteres.")]
    [InlineData("AM-X", 0, 1000.0, -5, 365, 0, "El proveedor es obligatorio.")]
    [InlineData("AM-X", 9999, 1000.0, -5, 365, 0, "El proveedor 9999 no existe.")]
    [InlineData("AM-X", 5, 1000.0, -5, 365, 0, "El proveedor 5 está inactivo.")]
    [InlineData("AM-X", 1, 1000.0, -5, 365, 501, "La descripción no puede superar 500 caracteres.")]
    [InlineData("AM-X", 1, 0.0, -5, 365, 0, "El monto máximo debe ser mayor a 0.")]
    [InlineData("AM-X", 1, 100.123, -5, 365, 0, "El monto máximo no es válido.")]
    [InlineData("AM-X", 1, 1000.0, 10, 5, 0, "La fecha de fin debe ser posterior a la de inicio.")]
    [InlineData("AM-X", 1, 1000.0, -10, -1, 0, "La fecha de fin debe ser futura.")]
    public async Task CreateWithInvalidDataReturnsBadRequestWithLiteralMessage(
        string numero,
        int proveedorId,
        double montoMaximo,
        int diasInicio,
        int diasFin,
        int largoDescripcion,
        string mensaje)
    {
        string? descripcion = largoDescripcion > 0 ? new string('x', largoDescripcion) : null;

        HttpResponseMessage respuesta = await PublicarAcuerdoAsync(
            numero,
            proveedorId,
            (decimal)montoMaximo,
            diasInicio,
            diasFin,
            descripcion);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task CreateWithExistingNumberReturnsConflict()
    {
        HttpResponseMessage respuesta = await PublicarAcuerdoAsync("am-2026-001", 1, 1000m, -5, 365, null);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un acuerdo con el número AM-2026-001.");
    }

    [Fact]
    public async Task UpdateChangesEditableFieldsAndKeepsNumberAndSupplier()
    {
        HttpResponseMessage respuesta = await ActualizarAsync(1, 120000m, 100, "  Acuerdo ampliado  ");
        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("AM-2026-001", detalle.Numero);
        Assert.Equal(1, detalle.ProveedorId);
        Assert.Equal(120000m, detalle.MontoMaximo);
        Assert.Equal("Acuerdo ampliado", detalle.Descripcion);
    }

    [Fact]
    public async Task UpdateOfMissingOrCancelledAgreementReturnsLiteralErrors()
    {
        HttpResponseMessage inexistente = await ActualizarAsync(9999, 1000m, 100, null);
        HttpResponseMessage cancelado = await ActualizarAsync(4, 1000m, 100, null);

        await AssertBusinessErrorAsync(inexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
        await AssertBusinessErrorAsync(cancelado, HttpStatusCode.Conflict, 409, "El acuerdo 4 está cancelado.");
    }

    [Fact]
    public async Task UpdateWithMaximumBelowCommittedLinesReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await ActualizarAsync(1, 80000m, 100, null);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El monto máximo no puede ser menor al compromiso de las líneas.");
    }

    [Fact]
    public async Task DeleteWithoutConsumptionRemovesAgreementAndLinesAndWithConsumptionReturnsConflict()
    {
        int id = await CrearAcuerdoAsync("AM-2026-101", 6, 50000m, -5, 365, null);
        HttpResponseMessage linea = await PublicarLineaAsync(id, 1, 10m, 100);
        HttpResponseMessage borrado = await _clienteHttp.DeleteAsync($"{Ruta}/{id}");
        HttpResponseMessage conConsumos = await _clienteHttp.DeleteAsync($"{Ruta}/1");
        HttpResponseMessage inexistente = await _clienteHttp.DeleteAsync($"{Ruta}/9999");
        HttpResponseMessage consulta = await _clienteHttp.GetAsync($"{Ruta}/{id}");

        Assert.Equal(HttpStatusCode.OK, linea.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
        await AssertBusinessErrorAsync(
            conConsumos,
            HttpStatusCode.Conflict,
            409,
            "El acuerdo 1 tiene consumos y no se puede eliminar; cancélelo.");
        await AssertBusinessErrorAsync(inexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
    }

    [Fact]
    public async Task AddLineIncreasesTheCommittedAmount()
    {
        HttpResponseMessage respuesta = await PublicarLineaAsync(1, 3, 100m, 50);
        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(3, detalle.Lineas.Count);
        Assert.Equal(95000m, detalle.MontoComprometido);
        Assert.Equal(50, detalle.Lineas.Single(linea => linea.ProductoId == 3).Saldo);
    }

    [Fact]
    public async Task AddLineToSuspendedAgreementIsAllowed()
    {
        HttpResponseMessage respuesta = await PublicarLineaAsync(3, 2, 10m, 10);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(0, 10.0, 10, "El producto es obligatorio.")]
    [InlineData(3, 0.0, 10, "El precio acordado debe ser mayor a 0.")]
    [InlineData(3, 10.123, 10, "El precio acordado no es válido.")]
    [InlineData(3, 10.0, 0, "La cantidad comprometida debe ser mayor a 0.")]
    [InlineData(3, 10.0, 1000001, "La cantidad comprometida no puede superar 1000000.")]
    [InlineData(9999, 10.0, 10, "El producto 9999 no existe.")]
    public async Task AddLineWithInvalidDataReturnsBadRequestWithLiteralMessage(
        int productoId,
        double precio,
        int cantidad,
        string mensaje)
    {
        HttpResponseMessage respuesta = await PublicarLineaAsync(1, productoId, (decimal)precio, cantidad);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task AddLineAboveTheMaximumAmountReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await PublicarLineaAsync(1, 3, 100m, 200);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El compromiso de las líneas supera el monto máximo del acuerdo.");
    }

    [Fact]
    public async Task AddLineWithRepeatedProductMissingOrCancelledAgreementReturnsLiteralErrors()
    {
        HttpResponseMessage repetido = await PublicarLineaAsync(1, 1, 10m, 10);
        HttpResponseMessage inexistente = await PublicarLineaAsync(9999, 3, 10m, 10);
        HttpResponseMessage cancelado = await PublicarLineaAsync(4, 3, 10m, 10);

        await AssertBusinessErrorAsync(repetido, HttpStatusCode.Conflict, 409, "El producto 1 ya está en el acuerdo 1.");
        await AssertBusinessErrorAsync(inexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
        await AssertBusinessErrorAsync(cancelado, HttpStatusCode.Conflict, 409, "El acuerdo 4 está cancelado.");
    }

    [Fact]
    public async Task DeleteLineWithoutConsumptionReturnsNoContent()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync($"{Ruta}/1/lineas/2");
        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(new[] { 1 }, detalle.Lineas.Select(linea => linea.Id).ToArray());
        Assert.Equal(50000m, detalle.MontoComprometido);
    }

    [Fact]
    public async Task DeleteLineReturnsLiteralErrors()
    {
        HttpResponseMessage conConsumo = await _clienteHttp.DeleteAsync($"{Ruta}/1/lineas/1");
        HttpResponseMessage lineaInexistente = await _clienteHttp.DeleteAsync($"{Ruta}/1/lineas/99");
        HttpResponseMessage acuerdoInexistente = await _clienteHttp.DeleteAsync($"{Ruta}/9999/lineas/1");
        HttpResponseMessage cancelado = await _clienteHttp.DeleteAsync($"{Ruta}/4/lineas/5");

        await AssertBusinessErrorAsync(
            conConsumo,
            HttpStatusCode.Conflict,
            409,
            "La línea 1 tiene consumos y no se puede eliminar.");
        await AssertBusinessErrorAsync(
            lineaInexistente,
            HttpStatusCode.NotFound,
            404,
            "La línea 99 no existe en el acuerdo 1.");
        await AssertBusinessErrorAsync(acuerdoInexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
        await AssertBusinessErrorAsync(cancelado, HttpStatusCode.Conflict, 409, "El acuerdo 4 está cancelado.");
    }

    [Fact]
    public async Task ConsumptionReducesTheLineBalance()
    {
        HttpResponseMessage respuesta = await RegistrarConsumoAsync(1, 1, 100);
        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(500, detalle.Lineas.Single(linea => linea.Id == 1).CantidadConsumida);
        Assert.Equal(500, detalle.Lineas.Single(linea => linea.Id == 1).Saldo);
        Assert.Equal(25000m, detalle.MontoConsumido);
    }

    [Fact]
    public async Task ConsumptionCanUseTheWholeBalance()
    {
        HttpResponseMessage respuesta = await RegistrarConsumoAsync(1, 1, 600);
        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(0, detalle?.Lineas.Single(linea => linea.Id == 1).Saldo);
    }

    [Fact]
    public async Task ConsumptionWithInvalidQuantityOrAboveTheBalanceReturnsLiteralErrors()
    {
        HttpResponseMessage cero = await RegistrarConsumoAsync(1, 1, 0);
        HttpResponseMessage excedido = await RegistrarConsumoAsync(1, 1, 601);
        HttpResponseMessage agotada = await RegistrarConsumoAsync(2, 3, 1);

        await AssertBusinessErrorAsync(cero, HttpStatusCode.BadRequest, 400, "La cantidad debe ser mayor a 0.");
        await AssertBusinessErrorAsync(
            excedido,
            HttpStatusCode.Conflict,
            409,
            "La cantidad supera el saldo de la línea (600).");
        await AssertBusinessErrorAsync(
            agotada,
            HttpStatusCode.Conflict,
            409,
            "La cantidad supera el saldo de la línea (0).");
    }

    [Fact]
    public async Task ConsumptionOfMissingResourcesOrNotValidAgreementsReturnsLiteralErrors()
    {
        HttpResponseMessage acuerdoInexistente = await RegistrarConsumoAsync(9999, 1, 1);
        HttpResponseMessage lineaInexistente = await RegistrarConsumoAsync(1, 99, 1);
        HttpResponseMessage suspendido = await RegistrarConsumoAsync(3, 4, 1);
        HttpResponseMessage cancelado = await RegistrarConsumoAsync(4, 5, 1);

        await AssertBusinessErrorAsync(acuerdoInexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
        await AssertBusinessErrorAsync(
            lineaInexistente,
            HttpStatusCode.NotFound,
            404,
            "La línea 99 no existe en el acuerdo 1.");
        await AssertBusinessErrorAsync(suspendido, HttpStatusCode.Conflict, 409, "El acuerdo 3 no está vigente.");
        await AssertBusinessErrorAsync(cancelado, HttpStatusCode.Conflict, 409, "El acuerdo 4 no está vigente.");
    }

    [Fact]
    public async Task ConsumptionOutsideTheValidityPeriodReturnsConflict()
    {
        CambiarVigencia(1, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(30));
        CambiarVigencia(2, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1));

        HttpResponseMessage noInicia = await RegistrarConsumoAsync(1, 1, 1);
        HttpResponseMessage vencido = await RegistrarConsumoAsync(2, 3, 1);

        await AssertBusinessErrorAsync(noInicia, HttpStatusCode.Conflict, 409, "El acuerdo 1 aún no inicia.");
        await AssertBusinessErrorAsync(vencido, HttpStatusCode.Conflict, 409, "El acuerdo 2 ya venció.");
    }

    [Fact]
    public async Task SuspendReactivateAndCancelChangeTheState()
    {
        HttpResponseMessage suspender = await _clienteHttp.PostAsync($"{Ruta}/1/suspender", null);
        AcuerdoDetalleDto? suspendido = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");
        HttpResponseMessage reactivar = await _clienteHttp.PostAsync($"{Ruta}/1/reactivar", null);
        AcuerdoDetalleDto? reactivado = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");
        HttpResponseMessage cancelar = await _clienteHttp.PostAsync($"{Ruta}/1/cancelar", null);
        AcuerdoDetalleDto? cancelado = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, suspender.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reactivar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);
        Assert.Equal("Suspendido", suspendido?.Estado);
        Assert.Equal("Vigente", reactivado?.Estado);
        Assert.Equal("Cancelado", cancelado?.Estado);
    }

    [Fact]
    public async Task CancelWorksFromSuspendedState()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync($"{Ruta}/3/cancelar", null);
        AcuerdoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<AcuerdoDetalleDto>($"{Ruta}/3");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Cancelado", detalle?.Estado);
    }

    [Fact]
    public async Task InvalidStateTransitionsReturnLiteralConflicts()
    {
        HttpResponseMessage suspenderSuspendido = await _clienteHttp.PostAsync($"{Ruta}/3/suspender", null);
        HttpResponseMessage suspenderCancelado = await _clienteHttp.PostAsync($"{Ruta}/4/suspender", null);
        HttpResponseMessage reactivarVigente = await _clienteHttp.PostAsync($"{Ruta}/1/reactivar", null);
        HttpResponseMessage cancelarCancelado = await _clienteHttp.PostAsync($"{Ruta}/4/cancelar", null);
        HttpResponseMessage suspenderInexistente = await _clienteHttp.PostAsync($"{Ruta}/9999/suspender", null);
        HttpResponseMessage reactivarInexistente = await _clienteHttp.PostAsync($"{Ruta}/9999/reactivar", null);
        HttpResponseMessage cancelarInexistente = await _clienteHttp.PostAsync($"{Ruta}/9999/cancelar", null);

        await AssertBusinessErrorAsync(suspenderSuspendido, HttpStatusCode.Conflict, 409, "El acuerdo 3 no está vigente.");
        await AssertBusinessErrorAsync(suspenderCancelado, HttpStatusCode.Conflict, 409, "El acuerdo 4 no está vigente.");
        await AssertBusinessErrorAsync(reactivarVigente, HttpStatusCode.Conflict, 409, "El acuerdo 1 no está suspendido.");
        await AssertBusinessErrorAsync(cancelarCancelado, HttpStatusCode.Conflict, 409, "El acuerdo 4 ya está cancelado.");
        await AssertBusinessErrorAsync(suspenderInexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
        await AssertBusinessErrorAsync(reactivarInexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
        await AssertBusinessErrorAsync(cancelarInexistente, HttpStatusCode.NotFound, 404, "El acuerdo 9999 no existe.");
    }

    [Fact]
    public async Task SummaryReturnsTotalsAmountsAndStateCounts()
    {
        ResumenAcuerdosDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenAcuerdosDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.TotalAcuerdos);
        Assert.Equal(160000m, resumen.MontoMaximoVigente);
        Assert.Equal(61750m, resumen.MontoConsumido);
        Assert.Equal(
            new[] { "Vigente:2", "Suspendido:1", "Cancelado:1" },
            resumen.PorEstado.Select(conteo => $"{conteo.Estado}:{conteo.Total}").ToArray());
    }

    [Fact]
    public async Task SummaryWithoutAgreementsReturnsZerosAndAllStates()
    {
        LimpiarAcuerdos();

        ResumenAcuerdosDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenAcuerdosDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.TotalAcuerdos);
        Assert.Equal(0m, resumen.MontoMaximoVigente);
        Assert.Equal(0m, resumen.MontoConsumido);
        Assert.Equal(3, resumen.PorEstado.Count);
    }

    [Fact]
    public async Task OpenApiContainsTwelveAgreementOperations()
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
        Assert.True(rutas.GetProperty(Ruta + "/{id}/lineas").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/lineas/{lineaId}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/lineas/{lineaId}/consumos").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/suspender").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/reactivar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/{id}/cancelar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(Ruta + "/resumen").TryGetProperty("get", out _));
    }

    private Task<HttpResponseMessage> PublicarAcuerdoAsync(
        string numero,
        int proveedorId,
        decimal montoMaximo,
        int diasInicio,
        int diasFin,
        string? descripcion)
    {
        return _clienteHttp.PostAsJsonAsync(
            Ruta,
            new CrearAcuerdoRequestDto
            {
                Numero = numero,
                ProveedorId = proveedorId,
                Descripcion = descripcion,
                FechaInicio = DateTime.UtcNow.AddDays(diasInicio),
                FechaFin = DateTime.UtcNow.AddDays(diasFin),
                MontoMaximo = montoMaximo
            });
    }

    private async Task<int> CrearAcuerdoAsync(
        string numero,
        int proveedorId,
        decimal montoMaximo,
        int diasInicio,
        int diasFin,
        string? descripcion)
    {
        HttpResponseMessage respuesta = await PublicarAcuerdoAsync(
            numero,
            proveedorId,
            montoMaximo,
            diasInicio,
            diasFin,
            descripcion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        IdCreado? creado = await respuesta.Content.ReadFromJsonAsync<IdCreado>();

        Assert.NotNull(creado);
        return creado.Id;
    }

    private Task<HttpResponseMessage> ActualizarAsync(int id, decimal montoMaximo, int diasFin, string? descripcion)
    {
        return _clienteHttp.PutAsJsonAsync(
            $"{Ruta}/{id}",
            new ActualizarAcuerdoRequestDto
            {
                Descripcion = descripcion,
                FechaInicio = DateTime.UtcNow.AddDays(-5),
                FechaFin = DateTime.UtcNow.AddDays(diasFin),
                MontoMaximo = montoMaximo
            });
    }

    private Task<HttpResponseMessage> PublicarLineaAsync(int acuerdoId, int productoId, decimal precio, int cantidad)
    {
        return _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/{acuerdoId}/lineas",
            new CrearLineaRequestDto
            {
                ProductoId = productoId,
                PrecioAcordado = precio,
                CantidadComprometida = cantidad
            });
    }

    private Task<HttpResponseMessage> RegistrarConsumoAsync(int acuerdoId, int lineaId, int cantidad)
    {
        return _clienteHttp.PostAsJsonAsync(
            $"{Ruta}/{acuerdoId}/lineas/{lineaId}/consumos",
            new RegistrarConsumoRequestDto { Cantidad = cantidad });
    }

    private void CambiarVigencia(int id, DateTime inicio, DateTime fin)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        AcuerdoAbastecimiento acuerdo = contexto.AcuerdosAbastecimiento.Single(entidad => entidad.Id == id);
        acuerdo.FechaInicio = inicio;
        acuerdo.FechaFin = fin;
        contexto.SaveChanges();
    }

    private void LimpiarAcuerdos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.LineasAcuerdoAbastecimiento.RemoveRange(contexto.LineasAcuerdoAbastecimiento);
        contexto.AcuerdosAbastecimiento.RemoveRange(contexto.AcuerdosAbastecimiento);
        contexto.SaveChanges();
    }

    private void RestablecerAcuerdos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.LineasAcuerdoAbastecimiento.RemoveRange(contexto.LineasAcuerdoAbastecimiento);
        contexto.AcuerdosAbastecimiento.RemoveRange(contexto.AcuerdosAbastecimiento);
        contexto.Proveedores.RemoveRange(contexto.Proveedores);
        contexto.Proveedores.AddRange(
            CrearProveedor(1, "Andes Suministros S.A.C.", "20600000001", "Peru", true),
            CrearProveedor(2, "Minerales del Sur S.A.C.", "20600000002", "Peru", true),
            CrearProveedor(3, "Logística Pacífico S.A.C.", "20600000003", "Peru", true),
            CrearProveedor(4, "Cobre Austral SpA", "76000000004", "Chile", true),
            CrearProveedor(5, "Servicios Andinos SpA", "76000000005", "Chile", false),
            CrearProveedor(6, "Shenzhen Parts Ltd.", "91000000006", "China", true));
        DateTime finLargo = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        contexto.AcuerdosAbastecimiento.AddRange(
            CrearAcuerdo(1, "AM-2026-001", 1, "Vigente", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), finLargo, 100000m),
            CrearAcuerdo(2, "AM-2026-002", 2, "Vigente", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), finLargo, 60000m),
            CrearAcuerdo(3, "AM-2026-003", 3, "Suspendido", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), finLargo, 40000m),
            CrearAcuerdo(
                4,
                "AM-2026-004",
                4,
                "Cancelado",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                25000m));
        contexto.LineasAcuerdoAbastecimiento.AddRange(
            CrearLinea(1, 1, 1, 50m, 1000, 400),
            CrearLinea(2, 1, 2, 80m, 500, 0),
            CrearLinea(3, 2, 3, 120m, 300, 300),
            CrearLinea(4, 3, 1, 55m, 200, 50),
            CrearLinea(5, 4, 4, 30m, 500, 100));
        contexto.SaveChanges();
    }

    private static Proveedor CrearProveedor(int id, string razonSocial, string ruc, string pais, bool activo)
    {
        return new Proveedor { Id = id, RazonSocial = razonSocial, Ruc = ruc, Pais = pais, Activo = activo };
    }

    private static AcuerdoAbastecimiento CrearAcuerdo(
        int id,
        string numero,
        int proveedorId,
        string estado,
        DateTime inicio,
        DateTime fin,
        decimal montoMaximo)
    {
        return new AcuerdoAbastecimiento
        {
            Id = id,
            Numero = numero,
            ProveedorId = proveedorId,
            Estado = estado,
            FechaInicio = inicio,
            FechaFin = fin,
            MontoMaximo = montoMaximo,
            FechaRegistro = new DateTime(2026, 10, id, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private static LineaAcuerdoAbastecimiento CrearLinea(
        int id,
        int acuerdoId,
        int productoId,
        decimal precio,
        int comprometida,
        int consumida)
    {
        return new LineaAcuerdoAbastecimiento
        {
            Id = id,
            AcuerdoId = acuerdoId,
            ProductoId = productoId,
            PrecioAcordado = precio,
            CantidadComprometida = comprometida,
            CantidadConsumida = consumida
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
