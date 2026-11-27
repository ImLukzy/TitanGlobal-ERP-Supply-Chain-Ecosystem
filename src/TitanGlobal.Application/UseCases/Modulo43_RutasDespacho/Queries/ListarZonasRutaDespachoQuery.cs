using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Queries;

public class ListarZonasRutaDespachoQuery : IRequest<List<string>>
{
}

public class ListarZonasRutaDespachoQueryHandler : IRequestHandler<ListarZonasRutaDespachoQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarZonasRutaDespachoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ListarZonasRutaDespachoQuery request, CancellationToken cancellationToken)
    {
        List<string> zonas = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().Select(r => r.Zona).Distinct().OrderBy(z => z).ToListAsync();
        return zonas;
    }
}
