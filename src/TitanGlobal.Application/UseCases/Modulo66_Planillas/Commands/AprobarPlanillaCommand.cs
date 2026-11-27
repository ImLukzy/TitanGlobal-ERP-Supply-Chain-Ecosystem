using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;

public class AprobarPlanillaCommand : IRequest<PlanillaIdDto>
{
    public int Id { get; set; }

    public required string Usuario { get; set; }
}

public class AprobarPlanillaCommandHandler : IRequestHandler<AprobarPlanillaCommand, PlanillaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AprobarPlanillaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PlanillaIdDto> Handle(AprobarPlanillaCommand request, CancellationToken cancellationToken)
    {
        Planilla planilla = await ValidacionPlanilla.ObtenerPlanillaAsync(_unidad, request.Id);
        if (planilla.Estado != ValidacionPlanilla.EstadoCalculada)
        {
            throw ValidacionPlanilla.Error409($"La planilla {planilla.Numero} debe estar calculada para aprobarse.");
        }

        List<PlanillaDetalle> incluidos = await _unidad.Repository<PlanillaDetalle>().AsQueryable()
            .Where(detalle => detalle.PlanillaId == request.Id && detalle.Incidencia == null)
            .ToListAsync();
        if (incluidos.Count == 0)
        {
            throw ValidacionPlanilla.Error409($"La planilla {planilla.Numero} no tiene empleados calculados.");
        }

        foreach (PlanillaDetalle detalle in incluidos)
        {
            detalle.NumeroBoleta = ValidacionPlanilla.NumeroBoleta(planilla.Anio, planilla.Mes, planilla.Tipo, detalle.EmpleadoId);
        }

        planilla.Estado = ValidacionPlanilla.EstadoAprobada;
        planilla.AprobadoPor = request.Usuario;
        planilla.FechaAprobacion = DateTime.UtcNow;
        await _unidad.CompleteAsync();
        return new PlanillaIdDto { Id = planilla.Id };
    }
}
