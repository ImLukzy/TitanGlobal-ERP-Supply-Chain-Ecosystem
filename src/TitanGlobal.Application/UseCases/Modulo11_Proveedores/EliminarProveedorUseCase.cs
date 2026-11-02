using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

public class EliminarProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        Proveedor? proveedor = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (proveedor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El proveedor {id} no existe.");
        }

        _unitOfWork.Repository<Proveedor>().Remove(proveedor);
        await _unitOfWork.CompleteAsync();
    }
}
