using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo49_LogisticaInversa;

[Collection("Modulo04")]
public class RetornosApiTests
{
    private const string Ruta = "/api/v1/logistica-inversa";
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;

    public RetornosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<RetornoLogistico>());
        db.SaveChanges();
    }

    internal static GuardarRetornoRequestDto Valida(string codigo = "RET-49")
    {
        return new GuardarRetornoRequestDto
        {
            Codigo = codigo,
            ReferenciaOrigen = " GUIA-49 ",
            Contraparte = " Cliente Norte ",
            PuntoRecogida = " Muelle Norte ",
            PuntoRecepcion = " Almacén Central ",
            Descripcion = " Pallet reutilizable ",
            Unidad = " unidad ",
            Tipo = " pallet ",
            FechaProgramada = new DateTime(2026, 11, 22, 10, 30, 12, 345),
            CantidadProgramada = 10.125m,
            Observaciones = " "
        };
    }

    private async Task<int> CrearAsync(GuardarRetornoRequestDto? datos = null)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, datos ?? Valida());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<RetornoDetalleDto> DetalleAsync(int id)
    {
        RetornoDetalleDto? detalle = await _cliente.GetFromJsonAsync<RetornoDetalleDto>($"{Ruta}/{id}");
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
    [InlineData("ReferenciaOrigen", "referencia de origen", 40)]
    [InlineData("Contraparte", "contraparte", 200)]
    [InlineData("PuntoRecogida", "punto de recogida", 200)]
    [InlineData("PuntoRecepcion", "punto de recepción", 200)]
    [InlineData("Descripcion", "descripción", 200)]
    [InlineData("Unidad", "unidad", 20)]
    public async Task ValidaTextosYLimitesSinPersistir(string campo, string etiqueta, int maximo)
    {
        foreach (bool largo in new[] { false, true })
        {
            GuardarRetornoRequestDto datos = Valida();
            typeof(GuardarRetornoRequestDto).GetProperty(campo)!.SetValue(datos, largo ? new string('x', maximo + 1) : " ");
            await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, largo ? $"El campo {etiqueta} no puede superar {maximo} caracteres." : $"El campo {etiqueta} es obligatorio.");
            Assert.Equal(0, (await _cliente.GetFromJsonAsync<ResumenRetornosDto>(Ruta + "/resumen"))!.Total);
            typeof(GuardarRetornoRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', maximo));
            int id = await CrearAsync(datos);
            Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        }
    }
    [Theory]
    [InlineData("0", "La cantidad programada debe estar entre 0.001 y 999999999.999.")]
    [InlineData("1000000000", "La cantidad programada debe estar entre 0.001 y 999999999.999.")]
    [InlineData("1.0001", "La cantidad programada no puede tener más de 3 decimales.")]
    public async Task ValidaCantidadProgramadaSinCambiarDatos(string valor, string mensaje)
    {
        int id = await CrearAsync();
        GuardarRetornoRequestDto datos = Valida();
        datos.CantidadProgramada = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos), HttpStatusCode.BadRequest, mensaje);
        Assert.Equal("10.125", (await DetalleAsync(id)).CantidadProgramada);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExigeFecha(bool defecto)
    {
        GuardarRetornoRequestDto datos = Valida();
        datos.FechaProgramada = defecto ? default(DateTime) : null;
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La fecha programada es obligatoria.");
    }
    [Fact]
    public async Task ValidaTipoYObservaciones()
    {
        GuardarRetornoRequestDto datos = Valida();
        datos.Tipo = "Otro";
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "El tipo debe ser Pallet, Retornable o DevolucionRuta.");
        datos.Tipo = "Retornable";
        datos.Observaciones = new string('x', 501);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "Las observaciones no pueden superar 500 caracteres.");
        datos.Observaciones = new string('x', 500);
        int id = await CrearAsync(datos);
        Assert.Equal(500, (await DetalleAsync(id)).Observaciones!.Length);
    }
    [Theory]
    [InlineData("0.001")]
    [InlineData("999999999.999")]
    public async Task PreservaExtremosDecimalesYUtc(string valor)
    {
        GuardarRetornoRequestDto datos = Valida();
        datos.CantidadProgramada = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        int id = await CrearAsync(datos);
        RetornoDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal(valor, detalle.CantidadProgramada);
        Assert.Equal("GUIA-49", detalle.ReferenciaOrigen);
        Assert.Equal("Pallet", detalle.Tipo);
        Assert.Null(detalle.Observaciones);
        Assert.Null(detalle.NoRecogida);
        Assert.Null(detalle.CantidadRecibida);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaProgramada.Kind);
        Assert.Equal(345, detalle.FechaProgramada.Millisecond);
    }
    [Fact]
    public async Task CodigoUnicoNormalizadoEnAltaYEdicion()
    {
        await CrearAsync();
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, Valida(" ret-49 ")), HttpStatusCode.Conflict, "Ya existe un retorno con el código ret-49.");
        int otro = await CrearAsync(Valida("OTRO"));
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{otro}", Valida("ret-49")), HttpStatusCode.Conflict, "Ya existe un retorno con el código ret-49.");
        Assert.Equal("OTRO", (await DetalleAsync(otro)).Codigo);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{otro}", Valida("EDITADO"))).StatusCode);
        Assert.Equal("EDITADO", (await DetalleAsync(otro)).Codigo);
    }
    [Theory]
    [InlineData("recogida", "0", "La cantidad debe estar entre 0.001 y 999999999.999.")]
    [InlineData("recogida", "1000000000", "La cantidad debe estar entre 0.001 y 999999999.999.")]
    [InlineData("recogida", "1.0001", "La cantidad no puede tener más de 3 decimales.")]
    [InlineData("recogida", "11", "La cantidad recogida no puede superar la programada.")]
    [InlineData("recepcion", "0", "La cantidad debe estar entre 0.001 y 999999999.999.")]
    [InlineData("recepcion", "1.0001", "La cantidad no puede tener más de 3 decimales.")]
    [InlineData("recepcion", "10", "La cantidad recibida no puede superar la recogida.")]
    public async Task ValidaCadaEtapaSinMutarla(string etapa, string cantidad, string mensaje)
    {
        int id = await CrearAsync();
        if (etapa == "recepcion")
        {
            Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recogida", new { cantidad = 9m, responsable = "Ana" })).StatusCode);
        }
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/{etapa}", new { cantidad = decimal.Parse(cantidad, System.Globalization.CultureInfo.InvariantCulture), responsable = "Ana" }), HttpStatusCode.BadRequest, mensaje);
        RetornoDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal(etapa == "recogida" ? "Programado" : "Recogido", detalle.Estado);
        Assert.Null(detalle.FechaRecepcion);
    }
    [Theory]
    [InlineData("recogida", false)]
    [InlineData("recogida", true)]
    [InlineData("recepcion", false)]
    [InlineData("recepcion", true)]
    public async Task ValidaResponsablePorEtapa(string etapa, bool largo)
    {
        int id = await CrearAsync();
        if (etapa == "recepcion") { await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recogida", new { cantidad = 9m, responsable = "Ana" }); }
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/{etapa}", new { cantidad = 8m, responsable = largo ? new string('x', 101) : " " }), HttpStatusCode.BadRequest, largo ? "El campo responsable no puede superar 100 caracteres." : "El campo responsable es obligatorio.");
        Assert.Null((await DetalleAsync(id)).FechaRecepcion);
    }
    [Fact]
    public async Task RecogeRecibeYConservaDiferenciasConDecimales()
    {
        int id = await CrearAsync();
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recepcion", new { cantidad = 8m, responsable = "Luis" }), HttpStatusCode.Conflict, "Solo se puede recibir un retorno recogido.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recogida", new { cantidad = 9.125m, responsable = " Ana " })).StatusCode);
        RetornoDetalleDto recogido = await DetalleAsync(id);
        Assert.Equal("1.000", recogido.NoRecogida);
        Assert.Equal("Ana", recogido.ResponsableRecogida);
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida()), HttpStatusCode.Conflict, "Solo se puede editar un retorno programado.");
        await ErrorAsync(await _cliente.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.Conflict, "Solo se puede eliminar un retorno programado.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recogida", new { cantidad = 9m, responsable = "Ana" }), HttpStatusCode.Conflict, "Solo se puede recoger un retorno programado.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recepcion", new { cantidad = 8.001m, responsable = new string('x', 100) })).StatusCode);
        RetornoDetalleDto recibido = await DetalleAsync(id);
        Assert.Equal("Recibido", recibido.Estado);
        Assert.Equal("1.124", recibido.NoRecibida);
        Assert.Equal(recogido.FechaRecogida, recibido.FechaRecogida);
        Assert.Equal(DateTimeKind.Utc, recibido.FechaRecepcion!.Value.Kind);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = "Cambio" }), HttpStatusCode.Conflict, "Solo se puede cancelar un retorno programado o recogido.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recepcion", new { cantidad = 8m, responsable = "Luis" }), HttpStatusCode.Conflict, "Solo se puede recibir un retorno recogido.");
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelaYConservaRecogidaCuandoExiste(bool recoger)
    {
        int id = await CrearAsync();
        if (recoger) { await _cliente.PostAsJsonAsync($"{Ruta}/{id}/recogida", new { cantidad = 9m, responsable = "Ana" }); }
        RetornoDetalleDto antes = await DetalleAsync(id);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = " " }), HttpStatusCode.BadRequest, "El motivo de cancelación es obligatorio.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = new string('x', 501) }), HttpStatusCode.BadRequest, "El motivo de cancelación no puede superar 500 caracteres.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = new string('x', 500) })).StatusCode);
        RetornoDetalleDto cancelado = await DetalleAsync(id);
        Assert.Equal("Cancelado", cancelado.Estado);
        Assert.Equal(antes.FechaRecogida, cancelado.FechaRecogida);
        Assert.Equal(antes.CantidadRecogida, cancelado.CantidadRecogida);
        Assert.Equal(DateTimeKind.Utc, cancelado.FechaCancelacion!.Value.Kind);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = "Otro" }), HttpStatusCode.Conflict, "El retorno ya está cancelado.");
    }
    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("POST", "/recogida")]
    [InlineData("POST", "/recepcion")]
    [InlineData("POST", "/cancelacion")]
    public async Task BuscaAntesDeValidar(string metodo, string sufijo)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + "/2147483647" + sufijo);
        if (metodo == "PUT" || metodo == "POST") { peticion.Content = JsonContent.Create(new { }); }
        await ErrorAsync(await _cliente.SendAsync(peticion), HttpStatusCode.NotFound, "El retorno 2147483647 no existe.");
    }
    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Programado, Recogido, Recibido o Cancelado.")]
    [InlineData("tipo=Otro", "El tipo debe ser Pallet, Retornable o DevolucionRuta.")]
    public async Task ValidaConsulta(string query, string mensaje)
    {
        await ErrorAsync(await _cliente.GetAsync(Ruta + "?" + query), HttpStatusCode.BadRequest, mensaje);
    }
    [Fact]
    public async Task CombinaFiltrosYPaginaSinSumarUnidades()
    {
        int b = await CrearAsync(Valida("B"));
        int a = await CrearAsync(Valida("A"));
        GuardarRetornoRequestDto otro = Valida("C");
        otro.Tipo = "DevolucionRuta";
        otro.Unidad = "kg";
        await CrearAsync(otro);
        await _cliente.PostAsJsonAsync($"{Ruta}/{b}/recogida", new { cantidad = 8m, responsable = "Ana" });
        foreach (string busqueda in new[] { "guia-49", "cliente norte", "pallet reutilizable" })
        {
            PaginaRetornosDto pagina = (await _cliente.GetFromJsonAsync<PaginaRetornosDto>($"{Ruta}?busqueda={Uri.EscapeDataString(busqueda)}&pagina=2&tamanoPagina=1"))!;
            Assert.Equal(3, pagina.Total);
            Assert.Equal(b, Assert.Single(pagina.Items).Id);
        }
        Assert.Equal(a, Assert.Single((await _cliente.GetFromJsonAsync<PaginaRetornosDto>($"{Ruta}?busqueda=a&estado=programado&tipo=pAlLeT"))!.Items).Id);
        Assert.Empty((await _cliente.GetFromJsonAsync<PaginaRetornosDto>($"{Ruta}?pagina=2147483647&tamanoPagina=100"))!.Items);
        ResumenRetornosDto resumen = (await _cliente.GetFromJsonAsync<ResumenRetornosDto>(Ruta + "/resumen"))!;
        Assert.Equal(3, resumen.Total);
        Assert.Equal(2, resumen.Programados);
        Assert.Equal(1, resumen.Recogidos);
        Assert.DoesNotContain("cantidad", await _cliente.GetStringAsync(Ruta + "/resumen"), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task BajaProgramadoYExigeJwt()
    {
        int id = await CrearAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        await ErrorAsync(await _cliente.GetAsync($"{Ruta}/{id}"), HttpStatusCode.NotFound, $"El retorno {id} no existe.");
        using HttpClient anonimo = _fabrica.CreateClient();
        anonimo.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Ruta)).StatusCode);
    }
}
