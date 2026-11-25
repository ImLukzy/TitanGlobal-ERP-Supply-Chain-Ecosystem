using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class NumerarDeclaracionAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public NumerarDeclaracionAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, NumerarDeclaracionRequestDto solicitud)
    {
        DeclaracionAduana? declaracion = await _unitOfWork.Repository<DeclaracionAduana>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (declaracion is null)
        {
            throw ValidacionDeclaracionAduana.NoExiste(id);
        }

        if (declaracion.Estado != ValidacionDeclaracionAduana.Registrada)
        {
            throw ValidacionDeclaracionAduana.NoRegistrada(id);
        }

        declaracion.Canal = ValidacionDeclaracionAduana.NormalizarCanal(solicitud.Canal);
        declaracion.FechaNumeracion = DateTime.UtcNow;
        declaracion.Estado = ValidacionDeclaracionAduana.Numerada;
        await _unitOfWork.CompleteAsync();

        return declaracion.Id;
    }
}
