using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.UseCases;

public class RegistrarTasaImpuestoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarTasaImpuestoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Ejecutar(
        int impuestoId,
        GuardarTasaImpuestoRequestDto dto)
    {
        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == impuestoId);

        if (impuesto == null || !impuesto.Activo)
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                "El impuesto no existe o está inactivo.");

        ValidacionImpuesto.ValidarTasa(
            dto.Porcentaje,
            dto.FechaInicio,
            dto.FechaFin);

        DateTime fechaFin = dto.FechaFin ?? new DateTime(9999, 12, 31);

        bool existeCruce = await _unitOfWork.Repository<TasaImpuesto>()
            .AsQueryable()
            .AnyAsync(t =>
                t.ImpuestoId == impuestoId &&
                t.Activo &&
                t.FechaInicio <= fechaFin &&
                (!t.FechaFin.HasValue || t.FechaFin.Value >= dto.FechaInicio));

        if (existeCruce)
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "Ya existe una tasa activa para ese periodo.");

        TasaImpuesto tasa = new TasaImpuesto
        {
            ImpuestoId = impuestoId,
            Porcentaje = dto.Porcentaje,
            FechaInicio = dto.FechaInicio.Date,
            FechaFin = dto.FechaFin?.Date,
            Activo = true,
            Observacion = dto.Observacion
        };

        await _unitOfWork.Repository<TasaImpuesto>().AddAsync(tasa);
        await _unitOfWork.CompleteAsync();

        return tasa.Id;
    }
}
