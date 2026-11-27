using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class AgregarAccionCommand : IRequest<AccionCorrectivaSstIdDto>
{
    public int Id { get; set; }

    public required GuardarAccionCorrectivaRequestDto Solicitud { get; set; }
}

public class AgregarAccionCommandHandler : IRequestHandler<AgregarAccionCommand, AccionCorrectivaSstIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AgregarAccionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AccionCorrectivaSstIdDto> Handle(AgregarAccionCommand request, CancellationToken cancellationToken)
    {
        AccionCorrectivaSst accion = ValidacionSst.ValidarAccion(request.Solicitud);
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, request.Id);
        OperacionesIncidentesSst.ExigirAbierto(incidente);
        accion.IncidenteSstId = incidente.Id;
        await _unidad.Repository<AccionCorrectivaSst>().AddAsync(accion);
        await _unidad.CompleteAsync();
        return new AccionCorrectivaSstIdDto { Id = accion.Id };
    }
}
