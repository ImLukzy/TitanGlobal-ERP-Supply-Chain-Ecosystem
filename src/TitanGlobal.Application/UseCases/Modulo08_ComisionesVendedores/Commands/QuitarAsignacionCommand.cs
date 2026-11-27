using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;

public class QuitarAsignacionCommand : IRequest
{
    public int OrdenCompraId { get; set; }
}

public class QuitarAsignacionCommandHandler : IRequestHandler<QuitarAsignacionCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public QuitarAsignacionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(QuitarAsignacionCommand request, CancellationToken cancellationToken)
    {
        OrdenCompra? orden = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.OrdenCompraId);
        if (orden is null)
        {
            throw new ReglaNegocioException(404, $"La orden de compra {request.OrdenCompraId} no existe.");
        }

        await OperacionesComisionesVendedores.ValidarMesNoLiquidadoAsync(_unitOfWork, orden.Fecha);
        AsignacionVenta? asignacion = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.OrdenCompraId == request.OrdenCompraId);
        if (asignacion is null)
        {
            throw new ReglaNegocioException(404, $"La orden {orden.NumeroOrden} no tiene vendedor.");
        }

        _unitOfWork.Repository<AsignacionVenta>().Remove(asignacion);
        await _unitOfWork.CompleteAsync();
    }
}
