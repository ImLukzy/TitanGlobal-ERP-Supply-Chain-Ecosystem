using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos;

public class ListarCategoriasProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCategoriasProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ExecuteAsync()
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
