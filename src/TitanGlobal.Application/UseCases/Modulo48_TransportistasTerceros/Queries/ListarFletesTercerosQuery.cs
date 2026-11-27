using MediatR;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Queries;

public class ListarFletesTercerosQuery : IRequest<PaginaFletesTercerosDto>
{
    public int? TransportistaId { get; set; }

    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarFletesTercerosQueryHandler : IRequestHandler<ListarFletesTercerosQuery, PaginaFletesTercerosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarFletesTercerosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaFletesTercerosDto> Handle(ListarFletesTercerosQuery request, CancellationToken cancellationToken)
    {
        ValidacionLogisticaExterna.Pagina(request.Pagina, request.TamanoPagina);
        IQueryable<FleteTercero> consulta = _unitOfWork.Repository<FleteTercero>().AsQueryable();
        if (request.TransportistaId.HasValue)
        {
            if (request.TransportistaId.Value <= 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El transportista es obligatorio."); }
            consulta = consulta.Where(f => f.TransportistaTerceroId == request.TransportistaId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            string termino = request.Busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(f => f.Referencia.ToLower().Contains(termino) || f.Origen.ToLower().Contains(termino) || f.Destino.ToLower().Contains(termino));
        }
        if (!string.IsNullOrWhiteSpace(request.Estado))
        {
            string estadoCanonico = request.Estado.Trim().Equals("Registrado", StringComparison.OrdinalIgnoreCase) ? "Registrado"
                : request.Estado.Trim().Equals("Cancelado", StringComparison.OrdinalIgnoreCase) ? "Cancelado" : string.Empty;
            if (estadoCanonico == string.Empty) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado debe ser Registrado o Cancelado."); }
            consulta = consulta.Where(f => f.Estado == estadoCanonico);
        }
        int total = await consulta.CountAsync();
        long desplazamiento = ((long)request.Pagina - 1) * request.TamanoPagina;
        List<FleteTerceroListadoDto> items = new List<FleteTerceroListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderByDescending(f => f.Fecha).ThenByDescending(f => f.Id).Skip((int)desplazamiento).Take(request.TamanoPagina)
                .Select(f => new FleteTerceroListadoDto
                {
                    Id = f.Id,
                    TransportistaId = f.TransportistaTerceroId,
                    TransportistaRazonSocial = f.TransportistaTercero.RazonSocial,
                    Referencia = f.Referencia,
                    Fecha = f.Fecha,
                    Origen = f.Origen,
                    Destino = f.Destino,
                    Importe = f.Importe.ToString("0.00", CultureInfo.InvariantCulture),
                    Moneda = f.Moneda,
                    Estado = f.Estado
                }).ToListAsync();
        }
        return new PaginaFletesTercerosDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
