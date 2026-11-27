using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados.Commands;

public class CesarEmpleadoCommand : IRequest<int>
{
    public int Id { get; set; }

    public required CeseEmpleadoRequestDto Solicitud { get; set; }
}

public class CesarEmpleadoCommandHandler : IRequestHandler<CesarEmpleadoCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CesarEmpleadoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CesarEmpleadoCommand request, CancellationToken cancellationToken)
    {
        DateOnly fechaCese = ValidacionEmpleado.ObtenerFechaCese(request.Solicitud.FechaCese);
        string motivo = ValidacionEmpleado.NormalizarMotivoCese(request.Solicitud.Motivo);
        Empleado empleado = await ValidacionEmpleado.ObtenerAsync(_unidad, request.Id);
        ValidacionEmpleado.ValidarCesable(empleado);
        ValidacionEmpleado.ValidarFechaCese(fechaCese, empleado.FechaIngreso);
        empleado.Estado = ValidacionEmpleado.EstadoCesado;
        empleado.FechaCese = fechaCese;
        empleado.MotivoCese = motivo;
        await CerrarContratosVigentesAsync(request.Id, fechaCese);
        await _unidad.CompleteAsync();
        return empleado.Id;
    }

    private async Task CerrarContratosVigentesAsync(int empleadoId, DateOnly fechaCese)
    {
        List<ContratoLaboral> vigentes = await _unidad.Repository<ContratoLaboral>().AsQueryable()
            .Where(contrato => contrato.EmpleadoId == empleadoId
                && contrato.Estado == ValidacionContratoLaboral.EstadoVigente)
            .ToListAsync();
        ContratoLaboral? posterior = vigentes.OrderBy(contrato => contrato.Id)
            .FirstOrDefault(contrato => contrato.FechaInicio > fechaCese);
        if (posterior is not null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"No se puede cesar al empleado {empleadoId}: el contrato {posterior.Numero} empieza después de la fecha de cese.");
        }

        foreach (ContratoLaboral contrato in vigentes.Where(contrato => contrato.FechaFin is null || contrato.FechaFin >= fechaCese))
        {
            contrato.Estado = ValidacionContratoLaboral.EstadoCerrado;
            contrato.FechaCierre = fechaCese;
            contrato.MotivoCierre = "Cese del empleado";
        }
    }
}
