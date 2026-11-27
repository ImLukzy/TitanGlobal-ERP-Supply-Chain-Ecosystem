using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Commands;

public class RegistrarPrestamoCommand : IRequest<PrestamoCreadoDto>
{
    public required RegistrarPrestamoRequestDto Solicitud { get; set; }
}

public class RegistrarPrestamoCommandHandler : IRequestHandler<RegistrarPrestamoCommand, PrestamoCreadoDto>
{
    private readonly IUnitOfWork _unidad;

    public RegistrarPrestamoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PrestamoCreadoDto> Handle(RegistrarPrestamoCommand request, CancellationToken cancellationToken)
    {
        ValidacionPrestamo.ValidarSolicitud(request.Solicitud, out string tipo, out int numeroCuotas, out string motivo);
        int empleadoId = request.Solicitud.EmpleadoId!.Value;
        decimal monto = request.Solicitud.Monto!.Value;
        DateOnly otorgamiento = request.Solicitud.FechaOtorgamiento!.Value;
        int primerAnio = request.Solicitud.PrimerAnio!.Value;
        int primerMes = request.Solicitud.PrimerMes!.Value;
        Empleado empleado = await ValidacionPrestamo.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionPrestamo.EstadoEmpleadoActivo)
        {
            throw ValidacionPrestamo.Error409($"El empleado {empleadoId} está cesado y no admite préstamos.");
        }

        decimal? sueldo = await ValidacionPrestamo.LeerSueldoVigenteAsync(_unidad, empleadoId, otorgamiento);
        if (sueldo is null)
        {
            throw ValidacionPrestamo.Error409($"El empleado {empleadoId} no tiene un contrato vigente en la fecha de otorgamiento.");
        }

        List<CronogramaPrestamo.CuotaProgramada> cronograma = CronogramaPrestamo.Generar(monto, numeroCuotas, primerAnio, primerMes);
        decimal maximo = ValidacionPrestamo.CuotaMaxima(ValidacionPrestamo.NetoEstimado(sueldo.Value));
        Dictionary<(int Anio, int Mes), decimal> comprometido =
            await ValidacionPrestamo.LeerComprometidoPorMesAsync(_unidad, empleadoId, primerAnio, primerMes);
        foreach (CronogramaPrestamo.CuotaProgramada cuota in cronograma)
        {
            decimal previo = comprometido.TryGetValue((cuota.Anio, cuota.Mes), out decimal existente) ? existente : 0m;
            decimal total = previo + cuota.Monto;
            if (total > maximo)
            {
                throw ValidacionPrestamo.Error409(
                    $"La cuota de {cuota.Mes:D2}/{cuota.Anio} sería {ValidacionPrestamo.Importe(total)} y supera el máximo de {ValidacionPrestamo.Importe(maximo)} (30 % del neto estimado).");
            }
        }

        Prestamo prestamo = new Prestamo
        {
            Numero = ValidacionPrestamo.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Monto = monto,
            NumeroCuotas = numeroCuotas,
            FechaOtorgamiento = otorgamiento,
            PrimerAnio = primerAnio,
            PrimerMes = primerMes,
            Motivo = motivo,
            Estado = ValidacionPrestamo.EstadoActivo,
            Saldo = monto
        };
        foreach (CronogramaPrestamo.CuotaProgramada cuota in cronograma)
        {
            prestamo.CuotasPrestamo.Add(new CuotaPrestamo
            {
                NumeroCuota = cuota.NumeroCuota,
                Anio = cuota.Anio,
                Mes = cuota.Mes,
                Monto = cuota.Monto,
                MontoDescontado = 0m,
                Estado = ValidacionPrestamo.CuotaPendiente
            });
        }

        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await _unidad.Repository<Prestamo>().AddAsync(prestamo);
        await _unidad.CompleteAsync();
        prestamo.Numero = ValidacionPrestamo.NumeroDefinitivo(prestamo.Id);
        _unidad.Repository<Prestamo>().Update(prestamo);
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new PrestamoCreadoDto { Id = prestamo.Id, Numero = prestamo.Numero };
    }
}
