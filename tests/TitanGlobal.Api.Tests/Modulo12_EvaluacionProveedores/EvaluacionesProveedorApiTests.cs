using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo12_EvaluacionProveedores;

[Collection("Modulo04")]
public class EvaluacionesProveedorApiTests
{
    private const string Ruta = "/api/v1/evaluaciones-proveedores";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public EvaluacionesProveedorApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerEvaluaciones();
    }

    [Fact]
    public async Task ListReturnsAllEvaluationsOrderedByPeriodDescendingThenProvider()
    {
        PaginaEvaluacionesDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaEvaluacionesDto>(Ruta);

        Assert.NotNull(pagina);
        Assert.Equal(6, pagina.Total);
        Assert.Equal(new[] { 2, 3, 5, 6, 1, 4 }, pagina.Items.Select(item => item.Id).ToArray());
        Assert.Equal("Andes Suministros S.A.C.", pagina.Items[0].ProveedorNombre);
    }

    [Fact]
    public async Task ListFiltersByProviderPeriodAndRatingWithoutCase()
    {
        PaginaEvaluacionesDto? porProveedor = await _clienteHttp.GetFromJsonAsync<PaginaEvaluacionesDto>(
            $"{Ruta}?proveedorId=1");
        PaginaEvaluacionesDto? porPeriodo = await _clienteHttp.GetFromJsonAsync<PaginaEvaluacionesDto>(
            $"{Ruta}?periodo=2026-09");
        PaginaEvaluacionesDto? porCalificacion = await _clienteHttp.GetFromJsonAsync<PaginaEvaluacionesDto>(
            $"{Ruta}?calificacion=excelente");
        PaginaEvaluacionesDto? segundaPagina = await _clienteHttp.GetFromJsonAsync<PaginaEvaluacionesDto>(
            $"{Ruta}?pagina=2&tamanoPagina=4");

        Assert.NotNull(porProveedor);
        Assert.Equal(new[] { 2, 1 }, porProveedor.Items.Select(item => item.Id).ToArray());
        Assert.NotNull(porPeriodo);
        Assert.Equal(new[] { 1, 4 }, porPeriodo.Items.Select(item => item.Id).ToArray());
        Assert.NotNull(porCalificacion);
        Assert.Equal(new[] { 2, 6 }, porCalificacion.Items.Select(item => item.Id).ToArray());
        Assert.NotNull(segundaPagina);
        Assert.Equal(6, segundaPagina.Total);
        Assert.Equal(new[] { 1, 4 }, segundaPagina.Items.Select(item => item.Id).ToArray());
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?periodo=2026-13", "El periodo debe tener el formato AAAA-MM.")]
    [InlineData("?calificacion=Perfecto", "La calificación debe ser Excelente, Bueno, Regular o Deficiente.")]
    public async Task InvalidListFiltersReturnBadRequestWithLiteralMessage(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsScoresAndObservations()
    {
        EvaluacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<EvaluacionDetalleDto>($"{Ruta}/2");

        Assert.NotNull(detalle);
        Assert.Equal(1, detalle.ProveedorId);
        Assert.Equal("2026-10", detalle.Periodo);
        Assert.Equal(95, detalle.PuntajeCumplimiento);
        Assert.Equal(92, detalle.PuntajeCalidad);
        Assert.Equal(90, detalle.PuntajePlazos);
        Assert.Equal(92.60m, detalle.PuntajeFinal);
        Assert.Equal("Excelente", detalle.Calificacion);
    }

    [Fact]
    public async Task DetailOfMissingEvaluationReturnsNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync($"{Ruta}/9999");

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "La evaluación 9999 no existe.");
    }

    [Theory]
    [InlineData(100, 100, 100, 100.00, "Excelente")]
    [InlineData(90, 90, 90, 90.00, "Excelente")]
    [InlineData(89, 89, 89, 89.00, "Bueno")]
    [InlineData(75, 75, 75, 75.00, "Bueno")]
    [InlineData(74, 74, 74, 74.00, "Regular")]
    [InlineData(60, 60, 60, 60.00, "Regular")]
    [InlineData(59, 59, 59, 59.00, "Deficiente")]
    [InlineData(1, 1, 0, 0.70, "Deficiente")]
    [InlineData(100, 0, 0, 40.00, "Deficiente")]
    [InlineData(0, 0, 0, 0.00, "Deficiente")]
    public async Task CreateCalculatesFinalScoreAndRating(
        int cumplimiento,
        int calidad,
        int plazos,
        double esperado,
        string calificacion)
    {
        int id = await CrearAsync(3, "2026-01", cumplimiento, calidad, plazos);

        EvaluacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<EvaluacionDetalleDto>($"{Ruta}/{id}");

        Assert.NotNull(detalle);
        Assert.Equal((decimal)esperado, detalle.PuntajeFinal);
        Assert.Equal(calificacion, detalle.Calificacion);
    }

    [Fact]
    public async Task CreateTrimsObservationsAndStoresBlankAsNull()
    {
        int conTexto = await CrearAsync(3, "2026-02", 80, 80, 80, "  Entrega a tiempo  ");
        int vacia = await CrearAsync(3, "2026-03", 80, 80, 80, "   ");

        EvaluacionDetalleDto? primera = await _clienteHttp.GetFromJsonAsync<EvaluacionDetalleDto>($"{Ruta}/{conTexto}");
        EvaluacionDetalleDto? segunda = await _clienteHttp.GetFromJsonAsync<EvaluacionDetalleDto>($"{Ruta}/{vacia}");

        Assert.Equal("Entrega a tiempo", primera?.Observaciones);
        Assert.Null(segunda?.Observaciones);
    }

    [Fact]
    public async Task CreateAcceptsInactiveProvider()
    {
        int id = await CrearAsync(5, "2026-04", 70, 70, 70);

        Assert.True(id > 0);
    }

    [Theory]
    [InlineData(0, "2026-01", 80, 80, 80, "El proveedor es obligatorio.")]
    [InlineData(1, "2026-1", 80, 80, 80, "El periodo debe tener el formato AAAA-MM.")]
    [InlineData(1, "2026-13", 80, 80, 80, "El periodo debe tener el formato AAAA-MM.")]
    [InlineData(1, "1999-12", 80, 80, 80, "El periodo debe tener el formato AAAA-MM.")]
    [InlineData(1, "", 80, 80, 80, "El periodo debe tener el formato AAAA-MM.")]
    [InlineData(1, "2026-01", 101, 80, 80, "El puntaje de cumplimiento debe estar entre 0 y 100.")]
    [InlineData(1, "2026-01", 80, -1, 80, "El puntaje de calidad debe estar entre 0 y 100.")]
    [InlineData(1, "2026-01", 80, 80, 101, "El puntaje de plazos debe estar entre 0 y 100.")]
    [InlineData(999, "2026-01", 80, 80, 80, "El proveedor 999 no existe.")]
    public async Task CreateWithInvalidDataReturnsBadRequestWithLiteralMessage(
        int proveedorId,
        string periodo,
        int cumplimiento,
        int calidad,
        int plazos,
        string mensaje)
    {
        HttpResponseMessage respuesta = await PublicarAsync(proveedorId, periodo, cumplimiento, calidad, plazos);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task CreateWithFuturePeriodReturnsBadRequest()
    {
        string futuro = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM", CultureInfo.InvariantCulture);

        HttpResponseMessage respuesta = await PublicarAsync(1, futuro, 80, 80, 80);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, "El periodo no puede ser futuro.");
    }

    [Fact]
    public async Task CreateWithLongObservationsReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await PublicarAsync(1, "2026-05", 80, 80, 80, new string('x', 501));

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "Las observaciones no pueden superar 500 caracteres.");
    }

    [Fact]
    public async Task CreateTwiceForSameProviderAndPeriodReturnsConflict()
    {
        HttpResponseMessage respuesta = await PublicarAsync(1, "2026-10", 80, 80, 80);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El proveedor 1 ya tiene una evaluación del periodo 2026-10.");
    }

    [Fact]
    public async Task UpdateRecalculatesScoreAndKeepsProviderAndPeriod()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            $"{Ruta}/3",
            new ActualizarEvaluacionRequestDto
            {
                PuntajeCumplimiento = 100,
                PuntajeCalidad = 100,
                PuntajePlazos = 100,
                Observaciones = "Mejoró"
            });
        EvaluacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<EvaluacionDetalleDto>($"{Ruta}/3");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(2, detalle.ProveedorId);
        Assert.Equal("2026-10", detalle.Periodo);
        Assert.Equal(100.00m, detalle.PuntajeFinal);
        Assert.Equal("Excelente", detalle.Calificacion);
        Assert.Equal("Mejoró", detalle.Observaciones);
    }

    [Fact]
    public async Task UpdateOfMissingEvaluationReturnsNotFoundBeforeValidating()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            $"{Ruta}/9999",
            new ActualizarEvaluacionRequestDto { PuntajeCumplimiento = 500 });

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "La evaluación 9999 no existe.");
    }

    [Fact]
    public async Task UpdateWithInvalidScoreReturnsBadRequest()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            $"{Ruta}/3",
            new ActualizarEvaluacionRequestDto { PuntajeCumplimiento = 80, PuntajeCalidad = 80, PuntajePlazos = 120 });

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El puntaje de plazos debe estar entre 0 y 100.");
    }

    [Fact]
    public async Task DeleteRemovesEvaluationAndSecondDeleteReturnsNotFound()
    {
        HttpResponseMessage borrada = await _clienteHttp.DeleteAsync($"{Ruta}/6");
        HttpResponseMessage repetida = await _clienteHttp.DeleteAsync($"{Ruta}/6");

        Assert.Equal(HttpStatusCode.NoContent, borrada.StatusCode);
        await AssertBusinessErrorAsync(repetida, HttpStatusCode.NotFound, 404, "La evaluación 6 no existe.");
    }

    [Fact]
    public async Task SummaryReturnsTotalsRatingCountsAndRanking()
    {
        ResumenEvaluacionesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenEvaluacionesDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(6, resumen.TotalEvaluaciones);
        Assert.Equal(4, resumen.ProveedoresEvaluados);
        Assert.Equal(78.72m, resumen.PromedioGeneral);
        Assert.Equal(
            new[] { "Excelente:2", "Bueno:2", "Regular:1", "Deficiente:1" },
            resumen.PorCalificacion.Select(conteo => $"{conteo.Calificacion}:{conteo.Total}").ToArray());
        Assert.Equal(new[] { 6, 1, 4, 2 }, resumen.Ranking.Select(item => item.ProveedorId).ToArray());
        Assert.Equal(new[] { 95.50m, 89.05m, 66.60m, 65.50m }, resumen.Ranking.Select(item => item.Promedio).ToArray());
        Assert.Equal(
            new[] { "Excelente", "Excelente", "Bueno", "Regular" },
            resumen.Ranking.Select(item => item.UltimaCalificacion).ToArray());
    }

    [Fact]
    public async Task SummaryWithoutEvaluationsReturnsZerosAndAllRatings()
    {
        LimpiarEvaluaciones();

        ResumenEvaluacionesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenEvaluacionesDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.TotalEvaluaciones);
        Assert.Equal(0m, resumen.PromedioGeneral);
        Assert.Equal(4, resumen.PorCalificacion.Count);
        Assert.Empty(resumen.Ranking);
    }

    [Fact]
    public async Task OpenApiContainsSixEvaluationOperations()
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
        Assert.True(rutas.GetProperty(Ruta + "/resumen").TryGetProperty("get", out _));
    }

    private Task<HttpResponseMessage> PublicarAsync(
        int proveedorId,
        string periodo,
        int cumplimiento,
        int calidad,
        int plazos,
        string? observaciones = null)
    {
        return _clienteHttp.PostAsJsonAsync(
            Ruta,
            new CrearEvaluacionRequestDto
            {
                ProveedorId = proveedorId,
                Periodo = periodo,
                PuntajeCumplimiento = cumplimiento,
                PuntajeCalidad = calidad,
                PuntajePlazos = plazos,
                Observaciones = observaciones
            });
    }

    private async Task<int> CrearAsync(
        int proveedorId,
        string periodo,
        int cumplimiento,
        int calidad,
        int plazos,
        string? observaciones = null)
    {
        HttpResponseMessage respuesta = await PublicarAsync(
            proveedorId,
            periodo,
            cumplimiento,
            calidad,
            plazos,
            observaciones);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        IdCreado? creado = await respuesta.Content.ReadFromJsonAsync<IdCreado>();

        Assert.NotNull(creado);
        return creado.Id;
    }

    private sealed class IdCreado
    {
        public int Id { get; set; }
    }

    private void LimpiarEvaluaciones()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.EvaluacionesProveedor.RemoveRange(contexto.EvaluacionesProveedor);
        contexto.SaveChanges();
    }

    private void RestablecerEvaluaciones()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.EvaluacionesProveedor.RemoveRange(contexto.EvaluacionesProveedor);
        contexto.Proveedores.RemoveRange(contexto.Proveedores);
        contexto.Proveedores.AddRange(
            CrearProveedor(1, "Andes Suministros S.A.C.", "20600000001", "Peru", true),
            CrearProveedor(2, "Minerales del Sur S.A.C.", "20600000002", "Peru", true),
            CrearProveedor(3, "Logística Pacífico S.A.C.", "20600000003", "Peru", true),
            CrearProveedor(4, "Cobre Austral SpA", "76000000004", "Chile", true),
            CrearProveedor(5, "Servicios Andinos SpA", "76000000005", "Chile", false),
            CrearProveedor(6, "Shenzhen Parts Ltd.", "91000000006", "China", true));
        contexto.EvaluacionesProveedor.AddRange(
            CrearEvaluacion(1, 1, "2026-09", 90, 85, 80, 85.50m, "Bueno"),
            CrearEvaluacion(2, 1, "2026-10", 95, 92, 90, 92.60m, "Excelente"),
            CrearEvaluacion(3, 2, "2026-10", 70, 65, 60, 65.50m, "Regular"),
            CrearEvaluacion(4, 4, "2026-09", 55, 60, 50, 55.00m, "Deficiente"),
            CrearEvaluacion(5, 4, "2026-10", 80, 78, 76, 78.20m, "Bueno"),
            CrearEvaluacion(6, 6, "2026-10", 100, 90, 95, 95.50m, "Excelente"));
        contexto.SaveChanges();
    }

    private static Proveedor CrearProveedor(int id, string razonSocial, string ruc, string pais, bool activo)
    {
        return new Proveedor { Id = id, RazonSocial = razonSocial, Ruc = ruc, Pais = pais, Activo = activo };
    }

    private static EvaluacionProveedor CrearEvaluacion(
        int id,
        int proveedorId,
        string periodo,
        int cumplimiento,
        int calidad,
        int plazos,
        decimal final,
        string calificacion)
    {
        return new EvaluacionProveedor
        {
            Id = id,
            ProveedorId = proveedorId,
            Periodo = periodo,
            PuntajeCumplimiento = cumplimiento,
            PuntajeCalidad = calidad,
            PuntajePlazos = plazos,
            PuntajeFinal = final,
            Calificacion = calificacion,
            FechaRegistro = DateTime.UtcNow
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
}
