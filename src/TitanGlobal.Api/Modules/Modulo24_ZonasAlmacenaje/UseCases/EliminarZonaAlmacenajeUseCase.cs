using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"La zona {id} no existe.");
        }

        _unitOfWork.Repository<ZonaAlmacenaje>().Remove(zona);
        await _unitOfWork.CompleteAsync();
    }
}
