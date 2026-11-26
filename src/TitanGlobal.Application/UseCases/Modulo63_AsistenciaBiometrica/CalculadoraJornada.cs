using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;

// Cálculo puro de la jornada de un día contra su programación (módulo 64). Lo reutiliza el módulo 66 (planillas).
public static class CalculadoraJornada
{
    // Los turnos están en hora local de Perú (UTC-5, sin horario de verano); las marcaciones se guardan en UTC.
    public const int DesfaseHoras = -5;

    // Hasta que exista ParametrosSistema (79).
    public const int TopeMinutosExtraDiario = 240;

    public const int HorasAntesDeLaEntrada = 4;

    public const int HorasDespuesDeLaSalida = 6;

    public const string TipoEntrada = "Entrada";

    public const string TipoSalida = "Salida";

    public const string OrigenReloj = "Reloj";

    public const string OrigenManual = "Manual";

    public const string EstadoAsistencia = "Asistencia";

    public const string EstadoTardanza = "Tardanza";

    public const string EstadoSinMarcacion = "SinMarcacion";

    public const string EstadoFalta = "Falta";

    public const string EstadoDescansoTrabajado = "DescansoTrabajado";

    public const string HorasExtraNoAplica = "NoAplica";

    public const string HorasExtraPendiente = "Pendiente";

    public const string HorasExtraAprobada = "Aprobada";

    public const string HorasExtraRechazada = "Rechazada";

    public static DateTime LocalAUtc(DateTime local)
    {
        return DateTime.SpecifyKind(local.AddHours(-DesfaseHoras), DateTimeKind.Utc);
    }

    public static DateTime UtcALocal(DateTime utc)
    {
        return DateTime.SpecifyKind(FechasUtc.Normalizar(utc).AddHours(DesfaseHoras), DateTimeKind.Unspecified);
    }

    public static DateOnly FechaLocal(DateTime instante)
    {
        return DateOnly.FromDateTime(UtcALocal(instante));
    }

    public static DateOnly HoyLocal()
    {
        return FechaLocal(DateTime.UtcNow);
    }

    // Ventana (en UTC) en la que se buscan las marcaciones de una jornada. Sin programación: todo el día local.
    public static (DateTime InicioUtc, DateTime FinUtc) Ventana(DateOnly fecha, TimeOnly? entrada, TimeOnly? salida)
    {
        if (entrada is null || salida is null)
        {
            DateTime medianoche = fecha.ToDateTime(TimeOnly.MinValue);
            return (LocalAUtc(medianoche), LocalAUtc(medianoche.AddDays(1).AddTicks(-1)));
        }

        DateTime entradaLocal = fecha.ToDateTime(entrada.Value);
        DateTime salidaLocal = fecha.ToDateTime(salida.Value);
        if (salida.Value < entrada.Value)
        {
            salidaLocal = salidaLocal.AddDays(1);
        }

        return (
            LocalAUtc(entradaLocal.AddHours(-HorasAntesDeLaEntrada)),
            LocalAUtc(salidaLocal.AddHours(HorasDespuesDeLaSalida)));
    }

    // Devuelve null cuando el día no genera jornada (no laborable y sin marcaciones). EmpleadoId lo asigna quien llama.
    public static JornadaAsistencia? Calcular(DiaProgramadoDto dia, IReadOnlyList<Marcacion> marcaciones)
    {
        bool laborable = dia.Laborable && dia.HoraEntrada is not null && dia.HoraSalida is not null;
        TimeOnly? horaEntrada = laborable ? dia.HoraEntrada : null;
        TimeOnly? horaSalida = laborable ? dia.HoraSalida : null;
        (DateTime inicio, DateTime fin) = Ventana(dia.Fecha, horaEntrada, horaSalida);
        List<Marcacion> enVentana = marcaciones
            .Where(marcacion =>
            {
                DateTime instante = FechasUtc.Normalizar(marcacion.Instante);
                return instante >= inicio && instante <= fin;
            })
            .OrderBy(marcacion => marcacion.Instante)
            .ToList();
        JornadaAsistencia jornada = new JornadaAsistencia
        {
            Fecha = dia.Fecha,
            TurnoId = dia.TurnoId,
            HoraEntradaProgramada = horaEntrada,
            HoraSalidaProgramada = horaSalida,
            ToleranciaMinutos = laborable ? dia.ToleranciaMinutos ?? 0 : 0,
            EstadoHorasExtra = HorasExtraNoAplica
        };
        if (enVentana.Count == 0)
        {
            if (!laborable)
            {
                return null;
            }

            jornada.Estado = EstadoFalta;
            return jornada;
        }

        Marcacion? primeraEntrada = enVentana.FirstOrDefault(marcacion => marcacion.Tipo == TipoEntrada);
        Marcacion? ultimaSalida = primeraEntrada is null
            ? enVentana.LastOrDefault(marcacion => marcacion.Tipo == TipoSalida)
            : enVentana.LastOrDefault(marcacion => marcacion.Tipo == TipoSalida && marcacion.Instante > primeraEntrada.Instante);
        jornada.Entrada = primeraEntrada is null ? null : FechasUtc.Normalizar(primeraEntrada.Instante);
        jornada.Salida = ultimaSalida is null ? null : FechasUtc.Normalizar(ultimaSalida.Instante);
        if (primeraEntrada is null || ultimaSalida is null)
        {
            jornada.Estado = EstadoSinMarcacion;
            return jornada;
        }

        DateTime entrada = jornada.Entrada!.Value;
        DateTime salida = jornada.Salida!.Value;
        jornada.MinutosTrabajados = Minutos(entrada, salida);
        if (!laborable)
        {
            jornada.Estado = EstadoDescansoTrabajado;
            jornada.MinutosExtra = Math.Min(jornada.MinutosTrabajados, TopeMinutosExtraDiario);
        }
        else
        {
            DateTime entradaProgramada = LocalAUtc(dia.Fecha.ToDateTime(horaEntrada!.Value));
            DateTime salidaProgramada = LocalAUtc(dia.Fecha.ToDateTime(horaSalida!.Value));
            if (horaSalida.Value < horaEntrada.Value)
            {
                salidaProgramada = salidaProgramada.AddDays(1);
            }

            jornada.MinutosTardanza = Minutos(entradaProgramada.AddMinutes(jornada.ToleranciaMinutos), entrada);
            int antes = Minutos(entrada, entrada < entradaProgramada ? (salida < entradaProgramada ? salida : entradaProgramada) : entrada);
            DateTime inicioDespues = entrada > salidaProgramada ? entrada : salidaProgramada;
            int despues = Minutos(inicioDespues, salida);
            jornada.MinutosExtra = Math.Min(antes + despues, TopeMinutosExtraDiario);
            jornada.Estado = jornada.MinutosTardanza > 0 ? EstadoTardanza : EstadoAsistencia;
        }

        jornada.EstadoHorasExtra = jornada.MinutosExtra > 0 ? HorasExtraPendiente : HorasExtraNoAplica;
        return jornada;
    }

    // Minutos completos entre dos instantes; nunca negativos.
    private static int Minutos(DateTime desde, DateTime hasta)
    {
        double minutos = Math.Floor((hasta - desde).TotalMinutes);
        return minutos > 0 ? (int)minutos : 0;
    }
}
