using System; // <-- Agregado para soportar DateTime y Math
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.UseCases;

public class CalcularImpuestoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CalcularImpuestoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CalcularImpuestoResponseDto> Ejecutar(
        CalcularImpuestoRequestDto dto)
    {
        if (dto.BaseImponible <= 0)
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La base imponible debe ser mayor que cero.");

        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == dto.ImpuestoId && x.Activo);

        if (impuesto == null)
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                "El impuesto no existe o está inactivo.");

        DateTime fecha = dto.FechaOperacion.Date;

        TasaImpuesto? tasa = await _unitOfWork.Repository<TasaImpuesto>()
            .AsQueryable()
            .Where(t =>
                t.ImpuestoId == dto.ImpuestoId &&
                t.Activo &&
                t.FechaInicio <= fecha &&
                (!t.FechaFin.HasValue || t.FechaFin.Value >= fecha))
            .OrderByDescending(t => t.FechaInicio)
            .FirstOrDefaultAsync();

        if (tasa == null)
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "No existe una tasa vigente para la fecha indicada.");

        decimal importe = Math.Round(
            dto.BaseImponible * tasa.Porcentaje / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return new CalcularImpuestoResponseDto
        {
            ImpuestoId = impuesto.Id,
            Codigo = impuesto.Codigo,
            Nombre = impuesto.Nombre,
            BaseImponible = dto.BaseImponible,
            PorcentajeAplicado = tasa.Porcentaje,
            ImporteImpuesto = importe,
            Total = dto.BaseImponible + importe,
            FechaOperacion = fecha
        };
    }
}
