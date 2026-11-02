using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes;

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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El almacén {id} no existe.");
        }

        bool tieneZonas = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .AnyAsync(zona => zona.AlmacenId == id);
        if (tieneZonas)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"No se puede eliminar el almacén {id} porque tiene zonas.");
        }

        _unitOfWork.Repository<Almacen>().Remove(almacen);
        await _unitOfWork.CompleteAsync();
    }
}
