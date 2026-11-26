using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

namespace TitanGlobal.Api.Tests.Modulo32_RutasProduccion;

public sealed class RutasProduccionApiTests : IClassFixture<RutasProduccionApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/rutas-produccion";
    private static readonly OperacionRutaProduccionRequestDto OperacionValida = new(1, "Montar", 1, 0m, 1.25m);
    private readonly HttpClient _cliente;

    public RutasProduccionApiTests(RutasProduccionApiFactory fabrica)
    {
        fabrica.PrepararDatos();
        _cliente = fabrica.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, fabrica.Services);
    }

    [Fact]
    public async Task AltaOrdenaLasOperacionesYEdicionReemplazaElDetalle()
    {
        OperacionRutaProduccionRequestDto segunda = new(2, " Revisar ", 1, 0.00m, 0.01m);
        int id = await CrearAsync(1, 1, segunda, OperacionValida);
        RutaProduccionDetalleDto creada = await ObtenerAsync(id);
        Assert.Equal("Producto uno", creada.ProductoNombre);
        Assert.Equal("Borrador", creada.Estado);
        Assert.Equal(new[] { 1, 2 }, creada.Operaciones.Select(operacion => operacion.Secuencia));
        Assert.Equal(new OperacionRutaProduccionDto(2, "Revisar", 1, "Ensamblaje", 0m, 0.01m), creada.Operaciones[1]);

        using HttpResponseMessage invalida = await EditarAsync(id, segunda);
        await VerificarErrorAsync(invalida, HttpStatusCode.BadRequest, "Las secuencias deben ser consecutivas y empezar en 1.");
        Assert.Equal(creada.Operaciones, (await ObtenerAsync(id)).Operaciones);

        OperacionRutaProduccionRequestDto cambio = new(1, "Empacar", 2, 99999999.99m, 99999999.99m);
        using HttpResponseMessage editada = await EditarAsync(id, cambio);
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(id, (await editada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        RutaProduccionDetalleDto reemplazada = await ObtenerAsync(id);
        Assert.Equal(1, reemplazada.ProductoId);
        Assert.Equal(1, reemplazada.Version);
        Assert.Equal(new OperacionRutaProduccionDto(1, "Empacar", 2, "Envasado", 99999999.99m, 99999999.99m),
            Assert.Single(reemplazada.Operaciones));

        using HttpResponseMessage ampliada = await EditarAsync(id, cambio, segunda);
        Assert.Equal(HttpStatusCode.OK, ampliada.StatusCode);
        Assert.Equal(new[] { 1, 2 }, (await ObtenerAsync(id)).Operaciones.Select(operacion => operacion.Secuencia));
    }

    [Fact]
    public async Task ListadoCombinaFiltrosYOrdenaPorProductoYVersion()
    {
        int versionUno = await CrearAsync(1, 1, OperacionValida);
        int versionDos = await CrearAsync(1, 2, OperacionValida);
        int otroProducto = await CrearAsync(2, 3, OperacionValida);
        using HttpResponseMessage activada = await _cliente.PostAsync($"{Ruta}/{versionUno}/activar", null);
        Assert.Equal(HttpStatusCode.OK, activada.StatusCode);

        PaginaRutasProduccionDto? primera = await _cliente.GetFromJsonAsync<PaginaRutasProduccionDto>($"{Ruta}?tamanoPagina=2");
        PaginaRutasProduccionDto? segunda = await _cliente.GetFromJsonAsync<PaginaRutasProduccionDto>($"{Ruta}?pagina=2&tamanoPagina=2");
        PaginaRutasProduccionDto? combinada = await _cliente.GetFromJsonAsync<PaginaRutasProduccionDto>($"{Ruta}?productoId=1&estado=bORRADOR");
        PaginaRutasProduccionDto? vacia = await _cliente.GetFromJsonAsync<PaginaRutasProduccionDto>($"{Ruta}?pagina=2147483647&tamanoPagina=100");

        Assert.NotNull(primera);
        Assert.Equal(3, primera.Total);
        Assert.Equal(new[] { versionDos, versionUno }, primera.Items.Select(ruta => ruta.Id));
        Assert.NotNull(segunda);
        Assert.Equal(3, segunda.Total);
        Assert.Equal(2, segunda.Pagina);
        Assert.Equal(otroProducto, Assert.Single(segunda.Items).Id);
        Assert.NotNull(combinada);
        Assert.Equal(new RutaProduccionListadoDto(versionDos, 1, "Producto uno", 2, "Borrador", 1),
            Assert.Single(combinada.Items));
        Assert.Equal(1, combinada.Total);
        Assert.NotNull(vacia);
        Assert.Equal(3, vacia.Total);
        Assert.Empty(vacia.Items);
    }

    [Fact]
    public async Task ActivarYRetirarConservanHistorialYExigenEstadoValido()
    {
        int primera = await CrearAsync(1, 1, OperacionValida);
        int siguiente = await CrearAsync(1, 2, OperacionValida);
        await VerificarTransicionAsync(primera, "retirar", "Solo se pueden retirar rutas de producción activas.");
        await VerificarTransicionAsync(primera, "activar");
        await VerificarTransicionAsync(primera, "activar", "Solo se pueden activar rutas de producción en borrador.");
        await VerificarTransicionAsync(siguiente, "activar", "El producto 1 ya tiene una ruta de producción activa.");
        using HttpResponseMessage editarActiva = await EditarAsync(primera, OperacionValida);
        await VerificarErrorAsync(editarActiva, HttpStatusCode.Conflict, "Solo se pueden editar rutas de producción en borrador.");
        await VerificarTransicionAsync(primera, "retirar");
        await VerificarTransicionAsync(primera, "activar", "Solo se pueden activar rutas de producción en borrador.");
        await VerificarTransicionAsync(primera, "retirar", "Solo se pueden retirar rutas de producción activas.");
        await VerificarTransicionAsync(siguiente, "activar");

        RutaProduccionDetalleDto retirada = await ObtenerAsync(primera);
        Assert.Equal("Retirada", retirada.Estado);
        Assert.Equal("Montar", Assert.Single(retirada.Operaciones).Nombre);
        Assert.Equal("Activa", (await ObtenerAsync(siguiente)).Estado);
    }

    [Fact]
    public async Task ActivacionRechazaElPrimerCentroInactivoPorSecuencia()
    {
        int id = await CrearAsync(1, 1, OperacionValida with { Secuencia = 2, CentroTrabajoId = 4 },
            OperacionValida with { CentroTrabajoId = 3 });
        await VerificarTransicionAsync(id, "activar", "El centro de trabajo 3 está inactivo.");
        Assert.Equal("Borrador", (await ObtenerAsync(id)).Estado);
    }

    [Fact]
    public async Task VersionDuplicadaSoloSeRechazaParaElMismoProducto()
    {
        await CrearAsync(1, 1, OperacionValida);
        using HttpResponseMessage conflicto = await _cliente.PostAsJsonAsync(Ruta,
            new CrearRutaProduccionRequestDto(1, 1, new List<OperacionRutaProduccionRequestDto?> { OperacionValida }));
        await VerificarErrorAsync(conflicto, HttpStatusCode.Conflict, "Ya existe la versión 1 para el producto 1.");
        await CrearAsync(2, 1, OperacionValida);
    }

    [Theory]
    [InlineData("?pagina=0&tamanoPagina=0&productoId=0&estado=Otro", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=0&productoId=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?productoId=0&estado=Otro", "El identificador del producto debe ser mayor que 0.")]
    [InlineData("?estado=Otro", "El estado de la ruta de producción no es válido.")]
    public async Task ListadoInvalidoDevuelveMensajeLiteral(string filtros, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + filtros);
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [InlineData(0, 0, "El identificador del producto debe ser mayor que 0.")]
    [InlineData(99, 0, "El producto 99 no existe.")]
    [InlineData(1, 0, "La versión debe ser mayor que 0.")]
    public async Task AltaValidaProductoYVersionAntesDeOperaciones(int productoId, int version, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, new CrearRutaProduccionRequestDto(productoId, version, null));
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [MemberData(nameof(OperacionesInvalidas))]
    public async Task AltaYEdicionValidanOperaciones(List<OperacionRutaProduccionRequestDto?>? operaciones, string mensaje)
    {
        int id = await CrearAsync(1, 1, OperacionValida);
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(Ruta, new CrearRutaProduccionRequestDto(1, 2, operaciones));
        await VerificarErrorAsync(alta, HttpStatusCode.BadRequest, mensaje);
        using HttpResponseMessage edicion = await _cliente.PutAsJsonAsync($"{Ruta}/{id}", new EditarRutaProduccionRequestDto(operaciones));
        await VerificarErrorAsync(edicion, HttpStatusCode.BadRequest, mensaje);
        Assert.Equal("Montar", Assert.Single((await ObtenerAsync(id)).Operaciones).Nombre);
    }

    public static TheoryData<List<OperacionRutaProduccionRequestDto?>?, string> OperacionesInvalidas()
    {
        OperacionRutaProduccionRequestDto operacion = OperacionValida;
        return new TheoryData<List<OperacionRutaProduccionRequestDto?>?, string>
        {
            { null, "La ruta de producción debe tener entre 1 y 100 operaciones." },
            { [], "La ruta de producción debe tener entre 1 y 100 operaciones." },
            { Enumerable.Repeat<OperacionRutaProduccionRequestDto?>(operacion, 101).ToList(), "La ruta de producción debe tener entre 1 y 100 operaciones." },
            { [null], "La operación es obligatoria." },
            { [operacion with { Secuencia = 0 }], "La secuencia debe estar entre 1 y 100." },
            { [operacion with { Secuencia = 101 }], "La secuencia debe estar entre 1 y 100." },
            { [operacion, operacion], "La secuencia 1 está repetida." },
            { [operacion with { Nombre = "  " }], "El nombre de la operación es obligatorio." },
            { [operacion with { Nombre = new string('a', 121) }], "El nombre de la operación no puede superar 120 caracteres." },
            { [operacion with { CentroTrabajoId = 0 }], "El identificador del centro de trabajo debe ser mayor que 0." },
            { [operacion with { CentroTrabajoId = 99 }], "El centro de trabajo 99 no existe." },
            { [operacion with { MinutosPreparacion = null }], "Los minutos de preparación deben estar entre 0 y 99999999.99." },
            { [operacion with { MinutosPreparacion = -0.01m }], "Los minutos de preparación deben estar entre 0 y 99999999.99." },
            { [operacion with { MinutosPreparacion = 100000000m }], "Los minutos de preparación deben estar entre 0 y 99999999.99." },
            { [operacion with { MinutosPreparacion = 1.001m }], "Los minutos de preparación admiten como máximo 2 decimales." },
            { [operacion with { MinutosPorUnidad = null }], "Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99." },
            { [operacion with { MinutosPorUnidad = 0m }], "Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99." },
            { [operacion with { MinutosPorUnidad = 100000000m }], "Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99." },
            { [operacion with { MinutosPorUnidad = 1.251m }], "Los minutos por unidad admiten como máximo 2 decimales." },
            { [operacion, operacion with { Secuencia = 3 }], "Las secuencias deben ser consecutivas y empezar en 1." }
        };
    }

    [Theory]
    [InlineData("GET", "/99")]
    [InlineData("PUT", "/99")]
    [InlineData("POST", "/99/activar")]
    [InlineData("POST", "/99/retirar")]
    public async Task RutaInexistenteDevuelve404(string verbo, string sufijo)
    {
        using HttpRequestMessage solicitud = new HttpRequestMessage(new HttpMethod(verbo), Ruta + sufijo);
        if (verbo == "PUT")
        {
            solicitud.Content = JsonContent.Create(new EditarRutaProduccionRequestDto(null));
        }

        using HttpResponseMessage respuesta = await _cliente.SendAsync(solicitud);
        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "La ruta de producción 99 no existe.");
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("GET", "/1")]
    [InlineData("POST", "")]
    [InlineData("PUT", "/1")]
    [InlineData("POST", "/1/activar")]
    [InlineData("POST", "/1/retirar")]
    public async Task OperacionesRequierenJwt(string verbo, string sufijo)
    {
        _cliente.DefaultRequestHeaders.Authorization = null;
        using HttpRequestMessage solicitud = new HttpRequestMessage(new HttpMethod(verbo), Ruta + sufijo);
        if (verbo == "PUT" || sufijo.Length == 0 && verbo == "POST")
        {
            solicitud.Content = JsonContent.Create(new { });
        }

        using HttpResponseMessage respuesta = await _cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private async Task<int> CrearAsync(int productoId, int version, params OperacionRutaProduccionRequestDto[] operaciones)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearRutaProduccionRequestDto(productoId, version, new List<OperacionRutaProduccionRequestDto?>(operaciones)));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        int id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal($"{Ruta}/{id}", respuesta.Headers.Location?.AbsolutePath);
        return id;
    }

    private Task<HttpResponseMessage> EditarAsync(int id, params OperacionRutaProduccionRequestDto[] operaciones)
    {
        return _cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new EditarRutaProduccionRequestDto(new List<OperacionRutaProduccionRequestDto?>(operaciones)));
    }

    private async Task<RutaProduccionDetalleDto> ObtenerAsync(int id)
    {
        RutaProduccionDetalleDto? detalle = await _cliente.GetFromJsonAsync<RutaProduccionDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(detalle);
        return detalle;
    }

    private async Task VerificarTransicionAsync(int id, string accion, string? mensaje = null)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsync($"{Ruta}/{id}/{accion}", null);
        if (mensaje != null)
        {
            await VerificarErrorAsync(respuesta, HttpStatusCode.Conflict, mensaje);
            return;
        }

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(id, (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, cuerpo.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, cuerpo.GetProperty("Message").GetString());
        Assert.False(cuerpo.TryGetProperty("Detail", out JsonElement _));
    }

    public void Dispose()
    {
        _cliente.Dispose();
    }
}
