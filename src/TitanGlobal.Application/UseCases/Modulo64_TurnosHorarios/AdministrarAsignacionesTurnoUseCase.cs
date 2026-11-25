using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;

public class AdministrarAsignacionesTurnoUseCase
{
    private const int MaximoDiasProgramacion = 31;

    private readonly IUnitOfWork _unidad;

    public AdministrarAsignacionesTurnoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaAsignacionesTurnoDto> ListarAsync(
        int? empleadoId,
        int? turnoId,
        string? estado,
        int pagina,
        int tamanoPagina)
    {
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionTurno.ValidarListadoAsignaciones(pagina, tamanoPagina, estadoNormalizado);
        DateOnly hoy = ValidacionTurno.Hoy();
        IQueryable<AsignacionTurno> consulta = _unidad.Repository<AsignacionTurno>().AsQueryable();
        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(asignacion => asignacion.EmpleadoId == idEmpleado);
        }

        if (turnoId is not null)
        {
            int idTurno = turnoId.Value;
            consulta = consulta.Where(asignacion => asignacion.TurnoId == idTurno);
        }

        consulta = AplicarFiltroEstado(consulta, estadoNormalizado, hoy);
        int total = await consulta.CountAsync();
        List<AsignacionTurnoListadoDto> items = await consulta
            .OrderByDescending(asignacion => asignacion.FechaInicio)
            .ThenByDescending(asignacion => asignacion.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(asignacion => new AsignacionTurnoListadoDto
            {
                Id = asignacion.Id,
                EmpleadoId = asignacion.EmpleadoId,
                EmpleadoLegajo = asignacion.Empleado.Legajo,
                EmpleadoNombre = asignacion.Empleado.Apellidos + ", " + asignacion.Empleado.Nombres,
                TurnoId = asignacion.TurnoId,
                TurnoCodigo = asignacion.Turno.Codigo,
                TurnoNombre = asignacion.Turno.Nombre,
                FechaInicio = asignacion.FechaInicio,
                FechaFin = asignacion.FechaFin,
                Estado = asignacion.FechaInicio > hoy
                    ? ValidacionTurno.EstadoFutura
                    : (asignacion.FechaFin != null && asignacion.FechaFin < hoy
                        ? ValidacionTurno.EstadoFinalizada
                        : ValidacionTurno.EstadoVigente)
            })
            .ToListAsync();
        return new PaginaAsignacionesTurnoDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<List<DiaProgramadoDto>> ProgramacionAsync(int? empleadoId, DateOnly? desde, DateOnly? hasta)
    {
        if (empleadoId is null || desde is null || hasta is null)
        {
            throw ValidacionTurno.Error400("El empleado y el rango de fechas son obligatorios.");
        }

        int idEmpleado = empleadoId.Value;
        DateOnly inicio = desde.Value;
        DateOnly fin = hasta.Value;
        if (fin < inicio)
        {
            throw ValidacionTurno.Error400("La fecha hasta no puede ser anterior a la fecha desde.");
        }

        if (fin.DayNumber - inicio.DayNumber + 1 > MaximoDiasProgramacion)
        {
            throw ValidacionTurno.Error400("El rango de la programación no puede superar 31 días.");
        }

        Empleado? empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == idEmpleado);
        if (empleado is null)
        {
            throw ValidacionTurno.EmpleadoInexistente(idEmpleado);
        }

        List<AsignacionTurno> asignaciones = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .Include(asignacion => asignacion.Turno)
            .Where(asignacion => asignacion.EmpleadoId == idEmpleado
                && asignacion.FechaInicio <= fin
                && (asignacion.FechaFin == null || asignacion.FechaFin >= inicio))
            .ToListAsync();
        List<DiaProgramadoDto> dias = new List<DiaProgramadoDto>();
        for (DateOnly fecha = inicio; fecha <= fin; fecha = fecha.AddDays(1))
        {
            DateOnly dia = fecha;
            AsignacionTurno? asignacionDelDia = asignaciones.FirstOrDefault(asignacion =>
                asignacion.FechaInicio <= dia && (asignacion.FechaFin == null || asignacion.FechaFin >= dia));
            dias.Add(CalculadoraTurnos.CalcularDia(asignacionDelDia?.Turno, fecha, empleado.FechaCese));
        }

        return dias;
    }

    public async Task<AsignacionTurnoIdDto> CrearAsync(GuardarAsignacionTurnoRequestDto solicitud)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw ValidacionTurno.Error400("El empleado es obligatorio.");
        }

        if (solicitud.TurnoId is null)
        {
            throw ValidacionTurno.Error400("El turno es obligatorio.");
        }

        if (solicitud.FechaInicio is null)
        {
            throw ValidacionTurno.Error400("La fecha de inicio es obligatoria.");
        }

        int empleadoId = solicitud.EmpleadoId.Value;
        int turnoId = solicitud.TurnoId.Value;
        DateOnly fechaInicio = solicitud.FechaInicio.Value;
        DateOnly? fechaFin = solicitud.FechaFin;
        if (fechaFin is not null && fechaFin.Value < fechaInicio)
        {
            throw ValidacionTurno.Error400("La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        Empleado? empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == empleadoId);
        if (empleado is null)
        {
            throw ValidacionTurno.EmpleadoInexistente(empleadoId);
        }

        if (empleado.Estado == ValidacionTurno.EstadoEmpleadoCesado)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El empleado {empleadoId} está cesado y no admite turnos.");
        }

        if (fechaInicio < empleado.FechaIngreso)
        {
            throw ValidacionTurno.Error400("La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        }

        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, turnoId);
        if (!turno.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El turno {turno.Codigo} está inactivo y no admite asignaciones.");
        }

        AsignacionTurno asignacion = new AsignacionTurno
        {
            EmpleadoId = empleadoId,
            TurnoId = turnoId,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            List<AsignacionTurno> existentes = await _unidad.Repository<AsignacionTurno>().AsQueryable()
                .Where(otra => otra.EmpleadoId == empleadoId)
                .ToListAsync();
            // H4: la asignación abierta que empezó antes se cierra el día anterior; cualquier otro cruce es conflicto.
            AsignacionTurno? abierta = existentes.FirstOrDefault(otra => otra.FechaFin == null && otra.FechaInicio < fechaInicio);
            DateOnly finNuevo = fechaFin ?? DateOnly.MaxValue;
            bool cruce = existentes.Any(otra => otra != abierta
                && otra.FechaInicio <= finNuevo
                && (otra.FechaFin == null || otra.FechaFin.Value >= fechaInicio));
            if (cruce)
            {
                throw ValidacionTurno.AsignacionCruzada(empleadoId);
            }

            if (abierta is not null)
            {
                abierta.FechaFin = fechaInicio.AddDays(-1);
            }

            await _unidad.Repository<AsignacionTurno>().AddAsync(asignacion);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionTurno.EsAsignacionCruzada(excepcion))
        {
            throw ValidacionTurno.AsignacionCruzada(empleadoId);
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionTurno.EsEmpleadoInexistente(excepcion))
        {
            throw ValidacionTurno.EmpleadoInexistente(empleadoId);
        }

        return new AsignacionTurnoIdDto { Id = asignacion.Id };
    }

    public async Task<AsignacionTurnoIdDto> FinalizarAsync(int id, FinalizarAsignacionTurnoRequestDto solicitud)
    {
        AsignacionTurno asignacion = await ObtenerAsync(id);
        if (solicitud.FechaFin is null)
        {
            throw ValidacionTurno.Error400("La fecha de fin es obligatoria.");
        }

        DateOnly fechaFin = solicitud.FechaFin.Value;
        if (fechaFin < asignacion.FechaInicio)
        {
            throw ValidacionTurno.Error400("La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        if (asignacion.FechaFin is not null && asignacion.FechaFin.Value <= fechaFin)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "La asignación ya termina en esa fecha o antes.");
        }

        asignacion.FechaFin = fechaFin;
        await _unidad.CompleteAsync();
        return new AsignacionTurnoIdDto { Id = asignacion.Id };
    }

    public async Task EliminarAsync(int id)
    {
        AsignacionTurno asignacion = await ObtenerAsync(id);
        if (asignacion.FechaInicio <= ValidacionTurno.Hoy())
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Solo se puede eliminar una asignación que todavía no empezó.");
        }

        _unidad.Repository<AsignacionTurno>().Remove(asignacion);
        await _unidad.CompleteAsync();
    }

    private async Task<AsignacionTurno> ObtenerAsync(int id)
    {
        AsignacionTurno? asignacion = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (asignacion is null)
        {
            throw ValidacionTurno.AsignacionInexistente(id);
        }

        return asignacion;
    }

    private static IQueryable<AsignacionTurno> AplicarFiltroEstado(
        IQueryable<AsignacionTurno> consulta,
        string estado,
        DateOnly hoy)
    {
        if (estado == ValidacionTurno.EstadoFutura)
        {
            return consulta.Where(asignacion => asignacion.FechaInicio > hoy);
        }

        if (estado == ValidacionTurno.EstadoFinalizada)
        {
            return consulta.Where(asignacion => asignacion.FechaInicio <= hoy
                && asignacion.FechaFin != null
                && asignacion.FechaFin < hoy);
        }

        if (estado == ValidacionTurno.EstadoVigente)
        {
            return consulta.Where(asignacion => asignacion.FechaInicio <= hoy
                && (asignacion.FechaFin == null || asignacion.FechaFin >= hoy));
        }

        return consulta;
    }
}
