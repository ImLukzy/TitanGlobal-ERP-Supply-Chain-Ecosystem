using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo06_ListasPrecios;

public class ListasPreciosApiTests : IDisposable
{
    private readonly HttpClient _cliente;
    private readonly ListasPreciosApiFactory _fabrica;

    public ListasPreciosApiTests()
    {
        _fabrica = new ListasPreciosApiFactory();
        _cliente = _fabrica.CreateClient();
        _fabrica.SeedDatabase();
        LimpiarListas();
        PrepararSegmentos();
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _fabrica.Dispose();
    }

    [Theory]
    [InlineData("", "Lista", "El código es obligatorio.")]
    [InlineData("A!", "Lista", "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("PRECIOS", " ", "El nombre de la lista es obligatorio.")]
    [InlineData("PRECIOS", "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX", "El nombre de la lista no puede superar 100 caracteres.")]
    [InlineData("PRECIOS", "Lista", "El segmento 999 no existe.", 999)]
    [InlineData("PRECIOS", "Lista", "La prioridad debe estar entre 1 y 999.", null, 0)]
    [InlineData("PRECIOS", "Lista", "La fecha de fin no puede ser anterior a la de inicio.", null, 5, true)]
    [InlineData("PRECIOS", "Lista", "La lista debe tener al menos un producto.", null, 5, false, "vacio")]
    [InlineData("PRECIOS", "Lista", "El precio debe ser mayor que 0.", null, 5, false, "precio")]
    [InlineData("PRECIOS", "Lista", "El producto 1 está repetido en la lista.", null, 5, false, "duplicado")]
    [InlineData("PRECIOS", "Lista", "El producto 999 no existe.", null, 5, false, "inexistente")]
    public async Task AltaInvalidaDevuelveMensajeLiteralYNoCambiaFilas(
        string codigo,
        string nombre,
        string mensaje,
        int? segmentoId = null,
        int prioridad = 5,
        bool fechaInvalida = false,
        string items = "normal")
    {
        int antes = await ContarListasAsync();
        GuardarListaRequestDto solicitud = SolicitudValida(codigo, nombre, prioridad);
        solicitud.SegmentoClienteId = segmentoId;
        if (fechaInvalida)
        {
            solicitud.FechaFin = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        }

        ConfigurarItems(solicitud, items);
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/listas-precios", solicitud);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(antes, await ContarListasAsync());
    }

    [Theory]
    [InlineData("codigo", "Ya existe una lista de precios con el código EXISTENTE.")]
    [InlineData("prioridad", "Ya existe una lista de precios con la prioridad 5.")]
    public async Task ConflictosDevuelvenMensajeLiteralYNoCreanFilas(string campo, string mensaje)
    {
        await CrearListaAsync("EXISTENTE", 5, null, 100m);
        int antes = await ContarListasAsync();
        GuardarListaRequestDto solicitud = SolicitudValida("NUEVA", "Nueva", 7);
        if (campo == "codigo")
        {
            solicitud.Codigo = "EXISTENTE";
        }
        else
        {
            solicitud.Prioridad = 5;
        }

        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/listas-precios", solicitud);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.Conflict, 409, mensaje);
        Assert.Equal(antes, await ContarListasAsync());
    }

    [Fact]
    public async Task CodigoSeGuardaEnMayusculasYPutPermiteConservarCodigoYPrioridad()
    {
        GuardarListaRequestDto solicitud = SolicitudValida("  vip-lista  ", "Lista VIP", 5);
        HttpResponseMessage alta = await _cliente.PostAsJsonAsync("/api/v1/listas-precios", solicitud);
        ListaCreadaDto? creada = await alta.Content.ReadFromJsonAsync<ListaCreadaDto>();
        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
        Assert.NotNull(creada);
        solicitud.Codigo = "vip-lista";
        HttpResponseMessage actualizacion = await _cliente.PutAsJsonAsync($"/api/v1/listas-precios/{creada.Id}", solicitud);
        Assert.Equal(HttpStatusCode.OK, actualizacion.StatusCode);
        ListaDetalleDto? detalle = await _cliente.GetFromJsonAsync<ListaDetalleDto>($"/api/v1/listas-precios/{creada.Id}");
        Assert.NotNull(detalle);
        Assert.Equal("VIP-LISTA", detalle.Codigo);
        Assert.Equal(5, detalle.Prioridad);
    }

    [Fact]
    public async Task ActualizacionInvalidaNoModificaLaListaNiSusItems()
    {
        int id = await CrearListaAsync("ORIGINAL", 5, null, 100m);
        GuardarListaRequestDto solicitud = SolicitudValida("CAMBIADO", "Nombre cambiado", 8);
        solicitud.Items[0].Precio = 999m;
        solicitud.Items[0].ProductoId = 999;
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"/api/v1/listas-precios/{id}", solicitud);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, "El producto 999 no existe.");
        ListaDetalleDto? detalle = await _cliente.GetFromJsonAsync<ListaDetalleDto>($"/api/v1/listas-precios/{id}");
        Assert.NotNull(detalle);
        Assert.Equal("ORIGINAL", detalle.Codigo);
        Assert.Equal("Lista ORIGINAL", detalle.Nombre);
        Assert.Equal(100m, Assert.Single(detalle.Items).Precio);
    }

    [Fact]
    public async Task IdentificadorInexistenteDevuelve404LiteralSinDetalle()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/listas-precios/999");
        await AfirmarErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "La lista de precios 999 no existe.");
    }

    [Theory]
    [InlineData(999, 1, "El producto 999 no existe.")]
    [InlineData(1, 999, "El cliente 999 no existe.")]
    public async Task ResolucionDeIdentificadoresInexistentesDevuelveErrorLiteralSinDetalle(
        int productoId,
        int clienteId,
        string mensaje)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync(
            $"/api/v1/listas-precios/precio?productoId={productoId}&clienteId={clienteId}&fecha=2026-06-01");
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ResolucionPrefiereSegmentoAunqueListaGeneralTengaPrioridadMenor()
    {
        await CrearListaAsync("GENERAL", 1, null, 1800m);
        await CrearListaAsync("VIP", 20, 1, 1700m);
        PrecioResueltoDto? precio = await _cliente.GetFromJsonAsync<PrecioResueltoDto>(
            "/api/v1/listas-precios/precio?productoId=1&clienteId=1&fecha=2026-06-01");
        Assert.NotNull(precio);
        Assert.Equal(1700m, precio.Precio);
        Assert.Equal("Segmento", precio.Origen);
        ListaDetalleDto? detalle = await _cliente.GetFromJsonAsync<ListaDetalleDto>("/api/v1/listas-precios/2");
        Assert.NotNull(detalle);
        Assert.Equal(-32m, Assert.Single(detalle.Items).DiferenciaPorcentaje);
    }

    [Fact]
    public async Task ResolucionEligeMenorPrioridadGeneralYRespetaVigenciaIncluyente()
    {
        await CrearListaAsync("GENERAL-2", 2, null, 2200m);
        await CrearListaAsync("GENERAL-1", 1, null, 2100m);
        PrecioResueltoDto? inicio = await _cliente.GetFromJsonAsync<PrecioResueltoDto>(
            "/api/v1/listas-precios/precio?productoId=1&clienteId=5&fecha=2026-01-01");
        PrecioResueltoDto? fin = await _cliente.GetFromJsonAsync<PrecioResueltoDto>(
            "/api/v1/listas-precios/precio?productoId=1&clienteId=5&fecha=2026-12-31");
        Assert.NotNull(inicio);
        Assert.NotNull(fin);
        Assert.Equal(2100m, inicio.Precio);
        Assert.Equal(2100m, fin.Precio);
        Assert.Equal("General", inicio.Origen);
    }

    [Fact]
    public async Task ResolucionIgnoraListaInactivaFueraDeFechaYSinProductoYUsaBase()
    {
        await CrearListaAsync("INACTIVA", 1, null, 100m, false);
        await CrearListaAsync("FUTURA", 2, null, 100m, true,
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        await CrearListaAsync("OTRO-PRODUCTO", 3, null, 100m, true, productoId: 2);
        PrecioResueltoDto? inicioNoVigente = await _cliente.GetFromJsonAsync<PrecioResueltoDto>(
            "/api/v1/listas-precios/precio?productoId=1&clienteId=5&fecha=2025-12-31");
        PrecioResueltoDto? sinLista = await _cliente.GetFromJsonAsync<PrecioResueltoDto>(
            "/api/v1/listas-precios/precio?productoId=1&clienteId=5&fecha=2026-06-01");
        Assert.NotNull(inicioNoVigente);
        Assert.NotNull(sinLista);
        Assert.Equal(2500m, inicioNoVigente.Precio);
        Assert.Equal(2500m, sinLista.Precio);
        Assert.Equal("Base", sinLista.Origen);
    }

    [Fact]
    public async Task ClienteConSegmentoNuloSoloRecibeListaGeneral()
    {
        await CrearListaAsync("VIP-CLIENTE", 1, 1, 900m);
        await CrearListaAsync("GENERAL-CLIENTE", 5, null, 2300m);
        PrecioResueltoDto? precio = await _cliente.GetFromJsonAsync<PrecioResueltoDto>(
            "/api/v1/listas-precios/precio?productoId=1&clienteId=5&fecha=2026-06-01");
        Assert.NotNull(precio);
        Assert.Equal(2300m, precio.Precio);
        Assert.Equal("General", precio.Origen);
    }

    [Fact]
    public async Task ListadoFiltraVigentesSegmentoBusquedaYPaginaConTotal()
    {
        await CrearListaAsync("GENERAL-A", 1, null, 100m);
        await CrearListaAsync("GENERAL-B", 2, 1, 200m);
        await CrearListaAsync("VENCIDA", 3, null, 300m, true,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        PaginaListasDto? vigentes = await _cliente.GetFromJsonAsync<PaginaListasDto>(
            "/api/v1/listas-precios?vigentes=true&pagina=1&tamanoPagina=1");
        PaginaListasDto? segmento = await _cliente.GetFromJsonAsync<PaginaListasDto>(
            "/api/v1/listas-precios?segmentoId=1");
        PaginaListasDto? busqueda = await _cliente.GetFromJsonAsync<PaginaListasDto>(
            "/api/v1/listas-precios?busqueda=general-b");
        Assert.NotNull(vigentes);
        Assert.Equal(2, vigentes.Total);
        Assert.Single(vigentes.Items);
        Assert.Equal(1, segmento!.Total);
        Assert.Equal("GENERAL-B", Assert.Single(segmento.Items).Codigo);
        Assert.Equal("GENERAL-B", Assert.Single(busqueda!.Items).Codigo);
    }

    [Fact]
    public async Task ListadoConSegmentoCeroDevuelveSoloGeneralesYTotalExacto()
    {
        await PrepararEscenarioFiltroSegmentosAsync();

        PaginaListasDto? resultado = await _cliente.GetFromJsonAsync<PaginaListasDto>(
            "/api/v1/listas-precios?segmentoId=0");

        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Total);
        Assert.Equal(new[] { "GENERAL-VENCIDA", "GENERAL-ACTUAL" },
            resultado.Items.Select(lista => lista.Codigo));
    }

    [Fact]
    public async Task ListadoConSegmentoCeroYVigentesDevuelveSoloGeneralesVigentesYTotalExacto()
    {
        await PrepararEscenarioFiltroSegmentosAsync();

        PaginaListasDto? resultado = await _cliente.GetFromJsonAsync<PaginaListasDto>(
            "/api/v1/listas-precios?segmentoId=0&vigentes=true");

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Total);
        Assert.Equal("GENERAL-ACTUAL", Assert.Single(resultado.Items).Codigo);
    }

    [Fact]
    public async Task ListadoConSegmentoMayorACeroDevuelveSoloEseSegmento()
    {
        await PrepararEscenarioFiltroSegmentosAsync();

        PaginaListasDto? resultado = await _cliente.GetFromJsonAsync<PaginaListasDto>(
            "/api/v1/listas-precios?segmentoId=1");

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Total);
        Assert.Equal("VIP-SEGMENTO", Assert.Single(resultado.Items).Codigo);
    }

    [Fact]
    public async Task ResumenCuentaListasYProductosDistintosVigentes()
    {
        await CrearListaAsync("GENERAL-UNO", 1, null, 100m);
        await CrearListaAsync("VIP-UNO", 2, 1, 200m);
        ResumenListasDto? resumen = await _cliente.GetFromJsonAsync<ResumenListasDto>("/api/v1/listas-precios/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(2, resumen.Total);
        Assert.Equal(2, resumen.Activas);
        Assert.Equal(2, resumen.Vigentes);
        Assert.Equal(1, resumen.DeSegmento);
        Assert.Equal(1, resumen.Generales);
        Assert.Equal(1, resumen.ProductosConPrecioEspecial);
    }

    private async Task PrepararEscenarioFiltroSegmentosAsync()
    {
        DateTime hoy = DateTime.UtcNow.Date;
        await CrearListaAsync("GENERAL-ACTUAL", 10, null, 2400m, true, hoy.AddDays(-1), hoy.AddDays(1));
        await CrearListaAsync("GENERAL-VENCIDA", 5, null, 2000m, true,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        await CrearListaAsync("VIP-SEGMENTO", 1, 1, 2300m);
    }

    [Fact]
    public async Task OpenApiExponeLasSieteOperacionesDelModulo()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        string documento = await respuesta.Content.ReadAsStringAsync();
        string[] rutas =
        {
            "\"/api/v1/listas-precios\"",
            "\"/api/v1/listas-precios/{id}\"",
            "\"/api/v1/listas-precios/precio\"",
            "\"/api/v1/listas-precios/resumen\""
        };
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        foreach (string ruta in rutas)
        {
            Assert.Contains(ruta, documento);
        }
    }

    private void LimpiarListas()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ItemsListaPrecios.RemoveRange(contexto.ItemsListaPrecios);
        contexto.ListasPrecios.RemoveRange(contexto.ListasPrecios);
        contexto.SaveChanges();
    }

    private void PrepararSegmentos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        if (!contexto.SegmentosClientes.Any())
        {
            contexto.SegmentosClientes.Add(new SegmentoCliente
            {
                Id = 1,
                Codigo = "VIP",
                Nombre = "Cliente VIP",
                Prioridad = 1,
                MontoMinimo = 5000m,
                OrdenesMinimas = 3,
                Activo = true
            });
            contexto.SaveChanges();
        }
    }

    private async Task<int> CrearListaAsync(
        string codigo,
        int prioridad,
        int? segmentoId,
        decimal precio,
        bool activa = true,
        DateTime? inicio = null,
        DateTime? fin = null,
        int productoId = 1)
    {
        GuardarListaRequestDto solicitud = SolicitudValida(codigo, "Lista " + codigo, prioridad);
        solicitud.SegmentoClienteId = segmentoId;
        solicitud.Activa = activa;
        solicitud.FechaInicio = inicio ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        solicitud.FechaFin = fin ?? new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        solicitud.Items[0].ProductoId = productoId;
        solicitud.Items[0].Precio = precio;
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/listas-precios", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ListaCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<ListaCreadaDto>();
        Assert.NotNull(creada);
        return creada.Id;
    }

    private static GuardarListaRequestDto SolicitudValida(string codigo, string nombre, int prioridad)
    {
        return new GuardarListaRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Prioridad = prioridad,
            FechaInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaFin = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Activa = true,
            Items = new List<GuardarItemListaDto> { new GuardarItemListaDto { ProductoId = 1, Precio = 100m } }
        };
    }

    private static void ConfigurarItems(GuardarListaRequestDto solicitud, string tipo)
    {
        if (tipo == "vacio")
        {
            solicitud.Items.Clear();
        }
        else if (tipo == "precio")
        {
            solicitud.Items[0].Precio = 0m;
        }
        else if (tipo == "duplicado")
        {
            solicitud.Items.Add(new GuardarItemListaDto { ProductoId = 1, Precio = 150m });
        }
        else if (tipo == "inexistente")
        {
            solicitud.Items[0].ProductoId = 999;
        }
    }

    private async Task<int> ContarListasAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.ListasPrecios.CountAsync();
    }

    private static async Task AfirmarErrorAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode estado,
        int codigo,
        string mensaje)
    {
        string json = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument documento = JsonDocument.Parse(json);
        JsonElement raiz = documento.RootElement;
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal(codigo, raiz.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, raiz.GetProperty("Message").GetString());
        Assert.False(raiz.TryGetProperty("Detail", out _));
    }
}

public sealed class ListasPreciosApiFactory : WebApplicationFactory<Program>
{
    private bool _baseDatosSembrada;
    private readonly string _nombreBaseDatos = "Modulo06ListasPreciosTests-" + Guid.NewGuid();

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        TokenPruebas.AutenticarComoAdministrador(client, Services);
    }

    protected override void ConfigureWebHost(IWebHostBuilder constructorAnfitrion)
    {
        constructorAnfitrion.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            servicios.AddDbContext<TitanGlobalDbContext>(opciones => opciones
                .UseInMemoryDatabase(_nombreBaseDatos)
                .ConfigureWarnings(avisos => avisos.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }

    public void SeedDatabase()
    {
        if (_baseDatosSembrada)
        {
            return;
        }

        using IServiceScope alcance = Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Cliente clienteAcme = new Cliente { Id = 1, Nombre = "Acme Industrial", Ruc = "20111111111" };
        Cliente clienteBeta = new Cliente { Id = 2, Nombre = "Beta Comercio", Ruc = "20222222222" };
        Cliente clienteSinOrdenes = new Cliente { Id = 5, Nombre = "Cliente sin órdenes", Ruc = "20555555555" };
        Producto laptop = new Producto { Id = 1, Nombre = "Laptop Pro", Categoria = "Computo", Precio = 2500m };
        Producto laptopBasica = new Producto { Id = 2, Nombre = "Laptop Básica", Categoria = "Computo", Precio = 1500m };
        Producto teclado = new Producto { Id = 4, Nombre = "Teclado Mecánico", Categoria = "Perifericos", Precio = 200m };
        contexto.Clientes.AddRange(clienteAcme, clienteBeta, clienteSinOrdenes);
        contexto.Productos.AddRange(laptop, laptopBasica, teclado);
        contexto.OrdenesCompra.AddRange(
            new OrdenCompra { Id = 1, NumeroOrden = "OC-01", Fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Total = 2500m, ClienteId = 1 },
            new OrdenCompra { Id = 2, NumeroOrden = "OC-02", Fecha = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), Total = 2500m, ClienteId = 1 },
            new OrdenCompra { Id = 3, NumeroOrden = "OC-03", Fecha = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), Total = 2500m, ClienteId = 1 },
            new OrdenCompra { Id = 4, NumeroOrden = "OC-04", Fecha = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), Total = 2500m, ClienteId = 1 },
            new OrdenCompra { Id = 5, NumeroOrden = "OC-05", Fecha = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), Total = 2000m, ClienteId = 2 });
        contexto.SegmentosClientes.Add(new SegmentoCliente
        {
            Id = 1,
            Codigo = "VIP",
            Nombre = "Cliente VIP",
            Prioridad = 1,
            MontoMinimo = 5000m,
            OrdenesMinimas = 3,
            Activo = true
        });
        contexto.SaveChanges();
        _baseDatosSembrada = true;
    }
}
