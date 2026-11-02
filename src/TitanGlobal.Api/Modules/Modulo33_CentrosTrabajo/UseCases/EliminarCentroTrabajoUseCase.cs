using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.UseCases;

public class EliminarCentroTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarCentroTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        CentroTrabajo? centro = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (centro is null)
        {
            throw PersistenciaCentroTrabajo.CrearNoEncontrado(id);
        }

        _unitOfWork.Repository<CentroTrabajo>().Remove(centro);
        await _unitOfWork.CompleteAsync();
    }
}
