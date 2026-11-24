using TitanGlobal.Application.UseCases.Modulo66_Planillas;

namespace TitanGlobal.Api.Tests.Modulo66_Planillas;

// Importes calculados a mano con UIT 5350, ONP 13 %, EsSalud 9 %, horas de jornada 8, recargo 25 % los primeros 120 minutos y 35 % el resto.
public class CalculadoraRemuneracionTests
{
    private static ParametrosRemuneracion Parametros()
    {
        ParametrosRemuneracion parametros = new ParametrosRemuneracion();
        parametros.Tramos.Add(new TramoQuintaRemuneracion { HastaUit = 5m, Tasa = 0.08m });
        parametros.Tramos.Add(new TramoQuintaRemuneracion { HastaUit = 20m, Tasa = 0.14m });
        parametros.Tramos.Add(new TramoQuintaRemuneracion { HastaUit = 35m, Tasa = 0.17m });
        parametros.Tramos.Add(new TramoQuintaRemuneracion { HastaUit = 45m, Tasa = 0.20m });
        parametros.Tramos.Add(new TramoQuintaRemuneracion { HastaUit = null, Tasa = 0.30m });
        return parametros;
    }

    private static PensionRemuneracion Onp()
    {
        return new PensionRemuneracion { Codigo = "ONP", Sistema = "ONP", TasaAporte = 0.13m, TasaPrimaSeguro = 0m, TasaComision = 0m };
    }

    private static PensionRemuneracion Integra()
    {
        return new PensionRemuneracion
        {
            Codigo = "INTEGRA",
            Sistema = "AFP",
            TasaAporte = 0.10m,
            TasaPrimaSeguro = 0.0137m,
            TasaComision = 0.0155m,
            RemuneracionMaximaAsegurable = 12027.10m
        };
    }

    private static DatosRemuneracion Datos(decimal sueldo, int diasContrato, PensionRemuneracion? pension)
    {
        return new DatosRemuneracion { SueldoBasico = sueldo, DiasContrato = diasContrato, Pension = pension };
    }

    [Fact]
    public void MesCompletoConOnpDescuentaElTreceYNoRetieneQuinta()
    {
        // Ingresos 3000.00; ONP 3000 x 0.13 = 390.00; EsSalud 3000 x 0.09 = 270.00.
        // Quinta: 3000 x 12 = 36000 - 7 x 5350 (37450) < 0 => 0. Neto 3000 - 390 = 2610.00.
        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(Datos(3000m, 30, Onp()), Parametros());

        Assert.Null(resultado.Incidencia);
        Assert.Equal(30, resultado.DiasLaborados);
        Assert.Equal(3000.00m, resultado.IngresoBasico);
        Assert.Equal(3000.00m, resultado.TotalIngresos);
        Assert.Equal(390.00m, resultado.DescuentoPension);
        Assert.Equal(0m, resultado.DescuentoQuinta);
        Assert.Equal(390.00m, resultado.TotalDescuentos);
        Assert.Equal(2610.00m, resultado.Neto);
        Assert.Equal(270.00m, resultado.AporteEssalud);
    }

    [Fact]
    public void AfpAplicaLaPrimaSoloHastaLaRemuneracionMaximaAsegurableYLaQuintaPorVariosTramos()
    {
        // Ingresos 15000.00. Aporte 15000 x 0.10 = 1500.00; comision 15000 x 0.0155 = 232.50;
        // prima min(15000, 12027.10) x 0.0137 = 164.771270 => 164.77. Pension 1500.00 + 232.50 + 164.77 = 1897.27.
        // Quinta: 15000 x 12 = 180000 - 37450 = 142550. Tramos en soles: 5 UIT = 26750, 20 UIT = 107000.
        // 26750 x 0.08 = 2140.00; (107000 - 26750) x 0.14 = 80250 x 0.14 = 11235.00; (142550 - 107000) x 0.17 = 35550 x 0.17 = 6043.50.
        // Impuesto anual 19418.50 / 12 = 1618.208333 => 1618.21. Descuentos 1897.27 + 1618.21 = 3515.48. Neto 15000 - 3515.48 = 11484.52.
        // EsSalud 15000 x 0.09 = 1350.00.
        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(Datos(15000m, 30, Integra()), Parametros());

        Assert.Equal(1897.27m, resultado.DescuentoPension);
        Assert.Equal(1618.21m, resultado.DescuentoQuinta);
        Assert.Equal(3515.48m, resultado.TotalDescuentos);
        Assert.Equal(11484.52m, resultado.Neto);
        Assert.Equal(1350.00m, resultado.AporteEssalud);
    }

    [Fact]
    public void QuintaConUnSoloTramoYBonosSumadosALosIngresos()
    {
        // Sueldo 3000.00 + bonos 500.50 = ingresos 3500.50. ONP 3500.50 x 0.13 = 455.065 => 455.07 (hacia arriba en el medio).
        // EsSalud 3500.50 x 0.09 = 315.045 => 315.05.
        // Quinta: 3500.50 x 12 = 42006 - 37450 = 4556 (< 26750, solo el primer tramo); 4556 x 0.08 = 364.48 / 12 = 30.373333 => 30.37.
        // Neto 3500.50 - 455.07 - 30.37 = 3015.06.
        DatosRemuneracion datos = Datos(3000m, 30, Onp());
        datos.IngresoBonos = 500.50m;

        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(datos, Parametros());

        Assert.Equal(500.50m, resultado.IngresoBonos);
        Assert.Equal(3500.50m, resultado.TotalIngresos);
        Assert.Equal(455.07m, resultado.DescuentoPension);
        Assert.Equal(30.37m, resultado.DescuentoQuinta);
        Assert.Equal(3015.06m, resultado.Neto);
        Assert.Equal(315.05m, resultado.AporteEssalud);
    }

    [Fact]
    public void QuintaDeUnSueldoDe5000UsaSoloElPrimerTramo()
    {
        // 5000 x 12 = 60000 - 37450 = 22550 (< 26750); 22550 x 0.08 = 1804.00 / 12 = 150.333333 => 150.33.
        Assert.Equal(150.33m, CalculadoraRemuneracion.CalcularQuinta(5000m, Parametros()));
        // Bajo 7 UIT anuales no hay retencion: 3120.83 x 12 = 37449.96 < 37450.
        Assert.Equal(0m, CalculadoraRemuneracion.CalcularQuinta(3120.83m, Parametros()));
    }

    [Fact]
    public void IngresoParcialPorAltaAMitadDeMesProrrateaElSueldo()
    {
        // Alta el dia 14: 17 dias cubiertos. 2800 x 17 / 30 = 1586.666667 => 1586.67.
        // ONP 1586.67 x 0.13 = 206.2671 => 206.27. EsSalud 1586.67 x 0.09 = 142.8003 => 142.80. Neto 1586.67 - 206.27 = 1380.40.
        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(Datos(2800m, 17, Onp()), Parametros());

        Assert.Equal(17, resultado.DiasLaborados);
        Assert.Equal(1586.67m, resultado.IngresoBasico);
        Assert.Equal(206.27m, resultado.DescuentoPension);
        Assert.Equal(142.80m, resultado.AporteEssalud);
        Assert.Equal(1380.40m, resultado.Neto);
    }

    [Fact]
    public void ElPermisoSinGoceDescuentaDiasLaborados()
    {
        // 30 - 2 = 28 dias: 3000 x 28 / 30 = 2800.00. ONP 364.00; EsSalud 252.00; neto 2436.00.
        DatosRemuneracion datos = Datos(3000m, 30, Onp());
        datos.DiasSinGoce = 2;

        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(datos, Parametros());

        Assert.Equal(28, resultado.DiasLaborados);
        Assert.Equal(2800.00m, resultado.IngresoBasico);
        Assert.Equal(364.00m, resultado.DescuentoPension);
        Assert.Equal(252.00m, resultado.AporteEssalud);
        Assert.Equal(2436.00m, resultado.Neto);
    }

    [Fact]
    public void LasFaltasDescuentanDiasYLosDiasLaboradosNuncaSonNegativos()
    {
        // 30 - 1 (sin goce) - 3 (faltas) = 26 dias: 3000 x 26 / 30 = 2600.00. ONP 338.00; neto 2262.00.
        DatosRemuneracion datos = Datos(3000m, 30, Onp());
        datos.DiasSinGoce = 1;
        datos.DiasFalta = 3;
        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(datos, Parametros());
        Assert.Equal(26, resultado.DiasLaborados);
        Assert.Equal(2600.00m, resultado.IngresoBasico);
        Assert.Equal(338.00m, resultado.DescuentoPension);
        Assert.Equal(2262.00m, resultado.Neto);

        // 30 - 20 - 15 = -5 => 0 dias: sin ingresos y sin incidencia.
        datos.DiasSinGoce = 20;
        datos.DiasFalta = 15;
        ResultadoRemuneracion sinDias = CalculadoraRemuneracion.Calcular(datos, Parametros());
        Assert.Equal(0, sinDias.DiasLaborados);
        Assert.Equal(0m, sinDias.TotalIngresos);
        Assert.Equal(0m, sinDias.Neto);
        Assert.Null(sinDias.Incidencia);
    }

    [Fact]
    public void LasHorasExtraUsanElRecargoDel25PorCientoLuegoElDel35PorJornada()
    {
        // Valor hora 3000 / 30 / 8 = 12.50.
        // Jornada de 90 min (solo 25 %): 90 x 1.25 = 112.5 min equivalentes. Jornada de 180 min: 120 x 1.25 + 60 x 1.35 = 150 + 81 = 231.
        // (112.5 + 231) / 60 x 12.50 = 343.5 / 60 x 12.50 = 71.5625 => 71.56.
        // Ingresos 3000 + 71.56 = 3071.56. ONP 3071.56 x 0.13 = 399.3028 => 399.30. EsSalud 3071.56 x 0.09 = 276.4404 => 276.44.
        // Neto 3071.56 - 399.30 = 2672.26.
        DatosRemuneracion datos = Datos(3000m, 30, Onp());
        datos.MinutosExtraPorJornada.Add(90);
        datos.MinutosExtraPorJornada.Add(180);

        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(datos, Parametros());

        Assert.Equal(270, resultado.MinutosExtra);
        Assert.Equal(71.56m, resultado.IngresoHorasExtra);
        Assert.Equal(3071.56m, resultado.TotalIngresos);
        Assert.Equal(399.30m, resultado.DescuentoPension);
        Assert.Equal(276.44m, resultado.AporteEssalud);
        Assert.Equal(2672.26m, resultado.Neto);
    }

    [Fact]
    public void ElTramoDel35SeAplicaPorJornadaYNoSobreLaSumaDelMes()
    {
        // Dos jornadas de 120 min: cada una al 25 %. 240 x 1.25 = 300 / 60 x 12.50 = 62.50 (con 35 % para el exceso daria mas).
        ParametrosRemuneracion parametros = Parametros();
        decimal importe = CalculadoraRemuneracion.CalcularHorasExtra(3000m, new List<int> { 120, 120 }, parametros);
        Assert.Equal(62.50m, importe);
    }

    [Fact]
    public void ElDescuentoDePrestamosNoSuperaElNetoRestante()
    {
        // Neto antes de prestamos 2610.00 (mes completo ONP de 3000). Cuota de 5000 => se descuenta 2610.00; neto 0.
        ResultadoRemuneracion limitado = CalculadoraRemuneracion.AplicarPrestamos(CalculadoraRemuneracion.Calcular(Datos(3000m, 30, Onp()), Parametros()), 5000m);
        Assert.Equal(2610.00m, limitado.DescuentoPrestamos);
        Assert.Equal(3000.00m, limitado.TotalDescuentos);
        Assert.Equal(0m, limitado.Neto);

        // Cuota de 400: descuentos 390 + 400 = 790.00; neto 3000 - 790 = 2210.00.
        ResultadoRemuneracion normal = CalculadoraRemuneracion.AplicarPrestamos(CalculadoraRemuneracion.Calcular(Datos(3000m, 30, Onp()), Parametros()), 400m);
        Assert.Equal(400.00m, normal.DescuentoPrestamos);
        Assert.Equal(790.00m, normal.TotalDescuentos);
        Assert.Equal(2210.00m, normal.Neto);
    }

    [Fact]
    public void SiPensionYQuintaSuperanLosIngresosElEmpleadoSeExcluyeConIncidencia()
    {
        // Aporte 0.90 + comision 0.20 = 110 % de los ingresos: 1000 x 1.10 = 1100 > 1000 => neto negativo, importes en 0.
        PensionRemuneracion excesiva = new PensionRemuneracion { Codigo = "X", Sistema = "AFP", TasaAporte = 0.90m, TasaComision = 0.20m, TasaPrimaSeguro = 0m };

        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(Datos(1000m, 30, excesiva), Parametros());

        Assert.True(resultado.Excluido);
        Assert.Equal("Neto negativo: revisar ingresos y descuentos.", resultado.Incidencia);
        Assert.Equal(30, resultado.DiasLaborados);
        Assert.Equal(0m, resultado.TotalIngresos);
        Assert.Equal(0m, resultado.TotalDescuentos);
        Assert.Equal(0m, resultado.Neto);
        Assert.Equal(0m, resultado.AporteEssalud);
    }

    [Fact]
    public void SinAfiliacionElEmpleadoSeExcluyeConIncidencia()
    {
        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(Datos(3000m, 30, null), Parametros());

        Assert.True(resultado.Excluido);
        Assert.Equal("Sin afiliación a un sistema de pensiones.", resultado.Incidencia);
        Assert.Null(resultado.CodigoPension);
        Assert.Equal(0m, resultado.TotalIngresos);
        Assert.Equal(0m, resultado.Neto);
    }

    [Fact]
    public void LaPlanillaComplementariaSoloTieneBonosConPensionYQuinta()
    {
        // Sin sueldo ni dias: bono 1000.00; ONP 130.00; quinta 1000 x 12 = 12000 - 37450 < 0 => 0; neto 870.00; EsSalud 90.00.
        DatosRemuneracion datos = Datos(0m, 0, Onp());
        datos.IngresoBonos = 1000m;

        ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(datos, Parametros());

        Assert.Equal(0m, resultado.IngresoBasico);
        Assert.Equal(1000.00m, resultado.TotalIngresos);
        Assert.Equal(130.00m, resultado.DescuentoPension);
        Assert.Equal(870.00m, resultado.Neto);
        Assert.Equal(90.00m, resultado.AporteEssalud);
    }
}
