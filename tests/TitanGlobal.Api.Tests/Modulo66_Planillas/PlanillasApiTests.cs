using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Api.Tests.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo66_Planillas;

[Collection("Modulo04")]
public class PlanillasApiTests
{
    private const string RutaBase = "/api/v1/planillas";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public PlanillasApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosPlanillas.Restablecer(fabrica);
    }

    // Mensajes 1 a 4 de la creación.
    [Fact]
    public async Task CrearConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        string anioInvalido = "El año es obligatorio y debe estar entre 2000 y 2100.";
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, null, 10, "Mensual"), 400, anioInvalido);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, 1999, 10, "Mensual"), 400, anioInvalido);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, 2101, 10, "Mensual"), 400, anioInvalido);
        string mesInvalido = "El mes es obligatorio y debe estar entre 1 y 12.";
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, null, "Mensual"), 400, mesInvalido);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 0, "Mensual"), 400, mesInvalido);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 13, "Mensual"), 400, mesInvalido);
        string tipoInvalido = "El tipo de planilla debe ser Mensual o Complementaria.";
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 10, null), 400, tipoInvalido);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 10, "Quincenal"), 400, tipoInvalido);
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.CrearPlanillaAsync(_cliente, 2100, 1, "Mensual"),
            400,
            "No se puede crear una planilla de un periodo futuro.");
        PaginaPlanillasDto? pagina = await _cliente.GetFromJsonAsync<PaginaPlanillasDto>(RutaBase);
        Assert.NotNull(pagina);
        Assert.Empty(pagina.Items);
    }

    [Fact]
    public async Task CrearDevuelveElNumeroDelPeriodoYRechazaDuplicadosYComplementariasSinMensualAprobada()
    {
        HttpResponseMessage creada = await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 10, "Mensual");
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        PlanillaCreadaDto? dto = await creada.Content.ReadFromJsonAsync<PlanillaCreadaDto>();
        Assert.NotNull(dto);
        Assert.Equal("PLA-202610-M", dto.Numero);
        PlanillaDetalleDto planilla = await DatosPlanillas.LeerPlanillaAsync(_cliente, dto.Id);
        Assert.Equal("Borrador", planilla.Estado);
        Assert.Equal(0, planilla.Empleados);
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 10, "Mensual"),
            409,
            "Ya existe la planilla Mensual del periodo 10/2026.");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 10, "Complementaria"),
            409,
            "La planilla complementaria necesita la planilla mensual de 10/2026 aprobada.");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.CrearPlanillaAsync(_cliente, 2026, 9, "Complementaria"),
            409,
            "La planilla complementaria necesita la planilla mensual de 9/2026 aprobada.");
    }

    [Fact]
    public async Task ElFlujoCompletoCalculaConImportesExactosYDescuentaElPrestamo()
    {
        await PrepararOctubreAsync();
        int id = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");

        PlanillaCalculoDto calculo = await DatosPlanillas.CalcularOkAsync(_cliente, id);

        Assert.Equal(2, calculo.Empleados);
        Assert.Equal(1, calculo.Incidencias);
        PlanillaDetalleDto planilla = await DatosPlanillas.LeerPlanillaAsync(_cliente, id);
        Assert.Equal("Calculada", planilla.Estado);
        Assert.Equal(14200.00m, planilla.TotalIngresos);
        Assert.Equal(2953.66m, planilla.TotalDescuentos);
        Assert.Equal(11246.34m, planilla.TotalNeto);
        Assert.Equal(1278.00m, planilla.TotalAportesEmpleador);
        Assert.Equal("pruebas", planilla.CalculadoPor);
        Assert.NotNull(planilla.FechaCalculo);
        List<PlanillaEmpleadoDto> detalles = await DatosPlanillas.LeerDetallesAsync(_cliente, id);
        Assert.Equal(new[] { 1, 2, 3 }, detalles.Select(detalle => detalle.EmpleadoId).ToArray());

        // Empleado 1 (9500 + bono 500 = 10000, INTEGRA): aporte 1000.00 + comision 155.00 + prima 137.00 = 1292.00;
        // quinta (120000 - 37450 = 82550): 2140.00 + 55800 x 0.14 (7812.00) = 9952.00 / 12 = 829.33; cuota de prestamo 200.00.
        // Descuentos 1292.00 + 829.33 + 200.00 = 2321.33; neto 7678.67; EsSalud 900.00.
        PlanillaEmpleadoDto primero = detalles[0];
        Assert.Equal(9500.00m, primero.SueldoBasico);
        Assert.Equal(30, primero.DiasLaborados);
        Assert.Equal(9500.00m, primero.IngresoBasico);
        Assert.Equal(500.00m, primero.IngresoBonos);
        Assert.Equal(10000.00m, primero.TotalIngresos);
        Assert.Equal("INTEGRA", primero.CodigoPension);
        Assert.Equal(1292.00m, primero.DescuentoPension);
        Assert.Equal(829.33m, primero.DescuentoQuinta);
        Assert.Equal(200.00m, primero.DescuentoPrestamos);
        Assert.Equal(2321.33m, primero.TotalDescuentos);
        Assert.Equal(7678.67m, primero.Neto);
        Assert.Equal(900.00m, primero.AporteEssalud);
        Assert.Null(primero.Incidencia);
        Assert.Equal("Gerente Comercial", primero.Cargo);

        // Empleado 2 (4200, ONP): 546.00; quinta (50400 - 37450 = 12950) x 0.08 = 1036.00 / 12 = 86.33; neto 3567.67; EsSalud 378.00.
        PlanillaEmpleadoDto segundo = detalles[1];
        Assert.Equal(546.00m, segundo.DescuentoPension);
        Assert.Equal(86.33m, segundo.DescuentoQuinta);
        Assert.Equal(0m, segundo.DescuentoPrestamos);
        Assert.Equal(3567.67m, segundo.Neto);
        Assert.Equal(378.00m, segundo.AporteEssalud);

        PlanillaEmpleadoDto sinAfiliacion = detalles[2];
        Assert.Equal("Sin afiliación a un sistema de pensiones.", sinAfiliacion.Incidencia);
        Assert.Equal(0m, sinAfiliacion.TotalIngresos);
        Assert.Equal(0m, sinAfiliacion.Neto);

        Prestamo prestamo = await DatosPlanillas.LeerPrestamoAsync(_fabrica, 1);
        Assert.Equal(400.00m, prestamo.Saldo);
        Assert.Equal(new[] { 200.00m, 0m, 0m }, prestamo.CuotasPrestamo.OrderBy(cuota => cuota.NumeroCuota).Select(cuota => cuota.MontoDescontado).ToArray());
        Assert.All((await DatosPlanillas.LeerBonosAsync(_fabrica)), bono => Assert.Equal(id, bono.PlanillaId));

        PaginaPlanillaEmpleadosDto? soloIncidencias = await _cliente.GetFromJsonAsync<PaginaPlanillaEmpleadosDto>($"{RutaBase}/{id}/detalles?soloIncidencias=true");
        Assert.NotNull(soloIncidencias);
        Assert.Equal(3, Assert.Single(soloIncidencias.Items).EmpleadoId);
        PaginaPlanillaEmpleadosDto? busqueda = await _cliente.GetFromJsonAsync<PaginaPlanillaEmpleadosDto>($"{RutaBase}/{id}/detalles?busqueda=rojas");
        Assert.NotNull(busqueda);
        Assert.Equal(2, Assert.Single(busqueda.Items).EmpleadoId);
    }

    [Fact]
    public async Task RecalcularNoDuplicaLosDescuentosDePrestamosNiLosDetalles()
    {
        await PrepararOctubreAsync();
        int id = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        await DatosPlanillas.CalcularOkAsync(_cliente, id);

        PlanillaCalculoDto segundo = await DatosPlanillas.CalcularOkAsync(_cliente, id);

        Assert.Equal(2, segundo.Empleados);
        Assert.Equal(1, segundo.Incidencias);
        Assert.Equal(3, (await DatosPlanillas.LeerDetallesAsync(_cliente, id)).Count);
        Assert.Equal(1, await DatosPlanillas.ContarDescuentosAsync(_fabrica));
        Prestamo prestamo = await DatosPlanillas.LeerPrestamoAsync(_fabrica, 1);
        Assert.Equal(400.00m, prestamo.Saldo);
        Assert.Equal(200.00m, prestamo.CuotasPrestamo.Single(cuota => cuota.NumeroCuota == 1).MontoDescontado);
        Assert.Equal("Calculada", (await DatosPlanillas.LeerPlanillaAsync(_cliente, id)).Estado);
        Assert.Equal(11246.34m, (await DatosPlanillas.LeerPlanillaAsync(_cliente, id)).TotalNeto);

        // Quitar el préstamo y recalcular libera el descuento anterior.
        await DatosPlanillas.ModificarAsync(_fabrica, contexto =>
        {
            Prestamo anulado = contexto.Prestamos.Single(entidad => entidad.Id == 1);
            anulado.Estado = "Anulado";
        });
        await DatosPlanillas.CalcularOkAsync(_cliente, id);
        Assert.Equal(0, await DatosPlanillas.ContarDescuentosAsync(_fabrica));
        Assert.Equal(0m, (await DatosPlanillas.LeerDetallesAsync(_cliente, id))[0].DescuentoPrestamos);
    }

    [Fact]
    public async Task ElPrestamoSeLimitaAlNetoRestanteDelEmpleado()
    {
        await DatosPlanillas.InsertarPrestamoAsync(_fabrica, 1, 1, (2026, 10, 20000m));
        int id = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");

        await DatosPlanillas.CalcularOkAsync(_cliente, id);

        // Empleado 1 sin bono: ingresos 9500.00; pension 950.00 + 147.25 + 130.15 = 1227.40; quinta 9112.00 / 12 = 759.33.
        // Neto antes del prestamo 9500.00 - 1227.40 - 759.33 = 7513.27: se descuenta todo y el neto queda en 0.
        PlanillaEmpleadoDto detalle = (await DatosPlanillas.LeerDetallesAsync(_cliente, id)).Single(fila => fila.EmpleadoId == 1);
        Assert.Equal(1227.40m, detalle.DescuentoPension);
        Assert.Equal(759.33m, detalle.DescuentoQuinta);
        Assert.Equal(7513.27m, detalle.DescuentoPrestamos);
        Assert.Equal(9500.00m, detalle.TotalDescuentos);
        Assert.Equal(0m, detalle.Neto);
        Prestamo prestamo = await DatosPlanillas.LeerPrestamoAsync(_fabrica, 1);
        Assert.Equal(12486.73m, prestamo.Saldo);
        Assert.Equal("Pendiente", prestamo.CuotasPrestamo.Single().Estado);
    }

    [Fact]
    public async Task ElCalculoLeeFaltasPermisosSinGoceHorasExtraYElSueldoDeLaAdenda()
    {
        await DatosPlanillas.ModificarAsync(_fabrica, contexto =>
        {
            contexto.AdendasContrato.Add(new AdendaContrato { ContratoLaboralId = 2, FechaVigencia = new DateOnly(2026, 10, 20), Cargo = "Analista Senior", SueldoBasico = 5000m });
            contexto.JornadasAsistencia.AddRange(
                Jornada(1, 2, new DateOnly(2026, 10, 1), "Falta", 0, "NoAplica"),
                Jornada(2, 2, new DateOnly(2026, 10, 2), "Falta", 0, "NoAplica"),
                Jornada(3, 2, new DateOnly(2026, 10, 6), "Asistencia", 90, "Aprobada"),
                Jornada(4, 2, new DateOnly(2026, 10, 7), "Asistencia", 180, "Aprobada"),
                Jornada(5, 2, new DateOnly(2026, 10, 8), "Asistencia", 60, "Pendiente"),
                Jornada(6, 2, new DateOnly(2026, 9, 30), "Falta", 0, "NoAplica"));
            contexto.Ausencias.AddRange(
                Ausencia(1, 2, "PermisoSinGoce", new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 13), "Aprobada"),
                Ausencia(2, 2, "PermisoSinGoce", new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 14), "Pendiente"),
                Ausencia(3, 2, "DescansoMedico", new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 16), "Aprobada"));
        });
        int id = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");

        await DatosPlanillas.CalcularOkAsync(_cliente, id);

        // Empleado 2: sueldo 5000 (adenda vigente el ultimo dia). Dias 30 - 2 (sin goce) - 2 (faltas de octubre) = 26: 5000 x 26 / 30 = 4333.33.
        // Horas extra: valor hora 5000 / 30 / 8 = 20.8333; jornadas de 90 y 180 min: (112.5 + 231) / 60 x 20.8333 = 119.27.
        // Ingresos 4452.60; ONP 578.838 => 578.84; quinta (53431.20 - 37450 = 15981.20) x 0.08 = 1278.496 / 12 = 106.54;
        // neto 4452.60 - 578.84 - 106.54 = 3767.22; EsSalud 400.734 => 400.73.
        PlanillaEmpleadoDto detalle = (await DatosPlanillas.LeerDetallesAsync(_cliente, id)).Single(fila => fila.EmpleadoId == 2);
        Assert.Equal(5000.00m, detalle.SueldoBasico);
        Assert.Equal("Analista Senior", detalle.Cargo);
        Assert.Equal(30, detalle.DiasContrato);
        Assert.Equal(2, detalle.DiasSinGoce);
        Assert.Equal(2, detalle.DiasFalta);
        Assert.Equal(26, detalle.DiasLaborados);
        Assert.Equal(4333.33m, detalle.IngresoBasico);
        Assert.Equal(270, detalle.MinutosExtra);
        Assert.Equal(119.27m, detalle.IngresoHorasExtra);
        Assert.Equal(4452.60m, detalle.TotalIngresos);
        Assert.Equal(578.84m, detalle.DescuentoPension);
        Assert.Equal(106.54m, detalle.DescuentoQuinta);
        Assert.Equal(3767.22m, detalle.Neto);
        Assert.Equal(400.73m, detalle.AporteEssalud);
    }

    [Fact]
    public async Task LaCoberturaDelMesIgnoraContratosAnuladosYRespetaLaAltaYElCese()
    {
        await DatosPlanillas.AfiliarAsync(_fabrica, 3, 1);
        await DatosPlanillas.AfiliarAsync(_fabrica, 4, 1);
        await DatosPlanillas.ModificarAsync(_fabrica, contexto =>
        {
            contexto.ContratosLaborales.AddRange(
                DatosPrestamos.CrearContrato(5, 3, "Indeterminado", new DateOnly(2026, 10, 15), null, 3000m, "Vigente", null),
                DatosPrestamos.CrearContrato(6, 1, "Indeterminado", new DateOnly(2026, 10, 1), null, 99999m, "Anulado", new DateOnly(2026, 10, 2)));
            contexto.Empleados.Single(empleado => empleado.Id == 4).FechaCese = new DateOnly(2026, 9, 19);
        });
        int octubre = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        int septiembre = await DatosPlanillas.CrearAsync(_cliente, 2026, 9, "Mensual");

        await DatosPlanillas.CalcularOkAsync(_cliente, octubre);
        await DatosPlanillas.CalcularOkAsync(_cliente, septiembre);

        // Alta el 15/10: 17 dias cubiertos, 3000 x 17 / 30 = 1700.00; ONP 221.00; quinta 0; neto 1479.00.
        List<PlanillaEmpleadoDto> detallesOctubre = await DatosPlanillas.LeerDetallesAsync(_cliente, octubre);
        Assert.Equal(new[] { 1, 2, 3 }, detallesOctubre.Select(detalle => detalle.EmpleadoId).ToArray());
        PlanillaEmpleadoDto alta = detallesOctubre.Single(detalle => detalle.EmpleadoId == 3);
        Assert.Equal(17, alta.DiasContrato);
        Assert.Equal(1700.00m, alta.IngresoBasico);
        Assert.Equal(221.00m, alta.DescuentoPension);
        Assert.Equal(1479.00m, alta.Neto);
        Assert.Equal(9500.00m, detallesOctubre.Single(detalle => detalle.EmpleadoId == 1).SueldoBasico);
        // Cese el 19/09 con contrato cerrado el 30/09: 19 dias, 2600 x 19 / 30 = 1646.666667 => 1646.67; ONP 214.0671 => 214.07; neto 1432.60.
        List<PlanillaEmpleadoDto> detallesSeptiembre = await DatosPlanillas.LeerDetallesAsync(_cliente, septiembre);
        PlanillaEmpleadoDto cesado = detallesSeptiembre.Single(detalle => detalle.EmpleadoId == 4);
        Assert.Equal(19, cesado.DiasContrato);
        Assert.Equal(1646.67m, cesado.IngresoBasico);
        Assert.Equal(214.07m, cesado.DescuentoPension);
        Assert.Equal(1432.60m, cesado.Neto);
        Assert.DoesNotContain(detallesSeptiembre, detalle => detalle.EmpleadoId == 3);
    }

    [Fact]
    public async Task CalcularYAprobarValidanElEstadoYLaExistencia()
    {
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CalcularAsync(_cliente, 999), 404, "La planilla 999 no existe.");
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.AprobarAsync(_cliente, 999), 404, "La planilla 999 no existe.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/999"), 404, "La planilla 999 no existe.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/999/detalles"), 404, "La planilla 999 no existe.");
        int borrador = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.AprobarAsync(_cliente, borrador),
            409,
            "La planilla PLA-202610-M debe estar calculada para aprobarse.");
        int vacia = await DatosPlanillas.CrearAsync(_cliente, 2000, 1, "Mensual");
        PlanillaCalculoDto calculo = await DatosPlanillas.CalcularOkAsync(_cliente, vacia);
        Assert.Equal(0, calculo.Empleados);
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.AprobarAsync(_cliente, vacia),
            409,
            "La planilla PLA-200001-M no tiene empleados calculados.");
    }

    [Fact]
    public async Task UnaPlanillaAprobadaEsInmutableYGeneraBoletasUnicas()
    {
        await PrepararOctubreAsync();
        int id = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        await DatosPlanillas.CalcularOkAsync(_cliente, id);
        List<PlanillaEmpleadoDto> antes = await DatosPlanillas.LeerDetallesAsync(_cliente, id);
        await DatosPrestamos.AssertErrorAsync(
            await _cliente.GetAsync($"{RutaBase}/boletas/{antes[0].Id}"),
            409,
            "La boleta se genera al aprobar la planilla.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/boletas/9999"), 404, "El detalle de planilla 9999 no existe.");

        HttpResponseMessage aprobada = await DatosPlanillas.AprobarAsync(_cliente, id);

        Assert.Equal(HttpStatusCode.OK, aprobada.StatusCode);
        PlanillaDetalleDto planilla = await DatosPlanillas.LeerPlanillaAsync(_cliente, id);
        Assert.Equal("Aprobada", planilla.Estado);
        Assert.Equal("pruebas", planilla.AprobadoPor);
        Assert.NotNull(planilla.FechaAprobacion);
        List<PlanillaEmpleadoDto> despues = await DatosPlanillas.LeerDetallesAsync(_cliente, id);
        Assert.Equal("BOL-202610-M-000001", despues[0].NumeroBoleta);
        Assert.Equal("BOL-202610-M-000002", despues[1].NumeroBoleta);
        Assert.Null(despues[2].NumeroBoleta);
        Assert.Equal(2, despues.Where(detalle => detalle.NumeroBoleta is not null).Select(detalle => detalle.NumeroBoleta).Distinct().Count());
        BoletaPagoDto? boleta = await _cliente.GetFromJsonAsync<BoletaPagoDto>($"{RutaBase}/boletas/{despues[0].Id}");
        Assert.NotNull(boleta);
        Assert.Equal("TitanGlobal", boleta.Empresa);
        Assert.Equal("BOL-202610-M-000001", boleta.NumeroBoleta);
        Assert.Equal("PLA-202610-M", boleta.NumeroPlanilla);
        Assert.Equal(10, boleta.Mes);
        Assert.Equal(2026, boleta.Anio);
        Assert.Equal("EMP-000001", boleta.EmpleadoLegajo);
        Assert.Equal(10000.00m, boleta.TotalIngresos);
        Assert.Equal(2321.33m, boleta.TotalDescuentos);
        Assert.Equal(900.00m, boleta.AporteEssalud);
        Assert.Equal(7678.67m, boleta.Neto);
        await DatosPrestamos.AssertErrorAsync(
            await _cliente.GetAsync($"{RutaBase}/boletas/{despues[2].Id}"),
            409,
            "La boleta se genera al aprobar la planilla.");

        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.CalcularAsync(_cliente, id),
            409,
            "La planilla PLA-202610-M está aprobada y no se modifica.");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.AprobarAsync(_cliente, id),
            409,
            "La planilla PLA-202610-M debe estar calculada para aprobarse.");
        Assert.Equal(1, await DatosPlanillas.ContarDescuentosAsync(_fabrica));
        Assert.Equal(7678.67m, (await DatosPlanillas.LeerDetallesAsync(_cliente, id))[0].Neto);
        List<BonoPlanilla> bonos = await DatosPlanillas.LeerBonosAsync(_fabrica);
        await DatosPrestamos.AssertErrorAsync(
            await _cliente.DeleteAsync($"/api/v1/bonos-planilla/{bonos[0].Id}"),
            409,
            "El bono está en una planilla aprobada y no se puede eliminar.");
    }

    // Mensajes 7, 8, 11 y 12 del pago.
    [Fact]
    public async Task ElPagoValidaLaFechaElEstadoYSeRegistraUnaSolaVez()
    {
        await PrepararOctubreAsync();
        int id = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.PagarAsync(_cliente, id, null), 400, "La fecha de pago es obligatoria.");
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.PagarAsync(_cliente, 999, hoy), 404, "La planilla 999 no existe.");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.PagarAsync(_cliente, id, hoy),
            409,
            "La planilla PLA-202610-M debe estar aprobada para registrar el pago.");
        await DatosPlanillas.CalcularOkAsync(_cliente, id);
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.PagarAsync(_cliente, id, hoy),
            409,
            "La planilla PLA-202610-M debe estar aprobada para registrar el pago.");
        Assert.Equal(HttpStatusCode.OK, (await DatosPlanillas.AprobarAsync(_cliente, id)).StatusCode);
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.PagarAsync(_cliente, id, hoy.AddDays(-1)),
            400,
            "La fecha de pago no puede ser anterior a la aprobación.");

        HttpResponseMessage pagada = await DatosPlanillas.PagarAsync(_cliente, id, hoy);

        Assert.Equal(HttpStatusCode.OK, pagada.StatusCode);
        PlanillaDetalleDto planilla = await DatosPlanillas.LeerPlanillaAsync(_cliente, id);
        Assert.Equal("Pagada", planilla.Estado);
        Assert.Equal(hoy, planilla.FechaPago);
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.PagarAsync(_cliente, id, hoy),
            409,
            "La planilla PLA-202610-M debe estar aprobada para registrar el pago.");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.CalcularAsync(_cliente, id),
            409,
            "La planilla PLA-202610-M está aprobada y no se modifica.");
    }

    [Fact]
    public async Task LaComplementariaIncluyeSoloSusBonosConPensionYQuintaSinPrestamos()
    {
        await PrepararOctubreAsync();
        int mensual = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        await DatosPlanillas.CalcularOkAsync(_cliente, mensual);
        await DatosPlanillas.AprobarAsync(_cliente, mensual);
        await DatosPlanillas.InsertarBonoAsync(_fabrica, 2, 2026, 10, "Complementaria", 1000m);
        int complementaria = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Complementaria");

        PlanillaCalculoDto calculo = await DatosPlanillas.CalcularOkAsync(_cliente, complementaria);

        // Empleado 2: bono 1000.00; ONP 130.00; quinta (12000 - 37450 < 0) 0; neto 870.00; EsSalud 90.00. Sin basico ni prestamos.
        Assert.Equal(1, calculo.Empleados);
        Assert.Equal(0, calculo.Incidencias);
        PlanillaEmpleadoDto detalle = Assert.Single(await DatosPlanillas.LeerDetallesAsync(_cliente, complementaria));
        Assert.Equal(2, detalle.EmpleadoId);
        Assert.Equal(0m, detalle.IngresoBasico);
        Assert.Equal(1000.00m, detalle.TotalIngresos);
        Assert.Equal(130.00m, detalle.DescuentoPension);
        Assert.Equal(0m, detalle.DescuentoQuinta);
        Assert.Equal(0m, detalle.DescuentoPrestamos);
        Assert.Equal(870.00m, detalle.Neto);
        Assert.Equal(90.00m, detalle.AporteEssalud);
        Assert.Equal(1, await DatosPlanillas.ContarDescuentosAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await DatosPlanillas.AprobarAsync(_cliente, complementaria)).StatusCode);
        PlanillaEmpleadoDto aprobado = Assert.Single(await DatosPlanillas.LeerDetallesAsync(_cliente, complementaria));
        Assert.Equal("BOL-202610-C-000002", aprobado.NumeroBoleta);
        Assert.Equal("PLA-202610-C", (await DatosPlanillas.LeerPlanillaAsync(_cliente, complementaria)).Numero);
    }

    [Fact]
    public async Task ListadoResumenYParametrosDevuelvenLosDatosYValidanLosFiltros()
    {
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?tipo=Otro"), 400, "El filtro tipo no es válido.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}?estado=Otro"), 400, "El filtro estado no es válido.");
        await DatosPrestamos.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/1/detalles?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        ResumenPlanillasDto? vacio = await _cliente.GetFromJsonAsync<ResumenPlanillasDto>($"{RutaBase}/resumen");
        Assert.NotNull(vacio);
        Assert.Null(vacio.UltimoPeriodo);
        Assert.Equal(0, vacio.BorradoresYCalculadas);

        await PrepararOctubreAsync();
        int octubre = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        await DatosPlanillas.CalcularOkAsync(_cliente, octubre);
        await DatosPlanillas.CrearAsync(_cliente, 2026, 9, "Mensual");

        PaginaPlanillasDto? todas = await _cliente.GetFromJsonAsync<PaginaPlanillasDto>($"{RutaBase}?anio=2026");
        Assert.NotNull(todas);
        Assert.Equal(new[] { "PLA-202610-M", "PLA-202609-M" }, todas.Items.Select(item => item.Numero).ToArray());
        PaginaPlanillasDto? calculadas = await _cliente.GetFromJsonAsync<PaginaPlanillasDto>($"{RutaBase}?estado=Calculada&tipo=Mensual");
        Assert.NotNull(calculadas);
        Assert.Equal(octubre, Assert.Single(calculadas.Items).Id);
        PaginaPlanillasDto? otroAnio = await _cliente.GetFromJsonAsync<PaginaPlanillasDto>($"{RutaBase}?anio=2025");
        Assert.NotNull(otroAnio);
        Assert.Empty(otroAnio.Items);
        ResumenPlanillasDto? resumen = await _cliente.GetFromJsonAsync<ResumenPlanillasDto>($"{RutaBase}/resumen");
        Assert.NotNull(resumen);
        Assert.Equal("2026-10", resumen.UltimoPeriodo);
        Assert.Equal("Calculada", resumen.EstadoUltimo);
        Assert.Equal(11246.34m, resumen.NetoUltimo);
        Assert.Equal(2, resumen.EmpleadosUltimo);
        Assert.Equal(1, resumen.IncidenciasUltimo);
        Assert.Equal(2, resumen.BorradoresYCalculadas);
        ParametrosPlanillaDto? parametros = await _cliente.GetFromJsonAsync<ParametrosPlanillaDto>($"{RutaBase}/parametros");
        Assert.NotNull(parametros);
        Assert.Equal(7, parametros.Parametros.Count);
        Assert.Equal(5350m, parametros.Parametros.Single(parametro => parametro.Clave == "UIT").Valor);
        Assert.Equal(5, parametros.Pensiones.Count);
        Assert.Equal(new decimal?[] { 5m, 20m, 35m, 45m, null }, parametros.Tramos.Select(tramo => tramo.HastaUit).ToArray());
    }

    [Fact]
    public async Task LosCambiosDeLaPlanillaGeneranRegistrosDeAuditoria()
    {
        await PrepararOctubreAsync();
        int id = await DatosPlanillas.CrearAsync(_cliente, 2026, 10, "Mensual");
        Assert.Single(await DatosPlanillas.LeerAuditoriaAsync(_fabrica, "Planilla", id), registro => registro.Accion == "Alta");

        await DatosPlanillas.CalcularOkAsync(_cliente, id);
        await DatosPlanillas.AprobarAsync(_cliente, id);

        List<RegistroAuditoria> registros = await DatosPlanillas.LeerAuditoriaAsync(_fabrica, "Planilla", id);
        Assert.Contains(registros, registro => registro.Accion == "Modificacion" && registro.Cambios.Contains("CalculadoPor"));
        Assert.Contains(registros, registro => registro.Accion == "Modificacion" && registro.Cambios.Contains("AprobadoPor"));
    }

    [Fact]
    public async Task SoloRrhhYAdminEscribenYCualquierAutenticadoConsulta()
    {
        HttpClient consulta = DatosPlanillas.ClienteConRoles(_fabrica, "CONSULTA");
        await DatosPrestamos.AssertErrorAsync(
            await DatosPlanillas.CrearPlanillaAsync(consulta, 2026, 10, "Mensual"),
            403,
            DatosPrestamos.MensajeSinPermiso);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.CalcularAsync(consulta, 1), 403, DatosPrestamos.MensajeSinPermiso);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.AprobarAsync(consulta, 1), 403, DatosPrestamos.MensajeSinPermiso);
        await DatosPrestamos.AssertErrorAsync(await DatosPlanillas.PagarAsync(consulta, 1, new DateOnly(2026, 11, 1)), 403, DatosPrestamos.MensajeSinPermiso);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/parametros")).StatusCode);
        HttpClient anonimo = DatosPrestamos.ClienteAnonimo(_fabrica);
        await DatosPrestamos.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, DatosPrestamos.MensajeSinSesion);
        foreach (string rol in new[] { "RRHH", "ADMIN" })
        {
            HttpClient escritor = DatosPlanillas.ClienteConRoles(_fabrica, rol);
            Assert.Equal(HttpStatusCode.OK, (await DatosPlanillas.CrearPlanillaAsync(escritor, rol == "RRHH" ? 2026 : 2025, 10, "Mensual")).StatusCode);
        }
    }

    // Empleados 1 y 2 con contratos vigentes y afiliaciones; el 3 con un contrato de octubre pero sin afiliación; un bono de 500 para el 1
    // y un préstamo de 600 del empleado 1 en tres cuotas de 200 desde octubre de 2026.
    private async Task PrepararOctubreAsync()
    {
        await DatosPlanillas.ModificarAsync(_fabrica, contexto => contexto.ContratosLaborales.Add(
            DatosPrestamos.CrearContrato(5, 3, "Indeterminado", new DateOnly(2026, 1, 1), null, 1500m, "Vigente", null)));
        await DatosPlanillas.InsertarBonoAsync(_fabrica, 1, 2026, 10, "Mensual", 500m);
        await DatosPlanillas.InsertarPrestamoAsync(_fabrica, 1, 1, (2026, 10, 200m), (2026, 11, 200m), (2026, 12, 200m));
    }

    private static JornadaAsistencia Jornada(int id, int empleadoId, DateOnly fecha, string estado, int minutosExtra, string estadoHorasExtra)
    {
        return new JornadaAsistencia
        {
            Id = id,
            EmpleadoId = empleadoId,
            Fecha = fecha,
            ToleranciaMinutos = 10,
            Estado = estado,
            MinutosExtra = minutosExtra,
            EstadoHorasExtra = estadoHorasExtra
        };
    }

    private static Ausencia Ausencia(int id, int empleadoId, string tipo, DateOnly inicio, DateOnly fin, string estado)
    {
        return new Ausencia
        {
            Id = id,
            Numero = "AUS-" + id.ToString("D6", System.Globalization.CultureInfo.InvariantCulture),
            EmpleadoId = empleadoId,
            Tipo = tipo,
            FechaInicio = inicio,
            FechaFin = fin,
            Dias = fin.DayNumber - inicio.DayNumber + 1,
            Estado = estado,
            FechaSolicitud = inicio
        };
    }
}
