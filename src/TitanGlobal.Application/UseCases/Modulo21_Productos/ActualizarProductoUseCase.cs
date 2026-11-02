using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos;

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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El producto {id} no existe.");
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
