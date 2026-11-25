using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo69_Capacitaciones;

[Collection("Modulo04")]
public class CursosApiTests
{
    private const string RutaBase = "/api/v1/cursos";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public CursosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosCapacitacion.Restablecer(fabrica);
    }

    // Mensajes 1 a 5 en el orden de la spec.
    [Fact]
    public async Task CreacionConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        string codigo = "El código es obligatorio y no puede superar 20 caracteres.";
        string nombre = "El nombre es obligatorio y no puede superar 150 caracteres.";
        string horas = "Las horas deben ser mayores que 0 y como máximo 500, con un decimal.";
        string tipo = "El tipo debe ser Interno, Externo o Virtual.";
        string proveedor = "El proveedor no puede superar 150 caracteres.";
        await AssertCrearAsync(Solicitud(null, "Curso", 8m, "Interno", null), 400, codigo);
        await AssertCrearAsync(Solicitud("   ", "Curso", 8m, "Interno", null), 400, codigo);
        await AssertCrearAsync(Solicitud(new string('A', 21), "Curso", 8m, "Interno", null), 400, codigo);
        await AssertCrearAsync(Solicitud("C1", null, 8m, "Interno", null), 400, nombre);
        await AssertCrearAsync(Solicitud("C1", new string('N', 151), 8m, "Interno", null), 400, nombre);
        await AssertCrearAsync(Solicitud("C1", "Curso", null, "Interno", null), 400, horas);
        await AssertCrearAsync(Solicitud("C1", "Curso", 0m, "Interno", null), 400, horas);
        await AssertCrearAsync(Solicitud("C1", "Curso", -1m, "Interno", null), 400, horas);
        await AssertCrearAsync(Solicitud("C1", "Curso", 500.1m, "Interno", null), 400, horas);
        await AssertCrearAsync(Solicitud("C1", "Curso", 8.25m, "Interno", null), 400, horas);
        await AssertCrearAsync(Solicitud("C1", "Curso", 8m, null, null), 400, tipo);
        await AssertCrearAsync(Solicitud("C1", "Curso", 8m, "interno", null), 400, tipo);
        await AssertCrearAsync(Solicitud("C1", "Curso", 8m, "Presencial", null), 400, tipo);
        await AssertCrearAsync(Solicitud("C1", "Curso", 8m, "Interno", new string('P', 151)), 400, proveedor);
        Assert.Empty(await DatosCapacitacion.LeerCursosAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud(new string('A', 20), new string('N', 150), 500m, "Virtual", new string('P', 150)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CrearAsync(Solicitud("C2", "Curso", 0.1m, "Externo", null))).StatusCode);
        Assert.Equal(2, (await DatosCapacitacion.LeerCursosAsync(_fabrica)).Count);
    }

    // Mensaje 6: el código se guarda en mayúsculas y es único, también al actualizar.
    [Fact]
    public async Task CrearYActualizarNormalizanElCodigoYRechazanDuplicadosConMensaje6()
    {
        HttpResponseMessage creado = await CrearAsync(Solicitud(" cur-1 ", " Seguridad industrial ", 12.5m, "Interno", " Area de seguridad "));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        CursoIdDto? id = await creado.Content.ReadFromJsonAsync<CursoIdDto>();
        Assert.NotNull(id);
        Curso guardado = Assert.Single(await DatosCapacitacion.LeerCursosAsync(_fabrica));
        Assert.Equal("CUR-1", guardado.Codigo);
        Assert.Equal("Seguridad industrial", guardado.Nombre);
        Assert.Equal(12.5m, guardado.Horas);
        Assert.Equal("Area de seguridad", guardado.Proveedor);
        Assert.True(guardado.Activo);
        string duplicado = "Ya existe un curso con el código CUR-1.";
        await AssertCrearAsync(Solicitud("cur-1", "Otro", 8m, "Interno", null), 409, duplicado);
        Assert.Single(await DatosCapacitacion.LeerCursosAsync(_fabrica));
        HttpResponseMessage segundo = await CrearAsync(Solicitud("CUR-2", "Segundo", 8m, "Virtual", null));
        CursoIdDto? idSegundo = await segundo.Content.ReadFromJsonAsync<CursoIdDto>();
        Assert.NotNull(idSegundo);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud("cur-1", "Segundo", 8m, "Virtual", null)), 409, duplicado);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud("cur-2", "Segundo editado", 16m, "Externo", null))).StatusCode);
        Curso editado = (await DatosCapacitacion.LeerCursosAsync(_fabrica)).Single(curso => curso.Id == idSegundo.Id);
        Assert.Equal("Segundo editado", editado.Nombre);
        Assert.Equal(16m, editado.Horas);
        Assert.Equal("Externo", editado.Tipo);
        Assert.Null(editado.Proveedor);
        await DatosCapacitacion.AssertErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaBase}/{idSegundo.Id}", Solicitud(null, "Segundo", 8m, "Virtual", null)),
            400,
            "El código es obligatorio y no puede superar 20 caracteres.");
    }

    // Mensaje 7 en todas las rutas con id.
    [Fact]
    public async Task CursoInexistenteDevuelve404ConMensaje7EnTodasLasRutas()
    {
        string mensaje = "El curso 9999 no existe.";
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999"), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PutAsJsonAsync($"{RutaBase}/9999", Solicitud("C1", "Curso", 8m, "Interno", null)), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/desactivacion", null), 404, mensaje);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/activacion", null), 404, mensaje);
    }

    // Mensaje 9 con «activo» e «inactivo».
    [Fact]
    public async Task ActivarYDesactivarCambianElEstadoYRechazanRepetirConMensaje9()
    {
        int id = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "EST", 8m);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/activacion", null), 409, "El curso EST ya está activo.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/desactivacion", null)).StatusCode);
        Assert.False((await DatosCapacitacion.LeerCursosAsync(_fabrica)).Single().Activo);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/desactivacion", null), 409, "El curso EST ya está inactivo.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/activacion", null)).StatusCode);
        Assert.True((await DatosCapacitacion.LeerCursosAsync(_fabrica)).Single().Activo);
    }

    [Fact]
    public async Task ListadoValidaPaginacionYFiltrosYAplicaBusquedaTipoYActivo()
    {
        await DatosCapacitacion.InsertarCursoAsync(_fabrica, "ALF", 8m);
        await DatosCapacitacion.InsertarCursoAsync(_fabrica, "BET", 16m, false);
        await DatosCapacitacion.InsertarCursoAsync(_fabrica, "GAM", 24m);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tipo=Otro"), 400, "El filtro tipo no es válido.");
        PaginaCursosDto todos = await LeerPaginaAsync(RutaBase);
        Assert.Equal(3, todos.Total);
        Assert.Equal(new[] { "ALF", "BET", "GAM" }, todos.Items.Select(item => item.Codigo).ToArray());
        PaginaCursosDto segunda = await LeerPaginaAsync($"{RutaBase}?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Equal(new[] { "GAM" }, segunda.Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "BET" }, (await LeerPaginaAsync($"{RutaBase}?busqueda=bet")).Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "ALF", "BET", "GAM" }, (await LeerPaginaAsync($"{RutaBase}?tipo=Interno")).Items.Select(item => item.Codigo).ToArray());
        Assert.Empty((await LeerPaginaAsync($"{RutaBase}?tipo=Virtual")).Items);
        Assert.Equal(new[] { "BET" }, (await LeerPaginaAsync($"{RutaBase}?activo=false")).Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { "ALF", "GAM" }, (await LeerPaginaAsync($"{RutaBase}?activo=true")).Items.Select(item => item.Codigo).ToArray());
        CursoListadoDto? detalle = await _cliente.GetFromJsonAsync<CursoListadoDto>($"{RutaBase}/{todos.Items[1].Id}");
        Assert.NotNull(detalle);
        Assert.Equal("BET", detalle.Codigo);
        Assert.Equal(16m, detalle.Horas);
        Assert.False(detalle.Activo);
    }

    [Fact]
    public async Task EscriturasExigenRolRrhhOAdminYLasLecturasSoloSesion()
    {
        int id = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "ROL", 8m);
        GuardarCursoRequestDto solicitud = Solicitud("X", "Curso", 8m, "Interno", null);
        HttpClient anonimo = DatosCapacitacion.ClienteAnonimo(_fabrica);
        string sesion = DatosCapacitacion.MensajeSinSesion;
        await DatosCapacitacion.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/{id}"), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, solicitud), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.PostAsync($"{RutaBase}/{id}/desactivacion", null), 401, sesion);
        HttpClient consulta = DatosCapacitacion.ClienteConRoles(_fabrica, "CONSULTA");
        string[] antes = await DatosCapacitacion.LeerFirmasAsync(_fabrica);
        string permiso = DatosCapacitacion.MensajeSinPermiso;
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/{id}")).StatusCode);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsJsonAsync(RutaBase, solicitud), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PutAsJsonAsync($"{RutaBase}/{id}", solicitud), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/desactivacion", null), 403, permiso);
        await DatosCapacitacion.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/activacion", null), 403, permiso);
        Assert.Equal(antes, await DatosCapacitacion.LeerFirmasAsync(_fabrica));
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenAdministrarCursos(string rol)
    {
        HttpClient cliente = DatosCapacitacion.ClienteConRoles(_fabrica, rol);
        HttpResponseMessage creado = await cliente.PostAsJsonAsync(RutaBase, Solicitud("R-" + rol, "Curso", 8m, "Interno", null));
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        CursoIdDto? id = await creado.Content.ReadFromJsonAsync<CursoIdDto>();
        Assert.NotNull(id);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/desactivacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{id.Id}/activacion", null)).StatusCode);
    }

    [Fact]
    public async Task CursoGeneraRegistrosDeAuditoria()
    {
        HttpResponseMessage creado = await CrearAsync(Solicitud("AUD", "Curso", 8m, "Interno", null));
        CursoIdDto? id = await creado.Content.ReadFromJsonAsync<CursoIdDto>();
        Assert.NotNull(id);
        Assert.Single(await DatosCapacitacion.LeerAuditoriaAsync(_fabrica, "Curso", id.Id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id.Id}/desactivacion", null)).StatusCode);
        RegistroAuditoria cambio = Assert.Single(
            await DatosCapacitacion.LeerAuditoriaAsync(_fabrica, "Curso", id.Id),
            registro => registro.Accion == "Modificacion");
        using JsonDocument cambios = JsonDocument.Parse(cambio.Cambios);
        List<string> campos = cambios.RootElement.EnumerateArray().Select(item => item.GetProperty("campo").GetString() ?? string.Empty).ToList();
        Assert.Contains("Activo", campos);
    }

    private static GuardarCursoRequestDto Solicitud(string? codigo, string? nombre, decimal? horas, string? tipo, string? proveedor)
    {
        return new GuardarCursoRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Horas = horas,
            Tipo = tipo,
            Proveedor = proveedor
        };
    }

    private async Task<HttpResponseMessage> CrearAsync(GuardarCursoRequestDto solicitud)
    {
        return await _cliente.PostAsJsonAsync(RutaBase, solicitud);
    }

    private async Task AssertCrearAsync(GuardarCursoRequestDto solicitud, int codigo, string mensaje)
    {
        await DatosCapacitacion.AssertErrorAsync(await CrearAsync(solicitud), codigo, mensaje);
    }

    private async Task<PaginaCursosDto> LeerPaginaAsync(string ruta)
    {
        PaginaCursosDto? pagina = await _cliente.GetFromJsonAsync<PaginaCursosDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }
}
