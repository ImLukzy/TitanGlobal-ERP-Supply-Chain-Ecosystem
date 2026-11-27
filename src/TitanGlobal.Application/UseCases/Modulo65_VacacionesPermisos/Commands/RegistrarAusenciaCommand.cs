using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Commands;

public class RegistrarAusenciaCommand : IRequest<AusenciaCreadaDto>
{
    public required RegistrarAusenciaRequestDto Solicitud { get; set; }
}

public class RegistrarAusenciaCommandHandler : IRequestHandler<RegistrarAusenciaCommand, AusenciaCreadaDto>
{
    private readonly IUnitOfWork _unidad;

    public RegistrarAusenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AusenciaCreadaDto> Handle(RegistrarAusenciaCommand request, CancellationToken cancellationToken)
    {
        string? motivo = ValidacionAusencia.ValidarSolicitud(request.Solicitud, out string tipo);
        int empleadoId = request.Solicitud.EmpleadoId!.Value;
        DateOnly inicio = request.Solicitud.FechaInicio!.Value;
        DateOnly fin = request.Solicitud.FechaFin!.Value;
        Empleado empleado = await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionAusencia.EstadoEmpleadoActivo)
        {
            throw ValidacionAusencia.Error409($"El empleado {empleadoId} está cesado y no admite ausencias.");
        }

        if (inicio < empleado.FechaIngreso)
        {
            throw ValidacionAusencia.Error400("La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        }

        await ValidacionAusencia.ValidarSinSolapeAsync(_unidad, empleadoId, inicio, fin, null);
        int dias = ValidacionAusencia.Dias(inicio, fin);
        if (tipo == ValidacionAusencia.TipoVacaciones)
        {
            await ValidacionAusencia.ValidarSaldoAsync(_unidad, empleado, inicio, dias, null);
        }

        Ausencia ausencia = new Ausencia
        {
            Numero = ValidacionAusencia.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Tipo = tipo,
            FechaInicio = inicio,
            FechaFin = fin,
            Dias = dias,
            Motivo = motivo,
            Estado = ValidacionAusencia.EstadoPendiente,
            FechaSolicitud = ValidacionAusencia.Hoy()
        };
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await _unidad.Repository<Ausencia>().AddAsync(ausencia);
        await _unidad.CompleteAsync();
        ausencia.Numero = ValidacionAusencia.NumeroDefinitivo(ausencia.Id);
        _unidad.Repository<Ausencia>().Update(ausencia);
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new AusenciaCreadaDto { Id = ausencia.Id, Numero = ausencia.Numero };
    }
}
