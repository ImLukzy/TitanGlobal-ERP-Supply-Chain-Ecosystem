using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;

public class ActualizarImpuestoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarImpuestoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Ejecutar(int id, GuardarImpuestoRequestDto dto)
    {
        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (impuesto == null)
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        impuesto.Codigo = dto.Codigo;
        impuesto.Nombre = dto.Nombre;
        impuesto.Tipo = dto.Tipo;
        impuesto.Descripcion = dto.Descripcion;
        impuesto.Pais = dto.Pais;
        impuesto.Activo = dto.Activo;

        ValidacionImpuesto.ValidarDatos(impuesto);

        bool codigoExiste = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .AnyAsync(x => x.Codigo == impuesto.Codigo && x.Id != id);

        if (codigoExiste)
            throw CrearConflictoCodigoException(impuesto.Codigo);

        _unitOfWork.Repository<Impuesto>().Update(impuesto);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion)
            when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoCodigoException(impuesto.Codigo);
        }
    }

    private static ReglaNegocioException CrearConflictoCodigoException(
        string codigo)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un impuesto con el código {codigo}.");
    }
}
