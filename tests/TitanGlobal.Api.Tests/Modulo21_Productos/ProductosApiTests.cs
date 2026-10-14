using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;

namespace TitanGlobal.Api.Tests.Modulo21_Productos;

[Collection("Modulo04")]
public class ProductosApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ProductosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
    }

    [Fact]
    public async Task ListFiltersByNameCategoryAndPriceAndPaginates()
    {
        PaginaProductosDto? porNombre = await _clienteHttp.GetFromJsonAsync<PaginaProductosDto>(
            "/api/v1/productos?busqueda=lApToP");
        PaginaProductosDto? porCategoria = await _clienteHttp.GetFromJsonAsync<PaginaProductosDto>(
            "/api/v1/productos?categoria=cOmPuTo");
        PaginaProductosDto? porPrecio = await _clienteHttp.GetFromJsonAsync<PaginaProductosDto>(
            "/api/v1/productos?precioMin=100&precioMax=1000");
        PaginaProductosDto? paginaDos = await _clienteHttp.GetFromJsonAsync<PaginaProductosDto>(
            "/api/v1/productos?pagina=2&tamanoPagina=3");

        Assert.NotNull(porNombre);
        Assert.Equal(3, porNombre.Total);
        Assert.Equal(new[] { 7, 2, 1 }, porNombre.Items.Select(producto => producto.Id).ToArray());
        Assert.NotNull(porCategoria);
        Assert.Equal(3, porCategoria.Total);
        Assert.All(porCategoria.Items, producto => Assert.Equal("Computo", producto.Categoria));
        Assert.NotNull(porPrecio);
        Assert.Equal(5, porPrecio.Total);
        Assert.Equal(new[] { 10, 5, 8, 9, 4 }, porPrecio.Items.Select(producto => producto.Id).ToArray());
        Assert.NotNull(paginaDos);
        Assert.Equal(10, paginaDos.Total);
        Assert.Equal(new[] { 1, 10, 5 }, paginaDos.Items.Select(producto => producto.Id).ToArray());
    }

    [Theory]
    [InlineData("/api/v1/productos?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("/api/v1/productos?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("/api/v1/productos?precioMin=-1", "Los precios del filtro no pueden ser negativos.")]
    [InlineData("/api/v1/productos?precioMax=-1", "Los precios del filtro no pueden ser negativos.")]
    [InlineData("/api/v1/productos?precioMin=10&precioMax=5", "El precio mínimo no puede ser mayor que el máximo.")]
    public async Task InvalidFiltersReturnBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsExactSalesIndicatorsAndZeroWhenNotSold()
    {
        ProductoDetalleDto? vendido = await _clienteHttp.GetFromJsonAsync<ProductoDetalleDto>("/api/v1/productos/1");
        ProductoDetalleDto? noVendido = await _clienteHttp.GetFromJsonAsync<ProductoDetalleDto>("/api/v1/productos/2");

        Assert.NotNull(vendido);
        Assert.Equal(1, vendido.UnidadesVendidas);
        Assert.Equal(2500m, vendido.MontoVendido);
        Assert.Equal(1, vendido.ClientesDistintos);
        Assert.NotNull(noVendido);
        Assert.Equal(0, noVendido.UnidadesVendidas);
        Assert.Equal(0m, noVendido.MontoVendido);
        Assert.Equal(0, noVendido.ClientesDistintos);
    }

    [Fact]
    public async Task DetailCountsDistinctCustomersAndQuantityForMouse()
    {
        ProductoDetalleDto? producto = await _clienteHttp.GetFromJsonAsync<ProductoDetalleDto>("/api/v1/productos/3");

        Assert.NotNull(producto);
        Assert.Equal(4, producto.UnidadesVendidas);
        Assert.Equal(200m, producto.MontoVendido);
        Assert.Equal(2, producto.ClientesDistintos);
    }

    [Fact]
    public async Task CategoriesAreDistinctAndSorted()
    {
        List<string>? categorias = await _clienteHttp.GetFromJsonAsync<List<string>>("/api/v1/productos/categorias");

        Assert.NotNull(categorias);
        Assert.Equal(new[] { "Accesorios", "Computo", "Oficina", "Pantallas", "Perifericos" }, categorias);
    }

    [Fact]
    public async Task SummaryReturnsExactCountsAveragesAndSalesRanking()
    {
        ResumenProductosDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenProductosDto>("/api/v1/productos/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(10, resumen.TotalProductos);
        Assert.Equal(3, resumen.ProductosSinDescripcion);
        Assert.Equal(994.5m, resumen.PrecioPromedio);
        Assert.Equal(new[] { "Accesorios", "Computo", "Oficina", "Pantallas", "Perifericos" },
            resumen.PorCategoria.Select(categoria => categoria.Categoria).ToArray());
        Assert.Equal(new[] { 1, 3, 1, 2, 3 }, resumen.PorCategoria.Select(categoria => categoria.TotalProductos).ToArray());
        Assert.Equal(new[] { 25m, 2400m, 800m, 775m, 370m / 3m },
            resumen.PorCategoria.Select(categoria => categoria.PrecioPromedio).ToArray());
        Assert.Equal(new[] { 25m, 3200m, 800m, 900m, 200m },
            resumen.PorCategoria.Select(categoria => categoria.PrecioMaximo).ToArray());
        Assert.Equal(new[] { 3, 7, 1, 5, 9 }, resumen.MasVendidos.Select(producto => producto.ProductoId).ToArray());
        Assert.Equal(new[] { 4, 1, 1, 1, 1 }, resumen.MasVendidos.Select(producto => producto.UnidadesVendidas).ToArray());
        Assert.Equal(new[] { 200m, 3200m, 2500m, 900m, 800m },
            resumen.MasVendidos.Select(producto => producto.MontoVendido).ToArray());
    }

    [Theory]
    [InlineData("", "Computo", 1, "El nombre del producto es obligatorio.")]
    [InlineData("Cliente válido", "", 1, "La categoría es obligatoria.")]
    [InlineData("Cliente válido", "   ", 1, "La categoría es obligatoria.")]
    [InlineData("Cliente válido", "Computo", 0, "El precio debe ser mayor que 0.")]
    [InlineData("Cliente válido", "Computo", -1, "El precio debe ser mayor que 0.")]
    public async Task InvalidProductFieldsReturnBadRequestWithoutCreatingRows(
        string nombre,
        string categoria,
        decimal precio,
        string mensajeEsperado)
    {
        long totalAntes = await ObtenerTotalProductosAsync();
        GuardarProductoRequestDto solicitud = new GuardarProductoRequestDto
        {
            Nombre = nombre,
            Categoria = categoria,
            Precio = precio
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/productos", solicitud);
        long totalDespues = await ObtenerTotalProductosAsync();

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensajeEsperado);
        Assert.Equal(totalAntes, totalDespues);
    }

    [Fact]
    public async Task NameLongerThanTwoHundredCharactersReturnsBadRequest()
    {
        GuardarProductoRequestDto solicitud = new GuardarProductoRequestDto
        {
            Nombre = new string('N', 201),
            Categoria = "Computo",
            Precio = 1m
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/productos", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre del producto no puede superar 200 caracteres.");
    }

    [Fact]
    public async Task CategoryLongerThanOneHundredCharactersReturnsBadRequest()
    {
        GuardarProductoRequestDto solicitud = new GuardarProductoRequestDto
        {
            Nombre = "Producto válido",
            Categoria = new string('C', 101),
            Precio = 1m
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/productos", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La categoría no puede superar 100 caracteres.");
    }

    [Fact]
    public async Task CreateTrimsNameAndCategoryAndStoresBlankDescriptionAsNull()
    {
        GuardarProductoRequestDto solicitud = new GuardarProductoRequestDto
        {
            Nombre = "  Producto Smoke Test  ",
            Categoria = "  Categoria Nueva  ",
            Precio = 12m,
            Descripcion = "   "
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/productos", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);

        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Producto? producto = await contexto.Productos.SingleOrDefaultAsync(entidad => entidad.Id == resultado["id"]);

        Assert.NotNull(producto);
        Assert.Equal("Producto Smoke Test", producto.Nombre);
        Assert.Equal("Categoria Nueva", producto.Categoria);
        Assert.Null(producto.Descripcion);
        contexto.Productos.Remove(producto);
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateChangesProductAndAllowsTheSameName()
    {
        GuardarProductoRequestDto solicitud = new GuardarProductoRequestDto
        {
            Nombre = "Laptop Pro",
            Categoria = "  Computo  ",
            Precio = 2600m,
            Descripcion = "Precio actualizado"
        };
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/productos/1", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado["id"]);

        GuardarProductoRequestDto restaurar = new GuardarProductoRequestDto
        {
            Nombre = "Laptop Pro",
            Categoria = "Computo",
            Precio = 2500m,
            Descripcion = "Laptop de alto rendimiento"
        };
        HttpResponseMessage restaurada = await _clienteHttp.PutAsJsonAsync("/api/v1/productos/1", restaurar);
        Assert.Equal(HttpStatusCode.OK, restaurada.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingProductReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/productos/9999");
        }
        else if (metodo == "PUT")
        {
            GuardarProductoRequestDto solicitud = new GuardarProductoRequestDto
            {
                Nombre = "Producto ausente",
                Categoria = "Computo",
                Precio = 1m
            };
            respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/productos/9999", solicitud);
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/productos/9999");
        }

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El producto 9999 no existe.");
    }

    [Fact]
    public async Task DeleteProductWithSalesReturnsConflict()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/productos/1");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "No se puede eliminar el producto 1 porque tiene ventas registradas.");
    }

    [Fact]
    public async Task DeleteProductWithoutSalesReturnsNoContent()
    {
        GuardarProductoRequestDto solicitud = new GuardarProductoRequestDto
        {
            Nombre = "Producto para eliminar",
            Categoria = "Pruebas",
            Precio = 1m
        };
        HttpResponseMessage creada = await _clienteHttp.PostAsJsonAsync("/api/v1/productos", solicitud);
        Dictionary<string, int>? resultado = await creada.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        Assert.NotNull(resultado);

        HttpResponseMessage eliminada = await _clienteHttp.DeleteAsync($"/api/v1/productos/{resultado["id"]}");

        Assert.Equal(HttpStatusCode.NoContent, eliminada.StatusCode);
    }

    [Fact]
    public async Task OpenApiContainsSevenProductOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        string documento = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using JsonDocument documentoJson = JsonDocument.Parse(documento);
        JsonElement rutas = documentoJson.RootElement.GetProperty("paths");
        Assert.True(rutas.GetProperty("/api/v1/productos").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/productos").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/productos/categorias").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/productos/resumen").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/productos/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/productos/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/productos/{id}").TryGetProperty("delete", out _));
    }

    private async Task<long> ObtenerTotalProductosAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        return await contexto.Productos.LongCountAsync();
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
