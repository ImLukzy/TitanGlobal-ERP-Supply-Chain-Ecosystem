using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo59_CentrosCosto;

public class CentrosCostoApiTests : IDisposable
{
    private const string Ruta = "/api/v1/centros-costo";
    private const string DatosInvalidos = "El código debe tener entre 2 y 20 caracteres y los datos del centro deben ser válidos.";
    private const string CentroInexistente = "El centro de costo no existe.";
    private const string CodigoDuplicado = "Ya existe un centro de costo con ese código.";
    private const string JerarquiaInvalida = "La jerarquía de centros debe evitar ciclos y tener como máximo tres niveles.";
    private const string PadreInactivo = "El centro de costo padre debe estar activo.";
    private const string EstadoRepetido = "El centro de costo ya tiene el estado solicitado.";
    private readonly CentrosCostoApiFactory _factory = new CentrosCostoApiFactory();
    private readonly HttpClient _cliente;

    public CentrosCostoApiTests()
    {
        _cliente = _factory.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, _factory.Services);
    }

    [Theory]
    [InlineData(" ab ", "AB")]
    [InlineData(" 12345678901234567890 ", "12345678901234567890")]
    [InlineData(" ña ", "ÑA")]
    public async Task CreaNormalizadoYDevuelveDetalleEnLocation(string codigo, string esperado)
    {
        GuardarCentroCostoRequestDto solicitud = new GuardarCentroCostoRequestDto
        {
            Codigo = codigo,
            Nombre = "  Operaciones  ",
            Descripcion = "  Descripción del centro  "
        };
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        CentroCostoDetalleDto creado = await LeerAsync<CentroCostoDetalleDto>(respuesta);
        Assert.Equal(esperado, creado.Codigo);
        Assert.Equal("Operaciones", creado.Nombre);
        Assert.Equal("Descripción del centro", creado.Descripcion);
        Assert.Equal(1, creado.Nivel);
        Assert.Null(creado.ResponsableEmpleadoId);
        Assert.Null(creado.CentroCostoPadreId);
        Assert.True(creado.Activo);
        Assert.Equal(DateTimeKind.Utc, creado.FechaRegistro.Kind);
        Assert.Null(creado.FechaDesactivacion);

        Uri ubicacion = Assert.IsType<Uri>(respuesta.Headers.Location);
        Assert.Equal($"{Ruta}/{creado.Id}", ubicacion.AbsolutePath);
        using HttpResponseMessage detalle = await _cliente.GetAsync(ubicacion);
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        using JsonDocument cuerpo = await JsonDocument.ParseAsync(await detalle.Content.ReadAsStreamAsync());
        Assert.Equal(creado.Id, cuerpo.RootElement.GetProperty("id").GetInt32());
        Assert.False(cuerpo.RootElement.TryGetProperty("importe", out _));
        Assert.False(cuerpo.RootElement.TryGetProperty("moneda", out _));
    }

    [Fact]
    public async Task ResponsableExistenteEsOpcionalYEditable()
    {
        int empleadoId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            Empleado empleado = new Empleado
            {
                Legajo = "EMP-59",
                TipoDocumento = "DNI",
                NumeroDocumento = "59000001",
                Nombres = "Ana",
                Apellidos = "Pérez",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                FechaIngreso = new DateOnly(2020, 1, 1),
                Cargo = "Analista",
                Area = "Finanzas",
                Estado = "Activo"
            };
            db.Empleados.Add(empleado);
            await db.SaveChangesAsync();
            empleadoId = empleado.Id;
        }

        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(Ruta, Solicitud("RESP", responsableId: empleadoId));
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        CentroCostoDetalleDto creado = await LeerAsync<CentroCostoDetalleDto>(alta);
        Assert.Equal(empleadoId, creado.ResponsableEmpleadoId);

        using HttpResponseMessage cambio = await _cliente.PutAsJsonAsync($"{Ruta}/{creado.Id}", Solicitud("RESP"));
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        Assert.Null((await LeerAsync<CentroCostoDetalleDto>(cambio)).ResponsableEmpleadoId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData(" A ")]
    [InlineData("123456789012345678901")]
    [InlineData("😀")]
    [InlineData("AB\0")]
    public async Task RechazaCodigoInvalidoSinCrearFila(string? codigo)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, new GuardarCentroCostoRequestDto
        {
            Codigo = codigo,
            Nombre = "Centro"
        });

        await AssertErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
        Assert.Empty((await ListarAsync()).Items);
    }

    [Fact]
    public async Task ValidaNombreDescripcionYReferenciasNoPositivas()
    {
        GuardarCentroCostoRequestDto[] invalidas =
        [
            new GuardarCentroCostoRequestDto { Codigo = "XX", Nombre = "  " },
            new GuardarCentroCostoRequestDto { Codigo = "XX", Nombre = new string('x', 121) },
            new GuardarCentroCostoRequestDto { Codigo = "XX", Nombre = "Centro", Descripcion = new string('x', 301) },
            Solicitud("XX", padreId: 0),
            Solicitud("XX", responsableId: -1)
        ];
        foreach (GuardarCentroCostoRequestDto invalida in invalidas)
        {
            using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, invalida);
            await AssertErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
        }
    }

    [Fact]
    public async Task DuplicadoNormalizadoEnAltaYEdicionMantieneLosDatos()
    {
        await CrearAsync("DUP");
        CentroCostoDetalleDto otro = await CrearAsync("OTRO");
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(Ruta, Solicitud(" dup "));
        await AssertErrorAsync(alta, HttpStatusCode.Conflict, CodigoDuplicado);
        using HttpResponseMessage cambio = await _cliente.PutAsJsonAsync($"{Ruta}/{otro.Id}", Solicitud(" dup "));
        await AssertErrorAsync(cambio, HttpStatusCode.Conflict, CodigoDuplicado);

        Assert.Equal("OTRO", (await ObtenerAsync(otro.Id)).Codigo);
        Assert.Equal(2, (await ListarAsync()).TotalRegistros);
    }

    [Theory]
    [InlineData(true, "El centro de costo padre no existe.")]
    [InlineData(false, "El empleado responsable no existe.")]
    public async Task ReferenciasInexistentesDevuelven404(bool esPadre, string literal)
    {
        GuardarCentroCostoRequestDto solicitud = esPadre ? Solicitud("XX", padreId: 999) : Solicitud("XX", responsableId: 999);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await AssertErrorAsync(respuesta, HttpStatusCode.NotFound, literal);
        Assert.Empty((await ListarAsync()).Items);
    }

    [Fact]
    public async Task OperacionesSobreCentroInexistenteDevuelven404()
    {
        using HttpResponseMessage consulta = await _cliente.GetAsync($"{Ruta}/999");
        using HttpResponseMessage cambio = await _cliente.PutAsJsonAsync($"{Ruta}/999", Solicitud("XX"));
        using HttpResponseMessage baja = await _cliente.PostAsync($"{Ruta}/999/desactivar", null);
        using HttpResponseMessage alta = await _cliente.PostAsync($"{Ruta}/999/reactivar", null);
        foreach (HttpResponseMessage respuesta in new[] { consulta, cambio, baja, alta })
        {
            await AssertErrorAsync(respuesta, HttpStatusCode.NotFound, CentroInexistente);
        }
    }

    [Fact]
    public async Task ConsultaTresNivelesYRechazaElCuarto()
    {
        CentroCostoDetalleDto raiz = await CrearAsync("RAIZ");
        CentroCostoDetalleDto hijo = await CrearAsync("HIJO", raiz.Id);
        CentroCostoDetalleDto nieto = await CrearAsync("NIETO", hijo.Id);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, Solicitud("BISNIETO", nieto.Id));
        await AssertErrorAsync(respuesta, HttpStatusCode.Conflict, JerarquiaInvalida);

        Assert.Equal(2, hijo.Nivel);
        Assert.Equal(3, (await ObtenerAsync(nieto.Id)).Nivel);
        Assert.Equal(raiz.Id, hijo.CentroCostoPadreId);
        Assert.Equal(nieto.Id, Assert.Single((await ListarAsync($"?centroCostoPadreId={hijo.Id}")).Items).Id);
        Assert.Equal(3, (await ListarAsync()).TotalRegistros);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RechazaAutorreferenciaYCicloIndirecto(bool asignaPadrePropio)
    {
        CentroCostoDetalleDto raiz = await CrearAsync("RAIZ");
        CentroCostoDetalleDto hijo = await CrearAsync("HIJO", raiz.Id);
        CentroCostoDetalleDto nieto = await CrearAsync("NIETO", hijo.Id);
        int padreId = asignaPadrePropio ? raiz.Id : nieto.Id;

        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{Ruta}/{raiz.Id}", Solicitud("RAIZ", padreId));
        await AssertErrorAsync(respuesta, HttpStatusCode.Conflict, JerarquiaInvalida);
        Assert.Null((await ObtenerAsync(raiz.Id)).CentroCostoPadreId);
        Assert.Equal(hijo.Id, (await ObtenerAsync(nieto.Id)).CentroCostoPadreId);
    }

    [Fact]
    public async Task PuedeMoverSubarbolYCambiarCodigoSinCambiarIdentidad()
    {
        CentroCostoDetalleDto centro = await CrearAsync("CENTRO");
        CentroCostoDetalleDto hijo = await CrearAsync("HIJO", centro.Id);
        CentroCostoDetalleDto raiz = await CrearAsync("RAIZ");

        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{Ruta}/{centro.Id}", Solicitud("  nuevo ", raiz.Id));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        CentroCostoDetalleDto actualizado = await LeerAsync<CentroCostoDetalleDto>(respuesta);

        Assert.Equal("NUEVO", actualizado.Codigo);
        Assert.Equal(centro.Id, actualizado.Id);
        Assert.Equal(centro.FechaRegistro, actualizado.FechaRegistro);
        Assert.Equal(raiz.Id, actualizado.CentroCostoPadreId);
        Assert.Equal(2, actualizado.Nivel);
        Assert.Equal(3, (await ObtenerAsync(hijo.Id)).Nivel);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RechazaMoverCentroSiHijoONietoPasaAlCuartoNivel(bool mueveAlTercerNivel)
    {
        CentroCostoDetalleDto origen = await CrearAsync("ORIGEN");
        CentroCostoDetalleDto hijo = await CrearAsync("HIJO", origen.Id);
        CentroCostoDetalleDto destino = await CrearAsync("DESTINO");
        int padreId = destino.Id;
        if (mueveAlTercerNivel)
        {
            padreId = (await CrearAsync("DESTINO-HIJO", destino.Id)).Id;
        }
        else
        {
            await CrearAsync("NIETO", hijo.Id);
        }

        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{Ruta}/{origen.Id}", Solicitud("ORIGEN", padreId));
        await AssertErrorAsync(respuesta, HttpStatusCode.Conflict, JerarquiaInvalida);
        Assert.Null((await ObtenerAsync(origen.Id)).CentroCostoPadreId);
        Assert.Equal(2, (await ObtenerAsync(hijo.Id)).Nivel);
    }

    [Fact]
    public async Task VigenciaRespetaHijosActivosYConservaLaReferenciaHistorica()
    {
        CentroCostoDetalleDto raiz = await CrearAsync("RAIZ");
        CentroCostoDetalleDto hijo = await CrearAsync("HIJO", raiz.Id);
        using HttpResponseMessage bloqueado = await _cliente.PostAsync($"{Ruta}/{raiz.Id}/desactivar", null);
        await AssertErrorAsync(bloqueado, HttpStatusCode.Conflict, "No se puede desactivar un centro de costo con hijos activos.");
        Assert.True((await ObtenerAsync(raiz.Id)).Activo);

        using HttpResponseMessage bajaHijo = await _cliente.PostAsync($"{Ruta}/{hijo.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.OK, bajaHijo.StatusCode);
        CentroCostoDetalleDto inactivo = await LeerAsync<CentroCostoDetalleDto>(bajaHijo);
        Assert.False(inactivo.Activo);
        Assert.Equal(DateTimeKind.Utc, inactivo.FechaDesactivacion?.Kind);
        using HttpResponseMessage bajaRepetida = await _cliente.PostAsync($"{Ruta}/{hijo.Id}/desactivar", null);
        await AssertErrorAsync(bajaRepetida, HttpStatusCode.Conflict, EstadoRepetido);

        using HttpResponseMessage bajaRaiz = await _cliente.PostAsync($"{Ruta}/{raiz.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.OK, bajaRaiz.StatusCode);
        CentroCostoDetalleDto historico = await ObtenerAsync(hijo.Id);
        Assert.Equal(raiz.Id, historico.CentroCostoPadreId);
        Assert.Equal(hijo.FechaRegistro, historico.FechaRegistro);
        Assert.Equal(inactivo.FechaDesactivacion, historico.FechaDesactivacion);

        using HttpResponseMessage reactivarHijoAntes = await _cliente.PostAsync($"{Ruta}/{hijo.Id}/reactivar", null);
        await AssertErrorAsync(reactivarHijoAntes, HttpStatusCode.Conflict, PadreInactivo);
        using HttpResponseMessage reactivarRaiz = await _cliente.PostAsync($"{Ruta}/{raiz.Id}/reactivar", null);
        Assert.Equal(HttpStatusCode.OK, reactivarRaiz.StatusCode);
        using HttpResponseMessage reactivarHijo = await _cliente.PostAsync($"{Ruta}/{hijo.Id}/reactivar", null);
        Assert.Equal(HttpStatusCode.OK, reactivarHijo.StatusCode);
        CentroCostoDetalleDto reactivado = await LeerAsync<CentroCostoDetalleDto>(reactivarHijo);
        Assert.True(reactivado.Activo);
        Assert.Equal(hijo.Id, reactivado.Id);
        Assert.Equal(hijo.FechaRegistro, reactivado.FechaRegistro);
        Assert.Null(reactivado.FechaDesactivacion);
        using HttpResponseMessage reactivarRepetida = await _cliente.PostAsync($"{Ruta}/{hijo.Id}/reactivar", null);
        await AssertErrorAsync(reactivarRepetida, HttpStatusCode.Conflict, EstadoRepetido);
    }

    [Fact]
    public async Task PadreInactivoImpideAltaYMudanzaPeroPermiteEdicionSinCambiarVinculo()
    {
        CentroCostoDetalleDto raiz = await CrearAsync("RAIZ");
        CentroCostoDetalleDto hijo = await CrearAsync("HIJO", raiz.Id);
        CentroCostoDetalleDto otro = await CrearAsync("OTRO");
        using HttpResponseMessage bajaHijo = await _cliente.PostAsync($"{Ruta}/{hijo.Id}/desactivar", null);
        using HttpResponseMessage bajaRaiz = await _cliente.PostAsync($"{Ruta}/{raiz.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.OK, bajaHijo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bajaRaiz.StatusCode);

        using HttpResponseMessage nuevo = await _cliente.PostAsJsonAsync(Ruta, Solicitud("NUEVO", raiz.Id));
        await AssertErrorAsync(nuevo, HttpStatusCode.Conflict, PadreInactivo);
        using HttpResponseMessage mudanza = await _cliente.PutAsJsonAsync($"{Ruta}/{otro.Id}", Solicitud("OTRO", raiz.Id));
        await AssertErrorAsync(mudanza, HttpStatusCode.Conflict, PadreInactivo);
        using HttpResponseMessage mantener = await _cliente.PutAsJsonAsync($"{Ruta}/{hijo.Id}", Solicitud("HISTORICO", raiz.Id));
        Assert.Equal(HttpStatusCode.OK, mantener.StatusCode);
        CentroCostoDetalleDto historico = await LeerAsync<CentroCostoDetalleDto>(mantener);
        Assert.Equal("HISTORICO", historico.Codigo);
        Assert.False(historico.Activo);
        Assert.Equal(raiz.Id, historico.CentroCostoPadreId);
    }

    [Fact]
    public async Task ListaConFiltrosOrdenPaginacionYTotales()
    {
        CentroCostoDetalleDto raiz = await CrearAsync("RAIZ");
        await CrearAsync("ZETA", raiz.Id);
        CentroCostoDetalleDto inactivo = await CrearAsync("INACTIVO", raiz.Id);
        await CrearAsync("ALFA", raiz.Id);
        using HttpResponseMessage baja = await _cliente.PostAsync($"{Ruta}/{inactivo.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.OK, baja.StatusCode);

        PaginaCentrosCostoDto sinFiltros = await ListarAsync();
        Assert.Equal(1, sinFiltros.Pagina);
        Assert.Equal(20, sinFiltros.TamanoPagina);
        Assert.Equal(4, sinFiltros.TotalRegistros);
        string filtro = $"?centroCostoPadreId={raiz.Id}&activo=true&tamanoPagina=1";
        PaginaCentrosCostoDto pagina1 = await ListarAsync(filtro);
        Assert.Equal("ALFA", Assert.Single(pagina1.Items).Codigo);
        Assert.Equal(2, pagina1.TotalRegistros);
        Assert.Equal(2, pagina1.TotalPaginas);
        PaginaCentrosCostoDto pagina2 = await ListarAsync(filtro + "&pagina=2");
        Assert.Equal(2, pagina2.Pagina);
        Assert.Equal("ZETA", Assert.Single(pagina2.Items).Codigo);
        Assert.Equal(inactivo.Id, Assert.Single((await ListarAsync($"?centroCostoPadreId={raiz.Id}&activo=false")).Items).Id);
        Assert.Empty((await ListarAsync("?pagina=2147483647&tamanoPagina=100")).Items);
    }

    [Theory]
    [InlineData("?pagina=0")]
    [InlineData("?tamanoPagina=0")]
    [InlineData("?tamanoPagina=101")]
    [InlineData("?centroCostoPadreId=0")]
    public async Task FiltrosFueraDeRangoDevuelven400(string filtro)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + filtro);
        await AssertErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
    }

    [Theory]
    [InlineData("POST", "")]
    [InlineData("PUT", "/1")]
    public async Task GuardarSinCuerpoDevuelveElLiteralDelPipeline(string metodo, string sufijo)
    {
        using HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + sufijo);
        using HttpResponseMessage respuesta = await _cliente.SendAsync(peticion);
        await AssertErrorAsync(respuesta, HttpStatusCode.BadRequest, "El cuerpo de la petición (Body) no puede estar vacío.");
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("GET", "/1")]
    [InlineData("POST", "")]
    [InlineData("PUT", "/1")]
    [InlineData("POST", "/1/desactivar")]
    [InlineData("POST", "/1/reactivar")]
    public async Task TodasLasRutasExigenSesion(string metodo, string sufijo)
    {
        using HttpClient anonimo = _factory.CreateClient();
        using HttpRequestMessage peticion = Peticion(metodo, sufijo);
        using HttpResponseMessage respuesta = await anonimo.SendAsync(peticion);
        await AssertErrorAsync(respuesta, HttpStatusCode.Unauthorized, "Debe iniciar sesión para acceder a este recurso.");
    }

    [Theory]
    [InlineData("POST", "")]
    [InlineData("PUT", "/1")]
    [InlineData("POST", "/1/desactivar")]
    [InlineData("POST", "/1/reactivar")]
    public async Task UsuarioDeConsultaPuedeLeerPeroNoEscribir(string metodo, string sufijo)
    {
        CentroCostoDetalleDto centro = await CrearAsync("LECTURA");
        using HttpClient consulta = _factory.CreateClient();
        consulta.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_factory.Services));
        using HttpResponseMessage lista = await consulta.GetAsync(Ruta);
        using HttpResponseMessage detalle = await consulta.GetAsync($"{Ruta}/{centro.Id}");
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);

        using HttpRequestMessage peticion = Peticion(metodo, sufijo);
        using HttpResponseMessage respuesta = await consulta.SendAsync(peticion);
        await AssertErrorAsync(respuesta, HttpStatusCode.Forbidden, "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.");
        Assert.True((await ObtenerAsync(centro.Id)).Activo);
        Assert.Single((await ListarAsync()).Items);
    }

    [Fact]
    public async Task FinanzasPuedeCrearEditarDesactivarYReactivar()
    {
        _cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_factory.Services, "FINANZAS"));
        CentroCostoDetalleDto centro = await CrearAsync("FIN");
        using HttpResponseMessage cambio = await _cliente.PutAsJsonAsync($"{Ruta}/{centro.Id}", Solicitud("FINANZAS"));
        using HttpResponseMessage baja = await _cliente.PostAsync($"{Ruta}/{centro.Id}/desactivar", null);
        using HttpResponseMessage alta = await _cliente.PostAsync($"{Ruta}/{centro.Id}/reactivar", null);
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        Assert.Equal(HttpStatusCode.OK, baja.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _factory.Dispose();
    }

    private static HttpRequestMessage Peticion(string metodo, string sufijo)
    {
        HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + sufijo);
        if ((metodo == "POST" && sufijo == "") || metodo == "PUT")
        {
            peticion.Content = JsonContent.Create(Solicitud("ESCRITURA"));
        }

        return peticion;
    }

    private static GuardarCentroCostoRequestDto Solicitud(string codigo, int? padreId = null, int? responsableId = null)
    {
        return new GuardarCentroCostoRequestDto
        {
            Codigo = codigo,
            Nombre = "Centro de prueba",
            CentroCostoPadreId = padreId,
            ResponsableEmpleadoId = responsableId
        };
    }

    private async Task<CentroCostoDetalleDto> CrearAsync(string codigo, int? padreId = null)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, Solicitud(codigo, padreId));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await LeerAsync<CentroCostoDetalleDto>(respuesta);
    }

    private async Task<CentroCostoDetalleDto> ObtenerAsync(int id)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/{id}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await LeerAsync<CentroCostoDetalleDto>(respuesta);
    }

    private async Task<PaginaCentrosCostoDto> ListarAsync(string filtro = "")
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + filtro);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await LeerAsync<PaginaCentrosCostoDto>(respuesta);
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta) where T : class
    {
        return Assert.IsType<T>(await respuesta.Content.ReadFromJsonAsync<T>());
    }

    private static async Task AssertErrorAsync(HttpResponseMessage respuesta, HttpStatusCode codigo, string literal)
    {
        Assert.Equal(codigo, respuesta.StatusCode);
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal((int)codigo, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(literal, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}

public sealed class CentrosCostoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _baseDatos = "CentrosCostoTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            servicios.AddDbContext<TitanGlobalDbContext>(opciones => opciones.UseInMemoryDatabase(_baseDatos));
        });
    }
}
