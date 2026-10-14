using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo21_Productos.UseCases;

public class ActualizarProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarProductoRequestDto solicitud)
    {
        Producto? producto = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (producto is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El producto {id} no existe.");
        }

        ValidacionProducto.ValidarSolicitud(solicitud);
        producto.Nombre = solicitud.Nombre;
        producto.Categoria = solicitud.Categoria;
        producto.Precio = solicitud.Precio;
        producto.Descripcion = solicitud.Descripcion;
        _unitOfWork.Repository<Producto>().Update(producto);
        await _unitOfWork.CompleteAsync();

        return producto.Id;
    }
}
