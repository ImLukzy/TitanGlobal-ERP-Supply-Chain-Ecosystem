using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Queries;

public class ObtenerPromocionQuery : IRequest<PromocionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerPromocionQueryHandler : IRequestHandler<ObtenerPromocionQuery, PromocionDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerPromocionQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PromocionDetalleDto> Handle(ObtenerPromocionQuery request, CancellationToken cancellationToken)
    {
        Promocion? promocion = await _unitOfWork.Repository<Promocion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (promocion is null)
        {
            throw OperacionesPromociones.Error(CodigosEstado.Status404NotFound, $"La promoción {request.Id} no existe.");
        }

        List<PromocionListadoDto> resultado = await OperacionesPromociones.ProyectarListadoAsync(_unitOfWork,
            new List<Promocion> { promocion }, FechasUtc.Normalizar(DateTime.UtcNow).Date);
        return CopiarDetalle(resultado[0]);
    }

    private static PromocionDetalleDto CopiarDetalle(PromocionListadoDto promocion)
    {
        return new PromocionDetalleDto
        {
            Id = promocion.Id,
            Codigo = promocion.Codigo,
            Nombre = promocion.Nombre,
            Tipo = promocion.Tipo,
            ProductoId = promocion.ProductoId,
            ProductoNombre = promocion.ProductoNombre,
            MontoMinimo = promocion.MontoMinimo,
            PorcentajeDescuento = promocion.PorcentajeDescuento,
            SegmentoId = promocion.SegmentoId,
            SegmentoCodigo = promocion.SegmentoCodigo,
            FechaInicio = promocion.FechaInicio,
            FechaFin = promocion.FechaFin,
            Activa = promocion.Activa,
            Vigente = promocion.Vigente
        };
    }
}
