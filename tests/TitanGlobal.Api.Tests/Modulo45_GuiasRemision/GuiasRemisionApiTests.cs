using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo45_GuiasRemision;

[Collection("Modulo04")]
public class GuiasRemisionApiTests
{
    private const string Ruta = "/api/v1/guias-remision";
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;

    public GuiasRemisionApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<TitanGlobal.Domain.Models.LineaGuiaRemision>());
        db.RemoveRange(db.Set<TitanGlobal.Domain.Models.GuiaRemision>());
        db.SaveChanges();
    }

    internal static GuardarGuiaRequestDto Valida(int numero = 1)
    {
        return new GuardarGuiaRequestDto
        {
            Serie = " t001 ",
            Numero = numero,
            FechaInicioTraslado = new DateTime(2026, 11, 22, 10, 30, 0),
            RemitenteNombre = " RemitenteNombre ",
            RemitenteDocumento = " RemitenteDocumento ",
            DestinatarioNombre = " DestinatarioNombre ",
            DestinatarioDocumento = " DestinatarioDocument ",
            DireccionOrigen = " DireccionOrigen ",
            DireccionDestino = " DireccionDestino ",
            MotivoTraslado = " MotivoTraslado ",
            Transportista = " Transportista ",
            Conductor = " Conductor ",
            Placa = " Placa ",
            PesoBrutoKg = 12.34m,
            Observaciones = " ",
            Lineas = new List<LineaGuiaRequestDto> { new LineaGuiaRequestDto { Sku = " SKU ", Descripcion = " Caja ", UnidadMedida = " UND ", Cantidad = 1.125m } }
        };
    }

    private async Task<int> CrearAsync(GuardarGuiaRequestDto? datos = null)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, datos ?? Valida());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return cuerpo.GetProperty("id").GetInt32();
    }

    private async Task ErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, cuerpo.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, cuerpo.GetProperty("Message").GetString());
        await Task.CompletedTask;
    }

    public static IEnumerable<object[]> TextosInvalidos()
    {
        yield return new object[] { "RemitenteNombre", "   ", "El campo nombre del remitente es obligatorio." };
        yield return new object[] { "RemitenteNombre", new string('x', 201), "El campo nombre del remitente no puede superar 200 caracteres." };
        yield return new object[] { "RemitenteDocumento", "   ", "El campo documento del remitente es obligatorio." };
        yield return new object[] { "RemitenteDocumento", "xxxxxxxxxxxxxxxxxxxxx", "El campo documento del remitente no puede superar 20 caracteres." };
        yield return new object[] { "DestinatarioNombre", "   ", "El campo nombre del destinatario es obligatorio." };
        yield return new object[] { "DestinatarioNombre", new string('x', 201), "El campo nombre del destinatario no puede superar 200 caracteres." };
        yield return new object[] { "DestinatarioDocumento", "   ", "El campo documento del destinatario es obligatorio." };
        yield return new object[] { "DestinatarioDocumento", "xxxxxxxxxxxxxxxxxxxxx", "El campo documento del destinatario no puede superar 20 caracteres." };
        yield return new object[] { "DireccionOrigen", "   ", "El campo dirección de origen es obligatorio." };
        yield return new object[] { "DireccionOrigen", new string('x', 301), "El campo dirección de origen no puede superar 300 caracteres." };
        yield return new object[] { "DireccionDestino", "   ", "El campo dirección de destino es obligatorio." };
        yield return new object[] { "DireccionDestino", new string('x', 301), "El campo dirección de destino no puede superar 300 caracteres." };
        yield return new object[] { "MotivoTraslado", "   ", "El campo motivo del traslado es obligatorio." };
        yield return new object[] { "MotivoTraslado", new string('x', 201), "El campo motivo del traslado no puede superar 200 caracteres." };
        yield return new object[] { "Transportista", "   ", "El campo transportista es obligatorio." };
        yield return new object[] { "Transportista", new string('x', 201), "El campo transportista no puede superar 200 caracteres." };
        yield return new object[] { "Conductor", "   ", "El campo conductor es obligatorio." };
        yield return new object[] { "Conductor", new string('x', 101), "El campo conductor no puede superar 100 caracteres." };
        yield return new object[] { "Placa", "   ", "El campo placa es obligatorio." };
        yield return new object[] { "Placa", "xxxxxxxxxxxxxxxxxxxxx", "El campo placa no puede superar 20 caracteres." };
    }

    [Theory]
    [MemberData(nameof(TextosInvalidos))]
    public async Task ValidaCadaTextoSinPersistir(string campo, string valor, string mensaje)
    {
        GuardarGuiaRequestDto datos = Valida();
        typeof(GuardarGuiaRequestDto).GetProperty(campo)!.SetValue(datos, valor);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, mensaje);
        Assert.Equal(0, (await _cliente.GetFromJsonAsync<ResumenGuiasDto>(Ruta + "/resumen"))!.Total);
    }

    [Theory]
    [InlineData("Serie", "", "La serie debe contener entre 1 y 8 letras mayúsculas o dígitos.")]
    [InlineData("Serie", "A-1", "La serie debe contener entre 1 y 8 letras mayúsculas o dígitos.")]
    [InlineData("Serie", "ABCDEFGHI", "La serie debe contener entre 1 y 8 letras mayúsculas o dígitos.")]
    [InlineData("Numero", "0", "El número debe estar entre 1 y 99999999.")]
    [InlineData("Numero", "100000000", "El número debe estar entre 1 y 99999999.")]
    [InlineData("PesoBrutoKg", "0", "El peso bruto debe estar entre 0.01 y 9999999999.99 kg.")]
    [InlineData("PesoBrutoKg", "10000000000", "El peso bruto debe estar entre 0.01 y 9999999999.99 kg.")]
    [InlineData("PesoBrutoKg", "1.001", "El peso bruto no puede tener más de 2 decimales.")]
    public async Task ValidaSerieNumeroYPeso(string campo, string valor, string mensaje)
    {
        GuardarGuiaRequestDto datos = Valida();
        object convertido = campo == "Serie" ? valor : campo == "Numero" ? (object)int.Parse(valor) : decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        typeof(GuardarGuiaRequestDto).GetProperty(campo)!.SetValue(datos, convertido);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExigeFecha(bool defecto)
    {
        GuardarGuiaRequestDto datos = Valida();
        datos.FechaInicioTraslado = defecto ? default(DateTime) : null;
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La fecha de inicio del traslado es obligatoria.");
    }

    [Fact]
    public async Task LimitaObservaciones()
    {
        GuardarGuiaRequestDto datos = Valida();
        datos.Observaciones = new string('x', 501);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "Las observaciones no pueden superar 500 caracteres.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ExigeColeccionAcotada(int cantidad)
    {
        GuardarGuiaRequestDto datos = Valida();
        datos.Lineas = cantidad == -1 ? null : Enumerable.Range(0, cantidad).Select(i => Valida().Lineas![0]).ToList();
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La guía debe tener entre 1 y 100 líneas.");
    }

    [Fact]
    public async Task RechazaLineaNula()
    {
        GuardarGuiaRequestDto datos = Valida();
        datos.Lineas = new List<LineaGuiaRequestDto> { null! };
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La guía no puede contener líneas nulas.");
    }

    [Theory]
    [InlineData("Sku", 40, "SKU")]
    [InlineData("Descripcion", 200, "descripción")]
    [InlineData("UnidadMedida", 10, "unidad de medida")]
    public async Task ValidaTextosDeLinea(string campo, int maximo, string etiqueta)
    {
        GuardarGuiaRequestDto datos = Valida();
        typeof(LineaGuiaRequestDto).GetProperty(campo)!.SetValue(datos.Lineas![0], " ");
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, $"Línea 1: el campo {etiqueta} es obligatorio.");
        typeof(LineaGuiaRequestDto).GetProperty(campo)!.SetValue(datos.Lineas![0], new string('x', maximo + 1));
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, $"Línea 1: el campo {etiqueta} no puede superar {maximo} caracteres.");
    }

    [Theory]
    [InlineData("0", "Línea 1: la cantidad debe estar entre 0.001 y 999999999.999.")]
    [InlineData("1000000000", "Línea 1: la cantidad debe estar entre 0.001 y 999999999.999.")]
    [InlineData("1.0001", "Línea 1: la cantidad no puede tener más de 3 decimales.")]
    public async Task ValidaCantidad(string cantidad, string mensaje)
    {
        GuardarGuiaRequestDto datos = Valida();
        datos.Lineas![0].Cantidad = decimal.Parse(cantidad, System.Globalization.CultureInfo.InvariantCulture);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task NormalizaConservaPrecisionYReemplazaLineasAtomicamente()
    {
        int id = await CrearAsync();
        GuiaDetalleDto original = (await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{id}"))!;
        Assert.Equal("T001", original.Serie);
        Assert.Equal("1.125", original.Lineas[0].Cantidad);
        Assert.Equal("12.34", original.PesoBrutoKg);
        Assert.Null(original.Observaciones);
        Assert.Equal(DateTimeKind.Utc, original.FechaInicioTraslado.Kind);
        Assert.Equal(10, original.FechaInicioTraslado.Hour);
        GuardarGuiaRequestDto datos = Valida();
        datos.DestinatarioNombre = "No persistir";
        datos.Lineas![0].Cantidad = 0;
        Assert.Equal(HttpStatusCode.BadRequest, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        GuiaDetalleDto intacta = (await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{id}"))!;
        Assert.Equal(original.DestinatarioNombre, intacta.DestinatarioNombre);
        Assert.Equal(original.Lineas[0].Id, intacta.Lineas[0].Id);
        datos = Valida();
        datos.Lineas!.Add(new LineaGuiaRequestDto { Sku = "SKU", Descripcion = "Segunda", UnidadMedida = "KG", Cantidad = 0.001m });
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        GuiaDetalleDto editada = (await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{id}"))!;
        Assert.Equal(2, editada.TotalLineas);
        Assert.Equal(new[] { 1, 2 }, editada.Lineas.Select(l => l.Posicion));
        Assert.Equal("0.001", editada.Lineas[1].Cantidad);
    }

    [Fact]
    public async Task AceptaTodosLosLimitesSuperiores()
    {
        GuardarGuiaRequestDto datos = Valida(99999999);
        datos.Serie = "ABCDEFGH";
        datos.RemitenteNombre = new string('x', 200);
        datos.RemitenteDocumento = new string('x', 20);
        datos.DestinatarioNombre = new string('x', 200);
        datos.DestinatarioDocumento = new string('x', 20);
        datos.DireccionOrigen = new string('x', 300);
        datos.DireccionDestino = new string('x', 300);
        datos.MotivoTraslado = new string('x', 200);
        datos.Transportista = new string('x', 200);
        datos.Conductor = new string('x', 100);
        datos.Placa = new string('x', 20);
        datos.Observaciones = new string('x', 500);
        datos.PesoBrutoKg = 9999999999.99m;
        datos.Lineas = Enumerable.Range(1, 100).Select(i => new LineaGuiaRequestDto { Sku = new string('x', 40), Descripcion = new string('x', 200), UnidadMedida = new string('x', 10), Cantidad = 999999999.999m }).ToList();
        int id = await CrearAsync(datos);
        GuiaDetalleDto detalle = (await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{id}"))!;
        Assert.Equal(100, detalle.TotalLineas);
        Assert.Equal("9999999999.99", detalle.PesoBrutoKg);
        Assert.All(detalle.Lineas, l => Assert.Equal("999999999.999", l.Cantidad));
    }

    [Fact]
    public async Task EmiteAnulaYConservaHistorial()
    {
        int id = await CrearAsync();
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{id}/emision", null)).StatusCode);
        GuiaDetalleDto emitida = (await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{id}"))!;
        Assert.Equal("Emitida", emitida.Estado);
        Assert.Equal(DateTimeKind.Utc, emitida.FechaEmision!.Value.Kind);
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida()), HttpStatusCode.Conflict, "Solo se puede editar una guía en borrador.");
        await ErrorAsync(await _cliente.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.Conflict, "Solo se puede eliminar una guía en borrador.");
        await ErrorAsync(await _cliente.PostAsync($"{Ruta}/{id}/emision", null), HttpStatusCode.Conflict, "Solo se puede emitir una guía en borrador.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = " " }), HttpStatusCode.BadRequest, "El motivo de anulación es obligatorio.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = new string('x', 501) }), HttpStatusCode.BadRequest, "El motivo de anulación no puede superar 500 caracteres.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = " Cambio " })).StatusCode);
        GuiaDetalleDto anulada = (await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{id}"))!;
        Assert.Equal("Anulada", anulada.Estado);
        Assert.Equal(emitida.FechaEmision, anulada.FechaEmision);
        Assert.Equal("Cambio", anulada.MotivoAnulacion);
        Assert.Equal(DateTimeKind.Utc, anulada.FechaAnulacion!.Value.Kind);
        Assert.Equal(emitida.Lineas[0].Id, anulada.Lineas[0].Id);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = "Otra" }), HttpStatusCode.Conflict, "La guía ya está anulada.");
    }

    [Fact]
    public async Task AnulaBorradorYEliminaOtroConSusLineas()
    {
        int id = await CrearAsync();
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = new string('x', 500) })).StatusCode);
        Assert.Null((await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{id}"))!.FechaEmision);
        int otro = await CrearAsync(Valida(2));
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{otro}")).StatusCode);
        using IServiceScope scope = _fabrica.Services.CreateScope();
        Assert.DoesNotContain(scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>().Set<TitanGlobal.Domain.Models.LineaGuiaRemision>(), l => l.GuiaRemisionId == otro);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("POST", "/emision")]
    [InlineData("POST", "/anulacion")]
    public async Task BuscaAntesDeValidar(string metodo, string sufijo)
    {
        HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + "/2147483647" + sufijo);
        if (metodo == "PUT" || sufijo == "/anulacion")
        {
            peticion.Content = JsonContent.Create(new { });
        }
        await ErrorAsync(await _cliente.SendAsync(peticion), HttpStatusCode.NotFound, "La guía de remisión 2147483647 no existe.");
    }

    [Fact]
    public async Task NumeroUnicoPorSerieNormalizadaEnAltaYEdicion()
    {
        await CrearAsync();
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, Valida()), HttpStatusCode.Conflict, "Ya existe una guía con la serie T001 y el número 1.");
        int otro = await CrearAsync(Valida(2));
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{otro}", Valida()), HttpStatusCode.Conflict, "Ya existe una guía con la serie T001 y el número 1.");
        Assert.Equal(2, (await _cliente.GetFromJsonAsync<GuiaDetalleDto>($"{Ruta}/{otro}"))!.Numero);
        GuardarGuiaRequestDto distinta = Valida();
        distinta.Serie = "T002";
        await CrearAsync(distinta);
    }

    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Borrador, Emitida o Anulada.")]
    public async Task ValidaConsulta(string query, string mensaje)
    {
        await ErrorAsync(await _cliente.GetAsync(Ruta + "?" + query), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task FiltraPaginaYResumeSinExponerLineas()
    {
        int segundo = await CrearAsync(Valida(2));
        await CrearAsync();
        await _cliente.PostAsync($"{Ruta}/{segundo}/emision", null);
        foreach (string busqueda in new[] { "t001", "destinatarionombre", "destinatario" })
        {
            PaginaGuiasDto pagina = (await _cliente.GetFromJsonAsync<PaginaGuiasDto>($"{Ruta}?busqueda={busqueda}&tamanoPagina=1&pagina=2"))!;
            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, Assert.Single(pagina.Items).Numero);
        }
        Assert.Equal(1, (await _cliente.GetFromJsonAsync<PaginaGuiasDto>($"{Ruta}?estado=eMiTiDa"))!.Total);
        Assert.Empty((await _cliente.GetFromJsonAsync<PaginaGuiasDto>($"{Ruta}?pagina=2147483647&tamanoPagina=100"))!.Items);
        Assert.DoesNotContain("\"lineas\":", await _cliente.GetStringAsync(Ruta), StringComparison.OrdinalIgnoreCase);
        ResumenGuiasDto resumen = (await _cliente.GetFromJsonAsync<ResumenGuiasDto>(Ruta + "/resumen"))!;
        Assert.Equal(2, resumen.Total);
        Assert.Equal(1, resumen.Emitidas);
        Assert.Equal(1, resumen.Borradores);
        Assert.Equal(0, resumen.Anuladas);
    }

    [Fact]
    public async Task ExigeToken()
    {
        using HttpClient anonimo = _fabrica.CreateClient();
        anonimo.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Ruta)).StatusCode);
    }
}
