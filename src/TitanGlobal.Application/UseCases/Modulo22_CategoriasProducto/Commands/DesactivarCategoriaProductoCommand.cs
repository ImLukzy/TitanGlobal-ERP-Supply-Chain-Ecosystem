using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Commands;

public class DesactivarCategoriaProductoCommand : IRequest
{
    public int Id { get; set; }
}

public class DesactivarCategoriaProductoCommandHandler : IRequestHandler<DesactivarCategoriaProductoCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DesactivarCategoriaProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DesactivarCategoriaProductoCommand request, CancellationToken cancellationToken)
    {
        CategoriaProducto? categoria = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == request.Id);
        if (categoria is null)
        {
            throw ValidacionCategoriaProducto.CrearNoEncontrada(request.Id);
        }

        categoria.Activa = false;
        await _unitOfWork.CompleteAsync();
    }
}
