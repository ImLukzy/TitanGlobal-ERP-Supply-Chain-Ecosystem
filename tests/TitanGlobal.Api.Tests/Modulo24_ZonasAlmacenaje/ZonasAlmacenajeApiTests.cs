using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo24_ZonasAlmacenaje;

[Collection("Modulo04")]
public class ZonasAlmacenajeApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ZonasAlmacenajeApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task ListarFiltraPorAlmacenTipoTemperaturaEstadoYBusquedaOrdenada()
    {
        PaginaZonasDto? porAlmacen = await _clienteHttp.GetFromJsonAsync<PaginaZonasDto>(
            "/api/v1/zonas-almacenaje?almacenId=1&pagina=1&tamanoPagina=20");
        PaginaZonasDto? porTipo = await _clienteHttp.GetFromJsonAsync<PaginaZonasDto>(
            "/api/v1/zonas-almacenaje?tipo=picking");
        PaginaZonasDto? porTemperatura = await _clienteHttp.GetFromJsonAsync<PaginaZonasDto>(
            "/api/v1/zonas-almacenaje?temperatura=refrigerada");
        PaginaZonasDto? inactivas = await _clienteHttp.GetFromJsonAsync<PaginaZonasDto>(
            "/api/v1/zonas-almacenaje?activa=false");
        PaginaZonasDto? busquedaCodigo = await _clienteHttp.GetFromJsonAsync<PaginaZonasDto>(
            "/api/v1/zonas-almacenaje?busqueda=rec-01");
        PaginaZonasDto? busquedaNombre = await _clienteHttp.GetFromJsonAsync<PaginaZonasDto>(
            "/api/v1/zonas-almacenaje?busqueda=pasillo%20fr%C3%ADo");
        PaginaZonasDto? segundaPagina = await _clienteHttp.GetFromJsonAsync<PaginaZonasDto>(
            "/api/v1/zonas-almacenaje?pagina=2&tamanoPagina=3");

        Assert.NotNull(porAlmacen);
        Assert.Equal(4, porAlmacen.Total);
        Assert.Equal(new[] { "ALM-A", "DES-01", "PIC-01", "REC-01" }, porAlmacen.Items.Select(zona => zona.Codigo).ToArray());
        Assert.All(porAlmacen.Items, zona => Assert.Equal(1, zona.AlmacenId));
        Assert.NotNull(porTipo);
        Assert.Equal("PIC-01", Assert.Single(porTipo.Items).Codigo);
        Assert.NotNull(porTemperatura);
        Assert.Equal("FRIO-01", Assert.Single(porTemperatura.Items).Codigo);
        Assert.NotNull(inactivas);
        Assert.Equal("CUA-01", Assert.Single(inactivas.Items).Codigo);
        Assert.NotNull(busquedaCodigo);
        Assert.Equal("REC-01", Assert.Single(busquedaCodigo.Items).Codigo);
        Assert.NotNull(busquedaNombre);
        Assert.Equal("FRIO-01", Assert.Single(busquedaNombre.Items).Codigo);
        Assert.NotNull(segundaPagina);
        Assert.Equal(8, segundaPagina.Total);
        Assert.Equal(3, segundaPagina.Items.Count);
        Assert.Equal(2, segundaPagina.Pagina);
        Assert.Equal(new[] { "TRAN-01", "ALM-A", "DES-01" }, segundaPagina.Items.Select(zona => zona.Codigo).ToArray());
    }

    [Theory]
    [InlineData("/api/v1/zonas-almacenaje?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("/api/v1/zonas-almacenaje?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("/api/v1/zonas-almacenaje?tipo=Externo", "El tipo de zona no es válido.")]
    [InlineData("/api/v1/zonas-almacenaje?temperatura=Caliente", "La temperatura no es válida.")]
    public async Task FiltrosInvalidosDevuelvenBadRequestLiteral(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ObtenerDetalleDevuelveZonaYAlmacen()
    {
        ZonaDetalleDto? zona = await _clienteHttp.GetFromJsonAsync<ZonaDetalleDto>("/api/v1/zonas-almacenaje/1");

        Assert.NotNull(zona);
        Assert.Equal(1, zona.AlmacenId);
        Assert.Equal("ALM-LIM-01", zona.AlmacenCodigo);
        Assert.Equal("Central Lima", zona.AlmacenNombre);
        Assert.Equal("REC-01", zona.Codigo);
        Assert.Equal("Recepción principal", zona.Nombre);
        Assert.Equal("Recepcion", zona.Tipo);
        Assert.Equal("Ambiente", zona.Temperatura);
        Assert.Equal(50m, zona.CapacidadM3);
        Assert.True(zona.Activa);
    }

    [Fact]
    public async Task ResumenDevuelveTiposYOcupacionExactos()
    {
        ResumenZonasDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenZonasDto>(
            "/api/v1/zonas-almacenaje/resumen");
        ResumenZonasDto? resumenAlmacen = await _clienteHttp.GetFromJsonAsync<ResumenZonasDto>(
            "/api/v1/zonas-almacenaje/resumen?almacenId=1");

        Assert.NotNull(resumen);
        Assert.Equal(8, resumen.TotalZonas);
        Assert.Equal(7, resumen.Activas);
        Assert.Equal(
            new[] { "Recepcion", "Almacenamiento", "Picking", "Despacho", "Cuarentena" },
            resumen.PorTipo.Select(tipo => tipo.Tipo).ToArray());
        Assert.Equal(new[] { 2, 3, 1, 1, 1 }, resumen.PorTipo.Select(tipo => tipo.TotalZonas).ToArray());
        Assert.Equal(new[] { 110m, 430m, 60m, 40m, 20m }, resumen.PorTipo.Select(tipo => tipo.CapacidadM3).ToArray());
        Assert.Equal(
            new[] { "ALM-ARE-01", "ALM-CAL-TR", "ALM-LIM-01", "ALM-TRU-01" },
            resumen.Ocupacion.Select(item => item.AlmacenCodigo).ToArray());
        Assert.Equal(new[] { 130m, 60m, 450m, 0m }, resumen.Ocupacion.Select(item => item.CapacidadAsignadaM3).ToArray());
        Assert.Equal(new[] { 86.67m, 80m, 90m, 0m }, resumen.Ocupacion.Select(item => item.PorcentajeAsignado).ToArray());
        Assert.NotNull(resumenAlmacen);
        Assert.Equal(4, resumenAlmacen.TotalZonas);
        Assert.Equal(4, resumenAlmacen.Activas);
        OcupacionAlmacenDto ocupacion = Assert.Single(resumenAlmacen.Ocupacion);
        Assert.Equal("ALM-LIM-01", ocupacion.AlmacenCodigo);
        Assert.Equal(500m, ocupacion.CapacidadAlmacenM3);
        Assert.Equal(450m, ocupacion.CapacidadAsignadaM3);
        Assert.Equal(90m, ocupacion.PorcentajeAsignado);
    }

    [Fact]
    public async Task ResumenDeAlmacenInexistenteDevuelveBadRequest()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/api/v1/zonas-almacenaje/resumen?almacenId=9999");

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El almacén 9999 no existe.");
    }

    [Theory]
    [InlineData(9999, "REC-02", "Zona nueva", "Recepcion", "Ambiente", 10, true, 400, "El almacén 9999 no existe.")]
    [InlineData(4, "REC-02", "Zona nueva", "Recepcion", "Ambiente", 10, true, 409, "El almacén ALM-TRU-01 está inactivo.")]
    [InlineData(1, "", "Zona nueva", "Recepcion", "Ambiente", 10, true, 400, "El código de zona es obligatorio.")]
    [InlineData(1, "codigo con espacios", "Zona nueva", "Recepcion", "Ambiente", 10, true, 400, "El código de zona solo admite letras, números y guiones (1 a 20 caracteres).")]
    [InlineData(1, "REC-02", "", "Recepcion", "Ambiente", 10, true, 400, "El nombre de la zona es obligatorio.")]
    [InlineData(1, "REC-02", "Zona nueva", "Otro", "Ambiente", 10, true, 400, "El tipo de zona no es válido.")]
    [InlineData(1, "REC-02", "Zona nueva", "Recepcion", "Caliente", 10, true, 400, "La temperatura no es válida.")]
    [InlineData(1, "REC-02", "Zona nueva", "Recepcion", "Ambiente", 0, true, 400, "La capacidad debe ser mayor que 0.")]
    public async Task CrearValidaAlmacenYCamposConErroresLiterales(
        int almacenId,
        string codigo,
        string nombre,
        string tipo,
        string temperatura,
        decimal capacidadM3,
        bool activa,
        int estado,
        string mensaje)
    {
        GuardarZonaRequestDto solicitud = CrearSolicitud(almacenId, codigo, nombre, tipo, temperatura, capacidadM3, activa);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/zonas-almacenaje", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, (HttpStatusCode)estado, estado, mensaje);
        Assert.Equal(8, await ContarZonasAsync());
    }

    [Theory]
    [InlineData("", "El código de zona es obligatorio.")]
    [InlineData("ZONA INVALIDA", "El código de zona solo admite letras, números y guiones (1 a 20 caracteres).")]
    [InlineData("REC-01", "Ya existe la zona REC-01 en el almacén ALM-LIM-01.")]
    public async Task CodigoInvalidoORepetidoDevuelveErrorLiteralSinCrearFilas(string codigo, string mensaje)
    {
        GuardarZonaRequestDto solicitud = CrearSolicitud(1, codigo, "Zona nueva", "Recepcion", "Ambiente", 10m, true);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/zonas-almacenaje", solicitud);

        HttpStatusCode estado = mensaje.StartsWith("Ya existe", StringComparison.Ordinal)
            ? HttpStatusCode.Conflict
            : HttpStatusCode.BadRequest;
        int codigoEstado = estado == HttpStatusCode.Conflict ? 409 : 400;
        await AfirmarErrorNegocioAsync(respuesta, estado, codigoEstado, mensaje);
        Assert.Equal(8, await ContarZonasAsync());
    }

    [Fact]
    public async Task NombreLargoDevuelveErrorLiteralSinCrearFilas()
    {
        GuardarZonaRequestDto solicitud = CrearSolicitud(
            1,
            "NOMBRE-LARGO",
            new string('N', 101),
            "Recepcion",
            "Ambiente",
            1m,
            true);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/zonas-almacenaje", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El nombre de la zona no puede superar 100 caracteres.");
        Assert.Equal(8, await ContarZonasAsync());
    }

    [Fact]
    public async Task MismoCodigoSePermiteEnOtroAlmacenYSeGuardaEnMayusculas()
    {
        GuardarZonaRequestDto solicitud = CrearSolicitud(2, "  rec-01  ", "  Zona duplicada válida  ", "almacenamiento", "refrigerada", 1m, false);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/zonas-almacenaje", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        ZonaDetalleDto? zona = resultado is null
            ? null
            : await _clienteHttp.GetFromJsonAsync<ZonaDetalleDto>($"/api/v1/zonas-almacenaje/{resultado["id"]}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(zona);
        Assert.Equal("REC-01", zona.Codigo);
        Assert.Equal("Zona duplicada válida", zona.Nombre);
        Assert.Equal("Almacenamiento", zona.Tipo);
        Assert.Equal("Refrigerada", zona.Temperatura);
        Assert.False(zona.Activa);
    }

    [Fact]
    public async Task CapacidadAlcanzaLimitePeroNoLoSuperaYZonaInactivaNoCuenta()
    {
        GuardarZonaRequestDto limite = CrearSolicitud(1, "LIMITE", "Zona al límite", "Picking", "Ambiente", 50m, true);
        HttpResponseMessage respuestaLimite = await _clienteHttp.PostAsJsonAsync("/api/v1/zonas-almacenaje", limite);
        Assert.Equal(HttpStatusCode.OK, respuestaLimite.StatusCode);

        GuardarZonaRequestDto supera = CrearSolicitud(1, "SUPERA", "Zona excedida", "Picking", "Ambiente", 1m, true);
        HttpResponseMessage respuestaSupera = await _clienteHttp.PostAsJsonAsync("/api/v1/zonas-almacenaje", supera);
        await AfirmarErrorNegocioAsync(
            respuestaSupera,
            HttpStatusCode.Conflict,
            409,
            "La capacidad de las zonas activas (501.00 m³) superaría la del almacén ALM-LIM-01 (500.00 m³).");
        Assert.Equal(9, await ContarZonasAsync());

        GuardarZonaRequestDto inactiva = CrearSolicitud(1, "INACTIVA", "Zona fuera de uso", "Cuarentena", "Congelada", 1000m, false);
        HttpResponseMessage respuestaInactiva = await _clienteHttp.PostAsJsonAsync("/api/v1/zonas-almacenaje", inactiva);
        Assert.Equal(HttpStatusCode.OK, respuestaInactiva.StatusCode);
        ResumenZonasDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenZonasDto>(
            "/api/v1/zonas-almacenaje/resumen?almacenId=1");
        Assert.NotNull(resumen);
        Assert.Equal(500m, Assert.Single(resumen.Ocupacion).CapacidadAsignadaM3);
    }

    [Fact]
    public async Task ActualizarQueSuperaCapacidadNoCambiaLaZonaExistente()
    {
        GuardarZonaRequestDto solicitud = CrearSolicitud(1, "REC-01", "No debe cambiar", "Recepcion", "Ambiente", 101m, true);
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/zonas-almacenaje/1", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "La capacidad de las zonas activas (501.00 m³) superaría la del almacén ALM-LIM-01 (500.00 m³).");
        ZonaDetalleDto? zona = await _clienteHttp.GetFromJsonAsync<ZonaDetalleDto>("/api/v1/zonas-almacenaje/1");
        Assert.NotNull(zona);
        Assert.Equal("Recepción principal", zona.Nombre);
        Assert.Equal(50m, zona.CapacidadM3);
    }

    [Fact]
    public async Task ActualizarPuedeMantenerCodigoPeroNoMoverAAlmacenInactivo()
    {
        GuardarZonaRequestDto actualizacion = CrearSolicitud(1, "REC-01", "Recepción modificada", "Recepcion", "Ambiente", 50m, true);
        HttpResponseMessage respuestaActualizacion = await _clienteHttp.PutAsJsonAsync(
            "/api/v1/zonas-almacenaje/1",
            actualizacion);
        Assert.Equal(HttpStatusCode.OK, respuestaActualizacion.StatusCode);

        GuardarZonaRequestDto mover = CrearSolicitud(4, "REC-01", "Recepción modificada", "Recepcion", "Ambiente", 10m, true);
        HttpResponseMessage respuestaMover = await _clienteHttp.PutAsJsonAsync("/api/v1/zonas-almacenaje/1", mover);
        await AfirmarErrorNegocioAsync(
            respuestaMover,
            HttpStatusCode.Conflict,
            409,
            "El almacén ALM-TRU-01 está inactivo.");
        ZonaDetalleDto? zona = await _clienteHttp.GetFromJsonAsync<ZonaDetalleDto>("/api/v1/zonas-almacenaje/1");
        Assert.NotNull(zona);
        Assert.Equal(1, zona.AlmacenId);
        Assert.Equal("Recepción modificada", zona.Nombre);
    }

    [Fact]
    public async Task ActualizarConCodigoDeOtraZonaNoCambiaLaZona()
    {
        GuardarZonaRequestDto solicitud = CrearSolicitud(
            1,
            "ALM-A",
            "No debe cambiar",
            "Almacenamiento",
            "Ambiente",
            1m,
            true);
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/zonas-almacenaje/1", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe la zona ALM-A en el almacén ALM-LIM-01.");
        ZonaDetalleDto? zona = await _clienteHttp.GetFromJsonAsync<ZonaDetalleDto>("/api/v1/zonas-almacenaje/1");
        Assert.NotNull(zona);
        Assert.Equal("REC-01", zona.Codigo);
        Assert.Equal("Recepción principal", zona.Nombre);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task ZonaInexistenteDevuelveNotFoundLiteral(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/zonas-almacenaje/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(
                "/api/v1/zonas-almacenaje/9999",
                CrearSolicitud(1, "ZONA-99", "Zona ausente", "Recepcion", "Ambiente", 1m, true));
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/zonas-almacenaje/9999");
        }

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "La zona 9999 no existe.");
    }

    [Fact]
    public async Task EliminarZonaDevuelveNoContent()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/zonas-almacenaje/8");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(7, await ContarZonasAsync());
    }

    [Fact]
    public async Task OpenApiIncluyeSeisOperacionesDeZonas()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/zonas-almacenaje").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/zonas-almacenaje").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/zonas-almacenaje/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/zonas-almacenaje/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/zonas-almacenaje/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/zonas-almacenaje/resumen").TryGetProperty("get", out _));
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ZonasAlmacenaje.RemoveRange(contexto.ZonasAlmacenaje);
        contexto.Almacenes.RemoveRange(contexto.Almacenes);
        contexto.Almacenes.AddRange(
            new Almacen
            {
                Id = 1,
                Codigo = "ALM-LIM-01",
                Nombre = "Central Lima",
                Tipo = "Central",
                Direccion = "Av. Industrial 100",
                Ciudad = "Lima",
                CapacidadM3 = 500m,
                Responsable = "Ana Torres",
                Activo = true
            },
            new Almacen
            {
                Id = 2,
                Codigo = "ALM-ARE-01",
                Nombre = "Regional Arequipa",
                Tipo = "Regional",
                Direccion = "Av. Sur 300",
                Ciudad = "Arequipa",
                CapacidadM3 = 150m,
                Responsable = "María León",
                Activo = true
            },
            new Almacen
            {
                Id = 3,
                Codigo = "ALM-CAL-TR",
                Nombre = "Tránsito Callao",
                Tipo = "Transito",
                Direccion = "Av. Puerto 500",
                Ciudad = "Callao",
                CapacidadM3 = 75m,
                Activo = true
            },
            new Almacen
            {
                Id = 4,
                Codigo = "ALM-TRU-01",
                Nombre = "Regional Trujillo",
                Tipo = "Regional",
                Direccion = "Av. Norte 400",
                Ciudad = "Trujillo",
                CapacidadM3 = 100m,
                Activo = false
            });
        contexto.SaveChanges();
        contexto.ZonasAlmacenaje.AddRange(
            CrearZona(1, 1, "REC-01", "Recepción principal", "Recepcion", "Ambiente", 50m, true),
            CrearZona(2, 1, "ALM-A", "Almacenamiento A", "Almacenamiento", "Ambiente", 300m, true),
            CrearZona(3, 1, "PIC-01", "Picking 01", "Picking", "Ambiente", 60m, true),
            CrearZona(4, 1, "DES-01", "Despacho 01", "Despacho", "Ambiente", 40m, true),
            CrearZona(5, 2, "ALM-A", "Almacenamiento A", "Almacenamiento", "Ambiente", 100m, true),
            CrearZona(6, 2, "FRIO-01", "Pasillo frío", "Almacenamiento", "Refrigerada", 30m, true),
            CrearZona(7, 3, "TRAN-01", "Tránsito 01", "Recepcion", "Ambiente", 60m, true),
            CrearZona(8, 3, "CUA-01", "Cuarentena 01", "Cuarentena", "Ambiente", 20m, false));
        contexto.SaveChanges();
    }

    private static ZonaAlmacenaje CrearZona(
        int id,
        int almacenId,
        string codigo,
        string nombre,
        string tipo,
        string temperatura,
        decimal capacidadM3,
        bool activa)
    {
        return new ZonaAlmacenaje
        {
            Id = id,
            AlmacenId = almacenId,
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Temperatura = temperatura,
            CapacidadM3 = capacidadM3,
            Activa = activa
        };
    }

    private static GuardarZonaRequestDto CrearSolicitud(
        int almacenId,
        string codigo,
        string nombre,
        string tipo,
        string temperatura,
        decimal capacidadM3,
        bool activa)
    {
        return new GuardarZonaRequestDto
        {
            AlmacenId = almacenId,
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Temperatura = temperatura,
            CapacidadM3 = capacidadM3,
            Activa = activa
        };
    }

    private async Task<int> ContarZonasAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.ZonasAlmacenaje.CountAsync();
    }

    private static async Task AfirmarErrorNegocioAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode codigoHttp,
        int estado,
        string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal(codigoHttp, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
