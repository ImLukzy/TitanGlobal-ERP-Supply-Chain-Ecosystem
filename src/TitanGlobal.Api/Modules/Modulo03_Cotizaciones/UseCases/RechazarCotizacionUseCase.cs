using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

public class RechazarCotizacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RechazarCotizacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionDetalleDto> ExecuteAsync(int id, RechazarCotizacionRequestDto solicitud)
    {
        Cotizacion? cotizacion = await _unitOfWork.Repository<Cotizacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (cotizacion is null)
        {
            throw ActualizarCotizacionUseCase.CrearNoEncontrada(id);
        }

        ActualizarCotizacionUseCase.ExigirEmitida(cotizacion);
        string? motivo = ValidacionCotizacion.NormalizarMotivo(solicitud.Motivo);
        cotizacion.Estado = "Rechazada";
        cotizacion.MotivoRechazo = motivo;
        cotizacion.FechaDecision = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<Cotizacion>().Update(cotizacion);
        await _unitOfWork.CompleteAsync();
        return await ConsultaCotizacion.ObtenerDetalleAsync(_unitOfWork, id)
            ?? throw ActualizarCotizacionUseCase.CrearNoEncontrada(id);
    }
}
