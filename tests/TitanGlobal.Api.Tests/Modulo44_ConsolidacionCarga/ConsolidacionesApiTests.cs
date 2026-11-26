using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo44_ConsolidacionCarga;

[Collection("Modulo04")]
public class ConsolidacionesApiTests
{
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;
    public ConsolidacionesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica; _cliente = fabrica.CreateClient(); fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.LineasConsolidacionCarga.RemoveRange(db.LineasConsolidacionCarga); db.SaveChanges();
        db.ConsolidacionesCarga.RemoveRange(db.ConsolidacionesCarga); db.SaveChanges();
        for (int id = 1; id <= 3; id++)
        {
            ConsolidacionCarga carga = new ConsolidacionCarga { Id = id, Codigo = $"C0{id}", Nombre = $"Carga {id}", Estado = id == 1 ? "Borrador" : id == 2 ? "Confirmada" : "Cancelada", FechaConfirmacion = id == 1 ? null : new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), FechaCancelacion = id == 3 ? new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) : null, MotivoCancelacion = id == 3 ? "Histórica" : null };
            carga.Lineas.Add(new LineaConsolidacionCarga { Posicion = 1, Sku = "SKU-1", Descripcion = "Producto manual", Cantidad = 10, CantidadRecogida = id == 1 ? 0 : 10, UnidadesPorBulto = id == 1 ? 0 : 4, LargoCm = 50, AnchoCm = 40, AltoCm = 30, PesoBultoKg = 2 }); db.ConsolidacionesCarga.Add(carga);
        }
        db.SaveChanges();
    }

    [Theory]
    [InlineData("Codigo", "", "El código de la consolidación es obligatorio.")]
    [InlineData("Nombre", " ", "El nombre de la consolidación es obligatorio.")]
    public async Task CabeceraObligatoria(string campo, string valor, string mensaje)
    { GuardarConsolidacionRequestDto datos = Solicitud(); typeof(GuardarConsolidacionRequestDto).GetProperty(campo)!.SetValue(datos, valor); await Ambas(datos, mensaje); }
    [Theory]
    [InlineData("Codigo", 20, "El código de la consolidación no puede superar 20 caracteres.")]
    [InlineData("Nombre", 100, "El nombre de la consolidación no puede superar 100 caracteres.")]
    [InlineData("Observaciones", 500, "Las observaciones no pueden superar 500 caracteres.")]
    public async Task LimitesCabecera(string campo, int limite, string mensaje)
    { GuardarConsolidacionRequestDto datos = Solicitud(); typeof(GuardarConsolidacionRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', limite + 1)); await Ambas(datos, mensaje); }
    [Theory]
    [InlineData("Sku", 0, "Línea 1: el SKU es obligatorio.")]
    [InlineData("Sku", 41, "Línea 1: el SKU no puede superar 40 caracteres.")]
    [InlineData("Descripcion", 0, "Línea 1: la descripción es obligatoria.")]
    [InlineData("Descripcion", 201, "Línea 1: la descripción no puede superar 200 caracteres.")]
    public async Task TextosDeLinea(string campo, int longitud, string mensaje)
    { GuardarConsolidacionRequestDto datos = Solicitud(); typeof(LineaCargaRequestDto).GetProperty(campo)!.SetValue(datos.Lineas![0], new string('x', longitud)); await Ambas(datos, mensaje); }
    [Theory]
    [InlineData("Cantidad", "0", "Línea 1: la cantidad debe estar entre 1 y 1000000.")]
    [InlineData("Cantidad", "1000001.0", "Línea 1: la cantidad debe estar entre 1 y 1000000.")]
    [InlineData("CantidadRecogida", "-1", "Línea 1: la cantidad recogida debe estar entre 0 y 1000000.")]
    [InlineData("CantidadRecogida", "1000001.0", "Línea 1: la cantidad recogida debe estar entre 0 y 1000000.")]
    [InlineData("UnidadesPorBulto", "-1", "Línea 1: las unidades por bulto debe estar entre 0 y 1000000.")]
    [InlineData("UnidadesPorBulto", "1000001.0", "Línea 1: las unidades por bulto debe estar entre 0 y 1000000.")]
    [InlineData("LargoCm", "-1", "Línea 1: el largo debe estar entre 0 y 9999.99.")]
    [InlineData("LargoCm", "10000.99", "Línea 1: el largo debe estar entre 0 y 9999.99.")]
    [InlineData("LargoCm", "1.001", "Línea 1: el largo no puede tener más de 2 decimales.")]
    [InlineData("AnchoCm", "-1", "Línea 1: el ancho debe estar entre 0 y 9999.99.")]
    [InlineData("AnchoCm", "10000.99", "Línea 1: el ancho debe estar entre 0 y 9999.99.")]
    [InlineData("AnchoCm", "1.001", "Línea 1: el ancho no puede tener más de 2 decimales.")]
    [InlineData("AltoCm", "-1", "Línea 1: el alto debe estar entre 0 y 9999.99.")]
    [InlineData("AltoCm", "10000.99", "Línea 1: el alto debe estar entre 0 y 9999.99.")]
    [InlineData("AltoCm", "1.001", "Línea 1: el alto no puede tener más de 2 decimales.")]
    [InlineData("PesoBultoKg", "-1", "Línea 1: el peso por bulto debe estar entre 0 y 999999.99.")]
    [InlineData("PesoBultoKg", "1000000.99", "Línea 1: el peso por bulto debe estar entre 0 y 999999.99.")]
    [InlineData("PesoBultoKg", "1.001", "Línea 1: el peso por bulto no puede tener más de 2 decimales.")]
    public async Task RangosYDecimales(string campo, string texto, string mensaje)
    {
        GuardarConsolidacionRequestDto datos = Solicitud(); System.Reflection.PropertyInfo propiedad = typeof(LineaCargaRequestDto).GetProperty(campo)!;
        decimal valor = decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);
        if (propiedad.PropertyType == typeof(int)) { propiedad.SetValue(datos.Lineas![0], (int)valor); }
        else { propiedad.SetValue(datos.Lineas![0], valor); }
        await Ambas(datos, mensaje);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task LimitaNumeroLineas(int numero)
    { GuardarConsolidacionRequestDto datos = Solicitud(); datos.Lineas = Enumerable.Range(0, numero).Select(i => Linea()).ToList(); await Ambas(datos, "La consolidación debe tener entre 1 y 100 líneas."); }
    [Fact]
    public async Task LineasNulasYRecogidaSuperiorSonInvalidas()
    {
        GuardarConsolidacionRequestDto datos = Solicitud(); datos.Lineas = null; await Ambas(datos, "La consolidación debe tener entre 1 y 100 líneas.");
        datos = Solicitud(); datos.Lineas![0].CantidadRecogida = 11; await Ambas(datos, "Línea 1: la cantidad recogida no puede superar la cantidad prevista.");
        datos.Lineas = new List<LineaCargaRequestDto> { null! }; await Ambas(datos, "La consolidación no puede contener líneas nulas.");
    }
    [Fact]
    public async Task NormalizaYConservaTotalesExactosConBultoParcial()
    {
        GuardarConsolidacionRequestDto datos = Solicitud(); datos.Codigo = "  NUEVA  "; datos.Nombre = "  Carga nueva  "; datos.Observaciones = " "; datos.Lineas![0].Sku = " SKU ";
        int id = await Id(await _cliente.PostAsJsonAsync(Raiz, datos)); ConsolidacionDetalleDto detalle = await Obtener(id);
        Assert.Equal("NUEVA", detalle.Codigo); Assert.Equal("Carga nueva", detalle.Nombre); Assert.Null(detalle.Observaciones); Assert.Equal("Borrador", detalle.Estado); Assert.Equal("SKU", detalle.Lineas[0].Sku); Assert.Equal(10, detalle.TotalUnidades); Assert.Equal(3, detalle.TotalBultos); Assert.Equal("0.180000", detalle.VolumenM3); Assert.Equal("6.00", detalle.PesoKg);
    }
    [Fact]
    public async Task RedondeaCadaLineaASeisDecimalesAntesDeSumar()
    {
        GuardarConsolidacionRequestDto datos = Solicitud(); LineaCargaRequestDto linea = datos.Lineas![0]; linea.Cantidad = 1; linea.CantidadRecogida = 1; linea.UnidadesPorBulto = 1; linea.LargoCm = 1; linea.AnchoCm = 1; linea.AltoCm = 0.50m; datos.Lineas.Add(linea);
        int id = await Id(await _cliente.PostAsJsonAsync(Raiz, datos)); ConsolidacionDetalleDto detalle = await Obtener(id); Assert.Equal("0.000002", detalle.VolumenM3); Assert.All(detalle.Lineas, l => Assert.Equal("0.000001", l.VolumenM3));
    }
    [Fact]
    public async Task LimitesExactosYCienLineasNoDesbordan()
    {
        GuardarConsolidacionRequestDto datos = Solicitud(); datos.Codigo = new string('c', 20); datos.Nombre = new string('n', 100); datos.Observaciones = new string('o', 500);
        LineaCargaRequestDto linea = Linea(); linea.Sku = new string('s', 40); linea.Descripcion = new string('d', 200); linea.Cantidad = 1000000; linea.CantidadRecogida = 1000000; linea.UnidadesPorBulto = 1; linea.LargoCm = 9999.99m; linea.AnchoCm = 9999.99m; linea.AltoCm = 9999.99m; linea.PesoBultoKg = 999999.99m; datos.Lineas = Enumerable.Range(0, 100).Select(i => linea).ToList();
        int id = await Id(await _cliente.PostAsJsonAsync(Raiz, datos)); ConsolidacionDetalleDto detalle = await Obtener(id); Assert.Equal(100, detalle.Lineas.Count); Assert.Equal(100000000, detalle.TotalBultos); Assert.Equal("99999999000000.00", detalle.PesoKg); Assert.Equal("99999700000299.999900", detalle.VolumenM3);
    }
    [Fact]
    public async Task ReemplazaLineasAtomicamenteYPermitePresentacionesDelMismoSku()
    {
        GuardarConsolidacionRequestDto datos = Solicitud(); datos.Codigo = "C01"; datos.Lineas!.Add(Linea());
        await Id(await _cliente.PutAsJsonAsync(Raiz + "/1", datos)); ConsolidacionDetalleDto antes = await Obtener(1); Assert.Equal(2, antes.Lineas.Count);
        datos.Lineas[1].Cantidad = 0; await Error(await _cliente.PutAsJsonAsync(Raiz + "/1", datos), 400, "Línea 2: la cantidad debe estar entre 1 y 1000000."); Assert.Equal(antes.Lineas.Select(l => l.Id), (await Obtener(1)).Lineas.Select(l => l.Id));
        datos.Lineas = new List<LineaCargaRequestDto> { Linea() }; await Id(await _cliente.PutAsJsonAsync(Raiz + "/1", datos)); Assert.Single((await Obtener(1)).Lineas);
    }
    [Fact]
    public async Task UnicidadNormalizadaEnAltaYEdicionConservaCodigoPropio()
    {
        GuardarConsolidacionRequestDto datos = Solicitud(); datos.Codigo = " c02 "; await Error(await _cliente.PostAsJsonAsync(Raiz, datos), 409, "Ya existe una consolidación con el código c02."); await Error(await _cliente.PutAsJsonAsync(Raiz + "/1", datos), 409, "Ya existe una consolidación con el código c02."); datos.Codigo = " c01 "; Assert.Equal(1, await Id(await _cliente.PutAsJsonAsync(Raiz + "/1", datos)));
    }
    [Fact]
    public async Task ConfirmaSoloPickingYPackingCompletosYCancelaConHistorialUtc()
    {
        await Error(await _cliente.PostAsync(Raiz + "/1/confirmacion", null), 409, "Completa el picking de todas las líneas antes de confirmar.");
        GuardarConsolidacionRequestDto datos = Solicitud(); datos.Codigo = "C01"; datos.Lineas![0].UnidadesPorBulto = 0; await Id(await _cliente.PutAsJsonAsync(Raiz + "/1", datos)); await Error(await _cliente.PostAsync(Raiz + "/1/confirmacion", null), 409, "Completa el packing de todas las líneas antes de confirmar.");
        datos.Lineas[0].UnidadesPorBulto = 4; await Id(await _cliente.PutAsJsonAsync(Raiz + "/1", datos)); await Id(await _cliente.PostAsync(Raiz + "/1/confirmacion", null)); ConsolidacionDetalleDto confirmada = await Obtener(1); Assert.Equal("Confirmada", confirmada.Estado); Assert.Equal(DateTimeKind.Utc, confirmada.FechaConfirmacion!.Value.Kind);
        await Error(await _cliente.PutAsJsonAsync(Raiz + "/1", new GuardarConsolidacionRequestDto()), 409, "Solo se puede editar una consolidación en borrador."); await Error(await _cliente.DeleteAsync(Raiz + "/1"), 409, "Solo se puede eliminar una consolidación en borrador."); await Error(await _cliente.PostAsync(Raiz + "/1/confirmacion", null), 409, "Solo se puede confirmar una consolidación en borrador.");
        await Error(await _cliente.PostAsJsonAsync(Raiz + "/1/cancelacion", new { motivo = " " }), 400, "El motivo de cancelación es obligatorio."); await Error(await _cliente.PostAsJsonAsync(Raiz + "/1/cancelacion", new { motivo = new string('x', 501) }), 400, "El motivo de cancelación no puede superar 500 caracteres.");
        await Id(await _cliente.PostAsJsonAsync(Raiz + "/1/cancelacion", new { motivo = "  Cambio de plan  " })); ConsolidacionDetalleDto cancelada = await Obtener(1); Assert.Equal("Cancelada", cancelada.Estado); Assert.Equal("Cambio de plan", cancelada.MotivoCancelacion); Assert.Equal(confirmada.FechaConfirmacion, cancelada.FechaConfirmacion); Assert.Equal(confirmada.Lineas[0].Id, cancelada.Lineas[0].Id); Assert.Equal(DateTimeKind.Utc, cancelada.FechaCancelacion!.Value.Kind); await Error(await _cliente.PostAsJsonAsync(Raiz + "/1/cancelacion", new { motivo = "" }), 409, "La consolidación ya está cancelada.");
    }
    [Fact]
    public async Task CancelaBorradorSinInventarConfirmacionYAdmiteMotivoDe500()
    { await Id(await _cliente.PostAsJsonAsync(Raiz + "/1/cancelacion", new { motivo = new string('x', 500) })); ConsolidacionDetalleDto dto = await Obtener(1); Assert.Null(dto.FechaConfirmacion); Assert.Equal(500, dto.MotivoCancelacion!.Length); }
    [Theory]
    [InlineData("PUT", "Solo se puede editar una consolidación en borrador.")]
    [InlineData("DELETE", "Solo se puede eliminar una consolidación en borrador.")]
    [InlineData("confirmacion", "Solo se puede confirmar una consolidación en borrador.")]
    public async Task CanceladaNoPermiteOtrasEscrituras(string accion, string mensaje)
    { HttpResponseMessage respuesta = accion == "PUT" ? await _cliente.PutAsJsonAsync(Raiz + "/3", Solicitud()) : accion == "DELETE" ? await _cliente.DeleteAsync(Raiz + "/3") : await _cliente.PostAsync(Raiz + "/3/confirmacion", null); await Error(respuesta, 409, mensaje); }
    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("confirmacion")]
    [InlineData("cancelacion")]
    public async Task InexistenteSeValidaAntesDeEstadoYCuerpo(string accion)
    { HttpResponseMessage respuesta = accion == "GET" ? await _cliente.GetAsync(Raiz + "/9999") : accion == "PUT" ? await _cliente.PutAsJsonAsync(Raiz + "/9999", Solicitud()) : accion == "DELETE" ? await _cliente.DeleteAsync(Raiz + "/9999") : accion == "confirmacion" ? await _cliente.PostAsync(Raiz + "/9999/confirmacion", null) : await _cliente.PostAsJsonAsync(Raiz + "/9999/cancelacion", new { motivo = "" }); await Error(respuesta, 404, "La consolidación 9999 no existe."); }
    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Borrador, Confirmada o Cancelada.")]
    public async Task FiltrosInvalidos(string query, string mensaje) { await Error(await _cliente.GetAsync(Raiz + "?" + query), 400, mensaje); }
    [Fact]
    public async Task ListaFiltraPaginaYNoExponeDetalleEnCadaItem()
    {
        PaginaConsolidacionesDto? primera = await _cliente.GetFromJsonAsync<PaginaConsolidacionesDto>(Raiz + "?tamanoPagina=2"); PaginaConsolidacionesDto? segunda = await _cliente.GetFromJsonAsync<PaginaConsolidacionesDto>(Raiz + "?tamanoPagina=2&pagina=2"); PaginaConsolidacionesDto? filtro = await _cliente.GetFromJsonAsync<PaginaConsolidacionesDto>(Raiz + "?estado=confirmada&busqueda=CARGA"); PaginaConsolidacionesDto? vacia = await _cliente.GetFromJsonAsync<PaginaConsolidacionesDto>(Raiz + "?pagina=2147483647"); Assert.Equal(new[] { 1, 2 }, primera!.Items.Select(c => c.Id).ToArray()); Assert.Equal(3, Assert.Single(segunda!.Items).Id); Assert.Equal("0.180000", Assert.Single(filtro!.Items).VolumenM3); Assert.Empty(vacia!.Items); Assert.Equal(3, vacia.Total);
        using JsonDocument json = JsonDocument.Parse(await _cliente.GetStringAsync(Raiz)); Assert.False(json.RootElement.GetProperty("items")[0].TryGetProperty("lineas", out _));
        ResumenConsolidacionesDto? resumen = await _cliente.GetFromJsonAsync<ResumenConsolidacionesDto>(Raiz + "/resumen"); Assert.Equal(3, resumen!.Total); Assert.Equal(1, resumen.Borradores); Assert.Equal(1, resumen.Confirmadas); Assert.Equal(1, resumen.Canceladas);
    }
    [Fact]
    public async Task BajaBorradorEliminaSusLineasEnCascada()
    { Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync(Raiz + "/1")).StatusCode); using IServiceScope scope = _fabrica.Services.CreateScope(); TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>(); Assert.DoesNotContain(db.LineasConsolidacionCarga, l => l.ConsolidacionCargaId == 1); }
    [Fact]
    public async Task OchoOperacionesYTokenObligatorio()
    {
        using JsonDocument json = JsonDocument.Parse(await _cliente.GetStringAsync("/openapi/v1.json")); JsonElement paths = json.RootElement.GetProperty("paths"); foreach ((string ruta, string verbo) in new[] { ("", "get"), ("", "post"), ("/{id}", "get"), ("/{id}", "put"), ("/{id}", "delete"), ("/{id}/confirmacion", "post"), ("/{id}/cancelacion", "post"), ("/resumen", "get") }) { Assert.True(paths.GetProperty(Raiz + ruta).TryGetProperty(verbo, out _)); }
        using HttpClient anonimo = _fabrica.CreateClient(); anonimo.DefaultRequestHeaders.Authorization = null; Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Raiz)).StatusCode);
    }
    private const string Raiz = "/api/v1/consolidaciones-carga";
    internal static LineaCargaRequestDto Linea() { return new LineaCargaRequestDto { Sku = "SKU-1", Descripcion = "Manual", Cantidad = 10, CantidadRecogida = 10, UnidadesPorBulto = 4, LargoCm = 50, AnchoCm = 40, AltoCm = 30, PesoBultoKg = 2 }; }
    internal static GuardarConsolidacionRequestDto Solicitud() { return new GuardarConsolidacionRequestDto { Codigo = "NUEVA", Nombre = "Carga nueva", Lineas = new List<LineaCargaRequestDto> { Linea() } }; }
    private async Task Ambas(GuardarConsolidacionRequestDto datos, string mensaje) { await Error(await _cliente.PostAsJsonAsync(Raiz, datos), 400, mensaje); await Error(await _cliente.PutAsJsonAsync(Raiz + "/1", datos), 400, mensaje); }
    private async Task<ConsolidacionDetalleDto> Obtener(int id) { ConsolidacionDetalleDto? dto = await _cliente.GetFromJsonAsync<ConsolidacionDetalleDto>(Raiz + "/" + id); Assert.NotNull(dto); return dto; }
    private static async Task<int> Id(HttpResponseMessage respuesta) { Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); Dictionary<string, int>? dto = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>(); return dto!["id"]; }
    private static async Task Error(HttpResponseMessage respuesta, int estado, string mensaje) { Assert.Equal(estado, (int)respuesta.StatusCode); using JsonDocument json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()); Assert.Equal(estado, json.RootElement.GetProperty("StatusCode").GetInt32()); Assert.Equal(mensaje, json.RootElement.GetProperty("Message").GetString()); Assert.False(json.RootElement.TryGetProperty("Detail", out _)); }
}
