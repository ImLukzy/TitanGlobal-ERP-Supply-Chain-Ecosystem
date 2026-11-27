using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Queries;

public class ListarPlantasCentroTrabajoQuery : IRequest<List<string>>
{
}

public class ListarPlantasCentroTrabajoQueryHandler : IRequestHandler<ListarPlantasCentroTrabajoQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarPlantasCentroTrabajoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ListarPlantasCentroTrabajoQuery request, CancellationToken cancellationToken)
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
