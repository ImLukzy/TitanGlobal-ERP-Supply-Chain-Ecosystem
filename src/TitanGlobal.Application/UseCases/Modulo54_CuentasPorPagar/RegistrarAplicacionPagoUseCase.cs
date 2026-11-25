using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class RegistrarAplicacionPagoUseCase
{
    private readonly IUnitOfWorkCuentasPorPagar _unitOfWork;
    private readonly TimeProvider _reloj;

    public RegistrarAplicacionPagoUseCase(IUnitOfWorkCuentasPorPagar unitOfWork, TimeProvider reloj)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public Task<AplicacionPagoDto> ExecuteAsync(int id, RegistrarAplicacionPagoRequestDto solicitud)
    {
        DateOnly fecha = ValidacionCuentasPorPagar.ValidarPago(solicitud, ValidacionCuentasPorPagar.CorteLima(_reloj));
        return _unitOfWork.ConCuentaBloqueadaAsync(id, async () =>
        {
            CuentaPorPagarConSaldo datos = await ConsultasCuentasPorPagar.CuentaAsync(_unitOfWork, id);
            ValidacionCuentasPorPagar.ExigirVigente(datos.Cuenta);
            if (fecha < datos.Cuenta.FechaDocumento)
            {
                throw ValidacionCuentasPorPagar.DatosInvalidos();
            }
            if (solicitud.Importe > datos.Saldo)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "El pago supera el saldo pendiente.");
            }

            AplicacionPago pago = new AplicacionPago
            {
                CuentaPorPagarId = id,
                Importe = solicitud.Importe,
                FechaPago = fecha,
                FechaRegistro = FechasUtc.Normalizar(_reloj.GetUtcNow().UtcDateTime)
            };
            await _unitOfWork.Repository<AplicacionPago>().AddAsync(pago);
            await ValidacionCuentasPorPagar.GuardarAsync(_unitOfWork);
            return await ConsultasCuentasPorPagar.ProyectarPagos(
                ConsultasCuentasPorPagar.Pagos(_unitOfWork, id).Where(fila => fila.Id == pago.Id)).SingleAsync();
        });
    }
}
