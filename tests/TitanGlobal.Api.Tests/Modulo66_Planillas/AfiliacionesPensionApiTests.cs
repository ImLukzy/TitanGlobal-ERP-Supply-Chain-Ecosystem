using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Api.Tests.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;

namespace TitanGlobal.Api.Tests.Modulo66_Planillas;

[Collection("Modulo04")]
public class AfiliacionesPensionApiTests
{
    private const string RutaBase = "/api/v1/afiliaciones-pension";

    private static readonly DateOnly Afiliacion = new DateOnly(2024, 2, 1);

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public AfiliacionesPensionApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosPlanillas.Restablecer(fabrica);
    }

    // Mensajes 22, 23, 24, 25 y 17, en el orden de la spec.
    [Fact]
    public async Task GuardarConDatosInvalidosDevuelveMensajesLiteralesSinCambios()
    {
        string sistema = "El sistema de pensiones es obligatorio.";
        await AssertGuardarAsync(3, null, null, Afiliacion, 400, sistema);
        await AssertGuardarAsync(3, "  ", null, Afiliacion, 400, sistema);
        await AssertGuardarAsync(3, "XYZ", "CUSPP0000003", Afiliacion, 404, "El sistema de pensiones XYZ no existe.");
        string cuspp = "El CUSPP es obligatorio en una AFP y tiene 12 caracteres.";
        await AssertGuardarAsync(3, "PRIMA", null, Afiliacion, 400, cuspp);
        await AssertGuardarAsync(3, "PRIMA", "   ", Afiliacion, 400, cuspp);
        await AssertGuardarAsync(3, "PRIMA", "CUSPP000003", Afiliacion, 400, cuspp);
        await AssertGuardarAsync(3, "PRIMA", "CUSPP00000033", Afiliacion, 400, cuspp);
        await AssertGuardarAsync(3, "PRIMA", "CUSPP0000003", null, 400, "La fecha de afiliación es obligatoria.");
        await AssertGuardarAsync(9999, "ONP", null, Afiliacion, 404, "El empleado 9999 no existe.");
        PaginaAfiliacionesPensionDto? pagina = await _cliente.GetFromJsonAsync<PaginaAfiliacionesPensionDto>(RutaBase);
        Assert.NotNull(pagina);
        Assert.Equal(new[] { 1, 2 }, pagina.Items.Select(item => item.EmpleadoId).ToArray());
    }

    [Fact]
    public async Task ElPutCreaYLuegoReemplazaLaAfiliacionDelEmpleado()
    {
        HttpResponseMessage creada = await Guardar(3, "PRIMA", "CUSPP0000003", Afiliacion);
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        AfiliacionPensionIdDto? primera = await creada.Content.ReadFromJsonAsync<AfiliacionPensionIdDto>();
        Assert.NotNull(primera);
        AfiliacionPensionDto fila = Assert.Single((await _cliente.GetFromJsonAsync<PaginaAfiliacionesPensionDto>($"{RutaBase}?empleadoId=3"))!.Items);
        Assert.Equal("PRIMA", fila.CodigoPension);
        Assert.Equal("AFP", fila.Sistema);
        Assert.Equal("CUSPP0000003", fila.Cuspp);
        Assert.Equal(Afiliacion, fila.FechaAfiliacion);
        Assert.Equal("EMP-000003", fila.EmpleadoLegajo);

        // El cambio a ONP conserva el mismo registro y borra el CUSPP.
        HttpResponseMessage reemplazada = await Guardar(3, "ONP", "CUSPP0000003", new DateOnly(2025, 1, 1));
        AfiliacionPensionIdDto? segunda = await reemplazada.Content.ReadFromJsonAsync<AfiliacionPensionIdDto>();
        Assert.NotNull(segunda);
        Assert.Equal(primera.Id, segunda.Id);
        AfiliacionPensionDto actual = Assert.Single((await _cliente.GetFromJsonAsync<PaginaAfiliacionesPensionDto>($"{RutaBase}?empleadoId=3"))!.Items);
        Assert.Equal("ONP", actual.CodigoPension);
        Assert.Null(actual.Cuspp);
        Assert.Equal(new DateOnly(2025, 1, 1), actual.FechaAfiliacion);
        Assert.Equal(3, (await _cliente.GetFromJsonAsync<PaginaAfiliacionesPensionDto>(RutaBase))!.Total);
        Assert.Single(await DatosPlanillas.LeerAuditoriaAsync(_fabrica, "AfiliacionPension", primera.Id), registro => registro.Accion == "Alta");
        Assert.Contains(await DatosPlanillas.LeerAuditoriaAsync(_fabrica, "AfiliacionPension", primera.Id), registro => registro.Accion == "Modificacion");
    }

    [Fact]
    public async Task ElListadoValidaLaPaginacionYElEmpleadoDelFiltro()
    {
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?empleadoId=9999"), 404, "El empleado 9999 no existe.");
    }

    [Fact]
    public async Task SoloRrhhYAdminGuardanAfiliaciones()
    {
        HttpClient consulta = DatosPlanillas.ClienteConRoles(_fabrica, "CONSULTA");
        HttpResponseMessage denegada = await consulta.PutAsJsonAsync(
            $"{RutaBase}/3",
            new GuardarAfiliacionPensionRequestDto { CodigoPension = "ONP", FechaAfiliacion = Afiliacion });
        await DatosPrestamos.AssertErrorAsync(denegada, 403, DatosPrestamos.MensajeSinPermiso);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        HttpClient rrhh = DatosPlanillas.ClienteConRoles(_fabrica, "RRHH");
        HttpResponseMessage permitida = await rrhh.PutAsJsonAsync(
            $"{RutaBase}/3",
            new GuardarAfiliacionPensionRequestDto { CodigoPension = "ONP", FechaAfiliacion = Afiliacion });
        Assert.Equal(HttpStatusCode.OK, permitida.StatusCode);
    }

    private async Task<HttpResponseMessage> Guardar(int empleadoId, string? codigo, string? cuspp, DateOnly? fecha)
    {
        return await _cliente.PutAsJsonAsync(
            $"{RutaBase}/{empleadoId}",
            new GuardarAfiliacionPensionRequestDto { CodigoPension = codigo, Cuspp = cuspp, FechaAfiliacion = fecha });
    }

    private async Task AssertGuardarAsync(int empleadoId, string? codigo, string? cuspp, DateOnly? fecha, int codigoEstado, string mensaje)
    {
        await DatosPrestamos.AssertErrorAsync(await Guardar(empleadoId, codigo, cuspp, fecha), codigoEstado, mensaje);
    }
}
