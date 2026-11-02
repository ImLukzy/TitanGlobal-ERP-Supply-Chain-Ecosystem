using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra;

public class ActualizarSolicitudCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarSolicitudCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SolicitudCreadaDto> ExecuteAsync(int id, GuardarSolicitudRequestDto solicitud)
    {
        GuardarSolicitudRequestDto datosValidados = await ValidacionSolicitudCompra.ValidarAsync(solicitud, _unitOfWork);
        SolicitudCompra? solicitudExistente = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (solicitudExistente is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La solicitud de compra {id} no existe.");
        }

        ExigirPendiente(solicitudExistente);
        List<DetalleSolicitudCompra> lineasAnteriores = await _unitOfWork.Repository<DetalleSolicitudCompra>()
            .AsQueryable()
            .Where(detalle => detalle.SolicitudCompraId == id)
            .ToListAsync();
        foreach (DetalleSolicitudCompra lineaAnterior in lineasAnteriores)
        {
            _unitOfWork.Repository<DetalleSolicitudCompra>().Remove(lineaAnterior);
        }

        solicitudExistente.Solicitante = datosValidados.Solicitante;
        solicitudExistente.ProveedorId = datosValidados.ProveedorId;
        solicitudExistente.Observaciones = datosValidados.Observaciones;
        foreach (LineaSolicitudRequestDto linea in datosValidados.Lineas!)
        {
            await _unitOfWork.Repository<DetalleSolicitudCompra>().AddAsync(new DetalleSolicitudCompra
            {
                SolicitudCompraId = id,
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioEstimado = linea.PrecioEstimado
            });
        }

        _unitOfWork.Repository<SolicitudCompra>().Update(solicitudExistente);
        await _unitOfWork.CompleteAsync();

        return new SolicitudCreadaDto
        {
            Id = solicitudExistente.Id,
            Numero = solicitudExistente.Numero
        };
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
