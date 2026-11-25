using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class DesactivarAgenteAduaneroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public DesactivarAgenteAduaneroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        AgenteAduanero? agente = await _unitOfWork.Repository<AgenteAduanero>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (agente is null)
        {
            throw ValidacionAgenteAduanero.NoExiste(id);
        }

        if (!agente.Activo)
        {
            throw ValidacionAgenteAduanero.Conflicto($"El agente aduanero {id} ya está inactivo.");
        }

        agente.Activo = false;
        await _unitOfWork.CompleteAsync();

        return agente.Id;
    }
}
