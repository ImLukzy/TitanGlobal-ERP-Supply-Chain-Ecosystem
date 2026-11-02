using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones;

public class ActualizarCotizacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarCotizacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionCreadaDto> ExecuteAsync(int id, GuardarCotizacionRequestDto solicitud)
    {
        CotizacionNormalizada datos = await ValidacionCotizacion.ValidarAsync(solicitud, _unitOfWork);
        Cotizacion? cotizacion = await _unitOfWork.Repository<Cotizacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (cotizacion is null)
        {
            throw CrearNoEncontrada(id);
        }

        ExigirEmitida(cotizacion);
        List<DetalleCotizacion> anteriores = await _unitOfWork.Repository<DetalleCotizacion>()
            .AsQueryable()
            .Where(detalle => detalle.CotizacionId == id)
            .ToListAsync();
        foreach (DetalleCotizacion detalle in anteriores)
        {
            _unitOfWork.Repository<DetalleCotizacion>().Remove(detalle);
        }

        cotizacion.ClienteId = solicitud.ClienteId;
        cotizacion.FechaValidez = datos.FechaValidez;
        cotizacion.Observaciones = datos.Observaciones;
        foreach (LineaCotizacionRequestDto linea in datos.Lineas)
        {
            await _unitOfWork.Repository<DetalleCotizacion>().AddAsync(new DetalleCotizacion
            {
                CotizacionId = cotizacion.Id,
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                DescuentoPorcentaje = linea.DescuentoPorcentaje
            });
        }

        _unitOfWork.Repository<Cotizacion>().Update(cotizacion);
        await _unitOfWork.CompleteAsync();
        return new CotizacionCreadaDto
        {
            Id = cotizacion.Id,
            Numero = cotizacion.Numero
        };
    }

    internal static void ExigirEmitida(Cotizacion cotizacion)
    {
        if (cotizacion.Estado != "Emitida")
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La cotización {cotizacion.Numero} ya está {cotizacion.Estado.ToLowerInvariant()}.");
        }
    }

    internal static ReglaNegocioException CrearNoEncontrada(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            $"La cotización {id} no existe.");
    }
}
