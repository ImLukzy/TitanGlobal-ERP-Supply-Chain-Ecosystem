using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class RegistrarTransferenciaBancariaUseCase
{
    private readonly IUnitOfWorkTesoreria _unitOfWork;
    private readonly TimeProvider _reloj;

    public RegistrarTransferenciaBancariaUseCase(IUnitOfWorkTesoreria unitOfWork, TimeProvider? reloj = null)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj ?? TimeProvider.System;
    }

    public Task<TransferenciaBancariaDto> ExecuteAsync(RegistrarTransferenciaBancariaRequestDto solicitud)
    {
        if (solicitud.CuentaOrigenId <= 0 || solicitud.CuentaDestinoId <= 0)
        {
            throw ValidacionTesoreriaBancos.DatosInvalidos();
        }
        if (solicitud.CuentaOrigenId == solicitud.CuentaDestinoId)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Las cuentas de origen y destino deben ser distintas.");
        }

        DateOnly hoy = FechaNegocioPeru.DesdeUtc(_reloj.GetUtcNow().UtcDateTime);
        DatosOperacionBancaria datos = ValidacionTesoreriaBancos.Operacion(solicitud.Importe, solicitud.FechaOperacion, solicitud.Referencia, hoy);
        return _unitOfWork.ConCuentasBloqueadasAsync([solicitud.CuentaOrigenId, solicitud.CuentaDestinoId], async () =>
        {
            CuentaBancaria origen = await ConsultasTesoreriaBancos.CuentaAsync(_unitOfWork, solicitud.CuentaOrigenId);
            CuentaBancaria destino = await ConsultasTesoreriaBancos.CuentaAsync(_unitOfWork, solicitud.CuentaDestinoId);
            if (origen.Moneda != destino.Moneda)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "Las cuentas de la transferencia deben tener la misma moneda.");
            }

            decimal saldo = await ConsultasTesoreriaBancos.SaldoAsync(_unitOfWork, origen.Id);
            ValidacionTesoreriaBancos.ExigirSaldo(saldo, datos.Importe);

            DateTime fechaRegistro = FechasUtc.Normalizar(DateTime.UtcNow);
            TransferenciaBancaria transferencia = new TransferenciaBancaria
            {
                CuentaOrigenId = origen.Id,
                CuentaDestinoId = destino.Id,
                Importe = datos.Importe,
                FechaOperacion = datos.FechaOperacion,
                Referencia = datos.Referencia,
                FechaRegistro = fechaRegistro,
                MovimientosBancarios =
                [
                    datos.CrearMovimiento(origen.Id, ValidacionTesoreriaBancos.Egreso, fechaRegistro),
                    datos.CrearMovimiento(destino.Id, ValidacionTesoreriaBancos.Ingreso, fechaRegistro)
                ]
            };
            await _unitOfWork.Repository<TransferenciaBancaria>().AddAsync(transferencia);
            await ValidacionTesoreriaBancos.GuardarAsync(_unitOfWork);
            return await ConsultasTesoreriaBancos.DetalleTransferenciaAsync(_unitOfWork, transferencia.Id);
        });
    }
}
