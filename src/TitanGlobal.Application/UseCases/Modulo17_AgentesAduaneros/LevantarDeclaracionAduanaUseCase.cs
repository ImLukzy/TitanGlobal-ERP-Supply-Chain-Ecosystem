using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class LevantarDeclaracionAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public LevantarDeclaracionAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        DeclaracionAduana? declaracion = await _unitOfWork.Repository<DeclaracionAduana>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (declaracion is null)
        {
            throw ValidacionDeclaracionAduana.NoExiste(id);
        }

        if (declaracion.Estado != ValidacionDeclaracionAduana.Numerada)
        {
            throw ValidacionDeclaracionAduana.Conflicto($"La declaración {id} no está numerada.");
        }

        declaracion.FechaLevante = DateTime.UtcNow;
        declaracion.Estado = ValidacionDeclaracionAduana.Levantada;
        await _unitOfWork.CompleteAsync();

        return declaracion.Id;
    }
}
