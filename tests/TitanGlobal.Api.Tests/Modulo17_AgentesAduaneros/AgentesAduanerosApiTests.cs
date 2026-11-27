using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

namespace TitanGlobal.Api.Tests.Modulo17_AgentesAduaneros;

[Collection("Modulo04")]
public class AgentesAduanerosApiTests
{
    private const string Ruta = "/api/v1/agentes-aduaneros";

    private readonly HttpClient _clienteHttp;

    public AgentesAduanerosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        Modulo17Datos.Restablecer(fabrica);
    }

    [Fact]
    public async Task CreateNormalizesDataAndStartsActive()
    {
        GuardarAgenteRequestDto solicitud = CrearSolicitud("20556789012", "aa-0105");
        solicitud.RazonSocial = "  Agente Nuevo  ";
        solicitud.Email = " contacto@agente.pe ";
        solicitud.Telefono = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        int id = await Modulo17Datos.LeerIdAsync(respuesta);
        AgenteDto? agente = await _clienteHttp.GetFromJsonAsync<AgenteDto>($"{Ruta}/{id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(agente);
        Assert.Equal("Agente Nuevo", agente.RazonSocial);
        Assert.Equal("AA-0105", agente.NumeroRegistro);
        Assert.Equal("contacto@agente.pe", agente.Email);
        Assert.Null(agente.Telefono);
        Assert.True(agente.Activo);
        Assert.Equal(0, agente.TotalDeclaraciones);
    }

    [Fact]
    public async Task CreateValidatesEveryFieldWithItsMessage()
    {
        GuardarAgenteRequestDto rucCorto = CrearSolicitud("123", "AA-1");
        GuardarAgenteRequestDto rucConLetras = CrearSolicitud("2051234567A", "AA-1");
        GuardarAgenteRequestDto sinRazon = CrearSolicitud("20556789012", "AA-1");
        sinRazon.RazonSocial = "  ";
        GuardarAgenteRequestDto razonLarga = CrearSolicitud("20556789012", "AA-1");
        razonLarga.RazonSocial = new string('R', 201);
        GuardarAgenteRequestDto sinRegistro = CrearSolicitud("20556789012", " ");
        GuardarAgenteRequestDto registroLargo = CrearSolicitud("20556789012", new string('A', 21));
        GuardarAgenteRequestDto correoInvalido = CrearSolicitud("20556789012", "AA-1");
        correoInvalido.Email = "no-es-correo";
        GuardarAgenteRequestDto telefonoLargo = CrearSolicitud("20556789012", "AA-1");
        telefonoLargo.Telefono = new string('9', 21);

        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, rucCorto), HttpStatusCode.BadRequest, "El RUC debe tener 11 dígitos.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, rucConLetras), HttpStatusCode.BadRequest, "El RUC debe tener 11 dígitos.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, sinRazon), HttpStatusCode.BadRequest, "La razón social es obligatoria.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, razonLarga), HttpStatusCode.BadRequest, "La razón social no puede superar 200 caracteres.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, sinRegistro), HttpStatusCode.BadRequest, "El número de registro es obligatorio.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, registroLargo), HttpStatusCode.BadRequest, "El número de registro no puede superar 20 caracteres.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, correoInvalido), HttpStatusCode.BadRequest, "El correo no es válido.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsJsonAsync(Ruta, telefonoLargo), HttpStatusCode.BadRequest, "El teléfono no puede superar 20 caracteres.");
    }

    [Fact]
    public async Task CreateAndUpdateRejectRepeatedRucAndRegistration()
    {
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud("20512345678", "AA-9999")),
            HttpStatusCode.Conflict,
            "Ya existe un agente con el RUC 20512345678.");
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud("20556789012", "aa-0101")),
            HttpStatusCode.Conflict,
            "Ya existe un agente con el número de registro AA-0101.");
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PutAsJsonAsync($"{Ruta}/2", CrearSolicitud("20512345678", "AA-0102")),
            HttpStatusCode.Conflict,
            "Ya existe un agente con el RUC 20512345678.");
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.PutAsJsonAsync($"{Ruta}/2", CrearSolicitud("20523456789", "AA-0101")),
            HttpStatusCode.Conflict,
            "Ya existe un agente con el número de registro AA-0101.");
    }

    [Fact]
    public async Task UpdateKeepsOwnValuesAndFailsForMissingAgent()
    {
        GuardarAgenteRequestDto cambio = CrearSolicitud("20512345678", "AA-0101");
        cambio.RazonSocial = "Aduanas Renovadas";
        HttpResponseMessage actualizado = await _clienteHttp.PutAsJsonAsync($"{Ruta}/1", cambio);
        AgenteDto? agente = await _clienteHttp.GetFromJsonAsync<AgenteDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);
        Assert.NotNull(agente);
        Assert.Equal("Aduanas Renovadas", agente.RazonSocial);
        Assert.Equal(3, agente.TotalDeclaraciones);
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PutAsJsonAsync($"{Ruta}/999", cambio), HttpStatusCode.NotFound, "El agente aduanero 999 no existe.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}/999"), HttpStatusCode.NotFound, "El agente aduanero 999 no existe.");
    }

    [Fact]
    public async Task ActivateAndDeactivateToggleAndRejectRepeats()
    {
        HttpResponseMessage desactivado = await _clienteHttp.PostAsync($"{Ruta}/1/desactivar", null);
        AgenteDto? inactivo = await _clienteHttp.GetFromJsonAsync<AgenteDto>($"{Ruta}/1");
        HttpResponseMessage activado = await _clienteHttp.PostAsync($"{Ruta}/1/activar", null);
        AgenteDto? activo = await _clienteHttp.GetFromJsonAsync<AgenteDto>($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.OK, desactivado.StatusCode);
        Assert.NotNull(inactivo);
        Assert.False(inactivo.Activo);
        Assert.Equal(HttpStatusCode.OK, activado.StatusCode);
        Assert.NotNull(activo);
        Assert.True(activo.Activo);
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsync($"{Ruta}/1/activar", null), HttpStatusCode.Conflict, "El agente aduanero 1 ya está activo.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsync($"{Ruta}/3/desactivar", null), HttpStatusCode.Conflict, "El agente aduanero 3 ya está inactivo.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.PostAsync($"{Ruta}/999/activar", null), HttpStatusCode.NotFound, "El agente aduanero 999 no existe.");
    }

    [Fact]
    public async Task DeleteOnlyWorksWithoutDeclarations()
    {
        await Modulo17Datos.AssertBusinessErrorAsync(
            await _clienteHttp.DeleteAsync($"{Ruta}/1"),
            HttpStatusCode.Conflict,
            "El agente aduanero 1 tiene declaraciones y no se puede eliminar; desactívelo.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.DeleteAsync($"{Ruta}/999"), HttpStatusCode.NotFound, "El agente aduanero 999 no existe.");

        HttpResponseMessage eliminado = await _clienteHttp.DeleteAsync($"{Ruta}/3");
        HttpResponseMessage luego = await _clienteHttp.GetAsync($"{Ruta}/3");

        Assert.Equal(HttpStatusCode.NoContent, eliminado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, luego.StatusCode);
    }

    [Fact]
    public async Task ListFiltersSearchesPagesAndCountsDeclarations()
    {
        PaginaAgentesDto? todos = await _clienteHttp.GetFromJsonAsync<PaginaAgentesDto>(Ruta);
        PaginaAgentesDto? inactivos = await _clienteHttp.GetFromJsonAsync<PaginaAgentesDto>($"{Ruta}?activo=false");
        PaginaAgentesDto? porRuc = await _clienteHttp.GetFromJsonAsync<PaginaAgentesDto>($"{Ruta}?busqueda=%2020523456789%20");
        PaginaAgentesDto? porNombre = await _clienteHttp.GetFromJsonAsync<PaginaAgentesDto>($"{Ruta}?busqueda=ANDINOS");
        PaginaAgentesDto? segunda = await _clienteHttp.GetFromJsonAsync<PaginaAgentesDto>($"{Ruta}?pagina=2&tamanoPagina=2");

        Assert.NotNull(todos);
        Assert.Equal(3, todos.Total);
        Assert.Equal(new[] { "Aduanas del Pacifico", "Agencia Norte", "Despachos Andinos" }, todos.Items.Select(item => item.RazonSocial).ToArray());
        Assert.Equal(new[] { 3, 0, 1 }, todos.Items.Select(item => item.TotalDeclaraciones).ToArray());
        Assert.NotNull(inactivos);
        Assert.Equal("20534567890", Assert.Single(inactivos.Items).Ruc);
        Assert.NotNull(porRuc);
        Assert.Equal("Despachos Andinos", Assert.Single(porRuc.Items).RazonSocial);
        Assert.NotNull(porNombre);
        Assert.Equal("20523456789", Assert.Single(porNombre.Items).Ruc);
        Assert.NotNull(segunda);
        Assert.Equal(3, segunda.Total);
        Assert.Equal("Despachos Andinos", Assert.Single(segunda.Items).RazonSocial);
    }

    [Fact]
    public async Task ListRejectsInvalidPaging()
    {
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?pagina=0"), HttpStatusCode.BadRequest, "La página debe ser mayor o igual a 1.");
        await Modulo17Datos.AssertBusinessErrorAsync(await _clienteHttp.GetAsync($"{Ruta}?tamanoPagina=101"), HttpStatusCode.BadRequest, "El tamaño de página debe estar entre 1 y 100.");
    }

    private static GuardarAgenteRequestDto CrearSolicitud(string ruc, string registro)
    {
        return new GuardarAgenteRequestDto
        {
            Ruc = ruc,
            RazonSocial = "Agente de prueba",
            NumeroRegistro = registro
        };
    }
}
