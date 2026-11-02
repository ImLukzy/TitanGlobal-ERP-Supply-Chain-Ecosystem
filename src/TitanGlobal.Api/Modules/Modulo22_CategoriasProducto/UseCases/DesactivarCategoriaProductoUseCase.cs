using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo22_CategoriasProducto.UseCases;

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
