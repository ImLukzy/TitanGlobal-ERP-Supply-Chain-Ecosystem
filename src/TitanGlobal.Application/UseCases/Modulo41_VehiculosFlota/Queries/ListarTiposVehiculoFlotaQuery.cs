using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Queries;

public class ListarTiposVehiculoFlotaQuery : IRequest<List<string>>
{
}

public class ListarTiposVehiculoFlotaQueryHandler : IRequestHandler<ListarTiposVehiculoFlotaQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarTiposVehiculoFlotaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ListarTiposVehiculoFlotaQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 11: aplanado y únicos (Distinct)
        List<string> tipos = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .Select(v => v.Tipo)
            .Distinct()
            .OrderBy(tipo => tipo)
            .ToListAsync();

        return tipos;
    }
}
