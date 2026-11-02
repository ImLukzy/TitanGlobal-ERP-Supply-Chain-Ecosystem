using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

public class CrearCategoriaProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCategoriaProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearCategoriaProductoDto solicitud)
    {
        string nombre = ValidacionCategoriaProducto.NormalizarNombre(solicitud.Nombre);
        string? descripcion = ValidacionCategoriaProducto.NormalizarDescripcion(solicitud.Descripcion);
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
