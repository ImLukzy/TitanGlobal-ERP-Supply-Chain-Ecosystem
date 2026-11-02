using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje;

public class EliminarZonaAlmacenajeUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarZonaAlmacenajeUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        ZonaAlmacenaje? zona = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (zona is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La zona {id} no existe.");
        }

        _unitOfWork.Repository<ZonaAlmacenaje>().Remove(zona);
        await _unitOfWork.CompleteAsync();
    }
}
