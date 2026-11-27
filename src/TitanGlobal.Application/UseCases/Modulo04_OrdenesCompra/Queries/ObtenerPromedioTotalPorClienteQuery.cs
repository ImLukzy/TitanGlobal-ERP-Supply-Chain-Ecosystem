using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerPromedioTotalPorClienteQuery : IRequest<decimal>
{
    public int ClienteId { get; set; }
}

public class ObtenerPromedioTotalPorClienteQueryHandler : IRequestHandler<ObtenerPromedioTotalPorClienteQuery, decimal>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerPromedioTotalPorClienteQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> Handle(ObtenerPromedioTotalPorClienteQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 7: promedio con validación previa.
        IQueryable<OrdenCompra> orders = _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.ClienteId == request.ClienteId);

        bool hasOrders = await orders.AnyAsync();
        if (!hasOrders)
        {
            return 0;
        }

        return await orders.AverageAsync(orden => orden.Total);
    }
}
