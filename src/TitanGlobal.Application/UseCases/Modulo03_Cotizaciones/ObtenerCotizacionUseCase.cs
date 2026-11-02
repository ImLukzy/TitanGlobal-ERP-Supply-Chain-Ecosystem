using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones;

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
