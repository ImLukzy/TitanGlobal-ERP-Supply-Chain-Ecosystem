using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Queries;

public class ResumenListasPreciosQuery : IRequest<ResumenListasDto>
{
}

public class ResumenListasPreciosQueryHandler : IRequestHandler<ResumenListasPreciosQuery, ResumenListasDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenListasPreciosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenListasDto> Handle(ResumenListasPreciosQuery request, CancellationToken cancellationToken)
    {
        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        IQueryable<ListaPrecios> listas = _unitOfWork.Repository<ListaPrecios>().AsQueryable();
        int total = await listas.CountAsync();
        int activas = await listas.CountAsync(lista => lista.Activa);
        int vigentes = await listas.CountAsync(lista => lista.Activa && lista.FechaInicio <= hoy
            && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy));
        int deSegmento = await listas.CountAsync(lista => lista.SegmentoClienteId.HasValue);
        int generales = await listas.CountAsync(lista => !lista.SegmentoClienteId.HasValue);
        int productosConPrecioEspecial = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
            .Where(item => item.ListaPrecios.Activa && item.ListaPrecios.FechaInicio <= hoy
                && (!item.ListaPrecios.FechaFin.HasValue || item.ListaPrecios.FechaFin.Value >= hoy))
            .Select(item => item.ProductoId).Distinct().CountAsync();
        return new ResumenListasDto
        {
            Total = total,
            Activas = activas,
            Vigentes = vigentes,
            DeSegmento = deSegmento,
            Generales = generales,
            ProductosConPrecioEspecial = productosConPrecioEspecial
        };
    }
}
