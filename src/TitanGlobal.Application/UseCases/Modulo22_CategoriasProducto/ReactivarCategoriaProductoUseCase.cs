using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

public class ReactivarCategoriaProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReactivarCategoriaProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        CategoriaProducto? categoria = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == id);
        if (categoria is null)
        {
            throw ValidacionCategoriaProducto.CrearNoEncontrada(id);
        }

        if (categoria.Activa)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La categoría {id} ya está activa.");
        }

        categoria.Activa = true;
        await _unitOfWork.CompleteAsync();
    }
}
