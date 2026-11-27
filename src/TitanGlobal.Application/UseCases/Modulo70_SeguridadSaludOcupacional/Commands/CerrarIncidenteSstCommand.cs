using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class CerrarIncidenteSstCommand : IRequest<IncidenteSstIdDto>
{
    public int Id { get; set; }
}

public class CerrarIncidenteSstCommandHandler : IRequestHandler<CerrarIncidenteSstCommand, IncidenteSstIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CerrarIncidenteSstCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<IncidenteSstIdDto> Handle(CerrarIncidenteSstCommand request, CancellationToken cancellationToken)
    {
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, request.Id);
        OperacionesIncidentesSst.ExigirAbierto(incidente);
        if (incidente.AccionesCorrectivasSst.Count == 0)
        {
            throw ValidacionSst.Error409($"El incidente {incidente.Numero} necesita al menos una acción correctiva para cerrarse.");
        }

        int pendientes = incidente.AccionesCorrectivasSst.Count(accion => !accion.Completada);
        if (pendientes > 0)
        {
            throw ValidacionSst.Error409($"El incidente {incidente.Numero} tiene {pendientes} acciones correctivas sin completar.");
        }

        incidente.Estado = ValidacionSst.EstadoCerrado;
        incidente.FechaCierre = ValidacionSst.Hoy();
        await _unidad.CompleteAsync();
        return new IncidenteSstIdDto { Id = incidente.Id };
    }
}
