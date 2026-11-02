using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo22_CategoriasProducto.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo22_CategoriasProducto.UseCases;

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
