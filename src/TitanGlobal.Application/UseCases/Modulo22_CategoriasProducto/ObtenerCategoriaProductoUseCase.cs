using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

public class ObtenerCategoriaProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCategoriaProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoriaProductoDto> ExecuteAsync(int id)
    {
        CategoriaProductoDto? resultado = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .Where(categoria => categoria.Id == id)
            .Select(categoria => new CategoriaProductoDto
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activa = categoria.Activa
            })
            .SingleOrDefaultAsync();

        if (resultado is null)
        {
            throw ValidacionCategoriaProducto.CrearNoEncontrada(id);
        }

        return resultado;
    }
}
