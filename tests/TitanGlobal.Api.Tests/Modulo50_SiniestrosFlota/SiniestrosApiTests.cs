using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo50_SiniestrosFlota;

[Collection("Modulo04")]
public class SiniestrosApiTests
{
    private const string Ruta = "/api/v1/siniestros-flota";
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;

    public SiniestrosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<SiniestroFlota>());
        db.SaveChanges();
    }

    internal static GuardarSiniestroRequestDto Valida(string codigo = "SIN-50")
    {
        return new GuardarSiniestroRequestDto
        {
            Codigo = codigo,
            Placa = " abc-500 ",
            Lugar = " Patio Norte ",
            Descripcion = " Incidente registrado manualmente ",
            Tipo = " accidente ",
            FechaIncidente = new DateTime(2026, 11, 23, 10, 30, 12, 345),
            Observaciones = " "
        };
    }

    internal static RegistrarReclamoRequestDto Reclamo()
    {
        return new RegistrarReclamoRequestDto { NumeroPoliza = " POL-50 ", Aseguradora = " Aseguradora Norte ", NumeroReclamo = " REC-50 ", Moneda = " pen ", MontoReclamado = 1250.50m };
    }

    private async Task ReclamarAsync(int id, RegistrarReclamoRequestDto? datos = null)
    {
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/reclamo", datos ?? Reclamo())).StatusCode);
    }

    private async Task<int> CrearAsync(GuardarSiniestroRequestDto? datos = null)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, datos ?? Valida());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<SiniestroDetalleDto> DetalleAsync(int id)
    {
        SiniestroDetalleDto? detalle = await _cliente.GetFromJsonAsync<SiniestroDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(detalle);
        return detalle;
    }

    private static async Task ErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, cuerpo.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, cuerpo.GetProperty("Message").GetString());
    }

    [Theory]
    [InlineData("Codigo", "código", 20)]
    [InlineData("Placa", "placa", 20)]
    [InlineData("Lugar", "lugar", 200)]
    [InlineData("Descripcion", "descripción", 500)]
    public async Task ValidaCadaTextoYLimiteSinPersistir(string campo, string etiqueta, int maximo)
    {
        foreach (bool largo in new[] { false, true })
        {
            GuardarSiniestroRequestDto datos = Valida();
            typeof(GuardarSiniestroRequestDto).GetProperty(campo)!.SetValue(datos, largo ? new string('x', maximo + 1) : " ");
            await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, largo ? $"El campo {etiqueta} no puede superar {maximo} caracteres." : $"El campo {etiqueta} es obligatorio.");
            Assert.Equal(0, (await _cliente.GetFromJsonAsync<ResumenSiniestrosDto>(Ruta + "/resumen"))!.Total);
            typeof(GuardarSiniestroRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', maximo));
            int id = await CrearAsync(datos);
            Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExigeFecha(bool defecto)
    {
        GuardarSiniestroRequestDto datos = Valida();
        datos.FechaIncidente = defecto ? default(DateTime) : null;
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La fecha del incidente es obligatoria.");
    }
    [Fact]
    public async Task ValidaTipoYObservacionesSinCambiarElRegistro()
    {
        int id = await CrearAsync();
        GuardarSiniestroRequestDto datos = Valida();
        datos.Tipo = "Otro";
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos), HttpStatusCode.BadRequest, "El tipo debe ser Accidente, Robo o DanoMaterial.");
        datos.Tipo = " robo ";
        datos.Observaciones = new string('x', 501);
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos), HttpStatusCode.BadRequest, "Las observaciones no pueden superar 500 caracteres.");
        Assert.Equal("Accidente", (await DetalleAsync(id)).Tipo);
        datos.Observaciones = new string('x', 500);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        SiniestroDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal(500, detalle.Observaciones!.Length);
        Assert.Equal("Robo", detalle.Tipo);
    }
    [Fact]
    public async Task NormalizaUtcPlacaYUnicidadEnAltaYEdicion()
    {
        int id = await CrearAsync();
        SiniestroDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal("ABC-500", detalle.Placa);
        Assert.Equal("Patio Norte", detalle.Lugar);
        Assert.Null(detalle.Observaciones);
        Assert.Null(detalle.MontoReclamado);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaIncidente.Kind);
        Assert.Equal(345, detalle.FechaIncidente.Millisecond);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, Valida(" sin-50 ")), HttpStatusCode.Conflict, "Ya existe un siniestro con el código sin-50.");
        int otro = await CrearAsync(Valida("OTRO"));
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{otro}", Valida("sin-50")), HttpStatusCode.Conflict, "Ya existe un siniestro con el código sin-50.");
        Assert.Equal("OTRO", (await DetalleAsync(otro)).Codigo);
    }
    [Theory]
    [InlineData("NumeroPoliza", "número de póliza", 40)]
    [InlineData("Aseguradora", "aseguradora", 150)]
    [InlineData("NumeroReclamo", "número de reclamo", 40)]
    public async Task ValidaReferenciasDeReclamo(string campo, string etiqueta, int maximo)
    {
        int id = await CrearAsync();
        foreach (bool largo in new[] { false, true })
        {
            RegistrarReclamoRequestDto datos = Reclamo();
            typeof(RegistrarReclamoRequestDto).GetProperty(campo)!.SetValue(datos, largo ? new string('x', maximo + 1) : " ");
            await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/reclamo", datos), HttpStatusCode.BadRequest, largo ? $"El campo {etiqueta} no puede superar {maximo} caracteres." : $"El campo {etiqueta} es obligatorio.");
            Assert.Null((await DetalleAsync(id)).FechaReclamo);
        }
        RegistrarReclamoRequestDto limite = Reclamo();
        typeof(RegistrarReclamoRequestDto).GetProperty(campo)!.SetValue(limite, new string('x', maximo));
        await ReclamarAsync(id, limite);
    }
    [Theory]
    [InlineData("")]
    [InlineData("P3N")]
    [InlineData("PENN")]
    [InlineData("ÅBC")]
    public async Task ValidaMoneda(string valor)
    {
        int id = await CrearAsync();
        RegistrarReclamoRequestDto datos = Reclamo();
        datos.Moneda = valor;
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/reclamo", datos), HttpStatusCode.BadRequest, "La moneda debe contener 3 letras mayúsculas.");
        Assert.Null((await DetalleAsync(id)).NumeroPoliza);
    }
    [Theory]
    [InlineData("0", "El monto reclamado debe estar entre 0.01 y 9999999999.99.")]
    [InlineData("10000000000", "El monto reclamado debe estar entre 0.01 y 9999999999.99.")]
    [InlineData("1.001", "El monto reclamado no puede tener más de 2 decimales.")]
    public async Task ValidaMontoReclamado(string valor, string mensaje)
    {
        int id = await CrearAsync();
        RegistrarReclamoRequestDto datos = Reclamo();
        datos.MontoReclamado = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/reclamo", datos), HttpStatusCode.BadRequest, mensaje);
        Assert.Equal("Registrado", (await DetalleAsync(id)).Estado);
    }
    [Theory]
    [InlineData("0.01")]
    [InlineData("9999999999.99")]
    public async Task PreservaExtremosMonetariosSinRedondear(string valor)
    {
        int id = await CrearAsync();
        RegistrarReclamoRequestDto datos = Reclamo();
        datos.MontoReclamado = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        await ReclamarAsync(id, datos);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cierre", new { resultado = "Aceptado", montoReconocido = datos.MontoReclamado, resolucion = "Referencia manual" })).StatusCode);
        SiniestroDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal(valor, detalle.MontoReclamado);
        Assert.Equal(valor, detalle.MontoReconocido);
    }
    [Theory]
    [InlineData("Otro", "1", "El resultado debe ser Aceptado o Rechazado.")]
    [InlineData("Aceptado", "-1", "El monto reconocido debe estar entre 0 y 9999999999.99.")]
    [InlineData("Aceptado", "10000000000", "El monto reconocido debe estar entre 0 y 9999999999.99.")]
    [InlineData("Aceptado", "1.001", "El monto reconocido no puede tener más de 2 decimales.")]
    [InlineData("Aceptado", "1251", "El monto reconocido no puede superar el reclamado.")]
    [InlineData("Aceptado", "0", "Un reclamo aceptado debe reconocer un importe mayor que cero.")]
    [InlineData("Rechazado", "1", "Un reclamo rechazado debe reconocer un importe de cero.")]
    public async Task ValidaResultadoYMontoSinCerrar(string resultado, string valor, string mensaje)
    {
        int id = await CrearAsync();
        await ReclamarAsync(id);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cierre", new { resultado, montoReconocido = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture), resolucion = "Resolución" }), HttpStatusCode.BadRequest, mensaje);
        SiniestroDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal("EnReclamo", detalle.Estado);
        Assert.Null(detalle.FechaCierre);
        Assert.Null(detalle.Resultado);
        Assert.Equal("1250.50", detalle.MontoReclamado);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ValidaResolucion(bool largo)
    {
        int id = await CrearAsync();
        await ReclamarAsync(id);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cierre", new { resultado = "Rechazado", montoReconocido = 0m, resolucion = largo ? new string('x', 501) : " " }), HttpStatusCode.BadRequest, largo ? "El campo resolución no puede superar 500 caracteres." : "El campo resolución es obligatorio.");
        Assert.Null((await DetalleAsync(id)).FechaCierre);
    }
    [Theory]
    [InlineData(" aceptado ", "1000.25", "Aceptado")]
    [InlineData(" rechazado ", "0", "Rechazado")]
    public async Task ReclamaYCierraConservandoLosDatos(string resultado, string valor, string normalizado)
    {
        int id = await CrearAsync();
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cierre", new { }), HttpStatusCode.Conflict, "Solo se puede cerrar un siniestro en reclamo.");
        await ReclamarAsync(id);
        SiniestroDetalleDto reclamo = await DetalleAsync(id);
        Assert.Equal("PEN", reclamo.Moneda);
        Assert.Equal("POL-50", reclamo.NumeroPoliza);
        Assert.Equal(DateTimeKind.Utc, reclamo.FechaReclamo!.Value.Kind);
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida()), HttpStatusCode.Conflict, "Solo se puede editar un siniestro registrado.");
        await ErrorAsync(await _cliente.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.Conflict, "Solo se puede eliminar un siniestro registrado.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/reclamo", Reclamo()), HttpStatusCode.Conflict, "Solo se puede reclamar un siniestro registrado.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cierre", new { resultado, montoReconocido = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture), resolucion = new string('x', 500) })).StatusCode);
        SiniestroDetalleDto cerrado = await DetalleAsync(id);
        Assert.Equal(normalizado, cerrado.Resultado);
        Assert.Equal(reclamo.FechaReclamo, cerrado.FechaReclamo);
        Assert.Equal(reclamo.NumeroReclamo, cerrado.NumeroReclamo);
        Assert.Equal(reclamo.MontoReclamado, cerrado.MontoReclamado);
        Assert.Equal(DateTimeKind.Utc, cerrado.FechaCierre!.Value.Kind);
        Assert.Equal(500, cerrado.Resolucion!.Length);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cierre", new { }), HttpStatusCode.Conflict, "Solo se puede cerrar un siniestro en reclamo.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = "Cambio" }), HttpStatusCode.Conflict, "Solo se puede anular un siniestro registrado o en reclamo.");
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnulaConservandoReclamoCuandoExiste(bool reclamar)
    {
        int id = await CrearAsync();
        if (reclamar)
        {
            await ReclamarAsync(id);
        }
        SiniestroDetalleDto antes = await DetalleAsync(id);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = " " }), HttpStatusCode.BadRequest, "El motivo de anulación es obligatorio.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = new string('x', 501) }), HttpStatusCode.BadRequest, "El motivo de anulación no puede superar 500 caracteres.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = new string('x', 500) })).StatusCode);
        SiniestroDetalleDto anulado = await DetalleAsync(id);
        Assert.Equal(antes.NumeroReclamo, anulado.NumeroReclamo);
        Assert.Equal(antes.FechaReclamo, anulado.FechaReclamo);
        Assert.Equal(antes.MontoReclamado, anulado.MontoReclamado);
        Assert.Equal("Anulado", anulado.Estado);
        Assert.Equal(DateTimeKind.Utc, anulado.FechaAnulacion!.Value.Kind);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = "Otro" }), HttpStatusCode.Conflict, "El siniestro ya está anulado.");
    }
    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("POST", "/reclamo")]
    [InlineData("POST", "/cierre")]
    [InlineData("POST", "/anulacion")]
    public async Task BuscaAntesDeValidar(string metodo, string sufijo)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + "/2147483647" + sufijo);
        if (metodo == "PUT" || metodo == "POST")
        {
            peticion.Content = JsonContent.Create(new { });
        }
        await ErrorAsync(await _cliente.SendAsync(peticion), HttpStatusCode.NotFound, "El siniestro 2147483647 no existe.");
    }
    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Registrado, EnReclamo, Cerrado o Anulado.")]
    [InlineData("tipo=Otro", "El tipo debe ser Accidente, Robo o DanoMaterial.")]
    public async Task ValidaConsulta(string query, string mensaje)
    {
        await ErrorAsync(await _cliente.GetAsync(Ruta + "?" + query), HttpStatusCode.BadRequest, mensaje);
    }
    [Fact]
    public async Task FiltraYPaginaSinSumarMonedas()
    {
        int b = await CrearAsync(Valida("B"));
        int a = await CrearAsync(Valida("A"));
        GuardarSiniestroRequestDto tercero = Valida("C");
        tercero.Tipo = "Robo";
        int c = await CrearAsync(tercero);
        await ReclamarAsync(b);
        RegistrarReclamoRequestDto usd = Reclamo();
        usd.Moneda = "USD";
        await ReclamarAsync(c, usd);
        foreach (string buscar in new[] { "abc-500", "patio norte" })
        {
            PaginaSiniestrosDto pagina = (await _cliente.GetFromJsonAsync<PaginaSiniestrosDto>($"{Ruta}?busqueda={Uri.EscapeDataString(buscar)}&pagina=2&tamanoPagina=1"))!;
            Assert.Equal(3, pagina.Total);
            Assert.Equal(b, Assert.Single(pagina.Items).Id);
        }
        Assert.Equal(a, Assert.Single((await _cliente.GetFromJsonAsync<PaginaSiniestrosDto>($"{Ruta}?busqueda=a&estado=registrado&tipo=aCcIdEnTe"))!.Items).Id);
        Assert.Empty((await _cliente.GetFromJsonAsync<PaginaSiniestrosDto>($"{Ruta}?pagina=2147483647&tamanoPagina=100"))!.Items);
        ResumenSiniestrosDto resumen = (await _cliente.GetFromJsonAsync<ResumenSiniestrosDto>(Ruta + "/resumen"))!;
        Assert.Equal(3, resumen.Total);
        Assert.Equal(1, resumen.Registrados);
        Assert.Equal(2, resumen.EnReclamo);
        Assert.DoesNotContain("monto", await _cliente.GetStringAsync(Ruta + "/resumen"), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task EliminaRegistradoYExigeJwt()
    {
        int id = await CrearAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        await ErrorAsync(await _cliente.GetAsync($"{Ruta}/{id}"), HttpStatusCode.NotFound, $"El siniestro {id} no existe.");
        using HttpClient anonimo = _fabrica.CreateClient();
        anonimo.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Ruta)).StatusCode);
    }
}
