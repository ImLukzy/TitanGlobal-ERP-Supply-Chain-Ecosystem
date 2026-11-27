using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos.Queries;

public class ListarProductosQuery : IRequest<PaginaProductosDto>
{
    public string? Busqueda { get; set; }

    public string? Categoria { get; set; }

    public decimal? PrecioMin { get; set; }

    public decimal? PrecioMax { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarProductosQueryHandler : IRequestHandler<ListarProductosQuery, PaginaProductosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarProductosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaProductosDto> Handle(ListarProductosQuery request, CancellationToken cancellationToken)
    {
        ValidacionProducto.ValidarFiltros(request.Pagina, request.TamanoPagina, request.PrecioMin, request.PrecioMax);
        IQueryable<Producto> consulta = _unitOfWork.Repository<Producto>().AsQueryable();

        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda de texto sin distinguir mayúsculas.
            consulta = consulta.Where(producto => producto.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        string categoriaNormalizada = request.Categoria?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(categoriaNormalizada))
        {
            consulta = consulta.Where(producto => producto.Categoria.ToLower() == categoriaNormalizada);
        }

        if (request.PrecioMin.HasValue)
        {
            // PATRÓN LINQ 2: filtro numérico por umbral.
            consulta = consulta.Where(producto => producto.Precio >= request.PrecioMin.Value);
        }

        if (request.PrecioMax.HasValue)
        {
            consulta = consulta.Where(producto => producto.Precio <= request.PrecioMax.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ProductoListadoDto> items = await consulta
            .OrderBy(producto => producto.Nombre)
            .ThenBy(producto => producto.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
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
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
