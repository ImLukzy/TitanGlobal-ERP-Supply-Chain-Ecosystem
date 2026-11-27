using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerProductoMasCaroQuery : IRequest<ProductoResumenDto?>
{
}

public class ObtenerProductoMasCaroQueryHandler : IRequestHandler<ObtenerProductoMasCaroQuery, ProductoResumenDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerProductoMasCaroQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductoResumenDto?> Handle(ObtenerProductoMasCaroQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 5: orden descendente y primero.
        return await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .OrderByDescending(producto => producto.Precio)
            .ThenBy(producto => producto.Id)
            .Select(producto => new ProductoResumenDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Precio = producto.Precio
            })
            .FirstOrDefaultAsync();
    }
}
