using MediatR;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Commands;

public class RechazarAusenciaCommand : IRequest<AusenciaIdDto>
{
    public int Id { get; set; }

    public required RechazarAusenciaRequestDto Solicitud { get; set; }

    public required string Usuario { get; set; }
}

public class RechazarAusenciaCommandHandler : IRequestHandler<RechazarAusenciaCommand, AusenciaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RechazarAusenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AusenciaIdDto> Handle(RechazarAusenciaCommand request, CancellationToken cancellationToken)
    {
        Ausencia ausencia = await ValidacionAusencia.ObtenerAsync(_unidad, request.Id);
        if (ausencia.Estado != ValidacionAusencia.EstadoPendiente)
        {
            throw ValidacionAusencia.NoPendiente(ausencia.Numero);
        }

        string motivo = ValidacionAusencia.ValidarMotivoRechazo(request.Solicitud.Motivo);
        ausencia.Estado = ValidacionAusencia.EstadoRechazada;
        ausencia.MotivoRechazo = motivo;
        OperacionesAusencias.Decidir(ausencia, request.Usuario);
        await _unidad.CompleteAsync();
        return new AusenciaIdDto { Id = ausencia.Id };
    }
}
