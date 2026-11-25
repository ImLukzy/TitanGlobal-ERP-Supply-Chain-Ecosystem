using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo65_VacacionesPermisos;

[Collection("Modulo04")]
public class AusenciasApiTests
{
    private const string RutaBase = "/api/v1/ausencias";

    private static readonly DateOnly Futuro = new DateOnly(2030, 6, 3);

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public AusenciasApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosAusencias.Restablecer(fabrica);
    }

    // Mensajes 1 a 9 del registro, en el orden de la spec.
    [Fact]
    public async Task RegistroConDatosInvalidosDevuelveMensajesLiteralesSinFilasNuevas()
    {
        DateOnly inicio = new DateOnly(2030, 6, 3);
        string tipoInvalido = "El tipo debe ser Vacaciones, PermisoConGoce, PermisoSinGoce, LicenciaMaternidad, LicenciaPaternidad o DescansoMedico.";
        await AssertRegistroAsync(null, "Vacaciones", inicio, inicio, null, 400, "El empleado es obligatorio.");
        await AssertRegistroAsync(1, null, inicio, inicio, null, 400, tipoInvalido);
        await AssertRegistroAsync(1, "Otro", inicio, inicio, null, 400, tipoInvalido);
        await AssertRegistroAsync(1, "Vacaciones", null, inicio, null, 400, "La fecha de inicio es obligatoria.");
        await AssertRegistroAsync(1, "Vacaciones", inicio, null, null, 400, "La fecha de fin es obligatoria.");
        await AssertRegistroAsync(1, "Vacaciones", inicio, inicio.AddDays(-1), null, 400, "La fecha de fin no puede ser anterior a la fecha de inicio.");
        string motivoObligatorio = "El motivo es obligatorio para este tipo de ausencia.";
        await AssertRegistroAsync(1, "PermisoConGoce", inicio, inicio, null, 400, motivoObligatorio);
        await AssertRegistroAsync(1, "PermisoSinGoce", inicio, inicio, "   ", 400, motivoObligatorio);
        await AssertRegistroAsync(1, "DescansoMedico", inicio, inicio, "", 400, motivoObligatorio);
        await AssertRegistroAsync(1, "PermisoConGoce", inicio, inicio, new string('a', 301), 400, "El motivo no puede superar 300 caracteres.");
        await AssertRegistroAsync(1, "LicenciaMaternidad", inicio, inicio.AddDays(98), null, 400, "La licencia de maternidad no puede superar 98 días.");
        await AssertRegistroAsync(1, "LicenciaPaternidad", inicio, inicio.AddDays(10), null, 400, "La licencia de paternidad no puede superar 10 días.");
        Assert.Empty(await DatosAusencias.LeerAsync(_fabrica));
        Assert.Equal(HttpStatusCode.OK, (await RegistrarAsync(1, "LicenciaMaternidad", inicio, inicio.AddDays(97), null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RegistrarAsync(2, "LicenciaPaternidad", inicio, inicio.AddDays(9), null)).StatusCode);
    }

    // Mensajes 10 a 14 del registro.
    [Fact]
    public async Task RegistroContraElEmpleadoYElSaldoDevuelveMensajesLiteralesSinFilasNuevas()
    {
        await DatosAusencias.AssertErrorAsync(
            await RegistrarAsync(9999, "Vacaciones", Futuro, Futuro, null),
            404,
            "El empleado 9999 no existe.");
        await DatosAusencias.AssertErrorAsync(
            await RegistrarAsync(4, "Vacaciones", Futuro, Futuro, null),
            409,
            "El empleado 4 está cesado y no admite ausencias.");
        await DatosAusencias.AssertErrorAsync(
            await RegistrarAsync(3, "PermisoConGoce", new DateOnly(2024, 1, 7), new DateOnly(2024, 1, 8), "Trámite"),
            400,
            "La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(3, "PermisoConGoce", new DateOnly(2030, 6, 10), new DateOnly(2030, 6, 12), "Pendiente"));
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(3, "PermisoSinGoce", new DateOnly(2030, 7, 10), new DateOnly(2030, 7, 12), "Aprobada"));
        string solape = "La ausencia se cruza con otra ausencia pendiente o aprobada del empleado.";
        await DatosAusencias.AssertErrorAsync(
            await RegistrarAsync(3, "PermisoConGoce", new DateOnly(2030, 6, 12), new DateOnly(2030, 6, 14), "Trámite"),
            409,
            solape);
        await DatosAusencias.AssertErrorAsync(
            await RegistrarAsync(3, "PermisoConGoce", new DateOnly(2030, 7, 1), new DateOnly(2030, 7, 10), "Trámite"),
            409,
            solape);
        // Empleado 3: 2 meses completos a 2024-03-08 = 5 días de saldo.
        await DatosAusencias.AssertErrorAsync(
            await RegistrarAsync(3, "Vacaciones", new DateOnly(2024, 3, 8), new DateOnly(2024, 3, 13), null),
            409,
            "El saldo vacacional es de 5.0 días y la solicitud pide 6.");
        Assert.Equal(2, (await DatosAusencias.LeerAsync(_fabrica)).Count);
        Assert.Equal(HttpStatusCode.OK, (await RegistrarAsync(3, "Vacaciones", new DateOnly(2024, 3, 8), new DateOnly(2024, 3, 12), null)).StatusCode);
    }

    [Fact]
    public async Task RegistroGuardaLaSolicitudPendienteConNumeroDefinitivo()
    {
        HttpResponseMessage respuesta = await RegistrarAsync(1, " PermisoConGoce ", Futuro, Futuro.AddDays(1), "  Mudanza  ");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        AusenciaCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<AusenciaCreadaDto>();
        Assert.NotNull(creada);
        Assert.Equal("AUS-" + creada.Id.ToString("D6"), creada.Numero);
        Ausencia guardada = Assert.Single(await DatosAusencias.LeerAsync(_fabrica));
        Assert.Equal(creada.Numero, guardada.Numero);
        Assert.Equal("PermisoConGoce", guardada.Tipo);
        Assert.Equal(2, guardada.Dias);
        Assert.Equal("Mudanza", guardada.Motivo);
        Assert.Equal("Pendiente", guardada.Estado);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), guardada.FechaSolicitud);
        Assert.Null(guardada.DecididoPor);
        Assert.Null(guardada.FechaDecision);
        Assert.Null(guardada.FechaCancelacion);
    }

    [Fact]
    public async Task VacacionesSinMotivoGuardanElMotivoNulo()
    {
        HttpResponseMessage respuesta = await RegistrarAsync(1, "Vacaciones", Futuro, Futuro.AddDays(4), "  ");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Ausencia guardada = Assert.Single(await DatosAusencias.LeerAsync(_fabrica));
        Assert.Null(guardada.Motivo);
        Assert.Equal(5, guardada.Dias);
    }

    [Fact]
    public async Task AprobarGuardaElUsuarioDelTokenYLaFechaDeDecision()
    {
        int id = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        DateTime antes = DateTime.UtcNow.AddSeconds(-1);
        HttpResponseMessage respuesta = await _cliente.PostAsync($"{RutaBase}/{id}/aprobacion", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        AusenciaIdDto? resultado = await respuesta.Content.ReadFromJsonAsync<AusenciaIdDto>();
        Assert.Equal(id, resultado?.Id);
        Ausencia guardada = Assert.Single(await DatosAusencias.LeerAsync(_fabrica));
        Assert.Equal("Aprobada", guardada.Estado);
        Assert.Equal(TokenPruebas.NombreUsuario, guardada.DecididoPor);
        Assert.NotNull(guardada.FechaDecision);
        Assert.True(guardada.FechaDecision >= antes);
        Assert.Null(guardada.MotivoRechazo);
    }

    [Fact]
    public async Task RechazarGuardaElMotivoYElUsuarioDelToken()
    {
        int id = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/{id}/rechazo",
            new RechazarAusenciaRequestDto { Motivo = "  Falta de personal  " });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Ausencia guardada = Assert.Single(await DatosAusencias.LeerAsync(_fabrica));
        Assert.Equal("Rechazada", guardada.Estado);
        Assert.Equal("Falta de personal", guardada.MotivoRechazo);
        Assert.Equal(TokenPruebas.NombreUsuario, guardada.DecididoPor);
        Assert.NotNull(guardada.FechaDecision);
    }

    // Mensajes 15 a 20 de aprobación, rechazo y cancelación.
    [Fact]
    public async Task AprobarRechazarYCancelarConEstadoInvalidoDevuelvenMensajesLiteralesSinCambios()
    {
        int pendiente = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        int aprobada = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro.AddDays(10), Futuro.AddDays(11), "Aprobada"));
        int rechazada = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(2, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Rechazada"));
        int cancelada = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(2, "PermisoConGoce", Futuro.AddDays(5), Futuro.AddDays(6), "Cancelada"));
        int empezada = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(3, "PermisoConGoce", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 13), "Aprobada"));
        string[] antes = await DatosAusencias.LeerFirmasAsync(_fabrica);
        List<Ausencia> filas = await DatosAusencias.LeerAsync(_fabrica);
        string NumeroDe(int id)
        {
            return filas.Single(fila => fila.Id == id).Numero;
        }

        await DatosAusencias.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/aprobacion", null), 404, "La ausencia 9999 no existe.");
        await DatosAusencias.AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/9999/rechazo", new RechazarAusenciaRequestDto { Motivo = "x" }),
            404,
            "La ausencia 9999 no existe.");
        await DatosAusencias.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/9999/cancelacion", null), 404, "La ausencia 9999 no existe.");
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/9999"), 404, "La ausencia 9999 no existe.");
        foreach (int id in new[] { aprobada, rechazada, cancelada })
        {
            string mensaje = $"La ausencia {NumeroDe(id)} no está pendiente.";
            await DatosAusencias.AssertErrorAsync(await _cliente.PostAsync($"{RutaBase}/{id}/aprobacion", null), 409, mensaje);
            await DatosAusencias.AssertErrorAsync(
                await _cliente.PostAsJsonAsync($"{RutaBase}/{id}/rechazo", new RechazarAusenciaRequestDto { Motivo = "x" }),
                409,
                mensaje);
        }

        await DatosAusencias.AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{pendiente}/rechazo", new RechazarAusenciaRequestDto { Motivo = "  " }),
            400,
            "El motivo del rechazo es obligatorio.");
        await DatosAusencias.AssertErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaBase}/{pendiente}/rechazo", new RechazarAusenciaRequestDto { Motivo = new string('a', 301) }),
            400,
            "El motivo del rechazo no puede superar 300 caracteres.");
        foreach (int id in new[] { rechazada, cancelada })
        {
            await DatosAusencias.AssertErrorAsync(
                await _cliente.PostAsync($"{RutaBase}/{id}/cancelacion", null),
                409,
                $"La ausencia {NumeroDe(id)} ya está rechazada o cancelada.");
        }

        await DatosAusencias.AssertErrorAsync(
            await _cliente.PostAsync($"{RutaBase}/{empezada}/cancelacion", null),
            409,
            $"La ausencia {NumeroDe(empezada)} ya empezó y no se puede cancelar.");
        Assert.Equal(antes, await DatosAusencias.LeerFirmasAsync(_fabrica));
    }

    [Fact]
    public async Task CancelarUnaAusenciaFuturaPendienteOAprobadaLaMarcaCancelada()
    {
        int pendiente = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        int aprobada = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro.AddDays(10), Futuro.AddDays(11), "Aprobada"));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{pendiente}/cancelacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{aprobada}/cancelacion", null)).StatusCode);
        List<Ausencia> filas = await DatosAusencias.LeerAsync(_fabrica);
        Assert.All(filas, fila => Assert.Equal("Cancelada", fila.Estado));
        Assert.All(filas, fila => Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), fila.FechaCancelacion));
        // Una ausencia cancelada libera las fechas.
        Assert.Equal(HttpStatusCode.OK, (await RegistrarAsync(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Otra vez")).StatusCode);
    }

    [Fact]
    public async Task ElSaldoSeComprobarTambienAlAprobar()
    {
        // Empleado 3 a 2024-03-08: 5 días devengados; dos pendientes de 5 días (insertadas sin pasar por el registro).
        int primera = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(3, "Vacaciones", new DateOnly(2024, 3, 8), new DateOnly(2024, 3, 12), "Pendiente"));
        int segunda = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(3, "Vacaciones", new DateOnly(2030, 3, 20), new DateOnly(2030, 3, 24), "Pendiente"));
        string[] antes = await DatosAusencias.LeerFirmasAsync(_fabrica);
        await DatosAusencias.AssertErrorAsync(
            await _cliente.PostAsync($"{RutaBase}/{primera}/aprobacion", null),
            409,
            "El saldo vacacional es de 0.0 días y la solicitud pide 5.");
        Assert.Equal(antes, await DatosAusencias.LeerFirmasAsync(_fabrica));
        // Al cancelar la otra pendiente el saldo vuelve a cubrir la solicitud.
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{segunda}/cancelacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{primera}/aprobacion", null)).StatusCode);
    }

    [Fact]
    public async Task AprobarVerificaDeNuevoElSolape()
    {
        int primera = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(3), "Pendiente"));
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro.AddDays(2), Futuro.AddDays(5), "Aprobada"));
        await DatosAusencias.AssertErrorAsync(
            await _cliente.PostAsync($"{RutaBase}/{primera}/aprobacion", null),
            409,
            "La ausencia se cruza con otra ausencia pendiente o aprobada del empleado.");
    }

    [Fact]
    public async Task AprobarVacacionesConSaldoSuficienteFunciona()
    {
        int id = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(3, "Vacaciones", new DateOnly(2024, 3, 8), new DateOnly(2024, 3, 12), "Pendiente"));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/aprobacion", null)).StatusCode);
        Assert.Equal("Aprobada", Assert.Single(await DatosAusencias.LeerAsync(_fabrica)).Estado);
    }

    [Fact]
    public async Task SoloLasVacacionesConsumenSaldo()
    {
        // El empleado 3 no tiene saldo a 2024-01-10, pero un permiso no lo necesita.
        HttpResponseMessage respuesta = await RegistrarAsync(3, "PermisoSinGoce", new DateOnly(2024, 1, 10), new DateOnly(2024, 1, 20), "Estudios");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task SaldoVacacionalCalculaDevengadoGozadoYProgramado()
    {
        // Empleado 1, ingreso 2021-03-01. A 2026-11-22: 68 meses completos = 170 días.
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "Vacaciones", new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 13), "Aprobada"));
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "Vacaciones", new DateOnly(2027, 1, 4), new DateOnly(2027, 1, 10), "Aprobada"));
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "Vacaciones", new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3), "Pendiente"));
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "Vacaciones", new DateOnly(2027, 5, 1), new DateOnly(2027, 5, 3), "Rechazada"));
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "Vacaciones", new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 3), "Cancelada"));
        await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 5), "Aprobada"));
        HttpResponseMessage respuesta = await _cliente.GetAsync($"{RutaBase}/saldo-vacacional/1?fecha=2026-11-22");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        SaldoVacacionalDto? saldo = await respuesta.Content.ReadFromJsonAsync<SaldoVacacionalDto>();
        Assert.NotNull(saldo);
        Assert.Equal(1, saldo.EmpleadoId);
        Assert.Equal(new DateOnly(2021, 3, 1), saldo.FechaIngreso);
        Assert.Equal(new DateOnly(2026, 11, 22), saldo.Fecha);
        Assert.Equal(68, saldo.MesesServicio);
        Assert.Equal(170.0m, saldo.DiasDevengados);
        Assert.Equal(12m, saldo.DiasGozados);
        Assert.Equal(10m, saldo.DiasProgramados);
        Assert.Equal(148.0m, saldo.Saldo);
        string contenido = await (await _cliente.GetAsync($"{RutaBase}/saldo-vacacional/1?fecha=2027-01-06")).Content.ReadAsStringAsync();
        using JsonDocument documento = JsonDocument.Parse(contenido);
        Assert.Equal(15m, documento.RootElement.GetProperty("diasGozados").GetDecimal());
        Assert.Equal(7m, documento.RootElement.GetProperty("diasProgramados").GetDecimal());
    }

    [Fact]
    public async Task SaldoVacacionalSinFechaUsaHoyYEmpleadoInexistenteDevuelve404()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync($"{RutaBase}/saldo-vacacional/2");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        SaldoVacacionalDto? saldo = await respuesta.Content.ReadFromJsonAsync<SaldoVacacionalDto>();
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), saldo?.Fecha);
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/saldo-vacacional/9999"), 404, "El empleado 9999 no existe.");
    }

    // Mensajes 21 a 23 y 10 del listado.
    [Fact]
    public async Task ListadoConParametrosInvalidosDevuelveMensajesLiterales()
    {
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=0"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?tipo=Otro"), 400, "El filtro tipo no es válido.");
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?estado=Otro"), 400, "El filtro estado no es válido.");
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?desde=2026-01-06&hasta=2026-01-05"), 400, "El filtro hasta no es válido.");
        await DatosAusencias.AssertErrorAsync(await _cliente.GetAsync(RutaBase + "?empleadoId=9999"), 404, "El empleado 9999 no existe.");
    }

    [Fact]
    public async Task ListadoFiltraPaginaYOrdenaPorInicioDescendente()
    {
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(1, "Vacaciones", new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6), "Aprobada"));
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(1, "PermisoConGoce", new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 5), "Pendiente"));
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(2, "DescansoMedico", new DateOnly(2026, 3, 9), new DateOnly(2026, 3, 13), "Aprobada"));
        PaginaAusenciasDto todas = await ListarAsync(string.Empty);
        Assert.Equal(3, todas.Total);
        Assert.Equal(
            new[] { new DateOnly(2026, 5, 4), new DateOnly(2026, 3, 9), new DateOnly(2026, 2, 2) },
            todas.Items.Select(item => item.FechaInicio).ToArray());
        Assert.Equal("Torres Vega, Ana", todas.Items[0].EmpleadoNombre);
        Assert.Equal("EMP-000001", todas.Items[0].EmpleadoLegajo);
        Assert.Equal(2, (await ListarAsync("?empleadoId=1")).Total);
        Assert.Single((await ListarAsync("?tipo=DescansoMedico")).Items);
        Assert.Single((await ListarAsync("?estado=Pendiente")).Items);
        Assert.Single((await ListarAsync("?busqueda=rojas")).Items);
        Assert.Single((await ListarAsync("?busqueda=EMP-000002")).Items);
        Assert.Equal(2, (await ListarAsync("?desde=2026-03-13&hasta=2026-05-04")).Total);
        Assert.Single((await ListarAsync("?desde=2026-02-06&hasta=2026-02-06")).Items);
        PaginaAusenciasDto segunda = await ListarAsync("?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Equal(2, segunda.Pagina);
        Assert.Single(segunda.Items);
    }

    [Fact]
    public async Task DetalleDevuelveLosDatosDeLaDecision()
    {
        int id = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsJsonAsync($"{RutaBase}/{id}/rechazo", new RechazarAusenciaRequestDto { Motivo = "No procede" })).StatusCode);
        AusenciaDetalleDto? detalle = await _cliente.GetFromJsonAsync<AusenciaDetalleDto>($"{RutaBase}/{id}");
        Assert.NotNull(detalle);
        Assert.Equal("Rechazada", detalle.Estado);
        Assert.Equal("No procede", detalle.MotivoRechazo);
        Assert.Equal(TokenPruebas.NombreUsuario, detalle.DecididoPor);
        Assert.NotNull(detalle.FechaDecision);
        Assert.Equal("Motivo de prueba", detalle.Motivo);
        Assert.Equal("Torres Vega, Ana", detalle.EmpleadoNombre);
    }

    [Fact]
    public async Task ResumenCuentaPendientesYLasAusenciasDeHoy()
    {
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(1, "Vacaciones", hoy.AddDays(-1), hoy.AddDays(1), "Aprobada"));
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(2, "DescansoMedico", hoy, hoy, "Aprobada"));
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(3, "PermisoConGoce", hoy, hoy, "Pendiente"));
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(3, "PermisoSinGoce", hoy.AddDays(-400), hoy.AddDays(-399), "Aprobada"));
        await DatosAusencias.InsertarAsync(_fabrica, DatosAusencias.CrearAusencia(2, "PermisoSinGoce", hoy.AddDays(-300), hoy.AddDays(-299), "Rechazada"));
        ResumenAusenciasDto? resumen = await _cliente.GetFromJsonAsync<ResumenAusenciasDto>(RutaBase + "/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(1, resumen.Pendientes);
        Assert.Equal(2, resumen.AusentesHoy);
        Assert.Equal(1, resumen.VacacionesHoy);
        Assert.Equal(1, resumen.DescansosMedicosHoy);
        Assert.True(resumen.AprobadasMes >= 0);
    }

    [Fact]
    public async Task CuerpoVacioDevuelveElLiteralDelMiddlewareSoloDondeSeExige()
    {
        string[] rutas = { RutaBase, RutaBase + "/1/rechazo" };
        foreach (string ruta in rutas)
        {
            HttpResponseMessage respuesta = await _cliente.PostAsync(ruta, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
            await DatosAusencias.AssertErrorAsync(respuesta, 400, "El cuerpo de la petición (Body) no puede estar vacío.");
        }

        int id = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{id}/cancelacion", null)).StatusCode);
    }

    [Fact]
    public async Task SinTokenTodasLasOperacionesDevuelven401()
    {
        HttpClient anonimo = DatosAusencias.ClienteAnonimo(_fabrica);
        await DatosAusencias.AssertErrorAsync(await anonimo.GetAsync(RutaBase), 401, DatosAusencias.MensajeSinSesion);
        await DatosAusencias.AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/resumen"), 401, DatosAusencias.MensajeSinSesion);
        await DatosAusencias.AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/1"), 401, DatosAusencias.MensajeSinSesion);
        await DatosAusencias.AssertErrorAsync(await anonimo.GetAsync(RutaBase + "/saldo-vacacional/1"), 401, DatosAusencias.MensajeSinSesion);
        await DatosAusencias.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase, new RegistrarAusenciaRequestDto()), 401, DatosAusencias.MensajeSinSesion);
        await DatosAusencias.AssertErrorAsync(await anonimo.PostAsync(RutaBase + "/1/aprobacion", null), 401, DatosAusencias.MensajeSinSesion);
        await DatosAusencias.AssertErrorAsync(await anonimo.PostAsJsonAsync(RutaBase + "/1/rechazo", new RechazarAusenciaRequestDto()), 401, DatosAusencias.MensajeSinSesion);
        await DatosAusencias.AssertErrorAsync(await anonimo.PostAsync(RutaBase + "/1/cancelacion", null), 401, DatosAusencias.MensajeSinSesion);
    }

    [Fact]
    public async Task TokenSinRolDeRrhhDevuelve403AlAprobarYRechazarSinCambiarFilas()
    {
        int id = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        string[] antes = await DatosAusencias.LeerFirmasAsync(_fabrica);
        HttpClient consulta = DatosAusencias.ClienteConRoles(_fabrica, "CONSULTA");
        await DatosAusencias.AssertErrorAsync(await consulta.PostAsync($"{RutaBase}/{id}/aprobacion", null), 403, DatosAusencias.MensajeSinPermiso);
        await DatosAusencias.AssertErrorAsync(
            await consulta.PostAsJsonAsync($"{RutaBase}/{id}/rechazo", new RechazarAusenciaRequestDto { Motivo = "x" }),
            403,
            DatosAusencias.MensajeSinPermiso);
        Assert.Equal(antes, await DatosAusencias.LeerFirmasAsync(_fabrica));
    }

    [Theory]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task RolesRrhhYAdminPuedenAprobarYRechazar(string rol)
    {
        int aprobar = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        int rechazar = await DatosAusencias.InsertarAsync(
            _fabrica,
            DatosAusencias.CrearAusencia(2, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Pendiente"));
        HttpClient cliente = DatosAusencias.ClienteConRoles(_fabrica, rol);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{aprobar}/aprobacion", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await cliente.PostAsJsonAsync($"{RutaBase}/{rechazar}/rechazo", new RechazarAusenciaRequestDto { Motivo = "Motivo" })).StatusCode);
    }

    [Theory]
    [InlineData("CONSULTA")]
    [InlineData("RRHH")]
    [InlineData("ADMIN")]
    public async Task LecturaRegistroYCancelacionConCualquierTokenAutenticadoDevuelvenOk(string rol)
    {
        HttpClient cliente = DatosAusencias.ClienteConRoles(_fabrica, rol);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase + "/resumen")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(RutaBase + "/saldo-vacacional/1")).StatusCode);
        HttpResponseMessage registro = await cliente.PostAsJsonAsync(
            RutaBase,
            new RegistrarAusenciaRequestDto
            {
                EmpleadoId = 2,
                Tipo = "PermisoConGoce",
                FechaInicio = Futuro,
                FechaFin = Futuro,
                Motivo = "Trámite"
            });
        Assert.Equal(HttpStatusCode.OK, registro.StatusCode);
        AusenciaCreadaDto? creada = await registro.Content.ReadFromJsonAsync<AusenciaCreadaDto>();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"{RutaBase}/{creada?.Id}/cancelacion", null)).StatusCode);
    }

    [Fact]
    public async Task RegistroAprobacionYCancelacionGeneranRegistrosDeAuditoria()
    {
        HttpResponseMessage respuesta = await RegistrarAsync(1, "PermisoConGoce", Futuro, Futuro.AddDays(1), "Mudanza");
        AusenciaCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<AusenciaCreadaDto>();
        Assert.NotNull(creada);
        Assert.Single(await DatosAusencias.LeerAuditoriaAsync(_fabrica, "Ausencia", creada.Id), registro => registro.Accion == "Alta");
        Assert.Equal(HttpStatusCode.OK, (await _cliente.PostAsync($"{RutaBase}/{creada.Id}/aprobacion", null)).StatusCode);
        RegistroAuditoria cambio = Assert.Single(
            await DatosAusencias.LeerAuditoriaAsync(_fabrica, "Ausencia", creada.Id),
            registro => registro.Accion == "Modificacion" && registro.Cambios.Contains("DecididoPor"));
        using JsonDocument cambios = JsonDocument.Parse(cambio.Cambios);
        List<string> campos = cambios.RootElement.EnumerateArray().Select(item => item.GetProperty("campo").GetString() ?? string.Empty).ToList();
        Assert.Contains("Estado", campos);
        Assert.Contains("DecididoPor", campos);
    }

    private async Task<HttpResponseMessage> RegistrarAsync(int? empleadoId, string? tipo, DateOnly? inicio, DateOnly? fin, string? motivo)
    {
        RegistrarAusenciaRequestDto solicitud = new RegistrarAusenciaRequestDto
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            FechaInicio = inicio,
            FechaFin = fin,
            Motivo = motivo
        };
        return await _cliente.PostAsJsonAsync(RutaBase, solicitud);
    }

    private async Task AssertRegistroAsync(int? empleadoId, string? tipo, DateOnly? inicio, DateOnly? fin, string? motivo, int codigo, string mensaje)
    {
        await DatosAusencias.AssertErrorAsync(await RegistrarAsync(empleadoId, tipo, inicio, fin, motivo), codigo, mensaje);
    }

    private async Task<PaginaAusenciasDto> ListarAsync(string consulta)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync(RutaBase + consulta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        PaginaAusenciasDto? pagina = await respuesta.Content.ReadFromJsonAsync<PaginaAusenciasDto>();
        Assert.NotNull(pagina);
        return pagina;
    }
}
