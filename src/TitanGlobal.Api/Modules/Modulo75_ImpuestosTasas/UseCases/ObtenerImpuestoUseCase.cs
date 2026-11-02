using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.UseCases;

public class ObtenerImpuestoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerImpuestoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ImpuestoDetalleDto> Ejecutar(int id)
    {
        ImpuestoDetalleDto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .Where(x => x.Id == id)
            .Select(x => new ImpuestoDetalleDto
            {
                Id = x.Id,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Tipo = x.Tipo,
                Descripcion = x.Descripcion,
                Pais = x.Pais,
                Activo = x.Activo,
                Tasas = x.Tasas
                    .OrderByDescending(t => t.FechaInicio)
                    .Select(t => new TasaImpuestoListadoDto
                    {
                        Id = t.Id,
                        Porcentaje = t.Porcentaje,
                        FechaInicio = t.FechaInicio,
                        FechaFin = t.FechaFin,
                        Activo = t.Activo,
                        Observacion = t.Observacion
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (impuesto == null)
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        return impuesto;
    }
}
