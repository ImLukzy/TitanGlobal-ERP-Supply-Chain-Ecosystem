using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo47_ControlCombustible;

[Collection("Modulo04")]
public class ValesCombustibleApiTests
{
    private const string Ruta = "/api/v1/control-combustible";
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;

    public ValesCombustibleApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<ValeCombustible>());
        db.SaveChanges();
    }

    internal static GuardarValeRequestDto Valida(string codigo = "VAL-47")
    {
        return new GuardarValeRequestDto
        {
            Codigo = codigo,
            Placa = " abc-470 ",
            Estacion = " Estación Norte ",
            FechaCarga = new DateTime(2026, 11, 22, 10, 30, 12, 345),
            TipoCombustible = " diesel ",
            Litros = 50m,
            PrecioLitro = 5.125m,
            Moneda = " pen ",
            OdometroInicial = 12000,
            OdometroFinal = 12500,
            Observaciones = " "
        };
    }

    private async Task<int> CrearAsync(GuardarValeRequestDto? datos = null)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, datos ?? Valida());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<ValeDetalleDto> DetalleAsync(int id)
    {
        ValeDetalleDto? detalle = await _cliente.GetFromJsonAsync<ValeDetalleDto>($"{Ruta}/{id}");
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
    [InlineData("Estacion", "estación", 150)]
    public async Task ValidaCadaTextoSinPersistir(string campo, string etiqueta, int maximo)
    {
        foreach (bool largo in new[] { false, true })
        {
            GuardarValeRequestDto datos = Valida();
            typeof(GuardarValeRequestDto).GetProperty(campo)!.SetValue(datos, largo ? new string('x', maximo + 1) : " ");
            await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest,
                largo ? $"El campo {etiqueta} no puede superar {maximo} caracteres." : $"El campo {etiqueta} es obligatorio.");
            Assert.Equal(0, (await _cliente.GetFromJsonAsync<ResumenValesDto>(Ruta + "/resumen"))!.Total);
        }
    }

    [Theory]
    [InlineData("Litros", "0", "Los litros deben estar entre 0.001 y 9999999.999.")]
    [InlineData("Litros", "10000000", "Los litros deben estar entre 0.001 y 9999999.999.")]
    [InlineData("Litros", "1.0001", "Los litros no pueden tener más de 3 decimales.")]
    [InlineData("PrecioLitro", "0", "El precio por litro debe estar entre 0.001 y 9999999.999.")]
    [InlineData("PrecioLitro", "10000000", "El precio por litro debe estar entre 0.001 y 9999999.999.")]
    [InlineData("PrecioLitro", "1.0001", "El precio por litro no puede tener más de 3 decimales.")]
    public async Task ValidaRangoYEscala(string campo, string valor, string mensaje)
    {
        GuardarValeRequestDto datos = Valida();
        typeof(GuardarValeRequestDto).GetProperty(campo)!.SetValue(datos, decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture));
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [InlineData("OdometroInicial", -1, "El odómetro inicial debe estar entre 0 y 999999999.")]
    [InlineData("OdometroInicial", 1000000000, "El odómetro inicial debe estar entre 0 y 999999999.")]
    [InlineData("OdometroFinal", -1, "El odómetro final debe estar entre 0 y 999999999.")]
    [InlineData("OdometroFinal", 1000000000, "El odómetro final debe estar entre 0 y 999999999.")]
    [InlineData("OdometroFinal", 12000, "El odómetro final debe ser mayor que el inicial.")]
    [InlineData("OdometroFinal", 11999, "El odómetro final debe ser mayor que el inicial.")]
    public async Task ValidaOdometros(string campo, int valor, string mensaje)
    {
        GuardarValeRequestDto datos = Valida();
        typeof(GuardarValeRequestDto).GetProperty(campo)!.SetValue(datos, valor);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, mensaje);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExigeFecha(bool defecto)
    {
        GuardarValeRequestDto datos = Valida(); datos.FechaCarga = defecto ? default(DateTime) : null;
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La fecha de carga es obligatoria.");
    }

    [Theory]
    [InlineData("TipoCombustible", "GNV", "El combustible debe ser Diesel o Gasolina.")]
    [InlineData("Moneda", "", "La moneda debe contener 3 letras mayúsculas.")]
    [InlineData("Moneda", "P3N", "La moneda debe contener 3 letras mayúsculas.")]
    [InlineData("Moneda", "PENN", "La moneda debe contener 3 letras mayúsculas.")]
    public async Task ValidaTipoYMoneda(string campo, string valor, string mensaje)
    {
        GuardarValeRequestDto datos = Valida(); typeof(GuardarValeRequestDto).GetProperty(campo)!.SetValue(datos, valor);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task RechazaObservacionesLargasYEdicionInvalidaNoCambiaNada()
    {
        int id = await CrearAsync(); GuardarValeRequestDto datos = Valida(); datos.Observaciones = new string('x', 501); datos.Placa = "OTRA";
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos), HttpStatusCode.BadRequest, "Las observaciones no pueden superar 500 caracteres.");
        ValeDetalleDto intacto = await DetalleAsync(id); Assert.Equal("ABC-470", intacto.Placa); Assert.Null(intacto.Observaciones);
        datos.Observaciones = new string('x', 500);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        Assert.Equal(500, (await DetalleAsync(id)).Observaciones!.Length);
    }

    [Theory]
    [InlineData("50", "5.125", 500, "256.25", "10.000", "10.000")]
    [InlineData("1", "1.005", 1, "1.01", "1.000", "100.000")]
    [InlineData("16", "1", 1, "16.00", "0.063", "1600.000")]
    [InlineData("0.001", "1", 200, "0.00", "200000.000", "0.001")]
    [InlineData("9999999.999", "9999999.999", 999999999, "99999999980000.00", "100.000", "1.000")]
    [InlineData("0.001", "0.001", 999999999, "0.00", "999999999000.000", "0.000")]
    [InlineData("9999999.999", "1", 1, "10000000.00", "0.000", "999999999.900")]
    public async Task CalculaImporteYRendimientosExactos(string litros, string precio, int kilometros, string importe, string rendimiento, string consumo)
    {
        GuardarValeRequestDto datos = Valida(); datos.Litros = decimal.Parse(litros, System.Globalization.CultureInfo.InvariantCulture); datos.PrecioLitro = decimal.Parse(precio, System.Globalization.CultureInfo.InvariantCulture); datos.OdometroInicial = 0; datos.OdometroFinal = kilometros;
        int id = await CrearAsync(datos); ValeDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal(importe, detalle.Importe); Assert.Equal(rendimiento, detalle.KmPorLitro); Assert.Equal(consumo, detalle.LitrosPor100Km); Assert.Equal(kilometros, detalle.KilometrosRecorridos);
    }

    [Fact]
    public async Task NormalizaYPermiteIntervaloPendientePeroNoConfirmarlo()
    {
        GuardarValeRequestDto datos = Valida(); datos.OdometroFinal = null;
        int id = await CrearAsync(datos); ValeDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal("ABC-470", detalle.Placa); Assert.Equal("Diesel", detalle.TipoCombustible); Assert.Equal("PEN", detalle.Moneda); Assert.Equal(DateTimeKind.Utc, detalle.FechaCarga.Kind); Assert.Equal(345, detalle.FechaCarga.Millisecond); Assert.Null(detalle.Observaciones); Assert.Null(detalle.KmPorLitro); Assert.Null(detalle.LitrosPor100Km); Assert.Null(detalle.KilometrosRecorridos);
        await ErrorAsync(await _cliente.PostAsync($"{Ruta}/{id}/confirmacion", null), HttpStatusCode.Conflict, "Registra el odómetro final antes de confirmar el vale.");
        Assert.Null((await DetalleAsync(id)).FechaConfirmacion);
        datos.OdometroFinal = 12500;
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{id}/confirmacion", null)).StatusCode);
    }

    [Fact]
    public async Task CodigoUnicoEnAltaYEdicion()
    {
        await CrearAsync(); await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, Valida(" val-47 ")), HttpStatusCode.Conflict, "Ya existe un vale con el código val-47.");
        int otro = await CrearAsync(Valida("OTRO")); await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{otro}", Valida("val-47")), HttpStatusCode.Conflict, "Ya existe un vale con el código val-47."); Assert.Equal("OTRO", (await DetalleAsync(otro)).Codigo);
    }

    [Fact]
    public async Task ConfirmaYAnulaConservandoDatosYFechas()
    {
        int id = await CrearAsync(); Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{id}/confirmacion", null)).StatusCode); ValeDetalleDto confirmado = await DetalleAsync(id); Assert.Equal("Confirmado", confirmado.Estado); Assert.Equal(DateTimeKind.Utc, confirmado.FechaConfirmacion!.Value.Kind);
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida()), HttpStatusCode.Conflict, "Solo se puede editar un vale en borrador."); await ErrorAsync(await _cliente.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.Conflict, "Solo se puede eliminar un vale en borrador."); await ErrorAsync(await _cliente.PostAsync($"{Ruta}/{id}/confirmacion", null), HttpStatusCode.Conflict, "Solo se puede confirmar un vale en borrador.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = " " }), HttpStatusCode.BadRequest, "El motivo de anulación es obligatorio."); await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = new string('x', 501) }), HttpStatusCode.BadRequest, "El motivo de anulación no puede superar 500 caracteres.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = " Cambio " })).StatusCode); ValeDetalleDto anulado = await DetalleAsync(id); Assert.Equal("Anulado", anulado.Estado); Assert.Equal(confirmado.FechaConfirmacion, anulado.FechaConfirmacion); Assert.Equal("Cambio", anulado.MotivoAnulacion); Assert.Equal(DateTimeKind.Utc, anulado.FechaAnulacion!.Value.Kind); Assert.Equal(confirmado.Importe, anulado.Importe); Assert.Equal(confirmado.KmPorLitro, anulado.KmPorLitro);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = "Otra" }), HttpStatusCode.Conflict, "El vale ya está anulado."); await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida()), HttpStatusCode.Conflict, "Solo se puede editar un vale en borrador.");
    }

    [Fact]
    public async Task AdmiteLimitesTextualesYAnulaBorradorConFinalPendiente()
    {
        GuardarValeRequestDto datos = Valida(new string('x', 20)); datos.Placa = new string('x', 20); datos.Estacion = new string('x', 150); datos.Observaciones = new string('x', 500); datos.OdometroInicial = 999999999; datos.OdometroFinal = null; datos.Moneda = "zzz"; datos.TipoCombustible = " gASolina ";
        int id = await CrearAsync(datos); Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/anulacion", new { motivo = new string('x', 500) })).StatusCode); ValeDetalleDto detalle = await DetalleAsync(id); Assert.Equal("ZZZ", detalle.Moneda); Assert.Equal("Gasolina", detalle.TipoCombustible); Assert.Equal(500, detalle.MotivoAnulacion!.Length); Assert.Null(detalle.FechaConfirmacion); Assert.Null(detalle.KmPorLitro);
    }

    [Fact]
    public async Task EliminaUnicamenteBorrador()
    {
        int id = await CrearAsync(); Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode); await ErrorAsync(await _cliente.GetAsync($"{Ruta}/{id}"), HttpStatusCode.NotFound, $"El vale de combustible {id} no existe.");
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("POST", "/confirmacion")]
    [InlineData("POST", "/anulacion")]
    public async Task BuscaAntesDeValidar(string metodo, string sufijo)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + "/2147483647" + sufijo); if (metodo == "PUT" || sufijo == "/anulacion") { peticion.Content = JsonContent.Create(new { }); }
        await ErrorAsync(await _cliente.SendAsync(peticion), HttpStatusCode.NotFound, "El vale de combustible 2147483647 no existe.");
    }

    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Borrador, Confirmado o Anulado.")]
    [InlineData("tipoCombustible=GNV", "El combustible debe ser Diesel o Gasolina.")]
    public async Task ValidaConsulta(string query, string mensaje)
    {
        await ErrorAsync(await _cliente.GetAsync(Ruta + "?" + query), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task CombinaFiltrosOrdenYPaginacionSinSumarMonedas()
    {
        int b = await CrearAsync(Valida("B")); int a = await CrearAsync(Valida("A")); GuardarValeRequestDto tercero = Valida("C"); tercero.TipoCombustible = "Gasolina"; tercero.Moneda = "USD"; await CrearAsync(tercero); await _cliente.PostAsync($"{Ruta}/{b}/confirmacion", null);
        foreach (string busqueda in new[] { "abc-470", "estación norte" }) { PaginaValesDto pagina = (await _cliente.GetFromJsonAsync<PaginaValesDto>($"{Ruta}?busqueda={Uri.EscapeDataString(busqueda)}&pagina=2&tamanoPagina=1"))!; Assert.Equal(3, pagina.Total); Assert.Equal(b, Assert.Single(pagina.Items).Id); }
        Assert.Equal(a, Assert.Single((await _cliente.GetFromJsonAsync<PaginaValesDto>($"{Ruta}?busqueda=a&estado=borrador&tipoCombustible=dIeSeL"))!.Items).Id); Assert.Empty((await _cliente.GetFromJsonAsync<PaginaValesDto>($"{Ruta}?pagina=2147483647&tamanoPagina=100"))!.Items);
        ResumenValesDto resumen = (await _cliente.GetFromJsonAsync<ResumenValesDto>(Ruta + "/resumen"))!; Assert.Equal(3, resumen.Total); Assert.Equal(2, resumen.Borradores); Assert.Equal(1, resumen.Confirmados); Assert.Equal(0, resumen.Anulados); Assert.DoesNotContain("importe", await _cliente.GetStringAsync(Ruta + "/resumen"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExigeToken()
    {
        using HttpClient anonimo = _fabrica.CreateClient(); anonimo.DefaultRequestHeaders.Authorization = null; Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Ruta)).StatusCode);
    }
}
