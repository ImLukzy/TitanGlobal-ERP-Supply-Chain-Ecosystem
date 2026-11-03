using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo42_Conductores;

[Collection("Modulo04")]
public class ConductoresApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ConductoresApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerConductores();
    }

    [Fact]
    public async Task ListFiltersByNameAndLicenseDocumentWithoutCase()
    {
        PaginaConductoresDto? porNombre = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?busqueda=QUISPE");
        PaginaConductoresDto? porDocumento = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?busqueda=plt-0003");
        PaginaConductoresDto? sinCoincidencias = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?busqueda=inexistente");

        Assert.NotNull(porNombre);
        Assert.Equal(new[] { 2 }, porNombre.Items.Select(conductor => conductor.Id).ToArray());
        Assert.NotNull(porDocumento);
        Assert.Equal(new[] { 3 }, porDocumento.Items.Select(conductor => conductor.Id).ToArray());
        Assert.NotNull(sinCoincidencias);
        Assert.Equal(0, sinCoincidencias.Total);
        Assert.Empty(sinCoincidencias.Items);
    }

    [Fact]
    public async Task ListFiltersByQualifiedValue()
    {
        PaginaConductoresDto? habilitados = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?habilitado=true");
        PaginaConductoresDto? noHabilitados = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?habilitado=false");

        Assert.NotNull(habilitados);
        Assert.Equal(2, habilitados.Total);
        Assert.All(habilitados.Items, conductor => Assert.True(conductor.Habilitado));
        Assert.NotNull(noHabilitados);
        Assert.Equal(3, Assert.Single(noHabilitados.Items).Id);
        Assert.False(noHabilitados.Items[0].Habilitado);
    }

    [Fact]
    public async Task ListPaginatesInStableLicenseDocumentOrder()
    {
        PaginaConductoresDto? primera = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores");
        PaginaConductoresDto? paginaUno = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?pagina=1&tamanoPagina=2");
        PaginaConductoresDto? paginaDos = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?pagina=2&tamanoPagina=2");
        PaginaConductoresDto? fueraDeRango = await _clienteHttp.GetFromJsonAsync<PaginaConductoresDto>(
            "/api/v1/conductores?pagina=9&tamanoPagina=2");

        Assert.NotNull(primera);
        Assert.Equal(3, primera.Total);
        Assert.Equal(1, primera.Pagina);
        Assert.Equal(20, primera.TamanoPagina);
        Assert.Equal(new[] { 1, 2, 3 }, primera.Items.Select(conductor => conductor.Id).ToArray());
        Assert.NotNull(paginaUno);
        Assert.Equal(new[] { 1, 2 }, paginaUno.Items.Select(conductor => conductor.Id).ToArray());
        Assert.NotNull(paginaDos);
        Assert.Equal(3, paginaDos.Total);
        Assert.Equal(2, paginaDos.Pagina);
        Assert.Equal(2, paginaDos.TamanoPagina);
        Assert.Equal(new[] { 3 }, paginaDos.Items.Select(conductor => conductor.Id).ToArray());
        Assert.NotNull(fueraDeRango);
        Assert.Empty(fueraDeRango.Items);
    }

    [Theory]
    [InlineData("/api/v1/conductores?pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("/api/v1/conductores?tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("/api/v1/conductores?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task InvalidPaginationReturnsBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsAllDriverFields()
    {
        ConductorDetalleDto? conductor = await _clienteHttp.GetFromJsonAsync<ConductorDetalleDto>(
            "/api/v1/conductores/3");

        Assert.NotNull(conductor);
        Assert.Equal(3, conductor.Id);
        Assert.Equal("Jose Flores", conductor.Nombre);
        Assert.Equal("PLT-0003", conductor.DocumentoLicencia);
        Assert.Equal(new DateTime(2098, 1, 15, 0, 0, 0, DateTimeKind.Utc), conductor.VencimientoLicencia);
        Assert.False(conductor.Habilitado);
        Assert.True(conductor.Activo);
        Assert.Equal("Sin habilitacion", conductor.Observaciones);
    }

    [Theory]
    [InlineData("", "El nombre es obligatorio.")]
    [InlineData("   ", "El nombre es obligatorio.")]
    public async Task NameIsRequired(string nombre, string mensaje)
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.Nombre = nombre;

        await AssertCreateBadRequestAsync(solicitud, mensaje);
    }

    [Fact]
    public async Task OverlongNameReturnsBadRequest()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.Nombre = new string('N', 101);

        await AssertCreateBadRequestAsync(solicitud, "El nombre no puede superar 100 caracteres.");
    }

    [Theory]
    [InlineData("", "El documento de licencia es obligatorio.")]
    [InlineData("   ", "El documento de licencia es obligatorio.")]
    public async Task LicenseDocumentIsRequired(string documento, string mensaje)
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.DocumentoLicencia = documento;

        await AssertCreateBadRequestAsync(solicitud, mensaje);
    }

    [Fact]
    public async Task OverlongLicenseDocumentReturnsBadRequest()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.DocumentoLicencia = new string('D', 21);

        await AssertCreateBadRequestAsync(
            solicitud,
            "El documento de licencia no puede superar 20 caracteres.");
    }

    [Fact]
    public async Task CreateWithPastExpirationReturnsBadRequest()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.VencimientoLicencia = DateTime.UtcNow.Date.AddDays(-1);

        await AssertCreateBadRequestAsync(solicitud, "La fecha de vencimiento de la licencia no es válida.");
    }

    [Fact]
    public async Task CreateWithPastDateWithoutZoneReturnsBadRequest()
    {
        string ayer = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Post,
            "/api/v1/conductores",
            CrearJson("PLT-NUE-1", ayer));

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La fecha de vencimiento de la licencia no es válida.");
    }

    [Fact]
    public async Task OverlongObservationsReturnBadRequestOnCreateAndUpdate()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.Observaciones = new string('O', 501);

        HttpResponseMessage alta = await _clienteHttp.PostAsJsonAsync("/api/v1/conductores", solicitud);
        HttpResponseMessage modificacion = await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/1", solicitud);

        string mensaje = "Las observaciones no pueden superar 500 caracteres.";
        await AssertBusinessErrorAsync(alta, HttpStatusCode.BadRequest, 400, mensaje);
        await AssertBusinessErrorAsync(modificacion, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ValidationRulesAreAppliedInSpecifiedOrder()
    {
        GuardarConductorRequestDto solicitud = new GuardarConductorRequestDto
        {
            Nombre = string.Empty,
            DocumentoLicencia = string.Empty,
            VencimientoLicencia = DateTime.UtcNow.Date.AddDays(-1),
            Observaciones = new string('O', 501)
        };

        await AssertCreateBadRequestAsync(solicitud, "El nombre es obligatorio.");
        solicitud.Nombre = "Conductor";
        await AssertCreateBadRequestAsync(solicitud, "El documento de licencia es obligatorio.");
        solicitud.DocumentoLicencia = "PLT-ORD-1";
        await AssertCreateBadRequestAsync(solicitud, "La fecha de vencimiento de la licencia no es válida.");
        solicitud.VencimientoLicencia = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await AssertCreateBadRequestAsync(solicitud, "Las observaciones no pueden superar 500 caracteres.");
    }

    [Theory]
    [InlineData("2099-12-31")]
    [InlineData("2099-12-31T00:00:00")]
    [InlineData("2099-12-31T00:00:00Z")]
    public async Task CreateAcceptsDatesWithAndWithoutZoneAndStoresThemAsUtcWithoutMovingTheDay(string fecha)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Post,
            "/api/v1/conductores",
            CrearJson("PLT-NUE-1", fecha));
        int id = await LeerIdAsync(respuesta);

        Conductor conductor = await ObtenerGuardadoAsync(id);

        Assert.Equal(DateTimeKind.Utc, conductor.VencimientoLicencia.Kind);
        Assert.Equal(new DateTime(2099, 12, 31), conductor.VencimientoLicencia.Date);
    }

    [Theory]
    [InlineData("2099-06-15")]
    [InlineData("2099-06-15T00:00:00")]
    [InlineData("2099-06-15T00:00:00Z")]
    public async Task UpdateAcceptsDatesWithAndWithoutZoneAndStoresThemAsUtcWithoutMovingTheDay(string fecha)
    {
        HttpResponseMessage respuesta = await EnviarJsonAsync(
            HttpMethod.Put,
            "/api/v1/conductores/1",
            CrearJson("PLT-0001", fecha));

        Assert.Equal(1, await LeerIdAsync(respuesta));
        Conductor conductor = await ObtenerGuardadoAsync(1);
        Assert.Equal(DateTimeKind.Utc, conductor.VencimientoLicencia.Kind);
        Assert.Equal(new DateTime(2099, 6, 15), conductor.VencimientoLicencia.Date);
    }

    [Theory]
    [InlineData("2020-06-30")]
    [InlineData("2020-06-30T00:00:00")]
    [InlineData("2020-06-30T00:00:00Z")]
    [InlineData("2020-06-30T15:30:00Z")]
    public async Task ExpiredDriverCanBeDeactivatedKeepingItsExpiredDate(string fecha)
    {
        string json = "{\"nombre\":\"Maria Quispe\",\"documentoLicencia\":\"PLT-0002\",\"vencimientoLicencia\":\""
            + fecha + "\",\"habilitado\":true,\"activo\":false}";
        HttpResponseMessage respuesta = await EnviarJsonAsync(HttpMethod.Put, "/api/v1/conductores/2", json);

        Assert.Equal(2, await LeerIdAsync(respuesta));
        Conductor conductor = await ObtenerGuardadoAsync(2);
        Assert.False(conductor.Activo);
        Assert.Equal(new DateTime(2020, 6, 30, 0, 0, 0, DateTimeKind.Utc), conductor.VencimientoLicencia);
        Assert.Equal(DateTimeKind.Utc, conductor.VencimientoLicencia.Kind);
    }

    [Fact]
    public async Task ExpiredDriverCannotMoveToAnotherPastDateButCanRenewWithAFutureDate()
    {
        GuardarConductorRequestDto otraFechaVencida = CrearSolicitud();
        otraFechaVencida.DocumentoLicencia = "PLT-0002";
        otraFechaVencida.VencimientoLicencia = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        HttpResponseMessage rechazo = await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/2", otraFechaVencida);

        await AssertBusinessErrorAsync(
            rechazo,
            HttpStatusCode.BadRequest,
            400,
            "La fecha de vencimiento de la licencia no es válida.");

        GuardarConductorRequestDto renovacion = CrearSolicitud();
        renovacion.DocumentoLicencia = "PLT-0002";
        renovacion.VencimientoLicencia = new DateTime(2099, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        HttpResponseMessage exito = await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/2", renovacion);

        Assert.Equal(2, await LeerIdAsync(exito));
        Conductor conductor = await ObtenerGuardadoAsync(2);
        Assert.Equal(new DateTime(2099, 3, 1), conductor.VencimientoLicencia.Date);
    }

    [Fact]
    public async Task UpdateOfValidDriverWithPastDateReturnsBadRequest()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.DocumentoLicencia = "PLT-0001";
        solicitud.VencimientoLicencia = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La fecha de vencimiento de la licencia no es válida.");
    }

    [Fact]
    public async Task TextFieldsAreTrimmedAndBlankObservationsBecomeNull()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.Nombre = "  Conductor nuevo  ";
        solicitud.DocumentoLicencia = " PLT-NUE-1 ";
        solicitud.Observaciones = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/conductores", solicitud);
        int id = await LeerIdAsync(respuesta);

        Conductor conductor = await ObtenerGuardadoAsync(id);

        Assert.Equal("Conductor nuevo", conductor.Nombre);
        Assert.Equal("PLT-NUE-1", conductor.DocumentoLicencia);
        Assert.Null(conductor.Observaciones);
        Assert.True(conductor.Habilitado);
        Assert.True(conductor.Activo);
    }

    [Fact]
    public async Task ObservationsAreTrimmedOnCreateAndUpdate()
    {
        GuardarConductorRequestDto alta = CrearSolicitud();
        alta.Observaciones = $"  {new string('O', 500)}  ";
        int id = await LeerIdAsync(await _clienteHttp.PostAsJsonAsync("/api/v1/conductores", alta));
        GuardarConductorRequestDto modificacion = CrearSolicitud();
        modificacion.DocumentoLicencia = "PLT-0001";
        modificacion.Observaciones = "  Nota  ";
        await LeerIdAsync(await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/1", modificacion));

        Assert.Equal(new string('O', 500), (await ObtenerGuardadoAsync(id)).Observaciones);
        Assert.Equal("Nota", (await ObtenerGuardadoAsync(1)).Observaciones);
    }

    [Fact]
    public async Task DuplicateLicenseDocumentReturnsConflictWithLiteralMessage()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.DocumentoLicencia = " PLT-0001 ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/conductores", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un conductor con el documento de licencia PLT-0001.");
    }

    [Fact]
    public async Task UpdateMayKeepItsOwnLicenseDocument()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.Nombre = "Carlos Ruiz Actualizado";
        solicitud.DocumentoLicencia = "PLT-0001";
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/1", solicitud);

        Assert.Equal(1, await LeerIdAsync(respuesta));
        Assert.Equal("Carlos Ruiz Actualizado", (await ObtenerGuardadoAsync(1)).Nombre);
    }

    [Fact]
    public async Task UpdateWithAnotherDriversLicenseDocumentReturnsConflict()
    {
        GuardarConductorRequestDto solicitud = CrearSolicitud();
        solicitud.DocumentoLicencia = "PLT-0002";
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un conductor con el documento de licencia PLT-0002.");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingDriverReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/conductores/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/conductores/9999", CrearSolicitud());
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/conductores/9999");
        }

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El conductor 9999 no existe.");
    }

    [Fact]
    public async Task DeleteDriverReturnsNoContentAndRemovesIt()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/conductores/3");
        HttpResponseMessage detalle = await _clienteHttp.GetAsync("/api/v1/conductores/3");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        await AssertBusinessErrorAsync(detalle, HttpStatusCode.NotFound, 404, "El conductor 3 no existe.");
    }

    [Fact]
    public async Task OpenApiContainsFiveDriverOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/conductores").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/conductores").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/conductores/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/conductores/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/conductores/{id}").TryGetProperty("delete", out _));
    }

    private void RestablecerConductores()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Conductores.RemoveRange(contexto.Conductores);
        contexto.Conductores.AddRange(
            new Conductor
            {
                Id = 1,
                Nombre = "Carlos Ruiz",
                DocumentoLicencia = "PLT-0001",
                VencimientoLicencia = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Habilitado = true,
                Activo = true
            },
            new Conductor
            {
                Id = 2,
                Nombre = "Maria Quispe",
                DocumentoLicencia = "PLT-0002",
                VencimientoLicencia = new DateTime(2020, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                Habilitado = true,
                Activo = true
            },
            new Conductor
            {
                Id = 3,
                Nombre = "Jose Flores",
                DocumentoLicencia = "PLT-0003",
                VencimientoLicencia = new DateTime(2098, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                Habilitado = false,
                Activo = true,
                Observaciones = "Sin habilitacion"
            });
        contexto.SaveChanges();
    }

    private static GuardarConductorRequestDto CrearSolicitud()
    {
        return new GuardarConductorRequestDto
        {
            Nombre = "Conductor",
            DocumentoLicencia = "PLT-NUE-1",
            VencimientoLicencia = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static string CrearJson(string documentoLicencia, string vencimiento)
    {
        return "{\"nombre\":\"Conductor\",\"documentoLicencia\":\"" + documentoLicencia
            + "\",\"vencimientoLicencia\":\"" + vencimiento + "\",\"habilitado\":true,\"activo\":true}";
    }

    private async Task<HttpResponseMessage> EnviarJsonAsync(HttpMethod metodo, string ruta, string json)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(metodo, ruta)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _clienteHttp.SendAsync(peticion);
    }

    private async Task AssertCreateBadRequestAsync(GuardarConductorRequestDto solicitud, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/conductores", solicitud);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    private static async Task<int> LeerIdAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.NotNull(resultado);

        return resultado["id"];
    }

    private async Task<Conductor> ObtenerGuardadoAsync(int id)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Conductor? conductor = await contexto.Conductores.SingleOrDefaultAsync(entidad => entidad.Id == id);

        Assert.NotNull(conductor);

        return conductor;
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
