using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class EliminarAgenteAduaneroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarAgenteAduaneroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        AgenteAduanero? agente = await _unitOfWork.Repository<AgenteAduanero>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (agente is null)
        {
            throw ValidacionAgenteAduanero.NoExiste(id);
        }

        bool tieneDeclaraciones = await _unitOfWork.Repository<DeclaracionAduana>()
            .AsQueryable()
            .AnyAsync(declaracion => declaracion.AgenteAduaneroId == id);
        if (tieneDeclaraciones)
        {
            throw ValidacionAgenteAduanero.Conflicto(
                $"El agente aduanero {id} tiene declaraciones y no se puede eliminar; desactívelo.");
        }

        _unitOfWork.Repository<AgenteAduanero>().Remove(agente);
        await _unitOfWork.CompleteAsync();
    }
}
