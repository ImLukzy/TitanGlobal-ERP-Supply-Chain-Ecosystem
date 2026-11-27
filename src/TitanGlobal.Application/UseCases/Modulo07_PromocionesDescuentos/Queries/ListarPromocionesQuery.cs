using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Queries;

public class ListarPromocionesQuery : IRequest<PaginaPromocionesDto>
{
    public string? Busqueda { get; set; }

    public string? Tipo { get; set; }

    public int? SegmentoId { get; set; }

    public bool? Vigentes { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarPromocionesQueryHandler : IRequestHandler<ListarPromocionesQuery, PaginaPromocionesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarPromocionesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaPromocionesDto> Handle(ListarPromocionesQuery request, CancellationToken cancellationToken)
    {
        ValidarPaginacion(request.Pagina, request.TamanoPagina);
        string? tipoNormalizado = NormalizarTipoFiltro(request.Tipo);
        IQueryable<Promocion> consulta = _unitOfWork.Repository<Promocion>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(promocion => promocion.Codigo.ToLower().Contains(termino)
                || promocion.Nombre.ToLower().Contains(termino));
        }

        if (tipoNormalizado is not null)
        {
            consulta = consulta.Where(promocion => promocion.Tipo == tipoNormalizado);
        }

        if (request.SegmentoId.HasValue)
        {
            consulta = consulta.Where(promocion => promocion.SegmentoClienteId == request.SegmentoId.Value);
        }

        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        if (request.Vigentes == true)
        {
            consulta = consulta.Where(promocion => promocion.Activa
                && promocion.FechaInicio <= hoy && promocion.FechaFin >= hoy);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<Promocion> promociones = await consulta.OrderByDescending(promocion => promocion.FechaInicio)
            .ThenBy(promocion => promocion.Codigo).Skip(saltar).Take(request.TamanoPagina).ToListAsync();
        List<PromocionListadoDto> items = await OperacionesPromociones.ProyectarListadoAsync(_unitOfWork, promociones, hoy);
        return new PaginaPromocionesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static string? NormalizarTipoFiltro(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return null;
        }

        string normalizado = tipo.Trim();
        if (normalizado.Equals("Producto", StringComparison.OrdinalIgnoreCase))
        {
            return "Producto";
        }

        if (normalizado.Equals("MontoMinimo", StringComparison.OrdinalIgnoreCase))
        {
            return "MontoMinimo";
        }

        throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, "El tipo de promoción no es válido.");
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }
    }
}
