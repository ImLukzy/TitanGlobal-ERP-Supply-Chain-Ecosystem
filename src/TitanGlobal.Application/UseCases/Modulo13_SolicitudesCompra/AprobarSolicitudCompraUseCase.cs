using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra;

public class AprobarSolicitudCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ObtenerSolicitudCompraUseCase _obtenerSolicitudCompraUseCase;

    public AprobarSolicitudCompraUseCase(
        IUnitOfWork unitOfWork,
        ObtenerSolicitudCompraUseCase obtenerSolicitudCompraUseCase)
    {
        _unitOfWork = unitOfWork;
        _obtenerSolicitudCompraUseCase = obtenerSolicitudCompraUseCase;
    }

    public async Task<SolicitudDetalleDto> ExecuteAsync(int id)
    {
        SolicitudCompra? solicitud = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (solicitud is null)
        {
            throw CrearNoEncontrada(id);
        }

        ExigirPendiente(solicitud);
        solicitud.Estado = "Aprobada";
        solicitud.FechaDecision = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<SolicitudCompra>().Update(solicitud);
        await _unitOfWork.CompleteAsync();

        return await _obtenerSolicitudCompraUseCase.ExecuteAsync(id);
    }

    private static ReglaNegocioException CrearNoEncontrada(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            $"La solicitud de compra {id} no existe.");
    }

    private static void ExigirPendiente(SolicitudCompra solicitud)
    {
        if (solicitud.Estado != "Pendiente")
        {
            string estadoNormalizado = solicitud.Estado.ToLowerInvariant();
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La solicitud {solicitud.Numero} ya está {estadoNormalizado}.");
        }
    }
}
