using System.Globalization;
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
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo54_CuentasPorPagar;

public class CuentasPorPagarApiTests : IDisposable
{
    private const string Ruta = "/api/v1/cuentas-por-pagar";
    private const string DatosInvalidos = "Los datos de la cuenta por pagar no son válidos.";
    private const string CuentaInexistente = "La cuenta por pagar no existe.";
    private const string CuentaAnulada = "La cuenta por pagar está anulada.";
    private const string Duplicada = "La factura del proveedor ya está registrada.";
    private static readonly DateOnly HoyLima = new DateOnly(2026, 11, 24);
    private readonly CuentasPorPagarApiFactory _factory = new CuentasPorPagarApiFactory();
    private readonly HttpClient _cliente;

    public CuentasPorPagarApiTests()
    {
        _cliente = _factory.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, _factory.Services);
        _factory.SemillaProveedoresYOrdenes();
    }

    [Fact]
    public async Task RegistraFacturaNormalizaClaveYCalculaTotalDelServidor()
    {
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(Ruta, new
        {
            proveedorId = 1,
            ordenAbastecimientoId = 1,
            serie = " f \t0 5 ",
            numeroDocumento = "0 0\u00a0ab",
            fechaDocumento = HoyLima,
            fechaVencimiento = HoyLima.AddDays(30),
            moneda = " pen ",
            baseImponible = 100.15m,
            importeImpuesto = 18.10m,
            total = 1m
        });
        CuentaPorPagarDetalleDto cuenta = await LeerAsync<CuentaPorPagarDetalleDto>(alta, HttpStatusCode.Created);
        Assert.Equal(1, cuenta.OrdenAbastecimientoId);
        Assert.Equal("F05", cuenta.Serie);
        Assert.Equal("00AB", cuenta.NumeroDocumento);
        Assert.Equal("PEN", cuenta.Moneda);
        Assert.Equal(100.15m, cuenta.BaseImponible);
        Assert.Equal(18.10m, cuenta.ImporteImpuesto);
        Assert.Equal(118.25m, cuenta.Total);
        Assert.Equal(cuenta.Total, cuenta.Saldo);
        Assert.Equal(0m, cuenta.ImportePagado);
        Assert.False(cuenta.Vencida);
        Assert.False(cuenta.Anulada);
        Uri ubicacion = Assert.IsType<Uri>(alta.Headers.Location);
        Assert.Equal($"{Ruta}/{cuenta.Id}", ubicacion.AbsolutePath);
        using HttpResponseMessage consulta = await _cliente.GetAsync(ubicacion);
        Assert.Equal(118.25m, (await LeerAsync<CuentaPorPagarDetalleDto>(consulta, HttpStatusCode.OK)).Total);
    }

    [Fact]
    public async Task OrdenOpcionalYVencimientoPactadoIndependiente()
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001", fechaDocumento: HoyLima, vencimiento: HoyLima.AddDays(-1));
        Assert.Null(cuenta.OrdenAbastecimientoId);
        Assert.True(cuenta.Vencida);
        CuentaPorPagarDetalleDto futura = await CrearAsync("002", vencimiento: HoyLima.AddYears(1));
        Assert.False(futura.Vencida);
        Assert.Equal(HoyLima.AddYears(1), futura.FechaVencimiento);
    }

    [Theory]
    [InlineData(99, null, 404, "El proveedor no existe.")]
    [InlineData(3, null, 409, "El proveedor debe estar activo para registrar la factura.")]
    [InlineData(1, 99, 404, "La orden de abastecimiento no existe.")]
    [InlineData(1, 2, 409, "La orden de abastecimiento no pertenece al proveedor de la factura.")]
    public async Task AltaValidaProveedorYOrden(int proveedor, int? orden, int estado, string literal)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, Solicitud("001", proveedor: proveedor, orden: orden));
        await ErrorAsync(respuesta, (HttpStatusCode)estado, literal);
        Assert.Empty((await ListarAsync()).Items);
    }

    [Theory]
    [InlineData(null, "001")]
    [InlineData("F54", "  \t ")]
    [InlineData("ABCDEFGHIJK", "001")]
    [InlineData("F54", "12345678901234567890123456789012345678901")]
    public async Task SerieYNumeroSonObligatoriosYRespetanLongitud(string? serie, string? numero)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, Solicitud(numero, serie: serie));
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, "La serie y el número de la factura del proveedor son obligatorios y deben respetar su longitud.");
    }

    [Fact]
    public async Task ClaveUnicaPorProveedorSeMantieneTrasAnulacion()
    {
        CuentaPorPagarDetalleDto original = await CrearAsync("0001");
        using HttpResponseMessage duplicada = await _cliente.PostAsJsonAsync(Ruta, Solicitud(" 00 01 ", serie: " f 54 "));
        await ErrorAsync(duplicada, HttpStatusCode.Conflict, Duplicada);
        using HttpResponseMessage anular = await AnularAsync(original.Id, "Factura duplicada");
        Assert.Equal(HttpStatusCode.OK, anular.StatusCode);
        using HttpResponseMessage repetida = await _cliente.PostAsJsonAsync(Ruta, Solicitud("0001"));
        await ErrorAsync(repetida, HttpStatusCode.Conflict, Duplicada);
        CuentaPorPagarDetalleDto otroProveedor = await CrearAsync("0001", proveedor: 2);
        Assert.Equal(2, otroProveedor.ProveedorId);
        Assert.Equal(2, (await ListarAsync()).TotalRegistros);
    }

    [Theory]
    [InlineData("-0.01", "18")]
    [InlineData("18", "-0.01")]
    [InlineData("100.001", "18")]
    [InlineData("100", "0.001")]
    [InlineData("0", "0")]
    [InlineData("9999999999999999.99", "0.01")]
    [InlineData("10000000000000000", "0")]
    [InlineData(null, "18")]
    [InlineData("100", null)]
    public async Task RechazaImportesInvalidos(string? baseTexto, string? impuestoTexto)
    {
        static decimal? Convertir(string? texto)
        {
            return texto is null ? null : decimal.Parse(texto, CultureInfo.InvariantCulture);
        }

        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, Solicitud("001",
            baseImponible: Convertir(baseTexto), impuesto: Convertir(impuestoTexto)));
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
        Assert.Empty((await ListarAsync()).Items);
    }

    [Fact]
    public async Task ValidaMonedaFechaDocumentariaYVencimientoObligatorio()
    {
        using HttpResponseMessage moneda = await _cliente.PostAsJsonAsync(Ruta, Solicitud("001", moneda: "EUR"));
        await ErrorAsync(moneda, HttpStatusCode.BadRequest, DatosInvalidos);
        using HttpResponseMessage fecha = await _cliente.PostAsJsonAsync(Ruta, Solicitud("002", fechaDocumento: HoyLima.AddDays(1)));
        await ErrorAsync(fecha, HttpStatusCode.BadRequest, DatosInvalidos);
        using HttpResponseMessage vencimiento = await _cliente.PostAsJsonAsync(Ruta, new
        {
            proveedorId = 1,
            serie = "F54",
            numeroDocumento = "003",
            fechaDocumento = HoyLima,
            moneda = "PEN",
            baseImponible = 100m,
            importeImpuesto = 0m
        });
        await ErrorAsync(vencimiento, HttpStatusCode.BadRequest, "La fecha de vencimiento es obligatoria.");
    }

    [Fact]
    public async Task PagoParcialYTotalHeredanMonedaYConservanImportesExactos()
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001", moneda: "USD", baseImponible: 100.13m, impuesto: 18.17m,
            vencimiento: HoyLima.AddDays(-1));
        AplicacionPagoDto parcial = await PagarAsync(cuenta.Id, 40.15m);
        Assert.Equal("USD", parcial.Moneda);
        Assert.Equal(40.15m, parcial.Importe);
        Assert.Equal(HoyLima, parcial.FechaPago);
        CuentaPorPagarDetalleDto conParcial = await ObtenerAsync(cuenta.Id);
        Assert.Equal(78.15m, conParcial.Saldo);
        Assert.Equal(40.15m, conParcial.ImportePagado);
        Assert.True(conParcial.Vencida);

        using HttpResponseMessage sobrepago = await _cliente.PostAsJsonAsync($"{Ruta}/{cuenta.Id}/pagos", Pago(78.16m));
        await ErrorAsync(sobrepago, HttpStatusCode.Conflict, "El pago supera el saldo pendiente.");
        Assert.Single((await HistorialAsync(cuenta.Id)).Items);

        await PagarAsync(cuenta.Id, 78.15m);
        CuentaPorPagarDetalleDto pagada = await ObtenerAsync(cuenta.Id);
        Assert.Equal(0m, pagada.Saldo);
        Assert.Equal(118.30m, pagada.ImportePagado);
        Assert.False(pagada.Vencida);
        Assert.Equal(2, (await HistorialAsync(cuenta.Id)).TotalRegistros);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("-1", 0)]
    [InlineData("1.001", 0)]
    [InlineData("10000000000000000", 0)]
    [InlineData("1", 1)]
    [InlineData("1", -1)]
    [InlineData("1", null)]
    public async Task RechazaImporteFechaFuturaYAnticipo(string importe, int? dias)
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001", fechaDocumento: HoyLima);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{cuenta.Id}/pagos",
            new RegistrarAplicacionPagoRequestDto
            {
                Importe = decimal.Parse(importe, CultureInfo.InvariantCulture),
                FechaPago = dias.HasValue ? HoyLima.AddDays(dias.Value) : null
            });
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
        Assert.Empty((await HistorialAsync(cuenta.Id)).Items);
        Assert.Equal(cuenta.Total, (await ObtenerAsync(cuenta.Id)).Saldo);
    }

    [Fact]
    public async Task ReversionCompletaRestauraSaldoYConservaOtrosPagos()
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001", baseImponible: 100m, impuesto: 18m);
        AplicacionPagoDto pago = await PagarAsync(cuenta.Id, 40m);
        await PagarAsync(cuenta.Id, 20m);
        using HttpResponseMessage respuesta = await RevertirAsync(cuenta.Id, pago.Id, "  Corrección de pago  ");
        AplicacionPagoDto revertido = await LeerAsync<AplicacionPagoDto>(respuesta, HttpStatusCode.OK);
        Assert.Equal(pago.Id, revertido.Id);
        Assert.Equal(pago.Importe, revertido.Importe);
        Assert.Equal(pago.FechaPago, revertido.FechaPago);
        Assert.Equal("Corrección de pago", revertido.MotivoReversion);
        Assert.Equal(DateTimeKind.Utc, revertido.FechaReversion?.Kind);
        Assert.Equal(98m, (await ObtenerAsync(cuenta.Id)).Saldo);
        Assert.Equal(20m, (await ObtenerAsync(cuenta.Id)).ImportePagado);

        using HttpResponseMessage repetida = await RevertirAsync(cuenta.Id, pago.Id, "Otro motivo");
        await ErrorAsync(repetida, HttpStatusCode.Conflict, "El pago ya fue revertido.");
        PaginaAplicacionesPagoDto historial = await HistorialAsync(cuenta.Id);
        Assert.Equal(2, historial.TotalRegistros);
        Assert.Equal(revertido.FechaReversion, Assert.Single(historial.Items, fila => fila.Id == pago.Id).FechaReversion);
    }

    [Fact]
    public async Task NoAnulaConPagoVigenteYConservaSaldo()
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001");
        await PagarAsync(cuenta.Id, 25m);
        using HttpResponseMessage respuesta = await AnularAsync(cuenta.Id, "Factura incorrecta");
        await ErrorAsync(respuesta, HttpStatusCode.Conflict, "No se puede anular una cuenta por pagar con pagos.");
        CuentaPorPagarDetalleDto vigente = await ObtenerAsync(cuenta.Id);
        Assert.False(vigente.Anulada);
        Assert.Equal(75m, vigente.Saldo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnulacionSinPagosVigentesEsDefinitivaYConservaHistoria(bool conPagoRevertido)
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001", vencimiento: HoyLima.AddDays(-1));
        if (conPagoRevertido)
        {
            AplicacionPagoDto pago = await PagarAsync(cuenta.Id, 20m);
            using HttpResponseMessage revertir = await RevertirAsync(cuenta.Id, pago.Id, "Pago erróneo");
            Assert.Equal(HttpStatusCode.OK, revertir.StatusCode);
        }
        using HttpResponseMessage respuesta = await AnularAsync(cuenta.Id, "  Corrección documento  ");
        CuentaPorPagarDetalleDto anulada = await LeerAsync<CuentaPorPagarDetalleDto>(respuesta, HttpStatusCode.OK);
        Assert.True(anulada.Anulada);
        Assert.Equal(0m, anulada.Saldo);
        Assert.Equal(0m, anulada.ImportePagado);
        Assert.Equal(cuenta.Total, anulada.Total);
        Assert.Equal("Corrección documento", anulada.MotivoAnulacion);
        Assert.Equal(DateTimeKind.Utc, anulada.FechaAnulacion?.Kind);
        Assert.False(anulada.Vencida);
        Assert.Equal(conPagoRevertido ? 1 : 0, (await HistorialAsync(cuenta.Id)).TotalRegistros);

        using HttpResponseMessage repetida = await AnularAsync(cuenta.Id, "Otro motivo");
        await ErrorAsync(repetida, HttpStatusCode.Conflict, CuentaAnulada);
        using HttpResponseMessage pagoNuevo = await _cliente.PostAsJsonAsync($"{Ruta}/{cuenta.Id}/pagos", Pago(1m));
        await ErrorAsync(pagoNuevo, HttpStatusCode.Conflict, CuentaAnulada);
        Assert.Equal(anulada.FechaAnulacion, (await ObtenerAsync(cuenta.Id)).FechaAnulacion);
    }

    [Fact]
    public async Task AnulacionYReversionRequierenMotivoValido()
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001");
        AplicacionPagoDto pago = await PagarAsync(cuenta.Id, 20m);
        foreach (string? motivo in new[] { null, " ab ", new string('m', 301) })
        {
            using HttpResponseMessage revertir = await RevertirAsync(cuenta.Id, pago.Id, motivo);
            await ErrorAsync(revertir, HttpStatusCode.BadRequest, "El motivo debe tener entre 3 y 300 caracteres.");
            using HttpResponseMessage anular = await AnularAsync(cuenta.Id, motivo);
            await ErrorAsync(anular, HttpStatusCode.BadRequest, "El motivo debe tener entre 3 y 300 caracteres.");
        }
        Assert.Equal(80m, (await ObtenerAsync(cuenta.Id)).Saldo);
        using HttpResponseMessage valida = await RevertirAsync(cuenta.Id, pago.Id, new string('m', 300));
        Assert.Equal(HttpStatusCode.OK, valida.StatusCode);
        using HttpResponseMessage minima = await AnularAsync(cuenta.Id, "ABC");
        Assert.Equal(HttpStatusCode.OK, minima.StatusCode);
    }

    [Fact]
    public async Task CuentaInexistenteYPagoAjenoDevuelven404()
    {
        CuentaPorPagarDetalleDto origen = await CrearAsync("001");
        CuentaPorPagarDetalleDto otra = await CrearAsync("002");
        AplicacionPagoDto pago = await PagarAsync(origen.Id, 5m);
        using HttpResponseMessage detalle = await _cliente.GetAsync($"{Ruta}/999");
        using HttpResponseMessage historial = await _cliente.GetAsync($"{Ruta}/999/pagos");
        using HttpResponseMessage registrar = await _cliente.PostAsJsonAsync($"{Ruta}/999/pagos", Pago(5m));
        using HttpResponseMessage anular = await AnularAsync(999, "Corrección");
        foreach (HttpResponseMessage respuesta in new[] { detalle, historial, registrar, anular })
        {
            await ErrorAsync(respuesta, HttpStatusCode.NotFound, CuentaInexistente);
        }
        using HttpResponseMessage ajeno = await RevertirAsync(otra.Id, pago.Id, "Corrección");
        await ErrorAsync(ajeno, HttpStatusCode.NotFound, "El pago indicado no existe en la cuenta.");
        using HttpResponseMessage inexistente = await RevertirAsync(origen.Id, 999, "Corrección");
        await ErrorAsync(inexistente, HttpStatusCode.NotFound, "El pago indicado no existe en la cuenta.");
        Assert.Equal(95m, (await ObtenerAsync(origen.Id)).Saldo);
    }

    [Fact]
    public async Task ListadoFiltraYPaginaCuentasYPagos()
    {
        CuentaPorPagarDetalleDto primera = await CrearAsync("001", vencimiento: HoyLima.AddDays(-1));
        CuentaPorPagarDetalleDto segunda = await CrearAsync("002");
        await CrearAsync("003", moneda: "USD", vencimiento: HoyLima.AddDays(-1));
        await CrearAsync("004", proveedor: 2, vencimiento: HoyLima.AddDays(-1));
        PaginaCuentasPorPagarDto pagina = await ListarAsync("?proveedorId=1&moneda=pen&tamanoPagina=1");
        Assert.Equal(2, pagina.TotalRegistros);
        Assert.Equal(2, pagina.TotalPaginas);
        Assert.Equal(segunda.Id, Assert.Single(pagina.Items).Id);
        Assert.Equal(primera.Id, Assert.Single((await ListarAsync("?proveedorId=1&moneda=PEN&soloVencidas=true")).Items).Id);
        Assert.Equal(primera.Id, Assert.Single((await ListarAsync("?proveedorId=1&moneda=PEN&pagina=2&tamanoPagina=1")).Items).Id);
        Assert.Empty((await ListarAsync("?pagina=2147483647&tamanoPagina=100")).Items);

        AplicacionPagoDto primero = await PagarAsync(primera.Id, 10m);
        AplicacionPagoDto segundo = await PagarAsync(primera.Id, 15m);
        PaginaAplicacionesPagoDto historial = await HistorialAsync(primera.Id, "?tamanoPagina=1");
        Assert.Equal(2, historial.TotalRegistros);
        Assert.Equal(segundo.Id, Assert.Single(historial.Items).Id);
        Assert.Equal(primero.Id, Assert.Single((await HistorialAsync(primera.Id, "?tamanoPagina=1&pagina=2")).Items).Id);
    }

    [Fact]
    public async Task ResumenSeparaMonedasYProveedoresYRespetaLimitesDeTramos()
    {
        await CrearAsync("HOY", baseImponible: 10m, vencimiento: HoyLima);
        await CrearAsync("FUTURA", baseImponible: 20m, vencimiento: HoyLima.AddDays(1));
        foreach (int dias in new[] { 1, 30, 31, 60, 61, 90, 91 })
        {
            CuentaPorPagarDetalleDto deuda = await CrearAsync($"DIAS{dias}", baseImponible: dias, vencimiento: HoyLima.AddDays(-dias));
            if (dias == 30)
            {
                await PagarAsync(deuda.Id, 5m);
            }
        }
        CuentaPorPagarDetalleDto pagada = await CrearAsync("PAGADA", vencimiento: HoyLima.AddDays(-100));
        await PagarAsync(pagada.Id, pagada.Total);
        CuentaPorPagarDetalleDto anulada = await CrearAsync("ANULADA", vencimiento: HoyLima.AddDays(-100));
        using HttpResponseMessage anular = await AnularAsync(anulada.Id, "Factura duplicada");
        Assert.Equal(HttpStatusCode.OK, anular.StatusCode);
        await CrearAsync("USD", moneda: "USD", baseImponible: 99m, vencimiento: HoyLima.AddDays(-31));
        await CrearAsync("OTRO", proveedor: 2, baseImponible: 22m, vencimiento: HoyLima);

        ResumenCuentasPorPagarDto resumen = await ResumenAsync();
        Assert.Equal(HoyLima, resumen.FechaCorte);
        Assert.Equal(3, resumen.Grupos.Count);
        ResumenProveedorMonedaDto pen = Assert.Single(resumen.Grupos, fila => fila.ProveedorId == 1 && fila.Moneda == "PEN");
        Assert.Equal(30m, pen.SaldoAlDia);
        Assert.Equal(26m, pen.Vencido0A30);
        Assert.Equal(91m, pen.Vencido31A60);
        Assert.Equal(151m, pen.Vencido61A90);
        Assert.Equal(91m, pen.VencidoMasDe90);
        Assert.Equal(359m, pen.SaldoVencido);
        ResumenProveedorMonedaDto usd = Assert.Single((await ResumenAsync("?proveedorId=1&moneda=usd")).Grupos);
        Assert.Equal("USD", usd.Moneda);
        Assert.Equal(99m, usd.Vencido31A60);
        Assert.Equal(22m, Assert.Single(resumen.Grupos, fila => fila.ProveedorId == 2).SaldoAlDia);
    }

    [Fact]
    public async Task CorteCambiaALaMedianocheDeLima()
    {
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("HOY", vencimiento: HoyLima);
        Assert.False(cuenta.Vencida);
        Assert.Equal(HoyLima, (await ResumenAsync()).FechaCorte);

        _factory.Reloj.Instante = _factory.Reloj.Instante.AddSeconds(1);
        ResumenCuentasPorPagarDto posterior = await ResumenAsync();
        Assert.Equal(HoyLima.AddDays(1), posterior.FechaCorte);
        Assert.Equal(cuenta.Total, Assert.Single(posterior.Grupos).Vencido0A30);
        Assert.True((await ObtenerAsync(cuenta.Id)).Vencida);
    }

    [Theory]
    [InlineData("?pagina=0")]
    [InlineData("?moneda=EUR")]
    [InlineData("?proveedorId=0")]
    [InlineData("/resumen?moneda=EUR")]
    [InlineData("/1/pagos?tamanoPagina=101")]
    public async Task FiltrosInvalidosDevuelven400(string sufijo)
    {
        await CrearAsync("001");
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + sufijo);
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("GET", "/1")]
    [InlineData("GET", "/1/pagos")]
    [InlineData("GET", "/resumen")]
    [InlineData("POST", "")]
    [InlineData("POST", "/1/pagos")]
    [InlineData("POST", "/1/pagos/1/revertir")]
    [InlineData("POST", "/1/anular")]
    public async Task TodasLasRutasExigenSesion(string metodo, string sufijo)
    {
        using HttpClient anonimo = _factory.CreateClient();
        using HttpRequestMessage peticion = Peticion(metodo, sufijo);
        using HttpResponseMessage respuesta = await anonimo.SendAsync(peticion);
        await ErrorAsync(respuesta, HttpStatusCode.Unauthorized, "Debe iniciar sesión para acceder a este recurso.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("/1/pagos")]
    [InlineData("/1/pagos/1/revertir")]
    [InlineData("/1/anular")]
    public async Task UsuarioDeConsultaPuedeLeerPeroNoEscribir(string sufijo)
    {
        using HttpClient consulta = _factory.CreateClient();
        consulta.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_factory.Services));
        using HttpResponseMessage lectura = await consulta.GetAsync(Ruta + "/resumen");
        Assert.Equal(HttpStatusCode.OK, lectura.StatusCode);
        using HttpRequestMessage peticion = Peticion("POST", sufijo);
        using HttpResponseMessage respuesta = await consulta.SendAsync(peticion);
        await ErrorAsync(respuesta, HttpStatusCode.Forbidden, "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.");
    }

    [Fact]
    public async Task FinanzasPuedeRegistrarPagarRevertirYAnular()
    {
        _cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_factory.Services, "FINANZAS"));
        CuentaPorPagarDetalleDto cuenta = await CrearAsync("001");
        AplicacionPagoDto pago = await PagarAsync(cuenta.Id, 50m);
        using HttpResponseMessage reversion = await RevertirAsync(cuenta.Id, pago.Id, "Pago equivocado");
        using HttpResponseMessage anulacion = await AnularAsync(cuenta.Id, "Cuenta duplicada");
        Assert.Equal(HttpStatusCode.OK, reversion.StatusCode);
        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
    }

    [Fact]
    public async Task AltaSinCuerpoConservaElMensajeDelPipeline()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsync(Ruta, null);
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, "El cuerpo de la petición (Body) no puede estar vacío.");
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _factory.Dispose();
    }

    private static RegistrarCuentaPorPagarRequestDto Solicitud(string? numero, string? serie = "F54", int proveedor = 1,
        int? orden = null, string moneda = "PEN", decimal? baseImponible = 100m, decimal? impuesto = 0m,
        DateOnly? fechaDocumento = null, DateOnly? vencimiento = null)
    {
        return new RegistrarCuentaPorPagarRequestDto
        {
            ProveedorId = proveedor,
            OrdenAbastecimientoId = orden,
            Serie = serie,
            NumeroDocumento = numero,
            Moneda = moneda,
            BaseImponible = baseImponible,
            ImporteImpuesto = impuesto,
            FechaDocumento = fechaDocumento ?? HoyLima.AddDays(-100),
            FechaVencimiento = vencimiento ?? HoyLima.AddDays(1)
        };
    }

    private static RegistrarAplicacionPagoRequestDto Pago(decimal importe)
    {
        return new RegistrarAplicacionPagoRequestDto { Importe = importe, FechaPago = HoyLima };
    }

    private static HttpRequestMessage Peticion(string metodo, string sufijo)
    {
        HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + sufijo);
        if (metodo == "POST")
        {
            peticion.Content = JsonContent.Create(new { });
        }

        return peticion;
    }

    private async Task<CuentaPorPagarDetalleDto> CrearAsync(string numero, int proveedor = 1, string moneda = "PEN",
        decimal baseImponible = 100m, decimal impuesto = 0m, DateOnly? fechaDocumento = null, DateOnly? vencimiento = null)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, Solicitud(numero, proveedor: proveedor,
            moneda: moneda, baseImponible: baseImponible, impuesto: impuesto, fechaDocumento: fechaDocumento, vencimiento: vencimiento));
        return await LeerAsync<CuentaPorPagarDetalleDto>(respuesta, HttpStatusCode.Created);
    }

    private async Task<AplicacionPagoDto> PagarAsync(int id, decimal importe)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{id}/pagos", Pago(importe));
        return await LeerAsync<AplicacionPagoDto>(respuesta, HttpStatusCode.Created);
    }

    private Task<HttpResponseMessage> RevertirAsync(int id, int pagoId, string? motivo)
    {
        return _cliente.PostAsJsonAsync($"{Ruta}/{id}/pagos/{pagoId}/revertir", new RevertirAplicacionPagoRequestDto { Motivo = motivo });
    }

    private Task<HttpResponseMessage> AnularAsync(int id, string? motivo)
    {
        return _cliente.PostAsJsonAsync($"{Ruta}/{id}/anular", new AnularCuentaPorPagarRequestDto { Motivo = motivo });
    }

    private async Task<CuentaPorPagarDetalleDto> ObtenerAsync(int id)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/{id}");
        return await LeerAsync<CuentaPorPagarDetalleDto>(respuesta, HttpStatusCode.OK);
    }

    private async Task<PaginaCuentasPorPagarDto> ListarAsync(string filtro = "")
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + filtro);
        return await LeerAsync<PaginaCuentasPorPagarDto>(respuesta, HttpStatusCode.OK);
    }

    private async Task<PaginaAplicacionesPagoDto> HistorialAsync(int id, string filtro = "")
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/{id}/pagos{filtro}");
        return await LeerAsync<PaginaAplicacionesPagoDto>(respuesta, HttpStatusCode.OK);
    }

    private async Task<ResumenCuentasPorPagarDto> ResumenAsync(string filtro = "")
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + "/resumen" + filtro);
        return await LeerAsync<ResumenCuentasPorPagarDto>(respuesta, HttpStatusCode.OK);
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta, HttpStatusCode esperado) where T : class
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        return Assert.IsType<T>(await respuesta.Content.ReadFromJsonAsync<T>());
    }

    private static async Task ErrorAsync(HttpResponseMessage respuesta, HttpStatusCode esperado, string mensaje)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal((int)esperado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}

public sealed class CuentasPorPagarApiFactory : WebApplicationFactory<Program>
{
    private readonly string _baseDatos = "CuentasPorPagarTests-" + Guid.NewGuid();

    internal RelojCuentasPorPagar Reloj { get; } = new RelojCuentasPorPagar();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            servicios.AddDbContext<TitanGlobalDbContext>(opciones => opciones.UseInMemoryDatabase(_baseDatos));
            servicios.RemoveAll<TimeProvider>();
            servicios.AddSingleton<TimeProvider>(Reloj);
        });
    }

    internal void SemillaProveedoresYOrdenes()
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Proveedores.AddRange(
            new Proveedor { Id = 1, RazonSocial = "Proveedor uno", Ruc = "20540000001", Pais = "Perú", Activo = true },
            new Proveedor { Id = 2, RazonSocial = "Proveedor dos", Ruc = "20540000002", Pais = "Perú", Activo = true },
            new Proveedor { Id = 3, RazonSocial = "Proveedor inactivo", Ruc = "20540000003", Pais = "Perú", Activo = false });
        contexto.OrdenesAbastecimiento.AddRange(
            new OrdenAbastecimiento { Id = 1, Numero = "OA-TEST-1", ProveedorId = 1, Estado = "Emitida" },
            new OrdenAbastecimiento { Id = 2, Numero = "OA-TEST-2", ProveedorId = 2, Estado = "Recibida" });
        contexto.SaveChanges();
    }
}

internal sealed class RelojCuentasPorPagar : TimeProvider
{
    public DateTimeOffset Instante { get; set; } = new DateTimeOffset(2026, 11, 25, 4, 59, 59, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow()
    {
        return Instante;
    }
}
