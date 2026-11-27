using MediatR;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;

public class EliminarAsignacionTurnoCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarAsignacionTurnoCommandHandler : IRequestHandler<EliminarAsignacionTurnoCommand>
{
    private readonly IUnitOfWork _unidad;

    public EliminarAsignacionTurnoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task Handle(EliminarAsignacionTurnoCommand request, CancellationToken cancellationToken)
    {
        AsignacionTurno asignacion = await OperacionesAsignacionesTurno.ObtenerAsync(_unidad, request.Id);
        if (asignacion.FechaInicio <= ValidacionTurno.Hoy())
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Solo se puede eliminar una asignación que todavía no empezó.");
        }

        _unidad.Repository<AsignacionTurno>().Remove(asignacion);
        await _unidad.CompleteAsync();
    }
}
