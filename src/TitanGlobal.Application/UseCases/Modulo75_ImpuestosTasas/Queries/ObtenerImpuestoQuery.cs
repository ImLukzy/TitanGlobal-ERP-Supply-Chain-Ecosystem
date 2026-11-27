using MediatR;
using System;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas.Queries;

public class ObtenerImpuestoQuery : IRequest<ImpuestoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerImpuestoQueryHandler : IRequestHandler<ObtenerImpuestoQuery, ImpuestoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerImpuestoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ImpuestoDetalleDto> Handle(ObtenerImpuestoQuery request, CancellationToken cancellationToken)
    {
        ImpuestoDetalleDto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .Where(x => x.Id == request.Id)
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
                CodigosEstado.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        return impuesto;
    }
}
