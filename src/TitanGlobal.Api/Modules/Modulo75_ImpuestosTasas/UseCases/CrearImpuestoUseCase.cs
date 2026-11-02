using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.UseCases;

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
                StatusCodes.Status400BadRequest,
                "Ya existe un impuesto registrado con ese código.");

        await _unitOfWork.Repository<Impuesto>().AddAsync(impuesto);
        await _unitOfWork.CompleteAsync();

        return impuesto.Id;
    }
}
