using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ObtenerPlanillaQuery : IRequest<PlanillaDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerPlanillaQueryHandler : IRequestHandler<ObtenerPlanillaQuery, PlanillaDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerPlanillaQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PlanillaDetalleDto> Handle(ObtenerPlanillaQuery request, CancellationToken cancellationToken)
    {
        PlanillaDetalleDto? detalle = await _unidad.Repository<Planilla>().AsQueryable()
            .AsNoTracking()
            .Where(planilla => planilla.Id == request.Id)
            .Select(planilla => new PlanillaDetalleDto
            {
                Id = planilla.Id,
                Numero = planilla.Numero,
                Anio = planilla.Anio,
                Mes = planilla.Mes,
                Tipo = planilla.Tipo,
                Estado = planilla.Estado,
                Empleados = planilla.Empleados,
                Incidencias = planilla.Incidencias,
                TotalIngresos = planilla.TotalIngresos,
                TotalDescuentos = planilla.TotalDescuentos,
                TotalNeto = planilla.TotalNeto,
                TotalAportesEmpleador = planilla.TotalAportesEmpleador,
                FechaCalculo = planilla.FechaCalculo,
                CalculadoPor = planilla.CalculadoPor,
                AprobadoPor = planilla.AprobadoPor,
                FechaAprobacion = planilla.FechaAprobacion,
                FechaPago = planilla.FechaPago
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionPlanilla.PlanillaInexistente(request.Id);
        }

        return detalle;
    }
}
