using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

public class ActualizarCategoriaProductoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarCategoriaProductoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, ActualizarCategoriaProductoDto solicitud)
    {
        CategoriaProducto? categoria = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == id);
        if (categoria is null)
        {
            throw ValidacionCategoriaProducto.CrearNoEncontrada(id);
        }

        string nombre = ValidacionCategoriaProducto.NormalizarNombre(solicitud.Nombre);
        string? descripcion = ValidacionCategoriaProducto.NormalizarDescripcion(solicitud.Descripcion);
        string nombreMinuscula = nombre.ToLowerInvariant();

        bool nombreExiste = await _unitOfWork.Repository<CategoriaProducto>()
            .AsQueryable()
            .AnyAsync(registro => registro.Id != id && registro.Nombre.ToLower() == nombreMinuscula);
        if (nombreExiste)
        {
            throw ValidacionCategoriaProducto.CrearConflictoNombre(nombre);
        }

        categoria.Nombre = nombre;
        categoria.Descripcion = descripcion;
        await _unitOfWork.CompleteAsync();

        return categoria.Id;
    }
}
