using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo33_CentrosTrabajo;

[Collection("Modulo04")]
public class CentrosTrabajoApiTests
{
    private const string Ruta = "/api/v1/centros-trabajo";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public CentrosTrabajoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerCentros();
    }

    [Fact]
    public async Task ListSearchesByCodeAndNameIgnoringCase()
    {
        PaginaCentrosTrabajoDto? porCodigo = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?busqueda=ct-lim-c01");
        PaginaCentrosTrabajoDto? porNombre = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?busqueda=MEZCLADO");

        Assert.NotNull(porCodigo);
        Assert.Equal(new[] { "CT-LIM-C01" }, porCodigo.Items.Select(centro => centro.Codigo).ToArray());
        Assert.NotNull(porNombre);
        Assert.Equal(new[] { "CT-ARE-L01" }, porNombre.Items.Select(centro => centro.Codigo).ToArray());
    }

    [Fact]
    public async Task ListFiltersByPlantTypeAndActive()
    {
        PaginaCentrosTrabajoDto? porPlanta = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?planta=planta%20lima");
        PaginaCentrosTrabajoDto? porTipo = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?tipo=celda");
        PaginaCentrosTrabajoDto? inactivos = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?activo=false");

        Assert.NotNull(porPlanta);
        Assert.Equal(3, porPlanta.Total);
        Assert.Equal(
            new[] { "CT-LIM-C01", "CT-LIM-L01", "CT-LIM-L02" },
            porPlanta.Items.Select(centro => centro.Codigo).ToArray());
        Assert.NotNull(porTipo);
        Assert.Equal(2, porTipo.Total);
        Assert.Equal(new[] { "CT-ARE-C01", "CT-LIM-C01" }, porTipo.Items.Select(centro => centro.Codigo).ToArray());
        Assert.All(porTipo.Items, centro => Assert.Equal("Celda", centro.Tipo));
        Assert.NotNull(inactivos);
        CentroTrabajoListadoDto inactivo = Assert.Single(inactivos.Items);
        Assert.Equal("CT-ARE-L01", inactivo.Codigo);
        Assert.False(inactivo.Activo);
    }

    [Fact]
    public async Task ListPaginatesWithTotalAndOrdersByCode()
    {
        PaginaCentrosTrabajoDto? primera = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?pagina=1&tamanoPagina=2");
        PaginaCentrosTrabajoDto? tercera = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?pagina=3&tamanoPagina=2");
        PaginaCentrosTrabajoDto? combinada = await _clienteHttp.GetFromJsonAsync<PaginaCentrosTrabajoDto>(
            $"{Ruta}?planta=PLANTA%20LIMA&tipo=LINEA&activo=true&pagina=2&tamanoPagina=1");

        Assert.NotNull(primera);
        Assert.Equal(5, primera.Total);
        Assert.Equal(1, primera.Pagina);
        Assert.Equal(2, primera.TamanoPagina);
        Assert.Equal(new[] { "CT-ARE-C01", "CT-ARE-L01" }, primera.Items.Select(centro => centro.Codigo).ToArray());
        Assert.NotNull(tercera);
        Assert.Equal(5, tercera.Total);
        Assert.Equal("CT-LIM-L02", Assert.Single(tercera.Items).Codigo);
        Assert.NotNull(combinada);
        Assert.Equal(2, combinada.Total);
        Assert.Equal("CT-LIM-L02", Assert.Single(combinada.Items).Codigo);
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tipo=Taller", "El tipo de centro de trabajo no es válido.")]
    public async Task InvalidQueryReturnsBadRequestWithLiteralMessage(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsAllFields()
    {
        CentroTrabajoDetalleDto? centro = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>($"{Ruta}/3");

        Assert.NotNull(centro);
        Assert.Equal(3, centro.Id);
        Assert.Equal("CT-LIM-C01", centro.Codigo);
        Assert.Equal("Celda de ensamblaje", centro.Nombre);
        Assert.Equal("Celda", centro.Tipo);
        Assert.Equal("Planta Lima", centro.Planta);
        Assert.Equal(8m, centro.CapacidadHorasDia);
        Assert.Equal(85.50m, centro.CostoHora);
        Assert.Equal("Rosa Quispe", centro.Responsable);
        Assert.True(centro.Activo);
    }

    [Fact]
    public async Task PlantsAreDistinctAndSorted()
    {
        List<string>? plantas = await _clienteHttp.GetFromJsonAsync<List<string>>($"{Ruta}/plantas");

        Assert.NotNull(plantas);
        Assert.Equal(new[] { "Planta Arequipa", "Planta Lima" }, plantas);
    }

    [Fact]
    public async Task SummaryReturnsExactCountsCapacityAndOrdering()
    {
        ResumenCentrosTrabajoDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenCentrosTrabajoDto>(
            $"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.TotalCentros);
        Assert.Equal(4, resumen.Activos);
        Assert.Equal(1, resumen.Inactivos);
        Assert.Equal(68m, resumen.CapacidadTotalHorasDia);
        Assert.Equal(44m, resumen.CapacidadActivaHorasDia);
        Assert.Equal(new[] { "Linea", "Celda" }, resumen.PorTipo.Select(tipo => tipo.Tipo).ToArray());
        Assert.Equal(new[] { 3, 2 }, resumen.PorTipo.Select(tipo => tipo.TotalCentros).ToArray());
        Assert.Equal(new[] { 48m, 20m }, resumen.PorTipo.Select(tipo => tipo.CapacidadHorasDia).ToArray());
        Assert.Equal(
            new[] { "Planta Lima", "Planta Arequipa" },
            resumen.PorPlanta.Select(planta => planta.Planta).ToArray());
        Assert.Equal(new[] { 3, 2 }, resumen.PorPlanta.Select(planta => planta.TotalCentros).ToArray());
        Assert.Equal(new[] { 3, 1 }, resumen.PorPlanta.Select(planta => planta.Activos).ToArray());
    }

    [Fact]
    public async Task SummaryOmitsTypesWithoutCenters()
    {
        HttpResponseMessage borrado = await _clienteHttp.DeleteAsync($"{Ruta}/3");
        HttpResponseMessage borradoDos = await _clienteHttp.DeleteAsync($"{Ruta}/5");
        ResumenCentrosTrabajoDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenCentrosTrabajoDto>(
            $"{Ruta}/resumen");

        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, borradoDos.StatusCode);
        Assert.NotNull(resumen);
        TipoCentrosTrabajoDto tipo = Assert.Single(resumen.PorTipo);
        Assert.Equal("Linea", tipo.Tipo);
    }

    [Theory]
    [InlineData("", "Centro", "Linea", "Planta Lima", 8, 10, "El código es obligatorio.")]
    [InlineData("   ", "Centro", "Linea", "Planta Lima", 8, 10, "El código es obligatorio.")]
    [InlineData("A", "Centro", "Linea", "Planta Lima", 8, 10, "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("CT 01", "Centro", "Linea", "Planta Lima", 8, 10, "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("CT-ABCDEFGHIJKLMNOPQR", "Centro", "Linea", "Planta Lima", 8, 10, "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("CT-01", "  ", "Linea", "Planta Lima", 8, 10, "El nombre del centro de trabajo es obligatorio.")]
    [InlineData("CT-01", "Centro", "Taller", "Planta Lima", 8, 10, "El tipo de centro de trabajo no es válido.")]
    [InlineData("CT-01", "Centro", "", "Planta Lima", 8, 10, "El tipo de centro de trabajo no es válido.")]
    [InlineData("CT-01", "Centro", "Linea", " ", 8, 10, "La planta es obligatoria.")]
    [InlineData("CT-01", "Centro", "Linea", "Planta Lima", 0, 10, "La capacidad debe ser mayor que 0 y como máximo 24 horas por día.")]
    [InlineData("CT-01", "Centro", "Linea", "Planta Lima", -1, 10, "La capacidad debe ser mayor que 0 y como máximo 24 horas por día.")]
    [InlineData("CT-01", "Centro", "Linea", "Planta Lima", 24.01, 10, "La capacidad debe ser mayor que 0 y como máximo 24 horas por día.")]
    [InlineData("CT-01", "Centro", "Linea", "Planta Lima", 8, -0.01, "El costo por hora no puede ser negativo.")]
    [InlineData("A", "", "Taller", "", 0, -1, "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("CT-01", "", "Taller", "", 0, -1, "El nombre del centro de trabajo es obligatorio.")]
    [InlineData("CT-01", "Centro", "Taller", "", 0, -1, "El tipo de centro de trabajo no es válido.")]
    [InlineData("CT-01", "Centro", "Celda", "", 0, -1, "La planta es obligatoria.")]
    [InlineData("CT-01", "Centro", "Celda", "Planta", 0, -1, "La capacidad debe ser mayor que 0 y como máximo 24 horas por día.")]
    public async Task CreateValidatesRulesInOrderWithoutCreatingRows(
        string codigo,
        string nombre,
        string tipo,
        string planta,
        double capacidadHorasDia,
        double costoHora,
        string mensaje)
    {
        GuardarCentroTrabajoRequestDto solicitud = CrearSolicitud(
            codigo,
            nombre,
            tipo,
            planta,
            (decimal)capacidadHorasDia,
            (decimal)costoHora);

        await AssertPostErrorAsync(solicitud, mensaje);
    }

    [Fact]
    public async Task OverlongValuesReturnLiteralErrorsWithoutCreatingRows()
    {
        GuardarCentroTrabajoRequestDto nombreLargo = CrearSolicitud("CT-NEW-01", new string('N', 201), "Linea", "Planta Lima", 8, 10);
        await AssertPostErrorAsync(nombreLargo, "El nombre del centro de trabajo no puede superar 200 caracteres.");

        GuardarCentroTrabajoRequestDto plantaLarga = CrearSolicitud("CT-NEW-02", "Centro", "Linea", new string('P', 101), 8, 10);
        await AssertPostErrorAsync(plantaLarga, "La planta no puede superar 100 caracteres.");

        GuardarCentroTrabajoRequestDto responsableLargo = CrearSolicitud("CT-NEW-03", "Centro", "Linea", "Planta Lima", 8, 10);
        responsableLargo.Responsable = new string('R', 101);
        await AssertPostErrorAsync(responsableLargo, "El responsable no puede superar 100 caracteres.");
    }

    [Fact]
    public async Task MaximumLengthsAreAccepted()
    {
        GuardarCentroTrabajoRequestDto solicitud = CrearSolicitud(
            "CT-ABCDEFGHIJKLMNOPQ",
            new string('N', 200),
            "Linea",
            new string('P', 100),
            8,
            10);
        solicitud.Responsable = new string('R', 100);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(6, await ContarCentrosAsync());
    }

    [Fact]
    public async Task CreateNormalizesCodeTypeTextAndBlankResponsible()
    {
        GuardarCentroTrabajoRequestDto solicitud = CrearSolicitud(
            "  ct-new-01 ",
            "  Celda de pintura  ",
            "celda",
            "  Planta Cusco  ",
            24m,
            0m);
        solicitud.Responsable = "   ";
        solicitud.Activo = false;

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        Dictionary<string, int>? creado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        CentroTrabajoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>(
            $"{Ruta}/{creado["id"]}");
        Assert.NotNull(detalle);
        Assert.Equal("CT-NEW-01", detalle.Codigo);
        Assert.Equal("Celda de pintura", detalle.Nombre);
        Assert.Equal("Celda", detalle.Tipo);
        Assert.Equal("Planta Cusco", detalle.Planta);
        Assert.Equal(24m, detalle.CapacidadHorasDia);
        Assert.Equal(0m, detalle.CostoHora);
        Assert.Null(detalle.Responsable);
        Assert.False(detalle.Activo);
    }

    [Fact]
    public async Task CreateTrimsResponsible()
    {
        GuardarCentroTrabajoRequestDto solicitud = CrearSolicitud("CT-NEW-02", "Línea", "LINEA", "Planta Lima", 0.5m, 12.75m);
        solicitud.Responsable = "  Ana Soto  ";

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        Dictionary<string, int>? creado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        CentroTrabajoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>(
            $"{Ruta}/{creado["id"]}");
        Assert.NotNull(detalle);
        Assert.Equal("Linea", detalle.Tipo);
        Assert.Equal("Ana Soto", detalle.Responsable);
        Assert.Equal(0.5m, detalle.CapacidadHorasDia);
        Assert.Equal(12.75m, detalle.CostoHora);
        Assert.True(detalle.Activo);
    }

    [Fact]
    public async Task CreateWithDuplicateCodeReturnsConflictWithoutCreatingRows()
    {
        GuardarCentroTrabajoRequestDto duplicado = CrearSolicitud(" ct-lim-l01 ", "Duplicado", "Linea", "Planta Lima", 8, 10);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, duplicado);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un centro de trabajo con el código CT-LIM-L01.");
        Assert.Equal(5, await ContarCentrosAsync());
    }

    [Fact]
    public async Task UpdateMayKeepItsOwnCodeAndChangesAllFields()
    {
        GuardarCentroTrabajoRequestDto solicitud = CrearSolicitud("ct-lim-l01", "Línea de envasado 1 editada", "celda", "Planta Callao", 12m, 99.90m);
        solicitud.Responsable = "Luis Vega";
        solicitud.Activo = false;

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado["id"]);
        CentroTrabajoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>($"{Ruta}/1");
        Assert.NotNull(detalle);
        Assert.Equal("CT-LIM-L01", detalle.Codigo);
        Assert.Equal("Línea de envasado 1 editada", detalle.Nombre);
        Assert.Equal("Celda", detalle.Tipo);
        Assert.Equal("Planta Callao", detalle.Planta);
        Assert.Equal(12m, detalle.CapacidadHorasDia);
        Assert.Equal(99.90m, detalle.CostoHora);
        Assert.Equal("Luis Vega", detalle.Responsable);
        Assert.False(detalle.Activo);
    }

    [Fact]
    public async Task UpdateWithDuplicateCodeDoesNotChangeExistingRow()
    {
        GuardarCentroTrabajoRequestDto solicitud = CrearSolicitud("CT-LIM-L02", "No debe guardar", "Celda", "Planta X", 1, 1);

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un centro de trabajo con el código CT-LIM-L02.");
        await AssertCentroUnoSinCambiosAsync();
    }

    [Fact]
    public async Task UpdateWithInvalidDataDoesNotChangeExistingRow()
    {
        GuardarCentroTrabajoRequestDto capacidad = CrearSolicitud("CT-LIM-L01", "No debe guardar", "Linea", "Planta Lima", 24.01m, 10);
        HttpResponseMessage respuestaCapacidad = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", capacidad);
        await AssertBusinessErrorAsync(
            respuestaCapacidad,
            HttpStatusCode.BadRequest,
            400,
            "La capacidad debe ser mayor que 0 y como máximo 24 horas por día.");

        GuardarCentroTrabajoRequestDto costo = CrearSolicitud("CT-LIM-L01", "No debe guardar", "Linea", "Planta Lima", 8, -1);
        HttpResponseMessage respuestaCosto = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", costo);
        await AssertBusinessErrorAsync(
            respuestaCosto,
            HttpStatusCode.BadRequest,
            400,
            "El costo por hora no puede ser negativo.");

        await AssertCentroUnoSinCambiosAsync();
    }

    [Theory]
    [InlineData("100000000000000000", "El costo por hora no puede superar 9999999999999999.99.")]
    [InlineData("10000000000000000", "El costo por hora no puede superar 9999999999999999.99.")]
    [InlineData("10.001", "El costo por hora admite como máximo 2 decimales.")]
    [InlineData("9999999999999999.991", "El costo por hora admite como máximo 2 decimales.")]
    public async Task CostOutOfRangeReturnsBadRequestOnCreateAndUpdateWithoutChangingRows(string costoHora, string mensaje)
    {
        string json = CrearJson("CT-NEW-01", "8", costoHora);

        HttpResponseMessage crear = await _clienteHttp.PostAsync(Ruta, CrearContenido(json));
        await AssertBusinessErrorAsync(crear, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(5, await ContarCentrosAsync());

        HttpResponseMessage actualizar = await _clienteHttp.PutAsync(
            $"{Ruta}/1",
            CrearContenido(CrearJson("CT-LIM-L01", "8", costoHora)));
        await AssertBusinessErrorAsync(actualizar, HttpStatusCode.BadRequest, 400, mensaje);
        await AssertCentroUnoSinCambiosAsync();
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("8.005")]
    [InlineData("23.999")]
    public async Task CapacityWithMoreThanTwoDecimalsReturnsBadRequestOnCreateAndUpdate(string capacidad)
    {
        const string mensaje = "La capacidad admite como máximo 2 decimales.";

        HttpResponseMessage crear = await _clienteHttp.PostAsync(
            Ruta,
            CrearContenido(CrearJson("CT-NEW-01", capacidad, "10")));
        await AssertBusinessErrorAsync(crear, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(5, await ContarCentrosAsync());

        HttpResponseMessage actualizar = await _clienteHttp.PutAsync(
            $"{Ruta}/1",
            CrearContenido(CrearJson("CT-LIM-L01", capacidad, "10")));
        await AssertBusinessErrorAsync(actualizar, HttpStatusCode.BadRequest, 400, mensaje);
        await AssertCentroUnoSinCambiosAsync();
    }

    [Fact]
    public async Task CapacityAndCostAtTheirLimitsWithTwoDecimalsAreAccepted()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(
            Ruta,
            CrearContenido(CrearJson("CT-NEW-01", "0.01", "9999999999999999.99")));
        Dictionary<string, int>? creado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        CentroTrabajoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>(
            $"{Ruta}/{creado["id"]}");
        Assert.NotNull(detalle);
        Assert.Equal(0.01m, detalle.CapacidadHorasDia);
        Assert.Equal(9999999999999999.99m, detalle.CostoHora);
    }

    [Fact]
    public async Task OmittedActiveInJsonDefaultsToTrueOnCreate()
    {
        string json = "{\"codigo\":\"CT-NEW-01\",\"nombre\":\"Sin activo\",\"tipo\":\"Linea\","
            + "\"planta\":\"Planta Lima\",\"capacidadHorasDia\":8,\"costoHora\":10}";

        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(Ruta, CrearContenido(json));
        Dictionary<string, int>? creado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        CentroTrabajoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>(
            $"{Ruta}/{creado["id"]}");
        Assert.NotNull(detalle);
        Assert.True(detalle.Activo);
    }

    [Fact]
    public async Task OmittedActiveInJsonKeepsCenterActiveOnUpdate()
    {
        string json = "{\"codigo\":\"CT-LIM-L01\",\"nombre\":\"Sin activo\",\"tipo\":\"Linea\","
            + "\"planta\":\"Planta Lima\",\"capacidadHorasDia\":8,\"costoHora\":10}";

        HttpResponseMessage respuesta = await _clienteHttp.PutAsync($"{Ruta}/1", CrearContenido(json));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        CentroTrabajoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>($"{Ruta}/1");
        Assert.NotNull(detalle);
        Assert.Equal("Sin activo", detalle.Nombre);
        Assert.True(detalle.Activo);
    }

    [Theory]
    [InlineData("A", "Centro", "Linea", "Planta Lima", "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("  ", "Centro", "Linea", "Planta Lima", "El código es obligatorio.")]
    [InlineData("CT-NEW-01", "  ", "Linea", "Planta Lima", "El nombre del centro de trabajo es obligatorio.")]
    [InlineData("CT-NEW-01", "Centro", "Taller", "Planta Lima", "El tipo de centro de trabajo no es válido.")]
    [InlineData("CT-NEW-01", "Centro", "Linea", "  ", "La planta es obligatoria.")]
    public async Task UpdateWithInvalidTextFieldsReturnsBadRequestWithoutChangingRow(
        string codigo,
        string nombre,
        string tipo,
        string planta,
        string mensaje)
    {
        GuardarCentroTrabajoRequestDto solicitud = CrearSolicitud(codigo, nombre, tipo, planta, 8, 10);

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", solicitud);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        await AssertCentroUnoSinCambiosAsync();
    }

    [Fact]
    public async Task UpdateWithOverlongTextFieldsReturnsBadRequestWithoutChangingRow()
    {
        GuardarCentroTrabajoRequestDto nombreLargo = CrearSolicitud("CT-LIM-L01", new string('N', 201), "Linea", "Planta Lima", 8, 10);
        await AssertPutErrorAsync(nombreLargo, "El nombre del centro de trabajo no puede superar 200 caracteres.");

        GuardarCentroTrabajoRequestDto plantaLarga = CrearSolicitud("CT-LIM-L01", "Centro", "Linea", new string('P', 101), 8, 10);
        await AssertPutErrorAsync(plantaLarga, "La planta no puede superar 100 caracteres.");

        GuardarCentroTrabajoRequestDto responsableLargo = CrearSolicitud("CT-LIM-L01", "Centro", "Linea", "Planta Lima", 8, 10);
        responsableLargo.Responsable = new string('R', 101);
        await AssertPutErrorAsync(responsableLargo, "El responsable no puede superar 100 caracteres.");

        GuardarCentroTrabajoRequestDto codigoLargo = CrearSolicitud("CT-ABCDEFGHIJKLMNOPQR", "Centro", "Linea", "Planta Lima", 8, 10);
        await AssertPutErrorAsync(codigoLargo, "El código solo admite letras, números y guiones (2 a 20 caracteres).");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingCenterReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync($"{Ruta}/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(
                $"{Ruta}/9999",
                CrearSolicitud("CT-NEW", "Nuevo", "Linea", "Planta Lima", 8, 10));
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync($"{Ruta}/9999");
        }

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "El centro de trabajo 9999 no existe.");
        Assert.Equal(5, await ContarCentrosAsync());
    }

    [Fact]
    public async Task DeleteReturnsNoContentAndRemovesRow()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync($"{Ruta}/5");
        HttpResponseMessage detalle = await _clienteHttp.GetAsync($"{Ruta}/5");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, detalle.StatusCode);
        Assert.Equal(4, await ContarCentrosAsync());
    }

    [Fact]
    public async Task OpenApiContainsSevenWorkCenterOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/plantas").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/resumen").TryGetProperty("get", out _));
    }

    private void RestablecerCentros()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.CentrosTrabajo.RemoveRange(contexto.CentrosTrabajo);
        contexto.CentrosTrabajo.AddRange(
            CrearCentro(1, "CT-LIM-L01", "Línea de envasado 1", "Linea", "Planta Lima", 16m, 120.00m, "Carlos Ramos", true),
            CrearCentro(2, "CT-LIM-L02", "Línea de envasado 2", "Linea", "Planta Lima", 8m, 110.00m, "Carlos Ramos", true),
            CrearCentro(3, "CT-LIM-C01", "Celda de ensamblaje", "Celda", "Planta Lima", 8m, 85.50m, "Rosa Quispe", true),
            CrearCentro(4, "CT-ARE-L01", "Línea de mezclado", "Linea", "Planta Arequipa", 24m, 150.00m, "Jorge Mamani", false),
            CrearCentro(5, "CT-ARE-C01", "Celda de etiquetado", "Celda", "Planta Arequipa", 12m, 60.00m, null, true));
        contexto.SaveChanges();
    }

    private static CentroTrabajo CrearCentro(
        int id,
        string codigo,
        string nombre,
        string tipo,
        string planta,
        decimal capacidadHorasDia,
        decimal costoHora,
        string? responsable,
        bool activo)
    {
        return new CentroTrabajo
        {
            Id = id,
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Planta = planta,
            CapacidadHorasDia = capacidadHorasDia,
            CostoHora = costoHora,
            Responsable = responsable,
            Activo = activo
        };
    }

    private static GuardarCentroTrabajoRequestDto CrearSolicitud(
        string codigo,
        string nombre,
        string tipo,
        string planta,
        decimal capacidadHorasDia,
        decimal costoHora)
    {
        return new GuardarCentroTrabajoRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Planta = planta,
            CapacidadHorasDia = capacidadHorasDia,
            CostoHora = costoHora
        };
    }

    private async Task AssertPostErrorAsync(GuardarCentroTrabajoRequestDto solicitud, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(5, await ContarCentrosAsync());
    }

    private static string CrearJson(string codigo, string capacidadHorasDia, string costoHora)
    {
        return "{\"codigo\":\"" + codigo + "\",\"nombre\":\"Centro\",\"tipo\":\"Linea\","
            + "\"planta\":\"Planta Lima\",\"capacidadHorasDia\":" + capacidadHorasDia
            + ",\"costoHora\":" + costoHora + ",\"activo\":true}";
    }

    private static StringContent CrearContenido(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private async Task AssertPutErrorAsync(GuardarCentroTrabajoRequestDto solicitud, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", solicitud);
        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        await AssertCentroUnoSinCambiosAsync();
    }

    private async Task AssertCentroUnoSinCambiosAsync()
    {
        CentroTrabajoDetalleDto? centro = await _clienteHttp.GetFromJsonAsync<CentroTrabajoDetalleDto>($"{Ruta}/1");
        Assert.NotNull(centro);
        Assert.Equal("CT-LIM-L01", centro.Codigo);
        Assert.Equal("Línea de envasado 1", centro.Nombre);
        Assert.Equal("Linea", centro.Tipo);
        Assert.Equal("Planta Lima", centro.Planta);
        Assert.Equal(16m, centro.CapacidadHorasDia);
        Assert.Equal(120.00m, centro.CostoHora);
        Assert.Equal("Carlos Ramos", centro.Responsable);
        Assert.True(centro.Activo);
    }

    private async Task<int> ContarCentrosAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.CentrosTrabajo.CountAsync();
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
