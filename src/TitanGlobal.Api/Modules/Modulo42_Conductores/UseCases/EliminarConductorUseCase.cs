using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo42_Conductores.UseCases;

public class EliminarConductorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarConductorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        Conductor? conductor = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .FirstOrDefaultAsync(conductor => conductor.Id == id);

        if (conductor is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El conductor {id} no existe.");
        }

        _unitOfWork.Repository<Conductor>().Remove(conductor);
        await _unitOfWork.CompleteAsync();
    }
}
