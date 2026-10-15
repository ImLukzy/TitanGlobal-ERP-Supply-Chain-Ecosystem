using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

public class ObtenerCotizacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCotizacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionDetalleDto> ExecuteAsync(int id)
    {
        CotizacionDetalleDto? cotizacion = await ConsultaCotizacion.ObtenerDetalleAsync(_unitOfWork, id);
        if (cotizacion is null)
        {
            throw ActualizarCotizacionUseCase.CrearNoEncontrada(id);
        }

        return cotizacion;
    }
}
