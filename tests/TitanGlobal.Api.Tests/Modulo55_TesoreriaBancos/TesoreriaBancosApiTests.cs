using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo55_TesoreriaBancos;

public class TesoreriaBancosApiTests : IDisposable
{
    private const string Ruta = "/api/v1/tesoreria-bancos";
    private const string Cuentas = Ruta + "/cuentas";
    private const string Transferencias = Ruta + "/transferencias";
    private const string DatosInvalidos = "Los datos de tesorería no son válidos.";
    private const string CuentaInexistente = "La cuenta bancaria no existe.";
    private const string SaldoInsuficiente = "La cuenta bancaria no tiene saldo suficiente.";
    private const string MonedaInmutable = "No se puede cambiar la moneda de una cuenta con movimientos.";
    private readonly TesoreriaBancosApiFactory _factory = new TesoreriaBancosApiFactory();
    private readonly HttpClient _cliente;

    public TesoreriaBancosApiTests()
    {
        _cliente = _factory.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, _factory.Services);
    }

    [Theory]
    [InlineData(" corriente ", " pen ", "Corriente", "PEN")]
    [InlineData("ahorros", "usd", "Ahorros", "USD")]
    public async Task AltaPreservaCerosYEmpiezaConSaldoCero(string tipo, string moneda, string tipoEsperado, string monedaEsperada)
    {
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(Cuentas, SolicitudCuenta(" 001-A ", moneda, " Banco ñ ", tipo));
        CuentaBancariaDetalleDto cuenta = await LeerAsync<CuentaBancariaDetalleDto>(alta, HttpStatusCode.Created);
        Assert.Equal("BANCO Ñ", cuenta.Banco);
        Assert.Equal("001-A", cuenta.Numero);
        Assert.Equal(tipoEsperado, cuenta.Tipo);
        Assert.Equal(monedaEsperada, cuenta.Moneda);
        Assert.Equal(0m, cuenta.Saldo);
        Assert.Equal(DateTimeKind.Utc, cuenta.FechaRegistro.Kind);
        Uri ubicacion = Assert.IsType<Uri>(alta.Headers.Location);
        Assert.Equal($"{Cuentas}/{cuenta.Id}", ubicacion.AbsolutePath);
        using HttpResponseMessage lectura = await _cliente.GetAsync(ubicacion);
        Assert.Equal(cuenta.Id, (await LeerAsync<CuentaBancariaDetalleDto>(lectura, HttpStatusCode.OK)).Id);
    }

    [Fact]
    public async Task UnicidadPorBancoYNumeroCanonicosEnAltaYEdicion()
    {
        CuentaBancariaDetalleDto original = await CrearCuentaAsync("0001", banco: "Banco 55");
        CuentaBancariaDetalleDto otroBanco = await CrearCuentaAsync("0001", banco: "Banco alterno");
        using HttpResponseMessage duplicado = await _cliente.PostAsJsonAsync(Cuentas, SolicitudCuenta(" 0001 ", banco: " banco 55 "));
        await ErrorAsync(duplicado, HttpStatusCode.Conflict, "La cuenta ya está registrada para ese banco y número.");
        using HttpResponseMessage edicion = await _cliente.PutAsJsonAsync($"{Cuentas}/{otroBanco.Id}", SolicitudCuenta("0001", banco: original.Banco));
        await ErrorAsync(edicion, HttpStatusCode.Conflict, "La cuenta ya está registrada para ese banco y número.");
        Assert.Equal("BANCO ALTERNO", (await CuentaAsync(otroBanco.Id)).Banco);
    }

    [Theory]
    [InlineData("banco", null)]
    [InlineData("numero", "   ")]
    [InlineData("numero", "00000000000000000000000000000000000000000")]
    [InlineData("nombre", "")]
    [InlineData("tipo", "Plazo")]
    [InlineData("moneda", "EUR")]
    public async Task AltaValidaDatosTipoYMoneda(string campo, string? valor)
    {
        GuardarCuentaBancariaRequestDto solicitud = new GuardarCuentaBancariaRequestDto
        {
            Banco = campo == "banco" ? valor : "Banco 55",
            Numero = campo == "numero" ? valor : "0001",
            Nombre = campo == "nombre" ? valor : "Cuenta de prueba",
            Tipo = campo == "tipo" ? valor : "Corriente",
            Moneda = campo == "moneda" ? valor : "PEN"
        };
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Cuentas, solicitud);
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
        Assert.Empty((await PaginaCuentasAsync()).Items);
    }

    [Fact]
    public async Task MonedaEditableHastaElPrimerMovimientoInclusoSiSeRevierte()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        using HttpResponseMessage cambio = await _cliente.PutAsJsonAsync($"{Cuentas}/{cuenta.Id}", SolicitudCuenta("002", "USD", "Otro banco", "Ahorros"));
        CuentaBancariaDetalleDto editada = await LeerAsync<CuentaBancariaDetalleDto>(cambio, HttpStatusCode.OK);
        Assert.Equal("002", editada.Numero);
        Assert.Equal("OTRO BANCO", editada.Banco);
        Assert.Equal("Ahorros", editada.Tipo);
        Assert.Equal("USD", editada.Moneda);
        Assert.Equal(cuenta.FechaRegistro, editada.FechaRegistro);

        MovimientoBancarioDto movimiento = await CrearMovimientoAsync(cuenta.Id, "Ingreso", 100m);
        Assert.Equal("USD", movimiento.Moneda);
        using HttpResponseMessage antes = await _cliente.PutAsJsonAsync($"{Cuentas}/{cuenta.Id}", SolicitudCuenta("002", "PEN", "Otro banco"));
        await ErrorAsync(antes, HttpStatusCode.Conflict, MonedaInmutable);
        using HttpResponseMessage reversion = await RevertirAsync(cuenta.Id, movimiento.Id, "Corrección de apertura");
        Assert.Equal(HttpStatusCode.OK, reversion.StatusCode);
        using HttpResponseMessage despues = await _cliente.PutAsJsonAsync($"{Cuentas}/{cuenta.Id}", SolicitudCuenta("002", "PEN", "Otro banco"));
        await ErrorAsync(despues, HttpStatusCode.Conflict, MonedaInmutable);
        using HttpResponseMessage mismo = await _cliente.PutAsJsonAsync($"{Cuentas}/{cuenta.Id}", SolicitudCuenta("003", "USD", "Otro banco"));
        Assert.Equal(HttpStatusCode.OK, mismo.StatusCode);
        Assert.Equal(0m, (await CuentaAsync(cuenta.Id)).Saldo);
    }

    [Fact]
    public async Task IngresosYEgresosCalculanSaldoExactoYAdmitenAgotarlo()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        MovimientoBancarioDto ingreso = await CrearMovimientoAsync(cuenta.Id, " ingreso ", 100.45m, referencia: "  Apertura  ");
        Assert.Equal("Ingreso", ingreso.Tipo);
        Assert.Equal("Apertura", ingreso.Referencia);
        Assert.Equal("PEN", ingreso.Moneda);
        Assert.Null(ingreso.TransferenciaBancariaId);
        MovimientoBancarioDto egreso = await CrearMovimientoAsync(cuenta.Id, "Egreso", 40.25m);
        Assert.Null(egreso.Referencia);
        Assert.Equal(60.20m, (await CuentaAsync(cuenta.Id)).Saldo);
        using HttpResponseMessage insuficiente = await _cliente.PostAsJsonAsync(RutaMovimientos(cuenta.Id), Movimiento("Egreso", 60.21m));
        await ErrorAsync(insuficiente, HttpStatusCode.Conflict, SaldoInsuficiente);
        Assert.Equal(60.20m, (await CuentaAsync(cuenta.Id)).Saldo);
        Assert.Equal(2, (await HistorialAsync(cuenta.Id)).TotalRegistros);
        await CrearMovimientoAsync(cuenta.Id, "Egreso", 60.20m);
        Assert.Equal(0m, (await CuentaAsync(cuenta.Id)).Saldo);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("1.001")]
    [InlineData("10000000000000000")]
    public async Task RechazaImporteFueraDePrecisionORango(string valor)
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        decimal importe = decimal.Parse(valor, CultureInfo.InvariantCulture);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaMovimientos(cuenta.Id), Movimiento("Ingreso", importe));
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
        Assert.Empty((await HistorialAsync(cuenta.Id)).Items);
    }

    [Fact]
    public async Task ValidaFechaTipoReferenciaYFormatoNumerico()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        RegistrarMovimientoBancarioRequestDto[] invalidos =
        [
            Movimiento("Ingreso", 1m, Hoy().AddDays(1)),
            new RegistrarMovimientoBancarioRequestDto { Tipo = "Ingreso", Importe = 1m },
            Movimiento("Transferencia", 1m),
            Movimiento("Ingreso", 1m, referencia: new string('R', 101))
        ];
        foreach (RegistrarMovimientoBancarioRequestDto invalido in invalidos)
        {
            using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaMovimientos(cuenta.Id), invalido);
            await ErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
        }

        using StringContent malformado = new StringContent(
            "{\"tipo\":\"Ingreso\",\"importe\":\"NaN\",\"fechaOperacion\":\"2026-01-01\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage invalidoJson = await _cliente.PostAsync(RutaMovimientos(cuenta.Id), malformado);
        Assert.Equal(HttpStatusCode.BadRequest, invalidoJson.StatusCode);
        Assert.Empty((await HistorialAsync(cuenta.Id)).Items);
    }

    [Fact]
    public async Task TransferenciaVinculaDosMovimientosYDevuelveUbicacion()
    {
        CuentaBancariaDetalleDto origen = await CrearCuentaAsync("001");
        CuentaBancariaDetalleDto destino = await CrearCuentaAsync("002");
        await CrearMovimientoAsync(origen.Id, "Ingreso", 100m);
        await CrearMovimientoAsync(destino.Id, "Ingreso", 20m);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, destino.Id, 30m));
        TransferenciaBancariaDto creada = await LeerAsync<TransferenciaBancariaDto>(respuesta, HttpStatusCode.Created);
        Assert.Equal("PEN", creada.Moneda);
        Assert.Equal(30m, creada.Importe);
        Assert.Equal(70m, (await CuentaAsync(origen.Id)).Saldo);
        Assert.Equal(50m, (await CuentaAsync(destino.Id)).Saldo);

        Uri ubicacion = Assert.IsType<Uri>(respuesta.Headers.Location);
        Assert.Equal($"{Transferencias}/{creada.Id}", ubicacion.AbsolutePath);
        using HttpResponseMessage detalle = await _cliente.GetAsync(ubicacion);
        TransferenciaBancariaDto obtenida = await LeerAsync<TransferenciaBancariaDto>(detalle, HttpStatusCode.OK);
        Assert.Equal(creada.MovimientoEgresoId, obtenida.MovimientoEgresoId);
        Assert.Equal(creada.MovimientoIngresoId, obtenida.MovimientoIngresoId);
        MovimientoBancarioDto egreso = Assert.Single((await HistorialAsync(origen.Id, "?tipo=Egreso")).Items);
        MovimientoBancarioDto ingreso = Assert.Single((await HistorialAsync(destino.Id)).Items, item => item.TransferenciaBancariaId == creada.Id);
        Assert.Equal(creada.MovimientoEgresoId, egreso.Id);
        Assert.Equal(creada.Id, egreso.TransferenciaBancariaId);
        Assert.Equal(creada.MovimientoIngresoId, ingreso.Id);
        Assert.Equal("Ingreso", ingreso.Tipo);
        Assert.Equal(egreso.Importe, ingreso.Importe);
        Assert.Equal("Traslado interno", ingreso.Referencia);
        Assert.Equal(creada.FechaOperacion, ingreso.FechaOperacion);
    }

    [Fact]
    public async Task TransferenciasInvalidasConservanAmbosSaldosYElHistorial()
    {
        CuentaBancariaDetalleDto origen = await CrearCuentaAsync("001");
        CuentaBancariaDetalleDto destino = await CrearCuentaAsync("002");
        CuentaBancariaDetalleDto cuentaUsd = await CrearCuentaAsync("003", "USD");
        await CrearMovimientoAsync(origen.Id, "Ingreso", 100m);

        using HttpResponseMessage misma = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, origen.Id, 1m));
        await ErrorAsync(misma, HttpStatusCode.BadRequest, "Las cuentas de origen y destino deben ser distintas.");
        using HttpResponseMessage monedas = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, cuentaUsd.Id, 1m));
        await ErrorAsync(monedas, HttpStatusCode.Conflict, "Las cuentas de la transferencia deben tener la misma moneda.");
        using HttpResponseMessage insuficiente = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, destino.Id, 100.01m));
        await ErrorAsync(insuficiente, HttpStatusCode.Conflict, SaldoInsuficiente);
        using HttpResponseMessage fecha = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, destino.Id, 1m, Hoy().AddDays(1)));
        await ErrorAsync(fecha, HttpStatusCode.BadRequest, DatosInvalidos);
        using HttpResponseMessage importe = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, destino.Id, 1.001m));
        await ErrorAsync(importe, HttpStatusCode.BadRequest, DatosInvalidos);
        using HttpResponseMessage faltaCuenta = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, 999, 1m));
        await ErrorAsync(faltaCuenta, HttpStatusCode.NotFound, CuentaInexistente);
        Assert.Equal(100m, (await CuentaAsync(origen.Id)).Saldo);
        Assert.Equal(0m, (await CuentaAsync(destino.Id)).Saldo);
        Assert.Single((await HistorialAsync(origen.Id)).Items);
        Assert.Empty((await HistorialAsync(destino.Id)).Items);
    }

    [Fact]
    public async Task RevertirEgresoRecuperaSaldoSinBorrarLaFilaYReintentoDevuelve409()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        await CrearMovimientoAsync(cuenta.Id, "Ingreso", 100m);
        MovimientoBancarioDto egreso = await CrearMovimientoAsync(cuenta.Id, "Egreso", 25m, referencia: "Ajuste");
        using HttpResponseMessage baja = await RevertirAsync(cuenta.Id, egreso.Id, "  Corrección  ");
        MovimientoBancarioDto revertido = await LeerAsync<MovimientoBancarioDto>(baja, HttpStatusCode.OK);
        Assert.Equal(egreso.Id, revertido.Id);
        Assert.Equal(25m, revertido.Importe);
        Assert.Equal("Ajuste", revertido.Referencia);
        Assert.Equal("Corrección", revertido.MotivoReversion);
        Assert.Equal(DateTimeKind.Utc, revertido.FechaReversion?.Kind);
        Assert.Equal(100m, (await CuentaAsync(cuenta.Id)).Saldo);
        using HttpResponseMessage repetida = await RevertirAsync(cuenta.Id, egreso.Id, "Otro motivo");
        await ErrorAsync(repetida, HttpStatusCode.Conflict, "El movimiento bancario ya fue revertido.");
        MovimientoBancarioDto historico = Assert.Single((await HistorialAsync(cuenta.Id, "?tipo=Egreso")).Items);
        Assert.Equal(revertido.FechaReversion, historico.FechaReversion);
        Assert.Equal(revertido.MotivoReversion, historico.MotivoReversion);
    }

    [Fact]
    public async Task ReversionDeIngresoCompruebaSaldoYConservaHistoria()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        MovimientoBancarioDto ingreso = await CrearMovimientoAsync(cuenta.Id, "Ingreso", 100m);
        await CrearMovimientoAsync(cuenta.Id, "Egreso", 80m);
        using HttpResponseMessage insuficiente = await RevertirAsync(cuenta.Id, ingreso.Id, "Corrección");
        await ErrorAsync(insuficiente, HttpStatusCode.Conflict, SaldoInsuficiente);
        Assert.Equal(20m, (await CuentaAsync(cuenta.Id)).Saldo);
        Assert.Null(Assert.Single((await HistorialAsync(cuenta.Id, "?tipo=Ingreso")).Items).FechaReversion);

        await CrearMovimientoAsync(cuenta.Id, "Ingreso", 80m);
        using HttpResponseMessage valida = await RevertirAsync(cuenta.Id, ingreso.Id, "Corrección");
        Assert.Equal(HttpStatusCode.OK, valida.StatusCode);
        Assert.Equal(0m, (await CuentaAsync(cuenta.Id)).Saldo);
        Assert.Equal(3, (await HistorialAsync(cuenta.Id)).TotalRegistros);
    }

    [Fact]
    public async Task ReversionValidaMotivoRequeridoYLongitud()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        MovimientoBancarioDto ingreso = await CrearMovimientoAsync(cuenta.Id, "Ingreso", 100m);
        foreach (string? motivo in new[] { null, " ab ", new string('m', 301) })
        {
            using HttpResponseMessage respuesta = await RevertirAsync(cuenta.Id, ingreso.Id, motivo);
            await ErrorAsync(respuesta, HttpStatusCode.BadRequest, "El motivo debe tener entre 3 y 300 caracteres.");
        }
        Assert.Equal(100m, (await CuentaAsync(cuenta.Id)).Saldo);
        using HttpResponseMessage maximo = await RevertirAsync(cuenta.Id, ingreso.Id, new string('m', 300));
        Assert.Equal(HttpStatusCode.OK, maximo.StatusCode);
    }

    [Fact]
    public async Task ReversionIndividualDeTransferenciaPreservaLasDosPatas()
    {
        CuentaBancariaDetalleDto origen = await CrearCuentaAsync("001");
        CuentaBancariaDetalleDto destino = await CrearCuentaAsync("002");
        await CrearMovimientoAsync(origen.Id, "Ingreso", 100m);
        using HttpResponseMessage creada = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, destino.Id, 30m));
        TransferenciaBancariaDto transferencia = await LeerAsync<TransferenciaBancariaDto>(creada, HttpStatusCode.Created);
        using HttpResponseMessage egreso = await RevertirAsync(origen.Id, transferencia.MovimientoEgresoId, "Corrección");
        using HttpResponseMessage ingreso = await RevertirAsync(destino.Id, transferencia.MovimientoIngresoId, "Corrección");
        foreach (HttpResponseMessage respuesta in new[] { egreso, ingreso })
        {
            await ErrorAsync(respuesta, HttpStatusCode.Conflict, "No se puede revertir por separado un movimiento de transferencia.");
        }
        Assert.Equal(70m, (await CuentaAsync(origen.Id)).Saldo);
        Assert.Equal(30m, (await CuentaAsync(destino.Id)).Saldo);
        Assert.Null(Assert.Single((await HistorialAsync(destino.Id)).Items).FechaReversion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GuardaDeConciliacionAplicaTambienAMovimientosDeTransferencia(bool esTransferencia)
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        int id = (await CrearMovimientoAsync(cuenta.Id, "Ingreso", 100m)).Id;
        if (esTransferencia)
        {
            CuentaBancariaDetalleDto destino = await CrearCuentaAsync("002");
            using HttpResponseMessage creada = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(cuenta.Id, destino.Id, 30m));
            id = (await LeerAsync<TransferenciaBancariaDto>(creada, HttpStatusCode.Created)).MovimientoEgresoId;
        }
        _factory.MovimientoConciliadoId = id;
        decimal saldo = (await CuentaAsync(cuenta.Id)).Saldo;

        using HttpResponseMessage respuesta = await RevertirAsync(cuenta.Id, id, "Corrección");
        await ErrorAsync(respuesta, HttpStatusCode.Conflict, "No se puede revertir un movimiento conciliado.");
        Assert.Equal(saldo, (await CuentaAsync(cuenta.Id)).Saldo);
        Assert.Null(Assert.Single((await HistorialAsync(cuenta.Id)).Items, item => item.Id == id).FechaReversion);
    }

    [Fact]
    public async Task RecursosInexistentesYMovimientoAjenoDevuelven404()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("001");
        CuentaBancariaDetalleDto otra = await CrearCuentaAsync("002");
        MovimientoBancarioDto ingreso = await CrearMovimientoAsync(cuenta.Id, "Ingreso", 5m);
        using HttpResponseMessage inexistente = await _cliente.GetAsync($"{Cuentas}/999");
        await ErrorAsync(inexistente, HttpStatusCode.NotFound, CuentaInexistente);
        using HttpResponseMessage listado = await _cliente.GetAsync(RutaMovimientos(999));
        await ErrorAsync(listado, HttpStatusCode.NotFound, CuentaInexistente);
        using HttpResponseMessage registro = await _cliente.PostAsJsonAsync(RutaMovimientos(999), Movimiento("Ingreso", 5m));
        await ErrorAsync(registro, HttpStatusCode.NotFound, CuentaInexistente);
        using HttpResponseMessage ajeno = await RevertirAsync(otra.Id, ingreso.Id, "Corrección");
        await ErrorAsync(ajeno, HttpStatusCode.NotFound, "El movimiento bancario no existe en la cuenta.");
        using HttpResponseMessage transferencia = await _cliente.GetAsync($"{Transferencias}/999");
        await ErrorAsync(transferencia, HttpStatusCode.NotFound, "La transferencia bancaria no existe.");
        Assert.Equal(5m, (await CuentaAsync(cuenta.Id)).Saldo);
    }

    [Fact]
    public async Task FiltrosYListadosPaginanEnElOrdenDelContrato()
    {
        CuentaBancariaDetalleDto cuenta = await CrearCuentaAsync("002");
        await CrearCuentaAsync("001");
        await CrearCuentaAsync("003", "USD");
        PaginaCuentasBancariasDto pagina = await PaginaCuentasAsync("?moneda=pen&tipo=corriente&tamanoPagina=1&pagina=2");
        Assert.Equal(cuenta.Id, Assert.Single(pagina.Items).Id);
        Assert.Equal(2, pagina.TotalRegistros);
        Assert.Equal(2, pagina.TotalPaginas);
        Assert.Equal(2, pagina.Pagina);
        Assert.Single((await PaginaCuentasAsync("?moneda=USD")).Items);

        DateOnly hoy = Hoy();
        await CrearMovimientoAsync(cuenta.Id, "Ingreso", 10m, hoy.AddDays(-1));
        MovimientoBancarioDto anterior = await CrearMovimientoAsync(cuenta.Id, "Ingreso", 30m, hoy);
        MovimientoBancarioDto reciente = await CrearMovimientoAsync(cuenta.Id, "Ingreso", 20m, hoy);
        await CrearMovimientoAsync(cuenta.Id, "Egreso", 2m, hoy);
        string filtro = $"?tipo=ingreso&desde={hoy:yyyy-MM-dd}&hasta={hoy:yyyy-MM-dd}&tamanoPagina=1";
        PaginaMovimientosBancariosDto primera = await HistorialAsync(cuenta.Id, filtro);
        Assert.Equal(reciente.Id, Assert.Single(primera.Items).Id);
        Assert.Equal(2, primera.TotalRegistros);
        Assert.Equal(2, primera.TotalPaginas);
        Assert.Equal(anterior.Id, Assert.Single((await HistorialAsync(cuenta.Id, filtro + "&pagina=2")).Items).Id);
        Assert.Empty((await HistorialAsync(cuenta.Id, "?pagina=2147483647&tamanoPagina=100")).Items);
    }

    [Theory]
    [InlineData("/cuentas?pagina=0")]
    [InlineData("/cuentas?moneda=EUR")]
    [InlineData("/cuentas?tipo=Plazo")]
    [InlineData("/cuentas/1/movimientos?tipo=Otro")]
    [InlineData("/cuentas/1/movimientos?tamanoPagina=101")]
    [InlineData("/cuentas/1/movimientos?desde=2024-02-01&hasta=2024-01-01")]
    public async Task FiltrosInvalidosDevuelven400(string sufijo)
    {
        await CrearCuentaAsync("001");
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + sufijo);
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, DatosInvalidos);
    }

    [Theory]
    [InlineData("GET", "/cuentas")]
    [InlineData("GET", "/cuentas/1")]
    [InlineData("GET", "/cuentas/1/movimientos")]
    [InlineData("GET", "/transferencias/1")]
    [InlineData("POST", "/cuentas")]
    [InlineData("PUT", "/cuentas/1")]
    [InlineData("POST", "/cuentas/1/movimientos")]
    [InlineData("POST", "/cuentas/1/movimientos/1/revertir")]
    [InlineData("POST", "/transferencias")]
    public async Task TodasLasRutasExigenSesion(string metodo, string sufijo)
    {
        using HttpClient anonimo = _factory.CreateClient();
        using HttpRequestMessage peticion = Peticion(metodo, sufijo);
        using HttpResponseMessage respuesta = await anonimo.SendAsync(peticion);
        await ErrorAsync(respuesta, HttpStatusCode.Unauthorized, "Debe iniciar sesión para acceder a este recurso.");
    }

    [Theory]
    [InlineData("POST", "/cuentas")]
    [InlineData("PUT", "/cuentas/1")]
    [InlineData("POST", "/cuentas/1/movimientos")]
    [InlineData("POST", "/cuentas/1/movimientos/1/revertir")]
    [InlineData("POST", "/transferencias")]
    public async Task UsuarioSinRolFinancieroPuedeLeerPeroNoEscribir(string metodo, string sufijo)
    {
        using HttpClient consulta = _factory.CreateClient();
        consulta.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_factory.Services));
        using HttpResponseMessage lectura = await consulta.GetAsync(Cuentas);
        Assert.Equal(HttpStatusCode.OK, lectura.StatusCode);
        using HttpRequestMessage peticion = Peticion(metodo, sufijo);
        using HttpResponseMessage respuesta = await consulta.SendAsync(peticion);
        await ErrorAsync(respuesta, HttpStatusCode.Forbidden, "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.");
    }

    [Fact]
    public async Task FinanzasPuedeEjecutarLasOperacionesDeEscritura()
    {
        _cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_factory.Services, "FINANZAS"));
        CuentaBancariaDetalleDto origen = await CrearCuentaAsync("001");
        CuentaBancariaDetalleDto destino = await CrearCuentaAsync("002");
        using HttpResponseMessage cambio = await _cliente.PutAsJsonAsync($"{Cuentas}/{origen.Id}", SolicitudCuenta("003"));
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        MovimientoBancarioDto ingreso = await CrearMovimientoAsync(origen.Id, "Ingreso", 100m);
        using HttpResponseMessage reversion = await RevertirAsync(origen.Id, ingreso.Id, "Apertura corregida");
        Assert.Equal(HttpStatusCode.OK, reversion.StatusCode);
        await CrearMovimientoAsync(origen.Id, "Ingreso", 100m);
        using HttpResponseMessage transferencia = await _cliente.PostAsJsonAsync(Transferencias, Transferencia(origen.Id, destino.Id, 30m));
        Assert.Equal(HttpStatusCode.Created, transferencia.StatusCode);
    }

    [Fact]
    public async Task EscrituraSinCuerpoConservaLiteralDelPipeline()
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsync(Transferencias, null);
        await ErrorAsync(respuesta, HttpStatusCode.BadRequest, "El cuerpo de la petición (Body) no puede estar vacío.");
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _factory.Dispose();
    }

    private static GuardarCuentaBancariaRequestDto SolicitudCuenta(string numero, string moneda = "PEN", string banco = "Banco 55", string tipo = "Corriente")
    {
        return new GuardarCuentaBancariaRequestDto { Banco = banco, Numero = numero, Nombre = "Cuenta de prueba", Moneda = moneda, Tipo = tipo };
    }

    private static RegistrarMovimientoBancarioRequestDto Movimiento(string tipo, decimal importe, DateOnly? fecha = null, string? referencia = null)
    {
        return new RegistrarMovimientoBancarioRequestDto { Tipo = tipo, Importe = importe, FechaOperacion = fecha ?? Hoy(), Referencia = referencia };
    }

    private static RegistrarTransferenciaBancariaRequestDto Transferencia(int origen, int destino, decimal importe, DateOnly? fecha = null)
    {
        return new RegistrarTransferenciaBancariaRequestDto
        {
            CuentaOrigenId = origen,
            CuentaDestinoId = destino,
            Importe = importe,
            FechaOperacion = fecha ?? Hoy(),
            Referencia = "  Traslado interno "
        };
    }

    private static string RutaMovimientos(int id)
    {
        return $"{Cuentas}/{id}/movimientos";
    }

    private static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static HttpRequestMessage Peticion(string metodo, string sufijo)
    {
        HttpRequestMessage peticion = new HttpRequestMessage(new HttpMethod(metodo), Ruta + sufijo);
        if (metodo != "GET")
        {
            peticion.Content = JsonContent.Create(new { });
        }

        return peticion;
    }

    private async Task<CuentaBancariaDetalleDto> CrearCuentaAsync(string numero, string moneda = "PEN", string banco = "Banco 55")
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Cuentas, SolicitudCuenta(numero, moneda, banco));
        return await LeerAsync<CuentaBancariaDetalleDto>(respuesta, HttpStatusCode.Created);
    }

    private async Task<CuentaBancariaDetalleDto> CuentaAsync(int id)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync($"{Cuentas}/{id}");
        return await LeerAsync<CuentaBancariaDetalleDto>(respuesta, HttpStatusCode.OK);
    }

    private async Task<MovimientoBancarioDto> CrearMovimientoAsync(
        int id, string tipo, decimal importe, DateOnly? fecha = null, string? referencia = null)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaMovimientos(id), Movimiento(tipo, importe, fecha, referencia));
        return await LeerAsync<MovimientoBancarioDto>(respuesta, HttpStatusCode.Created);
    }

    private Task<HttpResponseMessage> RevertirAsync(int id, int movimientoId, string? motivo)
    {
        return _cliente.PostAsJsonAsync($"{RutaMovimientos(id)}/{movimientoId}/revertir", new RevertirMovimientoBancarioRequestDto { Motivo = motivo });
    }

    private async Task<PaginaCuentasBancariasDto> PaginaCuentasAsync(string filtro = "")
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Cuentas + filtro);
        return await LeerAsync<PaginaCuentasBancariasDto>(respuesta, HttpStatusCode.OK);
    }

    private async Task<PaginaMovimientosBancariosDto> HistorialAsync(int id, string filtro = "")
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(RutaMovimientos(id) + filtro);
        return await LeerAsync<PaginaMovimientosBancariosDto>(respuesta, HttpStatusCode.OK);
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta, HttpStatusCode estadoEsperado) where T : class
    {
        Assert.Equal(estadoEsperado, respuesta.StatusCode);
        return Assert.IsType<T>(await respuesta.Content.ReadFromJsonAsync<T>());
    }

    private static async Task ErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string literal)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal((int)estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(literal, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}

public sealed class TesoreriaBancosApiFactory : WebApplicationFactory<Program>
{
    private readonly string _baseDatos = "TesoreriaBancosTests-" + Guid.NewGuid();

    public int? MovimientoConciliadoId { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            servicios.AddDbContext<TitanGlobalDbContext>(opciones => opciones.UseInMemoryDatabase(_baseDatos));
            servicios.RemoveAll<IConsultaConciliacionBancaria>();
            servicios.AddScoped<IConsultaConciliacionBancaria>(servicio => new ConsultaConciliacionPruebas(this));
        });
    }

    private sealed class ConsultaConciliacionPruebas : IConsultaConciliacionBancaria
    {
        private readonly TesoreriaBancosApiFactory _factory;

        public ConsultaConciliacionPruebas(TesoreriaBancosApiFactory factory)
        {
            _factory = factory;
        }

        public Task<bool> TieneCoincidenciaVigenteAsync(int movimientoId)
        {
            return Task.FromResult(_factory.MovimientoConciliadoId == movimientoId);
        }
    }
}
