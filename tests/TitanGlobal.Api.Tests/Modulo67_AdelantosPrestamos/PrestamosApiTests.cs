using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Api.Tests.Modulo67_AdelantosPrestamos;

[Collection("Modulo04")]
public class PrestamosApiTests
{
    private const string RutaBase = "/api/v1/prestamos";

    private static readonly DateOnly Otorgamiento = new DateOnly(2030, 1, 10);

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public PrestamosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosPrestamos.Restablecer(fabrica);
    }

    // Mensajes 1 a 12 del registro, en el orden de la spec.
    [Fact]
    public async Task RegistroConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        await AssertRegistroAsync(null, "Prestamo", 100m, 2, Otorgamiento, 2030, 1, "Compra", "El empleado es obligatorio.");
        string tipoInvalido = "El tipo debe ser Adelanto o Prestamo.";
        await AssertRegistroAsync(1, null, 100m, 2, Otorgamiento, 2030, 1, "Compra", tipoInvalido);
        await AssertRegistroAsync(1, "Otro", 100m, 2, Otorgamiento, 2030, 1, "Compra", tipoInvalido);
        await AssertRegistroAsync(1, "Prestamo", null, 2, Otorgamiento, 2030, 1, "Compra", "El monto es obligatorio.");
        await AssertRegistroAsync(1, "Prestamo", 0m, 2, Otorgamiento, 2030, 1, "Compra", "El monto debe ser mayor que cero.");
        await AssertRegistroAsync(1, "Prestamo", -5m, 2, Otorgamiento, 2030, 1, "Compra", "El monto debe ser mayor que cero.");
        string montoInvalido = "El monto admite como máximo 2 decimales y no puede superar 9999999999.99.";
        await AssertRegistroAsync(1, "Prestamo", 10.123m, 2, Otorgamiento, 2030, 1, "Compra", montoInvalido);
        await AssertRegistroAsync(1, "Prestamo", 10000000000m, 2, Otorgamiento, 2030, 1, "Compra", montoInvalido);
        await AssertRegistroAsync(1, "Adelanto", 100m, 2, Otorgamiento, 2030, 1, "Compra", "Un adelanto se descuenta en una sola cuota.");
        string cuotasInvalidas = "El número de cuotas debe estar entre 1 y 24.";
        await AssertRegistroAsync(1, "Prestamo", 100m, null, Otorgamiento, 2030, 1, "Compra", cuotasInvalidas);
        await AssertRegistroAsync(1, "Prestamo", 100m, 0, Otorgamiento, 2030, 1, "Compra", cuotasInvalidas);
        await AssertRegistroAsync(1, "Prestamo", 100m, 25, Otorgamiento, 2030, 1, "Compra", cuotasInvalidas);
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, null, 2030, 1, "Compra", "La fecha de otorgamiento es obligatoria.");
        string mesInvalido = "El primer mes de descuento es obligatorio y debe ser un mes válido.";
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, Otorgamiento, null, 1, "Compra", mesInvalido);
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, Otorgamiento, 2030, null, "Compra", mesInvalido);
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, Otorgamiento, 2030, 0, "Compra", mesInvalido);
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, Otorgamiento, 2030, 13, "Compra", mesInvalido);
        string mesAnterior = "El primer mes de descuento no puede ser anterior al mes de otorgamiento.";
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, new DateOnly(2030, 3, 15), 2030, 2, "Compra", mesAnterior);
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, new DateOnly(2030, 1, 15), 2029, 12, "Compra", mesAnterior);
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, Otorgamiento, 2030, 1, null, "El motivo es obligatorio.");
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, Otorgamiento, 2030, 1, "   ", "El motivo es obligatorio.");
        await AssertRegistroAsync(1, "Prestamo", 100m, 2, Otorgamiento, 2030, 1, new string('a', 301), "El motivo no puede superar 300 caracteres.");
        Assert.Empty(await DatosPrestamos.LeerAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await Registrar(1, "Prestamo", 2400m, 24, Otorgamiento, 2030, 1, new string('a', 300))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Registrar(1, "Adelanto", 100m, 1, new DateOnly(2030, 3, 15), 2030, 3, "Compra")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Registrar(2, "Adelanto", 100m, null, Otorgamiento, 2030, 1, "Compra")).StatusCode);
    }

    // Mensajes 13 a 16 del registro.
    [Fact]
    public async Task RegistroContraElEmpleadoYElTopeDevuelveMensajesLiteralesSinFilasNuevas()
    {
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(9999, "Adelanto", 100m, 1, Otorgamiento, 2030, 1, "Compra"),
            404,
            "El empleado 9999 no existe.");
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(4, "Adelanto", 100m, 1, Otorgamiento, 2030, 1, "Compra"),
            409,
            "El empleado 4 está cesado y no admite préstamos.");
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(3, "Adelanto", 100m, 1, Otorgamiento, 2030, 1, "Compra"),
            409,
            "El empleado 3 no tiene un contrato vigente en la fecha de otorgamiento.");
        // Empleado 1: sueldo vigente 9500 (adenda), neto 8265.00 y máximo 2479.50.
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(1, "Adelanto", 2479.51m, 1, Otorgamiento, 2030, 1, "Compra"),
            409,
            "La cuota de 01/2030 sería 2479.51 y supera el máximo de 2479.50 (30 % del neto estimado).");
        Assert.Empty(await DatosPrestamos.LeerAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await Registrar(1, "Adelanto", 2479.50m, 1, Otorgamiento, 2030, 1, "Compra")).StatusCode);
    }

    [Fact]
    public async Task LaVigenciaDelContratoIgnoraAnuladosRespetaElCierreYUsaLaAdenda()
    {
        await DatosPrestamos.InsertarContratoAsync(
            _fabrica,
            DatosPrestamos.CrearContrato(5, 3, "PlazoFijo", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 1500m, "Cerrado", new DateOnly(2026, 3, 31)));
        await DatosPrestamos.InsertarContratoAsync(
            _fabrica,
            DatosPrestamos.CrearContrato(6, 3, "Indeterminado", new DateOnly(2027, 1, 1), null, 1500m, "Anulado", new DateOnly(2027, 1, 2)));
        string sinContrato = "El empleado 3 no tiene un contrato vigente en la fecha de otorgamiento.";
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(3, "Adelanto", 100m, 1, new DateOnly(2026, 5, 10), 2026, 5, "Compra"),
            409,
            sinContrato);
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(3, "Adelanto", 100m, 1, new DateOnly(2027, 2, 1), 2027, 2, "Compra"),
            409,
            sinContrato);
        // Neto 1305.00 y máximo 391.50 con el contrato cerrado el 2026-03-31.
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(3, "Adelanto", 391.51m, 1, new DateOnly(2026, 3, 31), 2026, 3, "Compra"),
            409,
            "La cuota de 03/2026 sería 391.51 y supera el máximo de 391.50 (30 % del neto estimado).");
        Assert.Equal(HttpStatusCode.OK, (await Registrar(3, "Adelanto", 391.50m, 1, new DateOnly(2026, 3, 31), 2026, 3, "Compra")).StatusCode);
    }

    [Fact]
    public async Task RegistroGuardaElPrestamoActivoConNumeroDefinitivoYCuotasPendientes()
    {
        HttpResponseMessage respuesta = await Registrar(1, " Prestamo ", 600m, 6, Otorgamiento, 2030, 2, "  Compra de vehiculo  ");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PrestamoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<PrestamoCreadoDto>();
        Assert.NotNull(creado);
        Assert.Equal("PRE-" + creado.Id.ToString("D6"), creado.Numero);
        Prestamo guardado = Assert.Single(await DatosPrestamos.LeerAsync(_fabrica));
        Assert.Equal(creado.Numero, guardado.Numero);
        Assert.Equal("Prestamo", guardado.Tipo);
        Assert.Equal("Compra de vehiculo", guardado.Motivo);
        Assert.Equal("Activo", guardado.Estado);
        Assert.Equal(600m, guardado.Monto);
        Assert.Equal(600m, guardado.Saldo);
        Assert.Equal(6, guardado.NumeroCuotas);
        Assert.Equal(Otorgamiento, guardado.FechaOtorgamiento);
        Assert.Null(guardado.FechaAnulacion);
        Assert.Null(guardado.MotivoAnulacion);
        List<CuotaPrestamo> cuotas = await DatosPrestamos.LeerCuotasAsync(_fabrica, creado.Id);
        Assert.Equal(6, cuotas.Count);
        Assert.All(cuotas, cuota =>
        {
            Assert.Equal("Pendiente", cuota.Estado);
            Assert.Equal(0m, cuota.MontoDescontado);
        });
    }

    [Fact]
    public async Task AdelantoSinNumeroDeCuotasSeDescuentaEnUnaCuota()
    {
        HttpResponseMessage respuesta = await Registrar(1, "Adelanto", 250m, null, Otorgamiento, 2030, 1, "Emergencia");
        PrestamoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<PrestamoCreadoDto>();
        Assert.NotNull(creado);
        Assert.Equal(1, (await DatosPrestamos.LeerPrestamoAsync(_fabrica, creado.Id)).NumeroCuotas);
        CuotaPrestamo cuota = Assert.Single(await DatosPrestamos.LeerCuotasAsync(_fabrica, creado.Id));
        Assert.Equal(250m, cuota.Monto);
    }

    [Fact]
    public async Task CronogramaRedondeaLaCuotaYLaUltimaAbsorbeLaDiferencia()
    {
        int tres = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 1000m, 3, 2030, 11);
        List<CuotaPrestamo> cuotasTres = await DatosPrestamos.LeerCuotasAsync(_fabrica, tres);
        Assert.Equal(new[] { 333.33m, 333.33m, 333.34m }, cuotasTres.Select(cuota => cuota.Monto).ToArray());
        Assert.Equal(new[] { (2030, 11), (2030, 12), (2031, 1) }, cuotasTres.Select(cuota => (cuota.Anio, cuota.Mes)).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, cuotasTres.Select(cuota => cuota.NumeroCuota).ToArray());

        int seis = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 100m, 6, 2030, 1);
        Assert.Equal(
            new[] { 16.67m, 16.67m, 16.67m, 16.67m, 16.67m, 16.65m },
            (await DatosPrestamos.LeerCuotasAsync(_fabrica, seis)).Select(cuota => cuota.Monto).ToArray());

        // 0.10 / 4 = 0.025: el redondeo se aleja de cero (0.03) y la última cuota queda en 0.01.
        int centavos = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 0.10m, 4, 2030, 1);
        Assert.Equal(
            new[] { 0.03m, 0.03m, 0.03m, 0.01m },
            (await DatosPrestamos.LeerCuotasAsync(_fabrica, centavos)).Select(cuota => cuota.Monto).ToArray());
    }

    [Fact]
    public async Task ElTopeSumaLasCuotasPendientesDeTodosLosPrestamosActivosDelMes()
    {
        // Empleado 2: sueldo 4200, neto 3654.00 y máximo 1096.20.
        int primero = await DatosPrestamos.CrearPrestamoAsync(_cliente, 2, "Adelanto", 1000m, 1, 2030, 2);
        string superado = "La cuota de 02/2030 sería 1100.00 y supera el máximo de 1096.20 (30 % del neto estimado).";
        await DatosPrestamos.AssertErrorAsync(await Registrar(2, "Adelanto", 100m, 1, Otorgamiento, 2030, 2, "Compra"), 409, superado);
        Assert.Equal(HttpStatusCode.OK, (await Registrar(2, "Adelanto", 96.20m, 1, Otorgamiento, 2030, 2, "Compra")).StatusCode);
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(2, "Adelanto", 0.01m, 1, Otorgamiento, 2030, 2, "Compra"),
            409,
            "La cuota de 02/2030 sería 1096.21 y supera el máximo de 1096.20 (30 % del neto estimado).");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{RutaBase}/{primero}/anulacion", new AnularPrestamoRequestDto { Motivo = "Error de carga" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Registrar(2, "Adelanto", 100m, 1, Otorgamiento, 2030, 2, "Compra")).StatusCode);
    }

    [Fact]
    public async Task ElTopeSeEvaluaMesPorMesYNombraElMesQueSeSupera()
    {
        await DatosPrestamos.CrearPrestamoAsync(_cliente, 2, "Prestamo", 400m, 4, 2030, 6);
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(2, "Adelanto", 1000m, 1, Otorgamiento, 2030, 8, "Compra"),
            409,
            "La cuota de 08/2030 sería 1100.00 y supera el máximo de 1096.20 (30 % del neto estimado).");
        Assert.Equal(HttpStatusCode.OK, (await Registrar(2, "Adelanto", 1000m, 1, Otorgamiento, 2030, 10, "Compra")).StatusCode);
    }

    [Fact]
    public async Task CapacidadUsaElSueldoVigenteConAdendasYListaLoComprometido()
    {
        CapacidadPrestamoDto? antes = await _cliente.GetFromJsonAsync<CapacidadPrestamoDto>($"{RutaBase}/capacidad/1?fecha=2023-12-31");
        Assert.NotNull(antes);
        Assert.Equal(8500m, antes.SueldoVigente);
        Assert.Equal(7395.00m, antes.NetoEstimado);
        Assert.Equal(2218.50m, antes.CuotaMaxima);
        Assert.Empty(antes.CuotasComprometidasPorMes);
        CapacidadPrestamoDto? despues = await _cliente.GetFromJsonAsync<CapacidadPrestamoDto>($"{RutaBase}/capacidad/1?fecha=2024-01-01");
        Assert.NotNull(despues);
        Assert.Equal(9500m, despues.SueldoVigente);
        Assert.Equal(8265.00m, despues.NetoEstimado);
        Assert.Equal(2479.50m, despues.CuotaMaxima);

        CapacidadPrestamoDto? sinContrato = await _cliente.GetFromJsonAsync<CapacidadPrestamoDto>($"{RutaBase}/capacidad/3?fecha=2030-01-01");
        Assert.NotNull(sinContrato);
        Assert.Equal(0m, sinContrato.SueldoVigente);
        Assert.Equal(0m, sinContrato.CuotaMaxima);

        await DatosPrestamos.CrearPrestamoAsync(_cliente, 2, "Prestamo", 400m, 4, 2030, 2);
        CapacidadPrestamoDto? comprometida = await _cliente.GetFromJsonAsync<CapacidadPrestamoDto>($"{RutaBase}/capacidad/2?fecha=2030-03-15");
        Assert.NotNull(comprometida);
        Assert.Equal(
            new[] { (2030, 3), (2030, 4), (2030, 5) },
            comprometida.CuotasComprometidasPorMes.Select(cuota => (cuota.Anio, cuota.Mes)).ToArray());
        Assert.All(comprometida.CuotasComprometidasPorMes, cuota => Assert.Equal(100m, cuota.Monto));
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/capacidad/9999"), 404, "El empleado 9999 no existe.");
    }

    [Fact]
    public async Task DescontarRespetaElTopeLasCuotasAtrasadasPrimeroYNoGuardaPorSiMismo()
    {
        int prestamo = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 600m, 6, 2030, 1);
        Assert.Equal(200m, await DescontarAsync(1, 2030, 2, 1000m, 7, false));
        Assert.All(await DatosPrestamos.LeerCuotasAsync(_fabrica, prestamo), cuota => Assert.Equal("Pendiente", cuota.Estado));
        Assert.Equal(600m, (await DatosPrestamos.LeerPrestamoAsync(_fabrica, prestamo)).Saldo);

        Assert.Equal(200m, await DescontarAsync(1, 2030, 2, 1000m, 7, true));
        List<CuotaPrestamo> cuotas = await DatosPrestamos.LeerCuotasAsync(_fabrica, prestamo);
        Assert.Equal(new[] { "Descontada", "Descontada", "Pendiente", "Pendiente", "Pendiente", "Pendiente" }, cuotas.Select(cuota => cuota.Estado).ToArray());
        Assert.Equal(new[] { "7:100", "7:100", string.Empty, string.Empty, string.Empty, string.Empty }, await DatosPrestamos.LeerDescuentosPorCuotaAsync(_fabrica, prestamo));
        Assert.Equal(new[] { 100m, 100m, 0m, 0m, 0m, 0m }, cuotas.Select(cuota => cuota.MontoDescontado).ToArray());
        Prestamo guardado = await DatosPrestamos.LeerPrestamoAsync(_fabrica, prestamo);
        Assert.Equal(400m, guardado.Saldo);
        Assert.Equal("Activo", guardado.Estado);
    }

    [Fact]
    public async Task DescontarConMontoMaximoMenorDescuentaParcialmenteYLuegoCompleta()
    {
        int prestamo = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 600m, 6, 2030, 1);
        Assert.Equal(150m, await DescontarAsync(1, 2030, 3, 150m, 8, true));
        List<CuotaPrestamo> primera = await DatosPrestamos.LeerCuotasAsync(_fabrica, prestamo);
        Assert.Equal("Descontada", primera[0].Estado);
        Assert.Equal("Pendiente", primera[1].Estado);
        Assert.Equal(50m, primera[1].MontoDescontado);
        Assert.Equal(new[] { "8:100", "8:50", string.Empty, string.Empty, string.Empty, string.Empty }, await DatosPrestamos.LeerDescuentosPorCuotaAsync(_fabrica, prestamo));
        Assert.Equal("Pendiente", primera[2].Estado);
        Assert.Equal(450m, (await DatosPrestamos.LeerPrestamoAsync(_fabrica, prestamo)).Saldo);

        Assert.Equal(150m, await DescontarAsync(1, 2030, 3, 1000m, 9, true));
        List<CuotaPrestamo> segunda = await DatosPrestamos.LeerCuotasAsync(_fabrica, prestamo);
        Assert.Equal(new[] { "Descontada", "Descontada", "Descontada", "Pendiente", "Pendiente", "Pendiente" }, segunda.Select(cuota => cuota.Estado).ToArray());
        Assert.Equal(100m, segunda[1].MontoDescontado);
        Assert.Equal(new[] { "8:100", "8:50,9:50", "9:100", string.Empty, string.Empty, string.Empty }, await DatosPrestamos.LeerDescuentosPorCuotaAsync(_fabrica, prestamo));
        Assert.Equal(300m, (await DatosPrestamos.LeerPrestamoAsync(_fabrica, prestamo)).Saldo);
    }

    [Fact]
    public async Task DescontarOrdenaPorMesYNoTocaLasCuotasDeMesesPosteriores()
    {
        int a = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 200m, 2, 2030, 1);
        int b = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 50m, 1, 2030, 2);
        int c = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 100m, 1, 2030, 3);
        Assert.Equal(220m, await DescontarAsync(1, 2030, 2, 220m, 1, true));
        Assert.Equal(new[] { "Descontada", "Descontada" }, (await DatosPrestamos.LeerCuotasAsync(_fabrica, a)).Select(cuota => cuota.Estado).ToArray());
        CuotaPrestamo cuotaB = Assert.Single(await DatosPrestamos.LeerCuotasAsync(_fabrica, b));
        Assert.Equal(20m, cuotaB.MontoDescontado);
        Assert.Equal("Pendiente", cuotaB.Estado);
        Assert.Equal("Pendiente", Assert.Single(await DatosPrestamos.LeerCuotasAsync(_fabrica, c)).Estado);
        Assert.Equal(0m, await DescontarAsync(1, 2030, 2, 0m, 2, true));
        Assert.Equal(0m, await DescontarAsync(1, 2030, 2, -10m, 2, true));
        Assert.Equal(0m, await DescontarAsync(2, 2030, 12, 1000m, 2, true));
        Assert.Equal(30m, await DescontarAsync(1, 2030, 2, 1000m, 2, true));
        Assert.Equal("Pendiente", Assert.Single(await DatosPrestamos.LeerCuotasAsync(_fabrica, c)).Estado);
    }

    [Fact]
    public async Task DescontarCancelaElPrestamoAlLlegarAlSaldoCeroYNoSuperaElSaldo()
    {
        int adelanto = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 100m, 1, 2030, 1);
        Assert.Equal(100m, await DescontarAsync(1, 2030, 1, 1000m, 1, true));
        Prestamo cancelado = await DatosPrestamos.LeerPrestamoAsync(_fabrica, adelanto);
        Assert.Equal("Cancelado", cancelado.Estado);
        Assert.Equal(0m, cancelado.Saldo);
        Assert.Equal(0m, await DescontarAsync(1, 2030, 12, 1000m, 2, true));

        int tope = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 200m, 2, 2030, 1);
        await DatosPrestamos.ModificarAsync(_fabrica, contexto => contexto.Prestamos.Single(prestamo => prestamo.Id == tope).Saldo = 30m);
        Assert.Equal(30m, await DescontarAsync(1, 2030, 5, 1000m, 3, true));
        Prestamo agotado = await DatosPrestamos.LeerPrestamoAsync(_fabrica, tope);
        Assert.Equal(0m, agotado.Saldo);
        Assert.Equal("Cancelado", agotado.Estado);
        Assert.Equal(30m, (await DatosPrestamos.LeerCuotasAsync(_fabrica, tope))[0].MontoDescontado);
    }

    [Fact]
    public async Task DescontarIgnoraLosPrestamosAnulados()
    {
        int id = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 100m, 1, 2030, 1);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{RutaBase}/{id}/anulacion", new AnularPrestamoRequestDto { Motivo = "Error" })).StatusCode);
        Assert.Equal(0m, await DescontarAsync(1, 2030, 12, 1000m, 1, true));
    }

    [Fact]
    public async Task RevertirDevuelveLoDescontadoPorLaPlanillaSinTocarLasDemas()
    {
        int prestamo = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 600m, 6, 2030, 1);
        int adelanto = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 100m, 1, 2030, 1);
        Assert.Equal(300m, await DescontarAsync(1, 2030, 2, 300m, 5, true));
        Assert.Equal(100m, await DescontarAsync(1, 2030, 3, 100m, 6, true));
        Assert.Equal("Cancelado", (await DatosPrestamos.LeerPrestamoAsync(_fabrica, adelanto)).Estado);

        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            AdministrarPrestamosUseCase casoDeUso = alcance.ServiceProvider.GetRequiredService<AdministrarPrestamosUseCase>();
            IUnitOfWork unidad = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();
            Assert.Equal(300m, await casoDeUso.RevertirDescuentosAsync(5));
            await unidad.CompleteAsync();
        }

        Prestamo revertido = await DatosPrestamos.LeerPrestamoAsync(_fabrica, adelanto);
        Assert.Equal("Activo", revertido.Estado);
        Assert.Equal(100m, revertido.Saldo);
        Assert.Equal(500m, (await DatosPrestamos.LeerPrestamoAsync(_fabrica, prestamo)).Saldo);
        List<CuotaPrestamo> cuotas = await DatosPrestamos.LeerCuotasAsync(_fabrica, prestamo);
        Assert.Equal(new[] { "Pendiente", "Pendiente", "Descontada" }, cuotas.Take(3).Select(cuota => cuota.Estado).ToArray());
        Assert.Equal(new[] { string.Empty, string.Empty, "6:100" }, (await DatosPrestamos.LeerDescuentosPorCuotaAsync(_fabrica, prestamo)).Take(3).ToArray());
        CuotaPrestamo cuotaAdelanto = Assert.Single(await DatosPrestamos.LeerCuotasAsync(_fabrica, adelanto));
        Assert.Equal(0m, cuotaAdelanto.MontoDescontado);
        Assert.Equal(new[] { string.Empty }, await DatosPrestamos.LeerDescuentosPorCuotaAsync(_fabrica, adelanto));

        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            AdministrarPrestamosUseCase casoDeUso = alcance.ServiceProvider.GetRequiredService<AdministrarPrestamosUseCase>();
            Assert.Equal(0m, await casoDeUso.RevertirDescuentosAsync(99));
        }
    }

    [Fact]
    public async Task RevertirUnaPlanillaConservaLaParteDescontadaPorLaOtra()
    {
        int prestamo = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 200m, 2, 2030, 1);
        Assert.Equal(60m, await DescontarAsync(1, 2030, 1, 60m, 1, true));
        Assert.Equal(140m, await DescontarAsync(1, 2030, 2, 1000m, 2, true));
        Assert.Equal(new[] { "1:60,2:40", "2:100" }, await DatosPrestamos.LeerDescuentosPorCuotaAsync(_fabrica, prestamo));
        Prestamo agotado = await DatosPrestamos.LeerPrestamoAsync(_fabrica, prestamo);
        Assert.Equal("Cancelado", agotado.Estado);
        Assert.Equal(0m, agotado.Saldo);

        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            AdministrarPrestamosUseCase casoDeUso = alcance.ServiceProvider.GetRequiredService<AdministrarPrestamosUseCase>();
            IUnitOfWork unidad = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();
            Assert.Equal(140m, await casoDeUso.RevertirDescuentosAsync(2));
            await unidad.CompleteAsync();
        }

        Assert.Equal(new[] { "1:60", string.Empty }, await DatosPrestamos.LeerDescuentosPorCuotaAsync(_fabrica, prestamo));
        List<CuotaPrestamo> cuotas = await DatosPrestamos.LeerCuotasAsync(_fabrica, prestamo);
        Assert.Equal(60m, cuotas[0].MontoDescontado);
        Assert.Equal("Pendiente", cuotas[0].Estado);
        Assert.Equal(0m, cuotas[1].MontoDescontado);
        Prestamo revertido = await DatosPrestamos.LeerPrestamoAsync(_fabrica, prestamo);
        Assert.Equal("Activo", revertido.Estado);
        Assert.Equal(140m, revertido.Saldo);
    }

    // Mensajes 17 a 19 de la anulación y los dos del motivo.
    [Fact]
    public async Task AnulacionConDatosInvalidosDevuelveMensajesLiteralesSinCambiarFilas()
    {
        await DatosPrestamos.AssertErrorAsync(await Anular(9999, "Error"), 404, "El préstamo 9999 no existe.");
        int cancelado = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 100m, 1, 2030, 1);
        await DatosPrestamos.ModificarAsync(_fabrica, contexto => contexto.Prestamos.Single(prestamo => prestamo.Id == cancelado).Estado = "Cancelado");
        string numeroCancelado = (await DatosPrestamos.LeerPrestamoAsync(_fabrica, cancelado)).Numero;
        await DatosPrestamos.AssertErrorAsync(
            await Anular(cancelado, "Error"),
            409,
            $"El préstamo {numeroCancelado} no está activo y no se puede anular.");
        int conDescuento = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 200m, 2, 2030, 1);
        Assert.Equal(100m, await DescontarAsync(1, 2030, 1, 100m, 1, true));
        string numeroConDescuento = (await DatosPrestamos.LeerPrestamoAsync(_fabrica, conDescuento)).Numero;
        await DatosPrestamos.AssertErrorAsync(
            await Anular(conDescuento, "Error"),
            409,
            $"El préstamo {numeroConDescuento} ya tiene descuentos y no se puede anular.");
        int activo = await DatosPrestamos.CrearPrestamoAsync(_cliente, 2, "Adelanto", 100m, 1, 2030, 1);
        await DatosPrestamos.AssertErrorAsync(await Anular(activo, null), 400, "El motivo de la anulación es obligatorio.");
        await DatosPrestamos.AssertErrorAsync(await Anular(activo, "  "), 400, "El motivo de la anulación es obligatorio.");
        await DatosPrestamos.AssertErrorAsync(await Anular(activo, new string('a', 301)), 400, "El motivo de la anulación no puede superar 300 caracteres.");
        Assert.Equal("Activo", (await DatosPrestamos.LeerPrestamoAsync(_fabrica, activo)).Estado);
    }

    [Fact]
    public async Task AnulacionDejaElPrestamoAnuladoConSuMotivoYNoSePuedeRepetir()
    {
        int id = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 300m, 3, 2030, 1);
        HttpResponseMessage respuesta = await Anular(id, "  Carga duplicada  ");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PrestamoIdDto? resultado = await respuesta.Content.ReadFromJsonAsync<PrestamoIdDto>();
        Assert.NotNull(resultado);
        Assert.Equal(id, resultado.Id);
        Prestamo anulado = await DatosPrestamos.LeerPrestamoAsync(_fabrica, id);
        Assert.Equal("Anulado", anulado.Estado);
        Assert.Equal(0m, anulado.Saldo);
        Assert.Equal("Carga duplicada", anulado.MotivoAnulacion);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), anulado.FechaAnulacion);
        await DatosPrestamos.AssertErrorAsync(
            await Anular(id, "Otra vez"),
            409,
            $"El préstamo {anulado.Numero} no está activo y no se puede anular.");
    }

    [Fact]
    public async Task ListadoFiltraPaginaYValidaLosParametros()
    {
        int primero = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 100m, 1, 2030, 1);
        int segundo = await DatosPrestamos.CrearPrestamoAsync(_cliente, 2, "Prestamo", 300m, 3, 2030, 1);
        int tercero = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 200m, 2, 2030, 1);
        PaginaPrestamosDto todos = await ListarAsync(string.Empty);
        Assert.Equal(3, todos.Total);
        Assert.Equal(new[] { tercero, segundo, primero }, todos.Items.Select(item => item.Id).ToArray());
        PrestamoListadoDto fila = todos.Items[2];
        Assert.Equal("Torres Vega, Ana", fila.EmpleadoNombre);
        Assert.Equal("EMP-000001", fila.EmpleadoLegajo);
        Assert.Equal(100m, fila.Monto);
        Assert.Equal(100m, fila.Saldo);
        Assert.Equal("Activo", fila.Estado);
        Assert.Equal(2, (await ListarAsync("?empleadoId=1")).Total);
        Assert.Equal(primero, Assert.Single((await ListarAsync("?tipo=Adelanto")).Items).Id);
        Assert.Empty((await ListarAsync("?estado=Anulado")).Items);
        Assert.Equal(3, (await ListarAsync("?estado=Activo")).Total);
        Assert.Equal(segundo, Assert.Single((await ListarAsync("?busqueda=rojas")).Items).Id);
        Assert.Equal(segundo, Assert.Single((await ListarAsync("?busqueda=" + (await DatosPrestamos.LeerPrestamoAsync(_fabrica, segundo)).Numero)).Items).Id);
        PaginaPrestamosDto segunda = await ListarAsync("?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Equal(2, segunda.Pagina);
        Assert.Single(segunda.Items);
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tipo=Otro"), 400, "El filtro tipo no es válido.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?estado=Otro"), 400, "El filtro estado no es válido.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?empleadoId=9999"), 404, "El empleado 9999 no existe.");
    }

    [Fact]
    public async Task DetalleDevuelveElCronogramaYLaAnulacion()
    {
        int id = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 300m, 3, 2030, 11);
        Assert.Equal(HttpStatusCode.OK, (await Anular(id, "Carga duplicada")).StatusCode);
        PrestamoDetalleDto? detalle = await _cliente.GetFromJsonAsync<PrestamoDetalleDto>($"{RutaBase}/{id}");
        Assert.NotNull(detalle);
        Assert.Equal("Anulado", detalle.Estado);
        Assert.Equal("Carga duplicada", detalle.MotivoAnulacion);
        Assert.NotNull(detalle.FechaAnulacion);
        Assert.Equal("Motivo de prueba", detalle.Motivo);
        Assert.Equal("Torres Vega, Ana", detalle.EmpleadoNombre);
        Assert.Equal(2030, detalle.PrimerAnio);
        Assert.Equal(11, detalle.PrimerMes);
        Assert.Equal(new[] { 1, 2, 3 }, detalle.Cuotas.Select(cuota => cuota.NumeroCuota).ToArray());
        Assert.Equal(new[] { (2030, 11), (2030, 12), (2031, 1) }, detalle.Cuotas.Select(cuota => (cuota.Anio, cuota.Mes)).ToArray());
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "/9999"), 404, "El préstamo 9999 no existe.");
    }

    [Fact]
    public async Task ResumenCuentaActivosSaldoOtorgadoYCuotasDelMes()
    {
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Equal(HttpStatusCode.OK, (await Registrar(1, "Adelanto", 100m, 1, hoy, hoy.Year, hoy.Month, "Compra")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Registrar(2, "Prestamo", 300m, 3, hoy, hoy.Year, hoy.Month, "Compra")).StatusCode);
        HttpResponseMessage anulado = await Registrar(1, "Adelanto", 50m, 1, hoy, hoy.Year, hoy.Month, "Compra");
        PrestamoCreadoDto? creado = await anulado.Content.ReadFromJsonAsync<PrestamoCreadoDto>();
        Assert.NotNull(creado);
        Assert.Equal(HttpStatusCode.OK, (await Anular(creado.Id, "Error")).StatusCode);
        ResumenPrestamosDto? resumen = await _cliente.GetFromJsonAsync<ResumenPrestamosDto>(RutaBase + "/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(2, resumen.Activos);
        Assert.Equal(400m, resumen.SaldoPendienteTotal);
        Assert.Equal(400m, resumen.OtorgadoMes);
        Assert.Equal(200m, resumen.CuotasDelMes);
    }

    [Fact]
    public async Task CuerpoVacioDevuelveElLiteralDelMiddleware()
    {
        foreach (string ruta in new[] { RutaBase, RutaBase + "/1/anulacion" })
        {
            HttpResponseMessage respuesta = await _cliente.PostAsync(ruta, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
            await DatosPrestamos.AssertErrorAsync(respuesta, 400, "El cuerpo de la petición (Body) no puede estar vacío.");
        }
    }

    [Fact]
    public async Task SinTokenTodasLasOperacionesDevuelven401()
    {
        HttpClient anonimo = DatosPrestamos.ClienteAnonimo(_fabrica);
        await DatosPrestamos.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, DatosPrestamos.MensajeSinSesion);
        await DatosPrestamos.AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/resumen"), 401, DatosPrestamos.MensajeSinSesion);
        await DatosPrestamos.AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/1"), 401, DatosPrestamos.MensajeSinSesion);
        await DatosPrestamos.AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/capacidad/1"), 401, DatosPrestamos.MensajeSinSesion);
        await DatosPrestamos.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, new RegistrarPrestamoRequestDto()), 401, DatosPrestamos.MensajeSinSesion);
        await DatosPrestamos.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/1/anulacion", new AnularPrestamoRequestDto()), 401, DatosPrestamos.MensajeSinSesion);
    }

    [Fact]
    public async Task TokenSinRolDeRrhhDevuelve403AlRegistrarYAnularSinCambiarFilasPeroPuedeConsultar()
    {
        int id = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Adelanto", 100m, 1, 2030, 1);
        string[] antes = await DatosPrestamos.LeerFirmasAsync(_fabrica);
        HttpClient consulta = DatosPrestamos.ClienteConRoles(_fabrica, "CONSULTA");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPrestamos.RegistrarAsync(consulta, 1, "Adelanto", 100m, 1, Otorgamiento, 2030, 1, "Compra"),
            403,
            DatosPrestamos.MensajeSinPermiso);
        await DatosPrestamos.AssertErrorAsync(
            await consulta.PostAsJsonAsync($"{RutaBase}/{id}/anulacion", new AnularPrestamoRequestDto { Motivo = "x" }),
            403,
            DatosPrestamos.MensajeSinPermiso);
        Assert.Equal(antes, await DatosPrestamos.LeerFirmasAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/capacidad/1")).StatusCode);
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenRegistrarYAnular(string rol)
    {
        HttpClient cliente = DatosPrestamos.ClienteConRoles(_fabrica, rol);
        HttpResponseMessage respuesta = await DatosPrestamos.RegistrarAsync(cliente, 1, "Adelanto", 100m, 1, Otorgamiento, 2030, 1, "Compra");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PrestamoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<PrestamoCreadoDto>();
        Assert.NotNull(creado);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync($"{RutaBase}/{creado.Id}/anulacion", new AnularPrestamoRequestDto { Motivo = "Error" })).StatusCode);
    }

    [Fact]
    public async Task RegistroYAnulacionGeneranRegistrosDeAuditoria()
    {
        int id = await DatosPrestamos.CrearPrestamoAsync(_cliente, 1, "Prestamo", 200m, 2, 2030, 1);
        Assert.Single(await DatosPrestamos.LeerAuditoriaAsync(_fabrica, "Prestamo", id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.OK, (await Anular(id, "Carga duplicada")).StatusCode);
        RegistroAuditoria cambio = Assert.Single(
            await DatosPrestamos.LeerAuditoriaAsync(_fabrica, "Prestamo", id),
            registro => registro.Accion == "Modificacion" && registro.Cambios.Contains("MotivoAnulacion"));
        using JsonDocument cambios = JsonDocument.Parse(cambio.Cambios);
        List<string> campos = cambios.RootElement.EnumerateArray().Select(item => item.GetProperty("campo").GetString() ?? string.Empty).ToList();
        Assert.Contains("Estado", campos);
        Assert.Contains("MotivoAnulacion", campos);
    }

    private async Task<decimal> DescontarAsync(int empleadoId, int anio, int mes, decimal montoMaximo, int planillaId, bool guardar)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        AdministrarPrestamosUseCase casoDeUso = alcance.ServiceProvider.GetRequiredService<AdministrarPrestamosUseCase>();
        IUnitOfWork unidad = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();
        decimal descontado = await casoDeUso.DescontarCuotasAsync(empleadoId, anio, mes, montoMaximo, planillaId);
        if (guardar)
        {
            await unidad.CompleteAsync();
        }

        return descontado;
    }

    private async Task<HttpResponseMessage> Registrar(int? empleadoId, string? tipo, decimal? monto, int? numeroCuotas, DateOnly? otorgamiento, int? primerAnio, int? primerMes, string? motivo)
    {
        return await DatosPrestamos.RegistrarAsync(_cliente, empleadoId, tipo, monto, numeroCuotas, otorgamiento, primerAnio, primerMes, motivo);
    }

    private async Task AssertRegistroAsync(int? empleadoId, string? tipo, decimal? monto, int? numeroCuotas, DateOnly? otorgamiento, int? primerAnio, int? primerMes, string? motivo, string mensaje)
    {
        await DatosPrestamos.AssertErrorAsync(
            await Registrar(empleadoId, tipo, monto, numeroCuotas, otorgamiento, primerAnio, primerMes, motivo),
            400,
            mensaje);
    }

    private async Task<HttpResponseMessage> Anular(int id, string? motivo)
    {
        return await _cliente.PostAsJsonAsync($"{RutaBase}/{id}/anulacion", new AnularPrestamoRequestDto { Motivo = motivo });
    }

    private async Task<PaginaPrestamosDto> ListarAsync(string consulta)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync(RutaBase + consulta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PaginaPrestamosDto? pagina = await respuesta.Content.ReadFromJsonAsync<PaginaPrestamosDto>();
        Assert.NotNull(pagina);
        return pagina;
    }
}
