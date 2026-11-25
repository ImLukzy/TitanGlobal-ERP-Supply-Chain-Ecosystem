using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo70_SeguridadSaludOcupacional;

[Collection("Modulo04")]
public class EntregasEppApiTests
{
    private const string RutaBase = "/api/v1/entregas-epp";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public EntregasEppApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosSst.Restablecer(fabrica);
    }

    // S1: FechaVencimiento = FechaEntrega + VidaUtilDias, guardada al entregar.
    [Fact]
    public async Task EntregaGuardaElVencimientoSumandoLaVidaUtilYNoModificaLasAnteriores()
    {
        int equipo = await DatosSst.InsertarEquipoAsync(_fabrica, "CAS", 30);
        DateOnly primera = DatosSst.Hoy.AddDays(-20);
        HttpResponseMessage creada = await _cliente.PostAsJsonAsync(RutaBase, Solicitud(equipo, 1, primera, 2));
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        EntregaEppIdDto? id = await creada.Content.ReadFromJsonAsync<EntregaEppIdDto>();
        Assert.NotNull(id);
        EntregaEpp guardada = Assert.Single(await DatosSst.LeerEntregasAsync(_fabrica));
        Assert.Equal(primera.AddDays(30), guardada.FechaVencimiento);
        Assert.Equal(2, guardada.Cantidad);
        Assert.Equal(equipo, guardada.EquipoProteccionId);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(equipo, 1, DatosSst.Hoy, 1))).StatusCode);
        List<EntregaEpp> todas = await DatosSst.LeerEntregasAsync(_fabrica);
        Assert.Equal(2, todas.Count);
        Assert.Equal(primera.AddDays(30), todas[0].FechaVencimiento);
        Assert.Equal(DatosSst.Hoy.AddDays(30), todas[1].FechaVencimiento);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync(RutaBase, Solicitud(equipo, 2, DatosSst.Hoy, 100))).StatusCode);
    }

    // Mensajes 8, 11 y 12 en el orden de la spec.
    [Fact]
    public async Task EntregaConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        int equipo = await DatosSst.InsertarEquipoAsync(_fabrica, "CAS", 30);
        string fecha = "La fecha es obligatoria y no puede ser posterior a hoy.";
        string cantidad = "La cantidad debe estar entre 1 y 100.";
        await AssertRegistrarAsync(Solicitud(equipo, null, DatosSst.Hoy, 1), 400, "El empleado es obligatorio.");
        await AssertRegistrarAsync(Solicitud(equipo, 1, null, 1), 400, fecha);
        await AssertRegistrarAsync(Solicitud(equipo, 1, DatosSst.Hoy.AddDays(1), 1), 400, fecha);
        await AssertRegistrarAsync(Solicitud(equipo, 1, DatosSst.Hoy, null), 400, cantidad);
        await AssertRegistrarAsync(Solicitud(equipo, 1, DatosSst.Hoy, 0), 400, cantidad);
        await AssertRegistrarAsync(Solicitud(equipo, 1, DatosSst.Hoy, 101), 400, cantidad);
        Assert.Empty(await DatosSst.LeerEntregasAsync(_fabrica));
    }

    // Mensajes 5, 7, 9 y 10.
    [Fact]
    public async Task EntregaRechazaEquiposYEmpleadosNoValidos()
    {
        int activo = await DatosSst.InsertarEquipoAsync(_fabrica, "ACT", 30);
        int inactivo = await DatosSst.InsertarEquipoAsync(_fabrica, "INA", 30, false);
        await AssertRegistrarAsync(Solicitud(9999, 1, DatosSst.Hoy, 1), 404, "El equipo de protección 9999 no existe.");
        await AssertRegistrarAsync(Solicitud(null, 1, DatosSst.Hoy, 1), 404, "El equipo de protección 0 no existe.");
        await AssertRegistrarAsync(Solicitud(inactivo, 1, DatosSst.Hoy, 1), 409, "El equipo de protección INA está inactivo y no se entrega.");
        await AssertRegistrarAsync(Solicitud(activo, 9999, DatosSst.Hoy, 1), 404, "El empleado 9999 no existe.");
        await AssertRegistrarAsync(Solicitud(activo, 4, DatosSst.Hoy, 1), 409, "El empleado 4 está cesado.");
        Assert.Empty(await DatosSst.LeerEntregasAsync(_fabrica));
    }

    // El vencimiento se calcula al leer contra hoy: la fecha de vencimiento de hoy sigue vigente.
    [Fact]
    public async Task ListadoCalculaLaVencidaAlLeerYFiltraPorEmpleadoEquipoYVencidas()
    {
        int casco = await DatosSst.InsertarEquipoAsync(_fabrica, "CAS", 30);
        int guantes = await DatosSst.InsertarEquipoAsync(_fabrica, "GUA", 30);
        int vencida = await DatosSst.InsertarEntregaAsync(_fabrica, casco, 1, DatosSst.Hoy.AddDays(-40), DatosSst.Hoy.AddDays(-1));
        int venceHoy = await DatosSst.InsertarEntregaAsync(_fabrica, casco, 2, DatosSst.Hoy.AddDays(-30), DatosSst.Hoy);
        int vigente = await DatosSst.InsertarEntregaAsync(_fabrica, guantes, 1, DatosSst.Hoy.AddDays(-1), DatosSst.Hoy.AddDays(29));
        PaginaEntregasEppDto todas = await LeerPaginaAsync(RutaBase);
        Assert.Equal(3, todas.Total);
        Assert.Equal(new[] { vigente, venceHoy, vencida }, todas.Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { false, false, true }, todas.Items.Select(item => item.Vencida).ToArray());
        Assert.Equal("CAS", todas.Items[2].EquipoCodigo);
        Assert.Equal("Torres Vega, Ana", todas.Items[2].Empleado);
        Assert.Equal(new[] { vencida }, (await LeerPaginaAsync($"{RutaBase}?vencidas=true")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { vigente, venceHoy }, (await LeerPaginaAsync($"{RutaBase}?vencidas=false")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { vigente, vencida }, (await LeerPaginaAsync($"{RutaBase}?empleadoId=1")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { venceHoy, vencida }, (await LeerPaginaAsync($"{RutaBase}?equipoId={casco}")).Items.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { vencida }, (await LeerPaginaAsync($"{RutaBase}?empleadoId=1&equipoId={casco}")).Items.Select(item => item.Id).ToArray());
        PaginaEntregasEppDto segunda = await LeerPaginaAsync($"{RutaBase}?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Equal(new[] { vencida }, segunda.Items.Select(item => item.Id).ToArray());
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        int equipo = await DatosSst.InsertarEquipoAsync(_fabrica, "CAS", 30);
        RegistrarEntregaEppRequestDto solicitud = Solicitud(equipo, 1, DatosSst.Hoy, 1);
        HttpClient anonimo = DatosSst.ClienteAnonimo(_fabrica);
        await DatosSst.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, DatosSst.MensajeSinSesion);
        await DatosSst.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, solicitud), 401, DatosSst.MensajeSinSesion);
        HttpClient consulta = DatosSst.ClienteConRoles(_fabrica, "CONSULTA");
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        await DatosSst.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, solicitud), 403, DatosSst.MensajeSinPermiso);
        Assert.Empty(await DatosSst.LeerEntregasAsync(_fabrica));
        foreach (string rol in new[] { "RRHH", "ADMIN" })
        {
            HttpClient cliente = DatosSst.ClienteConRoles(_fabrica, rol);
            Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(RutaBase, solicitud)).StatusCode);
        }

        Assert.Equal(2, (await DatosSst.LeerEntregasAsync(_fabrica)).Count);
    }

    [Fact]
    public async Task EntregaGeneraRegistroDeAuditoria()
    {
        int equipo = await DatosSst.InsertarEquipoAsync(_fabrica, "CAS", 30);
        HttpResponseMessage creada = await _cliente.PostAsJsonAsync(RutaBase, Solicitud(equipo, 1, DatosSst.Hoy, 1));
        EntregaEppIdDto? id = await creada.Content.ReadFromJsonAsync<EntregaEppIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosSst.LeerAuditoriaAsync(_fabrica, "EntregaEpp", id.Id), registro => registro.Accion == "Alta");
    }

    private static RegistrarEntregaEppRequestDto Solicitud(int? equipoId, int? empleadoId, DateOnly? fechaEntrega, int? cantidad)
    {
        return new RegistrarEntregaEppRequestDto
        {
            EquipoId = equipoId,
            EmpleadoId = empleadoId,
            FechaEntrega = fechaEntrega,
            Cantidad = cantidad
        };
    }

    private async Task AssertRegistrarAsync(RegistrarEntregaEppRequestDto solicitud, int codigo, string mensaje)
    {
        await DatosSst.AssertErrorAsync(await _cliente.PostAsJsonAsync(RutaBase, solicitud), codigo, mensaje);
    }

    private async Task<PaginaEntregasEppDto> LeerPaginaAsync(string ruta)
    {
        PaginaEntregasEppDto? pagina = await _cliente.GetFromJsonAsync<PaginaEntregasEppDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }
}
