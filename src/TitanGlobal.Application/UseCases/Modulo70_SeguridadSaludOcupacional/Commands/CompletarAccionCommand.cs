using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class CompletarAccionCommand : IRequest<AccionCorrectivaSstIdDto>
{
    public int Id { get; set; }

    public int AccionId { get; set; }

    public required CompletarAccionCorrectivaRequestDto Solicitud { get; set; }
}

public class CompletarAccionCommandHandler : IRequestHandler<CompletarAccionCommand, AccionCorrectivaSstIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CompletarAccionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AccionCorrectivaSstIdDto> Handle(CompletarAccionCommand request, CancellationToken cancellationToken)
    {
        DateOnly fecha = ValidacionSst.ValidarFechaNoFutura(request.Solicitud.FechaCompletada);
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, request.Id);
        OperacionesIncidentesSst.ExigirAbierto(incidente);
        AccionCorrectivaSst? accion = incidente.AccionesCorrectivasSst.SingleOrDefault(otra => otra.Id == request.AccionId);
        if (accion is null)
        {
            throw ValidacionSst.AccionInexistente(request.AccionId);
        }

        if (accion.Completada)
        {
            throw ValidacionSst.AccionYaCompletada(request.AccionId);
        }

        accion.Completada = true;
        accion.FechaCompletada = fecha;
        await _unidad.CompleteAsync();
        return new AccionCorrectivaSstIdDto { Id = accion.Id };
    }
}
