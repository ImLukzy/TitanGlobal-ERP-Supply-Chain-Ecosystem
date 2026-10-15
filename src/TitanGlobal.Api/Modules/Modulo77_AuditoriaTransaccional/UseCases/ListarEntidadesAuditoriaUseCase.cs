using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.UseCases;

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
