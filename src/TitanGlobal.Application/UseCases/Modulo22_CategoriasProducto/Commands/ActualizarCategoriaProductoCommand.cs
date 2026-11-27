using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Commands;

public class ActualizarCategoriaProductoCommand : IRequest<int>
{
    public int Id { get; set; }

    public required ActualizarCategoriaProductoDto Solicitud { get; set; }
}

public class ActualizarCategoriaProductoCommandHandler : IRequestHandler<ActualizarCategoriaProductoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarCategoriaProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarCategoriaProductoCommand request, CancellationToken cancellationToken)
    {
        CategoriaProducto? categoria = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == request.Id);
        if (categoria is null)
        {
            throw ValidacionCategoriaProducto.CrearNoEncontrada(request.Id);
        }

        string nombre = ValidacionCategoriaProducto.NormalizarNombre(request.Solicitud.Nombre);
        string? descripcion = ValidacionCategoriaProducto.NormalizarDescripcion(request.Solicitud.Descripcion);
        string nombreMinuscula = nombre.ToLowerInvariant();

        bool nombreExiste = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .AnyAsync(registro => registro.Id != request.Id && registro.Nombre.ToLower() == nombreMinuscula);
        if (nombreExiste)
        {
            throw ValidacionCategoriaProducto.CrearConflictoNombre(nombre);
        }

        categoria.Nombre = nombre;
        categoria.Descripcion = descripcion;
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw ValidacionCategoriaProducto.CrearConflictoNombre(nombre);
        }

        return categoria.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
