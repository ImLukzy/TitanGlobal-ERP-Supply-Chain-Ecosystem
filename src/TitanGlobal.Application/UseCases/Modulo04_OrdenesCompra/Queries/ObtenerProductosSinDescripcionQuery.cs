using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerProductosSinDescripcionQuery : IRequest<List<ProductoResumenDto>>
{
}

public class ObtenerProductosSinDescripcionQueryHandler : IRequestHandler<ObtenerProductosSinDescripcionQuery, List<ProductoResumenDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerProductosSinDescripcionQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductoResumenDto>> Handle(ObtenerProductosSinDescripcionQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 8: campos nulos o vacíos.
        return await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => string.IsNullOrEmpty(producto.Descripcion))
            .OrderBy(producto => producto.Id)
            .Select(producto => new ProductoResumenDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Precio = producto.Precio
            })
            .ToListAsync();
    }
}
