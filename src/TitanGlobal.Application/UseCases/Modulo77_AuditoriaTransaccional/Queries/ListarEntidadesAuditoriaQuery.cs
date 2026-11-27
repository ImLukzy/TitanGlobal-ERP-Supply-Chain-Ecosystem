using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional.Queries;

public class ListarEntidadesAuditoriaQuery : IRequest<List<string>>
{
}

public class ListarEntidadesAuditoriaQueryHandler : IRequestHandler<ListarEntidadesAuditoriaQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarEntidadesAuditoriaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ListarEntidadesAuditoriaQuery request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<RegistroAuditoria>().AsQueryable()
            .Select(registro => registro.Entidad)
            .Distinct()
            .OrderBy(entidad => entidad)
            .ToListAsync();
    }
}
