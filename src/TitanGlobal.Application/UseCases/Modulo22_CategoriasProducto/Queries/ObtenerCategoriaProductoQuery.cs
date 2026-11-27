using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Queries;

public class ObtenerCategoriaProductoQuery : IRequest<CategoriaProductoDto>
{
    public int Id { get; set; }
}

public class ObtenerCategoriaProductoQueryHandler : IRequestHandler<ObtenerCategoriaProductoQuery, CategoriaProductoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCategoriaProductoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoriaProductoDto> Handle(ObtenerCategoriaProductoQuery request, CancellationToken cancellationToken)
    {
        CategoriaProductoDto? resultado = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .Where(categoria => categoria.Id == request.Id)
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
            throw ValidacionCategoriaProducto.CrearNoEncontrada(request.Id);
        }

        return resultado;
    }
}
