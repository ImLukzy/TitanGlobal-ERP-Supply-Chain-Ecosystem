namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos;

// Cálculo puro del saldo vacacional (V2 y V3): sin acceso a datos ni reloj.
public static class CalculadoraSaldoVacacional
{
    public const decimal DiasPorMes = 2.5m;

    // Un mes completo se cuenta al alcanzar el mismo día del mes siguiente; si ese día no existe
    // (ingreso el 31 y mes de 30 o menos días), vale el último día del mes.
    public static int MesesCompletos(DateOnly ingreso, DateOnly fecha)
    {
        int meses = ((fecha.Year - ingreso.Year) * 12) + fecha.Month - ingreso.Month;
        while (meses > 0 && ingreso.AddMonths(meses) > fecha)
        {
            meses--;
        }

        return meses < 0 ? 0 : meses;
    }

    public static decimal Devengados(DateOnly ingreso, DateOnly fecha)
    {
        return MesesCompletos(ingreso, fecha) * DiasPorMes;
    }

    // Días de vacaciones aprobadas ya transcurridos hasta la fecha, inclusive.
    public static int Gozados(IEnumerable<PeriodoVacacional> periodos, DateOnly fecha)
    {
        int total = 0;
        foreach (PeriodoVacacional periodo in periodos.Where(item => item.Aprobado))
        {
            if (fecha >= periodo.Inicio)
            {
                DateOnly ultimo = fecha < periodo.Fin ? fecha : periodo.Fin;
                total += ultimo.DayNumber - periodo.Inicio.DayNumber + 1;
            }
        }

        return total;
    }

    // Días aprobados aún no transcurridos más todos los días de las solicitudes pendientes.
    public static int Programados(IEnumerable<PeriodoVacacional> periodos, DateOnly fecha)
    {
        int total = 0;
        foreach (PeriodoVacacional periodo in periodos)
        {
            int dias = periodo.Fin.DayNumber - periodo.Inicio.DayNumber + 1;
            total += dias;
        }

        return total - Gozados(periodos, fecha);
    }

    public static decimal Saldo(decimal devengados, int gozados, int programados)
    {
        return devengados - gozados - programados;
    }

    public sealed class PeriodoVacacional
    {
        public PeriodoVacacional(DateOnly inicio, DateOnly fin, bool aprobado)
        {
            Inicio = inicio;
            Fin = fin;
            Aprobado = aprobado;
        }

        public DateOnly Inicio { get; }

        public DateOnly Fin { get; }

        public bool Aprobado { get; }
    }
}
