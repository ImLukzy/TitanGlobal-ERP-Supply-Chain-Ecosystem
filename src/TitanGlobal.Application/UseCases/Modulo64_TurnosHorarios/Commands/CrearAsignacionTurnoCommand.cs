using MediatR;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;

public class CrearAsignacionTurnoCommand : IRequest<AsignacionTurnoIdDto>
{
    public required GuardarAsignacionTurnoRequestDto Solicitud { get; set; }
}

public class CrearAsignacionTurnoCommandHandler : IRequestHandler<CrearAsignacionTurnoCommand, AsignacionTurnoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearAsignacionTurnoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AsignacionTurnoIdDto> Handle(CrearAsignacionTurnoCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.EmpleadoId is null)
        {
            throw ValidacionTurno.Error400("El empleado es obligatorio.");
        }

        if (request.Solicitud.TurnoId is null)
        {
            throw ValidacionTurno.Error400("El turno es obligatorio.");
        }

        if (request.Solicitud.FechaInicio is null)
        {
            throw ValidacionTurno.Error400("La fecha de inicio es obligatoria.");
        }

        int empleadoId = request.Solicitud.EmpleadoId.Value;
        int turnoId = request.Solicitud.TurnoId.Value;
        DateOnly fechaInicio = request.Solicitud.FechaInicio.Value;
        DateOnly? fechaFin = request.Solicitud.FechaFin;
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
}
