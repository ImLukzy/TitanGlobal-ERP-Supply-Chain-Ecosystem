using MediatR;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Commands;

public class CancelarConsolidacionCommand : IRequest<int>
{
    public int Id { get; set; }

    public required CancelarConsolidacionRequestDto Solicitud { get; set; }
}

public class CancelarConsolidacionCommandHandler : IRequestHandler<CancelarConsolidacionCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CancelarConsolidacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CancelarConsolidacionCommand request, CancellationToken cancellationToken)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, request.Id);
        if (carga.Estado == "Cancelada") { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "La consolidación ya está cancelada."); }
        string motivo = ValidacionCarga.Texto(request.Solicitud.Motivo, 500, "El motivo de cancelación es obligatorio.", "El motivo de cancelación no puede superar 500 caracteres.");
        carga.Estado = "Cancelada"; carga.MotivoCancelacion = motivo; carga.FechaCancelacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidad.Repository<ConsolidacionCarga>().Update(carga); await _unidad.CompleteAsync(); return request.Id;
    }
}
