using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class BuscarProductosPorFiltroQuery : IRequest<List<ProductoResumenDto>>
{
    public required string Termino { get; set; }

    public decimal MinPrecio { get; set; }
}

public class BuscarProductosPorFiltroQueryHandler : IRequestHandler<BuscarProductosPorFiltroQuery, List<ProductoResumenDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public BuscarProductosPorFiltroQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductoResumenDto>> Handle(BuscarProductosPorFiltroQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 1: filtro de texto insensible a mayúsculas.
        // PATRÓN LINQ 2: filtro numérico por umbral.
        // PATRÓN LINQ 3: proyección directa a DTO.
        string terminoNormalizado = request.Termino.ToLower();

        return await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => producto.Nombre.ToLower().Contains(terminoNormalizado))
            .Where(producto => producto.Precio > request.MinPrecio)
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
