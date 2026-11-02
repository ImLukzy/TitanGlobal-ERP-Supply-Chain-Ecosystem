using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo23_Almacenes;

[Collection("Modulo04")]
public class AlmacenesApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public AlmacenesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerAlmacenes();
    }

    [Fact]
    public async Task ListSearchesByCodeAndNameAndAppliesFiltersAndPagination()
    {
        PaginaAlmacenesDto? porCodigo = await _clienteHttp.GetFromJsonAsync<PaginaAlmacenesDto>(
            "/api/v1/almacenes?busqueda=alm-lim-01");
        PaginaAlmacenesDto? porNombre = await _clienteHttp.GetFromJsonAsync<PaginaAlmacenesDto>(
            "/api/v1/almacenes?busqueda=arequipa");
        PaginaAlmacenesDto? filtros = await _clienteHttp.GetFromJsonAsync<PaginaAlmacenesDto>(
            "/api/v1/almacenes?ciudad=lImA&tipo=regional&activo=true&pagina=1&tamanoPagina=1");

        Assert.NotNull(porCodigo);
        Assert.Equal(new[] { "ALM-LIM-01" }, porCodigo.Items.Select(almacen => almacen.Codigo).ToArray());
        Assert.NotNull(porNombre);
        Assert.Equal(new[] { "ALM-ARE-01" }, porNombre.Items.Select(almacen => almacen.Codigo).ToArray());
        Assert.NotNull(filtros);
        Assert.Equal(1, filtros.Total);
        Assert.Equal(1, filtros.Pagina);
        Assert.Equal(1, filtros.TamanoPagina);
        Assert.Equal("ALM-LIM-02", Assert.Single(filtros.Items).Codigo);
    }

    [Theory]
    [InlineData("/api/v1/almacenes?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("/api/v1/almacenes?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("/api/v1/almacenes?tipo=Externo", "El tipo de almacén no es válido.")]
    public async Task InvalidQueryReturnsBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsAllFields()
    {
        AlmacenDetalleDto? almacen = await _clienteHttp.GetFromJsonAsync<AlmacenDetalleDto>("/api/v1/almacenes/1");

        Assert.NotNull(almacen);
        Assert.Equal("ALM-LIM-01", almacen.Codigo);
        Assert.Equal("Central Lima", almacen.Nombre);
        Assert.Equal("Central", almacen.Tipo);
        Assert.Equal("Av. Industrial 100", almacen.Direccion);
        Assert.Equal("Lima", almacen.Ciudad);
        Assert.Equal(500m, almacen.CapacidadM3);
        Assert.Equal("Ana Torres", almacen.Responsable);
        Assert.True(almacen.Activo);
    }

    [Fact]
    public async Task CitiesAreDistinctAndSorted()
    {
        List<string>? ciudades = await _clienteHttp.GetFromJsonAsync<List<string>>("/api/v1/almacenes/ciudades");

        Assert.NotNull(ciudades);
        Assert.Equal(new[] { "Arequipa", "Callao", "Lima", "Trujillo" }, ciudades);
    }

    [Fact]
    public async Task SummaryReturnsExactCountsCapacityAndOrdering()
    {
        ResumenAlmacenesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenAlmacenesDto>(
            "/api/v1/almacenes/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.TotalAlmacenes);
        Assert.Equal(4, resumen.Activos);
        Assert.Equal(1, resumen.Inactivos);
        Assert.Equal(1025m, resumen.CapacidadTotalM3);
        Assert.Equal(925m, resumen.CapacidadActivaM3);
        Assert.Equal(new[] { "Central", "Regional", "Transito" }, resumen.PorTipo.Select(tipo => tipo.Tipo).ToArray());
        Assert.Equal(new[] { 1, 3, 1 }, resumen.PorTipo.Select(tipo => tipo.TotalAlmacenes).ToArray());
        Assert.Equal(new[] { 500m, 450m, 75m }, resumen.PorTipo.Select(tipo => tipo.CapacidadM3).ToArray());
        Assert.Equal(new[] { "Lima", "Arequipa", "Callao", "Trujillo" }, resumen.PorCiudad.Select(ciudad => ciudad.Ciudad).ToArray());
        Assert.Equal(new[] { 2, 1, 1, 1 }, resumen.PorCiudad.Select(ciudad => ciudad.TotalAlmacenes).ToArray());
        Assert.Equal(new[] { 2, 1, 1, 0 }, resumen.PorCiudad.Select(ciudad => ciudad.Activos).ToArray());
    }

    [Theory]
    [InlineData("", "Almacen Uno", "Central", "Dirección", "Lima", 1, null, "El código es obligatorio.")]
    [InlineData("A", "Almacen Uno", "Central", "Dirección", "Lima", 1, null, "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("AA", "", "Central", "Dirección", "Lima", 1, null, "El nombre del almacén es obligatorio.")]
    [InlineData("AA", "Almacen Uno", "Otro", "Dirección", "Lima", 1, null, "El tipo de almacén no es válido.")]
    [InlineData("AA", "Almacen Uno", "Central", "", "Lima", 1, null, "La dirección es obligatoria.")]
    [InlineData("AA", "Almacen Uno", "Central", "Dirección", "", 1, null, "La ciudad es obligatoria.")]
    [InlineData("AA", "Almacen Uno", "Central", "Dirección", "Lima", 0, null, "La capacidad debe ser mayor que 0.")]
    [InlineData("AA", "Almacen Uno", "Central", "Dirección", "Lima", 1, "  ", null)]
    public async Task CreateValidatesRequiredFieldsAndRuleOrder(
        string codigo,
        string nombre,
        string tipo,
        string direccion,
        string ciudad,
        decimal capacidadM3,
        string? responsable,
        string? mensaje)
    {
        GuardarAlmacenRequestDto solicitud = CrearSolicitud(codigo, nombre, tipo, direccion, ciudad, capacidadM3);
        solicitud.Responsable = responsable;
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/almacenes", solicitud);

        if (mensaje is null)
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            return;
        }

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(5, await ContarAlmacenesAsync());
    }

    [Fact]
    public async Task OverlongValuesReturnLiteralErrorsWithoutChangingRows()
    {
        GuardarAlmacenRequestDto nombreLargo = CrearSolicitud("NEW-01", new string('N', 201), "Central", "Dirección", "Lima", 1);
        await AssertPostErrorAsync(nombreLargo, "El nombre del almacén no puede superar 200 caracteres.");

        GuardarAlmacenRequestDto direccionLarga = CrearSolicitud("NEW-02", "Almacén", "Central", new string('D', 301), "Lima", 1);
        await AssertPostErrorAsync(direccionLarga, "La dirección no puede superar 300 caracteres.");

        GuardarAlmacenRequestDto ciudadLarga = CrearSolicitud("NEW-03", "Almacén", "Central", "Dirección", new string('C', 101), 1);
        await AssertPostErrorAsync(ciudadLarga, "La ciudad no puede superar 100 caracteres.");

        GuardarAlmacenRequestDto responsableLargo = CrearSolicitud("NEW-04", "Almacén", "Central", "Dirección", "Lima", 1);
        responsableLargo.Responsable = new string('R', 101);
        await AssertPostErrorAsync(responsableLargo, "El responsable no puede superar 100 caracteres.");
    }

    [Fact]
    public async Task CreateNormalizesCodeTypeAndOptionalResponsible()
    {
        GuardarAlmacenRequestDto solicitud = CrearSolicitud("  alm-new-01 ", "  Depósito Nuevo  ", "rEgIoNaL", "  Calle 1  ", "  Lima  ", 25.5m);
        solicitud.Responsable = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/almacenes", solicitud);
        Dictionary<string, int>? creado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        AlmacenDetalleDto? detalle = creado is null
            ? null
            : await _clienteHttp.GetFromJsonAsync<AlmacenDetalleDto>($"/api/v1/almacenes/{creado["id"]}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        Assert.NotNull(detalle);
        Assert.Equal("ALM-NEW-01", detalle.Codigo);
        Assert.Equal("Depósito Nuevo", detalle.Nombre);
        Assert.Equal("Regional", detalle.Tipo);
        Assert.Equal("Calle 1", detalle.Direccion);
        Assert.Equal("Lima", detalle.Ciudad);
        Assert.Null(detalle.Responsable);
    }

    [Fact]
    public async Task DuplicateCodeConflictsAndUpdateMayKeepItsOwnCode()
    {
        GuardarAlmacenRequestDto duplicado = CrearSolicitud("alm-lim-01", "Duplicado", "Central", "Dirección", "Lima", 5);
        HttpResponseMessage respuestaDuplicada = await _clienteHttp.PostAsJsonAsync("/api/v1/almacenes", duplicado);
        await AssertBusinessErrorAsync(
            respuestaDuplicada,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un almacén con el código ALM-LIM-01.");

        GuardarAlmacenRequestDto actualizacion = CrearSolicitud("alm-lim-01", "Central Lima Editado", "Central", "Dirección", "Lima", 500);
        HttpResponseMessage respuestaActualizacion = await _clienteHttp.PutAsJsonAsync("/api/v1/almacenes/1", actualizacion);
        Dictionary<string, int>? resultado = await respuestaActualizacion.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuestaActualizacion.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado["id"]);
    }

    [Fact]
    public async Task UpdateDuplicateCodeDoesNotChangeExistingRow()
    {
        GuardarAlmacenRequestDto solicitud = CrearSolicitud("ALM-LIM-02", "No debe guardar", "Regional", "Calle", "Lima", 999);
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/almacenes/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un almacén con el código ALM-LIM-02.");
        AlmacenDetalleDto? almacen = await _clienteHttp.GetFromJsonAsync<AlmacenDetalleDto>("/api/v1/almacenes/1");
        Assert.NotNull(almacen);
        Assert.Equal("Central Lima", almacen.Nombre);
        Assert.Equal(500m, almacen.CapacidadM3);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingWarehouseReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/almacenes/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(
                "/api/v1/almacenes/9999",
                CrearSolicitud("ALM-NEW", "Nuevo", "Central", "Dirección", "Lima", 1));
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/almacenes/9999");
        }

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El almacén 9999 no existe.");
    }

    [Fact]
    public async Task DeleteReturnsNoContent()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/almacenes/5");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(4, await ContarAlmacenesAsync());
    }

    [Fact]
    public async Task AlmacenConZonaInactivaNoPuedeEliminarse()
    {
        using (IServiceScope scope = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            contexto.ZonasAlmacenaje.Add(new ZonaAlmacenaje
            {
                AlmacenId = 1,
                Codigo = "ZONA-01",
                Nombre = "Zona inactiva",
                Tipo = "Cuarentena",
                Temperatura = "Ambiente",
                CapacidadM3 = 1m,
                Activa = false
            });
            await contexto.SaveChangesAsync();
        }

        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/almacenes/1");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "No se puede eliminar el almacén 1 porque tiene zonas.");
        AlmacenDetalleDto? almacen = await _clienteHttp.GetFromJsonAsync<AlmacenDetalleDto>("/api/v1/almacenes/1");
        Assert.NotNull(almacen);
        Assert.Equal("ALM-LIM-01", almacen.Codigo);
    }

    [Fact]
    public async Task OpenApiContainsSevenWarehouseOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/almacenes").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/almacenes").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/almacenes/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/almacenes/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/almacenes/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/almacenes/ciudades").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/almacenes/resumen").TryGetProperty("get", out _));
    }

    private void RestablecerAlmacenes()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ZonasAlmacenaje.RemoveRange(contexto.ZonasAlmacenaje);
        contexto.Almacenes.RemoveRange(contexto.Almacenes);
        contexto.Almacenes.AddRange(
            CrearAlmacen(1, "ALM-LIM-01", "Central Lima", "Central", "Av. Industrial 100", "Lima", 500m, "Ana Torres", true),
            CrearAlmacen(2, "ALM-LIM-02", "Regional Lima", "Regional", "Jr. Logística 200", "Lima", 200m, "Luis Rojas", true),
            CrearAlmacen(3, "ALM-ARE-01", "Regional Arequipa", "Regional", "Av. Sur 300", "Arequipa", 150m, "María León", true),
            CrearAlmacen(4, "ALM-TRU-01", "Regional Trujillo", "Regional", "Av. Norte 400", "Trujillo", 100m, "Carlos Díaz", false),
            CrearAlmacen(5, "ALM-CAL-TR", "Tránsito Callao", "Transito", "Av. Puerto 500", "Callao", 75m, null, true));
        contexto.SaveChanges();
    }

    private static Almacen CrearAlmacen(
        int id,
        string codigo,
        string nombre,
        string tipo,
        string direccion,
        string ciudad,
        decimal capacidadM3,
        string? responsable,
        bool activo)
    {
        return new Almacen
        {
            Id = id,
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Direccion = direccion,
            Ciudad = ciudad,
            CapacidadM3 = capacidadM3,
            Responsable = responsable,
            Activo = activo
        };
    }

    private static GuardarAlmacenRequestDto CrearSolicitud(
        string codigo,
        string nombre,
        string tipo,
        string direccion,
        string ciudad,
        decimal capacidadM3)
    {
        return new GuardarAlmacenRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Direccion = direccion,
            Ciudad = ciudad,
            CapacidadM3 = capacidadM3
        };
    }

    private async Task AssertPostErrorAsync(GuardarAlmacenRequestDto solicitud, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/almacenes", solicitud);
        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(5, await ContarAlmacenesAsync());
    }

    private async Task<int> ContarAlmacenesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Almacenes.CountAsync();
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
