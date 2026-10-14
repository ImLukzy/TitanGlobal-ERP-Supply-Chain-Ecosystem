using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.UseCases;

public class CrearOrdenCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearOrdenCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearOrdenCompraRequestDto request)
    {
        OrdenCompra order = new OrdenCompra
        {
            NumeroOrden = request.NumeroOrden,
            Fecha = DateTime.UtcNow,
            Total = request.Total,
            Descripcion = request.Descripcion,
            ClienteId = request.ClienteId
        };

        await _unitOfWork.Repository<OrdenCompra>().AddAsync(order);
        await _unitOfWork.CompleteAsync();

        return order.Id;
    }
}
