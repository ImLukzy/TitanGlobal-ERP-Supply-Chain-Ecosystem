using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Commands;

public class CrearCategoriaProductoCommand : IRequest<int>
{
    public required CrearCategoriaProductoDto Solicitud { get; set; }
}

public class CrearCategoriaProductoCommandHandler : IRequestHandler<CrearCategoriaProductoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCategoriaProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearCategoriaProductoCommand request, CancellationToken cancellationToken)
    {
        string nombre = ValidacionCategoriaProducto.NormalizarNombre(request.Solicitud.Nombre);
        string? descripcion = ValidacionCategoriaProducto.NormalizarDescripcion(request.Solicitud.Descripcion);
        string nombreMinuscula = nombre.ToLowerInvariant();

        bool nombreExiste = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .AnyAsync(categoria => categoria.Nombre.ToLower() == nombreMinuscula);
        if (nombreExiste)
        {
            throw ValidacionCategoriaProducto.CrearConflictoNombre(nombre);
        }

        CategoriaProducto categoriaNueva = new CategoriaProducto
        {
            Nombre = nombre,
            Descripcion = descripcion,
            Activa = true
        };
        await _unitOfWork.Repository<CategoriaProducto>().AddAsync(categoriaNueva);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw ValidacionCategoriaProducto.CrearConflictoNombre(nombre);
        }

        return categoriaNueva.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
