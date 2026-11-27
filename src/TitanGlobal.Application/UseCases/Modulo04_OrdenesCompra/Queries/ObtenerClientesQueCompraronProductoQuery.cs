using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerClientesQueCompraronProductoQuery : IRequest<List<string>>
{
    public int ProductoId { get; set; }
}

public class ObtenerClientesQueCompraronProductoQueryHandler : IRequestHandler<ObtenerClientesQueCompraronProductoQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerClientesQueCompraronProductoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ObtenerClientesQueCompraronProductoQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 12: navegación inversa y valores únicos.
        return await _unitOfWork.Repository<DetalleOrdenCompra>()
            .AsQueryable()
            .Where(detalle => detalle.ProductoId == request.ProductoId)
            .Select(detalle => detalle.OrdenCompra.Cliente.Nombre)
            .Distinct()
            .OrderBy(nombre => nombre)
            .ToListAsync();
    }
}
