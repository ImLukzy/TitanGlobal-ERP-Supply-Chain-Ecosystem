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
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo64_TurnosHorarios;

[Collection("Modulo04")]
public class TurnosApiTests
{
    private const string RutaBase = "/api/v1/turnos";
    private const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";
    private const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public TurnosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    public static IEnumerable<object[]> CasosDeCamposInvalidos()
    {
        return new List<object[]>
        {
            new object[] { "codigo-nulo", "El código es obligatorio." },
            new object[] { "codigo-espacios", "El código es obligatorio." },
            new object[] { "codigo-largo", "El código no puede superar 20 caracteres." },
            new object[] { "nombre-vacio", "El nombre es obligatorio." },
            new object[] { "nombre-largo", "El nombre no puede superar 100 caracteres." },
            new object[] { "tipo-otro", "El tipo de turno debe ser Fijo o Rotativo." },
            new object[] { "tipo-minusculas", "El tipo de turno debe ser Fijo o Rotativo." },
            new object[] { "entrada-nula", "La hora de entrada es obligatoria." },
            new object[] { "salida-nula", "La hora de salida es obligatoria." },
            new object[] { "salida-igual", "La hora de salida no puede ser igual a la hora de entrada." },
            new object[] { "salida-anterior-sin-cruce", "Si la hora de salida es anterior a la de entrada, el turno debe cruzar medianoche." },
            new object[] { "cruce-con-salida-posterior", "El turno solo cruza medianoche si la hora de salida es anterior a la de entrada." },
            new object[] { "tolerancia-nula", "La tolerancia es obligatoria." },
            new object[] { "tolerancia-negativa", "La tolerancia debe estar entre 0 y 30 minutos." },
            new object[] { "tolerancia-31", "La tolerancia debe estar entre 0 y 30 minutos." },
            new object[] { "fijo-sin-dias", "El turno fijo debe tener al menos un día laborable." },
            new object[] { "fijo-dias-nulos", "El turno fijo debe tener al menos un día laborable." },
            new object[] { "fijo-dia-cero", "Los días laborables deben ser números del 1 (lunes) al 7 (domingo) sin repetir." },
            new object[] { "fijo-dia-ocho", "Los días laborables deben ser números del 1 (lunes) al 7 (domingo) sin repetir." },
            new object[] { "fijo-dias-repetidos", "Los días laborables deben ser números del 1 (lunes) al 7 (domingo) sin repetir." },
            new object[] { "fijo-con-ciclo", "El turno fijo no usa ciclo de trabajo y descanso." },
            new object[] { "fijo-con-inicio-ciclo", "El turno fijo no usa ciclo de trabajo y descanso." },
            new object[] { "rotativo-con-dias", "El turno rotativo no usa días laborables de la semana." },
            new object[] { "rotativo-sin-trabajo", "El turno rotativo necesita días de trabajo y días de descanso." },
            new object[] { "rotativo-sin-descanso", "El turno rotativo necesita días de trabajo y días de descanso." },
            new object[] { "rotativo-trabajo-cero", "Los días de trabajo y de descanso deben estar entre 1 y 30." },
            new object[] { "rotativo-trabajo-31", "Los días de trabajo y de descanso deben estar entre 1 y 30." },
            new object[] { "rotativo-descanso-cero", "Los días de trabajo y de descanso deben estar entre 1 y 30." },
            new object[] { "rotativo-sin-inicio-ciclo", "La fecha de inicio del ciclo es obligatoria en el turno rotativo." }
        };
    }

    // Un caso por cada mensaje 1 a 19, en el alta y en la edición.
    [Theory]
    [MemberData(nameof(CasosDeCamposInvalidos))]
    public async Task AltaConCamposInvalidosDevuelveMensajeLiteralSinCrearFilas(string caso, string mensaje)
    {
        GuardarTurnoRequestDto solicitud = CrearTurnoValido("T1");
        AplicarCaso(caso, solicitud);
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        await AssertErrorAsync(respuesta, 400, mensaje);
        Assert.Equal(antes, await LeerFirmasAsync());
        Assert.Empty(antes);
    }

    [Theory]
    [MemberData(nameof(CasosDeCamposInvalidos))]
    public async Task EdicionConCamposInvalidosDevuelveMensajeLiteralSinCambiarFilas(string caso, string mensaje)
    {
        int id = await InsertarTurnoAsync(CrearTurnoFijo("ORIG"));
        GuardarTurnoRequestDto solicitud = CrearTurnoValido("T1");
        AplicarCaso(caso, solicitud);
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{RutaBase}/{id}", solicitud);
        await AssertErrorAsync(respuesta, 400, mensaje);
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AltaConVariosErroresDevuelveElPrimeroEnElOrdenDeLaSpec()
    {
        GuardarTurnoRequestDto solicitud = CrearTurnoValido("T1");
        solicitud.Codigo = null;
        solicitud.Tipo = "Otro";
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El código es obligatorio.");
        solicitud.Codigo = "T1";
        solicitud.ToleranciaMinutos = null;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El tipo de turno debe ser Fijo o Rotativo.");
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task CuerpoVacioEnElAltaDevuelveElLiteralDelMiddleware()
    {
        HttpResponseMessage respuesta = await _cliente.PostAsync(RutaBase, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        await AssertErrorAsync(respuesta, 400, "El cuerpo de la petición (Body) no puede estar vacío.");
    }

    [Fact]
    public async Task AltaDeTurnoFijoGuardaCodigoEnMayusculasYDiasOrdenados()
    {
        GuardarTurnoRequestDto solicitud = CrearTurnoValido("  dia1 ");
        solicitud.DiasLaborables = new List<int> { 5, 1, 3 };
        TurnoIdDto creado = await CrearAsync(solicitud);
        Turno guardado = await ObtenerTurnoAsync(creado.Id);
        Assert.Equal("DIA1", guardado.Codigo);
        Assert.Equal("Turno de prueba", guardado.Nombre);
        Assert.Equal("Fijo", guardado.Tipo);
        Assert.Equal("1,3,5", guardado.DiasLaborables);
        Assert.Equal(new TimeOnly(8, 0), guardado.HoraEntrada);
        Assert.Equal(new TimeOnly(17, 0), guardado.HoraSalida);
        Assert.False(guardado.CruzaMedianoche);
        Assert.Equal(10, guardado.ToleranciaMinutos);
        Assert.True(guardado.Activo);
        Assert.Null(guardado.DiasTrabajo);
        Assert.Null(guardado.DiasDescanso);
        Assert.Null(guardado.FechaInicioCiclo);
    }

    [Fact]
    public async Task AltaDeTurnoNocturnoGuardaCruceDeMedianoche()
    {
        GuardarTurnoRequestDto solicitud = CrearTurnoValido("NOCHE");
        solicitud.HoraEntrada = new TimeOnly(22, 0);
        solicitud.HoraSalida = new TimeOnly(6, 0);
        solicitud.CruzaMedianoche = true;
        solicitud.ToleranciaMinutos = 0;
        TurnoIdDto creado = await CrearAsync(solicitud);
        Turno guardado = await ObtenerTurnoAsync(creado.Id);
        Assert.True(guardado.CruzaMedianoche);
        Assert.Equal(new TimeOnly(22, 0), guardado.HoraEntrada);
        Assert.Equal(new TimeOnly(6, 0), guardado.HoraSalida);
        Assert.Equal(0, guardado.ToleranciaMinutos);

        TurnoDetalleDto? detalle = await _cliente.GetFromJsonAsync<TurnoDetalleDto>($"{RutaBase}/{creado.Id}");
        Assert.NotNull(detalle);
        Assert.True(detalle.CruzaMedianoche);
        Assert.Equal(new List<int> { 1, 2, 3, 4, 5 }, detalle.DiasLaborables);
    }

    [Fact]
    public async Task AltaDeTurnoRotativoGuardaElCicloYNoLosDiasDeLaSemana()
    {
        TurnoIdDto creado = await CrearAsync(CrearTurnoRotativo("ROT"));
        Turno guardado = await ObtenerTurnoAsync(creado.Id);
        Assert.Equal("Rotativo", guardado.Tipo);
        Assert.Null(guardado.DiasLaborables);
        Assert.Equal(4, guardado.DiasTrabajo);
        Assert.Equal(2, guardado.DiasDescanso);
        Assert.Equal(new DateOnly(2026, 1, 5), guardado.FechaInicioCiclo);
        Assert.True(guardado.Activo);
    }

    [Fact]
    public async Task AltaConCodigoRepetidoDevuelve409AunConOtraCapitalizacion()
    {
        await CrearAsync(CrearTurnoValido("ABC"));
        string[] antes = await LeerFirmasAsync();
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearTurnoValido("ABC")), 409, "Ya existe un turno con el código ABC.");
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearTurnoValido("abc")), 409, "Ya existe un turno con el código ABC.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task EdicionSinAsignacionesActualizaElTurno()
    {
        int id = await InsertarTurnoAsync(CrearTurnoFijo("ORIG"));
        GuardarTurnoRequestDto solicitud = CrearTurnoRotativo("nuevo");
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{RutaBase}/{id}", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Turno guardado = await ObtenerTurnoAsync(id);
        Assert.Equal("NUEVO", guardado.Codigo);
        Assert.Equal("Rotativo", guardado.Tipo);
        Assert.Null(guardado.DiasLaborables);
        Assert.Equal(4, guardado.DiasTrabajo);
        Assert.True(guardado.Activo);

        // El mismo código del turno que se edita no es duplicado.
        GuardarTurnoRequestDto igual = CrearTurnoRotativo("NUEVO");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{RutaBase}/{id}", igual)).StatusCode);
    }

    [Fact]
    public async Task EdicionConCodigoDeOtroTurnoDevuelve409()
    {
        await InsertarTurnoAsync(CrearTurnoFijo("UNO"));
        int id = await InsertarTurnoAsync(CrearTurnoFijo("DOS"));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{RutaBase}/{id}", CrearTurnoValido("uno"));
        await AssertErrorAsync(respuesta, 409, "Ya existe un turno con el código UNO.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task EdicionConAsignacionesDevuelve409SinCambios()
    {
        int id = await InsertarTurnoAsync(CrearTurnoFijo("USADO"));
        await InsertarAsignacionAsync(1, id, new DateOnly(2026, 1, 1), null);
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{RutaBase}/{id}", CrearTurnoValido("OTRO"));
        await AssertErrorAsync(respuesta, 409, "El turno USADO tiene asignaciones y no se puede modificar. Crea un turno nuevo.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task TurnoInexistenteDevuelve404EnLecturaEdicionYCambioDeEstado()
    {
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/999"), 404, "El turno 999 no existe.");
        await AssertErrorAsync(await _cliente.PutAsJsonAsync(RutaBase + "/999", CrearTurnoValido("X")), 404, "El turno 999 no existe.");
        await AssertErrorAsync(await _cliente.PostAsync(RutaBase + "/999/desactivacion", null), 404, "El turno 999 no existe.");
        await AssertErrorAsync(await _cliente.PostAsync(RutaBase + "/999/activacion", null), 404, "El turno 999 no existe.");
    }

    [Fact]
    public async Task DesactivarYActivarCambianElEstadoYRechazanLaRepeticion()
    {
        int id = await InsertarTurnoAsync(CrearTurnoFijo("CAMBIO"));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/desactivacion", null)).StatusCode);
        Assert.False((await ObtenerTurnoAsync(id)).Activo);
        await AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/desactivacion", null), 409, "El turno CAMBIO ya está inactivo.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/activacion", null)).StatusCode);
        Assert.True((await ObtenerTurnoAsync(id)).Activo);
        await AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/activacion", null), 409, "El turno CAMBIO ya está activo.");
    }

    [Fact]
    public async Task ListadoAplicaFiltrosYPaginacionOrdenadoPorCodigo()
    {
        await InsertarTurnoAsync(CrearTurnoFijo("DIA"));
        await InsertarTurnoAsync(CrearTurnoRotativoEntidad("ROT4X2"));
        Turno inactivo = CrearTurnoFijo("SABADO");
        inactivo.Nombre = "Sabado medio dia";
        inactivo.Activo = false;
        await InsertarTurnoAsync(inactivo);

        PaginaTurnosDto todos = await LeerPaginaAsync(string.Empty);
        Assert.Equal(3, todos.Total);
        Assert.Equal(new[] { "DIA", "ROT4X2", "SABADO" }, todos.Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(1, todos.Pagina);
        Assert.Equal(10, todos.TamanoPagina);

        Assert.Equal(new[] { "ROT4X2" }, (await LeerPaginaAsync("?tipo=Rotativo")).Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "SABADO" }, (await LeerPaginaAsync("?activo=false")).Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(2, (await LeerPaginaAsync("?activo=true")).Total);
        Assert.Equal(new[] { "SABADO" }, (await LeerPaginaAsync("?busqueda=MEDIO")).Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "ROT4X2" }, (await LeerPaginaAsync("?busqueda=rot4")).Items.Select(item => item.Codigo).ToArray());

        PaginaTurnosDto segunda = await LeerPaginaAsync("?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Equal(new[] { "SABADO" }, segunda.Items.Select(item => item.Codigo).ToArray());
        TurnoListadoDto rotativo = (await LeerPaginaAsync("?tipo=Rotativo")).Items.Single();
        Assert.Equal(4, rotativo.DiasTrabajo);
        Assert.Equal(2, rotativo.DiasDescanso);
        Assert.Empty(rotativo.DiasLaborables);
    }

    [Fact]
    public async Task ListadoConParametrosInvalidosDevuelveMensajeLiteral()
    {
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tipo=Otro"), 400, "El tipo debe ser Fijo o Rotativo.");
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task ResumenCuentaTurnosYEmpleadosConTurnoHoyIgnorandoCesados()
    {
        DateOnly hoy = Hoy();
        int dia = await InsertarTurnoAsync(CrearTurnoFijo("DIA"));
        await InsertarTurnoAsync(CrearTurnoRotativoEntidad("ROT"));
        Turno inactivo = CrearTurnoFijo("SAB");
        inactivo.Activo = false;
        await InsertarTurnoAsync(inactivo);
        await InsertarAsignacionAsync(1, dia, hoy.AddDays(-10), null);
        await InsertarAsignacionAsync(2, dia, hoy.AddDays(-30), hoy.AddDays(-1));
        await InsertarAsignacionAsync(3, dia, hoy.AddDays(5), null);
        await InsertarAsignacionAsync(4, dia, hoy.AddDays(-100), null);

        ResumenTurnosDto? resumen = await _cliente.GetFromJsonAsync<ResumenTurnosDto>(RutaBase + "/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.TotalTurnos);
        Assert.Equal(2, resumen.Activos);
        Assert.Equal(2, resumen.Fijos);
        Assert.Equal(1, resumen.Rotativos);
        Assert.Equal(1, resumen.EmpleadosConTurnoHoy);
        Assert.Equal(2, resumen.EmpleadosActivosSinTurnoHoy);
    }

    [Fact]
    public async Task DetalleIncluyeTotalDeAsignacionesYEmpleadosConTurnoHoy()
    {
        DateOnly hoy = Hoy();
        int id = await InsertarTurnoAsync(CrearTurnoFijo("DIA"));
        await InsertarAsignacionAsync(1, id, hoy.AddDays(-10), null);
        await InsertarAsignacionAsync(2, id, hoy.AddDays(-30), hoy.AddDays(-1));
        TurnoDetalleDto? detalle = await _cliente.GetFromJsonAsync<TurnoDetalleDto>($"{RutaBase}/{id}");
        Assert.NotNull(detalle);
        Assert.Equal("DIA", detalle.Codigo);
        Assert.Equal(2, detalle.TotalAsignaciones);
        Assert.Equal(1, detalle.EmpleadosConTurnoHoy);
        Assert.Equal(new List<int> { 1, 2, 3, 4, 5 }, detalle.DiasLaborables);
    }

    [Fact]
    public async Task SinTokenTodasLasOperacionesDevuelven401()
    {
        HttpClient anonimo = ClienteAnonimo();
        await AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/resumen"), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/1"), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, CrearTurnoValido("X")), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PutAsJsonAsync(RutaBase + "/1", CrearTurnoValido("X")), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsync(RutaBase + "/1/desactivacion", null), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsync(RutaBase + "/1/activacion", null), 401, MensajeSinSesion);
    }

    [Fact]
    public async Task TokenSinRolDeEscrituraDevuelve403SinCambiarFilas()
    {
        int id = await InsertarTurnoAsync(CrearTurnoFijo("DIA"));
        string[] antes = await LeerFirmasAsync();
        HttpClient consulta = ClienteConRoles("CONSULTA");
        await AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, CrearTurnoValido("NUEVO")), 403, MensajeSinPermiso);
        await AssertErrorAsync(await consulta.PutAsJsonAsync($"{RutaBase}/{id}", CrearTurnoValido("NUEVO")), 403, MensajeSinPermiso);
        await AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/desactivacion", null), 403, MensajeSinPermiso);
        await AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/activacion", null), 403, MensajeSinPermiso);
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenEscribirConEstadoOk(string rol)
    {
        HttpClient cliente = ClienteConRoles(rol);
        HttpResponseMessage creado = await cliente.PostAsJsonAsync(RutaBase, CrearTurnoValido("RR" + rol));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        TurnoIdDto? turno = await creado.Content.ReadFromJsonAsync<TurnoIdDto>();
        Assert.NotNull(turno);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"{RutaBase}/{turno.Id}", CrearTurnoValido("RR" + rol + "2"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{turno.Id}/desactivacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{turno.Id}/activacion", null)).StatusCode);
    }

    [Theory]
    [InlineData("CONSULTA")]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task LecturaConCualquierTokenAutenticadoDevuelveOk(string rol)
    {
        int id = await InsertarTurnoAsync(CrearTurnoFijo("DIA"));
        HttpClient cliente = ClienteConRoles(rol);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase + "/resumen")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"{RutaBase}/{id}")).StatusCode);
    }

    [Fact]
    public async Task AltaEdicionYCambioDeEstadoGeneranRegistrosDeAuditoria()
    {
        TurnoIdDto creado = await CrearAsync(CrearTurnoValido("AUD"));
        List<RegistroAuditoria> alta = await LeerAuditoriaAsync("Turno", creado.Id);
        Assert.Single(alta, registro => registro.Accion == "Alta");

        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{creado.Id}/desactivacion", null)).StatusCode);
        List<RegistroAuditoria> cambios = await LeerAuditoriaAsync("Turno", creado.Id);
        RegistroAuditoria modificacion = Assert.Single(cambios, registro => registro.Accion == "Modificacion");
        Assert.Contains("Activo", CamposDeCambio(modificacion));
    }

    private static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static GuardarTurnoRequestDto CrearTurnoValido(string codigo)
    {
        return new GuardarTurnoRequestDto
        {
            Codigo = codigo,
            Nombre = "Turno de prueba",
            Tipo = "Fijo",
            HoraEntrada = new TimeOnly(8, 0),
            HoraSalida = new TimeOnly(17, 0),
            CruzaMedianoche = false,
            ToleranciaMinutos = 10,
            DiasLaborables = new List<int> { 1, 2, 3, 4, 5 }
        };
    }

    private static GuardarTurnoRequestDto CrearTurnoRotativo(string codigo)
    {
        GuardarTurnoRequestDto solicitud = CrearTurnoValido(codigo);
        solicitud.Tipo = "Rotativo";
        solicitud.HoraEntrada = new TimeOnly(7, 0);
        solicitud.HoraSalida = new TimeOnly(19, 0);
        solicitud.ToleranciaMinutos = 15;
        solicitud.DiasLaborables = null;
        solicitud.DiasTrabajo = 4;
        solicitud.DiasDescanso = 2;
        solicitud.FechaInicioCiclo = new DateOnly(2026, 1, 5);
        return solicitud;
    }

    private static Turno CrearTurnoFijo(string codigo)
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
            Activo = true
        };
    }

    private static Turno CrearTurnoRotativoEntidad(string codigo)
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

    private static void AplicarCaso(string caso, GuardarTurnoRequestDto solicitud)
    {
        switch (caso)
        {
            case "codigo-nulo":
                solicitud.Codigo = null;
                break;
            case "codigo-espacios":
                solicitud.Codigo = "   ";
                break;
            case "codigo-largo":
                solicitud.Codigo = new string('A', 21);
                break;
            case "nombre-vacio":
                solicitud.Nombre = "";
                break;
            case "nombre-largo":
                solicitud.Nombre = new string('N', 101);
                break;
            case "tipo-otro":
                solicitud.Tipo = "Otro";
                break;
            case "tipo-minusculas":
                solicitud.Tipo = "fijo";
                break;
            case "entrada-nula":
                solicitud.HoraEntrada = null;
                break;
            case "salida-nula":
                solicitud.HoraSalida = null;
                break;
            case "salida-igual":
                solicitud.HoraSalida = solicitud.HoraEntrada;
                break;
            case "salida-anterior-sin-cruce":
                solicitud.HoraEntrada = new TimeOnly(22, 0);
                solicitud.HoraSalida = new TimeOnly(6, 0);
                break;
            case "cruce-con-salida-posterior":
                solicitud.CruzaMedianoche = true;
                break;
            case "tolerancia-nula":
                solicitud.ToleranciaMinutos = null;
                break;
            case "tolerancia-negativa":
                solicitud.ToleranciaMinutos = -1;
                break;
            case "tolerancia-31":
                solicitud.ToleranciaMinutos = 31;
                break;
            case "fijo-sin-dias":
                solicitud.DiasLaborables = new List<int>();
                break;
            case "fijo-dias-nulos":
                solicitud.DiasLaborables = null;
                break;
            case "fijo-dia-cero":
                solicitud.DiasLaborables = new List<int> { 0, 1 };
                break;
            case "fijo-dia-ocho":
                solicitud.DiasLaborables = new List<int> { 1, 8 };
                break;
            case "fijo-dias-repetidos":
                solicitud.DiasLaborables = new List<int> { 1, 1 };
                break;
            case "fijo-con-ciclo":
                solicitud.DiasTrabajo = 4;
                break;
            case "fijo-con-inicio-ciclo":
                solicitud.FechaInicioCiclo = new DateOnly(2026, 1, 5);
                break;
            case "rotativo-con-dias":
                AplicarRotativo(solicitud);
                solicitud.DiasLaborables = new List<int> { 1 };
                break;
            case "rotativo-sin-trabajo":
                AplicarRotativo(solicitud);
                solicitud.DiasTrabajo = null;
                break;
            case "rotativo-sin-descanso":
                AplicarRotativo(solicitud);
                solicitud.DiasDescanso = null;
                break;
            case "rotativo-trabajo-cero":
                AplicarRotativo(solicitud);
                solicitud.DiasTrabajo = 0;
                break;
            case "rotativo-trabajo-31":
                AplicarRotativo(solicitud);
                solicitud.DiasTrabajo = 31;
                break;
            case "rotativo-descanso-cero":
                AplicarRotativo(solicitud);
                solicitud.DiasDescanso = 0;
                break;
            case "rotativo-sin-inicio-ciclo":
                AplicarRotativo(solicitud);
                solicitud.FechaInicioCiclo = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(caso), caso, "Caso de prueba desconocido.");
        }
    }

    private static void AplicarRotativo(GuardarTurnoRequestDto solicitud)
    {
        solicitud.Tipo = "Rotativo";
        solicitud.DiasLaborables = null;
        solicitud.DiasTrabajo = 4;
        solicitud.DiasDescanso = 2;
        solicitud.FechaInicioCiclo = new DateOnly(2026, 1, 5);
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

    private async Task<TurnoIdDto> CrearAsync(GuardarTurnoRequestDto solicitud)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        TurnoIdDto? creado = await respuesta.Content.ReadFromJsonAsync<TurnoIdDto>();
        Assert.NotNull(creado);
        return creado;
    }

    private async Task<PaginaTurnosDto> LeerPaginaAsync(string consulta)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync(RutaBase + consulta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PaginaTurnosDto? pagina = await respuesta.Content.ReadFromJsonAsync<PaginaTurnosDto>();
        Assert.NotNull(pagina);
        return pagina;
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
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.AsignacionesTurno.Add(new AsignacionTurno
        {
            EmpleadoId = empleadoId,
            TurnoId = turnoId,
            FechaInicio = inicio,
            FechaFin = fin
        });
        await contexto.SaveChangesAsync();
    }

    private async Task<Turno> ObtenerTurnoAsync(int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Turnos.AsNoTracking().SingleAsync(turno => turno.Id == id);
    }

    private async Task<string[]> LeerFirmasAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        List<Turno> turnos = await contexto.Turnos.AsNoTracking().OrderBy(turno => turno.Id).ToListAsync();
        List<AsignacionTurno> asignaciones = await contexto.AsignacionesTurno.AsNoTracking().OrderBy(asignacion => asignacion.Id).ToListAsync();
        List<string> firmas = turnos.Select(FirmaTurno).ToList();
        firmas.AddRange(asignaciones.Select(asignacion =>
            $"A|{asignacion.Id}|{asignacion.EmpleadoId}|{asignacion.TurnoId}|{asignacion.FechaInicio:yyyy-MM-dd}|{asignacion.FechaFin:yyyy-MM-dd}"));
        return firmas.ToArray();
    }

    private static string FirmaTurno(Turno turno)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "T|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}",
            turno.Id,
            turno.Codigo,
            turno.Nombre,
            turno.Tipo,
            turno.HoraEntrada,
            turno.HoraSalida,
            turno.CruzaMedianoche,
            turno.ToleranciaMinutos,
            turno.DiasLaborables,
            turno.DiasTrabajo,
            turno.DiasDescanso,
            turno.FechaInicioCiclo,
            turno.Activo);
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
