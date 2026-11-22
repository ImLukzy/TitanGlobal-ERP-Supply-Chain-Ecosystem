using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo73_Sucursales;

[Collection("Modulo04")]
public class SucursalesApiTests
{
    private const string Ruta = "/api/v1/sucursales";
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;

    public SucursalesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<Sucursal>());
        db.SaveChanges();
    }

    internal static GuardarSucursalRequestDto Valida(string codigo = "SUC-73")
    {
        return new GuardarSucursalRequestDto
        {
            Codigo = codigo,
            Nombre = " Sucursal Norte ",
            Tipo = " oficina ",
            Pais = " Peru ",
            Ciudad = " Lima ",
            Direccion = " Avenida Norte 123 ",
            Contacto = " ",
            Email = " ",
            Telefono = " ",
            Observaciones = " ",
            FechaApertura = new DateTime(2026, 11, 23, 10, 30, 12, 345)
        };
    }

    private async Task<int> CrearAsync(GuardarSucursalRequestDto? datos = null)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, datos ?? Valida());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<SucursalDetalleDto> DetalleAsync(int id)
    {
        SucursalDetalleDto? detalle = await _cliente.GetFromJsonAsync<SucursalDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(detalle);
        return detalle;
    }

    private static async Task ErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, cuerpo.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, cuerpo.GetProperty("Message").GetString());
    }

    [Theory]
    [InlineData("Codigo", "código", 20)]
    [InlineData("Nombre", "nombre", 150)]
    [InlineData("Pais", "país", 100)]
    [InlineData("Ciudad", "ciudad", 100)]
    [InlineData("Direccion", "dirección", 300)]
    public async Task ValidaTextosYLimitesSinPersistir(string campo, string etiqueta, int maximo)
    {
        foreach (bool largo in new[] { false, true })
        {
            GuardarSucursalRequestDto datos = Valida();
            typeof(GuardarSucursalRequestDto).GetProperty(campo)!.SetValue(datos, largo ? new string('x', maximo + 1) : " ");
            await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, largo ? $"El campo {etiqueta} no puede superar {maximo} caracteres." : $"El campo {etiqueta} es obligatorio.");
            Assert.Equal(0, (await _cliente.GetFromJsonAsync<ResumenSucursalesDto>(Ruta + "/resumen"))!.Total);
            typeof(GuardarSucursalRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', maximo));
            int id = await CrearAsync(datos);
            Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{id}/desactivacion", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        }
    }
    [Theory]
    [InlineData("Contacto", 100, "El contacto no puede superar 100 caracteres.")]
    [InlineData("Email", 150, "El email no puede superar 150 caracteres.")]
    [InlineData("Telefono", 30, "El teléfono no puede superar 30 caracteres.")]
    [InlineData("Observaciones", 500, "Las observaciones no pueden superar 500 caracteres.")]
    public async Task ValidaOpcionalesYSusLimites(string campo, int maximo, string mensaje)
    {
        int id = await CrearAsync();
        GuardarSucursalRequestDto datos = Valida();
        typeof(GuardarSucursalRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', maximo + 1));
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos), HttpStatusCode.BadRequest, mensaje);
        Assert.Null(typeof(SucursalDetalleDto).GetProperty(campo)!.GetValue(await DetalleAsync(id)));
        string limite = campo == "Email" ? new string('a', 138) + "@example.com" : new string('x', maximo);
        typeof(GuardarSucursalRequestDto).GetProperty(campo)!.SetValue(datos, limite);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        Assert.Equal(maximo, ((string)typeof(SucursalDetalleDto).GetProperty(campo)!.GetValue(await DetalleAsync(id))!).Length);
    }
    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@@b")]
    [InlineData("Nombre <a@b>")]
    [InlineData("a b@c")]
    [InlineData("@b")]
    public async Task ValidaEmailSinMutar(string email)
    {
        int id = await CrearAsync();
        GuardarSucursalRequestDto datos = Valida();
        datos.Email = email;
        datos.Nombre = "Cambio inválido";
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos), HttpStatusCode.BadRequest, "El email no es válido.");
        Assert.Equal("Sucursal Norte", (await DetalleAsync(id)).Nombre);
    }
    [Fact]
    public async Task ValidaTipoYFechaOpcional()
    {
        GuardarSucursalRequestDto datos = Valida();
        datos.Tipo = "Otro";
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "El tipo debe ser Filial, Planta u Oficina.");
        datos.Tipo = " pLaNtA ";
        datos.FechaApertura = default(DateTime);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, datos), HttpStatusCode.BadRequest, "La fecha de apertura no es válida.");
        datos.FechaApertura = null;
        datos.Email = " a@b ";
        int id = await CrearAsync(datos);
        SucursalDetalleDto detalle = await DetalleAsync(id);
        Assert.Null(detalle.FechaApertura);
        Assert.Equal("Planta", detalle.Tipo);
        Assert.Equal("a@b", detalle.Email);
    }
    [Fact]
    public async Task NormalizaTextosUtcYOpcionalesYNoExponeEstadoEnEntrada()
    {
        int id = await CrearAsync();
        SucursalDetalleDto detalle = await DetalleAsync(id);
        Assert.True(detalle.Activa);
        Assert.Equal("Sucursal Norte", detalle.Nombre);
        Assert.Equal("Peru", detalle.Pais);
        Assert.Equal("Avenida Norte 123", detalle.Direccion);
        Assert.Null(detalle.Contacto);
        Assert.Null(detalle.Email);
        Assert.Null(detalle.Telefono);
        Assert.Null(detalle.Observaciones);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaApertura!.Value.Kind);
        Assert.Equal(345, detalle.FechaApertura.Value.Millisecond);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaRegistro.Kind);
        Assert.NotEqual(default(DateTime), detalle.FechaRegistro);
        Assert.Null(detalle.FechaCambioEstado);
        Assert.Null(typeof(GuardarSucursalRequestDto).GetProperty("Activa"));
    }
    [Fact]
    public async Task CodigoUnicoInclusoInactivaYExcluyeSuPropioId()
    {
        int id = await CrearAsync();
        await _cliente.PostAsync($"{Ruta}/{id}/desactivacion", null);
        await ErrorAsync(await _cliente.PostAsJsonAsync(Ruta, Valida(" suc-73 ")), HttpStatusCode.Conflict, "Ya existe una sucursal con el código suc-73.");
        int otra = await CrearAsync(Valida("OTRA"));
        await ErrorAsync(await _cliente.PutAsJsonAsync($"{Ruta}/{otra}", Valida("suc-73")), HttpStatusCode.Conflict, "Ya existe una sucursal con el código suc-73.");
        Assert.Equal("OTRA", (await DetalleAsync(otra)).Codigo);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", Valida("suc-73"))).StatusCode);
        Assert.Equal("suc-73", (await DetalleAsync(id)).Codigo);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditaEnAmbosEstadosSinCambiarFechasDeSistema(bool desactivar)
    {
        int id = await CrearAsync();
        if (desactivar)
        {
            await _cliente.PostAsync($"{Ruta}/{id}/desactivacion", null);
        }
        SucursalDetalleDto antes = await DetalleAsync(id);
        GuardarSucursalRequestDto datos = Valida();
        datos.Nombre = " Editada ";
        datos.FechaApertura = null;
        datos.Contacto = " Ana ";
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PutAsJsonAsync($"{Ruta}/{id}", datos)).StatusCode);
        SucursalDetalleDto despues = await DetalleAsync(id);
        Assert.Equal("Editada", despues.Nombre);
        Assert.Equal("Ana", despues.Contacto);
        Assert.Null(despues.FechaApertura);
        Assert.Equal(antes.Activa, despues.Activa);
        Assert.Equal(antes.FechaRegistro, despues.FechaRegistro);
        Assert.Equal(antes.FechaCambioEstado, despues.FechaCambioEstado);
    }
    [Fact]
    public async Task ActivaDesactivaSinCuerpoYConservaDatosEnRepeticionesRechazadas()
    {
        int id = await CrearAsync();
        SucursalDetalleDto inicial = await DetalleAsync(id);
        await ErrorAsync(await _cliente.PostAsync($"{Ruta}/{id}/activacion", null), HttpStatusCode.Conflict, "La sucursal ya está activa.");
        Assert.Null((await DetalleAsync(id)).FechaCambioEstado);
        await ErrorAsync(await _cliente.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.Conflict, "Desactiva la sucursal antes de eliminarla.");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{id}/desactivacion", null)).StatusCode);
        SucursalDetalleDto inactiva = await DetalleAsync(id);
        Assert.False(inactiva.Activa);
        Assert.Equal(DateTimeKind.Utc, inactiva.FechaCambioEstado!.Value.Kind);
        Assert.Equal(inicial.FechaRegistro, inactiva.FechaRegistro);
        Assert.Equal(inicial.FechaApertura, inactiva.FechaApertura);
        Assert.Equal(inicial.Direccion, inactiva.Direccion);
        await ErrorAsync(await _cliente.PostAsync($"{Ruta}/{id}/desactivacion", null), HttpStatusCode.Conflict, "La sucursal ya está inactiva.");
        Assert.Equal(inactiva.FechaCambioEstado, (await DetalleAsync(id)).FechaCambioEstado);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{Ruta}/{id}/activacion", null)).StatusCode);
        SucursalDetalleDto activa = await DetalleAsync(id);
        Assert.True(activa.Activa);
        Assert.NotNull(activa.FechaCambioEstado);
        Assert.Equal(inicial.FechaRegistro, activa.FechaRegistro);
        Assert.Equal(inicial.Direccion, activa.Direccion);
        await ErrorAsync(await _cliente.PostAsync($"{Ruta}/{id}/activacion", null), HttpStatusCode.Conflict, "La sucursal ya está activa.");
        Assert.Equal(activa.FechaCambioEstado, (await DetalleAsync(id)).FechaCambioEstado);
    }
    [Fact]
    public async Task EliminaSoloInactivaYExigeJwt()
    {
        int id = await CrearAsync();
        await _cliente.PostAsync($"{Ruta}/{id}/desactivacion", null);
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        await ErrorAsync(await _cliente.GetAsync($"{Ruta}/{id}"), HttpStatusCode.NotFound, $"La sucursal {id} no existe.");
        using HttpClient anonimo = _fabrica.CreateClient();
        anonimo.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Ruta)).StatusCode);
    }
    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("POST", "/activacion")]
    [InlineData("POST", "/desactivacion")]
    public async Task BuscaAntesDeValidar(string metodo, string sufijo)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + "/2147483647" + sufijo);
        if (metodo == "PUT")
        {
            peticion.Content = JsonContent.Create(new { });
        }
        await ErrorAsync(await _cliente.SendAsync(peticion), HttpStatusCode.NotFound, "La sucursal 2147483647 no existe.");
    }
    [Theory]
    [InlineData("pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tipo=Otro", "El tipo debe ser Filial, Planta u Oficina.")]
    public async Task ValidaConsulta(string query, string mensaje)
    {
        await ErrorAsync(await _cliente.GetAsync(Ruta + "?" + query), HttpStatusCode.BadRequest, mensaje);
    }
    [Fact]
    public async Task CombinaFiltrosPaisExactoYFalsePaginacionYResumen()
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            GuardarSucursalRequestDto datos = Valida(((char)('A' + i)).ToString());
            datos.Tipo = new[] { "Filial", "Filial", "Planta", "Planta", "Oficina", "Oficina" }[i];
            datos.Pais = i < 4 ? "Peru" : "Chile";
            ids.Add(await CrearAsync(datos));
            if (i == 1 || i == 4)
            {
                await _cliente.PostAsync($"{Ruta}/{ids[i]}/desactivacion", null);
            }
        }
        foreach (string buscar in new[] { "sucursal norte", "lima" })
        {
            PaginaSucursalesDto pagina = (await _cliente.GetFromJsonAsync<PaginaSucursalesDto>($"{Ruta}?busqueda={Uri.EscapeDataString(buscar)}&pagina=2&tamanoPagina=2"))!;
            Assert.Equal(6, pagina.Total);
            Assert.Equal(new[] { ids[2], ids[3] }, pagina.Items.Select(sucursal => sucursal.Id));
        }
        Assert.Equal(ids[1], Assert.Single((await _cliente.GetFromJsonAsync<PaginaSucursalesDto>($"{Ruta}?tipo=fIlIaL&pais=pErU&activa=false"))!.Items).Id);
        Assert.Empty((await _cliente.GetFromJsonAsync<PaginaSucursalesDto>($"{Ruta}?pais=Pe"))!.Items);
        Assert.Equal(2, (await _cliente.GetFromJsonAsync<PaginaSucursalesDto>($"{Ruta}?activa=false"))!.Total);
        Assert.Empty((await _cliente.GetFromJsonAsync<PaginaSucursalesDto>($"{Ruta}?pagina=2147483647&tamanoPagina=100"))!.Items);
        ResumenSucursalesDto resumen = (await _cliente.GetFromJsonAsync<ResumenSucursalesDto>(Ruta + "/resumen"))!;
        Assert.Equal(6, resumen.Total);
        Assert.Equal(4, resumen.Activas);
        Assert.Equal(2, resumen.Inactivas);
        Assert.Equal(2, resumen.Filiales);
        Assert.Equal(2, resumen.Plantas);
        Assert.Equal(2, resumen.Oficinas);
    }
}
