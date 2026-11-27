using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;

public class EliminarVendedorCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarVendedorCommandHandler : IRequestHandler<EliminarVendedorCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarVendedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarVendedorCommand request, CancellationToken cancellationToken)
    {
        Vendedor vendedor = await OperacionesComisionesVendedores.ObtenerVendedorEntidadAsync(_unitOfWork, request.Id);
        bool tieneAsignaciones = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .AnyAsync(asignacion => asignacion.VendedorId == request.Id);
        bool tieneLiquidaciones = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .AnyAsync(liquidacion => liquidacion.VendedorId == request.Id);
        if (tieneAsignaciones || tieneLiquidaciones)
        {
            throw new ReglaNegocioException(
                409,
                $"No se puede eliminar el vendedor {vendedor.Codigo} porque tiene ventas asignadas.");
        }

        _unitOfWork.Repository<Vendedor>().Remove(vendedor);
        await _unitOfWork.CompleteAsync();
    }
}
