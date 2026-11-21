using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo46_MonitoreoEntregas;

[Collection("Modulo04")]
public class MonitoreoEntregasApiTests
{
    private const string Ruta = "/api/v1/monitoreo-entregas";
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;

    public MonitoreoEntregasApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<EventoEntrega>());
        db.RemoveRange(db.Set<EntregaDespacho>());
        db.SaveChanges();
    }

    internal static GuardarEntregaRequestDto Valida(string codigo = "ENV-46")
    {
        return new GuardarEntregaRequestDto
        {
            Codigo = codigo,
            ReferenciaGuia = " T045-1 ",
            Destinatario = " Centro Sur ",
            DireccionDestino = " Avenida 100 ",
            FechaProgramada = new DateTime(2026, 11, 22, 10, 30, 12, 345),
            Observaciones = " "
        };
    }

    private static RegistrarPodRequestDto Pod()
    {
        return new RegistrarPodRequestDto { Receptor = " Receptor ", DocumentoReceptor = " DOC-46 ", ReferenciaConstancia = " POD-46 ", Observaciones = " Recibido " };
    }

    private async Task<int> CrearAsync(GuardarEntregaRequestDto? datos = null)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, datos ?? Valida());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return cuerpo.GetProperty("id").GetInt32();
    }

    private async Task<EntregaDetalleDto> DetalleAsync(int id)
    {
        EntregaDetalleDto? detalle = await _cliente.GetFromJsonAsync<EntregaDetalleDto>($"{Ruta}/{id}");
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
    [InlineData("ReferenciaGuia", "referencia de guía", 40)]
    [InlineData("Destinatario", "destinatario", 200)]
    [InlineData("DireccionDestino", "dirección de destino", 300)]
    public async Task ValidaTextosDeProgramacionSinPersistir(string campo, string etiqueta, int maximo)
    {
        foreach (bool largo in new[] { false, true })
        {
            GuardarEntregaRequestDto datos = Valida();
            typeof(GuardarEntregaRequestDto).GetProperty(campo)!.SetValue(datos, largo ? new string('x', maximo + 1) : " ");
            string mensaje = largo ? $"El campo {etiqueta} no puede superar {maximo} caracteres." : $"El campo {etiqueta} es obligatorio.";
            await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, mensaje);
            Assert.Equal(0, (await _cliente.GetFromJsonAsync<ResumenEntregasDto>(Ruta + "/resumen"))!.Total);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FechaObligatoria(bool defecto)
    {
        GuardarEntregaRequestDto datos = Valida();
        datos.FechaProgramada = defecto ? default(DateTime) : null;
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La fecha programada es obligatoria.");
    }

    [Fact]
    public async Task ObservacionesLimitadas()
    {
        GuardarEntregaRequestDto datos = Valida();
        datos.Observaciones = new string('x', 501);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "Las observaciones no pueden superar 500 caracteres.");
    }

    [Fact]
    public async Task CreaNormalizaEditaSinAlterarEventoYEliminaConCascada()
    {
        int id = await CrearAsync();
        EntregaDetalleDto original = await DetalleAsync(id);
        Assert.Equal("T045-1", original.ReferenciaGuia);
        Assert.Equal("Centro Sur", original.Destinatario);
        Assert.Null(original.Observaciones);
        Assert.Equal(DateTimeKind.Utc, original.FechaProgramada.Kind);
        Assert.Equal(345, original.FechaProgramada.Millisecond);
        EventoEntregaDto evento = Assert.Single(original.Historial);
        Assert.Equal("Programada", evento.Estado);
        Assert.Equal("Entrega programada.", evento.Descripcion);
        Assert.Equal(DateTimeKind.Utc, evento.Fecha.Kind);
        GuardarEntregaRequestDto datos = Valida();
        datos.Destinatario = "Nuevo destino";
        datos.Observaciones = new string('x', 501);
        Assert.Equal(HttpStatusCode.BadRequest, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        Assert.Equal(original.Destinatario, (await DetalleAsync(id)).Destinatario);
        datos.Observaciones = "Editada";
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        EntregaDetalleDto editada = await DetalleAsync(id);
        Assert.Equal("Nuevo destino", editada.Destinatario);
        Assert.Equal(evento.Id, Assert.Single(editada.Historial).Id);
        Assert.Equal(evento.Fecha, editada.Historial[0].Fecha);
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        using IServiceScope scope = _fabrica.Services.CreateScope();
        Assert.DoesNotContain(scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>().Set<EventoEntrega>(), e => e.EntregaDespachoId == id);
    }

    [Fact]
    public async Task AceptaLimitesDeProgramacion()
    {
        GuardarEntregaRequestDto datos = Valida();
        datos.Codigo = new string('x', 20);
        datos.ReferenciaGuia = new string('x', 40);
        datos.Destinatario = new string('x', 200);
        datos.DireccionDestino = new string('x', 300);
        datos.Observaciones = new string('x', 500);
        int id = await CrearAsync(datos);
        EntregaDetalleDto detalle = await DetalleAsync(id);
        Assert.Equal(500, detalle.Observaciones!.Length);
        Assert.Equal(300, detalle.DireccionDestino.Length);
    }

    [Fact]
    public async Task CodigoUnicoEnAltaYEdicionSinDistinguirMayusculas()
    {
        await CrearAsync();
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, Valida(" env-46 ")), HttpStatusCode.Conflict, "Ya existe una entrega con el código env-46.");
        int otro = await CrearAsync(Valida("OTRO"));
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{otro}", Valida("env-46")), HttpStatusCode.Conflict, "Ya existe una entrega con el código env-46.");
        Assert.Equal("OTRO", (await DetalleAsync(otro)).Codigo);
    }

    [Fact]
    public async Task SalidaYRecepcionConservanFechasYPodEHistorial()
    {
        int id = await CrearAsync();
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/entrega", Pod()), HttpStatusCode.Conflict, "Solo se puede completar una entrega en ruta.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{id}/salida", null)).StatusCode);
        EntregaDetalleDto ruta = await DetalleAsync(id);
        Assert.Equal("EnRuta", ruta.Estado);
        Assert.Equal(2, ruta.Historial.Count);
        Assert.Equal(ruta.FechaSalida, ruta.Historial[1].Fecha);
        Assert.Equal("Salida a ruta registrada.", ruta.Historial[1].Descripcion);
        Assert.Equal(DateTimeKind.Utc, ruta.FechaSalida!.Value.Kind);
        await ErrorAsync(await _cliente.PostAsync($"{Ruta}/{id}/salida", null), HttpStatusCode.Conflict, "Solo se puede iniciar una entrega programada.");
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida()), HttpStatusCode.Conflict, "Solo se puede editar una entrega programada.");
        await ErrorAsync(await _cliente.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.Conflict, "Solo se puede eliminar una entrega programada.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/entrega", Pod())).StatusCode);
        EntregaDetalleDto recibida = await DetalleAsync(id);
        Assert.Equal("Entregada", recibida.Estado);
        Assert.Equal(3, recibida.Historial.Count);
        Assert.Equal(new[] { "Programada", "EnRuta", "Entregada" }, recibida.Historial.Select(e => e.Estado));
        Assert.Equal(ruta.FechaSalida, recibida.FechaSalida);
        Assert.Equal(recibida.FechaEntrega, recibida.Historial[2].Fecha);
        Assert.Equal("Recepción registrada mediante constancia manual.", recibida.Historial[2].Descripcion);
        Assert.Equal(DateTimeKind.Utc, recibida.FechaEntrega!.Value.Kind);
        Assert.Equal("Receptor", recibida.Receptor);
        Assert.Equal("DOC-46", recibida.DocumentoReceptor);
        Assert.Equal("POD-46", recibida.ReferenciaConstancia);
        Assert.Equal("Recibido", recibida.ObservacionesEntrega);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/entrega", Pod()), HttpStatusCode.Conflict, "Solo se puede completar una entrega en ruta.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = "Otra" }), HttpStatusCode.Conflict, "Solo se puede cancelar una entrega programada o en ruta.");
        Assert.Equal(3, (await DetalleAsync(id)).Historial.Count);
    }

    [Theory]
    [InlineData("Receptor", "receptor", 100)]
    [InlineData("DocumentoReceptor", "documento del receptor", 20)]
    [InlineData("ReferenciaConstancia", "referencia de constancia", 100)]
    public async Task ValidaCadaCampoPodSinRegistrarUnEvento(string campo, string etiqueta, int maximo)
    {
        int id = await CrearAsync();
        await _cliente.PostAsync($"{Ruta}/{id}/salida", null);
        foreach (bool largo in new[] { false, true })
        {
            RegistrarPodRequestDto datos = Pod();
            typeof(RegistrarPodRequestDto).GetProperty(campo)!.SetValue(datos, largo ? new string('x', maximo + 1) : " ");
            await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/entrega", datos), HttpStatusCode.BadRequest,
                largo ? $"El campo {etiqueta} no puede superar {maximo} caracteres." : $"El campo {etiqueta} es obligatorio.");
            EntregaDetalleDto intacta = await DetalleAsync(id);
            Assert.Equal("EnRuta", intacta.Estado);
            Assert.Null(intacta.Receptor);
            Assert.Equal(2, intacta.Historial.Count);
        }
    }

    [Fact]
    public async Task AcotaObservacionesPodYAdmiteTodosSusLimites()
    {
        int id = await CrearAsync();
        await _cliente.PostAsync($"{Ruta}/{id}/salida", null);
        RegistrarPodRequestDto datos = Pod();
        datos.Observaciones = new string('x', 501);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/entrega", datos), HttpStatusCode.BadRequest, "Las observaciones de entrega no pueden superar 500 caracteres.");
        datos.Receptor = new string('x', 100); datos.DocumentoReceptor = new string('x', 20); datos.ReferenciaConstancia = new string('x', 100); datos.Observaciones = new string('x', 500);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/entrega", datos)).StatusCode);
        Assert.Equal(500, (await DetalleAsync(id)).ObservacionesEntrega!.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelaDesdeProgramadaOEnRutaConservandoHistorial(bool salir)
    {
        int id = await CrearAsync();
        if (salir) { await _cliente.PostAsync($"{Ruta}/{id}/salida", null); }
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = " " }), HttpStatusCode.BadRequest, "El motivo de cancelación es obligatorio.");
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = new string('x', 501) }), HttpStatusCode.BadRequest, "El motivo de cancelación no puede superar 500 caracteres.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = " Cambio " })).StatusCode);
        EntregaDetalleDto cancelada = await DetalleAsync(id);
        Assert.Equal("Cancelada", cancelada.Estado);
        Assert.Equal("Cambio", cancelada.MotivoCancelacion);
        Assert.Equal(salir ? 3 : 2, cancelada.Historial.Count);
        Assert.Equal("Cambio", cancelada.Historial.Last().Descripcion);
        Assert.Equal(cancelada.FechaCancelacion, cancelada.Historial.Last().Fecha);
        Assert.Equal(DateTimeKind.Utc, cancelada.FechaCancelacion!.Value.Kind);
        Assert.Equal(salir, cancelada.FechaSalida.HasValue);
        Assert.Null(cancelada.FechaEntrega);
        await ErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = "Otra" }), HttpStatusCode.Conflict, "La entrega ya está cancelada.");
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida()), HttpStatusCode.Conflict, "Solo se puede editar una entrega programada.");
        await ErrorAsync(await _cliente.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.Conflict, "Solo se puede eliminar una entrega programada.");
    }

    [Fact]
    public async Task AceptaMotivoDe500YObservacionesPodVacias()
    {
        int id = await CrearAsync();
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelacion", new { motivo = new string('x', 500) })).StatusCode);
        int otro = await CrearAsync(Valida("OTRA"));
        await _cliente.PostAsync($"{Ruta}/{otro}/salida", null);
        RegistrarPodRequestDto datos = Pod(); datos.Observaciones = " ";
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{Ruta}/{otro}/entrega", datos)).StatusCode);
        Assert.Null((await DetalleAsync(otro)).ObservacionesEntrega);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("POST", "/salida")]
    [InlineData("POST", "/entrega")]
    [InlineData("POST", "/cancelacion")]
    public async Task BuscaPrimero(string metodo, string sufijo)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + "/2147483647" + sufijo);
        if (metodo == "PUT" || sufijo == "/entrega" || sufijo == "/cancelacion") { peticion.Content = JsonContent.Create(new { }); }
        await ErrorAsync(await _cliente.SendAsync(peticion), HttpStatusCode.NotFound, "La entrega 2147483647 no existe.");
    }

    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Programada, EnRuta, Entregada o Cancelada.")]
    public async Task ValidaConsulta(string query, string mensaje)
    {
        await ErrorAsync(await _cliente.GetAsync(Ruta + "?" + query), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task FiltraOrdenaPaginaYResumeTodosLosEstados()
    {
        int b = await CrearAsync(Valida("B")); int a = await CrearAsync(Valida("A"));
        int c = await CrearAsync(Valida("C")); int d = await CrearAsync(Valida("D"));
        await _cliente.PostAsync($"{Ruta}/{b}/salida", null);
        await _cliente.PostAsync($"{Ruta}/{c}/salida", null);
        await _cliente.PostAsJsonAsync($"{Ruta}/{c}/entrega", Pod());
        await _cliente.PostAsJsonAsync($"{Ruta}/{d}/cancelacion", new { motivo = "Cancelada" });
        foreach (string busqueda in new[] { "t045", "centro sur" })
        {
            PaginaEntregasDto pagina = (await _cliente.GetFromJsonAsync<PaginaEntregasDto>($"{Ruta}?busqueda={Uri.EscapeDataString(busqueda)}&pagina=2&tamanoPagina=1"))!;
            Assert.Equal(4, pagina.Total); Assert.Equal(b, Assert.Single(pagina.Items).Id);
        }
        Assert.Equal(a, Assert.Single((await _cliente.GetFromJsonAsync<PaginaEntregasDto>($"{Ruta}?busqueda=a"))!.Items).Id);
        Assert.Equal(b, Assert.Single((await _cliente.GetFromJsonAsync<PaginaEntregasDto>($"{Ruta}?estado=enRUTA"))!.Items).Id);
        Assert.Empty((await _cliente.GetFromJsonAsync<PaginaEntregasDto>($"{Ruta}?pagina=2147483647&tamanoPagina=100"))!.Items);
        Assert.DoesNotContain("historial", await _cliente.GetStringAsync(Ruta), StringComparison.OrdinalIgnoreCase);
        ResumenEntregasDto resumen = (await _cliente.GetFromJsonAsync<ResumenEntregasDto>(Ruta + "/resumen"))!;
        Assert.Equal(4, resumen.Total); Assert.Equal(1, resumen.Programadas); Assert.Equal(1, resumen.EnRuta); Assert.Equal(1, resumen.Entregadas); Assert.Equal(1, resumen.Canceladas);
    }

    [Fact]
    public async Task ExigeToken()
    {
        using HttpClient anonimo = _fabrica.CreateClient(); anonimo.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Ruta)).StatusCode);
    }
}
