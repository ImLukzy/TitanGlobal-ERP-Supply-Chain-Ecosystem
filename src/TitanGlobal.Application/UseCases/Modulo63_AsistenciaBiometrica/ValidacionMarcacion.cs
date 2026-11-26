using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;

internal static class ValidacionMarcacion
{
    public const string EstadoEmpleadoActivo = "Activo";

    public const string EstadoContratoAnulado = "Anulado";

    public const int MaximoRegistrosImportacion = 1000;

    public const int SegundosDuplicado = 60;

    private static readonly string[] Tipos = { CalculadoraJornada.TipoEntrada, CalculadoraJornada.TipoSalida };

    private static readonly string[] Origenes = { CalculadoraJornada.OrigenReloj, CalculadoraJornada.OrigenManual };

    private static readonly string[] Estados =
    {
        CalculadoraJornada.EstadoAsistencia,
        CalculadoraJornada.EstadoTardanza,
        CalculadoraJornada.EstadoSinMarcacion,
        CalculadoraJornada.EstadoFalta,
        CalculadoraJornada.EstadoDescansoTrabajado
    };

    private static readonly string[] EstadosHorasExtra =
    {
        CalculadoraJornada.HorasExtraNoAplica,
        CalculadoraJornada.HorasExtraPendiente,
        CalculadoraJornada.HorasExtraAprobada,
        CalculadoraJornada.HorasExtraRechazada
    };

    public static bool EsTipoValido(string tipo)
    {
        return Tipos.Contains(tipo);
    }

    public static ReglaNegocioException Error400(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Error409(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El empleado {empleadoId} no existe.");
    }

    public static ReglaNegocioException MarcacionInexistente(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La marcación {id} no existe.");
    }

    public static ReglaNegocioException JornadaInexistente(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La jornada {id} no existe.");
    }

    public static ReglaNegocioException JornadaDecidida(DateOnly fecha)
    {
        return Error409($"La jornada del {fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} ya tiene las horas extra decididas y no se modifica.");
    }

    public static string MarcacionDuplicada()
    {
        return "Ya existe una marcación del mismo tipo en ese minuto.";
    }

    public static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw Error400("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Error400("El tamaño de página debe estar entre 1 y 100.");
        }
    }

    public static void ValidarFiltrosMarcaciones(DateOnly? desde, DateOnly? hasta, string tipo, string origen)
    {
        ValidarRangoDeFiltros(desde, hasta);
        if (tipo.Length > 0 && !Tipos.Contains(tipo))
        {
            throw FiltroInvalido("tipo");
        }

        if (origen.Length > 0 && !Origenes.Contains(origen))
        {
            throw FiltroInvalido("origen");
        }
    }

    public static void ValidarFiltrosJornadas(DateOnly? desde, DateOnly? hasta, string estado, string estadoHorasExtra)
    {
        ValidarRangoDeFiltros(desde, hasta);
        if (estado.Length > 0 && !Estados.Contains(estado))
        {
            throw FiltroInvalido("estado");
        }

        if (estadoHorasExtra.Length > 0 && !EstadosHorasExtra.Contains(estadoHorasExtra))
        {
            throw FiltroInvalido("estadoHorasExtra");
        }
    }

    // A1 y T2: devuelve el motivo (mensajes 7 y 8) o null si el empleado puede tener marcaciones en esa fecha local.
    public static string? MotivoNoVigente(Empleado empleado, IEnumerable<ContratoLaboral> contratos, DateOnly fechaLocal)
    {
        bool activo = empleado.Estado == EstadoEmpleadoActivo
            || (empleado.FechaCese is not null && fechaLocal <= empleado.FechaCese.Value);
        if (!activo)
        {
            return $"El empleado {empleado.Id} no está activo en esa fecha.";
        }

        bool contratoVigente = contratos.Any(contrato => contrato.EmpleadoId == empleado.Id
            && contrato.Estado != EstadoContratoAnulado
            && contrato.FechaInicio <= fechaLocal
            && FinDeVigencia(contrato) >= fechaLocal);
        if (!contratoVigente)
        {
            return $"El empleado {empleado.Id} no tiene un contrato vigente en esa fecha.";
        }

        return null;
    }

    // Fecha de la jornada decidida cuya ventana contiene el instante, o null.
    public static async Task<DateOnly?> BuscarJornadaDecididaAsync(IUnitOfWork unidad, int empleadoId, DateTime instante)
    {
        DateOnly fechaLocal = CalculadoraJornada.FechaLocal(instante);
        DateOnly desde = fechaLocal.AddDays(-2);
        DateOnly hasta = fechaLocal.AddDays(1);
        List<JornadaAsistencia> decididas = await unidad.Repository<JornadaAsistencia>().AsQueryable()
            .AsNoTracking()
            .Where(jornada => jornada.EmpleadoId == empleadoId
                && jornada.Fecha >= desde
                && jornada.Fecha <= hasta
                && (jornada.EstadoHorasExtra == CalculadoraJornada.HorasExtraAprobada
                    || jornada.EstadoHorasExtra == CalculadoraJornada.HorasExtraRechazada))
            .ToListAsync();
        foreach (JornadaAsistencia jornada in decididas)
        {
            (DateTime inicio, DateTime fin) = CalculadoraJornada.Ventana(
                jornada.Fecha,
                jornada.HoraEntradaProgramada,
                jornada.HoraSalidaProgramada);
            if (instante >= inicio && instante <= fin)
            {
                return jornada.Fecha;
            }
        }

        return null;
    }

    private static DateOnly FinDeVigencia(ContratoLaboral contrato)
    {
        if (contrato.FechaCierre is not null)
        {
            return contrato.FechaCierre.Value;
        }

        return contrato.FechaFin ?? DateOnly.MaxValue;
    }

    private static void ValidarRangoDeFiltros(DateOnly? desde, DateOnly? hasta)
    {
        if (desde is not null && hasta is not null && desde.Value > hasta.Value)
        {
            throw FiltroInvalido("hasta");
        }
    }

    private static ReglaNegocioException FiltroInvalido(string nombre)
    {
        return Error400($"El filtro {nombre} no es válido.");
    }
}
