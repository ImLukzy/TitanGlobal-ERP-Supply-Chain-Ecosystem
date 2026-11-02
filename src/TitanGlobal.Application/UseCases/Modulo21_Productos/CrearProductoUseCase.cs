using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos;

public class CrearProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarProductoRequestDto solicitud)
    {
        ValidacionProducto.ValidarSolicitud(solicitud);
        Producto producto = new Producto
        {
            Nombre = solicitud.Nombre,
            Categoria = solicitud.Categoria,
            Precio = solicitud.Precio,
            Descripcion = solicitud.Descripcion
        };

        await _unitOfWork.Repository<Producto>().AddAsync(producto);
        await _unitOfWork.CompleteAsync();

        return producto.Id;
    }
}
