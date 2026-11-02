using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

public class ListarPaisesProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarPaisesProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ExecuteAsync()
    {
        List<string> paises = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .Select(proveedor => proveedor.Pais)
            .Distinct()
            .OrderBy(pais => pais)
            .ToListAsync();
        // PATRÓN LINQ 11: valores únicos ordenados.

        return paises;
    }
}
