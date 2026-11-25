using System.Net.Http.Json;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

namespace TitanGlobal.Api.Tests.Modulo70_SeguridadSaludOcupacional;

[Collection("Modulo04")]
public class ResumenSstApiTests
{
    private const string Ruta = "/api/v1/incidentes-sst/resumen";

    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public ResumenSstApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        DatosSst.Restablecer(fabrica);
    }

    [Fact]
    public async Task ResumenSinDatosDevuelveCeros()
    {
        ResumenSstDto resumen = await LeerAsync(Ruta);
        Assert.Equal(DatosSst.Hoy.Year, resumen.Anio);
        Assert.Equal(0, resumen.IncidentesAbiertos);
        Assert.Equal(0, resumen.IncidentesAnio);
        Assert.Equal(0, resumen.AccidentesConBaja);
        Assert.Equal(0, resumen.DiasBajaAnio);
        Assert.Equal(0, resumen.EntregasEppVencidas);
        Assert.Equal(0, resumen.ExamenesVencidos);
        Assert.Equal(0, resumen.EmpleadosNoAptos);
    }

    [Fact]
    public async Task ResumenCuentaIncidentesDelAnioYLosVencimientosAlLeer()
    {
        DateOnly hoy = DatosSst.Hoy;
        DateOnly enAnio = new DateOnly(2031, 3, 10);
        DateOnly otroAnio = new DateOnly(2030, 11, 5);
        await InsertarIncidentePasadoAsync(1, "AccidenteConBaja", enAnio, "Abierto", 5);
        await InsertarIncidentePasadoAsync(2, "AccidenteConBaja", enAnio, "Cerrado", 3);
        await InsertarIncidentePasadoAsync(3, "IncidenteSinLesion", enAnio, "Abierto", 0);
        await InsertarIncidentePasadoAsync(1, "AccidenteConBaja", otroAnio, "Abierto", 9);
        ResumenSstDto anio = await LeerAsync($"{Ruta}?anio=2031");
        Assert.Equal(2031, anio.Anio);
        Assert.Equal(3, anio.IncidentesAbiertos);
        Assert.Equal(3, anio.IncidentesAnio);
        Assert.Equal(2, anio.AccidentesConBaja);
        Assert.Equal(8, anio.DiasBajaAnio);
        ResumenSstDto anterior = await LeerAsync($"{Ruta}?anio=2030");
        Assert.Equal(3, anterior.IncidentesAbiertos);
        Assert.Equal(1, anterior.IncidentesAnio);
        Assert.Equal(1, anterior.AccidentesConBaja);
        Assert.Equal(9, anterior.DiasBajaAnio);
        int equipo = await DatosSst.InsertarEquipoAsync(_fabrica, "CAS", 30);
        await DatosSst.InsertarEntregaAsync(_fabrica, equipo, 1, hoy.AddDays(-40), hoy.AddDays(-10));
        await DatosSst.InsertarEntregaAsync(_fabrica, equipo, 2, hoy.AddDays(-30), hoy);
        await DatosSst.InsertarExamenAsync(_fabrica, 1, "Periodico", hoy.AddDays(-400), "Apto", hoy.AddDays(-35));
        await DatosSst.InsertarExamenAsync(_fabrica, 2, "Periodico", hoy.AddDays(-30), "Apto", hoy);
        await DatosSst.InsertarExamenAsync(_fabrica, 4, "Retiro", hoy.AddDays(-39), "Apto", null);
        ResumenSstDto vencimientos = await LeerAsync(Ruta);
        Assert.Equal(1, vencimientos.EntregasEppVencidas);
        Assert.Equal(1, vencimientos.ExamenesVencidos);
    }

    // Un empleado cuenta como no apto solo si su examen más reciente es NoApto y sigue activo.
    [Fact]
    public async Task ResumenCuentaEmpleadosActivosConElUltimoExamenNoApto()
    {
        DateOnly hoy = DatosSst.Hoy;
        await DatosSst.InsertarExamenAsync(_fabrica, 1, "Periodico", hoy.AddDays(-100), "NoApto", hoy.AddDays(200));
        await DatosSst.InsertarExamenAsync(_fabrica, 1, "Periodico", hoy.AddDays(-10), "Apto", hoy.AddDays(300));
        await DatosSst.InsertarExamenAsync(_fabrica, 2, "Periodico", hoy.AddDays(-100), "Apto", hoy.AddDays(200));
        await DatosSst.InsertarExamenAsync(_fabrica, 2, "Periodico", hoy.AddDays(-10), "NoApto", hoy.AddDays(300));
        await DatosSst.InsertarExamenAsync(_fabrica, 3, "Ingreso", hoy.AddDays(-5), "NoApto", hoy.AddDays(300));
        await DatosSst.InsertarExamenAsync(_fabrica, 4, "Retiro", hoy.AddDays(-39), "NoApto", null);
        Assert.Equal(2, (await LeerAsync(Ruta)).EmpleadosNoAptos);
    }

    [Fact]
    public async Task ResumenValidaElAnio()
    {
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{Ruta}?anio=1899"), 400, "El filtro anio no es válido.");
        await DatosSst.AssertErrorAsync(await _cliente.GetAsync($"{Ruta}?anio=2201"), 400, "El filtro anio no es válido.");
    }

    private async Task InsertarIncidentePasadoAsync(int empleadoId, string tipo, DateOnly fecha, string estado, int diasBaja)
    {
        await DatosSst.InsertarIncidenteAsync(_fabrica, empleadoId, tipo, fecha, estado, diasBaja);
    }

    private async Task<ResumenSstDto> LeerAsync(string ruta)
    {
        ResumenSstDto? resumen = await _cliente.GetFromJsonAsync<ResumenSstDto>(ruta);
        Assert.NotNull(resumen);
        return resumen;
    }
}
