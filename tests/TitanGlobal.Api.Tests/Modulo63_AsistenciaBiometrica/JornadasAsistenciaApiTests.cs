using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo63_AsistenciaBiometrica;

[Collection("Modulo04")]
public class JornadasAsistenciaApiTests
{
    private const string RutaBase = "/api/v1/jornadas-asistencia";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public JornadasAsistenciaApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosAsistencia.Restablecer(fabrica);
    }

    // Mensajes 16 a 18 y 6 del cálculo, en el orden de la spec.
    [Fact]
    public async Task CalculoConParametrosInvalidosDevuelveMensajesLiteralesSinCambios()
    {
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        string obligatorias = "Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.";
        await DatosAsistencia.AssertErrorAsync(await CalcularRespuestaAsync(null, null, null), 400, obligatorias);
        await DatosAsistencia.AssertErrorAsync(await CalcularRespuestaAsync(new DateOnly(2026, 1, 5), null, null), 400, obligatorias);
        await DatosAsistencia.AssertErrorAsync(await CalcularRespuestaAsync(null, new DateOnly(2026, 1, 5), null), 400, obligatorias);
        await DatosAsistencia.AssertErrorAsync(await CalcularRespuestaAsync(new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 5), null), 400, obligatorias);
        await DatosAsistencia.AssertErrorAsync(
            await CalcularRespuestaAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), null),
            400,
            "El rango de cálculo no puede superar 31 días.");
        DateOnly manana = CalculadoraJornada.HoyLocal().AddDays(1);
        await DatosAsistencia.AssertErrorAsync(
            await CalcularRespuestaAsync(manana, manana, null),
            400,
            "No se pueden calcular jornadas futuras.");
        await DatosAsistencia.AssertErrorAsync(
            await CalcularRespuestaAsync(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), 9999),
            404,
            "El empleado 9999 no existe.");
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await CalcularRespuestaAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 1)).StatusCode);
    }

    [Fact]
    public async Task CuerpoVacioDevuelveElLiteralDelMiddleware()
    {
        string[] rutas = { RutaBase + "/calculo", RutaBase + "/1/horas-extra/rechazo" };
        foreach (string ruta in rutas)
        {
            HttpResponseMessage respuesta = await _cliente.PostAsync(ruta, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
            await DatosAsistencia.AssertErrorAsync(respuesta, 400, "El cuerpo de la petición (Body) no puede estar vacío.");
        }
    }

    [Fact]
    public async Task CalculoGeneraLasJornadasContraElTurnoDelModulo64()
    {
        int turno = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoDia("DIA"));
        await DatosAsistencia.AsignarTurnoAsync(_fabrica, 1, turno, new DateOnly(2025, 1, 1));
        await SembrarSemanaAsync();

        ResultadoCalculoDto resultado = await CalcularAsync(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11), 1);
        // Lunes a viernes laborables (5 jornadas); sábado y domingo sin marcas no generan jornada.
        Assert.Equal(5, resultado.Calculadas);
        Assert.Equal(0, resultado.Conservadas);
        Assert.Equal(0, resultado.Eliminadas);
        List<JornadaAsistencia> jornadas = await DatosAsistencia.LeerJornadasAsync(_fabrica);
        Assert.Equal(
            new[] { "Asistencia", "Asistencia", "SinMarcacion", "Falta", "Falta" },
            jornadas.OrderBy(jornada => jornada.Fecha).Select(jornada => jornada.Estado).ToArray());
        JornadaAsistencia lunes = jornadas.Single(jornada => jornada.Fecha == new DateOnly(2026, 1, 5));
        Assert.Equal(1, lunes.EmpleadoId);
        Assert.Equal(turno, lunes.TurnoId);
        Assert.Equal(540, lunes.MinutosTrabajados);
        Assert.Equal("NoAplica", lunes.EstadoHorasExtra);
        JornadaAsistencia martes = jornadas.Single(jornada => jornada.Fecha == new DateOnly(2026, 1, 6));
        Assert.Equal(90, martes.MinutosExtra);
        Assert.Equal("Pendiente", martes.EstadoHorasExtra);
        Assert.Null(martes.DecididoPor);
    }

    [Fact]
    public async Task ElRecalculoConservaLasJornadasConHorasExtraDecididasYRegeneraLasDemas()
    {
        int turno = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoDia("DIA"));
        await DatosAsistencia.AsignarTurnoAsync(_fabrica, 1, turno, new DateOnly(2025, 1, 1));
        await SembrarSemanaAsync();
        await CalcularAsync(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), 1);
        JornadaAsistencia pendiente = (await DatosAsistencia.LeerJornadasAsync(_fabrica)).Single(jornada => jornada.Fecha == new DateOnly(2026, 1, 6));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{pendiente.Id}/horas-extra/aprobacion", null)).StatusCode);

        // Cambian los datos: llega la salida del miércoles y una salida tardía del martes ya decidido.
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 7, 22, 0)));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 7, 23, 30)));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 6, 23, 30)));
        JornadaAsistencia antesDeRecalcular = (await DatosAsistencia.LeerJornadasAsync(_fabrica)).Single(jornada => jornada.Id == pendiente.Id);

        ResultadoCalculoDto resultado = await CalcularAsync(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), 1);
        Assert.Equal(4, resultado.Calculadas);
        Assert.Equal(1, resultado.Conservadas);
        Assert.Equal(0, resultado.Eliminadas);
        List<JornadaAsistencia> jornadas = await DatosAsistencia.LeerJornadasAsync(_fabrica);
        Assert.Equal(5, jornadas.Count);
        JornadaAsistencia decidida = jornadas.Single(jornada => jornada.Id == pendiente.Id);
        Assert.Equal(antesDeRecalcular.MinutosExtra, decidida.MinutosExtra);
        Assert.Equal(antesDeRecalcular.Salida, decidida.Salida);
        Assert.Equal("Aprobada", decidida.EstadoHorasExtra);
        Assert.Equal(TokenPruebas.NombreUsuario, decidida.DecididoPor);
        JornadaAsistencia miercoles = jornadas.Single(jornada => jornada.Fecha == new DateOnly(2026, 1, 7));
        Assert.Equal("Asistencia", miercoles.Estado);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 7, 23, 30), miercoles.Salida);
        Assert.Equal(630, miercoles.MinutosTrabajados);
        Assert.Equal(90, miercoles.MinutosExtra);
    }

    [Fact]
    public async Task ElRecalculoEliminaLaJornadaPendienteCuandoYaNoHayMarcaciones()
    {
        int turno = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoDia("DIA"));
        await DatosAsistencia.AsignarTurnoAsync(_fabrica, 1, turno, new DateOnly(2025, 1, 1));
        int entrada = await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Entrada", DatosAsistencia.Utc(2026, 1, 10, 14, 0)));
        int salida = await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 10, 17, 0)));
        ResultadoCalculoDto primero = await CalcularAsync(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11), 1);
        Assert.Equal(1, primero.Calculadas);
        JornadaAsistencia sabado = Assert.Single(await DatosAsistencia.LeerJornadasAsync(_fabrica));
        Assert.Equal("DescansoTrabajado", sabado.Estado);
        Assert.Equal(180, sabado.MinutosExtra);
        Assert.Equal("Pendiente", sabado.EstadoHorasExtra);

        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"/api/v1/marcaciones/{entrada}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"/api/v1/marcaciones/{salida}")).StatusCode);
        ResultadoCalculoDto segundo = await CalcularAsync(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11), 1);
        Assert.Equal(0, segundo.Calculadas);
        Assert.Equal(1, segundo.Eliminadas);
        Assert.Empty(await DatosAsistencia.LeerJornadasAsync(_fabrica));
    }

    [Fact]
    public async Task CalculoSinEmpleadoIncluyeATodosYUnTurnoNocturnoCaeEnLaFechaDeLaEntrada()
    {
        int dia = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoDia("DIA"));
        int noche = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoNoche("NOCHE"));
        await DatosAsistencia.AsignarTurnoAsync(_fabrica, 1, dia, new DateOnly(2025, 1, 1));
        await DatosAsistencia.AsignarTurnoAsync(_fabrica, 2, noche, new DateOnly(2025, 1, 1));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0)));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(2, "Entrada", DatosAsistencia.Utc(2026, 1, 6, 3, 3)));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(2, "Salida", DatosAsistencia.Utc(2026, 1, 6, 11, 0)));

        ResultadoCalculoDto resultado = await CalcularAsync(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), null);
        Assert.Equal(2, resultado.Calculadas);
        List<JornadaAsistencia> jornadas = await DatosAsistencia.LeerJornadasAsync(_fabrica);
        Assert.Equal(new[] { 1, 2 }, jornadas.Select(jornada => jornada.EmpleadoId).OrderBy(id => id).ToArray());
        JornadaAsistencia nocturna = jornadas.Single(jornada => jornada.EmpleadoId == 2);
        Assert.Equal(new DateOnly(2026, 1, 5), nocturna.Fecha);
        Assert.Equal(477, nocturna.MinutosTrabajados);
        Assert.Equal("Asistencia", nocturna.Estado);
    }

    [Fact]
    public async Task AprobarHorasExtraGuardaElUsuarioDelTokenYLaFechaDeDecision()
    {
        int id = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 5), "Asistencia", 0, 90, "Pendiente");
        DateTime antes = DateTime.UtcNow.AddMinutes(-1);
        HttpResponseMessage respuesta = await _cliente.PostAsync($"{RutaBase}/{id}/horas-extra/aprobacion", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JornadaAsistenciaIdDto? resultado = await respuesta.Content.ReadFromJsonAsync<JornadaAsistenciaIdDto>();
        Assert.NotNull(resultado);
        Assert.Equal(id, resultado.Id);
        JornadaAsistencia guardada = (await DatosAsistencia.LeerJornadasAsync(_fabrica)).Single();
        Assert.Equal("Aprobada", guardada.EstadoHorasExtra);
        Assert.Equal(TokenPruebas.NombreUsuario, guardada.DecididoPor);
        Assert.NotNull(guardada.FechaDecision);
        Assert.True(guardada.FechaDecision >= antes);
        Assert.Null(guardada.MotivoRechazo);
        RegistroAuditoria cambio = Assert.Single(
            await DatosAsistencia.LeerAuditoriaAsync(_fabrica, "JornadaAsistencia", id),
            registro => registro.Accion == "Modificacion");
        using JsonDocument cambios = JsonDocument.Parse(cambio.Cambios);
        List<string> campos = cambios.RootElement.EnumerateArray().Select(item => item.GetProperty("campo").GetString() ?? string.Empty).ToList();
        Assert.Contains("EstadoHorasExtra", campos);
        Assert.Contains("DecididoPor", campos);
    }

    [Fact]
    public async Task RechazarHorasExtraGuardaElMotivoYElUsuarioDelToken()
    {
        int id = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 5), "Asistencia", 0, 90, "Pendiente");
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{id}/horas-extra/rechazo",
            new RechazarHorasExtraRequestDto { Motivo = "  No estaban autorizadas  " });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JornadaAsistencia guardada = (await DatosAsistencia.LeerJornadasAsync(_fabrica)).Single();
        Assert.Equal("Rechazada", guardada.EstadoHorasExtra);
        Assert.Equal("No estaban autorizadas", guardada.MotivoRechazo);
        Assert.Equal(TokenPruebas.NombreUsuario, guardada.DecididoPor);
        Assert.NotNull(guardada.FechaDecision);
    }

    [Fact]
    public async Task AprobacionYRechazoConDatosInvalidosDevuelvenMensajesLiteralesSinCambios()
    {
        int pendiente = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 5), "Asistencia", 0, 90, "Pendiente");
        int noAplica = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 6), "Asistencia", 0, 0, "NoAplica");
        int aprobada = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 7), "Asistencia", 0, 30, "Aprobada");
        int rechazada = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 8), "Asistencia", 0, 30, "Rechazada");
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        RechazarHorasExtraRequestDto motivo = new RechazarHorasExtraRequestDto { Motivo = "Sin autorización" };

        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsync(RutaBase + "/999/horas-extra/aprobacion", null), 404, "La jornada 999 no existe.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase + "/999/horas-extra/rechazo", motivo), 404, "La jornada 999 no existe.");
        foreach (int id in new[] { noAplica, aprobada, rechazada })
        {
            string mensaje = $"La jornada {id} no tiene horas extra pendientes.";
            await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/horas-extra/aprobacion", null), 409, mensaje);
            await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/{id}/horas-extra/rechazo", motivo), 409, mensaje);
        }

        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{pendiente}/horas-extra/rechazo", new RechazarHorasExtraRequestDto()),
            400,
            "El motivo del rechazo es obligatorio.");
        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{pendiente}/horas-extra/rechazo", new RechazarHorasExtraRequestDto { Motivo = "   " }),
            400,
            "El motivo del rechazo es obligatorio.");
        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{pendiente}/horas-extra/rechazo", new RechazarHorasExtraRequestDto { Motivo = new string('x', 301) }),
            400,
            "El motivo del rechazo no puede superar 300 caracteres.");
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));

        // Una decisión no se repite: aprobada una vez, la segunda devuelve 409.
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{pendiente}/horas-extra/aprobacion", null)).StatusCode);
        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsync($"{RutaBase}/{pendiente}/horas-extra/aprobacion", null),
            409,
            $"La jornada {pendiente} no tiene horas extra pendientes.");
    }

    [Fact]
    public async Task ListadoAplicaFiltrosOrdenYPaginacion()
    {
        int turno = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoDia("DIA"));
        await SembrarJornadasAsync(turno);

        PaginaJornadasAsistenciaDto todas = await LeerPaginaAsync(string.Empty);
        Assert.Equal(7, todas.Total);
        Assert.Equal(new DateOnly(2026, 1, 20), todas.Items[0].Fecha);
        Assert.Equal(new[] { 1, 2 }, todas.Items.Where(item => item.Fecha == new DateOnly(2026, 1, 5)).Select(item => item.EmpleadoId).ToArray());
        JornadaAsistenciaDto ejemplo = todas.Items.Single(item => item.EmpleadoId == 1 && item.Fecha == new DateOnly(2026, 1, 5));
        Assert.Equal("EMP-000001", ejemplo.EmpleadoLegajo);
        Assert.Equal("Torres Vega, Ana", ejemplo.EmpleadoNombre);
        Assert.Equal("DIA", ejemplo.TurnoCodigo);
        Assert.Equal(new TimeOnly(8, 0), ejemplo.HoraEntradaProgramada);
        Assert.Equal(10, ejemplo.ToleranciaMinutos);
        Assert.Null(todas.Items.Single(item => item.EmpleadoId == 2 && item.Fecha == new DateOnly(2026, 1, 5)).TurnoCodigo);

        Assert.Equal(3, (await LeerPaginaAsync("?empleadoId=1")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?estado=Falta")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?estado=Tardanza&estadoHorasExtra=Pendiente")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?estadoHorasExtra=Aprobada")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?estadoHorasExtra=Pendiente")).Total);
        Assert.Equal(6, (await LeerPaginaAsync("?desde=2026-01-05&hasta=2026-01-07")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?desde=2026-01-20")).Total);
        PaginaJornadasAsistenciaDto segunda = await LeerPaginaAsync("?pagina=2&tamanoPagina=5");
        Assert.Equal(7, segunda.Total);
        Assert.Equal(2, segunda.Items.Count);
    }

    [Fact]
    public async Task ListadoConParametrosInvalidosDevuelveMensajeLiteral()
    {
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?estado=Otro"), 400, "El filtro estado no es válido.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?estadoHorasExtra=Otro"), 400, "El filtro estadoHorasExtra no es válido.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?desde=2026-01-06&hasta=2026-01-05"), 400, "El filtro hasta no es válido.");
    }

    [Fact]
    public async Task ResumenCuentaLasJornadasDelRangoYSumaLosMinutos()
    {
        int turno = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoDia("DIA"));
        await SembrarJornadasAsync(turno);
        HttpResponseMessage respuesta = await _cliente.GetAsync(RutaBase + "/resumen?desde=2026-01-05&hasta=2026-01-07");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ResumenAsistenciaDto? resumen = await respuesta.Content.ReadFromJsonAsync<ResumenAsistenciaDto>();
        Assert.NotNull(resumen);
        Assert.Equal(6, resumen.Jornadas);
        Assert.Equal(2, resumen.Asistencias);
        Assert.Equal(1, resumen.Tardanzas);
        Assert.Equal(1, resumen.SinMarcacion);
        Assert.Equal(1, resumen.Faltas);
        Assert.Equal(15, resumen.MinutosTardanza);
        Assert.Equal(60, resumen.MinutosExtraPendientes);
        Assert.Equal(100, resumen.MinutosExtraAprobados);

        HttpResponseMessage vacio = await _cliente.GetAsync(RutaBase + "/resumen?desde=2025-01-01&hasta=2025-01-02");
        ResumenAsistenciaDto? sinDatos = await vacio.Content.ReadFromJsonAsync<ResumenAsistenciaDto>();
        Assert.NotNull(sinDatos);
        Assert.Equal(0, sinDatos.Jornadas);
        Assert.Equal(0, sinDatos.MinutosExtraAprobados);
    }

    [Fact]
    public async Task ResumenSinFechasOConRangoInvertidoDevuelve400()
    {
        string mensaje = "Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.";
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/resumen"), 400, mensaje);
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/resumen?desde=2026-01-05"), 400, mensaje);
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/resumen?desde=2026-01-06&hasta=2026-01-05"), 400, mensaje);
    }

    [Fact]
    public async Task SinTokenTodasLasOperacionesDevuelven401()
    {
        HttpClient anonimo = DatosAsistencia.ClienteAnonimo(_fabrica);
        await DatosAsistencia.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, DatosAsistencia.MensajeSinSesion);
        await DatosAsistencia.AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/resumen?desde=2026-01-05&hasta=2026-01-06"), 401, DatosAsistencia.MensajeSinSesion);
        await DatosAsistencia.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/calculo", new CalcularJornadasRequestDto()), 401, DatosAsistencia.MensajeSinSesion);
        await DatosAsistencia.AssertErrorAsync(await anonimo.PostAsync(RutaBase + "/1/horas-extra/aprobacion", null), 401, DatosAsistencia.MensajeSinSesion);
        await DatosAsistencia.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/1/horas-extra/rechazo", new RechazarHorasExtraRequestDto()), 401, DatosAsistencia.MensajeSinSesion);
    }

    [Fact]
    public async Task TokenSinRolDeEscrituraDevuelve403EnLasTresEscriturasSinCambiarFilas()
    {
        int id = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 5), "Asistencia", 0, 90, "Pendiente");
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        HttpClient consulta = DatosAsistencia.ClienteConRoles(_fabrica, "CONSULTA");
        CalcularJornadasRequestDto calculo = new CalcularJornadasRequestDto { Desde = new DateOnly(2026, 1, 5), Hasta = new DateOnly(2026, 1, 5) };
        await DatosAsistencia.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase + "/calculo", calculo), 403, DatosAsistencia.MensajeSinPermiso);
        await DatosAsistencia.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/horas-extra/aprobacion", null), 403, DatosAsistencia.MensajeSinPermiso);
        await DatosAsistencia.AssertErrorAsync(
            await consulta.PostAsJsonAsync($"{RutaBase}/{id}/horas-extra/rechazo", new RechazarHorasExtraRequestDto { Motivo = "Motivo" }),
            403,
            DatosAsistencia.MensajeSinPermiso);
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenEscribirConEstadoOk(string rol)
    {
        int aprobar = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 5), "Asistencia", 0, 90, "Pendiente");
        int rechazar = await InsertarJornadaAsync(1, new DateOnly(2026, 1, 6), "Asistencia", 0, 90, "Pendiente");
        HttpClient cliente = DatosAsistencia.ClienteConRoles(_fabrica, rol);
        CalcularJornadasRequestDto calculo = new CalcularJornadasRequestDto { Desde = new DateOnly(2026, 1, 12), Hasta = new DateOnly(2026, 1, 12) };
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(RutaBase + "/calculo", calculo)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{aprobar}/horas-extra/aprobacion", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await cliente.PostAsJsonAsync($"{RutaBase}/{rechazar}/horas-extra/rechazo", new RechazarHorasExtraRequestDto { Motivo = "Motivo" })).StatusCode);
    }

    [Theory]
    [InlineData("CONSULTA")]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task LecturaConCualquierTokenAutenticadoDevuelveOk(string rol)
    {
        HttpClient cliente = DatosAsistencia.ClienteConRoles(_fabrica, rol);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase + "/resumen?desde=2026-01-05&hasta=2026-01-06")).StatusCode);
    }

    [Fact]
    public async Task ElCalculoGeneraRegistrosDeAuditoriaDeAlta()
    {
        int turno = await DatosAsistencia.InsertarTurnoAsync(_fabrica, DatosAsistencia.CrearTurnoDia("DIA"));
        await DatosAsistencia.AsignarTurnoAsync(_fabrica, 1, turno, new DateOnly(2025, 1, 1));
        await SembrarSemanaAsync();
        await CalcularAsync(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), 1);
        JornadaAsistencia jornada = Assert.Single(await DatosAsistencia.LeerJornadasAsync(_fabrica));
        Assert.Single(await DatosAsistencia.LeerAuditoriaAsync(_fabrica, "JornadaAsistencia", jornada.Id), registro => registro.Accion == "Alta");
    }

    // Lunes a miércoles de la semana del 2026-01-05 para el empleado 1 (turno DIA 08:00-17:00).
    private async Task SembrarSemanaAsync()
    {
        DateTime[] instantes =
        {
            DatosAsistencia.Utc(2026, 1, 5, 13, 0),
            DatosAsistencia.Utc(2026, 1, 5, 22, 0),
            DatosAsistencia.Utc(2026, 1, 6, 12, 30),
            DatosAsistencia.Utc(2026, 1, 6, 23, 0),
            DatosAsistencia.Utc(2026, 1, 7, 13, 0)
        };
        string[] tipos = { "Entrada", "Salida", "Entrada", "Salida", "Entrada" };
        for (int indice = 0; indice < instantes.Length; indice++)
        {
            await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, tipos[indice], instantes[indice]));
        }
    }

    // Siete jornadas: seis entre el 5 y el 7 de enero y una el 20.
    private async Task SembrarJornadasAsync(int turnoId)
    {
        JornadaAsistencia primera = Jornada(1, new DateOnly(2026, 1, 5), "Asistencia", 0, 0, "NoAplica");
        primera.TurnoId = turnoId;
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, primera);
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, Jornada(1, new DateOnly(2026, 1, 6), "Tardanza", 15, 60, "Pendiente"));
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, Jornada(1, new DateOnly(2026, 1, 7), "SinMarcacion", 0, 0, "NoAplica"));
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, Jornada(2, new DateOnly(2026, 1, 5), "Falta", 0, 0, "NoAplica"));
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, Jornada(2, new DateOnly(2026, 1, 6), "Asistencia", 0, 100, "Aprobada"));
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, Jornada(2, new DateOnly(2026, 1, 7), "DescansoTrabajado", 0, 50, "Rechazada"));
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, Jornada(2, new DateOnly(2026, 1, 20), "Asistencia", 0, 0, "NoAplica"));
    }

    private async Task<int> InsertarJornadaAsync(int empleadoId, DateOnly fecha, string estado, int tardanza, int extra, string estadoHorasExtra)
    {
        return await DatosAsistencia.InsertarJornadaAsync(_fabrica, Jornada(empleadoId, fecha, estado, tardanza, extra, estadoHorasExtra));
    }

    private static JornadaAsistencia Jornada(int empleadoId, DateOnly fecha, string estado, int tardanza, int extra, string estadoHorasExtra)
    {
        JornadaAsistencia jornada = DatosAsistencia.CrearJornadaDia(empleadoId, fecha, extra, estadoHorasExtra);
        jornada.Estado = estado;
        jornada.MinutosTardanza = tardanza;
        if (estadoHorasExtra == "Aprobada" || estadoHorasExtra == "Rechazada")
        {
            jornada.DecididoPor = "admin";
            jornada.FechaDecision = DatosAsistencia.Utc(2026, 1, 8, 15, 0);
        }

        return jornada;
    }

    private async Task<HttpResponseMessage> CalcularRespuestaAsync(DateOnly? desde, DateOnly? hasta, int? empleadoId)
    {
        return await _cliente.PostAsJsonAsync(
            RutaBase + "/calculo",
            new CalcularJornadasRequestDto { Desde = desde, Hasta = hasta, EmpleadoId = empleadoId });
    }

    private async Task<ResultadoCalculoDto> CalcularAsync(DateOnly desde, DateOnly hasta, int? empleadoId)
    {
        HttpResponseMessage respuesta = await CalcularRespuestaAsync(desde, hasta, empleadoId);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ResultadoCalculoDto? resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoCalculoDto>();
        Assert.NotNull(resultado);
        return resultado;
    }

    private async Task<PaginaJornadasAsistenciaDto> LeerPaginaAsync(string consulta)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync(RutaBase + consulta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PaginaJornadasAsistenciaDto? pagina = await respuesta.Content.ReadFromJsonAsync<PaginaJornadasAsistenciaDto>();
        Assert.NotNull(pagina);
        return pagina;
    }
}
