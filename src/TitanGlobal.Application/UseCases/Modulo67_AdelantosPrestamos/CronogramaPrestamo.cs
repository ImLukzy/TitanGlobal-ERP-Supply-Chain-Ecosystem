namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos;

// Cronograma de cuotas mensuales de un préstamo (L1): cuota redondeada a 2 decimales y la última absorbe la diferencia.
internal static class CronogramaPrestamo
{
    public sealed record CuotaProgramada(int NumeroCuota, int Anio, int Mes, decimal Monto);

    public static List<CuotaProgramada> Generar(decimal monto, int numeroCuotas, int primerAnio, int primerMes)
    {
        decimal cuota = Math.Round(monto / numeroCuotas, 2, MidpointRounding.AwayFromZero);
        List<CuotaProgramada> cuotas = new List<CuotaProgramada>();
        decimal acumulado = 0m;
        for (int indice = 0; indice < numeroCuotas; indice++)
        {
            DateOnly mes = new DateOnly(primerAnio, primerMes, 1).AddMonths(indice);
            decimal importe = indice == numeroCuotas - 1 ? monto - acumulado : cuota;
            acumulado += importe;
            cuotas.Add(new CuotaProgramada(indice + 1, mes.Year, mes.Month, importe));
        }

        return cuotas;
    }
}
