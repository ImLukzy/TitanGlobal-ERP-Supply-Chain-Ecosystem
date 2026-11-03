using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

public class ListarCategoriasProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCategoriasProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CategoriaProductoDto>> ExecuteAsync(string? buscar, bool incluirInactivas)
    {
        IQueryable<CategoriaProducto> consulta = _unitOfWork.Repository<CategoriaProducto>().AsQueryable();

        if (!incluirInactivas)
        {
            consulta = consulta.Where(categoria => categoria.Activa);
        }

        string texto = buscar?.Trim().ToLowerInvariant() ?? string.Empty;
        if (texto.Length > 0)
        {
            consulta = consulta.Where(categoria => categoria.Nombre.ToLower().Contains(texto));
        }

        List<CategoriaProductoDto> resultado = await consulta
            .OrderBy(categoria => categoria.Nombre)
            .Select(categoria => new CategoriaProductoDto
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activa = categoria.Activa
            })
            .ToListAsync();

        return resultado;
    }
}
