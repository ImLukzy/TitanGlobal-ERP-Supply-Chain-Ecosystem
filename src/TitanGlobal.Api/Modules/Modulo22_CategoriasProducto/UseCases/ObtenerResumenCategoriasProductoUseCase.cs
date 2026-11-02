using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo22_CategoriasProducto.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo22_CategoriasProducto.UseCases;

public class ObtenerResumenCategoriasProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerResumenCategoriasProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenCategoriasProductoDto> ExecuteAsync()
    {
        IQueryable<CategoriaProducto> consulta = _unitOfWork.Repository<CategoriaProducto>().AsQueryable();

        int total = await consulta.CountAsync();
        int activas = await consulta.CountAsync(categoria => categoria.Activa);

        ResumenCategoriasProductoDto resultado = new ResumenCategoriasProductoDto
        {
            TotalCategorias = total,
            CategoriasActivas = activas,
            CategoriasInactivas = total - activas
        };

        return resultado;
    }
}
