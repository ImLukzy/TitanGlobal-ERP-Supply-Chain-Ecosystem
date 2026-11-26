using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class ObtenerCuentaPorPagarUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    public ObtenerCuentaPorPagarUseCase(IUnitOfWork unitOfWork, TimeProvider reloj)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public Task<CuentaPorPagarDetalleDto> ExecuteAsync(int id)
    {
        return ConsultasCuentasPorPagar.DetalleAsync(_unitOfWork, id, ValidacionCuentasPorPagar.CorteLima(_reloj));
    }
}
