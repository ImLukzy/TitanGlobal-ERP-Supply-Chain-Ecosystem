using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos;

public class EliminarProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        Producto? producto = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (producto is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El producto {id} no existe.");
        }

        bool tieneVentas = await _unitOfWork.Repository<DetalleOrdenCompra>()
            .AsQueryable()
            .AnyAsync(detalle => detalle.ProductoId == id);
        if (tieneVentas)
        {
            throw CrearConflictoVentasException(id);
        }

        _unitOfWork.Repository<Producto>().Remove(producto);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw CrearConflictoVentasException(id);
        }
    }

    private static ReglaNegocioException CrearConflictoVentasException(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"No se puede eliminar el producto {id} porque tiene ventas registradas.");
    }
}
