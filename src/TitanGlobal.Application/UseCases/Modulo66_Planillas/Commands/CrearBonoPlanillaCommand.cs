using MediatR;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;

public class CrearBonoPlanillaCommand : IRequest<BonoPlanillaIdDto>
{
    public required GuardarBonoPlanillaRequestDto Solicitud { get; set; }
}

public class CrearBonoPlanillaCommandHandler : IRequestHandler<CrearBonoPlanillaCommand, BonoPlanillaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearBonoPlanillaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<BonoPlanillaIdDto> Handle(CrearBonoPlanillaCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.EmpleadoId is null)
        {
            throw ValidacionPlanilla.Error400("El empleado es obligatorio.");
        }

        int anio = ValidacionPlanilla.ValidarAnio(request.Solicitud.Anio);
        int mes = ValidacionPlanilla.ValidarMes(request.Solicitud.Mes);
        string tipo = ValidacionPlanilla.ValidarTipo(request.Solicitud.TipoPlanilla);
        string concepto = ValidacionPlanilla.ValidarConcepto(request.Solicitud.Concepto);
        decimal monto = ValidacionPlanilla.ValidarMonto(request.Solicitud.Monto);
        await ValidacionPlanilla.ObtenerEmpleadoAsync(_unidad, request.Solicitud.EmpleadoId.Value);
        BonoPlanilla bono = new BonoPlanilla
        {
            EmpleadoId = request.Solicitud.EmpleadoId.Value,
            Anio = anio,
            Mes = mes,
            TipoPlanilla = tipo,
            Concepto = concepto,
            Monto = monto
        };
        await _unidad.Repository<BonoPlanilla>().AddAsync(bono);
        await _unidad.CompleteAsync();
        return new BonoPlanillaIdDto { Id = bono.Id };
    }
}
