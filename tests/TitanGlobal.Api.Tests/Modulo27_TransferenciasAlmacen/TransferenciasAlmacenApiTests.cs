using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo27_TransferenciasAlmacen;

[Collection("Modulo04")]
public class TransferenciasAlmacenApiTests
{
    private const string Ruta = "/api/v1/transferencias-almacen";

    private const string RutaKardex = "/api/v1/kardex-valorizado";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public TransferenciasAlmacenApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task RegistrarCreaLaTransferenciaYSusDosMovimientosLigados()
    {
        GuardarTransferenciaRequestDto solicitud = CrearSolicitud(1, 1, 2, 4);
        solicitud.Observaciones = "  Reposición  ";

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        TransferenciaCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<TransferenciaCreadaDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Equal(6m, creada.CostoUnitario);
        List<MovimientoKardex> movimientos = await ObtenerMovimientosDeAsync(creada.Id);
        Assert.Equal(2, movimientos.Count);
        MovimientoKardex salida = Assert.Single(movimientos, movimiento => movimiento.Tipo == "Salida");
        MovimientoKardex entrada = Assert.Single(movimientos, movimiento => movimiento.Tipo == "Entrada");
        Assert.Equal(1, salida.AlmacenId);
        Assert.Null(salida.CostoUnitario);
        Assert.Equal(4, salida.Cantidad);
        Assert.Equal(2, entrada.AlmacenId);
        Assert.Equal(6m, entrada.CostoUnitario);
        Assert.Equal(4, entrada.Cantidad);
        Assert.Equal(5, await ContarMovimientosAsync());
        Assert.Equal(1, await ContarTransferenciasAsync());
        TransferenciaDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<TransferenciaDetalleDto>(
            Ruta + "/" + creada.Id);
        Assert.NotNull(detalle);
        Assert.Equal("Reposición", detalle.Observaciones);
    }

    [Fact]
    public async Task ElCostoEsElPromedioMovilDelOrigenYActualizaElKardexDeAmbosAlmacenes()
    {
        await RegistrarAsync(1, 1, 2, 5);

        KardexDto? destino = await _clienteHttp.GetFromJsonAsync<KardexDto>(
            RutaKardex + "/kardex?productoId=1&almacenId=2");
        KardexDto? origen = await _clienteHttp.GetFromJsonAsync<KardexDto>(
            RutaKardex + "/kardex?productoId=1&almacenId=1");

        Assert.NotNull(destino);
        Assert.Equal(5, destino.SaldoCantidad);
        Assert.Equal(30m, destino.SaldoValor);
        Assert.NotNull(origen);
        Assert.Equal(15, origen.SaldoCantidad);
        Assert.Equal(90m, origen.SaldoValor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000001)]
    public async Task RegistrarConCantidadFueraDeRangoDevuelveBadRequestSinCambiarDatos(int cantidad)
    {
        GuardarTransferenciaRequestDto solicitud = CrearSolicitud(1, 1, 2, cantidad);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La cantidad de la transferencia debe estar entre 1 y 1000000.");
        Assert.Equal(0, await ContarTransferenciasAsync());
        Assert.Equal(3, await ContarMovimientosAsync());
    }

    [Fact]
    public async Task RegistrarConObservacionesMuyLargasDevuelveBadRequest()
    {
        GuardarTransferenciaRequestDto solicitud = CrearSolicitud(1, 1, 2, 1);
        solicitud.Observaciones = new string('x', 501);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "Las observaciones no pueden superar 500 caracteres.");
    }

    [Fact]
    public async Task RegistrarConProductoInexistenteDevuelveBadRequest()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud(999, 1, 2, 1));

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El producto 999 no existe.");
    }

    [Theory]
    [InlineData(99, 2)]
    [InlineData(1, 99)]
    public async Task RegistrarConUnAlmacenInexistenteDevuelveBadRequest(int origenId, int destinoId)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            Ruta,
            CrearSolicitud(1, origenId, destinoId, 1));

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El almacén 99 no existe.");
    }

    [Fact]
    public async Task RegistrarConElMismoAlmacenDeOrigenYDestinoDevuelveBadRequest()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud(1, 1, 1, 1));

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El almacén de origen y el de destino deben ser distintos.");
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(1, 3)]
    public async Task RegistrarConUnAlmacenInactivoDevuelveConflicto(int origenId, int destinoId)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            Ruta,
            CrearSolicitud(1, origenId, destinoId, 1));

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El almacén ALM-T3 está inactivo.");
    }

    [Fact]
    public async Task RegistrarMasQueElSaldoDelOrigenDevuelveConflictoSinCambiarDatos()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud(1, 1, 2, 21));

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Stock insuficiente: el saldo es 20 y se pidieron 21.");
        Assert.Equal(0, await ContarTransferenciasAsync());
        Assert.Equal(3, await ContarMovimientosAsync());
    }

    [Fact]
    public async Task RegistrarDesdeUnAlmacenSinMovimientosDevuelveConflicto()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, CrearSolicitud(1, 2, 1, 1));

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Stock insuficiente: el saldo es 0 y se pidieron 1.");
    }

    [Fact]
    public async Task ListarFiltraPorProductoAlmacenesYFechasDeMasNuevaAMasAntigua()
    {
        TransferenciaCreadaDto primera = await RegistrarAsync(1, 1, 2, 4);
        TransferenciaCreadaDto segunda = await RegistrarAsync(2, 1, 2, 1);
        string hoy = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        string ayer = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");

        PaginaTransferenciasDto? todas = await _clienteHttp.GetFromJsonAsync<PaginaTransferenciasDto>(Ruta);
        PaginaTransferenciasDto? porProducto = await _clienteHttp.GetFromJsonAsync<PaginaTransferenciasDto>(
            Ruta + "?productoId=2");
        PaginaTransferenciasDto? porOrigen = await _clienteHttp.GetFromJsonAsync<PaginaTransferenciasDto>(
            Ruta + "?almacenOrigenId=1");
        PaginaTransferenciasDto? porDestinoSinResultados = await _clienteHttp.GetFromJsonAsync<PaginaTransferenciasDto>(
            Ruta + "?almacenDestinoId=1");
        PaginaTransferenciasDto? desdeHoy = await _clienteHttp.GetFromJsonAsync<PaginaTransferenciasDto>(
            Ruta + "?fechaDesde=" + hoy);
        PaginaTransferenciasDto? hastaAyer = await _clienteHttp.GetFromJsonAsync<PaginaTransferenciasDto>(
            Ruta + "?fechaHasta=" + ayer);

        Assert.NotNull(todas);
        Assert.Equal(new[] { segunda.Id, primera.Id }, todas.Items.Select(item => item.Id).ToArray());
        Assert.Equal("ALM-T1", todas.Items[0].AlmacenOrigenCodigo);
        Assert.Equal("ALM-T2", todas.Items[0].AlmacenDestinoCodigo);
        Assert.Equal(24m, todas.Items[1].ValorTotal);
        Assert.NotNull(porProducto);
        Assert.Equal(segunda.Id, Assert.Single(porProducto.Items).Id);
        Assert.NotNull(porOrigen);
        Assert.Equal(2, porOrigen.Total);
        Assert.NotNull(porDestinoSinResultados);
        Assert.Empty(porDestinoSinResultados.Items);
        Assert.NotNull(desdeHoy);
        Assert.Equal(2, desdeHoy.Total);
        Assert.NotNull(hastaAyer);
        Assert.Empty(hastaAyer.Items);
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?fechaDesde=2026-02-10&fechaHasta=2026-02-01", "La fecha inicial no puede ser posterior a la final.")]
    public async Task ListarConFiltrosInvalidosDevuelveBadRequest(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + consulta);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ObtenerDevuelveElDetalleConLosIdsDeLosDosMovimientos()
    {
        TransferenciaCreadaDto creada = await RegistrarAsync(1, 1, 2, 4);

        TransferenciaDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<TransferenciaDetalleDto>(
            Ruta + "/" + creada.Id);

        Assert.NotNull(detalle);
        Assert.Equal(1, detalle.ProductoId);
        Assert.Equal("ALM-T1", detalle.AlmacenOrigenCodigo);
        Assert.Equal("ALM-T2", detalle.AlmacenDestinoCodigo);
        Assert.Equal(4, detalle.Cantidad);
        Assert.Equal(6m, detalle.CostoUnitario);
        Assert.Equal(24m, detalle.ValorTotal);
        Assert.True(detalle.MovimientoSalidaId > 0);
        Assert.True(detalle.MovimientoEntradaId > 0);
        Assert.NotEqual(detalle.MovimientoSalidaId, detalle.MovimientoEntradaId);
    }

    [Fact]
    public async Task ObtenerUnaTransferenciaInexistenteDevuelveNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + "/999");

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.NotFound, 404, "La transferencia 999 no existe.");
    }

    [Fact]
    public async Task ResumenSumaTransferenciasUnidadesYValor()
    {
        await RegistrarAsync(1, 1, 2, 4);
        await RegistrarAsync(2, 1, 2, 1);

        ResumenTransferenciasDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenTransferenciasDto>(
            Ruta + "/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(2, resumen.TotalTransferencias);
        Assert.Equal(5, resumen.UnidadesTransferidas);
        Assert.Equal(124m, resumen.ValorTransferido);
    }

    [Fact]
    public async Task ResumenSinTransferenciasDevuelveCeros()
    {
        ResumenTransferenciasDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenTransferenciasDto>(
            Ruta + "/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.TotalTransferencias);
        Assert.Equal(0, resumen.UnidadesTransferidas);
        Assert.Equal(0m, resumen.ValorTransferido);
    }

    private static GuardarTransferenciaRequestDto CrearSolicitud(
        int productoId,
        int origenId,
        int destinoId,
        int cantidad)
    {
        return new GuardarTransferenciaRequestDto
        {
            ProductoId = productoId,
            AlmacenOrigenId = origenId,
            AlmacenDestinoId = destinoId,
            Cantidad = cantidad
        };
    }

    private async Task<TransferenciaCreadaDto> RegistrarAsync(int productoId, int origenId, int destinoId, int cantidad)
    {
        GuardarTransferenciaRequestDto solicitud = CrearSolicitud(productoId, origenId, destinoId, cantidad);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        TransferenciaCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<TransferenciaCreadaDto>();
        Assert.NotNull(creada);

        return creada;
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.MovimientosKardex.RemoveRange(contexto.MovimientosKardex);
        contexto.TransferenciasAlmacen.RemoveRange(contexto.TransferenciasAlmacen);
        contexto.UnidadesSerie.RemoveRange(contexto.UnidadesSerie);
        contexto.Lotes.RemoveRange(contexto.Lotes);
        contexto.ZonasAlmacenaje.RemoveRange(contexto.ZonasAlmacenaje);
        contexto.Almacenes.RemoveRange(contexto.Almacenes);
        contexto.Almacenes.AddRange(
            CrearAlmacen(1, "ALM-T1", true),
            CrearAlmacen(2, "ALM-T2", true),
            CrearAlmacen(3, "ALM-T3", false));
        contexto.MovimientosKardex.AddRange(
            CrearMovimiento(1, 1, 1, "Entrada", 10, 5m, 10),
            CrearMovimiento(2, 1, 1, "Entrada", 10, 7m, 9),
            CrearMovimiento(3, 2, 1, "Entrada", 3, 100m, 8));
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
            Direccion = "Av. Transferencias 1",
            Ciudad = "Lima",
            CapacidadM3 = 100m,
            Responsable = "QA",
            Activo = activo
        };
    }

    private static MovimientoKardex CrearMovimiento(
        int id,
        int productoId,
        int almacenId,
        string tipo,
        int cantidad,
        decimal? costoUnitario,
        int diasAtras)
    {
        return new MovimientoKardex
        {
            Id = id,
            ProductoId = productoId,
            AlmacenId = almacenId,
            Tipo = tipo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Fecha = DateTime.UtcNow.Date.AddDays(-diasAtras)
        };
    }

    private async Task<List<MovimientoKardex>> ObtenerMovimientosDeAsync(int transferenciaId)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.MovimientosKardex
            .Where(movimiento => movimiento.TransferenciaId == transferenciaId)
            .ToListAsync();
    }

    private async Task<int> ContarMovimientosAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.MovimientosKardex.CountAsync();
    }

    private async Task<int> ContarTransferenciasAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.TransferenciasAlmacen.CountAsync();
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
