using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.UseCases;

public class ListarCiudadesAlmacenUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCiudadesAlmacenUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ExecuteAsync()
    {
        List<string> ciudades = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .Select(almacen => almacen.Ciudad)
            .Distinct()
            .OrderBy(ciudad => ciudad)
            .ToListAsync();
        // PATRÓN LINQ 11: valores únicos ordenados.

        return ciudades;
    }
}
