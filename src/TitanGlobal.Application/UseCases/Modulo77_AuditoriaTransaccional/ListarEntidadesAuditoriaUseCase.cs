using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional;

public class ListarEntidadesAuditoriaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarEntidadesAuditoriaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ExecuteAsync()
    {
        return await _unitOfWork.Repository<RegistroAuditoria>().AsQueryable()
            .Select(registro => registro.Entidad)
            .Distinct()
            .OrderBy(entidad => entidad)
            .ToListAsync();
    }
}
