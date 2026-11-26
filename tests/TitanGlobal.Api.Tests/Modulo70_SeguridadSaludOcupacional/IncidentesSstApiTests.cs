using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo70_SeguridadSaludOcupacional;

[Collection("Modulo04")]
public class IncidentesSstApiTests
{
    private const string RutaBase = "/api/v1/incidentes-sst";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public IncidentesSstApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosSst.Restablecer(fabrica);
    }

    // Mensajes 8, 11 y 20 a 25 en el orden de la spec.
    [Fact]
    public async Task RegistroConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        DateOnly hoy = DatosSst.Hoy;
        string tipo = "El tipo de incidente debe ser AccidenteSinBaja, AccidenteConBaja, IncidenteSinLesion o EnfermedadOcupacional.";
        string severidad = "La severidad debe ser Leve, Moderada, Grave o Mortal.";
        string fecha = "La fecha es obligatoria y no puede ser posterior a hoy.";
        string descripcion = "La descripción es obligatoria y no puede superar 1000 caracteres.";
        await AssertRegistrarAsync(Solicitud(null, "AccidenteSinBaja", "Leve", hoy, "Planta", "Caída", 0), 400, "El empleado es obligatorio.");
        await AssertRegistrarAsync(Solicitud(1, null, "Leve", hoy, "Planta", "Caída", 0), 400, tipo);
        await AssertRegistrarAsync(Solicitud(1, "accidentesinbaja", "Leve", hoy, "Planta", "Caída", 0), 400, tipo);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", null, hoy, "Planta", "Caída", 0), 400, severidad);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Critica", hoy, "Planta", "Caída", 0), 400, severidad);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Leve", null, "Planta", "Caída", 0), 400, fecha);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Leve", hoy.AddDays(1), "Planta", "Caída", 0), 400, fecha);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteConBaja", "Grave", hoy, "Planta", "Caída", null), 400, "Un accidente con baja exige al menos 1 día de baja.");
        await AssertRegistrarAsync(Solicitud(1, "AccidenteConBaja", "Grave", hoy, "Planta", "Caída", 0), 400, "Un accidente con baja exige al menos 1 día de baja.");
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Leve", hoy, "Planta", "Caída", 2), 400, "Solo un accidente con baja tiene días de baja.");
        await AssertRegistrarAsync(Solicitud(1, "IncidenteSinLesion", "Leve", hoy, "Planta", "Caída", 1), 400, "Solo un accidente con baja tiene días de baja.");
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Leve", hoy, "Planta", null, 0), 400, descripcion);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Leve", hoy, "Planta", "   ", 0), 400, descripcion);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Leve", hoy, "Planta", new string('D', 1001), 0), 400, descripcion);
        await AssertRegistrarAsync(Solicitud(1, "AccidenteSinBaja", "Leve", hoy, new string('L', 151), "Caída", 0), 400, "El lugar no puede superar 150 caracteres.");
        await AssertRegistrarAsync(Solicitud(9999, "AccidenteSinBaja", "Leve", hoy, "Planta", "Caída", 0), 404, "El empleado 9999 no existe.");
        Assert.Empty(await DatosSst.LeerIncidentesAsync(_fabrica));
    }

    // S3: el número definitivo INC-{Id:D6} sustituye al temporal en la misma transacción.
    [Fact]
    public async Task RegistroAsignaElNumeroDefinitivoYNormalizaLosTextos()
    {
        DateOnly hoy = DatosSst.Hoy;
        HttpResponseMessage creado = await _cliente.PostAsJsonAsync(
            RutaBase,
            Solicitud(1, " AccidenteConBaja ", " Grave ", hoy, " Taller 2 ", " Corte en la mano ", 7));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        IncidenteSstCreadoDto? id = await creado.Content.ReadFromJsonAsync<IncidenteSstCreadoDto>();
        Assert.NotNull(id);
        Assert.Equal("INC-" + id.Id.ToString("D6"), id.Numero);
        IncidenteSst guardado = Assert.Single(await DatosSst.LeerIncidentesAsync(_fabrica));
        Assert.Equal(id.Numero, guardado.Numero);
        Assert.Equal("AccidenteConBaja", guardado.Tipo);
        Assert.Equal("Grave", guardado.Severidad);
        Assert.Equal("Taller 2", guardado.Lugar);
        Assert.Equal("Corte en la mano", guardado.Descripcion);
        Assert.Equal(7, guardado.DiasBaja);
        Assert.Equal("Abierto", guardado.Estado);
        Assert.Null(guardado.FechaCierre);
        HttpResponseMessage segundo = await _cliente.PostAsJsonAsync(RutaBase, Solicitud(4, "IncidenteSinLesion", "Mortal", hoy, null, new string('D', 1000), null));
        Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
        IncidenteSstCreadoDto? idSegundo = await segundo.Content.ReadFromJsonAsync<IncidenteSstCreadoDto>();
        Assert.NotNull(idSegundo);
        Assert.NotEqual(id.Numero, idSegundo.Numero);
        Assert.Equal(2, (await DatosSst.LeerIncidentesAsync(_fabrica)).Count);
        Assert.DoesNotContain(await DatosSst.LeerIncidentesAsync(_fabrica), incidente => incidente.Numero.StartsWith("TMP-"));
    }

    // Mensaje 26 en todas las rutas con id.
    [Fact]
    public async Task IncidenteInexistenteDevuelve404ConMensaje26EnTodasLasRutas()
    {
        string mensaje = "El incidente 9999 no existe.";
        GuardarAccionCorrectivaRequestDto accion = Accion("Señalizar", "Jefe", DatosSst.Hoy);
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999"), 404, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/9999/acciones", accion), 404, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/9999/acciones/1/completar", Completar(DatosSst.Hoy)), 404, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/cierre", null), 404, mensaje);
    }

    // Mensajes 28, 29 y 30, y la lectura del detalle con acciones.
    [Fact]
    public async Task AccionesSeAgreganYSeCompletanConSusMensajes()
    {
        DateOnly hoy = DatosSst.Hoy;
        int incidente = await DatosSst.InsertarIncidenteAsync(_fabrica, 1, "AccidenteSinBaja", hoy.AddDays(-3));
        int otro = await DatosSst.InsertarIncidenteAsync(_fabrica, 2, "AccidenteSinBaja", hoy.AddDays(-3));
        int ajena = await DatosSst.InsertarAccionAsync(_fabrica, otro, false);
        string invalida = "La acción correctiva necesita descripción (máximo 500), responsable (máximo 150) y fecha de compromiso.";
        string ruta = $"{RutaBase}/{incidente}/acciones";
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Accion(null, "Jefe", hoy)), 400, invalida);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Accion("  ", "Jefe", hoy)), 400, invalida);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Accion(new string('D', 501), "Jefe", hoy)), 400, invalida);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Accion("Señalizar", null, hoy)), 400, invalida);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Accion("Señalizar", new string('R', 151), hoy)), 400, invalida);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Accion("Señalizar", "Jefe", null)), 400, invalida);
        Assert.Single(await DatosSst.LeerAccionesAsync(_fabrica));
        HttpResponseMessage creada = await _cliente.PostAsJsonAsync(ruta, Accion(" Señalizar la zona ", " Jefe de planta ", hoy.AddDays(7)));
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        AccionCorrectivaSstIdDto? id = await creada.Content.ReadFromJsonAsync<AccionCorrectivaSstIdDto>();
        Assert.NotNull(id);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(ruta, Accion(new string('D', 500), new string('R', 150), hoy))).StatusCode);
        string rutaCompletar = $"{ruta}/{id.Id}/completar";
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(rutaCompletar, Completar(null)), 400, "La fecha es obligatoria y no puede ser posterior a hoy.");
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(rutaCompletar, Completar(hoy.AddDays(1))), 400, "La fecha es obligatoria y no puede ser posterior a hoy.");
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{ruta}/9999/completar", Completar(hoy)), 404, "La acción correctiva 9999 no existe en el incidente.");
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{ruta}/{ajena}/completar", Completar(hoy)), 404, $"La acción correctiva {ajena} no existe en el incidente.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(rutaCompletar, Completar(hoy))).StatusCode);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(rutaCompletar, Completar(hoy)), 409, $"La acción correctiva {id.Id} ya está completada.");
        AccionCorrectivaSst completada = (await DatosSst.LeerAccionesAsync(_fabrica)).Single(accion => accion.Id == id.Id);
        Assert.True(completada.Completada);
        Assert.Equal(hoy, completada.FechaCompletada);
        Assert.Equal("Señalizar la zona", completada.Descripcion);
        Assert.Equal("Jefe de planta", completada.Responsable);
        IncidenteSstDetalleDto? detalle = await _cliente.GetFromJsonAsync<IncidenteSstDetalleDto>($"{RutaBase}/{incidente}");
        Assert.NotNull(detalle);
        Assert.Equal(2, detalle.Acciones.Count);
        Assert.Equal(new[] { true, false }, detalle.Acciones.Select(accion => accion.Completada).ToArray());
        Assert.Equal("Torres Vega, Ana", detalle.Empleado);
    }

    // Mensajes 31 y 32 y el cierre completo (S5).
    [Fact]
    public async Task CierreExigeAccionesYTodasCompletadas()
    {
        int sinAcciones = await DatosSst.InsertarIncidenteAsync(_fabrica, 1, "AccidenteSinBaja", DatosSst.Hoy.AddDays(-3));
        string numeroSin = "INC-" + sinAcciones.ToString("D6");
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{sinAcciones}/cierre", null), 409, $"El incidente {numeroSin} necesita al menos una acción correctiva para cerrarse.");
        int incidente = await DatosSst.InsertarIncidenteAsync(_fabrica, 2, "AccidenteConBaja", DatosSst.Hoy.AddDays(-3), "Abierto", 4);
        string numero = "INC-" + incidente.ToString("D6");
        int completada = await DatosSst.InsertarAccionAsync(_fabrica, incidente, true);
        int pendiente = await DatosSst.InsertarAccionAsync(_fabrica, incidente, false);
        int otraPendiente = await DatosSst.InsertarAccionAsync(_fabrica, incidente, false);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{incidente}/cierre", null), 409, $"El incidente {numero} tiene 2 acciones correctivas sin completar.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{RutaBase}/{incidente}/acciones/{pendiente}/completar", Completar(DatosSst.Hoy))).StatusCode);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{incidente}/cierre", null), 409, $"El incidente {numero} tiene 1 acciones correctivas sin completar.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{RutaBase}/{incidente}/acciones/{otraPendiente}/completar", Completar(DatosSst.Hoy))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{incidente}/cierre", null)).StatusCode);
        IncidenteSst cerrado = (await DatosSst.LeerIncidentesAsync(_fabrica)).Single(item => item.Id == incidente);
        Assert.Equal("Cerrado", cerrado.Estado);
        Assert.Equal(DatosSst.Hoy, cerrado.FechaCierre);
        Assert.NotEqual(0, completada);
    }

    // T3 y mensaje 27: un incidente cerrado no admite acciones, cambios ni un segundo cierre.
    [Fact]
    public async Task IncidenteCerradoNoAdmiteCambiosConMensaje27()
    {
        int incidente = await DatosSst.InsertarIncidenteAsync(_fabrica, 1, "AccidenteSinBaja", DatosSst.Hoy.AddDays(-3), "Cerrado");
        int accion = await DatosSst.InsertarAccionAsync(_fabrica, incidente, true);
        string mensaje = $"El incidente INC-{incidente:D6} está cerrado y no admite cambios.";
        string[] antes = await DatosSst.LeerFirmasAsync(_fabrica);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/{incidente}/acciones", Accion("Nueva", "Jefe", DatosSst.Hoy)), 409, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/{incidente}/acciones/{accion}/completar", Completar(DatosSst.Hoy)), 409, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{incidente}/cierre", null), 409, mensaje);
        Assert.Equal(antes, await DatosSst.LeerFirmasAsync(_fabrica));
    }

    [Fact]
    public async Task ListadoValidaPaginacionYFiltrosYAplicaBusquedaTipoSeveridadEstadoYFechas()
    {
        DateOnly hoy = DatosSst.Hoy;
        int primero = await DatosSst.InsertarIncidenteAsync(_fabrica, 1, "AccidenteSinBaja", hoy.AddDays(-30));
        int segundo = await DatosSst.InsertarIncidenteAsync(_fabrica, 2, "AccidenteConBaja", hoy.AddDays(-20), "Abierto", 3);
        int tercero = await DatosSst.InsertarIncidenteAsync(_fabrica, 3, "EnfermedadOcupacional", hoy.AddDays(-10), "Cerrado");
        await DatosSst.InsertarAccionAsync(_fabrica, segundo, false);
        await DatosSst.InsertarAccionAsync(_fabrica, segundo, true);
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tipo=Otro"), 400, "El filtro tipo no es válido.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?severidad=Otra"), 400, "El filtro severidad no es válido.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?estado=Otro"), 400, "El filtro estado no es válido.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?desde={DatosSst.Fecha(hoy)}&hasta={DatosSst.Fecha(hoy.AddDays(-1))}"), 400, "El filtro hasta no es válido.");
        PaginaIncidentesSstDto todos = await LeerPaginaAsync(RutaBase);
        Assert.Equal(3, todos.Total);
        Assert.Equal(new[] { tercero, segundo, primero }, todos.Items.Select(item => item.Id).ToArray());
        IncidenteSstListadoDto conAcciones = todos.Items[1];
        Assert.Equal(2, conAcciones.Acciones);
        Assert.Equal(1, conAcciones.AccionesPendientes);
        Assert.Equal(3, conAcciones.DiasBaja);
        Assert.Equal("Rojas Mendoza, Luis", conAcciones.Empleado);
        Assert.Equal(new[] { primero }, (await LeerPaginaAsync($"{RutaBase}?pagina=2&tamanoPagina=2")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { segundo }, (await LeerPaginaAsync($"{RutaBase}?busqueda=inc-{segundo:D6}")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(3, (await LeerPaginaAsync($"{RutaBase}?busqueda=planta")).Total);
        Assert.Equal(new[] { segundo }, (await LeerPaginaAsync($"{RutaBase}?tipo=AccidenteConBaja")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(3, (await LeerPaginaAsync($"{RutaBase}?severidad=Leve")).Total);
        Assert.Empty((await LeerPaginaAsync($"{RutaBase}?severidad=Mortal")).Items);
        Assert.Equal(new[] { tercero }, (await LeerPaginaAsync($"{RutaBase}?estado=Cerrado")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { segundo, primero }, (await LeerPaginaAsync($"{RutaBase}?estado=Abierto")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { segundo, primero }, (await LeerPaginaAsync($"{RutaBase}?hasta={DatosSst.Fecha(hoy.AddDays(-20))}")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { segundo }, (await LeerPaginaAsync($"{RutaBase}?desde={DatosSst.Fecha(hoy.AddDays(-20))}&hasta={DatosSst.Fecha(hoy.AddDays(-20))}")).Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        DateOnly hoy = DatosSst.Hoy;
        int incidente = await DatosSst.InsertarIncidenteAsync(_fabrica, 1, "AccidenteSinBaja", hoy.AddDays(-3));
        int accion = await DatosSst.InsertarAccionAsync(_fabrica, incidente, false);
        RegistrarIncidenteSstRequestDto registro = Solicitud(1, "AccidenteSinBaja", "Leve", hoy, "Planta", "Caída", 0);
        GuardarAccionCorrectivaRequestDto nueva = Accion("Señalizar", "Jefe", hoy);
        HttpClient anonimo = DatosSst.ClienteAnonimo(_fabrica);
        string sesion = DatosSst.MensajeSinSesion;
        await DatosSst.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, sesion);
        await DatosSst.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/{incidente}"), 401, sesion);
        await DatosSst.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/resumen"), 401, sesion);
        await DatosSst.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, registro), 401, sesion);
        await DatosSst.AssertErrorAsync(await anonimo.PostAsync($"{RutaBase}/{incidente}/cierre", null), 401, sesion);
        HttpClient consulta = DatosSst.ClienteConRoles(_fabrica, "CONSULTA");
        string[] antes = await DatosSst.LeerFirmasAsync(_fabrica);
        string permiso = DatosSst.MensajeSinPermiso;
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/{incidente}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/resumen")).StatusCode);
        await DatosSst.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, registro), 403, permiso);
        await DatosSst.AssertErrorAsync(await consulta.PostAsJsonAsync($"{RutaBase}/{incidente}/acciones", nueva), 403, permiso);
        await DatosSst.AssertErrorAsync(await consulta.PostAsJsonAsync($"{RutaBase}/{incidente}/acciones/{accion}/completar", Completar(hoy)), 403, permiso);
        await DatosSst.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{incidente}/cierre", null), 403, permiso);
        Assert.Equal(antes, await DatosSst.LeerFirmasAsync(_fabrica));
        foreach (string rol in new[] { "RRHH", "ADMIN" })
        {
            HttpClient cliente = DatosSst.ClienteConRoles(_fabrica, rol);
            Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(RutaBase, registro)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync($"{RutaBase}/{incidente}/acciones", nueva)).StatusCode);
        }
    }

    [Fact]
    public async Task IncidenteYAccionGeneranRegistrosDeAuditoria()
    {
        DateOnly hoy = DatosSst.Hoy;
        HttpResponseMessage creado = await _cliente.PostAsJsonAsync(RutaBase, Solicitud(1, "AccidenteSinBaja", "Leve", hoy, "Planta", "Caída", 0));
        IncidenteSstCreadoDto? id = await creado.Content.ReadFromJsonAsync<IncidenteSstCreadoDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosSst.LeerAuditoriaAsync(_fabrica, "IncidenteSst", id.Id), registro => registro.Accion == "Alta");
        HttpResponseMessage accion = await _cliente.PostAsJsonAsync($"{RutaBase}/{id.Id}/acciones", Accion("Señalizar", "Jefe", hoy));
        AccionCorrectivaSstIdDto? idAccion = await accion.Content.ReadFromJsonAsync<AccionCorrectivaSstIdDto>();
        Assert.NotNull(idAccion);
        Assert.Single(await DatosSst.LeerAuditoriaAsync(_fabrica, "AccionCorrectivaSst", idAccion.Id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{RutaBase}/{id.Id}/acciones/{idAccion.Id}/completar", Completar(hoy))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id.Id}/cierre", null)).StatusCode);
        Assert.Contains(await DatosSst.LeerAuditoriaAsync(_fabrica, "IncidenteSst", id.Id), registro => registro.Accion == "Modificacion");
    }

    private static RegistrarIncidenteSstRequestDto Solicitud(
        int? empleadoId,
        string? tipo,
        string? severidad,
        DateOnly? fecha,
        string? lugar,
        string? descripcion,
        int? diasBaja)
    {
        return new RegistrarIncidenteSstRequestDto
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Severidad = severidad,
            Fecha = fecha,
            Lugar = lugar,
            Descripcion = descripcion,
            DiasBaja = diasBaja
        };
    }

    private static GuardarAccionCorrectivaRequestDto Accion(string? descripcion, string? responsable, DateOnly? fechaCompromiso)
    {
        return new GuardarAccionCorrectivaRequestDto
        {
            Descripcion = descripcion,
            Responsable = responsable,
            FechaCompromiso = fechaCompromiso
        };
    }

    private static CompletarAccionCorrectivaRequestDto Completar(DateOnly? fecha)
    {
        return new CompletarAccionCorrectivaRequestDto { FechaCompletada = fecha };
    }

    private async Task AssertRegistrarAsync(RegistrarIncidenteSstRequestDto solicitud, int codigo, string mensaje)
    {
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), codigo, mensaje);
    }

    private async Task<PaginaIncidentesSstDto> LeerPaginaAsync(string ruta)
    {
        PaginaIncidentesSstDto? pagina = await _cliente.GetFromJsonAsync<PaginaIncidentesSstDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }
}
