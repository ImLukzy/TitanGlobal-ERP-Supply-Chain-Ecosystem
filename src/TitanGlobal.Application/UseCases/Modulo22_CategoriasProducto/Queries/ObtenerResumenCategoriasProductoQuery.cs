using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Queries;

public class ObtenerResumenCategoriasProductoQuery : IRequest<ResumenCategoriasProductoDto>
{
}

public class ObtenerResumenCategoriasProductoQueryHandler : IRequestHandler<ObtenerResumenCategoriasProductoQuery, ResumenCategoriasProductoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerResumenCategoriasProductoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenCategoriasProductoDto> Handle(ObtenerResumenCategoriasProductoQuery request, CancellationToken cancellationToken)
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
