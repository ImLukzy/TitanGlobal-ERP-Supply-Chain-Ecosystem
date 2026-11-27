using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos.Queries;

public class ListarCategoriasProductoQuery : IRequest<List<string>>
{
}

public class ListarCategoriasProductoQueryHandler : IRequestHandler<ListarCategoriasProductoQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCategoriasProductoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ListarCategoriasProductoQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 11: categorías únicas mediante Distinct.
        return await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Select(producto => producto.Categoria)
            .Distinct()
            .OrderBy(categoria => categoria)
            .ToListAsync();
    }
}
