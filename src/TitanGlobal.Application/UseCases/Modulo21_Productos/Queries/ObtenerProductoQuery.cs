using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos.Queries;

public class ObtenerProductoQuery : IRequest<ProductoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerProductoQueryHandler : IRequestHandler<ObtenerProductoQuery, ProductoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerProductoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductoDetalleDto> Handle(ObtenerProductoQuery request, CancellationToken cancellationToken)
    {
        ProductoDetalleDto? producto = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.Id)
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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El producto {request.Id} no existe.");
        }

        return producto;
    }
}
