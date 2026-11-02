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
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Ya existe un impuesto registrado con ese código.");

        await _unitOfWork.Repository<Impuesto>().AddAsync(impuesto);
        await _unitOfWork.CompleteAsync();

        return impuesto.Id;
    }
}
