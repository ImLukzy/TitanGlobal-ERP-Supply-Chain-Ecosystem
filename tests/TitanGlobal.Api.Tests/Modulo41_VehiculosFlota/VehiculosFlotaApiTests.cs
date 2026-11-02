using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo41_VehiculosFlota;

[Collection("Modulo04")]
public class VehiculosFlotaApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public VehiculosFlotaApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerVehiculos();
    }

    [Fact]
    public async Task ListFiltersByPlateBrandAndModelWithoutCase()
    {
        PaginaVehiculosFlotaDto? porPlaca = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?busqueda=abc-1");
        PaginaVehiculosFlotaDto? porMarca = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?busqueda=SCANIA");
        PaginaVehiculosFlotaDto? porModelo = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?busqueda=sprint");
        PaginaVehiculosFlotaDto? sinCoincidencias = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?busqueda=inexistente");

        Assert.NotNull(porPlaca);
        Assert.Equal(new[] { 1 }, porPlaca.Items.Select(vehiculo => vehiculo.Id).ToArray());
        Assert.NotNull(porMarca);
        Assert.Equal(new[] { 3 }, porMarca.Items.Select(vehiculo => vehiculo.Id).ToArray());
        Assert.NotNull(porModelo);
        Assert.Equal(new[] { 2 }, porModelo.Items.Select(vehiculo => vehiculo.Id).ToArray());
        Assert.NotNull(sinCoincidencias);
        Assert.Equal(0, sinCoincidencias.Total);
        Assert.Empty(sinCoincidencias.Items);
    }

    [Fact]
    public async Task ListFiltersByTypeWithoutCaseAndByActiveValue()
    {
        PaginaVehiculosFlotaDto? camiones = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?tipo=cAmIoN");
        PaginaVehiculosFlotaDto? inactivos = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?activo=false");
        PaginaVehiculosFlotaDto? activos = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?activo=true");
        PaginaVehiculosFlotaDto? furgonesInactivos = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?tipo=Furgon&activo=false");

        Assert.NotNull(camiones);
        Assert.Equal(2, camiones.Total);
        Assert.All(camiones.Items, vehiculo => Assert.Equal("Camion", vehiculo.Tipo));
        Assert.NotNull(inactivos);
        Assert.Equal(1, inactivos.Total);
        Assert.Equal("JKL-012", Assert.Single(inactivos.Items).Placa);
        Assert.False(inactivos.Items[0].Activo);
        Assert.NotNull(activos);
        Assert.Equal(5, activos.Total);
        Assert.NotNull(furgonesInactivos);
        Assert.Equal(4, Assert.Single(furgonesInactivos.Items).Id);
    }

    [Fact]
    public async Task ListPaginatesInStablePlateOrder()
    {
        PaginaVehiculosFlotaDto? primera = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota");
        PaginaVehiculosFlotaDto? segunda = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?pagina=2&tamanoPagina=2");
        PaginaVehiculosFlotaDto? ultima = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?pagina=2&tamanoPagina=4");
        PaginaVehiculosFlotaDto? fueraDeRango = await _clienteHttp.GetFromJsonAsync<PaginaVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota?pagina=9&tamanoPagina=4");

        Assert.NotNull(primera);
        Assert.Equal(6, primera.Total);
        Assert.Equal(1, primera.Pagina);
        Assert.Equal(20, primera.TamanoPagina);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, primera.Items.Select(vehiculo => vehiculo.Id).ToArray());
        Assert.NotNull(segunda);
        Assert.Equal(6, segunda.Total);
        Assert.Equal(2, segunda.Pagina);
        Assert.Equal(2, segunda.TamanoPagina);
        Assert.Equal(new[] { 3, 4 }, segunda.Items.Select(vehiculo => vehiculo.Id).ToArray());
        Assert.NotNull(ultima);
        Assert.Equal(new[] { 5, 6 }, ultima.Items.Select(vehiculo => vehiculo.Id).ToArray());
        Assert.NotNull(fueraDeRango);
        Assert.Equal(6, fueraDeRango.Total);
        Assert.Empty(fueraDeRango.Items);
    }

    [Theory]
    [InlineData("/api/v1/vehiculos-flota?pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("/api/v1/vehiculos-flota?tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("/api/v1/vehiculos-flota?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task InvalidPaginationReturnsBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsAllVehicleFields()
    {
        VehiculoFlotaDetalleDto? vehiculo = await _clienteHttp.GetFromJsonAsync<VehiculoFlotaDetalleDto>(
            "/api/v1/vehiculos-flota/1");

        Assert.NotNull(vehiculo);
        Assert.Equal(1, vehiculo.Id);
        Assert.Equal("ABC-123", vehiculo.Placa);
        Assert.Equal("Volvo", vehiculo.Marca);
        Assert.Equal("FH16", vehiculo.Modelo);
        Assert.Equal(2022, vehiculo.Anio);
        Assert.Equal("Camion", vehiculo.Tipo);
        Assert.Equal(20000m, vehiculo.CapacidadKg);
        Assert.True(vehiculo.Activo);
        Assert.Equal("Unidad de larga distancia", vehiculo.Observaciones);
    }

    [Fact]
    public async Task TypesReturnsDistinctSortedValues()
    {
        List<string>? tipos = await _clienteHttp.GetFromJsonAsync<List<string>>("/api/v1/vehiculos-flota/tipos");

        Assert.NotNull(tipos);
        Assert.Equal(new[] { "Camion", "Furgon", "Moto", "Otro" }, tipos);
    }

    [Fact]
    public async Task SummaryReturnsExactCountsCapacityAndTypeOrder()
    {
        ResumenVehiculosFlotaDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(6, resumen.TotalVehiculos);
        Assert.Equal(5, resumen.Activos);
        Assert.Equal(1, resumen.Inactivos);
        Assert.Equal(44050m, resumen.CapacidadTotalKg);
        Assert.Equal(new[] { "Camion", "Furgon", "Moto", "Otro" }, resumen.PorTipo.Select(tipo => tipo.Tipo).ToArray());
        Assert.Equal(new[] { 2, 2, 1, 1 }, resumen.PorTipo.Select(tipo => tipo.TotalVehiculos).ToArray());
        Assert.Equal(new[] { 2, 1, 1, 1 }, resumen.PorTipo.Select(tipo => tipo.Activos).ToArray());
    }

    [Fact]
    public async Task SummaryOfEmptyFleetReturnsZeros()
    {
        VaciarVehiculos();

        ResumenVehiculosFlotaDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenVehiculosFlotaDto>(
            "/api/v1/vehiculos-flota/resumen");
        List<string>? tipos = await _clienteHttp.GetFromJsonAsync<List<string>>("/api/v1/vehiculos-flota/tipos");

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.TotalVehiculos);
        Assert.Equal(0, resumen.Activos);
        Assert.Equal(0, resumen.Inactivos);
        Assert.Equal(0m, resumen.CapacidadTotalKg);
        Assert.Empty(resumen.PorTipo);
        Assert.NotNull(tipos);
        Assert.Empty(tipos);
    }

    [Theory]
    [InlineData("", "La placa es obligatoria.")]
    [InlineData("   ", "La placa es obligatoria.")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU", "La placa no puede superar 20 caracteres.")]
    public async Task PlateIsValidated(string placa, string mensaje)
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Placa = placa;

        await AssertCreateBadRequestAsync(solicitud, mensaje);
    }

    [Theory]
    [InlineData("", "La marca es obligatoria.")]
    [InlineData("   ", "La marca es obligatoria.")]
    [InlineData("M", "La marca no puede superar 100 caracteres.")]
    public async Task BrandIsValidated(string marca, string mensaje)
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Marca = marca == "M" ? new string('M', 101) : marca;

        await AssertCreateBadRequestAsync(solicitud, mensaje);
    }

    [Theory]
    [InlineData("", "El modelo es obligatorio.")]
    [InlineData("   ", "El modelo es obligatorio.")]
    [InlineData("M", "El modelo no puede superar 100 caracteres.")]
    public async Task ModelIsValidated(string modelo, string mensaje)
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Modelo = modelo == "M" ? new string('M', 101) : modelo;

        await AssertCreateBadRequestAsync(solicitud, mensaje);
    }

    [Fact]
    public async Task YearOutsideAllowedRangeReturnsBadRequest()
    {
        int anioMaximo = DateTime.UtcNow.Year + 1;
        foreach (int anio in new[] { 0, 1899, anioMaximo + 1 })
        {
            GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
            solicitud.Anio = anio;

            await AssertCreateBadRequestAsync(solicitud, "El año del vehículo no es válido.");
        }
    }

    [Fact]
    public async Task YearAtTheLimitsIsAccepted()
    {
        foreach (int anio in new[] { 1900, DateTime.UtcNow.Year + 1 })
        {
            GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
            solicitud.Placa = $"LIM-{anio}";
            solicitud.Anio = anio;
            HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/vehiculos-flota", solicitud);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
    }

    [Theory]
    [InlineData("", "El tipo es obligatorio.")]
    [InlineData("   ", "El tipo es obligatorio.")]
    [InlineData("Bus", "El tipo debe ser Camion, Furgon, Moto u Otro.")]
    [InlineData("Camión", "El tipo debe ser Camion, Furgon, Moto u Otro.")]
    public async Task TypeIsValidated(string tipo, string mensaje)
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Tipo = tipo;

        await AssertCreateBadRequestAsync(solicitud, mensaje);
    }

    [Theory]
    [InlineData(0, "La capacidad debe ser mayor que cero.")]
    [InlineData(-5, "La capacidad debe ser mayor que cero.")]
    public async Task NonPositiveCapacityReturnsBadRequest(int capacidad, string mensaje)
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.CapacidadKg = capacidad;

        await AssertCreateBadRequestAsync(solicitud, mensaje);
    }

    [Theory]
    [InlineData("100000000000000000")]
    [InlineData("10000000000000000")]
    [InlineData("99999999999999999999.99")]
    public async Task CapacityAboveColumnLimitReturnsBadRequestInsteadOfServerError(string capacidad)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Post,
            "/api/v1/vehiculos-flota",
            CrearJson("NUEVA-1", capacidad));

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La capacidad no puede superar 9999999999999999.99.");
    }

    [Theory]
    [InlineData("100.123")]
    [InlineData("0.001")]
    [InlineData("1500.999")]
    public async Task CapacityWithMoreThanTwoDecimalsReturnsBadRequest(string capacidad)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Post,
            "/api/v1/vehiculos-flota",
            CrearJson("NUEVA-1", capacidad));

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La capacidad no puede tener más de 2 decimales.");
    }

    [Theory]
    [InlineData("9999999999999999.99")]
    [InlineData("0.01")]
    [InlineData("100.5")]
    [InlineData("100.50")]
    public async Task CapacityWithinColumnLimitIsAccepted(string capacidad)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Post,
            "/api/v1/vehiculos-flota",
            CrearJson("NUEVA-1", capacidad));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task OverlongObservationsReturnBadRequestOnCreateAndUpdate()
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Observaciones = new string('O', 501);

        HttpResponseMessage alta = await _clienteHttp.PostAsJsonAsync("/api/v1/vehiculos-flota", solicitud);
        HttpResponseMessage modificacion = await _clienteHttp.PutAsJsonAsync("/api/v1/vehiculos-flota/1", solicitud);

        string mensaje = "Las observaciones no pueden superar 500 caracteres.";
        await AssertBusinessErrorAsync(alta, HttpStatusCode.BadRequest, 400, mensaje);
        await AssertBusinessErrorAsync(modificacion, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ObservationsOfExactlyLimitAreAcceptedAfterTrimming()
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Observaciones = $"  {new string('O', 500)}  ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/vehiculos-flota", solicitud);
        int id = await LeerIdAsync(respuesta);

        VehiculoFlota vehiculo = await ObtenerGuardadoAsync(id);

        Assert.Equal(new string('O', 500), vehiculo.Observaciones);
    }

    [Fact]
    public async Task ValidationRulesAreAppliedInSpecifiedOrder()
    {
        GuardarVehiculoFlotaRequestDto solicitud = new GuardarVehiculoFlotaRequestDto
        {
            Placa = string.Empty,
            Marca = string.Empty,
            Modelo = string.Empty,
            Anio = 0,
            Tipo = string.Empty,
            CapacidadKg = 0,
            Observaciones = new string('O', 501)
        };
        string[] mensajes =
        {
            "La placa es obligatoria.",
            "La marca es obligatoria.",
            "El modelo es obligatorio.",
            "El año del vehículo no es válido.",
            "El tipo es obligatorio.",
            "La capacidad debe ser mayor que cero.",
            "Las observaciones no pueden superar 500 caracteres."
        };

        await AssertCreateBadRequestAsync(solicitud, mensajes[0]);
        solicitud.Placa = "ORD-001";
        await AssertCreateBadRequestAsync(solicitud, mensajes[1]);
        solicitud.Marca = "Marca";
        await AssertCreateBadRequestAsync(solicitud, mensajes[2]);
        solicitud.Modelo = "Modelo";
        await AssertCreateBadRequestAsync(solicitud, mensajes[3]);
        solicitud.Anio = 2020;
        await AssertCreateBadRequestAsync(solicitud, mensajes[4]);
        solicitud.Tipo = "Moto";
        await AssertCreateBadRequestAsync(solicitud, mensajes[5]);
        solicitud.CapacidadKg = 10;
        await AssertCreateBadRequestAsync(solicitud, mensajes[6]);
    }

    [Fact]
    public async Task PlateSpacesAreTrimmedTypeIsCanonicalAndBlankObservationsBecomeNull()
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Placa = "  NUE-001  ";
        solicitud.Marca = "  Marca nueva  ";
        solicitud.Modelo = "  Modelo nuevo  ";
        solicitud.Tipo = " furgon ";
        solicitud.Observaciones = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/vehiculos-flota", solicitud);
        int id = await LeerIdAsync(respuesta);

        VehiculoFlota vehiculo = await ObtenerGuardadoAsync(id);

        Assert.Equal("NUE-001", vehiculo.Placa);
        Assert.Equal("Marca nueva", vehiculo.Marca);
        Assert.Equal("Modelo nuevo", vehiculo.Modelo);
        Assert.Equal("Furgon", vehiculo.Tipo);
        Assert.Null(vehiculo.Observaciones);
        Assert.True(vehiculo.Activo);
    }

    [Fact]
    public async Task DuplicatePlateReturnsConflictWithLiteralMessage()
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Placa = " ABC-123 ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/vehiculos-flota", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un vehículo con la placa ABC-123.");
    }

    [Fact]
    public async Task UpdateMayKeepItsOwnPlateAndPersistsChanges()
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Placa = "ABC-123";
        solicitud.Marca = "Volvo Actualizado";
        solicitud.Activo = false;
        solicitud.Observaciones = "  Cambio  ";
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/vehiculos-flota/1", solicitud);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(1, await LeerIdAsync(respuesta));
        VehiculoFlota vehiculo = await ObtenerGuardadoAsync(1);
        Assert.Equal("Volvo Actualizado", vehiculo.Marca);
        Assert.False(vehiculo.Activo);
        Assert.Equal("Cambio", vehiculo.Observaciones);
    }

    [Fact]
    public async Task UpdateWithAnotherVehiclesPlateReturnsConflict()
    {
        GuardarVehiculoFlotaRequestDto solicitud = CrearSolicitud();
        solicitud.Placa = "DEF-456";
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/vehiculos-flota/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un vehículo con la placa DEF-456.");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingVehicleReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/vehiculos-flota/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/vehiculos-flota/9999", CrearSolicitud());
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/vehiculos-flota/9999");
        }

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El vehículo 9999 no existe.");
    }

    [Fact]
    public async Task DeleteVehicleReturnsNoContentAndRemovesIt()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/vehiculos-flota/6");
        HttpResponseMessage detalle = await _clienteHttp.GetAsync("/api/v1/vehiculos-flota/6");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        await AssertBusinessErrorAsync(detalle, HttpStatusCode.NotFound, 404, "El vehículo 6 no existe.");
    }

    [Fact]
    public async Task OpenApiContainsSevenVehicleOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/vehiculos-flota").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/vehiculos-flota").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/vehiculos-flota/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/vehiculos-flota/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/vehiculos-flota/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/vehiculos-flota/tipos").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/vehiculos-flota/resumen").TryGetProperty("get", out _));
    }

    private void RestablecerVehiculos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.VehiculosFlota.RemoveRange(contexto.VehiculosFlota);
        contexto.VehiculosFlota.AddRange(
            CrearVehiculo(1, "ABC-123", "Volvo", "FH16", 2022, "Camion", 20000m, true, "Unidad de larga distancia"),
            CrearVehiculo(2, "DEF-456", "Mercedes-Benz", "Sprinter", 2021, "Furgon", 1500m, true, null),
            CrearVehiculo(3, "GHI-789", "Scania", "R450", 2023, "Camion", 18000m, true, null),
            CrearVehiculo(4, "JKL-012", "Toyota", "Hilux", 2020, "Furgon", 1000m, false, "Unidad en mantenimiento"),
            CrearVehiculo(5, "MNO-345", "Honda", "CB500", 2022, "Moto", 50m, true, "Reparto urbano"),
            CrearVehiculo(6, "PQR-678", "Iveco", "Daily", 2021, "Otro", 3500m, true, null));
        contexto.SaveChanges();
    }

    private void VaciarVehiculos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.VehiculosFlota.RemoveRange(contexto.VehiculosFlota);
        contexto.SaveChanges();
    }

    private static VehiculoFlota CrearVehiculo(
        int id,
        string placa,
        string marca,
        string modelo,
        int anio,
        string tipo,
        decimal capacidadKg,
        bool activo,
        string? observaciones)
    {
        return new VehiculoFlota
        {
            Id = id,
            Placa = placa,
            Marca = marca,
            Modelo = modelo,
            Anio = anio,
            Tipo = tipo,
            CapacidadKg = capacidadKg,
            Activo = activo,
            Observaciones = observaciones
        };
    }

    private static GuardarVehiculoFlotaRequestDto CrearSolicitud()
    {
        return new GuardarVehiculoFlotaRequestDto
        {
            Placa = "NUEVA-1",
            Marca = "Marca",
            Modelo = "Modelo",
            Anio = 2020,
            Tipo = "Camion",
            CapacidadKg = 1000m,
            Activo = true
        };
    }

    private static string CrearJson(string placa, string capacidad)
    {
        return "{\"placa\":\"" + placa + "\",\"marca\":\"Marca\",\"modelo\":\"Modelo\",\"anio\":2020,"
            + "\"tipo\":\"Camion\",\"capacidadKg\":" + capacidad + ",\"activo\":true}";
    }

    private async Task<HttpResponseMessage> EnviarJsonAsync(HttpMethod metodo, string ruta, string json)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(metodo, ruta)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _clienteHttp.SendAsync(peticion);
    }

    private async Task AssertCreateBadRequestAsync(GuardarVehiculoFlotaRequestDto solicitud, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/vehiculos-flota", solicitud);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    private static async Task<int> LeerIdAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.NotNull(resultado);

        return resultado["id"];
    }

    private async Task<VehiculoFlota> ObtenerGuardadoAsync(int id)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        VehiculoFlota? vehiculo = await contexto.VehiculosFlota.SingleOrDefaultAsync(entidad => entidad.Id == id);

        Assert.NotNull(vehiculo);

        return vehiculo;
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
