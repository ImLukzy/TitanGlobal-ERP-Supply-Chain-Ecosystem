using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

public class ObtenerCentroCostoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCentroCostoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<CentroCostoDetalleDto> ExecuteAsync(int id)
    {
        return ConsultasCentroCosto.ObtenerDetalleAsync(_unitOfWork.Repository<CentroCosto>().AsQueryable(), id);
    }
}
