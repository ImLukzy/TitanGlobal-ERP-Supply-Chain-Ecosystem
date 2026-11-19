using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class CancelarLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelarLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        Licitacion? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(id);
        }

        if (licitacion.Estado == ValidacionLicitacion.Adjudicada || licitacion.Estado == ValidacionLicitacion.Cancelada)
        {
            throw ValidacionLicitacion.Conflicto(
                $"La licitación {id} no se puede cancelar porque está {licitacion.Estado}.");
        }

        licitacion.Estado = ValidacionLicitacion.Cancelada;
        await _unitOfWork.CompleteAsync();

        return licitacion.Id;
    }
}
