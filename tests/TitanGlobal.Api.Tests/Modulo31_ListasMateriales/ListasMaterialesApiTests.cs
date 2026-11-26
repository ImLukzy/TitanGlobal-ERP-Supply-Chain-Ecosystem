using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo31_ListasMateriales;

[Collection("Modulo04")]
public class ListasMaterialesApiTests
{
    private const string Ruta = "/api/v1/listas-materiales";

    private readonly HttpClient _cliente;

    public ListasMaterialesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DetallesListaMaterial.RemoveRange(contexto.DetallesListaMaterial);
        contexto.ListasMateriales.RemoveRange(contexto.ListasMateriales);
        contexto.SaveChanges();
    }

    [Fact]
    public async Task AltaYEdicionReemplazanTodoElDetalleYConservanPrecision()
    {
        int id = await CrearAsync(1, 1,
            new ComponenteListaMaterialRequestDto(4, 1m, "kg"),
            new ComponenteListaMaterialRequestDto(3, 0.000001m, "  unidad  "));

        ListaMaterialDetalleDto creada = await ObtenerAsync(id);
        Assert.Equal("Laptop Pro", creada.ProductoNombre);
        Assert.Equal("Borrador", creada.Estado);
        Assert.Equal(new[] { 3, 4 }, creada.Componentes.Select(componente => componente.ComponenteProductoId));
        Assert.Equal(new ComponenteListaMaterialDto(3, "Mouse Óptico", 0.000001m, "unidad"), creada.Componentes[0]);

        HttpResponseMessage invalida = await _cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new EditarListaMaterialRequestDto(new List<ComponenteListaMaterialRequestDto?>()));
        await VerificarErrorAsync(invalida, HttpStatusCode.BadRequest,
            "La lista de materiales debe tener entre 1 y 100 componentes.");
        Assert.Equal(creada.Componentes, (await ObtenerAsync(id)).Componentes);

        EditarListaMaterialRequestDto edicion = new EditarListaMaterialRequestDto(
            new List<ComponenteListaMaterialRequestDto?>
            {
                new ComponenteListaMaterialRequestDto(5, 2m, "unidad"),
                new ComponenteListaMaterialRequestDto(3, 999999999999.999999m, "kg")
            });
        HttpResponseMessage editada = await _cliente.PutAsJsonAsync($"{Ruta}/{id}", edicion);
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(id, (await editada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());

        ListaMaterialDetalleDto resultado = await ObtenerAsync(id);
        Assert.Equal(1, resultado.Version);
        Assert.Equal(1, resultado.ProductoId);
        Assert.Equal(new[]
        {
            new ComponenteListaMaterialDto(3, "Mouse Óptico", 999999999999.999999m, "kg"),
            new ComponenteListaMaterialDto(5, "Monitor 27", 2m, "unidad")
        }, resultado.Componentes);
    }

    [Fact]
    public async Task VersionesActivaYRetiradaConservanHistorialYProtegenEscrituras()
    {
        int primera = await CrearAsync(1, 1);
        int segunda = await CrearAsync(1, 2);
        HttpResponseMessage duplicada = await _cliente.PostAsJsonAsync(Ruta,
            new CrearListaMaterialRequestDto(1, 1, ComponentesValidos()));
        await VerificarErrorAsync(duplicada, HttpStatusCode.Conflict, "Ya existe la versión 1 para el producto 1.");

        await VerificarErrorAsync(await _cliente.PostAsync($"{Ruta}/{primera}/retirar", null),
            HttpStatusCode.Conflict, "Solo se pueden retirar listas de materiales activas.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{primera}/activar", null)).StatusCode);
        await VerificarErrorAsync(await _cliente.PostAsync($"{Ruta}/{segunda}/activar", null),
            HttpStatusCode.Conflict, "El producto 1 ya tiene una lista de materiales activa.");
        await VerificarErrorAsync(await _cliente.PostAsync($"{Ruta}/{primera}/activar", null),
            HttpStatusCode.Conflict, "Solo se pueden activar listas de materiales en borrador.");
        await VerificarErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{primera}",
            new EditarListaMaterialRequestDto(ComponentesValidos())), HttpStatusCode.Conflict,
            "Solo se pueden editar listas de materiales en borrador.");

        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{primera}/retirar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{segunda}/activar", null)).StatusCode);
        await VerificarErrorAsync(await _cliente.PostAsync($"{Ruta}/{primera}/activar", null),
            HttpStatusCode.Conflict, "Solo se pueden activar listas de materiales en borrador.");
        await VerificarErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{primera}",
            new EditarListaMaterialRequestDto(ComponentesValidos())), HttpStatusCode.Conflict,
            "Solo se pueden editar listas de materiales en borrador.");
        ListaMaterialDetalleDto retirada = await ObtenerAsync(primera);
        Assert.Equal("Retirada", retirada.Estado);
        Assert.Equal(3, Assert.Single(retirada.Componentes).ComponenteProductoId);
        Assert.Equal("Activa", (await ObtenerAsync(segunda)).Estado);
    }

    [Fact]
    public async Task ListadoAplicaFiltrosCombinadosOrdenYPaginacionEstable()
    {
        await CrearAsync(2, 1);
        await CrearAsync(1, 1);
        await CrearAsync(1, 2);
        PaginaListasMaterialesDto? pagina = await _cliente.GetFromJsonAsync<PaginaListasMaterialesDto>(
            $"{Ruta}?productoId=1&estado=bOrRaDoR&pagina=2&tamanoPagina=1");
        Assert.NotNull(pagina);
        Assert.Equal(2, pagina.Total);
        Assert.Equal(2, pagina.Pagina);
        Assert.Equal(1, pagina.TamanoPagina);
        ListaMaterialListadoDto lista = Assert.Single(pagina.Items);
        Assert.Equal(1, lista.Version);
        Assert.Equal("Laptop Pro", lista.ProductoNombre);
        Assert.Equal(1, lista.TotalComponentes);

        PaginaListasMaterialesDto? todas = await _cliente.GetFromJsonAsync<PaginaListasMaterialesDto>(Ruta);
        Assert.NotNull(todas);
        Assert.Equal(new[] { (1, 2), (1, 1), (2, 1) },
            todas.Items.Select(item => (item.ProductoId, item.Version)));
        PaginaListasMaterialesDto? vacia = await _cliente.GetFromJsonAsync<PaginaListasMaterialesDto>(
            $"{Ruta}?pagina={int.MaxValue}");
        Assert.NotNull(vacia);
        Assert.Empty(vacia.Items);
        Assert.Equal(3, vacia.Total);
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?estado=Inactiva", "El estado de la lista de materiales no es válido.")]
    [InlineData("?productoId=0", "El identificador del producto debe ser mayor que 0.")]
    public async Task ListadoInvalidoDevuelveMensajeLiteral(string consulta, string mensaje)
    {
        await VerificarErrorAsync(await _cliente.GetAsync(Ruta + consulta), HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [InlineData(0, 1, "El identificador del producto debe ser mayor que 0.")]
    [InlineData(999, 1, "El producto 999 no existe.")]
    [InlineData(1, 0, "La versión debe ser mayor que 0.")]
    public async Task AltaValidaCabecera(int productoId, int version, string mensaje)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearListaMaterialRequestDto(productoId, version, ComponentesValidos()));
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [InlineData("null", "La lista de materiales debe tener entre 1 y 100 componentes.")]
    [InlineData("[]", "La lista de materiales debe tener entre 1 y 100 componentes.")]
    [InlineData("[null]", "El identificador del componente debe ser mayor que 0.")]
    [InlineData("""[{"componenteProductoId":0}]""", "El identificador del componente debe ser mayor que 0.")]
    [InlineData("""[{"componenteProductoId":1}]""", "El producto fabricado no puede ser componente de su propia lista.")]
    [InlineData("""[{"componenteProductoId":3,"cantidad":1,"unidadMedida":"kg"},{"componenteProductoId":3}]""", "El componente 3 está repetido.")]
    [InlineData("""[{"componenteProductoId":3,"cantidad":0}]""", "La cantidad debe ser mayor que 0 y no superar 999999999999.999999.")]
    [InlineData("""[{"componenteProductoId":3,"cantidad":1000000000000}]""", "La cantidad debe ser mayor que 0 y no superar 999999999999.999999.")]
    [InlineData("""[{"componenteProductoId":3,"cantidad":1.0000001}]""", "La cantidad admite como máximo 6 decimales.")]
    [InlineData("""[{"componenteProductoId":3,"cantidad":1,"unidadMedida":"  "}]""", "La unidad de medida es obligatoria.")]
    [InlineData("""[{"componenteProductoId":3,"cantidad":1,"unidadMedida":"123456789012345678901"}]""", "La unidad de medida no puede superar 20 caracteres.")]
    [InlineData("""[{"componenteProductoId":999,"cantidad":1,"unidadMedida":"kg"}]""", "El producto componente 999 no existe.")]
    public async Task AltaValidaComponentesEnOrden(string componentes, string mensaje)
    {
        using StringContent cuerpo = new StringContent(
            $$"""{"productoId":1,"version":1,"componentes":{{componentes}}}""", Encoding.UTF8, "application/json");
        await VerificarErrorAsync(await _cliente.PostAsync(Ruta, cuerpo), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task AltaRechazaMasDeCienComponentes()
    {
        List<ComponenteListaMaterialRequestDto?> componentes = Enumerable.Repeat<ComponenteListaMaterialRequestDto?>(
            new ComponenteListaMaterialRequestDto(3, 1m, "kg"), 101).ToList();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearListaMaterialRequestDto(1, 1, componentes));
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest,
            "La lista de materiales debe tener entre 1 y 100 componentes.");
    }

    [Fact]
    public async Task OperacionesPorIdDevuelven404ConMensajeLiteral()
    {
        const string mensaje = "La lista de materiales 999 no existe.";
        await VerificarErrorAsync(await _cliente.GetAsync($"{Ruta}/999"), HttpStatusCode.NotFound, mensaje);
        await VerificarErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/999",
            new EditarListaMaterialRequestDto(null)), HttpStatusCode.NotFound, mensaje);
        await VerificarErrorAsync(await _cliente.PostAsync($"{Ruta}/999/activar", null), HttpStatusCode.NotFound, mensaje);
        await VerificarErrorAsync(await _cliente.PostAsync($"{Ruta}/999/retirar", null), HttpStatusCode.NotFound, mensaje);
    }

    [Fact]
    public async Task ListadoExigeToken()
    {
        _cliente.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync(Ruta)).StatusCode);
    }

    private async Task<int> CrearAsync(int productoId, int version, params ComponenteListaMaterialRequestDto[] componentes)
    {
        List<ComponenteListaMaterialRequestDto?> lineas = componentes.Length == 0
            ? ComponentesValidos()
            : componentes.Cast<ComponenteListaMaterialRequestDto?>().ToList();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearListaMaterialRequestDto(productoId, version, lineas));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        int id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.EndsWith($"{Ruta}/{id}", respuesta.Headers.Location?.ToString());
        return id;
    }

    private async Task<ListaMaterialDetalleDto> ObtenerAsync(int id)
    {
        ListaMaterialDetalleDto? resultado = await _cliente.GetFromJsonAsync<ListaMaterialDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(resultado);
        return resultado;
    }

    private static List<ComponenteListaMaterialRequestDto?> ComponentesValidos()
    {
        return new List<ComponenteListaMaterialRequestDto?> { new ComponenteListaMaterialRequestDto(3, 1m, "unidad") };
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement error = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, error.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, error.GetProperty("Message").GetString());
        Assert.False(error.TryGetProperty("Detail", out _));
    }
}
