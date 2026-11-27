using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Queries;

public class ListarCategoriasProductoQuery : IRequest<List<CategoriaProductoDto>>
{
    public string? Buscar { get; set; }

    public bool IncluirInactivas { get; set; }
}

public class ListarCategoriasProductoQueryHandler : IRequestHandler<ListarCategoriasProductoQuery, List<CategoriaProductoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCategoriasProductoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CategoriaProductoDto>> Handle(ListarCategoriasProductoQuery request, CancellationToken cancellationToken)
    {
        IQueryable<CategoriaProducto> consulta = _unitOfWork.Repository<CategoriaProducto>().AsQueryable();

        if (!request.IncluirInactivas)
        {
            consulta = consulta.Where(categoria => categoria.Activa);
        }

        string texto = request.Buscar?.Trim().ToLowerInvariant() ?? string.Empty;
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
