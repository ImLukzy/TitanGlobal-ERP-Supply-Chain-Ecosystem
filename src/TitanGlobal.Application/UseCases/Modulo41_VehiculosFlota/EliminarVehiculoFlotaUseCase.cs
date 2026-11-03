using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota;

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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El vehículo {id} no existe.");
        }

        _unitOfWork.Repository<VehiculoFlota>().Remove(vehiculo);
        await _unitOfWork.CompleteAsync();
    }
}
