using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;

internal static class ValidacionTurno
{
    public const string EstadoVigente = "Vigente";

    public const string EstadoFutura = "Futura";

    public const string EstadoFinalizada = "Finalizada";

    public const string EstadoEmpleadoCesado = "Cesado";

    public const string IndiceCodigoUnico = "IX_Turnos_Codigo";

    public const string IndiceAsignacionUnica = "IX_AsignacionesTurno_EmpleadoId_FechaInicio";

    public const string ClaveForaneaEmpleado = "FK_AsignacionesTurno_Empleados_EmpleadoId";

    private static readonly string[] Tipos = { CalculadoraTurnos.TipoFijo, CalculadoraTurnos.TipoRotativo };

    private static readonly string[] Estados = { EstadoVigente, EstadoFutura, EstadoFinalizada };

    // Reglas 1 a 19 en el orden de la spec; gana el primer fallo. Devuelve el turno ya normalizado (sin guardar).
    public static Turno ValidarTurno(GuardarTurnoRequestDto solicitud)
    {
        string codigo = solicitud.Codigo?.Trim() ?? string.Empty;
        if (codigo.Length == 0)
        {
            throw Error400("El código es obligatorio.");
        }

        if (codigo.Length > 20)
        {
            throw Error400("El código no puede superar 20 caracteres.");
        }

        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0)
        {
            throw Error400("El nombre es obligatorio.");
        }

        if (nombre.Length > 100)
        {
            throw Error400("El nombre no puede superar 100 caracteres.");
        }

        string tipo = solicitud.Tipo?.Trim() ?? string.Empty;
        if (!Tipos.Contains(tipo))
        {
            throw Error400("El tipo de turno debe ser Fijo o Rotativo.");
        }

        if (solicitud.HoraEntrada is null)
        {
            throw Error400("La hora de entrada es obligatoria.");
        }

        if (solicitud.HoraSalida is null)
        {
            throw Error400("La hora de salida es obligatoria.");
        }

        TimeOnly entrada = solicitud.HoraEntrada.Value;
        TimeOnly salida = solicitud.HoraSalida.Value;
        if (salida == entrada)
        {
            throw Error400("La hora de salida no puede ser igual a la hora de entrada.");
        }

        if (salida < entrada && !solicitud.CruzaMedianoche)
        {
            throw Error400("Si la hora de salida es anterior a la de entrada, el turno debe cruzar medianoche.");
        }

        if (salida > entrada && solicitud.CruzaMedianoche)
        {
            throw Error400("El turno solo cruza medianoche si la hora de salida es anterior a la de entrada.");
        }

        if (solicitud.ToleranciaMinutos is null)
        {
            throw Error400("La tolerancia es obligatoria.");
        }

        int tolerancia = solicitud.ToleranciaMinutos.Value;
        if (tolerancia < 0 || tolerancia > 30)
        {
            throw Error400("La tolerancia debe estar entre 0 y 30 minutos.");
        }

        Turno turno = new Turno
        {
            Codigo = codigo.ToUpperInvariant(),
            Nombre = nombre,
            Tipo = tipo,
            HoraEntrada = entrada,
            HoraSalida = salida,
            CruzaMedianoche = solicitud.CruzaMedianoche,
            ToleranciaMinutos = tolerancia
        };
        if (tipo == CalculadoraTurnos.TipoFijo)
        {
            ValidarFijo(solicitud, turno);
        }
        else
        {
            ValidarRotativo(solicitud, turno);
        }

        return turno;
    }

    public static void ValidarListadoTurnos(int pagina, int tamanoPagina, string tipo)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        if (tipo.Length > 0 && !Tipos.Contains(tipo))
        {
            throw Error400("El tipo debe ser Fijo o Rotativo.");
        }
    }

    public static void ValidarListadoAsignaciones(int pagina, int tamanoPagina, string estado)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        if (estado.Length > 0 && !Estados.Contains(estado))
        {
            throw Error400("El estado debe ser Vigente, Futura o Finalizada.");
        }
    }

    public static void CopiarEn(Turno turno, TurnoListadoDto dto)
    {
        dto.Id = turno.Id;
        dto.Codigo = turno.Codigo;
        dto.Nombre = turno.Nombre;
        dto.Tipo = turno.Tipo;
        dto.HoraEntrada = turno.HoraEntrada;
        dto.HoraSalida = turno.HoraSalida;
        dto.CruzaMedianoche = turno.CruzaMedianoche;
        dto.ToleranciaMinutos = turno.ToleranciaMinutos;
        dto.DiasLaborables = CalculadoraTurnos.LeerDiasLaborables(turno.DiasLaborables);
        dto.DiasTrabajo = turno.DiasTrabajo;
        dto.DiasDescanso = turno.DiasDescanso;
        dto.FechaInicioCiclo = turno.FechaInicioCiclo;
        dto.Activo = turno.Activo;
    }

    public static bool EsCodigoDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == IndiceCodigoUnico;
    }

    public static bool EsAsignacionCruzada(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == IndiceAsignacionUnica;
    }

    public static bool EsEmpleadoInexistente(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && excepcion.Restriccion == ClaveForaneaEmpleado;
    }

    public static ReglaNegocioException CodigoDuplicado(string codigo)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe un turno con el código {codigo}.");
    }

    public static ReglaNegocioException TurnoInexistente(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El turno {id} no existe.");
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El empleado {empleadoId} no existe.");
    }

    public static ReglaNegocioException AsignacionCruzada(int empleadoId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"El empleado {empleadoId} ya tiene un turno asignado que se cruza con esas fechas.");
    }

    public static ReglaNegocioException AsignacionInexistente(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La asignación de turno {id} no existe.");
    }

    public static async Task<Turno> ObtenerTurnoAsync(IUnitOfWork unidad, int id)
    {
        Turno? turno = await unidad.Repository<Turno>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (turno is null)
        {
            throw TurnoInexistente(id);
        }

        return turno;
    }

    public static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static ReglaNegocioException Error400(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
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

    // Reglas 13, 14 y 19.
    private static void ValidarFijo(GuardarTurnoRequestDto solicitud, Turno turno)
    {
        List<int> dias = solicitud.DiasLaborables ?? new List<int>();
        if (dias.Count == 0)
        {
            throw Error400("El turno fijo debe tener al menos un día laborable.");
        }

        if (dias.Any(dia => dia < 1 || dia > 7) || dias.Distinct().Count() != dias.Count)
        {
            throw Error400("Los días laborables deben ser números del 1 (lunes) al 7 (domingo) sin repetir.");
        }

        if (solicitud.DiasTrabajo is not null || solicitud.DiasDescanso is not null || solicitud.FechaInicioCiclo is not null)
        {
            throw Error400("El turno fijo no usa ciclo de trabajo y descanso.");
        }

        turno.DiasLaborables = string.Join(",", dias.OrderBy(dia => dia));
    }

    // Reglas 15 a 18.
    private static void ValidarRotativo(GuardarTurnoRequestDto solicitud, Turno turno)
    {
        if (solicitud.DiasLaborables is not null && solicitud.DiasLaborables.Count > 0)
        {
            throw Error400("El turno rotativo no usa días laborables de la semana.");
        }

        if (solicitud.DiasTrabajo is null || solicitud.DiasDescanso is null)
        {
            throw Error400("El turno rotativo necesita días de trabajo y días de descanso.");
        }

        int trabajo = solicitud.DiasTrabajo.Value;
        int descanso = solicitud.DiasDescanso.Value;
        if (trabajo < 1 || trabajo > 30 || descanso < 1 || descanso > 30)
        {
            throw Error400("Los días de trabajo y de descanso deben estar entre 1 y 30.");
        }

        if (solicitud.FechaInicioCiclo is null)
        {
            throw Error400("La fecha de inicio del ciclo es obligatoria en el turno rotativo.");
        }

        turno.DiasLaborables = null;
        turno.DiasTrabajo = trabajo;
        turno.DiasDescanso = descanso;
        turno.FechaInicioCiclo = solicitud.FechaInicioCiclo;
    }
}
