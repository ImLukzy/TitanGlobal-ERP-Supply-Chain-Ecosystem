using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class ObtenerNotaCreditoDebitoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerNotaCreditoDebitoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(int id)
    {
        return PersistenciaNotaCreditoDebito.ObtenerDetalleAsync(_unitOfWork, id);
    }
}
