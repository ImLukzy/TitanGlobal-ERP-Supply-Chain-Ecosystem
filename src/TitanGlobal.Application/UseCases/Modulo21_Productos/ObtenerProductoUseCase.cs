using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos;

public class ObtenerProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductoDetalleDto> ExecuteAsync(int id)
    {
        ProductoDetalleDto? producto = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new ProductoDetalleDto
            {
                Id = entidad.Id,
                Nombre = entidad.Nombre,
                Categoria = entidad.Categoria,
                Precio = entidad.Precio,
                Descripcion = entidad.Descripcion,
                UnidadesVendidas = entidad.DetallesOrdenCompra.Sum(detalle => (int?)detalle.Cantidad) ?? 0,
                MontoVendido = entidad.DetallesOrdenCompra.Sum(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioUnitario)) ?? 0m,
                ClientesDistintos = entidad.DetallesOrdenCompra
                    .Select(detalle => detalle.OrdenCompra.ClienteId)
                    .Distinct()
                    .Count()
            })
            .SingleOrDefaultAsync();

        if (producto is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El producto {id} no existe.");
        }

        return producto;
    }
}
