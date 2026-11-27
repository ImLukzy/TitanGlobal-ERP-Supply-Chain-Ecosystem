using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Queries;

public class ListarTransportistasTercerosQuery : IRequest<PaginaTransportistasTercerosDto>
{
    public string? Busqueda { get; set; }

    public string? Pais { get; set; }

    public string? TipoServicio { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarTransportistasTercerosQueryHandler : IRequestHandler<ListarTransportistasTercerosQuery, PaginaTransportistasTercerosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarTransportistasTercerosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaTransportistasTercerosDto> Handle(ListarTransportistasTercerosQuery request, CancellationToken cancellationToken)
    {
        ValidacionLogisticaExterna.Pagina(request.Pagina, request.TamanoPagina);
        IQueryable<TransportistaTercero> consulta = _unitOfWork.Repository<TransportistaTercero>().AsQueryable();
        // PATRÓN LINQ 1: filtros compuestos, sin cargar la tabla completa.
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            string termino = request.Busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(t => t.RazonSocial.ToLower().Contains(termino) || t.DocumentoFiscal.ToLower().Contains(termino));
        }
        if (!string.IsNullOrWhiteSpace(request.Pais))
        {
            string filtroPais = request.Pais.Trim().ToLowerInvariant();
            consulta = consulta.Where(t => t.Pais.ToLower() == filtroPais);
        }
        if (!string.IsNullOrWhiteSpace(request.TipoServicio))
        {
            string tipo = ValidacionLogisticaExterna.TipoServicio(request.TipoServicio);
            consulta = consulta.Where(t => t.TipoServicio == tipo);
        }
        if (request.Activo.HasValue) { consulta = consulta.Where(t => t.Activo == request.Activo.Value); }
        int total = await consulta.CountAsync();
        long desplazamiento = ((long)request.Pagina - 1) * request.TamanoPagina;
        List<TransportistaTerceroListadoDto> items = new List<TransportistaTerceroListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderBy(t => t.RazonSocial).ThenBy(t => t.Id).Skip((int)desplazamiento).Take(request.TamanoPagina)
                .Select(t => new TransportistaTerceroListadoDto
                {
                    Id = t.Id,
                    RazonSocial = t.RazonSocial,
                    DocumentoFiscal = t.DocumentoFiscal,
                    Pais = t.Pais,
                    TipoServicio = t.TipoServicio,
                    Activo = t.Activo
                }).ToListAsync();
        }
        return new PaginaTransportistasTercerosDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
