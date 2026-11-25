using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo70_SeguridadSaludOcupacional;

[Collection("Modulo04")]
public class EquiposProteccionApiTests
{
    private const string RutaBase = "/api/v1/equipos-proteccion";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public EquiposProteccionApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosSst.Restablecer(fabrica);
    }

    // Mensajes 1 a 3 en el orden de la spec.
    [Fact]
    public async Task CreacionConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        string codigo = "El código es obligatorio y no puede superar 20 caracteres.";
        string nombre = "El nombre es obligatorio y no puede superar 100 caracteres.";
        string vida = "La vida útil debe estar entre 1 y 3650 días.";
        await AssertCrearAsync(Solicitud(null, "Casco", 365), 400, codigo);
        await AssertCrearAsync(Solicitud("   ", "Casco", 365), 400, codigo);
        await AssertCrearAsync(Solicitud(new string('A', 21), "Casco", 365), 400, codigo);
        await AssertCrearAsync(Solicitud("CAS", null, 365), 400, nombre);
        await AssertCrearAsync(Solicitud("CAS", new string('N', 101), 365), 400, nombre);
        await AssertCrearAsync(Solicitud("CAS", "Casco", null), 400, vida);
        await AssertCrearAsync(Solicitud("CAS", "Casco", 0), 400, vida);
        await AssertCrearAsync(Solicitud("CAS", "Casco", 3651), 400, vida);
        Assert.Empty(await DatosSst.LeerEquiposAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud(new string('A', 20), new string('N', 100), 3650))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud("B", "Casco", 1))).StatusCode);
        Assert.Equal(2, (await DatosSst.LeerEquiposAsync(_fabrica)).Count);
    }

    // Mensaje 4: el código se guarda en mayúsculas y es único, también al actualizar.
    [Fact]
    public async Task CrearYActualizarNormalizanElCodigoYRechazanDuplicadosConMensaje4()
    {
        HttpResponseMessage creado = await CrearAsync(Solicitud(" cas-1 ", " Casco de seguridad ", 730));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        EquipoProteccionIdDto? id = await creado.Content.ReadFromJsonAsync<EquipoProteccionIdDto>();
        Assert.NotNull(id);
        EquipoProteccion guardado = Assert.Single(await DatosSst.LeerEquiposAsync(_fabrica));
        Assert.Equal("CAS-1", guardado.Codigo);
        Assert.Equal("Casco de seguridad", guardado.Nombre);
        Assert.Equal(730, guardado.VidaUtilDias);
        Assert.True(guardado.Activo);
        string duplicado = "Ya existe un equipo de protección con el código CAS-1.";
        await AssertCrearAsync(Solicitud("cas-1", "Otro", 10), 409, duplicado);
        Assert.Single(await DatosSst.LeerEquiposAsync(_fabrica));
        HttpResponseMessage segundo = await CrearAsync(Solicitud("GUA-1", "Guantes", 90));
        EquipoProteccionIdDto? idSegundo = await segundo.Content.ReadFromJsonAsync<EquipoProteccionIdDto>();
        Assert.NotNull(idSegundo);
        await DatosSst.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud("cas-1", "Guantes", 90)), 409, duplicado);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud("gua-1", "Guantes dieléctricos", 120))).StatusCode);
        EquipoProteccion editado = (await DatosSst.LeerEquiposAsync(_fabrica)).Single(equipo => equipo.Id == idSegundo.Id);
        Assert.Equal("Guantes dieléctricos", editado.Nombre);
        Assert.Equal(120, editado.VidaUtilDias);
        await DatosSst.AssertErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud(null, "Guantes", 90)),
            400,
            "El código es obligatorio y no puede superar 20 caracteres.");
    }

    // Mensaje 5 en todas las rutas con id.
    [Fact]
    public async Task EquipoInexistenteDevuelve404ConMensaje5EnTodasLasRutas()
    {
        string mensaje = "El equipo de protección 9999 no existe.";
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999"), 404, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{RutaBase}/9999", Solicitud("CAS", "Casco", 10)), 404, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/desactivacion", null), 404, mensaje);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/activacion", null), 404, mensaje);
    }

    // Mensaje 6 con «activo» e «inactivo».
    [Fact]
    public async Task ActivarYDesactivarCambianElEstadoYRechazanRepetirConMensaje6()
    {
        int id = await DatosSst.InsertarEquipoAsync(_fabrica, "EST", 30);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/activacion", null), 409, "El equipo de protección EST ya está activo.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/desactivacion", null)).StatusCode);
        Assert.False((await DatosSst.LeerEquiposAsync(_fabrica)).Single().Activo);
        await DatosSst.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/desactivacion", null), 409, "El equipo de protección EST ya está inactivo.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/activacion", null)).StatusCode);
        Assert.True((await DatosSst.LeerEquiposAsync(_fabrica)).Single().Activo);
    }

    [Fact]
    public async Task ListadoValidaPaginacionYAplicaBusquedaYActivo()
    {
        await DatosSst.InsertarEquipoAsync(_fabrica, "ALF", 30);
        await DatosSst.InsertarEquipoAsync(_fabrica, "BET", 60, false);
        await DatosSst.InsertarEquipoAsync(_fabrica, "GAM", 90);
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        PaginaEquiposProteccionDto todos = await LeerPaginaAsync(RutaBase);
        Assert.Equal(3, todos.Total);
        Assert.Equal(new[] { "ALF", "BET", "GAM" }, todos.Items.Select(item => item.Codigo).ToArray());
        PaginaEquiposProteccionDto segunda = await LeerPaginaAsync($"{RutaBase}?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Equal(new[] { "GAM" }, segunda.Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "BET" }, (await LeerPaginaAsync($"{RutaBase}?busqueda=bet")).Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "BET" }, (await LeerPaginaAsync($"{RutaBase}?activo=false")).Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "ALF", "GAM" }, (await LeerPaginaAsync($"{RutaBase}?activo=true")).Items.Select(item => item.Codigo).ToArray());
        EquipoProteccionListadoDto? detalle = await _cliente.GetFromJsonAsync<EquipoProteccionListadoDto>($"{RutaBase}/{todos.Items[1].Id}");
        Assert.NotNull(detalle);
        Assert.Equal("BET", detalle.Codigo);
        Assert.Equal(60, detalle.VidaUtilDias);
        Assert.False(detalle.Activo);
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        int id = await DatosSst.InsertarEquipoAsync(_fabrica, "ROL", 30);
        GuardarEquipoProteccionRequestDto solicitud = Solicitud("X", "Casco", 30);
        HttpClient anonimo = DatosSst.ClienteAnonimo(_fabrica);
        string sesion = DatosSst.MensajeSinSesion;
        await DatosSst.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, sesion);
        await DatosSst.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/{id}"), 401, sesion);
        await DatosSst.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, solicitud), 401, sesion);
        await DatosSst.AssertErrorAsync(await anonimo.PostAsync($"{RutaBase}/{id}/desactivacion", null), 401, sesion);
        HttpClient consulta = DatosSst.ClienteConRoles(_fabrica, "CONSULTA");
        string[] antes = await DatosSst.LeerFirmasAsync(_fabrica);
        string permiso = DatosSst.MensajeSinPermiso;
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/{id}")).StatusCode);
        await DatosSst.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, solicitud), 403, permiso);
        await DatosSst.AssertErrorAsync(await consulta.PutAsJsonAsync($"{RutaBase}/{id}", solicitud), 403, permiso);
        await DatosSst.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/desactivacion", null), 403, permiso);
        await DatosSst.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/activacion", null), 403, permiso);
        Assert.Equal(antes, await DatosSst.LeerFirmasAsync(_fabrica));
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenAdministrarEquipos(string rol)
    {
        HttpClient cliente = DatosSst.ClienteConRoles(_fabrica, rol);
        HttpResponseMessage creado = await cliente.PostAsJsonAsync(RutaBase, Solicitud("R-" + rol, "Casco", 30));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        EquipoProteccionIdDto? id = await creado.Content.ReadFromJsonAsync<EquipoProteccionIdDto>();
        Assert.NotNull(id);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/desactivacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/activacion", null)).StatusCode);
    }

    [Fact]
    public async Task EquipoGeneraRegistrosDeAuditoria()
    {
        HttpResponseMessage creado = await CrearAsync(Solicitud("AUD", "Casco", 30));
        EquipoProteccionIdDto? id = await creado.Content.ReadFromJsonAsync<EquipoProteccionIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosSst.LeerAuditoriaAsync(_fabrica, "EquipoProteccion", id.Id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id.Id}/desactivacion", null)).StatusCode);
        RegistroAuditoria cambio = Assert.Single(
            await DatosSst.LeerAuditoriaAsync(_fabrica, "EquipoProteccion", id.Id),
            registro => registro.Accion == "Modificacion");
        using JsonDocument cambios = JsonDocument.Parse(cambio.Cambios);
        List<string> campos = cambios.RootElement.EnumerateArray().Select(item => item.GetProperty("campo").GetString() ?? string.Empty).ToList();
        Assert.Contains("Activo", campos);
    }

    private static GuardarEquipoProteccionRequestDto Solicitud(string? codigo, string? nombre, int? vidaUtilDias)
    {
        return new GuardarEquipoProteccionRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            VidaUtilDias = vidaUtilDias
        };
    }

    private async Task<HttpResponseMessage> CrearAsync(GuardarEquipoProteccionRequestDto solicitud)
    {
        return await _cliente.PostAsJsonAsync(RutaBase, solicitud);
    }

    private async Task AssertCrearAsync(GuardarEquipoProteccionRequestDto solicitud, int codigo, string mensaje)
    {
        await DatosSst.AssertErrorAsync(await CrearAsync(solicitud), codigo, mensaje);
    }

    private async Task<PaginaEquiposProteccionDto> LeerPaginaAsync(string ruta)
    {
        PaginaEquiposProteccionDto? pagina = await _cliente.GetFromJsonAsync<PaginaEquiposProteccionDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }
}
