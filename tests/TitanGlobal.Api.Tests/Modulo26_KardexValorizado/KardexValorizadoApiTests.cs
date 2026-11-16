using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo26_KardexValorizado;

[Collection("Modulo04")]
public class KardexValorizadoApiTests
{
    private const string Ruta = "/api/v1/kardex-valorizado";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public KardexValorizadoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task ListarFiltraPorProductoAlmacenTipoYFechasDeMasNuevoAMasAntiguo()
    {
        DateTime hoy = DateTime.UtcNow.Date;
        string desde = hoy.AddDays(-9).ToString("yyyy-MM-dd");
        string hasta = hoy.AddDays(-8).ToString("yyyy-MM-dd");

        PaginaMovimientosDto? porProducto = await _clienteHttp.GetFromJsonAsync<PaginaMovimientosDto>(
            Ruta + "/movimientos?productoId=1");
        PaginaMovimientosDto? porAlmacen = await _clienteHttp.GetFromJsonAsync<PaginaMovimientosDto>(
            Ruta + "/movimientos?almacenId=2");
        PaginaMovimientosDto? salidas = await _clienteHttp.GetFromJsonAsync<PaginaMovimientosDto>(
            Ruta + "/movimientos?tipo=salida");
        PaginaMovimientosDto? porFechas = await _clienteHttp.GetFromJsonAsync<PaginaMovimientosDto>(
            Ruta + "/movimientos?fechaDesde=" + desde + "&fechaHasta=" + hasta);

        Assert.NotNull(porProducto);
        Assert.Equal(new[] { 5, 3, 2, 1 }, porProducto.Items.Select(movimiento => movimiento.Id).ToArray());
        Assert.NotNull(porAlmacen);
        Assert.Equal(5, Assert.Single(porAlmacen.Items).Id);
        Assert.NotNull(salidas);
        Assert.Equal(3, Assert.Single(salidas.Items).Id);
        Assert.NotNull(porFechas);
        Assert.Equal(new[] { 3, 2 }, porFechas.Items.Select(movimiento => movimiento.Id).ToArray());
    }

    [Theory]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?tipo=Otro", "El tipo de movimiento no es válido.")]
    [InlineData("?fechaDesde=2026-02-10&fechaHasta=2026-02-01", "La fecha inicial no puede ser posterior a la final.")]
    public async Task ListarConFiltrosInvalidosDevuelveBadRequest(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + "/movimientos" + consulta);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task RegistrarUnaEntradaLaAgregaConLaFechaActualPorDefecto()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(2, 1, "entrada", 5, 10.5m);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);
        PaginaMovimientosDto? delProducto = await _clienteHttp.GetFromJsonAsync<PaginaMovimientosDto>(
            Ruta + "/movimientos?productoId=2");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(delProducto);
        Assert.Equal(2, delProducto.Total);
        MovimientoListadoDto nuevo = delProducto.Items[0];
        Assert.Equal("Entrada", nuevo.Tipo);
        Assert.Equal(10.5m, nuevo.CostoUnitario);
        Assert.True(nuevo.Fecha <= DateTime.UtcNow);
    }

    [Fact]
    public async Task RegistrarUnaSalidaDescuentaElSaldo()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(2, 1, "Salida", 3, null);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);
        List<SaldoKardexDto>? saldos = await _clienteHttp.GetFromJsonAsync<List<SaldoKardexDto>>(
            Ruta + "/saldos?almacenId=1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(saldos);
        SaldoKardexDto saldoProducto = Assert.Single(saldos, saldo => saldo.ProductoId == 2);
        Assert.Equal(0, saldoProducto.Saldo);
    }

    [Theory]
    [InlineData("Otro", 5, 10.0, "El tipo de movimiento no es válido.")]
    [InlineData("Entrada", 0, 10.0, "La cantidad del movimiento debe estar entre 1 y 1000000.")]
    [InlineData("Entrada", 1000001, 10.0, "La cantidad del movimiento debe estar entre 1 y 1000000.")]
    [InlineData("Entrada", 5, null, "El costo unitario es obligatorio en las entradas.")]
    [InlineData("Entrada", 5, 0.0, "El costo unitario debe ser mayor que 0, no superar 1000000 y tener hasta 4 decimales.")]
    [InlineData("Entrada", 5, 10.12345, "El costo unitario debe ser mayor que 0, no superar 1000000 y tener hasta 4 decimales.")]
    [InlineData("Entrada", 5, 1000001.0, "El costo unitario debe ser mayor que 0, no superar 1000000 y tener hasta 4 decimales.")]
    [InlineData("Salida", 1, 10.0, "Las salidas no llevan costo unitario.")]
    public async Task RegistrarConDatosInvalidosDevuelveBadRequestSinCambiarDatos(
        string tipo,
        int cantidad,
        double? costo,
        string mensaje)
    {
        decimal? costoUnitario = null;
        if (costo.HasValue)
        {
            costoUnitario = (decimal)costo.Value;
        }

        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 1, tipo, cantidad, costoUnitario);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(5, await ContarMovimientosAsync());
    }

    [Fact]
    public async Task RegistrarConProductoInexistenteDevuelveBadRequest()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(999, 1, "Entrada", 1, 5m);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El producto 999 no existe.");
    }

    [Fact]
    public async Task RegistrarConAlmacenInexistenteDevuelveBadRequest()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 99, "Entrada", 1, 5m);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El almacén 99 no existe.");
    }

    [Fact]
    public async Task RegistrarEnUnAlmacenInactivoDevuelveConflicto()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 3, "Entrada", 1, 5m);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El almacén ALM-K3 está inactivo.");
    }

    [Fact]
    public async Task RegistrarConFechaFuturaDevuelveBadRequest()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 1, "Entrada", 1, 5m);
        solicitud.Fecha = DateTime.UtcNow.AddDays(2);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La fecha del movimiento no puede ser futura.");
    }

    [Fact]
    public async Task RegistrarConReferenciaMuyLargaDevuelveBadRequest()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 1, "Entrada", 1, 5m);
        solicitud.Referencia = new string('x', 101);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La referencia no puede superar 100 caracteres.");
    }

    [Fact]
    public async Task RegistrarConLoteInexistenteDevuelveBadRequest()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 1, "Entrada", 1, 5m);
        solicitud.LoteId = 99;

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El lote 99 no existe.");
    }

    [Fact]
    public async Task RegistrarConLoteDeOtroProductoDevuelveConflicto()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 1, "Entrada", 1, 5m);
        solicitud.LoteId = 2;

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El lote 2 no pertenece al producto 1.");
    }

    [Fact]
    public async Task RegistrarUnaSalidaMayorQueElSaldoDevuelveConflictoSinCambiarDatos()
    {
        GuardarMovimientoRequestDto solicitud = CrearSolicitud(1, 1, "Salida", 6, null);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta + "/movimientos", solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Stock insuficiente: el saldo es 5 y se pidieron 6.");
        Assert.Equal(5, await ContarMovimientosAsync());
    }

    [Fact]
    public async Task KardexPorPromedioValorizaLasSalidasConElCostoPromedioMovil()
    {
        KardexDto? kardex = await _clienteHttp.GetFromJsonAsync<KardexDto>(
            Ruta + "/kardex?productoId=1&almacenId=1&metodo=promedio");

        Assert.NotNull(kardex);
        Assert.Equal("Promedio", kardex.Metodo);
        Assert.Equal(3, kardex.Lineas.Count);
        Assert.Equal(20, kardex.Lineas[1].SaldoCantidad);
        Assert.Equal(120m, kardex.Lineas[1].SaldoValor);
        Assert.Equal(6m, kardex.Lineas[1].CostoPromedio);
        Assert.Equal(6m, kardex.Lineas[2].CostoUnitario);
        Assert.Equal(90m, kardex.Lineas[2].ValorMovimiento);
        Assert.Equal(5, kardex.SaldoCantidad);
        Assert.Equal(30m, kardex.SaldoValor);
    }

    [Fact]
    public async Task KardexPorPepsConsumeLasCapasMasAntiguasPrimero()
    {
        KardexDto? kardex = await _clienteHttp.GetFromJsonAsync<KardexDto>(
            Ruta + "/kardex?productoId=1&almacenId=1&metodo=PEPS");

        Assert.NotNull(kardex);
        Assert.Equal("PEPS", kardex.Metodo);
        Assert.Equal(5.6667m, kardex.Lineas[2].CostoUnitario);
        Assert.Equal(85m, kardex.Lineas[2].ValorMovimiento);
        Assert.Equal(5, kardex.SaldoCantidad);
        Assert.Equal(35m, kardex.SaldoValor);
        Assert.Equal(7m, kardex.Lineas[2].CostoPromedio);
    }

    [Fact]
    public async Task KardexSinMetodoUsaElPromedio()
    {
        KardexDto? kardex = await _clienteHttp.GetFromJsonAsync<KardexDto>(
            Ruta + "/kardex?productoId=1&almacenId=1");

        Assert.NotNull(kardex);
        Assert.Equal("Promedio", kardex.Metodo);
    }

    [Fact]
    public async Task KardexConMetodoInvalidoDevuelveBadRequest()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(
            Ruta + "/kardex?productoId=1&almacenId=1&metodo=lifo");

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El método de valoración no es válido.");
    }

    [Fact]
    public async Task KardexConProductoInexistenteDevuelveBadRequest()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + "/kardex?productoId=999&almacenId=1");

        await AfirmarErrorNegocioAsync(respuesta, HttpStatusCode.BadRequest, 400, "El producto 999 no existe.");
    }

    [Fact]
    public async Task SaldosAgrupanPorProductoYAlmacenYSeFiltranPorAlmacen()
    {
        List<SaldoKardexDto>? todos = await _clienteHttp.GetFromJsonAsync<List<SaldoKardexDto>>(Ruta + "/saldos");
        List<SaldoKardexDto>? delAlmacen = await _clienteHttp.GetFromJsonAsync<List<SaldoKardexDto>>(
            Ruta + "/saldos?almacenId=1");

        Assert.NotNull(todos);
        Assert.Equal(new[] { 5, 4, 3 }, todos.Select(saldo => saldo.Saldo).ToArray());
        Assert.Equal(new[] { 1, 1, 2 }, todos.Select(saldo => saldo.ProductoId).ToArray());
        Assert.NotNull(delAlmacen);
        Assert.Equal(new[] { 5, 3 }, delAlmacen.Select(saldo => saldo.Saldo).ToArray());
        Assert.Equal(20, delAlmacen[0].Entradas);
        Assert.Equal(15, delAlmacen[0].Salidas);
    }

    [Fact]
    public async Task ResumenDevuelveMovimientosYUnidades()
    {
        ResumenKardexDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenKardexDto>(Ruta + "/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.TotalMovimientos);
        Assert.Equal(4, resumen.TotalEntradas);
        Assert.Equal(1, resumen.TotalSalidas);
        Assert.Equal(27, resumen.UnidadesEntradas);
        Assert.Equal(15, resumen.UnidadesSalidas);
    }

    private static GuardarMovimientoRequestDto CrearSolicitud(
        int productoId,
        int almacenId,
        string tipo,
        int cantidad,
        decimal? costoUnitario)
    {
        return new GuardarMovimientoRequestDto
        {
            ProductoId = productoId,
            AlmacenId = almacenId,
            Tipo = tipo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario
        };
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.MovimientosKardex.RemoveRange(contexto.MovimientosKardex);
        contexto.UnidadesSerie.RemoveRange(contexto.UnidadesSerie);
        contexto.Lotes.RemoveRange(contexto.Lotes);
        contexto.ZonasAlmacenaje.RemoveRange(contexto.ZonasAlmacenaje);
        contexto.Almacenes.RemoveRange(contexto.Almacenes);
        contexto.Almacenes.AddRange(
            CrearAlmacen(1, "ALM-K1", true),
            CrearAlmacen(2, "ALM-K2", true),
            CrearAlmacen(3, "ALM-K3", false));
        contexto.Lotes.AddRange(
            new Lote { Id = 1, ProductoId = 1, Codigo = "LOT-K1", Cantidad = 100, Estado = "Disponible" },
            new Lote { Id = 2, ProductoId = 2, Codigo = "LOT-K2", Cantidad = 100, Estado = "Disponible" });
        contexto.MovimientosKardex.AddRange(
            CrearMovimiento(1, 1, 1, "Entrada", 10, 5m, 10),
            CrearMovimiento(2, 1, 1, "Entrada", 10, 7m, 9),
            CrearMovimiento(3, 1, 1, "Salida", 15, null, 8),
            CrearMovimiento(4, 2, 1, "Entrada", 3, 100m, 5),
            CrearMovimiento(5, 1, 2, "Entrada", 4, 8m, 3));
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
            Direccion = "Av. Kardex 1",
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

    private async Task<int> ContarMovimientosAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.MovimientosKardex.CountAsync();
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
