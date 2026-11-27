using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerCategoriasUnicasCompradasPorClienteQuery : IRequest<List<string>>
{
    public int ClienteId { get; set; }
}

public class ObtenerCategoriasUnicasCompradasPorClienteQueryHandler : IRequestHandler<ObtenerCategoriasUnicasCompradasPorClienteQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCategoriasUnicasCompradasPorClienteQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ObtenerCategoriasUnicasCompradasPorClienteQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 11: aplanado y valores únicos.
        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.ClienteId == request.ClienteId)
            .SelectMany(orden => orden.Detalles)
            .Select(detalle => detalle.Producto.Categoria)
            .Distinct()
            .OrderBy(categoria => categoria)
            .ToListAsync();
    }
}
