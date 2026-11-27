using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos.Commands;

public class EliminarProductoCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarProductoCommandHandler : IRequestHandler<EliminarProductoCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarProductoCommand request, CancellationToken cancellationToken)
    {
        Producto? producto = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (producto is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El producto {request.Id} no existe.");
        }

        bool tieneVentas = await _unitOfWork.Repository<DetalleOrdenCompra>()
            .AsQueryable()
            .AnyAsync(detalle => detalle.ProductoId == request.Id);
        if (tieneVentas)
        {
            throw CrearConflictoVentasException(request.Id);
        }

        _unitOfWork.Repository<Producto>().Remove(producto);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw CrearConflictoVentasException(request.Id);
        }
    }

    private static ReglaNegocioException CrearConflictoVentasException(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"No se puede eliminar el producto {id} porque tiene ventas registradas.");
    }
}
