using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos.Commands;

public class ActualizarProductoCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarProductoRequestDto Solicitud { get; set; }
}

public class ActualizarProductoCommandHandler : IRequestHandler<ActualizarProductoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarProductoCommand request, CancellationToken cancellationToken)
    {
        Producto? producto = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (producto is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El producto {request.Id} no existe.");
        }

        ValidacionProducto.ValidarSolicitud(request.Solicitud);
        producto.Nombre = request.Solicitud.Nombre;
        producto.Categoria = request.Solicitud.Categoria;
        producto.Precio = request.Solicitud.Precio;
        producto.Descripcion = request.Solicitud.Descripcion;
        _unitOfWork.Repository<Producto>().Update(producto);
        await _unitOfWork.CompleteAsync();

        return producto.Id;
    }
}
