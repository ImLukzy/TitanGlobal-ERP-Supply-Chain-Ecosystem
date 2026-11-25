using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo68_EvaluacionDesempeno;

[Collection("Modulo04")]
public class EvaluacionesApiTests
{
    private const string RutaBase = "/api/v1/evaluaciones";

    private const string RutaCiclos = "/api/v1/ciclos-evaluacion";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public EvaluacionesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosEvaluacion.Restablecer(fabrica);
    }

    // Mensajes 17, 18, 9, 15, 16, 19, 20, 21 y 22 en el orden de la spec.
    [Fact]
    public async Task CreacionConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        (int abierto, _) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "ABI", 100m);
        (int configuracion, _) = await DatosEvaluacion.InsertarCicloAsync(_fabrica, "CFG", "Configuracion", DatosEvaluacion.Hoy.AddDays(-5), DatosEvaluacion.Hoy.AddDays(20), 100m);
        (int cerrado, _) = await DatosEvaluacion.InsertarCicloAsync(_fabrica, "CER", "Cerrado", DatosEvaluacion.Hoy.AddDays(-5), DatosEvaluacion.Hoy.AddDays(20), 100m);
        DateOnly futuraApertura = DatosEvaluacion.Hoy.AddDays(3);
        DateOnly futuraCierre = DatosEvaluacion.Hoy.AddDays(20);
        (int futuro, _) = await DatosEvaluacion.InsertarCicloAsync(_fabrica, "FUT", "Abierto", futuraApertura, futuraCierre, 100m);
        DateOnly pasadaApertura = DatosEvaluacion.Hoy.AddDays(-30);
        DateOnly pasadaCierre = DatosEvaluacion.Hoy.AddDays(-1);
        (int pasado, _) = await DatosEvaluacion.InsertarCicloAsync(_fabrica, "PAS", "Abierto", pasadaApertura, pasadaCierre, 100m);
        string obligatorios = "El ciclo, el empleado y el evaluador son obligatorios.";
        await AssertCrearAsync(null, 1, 2, 400, obligatorios);
        await AssertCrearAsync(abierto, null, 2, 400, obligatorios);
        await AssertCrearAsync(abierto, 1, null, 400, obligatorios);
        await AssertCrearAsync(abierto, 1, 1, 400, "El evaluador no puede ser el propio evaluado.");
        await AssertCrearAsync(9999, 1, 2, 404, "El ciclo de evaluación 9999 no existe.");
        await AssertCrearAsync(configuracion, 1, 2, 409, "El ciclo CFG no está abierto.");
        await AssertCrearAsync(cerrado, 1, 2, 409, "El ciclo CER no está abierto.");
        await AssertCrearAsync(
            futuro,
            1,
            2,
            409,
            $"El ciclo FUT solo admite evaluaciones entre el {futuraApertura.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} y el {futuraCierre.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
        await AssertCrearAsync(
            pasado,
            1,
            2,
            409,
            $"El ciclo PAS solo admite evaluaciones entre el {pasadaApertura.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} y el {pasadaCierre.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
        await AssertCrearAsync(abierto, 9999, 2, 404, "El empleado 9999 no existe.");
        await AssertCrearAsync(abierto, 3, 2, 409, "El empleado 3 no tuvo contrato vigente durante el periodo del ciclo.");
        await AssertCrearAsync(abierto, 5, 2, 409, "El empleado 5 no tuvo contrato vigente durante el periodo del ciclo.");
        await AssertCrearAsync(abierto, 1, 9998, 404, "El empleado 9998 no existe.");
        await AssertCrearAsync(abierto, 1, 4, 409, "El evaluador 4 no está activo.");
        Assert.Empty(await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(abierto, 1, 2)).StatusCode);
        await AssertCrearAsync(abierto, 1, 5, 409, "El empleado 1 ya tiene una evaluación en el ciclo ABI.");
        Assert.Single(await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica));
    }

    // D5: basta un día de contrato dentro del periodo; un contrato anulado no cuenta.
    [Fact]
    public async Task ContratoConUnDiaDentroDelPeriodoPermiteEvaluarYElAnuladoNo()
    {
        (int cicloId, _) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "D5", 100m);
        DateOnly inicioPeriodo = DatosEvaluacion.Hoy.AddDays(10).AddDays(-179);
        await InsertarContratoAsync(3, inicioPeriodo, null, "Vigente");
        await InsertarContratoAsync(5, inicioPeriodo.AddDays(-400), inicioPeriodo.AddDays(-1), "Vencido");
        await InsertarContratoAsync(5, inicioPeriodo.AddDays(-30), null, "Anulado");
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(cicloId, 3, 1)).StatusCode);
        await AssertCrearAsync(cicloId, 5, 1, 409, "El empleado 5 no tuvo contrato vigente durante el periodo del ciclo.");
        await InsertarContratoAsync(5, inicioPeriodo.AddDays(-30), inicioPeriodo, "Renovado");
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(cicloId, 5, 1)).StatusCode);
    }

    [Fact]
    public async Task CreacionValidaDejaLaEvaluacionEnBorradorSinPuntaje()
    {
        (int cicloId, _) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "OK", 100m);
        HttpResponseMessage respuesta = await CrearAsync(cicloId, 1, 2);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Evaluacion evaluacion = Assert.Single(await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica));
        Assert.Equal("Borrador", evaluacion.Estado);
        Assert.Equal(1, evaluacion.EmpleadoId);
        Assert.Equal(2, evaluacion.EvaluadorId);
        Assert.Null(evaluacion.PuntajeFinal);
        Assert.Null(evaluacion.FechaEnvio);
    }

    // Flujo completo: ciclo, competencias, apertura, evaluación, calificaciones, envío y cierres.
    [Fact]
    public async Task FlujoCompletoDesdeElCicloHastaElCierreDeLaEvaluacion()
    {
        DateOnly fin = DatosEvaluacion.Hoy.AddDays(10);
        HttpResponseMessage ciclo = await _cliente.PostAsJsonAsync(
            RutaCiclos,
            new GuardarCicloEvaluacionRequestDto
            {
                Codigo = "FLUJO",
                Nombre = "Flujo completo",
                Periodo = "Semestral",
                FechaInicioPeriodo = fin.AddDays(-179),
                FechaFinPeriodo = fin,
                FechaApertura = DatosEvaluacion.Hoy.AddDays(-1),
                FechaCierre = DatosEvaluacion.Hoy.AddDays(30)
            });
        CicloEvaluacionIdDto? cicloId = await ciclo.Content.ReadFromJsonAsync<CicloEvaluacionIdDto>();
        Assert.NotNull(cicloId);
        List<int> competencias = new List<int>();
        foreach ((string nombre, decimal peso) in new[] { ("Calidad", 45.5m), ("Equipo", 54.5m) })
        {
            HttpResponseMessage creada = await _cliente.PostAsJsonAsync(
                $"{RutaCiclos}/{cicloId.Id}/competencias",
                new GuardarCompetenciaEvaluacionRequestDto { Nombre = nombre, Peso = peso });
            CompetenciaEvaluacionIdDto? id = await creada.Content.ReadFromJsonAsync<CompetenciaEvaluacionIdDto>();
            Assert.NotNull(id);
            competencias.Add(id.Id);
        }

        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaCiclos}/{cicloId.Id}/apertura", null)).StatusCode);
        HttpResponseMessage evaluacion = await CrearAsync(cicloId.Id, 1, 2);
        EvaluacionIdDto? evaluacionId = await evaluacion.Content.ReadFromJsonAsync<EvaluacionIdDto>();
        Assert.NotNull(evaluacionId);
        Assert.Equal(HttpStatusCode.OK, (await GuardarAsync(evaluacionId.Id, "Buen año", (competencias[0], 3m), (competencias[1], 4m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{evaluacionId.Id}/envio", null)).StatusCode);
        EvaluacionDetalleDto? enviada = await _cliente.GetFromJsonAsync<EvaluacionDetalleDto>($"{RutaBase}/{evaluacionId.Id}");
        Assert.NotNull(enviada);
        Assert.Equal("Enviada", enviada.Estado);
        Assert.Equal(3.55m, enviada.PuntajeFinal);
        Assert.Equal("Competente", enviada.Etiqueta);
        Assert.Equal("Buen año", enviada.Comentario);
        Assert.NotNull(enviada.FechaEnvio);
        Assert.Equal(new int?[] { 3, 4 }, enviada.Calificaciones.Select(calificacion => calificacion.Calificacion).ToArray());
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaCiclos}/{cicloId.Id}/cierre", null)).StatusCode);
        EvaluacionDetalleDto? cerrada = await _cliente.GetFromJsonAsync<EvaluacionDetalleDto>($"{RutaBase}/{evaluacionId.Id}");
        Assert.NotNull(cerrada);
        Assert.Equal("Cerrada", cerrada.Estado);
        Assert.NotNull(cerrada.FechaCierre);
        ResultadosCicloEvaluacionDto? resultados = await _cliente.GetFromJsonAsync<ResultadosCicloEvaluacionDto>($"{RutaCiclos}/{cicloId.Id}/resultados");
        Assert.NotNull(resultados);
        Assert.Equal(1, resultados.Evaluadas);
        Assert.Equal(3.55m, resultados.PuntajePromedio);
    }

    // Mensajes 23, 24, 15, 27, 25 y 26.
    [Fact]
    public async Task CalificacionesValidanEstadoCicloComentarioNotaYCompetencia()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "CAL", 50m, 50m);
        (int otroCiclo, List<int> ajenas) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "AJE", 100m);
        int borrador = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        int enviada = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 2, 1, "Enviada", 3m);
        int deCerrado = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, otroCiclo, 1, 2, "Borrador", null);
        await DatosEvaluacion.AssertErrorAsync(await GuardarAsync(9999, null), 404, "La evaluación 9999 no existe.");
        await DatosEvaluacion.AssertErrorAsync(await GuardarAsync(enviada, null, (competencias[0], 3m)), 409, $"La evaluación {enviada} no está en borrador.");
        await CerrarCicloAsync(otroCiclo);
        await DatosEvaluacion.AssertErrorAsync(await GuardarAsync(deCerrado, null, (ajenas[0], 3m)), 409, "El ciclo AJE no está abierto.");
        await DatosEvaluacion.AssertErrorAsync(
            await GuardarAsync(borrador, new string('c', 501), (competencias[0], 3m)),
            400,
            "El comentario no puede superar 500 caracteres.");
        string nota = "La calificación debe ser un número entero de 1 a 5.";
        await DatosEvaluacion.AssertErrorAsync(await GuardarAsync(borrador, null, (competencias[0], 0m)), 400, nota);
        await DatosEvaluacion.AssertErrorAsync(await GuardarAsync(borrador, null, (competencias[0], 6m)), 400, nota);
        await DatosEvaluacion.AssertErrorAsync(await GuardarAsync(borrador, null, (competencias[0], 3.5m)), 400, nota);
        await DatosEvaluacion.AssertErrorAsync(await GuardarAsync(borrador, null, (competencias[0], -1m)), 400, nota);
        HttpResponseMessage sinNota = await _cliente.PutAsJsonAsync(
            $"{RutaBase}/{borrador}/calificaciones",
            new GuardarCalificacionesRequestDto { Calificaciones = new List<CalificacionRequestDto> { new CalificacionRequestDto { CompetenciaId = competencias[0] } } });
        await DatosEvaluacion.AssertErrorAsync(sinNota, 400, nota);
        await DatosEvaluacion.AssertErrorAsync(
            await GuardarAsync(borrador, null, (ajenas[0], 3m)),
            400,
            $"La competencia {ajenas[0]} no pertenece al ciclo de la evaluación.");
        await DatosEvaluacion.AssertErrorAsync(
            await GuardarAsync(borrador, null, (9999, 3m)),
            400,
            "La competencia 9999 no pertenece al ciclo de la evaluación.");
        Assert.Empty(await DatosEvaluacion.LeerCalificacionesAsync(_fabrica, borrador));
    }

    [Fact]
    public async Task GuardarCalificacionesReemplazaLasExistentesYElComentario()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "REE", 50m, 50m);
        int evaluacion = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        Assert.Equal(HttpStatusCode.OK, (await GuardarAsync(evaluacion, "Primero", (competencias[0], 2m), (competencias[1], 3m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GuardarAsync(evaluacion, "  ", (competencias[1], 5m))).StatusCode);
        CalificacionEvaluacion unica = Assert.Single(await DatosEvaluacion.LeerCalificacionesAsync(_fabrica, evaluacion));
        Assert.Equal(competencias[1], unica.CompetenciaEvaluacionId);
        Assert.Equal(5, unica.Calificacion);
        Assert.Null((await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica)).Single().Comentario);
        Assert.Equal(HttpStatusCode.OK, (await GuardarAsync(evaluacion, null)).StatusCode);
        Assert.Empty(await DatosEvaluacion.LeerCalificacionesAsync(_fabrica, evaluacion));
    }

    // Mensajes 23, 24, 15, 16 y 28.
    [Fact]
    public async Task EnvioValidaEstadoCicloVentanaYCalificacionesCompletas()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "ENV", 40m, 30m, 30m);
        int evaluacion = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/envio", null), 404, "La evaluación 9999 no existe.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{evaluacion}/envio", null), 409, "Faltan calificar 3 competencias para enviar la evaluación.");
        await GuardarAsync(evaluacion, null, (competencias[0], 4m), (competencias[1], 4m));
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{evaluacion}/envio", null), 409, "Faltan calificar 1 competencias para enviar la evaluación.");
        Assert.Equal("Borrador", (await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica)).Single().Estado);
        await GuardarAsync(evaluacion, null, (competencias[0], 4m), (competencias[1], 4m), (competencias[2], 4m));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{evaluacion}/envio", null)).StatusCode);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{evaluacion}/envio", null), 409, $"La evaluación {evaluacion} no está en borrador.");
        DateOnly apertura = DatosEvaluacion.Hoy.AddDays(-30);
        DateOnly cierre = DatosEvaluacion.Hoy.AddDays(-1);
        (int vencido, List<int> otras) = await DatosEvaluacion.InsertarCicloAsync(_fabrica, "VEN", "Abierto", apertura, cierre, 100m);
        int tardia = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, vencido, 1, 2, "Borrador", null);
        await DatosEvaluacion.InsertarCalificacionAsync(_fabrica, tardia, otras[0], 3);
        await DatosEvaluacion.AssertErrorAsync(
            await _cliente.PostAsync($"{RutaBase}/{tardia}/envio", null),
            409,
            $"El ciclo VEN solo admite evaluaciones entre el {apertura.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} y el {cierre.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
        await CerrarCicloAsync(vencido);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{tardia}/envio", null), 409, "El ciclo VEN no está abierto.");
    }

    // D3: puntaje ponderado exacto con pesos no redondos y redondeo del punto medio hacia arriba.
    [Fact]
    public async Task PuntajeSePonderaConPesosNoRedondosYRedondeaElPuntoMedioHaciaArriba()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "PES", 33.33m, 33.33m, 33.34m);
        int evaluacion = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        await GuardarAsync(evaluacion, null, (competencias[0], 5m), (competencias[1], 4m), (competencias[2], 3m));
        await _cliente.PostAsync($"{RutaBase}/{evaluacion}/envio", null);
        Evaluacion enviada = (await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica)).Single(otra => otra.Id == evaluacion);
        Assert.Equal(4.00m, enviada.PuntajeFinal);
        (int medio, List<int> mitades) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "MED", 45.5m, 54.5m);
        int segunda = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, medio, 2, 1, "Borrador", null);
        await GuardarAsync(segunda, null, (mitades[0], 3m), (mitades[1], 4m));
        await _cliente.PostAsync($"{RutaBase}/{segunda}/envio", null);
        Assert.Equal(3.55m, (await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica)).Single(otra => otra.Id == segunda).PuntajeFinal);
    }

    [Theory]
    [InlineData(1, 1, 1.0, "Insuficiente")]
    [InlineData(1, 2, 1.5, "Insuficiente")]
    [InlineData(2, 2, 2.0, "En desarrollo")]
    [InlineData(2, 3, 2.5, "En desarrollo")]
    [InlineData(3, 3, 3.0, "Competente")]
    [InlineData(3, 4, 3.5, "Competente")]
    [InlineData(4, 4, 4.0, "Destacado")]
    [InlineData(4, 5, 4.5, "Excepcional")]
    [InlineData(5, 5, 5.0, "Excepcional")]
    public async Task EtiquetaSeDeduceDelPuntajeEnLosLimitesDeLaEscala(int primera, int segunda, double puntajeEsperado, string etiqueta)
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "ETQ", 50m, 50m);
        int evaluacion = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        await GuardarAsync(evaluacion, null, (competencias[0], primera), (competencias[1], segunda));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{evaluacion}/envio", null)).StatusCode);
        EvaluacionDetalleDto? detalle = await _cliente.GetFromJsonAsync<EvaluacionDetalleDto>($"{RutaBase}/{evaluacion}");
        Assert.NotNull(detalle);
        Assert.Equal((decimal)puntajeEsperado, detalle.PuntajeFinal);
        Assert.Equal(etiqueta, detalle.Etiqueta);
    }

    // Mensajes 23 y 29.
    [Fact]
    public async Task CierreDeLaEvaluacionExigeEstarEnviada()
    {
        (int cicloId, _) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "CEV", 100m);
        int borrador = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        int enviada = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 2, 1, "Enviada", 4m);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/cierre", null), 404, "La evaluación 9999 no existe.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{borrador}/cierre", null), 409, $"La evaluación {borrador} debe estar enviada para cerrarse.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{enviada}/cierre", null)).StatusCode);
        Evaluacion cerrada = (await DatosEvaluacion.LeerEvaluacionesAsync(_fabrica)).Single(otra => otra.Id == enviada);
        Assert.Equal("Cerrada", cerrada.Estado);
        Assert.NotNull(cerrada.FechaCierre);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{enviada}/cierre", null), 409, $"La evaluación {enviada} debe estar enviada para cerrarse.");
    }

    // Mensajes 31, 32, 33, 9 y 19 del listado.
    [Fact]
    public async Task ListadoValidaPaginacionFiltrosYAplicaCadaFiltro()
    {
        (int uno, _) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "UNO", 100m);
        (int dos, _) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "DOS", 100m);
        await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, uno, 1, 2, "Borrador", null);
        await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, uno, 2, 1, "Enviada", 4m);
        await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, dos, 1, 2, "Cerrada", 2m);
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?estado=Otro"), 400, "El filtro estado no es válido.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?cicloId=9999"), 404, "El ciclo de evaluación 9999 no existe.");
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?empleadoId=9999"), 404, "El empleado 9999 no existe.");
        Assert.Equal(3, (await ListarAsync(string.Empty)).Total);
        Assert.Equal(2, (await ListarAsync($"?cicloId={uno}")).Total);
        Assert.Equal(2, (await ListarAsync("?empleadoId=1")).Total);
        Assert.Equal(1, (await ListarAsync("?estado=Enviada")).Total);
        PaginaEvaluacionesDto porBusqueda = await ListarAsync("?busqueda=torres");
        Assert.Equal(2, porBusqueda.Total);
        Assert.Equal(1, (await ListarAsync("?busqueda=DOS")).Total);
        PaginaEvaluacionesDto pagina = await ListarAsync("?pagina=2&tamanoPagina=2");
        Assert.Equal(3, pagina.Total);
        Assert.Single(pagina.Items);
        EvaluacionListadoDto cerrada = (await ListarAsync("?estado=Cerrada")).Items.Single();
        Assert.Equal("DOS", cerrada.CicloCodigo);
        Assert.Equal("Torres Vega, Ana", cerrada.EmpleadoNombre);
        Assert.Equal("En desarrollo", cerrada.Etiqueta);
        Assert.Null((await ListarAsync("?estado=Borrador")).Items.Single().Etiqueta);
    }

    [Fact]
    public async Task DetalleIncluyeTodasLasCompetenciasDelCicloConSuCalificacion()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "DTL", 70m, 30m);
        int evaluacion = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        await DatosEvaluacion.InsertarCalificacionAsync(_fabrica, evaluacion, competencias[1], 5);
        EvaluacionDetalleDto? detalle = await _cliente.GetFromJsonAsync<EvaluacionDetalleDto>($"{RutaBase}/{evaluacion}");
        Assert.NotNull(detalle);
        Assert.Equal("DTL", detalle.CicloCodigo);
        Assert.Equal("Torres Vega, Ana", detalle.EmpleadoNombre);
        Assert.Equal("Rojas Mendoza, Luis", detalle.EvaluadorNombre);
        Assert.Null(detalle.Etiqueta);
        Assert.Equal(new int?[] { null, 5 }, detalle.Calificaciones.Select(calificacion => calificacion.Calificacion).ToArray());
        Assert.Equal(new[] { 70m, 30m }, detalle.Calificaciones.Select(calificacion => calificacion.Peso).ToArray());
        await DatosEvaluacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999"), 404, "La evaluación 9999 no existe.");
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "ROL", 100m);
        int evaluacion = await DatosEvaluacion.InsertarEvaluacionAsync(_fabrica, cicloId, 1, 2, "Borrador", null);
        HttpClient anonimo = DatosEvaluacion.ClienteAnonimo(_fabrica);
        string sesion = DatosEvaluacion.MensajeSinSesion;
        await DatosEvaluacion.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, sesion);
        await DatosEvaluacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/{evaluacion}"), 401, sesion);
        await DatosEvaluacion.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, new CrearEvaluacionRequestDto()), 401, sesion);
        await DatosEvaluacion.AssertErrorAsync(await anonimo.PostAsync($"{RutaBase}/{evaluacion}/envio", null), 401, sesion);
        HttpClient consulta = DatosEvaluacion.ClienteConRoles(_fabrica, "CONSULTA");
        string[] antes = await DatosEvaluacion.LeerFirmasAsync(_fabrica);
        string permiso = DatosEvaluacion.MensajeSinPermiso;
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/{evaluacion}")).StatusCode);
        await DatosEvaluacion.AssertErrorAsync(
            await consulta.PostAsJsonAsync(RutaBase, new CrearEvaluacionRequestDto { CicloId = cicloId, EmpleadoId = 2, EvaluadorId = 1 }),
            403,
            permiso);
        await DatosEvaluacion.AssertErrorAsync(
            await consulta.PutAsJsonAsync($"{RutaBase}/{evaluacion}/calificaciones", new GuardarCalificacionesRequestDto()),
            403,
            permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{evaluacion}/envio", null), 403, permiso);
        await DatosEvaluacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{evaluacion}/cierre", null), 403, permiso);
        Assert.Equal(antes, await DatosEvaluacion.LeerFirmasAsync(_fabrica));
        Assert.Single(competencias);
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenEvaluarDeExtremoAExtremo(string rol)
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "R-" + rol, 100m);
        HttpClient cliente = DatosEvaluacion.ClienteConRoles(_fabrica, rol);
        HttpResponseMessage creada = await cliente.PostAsJsonAsync(RutaBase, new CrearEvaluacionRequestDto { CicloId = cicloId, EmpleadoId = 1, EvaluadorId = 2 });
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        EvaluacionIdDto? id = await creada.Content.ReadFromJsonAsync<EvaluacionIdDto>();
        Assert.NotNull(id);
        Assert.Equal(
            HttpStatusCode.OK,
            (await cliente.PutAsJsonAsync(
                $"{RutaBase}/{id.Id}/calificaciones",
                new GuardarCalificacionesRequestDto { Calificaciones = new List<CalificacionRequestDto> { new CalificacionRequestDto { CompetenciaId = competencias[0], Calificacion = 4m } } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/envio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/cierre", null)).StatusCode);
    }

    [Fact]
    public async Task CreacionEnvioYCierreGeneranRegistrosDeAuditoria()
    {
        (int cicloId, List<int> competencias) = await DatosEvaluacion.InsertarCicloAbiertoAsync(_fabrica, "AUD", 100m);
        HttpResponseMessage creada = await CrearAsync(cicloId, 1, 2);
        EvaluacionIdDto? id = await creada.Content.ReadFromJsonAsync<EvaluacionIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosEvaluacion.LeerAuditoriaAsync(_fabrica, "Evaluacion", id.Id), registro => registro.Accion == "Alta");
        await GuardarAsync(id.Id, null, (competencias[0], 5m));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id.Id}/envio", null)).StatusCode);
        RegistroAuditoria cambio = Assert.Single(
            await DatosEvaluacion.LeerAuditoriaAsync(_fabrica, "Evaluacion", id.Id),
            registro => registro.Accion == "Modificacion" && registro.Cambios.Contains("PuntajeFinal"));
        using JsonDocument cambios = JsonDocument.Parse(cambio.Cambios);
        List<string> campos = cambios.RootElement.EnumerateArray().Select(item => item.GetProperty("campo").GetString() ?? string.Empty).ToList();
        Assert.Contains("Estado", campos);
        Assert.Contains("PuntajeFinal", campos);
    }

    private async Task<HttpResponseMessage> CrearAsync(int? cicloId, int? empleadoId, int? evaluadorId)
    {
        return await _cliente.PostAsJsonAsync(RutaBase, new CrearEvaluacionRequestDto { CicloId = cicloId, EmpleadoId = empleadoId, EvaluadorId = evaluadorId });
    }

    private async Task AssertCrearAsync(int? cicloId, int? empleadoId, int? evaluadorId, int codigo, string mensaje)
    {
        await DatosEvaluacion.AssertErrorAsync(await CrearAsync(cicloId, empleadoId, evaluadorId), codigo, mensaje);
    }

    private async Task<HttpResponseMessage> GuardarAsync(int id, string? comentario, params (int CompetenciaId, decimal Calificacion)[] notas)
    {
        GuardarCalificacionesRequestDto solicitud = new GuardarCalificacionesRequestDto
        {
            Comentario = comentario,
            Calificaciones = notas
                .Select(nota => new CalificacionRequestDto { CompetenciaId = nota.CompetenciaId, Calificacion = nota.Calificacion })
                .ToList()
        };
        return await _cliente.PutAsJsonAsync($"{RutaBase}/{id}/calificaciones", solicitud);
    }

    private async Task CerrarCicloAsync(int cicloId)
    {
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaCiclos}/{cicloId}/cierre", null)).StatusCode);
    }

    private async Task<PaginaEvaluacionesDto> ListarAsync(string consulta)
    {
        PaginaEvaluacionesDto? pagina = await _cliente.GetFromJsonAsync<PaginaEvaluacionesDto>(RutaBase + consulta);
        Assert.NotNull(pagina);
        return pagina;
    }

    private async Task InsertarContratoAsync(int empleadoId, DateOnly inicio, DateOnly? fin, string estado)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobal.Infrastructure.Persistence.TitanGlobalDbContext contexto =
            alcance.ServiceProvider.GetRequiredService<TitanGlobal.Infrastructure.Persistence.TitanGlobalDbContext>();
        contexto.ContratosLaborales.Add(new ContratoLaboral
        {
            Numero = "CTR-" + Guid.NewGuid().ToString("N").Substring(0, 12),
            EmpleadoId = empleadoId,
            Modalidad = fin is null ? "Indeterminado" : "PlazoFijo",
            FechaInicio = inicio,
            FechaFin = fin,
            SueldoBasico = 3000m,
            Cargo = "Operario",
            Estado = estado
        });
        await contexto.SaveChangesAsync();
    }
}
