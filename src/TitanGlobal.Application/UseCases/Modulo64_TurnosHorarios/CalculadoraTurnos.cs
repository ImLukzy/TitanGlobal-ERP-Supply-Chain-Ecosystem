using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;

// Cálculo puro de la programación de un día. Lo reutiliza el módulo 63 (asistencia) para saber qué turno toca.
public static class CalculadoraTurnos
{
    public const string TipoFijo = "Fijo";

    public const string TipoRotativo = "Rotativo";

    public const string ObservacionSinTurno = "SinTurno";

    public const string ObservacionDescanso = "Descanso";

    public const string ObservacionCesado = "Cesado";

    public static DiaProgramadoDto CalcularDia(Turno? turno, DateOnly fecha, DateOnly? fechaCese)
    {
        DiaProgramadoDto dia = new DiaProgramadoDto { Fecha = fecha };
        if (turno is null)
        {
            dia.Laborable = false;
            dia.Observacion = ObservacionSinTurno;
            return dia;
        }

        dia.TurnoId = turno.Id;
        dia.TurnoCodigo = turno.Codigo;
        if (fechaCese is not null && fecha > fechaCese.Value)
        {
            dia.Laborable = false;
            dia.Observacion = ObservacionCesado;
            return dia;
        }

        bool laborable = EsDiaLaborable(turno, fecha);
        dia.Laborable = laborable;
        if (!laborable)
        {
            dia.Observacion = ObservacionDescanso;
            return dia;
        }

        dia.HoraEntrada = turno.HoraEntrada;
        dia.HoraSalida = turno.HoraSalida;
        dia.CruzaMedianoche = turno.CruzaMedianoche;
        dia.ToleranciaMinutos = turno.ToleranciaMinutos;
        dia.Observacion = null;
        return dia;
    }

    // Días de la semana en formato ISO: 1 (lunes) a 7 (domingo).
    public static int DiaIso(DateOnly fecha)
    {
        int dia = (int)fecha.DayOfWeek;
        return dia == 0 ? 7 : dia;
    }

    public static List<int> LeerDiasLaborables(string? diasLaborables)
    {
        List<int> dias = new List<int>();
        if (string.IsNullOrWhiteSpace(diasLaborables))
        {
            return dias;
        }

        foreach (string parte in diasLaborables.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(parte, out int dia))
            {
                dias.Add(dia);
            }
        }

        return dias;
    }

    private static bool EsDiaLaborable(Turno turno, DateOnly fecha)
    {
        if (turno.Tipo == TipoRotativo)
        {
            if (turno.FechaInicioCiclo is null || turno.DiasTrabajo is null || turno.DiasDescanso is null)
            {
                return false;
            }

            int transcurridos = fecha.DayNumber - turno.FechaInicioCiclo.Value.DayNumber;
            if (transcurridos < 0)
            {
                return false;
            }

            int ciclo = turno.DiasTrabajo.Value + turno.DiasDescanso.Value;
            return transcurridos % ciclo < turno.DiasTrabajo.Value;
        }

        return LeerDiasLaborables(turno.DiasLaborables).Contains(DiaIso(fecha));
    }
}
