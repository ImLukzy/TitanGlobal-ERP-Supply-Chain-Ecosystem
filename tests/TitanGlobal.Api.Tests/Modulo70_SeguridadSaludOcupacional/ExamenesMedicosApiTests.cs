using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo70_SeguridadSaludOcupacional;

[Collection("Modulo04")]
public class ExamenesMedicosApiTests
{
    private const string RutaBase = "/api/v1/examenes-medicos";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public ExamenesMedicosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosSst.Restablecer(fabrica);
    }

    // Mensajes 8, 11 y 13 a 17 y 19 en el orden de la spec.
    [Fact]
    public async Task ExamenConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        DateOnly hoy = DatosSst.Hoy;
        string fecha = "La fecha es obligatoria y no puede ser posterior a hoy.";
        string tipo = "El tipo de examen debe ser Ingreso, Periodico o Retiro.";
        string resultado = "El resultado debe ser Apto, AptoConRestricciones o NoApto.";
        string restricciones = "Las restricciones son obligatorias si el resultado es AptoConRestricciones y no pueden superar 300 caracteres.";
        string vencimiento = "La fecha de vencimiento es obligatoria y posterior al examen en los exámenes de ingreso y periódicos.";
        await AssertRegistrarAsync(Solicitud(null, "Periodico", hoy, "Apto", null, hoy.AddDays(10), null), 400, "El empleado es obligatorio.");
        await AssertRegistrarAsync(Solicitud(1, "Periodico", null, "Apto", null, hoy.AddDays(10), null), 400, fecha);
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy.AddDays(1), "Apto", null, hoy.AddDays(10), null), 400, fecha);
        await AssertRegistrarAsync(Solicitud(1, null, hoy, "Apto", null, hoy.AddDays(10), null), 400, tipo);
        await AssertRegistrarAsync(Solicitud(1, "periodico", hoy, "Apto", null, hoy.AddDays(10), null), 400, tipo);
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy, null, null, hoy.AddDays(10), null), 400, resultado);
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy, "Bien", null, hoy.AddDays(10), null), 400, resultado);
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy, "AptoConRestricciones", null, hoy.AddDays(10), null), 400, restricciones);
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy, "AptoConRestricciones", "   ", hoy.AddDays(10), null), 400, restricciones);
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy, "Apto", new string('R', 301), hoy.AddDays(10), null), 400, restricciones);
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy, "Apto", null, null, null), 400, vencimiento);
        await AssertRegistrarAsync(Solicitud(1, "Ingreso", hoy, "Apto", null, hoy, null), 400, vencimiento);
        await AssertRegistrarAsync(Solicitud(1, "Ingreso", hoy, "Apto", null, hoy.AddDays(-1), null), 400, vencimiento);
        await AssertRegistrarAsync(Solicitud(1, "Retiro", hoy, "Apto", null, hoy.AddDays(10), null), 400, "El examen de retiro no tiene fecha de vencimiento.");
        await AssertRegistrarAsync(Solicitud(1, "Periodico", hoy, "Apto", null, hoy.AddDays(10), new string('C', 151)), 400, "La clínica no puede superar 150 caracteres.");
        Assert.Empty(await DatosSst.LeerExamenesAsync(_fabrica));
    }

    [Fact]
    public async Task ExamenValidoSeGuardaNormalizadoYAdmiteLosLimites()
    {
        DateOnly hoy = DatosSst.Hoy;
        HttpResponseMessage creado = await _cliente.PostAsJsonAsync(
            RutaBase,
            Solicitud(1, " Periodico ", hoy, "AptoConRestricciones", " Sin trabajo en altura ", hoy.AddDays(365), " Clinica Central "));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        ExamenMedicoIdDto? id = await creado.Content.ReadFromJsonAsync<ExamenMedicoIdDto>();
        Assert.NotNull(id);
        ExamenMedico guardado = Assert.Single(await DatosSst.LeerExamenesAsync(_fabrica));
        Assert.Equal("Periodico", guardado.Tipo);
        Assert.Equal("Sin trabajo en altura", guardado.Restricciones);
        Assert.Equal("Clinica Central", guardado.Clinica);
        Assert.Equal(hoy.AddDays(365), guardado.FechaVencimiento);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(2, "Ingreso", hoy, "Apto", new string('R', 300), hoy.AddDays(1), new string('C', 150)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(3, "Ingreso", hoy, "NoApto", null, hoy.AddDays(30), null))).StatusCode);
        Assert.Equal(3, (await DatosSst.LeerExamenesAsync(_fabrica)).Count);
    }

    // Mensajes 9 y 10: solo empleados activos, salvo el examen de retiro de un cesado.
    [Fact]
    public async Task ExamenesDeIngresoYPeriodicosSoloParaEmpleadosActivos()
    {
        DateOnly hoy = DatosSst.Hoy;
        await AssertRegistrarAsync(Solicitud(9999, "Periodico", hoy, "Apto", null, hoy.AddDays(10), null), 404, "El empleado 9999 no existe.");
        await AssertRegistrarAsync(Solicitud(4, "Periodico", hoy, "Apto", null, hoy.AddDays(10), null), 409, "El empleado 4 está cesado.");
        await AssertRegistrarAsync(Solicitud(5, "Ingreso", hoy, "Apto", null, hoy.AddDays(10), null), 409, "El empleado 5 está cesado.");
        await AssertRegistrarAsync(Solicitud(9999, "Retiro", hoy, "Apto", null, null, null), 404, "El empleado 9999 no existe.");
        Assert.Empty(await DatosSst.LeerExamenesAsync(_fabrica));
    }

    // Mensaje 18: el empleado 4 cesó hace 40 días (límite: hace 10 días) y el 5 hace 10 días.
    [Fact]
    public async Task ExamenDeRetiroSeAdmiteHastaTreintaDiasDespuesDelCese()
    {
        DateOnly hoy = DatosSst.Hoy;
        string fuera = "El examen de retiro se registra hasta 30 días después del cese del empleado 4.";
        await AssertRegistrarAsync(Solicitud(4, "Retiro", hoy.AddDays(-9), "Apto", null, null, null), 409, fuera);
        await AssertRegistrarAsync(Solicitud(4, "Retiro", hoy, "Apto", null, null, null), 409, fuera);
        Assert.Empty(await DatosSst.LeerExamenesAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(4, "Retiro", hoy.AddDays(-10), "Apto", null, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(4, "Retiro", hoy.AddDays(-40), "Apto", null, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(5, "Retiro", hoy, "NoApto", null, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(1, "Retiro", hoy, "Apto", null, null, null))).StatusCode);
        List<ExamenMedico> guardados = await DatosSst.LeerExamenesAsync(_fabrica);
        Assert.Equal(4, guardados.Count);
        Assert.All(guardados, examen => Assert.Null(examen.FechaVencimiento));
    }

    // El vencimiento se calcula al leer: un examen que vence hoy sigue vigente.
    [Fact]
    public async Task ListadoCalculaElVencidoAlLeerYAplicaLosFiltros()
    {
        DateOnly hoy = DatosSst.Hoy;
        int vencido = await DatosSst.InsertarExamenAsync(_fabrica, 1, "Periodico", hoy.AddDays(-400), "Apto", hoy.AddDays(-35));
        int venceHoy = await DatosSst.InsertarExamenAsync(_fabrica, 2, "Periodico", hoy.AddDays(-30), "AptoConRestricciones", hoy);
        int vigente = await DatosSst.InsertarExamenAsync(_fabrica, 1, "Ingreso", hoy.AddDays(-5), "NoApto", hoy.AddDays(360));
        int retiro = await DatosSst.InsertarExamenAsync(_fabrica, 4, "Retiro", hoy.AddDays(-39), "Apto", null);
        PaginaExamenesMedicosDto todos = await LeerPaginaAsync(RutaBase);
        Assert.Equal(4, todos.Total);
        Assert.Equal(new[] { vigente, venceHoy, retiro, vencido }, todos.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { false, false, false, true }, todos.Items.Select(item => item.Vencido).ToArray());
        Assert.Equal("Torres Vega, Ana", todos.Items[0].Empleado);
        Assert.Equal(new[] { vencido }, (await LeerPaginaAsync($"{RutaBase}?vencidos=true")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { vigente, venceHoy, retiro }, (await LeerPaginaAsync($"{RutaBase}?vencidos=false")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { vigente, vencido }, (await LeerPaginaAsync($"{RutaBase}?empleadoId=1")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { retiro }, (await LeerPaginaAsync($"{RutaBase}?tipo=Retiro")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { venceHoy }, (await LeerPaginaAsync($"{RutaBase}?resultado=AptoConRestricciones")).Items.Select(item => item.Id).ToArray());
        Assert.Equal("Sin trabajo en altura", (await LeerPaginaAsync($"{RutaBase}?resultado=AptoConRestricciones")).Items[0].Restricciones);
        PaginaExamenesMedicosDto segunda = await LeerPaginaAsync($"{RutaBase}?pagina=2&tamanoPagina=3");
        Assert.Equal(4, segunda.Total);
        Assert.Equal(new[] { vencido }, segunda.Items.Select(item => item.Id).ToArray());
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tipo=Otro"), 400, "El filtro tipo no es válido.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?resultado=Otro"), 400, "El filtro resultado no es válido.");
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        DateOnly hoy = DatosSst.Hoy;
        RegistrarExamenMedicoRequestDto solicitud = Solicitud(1, "Periodico", hoy, "Apto", null, hoy.AddDays(30), null);
        HttpClient anonimo = DatosSst.ClienteAnonimo(_fabrica);
        await DatosSst.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, DatosSst.MensajeSinSesion);
        await DatosSst.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, solicitud), 401, DatosSst.MensajeSinSesion);
        HttpClient consulta = DatosSst.ClienteConRoles(_fabrica, "CONSULTA");
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        await DatosSst.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, solicitud), 403, DatosSst.MensajeSinPermiso);
        Assert.Empty(await DatosSst.LeerExamenesAsync(_fabrica));
        foreach (string rol in new[] { "RRHH", "ADMIN" })
        {
            HttpClient cliente = DatosSst.ClienteConRoles(_fabrica, rol);
            Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(RutaBase, solicitud)).StatusCode);
        }

        Assert.Equal(2, (await DatosSst.LeerExamenesAsync(_fabrica)).Count);
    }

    [Fact]
    public async Task ExamenGeneraRegistroDeAuditoria()
    {
        DateOnly hoy = DatosSst.Hoy;
        HttpResponseMessage creado = await _cliente.PostAsJsonAsync(RutaBase, Solicitud(1, "Periodico", hoy, "Apto", null, hoy.AddDays(30), null));
        ExamenMedicoIdDto? id = await creado.Content.ReadFromJsonAsync<ExamenMedicoIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosSst.LeerAuditoriaAsync(_fabrica, "ExamenMedico", id.Id), registro => registro.Accion == "Alta");
    }

    private static RegistrarExamenMedicoRequestDto Solicitud(
        int? empleadoId,
        string? tipo,
        DateOnly? fecha,
        string? resultado,
        string? restricciones,
        DateOnly? vencimiento,
        string? clinica)
    {
        return new RegistrarExamenMedicoRequestDto
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Fecha = fecha,
            Resultado = resultado,
            Restricciones = restricciones,
            FechaVencimiento = vencimiento,
            Clinica = clinica
        };
    }

    private async Task AssertRegistrarAsync(RegistrarExamenMedicoRequestDto solicitud, int codigo, string mensaje)
    {
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), codigo, mensaje);
    }

    private async Task<PaginaExamenesMedicosDto> LeerPaginaAsync(string ruta)
    {
        PaginaExamenesMedicosDto? pagina = await _cliente.GetFromJsonAsync<PaginaExamenesMedicosDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }
}
