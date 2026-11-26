using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo29_AjustesInventario;

[Collection("Modulo04")]
public class AjustesInventarioApiTests
{
    private const string Ruta = "/api/v1/ajustes-inventario";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public AjustesInventarioApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task ListarFiltraPorAlmacenProductoTipoMotivoBusquedaYFechas()
    {
        DateTime hoy = DateTime.UtcNow.Date;
        string desde = hoy.AddDays(-6).ToString("yyyy-MM-dd");
        string hasta = hoy.AddDays(-2).ToString("yyyy-MM-dd");

        PaginaAjustesInventarioDto? porAlmacen = await _clienteHttp.GetFromJsonAsync<PaginaAjustesInventarioDto>(
            Ruta + "?almacenId=2");
        PaginaAjustesInventarioDto? porProducto = await _clienteHttp.GetFromJsonAsync<PaginaAjustesInventarioDto>(
            Ruta + "?productoId=1");
        PaginaAjustesInventarioDto? decrementos = await _clienteHttp.GetFromJsonAsync<PaginaAjustesInventarioDto>(
            Ruta + "?tipo=decremento");
        PaginaAjustesInventarioDto? porMotivo = await _clienteHttp.GetFromJsonAsync<PaginaAjustesInventarioDto>(
            Ruta + "?motivo=merma");
        PaginaAjustesInventarioDto? porBusqueda = await _clienteHttp.GetFromJsonAsync<PaginaAjustesInventarioDto>(
            Ruta + "?busqueda=AJU-002");
        PaginaAjustesInventarioDto? porFechas = await _clienteHttp.GetFromJsonAsync<PaginaAjustesInventarioDto>(
            Ruta + "?fechaDesde=" + desde + "&fechaHasta=" + hasta);

        Assert.NotNull(porAlmacen);
        Assert.Equal(3, Assert.Single(porAlmacen.Items).Id);
        Assert.NotNull(porProducto);
        Assert.Equal(new[] { 3, 2, 1 }, porProducto.Items.Select(item => item.Id).ToArray());
        Assert.NotNull(decrementos);
        Assert.Equal(2, Assert.Single(decrementos.Items).Id);
        Assert.NotNull(porMotivo);
        Assert.Equal(2, Assert.Single(porMotivo.Items).Id);
        Assert.NotNull(porBusqueda);
        Assert.Equal("AJU-002", Assert.Single(porBusqueda.Items).Codigo);
        Assert.NotNull(porFechas);
        Assert.Equal(new[] { 2, 1 }, porFechas.Items.Select(item => item.Id).ToArray());
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tipo=Otro", "El tipo de ajuste debe ser Incremento o Decremento.")]
    [InlineData("?motivo=Otro", "El motivo del ajuste no es válido.")]
    [InlineData("?fechaDesde=2026-05-10&fechaHasta=2026-05-01", "La fecha inicial no puede ser mayor que la fecha final.")]
    public async Task ListarConFiltrosInvalidosDevuelveBadRequest(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ObtenerPorIdDevuelveDetalleYNotFoundCuandoNoExiste()
    {
        AjusteInventarioItemDto? detalle = await _clienteHttp.GetFromJsonAsync<AjusteInventarioItemDto>(Ruta + "/1");
        HttpResponseMessage noExiste = await _clienteHttp.GetAsync(Ruta + "/999");

        Assert.NotNull(detalle);
        Assert.Equal("AJU-001", detalle.Codigo);
        Assert.Equal("ALM-A1", detalle.AlmacenCodigo);
        Assert.Equal("Incremento", detalle.Tipo);
        Assert.Equal(50m, detalle.ValorTotal);
        await AfirmarErrorNegocioAsync(
            noExiste,
            HttpStatusCode.NotFound,
            404,
            "El ajuste de inventario 999 no existe.");
    }

    [Fact]
    public async Task RegistrarIncrementoCreaAjusteYMovimientoEntradaEnKardex()
    {
        RegistrarAjusteInventarioRequestDto solicitud = CrearSolicitud(
            "AJU-100",
            1,
            1,
            "Incremento",
            "ConteoFisico",
            4,
            8m,
            "Maxi");

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        AjusteInventarioItemDto? creado = await respuesta.Content.ReadFromJsonAsync<AjusteInventarioItemDto>();
        KardexDto? kardex = await _clienteHttp.GetFromJsonAsync<KardexDto>(
            "/api/v1/kardex-valorizado/kardex?productoId=1&almacenId=1");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(creado);
        Assert.Equal("AJU-100", creado.Codigo);
        Assert.Equal("Incremento", creado.Tipo);
        Assert.Equal(8m, creado.CostoUnitario);
        Assert.Equal(32m, creado.ValorTotal);
        Assert.NotNull(kardex);
        Assert.Equal(14, kardex.SaldoCantidad);
        Assert.Equal(92m, kardex.SaldoValor);
    }

    [Fact]
    public async Task RegistrarIncrementoSinCostoUsaCostoPromedioDelKardex()
    {
        RegistrarAjusteInventarioRequestDto solicitud = CrearSolicitud(
            "AJU-101",
            1,
            1,
            "Incremento",
            "Correccion",
            2,
            null,
            "Maxi");

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        AjusteInventarioItemDto? creado = await respuesta.Content.ReadFromJsonAsync<AjusteInventarioItemDto>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(creado);
        Assert.Equal(6m, creado.CostoUnitario);
        Assert.Equal(12m, creado.ValorTotal);
    }

    [Fact]
    public async Task RegistrarDecrementoValorizaAlCostoPromedioYDescuentaKardex()
    {
        RegistrarAjusteInventarioRequestDto solicitud = CrearSolicitud(
            "AJU-102",
            1,
            1,
            "Decremento",
            "Dano",
            3,
            null,
            "Maxi");

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        AjusteInventarioItemDto? creado = await respuesta.Content.ReadFromJsonAsync<AjusteInventarioItemDto>();
        KardexDto? kardex = await _clienteHttp.GetFromJsonAsync<KardexDto>(
            "/api/v1/kardex-valorizado/kardex?productoId=1&almacenId=1");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(creado);
        Assert.Equal("Decremento", creado.Tipo);
        Assert.Equal(6m, creado.CostoUnitario);
        Assert.Equal(18m, creado.ValorTotal);
        Assert.NotNull(kardex);
        Assert.Equal(7, kardex.SaldoCantidad);
        Assert.Equal(42m, kardex.SaldoValor);
    }

    [Fact]
    public async Task RegistrarDecrementoMayorAlSaldoDevuelveConflictoSinAlterarDatos()
    {
        RegistrarAjusteInventarioRequestDto solicitud = CrearSolicitud(
            "AJU-103",
            1,
            1,
            "Decremento",
            "Merma",
            11,
            null,
            "Maxi");

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Stock insuficiente: el saldo es 10 y se pidieron 11.");
        Assert.Equal(3, await ContarAjustesAsync());
    }

    [Fact]
    public async Task RegistrarConCodigoDuplicadoOAlmacenInactivoDevuelveConflicto()
    {
        RegistrarAjusteInventarioRequestDto duplicado = CrearSolicitud(
            " aju-001 ",
            1,
            1,
            "Incremento",
            "ConteoFisico",
            1,
            5m,
            "Maxi");
        RegistrarAjusteInventarioRequestDto inactivo = CrearSolicitud(
            "AJU-104",
            3,
            1,
            "Incremento",
            "ConteoFisico",
            1,
            5m,
            "Maxi");

        HttpResponseMessage respDuplicado = await _clienteHttp.PostAsJsonAsync(Ruta, duplicado);
        HttpResponseMessage respInactivo = await _clienteHttp.PostAsJsonAsync(Ruta, inactivo);

        await AfirmarErrorNegocioAsync(
            respDuplicado,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un ajuste de inventario con el código aju-001.");
        await AfirmarErrorNegocioAsync(
            respInactivo,
            HttpStatusCode.Conflict,
            409,
            "El almacén ALM-A3 está inactivo.");
    }

    [Theory]
    [InlineData("", 1, 1, "Incremento", "ConteoFisico", 1, 5.0, "Maxi", "El código del ajuste de inventario es obligatorio.")]
    [InlineData("AJU-ERR", 99, 1, "Incremento", "ConteoFisico", 1, 5.0, "Maxi", "El almacén 99 no existe.")]
    [InlineData("AJU-ERR", 1, 999, "Incremento", "ConteoFisico", 1, 5.0, "Maxi", "El producto 999 no existe.")]
    [InlineData("AJU-ERR", 1, 1, "Otro", "ConteoFisico", 1, 5.0, "Maxi", "El tipo de ajuste debe ser Incremento o Decremento.")]
    [InlineData("AJU-ERR", 1, 1, "Incremento", "Otro", 1, 5.0, "Maxi", "El motivo del ajuste no es válido.")]
    [InlineData("AJU-ERR", 1, 1, "Incremento", "ConteoFisico", 0, 5.0, "Maxi", "La cantidad del movimiento debe estar entre 1 y 1000000.")]
    [InlineData("AJU-ERR", 1, 1, "Incremento", "ConteoFisico", 1, 0.0, "Maxi", "El costo unitario debe ser mayor que 0, no superar 1000000 y tener hasta 4 decimales.")]
    [InlineData("AJU-ERR", 1, 1, "Decremento", "Merma", 1, 5.0, "Maxi", "Los ajustes de decremento se valorizan automáticamente al costo promedio del Kardex y no reciben costo unitario.")]
    [InlineData("AJU-ERR", 1, 1, "Incremento", "ConteoFisico", 1, 5.0, "", "El responsable del ajuste es obligatorio.")]
    public async Task RegistrarConDatosInvalidosDevuelveBadRequest(
        string codigo,
        int almacenId,
        int productoId,
        string tipo,
        string motivo,
        int cantidad,
        double? costo,
        string responsable,
        string mensaje)
    {
        decimal? costoUnitario = costo.HasValue ? (decimal)costo.Value : null;
        RegistrarAjusteInventarioRequestDto solicitud = CrearSolicitud(
            codigo,
            almacenId,
            productoId,
            tipo,
            motivo,
            cantidad,
            costoUnitario,
            responsable);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(3, await ContarAjustesAsync());
    }

    [Fact]
    public async Task ResumenYValoresInicialesDevuelvenIndicadoresYCatalogos()
    {
        ResumenAjustesInventarioDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenAjustesInventarioDto>(
            Ruta + "/resumen");
        ValoresInicialesAjustesDto? iniciales = await _clienteHttp.GetFromJsonAsync<ValoresInicialesAjustesDto>(
            Ruta + "/valores-iniciales");

        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.TotalAjustes);
        Assert.Equal(2, resumen.TotalIncrementos);
        Assert.Equal(1, resumen.TotalDecrementos);
        Assert.Equal(74m, resumen.ValorIncrementos);
        Assert.Equal(12m, resumen.ValorDecrementos);
        Assert.Equal(62m, resumen.ValorNeto);
        Assert.NotNull(iniciales);
        Assert.Equal(new[] { "Incremento", "Decremento" }, iniciales.Tipos);
        Assert.Equal(new[] { "ConteoFisico", "Merma", "Dano", "Caducidad", "Correccion" }, iniciales.Motivos);
    }

    private static RegistrarAjusteInventarioRequestDto CrearSolicitud(
        string codigo,
        int almacenId,
        int productoId,
        string tipo,
        string motivo,
        int cantidad,
        decimal? costoUnitario,
        string responsable)
    {
        return new RegistrarAjusteInventarioRequestDto
        {
            Codigo = codigo,
            AlmacenId = almacenId,
            ProductoId = productoId,
            Tipo = tipo,
            Motivo = motivo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Responsable = responsable,
            Observaciones = "Prueba automatizada"
        };
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.MovimientosKardex.RemoveRange(contexto.MovimientosKardex);
        contexto.AjustesInventario.RemoveRange(contexto.AjustesInventario);
        contexto.UnidadesSerie.RemoveRange(contexto.UnidadesSerie);
        contexto.Lotes.RemoveRange(contexto.Lotes);
        contexto.ZonasAlmacenaje.RemoveRange(contexto.ZonasAlmacenaje);
        contexto.Almacenes.RemoveRange(contexto.Almacenes);

        contexto.Almacenes.AddRange(
            CrearAlmacen(1, "ALM-A1", true),
            CrearAlmacen(2, "ALM-A2", true),
            CrearAlmacen(3, "ALM-A3", false));

        AjusteInventario ajuste1 = new AjusteInventario
        {
            Id = 1,
            Codigo = "AJU-001",
            AlmacenId = 1,
            ProductoId = 1,
            Tipo = "Incremento",
            Motivo = "ConteoFisico",
            Cantidad = 10,
            CostoUnitario = 5m,
            Fecha = DateTime.UtcNow.Date.AddDays(-5),
            Responsable = "Ana Torres"
        };
        AjusteInventario ajuste2 = new AjusteInventario
        {
            Id = 2,
            Codigo = "AJU-002",
            AlmacenId = 1,
            ProductoId = 1,
            Tipo = "Decremento",
            Motivo = "Merma",
            Cantidad = 2,
            CostoUnitario = 6m,
            Fecha = DateTime.UtcNow.Date.AddDays(-3),
            Responsable = "Luis Vega"
        };
        AjusteInventario ajuste3 = new AjusteInventario
        {
            Id = 3,
            Codigo = "AJU-003",
            AlmacenId = 2,
            ProductoId = 1,
            Tipo = "Incremento",
            Motivo = "Correccion",
            Cantidad = 3,
            CostoUnitario = 8m,
            Fecha = DateTime.UtcNow.Date.AddDays(-1),
            Responsable = "Ana Torres"
        };
        contexto.AjustesInventario.AddRange(ajuste1, ajuste2, ajuste3);

        contexto.MovimientosKardex.AddRange(
            new MovimientoKardex
            {
                Id = 1,
                ProductoId = 1,
                AlmacenId = 1,
                Tipo = "Entrada",
                Cantidad = 10,
                CostoUnitario = 5m,
                Fecha = DateTime.UtcNow.Date.AddDays(-5),
                Referencia = "AJUSTE AJU-001",
                AjusteInventarioId = 1
            },
            new MovimientoKardex
            {
                Id = 2,
                ProductoId = 1,
                AlmacenId = 1,
                Tipo = "Entrada",
                Cantidad = 2,
                CostoUnitario = 11m,
                Fecha = DateTime.UtcNow.Date.AddDays(-4),
                Referencia = "COMPRA-01"
            },
            new MovimientoKardex
            {
                Id = 3,
                ProductoId = 1,
                AlmacenId = 1,
                Tipo = "Salida",
                Cantidad = 2,
                CostoUnitario = null,
                Fecha = DateTime.UtcNow.Date.AddDays(-3),
                Referencia = "AJUSTE AJU-002",
                AjusteInventarioId = 2
            },
            new MovimientoKardex
            {
                Id = 4,
                ProductoId = 1,
                AlmacenId = 2,
                Tipo = "Entrada",
                Cantidad = 3,
                CostoUnitario = 8m,
                Fecha = DateTime.UtcNow.Date.AddDays(-1),
                Referencia = "AJUSTE AJU-003",
                AjusteInventarioId = 3
            });

        contexto.SaveChanges();
    }

    private static Almacen CrearAlmacen(int id, string codigo, bool activo)
    {
        return new Almacen
        {
            Id = id,
            Codigo = codigo,
            Nombre = "Almacén " + codigo,
            Tipo = "Central",
            Direccion = "Av. Inventario 1",
            Ciudad = "Lima",
            CapacidadM3 = 100m,
            Responsable = "QA",
            Activo = activo
        };
    }

    private async Task<int> ContarAjustesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.AjustesInventario.CountAsync();
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
