using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class AnularDeclaracionAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AnularDeclaracionAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, AnularDeclaracionRequestDto solicitud)
    {
        DeclaracionAduana? declaracion = await _unitOfWork.Repository<DeclaracionAduana>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (declaracion is null)
        {
            throw ValidacionDeclaracionAduana.NoExiste(id);
        }

        string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionDeclaracionAduana.Error("El motivo de anulación es obligatorio.");
        }

        if (motivo.Length > ValidacionDeclaracionAduana.MaximoMotivo)
        {
            throw ValidacionDeclaracionAduana.Error(
                $"El motivo no puede superar {ValidacionDeclaracionAduana.MaximoMotivo} caracteres.");
        }

        if (declaracion.Estado == ValidacionDeclaracionAduana.Levantada
            || declaracion.Estado == ValidacionDeclaracionAduana.Anulada)
        {
            throw ValidacionDeclaracionAduana.Conflicto(
                $"La declaración {id} no se puede anular porque está {declaracion.Estado}.");
        }

        declaracion.MotivoAnulacion = motivo;
        declaracion.Estado = ValidacionDeclaracionAduana.Anulada;
        await _unitOfWork.CompleteAsync();

        return declaracion.Id;
    }
}
