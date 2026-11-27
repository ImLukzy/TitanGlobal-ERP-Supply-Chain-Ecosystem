using MediatR;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;

public class DesactivarTurnoCommand : IRequest<TurnoIdDto>
{
    public int Id { get; set; }
}

public class DesactivarTurnoCommandHandler : IRequestHandler<DesactivarTurnoCommand, TurnoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public DesactivarTurnoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<TurnoIdDto> Handle(DesactivarTurnoCommand request, CancellationToken cancellationToken)
    {
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, request.Id);
        if (!turno.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El turno {turno.Codigo} ya está inactivo.");
        }

        turno.Activo = false;
        await _unidad.CompleteAsync();
        return new TurnoIdDto { Id = turno.Id };
    }
}
