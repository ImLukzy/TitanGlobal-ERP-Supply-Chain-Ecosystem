using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

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
