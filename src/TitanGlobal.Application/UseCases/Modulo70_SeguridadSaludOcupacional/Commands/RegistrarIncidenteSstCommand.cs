using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class RegistrarIncidenteSstCommand : IRequest<IncidenteSstCreadoDto>
{
    public required RegistrarIncidenteSstRequestDto Solicitud { get; set; }
}

public class RegistrarIncidenteSstCommandHandler : IRequestHandler<RegistrarIncidenteSstCommand, IncidenteSstCreadoDto>
{
    private readonly IUnitOfWork _unidad;

    public RegistrarIncidenteSstCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<IncidenteSstCreadoDto> Handle(RegistrarIncidenteSstCommand request, CancellationToken cancellationToken)
    {
        IncidenteSst incidente = ValidacionSst.ValidarIncidente(request.Solicitud);
        await ValidacionSst.ObtenerEmpleadoAsync(_unidad, incidente.EmpleadoId);
        incidente.Numero = ValidacionSst.NuevoNumeroTemporal();
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await _unidad.Repository<IncidenteSst>().AddAsync(incidente);
        await _unidad.CompleteAsync();
        incidente.Numero = ValidacionSst.NumeroDefinitivo(incidente.Id);
        _unidad.Repository<IncidenteSst>().Update(incidente);
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new IncidenteSstCreadoDto { Id = incidente.Id, Numero = incidente.Numero };
    }
}
