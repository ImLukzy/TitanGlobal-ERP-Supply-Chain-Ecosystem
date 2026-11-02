using System;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;

public class CrearImpuestoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearImpuestoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Ejecutar(GuardarImpuestoRequestDto dto)
    {
        Impuesto impuesto = new Impuesto
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Tipo = dto.Tipo,
            Descripcion = dto.Descripcion,
            Pais = dto.Pais,
            Activo = dto.Activo
        };

        ValidacionImpuesto.ValidarDatos(impuesto);

        bool existe = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .AnyAsync(x => x.Codigo == impuesto.Codigo);

        if (existe)
            throw CrearConflictoCodigoException(impuesto.Codigo);

        await _unitOfWork.Repository<Impuesto>().AddAsync(impuesto);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion)
            when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoCodigoException(impuesto.Codigo);
        }

        return impuesto.Id;
    }

    private static ReglaNegocioException CrearConflictoCodigoException(
        string codigo)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un impuesto con el código {codigo}.");
    }
}
