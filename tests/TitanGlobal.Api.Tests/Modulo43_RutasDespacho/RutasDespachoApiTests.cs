using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo43_RutasDespacho;

[Collection("Modulo04")]
public class RutasDespachoApiTests
{
    private readonly HttpClient _cliente;

    public RutasDespachoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RutasDespacho.RemoveRange(db.RutasDespacho);
        db.SaveChanges();
        db.RutasDespacho.AddRange(
            new RutaDespacho { Id = 1, Codigo = "R01", Nombre = "Ruta Norte", Zona = "Norte", Origen = "Lima", Destino = "Trujillo", DistanciaKm = 550.25m, PeajeEstimado = 0, Moneda = "PEN", Activo = true },
            new RutaDespacho { Id = 2, Codigo = "R02", Nombre = "Ruta Sur", Zona = "Sur", Origen = "Lima", Destino = "Ica", DistanciaKm = 300, PeajeEstimado = 30, Moneda = "PEN", Activo = false },
            new RutaDespacho { Id = 3, Codigo = "R03", Nombre = "Ruta Internacional", Zona = "Norte", Origen = "Tumbes", Destino = "Guayaquil", DistanciaKm = 280, PeajeEstimado = 12.50m, Moneda = "USD", Activo = true });
        db.SaveChanges();
    }

    [Theory]
    [InlineData("Codigo", "El código de la ruta es obligatorio.")]
    [InlineData("Nombre", "El nombre de la ruta es obligatorio.")]
    [InlineData("Zona", "La zona es obligatoria.")]
    [InlineData("Origen", "El origen es obligatorio.")]
    [InlineData("Destino", "El destino es obligatorio.")]
    public async Task CamposObligatoriosEnAltaYEdicion(string campo, string mensaje)
    {
        GuardarRutaDespachoRequestDto datos = Solicitud();
        typeof(GuardarRutaDespachoRequestDto).GetProperty(campo)!.SetValue(datos, " ");
        await ValidacionEnAmbasOperaciones(datos, mensaje);
    }

    [Theory]
    [InlineData("Codigo", 20, "El código de la ruta no puede superar 20 caracteres.")]
    [InlineData("Nombre", 100, "El nombre de la ruta no puede superar 100 caracteres.")]
    [InlineData("Zona", 100, "La zona no puede superar 100 caracteres.")]
    [InlineData("Origen", 120, "El origen no puede superar 120 caracteres.")]
    [InlineData("Destino", 120, "El destino no puede superar 120 caracteres.")]
    [InlineData("Observaciones", 500, "Las observaciones no pueden superar 500 caracteres.")]
    public async Task LongitudesNoSeTruncan(string campo, int limite, string mensaje)
    {
        GuardarRutaDespachoRequestDto datos = Solicitud();
        typeof(GuardarRutaDespachoRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', limite + 1));
        await ValidacionEnAmbasOperaciones(datos, mensaje);
    }

    [Theory]
    [InlineData("DistanciaKm", "0", "La distancia debe ser mayor que cero.")]
    [InlineData("DistanciaKm", "-1", "La distancia debe ser mayor que cero.")]
    [InlineData("DistanciaKm", "100000000", "La distancia no puede superar 99999999.99 km.")]
    [InlineData("DistanciaKm", "1.001", "La distancia no puede tener más de 2 decimales.")]
    [InlineData("PeajeEstimado", "-0.01", "El peaje estimado no puede ser negativo.")]
    [InlineData("PeajeEstimado", "10000000000000000", "El peaje estimado no puede superar 9999999999999999.99.")]
    [InlineData("PeajeEstimado", "0.001", "El peaje estimado no puede tener más de 2 decimales.")]
    public async Task DecimalesFueraDeLimiteNoSeRedondean(string campo, string valor, string mensaje)
    {
        GuardarRutaDespachoRequestDto datos = Solicitud();
        typeof(GuardarRutaDespachoRequestDto).GetProperty(campo)!.SetValue(datos, decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture));
        await ValidacionEnAmbasOperaciones(datos, mensaje);
    }

    [Theory]
    [InlineData("")]
    [InlineData("P3N")]
    [InlineData("PENN")]
    [InlineData("PÉN")]
    public async Task MonedaExigeTresLetrasAscii(string moneda)
    { GuardarRutaDespachoRequestDto datos = Solicitud(); datos.Moneda = moneda; await ValidacionEnAmbasOperaciones(datos, "La moneda debe tener tres letras."); }

    [Fact]
    public async Task NormalizaTextosConservaFalseYAdmitePeajeCero()
    {
        GuardarRutaDespachoRequestDto datos = Solicitud();
        datos.Codigo = "  NUEVA  "; datos.Nombre = "  Nueva ruta  "; datos.Zona = "  Centro  "; datos.Origen = "  Callao  "; datos.Destino = "  Lima  "; datos.Moneda = " usd "; datos.Activo = false; datos.Observaciones = " "; datos.PeajeEstimado = 0;
        int id = await Id(await _cliente.PostAsJsonAsync("/api/v1/rutas-despacho", datos));
        RutaDespachoDetalleDto detalle = await Obtener(id);
        Assert.Equal("NUEVA", detalle.Codigo); Assert.Equal("Nueva ruta", detalle.Nombre); Assert.Equal("Centro", detalle.Zona); Assert.Equal("Callao", detalle.Origen); Assert.Equal("Lima", detalle.Destino); Assert.Equal("USD", detalle.Moneda); Assert.False(detalle.Activo); Assert.Null(detalle.Observaciones); Assert.Equal("0.00", detalle.PeajeEstimado);
        datos.Activo = true; datos.Observaciones = " Nota ";
        await Id(await _cliente.PutAsJsonAsync($"/api/v1/rutas-despacho/{id}", datos));
        detalle = await Obtener(id); Assert.True(detalle.Activo); Assert.Equal("Nota", detalle.Observaciones);
    }

    [Fact]
    public async Task LimitesExactosSeConservanComoTextoSinPerderCentavos()
    {
        GuardarRutaDespachoRequestDto datos = Solicitud(); datos.Codigo = new string('c', 20); datos.Nombre = new string('n', 100); datos.Zona = new string('z', 100); datos.Origen = new string('o', 120); datos.Destino = new string('d', 120); datos.Observaciones = new string('t', 500); datos.DistanciaKm = 99999999.99m; datos.PeajeEstimado = 9999999999999999.99m;
        int id = await Id(await _cliente.PostAsJsonAsync("/api/v1/rutas-despacho", datos));
        RutaDespachoDetalleDto detalle = await Obtener(id);
        Assert.Equal(datos.Codigo, detalle.Codigo); Assert.Equal(datos.Nombre, detalle.Nombre); Assert.Equal(datos.Zona, detalle.Zona); Assert.Equal(datos.Origen, detalle.Origen); Assert.Equal(datos.Destino, detalle.Destino); Assert.Equal(datos.Observaciones, detalle.Observaciones); Assert.Equal("99999999.99", detalle.DistanciaKm); Assert.Equal("9999999999999999.99", detalle.PeajeEstimado);
        PaginaRutasDespachoDto? pagina = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho?busqueda=" + datos.Codigo);
        Assert.Equal("9999999999999999.99", Assert.Single(pagina!.Items).PeajeEstimado);
    }

    [Fact]
    public async Task CodigoUnicoSinDistinguirMayusculasYPermiteElPropioAlEditar()
    {
        GuardarRutaDespachoRequestDto datos = Solicitud(); datos.Codigo = " r01 ";
        await Error(await _cliente.PostAsJsonAsync("/api/v1/rutas-despacho", datos), 409, "Ya existe una ruta de despacho con el código r01.");
        await Error(await _cliente.PutAsJsonAsync("/api/v1/rutas-despacho/2", datos), 409, "Ya existe una ruta de despacho con el código r01.");
        Assert.Equal(1, await Id(await _cliente.PutAsJsonAsync("/api/v1/rutas-despacho/1", datos)));
        Assert.Equal("r01", (await Obtener(1)).Codigo);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task InexistenteDevuelve404AntesDeValidarCampos(string metodo)
    {
        HttpResponseMessage respuesta = metodo == "GET" ? await _cliente.GetAsync("/api/v1/rutas-despacho/9999") : metodo == "PUT" ? await _cliente.PutAsJsonAsync("/api/v1/rutas-despacho/9999", new GuardarRutaDespachoRequestDto()) : await _cliente.DeleteAsync("/api/v1/rutas-despacho/9999");
        await Error(respuesta, 404, "La ruta de despacho 9999 no existe.");
    }

    [Theory]
    [InlineData("LIMA", 2)]
    [InlineData("guayaquil", 1)]
    [InlineData("r03", 1)]
    [InlineData("INTERNACIONAL", 1)]
    [InlineData("inexistente", 0)]
    public async Task BusquedaPorCamposSinDistinguirMayusculas(string texto, int total)
    {
        PaginaRutasDespachoDto? pagina = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho?busqueda=" + texto);
        Assert.NotNull(pagina); Assert.Equal(total, pagina.Total);
    }

    [Fact]
    public async Task FiltrosCombinadosYFalseSeAplicanAntesDePaginar()
    {
        PaginaRutasDespachoDto? norte = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho?zona=%20NORTE%20&activo=true&busqueda=tumbes");
        PaginaRutasDespachoDto? inactivas = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho?activo=false");
        Assert.Equal(3, Assert.Single(norte!.Items).Id); Assert.Equal(2, Assert.Single(inactivas!.Items).Id);
    }

    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("pagina=-1&tamanoPagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task PaginacionInvalida(string query, string mensaje)
    { await Error(await _cliente.GetAsync("/api/v1/rutas-despacho?" + query), 400, mensaje); }

    [Fact]
    public async Task PaginacionEstablePredeterminadaYFueraDeRango()
    {
        PaginaRutasDespachoDto? defecto = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho");
        PaginaRutasDespachoDto? primera = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho?tamanoPagina=2");
        PaginaRutasDespachoDto? segunda = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho?tamanoPagina=2&pagina=2");
        PaginaRutasDespachoDto? fuera = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho?pagina=2147483647");
        Assert.Equal(20, defecto!.TamanoPagina); Assert.Equal(1, defecto.Pagina); Assert.Equal(3, defecto.Total); Assert.Equal(new[] { 1, 2 }, primera!.Items.Select(r => r.Id).ToArray()); Assert.Equal(3, Assert.Single(segunda!.Items).Id); Assert.Equal(3, fuera!.Total); Assert.Empty(fuera.Items);
    }

    [Fact]
    public async Task ZonasYResumenIncluyenInactivasSinCombinarMonedas()
    {
        List<string>? zonas = await _cliente.GetFromJsonAsync<List<string>>("/api/v1/rutas-despacho/zonas");
        ResumenRutasDespachoDto? resumen = await _cliente.GetFromJsonAsync<ResumenRutasDespachoDto>("/api/v1/rutas-despacho/resumen");
        Assert.Equal(new[] { "Norte", "Sur" }, zonas); Assert.NotNull(resumen); Assert.Equal(3, resumen.TotalRutas); Assert.Equal(2, resumen.Activas); Assert.Equal(1, resumen.Inactivas); Assert.Equal("Norte", resumen.PorZona[0].Zona); Assert.Equal(2, resumen.PorZona[0].TotalRutas); Assert.Equal(0, resumen.PorZona[1].Activas);
    }

    [Fact]
    public async Task EliminarQuitaRutaYMaestroVacioRespondeSinErrores()
    {
        for (int id = 1; id <= 3; id++) { Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"/api/v1/rutas-despacho/{id}")).StatusCode); }
        PaginaRutasDespachoDto? pagina = await _cliente.GetFromJsonAsync<PaginaRutasDespachoDto>("/api/v1/rutas-despacho");
        ResumenRutasDespachoDto? resumen = await _cliente.GetFromJsonAsync<ResumenRutasDespachoDto>("/api/v1/rutas-despacho/resumen");
        Assert.Empty(pagina!.Items); Assert.Equal(0, resumen!.TotalRutas); Assert.Empty(resumen.PorZona);
        await Error(await _cliente.GetAsync("/api/v1/rutas-despacho/1"), 404, "La ruta de despacho 1 no existe.");
    }

    [Fact]
    public async Task ValidacionSigueOrdenAntesDeComprobarDuplicado()
    {
        GuardarRutaDespachoRequestDto datos = Solicitud(); datos.Codigo = "R01"; datos.Nombre = ""; datos.DistanciaKm = 0;
        await ValidacionEnAmbasOperaciones(datos, "El nombre de la ruta es obligatorio.");
    }

    [Fact]
    public async Task OpenApiContieneLasSieteOperaciones()
    {
        using JsonDocument documento = JsonDocument.Parse(await _cliente.GetStringAsync("/openapi/v1.json"));
        JsonElement paths = documento.RootElement.GetProperty("paths");
        foreach ((string ruta, string verbo) in new[] { ("", "get"), ("", "post"), ("/{id}", "get"), ("/{id}", "put"), ("/{id}", "delete"), ("/zonas", "get"), ("/resumen", "get") })
        { Assert.True(paths.GetProperty("/api/v1/rutas-despacho" + ruta).TryGetProperty(verbo, out _)); }
    }

    private async Task ValidacionEnAmbasOperaciones(GuardarRutaDespachoRequestDto datos, string mensaje)
    { await Error(await _cliente.PostAsJsonAsync("/api/v1/rutas-despacho", datos), 400, mensaje); await Error(await _cliente.PutAsJsonAsync("/api/v1/rutas-despacho/1", datos), 400, mensaje); }

    private async Task<RutaDespachoDetalleDto> Obtener(int id)
    { RutaDespachoDetalleDto? detalle = await _cliente.GetFromJsonAsync<RutaDespachoDetalleDto>($"/api/v1/rutas-despacho/{id}"); Assert.NotNull(detalle); return detalle; }

    internal static GuardarRutaDespachoRequestDto Solicitud()
    { return new GuardarRutaDespachoRequestDto { Codigo = "NUEVA", Nombre = "Ruta nueva", Zona = "Centro", Origen = "Lima", Destino = "Callao", DistanciaKm = 10.25m, PeajeEstimado = 1.25m, Moneda = "PEN" }; }

    private static async Task<int> Id(HttpResponseMessage respuesta)
    { Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); Dictionary<string, int>? dato = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>(); Assert.NotNull(dato); return dato["id"]; }

    private static async Task Error(HttpResponseMessage respuesta, int estado, string mensaje)
    { using JsonDocument json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()); Assert.Equal(estado, (int)respuesta.StatusCode); Assert.Equal(estado, json.RootElement.GetProperty("StatusCode").GetInt32()); Assert.Equal(mensaje, json.RootElement.GetProperty("Message").GetString()); Assert.False(json.RootElement.TryGetProperty("Detail", out _)); }
}
