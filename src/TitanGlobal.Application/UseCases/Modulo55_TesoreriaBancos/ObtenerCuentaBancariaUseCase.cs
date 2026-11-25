using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class ObtenerCuentaBancariaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCuentaBancariaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<CuentaBancariaDetalleDto> ExecuteAsync(int id)
    {
        return ConsultasTesoreriaBancos.DetalleCuentaAsync(_unitOfWork, id);
    }
}
