using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos;

internal static class ValidacionAusencia
{
    public const string TipoVacaciones = "Vacaciones";

    public const string TipoPermisoConGoce = "PermisoConGoce";

    public const string TipoPermisoSinGoce = "PermisoSinGoce";

    public const string TipoLicenciaMaternidad = "LicenciaMaternidad";

    public const string TipoLicenciaPaternidad = "LicenciaPaternidad";

    public const string TipoDescansoMedico = "DescansoMedico";

    public const string EstadoPendiente = "Pendiente";

    public const string EstadoAprobada = "Aprobada";

    public const string EstadoRechazada = "Rechazada";

    public const string EstadoCancelada = "Cancelada";

    public const string EstadoEmpleadoActivo = "Activo";

    public const int MaximoMotivo = 300;

    public const int MaximoDecididoPor = 100;

    private const int MaximoDiasMaternidad = 98;

    private const int MaximoDiasPaternidad = 10;

    private static readonly string[] Tipos =
    {
        TipoVacaciones,
        TipoPermisoConGoce,
        TipoPermisoSinGoce,
        TipoLicenciaMaternidad,
        TipoLicenciaPaternidad,
        TipoDescansoMedico
    };

    private static readonly string[] Estados = { EstadoPendiente, EstadoAprobada, EstadoRechazada, EstadoCancelada };

    private static readonly string[] TiposConMotivoObligatorio = { TipoPermisoConGoce, TipoPermisoSinGoce, TipoDescansoMedico };

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

    public static ReglaNegocioException AusenciaInexistente(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La ausencia {id} no existe.");
    }

    public static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static string NuevoNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"AUS-{id:D6}";
    }

    public static int Dias(DateOnly inicio, DateOnly fin)
    {
        return fin.DayNumber - inicio.DayNumber + 1;
    }

    // Reglas 1 a 9 del registro, en el orden de la spec. Devuelve el motivo normalizado (null si queda vacío).
    public static string? ValidarSolicitud(RegistrarAusenciaRequestDto solicitud, out string tipo)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw Error400("El empleado es obligatorio.");
        }

        tipo = solicitud.Tipo?.Trim() ?? string.Empty;
        if (!Tipos.Contains(tipo))
        {
            throw Error400("El tipo debe ser Vacaciones, PermisoConGoce, PermisoSinGoce, LicenciaMaternidad, LicenciaPaternidad o DescansoMedico.");
        }

        if (solicitud.FechaInicio is null)
        {
            throw Error400("La fecha de inicio es obligatoria.");
        }

        if (solicitud.FechaFin is null)
        {
            throw Error400("La fecha de fin es obligatoria.");
        }

        if (solicitud.FechaFin.Value < solicitud.FechaInicio.Value)
        {
            throw Error400("La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0 && TiposConMotivoObligatorio.Contains(tipo))
        {
            throw Error400("El motivo es obligatorio para este tipo de ausencia.");
        }

        if (motivo.Length > MaximoMotivo)
        {
            throw Error400("El motivo no puede superar 300 caracteres.");
        }

        int dias = Dias(solicitud.FechaInicio.Value, solicitud.FechaFin.Value);
        if (tipo == TipoLicenciaMaternidad && dias > MaximoDiasMaternidad)
        {
            throw Error400("La licencia de maternidad no puede superar 98 días.");
        }

        if (tipo == TipoLicenciaPaternidad && dias > MaximoDiasPaternidad)
        {
            throw Error400("La licencia de paternidad no puede superar 10 días.");
        }

        return motivo.Length == 0 ? null : motivo;
    }

    public static string ValidarMotivoRechazo(string? motivo)
    {
        string normalizado = motivo?.Trim() ?? string.Empty;
        if (normalizado.Length == 0)
        {
            throw Error400("El motivo del rechazo es obligatorio.");
        }

        if (normalizado.Length > MaximoMotivo)
        {
            throw Error400("El motivo del rechazo no puede superar 300 caracteres.");
        }

        return normalizado;
    }

    public static void ValidarListado(int pagina, int tamanoPagina, DateOnly? desde, DateOnly? hasta, string tipo, string estado)
    {
        if (pagina < 1)
        {
            throw Error400("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Error400("El tamaño de página debe estar entre 1 y 100.");
        }

        if (tipo.Length > 0 && !Tipos.Contains(tipo))
        {
            throw FiltroInvalido("tipo");
        }

        if (estado.Length > 0 && !Estados.Contains(estado))
        {
            throw FiltroInvalido("estado");
        }

        if (desde is not null && hasta is not null && desde.Value > hasta.Value)
        {
            throw FiltroInvalido("hasta");
        }
    }

    public static async Task<Empleado> ObtenerEmpleadoAsync(IUnitOfWork unidad, int empleadoId)
    {
        Empleado? empleado = await unidad.Repository<Empleado>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Id == empleadoId);
        if (empleado is null)
        {
            throw EmpleadoInexistente(empleadoId);
        }

        return empleado;
    }

    // Regla 13: se cruza con otra ausencia pendiente o aprobada del empleado (distinta de la indicada).
    public static async Task ValidarSinSolapeAsync(IUnitOfWork unidad, int empleadoId, DateOnly inicio, DateOnly fin, int? excluirId)
    {
        bool solapa = await unidad.Repository<Ausencia>().AsQueryable()
            .AnyAsync(otra => otra.EmpleadoId == empleadoId
                && (excluirId == null || otra.Id != excluirId.Value)
                && (otra.Estado == EstadoPendiente || otra.Estado == EstadoAprobada)
                && otra.FechaInicio <= fin
                && otra.FechaFin >= inicio);
        if (solapa)
        {
            throw Error409("La ausencia se cruza con otra ausencia pendiente o aprobada del empleado.");
        }
    }

    // Regla 14: el saldo a la fecha de inicio de la solicitud debe cubrir sus días.
    public static async Task ValidarSaldoAsync(IUnitOfWork unidad, Empleado empleado, DateOnly inicio, int dias, int? excluirId)
    {
        List<CalculadoraSaldoVacacional.PeriodoVacacional> periodos = await LeerPeriodosAsync(unidad, empleado.Id, excluirId);
        decimal devengados = CalculadoraSaldoVacacional.Devengados(empleado.FechaIngreso, inicio);
        int gozados = CalculadoraSaldoVacacional.Gozados(periodos, inicio);
        int programados = CalculadoraSaldoVacacional.Programados(periodos, inicio);
        decimal saldo = CalculadoraSaldoVacacional.Saldo(devengados, gozados, programados);
        if (saldo < dias)
        {
            throw Error409($"El saldo vacacional es de {saldo.ToString("0.0", CultureInfo.InvariantCulture)} días y la solicitud pide {dias}.");
        }
    }

    public static async Task<List<CalculadoraSaldoVacacional.PeriodoVacacional>> LeerPeriodosAsync(IUnitOfWork unidad, int empleadoId, int? excluirId)
    {
        List<Ausencia> ausencias = await unidad.Repository<Ausencia>().AsQueryable()
            .AsNoTracking()
            .Where(ausencia => ausencia.EmpleadoId == empleadoId
                && ausencia.Tipo == TipoVacaciones
                && (excluirId == null || ausencia.Id != excluirId.Value)
                && (ausencia.Estado == EstadoPendiente || ausencia.Estado == EstadoAprobada))
            .ToListAsync();
        return ausencias
            .Select(ausencia => new CalculadoraSaldoVacacional.PeriodoVacacional(
                ausencia.FechaInicio,
                ausencia.FechaFin,
                ausencia.Estado == EstadoAprobada))
            .ToList();
    }

    public static async Task<Ausencia> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        Ausencia? ausencia = await unidad.Repository<Ausencia>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (ausencia is null)
        {
            throw AusenciaInexistente(id);
        }

        return ausencia;
    }

    public static ReglaNegocioException NoPendiente(string numero)
    {
        return Error409($"La ausencia {numero} no está pendiente.");
    }

    private static ReglaNegocioException FiltroInvalido(string nombre)
    {
        return Error400($"El filtro {nombre} no es válido.");
    }
}
