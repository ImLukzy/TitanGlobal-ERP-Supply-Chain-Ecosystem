using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo75_ImpuestosTasas;

[Collection("Modulo04")]
public class ImpuestosApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ImpuestosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerImpuestos();
    }

    [Fact]
    public async Task ListReturnsEveryTaxWithItsCurrentRate()
    {
        List<ImpuestoListadoDto>? impuestos = await _clienteHttp.GetFromJsonAsync<List<ImpuestoListadoDto>>(
            "/api/v1/impuestos");

        Assert.NotNull(impuestos);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, impuestos.Select(impuesto => impuesto.Id).OrderBy(id => id).ToArray());
        ImpuestoListadoDto igv = impuestos.Single(impuesto => impuesto.Id == 1);
        Assert.Equal("IGV", igv.Codigo);
        Assert.Equal(18m, igv.PorcentajeVigente);
        Assert.Equal(new DateTime(2020, 1, 1), igv.FechaInicioVigencia?.Date);
        ImpuestoListadoDto retencion = impuestos.Single(impuesto => impuesto.Id == 2);
        Assert.Equal(4m, retencion.PorcentajeVigente);
        ImpuestoListadoDto sinTasas = impuestos.Single(impuesto => impuesto.Id == 3);
        Assert.Null(sinTasas.PorcentajeVigente);
        Assert.Null(sinTasas.FechaInicioVigencia);
        Assert.False(impuestos.Single(impuesto => impuesto.Id == 4).Activo);
    }

    [Fact]
    public async Task DetailReturnsTaxWithRatesFromNewestToOldest()
    {
        ImpuestoDetalleDto? impuesto = await _clienteHttp.GetFromJsonAsync<ImpuestoDetalleDto>(
            "/api/v1/impuestos/2");

        Assert.NotNull(impuesto);
        Assert.Equal("RET-4TA", impuesto.Codigo);
        Assert.Equal("Retención de cuarta categoría", impuesto.Nombre);
        Assert.Equal("RETENCION", impuesto.Tipo);
        Assert.Equal("PER", impuesto.Pais);
        Assert.Equal("Retención sobre honorarios", impuesto.Descripcion);
        Assert.True(impuesto.Activo);
        Assert.Equal(new[] { 4m, 3m }, impuesto.Tasas.Select(tasa => tasa.Porcentaje).ToArray());
        Assert.Null(impuesto.Tasas[0].FechaFin);
        Assert.Equal(new DateTime(2020, 12, 31), impuesto.Tasas[1].FechaFin?.Date);
    }

    [Fact]
    public async Task DetailOfMissingTaxReturnsNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/api/v1/impuestos/9999");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "No se encontró el impuesto solicitado.");
    }

    [Fact]
    public async Task CreateNormalizesFieldsAndReturnsCreatedWithLocation()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud("  isc-1 ", "  Impuesto selectivo  ", "igv");
        solicitud.Pais = " per ";
        solicitud.Descripcion = "Selectivo al consumo";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/impuestos", solicitud);
        int id = await LeerIdAsync(respuesta);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal($"/api/v1/impuestos/{id}", respuesta.Headers.Location?.OriginalString);
        Impuesto impuesto = await ObtenerImpuestoAsync(id);
        Assert.Equal("ISC-1", impuesto.Codigo);
        Assert.Equal("Impuesto selectivo", impuesto.Nombre);
        Assert.Equal("IGV", impuesto.Tipo);
        Assert.Equal("PER", impuesto.Pais);
        Assert.Equal("Selectivo al consumo", impuesto.Descripcion);
        Assert.True(impuesto.Activo);
    }

    [Theory]
    [InlineData("", "Nombre", "IGV", "PER", "El código del impuesto es obligatorio.")]
    [InlineData("   ", "Nombre", "IGV", "PER", "El código del impuesto es obligatorio.")]
    [InlineData("COD", "", "IGV", "PER", "El nombre del impuesto es obligatorio.")]
    [InlineData("COD", "   ", "IGV", "PER", "El nombre del impuesto es obligatorio.")]
    [InlineData("COD", "Nombre", "OTRO", "PER", "El tipo de impuesto no es válido.")]
    [InlineData("COD", "Nombre", "", "PER", "El tipo de impuesto no es válido.")]
    [InlineData("COD", "Nombre", "IGV", "PE", "El país debe indicarse mediante un código de tres caracteres.")]
    [InlineData("COD", "Nombre", "IGV", "PERU", "El país debe indicarse mediante un código de tres caracteres.")]
    public async Task CreateWithInvalidFieldsReturnsBadRequest(
        string codigo,
        string nombre,
        string tipo,
        string pais,
        string mensaje)
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud(codigo, nombre, tipo);
        solicitud.Pais = pais;
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/impuestos", solicitud);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task CreateWithCodeOfThirtyCharactersIsAccepted()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud(new string('A', 30), "Código largo", "IVA");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/impuestos", solicitud);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task CreateWithCodeOfThirtyOneCharactersReturnsBadRequest()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud(new string('A', 31), "Código largo", "IVA");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/impuestos", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El código del impuesto no puede superar 30 caracteres.");
    }

    [Fact]
    public async Task CreateWithOverlongNameReturnsBadRequest()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud("COD", new string('N', 121), "IVA");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/impuestos", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre del impuesto no puede superar 120 caracteres.");
    }

    [Fact]
    public async Task CreateWithOverlongDescriptionReturnsBadRequest()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud("COD", "Nombre", "IVA");
        solicitud.Descripcion = new string('D', 251);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/impuestos", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La descripción del impuesto no puede superar 250 caracteres.");
    }

    [Fact]
    public async Task CreateWithDuplicateCodeReturnsConflict()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud(" igv ", "Otro IGV", "IGV");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/impuestos", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un impuesto con el código IGV.");
    }

    [Fact]
    public async Task CreateTranslatesUniqueViolationRaisedWhileSavingToConflict()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        CrearImpuestoUseCase casoDeUso = new CrearImpuestoUseCase(
            new UnitOfWorkConUnicidadViolada(unitOfWork, TipoViolacionRestriccion.Unicidad));

        ReglaNegocioException excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => casoDeUso.Ejecutar(CrearSolicitud("carrera", "Carrera", "IVA")));

        Assert.Equal(409, excepcion.StatusCode);
        Assert.Equal("Ya existe un impuesto con el código CARRERA.", excepcion.Message);
    }

    [Fact]
    public async Task CreateDoesNotTranslateForeignKeyViolationToConflict()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        CrearImpuestoUseCase casoDeUso = new CrearImpuestoUseCase(
            new UnitOfWorkConUnicidadViolada(unitOfWork, TipoViolacionRestriccion.ReferenciaForanea));

        await Assert.ThrowsAsync<ViolacionRestriccionException>(
            () => casoDeUso.Ejecutar(CrearSolicitud("carrera", "Carrera", "IVA")));
    }

    [Fact]
    public async Task UpdateChangesFieldsAndMayKeepItsOwnCode()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud("igv", " IGV actualizado ", "IGV");
        solicitud.Descripcion = "Nueva descripción";
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/impuestos/1", solicitud);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Impuesto impuesto = await ObtenerImpuestoAsync(1);
        Assert.Equal("IGV", impuesto.Codigo);
        Assert.Equal("IGV actualizado", impuesto.Nombre);
        Assert.Equal("Nueva descripción", impuesto.Descripcion);
    }

    [Fact]
    public async Task UpdateWithCodeOfAnotherTaxReturnsConflict()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud("ret-4ta", "IGV", "IGV");
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/impuestos/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un impuesto con el código RET-4TA.");
    }

    [Fact]
    public async Task UpdateTranslatesUniqueViolationRaisedWhileSavingToConflict()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        ActualizarImpuestoUseCase casoDeUso = new ActualizarImpuestoUseCase(
            new UnitOfWorkConUnicidadViolada(unitOfWork, TipoViolacionRestriccion.Unicidad));

        ReglaNegocioException excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => casoDeUso.Ejecutar(1, CrearSolicitud("igv", "IGV", "IGV")));

        Assert.Equal(409, excepcion.StatusCode);
        Assert.Equal("Ya existe un impuesto con el código IGV.", excepcion.Message);
    }

    [Fact]
    public async Task UpdateWithOverlongCodeReturnsBadRequest()
    {
        GuardarImpuestoRequestDto solicitud = CrearSolicitud(new string('A', 31), "IGV", "IGV");
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/impuestos/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El código del impuesto no puede superar 30 caracteres.");
    }

    [Fact]
    public async Task UpdateOfMissingTaxReturnsNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            "/api/v1/impuestos/9999",
            CrearSolicitud("nuevo", "Nuevo", "IGV"));

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "No se encontró el impuesto solicitado.");
    }

    [Fact]
    public async Task DeactivateKeepsTheTaxAndItsRatesButBlocksCalculationAndNewRates()
    {
        HttpResponseMessage desactivar = await _clienteHttp.DeleteAsync("/api/v1/impuestos/1");

        Assert.Equal(HttpStatusCode.NoContent, desactivar.StatusCode);
        ImpuestoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<ImpuestoDetalleDto>("/api/v1/impuestos/1");
        Assert.NotNull(detalle);
        Assert.False(detalle.Activo);
        Assert.Single(detalle.Tasas);

        HttpResponseMessage calculo = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":1,\"baseImponible\":100,\"fechaOperacion\":\"2026-01-01T00:00:00Z\"}");
        await AssertBusinessErrorAsync(
            calculo,
            HttpStatusCode.NotFound,
            404,
            "El impuesto no existe o está inactivo.");

        HttpResponseMessage tasa = await EnviarJsonAsync(
            "/api/v1/impuestos/1/tasas",
            "{\"porcentaje\":10,\"fechaInicio\":\"2030-01-01T00:00:00Z\"}");
        await AssertBusinessErrorAsync(
            tasa,
            HttpStatusCode.NotFound,
            404,
            "El impuesto no existe o está inactivo.");
    }

    [Fact]
    public async Task DeactivateOfMissingTaxReturnsNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/impuestos/9999");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "No se encontró el impuesto solicitado.");
    }

    [Fact]
    public async Task RegisterRateWithoutEndDateAndUtcDatesStoresUtcDates()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/3/tasas",
            "{\"porcentaje\":2,\"fechaInicio\":\"2027-01-01T00:00:00Z\",\"observacion\":\"Percepción general\"}");
        int id = await LeerIdAsync(respuesta);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        TasaImpuesto tasa = await ObtenerTasaAsync(id);
        Assert.Equal(3, tasa.ImpuestoId);
        Assert.Equal(2m, tasa.Porcentaje);
        Assert.Equal(new DateTime(2027, 1, 1), tasa.FechaInicio);
        Assert.Equal(DateTimeKind.Utc, tasa.FechaInicio.Kind);
        Assert.Null(tasa.FechaFin);
        Assert.True(tasa.Activo);
        Assert.Equal("Percepción general", tasa.Observacion);
    }

    [Fact]
    public async Task RegisterRateWithDatesWithoutZoneStoresUtcDates()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/3/tasas",
            "{\"porcentaje\":2,\"fechaInicio\":\"2027-01-01\",\"fechaFin\":\"2027-06-30\"}");
        int id = await LeerIdAsync(respuesta);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        TasaImpuesto tasa = await ObtenerTasaAsync(id);
        Assert.Equal(new DateTime(2027, 1, 1), tasa.FechaInicio);
        Assert.Equal(DateTimeKind.Utc, tasa.FechaInicio.Kind);
        Assert.Equal(new DateTime(2027, 6, 30), tasa.FechaFin);
        Assert.Equal(DateTimeKind.Utc, tasa.FechaFin?.Kind);
    }

    [Fact]
    public async Task RegisterRateKeepsTheCalendarDayAndDropsTheTime()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/3/tasas",
            "{\"porcentaje\":2,\"fechaInicio\":\"2027-03-15T23:45:00Z\",\"fechaFin\":\"2027-03-20T01:30:00Z\"}");
        int id = await LeerIdAsync(respuesta);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        TasaImpuesto tasa = await ObtenerTasaAsync(id);
        Assert.Equal(new DateTime(2027, 3, 15), tasa.FechaInicio);
        Assert.Equal(DateTimeKind.Utc, tasa.FechaInicio.Kind);
        Assert.Equal(new DateTime(2027, 3, 20), tasa.FechaFin);
        Assert.Equal(DateTimeKind.Utc, tasa.FechaFin?.Kind);
    }

    [Fact]
    public async Task RegisterRateOverlappingAnOpenRateReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/1/tasas",
            "{\"porcentaje\":10,\"fechaInicio\":\"2030-01-01T00:00:00Z\"}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "Ya existe una tasa activa para ese periodo.");
    }

    [Fact]
    public async Task RegisterRateOverlappingBeforeAnOpenRateReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/1/tasas",
            "{\"porcentaje\":10,\"fechaInicio\":\"2019-01-01T00:00:00Z\",\"fechaFin\":\"2020-01-01T00:00:00Z\"}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "Ya existe una tasa activa para ese periodo.");
    }

    [Fact]
    public async Task RegisterRateComparesOverlapByCalendarDay()
    {
        HttpResponseMessage solapada = await EnviarJsonAsync(
            "/api/v1/impuestos/5/tasas",
            "{\"porcentaje\":12,\"fechaInicio\":\"2027-01-20T18:00:00Z\"}");
        HttpResponseMessage contigua = await EnviarJsonAsync(
            "/api/v1/impuestos/5/tasas",
            "{\"porcentaje\":12,\"fechaInicio\":\"2027-01-21T18:00:00Z\"}");

        await AssertBusinessErrorAsync(
            solapada,
            HttpStatusCode.BadRequest,
            400,
            "Ya existe una tasa activa para ese periodo.");
        Assert.Equal(HttpStatusCode.Created, contigua.StatusCode);
    }

    [Fact]
    public async Task RegisterRateIgnoresInactiveRatesWhenCheckingOverlap()
    {
        using (IServiceScope scope = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            TasaImpuesto tasa = await contexto.TasasImpuestos.SingleAsync(entidad => entidad.ImpuestoId == 5);
            tasa.Activo = false;
            await contexto.SaveChangesAsync();
        }

        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/5/tasas",
            "{\"porcentaje\":12,\"fechaInicio\":\"2027-01-15T00:00:00Z\"}");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("{\"porcentaje\":0,\"fechaInicio\":\"2027-01-01\"}", "El porcentaje debe ser mayor que 0 y no superar 100.")]
    [InlineData("{\"porcentaje\":-1,\"fechaInicio\":\"2027-01-01\"}", "El porcentaje debe ser mayor que 0 y no superar 100.")]
    [InlineData("{\"porcentaje\":100.01,\"fechaInicio\":\"2027-01-01\"}", "El porcentaje debe ser mayor que 0 y no superar 100.")]
    [InlineData("{\"porcentaje\":5,\"fechaInicio\":\"2027-02-01\",\"fechaFin\":\"2027-01-31\"}", "La fecha final no puede ser anterior a la fecha inicial.")]
    [InlineData("{\"porcentaje\":5}", "La fecha de inicio es obligatoria.")]
    public async Task RegisterRateWithInvalidDataReturnsBadRequest(string cuerpo, string mensaje)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync("/api/v1/impuestos/3/tasas", cuerpo);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task RegisterRateWithOverlongObservationReturnsBadRequest()
    {
        string cuerpo = JsonSerializer.Serialize(new
        {
            porcentaje = 5,
            fechaInicio = "2027-01-01",
            observacion = new string('O', 251)
        });
        HttpResponseMessage respuesta = await EnviarJsonAsync("/api/v1/impuestos/3/tasas", cuerpo);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La observación de la tasa no puede superar 250 caracteres.");
    }

    [Theory]
    [InlineData(9999)]
    [InlineData(4)]
    public async Task RegisterRateForMissingOrInactiveTaxReturnsNotFound(int impuestoId)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            $"/api/v1/impuestos/{impuestoId}/tasas",
            "{\"porcentaje\":5,\"fechaInicio\":\"2027-01-01\"}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "El impuesto no existe o está inactivo.");
    }

    [Fact]
    public async Task CalculateRoundsBaseToTwoDecimalsSoTotalHasNoMore()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":1,\"baseImponible\":100.125,\"fechaOperacion\":\"2026-05-10T00:00:00Z\"}");
        CalcularImpuestoResponseDto? resultado = await respuesta.Content.ReadFromJsonAsync<CalcularImpuestoResponseDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.ImpuestoId);
        Assert.Equal("IGV", resultado.Codigo);
        Assert.Equal("Impuesto general a las ventas", resultado.Nombre);
        Assert.Equal(100.13m, resultado.BaseImponible);
        Assert.Equal(18m, resultado.PorcentajeAplicado);
        Assert.Equal(18.02m, resultado.ImporteImpuesto);
        Assert.Equal(118.15m, resultado.Total);
        Assert.Equal(new DateTime(2026, 5, 10), resultado.FechaOperacion);
        Assert.Equal(DateTimeKind.Utc, resultado.FechaOperacion.Kind);
    }

    [Fact]
    public async Task CalculateRoundsMidpointsAwayFromZero()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":1,\"baseImponible\":0.005,\"fechaOperacion\":\"2026-05-10T00:00:00Z\"}");
        CalcularImpuestoResponseDto? resultado = await respuesta.Content.ReadFromJsonAsync<CalcularImpuestoResponseDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(0.01m, resultado.BaseImponible);
        Assert.Equal(0m, resultado.ImporteImpuesto);
        Assert.Equal(0.01m, resultado.Total);
    }

    [Fact]
    public async Task CalculateAcceptsDateWithoutZoneAndKeepsTheDay()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":2,\"baseImponible\":1000,\"fechaOperacion\":\"2020-06-15\"}");
        CalcularImpuestoResponseDto? resultado = await respuesta.Content.ReadFromJsonAsync<CalcularImpuestoResponseDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(3m, resultado.PorcentajeAplicado);
        Assert.Equal(30m, resultado.ImporteImpuesto);
        Assert.Equal(1030m, resultado.Total);
        Assert.Equal(new DateTime(2020, 6, 15), resultado.FechaOperacion);
        Assert.Equal(DateTimeKind.Utc, resultado.FechaOperacion.Kind);
    }

    [Theory]
    [InlineData("2020-12-31", 3)]
    [InlineData("2021-01-01", 4)]
    [InlineData("2021-01-01T23:59:59Z", 4)]
    public async Task CalculatePicksTheRateInForceOnTheOperationDay(string fecha, int porcentaje)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            $"{{\"impuestoId\":2,\"baseImponible\":100,\"fechaOperacion\":\"{fecha}\"}}");
        CalcularImpuestoResponseDto? resultado = await respuesta.Content.ReadFromJsonAsync<CalcularImpuestoResponseDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(porcentaje, resultado.PorcentajeAplicado);
    }

    [Fact]
    public async Task CalculateWithoutRateInForceReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":1,\"baseImponible\":100,\"fechaOperacion\":\"2019-12-31T00:00:00Z\"}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "No existe una tasa vigente para la fecha indicada.");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("0.004")]
    public async Task CalculateWithBaseThatIsNotPositiveReturnsBadRequest(string baseImponible)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            $"{{\"impuestoId\":1,\"baseImponible\":{baseImponible},\"fechaOperacion\":\"2026-05-10\"}}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La base imponible debe ser mayor que cero.");
    }

    [Fact]
    public async Task CalculateWithBaseThatOverflowsDecimalReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":1,\"baseImponible\":79228162514264337593543950335,\"fechaOperacion\":\"2026-05-10\"}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La base imponible es demasiado grande.");
    }

    [Fact]
    public async Task CalculateWithoutOperationDateReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":1,\"baseImponible\":100}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La fecha de operación es obligatoria.");
    }

    [Fact]
    public async Task CalculateAndRegisterRateWithNullDatesAreRejectedAsBadRequest()
    {
        HttpResponseMessage calculo = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            "{\"impuestoId\":1,\"baseImponible\":100,\"fechaOperacion\":null}");
        HttpResponseMessage tasa = await EnviarJsonAsync(
            "/api/v1/impuestos/3/tasas",
            "{\"porcentaje\":5,\"fechaInicio\":null}");

        Assert.Equal(HttpStatusCode.BadRequest, calculo.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tasa.StatusCode);
    }

    [Theory]
    [InlineData(9999)]
    [InlineData(4)]
    public async Task CalculateForMissingOrInactiveTaxReturnsNotFound(int impuestoId)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            "/api/v1/impuestos/calcular",
            $"{{\"impuestoId\":{impuestoId},\"baseImponible\":100,\"fechaOperacion\":\"2026-05-10\"}}");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "El impuesto no existe o está inactivo.");
    }

    [Fact]
    public async Task OpenApiContainsTheTaxOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/impuestos").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/impuestos").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/impuestos/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/impuestos/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/impuestos/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/impuestos/{id}/tasas").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/impuestos/calcular").TryGetProperty("post", out _));
    }

    private void RestablecerImpuestos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.TasasImpuestos.RemoveRange(contexto.TasasImpuestos);
        contexto.Impuestos.RemoveRange(contexto.Impuestos);
        contexto.SaveChanges();

        contexto.Impuestos.AddRange(
            new Impuesto
            {
                Id = 1,
                Codigo = "IGV",
                Nombre = "Impuesto general a las ventas",
                Tipo = "IGV",
                Pais = "PER",
                Activo = true
            },
            new Impuesto
            {
                Id = 2,
                Codigo = "RET-4TA",
                Nombre = "Retención de cuarta categoría",
                Tipo = "RETENCION",
                Descripcion = "Retención sobre honorarios",
                Pais = "PER",
                Activo = true
            },
            new Impuesto
            {
                Id = 3,
                Codigo = "PER-GEN",
                Nombre = "Percepción general",
                Tipo = "PERCEPCION",
                Pais = "PER",
                Activo = true
            },
            new Impuesto
            {
                Id = 4,
                Codigo = "DET-INACTIVA",
                Nombre = "Detracción inactiva",
                Tipo = "DETRACCION",
                Pais = "PER",
                Activo = false
            },
            new Impuesto
            {
                Id = 5,
                Codigo = "IVA-TEST",
                Nombre = "IVA de prueba",
                Tipo = "IVA",
                Pais = "CHL",
                Activo = true
            });
        contexto.TasasImpuestos.AddRange(
            CrearTasa(1, 1, 18m, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), null),
            CrearTasa(2, 2, 3m, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
            CrearTasa(3, 2, 4m, new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), null),
            CrearTasa(4, 5, 10m, new DateTime(2027, 1, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 20, 0, 0, 0, DateTimeKind.Utc)));
        contexto.SaveChanges();
    }

    private static TasaImpuesto CrearTasa(int id, int impuestoId, decimal porcentaje, DateTime inicio, DateTime? fin)
    {
        return new TasaImpuesto
        {
            Id = id,
            ImpuestoId = impuestoId,
            Porcentaje = porcentaje,
            FechaInicio = inicio,
            FechaFin = fin,
            Activo = true
        };
    }

    private static GuardarImpuestoRequestDto CrearSolicitud(string codigo, string nombre, string tipo)
    {
        return new GuardarImpuestoRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Pais = "PER"
        };
    }

    private async Task<HttpResponseMessage> EnviarJsonAsync(string ruta, string cuerpo)
    {
        StringContent contenido = new StringContent(cuerpo, Encoding.UTF8, "application/json");

        return await _clienteHttp.PostAsync(ruta, contenido);
    }

    private static async Task<int> LeerIdAsync(HttpResponseMessage respuesta)
    {
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.NotNull(resultado);

        return resultado["id"];
    }

    private async Task<Impuesto> ObtenerImpuestoAsync(int id)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        return await contexto.Impuestos.AsNoTracking().SingleAsync(entidad => entidad.Id == id);
    }

    private async Task<TasaImpuesto> ObtenerTasaAsync(int id)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        return await contexto.TasasImpuestos.AsNoTracking().SingleAsync(entidad => entidad.Id == id);
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

    private sealed class UnitOfWorkConUnicidadViolada : IUnitOfWork
    {
        private readonly IUnitOfWork _interno;

        private readonly TipoViolacionRestriccion _tipo;

        public UnitOfWorkConUnicidadViolada(IUnitOfWork interno, TipoViolacionRestriccion tipo)
        {
            _interno = interno;
            _tipo = tipo;
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            return _interno.Repository<T>();
        }

        public Task<int> CompleteAsync()
        {
            throw new ViolacionRestriccionException(
                _tipo,
                "IX_Impuestos_Codigo",
                "restricción violada",
                new InvalidOperationException("origen"));
        }

        public void Dispose()
        {
            _interno.Dispose();
        }
    }
}
