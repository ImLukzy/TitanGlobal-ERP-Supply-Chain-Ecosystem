using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

public class DesactivarCategoriaProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public DesactivarCategoriaProductoUseCase(IUnitOfWork unitOfWork)
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

        categoria.Activa = false;
        await _unitOfWork.CompleteAsync();
    }
}
