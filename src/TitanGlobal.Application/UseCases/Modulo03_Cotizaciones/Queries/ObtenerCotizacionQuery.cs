using MediatR;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Queries;

public class ObtenerCotizacionQuery : IRequest<CotizacionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerCotizacionQueryHandler : IRequestHandler<ObtenerCotizacionQuery, CotizacionDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCotizacionQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionDetalleDto> Handle(ObtenerCotizacionQuery request, CancellationToken cancellationToken)
    {
        CotizacionDetalleDto? cotizacion = await ConsultaCotizacion.ObtenerDetalleAsync(_unitOfWork, request.Id);
        if (cotizacion is null)
        {
            throw OperacionesCotizacion.CrearNoEncontrada(request.Id);
        }

        return cotizacion;
    }
}
