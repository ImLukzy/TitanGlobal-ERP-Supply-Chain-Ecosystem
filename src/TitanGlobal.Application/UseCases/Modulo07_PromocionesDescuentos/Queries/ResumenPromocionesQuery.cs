using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Queries;

public class ResumenPromocionesQuery : IRequest<ResumenPromocionesDto>
{
}

public class ResumenPromocionesQueryHandler : IRequestHandler<ResumenPromocionesQuery, ResumenPromocionesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenPromocionesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenPromocionesDto> Handle(ResumenPromocionesQuery request, CancellationToken cancellationToken)
    {
        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        IQueryable<Promocion> consulta = _unitOfWork.Repository<Promocion>().AsQueryable();
        int total = await consulta.CountAsync();
        int activas = await consulta.CountAsync(promocion => promocion.Activa);
        List<Promocion> vigentes = await consulta.Where(promocion => promocion.Activa
            && promocion.FechaInicio <= hoy && promocion.FechaFin >= hoy).ToListAsync();
        decimal promedio = vigentes.Count == 0
            ? 0m
            : decimal.Round(vigentes.Average(promocion => promocion.PorcentajeDescuento), 2, MidpointRounding.AwayFromZero);
        return new ResumenPromocionesDto
        {
            Total = total,
            Activas = activas,
            Vigentes = vigentes.Count,
            DeProducto = await consulta.CountAsync(promocion => promocion.Tipo == "Producto"),
            DeMontoMinimo = await consulta.CountAsync(promocion => promocion.Tipo == "MontoMinimo"),
            DeSegmento = await consulta.CountAsync(promocion => promocion.SegmentoClienteId.HasValue),
            PorcentajePromedioVigentes = promedio
        };
    }
}
