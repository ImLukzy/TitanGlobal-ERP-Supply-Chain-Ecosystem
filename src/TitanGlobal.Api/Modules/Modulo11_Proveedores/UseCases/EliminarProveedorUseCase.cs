using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo11_Proveedores.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El proveedor {id} no existe.");
        }

        _unitOfWork.Repository<Proveedor>().Remove(proveedor);
        await _unitOfWork.CompleteAsync();
    }
}
