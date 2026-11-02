using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes;

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
