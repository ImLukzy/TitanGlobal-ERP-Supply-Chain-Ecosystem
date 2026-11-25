using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class AnularCuentaPorPagarUseCase
{
    private readonly IUnitOfWorkCuentasPorPagar _unitOfWork;
    private readonly TimeProvider _reloj;

    public AnularCuentaPorPagarUseCase(IUnitOfWorkCuentasPorPagar unitOfWork, TimeProvider reloj)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public Task<CuentaPorPagarDetalleDto> ExecuteAsync(int id, AnularCuentaPorPagarRequestDto solicitud)
    {
        string motivo = ValidacionCuentasPorPagar.Motivo(solicitud.Motivo);
        return _unitOfWork.ConCuentaBloqueadaAsync(id, async () =>
        {
            CuentaPorPagarConSaldo datos = await ConsultasCuentasPorPagar.CuentaAsync(_unitOfWork, id);
            CuentaPorPagar cuenta = datos.Cuenta;
            ValidacionCuentasPorPagar.ExigirVigente(cuenta);
            if (datos.ImportePagado > 0m)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede anular una cuenta por pagar con pagos.");
            }

            cuenta.FechaAnulacion = FechasUtc.Normalizar(_reloj.GetUtcNow().UtcDateTime);
            cuenta.MotivoAnulacion = motivo;
            _unitOfWork.Repository<CuentaPorPagar>().Update(cuenta);
            await ValidacionCuentasPorPagar.GuardarAsync(_unitOfWork);
            return await ConsultasCuentasPorPagar.DetalleAsync(_unitOfWork, id, ValidacionCuentasPorPagar.CorteLima(_reloj));
        });
    }
}
