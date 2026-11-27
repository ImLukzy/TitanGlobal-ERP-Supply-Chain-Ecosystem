using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Queries;

public class ListarConsolidacionesQuery : IRequest<PaginaConsolidacionesDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarConsolidacionesQueryHandler : IRequestHandler<ListarConsolidacionesQuery, PaginaConsolidacionesDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarConsolidacionesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaConsolidacionesDto> Handle(ListarConsolidacionesQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de página debe ser mayor que cero."); }
        if (request.TamanoPagina < 1 || request.TamanoPagina > 100) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100."); }
        IQueryable<ConsolidacionCarga> consulta = _unidad.Repository<ConsolidacionCarga>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Estado))
        {
            string? normalizado = new[] { "Borrador", "Confirmada", "Cancelada" }.FirstOrDefault(e => e.Equals(request.Estado.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizado is null) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado debe ser Borrador, Confirmada o Cancelada."); }
            consulta = consulta.Where(c => c.Estado == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(request.Busqueda)) { string termino = request.Busqueda.Trim().ToLowerInvariant(); consulta = consulta.Where(c => c.Codigo.ToLower().Contains(termino) || c.Nombre.ToLower().Contains(termino)); }
        int total = await consulta.CountAsync(); long salto = (long)(request.Pagina - 1) * request.TamanoPagina;
        List<ConsolidacionListadoDto> items = new List<ConsolidacionListadoDto>();
        if (salto < total)
        {
            List<ConsolidacionCarga> cargas = await consulta.OrderBy(c => c.Codigo).ThenBy(c => c.Id).Skip((int)salto).Take(request.TamanoPagina).Include(c => c.Lineas).ToListAsync();
            foreach (ConsolidacionCarga carga in cargas) { items.Add(CalculoCarga.Detalle(carga)); }
        }
        return new PaginaConsolidacionesDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
