using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo62_ContratosLaborales;

[Collection("Modulo04")]
public class ContratosLaboralesApiTests
{
    private const string RutaBase = "/api/v1/contratos-laborales";
    private const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";
    private const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public ContratosLaboralesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    public static IEnumerable<object[]> CasosDeCamposInvalidosEnAlta()
    {
        return new List<object[]>
        {
            new object[] { "empleadoId-nulo", "El empleado es obligatorio." },
            new object[] { "modalidad-otra", "La modalidad no es válida." },
            new object[] { "modalidad-minusculas", "La modalidad no es válida." },
            new object[] { "modalidad-vacia", "La modalidad no es válida." },
            new object[] { "inicio-nulo", "La fecha de inicio es obligatoria." },
            new object[] { "sueldo-nulo", "El sueldo básico es obligatorio." },
            new object[] { "sueldo-cero", "El sueldo básico debe ser mayor que cero." },
            new object[] { "sueldo-negativo", "El sueldo básico debe ser mayor que cero." },
            new object[] { "sueldo-exceso", "El sueldo básico no puede superar 9999999999.99." },
            new object[] { "sueldo-decimales", "El sueldo básico admite como máximo 2 decimales." },
            new object[] { "cargo-vacio", "El cargo es obligatorio." },
            new object[] { "cargo-espacios", "El cargo es obligatorio." },
            new object[] { "cargo-largo", "El cargo no puede superar 100 caracteres." },
            new object[] { "indeterminado-con-fin", "El contrato indeterminado no tiene fecha de fin." },
            new object[] { "plazo-sin-fin", "La fecha de fin es obligatoria para esta modalidad." },
            new object[] { "formativo-sin-fin", "La fecha de fin es obligatoria para esta modalidad." },
            new object[] { "fin-igual-inicio", "La fecha de fin debe ser posterior a la fecha de inicio." },
            new object[] { "fin-anterior-inicio", "La fecha de fin debe ser posterior a la fecha de inicio." }
        };
    }

    public static IEnumerable<object[]> CasosDeCamposInvalidosEnRenovacion()
    {
        return new List<object[]>
        {
            new object[] { "modalidad-otra", "La modalidad no es válida." },
            new object[] { "sueldo-nulo", "El sueldo básico es obligatorio." },
            new object[] { "sueldo-cero", "El sueldo básico debe ser mayor que cero." },
            new object[] { "sueldo-exceso", "El sueldo básico no puede superar 9999999999.99." },
            new object[] { "sueldo-decimales", "El sueldo básico admite como máximo 2 decimales." },
            new object[] { "cargo-vacio", "El cargo es obligatorio." },
            new object[] { "cargo-largo", "El cargo no puede superar 100 caracteres." },
            new object[] { "indeterminado-con-fin", "El contrato indeterminado no tiene fecha de fin." },
            new object[] { "plazo-sin-fin", "La fecha de fin es obligatoria para esta modalidad." }
        };
    }

    // T1: un caso por cada mensaje 1 a 12 en el alta.
    [Theory]
    [MemberData(nameof(CasosDeCamposInvalidosEnAlta))]
    public async Task AltaConCamposInvalidosDevuelveMensajeLiteralSinCrearFilas(string caso, string mensaje)
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        AplicarCasoEnAlta(caso, solicitud);
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        await AssertErrorAsync(respuesta, 400, mensaje);
        Assert.Equal(antes, await LeerFirmasAsync());
        Assert.Empty(antes);
    }

    [Fact]
    public async Task AltaConVariosErroresDevuelveElPrimeroEnElOrdenDeLaSpec()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.EmpleadoId = null;
        solicitud.Modalidad = "Otra";
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El empleado es obligatorio.");

        solicitud.EmpleadoId = 9999;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "La modalidad no es válida.");

        solicitud.Modalidad = "PlazoFijo";
        solicitud.SueldoBasico = null;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El sueldo básico es obligatorio.");
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task CuerpoVacioDevuelveElLiteralDelMiddleware()
    {
        string[] rutas = { RutaBase, RutaBase + "/1/renovacion", RutaBase + "/1/adendas", RutaBase + "/1/anulacion" };
        foreach (string ruta in rutas)
        {
            HttpResponseMessage respuesta = await _cliente.PostAsync(ruta, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
            await AssertErrorAsync(respuesta, 400, "El cuerpo de la petición (Body) no puede estar vacío.");
        }
    }

    // T2: alta feliz por modalidad.
    [Theory]
    [InlineData("Indeterminado")]
    [InlineData("PlazoFijo")]
    [InlineData("Formativo")]
    public async Task AltaValidaPorModalidadGuardaVigenteConNumeroAutogeneradoYFechasIdaYVuelta(string modalidad)
    {
        DateOnly hoy = Hoy();
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.Modalidad = modalidad;
        solicitud.FechaInicio = hoy.AddDays(-30);
        solicitud.FechaFin = modalidad == "Indeterminado" ? null : hoy.AddDays(335);
        solicitud.SueldoBasico = 4200.50m;
        ContratoLaboralCreadoDto creado = await CrearAsync(solicitud);
        Assert.Equal("CTR-" + creado.Id.ToString("D6", CultureInfo.InvariantCulture), creado.Numero);
        Assert.DoesNotContain("TMP-", creado.Numero);

        ContratoLaboral guardado = await ObtenerContratoAsync(creado.Id);
        Assert.Equal(creado.Numero, guardado.Numero);
        Assert.Equal("Vigente", guardado.Estado);
        Assert.Equal(modalidad, guardado.Modalidad);
        Assert.Equal(1, guardado.EmpleadoId);
        Assert.Null(guardado.ContratoAnteriorId);
        Assert.Null(guardado.FechaCierre);
        Assert.Null(guardado.MotivoCierre);

        HttpResponseMessage respuesta = await _cliente.GetAsync($"{RutaBase}/{creado.Id}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(hoy.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), documento.RootElement.GetProperty("fechaInicio").GetString());
        if (modalidad == "Indeterminado")
        {
            Assert.Equal(JsonValueKind.Null, documento.RootElement.GetProperty("fechaFin").ValueKind);
        }
        else
        {
            Assert.Equal(hoy.AddDays(335).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), documento.RootElement.GetProperty("fechaFin").GetString());
        }

        Assert.Equal(4200.50m, documento.RootElement.GetProperty("sueldoBasico").GetDecimal());
        Assert.Equal("Vigente", documento.RootElement.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task AltaNormalizaEspaciosDeModalidadYCargo()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.Modalidad = "  PlazoFijo ";
        solicitud.Cargo = "  Analista de Operaciones  ";
        ContratoLaboralCreadoDto creado = await CrearAsync(solicitud);
        ContratoLaboral guardado = await ObtenerContratoAsync(creado.Id);
        Assert.Equal("PlazoFijo", guardado.Modalidad);
        Assert.Equal("Analista de Operaciones", guardado.Cargo);
    }

    [Fact]
    public async Task SueldoConTresDecimalesOConDoceEnterosSeRechazaYNoSeRedondea()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.SueldoBasico = 0.001m;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El sueldo básico admite como máximo 2 decimales.");
        solicitud.SueldoBasico = 100000000000m;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El sueldo básico no puede superar 9999999999.99.");
        solicitud.SueldoBasico = 1234.567m;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 400, "El sueldo básico admite como máximo 2 decimales.");
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task SueldoMaximoYMinimoValidosSeGuardanSinCambios()
    {
        GuardarContratoLaboralRequestDto maximo = CrearSolicitudValida();
        maximo.EmpleadoId = 1;
        maximo.SueldoBasico = 9999999999.99m;
        ContratoLaboralCreadoDto creadoMaximo = await CrearAsync(maximo);
        Assert.Equal(9999999999.99m, (await ObtenerContratoAsync(creadoMaximo.Id)).SueldoBasico);
        GuardarContratoLaboralRequestDto minimo = CrearSolicitudValida();
        minimo.EmpleadoId = 2;
        minimo.SueldoBasico = 0.01m;
        ContratoLaboralCreadoDto creadoMinimo = await CrearAsync(minimo);
        Assert.Equal(0.01m, (await ObtenerContratoAsync(creadoMinimo.Id)).SueldoBasico);
    }

    // T3: C2, un solo contrato vigente por empleado.
    [Fact]
    public async Task SegundoAltaDelMismoEmpleadoConUnoVigenteDevuelveConflictoSinFilasNuevas()
    {
        await CrearAsync(CrearSolicitudValida());
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitudValida());
        await AssertErrorAsync(respuesta, 409, "El empleado 1 ya tiene un contrato vigente.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AltaParaOtroEmpleadoSiendoPosibleConUnoVigenteEnElPrimero()
    {
        await CrearAsync(CrearSolicitudValida());
        GuardarContratoLaboralRequestDto otro = CrearSolicitudValida();
        otro.EmpleadoId = 2;
        ContratoLaboralCreadoDto creado = await CrearAsync(otro);
        Assert.Equal(2, (await ObtenerContratoAsync(creado.Id)).EmpleadoId);
    }

    [Theory]
    [InlineData("Anulado")]
    [InlineData("Renovado")]
    [InlineData("Cerrado")]
    [InlineData("Vencido")]
    public async Task AltaTrasUnContratoNoVigentePermiteCrearElNuevo(string estadoAnterior)
    {
        await InsertarContratoAsync(CrearContrato(1001, 1, estadoAnterior, "PlazoFijo", new DateOnly(2022, 1, 1), new DateOnly(2022, 12, 31)));
        ContratoLaboralCreadoDto creado = await CrearAsync(CrearSolicitudValida());
        Assert.Equal("Vigente", (await ObtenerContratoAsync(creado.Id)).Estado);
        Assert.Equal(estadoAnterior, (await ObtenerContratoAsync(1001)).Estado);
    }

    [Fact]
    public async Task AltaTrasAnularUnContratoPermiteCrearElNuevo()
    {
        ContratoLaboralCreadoDto primero = await CrearAsync(CrearSolicitudValida());
        HttpResponseMessage anulacion = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{primero.Id}/anulacion",
            new AnularContratoLaboralRequestDto { Motivo = "Registro duplicado por error" });
        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
        ContratoLaboralCreadoDto segundo = await CrearAsync(CrearSolicitudValida());
        Assert.NotEqual(primero.Id, segundo.Id);
    }

    [Fact]
    public async Task AltaConContratoVigenteConFinPasadoLoDejaVencidoGuardadoYCreaElNuevo()
    {
        DateOnly hoy = Hoy();
        await InsertarContratoAsync(CrearContrato(1001, 1, "Vigente", "PlazoFijo", hoy.AddDays(-100), hoy.AddDays(-10)));
        ContratoLaboralCreadoDto creado = await CrearAsync(CrearSolicitudValida());
        ContratoLaboral anterior = await ObtenerContratoAsync(1001);
        Assert.Equal("Vencido", anterior.Estado);
        ContratoLaboral nuevo = await ObtenerContratoAsync(creado.Id);
        Assert.Equal("Vigente", nuevo.Estado);
    }

    [Fact]
    public async Task AltaConContratoVigenteSinFinOConFinFuturoDevuelveConflicto()
    {
        DateOnly hoy = Hoy();
        await InsertarContratoAsync(CrearContrato(1001, 1, "Vigente", "PlazoFijo", hoy.AddDays(-100), hoy.AddDays(5)));
        await InsertarContratoAsync(CrearContrato(1002, 2, "Vigente", "Indeterminado", hoy.AddDays(-100), null));
        string[] antes = await LeerFirmasAsync();
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, CrearSolicitudValida()), 409, "El empleado 1 ya tiene un contrato vigente.");
        GuardarContratoLaboralRequestDto sobreEmpleadoDos = CrearSolicitudValida();
        sobreEmpleadoDos.EmpleadoId = 2;
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, sobreEmpleadoDos), 409, "El empleado 2 ya tiene un contrato vigente.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    // T4: vínculo con el empleado (decisión 8 A).
    [Fact]
    public async Task AltaConEmpleadoInexistenteDevuelve404SinFilas()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.EmpleadoId = 9999;
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        await AssertErrorAsync(respuesta, 404, "El empleado 9999 no existe.");
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task AltaConEmpleadoCesadoDevuelveConflictoSinFilas()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.EmpleadoId = 4;
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        await AssertErrorAsync(respuesta, 409, "El empleado 4 está cesado y no admite contratos nuevos.");
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task AltaConInicioAnteriorAlIngresoDevuelve400SinFilas()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.FechaInicio = new DateOnly(2021, 2, 28);
        solicitud.FechaFin = new DateOnly(2022, 2, 27);
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        await AssertErrorAsync(respuesta, 400, "La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task AltaConInicioIgualAlIngresoEsValida()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.FechaInicio = new DateOnly(2021, 3, 1);
        solicitud.FechaFin = new DateOnly(2022, 2, 28);
        ContratoLaboralCreadoDto creado = await CrearAsync(solicitud);
        Assert.Equal(new DateOnly(2021, 3, 1), (await ObtenerContratoAsync(creado.Id)).FechaInicio);
    }

    [Fact]
    public async Task AltaConEmpleadoCesadoYInicioAnteriorAlIngresoDevuelveElConflictoPrimero()
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.EmpleadoId = 4;
        solicitud.FechaInicio = new DateOnly(2018, 1, 1);
        solicitud.FechaFin = new DateOnly(2019, 1, 1);
        await AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), 409, "El empleado 4 está cesado y no admite contratos nuevos.");
    }

    // T5: renovación (C4).
    [Fact]
    public async Task RenovarPlazoFijoCreaElNuevoElDiaSiguienteYDejaElAnteriorRenovado()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto anterior = await CrearContratoVigenteAsync(2, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        RenovarContratoLaboralRequestDto solicitud = CrearRenovacionValida(hoy.AddDays(60 + 365));
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/{anterior.Id}/renovacion", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ContratoLaboralCreadoDto? nuevoCreado = await respuesta.Content.ReadFromJsonAsync<ContratoLaboralCreadoDto>();
        Assert.NotNull(nuevoCreado);
        Assert.Equal("CTR-" + nuevoCreado.Id.ToString("D6", CultureInfo.InvariantCulture), nuevoCreado.Numero);

        ContratoLaboral nuevo = await ObtenerContratoAsync(nuevoCreado.Id);
        Assert.Equal(hoy.AddDays(61), nuevo.FechaInicio);
        Assert.Equal(hoy.AddDays(60 + 365), nuevo.FechaFin);
        Assert.Equal(anterior.Id, nuevo.ContratoAnteriorId);
        Assert.Equal("Vigente", nuevo.Estado);
        Assert.Equal(2, nuevo.EmpleadoId);
        Assert.Equal("PlazoFijo", nuevo.Modalidad);
        Assert.Equal(4500.00m, nuevo.SueldoBasico);
        Assert.Equal("Analista Senior", nuevo.Cargo);
        ContratoLaboral renovado = await ObtenerContratoAsync(anterior.Id);
        Assert.Equal("Renovado", renovado.Estado);
        Assert.Equal(hoy.AddDays(60), renovado.FechaFin);
    }

    [Fact]
    public async Task RenovarAPlazoIndeterminadoNoRequiereFechaFinYDejaElNuevoVigente()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto anterior = await CrearContratoVigenteAsync(2, "Formativo", hoy.AddDays(-30), hoy.AddDays(60));
        RenovarContratoLaboralRequestDto solicitud = CrearRenovacionValida(null);
        solicitud.Modalidad = "Indeterminado";
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/{anterior.Id}/renovacion", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ContratoLaboralCreadoDto? nuevoCreado = await respuesta.Content.ReadFromJsonAsync<ContratoLaboralCreadoDto>();
        Assert.NotNull(nuevoCreado);
        ContratoLaboral nuevo = await ObtenerContratoAsync(nuevoCreado.Id);
        Assert.Null(nuevo.FechaFin);
        Assert.Equal("Indeterminado", nuevo.Modalidad);
        Assert.Equal(hoy.AddDays(61), nuevo.FechaInicio);
    }

    [Fact]
    public async Task RenovarContratoIndeterminadoDevuelveConflicto()
    {
        ContratoLaboralCreadoDto anterior = await CrearContratoVigenteAsync(2, "Indeterminado", Hoy().AddDays(-30), null);
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{anterior.Id}/renovacion",
            CrearRenovacionValida(Hoy().AddDays(400)));
        await AssertErrorAsync(respuesta, 409, $"El contrato {anterior.Numero} es indeterminado y no se renueva.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task RenovarDosVecesElMismoContratoDevuelveConflictoSinFilasNuevas()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto anterior = await CrearContratoVigenteAsync(2, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        HttpResponseMessage primera = await _cliente.PostAsJsonAsync($"{RutaBase}/{anterior.Id}/renovacion", CrearRenovacionValida(hoy.AddDays(500)));
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage segunda = await _cliente.PostAsJsonAsync($"{RutaBase}/{anterior.Id}/renovacion", CrearRenovacionValida(hoy.AddDays(600)));
        await AssertErrorAsync(segunda, 409, $"El contrato {anterior.Numero} no está vigente y no se puede renovar.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Theory]
    [InlineData("Anulado")]
    [InlineData("Cerrado")]
    [InlineData("Vencido")]
    public async Task RenovarContratoNoVigenteDevuelveConflicto(string estado)
    {
        await InsertarContratoAsync(CrearContrato(1001, 2, estado, "PlazoFijo", new DateOnly(2022, 1, 1), new DateOnly(2022, 12, 31)));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/1001/renovacion", CrearRenovacionValida(new DateOnly(2024, 12, 31)));
        await AssertErrorAsync(respuesta, 409, "El contrato CTR-001001 no está vigente y no se puede renovar.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task RenovarConFinMenorOIgualAlInicioDerivadoDevuelve400SinCambios()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto anterior = await CrearContratoVigenteAsync(2, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        string[] antes = await LeerFirmasAsync();
        DateOnly[] finesInvalidos = { hoy.AddDays(60), hoy.AddDays(61), hoy.AddDays(10) };
        foreach (DateOnly fin in finesInvalidos)
        {
            HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/{anterior.Id}/renovacion", CrearRenovacionValida(fin));
            await AssertErrorAsync(respuesta, 400, "La fecha de fin debe ser posterior a la fecha de inicio.");
        }

        Assert.Equal(antes, await LeerFirmasAsync());
        Assert.Equal("Vigente", (await ObtenerContratoAsync(anterior.Id)).Estado);
    }

    [Fact]
    public async Task RenovarConFinUnDiaDespuesDelInicioDerivadoEsValido()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto anterior = await CrearContratoVigenteAsync(2, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/{anterior.Id}/renovacion", CrearRenovacionValida(hoy.AddDays(62)));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task RenovacionTardiaDeContratoVigenteConFinPasadoEmpiezaElDiaSiguienteAlFin()
    {
        DateOnly hoy = Hoy();
        await InsertarContratoAsync(CrearContrato(1001, 2, "Vigente", "PlazoFijo", hoy.AddDays(-100), hoy.AddDays(-10)));
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/1001/renovacion", CrearRenovacionValida(hoy.AddDays(300)));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ContratoLaboralCreadoDto? nuevoCreado = await respuesta.Content.ReadFromJsonAsync<ContratoLaboralCreadoDto>();
        Assert.NotNull(nuevoCreado);
        ContratoLaboral nuevo = await ObtenerContratoAsync(nuevoCreado.Id);
        Assert.Equal(hoy.AddDays(-9), nuevo.FechaInicio);
        Assert.Equal(1001, nuevo.ContratoAnteriorId);
        Assert.Equal("Renovado", (await ObtenerContratoAsync(1001)).Estado);
    }

    [Theory]
    [MemberData(nameof(CasosDeCamposInvalidosEnRenovacion))]
    public async Task RenovarConCamposInvalidosDevuelveLiteralSinCambiarFilas(string caso, string mensaje)
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto anterior = await CrearContratoVigenteAsync(2, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        RenovarContratoLaboralRequestDto solicitud = CrearRenovacionValida(hoy.AddDays(500));
        AplicarCasoEnRenovacion(caso, solicitud);
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/{anterior.Id}/renovacion", solicitud);
        await AssertErrorAsync(respuesta, 400, mensaje);
        Assert.Equal(antes, await LeerFirmasAsync());
        Assert.Equal("Vigente", (await ObtenerContratoAsync(anterior.Id)).Estado);
    }

    [Fact]
    public async Task RenovarContratoInexistenteDevuelve404YLaValidacionDeCamposVaAntes()
    {
        HttpResponseMessage inexistente = await _cliente.PostAsJsonAsync($"{RutaBase}/9999/renovacion", CrearRenovacionValida(Hoy().AddDays(400)));
        await AssertErrorAsync(inexistente, 404, "El contrato laboral 9999 no existe.");
        RenovarContratoLaboralRequestDto invalida = CrearRenovacionValida(Hoy().AddDays(400));
        invalida.Cargo = " ";
        HttpResponseMessage conCampoInvalido = await _cliente.PostAsJsonAsync($"{RutaBase}/9999/renovacion", invalida);
        await AssertErrorAsync(conCampoInvalido, 400, "El cargo es obligatorio.");
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task RenovarContratoDeEmpleadoCesadoDevuelveConflicto()
    {
        DateOnly hoy = Hoy();
        await InsertarContratoAsync(CrearContrato(1001, 4, "Vigente", "PlazoFijo", hoy.AddDays(-100), hoy.AddDays(100)));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/1001/renovacion", CrearRenovacionValida(hoy.AddDays(500)));
        await AssertErrorAsync(respuesta, 409, "El empleado 4 está cesado y no admite contratos nuevos.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    // T6: adendas (C5).
    [Fact]
    public async Task AdendaCambiaSoloCargoSoloSueldoYAmbosSinTocarElContrato()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", hoy.AddDays(-100), null);
        ContratoLaboral original = await ObtenerContratoAsync(contrato.Id);
        int soloCargo = await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-50), "Jefe de Operaciones", null);
        int soloSueldo = await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-20), null, 6000m);
        int ambos = await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-5), "Gerente de Operaciones", 7500.25m);

        AdendaContrato primera = await ObtenerAdendaAsync(soloCargo);
        Assert.Equal("Jefe de Operaciones", primera.Cargo);
        Assert.Equal(original.SueldoBasico, primera.SueldoBasico);
        Assert.Equal(hoy.AddDays(-50), primera.FechaVigencia);
        AdendaContrato segunda = await ObtenerAdendaAsync(soloSueldo);
        Assert.Equal("Jefe de Operaciones", segunda.Cargo);
        Assert.Equal(6000m, segunda.SueldoBasico);
        AdendaContrato tercera = await ObtenerAdendaAsync(ambos);
        Assert.Equal("Gerente de Operaciones", tercera.Cargo);
        Assert.Equal(7500.25m, tercera.SueldoBasico);

        ContratoLaboral despues = await ObtenerContratoAsync(contrato.Id);
        Assert.Equal(original.Cargo, despues.Cargo);
        Assert.Equal(original.SueldoBasico, despues.SueldoBasico);
        Assert.Equal(original.Modalidad, despues.Modalidad);
        Assert.Equal(original.FechaInicio, despues.FechaInicio);
        Assert.Equal(original.FechaFin, despues.FechaFin);
        Assert.Equal("Vigente", despues.Estado);

        ContratoLaboralDetalleDto? detalle = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/{contrato.Id}");
        Assert.NotNull(detalle);
        Assert.Equal("Gerente de Operaciones", detalle.CargoVigente);
        Assert.Equal(7500.25m, detalle.SueldoVigente);
        Assert.Equal(original.Cargo, detalle.Cargo);
        Assert.Equal(original.SueldoBasico, detalle.SueldoBasico);
        Assert.Equal(new[] { soloCargo, soloSueldo, ambos }, detalle.Adendas.Select(adenda => adenda.Id).ToArray());
    }

    [Fact]
    public async Task AdendaConVigenciaFuturaNoCambiaElCargoNiElSueldoVigentesDeHoy()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", hoy.AddDays(-100), null);
        await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-10), "Analista Senior", 5000m);
        await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(3650), "Director", 20000m);
        ContratoLaboralDetalleDto? detalle = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/{contrato.Id}");
        Assert.NotNull(detalle);
        Assert.Equal("Analista Senior", detalle.CargoVigente);
        Assert.Equal(5000m, detalle.SueldoVigente);
        Assert.Equal(2, detalle.Adendas.Count);
    }

    [Fact]
    public async Task AdendaSinFechaDeVigenciaDevuelveLiteralSinCambiarFilas()
    {
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", Hoy().AddDays(-100), null);
        string[] antes = await LeerFirmasAsync();
        AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = null, Cargo = "Jefe", SueldoBasico = 5000m };
        await AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/adendas", solicitud), 400, "La fecha de vigencia es obligatoria.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public async Task AdendaSinCargoNiSueldoDevuelveLiteralSinCambiarFilas(string? cargo, decimal? sueldo)
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", hoy.AddDays(-100), null);
        string[] antes = await LeerFirmasAsync();
        AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = hoy, Cargo = cargo, SueldoBasico = sueldo };
        await AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/adendas", solicitud), 400, "La adenda debe cambiar el cargo o el sueldo básico.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Theory]
    [InlineData("sueldo-cero", "El sueldo básico debe ser mayor que cero.")]
    [InlineData("sueldo-negativo", "El sueldo básico debe ser mayor que cero.")]
    [InlineData("sueldo-exceso", "El sueldo básico no puede superar 9999999999.99.")]
    [InlineData("sueldo-decimales", "El sueldo básico admite como máximo 2 decimales.")]
    [InlineData("cargo-largo", "El cargo no puede superar 100 caracteres.")]
    public async Task AdendaConCargoOSueldoInvalidoDevuelveLiteralSinCambiarFilas(string caso, string mensaje)
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", hoy.AddDays(-100), null);
        AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = hoy };
        switch (caso)
        {
            case "sueldo-cero":
                solicitud.SueldoBasico = 0m;
                break;
            case "sueldo-negativo":
                solicitud.SueldoBasico = -5m;
                break;
            case "sueldo-exceso":
                solicitud.SueldoBasico = 10000000000m;
                break;
            case "sueldo-decimales":
                solicitud.SueldoBasico = 5000.123m;
                break;
            case "cargo-largo":
                solicitud.Cargo = new string('C', 101);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(caso), caso, "Caso de prueba desconocido.");
        }

        string[] antes = await LeerFirmasAsync();
        await AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/adendas", solicitud), 400, mensaje);
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AdendaSobreContratoInexistenteDevuelve404()
    {
        AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = Hoy(), Cargo = "Jefe" };
        await AssertErrorAsync(await _cliente.PostAsJsonAsync($"{RutaBase}/9999/adendas", solicitud), 404, "El contrato laboral 9999 no existe.");
    }

    [Theory]
    [InlineData("Renovado")]
    [InlineData("Anulado")]
    [InlineData("Cerrado")]
    [InlineData("Vencido")]
    public async Task AdendaSobreContratoNoVigenteDevuelveConflictoSinCambiarFilas(string estado)
    {
        await InsertarContratoAsync(CrearContrato(1001, 1, estado, "Indeterminado", new DateOnly(2022, 1, 1), null));
        string[] antes = await LeerFirmasAsync();
        AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = new DateOnly(2022, 6, 1), Cargo = "Jefe" };
        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/1001/adendas", solicitud),
            409,
            "El contrato CTR-001001 no está vigente y no admite adendas.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AdendaConVigenciaFueraDelPeriodoDelContratoDevuelve400SinCambiarFilas()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto plazo = await CrearContratoVigenteAsync(1, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        string[] antes = await LeerFirmasAsync();
        DateOnly[] fueras = { hoy.AddDays(-31), hoy.AddDays(61) };
        foreach (DateOnly fuera in fueras)
        {
            AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = fuera, Cargo = "Jefe" };
            await AssertErrorAsync(
                await _cliente.PostAsJsonAsync($"{RutaBase}/{plazo.Id}/adendas", solicitud),
                400,
                "La fecha de vigencia debe estar dentro del periodo del contrato.");
        }

        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AdendaEnLosLimitesDelPeriodoEsValidaYElIndeterminadoNoTieneTopeSuperior()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto plazo = await CrearContratoVigenteAsync(1, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        await AgregarAdendaValidaAsync(plazo.Id, hoy.AddDays(-30), "Jefe", null);
        await AgregarAdendaValidaAsync(plazo.Id, hoy.AddDays(60), "Gerente", null);
        ContratoLaboralCreadoDto indeterminado = await CrearContratoVigenteAsync(2, "Indeterminado", hoy.AddDays(-30), null);
        await AgregarAdendaValidaAsync(indeterminado.Id, hoy.AddDays(100000), "Gerente", null);
    }

    [Fact]
    public async Task AdendaConVigenciaIgualOAnteriorALaUltimaDevuelveConflictoSinCambiarFilas()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", hoy.AddDays(-100), null);
        await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-20), "Jefe", 5000m);
        string[] antes = await LeerFirmasAsync();
        DateOnly[] noPosteriores = { hoy.AddDays(-20), hoy.AddDays(-30) };
        foreach (DateOnly fecha in noPosteriores)
        {
            AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = fecha, Cargo = "Gerente", SueldoBasico = 9000m };
            await AssertErrorAsync(
                await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/adendas", solicitud),
                409,
                "Ya existe una adenda con fecha de vigencia igual o posterior.");
        }

        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AdendaQueNoCambiaCargoNiSueldoVigentesDevuelve400SinCambiarFilas()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", hoy.AddDays(-100), null);
        ContratoLaboral original = await ObtenerContratoAsync(contrato.Id);
        string[] antes = await LeerFirmasAsync();
        AdendaContratoRequestDto igualAlContrato = new AdendaContratoRequestDto
        {
            FechaVigencia = hoy.AddDays(-50),
            Cargo = original.Cargo,
            SueldoBasico = original.SueldoBasico
        };
        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/adendas", igualAlContrato),
            400,
            "La adenda no cambia el cargo ni el sueldo vigentes en esa fecha.");
        Assert.Equal(antes, await LeerFirmasAsync());

        await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-40), "Jefe", 6000m);
        string[] conAdenda = await LeerFirmasAsync();
        AdendaContratoRequestDto igualALaAdenda = new AdendaContratoRequestDto { FechaVigencia = hoy.AddDays(-10), Cargo = "Jefe", SueldoBasico = 6000m };
        await AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/adendas", igualALaAdenda),
            400,
            "La adenda no cambia el cargo ni el sueldo vigentes en esa fecha.");
        Assert.Equal(conAdenda, await LeerFirmasAsync());
    }

    // T7: anulación (decisión 5 A).
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnularSinMotivoDevuelveLiteralSinCambiarFilas(string? motivo)
    {
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "PlazoFijo", Hoy().AddDays(-30), Hoy().AddDays(60));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/anulacion", new AnularContratoLaboralRequestDto { Motivo = motivo });
        await AssertErrorAsync(respuesta, 400, "El motivo de anulación es obligatorio.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AnularConMotivoMayorDeTrescientosCaracteresDevuelveLiteralSinCambiarFilas()
    {
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "PlazoFijo", Hoy().AddDays(-30), Hoy().AddDays(60));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{contrato.Id}/anulacion",
            new AnularContratoLaboralRequestDto { Motivo = new string('M', 301) });
        await AssertErrorAsync(respuesta, 400, "El motivo de anulación no puede superar 300 caracteres.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AnularContratoInexistenteDevuelve404YElMotivoSeValidaAntes()
    {
        HttpResponseMessage inexistente = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/9999/anulacion",
            new AnularContratoLaboralRequestDto { Motivo = "Registro duplicado" });
        await AssertErrorAsync(inexistente, 404, "El contrato laboral 9999 no existe.");
        HttpResponseMessage sinMotivo = await _cliente.PostAsJsonAsync($"{RutaBase}/9999/anulacion", new AnularContratoLaboralRequestDto());
        await AssertErrorAsync(sinMotivo, 400, "El motivo de anulación es obligatorio.");
    }

    [Theory]
    [InlineData("Renovado")]
    [InlineData("Anulado")]
    [InlineData("Cerrado")]
    [InlineData("Vencido")]
    public async Task AnularContratoNoVigenteDevuelveConflictoSinCambiarFilas(string estado)
    {
        await InsertarContratoAsync(CrearContrato(1001, 1, estado, "PlazoFijo", new DateOnly(2022, 1, 1), new DateOnly(2022, 12, 31)));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/1001/anulacion", new AnularContratoLaboralRequestDto { Motivo = "Registro duplicado" });
        await AssertErrorAsync(respuesta, 409, "El contrato CTR-001001 no está vigente y no se puede anular.");
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Fact]
    public async Task AnularDosVecesElMismoContratoDevuelveConflictoLaSegundaVez()
    {
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "PlazoFijo", Hoy().AddDays(-30), Hoy().AddDays(60));
        AnularContratoLaboralRequestDto solicitud = new AnularContratoLaboralRequestDto { Motivo = "Registro duplicado" };
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/anulacion", solicitud)).StatusCode);
        HttpResponseMessage segunda = await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/anulacion", solicitud);
        await AssertErrorAsync(segunda, 409, $"El contrato {contrato.Numero} no está vigente y no se puede anular.");
    }

    [Fact]
    public async Task AnularContratoVigenteGuardaEstadoMotivoYFechaDeCierreYConservaLasAdendas()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "Indeterminado", hoy.AddDays(-100), null);
        int adendaId = await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-10), "Jefe", 6000m);
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{contrato.Id}/anulacion",
            new AnularContratoLaboralRequestDto { Motivo = "  Registro duplicado por error  " });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ContratoLaboralIdDto? resultado = await respuesta.Content.ReadFromJsonAsync<ContratoLaboralIdDto>();
        Assert.NotNull(resultado);
        Assert.Equal(contrato.Id, resultado.Id);
        ContratoLaboral anulado = await ObtenerContratoAsync(contrato.Id);
        Assert.Equal("Anulado", anulado.Estado);
        Assert.Equal("Registro duplicado por error", anulado.MotivoCierre);
        Assert.Equal(hoy, anulado.FechaCierre);
        AdendaContrato adenda = await ObtenerAdendaAsync(adendaId);
        Assert.Equal(contrato.Id, adenda.ContratoLaboralId);
        ContratoLaboralDetalleDto? detalle = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/{contrato.Id}");
        Assert.NotNull(detalle);
        Assert.Equal("Anulado", detalle.Estado);
        Assert.Single(detalle.Adendas);
    }

    // T8: listado.
    [Theory]
    [InlineData("ctr-000004", "4")]
    [InlineData("CTR-000004", "4")]
    [InlineData("almacenero", "5")]
    [InlineData("GERENTE", "7,1")]
    [InlineData("luis", "4,3,2")]
    [InlineData("TORRES", "7,1")]
    [InlineData("salazar", "5")]
    [InlineData("emp-000003", "5")]
    [InlineData("  Rojas ", "4,3,2")]
    [InlineData("no-existe", "")]
    public async Task BusquedaEncuentraPorNumeroCargoNombreApellidoYLegajoSinDistinguirMayusculas(string busqueda, string idsEsperados)
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto pagina = await LeerPaginaAsync("busqueda=" + Uri.EscapeDataString(busqueda));
        Assert.Equal(ConvertirIds(idsEsperados), pagina.Items.Select(item => item.Id).ToArray());
        Assert.Equal(ConvertirIds(idsEsperados).Length, pagina.Total);
    }

    [Theory]
    [InlineData("Vigente", "4,1")]
    [InlineData("Vencido", "5,8")]
    [InlineData("Renovado", "3,2")]
    [InlineData("Cerrado", "6")]
    [InlineData("Anulado", "7")]
    public async Task FiltroPorEstadoUsaElEstadoEfectivoConVencidoCalculado(string estado, string idsEsperados)
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto pagina = await LeerPaginaAsync("estado=" + estado);
        Assert.Equal(ConvertirIds(idsEsperados), pagina.Items.Select(item => item.Id).ToArray());
        Assert.Equal(ConvertirIds(idsEsperados).Length, pagina.Total);
        Assert.All(pagina.Items, item => Assert.Equal(estado, item.Estado));
    }

    [Theory]
    [InlineData("Indeterminado", "4")]
    [InlineData("PlazoFijo", "7,6,1,3,2,8")]
    [InlineData("Formativo", "5")]
    public async Task FiltroPorModalidadDevuelveSoloEsaModalidad(string modalidad, string idsEsperados)
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto pagina = await LeerPaginaAsync("modalidad=" + modalidad);
        Assert.Equal(ConvertirIds(idsEsperados), pagina.Items.Select(item => item.Id).ToArray());
        Assert.All(pagina.Items, item => Assert.Equal(modalidad, item.Modalidad));
    }

    [Fact]
    public async Task FiltroPorEmpleadoYCombinacionDeFiltros()
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto deEmpleadoDos = await LeerPaginaAsync("empleadoId=2");
        Assert.Equal(new[] { 4, 3, 2 }, deEmpleadoDos.Items.Select(item => item.Id).ToArray());
        PaginaContratosLaboralesDto combinado = await LeerPaginaAsync("empleadoId=2&estado=Renovado&modalidad=PlazoFijo");
        Assert.Equal(new[] { 3, 2 }, combinado.Items.Select(item => item.Id).ToArray());
        PaginaContratosLaboralesDto sinCoincidencias = await LeerPaginaAsync("empleadoId=9999");
        Assert.Equal(0, sinCoincidencias.Total);
        Assert.Empty(sinCoincidencias.Items);
    }

    [Fact]
    public async Task ListadoUsaTamanoPorDefectoDeDiezOrdenaPorInicioDescendenteEIdDescendenteYDevuelveTotal()
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto pagina = await LeerPaginaAsync(string.Empty);
        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(10, pagina.TamanoPagina);
        Assert.Equal(8, pagina.Total);
        Assert.Equal(new[] { 7, 6, 5, 4, 1, 3, 2, 8 }, pagina.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task ListadoPaginaConTotalGlobalYAceptaTamanoCien()
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto segunda = await LeerPaginaAsync("pagina=2&tamanoPagina=3");
        Assert.Equal(2, segunda.Pagina);
        Assert.Equal(3, segunda.TamanoPagina);
        Assert.Equal(8, segunda.Total);
        Assert.Equal(new[] { 4, 1, 3 }, segunda.Items.Select(item => item.Id).ToArray());
        PaginaContratosLaboralesDto grande = await LeerPaginaAsync("tamanoPagina=100");
        Assert.Equal(100, grande.TamanoPagina);
        Assert.Equal(8, grande.Items.Count);
        PaginaContratosLaboralesDto fueraDeRango = await LeerPaginaAsync("pagina=5&tamanoPagina=3");
        Assert.Equal(8, fueraDeRango.Total);
        Assert.Empty(fueraDeRango.Items);
    }

    [Fact]
    public async Task ListadoTrataEstadoYModalidadVaciosComoOmitidos()
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto pagina = await LeerPaginaAsync("estado=&modalidad=&busqueda=");
        Assert.Equal(8, pagina.Total);
    }

    [Fact]
    public async Task ListadoProyectaEmpleadoYValoresOriginalesDelContrato()
    {
        await InsertarConjuntoDeListadoAsync();
        await InsertarAdendaAsync(new AdendaContrato
        {
            Id = 1,
            ContratoLaboralId = 1,
            FechaVigencia = new DateOnly(2024, 1, 1),
            Cargo = "Gerente Comercial",
            SueldoBasico = 9500m
        });
        PaginaContratosLaboralesDto pagina = await LeerPaginaAsync("empleadoId=1&estado=Vigente");
        ContratoLaboralListadoDto item = Assert.Single(pagina.Items);
        Assert.Equal(1, item.Id);
        Assert.Equal("CTR-000001", item.Numero);
        Assert.Equal(1, item.EmpleadoId);
        Assert.Equal("EMP-000001", item.EmpleadoLegajo);
        Assert.Equal("Torres Vega, Ana", item.EmpleadoNombre);
        Assert.Equal("PlazoFijo", item.Modalidad);
        Assert.Equal("Gerente de Ventas", item.Cargo);
        Assert.Equal(8500m, item.SueldoBasico);
        Assert.Equal("Vigente", item.Estado);
    }

    [Fact]
    public async Task ListadoDevuelveVencidoParaContratoVigenteConFinPasadoYGuardadoSigueVigente()
    {
        await InsertarConjuntoDeListadoAsync();
        PaginaContratosLaboralesDto pagina = await LeerPaginaAsync("empleadoId=3");
        ContratoLaboralListadoDto item = Assert.Single(pagina.Items);
        Assert.Equal("Vencido", item.Estado);
        Assert.Equal("Vigente", (await ObtenerContratoAsync(5)).Estado);
    }

    [Theory]
    [InlineData("pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("pagina=-1", "La página debe ser mayor o igual a 1.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Vigente, Renovado, Vencido, Cerrado o Anulado.")]
    [InlineData("estado=vigente", "El estado debe ser Vigente, Renovado, Vencido, Cerrado o Anulado.")]
    [InlineData("modalidad=Otra", "La modalidad debe ser Indeterminado, PlazoFijo o Formativo.")]
    [InlineData("modalidad=plazofijo", "La modalidad debe ser Indeterminado, PlazoFijo o Formativo.")]
    public async Task ParametrosInvalidosDelListadoDevuelvenLiteral(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync($"{RutaBase}?{consulta}");
        await AssertErrorAsync(respuesta, 400, mensaje);
    }

    // T9: detalle.
    [Fact]
    public async Task DetalleDeContratoInexistenteDevuelveLiteral404()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync($"{RutaBase}/9999");
        await AssertErrorAsync(respuesta, 404, "El contrato laboral 9999 no existe.");
    }

    [Fact]
    public async Task DetalleDevuelveAdendasOrdenadasPorVigenciaYCargoYSueldoVigentesAHoy()
    {
        await InsertarConjuntoDeListadoAsync();
        DateOnly futura = Hoy().AddDays(30);
        await InsertarAdendaAsync(new AdendaContrato { Id = 3, ContratoLaboralId = 4, FechaVigencia = futura, Cargo = "Director", SueldoBasico = 15000m });
        await InsertarAdendaAsync(new AdendaContrato { Id = 2, ContratoLaboralId = 4, FechaVigencia = new DateOnly(2024, 6, 1), Cargo = "Jefe de Compras", SueldoBasico = 6000m });
        await InsertarAdendaAsync(new AdendaContrato { Id = 1, ContratoLaboralId = 4, FechaVigencia = new DateOnly(2024, 1, 1), Cargo = "Analista Senior", SueldoBasico = 4800m });
        ContratoLaboralDetalleDto? detalle = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/4");
        Assert.NotNull(detalle);
        Assert.Equal(new[] { 1, 2, 3 }, detalle.Adendas.Select(adenda => adenda.Id).ToArray());
        Assert.Equal(futura, detalle.Adendas[2].FechaVigencia);
        Assert.Equal("Jefe de Compras", detalle.CargoVigente);
        Assert.Equal(6000m, detalle.SueldoVigente);
        Assert.Equal("Analista de Compras", detalle.Cargo);
        Assert.Equal(4200m, detalle.SueldoBasico);
        Assert.Equal("EMP-000002", detalle.EmpleadoLegajo);
        Assert.Equal("Rojas Mendoza, Luis", detalle.EmpleadoNombre);
    }

    [Fact]
    public async Task DetalleSinAdendasUsaCargoYSueldoDelContrato()
    {
        await InsertarConjuntoDeListadoAsync();
        ContratoLaboralDetalleDto? detalle = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/1");
        Assert.NotNull(detalle);
        Assert.Empty(detalle.Adendas);
        Assert.Equal("Gerente de Ventas", detalle.CargoVigente);
        Assert.Equal(8500m, detalle.SueldoVigente);
    }

    [Fact]
    public async Task DetalleDevuelveEstadoEfectivoVencidoYDatosDeCierreYContratoAnterior()
    {
        await InsertarConjuntoDeListadoAsync();
        ContratoLaboralDetalleDto? vencido = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/5");
        Assert.NotNull(vencido);
        Assert.Equal("Vencido", vencido.Estado);
        ContratoLaboralDetalleDto? cerrado = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/6");
        Assert.NotNull(cerrado);
        Assert.Equal("Cerrado", cerrado.Estado);
        Assert.Equal(new DateOnly(2026, 9, 30), cerrado.FechaCierre);
        Assert.Equal("Cese del empleado", cerrado.MotivoCierre);
        ContratoLaboralDetalleDto? renovado = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/3");
        Assert.NotNull(renovado);
        Assert.Equal(2, renovado.ContratoAnteriorId);
        Assert.Equal("CTR-000002", renovado.ContratoAnteriorNumero);
        ContratoLaboralDetalleDto? sinAnterior = await _cliente.GetFromJsonAsync<ContratoLaboralDetalleDto>($"{RutaBase}/2");
        Assert.NotNull(sinAnterior);
        Assert.Null(sinAnterior.ContratoAnteriorId);
        Assert.Null(sinAnterior.ContratoAnteriorNumero);
    }

    // T10: sin PUT ni DELETE (T3).
    [Fact]
    public async Task PutYDeleteNoModificanNiEliminanElContrato()
    {
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "PlazoFijo", Hoy().AddDays(-30), Hoy().AddDays(60));
        string[] antes = await LeerFirmasAsync();
        HttpResponseMessage put = await _cliente.PutAsJsonAsync($"{RutaBase}/{contrato.Id}", CrearSolicitudValida());
        Assert.False(put.IsSuccessStatusCode);
        HttpResponseMessage delete = await _cliente.DeleteAsync($"{RutaBase}/{contrato.Id}");
        Assert.False(delete.IsSuccessStatusCode);
        Assert.Equal(antes, await LeerFirmasAsync());
        Assert.Equal("Vigente", (await ObtenerContratoAsync(contrato.Id)).Estado);
    }

    [Fact]
    public async Task OpenApiIncluyeLasOperacionesDeContratosLaboralesYNoPutNiDelete()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");
        Assert.True(rutas.GetProperty(RutaBase).TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty(RutaBase).TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(RutaBase + "/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty(RutaBase + "/{id}/renovacion").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(RutaBase + "/{id}/adendas").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty(RutaBase + "/{id}/anulacion").TryGetProperty("post", out _));
        foreach (JsonProperty ruta in rutas.EnumerateObject().Where(entrada => entrada.Name.StartsWith(RutaBase, StringComparison.Ordinal)))
        {
            Assert.False(ruta.Value.TryGetProperty("put", out _));
            Assert.False(ruta.Value.TryGetProperty("delete", out _));
        }
    }

    // T11: autenticación y roles (decisión 6 A).
    [Fact]
    public async Task SinTokenTodasLasOperacionesDevuelven401()
    {
        HttpClient anonimo = ClienteAnonimo();
        await AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/1"), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, CrearSolicitudValida()), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/1/renovacion", CrearRenovacionValida(Hoy().AddDays(400))), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/1/adendas", new AdendaContratoRequestDto { FechaVigencia = Hoy(), Cargo = "Jefe" }), 401, MensajeSinSesion);
        await AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/1/anulacion", new AnularContratoLaboralRequestDto { Motivo = "Error" }), 401, MensajeSinSesion);
        Assert.Empty(await LeerFirmasAsync());
    }

    [Fact]
    public async Task TokenSinRolDeEscrituraDevuelve403EnLasCuatroEscriturasSinCambiarFilas()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(2, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        string[] antes = await LeerFirmasAsync();
        HttpClient consulta = ClienteConRoles("CONSULTA");
        await AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, CrearSolicitudValida()), 403, MensajeSinPermiso);
        await AssertErrorAsync(await consulta.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/renovacion", CrearRenovacionValida(hoy.AddDays(400))), 403, MensajeSinPermiso);
        await AssertErrorAsync(
            await consulta.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/adendas", new AdendaContratoRequestDto { FechaVigencia = hoy, Cargo = "Jefe" }),
            403,
            MensajeSinPermiso);
        await AssertErrorAsync(
            await consulta.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/anulacion", new AnularContratoLaboralRequestDto { Motivo = "Error" }),
            403,
            MensajeSinPermiso);
        Assert.Equal(antes, await LeerFirmasAsync());
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenEscribirConEstadoOk(string rol)
    {
        DateOnly hoy = Hoy();
        HttpClient cliente = ClienteConRoles(rol);
        GuardarContratoLaboralRequestDto alta = CrearSolicitudValida();
        alta.EmpleadoId = 2;
        HttpResponseMessage creado = await cliente.PostAsJsonAsync(RutaBase, alta);
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        ContratoLaboralCreadoDto? contrato = await creado.Content.ReadFromJsonAsync<ContratoLaboralCreadoDto>();
        Assert.NotNull(contrato);
        HttpResponseMessage adenda = await cliente.PostAsJsonAsync(
            $"{RutaBase}/{contrato.Id}/adendas",
            new AdendaContratoRequestDto { FechaVigencia = hoy, Cargo = "Jefe de Operaciones" });
        Assert.Equal(HttpStatusCode.OK, adenda.StatusCode);
        HttpResponseMessage renovacion = await cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/renovacion", CrearRenovacionValida(hoy.AddDays(800)));
        Assert.Equal(HttpStatusCode.OK, renovacion.StatusCode);
        ContratoLaboralCreadoDto? renovado = await renovacion.Content.ReadFromJsonAsync<ContratoLaboralCreadoDto>();
        Assert.NotNull(renovado);
        HttpResponseMessage anulacion = await cliente.PostAsJsonAsync(
            $"{RutaBase}/{renovado.Id}/anulacion",
            new AnularContratoLaboralRequestDto { Motivo = "Registro duplicado" });
        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
    }

    [Theory]
    [InlineData("CONSULTA")]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task LecturaConCualquierTokenAutenticadoDevuelveOk(string rol)
    {
        await InsertarConjuntoDeListadoAsync();
        HttpClient cliente = ClienteConRoles(rol);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase + "/1")).StatusCode);
    }

    // T12: auditoría.
    [Fact]
    public async Task AltaRenovacionAdendaYAnulacionGeneranRegistrosDeAuditoria()
    {
        DateOnly hoy = Hoy();
        ContratoLaboralCreadoDto contrato = await CrearContratoVigenteAsync(1, "PlazoFijo", hoy.AddDays(-30), hoy.AddDays(60));
        List<RegistroAuditoria> altaContrato = await LeerAuditoriaAsync("ContratoLaboral", contrato.Id);
        Assert.Single(altaContrato, registro => registro.Accion == "Alta");

        int adendaId = await AgregarAdendaValidaAsync(contrato.Id, hoy.AddDays(-10), "Analista Senior", null);
        List<RegistroAuditoria> altaAdenda = await LeerAuditoriaAsync("AdendaContrato", adendaId);
        Assert.Single(altaAdenda, registro => registro.Accion == "Alta");

        HttpResponseMessage renovacion = await _cliente.PostAsJsonAsync($"{RutaBase}/{contrato.Id}/renovacion", CrearRenovacionValida(hoy.AddDays(500)));
        Assert.Equal(HttpStatusCode.OK, renovacion.StatusCode);
        ContratoLaboralCreadoDto? renovado = await renovacion.Content.ReadFromJsonAsync<ContratoLaboralCreadoDto>();
        Assert.NotNull(renovado);
        List<RegistroAuditoria> altaRenovado = await LeerAuditoriaAsync("ContratoLaboral", renovado.Id);
        Assert.Single(altaRenovado, registro => registro.Accion == "Alta");
        List<RegistroAuditoria> cambiosDelAnterior = await LeerAuditoriaAsync("ContratoLaboral", contrato.Id);
        RegistroAuditoria renovacionRegistrada = Assert.Single(
            cambiosDelAnterior,
            registro => registro.Accion == "Modificacion" && CamposDeCambio(registro).Contains("Estado"));
        Assert.Equal("ContratoLaboral", renovacionRegistrada.Entidad);

        HttpResponseMessage anulacion = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{renovado.Id}/anulacion",
            new AnularContratoLaboralRequestDto { Motivo = "Registro duplicado" });
        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
        List<RegistroAuditoria> auditoriaRenovado = await LeerAuditoriaAsync("ContratoLaboral", renovado.Id);
        List<RegistroAuditoria> modificaciones = auditoriaRenovado.Where(registro => registro.Accion == "Modificacion").ToList();
        Assert.Contains(modificaciones, registro => CamposDeCambio(registro).Contains("MotivoCierre"));
        Assert.All(auditoriaRenovado, registro => Assert.Equal("ContratoLaboral", registro.Entidad));
    }

    private static int[] ConvertirIds(string ids)
    {
        return ids.Length == 0 ? Array.Empty<int>() : ids.Split(',').Select(texto => int.Parse(texto, CultureInfo.InvariantCulture)).ToArray();
    }

    private static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static GuardarContratoLaboralRequestDto CrearSolicitudValida()
    {
        DateOnly hoy = Hoy();
        return new GuardarContratoLaboralRequestDto
        {
            EmpleadoId = 1,
            Modalidad = "PlazoFijo",
            FechaInicio = hoy.AddDays(-30),
            FechaFin = hoy.AddDays(335),
            SueldoBasico = 4200.50m,
            Cargo = "Analista de Operaciones"
        };
    }

    private static RenovarContratoLaboralRequestDto CrearRenovacionValida(DateOnly? fechaFin)
    {
        return new RenovarContratoLaboralRequestDto
        {
            Modalidad = "PlazoFijo",
            FechaFin = fechaFin,
            SueldoBasico = 4500.00m,
            Cargo = "Analista Senior"
        };
    }

    private static void AplicarCasoEnAlta(string caso, GuardarContratoLaboralRequestDto solicitud)
    {
        switch (caso)
        {
            case "empleadoId-nulo":
                solicitud.EmpleadoId = null;
                break;
            case "modalidad-otra":
                solicitud.Modalidad = "Otra";
                break;
            case "modalidad-minusculas":
                solicitud.Modalidad = "plazofijo";
                break;
            case "modalidad-vacia":
                solicitud.Modalidad = " ";
                break;
            case "inicio-nulo":
                solicitud.FechaInicio = null;
                break;
            case "sueldo-nulo":
                solicitud.SueldoBasico = null;
                break;
            case "sueldo-cero":
                solicitud.SueldoBasico = 0m;
                break;
            case "sueldo-negativo":
                solicitud.SueldoBasico = -1m;
                break;
            case "sueldo-exceso":
                solicitud.SueldoBasico = 10000000000.00m;
                break;
            case "sueldo-decimales":
                solicitud.SueldoBasico = 4200.505m;
                break;
            case "cargo-vacio":
                solicitud.Cargo = "";
                break;
            case "cargo-espacios":
                solicitud.Cargo = "   ";
                break;
            case "cargo-largo":
                solicitud.Cargo = new string('C', 101);
                break;
            case "indeterminado-con-fin":
                solicitud.Modalidad = "Indeterminado";
                break;
            case "plazo-sin-fin":
                solicitud.Modalidad = "PlazoFijo";
                solicitud.FechaFin = null;
                break;
            case "formativo-sin-fin":
                solicitud.Modalidad = "Formativo";
                solicitud.FechaFin = null;
                break;
            case "fin-igual-inicio":
                solicitud.FechaFin = solicitud.FechaInicio;
                break;
            case "fin-anterior-inicio":
                solicitud.FechaFin = solicitud.FechaInicio?.AddDays(-1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(caso), caso, "Caso de prueba desconocido.");
        }
    }

    private static void AplicarCasoEnRenovacion(string caso, RenovarContratoLaboralRequestDto solicitud)
    {
        switch (caso)
        {
            case "modalidad-otra":
                solicitud.Modalidad = "Otra";
                break;
            case "sueldo-nulo":
                solicitud.SueldoBasico = null;
                break;
            case "sueldo-cero":
                solicitud.SueldoBasico = 0m;
                break;
            case "sueldo-exceso":
                solicitud.SueldoBasico = 10000000000.00m;
                break;
            case "sueldo-decimales":
                solicitud.SueldoBasico = 4200.505m;
                break;
            case "cargo-vacio":
                solicitud.Cargo = "";
                break;
            case "cargo-largo":
                solicitud.Cargo = new string('C', 101);
                break;
            case "indeterminado-con-fin":
                solicitud.Modalidad = "Indeterminado";
                break;
            case "plazo-sin-fin":
                solicitud.FechaFin = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(caso), caso, "Caso de prueba desconocido.");
        }
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

    private async Task<ContratoLaboralCreadoDto> CrearAsync(GuardarContratoLaboralRequestDto solicitud)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaBase, solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ContratoLaboralCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<ContratoLaboralCreadoDto>();
        Assert.NotNull(creado);
        return creado;
    }

    private async Task<ContratoLaboralCreadoDto> CrearContratoVigenteAsync(int empleadoId, string modalidad, DateOnly inicio, DateOnly? fin)
    {
        GuardarContratoLaboralRequestDto solicitud = CrearSolicitudValida();
        solicitud.EmpleadoId = empleadoId;
        solicitud.Modalidad = modalidad;
        solicitud.FechaInicio = inicio;
        solicitud.FechaFin = fin;
        return await CrearAsync(solicitud);
    }

    private async Task<int> AgregarAdendaValidaAsync(int contratoId, DateOnly vigencia, string? cargo, decimal? sueldo)
    {
        AdendaContratoRequestDto solicitud = new AdendaContratoRequestDto { FechaVigencia = vigencia, Cargo = cargo, SueldoBasico = sueldo };
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{RutaBase}/{contratoId}/adendas", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ContratoLaboralIdDto? resultado = await respuesta.Content.ReadFromJsonAsync<ContratoLaboralIdDto>();
        Assert.NotNull(resultado);
        return resultado.Id;
    }

    private async Task<PaginaContratosLaboralesDto> LeerPaginaAsync(string consulta)
    {
        string ruta = consulta.Length == 0 ? RutaBase : $"{RutaBase}?{consulta}";
        PaginaContratosLaboralesDto? pagina = await _cliente.GetFromJsonAsync<PaginaContratosLaboralesDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }

    // Conjunto fijo del listado: el contrato 5 es Vigente guardado con fin pasado (Vencido efectivo).
    private async Task InsertarConjuntoDeListadoAsync()
    {
        DateOnly hoy = Hoy();
        ContratoLaboral renovadoAnterior = CrearContrato(2, 2, "Renovado", "PlazoFijo", new DateOnly(2019, 8, 16), new DateOnly(2020, 8, 15));
        ContratoLaboral renovado = CrearContrato(3, 2, "Renovado", "PlazoFijo", new DateOnly(2020, 8, 16), new DateOnly(2021, 8, 15));
        renovado.ContratoAnteriorId = 2;
        ContratoLaboral cerrado = CrearContrato(6, 4, "Cerrado", "PlazoFijo", new DateOnly(2026, 1, 1), new DateOnly(2027, 5, 1));
        cerrado.FechaCierre = new DateOnly(2026, 9, 30);
        cerrado.MotivoCierre = "Cese del empleado";
        ContratoLaboral anulado = CrearContrato(7, 1, "Anulado", "PlazoFijo", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        anulado.FechaCierre = new DateOnly(2026, 1, 5);
        anulado.MotivoCierre = "Registro duplicado por error";
        ContratoLaboral vigenteConFinFuturo = CrearContrato(1, 1, "Vigente", "PlazoFijo", new DateOnly(2021, 3, 1), hoy.AddDays(30));
        vigenteConFinFuturo.SueldoBasico = 8500m;
        vigenteConFinFuturo.Cargo = "Gerente de Ventas";
        anulado.Cargo = "Gerente de Ventas";
        ContratoLaboral indeterminado = CrearContrato(4, 2, "Vigente", "Indeterminado", new DateOnly(2021, 8, 16), null);
        indeterminado.SueldoBasico = 4200m;
        indeterminado.Cargo = "Analista de Compras";
        ContratoLaboral vencidoCalculado = CrearContrato(5, 3, "Vigente", "Formativo", new DateOnly(2024, 1, 8), new DateOnly(2024, 12, 31));
        vencidoCalculado.Cargo = "Almacenero";
        ContratoLaboral vencidoGuardado = CrearContrato(8, 4, "Vencido", "PlazoFijo", new DateOnly(2018, 6, 1), new DateOnly(2019, 5, 31));
        await InsertarContratoAsync(
            vigenteConFinFuturo,
            renovadoAnterior,
            renovado,
            indeterminado,
            vencidoCalculado,
            cerrado,
            anulado,
            vencidoGuardado);
    }

    private static ContratoLaboral CrearContrato(int id, int empleadoId, string estado, string modalidad, DateOnly inicio, DateOnly? fin)
    {
        return new ContratoLaboral
        {
            Id = id,
            Numero = $"CTR-{id:D6}",
            EmpleadoId = empleadoId,
            Modalidad = modalidad,
            FechaInicio = inicio,
            FechaFin = fin,
            SueldoBasico = 3000m,
            Cargo = "Analista",
            Estado = estado
        };
    }

    private async Task InsertarContratoAsync(params ContratoLaboral[] contratos)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ContratosLaborales.AddRange(contratos);
        await contexto.SaveChangesAsync();
    }

    private async Task InsertarAdendaAsync(AdendaContrato adenda)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.AdendasContrato.Add(adenda);
        await contexto.SaveChangesAsync();
    }

    private async Task<ContratoLaboral> ObtenerContratoAsync(int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.ContratosLaborales.AsNoTracking().SingleAsync(contrato => contrato.Id == id);
    }

    private async Task<AdendaContrato> ObtenerAdendaAsync(int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.AdendasContrato.AsNoTracking().SingleAsync(adenda => adenda.Id == id);
    }

    private async Task<string[]> LeerFirmasAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        List<ContratoLaboral> contratos = await contexto.ContratosLaborales.AsNoTracking().OrderBy(contrato => contrato.Id).ToListAsync();
        List<AdendaContrato> adendas = await contexto.AdendasContrato.AsNoTracking().OrderBy(adenda => adenda.Id).ToListAsync();
        List<string> firmas = contratos.Select(FirmaContrato).ToList();
        firmas.AddRange(adendas.Select(FirmaAdenda));
        return firmas.ToArray();
    }

    private static string FirmaContrato(ContratoLaboral contrato)
    {
        return $"C|{contrato.Id}|{contrato.Numero}|{contrato.EmpleadoId}|{contrato.Modalidad}|{contrato.FechaInicio:yyyy-MM-dd}|{contrato.FechaFin:yyyy-MM-dd}|{contrato.SueldoBasico.ToString(CultureInfo.InvariantCulture)}|{contrato.Cargo}|{contrato.Estado}|{contrato.ContratoAnteriorId}|{contrato.FechaCierre:yyyy-MM-dd}|{contrato.MotivoCierre}";
    }

    private static string FirmaAdenda(AdendaContrato adenda)
    {
        return $"A|{adenda.Id}|{adenda.ContratoLaboralId}|{adenda.FechaVigencia:yyyy-MM-dd}|{adenda.Cargo}|{adenda.SueldoBasico.ToString(CultureInfo.InvariantCulture)}";
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
        contexto.AdendasContrato.RemoveRange(contexto.AdendasContrato.ToList());
        contexto.ContratosLaborales.RemoveRange(contexto.ContratosLaborales.ToList());
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        BorrarAuditoriaDelModulo(contexto);
        contexto.Empleados.AddRange(
            new Empleado
            {
                Id = 1,
                Legajo = "EMP-000001",
                TipoDocumento = "DNI",
                NumeroDocumento = "10000001",
                Nombres = "Ana",
                Apellidos = "Torres Vega",
                FechaNacimiento = new DateOnly(1990, 5, 14),
                FechaIngreso = new DateOnly(2021, 3, 1),
                Cargo = "Gerente de Ventas",
                Area = "Ventas",
                Email = "ana.torres@titanglobal.example",
                Telefono = "999000001",
                Estado = "Activo"
            },
            new Empleado
            {
                Id = 2,
                Legajo = "EMP-000002",
                TipoDocumento = "DNI",
                NumeroDocumento = "10000002",
                Nombres = "Luis",
                Apellidos = "Rojas Mendoza",
                FechaNacimiento = new DateOnly(1988, 11, 2),
                FechaIngreso = new DateOnly(2019, 8, 16),
                Cargo = "Analista de Compras",
                Area = "Compras",
                Email = "luis.rojas@titanglobal.example",
                Telefono = "999000002",
                Estado = "Activo"
            },
            new Empleado
            {
                Id = 3,
                Legajo = "EMP-000003",
                TipoDocumento = "CE",
                NumeroDocumento = "004512367",
                Nombres = "Maria",
                Apellidos = "Salazar Quispe",
                FechaNacimiento = new DateOnly(1995, 2, 20),
                FechaIngreso = new DateOnly(2024, 1, 8),
                Cargo = "Almacenero",
                Area = "Almacen",
                Email = "maria.salazar@titanglobal.example",
                Telefono = "999000003",
                Estado = "Activo"
            },
            new Empleado
            {
                Id = 4,
                Legajo = "EMP-000004",
                TipoDocumento = "Pasaporte",
                NumeroDocumento = "P1234567",
                Nombres = "Carlos",
                Apellidos = "Diaz Soto",
                FechaNacimiento = new DateOnly(1982, 7, 30),
                FechaIngreso = new DateOnly(2018, 5, 2),
                Cargo = "Auxiliar de Logistica",
                Area = "Logistica",
                Estado = "Cesado",
                FechaCese = new DateOnly(2026, 9, 30),
                MotivoCese = "Renuncia voluntaria"
            });
        contexto.SaveChanges();
        BorrarAuditoriaDelModulo(contexto);
    }

    private static void BorrarAuditoriaDelModulo(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria
                .Where(registro => registro.Entidad == "Empleado"
                    || registro.Entidad == "ContratoLaboral"
                    || registro.Entidad == "AdendaContrato")
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
