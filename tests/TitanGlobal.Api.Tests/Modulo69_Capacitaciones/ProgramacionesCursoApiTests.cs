using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo69_Capacitaciones;

[Collection("Modulo04")]
public class ProgramacionesCursoApiTests
{
    private const string RutaBase = "/api/v1/programaciones-curso";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public ProgramacionesCursoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosCapacitacion.Restablecer(fabrica);
    }

    // Mensajes 10 a 13, 7 y 8 en el orden de la spec.
    [Fact]
    public async Task CreacionConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        int inactivo = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "INA", 8m, false);
        DateOnly inicio = DatosCapacitacion.Hoy.AddDays(10);
        DateOnly fin = inicio.AddDays(2);
        string fechas = "Las fechas de inicio y fin son obligatorias.";
        string orden = "La fecha de fin no puede ser anterior a la de inicio.";
        string cupo = "El cupo debe estar entre 1 y 500.";
        await AssertCrearAsync(Solicitud(curso, null, fin, 5, null), 400, fechas);
        await AssertCrearAsync(Solicitud(curso, inicio, null, 5, null), 400, fechas);
        await AssertCrearAsync(Solicitud(curso, fin, inicio, 5, null), 400, orden);
        await AssertCrearAsync(Solicitud(curso, inicio, fin, null, null), 400, cupo);
        await AssertCrearAsync(Solicitud(curso, inicio, fin, 0, null), 400, cupo);
        await AssertCrearAsync(Solicitud(curso, inicio, fin, 501, null), 400, cupo);
        await AssertCrearAsync(Solicitud(curso, inicio, fin, 5, new string('L', 151)), 400, "El lugar no puede superar 150 caracteres.");
        await AssertCrearAsync(Solicitud(9999, inicio, fin, 5, null), 404, "El curso 9999 no existe.");
        await AssertCrearAsync(Solicitud(null, inicio, fin, 5, null), 404, "El curso 0 no existe.");
        await AssertCrearAsync(Solicitud(inactivo, inicio, fin, 5, null), 409, "El curso INA está inactivo y no admite programaciones.");
        Assert.Empty(await DatosCapacitacion.LeerProgramacionesAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud(curso, inicio, inicio, 1, new string('L', 150)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud(curso, inicio, fin, 500, null))).StatusCode);
    }

    [Fact]
    public async Task CreacionValidaDejaLaProgramacionProgramadaConElAnioDeInicio()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly inicio = new DateOnly(DatosCapacitacion.Hoy.Year + 1, 12, 30);
        DateOnly fin = new DateOnly(DatosCapacitacion.Hoy.Year + 2, 1, 2);
        HttpResponseMessage respuesta = await CrearAsync(Solicitud(curso, inicio, fin, 4, " Sala 1 "));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ProgramacionIdDto? id = await respuesta.Content.ReadFromJsonAsync<ProgramacionIdDto>();
        Assert.NotNull(id);
        ProgramacionCurso guardada = Assert.Single(await DatosCapacitacion.LeerProgramacionesAsync(_fabrica));
        Assert.Equal(inicio.Year, guardada.Anio);
        Assert.Equal("Programada", guardada.Estado);
        Assert.Equal(4, guardada.Cupo);
        Assert.Equal("Sala 1", guardada.Lugar);
        ProgramacionDetalleDto detalle = await LeerDetalleAsync(id.Id);
        Assert.Equal("CUR", detalle.CursoCodigo);
        Assert.Equal(4, detalle.CupoLibre);
        Assert.Equal(0, detalle.Inscritos);
        Assert.Empty(detalle.Inscripciones);
    }

    [Fact]
    public async Task ListadoValidaPaginacionYFiltrosYMuestraCupoLibre()
    {
        int primero = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "UNO", 8m);
        int segundo = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "DOS", 16m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int futura = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, primero, new DateOnly(2090, 3, 1), new DateOnly(2090, 3, 2), 3, "Programada");
        await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, segundo, new DateOnly(2089, 3, 1), new DateOnly(2089, 3, 2), 3, "Cancelada");
        await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, segundo, hoy.AddDays(-10), hoy.AddDays(-9), 3, "Realizada");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, futura, 1, "Inscrito");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, futura, 2, "Retirado");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?estado=Otro"), 400, "El filtro estado no es válido.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?cursoId=9999"), 404, "El curso 9999 no existe.");
        PaginaProgramacionesDto todas = await LeerPaginaAsync(RutaBase);
        Assert.Equal(3, todas.Total);
        Assert.Equal(futura, todas.Items[0].Id);
        Assert.Equal(1, todas.Items[0].Inscritos);
        Assert.Equal(2, todas.Items[0].CupoLibre);
        Assert.Equal("UNO", todas.Items[0].CursoCodigo);
        Assert.Equal(2090, (await LeerPaginaAsync($"{RutaBase}?anio=2090")).Items.Single().Anio);
        Assert.Equal(2, (await LeerPaginaAsync($"{RutaBase}?cursoId={segundo}")).Total);
        Assert.Equal("Cancelada", (await LeerPaginaAsync($"{RutaBase}?estado=Cancelada")).Items.Single().Estado);
        PaginaProgramacionesDto segunda = await LeerPaginaAsync($"{RutaBase}?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Single(segunda.Items);
    }

    // Mensaje 14 en todas las rutas con id de programación.
    [Fact]
    public async Task ProgramacionInexistenteDevuelve404ConMensaje14EnTodasLasRutas()
    {
        string mensaje = "La programación 9999 no existe.";
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999"), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/9999/inscripciones", new InscribirEmpleadoRequestDto { EmpleadoId = 1 }), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/inscripciones/1/retiro", null), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/realizacion", null), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/cancelacion", null), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/9999/resultados", new RegistrarResultadosRequestDto()), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/9999/inscripciones/1/certificacion", new EmitirCertificacionRequestDto()), 404, mensaje);
    }

    // Mensajes 15, 16, 17, 18 y 19 en el orden de la spec.
    [Fact]
    public async Task InscripcionConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int abierta = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(10), hoy.AddDays(11), 1, "Programada");
        int realizada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), hoy.AddDays(-9), 5, "Realizada");
        int cancelada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(20), hoy.AddDays(21), 5, "Cancelada");
        await AssertInscribirAsync(realizada, 1, 409, $"La programación {realizada} no está programada.");
        await AssertInscribirAsync(cancelada, 1, 409, $"La programación {cancelada} no está programada.");
        await AssertInscribirAsync(abierta, 9999, 404, "El empleado 9999 no existe.");
        await AssertInscribirAsync(abierta, null, 404, "El empleado 0 no existe.");
        await AssertInscribirAsync(abierta, 4, 409, "El empleado 4 está cesado y no se puede inscribir.");
        Assert.Empty(await DatosCapacitacion.LeerInscripcionesAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await InscribirAsync(abierta, 1)).StatusCode);
        await AssertInscribirAsync(abierta, 1, 409, "El empleado 1 ya está inscrito en esta programación.");
        await AssertInscribirAsync(abierta, 2, 409, $"La programación {abierta} no tiene cupo libre.");
        Assert.Single(await DatosCapacitacion.LeerInscripcionesAsync(_fabrica));
    }

    // K2: el cupo cuenta las inscripciones no retiradas; el siguiente es 409 y tras un retiro vuelve a haber lugar.
    [Fact]
    public async Task CupoExactoRechazaAlSiguienteYUnRetiroLiberaUnLugar()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        int programacion = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, DatosCapacitacion.Hoy.AddDays(10), DatosCapacitacion.Hoy.AddDays(11), 2, "Programada");
        HttpResponseMessage primera = await InscribirAsync(programacion, 1);
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        InscripcionIdDto? idPrimera = await primera.Content.ReadFromJsonAsync<InscripcionIdDto>();
        Assert.NotNull(idPrimera);
        Assert.Equal(HttpStatusCode.OK, (await InscribirAsync(programacion, 2)).StatusCode);
        Assert.Equal(0, (await LeerDetalleAsync(programacion)).CupoLibre);
        await AssertInscribirAsync(programacion, 3, 409, $"La programación {programacion} no tiene cupo libre.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{programacion}/inscripciones/{idPrimera.Id}/retiro", null)).StatusCode);
        ProgramacionDetalleDto tras = await LeerDetalleAsync(programacion);
        Assert.Equal(1, tras.CupoLibre);
        Assert.Equal(1, tras.Inscritos);
        Assert.Equal(HttpStatusCode.OK, (await InscribirAsync(programacion, 3)).StatusCode);
        await AssertInscribirAsync(programacion, 1, 409, $"La programación {programacion} no tiene cupo libre.");
        Assert.Equal(0, (await LeerDetalleAsync(programacion)).CupoLibre);
    }

    // Un empleado retirado puede volver a inscribirse: se reactiva su inscripción (no duplica la fila).
    [Fact]
    public async Task UnEmpleadoRetiradoSePuedeReinscribirReactivandoSuInscripcion()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        int programacion = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, DatosCapacitacion.Hoy.AddDays(10), DatosCapacitacion.Hoy.AddDays(11), 2, "Programada");
        int retirada = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 1, "Retirado");
        HttpResponseMessage respuesta = await InscribirAsync(programacion, 1);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        InscripcionIdDto? id = await respuesta.Content.ReadFromJsonAsync<InscripcionIdDto>();
        Assert.NotNull(id);
        Assert.Equal(retirada, id.Id);
        InscripcionCurso inscripcion = Assert.Single(await DatosCapacitacion.LeerInscripcionesAsync(_fabrica));
        Assert.Equal("Inscrito", inscripcion.Estado);
        Assert.Equal(DatosCapacitacion.Hoy, inscripcion.FechaInscripcion);
    }

    // Mensajes 20, 15 y 21.
    [Fact]
    public async Task RetiroValidaInscripcionProgramacionYFechaDeInicio()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int futura = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(10), hoy.AddDays(11), 5, "Programada");
        int otra = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(10), hoy.AddDays(11), 5, "Programada");
        int iniciada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy, hoy.AddDays(1), 5, "Programada");
        int realizada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), hoy.AddDays(-9), 5, "Realizada");
        int enFutura = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, futura, 1, "Inscrito");
        int enOtra = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, otra, 2, "Inscrito");
        int enIniciada = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, iniciada, 1, "Inscrito");
        int enRealizada = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, realizada, 1, "Aprobado", 15m);
        string antesDelInicio = "Solo se puede retirar una inscripción antes del inicio del curso.";
        await DatosCapacitacion.AssertErrorAsync(await Retirar(futura, 9999), 404, "La inscripción 9999 no existe en esta programación.");
        await DatosCapacitacion.AssertErrorAsync(await Retirar(futura, enOtra), 404, $"La inscripción {enOtra} no existe en esta programación.");
        await DatosCapacitacion.AssertErrorAsync(await Retirar(realizada, enRealizada), 409, $"La programación {realizada} no está programada.");
        await DatosCapacitacion.AssertErrorAsync(await Retirar(iniciada, enIniciada), 409, antesDelInicio);
        Assert.Equal(HttpStatusCode.OK, (await Retirar(futura, enFutura)).StatusCode);
        await DatosCapacitacion.AssertErrorAsync(await Retirar(futura, enFutura), 409, antesDelInicio);
        Assert.Equal("Retirado", (await DatosCapacitacion.LeerInscripcionesAsync(_fabrica)).Single(inscripcion => inscripcion.Id == enFutura).Estado);
        Assert.Equal("Inscrito", (await DatosCapacitacion.LeerInscripcionesAsync(_fabrica)).Single(inscripcion => inscripcion.Id == enIniciada).Estado);
    }

    // Mensajes 15 y 22; con FechaFin igual a hoy ya se puede marcar como realizada.
    [Fact]
    public async Task RealizacionExigeProgramadaYFinAnteriorOIgualAHoy()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        DateOnly fin = hoy.AddDays(1);
        int enCurso = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-3), fin, 5, "Programada");
        int terminaHoy = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-3), hoy, 5, "Programada");
        int terminada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-9), hoy.AddDays(-8), 5, "Programada");
        int cancelada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-9), hoy.AddDays(-8), 5, "Cancelada");
        await DatosCapacitacion.AssertErrorAsync(
            await _cliente.PostAsync($"{RutaBase}/{enCurso}/realizacion", null),
            409,
            $"La programación {enCurso} termina el {DatosCapacitacion.Fecha(fin)} y aún no se puede marcar como realizada.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{cancelada}/realizacion", null), 409, $"La programación {cancelada} no está programada.");
        Assert.Equal("Programada", (await DatosCapacitacion.LeerProgramacionesAsync(_fabrica)).Single(programacion => programacion.Id == enCurso).Estado);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{terminaHoy}/realizacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{terminada}/realizacion", null)).StatusCode);
        Assert.Equal("Realizada", (await DatosCapacitacion.LeerProgramacionesAsync(_fabrica)).Single(programacion => programacion.Id == terminada).Estado);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{terminada}/realizacion", null), 409, $"La programación {terminada} no está programada.");
    }

    [Fact]
    public async Task CancelacionRetiraLasInscripcionesYSoloAplicaALasProgramadas()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int programacion = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(10), hoy.AddDays(11), 5, "Programada");
        int realizada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), hoy.AddDays(-9), 5, "Realizada");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 1, "Inscrito");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 2, "Inscrito");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{realizada}/cancelacion", null), 409, $"La programación {realizada} no está programada.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{programacion}/cancelacion", null)).StatusCode);
        Assert.Equal("Cancelada", (await DatosCapacitacion.LeerProgramacionesAsync(_fabrica)).Single(item => item.Id == programacion).Estado);
        Assert.All(await DatosCapacitacion.LeerInscripcionesAsync(_fabrica), inscripcion => Assert.Equal("Retirado", inscripcion.Estado));
        ProgramacionDetalleDto detalle = await LeerDetalleAsync(programacion);
        Assert.Equal(0, detalle.Inscritos);
        Assert.Equal(5, detalle.CupoLibre);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{programacion}/cancelacion", null), 409, $"La programación {programacion} no está programada.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{programacion}/realizacion", null), 409, $"La programación {programacion} no está programada.");
        await AssertInscribirAsync(programacion, 3, 409, $"La programación {programacion} no está programada.");
    }

    // Mensajes 23, 20, 24 y 26; un resultado inválido no deja cambios parciales.
    [Fact]
    public async Task ResultadosValidanProgramacionInscripcionYNotaYSonAtomicos()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int programada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(10), hoy.AddDays(11), 5, "Programada");
        int realizada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), hoy.AddDays(-9), 5, "Realizada");
        int otra = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), hoy.AddDays(-9), 5, "Realizada");
        int uno = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, realizada, 1, "Inscrito");
        int dos = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, realizada, 2, "Inscrito");
        int retirado = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, realizada, 3, "Retirado");
        int ajena = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, otra, 1, "Inscrito");
        int certificado = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, otra, 2, "Aprobado", 16m);
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, certificado, "CERT-X", hoy.AddDays(-5), null);
        await DatosCapacitacion.AssertErrorAsync(await Resultados(programada, Resultado(uno, true, 15m)), 409, $"La programación {programada} debe estar realizada para registrar resultados.");
        string[] antes = await DatosCapacitacion.LeerFirmasAsync(_fabrica);
        string nota = "La nota debe estar entre 0 y 20, con un decimal.";
        await DatosCapacitacion.AssertErrorAsync(await Resultados(realizada, Resultado(uno, true, 15m), Resultado(9999, true, 10m)), 404, "La inscripción 9999 no existe en esta programación.");
        await DatosCapacitacion.AssertErrorAsync(await Resultados(realizada, Resultado(uno, true, 15m), Resultado(ajena, true, 10m)), 404, $"La inscripción {ajena} no existe en esta programación.");
        await DatosCapacitacion.AssertErrorAsync(await Resultados(realizada, Resultado(retirado, true, 10m)), 404, $"La inscripción {retirado} no existe en esta programación.");
        await DatosCapacitacion.AssertErrorAsync(await Resultados(realizada, Resultado(uno, true, 15m), Resultado(dos, true, -0.1m)), 400, nota);
        await DatosCapacitacion.AssertErrorAsync(await Resultados(realizada, Resultado(uno, true, 20.1m)), 400, nota);
        await DatosCapacitacion.AssertErrorAsync(await Resultados(realizada, Resultado(uno, true, 15.55m)), 400, nota);
        await DatosCapacitacion.AssertErrorAsync(await Resultados(otra, Resultado(certificado, false, 5m)), 409, $"La inscripción {certificado} ya tiene certificación.");
        Assert.Equal(antes, await DatosCapacitacion.LeerFirmasAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await Resultados(realizada, Resultado(uno, true, 20m), Resultado(dos, false, 8.5m))).StatusCode);
        List<InscripcionCurso> guardadas = await DatosCapacitacion.LeerInscripcionesAsync(_fabrica);
        InscripcionCurso aprobada = guardadas.Single(inscripcion => inscripcion.Id == uno);
        InscripcionCurso desaprobada = guardadas.Single(inscripcion => inscripcion.Id == dos);
        Assert.Equal("Aprobado", aprobada.Estado);
        Assert.Equal(20m, aprobada.Nota);
        Assert.Equal("Desaprobado", desaprobada.Estado);
        Assert.Equal(8.5m, desaprobada.Nota);
        Assert.Equal("Retirado", guardadas.Single(inscripcion => inscripcion.Id == retirado).Estado);
        Assert.Equal(HttpStatusCode.OK, (await Resultados(realizada, Resultado(dos, true, 14m))).StatusCode);
        Assert.Equal("Aprobado", (await DatosCapacitacion.LeerInscripcionesAsync(_fabrica)).Single(inscripcion => inscripcion.Id == dos).Estado);
    }

    // Mensajes 20, 25, 26, 27, 28, 29 y 30 en el orden de la spec.
    [Fact]
    public async Task CertificacionValidaEstadoFechasYCodigoUnicoConMensajesLiterales()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        DateOnly fin = hoy.AddDays(-9);
        int programacion = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), fin, 5, "Realizada");
        int otra = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), fin, 5, "Realizada");
        int aprobado = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 1, "Aprobado", 15m);
        int segundoAprobado = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 2, "Aprobado", 17m);
        int desaprobado = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 3, "Desaprobado", 8m);
        int ajena = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, otra, 1, "Aprobado", 15m);
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, 9999, "C1", fin, null), 404, "La inscripción 9999 no existe en esta programación.");
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, ajena, "C1", fin, null), 404, $"La inscripción {ajena} no existe en esta programación.");
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, desaprobado, "C1", fin, null), 409, $"La inscripción {desaprobado} no está aprobada y no admite certificación.");
        string codigo = "El código de certificación es obligatorio y no puede superar 40 caracteres.";
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, aprobado, null, fin, null), 400, codigo);
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, aprobado, "   ", fin, null), 400, codigo);
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, aprobado, new string('C', 41), fin, null), 400, codigo);
        string emision = "La fecha de emisión es obligatoria y no puede ser anterior al fin del curso.";
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, aprobado, "C1", null, null), 400, emision);
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, aprobado, "C1", fin.AddDays(-1), null), 400, emision);
        await DatosCapacitacion.AssertErrorAsync(
            await Certificar(programacion, aprobado, "C1", fin.AddDays(1), fin),
            400,
            "La fecha de vencimiento no puede ser anterior a la de emisión.");
        Assert.Empty(await DatosCapacitacion.LeerCertificacionesAsync(_fabrica));
        HttpResponseMessage emitida = await Certificar(programacion, aprobado, " cert-001 ", fin, fin.AddDays(365));
        Assert.Equal(HttpStatusCode.OK, emitida.StatusCode);
        CertificacionIdDto? id = await emitida.Content.ReadFromJsonAsync<CertificacionIdDto>();
        Assert.NotNull(id);
        CertificacionCurso guardada = Assert.Single(await DatosCapacitacion.LeerCertificacionesAsync(_fabrica));
        Assert.Equal("CERT-001", guardada.Codigo);
        Assert.Equal(aprobado, guardada.InscripcionCursoId);
        Assert.Equal(fin.AddDays(365), guardada.FechaVencimiento);
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, aprobado, "OTRO", fin, null), 409, $"La inscripción {aprobado} ya tiene certificación.");
        await DatosCapacitacion.AssertErrorAsync(await Certificar(programacion, segundoAprobado, "cert-001", fin, null), 409, "Ya existe una certificación con el código CERT-001.");
        Assert.Single(await DatosCapacitacion.LeerCertificacionesAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await Certificar(programacion, segundoAprobado, "CERT-002", fin, null)).StatusCode);
    }

    // K4: la certificación se muestra vencida solo cuando su vencimiento es anterior a hoy.
    [Fact]
    public async Task DetalleCalculaVencidaAlLeerYMuestraInscritosYCertificaciones()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int programacion = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-400), hoy.AddDays(-399), 5, "Realizada");
        int uno = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 1, "Aprobado", 18m);
        int dos = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 2, "Aprobado", 17m);
        int tres = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 3, "Aprobado", 16m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, programacion, 4, "Retirado");
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, uno, "VENCIDA", hoy.AddDays(-399), hoy.AddDays(-1));
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, dos, "HOY", hoy.AddDays(-399), hoy);
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, tres, "SINVENCE", hoy.AddDays(-399), null);
        ProgramacionDetalleDto detalle = await LeerDetalleAsync(programacion);
        Assert.Equal(3, detalle.Inscritos);
        Assert.Equal(2, detalle.CupoLibre);
        Assert.Equal(4, detalle.Inscripciones.Count);
        Assert.Equal("Torres Vega, Ana", detalle.Inscripciones[0].EmpleadoNombre);
        Assert.True(detalle.Inscripciones[0].Certificacion!.Vencida);
        Assert.False(detalle.Inscripciones[1].Certificacion!.Vencida);
        Assert.False(detalle.Inscripciones[2].Certificacion!.Vencida);
        Assert.Null(detalle.Inscripciones[3].Certificacion);
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int id = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(10), hoy.AddDays(11), 5, "Programada");
        GuardarProgramacionRequestDto solicitud = Solicitud(curso, hoy.AddDays(10), hoy.AddDays(11), 5, null);
        HttpClient anonimo = DatosCapacitacion.ClienteAnonimo(_fabrica);
        string sesion = DatosCapacitacion.MensajeSinSesion;
        await DatosCapacitacion.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/{id}"), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, solicitud), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.PostAsync($"{RutaBase}/{id}/cancelacion", null), 401, sesion);
        HttpClient consulta = DatosCapacitacion.ClienteConRoles(_fabrica, "CONSULTA");
        string[] antes = await DatosCapacitacion.LeerFirmasAsync(_fabrica);
        string permiso = DatosCapacitacion.MensajeSinPermiso;
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/{id}")).StatusCode);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, solicitud), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsJsonAsync($"{RutaBase}/{id}/inscripciones", new InscribirEmpleadoRequestDto { EmpleadoId = 1 }), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/inscripciones/1/retiro", null), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/realizacion", null), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/cancelacion", null), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsJsonAsync($"{RutaBase}/{id}/resultados", new RegistrarResultadosRequestDto()), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsJsonAsync($"{RutaBase}/{id}/inscripciones/1/certificacion", new EmitirCertificacionRequestDto()), 403, permiso);
        Assert.Equal(antes, await DatosCapacitacion.LeerFirmasAsync(_fabrica));
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenProgramarEInscribir(string rol)
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        HttpClient cliente = DatosCapacitacion.ClienteConRoles(_fabrica, rol);
        DateOnly hoy = DatosCapacitacion.Hoy;
        HttpResponseMessage creada = await cliente.PostAsJsonAsync(RutaBase, Solicitud(curso, hoy.AddDays(10), hoy.AddDays(11), 5, null));
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        ProgramacionIdDto? id = await creada.Content.ReadFromJsonAsync<ProgramacionIdDto>();
        Assert.NotNull(id);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync($"{RutaBase}/{id.Id}/inscripciones", new InscribirEmpleadoRequestDto { EmpleadoId = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/cancelacion", null)).StatusCode);
    }

    [Fact]
    public async Task ProgramacionInscripcionYCertificacionGeneranRegistrosDeAuditoria()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CUR", 8m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        HttpResponseMessage creada = await CrearAsync(Solicitud(curso, hoy.AddDays(10), hoy.AddDays(11), 5, null));
        ProgramacionIdDto? id = await creada.Content.ReadFromJsonAsync<ProgramacionIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosCapacitacion.LeerAuditoriaAsync(_fabrica, "ProgramacionCurso", id.Id), registro => registro.Accion == "Alta");
        HttpResponseMessage inscrita = await InscribirAsync(id.Id, 1);
        InscripcionIdDto? inscripcion = await inscrita.Content.ReadFromJsonAsync<InscripcionIdDto>();
        Assert.NotNull(inscripcion);
        Assert.Single(await DatosCapacitacion.LeerAuditoriaAsync(_fabrica, "InscripcionCurso", inscripcion.Id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.OK, (await Retirar(id.Id, inscripcion.Id)).StatusCode);
        Assert.Single(await DatosCapacitacion.LeerAuditoriaAsync(_fabrica, "InscripcionCurso", inscripcion.Id), registro => registro.Accion == "Modificacion");
        int terminada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-10), hoy.AddDays(-9), 5, "Realizada");
        int aprobada = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, terminada, 2, "Aprobado", 15m);
        HttpResponseMessage certificada = await Certificar(terminada, aprobada, "AUD-1", hoy.AddDays(-9), null);
        CertificacionIdDto? certificacion = await certificada.Content.ReadFromJsonAsync<CertificacionIdDto>();
        Assert.NotNull(certificacion);
        Assert.Single(await DatosCapacitacion.LeerAuditoriaAsync(_fabrica, "CertificacionCurso", certificacion.Id), registro => registro.Accion == "Alta");
    }

    private static GuardarProgramacionRequestDto Solicitud(int? cursoId, DateOnly? inicio, DateOnly? fin, int? cupo, string? lugar)
    {
        return new GuardarProgramacionRequestDto
        {
            CursoId = cursoId,
            FechaInicio = inicio,
            FechaFin = fin,
            Cupo = cupo,
            Lugar = lugar
        };
    }

    private static ResultadoInscripcionRequestDto Resultado(int inscripcionId, bool aprobado, decimal? nota)
    {
        return new ResultadoInscripcionRequestDto { InscripcionId = inscripcionId, Aprobado = aprobado, Nota = nota };
    }

    private async Task<HttpResponseMessage> CrearAsync(GuardarProgramacionRequestDto solicitud)
    {
        return await _cliente.PostAsJsonAsync(RutaBase, solicitud);
    }

    private async Task AssertCrearAsync(GuardarProgramacionRequestDto solicitud, int codigo, string mensaje)
    {
        await DatosCapacitacion.AssertErrorAsync(await CrearAsync(solicitud), codigo, mensaje);
    }

    private async Task<HttpResponseMessage> InscribirAsync(int programacionId, int? empleadoId)
    {
        return await _cliente.PostAsJsonAsync($"{RutaBase}/{programacionId}/inscripciones", new InscribirEmpleadoRequestDto { EmpleadoId = empleadoId });
    }

    private async Task AssertInscribirAsync(int programacionId, int? empleadoId, int codigo, string mensaje)
    {
        await DatosCapacitacion.AssertErrorAsync(await InscribirAsync(programacionId, empleadoId), codigo, mensaje);
    }

    private async Task<HttpResponseMessage> Retirar(int programacionId, int inscripcionId)
    {
        return await _cliente.PostAsync($"{RutaBase}/{programacionId}/inscripciones/{inscripcionId}/retiro", null);
    }

    private async Task<HttpResponseMessage> Resultados(int programacionId, params ResultadoInscripcionRequestDto[] resultados)
    {
        return await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{programacionId}/resultados",
            new RegistrarResultadosRequestDto { Resultados = resultados.ToList() });
    }

    private async Task<HttpResponseMessage> Certificar(int programacionId, int inscripcionId, string? codigo, DateOnly? emision, DateOnly? vencimiento)
    {
        return await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{programacionId}/inscripciones/{inscripcionId}/certificacion",
            new EmitirCertificacionRequestDto { Codigo = codigo, FechaEmision = emision, FechaVencimiento = vencimiento });
    }

    private async Task<ProgramacionDetalleDto> LeerDetalleAsync(int id)
    {
        ProgramacionDetalleDto? detalle = await _cliente.GetFromJsonAsync<ProgramacionDetalleDto>($"{RutaBase}/{id}");
        Assert.NotNull(detalle);
        return detalle;
    }

    private async Task<PaginaProgramacionesDto> LeerPaginaAsync(string ruta)
    {
        PaginaProgramacionesDto? pagina = await _cliente.GetFromJsonAsync<PaginaProgramacionesDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }
}
