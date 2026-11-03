using System; // <-- Agregado para soportar DateTime y Math
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;

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
        decimal baseImponible = Math.Round(
            dto.BaseImponible,
            2,
            MidpointRounding.AwayFromZero);

        if (baseImponible <= 0)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La base imponible debe ser mayor que cero.");

        if (dto.FechaOperacion == default)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de operación es obligatoria.");

        DateTime fecha = ValidacionImpuesto.NormalizarDia(dto.FechaOperacion);

        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == dto.ImpuestoId && x.Activo);

        if (impuesto == null)
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "El impuesto no existe o está inactivo.");

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
                CodigosEstado.Status400BadRequest,
                "No existe una tasa vigente para la fecha indicada.");

        decimal importe;
        decimal total;

        try
        {
            importe = Math.Round(
                baseImponible * tasa.Porcentaje / 100m,
                2,
                MidpointRounding.AwayFromZero);
            total = baseImponible + importe;
        }
        catch (OverflowException)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La base imponible es demasiado grande.");
        }

        return new CalcularImpuestoResponseDto
        {
            ImpuestoId = impuesto.Id,
            Codigo = impuesto.Codigo,
            Nombre = impuesto.Nombre,
            BaseImponible = baseImponible,
            PorcentajeAplicado = tasa.Porcentaje,
            ImporteImpuesto = importe,
            Total = total,
            FechaOperacion = fecha
        };
    }
}
