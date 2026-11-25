using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Api.Tests.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo66_Planillas;

[Collection("Modulo04")]
public class BonosPlanillaApiTests
{
    private const string RutaBase = "/api/v1/bonos-planilla";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public BonosPlanillaApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosPlanillas.Restablecer(fabrica);
    }

    // Mensajes 16, 1, 2, 3, 18, 19 y 17 del registro, en el orden de la spec.
    [Fact]
    public async Task RegistroConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        await AssertRegistroAsync(null, 2026, 10, "Mensual", "Bono", 100m, 400, "El empleado es obligatorio.");
        string anio = "El año es obligatorio y debe estar entre 2000 y 2100.";
        await AssertRegistroAsync(1, null, 10, "Mensual", "Bono", 100m, 400, anio);
        await AssertRegistroAsync(1, 1999, 10, "Mensual", "Bono", 100m, 400, anio);
        string mes = "El mes es obligatorio y debe estar entre 1 y 12.";
        await AssertRegistroAsync(1, 2026, null, "Mensual", "Bono", 100m, 400, mes);
        await AssertRegistroAsync(1, 2026, 13, "Mensual", "Bono", 100m, 400, mes);
        string tipo = "El tipo de planilla debe ser Mensual o Complementaria.";
        await AssertRegistroAsync(1, 2026, 10, null, "Bono", 100m, 400, tipo);
        await AssertRegistroAsync(1, 2026, 10, "Otro", "Bono", 100m, 400, tipo);
        string concepto = "El concepto es obligatorio y no puede superar 100 caracteres.";
        await AssertRegistroAsync(1, 2026, 10, "Mensual", null, 100m, 400, concepto);
        await AssertRegistroAsync(1, 2026, 10, "Mensual", "   ", 100m, 400, concepto);
        await AssertRegistroAsync(1, 2026, 10, "Mensual", new string('a', 101), 100m, 400, concepto);
        string monto = "El monto debe ser mayor que cero, con 2 decimales como máximo y no superar 9999999999.99.";
        await AssertRegistroAsync(1, 2026, 10, "Mensual", "Bono", null, 400, monto);
        await AssertRegistroAsync(1, 2026, 10, "Mensual", "Bono", 0m, 400, monto);
        await AssertRegistroAsync(1, 2026, 10, "Mensual", "Bono", -1m, 400, monto);
        await AssertRegistroAsync(1, 2026, 10, "Mensual", "Bono", 10.123m, 400, monto);
        await AssertRegistroAsync(1, 2026, 10, "Mensual", "Bono", 10000000000m, 400, monto);
        await AssertRegistroAsync(9999, 2026, 10, "Mensual", "Bono", 100m, 404, "El empleado 9999 no existe.");
        Assert.Empty(await DatosPlanillas.LeerBonosAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await Registrar(1, 2026, 10, "Mensual", new string('a', 100), 9999999999.99m)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Registrar(2, 2026, 10, "Complementaria", "Bono", 0.01m)).StatusCode);
    }

    [Fact]
    public async Task ElListadoFiltraPorPeriodoYEmpleadoYValidaLaPaginacion()
    {
        await Registrar(1, 2026, 10, "Mensual", "Bono por metas", 500m);
        await Registrar(1, 2026, 11, "Mensual", "Bono de noviembre", 300m);
        await Registrar(2, 2026, 10, "Complementaria", "Bono extraordinario", 1000m);

        PaginaBonosPlanillaDto? todos = await _cliente.GetFromJsonAsync<PaginaBonosPlanillaDto>(RutaBase);
        PaginaBonosPlanillaDto? octubre = await _cliente.GetFromJsonAsync<PaginaBonosPlanillaDto>($"{RutaBase}?anio=2026&mes=10");
        PaginaBonosPlanillaDto? empleado = await _cliente.GetFromJsonAsync<PaginaBonosPlanillaDto>($"{RutaBase}?empleadoId=2");

        Assert.NotNull(todos);
        Assert.Equal(3, todos.Total);
        Assert.Equal("Bono de noviembre", todos.Items[0].Concepto);
        Assert.NotNull(octubre);
        Assert.Equal(2, octubre.Total);
        Assert.NotNull(empleado);
        BonoPlanillaDto unico = Assert.Single(empleado.Items);
        Assert.Equal("Complementaria", unico.TipoPlanilla);
        Assert.Equal(1000.00m, unico.Monto);
        Assert.Equal("EMP-000002", unico.EmpleadoLegajo);
        Assert.Null(unico.PlanillaId);
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task EliminarValidaLaExistenciaYElEstadoDeLaPlanilla()
    {
        await DatosPrestamos.AssertErrorAsync(await _cliente.DeleteAsync($"{RutaBase}/999"), 404, "El bono 999 no existe.");
        HttpResponseMessage libre = await Registrar(1, 2026, 10, "Mensual", "Bono libre", 100m);
        BonoPlanillaIdDto? idLibre = await libre.Content.ReadFromJsonAsync<BonoPlanillaIdDto>();
        Assert.NotNull(idLibre);
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{RutaBase}/{idLibre.Id}")).StatusCode);
        Assert.Empty(await DatosPlanillas.LeerBonosAsync(_fabrica));

        await Registrar(1, 2026, 10, "Mensual", "Bono calculado", 100m);
        int planillaId = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        await DatosPlanillas.CalcularOkAsync(_cliente, planillaId);
        BonoPlanilla enCalculada = Assert.Single(await DatosPlanillas.LeerBonosAsync(_fabrica));
        Assert.Equal(planillaId, enCalculada.PlanillaId);

        // En una planilla calculada todavía se puede eliminar; aprobada ya no.
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{RutaBase}/{enCalculada.Id}")).StatusCode);
        await Registrar(1, 2026, 10, "Mensual", "Bono aprobado", 200m);
        await DatosPlanillas.CalcularOkAsync(_cliente, planillaId);
        await DatosPlanillas.AprobarAsync(_cliente, planillaId);
        BonoPlanilla enAprobada = Assert.Single(await DatosPlanillas.LeerBonosAsync(_fabrica));
        await DatosPrestamos.AssertErrorAsync(
            await _cliente.DeleteAsync($"{RutaBase}/{enAprobada.Id}"),
            409,
            "El bono está en una planilla aprobada y no se puede eliminar.");
        Assert.Single(await DatosPlanillas.LeerBonosAsync(_fabrica));
    }

    [Fact]
    public async Task SoloRrhhYAdminRegistranYEliminanBonos()
    {
        HttpClient consulta = DatosPlanillas.ClienteConRoles(_fabrica, "CONSULTA");
        await DatosPrestamos.AssertErrorAsync(
            await consulta.PostAsJsonAsync(RutaBase, new GuardarBonoPlanillaRequestDto { EmpleadoId = 1, Anio = 2026, Mes = 10, TipoPlanilla = "Mensual", Concepto = "Bono", Monto = 100m }),
            403,
            DatosPrestamos.MensajeSinPermiso);
        await DatosPrestamos.AssertErrorAsync(await consulta.DeleteAsync($"{RutaBase}/1"), 403, DatosPrestamos.MensajeSinPermiso);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DatosPlanillas.ClienteConRoles(_fabrica, "RRHH").PostAsJsonAsync(
            RutaBase,
            new GuardarBonoPlanillaRequestDto { EmpleadoId = 1, Anio = 2026, Mes = 10, TipoPlanilla = "Mensual", Concepto = "Bono", Monto = 100m })).StatusCode);
        Assert.Single(await DatosPlanillas.LeerBonosAsync(_fabrica));
    }

    [Fact]
    public async Task ElRegistroGeneraAuditoria()
    {
        HttpResponseMessage respuesta = await Registrar(1, 2026, 10, "Mensual", "Bono por metas", 500m);
        BonoPlanillaIdDto? id = await respuesta.Content.ReadFromJsonAsync<BonoPlanillaIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosPlanillas.LeerAuditoriaAsync(_fabrica, "BonoPlanilla", id.Id), registro => registro.Accion == "Alta");
    }

    private async Task<HttpResponseMessage> Registrar(int? empleadoId, int? anio, int? mes, string? tipo, string? concepto, decimal? monto)
    {
        GuardarBonoPlanillaRequestDto solicitud = new GuardarBonoPlanillaRequestDto
        {
            EmpleadoId = empleadoId,
            Anio = anio,
            Mes = mes,
            TipoPlanilla = tipo,
            Concepto = concepto,
            Monto = monto
        };
        return await _cliente.PostAsJsonAsync(RutaBase, solicitud);
    }

    private async Task AssertRegistroAsync(int? empleadoId, int? anio, int? mes, string? tipo, string? concepto, decimal? monto, int codigo, string mensaje)
    {
        await DatosPrestamos.AssertErrorAsync(await Registrar(empleadoId, anio, mes, tipo, concepto, monto), codigo, mensaje);
    }
}
