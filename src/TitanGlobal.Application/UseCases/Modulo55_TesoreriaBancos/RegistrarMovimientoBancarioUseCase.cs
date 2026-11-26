using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class RegistrarMovimientoBancarioUseCase
{
    private readonly IUnitOfWorkTesoreria _unitOfWork;
    private readonly TimeProvider _reloj;

    public RegistrarMovimientoBancarioUseCase(IUnitOfWorkTesoreria unitOfWork, TimeProvider? reloj = null)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj ?? TimeProvider.System;
    }

    public Task<MovimientoBancarioDto> ExecuteAsync(int id, RegistrarMovimientoBancarioRequestDto solicitud)
    {
        string tipo = ValidacionTesoreriaBancos.TipoMovimiento(solicitud.Tipo);
        DateOnly hoy = FechaNegocioPeru.DesdeUtc(_reloj.GetUtcNow().UtcDateTime);
        DatosOperacionBancaria datos = ValidacionTesoreriaBancos.Operacion(solicitud.Importe, solicitud.FechaOperacion, solicitud.Referencia, hoy);
        return _unitOfWork.ConCuentasBloqueadasAsync([id], async () =>
        {
            await ConsultasTesoreriaBancos.CuentaAsync(_unitOfWork, id);
            if (tipo == ValidacionTesoreriaBancos.Egreso)
            {
                decimal saldo = await ConsultasTesoreriaBancos.SaldoAsync(_unitOfWork, id);
                ValidacionTesoreriaBancos.ExigirSaldo(saldo, datos.Importe);
            }

            MovimientoBancario movimiento = datos.CrearMovimiento(id, tipo, FechasUtc.Normalizar(DateTime.UtcNow));
            await _unitOfWork.Repository<MovimientoBancario>().AddAsync(movimiento);
            await ValidacionTesoreriaBancos.GuardarAsync(_unitOfWork);
            return await ConsultasTesoreriaBancos.ProyectarMovimientos(
                ConsultasTesoreriaBancos.Movimientos(_unitOfWork, id).Where(fila => fila.Id == movimiento.Id)).SingleAsync();
        });
    }
}
