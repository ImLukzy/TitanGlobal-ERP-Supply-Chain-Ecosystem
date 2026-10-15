using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Dtos;

namespace TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;

[CollectionDefinition("Modulo04", DisableParallelization = true)]
public class Modulo04Collection : ICollectionFixture<OrdenesCompraApiFactory>
{
}

[Collection("Modulo04")]
public class OrdenesCompraApiTests
{
    private readonly HttpClient _client;

    private readonly OrdenesCompraApiFactory _factory;

    public OrdenesCompraApiTests(OrdenesCompraApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Fact]
    public async Task PostCreatesAnOrderAndReturnsItsId()
    {
        CrearOrdenCompraRequestDto request = new CrearOrdenCompraRequestDto
        {
            NumeroOrden = "OC-TEST-001",
            ClienteId = 1,
            Total = 275m,
            Descripcion = "Orden agregada desde la prueba"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/ordenes-compra", request);
        Dictionary<string, int>? result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result["id"] > 0);

        using IServiceScope scope = _factory.Services.CreateScope();
        TitanGlobalDbContext context = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        OrdenCompra? createdOrder = await context.OrdenesCompra
            .SingleOrDefaultAsync(order => order.NumeroOrden == request.NumeroOrden);
        Assert.NotNull(createdOrder);
        context.OrdenesCompra.Remove(createdOrder);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ProductFilterUsesTextPriceAndDtoProjection()
    {
        List<ProductoResumenDto>? products = await _client.GetFromJsonAsync<List<ProductoResumenDto>>(
            "/api/v1/ordenes-compra/productos/filtro?termino=LAPTOP&minPrecio=1000");

        Assert.NotNull(products);
        Assert.Equal(new[] { 1, 2, 7 }, products.Select(product => product.Id).ToArray());
        Assert.Equal(new[] { 2500m, 1500m, 3200m }, products.Select(product => product.Precio).ToArray());
        Assert.Equal(new[] { "Laptop Pro", "Laptop Básica", "Laptop Air" }, products.Select(product => product.Nombre).ToArray());
    }

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-01-01T00:00:00")]
    [InlineData("2026-01-01T00:00:00Z")]
    public async Task SalesSumAcceptsDateValuesWithoutOffset(string date)
    {
        Dictionary<string, decimal>? result = await _client.GetFromJsonAsync<Dictionary<string, decimal>>(
            $"/api/v1/ordenes-compra/ventas/suma?fechaInicio={Uri.EscapeDataString(date)}");

        Assert.NotNull(result);
        Assert.Equal(10750m, result["totalVentas"]);
    }

    [Fact]
    public async Task MostExpensiveProductReturnsLaptopAir()
    {
        ProductoResumenDto? product = await _client.GetFromJsonAsync<ProductoResumenDto>(
            "/api/v1/ordenes-compra/productos/mas-caro");

        Assert.NotNull(product);
        Assert.Equal(7, product.Id);
        Assert.Equal("Laptop Air", product.Nombre);
        Assert.Equal(3200m, product.Precio);
    }

    [Fact]
    public async Task CustomerAverageIsExact()
    {
        Dictionary<string, decimal>? result = await _client.GetFromJsonAsync<Dictionary<string, decimal>>(
            "/api/v1/ordenes-compra/clientes/1/promedio");

        Assert.NotNull(result);
        Assert.Equal(1m, result["clienteId"]);
        Assert.Equal(2500m, result["promedioCompra"]);
    }

    [Fact]
    public async Task CustomerWithoutOrdersHasZeroAverage()
    {
        Dictionary<string, decimal>? result = await _client.GetFromJsonAsync<Dictionary<string, decimal>>(
            "/api/v1/ordenes-compra/clientes/5/promedio");

        Assert.NotNull(result);
        Assert.Equal(5m, result["clienteId"]);
        Assert.Equal(0m, result["promedioCompra"]);
    }

    [Fact]
    public async Task ProductsWithoutDescriptionAreExact()
    {
        List<ProductoResumenDto>? products = await _client.GetFromJsonAsync<List<ProductoResumenDto>>(
            "/api/v1/ordenes-compra/productos/sin-descripcion");

        Assert.NotNull(products);
        Assert.Equal(new[] { 4, 6, 9 }, products.Select(product => product.Id).ToArray());
        Assert.Equal(new[] { "Teclado Mecánico", "Cable HDMI", "Silla Ejecutiva" }, products.Select(product => product.Nombre).ToArray());
    }

    [Fact]
    public async Task CustomerOrderFrequencyHasExactRanking()
    {
        List<ClienteFrecuenciaDto>? ranking = await _client.GetFromJsonAsync<List<ClienteFrecuenciaDto>>(
            "/api/v1/ordenes-compra/clientes/frecuencia");

        Assert.NotNull(ranking);
        Assert.Equal(new[] { 1, 2, 3, 4 }, ranking.Select(item => item.ClienteId).ToArray());
        Assert.Equal(new[] { 4, 2, 1, 1 }, ranking.Select(item => item.TotalOrdenes).ToArray());
    }

    [Fact]
    public async Task HierarchicalOrdersContainExpectedParentAndChildren()
    {
        List<OrdenCompraJerarquicaDto>? orders = await _client.GetFromJsonAsync<List<OrdenCompraJerarquicaDto>>(
            "/api/v1/ordenes-compra/clientes/1/jerarquia");

        Assert.NotNull(orders);
        Assert.Equal(4, orders.Count);
        Assert.Equal("OC-001", orders[0].NumeroOrden);
        Assert.Equal("Acme Industrial", orders[0].NombreCliente);
        Assert.Equal(new[] { "Laptop Pro", "Mouse Óptico" }, orders[0].Detalles.Select(item => item.NombreProducto).ToArray());
        Assert.Equal(new[] { 1, 2 }, orders[0].Detalles.Select(item => item.Cantidad).ToArray());
    }

    [Fact]
    public async Task UniquePurchasedCategoriesAreExact()
    {
        List<string>? categories = await _client.GetFromJsonAsync<List<string>>(
            "/api/v1/ordenes-compra/clientes/1/categorias-unicas");

        Assert.NotNull(categories);
        Assert.Equal(new[] { "Computo", "Pantallas", "Perifericos" }, categories);
    }

    [Fact]
    public async Task ProductBuyersAreDistinctAndExact()
    {
        List<string>? buyers = await _client.GetFromJsonAsync<List<string>>(
            "/api/v1/ordenes-compra/productos/3/clientes-compradores");

        Assert.NotNull(buyers);
        Assert.Equal(new[] { "Acme Industrial", "Beta Comercio" }, buyers);
    }

    [Fact]
    public async Task BlockedRoleCannotCreateAnOrderWithValidBody()
    {
        CrearOrdenCompraRequestDto request = new CrearOrdenCompraRequestDto
        {
            NumeroOrden = "OC-BLOQUEADA",
            ClienteId = 1,
            Total = 100m
        };
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ordenes-compra");
        message.Headers.Add("X-User-Role", "Bloqueado");
        message.Content = JsonContent.Create(request);

        HttpResponseMessage response = await _client.SendAsync(message);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiContainsAllTenOrderRoutes()
    {
        HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
        string document = await response.Content.ReadAsStringAsync();
        string[] routes =
        {
            "\"/api/v1/ordenes-compra\"",
            "\"/api/v1/ordenes-compra/productos/filtro\"",
            "\"/api/v1/ordenes-compra/ventas/suma\"",
            "\"/api/v1/ordenes-compra/productos/mas-caro\"",
            "\"/api/v1/ordenes-compra/clientes/{clienteId}/promedio\"",
            "\"/api/v1/ordenes-compra/productos/sin-descripcion\"",
            "\"/api/v1/ordenes-compra/clientes/frecuencia\"",
            "\"/api/v1/ordenes-compra/clientes/{clienteId}/jerarquia\"",
            "\"/api/v1/ordenes-compra/clientes/{clienteId}/categorias-unicas\"",
            "\"/api/v1/ordenes-compra/productos/{productoId}/clientes-compradores\""
        };

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (string route in routes)
        {
            Assert.Contains(route, document);
        }
    }
}

public sealed class OrdenesCompraApiFactory : WebApplicationFactory<Program>
{
    private bool _databaseSeeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            services.AddDbContext<TitanGlobalDbContext>(options => options
                .UseInMemoryDatabase("Modulo04OrdenesTests")
                // EF InMemory ignores TransactionScope; PostgreSQL smoke verifies the atomic transaction behavior.
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }

    public void SeedDatabase()
    {
        if (_databaseSeeded)
        {
            return;
        }

        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext context = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        if (context.Clientes.Any())
        {
            _databaseSeeded = true;
            return;
        }

        Cliente[] clients = CreateClients();
        Producto[] products = CreateProducts();
        OrdenCompra[] orders = CreateOrders(clients, products);

        context.Clientes.AddRange(clients);
        context.Productos.AddRange(products);
        context.OrdenesCompra.AddRange(orders);
        context.SaveChanges();
        _databaseSeeded = true;
    }

    private static Cliente[] CreateClients()
    {
        return new[]
        {
            new Cliente { Id = 1, Nombre = "Acme Industrial", Ruc = "20111111111", Descripcion = "Cliente frecuente" },
            new Cliente { Id = 2, Nombre = "Beta Comercio", Ruc = "20222222222" },
            new Cliente { Id = 3, Nombre = "Gamma Manufactura", Ruc = "20333333333", Descripcion = "Cliente regional" },
            new Cliente { Id = 4, Nombre = "Delta Logística", Ruc = "20444444444" },
            new Cliente { Id = 5, Nombre = "Cliente sin órdenes", Ruc = "20555555555" }
        };
    }

    private static Producto[] CreateProducts()
    {
        return new[]
        {
            new Producto { Id = 1, Nombre = "Laptop Pro", Categoria = "Computo", Precio = 2500m, Descripcion = "Laptop de alto rendimiento" },
            new Producto { Id = 2, Nombre = "Laptop Básica", Categoria = "Computo", Precio = 1500m, Descripcion = "Laptop de oficina" },
            new Producto { Id = 3, Nombre = "Mouse Óptico", Categoria = "Perifericos", Precio = 50m, Descripcion = "Mouse USB" },
            new Producto { Id = 4, Nombre = "Teclado Mecánico", Categoria = "Perifericos", Precio = 200m },
            new Producto { Id = 5, Nombre = "Monitor 27", Categoria = "Pantallas", Precio = 900m, Descripcion = "Monitor QHD" },
            new Producto { Id = 6, Nombre = "Cable HDMI", Categoria = "Accesorios", Precio = 25m },
            new Producto { Id = 7, Nombre = "Laptop Air", Categoria = "Computo", Precio = 3200m, Descripcion = "Laptop ligera" },
            new Producto { Id = 8, Nombre = "Mouse Gamer", Categoria = "Perifericos", Precio = 120m, Descripcion = "Mouse RGB" },
            new Producto { Id = 9, Nombre = "Silla Ejecutiva", Categoria = "Oficina", Precio = 800m },
            new Producto { Id = 10, Nombre = "Monitor 24", Categoria = "Pantallas", Precio = 650m, Descripcion = "Monitor Full HD" }
        };
    }

    private static OrdenCompra[] CreateOrders(Cliente[] clients, Producto[] products)
    {
        return new[]
        {
            CreateOrder(1, "OC-001", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), 2000m, clients[0], new[] { (products[0], 1), (products[2], 2) }),
            CreateOrder(2, "OC-002", new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc), 3000m, clients[0], new[] { (products[4], 1) }),
            CreateOrder(3, "OC-003", new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc), 1000m, clients[0], new[] { (products[6], 1) }),
            CreateOrder(4, "OC-004", new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), 4000m, clients[0], new[] { (products[2], 1) }),
            CreateOrder(5, "OC-005", new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), 500m, clients[1], new[] { (products[2], 1) }),
            CreateOrder(6, "OC-006", new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), 1500m, clients[1], new[] { (products[3], 1) }),
            CreateOrder(7, "OC-007", new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 500m, clients[2], new[] { (products[7], 1) }),
            CreateOrder(8, "OC-008", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), 750m, clients[3], new[] { (products[8], 1) })
        };
    }

    private static OrdenCompra CreateOrder(
        int id,
        string number,
        DateTime date,
        decimal total,
        Cliente client,
        (Producto Product, int Quantity)[] items)
    {
        OrdenCompra order = new OrdenCompra
        {
            Id = id,
            NumeroOrden = number,
            Fecha = date,
            Total = total,
            ClienteId = client.Id,
            Cliente = client
        };

        foreach ((Producto product, int quantity) in items)
        {
            order.Detalles.Add(new DetalleOrdenCompra
            {
                ProductoId = product.Id,
                Producto = product,
                Cantidad = quantity,
                PrecioUnitario = product.Precio
            });
        }

        return order;
    }
}
