using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo68_EvaluacionDesempeno;

[Collection("Modulo04")]
public class CiclosEvaluacionApiTests
{
    private const string RutaBase = "/api/v1/ciclos-evaluacion";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public CiclosEvaluacionApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosEvaluacion.Restablecer(fabrica);
    }

    // Mensajes 1 a 7 en el orden de la spec.
    [Fact]
    public async Task CreacionConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        DateOnly inicio = fin.AddDays(-179);
        string mensajeCodigo = "El código es obligatorio y no puede superar 20 caracteres.";
        string mensajeNombre = "El nombre es obligatorio y no puede superar 100 caracteres.";
        string mensajeFechas = "Las fechas del periodo, de apertura y de cierre son obligatorias.";
        await AssertCrearAsync(Solicitud(null, "Ciclo", "Semestral", inicio, fin, fin, fin.AddDays(5)), 400, mensajeCodigo);
        await AssertCrearAsync(Solicitud("   ", "Ciclo", "Semestral", inicio, fin, fin, fin.AddDays(5)), 400, mensajeCodigo);
        await AssertCrearAsync(Solicitud(new string('A', 21), "Ciclo", "Semestral", inicio, fin, fin, fin.AddDays(5)), 400, mensajeCodigo);
        await AssertCrearAsync(Solicitud("C1", null, "Semestral", inicio, fin, fin, fin.AddDays(5)), 400, mensajeNombre);
        await AssertCrearAsync(Solicitud("C1", new string('n', 101), "Semestral", inicio, fin, fin, fin.AddDays(5)), 400, mensajeNombre);
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Mensual", inicio, fin, fin, fin.AddDays(5)), 400, "El periodo debe ser Semestral o Anual.");
        await AssertCrearAsync(Solicitud("C1", "Ciclo", null, inicio, fin, fin, fin.AddDays(5)), 400, "El periodo debe ser Semestral o Anual.");
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", null, fin, fin, fin.AddDays(5)), 400, mensajeFechas);
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", inicio, null, fin, fin.AddDays(5)), 400, mensajeFechas);
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", inicio, fin, null, fin.AddDays(5)), 400, mensajeFechas);
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", inicio, fin, fin, null), 400, mensajeFechas);
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", fin.AddDays(-168), fin, fin, fin.AddDays(5)), 400, "La duración del periodo no corresponde a un ciclo Semestral.");
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", fin.AddDays(-190), fin, fin, fin.AddDays(5)), 400, "La duración del periodo no corresponde a un ciclo Semestral.");
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Anual", inicio, fin, fin, fin.AddDays(5)), 400, "La duración del periodo no corresponde a un ciclo Anual.");
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", fin, inicio, fin, fin.AddDays(5)), 400, "La duración del periodo no corresponde a un ciclo Semestral.");
        string mensajeApertura = "La apertura no puede ser anterior a 30 días antes del fin del periodo.";
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", inicio, fin, fin.AddDays(-31), fin.AddDays(5)), 400, mensajeApertura);
        string mensajeCierre = "La fecha de cierre debe ser posterior a la de apertura.";
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", inicio, fin, fin, fin), 400, mensajeCierre);
        await AssertCrearAsync(Solicitud("C1", "Ciclo", "Semestral", inicio, fin, fin, fin.AddDays(-1)), 400, mensajeCierre);
        Assert.Empty(await DatosEvaluacion.LeerCiclosAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud("C1", "Ciclo", "Semestral", fin.AddDays(-169), fin, fin.AddDays(-30), fin.AddDays(-29)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud("C2", "Ciclo", "Semestral", fin.AddDays(-189), fin, fin, fin.AddDays(1)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud("C3", "Ciclo", "Anual", fin.AddDays(-359), fin, fin, fin.AddDays(1)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud("C4", "Ciclo", "Anual", fin.AddDays(-369), fin, fin, fin.AddDays(1)))).StatusCode);
    }

    [Fact]
    public async Task CrearYActualizarNormalizanElCodigoYRechazanDuplicadosConMensaje8()
    {
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        DateOnly inicio = fin.AddDays(-179);
        HttpResponseMessage creado = await CrearAsync(Solicitud("  s1-2027 ", " Primer semestre ", "Semestral", inicio, fin, fin, fin.AddDays(5)));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        CicloEvaluacionIdDto? id = await creado.Content.ReadFromJsonAsync<CicloEvaluacionIdDto>();
        Assert.NotNull(id);
        CicloEvaluacion ciclo = Assert.Single(await DatosEvaluacion.LeerCiclosAsync(_fabrica));
        Assert.Equal("S1-2027", ciclo.Codigo);
        Assert.Equal("Primer semestre", ciclo.Nombre);
        Assert.Equal("Configuracion", ciclo.Estado);
        string duplicado = "Ya existe un ciclo con el código S1-2027.";
        await DatosEvaluacion.AssertErrorAsync(await CrearAsync(Solicitud("s1-2027", "Otro", "Semestral", inicio, fin, fin, fin.AddDays(5))), 409, duplicado);
        HttpResponseMessage segundo = await CrearAsync(Solicitud("S2-2027", "Segundo", "Semestral", inicio, fin, fin, fin.AddDays(5)));
        CicloEvaluacionIdDto? idSegundo = await segundo.Content.ReadFromJsonAsync<CicloEvaluacionIdDto>();
        Assert.NotNull(idSegundo);
        await DatosEvaluacion.AssertErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud("s1-2027", "Segundo", "Semestral", inicio, fin, fin, fin.AddDays(5))),
            409,
            duplicado);
        HttpResponseMessage actualizado = await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud("s2-2027", "Segundo editado", "Anual", fin.AddDays(-364), fin, fin, fin.AddDays(9)));
        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);
        CicloEvaluacion editado = (await DatosEvaluacion.LeerCiclosAsync(_fabrica)).Single(otro => otro.Id == idSegundo.Id);
        Assert.Equal("Segundo editado", editado.Nombre);
        Assert.Equal("Anual", editado.Periodo);
        Assert.Equal(fin.AddDays(9), editado.FechaCierre);
    }

    [Fact]
    public async Task CicloInexistenteDevuelve404ConMensaje9EnTodasLasRutas()
    {
        string mensaje = "El ciclo de evaluación 9999 no existe.";
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        GuardarCicloEvaluacionRequestDto solicitud = Solicitud("C1", "Ciclo", "Semestral", fin.AddDays(-179), fin, fin, fin.AddDays(5));
        GuardarCompetenciaEvaluacionRequestDto competencia = new GuardarCompetenciaEvaluacionRequestDto { Nombre = "X", Peso = 10m };
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999"), 404, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999/resultados"), 404, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{RutaBase}/9999", solicitud), 404, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/9999/competencias", competencia), 404, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{RutaBase}/9999/competencias/1", competencia), 404, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.DeleteAsync($"{RutaBase}/9999/competencias/1"), 404, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/apertura", null), 404, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/cierre", null), 404, mensaje);
    }

    // Mensaje 10: un ciclo abierto o cerrado ya no admite cambios de datos ni de competencias.
    [Theory]
    [InlineData("Abierto", "AB")]
    [InlineData("Cerrado", "CE")]
    public async Task CicloFueraDeConfiguracionDevuelveMensaje10SinCambios(string estado, string codigo)
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAsync(
            _fabrica,
            codigo,
            estado,
            DatosEvaluacion.Hoy.AddDays(-5),
            DatosEvaluacion.Hoy.AddDays(20),
            60m,
            40m);
        string[] antes = await DatosEvaluacion.LeerFirmasAsync(_fabrica);
        string mensaje = $"El ciclo {codigo} ya no está en configuración.";
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        GuardarCompetenciaEvaluacionRequestDto competencia = new GuardarCompetenciaEvaluacionRequestDto { Nombre = "Nueva", Peso = 10m };
        await DatosEvaluacion.AssertErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaBase}/{cicloId}", Solicitud(codigo, "Ciclo", "Semestral", fin.AddDays(-179), fin, fin, fin.AddDays(5))),
            409,
            mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/{cicloId}/competencias", competencia), 409, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{RutaBase}/{cicloId}/competencias/{competencias[0]}", competencia), 409, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.DeleteAsync($"{RutaBase}/{cicloId}/competencias/{competencias[0]}"), 409, mensaje);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{cicloId}/apertura", null), 409, mensaje);
        Assert.Equal(antes, await DatosEvaluacion.LeerFirmasAsync(_fabrica));
    }

    // Mensajes 2, 34, 11, 12 y 13.
    [Fact]
    public async Task CompetenciasValidanNombreDescripcionPesoDuplicadoYPertenencia()
    {
        int cicloId = await CrearCicloEnConfiguracionAsync("CFG");
        string ruta = $"{RutaBase}/{cicloId}/competencias";
        string mensajeNombre = "El nombre es obligatorio y no puede superar 100 caracteres.";
        string mensajePeso = "El peso debe ser mayor que 0 y como máximo 100, con 2 decimales.";
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia(null, null, 10m)), 400, mensajeNombre);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia(new string('n', 101), null, 10m)), 400, mensajeNombre);
        await DatosEvaluacion.AssertErrorAsync(
            await _cliente.PostAsJsonAsync(ruta, Competencia("Liderazgo", new string('d', 301), 10m)),
            400,
            "La descripción no puede superar 300 caracteres.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia("Liderazgo", null, null)), 400, mensajePeso);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia("Liderazgo", null, 0m)), 400, mensajePeso);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia("Liderazgo", null, -5m)), 400, mensajePeso);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia("Liderazgo", null, 100.01m)), 400, mensajePeso);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia("Liderazgo", null, 10.005m)), 400, mensajePeso);
        Assert.Empty(await DatosEvaluacion.LeerCompetenciasAsync(_fabrica));
        HttpResponseMessage creada = await _cliente.PostAsJsonAsync(ruta, Competencia(" Liderazgo ", "  Guia al equipo ", 40.5m));
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        CompetenciaEvaluacionIdDto? id = await creada.Content.ReadFromJsonAsync<CompetenciaEvaluacionIdDto>();
        Assert.NotNull(id);
        CompetenciaEvaluacion guardada = Assert.Single(await DatosEvaluacion.LeerCompetenciasAsync(_fabrica));
        Assert.Equal("Liderazgo", guardada.Nombre);
        Assert.Equal("Guia al equipo", guardada.Descripcion);
        Assert.Equal(40.5m, guardada.Peso);
        string duplicada = "Ya existe la competencia liderazgo en el ciclo.";
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsJsonAsync(ruta, Competencia("liderazgo", null, 10m)), 409, duplicada);
        HttpResponseMessage otra = await _cliente.PostAsJsonAsync(ruta, Competencia("Calidad", null, 59.5m));
        CompetenciaEvaluacionIdDto? idOtra = await otra.Content.ReadFromJsonAsync<CompetenciaEvaluacionIdDto>();
        Assert.NotNull(idOtra);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{ruta}/{idOtra.Id}", Competencia("liderazgo", null, 10m)), 409, duplicada);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{ruta}/{idOtra.Id}", Competencia("Calidad", null, 0m)), 400, mensajePeso);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{ruta}/{idOtra.Id}", Competencia("CALIDAD", "Detalle", 20m))).StatusCode);
        CompetenciaEvaluacion editada = (await DatosEvaluacion.LeerCompetenciasAsync(_fabrica)).Single(competencia => competencia.Id == idOtra.Id);
        Assert.Equal("CALIDAD", editada.Nombre);
        Assert.Equal(20m, editada.Peso);
        string noExiste = "La competencia 9999 no existe en el ciclo.";
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{ruta}/9999", Competencia("Otra", null, 10m)), 404, noExiste);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.DeleteAsync($"{ruta}/9999"), 404, noExiste);
        int otroCiclo = await CrearCicloEnConfiguracionAsync("OTRO");
        string noPertenece = $"La competencia {id.Id} no existe en el ciclo.";
        await DatosEvaluacion.AssertErrorAsync(await _cliente.DeleteAsync($"{RutaBase}/{otroCiclo}/competencias/{id.Id}"), 404, noPertenece);
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{ruta}/{id.Id}")).StatusCode);
        Assert.DoesNotContain(await DatosEvaluacion.LeerCompetenciasAsync(_fabrica), competencia => competencia.Id == id.Id);
    }

    [Fact]
    public async Task DetalleMuestraCompetenciasSumaDePesosYConteoDeEvaluacionesPorEstado()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "DET", 33.33m, 33.33m, 33.34m);
        await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 2, 1, "Enviada", 4m);
        HttpResponseMessage respuesta = await _cliente.GetAsync($"{RutaBase}/{cicloId}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        CicloEvaluacionDetalleDto? detalle = await respuesta.Content.ReadFromJsonAsync<CicloEvaluacionDetalleDto>();
        Assert.NotNull(detalle);
        Assert.Equal("DET", detalle.Codigo);
        Assert.Equal("Abierto", detalle.Estado);
        Assert.Equal(100m, detalle.SumaPesos);
        Assert.Equal(competencias, detalle.Competencias.Select(competencia => competencia.Id).ToList());
        Assert.Equal(1, detalle.Evaluaciones.Borrador);
        Assert.Equal(1, detalle.Evaluaciones.Enviada);
        Assert.Equal(0, detalle.Evaluaciones.Cerrada);
    }

    // Mensaje 14: la suma de pesos se informa con dos decimales y punto.
    [Fact]
    public async Task AperturaExigePesosQueSumenCienConMensaje14()
    {
        int cicloId = await CrearCicloEnConfiguracionAsync("APE");
        string ruta = $"{RutaBase}/{cicloId}";
        await DatosEvaluacion.AssertErrorAsync(
            await _cliente.PostAsync($"{ruta}/apertura", null),
            409,
            "Los pesos de las competencias suman 0.00 y deben sumar 100.");
        await _cliente.PostAsJsonAsync($"{ruta}/competencias", Competencia("A", null, 60m));
        await _cliente.PostAsJsonAsync($"{ruta}/competencias", Competencia("B", null, 39.99m));
        await DatosEvaluacion.AssertErrorAsync(
            await _cliente.PostAsync($"{ruta}/apertura", null),
            409,
            "Los pesos de las competencias suman 99.99 y deben sumar 100.");
        Assert.Equal("Configuracion", (await DatosEvaluacion.LeerCiclosAsync(_fabrica)).Single().Estado);
        await _cliente.PostAsJsonAsync($"{ruta}/competencias", Competencia("C", null, 0.01m));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{ruta}/apertura", null)).StatusCode);
        Assert.Equal("Abierto", (await DatosEvaluacion.LeerCiclosAsync(_fabrica)).Single().Estado);
    }

    // Mensajes 15 y 30, y cierre de las evaluaciones enviadas (T3: los borradores quedan congelados).
    [Fact]
    public async Task CierreDelCicloCierraLasEnviadasYDejaCongeladosLosBorradores()
    {
        int configuracion = await CrearCicloEnConfiguracionAsync("CONF");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{configuracion}/cierre", null), 409, "El ciclo CONF no está abierto.");
        (int cicloId, _) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "CIE", 100m);
        int borrador = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        int enviada = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 2, 1, "Enviada", 4m);
        HttpResponseMessage respuesta = await _cliente.PostAsync($"{RutaBase}/{cicloId}/cierre", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        List<Evaluacion> evaluaciones = await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica);
        Evaluacion cerrada = evaluaciones.Single(evaluacion => evaluacion.Id == enviada);
        Assert.Equal("Cerrada", cerrada.Estado);
        Assert.NotNull(cerrada.FechaCierre);
        Assert.Equal("Borrador", evaluaciones.Single(evaluacion => evaluacion.Id == borrador).Estado);
        Assert.Equal("Cerrado", (await DatosEvaluacion.LeerCiclosAsync(_fabrica)).Single(ciclo => ciclo.Id == cicloId).Estado);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{cicloId}/cierre", null), 409, "El ciclo CIE ya está cerrado.");
    }

    // Mensajes 31, 32 y 33, y filtros del listado.
    [Fact]
    public async Task ListadoValidaPaginacionYFiltrosYAplicaBusquedaYEstado()
    {
        await DatosEvaluacion.InsertarCicloAsync(_fabrica, "ALFA", "Configuracion", DatosEvaluacion.Hoy, DatosEvaluacion.Hoy.AddDays(5), 100m);
        await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "BETA", 100m);
        await DatosEvaluacion.InsertarCicloAsync(_fabrica, "GAMA", "Cerrado", DatosEvaluacion.Hoy.AddDays(-30), DatosEvaluacion.Hoy.AddDays(-5), 100m);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?estado=Otro"), 400, "El filtro estado no es válido.");
        PaginaCiclosEvaluacionDto? todos = await _cliente.GetFromJsonAsync<PaginaCiclosEvaluacionDto>(RutaBase);
        Assert.NotNull(todos);
        Assert.Equal(3, todos.Total);
        PaginaCiclosEvaluacionDto? abiertos = await _cliente.GetFromJsonAsync<PaginaCiclosEvaluacionDto>($"{RutaBase}?estado=Abierto");
        Assert.NotNull(abiertos);
        Assert.Equal("BETA", Assert.Single(abiertos.Items).Codigo);
        PaginaCiclosEvaluacionDto? busqueda = await _cliente.GetFromJsonAsync<PaginaCiclosEvaluacionDto>($"{RutaBase}?busqueda=gam");
        Assert.NotNull(busqueda);
        Assert.Equal("GAMA", Assert.Single(busqueda.Items).Codigo);
        PaginaCiclosEvaluacionDto? paginada = await _cliente.GetFromJsonAsync<PaginaCiclosEvaluacionDto>($"{RutaBase}?pagina=2&tamanoPagina=2");
        Assert.NotNull(paginada);
        Assert.Equal(3, paginada.Total);
        Assert.Single(paginada.Items);
    }

    [Fact]
    public async Task ResultadosCuentanSoloEnviadasYCerradasConDistribucionYPromedios()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "RES", 50m, 50m);
        ResultadosCicloEvaluacionDto? sinDatos = await _cliente.GetFromJsonAsync<ResultadosCicloEvaluacionDto>($"{RutaBase}/{cicloId}/resultados");
        Assert.NotNull(sinDatos);
        Assert.Equal(0, sinDatos.Evaluadas);
        Assert.Equal(0m, sinDatos.PuntajePromedio);
        Assert.Equal(5, sinDatos.Distribucion.Count);
        Assert.All(sinDatos.Distribucion, item => Assert.Equal(0, item.Cantidad));
        int uno = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Enviada", 4.5m);
        int dos = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 2, 1, "Cerrada", 2.5m);
        int borrador = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 5, 1, "Borrador", null);
        await DatosEvaluacion.InsertarCalificacionAsync(_fabrica, uno, competencias[0], 4);
        await DatosEvaluacion.InsertarCalificacionAsync(_fabrica, uno, competencias[1], 5);
        await DatosEvaluacion.InsertarCalificacionAsync(_fabrica, dos, competencias[0], 2);
        await DatosEvaluacion.InsertarCalificacionAsync(_fabrica, dos, competencias[1], 3);
        await DatosEvaluacion.InsertarCalificacionAsync(_fabrica, borrador, competencias[0], 1);
        ResultadosCicloEvaluacionDto? resultados = await _cliente.GetFromJsonAsync<ResultadosCicloEvaluacionDto>($"{RutaBase}/{cicloId}/resultados");
        Assert.NotNull(resultados);
        Assert.Equal(2, resultados.Evaluadas);
        Assert.Equal(3.5m, resultados.PuntajePromedio);
        Assert.Equal(
            new[] { "Insuficiente", "En desarrollo", "Competente", "Destacado", "Excepcional" },
            resultados.Distribucion.Select(item => item.Etiqueta).ToArray());
        Assert.Equal(new[] { 0, 1, 0, 0, 1 }, resultados.Distribucion.Select(item => item.Cantidad).ToArray());
        Assert.Equal(new[] { "Competencia 1", "Competencia 2" }, resultados.PorCompetencia.Select(item => item.Competencia).ToArray());
        Assert.Equal(new[] { 3m, 4m }, resultados.PorCompetencia.Select(item => item.Promedio).ToArray());
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        int id = await CrearCicloEnConfiguracionAsync("ROL");
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        GuardarCicloEvaluacionRequestDto solicitud = Solicitud("X", "Ciclo", "Semestral", fin.AddDays(-179), fin, fin, fin.AddDays(5));
        HttpClient anonimo = DatosEvaluacion.ClienteAnonimo(_fabrica);
        string sesion = DatosEvaluacion.MensajeSinSesion;
        await DatosEvaluacion.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, sesion);
        await DatosEvaluacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/{id}"), 401, sesion);
        await DatosEvaluacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/{id}/resultados"), 401, sesion);
        await DatosEvaluacion.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, solicitud), 401, sesion);
        await DatosEvaluacion.AssertErrorAsync(await anonimo.PostAsync($"{RutaBase}/{id}/apertura", null), 401, sesion);
        HttpClient consulta = DatosEvaluacion.ClienteConRoles(_fabrica, "CONSULTA");
        string[] antes = await DatosEvaluacion.LeerFirmasAsync(_fabrica);
        string permiso = DatosEvaluacion.MensajeSinPermiso;
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/{id}")).StatusCode);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, solicitud), 403, permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PutAsJsonAsync($"{RutaBase}/{id}", solicitud), 403, permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PostAsJsonAsync($"{RutaBase}/{id}/competencias", Competencia("A", null, 10m)), 403, permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PutAsJsonAsync($"{RutaBase}/{id}/competencias/1", Competencia("A", null, 10m)), 403, permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.DeleteAsync($"{RutaBase}/{id}/competencias/1"), 403, permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/apertura", null), 403, permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/cierre", null), 403, permiso);
        Assert.Equal(antes, await DatosEvaluacion.LeerFirmasAsync(_fabrica));
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenCrearYAbrirCiclos(string rol)
    {
        HttpClient cliente = DatosEvaluacion.ClienteConRoles(_fabrica, rol);
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        HttpResponseMessage creado = await cliente.PostAsJsonAsync(RutaBase, Solicitud("R-" + rol, "Ciclo", "Semestral", fin.AddDays(-179), fin, fin, fin.AddDays(5)));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        CicloEvaluacionIdDto? id = await creado.Content.ReadFromJsonAsync<CicloEvaluacionIdDto>();
        Assert.NotNull(id);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync($"{RutaBase}/{id.Id}/competencias", Competencia("A", null, 100m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/apertura", null)).StatusCode);
    }

    [Fact]
    public async Task CicloYCompetenciasGeneranRegistrosDeAuditoria()
    {
        int cicloId = await CrearCicloEnConfiguracionAsync("AUD");
        Assert.Single(await DatosEvaluacion.LeerAuditoriaAsync(_fabrica, "CicloEvaluacion", cicloId), registro => registro.Accion == "Alta");
        HttpResponseMessage competencia = await _cliente.PostAsJsonAsync($"{RutaBase}/{cicloId}/competencias", Competencia("A", null, 100m));
        CompetenciaEvaluacionIdDto? id = await competencia.Content.ReadFromJsonAsync<CompetenciaEvaluacionIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosEvaluacion.LeerAuditoriaAsync(_fabrica, "CompetenciaEvaluacion", id.Id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{cicloId}/apertura", null)).StatusCode);
        RegistroAuditoria cambio = Assert.Single(
            await DatosEvaluacion.LeerAuditoriaAsync(_fabrica, "CicloEvaluacion", cicloId),
            registro => registro.Accion == "Modificacion");
        using JsonDocument cambios = JsonDocument.Parse(cambio.Cambios);
        List<string> campos = cambios.RootElement.EnumerateArray().Select(item => item.GetProperty("campo").GetString() ?? string.Empty).ToList();
        Assert.Contains("Estado", campos);
    }

    private static GuardarCicloEvaluacionRequestDto Solicitud(
        string? codigo,
        string? nombre,
        string? periodo,
        DateOnly? inicio,
        DateOnly? fin,
        DateOnly? apertura,
        DateOnly? cierre)
    {
        return new GuardarCicloEvaluacionRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Periodo = periodo,
            FechaInicioPeriodo = inicio,
            FechaFinPeriodo = fin,
            FechaApertura = apertura,
            FechaCierre = cierre
        };
    }

    private static GuardarCompetenciaEvaluacionRequestDto Competencia(string? nombre, string? descripcion, decimal? peso)
    {
        return new GuardarCompetenciaEvaluacionRequestDto { Nombre = nombre, Descripcion = descripcion, Peso = peso };
    }

    private async Task<HttpResponseMessage> CrearAsync(GuardarCicloEvaluacionRequestDto solicitud)
    {
        return await _cliente.PostAsJsonAsync(RutaBase, solicitud);
    }

    private async Task AssertCrearAsync(GuardarCicloEvaluacionRequestDto solicitud, int codigo, string mensaje)
    {
        await DatosEvaluacion.AssertErrorAsync(await CrearAsync(solicitud), codigo, mensaje);
    }

    private async Task<int> CrearCicloEnConfiguracionAsync(string codigo)
    {
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        HttpResponseMessage respuesta = await CrearAsync(Solicitud(codigo, "Ciclo " + codigo, "Semestral", fin.AddDays(-179), fin, fin.AddDays(-5), fin.AddDays(20)));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        CicloEvaluacionIdDto? id = await respuesta.Content.ReadFromJsonAsync<CicloEvaluacionIdDto>();
        Assert.NotNull(id);
        return id.Id;
    }
}
