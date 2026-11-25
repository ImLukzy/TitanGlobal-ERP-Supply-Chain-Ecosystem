using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class RegistrarCuentaPorPagarUseCase
{
    private readonly IUnitOfWorkCuentasPorPagar _unitOfWork;
    private readonly TimeProvider _reloj;

    public RegistrarCuentaPorPagarUseCase(IUnitOfWorkCuentasPorPagar unitOfWork, TimeProvider reloj)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public Task<CuentaPorPagarDetalleDto> ExecuteAsync(RegistrarCuentaPorPagarRequestDto solicitud)
    {
        DateOnly corte = ValidacionCuentasPorPagar.CorteLima(_reloj);
        CuentaPorPagar cuenta = ValidacionCuentasPorPagar.DatosCuenta(solicitud, corte);
        return _unitOfWork.ConReferenciasBloqueadasAsync(cuenta.ProveedorId, cuenta.OrdenAbastecimientoId, async () =>
        {
            await ValidacionCuentasPorPagar.ValidarReferenciasAsync(_unitOfWork, cuenta);
            if (await ConsultasCuentasPorPagar.Cuentas(_unitOfWork, cuenta.ProveedorId)
                .AnyAsync(fila => fila.Serie == cuenta.Serie && fila.NumeroDocumento == cuenta.NumeroDocumento))
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, ValidacionCuentasPorPagar.FacturaDuplicada);
            }

            cuenta.FechaRegistro = FechasUtc.Normalizar(_reloj.GetUtcNow().UtcDateTime);
            await _unitOfWork.Repository<CuentaPorPagar>().AddAsync(cuenta);
            await ValidacionCuentasPorPagar.GuardarAsync(_unitOfWork);
            return await ConsultasCuentasPorPagar.DetalleAsync(_unitOfWork, cuenta.Id, corte);
        });
    }
}
