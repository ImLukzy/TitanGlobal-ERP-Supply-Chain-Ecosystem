using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;

namespace TitanGlobal.Api.Tests.Modulo22_CategoriasProducto;

[Collection("Modulo04")]
public class CategoriasProductoReactivarApiTests
{
    private const string RutaBase = "/api/v1/categorias-producto";

    private readonly HttpClient _cliente;

    public CategoriasProductoReactivarApiTests(OrdenesCompraApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
    }

    [Fact]
    public async Task DesactivarYReactivarCategoria_ActualizaEstadoYRechazaRepetido()
    {
        CrearCategoriaProductoDto solicitud = new CrearCategoriaProductoDto
        {
            Nombre = "Periféricos Inalámbricos",
            Descripcion = "Accesorios sin cables"
        };

        HttpResponseMessage respuestaCrear = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        Assert.Equal(HttpStatusCode.OK, respuestaCrear.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await respuestaCrear.Content.ReadAsStringAsync());
        int id = documento.RootElement.GetProperty("id").GetInt32();

        // Unicidad sin distinguir mayúsculas ni espacios -> 409
        HttpResponseMessage respuestaDuplicada = await _cliente.PostAsJsonAsync(RutaBase, new CrearCategoriaProductoDto
        {
            Nombre = "  PERIFÉRICOS INALÁMBRICOS  "
        });
        Assert.Equal(HttpStatusCode.Conflict, respuestaDuplicada.StatusCode);

        // Reactivar categoría ya activa -> 409
        HttpResponseMessage respuestaYaActiva = await _cliente.PostAsJsonAsync($"{RutaBase}/{id}/reactivar", new { });
        Assert.Equal(HttpStatusCode.Conflict, respuestaYaActiva.StatusCode);

        // Desactivar -> 204
        HttpResponseMessage respuestaBaja = await _cliente.DeleteAsync($"{RutaBase}/{id}");
        Assert.Equal(HttpStatusCode.NoContent, respuestaBaja.StatusCode);

        // Reactivar categoría inactiva -> 204
        HttpResponseMessage respuestaReactivar = await _cliente.PostAsJsonAsync($"{RutaBase}/{id}/reactivar", new { });
        Assert.Equal(HttpStatusCode.NoContent, respuestaReactivar.StatusCode);

        CategoriaProductoDto? reactivada = await _cliente.GetFromJsonAsync<CategoriaProductoDto>($"{RutaBase}/{id}");
        Assert.NotNull(reactivada);
        Assert.True(reactivada.Activa);

        // Reactivar categoría inexistente -> 404
        HttpResponseMessage respuestaInexistente = await _cliente.PostAsJsonAsync($"{RutaBase}/999999/reactivar", new { });
        Assert.Equal(HttpStatusCode.NotFound, respuestaInexistente.StatusCode);
    }
}
