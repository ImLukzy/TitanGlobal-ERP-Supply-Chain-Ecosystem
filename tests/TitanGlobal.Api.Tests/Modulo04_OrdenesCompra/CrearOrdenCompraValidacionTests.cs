using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;

[Collection("Modulo04")]
public class CrearOrdenCompraValidacionTests
{
    private readonly HttpClient _client;

    private readonly OrdenesCompraApiFactory _factory;

    public CrearOrdenCompraValidacionTests(OrdenesCompraApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Theory]
    [InlineData("", 1, 1, "El número de orden es obligatorio.")]
    [InlineData("   ", 1, 1, "El número de orden es obligatorio.")]
    [InlineData("OC-INVALIDA-CLIENTE", 0, 1, "El cliente es obligatorio.")]
    [InlineData("OC-TOTAL-NEGATIVO", 1, -1, "El total no puede ser negativo.")]
    [InlineData("OC-CLIENTE-INEXISTENTE", 999, 1, "El cliente 999 no existe.")]
    public async Task InvalidOrderFieldsReturnBadRequestWithoutCreatingRows(
        string orderNumber,
        int clientId,
        decimal total,
        string expectedMessage)
    {
        long orderCountBefore = await GetOrderCountAsync();
        CrearOrdenCompraRequestDto request = new CrearOrdenCompraRequestDto
        {
            NumeroOrden = orderNumber,
            ClienteId = clientId,
            Total = total
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/ordenes-compra", request);
        JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        long orderCountAfter = await GetOrderCountAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, document.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(expectedMessage, document.RootElement.GetProperty("Message").GetString());
        Assert.False(document.RootElement.TryGetProperty("Detail", out _));
        Assert.Equal(orderCountBefore, orderCountAfter);
    }

    [Theory]
    [InlineData("OC-001")]
    [InlineData(" OC-001 ")]
    public async Task DuplicateOrderNumberReturnsConflictWithoutCreatingRows(string orderNumber)
    {
        long orderCountBefore = await GetOrderCountAsync();
        CrearOrdenCompraRequestDto request = new CrearOrdenCompraRequestDto
        {
            NumeroOrden = orderNumber,
            ClienteId = 1,
            Total = 1m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/ordenes-compra", request);
        JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        long orderCountAfter = await GetOrderCountAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, document.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(
            "Ya existe una orden de compra con el número OC-001.",
            document.RootElement.GetProperty("Message").GetString());
        Assert.False(document.RootElement.TryGetProperty("Detail", out _));
        Assert.Equal(orderCountBefore, orderCountAfter);
    }

    [Fact]
    public async Task ValidOrderNumberIsTrimmedAndCreated()
    {
        CrearOrdenCompraRequestDto request = new CrearOrdenCompraRequestDto
        {
            NumeroOrden = " OC-VALIDA-TRIM ",
            ClienteId = 1,
            Total = 25m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/ordenes-compra", request);
        Dictionary<string, int>? result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result["id"] > 0);

        using IServiceScope scope = _factory.Services.CreateScope();
        TitanGlobalDbContext context = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        OrdenCompra? createdOrder = await context.OrdenesCompra
            .SingleOrDefaultAsync(order => order.NumeroOrden == "OC-VALIDA-TRIM");
        Assert.NotNull(createdOrder);
        context.OrdenesCompra.Remove(createdOrder);
        await context.SaveChangesAsync();
    }

    private async Task<long> GetOrderCountAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        TitanGlobalDbContext context = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        return await context.OrdenesCompra.LongCountAsync();
    }
}
