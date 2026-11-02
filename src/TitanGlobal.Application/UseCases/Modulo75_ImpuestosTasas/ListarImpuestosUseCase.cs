using System;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;

public class ListarImpuestosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarImpuestosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ImpuestoListadoDto>> Ejecutar()
    {
        DateTime hoy = DateTime.UtcNow.Date;

        return await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .Select(impuesto => new ImpuestoListadoDto
            {
                Id = impuesto.Id,
                Codigo = impuesto.Codigo,
                Nombre = impuesto.Nombre,
                Tipo = impuesto.Tipo,
                Pais = impuesto.Pais,
                Activo = impuesto.Activo,

                PorcentajeVigente = impuesto.Tasas
                    .Where(tasa =>
                        tasa.Activo &&
                        tasa.FechaInicio <= hoy &&
                        (!tasa.FechaFin.HasValue || tasa.FechaFin >= hoy))
                    .OrderByDescending(tasa => tasa.FechaInicio)
                    .Select(tasa => (decimal?)tasa.Porcentaje)
                    .FirstOrDefault(),

                FechaInicioVigencia = impuesto.Tasas
                    .Where(tasa =>
                        tasa.Activo &&
                        tasa.FechaInicio <= hoy &&
                        (!tasa.FechaFin.HasValue || tasa.FechaFin >= hoy))
                    .OrderByDescending(tasa => tasa.FechaInicio)
                    .Select(tasa => (DateTime?)tasa.FechaInicio)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }
}
