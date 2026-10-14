using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo21_Productos.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El producto {id} no existe.");
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
        catch (DbUpdateException excepcion) when (excepcion.InnerException is PostgresException excepcionPostgres
            && excepcionPostgres.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw CrearConflictoVentasException(id);
        }
    }

    private static ReglaNegocioException CrearConflictoVentasException(int id)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"No se puede eliminar el producto {id} porque tiene ventas registradas.");
    }
}
