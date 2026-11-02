using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo02_SegmentacionClientes;

[Collection("Modulo04")]
public class SegmentacionClientesApiTests
{
    private readonly HttpClient _clienteHttp;
    private readonly OrdenesCompraApiFactory _fabrica;

    public SegmentacionClientesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerSegmentos();
    }

    [Fact]
    public async Task ClasificacionAsignaElSegmentoMasPrioritarioYOrdenaPorMonto()
    {
        PaginaClientesSegmentoDto? resultado = await _clienteHttp.GetFromJsonAsync<PaginaClientesSegmentoDto>(
            "/api/v1/segmentos-clientes/clasificacion");

        Assert.NotNull(resultado);
        Assert.Equal(5, resultado.Total);
        Assert.Equal(new[] { 1, 2, 4, 3, 5 }, resultado.Items.Select(cliente => cliente.ClienteId).ToArray());
        Assert.Equal("VIP", resultado.Items[0].SegmentoCodigo);
        Assert.Equal("FRECUENTE", resultado.Items[1].SegmentoCodigo);
        Assert.Equal("OCASIONAL", resultado.Items[2].SegmentoCodigo);
        Assert.Equal("OCASIONAL", resultado.Items[3].SegmentoCodigo);
        Assert.Null(resultado.Items[4].SegmentoId);
        Assert.Equal(4, resultado.Items[0].TotalOrdenes);
        Assert.Equal(10000m, resultado.Items[0].MontoTotal);
    }

    [Fact]
    public async Task MontoIgualAlMinimoClasificaEnElSegmentoActivo()
    {
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            SegmentoCliente vip = await contexto.SegmentosClientes.SingleAsync(segmento => segmento.Id == 1);
            vip.MontoMinimo = 10000m;
            await contexto.SaveChangesAsync();
        }

        PaginaClientesSegmentoDto? resultado = await _clienteHttp.GetFromJsonAsync<PaginaClientesSegmentoDto>(
            "/api/v1/segmentos-clientes/clasificacion?segmentoId=1");

        Assert.NotNull(resultado);
        Assert.Equal(1, Assert.Single(resultado.Items).ClienteId);
    }

    [Fact]
    public async Task SegmentoInactivoNuncaSeAsignaAunqueCumplaLosCriterios()
    {
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            foreach (SegmentoCliente segmento in contexto.SegmentosClientes.Where(entidad => entidad.Activo))
            {
                segmento.MontoMinimo = 20000m;
                segmento.OrdenesMinimas = 100;
            }

            await contexto.SaveChangesAsync();
        }

        PaginaClientesSegmentoDto? resultado = await _clienteHttp.GetFromJsonAsync<PaginaClientesSegmentoDto>(
            "/api/v1/segmentos-clientes/clasificacion?busqueda=Acme");

        Assert.NotNull(resultado);
        Assert.Null(Assert.Single(resultado.Items).SegmentoId);
    }

    [Fact]
    public async Task ClasificacionFiltraSegmentoSinSegmentoBusquedaYPaginacion()
    {
        PaginaClientesSegmentoDto? porSegmento = await _clienteHttp.GetFromJsonAsync<PaginaClientesSegmentoDto>(
            "/api/v1/segmentos-clientes/clasificacion?segmentoId=2&pagina=1&tamanoPagina=1");
        PaginaClientesSegmentoDto? sinSegmento = await _clienteHttp.GetFromJsonAsync<PaginaClientesSegmentoDto>(
            "/api/v1/segmentos-clientes/clasificacion?sinSegmento=true");
        PaginaClientesSegmentoDto? busqueda = await _clienteHttp.GetFromJsonAsync<PaginaClientesSegmentoDto>(
            "/api/v1/segmentos-clientes/clasificacion?busqueda=log%C3%ADstica");

        Assert.NotNull(porSegmento);
        Assert.Equal(1, porSegmento.Total);
        Assert.Equal(2, Assert.Single(porSegmento.Items).ClienteId);
        Assert.NotNull(sinSegmento);
        Assert.Equal(new[] { 5 }, sinSegmento.Items.Select(cliente => cliente.ClienteId).ToArray());
        Assert.NotNull(busqueda);
        Assert.Equal(new[] { 4 }, busqueda.Items.Select(cliente => cliente.ClienteId).ToArray());
    }

    [Fact]
    public async Task FiltradoIncompatibleYPaginacionInvalidaDevuelvenErroresLiterales()
    {
        HttpResponseMessage incompatible = await _clienteHttp.GetAsync(
            "/api/v1/segmentos-clientes/clasificacion?segmentoId=1&sinSegmento=true");
        HttpResponseMessage pagina = await _clienteHttp.GetAsync("/api/v1/segmentos-clientes?pagina=0");

        await AssertarErrorAsync(incompatible, HttpStatusCode.BadRequest, 400,
            "No se puede filtrar por segmento y sin segmento a la vez.");
        await AssertarErrorAsync(pagina, HttpStatusCode.BadRequest, 400,
            "La página debe ser mayor o igual a 1.");
    }

    [Fact]
    public async Task ResumenYClientesDeSegmentoCoincidenConComprasSeed()
    {
        ResumenSegmentacionDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenSegmentacionDto>(
            "/api/v1/segmentos-clientes/resumen");
        PaginaClientesSegmentoDto? clientesVip = await _clienteHttp.GetFromJsonAsync<PaginaClientesSegmentoDto>(
            "/api/v1/segmentos-clientes/1/clientes");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.TotalClientes);
        Assert.Equal(4, resumen.ClientesSegmentados);
        Assert.Equal(1, resumen.ClientesSinSegmento);
        Assert.Equal(new[] { "VIP", "FRECUENTE", "OCASIONAL" },
            resumen.PorSegmento.Select(segmento => segmento.Codigo).ToArray());
        Assert.Equal(new[] { 1, 1, 2 }, resumen.PorSegmento.Select(segmento => segmento.Clientes).ToArray());
        Assert.Equal(new[] { 10000m, 2000m, 1250m }, resumen.PorSegmento.Select(segmento => segmento.MontoTotal).ToArray());
        Assert.NotNull(clientesVip);
        Assert.Equal(new[] { 1 }, clientesVip.Items.Select(cliente => cliente.ClienteId).ToArray());
    }

    [Fact]
    public async Task ListadoDetalleYEliminacionFuncionan()
    {
        PaginaSegmentosDto? listado = await _clienteHttp.GetFromJsonAsync<PaginaSegmentosDto>(
            "/api/v1/segmentos-clientes?activo=true&busqueda=vip");
        SegmentoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SegmentoDetalleDto>(
            "/api/v1/segmentos-clientes/1");
        HttpResponseMessage eliminacion = await _clienteHttp.DeleteAsync("/api/v1/segmentos-clientes/4");

        Assert.NotNull(listado);
        Assert.Equal(1, listado.Total);
        Assert.Equal("VIP", Assert.Single(listado.Items).Codigo);
        Assert.Equal(1, listado.Items[0].Clientes);
        Assert.NotNull(detalle);
        Assert.Equal("Criterio de alto valor", detalle.Descripcion);
        Assert.Equal(HttpStatusCode.NoContent, eliminacion.StatusCode);
        Assert.Equal(3, await ContarSegmentosAsync());
    }

    [Theory]
    [InlineData("", "Cliente", "El código es obligatorio.")]
    [InlineData("X", "Cliente", "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("NUEVO", " ", "El nombre del segmento es obligatorio.")]
    public async Task AltaInvalidaDevuelveElMensajeLiteral(string codigo, string nombre, string mensaje)
    {
        GuardarSegmentoRequestDto solicitud = CrearSolicitud(codigo, nombre, 10, 1, 0m);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/segmentos-clientes", solicitud);

        await AssertarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(4, await ContarSegmentosAsync());
    }

    [Fact]
    public async Task ValidaCadaReglaNumericaYLongitudConMensajeLiteral()
    {
        await AssertarAltaInvalidaAsync(CrearSolicitud("NEW-A", "Nombre", 0, 1, 0m),
            "La prioridad debe estar entre 1 y 999.");
        await AssertarAltaInvalidaAsync(CrearSolicitud("NEW-B", "Nombre", 10, 1, -1m),
            "El monto mínimo no puede ser negativo.");
        await AssertarAltaInvalidaAsync(CrearSolicitud("NEW-C", "Nombre", 10, -1, 0m),
            "Las órdenes mínimas no pueden ser negativas.");
        await AssertarAltaInvalidaAsync(CrearSolicitud("NEW-D", "Nombre", 10, 0, 0m),
            "Define al menos un criterio: monto mínimo u órdenes mínimas.");
        GuardarSegmentoRequestDto descripcionLarga = CrearSolicitud("NEW-E", "Nombre", 10, 1, 1m);
        descripcionLarga.Descripcion = new string('D', 301);
        await AssertarAltaInvalidaAsync(descripcionLarga, "La descripción no puede superar 300 caracteres.");
        GuardarSegmentoRequestDto nombreLargo = CrearSolicitud("NEW-F", new string('N', 101), 10, 1, 1m);
        await AssertarAltaInvalidaAsync(nombreLargo, "El nombre del segmento no puede superar 100 caracteres.");
    }

    [Fact]
    public async Task NormalizaCodigoNombreDescripcionYPermiteConservarCodigoYPrioridad()
    {
        GuardarSegmentoRequestDto solicitud = CrearSolicitud("  nuevo-01 ", "  Nuevo segmento ", 20, 0, 100m);
        solicitud.Descripcion = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/segmentos-clientes", solicitud);
        Dictionary<string, int>? creado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        HttpResponseMessage actualizar = await _clienteHttp.PutAsJsonAsync(
            $"/api/v1/segmentos-clientes/{creado!["id"]}", solicitud);
        SegmentoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SegmentoDetalleDto>(
            $"/api/v1/segmentos-clientes/{creado["id"]}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, actualizar.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("NUEVO-01", detalle.Codigo);
        Assert.Equal("Nuevo segmento", detalle.Nombre);
        Assert.Null(detalle.Descripcion);
    }

    [Fact]
    public async Task CodigosYPrioridadesDuplicadosDan409YNoCambianFilas()
    {
        GuardarSegmentoRequestDto codigoDuplicado = CrearSolicitud("VIP", "Duplicado", 10, 1, 1m);
        HttpResponseMessage errorCodigo = await _clienteHttp.PostAsJsonAsync("/api/v1/segmentos-clientes", codigoDuplicado);
        await AssertarErrorAsync(errorCodigo, HttpStatusCode.Conflict, 409,
            "Ya existe un segmento con el código VIP.");
        GuardarSegmentoRequestDto prioridadDuplicada = CrearSolicitud("NUEVO-02", "Duplicado", 2, 1, 1m);
        HttpResponseMessage errorPrioridad = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/segmentos-clientes", prioridadDuplicada);
        await AssertarErrorAsync(errorPrioridad, HttpStatusCode.Conflict, 409,
            "Ya existe un segmento con la prioridad 2.");
        Assert.Equal(4, await ContarSegmentosAsync());
    }

    [Fact]
    public async Task CodigoSeGuardaEnMayusculasYPrioridadPropiaSePuedeMantener()
    {
        GuardarSegmentoRequestDto actualizacion = CrearSolicitud(" vip ", "VIP editado", 1, 3, 5000m);
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/segmentos-clientes/1", actualizacion);
        SegmentoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SegmentoDetalleDto>(
            "/api/v1/segmentos-clientes/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("VIP", detalle.Codigo);
        Assert.Equal(1, detalle.Prioridad);
        Assert.Equal("VIP editado", detalle.Nombre);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task IdentificadorInexistenteDevuelve404Literal(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/segmentos-clientes/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(
                "/api/v1/segmentos-clientes/9999", CrearSolicitud("NEW", "Nuevo", 10, 1, 0m));
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/segmentos-clientes/9999");
        }

        await AssertarErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El segmento 9999 no existe.");
    }

    private void RestablecerSegmentos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.SegmentosClientes.RemoveRange(contexto.SegmentosClientes);
        contexto.SaveChanges();
        contexto.SegmentosClientes.AddRange(
            new SegmentoCliente
            {
                Id = 1,
                Codigo = "VIP",
                Nombre = "Cliente VIP",
                Descripcion = "Criterio de alto valor",
                Prioridad = 1,
                MontoMinimo = 5000m,
                OrdenesMinimas = 3,
                Activo = true
            },
            new SegmentoCliente
            {
                Id = 2,
                Codigo = "FRECUENTE",
                Nombre = "Cliente frecuente",
                Prioridad = 2,
                MontoMinimo = 0m,
                OrdenesMinimas = 2,
                Activo = true
            },
            new SegmentoCliente
            {
                Id = 3,
                Codigo = "OCASIONAL",
                Nombre = "Cliente ocasional",
                Prioridad = 3,
                MontoMinimo = 0m,
                OrdenesMinimas = 1,
                Activo = true
            },
            new SegmentoCliente
            {
                Id = 4,
                Codigo = "PREMIUM-OLD",
                Nombre = "Premium antiguo",
                Prioridad = 4,
                MontoMinimo = 1000m,
                OrdenesMinimas = 1,
                Activo = false
            });
        contexto.SaveChanges();
    }

    private async Task<int> ContarSegmentosAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.SegmentosClientes.CountAsync();
    }

    private async Task AssertarAltaInvalidaAsync(GuardarSegmentoRequestDto solicitud, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/segmentos-clientes", solicitud);
        await AssertarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(4, await ContarSegmentosAsync());
    }

    private static GuardarSegmentoRequestDto CrearSolicitud(
        string codigo,
        string nombre,
        int prioridad,
        int ordenesMinimas,
        decimal montoMinimo)
    {
        return new GuardarSegmentoRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Prioridad = prioridad,
            OrdenesMinimas = ordenesMinimas,
            MontoMinimo = montoMinimo,
            Activo = true
        };
    }

    private static async Task AssertarErrorAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode estado,
        int codigo,
        string mensaje)
    {
        string contenido = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(contenido);
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal(codigo, json.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, json.RootElement.GetProperty("Message").GetString());
        Assert.False(json.RootElement.TryGetProperty("Detail", out _));
    }
}
