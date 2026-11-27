using MediatR;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos.Commands;

public class CrearProductoCommand : IRequest<int>
{
    public required GuardarProductoRequestDto Solicitud { get; set; }
}

public class CrearProductoCommandHandler : IRequestHandler<CrearProductoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearProductoCommand request, CancellationToken cancellationToken)
    {
        ValidacionProducto.ValidarSolicitud(request.Solicitud);
        Producto producto = new Producto
        {
            Nombre = request.Solicitud.Nombre,
            Categoria = request.Solicitud.Categoria,
            Precio = request.Solicitud.Precio,
            Descripcion = request.Solicitud.Descripcion
        };

        await _unitOfWork.Repository<Producto>().AddAsync(producto);
        await _unitOfWork.CompleteAsync();

        return producto.Id;
    }
}
