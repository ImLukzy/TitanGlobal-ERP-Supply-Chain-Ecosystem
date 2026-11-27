using MediatR;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;

public class FinalizarAsignacionTurnoCommand : IRequest<AsignacionTurnoIdDto>
{
    public int Id { get; set; }

    public required FinalizarAsignacionTurnoRequestDto Solicitud { get; set; }
}

public class FinalizarAsignacionTurnoCommandHandler : IRequestHandler<FinalizarAsignacionTurnoCommand, AsignacionTurnoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public FinalizarAsignacionTurnoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AsignacionTurnoIdDto> Handle(FinalizarAsignacionTurnoCommand request, CancellationToken cancellationToken)
    {
        AsignacionTurno asignacion = await OperacionesAsignacionesTurno.ObtenerAsync(_unidad, request.Id);
        if (request.Solicitud.FechaFin is null)
        {
            throw ValidacionTurno.Error400("La fecha de fin es obligatoria.");
        }

        DateOnly fechaFin = request.Solicitud.FechaFin.Value;
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
}
