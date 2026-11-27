using MediatR;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Queries;

public class ListarRutasDespachoQuery : IRequest<PaginaRutasDespachoDto>
{
    public string? Busqueda { get; set; }

    public string? Zona { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarRutasDespachoQueryHandler : IRequestHandler<ListarRutasDespachoQuery, PaginaRutasDespachoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarRutasDespachoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaRutasDespachoDto> Handle(ListarRutasDespachoQuery request, CancellationToken cancellationToken)
    {
        ValidacionRutaDespacho.Pagina(request.Pagina, request.TamanoPagina);
        IQueryable<RutaDespacho> consulta = _unitOfWork.Repository<RutaDespacho>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            string termino = request.Busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Codigo.ToLower().Contains(termino) || r.Nombre.ToLower().Contains(termino) || r.Origen.ToLower().Contains(termino) || r.Destino.ToLower().Contains(termino));
        }
        if (!string.IsNullOrWhiteSpace(request.Zona))
        {
            string zonaNormalizada = request.Zona.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Zona.Trim().ToLower() == zonaNormalizada);
        }
        if (request.Activo.HasValue) { consulta = consulta.Where(r => r.Activo == request.Activo.Value); }
        int total = await consulta.CountAsync();
        long desplazamiento = (long)(request.Pagina - 1) * request.TamanoPagina;
        List<RutaDespachoListadoDto> items = new List<RutaDespachoListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderBy(r => r.Codigo).ThenBy(r => r.Id).Skip((int)desplazamiento).Take(request.TamanoPagina)
                .Select(r => new RutaDespachoListadoDto
                {
                    Id = r.Id,
                    Codigo = r.Codigo,
                    Nombre = r.Nombre,
                    Zona = r.Zona,
                    Origen = r.Origen,
                    Destino = r.Destino,
                    Activo = r.Activo,
                    DistanciaKm = r.DistanciaKm.ToString("0.00", CultureInfo.InvariantCulture),
                    PeajeEstimado = r.PeajeEstimado.ToString("0.00", CultureInfo.InvariantCulture),
                    Moneda = r.Moneda
                }).ToListAsync();
        }
        return new PaginaRutasDespachoDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
