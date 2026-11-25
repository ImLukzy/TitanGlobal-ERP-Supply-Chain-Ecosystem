using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class RevertirAplicacionPagoUseCase
{
    private readonly IUnitOfWorkCuentasPorPagar _unitOfWork;
    private readonly TimeProvider _reloj;

    public RevertirAplicacionPagoUseCase(IUnitOfWorkCuentasPorPagar unitOfWork, TimeProvider reloj)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public Task<AplicacionPagoDto> ExecuteAsync(int id, int pagoId, RevertirAplicacionPagoRequestDto solicitud)
    {
        string motivo = ValidacionCuentasPorPagar.Motivo(solicitud.Motivo);
        return _unitOfWork.ConCuentaBloqueadaAsync(id, async () =>
        {
            CuentaPorPagarConSaldo datos = await ConsultasCuentasPorPagar.CuentaAsync(_unitOfWork, id);
            IQueryable<AplicacionPago> consulta = ConsultasCuentasPorPagar.Pagos(_unitOfWork, id).Where(fila => fila.Id == pagoId);
            AplicacionPago pago = await consulta.SingleOrDefaultAsync()
                ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "El pago indicado no existe en la cuenta.");
            if (pago.FechaReversion.HasValue)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "El pago ya fue revertido.");
            }
            ValidacionCuentasPorPagar.ExigirVigente(datos.Cuenta);

            pago.FechaReversion = FechasUtc.Normalizar(_reloj.GetUtcNow().UtcDateTime);
            pago.MotivoReversion = motivo;
            _unitOfWork.Repository<AplicacionPago>().Update(pago);
            await ValidacionCuentasPorPagar.GuardarAsync(_unitOfWork);
            return await ConsultasCuentasPorPagar.ProyectarPagos(consulta).SingleAsync();
        });
    }
}
