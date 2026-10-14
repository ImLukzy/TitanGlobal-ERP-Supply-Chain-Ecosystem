using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo21_Productos.UseCases;

public class ResumenProductosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenProductosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenProductosDto> ExecuteAsync()
    {
        IQueryable<Producto> productos = _unitOfWork.Repository<Producto>().AsQueryable();
        int totalProductos = await productos.CountAsync();
        int productosSinDescripcion = await productos.CountAsync(producto => string.IsNullOrEmpty(producto.Descripcion));
        bool hayProductos = await productos.AnyAsync();
        decimal precioPromedio = 0m;
        if (hayProductos)
        {
            precioPromedio = await productos.AverageAsync(producto => producto.Precio);
        }

        List<CategoriaResumenDto> porCategoria = await productos
            .GroupBy(producto => producto.Categoria)
            .Select(grupo => new CategoriaResumenDto
            {
                Categoria = grupo.Key,
                TotalProductos = grupo.Count(),
                PrecioPromedio = grupo.Average(producto => producto.Precio),
                PrecioMaximo = grupo.Max(producto => producto.Precio)
            })
            .OrderBy(categoria => categoria.Categoria)
            .ToListAsync();
        // PATRÓN LINQ 9: conteo agrupado por categoría.

        List<ProductoVentaDto> masVendidos = await productos
            .Select(producto => new ProductoVentaDto
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                UnidadesVendidas = producto.DetallesOrdenCompra.Sum(detalle => (int?)detalle.Cantidad) ?? 0,
                MontoVendido = producto.DetallesOrdenCompra.Sum(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioUnitario)) ?? 0m
            })
            .Where(producto => producto.UnidadesVendidas > 0)
            .OrderByDescending(producto => producto.UnidadesVendidas)
            .ThenByDescending(producto => producto.MontoVendido)
            .ThenBy(producto => producto.ProductoId)
            .Take(5)
            .ToListAsync();

        return new ResumenProductosDto
        {
            TotalProductos = totalProductos,
            ProductosSinDescripcion = productosSinDescripcion,
            PrecioPromedio = precioPromedio,
            PorCategoria = porCategoria,
            MasVendidos = masVendidos
        };
    }
}
