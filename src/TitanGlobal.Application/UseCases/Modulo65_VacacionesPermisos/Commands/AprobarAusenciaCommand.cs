using MediatR;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Commands;

public class AprobarAusenciaCommand : IRequest<AusenciaIdDto>
{
    public int Id { get; set; }

    public required string Usuario { get; set; }
}

public class AprobarAusenciaCommandHandler : IRequestHandler<AprobarAusenciaCommand, AusenciaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AprobarAusenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AusenciaIdDto> Handle(AprobarAusenciaCommand request, CancellationToken cancellationToken)
    {
        Ausencia ausencia = await ValidacionAusencia.ObtenerAsync(_unidad, request.Id);
        if (ausencia.Estado != ValidacionAusencia.EstadoPendiente)
        {
            throw ValidacionAusencia.NoPendiente(ausencia.Numero);
        }

        await ValidacionAusencia.ValidarSinSolapeAsync(_unidad, ausencia.EmpleadoId, ausencia.FechaInicio, ausencia.FechaFin, ausencia.Id);
        if (ausencia.Tipo == ValidacionAusencia.TipoVacaciones)
        {
            Empleado empleado = await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, ausencia.EmpleadoId);
            await ValidacionAusencia.ValidarSaldoAsync(_unidad, empleado, ausencia.FechaInicio, ausencia.Dias, ausencia.Id);
        }

        ausencia.Estado = ValidacionAusencia.EstadoAprobada;
        OperacionesAusencias.Decidir(ausencia, request.Usuario);
        await _unidad.CompleteAsync();
        return new AusenciaIdDto { Id = ausencia.Id };
    }
}
