using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

public class ActualizarCentroCostoUseCase
{
    private readonly IUnitOfWorkCentrosCosto _unitOfWork;

    public ActualizarCentroCostoUseCase(IUnitOfWorkCentrosCosto unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<CentroCostoDetalleDto> ExecuteAsync(int id, GuardarCentroCostoRequestDto solicitud)
    {
        return _unitOfWork.ConCentrosCostoBloqueadosAsync(async () =>
        {
            IQueryable<CentroCosto> consulta = _unitOfWork.Repository<CentroCosto>().AsQueryable();
            CentroCosto centro = await ConsultasCentroCosto.CargarParaEscrituraAsync(consulta, id);
            await ValidacionCentroCosto.AplicarDatosAsync(_unitOfWork, centro, solicitud);
            await ValidacionCentroCosto.GuardarAsync(_unitOfWork);
            return await ConsultasCentroCosto.ObtenerDetalleAsync(consulta, id);
        });
    }
}
