using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class ObtenerTransferenciaBancariaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerTransferenciaBancariaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<TransferenciaBancariaDto> ExecuteAsync(int id)
    {
        return ConsultasTesoreriaBancos.DetalleTransferenciaAsync(_unitOfWork, id);
    }
}
