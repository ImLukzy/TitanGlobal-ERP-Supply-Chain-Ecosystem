using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.UseCases;

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
            StatusCodes.Status404NotFound,
            $"La solicitud de compra {id} no existe.");
    }

    private static void ExigirPendiente(SolicitudCompra solicitud)
    {
        if (solicitud.Estado != "Pendiente")
        {
            string estadoNormalizado = solicitud.Estado.ToLowerInvariant();
            throw new ReglaNegocioException(
                StatusCodes.Status409Conflict,
                $"La solicitud {solicitud.Numero} ya está {estadoNormalizado}.");
        }
    }
}
