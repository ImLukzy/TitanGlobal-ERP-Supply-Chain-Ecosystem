using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes;

public class EliminarClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        Cliente? cliente = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (cliente is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El cliente {id} no existe.");
        }

        bool tieneOrdenes = await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .AnyAsync(orden => orden.ClienteId == id);
        if (tieneOrdenes)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"No se puede eliminar el cliente {id} porque tiene órdenes de compra.");
        }

        _unitOfWork.Repository<Cliente>().Remove(cliente);
        await _unitOfWork.CompleteAsync();
    }
}
