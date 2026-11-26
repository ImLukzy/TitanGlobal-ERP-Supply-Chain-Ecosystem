using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

public class CrearCentroCostoUseCase
{
    private readonly IUnitOfWorkCentrosCosto _unitOfWork;

    public CrearCentroCostoUseCase(IUnitOfWorkCentrosCosto unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<CentroCostoDetalleDto> ExecuteAsync(GuardarCentroCostoRequestDto solicitud)
    {
        return _unitOfWork.ConCentrosCostoBloqueadosAsync(async () =>
        {
            CentroCosto centro = new CentroCosto
            {
                Activo = true,
                FechaRegistro = FechasUtc.Normalizar(DateTime.UtcNow)
            };
            await ValidacionCentroCosto.AplicarDatosAsync(_unitOfWork, centro, solicitud);

            IGenericRepository<CentroCosto> repositorio = _unitOfWork.Repository<CentroCosto>();
            await repositorio.AddAsync(centro);
            await ValidacionCentroCosto.GuardarAsync(_unitOfWork);
            return await ConsultasCentroCosto.ObtenerDetalleAsync(repositorio.AsQueryable(), centro.Id);
        });
    }
}
