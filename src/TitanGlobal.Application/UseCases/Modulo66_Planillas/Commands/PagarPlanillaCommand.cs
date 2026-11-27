using MediatR;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;

public class PagarPlanillaCommand : IRequest<PlanillaIdDto>
{
    public int Id { get; set; }

    public required PagarPlanillaRequestDto Solicitud { get; set; }
}

public class PagarPlanillaCommandHandler : IRequestHandler<PagarPlanillaCommand, PlanillaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public PagarPlanillaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PlanillaIdDto> Handle(PagarPlanillaCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.FechaPago is null)
        {
            throw ValidacionPlanilla.Error400("La fecha de pago es obligatoria.");
        }

        Planilla planilla = await ValidacionPlanilla.ObtenerPlanillaAsync(_unidad, request.Id);
        if (planilla.Estado != ValidacionPlanilla.EstadoAprobada)
        {
            throw ValidacionPlanilla.Error409($"La planilla {planilla.Numero} debe estar aprobada para registrar el pago.");
        }

        DateOnly aprobacion = DateOnly.FromDateTime(planilla.FechaAprobacion ?? DateTime.UtcNow);
        if (request.Solicitud.FechaPago.Value < aprobacion)
        {
            throw ValidacionPlanilla.Error400("La fecha de pago no puede ser anterior a la aprobación.");
        }

        planilla.Estado = ValidacionPlanilla.EstadoPagada;
        planilla.FechaPago = request.Solicitud.FechaPago.Value;
        await _unidad.CompleteAsync();
        return new PlanillaIdDto { Id = planilla.Id };
    }
}
