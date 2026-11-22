namespace TitanGlobal.Application.UseCases.Modulo66_Planillas;

// Cálculo puro de la remuneración mensual de un empleado (spec modulo66, decisiones 1 a 9): sin base de datos ni reloj.
// Todo es decimal y cada concepto se redondea a 2 decimales con MidpointRounding.AwayFromZero.
public static class CalculadoraRemuneracion
{
    public const string IncidenciaSinAfiliacion = "Sin afiliación a un sistema de pensiones.";

    public const string IncidenciaNetoNegativo = "Neto negativo: revisar ingresos y descuentos.";

    // Mes comercial de 30 días.
    public const int DiasMesComercial = 30;

    public const string SistemaAfp = "AFP";

    private const decimal MinutosPorHora = 60m;

    public static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    public static ResultadoRemuneracion Calcular(DatosRemuneracion datos, ParametrosRemuneracion parametros)
    {
        ResultadoRemuneracion resultado = new ResultadoRemuneracion();
        int sinGoce = Math.Max(datos.DiasSinGoce, 0);
        int faltas = Math.Max(datos.DiasFalta, 0);
        resultado.DiasLaborados = Math.Max(datos.DiasContrato - sinGoce - faltas, 0);
        resultado.CodigoPension = datos.Pension?.Codigo;
        resultado.MinutosExtra = datos.MinutosExtraPorJornada.Where(minutos => minutos > 0).Sum();
        if (datos.Pension is null)
        {
            resultado.Incidencia = IncidenciaSinAfiliacion;
            return resultado;
        }

        decimal ingresoBasico = Redondear(datos.SueldoBasico * resultado.DiasLaborados / DiasMesComercial);
        decimal ingresoHorasExtra = CalcularHorasExtra(datos.SueldoBasico, datos.MinutosExtraPorJornada, parametros);
        decimal ingresoBonos = Redondear(datos.IngresoBonos);
        decimal totalIngresos = ingresoBasico + ingresoHorasExtra + ingresoBonos;
        decimal pension = CalcularPension(totalIngresos, datos.Pension);
        decimal quinta = CalcularQuinta(totalIngresos, parametros);
        if (pension + quinta > totalIngresos)
        {
            resultado.Incidencia = IncidenciaNetoNegativo;
            return resultado;
        }

        resultado.IngresoBasico = ingresoBasico;
        resultado.IngresoHorasExtra = ingresoHorasExtra;
        resultado.IngresoBonos = ingresoBonos;
        resultado.TotalIngresos = totalIngresos;
        resultado.DescuentoPension = pension;
        resultado.DescuentoQuinta = quinta;
        resultado.TotalDescuentos = pension + quinta;
        resultado.Neto = totalIngresos - resultado.TotalDescuentos;
        resultado.SaldoParaPrestamos = resultado.Neto;
        resultado.AporteEssalud = Redondear(totalIngresos * parametros.TasaEssalud);
        return resultado;
    }

    // Los descuentos de préstamos se aplican después de pensión y quinta y no pueden superar el neto restante.
    public static ResultadoRemuneracion AplicarPrestamos(ResultadoRemuneracion resultado, decimal descuentoPrestamos)
    {
        decimal descuento = Math.Min(Math.Max(descuentoPrestamos, 0m), resultado.SaldoParaPrestamos);
        resultado.DescuentoPrestamos = descuento;
        resultado.TotalDescuentos = resultado.DescuentoPension + resultado.DescuentoQuinta + descuento;
        resultado.Neto = resultado.TotalIngresos - resultado.TotalDescuentos;
        resultado.SaldoParaPrestamos = resultado.Neto;
        return resultado;
    }

    // Valor hora = sueldo / 30 / horas de la jornada. Por jornada, los primeros minutos con el recargo del 25 % y el resto con el del 35 %.
    public static decimal CalcularHorasExtra(decimal sueldoBasico, IEnumerable<int> minutosPorJornada, ParametrosRemuneracion parametros)
    {
        decimal valorHora = sueldoBasico / DiasMesComercial / parametros.HorasJornada;
        decimal acumulado = 0m;
        foreach (int minutos in minutosPorJornada)
        {
            if (minutos <= 0)
            {
                continue;
            }

            int minutos25 = Math.Min(minutos, parametros.MinutosRecargo25);
            int minutos35 = minutos - minutos25;
            acumulado += minutos25 * (1m + parametros.RecargoExtra25) + minutos35 * (1m + parametros.RecargoExtra35);
        }

        return Redondear(valorHora * acumulado / MinutosPorHora);
    }

    // ONP: tasa sobre los ingresos. AFP: aporte y comisión sobre los ingresos, más la prima sobre min(ingresos, tope asegurable).
    public static decimal CalcularPension(decimal totalIngresos, PensionRemuneracion pension)
    {
        decimal aporte = Redondear(totalIngresos * pension.TasaAporte);
        if (pension.Sistema != SistemaAfp)
        {
            return aporte;
        }

        decimal comision = Redondear(totalIngresos * pension.TasaComision);
        decimal baseSeguro = pension.RemuneracionMaximaAsegurable is null
            ? totalIngresos
            : Math.Min(totalIngresos, pension.RemuneracionMaximaAsegurable.Value);
        decimal prima = Redondear(baseSeguro * pension.TasaPrimaSeguro);
        return aporte + comision + prima;
    }

    // Método simplificado: proyección anual (ingresos x 12) menos 7 UIT, impuesto por tramos acumulados y retención = impuesto / 12.
    public static decimal CalcularQuinta(decimal totalIngresos, ParametrosRemuneracion parametros)
    {
        decimal baseImponible = totalIngresos * 12m - parametros.DeduccionUitQuinta * parametros.Uit;
        if (baseImponible <= 0m)
        {
            return 0m;
        }

        decimal impuesto = 0m;
        decimal limiteAnterior = 0m;
        foreach (TramoQuintaRemuneracion tramo in parametros.Tramos)
        {
            if (baseImponible <= limiteAnterior)
            {
                break;
            }

            decimal limite = tramo.HastaUit is null ? decimal.MaxValue : tramo.HastaUit.Value * parametros.Uit;
            decimal tope = Math.Min(baseImponible, limite);
            impuesto += (tope - limiteAnterior) * tramo.Tasa;
            limiteAnterior = limite;
        }

        return Redondear(impuesto / 12m);
    }
}

public class TramoQuintaRemuneracion
{
    public decimal? HastaUit { get; set; }

    public decimal Tasa { get; set; }
}

public class PensionRemuneracion
{
    public string Codigo { get; set; } = string.Empty;

    public string Sistema { get; set; } = string.Empty;

    public decimal TasaAporte { get; set; }

    public decimal TasaPrimaSeguro { get; set; }

    public decimal TasaComision { get; set; }

    public decimal? RemuneracionMaximaAsegurable { get; set; }
}

public class ParametrosRemuneracion
{
    public decimal Uit { get; set; } = 5350m;

    public decimal TasaEssalud { get; set; } = 0.09m;

    public decimal HorasJornada { get; set; } = 8m;

    public int MinutosRecargo25 { get; set; } = 120;

    public decimal RecargoExtra25 { get; set; } = 0.25m;

    public decimal RecargoExtra35 { get; set; } = 0.35m;

    public decimal DeduccionUitQuinta { get; set; } = 7m;

    // En orden ascendente; el último tramo no tiene límite.
    public List<TramoQuintaRemuneracion> Tramos { get; set; } = new List<TramoQuintaRemuneracion>();
}

public class DatosRemuneracion
{
    public decimal SueldoBasico { get; set; }

    public int DiasContrato { get; set; }

    public int DiasSinGoce { get; set; }

    public int DiasFalta { get; set; }

    // Minutos extra aprobados de cada jornada del mes.
    public List<int> MinutosExtraPorJornada { get; set; } = new List<int>();

    public decimal IngresoBonos { get; set; }

    public PensionRemuneracion? Pension { get; set; }
}

public class ResultadoRemuneracion
{
    public int DiasLaborados { get; set; }

    public decimal IngresoBasico { get; set; }

    public int MinutosExtra { get; set; }

    public decimal IngresoHorasExtra { get; set; }

    public decimal IngresoBonos { get; set; }

    public decimal TotalIngresos { get; set; }

    public string? CodigoPension { get; set; }

    public decimal DescuentoPension { get; set; }

    public decimal DescuentoQuinta { get; set; }

    public decimal DescuentoPrestamos { get; set; }

    public decimal TotalDescuentos { get; set; }

    public decimal Neto { get; set; }

    public decimal AporteEssalud { get; set; }

    public string? Incidencia { get; set; }

    // Neto disponible para descontar cuotas de préstamos (tope de DescontarCuotasAsync).
    public decimal SaldoParaPrestamos { get; set; }

    public bool Excluido
    {
        get
        {
            return Incidencia is not null;
        }
    }
}
