using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo01_Clientes;

[Collection("Modulo04")]
public class ClientesApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ClientesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
    }

    [Fact]
    public async Task ListFiltersByNameAndRucAndPaginatesInStableOrder()
    {
        PaginaClientesDto? porNombre = await _clienteHttp.GetFromJsonAsync<PaginaClientesDto>(
            "/api/v1/clientes?busqueda=acme&pagina=1&tamanoPagina=20");
        PaginaClientesDto? porRuc = await _clienteHttp.GetFromJsonAsync<PaginaClientesDto>(
            "/api/v1/clientes?busqueda=2022");
        PaginaClientesDto? paginaDos = await _clienteHttp.GetFromJsonAsync<PaginaClientesDto>(
            "/api/v1/clientes?pagina=2&tamanoPagina=2");

        Assert.NotNull(porNombre);
        Assert.Equal(1, porNombre.Total);
        Assert.Equal(new[] { 1 }, porNombre.Items.Select(cliente => cliente.Id).ToArray());
        Assert.NotNull(porRuc);
        Assert.Equal(1, porRuc.Total);
        Assert.Equal(new[] { 2 }, porRuc.Items.Select(cliente => cliente.Id).ToArray());
        Assert.NotNull(paginaDos);
        Assert.Equal(5, paginaDos.Total);
        Assert.Equal(2, paginaDos.Pagina);
        Assert.Equal(2, paginaDos.TamanoPagina);
        Assert.Equal(new[] { 5, 4 }, paginaDos.Items.Select(cliente => cliente.Id).ToArray());
    }

    [Theory]
    [InlineData("/api/v1/clientes?pagina=0&tamanoPagina=20", "La página debe ser mayor o igual a 1.")]
    [InlineData("/api/v1/clientes?pagina=1&tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task InvalidPaginationReturnsBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsExactOrderIndicatorsAndEmptyValues()
    {
        ClienteDetalleDto? clienteConOrdenes = await _clienteHttp.GetFromJsonAsync<ClienteDetalleDto>("/api/v1/clientes/1");
        ClienteDetalleDto? clienteSinOrdenes = await _clienteHttp.GetFromJsonAsync<ClienteDetalleDto>("/api/v1/clientes/5");

        Assert.NotNull(clienteConOrdenes);
        Assert.Equal(4, clienteConOrdenes.TotalOrdenes);
        Assert.Equal(10000m, clienteConOrdenes.MontoTotal);
        Assert.Equal(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), clienteConOrdenes.UltimaOrdenFecha);
        Assert.NotNull(clienteSinOrdenes);
        Assert.Equal(0, clienteSinOrdenes.TotalOrdenes);
        Assert.Equal(0m, clienteSinOrdenes.MontoTotal);
        Assert.Null(clienteSinOrdenes.UltimaOrdenFecha);
    }

    [Fact]
    public async Task SummaryReturnsExactCountsAndOrderedTopFive()
    {
        ResumenClientesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenClientesDto>("/api/v1/clientes/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.TotalClientes);
        Assert.Equal(4, resumen.ClientesConOrdenes);
        Assert.Equal(3, resumen.ClientesSinDescripcion);
        Assert.Equal(new[] { 1, 2, 4, 3 }, resumen.TopClientesPorMonto.Select(cliente => cliente.ClienteId).ToArray());
        Assert.Equal(new[] { 10000m, 2000m, 750m, 500m }, resumen.TopClientesPorMonto.Select(cliente => cliente.MontoTotal).ToArray());
        Assert.Equal(new[] { 4, 2, 1, 1 }, resumen.TopClientesPorMonto.Select(cliente => cliente.TotalOrdenes).ToArray());
    }

    [Theory]
    [InlineData("", "20123456789", "El nombre del cliente es obligatorio.")]
    [InlineData("   ", "20123456789", "El nombre del cliente es obligatorio.")]
    [InlineData("Cliente válido", "1234567890", "El RUC debe tener 11 dígitos numéricos.")]
    [InlineData("Cliente válido", "2012345678A", "El RUC debe tener 11 dígitos numéricos.")]
    public async Task InvalidClientDataReturnsBadRequestWithoutCreatingRows(
        string nombre,
        string ruc,
        string mensajeEsperado)
    {
        long totalAntes = await ObtenerTotalClientesAsync();
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = nombre,
            Ruc = ruc
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/clientes", solicitud);
        long totalDespues = await ObtenerTotalClientesAsync();

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensajeEsperado);
        Assert.Equal(totalAntes, totalDespues);
    }

    [Fact]
    public async Task NameAboveTwoHundredCharactersReturnsBadRequestWithoutCreatingRows()
    {
        long totalAntes = await ObtenerTotalClientesAsync();
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = new string('N', 201),
            Ruc = "20123456789"
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/clientes", solicitud);
        long totalDespues = await ObtenerTotalClientesAsync();

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre del cliente no puede superar 200 caracteres.");
        Assert.Equal(totalAntes, totalDespues);
    }

    [Fact]
    public async Task RucWithSurroundingSpacesIsTrimmedAndBlankDescriptionBecomesNull()
    {
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = "  Cliente de prueba  ",
            Ruc = " 20987654321 ",
            Descripcion = "   "
        };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/clientes", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);

        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Cliente? cliente = await contexto.Clientes.SingleOrDefaultAsync(entidad => entidad.Id == resultado["id"]);

        Assert.NotNull(cliente);
        Assert.Equal("Cliente de prueba", cliente.Nombre);
        Assert.Equal("20987654321", cliente.Ruc);
        Assert.Null(cliente.Descripcion);
        contexto.Clientes.Remove(cliente);
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task DuplicateRucReturnsConflictWithoutCreatingRows()
    {
        long totalAntes = await ObtenerTotalClientesAsync();
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = "Cliente duplicado",
            Ruc = " 20111111111 "
        };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/clientes", solicitud);
        long totalDespues = await ObtenerTotalClientesAsync();

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un cliente con el RUC 20111111111.");
        Assert.Equal(totalAntes, totalDespues);
    }

    [Fact]
    public async Task UpdateMayKeepItsOwnRucAndReturnsUpdatedId()
    {
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = "Acme Industrial Actualizada",
            Ruc = "20111111111",
            Descripcion = "Actualizada"
        };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/clientes/1", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado["id"]);

        GuardarClienteRequestDto restaurar = new GuardarClienteRequestDto
        {
            Nombre = "Acme Industrial",
            Ruc = "20111111111",
            Descripcion = "Cliente frecuente"
        };
        HttpResponseMessage restaurada = await _clienteHttp.PutAsJsonAsync("/api/v1/clientes/1", restaurar);
        Assert.Equal(HttpStatusCode.OK, restaurada.StatusCode);
    }

    [Fact]
    public async Task UpdateWithAnotherClientsRucReturnsConflict()
    {
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = "Beta Comercio",
            Ruc = "20222222222"
        };
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/clientes/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un cliente con el RUC 20222222222.");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingClientReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/clientes/9999");
        }
        else if (metodo == "PUT")
        {
            GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
            {
                Nombre = "Cliente ausente",
                Ruc = "20987654321"
            };
            respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/clientes/9999", solicitud);
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/clientes/9999");
        }

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El cliente 9999 no existe.");
    }

    [Fact]
    public async Task DeleteClientWithOrdersReturnsConflict()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/clientes/1");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "No se puede eliminar el cliente 1 porque tiene órdenes de compra.");
    }

    [Fact]
    public async Task DeleteClientWithoutOrdersReturnsNoContent()
    {
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = "Cliente para eliminar",
            Ruc = "20987654321"
        };
        HttpResponseMessage creada = await _clienteHttp.PostAsJsonAsync("/api/v1/clientes", solicitud);
        Dictionary<string, int>? resultado = await creada.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        Assert.NotNull(resultado);

        HttpResponseMessage eliminada = await _clienteHttp.DeleteAsync($"/api/v1/clientes/{resultado["id"]}");

        Assert.Equal(HttpStatusCode.NoContent, eliminada.StatusCode);
    }

    [Fact]
    public async Task OpenApiContainsSixCustomerOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        string documento = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("\"/api/v1/clientes\"", documento);
        Assert.Contains("\"/api/v1/clientes/resumen\"", documento);
        Assert.Contains("\"/api/v1/clientes/{id}\"", documento);
        using JsonDocument documentoJson = JsonDocument.Parse(documento);
        JsonElement rutas = documentoJson.RootElement.GetProperty("paths");
        Assert.True(rutas.GetProperty("/api/v1/clientes").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/clientes").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/clientes/resumen").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/clientes/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/clientes/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/clientes/{id}").TryGetProperty("delete", out _));
    }

    private async Task<long> ObtenerTotalClientesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        return await contexto.Clientes.LongCountAsync();
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
