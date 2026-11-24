using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo64_TurnosHorarios;

[Collection("Modulo04")]
public class AsignacionesTurnoApiTests
{
    private const string RutaBase = "/api/v1/asignaciones-turno";
    private const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";
    private const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public AsignacionesTurnoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    // Mensajes 25 a 28 del alta, uno por caso.
    [Theory]
    [InlineData("empleado-nulo", "El empleado es obligatorio.")]
    [InlineData("turno-nulo", "El turno es obligatorio.")]
    [InlineData("inicio-nulo", "La fecha de inicio es obligatoria.")]
    [InlineData("fin-anterior", "La fecha de fin no puede ser anterior a la fecha de inicio.")]
    public async Task AltaConCamposInvalidosDevuelveMensajeLiteralSinCrearFilas(string caso, string mensaje)
    {
        int turnoId = await InsertarTurnoAsync(CrearTurno("DIA", true));
        GuardarAsignacionTurnoRequestDto solicitud = CrearSolicitud(1, turnoId, new DateOnly(2026, 3, 1), null);
        switch (caso)
        {
            case "empleado-nulo":
                solicitud.EmpleadoId = null;
                break;
            case "turno-nulo":
                solicitud.TurnoId = null;
                break;
            case "inicio-nulo":
                solicitud.FechaInicio = null;
                break;
            default:
                solicitud.FechaFin = new DateOnly(2026, 2, 28);
                break;
        }

        string[] antes = await LeerFirmasAsync();
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, mensaje);
        Assert.Equal(antes, await LeerFirmasAsync());
        Assert.DoesNotContain(antes, firma => firma.StartsWith("A|", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AltaConVariosErroresDevuelveElPrimeroEnElOrdenDeLaSpec()
    {
        GuardarAsignacionTurnoRequestDto solicitud = new GuardarAsignacionTurnoRequestDto();
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El empleado es obligatorio.");
        solicitud.EmpleadoId = 9999;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El turno es obligatorio.");
        solicitud.TurnoId = 9999;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "La fecha de inicio es obligatoria.");
        solicitud.FechaInicio = new DateOnly(2026, 3, 1);
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 404, "El empleado 9999 no existe.");
        solicitud.EmpleadoId = 1;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 404, "El turno 9999 no existe.");
    }

    [Fact]
    public async Task CuerpoVacioDevuelveElLiteralDelMiddleware()
    {
        string[] rutas = { RutaBase, RutaBase + "/1/finalizacion" };
        foreach (string ruta in rutas)
        {
            HttpResponseMessage respuesta = await _cliente.PostAsync(ruta, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
            await AssertErrorAsync(respuesta, 400, "El cuerpo de la petición (Body) no puede estar vacío.");
        }
    }

    [Fact]
    public async Task AltaConEmpleadoCesadoDevuelve409()
    {
        int turnoId = await InsertarTurnoAsync(CrearTurno("DIA", true));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(4, turnoId, new DateOnly(2026, 11, 1), null));
        await AssertErrorAsync(respuesta, 409, "El empleado 4 está cesado y no admite turnos.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AltaAnteriorAlIngresoDelEmpleadoDevuelve400()
    {
        int turnoId = await InsertarTurnoAsync(CrearTurno("DIA", true));
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, turnoId, new DateOnly(2021, 2, 28), null));
        await AssertErrorAsync(respuesta, 400, "La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, turnoId, new DateOnly(2021, 3, 1), null))).StatusCode);
    }

    [Fact]
    public async Task AltaConTurnoInactivoDevuelve409()
    {
        int turnoId = await InsertarTurnoAsync(CrearTurno("SAB", false));
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, turnoId, new DateOnly(2026, 3, 1), null));
        await AssertErrorAsync(respuesta, 409, "El turno SAB está inactivo y no admite asignaciones.");
        Assert.Empty(await LeerAsignacionesAsync());
    }

    [Fact]
    public async Task AltaValidaGuardaLaAsignacion()
    {
        int turnoId = await InsertarTurnoAsync(CrearTurno("DIA", true));
        AsignacionTurnoIdDto creada = await CrearAsync(CrearSolicitud(1, turnoId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));
        AsignacionTurno guardada = await ObtenerAsignacionAsync(creada.Id);
        Assert.Equal(1, guardada.EmpleadoId);
        Assert.Equal(turnoId, guardada.TurnoId);
        Assert.Equal(new DateOnly(2026, 3, 1), guardada.FechaInicio);
        Assert.Equal(new DateOnly(2026, 3, 31), guardada.FechaFin);
    }

    [Fact]
    public async Task AltaDesdeDCierraLaAsignacionAbiertaAnteriorEnDMenosUno()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        int noche = await InsertarTurnoAsync(CrearTurno("NOCHE", true));
        AsignacionTurnoIdDto primera = await CrearAsync(CrearSolicitud(1, dia, new DateOnly(2026, 1, 1), null));
        AsignacionTurnoIdDto segunda = await CrearAsync(CrearSolicitud(1, noche, new DateOnly(2026, 3, 1), null));
        AsignacionTurno cerrada = await ObtenerAsignacionAsync(primera.Id);
        Assert.Equal(new DateOnly(2026, 2, 28), cerrada.FechaFin);
        AsignacionTurno abierta = await ObtenerAsignacionAsync(segunda.Id);
        Assert.Null(abierta.FechaFin);
    }

    [Fact]
    public async Task AltaQueSeCruzaConUnaAsignacionCerradaDevuelve409SinCambios()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        await InsertarAsignacionAsync(1, dia, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));
        string[] antes = await LeerFirmasAsync();
        string mensaje = "El empleado 1 ya tiene un turno asignado que se cruza con esas fechas.";
        // Empieza dentro de la cerrada.
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, dia, new DateOnly(2026, 2, 15), null)), 409, mensaje);
        // Empieza antes y sin fin: abarcaría la cerrada.
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, dia, new DateOnly(2026, 1, 15), null)), 409, mensaje);
        // Termina dentro de la cerrada.
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, dia, new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 1))), 409, mensaje);
        // Misma fecha de inicio (índice único como guarda mínima).
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, dia, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 3))), 409, mensaje);
        Assert.Equal(antes, await LeerFirmasAsync());

        // Justo antes y justo después no se cruzan.
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, dia, new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 31)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, dia, new DateOnly(2026, 3, 1), null))).StatusCode);
    }

    [Fact]
    public async Task FinalizacionConDatosInvalidosDevuelveMensajesLiterales()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        await InsertarAsignacionAsync(1, dia, new DateOnly(2026, 3, 1), null);
        int cerrada = await InsertarAsignacionConIdAsync(2, dia, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10));
        int abierta = (await LeerAsignacionesAsync()).Single(asignacion => asignacion.EmpleadoId == 1).Id;
        string[] antes = await LeerFirmasAsync();

        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync(RutaBase + "/999/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = new DateOnly(2026, 3, 5) }),
            404,
            "La asignación de turno 999 no existe.");
        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{abierta}/finalizacion", new FinalizarAsignacionTurnoRequestDto()),
            400,
            "La fecha de fin es obligatoria.");
        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{abierta}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = new DateOnly(2026, 2, 28) }),
            400,
            "La fecha de fin no puede ser anterior a la fecha de inicio.");
        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{cerrada}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = new DateOnly(2026, 3, 10) }),
            409,
            "La asignación ya termina en esa fecha o antes.");
        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{cerrada}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = new DateOnly(2026, 3, 20) }),
            409,
            "La asignación ya termina en esa fecha o antes.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task FinalizacionFijaOAdelantaLaFechaDeFin()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        AsignacionTurnoIdDto creada = await CrearAsync(CrearSolicitud(1, dia, new DateOnly(2026, 3, 1), null));
        HttpResponseMessage fijar = await _cliente.PostAsJsonAsync($"{RutaBase}/{creada.Id}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = new DateOnly(2026, 3, 31) });
        Assert.Equal(HttpStatusCode.OK, fijar.StatusCode);
        Assert.Equal(new DateOnly(2026, 3, 31), (await ObtenerAsignacionAsync(creada.Id)).FechaFin);
        HttpResponseMessage adelantar = await _cliente.PostAsJsonAsync($"{RutaBase}/{creada.Id}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = new DateOnly(2026, 3, 15) });
        Assert.Equal(HttpStatusCode.OK, adelantar.StatusCode);
        Assert.Equal(new DateOnly(2026, 3, 15), (await ObtenerAsignacionAsync(creada.Id)).FechaFin);
        HttpResponseMessage mismoDia = await _cliente.PostAsJsonAsync($"{RutaBase}/{creada.Id}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = new DateOnly(2026, 3, 1) });
        Assert.Equal(HttpStatusCode.OK, mismoDia.StatusCode);
    }

    [Fact]
    public async Task EliminarUnaAsignacionFuturaDevuelve204YUnaYaEmpezadaDevuelve409()
    {
        DateOnly hoy = Hoy();
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        AsignacionTurnoIdDto vigente = await CrearAsync(CrearSolicitud(1, dia, hoy, null));
        AsignacionTurnoIdDto futura = await CrearAsync(CrearSolicitud(2, dia, hoy.AddDays(1), null));
        await AssertErrorAsync(await _cliente.DeleteAsync($"{RutaBase}/{vigente.Id}"), 409, "Solo se puede eliminar una asignación que todavía no empezó.");
        await AssertErrorAsync(await _cliente.DeleteAsync(RutaBase + "/999"), 404, "La asignación de turno 999 no existe.");
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{RutaBase}/{futura.Id}")).StatusCode);
        Assert.Single(await LeerAsignacionesAsync());
        Assert.Equal(vigente.Id, (await LeerAsignacionesAsync()).Single().Id);
    }

    [Fact]
    public async Task ListadoCalculaElEstadoYAplicaFiltrosYOrden()
    {
        DateOnly hoy = Hoy();
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        int noche = await InsertarTurnoAsync(CrearTurno("NOCHE", true));
        await InsertarAsignacionAsync(1, dia, hoy.AddDays(-60), hoy.AddDays(-1));
        await InsertarAsignacionAsync(1, noche, hoy.AddDays(-30), null);
        await InsertarAsignacionAsync(2, dia, hoy.AddDays(10), null);
        await InsertarAsignacionAsync(3, dia, hoy, hoy);

        PaginaAsignacionesTurnoDto todas = await LeerPaginaAsync(string.Empty);
        Assert.Equal(4, todas.Total);
        Assert.Equal(new[] { "Futura", "Vigente", "Vigente", "Finalizada" }, todas.Items.Select(item => item.Estado).ToArray());
        Assert.Equal(new[] { 2, 3, 1, 1 }, todas.Items.Select(item => item.EmpleadoId).ToArray());
        AsignacionTurnoListadoDto primera = todas.Items[0];
        Assert.Equal("EMP-000002", primera.EmpleadoLegajo);
        Assert.Equal("Rojas Mendoza, Luis", primera.EmpleadoNombre);
        Assert.Equal("DIA", primera.TurnoCodigo);
        Assert.Equal("Turno DIA", primera.TurnoNombre);
        Assert.Equal(hoy.AddDays(10), primera.FechaInicio);
        Assert.Null(primera.FechaFin);

        Assert.Equal(2, (await LeerPaginaAsync("?estado=Vigente")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?estado=Futura")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?estado=Finalizada")).Total);
        Assert.Equal(2, (await LeerPaginaAsync("?empleadoId=1")).Total);
        Assert.Equal(1, (await LeerPaginaAsync($"?turnoId={noche}")).Total);
        PaginaAsignacionesTurnoDto segunda = await LeerPaginaAsync("?pagina=2&tamanoPagina=3");
        Assert.Equal(4, segunda.Total);
        Assert.Single(segunda.Items);
    }

    [Fact]
    public async Task ListadoConParametrosInvalidosDevuelveMensajeLiteral()
    {
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?estado=Otro"), 400, "El estado debe ser Vigente, Futura o Finalizada.");
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task ProgramacionDeTurnoFijoMarcaElFinDeSemanaComoDescanso()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        await InsertarAsignacionAsync(1, dia, new DateOnly(2026, 1, 1), null);
        List<DiaProgramadoDto> programacion = await LeerProgramacionAsync(1, "2026-01-05", "2026-01-11");
        Assert.Equal(7, programacion.Count);
        Assert.All(programacion.Take(5), item =>
        {
            Assert.True(item.Laborable);
            Assert.Null(item.Observacion);
            Assert.Equal(dia, item.TurnoId);
            Assert.Equal("DIA", item.TurnoCodigo);
            Assert.Equal(new TimeOnly(8, 0), item.HoraEntrada);
            Assert.Equal(new TimeOnly(17, 0), item.HoraSalida);
            Assert.Equal(10, item.ToleranciaMinutos);
            Assert.False(item.CruzaMedianoche);
        });
        Assert.All(programacion.Skip(5), item =>
        {
            Assert.False(item.Laborable);
            Assert.Equal("Descanso", item.Observacion);
        });
        Assert.Equal(new DateOnly(2026, 1, 5), programacion[0].Fecha);
        Assert.Equal(new DateOnly(2026, 1, 11), programacion[6].Fecha);
    }

    [Fact]
    public async Task ProgramacionDeTurnoNocturnoIndicaCruceDeMedianoche()
    {
        Turno noche = CrearTurno("NOCHE", true);
        noche.HoraEntrada = new TimeOnly(22, 0);
        noche.HoraSalida = new TimeOnly(6, 0);
        noche.CruzaMedianoche = true;
        noche.ToleranciaMinutos = 5;
        noche.DiasLaborables = "1,2,3,4,5,6";
        int id = await InsertarTurnoAsync(noche);
        await InsertarAsignacionAsync(2, id, new DateOnly(2026, 1, 1), null);
        List<DiaProgramadoDto> programacion = await LeerProgramacionAsync(2, "2026-01-10", "2026-01-11");
        Assert.True(programacion[0].Laborable);
        Assert.True(programacion[0].CruzaMedianoche);
        Assert.Equal(new TimeOnly(22, 0), programacion[0].HoraEntrada);
        Assert.Equal(new TimeOnly(6, 0), programacion[0].HoraSalida);
        Assert.Equal(5, programacion[0].ToleranciaMinutos);
        Assert.False(programacion[1].Laborable);
        Assert.Equal("Descanso", programacion[1].Observacion);
    }

    [Fact]
    public async Task ProgramacionDeTurnoRotativoSigueElCicloExactoYDescansaAntesDelInicioDelCiclo()
    {
        int rotativo = await InsertarTurnoAsync(CrearTurnoRotativo("ROT4X2"));
        await InsertarAsignacionAsync(1, rotativo, new DateOnly(2026, 1, 1), null);
        List<DiaProgramadoDto> programacion = await LeerProgramacionAsync(1, "2026-01-01", "2026-01-16");
        bool[] esperado =
        {
            false, false, false, false,
            true, true, true, true, false, false,
            true, true, true, true, false, false
        };
        Assert.Equal(esperado, programacion.Select(item => item.Laborable).ToArray());
        Assert.All(programacion.Where(item => !item.Laborable), item => Assert.Equal("Descanso", item.Observacion));
        Assert.Equal(new TimeOnly(7, 0), programacion[4].HoraEntrada);
        Assert.Equal(15, programacion[4].ToleranciaMinutos);
    }

    [Fact]
    public async Task ProgramacionReflejaElCambioDeTurnoAMitadDelRango()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        int rotativo = await InsertarTurnoAsync(CrearTurnoRotativo("ROT4X2"));
        AsignacionTurnoIdDto primera = await CrearAsync(CrearSolicitud(1, dia, new DateOnly(2026, 1, 1), null));
        await CrearAsync(CrearSolicitud(1, rotativo, new DateOnly(2026, 1, 8), null));
        Assert.Equal(new DateOnly(2026, 1, 7), (await ObtenerAsignacionAsync(primera.Id)).FechaFin);
        List<DiaProgramadoDto> programacion = await LeerProgramacionAsync(1, "2026-01-06", "2026-01-09");
        Assert.Equal(new[] { dia, dia, rotativo, rotativo }, programacion.Select(item => item.TurnoId ?? 0).ToArray());
        Assert.Equal(new[] { "DIA", "DIA", "ROT4X2", "ROT4X2" }, programacion.Select(item => item.TurnoCodigo ?? string.Empty).ToArray());
        // 2026-01-08 es el cuarto día de trabajo del ciclo (inicio 2026-01-05) y 2026-01-09 el primero de descanso.
        Assert.True(programacion[2].Laborable);
        Assert.False(programacion[3].Laborable);
        Assert.Equal("Descanso", programacion[3].Observacion);
    }

    [Fact]
    public async Task ProgramacionSinAsignacionDevuelveSinTurno()
    {
        List<DiaProgramadoDto> programacion = await LeerProgramacionAsync(3, "2026-01-05", "2026-01-07");
        Assert.Equal(3, programacion.Count);
        Assert.All(programacion, item =>
        {
            Assert.False(item.Laborable);
            Assert.Equal("SinTurno", item.Observacion);
            Assert.Null(item.TurnoId);
            Assert.Null(item.HoraEntrada);
        });
    }

    [Fact]
    public async Task ProgramacionDeEmpleadoCesadoMarcaCesadoTrasLaFechaDeCese()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        await InsertarAsignacionAsync(4, dia, new DateOnly(2018, 5, 2), null);
        List<DiaProgramadoDto> programacion = await LeerProgramacionAsync(4, "2026-09-28", "2026-10-02");
        Assert.Equal(new[] { true, true, true, false, false }, programacion.Select(item => item.Laborable).ToArray());
        Assert.Equal(new[] { "Cesado", "Cesado" }, programacion.Skip(3).Select(item => item.Observacion ?? string.Empty).ToArray());
    }

    [Fact]
    public async Task ProgramacionConParametrosInvalidosDevuelveMensajesLiterales()
    {
        string obligatorios = "El empleado y el rango de fechas son obligatorios.";
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/programacion?desde=2026-01-01&hasta=2026-01-02"), 400, obligatorios);
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/programacion?empleadoId=1&hasta=2026-01-02"), 400, obligatorios);
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/programacion?empleadoId=1&desde=2026-01-01"), 400, obligatorios);
        await AssertErrorAsync(
            await _cliente.GetAsync(RutaBase + "/programacion?empleadoId=1&desde=2026-01-05&hasta=2026-01-04"),
            400,
            "La fecha hasta no puede ser anterior a la fecha desde.");
        await AssertErrorAsync(
            await _cliente.GetAsync(RutaBase + "/programacion?empleadoId=1&desde=2026-01-01&hasta=2026-02-01"),
            400,
            "El rango de la programación no puede superar 31 días.");
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/programacion?empleadoId=999&desde=2026-01-01&hasta=2026-01-02"), 404, "El empleado 999 no existe.");
        Assert.Equal(31, (await LeerProgramacionAsync(1, "2026-01-01", "2026-01-31")).Count);
    }

    [Fact]
    public void CalculadoraCalculaUnDiaSinTocarLaBaseDeDatos()
    {
        Turno fijo = CrearTurno("DIA", true);
        fijo.Id = 7;
        DiaProgramadoDto lunes = CalculadoraTurnos.CalcularDia(fijo, new DateOnly(2026, 1, 5), null);
        Assert.True(lunes.Laborable);
        Assert.Equal(7, lunes.TurnoId);
        DiaProgramadoDto domingo = CalculadoraTurnos.CalcularDia(fijo, new DateOnly(2026, 1, 11), null);
        Assert.False(domingo.Laborable);
        Assert.Equal("Descanso", domingo.Observacion);
        Assert.Equal("SinTurno", CalculadoraTurnos.CalcularDia(null, new DateOnly(2026, 1, 5), null).Observacion);
        Assert.Equal("Cesado", CalculadoraTurnos.CalcularDia(fijo, new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 5)).Observacion);
        Assert.True(CalculadoraTurnos.CalcularDia(fijo, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5)).Laborable);
        Assert.Equal(7, CalculadoraTurnos.DiaIso(new DateOnly(2026, 1, 11)));
        Assert.Equal(1, CalculadoraTurnos.DiaIso(new DateOnly(2026, 1, 5)));
    }

    [Fact]
    public async Task SinTokenTodasLasOperacionesDevuelven401()
    {
        HttpClient anonimo = ClienteAnonimo();
        await AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/programacion?empleadoId=1&desde=2026-01-01&hasta=2026-01-02"), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, CrearSolicitud(1, 1, new DateOnly(2026, 3, 1), null)), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/1/finalizacion", new FinalizarAsignacionTurnoRequestDto()), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.DeleteAsync(RutaBase + "/1"), 401, MensajeSinSesion);
    }

    [Fact]
    public async Task TokenSinRolDeEscrituraDevuelve403EnLasTresEscriturasSinCambiarFilas()
    {
        DateOnly hoy = Hoy();
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        AsignacionTurnoIdDto futura = await CrearAsync(CrearSolicitud(1, dia, hoy.AddDays(5), null));
        string[] antes = await LeerFirmasAsync();
        HttpClient consulta = ClienteConRoles("CONSULTA");
        await AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, CrearSolicitud(2, dia, hoy.AddDays(5), null)), 403, MensajeSinPermiso);
        await AssertErrorAsync(
            await consulta.PostAsJsonAsync($"{RutaBase}/{futura.Id}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = hoy.AddDays(6) }),
            403,
            MensajeSinPermiso);
        await AssertErrorAsync(await consulta.DeleteAsync($"{RutaBase}/{futura.Id}"), 403, MensajeSinPermiso);
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenEscribirConEstadoOk(string rol)
    {
        DateOnly hoy = Hoy();
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        HttpClient cliente = ClienteConRoles(rol);
        HttpResponseMessage creada = await cliente.PostAsJsonAsync(RutaBase, CrearSolicitud(1, dia, hoy.AddDays(5), null));
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        AsignacionTurnoIdDto? asignacion = await creada.Content.ReadFromJsonAsync<AsignacionTurnoIdDto>();
        Assert.NotNull(asignacion);
        HttpResponseMessage finalizada = await cliente.PostAsJsonAsync($"{RutaBase}/{asignacion.Id}/finalizacion", new FinalizarAsignacionTurnoRequestDto { FechaFin = hoy.AddDays(9) });
        Assert.Equal(HttpStatusCode.OK, finalizada.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"{RutaBase}/{asignacion.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("CONSULTA")]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task LecturaConCualquierTokenAutenticadoDevuelveOk(string rol)
    {
        HttpClient cliente = ClienteConRoles(rol);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase + "/programacion?empleadoId=1&desde=2026-01-01&hasta=2026-01-02")).StatusCode);
    }

    [Fact]
    public async Task AltaYCierreAutomaticoGeneranRegistrosDeAuditoria()
    {
        int dia = await InsertarTurnoAsync(CrearTurno("DIA", true));
        AsignacionTurnoIdDto primera = await CrearAsync(CrearSolicitud(1, dia, new DateOnly(2026, 1, 1), null));
        List<RegistroAuditoria> alta = await LeerAuditoriaAsync("AsignacionTurno", primera.Id);
        Assert.Single(alta, registro => registro.Accion == "Alta");

        await CrearAsync(CrearSolicitud(1, dia, new DateOnly(2026, 3, 1), null));
        List<RegistroAuditoria> cambios = await LeerAuditoriaAsync("AsignacionTurno", primera.Id);
        RegistroAuditoria cierre = Assert.Single(cambios, registro => registro.Accion == "Modificacion");
        Assert.Contains("FechaFin", CamposDeCambio(cierre));
    }

    private static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static GuardarAsignacionTurnoRequestDto CrearSolicitud(int empleadoId, int turnoId, DateOnly inicio, DateOnly? fin)
    {
        return new GuardarAsignacionTurnoRequestDto
        {
            EmpleadoId = empleadoId,
            TurnoId = turnoId,
            FechaInicio = inicio,
            FechaFin = fin
        };
    }

    private static Turno CrearTurno(string codigo, bool activo)
    {
        return new Turno
        {
            Codigo = codigo,
            Nombre = "Turno " + codigo,
            Tipo = "Fijo",
            HoraEntrada = new TimeOnly(8, 0),
            HoraSalida = new TimeOnly(17, 0),
            ToleranciaMinutos = 10,
            DiasLaborables = "1,2,3,4,5",
            Activo = activo
        };
    }

    private static Turno CrearTurnoRotativo(string codigo)
    {
        return new Turno
        {
            Codigo = codigo,
            Nombre = "Turno " + codigo,
            Tipo = "Rotativo",
            HoraEntrada = new TimeOnly(7, 0),
            HoraSalida = new TimeOnly(19, 0),
            ToleranciaMinutos = 15,
            DiasTrabajo = 4,
            DiasDescanso = 2,
            FechaInicioCiclo = new DateOnly(2026, 1, 5),
            Activo = true
        };
    }

    private HttpClient ClienteAnonimo()
    {
        HttpClient cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = null;
        return cliente;
    }

    private HttpClient ClienteConRoles(params string[] roles)
    {
        HttpClient cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_fabrica.Services, roles));
        return cliente;
    }

    private async Task<AsignacionTurnoIdDto> CrearAsync(GuardarAsignacionTurnoRequestDto solicitud)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        AsignacionTurnoIdDto? creada = await respuesta.Content.ReadFromJsonAsync<AsignacionTurnoIdDto>();
        Assert.NotNull(creada);
        return creada;
    }

    private async Task<PaginaAsignacionesTurnoDto> LeerPaginaAsync(string consulta)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync(RutaBase + consulta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PaginaAsignacionesTurnoDto? pagina = await respuesta.Content.ReadFromJsonAsync<PaginaAsignacionesTurnoDto>();
        Assert.NotNull(pagina);
        return pagina;
    }

    private async Task<List<DiaProgramadoDto>> LeerProgramacionAsync(int empleadoId, string desde, string hasta)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync($"{RutaBase}/programacion?empleadoId={empleadoId}&desde={desde}&hasta={hasta}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        List<DiaProgramadoDto>? dias = await respuesta.Content.ReadFromJsonAsync<List<DiaProgramadoDto>>();
        Assert.NotNull(dias);
        return dias;
    }

    private async Task<int> InsertarTurnoAsync(Turno turno)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Turnos.Add(turno);
        await contexto.SaveChangesAsync();
        return turno.Id;
    }

    private async Task InsertarAsignacionAsync(int empleadoId, int turnoId, DateOnly inicio, DateOnly? fin)
    {
        await InsertarAsignacionConIdAsync(empleadoId, turnoId, inicio, fin);
    }

    private async Task<int> InsertarAsignacionConIdAsync(int empleadoId, int turnoId, DateOnly inicio, DateOnly? fin)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        AsignacionTurno asignacion = new AsignacionTurno
        {
            EmpleadoId = empleadoId,
            TurnoId = turnoId,
            FechaInicio = inicio,
            FechaFin = fin
        };
        contexto.AsignacionesTurno.Add(asignacion);
        await contexto.SaveChangesAsync();
        return asignacion.Id;
    }

    private async Task<AsignacionTurno> ObtenerAsignacionAsync(int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.AsignacionesTurno.AsNoTracking().SingleAsync(asignacion => asignacion.Id == id);
    }

    private async Task<List<AsignacionTurno>> LeerAsignacionesAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.AsignacionesTurno.AsNoTracking().OrderBy(asignacion => asignacion.Id).ToListAsync();
    }

    private async Task<string[]> LeerFirmasAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        List<Turno> turnos = await contexto.Turnos.AsNoTracking().OrderBy(turno => turno.Id).ToListAsync();
        List<AsignacionTurno> asignaciones = await contexto.AsignacionesTurno.AsNoTracking().OrderBy(asignacion => asignacion.Id).ToListAsync();
        List<string> firmas = turnos.Select(turno => $"T|{turno.Id}|{turno.Codigo}|{turno.Activo}").ToList();
        firmas.AddRange(asignaciones.Select(asignacion =>
            $"A|{asignacion.Id}|{asignacion.EmpleadoId}|{asignacion.TurnoId}|{asignacion.FechaInicio:yyyy-MM-dd}|{asignacion.FechaFin:yyyy-MM-dd}"));
        return firmas.ToArray();
    }

    private async Task<List<RegistroAuditoria>> LeerAuditoriaAsync(string entidad, int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        string idTexto = id.ToString(CultureInfo.InvariantCulture);
        return await contexto.RegistrosAuditoria.AsNoTracking()
            .Where(registro => registro.Entidad == entidad && registro.EntidadId == idTexto)
            .OrderBy(registro => registro.Id)
            .ToListAsync();
    }

    private static List<string> CamposDeCambio(RegistroAuditoria registro)
    {
        using JsonDocument cambios = JsonDocument.Parse(registro.Cambios);
        return cambios.RootElement.EnumerateArray()
            .Select(cambio => cambio.GetProperty("campo").GetString() ?? string.Empty)
            .ToList();
    }

    private void RestablecerDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.AsignacionesTurno.RemoveRange(contexto.AsignacionesTurno.ToList());
        contexto.Turnos.RemoveRange(contexto.Turnos.ToList());
        contexto.AdendasContrato.RemoveRange(contexto.AdendasContrato.ToList());
        contexto.ContratosLaborales.RemoveRange(contexto.ContratosLaborales.ToList());
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        BorrarAuditoriaDelModulo(contexto);
        contexto.Empleados.AddRange(
            CrearEmpleado(1, "Ana", "Torres Vega", new DateOnly(2021, 3, 1), "Activo", null),
            CrearEmpleado(2, "Luis", "Rojas Mendoza", new DateOnly(2019, 8, 16), "Activo", null),
            CrearEmpleado(3, "Maria", "Salazar Quispe", new DateOnly(2024, 1, 8), "Activo", null),
            CrearEmpleado(4, "Carlos", "Diaz Soto", new DateOnly(2018, 5, 2), "Cesado", new DateOnly(2026, 9, 30)));
        contexto.SaveChanges();
        BorrarAuditoriaDelModulo(contexto);
    }

    private static Empleado CrearEmpleado(int id, string nombres, string apellidos, DateOnly ingreso, string estado, DateOnly? cese)
    {
        return new Empleado
        {
            Id = id,
            Legajo = "EMP-" + id.ToString("D6", CultureInfo.InvariantCulture),
            TipoDocumento = "DNI",
            NumeroDocumento = "1000000" + id.ToString(CultureInfo.InvariantCulture),
            Nombres = nombres,
            Apellidos = apellidos,
            FechaNacimiento = new DateOnly(1990, 1, 1),
            FechaIngreso = ingreso,
            Cargo = "Operario",
            Area = "Operaciones",
            Estado = estado,
            FechaCese = cese,
            MotivoCese = cese is null ? null : "Renuncia voluntaria"
        };
    }

    private static void BorrarAuditoriaDelModulo(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria
                .Where(registro => registro.Entidad == "Empleado"
                    || registro.Entidad == "ContratoLaboral"
                    || registro.Entidad == "AdendaContrato"
                    || registro.Entidad == "Turno"
                    || registro.Entidad == "AsignacionTurno")
                .ToList());
        contexto.SaveChanges();
    }

    private static async Task AssertErrorAsync(HttpResponseMessage respuesta, int estado, string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
