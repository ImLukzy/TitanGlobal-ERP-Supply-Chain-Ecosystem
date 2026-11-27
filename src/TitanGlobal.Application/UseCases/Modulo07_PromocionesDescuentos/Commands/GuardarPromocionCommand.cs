using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Commands;

public class GuardarPromocionCommand : IRequest<int>
{
    public required GuardarPromocionRequestDto Solicitud { get; set; }

    public int? Id { get; set; }
}

public class GuardarPromocionCommandHandler : IRequestHandler<GuardarPromocionCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GuardarPromocionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GuardarPromocionCommand request, CancellationToken cancellationToken)
    {
        await ValidacionPromocion.ValidarAsync(request.Solicitud, request.Id, _unitOfWork);
        Promocion? promocion = null;
        if (request.Id.HasValue)
        {
            promocion = await _unitOfWork.Repository<Promocion>().AsQueryable()
                .SingleOrDefaultAsync(entidad => entidad.Id == request.Id.Value);
            if (promocion is null)
            {
                throw OperacionesPromociones.Error(CodigosEstado.Status404NotFound, $"La promoción {request.Id.Value} no existe.");
            }
        }

        if (promocion is null)
        {
            promocion = new Promocion();
            await _unitOfWork.Repository<Promocion>().AddAsync(promocion);
        }

        promocion.Codigo = request.Solicitud.Codigo;
        promocion.Nombre = request.Solicitud.Nombre;
        promocion.Tipo = request.Solicitud.Tipo;
        promocion.ProductoId = request.Solicitud.ProductoId;
        promocion.MontoMinimo = request.Solicitud.MontoMinimo;
        promocion.PorcentajeDescuento = request.Solicitud.PorcentajeDescuento;
        promocion.SegmentoClienteId = request.Solicitud.SegmentoClienteId;
        promocion.FechaInicio = FechasUtc.Normalizar(request.Solicitud.FechaInicio);
        promocion.FechaFin = FechasUtc.Normalizar(request.Solicitud.FechaFin);
        promocion.Activa = request.Solicitud.Activa;
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw OperacionesPromociones.Error(CodigosEstado.Status409Conflict, $"Ya existe una promoción con el código {request.Solicitud.Codigo}.");
        }

        return promocion.Id;
    }
}
