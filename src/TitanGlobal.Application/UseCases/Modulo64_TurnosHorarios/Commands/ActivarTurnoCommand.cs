using MediatR;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;

public class ActivarTurnoCommand : IRequest<TurnoIdDto>
{
    public int Id { get; set; }
}

public class ActivarTurnoCommandHandler : IRequestHandler<ActivarTurnoCommand, TurnoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActivarTurnoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<TurnoIdDto> Handle(ActivarTurnoCommand request, CancellationToken cancellationToken)
    {
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, request.Id);
        if (turno.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El turno {turno.Codigo} ya está activo.");
        }

        turno.Activo = true;
        await _unidad.CompleteAsync();
        return new TurnoIdDto { Id = turno.Id };
    }
}
