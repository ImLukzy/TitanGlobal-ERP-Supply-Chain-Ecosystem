using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

public class ReactivarCentroCostoUseCase
{
    private readonly IUnitOfWorkCentrosCosto _unitOfWork;

    public ReactivarCentroCostoUseCase(IUnitOfWorkCentrosCosto unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<CentroCostoDetalleDto> ExecuteAsync(int id)
    {
        return VigenciaCentroCosto.CambiarAsync(_unitOfWork, id, activo: true);
    }
}
