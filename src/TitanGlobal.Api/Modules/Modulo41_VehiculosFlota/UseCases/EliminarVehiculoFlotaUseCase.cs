using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

public class EliminarVehiculoFlotaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarVehiculoFlotaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        VehiculoFlota? vehiculo = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehiculo is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El vehículo {id} no existe.");
        }

        _unitOfWork.Repository<VehiculoFlota>().Remove(vehiculo);
        await _unitOfWork.CompleteAsync();
    }
}
