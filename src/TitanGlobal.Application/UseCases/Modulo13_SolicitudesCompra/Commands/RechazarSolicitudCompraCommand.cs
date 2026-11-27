using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra.Queries;

namespace TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra.Commands;

public class RechazarSolicitudCompraCommand : IRequest<SolicitudDetalleDto>
{
    public int Id { get; set; }

    public required RechazarSolicitudRequestDto SolicitudRechazo { get; set; }
}

public class RechazarSolicitudCompraCommandHandler : IRequestHandler<RechazarSolicitudCompraCommand, SolicitudDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ISender _sender;

    public RechazarSolicitudCompraCommandHandler(
        ISender sender,
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<SolicitudDetalleDto> Handle(RechazarSolicitudCompraCommand request, CancellationToken cancellationToken)
    {
        SolicitudCompra? solicitud = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (solicitud is null)
        {
            throw CrearNoEncontrada(request.Id);
        }

        ExigirPendiente(solicitud);
        string motivo = NormalizarMotivo(request.SolicitudRechazo.Motivo);
        solicitud.Estado = "Rechazada";
        solicitud.MotivoRechazo = motivo;
        solicitud.FechaDecision = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<SolicitudCompra>().Update(solicitud);
        await _unitOfWork.CompleteAsync();

        return await _sender.Send(new ObtenerSolicitudCompraQuery { Id = request.Id });
    }

    private static string NormalizarMotivo(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El motivo de rechazo es obligatorio.");
        }

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de rechazo no puede superar 500 caracteres.");
        }

        return valor;
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
