using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class RegistrarExamenMedicoCommand : IRequest<ExamenMedicoIdDto>
{
    public required RegistrarExamenMedicoRequestDto Solicitud { get; set; }
}

public class RegistrarExamenMedicoCommandHandler : IRequestHandler<RegistrarExamenMedicoCommand, ExamenMedicoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RegistrarExamenMedicoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ExamenMedicoIdDto> Handle(RegistrarExamenMedicoCommand request, CancellationToken cancellationToken)
    {
        ExamenMedico examen = ValidacionSst.ValidarExamen(request.Solicitud);
        Empleado empleado = await ValidacionSst.ObtenerEmpleadoAsync(_unidad, examen.EmpleadoId);
        if (empleado.Estado != ValidacionSst.EmpleadoActivo)
        {
            if (examen.Tipo != ValidacionSst.ExamenRetiro)
            {
                throw ValidacionSst.EmpleadoCesado(empleado.Id);
            }

            if (empleado.FechaCese is null
                || examen.Fecha > empleado.FechaCese.Value.AddDays(ValidacionSst.DiasRetiroTrasCese))
            {
                throw ValidacionSst.Error409($"El examen de retiro se registra hasta 30 días después del cese del empleado {empleado.Id}.");
            }
        }

        await _unidad.Repository<ExamenMedico>().AddAsync(examen);
        await _unidad.CompleteAsync();
        return new ExamenMedicoIdDto { Id = examen.Id };
    }
}
