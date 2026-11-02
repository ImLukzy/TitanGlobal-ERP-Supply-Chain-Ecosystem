using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.UseCases;

public class ListarPlantasCentroTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarPlantasCentroTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ExecuteAsync()
    {
        // PATRÓN LINQ 11: valores únicos ordenados.
        List<string> plantas = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .Select(centro => centro.Planta)
            .Distinct()
            .OrderBy(planta => planta)
            .ToListAsync();

        return plantas;
    }
}
