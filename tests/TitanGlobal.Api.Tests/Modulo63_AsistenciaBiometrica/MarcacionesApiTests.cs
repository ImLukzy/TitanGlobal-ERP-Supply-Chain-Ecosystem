using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo63_AsistenciaBiometrica;

[Collection("Modulo04")]
public class MarcacionesApiTests
{
    private const string RutaBase = "/api/v1/marcaciones";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public MarcacionesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosAsistencia.Restablecer(fabrica);
    }

    // Mensajes 1 a 5 del registro manual, uno por caso.
    [Theory]
    [InlineData("empleado-nulo", "El empleado es obligatorio.")]
    [InlineData("tipo-invalido", "El tipo de marcación debe ser Entrada o Salida.")]
    [InlineData("tipo-vacio", "El tipo de marcación debe ser Entrada o Salida.")]
    [InlineData("instante-nulo", "El instante de la marcación es obligatorio.")]
    [InlineData("observacion-vacia", "La observación es obligatoria en una marcación manual.")]
    [InlineData("observacion-larga", "La observación no puede superar 200 caracteres.")]
    public async Task RegistroManualConCamposInvalidosDevuelveMensajeLiteralSinCambios(string caso, string mensaje)
    {
        RegistrarMarcacionRequestDto solicitud = CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0));
        switch (caso)
        {
            case "empleado-nulo":
                solicitud.EmpleadoId = null;
                break;
            case "tipo-invalido":
                solicitud.Tipo = "Ingreso";
                break;
            case "tipo-vacio":
                solicitud.Tipo = "  ";
                break;
            case "instante-nulo":
                solicitud.Instante = null;
                break;
            case "observacion-vacia":
                solicitud.Observacion = "   ";
                break;
            default:
                solicitud.Observacion = new string('x', 201);
                break;
        }

        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, mensaje);
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
    }

    [Fact]
    public async Task RegistroManualConVariosErroresDevuelveElPrimeroEnElOrdenDeLaSpec()
    {
        RegistrarMarcacionRequestDto solicitud = new RegistrarMarcacionRequestDto();
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El empleado es obligatorio.");
        solicitud.EmpleadoId = 9999;
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El tipo de marcación debe ser Entrada o Salida.");
        solicitud.Tipo = "Entrada";
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El instante de la marcación es obligatorio.");
        solicitud.Instante = DatosAsistencia.Utc(2026, 1, 5, 13, 0);
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "La observación es obligatoria en una marcación manual.");
        solicitud.Observacion = "Olvido de marcar";
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 404, "El empleado 9999 no existe.");
    }

    [Fact]
    public async Task CuerpoVacioDevuelveElLiteralDelMiddleware()
    {
        string[] rutas = { RutaBase, RutaBase + "/importacion" };
        foreach (string ruta in rutas)
        {
            HttpResponseMessage respuesta = await _cliente.PostAsync(ruta, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
            await DatosAsistencia.AssertErrorAsync(respuesta, 400, "El cuerpo de la petición (Body) no puede estar vacío.");
        }
    }

    [Fact]
    public async Task RegistroManualDeEmpleadoCesadoDespuesDelCeseDevuelve409YEnLaFechaDeCeseFunciona()
    {
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        HttpResponseMessage rechazada = await _cliente.PostAsJsonAsync(RutaBase, CrearManual(4, "Entrada", DatosAsistencia.Utc(2026, 10, 5, 13, 0)));
        await DatosAsistencia.AssertErrorAsync(rechazada, 409, "El empleado 4 no está activo en esa fecha.");
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));

        // 2026-09-30 22:00 local (UTC-5) es 2026-10-01 03:00 UTC: la fecha local es la del cese y sí vale.
        HttpResponseMessage aceptada = await _cliente.PostAsJsonAsync(RutaBase, CrearManual(4, "Salida", DatosAsistencia.Utc(2026, 10, 1, 3, 0)));
        Assert.Equal(HttpStatusCode.OK, aceptada.StatusCode);
        // 2026-10-01 00:00 local es 05:00 UTC: ya es posterior al cese.
        HttpResponseMessage posterior = await _cliente.PostAsJsonAsync(RutaBase, CrearManual(4, "Salida", DatosAsistencia.Utc(2026, 10, 1, 5, 0)));
        await DatosAsistencia.AssertErrorAsync(posterior, 409, "El empleado 4 no está activo en esa fecha.");
    }

    [Fact]
    public async Task RegistroManualSinContratoVigenteDevuelve409()
    {
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        // El empleado 3 no tiene contrato.
        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsJsonAsync(RutaBase, CrearManual(3, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0))),
            409,
            "El empleado 3 no tiene un contrato vigente en esa fecha.");
        // El empleado 1 tiene contrato desde 2021-03-01.
        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2021, 2, 26, 13, 0))),
            409,
            "El empleado 1 no tiene un contrato vigente en esa fecha.");
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
    }

    [Fact]
    public async Task ElContratoAnuladoNoCuentaYElCierreTienePrioridadSobreElFinPrevisto()
    {
        await AgregarContratoAsync(10, "Anulado", null);
        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsJsonAsync(RutaBase, CrearManual(3, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0))),
            409,
            "El empleado 3 no tiene un contrato vigente en esa fecha.");

        await AgregarContratoAsync(11, "Cerrado", new DateOnly(2026, 6, 30));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearManual(3, "Entrada", DatosAsistencia.Utc(2026, 6, 30, 13, 0)))).StatusCode);
        await DatosAsistencia.AssertErrorAsync(
            await _cliente.PostAsJsonAsync(RutaBase, CrearManual(3, "Entrada", DatosAsistencia.Utc(2026, 7, 1, 13, 0))),
            409,
            "El empleado 3 no tiene un contrato vigente en esa fecha.");
    }

    [Fact]
    public async Task RegistroOEliminacionEnUnaJornadaConHorasExtraDecididasDevuelve409()
    {
        await DatosAsistencia.InsertarJornadaAsync(_fabrica, DatosAsistencia.CrearJornadaDia(1, new DateOnly(2026, 1, 5), 60, "Aprobada"));
        int manual = await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0)));
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        string mensaje = "La jornada del 2026-01-05 ya tiene las horas extra decididas y no se modifica.";
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0))), 409, mensaje);
        await DatosAsistencia.AssertErrorAsync(await _cliente.DeleteAsync($"{RutaBase}/{manual}"), 409, mensaje);
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
        // Otro día sin jornada decidida sí se registra.
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 6, 13, 0)))).StatusCode);
    }

    [Fact]
    public async Task RegistroManualDuplicadoEnElMismoMinutoDevuelve409YFueraDeSesentaSegundosFunciona()
    {
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        string mensaje = "Ya existe una marcación del mismo tipo en ese minuto.";
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0, 59))), 409, mensaje);
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 1, 0))), 409, mensaje);
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 1, 1)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Salida", DatosAsistencia.Utc(2026, 1, 5, 13, 0, 30)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, CrearManual(2, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0, 30)))).StatusCode);
    }

    [Fact]
    public async Task RegistroManualValidoGuardaElInstanteEnUtcYElOrigenManual()
    {
        HttpResponseMessage sinZona = await _cliente.PostAsJsonAsync(RutaBase, new
        {
            empleadoId = 1,
            tipo = "Entrada",
            instante = "2026-01-05T13:00:00",
            observacion = "  Olvido de marcar  "
        });
        Assert.Equal(HttpStatusCode.OK, sinZona.StatusCode);
        MarcacionIdDto? primera = await sinZona.Content.ReadFromJsonAsync<MarcacionIdDto>();
        Assert.NotNull(primera);
        HttpResponseMessage conDesfase = await _cliente.PostAsJsonAsync(RutaBase, new
        {
            empleadoId = 1,
            tipo = "Salida",
            instante = "2026-01-05T17:00:00-05:00",
            observacion = "Salida confirmada"
        });
        Assert.Equal(HttpStatusCode.OK, conDesfase.StatusCode);
        MarcacionIdDto? segunda = await conDesfase.Content.ReadFromJsonAsync<MarcacionIdDto>();
        Assert.NotNull(segunda);

        List<Marcacion> guardadas = await DatosAsistencia.LeerMarcacionesAsync(_fabrica);
        Marcacion entrada = guardadas.Single(marcacion => marcacion.Id == primera.Id);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 5, 13, 0), entrada.Instante);
        Assert.Equal("Manual", entrada.Origen);
        Assert.Equal("Olvido de marcar", entrada.Observacion);
        Assert.Null(entrada.Dispositivo);
        Assert.Null(entrada.NumeroRegistro);
        Marcacion salida = guardadas.Single(marcacion => marcacion.Id == segunda.Id);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 5, 22, 0), salida.Instante);
    }

    [Fact]
    public async Task ImportacionConDispositivoOCantidadInvalidaDevuelveMensajesLiterales()
    {
        ImportarMarcacionesRequestDto solicitud = new ImportarMarcacionesRequestDto { Registros = new List<RegistroRelojDto>() };
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase + "/importacion", solicitud), 400, "El dispositivo es obligatorio.");
        solicitud.Dispositivo = "RELOJ-01";
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase + "/importacion", solicitud), 400, "La importación debe tener entre 1 y 1000 registros.");
        solicitud.Registros = null;
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase + "/importacion", solicitud), 400, "La importación debe tener entre 1 y 1000 registros.");
        solicitud.Registros = Enumerable.Range(1, 1001)
            .Select(numero => CrearRegistro("N" + numero, "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0).AddMinutes(numero * 2)))
            .ToList();
        await DatosAsistencia.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase + "/importacion", solicitud), 400, "La importación debe tener entre 1 y 1000 registros.");
        Assert.Empty(await DatosAsistencia.LeerMarcacionesAsync(_fabrica));

        solicitud.Registros = solicitud.Registros.Take(1000).ToList();
        ResultadoImportacionDto resultado = await ImportarAsync(solicitud);
        Assert.Equal(1000, resultado.Recibidos);
        Assert.Equal(1000, resultado.Importados);
    }

    [Fact]
    public async Task ImportacionMixtaImportaLosValidosYRechazaConMotivoLiteralSinDetenerElResto()
    {
        ImportarMarcacionesRequestDto solicitud = new ImportarMarcacionesRequestDto
        {
            Dispositivo = "RELOJ-01",
            Registros = new List<RegistroRelojDto>
            {
                CrearRegistro("A1", "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)),
                CrearRegistro("A2", "EMP-000001", "Ingreso", DatosAsistencia.Utc(2026, 1, 5, 13, 0)),
                CrearRegistro("A3", "EMP-000001", "Entrada", null),
                CrearRegistro("A4", "EMP-999999", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)),
                CrearRegistro("A5", "EMP-000004", "Entrada", DatosAsistencia.Utc(2026, 10, 5, 13, 0)),
                CrearRegistro("A6", "EMP-000003", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)),
                CrearRegistro("A7", "EMP-000001", "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0))
            }
        };
        ResultadoImportacionDto resultado = await ImportarAsync(solicitud);
        Assert.Equal(7, resultado.Recibidos);
        Assert.Equal(2, resultado.Importados);
        Assert.Equal(0, resultado.YaImportados);
        Assert.Equal(0, resultado.Duplicados);
        string[] esperado =
        {
            "A2|El tipo de marcación debe ser Entrada o Salida.",
            "A3|El instante de la marcación es obligatorio.",
            "A4|No existe un empleado con el legajo EMP-999999.",
            "A5|El empleado 4 no está activo en esa fecha.",
            "A6|El empleado 3 no tiene un contrato vigente en esa fecha."
        };
        Assert.Equal(esperado, resultado.Rechazados.Select(rechazo => $"{rechazo.NumeroRegistro}|{rechazo.Motivo}").ToArray());
        List<Marcacion> guardadas = await DatosAsistencia.LeerMarcacionesAsync(_fabrica);
        Assert.Equal(2, guardadas.Count);
        Assert.All(guardadas, marcacion =>
        {
            Assert.Equal("Reloj", marcacion.Origen);
            Assert.Equal("RELOJ-01", marcacion.Dispositivo);
            Assert.Null(marcacion.Observacion);
        });
    }

    [Fact]
    public async Task ImportacionSinNumeroDeRegistroSeRechazaPorqueNoSePodriaDeduplicar()
    {
        ImportarMarcacionesRequestDto solicitud = new ImportarMarcacionesRequestDto
        {
            Dispositivo = "RELOJ-01",
            Registros = new List<RegistroRelojDto>
            {
                CrearRegistro(" ", "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)),
                CrearRegistro(new string('9', 51), "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 5))
            }
        };
        ResultadoImportacionDto resultado = await ImportarAsync(solicitud);
        Assert.Equal(0, resultado.Importados);
        Assert.Equal(2, resultado.Rechazados.Count);
        Assert.Equal("El número de registro es obligatorio.", resultado.Rechazados[0].Motivo);
        Assert.Equal("El número de registro no puede superar 50 caracteres.", resultado.Rechazados[1].Motivo);
    }

    [Fact]
    public async Task ImportacionEsIdempotenteElSegundoEnvioIgualQuedaTodoYaImportado()
    {
        ImportarMarcacionesRequestDto solicitud = new ImportarMarcacionesRequestDto
        {
            Dispositivo = "RELOJ-01",
            Registros = new List<RegistroRelojDto>
            {
                CrearRegistro("B1", "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)),
                CrearRegistro("B2", "EMP-000001", "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0)),
                CrearRegistro("B3", "EMP-000002", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0))
            }
        };
        ResultadoImportacionDto primero = await ImportarAsync(solicitud);
        Assert.Equal(3, primero.Importados);
        ResultadoImportacionDto segundo = await ImportarAsync(solicitud);
        Assert.Equal(3, segundo.Recibidos);
        Assert.Equal(0, segundo.Importados);
        Assert.Equal(3, segundo.YaImportados);
        Assert.Equal(0, segundo.Duplicados);
        Assert.Empty(segundo.Rechazados);
        Assert.Equal(3, (await DatosAsistencia.LeerMarcacionesAsync(_fabrica)).Count);

        // El mismo número en otro dispositivo es otro registro; el mismo número repetido en el envío cuenta una vez.
        solicitud.Dispositivo = "RELOJ-02";
        RegistroRelojDto repetido = CrearRegistro("B1", "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 30));
        solicitud.Registros = new List<RegistroRelojDto> { repetido, repetido };
        ResultadoImportacionDto otroDispositivo = await ImportarAsync(solicitud);
        Assert.Equal(1, otroDispositivo.Importados);
        Assert.Equal(1, otroDispositivo.YaImportados);
    }

    [Fact]
    public async Task ImportacionDescartaComoDuplicadasLasMarcacionesDentroDeSesentaSegundos()
    {
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(2, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        ImportarMarcacionesRequestDto solicitud = new ImportarMarcacionesRequestDto
        {
            Dispositivo = "RELOJ-01",
            Registros = new List<RegistroRelojDto>
            {
                CrearRegistro("C1", "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0, 0)),
                CrearRegistro("C2", "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0, 40)),
                CrearRegistro("C3", "EMP-000001", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 1, 30)),
                CrearRegistro("C4", "EMP-000001", "Salida", DatosAsistencia.Utc(2026, 1, 5, 13, 0, 20)),
                CrearRegistro("C5", "EMP-000002", "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0, 59))
            }
        };
        ResultadoImportacionDto resultado = await ImportarAsync(solicitud);
        // C2 duplica a C1 y C5 duplica la marcación ya guardada del empleado 2; C3 está a 90 s de C1 y a 50 s de C2 (descartada), así que entra.
        Assert.Equal(5, resultado.Recibidos);
        Assert.Equal(3, resultado.Importados);
        Assert.Equal(2, resultado.Duplicados);
        List<Marcacion> guardadas = await DatosAsistencia.LeerMarcacionesAsync(_fabrica);
        Assert.Equal(new[] { "C1", "C3", "C4" }, guardadas.Where(marcacion => marcacion.NumeroRegistro is not null).Select(marcacion => marcacion.NumeroRegistro!).ToArray());
    }

    [Fact]
    public async Task EliminarUnaMarcacionManualDevuelve204YLasDelRelojYLasInexistentesDevuelvenError()
    {
        int manual = await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        Marcacion reloj = DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0));
        reloj.Origen = "Reloj";
        reloj.Dispositivo = "RELOJ-01";
        reloj.NumeroRegistro = "Z1";
        reloj.Observacion = null;
        int idReloj = await DatosAsistencia.InsertarMarcacionAsync(_fabrica, reloj);
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        await DatosAsistencia.AssertErrorAsync(await _cliente.DeleteAsync(RutaBase + "/999"), 404, "La marcación 999 no existe.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.DeleteAsync($"{RutaBase}/{idReloj}"), 409, "Las marcaciones del reloj no se eliminan.");
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{RutaBase}/{manual}")).StatusCode);
        Assert.Equal(idReloj, Assert.Single(await DatosAsistencia.LeerMarcacionesAsync(_fabrica)).Id);
    }

    [Fact]
    public async Task ListadoAplicaFiltrosOrdenYPaginacion()
    {
        Marcacion reloj = DatosAsistencia.CrearMarcacion(2, "Salida", DatosAsistencia.Utc(2026, 1, 6, 22, 0));
        reloj.Origen = "Reloj";
        reloj.Dispositivo = "RELOJ-01";
        reloj.NumeroRegistro = "L1";
        reloj.Observacion = null;
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0)));
        await DatosAsistencia.InsertarMarcacionAsync(_fabrica, reloj);

        PaginaMarcacionesDto todas = await LeerPaginaAsync(string.Empty);
        Assert.Equal(3, todas.Total);
        Assert.Equal(new[] { 2, 1, 1 }, todas.Items.Select(item => item.EmpleadoId).ToArray());
        MarcacionDto primera = todas.Items[0];
        Assert.Equal("EMP-000002", primera.EmpleadoLegajo);
        Assert.Equal("Rojas Mendoza, Luis", primera.EmpleadoNombre);
        Assert.Equal("Reloj", primera.Origen);
        Assert.Equal("RELOJ-01", primera.Dispositivo);
        Assert.Equal("L1", primera.NumeroRegistro);

        Assert.Equal(2, (await LeerPaginaAsync("?empleadoId=1")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?tipo=Entrada")).Total);
        Assert.Equal(1, (await LeerPaginaAsync("?origen=Reloj")).Total);
        // Las fechas son locales (UTC-5): el día 2026-01-06 empieza a las 05:00 UTC.
        Assert.Equal(1, (await LeerPaginaAsync("?desde=2026-01-06&hasta=2026-01-06")).Total);
        Assert.Equal(2, (await LeerPaginaAsync("?desde=2026-01-05&hasta=2026-01-05")).Total);
        PaginaMarcacionesDto segunda = await LeerPaginaAsync("?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Single(segunda.Items);
    }

    [Fact]
    public async Task ListadoConParametrosInvalidosDevuelveMensajeLiteral()
    {
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tipo=Otro"), 400, "El filtro tipo no es válido.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?origen=Otro"), 400, "El filtro origen no es válido.");
        await DatosAsistencia.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?desde=2026-01-06&hasta=2026-01-05"), 400, "El filtro hasta no es válido.");
    }

    [Fact]
    public async Task SinTokenTodasLasOperacionesDevuelven401()
    {
        HttpClient anonimo = DatosAsistencia.ClienteAnonimo(_fabrica);
        await DatosAsistencia.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, DatosAsistencia.MensajeSinSesion);
        await DatosAsistencia.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0))), 401, DatosAsistencia.MensajeSinSesion);
        await DatosAsistencia.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/importacion", new ImportarMarcacionesRequestDto()), 401, DatosAsistencia.MensajeSinSesion);
        await DatosAsistencia.AssertErrorAsync(await anonimo.DeleteAsync(RutaBase + "/1"), 401, DatosAsistencia.MensajeSinSesion);
    }

    [Fact]
    public async Task TokenSinRolDeEscrituraDevuelve403EnLasTresEscriturasSinCambiarFilas()
    {
        int manual = await DatosAsistencia.InsertarMarcacionAsync(_fabrica, DatosAsistencia.CrearMarcacion(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        string[] antes = await DatosAsistencia.LeerFirmasAsync(_fabrica);
        HttpClient consulta = DatosAsistencia.ClienteConRoles(_fabrica, "CONSULTA");
        await DatosAsistencia.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, CrearManual(1, "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0))), 403, DatosAsistencia.MensajeSinPermiso);
        ImportarMarcacionesRequestDto importacion = new ImportarMarcacionesRequestDto
        {
            Dispositivo = "RELOJ-01",
            Registros = new List<RegistroRelojDto> { CrearRegistro("D1", "EMP-000001", "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0)) }
        };
        await DatosAsistencia.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase + "/importacion", importacion), 403, DatosAsistencia.MensajeSinPermiso);
        await DatosAsistencia.AssertErrorAsync(await consulta.DeleteAsync($"{RutaBase}/{manual}"), 403, DatosAsistencia.MensajeSinPermiso);
        Assert.Equal(antes, await DatosAsistencia.LeerFirmasAsync(_fabrica));
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenEscribirConEstadoOk(string rol)
    {
        HttpClient cliente = DatosAsistencia.ClienteConRoles(_fabrica, rol);
        HttpResponseMessage creada = await cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        MarcacionIdDto? marcacion = await creada.Content.ReadFromJsonAsync<MarcacionIdDto>();
        Assert.NotNull(marcacion);
        ImportarMarcacionesRequestDto importacion = new ImportarMarcacionesRequestDto
        {
            Dispositivo = "RELOJ-01",
            Registros = new List<RegistroRelojDto> { CrearRegistro("E-" + rol, "EMP-000001", "Salida", DatosAsistencia.Utc(2026, 1, 5, 22, 0)) }
        };
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(RutaBase + "/importacion", importacion)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"{RutaBase}/{marcacion.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("CONSULTA")]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task LecturaConCualquierTokenAutenticadoDevuelveOk(string rol)
    {
        HttpClient cliente = DatosAsistencia.ClienteConRoles(_fabrica, rol);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase)).StatusCode);
    }

    [Fact]
    public async Task AltaYBajaGeneranRegistrosDeAuditoria()
    {
        HttpResponseMessage creada = await _cliente.PostAsJsonAsync(RutaBase, CrearManual(1, "Entrada", DatosAsistencia.Utc(2026, 1, 5, 13, 0)));
        MarcacionIdDto? marcacion = await creada.Content.ReadFromJsonAsync<MarcacionIdDto>();
        Assert.NotNull(marcacion);
        Assert.Single(await DatosAsistencia.LeerAuditoriaAsync(_fabrica, "Marcacion", marcacion.Id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{RutaBase}/{marcacion.Id}")).StatusCode);
        List<RegistroAuditoria> registros = await DatosAsistencia.LeerAuditoriaAsync(_fabrica, "Marcacion", marcacion.Id);
        Assert.Contains(registros, registro => registro.Accion == "Baja");
    }

    private async Task AgregarContratoAsync(int id, string estado, DateOnly? cierre)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobal.Infrastructure.Persistence.TitanGlobalDbContext contexto =
            alcance.ServiceProvider.GetRequiredService<TitanGlobal.Infrastructure.Persistence.TitanGlobalDbContext>();
        contexto.ContratosLaborales.Add(new ContratoLaboral
        {
            Id = id,
            Numero = "CTR-" + id.ToString("D6", System.Globalization.CultureInfo.InvariantCulture),
            EmpleadoId = 3,
            Modalidad = "PlazoFijo",
            FechaInicio = new DateOnly(2025, 1, 1),
            FechaFin = new DateOnly(2027, 12, 31),
            SueldoBasico = 1500m,
            Cargo = "Almacenero",
            Estado = estado,
            FechaCierre = cierre
        });
        await contexto.SaveChangesAsync();
    }

    private static RegistrarMarcacionRequestDto CrearManual(int empleadoId, string tipo, DateTime instante)
    {
        return new RegistrarMarcacionRequestDto
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Instante = instante,
            Observacion = "Marcación manual de prueba"
        };
    }

    private static RegistroRelojDto CrearRegistro(string numero, string legajo, string tipo, DateTime? instante)
    {
        return new RegistroRelojDto { NumeroRegistro = numero, Legajo = legajo, Tipo = tipo, Instante = instante };
    }

    private async Task<ResultadoImportacionDto> ImportarAsync(ImportarMarcacionesRequestDto solicitud)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase + "/importacion", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ResultadoImportacionDto? resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoImportacionDto>();
        Assert.NotNull(resultado);
        return resultado;
    }

    private async Task<PaginaMarcacionesDto> LeerPaginaAsync(string consulta)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync(RutaBase + consulta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PaginaMarcacionesDto? pagina = await respuesta.Content.ReadFromJsonAsync<PaginaMarcacionesDto>();
        Assert.NotNull(pagina);
        return pagina;
    }
}
