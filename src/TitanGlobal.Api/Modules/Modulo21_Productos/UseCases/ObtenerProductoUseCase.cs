using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo21_Productos.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El producto {id} no existe.");
        }

        return producto;
    }
}
