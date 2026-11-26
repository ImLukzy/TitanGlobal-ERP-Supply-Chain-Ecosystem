using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class RevertirMovimientoBancarioUseCase
{
    private readonly IUnitOfWorkTesoreria _unitOfWork;

    public RevertirMovimientoBancarioUseCase(IUnitOfWorkTesoreria unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<MovimientoBancarioDto> ExecuteAsync(int id, int movimientoId, RevertirMovimientoBancarioRequestDto solicitud)
    {
        string motivo = ValidacionTesoreriaBancos.Motivo(solicitud.Motivo);
        return _unitOfWork.ConCuentasBloqueadasAsync([id], async () =>
        {
            await ConsultasTesoreriaBancos.CuentaAsync(_unitOfWork, id);
            IQueryable<MovimientoBancario> consulta = ConsultasTesoreriaBancos.Movimientos(_unitOfWork, id)
                .Where(fila => fila.Id == movimientoId);
            MovimientoBancario movimiento = await consulta.SingleOrDefaultAsync()
                ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "El movimiento bancario no existe en la cuenta.");

            if (await _unitOfWork.TieneCoincidenciaVigenteAsync(movimiento.Id))
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede revertir un movimiento conciliado.");
            }
            if (movimiento.FechaReversion.HasValue)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "El movimiento bancario ya fue revertido.");
            }
            if (movimiento.TransferenciaBancariaId.HasValue)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede revertir por separado un movimiento de transferencia.");
            }
            if (movimiento.Tipo == ValidacionTesoreriaBancos.Ingreso)
            {
                decimal saldo = await ConsultasTesoreriaBancos.SaldoAsync(_unitOfWork, id);
                ValidacionTesoreriaBancos.ExigirSaldo(saldo, movimiento.Importe);
            }

            movimiento.FechaReversion = FechasUtc.Normalizar(DateTime.UtcNow);
            movimiento.MotivoReversion = motivo;
            _unitOfWork.Repository<MovimientoBancario>().Update(movimiento);
            await ValidacionTesoreriaBancos.GuardarAsync(_unitOfWork);
            return await ConsultasTesoreriaBancos.ProyectarMovimientos(consulta).SingleAsync();
        });
    }
}
