using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo21_Productos.UseCases;

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
