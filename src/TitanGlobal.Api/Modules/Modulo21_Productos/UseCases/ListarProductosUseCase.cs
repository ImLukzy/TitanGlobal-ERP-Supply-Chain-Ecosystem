using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo21_Productos.UseCases;

public class ListarProductosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarProductosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaProductosDto> ExecuteAsync(
        string? busqueda,
        string? categoria,
        decimal? precioMin,
        decimal? precioMax,
        int pagina,
        int tamanoPagina)
    {
        ValidacionProducto.ValidarFiltros(pagina, tamanoPagina, precioMin, precioMax);
        IQueryable<Producto> consulta = _unitOfWork.Repository<Producto>().AsQueryable();

        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda de texto sin distinguir mayúsculas.
            consulta = consulta.Where(producto => producto.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        string categoriaNormalizada = categoria?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(categoriaNormalizada))
        {
            consulta = consulta.Where(producto => producto.Categoria.ToLower() == categoriaNormalizada);
        }

        if (precioMin.HasValue)
        {
            // PATRÓN LINQ 2: filtro numérico por umbral.
            consulta = consulta.Where(producto => producto.Precio >= precioMin.Value);
        }

        if (precioMax.HasValue)
        {
            consulta = consulta.Where(producto => producto.Precio <= precioMax.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ProductoListadoDto> items = await consulta
            .OrderBy(producto => producto.Nombre)
            .ThenBy(producto => producto.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(producto => new ProductoListadoDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Categoria = producto.Categoria,
                Precio = producto.Precio,
                TieneDescripcion = !string.IsNullOrEmpty(producto.Descripcion)
            })
            .ToListAsync();

        return new PaginaProductosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
