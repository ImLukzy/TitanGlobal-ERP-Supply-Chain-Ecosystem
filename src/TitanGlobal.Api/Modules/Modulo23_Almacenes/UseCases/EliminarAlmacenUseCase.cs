using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.UseCases;

public class EliminarAlmacenUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarAlmacenUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        Almacen? almacen = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (almacen is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El almacén {id} no existe.");
        }

        _unitOfWork.Repository<Almacen>().Remove(almacen);
        await _unitOfWork.CompleteAsync();
    }
}
