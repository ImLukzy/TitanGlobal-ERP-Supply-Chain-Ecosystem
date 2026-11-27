using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Commands;

public class RechazarCotizacionCommand : IRequest<CotizacionDetalleDto>
{
    public int Id { get; set; }

    public required RechazarCotizacionRequestDto Solicitud { get; set; }
}

public class RechazarCotizacionCommandHandler : IRequestHandler<RechazarCotizacionCommand, CotizacionDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public RechazarCotizacionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionDetalleDto> Handle(RechazarCotizacionCommand request, CancellationToken cancellationToken)
    {
        Cotizacion? cotizacion = await _unitOfWork.Repository<Cotizacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (cotizacion is null)
        {
            throw OperacionesCotizacion.CrearNoEncontrada(request.Id);
        }

        OperacionesCotizacion.ExigirEmitida(cotizacion);
        string? motivo = ValidacionCotizacion.NormalizarMotivo(request.Solicitud.Motivo);
        cotizacion.Estado = "Rechazada";
        cotizacion.MotivoRechazo = motivo;
        cotizacion.FechaDecision = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<Cotizacion>().Update(cotizacion);
        await _unitOfWork.CompleteAsync();
        return await ConsultaCotizacion.ObtenerDetalleAsync(_unitOfWork, request.Id)
            ?? throw OperacionesCotizacion.CrearNoEncontrada(request.Id);
    }
}
