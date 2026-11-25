using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo39_MantenimientoPreventivo;

public sealed class MantenimientosPreventivosApiTests : IClassFixture<MantenimientosPreventivosApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/mantenimientos-preventivos";
    private static readonly DateOnly Hoy = FechaPeru.Hoy();
    private static readonly DateOnly Manana = Hoy.AddDays(1);
    private readonly MantenimientosPreventivosApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public MantenimientosPreventivosApiTests(MantenimientosPreventivosApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.PrepararDatos();
        _cliente = fabrica.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, fabrica.Services);
    }

    // «Hoy» va en hora de Perú (UTC-5): 00:00 UTC del 25 son las 19:00 del 24 en Perú, que todavía es el 24.
    [Theory]
    [InlineData(24, 23, 59, 24)]
    [InlineData(25, 0, 0, 24)]
    [InlineData(25, 4, 59, 24)]
    [InlineData(25, 5, 0, 25)]
    public void HoyCambiaDeDiaALaMedianocheDePeru(int diaUtc, int horaUtc, int minutoUtc, int diaEsperado)
    {
        DateTime instanteUtc = new DateTime(2026, 11, diaUtc, horaUtc, minutoUtc, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 11, diaEsperado), FechaPeru.DiaDe(instanteUtc));
    }

    [Fact]
    public async Task AltaNormalizaLosCamposYAsignaElNumero()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearMantenimientoPreventivoRequestDto(1, "  Cambio de rodamientos  ", Manana, 4.5m, 30, "   "));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        MantenimientoPreventivoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<MantenimientoPreventivoCreadoDto>();
        Assert.NotNull(creado);
        Assert.Equal($"MP-{creado.Id:D6}", creado.Numero);
        Assert.Equal($"{Ruta}/{creado.Id}", respuesta.Headers.Location?.AbsolutePath);
        MantenimientoPreventivoDetalleDto detalle = await ObtenerAsync(creado.Id);
        Assert.Equal(new MantenimientoPreventivoDetalleDto(
            creado.Id, creado.Numero, 1, "CT-A", "Ensamblaje", "Cambio de rodamientos", Manana, 4.5m, "Programado",
            false, 30, null, null, null, null, null, null, null), detalle);
    }

    [Theory]
    [InlineData("{\"centroTrabajoId\":0,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1}", 400, "El identificador del centro de trabajo debe ser mayor que 0.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"   \",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1}", 400, "La descripción del mantenimiento es obligatoria.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"LARGA251\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1}", 400, "La descripción del mantenimiento no puede superar 250 caracteres.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"duracionHorasPlanificada\":1}", 400, "La fecha programada es obligatoria.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"fechaProgramada\":\"AYER\",\"duracionHorasPlanificada\":1}", 400, "La fecha programada no puede ser anterior a hoy.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":0}", 400, "La duración debe ser mayor que 0 y como máximo 168 horas.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":168.01}", 400, "La duración debe ser mayor que 0 y como máximo 168 horas.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1.234}", 400, "La duración admite como máximo 2 decimales.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1,\"frecuenciaDias\":0}", 400, "La frecuencia debe estar entre 1 y 3650 días.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1,\"frecuenciaDias\":3651}", 400, "La frecuencia debe estar entre 1 y 3650 días.")]
    [InlineData("{\"centroTrabajoId\":1,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1,\"responsable\":\"LARGA101\"}", 400, "El responsable no puede superar 100 caracteres.")]
    [InlineData("{\"centroTrabajoId\":99,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1}", 400, "El centro de trabajo 99 no existe.")]
    [InlineData("{\"centroTrabajoId\":3,\"descripcion\":\"x\",\"fechaProgramada\":\"MANANA\",\"duracionHorasPlanificada\":1}", 409, "El centro de trabajo 3 está inactivo.")]
    public async Task AltaInvalidaDevuelveElMensajeLiteralSinCrearFilas(string json, int estado, string mensaje)
    {
        string cuerpo = json
            .Replace("MANANA", Manana.ToString("yyyy-MM-dd"))
            .Replace("AYER", Hoy.AddDays(-1).ToString("yyyy-MM-dd"))
            .Replace("LARGA251", new string('a', 251))
            .Replace("LARGA101", new string('a', 101));
        using HttpResponseMessage respuesta = await EnviarJsonAsync(HttpMethod.Post, Ruta, cuerpo);

        await VerificarErrorAsync(respuesta, (HttpStatusCode)estado, mensaje);
        Assert.Equal(0, _fabrica.ContarMantenimientos());
    }

    [Fact]
    public async Task AltaAceptaLosLimitesYHoy()
    {
        int id = await CrearAsync(1, Hoy, 168m, 3650, new string('r', 100), new string('d', 250));
        MantenimientoPreventivoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal(Hoy, detalle.FechaProgramada);
        Assert.Equal(168m, detalle.DuracionHorasPlanificada);
        Assert.Equal(3650, detalle.FrecuenciaDias);
        Assert.False(detalle.Vencido);
        await CrearAsync(2, Manana, 0.01m, 1, null, "Mínimo");
    }

    [Fact]
    public async Task FechaRepetidaDevuelveConflictoYUnaCancelacionLaLibera()
    {
        int primero = await CrearAsync(1, Manana);
        using HttpResponseMessage repetida = await _cliente.PostAsJsonAsync(Ruta,
            new CrearMantenimientoPreventivoRequestDto(1, "Otra", Manana, 2m, null, null));
        await VerificarErrorAsync(repetida, HttpStatusCode.Conflict,
            $"El centro de trabajo CT-A ya tiene un mantenimiento programado el {Manana:yyyy-MM-dd}.");
        await CrearAsync(2, Manana);

        int segundo = await CrearAsync(1, Manana.AddDays(1));
        using HttpResponseMessage editada = await _cliente.PutAsJsonAsync($"{Ruta}/{segundo}",
            new EditarMantenimientoPreventivoRequestDto("Mover", Manana, 2m, null, null));
        await VerificarErrorAsync(editada, HttpStatusCode.Conflict,
            $"El centro de trabajo CT-A ya tiene un mantenimiento programado el {Manana:yyyy-MM-dd}.");

        using HttpResponseMessage cancelada = await _cliente.PostAsJsonAsync($"{Ruta}/{primero}/cancelar",
            new CancelarMantenimientoPreventivoRequestDto("  Sin repuestos  "));
        Assert.Equal(HttpStatusCode.OK, cancelada.StatusCode);
        Assert.Equal("Sin repuestos", (await ObtenerAsync(primero)).MotivoCancelacion);
        await CrearAsync(1, Manana);
    }

    [Fact]
    public async Task EdicionReemplazaLosCamposYConservaSuPropiaFecha()
    {
        int id = await CrearAsync(1, Manana);
        using HttpResponseMessage editada = await _cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new EditarMantenimientoPreventivoRequestDto(" Lubricación ", Manana, 6.25m, 15, " Rosa "));

        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(id, (await editada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        MantenimientoPreventivoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal("Lubricación", detalle.Descripcion);
        Assert.Equal(6.25m, detalle.DuracionHorasPlanificada);
        Assert.Equal(15, detalle.FrecuenciaDias);
        Assert.Equal("Rosa", detalle.Responsable);

        using HttpResponseMessage invalida = await _cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new EditarMantenimientoPreventivoRequestDto("x", Hoy.AddDays(-1), 1m, null, null));
        await VerificarErrorAsync(invalida, HttpStatusCode.BadRequest, "La fecha programada no puede ser anterior a hoy.");
        Assert.Equal(Manana, (await ObtenerAsync(id)).FechaProgramada);
    }

    [Fact]
    public async Task CicloCompletoRegistraFechasRealesYSugiereLaSiguiente()
    {
        int id = await CrearAsync(1, Manana, 4m, 30);
        DateTime antes = DateTime.UtcNow.AddSeconds(-1);
        await VerificarTransicionAsync(id, "iniciar");
        Assert.Equal("EnEjecucion", (await ObtenerAsync(id)).Estado);

        using HttpResponseMessage horasInvalidas = await CompletarAsync(id, 0m, null);
        await VerificarErrorAsync(horasInvalidas, HttpStatusCode.BadRequest, "Las horas reales deben ser mayores que 0 y como máximo 168.");
        using HttpResponseMessage decimales = await CompletarAsync(id, 1.005m, null);
        await VerificarErrorAsync(decimales, HttpStatusCode.BadRequest, "Las horas reales admiten como máximo 2 decimales.");
        using HttpResponseMessage observaciones = await CompletarAsync(id, 1m, new string('o', 501));
        await VerificarErrorAsync(observaciones, HttpStatusCode.BadRequest, "Las observaciones no pueden superar 500 caracteres.");
        Assert.Equal("EnEjecucion", (await ObtenerAsync(id)).Estado);

        using HttpResponseMessage completada = await CompletarAsync(id, 3.75m, "  Sin novedades  ");
        Assert.Equal(HttpStatusCode.OK, completada.StatusCode);
        MantenimientoPreventivoDetalleDto detalle = await ObtenerAsync(id);
        Assert.Equal("Completado", detalle.Estado);
        Assert.Equal(3.75m, detalle.HorasReales);
        Assert.Equal("Sin novedades", detalle.Observaciones);
        Assert.NotNull(detalle.FechaInicioReal);
        Assert.NotNull(detalle.FechaFinReal);
        Assert.True(detalle.FechaInicioReal >= antes);
        Assert.True(detalle.FechaFinReal >= detalle.FechaInicioReal);
        Assert.Equal(DateOnly.FromDateTime(detalle.FechaFinReal.Value).AddDays(30), detalle.FechaSiguienteSugerida);
    }

    [Fact]
    public async Task TransicionesFueraDeEstadoDevuelvenConflicto()
    {
        int id = await CrearAsync(1, Manana);
        await VerificarTransicionAsync(id, "completar", "Solo se pueden completar mantenimientos en ejecución.",
            new CompletarMantenimientoPreventivoRequestDto(1m, null));
        await VerificarTransicionAsync(id, "iniciar");
        await VerificarTransicionAsync(id, "iniciar", "Solo se pueden iniciar mantenimientos programados.");
        await VerificarTransicionAsync(id, "cancelar", "Solo se pueden cancelar mantenimientos programados.",
            new CancelarMantenimientoPreventivoRequestDto("Motivo"));
        using HttpResponseMessage editada = await _cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new EditarMantenimientoPreventivoRequestDto("x", Manana, 1m, null, null));
        await VerificarErrorAsync(editada, HttpStatusCode.Conflict, "Solo se pueden editar mantenimientos programados.");

        int otro = await CrearAsync(2, Manana);
        using HttpResponseMessage sinMotivo = await _cliente.PostAsJsonAsync($"{Ruta}/{otro}/cancelar",
            new CancelarMantenimientoPreventivoRequestDto("   "));
        await VerificarErrorAsync(sinMotivo, HttpStatusCode.BadRequest, "El motivo de cancelación es obligatorio.");
        using HttpResponseMessage motivoLargo = await _cliente.PostAsJsonAsync($"{Ruta}/{otro}/cancelar",
            new CancelarMantenimientoPreventivoRequestDto(new string('m', 251)));
        await VerificarErrorAsync(motivoLargo, HttpStatusCode.BadRequest, "El motivo de cancelación no puede superar 250 caracteres.");
        Assert.Equal("Programado", (await ObtenerAsync(otro)).Estado);
    }

    [Fact]
    public async Task MantenimientoInexistenteDevuelve404EnCadaOperacion()
    {
        const string mensaje = "El mantenimiento preventivo 999 no existe.";
        await VerificarErrorAsync(await _cliente.GetAsync($"{Ruta}/999"), HttpStatusCode.NotFound, mensaje);
        await VerificarErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/999",
            new EditarMantenimientoPreventivoRequestDto("", null, 0m, null, null)), HttpStatusCode.NotFound, mensaje);
        await VerificarErrorAsync(await _cliente.PostAsync($"{Ruta}/999/iniciar", null), HttpStatusCode.NotFound, mensaje);
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/999/completar",
            new CompletarMantenimientoPreventivoRequestDto(0m, null)), HttpStatusCode.NotFound, mensaje);
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync($"{Ruta}/999/cancelar",
            new CancelarMantenimientoPreventivoRequestDto(null)), HttpStatusCode.NotFound, mensaje);
    }

    [Fact]
    public async Task ListadoFiltraOrdenaYPagina()
    {
        int tarde = await CrearAsync(1, Manana.AddDays(10), descripcion: "Revisión eléctrica");
        int pronto = await CrearAsync(2, Manana, descripcion: "Cambio de filtros");
        int medio = await CrearAsync(1, Manana.AddDays(5), descripcion: "Calibración");
        await VerificarTransicionAsync(medio, "iniciar");
        int vencido = _fabrica.AgregarDirecto(Directo(4, Hoy.AddDays(-3), "Programado"));

        PaginaMantenimientosPreventivosDto todos = await ListarAsync("");
        Assert.Equal(4, todos.Total);
        Assert.Equal(new[] { vencido, pronto, medio, tarde }, todos.Items.Select(item => item.Id));
        Assert.True(todos.Items[0].Vencido);
        Assert.False(todos.Items[1].Vencido);
        Assert.Equal("CT-B", todos.Items[1].CentroTrabajoCodigo);
        Assert.Equal("Envasado", todos.Items[1].CentroTrabajoNombre);

        Assert.Equal(new[] { medio, tarde }, (await ListarAsync("?centroTrabajoId=1")).Items.Select(item => item.Id));
        Assert.Equal(new[] { medio }, (await ListarAsync("?estado=enejecucion")).Items.Select(item => item.Id));
        Assert.Equal(new[] { tarde }, (await ListarAsync("?busqueda=ELÉCTRICA")).Items.Select(item => item.Id));
        Assert.Equal(new[] { pronto }, (await ListarAsync($"?busqueda=mp-{pronto:D6}")).Items.Select(item => item.Id));
        Assert.Equal(new[] { pronto, medio },
            (await ListarAsync($"?desde={Manana:yyyy-MM-dd}&hasta={Manana.AddDays(5):yyyy-MM-dd}")).Items.Select(item => item.Id));

        PaginaMantenimientosPreventivosDto segunda = await ListarAsync("?pagina=2&tamanoPagina=3");
        Assert.Equal(4, segunda.Total);
        Assert.Equal(tarde, Assert.Single(segunda.Items).Id);
        PaginaMantenimientosPreventivosDto vacia = await ListarAsync("?pagina=2147483647&tamanoPagina=100");
        Assert.Equal(4, vacia.Total);
        Assert.Empty(vacia.Items);
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?busqueda=LARGA51", "La búsqueda no puede superar 50 caracteres.")]
    [InlineData("?centroTrabajoId=0", "El identificador del centro de trabajo debe ser mayor que 0.")]
    [InlineData("?estado=Pendiente", "El estado del mantenimiento preventivo no es válido.")]
    [InlineData("?desde=2027-02-02&hasta=2027-02-01", "La fecha desde no puede ser posterior a la fecha hasta.")]
    public async Task ListadoInvalidoDevuelveElMensajeLiteral(string consulta, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + consulta.Replace("LARGA51", new string('b', 51)));

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task ResumenAgregaPorEstadoVencidosProximosYHoras()
    {
        await CrearAsync(1, Hoy, 2m);
        await CrearAsync(2, Hoy.AddDays(6), 3m);
        await CrearAsync(1, Hoy.AddDays(7), 4m);
        int enEjecucion = await CrearAsync(2, Manana, 5.5m);
        await VerificarTransicionAsync(enEjecucion, "iniciar");
        int cancelado = await CrearAsync(4, Manana, 9m);
        using HttpResponseMessage cancelacion = await _cliente.PostAsJsonAsync($"{Ruta}/{cancelado}/cancelar",
            new CancelarMantenimientoPreventivoRequestDto("No aplica"));
        Assert.Equal(HttpStatusCode.OK, cancelacion.StatusCode);
        _fabrica.AgregarDirecto(Directo(4, Hoy.AddDays(-1), "Programado"));
        _fabrica.AgregarDirecto(Directo(4, Hoy.AddDays(-20), "Completado"));

        ResumenMantenimientosPreventivosDto? resumen =
            await _cliente.GetFromJsonAsync<ResumenMantenimientosPreventivosDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(7, resumen.TotalMantenimientos);
        Assert.Equal(new[]
        {
            new EstadoMantenimientosPreventivosDto("Programado", 4),
            new EstadoMantenimientosPreventivosDto("EnEjecucion", 1),
            new EstadoMantenimientosPreventivosDto("Completado", 1),
            new EstadoMantenimientosPreventivosDto("Cancelado", 1)
        }, resumen.PorEstado);
        Assert.Equal(1, resumen.Vencidos);
        Assert.Equal(2, resumen.Proximos7Dias);
        Assert.Equal(2m + 3m + 4m + 5.5m + 1m, resumen.HorasPlanificadasPendientes);
    }

    [Fact]
    public async Task EliminarCentroConMantenimientosDevuelveConflicto()
    {
        int id = await CrearAsync(4, Manana);
        using HttpResponseMessage cancelada = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/cancelar",
            new CancelarMantenimientoPreventivoRequestDto("Motivo"));
        Assert.Equal(HttpStatusCode.OK, cancelada.StatusCode);

        using HttpResponseMessage respuesta = await _cliente.DeleteAsync("/api/v1/centros-trabajo/4");

        await VerificarErrorAsync(respuesta, HttpStatusCode.Conflict,
            "No se puede eliminar el centro de trabajo 4 porque tiene mantenimientos preventivos.");
        using HttpResponseMessage libre = await _cliente.DeleteAsync("/api/v1/centros-trabajo/2");
        Assert.Equal(HttpStatusCode.NoContent, libre.StatusCode);
    }

    [Fact]
    public async Task OpenApiContieneLasOchoOperaciones()
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/resumen").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}/iniciar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}/completar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty($"{Ruta}/{{id}}/cancelar").TryGetProperty("post", out _));
    }

    [Fact]
    public async Task SinTokenDevuelve401()
    {
        _cliente.DefaultRequestHeaders.Authorization = null;

        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static MantenimientoPreventivo Directo(int centroTrabajoId, DateOnly fecha, string estado)
    {
        return new MantenimientoPreventivo
        {
            Numero = $"MP-D{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            CentroTrabajoId = centroTrabajoId,
            Descripcion = "Insertado en la prueba",
            FechaProgramada = fecha,
            DuracionHorasPlanificada = 1m,
            Estado = estado
        };
    }

    private async Task<int> CrearAsync(
        int centroTrabajoId, DateOnly fecha, decimal duracion = 2m, int? frecuencia = null,
        string? responsable = null, string descripcion = "Revisión general")
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta,
            new CrearMantenimientoPreventivoRequestDto(centroTrabajoId, descripcion, fecha, duracion, frecuencia, responsable));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<MantenimientoPreventivoDetalleDto> ObtenerAsync(int id)
    {
        MantenimientoPreventivoDetalleDto? detalle = await _cliente.GetFromJsonAsync<MantenimientoPreventivoDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(detalle);
        return detalle;
    }

    private async Task<PaginaMantenimientosPreventivosDto> ListarAsync(string consulta)
    {
        PaginaMantenimientosPreventivosDto? pagina = await _cliente.GetFromJsonAsync<PaginaMantenimientosPreventivosDto>(Ruta + consulta);
        Assert.NotNull(pagina);
        return pagina;
    }

    private Task<HttpResponseMessage> CompletarAsync(int id, decimal horas, string? observaciones)
    {
        return _cliente.PostAsJsonAsync($"{Ruta}/{id}/completar",
            new CompletarMantenimientoPreventivoRequestDto(horas, observaciones));
    }

    private Task<HttpResponseMessage> EnviarJsonAsync(HttpMethod metodo, string ruta, string json)
    {
        HttpRequestMessage solicitud = new HttpRequestMessage(metodo, ruta)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return _cliente.SendAsync(solicitud);
    }

    private async Task VerificarTransicionAsync(int id, string accion, string? mensaje = null, object? cuerpo = null)
    {
        using HttpResponseMessage respuesta = cuerpo == null
            ? await _cliente.PostAsync($"{Ruta}/{id}/{accion}", null)
            : await _cliente.PostAsJsonAsync($"{Ruta}/{id}/{accion}", cuerpo);
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
