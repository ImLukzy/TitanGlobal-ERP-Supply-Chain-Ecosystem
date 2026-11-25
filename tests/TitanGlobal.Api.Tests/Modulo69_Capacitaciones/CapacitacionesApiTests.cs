using System.Net;
using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

namespace TitanGlobal.Api.Tests.Modulo69_Capacitaciones;

[Collection("Modulo04")]
public class CapacitacionesApiTests
{
    private const string RutaBase = "/api/v1/capacitaciones";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public CapacitacionesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosCapacitacion.Restablecer(fabrica);
    }

    // K5: suma de horas de los cursos aprobados cuya programación terminó en el año consultado.
    [Fact]
    public async Task HorasSumanSoloAprobadosDelAnioConsultadoDeEmpleadosActivos()
    {
        int corto = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "COR", 8m);
        int largo = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "LAR", 12.5m);
        int p2024 = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, corto, new DateOnly(2024, 5, 1), new DateOnly(2024, 5, 2), 10, "Realizada");
        int p2025 = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, largo, new DateOnly(2025, 5, 1), new DateOnly(2025, 5, 2), 10, "Realizada");
        int p2025b = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, corto, new DateOnly(2025, 7, 1), new DateOnly(2025, 7, 2), 10, "Realizada");
        int cruza = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, largo, new DateOnly(2025, 12, 30), new DateOnly(2026, 1, 3), 10, "Realizada");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, p2024, 1, "Aprobado", 15m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, p2025, 1, "Aprobado", 15m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, p2025b, 1, "Desaprobado", 9m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, p2025, 2, "Retirado");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, p2025b, 2, "Aprobado", 14m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, cruza, 2, "Aprobado", 14m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, p2025, 4, "Aprobado", 16m);
        PaginaHorasDto anio2025 = await LeerHorasAsync($"{RutaBase}/horas?anio=2025");
        Assert.Equal(2025, anio2025.Anio);
        Assert.Equal(3, anio2025.Total);
        Assert.Equal(new[] { 2, 3, 1 }, anio2025.Items.Select(item => item.EmpleadoId).ToArray());
        Assert.Equal(new[] { 8m, 0m, 12.5m }, anio2025.Items.Select(item => item.Horas).ToArray());
        Assert.Equal(new[] { 1, 0, 1 }, anio2025.Items.Select(item => item.Cursos).ToArray());
        PaginaHorasDto anio2024 = await LeerHorasAsync($"{RutaBase}/horas?anio=2024");
        Assert.Equal(8m, anio2024.Items.Single(item => item.EmpleadoId == 1).Horas);
        Assert.Equal(0m, anio2024.Items.Single(item => item.EmpleadoId == 2).Horas);
        PaginaHorasDto anio2026 = await LeerHorasAsync($"{RutaBase}/horas?anio=2026");
        Assert.Equal(12.5m, anio2026.Items.Single(item => item.EmpleadoId == 2).Horas);
        Assert.DoesNotContain(anio2025.Items, item => item.EmpleadoId == 4);
    }

    [Fact]
    public async Task HorasValidanPaginacionAnioYAplicanBusqueda()
    {
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/horas?pagina=0"), 400, "La página debe ser mayor o igual a 1.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/horas?tamanoPagina=101"), 400, "El tamaño de página debe estar entre 1 y 100.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/horas?anio=0"), 400, "El filtro anio no es válido.");
        PaginaHorasDto porDefecto = await LeerHorasAsync($"{RutaBase}/horas");
        Assert.Equal(DatosCapacitacion.Hoy.Year, porDefecto.Anio);
        Assert.Equal(3, porDefecto.Total);
        PaginaHorasDto busqueda = await LeerHorasAsync($"{RutaBase}/horas?busqueda=salazar");
        Assert.Equal(new[] { 3 }, busqueda.Items.Select(item => item.EmpleadoId).ToArray());
        PaginaHorasDto segunda = await LeerHorasAsync($"{RutaBase}/horas?pagina=2&tamanoPagina=2");
        Assert.Equal(3, segunda.Total);
        Assert.Single(segunda.Items);
    }

    [Fact]
    public async Task EmpleadoMuestraCursosDelAnioHorasYCertificacionesConVencida()
    {
        int curso = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "SEG", 10m);
        int otro = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "CAL", 6m);
        DateOnly hoy = DatosCapacitacion.Hoy;
        int actual = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, curso, hoy.AddDays(-20), hoy.AddDays(-19), 10, "Realizada");
        int anterior = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, otro, new DateOnly(2020, 3, 1), new DateOnly(2020, 3, 2), 10, "Realizada");
        int aprobada = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, actual, 1, "Aprobado", 17m);
        int vieja = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, anterior, 1, "Aprobado", 12m);
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, aprobada, "VIGENTE", hoy.AddDays(-19), hoy.AddDays(30));
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, vieja, "CADUCADA", new DateOnly(2020, 3, 2), new DateOnly(2021, 3, 2));
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/empleados/9999"), 404, "El empleado 9999 no existe.");
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/empleados/1?anio=0"), 400, "El filtro anio no es válido.");
        CapacitacionEmpleadoDto? actualAnio = await _cliente.GetFromJsonAsync<CapacitacionEmpleadoDto>($"{RutaBase}/empleados/1?anio={hoy.AddDays(-19).Year}");
        Assert.NotNull(actualAnio);
        Assert.Equal(10m, actualAnio.Horas);
        Assert.Equal("SEG", Assert.Single(actualAnio.Cursos).CursoCodigo);
        Assert.Equal(new[] { "CADUCADA", "VIGENTE" }, actualAnio.Certificaciones.Select(item => item.Codigo).ToArray());
        Assert.Equal(new[] { true, false }, actualAnio.Certificaciones.Select(item => item.Vencida).ToArray());
        CapacitacionEmpleadoDto? antiguo = await _cliente.GetFromJsonAsync<CapacitacionEmpleadoDto>($"{RutaBase}/empleados/1?anio=2020");
        Assert.NotNull(antiguo);
        Assert.Equal(6m, antiguo.Horas);
        Assert.Equal("CAL", Assert.Single(antiguo.Cursos).CursoCodigo);
        CapacitacionEmpleadoDto? cesado = await _cliente.GetFromJsonAsync<CapacitacionEmpleadoDto>($"{RutaBase}/empleados/4");
        Assert.NotNull(cesado);
        Assert.Empty(cesado.Cursos);
        Assert.Equal(0m, cesado.Horas);
    }

    [Fact]
    public async Task ResumenCuentaCursosProgramacionesInscritosHorasYCertificacionesVencidas()
    {
        int activo = await DatosCapacitacion.InsertarCursoAsync(_fabrica, "ACT", 10m);
        await DatosCapacitacion.InsertarCursoAsync(_fabrica, "INA", 4m, false);
        int futura = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, activo, new DateOnly(2090, 3, 1), new DateOnly(2090, 3, 2), 5, "Programada");
        await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, activo, new DateOnly(2090, 6, 1), new DateOnly(2090, 6, 2), 5, "Cancelada");
        int realizada = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, activo, new DateOnly(2090, 1, 10), new DateOnly(2090, 1, 11), 5, "Realizada");
        int otroAnio = await DatosCapacitacion.InsertarProgramacionAsync(_fabrica, activo, new DateOnly(2091, 1, 10), new DateOnly(2091, 1, 11), 5, "Programada");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, futura, 1, "Inscrito");
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, futura, 2, "Retirado");
        int aprobada = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, realizada, 1, "Aprobado", 15m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, realizada, 2, "Desaprobado", 8m);
        await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, otroAnio, 3, "Inscrito");
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, aprobada, "RES-1", new DateOnly(2090, 1, 11), new DateOnly(2090, 12, 31));
        int vencida = await DatosCapacitacion.InsertarInscripcionAsync(_fabrica, realizada, 3, "Aprobado", 15m);
        await DatosCapacitacion.InsertarCertificacionAsync(_fabrica, vencida, "RES-2", new DateOnly(2090, 1, 11), DatosCapacitacion.Hoy.AddDays(-1));
        ResumenCapacitacionesDto? resumen = await _cliente.GetFromJsonAsync<ResumenCapacitacionesDto>($"{RutaBase}/resumen?anio=2090");
        Assert.NotNull(resumen);
        Assert.Equal(2090, resumen.Anio);
        Assert.Equal(1, resumen.CursosActivos);
        Assert.Equal(1, resumen.Programadas);
        Assert.Equal(1, resumen.Realizadas);
        Assert.Equal(4, resumen.Inscritos);
        Assert.Equal(20m, resumen.HorasTotales);
        Assert.Equal(1, resumen.CertificacionesVencidas);
        ResumenCapacitacionesDto? vacio = await _cliente.GetFromJsonAsync<ResumenCapacitacionesDto>($"{RutaBase}/resumen?anio=2085");
        Assert.NotNull(vacio);
        Assert.Equal(0, vacio.Programadas);
        Assert.Equal(0m, vacio.HorasTotales);
        await DatosCapacitacion.AssertErrorAsync(await _cliente.GetAsync($"{RutaBase}/resumen?anio=3000"), 400, "El filtro anio no es válido.");
    }

    [Fact]
    public async Task ConsultasExigenSesionPeroNoUnRolEspecifico()
    {
        HttpClient anonimo = DatosCapacitacion.ClienteAnonimo(_fabrica);
        string sesion = DatosCapacitacion.MensajeSinSesion;
        await DatosCapacitacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/horas"), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/empleados/1"), 401, sesion);
        await DatosCapacitacion.AssertErrorAsync(await anonimo.GetAsync($"{RutaBase}/resumen"), 401, sesion);
        HttpClient consulta = DatosCapacitacion.ClienteConRoles(_fabrica, "CONSULTA");
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/horas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/empleados/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"{RutaBase}/resumen")).StatusCode);
    }

    private async Task<PaginaHorasDto> LeerHorasAsync(string ruta)
    {
        PaginaHorasDto? pagina = await _cliente.GetFromJsonAsync<PaginaHorasDto>(ruta);
        Assert.NotNull(pagina);
        return pagina;
    }
}
