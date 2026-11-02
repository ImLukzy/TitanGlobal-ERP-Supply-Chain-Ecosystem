using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra;

public class EliminarSolicitudCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarSolicitudCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        SolicitudCompra? solicitud = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (solicitud is null)
        {
            throw CrearNoEncontrada(id);
        }

        ExigirPendiente(solicitud);
        List<DetalleSolicitudCompra> lineas = await _unitOfWork.Repository<DetalleSolicitudCompra>()
            .AsQueryable()
            .Where(detalle => detalle.SolicitudCompraId == id)
            .ToListAsync();
        foreach (DetalleSolicitudCompra linea in lineas)
        {
            _unitOfWork.Repository<DetalleSolicitudCompra>().Remove(linea);
        }

        _unitOfWork.Repository<SolicitudCompra>().Remove(solicitud);
        await _unitOfWork.CompleteAsync();
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
