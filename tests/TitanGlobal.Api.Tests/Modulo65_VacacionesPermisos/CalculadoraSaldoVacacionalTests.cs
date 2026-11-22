using TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos;

namespace TitanGlobal.Api.Tests.Modulo65_VacacionesPermisos;

// Cálculo puro del saldo vacacional: sin base de datos ni HTTP.
public class CalculadoraSaldoVacacionalTests
{
    [Theory]
    [InlineData(2024, 1, 8, 2024, 1, 8, 0)]
    [InlineData(2024, 1, 8, 2024, 2, 7, 0)]
    [InlineData(2024, 1, 8, 2024, 2, 8, 1)]
    [InlineData(2024, 1, 8, 2026, 3, 7, 25)]
    [InlineData(2024, 1, 8, 2026, 3, 8, 26)]
    [InlineData(2024, 1, 8, 2023, 12, 31, 0)]
    public void CuentaMesesCompletosAlAlcanzarElMismoDia(int anioIngreso, int mesIngreso, int diaIngreso, int anio, int mes, int dia, int esperados)
    {
        int meses = CalculadoraSaldoVacacional.MesesCompletos(
            new DateOnly(anioIngreso, mesIngreso, diaIngreso),
            new DateOnly(anio, mes, dia));
        Assert.Equal(esperados, meses);
    }

    [Theory]
    [InlineData(2025, 2, 27, 0)]
    [InlineData(2025, 2, 28, 1)]
    [InlineData(2025, 3, 30, 1)]
    [InlineData(2025, 3, 31, 2)]
    [InlineData(2025, 4, 29, 2)]
    [InlineData(2025, 4, 30, 3)]
    public void IngresoEl31UsaElUltimoDiaDelMesCuandoElDiaNoExiste(int anio, int mes, int dia, int esperados)
    {
        int meses = CalculadoraSaldoVacacional.MesesCompletos(new DateOnly(2025, 1, 31), new DateOnly(anio, mes, dia));
        Assert.Equal(esperados, meses);
    }

    [Theory]
    [InlineData(2024, 1, 31, 2024, 2, 28, 0)]
    [InlineData(2024, 1, 31, 2024, 2, 29, 1)]
    [InlineData(2024, 1, 31, 2024, 3, 30, 1)]
    [InlineData(2024, 1, 31, 2024, 3, 31, 2)]
    public void ElMesDeFebreroBisiestoCompletaElMesElDia29(int anioIngreso, int mesIngreso, int diaIngreso, int anio, int mes, int dia, int esperados)
    {
        int meses = CalculadoraSaldoVacacional.MesesCompletos(
            new DateOnly(anioIngreso, mesIngreso, diaIngreso),
            new DateOnly(anio, mes, dia));
        Assert.Equal(esperados, meses);
    }

    [Fact]
    public void DevengaDosComaCincoDiasPorMesCompleto()
    {
        Assert.Equal(0m, CalculadoraSaldoVacacional.Devengados(new DateOnly(2024, 1, 8), new DateOnly(2024, 2, 7)));
        Assert.Equal(2.5m, CalculadoraSaldoVacacional.Devengados(new DateOnly(2024, 1, 8), new DateOnly(2024, 2, 8)));
        Assert.Equal(65m, CalculadoraSaldoVacacional.Devengados(new DateOnly(2024, 1, 8), new DateOnly(2026, 3, 8)));
    }

    [Fact]
    public void GozadoCuentaSoloLosDiasAprobadosYaTranscurridosInclusive()
    {
        List<CalculadoraSaldoVacacional.PeriodoVacacional> periodos = new List<CalculadoraSaldoVacacional.PeriodoVacacional>
        {
            new CalculadoraSaldoVacacional.PeriodoVacacional(new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6), true),
            new CalculadoraSaldoVacacional.PeriodoVacacional(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 10), true),
            new CalculadoraSaldoVacacional.PeriodoVacacional(new DateOnly(2026, 2, 3), new DateOnly(2026, 2, 4), false)
        };
        Assert.Equal(0, CalculadoraSaldoVacacional.Gozados(periodos, new DateOnly(2026, 2, 1)));
        Assert.Equal(1, CalculadoraSaldoVacacional.Gozados(periodos, new DateOnly(2026, 2, 2)));
        Assert.Equal(3, CalculadoraSaldoVacacional.Gozados(periodos, new DateOnly(2026, 2, 4)));
        Assert.Equal(5, CalculadoraSaldoVacacional.Gozados(periodos, new DateOnly(2026, 3, 1)));
        Assert.Equal(8, CalculadoraSaldoVacacional.Gozados(periodos, new DateOnly(2026, 6, 3)));
        Assert.Equal(15, CalculadoraSaldoVacacional.Gozados(periodos, new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void ProgramadoSumaLosDiasAprobadosPendientesDeGozarYTodasLasPendientes()
    {
        List<CalculadoraSaldoVacacional.PeriodoVacacional> periodos = new List<CalculadoraSaldoVacacional.PeriodoVacacional>
        {
            new CalculadoraSaldoVacacional.PeriodoVacacional(new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6), true),
            new CalculadoraSaldoVacacional.PeriodoVacacional(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 10), true),
            new CalculadoraSaldoVacacional.PeriodoVacacional(new DateOnly(2026, 2, 3), new DateOnly(2026, 2, 4), false)
        };
        Assert.Equal(17, CalculadoraSaldoVacacional.Programados(periodos, new DateOnly(2026, 2, 1)));
        Assert.Equal(16, CalculadoraSaldoVacacional.Programados(periodos, new DateOnly(2026, 2, 2)));
        Assert.Equal(14, CalculadoraSaldoVacacional.Programados(periodos, new DateOnly(2026, 2, 4)));
        Assert.Equal(12, CalculadoraSaldoVacacional.Programados(periodos, new DateOnly(2026, 3, 1)));
        Assert.Equal(2, CalculadoraSaldoVacacional.Programados(periodos, new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void ElSaldoEsDevengadoMenosGozadoMenosProgramado()
    {
        Assert.Equal(12.5m, CalculadoraSaldoVacacional.Saldo(29.5m, 10, 7));
        Assert.Equal(-2m, CalculadoraSaldoVacacional.Saldo(5m, 5, 2));
    }
}
