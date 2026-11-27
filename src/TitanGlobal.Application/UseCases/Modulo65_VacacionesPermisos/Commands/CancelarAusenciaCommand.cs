using MediatR;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Commands;

public class CancelarAusenciaCommand : IRequest<AusenciaIdDto>
{
    public int Id { get; set; }
}

public class CancelarAusenciaCommandHandler : IRequestHandler<CancelarAusenciaCommand, AusenciaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CancelarAusenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AusenciaIdDto> Handle(CancelarAusenciaCommand request, CancellationToken cancellationToken)
    {
        Ausencia ausencia = await ValidacionAusencia.ObtenerAsync(_unidad, request.Id);
        if (ausencia.Estado == ValidacionAusencia.EstadoRechazada || ausencia.Estado == ValidacionAusencia.EstadoCancelada)
        {
            throw ValidacionAusencia.Error409($"La ausencia {ausencia.Numero} ya está rechazada o cancelada.");
        }

        DateOnly hoy = ValidacionAusencia.Hoy();
        if (ausencia.FechaInicio <= hoy)
        {
            throw ValidacionAusencia.Error409($"La ausencia {ausencia.Numero} ya empezó y no se puede cancelar.");
        }

        ausencia.Estado = ValidacionAusencia.EstadoCancelada;
        ausencia.FechaCancelacion = hoy;
        await _unidad.CompleteAsync();
        return new AusenciaIdDto { Id = ausencia.Id };
    }
}
